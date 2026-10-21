// The game build (Aster, 2026-10-17): `bun run game` opens out/build/play/The Suborned.app, building it first when it is missing or an input changed; `bun run game:build` builds whatever its age; `bun run game:check` proves the built app (the scenario
// match against headless, then the briefing, play and pause-menu shots with frame times, into out/build/play/proof/).
//
// How it builds (Unity holds a lock on the project while the Editor has it open, so a batch build cannot run beside it):
//   Editor closed: Unity in batch mode, -executeMethod Suborned.World.Play.GameBuild.Cli (log: out/build/play/build.log).
//   Editor open:   through the Editor (`unity cmd menu "Suborned/Build Game"`), only when it is stopped and not compiling; in Play it
//                  refuses and says so (a build would end the session). Either way the result is out/build/play/build.json.
// Staleness is by content (Splice, 2026-10-21): the app is rebuilt only when an input (unity/Assets, ProjectSettings, Packages, the icon, the engine
// DLL) differs from what the last build read (out/build/play/inputs.json, a fingerprint of blob ids); a commit that only records built files
// changes nothing. After a rebuild for play only the short draw check runs (`-drawCheck`: one frame, windowed, a few seconds); `--check` is the
// full proof. A running game is never built under: it says to close it. `--dry-run` says whether it would build and stops.
// `bun run game:pack` zips the app for another Mac (TheSuborned-0.2.<build>.zip); `bun run game:send [device]` packs and sends it by
// Taildrop (default the MacBook Air, skys-macbook-air-1; --dry-run prints the command) and logs it in runs/builds/builds.json.
// Build numbers: tools/game/version.json (committed) counts them; every successful build takes the next (GameBuild.Version), so the app's
// Finder Version, the pause menu, the briefing's corner and the zip all say 0.2.<build>.
// `bun run game:signing-check` says whether the Developer ID identity and the notary profile exist, and how to make each.
// Flags: --build (force), --check (pictures 1920x1080, frame times at the Air's 2880x1864, one proof run: the window switches to 1080p around each picture (CaptureSize -frames-size); --native: everything at the display's own size; --capture-size WxH: everything at that size; --no-air: frame times at 1920x1080; --reuse-proof: use the proof the build just took, if it is complete and newer than the build), --pack, --send [device], --dry-run, --signing-check, --no-open, --windowed (open in a window).
import { existsSync, readdirSync, readFileSync, statSync } from 'node:fs';
import { join, relative } from 'node:path';
import { spawnSync } from 'node:child_process';
import { createHash } from 'node:crypto';

const ROOT = join(import.meta.dir, '..', '..');
const UNITY = join(ROOT, 'unity');
const DIR = join(ROOT, 'out', 'build', 'play');
const APP = join(DIR, 'The Suborned.app');
const STAMP = join(DIR, 'build.json');
const PROFILE = 'suborned-notary';
/** Notary credentials: an App Store Connect API key named in ~/.suborned-signing/notary.json ({ key, keyId, issuer }), used when present
 *  (a keychain profile can't be written from a non-interactive session); else the keychain profile above. The .p8 never leaves that folder. */
const NOTARY_JSON = join(process.env.HOME ?? '', '.suborned-signing', 'notary.json');
function notaryAuth(): string[] {
  if (existsSync(NOTARY_JSON)) { const n = JSON.parse(readFileSync(NOTARY_JSON, 'utf8')); return ['--key', n.key, '--key-id', n.keyId, '--issuer', n.issuer]; }
  return ['--keychain-profile', PROFILE];
}
const ENTITLEMENTS = join(ROOT, 'tools', 'game', 'entitlements.plist');
const SIGNING = join(DIR, 'signing.json');
const FIX = {
  identity: 'Xcode → Settings → Accounts → (your team) → Manage Certificates → + → Developer ID Application (needs the Account Holder role), or create it at developer.apple.com → Certificates and install it; then `bun run game:signing-check`',
  profile: `make an app-specific password at appleid.apple.com, then: xcrun notarytool store-credentials ${PROFILE} --apple-id <email> --team-id <TEAM> (it asks for the password itself)`,
};
const args = process.argv.slice(2);
const has = (f: string) => args.includes(f);

const run = (cmd: string, a: string[], opts: { quiet?: boolean } = {}) => spawnSync(cmd, a, { cwd: ROOT, encoding: 'utf8', stdio: opts.quiet ? 'pipe' : 'inherit' });
const sleep = (ms: number) => new Promise(r => setTimeout(r, ms));

/** What a build reads (Splice, 2026-10-21): the Unity project's Assets, ProjectSettings and Packages, and the icon. tools/game/version.json is an
 *  output of a build (its counter), so it is not here. */
const INPUTS = ['unity/Assets', 'unity/ProjectSettings', 'unity/Packages', 'tools/game/app-icon.png'];
/** The engine DLL and its inputs are git-ignored but part of the app, so they are read by content too. */
const IGNORED_INPUTS = [join(UNITY, 'Assets', 'Suborned', 'Sim')];
const INPUTS_JSON = join(DIR, 'inputs.json');

