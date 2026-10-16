// Brief 014 · Waymark · the cell's start (Sky, playing the Handover: "can we move the start position of the cell - you can't see them!").
// Plan view of the old start (HEAD's mission.json crew) and the new one (the tree's), with the play camera's opening view outlined:
// the ground the screen covers at zoom 14, pitch 38, yaw 30 (GDD §10) around FOCUS, at Sky's play window (1726 x 1142, the brief 014
// screenshots), with the briefing card and the roster drawn where they sit on screen. Measures each crew point with V-1's own code
// (tools/missions/handover/visibility.ts → tools/missions/sight.ts), the engine's walk grid (tools/plan/walk-grid.ts), the watchers'
// fans at 0:00 (the engine's `fans 0`, out/waymark/start/fans0.json), the people's places (P-16 slots: seats, stands, doors, stops,
// bays) and the block's keep-clear zones.
// Run (repo root): dotnet run --project sim/cli -c Release -- fans 0 > out/waymark/start/fans0.json; bun briefs/014/waymark-start.ts
//   → briefs/014/waymark-start.png (rsvg-convert) and the measurements on stdout. Writes nothing else.
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { execFileSync } from 'node:child_process';
import { resolve } from 'node:path';
import { derive } from '../../tools/plan/plan';
import { civic, toPlanFn } from '../../tools/plan/civic';
import { generated } from '../../tools/plan/world-ctx';
import { blockersOf } from '../../tools/plan/walk';
import { walkGrid } from '../../tools/plan/walk-grid';
import { walkBetween } from '../../tools/plan/walk-fine';
import { type V, inPoly } from '../../tools/plan/geom';
import { PLAY, project, seen } from '../../tools/missions/sight';
import { blockersFor } from '../../tools/missions/handover/visibility';

const ROOT = resolve(import.meta.dir, '../..'), OUT = resolve(ROOT, 'briefs/014/waymark-start.png');
const J = (p: string) => JSON.parse(readFileSync(resolve(ROOT, p), 'utf8'));
const plan = J('contracts/world/harbour-district.plan.json'), block = J(plan.civic.block), mission = J('contracts/missions/handover/mission.json');
const head = JSON.parse(execFileSync('git', ['show', 'HEAD:contracts/missions/handover/mission.json'], { cwd: ROOT, encoding: 'utf8' }));
const d = derive(plan), civ = civic(plan.civic, block), toPlan = toPlanFn(plan.civic), { w } = generated(plan, d, civ);
const B = blockersFor(w, civ, plan), G = walkGrid(plan, d, civ), { polys } = blockersOf(plan, d, civ);
const P2B = (p: V): V => [-230 - p[1], p[0] + 130];

/** Sky's play window (the brief 014 screenshots) and what covers it at the start: the briefing card (modal until Begin) and the roster. */
const SCREEN: V = [1726, 1142];
const HUD: { what: string; r: [number, number, number, number] }[] = [
  { what: 'briefing card', r: [605, 150, 1120, 460] }, { what: 'roster', r: [14, 14, 101, 347] }, { what: 'verbs', r: [108, 30, 350, 100] }, { what: 'pill', r: [1225, 14, 1712, 58] },
];
/** The opening focus (block frame, m): the point the camera centres on at 0:00 (Latch: HandoverHost.Begin). */
export const FOCUS: V = (process.env.FOCUS?.split(",").map(Number) as V | undefined) ?? [-2.5, -40.5];

const dest = (m: any, n: string) => m.destinations.find((x: any) => x.name === n).at as V;
const pt = (id: string) => mission.points.find((x: any) => x.id === id).at as V;
const goals: Record<string, { to: string; at: V }> = {
  Scout: { to: 'beside Inez', at: pt('beside-inez') }, Fixer: { to: 'Café door', at: dest(mission, 'Café door') },
  Tech: { to: 'cabinet', at: pt('cabinet') }, Enforcer: { to: 'Office approach', at: dest(mission, 'Office approach') },
};
const fans: { id: string; poly: V[] }[] = J('out/waymark/start/fans0.json').fans.map((f: any) => ({ id: f.watcher, poly: f.poly.map((p: number[]) => [p[0] / 100, p[1] / 100] as V) }));
const places: { id: string; kind: string; b: V }[] = J('out/people/harbour-district.people.json').places.map((p: any) => ({ id: p.id, kind: p.kind, b: P2B(p.at) }));
const keep = (block.dressing.keepClear as any[]);
const segDist = (p: V, a: V, b: V) => { const dx = b[0] - a[0], dy = b[1] - a[1], L = dx * dx + dy * dy, t = L ? Math.max(0, Math.min(1, ((p[0] - a[0]) * dx + (p[1] - a[1]) * dy) / L)) : 0; return Math.hypot(p[0] - a[0] - t * dx, p[1] - a[1] - t * dy); };
const polyDist = (p: V, poly: V[]) => (inPoly(p, poly) ? 0 : Math.min(...poly.map((a, i) => segDist(p, a, poly[(i + 1) % poly.length]!))));
const inKeep = (b: V) => keep.filter(k => (k.polygon ? inPoly(b, k.polygon) : Math.hypot(b[0] - k.ring.centre[0], b[1] - k.ring.centre[1]) >= k.ring.r0 && Math.hypot(b[0] - k.ring.centre[0], b[1] - k.ring.centre[1]) <= k.ring.r1)).map(k => k.id);
const onScreen = (b: V) => project(toPlan(b), 0.18 + 0.9, toPlan(FOCUS), PLAY, SCREEN);

