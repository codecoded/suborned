// The mission checker's judgement (GDD §09 "mission checker: ships only if ≥ 2 quiet routes win · loud route exists and costs more T ·
// every fail condition reachable · no single contact essential · fully informed player finishes ≥ 1:00 before closing event";
// brief 011 B). Pure: it reads the route manifest, the scripts and what each play reported, and says which conditions hold and why.
// MC-0 every script plays as its manifest expects, and replays to identical checkpoints.
// MC-1 ≥ 2 quiet routes win: routes, not scripts (a variant of route A is still route A).
// MC-2 the loud route exists, wins, and costs more Trail than every quiet win, variants included (Motive's addendum).
// MC-3 every §07 fail is reached by a script that is played (a pending script reaches nothing yet, and is listed).
// MC-4 no single contact is essential: without each contact a quiet route still wins. The routes that need the contact are named,
//      and a contact that leaves fewer than 2 quiet routes is named as the one those routes depend on (brief 011 B).
// MC-6 T2 (§13): the fastest informed quiet win is not slower than the fastest loud win; the margin is reported.
// MC-7 careless violence is worse (brief 013 D): every careless script fails, or wins only at Alert ≥ 2 (brief 031: or at a higher
//      Alert than its careful twin's, as a body left unhidden and missed at its post's mark) and dearer in total Trail than its careful twin (a takedown seen, found or reported is never a cheap quiet win).
// MC-8 takedown routes are viable with a real time cost (brief 013 D): at least one quiet route whose orders take someone down wins
//      at Alert ≤ 1, and each such win spends Enforcer time on the takedown (the act, the lift, the carry, the stash: > 0 s).
// MC-9 turning people stays competitive (brief 013 D): no takedown win dominates every turning win, i.e. for each takedown win some
//      win that turns a contact is faster, or cheaper in total Trail.
// MC-10 quiet beats loud on every count (brief 018 §5): against every loud win (variants too), every quiet win costs less total Trail
//      (a captive's +captured-trail is campaign Trail), stands at a lower Alert, ends at a Standing no worse, and leaves no seal clock
//      running (anyone a loud getaway leaves standing is captured; a quiet one walks out).
// MC-11 no stop holds the car (brief 018 §1, §5): every place the final fight posts a hostile at (the ambush spots, the police stands)
//      stands outside fight-hold-m of the car and outside the exit lane, so only a hostile that comes for the car (the blocker, or one
//      that walks in) holds it; and every reference win's car left free (none waits on a hostile it merely stands at).
// MC-12 loitering costs (brief 031): no quiet script has a watcher report an agent loitering (the attention clock's report: a found
//      fault, Alert ≥ 1), and each careless-loiter script (its careful twin with an agent left standing in a watcher's sight) is
//      reported and fails, or ends at a higher Alert, where its twin wins.
// MC-13 every stand's loud-wait twin is held (brief 059, Motive M059-5.3): for each getaway stand the site places, a loud run whose cell
//      waits in the car while the Enforcer brings Vale out (`holds: <stand>`) never wins: with Vale aboard its Go is refused car-held (the
//      car held at his Board), or it is lost before he gets Vale in (the square: the Enforcer alone is shot down on the long walk), so no
//      stand opens an unfought loud exit.
// MC-14 the fight always comes (brief 059, Motive M059-S1.2; Sky 2026-10-25, S1 = no): on every win the checker plays (variations too),
//      Go comes at or after `blocker.out`.
// MC-15 no reference route reaches crew-wiped (brief 059, Motive M059-S1.5): no quiet or loud script logs `rivals.wiped` (the crew
//      destroyed, the fight over unfought).
// MC-3 counts a fail line that `needs` a site fact (brief 067: `patrol-watches-entry`, from `suborned-sim facts`) only where the fact holds
//      (§09: a fail that needs a watcher the site doesn't place is not counted); its script stays in the manifest, pending.
//      Since GDD v0.28.0 (Sky 2026-10-26) no Handover line needs one: entry seen by patrol is a report (engine 0.34.2), not a fail.
//      A line may name an engine scenario (`scenario`, brief 039, Keel's spec; Sky 2026-10-27): it is reached when `suborned-sim check
//      <scenario>` prints "<scenario>: ok" (the Handover's cell down is K3-CELL-DOWN, not a route).
// MC-11 reads every stand of the site (brief 059 K6: each with its own car, exit lane, ambush spots and police stands), and (brief 067,
//      Motive M067-SQ1, Aster's clause with Motive's refinement) no played script logs a hold that did not come for the car:
//      `car.held.answer` (an answer to fire) always counts; `car.held.post` counts when the holder's post is one the mission places (a rival
//      at or on his way to an ambush spot, a final-fight officer to a police stand), never a response-unit member's join point (`ai.join`: a
//      loud cell waiting at the car causes that hold itself) or a search unit at the stash it opens; blocker, press and advance came for it.
// Trail is compared in total (brief 013): the mission's own Trail plus what the debrief carries into the campaign (a knockout's
// statement, a missing person's file), plus `case-trail-eq` T for each open case it leaves (rulebook, rev 4 R4.4), so a body hidden
// until the debrief is not cheaper than it is.
// MC-5 the fully informed player (each primary quiet route as scripted) secures Vale ≥ informedSpareS before that route's closing
//      event (route A: inside before Vale exits at 5:50; route B: Vale taken before the 8:00 close). Each route's extraction time
//      against the mission's close, and how late each of its steps can be before it stops winning, are reported beside it.
import type { Routes, RouteScript } from '../../../contracts/missions/routes.schema';
import { scenarioOk, type Facts, type Play, type Scenarios, type Script } from './engine';
import { blockerOut } from './blocker';

