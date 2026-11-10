import { gddPair, pick, type Read } from './guide';
import { VoiceSheet, type Field, type Pattern, type ShortBrief, type ShortGuideVersion, type ShortItem } from './short-types';
import { FAMILIES_13, GONE_13, REACTS_13, reply13, scene13, WORLD_13 } from './short-natural';
import { ANSWER_14, family14, MISLEAD_14, scene14 } from './short-natural14';
import { GUARD_BUSY_18, RADIO_18, SUMMON_18 } from './short-hand18';
import { guard102 } from './short-guard102';
import { speech12, VOICE_SHEET } from './short-voice';

/** The bark table (text.json `dialogue`) as families, one run each: what each bark must tell the player, from the event's own trigger,
 *  the verb's plain line (`actions.*.does`), what a Watch or a Listen teaches (`targets`) and the GDD's temperaments (§04). */
type Row = { event: string; verb?: string; speaker: string; outcome?: string; temperament?: string; about?: string; pending?: string; parked?: string; silent?: string; sky?: string; hand?: string; lines: string[] };
const PEOPLE = ['waitress', 'driver', 'guard', 'councillor'];
const MACHINES = ['camera-entrance', 'camera-pickup', 'camera-mast-z0', 'patrol', 'rivals'];
const any = (...events: string[]) => (r: Row) => events.includes(r.event);
/** Families: a run each. Parked moves (weapons.moves off) and the Halcyon cliffhanger (pending Sky) are not played, so not written. */
export const BARK_FAMILIES: { id: string; title: string; pick: (r: Row) => boolean }[] = [
  ...['scout', 'fixer', 'tech', 'enforcer'].map(s => ({ id: `bark/ack-${s}`, title: `The ${s}'s acknowledgements: an order taken`, pick: (r: Row) => r.event === 'ack' && r.speaker === s })),
  { id: 'bark/arrive', title: 'In reach: the act begins', pick: any('arrive') },
  { id: 'bark/done', title: 'The act lands', pick: any('done') },
  { id: 'bark/read-people', title: 'What a Watch or a Listen on a person taught', pick: r => r.event === 'read' && !MACHINES.includes(r.about ?? '') },
  { id: 'bark/read-machines', title: 'What a Watch on a camera, the patrol or the rival crew taught', pick: r => r.event === 'read' && MACHINES.includes(r.about ?? '') },
  { id: 'bark/reply-inez-ask-pay', title: 'Inez answers Ask and Pay, and confirms the call', pick: r => (r.event === 'reply' && r.speaker === 'waitress' && ['ask', 'pay'].includes(r.verb!)) || r.event === 'confirm' },
  { id: 'bark/reply-inez-help-pressure', title: 'Inez answers Help and Pressure', pick: r => r.event === 'reply' && r.speaker === 'waitress' && ['help', 'pressure'].includes(r.verb!) },
  { id: 'bark/reply-driver-ask-pay', title: 'The driver answers Ask and Pay', pick: r => r.event === 'reply' && r.speaker === 'driver' && ['ask', 'pay'].includes(r.verb!) },
  { id: 'bark/reply-driver-help-pressure', title: 'The driver answers Help and Pressure', pick: r => r.event === 'reply' && r.speaker === 'driver' && ['help', 'pressure'].includes(r.verb!) },
  { id: 'bark/reply-guard-popovic', title: 'The guard answers; Popovic is taken', pick: r => (r.event === 'reply' && r.speaker === 'guard') || r.event === 'taken' },
  { id: 'bark/summons', title: 'The call refused; questioned later', pick: any('summon-refused', 'summon-unanswered', 'summon-knows-down', 'questioned') }, // brief 102: nobody answers her call, or she knows why
  { id: 'bark/clock', title: 'The mission\'s clock: the chime, Popovic, the rival car, the driver', pick: any('chime', 'last-turn', 'vale-exits', 'vale-at-car', 'rivals-coming', 'rivals-arrive', 'rivals-follow-car', 'car-parks', 'car-leaves', 'driver-tail', 'close-warning') },
  { id: 'bark/guard-call', title: 'The guard off his door and back', pick: any('guard-lured', 'guard-at-cafe', 'guard-leaves-cafe', 'guard-back', 'guard-recalled', 'guard-woken', 'guard-stands-down') },
  { id: 'bark/machines', title: 'Cameras, the loop and the patrol', pick: any('camera-shot', 'camera-reported', 'loop-ends', 'patrol-rerouted', 'patrol-returns', 'patrol-breaks', 'patrol-alarm', 'patrol-stands-down', 'patrol-resumes', 'patrol-sweep', 'post-missed') },
  { id: 'bark/noticed', title: 'A watcher notices: a runner, a gun, a takedown', pick: any('run-seen', 'drawn-seen', 'takedown-seen') },
  { id: 'bark/witnesses', title: 'A witness reacts and reports', pick: any('witness-reacts', 'witness-screams', 'report-begins') },
  { id: 'bark/aftermath', title: 'Shots remembered, victims, the missed and the crowd', pick: any('saw-gunfire', 'gunfire', 'victim-wakes', 'victim-calls-out', 'person-missed', 'crowd-state') },
  { id: 'bark/alarm', title: 'The alarm, the response and going loud', pick: any('alarm', 'response-sent', 'response-checks', 'lockdown', 'going-loud') },
  { id: 'bark/final-fight', title: 'The final fight at the car', pick: any('fight-loud', 'fight-safe', 'fight-ambush', 'fight-warned', 'fight-guard', 'blocker', 'fight-over', 'rivals-recalled', 'rivals-wiped', 'rivals-for-car', 'rivals-back-to-cover') },
  { id: 'bark/gunmen', title: 'The armed side\'s states: alerted, advance, engage, hold, cover', pick: any('ai-state', 'ai-cover') },
  { id: 'bark/rivals', title: 'The rival crew moves; the cell is hurt', pick: any('ai-advance', 'ai-break', 'ai-gone', 'ai-join', 'rivals-saw-takedown', 'rivals-retreat', 'rivals-drawn', 'rivals-misled', 'car-burns', 'agent-down', 'enforcer-warning') },
  { id: 'bark/end', title: 'Away, withdrawn, and the file', pick: r => r.event === 'win' || r.event === 'withdraw' || (r.event === 'cliffhanger' && !r.pending) },
  { id: 'bark/fail', title: 'A failed attempt, in the moment', pick: any('fail') },
];

