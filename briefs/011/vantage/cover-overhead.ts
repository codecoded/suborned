// Brief 011 · Vantage · cover on the square: an overhead of the Handover's fight ground (block frame drawn north up: east right, south down), with the
// cover-capable objects, the keep-clear zones, the walk grid's blocked cells, the cover points (a tick toward what they shield from; green when they
// shield from the getaway car), the fight posts and the car. Reads the committed block, site and cover file.
// Run (repo root): bun briefs/011/vantage/cover-overhead.ts <out.png> [title] [block.json] [cover.json]   (needs rsvg-convert; the block and cover default to the committed pair)
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { execFileSync } from 'node:child_process';
import { resolve } from 'node:path';
import { gridOf, objectsOf } from '../../../tools/cover/gen';
import { protects } from '../../../tools/cover/select';

const ROOT = resolve(import.meta.dir, '../../..'), J = (p: string) => JSON.parse(readFileSync(resolve(ROOT, p), 'utf8'));
const out = resolve(ROOT, process.argv[2] ?? 'out/vantage/cover.png'), title = process.argv[3] ?? 'cover on the square';
const plan = J('contracts/world/harbour-district.plan.json'), block = J(process.argv[4] ?? plan.civic.block), site = J('sim/data/handover.site.json'), cover = J(process.argv[5] ?? 'contracts/world/civic-block.cover.json');
const objs = objectsOf(plan, block, site), grid = gridOf(site);
const X0 = -64, X1 = 4, Y0 = -30, Y1 = 46, K = 22; // block y (east) range → screen x; block x (south) range → screen y; px per m
const sx = (by: number) => (by - X0) * K, sy = (bx: number) => (bx - Y0) * K + 60;
const W = (X1 - X0) * K, H = (Y1 - Y0) * K + 130;
const pts = (poly: number[][]) => poly.map(p => `${sx(p[1]!).toFixed(1)},${sy(p[0]!).toFixed(1)}`).join(' ');
const at = (id: string): [number, number] => { const p = site.places[id], a = p.stand ?? p.at; return [a[0], a[1]]; };
const car = at(site.getaway.car), s: string[] = [];
s.push(`<rect width="${W}" height="${H}" fill="#f4efe6"/>`);
// roads: Rocket Lane (block y -62..-50), Orbit Walk (x 31..44), Starline Street (x < -26)
for (const r of [[[-30, -62], [46, -62], [46, -50], [-30, -50]], [[31, -50], [44, -50], [44, 4], [31, 4]], [[-30, -50], [-26, -50], [-26, 4], [-30, 4]]]) s.push(`<polygon points="${pts(r)}" fill="#cfc9d6"/>`);
// the walk grid's blocked cells
for (let i = 0; i < grid.nx; i++) for (let j = 0; j < grid.ny; j++) if (!grid.free(i, j)) { const c = grid.centre(i, j); if (c[0] > Y0 && c[0] < Y1 && c[1] > X0 && c[1] < X1) s.push(`<rect x="${sx(c[1]) - K / 4}" y="${sy(c[0]) - K / 4}" width="${K / 2}" height="${K / 2}" fill="#d9d2c6"/>`); }
for (const b of block.buildings) s.push(`<polygon points="${pts(b.polygon)}" fill="#e3ddd2" stroke="#8a8072" stroke-width="2"/>`);
for (const o of block.objects) s.push(`<polygon points="${pts(o.polygon)}" fill="${o.height >= 0.9 ? '#a9c79a' : '#cfe0c2'}" stroke="#6f8f63" stroke-width="1"/>`);
for (const z of block.dressing.keepClear) if (z.polygon) s.push(`<polygon points="${pts(z.polygon)}" fill="none" stroke="#b9895a" stroke-width="1.5" stroke-dasharray="6 4"/><text x="${sx(z.polygon[0][1]) + 4}" y="${sy(z.polygon[0][0]) + 14}" font-size="12" fill="#9a6e45" font-family="Helvetica">${z.id}</text>`);
else s.push(`<circle cx="${sx(z.ring.centre[1])}" cy="${sy(z.ring.centre[0])}" r="${z.ring.r1 * K}" fill="none" stroke="#b9895a" stroke-width="1.5" stroke-dasharray="6 4"/><circle cx="${sx(z.ring.centre[1])}" cy="${sy(z.ring.centre[0])}" r="${z.ring.r0 * K}" fill="none" stroke="#b9895a" stroke-width="1" stroke-dasharray="3 4"/>`);
for (const o of objs) s.push(`<polygon points="${pts(o.poly)}" fill="${o.kind === 'car' ? '#9fb3c8' : /proposal|run-sq/.test(o.id) ? '#4f8a3c' : '#7fa86d'}" stroke="#2f4f2a" stroke-width="${/run-sq/.test(o.id) ? 3 : 1.5}"/>`);
for (const c of cover.points) {
  const x = sx(c.at[1] / 100), y = sy(c.at[0] / 100), good = c.of !== 'car/getaway' && protects(c, car);
  s.push(`<line x1="${x}" y1="${y}" x2="${x + (c.faces[1] / 1000) * 14}" y2="${y + (c.faces[0] / 1000) * 14}" stroke="${good ? '#1f8f5f' : '#888'}" stroke-width="2.5"/><circle cx="${x}" cy="${y}" r="3.5" fill="${good ? '#1f8f5f' : '#999'}"/>`);
}
const post = (id: string, colour: string, label: string) => { const [a, b] = at(id); const x = sx(b / 100), y = sy(a / 100); s.push(`<circle cx="${x}" cy="${y}" r="9" fill="${colour}" stroke="white" stroke-width="2.5"/><text x="${x + 12}" y="${y + 5}" font-size="15" font-weight="700" font-family="Helvetica" fill="#1d2433" stroke="white" stroke-width="3" paint-order="stroke">${label}</text>`); };
for (const a of cover.areas) for (const h of a.hostileCover) {
  const [pa, pb] = at(h.post);
  for (const id of h.points.slice(0, 1)) { const c = cover.points.find((q: any) => q.id === id); s.push(`<line x1="${sx(pb / 100)}" y1="${sy(pa / 100)}" x2="${sx(c.at[1] / 100)}" y2="${sy(c.at[0] / 100)}" stroke="#1f8f5f" stroke-width="2" stroke-dasharray="4 3"/>`); }
  post(h.post, a.id.endsWith('safe') ? '#7a4fc2' : '#2f5fb3', `${h.post} ${h.points.length ? '✓' : '✗'}`);
}
post(site.fight.guard, '#c98a1a', 'guard-ally');
const [ca, cb] = car; s.push(`<text x="${sx(cb / 100) + 10}" y="${sy(ca / 100) - 14}" font-size="15" font-weight="700" font-family="Helvetica" fill="#1d2433">getaway car</text>`);
const cv2 = cover.areas.map((a: any) => `${a.id}: hostile ${a.hostileCover.filter((h: any) => h.points.length).length}/${a.hostile.length}, cell ${a.cellCover.filter((h: any) => h.points.length).length}/${a.hostile.length}`).join(' · ');
s.push(`<text x="16" y="34" font-size="24" font-weight="700" font-family="Helvetica" fill="#1d2433">${title}</text><text x="16" y="${H - 70}" font-size="17" font-family="Helvetica" fill="#1d2433">${cv2}</text>`);
s.push(`<text x="16" y="${H - 44}" font-size="15" font-family="Helvetica" fill="#444">north up · ${K} px/m · green tick: a cover point shielding from the getaway car (grey: other ways) · violet: ambush posts · blue: police posts</text>`);
s.push(`<text x="16" y="${H - 20}" font-size="15" font-family="Helvetica" fill="#444">dashed brown: keep-clear zones · grey cells: blocked walk grid · Rocket Lane left (west), Orbit Walk bottom (south), the café top · new runs outlined dark</text>`);
const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${W}" height="${H}">${s.join('\n')}</svg>`;
mkdirSync(resolve(ROOT, 'out/vantage'), { recursive: true }); const tmp = resolve(ROOT, 'out/vantage/cover-overhead.svg'); writeFileSync(tmp, svg);
execFileSync('rsvg-convert', ['-w', '1500', '-o', out, tmp]);
console.log(out);
