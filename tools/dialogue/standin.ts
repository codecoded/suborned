// Stand-in tables, made by code: what a preview answers for Fable (no model call), and the fixture the tests expand (brief 012).
// They are not good words; they are tables the checker accepts, so a run can be tried end to end offline. Where the Handover's hand-written
// text has the same situation for a group (fixture.ts), its lines are used, so the checker also runs on the reference. The stand-in needs the
// lexicon to exist, so a group or scenes stand-in is made with the stand-in lexicon unless a real one is given.
import type { Cell, Group, Lexicon, SceneT, Voices } from '../../contracts/dialogue/voices.schema';
import type { TextFile } from '../../contracts/missions/handover/text.schema';
import { referenceOf } from './fixture';
import { debriefFields, lexiconNeeds, TEMPERAMENTS, type DialogueRules, type PeopleRules, type Rulebook } from './rules';
import { keyOf, requiredCells, requiredScenes, type Key, type SceneKey } from './situations';

const ADDRESS: Record<string, string[]> = {
  counter: ['love', 'honey', 'friend'], workshop: ['pal', 'chum', 'friend'], resident: ['neighbour', 'friend', 'stranger'], clerk: ['citizen', 'friend', 'stranger'],
  security: ['citizen', 'stranger', 'friend'], driver: ['pal', 'chum', 'stranger'], official: ['citizen', 'friend', 'stranger'],
};
const TAG: Record<string, string> = { counter: 'at the counter', workshop: 'in the back', resident: 'on my street', clerk: 'at my desk', security: 'on my post', driver: 'in my car', official: 'in my chamber' };
const OPEN: Record<string, string[]> = {
  idealist: ['For the block', 'For my neighbours', 'For what is right', 'For the street'], mercenary: ['For a price', 'Money talks', 'For the right fee', 'Name it'],
  fearful: ['Please', 'Oh no', 'Quietly now', 'Easy now'], loyal: ['By the book', 'Rules are rules', 'On my honour', 'As ordered'], none: ['Well', 'Look', 'Listen', 'Mind'],
};
const VERB: Record<string, string> = { ask: 'A favour:', pay: 'Money:', help: 'Help:', pressure: 'Threats:' };
const OUTCOME: Record<string, string> = { turned: 'yes', refused: 'no', 'not-yet': 'not yet', kept: 'still yes', suspicious: 'reported', gives: 'I saw it all', lies: 'I saw nothing', nothing: 'I forget' };
const ORD = ['first', 'second', 'third', 'fourth'];
const MANNER: Record<string, string> = { idealist: 'kindly', mercenary: 'sharply', fearful: 'shakily', loyal: 'firmly', none: 'plainly' };
const TIE: Record<string, string> = { workmate: 'the shift', regular: 'the usual', family: 'the family', debtor: 'the debt', rival: 'the rival', lover: 'the secret', none: 'the day' };

/** A filler template for a cell and a variant: unique over the scope (temperament, verb, outcome, variant and the group's own words differ). */
function filler(k: Key, group: string, v: number): string {
  const open = OPEN[k.temperament ?? 'none']![v]!, tag = TAG[group]!;
  if (k.event === 'reply') return `${open}, {address}. ${VERB[k.verb!]} ${OUTCOME[k.outcome!]} ${tag}.`;
  if (k.event === 'confirm') return `${open}, {address}. Calling now ${tag}.`;
  if (k.event === 'questioned') return `${open}, {address}. ${OUTCOME[k.outcome!]} ${tag}.`;
  if (k.event === 'gunfire') return `${open}, {address}. Shots! Get down ${tag}.`;
  return `${open}, {address}. I saw the shots ${tag}.`;
}

