// usage: bun run drift:auto [--check] [--cause "<text>"] [--base <rev>] [--objective people,skyline,dialogue,transit] [--plan harbour-district]
// When `bun run verify` fails on "CHECKER CODE changed", this finds which drift ledgers need a declaration (people, skyline, dialogue and
// transit, whose hash covers tools/plan, the plan checker), shows the files behind each, and declares them with a cause that names those files.
//   --check      report only: what would be declared or refused, nothing written (exit 1 if anything needs a declaration)
//   --cause      the cause words, in place of the generated one (the files are still listed in the report)
//   --base       the state the working tree is compared with (default HEAD, with its own ledgers)
// It never declares a drift that changes a replay's result. For each objective with a failing run it replays that objective's runs at the base
// state and at the working tree with the declaration in place provisionally (a scratch ledger, LEVELS_LEDGER), and compares line for line, with the
// code line reduced to the rules whose result differs. Any run that still fails, or whose replay differs from the base's, refuses the whole
// objective: it prints the lines apart and declares nothing (a rule that moved is named by a person: `bun tools/levels/<objective>/drift.ts declare
// "<cause>" --rules <ids>`). A declaration it makes has no rules (--rules none) and is confirmed by replaying the objective's runs once more.
import { cpSync, existsSync, mkdirSync, readFileSync, rmSync, symlinkSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { declaredFor } from '../gen/ledger';
import { runDirs } from '../gen/paths';
import { declareCause, ledgerPath, readLedger } from '../levels/drift';
import { dialogueFiles, dialogueHash } from '../levels/dialogue/hash';
import { peopleFiles, peopleHash } from '../levels/people/hash';
import { skylineFiles, skylineHash } from '../levels/skyline/hash';
import { toolsHash, transitFiles } from '../levels/toolhash';
import { root } from '../rag/config';
import { causeText, codeFinding, judgeRun, type RunReplay, type RunVerdict } from './core';

type Objective = { name: string; ledger: string; id: string; files: (base: string) => string[]; hash: (base: string) => Promise<string> };
/** The four ledgers. tools/plan (the plan checker) is part of transit's hash: there is no separate plan ledger. */
export const OBJECTIVES: Objective[] = [
  { name: 'transit', id: 'transit', ledger: 'tools/levels/drift.json', files: transitFiles, hash: toolsHash },
  { name: 'skyline', id: 'skyline', ledger: 'tools/levels/skyline/drift.json', files: skylineFiles, hash: skylineHash },
  { name: 'people', id: 'people', ledger: 'tools/levels/people/drift.json', files: peopleFiles, hash: peopleHash },
  { name: 'dialogue', id: 'dialogue', ledger: 'tools/levels/dialogue/drift.json', files: dialogueFiles, hash: dialogueHash },
];

const git = (...a: string[]) => { const r = Bun.spawnSync(['git', ...a], { cwd: root, stdout: 'pipe', stderr: 'pipe' }); if (r.exitCode !== 0) throw new Error(`git ${a.join(' ')}: ${r.stderr.toString().trim()}`); return r.stdout.toString().trim(); };

/** The tree at `rev`, enough to replay in: tools, contracts, runs, GDD, the crew cards (a run pins their hashes) and the sim's data, with this tree's node_modules. Cached by commit. */
export function materialize(rev: string): { dir: string; label: string } {
  const commit = git('rev-parse', `${rev}^{commit}`), dir = resolve(root, 'out/drift-auto/base', commit.slice(0, 12));
  const have = ['tools', 'contracts', 'runs', 'GDD', '.claude/agents', 'sim/data', 'package.json', 'tsconfig.json', 'bunfig.toml'].filter(p => git('ls-tree', commit, '--', p) !== '');
  if (!existsSync(resolve(dir, '.ready'))) {
    rmSync(dir, { recursive: true, force: true }); mkdirSync(dir, { recursive: true });
    const tar = Bun.spawnSync(['sh', '-c', `git archive ${commit} ${have.join(' ')} | tar -x -C '${dir}'`], { cwd: root, stdout: 'pipe', stderr: 'pipe' });
    if (tar.exitCode !== 0) throw new Error(`git archive ${commit}: ${tar.stderr}`);
    symlinkSync(resolve(root, 'node_modules'), resolve(dir, 'node_modules'));
    writeFileSync(resolve(dir, '.ready'), commit + '\n');
  }
  // the images a run pins by sha256 (concept sheets, round pictures) are git-ignored, so the commit does not hold them: read them where they are, from this tree.
  // Without them the base replays "not on this machine" and the tree replays "checked", a difference that is not the code's, and every skyline declaration is refused.
  if (existsSync(resolve(root, 'briefs')) && !existsSync(resolve(dir, 'briefs'))) symlinkSync(resolve(root, 'briefs'), resolve(dir, 'briefs'));
  return { dir, label: `${rev === 'HEAD' ? 'HEAD ' : ''}${commit.slice(0, 12)}` };
}

/** Replay one run in the tree at `dir` (the same `bun tools/levels/replay.ts` `verify` uses), many at once. */
async function replayAll(dir: string, plan: string, runs: { id: string; objective: string }[], env: Record<string, string> = {}): Promise<RunReplay[]> {
  return Promise.all(runs.map(async r => {
    const p = Bun.spawn(['bun', 'tools/levels/replay.ts', r.id, plan], { cwd: dir, env: { ...process.env, ...env }, stdout: 'pipe', stderr: 'pipe' });
    const [o, e, code] = await Promise.all([new Response(p.stdout).text(), new Response(p.stderr).text(), p.exited]);
    return { id: r.id, objective: r.objective, lines: (o + e).split('\n').filter(Boolean), ok: code === 0 || code === 3 }; // 3: the run never finished (listed, not a failure)
  }));
}

/** Files of a hash that differ between the tree at `baseDir` and this one (changed, added, removed), as repo-relative paths. */
export function changedFiles(o: Objective, baseDir: string): string[] {
  const names = new Set([...o.files(root), ...(existsSync(baseDir) ? o.files(baseDir) : [])]);
  const read = (dir: string, f: string) => { const p = resolve(dir, f); return existsSync(p) ? readFileSync(p) : undefined; };
  return [...names].filter(f => { const a = read(baseDir, f), b = read(root, f); return !a || !b || !a.equals(b); }).sort();
}

if (import.meta.main) {
  const args = process.argv.slice(2), flag = (k: string) => { const i = args.indexOf(k); return i >= 0 ? args.splice(i, 2)[1] : undefined; }, bool = (k: string) => { const i = args.indexOf(k); if (i >= 0) args.splice(i, 1); return i >= 0; };
  const cause = flag('--cause'), rev = flag('--base') ?? 'HEAD', plan = flag('--plan') ?? 'harbour-district', only = flag('--objective')?.split(','), check = bool('--check');
  if (args.length || only?.some(n => !OBJECTIVES.some(o => o.name === n))) { console.error(`unknown argument ${args.join(' ') || only!.join(',')}; see the usage line at the top of tools/drift-auto/cli.ts`); process.exit(2); }
  const t0 = performance.now(), secs = () => `${((performance.now() - t0) / 1000).toFixed(0)} s`;
  const runIds = runDirs(plan), objectiveOf = (id: string) => JSON.parse(readFileSync(resolve(root, 'runs/levels', plan, id, 'run.json'), 'utf8')).objective as string;
  const runs = runIds.map(id => ({ id, objective: objectiveOf(id) })).filter(r => !only || only.includes(r.objective)); // --objective: only those objectives' runs are replayed at all

  // 1. verify's replays, in this tree
  const now = await replayAll(root, plan, runs);
  const failing = new Map<string, RunReplay[]>();
  for (const r of now) if (r.lines.some(l => l.startsWith('FAIL') && /CHECKER CODE changed/.test(l))) failing.set(r.objective, [...(failing.get(r.objective) ?? []), r]);
  const wanted = OBJECTIVES.filter(o => failing.has(o.name) && (!only || only.includes(o.name)));
  const superseded = (r: RunReplay) => r.lines.filter(l => l.startsWith('FAIL')).every(l => /^FAIL\s+superseded:/.test(l)); // a pre-pinning run: verify expects it
  const otherFails = now.filter(r => !r.ok && !failing.has(r.objective) && !superseded(r)).map(r => r.id);
  console.log(`${now.length} runs replayed (${secs()}): ${failing.size ? `CHECKER CODE changed fails ${[...failing].map(([o, rs]) => `${o} (${rs.map(r => r.id).join(', ')})`).join('; ')}` : 'no CHECKER CODE failure, nothing to declare'}`);
  if (otherFails.length) console.log(`note: ${otherFails.join(', ')} fail for other reasons; drift:auto does not touch those`);
  if (!wanted.length) process.exit(0);

  // 2. the base state, replayed with its own ledgers
  const base = materialize(rev);
  const baseReplays = await replayAll(base.dir, plan, runs.filter(r => wanted.some(o => o.name === r.objective) && existsSync(resolve(base.dir, 'runs/levels', plan, r.id, 'run.json'))));
  const byId = new Map(baseReplays.map(r => [r.id, r]));

  // 3. per objective: files behind it, the provisional declaration, the comparison
  const scratch = resolve(root, 'out/drift-auto/ledgers');
  rmSync(scratch, { recursive: true, force: true }); mkdirSync(scratch, { recursive: true });
  for (const o of OBJECTIVES) { const real = ledgerPath(o.ledger, o.id); if (existsSync(real)) cpSync(real, resolve(scratch, `${o.id}.json`)); }
  const decisions: { o: Objective; hash: string; cause: string; files: string[]; verdicts: RunVerdict[]; hand?: string[] }[] = [];
  for (const o of wanted) {
    const hash = await o.hash(root), files = changedFiles(o, base.dir), rs = runs.filter(r => r.objective === o.name);
    const hand = failing.get(o.name)!.flatMap(r => r.lines.filter(l => /CHECKER CODE changed/.test(l) && codeFinding([l]).unknownRecorded).map(l => `${r.id}: ${l.slice(0, 220)}`));
    const text = cause ?? causeText(o.name, base.label, files, rs.length);
    declareCause(resolve(scratch, `${o.id}.json`), hash, text, [], true); // provisional: the scratch ledger only
    decisions.push({ o, hash, cause: text, files, verdicts: [], hand });
  }
  const prov = await replayAll(root, plan, runs.filter(r => wanted.some(o => o.name === r.objective)), { LEVELS_LEDGER: scratch });
  for (const d of decisions) d.verdicts = prov.filter(r => r.objective === d.o.name).map(r => judgeRun(r, byId.get(r.id)));

  // 4. report, declare
  let refused = 0, declared = 0;
  for (const d of decisions) {
    const moved = d.verdicts.filter(v => v.moved);
    console.log(`\n${d.o.name}  (${d.o.ledger}; hash ${d.hash.slice(0, 12)}, ${declaredFor(readLedger(ledgerPath(d.o.ledger, d.o.id)), d.hash).length} causes declared for it now)`);
    console.log(`  files changed since ${base.label}: ${d.files.length || 'none listed'}`);
    for (const f of d.files) console.log(`    ${f}`);
    if (d.hand?.length) { console.log('  note: the recorded state of a run is not in the ledger; a declaration cannot fix that (a run records its own state when it ends)'); for (const h of d.hand) console.log('    ' + h); }
    if (moved.length) {
      refused++;
      console.log(`  REFUSED: ${moved.length} of ${d.verdicts.length} runs would change their replay result; nothing declared`);
      for (const v of moved) {
        console.log(`    ${v.id}: ${v.reason}`);
        for (const l of v.failLines ?? []) {
          const f = /CHECKER CODE changed/.test(l) ? codeFinding([l]) : undefined;
          console.log(f ? `      FAIL CHECKER CODE changed${f.uncovered.length ? `; rules whose result differs and no declared cause names: ${f.uncovered.join(', ')}` : f.unknownRecorded ? '; the run\'s recorded state is not in the ledger' : '; no cause declared for this code state'}` : `      FAIL ${l.replace(/^FAIL\s+/, '').slice(0, 260)}`);
        }
        for (const l of v.diff?.onlyBase ?? []) console.log(`      - ${l.slice(0, 200)}   (at ${base.label})`);
        for (const l of v.diff?.onlyNow ?? []) console.log(`      + ${l.slice(0, 200)}   (in the working tree)`);
      }
      console.log(`  a person declares a change that moves a rule: bun tools/levels/${d.o.name === 'transit' ? '' : d.o.name + '/'}drift.ts declare "<cause>" --rules <ids>`);
      continue;
    }
    console.log(`  every one of the ${d.verdicts.length} ${d.o.name} runs replays as it does at ${base.label}`);
    console.log(`  cause: ${d.cause}`);
    if (check) { console.log('  --check: not written'); continue; }
    declareCause(ledgerPath(d.o.ledger, d.o.id), d.hash, d.cause, [], true);
    declared++;
    console.log(`  declared in ${d.o.ledger} (--rules none; the earlier causes carried forward)`);
  }

  // 5. confirm: the declared objectives, replayed under the real ledgers
  if (declared) {
    const doneNames = decisions.filter(d => !d.verdicts.some(v => v.moved) && !d.hand?.length).map(d => d.o.name);
    const after = await replayAll(root, plan, runs.filter(r => doneNames.includes(r.objective)));
    const bad = after.filter(r => !r.ok);
    console.log(`\nconfirmed: ${after.length - bad.length}/${after.length} runs of ${doneNames.join(', ')} replay OK under the real ledgers${bad.length ? `; STILL FAILING: ${bad.map(b => b.id).join(', ')}` : ''}`);
    if (bad.length) refused++;
  }
  console.log(`\n${check ? 'check' : 'drift:auto'}: ${declared} declared, ${refused} refused, ${decisions.length - declared - refused} left for --check (${secs()}); then run bun run verify`);
  process.exit(refused || (check && decisions.length) ? 1 : 0);
}
