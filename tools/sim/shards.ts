// The C# engine suite in shards (Splice, brief 098 fast loop; Sky 2026-11-07 "yes" to a fast-loop pass): sim/Suborned.Sim.Tests as one Debug process
// outlasts 30 minutes, and one Release process still waits on its longest class (xUnit runs a class's cases one after another). Here the suite is built
// once in Release and cut into shards by class, then by test method, then (the 473-case theories) into slices of cases picked by display name; the
// shards run as single-threaded processes side by side, the longest first (the last run's times, out/sim-tests/timings.json, else shards.seed.json).
// Each shard writes a TRX; the summary names every failure with its message and the slowest tests; exit 1 on any failure or a shard that did not run.
// usage: bun run sim:test [--jobs N] [--debug] [--class A,B] [--filter <vstest expr>] [--slice N] [--no-build] [--plan] [--write-seed]
//   --jobs     shards at once (default: cores - 2)        --debug     the Debug build (slower; what `dotnet test` gives by default)
//   --class    only these test classes                    --filter    one plain vstest filter instead of the plan (a single shard)
//   --slice    at most this many cases in a slice (default 40)  --no-build  use the last build   --plan  print the shards and stop
//   --write-seed  after a run where every shard reported, rewrite tools/sim/shards.seed.json (per-method costs a checkout with no timings plans with)
// Run through the guard (`bun run sim:test` does): it uses every core.
import { cpus } from 'node:os';
import { existsSync, mkdirSync, readFileSync, readdirSync, writeFileSync } from 'node:fs';
import { join, resolve } from 'node:path';

export const ROOT = resolve(import.meta.dir, '../..');
const PROJECT = 'sim/Suborned.Sim.Tests';
const OUT = join(ROOT, 'out', 'sim-tests'), TIMINGS = join(OUT, 'timings.json'), SEED = join(import.meta.dir, 'shards.seed.json');

export type Shard = { id: string; filter: string; tests: string[]; cost: number };
export type Result = { name: string; outcome: string; seconds: number; message: string };

/** VSTest filter values escape these with a backslash (its FilterHelper's special characters; anything else escaped is an error). */
export const escape = (s: string) => s.replace(/[\\()&|=!~]/g, c => `\\${c}`);
/** "ScenarioTests.Passes(id: \"S01\")" → ["ScenarioTests", "ScenarioTests.Passes"]. */
export const nameOf = (t: string) => { const m = t.indexOf('('), fq = m >= 0 ? t.slice(0, m) : t; return { cls: fq.slice(0, fq.indexOf('.')), fq }; };

/** A test's seconds: the last run's (out/sim-tests/timings.json), else its method's average from the committed seed (shards.seed.json: method → [cases, s]), else 0.5. */
export type Costs = { tests: Record<string, number>; methods: Record<string, [number, number]> };
export const costOf = (c: Costs) => (t: string) => c.tests[t] ?? ((m?: [number, number]) => (m ? m[1] / Math.max(1, m[0]) : 0.5))(c.methods[nameOf(t).fq]);

/** Shards about total / (2 × jobs) seconds each, the longest first: a method is one shard; a theory above that (or above `maxCases` cases) is cut into
 *  slices of cases picked by display name, down to one case; methods of one class under it share a shard (each process costs about a second to start). */
export function plan(tests: string[], costs: Costs, jobs: number, maxCases = 40): Shard[] {
  const one = costOf(costs), cost = (ts: string[]) => ts.reduce((s, t) => s + one(t), 0);
  const byMethod = new Map<string, string[]>();
  for (const t of tests) { const { fq } = nameOf(t); (byMethod.get(fq) ?? byMethod.set(fq, []).get(fq)!).push(t); }
  const target = Math.max(cost(tests) / (2 * Math.max(1, jobs)), 1);
  const units: Shard[] = [];
  for (const [fq, ts] of byMethod) {
    if (!ts[0]!.includes('(') || (ts.length <= maxCases && cost(ts) <= target)) { units.push({ id: fq, filter: `FullyQualifiedName=${escape(fq)}`, tests: ts, cost: cost(ts) }); continue; }
    let part: string[] = [], n = 0;
    const cut = () => { if (!part.length) return; units.push({ id: `${fq}#${++n}`, filter: part.map(t => `DisplayName=${escape(t)}`).join('|'), tests: part, cost: cost(part) }); part = []; };
    for (const t of ts) { if (part.length && (part.length >= maxCases || cost(part) + one(t) > target)) cut(); part.push(t); }
    cut();
  }
  const merged: Shard[] = [], open = new Map<string, Shard>();
  for (const u of units.sort((a, b) => a.id.localeCompare(b.id))) {
    const cls = nameOf(u.tests[0]!).cls, o = open.get(cls);
    if (u.id.includes('#') || u.cost > target / 2) { merged.push(u); continue; }
    if (o && o.cost + u.cost <= target / 2) { o.id = `${cls}.*`; o.filter = `${o.filter}|${u.filter}`; o.tests.push(...u.tests); o.cost += u.cost; continue; }
    const s = { ...u, tests: [...u.tests] }; open.set(cls, s); merged.push(s);
  }
  return merged.sort((a, b) => b.cost - a.cost);
}