/** A fingerprint of the inputs by content, not by time (a commit that only records files already built changes nothing): every git file under
 *  INPUTS by its blob id (the index's, or the working file's when it differs or is untracked), plus the ignored engine files by sha256. */
function inputFiles(): Map<string, string> {
  const git = (a: string[], input?: string) => spawnSync('git', a, { cwd: ROOT, encoding: 'utf8', input, maxBuffer: 1 << 28 }).stdout ?? '';
  const files = new Map<string, string>();
  for (const l of git(['ls-files', '-s', '-z', '--', ...INPUTS]).split('\0').filter(Boolean)) { const m = l.match(/^\d+ ([0-9a-f]+) \d\t(.+)$/); if (m) files.set(m[2], m[1]); }
  const dirty = [...git(['diff-files', '--name-only', '-z', '--', ...INPUTS]).split('\0'), ...git(['ls-files', '-o', '--exclude-standard', '-z', '--', ...INPUTS]).split('\0')].filter(Boolean);
  const live = dirty.filter(f => existsSync(join(ROOT, f)));
  for (const f of dirty) if (!live.includes(f)) files.set(f, 'deleted');
  if (live.length) { const ids = git(['hash-object', '--stdin-paths'], live.join('\n') + '\n').trim().split('\n'); live.forEach((f, i) => files.set(f, ids[i])); }
  const walk = (d: string) => {
    if (!existsSync(d)) return;
    for (const e of readdirSync(d, { withFileTypes: true })) {
      const p = join(d, e.name);
      if (e.isDirectory()) walk(p); else files.set(relative(ROOT, p), 'sha256:' + createHash('sha256').update(readFileSync(p)).digest('hex'));
    }
  };
  for (const d of IGNORED_INPUTS) walk(d);
  return files;
}
function fingerprint(files: Map<string, string>): string {
  return createHash('sha256').update([...files].sort(([a], [b]) => (a < b ? -1 : 1)).map(([f, h]) => `${f}\t${h}`).join('\n')).digest('hex');
}
/** Written after every successful build: the fingerprint of the inputs the app was built from (and the files, to say what changed later). */
function recordInputs(): void {
  const files = inputFiles();
  Bun.write(INPUTS_JSON, JSON.stringify({ schema: 'suborned.build-inputs/1', fingerprint: fingerprint(files), files: Object.fromEntries(files) }) + '\n');
}
/** Why the app is out of date, or '' when no input changed since it was built. */
function inputsChanged(): string {
  const files = inputFiles();
  if (existsSync(INPUTS_JSON)) {
    const rec = JSON.parse(readFileSync(INPUTS_JSON, 'utf8'));
    if (rec.fingerprint === fingerprint(files)) return '';
    const was: Record<string, string> = rec.files ?? {};
    const diff = [...new Set([...Object.keys(was), ...files.keys()])].filter(f => was[f] !== files.get(f));
    return `${diff[0] ?? 'an input'}${diff.length > 1 ? ` and ${diff.length - 1} more` : ''} changed since the build`;
  }
  // an app built before the record: what differs from the commit it was built on, and is newer than the build
  const sha = existsSync(STAMP) ? JSON.parse(readFileSync(STAMP, 'utf8')).sha : '';
  if (!sha) return 'no record of what the app was built from';
  const git = (a: string[]) => run('git', a, { quiet: true }).stdout.split('\0').filter(Boolean);
  const moved = [...git(['diff', '--name-only', '-z', sha, '--', ...INPUTS]), ...git(['ls-files', '-o', '--exclude-standard', '-z', '--', ...INPUTS])];
  const at = statSync(STAMP).mtimeMs;
  const newer = moved.find(f => !existsSync(join(ROOT, f)) || statSync(join(ROOT, f)).mtimeMs > at);
  if (newer) return `${newer} changed since the build`;
  recordInputs(); // current: keep the record from here on
  return '';
}

function built(): { ok: boolean; at: number; why: string } {
  if (!existsSync(APP)) return { ok: false, at: 0, why: 'no app yet' };
  if (!existsSync(STAMP)) return { ok: false, at: 0, why: 'no build.json' };
  const j = JSON.parse(readFileSync(STAMP, 'utf8'));
  if (j.result !== 'Succeeded') return { ok: false, at: 0, why: `last build ${j.result}${j.error ? `: ${j.error}` : ''}` };
  return { ok: true, at: statSync(STAMP).mtimeMs, why: '' };
}

/** The Editor with this project open, if any: its status through the Unity CLI. */
function editorOpen(): boolean {
  const ps = run('pgrep', ['-fil', 'Unity.app/Contents/MacOS/Unity'], { quiet: true }).stdout;
  return ps.split('\n').some(l => l.toLowerCase().includes(UNITY.toLowerCase()) && !l.includes('-batchmode'));
}
function editorStatus(): { playMode?: string; compiling?: boolean } | null {
  const r = run('unity', ['cmd', 'editor_status'], { quiet: true });
  const line = (r.stdout ?? '').split('\n').find(l => l.includes('"playMode"'));
  if (!line) return null;
  try { return JSON.parse(line.slice(line.indexOf('{'))); } catch { return null; }
}

