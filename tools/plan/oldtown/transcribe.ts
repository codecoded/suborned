// Brief 075, the remap (Sky 2026-10-28: "why can't we just remap the world around d handover using old buildings" · "And old props. So the sky trak and other things
// all planned properly but looking great"): the district plan as the old DecoKit town, transcribed into suborned.plan/1, with the Handover zone fixed inside it.
//
//   bun tools/plan/oldtown/transcribe.ts [--base briefs/075/remap/base.plan.json (the live plan as it stood before the remap, Datum's conformed blocks included)] [--out contracts/world/harbour-district.oldtown.plan.json]
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
import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { readOld, sources, OLD_DIR, type OldWall, type Seg } from './old';
import { derive, type Plan } from '../plan';
import { placements } from '../transit';
import { civic } from '../civic';
import { ctxOf } from '../../world/gen/common';
import { buildingLine, plotRing, kindOf } from '../../world/gen/edges';

const ROOT = resolve(import.meta.dir, '../../..');
const args = process.argv.slice(2), opt = (k: string, d: string) => (args.includes(k) ? args[args.indexOf(k) + 1]! : d);
const BASE = opt('--base', 'briefs/075/remap/base.plan.json'), OUT = opt('--out', 'contracts/world/harbour-district.oldtown.plan.json');
const live: any = JSON.parse(readFileSync(resolve(ROOT, BASE), 'utf8'));
// the plan already at --out (Pulse's journeys and the SpaceHQ switch are kept from it)
const PREV: any = existsSync(resolve(ROOT, OUT)) ? JSON.parse(readFileSync(resolve(ROOT, OUT), 'utf8')) : null;
// The SpaceHQ switch (brief 075 remap step 2: the form is Sky's call): 'old' = the old gold SpaceHQ (DecoKitStreetSkyline: the kit SpaceHQ × 1.35 at old (0, 450), 340 m)
// with S1's Crown Circus station beside it, the default; 'ours' = our SpaceHQ_Portal remake (252 m) with S1 through its drum and the station inside. One setting:
// plan.spacehq.form, read back from the plan at --out (or --spacehq old|ours); both forms are carried in plan.spacehq.forms.
const HQ_FORM: 'old' | 'ours' = opt('--spacehq', PREV?.spacehq?.form ?? 'old') as 'old' | 'ours';
if (HQ_FORM !== 'old' && HQ_FORM !== 'ours') throw new Error(`--spacehq: old or ours, not ${HQ_FORM}`);
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
// The old SpaceHQ (DecoKitStreetSkyline.Landmarks: Put("SpaceHQ", (0, 0, Z.y + 70), 0, scale 1.35), "grand stair foot at Z.y + 5"): the kit SpaceHQ's box (kit.json size and
// pivotOffset, the piece the old builders place) × 1.35 round the old pivot, so its ground runs from the stair foot (y 178.1, old z 384.1) to y 298 and x ±54; tip 340.2.
const HQ_OLD = (() => {
  const p = old.puts.find(q => q.kit === 'SpaceHQ')!, k = p.scale, at = P(p.at[0], p.at[2]);
  const kit = (JSON.parse(readFileSync(resolve(ROOT, 'contracts/world/kit.json'), 'utf8')).assets as any[]).find(a => a.id === 'SpaceHQ');
  const [w, h, dpt] = kit.size as number[], off = (kit.pivotOffset as number[])[1]! * k, cy = at[1] + off;
  return { put: p, at, scale: k, size: [r2(w * k), r2(h * k), r2(dpt * k)], tip: r2(h * k), ground: [r2(at[0] - w * k / 2), r2(at[0] + w * k / 2), r2(cy - dpt * k / 2), r2(cy + dpt * k / 2)] as [number, number, number, number] };
})();
const HQ_OURS_SOUTH = 204; // the remake's drum ground (r 40 round (0, 244)) starts at y 204

// The plan's street section on the Boulevard is sections.tramStreet.district: 20 kerb to kerb, 8 m pavements, so the walls stand at x ±18 exactly where the old facades stood
// (old: kerb 8, 10 m promenade). The Handover's Satellite Street (x -60, w 12) puts its east kerb at -54: the old west wall's back (-46) is 8 m inside it, on the line.
const BLV = { w: live.roads.find((r: any) => r.id === 'boulevard').w as number }; // 20
const KERB = BLV.w / 2; // 10
// Crown Avenue at the old 16 m (brief 075 plan-match, briefs/075/oldbuild/plan-patch-applied.json op 105): it carries no tram, so it takes the old-built kerbs (x ±8, the old stones)
// and its pavements are 10 m to the old walls (x ±18); the Boulevard keeps 20 because T1's stop islands need 10 m a half (sections.tramStreet.district.island: edge 3.2 + island 3.0
// + 0.5 shy + passing lane 3.3), and two of its stops (circus, orbit) stand on it.
const AV_W = 16, AV_KERB = AV_W / 2;
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
node('n_avenue', [0, north + 4 - AV_KERB]); // its end kerb on the forecourt's edge (y 144), as at 20 m (134 + 10)
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
  { id: 'crown-avenue', name: 'Crown Avenue', class: 'access', w: AV_W, path: ['c_n', 'n_avenue'], why: `the old avenue north of the circus to the forecourt (old z 236 to ${S.NorthFacade}), at its old-built 16 m (kerbs x ±8, the old stones; 10 m pavements to the old walls at ±18; no tram, so not the Boulevard's 20 m tram section): the arch spans it, the HQ closes it` },
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
// The Sphere at the old ×1 (brief 075 swap): the old SphereStation as built is 143.3 × 132 × 188.2 m, round in plan (legs to r 67 at the ground, service towers to r 72.4);
// the disc r 72.5 is the ground it stands on (fit foot 145 ≥ 143.3), top 188.4 = 188.2 on the 0.18 pavement (rivalMax 200; more than 150 m from the quay, so the harbour band's
// 108 does not bind). S1_EAST: S1's way round its east side (see S1_OLD); PE: the park's east edge where the line runs on x S1_EAST.x (Sphere Park and the north lawn take
// 4 m of west-wall-n's back yard, x -54..-50: the old W4 wall's back is at x -46, so no old building moves).
const SPHERE = { r: 72.5, top: 188.4, scale: 1, kitFit: 'brief 075 swap (Latch, cc903ae0): the old builders stand the SphereStation at ×1 (DecoKitStreetSkyline Put("SphereStation", (-130, 0, 330), 20), 188 m), so the plan does too (Sky 2026-10-29: every landmark at its old height): the kit as built, 143.3 × 132 × 188.2 m (six lattice legs from r 58–67 at the ground to r 53–61 at 24 m, a ring girder r 54 at 34 m, service towers to r 72.4), × 1.0. r 72.5 = the ground it stands on (fit foot 145 ≥ 143.3); top 188.4 = 188.2 on the 0.18 pavement, under rivalMax 200 and more than 150 m from the quay (the harbour band does not bind). Was × 0.425 (top 80, r 31; Aster review 2026-10-12)' };
const S1_EAST = { x: -52, c2: 156, c3: 198, station: 253 }, PE = -50;
plot('boulevard-west', 'commercial', 'boulevard', [W0, W1, sunset[0], -192], { name: 'Boulevard west (old W1; the west lane a passage)' });
{ // old W2 north of lamplight-south-e: the wall plot (x -54..-10) to the gate, the notch's arc sampled, and a 12 m strip west to x -94 under the gardens (today's lamplight-east edge)
  const arc: V[] = []; for (let k = 0; k <= 16; k++) { const a = Math.atan2(-Math.sqrt(circus.crescent ** 2 - W1 ** 2), W1) + (k / 16) * (Math.atan2(-9, -Math.sqrt(circus.crescent ** 2 - 81)) - Math.atan2(-Math.sqrt(circus.crescent ** 2 - W1 ** 2), W1)); arc.push([r2(circus.crescent * Math.cos(a)), r2(circus.crescent * Math.sin(a))]); }
  plots.push({ id: 'lamplight-east', name: 'Boulevard west (old W2, the civic hall)', use: 'commercial', area: 'boulevard', poly: [[-94, -126], [W1, -126], ...arc, [W0, -9], [W0, -114], [-94, -114]] }); }
