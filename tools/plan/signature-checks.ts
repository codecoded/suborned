// World rules from brief 007 A (Datum's hand-off): SG-1 signature grounds, P-1 sky rail portals, SK-1 skyline heights, SK-2 crown types.
// Each reads the generated world; each carries its source and a failing example. They apply only to a plan that declares the section
// (signatures, skyrail.portal, skyline), so plans without them (the recorded A04 plans) are untouched.
import type { Check } from './checks';
import { type V, dist } from './geom';
import { areaOf, clipHalf } from '../world/gen/geo';
import { adoptedBy } from '../world/gen/kitrefs';
import { type R4, type WCtx, TOL, f1, rule } from './world-ctx';

const G = 'Skyline';
type Rect = R4;
const rectRing = (r: Rect): V[] => [[r[0], r[2]], [r[1], r[2]], [r[1], r[3]], [r[0], r[3]]];
/** Area of a ring inside a rect (four half-plane clips). */
function areaIn(ring: V[], r: Rect): number {
  let q: { p: V[]; c: string[] } | null = { p: ring, c: ring.map(() => 'x') };
  for (const [a, n] of [[[r[0], 0], [1, 0]], [[r[1], 0], [-1, 0]], [[0, r[2]], [0, 1]], [[0, r[3]], [0, -1]]] as [V, V][]) { if (!q) return 0; q = clipHalf(q, a, n); }
  return q ? Math.abs(areaOf(q.p)) : 0;
}
const rectsOverlap = (a: Rect, b: Rect) => a[0] < b[1] - TOL && b[0] < a[1] - TOL && a[2] < b[3] - TOL && b[2] < a[3] - TOL;

export { portalsOf, type Portal } from './plan';
import { portalsOf } from './plan';

