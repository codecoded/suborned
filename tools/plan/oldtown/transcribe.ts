// Brief 075, the remap (Sky 2026-10-28: "why can't we just remap the world around d handover using old buildings" · "And old props. So the sky trak and other things
// all planned properly but looking great"): the district plan as the old DecoKit town, transcribed into suborned.plan/1, with the Handover zone fixed inside it.
//
//   bun tools/plan/oldtown/transcribe.ts [--base contracts/world/harbour-district.plan.json] [--out contracts/world/harbour-district.oldtown.plan.json]
//
// The frame. The old town's x is the plan's x (the Boulevard on x 0, as today) and its z less 206 is the plan's y, so Crown Circus, old (0, 206), is the plan's origin
// (frame.origin), and every old coordinate and dimension lands unscaled. The Handover zone keeps its plan coordinates byte for byte (civic.blockToPlan, the civic plots,
// its streets and nodes, its gates): relative to the old town it stands 34 m west and 54 m north of where the old world sited it (site-2, block origin old (-96, -78)),
// because Satellite Street (x -60) already meets the Boulevard's west wall at the old grammar's 8 m pavement, and Starline Street (y -198) runs into the old west lane.
//
// What is read and what is decided. Read from the old sources (tools/plan/oldtown/old.ts): the street section (kerb 8, facade 18), the block depth 28, the walls with their
// segments, caps and storey classes, the short walls, the four crescents, the lanes, Market Street's kerbs, the circus radii, the harbour terrace, steps, promenade and quay
// lines, the piers, Orbit Gardens (canal, pool, pavilion), the Southside blocks, every landmark Put and the sky rail control points. Decided here (the manual part, each
// listed in MANUAL below with its reason): where the old town is trimmed to clear the Handover zone, the road classes and the seams, the roads the plan needs that the old
// town had as pavements, the tram line and the sky rail as the plan's rules allow them, and the old pieces the plan has no room for.
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { readOld, sources, OLD_DIR, type OldWall, type Seg } from './old';
import { derive, type Plan } from '../plan';
import { civic } from '../civic';
import { ctxOf } from '../../world/gen/common';
import { buildingLine, plotRing, kindOf } from '../../world/gen/edges';

const ROOT = resolve(import.meta.dir, '../../..');
const args = process.argv.slice(2), opt = (k: string, d: string) => (args.includes(k) ? args[args.indexOf(k) + 1]! : d);
const BASE = opt('--base', 'contracts/world/harbour-district.plan.json'), OUT = opt('--out', 'contracts/world/harbour-district.oldtown.plan.json');
const live: any = JSON.parse(readFileSync(resolve(ROOT, BASE), 'utf8'));
const old = readOld(), S = old.S;
type V = [number, number];
const DZ = S['Circus.z']!; // 206
const P = (x: number, z: number): V => [r2(x), r2(z - DZ)];
const r2 = (v: number) => Math.round(v * 100) / 100;
const clone = <T>(x: T): T => JSON.parse(JSON.stringify(x));
export const MANUAL: string[] = [];
const manual = (s: string) => MANUAL.push(s);

// ------------------------------------------------------------------ the old town's lines in the plan frame
const fx = S.FrontX!, kx = S.KerbX!, eb = S.EastBack!, wb = S.WestBack!; // 18, 8, 46, -46
const circus = { c: P(S['Circus.x']!, DZ), island: S.IslandR!, ring: S.RingR!, crescent: S.CrescentR! };
const yOf = (z: number) => r2(z - DZ);
const lanes = { east: [yOf(S['EastLane.x']!), yOf(S['EastLane.y']!)] as V, west: [yOf(S['WestLane.x']!), yOf(S['WestLane.y']!)] as V };
const market = [yOf(S['SideZ.x']!), yOf(S['SideZ.y']!)] as V; // Market Street's kerb lines, -146 / -130
const sunset = [yOf(S['Z.x']! - 12), yOf(S['Z.x']!)] as V; // Sunset Way: the boulevard blocks' south end kerb (-40) and Southside's N1/N2 head walks (-52): -258 / -246
const harbour = { terrace: [eb, S.StepsX!] as V, promenade: [S.PromenadeX!, S.QuayX!] as V, quay: S.QuayX!, z: [yOf(S['HarbourZ.x']!), yOf(S['HarbourZ.y']!)] as V };
const piers = { market: [yOf(S['MarketPierZ.x']!), yOf(S['MarketPierZ.y']!)] as V, skyport: [yOf(S['SkyportPierZ.x']!), yOf(S['SkyportPierZ.y']!)] as V };
const north = yOf(S.NorthFacade!); // 140
const gardensX = [S['GardensX.x']!, S['GardensX.y']!] as V; // -110, -46

// The plan's street section on the Boulevard is sections.tramStreet.district: 20 kerb to kerb, 8 m pavements, so the walls stand at x ±18 exactly where the old facades stood
// (old: kerb 8, 10 m promenade). The Handover's Satellite Street (x -60, w 12) puts its east kerb at -54: the old west wall's back (-46) is 8 m inside it, on the line.
const BLV = { w: live.roads.find((r: any) => r.id === 'boulevard').w as number }; // 20
const KERB = BLV.w / 2; // 10
if (KERB - kx !== 2) manual(`The Boulevard is 20 m kerb to kerb (sections.tramStreet.district, Sky's tram street), 4 m wider than the old 16; the old facades (x ±${fx}) stand where they stood, so the pavements are 8 m (old 10)`);

// ------------------------------------------------------------------ the Handover zone, kept byte for byte
const KEEP_ROADS = ['rocket-lane', 'starline-street', 'garden-row', 'orbit-walk', 'pylon-row', 'apron-road', 'satellite-street', 'lantern-lane'];
const KEEP_PLOTS = ['records-square', 'records-office', 'garden-row-east', 'pylon-row-east', 'orbit-gardens', 'rocket-west', 'lamplight-south-w', 'lamplight-south-e', 'lamplight-west'];
const KEEP_GATES = ['rocket-gate', 'satellite-gate', 'lantern-west', 'lantern-east'];
const byId = (xs: any[], id: string) => { const x = xs.find(y => y.id === id); if (!x) throw new Error(`base plan has no ${id}`); return clone(x); };

// ------------------------------------------------------------------ nodes and roads
const nodes: Record<string, V> = {};
for (const r of KEEP_ROADS) for (const n of byId(live.roads, r).path) nodes[n] = clone(live.nodes[n]);
const node = (id: string, p: V) => { if (nodes[id] && (nodes[id]![0] !== p[0] || nodes[id]![1] !== p[1])) throw new Error(`node ${id} moved`); nodes[id] = p; return id; };
// the Lantern loop's south arm: the old Southside's Apollo Way line, south of Market Street
nodes.e_sw = [-186, -490]; nodes.e_se = [-60, -490];
manual('Lantern Lane keeps its path (g_rocket, e_sw, e_se, g_satellite); its south arm moves from y -600 to -490, the sheet\'s south edge being the old Southside\'s (-506)');

const sheetY: V = [-560, 300], sheetX: V = [-460, 380];
const SK = 170; // the Skyport Tower's centre on the pier axis (old Skyport x 170)
const MALL_Y = 320; // Crown Mall, behind the SpaceHQ: S1's east-west stretch over it
const RING_OUT = 29;
node('off_southside', [-340, sheetY[0]]); node('w_market', [-340, -410]); node('w_circus', [-340, 0]); node('nw', [-340, 280]); node('off_north', [-340, sheetY[1]]); node('off_west', [sheetX[0], 280]);
node('off_circus_west', [sheetX[0], 0]); node('m_boulevard', [0, -410]);
const ring = (circus.island + RING_OUT) / 2, ringW = RING_OUT - circus.island;
manual(`Crown Circus: island r ${circus.island}, the ring road ${circus.island}–${RING_OUT} (old ${circus.island}–${circus.ring}: 3 m wider so the tram's terminal loop runs on R 25 with its swept envelope, ±1.825, inside the carriageway, and its transitions clear the boulevard mouth's kerb returns), the walk ring to the crescents at r ${circus.crescent} (old)`);
node('c_s', [0, -ring]); node('c_w', [-ring, 0]); node('c_n', [0, ring]);
node('b_sunset', [0, r2((sunset[0] + sunset[1]) / 2)]); node('b_market', [0, r2((market[0] + market[1]) / 2)]);
node('h_apollo', [0, -472]); node('off_seagates', [0, sheetY[0]]);
node('s_terrace', [eb, nodes.b_sunset![1]]); node('m_terrace', [eb, nodes.b_market![1]]); node('a_terrace', [eb, -472]);
node('n_avenue', [0, north - 6]);
node('c_end', [-156, 0]); node('g_lamp_w', clone(live.nodes.g_lamp_w)); node('l_sw', clone(live.nodes.l_sw)); node('l_se', clone(live.nodes.l_se));