plot('west-wall-n', 'commercial', 'crown', [HQ_FORM === 'old' ? PE : W0, -AV_KERB, 9, north + 4], { name: 'Crown Avenue west (old W4)', notch });
plot('east-wall-s', 'commercial', 'boulevard', [E0, E1, sunset[1], market[0]], { name: 'Boulevard east (old E1)' });
plot('east-wall', 'commercial', 'boulevard', [E0, E1, market[1], -9], { name: 'Boulevard east (old E2, E3: the east lane a passage)', notch });
plot('east-wall-n', 'commercial', 'crown', [AV_KERB, E1, 9, north + 4], { name: 'Crown Avenue east (old E4)', notch });
manual('The east lane (old z 134–140) and the west lane (old z 2–8, on the line of Starline Street) are passages through one plot each (east-wall: old E2 and E3; west-wall: old W1 and W2); old Sunset Way west of the boulevard is the west wall plot\'s south flags, so the Handover\'s streets never join the boulevard\'s');
// Orbit Gardens and the west terrace (old DecoKitGardens; DecoKitCityBlocks' garden-square wall at x -110), from the zone's edge (y -148) north
plot('gardens-s', 'park', 'gardens', [gardensX[0], W0, -114, -9], { name: 'Orbit Gardens (canal)' });
plot('gardens-n', 'park', 'gardens', [-156, HQ_FORM === 'old' ? PE : W0, 9, 50], { name: 'Orbit Gardens (north lawn, and the old west terrace\'s north end)' });
plot('terrace-s', 'residential', 'gardens', [-156, gardensX[0], -114, -9], { name: 'Garden terrace (old west terrace)' });
manual(`Orbit Gardens are trimmed at the Handover zone's edge (y -148, old z 58): the old lawns, canal and west terrace south of it (old z -40..58) are the Handover zone's; the canal runs from y -148 to ${yOf(old.gardens.canal.to)}, the pool at (${old.gardens.pool.c[0]}, 0), the pavilion at (${old.gardens.pavilion.at[0]}, ${yOf(old.gardens.pavilion.at[1])})`);
manual('The old west terrace (one 286 m wall facing the gardens at x -110, its back to the procedural massing) is one 28 m deep block south of the Gardens Gate at x -138..-110 (terrace-s, fronting Lamplight Lane, the garden walk at its back; the grammar wants 28 back to back, so its back faces Orbit Row); its 37 m north of the gate is lawn (gardens-n), where S1 turns north');
// Sphere Park: the old sphere station's ground and the sky rail's loop round it; the park the old town left north of the gardens
const swx = r2(old.skyrail.sphere[0]! - old.skyrail.ringR); // the sphere walk on the old ring's west point (x -218), under S1's north leg and its station
// The SpaceHQ switch's ground on Sphere Park (brief 075 remap step 2). 'old': Crown Walk, a 12 m walk on y 253 (brief 075 swap; was 238) from the west of S1's north leg (x -115) through Crown
// Circus station (x -95) east to the old HQ's west face (x -54): the station stands on it, not on the park (N-P2), and fronts it (W-3); the station's banks stand on the park
// either side of the line, S1's pylons on the park north and south of the walk. Sphere Park keeps the rest, a slot cut from its east edge for the walk (to its west end cap, x -121). 'ours': Crown Walk
// runs on the HQ's axis (y 244) from Sphere Park through the drum's ground hall (between the legs, y 224..264) to the harbour quay (x 48), under S1; Sphere Park stays whole.
const CW = { x0: -115, w: 12, y: S1_EAST.station }; // on Crown Circus station (brief 075 swap: y 253, north of the Sphere; was the HQ's middle, 238)
const crownWalk = HQ_FORM === 'old'
  ? { id: 'crown-walk', name: 'Crown Walk', kind: 'walk', w: CW.w, pts: [[CW.x0, CW.y], [W0, CW.y]] }
  : { id: 'crown-walk', name: 'Crown Walk', kind: 'walk', w: CW.w, pts: [[W0 + CW.w / 2, 244], [E1, 244]] };
plots.push({ id: 'sphere-park', name: 'Sphere Park', use: 'park', area: 'sphere', poly: [[-328, 9], [-180, 9], [-180, 30], [-156, 30], [-156, 50], ...(HQ_FORM === 'old' ? [[PE, 50], [PE, north + 4]] : []), [W0, HQ_FORM === 'old' ? north + 4 : 50],
  ...(HQ_FORM === 'old' ? [[W0, CW.y - CW.w / 2], [CW.x0 - CW.w / 2, CW.y - CW.w / 2], [CW.x0 - CW.w / 2, CW.y + CW.w / 2], [W0, CW.y + CW.w / 2]] : []), [W0, sheetY[1] + 1], [-328, sheetY[1] + 1]] });
plot('west-terraces', 'residential', 'westend', [-244, -164, -114, -45], { name: 'West Terraces' });
plot('gardens-forecourt', 'open', 'westend', [-244, -164, -45, -9], { name: 'Gardens Forecourt (S1 Orbit Gardens station\'s south banks)' });
plot('sphere-gate', 'open', 'sphere', [-180, -156, 9, 30], { name: 'Sphere Park gate (the garden walk\'s crossing of Circus Road lands here)' });
plot('western-park-n', 'park', 'westrow', [sheetX[0], -352, 9, 268], { name: 'Western Park (run on north to Crown Road)' });
// Western Park south of Circus Road: West Row (x -422..-410) runs through it; S1's Western Park station banks (x -395..-355) stand on its east part
plot('western-park-s', 'park', 'westrow', [-410, -352, -60, -9], { name: 'Western Park' });
// Southside, east of the Handover zone (old DecoKitSouthside), the boulevard through it
plot('n1', 'mixed', 'southside', [W0, W1, -306, sunset[0]], { name: 'Constellation (old N1, west half)' });
plot('n2', 'mixed', 'southside', [E0, E1, -306, sunset[0]], { name: 'Constellation (old N2)' });
plot('orbit-square-w', 'open', 'southside', [W0, W1, -356, -306], { name: 'Orbit Square' });
plot('orbit-square-e', 'open', 'southside', [E0, E1, -356, -306], { name: 'Orbit Square' });
// ss1 (brief 075 plan-match, plan-patch op 12): the old-built town has no building here (old: the rocket street's mouth off the square's south side, old z -150, and RL1 beside it, both cut by
// Satellite and Market Streets), so it is open ground, Orbit Square's south end, not a block; the old SS1 stood across Market Street's line and shifts onto ss3 (below)
plot('ss1', 'open', 'southside', [W0, W1, -401, -356], { name: 'Orbit Square south (old: the rocket street\'s mouth)' });
plot('starline-green', 'park', 'southside', [E0, E1, -401, -356], { name: 'Starline Green' });
plot('ss2', 'mixed', 'southside', [E0, E1, -466, -419], { name: 'old SS2' });
plot('ss4', 'mixed', 'southside', [E0, E1, -548, -478], { name: 'old SS4' });
plot('ss3', 'mixed', 'southside', [W0, W1, -548, -419], { name: 'old SS3 / SS5' });
plot('lantern-court', 'residential', 'lantern', [-180, -66, -484, -419]);
plot('lantern-west', 'residential', 'lantern', [-328, -192, -548, -419]);
plot('lantern-south', 'residential', 'lantern', [-180, -66, -548, -496]);
plot('crown-forecourt', 'open', 'crown', [W0, E1, north + 4, HQ_FORM === 'old' ? HQ_OLD.ground[2] : HQ_OURS_SOUTH], { name: 'Crown Forecourt' });

// ------------------------------------------------------------------ West Row: the strip between the sheet's west edge and Sunset Way (brief 075 remap follow-up, Aster:
// "no bare sand is left in the district outside water, parks and plazas"). The old town had procedural massing west of the gardens (DecoKitCityMass); the first remap left
// the strip x -460..-352 as the beyond hatch, which the world draws as bare ground. Filled by rule, in the old grammar, clear of the Handover zone (x ≥ -226) by 126 m:
//  - an access street (West Row) from Circus Road south to the sheet's south plot line, the strip's only way in (Sunset Way and Crown Road are primaries: R-H1), gated at Circus Road;
//    its line puts a 28 m block (8 + 6k + 8) on the sheet-edge side and the deepest grammar block (open court from 40) on the Sunset Way side;
//  - 8 m lanes (walks, the Garden Walk's section: the old 6 m lanes at the walk standard's minimum) across the strip, opposite every street that meets Sunset Way from the
//    east and every seam between two built plots there, and at the midpoint of any longer span than the plan's longest old-style block side (WR.maxSpan);
//  - each plot between lanes is fitted to its building line by the grammar (fit, below); old classes at the remap's heights are the generator's (Splice);
//  - north of Circus Road the strip is Western Park, run on to Crown Road (S1's Western Park station already stands in it; no access street can reach a block there
//    without a dead end off a primary).
const WR = (() => {
  const sw = byId(roads, 'sunset-way'), xw = r2(live.nodes[sw.path[1]][0] - sw.w / 2), x0 = sheetX[0]; // Sunset Way's west kerb (-352), the sheet edge (-460)
  const acc = live.standards.streetscape.sections.access.pavement as number, prim = live.standards.streetscape.sections.primary.pavement as number, edgeSet = 3, band = 28, w = 12;
  // the street's centre: the sheet-edge plot takes one 28 m block behind the sheet edge's 3 m setback and the access pavement; the rest goes to the Sunset Way side
  const cx = Math.round(x0 + edgeSet + band + acc + w / 2); // -416
  const yS = -548, yN = -60, laneW = 8, maxSpan = 140; // the district's south plot line (lantern-west, ss3), Western Park's south edge
  // seams across Sunset Way: the shared edge of two built plots on its east side (a lane opposite a street that meets Sunset Way would land its crossing in that street's
  // mouth, so street axes are not seams: Market Street's own crossings serve)
  const eastX = r2(live.nodes[sw.path[1]][0] + sw.w / 2), seams = new Set<number>();
  const eastPlots = plots.filter(p => p.rect && Math.abs(p.rect[0] - eastX) < .01 && !['park', 'open', 'square'].includes(p.use));
  for (const a of eastPlots) for (const b of eastPlots) if (a !== b && Math.abs(a.rect[3] - b.rect[2]) < .01) seams.add(a.rect[3]);
  const cuts = [...seams].filter(y => y > yS + laneW && y < yN - laneW).sort((a, b) => a - b);
  // spans between the seams, each cut into the fewest equal blocks no longer than maxSpan
  const spans: [number, number][] = [], lanes: number[] = []; let lo = yS;
  for (const y of [...cuts, yN]) {
    const hi = y === yN ? yN : y - laneW / 2, n = Math.ceil((hi - lo) / maxSpan), L = (hi - lo - (n - 1) * laneW) / n;
    for (let k = 0; k < n; k++) { const a = r2(lo + k * (L + laneW)), b = r2(a + L); spans.push([a, b]); if (k < n - 1) lanes.push(r2(b + laneW / 2)); }
    if (y !== yN) lanes.push(y);
    lo = y + laneW / 2;
  }
  return { x0, xw, cx, w, yS, yN, laneW, maxSpan, cuts: lanes.sort((a, b) => a - b), spans, seams: [...seams].sort((a, b) => a - b), circusKerb: r2(-byId(roads, 'circus-road').w / 2) };
})();
node('w_row', [WR.cx, 0]); node('w_row_s', [WR.cx, WR.yS]);
{ const cr = roads.find(r => r.id === 'circus-road')!; cr.path = [cr.path[0], 'w_row', ...cr.path.slice(1)]; }
roads.push({ id: 'west-row', name: 'West Row', class: 'access', w: WR.w, path: ['w_row', 'w_row_s'], why: `the strip west of Sunset Way (old: procedural massing, DecoKitCityMass) reached from Circus Road (district), its only distributor that does not run along a primary: x ${WR.cx} puts a 28 m block on the sheet-edge side and the deepest grammar block on Sunset Way's; it ends at the district's south plot line (y ${WR.yS}), as Crown Avenue ends at its forecourt` });
gates.push({ id: 'west-row-gate', name: 'West Row', node: 'w_row', road: 'west-row', area: 'westrow' });
areas.push({ id: 'westrow', name: 'West Row', rect: [WR.x0, WR.xw, WR.yS, 268], label: [-406, -300] });
for (const a of areas) if (a.id === 'westend' || a.id === 'sphere') a.rect = [WR.xw, ...a.rect.slice(1)];
const wrLanes = WR.cuts.map((y, i) => ({ id: `west-row-lane-${i + 1}`, name: `West Row lane ${i + 1}`, kind: 'walk', w: WR.laneW, pts: [[WR.x0, y], [WR.xw, y]] }));
const wrPlots: string[] = [];
WR.spans.forEach(([a, b], i) => {
  const top = i === WR.spans.length - 1 ? WR.circusKerb : b; // the last west plot runs on to Circus Road (the corner block), the east one stops at Western Park (S1's station banks)
  plot(`west-row-w${i + 1}`, 'residential', 'westrow', [WR.x0, r2(WR.cx - WR.w / 2), a, top], { name: `West Row west ${i + 1}` });
  plot(`west-row-e${i + 1}`, 'mixed', 'westrow', [r2(WR.cx + WR.w / 2), WR.xw, a, b], { name: `West Row east ${i + 1} (Sunset Way)` });
  wrPlots.push(`west-row-w${i + 1}`, `west-row-e${i + 1}`);
});
manual(`West Row (the strip x ${WR.x0}..${WR.xw} west of Sunset Way, the first remap's beyond hatch): an access street on x ${WR.cx} from Circus Road (gate west-row-gate) south to y ${WR.yS}; 8 m lanes at y ${WR.cuts.join(', ')} (opposite the seams of Sunset Way's east-side plots ${WR.seams.join(', ')}, and each span cut into the fewest equal blocks of at most ${WR.maxSpan} m); ${WR.spans.length} plots each side fitted by the grammar (west residential, east mixed), the last west plot running on to Circus Road as the corner block; Western Park (east of West Row, where S1's station banks stand) runs on north to Crown Road. The beyond hatch keeps only the sheet's slivers`);
manual('Southside trimmed: the Handover zone is where the old world sited the Handover too (its N4 and W2), and Satellite Street and the Records Office stand on old W1, N1\'s west half and the Rocket Lane diagonal (RL1, RL2, RB1), which go; Orbit Square keeps its east and west halves either side of the boulevard, which runs through it to the south edge (old Comet Row ended on the square)');

