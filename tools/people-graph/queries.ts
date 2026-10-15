// The canned queries (queries/*.surql): each file is a complete SurrealQL script a person can paste into Surrealist as it stands. Its header comments say what it asks and name its
// parameters (`-- param $name type default what`); the `LET $name = …;` lines below set them. The command line rewrites those LET lines from its arguments, so what runs here is
// the file with new values, never another query. No server is touched in this file but runQuery.
import { readdirSync, readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { root } from '../rag/config';
import { sql } from '../rag/database';

export const QUERY_DIR = 'tools/people-graph/queries';
export const PARAM_TYPES = ['person', 'place', 'unit', 'time', 'int', 'string'] as const;
export type ParamType = (typeof PARAM_TYPES)[number];
export type Param = { name: string; type: ParamType; default: string; about: string };
export type Query = { name: string; about: string; params: Param[]; text: string };

const ID = /^[a-z0-9][a-z0-9._-]*$/;

export function parseQuery(name: string, text: string): Query {
  const lines = text.split('\n'), about = lines.find(l => l.startsWith('-- about: '))?.slice(10) ?? '';
  if (lines[0] !== `-- query: ${name}`) throw new Error(`queries/${name}.surql must start with "-- query: ${name}"`);
  if (!about) throw new Error(`queries/${name}.surql has no "-- about:" line`);
  const params: Param[] = [];
  for (const l of lines.filter(x => x.startsWith('-- param '))) {
    const m = l.match(/^-- param \$(\w+) (\w+) (\S+)(?: (.*))?$/);
    if (!m) throw new Error(`queries/${name}.surql: cannot read "${l}"; expected "-- param $name type default what"`);
    const type = m[2] as ParamType;
    if (!PARAM_TYPES.includes(type)) throw new Error(`queries/${name}.surql: param $${m[1]} has type ${type}; known: ${PARAM_TYPES.join(', ')}`);
    params.push({ name: m[1]!, type, default: m[3]!, about: m[4] ?? '' });
  }
  for (const p of params) if (!lines.some(l => l.startsWith(`LET $${p.name} = `) && l.endsWith(';'))) throw new Error(`queries/${name}.surql: param $${p.name} has no "LET $${p.name} = …;" line`);
  return { name, about, params, text };
}

export const queryNames = (base: string = root): string[] => readdirSync(resolve(base, QUERY_DIR)).filter(f => f.endsWith('.surql')).map(f => f.slice(0, -6)).sort();
export const readQuery = (name: string, base: string = root): Query => {
  if (!/^[a-z][a-z0-9-]*$/.test(name) || !queryNames(base).includes(name)) throw new Error(`no query ${JSON.stringify(name)}; known: ${queryNames(base).join(', ')}`);
  return parseQuery(name, readFileSync(resolve(base, QUERY_DIR, `${name}.surql`), 'utf8'));
};

/** hh:mm → minutes of the day (24:00 is allowed: the end of the day clock). */
export function minutes(s: string): number {
  const m = s.match(/^(\d{1,2}):(\d{2})$/);
  if (!m) throw new Error(`a time is hh:mm, not ${JSON.stringify(s)}`);
  const t = Number(m[1]) * 60 + Number(m[2]);
  if (Number(m[2]) > 59 || t > 1440) throw new Error(`${s} is not a time of the day clock (00:00 to 24:00)`);
  return t;
}

/** The SurrealQL literal for an argument of a parameter type; anything that is not a plain id, time or number is refused (an argument never becomes SurrealQL of its own). */
export function literalFor(p: Param, arg: string): string {
  switch (p.type) {
    case 'person': case 'place': case 'unit': if (!ID.test(arg)) throw new Error(`$${p.name}: ${JSON.stringify(arg)} is not a ${p.type} id`); return `${p.type}:⟨${arg}⟩`;
    case 'time': return arg.toLowerCase() === 'none' ? 'NONE' : String(minutes(arg));
    case 'int': if (!/^\d{1,6}$/.test(arg)) throw new Error(`$${p.name}: ${JSON.stringify(arg)} is not a whole number`); return String(Number(arg));
    case 'string': if (!/^[A-Za-z0-9._ -]{1,60}$/.test(arg)) throw new Error(`$${p.name}: ${JSON.stringify(arg)} is not a plain word`); return JSON.stringify(arg);
  }
}

/** The query with its arguments set. `args` are positional, in the order of the params; a missing one keeps the file's default. A bare `at` before a time argument is read as the word it is. */
export function bind(q: Query, argv: string[]): string {
  const args = [...argv];
  const set = new Map<string, string>();
  for (const p of q.params) {
    if (p.type === 'time' && args[0]?.toLowerCase() === 'at' && args.length > 1) args.shift();
    const a = args.shift();
    if (a !== undefined) set.set(p.name, literalFor(p, a));
  }
  if (args.length) throw new Error(`${q.name} takes ${q.params.length ? q.params.map(p => `<${p.name}>`).join(' ') : 'no arguments'}; got extra: ${args.join(' ')}`);
  return q.text.split('\n').map(l => { const m = l.match(/^LET \$(\w+) = /); return m && set.has(m[1]!) ? `LET $${m[1]} = ${set.get(m[1]!)};` : l; }).join('\n');
}

export const usage = (q: Query) => `${q.name} ${q.params.map(p => (p.default === 'none' ? `[${p.name}]` : `<${p.name}>`)).join(' ')}`.trim();

/** Run a query in `db` and return the answer: the result of its last statement (the LETs before it answer nothing). */
export async function runQuery(db: string, q: Query, argv: string[] = []): Promise<unknown> {
  const res = await sql(bind(q, argv), db);
  return res.at(-1);
}