const tram = (line: string, why: string) => ({ line, section: 'standards.streetscape.sections.tramStreet', runs: [{ centre: true }], why });
const roads: any[] = [
  { id: 'sunset-way', name: 'Sunset Way', class: 'primary', w: 24, path: ['off_southside', 'w_market', 'w_circus', 'nw', 'off_north'], tram: tram('T1', 'T1 runs on its two centre lanes from w_market north to nw (sections.tramStreet.primary)'), why: 'the west primary, as today (x -340): rocket-west and lamplight-south-w, both in the Handover zone, front it. The old town had procedural massing here (DecoKitCityMass)' },
  { id: 'crown-road', name: 'Crown Road', class: 'primary', w: 24, path: ['off_west', 'nw'], tram: tram('T1', 'T1 leaves the sheet west on it'), why: 'the stub west to the West End; east of nw the old town has Sphere Park and the SpaceHQ, so the old Crown Road across the north is gone' },
  { ...byId(live.roads, 'market-street'), name: 'Market Street', w: 18, median: undefined, path: ['w_market', 'g_rocket', 'g_satellite', 'm_boulevard'], tram: tram('T1', 'T1 runs west on it from the Boulevard to Sunset Way'),
    why: 'the Handover zone\'s distributor, on its line and width (y -410, w 18, its kerbs where they were): a tram street now, its 6 m median gone (a tram street has none, sections.tramStreet; the kerbside lane is 1.8 m, so loading only); east of the Boulevard the old town has Starline Green and SS2, so it ends at the Boulevard' },
  { id: 'boulevard', name: 'The Boulevard', class: 'district', w: BLV.w, path: ['off_seagates', 'h_apollo', 'm_boulevard', 'b_sunset', 'b_market', 'c_s'], tram: tram('T1', 'T1 on the centre lanes from Market Street to Crown Circus'),
    why: `the old boulevard on x 0 from Crown Circus (old z 176) to Sunset Way (old -40), then the old Comet Row and Satellite Street on the same axis to the sheet's south edge (the Sea Gates beyond); facades at x ±${fx} (old), pavements 8, sections.tramStreet.district` },
  { id: 'crown-circus', name: 'Crown Circus', class: 'district', w: ringW, gyratory: true, arc: circus.c, cw: true, path: ['c_s', 'c_w', 'c_n', 'c_s'], why: 'the old circus (DecoKitCircus): a ring road round the monument island, the four crescents on its walk ring. District, as today: Crown Avenue (access) is gated off it at c_n; the crescents\' plots front Crown Avenue, Satellite Street and Market Street' },
  { id: 'crown-avenue', name: 'Crown Avenue', class: 'access', w: BLV.w, path: ['c_n', 'n_avenue'], why: `the boulevard's section north of the circus to the forecourt (old z 236 to ${S.NorthFacade}): the arch spans it, the HQ closes it` },
  { id: 'sunset-east', name: 'Sunset Way', class: 'access', w: 12, path: ['b_sunset', 's_terrace'], why: 'old Sunset Way east of the boulevard, from the kerb to the harbour terrace (old kerb lines z -52 / -40). West of the boulevard it is the Sunset Walk (foot), so the Handover\'s access streets never join the boulevard\'s' },
  { id: 'harbour-market', name: 'Market Street', class: 'access', w: market[1] - market[0], path: ['b_market', 'm_terrace'], why: 'old Market Street: kerbs z 60 / 76, from the boulevard to the harbour terrace, where it ends on a kerb (old SideStreet); its vista is the Market Pier and the liner' },
  { id: 'apollo-way', name: 'Apollo Way', class: 'access', w: 12, path: ['h_apollo', 'a_terrace'], why: 'old Apollo Way (z -253, lane to terrace), 13 m south of its old line so the boulevard\'s junctions keep 60 m (Market Street at -410)' },
  { id: 'circus-road', name: 'Circus Road', class: 'district', w: 18, median: 6, path: ['off_circus_west', 'w_circus', 'g_lamp_w', 'c_end'], why: 'the west approach on the circus axis (y 0), as today, ending at the garden walk (x -151, its end on today\'s line): east of it the axis is the Gardens Gate walk to the old pool and the circus\'s west mouth (old: the gardens gate promenade). Its median would carry the S1 pylons (oldtown.skyrail.planned)' },
  { id: 'lamplight-lane', name: 'Lamplight Lane', class: 'access', w: 12, path: ['g_lamp_w', 'l_sw', 'l_se'], why: 'today\'s Lamplight Lane on its line (Circus Road at x -250, south to y -120, east to x -100; its east arm to Circus Road goes, the gardens are there): lamplight-south-w and -e (the Handover zone\'s walk window) face it on their north edges, so their buildings, the sim site\'s occluders and walk grid stay as recorded; the West Terraces\' walk and the garden terrace face it too' },
];
for (const id of KEEP_ROADS) if (!roads.some(r => r.id === id)) roads.push(byId(live.roads, id));
for (const r of roads) for (const k of Object.keys(r)) if (r[k] === undefined) delete r[k];
manual('The Handover zone\'s eight streets keep their ids, paths and widths (rocket-lane, starline-street, garden-row, orbit-walk, pylon-row, apron-road, satellite-street, lantern-lane); Market Street keeps its id, its line and the gates on it, and becomes a tram street (w 19, no median)');

const links = [
  { node: 'off_southside', to: 'Southside', via: 'Sunset Way' },
  { node: 'off_west', to: 'West End', via: 'Crown Road (and tram T1)' },
  { node: 'off_north', to: 'North Shore', via: 'Sunset Way' },
  { node: 'off_circus_west', to: 'West End', via: 'Circus Road (and sky rail S1)' },
  { node: 'off_seagates', to: 'Sea Gates', via: 'The Boulevard (old Satellite Street) south' },
];

const gates = [
  ...KEEP_GATES.map(id => byId(live.gates, id)),
  { id: 'avenue-gate', name: 'Crown Avenue', node: 'c_n', road: 'crown-avenue', area: 'crown' },
  { id: 'sunset-gate', name: 'Sunset', node: 'b_sunset', road: 'sunset-east', area: 'southside' },
  { id: 'market-gate', name: 'Market', node: 'b_market', road: 'harbour-market', area: 'boulevard' },
  { id: 'apollo-gate', name: 'Apollo', node: 'h_apollo', road: 'apollo-way', area: 'southside' },
  byId(live.gates, 'lamplight-west'),
];

// ------------------------------------------------------------------ areas
const records = byId(live.areas, 'records'), lamplight = byId(live.areas, 'lamplight');

const areas: any[] = [
  records, lamplight,
  { id: 'westend', name: 'West Terraces', rect: [sheetX[0], -157, -126, -9], label: [-250, -80] },
  { id: 'gardens', name: 'Orbit Gardens', rect: [-157, -54, -148, 50], label: [-82, -80] },
  { id: 'sphere', name: 'Sphere Park', rect: [sheetX[0], -54, 9, sheetY[1]], label: [-250, 220] },
  { id: 'boulevard', name: 'The Boulevard', rect: [-54, 48, -258, -6], label: [30, -200] },
  { id: 'crown', name: 'Crown Circus', rect: [-54, 48, -6, 204], label: [30, 100] },
  { id: 'southside', name: 'Southside', rect: [-54, 48, -548, -258], label: [30, -440] },
  { id: 'lantern', name: 'Lantern Quarter', rect: [-328, -54, -548, -419], label: [-260, -520] },
  { ...byId(live.areas, 'harbour'), rect: [48, sheetX[1], -548, 290], label: [150, -300], notch: undefined },
];
for (const a of areas) for (const k of Object.keys(a)) if (a[k] === undefined) delete a[k];

// ------------------------------------------------------------------ plots
const plots: any[] = KEEP_PLOTS.map(id => byId(live.plots, id));
const lsw = plots.find(p => p.id === 'lamplight-south-w'), lse = plots.find(p => p.id === 'lamplight-south-e');
void lsw; void lse;
manual('lamplight-south-w and lamplight-south-e (in the walk window) and lamplight-west stay as today with today\'s neighbours on every edge (Lamplight Lane at y -120, lamplight-east north of -e, boulevard-west south of it, the garden walk): their edge classes, buildings and building lines are byte-identical, so the sim site (grid and occluders) and the takedown places are unchanged and the Handover\'s replays stand. The old town gives way: the boulevard\'s west wall between Starline Street and y -126 is lamplight-south-e (today\'s massing), the old W2 starts at -126, the gardens at -114');
const notch: [number, number, number] = [circus.c[0], circus.c[1], circus.crescent];
const plot = (id: string, use: string, area: string, rect: number[], extra: any = {}) => plots.push({ id, use, area, rect, ...extra });
// the boulevard walls (old DecoKitCityBlocks): plots from the kerb (x ±10) to 8 m past the old back facade on the garden side (the gardens' edge) and 2 m on the terrace side
const W0 = -54, W1 = -KERB, E0 = KERB, E1 = eb + 2;
plot('boulevard-west', 'commercial', 'boulevard', [W0, W1, sunset[0], -192], { name: 'Boulevard west (old W1; the west lane a passage)' });
{ // old W2 north of lamplight-south-e: the wall plot (x -54..-10) to the gate, the notch's arc sampled, and a 12 m strip west to x -94 under the gardens (today's lamplight-east edge)
  const arc: V[] = []; for (let k = 0; k <= 16; k++) { const a = Math.atan2(-Math.sqrt(circus.crescent ** 2 - W1 ** 2), W1) + (k / 16) * (Math.atan2(-9, -Math.sqrt(circus.crescent ** 2 - 81)) - Math.atan2(-Math.sqrt(circus.crescent ** 2 - W1 ** 2), W1)); arc.push([r2(circus.crescent * Math.cos(a)), r2(circus.crescent * Math.sin(a))]); }
  plots.push({ id: 'lamplight-east', name: 'Boulevard west (old W2, the civic hall)', use: 'commercial', area: 'boulevard', poly: [[-94, -126], [W1, -126], ...arc, [W0, -9], [W0, -114], [-94, -114]] }); }