// ------------------------------------------------------------------ foot paths, water, quay
// brief 075 review (Sky 2026-10-28: "we need the skytrak to get here - it's unreachable right now. that's datums job to map the world to make sure everywhere has a real
// structure"): the Skyport Pier's head widens to the Skyport apron (old DecoKitHarbour: the tower stood on its apron at the pier head, skyportApron 22), x quay..140 by
// ±30, which carries S2's Skyport station banks either side of the line (x 114) and runs 8 m onto the tower's caisson (its drum edge at x 132): the tower's way in on foot.
// The Market Pier runs 6 m onto the liner berth's caisson (edge x 248), its way in (RCH-1: a joint of at least 9 m²).
const APRON = { x1: SK - 30, y: 30 }, MPIER_X1 = 254;
// the Market Pier's landing stage at its root (x quay..140, y -196..-106: the banks clear of the pier walk), carrying S2's Harbour station banks either side of the line (x 117)
const STAGE = { x1: 140, y: [-196, -106] as V };
const foot = [
  byId(live.foot, 'garden-walk'), ...wrLanes, crownWalk,
  { id: 'gardens-gate', name: 'Gardens Gate', kind: 'mall', w: 18, pts: [[-146, 0], [-circus.crescent, 0]] },
  { id: 'crown-parade', name: 'Crown Parade', kind: 'walk', w: 18, pts: [[circus.crescent, 0], [harbour.quay, 0]] },
  { id: 'skyport-pier', name: 'Skyport Pier', kind: 'walk', w: 12, pts: [[harbour.quay, 0], [APRON.x1, 0]] },
  { id: 'harbour-promenade', name: 'Harbour Promenade', kind: 'walk', w: harbour.promenade[1] - harbour.promenade[0], pts: [[r2((harbour.promenade[0] + harbour.promenade[1]) / 2), sheetY[0]], [r2((harbour.promenade[0] + harbour.promenade[1]) / 2), north + 4]] },
  { id: 'market-pier', name: 'Market Pier', kind: 'walk', w: piers.market[1] - piers.market[0], pts: [[harbour.quay, r2((piers.market[0] + piers.market[1]) / 2)], [MPIER_X1, r2((piers.market[0] + piers.market[1]) / 2)]] },
];
manual('The Gardens Gate (old: the circus\'s west gate promenade to the pool, 12 m) is an 18 m mall from Orbit Row to the circus walk ring: S1\'s pylons stand on it where it leaves Circus Road');
manual('Crown Parade is an 18 m walk on the circus axis to the quay (old: the wedge between the crescents, 24 m at the circus widening to 56 at the terrace): the crescents\' 14 m bands, which end ±12 m off the axis, need the plots to reach ±9');
const qy = harbour.quay;
const quay = [
  [[eb, sheetY[0]], [qy, sheetY[0]], [qy, sheetY[1]], [eb + 2, sheetY[1]], [eb + 2, north + 4], [eb, north + 4]],
  [[qy, -APRON.y], [APRON.x1, -APRON.y], [APRON.x1, APRON.y], [qy, APRON.y]],
  [[qy, STAGE.y[0]], [STAGE.x1, STAGE.y[0]], [STAGE.x1, piers.market[0]], [MPIER_X1, piers.market[0]], [MPIER_X1, piers.market[1]], [STAGE.x1, piers.market[1]], [STAGE.x1, STAGE.y[1]], [qy, STAGE.y[1]]],
];
const water = [[[qy, sheetY[0]], [sheetX[1], sheetY[0]], [sheetX[1], sheetY[1]], [qy, sheetY[1]], [qy, APRON.y], [APRON.x1, APRON.y], [APRON.x1, -APRON.y], [qy, -APRON.y], [qy, STAGE.y[1]], [STAGE.x1, STAGE.y[1]], [STAGE.x1, piers.market[1]], [MPIER_X1, piers.market[1]], [MPIER_X1, piers.market[0]], [STAGE.x1, piers.market[0]], [STAGE.x1, STAGE.y[0]], [qy, STAGE.y[0]]]];
manual(`The Skyport apron: the Skyport Pier's head widens to x ${qy}–${APRON.x1} by ±${APRON.y} (old: the tower on its 22 m apron at the pier head), carrying S2's Skyport station banks and running 8 m onto the tower's caisson (drum edge x ${SK - 38}), the tower's way in on foot; the Market Pier widens at its root to a landing stage (x ${qy}–${STAGE.x1}, y ${STAGE.y.join('..')}) for S2's Harbour station banks and runs on to x ${MPIER_X1}, 6 m onto the liner berth's caisson (edge x 248), its way in (RCH-1, brief 075 review)`);
manual(`The harbour: the upper terrace (x ${harbour.terrace.join('–')}), steps and promenade (to ${qy}) are one quay strip down the whole sheet (old z -300..380, run on to both edges); the sea from x ${qy}; the Skyport Pier on the axis to its apron, the Market Pier (old z ${S['MarketPierZ.x']}–${S['MarketPierZ.y']}) to the liner berth`);
// the beyond hatch: the sheet's slivers only (north of Crown Road at the west edge, north of the forecourt, the south edge); West Row and Western Park took the west strip
const beyond = [[[sheetX[0], 292], [-352, 292], [-352, sheetY[1]], [sheetX[0], sheetY[1]]], [[W0, 288], [qy, 288], [qy, sheetY[1]], [W0, sheetY[1]]], [[sheetX[0], sheetY[0]], [qy, sheetY[0]], [qy, -552], [sheetX[0], -552]]];

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
const S1_STATIONS = [
  { id: 'west-end', name: 'Western Park', path: 'trunk', at: [-395, 0], length: 50, interchange: 'T1/terraces', fronts: 'circus-road', kit: 'SkyRailStation_Twin' },
  { id: 'gardens', name: 'Orbit Gardens', path: 'trunk', at: [-191, 0], length: 50, fronts: 'circus-road', kit: 'SkyRailStation_Twin' },
];
// The SpaceHQ switch's two S1s (brief 075 remap step 2). 'old': the line as before (north on x -95 past the Sphere, off the sheet), and a third station, Crown Circus,
// on its north leg beside the old HQ (platforms y 228..278 north of the Sphere, x -95; brief 075 swap, was 213..263: its banks, stairs and lifts on Sphere Park, the east bank tower 26 m off the
// HQ's west face at x -54), north–south as S2's. 'ours': north on x -95, east on the HQ's axis (y 244) through the remake's drum, the station inside it at the concourse
// (the drum's legs stand either side of the line, y 204–224 and 264–284), and on east over the quay and the water off the sheet.
// Brief 075 swap (Latch's old-built town, cc903ae0): the old builders stand the SphereStation at ×1 (legs r 58–67 at the ground, 53–61 at the beam, service towers to r 72.4),
// and S1's north leg on x -95 ran 35 m from its centre, through the leg ring. 'old' S1 now goes round the Sphere's east side as the old main loop circled it: east along
// Circus Road's line past Orbit Gardens station, north (R 70) over the north lawn onto x -52 (78 m off the Sphere's centre: pylons keep pylonClear.landmark outside
// its disc), then an S of two R 70 curves (corners (-52, 156) and (-95, 198)) between the Sphere and the west gate tower's shaft onto x -95, north of the
// Sphere, where Crown Circus station stands on the straight (y 253); off the sheet north as before.
const S1_OLD = {
  id: 'S1', name: 'Sphere Line', beyond: 'west off-sheet along Circus Road to the West End; north off-sheet past the Sphere toward the old main loop\'s north arc',
  paths: [{ id: 'trunk', role: 'line', double: true, pts: [[sheetX[0], 0], [S1_EAST.x, 0, 70], [S1_EAST.x, S1_EAST.c2, 70], [-95, S1_EAST.c3, 70], [-95, sheetY[1]]] }],
  order: ['trunk'],
  stations: [...S1_STATIONS,
    { id: 'crown-circus', name: 'Crown Circus', path: 'trunk', at: [-95, S1_EAST.station], length: 50, fronts: 'crown-walk', kit: 'SkyRailStation_Twin',
      kitWhy: `open-air on S1's north leg beside the old SpaceHQ (its west face x ${HQ_OLD.ground[0]}), north of the Sphere, north–south; banks (stairs, escalators, lifts to the deck 9.43 and the concourse 24) on Sphere Park either side of the line; the old HQ is the old builders' piece and carries no beam` }],
};
const S1_OURS = {
  id: 'S1', name: 'Sphere Line', beyond: 'west off-sheet along Circus Road to the West End; east off-sheet over the harbour north of the Skyport',
  paths: [{ id: 'trunk', role: 'line', double: true, pts: [[sheetX[0], 0], [-95, 0, 70], [-95, 244, 70], [sheetX[1], 244]] }],
  order: ['trunk'],
  stations: [...S1_STATIONS,
    { id: 'crown-circus', name: 'Crown Circus', path: 'trunk', at: [0, 244], length: 50, fronts: 'crown-walk', in: 'signatures/space-hq', kit: 'SkyRailStation_Twin',
      kitWhy: 'inside the SpaceHQ_Portal drum at the concourse (24), east–west on the HQ\'s axis; the drum carries the beam (portal openings at x ±40), the banks stand in the ground hall between the legs' }],
};
const S0 = HQ_FORM === 'old' ? S1_OLD : S1_OURS;
// S2 in the plan, the Harbour Line (brief 075 review, Sky 2026-10-28: "we need the skytrak to get here - it's unreachable right now"): the old harbour line's sweep out
// to the Skyport on pylons (DecoKitSkyRail.HarbourRoute: up the quay, round the Skyport at R 48, back over the sea at x 122), laid to the plan's rules. From the south edge
// over the water on x 117 (the old sea leg's line, 21 m off the quay), Harbour station on the Market Pier's landing stage (y -140; T1's Crown Circus stop 136 m: the
// interchange); the double track ends at y -105 and the terminal loop (R 40) goes round the Skyport Tower: east under its south face (y -60), north up its east side
// (x 225), west over the water north of it (y 75) and south down x 114 through the Skyport station on the Skyport apron (platforms y -25..25, banks on the apron either
// side) to the track's end. The beam clears the tower's faces (x 144..196, y -25..25 at × 0.94) by 29 m or more and the liner berth's caisson by 50 m. The apron runs
// onto the tower's caisson (its way in on foot). Nothing of it is in the Handover zone (x -236..18, y -428..-176) or nearer it than the quay (x 96): the Handover's escape
// walks keep their stations (W-1). Tried first: up the harbour terrace (x 64.5) with Apollo Quay at y -478 (T1/Orbit Square); the terrace (x 46..80 between the east blocks
// and the promenade walk) is 1.5 m too narrow for the generator's north-south bank kit (a phone booth landed in the promenade walk, SF-1; notes "Remap: Skyport and reach").
const HXS = 117, HY = -140, J = -105;
const SH = {
  id: 'S2', name: 'Harbour Line', beyond: 'south off-sheet over the harbour; the terminal loop round the Skyport Tower',
  paths: [
    { id: 'trunk', role: 'line', double: true, pts: [[HXS, sheetY[0]], [HXS, J]] },
    { id: 'loop', role: 'loop', pts: [[HXS + 3, J], [HXS + 3, -60, 40], [SK + 55, -60, 40], [SK + 55, 75, 40], [HXS - 3, 75, 40], [HXS - 3, J]] },
  ],
  order: ['trunk', 'loop'],
  stations: [
    { id: 'harbour', name: 'Harbour', path: 'trunk', at: [HXS, HY], length: 50, interchange: 'T1/circus', fronts: 'market-pier', kit: 'SkyRailStation_Twin', kitWhy: 'open-air over the Market Pier\'s landing stage (x 96–140, y -196..-106), north–south; banks on the stage either side; the pier walk runs under it to the liner berth' },
    { id: 'skyport', name: 'Skyport', path: 'loop', at: [HXS - 3, 0], length: 50, fronts: 'skyport-pier', kit: 'SkyRailStation_Twin', kitWhy: 'open-air on the loop\'s west leg over the Skyport apron (x 96–140, y ±30), north–south; banks on the apron either side; the Skyport Pier walk runs on through the apron onto the tower\'s caisson' },
  ],
};
manual('S2 (Harbour Line, brief 075 review: the Skyport reachable): from the south edge over the water on x 117 (the old sea leg), Harbour station on the Market Pier\'s landing stage (y -140, the T1/Crown Circus interchange, 136 m), and a terminal loop (R 40) round the Skyport Tower with the Skyport station on its west leg over the Skyport apron: the old harbour line\'s sweep round the Skyport on pylons, round the old Skyport_Tower as the old reel (not the portal tower)');

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
manual('The plan\'s S1 (Sphere Line) is not an old line: the generator lays station banks only for east-west lines on y 0 (the circus axis), so S1 runs from the West End along Circus Road (Western Park and Orbit Gardens stations), north past the Sphere\'s east side (x -95) and off the sheet; it joins T1 at Sunset Terraces. S1 does not reach the Skyport (a station there would have to be 200–500 m from another on y 0, and the circus\'s 14 m island takes no pylon): S2 does (brief 075 review). The old lines stay planned data:');
manual('The old lines are not in plan.skyrail in this draft: the generator lays station banks for east-west lines only and the old lines run north-south (the main loop over the boulevard\'s west promenade, x -13; the harbour line on the quay, x 88). S1 (Sphere Line: Circus Road, north on x -218, a loop by the Sphere) and S2 (Harbour Line: up the quay, into the Skyport Tower, round the pad) are planned in oldtown.skyrail.planned with radii, spans and stations, ready for plan.skyrail once the generator lays north-south banks (Splice). The old boulevard leg also needs a pylon zone on the promenade (pylonZones has none) and is drawn as data only');
manual(`The harbour sky rail (planned) keeps the old quay leg (x ${hx}) and Harbour station (old (88, 110) → plan (88, -96), here 44 m south at -140 so the station spacing to the Skyport holds, 200 m); the old loop round the Skyport (R 48) becomes the plan's line into the Skyport Tower's portal (the tower 15 m east of the old Skyport, x 170 → ${SK}, so the R 70 turn off the quay ends before the portal) and the loop round the pad beyond; the old sea leg (x 122) is not built (the line ends on its loop)`);

