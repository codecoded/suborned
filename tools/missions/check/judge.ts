// The mission checker's judgement (GDD §09 "mission checker: ships only if ≥ 2 quiet routes win · loud route exists and costs more T ·
// every fail condition reachable · no single contact essential · fully informed player finishes ≥ 1:00 before closing event";
// brief 011 B). Pure: it reads the route manifest, the scripts and what each play reported, and says which conditions hold and why.
// MC-0 every script plays as its manifest expects, and replays to identical checkpoints.
// MC-1 ≥ 2 quiet routes win: routes, not scripts (a variant of route A is still route A).
// MC-2 the loud route exists, wins, and costs more Trail than every primary quiet route (variants are reported beside it).
// MC-3 every §07 fail is reached by a script that is played (a pending script reaches nothing yet, and is listed).
// MC-4 no single contact is essential: without each contact a quiet route still wins. The routes that need the contact are named,
//      and a contact that leaves fewer than 2 quiet routes is named as the one those routes depend on (brief 011 B).
// MC-6 T2 (§13): the fastest informed quiet win is not slower than the fastest loud win; the margin is reported.
// MC-5 the fully informed player (each primary quiet route as scripted) secures Vale ≥ informedSpareS before that route's closing
//      event (route A: inside before Vale exits at 5:50; route B: Vale taken before the 8:00 close). Each route's extraction time
//      against the mission's close, and how late each of its steps can be before it stops winning, are reported beside it.
import type { Routes, RouteScript } from '../../../contracts/missions/routes.schema';
import type { Play, Script } from './engine';

/** What the record keeps of a play: enough to judge and to replay against. */
export type Digest = Pick<Play, 'outcome' | 'fail' | 'tick' | 'cash' | 'trail' | 'alert' | 'gunfire' | 'startHash' | 'finalHash' | 'replayIdentical'> & {
  rejected: number; scriptSha: string;
  /** the first tick of each event-log code (enter.in, take.done, …): when the route secured Vale */
  marks: Record<string, number>;
  /** the Enforcer's shots before the final fight (§07: the fight's branch is loud if any shot was fired before it) */
  shots: number;
  /** the final fight's branch, if one began */
  fight?: 'safe' | 'loud';
};
/** `marks`: keep the event marks (a base script's; a variation needs only its outcome and hash). */
export const digest = (p: Play, scriptSha: string, keepMarks = true): Digest => {
  const marks: Record<string, number> = {};
  if (keepMarks) for (const l of p.log) if (!(l.code in marks) && !l.code.startsWith('order.')) marks[l.code] = l.tick;
  const fightAt = p.log.find(l => l.code === 'fight.safe' || l.code === 'fight.loud');
  const shots = p.log.filter(l => l.code.startsWith('fire.') && l.code !== 'fire.hostiles' && (!fightAt || l.tick < fightAt.tick)).length;
  return {
    outcome: p.outcome, fail: p.fail, tick: p.tick, cash: p.cash, trail: p.trail, alert: p.alert, gunfire: p.gunfire, startHash: p.startHash, finalHash: p.finalHash, replayIdentical: p.replayIdentical,
    rejected: p.orders.filter(o => !o.ok).length, scriptSha, marks, shots, ...(fightAt ? { fight: fightAt.code === 'fight.safe' ? 'safe' as const : 'loud' as const } : {}),
  };
};
/** Every play the checker made, by name: `<script>`, `<script>~no-<role>`, `<script>~late@<tick>+<s>s`. */
export type Plays = Record<string, Digest>;
/** How late each planned step of a primary route can be (its steps up to the one that secures Vale; the final fight after it is
 *  fought, not planned): the most seconds, contiguous from 0, every later order can slip and still win. */
export type Slack = { script: string; steps: { tick: number; orders: string; slackS: number; breaksAs?: string }[]; maxS: number };

export type Condition = { id: string; rule: string; ok: boolean; value: string; detail: string };
export type Report = { ok: boolean; conditions: Condition[]; scripts: Row[]; contacts: ContactRow[]; informed: InformedRow[]; slack: Slack[]; pending: string[] };
type Row = { id: string; kind: string; route?: string; expects: string; got: string; ok: boolean; at: string; trail: number; alert: number; shots: number; fightShots: number; fight: string; cash: number; replay: boolean };
type ContactRow = { role: string; who: string; quietWinsWithout: string[]; routesWithout: string[]; needs: string[]; named: boolean };
type InformedRow = { script: string; route: string; closes: string; secured: string; spareS: number; extracted: string; closeSpareS: number; slackS: number };

/** A quiet win: Vale at the getaway car with no shot before the final fight (the fight's own shots don't make a route loud, §07). */
export const quietWin = (d?: Digest) => !!d && d.outcome === 'success' && d.shots === 0;
export const clock = (tick: number) => { const s = Math.floor(tick / 10); return `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}${tick % 10 ? `.${tick % 10}` : ''}`; };
const ticksOf = (at: string) => { const [m, s] = at.split(':').map(Number); return (m! * 60 + s!) * 10; };
const got = (d: Digest) => (d.outcome === 'failure' ? `failure ${d.fail}` : d.outcome);
const want = (s: RouteScript) => (s.expects.fail ? `${s.expects.outcome} ${s.expects.fail}` : s.expects.outcome);
const met = (s: RouteScript, d?: Digest) => !!d && d.outcome === s.expects.outcome && (!s.expects.fail || d.fail === s.expects.fail) && d.replayIdentical;

