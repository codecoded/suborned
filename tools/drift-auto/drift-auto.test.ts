// drift:auto (brief 034 item 2). The pure reading of replay lines (built from the real codeLine, so a change to its wording breaks this test, not the
// tool silently), and an end-to-end run on a scratch copy of the repo: a harmless edit to code the dialogue verdict rests on is declared with the
// file named, an edit that changes what a rule says is refused with the rule shown, and --check writes nothing.
// Run: bun test tools/drift-auto
import { afterAll, describe, expect, setDefaultTimeout, test } from 'bun:test';
import { cpSync, existsSync, mkdirSync, readFileSync, rmSync, symlinkSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { codeLine } from '../gen/replay-code';
import type { Code } from '../gen/plugin';
import { root } from '../rag/config';
import { causeText, codeFinding, diffSignatures, judgeRun, signature } from './core';

setDefaultTimeout(600_000);
const h = (c: string) => c.repeat(64), NOW = h('a'), THEN = h('b');
const code = (declared: Record<string, ({ cause: string; rules: string[] })[]>, moves = true): Code => ({ label: 'x', hash: async () => NOW, ledger: () => ({ recorded: { [THEN.slice(0, 16)]: 'run' }, declared }), ledgerFile: 'tools/levels/people/drift.json', declare: 'bun tools/levels/people/drift.ts declare', moves });
const line = (c: Code, f: { rerunDiffers?: boolean; changed?: string[]; run?: string } = {}) => codeLine({ run: { toolsHash: f.run ?? THEN }, tools: NOW, code: c, allowDrift: false, reran: 1, rerunDiffers: !!f.rerunDiffers, added: new Set(), changed: new Set(f.changed ?? []), failing: new Set() }).line;

describe('reading the code line of a replay', () => {
  test('a hash no cause is declared for, with a rule moved', () => {
    const f = codeFinding([line(code({}), { rerunDiffers: true, changed: ['P-6'] })]);
    expect(f).toMatchObject({ kind: 'failed', undeclared: true, ledgerFile: 'tools/levels/people/drift.json' });
  });
  test('a rule moved that the declared causes do not name: the rule is listed', () => {
    const f = codeFinding([line(code({ [NOW]: [{ cause: 'old', rules: ['P-1'] }] }), { rerunDiffers: true, changed: ['P-6', 'P-7'] })]);
    expect(f).toMatchObject({ kind: 'failed', uncovered: ['P-6', 'P-7'] });
  });
  test('a run whose own recorded state is not in the ledger is not a declaration problem', () => {
    expect(codeFinding([line(code({}), { run: h('c'), rerunDiffers: true, changed: ['P-6'] })]).unknownRecorded).toBe(true);
  });
  test('labelled drift: identical, or a moved rule a cause names', () => {
    expect(codeFinding([line(code({}))])).toMatchObject({ kind: 'labelled', identical: true, differs: [] });
    const f = codeFinding([line(code({ [NOW]: [{ cause: 'c', rules: ['P-6'] }] }), { rerunDiffers: true, changed: ['P-6'] })]);
    expect(f).toMatchObject({ kind: 'labelled', identical: false, differs: ['P-6'] });
  });
});

describe('comparing a replay with the base state\'s', () => {
  const run = (lines: string[], ok = true) => ({ id: 'r016', objective: 'people', lines, ok });
  const body = ['ok    GDD v0.22.0', 'warn  corpus hash changed: recorded 0123456789abcdef, now fedcba9876543210', 'ok    outcome ACCEPTED'];
  test('hashes and the wording of the code line do not count; the rules that moved do', () => {
    const base = run([...body.slice(0, 1), line(code({ [NOW]: [{ cause: 'standing', rules: ['P-6'] }] }), { rerunDiffers: true, changed: ['P-6'] }), body[2]!]);
    const same = run([...body.slice(0, 1), line(code({ [NOW]: [{ cause: 'standing', rules: ['P-6'] }, { cause: 'new cause', rules: [] }] }), { rerunDiffers: true, changed: ['P-6'] }).replace(NOW.slice(0, 12), 'ffffffffffff'), body[2]!]);
    expect(judgeRun(same, base).moved).toBe(false);
    expect(signature(base.lines)[1]).toBe('CODE differs: P-6');
    expect(signature(run(body).lines)[1]).toBe('warn  corpus hash changed: recorded #, now #');
  });
  test('an extra moved rule is a move, and the diff names it', () => {
    const base = run([line(code({}))]);
    const now = run([line(code({ [NOW]: [{ cause: 'c', rules: ['P-9'] }] }), { rerunDiffers: true, changed: ['P-9'] })]);
    const v = judgeRun(now, base);
    expect(v.moved).toBe(true); expect(v.diff).toEqual({ onlyBase: ['CODE identical'], onlyNow: ['CODE differs: P-9'] });
  });
  test('a replay that still fails with the declaration in place is a move', () => {
    const v = judgeRun(run([line(code({}), { rerunDiffers: true, changed: ['P-6'] })], false), run([line(code({}))]));
    expect(v.moved).toBe(true); expect(v.failLines?.[0]).toContain('CHECKER CODE changed');
  });
  test('a run the base does not have only has to pass', () => { expect(judgeRun(run(['ok    x']), undefined).moved).toBe(false); });
  test('diffSignatures is a multiset difference', () => { expect(diffSignatures(['a', 'a', 'b'], ['a', 'c'])).toEqual({ onlyBase: ['a', 'b'], onlyNow: ['c'] }); });
});

test('the cause names the files behind the change', () => {
  const t = causeText('dialogue', 'HEAD abc123', ['tools/dialogue/check.ts', 'tools/dialogue/rules.ts'], 3);
  expect(t).toContain('tools/dialogue/check.ts'); expect(t).toContain('2 files'); expect(t).toContain('3 recorded dialogue runs');
  expect(causeText('x', 'b', Array.from({ length: 12 }, (_, i) => `f${i}.ts`), 1)).toContain('and 4 more');
});

// ---------------------------------------------------------------- end to end, on a copy of the repo (this tree's tools, contracts, runs, GDD; its own git)
describe('end to end on a scratch copy', () => {
  const box = resolve(root, 'out/drift-auto-test/box');
  const sh = (cmd: string[], cwd = box) => { const r = Bun.spawnSync(cmd, { cwd, stdout: 'pipe', stderr: 'pipe' }); return { code: r.exitCode, out: r.stdout.toString() + r.stderr.toString() }; };
  const cli = (...a: string[]) => sh(['bun', 'tools/drift-auto/cli.ts', '--objective', 'dialogue', ...a]); // dialogue's 3 runs: fast; the unfiltered pass is the first test below
  const ledger = () => readFileSync(resolve(box, 'tools/levels/dialogue/drift.json'), 'utf8');
  const edit = (rel: string, f: (s: string) => string) => writeFileSync(resolve(box, rel), f(readFileSync(resolve(box, rel), 'utf8')));
  afterAll(() => rmSync(box, { recursive: true, force: true })); // a 100 MB copy
  const original = new Map<string, string>();
  const keep = (rel: string) => { if (!original.has(rel)) original.set(rel, readFileSync(resolve(box, rel), 'utf8')); };
  const restore = () => { for (const [rel, text] of original) writeFileSync(resolve(box, rel), text); };

  test('set up the copy', () => {
    rmSync(box, { recursive: true, force: true }); mkdirSync(box, { recursive: true });
    for (const p of ['tools', 'contracts', 'runs', 'GDD', '.claude/agents']) cpSync(resolve(root, p), resolve(box, p), { recursive: true, filter: s => !/\/node_modules(\/|$)/.test(s) });
    mkdirSync(resolve(box, 'sim'), { recursive: true }); mkdirSync(resolve(box, '.claude'), { recursive: true }); cpSync(resolve(root, 'sim/data'), resolve(box, 'sim/data'), { recursive: true });
    for (const f of ['package.json', 'tsconfig.json', 'bunfig.toml']) cpSync(resolve(root, f), resolve(box, f));
    symlinkSync(resolve(root, 'node_modules'), resolve(box, 'node_modules'));
    writeFileSync(resolve(box, '.gitignore'), 'node_modules\nout\n');
    for (const c of [['init', '-q'], ['add', '-A'], ['-c', 'user.name=t', '-c', 'user.email=t@t', 'commit', '-q', '-m', 'base']]) expect(sh(['git', ...c]).code).toBe(0);
    expect(existsSync(resolve(box, 'tools/drift-auto/cli.ts'))).toBe(true);
  });
  test('nothing changed: nothing to declare (every objective, every run)', () => {
    const r = sh(['bun', 'tools/drift-auto/cli.ts', '--check']);
    expect(r.out).toContain('nothing to declare'); expect(r.code).toBe(0);
  });
  test('an edit that changes what D-7 says is refused, with the rule shown, and nothing is written', () => {
    keep('tools/dialogue/check.ts');
    edit('tools/dialogue/check.ts', s => s.replace("out.push({ id: 'D-7', rule: RULES['D-7']!, bad });", "out.push({ id: 'D-7', rule: RULES['D-7']!, bad: [...bad, 'drift-auto test'] });"));
    const before = ledger(), r = cli();
    expect(r.out).toContain('CHECKER CODE changed fails dialogue'); expect(r.out).toContain('REFUSED'); expect(r.out).toContain('tools/dialogue/check.ts'); expect(r.out).toContain('checker rerun differs from checks.json');
    expect(r.code).toBe(1); expect(ledger()).toBe(before);
    restore();
  });
  test('an edit to the people checker that moves P-3 is refused with the rule named', () => {
    keep('tools/people/check.ts');
    edit('tools/people/check.ts', s => s.replace("bad: [...new Set(bad)], value: `${cast.rows.length} rows, ${P.length} people` });", "bad: [...new Set(bad), 'probe'], value: `${cast.rows.length} rows, ${P.length} people` });"));
    const ledgers = ['tools/levels/people/drift.json', 'tools/levels/drift.json', 'tools/levels/skyline/drift.json'].map(f => readFileSync(resolve(box, f), 'utf8'));
    const r = sh(['bun', 'tools/drift-auto/cli.ts', '--objective', 'people']);
    expect(r.out).toContain('REFUSED'); expect(r.out).toContain('tools/people/check.ts'); expect(r.out).toMatch(/rules whose result differs and no declared cause names: .*P-3/); expect(r.code).toBe(1);
    expect(['tools/levels/people/drift.json', 'tools/levels/drift.json', 'tools/levels/skyline/drift.json'].map(f => readFileSync(resolve(box, f), 'utf8'))).toEqual(ledgers);
    restore();
  });
  test('a harmless edit: --check names the file and writes nothing; the real run declares it and the replays pass', () => {
    keep('tools/dialogue/rules.ts');
    edit('tools/dialogue/rules.ts', s => s + '\n// drift-auto test: a comment moves the hash and nothing else\n');
    const before = ledger(), c = cli('--check');
    expect(c.out).toContain('tools/dialogue/rules.ts'); expect(c.out).toContain('not written'); expect(c.code).toBe(1); expect(ledger()).toBe(before);
    const r = cli();
    expect(r.code).toBe(0); expect(r.out).toContain('declared in tools/levels/dialogue/drift.json'); expect(r.out).toMatch(/confirmed: 3\/3 runs of dialogue replay OK/);
    expect(ledger()).not.toBe(before); expect(ledger()).toContain('drift:auto dialogue: 1 file changed since');
    const again = cli('--check');
    expect(again.out).toContain('nothing to declare'); expect(again.code).toBe(0);
    restore();
  });
});