// ------------------------------------------------------------------ landmarks, signatures, megastructures (the live entries, moved to the old positions)
const put = (kit: string) => old.puts.filter(p => p.kit === kit);
const mv = (p: number[], d: V) => [r2(p[0]! + d[0]), r2(p[1]! + d[1])];
const mvRect = (r: number[], d: V) => [r2(r[0]! + d[0]), r2(r[1]! + d[0]), r2(r[2]! + d[1]), r2(r[3]! + d[1])];
const landmarks: any[] = [];
{ const l = byId(live.landmarks, 'monument'); l.c = circus.c; l.r = circus.island; landmarks.push(l); }
{ const l = byId(live.landmarks, 'sphere'); l.c = sph; l.oldPut = put('SphereStation')[0]; Object.assign(l, SPHERE); landmarks.push(l);
  manual(`The Sphere stands at the old SphereStation (old (-130, 330) → plan (${sph}), yaw 20) at the old ×1 (r ${SPHERE.r}, top ${SPHERE.top}; brief 075 swap: the old builders stand it at ×1, and Sky's heights rule keeps every landmark at its old height); was ×0.425 (top 80, Aster review 2026-10-12)`); }
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
  landmarks.push({ id, name: `Avenue gate tower ${id === 'disc-west' ? 'west' : 'east'}`, c: at, r: 20.5, kit: 'DiscTower', top: 156.6, scale: 1, oldPut: p,
    kitFit: "the old DecoKitStreetSkyline pair framing the arch (x ±44, z 372): the old stood DiscTower × 1.0 (156.6); here the same, the kit DiscTower as built (40.2 × 156.6 × 40.2, × 1.0: inside the disc Ø 41 and the top 156.6, tip 156.8 on the 0.2 landmark base), under rivalMax 200; within 150 m of the quay, so it passes the harbour icon (108) by standards.skyline.harbour.exceptions (brief 075 remap, Aster: back to the old reel); r 20.5 ≥ 40.2 / 2; no crown type (SK-2 has no landmark pairs; the pair is matched)" });
}
manual("The gate towers (old DiscTower pair at x ±44, z 372) stand at the old 156.6 (× 1.0, tip 156.8): within 150 m of the quay, so standards.skyline.harbour.exceptions names the pair (brief 075 remap, Aster); every other tip there keeps 108");
const arch = put('ParabolicArch')[0]!;
{ const v = byId(live.landmarks, 'crown-vista').gateway; v.c = P(arch.at[0], arch.at[2]); v.feet = v.feet.map((f: number[]) => [f[0], f[1], r2(f[2]! - 53 + v.c[1]), r2(f[3]! - 53 + v.c[1])]);
  manual("The ParabolicArch across Crown Avenue (old (0, 250) → plan (0, 44)) stands as landmarks[disc-west].gateway: its plinths (x ±14–17, y 41–47) in the avenue's frontage zone, which here runs to the old walls' building line at |x| 18 (K-1, Splice's measure, briefs/075/notes.md)");
  const { status: _s, ...gw } = v; void _s; landmarks.find(l => l.id === 'disc-west').gateway = { ...gw, ...{"name": "Crown Avenue arch", "piece": "a regen of ParabolicArch, gold as the pier's (DK_Crown_Gold): outer span 34.0 (foot to foot), foot centres ±15.5, clear 28.0 between the plinths, plinths 3.0 across × 6.0 along, depth ≤ 11.6, crest 95.2 and top 105.3 (brief 075 golden vista 2: the vista 1 piece, crest 68 and top 75.2, × 1.4 in height only). At (0, 44) the frontage zone is |x| 11.5–18 (6.5 m: Crown Avenue at its old-built 16 m, brief 075 plan-match; 13.5–18 at 20 m), so the plinths at |x| 14–17 stand 2.5 m off the walk's edge and 1 m off the building line; the piece is not widened for it", "whyNot38": "the adopted ParabolicArch is 38.5 m across (foot centres ±16, plinths 6.4 → |x| 12.8–19.2): at (0, 44) its feet stand 1.2 m past the old walls' building line (|x| 18) (at the 20 m avenue they also stood 0.7 m on the clear walk, which ran to |x| 13.5; at the old 16 m it runs to |x| 11.5). At its tag floor × 0.9 (34.65, plinths |x| 11.5–17.3) the feet stand in the frontage zone (|x| 11.5–18) since the avenue is 16 m (brief 075 plan-match): the old piece × 0.9 would now fit, a piece choice for Splice and Vela; the regen (plinths |x| 14–17) stands until they make it", "siteWhy": "brief 075 remap: the old ParabolicArch's place across Crown Avenue (old (0, 250) → plan (0, 44)), just past the circus mouth, as the old reel. Splice's measure at the site (briefs/075/notes.md, K-1's own test, both sides alike, every y 38–50): Crown Avenue's kerb face at |x| 8 (its old-built 16 m since the brief 075 plan-match; |x| 10 when Splice measured it at 20 m), kerb and walk zones to |x| 11.5 (3.5 m), the 6.5 m access pavement to |x| 14.5, then the 3.5 m paved setback to the old walls' building line at |x| 18 (block runs facing access:crown-avenue). So the frontage zone at y 41–47 is |x| 11.5–18 and the plinths (|x| 14–17, y 41–47) stand inside it, with the old walls behind them; the arch's depth (≤ 11.64, y 38.2–49.8) stays on that same avenue section. Outside the play-street cap", "keep": "the generator: the plinths only in the frontage zone (never the walk, kerb zone or carriageway); no doorway, spill or vending inside 1.5 m of a plinth; no palm or lamp whose crown or head comes within 1 m of a leg (the leg is at about x ±14.8 at 8 m up); the shopfronts behind the plinths (the old walls at |x| 18, y 41–47) keep their doors (a door moves along its facade, never under a plinth)"} }; }
