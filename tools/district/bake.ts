// Compile the promoted SurrealDB graph and the district plan into one immutable, replayable game input.
import { createHash } from 'node:crypto';
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { prepare } from '../people-graph/source';
import { loadGraph } from '../people-graph/load';
import { sql } from '../rag/database';
import { streetPlaces, furnish, project, cafeCourt, type Venue } from './places';
import { districtPopulation, LOCAL } from './population';
import { streetscape, straightRuns } from './streetscape';
import { roofTerraces, waterfront } from './waterfront';
import type { StreetSurvey } from './survey';
import { civic } from '../plan/civic';
import { derive, type Plan } from '../plan/plan';
import { worldOf } from '../world/build';

const root = resolve(import.meta.dir, '../..');
const read = (file: string) => JSON.parse(readFileSync(resolve(root, file), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json');
const { world } = worldOf(plan, derive(plan), civic(plan.civic, read(plan.civic.block)));
const site = read('sim/data/handover.site.json');
const { origin, x, y } = plan.civic.blockToPlan;
type Point = [number, number];
function block(p: readonly number[]): Point {
  const dx = p[0]! - origin[0],
    dy = p[1]! - origin[1];
  return [Math.round(100 * (dx * x[0] + dy * x[1])), Math.round(100 * (dx * y[0] + dy * y[1]))];
}
const ground = world.ground;
const streets = [...ground.roads, ...ground.foot];
const ends = streets.flatMap((s) => s.line.map(block));
const cell = 200,
  margin = 1600;
const min = (axis: 0 | 1) => Math.floor((Math.min(...ends.map((p) => p[axis])) - margin) / cell) * cell;
const x0 = min(0),
  y0 = min(1);
const size = (axis: 0 | 1, start: number) =>
  Math.ceil((Math.max(...ends.map((p) => p[axis])) + margin - start) / cell);
const nx = size(0, x0),
  ny = size(1, y0);
const walk = new Uint8Array(nx * ny),
  drive = new Uint8Array(nx * ny);
function visit(
  ax: number,
  ay: number,
  bx: number,
  by: number,
  f: (idx: number, px: number, py: number) => void,
) {
  for (
    let i = Math.max(0, Math.floor((ax - x0) / cell));
    i <= Math.min(nx - 1, Math.floor((bx - x0) / cell));
    i++
  )
    for (
      let j = Math.max(0, Math.floor((ay - y0) / cell));
      j <= Math.min(ny - 1, Math.floor((by - y0) / cell));
      j++
    )
      f(i * ny + j, x0 + (i + 0.5) * cell, y0 + (j + 0.5) * cell);
}
for (const s of streets) {
  const road = s.id.startsWith('road/'),
    radius = s.width * 50,
    outer = radius + (road ? 320 : 0);
  const points = s.line.map(block);
  for (let k = 1; k < points.length; k++) {
    const [a, b] = points[k - 1]!,
      [c, d] = points[k]!,
      vx = c - a,
      vy = d - b;
    visit(
      Math.min(a, c) - outer,
      Math.min(b, d) - outer,
      Math.max(a, c) + outer,
      Math.max(b, d) + outer,
      (idx, px, py) => {
        const t = Math.max(0, Math.min(1, ((px - a) * vx + (py - b) * vy) / (vx * vx + vy * vy || 1)));
        const dist = Math.hypot(px - a - t * vx, py - b - t * vy);
        if (dist <= outer) walk[idx] = 1;
        if (road && dist <= radius - 120) drive[idx] = 1;
      },
    );
  }
}
function polygon(ring: readonly (readonly number[])[], f: (i: number) => void) {
  const pts = ring.map(block),
    xs = pts.map((p) => p[0]),
    ys = pts.map((p) => p[1]);
  visit(Math.min(...xs), Math.min(...ys), Math.max(...xs), Math.max(...ys), (idx, px, py) => {
    let inside = false;
    for (let a = 0, b = pts.length - 1; a < pts.length; b = a++) {
      const [ax, ay] = pts[a]!,
        [bx, by] = pts[b]!;
      if (ay > py !== by > py && px < ((bx - ax) * (py - ay)) / (by - ay) + ax) inside = !inside;
    }
    if (inside) f(idx);
  });
}
const surface = waterfront(world, block);
const survey: StreetSurvey = read('contracts/world/harbour-district.street-survey.json');
const planHash = createHash('sha256').update(readFileSync(resolve(root, 'contracts/world/harbour-district.plan.json'))).digest('hex');
if (survey.planHash !== planHash) throw Error('Refresh the exterior street survey from the matching OldTown swap');
const solid = new Uint8Array(walk.length), water = new Uint8Array(walk.length), occupied = new Uint8Array(walk.length);
function cellOf(point: Point) {
  const [a,b]=block(point);
  const i=Math.floor((a-x0)/cell), j=Math.floor((b-y0)/cell);
  return i<0||j<0||i>=nx||j>=ny?-1:i*ny+j;
}
survey.rows.forEach((row,y)=>{
  for(let k=0;k<row.length;k+=3) for(let x=row[k]!;x<row[k+1]!;x++){
    const idx=cellOf([survey.origin[0]+(x+.5)*survey.cell,survey.origin[1]+(y+.5)*survey.cell]);
    if(idx<0)continue;
    if(row[k+2]===1)walk[idx]=1;else solid[idx]=1;
  }
});
for (const p of [...ground.plots,...ground.quays])
  if (['square','open','park','quay','pavement','terrazzo'].includes(p.surface))
    polygon(p.ring, (i) => {
      walk[i] = 1;
    });
for (const p of ground.drives ?? [])
  polygon(p.ring, (i) => {
    walk[i] = drive[i] = 1;
  });
for (const p of [...ground.medians, ...(ground.islands ?? [])])
  polygon(p.ring, (i) => {
    drive[i] = 0;
  });
// The trunk has a reserved bed, with cross traffic at junctions. The turnaround rails
// share the circus carriageway, including the narrow mouth where they split in two.
const junctions = Object.values(plan.nodes).map(block);
for (const tram of world.transit.tram.filter(t => t.id.endsWith('/trunk'))) {
  const rail = line(tram).points;
  for (let k=1; k<rail.length; k++) {
    const [ax,ay]=rail[k-1]!, [bx,by]=rail[k]!, dx=bx-ax,dy=by-ay;
    visit(Math.min(ax,bx)-300, Math.min(ay,by)-300, Math.max(ax,bx)+300, Math.max(ay,by)+300, (idx,x,y)=>{
      if (!drive[idx]) return;
      const t=Math.max(0,Math.min(1,((x-ax)*dx+(y-ay)*dy)/(dx*dx+dy*dy||1)));
      if (Math.hypot(x-ax-t*dx,y-ay-t*dy) < 285 && !junctions.some(p=>Math.hypot(x-p[0],y-p[1])<1050)) drive[idx]=0;
    });
  }
}
for (const platform of world.transit.stops) polygon(platform.ring, i=>{walk[i]=1; drive[i]=0;});
for(const face of ground.footways??[]) polygon(face.table.ring,i=>{walk[i]=1;});
for(const p of ground.water) polygon(p.ring,i=>{water[i]=1;});
for (const p of [...ground.water, ...world.buildings.filter((b) => b.base < 2 && b.base + b.height > 1)])
  polygon(p.ring, (i) => {
    walk[i] = drive[i] = 0;
  });
for (const b of world.buildings.filter(b => b.base < 2 && b.base + b.height > 1)) polygon(b.ring,i=>{ occupied[i]=1; });
// The two stepped housing estates are retained from the district plan in OldTown.
// Their street-level podiums are separate from the flyby's exported building meshes.
for (const m of world.megastructures)
  for (const part of m.parts)
    if (part.base < 1.8 && part.base + part.height > 1)
      polygon(part.ring, i => { solid[i] = 1; occupied[i] = 1; });
for (const c of ground.circuses) {
  const [a, b] = block(c.centre),
    r = c.radius * 100;
  visit(a - r, b - r, a + r, b + r, (i, px, py) => {
    const d = Math.hypot(px - a, py - b);
    if (d <= r) {
      walk[i] = 1;
      drive[i] = Number(d > c.islandRadius * 100 + 120);
    }
  });
}
for(let i=0;i<walk.length;i++)if(solid[i] || water[i]) walk[i]=drive[i]=0;
const open=(p: Point)=>walk[cellOf(p)]===1;
const venues=streetPlaces(world,open);
const furniture: {at:Point; radius:number}[]=[];
for (const v of venues) {
  const court = cafeCourt(v);
  if (court.length) polygon(court, i=>{if (!solid[i] && !drive[i] && !water[i]) walk[i]=1;});
  furnish(v,(p,r)=>{
    if(furniture.some(f=>Math.hypot(f.at[0]-p[0],f.at[1]-p[1])<f.radius+r+1))return false;
    const samples: Point[]=[p,[p[0]-r,p[1]],[p[0]+r,p[1]],[p[0],p[1]-r],[p[0],p[1]+r]];
    return samples.every(q=>{const i=cellOf(q);return i>=0&&!solid[i]&&!water[i]&&!drive[i];});
  });
  for(const f of v.fixtures){
    furniture.push(f);
    const [a,b]=block(f.at),r=f.radius*100+35;
    visit(a-r,b-r,a+r,b+r,(idx,x,y)=>{if((a-x)**2+(b-y)**2<=r*r)walk[idx]=0;});
  }
}
// Leave the Handover's surveyed furniture and the original entrances alone.
const neighbourhood = streetscape(world,(p,r)=>{
  const [x,y]=block(p);
  if(x>=site.grid.x0 && x<site.grid.x0+site.grid.nx*site.grid.cell && y>=site.grid.y0 && y<site.grid.y0+site.grid.ny*site.grid.cell) return false;
  const samples: Point[]=[p,[p[0]+r,p[1]],[p[0]-r,p[1]],[p[0],p[1]+r],[p[0],p[1]-r]];
  return samples.every(q=>{const i=cellOf(q);return i>=0&&!solid[i]&&!occupied[i]&&!drive[i]&&!water[i];});
},venues.flatMap(v=>v.fixtures));
for (const c of neighbourhood.courts) polygon(c.ring,i=>{if(!solid[i]&&!occupied[i]&&!drive[i]&&!water[i])walk[i]=1;});
for (const b of neighbourhood.buildings) polygon(b.ring,i=>{walk[i]=0;solid[i]=1;});
for (const f of [...venues.flatMap(v=>v.fixtures), ...neighbourhood.fixtures]) {
  const [a,b]=block(f.at),r=f.radius*100+32;
  visit(a-r,b-r,a+r,b+r,(i,x,y)=>{if((a-x)**2+(b-y)**2<=r*r)walk[i]=0;});
}
furniture.push(...neighbourhood.fixtures);
function standing(p:Point,v:Venue): Point {
  const usable=(q:Point)=>open(q) && (v.style==='platform'||!drive[cellOf(q)]) &&
    !furniture.some(f=>Math.hypot(f.at[0]-q[0],f.at[1]-q[1])<f.radius+.55);
  if(usable(p))return p;
  for(let r=1;r<=12;r++)for(const [a,b] of [[0,1],[0,-1],[1,0],[-1,0],[1,1],[-1,1],[1,-1],[-1,-1]]) {
    const q=project({...v,at:p},r*a!,r*b!); if(usable(q))return q;
  }
  return v.at;
}
function rows(bits: Uint8Array, legacy: typeof site.grid, mark: string) {
  return Array.from({ length: nx }, (_, i) =>
    Array.from({ length: ny }, (_, j) => {
      const ix = Math.floor((x0 + (i + 0.5) * cell - legacy.x0) / legacy.cell),
        iy = Math.floor((y0 + (j + 0.5) * cell - legacy.y0) / legacy.cell);
      if (ix >= 0 && iy >= 0 && ix < legacy.nx && iy < legacy.ny)
        return legacy.rows[ix][iy] === '#' ? '#' : mark;
      return bits[i * ny + j] ? mark : '#';
    }).join(''),
  );
}
const p = await districtPopulation(await prepare(), venues, standing);
if (p.source.problems.length) throw Error(p.source.problems.join('\n'));
await loadGraph(p);
type Resident = { key: string; name: string; job: string; look: string; need: string };
type Tie = { a: string; b: string; kind: string; mutual: boolean };
type Stop = { who: string; place: string; at: Point; doing: string; from: number; seq: number };
const [people, ties, stops] = (await sql(
  `SELECT record::id(id) AS key,name,position AS job,look,record::id(need) AS need FROM person ORDER BY key;
SELECT record::id(in) AS a,record::id(out) AS b,record::id(kind) AS kind,mutual FROM tie WHERE introduces ORDER BY a,b,kind;
SELECT record::id(in) AS who,out.name AS place,out.at AS at,doing,from_min AS from,seq FROM stops_at WHERE out.at != NONE ORDER BY who,seq;`,
  p.db,
)) as [Resident[], Tie[], Stop[]];
const principal = new Set(Object.values(site.cast).map((v: any) => v.id));
const residents = people
  .filter((r) => !principal.has(r.key))
  .map((r) => ({
    ...r,
    stops: stops
      .filter((s) => s.who === r.key)
      .map((s) => ({ at: block(s.at), place: s.place, doing: s.doing, from: s.from })),
  }))
  .filter((r) => r.stops.length)
  .sort((a,b)=>Number(a.key.startsWith(LOCAL))-Number(b.key.startsWith(LOCAL)));
const ids = new Set(residents.map((r) => r.key));
function line(s: { id: string; line: readonly (readonly number[])[] }) {
  return { id: s.id, points: straightRuns(s.line.map(block)) };
}
function runs(bits: Uint8Array): number[][] {
  return Array.from({length:nx},(_,i)=>{
    const spans:number[]=[];
    for(let j=0;j<ny;) {
      if (!bits[i*ny+j]) { j++; continue; }
      const start=j++;
      while(j<ny && bits[i*ny+j]) j++;
      spans.push(start,j);
    }
    return spans;
  });
}
const data = {
  schema: 'suborned.district/1',
  graph: p.hash,
  grid: { x0, y0, nx, ny, cell, rows: rows(walk, site.grid, '.') },
  roads: { x0, y0, nx, ny, cell, rows: rows(drive, site.drive, 'r') },
  water: runs(water),
  walls: runs(solid),
  waterfront:surface,
  roofTerraces: roofTerraces(world,block),
  courts:neighbourhood.courts.map(c=>({...c,ring:c.ring.map(block)})),
  buildings:neighbourhood.buildings.map(b=>({at:block(b.at),yaw:b.yaw,width:b.width,depth:b.depth,floors:b.floors,palette:b.palette})),
  galleries:neighbourhood.galleries.map(g=>({...g,at:block(g.at)})),
  streetFixtures:neighbourhood.fixtures.map(f=>({kit:f.kit, at:block(f.at),yaw:f.yaw})),
  venues:venues.map(v=>({id:v.id, name:v.name, at:block(v.at), style:v.style, court:cafeCourt(v).map(block), fixtures:v.fixtures.map(f=>({kit:f.kit,at:block(f.at),yaw:f.yaw}))})),
  platforms:world.transit.stops.map(s=>{
    const xs=s.ring.map(p=>p[0]),ys=s.ring.map(p=>p[1]);
    return {id:s.id,a:block([Math.min(...xs),Math.min(...ys)]),b:block([Math.max(...xs),Math.max(...ys)]),topCm:Math.round((s.base+s.height)*100)};
  }),
  residents,
  ties: ties.filter((t) => ids.has(t.a) && ids.has(t.b)),
  tram: world.transit.tram.map(line),
  streets: ground.roads.map(s => ({ ...line(s), laneCm: Math.round(s.width >= 18 ? s.width * 50 - 180 : s.width * 25) })),
};
const digest = createHash('sha256').update(JSON.stringify(data)).digest('hex');
writeFileSync(
  resolve(root, 'sim/Suborned.Sim/District/harbour.json'),
  JSON.stringify({ ...data, digest }) + '\n',
);
console.log(
  `${residents.length} residents, ${data.ties.length} introductions, ${data.streets.length} roads, ${data.tram.length} tram paths. ${digest}`,
);
