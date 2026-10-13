// Procedural facade skins (brief 007 round 2): every wall the kit does not cover follows the grammar (contracts/world/harbour-district.grammar.md
// §2-3, §7) instead of the Unity fallback's blue glass grids. Per building: one window family (ribbon, porthole, balcony; sheds a porthole row under
// the eave), one accent role (§2 weights, neighbours differ), clear glass set in stone and chrome. Per face of every building mass: the bay rhythm
// (one letter per bay), the storey rows and the porthole columns, so Unity draws exactly this and nothing else. The numbers:
//   bay ~6 m (whole bays per face); ribbon glass = GLASS_SHARE (55%) of the bay between limestone piers, never wider than MAX_PANE (4 m, §3);
//   sill 0.9 / head 2.6 above each upper floor (1.7 of 3.6), reveal 0.35, chrome mullions every 1.3 m, stone frame 0.45;
//   porthole d 0.9 with a 0.1 chrome ring, two per porthole bay and storey, one column per party (end) wall; balconies: a 1.8 m glazed door per bay.
// Every face's glass share (glass area / wall area over its rows) is reported and must stay <= 0.55 (§3 "> 55% glass on any facade" is forbidden).
import type { Mass, Skin, SkinFace } from '../../../contracts/world/world.schema';
import { type V, dist } from '../../plan/geom';
import { type Ctx, r3, rng } from './common';
import { isStreetClass } from './grammar';
import { kindOf } from './edges';
import { floorTop, type Bld } from './masses';
import { ACCENT_ROLES } from './colour';

export const SKIN = {
  bay: 6, glassShare: 0.55, maxPane: 4, sill: 0.9, head: 2.6, reveal: 0.35, mullion: 1.3, frame: 0.45,
  porthole: { d: 0.9, ring: 0.1, perBay: 2 }, balcony: { door: 1.8, doorH: 2.4 }, minFace: 4, cap: 0.9,
} as const;
/** §2 accent roles (look.json `accent_*` surfaces, grammar round 3): teal 35, coral 20, turquoise 15, mint 10, flamingo 10, peach 10 (cobalt is civic only, gold the trim). */
const ACCENTS = ACCENT_ROLES;
const FAMILIES: Record<string, [Skin['family'], number][]> = {
  residential: [['balcony', 4], ['ribbon', 3], ['porthole', 2]], commercial: [['ribbon', 5], ['porthole', 2]], mixed: [['ribbon', 3], ['balcony', 2], ['porthole', 2]],
  port: [['porthole', 3], ['ribbon', 2]], office: [['porthole', 3], ['ribbon', 2]],
};

/** Glass area of one bay of width w over one storey, by bay kind. */
function bayGlass(kind: string, w: number): number {
  const S = SKIN, rib = Math.min(S.glassShare * w, S.maxPane);
  return kind === 'R' ? rib * (S.head - S.sill) : kind === 'P' ? S.porthole.perBay * Math.PI * (S.porthole.d / 2) ** 2 : kind === 'B' ? S.balcony.door * S.balcony.doorH : 0;
}

/** The bay rhythm of a face: n bays, letters by family (R ribbon, P porthole bay, B balcony bay, - blank stone). */
function rhythm(family: Skin['family'], n: number, street: boolean, seed: string): string {
  if (!street) return '-'.repeat(n);
  const r = rng(seed), off = r.int(0, 3);
  return Array.from({ length: n }, (_, i) => family === 'porthole' ? (i % 2 ? 'R' : 'P') : family === 'balcony' ? (i === 0 || i === n - 1 ? 'R' : 'B') : family === 'shed' ? 'P' : (i + off) % 4 === 3 ? 'P' : 'R').join('');
}

/** A skin before its colour life (gen/colour.ts adds the §2a fields). */
export type BaseSkin = Omit<Skin, 'accent2' | 'trim' | 'faience' | 'spandrel' | 'fins' | 'share' | 'porthole'> & { porthole: Omit<Skin['porthole'], 'panel'> };