manual('The old SkyBridge between the gate towers (60 m up) is in oldtown.props, not placed: no plan structure carries it yet');
// SpaceHQ at the old position closing the boulevard (old (0, 450) × 1.35): the plan's regenerated SpaceHQ_Portal, its drum without a beam (the plan's portal reads a line on y 0)
const hqDelta: V = (() => { const p = put('SpaceHQ')[0]!, at = P(p.at[0], p.at[2]); return [r2(at[0] - 96), r2(at[1] - 0)]; })();
const signatures: any[] = [];
const HQ_FORMS: Record<'old' | 'ours', any> = { old: null, ours: null };
{ const s = byId(live.signatures, 'space-hq'); s.c = mv(s.c, hqDelta); s.ground = mvRect(s.ground, hqDelta); s.legs = s.legs.map((l: number[]) => mvRect(l, hqDelta)); s.areas = ['crown'];
  s.spans = { plots: [], foot: [] }; s.portalWas = s.portal; delete s.portal; s.oldPut = put('SpaceHQ')[0];
  s.standWhy = `the old SpaceHQ's place: on the axis at old z 450 (plan ${s.c}), closing the boulevard and Crown Avenue through the arch; the plan's SpaceHQ_Portal (252 m) at × 1 (the old stood the kit at × 1.35)`;
  // 'ours': the remake, its drum on the HQ's axis carrying S1 (the portal as data: P-1 reads a portal's line on y 0, so it is kept in portalOnAxis until the rule reads
  // the axis; Splice) and Crown Circus station inside it
  const ours = clone(s);
  ours.form = 'ours'; ours.stationIn = 'S1/crown-circus';
  ours.portalOnAxis = { ...clone(s.portalWas), line: 'S1', path: 'trunk', axis: 'y 244, x -40 → 40', station: 'S1/crown-circus', openings: { ...(s.portalWas?.openings ?? {}), at: [-40, 40] },
    why: 'the remake\'s portal moved with the drum to the old place: S1 runs east on y 244 through it. Not in signatures[].portal: P-1 (signature-checks.ts) and portalsOf (plan.ts) read a portal corridor on y 0 (Splice: read the portal\'s axis)' };
  ours.formWhy = 'our remake (brief 007, Sky 2026-10-13): the SpaceHQ_Portal piece, 252 m, at the old place; S1 through its drum with Crown Circus station inside at the concourse';
  // 'old': the old gold SpaceHQ as the old reel stood it (DecoKitStreetSkyline), the old builders' piece
  const o: any = { id: s.id, name: 'SpaceHQ', kind: 'tower', role: 'dominant', form: 'old', areas: ['crown'], c: HQ_OLD.at, yaw: 'grand stair to the south (−y), facing Crown Circus through the arch down the Boulevard', yawDeg: 180, oldYaw: HQ_OLD.put.yaw,
    base: s.base, ground: HQ_OLD.ground, kit: 'SpaceHQ', scale: HQ_OLD.scale, height: HQ_OLD.tip, size: HQ_OLD.size, spans: { plots: [], foot: [] }, oldPut: HQ_OLD.put,
    standWhy: `the old SpaceHQ as the old reel stood it: DecoKitStreetSkyline Put("SpaceHQ", (0, 0, Z.y + 70 = 450), yaw 0, scale 1.35), plan (${HQ_OLD.at}); the kit SpaceHQ (${'80 × 252 × 88.8'}, pivotOffset z -4.4) × 1.35 = ${HQ_OLD.size.join(' × ')} m, tip ${HQ_OLD.tip}; it closes the Boulevard and Crown Avenue through the arch, between the gate towers`,
    groundWhy: `the scaled kit box round the old pivot: x ${HQ_OLD.ground[0]}..${HQ_OLD.ground[1]}, y ${HQ_OLD.ground[2]} (the grand stair's foot, old z 384) to ${HQ_OLD.ground[3]}. No plot building stands on it; Crown Forecourt (open) runs from the north walls (y 144) to the stair foot`,
    station: 'S1/crown-circus beside it, on S1\'s north leg (x -95), no beam through the HQ',
    oldBuilder: 'the old DecoKit builders place the piece (DecoKitStreetSkyline.Landmarks), with its old gold (DecoKitColourways: DK_Gold #F2C14E, metallic 1, smoothness 0.45) and the showpiece probe (DecoKitStreetDemo: DecoKitShowpiece.Apply("SpaceHQ")). Our kit has SpaceHQ rejected, so the TS world carries it as a procedural signature to its tip (data only, nothing drawn from it)',
    crownType: s.crownType, crownWhy: 'the old kit\'s own crown (the old reel\'s gold HQ); the other atom (crescent-tower) is not in the remap',
    watchers: s.watchers, why: 'Sky\'s call between the forms (brief 075 remap step 2); the old reel\'s gold tower closing the Boulevard, the tallest thing in the district (standards.skyline.dominant)',
    source: 'old DecoKit: DecoKitStreetSkyline.cs (Landmarks), DecoKitStreetLayout.cs (Z), the kit SpaceHQ (contracts/world/kit.json)' };
  HQ_FORMS.old = o; HQ_FORMS.ours = ours;
  signatures.push(clone(HQ_FORMS[HQ_FORM]));
  manual(`SpaceHQ switch (plan.spacehq.form = ${HQ_FORM}; brief 075 remap step 2, Sky's call): 'old' = the old gold SpaceHQ, the kit SpaceHQ × 1.35 at the old place (plan (${HQ_OLD.at}), ground x ${HQ_OLD.ground[0]}..${HQ_OLD.ground[1]}, y ${HQ_OLD.ground[2]}..${HQ_OLD.ground[3]}, tip ${HQ_OLD.tip}, the dominant), S1's Crown Circus station beside it on x -95; 'ours' = the SpaceHQ_Portal remake (252 m, drum r 40 at (0, 244)) with S1 east on y 244 through the drum and the station inside. Both forms are in plan.spacehq.forms`); }
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
  landmarks, megastructures, signatures, tram: [T1], skyrail: [S0, SH],
});
plan.routes = undefined; delete plan.routes;
if (args.includes('--with-skyrail')) plan.skyrail = [S1, S2];

