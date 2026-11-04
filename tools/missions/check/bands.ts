// The Handover's bands (brief 080, Sky 2026-10-30: "Re turning the tests to check this shouldn't be so rigid"). Each adaptive plan (contracts/missions/<mission>/adaptive,
// played by sim/cli/Adaptive.cs: rules on what a player can see, a person's reaction time) plays across many seeds, and the result is read as win rates by playing
// style, fight length, idle time and the price of loud. Two kinds of line:
//  - RIGID, pass or fail, exit 1: no crash or stall, a saved run replays exactly from seed + orders, unknown = reject, every order shows its price, every fail names its
//    cause, no reference window under 30 s at a person's pace.
//  - BAND, a report: a miss is what Motive tunes from next, never a reason to change a plan to fit or to fail a build. Exit stays 0.
// The numbers are Motive's (briefs/080/design.html §7) except the loud split, which is Waymark's proposal for Motive to confirm (Sky's "preparation > improvisation holds").
//
// usage: bun run missions:check bands <mission> [--seeds n] [--seed-from k] [--engine tree] [--out dir]
//        one process and every core (`suborned-sim batch --seeds n --digest`); through `bun run guard` while anyone may be playing.
import { mkdirSync, readdirSync, readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { ROOT, build, env, type EngineAt } from './engine';
import { caseTrailEqOf } from './judge';

export type Row = {
  id: string; plan: string; style: string; seed: number; outcome: string; fail: string; tick: number; cash: number; trail: number; alert: number; gunfire: number; standing: number; startHash: string; finalHash: string;
  replayIdentical?: boolean; stats: { fightS: number; shots: number; hits: number; hpLost: number; down: number }; campaignTrail?: number; campaignStanding?: number; openCases?: number; cause?: { why?: string; culprits?: { id: string }[] };
  marks: Record<string, number>; idleMaxS: number; idleAt: number; inputs: number; refused: number; unpriced: number; giveUps: number; firstShot: number; held?: { tick: number; how: string; who: string }[]; fullToDown?: { op: string; s10: number }[]; refusals: Record<string, number>; given: Record<string, number>; never: string[]; error?: string;
};
export type Style = 'prepared-quiet' | 'thin-quiet' | 'careless' | 'prepared-loud' | 'unprepared-loud' | 'twin';
/** Brief 096 (Motive's rules page): a `twin` plays its quiet plan with one mistake (quiet-b-taped: the Pay on the live camera; drive-early: the car parked on the drive before the chime, no Reroute). */
export const TWINS: Record<string, string> = { 'quiet-b-taped': 'quiet-b', 'drive-early': 'quiet-b' };
export const REFERENCE: Style[] = ['prepared-quiet', 'prepared-loud'];
/** Motive's floor for each prepared route (briefs/080/motive/tuning): the Handover's three aren't equally forgiving, and needn't be. */
export const ROUTE_FLOOR: Record<string, number> = { 'quiet-a': 90, 'quiet-b': 80, 'quiet-g': 75 };

const q = (v: number[], p: number) => { const s = [...v].sort((a, b) => a - b); return s.length ? s[Math.min(s.length - 1, Math.floor(s.length * p))]! : 0; };
const med = (v: number[]) => q(v, 0.5);
/** Total Trail as the checker counts it (judge.ts totalTrailOf): the mission's, what the debrief carries, and each open case at its price. */
export const totalT = (r: Row, caseEq: number) => r.trail + (r.campaignTrail ?? 0) + (r.openCases ?? 0) * caseEq;
/** Heat as §01 reads it at the next mission's start: campaign T + Menace, how far Standing has gone below 0. */
export const heatOf = (r: Row, caseEq: number) => totalT(r, caseEq) + Math.max(0, -(r.standing + (r.campaignStanding ?? 0)));
/** A loud run: shots before the final fight or Alert ≥ 2 at its start: the engine's own word for it. */
export const wentLoud = (r: Row) => 'fight.loud' in r.marks;

export type Summary = {
  plan: string; style: string; plays: number; wins: number; winPct: number; fails: Record<string, number>; undecided: number;
  loudShare: number; winsLoud: number;
  fightMed: number; fightP25: number; fightP75: number; fightP95: number; fightMax: number; fightN: number;
  endMed: string; hpMed: number; downPerPlay: number; downPerWin: number;
  tMed: number; tMax: number; aMed: number; standingMed: number; heatMed: number; heatMax: number; tAll: number; standingAll: number;
  idleMed: number; idleP95: number; idleMax: number; refusedMed: number; inputsMed: number; reported: number; givenUp: number;
};
const clock = (t: number) => `${Math.floor(t / 600)}:${String(Math.floor(t / 10) % 60).padStart(2, '0')}`;
export function summarize(rows: Row[], caseEq: number): Summary {
  const r0 = rows[0]!, wins = rows.filter(r => r.outcome === 'success'), fights = rows.map(r => r.stats.fightS).filter(s => s > 0);
  const fails: Record<string, number> = {};
  for (const r of rows) if (r.outcome !== 'success') { const k = r.outcome === 'failure' ? r.fail : r.outcome; fails[k] = (fails[k] ?? 0) + 1; }
  return {
    plan: r0.plan, style: r0.style, plays: rows.length, wins: wins.length, winPct: Math.round(wins.length * 1000 / rows.length) / 10, fails, undecided: rows.filter(r => r.outcome === 'none').length,
    loudShare: Math.round(rows.filter(wentLoud).length * 100 / rows.length), winsLoud: wins.filter(wentLoud).length,
    fightMed: med(fights), fightP25: q(fights, 0.25), fightP75: q(fights, 0.75), fightP95: q(fights, 0.95), fightMax: q(fights, 1), fightN: fights.length,
    endMed: clock(med(wins.map(r => r.tick))), hpMed: med(rows.map(r => r.stats.hpLost)), downPerPlay: Math.round(rows.reduce((n, r) => n + r.stats.down, 0) * 100 / rows.length) / 100, downPerWin: wins.length ? Math.round(wins.reduce((n, r) => n + r.stats.down, 0) * 100 / wins.length) / 100 : 0,
    tMed: med(wins.map(r => totalT(r, caseEq))), tMax: q(wins.map(r => totalT(r, caseEq)), 1), aMed: med(wins.map(r => r.alert)), standingMed: med(wins.map(r => r.standing + (r.campaignStanding ?? 0))), heatMed: med(wins.map(r => heatOf(r, caseEq))), heatMax: q(wins.map(r => heatOf(r, caseEq)), 1),
    tAll: med(rows.map(r => r.trail)), standingAll: med(rows.map(r => r.standing)),
    idleMed: med(rows.map(r => r.idleMaxS)), idleP95: q(rows.map(r => r.idleMaxS), 0.95), idleMax: q(rows.map(r => r.idleMaxS), 1), refusedMed: med(rows.map(r => r.refused)), inputsMed: med(rows.map(r => r.inputs)), reported: rows.filter(r => 'attention.reporting' in r.marks).length, givenUp: rows.filter(r => r.giveUps > 0).length,
  };
}

export type Line = { id: string; rule: string; ok: boolean; value: string; note?: string };
/** The bands: Motive's, the loud split proposed. Pure, over one summary a plan. */
export function bands(s: Summary[], rows: Row[], caseEq = 3): Line[] {
  const L: Line[] = [], by = (st: Style) => s.filter(x => x.style === st);
  const win = (id: string, rule: string, st: Style, lo: number, hi: number, read: (x: Summary) => number = x => x.winPct) => { for (const x of by(st)) L.push({ id: `${id} ${x.plan}`, rule, ok: read(x) >= lo && read(x) <= hi, value: `${read(x)}%` }); };
  // Motive's rulings (briefs/080/motive/tuning, relayed by Aster 2026-10-30): a floor per route, thin 60-90, prepared loud 50-75 real loud wins, unprepared ≤ 20
  for (const x of by('prepared-quiet')) { const f = ROUTE_FLOOR[x.plan] ?? 90; L.push({ id: `B-1 ${x.plan}`, rule: `prepared quiet wins ≥ ${f} %`, ok: x.winPct >= f, value: `${x.winPct}%` }); }
  win('B-2', 'thin quiet wins 60-90 %', 'thin-quiet', 60, 90);
  win('B-3', 'careless wins ≤ 30 %', 'careless', 0, 30);
  // the loud split counts only wins that really went loud: a loud plan that never fired early won a quiet game
  const loudPct = (x: Summary) => Math.round(x.winsLoud * 1000 / x.plays) / 10;
  win('B-4', 'prepared loud wins 50-75 %: only wins whose final fight was loud', 'prepared-loud', 50, 75, loudPct);
  win('B-5', 'unprepared loud wins ≤ 20 %', 'unprepared-loud', 0, 20, loudPct);
  const pl = by('prepared-loud'), ul = by('unprepared-loud');
  if (pl.length && ul.length) { const a = Math.min(...pl.map(loudPct)), b = Math.max(...ul.map(loudPct)); L.push({ id: 'B-6', rule: 'prepared loud clearly above unprepared (≥ 40 points)', ok: a - b >= 40, value: `${a} % against ${b} %` }); }
  const quiet = [...by('prepared-quiet'), ...by('thin-quiet')];
  for (const x of quiet) L.push({ id: `B-7 ${x.plan}`, rule: 'quiet final fight: median 15-40 s (Sky 2026-10-30), 95 % under 90 s', ok: x.fightMed >= 15 && x.fightMed <= 40 && x.fightP95 < 90, value: `median ${x.fightMed} s, p95 ${x.fightP95} s` });
  for (const x of s.filter(x => (REFERENCE as string[]).includes(x.style))) L.push({ id: `B-8 ${x.plan}`, rule: 'no reference plan waits over 45 s with the whole cell idle (p95 of seeds)', ok: x.idleP95 <= 45, value: `median ${x.idleMed} s, p95 ${x.idleP95} s, worst ${x.idleMax} s` });
  for (const x of by('prepared-quiet')) L.push({ id: `B-9 ${x.plan}`, rule: 'agents down per win ≤ 0.5, prepared', ok: x.downPerWin <= 0.5, value: `${x.downPerWin}` });
  for (const x of [...pl, ...ul]) L.push({ id: `B-10 ${x.plan}`, rule: 'agents down per win ≤ 1.5, loud', ok: x.downPerWin <= 1.5, value: x.wins ? `${x.downPerWin}` : 'no win' });
  for (const x of s.filter(x => x.style !== 'careless' && x.style !== 'unprepared-loud')) L.push({ id: `B-11 ${x.plan}`, rule: 'variety: middle half of fight lengths spans ≥ 10 s', ok: x.fightP75 - x.fightP25 >= 10, value: `${x.fightP25}-${x.fightP75} s` });
  // every seed, at least one prepared quiet route wins (Motive's "worst single seed")
  const seeds = new Map<number, boolean>(); for (const r of rows) if (r.style === 'prepared-quiet') seeds.set(r.seed, (seeds.get(r.seed) ?? false) || r.outcome === 'success');
  const lost = [...seeds].filter(([, w]) => !w).map(([k]) => k);
  L.push({ id: 'B-12', rule: 'at least one prepared quiet route wins on every seed', ok: !lost.length, value: `${seeds.size - lost.length}/${seeds.size}${lost.length ? ` (lost on ${lost.slice(0, 8).join(', ')})` : ''}` });
  // what loud costs against quiet (design §7): on each seed a loud plan won loud, its Trail against the dearest quiet median, in 95 % of those seeds
  const qs = by('prepared-quiet').filter(x => x.wins), lw = rows.filter(r => r.outcome === 'success' && wentLoud(r) && r.style === 'prepared-loud');
  if (qs.length && lw.length) {
    const qT = Math.max(...qs.map(x => x.tMed)), lts = lw.map(r => totalT(r, caseEq)), ok = lts.filter(t => t >= 3 * qT).length;
    L.push({ id: 'B-13', rule: "loud's Trail at least 3× quiet's in 95 % of the loud wins", ok: ok * 100 >= 95 * lts.length, value: `${ok}/${lts.length} loud wins ≥ ${3 * qT} T (3 × quiet's dearest median ${qT} T); loud median ${med(lts)} T` });
  } else L.push({ id: 'B-13', rule: "loud's Trail at least 3× quiet's in 95 % of the loud wins", ok: false, value: 'too few loud wins to read' });
  // design §4: one roll can't lose the mission. First hit to down: nobody under 5 s, the Enforcer under 7 s, on every seed of every plan
  const falls = rows.flatMap(r => (r.fullToDown ?? []).map(f => ({ ...f, plan: r.plan, seed: r.seed }))).filter(f => f.s10 < 10000), en = falls.filter(f => f.op === 'enforcer'), rest = falls.filter(f => f.op !== 'enforcer');
  const min = (v: typeof falls) => v.length ? Math.min(...v.map(f => f.s10)) / 10 : Infinity, under = (v: typeof falls, s: number) => v.filter(f => f.s10 < s * 10).length;
  L.push({ id: 'B-14', rule: 'nobody goes full to down under 5 s, the Enforcer under 7 s (first hit to falling)', ok: !under(rest, 5) && !under(en, 7), value: `Enforcer: ${under(en, 7)} of ${en.length} falls under 7 s, fastest ${min(en)} s; others: ${under(rest, 5)} of ${rest.length} under 5 s, fastest ${min(rest)} s` });
  // brief 096 (Motive): a one-mistake twin is clearly worse than its quiet plan, and its mistake is seen (Alert on nearly every seed)
  for (const x of by('twin')) { const q = s.find(y => y.plan === TWINS[x.plan]); L.push({ id: `B-15 ${x.plan}`, rule: `one mistake costs: wins at least 10 points under ${TWINS[x.plan] ?? '(no twin)'}`, ok: !!q && x.winPct <= q.winPct - 10, value: q ? `${x.winPct} % against ${q.winPct} %` : 'no twin played' }); }
  for (const x of by('twin')) { const r = rows.filter(y => y.plan === x.plan), seen = r.filter(y => y.alert >= 1).length; L.push({ id: `B-16 ${x.plan}`, rule: 'the mistake is seen: Alert ≥ 1 on ≥ 95 % of seeds', ok: seen * 100 >= 95 * r.length, value: `${seen}/${r.length}` }); }
  return L;
}

/** The rigid lines read from the band's rows; the replay, reject and window lines are played (rigid below). */
export function rigidFromRows(rows: Row[], errors: { id: string; error?: string }[]): Line[] {
  const none = rows.filter(r => r.outcome === 'none'), unnamed = rows.filter(r => r.outcome === 'failure' && !(r.fail && r.cause));
  return [
    { id: 'R-1', rule: 'no crash: every play gave its line', ok: !errors.length, value: `${rows.length} plays, ${errors.length} errors`, ...(errors.length ? { note: `${errors[0]!.id}: ${errors[0]!.error}` } : {}) },
    { id: 'R-2', rule: 'no stall: every play reaches a verdict by the close', ok: !none.length, value: `${none.length} plays reach the close with no verdict`, ...(none.length ? { note: `first: ${none.slice(0, 6).map(r => `${r.plan}#${r.seed}`).join(', ')}` } : {}) },
    { id: 'R-3', rule: 'every fail names its cause (fail code and culprit)', ok: !unnamed.length, value: `${rows.filter(r => r.outcome === 'failure').length - unnamed.length}/${rows.filter(r => r.outcome === 'failure').length}` },
  ];
}

type Bad = { name: string; plan: unknown; why: RegExp };
const badPlan = (rules: unknown[]) => ({ schema: 'suborned.mission.adaptive/1', mission: 'handover', id: 'bad', style: 'careless', note: 'a plan the driver must reject', rules });
/** Unknown = reject, the driver's half: each of these must be an error line, not a play. */
export const BAD_PLANS: Bad[] = [
  { name: 'an unknown condition', plan: badPlan([{ id: 'a', op: 'scout', do: 'move', to: 'cabinet', when: ['telepathy:guard'] }]), why: /unknown condition/ },
  { name: 'a log line a player does not see', plan: badPlan([{ id: 'a', op: 'scout', do: 'move', to: 'cabinet', when: ['log:ai.kite'] }]), why: /isn't one a player sees/ },
  { name: 'a rule it waits on that is not there', plan: badPlan([{ id: 'a', op: 'scout', do: 'move', to: 'cabinet', after: ['nope'] }]), why: /no rule 'nope'/ },
  { name: 'an unknown key on a rule', plan: badPlan([{ id: 'a', op: 'scout', do: 'move', to: 'cabinet', peek: true }]), why: /peek/ },
  { name: 'an unknown pace', plan: { ...badPlan([{ id: 'a', op: 'scout', do: 'move', to: 'cabinet' }]), pace: 'psychic' }, why: /pace/ },
];
/** The steps whose window a person's pace must have ≥ 30 s of (design §6). A plan's other steps wait on what they see and have no window to lose. */
export const WINDOWS: Record<string, string[]> = {
  'quiet-a': ['call', 'enter', 'go'],
  'quiet-b': ['pay', 'tip', 'reroute', 'take', 'go'],
  'quiet-g': ['beside-guard', 'enter', 'go'],
};

/** Batch over paths, seeds n..: every line of `suborned-sim batch --digest`. The batch's exit is not read: a play that failed its checks is a row here. */
function batch(dll: string, dir: string, items: string[], seeds: number, from: number, out: string, replay = false) {
  const r = Bun.spawnSync(['dotnet', dll, 'batch', ...items, '--seeds', String(seeds), '--seed-from', String(from), '--digest', ...(replay ? [] : ['--no-replay']), '--out', out], { cwd: dir, env: { ...env, DOTNET_gcServer: '1', DOTNET_gcConcurrent: '0', DOTNET_TieredPGO: '0' }, stdout: 'pipe', stderr: 'pipe' });
  if (r.exitCode !== 0 && r.exitCode !== 1) throw new Error(`suborned-sim batch: exit ${r.exitCode}: ${r.stderr}`);
  return readFileSync(out, 'utf8').trim().split('\n').filter(Boolean).map(l => JSON.parse(l) as Row);
}

export type BandReport = { rows?: Row[]; seeds: number; from: number; engine: string; seconds: number; rigid: Line[]; bands: Line[]; plans: Summary[]; windows: { plan: string; rule: string; base: number; late: number; holds: boolean }[]; saved: { plan: string; seed: number; outcome: string; same: boolean }[] };

export async function runBands(mission: string, e: EngineAt, o: { seeds: number; from: number; log: (m: string) => void; windowSeeds?: number }): Promise<BandReport> {
  const t0 = performance.now(), dll = build(e), dir = e.dir, planDir = join(ROOT, 'contracts/missions', mission, 'adaptive'), tmp = join(ROOT, 'out/missions', mission, 'bands');
  mkdirSync(tmp, { recursive: true });
  const plans = readdirSync(planDir).filter(f => f.endsWith('.adaptive.json')).sort().map(f => join(planDir, f));
  const rulebook = JSON.parse(readFileSync(join(dir, 'contracts/rules/rulebook.json'), 'utf8')), caseEq = caseTrailEqOf(rulebook);
  o.log(`${plans.length} plans × ${o.seeds} seeds`);
  const all = batch(dll, dir, plans, o.seeds, o.from, join(tmp, 'rows.jsonl')), errors = all.filter(r => r.error), rows = all.filter(r => !r.error);
  const names = [...new Set(rows.map(r => r.plan))], plansOut = names.map(n => summarize(rows.filter(r => r.plan === n), caseEq));
  const rigid = rigidFromRows(rows, errors), bandLines = bands(plansOut, rows, caseEq);

  // R-4: every order shows its price: the engine held its quote for the acts it quotes (Listen, Turn), and every verb a plan gives has its price line on the card (text.json less.price)
  const verbs = new Set(plans.flatMap(p => (JSON.parse(readFileSync(p, 'utf8')).rules as { do: string; approach?: string }[]).map(x => x.approach ?? x.do)));
  const priceLines = JSON.parse(readFileSync(join(ROOT, 'contracts/missions', mission, 'text.json'), 'utf8')).less.price.verbs as Record<string, string[]>;
  const noLine = [...verbs].filter(v => !priceLines[v]), unpriced = rows.reduce((n, r) => n + r.unpriced, 0);
  rigid.push({ id: 'R-4', rule: 'every order shows its price: each verb given has its line on the card (text.json less.price), each Listen and Turn had its quote when given', ok: !noLine.length && !unpriced, value: `${verbs.size - noLine.length}/${verbs.size} verbs, ${unpriced} unquoted acts`, ...(noLine.length ? { note: noLine.join(', ') } : {}) });

  // R-5: a saved run replays exactly from seed + orders. For each plan, a win and a fail: the orders it gave go into a plain script, which replays through `play`'s path.
  o.log('saved runs');
  const saved: BandReport['saved'] = [];
  const picks = names.flatMap(n => { const v = rows.filter(r => r.plan === n), w = v.find(r => r.outcome === 'success'), f = v.find(r => r.outcome !== 'success'); return [w, f].filter(Boolean) as Row[]; });
  for (const r of picks) {
    const file = plans.find(p => p.endsWith(`/${r.plan}.adaptive.json`))!;
    const full = Bun.spawnSync(['dotnet', dll, 'batch', `${file}#seed=${r.seed}`], { cwd: dir, env, stdout: 'pipe', stderr: 'pipe' });
    const line = JSON.parse(full.stdout.toString().trim().split('\n')[0]!) as { finalHash: string; replayIdentical: boolean; tick: number; orders: { order: unknown }[] };
    const path = join(tmp, `saved-${r.plan}-${r.seed}.json`);
    writeFileSync(path, JSON.stringify({ schema: 'suborned.sim.script/1', mission, start: { seed: r.seed }, orders: line.orders.map(x => x.order), until: line.tick }));
    const again = Bun.spawnSync(['dotnet', dll, 'batch', path, '--digest', '--no-replay'], { cwd: dir, env, stdout: 'pipe', stderr: 'pipe' });
    const back = JSON.parse(again.stdout.toString().trim().split('\n')[0]!) as Row;
    saved.push({ plan: r.plan, seed: r.seed, outcome: r.outcome, same: line.replayIdentical && back.finalHash === line.finalHash && line.finalHash === r.finalHash });
  }
  rigid.push({ id: 'R-5', rule: 'a saved run replays exactly from seed + orders (a win and a fail of each plan; the bands\' own run gives the same hash)', ok: saved.length > 0 && saved.every(s => s.same), value: `${saved.filter(s => s.same).length}/${saved.length}`, ...(saved.some(s => !s.same) ? { note: saved.filter(s => !s.same).map(s => `${s.plan}#${s.seed}`).join(', ') } : {}) });

  // R-6: unknown = reject
  const off: string[] = [];
  for (const [i, b] of BAD_PLANS.entries()) {
    const path = join(tmp, `bad-${i}.adaptive.json`); writeFileSync(path, JSON.stringify(b.plan));
    const r = Bun.spawnSync(['dotnet', dll, 'batch', path, '--digest', '--no-replay'], { cwd: dir, env, stdout: 'pipe', stderr: 'pipe' });
    const l = JSON.parse(r.stdout.toString().trim().split('\n')[0]!) as { error?: string };
    if (!l.error || !b.why.test(l.error)) off.push(`${b.name}: ${l.error ?? 'it played'}`);
  }
  rigid.push({ id: 'R-6', rule: 'unknown = reject: an unknown condition, a log line a player does not see, a missing rule, an unknown key, an unknown pace', ok: !off.length, value: `${BAD_PLANS.length - off.length}/${BAD_PLANS.length}`, ...(off.length ? { note: off.join('; ') } : {}) });

  // R-7: no reference window under 30 s at a person's pace: the step given 30 s later than the plan would, the win rate stays within 10 points
  o.log('windows');
  const n = o.windowSeeds ?? o.seeds, windows: BandReport['windows'] = [], files: { plan: string; rule: string; path: string }[] = [];
  for (const [plan, steps] of Object.entries(WINDOWS)) {
    const src = plans.find(p => p.endsWith(`/${plan}.adaptive.json`)); if (!src) continue;
    for (const rule of steps) {
      const d = JSON.parse(readFileSync(src, 'utf8')); const it = d.rules.find((x: { id: string }) => x.id === rule);
      if (!it) throw new Error(`window ${plan} ${rule}: no such rule`);
      it.late = 30; const path = join(tmp, `${plan}~${rule}.adaptive.json`); writeFileSync(path, JSON.stringify(d)); files.push({ plan, rule, path });
    }
  }
  if (files.length) {
    const late = batch(dll, dir, files.map(f => f.path), n, o.from, join(tmp, 'late.jsonl')), base = batch(dll, dir, Object.keys(WINDOWS).map(p => plans.find(x => x.endsWith(`/${p}.adaptive.json`))!), n, o.from, join(tmp, 'late-base.jsonl'));
    const pct = (v: Row[]) => Math.round(v.filter(r => r.outcome === 'success').length * 100 / Math.max(1, v.length));
    for (const f of files) {
      const b = pct(base.filter(r => r.plan === f.plan)), l = pct(late.filter(r => r.id.startsWith(`${f.plan}~${f.rule}.`)));
      windows.push({ plan: f.plan, rule: f.rule, base: b, late: l, holds: l >= b - 10 });
    }
  }
  const short = windows.filter(w => !w.holds);
  rigid.push({ id: 'R-7', rule: 'no reference window under 30 s at a person\'s pace: each step 30 s late loses at most 10 points of wins', ok: !short.length, value: `${windows.length - short.length}/${windows.length}`, ...(short.length ? { note: short.map(w => `${w.plan} ${w.rule}: ${w.base} % → ${w.late} %`).join('; ') } : {}) });

  return { seeds: o.seeds, from: o.from, engine: e.rev === 'tree' ? 'tree' : e.commit.slice(0, 12), seconds: Math.round((performance.now() - t0) / 1000), rigid, bands: bandLines, plans: plansOut, windows, saved, rows };
}

export const table = (r: BandReport) => {
  const head = '| plan | style | wins | fails | loud | fight med (p25-p75, p95) | end | HP lost | down/win | T | A | Standing | Heat | idle med/p95 | reported |', sep = '|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|';
  const row = (x: Summary) => `| ${x.plan} | ${x.style} | ${x.winPct} % | ${Object.entries(x.fails).map(([k, v]) => `${k} ${v}`).join(', ') || '-'} | ${x.loudShare} % (${x.winsLoud} won) | ${x.fightMed} s (${x.fightP25}-${x.fightP75}, ${x.fightP95}) | ${x.endMed} | ${x.hpMed} | ${x.downPerWin} | ${x.tMed} | ${x.aMed} | ${x.standingMed} | ${x.heatMed} | ${x.idleMed}/${x.idleP95} s | ${x.reported} |`;
  return [head, sep, ...r.plans.map(row)].join('\n');
};
const esc = (s: unknown) => String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;');
export const html = (r: BandReport) => `<!doctype html><meta charset="utf-8"><title>Handover bands</title><style>body{font:14px system-ui;margin:2em;max-width:1200px}table{border-collapse:collapse;margin:1em 0}td,th{border:1px solid #ccc;padding:3px 7px;text-align:left}.ok{color:#176}.no{color:#b22;font-weight:600}</style>
<h1>Handover bands: ${r.plans.length} plans × ${r.seeds} seeds</h1><p>Engine ${esc(r.engine)}, ${r.seconds} s. T, A, Standing and Heat are medians of the wins. A band miss is a report for Motive, not a tuning instruction.</p>
<h2>Plans</h2><table><tr><th>plan</th><th>style</th><th>wins</th><th>fails</th><th>went loud</th><th>fight med</th><th>p25-p75</th><th>p95</th><th>end</th><th>HP</th><th>down/win</th><th>T</th><th>A</th><th>Standing</th><th>Heat</th><th>idle med</th><th>idle p95</th><th>reported</th></tr>
${r.plans.map(x => `<tr><td>${esc(x.plan)}</td><td>${esc(x.style)}</td><td>${x.winPct} %</td><td>${esc(Object.entries(x.fails).map(([k, v]) => `${k} ${v}`).join(', ') || '-')}</td><td>${x.loudShare} %, ${x.winsLoud} won</td><td>${x.fightMed} s</td><td>${x.fightP25}-${x.fightP75}</td><td>${x.fightP95}</td><td>${x.endMed}</td><td>${x.hpMed}</td><td>${x.downPerWin}</td><td>${x.tMed}</td><td>${x.aMed}</td><td>${x.standingMed}</td><td>${x.heatMed}</td><td>${x.idleMed}</td><td>${x.idleP95}</td><td>${x.reported}</td></tr>`).join('\n')}</table>
${[['Rigid (pass or fail)', r.rigid], ['Bands (a report)', r.bands]].map(([t, L]) => `<h2>${t}</h2><table>${(L as Line[]).map(l => `<tr><td class="${l.ok ? 'ok' : 'no'}">${l.ok ? 'ok' : 'MISS'}</td><td>${esc(l.id)}</td><td>${esc(l.rule)}</td><td>${esc(l.value)}</td><td>${esc(l.note ?? '')}</td></tr>`).join('\n')}</table>`).join('')}
<h2>Windows (each step 30 s late)</h2><table><tr><th>plan</th><th>step</th><th>wins as planned</th><th>30 s late</th></tr>${r.windows.map(w => `<tr><td>${esc(w.plan)}</td><td>${esc(w.rule)}</td><td>${w.base} %</td><td class="${w.holds ? 'ok' : 'no'}">${w.late} %</td></tr>`).join('')}</table>`;
