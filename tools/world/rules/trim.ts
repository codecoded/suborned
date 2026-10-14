// SF-21: `trim` is thin. Sky (14 Oct, brief 008): brass at street level and on facades, "hairline, frame and bead only". `trim` and `accent_gold` are brass in
// look.json, so a part that draws either must be a hairline, a frame member or a bead; anything broad draws a key of its own (gen/surfaces.ts) and its
// colour is one look.json row. Latch's district check after the rebuild found the Parade canopy slabs, the stair-bank plates, the booths and the palm tubs
// in brass by eye; this rule is that check on the export, so the next broad part fails here and not in a screenshot.
//
// The measure is Latch's: a part is broad when it has a face of at least TRIM_FACE m² whose shorter side is wider than TRIM_ACROSS. The export gives a prim as
// a bounding box (a along yaw, b across, h), so the faces are the box's three: the widest has the two longest sides, and the part is thin when those two are
// not both wide enough. A cylinder or a sphere is measured by its bounding box (an over-estimate, never an under-estimate). A vault is the shell of an
// arch, a rib being the arch's thickness `a` along the barrel: its face is `a` by the span (PrimKit.Vault draws the arch and its flat ends; the ends are the
// arch's section, seen only end-on). The boundary is open: a part exactly 0.6 m across is thin (the stair towers' fins are 0.3 x 0.6 x 29 m, a public clock
// 0.6 x 0.6 x 4.5 m), a part more than 1 mm wider is broad.
//
// What it reads is what Unity paints through look.json `trim` / `accent_gold`: street items drawn from kit.json (not those with a kit piece, which carry the
// piece's own materials), megastructure prims and parts, connector dress (CityLayers.Connectors: one part per surface), buildings and civic masses without a kit
// piece, and the gold beads (a pier frame, a porthole ring, a fin's bead, a banner's border) by their width. It leaves out:
//   - roof items. RoofKit.Roofs paints `roofs[].parts` from facade.json `roofItems`, where `trim` is the host building's enamel pair ("@A") and a key it does not
//     list falls back to "paper": a new key there would repaint the part, and brass is not what `trim` draws on a roof;
//   - frontage prims. ProcShop reads the entrance and the service bay only for their centre and size and paints the frame, the glass door and the bay from the
//     facade swatches (chrome, glass_dark, service); nothing paints a frontage prim from its own `surface`;
//   - flag pennants (cloth in the accent role: their `surface` is the cloth, not a brass part).
import type { Mass, Prim, World } from '../../../contracts/world/world.schema';
import type { Check } from '../../plan/checks';
import { TOL, type WCtx, rule } from '../../plan/world-ctx';

/** Metres across a face: the widest a brass part may be. Latch's 0.6: at the play zoom a brass part wider than this stops reading as a line. */
export const TRIM_ACROSS = 0.6;
/** Square metres: a face smaller than this is a bead whatever its proportions. Latch's 1 m². */
export const TRIM_FACE = 1;
/** look.json surfaces that are brass (the `trim` role, and the gold role that facade trim and beads draw). */
export const BRASS = new Set(['trim', 'accent_gold']);

const SOURCE = 'Sky 2026-10-14: brass is hairline, frame and bead only · Latch\'s district check (a face of at least 1 m² that is wider than 0.6 m) · '
  + 'Vela 008-large-trim · look.json trim, accent_gold · RoofKit roofItems, ProcShop swatches (what is left out, and why)';

export type Part = { id: string; what: string; face: [number, number] };
const m1 = (n: number) => (!Number.isFinite(n) ? '∞' : n < 10 ? String(Number(n.toFixed(2))) : String(Math.round(n)));

/** The two sides of a prim's widest face, shorter first. */
export function faceOf(shape: Prim['shape'], size: readonly [number, number, number]): [number, number] {
  const [a, b, h] = size;
  const d = shape === 'vault' ? [a, b] : [a, b, h].sort((x, y) => x - y).slice(1);
  return [Math.min(d[0]!, d[1]!), Math.max(d[0]!, d[1]!)];
}
/** True when a face is broad: wider than TRIM_ACROSS (1 mm of export rounding in the part's favour) and at least TRIM_FACE m². */
export const broad = (f: [number, number]): boolean => f[0] > TRIM_ACROSS + TOL && f[0] * f[1] >= TRIM_FACE - TOL;

/** A brass-surfaced prim or street item, as a part. */
const sized = (id: string, what: string, shape: Prim['shape'], size: readonly [number, number, number]): Part => ({ id, what: `${what} ${shape} ${size.join(' x ')} m`, face: faceOf(shape, size) });
const massed = (m: Mass, what: string): Part => {
  const xs = m.ring.map(p => p[0]), ys = m.ring.map(p => p[1]);
  return sized(m.id, what, 'box', [Math.max(...xs) - Math.min(...xs), Math.max(...ys) - Math.min(...ys), m.height]);
};
/** A bead, frame or border is a width and runs on: its face is that wide and as long as it needs to be. */
const bead = (id: string, what: string, width: number): Part => ({ id, what: `${what} ${width} m wide`, face: [width, Infinity] });

/** Every brass-surfaced part Unity paints through look.json, with its widest face. */
export function brassParts(w: World): Part[] {
  const out: Part[] = [];
  for (const s of w.street) if (!s.kit && BRASS.has(s.surface)) out.push(sized(s.id, s.kind, 'box', s.size));
  for (const m of w.megastructures) {
    for (const p of m.prims) if (BRASS.has(p.surface)) out.push(sized(p.id, 'megastructure prim', p.shape, p.size));
    for (const p of m.parts) if (BRASS.has(p.surface) && !p.kit) out.push(massed(p, 'megastructure part'));
  }
  for (const k of w.connectors) for (const p of k.dress ?? []) if (BRASS.has(p.surface)) out.push(sized(p.id, 'connector dress', p.shape, p.size));
  for (const m of [...w.buildings, ...w.civic]) if (BRASS.has(m.surface) && !m.kit) out.push(massed(m, 'mass'));
  for (const k of w.skins) {
    if (BRASS.has(k.pier.frameSurface)) out.push(bead(`${k.id}/pier`, 'pier frame', k.pier.frame));
    if (BRASS.has(k.porthole.ringSurface)) out.push(bead(`${k.id}/porthole`, 'porthole ring', k.porthole.ring));
    for (const f of k.fins) if (BRASS.has(f.bead.surface)) out.push(bead(f.id, 'fin bead', f.bead.w));
  }
  for (const b of w.fabric.banners) if (BRASS.has(b.border.surface)) out.push(bead(b.id, 'banner border', b.border.w));
  return out;
}

/** The brass parts that are broad, with the reason each is named. */
export const broadParts = (w: World): string[] => brassParts(w).filter(p => broad(p.face)).map(p => `${p.id} (${p.what}, face ${m1(p.face[0])} x ${m1(p.face[1])} m)`);

export function trimRules(c: WCtx): Check[] {
  const parts = brassParts(c.w), bad = broadParts(c.w);
  return [rule('SF-21', 'Streetscape', `Brass is thin: no part drawing trim or accent_gold has a face of ${TRIM_FACE} m² or more wider than ${TRIM_ACROSS} m (a broad part draws its own look key)`,
    parts.length, bad, `face ≤ ${TRIM_ACROSS} m across`, SOURCE)];
}
