import type { Field, Fill, Form, Pattern, ShortBrief, ShortGuideVersion, ShortItem } from './short-types';
import { whyBriefs19 } from './short-whys19';

/** The why block (text.json `why`, brief 100) as families: every Alert cause and police call with its variants, the report on its way,
 *  the meter and the glossary words, each with the engine's facts from briefs/100/fable/words.md (Fable's notes on the ledger and the log). */
export const FILL: Record<string, string> = {
  who: 'the reporter or witness: a cast person by name or role (Inez, Popovic, the guard, the driver), a passer-by by job (a clerk, a local), a camera by its place, the patrol, the desk, an officer',
  about: 'who or what it is about: the body, the subject of a Pay, the cut camera', agent: 'the agent, by role (the Scout, the Fixer, the Tech, the Enforcer)',
  where: 'the nearest named place within 13 m with its preposition ("at the Office door"), else "nearby"', to: 'the report point: the café phone, the Office door, the official door, the call box, a phone',
  alert: 'the Alert reached, 1 to 3', clock: 'the mission clock, m:ss', s: 'seconds until it lands', cause: 'the short of the cause that set it off', job: 'a person\'s job, one word',
};
const WIDE: Record<string, string> = { who: 'the entrance camera', about: 'a kiosk attendant', agent: 'the Enforcer', where: 'by the control cabinet', to: 'the official door', alert: '3', clock: '10:30', s: '99', cause: 'the desk saw the entrance camera go dead', job: 'shopkeeper' };
const WIDE_BY: Record<string, Partial<Record<string, string>>> = { recorded: { about: 'the driver' }, cut: { about: 'the entrance camera' } };
const PERSON = 'a person by name, role or job (Inez, the guard, a clerk)', WATCHER = 'a person by name, role or job, a guard, an officer, the patrol, or a camera by its place';
/** Guide /5: what each fill names for this cause (the /4 meanings were the same for every cause, so a line could read "the entrance camera walks to the call box"). [means, widest] */
export const FILL_BY: Record<string, Record<string, [string, string]>> = {
  seen: { who: [`who saw it: ${WATCHER}`, 'the entrance camera'], about: [`the person taken down: ${PERSON}`, 'a kiosk attendant'] },
  carry: { who: [`who saw it: ${WATCHER}`, 'the entrance camera'], about: [`the person carried, a body: ${PERSON}`, 'a kiosk attendant'] },
  dead: { who: ['who found the body: a person, the patrol, the bin round, or a camera by its place', 'the entrance camera'], about: [`the dead person: ${PERSON}`, 'a kiosk attendant'] },
  down: { who: ['who found them: a person, the patrol, the bin round, or a camera by its place', 'the entrance camera'], about: [`the person lying knocked out: ${PERSON}`, 'a kiosk attendant'] },
  woken: { about: [`the person who came round: ${PERSON}, or Popovic`, 'a kiosk attendant'] }, noise: { who: [`who heard it: ${PERSON}`, 'a kiosk attendant'], about: [`the hidden person calling out: ${PERSON}`, 'a kiosk attendant'] },
  mark: { about: ['the downed staff member who missed the check-in: the guard, a guard, a clerk', 'the guard'] }, missing: { who: [`who expected them: ${PERSON}`, 'a kiosk attendant'], about: [`the missing person: ${PERSON}`, 'a kiosk attendant'] },
  cut: { about: ['the camera the Tech cut, by its place: the entrance camera, the pickup camera, the square\'s mast camera', 'the square\'s mast camera'] },
  approach: { who: ['the person the Fixer approached: Inez, the driver, the guard', 'the driver'] }, confirmed: { about: [`the dead person: ${PERSON}`, 'a kiosk attendant'] },
  gunfire: { agent: ['the agent who fired', 'the Enforcer'], about: ['who was fired at: a person, the guard, an officer, a rival, the patrol', 'a kiosk attendant'], who: ['the shooter who is not the cell: the guard, an officer', 'an officer'] },
  violence: { who: [`the witness: ${PERSON}`, 'a kiosk attendant'], about: ['who was fired on: a person, the guard, an officer', 'a kiosk attendant'] },
  drawn: { who: ['the watcher who saw the gun: the guard, a guard, an officer, the patrol, or a camera by its place', 'the entrance camera'], agent: ['the agent with the gun out', 'the Enforcer'] },
  'drawn-people': { agent: ['the agent with the gun out', 'the Enforcer'] }, warned: { who: ['the watcher who said it: the guard, a guard, an officer, the patrol', 'the guard'], agent: ['the agent told', 'the Enforcer'] },
  fought: { about: ['the person the Enforcer went for: the guard, a guard, an officer', 'an officer'] },
  loiter: { who: ['the watcher who saw them: a person, the patrol, or a camera by its place', 'the entrance camera'], agent: ['the agent seen hanging about', 'the Enforcer'] },
  recorded: { who: ['the camera that taped it, by its place', 'the pickup camera'], about: ['who the Fixer paid or threatened: Inez, the driver, the guard, a rival', 'the driver'] },
  car: { who: ['the watcher who reported our car: a person, the patrol, or a camera by its place', 'the entrance camera'] }, sighting: { who: ['the watcher who spotted them', 'the entrance camera'], agent: ['the agent spotted', 'the Enforcer'] },
  witness: { who: ['who saw the shooting and called: a person, a watcher, or a camera by its place', 'the entrance camera'] },
  'shot-at': { about: ['who was shot at: a passer-by by job, Inez, the driver, Popovic', 'a kiosk attendant'] }, attack: { about: ['who fought the Enforcer off: the guard, a guard, an officer', 'an officer'] },
  radio: { who: ['the staff member radioing: the guard, a guard, an officer', 'the guard'] }, walk: { who: [`the person walking to report it: ${PERSON}`, 'a kiosk attendant'] },
  run: { who: [`the person running to report it: ${PERSON}`, 'a kiosk attendant'] }, look: { who: [`the person going to look at the body: ${PERSON}`, 'a kiosk attendant'] },
  camera: { who: ['the camera that has it on tape, by its place', 'the entrance camera'] }, screen: { about: ['the camera the Tech cut, by its place', 'the square\'s mast camera'] },
  robot: { who: ['the patrol, or the bin round', 'the bin round'] }, checkin: { about: ['the downed staff member due to check in: the guard, a guard, a clerk', 'the guard'] },
  tell: { who: [`the witness going to the phone: ${PERSON}`, 'a kiosk attendant'] }, stopped: { who: ['the reporter who never reported: a person, or the patrol', 'a kiosk attendant'] },
};
let V: ShortGuideVersion = 4; // the version the families are built for (set by whyBriefs)
const fills = (key: string, ids: string[]): Fill[] => ids.map(id => {
  const by = V >= 5 ? FILL_BY[key]?.[id] : undefined;
  return { id, means: by?.[0] ?? FILL[id]!, widest: by?.[1] ?? WIDE_BY[key]?.[id] ?? WIDE[id]! };
});
/** The engine's facts per cause: its ledger cause and log, the Alert it sets, and the fills the host has for it. */
export const ALERT: Record<string, [string, string[]]> = {
  seen: ['A watcher saw a takedown and the report landed (report:seen; Alert 3). vale: the act was the Take of Popovic.', ['who', 'about', 'where']],
  carry: ['A watcher saw a body carried and the report landed (report:carry; Alert 2). vale: Popovic led off on foot by the Enforcer (the Take\'s escort).', ['who', 'about', 'where']],
  dead: ['Someone found a body, a killing, and reported it (report:dead; Alert 2): a camera, the patrol or the bin round can find it too.', ['who', 'about', 'where']],
  down: ['Someone found a knocked-out person lying and reported it (report:down; Alert 1, a knockout is never more).', ['who', 'about', 'where']],
  woken: ['A knocked-out person came round and reported the attack (victim.wakes, then report.landed; Alert 1). vale: Popovic, let go after a Take, reported it himself.', ['about', 'where']],
  noise: ['A hidden victim, awake and untied, called out and someone heard and reported it (Alert 1).', ['who', 'about']],
  mark: ['A downed staff member missed their check-in mark and the post was reported empty (post.missed; Alert 1). unknown: the cell has not watched their routine, so the time is not known.', ['about', 'clock']],
  missing: ['Someone who expected a person found them gone and raised the alarm (person.missed; Alert 1).', ['who', 'about']],
  cut: ['The desk saw a camera the Tech cut go dead at its next scan and sent someone to look (camera.cut-reported; Alert 1, or 2 as a second incident).', ['about']],
  approach: ['A person the Fixer approached turned the offer down and reported it (a Loyal subject reports any approach; Alert by the count of incidents).', ['who']],
  staff: ['The Enforcer\'s Enter was cancelled and he came out without Popovic: the Office staff reported it.', []],
  'let-go': ['A Take was cancelled and nobody saw: Popovic, let go, reported it himself.', []],
  confirmed: ['Police checked a body and confirmed a killing, not a knockout (report.confirmed; Alert 2).', ['about']],
  gunfire: ['A shot was fired and heard (gunfire; Alert 2 at once; the police are called and the district seal starts). Base: the cell fired. other: the shooter was not the cell (the guard, an officer).', ['agent', 'about', 'who']],
  violence: ['A person saw someone fired on (violence-seen; Alert 3). shot: a civilian or contact shot at, no witness named. blast: a car blew up near people.', ['who', 'about', 'where']],
  drawn: ['A watcher (a guard, the patrol, the police, a camera) saw an agent with a gun out (drawn.seen; Alert 2).', ['who', 'agent', 'where']],
  'drawn-people': ['People saw an agent with a gun out (witness.weapon-drawn; Alert 1).', ['agent', 'where']],
  warned: ['A watcher told an agent to drop the gun: 2 s to holster or it is called in (only before any shot, at Alert 1 or less; no log line; Alert 1).', ['who', 'agent']],
  fought: ['The person the Enforcer went for saw him coming, drew and called it in (takedown.fails; Alert 2).', ['about']],
  loiter: ['A watcher saw an agent standing in sight too long, and the report landed (attention.landed; Alert 1 or more).', ['who', 'agent', 'where']],
  recorded: ['A camera taped the Fixer paying (base) or threatening (pressure) someone, and the desk saw the tape at its next scan (camera.record-lands; Alert 1 or more).', ['who', 'about']],
  car: ['A watcher saw our car parked where cars don\'t stand, and the report landed (car.report-lands; Alert 1 or more).', ['who']],
  entry: ['The patrol saw the Enforcer go in at the Office door and radioed it (entry.reported; Alert 1 or more).', []],
  sighting: ['The district fears the cell (Menace), so a watcher who sees an agent reports them on sight (no log line).', ['who', 'agent']],
  start: ['The job began with the Alert already above 0 (the start state).', []],
};
export const POLICE: Record<string, [string, string[]]> = {
  heard: ['Shots were heard in the district (security.called): the police are coming.', ['where']], witness: ['A person or a watcher saw the shooting and called the police.', ['who']],
  'shot-at': ['A civilian, Inez, the driver or Popovic was shot at, and people called the police.', ['about']], patrol: ['The patrol was shot at and called it in.', ['where']],
  rivals: ['Someone reported the gunfight with the rival crew.', ['where']], gang: ['A shooting nobody saw the shooter of (security.gang): police come, but the district does not seal yet.', []],
  drawn: ['A watcher saw an agent with a gun out and called the police.', ['who', 'agent']], attack: ['The person the Enforcer went for fought him off and called the police.', ['about']],
  post: ['The guard missed his check-in, so police come to the empty post. unknown: the cell has not watched his routine.', []],
  report: ['A report landed at Alert 2 or more: the police unit is sent to the scene; the short is the cause\'s.', ['cause']],
  body: ['A body was found: police come to hold the doors near it.', []], robots: ['A report landed at Alert 1: robots, not police, come to look; the short is the cause\'s.', ['cause']],
  more: ['The search goes on: the desk sends more police (response.reinforces).', []], cordon: ['At Alert 3 the doors lock and police close the district\'s exits (doors.lock).', []],
  loud: ['The final fight is loud: police come in for it. Base: there were shots earlier. alert: no shot before, but security was already searching (Alert 2).', []],
  split: ['Popovic is gone (away in a car): police close in on whoever of the cell is left.', []],
  seal: ['The district seal has started: at its clock the district seals (seal.starts); the short is the cause that set it off.', ['cause', 'clock']],
};
const WAYS: Record<string, [string, string[]]> = {
  radio: ['Staff (a guard) radio the report from where they stand; it lands in {s} s. The cell can still stop it: down the reporter first.', ['who', 's']],
  walk: ['A person walks to the nearest report point (a staffed door or the call box) to report it; it lands in {s} s. Stoppable.', ['who', 'to', 's']],
  run: ['A person runs to the nearest report point to report it; it lands in {s} s. Stoppable.', ['who', 'to', 's']],
  look: ['An Idealist goes to look at the body first, then reports it: no landing time yet. Stoppable.', ['who']],
  camera: ['A camera has it on tape: the desk sees it at its next scan, in {s} s. Not stoppable.', ['who', 's']],
  screen: ['A camera the Tech cut: the desk sees the dead screen at its next scan, in {s} s. Not stoppable.', ['about', 's']],
  robot: ['The patrol or the bin round is ringing the desk; it lands in {s} s. Stoppable only for a loiterer, a parked car or an entry (down the robot).', ['who', 's']],
  checkin: ['A downed staff member is due to check in; when they don\'t, the desk knows. Shown only once their routine is known. Not stoppable.', ['about', 's']],
  tell: ['A witness is going to a phone to tell a friend: not a report, though a Loyal friend may make one. Stoppable.', ['who', 'to']],
};
const FORM: Record<string, Form> = { title: 'title', short: 'short', line: 'line', plate: 'plate' };
const LIMIT: Record<Form, [number, number]> = { title: [4, 32], short: [6, 56], line: [16, 80], plate: [6, 36], speech: [12, 80], label: [4, 28], note: [6, 40] };
const field = (id: string, form: Form, key: string, ids: string[]): Field => ({ id, form, words: LIMIT[form][0], chars: LIMIT[form][1], count: 1, fills: fills(key, ids), keep: [], patterns: [] });
/** Guide /7: the fills today's text carries for who and what (the cause itself) and when it lands stay; a counting plate keeps its countdown. /9: and the level (the meter's head). */
const KEEP_FILLS = ['who', 'about', 'agent', 'to', 's'];
const MEANS: Record<string, string> = { who: 'who saw it or makes the report', about: 'whom or what it is about', agent: 'which agent', to: 'where the report goes', s: 'when it lands', alert: 'the level it names' };
function kept(f: Field, now: string | undefined): Field {
  if (V < 7 || !now) return f;
  const need = (V >= 9 ? [...KEEP_FILLS, 'alert'] : KEEP_FILLS).filter(id => now.includes(`{${id}}`)).map(id => ({ re: `\\{${id}\\}`, flags: '', must: true, why: `keep {${id}}: it says ${MEANS[id]}` }));
  const count = f.form === 'plate' && / · \{s\} s$/.test(now) ? [{ re: ' · \\{s\\} s$', flags: '', must: true, why: 'a plate that counts down ends " · {s} s", the seconds to its landing (brief 100)' }] : [];
  return { ...f, patterns: [...need, ...count] };
}
/** Guide /9 (Aster's review, 2026-11-07): the /5–/7 police titles named their cause, so the card read "Shots heard · shots were heard" and never said police were coming,
 *  and brief 100's HUD tests failed on the player's copy. A police title names who or what is coming; the words those tests read are held (tools/game/hostlogic/HostLogic.Why100.cs). */