export function judge(r: Routes, scripts: Record<string, Script>, plays: Plays, slack: Slack[]): Report {
  const live = r.scripts.filter(s => !s.pending), pending = r.scripts.filter(s => s.pending).map(s => s.id);
  const rows: Row[] = live.map(s => { const d = plays[s.id]!; return { id: s.id, kind: s.kind, route: s.route, expects: want(s), got: got(d), ok: met(s, d), at: clock(d.tick), trail: d.trail, alert: d.alert, shots: d.shots, fightShots: d.gunfire - d.shots, fight: d.fight ?? '', cash: d.cash, replay: d.replayIdentical }; });
  const C: Condition[] = [];

  const off = rows.filter(x => !x.ok);
  C.push({ id: 'MC-0', rule: 'Every script plays as the manifest expects and replays to identical checkpoints', ok: !off.length, value: `${rows.length - off.length}/${rows.length}`, detail: off.map(x => `${x.id}: expected ${x.expects}, got ${x.got}${x.replay ? '' : ' (replay differs)'}`).join('; ') || `${pending.length} pending: ${pending.join(', ') || 'none'}` });

  const quiet = live.filter(s => s.kind === 'quiet'), wins = (s: RouteScript) => quietWin(plays[s.id]);
  const quietRoutes = [...new Set(quiet.filter(wins).map(s => s.route!))].sort();
  C.push({ id: 'MC-1', rule: '≥ 2 quiet routes win (no shot before the final fight), counted by route', ok: quietRoutes.length >= 2, value: `${quietRoutes.length} (${quietRoutes.join(', ')})`, detail: quiet.map(s => `${s.id} (route ${s.route}) ${wins(s) ? `wins at ${clock(plays[s.id]!.tick)}, ${plays[s.id]!.trail} T` : got(plays[s.id]!)}`).join('; ') });

  const loud = live.find(s => s.kind === 'loud' && s.primary), ld = loud && plays[loud.id];
  const primaries = quiet.filter(s => s.primary), maxPrimary = Math.max(...primaries.map(s => plays[s.id]!.trail));
  const variants = quiet.filter(s => !s.primary && wins(s) && ld && plays[s.id]!.trail >= ld.trail);
  C.push({
    id: 'MC-2', rule: 'The loud route exists, wins, and costs more T than every primary quiet route', ok: !!ld && ld.outcome === 'success' && ld.shots > 0 && ld.trail > maxPrimary,
    value: ld ? `loud ${ld.trail} T vs quiet ${primaries.map(s => `${s.route} ${plays[s.id]!.trail}`).join(', ')}` : 'no loud script',
    detail: [...live.filter(s => s.kind === 'loud').map(s => `${s.id}: ${got(plays[s.id]!)}, ${plays[s.id]!.trail} T, ${plays[s.id]!.shots} shots before the fight, A ${plays[s.id]!.alert}`),
      ...variants.map(s => `note: quiet variant ${s.id} costs ${plays[s.id]!.trail} T, not less than the loud route`)].join('; '),
  });

  const reached = (f: Routes['fails'][number]) => live.filter(s => s.kind === 'fail' && s.reaches === f.id && plays[s.id]?.outcome === 'failure' && f.codes.includes(plays[s.id]!.fail)).map(s => s.id);
  const missing = r.fails.filter(f => !reached(f).length);
  C.push({ id: 'MC-3', rule: 'Every §07 fail is reached by a played script', ok: !missing.length, value: `${r.fails.length - missing.length}/${r.fails.length}`, detail: [...r.fails.map(f => `${f.gdd}: ${reached(f).join(', ') || 'NOT REACHED'}`), ...(pending.length ? [`pending (not played): ${pending.join(', ')}`] : [])].join('; ') });

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
  const loudWins = live.filter(s => s.kind === 'loud' && plays[s.id]?.outcome === 'success').map(s => ({ id: s.id, tick: plays[s.id]!.tick })).sort((a, b) => a.tick - b.tick);
  const fq = quietWins[0], fl = loudWins[0], margin = fq && fl ? fl.tick - fq.tick : undefined;
  C.push({
    id: 'MC-6', rule: 'The fastest informed quiet win is not slower than the fastest loud win (T2)', ok: margin !== undefined && margin >= 0,
    value: fq && fl ? `${fq.id} ${clock(fq.tick)} vs ${fl.id} ${clock(fl.tick)}: ${margin! >= 0 ? `${(margin! / 10).toFixed(1)} s faster` : `${(-margin! / 10).toFixed(1)} s SLOWER`}` : 'no quiet or no loud win',
    detail: [...quietWins.map(x => `${x.id} (quiet) ${clock(x.tick)}`), ...loudWins.map(x => `${x.id} (loud) ${clock(x.tick)}`)].join('; '),
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
