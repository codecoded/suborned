// Brief 075 golden view (Latch, 2026-10-28): rebuild the scene, render the one boulevard frame, rebuild index.html.
//
// usage (from the repo root):
//   bun briefs/075/golden/golden.ts <step> --what "what changed" [--by name] [--no-build]
//        world export (bun run world) -> pinned copy -> bun run kit -> Unity HarbourWorldBuilder.Build -> Unity Golden075.Cli -tag <step>
//        -> briefs/075/golden/<step>.png + <step>.json, steps.json entry (replaced if the step exists), index.html
//        --no-build: render the scene as it is now (no export, no kit copy, no Unity build)
//        --life t: Pulse's presentation life (bun run life:presentation from this export) posed at presentation time t
//        --life-cmd f.ts: make the life with bun f.ts instead of bun run life:presentation (a scratch copy while Pulse's tool lags the plan, e.g. 075 remap: T2 gone)
//   bun briefs/075/golden/golden.ts --old      render the old frame once from out/scratch/latch/oldworld/unity -> old.png
//   bun briefs/075/golden/golden.ts --index    rebuild index.html from steps.json only
//
// Every Unity run is batchmode (offscreen, no window, no desktop capture), wrapped in the Sky-is-playing guard (tools/game/playing.ts), and waits
// while another Unity has the same project open. Logs: out/scratch/latch/golden075/<step>-*.log.
import { spawnSync } from 'node:child_process';
import { copyFileSync, existsSync, mkdirSync, readFileSync, statSync, writeFileSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';

const root = resolve(import.meta.dir, '../../..');
const here = join(root, 'briefs/075/golden');
const scratch = join(root, 'out/scratch/latch/golden075');
const UNITY = '/Applications/Unity/Hub/Editor/6000.6.2f1/Unity.app/Contents/MacOS/Unity';
const OLD_PROJECT = join(root, 'out/scratch/latch/oldworld/unity');
const stepsPath = join(here, 'steps.json');

type Stats = { satMean: number; lumaMedian: number; p5: number; p95: number; leftThirdMedian: number; rightThirdMedian: number };
type Step = { step: string; date: string; what: string; by?: string; export_sha256?: string; life_t?: number; stats?: Stats };

/** Glint's frame stats (stats.py: HSV saturation mean, Rec. 601 luma median, p5, p95, third medians) of a rendered frame. */
function stats(png: string): Stats {
  const r = spawnSync('python3', ['-I', join(here, 'stats.py'), png], { encoding: 'utf8' });
  if (r.status !== 0) { console.error(`golden: stats failed on ${png}: ${r.stderr}`); process.exit(1); }
  return JSON.parse(r.stdout);
}
const fmt = (s: Stats) => `sat ${s.satMean} · median ${s.lumaMedian} · p5 ${s.p5} · p95 ${s.p95} · left/right third ${s.leftThirdMedian}/${s.rightThirdMedian}`;
type Steps = { schema: 'latch.golden075-steps/1'; steps: Step[] };

const args = process.argv.slice(2);
const opt = (k: string) => (args.includes(k) ? args[args.indexOf(k) + 1] : undefined);
const has = (k: string) => args.includes(k);

function run(label: string, cmd: string[], log?: string) {
  console.log(`golden: ${label}: ${cmd.join(' ')}`);
  const r = spawnSync(cmd[0]!, cmd.slice(1), { cwd: root, stdio: 'inherit' });
  if (r.status !== 0) {
    console.error(`golden: ${label} failed (exit ${r.status})${log ? `; log ${log}` : ''}`);
    if (log && existsSync(log)) {
      const lines = readFileSync(log, 'utf8').split('\n').filter(l => /error|exception|golden075/i.test(l)).slice(-25);
      console.error(lines.join('\n'));
    }
    process.exit(r.status ?? 1);
  }
}

/** Waits while another Unity has this project open (two editors on one project fail at the lock). */
function waitForProject(project: string) {
  const t0 = Date.now();
  for (;;) {
    const ps = spawnSync('ps', ['-Ao', 'pid=,command='], { encoding: 'utf8' }).stdout ?? '';
    const busy = ps.split('\n').filter(l => l.includes('/Unity.app/Contents/MacOS/Unity') && (l.includes(`-projectPath ${project}`) || l.includes(`-projectPath ${resolve(project)}`) ||
      (project === 'unity' && l.includes(`-projectPath ${join(root, 'unity')}`))));
    if (!busy.length) return;
    if (Date.now() - t0 > 30 * 60_000) { console.error(`golden: project ${project} still open after 30 min (${busy.map(b => b.trim().split(' ')[0]).join(', ')})`); process.exit(3); }
    if ((Date.now() - t0) % 60_000 < 5_000) console.log(`golden: Unity has ${project} open (${busy.map(b => b.trim().split(' ')[0]).join(', ')}); waiting`);
    Bun.sleepSync(5_000);
  }
}

function unity(label: string, project: string, method: string, extra: string[], log: string) {
  waitForProject(project);
  run(label, ['bun', 'tools/game/playing.ts', UNITY, '-batchmode', '-quit', '-projectPath', project, '-executeMethod', method, ...extra, '-logFile', log], log);
}

function loadSteps(): Steps {
  return existsSync(stepsPath) ? JSON.parse(readFileSync(stepsPath, 'utf8')) : { schema: 'latch.golden075-steps/1', steps: [] };
}

const esc = (s: string) => s.replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' })[c]!);

function index() {
  const { steps } = loadSteps();
  const v = (f: string) => (existsSync(join(here, f)) ? `${f}?v=${Math.round(statSync(join(here, f)).mtimeMs)}` : '');
  const card = (img: string, title: string, caption: string) =>
    `<figure>${img ? `<img src="${img}" alt="${esc(title)}">` : `<div class="missing">not rendered</div>`}<figcaption><b>${esc(title)}</b> ${esc(caption)}</figcaption></figure>`;
  const before = steps.find(s => s.step === 'before');
  const rest = steps.filter(s => s.step !== 'before').reverse(); // newest first
  const cards = [
    card(v('old.png'), 'old', 'the old DecoKit reel, rendered from the old project copy at its t 10 s camera'),
    ...rest.map(s => card(v(`${s.step}.png`), s.step, `${s.date} · ${s.what}${s.by ? ` (${s.by})` : ''}${s.stats && !s.what.includes('sat ') ? ` | ${fmt(s.stats)}` : ''}`)),
    before ? card(v('before.png'), 'before', `${before.date} · ${before.what}`) : '',
  ].join('\n');
  const html = `<!doctype html><html lang="en"><head><meta charset="utf-8"><title>075 golden view</title>
<style>
body{margin:0;background:#0b1615;color:#f7f4ec;font:14px/1.4 Jost,system-ui,sans-serif}
header{padding:12px 16px}h1{font-size:18px;margin:0 0 4px}p{margin:0;color:#b0cdc7}
.strip{display:flex;gap:12px;overflow-x:auto;padding:0 16px 16px;align-items:flex-start}
figure{margin:0;flex:0 0 auto;width:min(46vw,960px)}figure img,.missing{width:100%;aspect-ratio:16/9;display:block;border:1px solid #17302f;object-fit:contain;background:#000}
.missing{display:flex;align-items:center;justify-content:center;color:#b0cdc7}
figcaption{padding:6px 2px;color:#d2e6e2}figcaption b{color:#f7f4ec}
details{padding:0 16px 16px}summary{cursor:pointer;color:#b0cdc7}
</style></head><body>
<header><h1>Brief 075 · golden view: the boulevard toward Crown Circus</h1>
<p>old | newest step … | before, every frame a true render at 1920 × 1080 (supersample 2), fov 58. Cameras: cameras.json. Steps: steps.json. Rebuild one step: <code>bun briefs/075/golden/golden.ts &lt;step&gt; --what "…"</code></p></header>
<div class="strip">
${cards}
</div>
<details><summary>Sky's two frames (reference, 2026-10-28)</summary><div class="strip">
${card(v('sky-old.png'), 'sky-old', 'Sky\'s frame of the old reel')}
${card(v('sky-ours.png'), 'sky-ours', 'Sky\'s frame of our flyby')}
</div></details>
</body></html>
`;
  writeFileSync(join(here, 'index.html'), html);
  console.log(`golden: index.html (${steps.length} steps)`);
}

mkdirSync(scratch, { recursive: true });

if (has('--index')) { index(); process.exit(0); }

if (has('--old')) {
  if (!existsSync(OLD_PROJECT)) { console.error(`golden: no old project copy at ${OLD_PROJECT}`); process.exit(1); }
  const log = join(scratch, 'old.log');
  unity('old frame', OLD_PROJECT, 'TheSuborned.Editor.DecoKit.DecoKitMenu.Flyby', ['-dkOut', 'golden', '-dkStoryboard', '1', '-dkFrom', '10', '-dkTo', '10'], log);
  const src = join(OLD_PROJECT, 'Scratch/Captures/DecoKit/golden_t10.png');
  if (!existsSync(src)) { console.error(`golden: old render missing (${src}); log ${log}`); process.exit(1); }
  copyFileSync(src, join(here, 'old.png'));
  console.log(`golden: old.png from ${src}`);
  index();
  process.exit(0);
}

const step = args.find(a => !a.startsWith('--') && a !== opt('--what') && a !== opt('--by') && a !== opt('--life') && a !== opt('--life-cmd'));
const lifeT = opt('--life'); // brief 075 s3: Pulse's presentation life posed at presentation time t (Golden075 -life t)
if (lifeT !== undefined && !/^[0-9]+(\.[0-9]+)?$/.test(lifeT)) { console.error(`golden: --life wants seconds, got ${lifeT}`); process.exit(2); }
const what = opt('--what');
if (!step || !/^[A-Za-z0-9._-]+$/.test(step) || !what) {
  console.error('usage: bun briefs/075/golden/golden.ts <step> --what "what changed" [--by name] [--no-build] [--life t] | --old | --index');
  process.exit(2);
}
if (step === 'old' || step.startsWith('sky-')) { console.error(`golden: step name ${step} is reserved`); process.exit(2); }

if (!has('--no-build')) {
  run('world export', ['bun', 'run', 'world']);
  // the play host (PlayInstall) refuses a walk file made from another export: regenerate it from this one (3 s, no pictures)
  run('walk file', ['bun', 'tools/game/playing.ts', 'bun', 'tools/geometry/cli.ts', '--no-shots']);
  const pin = join(scratch, 'pin', `${step}.world.json`);
  mkdirSync(dirname(pin), { recursive: true });
  copyFileSync(join(root, 'out/world/harbour-district.world.json'), pin);
  run('kit copy', ['bun', 'tools/game/playing.ts', 'bun', 'run', 'kit']);
  unity('scene build', 'unity', 'Suborned.World.HarbourWorldBuilder.Build', ['-world', pin], join(scratch, `${step}-build.log`));
}
if (lifeT !== undefined) run('presentation life', ['bun', 'tools/game/playing.ts', 'bun', ...(opt('--life-cmd') ? [opt('--life-cmd')!] : ['run', 'life:presentation'])]); // from this world export (git-ignored, regenerated)
unity('golden render', 'unity', 'Suborned.World.Review.Golden075.Cli', ['-tag', step, ...(lifeT !== undefined ? ['-life', lifeT] : [])], join(scratch, `${step}-render.log`));

const frame = JSON.parse(readFileSync(join(here, `${step}.json`), 'utf8'));
const s = loadSteps();
const st = stats(join(here, `${step}.png`));
const entry: Step = { step, date: new Date().toISOString().slice(0, 16).replace('T', ' ') + 'Z', what, ...(opt('--by') ? { by: opt('--by') } : {}), export_sha256: frame.export_sha256, ...(lifeT !== undefined ? { life_t: Number(lifeT) } : {}), stats: st };
const old = existsSync(join(here, 'old.png')) ? stats(join(here, 'old.png')) : undefined;
console.log(`golden: ${step}: ${fmt(st)}${old ? `\ngolden: old:  ${fmt(old)}` : ''}`);
const at = s.steps.findIndex(x => x.step === step);
if (at >= 0) s.steps[at] = entry; else s.steps.push(entry);
writeFileSync(stepsPath, JSON.stringify(s, null, 2) + '\n');
index();
console.log(`golden: ${step}.png done`);