const NEEDS: Record<string, [string, string, string]> = {
  debt: ['debt', 'my debt', 'the debt is gone'], cash: ['cash', 'my wages', 'the wages are paid'], housing: ['housing', 'my rent', 'the rent is paid'], papers: ['papers', 'my papers', 'the papers are in order'],
  kin: ['family', 'my family', 'my family is safe'], care: ['care', 'the care I give', 'the care is covered'], protection: ['protection', 'my safety', 'I am safe now'], advancement: ['advancement', 'my chance', 'my chance has come'],
  secret: ['secrets', 'my secret', 'the secret is buried'],
};
const POSITIONS: Record<string, [string, string]> = {
  keeper: ['keep the place', 'the keeper'], server: ['serve the tables', 'the server'], cook: ['work the kitchen', 'the cook'], door: ['mind the door', 'the doorkeeper'], entertainer: ['play the room', 'the entertainer'],
  assistant: ['serve the shop', 'the assistant'], hand: ['fix the machines', 'the mechanic'], operator: ['work the counter', 'the operator'], attendant: ['tend the kiosk', 'the attendant'], usher: ['seat the crowd', 'the usher'],
  projectionist: ['run the projector', 'the projectionist'], porter: ['carry the bags', 'the porter'], resident: ['live round here', 'the neighbour'], visitor: ['am only visiting', 'the visitor'], docker: ['load the ships', 'the docker'],
  clerk: ['stamp the forms', 'the clerk'], guard: ['hold the door', 'the guard'], 'official-driver': ['drive the car', 'the driver'], councillor: ['sit on the council', 'the councillor'], watcher: ['watch the screens', 'the watcher'],
  'patrol-officer': ['walk the beat', 'the patrol officer'], investigator: ['follow the leads', 'the investigator'],
};
const READS: Record<string, string[]> = {
  idealist: ['{need}. Proud. Wants help, not money.', '{need}. Will not be bought.'], mercenary: ['{need}. Practical. Has a price.', '{need}. Counts every coin.'],
  fearful: ['{need}. Frightened. Easily pushed.', '{need}. Jumps at every sound.'], loyal: ['{need}. Loyal. Reports what they see.', '{need}. Keeps to the rules.'],
};
const WORDS = (id: string) => id.replace(/-/g, ' ');

/** The stand-in lexicon: every need, every position the groups cover, the Scout's Listen read-outs, the debrief lines (every field of its rulebook line) and the summons. */
export function standInLexicon(dr: DialogueRules, people: PeopleRules, rb: Rulebook): Lexicon {
  const positions = [...new Set(Object.values(dr.groups).flatMap(g => g.covers))].sort();
  const fields = debriefFields(rb);
  return {
    needs: lexiconNeeds(people).map(need => ({ need, noun: NEEDS[need]![0], own: NEEDS[need]![1], met: NEEDS[need]![2] })),
    positions: positions.map(position => ({ position, job: POSITIONS[position]![0], noun: POSITIONS[position]![1] })),
    reads: TEMPERAMENTS.map(temperament => ({ temperament, lines: [...READS[temperament]!] })),
    debrief: Object.entries(fields).map(([id, fs]) => ({ id, lines: [`${WORDS(id)} one: ${fs.map(f => `{${f}}`).join(' ')}.`, `${WORDS(id)} two: ${fs.map(f => `{${f}}`).join(' ')}.`] })),
    summons: dr.lexicon.summonKeys.map(key => ({
      key, caller: [`{other} is not coming: ${WORDS(key)}.`, `Not now, {other}: ${WORDS(key)}.`], summoned: [`I cannot come, ${WORDS(key)}.`, `Not this time: ${WORDS(key)}.`],
    })),
  };
}

