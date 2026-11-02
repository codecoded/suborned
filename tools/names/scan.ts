// What the player sees, as strings for the name ban (brief 089): where each source lives and which of its strings are words on screen.
//   text            contracts/missions/handover/text.json: every string but the notes, rules, sources, triggers and engine codes (SKIP_KEYS)
//   people          sim/data/handover.{people,site,rounds}.json: every `name` (a person's name: the lookalike patterns apply)
//   play            unity/Assets/Suborned/Runtime/Play/*.cs: the string literals (the host's fallbacks), comments left out
//   streaming-*     the same words and people as staged in unity/Assets/StreamingAssets/sim (git-ignored: checked when present)
//   pool            the generator's name pool: `names.given` and `names.family` of contracts/people/*.cast.json, and of any contracts/people/**/*pool*.json
//                   Pulse moves them to (read when present). The real frequency lists (contracts/people/names/<country>.json) are source data that hold
//                   ordinary names on the list (Thomas, Sarah, Elena): the generator drops those at the draw by people.rules.json names.lists.banList,
//                   so what the check holds there is that pointer (generatorBan), and the draws themselves (people)
//   (out/people/ is left out: an intermediate, machine-local; sim/data/handover.people.json is the committed copy the game reads)
import { existsSync, readdirSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { ROOT } from './ban.ts';

export type Str = { source: string; path: string; text: string; person: boolean; capitals?: boolean };
export type Source = { id: string; file: string; optional: boolean; strings: (root: string) => Str[] };

/** text.json keys whose strings are never shown: authoring notes, rules, sources, triggers, engine state and ids (text.schema.ts). */
export const SKIP_KEYS = new Set(['schema', 'mission', 'author', 'gdd', 'rule', 'rules', 'note', 'notes', 'src', 'source', 'from', 'trigger', 'rings',
  'goneWhen', 'shows', 'hiddenWhen', 'pick', 'log', 'tone', 'events', 'exchange', 'silentStates', 'silentLogs', 'fates', 'named', 'status', 'check',
  'id', 'seed', 'key', 'guess', 'brief', 'on']);
/** Paths whose key is shown elsewhere but not here: an objective step's `when` is engine state (a way's `when`, "Before 4:50", is shown). */
const SKIP_PATHS = [/^less\.objective\.steps\.\d+\.when$/, /\.pending$/];
/** An engine code, not a word: lower case, no space, joined by - . : _ / | or , ("vale-car", "guard.at-cafe", ":after-vale"). A bare lower-case
 *  word ("guard", the walk-in plate's who) is a word on screen, so it is still checked. */
export const CODE = /^(?=.*[.:_/|,-])[a-z0-9.:_/|,-]*[a-z][a-z0-9.:_/|,-]*$/;
/** text.json strings that are a person's name (the lookalike patterns apply). */
const PERSON_PATHS = [/^names\.(speakers|targets)\./, /^scenes\.conversations\.(names|ui\.voice)\./];

export function textStrings(source: string, json: unknown): Str[] {
  const out: Str[] = [];
  const walk = (v: unknown, path: string) => {
    if (typeof v === 'string') {
      if (SKIP_PATHS.some(r => r.test(path)) || CODE.test(v)) return;
      out.push({ source, path, text: v, person: PERSON_PATHS.some(r => r.test(path)) });
    } else if (v && typeof v === 'object') {
      for (const [k, x] of Object.entries(v)) {
                if (!Array.isArray(v) && SKIP_KEYS.has(k)) continue;
        walk(x, path ? `${path}.${k}` : k);
      }
    }
  };
  walk(json, '');
  return out;
}

/** Every `name` string in a people file, at its path. */
export function nameStrings(source: string, json: unknown): Str[] {
  const out: Str[] = [];
  const walk = (v: unknown, path: string) => {
    if (v && typeof v === 'object') for (const [k, x] of Object.entries(v)) {
      const p = path ? `${path}.${k}` : k;
      if (k === 'name' && typeof x === 'string') out.push({ source, path: p, text: x, person: true }); else walk(x, p);
    }
  };
  walk(json, '');
  return out;
}

/** The string literals of a C# file, with their line: regular, verbatim (@"") and interpolated ($"", the holes' code left out). Comments are skipped. */
export function csStrings(source: string, file: string, cs: string): Str[] {
  const out: Str[] = [];
  let line = 1;
  /** Code from i: comments, char literals and strings, until the end or (in an interpolation hole) the `}` that closes it; returns that index. */
  const code = (i: number, hole: boolean): number => {
    let depth = 0;
    while (i < cs.length) {
      const c = cs[i]!;
      if (c === '\n') { line++; i++; continue; }
      if (c === '/' && cs[i + 1] === '/') { while (i < cs.length && cs[i] !== '\n') i++; continue; }
      if (c === '/' && cs[i + 1] === '*') { const e = cs.indexOf('*/', i + 2), end = e < 0 ? cs.length : e + 2; line += (cs.slice(i, end).match(/\n/g) ?? []).length; i = end; continue; }
      if (c === '\'') { let j = i + (cs[i + 1] === '\\' ? 3 : 2); while (j < cs.length && cs[j] !== '\'' && cs[j] !== '\n') j++; i = j + 1; continue; }
      if (c === '"') { i = literal(i); continue; }
      if (hole && c === '{') depth++;
      if (hole && c === '}') { if (depth === 0) return i; depth--; }
      i++;
    }
    return i;
  };
  /** The literal opening at i (its @ and $ just before it); records it, returns the index after it. */
  const literal = (i: number): number => {
    const pre = cs.slice(Math.max(0, i - 2), i), verbatim = pre.endsWith('@') || pre === '@$', interp = pre.endsWith('$') || pre === '$@', at = line;
    let j = i + 1, s = '';
    for (; j < cs.length; j++) {
      const d = cs[j]!;
      if (d === '\n') line++;
      if (interp && d === '{') { if (cs[j + 1] === '{') { j++; s += '{'; continue; } j = code(j + 1, true); s += ' '; continue; } // a hole: its own literals are read on their own
      if (interp && d === '}' && cs[j + 1] === '}') { j++; s += '}'; continue; }
      if (!verbatim && d === '\\') { j++; s += ' '; continue; }
      if (d === '"') { if (verbatim && cs[j + 1] === '"') { j++; s += '"'; continue; } break; }
      s += d;
    }
    if (s.trim() && !CODE.test(s)) out.push({ source, path: `${file}:${at}`, text: s, person: false, capitals: true });
    return j + 1;
  };
  code(0, false);
  return out;
}

/** Every name in a pool: the `given` and `family` arrays anywhere in the file, of strings or of { name }. */
export function poolStrings(source: string, file: string, json: unknown): Str[] {
  const out: Str[] = [];
  const walk = (v: unknown, path: string) => {
    if (v && typeof v === 'object') for (const [k, x] of Object.entries(v)) {
      const p = path ? `${path}.${k}` : k;
      if ((k === 'given' || k === 'family') && Array.isArray(x)) x.forEach((e, i) => { const n = typeof e === 'string' ? e : (e as { name?: unknown })?.name; if (typeof n === 'string') out.push({ source, path: `${file}:${p}.${i}`, text: n, person: true }); });
      else walk(x, p);
    }
  };
  walk(json, '');
  return out;
}
/** The pool files: contracts/people/*.cast.json and any *pool*.json under contracts/people. */
export function poolFiles(root = ROOT): string[] {
  const out: string[] = [], dir = join(root, 'contracts/people');
  const walk = (d: string, rel: string) => { for (const e of readdirSync(d, { withFileTypes: true })) { const r = `${rel}/${e.name}`; if (e.isDirectory()) walk(join(d, e.name), r); else if (/\.cast\.json$|pool.*\.json$/.test(e.name)) out.push(r); } };
  if (existsSync(dir)) walk(dir, 'contracts/people');
  return out.sort();
}
/** Where the people generator reads its ban list (people.rules.json names.lists.banList), or null when the rules name none. */
export function generatorBan(root = ROOT): string | null {
  try { return (JSON.parse(readFileSync(join(root, 'contracts/people/people.rules.json'), 'utf8')) as { names?: { lists?: { banList?: string } } }).names?.lists?.banList ?? null; } catch { return null; }
}

const json = (root: string, f: string) => JSON.parse(readFileSync(join(root, f), 'utf8'));
const PEOPLE = ['handover.people.json', 'handover.site.json', 'handover.rounds.json'];
const PLAY = 'unity/Assets/Suborned/Runtime/Play';
/** Play files that are checks, not the game: the proofs, the bot runs and the shot runners compare and report words, they never show them. */
export const NOT_SHOWN = /^(Proof\w*|BotRun(\.\w+)?|FrontEnd\.Proof|\w*Shots\w*|ShotRunner)\.cs$/;
const SA = 'unity/Assets/StreamingAssets/sim';

export const SOURCES: Source[] = [
  { id: 'text', file: 'contracts/missions/handover/text.json', optional: false, strings: r => textStrings('text', json(r, 'contracts/missions/handover/text.json')) },
  { id: 'people', file: 'sim/data', optional: false, strings: r => PEOPLE.flatMap(f => nameStrings('people', json(r, `sim/data/${f}`)).map(s => ({ ...s, path: `${f}:${s.path}` }))) },
  { id: 'pool', file: 'contracts/people', optional: false, strings: r => poolFiles(r).flatMap(f => poolStrings('pool', f.replace('contracts/people/', ''), json(r, f))) },
  { id: 'play', file: PLAY, optional: false, strings: r => readdirSync(join(r, PLAY)).filter(f => f.endsWith('.cs') && !NOT_SHOWN.test(f)).sort().flatMap(f => csStrings('play', f, readFileSync(join(r, PLAY, f), 'utf8'))) },
  { id: 'streaming-text', file: `${SA}/text.json`, optional: true, strings: r => textStrings('streaming-text', json(r, `${SA}/text.json`)) },
  { id: 'streaming-people', file: `${SA}/handover.people.json`, optional: true, strings: r => PEOPLE.filter(f => existsSync(join(r, SA, f))).flatMap(f => nameStrings('streaming-people', json(r, `${SA}/${f}`)).map(s => ({ ...s, path: `${f}:${s.path}` }))) },
];

/** The sources on this machine: the committed ones always (a missing one throws), a staged or generated one only when present. */
export function present(root = ROOT): Source[] { return SOURCES.filter(s => !s.optional || existsSync(join(root, s.file))); }
