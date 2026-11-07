import { pick, type Read } from './guide';
import { DEBRIEF_WORDS } from './short-guide';
import type { Field, Fill, Form, ShortBrief, ShortGuideVersion, ShortItem } from './short-types';

/** The debrief page (brief 099, concept A, the front page Sky picked) as families: Glyph's stand-ins (briefs/099/glyph/words.md) by the
 *  keys the page reads, with the data each one shows (Glyph's notes, "Where each number comes from") and the keys a true page also needs
 *  (every way a start-page row comes true, every Trail cause the engine records, a Next time for every fail the fail camera knows). */
const FILLS: Record<string, [string, string]> = {
  clock: ['a time on the mission clock, m:ss', '10:30'], op: ['an agent, by the name the book\'s cell page gives them, beside their face', 'Arruns'],
  who: ['a person or thing, as the player knows them: Inez, Popovic, the guard, the driver, their lookout, a camera by its place, the patrol', 'the entrance camera'],
  n: ['a count', '12'], trail: ['the run\'s Trail', '27'], alert: ['the Alert reached', '3'], missions: ['a number of missions', '3'], guards: ['extra guards', '2'],
  cameras: ['extra cameras', '2'], place: ['a place, as the city names it', 'the service lane'], by: ['who saw it: a police officer, the patrol, a robot', 'a police officer'],
  list: ['the biggest Trail shorts, joined', 'the pay, the loop and her call'],
};
const LIMIT: Record<Form, [number, number]> = { label: [3, 20], note: [6, 40], line: [16, 80], title: [4, 32], short: [6, 40], plate: [6, 36], speech: [12, 80] };
/** A field; `wide` is a fill's widest where this slot's is narrower than the general one (a turned beat names Inez or the driver, never a camera). */
const f = (id: string, form: Form, fills: string[] = [], limit?: [number, number], wide: Record<string, string> = {}): Field => ({ id, form, words: (limit ?? LIMIT[form])[0], chars: (limit ?? LIMIT[form])[1], count: 1,
  fills: fills.map(x => ({ id: x, means: FILLS[x]![0], widest: wide[x] ?? FILLS[x]![1] }) as Fill), keep: [], patterns: [] });
const PERSON = { who: 'their lookout' }, CAMERA = { who: 'the entrance camera' };
const item = (key: string, where: string, tells: string, fields: Field[], current: Record<string, string>): ShortItem =>
  ({ key, where, tells, speaker: null, fields, current: Object.fromEntries(Object.entries(current).filter(([, v]) => v).map(([k, v]) => [k, [v]])), stays: 'The page reads it (brief 099, concept A).' });