async function build(): Promise<boolean> {
  const before = existsSync(STAMP) ? statSync(STAMP).mtimeMs : 0;
  if (editorOpen()) {
    const st = editorStatus();
    if (!st) { console.error('game: Unity has the project open but does not answer the CLI. Close Unity, or build from its menu: Suborned/Build Game.'); return false; }
    if (st.playMode && st.playMode !== 'stopped') { console.error(`game: Unity is in Play (${st.playMode}). Stop Play, or close Unity, and run again; a build would end the session.`); return false; }
    for (let i = 0; i < 120 && editorStatus()?.compiling; i++) await sleep(2000);
    console.log('game: building through the open Editor (Suborned/Build Game)…');
    run('unity', ['cmd', 'menu', '--path', 'Suborned/Build Game'], { quiet: true });
    for (let i = 0; i < 600; i++) { // up to 20 min
      await sleep(2000);
      if (existsSync(STAMP) && statSync(STAMP).mtimeMs > before) break;
    }
  } else {
    const ver = readFileSync(join(UNITY, 'ProjectSettings', 'ProjectVersion.txt'), 'utf8').match(/m_EditorVersion: (\S+)/)?.[1];
    const exe = `/Applications/Unity/Hub/Editor/${ver}/Unity.app/Contents/MacOS/Unity`;
    if (!existsSync(exe)) { console.error(`game: Unity ${ver} is not installed at ${exe}`); return false; }
    console.log(`game: building in batch mode (Unity ${ver}; log out/build/play/build.log)…`);
    run(exe, ['-batchmode', '-quit', '-projectPath', UNITY, '-executeMethod', 'Suborned.World.Play.GameBuild.Cli', '-logFile', join(DIR, 'build.log')]);
  }
  const b = built();
  if (!b.ok || b.at <= before) { console.error(`game: the build did not finish (${b.why || 'no new build.json'}); see out/build/play/build.json and build.log or the Editor console`); return false; }
  recordInputs();
  const j = JSON.parse(readFileSync(STAMP, 'utf8'));
  console.log(`game: built ${j.version ? `Build ${j.version} (${j.recording}, sim ${j.sim}, ${j.sha}${j.dirty ? '+' : ''}) ` : ''}in ${j.seconds} s, engine ${j.engine}; ${j.post}`);
  return true;
}

/** The proof: -runScenarios (headless, the Editor's SimCheck), then -gameProof (the briefing, play, the menu; frame times; Quit). */
function check(): boolean {
  const exe = join(APP, 'Contents', 'MacOS', 'The Suborned');
  const bin = existsSync(exe) ? exe : join(APP, 'Contents', 'MacOS', readdirSync(join(APP, 'Contents', 'MacOS'))[0]);
  const out = join(DIR, 'proof');
  const scen = join(out, 'scenarios.json');
  run('mkdir', ['-p', out]);
  const s = spawnSync(bin, ['-batchmode', '-nographics', '-runScenarios', scen, '-logFile', join(out, 'scenarios.log')], { encoding: 'utf8', timeout: 600000 });
  const sj = existsSync(scen) ? JSON.parse(readFileSync(scen, 'utf8')) : null;
  console.log(`game: scenarios ${sj ? `${sj.matching}/${sj.scenarios} match headless, passed ${sj.passed} (${sj.runtime}, DLL ${String(sj.dllSha256).slice(0, 12)})` : 'no report'}; exit ${s.status}`);
  const reuse = has('--reuse-proof') ? reusableProof() : null; // `bun run batch`: the build just took this proof (buildDrawn); it is not taken a second time
  if (reuse) console.log(`game: proof reused, taken right after the build (${reuse.screen} ${reuse.mode}, ${reuse.frames?.fps} fps mean, p99 ${reuse.frames?.p99Ms} ms)`);
  const first = reuse ? { pj: reuse, status: 0 } : proof();
  const pj = airFrames(first.pj), status = first.status; // the pictures are 1920x1080; the frame times are the Air's size
  const drive = driveReplay(pj) && driveReplay(pj, 'fireSession', 'fire-session.script.json'); // brief 024: the trigger session (press, hold, release, tap, a lock) replays too
  const inputs = inputChecks(pj);
  return !!sj?.passed && s.status === 0 && pj?.status === 'ok' && status === 0 && drive && inputs;
}

/** Latch 2026-10-21 (ProofInput.cs): the input checks the proof ran through the Input System's devices. ok false fails the check; ok null
 *  (the fight gave no hostile in range) is reported, not failed. */
function inputChecks(pj: any): boolean {
  let pass = true;
  for (const k of ['driveClick', 'fireStop', 'ringCheck', 'inputMap', 'act', 'fire', 'cars', 'hudClick', 'dblClick', 'leash']) {
    const r = pj?.frames?.[k];
    if (r === undefined) { console.log(`game: ${k}: not in this proof (an older build)`); continue; }
    const { ok, ...rest } = r ?? {};
    console.log(`game: ${k} ${ok === true ? 'ok' : ok === false ? 'FAILED' : 'not reached'}: ${JSON.stringify(rest).slice(0, 600)}`);
    if (ok === false || r === null) pass = false;
  }
  return pass;
}