/** What the record keeps of a play: enough to judge and to replay against. */
export type Digest = Pick<Play, 'outcome' | 'fail' | 'tick' | 'cash' | 'trail' | 'alert' | 'gunfire' | 'startHash' | 'finalHash' | 'replayIdentical'> & {
  rejected: number; scriptSha: string;
  /** the first tick of each event-log code (enter.in, take.done, …): when the route secured Vale */
  marks: Record<string, number>;
  /** the Enforcer's shots before the final fight (§07: the fight's branch is loud if any shot was fired before it) */
  shots: number;
  /** the final fight's branch, if one began */
  fight?: 'safe' | 'loud';
  /** brief 013: Trail the debrief carries into the campaign (a knockout's statement, a missing person), Standing now, open cases */
  campaignTrail?: number; standing?: number; openCases?: number;
  /** brief 013: seconds the takedown work held the Enforcer, from the first takedown.starts to the last of its act, lift, carry, stash or drop */
  takedownS?: number;
  /** brief 018 §1: seconds from the moment every blocker was out (compose.ts blockerOut) to the car leaving; absent if none was */
  carFreeS?: number;
  /** brief 018 §3: everyone the car left behind, op → "captured <why>" or "walked" (the debrief's left-behind lines) */
  left?: Record<string, string>;
  /** brief 067 (MC-11, M067-SQ1): each body's first hold of the car (`car.held.<how>`: who, how, when), and for a response-unit member
   *  the join point it was walking to (its `ai.join`); a base script's only */
  held?: { tick: number; how: string; who: string; join?: string }[];
  /** brief 059 (MC-13): the script's Go orders the engine refused, with the refusal code (car-held: a held car), and when Vale was aboard
   *  (board.in "… with Vale"); a base script's only */
  goRefused?: { tick: number; code: string }[]; valeIn?: number;
};
/** Trail in total: the mission's and what the debrief carries into the campaign (brief 013). */
export const totalTrailOf = (d: Digest, caseTrailEq = 0) => d.trail + (d.campaignTrail ?? 0) + (d.openCases ?? 0) * caseTrailEq;
/** The rulebook's price of an open case for the checker's totals (rev 4 R4.4, `case-trail-eq`, under campaign.cases.trailEq). */
export const caseTrailEqOf = (rulebook: { values?: Record<string, { v?: unknown }> }) => { const v = rulebook.values?.['case-trail-eq']?.v; return typeof v === 'number' ? v : 0; };
const TD_END = /^(takedown\.(out|dead)|carry\.(body|down)|stash\.)/;
/** `marks`: keep the event marks (a base script's; a variation needs only its outcome and hash). */
export const digest = (p: Play, scriptSha: string, keepMarks = true): Digest => {
  const marks: Record<string, number> = {};
  if (keepMarks) for (const l of p.log) if (!(l.code in marks) && !l.code.startsWith('order.')) marks[l.code] = l.tick;
  const fightAt = p.log.find(l => l.code === 'fight.safe' || l.code === 'fight.loud');
  const shots = p.log.filter(l => l.code.startsWith('fire.') && l.code !== 'fire.hostiles' && (!fightAt || l.tick < fightAt.tick)).length;
  const held: NonNullable<Digest['held']> = [], joins = new Map<string, string>();
  if (keepMarks) for (const l of p.log) {
    const j = l.code === 'ai.join' ? l.text.match(JOIN) : null;
    if (j) joins.set(j[1]!, j[2]!);
    if (l.code.startsWith('car.held.')) { const who = l.text.split(' ')[0]!, join = joins.get(who); held.push({ tick: l.tick, how: l.code.slice('car.held.'.length), who, ...(join ? { join } : {}) }); }
  }
  const goRefused = keepMarks ? p.orders.filter(o => o.order.do === 'go' && !o.ok).map(o => ({ tick: o.order.tick, code: o.code ?? 'refused' })) : [];
  const valeIn = keepMarks ? p.log.find(l => l.code === 'board.in' && / with Vale\.$/.test(l.text))?.tick : undefined;
  const free = blockerOut(p.log), nm = p.debrief?.nextMission, left = Object.fromEntries((nm?.lines ?? []).filter(l => l.line === 'left-behind').map(l => [l.who, l.detail.startsWith('walked') ? 'walked' : l.detail.replace(/^captured \((\w+)\)$/, 'captured $1')])), tdFrom = p.log.find(l => l.code === 'takedown.starts')?.tick, tdTo = p.log.filter(l => TD_END.test(l.code)).at(-1)?.tick;
  return {
    outcome: p.outcome, fail: p.fail, tick: p.tick, cash: p.cash, trail: p.trail, alert: p.alert, gunfire: p.gunfire, startHash: p.startHash, finalHash: p.finalHash, replayIdentical: p.replayIdentical,
    rejected: p.orders.filter(o => !o.ok).length, scriptSha, marks, shots, ...(fightAt ? { fight: fightAt.code === 'fight.safe' ? 'safe' as const : 'loud' as const } : {}),
    ...(nm?.campaignTrail ? { campaignTrail: nm.campaignTrail } : {}), ...(p.standing ? { standing: p.standing } : {}), ...(nm?.openCases ? { openCases: nm.openCases } : {}),
    ...(tdFrom !== undefined && tdTo !== undefined && tdTo >= tdFrom ? { takedownS: (tdTo - tdFrom) / 10 } : {}),
    ...(Object.keys(left).length ? { left } : {}),
    ...(free !== undefined && p.outcome === 'success' ? { carFreeS: (p.tick - free) / 10 } : {}),
    ...(held.length ? { held } : {}), ...(goRefused.length ? { goRefused } : {}), ...(valeIn !== undefined ? { valeIn } : {}),
  };
};
/** the engine's ai.join line (GameAi.cs): "<id> of the response unit joins the fight at <stand or cover>." */
const JOIN = /^(\S+) of the response unit joins the fight at (.+)\.$/;
/** the bodies the final fight sends to the posts the mission places at a stand (GameFight.cs, GameCrew.cs): the rival crew to its ambush
 *  spots, the loud fight's officers to its police stands; the response unit (security-n) joins where it stands, a search unit (police-n)
 *  goes to its stash. */
