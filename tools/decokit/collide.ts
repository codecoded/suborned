// bun run collide [--force] [--only A,B] [--check] [--plan] [--out FILE]: the collision and hit volumes of every kit piece (brief 055, Vela), measured from the shipped
// mesh by Blender (collide.py) and kept in contracts/world/kit.collide.json, keyed by the FBX's sha256: a piece is measured again only when its mesh changes.
// Who declares a piece's volumes instead (its builder, its extra root, never brief 097's pins) is engine 0.49.0's law, tools/decokit/collide_law.ts.
// Light-only materials (kind glow: a glow on the paving, a strip up a wall, a crossing's beam) are left out of the measure (brief 107, tools/decokit/collide_light.ts).
// --only A,B: measure those pieces again even when their mesh is unchanged (the measure itself changed).
// --check: exit 1 if a catalogued piece with a mesh has no volumes for its current mesh (run `bun run collide`). Nothing is placed by hand.
// --plan: no Blender: list what a run would keep, measure and declare; with --out, write the records it keeps there. --out FILE: write there, not the contract.
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import type { KitAsset } from '../../contracts/world/kit.schema';
import { BLENDER, fbxOf, kitAssets, ROOT } from '../geometry/pieces';
import { GEN_ROOT } from '../kit/store';
import { declarationOf, planCollide, readPins, readRows } from './collide_law';
import { jobArg, lightOf, readLight } from './collide_light';

/** Bump when collide.py changes what it writes. */
export const COLLIDE_VERSION = 8;
export const COLLIDE_FILE = resolve(ROOT, 'contracts/world/kit.collide.json');

export type Volume = { shape: 'box'; min: [number, number, number]; max: [number, number, number]; stand?: true } | { shape: 'cylinder'; c: [number, number]; r: number; y0: number; y1: number; stand?: true };
/** A thin open part (a ring, hoop, rail, wire, lattice): measured and listed for information, never a blocker. */
export type ThinPart = { shape: 'box'; thin: true; min: [number, number, number]; max: [number, number, number] };
/** `light`: the light-only materials the measure left out (collide_light.ts); bounds, volumes and thin parts are of the rest, `tris` counts the whole mesh. */
export type PieceCollide = { sha256: string; source: 'measured' | 'declared'; declared?: string; class: 'solid' | 'flat' | 'elevated' | 'far'; bounds: [[number, number, number], [number, number, number]]; tris: number; cell: number; volumes: Volume[]; thin?: ThinPart[]; light?: string[] } | { sha256: string; error: string };
export type CollideFile = { schema: 'suborned.kit.collide/1'; version: number; frame: string; rule: string; pieces: Record<string, PieceCollide> };

const FRAME = 'kit frame: x across, y up, z front, metres from the piece pivot; yaw and scale apply as the piece is placed';
const RULE = 'a volume blocks a walker standing at floor height f when y0 < f + 1.8 and y1 > f + 0.35, unless stand and y1 - f <= 0.35 (a step); a shot at height h is stopped when y0 <= h <= y1; stand = the top is a drawn flat surface; a piece\'s `thin` boxes are open frames (rings, hoops, rails, wire, lattice, fronds) left out of the volumes: they stop neither a walker nor a shot';
const sha = (b: Uint8Array) => new Bun.CryptoHasher('sha256').update(b).digest('hex');