/** Brief 025: the driven session the proof saved in the built player (take control of the Scout, E on Inez, let go) replays headless on
 *  every checkpoint (the repo's engine CLI, `sim play`); a copy goes beside the proof. */
function driveReplay(pj: any, key = 'driveSession', name = 'drive-session.script.json'): boolean {
  const f = pj?.frames?.[key] as string | undefined;
  if (!f || !existsSync(f)) { console.error(`game: no ${key} from the proof (${f ?? 'none'})`); return false; }
  const out = join(DIR, 'proof', name); run('cp', [f, out], { quiet: true });
  const env = { ...process.env, DOTNET_CLI_TELEMETRY_OPTOUT: '1', DOTNET_NOLOGO: '1', ...(process.env.DOTNET_ROOT || !existsSync('/opt/homebrew/opt/dotnet/libexec') ? {} : { DOTNET_ROOT: '/opt/homebrew/opt/dotnet/libexec' }) };
  const r = spawnSync('dotnet', ['run', '--project', 'sim/cli', '--', 'play', out], { cwd: ROOT, encoding: 'utf8', env, timeout: 300000 });
  const ok = r.status === 0 && /\[ok\] the replay matches its record/.test(r.stdout);
  const cps = r.stdout.match(/checkpoints (\d+)/)?.[1] ?? '?';
  const d = pj.frames.drive?.[0];
  console.log(`game: ${key} ${ok ? 'replays headless on every checkpoint' : 'DOES NOT replay'} (${cps} checkpoints; ${out})${d ? `; street level driven ${d.fps} fps (p99 ${d.p99Ms} ms${d.gpuMs >= 0 ? `, gpu ${d.gpuMs} ms` : ''})` : ''}`);
  if (!ok) console.error(r.stdout.slice(-2000), r.stderr.slice(-1000));
  return ok;
}

/** -gameProof: the briefing, play, the menu, frame times; `blank` when the frame was one flat colour (the build's URP failed to start). */
function proof(sub = 'proof', extra: string[] = []): { pj: any; status: number | null } {
  const exe = join(APP, 'Contents', 'MacOS', 'The Suborned');
  const bin = existsSync(exe) ? exe : join(APP, 'Contents', 'MacOS', readdirSync(join(APP, 'Contents', 'MacOS'))[0]);
  const out = join(DIR, sub);
  run('mkdir', ['-p', out]);
  if (existsSync(join(out, 'proof.json'))) run('rm', ['-f', join(out, 'proof.json')]);
  const p = spawnSync(bin, ['-gameProof', out, '-novsync', ...(has('--windowed') ? ['-windowed'] : []), ...(has('--native') ? ['-capture-native'] : []), ...(has('--capture-size') ? ['-capture-size', args[args.indexOf('--capture-size') + 1]] : []), ...(onePass() && !extra.includes('-capture-size') ? ['-frames-size', AIR] : []), ...extra, '-logFile', join(out, 'player.log')], { encoding: 'utf8', timeout: 900000 }); // brief 024's act, fire and car checks add about 3 min; uncapped: the headroom over 60 fps; 420 s: the follow spots, the people cost, brief 022's café call and brief 025's drive run about 4 min (180 s cut it off, 2026-10-18)
  const pj = existsSync(join(out, 'proof.json')) ? JSON.parse(readFileSync(join(out, 'proof.json'), 'utf8')) : null;
  console.log(`game: proof ${pj ? `${pj.status}, ${pj.screen} ${pj.mode}, HUD x${pj.hudScale}, ${pj.frames ? `${pj.frames.fps} fps mean (p99 ${pj.frames.p99Ms} ms)${pj.frames.byYaw ? `; by yaw ${pj.frames.byYaw.map((y: { yaw: number; fps: number; p99Ms: number }) => `${y.yaw}° ${y.fps} fps (p99 ${y.p99Ms} ms)`).join(', ')}` : ''}${pj.frames.follow?.length ? `; spots ${pj.frames.follow.map((f: { spot: string; fps: number; p99Ms: number; gpuMs?: number }) => `${f.spot} ${f.fps} fps (p99 ${f.p99Ms} ms${f.gpuMs !== undefined && f.gpuMs >= 0 ? `, gpu ${f.gpuMs} ms` : ''})`).join(', ')}` : ''}${pj.frames.people?.length ? `; people ${pj.frames.people.map((f: { spot: string; fps: number; p99Ms: number; gpuMs?: number }) => `${f.spot} ${f.fps} fps (p99 ${f.p99Ms} ms${f.gpuMs !== undefined && f.gpuMs >= 0 ? `, gpu ${f.gpuMs} ms` : ''})`).join(', ')}` : ''}` : 'no frames'}` : 'no proof.json'}; exit ${p.status}`);
  return { pj, status: p.status };
}

