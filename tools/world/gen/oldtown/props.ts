// Brief 075 remap (Aster, Sky 2026-10-28: "And old props ... planned properly but looking great"): the old town's props by the old placement rules, generated
// from the plan (plan.oldtown, the plan's foot walks, circus and quay, the buildings the plan made) by the old builders' rules
// (~/Projects/the-suborned/game/Assets/TheSuborned/Editor/DecoKit/: DecoKitGardens.Plant, DecoKitHarbour, DecoKitDistrictDressing.Boulevard / Circus / Parade /
// Harbour, DecoKitStreetLife.Frontage, read only). The old frame is the plan's (plan (x, y) = old (x, z - 206), unscaled; Datum's transcription), so an old
// rule's numbers stand where the old town had them; the street layer's tests (SF-1 walk zones, SF-2 doors and crossings, SF-8 corners, SF-14 the quay edge,
// solids, drives, neighbours) judge every spot, and a spot they refuse is logged with its rule, never squeezed (SF-6) or forced.
//
//  - Placed last in the street layer under explicit ids (`street/old/<rule>/<n>`), so no other street id moves.
//  - Every piece is the old prop, named by the rule (the first adopted of the old builder's own fallbacks: Any(p, want, fallback)); a piece the kit lacks is a gap.
//  - The old prop wins where both exist: an item of ours standing in the old basin or canal is taken out (it would stand in water), and an item of the same
//    family (a lamp where the old lamp stands, a bench where the old bench stands, a lawn palm where the old planting goes) gives way to the old one; street
//    palms, transit kits, crossings and the takedown places never give way.
//  - Nothing within OLD_PROPS.protect m of the Handover's capstone zone (zoneOf): its dressing is the capstone programme's and the Handover's site reads it.
//  - The old kerb was 2 m nearer the road line than ours (old KerbX 8, our boulevard kerb face at 10): a kerb-relative old number moves out by the difference.
import type { StreetItem } from '../../../../contracts/world/world.schema';
import { type V, dist, inPoly, lineDist } from '../../../plan/geom';
import { type Ctx, r3, rng } from '../common';
import { adopted } from '../kitrefs';
import { bbox } from '../geo';
import { BB } from '../billboards';
import type { Layer } from '../street/kit';
import { zoneOf } from '../street/capstone';
import { roadAt } from '../../../plan/street-zones';
import type { OldGround } from './ground';

export const OLD_PROPS = {
  /** m round the capstone zone where no old prop goes (the zone is the Handover's walk window grown by 30 m already). */
  protect: 0,
  /** m round the capstone zone where nothing of ours gives way: street pieces are picked against their neighbours within 20 m (kitrefs.ts NEAR), so a removal
   *  nearer than that could re-pick a piece in the zone. */
  evictGuard: 25,
  /** The old frame's circus z and kerb line (DecoKitCityPlan.Circus, DecoKitStreetLayout.KerbX): plan y = old z - 206. */
  oldCircusZ: 206, oldKerbX: 8,
  /** Kinds of ours that give way to an old prop of the same family (old wins); street palms (tree on a street edge) never do. */
  family: { garden: ['tree', 'lamp', 'bench', 'bin', 'planter', 'planter-bed', 'tub-palm', 'round-bench'], pavilion: ['kiosk-pavilion', 'tree', 'lamp', 'bench', 'bin', 'planter', 'planter-bed', 'tub-palm', 'round-bench'], lamp: ['lamp'], bench: ['bench', 'round-bench'], tree: ['tree'], planter: ['planter', 'planter-bed', 'tub-palm'], bin: ['bin'], 'advertising-column': ['advertising-column'], 'public-clock': ['public-clock'], 'kiosk-food': ['kiosk-food'], 'drinking-fountain': ['drinking-fountain'] } as Record<string, string[]>,
  /** Edges whose items never give way (transit kits, station banks, crossings, the takedown, the tram street's kerbside). */
  fixedEdge: /^(stop\/|bank\/|tram\/|civic|takedown|crossing|island\/)/,
};

