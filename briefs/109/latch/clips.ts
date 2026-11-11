// usage: bun briefs/109/latch/clips.ts <app> [--perf] [--only off,on,...]
// Brief 109 round 1 (Latch): the before/after clips and the Studio cost table, from one built player.
//   clips (default): briefs/109/latch/shots.json, played twice: -juice off (before) and -juice on (after), the batch's own clip capture (ShotRunner),
//     then each clip's frames to an mp4 (ffmpeg, h264, at its true game speed) and a side-by-side, before left | after right. Out: out/scratch/latch/109/clips/{off,on}/ (frames),
//     briefs/109/latch/clips/<name>-{before,after,side}.mp4 (git-ignored).
//   --perf: briefs/109/latch/perf.json once per variant (off, on idle, and each -juicedemo moment), its frame times into briefs/109/latch/costs.json.
// Every launch goes through the playing guard (no load while Sky plays) at 1920x1080 windowed, as the batch's shots step does.
import { existsSync, mkdirSync, readdirSync, readFileSync, writeFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { resolveShots } from '../../../tools/game/shots';

const ROOT = resolve(import.meta.dir, '../../..'), HERE = import.meta.dir, OUT = join(ROOT, 'out/scratch/latch/109/clips'), CLIPS = join(HERE, 'clips');
const app = process.argv[2]; if (!app || !existsSync(app)) { console.error('usage: clips.ts <The Suborned.app> [--perf] [--only a,b]'); process.exit(2); }
const perf = process.argv.includes('--perf'); const oi = process.argv.indexOf('--only'); const only = oi > 0 ? process.argv[oi + 1]!.split(',') : null;

function launch(file: string, dir: string, extra: string[]): number {
  mkdirSync(dir, { recursive: true });
  const resolved = resolveShots(JSON.parse(readFileSync(file, 'utf8')), dir); const rf = join(dir, 'shots.resolved.json'); writeFileSync(rf, JSON.stringify(resolved, null, 1));
  const cmd = ['bun', join(ROOT, 'tools/game/playing.ts'), join(app, 'Contents/MacOS/The Suborned'), '-screen-fullscreen', '0', '-screen-width', '1920', '-screen-height', '1080',
    '-shots', rf, dir, '-novsync', '-fastExit', '-logFile', join(dir, 'player.log'), ...extra];
  const t0 = performance.now(); const r = Bun.spawnSync(cmd, { cwd: ROOT, stdout: 'inherit', stderr: 'inherit' });
  console.log(`${extra.join(' ')}: exit ${r.exitCode} in ${((performance.now() - t0) / 1000).toFixed(0)} s (${dir})`); return r.exitCode ?? 1;
}
const ff = (args: string[]) => Bun.spawnSync(['ffmpeg', '-y', '-loglevel', 'error', ...args], { stdout: 'inherit', stderr: 'inherit' }).exitCode;
/** Frames per second of game time: the clip's ticks over its frames (a frame steps the game by its real frame time, about 27 ms); 37.5 when the
 *  clock stopped (the getaway's leaving runs after the end, on the wall clock, at the same frame pace). */
function trueRate(v: string, n: string, count: number): number {
  const res = JSON.parse(readFileSync(join(OUT, v, 'shots.resolved.json'), 'utf8')).shots.find((s: any) => s.name === n), rep = JSON.parse(readFileSync(join(OUT, v, 'shots.json'), 'utf8')).shots.find((s: any) => s.name === n);
  const secs = res && rep ? (rep.tick - res.tick) / 10 : 0; return secs > 0.5 ? count / secs : 37.5;
}
const frames = (d: string) => existsSync(d) ? readdirSync(d).filter(f => /^f\d+\.(jpg|png)$/.test(f)).sort() : [];

if (!perf) {
  const names = (JSON.parse(readFileSync(join(HERE, 'shots.json'), 'utf8')).shots as { name: string }[]).map(s => s.name);
  for (const v of ['off', 'on']) if (!only || only.includes(v)) launch(join(HERE, 'shots.json'), join(OUT, v), ['-juice', v]);
  mkdirSync(CLIPS, { recursive: true });
  for (const n of names) {
    const made: string[] = [];
    for (const [v, tag] of [['off', 'before'], ['on', 'after']] as const) {
      const d = join(OUT, v, n), fs = frames(d); if (!fs.length) { console.log(`${n} ${v}: no frames`); continue; }
      const ext = fs[0]!.endsWith('.png') ? 'png' : 'jpg', out = join(CLIPS, `${n}-${tag}.mp4`), rate = trueRate(v, n, fs.length);
      if (ff(['-framerate', rate.toFixed(2), '-i', join(d, `f%04d.${ext}`), '-vf', 'scale=1920:-2', '-c:v', 'libx264', '-pix_fmt', 'yuv420p', '-crf', '20', out]) === 0) made.push(out);
    }
    if (made.length === 2) {
      const side = join(CLIPS, `${n}-side.mp4`);
      ff(['-i', made[0]!, '-i', made[1]!, '-filter_complex', '[0:v]scale=960:-2[a];[1:v]scale=960:-2[b];[a][b]hstack=inputs=2', '-c:v', 'libx264', '-pix_fmt', 'yuv420p', '-crf', '20', side]);
      console.log(`${n}: ${made.join(', ')}, ${side}`);
    }
  }
} else {
  const variants: [string, string[]][] = [['off', ['-juice', 'off']], ['on-idle', ['-juice', 'on']], ['order', ['-juice', 'on', '-juicedemo', 'order']], ['takedown', ['-juice', 'on', '-juicedemo', 'takedown']], ['getaway', ['-juice', 'on', '-juicedemo', 'getaway']]];
  const rows: Record<string, unknown> = {};
  for (const [id, extra] of variants) {
    if (only && !only.includes(id)) continue;
    const dir = join(OUT, 'perf', id); launch(join(HERE, 'perf.json'), dir, extra);
    const sj = join(dir, 'shots.json'); rows[id] = existsSync(sj) ? JSON.parse(readFileSync(sj, 'utf8')) : { missing: sj };
  }
  writeFileSync(join(HERE, 'costs.raw.json'), JSON.stringify(rows, null, 1)); console.log(`wrote ${join(HERE, 'costs.raw.json')}`);
}