/** The 15-inch MacBook Air M4's screen (Mac16,13), the machine the 60 fps limit is set on (GDD §01, §11). */
const AIR = '2880x1864';
/** Frame times at the Air's resolution (Sky/coordinator, 2026-10-21: the pictures are 1920x1080, the limit is 60 fps on the Air). The proof's pictures are taken at 1920x1080
 *  (CaptureSize.cs), whose frame times flatter the machine; so the same proof runs once more at 2880x1864 into proof-air/ (its pictures are not kept as evidence) and its
 *  frames replace the proof's in proof.json, labelled: `framesAt`, and `framesNote` saying whether this machine is the Air or an Air-sized proxy. Skipped with --native or
 *  --capture-size (one run at that size), --no-air, or when proof.json already carries Air-sized frames. */
function airFrames(pj: any): any {
  if (pj?.status === 'ok' && pj.frames && pj.framesAt && !pj.framesNote) return noteFrames(pj); // one pass: the proof took them at the Air's size itself
  if (!pj || pj.status !== 'ok' || !pj.frames || pj.framesAt || has('--native') || has('--capture-size') || has('--no-air')) return pj;
  console.log(`game: frame times again at ${AIR} (the Air's screen); the pictures stay ${pj.screen}`);
  const a = proof('proof-air', ['-capture-size', AIR]);
  if (a.pj?.status !== 'ok' || !a.pj.frames) { console.error(`game: the ${AIR} run gave no frame times (${a.pj?.status ?? 'no proof.json'}); the proof's frames stay at ${pj.screen}`); return pj; }
  const model = spawnSync('sysctl', ['-n', 'hw.model'], { encoding: 'utf8' }).stdout.trim() || '?';
  const framesNote = model === 'Mac16,13' ? `at ${a.pj.screen} on the reference MacBook Air` : `at ${a.pj.screen}: an Air-sized proxy on ${model}, not the Air's own GPU`;
  const merged = { ...pj, picturesScreen: pj.screen, picturesFrames: pj.frames, frames: a.pj.frames, framesAt: a.pj.screen, framesNote, framesModel: model };
  Bun.write(join(DIR, 'proof', 'proof.json'), JSON.stringify(merged, null, 1) + '\n');
  console.log(`game: frame times ${framesNote}: ${a.pj.frames.fps} fps mean (p99 ${a.pj.frames.p99Ms} ms)`);
  return merged;
}

/** One pass (Latch, 2026-10-21; CaptureSize.cs `-frames-size`): the proof runs once with its window at the Air's 2880x1864 for the frame times and
 *  switches to 1920x1080 around each picture (HudToolkit.ShootCo), so the second run (proof-air/) is not needed. Off with --native,
 *  --capture-size or --no-air, which keep one size for both. */
function onePass(): boolean { return !has('--native') && !has('--capture-size') && !has('--no-air'); }

/** The one-pass proof's frames labelled as the two-run merge labels them (`framesNote`, `framesModel`); written back to proof.json. */
function noteFrames(pj: any): any {
  const model = spawnSync('sysctl', ['-n', 'hw.model'], { encoding: 'utf8' }).stdout.trim() || '?';
  const framesNote = model === 'Mac16,13' ? `at ${pj.framesAt} on the reference MacBook Air` : `at ${pj.framesAt}: an Air-sized proxy on ${model}, not the Air's own GPU`;
  const merged = { ...pj, framesNote, framesModel: model, framesPass: 'one' };
  Bun.write(join(DIR, 'proof', 'proof.json'), JSON.stringify(merged, null, 1) + '\n');
  console.log(`game: frame times ${framesNote} (one pass; pictures ${pj.screen}): ${pj.frames.fps} fps mean (p99 ${pj.frames.p99Ms} ms)`);
  return merged;
}

/** The proof the last build took (buildDrawn), when it is complete and newer than the build; else null and the check takes its own. */
function reusableProof(): any | null {
  const f = join(DIR, 'proof', 'proof.json');
  if (!existsSync(f) || !existsSync(STAMP) || statSync(f).mtimeMs < statSync(STAMP).mtimeMs) return null;
  const j = JSON.parse(readFileSync(f, 'utf8'));
  return j.status === 'ok' && j.frames ? j : null;
}

function proofBlank(): boolean { const f = join(DIR, 'proof', 'proof.json'); return existsSync(f) && JSON.parse(readFileSync(f, 'utf8')).status === 'blank'; }

/** A blank build is never shipped: its number goes back to tools/game/version.json, so the rebuild takes the same one. */
function giveBackNumber(): void {
  const f = join(ROOT, 'tools', 'game', 'version.json'); if (!existsSync(f)) return;
  const v = JSON.parse(readFileSync(f, 'utf8')); if (typeof v.build === 'number' && v.build > 0) { v.build -= 1; Bun.write(f, JSON.stringify(v, null, 4) + '\n'); }
}

/** The cheap proof that a fresh build draws (Splice, 2026-10-21; GameBoot.DrawCheck, `-drawCheck`; plain `bun run game` only): the player in a window for a few seconds,
 *  one frame tested for being a picture, no shot tour, no frame times. `bun run game:check` still takes the full proof. */
