// suborned.tplong/1 (brief 048, Splice; Redline's proposed row): the draw-distance check of `bun run batch`. The third-person (follow) view draws the district out to its far
// clip, so its cost is counted in what it draws (triangles, SetPass calls, shadow casters, renderers, those far away) as well as in milliseconds. This times the two
// third-person views Redline measured (Sky's street, and the worst heading) and the play view at the opening for comparison, at the Air's panel size, ~40 s of player time.
//
//   row   name (kebab-case) · view: follow | overhead · for follow: agent (default scout), walk [dx, dz] metres from the start the cell walks first (engine move orders), faceHeading
//         degrees clockwise from +z (dir = (sin h, cos h) in x, z; as briefs/048/probe/Probe.cs FollowAt) · for overhead: yaw (default 30), zoom (default 14, the play zoom)
//         · settle seconds before the window (1.5) · seconds the window lasts (6) · size [w, h] (default 2880 x 1864, the Air's panel; render scale 1)
//   run   clock running, HUD on, -novsync. Never paused: the sim ticks as in play.
//
// The player's side is `-tplong <resolved.json> <outDir>` (the contract is in briefs/048-draw-distance.md, "The player's hook"); it writes <outDir>/tplong.json
// (suborned.tplong.report/1). judgeTpLong reads it. A row fails when a gate below is over, or when the drive was not what the row said (wrong size, wrong heading, too few frames).
// Counts do not depend on the machine; frame times do, so on a noisy machine (late.ts `noisy`) a frame-time gate is noted, not judged.
import { z } from 'zod';
import { noisy, type LoadSample } from './late';

export const DEFAULT_SIZE: [number, number] = [2880, 1864];
export const MARKER = 'suborned.tplong.report/1';

/** The gates (Redline, briefs/048-draw-distance.md "Gates on the Studio"). Third person: counts and a frame time with headroom; play view: its triangles and mean.
 *  Rebased 2026-10-23 (brief 048, Redline's review of build 0.2.79, out/scratch/redline/048/worst79/), as regression ceilings: Latch's 0.2.79 cull removed the thousands of tiny far
 *  pieces the count gates were written for, and what is left in the far band is the city's own blocks. The far band is a CPU cost (draw submission), cheap on the GPU; the Air's limit
 *  is pixels, so the Air gate (mean <= 16.7 ms, p99 <= 20 ms) and the frame times below stay as they were.
 *  setPass 250 -> 350 (0.2.79 tp-worst 332); beyond150 800 -> 1,500 (0.2.79 tp-worst 1,376 in the probe's census; 1,300 was set on the hook's old census).
 *  The census (TpLongRun.Census) now matches Redline's probe (Probe.cs Census): what the frustum keeps, no straight-line far cull, only the active LOD level counted. tp-worst reads
 *  2,398 drawn (limit 2,500, kept) and 1,376 beyond 150 m.
 *  setPass 350 -> 375 (Redline, build 0.2.85): Vela's DK_GlassSky window film is a transparent pass (sorted back to front, so the SRP Batcher cannot merge it across
 *  the opaque runs): tp-worst 332 -> 351 with the shader now GPU Resident Drawer compatible (casters back to 898) and the frame faster than 0.2.79 (4.3 / 5.0 ms). */
export const GATES = {
  third: { triangles: 6_000_000, setPass: 375, shadowCasters: 1200, drawn: 2500, beyond150: 1500, meanMs: 8.3, p99Ms: 16.7 },
  play: { triangles: 2_500_000, meanMs: 6 },
} as const;