const POLICE_TITLE: Pattern = { re: 'police|robots|lockdown|seal', flags: 'i', must: true, why: 'a police title names who or what is coming (Police coming, More police, Robots coming, Lockdown); the cause is the short, under it on the same card' };
const HL = (test: string) => `brief 100's HUD test reads it (HostLogic.Why100.cs, ${test})`;
const HELD_9: Record<string, Record<string, { keep?: string; patterns?: Pattern[] }>> = {
  'alert.gunfire': { line: { patterns: [{ re: '^\\{agent\\} fired at \\{about\\}', flags: '', must: true, why: HL('the loud pin: "The Enforcer fired at the guard"') }] } },
  'alert.drawn': { short: { keep: '{who} saw {agent}\'s gun' } }, // the seal's short on fail-enforcer-down
  'alert.down': { short: { patterns: [{ re: '^\\{who\\} .*\\{about\\}', flags: '', must: true, why: HL('the radio: "another guard … the guard … at the Office door", the fills in that order') }] } },
  'police.body': { title: { keep: 'Police coming' } }, 'police.more': { short: { keep: 'the search goes on' } }, 'police.attack': { short: { keep: '{about} called in an attack' } },
  'onWay.ways.radio': { plate: { keep: 'Radioing it in · {s} s' } }, 'onWay.ways.walk': { plate: { keep: 'Going to {to} · {s} s' } },
  'meter.head': { plate: { patterns: [{ re: '^Why it', flags: '', must: true, why: HL('the loud pin: "Why it\'s Alert 2"') }, { re: '\\{alert\\}', flags: '', must: true, why: 'the meter\'s head names the level its list explains' }] } },
};
function held(key: string, f: Field): Field {
  if (V < 9) return f;
  const h = HELD_9[key]?.[f.id], title = key.startsWith('police.') && f.id === 'title';
  if (!h && !title) return f;
  const more = [...(title ? [POLICE_TITLE] : []), ...(h?.patterns ?? [])].filter(p => !f.patterns.some(q => q.re === p.re)); // a fill /7's kept() already holds is held once
  return { ...f, keep: h?.keep ? [h.keep] : f.keep, patterns: [...f.patterns, ...more] };
}
const SHOWN: Record<string, string> = {
  title: 'title: the card and the Journal row', short: 'short: the crucial card\'s dim line with {where} after it, and the Alert meter\'s row', line: 'line: one sentence on Alt, in the Journal, on the meter\'s hover and in the debrief\'s timeline',
  plate: 'plate: over the reporter while the report is on its way, then " · {s} s"',
};
/** Guide /9: a police card's title, said where it shows (Aster's review: "Shots heard · shots were heard"). */
const TITLE_9 = 'title: the police card\'s head, over its short on the same card, and the Journal row: who or what is coming (Police coming, More police, Robots coming, Lockdown), never the cause, which the short gives';
const SHOWN_7 = 'plate: over the reporter while the report is on its way; a plate that counts down ends " · {s} s", the seconds to its landing, and its six words are the part before';
type W = { title?: string; short?: string; line?: string; plate?: string; variants?: Record<string, W> };

