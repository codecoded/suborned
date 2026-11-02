// The name ban (brief 089, Splice): names language models favour, kept out of what the player sees. The list is Fable's research
// (strong = measured, reported = named in a complaint: these fail; lookalike = patterns: a warning to review, Aster 2026-11-02), read from contracts/people/ once Pulse moves it there,
// else from briefs/089/research/llm-name-ban-list.json. Matching is by whole word: a string is cut into words at anything that is
// not a letter, so "Vale's" is the name Vale plus "s" (a possessive counts as the name) and "valet" is not Vale. Case and accents
// are ignored ("vale", "Válé"), except for names that are also English words (`alsoWords`: moss, reed, iris, veil), which count
// only when written with a capital. The lookalike patterns apply only to a person's name, never to prose ("very" is not a name).
import { existsSync, readdirSync, readFileSync } from 'node:fs';
import { join, resolve } from 'node:path';

export const ROOT = resolve(import.meta.dir, '../..');
export const BAN_BRIEF = 'briefs/089/research/llm-name-ban-list.json';
export const CONFIG = 'tools/names/names.check.json';

export type BanFile = { strong: Record<string, string>; reported: string[]; lookalike: string[]; sources?: Record<string, string> };
export type Expected = { source: string; name: string };
export type Ignore = { source: string; at: string; text: string; why: string };
export type Config = {
  about?: string;
  allow: { name: string; why: string; by: string }[];
  ignore: Ignore[];
  alsoWords: string[];
  expected: { brief: string; until: string; why: string; hits: Expected[] };
};
export type Ban = { names: Map<string, { name: string; why: string }>; lookalike: RegExp[]; alsoWords: Set<string>; from: string };
/** warn: a lookalike, printed for review and never a failure (Sky wants normal names; the patterns also catch Vidal, Varma, Vera, Elias). */
export type Found = { word: string; name: string; why: string; warn?: boolean };

/** Accents off: "Válé" → "Vale". */
export const plain = (s: string) => s.normalize('NFD').replace(/\p{M}/gu, '');
/** Lower case, accents off: "Válé" → "vale". */
export const fold = (s: string) => plain(s).toLowerCase();
/** The words of a string, cut at anything that is not a letter (accents off, case kept). */
export const wordsOf = (s: string) => plain(s).split(/[^\p{L}]+/u).filter(Boolean);
const capital = (w: string) => w[0] !== w[0]!.toLowerCase();

/** The ban list's path: contracts/people/ first (any *ban*.json there with `strong` and `lookalike`), else the brief's research copy. */
export function banPath(root = ROOT): string {
  const dir = join(root, 'contracts/people');
  if (existsSync(dir)) for (const f of readdirSync(dir).filter(f => /ban.*\.json$/.test(f)).sort()) {
    try { const j = JSON.parse(readFileSync(join(dir, f), 'utf8')); if (j && j.strong && j.lookalike) return `contracts/people/${f}`; } catch { /* not the list */ }
  }
  return BAN_BRIEF;
}
export const readConfig = (root = ROOT): Config => JSON.parse(readFileSync(join(root, CONFIG), 'utf8'));

/** The list as a lookup, less the allow list (a name Sky pinned that happens to be on it). */
export function loadBan(root = ROOT, config = readConfig(root)): Ban {
  const from = banPath(root), b = JSON.parse(readFileSync(join(root, from), 'utf8')) as BanFile;
  return makeBan(b, config, from);
}
export function makeBan(b: BanFile, config: Pick<Config, 'allow' | 'alsoWords'>, from = 'inline'): Ban {
  const names = new Map<string, { name: string; why: string }>();
  for (const [n, src] of Object.entries(b.strong)) names.set(fold(n), { name: n, why: `strong (${src})` });
  for (const n of b.reported) if (!names.has(fold(n))) names.set(fold(n), { name: n, why: 'reported' });
  const allow = new Set(config.allow.map(a => fold(a.name)));
  for (const a of allow) names.delete(a);
  const lookalike = b.lookalike.map(p => new RegExp(p));
  // an allowed name passes the lookalikes too, or a pinned "Elena" would still fail on ^el[aeiy]
  return { names, lookalike: allow.size ? lookalike.map(r => new RegExp(`^(?!(?:${[...allow].join('|')})$)(?:${r.source})`)) : lookalike, alsoWords: new Set(config.alsoWords.map(fold)), from };
}

/** The banned words in one string: a name on the strong or reported list fails. `person`: the string is a person's name, so the lookalike
 *  patterns are tried too (capitalised words), and a lookalike comes back with `warn` set: for review, not a failure.
 *  `capitals`: only a capitalised word counts (C# literals, where a lower-case "vale" is a code or a key, never a word on screen). */
export function match(ban: Ban, text: string, person = false, capitals = false): Found[] {
  const out: Found[] = [];
  for (const w of wordsOf(text)) {
    const f = w.toLowerCase(), hit = ban.names.get(f);
    if (hit && (capital(w) || !(capitals || ban.alsoWords.has(f)))) { out.push({ word: w, name: hit.name, why: hit.why }); continue; }
    if (person && capital(w)) { const re = ban.lookalike.find(r => r.test(f)); if (re) out.push({ word: w, name: w, why: `looks like the list (${re.source})`, warn: true }); }
  }
  return out;
}