plan.notes = {
  ...live.notes,
  concept: [
    'The old DecoKit town at its own coordinates: Crown Circus at the origin (old z 206), the boulevard on x 0 with its walls at x ±18 and 28 m deep blocks, Market Street, the lanes, the four crescents, Orbit Gardens, the harbour terrace, steps, promenade and quay, the piers, the landmarks.',
    'The Handover zone byte for byte: the civic block, its streets (Rocket Lane, Starline Street, Garden Row, Orbit Walk, Pylon Row, Apron Road, Satellite Street, Lantern Lane), its plots and gates, and today\'s neighbours on every edge of the plots in its walk window.',
    'Seams: Satellite Street\'s east pavement is the boulevard\'s west wall (boulevard-west, old W1); Starline Street runs on as the old west lane through it; Market Street meets the boulevard at (0, -410); Lamplight Lane (today\'s line) fronts the garden terrace and the West Terraces; the garden walk crosses Circus Road into Sphere Park.',
    'The boulevard runs through to the Sea Gates (old Comet Row and Satellite Street on its axis); Southside east of it (Constellation N2, Orbit Square, Starline Green, SS1–SS4); the Lantern Quarter south of Market Street.',
    'West Row west of Sunset Way (the old procedural massing\'s place): an access street off Circus Road, lanes, old-grammar blocks; Western Park north of Circus Road to Crown Road.',
    'T1 tram: street running from the West End down Sunset Way and Market Street, up the boulevard to the circus, where it turns on the ring road (R 25). No overhead wires.',
    HQ_FORM === 'old' ? 'S1 sky rail on the circus axis from the West End (Western Park and Orbit Gardens stations), north past the Sphere with Crown Circus station beside the SpaceHQ; the old main loop is planned data (oldtown.skyrail).' : 'S1 sky rail on the circus axis from the West End (Western Park and Orbit Gardens stations), north past the Sphere and east through the SpaceHQ drum (Crown Circus station inside); the old main loop is planned data (oldtown.skyrail).',
    'S2 sky rail (Harbour Line) from the south over the water, Harbour station on the Market Pier\'s landing stage (the T1 interchange at Crown Circus), and a terminal loop round the Skyport Tower with the Skyport station on the Skyport apron: the old harbour line\'s sweep round the Skyport.',
    `SpaceHQ closes the axis at the old place (0, 244) (plan.spacehq.form ${HQ_FORM}: ${HQ_FORM === 'old' ? 'the old gold HQ, 340 m' : 'the SpaceHQ_Portal remake, 252 m'}), the gate towers either side of the forecourt, the Skylon, the Sphere, the Skyport on its caisson at the pier head, the liner at the Market Pier.`,
  ],
  why: ['Sky 2026-10-28 (brief 075): "why can\'t we just remap the world around d handover using old buildings" and "And old props. So the sky trak and other things all planned properly but looking great".', ...MANUAL.slice(0, 0)],
  sources: [...(live.notes?.sources ?? []), `old DecoKit town: ${OLD_DIR} (DecoKitCityPlan, CityBlocks, Block, StreetLayout, Circus, Southside, Gardens, SkyRail, Harbour, StreetSkyline), read by tools/plan/oldtown/old.ts`],
};
// history and skyline notes recorded on the live plan after the remap's first transcription (kept here so the output is reproducible from the base)
plan.version = '0.5.5'; plan.date = '2026-10-29';
plan.notes.history = ["2026-10-29 (Datum, brief 075 plan-match; version 0.5.5): Splice's plan-match patch (briefs/075/oldbuild/plan-patch.json, 175 ops from match.json) decided op by op in briefs/075/oldbuild/plan-patch-applied.json; the old-built town wins outside the Handover where the plan's rules let it. Applied: Crown Avenue at its old-built 16 m (kerbs x ±8, 10 m pavements to the old walls; no tram), its plots' kerb edges and circus-mouth faces with it, n_avenue 2 m north so its end kerb stays on the forecourt; the old Southside blocks on the plan's lines (N1 on its old line y -302..-262; SS1 and SS3 as ss3/a y -473..-427 and ss3/b y -525..-491, the old 18 m gap kept; SS4 34 m at y -518.5..-484.5); ss1 open ground (no old building stands there). Recorded: the old harbour promenade (-0.85) as street-level ground (standards.levels.grounds), not a fourth level. Kept: the Boulevard at 20 m (T1's stop islands need 10 m a half: two stops stand on it); the 0.18 pavement everywhere (one kerb height; the Handover zone fixes it); every Handover plot and street; the crescents, landmarks and old walls as transcribed (box artefacts rejected). Asked of Splice: a per-road pavement width (the old 2-4 m side-street flags at E1, E2, N2), a BG-1 'massing' exemption (the old CityMass west of the gardens), R-F1 for a plot fronting a square (old E1 on the harbour terrace). Handover zone, routes, timings and points unchanged.", "2026-10-29 (Datum, brief 075 swap: network; version 0.5.4): Latch's old-built town (cc903ae0) stands the old SphereStation at ×1 (188 m, legs to r 67, service towers to r 72.4) and S1's north leg on x -95 ran through its leg ring. The Sphere landmark is now the old ×1 (r 72.5, top 188.4; was ×0.425, r 31, top 80), and S1 goes round its east side: east on Circus Road's line past Orbit Gardens station, north (R 70) onto x -52 (78 m off the Sphere's centre, so pylons keep pylonClear.landmark outside its disc), then an S of two R 70 curves (corners (-52, 156) and (-95, 198)) between the Sphere and the west gate tower's shaft onto x -95 north of the Sphere. Crown Circus station moves north 15 m on that straight to (-95, 253) (platforms y 228..278), Crown Walk with it (y 253) and Sphere Park's slot. Sphere Park and the north lawn take x -54..-50 of west-wall-n's back yard for y 9..144 (the old W4 wall's back is at x -46), so the line and its pylons stand on park (S-C1, S-P2). S1 gardens → Crown Circus 304 → 374 m (run 25 → 31 s). T1 is unchanged: its streets (Crown Road, Sunset Way, Market Street) are the Handover's mission streets (official 1124, getaway 447, rivals 593) and its stops are kept, so the old pieces give way to Market Street at the Southside seam (briefs/075/notes.md, Swap: network). Handover zone unchanged (overlay: C-0, C-3, M-1, M-2, M-3, V-1, W-1 identical). The 'ours' form keeps S1 on x -95 (data only, through the ×1 Sphere: it would need the same detour if Sky picks it).", "2026-10-29 (Datum, brief 075 remap step 2, Aster; version 0.5.3): the SpaceHQ switch, plan.spacehq.form (Sky's call; default 'old'). 'old': the old gold SpaceHQ as the old reel stood it (DecoKitStreetSkyline: the kit SpaceHQ × 1.35 at old (0, 450)), ground x -54..54, y 178.12..298, tip 340.2 = standards.skyline.dominant (rivalMax 200 kept; SK-1 exempts the dominant from the harbour band, so no exception), Crown Forecourt to the grand stair's foot (y 178.12), and S1's third station, Crown Circus, on its north leg beside it (x -95, y 238; banks, stairs and lifts on Sphere Park). 'ours': the SpaceHQ_Portal remake (252) at (0, 244) with S1 east on y 244 through its drum and the station inside. Both forms in plan.spacehq.forms; tools/plan/oldtown/transcribe.ts writes the chosen one. Handover zone untouched; Pulse's journeys kept (no journey rides the new run). Notes: briefs/075/notes.md 'SpaceHQ switch'.", "2026-10-29 (Datum, brief 075 review, Aster relaying Sky; version 0.5.2): (1) the old asphalt road back (Sky: overriding 'keep powdblue'): roads.finish.set asphalt, road-asphalt = the old DK_Asphalt_Road (Vela's port, #817B75, 16 m repeat), dashes 0.12 m, 2 m on 4 m off; the light ribbon kept on every class (GDD §14/§15), so SF-24 holds unchanged. (2) The Skyport reachable (Sky: 'we need the skytrak to get here'): S2 Harbour Line from the south edge over the water on x 117, Harbour station on the Market Pier's new landing stage (x 96..140, y -196..-106; T1/Crown Circus interchange 137 m), a terminal loop (R 40) round the Skyport Tower with the Skyport station on its west leg over the new Skyport apron (x 96..140, y ±30), 41 pylons in water and on the quays. (3) Every structure reachable (RCH-1 33/33): the Skyport apron runs 8 m onto the tower's caisson, the Market Pier 6 m onto the liner berth's. The Handover zone, its routes (M-1), escape walks (W-1) and V-1 unchanged; Pulse's journeys kept (T1 and S1 unchanged; S2's runs added, its journeys Pulse's). Notes: briefs/075/notes.md 'Remap: Skyport and reach'.", "2026-10-28 (Datum, brief 075 remap follow-up, Aster; version 0.5.1): no bare sand in the district outside water, parks and plazas. The first remap left the strip west of Sunset Way (x -460..-352) as the beyond hatch, which the world draws as bare ground. It is now West Row (area westrow): an access street on x -416 from Circus Road (gate west-row-gate) to the south plot line, 8 m lanes opposite the seams of Sunset Way's east-side plots and at equal spans of at most 140 m, ten plots fitted as old-grammar blocks (28 m solid on the sheet-edge side, 46 m open-court on Sunset Way, the corner block on Circus Road), Western Park run on north to Crown Road; westend and sphere areas start at Sunset Way's west kerb; the beyond hatch keeps only the sheet's slivers. Rule-based in tools/plan/oldtown/transcribe.ts (WR); the Handover zone, its streets, plots, gates, routes and site are unchanged", "2026-10-28 (Datum, brief 075 remap, Aster; input edit, version kept at 0.5.0): the gate towers back to the old reel: landmarks disc-west / disc-east top 108 → 156.6, scale 0.69 → 1 (the kit DiscTower as built, tip 156.8; at 108 the fit fell under tags.scale 0.9 and the world drew procedural cylinders). New standards.skyline.harbour.exceptions (the pair, tip 156.8) and exceptionsWhy, as standards.skyline.vista was done: SK-1 to read it (Splice; the one-line read is in signature-checks.ts); bends GDD §10's harbour line for this pair (next GDD version, Aster). The Crown Avenue arch's text (landmarks[disc-west].gateway piece, whyNot38, siteWhy, keep) restated for its site at (0, 44) from Splice's measure (frontage zone |x| 13.5–18 at y 41–47, the old walls behind the plinths); oldtown.manual's arch and gate tower lines updated (the duplicate gate line dropped). No road, plot, line, stop or Handover geometry changed", ...(live.notes?.history ?? [])];
{ const t = plan.standards.skyline.tiers as string[]; if (!t.some(x => x.startsWith('gate pair'))) t.splice(3, 0, "gate pair 156.8: disc-west / disc-east (landmarks, the kit DiscTower × 1.0 either side of Crown Avenue at (±44, 166); standards.skyline.harbour.exceptions)"); }
// standards that name today's roads, plots and lines
const st = plan.standards;
// The old harbour promenade (brief 075 plan-match, plan-patch builderFixes old/promenade): ground of the street level, not a fourth level. GDD §01 fixes three levels (street 0, deck 9.43,
// concourse 24); the promenade is the quay strip's lower walk, 1.03 m under the pavement, reached on foot by the harbour steps (no connector, no deck), so it is recorded as a street-level
// ground with its own walk height, as the civic block records its landings: Latch keeps it as built (-0.85), Splice's plan-match reads it from here.
st.levels.grounds = [{ id: 'promenade', level: 'street', walk: S.PromenadeY, x: harbour.promenade, y: [sheetY[0], sheetY[1]], from: 'steps', steps: [S.StepsX, S.PromenadeX],
  why: `the old harbour (DecoKitHarbour): the upper terrace at the pavement (x ${harbour.terrace.join('–')}), the steps (x ${S.StepsX}–${S.PromenadeX}) down to the promenade at ${S.PromenadeY} (x ${harbour.promenade.join('–')}), the quay edge at x ${harbour.quay}, water ${S.WaterY}. Street-level ground, walked on foot from the terrace by the steps: not a level (GDD §01: street, deck, concourse)` }];