/** Guide /4: an order's acknowledgements by verb, all four agents together, so their voices are scored side by side. */
const ACKS_4: [string, string, string[]][] = [
  ['acts', 'Acknowledgements: watch, listen, the approaches, the call, the Tech\'s and the Enforcer\'s jobs', ['watch', 'listen', 'mislead', 'ask', 'pay', 'help', 'pressure', 'call', 'loop', 'disable', 'reroute', 'enter', 'take', 'takedown']],
  ['guns', 'Acknowledgements: fire, draw, holster, cease fire', ['fire', 'draw', 'holster', 'cease']], ['fire', 'Acknowledgements: volley, when to fire, panic, the field', ['volley', 'stance', 'panic', 'field']],
  ['car', 'Acknowledgements: board, go, drive, withdraw, end, the range\'s spawn', ['board', 'take-car', 'go', 'drive-car', 'withdraw', 'end', 'spawn']], ['feet', 'Acknowledgements: walk, run, cancel', ['move', 'run', 'cancel']],
  ['bodies', 'Acknowledgements: carry, put down, hide, tie up', ['carry', 'drop', 'stash', 'restrain']],
];
/** Guide /12: the acknowledgements that still speak under Fable's frequency (walks, runs, the car, fire and put down are silent), in three families. */
const ACKS_12: [string, string, string[]][] = [
  ['acts', 'Acknowledgements: watch, listen, the approaches, the call, the Tech\'s and the Enforcer\'s jobs, the end', [...ACKS_4[0]![2], 'end']],
  ['fight', 'Acknowledgements: draw, and everyone fire', ['draw', 'panic']], ['bodies', 'Acknowledgements: carry, hide, tie up', ['carry', 'stash', 'restrain']],
];
const familiesFor = (v: ShortGuideVersion) => v < 4 ? BARK_FAMILIES : v >= 13 ? [ // guide /13 (brief 102): the agents' families go; the cell's scene lines have their own
  ...FAMILIES_13, ...(v >= 14 ? [MISLEAD_14] : []), ...BARK_FAMILIES.filter(f => !GONE_13.test(f.id)).map(f => v >= 14 ? family14(f) : f).flatMap(f => f.id === 'bark/witnesses' ? WITNESSES_6 : [f]),
] : [
  ...(v >= 12 ? ACKS_12 : ACKS_4).map(([id, title, verbs]) => ({ id: `bark/ack-${id}`, title, pick: (r: Row) => r.event === 'ack' && verbs.includes(r.verb!) })),
  ...BARK_FAMILIES.filter(f => !f.id.startsWith('bark/ack-')).flatMap(f => v >= 6 && f.id === 'bark/witnesses' ? WITNESSES_6 : [f]),
];
/** Guide /6: the witnesses in two families, passers-by and the cast. */
const WITNESSES_6 = [
  { id: 'bark/witnesses-crowd', title: 'A passer-by reacts and reports; the guard radios', pick: (r: Row) => ['witness-reacts', 'witness-screams', 'report-begins'].includes(r.event) && ['crowd', 'guard'].includes(r.speaker) },
  { id: 'bark/witnesses-cast', title: 'Inez and the driver react to what they saw', pick: (r: Row) => ['witness-reacts', 'witness-screams', 'report-begins'].includes(r.event) && !['crowd', 'guard'].includes(r.speaker) },
];
const AGENT: Record<string, string> = { scout: 'the Scout', fixer: 'the Fixer', tech: 'the Tech', enforcer: 'the Enforcer' };
const WHO: Record<string, string> = { ...AGENT, waitress: 'Inez', driver: 'the driver', guard: 'the guard', councillor: 'Popovic', rivals: 'a rival', patrol: 'the clockwork patrol', security: 'the police', watcher: 'the person watching a camera', crowd: 'a passer-by' };
/** Guide /5: each temperament in plain words, from its GDD §04 row (the Hero and Menace columns are the city's view of the cell). */
const PLAIN_T: Record<string, string> = {
  idealist: 'acts on conscience: Help works, money never does; if the city fears the cell, they may report it',
  mercenary: 'in it for the money: Pay works, dearer if the city likes the cell and cheaper if it fears it',
  fearful: 'frightened: Pressure works; Help takes two visits',
  loyal: 'loyal to their employer: only repeated Help turns them, and if the city fears the cell they report any approach',
};
const POPOVIC: Record<string, string> = { loyal: 'he blusters and threatens, and won\'t bend', mercenary: 'he bargains, offering to outbid whoever pays the cell', fearful: 'he begs, frightened of someone, and it isn\'t the cell' };
/** Guide /4: the one consequence an acknowledgement may add, where it changes what the player does next. */
const ACK_SAY: Record<string, string> = { go: 'whoever isn\'t in the car is left behind', end: 'anyone not away in a car is caught', run: 'running gets noticed', draw: 'a drawn gun gets seen', withdraw: 'the cell is pulling out, to try again' };
/** Order extras the verb's own line doesn't say. */
const ORDER: Record<string, string> = {
  watch: 'Any agent can Watch; the Scout is the cell\'s watcher and the Tech reads machines.', go: 'Go with Popovic aboard is the win; anyone not in the car is left behind.',
  end: 'Offered only once Popovic is out.', spawn: 'The fight range only, a practice ground: not the Handover.', takedown: 'From behind, close; the way is chosen on the order.',
  'takedown/knockout': 'The way chosen: a knockout. They wake later and may report it, unless tied or hidden.', 'takedown/kill': 'The way chosen: a kill. A body; a found killing raises the Alert and costs the cell.',
  call: 'Inez then calls the guard over from his door.', enter: 'Interiors are off-screen: he comes back out with Popovic.', carry: 'Carrying, everyone but the Enforcer is slow.',
};
const DONE: Record<string, string> = {
  mislead: 'He believed the story and walks off for a while. He comes back later, and he knows the Scout\'s face.',
  loop: 'The camera is looped: for two minutes the desk sees an old picture. The Tech is free to go.',
  disable: 'The camera is cut for good. At its next scan the desk sees the dead screen and sends someone; that counts as a report.',
  reroute: 'The patrol has left its sweep on false orders; it comes back when they run out.',
  enter: 'The Enforcer is inside the Office, looking for Popovic (off-screen); he comes out with him.',
  take: 'The Enforcer has Popovic and walks him to our car: walking is the only pace with Popovic.',
  'turn/turned': 'The person is turned: the cell can call on them when it is time.', 'turn/refused': 'They said no, and they will remember it: a later offer costs more.',
  'turn/not-yet': 'Not yet: one more Help would do it; the Fixer can come back.', 'turn/kept': 'A second Help for someone already turned: their need is met, and they are gladder of the cell.',
  'turn/suspicious': 'They are reporting the approach: a report is on its way, and the Alert can rise.', 'turn/small-offer': 'Too little: a Help before the Listen knows too little of their need and is refused. Listen first, then offer.',
  takedown: 'The person is down.', 'takedown/knockout': 'Knocked out: they will wake, and may report it unless tied or hidden.', 'takedown/kill': 'Dead: a body now, and a found killing raises the Alert.',
  stash: 'The body is hidden in a bin or behind the service door: out of sight, though a bin round or a search may find it.', restrain: 'Tied: when they wake they can\'t call out or report.',
};
/** Guide /12: the landings' facts where /4–/11 had them wrong or read out a time. enter.out plays as the Enforcer comes back out with Popovic, not while he looks for him. */
const DONE_12: Record<string, string> = {
  enter: 'The Enforcer has come back out of the Office with Popovic (inside was off-screen). Popovic\'s own first words follow at once (he blusters, bargains or begs, by his temperament), so the line can lead into them.',
  loop: 'The camera is looped: the desk sees an old picture until the loop runs out (the HUD shows how long). The Tech is free to go.',
  disable: 'The camera is cut for good. At its next scan the desk sees the dead screen and sends someone if anyone is free; that counts as a report.',
  mislead: 'One of the rival crew believed the story and walks off for a while. He comes back later, and he knows the Scout\'s face.',
};
const OUTCOME: Record<string, string> = {
  turned: 'Yes: turned. They will do their part when the cell calls.', refused: 'No, and they remember the asking: a later offer costs more.',
  'not-yet': 'Not yet: one more Help would turn them.', kept: 'Already turned and helped again: grateful, their need met.',
  suspicious: 'They will report the approach to the Office or the owner: a report is on its way.', 'small-offer': 'No: too little for what they owe, offered before the cell knew their need.',
};
const SUMMON: Record<string, string> = {
  'summon-busy': 'he can\'t leave the door just now', 'summon-came-once': 'he has come once already this shift, and won\'t again', 'summon-alert': 'an alarm is up, so he stays at his post',
  'summon-saw-violence': 'he saw the cell\'s violence, so he won\'t come', 'summon-too-far': 'he is too far from his post to come quickly', 'summon-off-street': 'he isn\'t out front where she can reach him',
  'summon-no-visit': 'there is nobody at a post she can send for',
};
/** Guide /17: the no-visit refusal as the engine gives it. Only a call on a contact whose tie has no post to leave gets it, and in the Handover's cast that is a call on the
 *  turned guard (sim GameOrders.cs SummonRefusal: his tie is Inez); the host's card shows his answer, then Inez's line as the tie's end (HostFeed.cs FeedSummonRefused). */
