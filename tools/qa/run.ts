// Brief 060 tier 1 (Suture): one QA pass. Seeds (the Handover routes, Sky's saved sessions) and their mutants play headless on each lane's
// engine through the QA host; every play goes through the invariant checks (tools/qa/checks); the pass writes out/qa/<run>/report.html (at
// most 10 ranked issues for Sky, then every issue, the win and price tables, the T2 and T9 proxies) and briefs/060/qa/<run>.summary.json.
// Only the exemplar plays' scripts are kept (out/qa/<run>/scripts/<lane>/), so any finding replays exactly.
//
// usage: bun run guard bun tools/qa/run.ts [--lanes build,tree] [--route-mutants n] [--session-mutants n] [--sessions dir] [--max-sessions n]
//                                          [--jobs n] [--no-replay] [--run id] [--no-summary]
//        bun tools/qa/run.ts --replay <script.json> [--engine build|tree|<rev>]     play one script and print its findings
//        bun tools/qa/run.ts --dev ...    a small pass while Sky plays (at most 300 plays a lane, 2 threads; no summary)
// Lanes: bot = tier 2, the built player playing itself through real input (tools/qa/bot.ts; ~10 min a row; `bun run qa --nightly` adds it).
// Lanes: build = the built player's engine and inputs (what Sky plays: his sessions); tree = the working tree (the routes, which are timed
// for its site, and the sessions). runPass() is the module entry point for Splice's `bun run qa`.
import { cpSync, existsSync, mkdirSync, readdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { ROOT } from '../missions/check/engine';
import { whoIsPlaying } from '../game/playing';
import { CHECKS, checkPlay, checkTwins } from './checks';
import { IssueBook } from './issues';
import { mutants, vocabulary } from './mutate';
import { html, summary, type LaneStat, type Pass } from './report';
import { routeSeeds, sessionSeeds, SESSIONS_DIR } from './seeds';
import { lane, playStream, type Lane } from './sim';
import { rowOf, tables, type Row } from './tables';
import { runStatics } from './statics';
import { loadText } from './text';
import { runBotLane, findingsOf, gradeRows } from './bot';
import { sessionChecks } from './bot-human';
import { DEFAULT_RULES, type Ctx, type Item, type Play, type Seed } from './types';

export type PassOpts = {
  lanes: string[]; routeMutants: number; sessionMutants: number; sessionsDir: string; maxSessions?: number; jobs?: number; replay: boolean;
  run: string; writeSummary: boolean; maxPlays?: number; say?: (m: string) => void; /** the bot lane (tier 2, `bot` in lanes): rows to play (default all of tools/qa/bot.json) and minutes a row */ botRows?: number; botMinutes?: number; /** run the static plan, world and geometry checks beside the plays (statics.ts): `bun run qa` does */ statics?: boolean;
};
export const DEFAULTS: PassOpts = { lanes: ['build', 'tree'], routeMutants: 60, sessionMutants: 12, sessionsDir: SESSIONS_DIR, replay: true, run: '', writeSummary: true };

const safe = (s: string) => s.replace(/[^a-z0-9.~_-]+/gi, '_');
const runId = () => new Date().toISOString().replace(/[-:]/g, '').replace(/\..*/, '').replace('T', '-');

/** The items a lane plays: each seed, its twin (Q-DET across plays), and its mutants. */
export function itemsFor(laneName: string, routes: Seed[], sessions: Seed[], o: PassOpts): Item[] {
  const seeds = laneName === 'build' ? sessions : [...routes, ...sessions];
  const v = vocabulary([...routes, ...sessions]), out: Item[] = [];
  for (const s of seeds) {
    out.push({ id: s.id, seed: s, mutations: [], script: s.script });
    out.push({ id: s.id + '~twin', seed: s, mutations: [], script: s.script, twin: true });
    out.push(...mutants(s, s.kind === 'route' ? o.routeMutants : o.sessionMutants, v));
  }
  return o.maxPlays ? out.slice(0, o.maxPlays) : out;
}

export async function runPass(opts: Partial<PassOpts> = {}): Promise<{ pass: Pass; dir: string; summaryFile?: string }> {
  const o: PassOpts = { ...DEFAULTS, ...opts }, say = o.say ?? ((m: string) => console.error(m));
  const run = o.run || runId(), dir = join(ROOT, 'out/qa', run), t0 = performance.now();
  mkdirSync(dir, { recursive: true });
  const routes = routeSeeds(ROOT);
  let sessions = sessionSeeds(o.sessionsDir); if (o.maxSessions) sessions = sessions.slice(-o.maxSessions);
  say(`qa ${run}: ${routes.length} routes, ${sessions.length} sessions; lanes ${o.lanes.join(', ')}`);
  const book = new IssueBook(3), rows: Row[] = [], stats: LaneStat[] = [], checks: Record<string, Record<string, number>> = {};
  for (const c of CHECKS) checks[c] = {};
  const playsPerLane: Record<string, number> = {};
  for (const spec of o.lanes) {
    const tl = performance.now(); let l: Lane;
    if (spec === 'bot') { await botLane(dir, o, book, stats, checks, playsPerLane, say); continue; }
    try { l = lane(spec); } catch (e) { say(`qa: lane ${spec} skipped: ${(e as Error).message.split('\n')[0]}`); continue; }
    const ctx: Ctx = { text: loadText(l.textFile), rules: DEFAULT_RULES };
    const items = itemsFor(l.name, routes, sessions, o), work = join(dir, 'work', l.name);
    mkdirSync(work, { recursive: true });
    const files = items.map((it, i) => { const f = join(work, `${String(i).padStart(5, '0')}-${safe(it.id)}.json`); writeFileSync(f, JSON.stringify(it.script)); return f; });
    writeFileSync(join(work, 'list.txt'), files.join('\n') + '\n');
    say(`qa: lane ${l.name} (${l.label}): ${items.length} plays`);
    const firstOf = new Map<string, Play>(); let errors = 0, replayChecked = 0, faithful = 0;
    const flagged = new Map<string, Set<string>>();
    const host = await playStream(l, join(work, 'list.txt'), (p, i) => {
      const it = items[i]!;
      if (p.error) errors++;
      if (!it.mutations.length && !it.twin && it.seed.kind === 'session' && it.seed.by === 'sky' && p.qa?.record === '') faithful++;
      if (p.replayIdentical !== null && p.replayIdentical !== undefined) replayChecked++;
      const fs = checkPlay(p, it, ctx);
      const twin = firstOf.get(it.seed.id);
      if (!it.mutations.length) { if (twin && it.twin) fs.push(...checkTwins(twin, p)); else if (!it.twin) firstOf.set(it.seed.id, p); }
      for (const f of fs) { const s = flagged.get(f.check) ?? new Set(); s.add(it.id); flagged.set(f.check, s); }
      book.add(l.name, it, fs);
      if (!it.twin) rows.push(rowOf(l.name, p, it));
    }, { jobs: o.jobs, replay: o.replay });
    for (const [c, s] of flagged) (checks[c] ??= {})[l.name] = s.size;
    playsPerLane[l.name] = items.length;
    stats.push({ lane: l, faithful, skySeeds: items.filter(i => !i.mutations.length && !i.twin && i.seed.by === 'sky').length, seeds: items.filter(i => !i.mutations.length && !i.twin).length, mutants: items.filter(i => i.mutations.length).length, plays: items.length, errors, replayChecked, seconds: (performance.now() - tl) / 1000, host });
    // keep the exemplars' scripts; drop the rest of the work folder
    const keep = join(dir, 'scripts', l.name); mkdirSync(keep, { recursive: true });
    for (const is of book.ranked(playsPerLane)) for (const ex of is.exemplars) if (ex.lane === l.name && !ex.script) {
      const k = items.findIndex(it => it.id === ex.item); if (k < 0) continue;
      const to = join(keep, `${safe(ex.item)}.json`); cpSync(files[k]!, to); ex.script = to.replace(ROOT + '/', '');
    }
    rmSync(work, { recursive: true, force: true });
    say(`qa: lane ${l.name} done in ${((performance.now() - tl) / 1000).toFixed(1)} s (${host})`);
  }
  if (existsSync(join(dir, 'work'))) rmSync(join(dir, 'work'), { recursive: true, force: true });
  const issues = book.ranked(playsPerLane);
  // change since the last pass with a summary
  const sumDir = join(ROOT, 'briefs/060/qa');
  const prevs = existsSync(sumDir) ? readdirSync(sumDir).filter(f => f.endsWith('.summary.json') && !f.startsWith(run)).sort() : [];
  const prior = prevs.map(f => { try { return JSON.parse(readFileSync(join(sumDir, f), 'utf8')); } catch { return null; } }).filter(Boolean) as any[];
  const last = prior[prior.length - 1];
  for (const is of issues) {
    const first = prior.find(s => s.issues?.some((x: any) => x.key === is.key));
    const where = (ls: { name: string; build?: string | null; engine: string }[], keys: string[]) => ls.filter(l => keys.includes(l.name)).map(l => l.build ? `build ${l.build}` : `${l.name} ${l.engine.replace('suborned.sim/', 'engine ')}`).join(', ');
    is.firstSeen = first ? `${first.run} (${where(first.lanes ?? [], Object.keys(first.issues.find((x: any) => x.key === is.key)?.plays ?? {}))})` : `${run} (${where(stats.map(s => s.lane), Object.keys(is.plays))})`;
    is.change = !last ? 'new' : last.issues?.some((x: any) => x.key === is.key) ? 'known' : first ? 'regressed' : 'new';
  }
  const fixed: string[] = last ? last.issues.filter((x: any) => !issues.some(i => i.key === x.key)).map((x: any) => `${x.key}: ${x.title}`) : [];
  const statics = o.statics ? runStatics(say) : undefined, was = new Set<string>((last?.statics?.failed ?? []).map((x: any) => x.id)), now = new Set<string>((statics?.failed ?? []).map(x => x.id));
  const staticsNew = last?.statics ? [...now].filter(i => !was.has(i)) : [], staticsFixed = last?.statics ? [...was].filter(i => !now.has(i)) : []; // the first pass with static checks has nothing to compare with: its failures are the baseline, not new
  const pass: Pass = { run, at: new Date().toISOString(), seconds: (performance.now() - t0) / 1000, lanes: stats, issues, tables: tables(rows), checks, previous: last?.run, fixed, root: ROOT, ...(statics ? { statics, staticsNew, staticsFixed } : {}) };
  writeFileSync(join(dir, 'report.html'), html(pass));
  writeFileSync(join(dir, 'rows.json'), JSON.stringify(rows));
  let summaryFile: string | undefined;
  if (o.writeSummary) { mkdirSync(sumDir, { recursive: true }); summaryFile = join(sumDir, `${run}.summary.json`); writeFileSync(summaryFile, JSON.stringify(summary(pass), null, 1) + '\n'); }
  return { pass, dir, summaryFile };
}

/** Tier 2 (Latch): the built player plays rows of the Handover through real input (tools/qa/bot.ts); each anomaly it kept is a finding with
 *  its shot and replay script. Rows play at real time (~10 min each); the guard is asked before each row. */
async function botLane(dir: string, o: PassOpts, book: IssueBook, stats: LaneStat[], checks: Record<string, Record<string, number>>, playsPerLane: Record<string, number>, say: (m: string) => void) {
  const tl = performance.now(), out = join(dir, 'bot');
  let r: Awaited<ReturnType<typeof runBotLane>>;
  try { r = await runBotLane(out, { rows: o.botRows, minutes: o.botMinutes, say }); } catch (e) { say(`qa: lane bot skipped: ${(e as Error).message}`); return; }
  if (r.skipped) { say(`qa: lane bot skipped (a note, not a finding): ${r.skipped}`); return; }
  if (r.report) await gradeRows(r.report.rows, out, { say }); // Suture's offline regrade (grade.ts): causes, races, held memory growth, the feet share
  if (r.report) await sessionChecks(r.report.rows, out, { say }); // the engine-side checks on each row's session (checks/human.ts): silent cancels, the loop left, piles, stale rings, leaks; a human row's intent
  const flagged = new Map<string, Set<string>>(); let errors = 0;
  for (const row of r.rows) {
    const rep = r.report?.rows.find(x => x.name === row.name); if (!rep && !r.ran.includes(row.name)) continue; // not started (Sky opened the game): not a finding
    const session = join(out, row.name, 'session.script.json'); let script: Item['script'];
    try { script = JSON.parse(readFileSync(session, 'utf8')); } catch { script = { schema: 'suborned.sim.script/1', mission: 'handover', start: {}, orders: [] }; }
    const it: Item = { id: `${row.name} (${row.policy}, seed ${row.seed})`, seed: { id: `bot ${row.policy} seed ${row.seed}`, kind: 'route', file: session, script }, mutations: [], script };
    const fs = findingsOf(rep, row.name, out); if (!rep || rep.status === 'failed') errors++;
    for (const f of fs) { const s2 = flagged.get(f.check) ?? new Set(); s2.add(row.name); flagged.set(f.check, s2); if (f.script) f.script = f.script.replace(ROOT + '/', ''); if (f.shot) f.shot = f.shot.replace(ROOT + '/', ''); }
    book.add('bot', it, fs);
  }
  for (const [c, s2] of flagged) (checks[c] ??= {})['bot'] = s2.size;
  playsPerLane['bot'] = Math.max(1, r.ran.length);
  const rows = r.report?.rows ?? [];
  const host = `${r.ran.length}/${r.rows.length} rows${r.stoppedForSky ? ' (stopped: Sky opened the game)' : ''}: ${rows.map(x => `${x.name} ${x.outcome || x.status}${x.failCode ? ` (${x.failCode})` : ''}, p99 ${x.frames.p99Ms} ms, ${x.input.taken} ways taken, ${x.orders.refused} refused`).join('; ')}`;
  const bl: Lane = { name: 'bot', dll: '', cwd: ROOT, textFile: '', engine: `the built player ${r.build}`, label: `the built player ${r.build} played through real input (random, greedy, cautious)`, inputs: 'the built player', build: r.build };
  stats.push({ lane: bl, faithful: 0, seeds: r.rows.length, mutants: 0, plays: r.ran.length, errors, replayChecked: 0, seconds: (performance.now() - tl) / 1000, host });
  say(`qa: lane bot done in ${((performance.now() - tl) / 1000).toFixed(0)} s (${host})`);
}

async function replayOne(script: string, engine: string) {
  const l = lane(engine), ctx: Ctx = { text: loadText(l.textFile), rules: DEFAULT_RULES };
  const tmp = join(ROOT, 'out/scratch/suture/qa-replay'); mkdirSync(tmp, { recursive: true });
  const abs = script.startsWith('/') ? script : join(process.cwd(), script);
  writeFileSync(join(tmp, 'list.txt'), abs + '\n');
  const sc = JSON.parse(readFileSync(abs, 'utf8'));
  const it: Item = { id: script, seed: { id: script, kind: 'route', file: abs, script: sc }, mutations: [], script: sc };
  await playStream(l, join(tmp, 'list.txt'), p => {
    console.log(`${p.id} on ${l.name} (${l.engine}): ${p.outcome}${p.fail ? ' (' + p.fail + ')' : ''} at ${Math.floor(p.tick / 600)}:${String(Math.floor(p.tick / 10) % 60).padStart(2, '0')}  $${100000 - p.cash} spent  T ${p.trail}  A ${p.alert}  replay ${p.replayIdentical}`);
    for (const l2 of p.log) if (!/^(shot\.|ai\.|crowd\.)/.test(l2.code)) console.log(`  ${(l2.tick / 10).toFixed(1).padStart(6)} s  ${l2.code.padEnd(26)} ${l2.text.slice(0, 140)}`);
    const fs = checkPlay(p, it, ctx).filter(f => f.check !== 'Q-OFFER'); // a replayed mutant is meant to be refused: Q-OFFER is for seeds
    console.log(fs.length ? fs.map(f => `  [${f.severity}] ${f.check} ${f.title}\n        expected: ${f.expected}\n        observed: ${f.observed}`).join('\n') : '  no findings');
  });
}

if (import.meta.main) {
  const a = process.argv.slice(2), val = (k: string) => { const i = a.indexOf(k); return i >= 0 ? a[i + 1] : undefined; };
  if (val('--replay')) { await replayOne(val('--replay')!, val('--engine') ?? 'tree'); process.exit(0); }
  const dev = a.includes('--dev');
  if (!dev && whoIsPlaying().length) { console.error('qa: Sky is playing; run this through `bun run guard bun tools/qa/run.ts` (it waits), or --dev for a small pass'); process.exit(3); }
  const o: Partial<PassOpts> = {
    lanes: (val('--lanes') ?? DEFAULTS.lanes.join(',')).split(',').filter(Boolean),
    routeMutants: Number(val('--route-mutants') ?? DEFAULTS.routeMutants), sessionMutants: Number(val('--session-mutants') ?? DEFAULTS.sessionMutants),
    sessionsDir: val('--sessions') ?? SESSIONS_DIR, maxSessions: val('--max-sessions') ? Number(val('--max-sessions')) : undefined,
    jobs: dev ? 2 : val('--jobs') ? Number(val('--jobs')) : undefined, replay: !a.includes('--no-replay'), run: val('--run') ?? (dev ? 'dev-' + runId() : ''),
    writeSummary: !dev && !a.includes('--no-summary'), maxPlays: dev ? 300 : undefined,
  };
  const t0 = performance.now();
  const { pass, dir, summaryFile } = await runPass(o);
  const sky = pass.issues.filter(i => i.severity !== 'info').slice(0, 10);
  console.log(`qa ${pass.run}: ${pass.lanes.map(l => `${l.lane.name} ${l.plays} plays (${l.errors} errors)`).join(', ')} in ${((performance.now() - t0) / 1000).toFixed(1)} s`);
  for (const [k, i] of sky.entries()) console.log(`  ${k + 1}. ${i.severity} ${i.check} ${i.title} [${Object.entries(i.plays).map(([l, n]) => `${l} ${n}`).join(', ')}] → ${i.owner}`);
  console.log(`report ${join(dir, 'report.html')}${summaryFile ? `\nsummary ${summaryFile}` : ''}`);
}