/** A TRX file's results (VSTest's own XML: UnitTestResult testName, outcome, duration, ErrorInfo/Message). */
export function readTrx(xml: string): Result[] {
  const out: Result[] = [];
  const dec = (s: string) => s.replace(/&quot;/g, '"').replace(/&apos;/g, "'").replace(/&lt;/g, '<').replace(/&gt;/g, '>').replace(/&#xD;/g, '').replace(/&#xA;/g, '\n').replace(/&amp;/g, '&');
  for (const m of xml.matchAll(/<UnitTestResult\b([^>]*?)(\/>|>([\s\S]*?)<\/UnitTestResult>)/g)) {
    const a = m[1]!, at = (k: string) => a.match(new RegExp(`\\b${k}="([^"]*)"`))?.[1] ?? '';
    const d = at('duration').match(/(\d+):(\d+):([\d.]+)/), seconds = d ? +d[1]! * 3600 + +d[2]! * 60 + +d[3]! : 0;
    out.push({ name: dec(at('testName')), outcome: at('outcome'), seconds, message: dec(m[3]?.match(/<Message>([\s\S]*?)<\/Message>/)?.[1] ?? '').trim() });
  }
  return out;
}

const env = { ...process.env, DOTNET_CLI_TELEMETRY_OPTOUT: '1', DOTNET_NOLOGO: '1', ...(process.env.DOTNET_ROOT || !existsSync('/opt/homebrew/opt/dotnet/libexec') ? {} : { DOTNET_ROOT: '/opt/homebrew/opt/dotnet/libexec' }) };
const arg = (k: string) => { const i = process.argv.indexOf(k); return i >= 0 ? process.argv[i + 1] : undefined; };

async function run(cmd: string[], log?: string): Promise<{ code: number; out: string }> {
  const p = Bun.spawn(cmd, { cwd: ROOT, env, stdout: 'pipe', stderr: 'pipe' });
  const [o, e, code] = await Promise.all([new Response(p.stdout).text(), new Response(p.stderr).text(), p.exited]);
  if (log) writeFileSync(log, `${cmd.join(' ')}\n\n${o}\n${e}`);
  return { code, out: o + e };
}

async function main() {
  if (!Bun.which('dotnet')) { console.error('sim:test: the .NET SDK (dotnet) is not on PATH'); process.exit(2); }
  const config = process.argv.includes('--debug') ? 'Debug' : 'Release', jobs = Number(arg('--jobs') ?? Math.max(1, cpus().length - 2)), slice = Number(arg('--slice') ?? 40);
  const t0 = Date.now(), stamp = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19), dir = join(OUT, stamp);
  mkdirSync(dir, { recursive: true });
  if (!process.argv.includes('--no-build')) {
    const b = await run(['dotnet', 'build', PROJECT, '-c', config, '-nologo', '-v', 'q'], join(dir, 'build.log'));
    if (b.code !== 0) { console.error(`sim:test: the build failed (${join(dir, 'build.log')})\n${b.out.split('\n').filter(l => /error/.test(l)).slice(0, 20).join('\n')}`); process.exit(1); }
  }
  // the built DLL, not the project: `dotnet test <project> --filter` goes through MSBuild, which rejects a display name's quotes and brackets
  const bin = join(ROOT, PROJECT, 'bin', config), tfm = existsSync(bin) ? readdirSync(bin).sort().pop() : undefined, dll = tfm ? join(bin, tfm, 'Suborned.Sim.Tests.dll') : '';
  if (!dll || !existsSync(dll)) { console.error(`sim:test: no ${config} build of ${PROJECT} (run without --no-build)`); process.exit(1); }
  const listed = await run(['dotnet', 'test', dll, '--list-tests']);
  const all = listed.out.split('\n').slice(listed.out.split('\n').findIndex(l => /following Tests are available/.test(l)) + 1).map(l => l.trim()).filter(Boolean);
  if (all.length === 0) { console.error(`sim:test: no tests listed\n${listed.out.slice(0, 2000)}`); process.exit(1); }
  const only = arg('--class')?.split(','), tests = only ? all.filter(t => only.includes(nameOf(t).cls)) : all;
  const timings: Record<string, number> = existsSync(TIMINGS) ? JSON.parse(readFileSync(TIMINGS, 'utf8')) : {};
  const seed: Record<string, [number, number]> = existsSync(SEED) ? JSON.parse(readFileSync(SEED, 'utf8')).methods : {};
  const shards = arg('--filter') ? [{ id: 'filter', filter: arg('--filter')!, tests, cost: 0 }] : plan(tests, { tests: timings, methods: seed }, jobs, slice);
  if (process.argv.includes('--plan')) { for (const s of shards) console.log(`${s.cost.toFixed(1).padStart(7)} s  ${String(s.tests.length).padStart(4)} tests  ${s.id}`); return; }
  console.log(`sim:test: ${tests.length} tests (${config}) in ${shards.length} shards, ${jobs} at a time → ${dir}`);

  const results: Result[] = [], broken: string[] = []; let next = 0, done = 0;
  const worker = async () => {
    for (let i = next++; i < shards.length; i = next++) {
      const s = shards[i]!, trx = `${String(i).padStart(3, '0')}.trx`, filterFile = join(dir, `${String(i).padStart(3, '0')}.filter`);
      writeFileSync(filterFile, s.filter);
      const r = await run(['dotnet', 'test', dll, '--filter', s.filter, '--logger', `trx;LogFileName=${trx}`, '--results-directory', dir,
        '--', 'xUnit.ParallelizeTestCollections=false', 'xUnit.MaxParallelThreads=1'], join(dir, `${String(i).padStart(3, '0')}.log`));
      const got = existsSync(join(dir, trx)) ? readTrx(readFileSync(join(dir, trx), 'utf8')) : [];
      results.push(...got); done++;
      if (got.length < s.tests.length) broken.push(`${s.id}: ${got.length} of ${s.tests.length} tests reported (exit ${r.code}; ${join(dir, `${String(i).padStart(3, '0')}.log`)})`);
      const bad = got.filter(x => x.outcome !== 'Passed' && x.outcome !== 'NotExecuted').length;
      process.stdout.write(`  [${String(done).padStart(3)}/${shards.length}] ${s.id} ${got.length} tests${bad ? `, ${bad} failed` : ''} (${Math.round((Date.now() - t0) / 1000)} s)\n`);
    }
  };
  await Promise.all(Array.from({ length: Math.min(jobs, shards.length) }, worker));

  const failed = results.filter(r => r.outcome === 'Failed'), passed = results.filter(r => r.outcome === 'Passed'), skipped = results.filter(r => r.outcome === 'NotExecuted');
  const next2 = { ...timings }; for (const r of results) next2[r.name] = Math.round(r.seconds * 1000) / 1000;
  writeFileSync(TIMINGS, JSON.stringify(next2, null, 0) + '\n');
  if (process.argv.includes('--write-seed') && !broken.length) { // the committed per-method costs a fresh checkout plans with
    const methods: Record<string, [number, number]> = {};
    for (const r of results) { const m = (methods[nameOf(r.name).fq] ??= [0, 0]); m[0]++; m[1] = Math.round((m[1] + r.seconds) * 10) / 10; }
    writeFileSync(SEED, JSON.stringify({ note: 'tools/sim/shards.ts: each test method\'s cases and seconds (Release, the Studio), the plan of a run with no out/sim-tests/timings.json; `bun run sim:test --write-seed` rewrites it', at: new Date().toISOString().slice(0, 10), methods }, null, 0).replace(/\],"/g, '],\n"') + '\n');
  }
  const wall = (Date.now() - t0) / 1000, cpu = results.reduce((s, r) => s + r.seconds, 0);
  const summary = { at: new Date().toISOString(), config, jobs, shards: shards.length, tests: tests.length, passed: passed.length, failed: failed.length, skipped: skipped.length, missing: broken, wallSeconds: Math.round(wall), testSeconds: Math.round(cpu),
    slowest: [...results].sort((a, b) => b.seconds - a.seconds).slice(0, 10).map(r => `${r.seconds.toFixed(1)} s ${r.name}`), failures: failed.map(f => ({ name: f.name, message: f.message.slice(0, 600) })) };
  writeFileSync(join(dir, 'summary.json'), JSON.stringify(summary, null, 1) + '\n');
  console.log(`\nsim:test: ${passed.length} passed, ${failed.length} failed, ${skipped.length} skipped of ${tests.length} in ${Math.round(wall)} s wall (${Math.round(cpu)} s of tests); slowest:`);
  for (const l of summary.slowest.slice(0, 5)) console.log(`  ${l}`);
  for (const f of failed) console.log(`FAIL ${f.name}\n  ${f.message.split('\n').slice(0, 4).join('\n  ')}`);
  for (const b of broken) console.log(`NOT RUN ${b}`);
  process.exit(failed.length || broken.length ? 1 : 0);
}

if (import.meta.main) await main();
