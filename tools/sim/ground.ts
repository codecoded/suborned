// Brief 096 (Motive's rules page, Sky 2026-11-04 "all. recommended"; Keel): the block's car ground the engine reads, in the site's frame and
// units (block frame, integer cm): each bay with its use (public, official or service: Vantage's, civic-block.adjust.json addBay) and each drive
// with its kerb (the segment of the carriageway edge it crosses) and the bay it reaches. The engine derives from them, at load, every stand's way
// out (rulebook car.box), the turned driver's kerbside stop (car.kerb) and which ground is private for a parked car (attention.parked).
// Unknown = reject: a bay with no use, or a drive with no kerb, stops the site.
import type { V } from '../plan/geom';

export const BAY_USES = ['public', 'official', 'service'] as const;
type Block = { bays: { id: string; use?: string; polygon: V[] }[]; drives?: { id: string; polygon: V[]; kerb?: V[]; to?: string; road?: string }[] };
const cm = (m: number) => Math.round(m * 100);
const pts = (l: V[]) => l.map(([x, y]) => [cm(x), cm(y)]);

export function groundOf(block: Block) {
  const bays = block.bays.map((b) => {
    if (!BAY_USES.includes(b.use as (typeof BAY_USES)[number])) throw new Error(`ground: bay ${b.id} has no use (public, official or service: civic-block.adjust.json addBay, brief 096)`);
    return { id: b.id, use: b.use!, polygon: pts(b.polygon) };
  });
  const drives = (block.drives ?? []).map((d) => {
    if (!d.kerb || d.kerb.length !== 2 || !d.to || !d.road) throw new Error(`ground: drive ${d.id} needs its kerb, road and bay (civic-block.json drives[])`);
    if (!bays.some((b) => b.id === d.to)) throw new Error(`ground: drive ${d.id} reaches no bay ${d.to}`);
    return { id: d.id, road: d.road, to: d.to, kerb: pts(d.kerb), polygon: pts(d.polygon) };
  });
  return {
    bays, drives,
    src: 'brief 096 (Motive, briefs/096/motive/rules.html; Keel): civic-block.json bays[] with their use (Vantage, civic-block.adjust.json addBay) and drives[] with their kerb, road and bay (Splice, brief 023), block frame in integer cm (tools/sim/ground.ts). The engine reads them for car.box (each stand\'s way out), car.kerb (the turned driver\'s kerbside stop) and attention.parked (private ground: a drive, an official or a service bay)',
  };
}
