// Brief 023, Datum: Starline Street north of Garden Row (block frame) already has its 6.5 m access pavements in the plan and the world; the walk grid counts them as built ground.
// Same window and frame as Splice's nw-corner.ts, with the world's building footprints and the pavement's street furniture drawn over the walk grid.
// usage: bun briefs/023/datum/starline-pavement.ts   (reads the plan and out/world/harbour-district.world.json; writes starline-pavement.svg and .png beside this file)
import { readFileSync, writeFileSync } from 'node:fs';
import { execFileSync } from 'node:child_process';
import { derive } from '../../../tools/plan/plan';
import { civic } from '../../../tools/plan/civic';
import { toBlockFn, walkGrid } from '../../../tools/plan/walk-grid';
const json = (p: string) => JSON.parse(readFileSync(p, 'utf8'));
const plan = json('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, json(plan.civic.block)), w = json('out/world/harbour-district.world.json');
const grid = walkGrid(plan, d, civ), tb = toBlockFn(plan.civic), S = 14, X0 = -46, X1 = 14, Y0 = -34, Y1 = 20;
const X = (x: number) => (x - X0) * S, Y = (y: number) => (Y1 - y) * S;
const inWin = (p: number[]) => p[0]! >= X0 && p[0]! <= X1 && p[1]! >= Y0 && p[1]! <= Y1;
const pts = (ring: number[][]) => ring.map((p) => tb([p[0]!, p[1]!])).map((p) => `${X(p[0])},${Y(p[1])}`).join(' ');
let s = `<svg xmlns="http://www.w3.org/2000/svg" width="${(X1 - X0) * S}" height="${(Y1 - Y0) * S}" font-family="Helvetica" font-size="13"><rect width="100%" height="100%" fill="#f4f0e6"/>`;
s += `<defs><clipPath id="win"><rect width="${(X1 - X0) * S}" height="${(Y1 - Y0) * S}"/></clipPath></defs><g clip-path="url(#win)">`;
for (let i = 0; i < grid.nx; i++) for (let j = 0; j < grid.ny; j++) if (grid.rows[i]![j] === '#') { const c = grid.centre(i, j); if (!inWin(c)) continue; s += `<rect x="${X(c[0] - .25)}" y="${Y(c[1] + .25)}" width="${.5 * S + .5}" height="${.5 * S + .5}" fill="#cfcac0"/>`; }
for (const r of d.roads) s += `<polyline points="${r.line.map((p: any) => tb(p)).map((p: any) => `${X(p[0])},${Y(p[1])}`).join(' ')}" fill="none" stroke="#f3c79a" stroke-width="${r.w * S}" opacity=".55"/>`;
// the access pavements, plan frame: Starline Street's north side (lamplight-south-e), its south side east of Garden Row and Garden Row's east side (garden-row-east): kerb face (road edge, y -192 / -204) to building line (6.5 m in, B-3)
for (const [x0, x1, y0, y1] of [[-156, -60, -192, -185.5], [-126, -60, -210.5, -204], [-126, -119.5, -261, -204]]) s += `<polygon points="${pts([[x0, y0], [x1, y0], [x1, y1], [x0, y1]])}" fill="#3a9a5a" opacity=".28" stroke="#2a7a44" stroke-width="2"/>`;
for (const b of w.buildings) if (b.ring?.some((p: number[]) => inWin(tb([p[0]!, p[1]!])))) s += `<polygon points="${pts(b.ring)}" fill="#7d7468" stroke="#3b342c" stroke-width="1.5"/>`;
const kinds = new Set(['tree', 'lamp', 'bench', 'bin', 'hydrant', 'bollard']), col: Record<string, string> = { tree: '#2a7a44', lamp: '#b8860b', bench: '#6b4f2a', bin: '#555', hydrant: '#c0392b', bollard: '#444' };
for (const it of w.street) { if (!kinds.has(it.kind)) continue; const p = tb(it.at); if (!inWin(p)) continue; s += `<circle cx="${X(p[0])}" cy="${Y(p[1])}" r="${it.kind === 'tree' ? 6 : 4}" fill="${col[it.kind]}" stroke="#fff" stroke-width="1.5"/>`; }
s += `</g>`;
for (const x of [-37, -35]) for (const y of [11, 13, 15]) s += `<circle cx="${X(x)}" cy="${Y(y)}" r="4" fill="#d22"/>`;
s += `<rect x="0" y="0" width="${(X1 - X0) * S}" height="${3.6 * S}" fill="#f4f0e6" opacity=".9"/><text x="${X(-45)}" y="${Y(17.2)}" fill="#d22" font-weight="bold">the 14 samples (block -37..-35, 11..15) stand on the carriageway</text>`;
s += `<text x="${X(-45.6)}" y="${Y(8)}" fill="#1f5e33" font-weight="bold" font-size="12" transform="rotate(-90 ${X(-45.6)} ${Y(8)})">pavement 6.5 m</text>`;
s += `<text x="${X(-23.4)}" y="${Y(8)}" fill="#1f5e33" font-weight="bold" font-size="12" transform="rotate(-90 ${X(-23.4)} ${Y(8)})">pavement 6.5 m</text>`;
for (let x = Math.ceil(X0 / 5) * 5; x < X1; x += 5) s += `<text x="${X(x)}" y="12" fill="#555" font-size="11">${x}</text>`;
for (let y = Math.ceil(Y0 / 5) * 5; y < Y1; y += 5) s += `<text x="2" y="${Y(y)}" fill="#555" font-size="11">${y}</text>`;
s += `<rect x="0" y="${Y(-29.6)}" width="${(X1 - X0) * S}" height="${4.4 * S}" fill="#f4f0e6" opacity=".9"/>`;
s += `<text x="${X(-45)}" y="${Y(-30.8)}" font-size="12" fill="#333">Block frame, m (Splice's nw-corner view). Orange: carriageways. Dark: the world's buildings. Grey: built for the walk grid.</text>`;
s += `<text x="${X(-45)}" y="${Y(-32)}" font-size="12" fill="#333">Green: the access pavements, kerb 1.5 + walk 2 + frontage 3 = 6.5 m, kerb to building line, with trees, lamps, bins.</text>`;
s += `<text x="${X(-45)}" y="${Y(-33.2)}" font-size="12" fill="#333">The plan and the world have the footway; the walk grid blocks a built plot to its edge, pavement and all.</text></svg>`;
writeFileSync('briefs/023/datum/starline-pavement.svg', s);
execFileSync('rsvg-convert', ['-o', 'briefs/023/datum/starline-pavement.png', 'briefs/023/datum/starline-pavement.svg']);
