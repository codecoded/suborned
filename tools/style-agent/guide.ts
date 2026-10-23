import { createHash } from 'node:crypto';
import { readdirSync, readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { selectLatestGdd } from '../rag/gdd-sources';
import type { Guide, Source } from './types';

export const TEXT_FILE = 'contracts/missions/handover/text.json';
export const TEXT_SCHEMA = 'contracts/missions/handover/text.schema.ts';
export const BRIEF_FILE = 'briefs/054-conversations-that-reveal.md';
export const hash = (text: string): string => createHash('sha256').update(text).digest('hex');
export type Read = (file: string) => string;
/** A file's sha256. Live: hashed from disk. Replay: taken from the run's record, so fingerprint-only sources need no copy. */
export type Sha = (file: string) => string;
/** The human GDD (~2 MB of markup) is fingerprinted, never read into a prompt, so runs keep its hash and not its text. */
export const fingerprintOnly = (file: string) => /^GDD\/[^/]+\.html$/.test(file);

export const readerFor = (base: string): Read => file => readFileSync(resolve(base, file), 'utf8');
export const readerFromFiles = (files: Record<string, string>): Read => file => {
  if (!Object.hasOwn(files, file)) throw new Error(`Missing archived source ${file}`);
  return files[file]!;
};
/** The approved GDD pair only: GDD/draft is never a source. */
export const gddPair = (names: string[]) => selectLatestGdd(names);
export const gddNames = (base: string) => readdirSync(resolve(base, 'GDD'));
export const gddNamesFromFiles = (files: Record<string, string>) =>
  Object.keys(files).filter(f => /^GDD\/[^/]+$/.test(f)).map(f => f.slice(4));

/** Pull one value out of the GDD, or fail: a moved sentence must stop the guide rather than leave a guess behind. */
export function pick(text: string, pattern: RegExp, what: string): string[] {
  const m = text.match(pattern);
  if (!m) throw new Error(`GDD no longer states ${what}; update the style guide extraction`);
  return m.slice(1);
}
const lineOf = (text: string, start: string, what: string) => {
  const line = text.split('\n').find(l => l.startsWith(start));
  if (!line) throw new Error(`GDD no longer has ${what}`);
  return line;
};
const block = (gdd: string, heading: string) => {
  const m = gdd.match(new RegExp(`${heading}[^\\n]*\\n[\\s\\S]*?(?=\\n### |\\n## |$)`));
  if (!m) throw new Error(`GDD no longer has ${heading}`);
  return m[0];
};

/** The style guide, built only from the approved GDD, the Handover's voices and text contract, and Sky's recorded decision (brief 054). */
export function buildGuide(read: Read, names: string[], sha: Sha = file => hash(read(file))): Guide {
  const pair = gddPair(names), gdd = read(pair.agent.path), text = JSON.parse(read(TEXT_FILE)), schema = read(TEXT_SCHEMA);
  const sources: Source[] = [];
  const add = (id: string, file: string, locator: string, excerpt: string) => sources.push({ id, file, locator, sha256: sha(file), excerpt });

  add('world', pair.agent.path, '§01 world', [
    lineOf(gdd, 'world (Sky, 2026-10-14)', '§01 world'),
    lineOf(gdd, '- thinking machines and networks never came', '§01 no networks'),
    lineOf(gdd, '- reads (rules unchanged)', '§01 reads'),
  ].join('\n'));
  add('approaches', pair.agent.path, '§04 approaches, temperament, preview', [
    lineOf(gdd, '| Help (clear debt)', '§04 Help row'), lineOf(gdd, '| Pressure | need known', '§04 Pressure row'),
    lineOf(gdd, '| Idealist |', '§04 Idealist'), lineOf(gdd, '| Mercenary |', '§04 Mercenary'),
    lineOf(gdd, '| Fearful |', '§04 Fearful'), lineOf(gdd, '| Loyal (institution) |', '§04 Loyal'),
    lineOf(gdd, 'preview: ', '§04 preview'),
  ].join('\n'));
  add('handover', pair.agent.path, '§07 cast, capstone, route A', [
    lineOf(gdd, 'cast: Vale', '§07 cast'), lineOf(gdd, 'the capstone (Sky, 2026-10-15', '§07 capstone'), lineOf(gdd, 'route A, front door', '§07 route A'),
  ].join('\n'));
  add('lines', pair.agent.path, '§10 lines', lineOf(gdd, '- lines: the goal line', '§10 lines'));
  add('fable', pair.agent.path, '§14 Fable', block(gdd, '### Fable'));
  add('motive', pair.agent.path, '§14 Motive', block(gdd, '### Motive'));
  add('voices', TEXT_FILE, '/tone', JSON.stringify(text.tone));
  add('text-shape', TEXT_SCHEMA, 'MAX_LINE, MAX_WORDS, NAMED', schema.split('\n').filter(l => /export const (MAX_LINE|MAX_WORDS|NAMED) /.test(l)).join('\n'));
  add('decision', BRIEF_FILE, 'Sky, 2026-10-23', read(BRIEF_FILE).split('\n').filter(l => l.startsWith('- Sky')).join('\n'));
  // The human GDD wins on conflict. Fingerprint it; its 2 MB of markup never enters a prompt.
  add('human-gdd', pair.human.path, 'authoritative human GDD', `Human GDD v${pair.human.version}; it wins on conflict with the agent copy.`);

  const [bubbleSeconds] = pick(gdd, /Speech bubbles at the speaker \(≤ \d+, (\d+) s, ≤ \d+ words\)/, 'the bubble time (§10)').map(Number) as [number];
  const maxWords = Number(pick(schema, /export const MAX_WORDS = (\d+);/, 'MAX_WORDS')[0]);
  const maxChars = Number(pick(schema, /export const MAX_LINE = (\d+);/, 'MAX_LINE')[0]);
  const named = pick(schema, /export const NAMED = \[([^\]]+)\]/, 'NAMED')[0]!.replace(/['\s]/g, '');

  return {
    schema: 'suborned.style-guide/2', game: 'The Suborned', gdd: pair.agent.version, sources,
    voices: text.tone.voices, avoid: text.tone.avoid, profanity: text.tone.profanity,
    bubble: { seconds: bubbleSeconds, maxWords, maxChars },
    rules: [
      { id: 'REVEAL', kind: 'story function: what the conversation teaches and sets up', sources: ['approaches', 'handover', 'decision', 'motive'], instruction:
        'A conversation exists to teach the player one or two facts and the move they open. Make each fact in the brief plain from what people say to each other, without anyone explaining the game: the player should finish knowing what is going on and what they could do next. ' +
        'Keep to the act\'s knowledge: Watch shows only where and when (routine); Listen shows who someone is (their need, their temperament and, for Inez, her secret); an approach shows how they answer the cell. Never reveal what the brief withholds. ' +
        'Inez\'s debt is her need (Help clears it); her affair with the door guard is her secret (Pressure threatens to expose it); the call that pulls the guard off his door is the capstone. Temperament shows in what a person will and will not take. The outcome in the brief is what happens; do not soften or change it. ' +
        'History, not backstory (§14 Fable): add no new plots, jobs, crimes, deals or relationships beyond the brief, and nobody else offers to solve the person\'s problem: that is the cell\'s lever.' },
      { id: 'VOICE', kind: 'character and tone', sources: ['voices', 'fable', 'world'], instruction:
        'Each speaker keeps their own voice from the mission\'s voice sheet: Inez warm, quick café patter, tired about money; the guard clipped officialese about his post and the rules; the Fixer warm and persuasive with steel under it; customers and the keeper short and human. ' +
        'Optimistic and polite on the surface, coercive underneath. Plain spoken speech: no decorative period slang, no modern internet speech, no rom-com gush, no speeches. Subtext over statement: lovers and debtors talk around things in public, low but not hidden: someone listening close by (the Scout) must be sure what is going on, from an endearment, a promise for after closing, the lender\'s man, while a customer a table away would not. People are memorable through what they do, not backstory.' },
      { id: 'WORLD', kind: 'lore and vocabulary', sources: ['world', 'voices', 'text-shape'], instruction:
        `${text.tone.world} Atomic power made everything physical cheap; thinking machines and networks never came. Records are paper, ledgers, stamped forms and microfilm; calls are voice (telephone, radio); cameras are closed-circuit and watched by people; robots follow fixed routines. Debts are owed to a lender and his man, in cash and paper, never apps, accounts online or texts. ` +
        `Only ${named.split(',').join(' and ')} are named; everyone else by role (the guard, the keeper, the customer, the Office). The cell are strangers to everyone here.` },
      { id: 'FORM', kind: 'length and shape', sources: ['lines', 'text-shape', 'decision', 'fable'], instruction:
        `A conversation is a run of speech bubbles, one speaker each, ${bubbleSeconds} s per bubble, and it fills the act's own seconds: the brief gives the line range (at most seconds ÷ ${bubbleSeconds}). ` +
        `Each line at most ${maxWords} words and ${maxChars} characters, one plain line, no stage directions, no narration, no markup. Exchanges, not monologues: people answer each other. Only the brief's cast speaks, each in their part. British spelling, as in the GDD and the mission text.` },
    ],
  };
}

export const loadGuide = (base: string) => buildGuide(readerFor(base), gddNames(base));
export const guideFromFiles = (files: Record<string, string>, shas: Record<string, string>) =>
  buildGuide(readerFromFiles(files), gddNamesFromFiles(shas), file => shas[file] ?? hash(readerFromFiles(files)(file)));