export const TpLongRow = z.object({
  name: z.string().regex(/^[a-z0-9][a-z0-9-]*$/, 'kebab-case: lower case letters, digits and hyphens'),
  note: z.string().optional(),
  view: z.enum(['follow', 'overhead']),
  agent: z.enum(['scout', 'fixer', 'tech', 'enforcer']).optional(),
  walk: z.tuple([z.number().min(-500).max(500), z.number().min(-500).max(500)]).optional(),
  faceHeading: z.number().min(0).max(360).optional(),
  yaw: z.number().min(0).max(360).optional(), zoom: z.number().min(1).max(60).optional(),
  settle: z.number().min(0).max(10).optional(), seconds: z.number().min(2).max(30).optional(),
  size: z.tuple([z.number().int().min(320).max(7680), z.number().int().min(180).max(4320)]).optional(),
}).strict();
export const TpLongFile = z.object({ schema: z.literal('suborned.tplong/1'), note: z.string().optional(), rows: z.array(TpLongRow).min(1) }).strict();
export type TpLongFile = z.infer<typeof TpLongFile>;

export type TpLongResolved = {
  name: string; view: 'follow' | 'overhead'; kind: 'third' | 'play';
  agent: string; walk: [number, number]; faceHeading: number; yaw: number; zoom: number;
  settleSeconds: number; seconds: number; w: number; h: number;
  /** always: the clock runs, the HUD is on, vsync is off */
  clockRunning: true; hud: true; vsync: false;
};
export type TpLongResolvedFile = { schema: 'suborned.tplong.resolved/1'; outDir: string; rows: TpLongResolved[] };

export function resolveTpLong(raw: unknown, outDir: string): TpLongResolvedFile {
  const p = TpLongFile.safeParse(raw);
  if (!p.success) throw new Error(`tplong.json has ${p.error.issues.length} problem(s):\n  ${p.error.issues.map(i => `${i.path.join('.') || 'tplong.json'}: ${i.message}`).join('\n  ')}`);
  const seen = new Set<string>(), problems: string[] = [], rows: TpLongResolved[] = [];
  for (const r of p.data.rows) {
    const bad = (m: string) => problems.push(`${r.name}: ${m}`);
    if (seen.has(r.name)) bad('the name is used twice'); seen.add(r.name);
    const follow = r.view === 'follow';
    if (follow && r.faceHeading === undefined) bad('a follow view needs faceHeading (degrees clockwise from +z)');
    if (!follow && (r.faceHeading !== undefined || r.walk !== undefined || r.agent !== undefined)) bad('agent, walk and faceHeading go with the follow view');
    if (follow && (r.yaw !== undefined || r.zoom !== undefined)) bad('yaw and zoom go with the overhead (play) view');
    rows.push({
      name: r.name, view: r.view, kind: follow ? 'third' : 'play', agent: r.agent ?? 'scout', walk: r.walk ?? [0, 0], faceHeading: r.faceHeading ?? -1, yaw: r.yaw ?? 30, zoom: r.zoom ?? 14,
      settleSeconds: r.settle ?? 1.5, seconds: r.seconds ?? 6, w: r.size?.[0] ?? DEFAULT_SIZE[0], h: r.size?.[1] ?? DEFAULT_SIZE[1], clockRunning: true, hud: true, vsync: false,
    });
  }
  if (problems.length) throw new Error(`tplong.json: ${problems.join('; ')}`);
  return { schema: 'suborned.tplong.resolved/1', outDir, rows };
}

/** What the player reports per row (suborned.tplong.report/1, <outDir>/tplong.json). Times in ms per rendered frame (unscaled delta); a time the platform cannot give is -1. */
export type TpLongReportRow = {
  name: string; status: 'ok' | 'failed'; error?: string;
  /** the size the window really rendered at, and for a follow view the heading it really faced (degrees, same convention) */
  screen: [number, number]; faceHeading?: number;
  frames: { count: number; meanMs: number; p99Ms: number; maxMs: number };
  /** FrameTiming means over the window */
  gpuMs: number; mainMs: number; renderMs: number;
  /** the Render-category ProfilerRecorder means over the window (camera + shadow passes together, as the Profiler's Rendering module shows them) */
  counters: { triangles: number; setPass: number; shadowCasters: number };
  /** renderers drawn this view (LOD- and static-batch-aware, as probe/Probe.cs Census) and those whose bounds are beyond 150 m of the camera */
  drawn: { renderers: number; beyond150: number };
};
export type TpLongReport = { schema: 'suborned.tplong.report/1'; status: string; build?: string; rows: TpLongReportRow[] };

