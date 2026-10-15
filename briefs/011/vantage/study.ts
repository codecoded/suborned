// Brief 011 · Vantage · the Handover at the play camera: what the fixed camera (GDD §10: zoom 14 at 38°, yaw 30) sees of every
// Handover point today, and under four options for the Records Office's public door. Measures with rule V-1's own code
// (tools/missions/handover/visibility.ts on tools/missions/sight.ts), renders study pictures at the play camera's angles
// (oblique.ts) and marks Latch's Unity plates (briefs/011/ui/plates/) with the same points.
// Run (repo root): bun briefs/011/vantage/study.ts   → briefs/011/vantage/*.png, study.json. Needs rsvg-convert. Writes nothing else.
// Nothing here changes the plan, the block, the adjust file or the mission: options are transforms applied in memory.
import { readFileSync, writeFileSync, mkdirSync, rmSync } from 'node:fs';
import { execFileSync } from 'node:child_process';
import { resolve } from 'node:path';
import { derive } from '../../../tools/plan/plan';
import { civic, toPlanFn, type Civic } from '../../../tools/plan/civic';
import { generated } from '../../../tools/plan/world-ctx';
import { walkBetween } from '../../../tools/plan/walk-fine';
import { type V, inPoly } from '../../../tools/plan/geom';
import { type Blocker, type View, PLAY, blocks, project, seen } from '../../../tools/missions/sight';
import { blockersFor, visibility, v1, type Row, V1_LIMIT } from '../../../tools/missions/handover/visibility';
import { render, png, type Solid, type Flat, type RGB } from './oblique';

const ROOT = resolve(import.meta.dir, '../../..'), OUT = resolve(import.meta.dir);
const J = (p: string) => JSON.parse(readFileSync(resolve(ROOT, p), 'utf8'));
const plan = J('contracts/world/harbour-district.plan.json'), block = J(plan.civic.block), mission = J('contracts/missions/handover/mission.json');
const d = derive(plan), civ = civic(plan.civic, block), { w } = generated(plan, d, civ), toPlan = toPlanFn(plan.civic);
const B0 = blockersFor(w, civ, plan);

// ---- frames: block (x south, y east) <-> plan (x east, y north) ----
const P2B = (p: V): V => [-230 - p[1], p[0] + 130];
const cen = (r: V[]): V => [r.reduce((s, p) => s + p[0], 0) / r.length, r.reduce((s, p) => s + p[1], 0) / r.length];
type XF = (p: V) => V; // plan → plan
const inB = (p: V, x0: number, x1: number, y0: number, y1: number) => { const b = P2B(p); return b[0] >= x0 && b[0] <= x1 && b[1] >= y0 && b[1] <= y1; };
const viaBlock = (f: (b: V) => V): XF => p => toPlan(f(P2B(p)));

// ---- the options, each a transform of the civic layer and the mission, in memory ----
type Mission = typeof mission;
type Option = { id: string; title: string; civicXf: (id: string, ring: V[]) => V[] | null; extra?: Solid[]; extraBlockers?: Blocker[]; mission: Mission; roads?: (id: string, line: V[]) => V[]; plots?: (id: string, ring: V[]) => V[]; bays: V[][]; door: string };
const clone = <T>(x: T): T => JSON.parse(JSON.stringify(x));
const mapPts = (m: Mission, f: (name: string, at: V) => V | undefined): Mission => {
  const n = clone(m);
  for (const x of n.destinations) x.at = f(x.name, x.at) ?? x.at;
  for (const x of n.crew) x.at = f(`crew ${x.name}`, x.at) ?? x.at;
  for (const x of n.points) { if (x.at) x.at = f(x.id, x.at) ?? x.at; else if (x.structure) { const s = civ.structures.find(q => q.id === x.structure)!; const c = P2B(cen(s.poly)); x.at = f(x.id, c) ?? c; delete x.structure; } }
  for (const x of n.cameras) x.at = f(`camera ${x.id}`, x.at) ?? x.at;
  return n;
};
// the fixes every option needs, outside the door question: the crew line 2.5 m east (clear of the 31 m terrace across Rocket Lane), the
// rerouted patrol off Rocket Lane's carriageway, the cabinet and South walkway out of the public tower's band across the square
const FIX: Record<string, V> = { 'crew Fixer': [1.168, -47.2], 'crew Tech': [2.368, -47.2], 'crew Scout': [3.568, -47.2], 'crew Enforcer': [4.768, -47.2], 'patrol-reroute': [20, -46], cabinet: [25.4, -23], 'South walkway': [26.6, -23] };
const M1 = mapPts(mission, n => FIX[n]); // every option starts from the mission with the common fixes
const ringXf = (r: V[], f: XF) => r.map(f);
const bayRings = civ.bays.map(b => b.poly);

