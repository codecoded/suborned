import { mkdir, readFile, rm } from 'node:fs/promises';
import { isAbsolute, relative, resolve } from 'node:path';
import { hash } from '../rag/corpus';
import { CheckSchema, type CheckResult, type Scope } from './schema';

export type TrxResult = { total: number; executed: number; passed: number; failed: number; ok: boolean };
const invalid: TrxResult = { total: 0, executed: 0, passed: 0, failed: 0, ok: false };

/** Read only the TRX result summary, rejecting malformed XML rather than trusting a loose Counters match. */
export function parseTrx(xml: string, minTests: number): TrxResult {
  if (!Number.isSafeInteger(minTests) || minTests < 1 || /<!DOCTYPE|<!ENTITY/i.test(xml)) return { ...invalid };
  const stack: string[] = [];
  let rootSeen = false, rootClosed = false, counters: Record<string, string> | undefined, outcome: string | undefined;
  const tokens = xml.match(/<!--[\s\S]*?-->|<\?[\s\S]*?\?>|<(?:(?:"[^"]*")|(?:'[^']*')|[^'">])*>|[^<]+/g) ?? [];
  if (tokens.join('') !== xml) return { ...invalid };
  for (const token of tokens) {
    if (token.startsWith('<!--')) { if (token.slice(4, -3).includes('--')) return { ...invalid }; continue; }
    if (token.startsWith('<?')) { if (rootSeen || !/^<\?xml\s/.test(token)) return { ...invalid }; continue; }
    if (!token.startsWith('<')) {
      if ((!stack.length && token.trim()) || /&(?!amp;|lt;|gt;|quot;|apos;|#\d+;|#x[\da-fA-F]+;)/.test(token)) return { ...invalid };
      continue;
    }
    const close = /^<\/([\w:.-]+)\s*>$/.exec(token);
    if (close) { if (stack.pop() !== close[1]) return { ...invalid }; if (!stack.length) rootClosed = true; continue; }
    const open = /^<([A-Za-z_][\w:.-]*)([\s\S]*?)(\/?)>$/.exec(token);
    if (!open || rootClosed) return { ...invalid };
    const name = open[1]!, local = name.split(':').at(-1)!;
    const attrs: Record<string, string> = {};
    let rest = open[2]!;
    while (rest.trim()) {
      const attr = /^\s+([A-Za-z_][\w:.-]*)\s*=\s*(?:"([^"<]*)"|'([^'<]*)')/.exec(rest);
      if (!attr || Object.hasOwn(attrs, attr[1]!)) return { ...invalid };
      const value = attr[2] ?? attr[3]!;
      if (/&(?!amp;|lt;|gt;|quot;|apos;|#\d+;|#x[\da-fA-F]+;)/.test(value)) return { ...invalid };
      attrs[attr[1]!] = value; rest = rest.slice(attr[0].length);
    }
    if (!stack.length) { if (rootSeen || local !== 'TestRun') return { ...invalid }; rootSeen = true; }
    if (local === 'ResultSummary') {
      if (stack.length !== 1 || outcome !== undefined || !attrs.outcome) return { ...invalid };
      outcome = attrs.outcome;
    }
    if (local === 'Counters') {
      if (stack.length !== 2 || stack.at(-1)!.split(':').at(-1) !== 'ResultSummary' || counters) return { ...invalid };
      counters = attrs;
    }
    if (!open[3]) stack.push(name); else if (!stack.length) rootClosed = true;
  }
  if (!rootSeen || !rootClosed || stack.length || !counters) return { ...invalid };
  const count = (name: string): number => /^\d+$/.test(counters![name] ?? '') && Number.isSafeInteger(Number(counters![name])) ? Number(counters![name]) : NaN;
  const total = count('total'), executed = count('executed'), passed = count('passed'), failed = count('failed');
  if ([total, executed, passed, failed].some(n => !Number.isFinite(n))) return { ...invalid };
  // TRX has additional failure/skip counters; reject inconsistent summaries too.
  const other = ['error', 'timeout', 'aborted', 'inconclusive', 'passedButRunAborted', 'notRunnable', 'notExecuted', 'disconnected', 'warning', 'completed', 'inProgress', 'pending'];
  const clean = other.every(key => counters![key] === undefined || count(key) === 0);
  return { total, executed, passed, failed, ok: ['Completed', 'Passed'].includes(outcome ?? '') && clean && total >= minTests && executed === total && passed === total && failed === 0 };
}

function portablePath(root: string, path: string): string {
  const rel = relative(root, path);
  return rel !== '..' && !rel.startsWith('../') && !isAbsolute(rel) ? rel : path;
}

/** Each invocation starts from a fresh report and records the exact command, output and TRX. No model calls. */
export async function runChecks(root: string, scope: Scope, directory: string): Promise<CheckResult[]> {
  if (!isAbsolute(directory)) throw new Error('Check output directory must be absolute');
  const specs = scope.checks.map(spec => CheckSchema.parse(spec));
  if (new Set(specs.map(spec => spec.id)).size !== specs.length) throw new Error('Check IDs must be unique');
  await mkdir(directory, { recursive: true });
  const results: CheckResult[] = [];
  for (const spec of specs) {
    const outputFile = resolve(directory, `${spec.id}.log`), reportFile = resolve(directory, `${spec.id}.trx`);
    await rm(reportFile, { force: true });
    const argv = ['dotnet', 'test', scope.testProject, '--filter', spec.filter, '--logger', `trx;LogFileName=${reportFile}`, '--verbosity', 'minimal'];
    const start = Date.now();
    let exitCode = -1, timedOut = false, stdout = '', stderr = '';
    try {
      const child = Bun.spawn(argv, { cwd: root, stdout: 'pipe', stderr: 'pipe' });
      const timer = setTimeout(() => { timedOut = true; child.kill('SIGKILL'); }, 300000);
      try { [exitCode, stdout, stderr] = await Promise.all([child.exited, new Response(child.stdout).text(), new Response(child.stderr).text()]); }
      finally { clearTimeout(timer); }
    } catch (error) { stderr += `\nRunner error: ${error instanceof Error ? error.message : String(error)}`; }
    const output = `Command: ${JSON.stringify(argv)}\nExit: ${exitCode}; timedOut: ${timedOut}\n\nSTDOUT\n${stdout}\nSTDERR\n${stderr}\n`;
    await Bun.write(outputFile, output);
    const parsed = parseTrx(await readFile(reportFile, 'utf8').catch(() => ''), spec.minTests);
    results.push({ id: spec.id, argv, exitCode, timedOut, total: parsed.total, passed: parsed.passed, failed: parsed.failed,
      ok: exitCode === 0 && !timedOut && parsed.ok, outputFile: portablePath(root, outputFile), reportFile: portablePath(root, reportFile), outputHash: hash(output), seconds: (Date.now() - start) / 1000 });
  }
  return results;
}