// the road finish (brief 075 review, Sky 2026-10-28: the old asphalt road back, overriding "keep powdblue"): the asphalt set is the plan's, on the old DK_Asphalt_Road
// (Vela: DK_Ground_RoadAsphalt is the old sheet byte for byte, 16 m a repeat), the old reel's dashes (0.12 m, 2 m on, 4 m off); the light ribbon stays (GDD §14 Datum, §15: every
// road class carries the light ribbon), so SF-24's mouths keep their strip and no rule moves (notes "Remap: Skyport and reach")
{ const F = st.streetscape.roads.finish, A = F.sets.asphalt, K = F.keys['road-asphalt'], M = st.streetscape.roads.marks;
  F.set = 'asphalt';
  A.drop = ['shoulder'];
  A.why = "brief 075 review (Sky 2026-10-28: the old asphalt road back, overriding 'keep powdblue'; Vela ported the old DK_Asphalt_Road): the old city's dark road with a few clean white lines (roads.marks: one dashed centre line, dividers only where roads.marks.lanes names a road), the pearl kerb, and the light ribbon at the kerb face, which GDD §14 (Datum: every road class carries the light ribbon) and §15 keep on every class: so a gate's lane mouth keeps its strip straight on (SF-24) and nothing is loosened. No pearl shoulder band, no white edge lines. Was (brief 055 road test): ribbon dropped too, as one more bright line beside the dashes; the old road had none, so dropping it again is Sky's call through Aster with a GDD change, then SF-24 reads the set (notes \"Remap: Skyport and reach\")";
  Object.assign(K, { hex: '#817B75', tile: 16, why: "brief 075 review (Sky 2026-10-28: the old asphalt road back): the old DecoKit DK_Asphalt_Road, ported byte for byte by Vela (tools/decokit/road_tex.py asphalt(), road_ribbon.py DK_Ground_RoadAsphalt: the old asphalt_road_v1 sheet and normal, smoothness .28, no mask); hex = the sheet's mean (look.json harbour.road-asphalt); tile 16 = the sheet's repeat in metres, the old .mat's 16 m (look.json road-asphalt tile 1 = UV in world metres, the repeat lives in the material). Was (brief 055 road test): a made slate sheet, #505359, tile 4" });
  Object.assign(M.dash, { width: 0.12, dividerWidth: 0.12, length: 2, spacing: 6 });
  M.dash.on = String(M.dash.on).replace('two ways, 0.2 m wide; and each lane divider below, 0.12 m wide', 'two ways, 0.12 m wide; and each lane divider below, as wide');
  M.dash.why = 'brief 075 review: the old reel\'s dashes (DecoKit road paint DK_Road_Paint #E8E2D2, matte): 0.12 m wide, 2 m on and 4 m off (length 2, spacing 6 = the pitch, centre to centre); was 0.2 m centre lines (brief 055)';
  manual('Road finish: the asphalt set (Sky 2026-10-28, the old asphalt road back): road-asphalt = the old DK_Asphalt_Road (#817B75, 16 m repeat), dashes 0.12 m, 2 m on 4 m off; the light ribbon kept on every class (GDD §14/§15), so SF-24 holds as written'); }