/** Guide /6: a fact the /5 families had wrong (told and learned: before a Watch the cell doesn't know a post has a check-in at all). */
export const ALERT_6: Record<string, [string, string[]]> = {
  mark: ['A downed staff member missed their check-in mark and the post was reported empty (post.missed; Alert 1). unknown: the cell has not watched their routine, so it doesn\'t know the post has a check-in at all: that variant says only that the person was missed from the post, never "check-in" and never the time.', ['about', 'clock']],
};
function causeItems(group: 'alert' | 'police', block: Record<string, W>, facts: Record<string, [string, string[]]>, keys: string[]): ShortItem[] {
  return keys.flatMap(k => {
    const c = block[k]!, [tells, ids] = (V >= 6 && group === 'alert' ? ALERT_6[k] : undefined) ?? facts[k]!;
    const one = (path: string, w: W, label: string): ShortItem => {
      const parts = (['title', 'short', 'line'] as const).filter(f => w[f] !== undefined);
      return { key: path, where: parts.map(p => V >= 9 && group === 'police' && p === 'title' ? TITLE_9 : SHOWN[p]).join('; '), tells: `${label}: ${tells}`, speaker: null,
        fields: parts.map(p => held(path, kept(field(p, FORM[p]!, k, ids), w[p]))), current: Object.fromEntries(parts.map(p => [p, [w[p]!]])), stays: 'The host reads it for every rise of this cause (why.schema.ts).' };
    };
    return [one(`${group}.${k}`, c, `${group === 'alert' ? 'Alert' : 'Police'} cause "${k}"`),
      ...Object.entries(c.variants ?? {}).map(([v, w]) => one(`${group}.${k}.variants.${v}`, w, `"${k}", its ${v} variant`))];
  });
}
const plain = (key: string, form: Form, text: string, tells: string, ids: string[] = []): ShortItem => ({ key, where: SHOWN[form] ?? form, tells, speaker: null,
  fields: [held(key, kept(field(form, form, key, ids), text))], current: { [form]: [text] }, stays: 'The host reads it (why.schema.ts).' });