function drawCheck(): { status: string; seconds: number } {
  const exe = join(APP, 'Contents', 'MacOS', 'The Suborned');
  const bin = existsSync(exe) ? exe : join(APP, 'Contents', 'MacOS', readdirSync(join(APP, 'Contents', 'MacOS'))[0]);
  const out = join(DIR, 'drawcheck');
  run('mkdir', ['-p', out]);
  run('rm', ['-f', join(out, 'proof.json')]);
  const t0 = Date.now();
  spawnSync(bin, ['-drawCheck', out, '-windowed', '-logFile', join(out, 'player.log')], { encoding: 'utf8', timeout: 90000 });
  const f = join(out, 'proof.json');
  return { status: existsSync(f) ? JSON.parse(readFileSync(f, 'utf8')).status : 'no result', seconds: Math.round((Date.now() - t0) / 100) / 10 };
}

/** The app is open in a Finder session: a build would replace the bundle under it. */
function appRunning(): boolean { return run('pgrep', ['-f', join(APP, 'Contents', 'MacOS')], { quiet: true }).status === 0; }

/** The full proof after a build (the tour, the frame times); a blank frame gets one more build under the same number, then the proof again. */
async function fullDrawn(): Promise<boolean> {
  const first = proof();
  if (first.pj?.status !== 'blank') return true;
  console.log('game: the build drew a blank frame (the second-build black screen); building once more, under the same number…');
  giveBackNumber();
  if (!(await build())) return false;
  const again = proof();
  if (again.pj?.status === 'blank') { console.error('game: the rebuild drew a blank frame too; see out/build/play/proof/player.log'); return false; }
  return true;
}

/** A fresh build is checked to draw before it is trusted: a second player build in one Editor domain can ship a player whose URP pipeline
 *  fails to start (a black screen; GameBuild.cs). A blank frame gets one more build, then the check again. The check is the short one
 *  (drawCheck); `--build`, `--check`, `--pack` and `--send` keep the full proof. */
async function buildDrawn(why: string, quick: boolean): Promise<boolean> {
  if (appRunning()) { console.error('game: the game is open and out of date (' + why + '); close it and run again. A build would replace the app under it.'); return false; }
  const next = (JSON.parse(existsSync(join(ROOT, 'tools', 'game', 'version.json')) ? readFileSync(join(ROOT, 'tools', 'game', 'version.json'), 'utf8') : '{}').build ?? 0) + 1;
  console.log(`game: building 0.2.${next} (${why}), about a minute, then the game opens`);
  if (!(await build())) return false;
  if (!quick) return fullDrawn(); // `--build`, `--check`, `--pack`: the full proof right after the build (bun run batch reuses it)
  const first = drawCheck();
  console.log(`game: draw check ${first.status} in ${first.seconds} s`);
  if (first.status !== 'blank') return first.status === 'ok' || (console.error(`game: the draw check gave ${first.status}; see out/build/play/drawcheck/player.log`), false);
  console.log('game: the build drew a blank frame (the second-build black screen); building once more, under the same number…');
  giveBackNumber();
  if (!(await build())) return false;
  const again = drawCheck();
  console.log(`game: draw check ${again.status} in ${again.seconds} s`);
  if (again.status !== 'ok') { console.error('game: the rebuild did not draw either; see out/build/play/drawcheck/player.log'); return false; }
  return true;
}

if (has('--signing-check')) process.exit(signingCheck() ? 0 : 1);
const b = built();
const stale = !b.ok ? b.why : inputsChanged();
if (has('--dry-run') && !has('--send') && (stale || has('--build'))) { console.log(`game: would build (${has('--build') ? 'forced' : stale})`); process.exit(0); }
if (has('--build') || stale) {
  if (!(await buildDrawn(has('--build') ? 'forced' : stale, !has('--build') && !has('--check') && !has('--pack') && !has('--send')))) process.exit(1);
} else console.log('game: the app is current');
if (has('--check')) {
  if (check()) process.exit(0);
  if (proofBlank()) { // a blank frame from an app built earlier: build once more and prove again
    console.log('game: the app drew a blank frame (the second-build black screen); building once more, under the same number…');
    giveBackNumber();
    if (!(await build())) process.exit(1);
    process.exit(check() ? 0 : 1);
  }
  process.exit(1);
}
if (has('--pack') || has('--send')) {
  const zip = pack();
  if (!zip) process.exit(1);
  if (has('--send')) process.exit(send(zip) ? 0 : 1);
  process.exit(0);
}
if (!has('--no-open') && !has('--build')) run('open', has('--windowed') ? ['-n', APP, '--args', '-windowed'] : ['-n', APP]);

// ---------------------------------------------------------------- signing and notarisation (Gatekeeper on other Macs)

/** A "Developer ID Application" identity in the keychain (SUBORNED_SIGN_ID overrides: a name or a SHA-1). */
/** A dedicated signing keychain (~/Library/Keychains/suborned-signing.keychain-db, its password in ~/.suborned-signing/keychain.pw) holds the
 *  Developer ID key with codesign allowed, so signing never waits on a keychain prompt nobody is there to answer; unlocked before use. */
