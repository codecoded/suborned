// bandFor (brief 066): the storey band a building draws from, under an area cap with or without its own storey band.
import { expect, test } from 'bun:test';
import { bandFor, heightOf, storeysUnder } from './common';

const B = { plinth: 0.45, groundStorey: 5.2, upperStorey: 3.6, roofCrown: 3.95, bands: { residential: { storeys: [4, 9], mode: 6 }, kiosk: { storeys: [1, 2], mode: 1 } }, caps: {} as any };

test('no cap: the use band as it is', () => expect(bandFor(B, 'residential', 'x')).toEqual({ lo: 4, hi: 9, mode: 6, capped: false }));
test('a metres cap clips the top of the use band (today\'s records 38, lamplight 31.2)', () => {
  const b = { ...B, caps: { lamplight: { m: 31.2 } } };
  expect(bandFor(b, 'residential', 'lamplight')).toEqual({ lo: 4, hi: storeysUnder(B, 31.2), mode: 6, capped: false });
});
test('a play-street cap with its own band replaces a use band that reaches higher', () => {
  const b = { ...B, caps: { records: { storeys: [3, 4], mode: 3, m: 20.4 } } };
  expect(heightOf(B, 4)).toBeCloseTo(20.4, 6);
  expect(bandFor(b, 'residential', 'records')).toEqual({ lo: 3, hi: 4, mode: 3, capped: true });
  expect(bandFor(b, 'kiosk', 'records').capped).toBe(false); // a band already under the cap's keeps its own
});
test('the cap band\'s top is held under its metres too', () => {
  const b = { ...B, caps: { records: { storeys: [3, 5], mode: 5, m: 20.4 } } };
  expect(bandFor(b, 'residential', 'records')).toEqual({ lo: 3, hi: 4, mode: 4, capped: true });
});
