import { describe, expect, test } from 'bun:test';
import { cmpVersion, missingFlags, parseVersion, portOwner } from './doctor';

describe('doctor', () => {
  test('version compare is numeric, not lexical', () => {
    expect(cmpVersion('1.10.0', '1.4.0')).toBeGreaterThan(0);
    expect(cmpVersion('1.4', '1.4.0')).toBe(0);
    expect(cmpVersion('2.1.295', '2.1.296')).toBeLessThan(0);
    expect(parseVersion('2.1.296 (Claude Code)')).toBe('2.1.296');
    expect(parseVersion('nope')).toBeNull();
  });
  test('port owner: ours, other, none', () => {
    const ps = ['127.0.0.1:18504->8000/tcp|suborned', '127.0.0.1:18505->8080/tcp|other', '0.0.0.0:3000->3000/tcp|'];
    expect(portOwner(ps, 18504)).toBe('ours');
    expect(portOwner(ps, 18505)).toBe('other');
    expect(portOwner(ps, 3000)).toBe('other');
    expect(portOwner(ps, 18506)).toBe('none');
    expect(portOwner(['127.0.0.1:118504->1/tcp|suborned'], 18504)).toBe('none');
  });
  test('missing flags are reported', () => {
    expect(missingFlags('--json-schema --effort', ['--json-schema', '--tools'])).toEqual(['--tools']);
  });
});