export type OldPropsLog = { placed: Record<string, number>; pieces: Record<string, number>; refused: Record<string, number>; evicted: { id: string; kind: string; why: string }[]; gaps: { rule: string; why: string }[] };

type Opt = { z?: number; ground?: boolean; family?: string; scale?: number; basin?: boolean; quiet?: boolean };

export function oldProps(ctx: Ctx, L: Layer, g: OldGround | undefined, o: { buildings: { ring: V[]; base: number; height: number }[]; frontBays?: { a: V; b: V; road: string }[] }): OldPropsLog | undefined {
  const ot = (ctx.plan as { oldtown?: any }).oldtown;
  if (!ot || !g) return undefined;
  const log: OldPropsLog = { placed: {}, pieces: {}, refused: {}, evicted: [], gaps: [] }, pav = ctx.pav, P = OLD_PROPS;
  const zone = zoneOf(ctx), near = (p: V, m: number) => p[0] > zone[0] - m && p[0] < zone[1] + m && p[1] > zone[2] - m && p[1] < zone[3] + m, guarded = (p: V) => near(p, P.protect), held = (p: V) => near(p, P.evictGuard);
  const ids = new Map<string, number>(), have = new Map(adopted().map(a => [a.id, a]));
  const pieceOf = (want: string[]) => want.map(id => have.get(id)).find(Boolean);
  const roads = ctx.d.roads.map(r => ({ line: r.line as V[], half: r.w / 2 })), cc = ctx.plan.circus;
  const water = ctx.plan.water as V[][], keep = g.keep;
  /** On ground a prop may stand on: not a carriageway, not the circus ring road, not the sea, not the basin or canal. */
  const onGround = (q: V, basin = false) => !roads.some(r => lineDist(q, r.line) < r.half) && !(Math.hypot(q[0] - cc.c[0], q[1] - cc.c[1]) > cc.ring[0] && Math.hypot(q[0] - cc.c[0], q[1] - cc.c[1]) < cc.ring[1]) && !water.some(w => inPoly(q, w)) && (basin || !keep.some(k => inPoly(q, k.ring)));
  const bump = (rec: Record<string, number>, k: string) => { rec[k] = (rec[k] ?? 0) + 1; };

  /** One old Put: the piece `want` (first adopted) at old-rule position `at`, Unity yaw `psi` (degrees; plan yaw = -psi). */
  const put = (rule: string, kind: string, want: string[], at: V, psi: number, edge: string, opt: Opt = {}): boolean => {
    const a = pieceOf(want);
    if (!a) { log.gaps.push({ rule, why: `${want.join(' / ')}: not adopted in the kit` }); return false; }
    if (guarded(at)) { bump(log.refused, `${rule}: the Handover's zone`); return false; }
    const s = opt.scale ?? 1, yaw = r3((-psi * Math.PI) / 180), size: [number, number, number] = [r3(a.size[0] * s), r3(a.size[2] * s), r3(a.size[1] * s)];
    const cs = Math.cos(yaw), sn = Math.sin(yaw), corners: V[] = [[-1, -1], [1, -1], [1, 1], [-1, 1]].map(([u, v]) => [at[0] + (u! * size[0] / 2) * cs - (v! * size[1] / 2) * sn, at[1] + (u! * size[0] / 2) * sn + (v! * size[1] / 2) * cs] as V);
    if (![at, ...corners].every(q => onGround(q, opt.basin))) { bump(log.refused, `${rule}: off the ground (carriageway, sea or basin)`); return false; }
    const n = ids.get(rule) ?? 0, id = `street/old/${rule}/${n}`, z = opt.z ?? pav, ground = opt.ground ?? true;
    if (kind === 'advertising-column' && L.items.some(x => x.kind === kind && dist(x.at, at) < BB.columnGap - 0.5)) { bump(log.refused, `${rule}: BB-4 (columns ${BB.columnGap} m apart)`); return false; }
    if (kind === 'kiosk-food' && L.items.some(x => x.kind === kind && dist(x.at, at) < 30 - 0.5)) { bump(log.refused, `${rule}: SF-7 (food kiosks 30 m apart)`); return false; }
    if (kind === 'kiosk-pavilion') { const pl = ctx.d.plots.find(q => inPoly(at, q.polyV)); if (!pl || L.items.some(x => x.kind === kind && inPoly(x.at, pl.polyV))) { bump(log.refused, `${rule}: SF-7 (one kiosk pavilion per plot)`); return false; } }
    const ed = edge.startsWith('lawn/') ? edge : `old/${edge}`; // a lawn's planting is free (SF-7 spaces trees on street edges only)
    const add = () => L.add(kind, at, yaw, z, ed, { size, id, ground, kit: { id: a.id, scale: [s, s, s], yaw } });
    let ok = add();
    if (!ok && opt.family) { // the old prop wins: ours of the same family standing in its way give way, if that is all that stands there
      const fam = P.family[opt.family] ?? [opt.family], r = Math.max(size[0], size[1]) / 2;
      const mine = L.items.filter(x => fam.includes(x.kind) && !x.id.startsWith('street/old/') && !P.fixedEdge.test(x.edge) && !(x.kind === 'tree' && !x.edge.startsWith('lawn/')) && !held(x.at) && dist(x.at, at) < r + Math.max(x.size[0], x.size[1]) / 2 + 3);
      if (mine.length) {
        const gone = mine.map(x => ({ ...x }));
        L.evict(gone.map(x => x.id)); ok = add();
        if (ok) for (const x of gone) log.evicted.push({ id: x.id, kind: x.kind, why: `${rule}: the old ${a.id} stands there` });
        else for (const x of gone) L.add(x.kind, x.at, x.yaw, x.z, x.edge, restore(x));
      }
    }
    if (!ok) { if (!opt.quiet) bump(log.refused, `${rule}: ${kind} blocked (street layer)`); return false; }
    ids.set(rule, n + 1); bump(log.placed, rule); bump(log.pieces, a.id);
    return true;
  };
  const restore = (x: StreetItem) => ({ size: x.size, id: x.id, uncounted: true as const, force: true, ...(x.variant ? { variant: x.variant } : {}), ...(x.kit ? { kit: x.kit } : {}) });
  const oz = (z: number) => z - P.oldCircusZ; // old z → plan y

  // the basin and canal: whatever of ours stands in the water or on the coping gives way (the old prop wins)
  const wet = L.items.filter(x => !P.fixedEdge.test(x.edge) && !held(x.at) && keep.some(k => inPoly(x.at, k.ring)));
  if (wet.length) { L.evict(wet.map(x => x.id)); for (const x of wet) log.evicted.push({ id: x.id, kind: x.kind, why: `in the old ${keep.find(k => inPoly(x.at, k.ring))!.id.split('/').slice(1).join(' ')}` }); }

  // ---- Orbit Gardens (DecoKitGardens.Build, .Plant)
  const G = g.gardens;
  if (G) {
    const { canal: C, pool: Pl } = G, lawn = (q: V) => `lawn/${ctx.d.plots.find(p => inPoly(q, p.polyV))?.id ?? 'gardens'}`;
    put('G7-fountain', 'fountain', ['PlazaFountain', 'AtomFountain'], Pl.c, 0, 'gardens/basin', { z: Pl.water, ground: false, basin: true });
    // G1..G3: cypress walks along the canal, benches facing the water every 24 m, sphere lamps every 24 m (the old 12 m out of phase stood a lamp 1.4 m from a
    // cypress; SF-11 keeps a tree 3 m from a lamp, so the lamp stands between two cypresses, 4 m on)
    for (let y = C.y0 + 4; y < C.y1; y += 8) for (const s of [-1, 1]) {
      put('G1-cypress', 'tree', ['Cypress'], [C.x + s * (C.half + 7), y], y * 13, lawn([C.x + s * (C.half + 7), y]), { family: 'garden' });
      if (Math.abs(((y - C.y0 - 4) % 24 + 24) % 24) < 0.1) put('G2-bench', 'bench', ['Bench'], [C.x + s * (C.half + 4.2), y + 4], s < 0 ? 90 : -90, lawn([C.x, y]), { family: 'garden' });
      if (Math.abs(((y - C.y0 - 12) % 24 + 24) % 24) < 0.1) put('G3-lamp', 'lamp', ['PlazaSphereLamp'], [C.x + s * (C.half + 5.6), y + 4], 0, lawn([C.x, y]), { family: 'garden' });
    }
    // G4: fan palms round the basin, clear of the gate axis and the canal
    const dir = (phi: number): V => [Math.sin((phi * Math.PI) / 180), Math.cos((phi * Math.PI) / 180)];
    for (let k = 0; k < 10; k++) {
      const phi = 18 + 36 * k, dd = (a: number) => Math.abs(((phi - a + 540) % 360) - 180);
      if (dd(90) < 20 || dd(270) < 20 || dd(180) < 20) continue;
      const q: V = [Pl.c[0] + dir(phi)[0] * (Pl.r + 7.5), Pl.c[1] + dir(phi)[1] * (Pl.r + 7.5)];
      put('G4-palm', 'tree', ['FanPalm'], q, phi * 3, lawn(q), { family: 'garden' });
    }
    // G6: the pavilion west of the canal, café tables on its terrace; a coffee cart at the gate
    // the old pavilion stood mid-garden beside the canal; the remap's garden ends on a street (its south edge is Lamplight Lane's pavement), so where its stored place
    // reaches the pavement's zones it moves north along the canal, a metre at a time up to 20 m, to the first place the street layer takes (its tables go with it)
    let pv = G.pavilion;
    for (let d = 0; d <= 20; d++) { const at: V = [G.pavilion.at[0], G.pavilion.at[1] + d]; if (put('G6-pavilion', 'pavilion', ['DecoPavilion'], at, pv.yaw, lawn(at), { family: 'pavilion', quiet: d < 20 })) { pv = { ...pv, at }; if (d) log.gaps.push({ rule: 'G6-pavilion', why: `moved ${d} m north of its stored place (${G.pavilion.at.join(', ')}): the street's zones reach it there` }); break; } } // the old pavilion takes the place of our curved kiosk on its lawn
    for (let k = 0; k < 4; k++) put('G6-cafe', 'cafe-set', ['CafeSet', 'CafeSet_Tulip'], [pv.at[0] + 7.5, pv.at[1] - 8 + 5.5 * k], k * 41, lawn(pv.at), { family: 'garden' });
    put('G6-cart', 'kiosk-food', ['CoffeeCart'], [G.x[1] - 5, Pl.c[1] + 8.5], 180, 'gardens/gate', { family: 'kiosk-food' });
    // G5: shrub mounds and flower beds seeded over the open lawns (60 tries' worth, as the old seed 7), clear of the canal walks, the gate, the basin and the pavilion
    const r = rng('oldtown/gardens/beds'), plots = ctx.d.plots.filter(p => G.plots.includes(p.id)), boxes = plots.map(p => bbox(p.polyV));
    const bx: [number, number, number, number] = [Math.min(...boxes.map(b => b[0])), Math.max(...boxes.map(b => b[1])), Math.min(...boxes.map(b => b[2])), Math.max(...boxes.map(b => b[3]))];
    for (let placed = 0, tries = 0; placed < 60 && tries < 2000; tries++) {
      const q: V = [bx[0] + 3 + r.f() * (bx[1] - bx[0] - 6), bx[2] + 3 + r.f() * (bx[3] - bx[2] - 6)], shrub = r.f() < 0.6, yaw = r.f() * 360;
      if (!plots.some(p => inPoly(q, p.polyV))) continue;
      if (Math.abs(q[0] - C.x) < C.half + 11 && q[1] < C.y1 + 6 && q[1] > C.y0 - 6) continue; // canal walks
      if (Math.abs(q[1] - Pl.c[1]) < 9 || dist(q, Pl.c) < Pl.r + 12) continue; // gate, basin
      if (dist(q, [pv.at[0] + 3, pv.at[1]]) < 16) continue; // pavilion
      if (put('G5-bed', shrub ? 'shrub' : 'flower-bed', [shrub ? 'ShrubMound' : 'FlowerBed'], q, yaw, lawn(q), { family: 'garden' })) placed++;
    }
  }

  // ---- The harbour (DecoKitDistrictDressing.Harbour, DecoKitHarbour mooring bollards)
  const H = g.harbour;
  if (H) {
    const z0 = oz(-300), z1 = oz(380), qy = (y: number) => !H.piers.some(([a, b]) => y > a - 3 && y < b + 3);
    // H1: café terraces against the east blocks' sea walls (old z 20, 104, 254), four tables, screens on the walk side, ferns at the ends, a board out front
    for (const z of [20, 104, 254]) {
      const y = oz(z), back = seaWall(o.buildings, y, H.terrace[0]);
      if (back === undefined) { log.gaps.push({ rule: 'H1-terrace', why: `no sea wall at y ${y}` }); continue; }
      terrace(put, 'H1-terrace', [back, y], [0, 1], [1, 0], 4, z, true, 'harbour/terrace');
    }
    // H2: lamps at the terrace's edge every 20 m, a round bench every other one
    let k = 0;
    for (let y = z0 + 10; y < z1; y += 20, k++) {
      put('H2-lamp', 'lamp', ['Streetlamp'], [H.steps[0] - 0.8, y], 90, 'harbour/terrace', { family: 'lamp' });
      if ((k & 1) === 0) put('H2-bench', 'round-bench', ['RoundBench', 'Bench'], [H.steps[0] - 3, y + 10], 90, 'harbour/terrace', { family: 'bench' });
    }
    // H3: the promenade's seaside kit every 24 m (old z -300..700) clear of the pier roots: a lamp at the quay edge, then a lifebuoy, a coin telescope or a bench.
    // The old row stood 2.2 m in from the coping; the promenade walk's clear middle (SF-1: 2 m edge bands on a 16 m walk) leaves the lamps at the edge band and
    // the lifebuoys and telescopes on its last half metre clear of the edge (SF-14), and the benches on the landward band at the steps' foot.
    k = 0;
    for (let z = -300; z < 700; z += 24, k++) {
      const y = oz(z); if (!qy(y) || y < -552 || y > 288) continue;
      const lip = H.promenade[1];
      put('H3-lamp', 'lamp', ['Streetlamp'], [lip - 0.65, y], 90, 'harbour/quay', { family: 'lamp' });
      if (k % 3 === 0) put('H3-lifebuoy', 'lifebuoy', ['LifebuoyStand'], [lip - 1.15, y + 4], -90, 'harbour/quay');
      else if (k % 3 === 1) put('H3-telescope', 'telescope', ['CoinTelescope'], [lip - 1.15, y + 5], 90, 'harbour/quay');
      else put('H3-bench', 'bench', ['Bench'], [H.promenade[0] + 1, y + 6], 90, 'harbour/quay', { family: 'bench' });
    }
    // H4: mooring bollards on the coping every 16 m, clear of the piers (DecoKitHarbour)
    for (let y = z0 + 6; y < z1; y += 16) if (qy(y)) put('H4-mooring', 'mooring-cleat', ['Bollard'], [H.quay - 0.9, y], 0, 'harbour/quay');
    // H5: a row of deckchairs by the market pier; H6: a drinking fountain on the promenade, a pylon board facing the sea
    for (let d = 0; d < 6; d++) put('H5-deckchair', 'deckchair', ['Deckchair'], [H.promenade[0] + 0.6 + (d % 2) * 0.8, oz(30) + d * 1.9], 80 + d * 6, 'harbour/quay');
    put('H6-fountain', 'drinking-fountain', ['DrinkingFountain'], [H.promenade[0] + 1, oz(140)], -90, 'harbour/quay', { family: 'drinking-fountain' });
    // (the old pylon board on the promenade, old z 52, is billboards.ts's BB-3 quay pylon since 075-F: not placed twice)
  }

  // ---- Crown Parade (DecoKitDistrictDressing.Parade): pylon boards frame the vista, planters line the axis, a kiosk, a cart, a clock and benches
  {
    const c = cc.c, EastBack = 46, at = (x: number, dz: number): V => [x, c[1] + dz];
    // (the vista's pylon pair is billboards.ts's BB-3 parade pair since 075-F: not placed twice)
    for (let x = 24; x < EastBack - 1; x += 5) for (const s of [-1, 1]) {
      const urn = ((x - 24) % 10) < 1;
      put('P-planter', urn ? 'planter' : 'tub-palm', urn ? ['Planter_Urn', 'OvalPlanter'] : ['PottedPalm_Small', 'PottedTopiary'], at(x, s * 9), x * 11, 'parade', { family: 'planter' });
    }
    put('P-clock', 'public-clock', ['StreetClock'], at(EastBack - 4, 5), 0, 'parade', { family: 'public-clock' });
    put('P-fountain', 'drinking-fountain', ['DrinkingFountain'], at(22, -11), 90, 'parade', { family: 'drinking-fountain' });
    put('P-kiosk', 'kiosk-pavilion', ['CurvedKiosk'], at(30, -20), 0, 'parade');
    put('P-cart', 'kiosk-food', ['CoffeeCart'], at(31, 20), 180, 'parade', { family: 'kiosk-food' });
    put('P-bench', 'bench', ['Bench'], at(27, -12.5), 0, 'parade', { family: 'bench' });
    put('P-bench', 'bench', ['Bench'], at(37, 12.5), 180, 'parade', { family: 'bench' });
  }

  // ---- Crown Circus (DecoKitDistrictDressing.Circus): advertising columns at the four boulevard corners, cafés under the SE and NW crescents
  {
    const c = cc.c, R = cc.pavement, ring = cc.ring[1], phi0 = (Math.asin(18 / R) * 180) / Math.PI, step = (2 * Math.asin(3 / R) * 180) / Math.PI;
    const dir = (phi: number): V => [Math.sin((phi * Math.PI) / 180), Math.cos((phi * Math.PI) / 180)];
    for (const phi of [phi0 + 3, -phi0 - 3, 180 - phi0 - 3, 180 + phi0 + 3]) put('C-column', 'advertising-column', ['AdColumn'], [c[0] + dir(phi)[0] * (ring + 3.5), c[1] + dir(phi)[1] * (ring + 3.5)], 0, 'circus', { family: 'advertising-column' });
    for (const phi of [180 - phi0 - 1.5 * step, -phi0 - 1.5 * step]) for (let k = 0; k < 3; k++) {
      const d = dir(phi - step * 0.9 * k);
      put('C-cafe', 'cafe-set', ['CafeSet_Tulip', 'CafeSet'], [c[0] + d[0] * (R - 3), c[1] + d[1] * (R - 3)], phi * 7 + k * 50, 'circus');
    }
  }

  // ---- The Boulevard (DecoKitDistrictDressing.Boulevard; the cinema marquee and blade signs are the frontage's signs): café terraces, vending machines, the
  //      pneumatic post, the automat, advertising columns on the kerb (old kerb + 3), the clock between the stop canopies
  {
    const e = 18, w = -18, kerb = (ctx.d.roads.find(r => r.id === 'boulevard')?.w ?? 2 * P.oldKerbX) / 2, kx = (x: number) => Math.sign(x) * (Math.abs(x) + kerb - P.oldKerbX), b = 'boulevard';
    const fz = (p: V) => { const r = roadAt(ctx as never, p, b); return r ? Math.max(0, r.sec.pavement - r.sec.kerb - r.sec.walk) : Infinity; }; // the frontage zone's depth at the wall
    terrace(put, 'B-terrace', [e, oz(37)], [0, 1], [-1, 0], 3, 1, true, b, fz([e - 0.5, oz(37)]));
    put('B-vending', 'vending-machine', ['VendingMachine_Cola'], [e - 0.42, oz(15.1)], -90, b);
    put('B-vending', 'vending-machine', ['VendingMachine_Snack'], [e - 0.42, oz(16.3)], -90, b);
    put('B-post', 'pneumatic-post', ['PneumaticPost'], [e - 0.6, oz(22.5)], -90, b);
    put('B-column', 'advertising-column', ['AdColumn'], [kx(P.oldKerbX + 3), oz(84)], 0, b, { family: 'advertising-column' });
    put('B-automat', 'automat', ['Automat'], [e - 0.27, oz(149.5)], -90, b);
    put('B-clock', 'public-clock', ['StreetClock'], [-5, oz(112)], 0, b, { family: 'public-clock' });
    terrace(put, 'B-terrace', [e, oz(262)], [0, 1], [-1, 0], 3, 2, true, b, fz([e - 0.5, oz(262)]));
    put('B-column', 'advertising-column', ['AdColumn'], [kx(-P.oldKerbX - 3), oz(12)], 0, b, { family: 'advertising-column' });
    put('B-vending', 'vending-machine', ['VendingMachine_Snack'], [w + 0.42, oz(161.1)], 90, b);
    put('B-vending', 'vending-machine', ['VendingMachine_Cola'], [w + 0.42, oz(162.3)], 90, b);
    put('B-post', 'pneumatic-post', ['PneumaticPost'], [w + 0.6, oz(140)], 90, b);
    terrace(put, 'B-terrace', [w, oz(294)], [0, 1], [1, 0], 3, 3, false, b, fz([w + 0.5, oz(294)]));
  }

  // ---- The boulevard's frontage (DecoKitStreetLife.Frontage): along each unbroken run of bays, urns flanking bays 1, 5, 9 .., a trough under bays 3, 7 ..,
  //      topiary where a run ends. Bays are the street pieces fronting an old-street road (o.frontBays, south to north / along the edge).
  if (o.frontBays?.length) {
    const byRoad = new Map<string, { a: V; b: V; road: string }[]>();
    for (const f of o.frontBays) byRoad.set(`${f.road}|${Math.sign((f.a[0] + f.b[0]) / 2)}`, [...(byRoad.get(`${f.road}|${Math.sign((f.a[0] + f.b[0]) / 2)}`) ?? []), f]);
    for (const [key, list] of byRoad) {
      const c = list.map(f => ({ m: [(f.a[0] + f.b[0]) / 2, (f.a[1] + f.b[1]) / 2] as V, u: unit(f.a, f.b) })).sort((p, q) => p.m[1] - q.m[1] || p.m[0] - q.m[0]);
      for (let i = 0; i < c.length;) {
        let j = i; while (j + 1 < c.length && dist(c[j + 1]!.m, c[j]!.m) < 6.5) j++;
        const n0 = inward(c[i]!.u, c[i]!.m, o.buildings), psi = (n: V) => (Math.atan2(n[0], n[1]) * 180) / Math.PI;
        for (let q = i; q <= j; q++) {
          const { m: mid, u } = c[q]!, n = inward(u, mid, o.buildings), along = (t: number, off: number): V => [mid[0] + u[0] * t + n[0] * off, mid[1] + u[1] * t + n[1] * off];
          if ((q - i) % 4 === 1) for (const t of [-2.2, 2.2]) put('F-urn', 'planter', ['Planter_Urn'], along(t, 0.45), psi(n), `frontage/${key}`, { family: 'planter' });
          else if ((q - i) % 4 === 3) put('F-trough', 'planter', ['Planter_Trough'], along(0, 0.4), psi(n), `frontage/${key}`, { family: 'planter' });
        }
        const e0 = c[i]!, e1 = c[j]!, n1 = inward(e1.u, e1.m, o.buildings);
        put('F-topiary', 'planter', ['PottedTopiary'], [e0.m[0] - e0.u[0] * 2.2 + n0[0] * 0.6, e0.m[1] - e0.u[1] * 2.2 + n0[1] * 0.6], psi(n0), `frontage/${key}`, { family: 'planter' });
        if (j > i) put('F-topiary', 'planter', ['PottedTopiary'], [e1.m[0] + e1.u[0] * 2.2 + n1[0] * 0.6, e1.m[1] + e1.u[1] * 2.2 + n1[1] * 0.6], psi(n1), `frontage/${key}`, { family: 'planter' });
        i = j + 1;
      }
    }
  }
  return log;
}

