// Compile the promoted SurrealDB graph and the district plan into one immutable, replayable game input.
import { createHash } from 'node:crypto';
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { prepare } from '../people-graph/source';
import { loadGraph } from '../people-graph/load';
import { sql } from '../rag/database';
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
  const dx = p[0]! - origin[0], dy = p[1]! - origin[1];
  return [Math.round(100 * (dx * x[0] + dy * x[1])), Math.round(100 * (dx * y[0] + dy * y[1]))];
}
const ground = world.ground;
const streets = [...ground.roads, ...ground.foot];
const ends = streets.flatMap(s => s.line.map(block));
const cell = 200, margin = 1600;
const min = (axis: 0 | 1) => Math.floor((Math.min(...ends.map(p => p[axis])) - margin) / cell) * cell;
const x0 = min(0), y0 = min(1);
const size = (axis: 0 | 1, start: number) => Math.ceil((Math.max(...ends.map(p => p[axis])) + margin - start) / cell);
const nx = size(0, x0), ny = size(1, y0);
const walk = new Uint8Array(nx * ny), drive = new Uint8Array(nx * ny);
function visit(ax: number, ay: number, bx: number, by: number, f: (idx: number, px: number, py: number) => void) {
  for (let i = Math.max(0, Math.floor((ax-x0)/cell)); i <= Math.min(nx-1, Math.floor((bx-x0)/cell)); i++)
    for (let j = Math.max(0, Math.floor((ay-y0)/cell)); j <= Math.min(ny-1, Math.floor((by-y0)/cell)); j++)
      f(i*ny+j, x0+(i+0.5)*cell, y0+(j+0.5)*cell);
}
for (const s of streets) {
  const road = s.id.startsWith('road/'), radius = s.width*50, outer = radius + (road ? 320 : 0);
  const points = s.line.map(block);
  for (let k=1; k<points.length; k++) {
    const [a,b] = points[k-1]!, [c,d] = points[k]!, vx=c-a, vy=d-b;
    visit(Math.min(a,c)-outer, Math.min(b,d)-outer, Math.max(a,c)+outer, Math.max(b,d)+outer, (idx,px,py) => {
      const t = Math.max(0, Math.min(1, ((px-a)*vx+(py-b)*vy)/(vx*vx+vy*vy || 1)));
      const dist = Math.hypot(px-a-t*vx, py-b-t*vy);
      if (dist<=outer) walk[idx]=1;
      if (road && dist<=radius-120) drive[idx]=1;
    });
  }
}
function polygon(ring: readonly (readonly number[])[], f: (i: number) => void) {
  const pts=ring.map(block), xs=pts.map(p=>p[0]), ys=pts.map(p=>p[1]);
  visit(Math.min(...xs),Math.min(...ys),Math.max(...xs),Math.max(...ys),(idx,px,py)=>{
    let inside=false;
    for(let a=0,b=pts.length-1;a<pts.length;b=a++) {
      const [ax,ay]=pts[a]!, [bx,by]=pts[b]!;
      if ((ay>py)!==(by>py) && px<(bx-ax)*(py-ay)/(by-ay)+ax) inside=!inside;
    }
    if(inside) f(idx);
  });
}
for (const p of ground.plots) if (p.surface==='square') polygon(p.ring, i=>{walk[i]=1;});
for (const p of ground.drives ?? []) polygon(p.ring,i=>{walk[i]=drive[i]=1;});
for (const p of [...ground.medians,...(ground.islands ?? [])]) polygon(p.ring,i=>{drive[i]=0;});
for (const p of [...ground.water,...world.buildings.filter(b=>b.base<2)]) polygon(p.ring,i=>{walk[i]=drive[i]=0;});
for (const c of ground.circuses) {
  const [a,b]=block(c.centre), r=c.radius*100;
  visit(a-r,b-r,a+r,b+r,(i,px,py)=>{const d=Math.hypot(px-a,py-b); if(d<=r){walk[i]=1;drive[i]=Number(d>c.islandRadius*100+120);}});
}
function rows(bits: Uint8Array, legacy: typeof site.grid, mark: string) {
  return Array.from({length:nx},(_,i)=>Array.from({length:ny},(_,j)=>{
    const ix=Math.floor((x0+(i+.5)*cell-legacy.x0)/legacy.cell), iy=Math.floor((y0+(j+.5)*cell-legacy.y0)/legacy.cell);
    if(ix>=0&&iy>=0&&ix<legacy.nx&&iy<legacy.ny) return legacy.rows[ix][iy]==='#'?'#':mark;
    return bits[i*ny+j]?mark:'#';
  }).join(''));
}
const p=await prepare(); if(p.source.problems.length) throw Error(p.source.problems.join('\n')); await loadGraph(p);
type Resident={key:string;name:string;job:string;look:string;need:string};
type Tie={a:string;b:string;kind:string;mutual:boolean};
type Stop={who:string;place:string;at:Point;doing:string;from:number;seq:number};
const [people,ties,stops]=await sql(`SELECT record::id(id) AS key,name,position AS job,look,record::id(need) AS need FROM person ORDER BY key;
SELECT record::id(in) AS a,record::id(out) AS b,record::id(kind) AS kind,mutual FROM tie WHERE introduces ORDER BY a,b,kind;
SELECT record::id(in) AS who,out.name AS place,out.at AS at,doing,from_min AS from,seq FROM stops_at WHERE out.at != NONE ORDER BY who,seq;`,p.db) as [Resident[],Tie[],Stop[]];
const principal=new Set(Object.values(site.cast).map((v:any)=>v.id));
const residents=people.filter(r=>!principal.has(r.key)).map(r=>({...r,stops:stops.filter(s=>s.who===r.key).map(s=>({at:block(s.at),place:s.place,doing:s.doing,from:s.from}))})).filter(r=>r.stops.length);
const ids=new Set(residents.map(r=>r.key));
function line(s:{id:string;line:readonly (readonly number[])[]}) {
  // Remove collinear samples. Retain the exact bends of the exported lanes and tracks.
  const points:Point[]=[]; for(const pt of s.line.map(block)) {const b=points.at(-1),a=points.at(-2);if(b&&a&&(b[0]-a[0])*(pt[1]-b[1])===(b[1]-a[1])*(pt[0]-b[0]) && (pt[0]-b[0])*(b[0]-a[0])+(pt[1]-b[1])*(b[1]-a[1])>=0)points.pop();points.push(pt);} return {id:s.id,points};
}
const data={schema:'suborned.district/1',graph:p.hash,grid:{x0,y0,nx,ny,cell,rows:rows(walk,site.grid,'.')},roads:{x0,y0,nx,ny,cell,rows:rows(drive,site.drive,'r')},residents,ties:ties.filter(t=>ids.has(t.a)&&ids.has(t.b)),tram:world.transit.tram.map(line),streets:ground.roads.map(line)};
const digest=createHash('sha256').update(JSON.stringify(data)).digest('hex');
writeFileSync(resolve(root,'sim/Suborned.Sim/District/harbour.json'),JSON.stringify({...data,digest})+'\n');
console.log(`${residents.length} residents, ${data.ties.length} introductions, ${data.streets.length} roads, ${data.tram.length} tram paths. ${digest}`);
