import { expect, test } from 'bun:test';
import { checkFlags } from './flags';

const spec = { flags: ['--build', '--dry-run'], valued: [{ flag: '--send' }, { flag: '--capture-size', required: true }] };

test('--help and -h stop the run, anywhere in the arguments', () => {
  expect(checkFlags(['--help'], spec)).toEqual({ run: false, reason: 'help' });
  expect(checkFlags(['-h'], spec)).toEqual({ run: false, reason: 'help' });
  expect(checkFlags(['--build', '--help'], spec)).toEqual({ run: false, reason: 'help' });
});
test('an unknown flag or stray word stops the run and is named', () => {
  expect(checkFlags(['--bogus'], spec)).toEqual({ run: false, reason: 'unknown', unknown: '--bogus' });
  expect(checkFlags(['--build', '-x'], spec)).toEqual({ run: false, reason: 'unknown', unknown: '-x' });
  expect(checkFlags(['stray'], spec)).toEqual({ run: false, reason: 'unknown', unknown: 'stray' });
});
test('known flags and their values run; a required value that is missing does not', () => {
  expect(checkFlags([], spec)).toEqual({ run: true });
  expect(checkFlags(['--build', '--dry-run'], spec)).toEqual({ run: true });
  expect(checkFlags(['--send'], spec)).toEqual({ run: true });
  expect(checkFlags(['--send', 'skys-macbook-air-1', '--dry-run'], spec)).toEqual({ run: true });
  expect(checkFlags(['--capture-size', '1920x1080'], spec)).toEqual({ run: true });
  expect(checkFlags(['--capture-size'], spec)).toMatchObject({ run: false, reason: 'unknown' });
  expect(checkFlags(['--capture-size', '--build'], spec)).toMatchObject({ run: false, reason: 'unknown' });
});