// (a) mirror the office plot's contents N–S about its mid-line (block x 62): the public tower, entrance and forecourt go to Apron Road, the official tower, canopy and bay to Orbit Walk
const mirA = viaBlock(([x, y]) => [124 - x, y]);
const optA: Option = {
  id: 'a', title: '(a) mirror the Office in its plot', door: 'Office entrance',
  civicXf: (_id, r) => (P2B(cen(r))[0] > 31 ? ringXf(r, mirA) : r),
  mission: mapPts(M1, (_n, at) => (at[0] > 31 ? [124 - at[0], at[1]] : undefined)), bays: [bayRings[1]!, ringXf(bayRings[0]!, mirA)],
};
// (b) the public door and the paintings' entrance turn onto the Office's west face (block (64, -40.5), front west); the forecourt in front of the old door turns with it
const PIV0: V = [52.5, -30], PIV1: V = [64, -40.5];
const rotB = (b: V): V => [PIV1[0] - (b[1] - PIV0[1]), PIV1[1] + (b[0] - PIV0[0])];
const inForecourt = (r: V[]) => inB(cen(r), 44, 52.55, -40.2, -19.8);
// the sweep, the camera and the cabinet stay within 5.5 m of the west face: further west, the 35 m terrace across Rocket Lane (bld/rocket-west/17) hides the ground
const B_SITES: Record<string, V> = { 'patrol-a': [68, -44.8], 'patrol-b': [52, -44.8], 'camera entrance': [53, -44.5], 'Service lane': [70.5, -43.2], cabinet: [46.5, -44.5] };
const optB: Option = {
  id: 'b', title: '(b) front door and entrance onto the west face', door: 'Office entrance',
  civicXf: (id, r) => (/^civic\/(civic-office|cafe)/.test(id) ? r : inForecourt(r) ? ringXf(r, viaBlock(rotB)) : r),
  mission: mapPts(M1, (n, at) => B_SITES[n] ?? (n === 'Office entrance' || n === 'Office approach' ? rotB(at) : undefined)), bays: bayRings,
};
// (c) the whole civic site mirrored N–S in the plan (about block x 27): the Office plot to the north, the square to the south; Orbit Walk moves (Datum)
const mirC = viaBlock(([x, y]) => [54 - x, y]);
const optC: Option = {
  id: 'c', title: '(c) turn the civic site round in the plan', door: 'Office entrance',
  civicXf: (_id, r) => ringXf(r, mirC), mission: mapPts(M1, (_n, at) => [54 - at[0], at[1]]), bays: bayRings.map(b => ringXf(b, mirC)),
  roads: (id, line) => (id === 'road/orbit-walk' ? line.map(([x, y]) => [x, -514 - y] as V) : line),
  plots: (id, ring) => (id === 'plot/records-square' || id === 'plot/records-office' ? ring.map(mirC) : ring),
};
// (d) the paintings' layout in the plots as they are: the Office at the square's north edge, front south over the square; the café on the
// square's east side, front west; the old office plot a low garden with the official bay on Orbit Walk; tall masses all at the back
const OFF_D = (p: V): V => [p[0] - 6.5, -294 * 2 - p[1] + 74.5]; // the Office mirrored N–S about its centre (public tower south), then moved: door face y -231, axis x -166.5
const CAFE_C: V = [-159, -213.65], CAFE_N: V = [-146.65, -247];
const CAFE_D = (p: V): V => [CAFE_N[0] + (p[1] - CAFE_C[1]), CAFE_N[1] - (p[0] - CAFE_C[0])]; // quarter turn clockwise: the south door faces west
const FOUNT_D = (p: V): V => [p[0] - 7, p[1] - 9.5];
const isOffice = (id: string) => /^civic\/(civic-office|office-public-entrance|office-public-landing|office-public-steps|cyp-office|top-office)/.test(id);
const isCafe = (r: V[]) => { const c = P2B(cen(r)); return c[0] < -3.2 && c[1] > -45 && c[1] < -12; }; // the café, its terrace, parasols, tables and front bed
const isFountain = (r: V[]) => { const c = cen(r); return Math.hypot(c[0] + 160, c[1] + 236.5) < 10.5; };
const D_PTS: Record<string, V> = {
  'Office entrance': [-166.5, -231.75], 'Office approach': [-166.5, -234.9], 'Official entrance': [-166.5, -231.75], 'Meet Vale': [-166.5, -231.75],
  'Official drop-off': [-169, -259.4], 'Meet driver': [-172, -259.4], 'Boarding pavilion': [-169, -258.4], 'Service lane': [-178.6, -222], 'South walkway': [-146.6, -261],
  'patrol-a': [-170.5, -242.75], 'patrol-b': [-152.5, -242.75], 'camera entrance': [-177, -238], 'camera pickup': [-178.5, -253], cabinet: [-176.5, -257.5],
  'rival-park': [-176, -265.5], 'patrol-reroute': [-158, -259], 'block-spot': [-175.5, -255.5],
};
const optD: Option = {
  id: 'd', title: '(d) the paintings\' layout: Office and café at the back of the square', door: 'Office entrance',
  civicXf: (id, r) => (isOffice(id) ? ringXf(r, OFF_D) : /^civic\/(official|office-official|top-official|cyp-facade|civic-office)/.test(id) ? ringXf(r, OFF_D) : isCafe(r) ? ringXf(r, CAFE_D) : isFountain(r) ? ringXf(r, FOUNT_D) : inB(cen(r), 40, 90, -60, 10) ? null : r),
  mission: mapPts(M1, (n, at) => { if (D_PTS[n]) return P2B(D_PTS[n]!); const p = toPlan(at); if (at[0] < -3.2 && at[0] > -30 && at[1] > -45 && at[1] < -12) return P2B(CAFE_D(p)); if (Math.hypot(p[0] + 160, p[1] + 236.5) < 10.5) return P2B(FOUNT_D(p)); return undefined; }),
  bays: [bayRings[1]!, [[-177, -261], [-161, -261], [-161, -258], [-177, -258]]],
};
const NOW: Option = { id: 'now', title: 'now (as built, 2026-10-15)', door: 'Office entrance', civicXf: (_i, r) => r, mission, bays: bayRings };
const NOWFIX: Option = { ...NOW, id: 'now+fix', title: 'now, with the common fixes', mission: M1 };
const OPTIONS = [NOW, NOWFIX, optA, optB, optC, optD];

