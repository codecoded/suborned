// Brief 075, the golden view (Sky 2026-10-28: "just still nothing like old in terms of color, style, richness"): the old DecoKit boulevard's rules,
// ported from the old builders (~/Projects/the-suborned/game/Assets/TheSuborned/Editor/DecoKit/, read only) onto our plan's geometry, as generator
// settings scoped by road id. Nothing is placed by hand: every rule below runs over plan data (the road's line, its plot edges, the buildings fronting it)
// and is seeded by element id like the rest of the generator, so the same plan gives the same world. A road joins by its id in OLD_STREET.roads; the
// rules are written for any straight road and may be widened district-wide once Sky has seen the view.
//
// What was ported (old file · parameter → our rule):
//   palms   DecoKitStreetLayout.Walks / PaveOrPit / Pit: CoconutPalm in a tree pit 1 m behind the kerb line (pit cell at KerbX + 1; ours 1.2: SF-5, below), every 10 m
//           (Pit: Repeat(z - at, 10)), staggered: the west walk's pits at z ≡ 1, the east's at z ≡ 5 (mod 10) ("west pits 6 m off"), scale 1 (the same
//           FBX, sha256 176c68fb…, as our CoconutPalm). Old frame: the circus at z 206; ours: the circus at the plan origin, so in our frame the left
//           (west) walk's palms stand at u ≡ 5 and the right (east) walk's at u ≡ 9 (mod 10), u = the point's coordinate along the road's direction.
//           This reverses the 2026-10-22 call (standards.streetscape.trees.spacing 12, kept against the old 10 m) on these roads only: Sky's to confirm.
//   kerb    (consequence, not a port) the kerb furniture (lamps, benches and bins, hydrants, booths, mailboxes) walks the palm grid's midpoints from its own
//           first station, at the first free midpoint its spacing past the last (street/roads.ts walk): the 10 m grid leaves no other ground clear of a
//           palm (SF-11: lamp 3 m, hydrant 2 m), and the catalogue's spacings (SF-7) hold.
//   bays    DecoKitCityBlocks (the boulevard walls) + DecoKitFrontage.Of: the boulevard's walls are streamline (S9, S9W, S6) and deco stone (D10) runs; we
//           keep the streamline family on both sides (`classes`), at the old storey codes 6 and 9 inside the use band (`codes`, weighted by the old walls'
//           bays: 26 at code 9, 10 at code 6), so every bay is a whole-wall old piece at its own proportions (no stacked modules). Code 9 has two bays
//           (StreamBay_9, StreamBay_9W), so two code-9 buildings may stand side by side as the old S9W | S9 runs did; code 6 has one and keeps the
//           neighbour contrast. In the play streets the cap's own storeys stand (brief 066) and only the class is held. A bay may stand up to 8 % narrower
//           than its height scale asks (`squash` 0.92 against the kit tag's 1: H(9) 38.4 against S9's 36.7 asks 6.28 m, which leaves the plan's 6 m-bay
//           buildings a procedural gap), so the walls close on whole bays of about the old 6 m.
//   ends    DecoKitFrontage.Bays: a streamline segment rounds off (StreamEnd) only where it stands taller than its neighbour (h > neighbour + 1; a tower
//           site's podium counts as a neighbour at the tower's storeys), or at a run's open end; never at every party wall. Our ends round toward the edge's tail only (stream.ts KIT_X: the export has no mirror), so a
//           taller building whose lower neighbour is at its head stays square there (a mirror flag would need the schema and builder: Aster's call).
//   colour  DecoKitColourways (scheme "plan+livery+soft", DecoKitStreetDemo.Colour): HighStreet walks each boulevard wall in order; a rendered
//           (streamline) building is painted when the one before it is not, the palette running on in order from wall to wall (Soft: apricot #E8B793,
//           celadon #B7CDB4, terracotta-rose #DDA596, ochre #E2C07A), every other building the kit's cream; a painted building's eyebrows and canopy
//           (DK_Enamel_Ivory) take Lighter(wall) = halfway (linear) to ivory #EFE8D6. The four are facade.json families `oldapricot` … `oldochre`
//           (tools/decokit/colourways.py OLD_FAMILIES: the exact hexes, no softening, wall + trim slots); the unpainted take `cream` (dE2000 2.5
//           from the old kit cream #E6D7C2). The old Boulevard ensemble's coral fascia (#C8553D on DK_Enamel_Teal) is NOT ported: 074-H keeps the fascia
//           teal everywhere (Sky, Redline); it is listed for Sky.
import { type V, dist } from '../../plan/geom';
import type { Ctx } from './common';

const idOf = (c: string) => c.split(':')[1] ?? ''; // edges.ts idOf (kept local: facades.ts and masses.ts read this module, edges.ts reads theirs)

