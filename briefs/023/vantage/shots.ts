// Brief 023 · Vantage · door option (b) made real (Sky 2026-10-19): the Records Office's public door, its landing, steps and forecourt on the
// wing's west face (contracts/world/civic-block.adjust.json, `bun run block`) and the Handover points moved with it (contracts/missions/handover/mission.json).
// Pictures at the play camera (GDD §10: pitch 38, zoom 14 for the door shot; zoomed out to 34 for the overview) at the opening yaw 30 and at yaw 210,
// each marked with rule V-1's own measure of every point from that heading (tools/missions/handover/visibility.ts on tools/missions/sight.ts).
// Renders the sources as they are on disk, through the checker's generated world (in memory or out/cache); writes nothing else and never out/world.
// Run (repo root): bun briefs/023/vantage/shots.ts → briefs/023/vantage/{door,service,overview}-{30,210}.png. Needs rsvg-convert.
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { execFileSync } from 'node:child_process';
import { resolve } from 'node:path';
import { derive } from '../../../tools/plan/plan';
import { civic, toPlanFn } from '../../../tools/plan/civic';
import { generated } from '../../../tools/plan/world-ctx';
import { walkBetween } from '../../../tools/plan/walk-fine';
import type { V } from '../../../tools/plan/geom';
import { type View, PLAY, project } from '../../../tools/missions/sight';
import { blockersFor, visibility, v1, type Row, V1_LIMIT } from '../../../tools/missions/handover/visibility';
import { render, png, type Solid, type Flat, type RGB } from '../../011/vantage/oblique';

const ROOT = resolve(import.meta.dir, '../../..'), OUT = resolve(import.meta.dir);
const J = (p: string) => JSON.parse(readFileSync(resolve(ROOT, p), 'utf8'));
const plan = J('contracts/world/harbour-district.plan.json'), block = J(plan.civic.block), mission = J('contracts/missions/handover/mission.json');
const d = derive(plan), civ = civic(plan.civic, block), { w } = generated(plan, d, civ), toPlan = toPlanFn(plan.civic);
const B = blockersFor(w, civ, plan);
const atYaw = (yaw: number) => { const m = JSON.parse(JSON.stringify(mission)); for (const k of ['destinations', 'crew', 'points', 'cameras']) for (const x of m[k] ?? []) x.yaw = yaw; return m; };
const rowsAt = (yaw: number) => visibility(atYaw(yaw), civ, plan, B);
const asChecked = v1(visibility(mission, civ, plan, B));
const pt = (n: string): V => toPlan(mission.destinations.find((x: any) => x.name === n)!.at as V);
const walk = (a: string, b: string) => Math.round(walkBetween(plan, d, civ, pt(a), pt(b), 0.25) * 10) / 10;
const walks = { guard: walk('Office entrance', 'Café door'), escort: walk('Service lane', 'Getaway bay'), vale: walk('Official entrance', 'Meet driver') };
const office = block.buildings.find((b: any) => b.id === 'civic-office');
const serviceDoor = toPlan(office.entrances.find((e: any) => e.id === 'office-service-door').at as V);

