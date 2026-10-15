// Load a Model into its own SurrealDB database (p_<people hash>), through tools/rag's HTTP client, and read back whether a database is loaded. Derived and disposable: nothing in a
// run or a replay reads it; delete the database and `bun run people:graph` makes it again. The load record graph:current is published last, so a half-loaded database is never reused.
import { resolve } from 'node:path';
import { root } from '../rag/config';
import { sql } from '../rag/database';
import { SCHEMA_VERSION, type Model } from './model';
import { SCHEMA_FILE, type Prepared } from './source';
import { literal, loadBatches, TABLES } from './surql';

export type Loaded = { people_hash: string; schema_version: number; source: string; plan: string; counts: Model['counts'] };

/** What database `db` holds as its load record, if it is a complete load of `hash`; undefined when the database is absent, half loaded or another version. */
export async function loadedRecord(db: string, hash: string): Promise<Loaded | undefined> {
  const rows = await sql<Loaded[]>('SELECT * FROM graph:current;', db).catch(() => [[]] as Loaded[][]);
  const rec = rows[0]?.[0];
  return rec && rec.people_hash === hash && rec.schema_version === SCHEMA_VERSION ? rec : undefined;
}

export async function rowCounts(db: string): Promise<Record<string, number>> {
  const res = await sql<{ n: number }[]>(TABLES.map(t => `SELECT count() AS n FROM ${t} GROUP ALL;`).join('\n'), db);
  return Object.fromEntries(TABLES.map((t, i) => [t, res[i]?.[0]?.n ?? 0]));
}

export async function ensureDatabase(db: string): Promise<void> {
  await sql(`DEFINE NAMESPACE IF NOT EXISTS suborned; USE NS suborned; DEFINE DATABASE IF NOT EXISTS ${db};`, false);
  await sql(await Bun.file(resolve(root, SCHEMA_FILE)).text(), db);
}

/** Load `p` unless its database already holds exactly this graph. `force` loads again over it. Returns whether it loaded. */
export async function loadGraph(p: Prepared, force = false): Promise<{ loaded: boolean }> {
  await ensureDatabase(p.db);
  if (!force && (await loadedRecord(p.db, p.hash))) return { loaded: false };
  await sql(`BEGIN; DELETE graph:current; ${TABLES.map(t => `DELETE ${t};`).join(' ')} COMMIT;`, p.db);
  for (const batch of loadBatches(p.model)) await sql(`BEGIN;\n${batch.join('\n')}\nCOMMIT;`, p.db);
  const have = await rowCounts(p.db), want = { ...p.model.counts.nodes, ...p.model.counts.edges } as Record<string, number>;
  const off = TABLES.filter(t => (have[t] ?? 0) !== (want[t] ?? 0)).map(t => `${t} ${have[t] ?? 0} of ${want[t] ?? 0}`);
  if (off.length) throw new Error(`Loaded ${off.join(', ')}; load record withheld`);
  await sql(`CREATE graph:current CONTENT ${literal({ people_hash: p.hash, schema_version: SCHEMA_VERSION, source: p.source.label, plan: p.plan, inputs: p.inputs, counts: p.model.counts })};`, p.db);
  return { loaded: true };
}

/** The people graphs on the server, newest schema or not: every p_ database with its load record (absent: half loaded). */
export async function listGraphs(): Promise<{ db: string; record?: Loaded }[]> {
  const info = (await sql<{ databases?: Record<string, string> }>('USE NS suborned; INFO FOR NS;', false)).at(-1);
  const dbs = Object.keys(info?.databases ?? {}).filter(d => /^p_[0-9a-f]{16}$/.test(d)).sort();
  return Promise.all(dbs.map(async db => ({ db, record: (await sql<Loaded[]>('SELECT * FROM graph:current;', db).catch(() => [[]] as Loaded[][]))[0]?.[0] })));
}