export function readCollide(): CollideFile | undefined { return existsSync(COLLIDE_FILE) ? JSON.parse(readFileSync(COLLIDE_FILE, 'utf8')) as CollideFile : undefined; }
/** The file's text as the tool writes it: pieces sorted by id, one piece a line. */
export function collideText(pieces: Record<string, PieceCollide>): string {
  const sorted = Object.fromEntries(Object.entries(pieces).sort(([a], [b]) => (a < b ? -1 : 1)));
  const file: CollideFile = { schema: 'suborned.kit.collide/1', version: COLLIDE_VERSION, frame: FRAME, rule: RULE, pieces: sorted };
  return JSON.stringify(file, null, 0).replace(/\},"/g, '},\n"').replace(/^\{/, '{\n') + '\n';
}
/** The plan of a plain run on this tree: the catalogue, the declaring tables (GEN_ROOT and the extra roots), the committed file and the meshes on disk. */
export function planHere(o: { only?: string[]; force?: boolean } = {}) {
  const assets = [...kitAssets().values()], pins = readPins(), rows = readRows(assets, GEN_ROOT);
  const light = readLight([GEN_ROOT, ...new Set(assets.flatMap(a => (a.extra ? [resolve(ROOT, a.extra)] : [])))]); // no catalogue on this machine: each record's own list stands
  return planCollide({ assets, prior: readCollide(), version: COLLIDE_VERSION, decl: a => declarationOf(a, rows, pins), fbx: fbxOf, sha: f => sha(readFileSync(f)), ...(light ? { light: (a: KitAsset) => lightOf(a.materials, light) } : {}), ...o });
}

if (import.meta.main) {
  const argv = process.argv.slice(2), flag = (k: string) => argv.includes(k), opt = (k: string) => { const i = argv.indexOf(k); return i >= 0 ? argv[i + 1] : undefined; };
  const only = opt('--only')?.split(',').filter(Boolean), out = opt('--out') ? resolve(opt('--out')!) : COLLIDE_FILE;
  const { pieces, todo, errors, unread } = planHere({ only, force: flag('--force') });
  const list = (ids: string[]) => `${ids.slice(0, 8).join(', ')}${ids.length > 8 ? ', ...' : ''}`;
  const pins = unread.filter(u => u.why === 'pin').map(u => u.id), rowless = unread.filter(u => u.why === 'no row').map(u => u.id);
  if (pins.length) console.log(`collide: ${pins.length} manifest rows still hold brief 097's pins (written before engine 0.49.0, kept by partial exports): no declaration, so those pieces keep their records (${list(pins)})`);
  if (rowless.length) console.log(`collide: ${rowless.length} generated pieces have no manifest row here (${GEN_ROOT} or their extra root): they keep their records (${list(rowless)})`);
  for (const e of errors) console.error(`collide: ${e.id}: ${e.why}`);
  if (flag('--check')) {
    if (todo.length || errors.length) { console.log(`collide: ${todo.length + errors.length} pieces have no volumes for their current mesh: ${list([...todo.map(t => t.id), ...errors.map(e => e.id)])}. Run bun run collide.`); process.exit(1); }
    console.log(`collide: ok, ${Object.keys(pieces).length} pieces, ${Object.values(pieces).reduce((n, p) => n + ('volumes' in p ? p.volumes.length : 0), 0)} volumes`); process.exit(0);
  }
  if (flag('--plan')) {
    for (const t of todo) console.log(`  ${t.declare ? 'declare' : 'measure'} ${t.id} (${t.why}${t.light.length ? `, without light ${t.light.join(',')}` : ''}${t.declare ? `, ${t.declare.volumes.length} volumes ${t.declare.hash.slice(0, 12)}` : ''})`);
    console.log(`collide: plan, ${Object.keys(pieces).length} kept, ${todo.filter(t => !t.declare).length} to measure, ${todo.filter(t => t.declare).length} to declare, ${errors.length} errors`);
    if (opt('--out')) { mkdirSync(dirname(out), { recursive: true }); writeFileSync(out, collideText(pieces)); console.log(`collide: wrote the ${Object.keys(pieces).length} kept records to ${out}${todo.length ? ` (${todo.length} to measure left out)` : ''}`); }
    process.exit(errors.length ? 1 : 0);
  }
  if (errors.length) process.exit(1);
  if (todo.length) {
    if (!existsSync(BLENDER)) { console.error(`collide: Blender not found at ${BLENDER} (set BLENDER)`); process.exit(1); }
    const tmp = resolve(ROOT, 'out/scratch/vela'); mkdirSync(tmp, { recursive: true });
    console.log(`collide: measuring ${todo.length} pieces in Blender`);
    for (let i = 0; i < todo.length; i += 25) {
      const batch = todo.slice(i, i + 25), part = resolve(tmp, 'collide.part.json');
      const p = Bun.spawnSync([BLENDER, '-b', '-P', resolve(ROOT, 'tools/decokit/collide.py'), '--', part, ...batch.map(t => jobArg(t.id, t.fbx, t.light))], { stdout: 'pipe', stderr: 'pipe' });
      if (p.exitCode !== 0 || !existsSync(part)) { console.error(`collide: Blender failed on ${batch.map(b => b.id).join(', ')}\n${p.stderr.toString().slice(-800)}`); process.exit(1); }
      const got = JSON.parse(readFileSync(part, 'utf8')) as Record<string, Omit<Extract<PieceCollide, { volumes: Volume[] }>, 'sha256'> & { error?: string }>;
      for (const { id, sha: s, declare } of batch) { const g = got[id]; pieces[id] = !g || g.error ? { sha256: s, error: g?.error ?? 'no result' } : declare ? { sha256: s, ...g, source: 'declared', declared: declare.hash, volumes: declare.volumes, class: 'solid' } as PieceCollide : { sha256: s, ...g, source: 'measured' } as PieceCollide; }
      console.log(`  ${Math.min(i + 25, todo.length)}/${todo.length}`);
    }
  }
  mkdirSync(dirname(out), { recursive: true }); writeFileSync(out, collideText(pieces));
  const all = Object.entries(pieces).sort(([a], [b]) => (a < b ? -1 : 1)), bad = all.filter(([, p]) => 'error' in p);
  const cls: Record<string, number> = {}; for (const [, p] of all) if ('class' in p) cls[p.class] = (cls[p.class] ?? 0) + 1;
  console.log(`collide: ${all.length} pieces (${Object.entries(cls).map(([k, v]) => `${v} ${k}`).join(', ')}), ${all.reduce((n, [, p]) => n + ('volumes' in p ? p.volumes.length : 0), 0)} volumes, ${todo.length} measured now; wrote ${out}`);
  for (const [id, p] of bad) console.log(`  ERROR ${id}: ${(p as { error: string }).error}`);
  process.exit(bad.length ? 1 : 0);
}