const NO_VISIT_17: Record<string, string> = {
  guard: 'The Fixer has called the guard, turned. A call has a contact send for their tie, and his is Inez, who has no post to leave, so this call moves nobody. The guard answers the Fixer. Nothing is spent.',
  waitress: 'Inez\'s line after the guard\'s on the same card. In the Handover it plays only when the Fixer calls the turned guard: his tie is Inez, who has no post to leave, so the call moves nobody; she is not on that call. (In a cast where her own tie had no post, she would tell the Fixer on the telephone that there is nobody she can send for.) Nothing is spent.',
};
const WORLD: Record<string, string> = {
  chime: '5:10, the handover chime: the shift ends and the Office staff come out. Popovic comes out at about 5:30 and walks to his car.',
  'last-turn': '5:20: the last chance to turn the driver before Popovic comes out.', 'vale-exits': 'Popovic has left the Office and is walking to his car at the pickup.',
  'vale-at-car': 'Popovic is at his car: unless the driver is turned, he gets in and is driven off.', 'rivals-coming': 'A car turns in off Orbit Walk, out of sight: heard, never seen. Not one of ours: the rival crew\'s.',
  'rivals-arrive': 'The rival car has stopped across our car\'s way out: our car is boxed in and can\'t drive off.', 'rivals-follow-car': 'Our getaway car drives off, and the three posted rivals go after it.',
  'car-parks': 'The turned driver parks Popovic\'s car in the service bay, the back way: Popovic is there to take.', 'car-leaves': 'The turned driver drives Popovic round the back, to the service door.',
  'driver-tail': 'A Listen on the driver: he says the same car has tailed them all week (the rival crew).', 'close-warning': 'A minute before the close, Popovic is missed: one minute to get away.',
  'guard-lured': 'Inez has called the guard: he leaves his door for her café.', 'guard-at-cafe': 'The guard has reached Inez, who keeps him talking: the Office door is unguarded for about a minute.',
  'guard-leaves-cafe': 'The guard starts back to his door: the door is open only until he is back.', 'guard-back': 'The guard is back on the door: the door is guarded again.',
  'guard-recalled': 'The alarm is up during his visit: the guard walks back to his post.', 'guard-woken': 'A knocked-out guard is back on his feet and walking to his post.',
  'guard-stands-down': 'Nobody to fight in reach: the guard lowers his gun and goes back to the door.',
  'camera-shot': 'A camera has been shot out: it sees nothing now.', 'camera-reported': 'The desk\'s scan found a cut camera dead.',
  'loop-ends': 'The loop is over, run out or cut by a Disable: the camera no longer shows the old picture. Word it for both; never say the camera sees again.',
  'patrol-rerouted': 'The patrol took the false orders and leaves its sweep.', 'patrol-returns': 'The false orders ran out: the patrol is back on its sweep of the yard.',
  'patrol-breaks': 'The patrol breaks its route and heads for gunfire or a drawn weapon.', 'patrol-alarm': 'The patrol leaves its sweep for an alarm.',
  'patrol-stands-down': 'The patrol stands down from the alarm and goes back to its sweep.', 'patrol-resumes': 'Nobody to fight in reach: the patrol turns back to its sweep.', 'patrol-sweep': 'The patrol is back on its sweep.',
  'post-missed': 'A post missed its check-in and is reported empty: someone is sent to look.',
  'run-seen': 'A watcher saw an agent running: noted, though running alone raises no Alert.',
  'drawn-seen': 'A drawn gun was seen: security raise the Alert; people count it as trouble. Or an armed person saw a takedown coming and drew.',
  'takedown-seen': 'A guard, the patrol, the police or a camera saw a takedown or a carried body: the Alert rises at once, and the Enforcer is a foe to them.',
  'witness-screams': 'A witness screams: anyone near hears it.', 'saw-gunfire': 'A person with a clear view of a shot remembers it: asked later, they can say where and when, and who.',
  gunfire: 'Shots seen or heard by people nearby: they react, and the police will hear of it.', 'victim-wakes': 'A knocked-out person comes round, staggers up, and will report the attack.',
  'victim-calls-out': 'A hidden, untied victim is awake and calling out, muffled: anyone near hears it.',
  alarm: 'A report has called the police: they are coming, and the district will seal on a clock.', 'response-sent': 'A report reached the desk: police or robots are sent, or more are added.',
  lockdown: 'The doors are locking and the district is closing: little time left to get out.', 'going-loud': 'The first shot or drawn gun has made the job loud: from now on it is a fight.',
  'fight-loud': 'The final fight, loud: the police come in at our car.', 'fight-safe': 'The final fight: the rivals are waiting at our car.', 'fight-ambush': 'The rivals spring their ambush at the car.',
  'fight-warned': 'Inez, helped, warns the cell in time: armed men by the car.', 'fight-guard': 'The guard, who never saw the cell, runs in against the rivals.',
  blocker: 'A hostile is going for the road to block our car.', 'fight-over': 'The last hostile is down: the car can go.',
  'rivals-recalled': 'The fight starts short-handed: rivals who were away are coming back.', 'rivals-wiped': 'The whole rival crew is already down or tied: the fight is over at once, and the car can go.',
  'rivals-for-car': 'Popovic is in our blocked car and nobody of the cell is outside it with a gun drawn: the rivals walk up to take him. The fix: step out and draw, or down them.',
  'rivals-back-to-cover': 'Someone of the cell is outside the car with a gun drawn, or the car is free: the rivals go back to cover.', 'ai-cover': 'An armed man takes cover.',
  'ai-advance': 'A rival with nobody in reach walks in on the nearest agent.', 'ai-break': 'More than half the rival crew is down: they break for their car.', 'ai-gone': 'A rival made it to his car and is gone.',
  'ai-join': 'The police unit on the street joins the loud fight.', 'rivals-saw-takedown': 'A rival on foot saw one of his crew taken down and fires on the one who did it: the job goes loud.',
  'rivals-retreat': 'The rivals are driven back.', 'rivals-drawn': 'Gunfire at the Office draws the rivals in.', 'rivals-misled': 'A Mislead landed: one of the rival crew on foot walks off for a while.',
  'car-burns': 'A shot car is on fire and will explode in a few seconds: get clear.', 'agent-down': 'The speaker has been hit and is down (the Enforcer down would end the job): another agent can carry them.',
  'witness-reacts': 'A person saw a takedown or a body carried, or found a body: their first reaction, by what they do next.', 'report-begins': 'A witness starts a report.',
  'crowd-state': 'A passer-by near the cell reacts to trouble in the street.', 'person-missed': 'Someone who expected a person (a lover, kin, a workmate, the driver) finds them gone and grows suspicious.',
  'response-checks': 'The police check a scene or a hiding place.', 'ai-state': 'An armed man on the other side changes what he is doing.', 'enforcer-warning': 'The Enforcer is badly hurt, a hit or two from going down: get him into cover.',
  win: 'The car has left with Popovic in it: the job is done.', withdraw: 'The cell has called off the attempt (or the car left without Popovic).',
  cliffhanger: 'In the getaway car Popovic says someone else has a copy of the file, unnamed. The Fixer answers.',
};
const BY_OUTCOME: Record<string, Record<string, string>> = {
  'witness-reacts': { report: 'they will go and report it', flee: 'they freeze or flee, too frightened to report', avoid: 'they look away and keep walking, no report', check: 'they go to look first, then report it', quiet: 'they keep out of it, no report', radio: 'the guard radios it in from where he stands' },
  'report-begins': { radio: 'staff radio the desk from where they stand', phone: 'a passer-by goes for a telephone to call the police' },
  'crowd-state': { startled: 'a passer-by is startled by a shot', fleeing: 'running off the street', sheltered: 'sheltering indoors', returning: 'coming back out, unsure it is over', back: 'back to their day' },
  'ai-state': { alerted: 'they have spotted the cell and ready their guns', advance: 'they move up on the cell', engage: 'they open fire', hold: 'they hold their ground' },
  'response-checks': { found: 'the police found a body at the scene', nothing: 'the police found nothing and move on' },
  'camera-reported': { 'nobody-sent': 'nobody is near enough to send', '': 'robots are sent to look' },
  'person-missed': { loyal: 'a loyal Popovic', mercenary: 'a mercenary Popovic', fearful: 'a fearful Popovic' },
};
const SPEAKS: Record<string, string> = {
  scout: 'The Scout tells her cell.', fixer: 'The Fixer tells the cell.', tech: 'The Tech tells the cell.', enforcer: 'The Enforcer, few words.',
  rivals: 'A rival to his crew: an order or a warning.', patrol: 'The patrol\'s fixed polite phrase.', security: 'The police to each other.', watcher: 'The person at the camera screen, into a handset.',
  crowd: 'A passer-by, to anyone near.', guard: 'The guard.', waitress: 'Inez.', driver: 'The driver.', councillor: 'Popovic.',
};
/** The test and the host's needs: a row that must stay, and why. */
const NAMED = new Set(['rivals-arrive/rivals', 'drawn-seen/guard', 'run-seen/guard', 'run-seen/patrol', 'alarm/security', 'going-loud/enforcer', 'fight-loud/security', 'fight-safe/rivals',
  'fight-warned/waitress', 'win/fixer', 'win/councillor', 'cliffhanger/councillor', 'guard-at-cafe/waitress']);
