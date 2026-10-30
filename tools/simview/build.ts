// bun run simview (brief 081): the Handover's runs through the real engine (suborned-sim dump), the real world's Handover zone, one self-contained page: briefs/081/simview.html.
// Scripts and the hashes they must end on come from the newest mission record (runs/missions/handover/cNNN); exit 1 if any run's final hash differs from it, or the dump's reads moved a checkpoint.
// usage: bun run simview [--record c046] [--out path] [--crowd-step n]
import { existsSync, mkdirSync, readdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { buildScene } from './scene';

export const ROOT = resolve(import.meta.dir, '../..');
const env = { ...process.env, DOTNET_CLI_TELEMETRY_OPTOUT: '1', DOTNET_NOLOGO: '1', ...(process.env.DOTNET_ROOT || !existsSync('/opt/homebrew/opt/dotnet/libexec') ? {} : { DOTNET_ROOT: '/opt/homebrew/opt/dotnet/libexec' }) };
const BIN = join(ROOT, 'out/simview/bin'), DUMPS = join(ROOT, 'out/simview/dump');
const MAX_BYTES = 40 * 1024 * 1024;

/** A run as the page knows it. `seed` stays null until brief 080 lands: the key is {script, seed} so a seed picker slots in without changing the page's model. */
export const RUNS: { script: string; title: string; blurb: string; scenario?: string }[] = [
  { script: 'route-a', title: 'Route A · Inez (front door)', blurb: 'Quiet: Inez calls the guard away, the Enforcer takes Vale out by the service door, then the fight with the rivals.' },
  { script: 'route-b', title: 'Route B · the driver', blurb: 'Quiet: the turned driver brings the car round; Vale is taken at the pickup.' },
  { script: 'route-g', title: 'Route G · the guard', blurb: 'The door guard is turned, and the door is open.' },
  { script: 'route-g-kill', title: 'Route G · kill', blurb: 'The same way in with a killing at the door.' },
  { script: 'loud', title: 'Loud', blurb: 'Guns from the door: straight in, straight out.' },
  { script: 'loud-fight', title: 'Loud · fight it out', blurb: 'Loud, and the cell stays to fight the rivals and the response.' },
  { script: 'careless-takedown-seen-camera', title: 'Fail · takedown seen by camera', blurb: 'A takedown in sight of a live camera.' },
  { script: 'fail-take-seen-camera', title: 'Fail · abduction seen by camera', blurb: 'Vale is taken in front of the pickup camera.' },
  { script: 'fail-rivals-reach-vale', title: 'Fail · rivals reach Vale', blurb: 'The rival crew gets to Vale first.' },
  { script: 'fail-enforcer-down', title: 'Fail · Enforcer down', blurb: 'The Enforcer goes down before the take.' },
  { script: 'fail-vale-dead', title: 'Fail · Vale dead', blurb: 'Vale is killed.' },
  { script: 'car-blast', scenario: 'CAR-BLAST', title: 'Test · car blast', blurb: "The engine's own CAR-BLAST scenario, not a mission route (no recorded route blows a car): shot to disabled, burning, exploding. Played on 10 s past the blast so it can be seen." },
];

export function latestRecord(): string {
  const dir = join(ROOT, 'runs/missions/handover');
  const all = readdirSync(dir).filter(d => /^c\d+$/.test(d) && existsSync(join(dir, d, 'run.json')) && existsSync(join(dir, d, 'routes'))).sort((a, b) => +a.slice(1) - +b.slice(1));
  if (!all.length) throw new Error('no mission record under runs/missions/handover');
  return all[all.length - 1]!;
}

/** Build the CLI into its own folder, so nobody else's bin moves under them. */
export function buildCli(): string {
  const r = Bun.spawnSync(['dotnet', 'build', join(ROOT, 'sim/cli'), '-c', 'Release', '-o', BIN, '-v', 'q', '-nologo'], { cwd: ROOT, env, stdout: 'pipe', stderr: 'pipe' });
  if (r.exitCode !== 0) throw new Error(`dotnet build sim/cli failed:\n${r.stdout}${r.stderr}`);
  return join(BIN, 'suborned-sim.dll');
}

/** A scenario as a script (suborned-sim script), and a copy played on `more` ticks so the end can be seen. The copy's record: the unextended script, dumped too, must end on the scenario's own final checkpoint, and the copy must pass through its earlier checkpoints. */
function scenarioScript(dll: string, scenario: string, more = 100): { path: string; exact: string; expect: { tick: number; hash: string; outcome: string } } {
  const r = Bun.spawnSync(['dotnet', dll, 'script', scenario], { cwd: ROOT, env, stdout: 'pipe', stderr: 'pipe' });
  if (r.exitCode !== 0) throw new Error(`script ${scenario}: ${r.stderr}`);
  const s = JSON.parse(r.stdout.toString()), last = s.expect.checkpoints.at(-1) as [number, string];
  const expect = { tick: last[0], hash: last[1], outcome: s.expect.outcome as string };
  const exact = join(DUMPS, `.${scenario}.exact.json`); writeFileSync(exact, JSON.stringify(s));
  delete s.expect; s.until += more;
  const path = join(DUMPS, `.${scenario}.script.json`); writeFileSync(path, JSON.stringify(s));
  return { path, exact, expect };
}

export async function dump(dll: string, record: string, script: string, crowdStep = 1, scenario?: string): Promise<{ code: number; path: string; say: string; recorded: any; data: any }> {
  mkdirSync(DUMPS, { recursive: true });
  const sc = scenario ? scenarioScript(dll, scenario) : null;
  const src = sc ? sc.path : join(ROOT, 'runs/missions/handover', record, 'routes', `${script}.json`), path = join(DUMPS, `${script}.json`);
  const p = Bun.spawn(['dotnet', dll, 'dump', src, path, '--crowd-step', String(crowdStep)], { cwd: ROOT, env, stdout: 'pipe', stderr: 'pipe' });
  const [out, err] = await Promise.all([new Response(p.stdout).text(), new Response(p.stderr).text()]);
  const code = await p.exited;
  if (code !== 0 && !existsSync(path)) throw new Error(`dump ${script}: ${out}${err}`);
  const data = JSON.parse(readFileSync(path, 'utf8'));
  if (sc) {
    const probe = join(DUMPS, `.${scenario}.exact.dump.json`);
    const q = Bun.spawnSync(['dotnet', dll, 'dump', sc.exact, probe, '--no-fans'], { cwd: ROOT, env, stdout: 'pipe', stderr: 'pipe' });
    const x = JSON.parse(readFileSync(probe, 'utf8'));
    const longer = new Map<number, string>(data.checkpoints.map((c: [number, string]) => [c[0], c[1]]));
    const pass = q.exitCode === 0 && x.finalHash === sc.expect.hash && x.checkpoints.slice(0, -1).every((c: [number, string]) => longer.get(c[0]) === c[1]);
    return { code, path, say: out.trim() + err.trim(), data, recorded: pass ? { finalHash: data.finalHash, outcome: data.outcome, fail: data.fail, tick: data.tick } : { finalHash: `scenario ${scenario}: final ${x.finalHash}, expected ${sc.expect.hash}` } };
  }
  const recorded = JSON.parse(readFileSync(join(ROOT, 'runs/missions/handover', record, 'run.json'), 'utf8')).plays?.[script];
  return { code, path, say: out.trim() + err.trim(), recorded, data };
}

const b64gz = (s: string) => Buffer.from(Bun.gzipSync(Buffer.from(s), { level: 9 })).toString('base64');

async function main() {
  const args = process.argv.slice(2), opt = (k: string) => { const i = args.indexOf(k); return i >= 0 ? args[i + 1] : undefined; };
  const record = opt('--record') ?? latestRecord(), out = resolve(opt('--out') ?? join(ROOT, 'briefs/081/simview.html'));
  let crowdStep = +(opt('--crowd-step') ?? 1);
  console.log(`record ${record}; building sim/cli`);
  const dll = buildCli();
  const worldPath = join(ROOT, 'out/world/harbour-district.world.json');
  if (!existsSync(worldPath)) throw new Error('out/world/harbour-district.world.json is missing: bun run world');
  const scene = buildScene(JSON.parse(readFileSync(worldPath, 'utf8')), JSON.parse(readFileSync(join(ROOT, 'sim/data/handover.site.json'), 'utf8')));
  const bundle = await Bun.build({ entrypoints: [join(import.meta.dir, 'app.ts')], target: 'browser', format: 'esm', minify: true });
  if (!bundle.success) throw new Error(bundle.logs.join('\n'));
  const js = (await bundle.outputs[0]!.text()).replace(/<\/script/gi, '<\\/script');
  const css = readFileSync(join(import.meta.dir, 'style.css'), 'utf8'), shell = readFileSync(join(import.meta.dir, 'shell.html'), 'utf8');
  let bad = 0;
  for (;;) {
    bad = 0;
    const runs: any[] = [], blobs: string[] = [];
    const done = await Promise.all(RUNS.map(r => dump(dll, record, r.script, crowdStep, r.scenario)));
    done.forEach((d, i) => {
      const r = RUNS[i]!, rec = d.recorded, x = d.data;
      const ok = d.code === 0 && x.readsChangedNothing && rec && rec.finalHash === x.finalHash && rec.outcome === x.outcome && rec.fail === x.fail && rec.tick === x.tick;
      if (!ok) bad++;
      console.log(`${ok ? 'ok  ' : 'FAIL'} ${r.script}: ${x.outcome} ${x.fail} t${x.tick} final ${x.finalHash} record ${rec?.finalHash ?? 'none'} reads ${x.readsChangedNothing ? 'clean' : 'MOVED THE RUN'} shots ${x.shots.length}`);
      runs.push({ id: r.script, key: `${r.script}#${x.seed ?? 'noseed'}`, script: r.script, seed: x.seed, title: r.title, blurb: r.blurb, outcome: x.outcome, fail: x.fail, failText: x.failText, tick: x.tick, shots: x.shots.length, finalHash: x.finalHash, recorded: rec?.finalHash ?? '', hashOk: !!ok, alert: +x.hud.at(-1)[1].split(',')[0], trail: +x.hud.at(-1)[1].split(',')[1], scenario: !!r.scenario });
      blobs.push(`<script type="text/plain" id="run-${i}">${b64gz(JSON.stringify(x))}</script>`);
    });
    const manifest = { engine: done[0]!.data.engine, record, built: new Date().toISOString().slice(0, 16).replace('T', ' '), inputs: done[0]!.data.inputsHash, crowdStep, runs };
    const html = shell
      .replace('/*CSS*/', () => css)
      .replace('<!--DATA-->', () => `<script type="application/json" id="manifest">${JSON.stringify(manifest).replace(/</g, '\\u003c')}</script>\n<script type="text/plain" id="scene">${b64gz(JSON.stringify(scene))}</script>\n${blobs.join('\n')}`)
      .replace('/*JS*/', () => js);
    if (html.length > MAX_BYTES && crowdStep < 10) { crowdStep = crowdStep < 2 ? 2 : crowdStep * 2; console.log(`page ${(html.length / 1048576).toFixed(1)} MB: thinning the crowd to every ${crowdStep} ticks`); continue; }
    mkdirSync(dirname(out), { recursive: true });
    writeFileSync(out, html);
    console.log(`${out}: ${(html.length / 1048576).toFixed(2)} MB, ${runs.length} runs, crowd every ${crowdStep} tick(s), final hash ${runs.length - bad}/${runs.length} match the record (${record}; the car-blast scenario against its own checkpoints)`);
    break;
  }
  if (bad) process.exit(1);
}
if (import.meta.main) await main();