const unit = (a: V, b: V): V => { const d = Math.hypot(b[0] - a[0], b[1] - a[1]) || 1; return [(b[0] - a[0]) / d, (b[1] - a[1]) / d]; };
/** The facade's outward normal (away from its building): the side of the bay line no ground mass stands on. */
function inward(u: V, mid: V, buildings: { ring: V[]; base: number }[]): V {
  const n: V = [-u[1], u[0]], probe: V = [mid[0] + n[0] * 1.5, mid[1] + n[1] * 1.5];
  return buildings.some(b => b.base < 1 && inPoly(probe, b.ring)) ? [-n[0], -n[1]] : n;
}

/** The east face (largest x below the quay's landward edge + 4) of the ground masses that the line y crosses: the blocks' sea wall at y. */
function seaWall(buildings: { ring: V[]; base: number }[], y: number, quayX: number): number | undefined {
  let best: number | undefined;
  for (const b of buildings) {
    if (b.base > 1) continue;
    const bb = bbox(b.ring); if (y < bb[2] + 1 || y > bb[3] - 1 || bb[1] > quayX + 4 || bb[1] < quayX - 30) continue;
    best = Math.max(best ?? -Infinity, bb[1]);
  }
  return best;
}

type Put = (rule: string, kind: string, want: string[], at: V, psi: number, edge: string, opt?: Opt) => boolean;
/** DecoKitDistrictDressing.Terrace: tables in a row under the canopy (4.5 m apart, 2.4 m off the wall), screen tiles (2 m) on the walk side 4.6 m off, potted ferns
 *  at its ends, a sandwich board out front. `start` on the wall, `along` its direction, `n` into the street. */