const measure = (label: string, crew: { name: string; at: V }[]) => crew.map(c => {
  const s = seen(toPlan(c.at), 0.18, B), g = goals[c.name]!, near = places.map(q => ({ q, dd: Math.hypot(q.b[0] - c.at[0], q.b[1] - c.at[1]) })).sort((a, b) => a.dd - b.dd)[0]!;
  const sc = onScreen(c.at), hud = HUD.filter(h => sc[0] >= h.r[0] && sc[0] <= h.r[2] && sc[1] >= h.r[1] && sc[1] <= h.r[3]).map(h => h.what);
  const inFrame = sc[0] >= 0 && sc[0] <= SCREEN[0] && sc[1] >= 0 && sc[1] <= SCREEN[1];
  return {
    start: label, who: c.name, at: c.at, free: G.free(c.at), seen: +s.pastArchitecture.toFixed(2), clear: +s.clear.toFixed(2), by: s.by,
    fanM: +Math.min(...fans.map(f => polyDist(c.at, f.poly))).toFixed(1), nearestPlace: `${near.q.id} ${near.dd.toFixed(1)} m`, keepClear: inKeep(c.at),
    to: g.to, walkM: +walkBetween(plan, d, civ, toPlan(c.at), toPlan(g.at)).toFixed(1), screen: sc.map(Math.round), inFrame, underHud: hud,
  };
});
const was = measure('old', head.crew), now = measure('new', mission.crew);
const apart = (cr: { name: string; at: V }[]) => cr.flatMap((a, i) => cr.slice(i + 1).map(b => `${a.name[0]}-${b.name[0]} ${Math.hypot(a.at[0] - b.at[0], a.at[1] - b.at[1]).toFixed(1)}`));
for (const r of [...was, ...now]) console.log(JSON.stringify(r));
console.log('apart new:', apart(mission.crew).join(', '));
console.log('focus', FOCUS, 'plan', toPlan(FOCUS).map(x => +x.toFixed(2)), Object.fromEntries(['Café door', 'Getaway bay', 'Fountain'].map(n => [n, onScreen(dest(mission, n)).map(Math.round).join(',')])));

