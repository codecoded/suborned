// usage: bun run shots:shrink [--check] [--apply] [--jpg] [--max-width 1920] [--root briefs,out] [--list]
// Screenshots taken at the Studio's native 6016 x 3384 are 20 to 26 MB each as lossless PNG (Sky, 2026-10-21). This finds the PNGs wider than 1920 and
// shrinks them to 1920 wide (macOS `sips`, keeping the aspect). READY, NOT RUN: without --apply it only reports, and --check says so as well. Nothing is
// changed until Sky says so and someone passes --apply.
//   --check     (the default) list what would be done and the space saved
//   --apply     do it: each file is resampled to a temp file, its size verified, and moved over the original (modified time kept)
//   --jpg       convert to JPEG quality 90 (<name>.jpg) and remove the PNG; the PNG's name may be in a brief or a report, so --check lists the files that name each
//   --list      every file, not the biggest 15
//   --root      folders under the repo to look in (default briefs,out)
// Never touched, whatever their size: files git tracks (concept art and images a tool reads are committed); anything a tool reads lossless: *.hud.png,
// *.world.png, *.debug.png, and a shot that has a HUD layer, manifest or rects beside it (a budget set: its layer and manifest are in that size's pixels,
// so the set would have to be resampled together; the shots tool takes 1080p sets from now on); out/build (the apps), out/missions, out/batch/compile,
// out/drift-auto, out/route-diff, out/world, out/decokit*, Textures folders, node_modules, unity; and anything not screen-shaped (aspect 1.3 to 2.2:
// atlases and textures).
import { closeSync, existsSync, openSync, readdirSync, readFileSync, readSync, renameSync, rmSync, statSync, utimesSync } from 'node:fs';
import { basename, dirname, join, relative, resolve } from 'node:path';
import { pngSize } from './shots';

export const ROOT = resolve(import.meta.dir, '../..');
const SKIP_DIRS = ['node_modules', '.git', 'unity', 'Textures'], SKIP_PATHS = ['out/build', 'out/missions', 'out/batch/compile', 'out/drift-auto', 'out/route-diff', 'out/sim-unity', 'out/world', 'out/kit'];
const SKIP_PREFIX = ['out/decokit']; // the DecoKit's own renders and atlases: assets, not screenshots

export type Found = { file: string; w: number; h: number; bytes: number };
export type Plan = { file: string; w: number; h: number; bytes: number; newW: number; newH: number; estBytes: number; to: string };
export type Kept = { file: string; why: 'committed' | 'budget layer' | 'budget set'; bytes: number };

/** Every PNG under `roots` (absolute folders), with its size from the header; the skipped folders left out. */
export function findPngs(roots: string[], base = ROOT): Found[] {
  const out: Found[] = [];
  const walk = (d: string) => {
    for (const e of readdirSync(d, { withFileTypes: true })) {
      const p = join(d, e.name), r = relative(base, p);
      if (e.isDirectory()) { if (!SKIP_DIRS.includes(e.name) && !SKIP_PATHS.some(s => r === s || r.startsWith(s + '/')) && !SKIP_PREFIX.some(s => r.startsWith(s))) walk(p); continue; }
      if (!e.name.toLowerCase().endsWith('.png')) continue;
      const fd = openSync(p, 'r'), b = new Uint8Array(24);
      try { if (readSync(fd, b, 0, 24, 0) !== 24) continue; } finally { closeSync(fd); }
      const sz = pngSize(b); if (sz) out.push({ file: p, w: sz[0], h: sz[1], bytes: statSync(p).size });
    }
  };
  for (const r of roots) if (existsSync(r)) walk(r);
  return out;
}