// ---- measure ----
const blockersOf = (o: Option): Blocker[] => B0.flatMap(b => { if (!b.id.startsWith('civic/')) return [b]; const r = o.civicXf(b.id.split('#')[0]!, b.ring); return r ? [{ ...b, ring: r, box: undefined }] : []; }).concat(o.extraBlockers ?? []);
const civOf = (o: Option): Civic => {
  const xf = (id: string, r: V[]) => o.civicXf(`civic/${id}`, r) ?? r;
  return { ...civ, buildings: civ.buildings.map(b => ({ ...b, poly: xf(b.id, b.poly), towers: b.towers?.map(t => ({ ...t, poly: t.poly && xf(`${b.id}/tower-${t.id}`, t.poly) })) })), bays: o.bays.map((poly, i) => ({ id: civ.bays[i]?.id ?? `bay${i}`, poly })), solids: (civ.solids ?? []).map(s => o.civicXf('civic/solid', s) ?? s).filter(s => !(o.id === 'd' && inB(cen(s), 40, 90, -60, 10))) };
};
const pt = (m: Mission, n: string) => toPlan(m.destinations.find((x: any) => x.name === n)!.at as V);
type Result = { id: string; title: string; rows: Row[]; v1: ReturnType<typeof v1>; walks: Record<string, number>; ground: Record<string, number> };
const results: Result[] = [];
const groundShare = (B: Blocker[], ring: V[], skip: V[][]) => { // the open ground of a plot seen past architecture (a figure's middle, 0.9 m up), 1 m grid
  const xs = ring.map(p => p[0]), ys = ring.map(p => p[1]); let n = 0, ok = 0;
  for (let x = Math.min(...xs) + 0.5; x < Math.max(...xs); x += 1) for (let y = Math.min(...ys) + 0.5; y < Math.max(...ys); y += 1) {
    const p: V = [x, y]; if (!inPoly(p, ring) || skip.some(s => inPoly(p, s))) continue; n++;
    if (!B.some(b => b.kind === 'architecture' && blocks(p, 1.08, b))) ok++;
  }
  return n ? ok / n : 1;
};
for (const o of OPTIONS) {
  const B = blockersOf(o), c2 = civOf(o), m = o.mission, rows = visibility(m, c2, plan, B);
  const walk = (a: string, b: string) => Math.round(walkBetween(plan, d, c2, pt(m, a), pt(m, b), 0.25) * 10) / 10;
  const foot = c2.buildings.flatMap(b => [b.poly, ...(b.towers ?? []).flatMap(t => (t.poly ? [t.poly] : []))]);
  const plots = w.ground.plots.filter(p => /records-(square|office)$/.test(p.id)).map(p => ({ id: p.id, ring: o.plots ? o.plots(p.id, p.ring) : p.ring }));
  results.push({
    id: o.id, title: o.title, rows, v1: v1(rows),
    walks: { 'guard: Office entrance → Café door': walk('Office entrance', 'Café door'), 'escort: Service lane → Getaway bay': walk('Service lane', 'Getaway bay'), 'Vale: Official entrance → Meet driver': walk('Official entrance', 'Meet driver') },
    ground: Object.fromEntries(plots.map(p => [p.id.replace('plot/', ''), Math.round(groundShare(B, p.ring, foot) * 1000) / 1000])),
  });
}

