// In-game-angle pictures (brief 055, Splice): Blender clay renders (scene.py) at the game's camera (pitch 38, yaw 30 or 210, orthographic), no Unity editor and no player.
// Each shows the kit pieces as the world places them, the people the host stands at the height it gives them (red), and a green disc on the floor that is drawn there.
import { copyFileSync, mkdirSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { World } from '../../contracts/world/world.schema';
import type { V } from '../plan/geom';
import type { GroundAudit, WalkAudit, SightAudit } from './audit';
import { BLENDER, ROOT, type PieceData } from './pieces';
import type { Sim } from './sim';
import type { Visible } from './visible';

export type Shot = { id: string; title: string; caption: string; file: string; yaw: number };
type Ctx = { sim: Sim; vis: Visible; pieces: Map<string, PieceData>; world: World };

const COLOUR: Record<string, string> = { civic: '#e6dccb', street: '#c9d3c4', facade: '#dcd2c0', frontage: '#d8cdb8', building: '#cdc6b8', ground: '#bdb6a6' };
const P = (b: V, sim: Sim) => sim.frame.toPlan(b);

export async function runShots(c: Ctx, ground: GroundAudit, walk: WalkAudit, sight: SightAudit, log: (s: string) => void): Promise<Shot[]> {
  const { sim, vis, world } = c, dir = resolve(ROOT, 'out/scratch/splice/geometry/shots'), outDir = resolve(ROOT, 'out/geometry/shots');
  mkdirSync(dir, { recursive: true }); mkdirSync(outDir, { recursive: true });
  const specs: { id: string; title: string; caption: string; yaw: number; target: [number, number, number]; scale: number; people?: { at: V; z: number; color?: string }[]; marks?: { at: V; z: number; color?: string }[]; wire?: { ring: V[]; base: number; top: number; color: string }[]; cap?: number }[] = [];

  // 1 + 2: the doors: the guard at the public landing, the councillor at the official door
  const place = (id: string) => sim.places.find(p => p.id === id)!;
  const stand = (id: string) => { const p = place(id), col = ground.places.find(r => r.id === id)!; const pp = P(p.at, sim); return { pp, host: col.host, top: col.top }; };
  { const s = stand('d:Office entrance'); specs.push({ id: 'office-entrance', title: 'The door guard at the public entrance', yaw: 30, scale: 9, target: [s.pp[0], s.pp[1], 0.9], people: [{ at: s.pp, z: s.host }], marks: [{ at: s.pp, z: s.top }], caption: `Red: the guard where the host puts him (floor ${s.host.toFixed(2)} m). Green disc: the landing the world draws under him (${s.top.toFixed(2)} m). He stands ${(s.top - s.host).toFixed(2)} m too low: hips in the stone.` }); }
  { const s = stand('d:Official entrance'); specs.push({ id: 'official-entrance', title: 'The official door', yaw: 210, scale: 9, target: [s.pp[0], s.pp[1], 0.6], people: [{ at: s.pp, z: s.host }], marks: [{ at: s.pp, z: s.top }], caption: `Floor under the door point: host ${s.host.toFixed(2)} m, drawn ${s.top.toFixed(2)} m.` }); }
  // 3: the café terrace and the seats the sim sits people on
  const seats = ground.places.filter(p => /^cafe-table-\d-seat-\d$/.test(p.id));
  { const t = P([-8, -29], sim);
    specs.push({ id: 'cafe-seats', title: 'Café terrace: the seat points against the drawn chairs', yaw: 30, scale: 34, target: [t[0], t[1], 0.5], people: seats.slice(0, 24).map(s => ({ at: P(s.at, sim), z: s.host, color: Math.abs(s.delta) > 0.15 ? '#d23a2a' : '#e9a23b' })), caption: 'Each figure stands at a seat point at the host height (0.18 m). Red where the drawn floor under the seat is more than 15 cm off (a chair seat or the table top); amber otherwise.' }); }
  // 3b: the worst table close up: the three seat points against the chairs
  { const tab = new Map<string, typeof seats>(); for (const sp of seats) { const k = sp.id.replace(/-seat-\d$/, ''); (tab.get(k) ?? tab.set(k, []).get(k)!).push(sp); }
    const worst = [...tab.values()].sort((a, b) => Math.max(...b.map(x => Math.abs(x.delta))) - Math.max(...a.map(x => Math.abs(x.delta))))[0];
    if (worst) { const pts = worst.map(w => P(w.at, sim)), cx = pts.reduce((s, p) => s + p[0], 0) / pts.length, cy = pts.reduce((s, p) => s + p[1], 0) / pts.length;
      specs.push({ id: 'seat-close', title: `A café table and its three seat points (${worst[0]!.id.replace(/-seat-\d$/, '')})`, yaw: 30, scale: 5.5, target: [cx, cy, 0.5], people: worst.map(w => ({ at: P(w.at, sim), z: w.host, color: '#d23a2a' })), marks: worst.map(w => ({ at: P(w.at, sim), z: w.top, color: '#1f9d55' })), caption: 'Red: a figure at each seat point at the host floor (0.18 m). Green: the floor the world draws under that point (a chair foot, the table top). The seat points are not on the chairs.' }); } }
  // 4: where walkers go through drawn furniture: the densest cluster of street furniture cells
  { const furn = walk.throughCells.filter(x => { const m = vis.meta.get(x.owner); return m && m.layer === 'street' && m.kind !== 'tree'; });
    const bins = new Map<string, { n: number; at: V }>(); for (const f of furn) { const k = `${Math.floor(f.b[0] / 12)},${Math.floor(f.b[1] / 12)}`; const e = bins.get(k) ?? { n: 0, at: f.b }; e.n++; bins.set(k, e); }
    const best = [...bins.values()].sort((a, b) => b.n - a.n)[0];
    if (best) { const t = P(best.at, sim), near = furn.filter(f => Math.hypot(f.b[0] - best.at[0], f.b[1] - best.at[1]) < 9);
      specs.push({ id: 'furniture', title: 'Walkers through street furniture', yaw: 30, scale: 20, target: [t[0], t[1], 0.8], people: near.filter((_, i) => i % 3 === 0).slice(0, 14).map(f => ({ at: P(f.b, sim), z: sim.host.at(f.b) })), caption: 'Each red figure stands on a cell the walk grid calls free: inside a bench, phone booth, vending machine or police post. The grid holds only the civic block\'s own solids.' }); } }
  // 5: sight: the occluders the sim has, as wire, against what is drawn
  { const t = P([-8, -30], sim);
    specs.push({ id: 'sight-cafe', title: 'Sight and fire: the café occluder against the drawn café', yaw: 30, scale: 42, target: [t[0], t[1], 2], wire: sim.occluders.filter(o => o.h > 3 && Math.hypot(...(sim.frame.toPlan(o.poly[0]!).map((v, i) => v - t[i]!) as [number, number])) < 60 && !o.id.startsWith('plot')).map(o => ({ ring: o.poly.map(q => P(q, sim)), base: o.z, top: Math.min(o.z + o.h, 7), color: '#2a6fd0' })), cap: 7, caption: 'Blue wire: what the engine treats as solid for sight and fire (up to 7 m shown). Clay: what is drawn. Where wire stands over nothing, a shot or a look goes through nothing; where clay stands outside any wire, it blocks nothing.' }); }
  // 6: the plaza: the cover blockers and the planter runs that block no shot
  { const t = P([14, -29], sim);
    specs.push({ id: 'sight-plaza', title: 'The square: the four tall cover objects against the drawn ones', yaw: 30, scale: 30, target: [t[0], t[1], 0.8], wire: sim.occluders.filter(o => o.id.startsWith('cover:')).map(o => ({ ring: o.poly.map(q => P(q, sim)), base: o.z, top: o.z + o.h, color: '#2a6fd0' })), caption: 'Blue wire: the only low objects that block a line at eye height (1.6 m). The planter runs, cabinets, lamps, kiosks and the cars are drawn but stop nothing.' }); }

  // build the Blender spec: every placed piece within reach of the target
  const shots = specs.map(sp => {
    const R = sp.scale * 0.9, t = sp.target;
    const near = (ring: V[]) => ring.some(p => Math.hypot(p[0] - t[0], p[1] - t[1]) < R);
    const pieces: object[] = [], prisms: object[] = [];
    for (const m of vis.meta.values()) {
      if (!near(m.ring)) continue;
      if (m.place && m.piece) { const pd = c.pieces.get(m.piece); if (pd?.fbx) pieces.push({ fbx: pd.fbx, at: m.place.at, yaw: m.place.yaw, scale: m.place.scale, z0: m.place.z0, color: COLOUR[m.layer] }); }
      else if (m.base !== undefined && m.layer !== 'ground') prisms.push({ ring: m.ring, base: m.base, top: Math.min(m.top, sp.cap ?? 9), color: COLOUR[m.layer] });
    }
    for (const pl of world.ground.plots) if (near(pl.ring as V[])) prisms.push({ ring: pl.ring, base: -0.3, top: pl.base + pl.height, color: '#bfb8a8' });
    prisms.push({ ring: [[t[0] - 90, t[1] - 90], [t[0] + 90, t[1] - 90], [t[0] + 90, t[1] + 90], [t[0] - 90, t[1] + 90]], base: -0.6, top: -0.01, color: '#9aa39f' });
    return { out: resolve(dir, `${sp.id}.jpg`), target: sp.target, yaw: sp.yaw, pitch: 38, scale: sp.scale, w: 1100, h: 800, pieces, prisms, people: sp.people ?? [], marks: sp.marks ?? [], wire: sp.wire ?? [] };
  });
  const specPath = resolve(dir, 'shots.json'); writeFileSync(specPath, JSON.stringify({ shots }));
  log(`rendering ${shots.length} pictures in Blender`);
  const r = Bun.spawnSync([BLENDER, '-b', '-P', resolve(ROOT, 'tools/geometry/scene.py'), '--', specPath], { stdout: 'pipe', stderr: 'pipe' });
  if (r.exitCode !== 0) { log(`pictures failed: ${r.stderr.toString().slice(-300)}`); return []; }
  return specs.map(sp => { copyFileSync(resolve(dir, `${sp.id}.jpg`), resolve(outDir, `${sp.id}.jpg`)); return { id: sp.id, title: sp.title, caption: sp.caption, file: `shots/${sp.id}.jpg`, yaw: sp.yaw }; });
}
void (null as unknown as SightAudit);
