import { describe, expect, test } from 'bun:test';
import { parseTrx } from '../checks';

function report(counters = 'total="3" executed="3" passed="3" failed="0"', outcome = 'Completed') {
  return `<?xml version="1.0" encoding="utf-8"?>\n<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"><ResultSummary outcome="${outcome}"><Counters ${counters}/><Output><StdOut>check &amp; verify</StdOut></Output></ResultSummary></TestRun>`;
}

describe('TRX check evidence', () => {
  test('accepts complete passing runs with minimum test coverage', () => {
    expect(parseTrx(report(), 3)).toEqual({ total: 3, executed: 3, passed: 3, failed: 0, ok: true });
    expect(parseTrx(report().replace('failed="0"', "failed='0'"), 1).ok).toBe(true);
  });

  test('zero tests and too few matched tests cannot verify a requirement', () => {
    expect(parseTrx(report('total="0" executed="0" passed="0" failed="0"'), 1).ok).toBe(false);
    expect(parseTrx(report(), 4).ok).toBe(false);
    expect(parseTrx(report(), 0).ok).toBe(false);
  });

  test('skipped tests fail even if the completed tests passed', () => {
    const result = parseTrx(report('total="3" executed="2" passed="2" failed="0" notExecuted="1"'), 1);
    expect(result).toMatchObject({ total: 3, executed: 2, passed: 2, failed: 0, ok: false });
  });

  test('failed tests retain counters and fail the check', () => {
    expect(parseTrx(report('total="3" executed="3" passed="2" failed="1"', 'Failed'), 1)).toMatchObject({ failed: 1, ok: false });
  });

  test('aborted, error and inconsistent summary outcomes cannot pass', () => {
    for (const outcome of ['Failed', 'Aborted', 'NotExecuted', 'Unknown']) expect(parseTrx(report(undefined, outcome), 1).ok).toBe(false);
    for (const key of ['error', 'timeout', 'aborted', 'inconclusive', 'notExecuted', 'passedButRunAborted'])
      expect(parseTrx(report(`total="3" executed="3" passed="3" failed="0" ${key}="1"`), 1).ok).toBe(false);
    expect(parseTrx(report('total="3" executed="2" passed="3" failed="0"'), 1).ok).toBe(false);
  });

  test('rejects missing, nonnumeric, negative, fractional and overflowing counters', () => {
    for (const value of ['-1', 'NaN', 'Infinity', '3.1', '9007199254740993', '&#51;'])
      expect(parseTrx(report(`total="${value}" executed="3" passed="3" failed="0"`), 1).ok).toBe(false);
    expect(parseTrx(report('total="3" passed="3" failed="0"'), 1).ok).toBe(false);
  });

  test('rejects truncated, mismatched, duplicate and malformed XML', () => {
    const valid = report();
    for (const malformed of [
      '', '<Counters total="3" executed="3" passed="3" failed="0"/>',
      valid.slice(0, -10), valid.replace('</ResultSummary>', '</Wrong>'),
      valid + '<', valid + '<Other/>', valid.replace('total="3"', 'total=3'),
      valid.replace('total="3"', 'total="3" total="3"'), valid.replace(' &amp; ', ' & '),
      valid.replace('<Counters ', '<Wrong><Counters '), valid.replace('<Counters ', '<Counters bad '),
      valid.replace('</ResultSummary>', '<Counters total="3" executed="3" passed="3" failed="0"/></ResultSummary>'),
    ]) expect(parseTrx(malformed, 1).ok).toBe(false);
  });

  test('requires one root and one direct result summary; comment evidence is ignored', () => {
    expect(parseTrx(`<TestRun><!-- ${report()} --></TestRun>`, 1).ok).toBe(false);
    expect(parseTrx(report().replace('<ResultSummary', '<Wrapper><ResultSummary').replace('</ResultSummary>', '</ResultSummary></Wrapper>'), 1).ok).toBe(false);
    expect(parseTrx(report().replace('</TestRun>', '<ResultSummary outcome="Completed"/></TestRun>'), 1).ok).toBe(false);
  });

  test('rejects entity declarations rather than resolving untrusted XML', () => {
    expect(parseTrx(`<!DOCTYPE TestRun [<!ENTITY secret SYSTEM "file:///etc/passwd">]>${report()}`, 1).ok).toBe(false);
  });
});
