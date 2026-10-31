// Brief 085, the live blind test, second arm: the same pipeline as conversations.ts (three takes, one editor pass, check(), one fix, Motive's review) with the report's first-round fixes that survive Sky's calls.
// The control is conversations.ts itself, run untouched. Facts, retrieval queries, beat limits, schemas and check() are the control's, so only the writing inputs differ. Nothing here reaches the game.
//   SUBORNED_LIVE=1 bun tools/dialogue/blind-arm.ts [--base runs/conversations/085-blind/arm-b] [--run r001]     a live run needs Sky's go-ahead: this test only
//   bun tools/dialogue/blind-arm.ts --preview                                                                     stand-in model, writes the prompts under out/scratch/fable/preview
import { mkdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { z } from 'zod';
import { runAgent, type Call, type Objective } from '../rag/agent';
import { card, invokeClaude, type Card } from '../rag/claude';
import { root, writeJson } from '../rag/config';
import { hash, loadCorpus } from '../rag/corpus';
import { retrieve } from '../rag/retrieval';
import { check, specs } from './conversations';
import { loadJobs, loadRules, lifted, type Excerpt, type Job, type Rules } from './exemplars/exemplars';

const read = (p: string) => Bun.file(resolve(root, p)).text();
const peopleFile = 'sim/data/handover.people.json', textFile = 'contracts/missions/handover/text.json';
const people = JSON.parse(await read(peopleFile)), text = JSON.parse(await read(textFile));
const person = (id: string) => people.people.find((p: { id: string }) => p.id === id);
const recordLabel = (r: string) => people.needs.records[r]?.[1] ?? r;
const place = (id: string) => (people.places as Record<string, [number, number]>)[id]!;
const metres = (a: string, b: string) => { const [x, y] = [place(a), place(b)]; return Math.round(Math.hypot(x[0] - y[0], x[1] - y[1]) / 100); };

/** The test's only change to Fable's role card, made for this arm's calls and never on disk: the 10-word bubble limit in the system prompt becomes Sky's 12. Since GDD v0.34.0 the card says 12 itself, and the patch leaves it. */
export const CARD_FROM = 'bubbles ≤ 10 words', CARD_TO = 'bubbles ≤ 12 words';
export function patchCard(c: Card): Card {
  if (c.name !== 'dialogue-designer') return c;
  if (/bubbles\s+≤\s+12\s+words/.test(c.body)) return c;
  const norm = (s: string) => s.replace(/bubbles\s+≤\s+10\s+words/, CARD_TO);
  if (!/bubbles\s+≤\s+10\s+words/.test(c.body)) throw new Error(`the card no longer says "${CARD_FROM}"`);
  return { ...c, body: norm(c.body), text: norm(c.text) };
}
export const cardAdapter = { provider: 'claude' as const, loadCard: async (who: string) => patchCard(await card(who)) };

/** The second arm's voices: the contract's with "the lender" taken out, a word the jargon check fails. text.json itself is untouched. */
export const voicesB = (k: string): string => (text.tone.voices[k] as string).replace('about money and the lender', 'about money');

function who(id: string, role: string, also = ''): string {
  const p = person(id);
  return `- ${role}: ${p.name}. Job: ${p.job}. Temperament (show it, never name it): ${p.temperament}. Owes or lacks: ${p.need}, on record as "${recordLabel(p.record)}".${also ? ` ${also}` : ''}`;
}
const INEZ = 'p-cafe-14', MILENA = 'p-cafe-12', apart = metres('cafe-entrance', 'office-entrance');
const WORLD = 'The place: the café and its terrace face Records Square; the Civic Records Office is across the square. Evening, shift change coming. A polite, orderly city that writes everything down. Records are paper: stamped cards, forms, pneumatic tubes. Calls are voice, by telephone. Robots follow fixed routines. Everyone in the scene is a person of this city except the cell.';
const INEZ_GUARD = `Inez and the door guard are lovers, in secret. He stands the Office's public door, ${apart} m across the square from her. The two are never together unless she calls him. In speech call him Okafor (never his given name, and no gendered word beyond "he").`;

export type SceneB = { id: string; sheet: string; jobs: Job[]; excerpts: string[] };
/** Bare facts plus each person's want, fear and stake, and what causes the yes or the no. No quotable phrasing and no direction for how a line should land. */
export const scenesB: SceneB[] = [
  {
    id: 'listen-inez', jobs: ['overheard-at-work', 'debt', 'bargain', 'dodge'],
    excerpts: ['carriers-inn-yard', 'lunch-counter-dinner', 'krogstad-paying-regularly', 'crofts-forty-thousand', 'dinah-my-language', 'gaev-anchovies'],
    sheet: `${WORLD}
Situation: the Scout has sat at the next table for half a minute. The player hears what the Scout hears. Inez, at the café door, is working her tables. Milena speaks through the open door from just inside. Neither knows the Scout is listening.
${who(INEZ, 'Inez')}
${who(MILENA, 'Milena', 'Works the same café, inside this shift; she owes the Bureau too. No secret of hers comes up.')}
${INEZ_GUARD} Milena has noticed where Inez keeps looking; she doesn't know for sure until this talk.
Inez wants: to get through this shift with the debt and Okafor both out of sight. Fears: the tables noticing; the Office hearing of Okafor, which costs them both. Stake: his post and her place.
Milena wants: something small from Inez this shift that has a price, of the writer's choosing, within a waitress's work. Her noticing is what she has to trade. Fears: her own debt being the subject. Stake: a better shift.
The cause of the turn: Inez gives Milena something, or gives something away, to stop the noticing.
Facts the whole scene carries, in speech or in the Scout's line: Inez is a waitress at this café; she owes the Bureau and is behind; she is seeing the guard on the Office door; if the Office hears of it, it costs them both; she is nervous by nature.
Last beat: the Scout, very low, to the cell: what was heard, as facts. No verdict and no count of options.
Speakers: Inez, Milena, Scout.
learned: 2 lines (the debt; the lover). opens: "Help" and "Pressure" (both are now open on Inez; they cost what the ring says).
Voices from the contract: waitress: ${voicesB('waitress')}. scout: ${voicesB('scout')}.`,
  },
  {
    id: 'pressure-inez', jobs: ['pressure', 'yes-or-no', 'lie-to-loved', 'dodge'],
    excerpts: ['krogstad-so-good', 'milverton-terms', 'tulkinghorn-whose-secret', 'milverton-coat', 'jig-then-ill-do-it', 'nora-here-no'],
    sheet: `${WORLD}
Situation: the Fixer comes to Inez at the café door with no money, and keeps the voice low because a threat can be seen. The Fixer knows about her and Okafor; the Office doesn't.
${who(INEZ, 'Inez')}
${INEZ_GUARD}
The Fixer wants: her agreement that when the Fixer telephones, she calls Okafor over from his door; he comes to her side and stays about a minute. Fears: being seen making a threat; being remembered. Stake: the plan, and the Fixer's own face.
Inez wants: this person gone before the tables see, and Okafor safe. Fears: the Office learning of the two of them. Stake: his post and her place.
The cause of her yes: something the Fixer says or does just before it shows her the Office could really learn of it, and that refusing won't keep it from them. She gives in quickly, and hates it.
What it costs the Fixer: being taken for kind no longer works on her. She will remember the Fixer's face and could describe it if she's ever questioned. The Fixer knows it, and does not get to leave the matter closed.
Facts the whole scene carries, in speech or in the cards: the threat is to expose the two of them; she agreed out of fear and resents it; when told, she calls Okafor off the door.
Speakers: Inez, Fixer.
learned: 1 line, and it mentions she knows the Fixer's face. opens: "Call Inez".
Voices from the contract: waitress: ${voicesB('waitress')}. fixer: ${voicesB('fixer')}.`,
  },
];

const allExcerpts = () => loadJobs().flatMap(j => j.excerpts);
const byId = (id: string): Excerpt => { const e = allExcerpts().find(x => x.id === id); if (!e) throw new Error(`no excerpt ${id}`); return e; };
const renderRules = (r: Rules) => `${r.speaker}\n- Notices first: ${r.noticesFirst}\n- Refuses to say: ${r.refusesToSay}\n- Talks about instead: ${r.talksAbout}\n- Dodges by: ${r.dodges}\n- Line length: ${r.length}${r.pays ? `\n- Pays: ${r.pays}` : ''}`;
const renderExcerpt = (e: Excerpt) => `[${e.id}]\n${e.text}\nThe moves:\n${e.moves.map(m => `- "${m.at}": ${m.move}`).join('\n')}`;
/** The voice material a scene's writer and editor read: its speakers' rules, then its excerpts with their moves. */
export function voiceMaterial(s: SceneB, speakers: string[]): string {
  const rules = loadRules().speakers.filter(r => speakers.includes(r.speaker));
  return `Speech rules for the people in this scene:\n\n${rules.map(renderRules).join('\n\n')}\n\nClassic conversations, for their moves. Take the moves. Never take the period style, the situation or the words: our people speak plainly, with contractions and 1950s manners.\n\n${s.excerpts.map(id => renderExcerpt(byId(id))).join('\n\n')}`;
}

const Beat = z.strictObject({ speaker: z.string(), line: z.string() });
const Take = z.strictObject({ situation: z.string().describe('one line: what makes this take different from the others'), beats: z.array(Beat), learned: z.array(z.string()), opens: z.array(z.string()) });
const Takes = z.strictObject({ takes: z.array(Take).length(3) });
const Edited = z.strictObject({ picked: z.number().int().min(1).max(3), why: z.string(), changes: z.array(z.string()).describe('what you changed and why, at most 4'), beats: z.array(Beat), learned: z.array(z.string()), opens: z.array(z.string()) });
const Review = z.strictObject({ faults: z.array(z.strictObject({ scene: z.string(), line: z.string(), problem: z.string(), fix: z.string() })), notes: z.string() });
type Final = z.infer<typeof Edited>;
const words = (s: string) => s.trim().split(/\s+/).length;

/** This arm's faults: the control's check(), unchanged, plus no run of 5 or more words from any excerpt. */
export const checkB = (spec: Parameters<typeof check>[0], f: Pick<Final, 'beats' | 'learned' | 'opens'>): string[] => [
  ...check(spec, f),
  ...lifted(f.beats.map(b => b.line), allExcerpts().map(e => e.text)).map(g => `run of 5 words from an excerpt: "${g}"`),
];

export type Deps = { invoke: typeof invokeClaude; retriever?: typeof retrieve };
export async function runArm(dir: string, deps: Deps) {
  mkdirSync(resolve(root, dir), { recursive: true });
  const start = Date.now(), allCalls: Call[] = [];
  const LIMITS = (await read('briefs/085/fable/writer-b.md')).split('## Hard limits')[1]!.trim();
  const call = <T>(o: Objective, task: string, schema: z.ZodType<T>, calls: Call[], d: string) => runAgent(o, task, schema, calls, d, [], deps.retriever, deps.invoke, cardAdapter);
  const one = async (b: SceneB) => {
    const s = specs.find(x => x.id === b.id)!, d = `${dir}/${s.id}`, calls: Call[] = [], voice = voiceMaterial(b, s.speakers);
    const o: Objective = { id: `write-${s.id}`, role: 'dialogue-designer', brief: 'briefs/085/fable/writer-b.md', query: s.query, effort: 'high' };
    const task = `Scene: ${s.title}. At most ${s.maxBeats} beats.\n\nFacts (these win over any passage above):\n${b.sheet}\n\n${voice}\n\nWrite three takes that differ in situation and shape, not in wording. Each returns beats (speaker, line), learned, opens.`;
    const takes = await call(o, task, Takes, calls, d);
    await writeJson(`${d}/takes.json`, takes);
    const e: Objective = { id: `edit-${s.id}`, role: 'dialogue-designer', brief: 'briefs/085/fable/editor-b.md', query: s.query.slice(0, 2), effort: 'high' };
    const edTask = (extra = '') => `Scene: ${s.title}. At most ${s.maxBeats} beats.\n\nFacts:\n${b.sheet}\n\n${voice}\n\nThe writer's hard limits: ${LIMITS}\n\nThe takes:\n${JSON.stringify(takes.takes, null, 1)}\n\n${extra}Return the finished scene.`;
    let final = await call(e, edTask(), Edited, calls, d), faults = checkB(s, final);
    if (faults.length) { final = await call({ ...e, id: `fix-${s.id}` }, edTask(`Your last version:\n${JSON.stringify(final.beats)}\nIt failed these checks; fix them and change nothing else:\n${faults.join('\n')}\n\n`), Edited, calls, d); faults = checkB(s, final); }
    await writeJson(`${d}/final.json`, { id: s.id, title: s.title, kind: s.kind, ...final, faults });
    allCalls.push(...calls);
    console.log(`${s.id}: ${final.beats.length} beats, ${final.beats.reduce((n, x) => n + words(x.line), 0)} words, ${faults.length} faults`);
    return { spec: s, sheet: b.sheet, final, faults };
  };
  const done = await Promise.all(scenesB.map(one));
  const reviewCalls: Call[] = [];
  const review = await runAgent(
    { id: 'review-fit', role: 'systems-designer', brief: 'briefs/085/fable/review.md', query: ['Inez Fearful Help Pressure Pay approaches', 'route B driver turned service bay rivals lookout', 'capstone partners call guard stays', 'conversation scene clock pauses'], effort: 'medium' },
    `The scenes:\n${JSON.stringify(done.map(x => ({ scene: x.spec.id, facts: x.sheet, beats: x.final.beats, learned: x.final.learned, opens: x.final.opens })), null, 1)}`,
    Review, reviewCalls, `${dir}/review`, [], deps.retriever, deps.invoke,
  );
  await writeJson(`${dir}/review/review.json`, review);
  allCalls.push(...reviewCalls);
  await writeJson(`${dir}/run.json`, {
    note: 'Proposal for Sky to read; nothing here reaches text.json or the game.', corpusHash: hash(JSON.stringify(await loadCorpus())), git: Bun.spawnSync(['git', 'rev-parse', 'HEAD'], { cwd: root }).stdout.toString().trim(),
    cardChange: `${CARD_FROM} -> ${CARD_TO}, in the system prompt of this arm's dialogue-designer calls only; the card on disk is untouched`,
    calls: allCalls.map(c => ({ id: c.id, role: c.role, model: c.model, modelsUsed: c.modelsUsed, status: c.status, seconds: c.seconds, costUsd: c.costUsd, schemaSha256: c.schemaSha256, hits: c.retrieval.hits.length })),
    callCount: allCalls.length, wallSeconds: Math.round((Date.now() - start) / 1000), faults: done.flatMap(x => x.faults.map(f => `${x.spec.id}: ${f}`)), reviewFaults: review.faults.length,
  });
  console.log(`${allCalls.length} calls, ${Math.round((Date.now() - start) / 1000)} s; review faults ${review.faults.length}`);
  return { done, review, calls: allCalls };
}

if (import.meta.main) {
  const args = Bun.argv.slice(2), arg = (k: string) => { const i = args.indexOf(k); return i >= 0 ? args[i + 1] : undefined; };
  if (args.includes('--preview')) {
    const canned = (schema: unknown) => { const json = JSON.stringify(schema); return json.includes('takes') ? { takes: [0, 1, 2].map(() => ({ situation: 'x', beats: [], learned: [], opens: [] })) } : json.includes('faults') ? { faults: [], notes: '' } : { picked: 1, why: '', changes: [], beats: [], learned: [], opens: [] }; };
    const invoke: typeof invokeClaude = async (_c, _p, schema) => ({ stdout: '', stderr: '', exitCode: 0, ok: true, output: canned(z.toJSONSchema(schema)), usage: null, costUsd: 0, model: 'stand-in', modelsUsed: [], tools: [], cliVersion: null });
    await runArm('out/scratch/fable/preview', { invoke });
  } else {
    await runArm(`${arg('--base') ?? 'runs/conversations/085-blind/arm-b'}/${arg('--run') ?? 'r001'}`, { invoke: invokeClaude });
  }
}
