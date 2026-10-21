// tools/game/shots.json → the file the player's shot runner reads (brief 034 item 3). Validation is strict and says which shot and why: a bad row stops the
// batch before a 4-minute player run, not after it. Pure but for reading the files a shot names (route scripts, checkpoints) and, for a scenario id, one call to
// the engine CLI that exports it as a script.
import { closeSync, existsSync, openSync, readFileSync, readSync } from 'node:fs';
import { isAbsolute, join, resolve } from 'node:path';
import { Shots, type Shot } from './shots.schema';

export const ROOT = resolve(import.meta.dir, '../..');
export const DEFAULT_SIZE: [number, number] = [1920, 1080];
export const ROUTES = 'contracts/missions/handover/routes', CHECKPOINTS = 'briefs/027/checkpoints';
export const DEFAULT_FILE = join(ROOT, 'tools/game/shots.json');

/** The order script a shot starts from: just enough to check it (suborned.sim.script/1). */
export type ScriptInfo = { start: Record<string, unknown>; until: number };

export type Resolved = {
  name: string; scriptPath: string; checkpoint: string; preset: string; tick: number;
  night: boolean; setNight: boolean; briefing: boolean;
  follow: boolean; agent: string; face: string; focus: string; hasAt: boolean; atX: number; atY: number; yaw: number; zoom: number;
  hud: boolean; state: string; select: string; arm: string; hasPointer: boolean; px: number; py: number; intel: boolean; settle: number; perf: number; hover: string; mark: boolean;
  pose: { on: boolean; view: string; pos: number[]; look: number[]; offset: number[]; lookOffset: number[]; fov: number };
  /** the capture size in pixels (1920 x 1080 unless the row says), and whether its files are JPEG */
  w: number; h: number; jpg: boolean;
  clipSeconds: number; clipFps: number;
};
export type ResolvedFile = { schema: 'suborned.shots.resolved/1'; outDir: string; shots: Resolved[] };

export type Resolver = {
  /** a route script by name (routes dir) or by path; undefined when there is none */
  script(ref: string): { path: string; info: ScriptInfo } | undefined;
  checkpoint(name: string): boolean;
  /** a scenario id exported as a script on disk; undefined when the engine has no such scenario */
  scenario(id: string): { path: string; info: ScriptInfo } | undefined;
};

const readScript = (path: string): ScriptInfo => {
  const j = JSON.parse(readFileSync(path, 'utf8'));
  if (j.schema !== 'suborned.sim.script/1') throw new Error(`${path} is not a suborned.sim.script/1 script`);
  return { start: j.start ?? {}, until: j.until ?? 0 };
};

export const fileResolver = (scenario: Resolver['scenario'] = () => undefined): Resolver => ({
  script(ref) {
    const path = isAbsolute(ref) ? ref : existsSync(resolve(ROOT, ref)) && ref.endsWith('.json') ? resolve(ROOT, ref) : join(ROOT, ROUTES, ref.endsWith('.json') ? ref : `${ref}.json`);
    return existsSync(path) ? { path, info: readScript(path) } : undefined;
  },
  checkpoint: name => existsSync(join(ROOT, CHECKPOINTS, `${name}.script.json`)),
  scenario,
});

/** The merged shot (defaults under the row), with its problems. Nothing is run. */
export function check(raw: unknown, r: Resolver): { shots: Shot[]; problems: string[] } {
  const parsed = Shots.safeParse(raw);
  if (!parsed.success) return { shots: [], problems: parsed.error.issues.map(i => `${i.path.join('.') || 'shots.json'}: ${i.message}`) };
  const problems: string[] = [], seen = new Set<string>(), shots: Shot[] = [];
  for (const [i, row] of parsed.data.shots.entries()) {
    const s: Shot = { ...parsed.data.defaults, ...row }, p = (m: string) => problems.push(`shot ${i + 1} (${s.name}): ${m}`);
    if (seen.has(s.name)) p('the name is used twice'); seen.add(s.name);
    if (s.skip) { shots.push(s); continue; }
    const starts = [s.script, s.checkpoint, s.scenario].filter(x => x !== undefined);
    if (starts.length > 1) p('give one start: script, checkpoint or scenario');
    let until = 36000;
    if (s.script !== undefined) { const f = r.script(s.script); if (!f) p(`no route script ${s.script} (contracts/missions/handover/routes, or a path)`); else { until = f.info.until || until; if (Object.keys(f.info.start).length) p(`${s.script} starts from a fixture (${Object.keys(f.info.start).join(', ')}); the game can start only from the mission's own start or a fight-lab checkpoint`); } }
    if (s.scenario !== undefined) { const f = r.scenario(s.scenario); if (!f) p(`no engine scenario ${s.scenario}`); else { until = f.info.until || until; if (Object.keys(f.info.start).length) p(`scenario ${s.scenario} starts from a fixture (${Object.keys(f.info.start).join(', ')}); use a fight-lab checkpoint, or a route script`); } }
    if (s.checkpoint !== undefined && !r.checkpoint(s.checkpoint)) p(`no checkpoint ${s.checkpoint} in ${CHECKPOINTS}`);
    if (s.preset !== undefined && s.checkpoint === undefined) p('preset goes with a checkpoint');
    if ((s.tick ?? 0) > until) p(`tick ${s.tick} is past the end of its script (${until})`);
    const follow = s.view === 'follow';
    if (follow && (s.focus !== undefined || s.at !== undefined)) p('a follow view looks where the agent faces: use face, not focus or at');
    if (!follow && s.face !== undefined) p('face goes with the follow view');
    if (s.focus !== undefined && s.at !== undefined) p('focus and at both say where to look');
    if (s.arm !== undefined && s.select === undefined) p('arm needs select (whose plate)');
    if (s.pose) {
      const abs = s.pose.pos !== undefined || s.pose.look !== undefined, rel = s.pose.view !== undefined || s.pose.offset !== undefined || s.pose.lookOffset !== undefined;
      if (abs === rel) p('pose: give pos and look, or view with offset and lookOffset');
      if (abs && (s.pose.pos === undefined || s.pose.look === undefined)) p('pose: pos and look go together');
      if (rel && (s.pose.view === undefined || s.pose.offset === undefined)) p('pose: view needs offset (and lookOffset)');
      if (s.hud) p('pose draws the world with no HUD: hud must be false');
      if (follow || s.focus !== undefined || s.at !== undefined || s.yaw !== undefined || s.zoom !== undefined) p('pose is its own camera: no follow, focus, at, yaw or zoom');
    }
    if (s.briefing && ((s.tick ?? 0) > 0 || s.script !== undefined || s.pose || s.clip)) p('the briefing card is up only at tick 0 of the plain start, as a still');
    if (s.format === 'jpg' && (s.hud ?? true) && !s.pose) p('a HUD shot is png: the budget reads its frame lossless (format jpg goes with hud false, a pose or a clip)');
    if (s.hud === false && s.state !== undefined) p('state is the HUD budget\'s: it goes with hud true');
    if (s.clip && s.perf) p('a clip and a frame-time sample both run the clock: give one');
    if (s.clip && (s.clip.seconds * (s.clip.fps ?? 30)) > 1800) p(`a clip of ${s.clip.seconds} s at ${s.clip.fps ?? 30} fps is over 1800 frames`);
    shots.push(s);
  }
  return { shots, problems };
}

