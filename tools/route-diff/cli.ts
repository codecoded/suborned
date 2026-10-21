// usage: bun run route-diff [--a <engine>] [--b <engine>] [--matrix] [--all] [--only id,id] [--scripts side|tree] [--json <file>] [--no-cache] [--fail-on-move]
//   <engine>: HEAD | <commit or ref> | tree (the working tree) | <path>.dll (a built suborned-sim.dll; its inputs from --a-root/--b-root, default this repo's
//             working tree) | <dir> (a repo-shaped folder holding sim/cli, built here). Default: --a HEAD --b tree.
//   --scripts side  each side plays its own route scripts (HEAD's at HEAD, the tree's in the tree): the whole change (default)
//   --scripts tree  both sides play the working tree's scripts: only the engine moved
//   --a-scripts / --b-scripts <dir>  a folder of route scripts for that side (e.g. a recomposed set before it is committed)
//   --matrix        also the fight-lab matrix's prepared rows (P-A, P-G, P-B under today and lab; --matrix-runs L1,L2,P-A… to choose)
// Replays every recorded Handover route (contracts/missions/handover/routes/*.json, the same set `tools/sim/unity.ts --routes` plays) on both
// engines headless and prints one table of what moved: outcome, fail code, end time, Trail, Alert, health lost, and under each moved route the
// first event-log line where the two runs part. Plays are cached by engine bytes + inputs + script (out/route-diff/cache), so the unchanged side
// costs nothing the second time. Exit 0 once the table is printed (1 with --fail-on-move and anything moved; 2 for a usage or engine error).
import { createHash } from 'node:crypto';
import { existsSync, mkdirSync, readdirSync, readFileSync, writeFileSync } from 'node:fs';
import { cpus } from 'node:os';
import { basename, join, resolve } from 'node:path';
import { ROOT, build, engineAt, engineInputs, env, hasDotnet, type EngineAt, type Play } from '../missions/check/engine';
import { compareAll, compareLab, errorRow, parseLab, renderLab, renderTable, rowOf, type Healths, type LabRow, type Row } from './core';

const ROUTES = 'contracts/missions/handover/routes';
const OUT = join(ROOT, 'out/route-diff');
const sha = (b: string | Buffer) => createHash('sha256').update(b).digest('hex');
const git = (...a: string[]) => Bun.spawnSync(['git', ...a], { cwd: ROOT, stdout: 'pipe', stderr: 'pipe' });

export type Side = { label: string; dll: string; root: string; scripts: string; fingerprint: string; healths: Healths };

/** The engine side a spec names; builds it when it is source. */
export function resolveSide(spec: string, rootOverride?: string): Side {
  let dll: string, root: string, label: string, scripts = join(ROOT, ROUTES);
  if (spec.endsWith('.dll')) {
    dll = resolve(spec); root = resolve(rootOverride ?? ROOT); label = `${basename(dll)} ${sha(readFileSync(dll)).slice(0, 8)} on ${root === ROOT ? 'the working tree\'s inputs' : root}`;
    if (!existsSync(dll)) throw new Error(`no such engine DLL: ${dll}`);
  } else if (existsSync(join(resolve(spec), 'sim/cli/cli.csproj'))) {
    const dir = resolve(spec), e: EngineAt = { rev: 'dir', commit: basename(dir), dir, hash: '', inputs: 'commit' };
    dll = build(e); root = resolve(rootOverride ?? dir); label = `${spec} (built here)`; scripts = existsSync(join(dir, ROUTES)) ? join(dir, ROUTES) : scripts;
  } else {
    const e = engineAt(spec, 'commit');
    dll = build(e); root = resolve(rootOverride ?? e.dir);
    if (spec === 'tree') label = 'the working tree';
    else {
      label = `${spec === 'HEAD' ? 'HEAD ' : ''}${e.commit.slice(0, 12)}`;
      const dest = join(OUT, 'scripts', e.commit.slice(0, 12));
      if (!existsSync(join(dest, ROUTES, 'routes.json'))) {
        mkdirSync(dest, { recursive: true });
        const tar = Bun.spawnSync(['sh', '-c', `git archive ${e.commit} ${ROUTES} | tar -x -C '${dest}'`], { cwd: ROOT, stdout: 'pipe', stderr: 'pipe' });
        if (tar.exitCode !== 0) throw new Error(`git archive ${e.commit} ${ROUTES}: ${tar.stderr}`);
      }
      scripts = join(dest, ROUTES);
    }
  }
  if (!existsSync(join(root, 'contracts/rules/rulebook.json'))) throw new Error(`${root} has no contracts/rules/rulebook.json: the engine needs the rulebook above its working folder (--a-root/--b-root)`);
  const rb = JSON.parse(readFileSync(join(root, 'contracts/rules/rulebook.json'), 'utf8')).values ?? {};
  const inputs = engineInputs(root).filter(f => existsSync(join(root, f)));
  return { label, dll, root, scripts, fingerprint: sha(readFileSync(dll) + inputs.map(f => `\n${f}\n${sha(readFileSync(join(root, f)))}`).join('')), healths: { op: rb['health-op']?.v ?? 4, enforcer: rb['health-enforcer']?.v ?? 6, lab: rb['lab-health-scale']?.v ?? 10 } };
}