// ---- scene (as briefs/011/vantage/study.ts draws it) ----
const C: Record<string, RGB> = { plot: [229, 219, 203], road: [168, 163, 176], bay: [190, 184, 198], bld: [233, 226, 214], office: [243, 236, 220], tower: [236, 226, 206], cafe: [214, 234, 228], entrance: [118, 182, 168], green: [92, 140, 96], parasol: [236, 206, 196], water: [150, 202, 214], low: [204, 196, 182], bed: [150, 182, 128], steps: [222, 210, 190] };
const ribbon = (line: V[], half: number): V[][] => {
  const q: V[][] = [];
  for (let i = 0; i + 1 < line.length; i++) { const a = line[i]!, b = line[i + 1]!, dx = b[0] - a[0], dy = b[1] - a[1], n = Math.hypot(dx, dy) || 1, nx = (-dy / n) * half, ny = (dx / n) * half, ex = (dx / n) * 0.05, ey = (dy / n) * 0.05; q.push([[a[0] + nx - ex, a[1] + ny - ey], [b[0] + nx + ex, b[1] + ny + ey], [b[0] - nx + ex, b[1] - ny + ey], [a[0] - nx - ex, a[1] - ny - ey]]); }
  return q;
};
const near = (r: V[], f: V, rad: number) => r.some(p => Math.hypot(p[0] - f[0], p[1] - f[1]) < rad);
function scene(f: V, rad: number): { flats: Flat[]; solids: Solid[] } {
  const flats: Flat[] = [], solids: Solid[] = [];
  const inView = (r: V[]) => { const xs = r.map(p => p[0]), ys = r.map(p => p[1]); return Math.max(...xs) > f[0] - rad * 2.5 && Math.min(...xs) < f[0] + rad * 2.5 && Math.max(...ys) > f[1] - rad * 2.5 && Math.min(...ys) < f[1] + rad * 2.5; };
  for (const p of w.ground.plots) if (inView(p.ring)) flats.push({ ring: p.ring, z: 0.18, color: C.plot! });
  for (const r of w.ground.roads) for (const q of ribbon(r.line, r.width / 2)) if (inView(q)) flats.push({ ring: q, z: 0.02, color: C.road! });
  for (const b of civ.bays) flats.push({ ring: b.poly, z: 0.19, color: C.bay! }); // drawn over the plot slab (0.18) so the lay-bys read; they are carriageway level
  const drive = ((civ.dressing as any)?.keepClear ?? []).find((k: any) => k.id === 'service-drive'); // the reserved drive to serviceBay (not yet a world surface)
  if (drive) flats.push({ ring: drive.polygon.map(toPlan), z: 0.19, color: [208, 202, 214] });
  for (const b of B) {
    if (!inView(b.ring)) continue;
    const color = b.kind === 'dressing' ? (/parasol/.test(b.id) ? C.parasol! : /fountain/.test(b.id) ? C.water! : C.green!) : /cafe/.test(b.id) ? C.cafe! : /tower/.test(b.id) ? C.tower! : /entrance/.test(b.id) ? C.entrance! : /^civic/.test(b.id) ? C.office! : C.bld!;
    solids.push({ id: b.id, ring: b.ring, z0: b.z0, z1: b.z1, color });
  }
  for (const m of w.civic) { // low furniture: beds, pots, cabinets, the terrace, the steps and landing
    if (m.height >= 2.5 || !m.ring.length || /Inlay|BedTuft|Bollard/.test(m.kit?.id ?? '') || !near(m.ring, f, rad * 2.5)) continue;
    const top = /CafeTerrace/.test(m.kit?.id ?? '') ? m.base + 0.05 : m.base + m.height;
    solids.push({ id: m.id, ring: m.ring, z0: m.base, z1: top, color: /office-public-(steps|landing)/.test(m.id) ? C.steps! : /bed|Planter|Palm|Topiary|hedge/.test(`${m.kit?.id ?? ''}${m.surface}${m.id}`) ? C.bed! : C.low! });
  }
  return { flats, solids };
}

