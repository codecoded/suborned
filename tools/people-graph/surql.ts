// SurrealQL text from plain data: literals, record ids and the statements that load a Model. Record ids are always written ⟨…⟩, so a hyphen or a dot in an id is never read as
// an operator; an id that could end the bracket or break a line is refused. Pure: no server is touched here.
import type { Edge, Model, Node } from './model';
import { Rec } from './model';

const BAD_ID = /[⟩\r\n\u0000]/;
export const recordId = (table: string, id: string): string => {
  if (!/^[a-z_][a-z0-9_]*$/.test(table)) throw new Error(`bad table name ${JSON.stringify(table)}`);
  if (!id || BAD_ID.test(id)) throw new Error(`bad record id ${JSON.stringify(id)} in ${table}`);
  return `${table}:⟨${id}⟩`;
};

/** A SurrealQL literal for a value of the model: a Rec is a record id, undefined is left out of an object, null is refused (an absent value is omitted, never written as null). */
export function literal(v: unknown): string {
  if (v instanceof Rec) return recordId(v.table, v.id);
  if (v === null) throw new Error('null in a graph value: omit the field instead');
  if (typeof v === 'string' || typeof v === 'boolean') return JSON.stringify(v);
  if (typeof v === 'number') { if (!Number.isFinite(v)) throw new Error('non-finite number'); return String(v); }
  if (Array.isArray(v)) return `[${v.map(literal).join(', ')}]`;
  if (typeof v === 'object' && v !== undefined) return `{${Object.entries(v as Record<string, unknown>).filter(([, x]) => x !== undefined).map(([k, x]) => `${JSON.stringify(k)}: ${literal(x)}`).join(', ')}}`;
  throw new Error(`cannot write ${typeof v} as SurrealQL`);
}

export const nodeStatement = (n: Node) => `CREATE ${recordId(n.table, n.id)} CONTENT ${literal(n.fields)};`;
export const edgeStatement = (e: Edge) => `RELATE ${recordId(e.from.table, e.from.id)}->${recordId(e.table, e.id)}->${recordId(e.to.table, e.to.id)} CONTENT ${literal(e.fields)};`;

/** The load order: lookups first, then the nodes a link points to, then everything that links. */
export const NODE_ORDER = ['temperament', 'need', 'tie_kind', 'place', 'unit', 'shift', 'role', 'person'] as const;
export const EDGE_ORDER = ['lives_at', 'works_at', 'tie', 'stops_at', 'sits_at', 'knows_need_of', 'has_need', 'has_temperament', 'casts'] as const;
export const TABLES = [...NODE_ORDER, ...EDGE_ORDER] as const;

/** The statements of a Model in load order, as batches of at most `size` statements. */
export function loadBatches(m: Model, size = 120): string[][] {
  const all: string[] = [];
  for (const t of NODE_ORDER) for (const n of m.nodes.filter(x => x.table === t)) all.push(nodeStatement(n));
  for (const t of EDGE_ORDER) for (const e of m.edges.filter(x => x.table === t)) all.push(edgeStatement(e));
  const known = new Set<string>(TABLES);
  for (const x of [...m.nodes, ...m.edges]) if (!known.has(x.table)) throw new Error(`table ${x.table} is not in the load order`);
  const out: string[][] = [];
  for (let i = 0; i < all.length; i += size) out.push(all.slice(i, i + size));
  return out;
}
