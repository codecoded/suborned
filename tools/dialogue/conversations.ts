// Brief 085: full conversation scenes, written through the RAG core at build time. A proposal for Sky to read: nothing here reaches the game; what Sky agrees is moved by hand into text.json scenes.conversations.
// Each scene: facts bound by code from the sim's people (sheet) + retrieved world and rules + the writer brief -> Fable writes takes -> a script-editor pass ->
// this file's checks (a fault goes back to the editor once) -> Motive reviews the set for fit with the rules. Every call is saved by runAgent under the run.
//   bun tools/dialogue/conversations.ts [--only id,id] [--review-also id,id] [--run r002] [--base dir] [--retrieve-only]   then: bun tools/dialogue/conversations-page.ts
// With --only, a scene's `after` scenes outside the list come from what Sky agreed (briefs/085/fable/approved.json), so the writer still knows what the player has read.
import { createHash } from 'node:crypto';
import { existsSync, mkdirSync, readdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { z } from 'zod';
import { runAgent, type Call, type Objective } from '../rag/agent';
import { root, writeJson } from '../rag/config';
import { hash, loadCorpus } from '../rag/corpus';
import { retrieve } from '../rag/retrieval';
import { CONVERSATION_MAX_BEATS, CUTAWAY_MAX_LINE, CUTAWAY_MAX_QUESTIONS, CUTAWAY_MAX_SENTENCES, CUTAWAY_MAX_WORDS, lineWords, questionCount, sentenceCount } from '../../contracts/missions/handover/conversations.schema.ts';

const read = (p: string) => Bun.file(resolve(root, p)).text();
const peopleFile = 'sim/data/handover.people.json', textFile = 'contracts/missions/handover/text.json';
const people = JSON.parse(await read(peopleFile)), text = JSON.parse(await read(textFile));
const approved = JSON.parse(await read('briefs/085/fable/approved.json')) as { scenes: { id: string; beats: [string, string][] }[] };
const person = (id: string) => people.people.find((p: { id: string }) => p.id === id);
const recordLabel = (r: string) => people.needs.records[r]?.[1] ?? r;
const place = (id: string) => (people.places as Record<string, [number, number]>)[id]!;
const metres = (a: string, b: string) => { const [x, y] = [place(a), place(b)]; return Math.round(Math.hypot(x[0] - y[0], x[1] - y[1]) / 100); };

/** One person's facts, in words, bound from the sim's data so a sheet can't drift from the game. */
function who(id: string, role: string, also = ''): string {
  const p = person(id);
  return `- ${role}: ${p.name}. Job: ${p.job}. Temperament (show it, never name it): ${p.temperament}. Owes or lacks: ${p.need}, on record as "${recordLabel(p.record)}".${also ? ` ${also}` : ''}`;
}
const INEZ = 'p-cafe-14', GUARD = 'p-records-office-01', DRIVER = 'p-records-office-31', MILENA = 'p-cafe-12';
const apart = metres('cafe-entrance', 'office-entrance');
/** Who Inez is, past the one word the sim holds (Sky, 2026-10-31: "not something Inez would say"). */
const INEZ_WHO = who(INEZ, 'Inez', `She looks at who can see her before she looks at you. She talks about the shift, the terrace and how she can't keep up. She has no money to offer anyone and never offers any. She never says she's frightened and never threatens anyone: her fear shows in her questions, and in giving way too fast. She's kind, and tired. With people she likes she's quick and a little funny about being tired.`);
const WORLD = `The place: the café and its terrace face Records Square; the Civic Records Office is across the square. Daylight, shift change coming. A polite, orderly city that writes everything down. Records are paper: stamped cards, forms, pneumatic tubes. Calls are voice, by telephone. Robots follow fixed routines. Everyone in the scene is a person of this city except the cell.
What the player sees (the game's scene, §10, brief 085: Sky, "unattached to the reality of the game"): a close shot of the speakers standing idle, facing each other, feet planted. Nobody sits, walks, carries, holds, hands over, points at or touches anything, and nothing arrives. No tray, plate, cup, card, letter, envelope or money is in the shot. They may talk about things that are elsewhere, but no line points at anything in the shot or at something being done now.
Only what the game does happens: the cell's money moves as the action's price, away from the shot. Nobody else pays, bribes, trades or bargains for anything, and nobody gains anything the facts here don't give. Don't say evening or tonight.`;
/** Off his door from the walk over to the walk back (GDD §07: about 57 s, 60 s, 57 s). Players plan on this, so "a minute" is wrong. */
const GUARD_AWAY = `He leaves the door, walks across the square, stands at her side and walks back: the door is empty about three minutes. In speech say "a few minutes", never "a minute".`;
/** Sky, 2026-10-31: asking him over "feels unrealistic without a reason", and if she said someone knew, "he runs back to the door". */
const GUARD_REASON = `He only leaves his post for a reason: she says she has something to give him or tell him that can't wait and can't be said on the line. It's her reason, in her own words, and it's enough to keep him at her side. She never tells him anyone knows about them: he'd run straight back to his door.`;
/** GDD v0.34.0 §07 (Sky, brief 085): a pressured Inez, and only she, tells him. She still needs a reason to get him off the door. */
const GUARD_REASON_THEN_TELLS = `He only leaves his post for a reason: she says she has something to tell him that can't wait and can't be said on the line. It's her reason, in her own words, and he steps off the door for it.`;
const VOICES = (...k: string[]) => k.map(v => `- ${v}: ${text.tone.voices[v]}`).join('\n');
const INEZ_GUARD = `Inez and the door guard are lovers, in secret. He stands the Office's public door, ${apart} m across the square from her. The two are never together unless she calls him. In speech call him Arthur, his given name: it is what Inez calls him, and the cell knows him by what it heard from her (never his surname, and no gendered word beyond "he").`;

type Spec = { id: string; title: string; after?: string[]; kind: 'learn' | 'turn' | 'call'; maxBeats: number; speakers: string[]; query: string[]; sheet: string; mustSay: { what: string; re: RegExp }[] };
export const specs: Spec[] = [
  {
    id: 'listen-inez', title: 'Listen to Inez', kind: 'learn', maxBeats: CONVERSATION_MAX_BEATS, speakers: ['Inez', 'Milena', 'Scout'],
    query: ['Inez door guard lovers debt affair Help Pressure secret', 'Fearful temperament hesitates Listen reveals need secret', 'world records paper calls voice orderly city fears disorder', 'conversation scene overheard Listen'],
    mustSay: [{ what: 'the Bureau loan', re: /bureau/i }, { what: 'the guard', re: /arthur|guard/i }],
    sheet: `${WORLD}
Situation: the Scout has stood two metres away, between the palms, for half a minute. The player hears what the Scout hears. Inez is standing at the café door between tables. Milena is a voice through the open door, from just inside. Neither knows the Scout is listening.
${INEZ_WHO}
${who(MILENA, 'Milena', 'Works the same café, inside this shift. No debt or secret of hers comes up: she never competes with Inez\'s trouble, she asks about it and is sorry for her (Sky, 2026-11-05: "more empathise"). She\'s fond of Inez: she worries whether she\'s eaten, and won\'t tell.')}
${INEZ_GUARD} Milena has noticed where Inez keeps looking; she doesn't know for sure until this talk.
The scene must make plain, by its end: Inez is a waitress at this café; she owes the Bureau and can't keep up; she is seeing the guard on the Office door; if the Office hears of it, it costs them both; she is nervous by nature.
Last beat: the Scout, low, alone, in one or two full sentences: a plain report of what she heard (the debt; Arthur). Nothing about what the cell could do: the cards after the cut say it (Sky, 2026-11-05: "Fables rewrite is better").
Speakers: Inez, Milena, Scout.
learned: 2 lines (the debt; the lover). opens: "Help" and "Pressure" (both are now open on Inez; they cost what the ring says).
${VOICES('waitress', 'scout')}`,
  },
  // Engine 0.44.0 (Sky, 2026-11-05, option 1): a Help before the Scout's Listen is a small offer everyone refuses, no money paid ($0, 35 s, +1 T, a remembered
  // refusal); after the Listen a Fearful person takes the full offer once and is turned. help-inez-2-known is the full offer after she refused a small one (helped).
  {
    id: 'help-inez-1', title: 'Help Inez, a small offer', kind: 'turn', maxBeats: CONVERSATION_MAX_BEATS, speakers: ['Inez', 'Fixer'],
    query: ['Help small offer refused before Listen need told not heard', 'Fearful temperament refuses remembered refusal can identify Fixer', 'approach conversation private no witnesses', 'world paper records optimistic surface coercive underneath'],
    mustSay: [{ what: 'the Bureau', re: /bureau/i }, { what: 'that a little would not free her', re: /month or two|not enough|back where/i }, { what: 'that she won\'t take it', re: /keep your money|won't take|can't take/i }],
    sheet: `${WORLD}
Situation: before the Scout has listened. The cell knows only the briefing's word, that Inez is in debt; it doesn't know to whom she owes, how much or how far behind she is. The Fixer comes to Inez at the café door and offers her a little money toward what she owes, without being able to say how much is owed. Fact of the game: she refuses. No money changes hands, and nothing is paid at the Bureau. Why, in her own words: a little would buy a month, and then she'd owe a stranger as well as the Bureau; nobody gives money for nothing.
${INEZ_WHO}
Speakers: Inez, Fixer. She has never seen the Fixer. Her fear shows in her questions and in wanting the Fixer gone before anyone asks who that was. She's polite, and firm in the end.
Fact of the game, for learned only, and nobody says it aloud: she will remember the Fixer's face. She makes no threat.
The Fixer is warm, says "we", doesn't push past a polite no, and takes the refusal well. Nobody says what the cell should do next: the cards do.
learned: 2 lines (she refused, too little; she knows the Fixer's face). opens: 1 line: Listen first, to learn what she owes.
${VOICES('waitress', 'fixer')}`,
  },
  {
    id: 'help-inez-2', title: 'Help Inez, the full offer', after: ['listen-inez'], kind: 'turn', maxBeats: CONVERSATION_MAX_BEATS, speakers: ['Inez', 'Fixer'],
    query: ['Help clear debt gratitude bravery helped Inez warns rivals', 'Inez call guard walks to her side partners stays 60 s', 'capstone the call that pulls the guard off his door'],
    mustSay: [{ what: 'calling him over', re: /call|come over|fetch|over here|send for/i }, { what: 'the guard', re: /arthur|guard|him/i }, { what: 'the whole debt', re: /all of it|the whole|everything/i }],
    sheet: `${WORLD}
Situation: after the Scout's Listen, so the cell knows what she owes the Bureau and how far behind she is. The Fixer comes to Inez at the café door and gives her enough to clear the whole loan: the money is hers, and she owes nobody for it (nobody goes to the Bureau; nothing is stamped). This one visit wins her over, gratefully and still frightened. Then the Fixer says what's wanted, and it is small: when the Fixer telephones, she calls Arthur over from his door. ${GUARD_AWAY} ${GUARD_REASON}
${INEZ_WHO}
${INEZ_GUARD}
She asks how the Fixer knows what she owes; the Fixer doesn't explain beyond that people talk and she listens. She must work out for herself that the Fixer knows about Arthur, and be frightened by that; the Fixer is kind about it and makes no threat. She asks what happens to him. The honest answer: nothing happens to him if this goes right, and he never sees us.
Fact of the game: because she was helped, she will warn the cell if she sees trouble coming on the square. Let her offer that, in her own frightened way, at the end.
The scene must make plain: she has all she owes, and it's hers; she's with us now; when we say so, she calls Arthur off the door.
learned: 1 line. opens: "Call Inez".
${VOICES('waitress', 'fixer')}`,
  },
  {
    id: 'help-inez-2-known', title: 'Help Inez again, the full offer', after: ['help-inez-1', 'listen-inez'], kind: 'turn', maxBeats: CONVERSATION_MAX_BEATS, speakers: ['Inez', 'Fixer'],
    query: ['Help clear debt after a refused small offer remembered refusal', 'Inez call guard walks to her side partners stays 60 s', 'capstone the call that pulls the guard off his door'],
    mustSay: [{ what: 'that she knows the Fixer', re: /you again|before|last time/i }, { what: 'calling him over', re: /call|come over|fetch|over here|send for/i }, { what: 'the guard', re: /arthur|guard|him/i }],
    sheet: `${WORLD}
Situation: as help-inez-2 (the full offer after the Listen, the whole debt, hers; she's won over and agrees to call Arthur off his door), but she refused this same Fixer's small offer earlier and recognises her at once. The first lines meet that: she said no to a little; this time it's everything. From the ask on, the scene is help-inez-2's. ${GUARD_AWAY} ${GUARD_REASON}
${INEZ_WHO}
${INEZ_GUARD}
learned: 1 line. opens: "Call Inez".
${VOICES('waitress', 'fixer')}`,
  },
  {
    id: 'pressure-inez', title: 'Pressure Inez', after: ['listen-inez'], kind: 'turn', maxBeats: CONVERSATION_MAX_BEATS, speakers: ['Inez', 'Fixer'],
    query: ['Pressure threat fear resentment can identify Fixer seen', 'Inez secret affair Pressure threatens to expose', 'capstone the call that pulls the guard off his door'],
    mustSay: [{ what: 'the affair exposed', re: /office|tell|know|hear/i }, { what: 'calling him over', re: /call|come over|fetch|over here|send for/i }],
    sheet: `${WORLD}
Situation: the same woman, a different way in. The Fixer comes to Inez at the café door with no money and tells her, gently, that the Office could learn about her and Arthur. She gives in quickly, and hates it. The Fixer is warm in manner, steel underneath, takes no pleasure in it and doesn't pretend otherwise. The Fixer keeps her voice low because a threat can be seen. What's wanted: when the Fixer telephones, she calls Arthur over from his door. ${GUARD_REASON_THEN_TELLS} She will warn him when he arrives, and the door is clear about 1:40, so nobody in this scene says how long he's away.
${INEZ_WHO}
${INEZ_GUARD}
Fact of the game, for learned only, and nobody says it aloud: she will remember the Fixer's face, and could describe it if she's ever questioned. She wouldn't threaten a stranger who frightens her, and the Fixer wouldn't remind her.
The Fixer stays good company all through: gentle, unhurried, kind about her shift in the same breath as the threat. No raised voice and no cruelty, and that's what frightens. By the end the threat is plain.
The scene must make plain, the cards included: we threatened to expose the two of them; she agreed, and hates it, and never says she's scared; when we say so, she calls Arthur off the door.
learned: 1 line, and it mentions she knows the Fixer's face. opens: "Call Inez".
${VOICES('waitress', 'fixer')}`,
  },
  {
    id: 'pay-driver', title: 'Pay the driver', kind: 'turn', maxBeats: CONVERSATION_MAX_BEATS, speakers: ['Lukac', 'Fixer'],
    query: ['route B pickup driver turned Pay service bay waits', 'official driver needs cash Mercenary Pay', 'getaway rivals lookout turned driver warns lookout off', 'world paper records voice calls'],
    mustSay: [{ what: 'the service bay', re: /service bay|round the back|service drive|round to the/i }, { what: 'the Councillor', re: /councillor/i }],
    sheet: `${WORLD}
Situation: the Fixer finds Popovic's official driver waiting by the official car in the bay on the Office's south side, on Apron Road. Lukac has never seen her. She opens as a stranger who'd rather stay one, says what she has heard (his wages are late again, and waiting costs him: what the Scout's Listen overheard), and offers work for a driver, paid now; then the job (Sky, 2026-11-05: it mustn't open on "it's paid" with no idea what or why). The game pays it away from the shot, so no envelope, and nobody says the amount: the Pay card shows it. It works. The driver has had a lean month and presses on the terms of the job, never the money, because this is someone who's been paid to be vague before and wants it spelled out; Lukac takes the money last.
${who(DRIVER, 'Lukac', 'Call the driver Lukac, his surname: the Fixer matches his brisk way and adds no Mr; no gendered word. Calls Popovic "the Councillor", always.')}
The job, exactly as the game plays it: when the chime goes the Councillor comes out and gets in. A paid Lukac doesn't drive off. Lukac takes the Councillor round to the service bay at the back, the east yard, and waits there with him. Lukac won't wait for ever: a couple of minutes, then drives on. Lukac also says that if someone's hanging about watching the bay, Lukac can send them off; Lukac has noticed nothing specific and names nobody. What happens to the Councillor at the bay is the Fixer's people's business; Lukac doesn't want to see it.
The Fixer says "we" and never explains who "we" are. Lukac doesn't ask, and says why not, in a line.
The Fixer likes Lukac, and it shows once.
The scene must make plain: Lukac is the Councillor's driver and is short of money; we paid; at the chime the Councillor's car goes to the service bay, not home, and waits a short while; that's where to be.
learned: 1 line. opens: nothing new to press; instead 1 line naming the place to be ("Be at the service bay").
${VOICES('driver', 'fixer')}`,
  },
  {
    id: 'call-inez', title: 'The call, after Help', after: ['listen-inez', 'help-inez-2'], kind: 'call', maxBeats: CONVERSATION_MAX_BEATS, speakers: ['Fixer', 'Inez', 'Arthur'],
    query: ['capstone the call pulls guard off door partners ring side stays 60 s', 'route A front door Call Inez window Enforcer enters', 'calls are voice telephone'],
    mustSay: [{ what: 'he leaves the door', re: /door|post|step/i }],
    sheet: `${WORLD}
Situation: the moment the whole front-door plan was for. The Fixer telephones the café and says it's time. Inez, whom the cell has helped, then gets Arthur to leave his door: she calls across to him on the Office door's handset. ${GUARD_AWAY} ${GUARD_REASON} The shot cuts between them, each standing alone; no one is seen holding a telephone.
${INEZ_WHO}
${who(GUARD, 'Arthur', 'In speech he is Arthur to the Fixer and to Inez; Inez may call him "love". He has a shift to finish and likes her more than the job. He says nothing about money here, and doesn\'t say how long the door stands empty.')}
${INEZ_GUARD}
They're glad of each other. He's fond, a little clumsy, and pleased to be asked. He ends the scene happy to come, knowing nothing.
She is scared and must sound ordinary to him. He must not suspect. Tenderness matters: this is two people who love each other, and one of them is using it. Let the player feel that, in a line. No coffee spill: he comes because she asked and gave him a reason.
The scene must make plain: she called him and he is leaving the door now. The card says how long.
learned: 1 line ("Door empty a few minutes"). opens: 1 line naming what the door allows now, "Enter the Office" (the Enforcer's action).
${VOICES('waitress', 'guard', 'fixer')}`,
  },
  {
    id: 'call-inez-pressure', title: 'The call, after Pressure', after: ['listen-inez', 'pressure-inez'], kind: 'call', maxBeats: CONVERSATION_MAX_BEATS, speakers: ['Fixer', 'Inez', 'Arthur'],
    query: ['pressured Inez tells the guard someone knows, they looked suspicious, he runs back to his door, window shorter', 'Pressure leaves fear resentment can identify Fixer', 'route A front door Call Inez window Enforcer enters', 'calls are voice telephone'],
    mustSay: [{ what: 'the reason she gives him', re: /tell you|can't wait/i }, { what: 'that he is coming', re: /coming/i }],
    sheet: `${WORLD}
Situation: the moment the front-door plan was for. The Fixer threatened to tell the Office about Inez and Arthur, and told her not to say a word of it to him. Now the Fixer telephones the café and says it's time. Inez then speaks to Arthur on the Office door's handset. The shot cuts between them, each standing alone; no one is seen holding a telephone. He doesn't arrive in this scene.
${INEZ_WHO}
${who(GUARD, 'Arthur', 'In speech he is Arthur to the Fixer and to Inez; Inez may call him "love". He has a shift to finish and likes her more than the job. He says nothing about money here, and doesn\'t say how long the door stands empty.')}
${INEZ_GUARD}
It's the same woman the player has just read, and she hates the Fixer. She answers the Fixer as if she's giving way, as she did before. She talks to Arthur the way she always does, an ordinary word about his shift. ${GUARD_REASON_THEN_TELLS} The scene ends with him coming.
They're glad of each other, here too: he's fond, a little clumsy, pleased to hear her, and she's fond of him and sick about it. With the Fixer she has no warmth left, and Arthur's as kind as ever.
Fact of the game, for the cards only: when he reaches her, in play, she tells him someone knows about them and that they looked suspicious, and he runs back to his door, so the door is clear for a much shorter time (Motive: he walks over, stands 5 s with her, runs back). Those two lines aren't in this scene and nothing in it says so aloud: she can't tell him here, because he hasn't walked over yet.
learned: 1 line: she'll warn him. opens: 1 line truthful about the shorter window and the entrance camera, which still stops an Enter until it's looped or off.
${VOICES('waitress', 'guard', 'fixer')}`,
  },
];

const Beat = z.strictObject({ speaker: z.string(), line: z.string() });
const Take = z.strictObject({ situation: z.string().describe('one line: what makes this take different from the others'), beats: z.array(Beat), learned: z.array(z.string()), opens: z.array(z.string()) });
const Takes = z.strictObject({ takes: z.array(Take).length(3) });
const Edited = z.strictObject({ picked: z.number().int().min(1).max(3), why: z.string(), changes: z.array(z.string()).describe('what you changed and why, at most 4'), beats: z.array(Beat), learned: z.array(z.string()), opens: z.array(z.string()) });
const Review = z.strictObject({ faults: z.array(z.strictObject({ scene: z.string(), line: z.string(), problem: z.string(), fix: z.string() })), notes: z.string() });
type Final = z.infer<typeof Edited>;

const NAMES_FEAR = /\bI'?m (too |so |very )?(scared|frightened|terrified)\b|\bI'?m (too |so |very )?afraid of\b/i;
/** Sky on r001: things no one has in the shot, Inez paying her way out, and the guard's "one minute". */
const NOT_IN_THE_GAME = /\b(saucer|plate|tray|cup|envelope|bonnet|soup|tonight|evening)\b|\b(take|have|keep) (my|your|her) tips\b/i;
const ONE_MINUTE = /\b(a|one) minute\b/i;
const JARGON = /\b(alert|trail|standing|heat|turned|temperament|approach|lever|mission|agent|asset|halcyon|lender)\b/i;
const avoid: string[] = [...text.tone.avoid, ...text.tone.profanity];
const words = lineWords;
/** Sky, 2026-11-05: nobody explains the game; the cards after the cut say what's now open. A mouth listing options, or naming the actions as moves, fails. */
export const TUTORIAL = /\bwe could\b[^.?]*\bor\b|\byou can now\b|\b(?:we|you) (?:could|can) (?:now )?(?:help|pressure|pay|call|listen to|watch|mislead|use)\b/i;
/** Sky, 2026-11-05: "We give money to Inez, not go and pay at the Bureau". The cell hands the money over; nobody pays at a counter, stamps or files. */
export const PAYS_AT_BUREAU = /\bstamp(?:ed|s|ing)?\b|\bfiled\b|\b(?:at|with) the bureau\b|\bowe the bureau nothing\b|\b(?:paid|pay|settled?|cleared?) (?:part of |off )?(?:your|her|the) (?:bureau )?(?:loan|debt)\b|\bthe rest paid\b/i;
/** The checks a scene must pass. Empty = pass. Line limits are the cutaway's (conversations.schema.ts, brief 096); the game's own lines (cards) keep 6 words. */
export function check(s: Spec, f: Pick<Final, 'beats' | 'learned' | 'opens'>): string[] {
  const out: string[] = [], all = f.beats.map(b => b.line).join(' ');
  if (f.beats.length < 4 || f.beats.length > s.maxBeats) out.push(`${f.beats.length} beats: must be 4 to ${s.maxBeats}`);
  for (const b of f.beats) {
    if (!s.speakers.includes(b.speaker)) out.push(`speaker "${b.speaker}" isn't in the scene (${s.speakers.join(', ')})`);
    if (words(b.line) > CUTAWAY_MAX_WORDS) out.push(`over ${CUTAWAY_MAX_WORDS} words: "${b.line}"`);
    if (b.line.length > CUTAWAY_MAX_LINE) out.push(`over ${CUTAWAY_MAX_LINE} characters: "${b.line}"`);
    if (sentenceCount(b.line) > CUTAWAY_MAX_SENTENCES) out.push(`over ${CUTAWAY_MAX_SENTENCES} sentences: "${b.line}"`);
    if (questionCount(b.line) > CUTAWAY_MAX_QUESTIONS) out.push(`asks more than one thing: "${b.line}"`);
    if (TUTORIAL.test(b.line)) out.push(`explains the game; the cards do that: "${b.line}"`);
    if (PAYS_AT_BUREAU.test(b.line)) out.push(`pays at the Bureau; the cell gives the money to her: "${b.line}"`);
    if (b.line.includes('!')) out.push(`exclamation mark: "${b.line}"`);
    if (JARGON.test(b.line)) out.push(`design word or "lender": "${b.line}"`);
    if (NOT_IN_THE_GAME.test(b.line)) out.push(`isn't in the game's shot or acts: "${b.line}"`);
    if (ONE_MINUTE.test(b.line)) out.push(`the door is empty a few minutes, not one: "${b.line}"`);
    if (NAMES_FEAR.test(b.line)) out.push(`names the fear instead of showing it: "${b.line}"`);
    for (const a of avoid) if (new RegExp(`\\b${a.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}\\b`, 'i').test(b.line)) out.push(`avoid-list word "${a}": "${b.line}"`);
  }
  for (const l of [...f.learned, ...f.opens]) if (words(l) > 6) out.push(`game line over 6 words: "${l}"`);
  for (const m of s.mustSay) if (!m.re.test(all)) out.push(`never says: ${m.what}`);
  return out;
}

type Read = { id: string; title: string; beats: { speaker: string; line: string }[] };
/** What the player has read before a scene, in its `after` order: the scenes this run wrote, else what Sky agreed. */
export function priorScenes(s: Pick<Spec, 'after'>, written: Read[]): Read[] {
  return (s.after ?? []).flatMap((id): Read[] => {
    const w = written.find(x => x.id === id); if (w) return [w];
    const a = approved.scenes.find(x => x.id === id); if (!a) return [];
    return [{ id, title: specs.find(x => x.id === id)?.title ?? id, beats: a.beats.map(([speaker, line]) => ({ speaker, line })) }];
  });
}

const args = Bun.argv.slice(2), arg = (k: string) => { const i = args.indexOf(k); return i >= 0 ? args[i + 1] : undefined; };
if (import.meta.main) {
  const only = arg('--only')?.split(','), also = arg('--review-also')?.split(',') ?? [], base = arg('--base') ?? 'runs/conversations/085';
  mkdirSync(resolve(root, base), { recursive: true });
  const taken = readdirSync(resolve(root, base)).filter(n => /^r\d+$/.test(n)).length;
  const run = arg('--run') ?? `r${String(taken + 1).padStart(3, '0')}`, dir = `${base}/${run}`, start = Date.now();
  const chosen = specs.filter(s => !only || only.includes(s.id));
  const corpusHash = hash(JSON.stringify(await loadCorpus()));
  const allCalls: Call[] = [];
  if (args.includes('--retrieve-only')) { // no model: what each scene would be given, saved as evidence of what the corpus holds
    for (const s of chosen) { const r = await retrieve(s.query, 8); await writeJson(`${dir}/${s.id}.retrieval.json`, r); console.log(`${s.id}: ${r.hits.map(h => h.section.slice(0, 70)).join(' | ')}`); }
    process.exit(0);
  }

  type Scene = { spec: Spec; final: Final; faults: string[]; reviewFix: (found: string[], into: Call[]) => Promise<void> };
  const one = async (s: Spec, before: Scene[]): Promise<Scene> => {
    const d = `${dir}/${s.id}`, calls: Call[] = [];
    const seen = priorScenes(s, before.map(b => ({ title: b.spec.title, id: b.spec.id, beats: b.final.beats })));
    const story = seen.length ? `What the player has already read, in order. Stay true to it, and don't reuse its phrases:\n${seen.map(b => `${b.title}:\n${b.beats.map(x => `${x.speaker}: ${x.line}`).join('\n')}`).join('\n\n')}\n\n` : '';
    const o: Objective = { id: `write-${s.id}`, role: 'dialogue-designer', brief: 'briefs/085/fable/writer.md', query: s.query, effort: 'high' };
    const task = `Scene: ${s.title}. At most ${s.maxBeats} beats.\n\nFacts (these win over any passage above):\n${s.sheet}\n\n${story}Write three takes that differ in situation and shape, not in wording. Each returns beats (speaker, line), learned, opens.`;
    const takes = await runAgent(o, task, Takes, calls, d);
    await writeJson(`${d}/takes.json`, takes);
    const e: Objective = { id: `edit-${s.id}`, role: 'dialogue-designer', brief: 'briefs/085/fable/editor.md', query: s.query.slice(0, 2), effort: 'high' };
    const edTask = (extra = '') => `Scene: ${s.title}. At most ${s.maxBeats} beats.\n\nFacts:\n${s.sheet}\n\n${story}The writer's hard limits: ${(WRITER_LIMITS)}\n\nThe takes:\n${JSON.stringify(takes.takes, null, 1)}\n\n${extra}Return the finished scene.`;
    let final = await runAgent(e, edTask(), Edited, calls, d), faults = check(s, final);
    if (faults.length) { final = await runAgent({ ...e, id: `fix-${s.id}` }, edTask(`Your last version:\n${JSON.stringify(final.beats)}\nIt failed these checks; fix them and change nothing else:\n${faults.join('\n')}\n\n`), Edited, calls, d); faults = check(s, final); }
    await writeJson(`${d}/final.json`, { id: s.id, title: s.title, kind: s.kind, ...final, faults });
    allCalls.push(...calls);
    console.log(`${s.id}: ${final.beats.length} beats, ${final.beats.reduce((n, b) => n + words(b.line), 0)} words, ${faults.length} faults`);
    const reviewFix = async (found: string[], into: Call[]) => { // the rules review's line faults go back to the editor once, then the checks again
      const next = await runAgent({ ...e, id: `review-fix-${s.id}` }, edTask(`Your last version:\n${JSON.stringify(final.beats)}\nThe rules review found these faults; fix them and change nothing else:\n${found.join('\n')}\n\n`), Edited, into, d);
      const left = check(s, next);
      if (left.length) { console.log(`${s.id}: review fix kept out, it fails ${left.join('; ')}`); return; }
      await writeJson(`${d}/final.json`, { id: s.id, title: s.title, kind: s.kind, ...next, faults: left });
      console.log(`${s.id}: ${found.length} review faults sent back and fixed`);
    };
    return { spec: s, final, faults, reviewFix };
  };
  const WRITER_LIMITS = (await read('briefs/085/fable/writer.md')).split('## Hard limits')[1]!.trim();
  // In story order, so each scene knows what the player has read; Pressure and the driver don't wait on the Help scenes.
  const started = new Map<string, Promise<Scene>>();
  const play = (s: Spec): Promise<Scene> => {
    if (!started.has(s.id)) started.set(s.id, Promise.all((s.after ?? []).flatMap(id => chosen.filter(c => c.id === id)).map(play)).then(before => one(s, before)));
    return started.get(s.id)!;
  };
  const done = await Promise.all(chosen.map(play));

  const reviewCalls: Call[] = [];
  const review = await runAgent(
    { id: 'review-fit', role: 'systems-designer', brief: 'briefs/085/fable/review.md', query: ['Inez Fearful Help Pressure Pay approaches', 'route B driver turned service bay rivals lookout', 'capstone partners call guard stays', 'conversation scene clock pauses'], effort: 'medium' },
    `The scenes:\n${JSON.stringify([...done.map(x => ({ scene: x.spec.id, facts: x.spec.sheet, beats: x.final.beats, learned: x.final.learned, opens: x.final.opens })), ...also.map(id => { const sc = text.scenes.conversations.items.find((i: { id: string }) => i.id === id); return { scene: id, facts: specs.find(s => s.id === id)!.sheet, beats: sc.beats.map((b: { speaker: string; line: string }) => ({ speaker: b.speaker, line: b.line })), learned: sc.learned, opens: sc.opens }; })], null, 1)}`,
    Review, reviewCalls, `${dir}/review`,
  );
  await writeJson(`${dir}/review/review.json`, review);
  allCalls.push(...reviewCalls);
  await Promise.all(done.map(x => {
    const found = review.faults.filter(f => f.scene === x.spec.id).map(f => `"${f.line}": ${f.problem} Fix: ${f.fix}`);
    return found.length ? x.reviewFix(found, allCalls) : undefined;
  }));

  const sha = async (p: string) => createHash('sha256').update(await read(p)).digest('hex');
  await writeJson(`${dir}/run.json`, {
    run, brief: '085', note: 'Proposal for Sky to read; nothing here reaches text.json or the game.', corpusHash, git: Bun.spawnSync(['git', 'rev-parse', 'HEAD'], { cwd: root }).stdout.toString().trim(),
    inputs: { [peopleFile]: await sha(peopleFile), [textFile]: await sha(textFile), 'briefs/085/fable/writer.md': await sha('briefs/085/fable/writer.md'), 'briefs/085/fable/editor.md': await sha('briefs/085/fable/editor.md'), 'briefs/085/fable/approved.json': await sha('briefs/085/fable/approved.json') },
    calls: allCalls.map(c => ({ id: c.id, role: c.role, model: c.model, status: c.status, seconds: c.seconds, costUsd: c.costUsd, hits: c.retrieval.hits.length })),
    callCount: allCalls.length, wallSeconds: Math.round((Date.now() - start) / 1000), modelSeconds: allCalls.reduce((n, c) => n + c.seconds, 0),
    faults: done.flatMap(x => x.faults.map(f => `${x.spec.id}: ${f}`)), reviewFaults: review.faults.length,
  });
  console.log(`${run}: ${allCalls.length} calls, ${Math.round((Date.now() - start) / 1000)} s; review faults ${review.faults.length}`);
  if (existsSync(resolve(root, dir))) console.log(`saved under ${dir}`);
}