// ---- markers ----
const esc = (s: string) => s.replace(/&/g, '&amp;').replace(/</g, '&lt;');
const KEY = ['Café door', 'Office entrance', 'Office approach', 'Service lane', 'Official entrance', 'Meet driver', 'Getaway bay', 'South walkway', 'Behind café', 'cabinet', 'patrol-a', 'patrol-b', 'patrol-reroute', 'crew Enforcer', 'block-spot'];
const colourOf = (r: Row) => (r.seen >= V1_LIMIT ? '#1f8f5f' : r.onlyFade > 0 ? '#e0761b' : '#c62828');
function markers(rows: Row[], focus: V, view: View, size: V, labels: boolean): string {
  const out: string[] = [];
  const cam = mission.cameras.find((c: any) => c.id === 'entrance');
  if (cam) { const a = project(toPlan(cam.at), 0.18, focus, view, size), b = project(pt(cam.looksAt), 0.18, focus, view, size); out.push(`<line x1="${a[0]}" y1="${a[1]}" x2="${b[0]}" y2="${b[1]}" stroke="#7a3db8" stroke-width="3" stroke-dasharray="8 6"/><circle cx="${a[0]}" cy="${a[1]}" r="7" fill="#7a3db8"/>${labels ? `<text x="${a[0] - 10}" y="${a[1] + 26}" font-family="Helvetica, Arial" font-size="16" font-weight="600" fill="#7a3db8" stroke="white" stroke-width="4" paint-order="stroke">entrance camera</text>` : ''}`); }
  const sd = project(serviceDoor, 0.18, focus, view, size);
  out.push(`<rect x="${sd[0] - 7}" y="${sd[1] - 7}" width="14" height="14" fill="#333"/>${labels ? `<text x="${sd[0] + 11}" y="${sd[1] + 18}" font-family="Helvetica, Arial" font-size="16" font-weight="600" fill="#333" stroke="white" stroke-width="4" paint-order="stroke">service door</text>` : ''}`);
  for (const r of rows) {
    if (r.kind !== 'person') continue;
    const feet = project(r.p, r.z, focus, view, size), head = project(r.p, r.z + 1.75, focus, view, size);
    if (feet[0] < -40 || feet[0] > size[0] + 40 || feet[1] < -40 || feet[1] > size[1] + 40) continue;
    const c = colourOf(r), hidden = r.seen < V1_LIMIT, show = KEY.includes(r.what);
    out.push(`<line x1="${feet[0]}" y1="${feet[1]}" x2="${head[0]}" y2="${head[1]}" stroke="${c}" stroke-width="5" ${hidden ? 'stroke-dasharray="5 4"' : ''} stroke-linecap="round"/>`);
    out.push(`<circle cx="${feet[0]}" cy="${feet[1]}" r="${show ? 8 : 5}" fill="${hidden ? 'white' : c}" stroke="${c}" stroke-width="3"/>`);
    if (labels && show) out.push(`<text x="${feet[0] + 11}" y="${feet[1] - 6}" font-family="Helvetica, Arial" font-size="17" font-weight="600" fill="#1d2433" stroke="white" stroke-width="4" paint-order="stroke">${esc(r.what)} ${Math.round(r.seen * 100)} %</text>`);
  }
  return out.join('\n');
}
function compose(file: string, base: Uint8Array, size: V, overlay: string, line1: string, line2: string) {
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="${size[0]}" height="${size[1]}" viewBox="0 0 ${size[0]} ${size[1]}">
<image width="${size[0]}" height="${size[1]}" xlink:href="data:image/png;base64,${Buffer.from(base).toString('base64')}"/>
${overlay}
<rect x="0" y="${size[1] - 74}" width="${size[0]}" height="74" fill="white" fill-opacity="0.86"/>
<text x="18" y="${size[1] - 44}" font-family="Helvetica, Arial" font-size="22" font-weight="700" fill="#1d2433">${esc(line1)}</text>
<text x="18" y="${size[1] - 16}" font-family="Helvetica, Arial" font-size="17" fill="#333">${esc(line2)}</text>
<g transform="translate(${size[0] - 560}, ${size[1] - 66})" font-family="Helvetica, Arial" font-size="16" fill="#1d2433">
<circle cx="10" cy="10" r="7" fill="#1f8f5f"/><text x="24" y="15">seen (≥ 50 % of a figure)</text>
<circle cx="230" cy="10" r="7" fill="white" stroke="#e0761b" stroke-width="3"/><text x="244" y="15">only through the fade</text>
<circle cx="10" cy="40" r="7" fill="white" stroke="#c62828" stroke-width="3"/><text x="24" y="45">hidden</text>
<text x="230" y="45">dashed = behind architecture</text></g>
</svg>`;
  const tmp = resolve(ROOT, `out/vantage/023/${file}.svg`); mkdirSync(resolve(ROOT, 'out/vantage/023'), { recursive: true }); writeFileSync(tmp, svg);
  execFileSync('rsvg-convert', ['-o', resolve(OUT, `${file}.png`), tmp]);
}

const SIZE: V = [1600, 900], door = pt('Office entrance'), OVER: V = [-160, -268];
const count = (rows: Row[]) => `${rows.filter(r => r.seen >= V1_LIMIT - 1e-9).length}/${rows.length}`;
for (const yaw of [30, 210]) {
  const rows = rowsAt(yaw), ov: View = { ...PLAY, yaw, zoom: 34 }, S = scene(OVER, 70);
  compose(`overview-${yaw}`, png(render({ focus: OVER, view: ov, size: SIZE, ss: 2 }, S.flats, S.solids), SIZE[0], SIZE[1]), SIZE, markers(rows, OVER, ov, SIZE, true),
    `Door option (b) · yaw ${yaw}, zoomed out to 34 · all points read at yaw ${yaw}: ${count(rows)} seen`,
    `V-1 as checked (each point at its own heading: the front at 30, Service lane at 210): ${asChecked.value} · guard walk ${walks.guard} m = ${Math.round(walks.guard / 1.4)} s · escort ${walks.escort} m`);
  const dv: View = { ...PLAY, yaw }, S2 = scene(door, 40), rd = rows.find(r => r.what === 'Office entrance')!;
  compose(`door-${yaw}`, png(render({ focus: door, view: dv, size: SIZE, ss: 2 }, S2.flats, S2.solids), SIZE[0], SIZE[1]), SIZE, markers(rows, door, dv, SIZE, true),
    `The Records Office public door on the west face · play zoom 14, yaw ${yaw}`,
    `Office entrance ${Math.round(rd.seen * 100)} % seen past architecture (${Math.round(rd.onlyFade * 100)} % only through the fade) · frame 50 × 28 m as the player sees it (1600 px wide here)`);
  const back = pt('Service lane'), S3 = scene(back, 40), rs = rows.find(r => r.what === 'Service lane')!;
  compose(`service-${yaw}`, png(render({ focus: back, view: dv, size: SIZE, ss: 2 }, S3.flats, S3.solids), SIZE[0], SIZE[1]), SIZE, markers(rows, back, dv, SIZE, true),
    `The service door on the east face, the quiet back way · play zoom 14, yaw ${yaw}`,
    `Service lane ${Math.round(rs.seen * 100)} % seen past architecture (${Math.round(rs.onlyFade * 100)} % only through the fade) · escort to the getaway bay ${walks.escort} m`);
  console.log(`yaw ${yaw}: ${count(rows)} seen; hidden: ${rows.filter(r => r.seen < V1_LIMIT).map(r => `${r.what} ${Math.round(r.seen * 100)}`).join(', ')}`);
}
console.log(`V-1 as checked: ${asChecked.value} ${asChecked.ok ? 'PASS' : 'FAIL'}${asChecked.detail ? ` · ${asChecked.detail}` : ''}`);
console.log(`walks: guard Office entrance → Café door ${walks.guard} m · escort Service lane → Getaway bay ${walks.escort} m · Vale Official entrance → Meet driver ${walks.vale} m`);
