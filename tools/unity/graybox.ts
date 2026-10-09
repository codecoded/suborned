// Rebuilds the Harbour District graybox from the plan, end to end, and stops at the first failed step.
//   bun run graybox [--no-shots]
// 1 `bun run world` (exits 1 unless the plan passes), then V-1's per-point occluders → out/unity/v1.json · 2 build from a clean scene, dump · 3 again, dump · 4 the two dumps
// must be identical · 5 shots + camera study (needs a GPU, so no -nographics; fails if the study disagrees with V-1). Logs and dumps → out/unity/.
// Every step's exit code is checked; Unity logs are also scanned for compile errors and the builder's own checks.
// Batchmode needs the project closed. With the editor open on unity/, drive the same sequence through the Unity MCP
// (this script can't): first confirm no dirty scene and not in play mode (else stop; Build's save prompt would block
// the editor) · `bun run world` · `bun tools/unity/v1.ts` · refresh + compile · menu Suborned/Graybox/Build Harbour District · Inspect.Dump · Build
// · Dump · diff the dumps · Shots.Render · Study.Render · console 0 errors · leave HarbourDistrict open and saved.
import { $ } from 'bun';
import { existsSync, mkdirSync, readFileSync, rmSync } from 'node:fs';
import { join } from 'node:path';
import { writeV1 } from './v1';

const repo = join(import.meta.dir, '..', '..');
const project = join(repo, 'unity');
const logs = join(repo, 'out', 'unity');
const scene = join(project, 'Assets', 'Scenes', 'HarbourDistrict.unity');
const version = /m_EditorVersion: (\S+)/.exec(readFileSync(join(project, 'ProjectSettings', 'ProjectVersion.txt'), 'utf8'))![1];
const unity = process.env.UNITY ?? `/Applications/Unity/Hub/Editor/${version}/Unity.app/Contents/MacOS/Unity`;
const shots = !process.argv.includes('--no-shots');

function fail(step: string, why: string): never {
  console.error(`graybox: ${step} failed: ${why}`);
  process.exit(1);
}

/** Runs one batchmode method; fails on a non-zero exit, a compile error, or a missing success line. */
async function run(step: string, method: string, opts: { gpu?: boolean; args?: string[]; expect?: string } = {}) {
  const log = join(logs, `${step}.log`);
  const args = ['-batchmode', ...(opts.gpu ? [] : ['-nographics']), '-quit', '-projectPath', project,
    '-executeMethod', method, ...(opts.args ?? []), '-logFile', log];
  const code = (await Bun.spawn([unity, ...args], { stdout: 'ignore', stderr: 'ignore' }).exited);
  const text = existsSync(log) ? readFileSync(log, 'utf8') : '';
  if (code !== 0) fail(step, `Unity exit ${code}; see ${log}`);
  if (/error CS\d+/.test(text)) fail(step, `compile errors in ${log}`);
  if (opts.expect && !text.includes(opts.expect)) fail(step, `"${opts.expect}" not in ${log}`);
  const warnings = text.split('\n').filter((l) => /^Graybox: .*(repairs|fallbacks|skipped|too small)/.test(l));
  for (const w of warnings) console.warn(`  warning: ${w}`);
  console.log(`graybox: ${step} ok (${log})`);
  return text;
}

mkdirSync(logs, { recursive: true });
const world = await $`bun run world`.cwd(repo).quiet().nothrow();
process.stdout.write(world.stdout);
if (world.exitCode !== 0) fail('world', `bun run world exit ${world.exitCode}\n${world.stderr}`);
const v1 = join(logs, 'v1.json');
const v1Points = writeV1('contracts/world/harbour-district.plan.json', v1);
console.log(`graybox: V-1 ${v1Points.filter(p => !p.blockers.length).length}/${v1Points.length} unoccluded → ${v1}`);

const dumps: string[] = [];
for (const i of [1, 2]) {
  rmSync(scene, { force: true });
  rmSync(`${scene}.meta`, { force: true });
  const text = await run(`build-${i}`, 'Suborned.Graybox.WorldBuilder.Build', { expect: 'Graybox: every layer count matches the export' });
  if (i === 1) console.log(`  ${/Graybox: world .*/.exec(text)?.[0]}`);
  const dump = join(logs, `hierarchy-${i}.txt`);
  await run(`dump-${i}`, 'Suborned.Graybox.Inspect.Dump', { args: ['-dump', dump], expect: 'Graybox: dumped' });
  dumps.push(readFileSync(dump, 'utf8'));
}
if (dumps[0] !== dumps[1]) fail('compare', 'hierarchy-1.txt and hierarchy-2.txt differ');
console.log(`graybox: two clean builds identical (${dumps[0].length} bytes)\n  ${dumps[0].split('\n').slice(0, 2).join('\n  ')}`);

if (shots) {
  await run('shots', 'Suborned.Graybox.Shots.Render', { gpu: true, expect: 'play-handover-wide.png' });
  const study = await run('study', 'Suborned.Graybox.Study.Render', { gpu: true, args: ['-v1', v1], expect: 'Graybox: study at yaw' });
  console.log(`  ${/Graybox: study vs V-1[^\n]*\n(?:  .*\n)*/.exec(study)?.[0].trimEnd().replace(/\n/g, '\n  ') ?? ''}`);
}