export const OLD_STREET = {
  /** The roads the old street's rules dress (plan road ids). */
  roads: ['boulevard'] as string[],
  palms: {
    /** m between palms along one walk (old Pit: every 10 m). */
    spacing: 10,
    /** m from the kerb line to the trunk. The old pit cell's centre stood at KerbX + 1 (1.0 m), but SF-5 judges a tree by its 1.5 m pit (species.coconut.pit):
     *  at 1.0 the pit's edge is 0.25 m off the kerb face (< 0.45, car doors and mirrors), so the trunk stands at 0.45 + 0.75 = 1.2 m, today's kerb-zone middle. */
    kerb: 1.2,
    /** m: where along the road (u mod spacing) the left and right walks' palms stand (old z ≡ 1 west, ≡ 5 east, circus at z 206 → our origin). */
    phase: { left: 5, right: 9 },
    /** Brief 075 roll-out (palms district-wide): the road classes whose pavement palms take this grid on every road, not only the old-street roads. The old town
     *  (DecoKitBlock.Ring, Southside) pitted every kerbed long face with a pavement of 4 m or more every 10 m (DecoKitBlock.Pit), 1 m behind the kerb, 4 m clear of
     *  the corners; the boulevard's walks the same (DecoKitStreetLayout.Walks). Our pavements are 8 m (district) and 6.5 m (access), so every edge the plan plants
     *  (standards.streetscape.trees.avenues: district both pavements, access avenues) takes it; the plan's corners-only (Lantern) and gates-only (Docks) areas
     *  and the primaries (palm islands in the angle parking, tramstreet.ts) keep their own rules. */
    classes: ['district', 'access'] as string[],
    /** What keeps the 12 m grid (old-street roads excepted): 'capstone' = the plots in the capstone zone (street/palms.ts: the Handover's walk window grown by
     *  30 m). The Handover's site (bun run sim:site) draws the street furniture there and sites its call boxes between it, so the 10 m grid there moves the
     *  Handover and c041 would need re-recording; the Handover is the one protected thing (brief 075, Sky 2026-10-28). Empty it once the replays are re-recorded (Waymark). */
    keep: ['capstone'] as string[],
  },
  frontage: {
    /** stream.ts classes a building fronting the road may take (old boulevard walls: streamline). */
    classes: ['S'] as string[],
    /** Storey codes (old DecoKitFrontage heights: S6 26.5, S9 36.7) a building fronting the road stands at, where its use band reaches them, with the
     *  old walls' bay counts as weights. */
    codes: { 6: 10, 9: 26 } as Record<number, number>,
    /** Codes with two streamline bays (S9, S9W): neighbours may share them (the old S9W | S9 runs); the rest keep the neighbour contrast. */
    twins: [9] as number[],
    /** Width over height scale a bay may take on an old-street face (the streamline bays' kit tag `squash` is 1). 0.92: the plan's buildings are whole 6 m bays, and
     *  at H(9) (scale 1.046) a 24 m building takes four bays of 6 m (0.956), one with an end 17.72 m over three (5.91 m, 0.941), 11.72 over two (0.933), against
     *  three bays of 6.6 and a 4 m procedural gap under the tag. Vela's to confirm (the ribbon windows narrow by up to 8 %). */
    squash: 0.92,
  },
  colour: {
    /** The old Soft palette as facade.json families, in the old order. */
    palette: ['oldapricot', 'oldceladon', 'oldrose', 'oldochre'],
    /** The unpainted buildings' family (the old kit cream). */
    plain: 'cream',
  },
};


/** Is this edge class (`kind:id`) one of the old street's roads? */
export const isOldRoad = (c: string): boolean => OLD_STREET.roads.includes(idOf(c));
/** The road id of an old-street edge class, else ''. */
export const oldRoadOf = (c: string): string => (isOldRoad(c) ? idOf(c) : '');

/** The road's local direction (unit, along its path) and whether `p` is on its left, at the road segment nearest `p`. */
export function roadFrame(ctx: Ctx, road: string, p: V): { dir: V; left: boolean } | null {
  const r = ctx.d.roads.find(x => x.id === road); if (!r) return null;
  let best = Infinity, out: { dir: V; left: boolean } | null = null;
  for (let i = 0; i + 1 < r.line.length; i++) {
    const a = r.line[i]!, b = r.line[i + 1]!, L = dist(a, b); if (L < 1e-6) continue;
    const t: V = [(b[0] - a[0]) / L, (b[1] - a[1]) / L], s = Math.max(0, Math.min(L, (p[0] - a[0]) * t[0] + (p[1] - a[1]) * t[1])), q: V = [a[0] + t[0] * s, a[1] + t[1] * s], d = dist(p, q);
    if (d < best) { best = d; out = { dir: t, left: t[0] * (p[1] - q[1]) - t[1] * (p[0] - q[0]) > 0 }; }
  }
  return out;
}

/** The palm stations along a plot edge on an old-street road: s (m from the edge's start a) for every u ≡ phase (mod spacing) inside [s0, s1], where
 *  u(s) = (a + t s) · dir. Null where the edge does not run along the road (|t · dir| < 0.9): the default grid then stands. */
export function palmStations(ctx: Ctx, road: string, a: V, t: V, mid: V, s0: number, s1: number): number[] | null {
  const f = roadFrame(ctx, road, mid); if (!f) return null;
  const k = t[0] * f.dir[0] + t[1] * f.dir[1]; if (Math.abs(k) < 0.9) return null;
  const P = OLD_STREET.palms, ph = f.left ? P.phase.left : P.phase.right, u0 = a[0] * f.dir[0] + a[1] * f.dir[1], out: number[] = [];
  // u(s) = u0 + k s; the stations u = ph + m * spacing with s in [s0, s1]
  const ua = u0 + k * s0, ub = u0 + k * s1, lo = Math.min(ua, ub), hi = Math.max(ua, ub);
  for (let m = Math.ceil((lo - ph) / P.spacing - 1e-9); ph + m * P.spacing <= hi + 1e-9; m++) out.push((ph + m * P.spacing - u0) / k);
  return out.sort((x, y) => x - y);
}

/** A lamp's station on an old-street edge: the palm-grid midpoint nearest `s` (palm stations every `spacing`, the lamp half a spacing off). */
export function lampStation(stations: number[], s: number): number {
  if (!stations.length) return s;
  const half = OLD_STREET.palms.spacing / 2, cands = [stations[0]! - half, ...stations.map(x => x + half)];
  return cands.reduce((b, x) => (Math.abs(x - s) < Math.abs(b - s) ? x : b), cands[0]!);
}