const CATALOGUE = ['reply', 'confirm', 'questioned', 'saw-gunfire', 'gunfire'];
/** The cell's scene lines by version: /13's, and from /14 the Scout's tip in a Mislead. */
const sceneAt = (v: ShortGuideVersion) => v >= 14 ? scene14 : scene13;
function stays(r: Row, v: ShortGuideVersion): string | null {
  if (v >= 3 && (PINNED_ROWS.has(`${r.event}/${r.speaker}`) || PINNED_ROWS.has(keyOf(r)))) return 'text.test.ts needs this event\'s line from this speaker.';
  // guide /12: the approach scenes play the Fixer's opener and closer; any other order's beat may go silent (a silent row, step 2), since the host's stock line goes too
  if (v >= 12 && r.speaker === 'fixer' && ((r.event === 'arrive' && ['ask', 'pay', 'help', 'pressure'].includes(r.verb!)) || (r.event === 'done' && r.verb === 'turn')))
    return 'The approach scenes play it (text.json scenes, brief 085).';
  if (v >= 13 && sceneAt(v)(r, '')) return 'A scene with a person plays it (brief 102: the cell speak only there).';
  if (v >= 14 && r.event === 'rivals-misled') return 'The Mislead\'s cutaway plays his answer (HostLogic.Quiet102.cs: the rival answers).';
  if (v >= 12 && ['ack', 'arrive', 'done'].includes(r.event)) return null;
  if (['ack', 'arrive', 'done'].includes(r.event)) return 'Every order has this beat for each agent who can give it (text.test.ts); without a line the host speaks a stock "On it."';
  if (['reply', 'read', 'confirm', 'taken'].includes(r.event)) return 'The rulebook can produce it, and the host, the scenes and the checks read it (text.test.ts).';
  if (CATALOGUE.includes(r.event) && PEOPLE.includes(r.speaker)) return 'The dialogue generator holds the city\'s voices to these lines (tools/dialogue fixture).';
  if (NAMED.has(`${r.event}/${r.speaker}`)) return 'One of the brief\'s named moments (text.test.ts).';
  if (r.event === 'guard-leaves-cafe' && r.speaker === 'guard') return 'A proof (Proof085Scenes) knows this line by "Back to my post" or "Break".';
  if (r.event === 'fail' && r.speaker === 'fixer') return 'Every fail the engine can give has the Fixer\'s spoken line (text.test.ts).';
  return null;
}
/** text.test.ts holds the Fixer's report-back on a turn to no gendered word and no narration; Proof085Scenes knows the guard leaving the café by two words. */
const GENDER = '\\b(he|she|him|her|hers|his|himself|herself|man|men|woman|women|boy|girl|sir|madam|lady|lord)\\b', NARRATION = '\\b(will be remembered|was noticed|and they\'ll remember|remembered)\\b|^A kindness';
function patterns(r: Row, v: ShortGuideVersion): Pattern[] {
  if (r.event === 'done' && r.verb === 'turn') return [{ re: GENDER, flags: 'i', must: false, why: 'no gendered word in the Fixer\'s report-back (text.test.ts)' },
    { re: NARRATION, flags: 'i', must: false, why: 'the Fixer reports, never narrates for herself (text.test.ts)' }];
  if (r.event === 'guard-leaves-cafe' && r.speaker === 'guard') return [{ re: 'Break|Back to my post', flags: '', must: true, why: 'Proof085Scenes knows this line by "Back to my post" or "Break"' }];
  return v >= 3 ? [...PINNED, ...(v >= 8 ? PINNED_8 : [])].filter(p => p.on(r)).map(({ on: _, ...p }) => p) : [];
}
/** Guide /3: what text.test.ts pins on a bark's words (each test's own pattern), so the writer knows and the code check holds it. */
const PINNED: (Pattern & { on: (r: Row) => boolean })[] = [
  { on: r => r.event === 'loop-ends', re: 'live|sees|entrance', flags: 'i', must: false, why: 'a loop ends by a run-out or a cut alike, so it never says the camera sees again (text.test.ts)' },
  { on: r => r.event === 'camera-reported' && r.outcome === 'nobody-sent', re: 'coming|robots? (is|are) (coming|on)', flags: 'i', must: false, why: 'nobody is sent, so nobody is coming (text.test.ts)' },
  { on: r => r.event === 'rivals-coming', re: 'Orbit Walk', flags: '', must: true, why: 'the car is heard turning in off Orbit Walk (text.test.ts)' },
  { on: r => r.event === 'rivals-coming', re: '\\b(see|sees|saw|seen|sight|look|looks|unfamiliar|there)\\b', flags: 'i', must: false, why: 'the car is out of sight: heard, never seen (text.test.ts)' },
  { on: r => r.event === 'rivals-for-car' && r.speaker === 'scout', re: 'outside|unguarded', flags: 'i', must: true, why: 'the Scout says what is wrong: nobody outside the car with a gun drawn (text.test.ts)' },
  { on: r => ['rivals-for-car', 'rivals-back-to-cover', 'ai-advance'].includes(r.event), re: 'alert', flags: 'i', must: false, why: 'these moves raise no Alert, so no line names it (text.test.ts)' },
  { on: r => r.event === 'run-seen', re: 'reported', flags: '', must: false, why: 'a run is noticed, never reported (text.test.ts)' },
];
/** Guide /8: conversations.test.ts's "no reachable line still has the spill…" on the rows it reads, so a run can't accept them (the /4 guard-call run said "about a minute"). */
const PINNED_8: (Pattern & { on: (r: Row) => boolean })[] = [
  { on: r => ['guard-at-cafe', 'guard-lured', 'guard-back'].includes(r.event) || r.temperament === undefined && ['ack', 'arrive', 'done'].includes(r.event) || r.temperament === 'fearful' && r.event === 'confirm',
    re: 'spill|coffee|clumsy|\\bcloth\\b|envelope|\\bbonus\\b|as promised|as agreed|half now|\\b(a|one|five) minutes?\\b|tonight|evening', flags: 'i', must: false,
    why: 'no old spill, envelope, bonus or promise, and no time: the guard is away as long as Inez keeps him (conversations.test.ts)' },
];
/** Guide /8: the guard at the café without the GDD's 60 s, which the lines read as a promise. */
const WORLD_8: Record<string, string> = { 'guard-at-cafe': 'The guard has reached Inez, who keeps him talking: the Office door is unguarded while she does.' };
/** Guide /3: rows a test needs by event and speaker, beyond the named moments. */
const PINNED_ROWS = new Set(['rivals-coming/scout', 'rivals-for-car/rivals', 'rivals-for-car/scout', 'rivals-back-to-cover/rivals', 'rivals-back-to-cover/scout', 'ai-advance/rivals', 'ai-advance/scout', 'driver-tail/driver',
  'camera-reported/scout', 'fail/scout/cell-down']);
