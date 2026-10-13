// The command line and the live guard (brief 007 F review, P5): an unknown option, a bad LEVELS_LOOP or --max-usd is an
// error before anything starts; a run that would reach the real model needs approval and, from an agent session, Sky's
// go-ahead; a truthy CLAUDE_BIN alone opens nothing. None of these starts a run or calls a model.
import { describe, expect, test } from 'bun:test';
import { existsSync } from 'node:fs';
import { mkdtemp } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { root } from '../../rag/config';
import { loopFrom, maxUsdFrom, parseArgs } from '../cli-args';

const stub = resolve(root, 'tools/levels/test/claude-stub.ts');
type Env = Record<string, string | undefined>;

/** `bun run levels ...` under `env` (the keys set to undefined are removed). */
async function levels(args: string[], env: Env) {
  const merged: Env = { ...process.env, ...env };
  for (const [k, v] of Object.entries(merged)) if (v === undefined) delete merged[k];
  const p = Bun.spawn(['bun', 'tools/levels/cli.ts', ...args], {
    cwd: root, env: merged as Record<string, string>, stdout: 'pipe', stderr: 'pipe',
  });
  const text = (s: ReadableStream) => new Response(s).text();
  const [stdout, stderr, code] = await Promise.all([text(p.stdout), text(p.stderr), p.exited]);
  return { stdout, stderr, code };
}

describe('the words of the command line', () => {
  test('known options parse; anything else is named', () => {
    const ok = parseArgs(['skyline', '--plan', 'x.json', '--rounds', '2', '--preview', '--max-usd', '3']);
    expect('error' in ok).toBe(false);
    const bad = [['skyline', '--previw'], ['skyline', '--rounds=1'], ['skyline', 'extra'], ['skyline', '--plan']];
    for (const args of bad) expect(parseArgs(args)).toHaveProperty('error');
    expect(parseArgs(['skyline', '--previw'])).toEqual({ error: expect.stringContaining('unknown option --previw') });
  });
  test('LEVELS_LOOP is 1 or 2 or unset; --max-usd is a positive number', () => {
    expect([loopFrom(undefined), loopFrom(''), loopFrom('1'), loopFrom('2')]).toEqual([undefined, undefined, 1, 2]);
    for (const bad of ['0', '3', 'two', '1.5']) expect(loopFrom(bad)).toHaveProperty('error');
    expect([maxUsdFrom(undefined, 10), maxUsdFrom('2.5', 10)]).toEqual([10, 2.5]);
    for (const bad of ['0', '-1', 'abc', 'Infinity']) expect(maxUsdFrom(bad, 10)).toHaveProperty('error');
  });
});

describe('the command refuses before a run starts', () => {
  const offline = { CLAUDE_BIN: stub, LEVELS_TEST_RETRIEVER: 'lexical' };
  test('an unknown option, a bad LEVELS_LOOP and a bad --max-usd exit 2 with the reason', async () => {
    const typo = await levels(['transit', '--previw'], offline);
    expect(typo.code).toBe(2); expect(typo.stderr).toContain('unknown option --previw');
    const loop = await levels(['transit'], { ...offline, LEVELS_LOOP: '3' });
    expect(loop.code).toBe(2); expect(loop.stderr).toContain('LEVELS_LOOP must be 1 or 2');
    const usd = await levels(['transit', '--max-usd', '0'], offline);
    expect(usd.code).toBe(2); expect(usd.stderr).toContain('--max-usd must be a positive number');
  });

  test('a truthy CLAUDE_BIN alone does not open the guard: an agent session is refused, nothing is written', async () => {
    const runs = join(await mkdtemp(join(tmpdir(), 'guard-')), 'runs');
    const r = await levels(['transit'], {
      CLAUDECODE: '1', CLAUDE_BIN: '/usr/bin/true', SUBORNED_LIVE: undefined, LEVELS_RUNS: runs,
      LEVELS_TEST_RETRIEVER: undefined,
    });
    expect(r.code).toBe(2); expect(r.stderr).toContain('refusing a live model call');
    expect(existsSync(runs)).toBe(false);
  });

  test('the refusal tells how to get a run approved, and carries no command that would bypass it', async () => {
    const r = await levels(['transit'], { CLAUDECODE: '1', CLAUDE_BIN: '/usr/bin/true', SUBORNED_LIVE: undefined });
    expect(r.stderr).not.toMatch(/SUBORNED_LIVE|CLAUDE_BIN=/);
    expect(r.stderr).toContain("Sky's");
  });

  test('a live run (the real model) needs --approved-by, even when the guard is opened', async () => {
    const runs = join(await mkdtemp(join(tmpdir(), 'approved-')), 'runs');
    const r = await levels(['transit'], {
      SUBORNED_LIVE: '1', CLAUDE_BIN: '/nonexistent/claude', LEVELS_RUNS: runs, LEVELS_TEST_RETRIEVER: undefined,
    });
    expect(r.code).toBe(2); expect(r.stderr).toContain('--approved-by');
    expect(existsSync(runs)).toBe(false);
  });
});
