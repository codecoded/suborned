// The geometry report (brief 055, Splice): one self-contained HTML (maps and pictures inline) and the numbers in geometry.json. Written to out/geometry/.
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { World } from '../../contracts/world/world.schema';
import type { V } from '../plan/geom';
import type { GroundAudit, Group, Route, SightAudit, WalkAudit } from './audit';
import { colours, mapHtml, renderMap } from './heat';
import { ROOT } from './pieces';
import type { Sim } from './sim';
import type { Shot } from './shots';
import type { Visible } from './visible';

export type ReportIn = {
  sim: Sim; vis: Visible; world: World; ground: GroundAudit; walk: WalkAudit; sight: SightAudit; routes: { list: Route[] };
  summary: unknown; shots?: Shot[]; worldSha: string; stale: boolean; unmeasured: string[];
  kit?: KitRow[];
};
export type KitRow = { id: string; status: string; category: string; minY: number; sizeDelta: number; tags: string; used: boolean; why: string };

const esc = (s: unknown) => String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
const table = (head: string[], rows: (string | number)[][], cls = '') => `<table class="${cls}"><thead><tr>${head.map(h => `<th>${esc(h)}</th>`).join('')}</tr></thead><tbody>${rows.map(r => `<tr>${r.map(c => `<td>${typeof c === 'string' && c.startsWith('<') ? c : esc(c)}</td>`).join('')}</tr>`).join('')}</tbody></table>`;
const at = (b: V) => `(${b[0].toFixed(1)}, ${b[1].toFixed(1)})`;
const gtable = (gs: Group[], n = 14) => table(['what', 'kind', 'items', 'cells', 'area m²', 'worst (m)', 'mean (m)', 'where (block x, y)'], gs.slice(0, n).map(g => [g.id, g.kind, g.instances, g.count, g.area, g.max, g.mean, at(g.worst)]));