const keyOf = (r: Row) => [r.event, r.verb, r.speaker, r.outcome, r.temperament, r.about].filter(Boolean).join('/');

/** `hand`: the families of a hand run (guide /18), whose hand rows are items again, so the judge reads the family as it plays. */
export function barkBriefs(read: Read, names: string[], text: any, v: ShortGuideVersion, hand = false): ShortBrief[] {
  const gdd = read(gddPair(names).agent.path), voices = text.tone.voices as Record<string, string>;
  const temperament = (t: string) => {
    const name = t[0]!.toUpperCase() + t.slice(1);
    const [respondsTo, hero, menace] = pick(gdd, new RegExp(`\\| ${name}(?: \\(institution\\))? \\| ([^|]+) \\| ([^|]+) \\| ([^|]+) \\|`), `the ${name} row (§04)`).map(x => x.trim());
    return `${t}${v >= 5 ? `, in plain words: ${PLAIN_T[t]}` : ''} (GDD §04: responds to ${respondsTo}; with a Hero cell ${hero}; with a Menace cell ${menace})`;
  };
  /** Guide /6: the cell has no approach on Popovic, so his temperament is how he takes being taken (the voice sheet), never what turns him. */
  const popovic = (t: string) => `${t}: the cell has no approach on him (no Ask, Pay, Help or Pressure), so this is how he will take being taken: ${POPOVIC[t]}`;
  const verbLine = (v: string) => text.actions[v] ? `${text.actions[v].label}: "${text.actions[v].does}"` : v;
  const fail = (code: string) => {
    const causes = Object.values(text.debrief.cause[code] ?? {}) as { head: string; line: string }[];
    return [text.briefing.fails[code], ...causes.map(c => `${c.head}: ${c.line}`)].filter(Boolean).join(' Or: ');
  };
  const tells = (r: Row): string => {
    const ev = text.events[r.event], who = WHO[r.speaker] ?? r.speaker, subject = r.event === 'read' ? WHO[r.about ?? ''] ?? 'the person' : who;
    const temp = r.temperament ? ` ${subject[0]!.toUpperCase() + subject.slice(1)} is ${v >= 6 && r.event === 'read' && r.about === 'councillor' ? popovic(r.temperament) : temperament(r.temperament)}.` : '';
    const sc = v >= 13 ? sceneAt(v)(r, text.actions[r.verb!]?.does ?? '') : null; // guide /13: the cell's scene lines, said to the person
    if (sc) return sc.tells;
    if (v >= 14 && r.event === 'rivals-misled') return ANSWER_14.tells; // guide /14: the rival answers the Scout's tip, to her face
    const g102 = guard102(r, temp); if (g102) return g102; // brief 102: the guard's end of the call, rows no older run's text has
    switch (r.event) {
      case 'ack': if (v >= 12) return `Say what only ${who} would say as they take this order, now: how they take the job, a feeling or a detail the card can't give; never the order read back. Facts, on the order's card already and not to be read out: "${text.actions[r.verb!]?.does ?? ''}" ${ORDER[`${r.verb}/${r.outcome}`] ?? ORDER[r.verb!] ?? ''}`.trim();
        return v >= 4
        ? `Say: ${who} has the order and is on it, the act itself (${text.actions[r.verb!]?.label ?? r.verb}${r.outcome ? `, ${r.outcome}` : ''}) as ${who} would put it${ACK_SAY[r.verb!] ? `; worth adding where it fits: ${ACK_SAY[r.verb!]}` : ''}. Facts, on the order's card already and not to be read out: "${text.actions[r.verb!]?.does ?? ''}" ${ORDER[`${r.verb}/${r.outcome}`] ?? ORDER[r.verb!] ?? ''}`.trim()
        : `${who} has just been given this order and starts it now, walking over first if it is out of reach. ${verbLine(r.verb!)} ${ORDER[`${r.verb}/${r.outcome}`] ?? ORDER[r.verb!] ?? ''}`.trim();
      case 'arrive': if (v >= 12) return r.outcome === 'struggle' ? `Say: the person saw ${who} coming and fights back, a noisy struggle that people near may hear; his words as it starts. Facts, not to be read out: "${text.actions[r.verb!]?.does ?? ''}"`
          : `Say: ${who}'s first words to the person, face to face, opening the approach before the offer; the scene that follows plays the rest. Facts, not to be read out: "${text.actions[r.verb!]?.does ?? ''}"`;
        if (v >= 4) return `Say: ${who} is in reach and the act begins now${['ask', 'pay', 'help', 'pressure'].includes(r.verb!) ? ': the Fixer\'s first words to the person, face to face, the opening before the offer' : r.outcome === 'struggle' ? ': the person saw the Enforcer coming and fights back, a noisy struggle' : ''}. Facts, not to be read out: "${text.actions[r.verb!]?.does ?? ''}"`;
        return `${who} is in reach and the act begins now. ${verbLine(r.verb!)} ${['ask', 'pay', 'help', 'pressure'].includes(r.verb!) ? 'These are the Fixer\'s first words to the person, face to face: the opening, before the offer.' : r.outcome === 'struggle' ? 'The person saw the Enforcer coming and fights back: a noisy struggle.' : ''}`.trim();
      case 'done': if (v >= 12 && r.verb !== 'turn') return `Say: ${who}'s act has landed; what it means now, as ${who} would put it. Facts, not to be read out: ${DONE_12[`${r.verb}/${r.outcome}`] ?? DONE_12[r.verb!] ?? DONE[`${r.verb}/${r.outcome}`] ?? DONE[r.verb!] ?? ''}`.trim();
        return `${who}'s act has landed. ${DONE[`${r.verb}/${r.outcome}`] ?? DONE[r.verb!] ?? ''} ${r.verb !== 'turn' ? '' : v >= 5
        ? 'Said to the person\'s face after their reply, closing the approach (brief 085), then kept in the Journal: the Fixer\'s own short last word, first person or plain; no he or she.'
        : 'The Fixer\'s own terse report-back to the cell, first person or plain report, after the person\'s reply; no he or she.'}`.trim();
      case 'read': {
        const t = text.targets[r.about ?? ''], learned = r.verb === 'listen' ? (['guard', 'councillor'].includes(r.about!) ? `Listen taught who they are: their need (Intel shows it; it varies, so the line doesn't say it) and their temperament, ${r.temperament}.` : `Listen taught who they are: ${t?.heard ?? ''}`.replace('{temperament}', r.temperament ?? '').replace('{need}', 'cash'))
          : r.outcome === 'routine' ? `Watch taught its rounds: ${t?.routine}` : `Watch taught: ${t?.watched ?? 'their routine, where and when they go, and their mood'}`;
        return `${who} says what the cell just learned. ${learned}${r.about === 'waitress' && r.verb === 'listen' ? ' Inez owes a lender, and is secretly seeing the door guard.' : ''}${r.outcome === 'sight' ? ' Its rounds need more watching.' : ''}${temp}`;
      }
      case 'reply': if (v >= 13) return reply13(who, text.actions[r.verb!]?.label ?? r.verb!, r.outcome!, r.speaker, temp, verbLine(r.verb!));
        return `${who} answers the Fixer's ${r.verb}. ${verbLine(r.verb!)} The answer: ${OUTCOME[r.outcome!]}${temp} Plays as their bubble, and as their answer in the approach scene after the Fixer's offer.`;
      case 'confirm': return `The Fixer telephones Inez, turned: she says yes, she will call the guard over now.${temp} Voiced aloud.`;
      case 'taken': return `The Enforcer has hold of Popovic (${r.verb === 'enter' ? 'inside the Office' : 'at his car'}): his first words.${temp}`;
      case 'summon-refused': if (v >= 17 && r.outcome === 'summon-no-visit') return NO_VISIT_17[r.speaker] ?? NO_VISIT_17.guard!; // guide /17: as the engine plays it
        return `The Fixer's call can't work now: ${(v >= 18 ? SUMMON_18[r.outcome!] : undefined) ?? SUMMON[r.outcome!]}. ${r.speaker === 'waitress' ? 'Inez tells the Fixer on the telephone.' : 'The guard answers Inez, near enough to hear.'}${v >= 18 && r.speaker === 'guard' && r.outcome === 'summon-busy' ? ` ${GUARD_BUSY_18}` : ''} Nothing is spent.`; // guide /18: as the engine checks it
      case 'questioned': return `Later, an investigator questions ${who}: ${r.outcome === 'gives' ? 'they give the Fixer up, a face and what happened' : r.outcome === 'lies' ? 'they cover for the cell with a lie' : 'they know nothing useful'}. Not played in the Handover yet (investigators come later); a debrief hook.`;
      case 'fail': return `The attempt has failed: ${fail(r.outcome!)} The line plays for each of these, so every variant must be true whichever it was. ${SPEAKS[r.speaker] ?? ''} Spoken as it ends, before the end card.`;
      default: {
        if (v >= 18 && r.event === 'report-begins' && r.speaker === 'guard') return RADIO_18; // guide /18: the host plays it only for his report of one of the cell
        const o = r.outcome ? (v >= 13 && r.event === 'witness-reacts' ? REACTS_13[r.outcome] : undefined) ?? BY_OUTCOME[r.event]?.[r.outcome] ?? r.outcome : BY_OUTCOME[r.event]?.[''] ?? null;
        const base = (v >= 13 ? WORLD_13[r.event] : undefined) ?? (v >= 8 ? WORLD_8[r.event] : undefined) ?? WORLD[r.event] ?? ev?.trigger ?? r.event;
        const about = r.event === 'person-missed' ? (r.about === 'driver' ? ` ${BY_OUTCOME['person-missed']![r.temperament!] ?? 'Popovic'} at his car finds his driver gone.` : '') : '';
        return `${base}${o ? ` This one: ${o}.` : ''}${about} ${SPEAKS[r.speaker] ?? ''}${r.event !== 'person-missed' ? temp : ''}`.trim();
      }
    }
  };
  const where = (r: Row) => (v >= 13 ? sceneAt(v)(r, '')?.where : undefined) ?? (v >= 14 && r.event === 'rivals-misled' ? ANSWER_14.where : undefined) ?? (r.event === 'arrive' ? 'The Journal only: the moment itself plays in the scene.'
    : r.event === 'questioned' ? 'Not played yet: a debrief hook for when investigators run.' : ['win', 'fail', 'withdraw', 'cliffhanger'].includes(r.event) ? 'A bubble at the speaker as the game ends, then the Journal.'
      : 'A speech bubble at the speaker in play (3 s), then the Journal.');
  const told = (r: Row) => { const s = tells(r); return s[0]!.toUpperCase() + s.slice(1); };
  // guide /12: only the rows the host speaks under Fable's frequency are written; the silent ones keep their reason (short-voice.ts) for step 2's silent rows
  // guide /13 (brief 102): the agents are quiet in play, so a row marked `silent` in text.json is not written (its reason is there)
  const speech = v === 12 ? speech12(text, VoiceSheet.parse(JSON.parse(read(VOICE_SHEET))).speech) : null;
  // guide /14 (brief 102's sweep): a row of Sky's own words (`sky`) is not an item; its family's context gives it, so nobody repeats it
  // guide /17 (brief 102): a row of Fable's hand edits (`hand`) is not an item either; its family's context gives it as it gives Sky's
  // guide /18: except in a hand run, which scores the family as it plays, hand rows and all
  const outOfLoop = (r: Row) => v >= 17 && !!r.hand && !(v >= 18 && hand);
  const rows: Row[] = (text.dialogue as Row[]).filter(r => !r.parked && !r.pending && !speech?.silent(r as never) && !(v >= 13 && r.silent) && !(v >= 14 && r.sky) && !outOfLoop(r));
  const skys = (f: { pick: (r: Row) => boolean }) => v >= 14 ? (text.dialogue as Row[]).filter(r => r.sky && !r.silent && f.pick(r)) : [];
  const hands = (f: { pick: (r: Row) => boolean }) => (text.dialogue as Row[]).filter(r => outOfLoop(r) && !r.sky && !r.silent && f.pick(r));
  const said = (xs: Row[]) => xs.map(r => `${keyOf(r)}: ${r.lines.map(l => JSON.stringify(l)).join(' / ')}`).join('; ');
  const skyWords = (f: { pick: (r: Row) => boolean }) => (skys(f).length ? ` Sky's own words, already in the game and not items here: never rewrite them, and never say them in another row: ${said(skys(f))}.` : '')
    + (hands(f).length ? ` Rows Fable wrote by hand to the same bar (brief 102), already in the game and not items here: never rewrite them, and never say them in another row: ${said(hands(f))}.` : '');
  return familiesFor(v).filter(f => (!speech && v < 13) || rows.some(f.pick)).map(f => {
    const mine = rows.filter(f.pick);
    const items: ShortItem[] = mine.map(r => {
      const keep = v < 13 && r.event === 'done' && r.verb === 'turn' && r.outcome === 'refused' ? [r.lines[0]!] // /13: brief 102 took text.test.ts's pin on it
        : v >= 3 && r.event === 'driver-tail' ? r.lines.filter(l => /tailed us all week/.test(l)).slice(0, 1) : []; // /3: text.test.ts wants one line that says it
      const field: Field = { id: 'lines', form: 'speech', words: 12, chars: 80, count: r.lines.length, fills: [], patterns: patterns(r, v), keep };
      const item: ShortItem = { key: keyOf(r), where: speech?.when(r as never) ?? where(r), tells: told(r), speaker: `${r.speaker}: ${voices[r.speaker] ?? SPEAKS[r.speaker] ?? r.speaker}`, fields: [field], current: { lines: r.lines }, stays: stays(r, v) };
      return item;
    });
    if (!items.length) throw new Error(`${f.id} has no barks: update the families`);
    return { id: f.id, kind: 'bark' as const, title: f.title, context: v >= 13
      ? `${f.title}. Each item is one bark row of the mission's table (event / verb / speaker / outcome / temperament / about) that the host speaks, and its \`where\` says when and how often. Write its variants, each a different take on the moment in its speaker's voice. Keep a line of today's only where it already holds the bar; rewrite every line that explains the game (a rule, or what follows), reads the HUD back, or could come from anyone.${skyWords(f)}`
      : v >= 12
      ? `${f.title}. Each item is one bark row of the mission's table (event / verb / speaker / outcome / temperament / about) that the host speaks, and its \`where\` says when and how often. Write its variants, each a different take on the moment in its speaker's voice. Keep a line of today's only where it already earns its place in that voice; rewrite every line that reads the click back, narrates what the screen shows, talks mechanics, or could come from any of the four.`
      : v >= 4
      ? `${f.title}. Each item is one bark row of the mission's table (event / verb / speaker / outcome / temperament / about); write its variants, each a different way to say the same thing. Keep a line of today's that already does its job in its speaker's voice; rewrite only lines that are filler, untrue, off-voice, too long or repeated.`
      : `${f.title}. Each item is one bark row of the mission's table (event / verb / speaker / outcome / temperament / about); write its variants, each a different way to say the same fact. Today's lines are there to beat, not to keep.`, items };
  });
}
/** The text.json row a bark item writes. */
export const barkKey = keyOf;
