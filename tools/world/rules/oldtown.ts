// OT-1..OT-3 (brief 075 remap, Aster 2026-10-28: Orbit Gardens and the harbour terrace from plan.oldtown by the old rules; "no bare-sand patches ... along the
// water"; the old props by the old placement rules, the Handover zone's dressing left alone). What the generator lays (tools/world/gen/oldtown/ground.ts) and
// places (tools/world/gen/oldtown/props.ts) is judged here; a plan without `oldtown` has nothing to judge.
//   OT-1 the coast quay is dressed: every point of the long quay strip lies under the terrace, steps or promenade flags or the coping (no bare quay), and every
//        rectangular park plot has its lawn border and lawn.
//   OT-2 the old basin and canal stand clear: no street item but the basin's own fountain stands in their water or on their coping.
//   OT-3 an old prop (street/old/*) is an adopted kit piece at its own size (scale 1, the slot its item), outside the Handover's capstone zone, on ground
//        (never on a carriageway, the circus ring road or the sea).
// Failing examples: tools/world/rules/oldtown.test.ts.
import type { World } from '../../../contracts/world/world.schema';
import type { Check } from '../../plan/checks';
import { type V, inPoly, lineDist } from '../../plan/geom';
import { type WCtx, rule } from '../../plan/world-ctx';
import { ctxOf as genCtx } from '../gen/common';
import { adopted } from '../gen/kitrefs';
import { zoneOf } from '../gen/street/capstone';

const G = 'Old town';
const SRC = 'brief 075 remap (Aster 2026-10-28; Sky "And old props", "no bare-sand patches in the play streets or along the water") · tools/world/gen/oldtown/ground.ts, props.ts (DecoKitGardens, DecoKitHarbour, DecoKitDistrictDressing, DecoKitStreetLife) · failing examples: tools/world/rules/oldtown.test.ts';

/** OT-1: the coast quay's bare points (a 2 m grid) and the rectangular park plots without their border or lawn. */
export function quayFaults(c: Pick<WCtx, 'plan' | 'd'>, w: World): { n: number; bad: string[] } {
  const ot = (c.plan as { oldtown?: { harbour?: { quay: number } } }).oldtown;
  if (!ot?.harbour) return { n: 0, bad: [] };
  const coast = c.plan.quay.find(q => q.some(p => Math.abs(p[0] - ot.harbour!.quay) < 1e-6)) as V[] | undefined, bad: string[] = [];
  if (!coast) return { n: 1, bad: [`no coast quay on x ${ot.harbour.quay}`] };
  const cover = [...w.ground.flats.filter(f => f.id.startsWith('oldtown/harbour/')).map(f => f.ring as V[]), ...w.ground.quays.filter(q => q.id.startsWith('oldtown/harbour/')).map(q => q.ring as V[])];
  const xs = coast.map(p => p[0]), ys = coast.map(p => p[1]); let n = 0, bare = 0, first = '';
  for (let x = Math.min(...xs) + 1; x < Math.max(...xs); x += 2) for (let y = Math.min(...ys) + 1; y < Math.max(...ys); y += 2) {
    const p: V = [x, y]; if (!inPoly(p, coast)) continue; n++;
    if (!cover.some(r => inPoly(p, r))) { bare++; if (!first) first = `${x}, ${y}`; }
  }
  if (bare) bad.push(`${bare} points of the coast quay bare (first at ${first}): no terrace, steps, promenade or coping over them`);
  for (const p of c.d.plots) {
    if (p.use !== 'park' || (p as { civic?: unknown }).civic || p.polyV.length !== 4) continue; n++;
    for (const id of [`oldtown/lawn/${p.id}/border`, `oldtown/lawn/${p.id}`]) if (!w.ground.flats.some(f => f.id === id)) bad.push(`${p.id}: no ${id.endsWith('/border') ? 'lawn border' : 'lawn'} (${id})`);
  }
  return { n, bad };
}

/** OT-2: street items standing in the old basin's or canal's water or on their coping (the basin's fountain excepted). */
export function basinFaults(w: World): { n: number; bad: string[] } {
  const wet = [...w.ground.water.filter(f => f.id.startsWith('oldtown/')), ...w.ground.quays.filter(q => /^oldtown\/gardens\/(basin|canal)\/coping/.test(q.id))];
  if (!wet.length) return { n: 0, bad: [] };
  const bad: string[] = [];
  for (const s of w.street) { if (s.id.startsWith('street/old/G7-fountain/')) continue; const r = wet.find(f => inPoly(s.at, f.ring as V[])); if (r) bad.push(`${s.id} (${s.kind}) stands in ${r.id}`); }
  return { n: w.street.length, bad };
}

/** OT-3: old props that are not an adopted piece at its own size, stand in the Handover's capstone zone, or stand off the ground. */
export function propFaults(c: Pick<WCtx, 'plan' | 'd' | 'civ'>, w: World): { n: number; bad: string[] } {
  const mine = w.street.filter(s => s.id.startsWith('street/old/'));
  if (!mine.length) return { n: 0, bad: [] };
  const have = new Set(adopted().map(a => a.id)), zone = zoneOf(genCtx(c.plan, c.d, c.civ, 0.18)), cc = c.plan.circus, bad: string[] = [];
  const roads = c.d.roads.map(r => ({ id: r.id, line: r.line as V[], half: r.w / 2 }));
  for (const s of mine) {
    if (!s.kit || !have.has(s.kit.id)) bad.push(`${s.id}: ${s.kit ? `${s.kit.id} is not adopted` : 'no kit piece'}`);
    else if (s.kit.scale.some(v => Math.abs(v - 1) > 1e-6)) bad.push(`${s.id}: ${s.kit.id} scaled ${s.kit.scale.join(' x ')} (old props stand at their own size)`);
    if (s.at[0] > zone[0] && s.at[0] < zone[1] && s.at[1] > zone[2] && s.at[1] < zone[3]) bad.push(`${s.id}: in the Handover's capstone zone`);
    const rc = Math.hypot(s.at[0] - cc.c[0], s.at[1] - cc.c[1]), road = roads.find(r => lineDist(s.at, r.line) < r.half);
    if (road) bad.push(`${s.id}: on the carriageway of ${road.id}`);
    else if (rc > cc.ring[0] && rc < cc.ring[1]) bad.push(`${s.id}: on the circus ring road`);
    else if ((c.plan.water as V[][]).some(r => inPoly(s.at, r))) bad.push(`${s.id}: in the sea`);
  }
  return { n: mine.length, bad };
}

export function oldTownRules(c: WCtx): Check[] {
  if (!(c.plan as { oldtown?: unknown }).oldtown) return [];
  const q = quayFaults(c, c.w), b = basinFaults(c.w), p = propFaults(c, c.w);
  return [
    rule('OT-1', G, 'The coast quay is dressed (terrace, steps and promenade flags, the coping: no bare quay) and every rectangular park plot has its lawn border and lawn', q.n, q.bad, 'none bare', SRC),
    rule('OT-2', G, 'Nothing but the basin\'s fountain stands in the old basin\'s or canal\'s water or on their coping', b.n, b.bad, 'none', SRC),
    rule('OT-3', G, 'An old prop is an adopted kit piece at its own size, outside the Handover\'s capstone zone, on ground (no carriageway, ring road or sea)', p.n, p.bad, 'all', SRC),
  ];
}
