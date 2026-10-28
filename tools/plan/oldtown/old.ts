// Brief 075 (Sky 2026-10-28: "why can't we just remap the world around d handover using old buildings"): the old DecoKit town read from its C# sources
// (read only: ~/Projects/the-suborned/game/Assets/TheSuborned/Editor/DecoKit, or $OLD_DECOKIT). Nothing here is typed in from the old code: every constant,
// wall, short wall, crescent, Southside block, garden feature, landmark and sky rail point is parsed from the .cs files, so a change there shows here.
// The old frame is Unity's (x east, z north, metres; Crown Circus at (0, 206)). tools/plan/oldtown/transcribe.ts maps it into the plan.
import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { createHash } from 'node:crypto';

export const OLD_DIR = process.env.OLD_DECOKIT ?? join(process.env.HOME ?? '', 'Projects/the-suborned/game/Assets/TheSuborned/Editor/DecoKit');
export const FILES = ['DecoKitCityPlan.cs', 'DecoKitStreetLayout.cs', 'DecoKitCityBlocks.cs', 'DecoKitBlock.cs', 'DecoKitFrontage.cs', 'DecoKitCircus.cs', 'DecoKitSouthside.cs',
  'DecoKitGardens.cs', 'DecoKitStreetSkyline.cs', 'DecoKitSkyRail.cs', 'DecoKitHarbour.cs', 'DecoKitDecks.cs', 'DecoKitCityMass.cs'] as const;

const src = (f: string) => { const p = join(OLD_DIR, f); if (!existsSync(p)) throw new Error(`old DecoKit source missing: ${p} (set OLD_DECOKIT)`); return readFileSync(p, 'utf8'); };
/** The sources' sha256, so the transcription names exactly what it read. */
export const sources = () => Object.fromEntries(FILES.map(f => [f, createHash('sha256').update(src(f)).digest('hex')]));