/** The file the player reads: every default filled, paths absolute. Throws with all the problems when the file is not good. */
export function resolveShots(raw: unknown, outDir: string, r: Resolver = fileResolver()): ResolvedFile {
  const { shots, problems } = check(raw, r);
  if (problems.length) throw new Error(`shots.json has ${problems.length} problem${problems.length === 1 ? '' : 's'}:\n  ${problems.join('\n  ')}`);
  return {
    schema: 'suborned.shots.resolved/1', outDir,
    shots: shots.filter(s => !s.skip).map(s => {
      const sc = s.script !== undefined ? r.script(s.script)!.path : s.scenario !== undefined ? r.scenario(s.scenario)!.path : '';
      const pose = s.pose;
      return {
        name: s.name, scriptPath: sc, checkpoint: s.checkpoint ?? '', preset: s.preset ?? 'today', tick: s.tick ?? 0,
        night: s.night ?? false, setNight: s.night !== undefined, briefing: s.briefing ?? false,
        follow: s.view === 'follow', agent: s.agent ?? s.select ?? 'scout', face: s.face ?? '', focus: s.focus ?? '', hasAt: s.at !== undefined, atX: s.at?.[0] ?? 0, atY: s.at?.[1] ?? 0,
        yaw: s.yaw ?? -1, zoom: s.zoom ?? 0,
        hud: pose ? false : s.hud ?? true, state: s.state ?? 'rest', select: s.select ?? '', arm: s.arm ?? '', hasPointer: s.pointer !== undefined, px: s.pointer?.[0] ?? 0, py: s.pointer?.[1] ?? 0, intel: s.intel ?? false, hover: s.hover ?? '', mark: s.mark ?? true,
        settle: s.settle ?? 0.35, perf: s.perf ?? 0,
        pose: { on: !!pose, view: pose?.view ?? '', pos: pose?.pos ?? [], look: pose?.look ?? [], offset: pose?.offset ?? [], lookOffset: pose?.lookOffset ?? [], fov: pose?.fov ?? 39.6 },
        w: s.size?.[0] ?? DEFAULT_SIZE[0], h: s.size?.[1] ?? DEFAULT_SIZE[1], jpg: (s.format ?? ((pose ? false : s.hud ?? true) && !s.clip ? 'png' : 'jpg')) === 'jpg',
        clipSeconds: s.clip?.seconds ?? 0, clipFps: s.clip?.fps ?? 30,
      };
    }),
  };
}

/** The files a good run leaves for a shot, relative to the output folder. */
export function expectedFiles(s: Resolved): string[] {
  const ext = s.jpg ? 'jpg' : 'png';
  if (s.clipSeconds > 0) return [`${s.name}/f0000.${ext}`];
  return [`${s.name}.${ext}`, ...(s.hud ? [`${s.name}.hud.png`, `${s.name}.manifest.json`] : [])];
}

/** Width and height from a PNG's header (IHDR), or undefined for anything else. */
export function pngSize(bytes: Uint8Array): [number, number] | undefined {
  if (bytes.length < 24 || bytes[0] !== 0x89 || bytes[1] !== 0x50 || bytes[2] !== 0x4e || bytes[3] !== 0x47) return undefined;
  const v = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
  return [v.getUint32(16), v.getUint32(20)];
}

/** The size of a PNG file on disk (reads 24 bytes). */
export function pngFileSize(file: string): [number, number] | undefined {
  const fd = openSync(file, 'r'), b = new Uint8Array(24);
  try { return readSync(fd, b, 0, 24, 0) === 24 ? pngSize(b) : undefined; } finally { closeSync(fd); }
}
