// usage: bun briefs/109/latch/clips.ts <app> [--shots <file>] [--before "<args>"] [--after "<args>"] [--perf [--round 1|4]] [--only off,on,...]
// Brief 109 round 1 (Latch): the before/after clips and the Studio cost table, from one built player.
//   clips (default): briefs/109/latch/shots.json, played twice: -juice off (before) and -juice on (after), the batch's own clip capture (ShotRunner),
//     then each clip's frames to an mp4 (ffmpeg, h264, at the shot's own clip fps with constant timestamps) and a side-by-side, before left | after right. Out: out/scratch/latch/109/clips/{off,on}/ (frames),
//     briefs/109/latch/clips/<name>-{before,after,side}.mp4 (git-ignored).
//   --perf: briefs/109/latch/perf.json once per variant (off, on idle, and each -juicedemo moment), its frame times into briefs/109/latch/costs.json.
// Every launch goes through the playing guard (no load while Sky plays) at 1920x1080 windowed, as the batch's shots step does.
import { existsSync, mkdirSync, readdirSync, readFileSync, writeFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { resolveShots } from '../../../tools/game/shots';

const ROOT = resolve(import.meta.dir, '../../..'), HERE = import.meta.dir, OUT = join(ROOT, 'out/scratch/latch/109/clips'), CLIPS = join(HERE, 'clips');
const app = process.argv[2]; if (!app || !existsSync(app)) { console.error('usage: clips.ts <The Suborned.app> [--perf] [--only a,b]'); process.exit(2); }
const perf = process.argv.includes('--perf'); const argOf = (k: string) => { const i = process.argv.indexOf(k); return i > 0 ? process.argv[i + 1]! : null; };
const BEFORE = (argOf('--before') ?? '-juice off').split(' ').filter(Boolean), AFTER = (argOf('--after') ?? '-juice on').split(' ').filter(Boolean); const si = process.argv.indexOf('--shots'); const SHOTS = si > 0 ? resolve(process.argv[si + 1]!) : join(HERE, 'shots.json'); const oi = process.argv.indexOf('--only'); const only = oi > 0 ? process.argv[oi + 1]!.split(',') : null; // round 3: the tilt-shift pair is -juice on vs -juice on -tiltshift on

function launch(file: string, dir: string, extra: string[]): number {
  mkdirSync(dir, { recursive: true });
  const resolved = resolveShots(JSON.parse(readFileSync(file, 'utf8')), dir); const rf = join(dir, 'shots.resolved.json'); writeFileSync(rf, JSON.stringify(resolved, null, 1));
  const cmd = ['bun', join(ROOT, 'tools/game/playing.ts'), join(app, 'Contents/MacOS/The Suborned'), '-screen-fullscreen', '0', '-screen-width', '1920', '-screen-height', '1080',
    '-shots', rf, dir, '-novsync', '-fastExit', '-logFile', join(dir, 'player.log'), ...extra];
  const t0 = performance.now(); const r = Bun.spawnSync(cmd, { cwd: ROOT, stdout: 'inherit', stderr: 'inherit' });
  console.log(`${extra.join(' ')}: exit ${r.exitCode} in ${((performance.now() - t0) / 1000).toFixed(0)} s (${dir})`); return r.exitCode ?? 1;
}
const ff = (args: string[]) => Bun.spawnSync(['ffmpeg', '-y', '-loglevel', 'error', ...args], { stdout: 'inherit', stderr: 'inherit' }).exitCode;
/** The clip's own capture rate (the shot's clip.fps): every frame gets a constant timestamp at that rate. Round 1 derived a rate from the ticks a clip
 *  covered, which broke when the clock stopped (the getaway's end: 360 frames over 0.6 s of ticks played in under a second); Aster re-encoded them. */
function shotFps(v: string, n: string): number {
  const res = JSON.parse(readFileSync(join(OUT, v, 'shots.resolved.json'), 'utf8')).shots.find((s: any) => s.name === n);
  return res?.clipFps > 0 ? res.clipFps : 30;
}
const frames = (d: string) => existsSync(d) ? readdirSync(d).filter(f => /^f\d+\.(jpg|png)$/.test(f)).sort() : [];

if (!perf) {
  const names = (JSON.parse(readFileSync(SHOTS, 'utf8')).shots as { name: string }[]).map(s => s.name);
  for (const v of ['off', 'on']) if (!only || only.includes(v)) launch(SHOTS, join(OUT, v), v === 'off' ? BEFORE : AFTER);
  mkdirSync(CLIPS, { recursive: true });
  for (const n of names) {
    const made: string[] = [];
    for (const [v, tag] of [['off', 'before'], ['on', 'after']] as const) {
      const d = join(OUT, v, n), fs = frames(d); if (!fs.length) { console.log(`${n} ${v}: no frames`); continue; }
      const ext = fs[0]!.endsWith('.png') ? 'png' : 'jpg', out = join(CLIPS, `${n}-${tag}.mp4`), rate = shotFps(v, n);
      if (ff(['-framerate', String(rate), '-i', join(d, `f%04d.${ext}`), '-vf', `scale=1920:-2,setpts=N/(${rate}*TB)`, '-r', String(rate), '-c:v', 'libx264', '-pix_fmt', 'yuv420p', '-crf', '20', out]) === 0) made.push(out);
    }
    if (made.length === 2) {
      const side = join(CLIPS, `${n}-side.mp4`);
      ff(['-i', made[0]!, '-i', made[1]!, '-filter_complex', '[0:v]scale=960:-2[a];[1:v]scale=960:-2[b];[a][b]hstack=inputs=2', '-r', String(shotFps('on', n)), '-c:v', 'libx264', '-pix_fmt', 'yuv420p', '-crf', '20', side]);
      console.log(`${n}: ${made.join(', ')}, ${side}`);
    }
  }
} else {
  // round 3: each variant names its shot file (perf.json day, perf-night.json the same moment at night); -juicedemo repeats one moment without end
  const day = join(HERE, 'perf.json'), night = join(HERE, 'perf-night.json');
  const variants: [string, string, string[]][] = argOf('--round') === '1' ? [['off', day, ['-juice', 'off']], ['on-idle', day, ['-juice', 'on']], ['order', day, ['-juice', 'on', '-juicedemo', 'order']], ['getaway', day, ['-juice', 'on', '-juicedemo', 'getaway']]]
    : [['off', day, ['-juice', 'off']], ['on-idle', day, ['-juice', 'on']], ['turn', day, ['-juice', 'on', '-juicedemo', 'turn']], ['shot', day, ['-juice', 'on', '-juicedemo', 'shot']], ['tiltshift', day, ['-juice', 'off', '-tiltshift', 'on']],
       ['night-off', night, ['-juice', 'off']], ['night-on', night, ['-juice', 'on']]];
  if (argOf('--round') === '4') { const drive = join(HERE, 'perf-drive.json'); variants.splice(0, variants.length, ['off', day, ['-juice', 'off']], ['on-idle', day, ['-juice', 'on']], ['win', day, ['-juice', 'on', '-juicedemo', 'win']], ['hit', day, ['-juice', 'on', '-juicedemo', 'hit']], ['shot', day, ['-juice', 'on', '-juicedemo', 'shot']], ['night-off', night, ['-juice', 'off']], ['night-on', night, ['-juice', 'on']], ['drive-off', drive, ['-juice', 'off']], ['drive-on', drive, ['-juice', 'on']]); } // round 4: social wins, hit flash, the lights
  const rows: Record<string, unknown> = {};
  for (const [id, file, extra] of variants) {
    if (only && !only.includes(id)) continue;
    const dir = join(OUT, 'perf', id); launch(file, dir, extra);
    const sj = join(dir, 'shots.json'); rows[id] = existsSync(sj) ? JSON.parse(readFileSync(sj, 'utf8')) : { missing: sj };
  }
  writeFileSync(join(HERE, 'costs.raw.json'), JSON.stringify(rows, null, 1)); console.log(`wrote ${join(HERE, 'costs.raw.json')}`);
}
