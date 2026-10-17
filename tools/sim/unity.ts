// The engine for Unity (brief 011 D, Latch): one build of Keel's engine (sim/Suborned.Sim, netstandard2.1) for both hosts. It exports the
// engine and its three inputs at a commit (default HEAD: the committed engine; --tree: the working tree), builds the CLI against that very
// project (Release), plays every scenario headless through it, and copies into the Unity project:
//   unity/Assets/Suborned/Sim/Plugins/Suborned.Sim.dll   the DLL the CLI ran (same bytes, checked)
//   unity/Assets/StreamingAssets/sim/                     rulebook.json, people.rules.json, handover.site.json, handover.people.json, handover.provisional.json, handover.takedown.json (the inputs it ran on),
//                                                         headless.json (every scenario's outcome, checks and checkpoint hashes), engine.json
// Unity's Suborned/Sim/Run Scenarios plays the same scenarios with the same DLL and inputs and must give the same hashes (§11).
// The inputs are validated by bun first, with the schemas of the same commit (Redline E1: Engine.FromTexts lets unknown keys through, and a
// bad text can throw out of Step() or overflow the stack): the rulebook and people rules parse with their zod schemas and every rulebook ref
// resolves; the site parses, names its schema and nests no deeper than MAX_DEPTH; and the CLI loads all three and plays every scenario.
// engine.json records their sha256 only after that, and Unity loads only texts with those hashes.
// Both folders are generated (git-ignored). Run: bun tools/sim/unity.ts [--tree] [--routes]   (--routes: also play the route scripts in
// contracts/missions/handover/routes/ headless, for the hand-played comparison; they are read from the working tree; --dll <file>: use those
// exact engine bytes (e.g. Keel's Release build) in place of this build's, for the CLI and for Unity alike: the SDK stamps the git commit into
// the DLL's informational version, so a build made before a commit differs from one made after it in those bytes only)
import { cpSync, existsSync, mkdirSync, readdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { join } from 'node:path';

const root = join(import.meta.dir, '..', '..');
const argv = process.argv.slice(2);
const dllArg = (() => { const i = argv.indexOf('--dll'); return i < 0 ? undefined : argv[i + 1]; })();
for (const a of argv) if (a.startsWith('--') && !['--tree', '--routes', '--dll'].includes(a)) { console.error(`unknown option ${a}; known: --tree --routes --dll <Suborned.Sim.dll>`); process.exit(2); }
const tree = argv.includes('--tree');
const env = { ...process.env, DOTNET_CLI_TELEMETRY_OPTOUT: '1', DOTNET_NOLOGO: '1', ...(process.env.DOTNET_ROOT || !existsSync('/opt/homebrew/opt/dotnet/libexec') ? {} : { DOTNET_ROOT: '/opt/homebrew/opt/dotnet/libexec' }) };
const sh = (cmd: string[], cwd = root) => {
  const r = Bun.spawnSync(cmd, { cwd, env, stdout: 'pipe', stderr: 'pipe' });
  return { code: r.exitCode, out: r.stdout.toString(), err: r.stderr.toString() };
};
const sha = (b: Buffer | string) => createHash('sha256').update(b).digest('hex');
const need = (r: { code: number; out: string; err: string }, what: string) => { if (r.code !== 0) { console.error(`${what} failed:\n${r.out}\n${r.err}`); process.exit(1); } return r.out; };

const SCHEMAS = ['contracts/rules/rulebook.schema.ts', 'contracts/people/people.rules.schema.ts'];
const depth = (v: unknown): number => Array.isArray(v) ? 1 + Math.max(0, ...v.map(depth)) : v && typeof v === 'object' ? 1 + Math.max(0, ...Object.values(v).map(depth)) : 0;
const MAX_DEPTH = 24; // the site nests 6 deep today; the engine's JSON reader recurses per level
// brief 013 E: the provisional facts (security level) and the world's takedown places (stashes, service doors, call boxes, posts, watchers)
// are inputs too: without them the engine runs at level 1 with no stashes, and its records differ from every headless run of the repo.
const INPUTS = { 'rulebook.json': 'contracts/rules/rulebook.json', 'people.rules.json': 'contracts/people/people.rules.json', 'handover.site.json': 'sim/data/handover.site.json', 'handover.people.json': 'sim/data/handover.people.json', 'handover.provisional.json': 'sim/data/handover.provisional.json', 'handover.takedown.json': 'sim/data/handover.takedown.json' } as const;
const commit = need(sh(['git', 'rev-parse', 'HEAD']), 'git rev-parse').trim();
const dirty = sh(['git', 'diff', '--quiet', 'HEAD', '--', 'sim/Suborned.Sim', ...Object.values(INPUTS)]).code !== 0;
const label = (tree ? `working tree on ${commit.slice(0, 12)}${dirty ? ' (uncommitted engine or input changes)' : ''}` : `commit ${commit.slice(0, 12)}`) + (dllArg ? `, engine bytes from ${dllArg}` : '');
const work = join(root, 'out', 'sim-unity', tree ? 'tree' : commit.slice(0, 12));
rmSync(work, { recursive: true, force: true }); mkdirSync(work, { recursive: true });

// 1. the source: git archive of the commit, or a copy of the working tree (no bin/obj)
if (tree) {
  for (const p of ['sim/Suborned.Sim', 'sim/cli', ...Object.values(INPUTS)])
    cpSync(join(root, p), join(work, p), { recursive: true, filter: s => !/\/(bin|obj)(\/|$)/.test(s) });
  for (const p of SCHEMAS) cpSync(join(root, p), join(work, p));
} else {
  const tar = join(work, 'src.tar');
  need(sh(['git', 'archive', '-o', tar, 'HEAD', 'sim/Suborned.Sim', 'sim/cli', ...Object.values(INPUTS), ...SCHEMAS]), 'git archive');
  need(sh(['tar', '-xf', tar, '-C', work]), 'tar'); rmSync(tar);
}

// 1b. bun validates the texts with the schemas of the same source (zod resolves from the repo's node_modules above out/)
{
  const { Rulebook, checkRulebook } = await import(join(work, SCHEMAS[0]!));
  const { PeopleRules } = await import(join(work, SCHEMAS[1]!));
  const read = (rel: string) => JSON.parse(readFileSync(join(work, rel), 'utf8'));
  const people = PeopleRules.parse(read(INPUTS['people.rules.json']));
  const problems: string[] = checkRulebook(Rulebook.parse(read(INPUTS['rulebook.json'])), people);
  if (problems.length) { console.error(`rulebook refs fail:\n  ${problems.join('\n  ')}`); process.exit(1); }
  const site = read(INPUTS['handover.site.json']);
  if (site.schema !== 'suborned.sim.site/1' || site.mission !== 'handover') { console.error('the site is not suborned.sim.site/1 for the Handover'); process.exit(1); }
  if (read(INPUTS['handover.people.json']).schema !== 'suborned.sim.people/2') { console.error('handover.people.json is not suborned.sim.people/2'); process.exit(1); }
  for (const [f, id] of [['handover.provisional.json', 'suborned.sim.provisional/1'], ['handover.takedown.json', 'suborned.sim.takedown/1']] as const) {
    const n = read(INPUTS[f]); if (n.schema !== id || n.mission !== 'handover') { console.error(`${f} is not ${id} for the Handover`); process.exit(1); }
  }
  for (const rel of Object.values(INPUTS)) { const d = depth(read(rel)); if (d > MAX_DEPTH) { console.error(`${rel} nests ${d} deep (> ${MAX_DEPTH})`); process.exit(1); } }
  console.log('bun: rulebook.json and people.rules.json parse with their schemas and every ref resolves; the site, people, provisional and takedown files parse with their schema ids; nesting within bounds');
}

// 2. one Release build: the CLI references the engine project, so both hosts load the bytes built here
need(sh(['dotnet', 'build', join(work, 'sim/cli/cli.csproj'), '-c', 'Release', '-nologo', '-v', 'q']), 'dotnet build');
const cliDir = join(work, 'sim/cli/bin/Release/net10.0');
let dll = join(work, 'sim/Suborned.Sim/bin/Release/netstandard2.1/Suborned.Sim.dll');
if (dllArg) { const given = join(root, dllArg); cpSync(given, join(cliDir, 'Suborned.Sim.dll')); cpSync(given, dll); dll = join(cliDir, 'Suborned.Sim.dll'); }
const dllSha = sha(readFileSync(dll));
if (sha(readFileSync(join(cliDir, 'Suborned.Sim.dll'))) !== dllSha) { console.error('the CLI copy of Suborned.Sim.dll differs from the build'); process.exit(1); }
const cli = (...a: string[]) => sh(['dotnet', join(cliDir, 'suborned-sim.dll'), ...a], work);

// 3. every scenario headless, through the same DLL
const ids = need(cli('list'), 'cli list').trim().split('\n').map(l => l.split('\t')[0]!).filter(Boolean);
type Played = { id: string; outcome: string; fail: string; tick: number; cash: number; trail: number; alert: number; startHash: string; finalHash: string; replayIdentical: boolean; checks: number; checksFailed: number; checkpoints: { tick: number; hash: string }[]; exit: number; error?: string };
const play = (what: string): Played => {
  const r = cli('play', what, '--json');
  try {
    const j = JSON.parse(r.out);
    const failed = (j.checks as { ok: boolean }[]).filter(c => !c.ok).length;
    return { id: j.id, outcome: j.outcome, fail: j.fail, tick: j.tick, cash: j.cash, trail: j.trail, alert: j.alert, startHash: j.startHash, finalHash: j.finalHash, replayIdentical: j.replayIdentical, checks: j.checks.length, checksFailed: failed, checkpoints: j.checkpoints, exit: r.code };
  } catch { return { id: what, outcome: 'error', fail: '', tick: 0, cash: 0, trail: 0, alert: 0, startHash: '', finalHash: '', replayIdentical: false, checks: 0, checksFailed: 0, checkpoints: [], exit: r.code, error: (r.err || r.out).trim().slice(0, 400) }; }
};
const scenarios = ids.map(play);
const routes: Played[] = [];
if (argv.includes('--routes')) {
  const dir = join(root, 'contracts/missions/handover/routes');
  for (const f of existsSync(dir) ? readdirSync(dir).filter(f => f.endsWith('.json') && f !== 'routes.json').sort() : []) routes.push({ ...play(join(dir, f)), id: f });
}

// 4. into Unity
const plugin = join(root, 'unity/Assets/Suborned/Sim/Plugins'), stream = join(root, 'unity/Assets/StreamingAssets/sim');
mkdirSync(plugin, { recursive: true }); mkdirSync(stream, { recursive: true });
cpSync(dll, join(plugin, 'Suborned.Sim.dll'));
const inputs: Record<string, string> = {};
for (const [name, rel] of Object.entries(INPUTS)) { const b = readFileSync(join(work, rel)); writeFileSync(join(stream, name), b); inputs[name] = sha(b); }
// 4b. the mission's words (Fable, suborned.text/1): host content, not an engine input. From the same source as the inputs (the commit, or
// the tree with --tree), validated by its own zod schema; absent, the host shows its placeholders and any stale copy is removed.
const TEXT = 'contracts/missions/handover/text.json', TEXT_SCHEMA = 'contracts/missions/handover/text.schema.ts';
let text = 'absent (the host shows its placeholders)';
{
  const at = (rel: string) => tree ? (existsSync(join(root, rel)) ? readFileSync(join(root, rel)) : null)
    : (sh(['git', 'cat-file', '-e', `HEAD:${rel}`]).code === 0 ? Buffer.from(need(sh(['git', 'show', `HEAD:${rel}`]), `git show ${rel}`)) : null);
  const body = at(TEXT), schema = at(TEXT_SCHEMA);
  if (body && schema) {
    const dir = join(work, 'text'); mkdirSync(join(dir, 'contracts/missions/handover'), { recursive: true });
    writeFileSync(join(dir, TEXT_SCHEMA), schema);
    // the schema imports the people rules and the rulebook ref from the same source tree
    for (const dep of ['contracts/people/people.rules.schema.ts', 'contracts/rules/rulebook.ref.ts', 'contracts/rules/rulebook.schema.ts']) {
      const b = at(dep); if (!b) { console.error(`${TEXT_SCHEMA} needs ${dep}, which is missing`); process.exit(1); }
      mkdirSync(join(dir, dep, '..'), { recursive: true }); writeFileSync(join(dir, dep), b);
    }
    const { TextFile } = await import(join(dir, TEXT_SCHEMA));
    const r = TextFile.safeParse(JSON.parse(body.toString('utf8')));
    if (!r.success) { console.error(`${TEXT} fails its schema:\n${r.error.message}`); process.exit(1); }
    if (depth(JSON.parse(body.toString('utf8'))) > MAX_DEPTH) { console.error(`${TEXT} nests deeper than ${MAX_DEPTH}`); process.exit(1); }
    writeFileSync(join(stream, 'text.json'), body); text = `text.json ${sha(body).slice(0, 12)} (suborned.text/1 parses)`;
  } else rmSync(join(stream, 'text.json'), { force: true });
}
const engine = { schema: 'suborned.sim.unity/1', validated: 'bun: zod schemas of the same source, rulebook refs, site schema id, nesting <= ' + MAX_DEPTH + '; CLI played every scenario', source: label, commit, tree, dirtyAtCommit: dirty, dll: 'Assets/Suborned/Sim/Plugins/Suborned.Sim.dll', dllSha256: dllSha, inputs, text, dotnet: need(sh(['dotnet', '--version']), 'dotnet --version').trim(), built: new Date().toISOString() };
writeFileSync(join(stream, 'engine.json'), JSON.stringify(engine, null, 1) + '\n');
writeFileSync(join(stream, 'headless.json'), JSON.stringify({ schema: 'suborned.sim.headless/1', engine: { commit, dllSha256: dllSha, inputs }, scenarios, routes }, null, 1) + '\n');

const bad = scenarios.filter(s => s.exit !== 0 || !s.replayIdentical);
console.log(`engine ${label}: Suborned.Sim.dll sha256 ${dllSha.slice(0, 12)} (${readFileSync(dll).length} bytes), inputs ${Object.entries(inputs).map(([k, v]) => `${k} ${v.slice(0, 12)}`).join(', ')}`);
console.log(`headless: ${scenarios.length - bad.length}/${scenarios.length} scenarios pass with identical replay${bad.length ? `; failing: ${bad.map(b => b.id).join(' ')}` : ''}`);
for (const r of routes) console.log(`route ${r.id}: ${r.outcome}${r.fail ? ` (${r.fail})` : ''} at tick ${r.tick}, trail ${r.trail}, final ${r.finalHash.slice(0, 12)}${r.error ? `, error ${r.error}` : ''}`);
console.log(`mission text: ${text}`);
console.log(`→ ${plugin.replace(root + '/', '')}/Suborned.Sim.dll, ${stream.replace(root + '/', '')}/ (engine.json, headless.json, ${Object.keys(INPUTS).length} inputs)`);
process.exit(bad.length ? 1 : 0);