function terrace(put: Put, rule: string, start: V, along: V, n: V, tables: number, seed: number, screen: boolean, edge: string, depth = Infinity): void {
  // the old terrace was 4.6 m deep (screens at 4.6 off the wall on the old 10 m pavement); where the street's frontage zone is shallower (`depth`, SF-1 keeps the walk
  // zone clear) the tables stand at its middle and the screens and the board stay out (skip, never squeeze)
  const psi = (Math.atan2(n[0], n[1]) * 180) / Math.PI, len = 4.5 * (tables - 1), at = (t: number, off: number): V => [start[0] + along[0] * t + n[0] * off, start[1] + along[1] * t + n[1] * off];
  const tOff = Math.min(2.4, depth / 2), deep = depth >= 5;
  for (let k = 0; k < tables; k++) put(rule, 'cafe-set', k % 3 === 2 ? ['BistroSet', 'CafeSet_Tulip', 'CafeSet'] : ['CafeSet_Tulip', 'CafeSet'], at(4.5 * k, tOff), seed * 17 + k * 41, edge);
  if (screen && deep) for (let t = -1; t <= len + 1.01; t += 2) put(rule, 'terrace-screen', ['TerraceScreen'], at(t + 1, 4.6), psi, edge);
  for (const t of [-2, len + 2]) put(rule, 'tub-palm', ['PottedFern'], at(t, Math.min(1.2, depth / 2)), seed * 29 + t, edge, { family: 'planter' });
  if (deep) put(rule, 'sandwich-board', ['SandwichBoard'], at(-2.5, 4.2), psi + 20, edge);
}