st.streetscape.sections.tramStreet.district.roads = ['boulevard', 'market-street'];
st.streetscape.sections.tramStreet.primary.roads = ['sunset-way', 'crown-road'];
delete st.streetscape.sections.tramStreet.notTram;
// the kerb faces with no plot edge to run on (streetscape.roads.kerbs): the circus mouths of the two radial roads, and every foot way's mouth on a road
{ const K = st.streetscape.roads.kerbs, R = RING_OUT, pav = circus.crescent, h = KERB, y0 = r2(Math.sqrt(R * R - h * h)), y1 = r2(Math.sqrt(pav * pav - h * h));
  const ha = AV_KERB, ya0 = r2(Math.sqrt(R * R - ha * ha)), ya1 = r2(Math.sqrt(pav * pav - ha * ha)); // Crown Avenue's mouth at its own 16 m
  K.circusMouths.faces = [
    { id: 'circus-mouth/crown-avenue/e', road: 'crown-avenue', a: [ha, ya0], b: [ha, ya1], toward: [-1, 0] }, { id: 'circus-mouth/crown-avenue/w', road: 'crown-avenue', a: [-ha, ya0], b: [-ha, ya1], toward: [1, 0] },
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
st.skyline.harbour.why = "the Skyport Tower stays the harbour's icon (brief 007 Fixed): nothing standing within 150 m of the quay edge (SK-1 takes the quay's westmost point, x 46 in the remap) tops 108, save the exceptions below";
st.skyline.harbour.exceptions = [{ id: 'disc-west', tip: 156.8 }, { id: 'disc-east', tip: 156.8 }];
st.skyline.harbour.exceptionsWhy = "brief 075 remap (Aster, under Sky's scope: only the Handover is protected): the Crown Avenue gate towers stand as the old reel stood them, the kit DiscTower × 1.0 (156.6 on the 0.2 landmark base, tip 156.8), not a × 0.69 stand-in the fit rejects (tags.scale 0.9–1.1). In the remap the quay runs to x 46, so the 150 m band reaches x −104 and takes the whole axis; the pair stands inland of the harbour front at (±44, 166), under rivalMax 200 and under the dominant. Scoped to these two landmarks only; every other tip within the band keeps 108. Bends GDD §10's harbour line for this pair: the note goes in the next GDD version (Aster)";
// the dominant follows the SpaceHQ switch: 340.2 for the old gold HQ, 252 for the remake. rivalMax stays 200 (under 0.8 × either); SK-1 exempts the dominant from the
// harbour band, so the old HQ needs no exception there
const DOM = {
  old: { id: 'space-hq', tip: HQ_OLD.tip, why: `the old gold SpaceHQ as the old reel stood it (the kit SpaceHQ × 1.35, ${HQ_OLD.tip} m; brief 075 remap step 2, plan.spacehq.form 'old'): the tallest thing in the district; every other tip stays under rivalMax` },
  ours: clone(st.skyline.dominant),
};
st.skyline.dominant = clone(DOM[HQ_FORM]);
{ const t = st.skyline.tiers as string[], i = t.findIndex(x => x.startsWith('dominant')); if (i >= 0) t[i] = HQ_FORM === 'old' ? `dominant ${HQ_OLD.tip}: space-hq (signatures; the old gold SpaceHQ, the kit × 1.35, plan.spacehq.form 'old')` : t[i]!; }
plan.spacehq = {
  form: HQ_FORM,
  setting: 'the one switch: set form to "old" or "ours" and run bun tools/plan/oldtown/transcribe.ts --out contracts/world/harbour-district.plan.json (or pass --spacehq old|ours); it writes the chosen form into signatures[space-hq], skyrail S1, the Crown Forecourt plot and standards.skyline.dominant, and keeps both here',
  why: 'brief 075 remap step 2 (Aster): the SpaceHQ form is Sky\'s call; "old" is the default until Sky picks',
  forms: {
    old: { what: 'the old gold SpaceHQ (the old reel\'s tower closing the Boulevard), placed by the old builders; S1 Crown Circus station beside it', signature: HQ_FORMS.old, skyrail: S1_OLD, forecourt: [W0, E1, north + 4, HQ_OLD.ground[2]], dominant: DOM.old },
    ours: { what: 'our SpaceHQ_Portal remake at the old place with S1 through its drum and Crown Circus station inside', signature: HQ_FORMS.ours, skyrail: S1_OURS, forecourt: [W0, E1, north + 4, HQ_OURS_SOUTH], dominant: DOM.ours },
  },
};
{ const mx = st.frontage.mix; st.frontage.mix = { records: mx.records, lamplight: mx.lamplight, boulevard: mx.crescent, crown: mx.records, southside: mx.gardens, westrow: mx.gardens, westend: mx.gardens, gardens: mx.lamplight, lantern: mx.lantern, harbour: mx.harbour };
  manual('Frontage mixes for the new areas from today\'s: the Boulevard takes the Crescent Quarter\'s, Crown Circus the Records Precinct\'s, Southside, West Row (Southside\'s spirit, brief 075 follow-up) and the West Terraces Sphere Gardens\' (their faces on squares and greens), the garden terrace Lamplight\'s, the Lantern Quarter and the harbour keep theirs'); }
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
// The old Southside blocks on the plan's lines (brief 075 plan-match, briefs/075/oldbuild/plan-patch-applied.json). The old DecoKitSouthside stood its blocks on a 16 m street (facades x ±8)
// and across the lines of Market Street (y -410, the Handover's distributor and T1) and Apollo Way (y -472, 60 m junction spacing); the plan's Boulevard (20 m, facades ±18) and those streets
// stand, so each old block shifts onto one plan block of its own length where its plot allows (Latch shifts the old DecoKitBlock onto it): N1 keeps its old line (y -302..-262), SS1 (46) and
// SS3 (34) share ss3 with the old 18 m gap between them, SS4 (34) runs from Apollo Way's building line, SS2 keeps the plan's 40 (its plot's building line is 40.5, under the old 46).
const blockAt = (id: string, plotId: string, [bx0, bx1, by0, by1]: [number, number, number, number], old: string) => {
  const W = r2(bx1 - bx0), H = r2(by1 - by0), court = Math.min(W, H) >= st.blocks.solidBelow ? 'open' : 'solid';
  const run = (face: 'w' | 'e' | 's' | 'n'): Run => {
    const along = face === 's' || face === 'n', a: V = face === 'e' ? [bx1, by0] : face === 'n' ? [bx0, by1] : [bx0, by0], b: V = along ? [bx1, a[1]] : [a[0], by1];
    return { face, a, b, n: ({ w: [-1, 0], e: [1, 0], s: [0, -1], n: [0, 1] } as Record<string, V>)[face]!, faces: facing(plotId, face), caps: ['corner', 'corner'], bays: ((along ? W : H) - 16) / 6, old };
  };
  block(id, plotId, (['w', 'e', 's', 'n'] as const).map(run), court);
};
// where each old Southside block stands in the plan (brief 075 plan-match; Latch shifts the old DecoKitBlock onto its plan block, old pieces, Sky's height rule)
const SS_BUILT: Record<string, string> = {
  N1: 'block n1, on its old line (y -302..-262), 28 deep: its west half is Satellite Street (gives way)', N2: 'block n2 (y -304.5..-264.5): 2.5 m south of its old line, the plan\'s 6.5 m access pavement on Sunset Way east (old 4 m)',
  SS1: 'block ss3/a (y -473..-427): shifted 23 m south and 10 m west, off Market Street\'s line and onto the Boulevard\'s wall line (x -18)', SS2: 'block ss2 (y -459.5..-419.5) at 40 m (old 46): shifted 10 m east and south of Market Street',
  SS3: 'block ss3/b (y -525..-491): shifted 23 m south with SS1 (the old 18 m gap kept) and 10 m west', SS4: 'block ss4 (y -518.5..-484.5): shifted 16.5 m south off Apollo Way\'s line and 10 m east',
  SS5: 'gone: it stood on Lantern Lane\'s corner (x -60, y -490), a Handover street; lantern-south keeps its grammar block', E1: 'stands on the harbour terrace (x 46..74) in the old-built town; not in the plan: a plot there fronts no access road (R-F1), asked of Splice',
  W1: 'gone: it stood across Satellite Street (x -66..-54), a Handover street',
};
const SS_OLD = (n: string) => { const b = old.southside.blocks.find(q => q.name === n)!; return `old ${n}: ${b.src} (plan centre ${P(b.x, b.z).join(', ')}, ${b.w} × ${b.d})`; };
blockAt('n1', 'n1', [-46, -18, -302, -262], SS_OLD('N1'));
fit('n2'); fit('ss2');
blockAt('ss3/a', 'ss3', [-46, -18, -473, -427], SS_OLD('SS1'));
blockAt('ss3/b', 'ss3', [-46, -18, -525, -491], SS_OLD('SS3'));
blockAt('ss4', 'ss4', [18, 46, -518.5, -484.5], SS_OLD('SS4'));
manual('The old Southside blocks shift onto the plan\'s lines (brief 075 plan-match): N1 on its old line y -302..-262 (28 deep: its west half is Satellite Street); SS1 (46) and SS3 (34) on ss3 at y -473..-427 and -525..-491 (the old 18 m gap between them; old SS1 stood across Market Street); SS4 (34) at y -518.5..-484.5 from Apollo Way\'s building line (old: across Apollo Way\'s plan line); SS2 at the plan\'s 40 (old 46: its plot\'s building line is 40.5 between Market Street and Apollo Way); ss1 open (no old building stands there); N2 at y -304.5..-264.5, 2.5 m south of its old line (Sunset Way east\'s 6.5 m access pavement; old 4 m)');
for (const id of ['lamplight-west', 'lantern-west', 'lantern-south', ...wrPlots]) fit(id);
st.blocks.plots = ['boulevard-west', 'lamplight-east', 'west-wall-n', 'east-wall-s', 'east-wall', 'east-wall-n', 'terrace-s', 'lamplight-west', 'n1', 'n2', 'ss2', 'ss3', 'ss4', 'lantern-west', 'lantern-south', ...wrPlots];
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
  southside: old.southside.blocks.map(b => ({ ...b, c: P(b.x, b.z), built: SS_BUILT[b.name] ?? 'gone: the Handover zone' })),
  gardens: { canal: { x: old.gardens.canal.x, half: old.gardens.canal.half, y: [-148, yOf(old.gardens.canal.to)], oldY: [yOf(old.gardens.canal.from), yOf(old.gardens.canal.to)] }, pool: { c: P(old.gardens.pool.c[0], old.gardens.pool.c[1]), r: old.gardens.pool.r, coping: old.gardens.pool.coping, rim: old.gardens.pool.rim }, pavilion: { at: P(old.gardens.pavilion.at[0], old.gardens.pavilion.at[1]), yaw: old.gardens.pavilion.yaw } },
  harbour: { terrace: harbour.terrace, steps: [S.StepsX, S.PromenadeX], promenade: harbour.promenade, quay: harbour.quay, levels: { terrace: 0.15, promenade: S.PromenadeY, water: S.WaterY }, piers: { market: piers.market, skyport: piers.skyport }, skyportApron: old.apron },
  props: { puts: old.puts.map(p => ({ kit: p.kit, at: P(p.at[0], p.at[2]), z: p.at[1], yaw: p.yaw, scale: p.scale })), towers: old.towers.map(t => ({ kit: t.kit, at: P(t.at[0], t.at[1]), yaw: t.yaw })), monument: old.monument },
  skyrail: {
    main: { closed: true, spline: 'SkyPath.Set(route, closed: true): a closed spline through these points (x, height, y)', pts: old.skyrail.main.map(([x, h, z]) => [x, r2(h), yOf(z)]), sphereRing: { c: sph, r: old.skyrail.ringR, deg: [90, 225], height: 34 },
      planned: 'S1 (west half: Circus Road, north through Sphere Park, the loop round the Sphere). Not planned: the boulevard leg over the west promenade (x -13, y -906..154 at 24 m): its pylons stood on the promenade, which standards.skyrail.pylonZones does not allow; and the return leg (x -150..-110), which crossed the Handover\'s site. Splice\'s call (notes "Remap: plan")' },
    harbour: { pts: old.skyrail.harbour.map(([x, h, z]) => [r2(x), r2(h), yOf(z)]), station: [old.skyrail.station[0], yOf(old.skyrail.station[2])], railOverGround: old.skyrail.stationRail, planned: 'S2' },
    planned: { S1, S2, status: 'the old lines as first planned (S2 here is the old portal-tower variant; the plan\'s S2 is plan.skyrail[1], brief 075 review). Was: not in plan.skyrail: station banks are laid for east-west lines only (generator); S2/harbour and S2/apollo, S1/sphere stand on north-south lines. Checked with tools/plan/oldtown/transcribe.ts --with-skyrail (the transit rules pass on them; the world build stops at the banks)' },
    beam: { h: old.skyrail.pylon.beamH, halfWidth: old.skyrail.pylon.beamW, pylonStep: old.skyrail.pylon.step, footClear: old.skyrail.pylon.footClear, speed: old.skyrail.pylon.speed },
    levels: { street: 0, deck: 9.43, concourse: 24, oldMain: '24 on the boulevard, rising 1 m per ~35 m to 34 round the Sphere', oldHarbour: '21.15 over the quay (rail 22 over the promenade at -0.85), 24 round the Skyport' },
  },
};

// the timetable (Pulse's format): the new lines' runs, stop to stop, at today's speeds, headways and dwells; journeys left to Pulse
{ const dT = derive(plan as Plan), speedOf = (l: string) => (live.timetable?.lines ?? []).find((x: any) => x.line === (l === 'T1' ? 'T1' : 'S1')) ?? { headwaySec: 180, dwellSec: 15, speed: 8 };
  plan.timetable = { ...(live.timetable ?? {}), lines: [...plan.tram.map((l: any) => ({ l, mode: 'tram' as const })), ...plan.skyrail.map((l: any) => ({ l, mode: 'sky' as const }))].map(({ l, mode }) => {
    const sp = speedOf(l.id), items = placements(plan as Plan, dT, l, mode).items;
    return { line: l.id, headwaySec: sp.headwaySec, dwellSec: sp.dwellSec, speed: sp.speed, runs: items.slice(1).map((it, i) => ({ from: `${l.id}/${items[i]!.id}`, to: `${l.id}/${it.id}`, sec: Math.round((it.s - items[i]!.s) / sp.speed) })) };
  }), journeys: [], notes: [...(live.timetable?.notes ?? []).slice(0, 0), 'brief 075 remap (Datum): lines regenerated from the remapped plan by tools/plan/oldtown/transcribe.ts at today\'s speeds, headways and dwells; journeys cleared for Pulse to set again'] };
  manual('Timetable: T1 and S1 runs regenerated stop to stop at today\'s speeds; journeys cleared (they named T2 and today\'s stops) for Pulse'); }

// Pulse's part of the timetable (journeys, open questions, notes; bun run life:journeys) is kept from the plan already at --out when the regenerated lines are the same
// lines (same runs, speeds, headways and dwells), so a re-run of this transcription never wipes it; a change to the lines clears the journeys for Pulse, as above.
{ const prev: any = PREV;
  // Pulse's journeys stand while every line they ride is the same: the same speed, headway and dwell, and every run between two stops the journeys name still run as
  // it was. A line may gain a station or a run (brief 075's S2; S1's Crown Circus, whose run differs between the SpaceHQ forms and is named by no journey yet)
  const key = (l: any) => JSON.stringify([l.line, l.headwaySec, l.dwellSec, l.speed]);
  const named = new Set<string>((prev?.timetable?.journeys ?? []).flatMap((j: any) => [j.from, j.to, ...(j.via ?? [])]));
  const kept = (a: any) => plan.timetable.lines.some((b: any) => key(a) === key(b) && a.runs.filter((r: any) => named.has(r.from) && named.has(r.to)).every((r: any) => b.runs.some((q: any) => JSON.stringify(q) === JSON.stringify(r))));
  if (prev?.timetable && prev.timetable.lines.every(kept)) {
    for (const k of ['journeys', 'openQuestions', 'notes']) if (prev.timetable[k] !== undefined) plan.timetable[k] = prev.timetable[k]; }
  else if (prev?.timetable) console.log(`timetable: the lines changed from ${OUT}'s; journeys cleared for Pulse`); }

function bump(v: string) { const p = v.split('.').map(Number); p[1]! += 1; p[2] = 0; return p.join('.'); }
writeFileSync(resolve(ROOT, OUT), JSON.stringify(plan, null, 1) + '\n');
console.log(`wrote ${OUT}: ${plan.roads.length} roads, ${plan.plots.length} plots, ${plan.blocks.length} blocks, ${plan.circus.crescents.length} crescents; ${MANUAL.length} manual decisions`);
