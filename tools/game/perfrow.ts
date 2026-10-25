// The machine's load beside the perf numbers (Redline R7, brief 064, 2026-10-26): every batch step that reports frame times records the load
// average (1, 5 and 15 minutes) when it starts and when it ends, and the screen prints it under the row, so a NOISY MACHINE shows in the row
// itself and not only in a note. Also the reader for the perf probe's report (`-perf064 <dir>` writes perf064.json, Perf064.cs).
//
// Fields the reader takes from the player when present (all optional; Latch defines them in Perf064.cs, Redline R7):
//   per window  loadStart, loadEnd         [1, 5, 15] minute load averages at the window's start and end (also {l1,l5,l15} or {m1,m5,m15})
//               mainMs, renderMs, gpuMs    {n, mean, p99, max, over16_7} like frameMs: FrameTimingManager's main-thread, render-thread and GPU time
//   top level   cpuGpuSplit                false/absent = the build has no split yet; the reader says so
import { cpus, loadavg } from 'node:os';
import { NOISY_LOAD_PER_CORE } from './late';

export type Load3 = [number, number, number];
/** The machine's load at the start and at the end of one step. */
export type LoadSpan = { ncpu: number; start: Load3; end: Load3 };

const r1 = (n: number) => Math.round(n * 10) / 10;
const f1 = (n: number) => r1(n).toString();
export const loadNow = (): Load3 => { const l = loadavg(); return [r1(l[0]!), r1(l[1]!), r1(l[2]!)]; };
export const spanStart = (): { ncpu: number; start: Load3 } => ({ ncpu: cpus().length, start: loadNow() });
export const spanEnd = (s: { ncpu: number; start: Load3 }): LoadSpan => ({ ...s, end: loadNow() });

/** Noisy: the 1-minute average at either end reached 0.6 of the cores (the same line as late-fight and tp-long, late.ts). */
export const spanNoisy = (s: LoadSpan | undefined): boolean => !!s && Math.max(s.start[0], s.end[0]) >= NOISY_LOAD_PER_CORE * s.ncpu;
const l3 = (l: Load3): string => l.map(f1).join('/');
export const spanText = (s: LoadSpan | undefined): string => s ? `load 1/5/15 min ${l3(s.start)} at start -> ${l3(s.end)} at end, ${s.ncpu} cores${spanNoisy(s) ? ' · NOISY MACHINE' : ''}` : 'load not recorded';

/** A [1,5,15] reading from whatever shape the player wrote, or undefined. */
export function readLoad3(v: unknown): Load3 | undefined {
  const num = (x: unknown): number | undefined => (typeof x === 'number' && Number.isFinite(x) ? x : undefined);
  if (Array.isArray(v) && v.length >= 3) { const a = v.slice(0, 3).map(num); return a.every(x => x !== undefined) ? (a as Load3) : undefined; }
  if (v && typeof v === 'object') {
    const o = v as Record<string, unknown>;
    for (const keys of [['l1', 'l5', 'l15'], ['m1', 'm5', 'm15'], ['1', '5', '15']]) { const a = keys.map(k => num(o[k])); if (a.every(x => x !== undefined)) return a as Load3; }
  }
  return undefined;
}

type Stats = { n?: number; mean?: number; p99?: number; max?: number; over16_7?: number };
export type Perf064Window = { name: string; legacy?: boolean; seconds?: number; frameMs?: Stats; conesMs?: Stats; mainMs?: Stats; renderMs?: Stats; gpuMs?: Stats; loadStart?: unknown; loadEnd?: unknown };
export type Perf064Report = { schema?: string; status?: string; build?: string; screen?: string; device?: string; windows?: Perf064Window[]; cpuGpuSplit?: boolean };

const st = (label: string, s: Stats | undefined): string | undefined => s && typeof s.mean === 'number' ? `${label} ${f1(s.mean)}${typeof s.p99 === 'number' ? `/${f1(s.p99)}` : ''}${typeof s.max === 'number' ? `/${f1(s.max)}` : ''}` : undefined;

/** One line a window: frame ms (mean/p99/max), the host's cone cost, the CPU/GPU split when the build has it, the window's own load when it has that. */
export function perf064Lines(rep: Perf064Report | undefined, stepNoisy = false): { lines: string[]; noisy: boolean; notes: string[] } {
  const notes: string[] = [];
  if (!rep || !rep.windows?.length) return { lines: [], noisy: stepNoisy, notes: [rep?.status ? `the probe wrote status "${rep.status}"` : 'the probe wrote no windows'] };
  const lines: string[] = [`${rep.build ?? 'build ?'} · ${rep.screen ?? '?'} · ${rep.device ?? '?'} · ms are mean/p99/max`];
  let noisyWin = false, haveLoad = false, haveSplit = false;
  for (const w of rep.windows) {
    const a = readLoad3(w.loadStart), b = readLoad3(w.loadEnd);
    const span: LoadSpan | undefined = a && b ? { ncpu: cpus().length, start: a, end: b } : undefined;
    if (span) { haveLoad = true; if (spanNoisy(span)) noisyWin = true; }
    const split = [st('main', w.mainMs), st('render', w.renderMs), st('gpu', w.gpuMs)].filter((x): x is string => !!x);
    if (split.length) haveSplit = true;
    lines.push([`${w.name}${w.legacy ? ' (legacy)' : ''}`, st('frame', w.frameMs), st('cones', w.conesMs), ...split, span ? `${l3(a!)} -> ${l3(b!)}${spanNoisy(span) ? ' NOISY' : ''}` : undefined].filter(Boolean).join(' · '));
  }
  if (!haveLoad) notes.push('the player records no load per window (loadStart/loadEnd); the step row has the load around the whole probe');
  if (!haveSplit) notes.push('no CPU/GPU split in this report (mainMs/renderMs/gpuMs); frame ms is the wall-clock interval only');
  const noisy = noisyWin || stepNoisy;
  if (noisy) notes.push('NOISY MACHINE: the ms are not judged; rerun on a quiet machine (the counts per second still hold)');
  return { lines, noisy, notes };
}
