// The drift ledger: which rules a declared cause covers, and what replay says about a changed result (brief 007 F
// retest, N1). Pure: no run, no files.
import { describe, expect, test } from 'bun:test';
import { resolve } from 'node:path';
import { root } from '../../rag/config';
import { carryForward, declaredFor, parseRules, RULE_ID, withCause, type Ledger } from '../ledger';
import { codeLine, type CodeFacts } from '../replay-code';
import type { Code } from '../plugin';

const THEN = 'a'.repeat(64), NOW = 'b'.repeat(64);
const cause = (rules: string[], text = 'the cause') => ({ cause: text, rules });

function facts(ledger: Ledger, over: Partial<CodeFacts> = {}, moves = true): CodeFacts {
  const code: Code = {
    label: 'test code', hash: async () => NOW, ledger: () => ledger, ledgerFile: 'test/drift.json',
    declare: 'bun test/drift.ts declare', moves,
  };
  return {
    run: { toolsHash: THEN }, tools: NOW, code, allowDrift: false, reran: 1, rerunDiffers: true,
    added: new Set(), changed: new Set(['SK-1']), failing: new Set(), ...over,
  };
}
const ledger = (declared: Ledger['declared']): Ledger => ({ recorded: { [THEN.slice(0, 16)]: 'the run' }, declared });

describe('--rules', () => {
  test('a list of rule ids, or none', () => {
    expect(parseRules('SK-1,F-7, S-C1,W-1h,SK-1')).toEqual(['SK-1', 'F-7', 'S-C1', 'W-1h']);
    expect(parseRules('none')).toEqual([]);
  });
  test('missing, empty or not a rule id is a problem naming what is wrong', () => {
    expect(parseRules(undefined)).toHaveProperty('error', expect.stringContaining('--rules is required'));
    expect(parseRules('  ')).toHaveProperty('error');
    expect(parseRules('SK-1,the walk,sk-2')).toHaveProperty('error', expect.stringContaining('the walk, sk-2'));
  });
});

describe('declared causes', () => {
  test('a cause declared again names the union of its rules; another cause is added', () => {
    const l = ledger({});
    withCause(l, NOW, 'moved SK-1', ['SK-1']);
    withCause(l, NOW, 'moved SK-1', ['C-0']);
    withCause(l, NOW, 'moved F-7', ['F-7']);
    expect(declaredFor(l, NOW)).toEqual([cause(['SK-1', 'C-0'], 'moved SK-1'), cause(['F-7'], 'moved F-7')]);
  });
  test('a bare string is a cause written before rules were named: it covers none', () => {
    expect(declaredFor(ledger({ [NOW]: ['old cause'] }), NOW)).toEqual([cause([], 'old cause')]);
  });
});

describe('carrying causes forward', () => {
  const MID = 'c'.repeat(64);
  test('a new hash gets every cause declared for an earlier hash, rules merged, and the new cause comes last', () => {
    const l = ledger({ [THEN]: [cause(['P-6'], 'first'), cause([], 'second')], [MID]: [cause(['P-7'], 'first'), cause(['P-11'], 'third'), 'bare cause'] });
    expect(carryForward(l, NOW)).toBe(4);
    withCause(l, NOW, 'fourth', ['P-12']);
    expect(declaredFor(l, NOW)).toEqual([cause(['P-6', 'P-7'], 'first'), cause([], 'second'), cause(['P-11'], 'third'), cause([], 'bare cause'), cause(['P-12'], 'fourth')]);
    expect(declaredFor(l, THEN)).toEqual([cause(['P-6'], 'first'), cause([], 'second')]); // the earlier hashes keep their own lists
  });
  test('carrying twice, or into a hash that already has some of them, adds nothing twice', () => {
    const l = ledger({ [THEN]: [cause(['P-6'], 'first')], [NOW]: [cause(['P-9'], 'first'), cause([], 'own')] });
    expect(carryForward(l, NOW)).toBe(0);
    expect(declaredFor(l, NOW)).toEqual([cause(['P-9', 'P-6'], 'first'), cause([], 'own')]);
    expect(carryForward(l, NOW)).toBe(0);
  });
  test('a ledger with no earlier hash carries nothing and writes no empty entry', () => {
    const l = ledger({});
    expect(carryForward(l, NOW)).toBe(0);
    expect(l.declared[NOW]).toBeUndefined();
  });
});

describe('the committed ledgers', () => {
  test('every declared cause names the rules it covered, as rule ids (none is an empty list, never a bare string)', async () => {
    for (const file of ['tools/levels/drift.json', 'tools/levels/skyline/drift.json']) {
      const ledger: Ledger = await Bun.file(resolve(root, file)).json();
      for (const hash of Object.keys(ledger.declared)) {
        expect(hash, file).toMatch(/^[0-9a-f]{64}$/);
        for (const raw of ledger.declared[hash]!) expect(typeof raw, `${file} ${hash.slice(0, 12)}`).toBe('object');
        for (const c of declaredFor(ledger, hash)) for (const id of c.rules) expect(id).toMatch(RULE_ID);
      }
    }
  });
});

describe('replay of a changed result', () => {
  test('a rule a declared cause names is labelled by the cause, and says which now fail', () => {
    const l = ledger({ [NOW]: [cause(['SK-1', 'F-7'])] });
    const r = codeLine(facts(l, { changed: new Set(['SK-1', 'F-7']), failing: new Set(['F-7']) }));
    expect(r.failed).toBe(false);
    expect(r.line).toContain('the cause [covers SK-1, F-7]');
    expect(r.line).toContain('rules whose result differs: SK-1, F-7 (now fails)');
  });
  test('a rule no declared cause names fails, however many causes are declared (the N1 probe)', () => {
    const l = ledger({ [NOW]: [cause(['SK-1']), cause(['F-7'], 'another')] });
    const r = codeLine(facts(l, { changed: new Set(['SK-1', 'C-0']) }));
    expect(r.failed).toBe(true);
    expect(r.line).toContain('no declared cause names them: C-0');
    expect(r.line).toContain('declared: SK-1, F-7');
    expect(r.line).toContain('bun test/drift.ts declare "<cause>" --rules <ids>');
  });
  test('a cause that covers no rule does not cover a changed one', () => {
    const r = codeLine(facts(ledger({ [NOW]: ['old cause', cause([], 'none')] })));
    expect(r.failed).toBe(true);
    expect(r.line).toContain('no declared cause names them: SK-1; declared: no rule');
  });
  test('the run needs a recorded state and the code a declared cause, as before', () => {
    const unknown = codeLine(facts({ recorded: {}, declared: { [NOW]: [cause(['SK-1'])] } }));
    expect(unknown.failed).toBe(true); expect(unknown.line).toContain('recorded hash is not a state');
    const bare = codeLine(facts(ledger({})));
    expect(bare.failed).toBe(true); expect(bare.line).toContain('current code hash not declared');
  });
  test('drift allowed passes it with a warning; code that does not move never labels a changed result', () => {
    const l = ledger({ [NOW]: [cause(['SK-1'])] });
    expect(codeLine(facts(l, { allowDrift: true, changed: new Set(['C-0']) }))).toMatchObject({ failed: false });
    expect(codeLine(facts(l, {}, false))).toMatchObject({ failed: true });
  });
  test('a result that reruns identically needs no rule named', () => {
    const l = ledger({ [NOW]: [cause([], 'no rule moved')] });
    const r = codeLine(facts(l, { rerunDiffers: false, changed: new Set() }));
    expect(r.failed).toBe(false);
    expect(r.line).toContain('every rule the run recorded reruns identically');
  });
});