const MISSION_POST = /^(rival|officer)-\d+$/;
/** Every play the checker made, by name: `<script>`, `<script>~no-<role>`, `<script>~late@<tick>+<s>s`. */
export type Plays = Record<string, Digest>;
/** How late each planned step of a primary route can be (its steps up to the one that secures Vale; the final fight after it is
 *  fought, not planned): the most seconds, contiguous from 0, every later order can slip and still win. */
export type Slack = { script: string; steps: { tick: number; orders: string; slackS: number; breaksAs?: string }[]; maxS: number };

/** The getaway as the engine's site and rulebook give it (brief 018 §1): the car, the hold ring, the exit lane, the fight's posts. */
export type Getaway = { id?: string; car: [number, number]; holdCm: number; lane: [number, number][]; laneHalfCm: number; posts: { id: string; at: [number, number] }[] };
/** The engine's Site.InExitLane: within the lane's half width of a segment, between its ends, or of an inner joint. */
export function inLane(g: Getaway, p: [number, number]): boolean {
  const L = g.lane;
  for (let i = 1; i < L.length; i++) {
    const [ax, ay] = L[i - 1]!, [bx, by] = L[i]!, abx = bx - ax, aby = by - ay, apx = p[0] - ax, apy = p[1] - ay, ab2 = abx * abx + aby * aby, dot = apx * abx + apy * aby;
    if (dot < 0 || dot > ab2) continue;
    if (Math.abs(apx * aby - apy * abx) <= g.laneHalfCm * Math.sqrt(ab2)) return true;
  }
  return L.slice(1, -1).some(q => Math.hypot(p[0] - q[0], p[1] - q[1]) <= g.laneHalfCm);
}

export type Condition = { id: string; rule: string; ok: boolean; value: string; detail: string };
export type Report = { ok: boolean; conditions: Condition[]; scripts: Row[]; contacts: ContactRow[]; informed: InformedRow[]; slack: Slack[]; pending: string[] };
type Row = { id: string; kind: string; route?: string; expects: string; got: string; ok: boolean; at: string; trail: number; alert: number; shots: number; fightShots: number; fight: string; cash: number; replay: boolean };
type ContactRow = { role: string; who: string; quietWinsWithout: string[]; routesWithout: string[]; needs: string[]; named: boolean };
type InformedRow = { script: string; route: string; closes: string; secured: string; spareS: number; extracted: string; closeSpareS: number; slackS: number };

/** A quiet win: Vale at the getaway car with no shot before the final fight (the fight's own shots don't make a route loud, §07). */
export const quietWin = (d?: Digest) => !!d && d.outcome === 'success' && d.shots === 0;
export const clock = (tick: number) => { const s = Math.floor(tick / 10); return `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}${tick % 10 ? `.${tick % 10}` : ''}`; };
const ticksOf = (at: string) => { const [m, s] = at.split(':').map(Number); return (m! * 60 + s!) * 10; };
const got = (d: Digest) => (d.outcome === 'failure' ? `failure ${d.fail}` : d.outcome) + (d.left && Object.values(d.left).some(f => f !== 'walked') ? `; left: ${leftOf(d.left)}` : '');
const gotA = (s: RouteScript, d: Digest) => got(d) + (s.expects.alert !== undefined ? ` at A ${d.alert}` : '');
/** brief 031: a watcher reported an agent loitering (attention.reporting: the report may still be stopped before it lands) */
export const loiterReported = (d?: Digest) => !!d && 'attention.reporting' in d.marks;
const leftOf = (l?: Record<string, string>) => Object.entries(l ?? {}).sort(([a], [b]) => a.localeCompare(b)).map(([op, f]) => `${op} ${f}`).join(', ');
const want = (s: RouteScript) => (s.expects.fail ? `${s.expects.outcome} ${s.expects.fail}` : s.expects.outcome) + (s.expects.left ? `; left: ${leftOf(s.expects.left)}` : '') + (s.expects.alert !== undefined ? ` at A ${s.expects.alert}` : '');
/** brief 018: a script that names who is left behind must leave exactly them, so (as `want`) */
const met = (s: RouteScript, d?: Digest) => !!d && d.outcome === s.expects.outcome && (!s.expects.fail || d.fail === s.expects.fail) && (!s.expects.left || leftOf(s.expects.left) === leftOf(d.left)) && (s.expects.alert === undefined || d.alert === s.expects.alert) && d.replayIdentical;

