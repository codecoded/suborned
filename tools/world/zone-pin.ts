// Brief 075 (stable zone): write the Handover zone's kit pins, contracts/world/<district>.zone-kit.json (kitrefs.ts ZonePins).
// Every street item in the zone (standards.blocks.zone) that takes a kit piece by choice is pinned by place (kind, edge, position to the cm) to the piece it takes,
// so the zone draws the same pieces whatever changes outside it. The pins are written from a world, never by hand:
//   bun run world:pin-zone                          the live plan's world as it builds now (with the pins it has): re-pin after a zone change Sky signed off
//   bun run world:pin-zone --from <street.json> --note "<what it is>"   the pieces of another build ({ street: StreetItem[] }, a world export or a street dump), matched by place
// It refuses (exit 1) if an item the live world chooses for has no item at its place in the source: the zone moved, and pinning would hide it.
import { readFileSync, writeFileSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { relative, resolve } from 'node:path';
import { derive } from '../plan/plan';
import { civic } from '../plan/civic';
import { worldOf } from './build';
import { type ZonePins, forgetZonePins, placeKey, withZonePins, zonePinFile } from './gen/kitrefs';

const root = resolve(import.meta.dir, '../..');
const args = process.argv.slice(2), flag = (k: string) => { const i = args.indexOf(k); return i >= 0 ? args.splice(i, 2)[1] : undefined; };
const from = flag('--from'), note = flag('--note');
const PLAN = 'contracts/world/harbour-district.plan.json';
const sha = (f: string) => createHash('sha256').update(readFileSync(resolve(root, f))).digest('hex');
const plan = JSON.parse(readFileSync(resolve(root, PLAN), 'utf8')), d = derive(plan), civ = civic(plan.civic, JSON.parse(readFileSync(resolve(root, plan.civic.block), 'utf8')));
const zone = plan.standards?.blocks?.zone as [number, number, number, number] | undefined;
if (!zone) { console.error('the plan has no Handover zone (standards.blocks.zone): nothing to pin'); process.exit(2); }

const live = worldOf(plan, d, civ);
const chosen = live.gen.zonePins?.chosen ?? [];
type Item = { kind: string; edge: string; at: number[]; kit?: { id: string } };
const src: Item[] = from ? (JSON.parse(readFileSync(resolve(from), 'utf8')).street as Item[]) : (live.world.street as Item[]);
const byKey = new Map(src.map(s => [placeKey(s), s] as const));
const missing: string[] = [], pins: ZonePins['pins'] = [];
for (const k of chosen) {
  const s = byKey.get(k);
  if (!s?.kit) { missing.push(k); continue; }
  pins.push({ kind: s.kind, edge: s.edge, at: [s.at[0]!, s.at[1]!], kit: s.kit.id });
}
if (missing.length) { console.error(`${missing.length} zone item(s) the live world chooses a piece for have no item at their place in the source (the zone moved; not pinned):\n  ${missing.join('\n  ')}`); process.exit(1); }
pins.sort((a, b) => placeKey(a).localeCompare(placeKey(b)));
const out: ZonePins = {
  schema: 'suborned.zone-kit/1', district: plan.id, zone,
  why: 'brief 075 (stable zone): the kit pieces the Handover zone\'s street items took when the zone was signed off, pinned by place (kind, edge, position), so no change outside the zone swaps a piece inside it (Datum\'s S2 stations renumbered the zone and swapped 8 lamps). Written by bun run world:pin-zone, never by hand; world rule ZK-1 fails if a pin finds no item or an item in the zone has no pin.',
  source: { from: from ? relative(root, resolve(from)) : 'the live plan\'s world', ...(note ? { note } : {}), plan: sha(PLAN) },
  pins,
};
const file = zonePinFile(plan.id);
writeFileSync(file, JSON.stringify(out, null, 1).replace(/\n {3,}/g, ' ').replace(/\n  \}/g, ' }') + '\n');
// read back through the generator: every pin must be used and every chosen item pinned
forgetZonePins();
const check = withZonePins(file, () => worldOf(plan, d, civ)).gen.zonePins!;
console.log(`pinned ${pins.length} zone items → ${relative(root, file)}; read back: used ${check.used}, unmatched ${check.unmatched.length}, unpinned ${check.unpinned.length}, refit ${check.refit.length}`);
process.exit(check.unmatched.length || check.unpinned.length || check.refit.length ? 1 : 0);