plot('west-wall-n', 'commercial', 'crown', [W0, W1, 9, north + 4], { name: 'Crown Avenue west (old W4)', notch });
plot('east-wall-s', 'commercial', 'boulevard', [E0, E1, sunset[1], market[0]], { name: 'Boulevard east (old E1)' });
plot('east-wall', 'commercial', 'boulevard', [E0, E1, market[1], -9], { name: 'Boulevard east (old E2, E3: the east lane a passage)', notch });
plot('east-wall-n', 'commercial', 'crown', [E0, E1, 9, north + 4], { name: 'Crown Avenue east (old E4)', notch });
manual('The east lane (old z 134–140) and the west lane (old z 2–8, on the line of Starline Street) are passages through one plot each (east-wall: old E2 and E3; west-wall: old W1 and W2); old Sunset Way west of the boulevard is the west wall plot\'s south flags, so the Handover\'s streets never join the boulevard\'s');
// Orbit Gardens and the west terrace (old DecoKitGardens; DecoKitCityBlocks' garden-square wall at x -110), from the zone's edge (y -148) north
plot('gardens-s', 'park', 'gardens', [gardensX[0], W0, -114, -9], { name: 'Orbit Gardens (canal)' });
plot('gardens-n', 'park', 'gardens', [-156, W0, 9, 50], { name: 'Orbit Gardens (north lawn, and the old west terrace\'s north end)' });
plot('terrace-s', 'residential', 'gardens', [-156, gardensX[0], -114, -9], { name: 'Garden terrace (old west terrace)' });
manual(`Orbit Gardens are trimmed at the Handover zone's edge (y -148, old z 58): the old lawns, canal and west terrace south of it (old z -40..58) are the Handover zone's; the canal runs from y -148 to ${yOf(old.gardens.canal.to)}, the pool at (${old.gardens.pool.c[0]}, 0), the pavilion at (${old.gardens.pavilion.at[0]}, ${yOf(old.gardens.pavilion.at[1])})`);
manual('The old west terrace (one 286 m wall facing the gardens at x -110, its back to the procedural massing) is one 28 m deep block south of the Gardens Gate at x -138..-110 (terrace-s, fronting Lamplight Lane, the garden walk at its back; the grammar wants 28 back to back, so its back faces Orbit Row); its 37 m north of the gate is lawn (gardens-n), where S1 turns north');
// Sphere Park: the old sphere station's ground and the sky rail's loop round it; the park the old town left north of the gardens
const swx = r2(old.skyrail.sphere[0]! - old.skyrail.ringR); // the sphere walk on the old ring's west point (x -218), under S1's north leg and its station
plots.push({ id: 'sphere-park', name: 'Sphere Park', use: 'park', area: 'sphere', poly: [[-328, 9], [-156, 9], [-156, 50], [W0, 50], [W0, sheetY[1] + 1], [-328, sheetY[1] + 1]] });
plot('west-terraces', 'residential', 'westend', [-244, -164, -114, -45], { name: 'West Terraces' });
plot('gardens-forecourt', 'open', 'westend', [-244, -164, -45, -9], { name: 'Gardens Forecourt (S1 Orbit Gardens station\'s south banks)' });
plot('western-park-n', 'park', 'sphere', [sheetX[0], -352, 9, 60], { name: 'Western Park' });
plot('western-park-s', 'park', 'westend', [sheetX[0], -352, -60, -9], { name: 'Western Park' });
// Southside, east of the Handover zone (old DecoKitSouthside), the boulevard through it
plot('n1', 'mixed', 'southside', [W0, W1, -306, sunset[0]], { name: 'Constellation (old N1, west half)' });
plot('n2', 'mixed', 'southside', [E0, E1, -306, sunset[0]], { name: 'Constellation (old N2)' });
plot('orbit-square-w', 'open', 'southside', [W0, W1, -356, -306], { name: 'Orbit Square' });
plot('orbit-square-e', 'open', 'southside', [E0, E1, -356, -306], { name: 'Orbit Square' });
plot('ss1', 'mixed', 'southside', [W0, W1, -401, -356], { name: 'old SS1' });
plot('starline-green', 'park', 'southside', [E0, E1, -401, -356], { name: 'Starline Green' });
plot('ss2', 'mixed', 'southside', [E0, E1, -466, -419], { name: 'old SS2' });
plot('ss4', 'mixed', 'southside', [E0, E1, -548, -478], { name: 'old SS4' });
plot('ss3', 'mixed', 'southside', [W0, W1, -548, -419], { name: 'old SS3 / SS5' });
plot('lantern-court', 'residential', 'lantern', [-180, -66, -484, -419]);
plot('lantern-west', 'residential', 'lantern', [-328, -192, -548, -419]);
plot('lantern-south', 'residential', 'lantern', [-180, -66, -548, -496]);
plot('crown-forecourt', 'open', 'crown', [W0, E1, north + 4, 204], { name: 'Crown Forecourt' });
manual('Southside trimmed: the Handover zone is where the old world sited the Handover too (its N4 and W2), and Satellite Street and the Records Office stand on old W1, N1\'s west half and the Rocket Lane diagonal (RL1, RL2, RB1), which go; Orbit Square keeps its east and west halves either side of the boulevard, which runs through it to the south edge (old Comet Row ended on the square)');

// ------------------------------------------------------------------ foot paths, water, quay
const foot = [
  byId(live.foot, 'garden-walk'),
  { id: 'gardens-gate', name: 'Gardens Gate', kind: 'mall', w: 18, pts: [[-156, 0], [-circus.crescent, 0]] },
  { id: 'crown-parade', name: 'Crown Parade', kind: 'walk', w: 18, pts: [[circus.crescent, 0], [harbour.quay, 0]] },
  { id: 'skyport-pier', name: 'Skyport Pier', kind: 'walk', w: 12, pts: [[harbour.quay, 0], [SK - 38, 0]] },
  { id: 'harbour-promenade', name: 'Harbour Promenade', kind: 'walk', w: harbour.promenade[1] - harbour.promenade[0], pts: [[r2((harbour.promenade[0] + harbour.promenade[1]) / 2), sheetY[0]], [r2((harbour.promenade[0] + harbour.promenade[1]) / 2), north + 4]] },
  { id: 'market-pier', name: 'Market Pier', kind: 'walk', w: piers.market[1] - piers.market[0], pts: [[harbour.quay, r2((piers.market[0] + piers.market[1]) / 2)], [248, r2((piers.market[0] + piers.market[1]) / 2)]] },
];
manual('The Gardens Gate (old: the circus\'s west gate promenade to the pool, 12 m) is an 18 m mall from Orbit Row to the circus walk ring: S1\'s pylons stand on it where it leaves Circus Road');
manual('Crown Parade is an 18 m walk on the circus axis to the quay (old: the wedge between the crescents, 24 m at the circus widening to 56 at the terrace): the crescents\' 14 m bands, which end ±12 m off the axis, need the plots to reach ±9');
const qy = harbour.quay;
const quay = [
  [[eb, sheetY[0]], [qy, sheetY[0]], [qy, sheetY[1]], [eb + 2, sheetY[1]], [eb + 2, north + 4], [eb, north + 4]],
  [[qy, -6], [SK - 38, -6], [SK - 38, 6], [qy, 6]],
  [[qy, piers.market[0]], [248, piers.market[0]], [248, piers.market[1]], [qy, piers.market[1]]],
];
const water = [[[qy, sheetY[0]], [sheetX[1], sheetY[0]], [sheetX[1], sheetY[1]], [qy, sheetY[1]], [qy, 6], [SK - 38, 6], [SK - 38, -6], [qy, -6], [qy, piers.market[1]], [248, piers.market[1]], [248, piers.market[0]], [qy, piers.market[0]]]];
manual(`The harbour: the upper terrace (x ${harbour.terrace.join('–')}), steps and promenade (to ${qy}) are one quay strip down the whole sheet (old z -300..380, run on to both edges); the sea from x ${qy}; the Skyport Pier on the axis to its apron, the Market Pier (old z ${S['MarketPierZ.x']}–${S['MarketPierZ.y']}) to the liner berth`);
const beyond = [[[sheetX[0], sheetY[0]], [-352, sheetY[0]], [-352, sheetY[1]], [sheetX[0], sheetY[1]]], [[W0, 288], [qy, 288], [qy, sheetY[1]], [W0, sheetY[1]]], [[-352, sheetY[0]], [qy, sheetY[0]], [qy, -552], [-352, -552]]];

// ------------------------------------------------------------------ transit: T1 (tram), S1 (west sky rail), S2 (harbour sky rail)
const ap = 25.2, vr = ap / Math.cos(Math.PI / 8), tri = (a: number): V => [r2(vr * Math.cos(a * Math.PI / 180)), r2(vr * Math.sin(a * Math.PI / 180))];
// the circus loop: an octagon circumscribing R 25 (apothem 25.0) with R 25 fillets = the circle; in and out on the side lines at ±45° below the centre
const yIn = r2(-1.8 - ap * Math.SQRT2); // where x = -1.8 meets the side line with outward normal at -45°
const loopPts = [[-1.8, -52], [-1.8, yIn, 25], [...tri(-22.5), 25], [...tri(22.5), 25], [...tri(67.5), 25], [...tri(112.5), 25], [...tri(157.5), 25], [...tri(202.5), 25], [1.8, yIn, 25], [1.8, -52]];
const T1 = {
  id: 'T1', name: 'Boulevard Line', beyond: 'west off-sheet along Crown Road to the West End: through-running; the terminal loop round Crown Circus\'s island on the ring road',
  paths: [
    { id: 'trunk', role: 'running', double: true, pts: [[sheetX[0], 280], [-340, 280, 25], [-340, -410, 25], [0, -410, 25], [0, -52]] },
    { id: 'loop', role: 'loop', pts: loopPts },
  ],
  order: ['trunk', 'loop'],
  stops: [
    { id: 'terraces', name: 'Sunset Terraces', path: 'trunk', at: [-340, 131], interchange: 'S1/west-end', fronts: 'sunset-way' },
    { id: 'lamplight', name: 'Lamplight', path: 'trunk', at: [-340, -155], fronts: 'sunset-way' },
    { id: 'rocket', name: 'Rocket', path: 'trunk', at: [-270, -410], fronts: 'market-street' },
    { id: 'orbit', name: 'Orbit Square', path: 'trunk', at: [0, -368], fronts: 'boulevard' },
    { id: 'circus', name: 'Crown Circus', path: 'trunk', at: [0, -68], fronts: 'boulevard' },
  ],
};
manual('The tram: the old line ran on the boulevard\'s west lane from Sunset Way to short of the circus, one track (old TrackX -2.5), one stop island (old z 100–124). The plan\'s rules want double track in the centre lanes, a loop at an end inside the sheet and stops 300–500 m apart: T1 runs the boulevard (stop "Crown Circus" at y -64, the old island\'s place) and turns on a loop round the circus island on the ring road (R 25), and its other end runs Market Street and Sunset Way to the West End, joining the sky rail at both ends; no overhead wires (TS-W0)');