/** A scene of three beats for a key and variant. */
function scene(k: SceneKey): SceneT {
  const variants = [0, 1].map(v => {
    const open = OPEN[k.temperament ?? 'none']![v]!, ord = ORD[v]!, how = MANNER[k.temperament ?? 'none']!;
    if (k.verb === 'watch' && k.group) return { beats: [{ who: 'scout' as const, line: `Same round ${TAG[k.group]!}, ${ord} look.` }, { who: 'focus' as const, line: `${open}. Back to it ${TAG[k.group]!}.` }, { who: 'bystander' as const, line: `Busy ${TAG[k.group]!}, ${ord} word.` }] };
    if (k.verb === 'watch') return { beats: [{ who: 'scout' as const, line: `Two together, ${ord} look.` }, { who: 'focus' as const, line: `Quickly, {other}. ${ord} word.` }, { who: 'other' as const, line: `Yes, {focus}. Quietly, ${ord} time.` }] };
    const tie = k.tie ?? 'none';
    if (tie === 'none') return { beats: [{ who: 'focus' as const, line: `${open}. It is {own}, ${ord} time.` }, { who: 'bystander' as const, line: `Hm. ${how} ${ord} word on it.` }, { who: 'focus' as const, line: `${open}. Never mind, ${ord} time.` }] };
    const topic = k.verb === 'call' ? 'come over' : TIE[tie]!;
    return { beats: [{ who: 'focus' as const, line: `${open}, {other}. ${topic}, ${ord} time.` }, { who: 'other' as const, line: `Yes, {focus}. ${topic}, said ${how} ${ord}.` }, { who: 'focus' as const, line: `Not here, {other}. About ${topic}, ${how} ${ord}.` }] };
  });
  return { ...k, variants };
}

/** The stand-in tables for a scope. `text`: the Handover's text; where it has a group's situation, its lines stand in the cell, unless they name someone (the Handover names Inez and Vale;
 *  generated tables name no one) or gender someone (he, sir: generated tables gender no one), or `keepNames` is set (the fixture test, which runs the checker on the hand-written text itself). */
export function standIn(scope: string, dr: DialogueRules, rb: Rulebook, people: PeopleRules, text?: Pick<TextFile, 'dialogue' | 'scenes'>, o: { keepNames?: boolean } = {}): Voices {
  const s = dr.scopes[scope];
  if (!s) throw new Error(`unknown scope ${scope}`);
  const banned = o.keepNames ? [] : [...people.names.pinned.map(n => n.name), ...dr.tone.gendered], clean = (ls: readonly string[]) => !ls.some(l => banned.some(b => new RegExp(`\\b${b}\\b`, 'i').test(l)));
  const out: Voices = { schema: 'suborned.voices/1', scope, groups: [], scenes: [] };
  if (s.blocks.includes('lexicon')) out.lexicon = standInLexicon(dr, people, rb);
  if (s.blocks.includes('scenes')) out.scenes = requiredScenes(people).map(scene);
  for (const group of s.groups ?? []) {
    const ref = text ? referenceOf(text, group) : undefined;
    const cells: Cell[] = requiredCells(dr, rb, people, group).map(k => {
      const real = ref?.cells.find(c => keyOf({ event: c.event, verb: c.verb, outcome: c.outcome, temperament: c.temperament }) === keyOf(k) && clean(c.lines));
      const ref4 = ref?.cells.find(c => keyOf({ event: c.event, verb: c.verb, outcome: c.outcome, temperament: c.temperament }) === keyOf(k))?.lines.length ?? 3; // the bar: as many variants as the Handover's
      return { ...k, lines: real ? real.lines : Array.from({ length: Math.max(3, ref4) }, (_, v) => filler(k, group, v)) } as Cell;
    });
    const watch = ref?.watch.length && clean(ref.watch) ? ref.watch : [`Same round ${TAG[group]!}, as ever.`, `Back at it ${TAG[group]!}, again.`, `Nothing new ${TAG[group]!}, today.`, `Quiet round ${TAG[group]!}, so far.`].slice(0, Math.max(3, ref?.watch.length ?? 0));
    const g: Group = { group, voice: `the ${group} voice (stand-in: plain and short)`, address: [...ADDRESS[group]!], cells, reads: { watch } };
    out.groups.push(g);
    const real = ref?.scenes[0];
    out.scenes.push(real && real.variants.length >= 2 && real.variants.every(v => clean(v.beats.map(b => b.line))) ? { ...real, variants: real.variants.slice(0, 3) } : scene({ verb: 'watch', group }));
  }
  return out;
}