const f1 = (n: number) => (Math.round(n * 10) / 10).toString();
const mil = (n: number) => `${(Math.round(n / 1e5) / 10).toString()} M`;
const num = (n: number) => Math.round(n).toLocaleString('en-US');

export function judgeTpLong(resolved: TpLongResolvedFile, rep: TpLongReport | undefined, load?: LoadSample): { ok: boolean; problems: string[]; notes: string[]; lines: string[] } {
  const problems: string[] = [], notes: string[] = [], lines: string[] = [];
  if (!rep || rep.schema !== MARKER) return { ok: false, problems: ['the player wrote no suborned.tplong.report/1'], notes, lines };
  if (/^(unreadable|no host)/.test(rep.status)) problems.push(`player: ${rep.status}`);
  const quiet = !noisy(load);
  for (const r of resolved.rows) {
    const x = rep.rows.find(y => y.name === r.name);
    if (!x) { problems.push(`${r.name}: not run (the player stopped before it)`); continue; }
    if (x.status !== 'ok') { problems.push(`${r.name}: ${x.error ?? 'failed'}`); continue; }
    const p = (m: string) => problems.push(`${r.name}: ${m}`), c = x.counters, t = x.frames;
    // the drive was what the row said
    if (x.screen[0] !== r.w || x.screen[1] !== r.h) p(`rendered at ${x.screen[0]} x ${x.screen[1]}, the row says ${r.w} x ${r.h}: the numbers are not the Air's size`);
    if (r.kind === 'third') {
      const d = x.faceHeading === undefined ? NaN : Math.abs(((x.faceHeading - r.faceHeading + 540) % 360) - 180);
      if (!(d <= 2)) p(`faced ${x.faceHeading === undefined ? 'no heading reported' : f1(x.faceHeading) + ' degrees'}, the row says ${r.faceHeading}: the view is not the one named`);
    }
    if (t.count < 100) p(`only ${t.count} frames timed in the window`);
    // gates: counts always, frame times when the machine was quiet
    const g = GATES[r.kind], over: string[] = [], slow: string[] = [];
    if (c.triangles > g.triangles) over.push(`triangles ${mil(c.triangles)} (limit ${mil(g.triangles)})`);
    if (r.kind === 'third') {
      const h = GATES.third;
      if (c.setPass > h.setPass) over.push(`SetPass ${num(c.setPass)} (limit ${h.setPass})`);
      if (c.shadowCasters > h.shadowCasters) over.push(`shadow casters ${num(c.shadowCasters)} (limit ${num(h.shadowCasters)})`);
      if (x.drawn.renderers > h.drawn) over.push(`renderers drawn ${num(x.drawn.renderers)} (limit ${num(h.drawn)})`);
      if (x.drawn.beyond150 > h.beyond150) over.push(`renderers beyond 150 m ${num(x.drawn.beyond150)} (limit ${num(h.beyond150)})`);
      if (t.p99Ms > h.p99Ms) slow.push(`p99 ${f1(t.p99Ms)} ms (limit ${h.p99Ms})`);
    }
    if (t.meanMs > g.meanMs) slow.push(`mean ${f1(t.meanMs)} ms (limit ${g.meanMs})`);
    if (over.length) p(over.join('; '));
    if (slow.length) { if (quiet) p(slow.join('; ')); else notes.push(`${r.name}: ${slow.join('; ')}: NOISY MACHINE, not judged; rerun on a quiet machine to judge it`); }
    lines.push(`${r.name}: mean ${f1(t.meanMs)} / p99 ${f1(t.p99Ms)} / max ${f1(t.maxMs)} ms, gpu ${x.gpuMs < 0 ? 'n/a' : f1(x.gpuMs)} main ${f1(x.mainMs)} render ${f1(x.renderMs)}; ${mil(c.triangles)} tris, SetPass ${num(c.setPass)}, casters ${num(c.shadowCasters)}, drawn ${num(x.drawn.renderers)} (${num(x.drawn.beyond150)} beyond 150 m)`);
  }
  return { ok: problems.length === 0, problems, notes, lines };
}