// S1 in the plan, the sky line the generator can build here: its station banks are laid for east-west lines on the plan's y 0 only (tools/world/gen/connectors.ts:
// the bank rects stand at ±(t + c) from y 0), on park, open ground, foot paths or quay. So: from the West End along Circus Road's median, two stations on the circus axis,
// north over Orbit Gardens' north lawn and Sphere Park past the Sphere's east side (x -95), off the sheet north. (Was: from the West
// End along Circus Road's median with two stations on the circus axis (the old gardens gate's line), north over Orbit Gardens' north lawn and Sphere Park past the Sphere's
// east side, east behind the SpaceHQ over Crown Mall (its third station), south over the water off the quay and onto the Skyport Pier axis into the Skyport Tower's portal
// (its fourth station), turning on the loop round the aircar pad, as today. It joins T1 at Sunset Terraces. The old lines' own geometry (S1, S2 below) waits for north-south banks.
const S0 = {
  id: 'S1', name: 'Sphere Line', beyond: 'west off-sheet along Circus Road to the West End; north off-sheet past the Sphere toward the old main loop\'s north arc',
  paths: [{ id: 'trunk', role: 'line', double: true, pts: [[sheetX[0], 0], [-95, 0, 70], [-95, sheetY[1]]] }],
  order: ['trunk'],
  stations: [
    { id: 'west-end', name: 'Western Park', path: 'trunk', at: [-395, 0], length: 50, interchange: 'T1/terraces', fronts: 'circus-road', kit: 'SkyRailStation_Twin' },
    { id: 'gardens', name: 'Orbit Gardens', path: 'trunk', at: [-191, 0], length: 50, fronts: 'circus-road', kit: 'SkyRailStation_Twin' },
  ],
};
// The sky rail. The generator lays a station's stair, escalator and lift banks for an east-west line only (tools/world/gen/connectors.ts: "banks are laid for an east-west
// line"), and every old sky rail station stood on a north-south line (the main loop on the boulevard's west promenade, the harbour line on the quay). S1 and S2 below are the
// plan's geometry for the two old lines (radii, spans, stations), kept in oldtown.skyrail.planned and out of plan.skyrail until the generator lays north-south banks
// (Splice); with them the plan's network is T1 alone (N-C1, N-R1 hold on it). MANUAL says so.
// S1: the old main loop's west half (DecoKitSkyRail.Route: x -150..-218 round the Sphere at r 88, 24–34 m): from the West End along Circus Road's median, north through
// Sphere Park and round the Sphere on a terminal loop. The old loop's boulevard leg (x -13 over the west promenade) has no pylon ground the plan allows (pylonZones: median,
// mall, park, quay, water, island), and its return leg (x -150..-110) crossed the Handover's site; both are in oldtown.skyrail.main, for Splice (briefs/075/notes.md).
const sph = P(old.skyrail.sphere[0]!, old.skyrail.sphere[1]!), sx1 = r2(sph[0] - old.skyrail.ringR); // the old ring's west point (x -218): the north leg
const S1 = {
  id: 'S1', name: 'Sphere Line', beyond: 'west off-sheet along Circus Road to the West End; the terminal loop round the Sphere',
  paths: [
    { id: 'trunk', role: 'line', double: true, pts: [[sheetX[0], 0], [sx1, 0, 70], [sx1, 120]] },
    { id: 'loop', role: 'loop', pts: [[sx1 - 3, 120], [sx1 - 3, 145, 40], [sx1 - 57, 205, 40], [sx1, 265, 40], [sx1 + 57, 205, 40], [sx1 + 3, 145, 40], [sx1 + 3, 120]] },
  ],
  order: ['trunk', 'loop'],
  stations: [
    { id: 'circus-road', name: 'Circus Road', path: 'trunk', at: [-365, 0], length: 50, fronts: 'circus-road', kit: 'SkyRailStation_Twin' },
    { id: 'sphere', name: 'The Sphere', path: 'trunk', at: [sx1, 95], length: 50, fronts: 'sphere-park', kit: 'SkyRailStation_Twin' },
  ],
};
// S2: the old harbour line (DecoKitSkyRail.HarbourRoute: up the quay promenade at x 88, round the Skyport at R 48, back over the sea at x 122). The plan's line turns off the
// quay onto the pier axis on R 70 (S-R1) into the Skyport Tower's portal, its station inside, and turns on a loop round the aircar pad beyond.
const hx = old.skyrail.station[0]; // 88
const S2 = {
  id: 'S2', name: 'Harbour Line', beyond: 'south off-sheet along the quay; the terminal loop round the Skyport pad',
  paths: [
    { id: 'trunk', role: 'line', double: true, pts: [[hx, sheetY[0]], [hx, 0, 70], [SK + 30, 0]] },
    { id: 'loop', role: 'loop', pts: [[SK + 30, 3], [SK + 55, 3, 40], [SK + 105, 60, 40], [SK + 163, 0, 40], [SK + 105, -60, 40], [SK + 55, -3, 40], [SK + 30, -3]] },
  ],
  order: ['trunk', 'loop'],
  stations: [
    { id: 'apollo', name: 'Apollo Quay', path: 'trunk', at: [hx, -420], length: 50, fronts: 'harbour-promenade', kit: 'SkyRailStation_Twin' },
    { id: 'harbour', name: 'Harbour', path: 'trunk', at: [hx, -140], length: 50, interchange: 'T1/circus', fronts: 'harbour-promenade', kit: 'SkyRailStation_Twin' },
    { id: 'skyport', name: 'Skyport', path: 'trunk', at: [SK, 0], length: 50, fronts: 'skyport-pier', in: 'megastructures/skyport-tower', kit: 'SkyRailStation_Twin' },
  ],
};
manual('The plan\'s S1 (Sphere Line) is not an old line: the generator lays station banks only for east-west lines on y 0 (the circus axis), so S1 runs from the West End along Circus Road (Western Park and Orbit Gardens stations), north past the Sphere\'s east side (x -95) and off the sheet; it joins T1 at Sunset Terraces. No line reaches the Skyport (a station there would have to be 200–500 m from another on y 0, and the circus\'s 14 m island takes no pylon). The old lines stay planned data:');
manual('The old lines are not in plan.skyrail in this draft: the generator lays station banks for east-west lines only and the old lines run north-south (the main loop over the boulevard\'s west promenade, x -13; the harbour line on the quay, x 88). S1 (Sphere Line: Circus Road, north on x -218, a loop by the Sphere) and S2 (Harbour Line: up the quay, into the Skyport Tower, round the pad) are planned in oldtown.skyrail.planned with radii, spans and stations, ready for plan.skyrail once the generator lays north-south banks (Splice). The old boulevard leg also needs a pylon zone on the promenade (pylonZones has none) and is drawn as data only');
manual(`The harbour sky rail (planned) keeps the old quay leg (x ${hx}) and Harbour station (old (88, 110) → plan (88, -96), here 44 m south at -140 so the station spacing to the Skyport holds, 200 m); the old loop round the Skyport (R 48) becomes the plan's line into the Skyport Tower's portal (the tower 15 m east of the old Skyport, x 170 → ${SK}, so the R 70 turn off the quay ends before the portal) and the loop round the pad beyond; the old sea leg (x 122) is not built (the line ends on its loop)`);

// ------------------------------------------------------------------ landmarks, signatures, megastructures (the live entries, moved to the old positions)
const put = (kit: string) => old.puts.filter(p => p.kit === kit);
const mv = (p: number[], d: V) => [r2(p[0]! + d[0]), r2(p[1]! + d[1])];
const mvRect = (r: number[], d: V) => [r2(r[0]! + d[0]), r2(r[1]! + d[0]), r2(r[2]! + d[1]), r2(r[3]! + d[1])];
const landmarks: any[] = [];
{ const l = byId(live.landmarks, 'monument'); l.c = circus.c; l.r = circus.island; landmarks.push(l); }
{ const l = byId(live.landmarks, 'sphere'); l.c = sph; l.oldPut = put('SphereStation')[0]; landmarks.push(l);
  manual(`The Sphere stands at the old SphereStation (old (-130, 330) → plan (${sph}), yaw 20) at the plan's ×0.425 (top 80, Aster review 2026-10-12); the old town stood it at ×1 (188 m)`); }
{ const l = byId(live.landmarks, 'skylon'), p = put('Skylon')[0]!; l.c = P(p.at[0], p.at[2]); landmarks.push(l); }
const skyDelta: V = [SK - 545, 0];
{ const p = old.puts.find(q => q.kit === 'Skyport_Tower')!; landmarks.push({ id: 'skyport-tower', name: 'Skyport Tower', c: P(p.at[0], p.at[2]), r: 38, kit: 'Skyport_Tower', top: 108.2, caisson: { r: 38, deck: 1.2, how: 'the old Skyport stood on the water at the Skyport Pier\'s head (DecoKitHarbour: Skyport_Tower at WaterY, its apron 22 m); procedural caisson drum, as the liner berth\'s' }, oldPut: p,
  kitFit: 'the old Skyport (DecoKitHarbour: Skyport_Tower at old (170, 206), yaw 180, on its apron at the Skyport Pier\'s head): the kit 55.1 × 114.8 × 52.3 at × 0.94 (top 108, standards.skyline.harbour: the harbour\'s icon), r 38 its half diagonal; the landing discs are the kit\'s' });
  manual('The Skyport is the old Skyport_Tower piece at the old position (plan (170, 0)) as a landmark at 108 m (× 0.94), not today\'s Skyport Tower megastructure: that portal tower needs a sky line and its station through it, and no line the generator can build reaches the pier (S2 planned); the aircar pad landmark goes (the tower carries the old discs)'); }
