// bun run geometry (brief 055, Splice): the geometry audit. Where people stand against the drawn floor (G-1), where they walk against what is drawn (G-2),
// where sight and fire run against what is drawn (G-3), and whether the kit pieces stand where the catalogue says (K-1).
// Writes out/geometry/geometry.json and out/geometry/report.html (heat maps, tables, in-game-angle pictures from Blender), and briefs/055/geometry.summary.json (the counts, committed).
// usage: bun tools/geometry/cli.ts [--world file] [--no-shots] [--check] [--ratchet]
//   --check    no pictures; exit 1 when a rule's count is above contracts/world/geometry.ratchet.json (a regression); prints each rule's distance to 0
//   --ratchet  write the current counts to contracts/world/geometry.ratchet.json (only ever lower them: run it after a fix)
import { existsSync, mkdirSync, readFileSync, statSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { World } from '../../contracts/world/world.schema';
import { auditGround, auditLegs, auditSight, auditWalk, NUDGE, routes } from './audit';
import { loadBounds, loadPieces, ROOT } from './pieces';
import { buildVisible, kitIdsIn } from './visible';
import { loadSim } from './sim';
import { type KitRow, writeReport } from './report';
import { evaluate, type Ratchet } from './rules';
import { runShots } from './shots';
import { walkExport, walkFloorAt } from './walk-export';

const args = process.argv.slice(2), flag = (k: string) => args.includes(k), opt = (k: string) => (args.includes(k) ? args[args.indexOf(k) + 1] : undefined);
const t0 = Date.now(), quiet = flag('--check'), log = (s: string) => { if (!quiet) console.log(`geometry: ${s} (${((Date.now() - t0) / 1000).toFixed(1)} s)`); };

const worldPath = resolve(ROOT, opt('--world') ?? 'out/world/harbour-district.world.json');
if (!existsSync(worldPath)) { console.error(`geometry: no world export at ${worldPath}; run \`bun run world\``); process.exit(1); }
const world: World = JSON.parse(readFileSync(worldPath, 'utf8'));
const worldSha = new Bun.CryptoHasher('sha256').update(readFileSync(worldPath)).digest('hex');
const stale = statSync(worldPath).mtimeMs < statSync(resolve(ROOT, 'contracts/world/harbour-district.plan.json')).mtimeMs;

const sim = loadSim(world);
const win = (() => { const c = [[sim.window.x0, sim.window.y0], [sim.window.x1, sim.window.y0], [sim.window.x0, sim.window.y1], [sim.window.x1, sim.window.y1]].map(b => sim.frame.toPlan(b as [number, number])); return { x0: Math.min(...c.map(p => p[0])), x1: Math.max(...c.map(p => p[0])), y0: Math.min(...c.map(p => p[1])), y1: Math.max(...c.map(p => p[1])) }; })();
log(`world ${worldSha.slice(0, 10)}${stale ? ' (older than the plan: run bun run world)' : ''}; window plan x ${win.x0}..${win.x1}, y ${win.y0}..${win.y1}`);
const ids = kitIdsIn(world, win), pieces = loadPieces(ids, log);
log(`${pieces.size} kit pieces measured`);
const vis = buildVisible(world, pieces, win);
log(`visible world: ${vis.R.nx}x${vis.R.ny} cells, ${vis.meta.size} placed elements; unmeasured kit ids: ${[...vis.unmeasured.keys()].join(', ') || 'none'}`);
const ctx = { sim, vis };
// the walk surface file the host would read (out/world/<plan id>.walk.json)
const walkFile = walkExport(sim, vis, sim.plan.id, worldSha);
const ground = auditGround(ctx), walk = auditWalk(ctx), sight = auditSight(ctx), route = routes(ctx, ground, walk);
log('audits done');
// the fix, measured: the same audit with the host standing people on the walk file (what is left of G-1 once the host reads it)
const hostNow = sim.host.at, floorAt = walkFloorAt(walkFile);
sim.host.at = b => floorAt([b[0] + NUDGE, b[1] + NUDGE]);
const groundFixed = auditGround(ctx), routeFixed = routes(ctx, groundFixed, walk);
sim.host.at = hostNow;

// K-1: every catalogued piece's mesh against its catalogue row
const used = new Set([...ids, ...(world.ground.pieces ?? []).filter(g => g.at[0] >= win.x0 && g.at[0] <= win.x1 && g.at[1] >= win.y0 && g.at[1] <= win.y1 && g.kit).map(g => g.kit!.id)]);
const kit: KitRow[] = [...loadBounds(log).values()].map(({ b, asset }): KitRow => {
  if ('error' in b) return { id: asset.id, status: asset.status, category: asset.category, minY: NaN, sizeDelta: NaN, tags: asset.tags?.use?.join(',') ?? '', used: used.has(asset.id), why: b.error };
  const sz = [b.max[0] - b.min[0], b.max[1] - b.min[1], b.max[2] - b.min[2]], dz = Math.max(...sz.map((v, i) => Math.abs(v - asset.size[i]!)));
  const why = [Math.abs(b.min[1]) > 0.03 && Math.abs(b.min[1]) <= 0.3 ? `mesh bottom ${b.min[1].toFixed(2)} m off the pivot (a piece that stands on the ground is modelled on it)` : '', dz > 0.03 ? `size ${dz.toFixed(2)} m off the catalogue` : ''].filter(Boolean).join('; ');
  return { id: asset.id, status: asset.status, category: asset.category, minY: b.min[1], sizeDelta: dz, tags: asset.tags?.use?.join(',') ?? '', used: used.has(asset.id), why };
});
const legs = auditLegs(ctx, world, route), rules = evaluate(ground, walk, sight, route.list, kit, legs);

const summary = {
  schema: 'suborned.geometry.summary/1', brief: '055', world: { sha256: worldSha, stale },
  window: { block: sim.window, cell: 0.25 },
  ground: { walkCells: ground.walkCells, walkableCells: ground.walkableCells, sunk: ground.sunk, floating: ground.floating, bands: ground.bands, byCause: ground.byCause },
  walk: { freeCells: walk.freeCells, throughSolid: walk.throughSolid, lowCeiling: walk.lowCeiling, brush: walk.brush, invisibleWall: walk.invisibleWall },
  sight: { drawnAtEye: sight.eyeMissing, drawnAtChest: sight.chestMissing, occludersWithGaps: sight.walls.filter(w => w.perimeter > 0 && w.share < 0.9).map(w => ({ id: w.id, share: w.share })) },
  routes: { total: route.list.length, off5cm: route.list.filter(r => Math.abs(r.sunkMax) > 0.05).length, off30cm: route.list.filter(r => Math.abs(r.sunkMax) > 0.3).length, off50cm: route.list.filter(r => Math.abs(r.sunkMax) >= 0.5).length, inSolid: route.list.filter(r => r.through > 0).length },
  guard: ground.places.find(p => p.id === 'd:Office entrance'),
  kit: { measured: kit.length, offPivotOrSize: kit.filter(k => k.why).length, placedHere: kit.filter(k => k.used && k.why).map(k => `${k.id}: ${k.why}`) },
  rules: rules.map(r => ({ id: r.id, count: r.count, unit: r.unit, example: r.example })),
  ifHostReadsWalkFile: { 'G-1a': groundFixed.persons.filter(p => Number.isFinite(p.delta) && Math.abs(p.delta) > 0.05).length, 'G-1b': routeFixed.list.filter(r => Math.abs(r.sunkMax) > 0.3).length, 'G-1c': groundFixed.sunk + groundFixed.floating },
};
mkdirSync(resolve(ROOT, 'out/geometry'), { recursive: true });
if (!flag('--check')) { writeFileSync(resolve(ROOT, `out/world/${sim.plan.id}.walk.json`), JSON.stringify(walkFile)); log(`walk surface out/world/${sim.plan.id}.walk.json (${walkFile.nx}x${walkFile.ny} cells; ${Object.entries(walkFile.counts).map(([k, v]) => `${v} ${k}`).join(', ')})`); }

// the ratchet: each rule's count may not rise
const ratchetPath = resolve(ROOT, 'contracts/world/geometry.ratchet.json');
if (flag('--ratchet')) {
  const r: Ratchet = { schema: 'suborned.geometry.ratchet/1', note: 'The most each geometry rule (tools/geometry/rules.ts) may count. Written by `bun run geometry --ratchet` from the current audit; fixes lower it, nothing raises it. The target of every rule is 0.', counts: Object.fromEntries(rules.map(x => [x.id, x.count])) };
  const prev = existsSync(ratchetPath) ? JSON.parse(readFileSync(ratchetPath, 'utf8')) as Ratchet & { changes?: unknown[] } : undefined;
  const raised = Object.entries(r.counts).filter(([k, v]) => prev && prev.counts[k] !== undefined && v > prev.counts[k]!).map(([k]) => k);
  if (raised.length) { console.error(`geometry: ${raised.join(', ')} would rise: add a reason to \`changes\` in the ratchet file by hand, then keep the count; nothing written`); process.exit(1); }
  writeFileSync(ratchetPath, JSON.stringify({ ...r, note: prev?.note ?? r.note, changes: prev?.changes }, null, 2) + '\n'); console.log(`geometry: ratchet written to ${ratchetPath}`);
}
if (flag('--check')) {
  const have: Ratchet | undefined = existsSync(ratchetPath) ? JSON.parse(readFileSync(ratchetPath, 'utf8')) : undefined;
  let bad = 0;
  for (const x of rules) {
    const cap = have?.counts[x.id];
    const over = cap === undefined || x.count > cap;
    if (over) bad++;
    console.log(`${over ? 'FAIL' : 'ok  '} ${x.id} ${x.count} ${x.unit}${cap === undefined ? ' (no ratchet)' : ` (ratchet ${cap}, target 0)`}: ${x.rule}${over ? `\n        e.g. ${x.example}` : ''}`);
  }
  if (stale) console.log('warn the world export is older than the plan: run bun run world');
  process.exit(bad ? 1 : 0);
}

writeFileSync(resolve(ROOT, 'out/geometry/geometry.json'), JSON.stringify({ ...summary, ground, walk, sight, legs, routes: route.list, checks: vis.checks, rulesFull: rules }, (k, v) => (k === 'cells' || k === 'throughCells' || k === 'invisibleCells' || k === 'ceilingCells') ? undefined : v));
mkdirSync(resolve(ROOT, 'briefs/055'), { recursive: true });
writeFileSync(resolve(ROOT, 'briefs/055/geometry.summary.json'), JSON.stringify(summary, null, 2) + '\n');
const shots = flag('--no-shots') ? undefined : await runShots({ ...ctx, pieces, world }, ground, walk, sight, log);
writeReport({ ...ctx, world, ground, walk, sight, routes: route, summary, shots, worldSha, stale, unmeasured: [...vis.unmeasured.keys()], kit });
if (shots) for (const s of shots) { mkdirSync(resolve(ROOT, 'briefs/055/shots'), { recursive: true }); writeFileSync(resolve(ROOT, 'briefs/055', s.file), readFileSync(resolve(ROOT, 'out/geometry', s.file))); }
log('report out/geometry/report.html');
for (const x of rules) console.log(`${x.id}  ${String(x.count).padStart(5)} ${x.unit.padEnd(10)} ${x.rule}`);