// ---- pictures ----
const C: Record<string, RGB> = { plot: [229, 219, 203], road: [168, 163, 176], bay: [190, 184, 198], garden: [196, 214, 172], bld: [233, 226, 214], office: [243, 236, 220], tower: [236, 226, 206], cafe: [214, 234, 228], entrance: [118, 182, 168], green: [92, 140, 96], parasol: [236, 206, 196], water: [150, 202, 214], low: [204, 196, 182], bed: [150, 182, 128] };
const ribbon = (line: V[], half: number): V[][] => {
  const quads: V[][] = []; // one quad a segment, overlapping a little at the joints
  for (let i = 0; i + 1 < line.length; i++) {
    const a = line[i]!, b = line[i + 1]!, dx = b[0] - a[0], dy = b[1] - a[1], n = Math.hypot(dx, dy) || 1, nx = (-dy / n) * half, ny = (dx / n) * half, ex = (dx / n) * 0.05, ey = (dy / n) * 0.05;
    quads.push([[a[0] + nx - ex, a[1] + ny - ey], [b[0] + nx + ex, b[1] + ny + ey], [b[0] - nx + ex, b[1] - ny + ey], [a[0] - nx - ex, a[1] - ny - ey]]);
  }
  return quads;
};
const near = (r: V[], f: V, rad: number) => r.some(p => Math.hypot(p[0] - f[0], p[1] - f[1]) < rad);
function scene(o: Option, f: V, rad: number): { flats: Flat[]; solids: Solid[] } {
  const flats: Flat[] = [], solids: Solid[] = [];
  const inView = (r: V[]) => { const xs = r.map(p => p[0]), ys = r.map(p => p[1]); return Math.max(...xs) > f[0] - rad * 2 && Math.min(...xs) < f[0] + rad * 2 && Math.max(...ys) > f[1] - rad * 2 && Math.min(...ys) < f[1] + rad * 2.5; };
  for (const p of w.ground.plots) { const ring = o.plots ? o.plots(p.id, p.ring) : p.ring; if (inView(ring)) flats.push({ ring, z: 0.18, color: o.id === 'd' && p.id === 'plot/records-office' ? C.garden! : C.plot! }); }
  for (const r of w.ground.roads) { const line = (o.roads ? o.roads(r.id, r.line) : r.line); for (const q of ribbon(line, r.width / 2)) if (inView(q)) flats.push({ ring: q, z: 0.02, color: C.road! }); }
  for (const b of o.bays) flats.push({ ring: b, z: 0.05, color: C.bay! });
  for (const b of blockersOf(o)) {
    if (!inView(b.ring)) continue;
    const color = b.kind === 'dressing' ? (/parasol/.test(b.id) ? C.parasol! : /fountain/.test(b.id) ? C.water! : C.green!) : /cafe/.test(b.id) ? C.cafe! : /tower/.test(b.id) ? C.tower! : /entrance/.test(b.id) ? C.entrance! : /^civic/.test(b.id) ? C.office! : C.bld!;
    solids.push({ id: b.id, ring: b.ring, z0: b.z0, z1: b.z1, color });
  }
  for (const m of w.civic) { // low furniture for the read: beds, planters, tables, the terrace, the steps and landing
    if (m.height >= 2.5 || !m.ring.length || /Inlay|BedTuft|Bollard|Topiary/.test(m.kit?.id ?? '') || !near(m.ring, f, rad * 2)) continue;
    const r = o.civicXf(m.id, m.ring); if (!r) continue;
    const top = /CafeTerrace/.test(m.kit?.id ?? '') ? m.base + 0.05 : m.base + m.height;
    solids.push({ id: m.id, ring: r, z0: m.base, z1: top, color: /bed|Planter|Palm/.test(`${m.kit?.id ?? ''}${m.surface}`) ? C.bed! : /Pier|Cabinet/.test(m.kit?.id ?? '') ? C.low! : C.low! });
  }
  return { flats, solids };
}
const esc = (s: string) => s.replace(/&/g, '&amp;').replace(/</g, '&lt;');
const KEY = ['Café door', 'Office entrance', 'Office approach', 'Service lane', 'Official entrance', 'Meet driver', 'Getaway bay', 'South walkway', 'Behind café', 'cabinet', 'beside-inez', 'patrol-a', 'patrol-b', 'rival-park', 'crew Enforcer', 'block-spot'];
const colourOf = (r: Row) => (r.seen >= V1_LIMIT ? '#1f8f5f' : r.onlyFade > 0 ? '#e0761b' : '#c62828');
function markers(rows: Row[], focus: V, view: View, size: V, opt: { labels?: boolean; all?: boolean } = {}): string {
  const out: string[] = [];
  for (const r of rows) {
    if (r.kind !== 'person') continue;
    const show = opt.all || KEY.includes(r.what);
    const feet = project(r.p, r.z, focus, view, size), head = project(r.p, r.z + 1.75, focus, view, size);
    if (feet[0] < -40 || feet[0] > size[0] + 40 || feet[1] < -40 || feet[1] > size[1] + 40) continue;
    const c = colourOf(r), hidden = r.seen < V1_LIMIT;
    out.push(`<line x1="${feet[0]}" y1="${feet[1]}" x2="${head[0]}" y2="${head[1]}" stroke="${c}" stroke-width="5" ${hidden ? 'stroke-dasharray="5 4"' : ''} stroke-linecap="round"/>`);
    out.push(`<circle cx="${feet[0]}" cy="${feet[1]}" r="${show ? 8 : 5}" fill="${hidden ? 'white' : c}" stroke="${c}" stroke-width="3"/>`);
    if (opt.labels !== false && show) out.push(`<text x="${feet[0] + 11}" y="${feet[1] - 6}" font-family="Helvetica, Arial" font-size="17" font-weight="600" fill="#1d2433" stroke="white" stroke-width="4" paint-order="stroke">${esc(r.what)} ${Math.round(r.seen * 100)} %</text>`);
  }
  return out.join('\n');
}
function compose(file: string, base: Uint8Array, size: V, overlay: string, line1: string, line2: string, outW?: number) {
  const caption = `${line1}\n${line2}`;
  const b64 = Buffer.from(base).toString('base64');
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="${size[0]}" height="${size[1]}" viewBox="0 0 ${size[0]} ${size[1]}">
<image width="${size[0]}" height="${size[1]}" xlink:href="data:image/png;base64,${b64}"/>
${overlay}
<rect x="0" y="${size[1] - 74}" width="${size[0]}" height="74" fill="white" fill-opacity="0.86"/>
<text x="18" y="${size[1] - 44}" font-family="Helvetica, Arial" font-size="22" font-weight="700" fill="#1d2433">${esc(caption.split('\n')[0]!)}</text>
<text x="18" y="${size[1] - 16}" font-family="Helvetica, Arial" font-size="17" fill="#333">${esc(caption.split('\n')[1] ?? '')}</text>
<g transform="translate(${size[0] - 560}, ${size[1] - 66})" font-family="Helvetica, Arial" font-size="16" fill="#1d2433">
<circle cx="10" cy="10" r="7" fill="#1f8f5f"/><text x="24" y="15">seen (≥ 50 % of a figure)</text>
<circle cx="230" cy="10" r="7" fill="white" stroke="#e0761b" stroke-width="3"/><text x="244" y="15">only through the fade</text>
<circle cx="10" cy="40" r="7" fill="white" stroke="#c62828" stroke-width="3"/><text x="24" y="45">hidden</text>
<text x="230" y="45">dashed = behind architecture</text></g>
</svg>`;
  const tmp = resolve(ROOT, `out/vantage/${file}.svg`); mkdirSync(resolve(ROOT, 'out/vantage'), { recursive: true }); writeFileSync(tmp, svg);
  execFileSync('rsvg-convert', [...(outW ? ['-w', String(outW)] : []), '-o', resolve(OUT, `${file}.png`), tmp]);
}
const SIZE: V = [1600, 900];
const overviewFocus = (o: Option): V => (o.id === 'c' ? [-157, -257] : o.id === 'd' ? [-158, -240] : [-157, -262]);
for (const o of OPTIONS.filter(x => x.id !== 'now+fix')) {
  const res = results.find(r => r.id === o.id)!;
  const fo = overviewFocus(o), view: View = { ...PLAY, zoom: 34 }, S = scene(o, fo, 70);
  const base = png(render({ focus: fo, view, size: SIZE, ss: 2 }, S.flats, S.solids), SIZE[0], SIZE[1]);
  const walk = res.walks['guard: Office entrance → Café door']!;
  compose(`${o.id}-overview`, base, SIZE, markers(res.rows, fo, view, SIZE), `${o.title} · V-1 ${res.v1.value} points seen`, `play camera angles (yaw 30, pitch 38), zoomed out to 34 · guard walk ${walk} m = ${Math.round(walk / 1.4)} s · square ground seen ${Math.round((res.ground['records-square'] ?? 1) * 100)} %`);
  const door = pt(o.mission, o.door), dv: View = { ...PLAY };
  const S2 = scene(o, door, 40), base2 = png(render({ focus: door, view: dv, size: SIZE, ss: 2 }, S2.flats, S2.solids), SIZE[0], SIZE[1]);
  const rd = res.rows.find(r => r.what === 'Office entrance')!;
  compose(`${o.id}-door`, base2, SIZE, markers(res.rows, door, { ...dv, zoom: 14 }, SIZE), `${o.title} · the public door at play zoom 14`, `Office entrance ${Math.round(rd.seen * 100)} % seen past architecture (${Math.round(rd.onlyFade * 100)} % only through the fade) · frame 50 × 28 m as the player sees it (1600 px wide here)`);
}
// Latch's plates, marked: the same points projected with each plate's own camera (focus at height 0, ortho 14, 1920 × 1080)
const plates = J('briefs/011/ui/plates/plates.json').plates as { file: string; camera: { focusPlan: V; zoom: number } }[];
const nowRows = results[0]!.rows;
for (const f of ['office-door-1730.png', 'civic-block-1728.png', 'cafe-front-1730.png', 'service-lane-1730.png']) {
  const p = plates.find(x => x.file === f)!, view: View = { ...PLAY, zoom: p.camera.zoom }, size: V = [1920, 1080];
  const img = readFileSync(resolve(ROOT, 'briefs/011/ui/plates', f));
  compose(`now-plate-${f.replace('.png', '')}`, img, size, markers(nowRows, p.camera.focusPlan, view, size, { all: f !== 'civic-block-1728.png' }), `Latch's Unity plate ${f} (17:30, fade on), marked with V-1`, 'each point: feet circle, 1.75 m figure line; colour = what the camera sees without the fade', 1600);
}
writeFileSync(resolve(OUT, 'study.json'), JSON.stringify({
  made: 'bun briefs/011/vantage/study.ts', camera: PLAY, limit: V1_LIMIT, sources: { plan: plan.version, block: block.adjustments?.sha256, mission: 'contracts/missions/handover/mission.json' },
  options: results.map(r => ({ id: r.id, title: r.title, v1: { value: r.v1.value, ok: r.v1.ok, detail: r.v1.detail }, walks: r.walks, ground: r.ground, rows: r.rows.map(x => ({ what: x.what, kind: x.kind, plan: x.p.map(v => Math.round(v * 100) / 100), z: x.z, seen: Math.round(x.seen * 100) / 100, clear: Math.round(x.clear * 100) / 100, onlyFade: Math.round(x.onlyFade * 100) / 100, by: x.by })) })),
}, null, 1));
for (const r of results) console.log(`${r.id.padEnd(4)} V-1 ${r.v1.value.padEnd(6)} walks ${Object.values(r.walks).join(' / ')} m · ground ${JSON.stringify(r.ground)}\n     ${r.rows.filter(x => x.seen < V1_LIMIT).map(x => `${x.what} ${Math.round(x.seen * 100)}`).join(', ')}`);
rmSync(resolve(ROOT, 'out/vantage/tmp'), { recursive: true, force: true });