const linerDelta: V = (() => { const l = byId(live.landmarks, 'liner-berth'); const want: V = [248 + l.r, r2((piers.market[0] + piers.market[1]) / 2)]; return [r2(want[0] - l.c[0]), r2(want[1] - l.c[1])]; })();
{ const l = byId(live.landmarks, 'liner-berth'); l.c = mv(l.c, linerDelta); landmarks.push(l);
  manual(`The liner berth closes old Market Street's vista at the Market Pier's head (old LinerDock at x 270 on the pier's axis; plan (${l.c}), the pier 152 m long)`); }
for (const [id, p] of put('DiscTower').map((p, i) => [i ? 'disc-east' : 'disc-west', p] as const)) {
  const at = P(p.at[0], p.at[2]);
  landmarks.push({ id, name: `Avenue gate tower ${id === 'disc-west' ? 'west' : 'east'}`, c: at, r: 20.5, kit: 'DiscTower', top: 108, scale: r2(108 / 156.6), oldPut: p,
    kitFit: 'the old DecoKitStreetSkyline pair framing the arch (x ±44, z 372): the old stood DiscTower × 1.0 (156.6); here capped at 108, standards.skyline.harbour (nothing within 150 m of the quay tops the Skyport Tower), so × 0.69; r 20.5 ≥ 40.2 / 2; no crown type (SK-2 has no landmark pairs; the pair is matched)' });
  manual(`The gate towers (old DiscTower pair at x ±44, z 372) stand at 108 (× 0.69), not the old 156.6: they are within 150 m of the quay and standards.skyline.harbour holds that for the Skyport Tower (Sky\'s call to lift)`);
}
const arch = put('ParabolicArch')[0]!;
{ const v = byId(live.landmarks, 'crown-vista').gateway; v.c = P(arch.at[0], arch.at[2]); v.feet = v.feet.map((f: number[]) => [f[0], f[1], r2(f[2]! - 53 + v.c[1]), r2(f[3]! - 53 + v.c[1])]);
  void v; manual(`The ParabolicArch across Crown Avenue (old (0, 250) → plan (0, 44)) is not placed: its feet (plinths at x 14–17) need a district street's 8 m pavement frontage zone and Crown Avenue is access (6.5 m), so the circus stays sealed at c_s; it is in oldtown.props for Splice and Vela`);
  const archWas = { ...v, status: `the old ParabolicArch across Crown Avenue (old (0, ${arch.at[2]}) → plan (${v.c})), carried as the west gate tower's gateway (the generator places a landmark's gateway, kitrefs.ts gatewayRef)` }; landmarks.find(l => l.id === 'disc-west').archWas = archWas; }
manual('The old SkyBridge between the gate towers (60 m up) is in oldtown.props, not placed: no plan structure carries it yet');
// SpaceHQ at the old position closing the boulevard (old (0, 450) × 1.35): the plan's regenerated SpaceHQ_Portal, its drum without a beam (the plan's portal reads a line on y 0)
const hqDelta: V = (() => { const p = put('SpaceHQ')[0]!, at = P(p.at[0], p.at[2]); return [r2(at[0] - 96), r2(at[1] - 0)]; })();
const signatures: any[] = [];
{ const s = byId(live.signatures, 'space-hq'); s.c = mv(s.c, hqDelta); s.ground = mvRect(s.ground, hqDelta); s.legs = s.legs.map((l: number[]) => mvRect(l, hqDelta)); s.areas = ['crown'];
  s.spans = { plots: [], foot: [] }; s.portalWas = s.portal; delete s.portal; s.oldPut = put('SpaceHQ')[0];
  s.standWhy = `the old SpaceHQ's place: on the axis at old z 450 (plan ${s.c}), closing the boulevard and Crown Avenue through the arch; the plan's SpaceHQ_Portal (252 m) at × 1 (the old stood the kit at × 1.35)`;
  signatures.push(s);
  manual('SpaceHQ: at the old position (plan (0, 244)), closing the axis; its drum keeps the regenerated SpaceHQ_Portal but carries no beam (portalWas): the plan\'s signature portal reads a line on y 0, and the old main loop turned west before the HQ'); }
{ const s = byId(live.signatures, 'ocean-liner'); s.c = mv(s.c, linerDelta); s.ground = mvRect(s.ground, linerDelta); signatures.push(s); }
const megastructures: any[] = [];
{ const m = byId(live.megastructures, 'crown-terraces'), r = plots.find(q => q.id === 'lantern-court').rect as number[];
  m.id = 'lantern-terraces'; m.name = 'Lantern Terraces'; m.areas = ['lantern']; m.footprint = [r]; m.spans = { plots: ['lantern-court'], roads: ['lantern-lane'], zones: [] };
  m.section = 'two-storey podium full depth to the deck (9.43); the 8 m Terrace Walk along its south edge (Lantern Lane\'s south arm); housing steps up one storey per step from 5 storeys at the walk to 12 on Market Street';
  m.walk = { edge: 'y0', w: 8, from: `the plot edge y ${r[2]} (Lantern Lane) to y ${r[2]! + 8}` };
  m.steps = { storeys: [5, 12], from: `the walk's back (y ${r[2]! + 8})`, to: `Market Street's building line (y ${r[3]! - 8})`, why: 'one storey per step, equal depths' };
  m.breaks = []; m.breaksWhy = 'none';
  m.colonnade = [{ footprint: 0, edge: 'y0', faces: 'lantern-lane', class: 'access', depth: 6.5, over: 'deck', clearHeight: 8.05, columnZone: 'kerb', columnEvery: 12, why: 'the Terrace Walk overhangs Lantern Lane\'s pavement' }];
  m.buildingLine = 'Market Street (north, district): blank stone; Rocket Lane and Satellite Street (west, east): the podium\'s shops; Lantern Lane the colonnade';
  m.bridges = []; m.connectors = 'stair-lift cores in the podium every 60 m along the walk'; m.supports = { zones: ['own plots'] };
  m.why = 'the second megastructure the plan keeps (MS-1, 2–4), the same terraced housing as the West Terraces, on the Lantern Quarter\'s court inside Lantern Lane (the old Southside SS5\'s place), facing south';
  megastructures.push(m);
  manual('Lantern Terraces (a second terraced-housing megastructure, Crown Terraces\' kind) on lantern-court, facing south over Lantern Lane: MS-1 keeps 2–4 megastructures and the old town\'s only one (the Skyport) cannot be today\'s portal tower without a sky line'); }
{ const m = byId(live.megastructures, 'crown-terraces');
  const fp: number[] = [-244, -164, -114, -45];
  m.name = 'West Terraces'; m.areas = ['westend']; m.footprint = [fp]; m.spans = { plots: ['west-terraces'], roads: ['lamplight-lane'], zones: [] };
  m.section = 'two-storey podium full depth to the deck (9.43); the 8 m Terrace Walk along its south edge (Lamplight Lane); housing steps up one storey per step from 5 storeys at the walk to 12 on Circus Road';
  m.walk = { edge: 'y0', w: 8, from: 'the building line y -129.5 (Lamplight Lane) to y -121.5' };
  m.steps = { storeys: [5, 12], from: 'the walk\'s back (y -121.5)', to: 'Circus Road\'s building line (y -17)', why: 'one storey per step, equal depths' };
  m.breaks = []; m.breaksWhy = 'none: the terraces face south over Lamplight Lane, the Sphere is north of Circus Road';
  m.colonnade = [{ footprint: 0, edge: 'y0', faces: 'lamplight-lane', class: 'access', depth: 6.5, over: 'deck', clearHeight: 8.05, columnZone: 'kerb', columnEvery: 12, why: 'the Terrace Walk overhangs Lamplight Lane\'s pavement' }];
  m.buildingLine = 'Sunset Way (west, primary) x -323 and Circus Road (north, district) y -17: blank stone; Orbit Row (east) the podium\'s shops; Lamplight Lane the colonnade';
  m.bridges = []; m.connectors = 'stair-lift cores in the podium every 60 m along the walk'; m.supports = { zones: ['own plots'] };
  m.why = 'the plan keeps two megastructures (MS-1, 2–4): the Skyport Tower and these terraces, re-sited from the north edge (where the old town has Sphere Park and the SpaceHQ) to the procedural massing\'s place west of the gardens (old DecoKitCityMass), facing south';
  megastructures.push(m);
  manual('Crown Terraces (megastructure) move to the West Terraces plot (x -244..-164, y -114..-45) facing south over Lamplight Lane, keeping MS-1\'s 2–4: the old town had procedural massing there (DecoKitCityMass); no bridges'); }

