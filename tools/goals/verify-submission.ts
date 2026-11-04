// Reviewer entry point: fixed offline checks only; never invokes a model or database.
import { mkdir, readFile } from 'node:fs/promises';
import { resolve, relative } from 'node:path';
import { root } from '../rag/config';
import { parseTrx } from './checks';
import { verifyCampaign } from './verify-campaign';
import { verificationHtml } from './verification-report';

const completedRun = 'runs/goals/2026-10-19T11-05-43-376Z-discovery';
const rerun = 'runs/goals/2026-10-19T11-14-05-156Z-discovery';
const startedAt = new Date().toISOString();
const directory = resolve(root, 'out/goals/submission', startedAt.replace(/[:.]/g, '-'));
const stages: { name: string; ok: boolean; seconds: number; result?: unknown; error?: string }[] = [];

async function command(name: string, argv: string[]) {
  const child = Bun.spawn(argv, { cwd: root, env: { ...process.env, SUBORNED_LIVE: '0', GOALS_DOTNET_TESTS: '1' }, stdout: 'pipe', stderr: 'pipe' });
  let timedOut = false;
  const timer = setTimeout(() => { timedOut = true; child.kill('SIGKILL'); }, 600_000);
  let exitCode: number, stdout: string, stderr: string;
  try { [exitCode, stdout, stderr] = await Promise.all([child.exited, new Response(child.stdout).text(), new Response(child.stderr).text()]); }
  finally { clearTimeout(timer); }
  const log = resolve(directory, `${name}.log`);
  await Bun.write(log, `Command: ${JSON.stringify(argv)}\nExit: ${exitCode}; timedOut: ${timedOut}\n\n${stdout}\n${stderr}`);
  if (exitCode !== 0 || timedOut) throw new Error(`${name} failed; see ${relative(root, log)}`);
  return { argv, exitCode, log: relative(root, log) };
}

async function stage(name: string, work: () => Promise<unknown>) {
  console.log(`[${stages.length + 1}/6] ${name} …`);
  const start = Date.now();
  try {
    const result = await work();
    stages.push({ name, ok: true, seconds: (Date.now() - start) / 1000, result });
    console.log(`PASS ${name}`);
  } catch (error) {
    stages.push({ name, ok: false, seconds: (Date.now() - start) / 1000, error: String(error) });
    throw error;
  }
}

async function replay(path: string) {
  const result = await verifyCampaign(root, path);
  if (!result.ok || !result.complete) throw new Error(`Retained campaign did not verify COMPLETE: ${path}`);
  return result;
}

await mkdir(directory, { recursive: true });
console.log('Assignment 5 offline verification — no model calls, database or Unity.');
console.log('Archived baseline acceptance failures are expected; the generated patch must turn them green.');
try {
  await stage('typecheck', () => command('typecheck', ['bun', 'run', 'typecheck']));
  await stage('tooling-tests', () => command('tooling-tests', ['bun', 'test', 'tools/goals/test', 'tools/rag/rag.test.ts', 'tools/crew']));
  await stage('crew-cards', () => command('crew-cards', ['bun', 'run', 'crew', '--check']));
  await stage('submitted-simulation', async () => {
    const trx = resolve(directory, 'submitted-simulation.trx');
    const execution = await command('submitted-simulation', ['dotnet', 'test', 'sim/Suborned.Sim.Tests/Suborned.Sim.Tests.csproj', '--logger', `trx;LogFileName=${trx}`, '--verbosity', 'minimal']);
    const tests = parseTrx(await readFile(trx, 'utf8'), 871);
    if (!tests.ok) throw new Error(`Expected at least 871 passing simulation tests; see ${trx}`);
    return { ...execution, tests };
  });
  await stage('generated-feature-replay', () => replay(completedRun));
  await stage('unchanged-rerun-replay', () => replay(rerun));
} catch (error) {
  console.error(String(error));
  process.exitCode = 1;
} finally {
  const ok = stages.length === 6 && stages.every(stage => stage.ok);
  const report = { schema: 'suborned.a05.submission-verification/1', ok, startedAt, finishedAt: new Date().toISOString(), modelCalls: 0, stages };
  await Bun.write(resolve(directory, 'verification.json'), JSON.stringify(report, null, 2) + '\n');
  await Bun.write(resolve(directory, 'index.html'), verificationHtml(report));
  console.log(`\nASSIGNMENT 5 OFFLINE VERIFICATION ${ok ? 'PASSED' : 'FAILED'}`);
  if (ok) console.log('Submitted simulation: at least 871 passing tests. Archived feature: 5 red → 5 green; 866 regressions. Retained rerun: verified.');
  console.log(`Your verification report: ${relative(root, directory)}/index.html`);
  console.log(`JSON result and logs: ${relative(root, directory)}/verification.json`);
}