export function whyBriefs(text: any, v: ShortGuideVersion): ShortBrief[] {
  if (v >= 19) return whyBriefs19(text, whyBriefs(text, 18), { FILL, FILL_BY, ALERT: { ...ALERT, ...ALERT_6 }, POLICE }, v); // guide /19 (brief 101): the lozenge's shorts and the places
  V = v;
  const w = text.why, A = w.alert as Record<string, W>, P = w.police as Record<string, W>;
  const brief = (id: string, title: string, context: string, items: ShortItem[]): ShortBrief => ({ id, kind: 'why', title, context, items });
  const ctx = 'The why block\'s words, keyed by the engine\'s cause; a fill is filled by the host from the engine. A `variants` entry words the same cause in a case the engine names (its `pick` says which), and keeps the base\'s fields it doesn\'t write.';
  return [
    brief('why/witnessed', 'Why the Alert rose: someone saw, found or missed something', ctx, causeItems('alert', A, ALERT, ['seen', 'carry', 'dead', 'down', 'woken', 'noise', 'mark', 'missing', 'cut', 'confirmed'])),
    brief('why/reported', 'Why the Alert rose: a report, a tape, a car, an entry', ctx, causeItems('alert', A, ALERT, ['approach', 'staff', 'let-go', 'recorded', 'car', 'entry', 'sighting', 'start', 'loiter'])),
    brief('why/gunfire', 'Why the Alert rose and the police came: shots and guns', ctx, [...causeItems('alert', A, ALERT, ['gunfire', 'violence', 'drawn', 'drawn-people', 'warned', 'fought']),
      ...causeItems('police', P, POLICE, ['heard', 'witness', 'shot-at', 'patrol', 'rivals', 'gang', 'drawn', 'attack'])]),
    brief('why/police', 'Why the police came, the second report and what each Alert sets off', ctx, [...causeItems('police', P, POLICE, ['post', 'report', 'body', 'robots', 'more', 'cordon', 'loud', 'split', 'seal']),
      { key: 'again', where: 'short after the cause\'s own short; line after its line', tells: 'The Alert came from the count of incidents, not this cause alone: a second report means a search (Alert 2).', speaker: null,
        fields: [field('short', 'short', 'again', []), field('line', 'line', 'again', [])], current: { short: [w.again.short], line: [w.again.line] }, stays: 'The host reads it (why.schema.ts).' },
      ...(['1', '2', '3'] as const).map(n => plain(`effect.${n}`, 'line', w.effect[n], `What Alert ${n} sets off (GDD §05, the Handover's dial): ${['robots come to look', 'police come to search, hiding places too', 'the doors lock, police close the exits and the roads shut'][Number(n) - 1]}.`))]),
    brief('why/on-the-way', 'A report on its way: the reporter\'s plate, and the Alert meter', ctx, [
      ...Object.entries(WAYS).map(([k, [tells, ids]]) => ({ key: `onWay.ways.${k}`, where: `${v >= 7 ? SHOWN_7 : SHOWN.plate}; line: on Alt and in the Journal`, tells: `How it travels, "${k}": ${tells}`, speaker: null,
        fields: [held(`onWay.ways.${k}`, kept(field('plate', 'plate', k, ids), w.onWay.ways[k].plate)), held(`onWay.ways.${k}`, kept(field('line', 'line', k, ids), w.onWay.ways[k].line))], current: { plate: [w.onWay.ways[k].plate], line: [w.onWay.ways[k].line] }, stays: 'The host reads it (why.schema.ts).' }) as ShortItem),
      plain('onWay.fix', 'plate', w.onWay.fix, 'Follows a plate the cell can still stop: downing the reporter first drops the report.'),
      plain('onWay.nofix', 'plate', w.onWay.nofix, 'Follows a plate the cell can\'t stop: a tape, a dead screen, a robot\'s ring, a check-in.'),
      { key: 'onWay.stopped', where: 'plate: the pop when a report never lands; line: the Journal', tells: 'The report never landed: its reporter was downed or stopped first.', speaker: null,
        fields: [kept(field('plate', 'plate', 'stopped', []), w.onWay.stopped.plate), kept(field('line', 'line', 'stopped', ['who']), w.onWay.stopped.line)], current: { plate: [w.onWay.stopped.plate], line: [w.onWay.stopped.line] }, stays: 'The host reads it (why.schema.ts).' },
      plain('meter.head', 'plate', w.meter.head, `Heads the Alert meter's list of every rise so far, newest first (a click or hover on the Alert lamps)${v >= 9 ? ': it names the Alert now, {alert}, the level the list explains' : ''}.`, ['alert']),
      plain('meter.empty', 'plate', w.meter.empty, 'The meter\'s list at Alert 0, with nothing raised.'),
      plain('meter.start', 'plate', w.meter.start, 'The meter\'s row for an Alert set at the start of the job.', ['alert'])]),
    brief('why/words', 'The why block\'s glossary: reporters who are no person, report points, places', ctx, [
      ...Object.entries(w.by as Record<string, string>).map(([k, x]) => ({ ...plain(`by.${k}`, 'label', x, `{who} when the reporter is no person: "${k}"${k === 'another' ? ' (when the reporter shares a job with {about}, so no line reads "a guard spotted the guard")' : ''}.${v >= 5 && k === 'camera' ? ' Only for a city camera with no name of its own: the entrance camera, the pickup camera and the square\'s mast camera are named from names.targets.' : ''}`, k === 'another' ? ['job'] : []), where: 'a word filled into {who}' })),
      ...Object.entries(w.onWay.to as Record<string, string>).map(([k, v]) => ({ ...plain(`onWay.to.${k}`, 'label', v, `{to}: the report point "${k}".`), where: 'a word filled into {to}' })),
      ...Object.entries(w.places as Record<string, string>).map(([k, v]) => ({ ...plain(`places.${k}`, 'label', v, `{where}: the site's place "${k.replace(/^d:/, '')}", with its preposition, read after a short ("a clerk saw a takedown ___").`), where: 'a phrase filled into {where}' })),
    ]),
  ];
}