/** The route scripts in a folder: every top-level .json except the manifest (what `unity.ts --routes` plays). */
export const scriptsIn = (dir: string) => readdirSync(dir).filter(f => f.endsWith('.json') && f !== 'routes.json').sort();

const cacheFile = (side: Side, scriptSha: string) => join(OUT, 'cache', `${sha('v2' + side.fingerprint + scriptSha).slice(0, 32)}.json`);

async function playOne(side: Side, file: string, cache: boolean): Promise<Row> {
  const id = basename(file).replace(/\.json$/, ''), text = readFileSync(file, 'utf8'), ss = sha(text), cf = cacheFile(side, ss);
  if (cache && existsSync(cf)) { const r = JSON.parse(readFileSync(cf, 'utf8')) as Row; return { ...r, id }; }
  const p = Bun.spawn(['dotnet', side.dll, 'play', file, '--json'], { cwd: side.root, env, stdout: 'pipe', stderr: 'pipe' });
  const [out, err] = await Promise.all([new Response(p.stdout).text(), new Response(p.stderr).text(), p.exited]);
  if (!out.trim().startsWith('{')) return errorRow(id, ss, ((err || out).trim().split('\n')[0] ?? 'no output').slice(0, 300)); // not cached: a refusal may be fixed by a rebuild
  const row = rowOf(id, JSON.parse(out) as Play, ss, side.healths);
  if (cache) { mkdirSync(join(OUT, 'cache'), { recursive: true }); writeFileSync(cf, JSON.stringify(row)); }
  return row;
}

export async function playRoutes(side: Side, scripts: string, only: string[] | undefined, cache: boolean, n = Math.max(2, Math.min(8, cpus().length - 1))): Promise<Map<string, Row>> {
  const files = scriptsIn(scripts).filter(f => !only || only.includes(f.replace(/\.json$/, ''))), out = new Map<string, Row>();
  let next = 0;
  await Promise.all(Array.from({ length: Math.min(n, files.length) }, async () => {
    while (next < files.length) { const f = files[next++]!; out.set(f.replace(/\.json$/, ''), await playOne(side, join(scripts, f), cache)); }
  }));
  return out;
}

/** fight-lab's prepared rows on one side. Needs briefs/027/presets and the route scripts beside the rulebook; a commit side gets them from git. */
export function playMatrix(side: Side, runs: string): LabRow[] {
  const lab = join(side.root, 'briefs/027/presets/today.json');
  if (!existsSync(lab) || !existsSync(join(side.root, ROUTES, 'route-a-board.json'))) {
    if (side.root === ROOT) throw new Error('the working tree has no briefs/027/presets or no route-a-board.json');
    const commit = basename(side.root).replace(/\+tree$/, '');
    const r = Bun.spawnSync(['sh', '-c', `git archive ${commit} briefs/027/presets ${ROUTES} | tar -x -C '${side.root}'`], { cwd: ROOT, stdout: 'pipe', stderr: 'pipe' });
    if (r.exitCode !== 0) throw new Error(`the matrix needs briefs/027/presets and the routes in ${side.root} and git archive ${commit} failed: ${r.stderr}`);
  }
  const r = Bun.spawnSync(['dotnet', side.dll, 'fight-lab', '--runs', runs, '--presets', 'today,lab'], { cwd: side.root, env, stdout: 'pipe', stderr: 'pipe' });
  const rows = parseLab(r.stdout.toString());
  if (!rows.length) throw new Error(`fight-lab on ${side.label} printed no matrix rows (exit ${r.exitCode}): ${(r.stderr.toString() || r.stdout.toString()).trim().split('\n')[0]}`);
  return rows;
}