// T1's stops between Sunset Terraces and Crown Circus at equal chainage (T-S1 300–500 m), on the trunk as derive lays it
{ const tmp = derive({ ...live, nodes, roads, plots: [], foot: [], tram: [T1], skyrail: [], landmarks: [], megastructures: [], signatures: [] } as any).tram.find(p => p.id === 'trunk')!;
  const sAt = (p: V) => tmp.samples.reduce((b, x) => (Math.hypot(x.p[0] - p[0], x.p[1] - p[1]) < Math.hypot(b.p[0] - p[0], b.p[1] - p[1]) ? x : b)).s;
  const ids = ['terraces', 'lamplight', 'rocket', 'orbit', 'circus'], s0 = sAt(T1.stops[0]!.at as V), s1 = sAt(T1.stops[4]!.at as V), g = (s1 - s0) / 4;
  for (let k = 1; k < 4; k++) { const want = s0 + k * g, x = tmp.samples.reduce((b, q) => (Math.abs(q.s - want) < Math.abs(b.s - want) ? q : b)); T1.stops.find(t => t.id === ids[k])!.at = [r2(x.p[0]), r2(x.p[1])]; }
  manual(`T1's stops: Sunset Terraces (-340, 131, the S1 interchange) and Crown Circus (0, -68, the old island's place) fixed; Lamplight, Rocket and Orbit Square at equal chainage between them (${r2(g)} m)`); }

// ------------------------------------------------------------------ assemble
const plan: any = clone(live);
Object.assign(plan, {
  version: bump(live.version), date: '2026-10-28',
  status: `brief 075 remap (Sky 2026-10-28: "why can't we just remap the world around d handover using old buildings"): the old DecoKit town transcribed by tools/plan/oldtown/transcribe.ts from ${OLD_DIR} (sources sha256 in oldtown.sources), the Handover zone kept byte for byte. ${live.status}`,
  frame: { ...live.frame, origin: 'Crown Circus centre = the old DecoKit frame\'s (0, 206): plan (x, y) = old Unity (x, z − 206), unscaled' },
  sheet: { ...live.sheet, x: sheetX, y: sheetY },
  nodes, links, roads, gates, areas, plots, foot, water, quay, beyond,
  circus: { c: circus.c, island: circus.island, garden: 10, ring: [circus.island, RING_OUT], pavement: circus.crescent },
  landmarks, megastructures, signatures, tram: [T1], skyrail: [S0],
});
plan.routes = undefined; delete plan.routes;
if (args.includes('--with-skyrail')) plan.skyrail = [S1, S2];

// standards that name today's roads, plots and lines
const st = plan.standards;
st.streetscape.sections.tramStreet.district.roads = ['boulevard', 'market-street'];
st.streetscape.sections.tramStreet.primary.roads = ['sunset-way', 'crown-road'];
delete st.streetscape.sections.tramStreet.notTram;
// the kerb faces with no plot edge to run on (streetscape.roads.kerbs): the circus mouths of the two radial roads, and every foot way's mouth on a road
{ const K = st.streetscape.roads.kerbs, R = RING_OUT, pav = circus.crescent, h = KERB, y0 = r2(Math.sqrt(R * R - h * h)), y1 = r2(Math.sqrt(pav * pav - h * h));
  K.circusMouths.faces = [
    { id: 'circus-mouth/crown-avenue/e', road: 'crown-avenue', a: [h, y0], b: [h, y1], toward: [-1, 0] }, { id: 'circus-mouth/crown-avenue/w', road: 'crown-avenue', a: [-h, y0], b: [-h, y1], toward: [1, 0] },
    { id: 'circus-mouth/boulevard/e', road: 'boulevard', a: [h, -y0], b: [h, -y1], toward: [-1, 0] }, { id: 'circus-mouth/boulevard/w', road: 'boulevard', a: [-h, -y0], b: [-h, -y1], toward: [1, 0] },
  ];
  K.circusMouths.why = `the kerb of each radial road between the ring's outer kerb (r ${R}) and the plot corner at the circus pavement (r ${pav}); the Gardens Gate and Crown Parade mouths are walks at pavement level, so the ring's outer kerb runs on across them (old DecoKitCircus)`;
  const keeps = K.footMouths.faces.filter((f: any) => f.id.startsWith('foot-mouth/garden-walk/'));
  const wl = r2((lanes.west[0] + lanes.west[1]) / 2), sw = nodes.b_sunset![1];
  K.footMouths.faces = [...keeps,
  ];
  void wl; void sw;
  const M = st.streetscape.roads.marks; if (M?.lanes) M.lanes = {};
  const C = st.streetscape.crossings.coverage; C.remove = []; C.add = [];
  manual('Kerb faces (streetscape.roads.kerbs): the circus mouths of the Boulevard and Crown Avenue, and the mouths of the garden walk, the west lane, the Sunset Walk, the Gardens Gate and the Sphere Walk on their roads, recomputed for the old town; the Handover\'s (garden walk on Starline Street) kept. The crossing coverage\'s hand removals named today\'s crossings: cleared (the rules still apply)'); }
st.buildings.towers.sites = [];
manual('towers.sites: none. The old town has no frontage towers on the composed district (its towers were Southside podium towers, DecoKitHighrise, and background towers up the coast, oldtown.props.towers); every live site named a plot that is gone');
st.skyline.vista = undefined; delete st.skyline.vista;
manual('The Crown Vista tower (250 m, standards.skyline.vista) goes: the old axis closes on the SpaceHQ through the arch, between the two DiscTowers, as the old reel');
{ const mx = st.frontage.mix; st.frontage.mix = { records: mx.records, lamplight: mx.lamplight, boulevard: mx.crescent, crown: mx.records, southside: mx.gardens, westend: mx.gardens, gardens: mx.lamplight, lantern: mx.lantern, harbour: mx.harbour };
  manual('Frontage mixes for the new areas from today\'s: the Boulevard takes the Crescent Quarter\'s, Crown Circus the Records Precinct\'s, Southside and the West Terraces Sphere Gardens\' (their faces on squares and greens), the garden terrace Lamplight\'s, the Lantern Quarter and the harbour keep theirs'); }
if (st.buildings.caps) st.buildings.caps = { records: st.buildings.caps.records, lamplight: st.buildings.caps.lamplight };

// ------------------------------------------------------------------ the old frontage as blocks (standards.blocks, BG-1..BG-5)
plan.blocks = []; plan.circus.crescents = [];
const d0 = derive(plan as Plan), civ0 = civic(plan.civic, JSON.parse(readFileSync(resolve(ROOT, plan.civic.block), 'utf8')));
const ctx = ctxOf(plan as Plan, d0, civ0, 0.18);
const lineOf = (id: string) => { const pl = d0.plots.find(p => p.id === id)!; return buildingLine(ctx, plotRing(ctx, pl))!; };
const facing = (id: string, side: 'w' | 'e' | 's' | 'n') => {
  const bl = lineOf(id), n = bl.p.length, want = { w: [-1, 0], e: [1, 0], s: [0, -1], n: [0, 1] }[side];
  let best = 'none', len = 0;
  for (let i = 0; i < n; i++) { const a = bl.p[i]!, b = bl.p[(i + 1) % n]!, L = Math.hypot(b[0] - a[0], b[1] - a[1]); const nrm = [(b[1] - a[1]) / L, -(b[0] - a[0]) / L]; if (nrm[0]! * want[0]! + nrm[1]! * want[1]! > 0.99 && L > len) { len = L; best = bl.c[i]!; } }
  return best;
};
const capOf = (c: string) => ({ Corner: 'corner', Tower: 'corner', End: 'end', Open: 'open' } as Record<string, string>)[c]!;
type Run = { face: string; a: V; b: V; n: V; faces: string; caps: [string, string]; bays: number; gap?: [number, number]; segs?: Seg[]; old?: string };
/** An old wall as a run. caps: the plan's caps (an old corner at the start that becomes an end moves the start one cell on, so the bays stay where they stood);
 *  drop: bays taken off the first (start) segment; y0 / y1: the run's start / end set (a run trimmed to the building line, or snapped to its perpendicular wall). */