export function generateSkins(ctx: Ctx, blds: Bld[], masses: Mass[], near: (id: string) => string[]): BaseSkin[] {
  const B = ctx.B, pav = ctx.pav, out: BaseSkin[] = [], chosen = new Map<string, { family: string; accent: string }>();
  const byB = new Map<string, Mass[]>();
  for (const m of masses) { const id = m.id.slice(0, m.id.lastIndexOf('/')); if (/\/(podium|body|step|upper)$/.test(m.id)) (byB.get(id) ?? byB.set(id, []).get(id)!).push(m); }
  for (const b of blds) {
    if (b.tower) continue;
    const ms = byB.get(b.id) ?? []; if (!ms.length) continue;
    const r = rng(`skin/${b.id}`), nb = near(b.id).map(id => chosen.get(id)).filter(x => !!x);
    const shed = b.kind === 'shed', fams = shed ? [['shed', 1]] as [Skin['family'], number][] : FAMILIES[b.use] ?? FAMILIES.mixed!;
    const fresh = <T extends string>(xs: [T, number][], k: 'family' | 'accent') => { const f = xs.filter(([x]) => !nb.some(o => o![k] === x)); return f.length ? f : xs; };
    const family = r.weighted(fresh(fams, 'family'), x => x[1])[0], accent = r.weighted(fresh(ACCENTS, 'accent'), x => x[1])[0];
    chosen.set(b.id, { family, accent });
    // a face's class: the ground ring edge it lies on or behind (street faces step back from the line; party walls stay on it)
    const classOf = (a: V, z: V): string => {
      const m: V = [(a[0] + z[0]) / 2, (a[1] + z[1]) / 2], p = b.ring.p, N = p.length; let best = Infinity, c = 'party';
      for (let i = 0; i < N; i++) { const q0 = p[i]!, q1 = p[(i + 1) % N]!, L = dist(q0, q1), u: V = [(q1[0] - q0[0]) / L, (q1[1] - q0[1]) / L];
        const s = (m[0] - q0[0]) * u[0] + (m[1] - q0[1]) * u[1], dd = Math.abs((m[0] - q0[0]) * u[1] - (m[1] - q0[1]) * u[0]), par = Math.abs(((z[0] - a[0]) * u[1] - (z[1] - a[1]) * u[0]) / dist(a, z));
        if (s > -1 && s < L + 1 && par < 0.2 && dd < best) { best = dd; c = b.ring.c[i]!; } }
      return c;
    };
    const faces: SkinFace[] = [];
    for (const m of ms) {
      const top = m.base + m.height - SKIN.cap, rows: number[] = [];
      for (let k = 2; k <= b.n; k++) { const z = pav + floorTop(B, k - 1); if (z >= m.base - 0.01 && z + SKIN.head <= top + 0.01) rows.push(r3(z)); }
      if (shed && m.height > 4) rows.push(r3(m.base + m.height - 2.4)); // §3: a porthole row under the eave
      if (!rows.length) continue;
      const ring = m.ring as V[], N = ring.length;
      for (let i = 0; i < N; i++) {
        const a = ring[i]!, z = ring[(i + 1) % N]!, L = dist(a, z); if (L < SKIN.minFace) continue;
        const cls = classOf(a, z), street = isStreetClass(cls) || kindOf(cls) === 'court', n = Math.max(1, Math.round(L / SKIN.bay)), w = L / n;
        const bays = rhythm(family, n, street, `${m.id}/${i}`), area = L * rows.length * B.upperStorey, glass = [...bays].reduce((s, k) => s + bayGlass(k, w), 0) * rows.length;
        const portholes = street ? [] : [r3(L / 2)]; // §3: one porthole column per end (party) wall, one per storey
        const pg = portholes.length * rows.length * Math.PI * (SKIN.porthole.d / 2) ** 2;
        faces.push({ mass: m.id, edge: i, a: [r3(a[0]), r3(a[1])], b: [r3(z[0]), r3(z[1])], bays, rows, portholes, glass: r3((glass + pg) / area) });
      }
    }
    if (!faces.length) continue;
    out.push({ id: `${b.id}/skin`, building: b.id, family, accent, glass: { surface: 'glass', tint: 'clear', share: SKIN.glassShare, maxPane: SKIN.maxPane, sill: SKIN.sill, head: SKIN.head, reveal: SKIN.reveal, mullion: SKIN.mullion },
      pier: { surface: 'limestone', frame: SKIN.frame, frameSurface: 'trim' }, porthole: { d: SKIN.porthole.d, ring: SKIN.porthole.ring, ringSurface: 'trim', perBay: SKIN.porthole.perBay }, faces });
  }
  return out;
}
