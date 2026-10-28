// ZK-1, the Handover zone keeps its pieces (brief 075 stable zone, Aster 2026-10-29: "no change outside the zone can touch anything inside it"). Every street item in the
// zone (standards.blocks.zone) that takes a kit piece by choice takes the piece its pin names (contracts/world/<district>.zone-kit.json, bun run world:pin-zone),
// every pin finds its item (same kind and edge, same place to the cm), and no pinned piece has stopped fitting its slot. A miss means the zone itself changed (an item
// moved, came or went) or a piece was retired: re-pin only once that change is signed off. The ids the zone's items take are kept apart from the district's by the
// street layer (gen/street/kit.ts: `<kind>-z<n>`), and the zone chooses its pieces before the rest (kitrefs.ts streetKit). Tests: zonekit.test.ts (a failing example
// each: a pin with no item, an item with no pin, a piece that no longer fits) and zone-stable.test.ts (the zone byte for byte with an extra gate and without S2).
import type { Check } from '../../plan/checks';
import { type WCtx, rule } from '../../plan/world-ctx';
import type { PinLog } from '../gen/kitrefs';

const SOURCE = 'brief 075 stable zone (Aster 2026-10-29; Datum\'s S2 renumbered the zone and swapped 8 lamps, moving the sim walk grid 10 cells) · standards.blocks.zone · contracts/world/<district>.zone-kit.json · tools/world/rules/zonekit.ts · failing examples (zonekit.test.ts)';

export function zoneKitReport(log: PinLog | undefined): { checked: number; bad: string[] } {
  if (!log?.file) return { checked: 0, bad: [] };
  const bad = [...log.unmatched.map(k => `pin ${k} finds no item`), ...log.unpinned.map(id => `${id} has no pin`), ...log.refit.map(x => `${x}: the pinned piece no longer fits`)];
  return { checked: log.used + log.unmatched.length + log.unpinned.length + log.refit.length, bad };
}

export function zoneKitRules(c: WCtx): Check[] {
  const log = c.gen.zonePins;
  if (!log?.zone) return [];
  if (!log.file) return [{ ...rule('ZK-1', 'Streetscape', 'The Handover zone\'s street items take the kit pieces they are pinned to (bun run world:pin-zone)', 0, [], 'every pin used, every chosen item pinned', SOURCE), value: 'no pins' }];
  const r = zoneKitReport(log);
  return [rule('ZK-1', 'Streetscape', 'The Handover zone\'s street items take the kit pieces they are pinned to (bun run world:pin-zone): every pin finds its item, every item that chooses a piece is pinned, every pinned piece still fits', r.checked, r.bad, 'every pin used, every chosen item pinned', SOURCE)];
}