/** What to do with each PNG wider than `maxW`, and what to leave and why. `tracked`: the repo-relative paths git holds. */
export function plan(found: Found[], o: { maxW: number; tracked: Set<string>; jpg: boolean; base?: string }): { todo: Plan[]; kept: Kept[]; small: number; other: number } {
  const base = o.base ?? ROOT, todo: Plan[] = [], kept: Kept[] = [];
  let small = 0, other = 0;
  for (const f of found) {
    if (f.w <= o.maxW) { small++; continue; }
    if (f.w / f.h < 1.3 || f.w / f.h > 2.2 || /atlas|texture|albedo|normal/i.test(f.file)) { other++; continue; } // a screenshot is a screen: 16:9 or 16:10; squares are textures and atlases
    const name = basename(f.file), stem = f.file.replace(/\.png$/i, '');
    if (o.tracked.has(relative(base, f.file))) { kept.push({ file: f.file, why: 'committed', bytes: f.bytes }); continue; }
    if (/\.(hud|world|debug)\.png$/i.test(name)) { kept.push({ file: f.file, why: 'budget layer', bytes: f.bytes }); continue; }
    if (['.hud.png', '.world.png', '.manifest.json', '.rects.json'].some(x => existsSync(stem + x))) { kept.push({ file: f.file, why: 'budget set', bytes: f.bytes }); continue; }
    const newW = o.maxW, newH = Math.round(f.h * o.maxW / f.w), ratio = (newW * newH) / (f.w * f.h);
    // estimates: a PNG shrinks about with its pixel count; a JPEG at quality 90 is about 0.45 byte per pixel
    todo.push({ ...f, newW, newH, estBytes: o.jpg ? Math.round(newW * newH * 0.45) : Math.round(f.bytes * ratio), to: o.jpg ? stem + '.jpg' : f.file });
  }
  return { todo, kept, small, other };
}

const mb = (n: number) => `${(n / 1048576).toFixed(1)} MB`;
export function report(p: ReturnType<typeof plan>, o: { list: boolean; jpg: boolean; maxW: number; base?: string; names?: Map<string, string[]> }): string {
  const base = o.base ?? ROOT, out: string[] = [];
  const before = p.todo.reduce((a, t) => a + t.bytes, 0), after = p.todo.reduce((a, t) => a + t.estBytes, 0);
  out.push(`${p.todo.length} PNGs wider than ${o.maxW} px would be ${o.jpg ? 'converted to 1920-wide JPEG (q90)' : `resampled to ${o.maxW} wide, still PNG`}: ${mb(before)} → about ${mb(after)} (estimate), saving about ${mb(before - after)}`);
  const sorted = [...p.todo].sort((a, b) => b.bytes - a.bytes);
  for (const t of o.list ? sorted : sorted.slice(0, 15)) {
    out.push(`  ${mb(t.bytes).padStart(8)} → ~${mb(t.estBytes).padStart(7)}  ${t.w}x${t.h} → ${t.newW}x${t.newH}  ${relative(base, t.file)}${o.jpg ? ` → ${basename(t.to)}` : ''}`);
    if (o.jpg) for (const r of o.names?.get(t.file) ?? []) out.push(`      named in ${relative(base, r)}`);
  }
  if (!o.list && sorted.length > 15) out.push(`  … ${sorted.length - 15} more (--list)`);
  const why = (w: Kept['why']) => p.kept.filter(k => k.why === w);
  out.push(`left alone: ${why('committed').length} committed (git tracks them), ${why('budget layer').length} HUD layers and ${why('budget set').length} shots that have one (${mb(p.kept.reduce((a, k) => a + k.bytes, 0))}); ${p.small} PNGs already ${o.maxW} wide or less; ${p.other} wide PNGs that are not screens (square atlases, textures)`);
  return out.join('\n');
}

