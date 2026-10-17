// The game build (Aster, 2026-10-17): `bun run game` opens out/build/play/The Suborned.app, building it first when it is missing or older
// than the last commit or any input; `bun run game:build` builds whatever its age; `bun run game:check` proves the built app (the scenario
// match against headless, then the briefing, play and pause-menu shots with frame times, into out/build/play/proof/).
//
// How it builds (Unity holds a lock on the project while the Editor has it open, so a batch build cannot run beside it):
//   Editor closed: Unity in batch mode, -executeMethod Suborned.World.Play.GameBuild.Cli (log: out/build/play/build.log).
//   Editor open:   through the Editor (`unity cmd menu "Suborned/Build Game"`), only when it is stopped and not compiling; in Play it
//                  refuses and says so (a build would end the session). Either way the result is out/build/play/build.json.
// `bun run game:pack` zips the app for another Mac; `bun run game:send [device]` packs and sends it by Taildrop (default the MacBook Air,
// skys-macbook-air-1; --dry-run prints the command).
// `bun run game:signing-check` says whether the Developer ID identity and the notary profile exist, and how to make each.
// Flags: --build (force), --check, --pack, --send [device], --dry-run, --signing-check, --no-open, --windowed (open in a window).
import { existsSync, readdirSync, readFileSync, statSync } from 'node:fs';
import { join, relative } from 'node:path';
import { spawnSync } from 'node:child_process';

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

/** Newest input: the last commit, and every file under unity/Assets (scripts, the scene, StreamingAssets/sim, the kit), ProjectSettings,
 *  Packages, and the icon. */
function newestInput(): { at: number; what: string } {
  let at = 0, what = '';
  const commit = Number(run('git', ['log', '-1', '--format=%ct'], { quiet: true }).stdout.trim()) * 1000;
  if (commit > at) { at = commit; what = 'the last commit'; }
  const walk = (d: string) => {
    for (const e of readdirSync(d, { withFileTypes: true })) {
      const p = join(d, e.name);
      if (e.isDirectory()) walk(p);
      else { const m = statSync(p).mtimeMs; if (m > at) { at = m; what = relative(ROOT, p); } }
    }
  };
  for (const d of ['Assets', 'ProjectSettings', 'Packages']) if (existsSync(join(UNITY, d))) walk(join(UNITY, d));
  const icon = join(ROOT, 'tools', 'game', 'app-icon.png');
  if (existsSync(icon) && statSync(icon).mtimeMs > at) { at = statSync(icon).mtimeMs; what = 'tools/game/app-icon.png'; }
  return { at, what };
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
  const j = JSON.parse(readFileSync(STAMP, 'utf8'));
  console.log(`game: built in ${j.seconds} s, engine ${j.engine}; ${j.post}`);
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
  const p = spawnSync(bin, ['-gameProof', out, '-novsync', ...(has('--windowed') ? ['-windowed'] : []), '-logFile', join(out, 'player.log')], { encoding: 'utf8', timeout: 180000 }); // uncapped: the headroom over 60 fps
  const pj = existsSync(join(out, 'proof.json')) ? JSON.parse(readFileSync(join(out, 'proof.json'), 'utf8')) : null;
  console.log(`game: proof ${pj ? `${pj.status}, ${pj.screen} ${pj.mode}, HUD x${pj.hudScale}, ${pj.frames ? `${pj.frames.fps} fps mean (p99 ${pj.frames.p99Ms} ms)` : 'no frames'}` : 'no proof.json'}; exit ${p.status}`);
  return !!sj?.passed && s.status === 0 && pj?.status === 'ok' && p.status === 0;
}

if (has('--signing-check')) process.exit(signingCheck() ? 0 : 1);
const b = built(), inp = newestInput();
const stale = !b.ok ? b.why : inp.at > b.at ? `older than ${inp.what}` : '';
if (has('--build') || stale) {
  if (stale && !has('--build')) console.log(`game: rebuilding (${stale})`);
  if (!(await build())) process.exit(1);
} else console.log('game: the app is current');
if (has('--check')) process.exit(check() ? 0 : 1);
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

function zipOf(): string {
  const sha = run('git', ['rev-parse', '--short', 'HEAD'], { quiet: true }).stdout.trim();
  const dirty = run('git', ['status', '--porcelain'], { quiet: true }).stdout.trim().length > 0;
  return join(DIR, `TheSuborned-${sha}${dirty ? '-dirty' : ''}.zip`);
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
  if (has('--dry-run')) console.log(`game: would run: ${cmd.map(c => (c.includes(' ') ? `"${c}"` : c)).join(' ')}`);
  else {
    console.log(`game: sending to ${device} by Taildrop…`);
    const r = run(cmd[0], cmd.slice(1));
    if (r.status !== 0) { console.error(`game: tailscale file cp exited ${r.status}`); return false; }
    console.log(`game: sent ${relative(ROOT, zip)} to ${device}`);
  }
  const name = zip.split('/').pop();
  const notarised = existsSync(SIGNING) && JSON.parse(readFileSync(SIGNING, 'utf8')).notarised === true;
  console.log(`On ${device}: accept the file in the Tailscale menu if asked (it lands in Downloads), then
  cd ~/Downloads && ditto -x -k ${name} .
  ${notarised ? 'and open "The Suborned.app" (notarised: Gatekeeper lets it open)' : `open the app by right-click → Open the first time (it is signed ad hoc, not notarised), or first run
  xattr -dr com.apple.quarantine "The Suborned.app"`}`);
  return true;
}
