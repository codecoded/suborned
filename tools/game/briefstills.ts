// usage: bun tools/game/briefstills.ts [--mission handover] [--app <The Suborned.app>] [--resolve-only] [--out <dir>]
// Brief 070 (GDD v0.27.0 §10 briefing): the briefing page's stills, captured at build time from a replay of the mission's reference routes, HUD off,
// one per pinned place, so a generated mission gets its own page from its own data. This file derives the pins from the mission's data (never by
// hand) and writes two files into out/build/play/stills/<mission>/:
//   stills.json        suborned.brief-stills/1: the pins in page order (way, number, place, where it is on the block, the text key of its caption, the
//                      file), and the plan the page draws (civic-block.json buildings, bays and drives, fountains and planters, the bounds)
//   stills.shots.json  the shot runner's file (suborned.shots.resolved/1; ShotRunner.cs): one world-only capture per pin
// then, unless --resolve-only, runs the built player with -shots over it, checks every still was written, copies the folder into the app's
// StreamingAssets/missions/<mission>/stills/ (where BriefScreen reads it) and signs the app ad hoc again (as GameBuild.Finish does).
// The rules (Glyph's README, briefs/070/glyph/README.md "Capture points"):
//   ways      brief.json ways[] in order; a way's route is the primary quiet script (routes.json) of its window's route (mission.json windows[id = way]),
//             else of the route its contact is named in (routes.json contacts[role].who "route X")
//   pins      per way: the contact (roles[contact].at), then the window's role (roles[window.role].at), else the place the timeline moves the
//             target's car to after a turn (a timeline entry naming a points[] id, "service bay (6:25)"); the shared ending is the getaway bay
//   tick      the contact: its shift start + 0:30, or, when it waits for the target (roles[].waitsFor), the target's leavesTo.by (Vale at his car);
//             the window's role: 1:00, on his post before any call; a timeline place: its m:ss + 1 s; the ending: 0:00
//   camera    a person at a door: the conversation camera's profile rule (3.6 m off the door's outward side, 1.9 m up, 8° down), framed head to
//             waist; a place with a car: the play camera at the quarter-step yaw that best faces the nearest building face's outward side
//             (civic-block.json), the play pitch, zoom 9, the architecture cut off; the ending: a low camera square onto the getaway car's side
import { copyFileSync, existsSync, mkdirSync, readFileSync, readdirSync, rmSync, writeFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { spawnSync } from 'node:child_process';

export const ROOT = resolve(import.meta.dir, '../..');
type V2 = [number, number];
type Json = any;

export type Pin = {
  id: string; way: string; n: string; text: string; place: string; at: V2; tick: number; route: string; file: string;
  camera: { kind: 'person' | 'car'; view: string; normal: V2 | null } | { kind: 'play'; yaw: number; zoom: number; normal: V2 | null };
  why: string;
};
export type Plan = { bounds: [number, number, number, number]; buildings: { name: string; polygon: V2[] }[]; ground: V2[][]; green: V2[][]; water: V2[][] };
export type Stills = { schema: 'suborned.brief-stills/1'; mission: string; size: V2; pins: Pin[]; plan: Plan;
  /** the target's door (brief.target.at) and the outward side of its face: Begin lifts the page onto the camera there (HostBriefing) */
  targetDoor: { place: string; at: V2; normal: V2 | null } | null; sources: string[] };

const secs = (mss: string) => { const m = /^(\d+):(\d{2})$/.exec(mss.trim()); return m ? Number(m[1]) * 60 + Number(m[2]) : NaN; };
const TICKS = 10; // the engine's ticks per second (rulebook tick 0.1 s)

/** Whether a point lies inside a polygon (even-odd). */
export function inside(p: V2, pg: V2[] | undefined): boolean {
  if (!pg) return false; let c = false;
  for (let i = 0, j = pg.length - 1; i < pg.length; j = i++) { const a = pg[i]!, b = pg[j]!; if ((a[1] > p[1]) !== (b[1] > p[1]) && p[0] < (b[0] - a[0]) * (p[1] - a[1]) / (b[1] - a[1]) + a[0]) c = !c; }
  return c;
}

/** The outward normal of the building face nearest `p` (block metres), with its distance; null when no building. */
export function nearestFace(p: V2, buildings: { polygon: V2[] }[]): { normal: V2; d: number } | null {
  let best: { normal: V2; d: number } | null = null;
  for (const b of buildings) {
    const pg = b.polygon, n = pg.length; let cx = 0, cy = 0; for (const q of pg) { cx += q[0] / n; cy += q[1] / n; }
    for (let i = 0; i < n; i++) {
      const a = pg[i]!, c = pg[(i + 1) % n]!, ex = c[0] - a[0], ey = c[1] - a[1], L = Math.hypot(ex, ey); if (L < 1e-6) continue;
      const t = Math.max(0, Math.min(1, ((p[0] - a[0]) * ex + (p[1] - a[1]) * ey) / (L * L)));
      const d = Math.hypot(p[0] - (a[0] + ex * t), p[1] - (a[1] + ey * t));
      let nx = ey / L, ny = -ex / L; const mx = (a[0] + c[0]) / 2 - cx, my = (a[1] + c[1]) / 2 - cy; if (nx * mx + ny * my < 0) { nx = -nx; ny = -ny; }
      if (!best || d < best.d - 1e-6) best = { normal: [nx + 0, ny + 0], d }; // + 0: no -0 in the file
    }
  }
  return best;
}

export function derive(mission = 'handover', root = ROOT): Stills {
  const dir = join(root, 'contracts/missions', mission), J = (f: string) => JSON.parse(readFileSync(join(dir, f), 'utf8'));
  const brief = J('brief.json'), m = J('mission.json'), text = J('text.json'), routes = J('routes/routes.json');
  const block = JSON.parse(readFileSync(join(root, 'contracts/world/civic-block.json'), 'utf8'));
  const sources = ['contracts/missions/' + mission + '/brief.json ways', 'mission.json roles, windows, timeline, points, destinations', 'routes/routes.json primary scripts and contacts', 'contracts/world/civic-block.json buildings, bays, drives, objects'];
  const roles: Record<string, Json> = Object.fromEntries((m.roles ?? []).map((r: Json) => [r.id, r]));
  const dest: Record<string, V2> = Object.fromEntries((m.destinations ?? []).map((d: Json) => [d.name, d.at]));
  const pts: Record<string, V2> = Object.fromEntries((m.points ?? []).map((p: Json) => [p.id, p.at]));
  const placeAt = (id: string): V2 => { const k = id.startsWith('d:') ? id.slice(2) : id; const a = dest[k] ?? pts[k]; if (!a) throw new Error(`stills: no place ${id} in mission.json destinations or points`); return a; };
  const targetRole = typeof brief.target === 'string' ? brief.target : brief.target?.role ?? 'councillor', target = roles[targetRole];
  const primary = (route: string) => { const s = (routes.scripts ?? []).find((x: Json) => x.route === route && x.primary && x.kind === 'quiet'); if (!s) throw new Error(`stills: routes.json has no primary quiet script for route ${route}`); return 'contracts/missions/' + mission + '/routes/' + s.file; };
  const textKey = (k: string) => text.targets?.[k] ? k : '';
  const buildings = (block.buildings ?? []).map((b: Json) => ({ name: b.name as string, polygon: b.polygon as V2[] }));
  const face = (p: V2) => nearestFace(p, buildings);
  const pins: Pin[] = []; let n = 0;
  const play = (at: V2, zoom: number) => ({ kind: 'play' as const, yaw: -1, zoom, normal: face(at)?.normal ?? null });
  const person = (view: string, at: V2) => ({ kind: 'person' as const, view, normal: face(at)?.normal ?? null });
  for (const w of brief.ways ?? []) {
    const win = (m.windows ?? []).find((x: Json) => x.id === w.id);
    const named = (routes.contacts ?? []).find((c: Json) => c.role === w.contact)?.who as string | undefined;
    const route = win?.route ?? /route ([A-Z])\b/.exec(named ?? '')?.[1];
    if (!route) throw new Error(`stills: way ${w.id} has no window and its contact ${w.contact} is named in no route (routes.json contacts)`);
    const script = primary(route), c = roles[w.contact]; if (!c) throw new Error(`stills: way ${w.id}: no role ${w.contact}`);
    // the contact
    const waits = c.waitsFor === targetRole && target?.leavesTo?.by;
    const cTick = waits ? secs(target.leavesTo.by) : (secs(c.shift?.from ?? '0:00') || 0) + 3;
    const cAt = placeAt('d:' + c.at);
    pins.push({ id: `${w.id}-${w.contact}`, way: w.id, n: String(++n), text: textKey(w.contact), place: 'd:' + c.at, at: cAt, tick: cTick * TICKS, route: script, file: `brief-${w.contact}.jpg`,
      camera: waits ? play(cAt, 9) : person(w.contact, cAt),
      why: waits ? `roles[${w.contact}].waitsFor ${targetRole}: at the target's leavesTo.by ${target.leavesTo.by}` : `roles[${w.contact}].shift.from ${c.shift?.from ?? '0:00'} + 0:03, on her post before the cell walks over` });
    // the second place: the window's role, else where the timeline moves the target's car after the turn
    if (win?.role && roles[win.role]) {
      const r = roles[win.role], at = placeAt('d:' + r.at);
      pins.push({ id: `${w.id}-${win.role}`, way: w.id, n: String(++n), text: textKey(win.role), place: 'd:' + r.at, at, tick: 60 * TICKS, route: script, file: `brief-${win.role}.jpg`,
        camera: person(win.role, at), why: `windows[${w.id}].role ${win.role}: 1:00, on his post before any call` });
    } else {
      const hit = (m.timeline ?? []).map((t: Json) => ({ t, p: Object.keys(pts).find(id => new RegExp(`${id.replace(/-/g, '[ -]')} \\((\\d+:\\d{2})\\)`).test(t.what ?? '')) })).find((x: Json) => x.p);
      if (hit) {
        const mss = new RegExp(`${hit.p.replace(/-/g, '[ -]')} \\((\\d+:\\d{2})\\)`).exec(hit.t.what)![1]!, at = pts[hit.p]!;
        pins.push({ id: `${w.id}-${hit.p}`, way: w.id, n: String(++n), text: textKey('car-official'), place: hit.p, at, tick: (secs(mss) + 1) * TICKS, route: script, file: `brief-${hit.p}.jpg`,
          camera: play(at, 9), why: `timeline ${hit.t.at}: "${hit.p.replace(/-/g, ' ')} (${mss})" + 1 s` });
      }
    }
  }
  // the shared ending: the getaway car in its bay at 0:00, a low camera off its end (the bay's polygon: along its long axis, from the side of the
  // other pins), so the whole car fits the page's tall print; from above, the boarding pavilion's canopy hides it
  const gk = Object.keys(dest).find(k => /getaway/i.test(k)); if (!gk) throw new Error('stills: no getaway destination in mission.json');
  const gAt = dest[gk]!, bay = (block.bays ?? []).find((b: Json) => inside(gAt, b.polygon));
  let gN: V2 | null = null;
  if (bay) {
    const bx = bay.polygon.map((q: V2) => q[0]), by = bay.polygon.map((q: V2) => q[1]);
    const along = Math.max(...bx) - Math.min(...bx) >= Math.max(...by) - Math.min(...by); // the long axis
    const mx = pins.reduce((a, p) => a + p.at[0], 0) / pins.length - gAt[0], my = pins.reduce((a, p) => a + p.at[1], 0) / pins.length - gAt[1];
    gN = along ? [mx > 0 ? 1 : -1, 0] : [0, my > 0 ? 1 : -1]; // along the long axis, from the block's side: the car end-on fits the page's tall print
  }
  pins.push({ id: 'ending-getaway', way: '', n: '', text: textKey('car-getaway'), place: 'd:' + gk, at: gAt, tick: 0, route: pins[pins.length - 1]?.route ?? primary('A'), file: 'brief-getaway.jpg',
    camera: { kind: 'car', view: 'car/getaway', normal: gN }, why: `the getaway car at 0:00, off its end (civic-block.json bay ${bay?.id ?? 'none'})` });
  // the plan: the block round every pin, 6 m of margin
  const xs = pins.map(p => p.at[0]), ys = pins.map(p => p.at[1]), M = 6;
  const bounds: [number, number, number, number] = [Math.min(...xs) - M, Math.min(...ys) - M, Math.max(...xs) + M, Math.max(...ys) + M];
  const kind = (o: Json, re: RegExp) => re.test(String(o.kind ?? '') + ' ' + String(o.id ?? ''));
  const plan: Plan = {
    bounds, buildings,
    ground: [...(block.bays ?? []), ...(block.drives ?? [])].filter((o: Json) => Array.isArray(o.polygon)).map((o: Json) => o.polygon),
    green: (block.objects ?? []).filter((o: Json) => Array.isArray(o.polygon) && kind(o, /planter|bed|lawn|hedge/i)).map((o: Json) => o.polygon),
    water: (block.objects ?? []).filter((o: Json) => Array.isArray(o.polygon) && kind(o, /fountain|pool/i)).map((o: Json) => o.polygon),
  };
  const tAt = typeof brief.target === 'object' ? brief.target?.at as string | undefined : undefined;
  const targetDoor = tAt ? { place: tAt, at: placeAt(tAt), normal: face(placeAt(tAt))?.normal ?? null } : null;
  return { schema: 'suborned.brief-stills/1', mission, size: [1920, 1080], pins, plan, targetDoor, sources };
}

/** The shot runner's rows (ShotRunner.cs ShotSpec; suborned.shots.resolved/1), grouped by route so each run goes on while its ticks rise. */
export function shotsFile(s: Stills, outDir: string, root = ROOT): Json {
  const order = [...s.pins].sort((a, b) => a.route.localeCompare(b.route) || a.tick - b.tick);
  return {
    schema: 'suborned.shots.resolved/1', outDir,
    shots: order.map(p => {
      const person = p.camera.kind === 'person' || p.camera.kind === 'car';
      // the conversation camera's profile rule (HostConvo ConvoDist 3.6, ConvoHeight 1.9, ConvoPitch 8°), framed head to waist: 3.6 m off, out along the
      // door's outward side and 1.25 m along the face (ShotRunner: doorN makes the offset the door's frame), 1.9 m up, aimed 0.55 m lower (8° down
      // at 3.6 m), a 16° field (35° frames the whole figure at 3.6 m)
      // the car: 9.5 m off its end, 1.8 m to the side, 3 m up, aimed at its middle, a 32° field (the whole car in the print's tall centre)
      const pose = p.camera.kind === 'car' ? { on: true, view: p.camera.view, pos: [], look: [], offset: [1.8, 3.0, 9.5], lookOffset: [0, 0.4, 0], fov: 32 }
        : person ? { on: true, view: (p.camera as { view: string }).view, pos: [], look: [], offset: [1.25, 1.9, 3.38], lookOffset: [0, 1.35, 0], fov: 16 } : { on: false, view: '', pos: [], look: [], offset: [], lookOffset: [], fov: 39.6 };
      const cam = p.camera.kind === 'play' ? p.camera : null;
      return {
        name: p.file.replace(/\.jpg$/, ''), scriptPath: join(root, p.route), checkpoint: '', preset: 'today', tick: p.tick,
        night: false, setNight: false, briefing: false, follow: false, agent: 'scout', face: '', focus: '', hasAt: !person, atX: Math.round(p.at[0] * 100), atY: Math.round(p.at[1] * 100),
        yaw: cam ? cam.yaw : -1, zoom: cam ? cam.zoom : 0, nocut: !!cam && cam.yaw < 0, doorN: p.camera.normal ?? [],
        hud: false, state: 'rest', select: '', arm: '', hasPointer: false, px: 0, py: 0, intel: false, hover: '', mark: false, settle: 0.6, perf: 0,
        pose, w: s.size[0], h: s.size[1], jpg: true, clipSeconds: 0, clipFps: 30,
      };
    }),
  };
}

function main() {
  const args = process.argv.slice(2), val = (k: string) => { const i = args.indexOf(k); return i >= 0 ? args[i + 1] : undefined; };
  const mission = val('--mission') ?? 'handover', app = val('--app') ?? join(ROOT, 'out/build/play/The Suborned.app');
  const out = resolve(val('--out') ?? join(ROOT, 'out/build/play/stills', mission));
  const s = derive(mission);
  rmSync(out, { recursive: true, force: true }); mkdirSync(out, { recursive: true });
  writeFileSync(join(out, 'stills.json'), JSON.stringify(s, null, 1) + '\n');
  const sf = shotsFile(s, out); writeFileSync(join(out, 'stills.shots.json'), JSON.stringify(sf, null, 1) + '\n');
  for (const p of s.pins) console.log(`stills: ${p.n || '·'} ${p.id.padEnd(24)} ${p.place.padEnd(20)} tick ${String(p.tick).padStart(4)} ${p.camera.kind !== 'play' ? `${p.camera.kind} ${p.camera.view} side ${JSON.stringify(p.camera.normal)}` : `yaw ${p.camera.yaw < 0 ? 'faces ' + JSON.stringify(p.camera.normal?.map(v => +v.toFixed(2))) : p.camera.yaw} zoom ${p.camera.zoom}`}  ${p.route.split('/').pop()}  (${p.why})`);
  if (args.includes('--resolve-only')) return;
  const exe = join(app, 'Contents/MacOS/The Suborned');
  if (!existsSync(exe)) { console.error(`stills: no player at ${exe}`); process.exit(1); }
  const t0 = Date.now();
  const r = spawnSync(exe, ['-shots', join(out, 'stills.shots.json'), out, '-novsync', '-logFile', join(out, 'player.log')], { encoding: 'utf8', timeout: 600000 });
  const rep = existsSync(join(out, 'shots.json')) ? JSON.parse(readFileSync(join(out, 'shots.json'), 'utf8')) : null;
  const missing = s.pins.filter(p => !existsSync(join(out, p.file)));
  console.log(`stills: the player took ${rep?.shots?.filter((x: Json) => x.status === 'ok').length ?? 0}/${s.pins.length} in ${Math.round((Date.now() - t0) / 1000)} s (exit ${r.status}; ${out}/player.log)`);
  for (const x of rep?.shots ?? []) if (x.status !== 'ok') console.error(`stills: ${x.name}: ${x.status} ${x.error}`);
  if (missing.length) { console.error(`stills: FAILED: missing ${missing.map(p => p.file).join(', ')}`); process.exit(1); }
  // into the app, where BriefScreen reads it (StreamingAssets/missions/<mission>/stills), then the ad hoc signature over the changed bundle
  const dst = join(app, 'Contents/Resources/Data/StreamingAssets/missions', mission, 'stills');
  rmSync(dst, { recursive: true, force: true }); mkdirSync(dst, { recursive: true });
  for (const f of ['stills.json', ...s.pins.map(p => p.file)]) copyFileSync(join(out, f), join(dst, f));
  const sign = spawnSync('/usr/bin/codesign', ['--force', '--deep', '--sign', '-', app], { encoding: 'utf8' });
  console.log(`stills: ${readdirSync(dst).length} files into ${dst.replace(ROOT + '/', '')}; ad hoc signature ${sign.status === 0 ? 'ok' : 'FAILED ' + sign.stderr}`);
  if (sign.status !== 0) process.exit(1);
}

if (import.meta.main) main();
