// bun run collide [--force] [--only A,B] [--check]: the collision and hit volumes of every kit piece (brief 055, Vela), measured from the shipped mesh by Blender (collide.py)
// and kept in contracts/world/kit.collide.json, keyed by the FBX's sha256: a piece is measured again only when its mesh changes.
// --only A,B: measure those pieces again even when their mesh is unchanged (the measure itself changed).
// --check: exit 1 if a catalogued piece with a mesh has no volumes for its current mesh (run `bun run collide`). Nothing is placed by hand.
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { BLENDER, fbxOf, kitAssets, ROOT } from '../geometry/pieces';
import { GEN_ROOT } from '../kit/store';

/** Bump when collide.py changes what it writes. */
export const COLLIDE_VERSION = 8;
export const COLLIDE_FILE = resolve(ROOT, 'contracts/world/kit.collide.json');

export type Volume = { shape: 'box'; min: [number, number, number]; max: [number, number, number]; stand?: true } | { shape: 'cylinder'; c: [number, number]; r: number; y0: number; y1: number; stand?: true };
/** A thin open part (a ring, hoop, rail, wire, lattice): measured and listed for information, never a blocker. */
export type ThinPart = { shape: 'box'; thin: true; min: [number, number, number]; max: [number, number, number] };
export type PieceCollide = { sha256: string; source: 'measured' | 'declared'; declared?: string; class: 'solid' | 'flat' | 'elevated' | 'far'; bounds: [[number, number, number], [number, number, number]]; tris: number; cell: number; volumes: Volume[]; thin?: ThinPart[] } | { sha256: string; error: string };
export type CollideFile = { schema: 'suborned.kit.collide/1'; version: number; frame: string; rule: string; pieces: Record<string, PieceCollide> };

const FRAME = 'kit frame: x across, y up, z front, metres from the piece pivot; yaw and scale apply as the piece is placed';
const RULE = 'a volume blocks a walker standing at floor height f when y0 < f + 1.8 and y1 > f + 0.35, unless stand and y1 - f <= 0.35 (a step); a shot at height h is stopped when y0 <= h <= y1; stand = the top is a drawn flat surface; a piece\'s `thin` boxes are open frames (rings, hoops, rails, wire, lattice, fronds) left out of the volumes: they stop neither a walker nor a shot';
const sha = (b: Uint8Array) => new Bun.CryptoHasher('sha256').update(b).digest('hex');

/** Volumes a piece's builder declared (geo.Shape.solid), written by the generator into the manifest: they replace the measured ones for that piece. */
function declaredVolumes(): Map<string, Volume[]> {
  const f = resolve(GEN_ROOT, 'decokit_manifest.json'); if (!existsSync(f)) return new Map();
  const rows = (JSON.parse(readFileSync(f, 'utf8')).assets as { name: string; collide?: Volume[] }[]).filter(r => r.collide?.length);
  return new Map(rows.map(r => [r.name, r.collide!]));
}

export function readCollide(): CollideFile | undefined { return existsSync(COLLIDE_FILE) ? JSON.parse(readFileSync(COLLIDE_FILE, 'utf8')) as CollideFile : undefined; }