export function writeReport(r: ReportIn) {
  if (!r.sim) return;
  const { sim, vis, ground, walk, sight } = r, out = resolve(ROOT, 'out/geometry');
  mkdirSync(out, { recursive: true });
  const box = { x0: sim.window.x0, y0: sim.window.y0, x1: sim.window.x1, y1: sim.window.y1 };
  const polyOf = (id: string) => sim.occluders.find(o => o.id === id)?.poly;
  const mk = (b: V, label: string, colour: string) => ({ b, label, colour });
  const keyPlaces = sim.places.filter(p => /^d:|^crew:|^cabinet$|^patrol-a$|^patrol-b$|^security-entry$|^rival-entry$/.test(p.id)).map(p => mk(p.at, p.id.replace(/^d:/, ''), '#1b4fd8'));
  mkdirSync(resolve(out, 'maps'), { recursive: true });
  const save = (name: string, m: ReturnType<typeof renderMap>) => { writeFileSync(resolve(out, 'maps', `${name}.png`), Buffer.from(m.png, 'base64')); return m; };
  let mapN = 0;
  const full = (layers: Parameters<typeof renderMap>[4], caption: string, markers = keyPlaces, outlines: Parameters<typeof renderMap>[5] = []) => mapHtml(save(`map-${++mapN}`, renderMap(sim, vis, box, 6, layers, outlines)), markers, caption);
  const occOutlines = sim.occluders.filter(o => o.h > 1 && polyOf(o.id)).map(o => ({ poly: o.poly, colour: [30, 90, 200] as [number, number, number] }));
  const crop = (b: V, half: [number, number], layers: Parameters<typeof renderMap>[4], caption: string, markers: ReturnType<typeof mk>[] = []) => mapHtml(save(`crop-${++mapN}`, renderMap(sim, vis, { x0: b[0] - half[0], x1: b[0] + half[0], y0: b[1] - half[1], y1: b[1] + half[1] }, 22, layers, occOutlines)), markers, caption);

  const edgeOf = (o: string) => /\/kerb$|^plot\/|^road\/|^foot\/|^civic\/(drive|bay)\/|^street\/capstone\/(kerb|tree-pit)/.test(o);
  const edgeLayer = { cells: ground.cells.filter(c => edgeOf(c.owner)).map(c => ({ b: c.b, v: c.delta })), size: 0.5, colour: () => [200, 190, 150] as [number, number, number], alpha: 0.7 };
  const sunkLayer = { cells: ground.cells.filter(c => !edgeOf(c.owner)).map(c => ({ b: c.b, v: c.delta })), size: 0.5, colour: colours.sink };
  const maps = {
    g1: full([edgeLayer, sunkLayer], 'G-1: free walk cells where the host floor is not the drawn floor. Tan: kerb stones and slab edges the 1 m height grid puts on the wrong side (16 to 19 cm). Yellow to red: the person stands that deep in the floor (5 cm to 0.8 m); blue: the host holds them above the drawn floor. Blue lines: the engine\'s occluders.', keyPlaces, occOutlines),
    g2: full([{ cells: walk.throughCells.map(c => ({ b: c.b })), size: 0.5, colour: colours.through }, { cells: walk.invisibleCells.map(c => ({ b: c.b })), size: 0.5, colour: colours.invisible }, { cells: walk.ceilingCells.map(c => ({ b: c.b })), size: 0.5, colour: colours.ceiling }], 'G-2: magenta, a cell the walk grid calls free that is inside a drawn solid; violet, free under something lower than 1.8 m; cyan, blocked by a sim solid with nothing drawn near it.', keyPlaces),
    g3: full([{ cells: sight.cells.filter(c => c.kind === 'chest').map(c => ({ b: c.b })), size: 0.25, colour: colours.chest }, { cells: sight.cells.filter(c => c.kind === 'eye').map(c => ({ b: c.b })), size: 0.25, colour: colours.eye }], 'G-3: orange, drawn solid at eye height (1.6 m) that is in no occluder; yellow, drawn solid at chest height (1.2 m) that is in none. Blue outlines: the occluders the engine has.', keyPlaces, occOutlines),
  };
  const guardAt = sim.places.find(p => p.id === 'd:Office entrance')!.at;
  const officeCrop = crop(guardAt, [8, 11], [edgeLayer, sunkLayer, { cells: walk.throughCells.map(c => ({ b: c.b })), size: 0.5, colour: colours.through }], 'The Office front at 22 px per metre: the landing and its six steps (red: floor deeper than the host knows).', [mk(guardAt, 'guard post', '#1b4fd8'), mk(sim.places.find(p => p.id === 'd:Office approach')!.at, 'approach', '#1b4fd8')]);
  const cafeAt = sim.places.find(p => p.id === 'd:Café door')!.at;
  const cafeCrop = crop([cafeAt[0] + 8, cafeAt[1] + 4], [14, 22], [edgeLayer, sunkLayer, { cells: walk.throughCells.map(c => ({ b: c.b })), size: 0.5, colour: colours.through }, { cells: sight.cells.filter(c => c.kind === 'eye').map(c => ({ b: c.b })), size: 0.25, colour: colours.eye }], 'The café and its terrace at 22 px per metre.', sim.places.filter(p => /^cafe-table-\d-seat-0$|^d:Café door$/.test(p.id)).map(p => mk(p.at, p.id.replace('cafe-table-', 't').replace('-seat-0', ''), '#1b4fd8')));

  // numbers for the headline
  const guard = ground.places.find(p => p.id === 'd:Office entrance')!;
  const bandRows = Object.entries(ground.bands).map(([k, v]) => [k, v, (v * 0.25).toFixed(0) + ' m²']);
  const pieceRows = ground.byOwner.filter(g => !/kerb|ground layer|tree-pit/.test(g.id)).slice(0, 12);
  const kerbCells = ground.byOwner.filter(g => /kerb|ground layer|tree-pit/.test(g.id)).reduce((s, g) => s + g.count, 0);
  const rd = r.world.ground.roads[0], plot0 = r.world.ground.plots[0], foot0 = r.world.ground.foot[0], flat0 = r.world.ground.flats.find(f => f.id.startsWith('civic/bay'));
  const heightRows: (string | number)[][] = [
    ['plot slab (pavement, court, setback)', (plot0!.base + plot0!.height).toFixed(2), '0.18', `ground.plots (${r.world.ground.plots.length} slabs)`],
    ['carriageway', rd!.level.toFixed(2), '0 (outside every slab)', 'ground.roads[].level'],
    ['kerb strip (0.3 m)', Math.max(rd!.level + rd!.kerbHeight, 0.18 + 0.004).toFixed(3), '0 (the strip is outside the slab)', 'roads[].kerbHeight 0.168, lifted to the slab top by Layers.Ground; kerb stones 0.185'],
    ['bays and drives', flat0 ? flat0.level.toFixed(2) : '0.02', '0.02 only where a drive cuts the slab; the other bays read the slab', 'ground.flats, ground.drives'],
    ['foot path ribbon', foot0 ? foot0.level.toFixed(2) : '0.02', '0 or 0.18', 'ground.foot[].level'],
    ['Office public landing, six steps', '0.93 (0.75 above the slab)', '0.18', 'RecordsEntrance mesh; mission.json prose'],
    ['Office official door landing', '0.48 (0.30 above the slab)', '0.18', 'RecordsEntrance_Official mesh'],
    ['tree collars, planter beds, lamp feet', '0.18 + 0.07 to 0.69', '0.18', 'street items (kit)'],
    ['deck / sky concourse', r.world.levels.map(l => l.height).filter(h => h > 0).join(' / '), 'no host height above 0.18', 'standards.levels; none inside the window'],
  ];
  const places = ground.places.filter(p => p.src === 'site' && Number.isFinite(p.delta)).sort((a, b) => Math.abs(b.delta) - Math.abs(a.delta)).slice(0, 8);
  const seatPts = ground.places.filter(p => /^cafe-table-\d-seat-\d$/.test(p.id));
  const routeRows = r.routes.list.filter(x => x.sunk || x.through).sort((a, b) => Math.abs(b.sunkMax) - Math.abs(a.sunkMax)).slice(0, 14).map(x => [x.id, x.length, x.sunk, x.sunkMax, x.through, x.throughOwners.join(', ')]);
  const routeTotals = (() => { const n = r.routes.list.length; return { n, sunk: r.routes.list.filter(x => Math.abs(x.sunkMax) > 0.05).length, deep: r.routes.list.filter(x => Math.abs(x.sunkMax) >= 0.5).length, through: r.routes.list.filter(x => x.through > 0).length }; })();
  const cover = walk.solidsCover.slice(0, 10).map(s => [s.id, s.area, s.covered, s.share, s.drawnAs || 'nothing']);
  const wallRows = sight.walls.filter(w => w.perimeter > 0).map(w => [w.id, w.h, w.area, w.perimeter, w.walled, w.share, w.gaps.slice(0, 2).map(g => `${g.len} m at ${at(g.from)}`).join('; ') || '-']);
  const eyeKinds = (gs: Group[]) => gs.filter(g => !/^frontage|^facade/.test(g.id)).slice(0, 12);
  const lv = r.world.levels.map(l => [l.id, l.height, l.decks.length, l.stairs.length]);
  const conn = new Map<string, number>(); for (const c of r.world.connectors) conn.set(c.kind, (conn.get(c.kind) ?? 0) + 1);
  const inWin = (ring: V[]) => ring.some(p => { const b = sim.frame.toBlock(p); return b[0] >= sim.window.x0 && b[0] <= sim.window.x1 && b[1] >= sim.window.y0 && b[1] <= sim.window.y1; });
  const decksHere = r.world.levels.reduce((n, l) => n + l.decks.filter(d => inWin(d.ring as V[])).length, 0), connHere = r.world.connectors.filter(c => inWin(c.ring as V[])).length, podiums = r.world.buildings.filter(b => b.id.endsWith('/podium') && inWin(b.ring as V[])).length;
  const chk = vis.checks.filter(c => c.offset > 0.3).sort((a, b) => b.offset - a.offset).slice(0, 10).map(c => [c.id, c.piece, c.offset.toFixed(2)]);
  const kit = r.kit ?? [];
  const kitBad = kit.filter(k => k.why);

  const shotsHtml = (r.shots ?? []).map(s => `<figure><img src="data:image/jpeg;base64,${readFileSync(resolve(out, s.file)).toString('base64')}"><figcaption><b>${esc(s.title)}</b> (camera yaw ${s.yaw}, pitch 38). ${esc(s.caption)}</figcaption></figure>`).join('');
  const css = `body{font:14px/1.45 -apple-system,Helvetica,Arial,sans-serif;margin:24px auto;max-width:1180px;color:#1b1b1b;padding:0 16px}h1{font-size:26px}h2{margin-top:36px;border-bottom:2px solid #1b4fd8;padding-bottom:4px}h3{margin-top:22px}table{border-collapse:collapse;margin:8px 0;font-size:12.5px}th,td{border:1px solid #cfcfcf;padding:3px 8px;text-align:left}th{background:#f1f1ee}tr:nth-child(even) td{background:#fafaf8}.cards{display:flex;gap:12px;flex-wrap:wrap}.card{border:1px solid #cfcfcf;border-radius:6px;padding:8px 14px;min-width:150px}.card b{font-size:22px;display:block}figure{margin:12px 0}figure.map{display:inline-block;vertical-align:top;margin-right:16px}img{max-width:100%}figcaption{font-size:12px;color:#444;max-width:1000px}code{background:#f1f1ee;padding:1px 4px;border-radius:3px}.bad{color:#b3261e;font-weight:600}.note{background:#fff8e1;border-left:4px solid #e0a800;padding:6px 12px;margin:10px 0}`;
  const html = `<!doctype html><html lang="en"><head><meta charset="utf-8"><title>Geometry audit, brief 055</title><style>${css}</style></head><body>
<h1>Levels and collision: the geometry audit</h1>
<p>Brief 055 (Splice). World export <code>${r.worldSha.slice(0, 12)}</code>${r.stale ? ' <span class="bad">(older than the plan: run bun run world)</span>' : ''}; the civic block's site window (block x ${sim.window.x0}..${sim.window.x1} m, y ${sim.window.y0}..${sim.window.y1} m; x runs south, y east). Kit pieces measured from the shipped meshes by Blender (${r.unmeasured.length ? 'unmeasured: ' + r.unmeasured.join(', ') : 'all measured'}). Nothing here edits the world, the sim or Unity.</p>
<div class="note"><b>Sky's screenshot is the door guard at the Office public entrance.</b> The sim puts him at <code>d:Office entrance</code> ${at(guard.at)}; the host stands him at ${guard.host.toFixed(2)} m (the plot slab); the world draws the public landing, ${guard.rise.toFixed(2)} m up six steps, under him: floor ${guard.top.toFixed(2)} m. He is ${guard.delta.toFixed(2)} m too low, to the hips. Cause: the host bakes ground heights from the plot slabs only (<code>PlayInstall.Heights</code>), so no stair, landing, terrace or kerb stone is in the heights.</div>
<div class="cards">
<div class="card"><b>${ground.sunk + ground.floating}</b>walk cells (${((ground.sunk + ground.floating) * 0.25).toFixed(0)} m²) where a person stands more than 5 cm off the drawn floor, of ${ground.walkCells}</div>
<div class="card"><b>${r.routes.list.length ? routeTotals.sunk : 0} / ${routeTotals.n}</b>mission routes cross such a stretch; ${routeTotals.deep} cross one 0.5 m or deeper</div>
<div class="card"><b>${walk.throughSolid}</b>free walk cells inside a drawn solid (${(walk.throughSolid * 0.25).toFixed(0)} m²); ${walk.lowCeiling} under a ceiling lower than 1.8 m</div>
<div class="card"><b>${walk.invisibleWall}</b>blocked cells with nothing drawn near (sim solids that are not there)</div>
<div class="card"><b>${sight.eyeMissing}</b>cells of drawn solid at eye height that stop no sightline or shot (${(sight.eyeMissing * 0.0625).toFixed(0)} m²)</div>
</div>

<h2>G-1  Where people stand against the floor drawn there</h2>
<p><b>How the game places a person:</b> the sim knows only x and y. The host stands each figure at <code>SiteFrame.Height(x, y)</code>, which reads a 1 m grid baked once from the plot slabs (0.18 m) and the drive and bay cuts (0.02 m): ${ground.hostCheck.hostOnSlab} of ${ground.hostCheck.cells} cells read 0.18 m, the rest 0 or 0.02. Stairs, landings, terraces, tree-pit collars, kerb stones and bay edges are not in it. The drawn floor: the ground layers and every kit piece's walkable top, a stair's treads and risers bridged, reached by steps of 0.35 m or less from open ground.</p>
${table(['band', 'cells', 'area'], bandRows)}
<p>Causes: ${Object.entries(ground.byCause).map(([k, v]) => `${v} cells, ${esc(k)}`).join('; ')}. The kerb and edge rows (${kerbCells} cells) are 0.16 to 0.19 m: a kerb stone or a slab edge the 1 m grid puts on the wrong side. The rows below are the drawn pieces that matter.</p>
${gtable(pieceRows, 10)}
<h3>The heights the world draws under people</h3>
${table(['surface', 'drawn height (m)', 'the host stands people at', 'where it comes from'], heightRows)}
<h3>The places the mission uses</h3>
${table(['place', 'at (block)', 'host floor', 'drawn floor', 'off by', 'floor is'], places.map(p => [p.id, at(p.at), p.host.toFixed(2), p.top.toFixed(2), p.delta.toFixed(2), p.owner]))}
<h3>The routes people walk</h3>
<p>Every mission route (guard, patrol, councillor, driver, the four crew to each door, rivals, security, the waitress to her tables, the final fight) walked on the sim grid: ${routeTotals.sunk} of ${routeTotals.n} cross a stretch where the floor is more than 5 cm off; every crew route to the Office door and the guard's walk to the café and back climb the landing.</p>
${table(['route', 'length m', 'cells off', 'worst (m)', 'cells in a solid', 'solid'], routeRows)}
<h3>The café seats</h3>
<p>The ${seatPts.length} seat points (<code>cafe-table-N-seat-M</code>) lie inside the tables' collision solids by design: a seated person is on a chair. The drawn surface under them is ${Math.min(...seatPts.map(p => p.rise)).toFixed(2)} to ${Math.max(...seatPts.map(p => p.rise)).toFixed(2)} m over the pavement (a chair seat, the table top), and the host plays the seated clip at the pavement height. Not counted in G-1; a seat needs its own height when the chairs are measured.</p>
<div style="display:flex;flex-wrap:wrap">${maps.g1}${officeCrop}</div>

<h2>G-2  Where people walk against what is drawn</h2>
<p>The walk grid (0.5 m cells, 164 x 328) is made by <code>walk-grid.ts</code> from three things: the buildings' lines, the civic footprints, and the civic block's own ground solids (<code>civic-block.json</code> dressing: beds, tables, fountain, planter runs, pots, bollards, lamp bases) with a 0.65 m clearance. Nothing the world generator places in the street is in it. Measured against the drawn world: ${walk.throughSolid} free cells (${(walk.throughSolid * 0.25).toFixed(0)} m²) lie inside a drawn solid and ${walk.brush} more have a wall within a body's width.</p>
${gtable(walk.through, 18)}
<h3>Solids the sim keeps against what is drawn there</h3>
${table(['sim solid', 'area m²', 'drawn m²', 'share', 'drawn as'], cover)}
<p>Blocked with nothing drawn within a body of it: ${walk.invisible.map(g => `${g.id} ${g.count} cells`).join(', ') || 'none'}.</p>
<h3>Under a low ceiling</h3>
${gtable(walk.ceiling, 6)}
<h3>Kit pieces whose mesh stands off their ring</h3>
<p>For a civic kit mass the ring in the export is the footprint the sim reads; the mesh is what is drawn. Largest differences (m):</p>
${table(['mass', 'piece', 'mesh edge off ring'], chk)}
<div style="display:flex;flex-wrap:wrap">${maps.g2}${cafeCrop}</div>

<h2>G-3  What stops a sightline or a shot against what is drawn</h2>
<p>The engine asks one thing: does an occluder span eye height (${sight.eye} m above the feet). Its occluders are the buildings, the plots behind their lines, and four cover blockers (the fountain, the civic robot, the two pavilion supports). Every other drawn object is invisible to a look or a shot. Cover points lower a hit chance only (planter runs 30, walls 28, cars 14, the fountain 11, cabinets 9, piers 8); none stops a line.</p>
${table(['occluder', 'height m', 'area m²', 'outline m', 'walled m', 'share', 'longest gaps'], wallRows)}
<p>The fountain occluder blocks a ${sight.walls.find(w => w.id === 'cover:civic/atom-fountain')?.area ?? '?'} m² disc 4.2 m high; the drawn fountain is a low basin with a thin central column, so a line across the basin is stopped by nothing drawn.</p>
<h3>Drawn at eye height, stops nothing (${sight.eyeMissing} cells outside every occluder)</h3>
${gtable(eyeKinds(sight.drawnEye), 12)}
<h3>Drawn at chest height, stops nothing (${sight.chestMissing} cells)</h3>
${gtable(eyeKinds(sight.drawnChest), 12)}
<div>${maps.g3}</div>

<h2>Levels</h2>
${table(['level', 'height m', 'decks', 'stairs'], lv)}
<p>Connectors in the world: ${[...conn].map(([k, v]) => `${v} ${k}`).join(', ')}. Inside the site window: ${decksHere} decks, ${connHere} connectors, ${podiums} building podiums (the world's podium top, 9.43 m, is the deck level); every person the Handover places stands on the street. The sim has no level: its positions are x and y, its occluders z and h from the pavement, its grid one layer. The host's height is the one thing that knows the floor, and it knows two values. Nothing above 0.18 m can hold a person today. The concourse stands at 24 m: a mission on a deck needs a level on every stand, route and occluder, and a height raster for each level.</p>

<h2>Kit pieces against the catalogue (K-1)</h2>
<p>${kit.length} catalogued pieces measured; ${kitBad.length} stand off their pivot or differ from the catalogued size, ${kitBad.filter(k => k.used).length} of them placed in this window. Flat paving modelled at 0.15 m (Pave_Slab, Pave_Flags, inlay arcs) is the case Vela and Latch already know: Latch lays them on their footprint (<code>CityLayers.FootLift</code>); the export places them at 0.181 and the catalogue says height 0.</p>
${table(['piece', 'status', 'category', 'mesh bottom (m)', 'size off (m)', 'tag', 'placed here', 'why'], kitBad.slice(0, 40).map(k => [k.id, k.status, k.category, k.minY.toFixed(3), k.sizeDelta.toFixed(3), k.tags, k.used ? 'yes' : '', k.why]))}

<h2>Pictures at the game's camera</h2>
<p>Blender clay renders of the placed kit pieces; red figures stand where the host puts them; green discs mark the drawn floor under them; blue outlines are the engine's occluders.</p>
${shotsHtml || '<p>(run without --no-shots to render them)</p>'}

<h2>Method</h2>
<p>Kit pieces: the shipped FBX of each piece, voxelised (0.125 m across, 0.05 m up) by Blender with three axis rays and a two-of-three vote, folded to 0.25 m columns of height intervals (tools/geometry/pieces.py), cached by the mesh's hash. Placement as Unity does it (KitPlacer, FacadeKit.Civic, CityLayers.Street). Masses, buildings, frontage boxes and the ground layers are prisms and ribbons from the export. Every number above reads the raster and the sim files; nothing is asked of Unity. Rules: tools/geometry/rules.ts; <code>bun run geometry --check</code> fails on them.</p>
</body></html>`;
  writeFileSync(resolve(out, 'report.html'), html);
}