/** Resample one file with sips into a temp file, verify it, and move it over (or beside, for jpg). Returns the new size in bytes. */
export function shrinkOne(t: Plan, jpg: boolean): number {
  const tmp = join(dirname(t.file), `.shrink-${process.pid}-${basename(t.to)}`), mtime = statSync(t.file).mtime;
  const args = ['--resampleWidth', String(t.newW), ...(jpg ? ['-s', 'format', 'jpeg', '-s', 'formatOptions', '90'] : []), t.file, '--out', tmp];
  const r = Bun.spawnSync(['sips', ...args], { stdout: 'pipe', stderr: 'pipe' });
  if (r.exitCode !== 0 || !existsSync(tmp)) { rmSync(tmp, { force: true }); throw new Error(`sips failed on ${t.file}: ${r.stderr.toString() || r.stdout.toString()}`); }
  if (!jpg) { const sz = pngSize(new Uint8Array(readFileSync(tmp).subarray(0, 24))); if (!sz || sz[0] !== t.newW) { rmSync(tmp, { force: true }); throw new Error(`${t.file}: the shrunk copy is ${sz?.join('x')}, not ${t.newW} wide; left as it was`); } }
  renameSync(tmp, t.to); utimesSync(t.to, mtime, mtime);
  if (jpg) rmSync(t.file);
  return statSync(t.to).size;
}

function trackedSet(): Set<string> {
  const r = Bun.spawnSync(['git', 'ls-files', '-z', '--', '*.png'], { cwd: ROOT, stdout: 'pipe', stderr: 'pipe' });
  return new Set(r.stdout.toString().split('\0').filter(Boolean));
}

if (import.meta.main) {
  const args = process.argv.slice(2), flag = (k: string) => { const i = args.indexOf(k); return i >= 0 ? args.splice(i, 2)[1] : undefined; }, bool = (k: string) => { const i = args.indexOf(k); if (i >= 0) args.splice(i, 1); return i >= 0; };
  const maxW = Number(flag('--max-width') ?? 1920), roots = (flag('--root') ?? 'briefs,out').split(',').map(r => resolve(ROOT, r));
  const apply = bool('--apply'), jpg = bool('--jpg'), list = bool('--list'); bool('--check');
  if (args.length || !(maxW >= 320)) { console.error(`unknown argument ${args.join(' ') || '--max-width'}; see the usage line at the top of tools/game/shrink.ts`); process.exit(2); }
  const t0 = performance.now(), found = findPngs(roots), p = plan(found, { maxW, tracked: trackedSet(), jpg });
  let names: Map<string, string[]> | undefined;
  if (jpg && p.todo.length) { // who names a PNG: briefs and reports that would point at a file that is gone
    const r = Bun.spawnSync(['rg', '-o', '--no-messages', '-I', '-H', '-e', '[A-Za-z0-9_./-]+\\.png', '-g', '*.md', '-g', '*.html', 'briefs', 'out/latch'], { cwd: ROOT, stdout: 'pipe', stderr: 'pipe' });
    const by = new Map<string, Set<string>>();
    for (const l of r.stdout.toString().split('\n')) { const i = l.indexOf(':'); if (i < 0) continue; const k = basename(l.slice(i + 1)); (by.get(k) ?? by.set(k, new Set()).get(k)!).add(resolve(ROOT, l.slice(0, i))); }
    names = new Map(p.todo.map(t => [t.file, [...(by.get(basename(t.file)) ?? [])].slice(0, 3)]));
  }
  console.log(report(p, { list, jpg, maxW, names }));
  if (!apply) { console.log(`\nnothing changed (${((performance.now() - t0) / 1000).toFixed(1)} s). --apply does it, once Sky has said so.`); process.exit(0); }
  let saved = 0, done = 0; const bad: string[] = [];
  for (const t of p.todo) { try { saved += t.bytes - shrinkOne(t, jpg); done++; } catch (e) { bad.push((e as Error).message); } }
  console.log(`\nshrunk ${done} of ${p.todo.length}, saved ${mb(saved)}${bad.length ? `; ${bad.length} failed:\n  ${bad.slice(0, 10).join('\n  ')}` : ''}`);
  process.exit(bad.length ? 1 : 0);
}