export function judge(r: Routes, scripts: Record<string, Script>, plays: Plays, slack: Slack[], o: { caseTrailEq?: number; getaway?: Getaway | Getaway[]; facts?: Facts; scenarios?: Scenarios } = {}): Report {
  const eq = o.caseTrailEq ?? 0, totalTrail = (d: Digest) => totalTrailOf(d, eq);
  const live = r.scripts.filter(s => !s.pending), pending = r.scripts.filter(s => s.pending).map(s => s.id);
  const rows: Row[] = live.map(s => { const d = plays[s.id]!; return { id: s.id, kind: s.kind, route: s.route, expects: want(s), got: gotA(s, d), ok: met(s, d), at: clock(d.tick), trail: d.trail, alert: d.alert, shots: d.shots, fightShots: d.gunfire - d.shots, fight: d.fight ?? '', cash: d.cash, replay: d.replayIdentical }; });
  const C: Condition[] = [];

  const off = rows.filter(x => !x.ok);
  C.push({ id: 'MC-0', rule: 'Every script plays as the manifest expects and replays to identical checkpoints', ok: !off.length, value: `${rows.length - off.length}/${rows.length}`, detail: off.map(x => `${x.id}: expected ${x.expects}, got ${x.got}${x.replay ? '' : ' (replay differs)'}`).join('; ') || `${pending.length} pending: ${pending.join(', ') || 'none'}` });

  const quiet = live.filter(s => s.kind === 'quiet'), wins = (s: RouteScript) => quietWin(plays[s.id]);
  const quietRoutes = [...new Set(quiet.filter(wins).map(s => s.route!))].sort();
  C.push({ id: 'MC-1', rule: '≥ 2 quiet routes win (no shot before the final fight), counted by route', ok: quietRoutes.length >= 2, value: `${quietRoutes.length} (${quietRoutes.join(', ')})`, detail: quiet.map(s => `${s.id} (route ${s.route}) ${wins(s) ? `wins at ${clock(plays[s.id]!.tick)}, ${plays[s.id]!.trail} T` : got(plays[s.id]!)}`).join('; ') });

  const loud = live.find(s => s.kind === 'loud' && s.primary), ld = loud && plays[loud.id];
  // every quiet win, variants too (Motive, briefs/011/motive-fights.md: Pressure on an Idealist must not cost as much as going loud)
  const primaries = quiet.filter(s => s.primary), quietAll = quiet.filter(wins), maxQuiet = Math.max(...quietAll.map(s => totalTrail(plays[s.id]!)));
  const dear = quietAll.filter(s => ld && totalTrail(plays[s.id]!) >= totalTrail(ld));
  const T = (id: string) => { const d = plays[id]!; return [d.trail, ...(d.campaignTrail ? [d.campaignTrail] : []), ...(d.openCases && eq ? [`${d.openCases * eq} (case)`] : [])].join('+'); };
  C.push({
    id: 'MC-2', rule: 'The loud route exists, wins, and costs more T than every quiet win (variants too)', ok: !!ld && ld.outcome === 'success' && ld.shots > 0 && quietAll.length > 0 && totalTrail(ld) > maxQuiet,
    value: ld ? `loud ${T(loud!.id)} T vs quiet ${quietAll.map(s => `${s.id} ${T(s.id)}`).join(', ')} (mission+campaign+cases at ${eq} T)` : 'no loud script',
    detail: [...live.filter(s => s.kind === 'loud').map(s => `${s.id}: ${got(plays[s.id]!)}, ${plays[s.id]!.trail} T, ${plays[s.id]!.shots} shots before the fight, A ${plays[s.id]!.alert}`),
      ...dear.map(s => `${s.id} (quiet) costs ${totalTrail(plays[s.id]!)} T, not less than the loud route's ${totalTrail(ld!)}`)].join('; '),
  });

  // brief 039 (Keel's spec; Sky 2026-10-27): a line that names an engine scenario is reached, too, when `suborned-sim check <scenario>` prints ": ok"
  const reached = (f: Routes['fails'][number]) => [...live.filter(s => s.kind === 'fail' && s.reaches === f.id && plays[s.id]?.outcome === 'failure' && f.codes.includes(plays[s.id]!.fail)).map(s => s.id),
    ...(f.scenario && scenarioOk(o.scenarios, f.scenario) ? [`scenario ${f.scenario}`] : [])];
  // brief 067 (§09: a fail that needs a watcher the site doesn't place is not counted): a line with `needs` counts only where the site's fact
  // holds; an engine that states no facts (before 0.34.0) counts it, as before
  const FACT = { 'patrol-watches-entry': 'patrolWatchesEntry' } as const, NOT_ON = { 'patrol-watches-entry': 'no patrol watches an entry' } as const;
  const onSite = (f: Routes['fails'][number]) => !f.needs || o.facts?.[FACT[f.needs]] !== false;
  const counted = r.fails.filter(onSite), missing = counted.filter(f => !reached(f).length);
  C.push({ id: 'MC-3', rule: 'Every §07 fail is reached by a played script', ok: !missing.length, value: `${counted.length - missing.length}/${counted.length}${counted.length < r.fails.length ? ` (${r.fails.length - counted.length} not on this site)` : ''}`,
    detail: [...r.fails.map(f => onSite(f) ? `${f.gdd}: ${reached(f).join(', ') || `NOT REACHED${f.scenario ? ` (${o.scenarios?.[f.scenario] ?? `scenario ${f.scenario} not run`})` : ''}`}` : `${f.gdd}: not on this site (${NOT_ON[f.needs!]})`), ...(pending.length ? [`pending (not played): ${pending.join(', ')}`] : [])].join('; ') });

  const contacts: ContactRow[] = r.contacts.map(c => {
    const without = quiet.filter(s => quietWin(plays[`${s.id}~no-${c.role}`] ?? plays[s.id]));
    const routesWithout = [...new Set(without.map(s => s.route!))].sort(), all = [...new Set(quiet.filter(wins).map(s => s.route!))];
    const needs = all.filter(rt => !routesWithout.includes(rt)).sort();
    return { role: c.role, who: c.who, quietWinsWithout: without.map(s => s.id), routesWithout, needs, named: routesWithout.length < 2 };
  });
  const essential = contacts.filter(c => !c.routesWithout.length);
  C.push({
    id: 'MC-4', rule: 'No single contact is essential: without each, a quiet route still wins (a contact that leaves < 2 quiet routes is named)', ok: !essential.length,
    value: contacts.map(c => `without ${c.role}: ${c.routesWithout.join(', ') || 'none'}`).join('; '),
    detail: contacts.map(c => `${c.role} (${c.who}): ${c.needs.length ? `route ${c.needs.join(', ')} depends on it` : 'no route depends on it'}${c.named ? `; NAMED: only ${c.routesWithout.length} quiet route${c.routesWithout.length === 1 ? '' : 's'} without it` : ''}`).join('; '),
  });

  const close = ticksOf(r.closing.at);
  const informed: InformedRow[] = primaries.filter(wins).map(s => {
    const d = plays[s.id]!, cl = s.closes!, at = d.marks[cl.secured], sl = slack.find(x => x.script === s.id);
    return { script: s.id, route: s.route!, closes: `${cl.at} ${cl.what}`, secured: at === undefined ? `never (${cl.secured})` : clock(at), spareS: at === undefined ? -1 : Math.floor((ticksOf(cl.at) - at) / 10), extracted: clock(d.tick), closeSpareS: Math.floor((close - d.tick) / 10), slackS: sl?.maxS ?? 0 };
  });
  const short = informed.filter(x => x.spareS < r.informedSpareS), spare = `${Math.floor(r.informedSpareS / 60)}:${String(r.informedSpareS % 60).padStart(2, '0')}`;
  C.push({
    id: 'MC-5', rule: `The fully informed player secures Vale ≥ ${spare} before each primary quiet route's closing event`, ok: informed.length >= 1 && informed.length === primaries.length && !short.length,
    value: informed.map(x => `route ${x.route} ${x.spareS >= 0 ? clock(x.spareS * 10) : 'never'} early`).join(', ') || 'no primary quiet route wins',
    detail: informed.map(x => `route ${x.route} (${x.script}): Vale secured at ${x.secured}, ${x.spareS >= 0 ? clock(x.spareS * 10) : '-'} before ${x.closes}${x.spareS < r.informedSpareS ? ' (UNDER the spare)' : ''}; at the getaway car ${x.extracted}, ${clock(x.closeSpareS * 10)} before the mission's ${r.closing.at} close; every planned step can slip ${x.slackS} s and still win`).join('; '),
  });
  // MC-6 (T2, §13; Motive's +30 s Enter at Alert 2): informed quiet play is not slower than the fastest loud win
  const quietWins = quiet.filter(wins).map(s => ({ id: s.id, tick: plays[s.id]!.tick })).sort((a, b) => a.tick - b.tick);
  // a loud win that leaves an operative captured is reported, but T2 races the fastest loud win that brings everyone home (Motive)
  const loudAll = live.filter(s => s.kind === 'loud' && plays[s.id]?.outcome === 'success').map(s => ({ id: s.id, tick: plays[s.id]!.tick, captured: 'captured' in plays[s.id]!.marks })).sort((a, b) => a.tick - b.tick);
  const loudWins = loudAll.filter(x => !x.captured);
  const fq = quietWins[0], fl = loudWins[0], margin = fq && fl ? fl.tick - fq.tick : undefined;
  C.push({
    id: 'MC-6', rule: 'The fastest informed quiet win is not slower than the fastest loud win that leaves nobody captured (T2)', ok: margin !== undefined && margin >= 0,
    value: fq && fl ? `${fq.id} ${clock(fq.tick)} vs ${fl.id} ${clock(fl.tick)}: ${margin! >= 0 ? `${(margin! / 10).toFixed(1)} s faster` : `${(-margin! / 10).toFixed(1)} s SLOWER`}` : 'no quiet or no loud win',
    detail: [...quietWins.map(x => `${x.id} (quiet) ${clock(x.tick)}`), ...loudAll.map(x => `${x.id} (loud) ${clock(x.tick)}${x.captured ? ', leaves an operative captured: not raced' : ''}`)].join('; '),
  });
  // MC-7, MC-8, MC-9 (brief 013 D): takedowns
  const careless = live.filter(s => s.kind === 'careless' && (s.careless ?? 'violence') === 'violence');
  const worse = careless.map(s => {
    const d = plays[s.id]!, tw = plays[s.twin!], dear = !!tw && totalTrail(d) > totalTrail(tw);
    const ok = d.outcome === 'failure' || (d.outcome === 'success' && (d.alert >= 2 || (!!tw && d.alert > tw.alert)) && dear) || (d.outcome !== 'success' && d.alert >= 2);
    return { s, d, ok, why: d.outcome === 'failure' ? `fails: ${d.fail}` : `${got(d)} at Alert ${d.alert}, ${totalTrail(d)} T against ${s.twin}'s ${tw ? totalTrail(tw) : '?'} T at Alert ${tw?.alert ?? '?'}` };
  });
  C.push({
    id: 'MC-7', rule: 'Careless violence is worse: a takedown seen, found or reported fails, or wins only at Alert ≥ 2 (or above its careful twin\'s) and dearer in total Trail than its careful twin', ok: careless.length > 0 && worse.every(x => x.ok),
    value: `${worse.filter(x => x.ok).length}/${careless.length}`, detail: worse.map(x => `${x.s.id}: ${x.why}${x.ok ? '' : ' (NOT WORSE)'}`).join('; ') || 'no careless script',
  });
  const takes = (s: RouteScript) => scripts[s.id]?.orders.some(o => o.do === 'takedown') ?? false, turns = (s: RouteScript) => scripts[s.id]?.orders.some(o => o.do === 'turn') ?? false;
  const td = quiet.filter(s => takes(s) && wins(s) && plays[s.id]!.alert <= 1), tdAll = quiet.filter(takes);
  C.push({
    id: 'MC-8', rule: 'Takedown routes are viable with a real time cost: ≥ 1 quiet route that takes someone down wins at Alert ≤ 1, each spending Enforcer time on it', ok: td.length > 0 && td.every(s => (plays[s.id]!.takedownS ?? 0) > 0),
    value: td.map(s => `${s.id} ${clock(plays[s.id]!.tick)}, ${plays[s.id]!.takedownS ?? 0} s of takedown`).join(', ') || 'none',
    detail: tdAll.map(s => { const d = plays[s.id]!; return `${s.id} (route ${s.route}): ${got(d)} at ${clock(d.tick)}, ${T(s.id)} T, A ${d.alert}, $${(d.cash).toLocaleString('en-US')} left, takedown work ${d.takedownS ?? 0} s${d.openCases ? `, ${d.openCases} open case` : ''}`; }).join('; '),
  });
  const turnWins = quiet.filter(s => turns(s) && wins(s)), dominated = td.filter(s => turnWins.length > 0 && turnWins.every(t => plays[t.id]!.tick >= plays[s.id]!.tick && totalTrail(plays[t.id]!) >= totalTrail(plays[s.id]!)));
  C.push({
    id: 'MC-9', rule: 'Turning people stays competitive: no takedown win is at once as fast and as cheap in total Trail as every turning win', ok: turnWins.length > 0 && !dominated.length,
    value: dominated.length ? `dominant: ${dominated.map(s => s.id).join(', ')}` : `${td.length} takedown win${td.length === 1 ? '' : 's'}, none dominant over ${turnWins.length} turning wins`,
    detail: td.map(s => { const d = plays[s.id]!, f = turnWins.filter(t => plays[t.id]!.tick < d.tick).map(t => t.id), c = turnWins.filter(t => totalTrail(plays[t.id]!) < totalTrail(d)).map(t => t.id); return `${s.id} (${clock(d.tick)}, ${totalTrail(d)} T): faster ${f.join(', ') || 'none'}; cheaper ${c.join(', ') || 'none'}`; }).join('; '),
  });
  // MC-10 (brief 018 §5): quiet beats loud on Trail, Alert, Standing and the seal, against every loud win
  const loudWon = live.filter(s => s.kind === 'loud' && plays[s.id]?.outcome === 'success'), st = (d: Digest) => d.standing ?? 0;
  const sealed = (d: Digest) => d.fight === 'loud' || 'seal.starts' in d.marks;
  const beats = quietAll.map(q => {
    const d = plays[q.id]!, off: string[] = [];
    for (const l of loudWon) {
      const e = plays[l.id]!;
      if (totalTrail(d) >= totalTrail(e)) off.push(`Trail ${totalTrail(d)} not under ${l.id}'s ${totalTrail(e)}`);
      if (d.alert >= e.alert) off.push(`Alert ${d.alert} not under ${l.id}'s ${e.alert}`);
      if (st(d) < st(e)) off.push(`Standing ${st(d)} under ${l.id}'s ${st(e)}`);
    }
    if (sealed(d)) off.push('the seal clock runs');
    return { q, d, off: [...new Set(off)] };
  });
  const loudLine = loudWon.map(l => { const e = plays[l.id]!; return `${l.id} ${T(l.id)} T, A ${e.alert}, Standing ${st(e)}${sealed(e) ? ', seal clock' : ''}${e.left ? `, left: ${leftOf(e.left)}` : ''}`; });
  C.push({
    id: 'MC-10', rule: 'Quiet beats loud on every count: each quiet win costs less total Trail, stands at a lower Alert and no worse Standing than every loud win, with no seal clock', ok: loudWon.length > 0 && quietAll.length > 0 && beats.every(b => !b.off.length) && loudWon.every(l => sealed(plays[l.id]!)),
    value: quietAll.length && loudWon.length ? `quiet ${Math.min(...quietAll.map(q => totalTrail(plays[q.id]!)))}-${maxQuiet} T, A ≤ ${Math.max(...quietAll.map(q => plays[q.id]!.alert))}, Standing ≥ ${Math.min(...quietAll.map(q => st(plays[q.id]!)))}; loud ${Math.min(...loudWon.map(l => totalTrail(plays[l.id]!)))}-${Math.max(...loudWon.map(l => totalTrail(plays[l.id]!)))} T, A ≥ ${Math.min(...loudWon.map(l => plays[l.id]!.alert))}, Standing ≤ ${Math.max(...loudWon.map(l => st(plays[l.id]!)))}` : 'no quiet or no loud win',
    detail: [...loudLine, ...beats.filter(b => b.off.length).map(b => `${b.q.id} NOT BETTER: ${b.off.join(', ')}`), ...loudWon.filter(l => !sealed(plays[l.id]!)).map(l => `${l.id}: no seal clock (NOT LOUD)`)].join('; '),
  });
  // MC-11 (brief 018 §1, §5; brief 059 K6: every stand; brief 067 M067-SQ1: no hold that did not come for the car): no stop holds the car
  const stands = o.getaway === undefined ? [] : Array.isArray(o.getaway) ? o.getaway : [o.getaway];
  if (stands.length) {
    const posts = stands.flatMap(g => g.posts.map(x => ({ ...x, stand: g.id, m: Math.hypot(x.at[0] - g.car[0], x.at[1] - g.car[1]) / 100, holds: Math.hypot(x.at[0] - g.car[0], x.at[1] - g.car[1]) <= g.holdCm || inLane(g, x.at), lane: inLane(g, x.at) })));
    const refs = live.filter(s => (s.kind === 'quiet' || s.kind === 'loud') && plays[s.id]?.outcome === 'success' && plays[s.id]!.fight);
    const late = refs.filter(s => { const d = plays[s.id]!; return d.carFreeS === undefined || d.carFreeS < 0; });
    // every hold a played script logged: blocker, press and advance came for the car; an answer to fire never did; a walk to (or a stand at)
    // a post the mission places (a rival's ambush spot, a final-fight officer's police stand) did not; any other post (a response-unit
    // member's join point, a search unit at its stash) is not the mission's, and is listed, not counted
    const placed = (who: string) => MISSION_POST.test(who);
    const holds = live.flatMap(s => (plays[s.id]?.held ?? []).map(h => ({ s, h })));
    const bad = holds.filter(({ h }) => h.how === 'answer' || (h.how === 'post' && placed(h.who))), joined = holds.filter(({ h }) => h.how === 'post' && !placed(h.who));
    const rule = stands.length > 1 ? `every ambush spot and police stand of each of the ${stands.length} stands lies beyond fight-hold ${stands[0]!.holdCm / 100} m of its car and outside its exit lane` : `every ambush spot and police stand lies beyond fight-hold ${stands[0]!.holdCm / 100} m of the car and outside the exit lane`;
    C.push({
      id: 'MC-11', rule: `No stop holds the car: ${rule}, every reference win's car was free when it left, and no played script logs a hold that did not come for the car (car.held.answer, or car.held.post at a post the mission places)`, ok: posts.every(x => !x.holds) && !late.length && !bad.length,
      value: `${posts.filter(x => !x.holds).length}/${posts.length} posts clear (nearest ${Math.min(...posts.map(x => x.m)).toFixed(1)} m); ${refs.length - late.length}/${refs.length} reference wins free at Go; ${holds.length - bad.length - joined.length}/${holds.length - joined.length} holds by bodies that came for the car${joined.length ? ` (${joined.length} more at posts the mission does not place, not counted)` : ''}`,
      detail: [...posts.filter(x => x.holds).map(x => `${x.stand ? `${x.stand} ` : ''}${x.id} at ${x.m.toFixed(1)} m HOLDS the car${x.lane ? ' (in the exit lane)' : ''}`),
        ...refs.map(s => { const d = plays[s.id]!; return `${s.id}: ${d.carFreeS === undefined ? 'no blocker out logged (NOT FREE)' : `blocker out ${d.carFreeS.toFixed(1)} s before Go`}`; }),
        ...bad.map(({ s, h }) => `${s.id}: ${h.who} holds the car by ${h.how === 'answer' ? 'an answer to fire' : 'standing at or walking to his post'} at ${clock(h.tick)} (car.held.${h.how}, NOT FOR THE CAR)`),
        ...joined.map(({ s, h }) => `${s.id}: ${h.who} holds the car at ${clock(h.tick)} ${h.join ? `on its way to the response unit's join point ${h.join}` : 'at a post the mission does not place'} (not counted)`)].join('; '),
    });
  }
  // MC-12 (brief 031): loitering in a watcher's sight is reported, so the quiet routes stand out of sight
  const quietRep = live.filter(s => s.kind === 'quiet' && loiterReported(plays[s.id])), loiter = live.filter(s => s.kind === 'careless' && s.careless === 'loiter');
  const loiterRows = loiter.map(s => { const d = plays[s.id]!, tw = plays[s.twin!]; return { s, d, tw, ok: loiterReported(d) && !!tw && (d.alert > tw.alert || (d.outcome === 'failure' && tw.outcome === 'success')) }; });
  C.push({
    id: 'MC-12', rule: 'Loitering costs: no quiet route has a watcher report an agent loitering, and each careless-loiter twin is reported and fails or ends at a higher Alert than its careful twin', ok: !quietRep.length && loiter.length > 0 && loiterRows.every(x => x.ok),
    value: `${live.filter(s => s.kind === 'quiet').length - quietRep.length}/${live.filter(s => s.kind === 'quiet').length} quiet scripts unreported; ${loiterRows.filter(x => x.ok).length}/${loiter.length} loiter twins dearer`,
    detail: [...quietRep.map(s => `${s.id}: a loiterer reported at ${clock(plays[s.id]!.marks['attention.reporting']!)} (NOT QUIET)`),
      ...loiterRows.map(x => `${x.s.id}: ${loiterReported(x.d) ? `reported at ${clock(x.d.marks['attention.reporting']!)}` : 'never reported'}, ${got(x.d)} at A ${x.d.alert} against ${x.s.twin}'s ${x.tw ? `${got(x.tw)} at A ${x.tw.alert}` : '?'}${x.ok ? '' : ' (NOT DEARER)'}`)].join('; ') || 'no careless-loiter script',
  });
  // MC-13 (brief 059, Motive M059-5.3): every stand's loud-wait twin is held: the cell waits in the car and fights nobody, so it may never win;
  // with Vale aboard its Go is refused car-held (the car held at the Enforcer's Board), or it is lost before he gets Vale in
  if (stands.length) {
    const heldAt = (d: Digest) => (d.goRefused ?? []).find(g => g.code === 'car-held' && d.valeIn !== undefined && g.tick >= d.valeIn);
    const how = (d: Digest) => d.outcome === 'success' ? 'WINS' : heldAt(d) ? `Go refused car-held at ${clock(heldAt(d)!.tick)} with Vale aboard` : d.valeIn === undefined ? 'lost before Vale is aboard' : 'NOT HELD: Vale aboard and no Go refused';
    const rows13 = stands.map(g => {
      const twins = live.filter(s => s.holds === g.id), held = twins.filter(s => { const d = plays[s.id]; return !!d && d.fight === 'loud' && d.outcome !== 'success' && (!!heldAt(d) || d.valeIn === undefined); });
      return { g, twins, held };
    });
    C.push({
      id: 'MC-13', rule: 'Every stand\'s loud-wait twin is held: a loud run whose cell waits in the car while the Enforcer brings Vale out never wins; with Vale aboard its Go is refused car-held', ok: rows13.every(x => x.held.length > 0),
      value: `${rows13.filter(x => x.held.length).length}/${rows13.length} stands held`,
      detail: rows13.map(({ g, twins, held }) => `${g.id ?? 'the stand'}: ${twins.length ? twins.map(s => { const d = plays[s.id]!; return `${s.id} ${held.includes(s) ? 'held' : 'NOT HELD'} (${how(d)}: ${got(d)} at ${clock(d.tick)}, ${d.fight ?? 'no'} fight)`; }).join(', ') : 'NO loud-wait script'}`).join('; '),
    });
  }
  // MC-14 (brief 059, Motive M059-S1.2; Sky 2026-10-25): the fight always comes: every win the checker played Goes at or after blocker.out
  const winsAll = Object.entries(plays).filter(([, d]) => d.outcome === 'success'), early = winsAll.filter(([, d]) => d.carFreeS === undefined || d.carFreeS < 0);
  C.push({
    id: 'MC-14', rule: 'The fight always comes: every win Goes at or after blocker.out (variations too)', ok: !early.length,
    value: `${winsAll.length - early.length}/${winsAll.length} wins Go after blocker.out`,
    detail: early.slice(0, 12).map(([k, d]) => `${k}: ${d.carFreeS === undefined ? 'no blocker.out logged' : `Go ${(-d.carFreeS).toFixed(1)} s before blocker.out`} (UNFOUGHT)`).join('; ') + (early.length > 12 ? `; and ${early.length - 12} more` : '')
      || (winsAll.length ? `the closest Go to blocker.out: ${Math.min(...winsAll.map(([, d]) => d.carFreeS!)).toFixed(1)} s after it` : 'no win'),
  });
  // MC-15 (brief 059, Motive M059-S1.5): no reference route reaches crew-wiped
  const refsAll = live.filter(s => s.kind === 'quiet' || s.kind === 'loud'), wiped = refsAll.filter(s => 'rivals.wiped' in (plays[s.id]?.marks ?? {}));
  C.push({
    id: 'MC-15', rule: 'No reference route reaches crew-wiped (the rival crew destroyed, the fight over unfought)', ok: !wiped.length,
    value: `${refsAll.length - wiped.length}/${refsAll.length} reference scripts`, detail: wiped.map(s => `${s.id}: rivals.wiped at ${clock(plays[s.id]!.marks['rivals.wiped']!)}`).join('; ') || 'none',
  });
  return { ok: C.every(c => c.ok), conditions: C, scripts: rows, contacts, informed, slack, pending };
}

/** The report in the generation pipeline's terms (tools/gen/plugin.ts Assessment: ok, line, record, failures), so the mission
 *  objective (brief 003) can take this checker as its `assess` step when generated missions run. */
export function assessment(rep: Report, owner = 'mission-designer') {
  return {
    ok: rep.ok,
    line: rep.conditions.map(c => `${c.id} ${c.ok ? 'ok' : 'FAIL'}`).join(' · '),
    record: { conditions: rep.conditions },
    failures: rep.conditions.filter(c => !c.ok).map(c => ({ rule: c.id, owners: [owner], fault: `${c.rule}: ${c.value}`, fix: c.detail })),
  };
}
