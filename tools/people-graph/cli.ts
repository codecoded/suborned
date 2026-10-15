// usage: bun run people:graph [--run r015 | --cast <file>] [--force] [--dry-run]
//        bun run people:graph query <name> [args…] [--run … | --cast … | --db p_…] [--json]
//        bun run people:graph queries | list | verify [--run … | --cast …] | status [--run … | --cast …]
// Expands the cast exactly as `bun run people` does (the promoted cast; --run: an accepted run's cast; --cast: a file), then loads the people as a graph into SurrealDB, database
// p_<people hash> in namespace suborned (tools/people-graph/README.md). Derived and disposable: the JSON stays the source of truth, and no run or replay reads this database.
// Reused when the same people are already loaded. No model calls. The password in .env is never printed.
import { listGraphs, loadedRecord, loadGraph } from './load';
import { endpoint, render } from './render';
import { QUERY_DIR, queryNames, readQuery, runQuery, usage } from './queries';
import { SCHEMA_VERSION } from './model';
import { prepare, type Prepared, type SourceOptions } from './source';
import { verify } from './verify';

const VALUE = ['--run', '--cast', '--plan', '--db'], FLAG = ['--force', '--json', '--dry-run'];
const argv = process.argv.slice(2), opts = new Map<string, string>(), flags = new Set<string>(), pos: string[] = [];
for (let i = 0; i < argv.length; i++) {
  const a = argv[i]!;
  if (VALUE.includes(a)) { const v = argv[++i]; if (v === undefined || v.startsWith('--')) { console.error(`${a} takes a value`); process.exit(2); } opts.set(a, v); }
  else if (FLAG.includes(a)) flags.add(a);
  else if (a.startsWith('--')) { console.error(`unknown option ${a}; known: ${[...VALUE, ...FLAG].join(' ')}`); process.exit(2); }
  else pos.push(a);
}
const source: SourceOptions = { run: opts.get('--run'), cast: opts.get('--cast'), plan: opts.get('--plan') };

async function load(p: Prepared) {
  const faults = p.checks.failed.length + p.checks.gaps.length;
  console.log(`${faults ? 'FAIL' : 'ok  '} people checks (bun run people's P-rules)  ${faults ? `${p.checks.failed.concat(p.checks.gaps).slice(0, 4).join('; ')}` : `${p.people.persons.length} people, no faults, ${p.checks.notes} seat note(s)`}`);
  for (const pr of p.source.problems) console.log(`FAIL source  ${pr}`);
  if ((faults || p.source.problems.length) && !flags.has('--force')) { console.log('not loaded: the people do not pass the checker (--force loads them anyway)'); process.exit(1); }
  const c = p.model.counts;
  console.log(`source   ${p.source.label}\nhash     ${p.hash.slice(0, 16)}  database ${p.db}  (people ${p.plan}, schema v${SCHEMA_VERSION})`);
  if (flags.has('--dry-run')) { console.log('dry run: nothing sent to the server'); return; }
  const { loaded } = await loadGraph(p, flags.has('--force'));
  console.log(`${loaded ? 'Loaded' : 'Already loaded'} ${c.nodes.person} people, ${Object.values(c.nodes).reduce((a, b) => a + b, 0)} nodes, ${Object.values(c.edges).reduce((a, b) => a + b, 0)} edges`);
  console.log(`nodes    ${Object.entries(c.nodes).map(([k, n]) => `${k} ${n}`).join(', ')}`);
  console.log(`edges    ${Object.entries(c.edges).map(([k, n]) => `${k} ${n}`).join(', ')}`);
  console.log(`ties     ${Object.entries(c.ties).map(([k, n]) => `${k} ${n}`).join(', ')}`);
  console.log(`Surrealist: connect to ${endpoint()}  namespace suborned  database ${p.db}  (user and password from .env; see tools/people-graph/README.md)`);
}

const cmd = pos[0] && ['query', 'queries', 'list', 'verify', 'status'].includes(pos[0]) ? pos.shift()! : 'load';
if (cmd === 'load' && pos.length) { console.error(`unexpected ${pos.join(' ')}; commands: query, queries, list, verify, status`); process.exit(2); }

if (cmd === 'queries') {
  for (const n of queryNames()) { const q = readQuery(n); console.log(`${usage(q)}\n    ${q.about}`); }
  console.log(`\nThe files are ${QUERY_DIR}/*.surql: paste one into Surrealist as it stands.`);
  process.exit(0);
}
if (cmd === 'list') {
  const graphs = await listGraphs();
  for (const g of graphs) console.log(`${g.db}  ${g.record ? `${g.record.counts.nodes.person} people, ${g.record.source}, schema v${g.record.schema_version}, people ${g.record.people_hash.slice(0, 12)}` : 'half loaded (no load record)'}`);
  console.log(`${graphs.length} people graph(s) in namespace suborned. Remove one with REMOVE DATABASE <name> in Surrealist; nothing else reads them.`);
  process.exit(0);
}
if (cmd === 'query') {
  const name = pos.shift();
  if (!name) { console.error(`bun run people:graph query <name> [args]; queries: ${queryNames().join(', ')}`); process.exit(2); }
  let q; try { q = readQuery(name); } catch (e) { console.error((e as Error).message); process.exit(2); }
  let db = opts.get('--db');
  if (!db) { const p = await prepare(source); db = p.db; if (!(await loadedRecord(db, p.hash))) { console.error(`database ${db} is not loaded for ${p.source.label}; run bun run people:graph${opts.has('--run') ? ` --run ${opts.get('--run')}` : opts.has('--cast') ? ` --cast ${opts.get('--cast')}` : ''} first`); process.exit(1); } }
  try {
    const answer = await runQuery(db, q, pos);
    console.log(`-- ${usage(q)}  [database ${db}]`);
    console.log(flags.has('--json') ? JSON.stringify(answer, null, 2) : render(answer));
  } catch (e) { console.error((e as Error).message); process.exit(1); }
  process.exit(0);
}

const p = await prepare(source);
if (cmd === 'status') {
  const rec = await loadedRecord(p.db, p.hash).catch(() => undefined);
  console.log(`${rec ? 'loaded' : 'not loaded'}  ${p.db}  ${p.source.label}`);
  process.exit(0);
}
if (cmd === 'verify') {
  if (!(await loadedRecord(p.db, p.hash))) { console.error(`database ${p.db} is not loaded for ${p.source.label}; run bun run people:graph first`); process.exit(1); }
  const checks = await verify(p.db, p);
  for (const c of checks) console.log(`${c.ok ? 'ok  ' : 'FAIL'} ${c.name}${c.ok ? '' : `\n       ${c.detail}`}`);
  const bad = checks.filter(c => !c.ok).length;
  console.log(`${checks.length - bad} of ${checks.length} agree with the JSON (${p.source.label}, database ${p.db})`);
  process.exit(bad ? 1 : 0);
}
await load(p);