if (import.meta.main) {
  const argv = process.argv.slice(2), flag = (k: string) => argv.includes(k), opt = (k: string) => { const i = argv.indexOf(k); return i >= 0 ? argv[i + 1] : undefined; };
  const only = opt('--only')?.split(',').filter(Boolean);
  const decl = declaredVolumes(), dsha = (id: string) => (decl.has(id) ? sha(new TextEncoder().encode(JSON.stringify(decl.get(id)))) : undefined), prior = readCollide(), keep = prior && prior.version === COLLIDE_VERSION && !flag('--force') ? prior.pieces : {};
  const pieces: Record<string, PieceCollide> = {}, todo: [string, string, string][] = [];
  for (const a of [...kitAssets().values()].sort((x, y) => (x.id < y.id ? -1 : 1))) {
    if (a.status === 'rejected' || (only && !only.includes(a.id))) { if (keep[a.id]) pieces[a.id] = keep[a.id]!; continue; }
    const f = fbxOf(a); if (!f) continue;
    const s = sha(readFileSync(f)), k = keep[a.id];
    if (k && !only?.includes(a.id) && k.sha256 === s && ('declared' in k ? k.declared : undefined) === dsha(a.id)) pieces[a.id] = k; else todo.push([a.id, f, s]);
  }
  if (flag('--check')) {
    if (todo.length) { console.log(`collide: ${todo.length} pieces have no volumes for their current mesh: ${todo.slice(0, 8).map(t => t[0]).join(', ')}${todo.length > 8 ? ', ...' : ''}. Run bun run collide.`); process.exit(1); }
    console.log(`collide: ok, ${Object.keys(pieces).length} pieces, ${Object.values(pieces).reduce((n, p) => n + ('volumes' in p ? p.volumes.length : 0), 0)} volumes`); process.exit(0);
  }
  if (todo.length) {
    if (!existsSync(BLENDER)) { console.error(`collide: Blender not found at ${BLENDER} (set BLENDER)`); process.exit(1); }
    const tmp = resolve(ROOT, 'out/scratch/vela'); mkdirSync(tmp, { recursive: true });
    console.log(`collide: measuring ${todo.length} pieces in Blender`);
    for (let i = 0; i < todo.length; i += 25) {
      const batch = todo.slice(i, i + 25), out = resolve(tmp, 'collide.part.json');
      const p = Bun.spawnSync([BLENDER, '-b', '-P', resolve(ROOT, 'tools/decokit/collide.py'), '--', out, ...batch.map(([id, f]) => `${id}=${f}`)], { stdout: 'pipe', stderr: 'pipe' });
      if (p.exitCode !== 0 || !existsSync(out)) { console.error(`collide: Blender failed on ${batch.map(b => b[0]).join(', ')}\n${p.stderr.toString().slice(-800)}`); process.exit(1); }
      const got = JSON.parse(readFileSync(out, 'utf8')) as Record<string, Omit<Extract<PieceCollide, { volumes: Volume[] }>, 'sha256'> & { error?: string }>;
      for (const [id, , s] of batch) { const g = got[id]; pieces[id] = !g || g.error ? { sha256: s, error: g?.error ?? 'no result' } : decl.has(id) ? { sha256: s, ...g, source: 'declared', declared: dsha(id), volumes: decl.get(id)!, class: 'solid' } as PieceCollide : { sha256: s, ...g, source: 'measured' } as PieceCollide; }
      console.log(`  ${Math.min(i + 25, todo.length)}/${todo.length}`);
    }
  }
  const sorted = Object.fromEntries(Object.entries(pieces).sort(([a], [b]) => (a < b ? -1 : 1)));
  const file: CollideFile = { schema: 'suborned.kit.collide/1', version: COLLIDE_VERSION, frame: FRAME, rule: RULE, pieces: sorted };
  writeFileSync(COLLIDE_FILE, JSON.stringify(file, null, 0).replace(/\},"/g, '},\n"').replace(/^\{/, '{\n') + '\n');
  const all = Object.entries(sorted), bad = all.filter(([, p]) => 'error' in p);
  const cls: Record<string, number> = {}; for (const [, p] of all) if ('class' in p) cls[p.class] = (cls[p.class] ?? 0) + 1;
  console.log(`collide: ${all.length} pieces (${Object.entries(cls).map(([k, v]) => `${v} ${k}`).join(', ')}), ${all.reduce((n, [, p]) => n + ('volumes' in p ? p.volumes.length : 0), 0)} volumes, ${todo.length} measured now; wrote ${COLLIDE_FILE}`);
  for (const [id, p] of bad) console.log(`  ERROR ${id}: ${(p as { error: string }).error}`);
  process.exit(bad.length ? 1 : 0);
}