function signKeychain(): string[] {
  const SIGN_KC = join(process.env.HOME ?? '', 'Library', 'Keychains', 'suborned-signing.keychain-db');
  const SIGN_PW = join(process.env.HOME ?? '', '.suborned-signing', 'keychain.pw');
  if (!existsSync(SIGN_KC) || !existsSync(SIGN_PW)) return [];
  run('security', ['unlock-keychain', '-p', readFileSync(SIGN_PW, 'utf8').trim(), SIGN_KC], { quiet: true });
  return ['--keychain', SIGN_KC];
}
function identity(): string | null {
  if (process.env.SUBORNED_SIGN_ID) return process.env.SUBORNED_SIGN_ID;
  const out = run('security', ['find-identity', '-v', '-p', 'codesigning'], { quiet: true }).stdout ?? '';
  const m = out.split('\n').map(l => l.match(/\)\s+([0-9A-F]{40})\s+"(Developer ID Application:[^"]+)"/)).find(Boolean);
  return m ? m[2] : null;
}

/** The notarytool keychain profile exists (notarytool reads it and asks Apple for the history; no profile is an immediate error). */
function profileOk(): boolean {
  const r = spawnSync('xcrun', ['notarytool', 'history', ...notaryAuth()], { encoding: 'utf8', timeout: 60000 });
  return r.status === 0;
}

function signingCheck(): boolean {
  const id = identity(), prof = profileOk();
  console.log(`game: signing identity ${id ? `found: ${id}` : 'missing (no "Developer ID Application" in the keychain)'}`);
  if (!id) console.log(`  fix: ${FIX.identity}`);
  console.log(`game: notary profile "${PROFILE}" ${prof ? 'found' : 'missing'}`);
  if (!prof) console.log(`  fix: ${FIX.profile}`);
  console.log(id && prof ? 'game: ready; `bun run game:pack` signs, notarises and staples' : 'game: until both exist, `bun run game:pack` makes the ad hoc build (right-click → Open on other Macs)');
  return !!(id && prof);
}

/** The built app's stamp (build.json): its version 0.2.<build> (tools/game/version.json, GameBuild.Version), recording, engine and commit. */
interface Stamp { version?: string; build?: number; recording?: string; sim?: string; sha?: string; dirty?: boolean; engine?: string; dllSha256?: string; at?: string }
function stamp(): Stamp { return existsSync(STAMP) ? JSON.parse(readFileSync(STAMP, 'utf8')) : {}; }

/** TheSuborned-0.2.<build>.zip (Sky 2026-10-19: builds told apart by number); an app built before the counter keeps the commit name. */
function zipOf(): string {
  const st = stamp();
  if (st.version) return join(DIR, `TheSuborned-${st.version}.zip`);
  const sha = run('git', ['rev-parse', '--short', 'HEAD'], { quiet: true }).stdout.trim();
  const dirty = run('git', ['status', '--porcelain'], { quiet: true }).stdout.trim().length > 0;
  return join(DIR, `TheSuborned-${sha}${dirty ? '-dirty' : ''}.zip`);
}

/** The build log (committed): runs/builds/builds.json, one entry per build sent, newest last: number, version, date, commit (and dirty),
 *  engine, recording, the zip, where it went and a one-line "what's new" for Aster to fill. A build sent again adds the device and date. */
function buildsPath() { return join(ROOT, 'runs', 'builds', 'builds.json'); }
function logSent(zip: string, device: string): string {
  const st = stamp();
  const log: { schema: string; note: string; builds: any[] } = existsSync(buildsPath()) ? JSON.parse(readFileSync(buildsPath(), 'utf8'))
    : { schema: 'suborned.builds/1', note: 'One entry per game build sent (tools/game/cli.ts --send). whatsNew is one line, filled by Aster.', builds: [] };
  const at = new Date().toISOString();
  const same = st.build !== undefined ? log.builds.find(b => b.build === st.build) : undefined;
  if (same) { same.sent = [...(same.sent ?? []), { device, at }]; }
  else log.builds.push({
    build: st.build ?? null, version: st.version ?? null, date: at, builtAt: st.at ?? null, sha: st.sha ?? null, dirty: st.dirty ?? null,
    engine: st.sim ?? null, engineSource: st.engine ?? null, dllSha256: st.dllSha256 ?? null, recording: st.recording ?? null,
    zip: zip.split('/').pop(), sent: [{ device, at }], whatsNew: '',
  });
  run('mkdir', ['-p', join(ROOT, 'runs', 'builds')]);
  Bun.write(buildsPath(), JSON.stringify(log, null, 1) + '\n');
  return relative(ROOT, buildsPath());
}
function zipApp(zip: string): boolean {
  if (existsSync(zip)) run('rm', ['-f', zip]);
  const r = run('ditto', ['-c', '-k', '--keepParent', APP, zip]);
  if (r.status !== 0 || !existsSync(zip)) { console.error('game: ditto could not zip the app'); return false; }
  return true;
}

