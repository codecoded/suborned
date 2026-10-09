// bun run world: check the plan, then write the derived world as JSON for Unity (suborned.world/1).
// usage (repo root): bun tools/plan/export.ts [plan.json] [--out world.json]
// Exits 1 when any checker rule fails or any exported polygon is not simple. It then writes nothing and deletes the
// previous world at --out, so a builder can never build from a stale one.
import { mkdirSync, rmSync, writeFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { cliArgs, load, root } from './load';
import { fmtCheck } from './checks';
import { nonSimple } from './simple';
import { overlaps } from './overlap';
import { world } from './world';

const { src, opt } = cliArgs(process.argv.slice(2));
const out = resolve(root, opt('--out') ?? 'out/world/harbour-district.world.json');
const fail = (lines: string[], why: string): never => {
  rmSync(out, { force: true });
  for (const l of lines) console.error(l);
  console.error(`${why}: no world written, ${out} removed`);
  process.exit(1);
};

let loaded: ReturnType<typeof load>;
try { loaded = load(src); } catch (e) { fail([String(e)], 'plan failed to load'); }
const { plan, d, civ, R, planHash } = loaded!;
const failed = R.checks.filter(c => !c.ok);
if (failed.length) fail(failed.map(fmtCheck), `${failed.length} rule(s) failed`);

const w = world(plan, d, civ, R, planHash);
const bad = nonSimple(w);
if (bad.length) fail(bad.map(b => `FAIL  not simple  ${b}`), `${bad.length} polygon(s) not simple`);

const lap = overlaps(w);
if (lap.length) fail(lap.map(l => `FAIL  z-fight  ${l}`), `${lap.length} same-level overlap(s)`);

mkdirSync(dirname(out), { recursive: true });
const text = JSON.stringify(w) + '\n';
writeFileSync(out, text);
const counts = Object.entries(w).filter(([, v]) => Array.isArray(v)).map(([k, v]) => `${k} ${(v as unknown[]).length}`);
const h = w.handover;
console.log(`${R.checks.length} rules pass, all polygons simple, no same-level overlap; world → ${out} (${Buffer.byteLength(text)} bytes, plan ${planHash.slice(0, 12)})`);
console.log(counts.join(', '));
console.log(`handover: buildings ${h.buildings.length}, structures ${h.structures.length}, objects ${h.objects.length}, bays ${h.bays.length}, points ${h.points.length}, blocks ${h.blocks.length}`);
