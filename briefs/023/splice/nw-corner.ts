// Picture of the block's north-west corner for Sky (brief 023): the walk grid with Starline's pavements walkable (a built plot blocks only behind its building line, tools/plan/walk.ts),
// and where the call boxes, police posts and the robot dock stand now. The 14 samples (block -37..-35, 11..15) are drawn green when a call box or a door is within 40 m (walk) of them.
// usage: bun briefs/023/splice/nw-corner.ts   (reads out/world/harbour-district.world.json, writes briefs/023/splice/nw-corner.svg and .png via rsvg-convert)
import { readFileSync, writeFileSync } from 'node:fs';
import { execFileSync } from 'node:child_process';
import { derive } from '../../../tools/plan/plan';
import { civic } from '../../../tools/plan/civic';
import { buildingLines } from '../../../tools/plan/walk';
import { reachField, toBlockFn, walkGrid } from '../../../tools/plan/walk-grid';
const json = (p: string) => JSON.parse(readFileSync(p, 'utf8'));
const plan = json('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, json(plan.civic.block)), w = json('out/world/harbour-district.world.json');
const grid = walkGrid(plan, d, civ), tb = toBlockFn(plan.civic), S = 14, X0 = -46, X1 = 14, Y0 = -41, Y1 = 20;
const X = (x: number) => (x - X0) * S, Y = (y: number) => (Y1 - y) * S;
const inWin = (p: number[]) => p[0]! >= X0 && p[0]! <= X1 && p[1]! >= Y0 && p[1]! <= Y1;
let s = `<svg xmlns="http://www.w3.org/2000/svg" width="${(X1 - X0) * S}" height="${(Y1 - Y0) * S}" font-family="Helvetica" font-size="13"><rect width="100%" height="100%" fill="#f4f0e6"/>`;
for (const r of d.roads) { const pts = r.line.map((p: any) => tb(p)); s += `<polyline points="${pts.map((p: any) => `${X(p[0])},${Y(p[1])}`).join(' ')}" fill="none" stroke="#f3c79a" stroke-width="${r.w * S}" opacity=".45"/>`; }
// walkable ground that was built plot before (the pavement between the kerb and the building line): green
const oldBlocked = (c: number[]) => d.plots.some(p => !['park', 'open', 'square', 'civic'].includes(p.use) && inPolyP(c, p.polyV));
function inPolyP(q: number[], poly: number[][]) { let o = false; for (let i = 0, j = poly.length - 1; i < poly.length; j = i++) { const a = poly[i]!, b = poly[j]!; if ((a[1]! > q[1]!) !== (b[1]! > q[1]!) && q[0]! < ((b[0]! - a[0]!) * (q[1]! - a[1]!)) / (b[1]! - a[1]!) + a[0]!) o = !o; } return o; }
const toPlan = (b: number[]) => { const o = plan.civic.blockToPlan.origin, ax = plan.civic.blockToPlan.x, ay = plan.civic.blockToPlan.y; return [o[0] + b[0] * ax[0] + b[1] * ay[0], o[1] + b[0] * ax[1] + b[1] * ay[1]]; };
for (let i = 0; i < grid.nx; i++) for (let j = 0; j < grid.ny; j++) { const c = grid.centre(i, j); if (!inWin(c)) continue; const blocked = grid.rows[i]![j] === '#', fill = blocked ? '#b9b4a8' : oldBlocked(toPlan(c)) ? '#bfe3c4' : ''; if (fill) s += `<rect x="${X(c[0] - .25)}" y="${Y(c[1] + .25)}" width="${.5 * S + .5}" height="${.5 * S + .5}" fill="${fill}"/>`; }
void buildingLines;
const kind = (it: any) => /call-box/.test(it.id) || it.kind === 'phone-booth' ? 'box' : /police-post/.test(it.id) ? 'post' : it.kind === 'civic-robot-dock' ? 'dock' : '';
const colour: Record<string, string> = { box: '#8a2be2', post: '#1c5fd6', dock: '#1a9a4a' }, label: Record<string, string> = { box: 'call box', post: 'police post', dock: 'robot dock' };
const srcs: [number, number][] = [...w.takedown.report.map((r: any) => tb(r.stand))];
const field = reachField(grid, srcs as any);
for (const x of [-37, -35]) for (const y of [11, 13, 15]) { const [i, j] = grid.cellOf([x, y]); const m = field[i * grid.ny + j]! / 100; s += `<circle cx="${X(x)}" cy="${Y(y)}" r="4" fill="${m <= 40 ? '#1a9a4a' : '#d22'}"/>`; }
for (const it of w.street) { const k = kind(it); if (!k) continue; const p = tb(it.at); if (!inWin(p)) continue; s += `<rect x="${X(p[0]) - 7}" y="${Y(p[1]) - 7}" width="14" height="14" fill="${colour[k]}" stroke="#fff" stroke-width="2"/><text x="${X(p[0]) + 12}" y="${Y(p[1]) + 4}" fill="${colour[k]}" font-weight="bold">${label[k]} (${p.map((v: number) => +v.toFixed(0)).join(', ')})</text>`; }
for (let x = Math.ceil(X0 / 5) * 5; x < X1; x += 5) s += `<text x="${X(x)}" y="12" fill="#555" font-size="11">${x}</text>`;
for (let y = Math.ceil(Y0 / 5) * 5; y < Y1; y += 5) s += `<text x="2" y="${Y(y)}" fill="#555" font-size="11">${y}</text>`;
s += `<text x="${X(-45)}" y="${Y(17.5)}" fill="#1a9a4a" font-weight="bold">the 14 samples (block -37..-35, 11..15): within 40 m of a call box or door, green = yes</text>`;
s += `<text x="${X(-45)}" y="${Y(-38)}" font-size="12" fill="#333">block frame, m. Orange: roads (Starline Street north-south at x -32; Garden Row east from it). Grey: built ground behind the building line.</text><text x="${X(-45)}" y="${Y(-39.5)}" font-size="12" fill="#333">Pale green: the pavements (6.5 m, kerb to building line) that were counted as built plot and now walk. Squares: what stands there.</text></svg>`;
writeFileSync('briefs/023/splice/nw-corner.svg', s);
execFileSync('rsvg-convert', ['-o', 'briefs/023/splice/nw-corner.png', 'briefs/023/splice/nw-corner.svg']);
