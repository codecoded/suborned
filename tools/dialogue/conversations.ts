// Brief 085: full conversation scenes, written through the RAG core at build time. A proposal for Sky to read: nothing here reaches the game or text.json.
// Each scene: facts bound by code from the sim's people (sheet) + retrieved world and rules + the writer brief -> Fable writes takes -> a script-editor pass ->
// this file's checks (a fault goes back to the editor once) -> Motive reviews the set for fit with the rules. Every call is saved by runAgent under the run.
//   bun tools/dialogue/conversations.ts [--only id,id] [--run r002] [--base dir] [--retrieve-only]   then: bun tools/dialogue/conversations-page.ts
import { createHash } from 'node:crypto';
import { existsSync, mkdirSync, readdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { z } from 'zod';
import { runAgent, type Call, type Objective } from '../rag/agent';
import { root, writeJson } from '../rag/config';
import { hash, loadCorpus } from '../rag/corpus';
import { retrieve } from '../rag/retrieval';

const read = (p: string) => Bun.file(resolve(root, p)).text();
const peopleFile = 'sim/data/handover.people.json', textFile = 'contracts/missions/handover/text.json';
const people = JSON.parse(await read(peopleFile)), text = JSON.parse(await read(textFile));
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
const INEZ_WHO = who(INEZ, 'Inez', `She looks at who can see her before she looks at you. She talks about her tables, her tips and her Bureau card. She never says she's frightened and never threatens anyone: her fear shows in her questions, and in giving way too fast. She's kind, and tired.`);
const WORLD = `The place: the café and its terrace face Records Square; the Civic Records Office is across the square. Evening, shift change coming. A polite, orderly city that writes everything down. Records are paper: stamped cards, forms, pneumatic tubes. Calls are voice, by telephone. Robots follow fixed routines. Everyone in the scene is a person of this city except the cell.`;
const VOICES = (...k: string[]) => k.map(v => `- ${v}: ${text.tone.voices[v]}`).join('\n');
const INEZ_GUARD = `Inez and the door guard are lovers, in secret. He stands the Office's public door, ${apart} m across the square from her. The two are never together unless she calls him. In speech call him Okafor (never his given name, and no gendered word beyond "he").`;

type Spec = { id: string; title: string; kind: 'learn' | 'turn' | 'call'; maxBeats: number; speakers: string[]; query: string[]; sheet: string; mustSay: { what: string; re: RegExp }[] };
export const specs: Spec[] = [
  {
    id: 'listen-inez', title: 'Listen to Inez', kind: 'learn', maxBeats: 10, speakers: ['Inez', 'Milena', 'Scout'],
    query: ['Inez door guard lovers debt affair Help Pressure secret', 'Fearful temperament hesitates Listen reveals need secret', 'world records paper calls voice orderly city fears disorder', 'conversation scene overheard Listen'],
    mustSay: [{ what: 'the Bureau loan', re: /bureau/i }, { what: 'the guard', re: /okafor|guard/i }],
    sheet: `${WORLD}
Situation: the Scout has sat at the next table for half a minute. The player hears what the Scout hears. Inez, at the café door, is working her tables. Milena speaks through the open door from just inside. Neither knows the Scout is listening.
${INEZ_WHO}
${who(MILENA, 'Milena', 'Works the same café, inside this shift; she owes the Bureau too. No secret of hers comes up.')}
${INEZ_GUARD} Milena has noticed where Inez keeps looking; she doesn't know for sure until this talk.
The scene must make plain, by its end: Inez is a waitress at this café; she owes the Bureau and can't keep up; she is seeing the guard on the Office door; if the Office hears of it, it costs them both; she is nervous by nature.
Last beat: the Scout, very low, to the cell, terse fragments: what was heard, and what each thing could get you with her: the loan could be cleared, Okafor could be used. Never counts the ways, and gives no verdict on which is better.
Speakers: Inez, Milena, Scout.
learned: 2 lines (the debt; the lover). opens: "Help" and "Pressure" (both are now open on Inez; they cost what the ring says).
${VOICES('waitress', 'scout')}`,
  },
  {
    id: 'help-inez-1', title: 'Help Inez, first visit', kind: 'turn', maxBeats: 8, speakers: ['Inez', 'Fixer'],
    query: ['Help clear debt gratitude bravery Fearful needs Help twice', 'Inez capstone call pulls guard off his door', 'approach conversation private no witnesses', 'world paper records optimistic surface coercive underneath'],
    mustSay: [{ what: 'the Bureau', re: /bureau/i }],
    sheet: `${WORLD}
Situation: the Fixer has come to Inez at the café door, and has just paid a first instalment on her Bureau loan. It is a real payment. Fact of the game: this first Help does NOT win her over; she is grateful and scared and not yet willing. One more visit, one more payment, will do it. Nobody calls it a "first Help". The Fixer has not yet said what would be wanted, only hinted.
${INEZ_WHO}
Speakers: Inez, Fixer. She doesn't know the Fixer. Her fear: nobody pays a stranger's debt for nothing; who has been asking about her?
The scene must make plain: a stranger just paid part of her Bureau loan; she took it but isn't ready to do anything for it; he will be back with the rest. End in a way that makes it clear coming back is what's next, while not saying it's the only way.
learned: 1 line (she took it; not ready yet). opens: "Help again" as the action's plain name.
${VOICES('waitress', 'fixer')}`,
  },
  {
    id: 'help-inez-2', title: 'Help Inez, second visit', kind: 'turn', maxBeats: 8, speakers: ['Inez', 'Fixer'],
    query: ['Help clear debt gratitude bravery helped Inez warns rivals', 'Inez call guard walks to her side partners stays 60 s', 'capstone the call that pulls the guard off his door'],
    mustSay: [{ what: 'calling him over', re: /call|come over|fetch|over here|send for/i }, { what: 'the guard', re: /okafor|guard|him/i }],
    sheet: `${WORLD}
Situation: the Fixer comes back and clears the rest of the Bureau loan. This time she is won over, gratefully and still frightened. Now the Fixer finally says what's wanted, and it is small: when the Fixer telephones, she calls Okafor over from his door. He'll come to her side and stay about a minute.
${INEZ_WHO}
${INEZ_GUARD}
She must work out for herself that the Fixer knows about Okafor, and be frightened by that; the Fixer is kind about it and makes no threat. She asks what happens to him. The honest answer the Fixer can give: nothing happens to him if this goes right, and he never sees us.
Fact of the game: because she was helped, she will warn the cell if she sees trouble coming on the square. Let her offer that, in her own frightened way, near the end.
The scene must make plain: the loan is paid; she's with us now; when we say so, she calls Okafor off the door.
learned: 1 line. opens: "Call Inez".
${VOICES('waitress', 'fixer')}`,
  },
  {
    id: 'pressure-inez', title: 'Pressure Inez', kind: 'turn', maxBeats: 8, speakers: ['Inez', 'Fixer'],
    query: ['Pressure threat fear resentment can identify Fixer seen', 'Inez secret affair Pressure threatens to expose', 'capstone the call that pulls the guard off his door'],
    mustSay: [{ what: 'the affair exposed', re: /office|tell|know|hear/i }, { what: 'calling him over', re: /call|come over|fetch|over here|send for/i }],
    sheet: `${WORLD}
Situation: the same woman, a different way in. The Fixer comes to Inez at the café door with no money and tells her, gently, that the Office could learn about her and Okafor. She gives in quickly, and hates it. The Fixer is warm in manner, steel underneath, takes no pleasure in it and doesn't pretend otherwise. The Fixer keeps their voice low because a threat can be seen. What's wanted: when the Fixer telephones, she calls Okafor over from his door; he comes to her side and stays about a minute.
${INEZ_WHO}
${INEZ_GUARD}
Fact of the game, for learned only, and nobody says it aloud: she will remember the Fixer's face, and could describe it if she's ever questioned. She wouldn't threaten a stranger who frightens her, and the Fixer wouldn't remind her.
The scene must make plain, the cards included: we threatened to expose the two of them; she agreed, and hates it, and never says she's scared; when we say so, she calls Okafor off the door.
learned: 1 line, and it mentions she knows the Fixer's face. opens: "Call Inez".
${VOICES('waitress', 'fixer')}`,
  },
  {
    id: 'pay-driver', title: 'Pay the driver', kind: 'turn', maxBeats: 9, speakers: ['Zhou', 'Fixer'],
    query: ['route B pickup driver turned Pay service bay waits', 'official driver needs cash Mercenary Pay', 'getaway rivals lookout turned driver warns lookout off', 'world paper records voice calls'],
    mustSay: [{ what: 'the service bay', re: /service bay|round the back|service drive|round to the/i }, { what: 'the Councillor', re: /councillor/i }],
    sheet: `${WORLD}
Situation: the Fixer finds Vale's official driver waiting by the official car in the bay on the Office's south side, on Apron Road, and offers an envelope. It works. The driver has had a lean month and bargains over the terms, and the conversation is about the exact job, because this is someone who's been paid to be vague before and wants it spelled out.
${who(DRIVER, 'Zhou', 'Call the driver Zhou; no gendered word. Calls Vale "the Councillor", always.')}
The job, exactly as the game plays it: when the chime goes the Councillor comes out and gets in. A paid Zhou doesn't drive off. Zhou takes the Councillor round to the service bay at the back, the east yard, and waits there with him. Zhou won't wait for ever: a couple of minutes, then drives on. Zhou also says that if someone's hanging about watching the bay, Zhou can send them off; Zhou has noticed nothing specific and names nobody. Getting Vale out of the back seat is the Fixer's people's job; Zhou doesn't want to see it.
The Fixer says "we" and never explains who "we" are. Zhou doesn't ask, and says why not, in a line.
The scene must make plain: Zhou is the Councillor's driver and is short of money; we paid; at the chime the Councillor's car goes to the service bay, not home, and waits a short while; that's where to be.
learned: 1 line. opens: nothing new to press; instead 1 line naming the place to be ("Be at the service bay").
${VOICES('driver', 'fixer')}`,
  },
  {
    id: 'call-inez', title: 'The call, after Help', kind: 'call', maxBeats: 8, speakers: ['Fixer', 'Inez', 'Okafor'],
    query: ['capstone the call pulls guard off door partners ring side stays 60 s', 'route A front door Call Inez window Enforcer enters', 'calls are voice telephone'],
    mustSay: [{ what: 'he leaves the door', re: /door|post|step/i }],
    sheet: `${WORLD}
Situation: the moment the whole front-door plan was for. The Fixer telephones the café and says it's time. Inez, whom the cell has helped, then gets Okafor to leave his door: she calls across to him on the Office door's handset. He comes, walks across the square, stands at her side for a minute, and goes back. The shot cuts between them.
${INEZ_WHO}
${who(GUARD, 'Okafor', 'In speech he is Okafor to the Fixer; Inez may call him "love". He has a shift to finish and likes her more than the job.')}
${INEZ_GUARD}
She is scared and must sound ordinary to him. He must not suspect. Tenderness matters: this is two people who love each other, and one of them is using it. Let the player feel that, in a line. No coffee spill and no trick: he comes because she asked.
The scene must make plain: she called him and he is leaving the door now; the door stands empty while he's with her, a few minutes at most counting his walk there and back.
learned: 1 line ("The door is empty"). opens: 1 line naming what the door allows now, "Enter the Office" (the Enforcer's action).
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
const JARGON = /\b(alert|trail|standing|heat|turned|temperament|approach|lever|mission|agent|asset|halcyon|lender)\b/i;
const avoid: string[] = [...text.tone.avoid, ...text.tone.profanity];
const words = (s: string) => s.trim().split(/\s+/).length;
/** The checks a scene must pass. Empty = pass. Word limits are the contract's (12) and the plain-copy limit for the game's own lines (6). */
export function check(s: Spec, f: Pick<Final, 'beats' | 'learned' | 'opens'>): string[] {
  const out: string[] = [], all = f.beats.map(b => b.line).join(' ');
  if (f.beats.length < 4 || f.beats.length > s.maxBeats) out.push(`${f.beats.length} beats: must be 4 to ${s.maxBeats}`);
  for (const b of f.beats) {
    if (!s.speakers.includes(b.speaker)) out.push(`speaker "${b.speaker}" isn't in the scene (${s.speakers.join(', ')})`);
    if (words(b.line) > 12) out.push(`over 12 words: "${b.line}"`);
    if (b.line.includes('!')) out.push(`exclamation mark: "${b.line}"`);
    if (JARGON.test(b.line)) out.push(`design word or "lender": "${b.line}"`);
    if (NAMES_FEAR.test(b.line)) out.push(`names the fear instead of showing it: "${b.line}"`);
    for (const a of avoid) if (new RegExp(`\\b${a.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}\\b`, 'i').test(b.line)) out.push(`avoid-list word "${a}": "${b.line}"`);
  }
  for (const l of [...f.learned, ...f.opens]) if (words(l) > 6) out.push(`game line over 6 words: "${l}"`);
  for (const m of s.mustSay) if (!m.re.test(all)) out.push(`never says: ${m.what}`);
  return out;
}

const args = Bun.argv.slice(2), arg = (k: string) => { const i = args.indexOf(k); return i >= 0 ? args[i + 1] : undefined; };
if (import.meta.main) {
  const only = arg('--only')?.split(','), base = arg('--base') ?? 'runs/conversations/085';
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

  const one = async (s: Spec) => {
    const d = `${dir}/${s.id}`, calls: Call[] = [];
    const o: Objective = { id: `write-${s.id}`, role: 'dialogue-designer', brief: 'briefs/085/fable/writer.md', query: s.query, effort: 'high' };
    const task = `Scene: ${s.title}. At most ${s.maxBeats} beats.\n\nFacts (these win over any passage above):\n${s.sheet}\n\nWrite three takes that differ in situation and shape, not in wording. Each returns beats (speaker, line), learned, opens.`;
    const takes = await runAgent(o, task, Takes, calls, d);
    await writeJson(`${d}/takes.json`, takes);
    const e: Objective = { id: `edit-${s.id}`, role: 'dialogue-designer', brief: 'briefs/085/fable/editor.md', query: s.query.slice(0, 2), effort: 'high' };
    const edTask = (extra = '') => `Scene: ${s.title}. At most ${s.maxBeats} beats.\n\nFacts:\n${s.sheet}\n\nThe writer's hard limits: ${(WRITER_LIMITS)}\n\nThe takes:\n${JSON.stringify(takes.takes, null, 1)}\n\n${extra}Return the finished scene.`;
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
  const done = await Promise.all(chosen.map(one));

  const reviewCalls: Call[] = [];
  const review = await runAgent(
    { id: 'review-fit', role: 'systems-designer', brief: 'briefs/085/fable/review.md', query: ['Inez Fearful Help Pressure Pay approaches', 'route B driver turned service bay rivals lookout', 'capstone partners call guard stays', 'conversation scene clock pauses'], effort: 'medium' },
    `The scenes:\n${JSON.stringify(done.map(x => ({ scene: x.spec.id, facts: x.spec.sheet, beats: x.final.beats, learned: x.final.learned, opens: x.final.opens })), null, 1)}`,
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
    inputs: { [peopleFile]: await sha(peopleFile), [textFile]: await sha(textFile), 'briefs/085/fable/writer.md': await sha('briefs/085/fable/writer.md'), 'briefs/085/fable/editor.md': await sha('briefs/085/fable/editor.md') },
    calls: allCalls.map(c => ({ id: c.id, role: c.role, model: c.model, status: c.status, seconds: c.seconds, costUsd: c.costUsd, hits: c.retrieval.hits.length })),
    callCount: allCalls.length, wallSeconds: Math.round((Date.now() - start) / 1000), modelSeconds: allCalls.reduce((n, c) => n + c.seconds, 0),
    faults: done.flatMap(x => x.faults.map(f => `${x.spec.id}: ${f}`)), reviewFaults: review.faults.length,
  });
  console.log(`${run}: ${allCalls.length} calls, ${Math.round((Date.now() - start) / 1000)} s; review faults ${review.faults.length}`);
  if (existsSync(resolve(root, dir))) console.log(`saved under ${dir}`);
}