// ---- the picture: top down, turned so the camera's screen-up points up the page (north arrow drawn)
const yaw = (PLAY.yaw * Math.PI) / 180, R: V = [Math.cos(yaw), -Math.sin(yaw)], F: V = [Math.sin(yaw), Math.cos(yaw)];
const C = toPlan(FOCUS), S = 13, W = 1500, H = 1180, ox = W / 2 - 40, oy = H / 2 + 10;
const X = (b: V): V => { const p = toPlan(b), q: V = [p[0] - C[0], p[1] - C[1]]; return [ox + S * (q[0] * R[0] + q[1] * R[1]), oy - S * (q[0] * F[0] + q[1] * F[1])]; };
const Xp = (p: V): V => X(P2B(p));
const path = (pts: V[]) => 'M' + pts.map(p => `${p[0].toFixed(1)},${p[1].toFixed(1)}`).join(' L') + ' Z';
const esc = (s: string) => s.replace(/&/g, '&amp;').replace(/</g, '&lt;');
const svg: string[] = [];
const add = (s: string) => svg.push(s);
add(`<svg xmlns="http://www.w3.org/2000/svg" width="${W}" height="${H}" viewBox="0 0 ${W} ${H}" font-family="Helvetica, Arial, sans-serif">`);
add(`<rect width="${W}" height="${H}" fill="#f4efe6"/>`);
// hidden ground: a 1 m lattice where a standing figure is < 50 % seen past architecture (V-1)
for (let x = -40; x <= 50; x += 1) for (let y = -80; y <= 0; y += 1) {
  const b: V = [x + 0.5, y + 0.5], s = X(b); if (s[0] < 0 || s[0] > W || s[1] < 0 || s[1] > H) continue;
  if (!G.free(b)) continue;
  const v = seen(toPlan(b), 0.18, B).pastArchitecture;
  if (v < 0.5) add(`<path d="${path([X([x, y]), X([x + 1, y]), X([x + 1, y + 1]), X([x, y + 1])])}" fill="#8c7fa8" fill-opacity="0.28" stroke="none"/>`);
}
for (const p of polys) add(`<path d="${path(p.map(Xp))}" fill="#c9c1b4" stroke="#8a8174" stroke-width="1"/>`);
for (const b of civ.bays) add(`<path d="${path(b.poly.map(Xp))}" fill="#d9d3e6" stroke="#7d7598" stroke-width="1" stroke-dasharray="4 3"/>`);
for (const s of civ.structures) add(`<path d="${path(s.poly.map(Xp))}" fill="${s.z > 0 || /canopy/.test(s.category) ? 'none' : '#9a9184'}" stroke="#5f5a52" stroke-width="1.2" stroke-dasharray="${/canopy/.test(s.category) ? '6 3' : ''}"/>`);
for (const s of civ.solids ?? []) add(`<path d="${path(s.map(Xp))}" fill="#7f9c84" stroke="none" fill-opacity="0.8"/>`);
for (const k of keep.filter(k => k.polygon)) add(`<path d="${path(k.polygon.map(X))}" fill="none" stroke="${k.id === 'crew-line-getaway' ? '#2e7d5b' : '#b0a596'}" stroke-width="${k.id === 'crew-line-getaway' ? 1.6 : 1}" stroke-dasharray="2 3"/>`);
for (const f of fans) add(`<path d="${path(f.poly.map(X))}" fill="#d0473a" fill-opacity="0.16" stroke="#d0473a" stroke-width="1"/>`);
for (const q of places) { const s = X(q.b); add(`<circle cx="${s[0]}" cy="${s[1]}" r="2.6" fill="#6d6458"/>`); }
// the opening view: the screen's ground footprint (z = pavement) and the HUD on it
const fwdM = 1 / Math.sin((PLAY.pitch * Math.PI) / 180), k = SCREEN[1] / (2 * PLAY.zoom);
const ground = (sx: number, sy: number): V => { const a = (sx - SCREEN[0] / 2) / k, bb = ((SCREEN[1] / 2 - sy) / k - 0.18 * Math.cos((PLAY.pitch * Math.PI) / 180)) * fwdM, p: V = [C[0] + a * R[0] + bb * F[0], C[1] + a * R[1] + bb * F[1]]; return Xp(p); };
const rect = (r: [number, number, number, number]) => [ground(r[0], r[1]), ground(r[2], r[1]), ground(r[2], r[3]), ground(r[0], r[3])];
for (const h of HUD) add(`<path d="${path(rect(h.r))}" fill="#21544a" fill-opacity="0.13" stroke="#21544a" stroke-width="1" stroke-dasharray="3 3"/>`);
add(`<path d="${path(rect([0, 0, SCREEN[0], SCREEN[1]]))}" fill="none" stroke="#1d6fb8" stroke-width="3"/>`);
{ const c = ground(SCREEN[0] / 2, SCREEN[1] / 2); add(`<path d="M${c[0] - 9},${c[1]} L${c[0] + 9},${c[1]} M${c[0]},${c[1] - 9} L${c[0]},${c[1] + 9}" stroke="#1d6fb8" stroke-width="2"/>`); add(`<text x="${c[0] + 8}" y="${c[1] + 18}" font-size="13" fill="#1d6fb8">focus (${FOCUS.join(', ')})</text>`); }
{ const a = ground(HUD[0]!.r[0], HUD[0]!.r[1]); add(`<text x="${a[0] + 6}" y="${a[1] + 16}" font-size="12" fill="#21544a">briefing card (until Begin)</text>`); }
{ const a = ground(0, 0); add(`<text x="${a[0] + 6}" y="${a[1] - 8}" font-size="14" fill="#1d6fb8">opening view, zoom 14 · 38° · yaw 30 · 1726 × 1142</text>`); }
// goals and walks
const lab = (b: V, t: string, col: string, dx = 8, dy = -8) => { const s = X(b); add(`<text x="${s[0] + dx}" y="${s[1] + dy}" font-size="13" fill="${col}">${esc(t)}</text>`); };
for (const [n, at] of [['Café door (Inez)', dest(mission, 'Café door')], ['beside Inez', pt('beside-inez')], ['Getaway bay', dest(mission, 'Getaway bay')], ['cabinet', pt('cabinet')], ['Office approach', dest(mission, 'Office approach')]] as [string, V][]) {
  const s = X(at); add(`<rect x="${s[0] - 4}" y="${s[1] - 4}" width="8" height="8" fill="#1f1f1f"/>`); lab(at, n, '#1f1f1f');
}
for (let i = 0; i < 4; i++) { const a = X(head.crew[i].at), b = X(mission.crew[i].at); add(`<path d="M${a[0]},${a[1]} L${b[0]},${b[1]}" stroke="#555" stroke-width="1" stroke-dasharray="2 2"/>`); }
const col: Record<string, string> = { Scout: '#2a7fbf', Fixer: '#b5651d', Tech: '#2e8b57', Enforcer: '#7a3b8f' };
for (const c of head.crew) { const s = X(c.at); add(`<circle cx="${s[0]}" cy="${s[1]}" r="7" fill="#fff" stroke="${col[c.name]}" stroke-width="2.4"/>`); }
for (const c of mission.crew) { const s = X(c.at); add(`<circle cx="${s[0]}" cy="${s[1]}" r="9" fill="${col[c.name]}" stroke="#fff" stroke-width="2"/><text x="${s[0]}" y="${s[1] + 4.5}" font-size="12" font-weight="bold" fill="#fff" text-anchor="middle">${c.name[0]}</text>`); }
{ const a = X(head.crew[0].at); add(`<text x="${a[0] - 14}" y="${a[1] + 26}" font-size="13" fill="#444" text-anchor="end">old start: a 1.2 m line, 6 % seen (behind rocket-west/19)</text>`); }
// north arrow
{ const n0: V = [W - 70, 110], up = [-(0 * R[0] + 1 * R[1]), (0 * F[0] + 1 * F[1])]; const e: V = [n0[0] + 40 * (0 * R[0] + 1 * R[1]), n0[1] - 40 * (0 * F[0] + 1 * F[1])]; void up; add(`<path d="M${n0[0]},${n0[1]} L${e[0]},${e[1]}" stroke="#333" stroke-width="2.5"/><text x="${e[0] - 4}" y="${e[1] - 6}" font-size="14">N</text>`); }
// legend and table
const L0 = 18; let ly = 30;
add(`<text x="${L0}" y="${ly}" font-size="20" font-weight="bold">The Handover · the cell's start, old and new (Waymark, brief 014)</text>`); ly += 22;
add(`<text x="${L0}" y="${ly}" font-size="13" fill="#444">Top down, turned so the camera's screen-up is up the page. Hollow = old crew, filled = new (S Scout, F Fixer, T Tech, E Enforcer). Blue frame = what the screen shows at 0:00.</text>`); ly += 18;
add(`<text x="${L0}" y="${ly}" font-size="13" fill="#444">Lilac = ground where a standing figure is under 50 % seen past architecture (V-1). Red = watchers' fans at 0:00 (engine). Dots = people's places (P-16). Green dotted = keep-clear zone crew-line-getaway.</text>`); ly = H - 20 - 18 * 8;
add(`<rect x="${L0 - 6}" y="${ly - 16}" width="900" height="${18 * 9 + 6}" fill="#fff" fill-opacity="0.85"/>`);
add(`<text x="${L0}" y="${ly}" font-size="13" font-weight="bold">who · start (block m) · seen past architecture · nearest fan at 0:00 · walk to · screen px at the opening</text>`); ly += 18;
for (const r of [...was, ...now]) { add(`<text x="${L0}" y="${ly}" font-size="12.5" fill="${r.start === 'new' ? '#111' : '#777'}">${r.start} ${r.who} · (${r.at.join(', ')}) · ${Math.round(r.seen * 100)} % · ${r.fanM} m · ${esc(r.to)} ${r.walkM} m · ${r.inFrame ? `(${r.screen.join(', ')})${r.underHud.length ? ' under ' + r.underHud.join(', ') : ''}` : 'off screen'}</text>`); ly += 18; }
add('</svg>');
mkdirSync(resolve(ROOT, 'out/waymark/start'), { recursive: true });
const svgFile = resolve(ROOT, 'out/waymark/start/waymark-start.svg');
writeFileSync(svgFile, svg.join('\n'));
execFileSync('rsvg-convert', ['-o', OUT, svgFile]);
console.log(`→ ${OUT.replace(ROOT + '/', '')}`);