/** The app for another Mac: signed with the Developer ID under the hardened runtime and notarised and stapled when the identity and the
 *  notary profile exist, else the ad hoc build as it is (and what is missing, with its fix); then zipped with ditto (it keeps the bundle's
 *  links, modes and signature) to out/build/play/TheSuborned-<sha>[-dirty].zip, -dirty when the tree has changes the commit does not. */
function pack(): string | null {
  const zip = zipOf(), id = identity(), prof = id ? profileOk() : false;
  let state = 'ad hoc';
  if (id) {
    console.log(`game: signing with ${id} (hardened runtime, timestamp, tools/game/entitlements.plist)…`);
    const r = run('codesign', ['--force', '--deep', '--options', 'runtime', '--timestamp', ...(id === '-' ? [] : signKeychain()), '--entitlements', ENTITLEMENTS, '--sign', id, APP]);
    if (r.status !== 0) { console.error('game: codesign failed (see above)'); return null; }
    state = id === '-' ? 'ad hoc, hardened runtime' : 'Developer ID';
  } else console.log(`game: no Developer ID identity: the ad hoc build (right-click → Open on other Macs). Fix: ${FIX.identity}`);
  if (!zipApp(zip)) return null;
  if (id && !prof) console.log(`game: no notary profile "${PROFILE}": signed but not notarised. Fix: ${FIX.profile}`);
  if (id && prof) {
    console.log('game: notarising (xcrun notarytool submit --wait; a few minutes)…');
    const n = spawnSync('xcrun', ['notarytool', 'submit', zip, ...notaryAuth(), '--wait'], { encoding: 'utf8', timeout: 3600000 });
    process.stdout.write(n.stdout ?? ''); process.stderr.write(n.stderr ?? '');
    const sub = (n.stdout ?? '').match(/id: ([0-9a-f-]{36})/)?.[1];
    if (n.status !== 0 || !/status: Accepted/.test(n.stdout ?? '')) { console.error(`game: notarisation not accepted${sub ? `; the log: xcrun notarytool log ${sub} ${notaryAuth().join(' ')}` : ''}`); return null; }
    if (run('xcrun', ['stapler', 'staple', APP]).status !== 0) { console.error('game: stapler failed'); return null; }
    if (!zipApp(zip)) return null;
    state = 'Developer ID, notarised, stapled';
  }
  const v = run('codesign', ['--verify', '--deep', '--strict', APP], { quiet: true });
  const g = spawnSync('spctl', ['-a', '-vvv', '-t', 'exec', APP], { encoding: 'utf8' });
  const gk = `${g.stderr ?? ''}${g.stdout ?? ''}`.trim().split('\n').slice(0, 2).join('; ');
  console.log(`game: codesign --verify ${v.status === 0 ? 'ok' : `FAILED ${v.stderr}`}; Gatekeeper: ${gk}`);
  Bun.write(SIGNING, JSON.stringify({ state, identity: id, notarised: state.includes('notarised'), zip: relative(ROOT, zip), at: new Date().toISOString() }, null, 1) + '\n');
  console.log(`game: ${relative(ROOT, zip)} (${(statSync(zip).size / 1048576).toFixed(0)} MB, ${state})`);
  return v.status === 0 ? zip : null;
}

/** Taildrop to another Mac on the tailnet (no SSH needed): the file lands in its Downloads through the Tailscale app. */
function send(zip: string): boolean {
  const i = args.indexOf('--send'); const next = args[i + 1];
  const device = next && !next.startsWith('--') ? next : 'skys-macbook-air-1';
  const ts = existsSync('/Applications/Tailscale.app/Contents/MacOS/Tailscale') ? '/Applications/Tailscale.app/Contents/MacOS/Tailscale' : 'tailscale';
  const cmd = [ts, 'file', 'cp', zip, `${device}:`];
  if (has('--dry-run')) console.log(`game: would run: ${cmd.map(c => (c.includes(' ') ? `"${c}"` : c)).join(' ')}; would log build ${stamp().version ?? '(no version: built before the counter)'} in runs/builds/builds.json`);
  else {
    console.log(`game: sending to ${device} by Taildrop…`);
    const r = run(cmd[0], cmd.slice(1));
    if (r.status !== 0) { console.error(`game: tailscale file cp exited ${r.status}`); return false; }
    console.log(`game: sent ${relative(ROOT, zip)} to ${device}; logged in ${logSent(zip, device)}`);
  }
  const name = zip.split('/').pop();
  const notarised = existsSync(SIGNING) && JSON.parse(readFileSync(SIGNING, 'utf8')).notarised === true;
  console.log(`On ${device}: accept the file in the Tailscale menu if asked (it lands in Downloads), then
  cd ~/Downloads && ditto -x -k ${name} .
  ${notarised ? 'and open "The Suborned.app" (notarised: Gatekeeper lets it open)' : `open the app by right-click → Open the first time (it is signed ad hoc, not notarised), or first run
  xattr -dr com.apple.quarantine "The Suborned.app"`}`);
  return true;
}