const runOfWall = (w: OldWall, face: 'e' | 'w', faces: string, o: { caps?: [string, string]; drop?: number; y0?: number; y1?: number } = {}): Run => {
  const c = o.caps ?? [capOf(w.caps[0]), capOf(w.caps[1])], segs = clone(w.segs);
  for (let left = o.drop ?? 0; left > 0;) { const g = segs[0]; if (!g) throw new Error(`${w.src}: cannot drop ${o.drop} bays`); const t = Math.min(left, g.n); g.n -= t; left -= t; if (!g.n) segs.shift(); }
  const bays = segs.reduce((s, g) => s + g.n, 0), L = (c[0] === 'corner' ? 8 : 0) + 6 * bays + (c[1] === 'corner' ? 8 : 0);
  let a = P(w.start[0], w.start[1]);
  if (w.caps[0] !== 'Open' && capOf(w.caps[0]) === 'corner' && c[0] !== 'corner') a = [a[0], r2(a[1] + 8)];
  if (o.y0 !== undefined) a = [a[0], o.y0]; else if (o.y1 !== undefined) a = [a[0], r2(o.y1 - L)];
  return { face, a, b: [a[0], r2(a[1] + L)], n: w.normal === 'E' ? [1, 0] : [-1, 0], faces, caps: c, bays, segs, old: w.src };
};
const wall = (x: number, z: number) => { const w = old.walls.find(v => Math.abs(v.start[0] - x) < .01 && Math.abs(v.start[1] - z) < .02); if (!w) throw new Error(`no old wall at ${x}, ${z}`); return w; };
const short = (x0: number, z: number) => old.shorts.find(s => Math.abs(s.x0 - x0) < .01 && Math.abs(s.z - z) < .01);
const shortRun = (x0: number, z: number, face: 's' | 'n', faces: string, yAt?: number): Run => {
  const s = short(x0, z)!, y = yAt ?? yOf(z), segs: Seg[] = s.west === s.east ? [{ cls: s.west, n: 2 }] : [{ cls: s.west, n: 1 }, { cls: s.east, n: 1 }];
  return { face, a: [x0, y], b: [x0 + 28, y], n: face === 's' ? [0, -1] : [0, 1], faces, caps: ['corner', 'corner'], bays: 2, segs, old: s.src };
};
const block = (id: string, plotId: string, runs: Run[], court = 'solid') => {
  const xs = runs.flatMap(r => [r.a[0], r.b[0]]), ys = runs.flatMap(r => [r.a[1], r.b[1]]);
  const corners: V[] = []; for (const r of runs) { if (r.caps[0] === 'corner') corners.push(r.a); if (r.caps[1] === 'corner') corners.push(r.b); }
  const uniq = corners.filter((c, i) => corners.findIndex(o => Math.abs(o[0] - c[0]) < .01 && Math.abs(o[1] - c[1]) < .01) === i);
  plan.blocks.push({ id, plot: plotId, kind: 'perimeter', rect: [Math.min(...xs), Math.max(...xs), Math.min(...ys), Math.max(...ys)], depth: st.blocks.band, court, runs, corners: uniq });
};
const crescent = (id: string, plotId: string, blockId: string, oc: typeof old.crescents[number], sweep: 'cw' | 'ccw', x: number, ySign: number, toward: string) => {
  const r = circus.crescent, half = Math.asin(st.blocks.bay / 2 / r), step = 2 * half * 180 / Math.PI, rEnds = r / Math.cos(half);
  const th0 = Math.atan2(ySign * Math.sqrt(rEnds * rEnds - x * x), x) * 180 / Math.PI;
  plan.circus.crescents.push({ id, plot: plotId, block: blockId, from: `${blockId} ${x < 0 ? 'e' : 'w'} run (arc)`, toward, c: circus.c, r, rEnds: r2(rEnds), chord: st.blocks.bay, stepDeg: Math.round(step * 1e4) / 1e4,
    deg: [Math.round(th0 * 1e3) / 1e3, Math.round((th0 + (sweep === 'ccw' ? 1 : -1) * oc.count * step) * 1e3) / 1e3], sweep, bays: oc.count, caps: ['open', 'end'], band: st.blocks.band, cls: oc.cls, old: oc.src });
};
const arcGap = (x: number, y: number) => r2(Math.abs(Math.abs(y) - Math.sqrt((circus.crescent / Math.cos(Math.asin(3 / circus.crescent))) ** 2 - x * x)));
const blv = 'district:boulevard', av = 'access:crown-avenue';
// W1: old -fx / wb from z -38, four bays, corners all round; short walls south (Sunset Walk) and north (west lane)
block('boulevard-west', 'boulevard-west', [runOfWall(wall(-fx, S.WestSouth!), 'e', blv), runOfWall(wall(wb, S.WestSouth!), 'w', facing('boulevard-west', 'w')), shortRun(wb, S.WestSouth!, 's', 'party:n1'), shortRun(wb, S['WestLane.x']!, 'n', 'passage:west-lane')]);
// W2: from the west lane (end) to the circus (arc); the garden wall's corner at 178 (old: no perpendicular run, the crescent's back) is an end here
{ const ew = wall(-fx, S['WestLane.y']!), ww = wall(wb, S['WestLane.y']!), y0 = -126, eDrop = Math.round((y0 - yOf(S['WestLane.y']!)) / 6);
  const e = runOfWall(ew, 'e', blv, { drop: eDrop, y0 }), w = runOfWall(ww, 'w', facing('lamplight-east', 'w'), { caps: ['end', 'end'], drop: eDrop, y0 });
  manual(`W2 starts at y ${y0} (old -198: the west lane), north of lamplight-south-e: ${eDrop} bays off its south end (old civic hall run S6 2, H 12 → H ${12 - (eDrop - 2)}), its garden wall the same`);
  e.caps = ['end', 'arc']; e.gap = [0, arcGap(e.b[0], e.b[1])];
  block('lamplight-east', 'lamplight-east', [e, w]); crescent('crescent-sw', 'lamplight-east', 'lamplight-east', old.crescents.find(c => Math.abs(c.phi0 - (S.phi! - 180)) < .01)!, 'cw', -fx, -1, 'walk:gardens-gate');
  manual('W2\'s garden wall (old: End, Corner at z 178) ends square here: its corner cell at the crescent\'s back had no perpendicular run (BG-2); 27 bays from the lane as old'); }
// W4 / E4: from the circus (arc) to the forecourt. The old boulevard walls started at CircusNorth (236.017) and the short walls stood at 346, 1.7 cm apart: the walls end on
// the short walls here. The garden and terrace walls started at old z 234 on a corner no perpendicular run meets: an end one cell on, their bays where they stood.
const nY = yOf(S.NorthFacade!);
{ const e = runOfWall(wall(-fx, S.CircusNorth!), 'e', av, { y1: nY }), w = runOfWall(wall(wb, S.ParadeNorth!), 'w', facing('west-wall-n', 'w'), { caps: ['end', 'corner'] }), n = shortRun(wb, S.NorthFacade!, 'n', facing('west-wall-n', 'n'));
  e.caps = ['arc', 'corner']; e.gap = [arcGap(e.a[0], e.a[1]), 0];
  block('west-wall-n', 'west-wall-n', [e, w, n]); crescent('crescent-nw', 'west-wall-n', 'west-wall-n', old.crescents.find(c => Math.abs(c.phi0 + S.phi!) < .01)!, 'ccw', -fx, 1, 'walk:gardens-gate'); }
{ const w = runOfWall(wall(fx, S.CircusNorth!), 'w', av, { y1: nY }), e = runOfWall(wall(eb, S.ParadeNorth!), 'e', facing('east-wall-n', 'e'), { caps: ['end', 'corner'] }), n = shortRun(fx, S.NorthFacade!, 'n', facing('east-wall-n', 'n'));
  w.caps = ['arc', 'corner']; w.gap = [arcGap(w.a[0], w.a[1]), 0];
  block('east-wall-n', 'east-wall-n', [w, e, n]); crescent('crescent-ne', 'east-wall-n', 'east-wall-n', old.crescents.find(c => Math.abs(c.phi0 - S.phi!) < .01)!, 'cw', fx, 1, 'walk:crown-parade'); }
manual('W4 and E4\'s boulevard walls end on their short walls at y 140 (old: they started at CircusNorth 236.017 and ran 110 m, 1.7 cm past the short walls at 346); their garden and terrace walls (old: a corner at z 234 meeting no run) start one cell on with an end');
// E1 and E2 face the plan's side streets (access, 6.5 m pavements; old Sunset Way and Market Street had 2–4 m flags): their runs are trimmed to the building line
{ const bl = lineOf('east-wall-s'), ys = bl.p.map(p => p[1]), top = r2(Math.max(...ys)), bot = r2(Math.min(...ys));
  const ow = wall(fx, S.EastSouth!), oldBays = ow.segs.reduce((t, g) => t + g.n, 0), fitBays = Math.floor((top - bot - 16 + 1e-6) / 6), drop = oldBays - fitBays;
  const w = runOfWall(ow, 'w', blv, { drop, y1: top }), e = runOfWall(wall(eb, S.EastSouth!), 'e', facing('east-wall-s', 'e'), { drop, y1: top });
  block('east-wall-s', 'east-wall-s', [w, e, shortRun(fx, S.EastSouth!, 's', 'access:sunset-east', w.a[1]), shortRun(fx, S['SideZ.x']! - 2, 'n', 'access:harbour-market', top)]);
  manual(`E1: ${fitBays} bays (old ${oldBays}, ${drop} off its south segment), between the plan's 6.5 m pavements on Sunset Way and Market Street (y ${w.a[1]}..${top}; old -242..-148)`); }
{ const bl = lineOf('east-wall'), y0 = r2(Math.min(...bl.p.filter(p => p[0] > fx - 1 && p[0] < eb + 1).map(p => p[1]))), lane = lanes.east[0];
  const ow = wall(fx, S['SideZ.y']! + 2), oldBays = ow.segs.reduce((t, g) => t + g.n, 0), fitBays = Math.floor((lane - y0 - 8 + 1e-6) / 6), drop = oldBays - fitBays;
  const e2w = runOfWall(ow, 'w', blv, { drop, y1: lane }), e2e = runOfWall(wall(eb, S['SideZ.y']! + 2), 'e', facing('east-wall', 'e'), { drop, y1: lane }), e2s = shortRun(fx, S['SideZ.y']! + 2, 's', 'access:harbour-market', e2w.a[1]);
  block('east-wall/a', 'east-wall', [e2w, e2e, e2s]);
  manual(`E2: ${fitBays} bays (old ${oldBays}), from Market Street's building line to the east lane (y ${e2w.a[1]}..${lane}; old -128..-72)`);
  const e3w = runOfWall(wall(fx, S['EastLane.y']!), 'w', blv), e3e = runOfWall(wall(eb, S['EastLane.y']!), 'e', facing('east-wall', 'e'), { caps: ['end', 'end'] });
  e3w.caps = ['end', 'arc']; e3w.gap = [0, arcGap(e3w.b[0], e3w.b[1])];
  block('east-wall/b', 'east-wall', [e3w, e3e]); crescent('crescent-se', 'east-wall', 'east-wall/b', old.crescents.find(c => Math.abs(c.phi0 - (180 - S.phi!)) < .01)!, 'ccw', fx, -1, 'walk:crown-parade');
  manual('E3\'s terrace wall (old: End, Corner at z 178, the parade\'s corner) ends square: no perpendicular run meets that corner (BG-2)'); }