/** Glyph's stand-ins by key, read from words.md ("- `key`: words"). */
export function standIns(read: Read): Record<string, string> {
  const out: Record<string, string> = {};
  for (const m of read(DEBRIEF_WORDS).matchAll(/^- `([^`]+)`: (.+)$/gm)) out[m[1]!] = m[2]!.trim();
  if (!out['caps.win'] || !out['next.rivals-reach-vale.lookout']) throw new Error('Glyph\'s words.md no longer has the stand-ins the page reads');
  return out;
}

export function debriefBriefs(read: Read, text: any, rulebook: string, v: ShortGuideVersion): ShortBrief[] {
  const G = standIns(read), rb = (id: string) => Number(pick(rulebook, new RegExp(`"${id}": \\{ ?"v": (\\d+)`), `rulebook ${id}`)[0]);
  const [fire, hit, down] = [rb('gunfire-trail'), rb('hit-trail'), rb('down-trail')];
  const rows = Object.fromEntries((text.book.job.needs.ways['front-door'] as { id: string; words: string }[]).concat(text.book.job.needs.ways.pickup).map(r => [r.id, r.words]));
  const brief = (id: string, title: string, context: string, items: ShortItem[]): ShortBrief => ({ id, kind: 'debrief', title, context, items });
  const ctx = 'Concept A, the front page: the outcome and its line at the top, the way in the player took with the start page\'s own rows answered, five numbers each with a one-line reason, what the city does now, and the run along the foot. Glyph\'s three runs: the quiet win (front door, away at 4:12), the lookout fail (5:53) and the loud win (Trail +27).';
  const row = (k: string) => `the start page's row "${rows[k]}"`;
  const HOW: [string, string, string][] = [
    ['called', 'guard-away', 'Inez called the guard over (call.waitress)'], ['knocked', 'guard-away', 'the guard was knocked out (takedown)'], ['shot', 'guard-away', 'the guard was shot down (guard.down)'],
    ['looped', 'door-camera', 'the Tech looped the entrance camera (loop.on)'], ['cut', 'door-camera', 'the Tech cut the entrance camera for good (camera.cut)'], ['shot-out', 'door-camera', 'the entrance camera was shot out (fire.camera-dark)'],
    ['in', 'inside', 'the Enforcer went in at the door (enter.in)'], ['paid', 'driver', 'the Fixer paid the driver (turn.pay.turned)'], ['helped', 'driver', 'the Fixer helped the driver (turn.help.turned)'],
    ['pressed', 'driver', 'the Fixer leaned on the driver (turn.pressure.turned)'], ['asked', 'driver', 'the Fixer asked the driver outright (turn.ask.turned)'],
    ['taped', 'pickup-camera', 'the row failed: the pickup camera taped the Pay at the car (camera.recorded)'], ['never', '', 'the row never came true'],
  ];
  const WIDE: Record<string, Record<string, string>> = { ask: PERSON, pay: PERSON, help: PERSON, pressure: PERSON, call: { who: 'Inez' }, recorded: CAMERA, loop: CAMERA, disable: CAMERA,
    mislead: PERSON, shot: { who: 'three rivals' }, knockout: PERSON, kill: PERSON, seen: { who: 'a kiosk attendant' }, warned: { who: 'the patrol' } };
  const TRAIL: [string, string, string[], string, string][] = [
    ['ask', 'turn.ask:<role>: the Fixer asked a person outright (+1)', ['op', 'who'], '', ''], ['pay', 'turn.pay:<role> or pay:<rival>: the Fixer paid a person, or one of the rival crew (+2)', ['op', 'who'], G['trail.turn.pay']!, G['short.turn.pay']!],
    ['help', 'turn.help:<role>: the Fixer helped a person (+1)', ['op', 'who'], G['trail.turn.help']!, G['short.turn.help']!], ['pressure', 'turn.pressure:<role>: the Fixer leaned on a person (+3)', ['op', 'who'], '', ''],
    ['call', 'call:<role>: a turned contact did their part, Inez calling the guard over (+1)', ['who'], G['trail.call']!, G['short.call']!],
    ['recorded', 'a camera taped the Fixer paying or threatening someone (+1, camera.record-lands)', ['who', 'op'], G['trail.recorded']!, G['short.recorded']!],
    ['loop', 'loop: the Tech looped a camera (+1)', ['op', 'who'], G['trail.loop']!, G['short.loop']!], ['disable', 'disable:<camera>: the Tech cut a camera for good (+1)', ['op', 'who'], '', ''],
    ['reroute', 'reroute: the Tech sent the patrol off its sweep (+1)', ['op'], G['trail.reroute']!, G['short.reroute']!], ['mislead', 'mislead:<id>: the Scout misled one of the rival crew (+1)', ['op', 'who'], '', ''],
    ['shot', `gunfire, hit and down on one target, grouped: +${fire} for firing on them, +${hit} if hit, +${down} if they go down; who is the person, or "three rivals", "the police"`, ['op', 'who'], G['trail.shot']!, ''],
    ['knockout', 'takedown:knockout:<victim>: the Enforcer knocked someone out', ['op', 'who'], '', ''], ['kill', 'takedown:kill or takedown:shot:<victim>: an agent killed someone', ['op', 'who'], '', ''],
    ['seen', 'witnessed:<incident>: someone saw a takedown, a shooting, a gun out or a threat (+1 each)', ['who'], '', ''], ['warned', 'warning-heeded:<watcher>: a watcher told an agent to put the gun away, and they did', ['who', 'op'], '', ''],
    ['car-seen', 'car-seen:<by>: someone saw our getaway car leave', ['by'], G['trail.car-seen']!, G['short.car-seen']!], ['lent-seen', 'lent-seen:<by>: someone saw Popovic\'s car leave with him, lent', ['by'], '', ''],
    ['car-lost', 'car-lost: our car was left behind, and it can be traced', [], '', ''], ['take-unturned', 'take:unturned-driver: Popovic was taken at his car with the driver not turned, who saw it', [], '', ''],
  ];
  const BEATS: [string, string, string[]][] = [
    ['watched', 'scout.watch.watched: the Scout\'s first Watch landed', []], ['listened', 'scout.listen.heard: the Scout\'s first Listen landed', []], ['turned', 'turn.*.turned: someone was turned', ['who']],
    ['loop', 'loop.on: a camera was looped', []], ['reroute', 'patrol.rerouted: the patrol was sent off', []], ['call', 'call.waitress: Inez drew the guard off the door', []],
    ['in', 'enter.in: the Enforcer went in', []], ['out', 'enter.out: the Enforcer came out with Popovic', []], ['rivals', 'rivals.coming: the rival car turned in (2:35)', []],
    ['lookout', 'rivals.lookout-posted: their lookout took his post', []], ['alert', 'attention.landed: the Alert rose, to {alert}', ['alert']], ['taped', 'camera.record-lands: a camera\'s tape of a Pay reached the desk', []],
    ['vale-out', 'vale.exits: Popovic walked out of the Office (5:30)', []], ['fight', 'fight.judged, the final fight', []], ['shots', 'fight.judged, an earlier exchange of shots', []],
    ['report', 'report.landed: a report reached the desk (the row\'s hover gives its why)', []], ['car-seen', 'car.seen: someone saw our car leave', []],
    ['away', 'success: the car left with Popovic', []], ['lost', 'fail.*: the end of a failed run, when no shorter word fits (less.end.fail says the fail)', []],
  ];
  const tips = text.less.failcam.tip as Record<string, Record<string, string>>, caps = text.less.failcam.caption as Record<string, Record<string, string>>;
  const cause = text.debrief.cause as Record<string, Record<string, { head: string; line: string }>>;
  const fails = Object.entries(tips).filter(([k]) => k !== 'engine-error').flatMap(([code, by]) => Object.keys(by).map(why => {
    const end = cause[code]?.[why] ?? Object.values(cause[code] ?? {})[0];
    return item(`next.by.${code}.${why}`, 'On a fail: the Next time line in the orange box, and the reason under the Time number',
      `The fail "${code}", why "${why}". The fail camera's caption: "${caps[code]?.[why] ?? ''}"; its tip: "${by[why]}". The end card: ${end ? `"${end.head}: ${end.line}"` : text.briefing.fails[code]}. ` +
      'The Next time line says what to deal with and why, never the order to give; the time reason says what the clock ran out against, or what happened at that moment, in a few words (the Time number above it shows when).',
      [f('line', 'line'), f('time', 'note')], { line: code === 'rivals-reach-vale' && why === 'lookout' ? G['next.rivals-reach-vale.lookout']! : '', time: code === 'rivals-reach-vale' && why === 'lookout' ? G['why.time.fail']! : '' });
  }));
  return [
    brief('debrief/head', 'The page\'s head, the way in, how each row came true, and the ways on', ctx, [
      item('head.win', 'The small caps over the headline on a win (the host sets them in capitals)', 'The mission\'s name and the clock when the car left with Popovic ({clock}).', [f('label', 'label', ['clock'], [6, 40])], { label: 'The Handover · done at {clock}' }),
      item('head.fail', 'The small caps over the headline on a fail', 'The mission\'s name and the clock when the run was lost ({clock}).', [f('label', 'label', ['clock'], [6, 40])], { label: 'The Handover · lost at {clock}' }),
      item('way.yours', 'Beside the way\'s name ("Front door", "Pickup", from less.briefing.ways) over the start page\'s rows', 'Marks the way in this run took: the pickup if the driver was turned, else the front door.', [f('label', 'note', [], [3, 16])], { label: G['way.yours']! }),
      ...HOW.map(([k, r, what]) => item(`how.${k}`, `A note right of ${r ? row(r) : 'any start-page row'}, before the time it came true ("· 2:31")`, `How the row came true: ${what}.`, [f('note', 'note', [], [4, 24])], { note: G[`how.${k}`] ?? '' })),
      item('next.head', 'The head of the orange box on a fail', 'Over the line that says what would have saved the run, and the cell\'s ways to do it.', [f('label', 'label', [], [2, 16])], { label: G['next.head']! }),
      ...[['again', 'Try again (opens the choice of a new start or the same one)'], ['new', 'A new start: a new seed, so a different game'], ['same', 'The same start: the run\'s seed, shown after it ("· 3")'],
        ['replay', 'Watch this run again from the start, as it was played (brief 080: a saved run replays exactly); the word "replay" is reserved for the engine\'s verdict (text.test.ts)'], ['start', 'Back to the start page, the book\'s first page']]
        .map(([k, what]) => item(`ways.${k}`, 'A button at the foot of the page', what!, [f('label', 'label', [], [3, 20])], { label: G[`way.${k}`] ?? '' })),
    ]),
    brief('debrief/numbers', 'The five numbers and the reason under each', ctx, [
      ...[['time', 'when the run ended (the play clock)'], ['cash', 'the money spent of the $100,000 purse'], ['trail', 'the Trail added (what investigators can trace)'], ['alert', 'the highest Alert reached'], ['standing', 'the district\'s Standing after the run, as its band and number']]
        .map(([k, what]) => item(`numbers.${k}`, 'A number\'s label, beside its icon; a › opens its explainer', `The label of the number that shows ${what}. Trail, Alert and Standing are the game's own names and stay.`, [f('label', 'label', [], [1, 10])], { label: G[`stat.${k}`] ?? '' })),
      item('reasons.time.win', 'Under the Time number on a win', 'How much time was left against Popovic\'s own car leaving the pickup at 5:50 ({clock} is that margin, m:ss).', [f('note', 'note', ['clock'])], { note: G['why.time.win']!.replace('{left}', '{clock}') }),
      ...[['help', 'all of it went on a Help (Inez\'s debt, or the driver\'s need)'], ['pay', 'all of it went on a Pay'], ['both', 'a Help and a Pay'], ['none', 'nothing was spent']]
        .map(([k, what]) => item(`reasons.cash.${k}`, 'Under the Spent number', `What the money went on: ${what}.`, [f('note', 'note')], { note: G[`why.cash.${k}`] ?? '' })),
      item('reasons.trail.shooting', 'Under the Trail number when shooting made half of it or more', '{n} of the {trail} Trail came from shooting.', [f('note', 'note', ['n', 'trail'])], { note: G['why.trail.shooting']!.replace('{total}', '{trail}') }),
      item('reasons.trail.none', 'Under the Trail number at 0', 'Nothing the cell did could be traced: Trail 0.', [f('note', 'note')], {}),
      item('reasons.trail.list', 'Under the Trail number otherwise: the biggest shorts joined ("the pay, the loop and her call")', v >= 5
        ? 'What left the Trail: the host fills {list} with the biggest Trail shorts, joined ("the pay, the loop and her call"). Write the few words around {list}, so the reason names what left it.'
        : 'A list of what left Trail, from the Trail rows\' shorts ({list}); one word to join the last two.', [f('note', 'note', ['list'])], {}),
      item('reasons.alert.none', 'Under the Alert number at 0 (a rise shows its cause\'s short from the why block, "the pickup camera saw the Fixer hanging about")', 'Nobody raised the alarm all run.', [f('note', 'note')], { note: G['why.alert.0']! }),
    ]),
    brief('debrief/trail', 'The Trail explainer: what Trail is, the law, and a row for every cause', ctx, [
      item('trail.means', 'The fold\'s first line, under "Trail +27"', 'What Trail is: what investigators can trace back to the cell; it stays with the campaign.', [f('line', 'line')], { line: G['trail.means']! }),
      item('trail.law', 'The fold\'s last line, under the rows', `The rule behind the shooting rows (rulebook gunfire-trail, hit-trail, down-trail): +${fire} for firing on someone, +${hit} if they are hit, +${down} if they go down.`, [f('line', 'line')], { line: G['trail.law']! }),
      item('trail.byWhat', 'The fold\'s view toggle', 'Rows by what left the Trail.', [f('label', 'label', [], [3, 16])], { label: G['trail.by.what']! }),
      item('trail.byWho', 'The fold\'s view toggle', 'Rows by which agent left it.', [f('label', 'label', [], [3, 16])], { label: G['trail.by.who']! }),
      ...TRAIL.map(([k, what, fills, rowNow, shortNow]) => item(`trail.rows.${k}`, 'row: one row of the fold, after its +n and before its time; short: the Trail number\'s reason lists it ("the pay, the loop and her call")',
        `The engine's Trail cause ${what}.`, [f('row', 'note', fills, [8, 48], WIDE[k]), f('short', 'note', fills.filter(x => x !== 'op'), [4, 24], WIDE[k])], { row: rowNow, short: shortNow })),
    ]),
    brief('debrief/city', 'What the city does now', ctx, [
      item('city.case', 'A city row\'s head, with its line under it', 'A case is open on a killing found ({who} is the victim): nextMission.cases.', [f('head', 'note', ['who'], undefined, PERSON), f('line', 'note', ['missions'])], { head: G['city.case']!.replace('the guard', '{who}'), line: G['city.case.line']!.replace('{n}', '{missions}') }),
      item('city.watch', 'A city row', 'More watch at a place for the next missions (nextMission.areaWatch): {guards} guards and {cameras} cameras more at {place}, for {missions} missions.', [f('head', 'note', ['guards', 'cameras', 'place']), f('line', 'note', ['missions'])],
        { head: G['city.watch']!.replace('{g}', '{guards}').replace('{c}', '{cameras}').replace('the door', '{place}'), line: G['city.watch.line']!.replace('{n}', '{missions}') }),
      item('city.hunt', 'A city row', 'Investigators are hunting our car (investigators.hunted): {by} saw it leave.', [f('head', 'note'), f('line', 'note', ['by'])], { head: G['city.hunt.car']!, line: G['city.hunt.line']!.replace('a police officer', '{by}') }),
      item('city.kept', 'A city row\'s head over a turned contact kept for the next mission (its line is debrief.assets: "Inez, who can introduce you at the café")', 'A contact the cell keeps.', [f('head', 'note', [], [2, 16])], { head: G['city.kept']! }),
      item('city.word', 'A city row', 'Word of mouth between missions (nextMission.lines, word): {n} people will tell others how the cell treated them; a feeling, never a face or a place.', [f('head', 'note', ['n']), f('line', 'note')], { head: G['city.word']!, line: G['city.word.line']! }),
      item('city.face', 'A city row', '{n} people saw the Fixer\'s face and will describe it ({op} is the Fixer\'s name; nextMission.lines, word, with the face).', [f('head', 'note', ['n', 'op'])], { head: G['city.word.face']!.replace('Hasti', '{op}') }),
    ]),
    brief('debrief/strip', 'The run along the foot: one word or two under each beat', ctx, BEATS.map(([k, what, fills]) =>
      item(`beats.${k}`, 'Under a beat on the strip at the foot of the page, over its time; the strip is narrow, often two lines', `The beat for ${what}.`, [f('label', 'label', fills, [3, 18], { who: 'the driver' })], { label: G[`beat.${k}`] ?? '' }))),
    brief('debrief/next', 'A fail: Next time, and the reason under the Time number', ctx, fails),
  ];
}