export function signatureChecks(c: WCtx): Check[] {
  const { plan, d, w, gen, B } = c, out: Check[] = [], sigs = plan.signatures ?? [], sk = plan.standards.skyline;
  const solids = [...w.buildings.map(m => ({ id: m.id, ring: m.ring as V[], base: m.base, top: m.base + m.height })), ...w.megastructures.flatMap(m => m.parts.map(p => ({ id: p.id, ring: p.ring as V[], base: p.base, top: p.base + p.height }))), ...w.civic.map(m => ({ id: m.id, ring: m.ring as V[], base: m.base, top: m.base + m.height }))];

  // SG-1 a plan signature's ground holds no plot building, and the signature stands there as built: the regenerated piece when adopted, within its ground and to its tip
  if (sigs.length) {
    const bad: string[] = [];
    for (const s of sigs) {
      for (const m of w.buildings) if (m.id.startsWith('bld/') && areaIn(m.ring as V[], s.ground) > 0.5) bad.push(`${m.id} stands on ${s.id}'s ground`);
      const o = w.signatures.find(x => x.id === `signature/${s.id}`);
      if (!o) { bad.push(`${s.id}: not in the world's signatures`); continue; }
      const variant = s.regen?.variant ? adoptedBy(s.regen.variant) : undefined;
      if (variant) {
        const [sx, , sz] = o.kit?.scale ?? [1, 1, 1], gw = s.ground[1] - s.ground[0], gd = s.ground[3] - s.ground[2];
        if (o.kit?.id !== variant.id) bad.push(`${s.id}: ${o.kit?.id ?? 'no piece'}, the adopted variant is ${variant.id}`);
        if (variant.size[0] * sx > gw + TOL || variant.size[2] * sz > gd + TOL) bad.push(`${s.id}: ${variant.id} ${f1(variant.size[0] * sx)} × ${f1(variant.size[2] * sz)} m outside its ground ${f1(gw)} × ${f1(gd)} m`);
        if (s.height && Math.abs(o.tip - 0.18 - s.height) > 0.5) bad.push(`${s.id}: tip ${f1(o.tip)} vs the plan's ${s.height}`);
      }
    }
    out.push(rule('SG-1', G, 'A signature structure\'s ground (plan signatures[].ground) holds no plot building; the signature stands on it as the regenerated variant, inside the ground, to its tip', Math.max(sigs.length, bad.length), bad, 'all', 'brief 007 A · plan signatures[] · failing example: a plot building on the SpaceHQ drum ground x 56–136, y ±40 (the Parade\'s west plots as first built)'));
  }

  // P-1 a declared sky rail portal: no solid stands in its opening below its ceiling, and the building carries the beam (no pylon in it)
  const portals = portalsOf(plan);
  if (portals.length) {
    const bad: string[] = [];
    for (const p of portals) {
      const line = Number(String(p.line).startsWith('S1') ? 0 : 0), corridor: Rect = [p.rect[0], p.rect[1], line - p.half, line + p.half];
      for (const s of solids) {
        if (s.base >= p.ceiling - TOL || s.top <= p.z[0] + TOL) continue; // clear of the opening's height band [sill, ceiling)
        if (areaIn(s.ring, corridor) > 0.5) bad.push(`${s.id} stands in ${p.id}'s opening (z ${f1(Math.max(s.base, p.z[0]))}–${f1(Math.min(s.top, p.ceiling))})`);
      }
      for (const py of d.pylons) if (py.p[0] > p.rect[0] + TOL && py.p[0] < p.rect[1] - TOL && Math.abs(py.p[1] - line) < p.half + TOL) bad.push(`pylon ${py.p.map(v => f1(v)).join(',')} inside ${p.id} (the building carries the beam)`);
    }
    out.push(rule('P-1', 'Sky rail', 'Sky rail portals: nothing stands in a declared opening below its ceiling, and no pylon stands inside the portal (the building carries the beam)', Math.max(portals.length, bad.length), bad, 'all', 'standards.skyrail.portal · brief 007 · failing example: the Parade gate block (x 264–288, y ±20, z 9.43–34.8) built without its opening'));
  }

  // SK-1 one dominant tip (SpaceHQ), every other tip at or under rivalMax, at or under the harbour icon within `within` of the quay
  if (sk?.dominant) {
    const bad: string[] = [], quayX = Math.min(...plan.quay.flat().map(p => p[0]));
    type Tip = { id: string; at: V; tip: number };
    const tips: Tip[] = [], ctr = (r: V[]): V => [r.reduce((s, q) => s + q[0], 0) / r.length, r.reduce((s, q) => s + q[1], 0) / r.length];
    for (const s of w.signatures) if (s.kind !== 'station' && s.kind !== 'skybridge' && s.kind !== 'portal') tips.push({ id: s.id, at: s.kit ? s.kit.at : [0, 0], tip: s.tip });
    for (const l of w.landmarks) { const a = l.kit ? adoptedBy(l.kit.id) : undefined; tips.push({ id: l.id, at: l.centre, tip: l.base + (a && l.kit ? a.size[1] * l.kit.scale[1] + (l.id.endsWith('liner-berth') ? l.height : 0) : l.height) }); }
    for (const b of gen.blds) { const r = w.roofs.find(x => x.building === b.id); tips.push({ id: b.id, at: ctr(b.ring.p), tip: b.z + (r?.crown ? (adoptedBy(r.crown.id)?.size[1] ?? 0) * r.crown.scale[1] : 0) }); }
    for (const m of w.megastructures) { const top = Math.max(...m.parts.map(p => p.base + p.height), ...m.prims.map(p => p.base + p.size[2])); tips.push({ id: m.id, at: ctr(m.parts[0]?.ring as V[] ?? [[0, 0]]), tip: top }); }
    const dom = tips.find(t => t.id === `signature/${sk.dominant.id}`);
    if (!dom) bad.push(`dominant ${sk.dominant.id} is not in the world`);
    else {
      if (Math.abs(dom.tip - 0.18 - sk.dominant.tip) > 0.5) bad.push(`${sk.dominant.id} tip ${f1(dom.tip - 0.18)} vs ${sk.dominant.tip}`);
      for (const t of tips) {
        if (t === dom) continue;
        if (t.tip > sk.rivalMax + 0.5) bad.push(`${t.id} tip ${f1(t.tip)} over rivalMax ${sk.rivalMax}`);
        if (t.at[0] >= quayX - sk.harbour.within && t.tip > sk.harbour.tip + 0.5) bad.push(`${t.id} tip ${f1(t.tip)} within ${sk.harbour.within} m of the quay, over ${sk.harbour.tip}`);
      }
    }
    out.push(rule('SK-1', G, `One dominant tip (${sk.dominant.id} ${sk.dominant.tip}); every other tip ≤ rivalMax ${sk.rivalMax}, and ≤ ${sk.harbour.tip} within ${sk.harbour.within} m of the quay`, tips.length, bad, 'all', 'standards.skyline · brief 007 · failing example: LinerDock as built (205.1 m) on the liner berth'));
  }

  // SK-2 every tower site and signature names a crown type; within 150 m no two crowns of one type (matched pairs excepted)
  if (sk?.crowns) {
    const bad: string[] = [], sites = (B.towers.sites as { id: string; rect: Rect; crownType?: string; regen?: { same?: string } }[]);
    const typ = (s: { crownType?: string }) => s.crownType;
    const items = [...sites.map(s => ({ id: s.id, at: [(s.rect[0] + s.rect[1]) / 2, (s.rect[2] + s.rect[3]) / 2] as V, type: typ(s), pair: s.regen?.same })), ...sigs.map(s => ({ id: s.id, at: s.c, type: (s as { crownType?: string }).crownType, pair: undefined as string | undefined })), ...plan.landmarks.filter(l => (l as { crownType?: string }).crownType).map(l => ({ id: l.id, at: l.c, type: (l as { crownType?: string }).crownType, pair: undefined as string | undefined }))];
    for (const i of items) if (!i.type) bad.push(`${i.id} names no crown type`);
    for (let a = 0; a < items.length; a++) for (let b = a + 1; b < items.length; b++) {
      const x = items[a]!, y = items[b]!;
      if (x.type && x.type === y.type && dist(x.at, y.at) < 150 && x.pair !== y.id && y.pair !== x.id) bad.push(`${x.id} and ${y.id} share the ${x.type} crown ${f1(dist(x.at, y.at))} m apart`);
    }
    out.push(rule('SK-2', G, 'Every tower site and signature names a crown type; within 150 m no two share one (matched pairs excepted)', items.length, bad, 'all', 'standards.skyline.crowns · grammar §6 · failing example: avenue-gate-w and a second disc crown 90 m away that is not its pair'));
  }
  return out;
}