// the rest: blocks fitted to each plot's building line by the grammar (8 + 6k + 8), long faces on the highest-rank street, the old classes where an old block stood there
const rank = st.blocks.rank as Record<string, number>;
const fit = (id: string, oldSegs?: { long: Seg[] }, anchor?: { y?: 's' | 'n'; x?: 'w' | 'e'; W?: number; H?: number }) => {
  const bl = lineOf(id), xs = bl.p.map(p => p[0]), ys = bl.p.map(p => p[1]), x0 = Math.min(...xs), x1 = Math.max(...xs), y0 = Math.min(...ys), y1 = Math.max(...ys);
  const L = (len: number) => 16 + 6 * Math.floor((len - 16 + 1e-6) / 6), W = Math.min(L(x1 - x0), anchor?.W ?? Infinity), H = Math.min(L(y1 - y0), anchor?.H ?? Infinity);
  if (Math.min(W, H) < st.blocks.backToBack) throw new Error(`${id}: building line ${r2(x1 - x0)} × ${r2(y1 - y0)} is too small for a 28 m block`);
  const rk = (s: 'w' | 'e' | 's' | 'n') => rank[kindOf(facing(id, s))] ?? 0;
  const bx0 = (anchor?.x ? anchor.x === 'e' : rk('e') > rk('w')) ? r2(x1 - W) : r2(x0), by0 = (anchor?.y ? anchor.y === 'n' : rk('n') > rk('s')) ? r2(y1 - H) : r2(y0), bx1 = r2(bx0 + W), by1 = r2(by0 + H);
  const court = Math.min(W, H) >= st.blocks.solidBelow ? 'open' : 'solid';
  const run = (face: 'w' | 'e' | 's' | 'n'): Run => {
    const along = face === 's' || face === 'n', a: V = face === 'e' ? [bx1, by0] : face === 'n' ? [bx0, by1] : [bx0, by0], b: V = along ? [bx1, a[1]] : [a[0], by1];
    return { face, a, b, n: ({ w: [-1, 0], e: [1, 0], s: [0, -1], n: [0, 1] } as Record<string, V>)[face]!, faces: facing(id, face), caps: ['corner', 'corner'], bays: ((along ? W : H) - 16) / 6, ...(oldSegs ? { segs: oldSegs.long } : {}) };
  };
  block(id, id, (['w', 'e', 's', 'n'] as const).map(run), court);
};
fit('terrace-s', undefined, { y: 's', x: 'e', W: 28, H: 88 }); // the old terrace: 28 deep with its face on the gardens (x -110); 88 long, its north end clear of Circus Road's end and pavement (B-3)
for (const id of ['lamplight-west', 'n1', 'n2', 'ss1', 'ss2', 'ss3', 'ss4', 'lantern-west', 'lantern-south']) fit(id);
st.blocks.plots = ['boulevard-west', 'lamplight-east', 'west-wall-n', 'east-wall-s', 'east-wall', 'east-wall-n', 'terrace-s', 'lamplight-west', 'n1', 'n2', 'ss1', 'ss2', 'ss3', 'ss4', 'lantern-west', 'lantern-south'];
st.blocks.exempt = {
  'garden-row-east': live.standards.blocks.exempt['garden-row-east'], 'pylon-row-east': live.standards.blocks.exempt['pylon-row-east'], 'rocket-west': live.standards.blocks.exempt['rocket-west'],
  'lamplight-south-w': 'handover: reaches into the Handover zone (y -192..-148)', 'lamplight-south-e': 'handover: reaches into the Handover zone (x -156..-84, y -192..-148)',
  'records-square': live.standards.blocks.exempt['records-square'], 'records-office': live.standards.blocks.exempt['records-office'],
  'west-terraces': 'megastructure: the West Terraces (terraced housing over a podium, megastructures.crown-terraces) cover the plot', 'lantern-court': 'megastructure: the Lantern Terraces (megastructures.lantern-terraces) cover the plot',
};
manual('Blocks: the boulevard walls, their short walls and the four crescents are the old walls exactly (start, length, caps, segments and storey classes; plan.blocks[].runs[].segs and .old). Every other laid plot (the garden terrace, Southside, the Lantern Quarter) is fitted by the grammar to its building line, its long faces on the higher-rank street; their classes are left to the generator (Splice) where no old block stood');

// ------------------------------------------------------------------ the old town as data for Splice, Vela and Latch (not read by the checker)
plan.oldtown = {
  source: OLD_DIR, sources: sources(), frame: 'plan (x, y) = old (x, z − 206)', manual: MANUAL,
  walls: old.walls.map(w => ({ start: P(w.start[0], w.start[1]), dir: w.dir, faces: w.normal, segs: w.segs, caps: w.caps, src: w.src })),
  shorts: old.shorts.map(s => ({ x0: s.x0, y: yOf(s.z), faces: s.normal, west: s.west, east: s.east, src: s.src })),
  crescents: old.crescents.map(c => ({ phi0Compass: c.phi0, step: c.step, cls: c.cls, count: c.count, endCap: c.endCap, src: c.src })),
  hallFill: old.hall.map(([x, z]) => ({ x: -fx - 14, z: [yOf(x[0]!), yOf(x[1]!)], z2: [yOf(z[0]!), yOf(z[1]!)], deck: 11, note: 'DecoKitCityBlocks.HallFill: a limestone deck at the civic hall\'s parapet behind its 8 m depth' })),
  southside: old.southside.blocks.map(b => ({ ...b, c: P(b.x, b.z), built: ['N2', 'SS2', 'SS4'].includes(b.name) ? 'as plotted' : ['N1', 'SS1', 'SS3', 'SS5'].includes(b.name) ? 'trimmed into a plot of the plan' : 'gone: the Handover zone' })),
  gardens: { canal: { x: old.gardens.canal.x, half: old.gardens.canal.half, y: [-148, yOf(old.gardens.canal.to)], oldY: [yOf(old.gardens.canal.from), yOf(old.gardens.canal.to)] }, pool: { c: P(old.gardens.pool.c[0], old.gardens.pool.c[1]), r: old.gardens.pool.r, coping: old.gardens.pool.coping, rim: old.gardens.pool.rim }, pavilion: { at: P(old.gardens.pavilion.at[0], old.gardens.pavilion.at[1]), yaw: old.gardens.pavilion.yaw } },
  harbour: { terrace: harbour.terrace, steps: [S.StepsX, S.PromenadeX], promenade: harbour.promenade, quay: harbour.quay, levels: { terrace: 0.15, promenade: S.PromenadeY, water: S.WaterY }, piers: { market: piers.market, skyport: piers.skyport }, skyportApron: old.apron },
  props: { puts: old.puts.map(p => ({ kit: p.kit, at: P(p.at[0], p.at[2]), z: p.at[1], yaw: p.yaw, scale: p.scale })), towers: old.towers.map(t => ({ kit: t.kit, at: P(t.at[0], t.at[1]), yaw: t.yaw })), monument: old.monument },
  skyrail: {
    main: { closed: true, spline: 'SkyPath.Set(route, closed: true): a closed spline through these points (x, height, y)', pts: old.skyrail.main.map(([x, h, z]) => [x, r2(h), yOf(z)]), sphereRing: { c: sph, r: old.skyrail.ringR, deg: [90, 225], height: 34 },
      planned: 'S1 (west half: Circus Road, north through Sphere Park, the loop round the Sphere). Not planned: the boulevard leg over the west promenade (x -13, y -906..154 at 24 m): its pylons stood on the promenade, which standards.skyrail.pylonZones does not allow; and the return leg (x -150..-110), which crossed the Handover\'s site. Splice\'s call (notes "Remap: plan")' },
    harbour: { pts: old.skyrail.harbour.map(([x, h, z]) => [r2(x), r2(h), yOf(z)]), station: [old.skyrail.station[0], yOf(old.skyrail.station[2])], railOverGround: old.skyrail.stationRail, planned: 'S2' },
    planned: { S1, S2, status: 'not in plan.skyrail: station banks are laid for east-west lines only (generator); S2/harbour and S2/apollo, S1/sphere stand on north-south lines. Checked with tools/plan/oldtown/transcribe.ts --with-skyrail (the transit rules pass on them; the world build stops at the banks)' },
    beam: { h: old.skyrail.pylon.beamH, halfWidth: old.skyrail.pylon.beamW, pylonStep: old.skyrail.pylon.step, footClear: old.skyrail.pylon.footClear, speed: old.skyrail.pylon.speed },
    levels: { street: 0, deck: 9.43, concourse: 24, oldMain: '24 on the boulevard, rising 1 m per ~35 m to 34 round the Sphere', oldHarbour: '21.15 over the quay (rail 22 over the promenade at -0.85), 24 round the Skyport' },
  },
};

function bump(v: string) { const p = v.split('.').map(Number); p[1]! += 1; p[2] = 0; return p.join('.'); }
writeFileSync(resolve(ROOT, OUT), JSON.stringify(plan, null, 1) + '\n');
console.log(`wrote ${OUT}: ${plan.roads.length} roads, ${plan.plots.length} plots, ${plan.blocks.length} blocks, ${plan.circus.crescents.length} crescents; ${MANUAL.length} manual decisions`);