// ------------------------------------------------------------------ a small C# expression reader (numbers, + - * /, parentheses, Mathf, P. / L. symbols)
type Sym = Record<string, number>;
const num = (s: string) => s.replace(/(\d+(?:\.\d+)?)f\b/g, '$1');
export function evalExpr(expr: string, sym: Sym): number {
  let e = num(expr.trim()).replace(/\bMathf\.(Sqrt|Asin|Acos|Sin|Cos|Abs)\b/g, (_, f) => `Math.${f.toLowerCase()}`).replace(/\bMathf\.Rad2Deg\b/g, String(180 / Math.PI)).replace(/\bMathf\.Deg2Rad\b/g, String(Math.PI / 180));
  e = e.replace(/\b(?:P|L|DecoKitCityPlan|DecoKitStreetLayout)\.(\w+(?:\.[xyz])?)/g, (_, k) => `S["${k}"]`);
  e = e.replace(/(?<![."\w])([A-Za-z_]\w*(?:\.[xyz])?)(?![\w(])/g, (m, k) => (k.startsWith('Math') || k === 'S' ? m : `S["${k}"]`));
  if (/[^\d\s.+\-*/()eE,\[\]"SMathsqrtincobl_A-Za-z]/.test(e)) throw new Error(`cannot read ${expr}`);
  const v = new Function('S', `"use strict"; return (${e});`)(new Proxy(sym, { get: (t, k: string) => { if (!(k in t)) throw new Error(`unknown symbol ${k} in ${expr}`); return t[k]; } }));
  if (typeof v !== 'number' || !Number.isFinite(v)) throw new Error(`${expr} is not a number`);
  return v;
}

/** Constants of a static class: `const float A = 1f, B = 2f;`, `static readonly Vector3 C = new(0f, 0f, 206f);`, `static float D => expr;` (in order, so later ones read earlier). */
function readConsts(text: string, sym: Sym) {
  const body = text.replace(/\/\/[^\n]*/g, '');
  const re = /(?:const|static readonly|static)\s+(float|Vector2|Vector3|int)\s+([^;]+?);/g;
  for (const m of body.matchAll(re)) {
    const [, type, decl] = m;
    if (/\(/.test(decl!.split('=')[0]!)) continue; // a method
    if (decl!.includes('=>')) { const [k, e] = decl!.split('=>'); try { sym[k!.trim()] = evalExpr(e!, sym); } catch { /* a property we do not need */ } continue; }
    if (type === 'Vector2' || type === 'Vector3') {
      for (const v of decl!.matchAll(/(\w+)\s*=\s*new(?:\s*Vector[23])?\s*\(([^)]*)\)/g)) {
        const parts = v[2]!.split(',').map(x => evalExpr(x, sym)), k = v[1]!;
        sym[`${k}.x`] = parts[0]!; if (type === 'Vector2') sym[`${k}.y`] = parts[1]!; else { sym[`${k}.y`] = parts[1]!; sym[`${k}.z`] = parts[2]!; }
      }
      continue;
    }
    for (const part of decl!.split(/,(?![^(]*\))/)) { const [k, e] = part.split('='); if (e === undefined) continue; try { sym[k!.trim()] = evalExpr(e, sym); } catch { /* not a constant we can read */ } }
  }
}

export type Seg = { cls: string; n: number };
export type Cap = 'Open' | 'Corner' | 'End' | 'Tower';
export type OldWall = { start: [number, number]; dir: 'N' | 'E' | 'S' | 'W'; normal: 'N' | 'E' | 'S' | 'W'; segs: Seg[]; caps: [Cap, Cap]; src: string };
export type OldShort = { x0: number; z: number; normal: 'N' | 'S'; west: string; east: string; src: string };
export type OldCrescent = { phi0: number; step: number; cls: string; count: number; endCap: boolean; src: string };
export type OldBlock = { name: string; x: number; z: number; phi: number; w: number; right: Seg[]; left: Seg[]; walk: { r: number; l: number; h: number; t: number }; d: number; src: string };
export type OldPut = { kit: string; at: [number, number, number]; yaw: number; scale: number; src: string };

const segsOf = (s: string): Seg[] => [...s.matchAll(/\("([\w]+)",\s*(\d+)\)/g)].map(m => ({ cls: m[1]!, n: Number(m[2]) }));
const flat = (t: string) => t.replace(/\/\/[^\n]*/g, '').replace(/\s+/g, ' ');

export function readOld() {
  const S: Sym = {};
  readConsts(src('DecoKitStreetLayout.cs').replace(/P\.EastBack/g, '46'), S); // SideEnd reads P.EastBack before the plan is read; re-read below
  readConsts(src('DecoKitCityPlan.cs'), S);
  readConsts(src('DecoKitStreetLayout.cs'), S);
  readConsts(src('DecoKitCityPlan.cs'), S); // CircusSouth / North read L.FrontX
  // DecoKitCityBlocks.Build's locals
  const cb = flat(src('DecoKitCityBlocks.cs'));
  const loc = cb.match(/float fx = ([^,]+), eb = ([^,]+), wb = ([^,]+), n0 = ([^;]+);/)!;
  S.fx = evalExpr(loc[1]!, S); S.eb = evalExpr(loc[2]!, S); S.wb = evalExpr(loc[3]!, S); S.n0 = evalExpr(loc[4]!, S);
  S.phi = Math.asin(S.fx! / S.CrescentR!) * 180 / Math.PI;
  const walls: OldWall[] = [...cb.matchAll(/Wall\(V\(([^,]+), ([^)]+)\), (\w), (\w), Seg\(((?:\("\w+", \d+\),? ?)+)\), Cap\.(\w+), Cap\.(\w+)\)/g)].map(m => ({
    start: [evalExpr(m[1]!, S), evalExpr(m[2]!, S)], dir: m[3] as OldWall['dir'], normal: m[4] as OldWall['normal'], segs: segsOf(m[5]!), caps: [m[6] as Cap, m[7] as Cap], src: m[0]!,
  }));
  const shorts: OldShort[] = [...cb.matchAll(/Short\(p, ([^,]+), ([^,]+), (\w), "(\w+)", "(\w+)"\)/g)].map(m => ({ x0: evalExpr(m[1]!, S), z: evalExpr(m[2]!, S), normal: m[3] as 'N' | 'S', west: m[4]!, east: m[5]!, src: m[0]! }));
  const crescents: OldCrescent[] = [...cb.matchAll(/Crescent\(p, P\.Circus, P\.CrescentR, ([^,]+), ([-\d.f]+), "(\w+)", (\d+), (true|false)\)/g)].map(m => ({
    phi0: evalExpr(m[1]!, S), step: evalExpr(m[2]!, S), cls: m[3]!, count: Number(m[4]), endCap: m[5] === 'true', src: m[0]!,
  }));
  const hall = [...cb.matchAll(/HallFill\(new Vector2\(([^,]+), ([^)]+)\), new Vector2\(([^,]+), ([^)]+)\)\)/g)].map(m => [[evalExpr(m[1]!, S), evalExpr(m[2]!, S)], [evalExpr(m[3]!, S), evalExpr(m[4]!, S)]]);

  // Southside: the free-standing blocks (the Rocket Lane trio is placed by Lane(); those three are read with their expressions resolved)
  const ss = flat(src('DecoKitSouthside.cs'));
  const ssS: Sym = { ...S };
  readConsts(src('DecoKitSouthside.cs').replace(/P\.EastBack/g, String(S.EastBack)).replace(/L\.Z\.x/g, String(S['Z.x'])), ssS);
  const rocket = { from: [ssS['RocketFrom.x']!, ssS['RocketFrom.y']!], phi: ssS.RocketPhi!, half: ssS.Half! };
  const lane = (t: number, off: number): [number, number] => {
    const d = [Math.sin(rocket.phi * Math.PI / 180), Math.cos(rocket.phi * Math.PI / 180)], s = [Math.sin((rocket.phi + 90) * Math.PI / 180), Math.cos((rocket.phi + 90) * Math.PI / 180)];
    return [rocket.from[0]! + d[0]! * t + s[0]! * off, rocket.from[1]! + d[1]! * t + s[1]! * off];
  };
  for (const m of ss.matchAll(/Vector2 rl1 = Lane\(([^,]+), ([^)]+)\), rl2 = Lane\(([^,]+), ([^)]+)\), rb1 = Lane\(([^,]+), ([^)]+)\);/g)) {
    for (const [k, a, b] of [['rl1', m[1], m[2]], ['rl2', m[3], m[4]], ['rb1', m[5], m[6]]] as const) { const p = lane(evalExpr(a!, S), evalExpr(b!, S)); ssS[`${k}.x`] = p[0]; ssS[`${k}.y`] = p[1]; }
  }
  const blocks: OldBlock[] = [...ss.matchAll(/new DecoKitBlock\("(\w+)", ([^,]+), ([^,]+), ([^,]+), ([^,]+), S\(((?:\("\w+", \d+\),? ?)+)\), S\(((?:\("\w+", \d+\),? ?)+)\)([^)]*)\)/g)].map(m => {
    const tail = m[8]!, named = Object.fromEntries([...tail.matchAll(/(\w+): ([-\d.f]+)/g)].map(x => [x[1], evalExpr(x[2]!, ssS)]));
    const pos = tail.split(',').map(x => x.trim()).filter(x => x && !x.includes(':')).map(x => evalExpr(x, ssS)); // positional walkR, walkL, walkH, walkT
    const w = { r: pos[0] ?? named.walkR ?? 4, l: pos[1] ?? named.walkL ?? 4, h: pos[2] ?? named.walkH ?? 4, t: pos[3] ?? named.walkT ?? 4 };
    return { name: m[1]!, x: evalExpr(m[2]!, ssS), z: evalExpr(m[3]!, ssS), phi: evalExpr(m[4]!, ssS), w: evalExpr(m[5]!, ssS), right: segsOf(m[6]!), left: segsOf(m[7]!), walk: w, d: named.d ?? S.Depth!, src: m[0]! };
  });
  const square = ss.match(/Rect Square = Rect\.MinMaxRect\(([^,]+), ([^,]+), ([^,]+), ([^)]+)\)/)!;
  const southside = { blocks, rocket, square: [evalExpr(square[1]!, ssS), evalExpr(square[2]!, ssS), evalExpr(square[3]!, ssS), evalExpr(square[4]!, ssS)] as [number, number, number, number], bounds: (() => { const b = ss.match(/Rect Bounds = Rect\.MinMaxRect\(([^,]+), ([^,]+), ([^,]+), ([^)]+)\)/)!; return [1, 2, 3, 4].map(i => evalExpr(b[i]!.replace('P.EastBack', String(S.EastBack)).replace('L.Z.x', String(S['Z.x'])), ssS)); })() };

  // Orbit Gardens
  const gS: Sym = { ...S };
  readConsts(src('DecoKitGardens.cs'), gS);
  const gd = flat(src('DecoKitGardens.cs'));
  const pav = gd.match(/p\.Put\("DecoPavilion", new Vector3\(([^,]+), w, ([^)]+)\), ([^)]+)\)/)!;
  const gardens = {
    canal: { x: gS.CanalX!, half: gS.CanalHalf!, from: S['WestLane.x']! - 6, to: S['Circus.z']! - gS.PoolR! - gS.Coping! - 4 },
    pool: { c: [gS.CanalX!, S['Circus.z']!] as [number, number], r: gS.PoolR!, coping: gS.Coping!, rim: gS.Rim! },
    pavilion: { at: [evalExpr(pav[1]!, gS), evalExpr(pav[2]!, gS)] as [number, number], yaw: evalExpr(pav[3]!, gS) },
    x: [S['GardensX.x']!, S['GardensX.y']!], z: [S['GardensZ.x']!, S['GardensZ.y']!],
  };

  // the skyline: landmarks Put on the street, and the background towers
  const sk = flat(src('DecoKitStreetSkyline.cs'));
  const kS: Sym = { ...S, 'P.WaterY': S.WaterY! };
  const puts: OldPut[] = [];
  for (const m of sk.matchAll(/p\.Put\("(\w+)", new Vector3\(([^,]+), ([^,]+), ([^)]+?)\)(?:, ([^,)]+))?(?:, scale: ([\d.f]+))?\);/g)) {
    if (m[2]!.trim() === 'x' || m[1] === 'name') continue; // the foreach forms, read below
    puts.push({ kit: m[1]!, at: [evalExpr(m[2]!, kS), evalExpr(m[3]!, kS), evalExpr(m[4]!, kS)], yaw: m[5] ? evalExpr(m[5], kS) : 0, scale: m[6] ? evalExpr(m[6], kS) : 1, src: m[0]! });
  }
  for (const m of sk.matchAll(/foreach \(var x in new\[\] \{ ([^}]+) \}\) p\.Put\("(\w+)", new Vector3\(x, ([^,]+), ([^)]+)\), x \* ([\d.f]+)\);/g)) {
    for (const x of m[1]!.split(',').map(v => evalExpr(v, kS))) puts.push({ kit: m[2]!, at: [x, evalExpr(m[3]!, kS), evalExpr(m[4]!, kS)], yaw: x * evalExpr(m[5]!, kS), scale: 1, src: m[0]! });
  }
  const towers = [...sk.matchAll(/\("(\w+)", new\(([^,]+), ([^,]+), ([^)]+)\), ([-\d.f]+)\)/g)].map(m => ({ kit: m[1]!, at: [evalExpr(m[2]!, S), evalExpr(m[4]!, S)] as [number, number], yaw: evalExpr(m[5]!, S) }));
  const circusPut = flat(src('DecoKitCircus.cs')).match(/p\.Put\(orrery \? "(\w+)"/)!;
  const harbour = flat(src('DecoKitHarbour.cs'));
  const sp = harbour.match(/p\.Put\("(Skyport_Tower)", P\.Skyport, ([-\d.f]+)\)/);
  if (sp) puts.push({ kit: sp[1]!, at: [S['Skyport.x']!, S['Skyport.y']!, S['Skyport.z']!], yaw: evalExpr(sp[2]!, S), scale: 1, src: sp[0]! });
  const apron = evalExpr(harbour.match(/SkyportApron = ([\d.f]+)/)![1]!, S);

  // the sky rail: the main loop's control points (SkyPath.Set(route, closed: true): a closed spline) and the harbour line
  const rail = flat(src('DecoKitSkyRail.cs'));
  const rS: Sym = { ...S };
  readConsts(src('DecoKitSkyRail.cs').replace(/P\.(\w+)/g, (_, k) => String(S[k] ?? S[`${k}`])), rS);
  const routeBody = rail.match(/internal static Vector3\[\] Route\(\) \{(.*?)return r\.ToArray\(\);/)![1]!;
  const vec = (s: string) => [...s.matchAll(/new\(([^,]+), ([^,]+), ([^)]+)\)/g)].map(m => [evalExpr(m[1]!, rS), evalExpr(m[2]!, rS), evalExpr(m[3]!, rS)] as [number, number, number]);
  const lists = [...routeBody.matchAll(/\{([^{}]*new\([^{}]*)\}/g)].map(m => vec(m[1]!));
  const sphereC = [rS['Sphere.x']!, rS['Sphere.y']!];
  const ringSpec = routeBody.match(/foreach \(var deg in new\[\] \{ ([^}]+) \}\).*?(\d+)f \* Mathf\.Cos\(a\), ([\d.f]+), /)!;
  const ringR = Number(ringSpec[2]), ringH = evalExpr(ringSpec[3]!, rS);
  const ring = ringSpec[1]!.split(',').map(v => evalExpr(v, rS)).map(deg => [sphereC[0]! + ringR * Math.cos(deg * Math.PI / 180), ringH, sphereC[1]! + ringR * Math.sin(deg * Math.PI / 180)] as [number, number, number]);
  const main = [...lists[0]!, ...ring, ...lists[1]!];
  const st = rail.match(/Vector3 Station = new\(([^,]+), ([^,]+), ([^)]+)\);/)!;
  const station: [number, number, number] = [evalExpr(st[1]!, S), evalExpr(st[2]!, S), evalExpr(st[3]!, S)];
  const stationRail = evalExpr(rail.match(/StationRail = ([\d.f]+)/)![1]!, S);
  const hb = rail.match(/float x = Station\.x, sea = P\.Skyport\.x - ([\d.f]+), h = Station\.y \+ StationRail, R = ([\d.f]+)f;/)!;
  const seaX = S['Skyport.x']! - evalExpr(hb[1]!, S), R = evalExpr(hb[2]!, S), h = station[1] + stationRail, x = station[0];
  const harbourRoute: [number, number, number][] = [[x, h, -300], [x, h, 140], [x + 20, h - 1, 160]];
  for (let deg = 225; deg <= 540; deg += 22.5) harbourRoute.push([S['Skyport.x']! + R * Math.sin(deg * Math.PI / 180), 24, S['Skyport.z']! + R * Math.cos(deg * Math.PI / 180)]);
  harbourRoute.push([seaX, h - 1, 120], [seaX, h, -300], [(x + seaX) / 2, h, -318]);
  const pylon = { step: rS.PylonStep!, beamH: rS.BeamH!, beamW: rS.BeamW!, footClear: rS.FootClear!, speed: rS.Speed! };
  if (!harbour.includes('Skyport pier') || !rail.includes('HarbourRoute')) throw new Error('old harbour / sky rail not as expected');

  return { S, walls, shorts, crescents, hall, southside, gardens, puts, towers, monument: circusPut[1]!, apron, skyrail: { main, mainClosed: true, sphere: sphereC, ringR, harbour: harbourRoute, station, stationRail, pylon } };
}
export type Old = ReturnType<typeof readOld>;