if (import.meta.main) {
  const args = process.argv.slice(2), flag = (k: string) => { const i = args.indexOf(k); return i >= 0 ? args.splice(i, 2)[1] : undefined; }, bool = (k: string) => { const i = args.indexOf(k); if (i >= 0) args.splice(i, 1); return i >= 0; };
  const a = flag('--a') ?? 'HEAD', b = flag('--b') ?? 'tree', aRoot = flag('--a-root'), bRoot = flag('--b-root'), aScripts = flag('--a-scripts'), bScripts = flag('--b-scripts');
  const mode = flag('--scripts') ?? 'side', json = flag('--json'), only = flag('--only')?.split(','), matrixRuns = flag('--matrix-runs');
  const matrix = bool('--matrix') || !!matrixRuns, all = bool('--all'), cache = !bool('--no-cache'), failOnMove = bool('--fail-on-move');
  if (args.length || !['side', 'tree'].includes(mode)) { console.error(`unknown argument ${args.join(' ') || `--scripts ${mode}`}; see the usage line at the top of tools/route-diff/cli.ts`); process.exit(2); }
  if (!hasDotnet()) { console.error('route-diff needs the .NET SDK (dotnet on PATH; DOTNET_ROOT=/opt/homebrew/opt/dotnet/libexec on the Studio)'); process.exit(2); }
  const t0 = performance.now();
  try {
    const A = resolveSide(a, aRoot), B = resolveSide(b, bRoot), tick = () => ((performance.now() - t0) / 1000).toFixed(1);
    const dirA = aScripts ? resolve(aScripts) : mode === 'tree' ? join(ROOT, ROUTES) : A.scripts, dirB = bScripts ? resolve(bScripts) : mode === 'tree' ? join(ROOT, ROUTES) : B.scripts;
    console.error(`· engines ready (${tick()} s); playing ${scriptsIn(dirA).filter(f => !only || only.includes(f.replace(/\.json$/, ''))).length} routes on A and ${scriptsIn(dirB).filter(f => !only || only.includes(f.replace(/\.json$/, ''))).length} on B`);
    const [ra, rb] = await Promise.all([playRoutes(A, dirA, only, cache), playRoutes(B, dirB, only, cache)]);
    const moved = compareAll(ra, rb);
    console.log(renderTable(moved, { all, labelA: A.label, labelB: B.label }));
    let labMoved: ReturnType<typeof compareLab> | undefined;
    if (matrix) {
      const runs = matrixRuns ?? 'P-A,P-G,P-B';
      const lab = (s: Side) => { try { return playMatrix(s, runs); } catch (e) { console.log(`\nfight-lab matrix: ${s.label} has none (${(e as Error).message})`); return undefined; } };
      const la = lab(A), lb = lab(B);
      labMoved = compareLab(la ?? [], lb ?? []);
      console.log(`\nfight-lab matrix (${runs}; presets today, lab)${la && lb ? '' : ' - only one side has the matrix, so its rows show as added or removed'}`);
      console.log(renderLab(labMoved, { all }));
    }
    console.error(`(${tick()} s)`);
    if (json) { mkdirSync(resolve(json, '..'), { recursive: true }); writeFileSync(json, JSON.stringify({ a: A.label, b: B.label, routes: moved.map(m => ({ ...m, a: m.a && { ...m.a, log: undefined, checkpoints: undefined }, b: m.b && { ...m.b, log: undefined, checkpoints: undefined } })), matrix: labMoved }, null, 1) + '\n'); }
    const any = moved.some(m => m.status !== 'same') || !!labMoved?.some(m => m.status !== 'same');
    process.exit(failOnMove && any ? 1 : 0);
  } catch (e) { console.error(`route-diff: ${(e as Error).message}`); process.exit(2); }
}
