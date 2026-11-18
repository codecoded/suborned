// usage: bun run art:fetch [--check] [--reseed] [--force] [--part a,b]: on a machine without the art (the Air), install each part of the art pack
// (tools/art/parts.ts) whose installed content differs from tools/art/lock.json: its manifest and archive from the GitHub release `art`, the archive's
// hash checked, every file checked against the manifest, then moved into place (the files the part's last install had and this one has not are removed).
// A part already installed at the lock's content is not downloaded again; a seed part, once installed, is kept (this machine's builders keep it) unless --reseed.
// After it: bun run batch --sky (or bun run game) builds as on the Studio.
import { copyFileSync, existsSync, mkdirSync, readFileSync, renameSync, rmSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { usageGate } from '../gen/flags';
import { streamSha } from './pack';
import { assetUrl, baseOf, fromRows, installedRows, localRelease, mb, parseRows, PARTS, pick, readLock, readStamp, REPO, LOCK, sha256, stateDir, writeStamp, type Lock, type Part } from './parts';

export type FetchOpts = { repo?: string; lockFile?: string; parts?: Part[]; check?: boolean; reseed?: boolean; force?: boolean; log?: (s: string) => void };
export type FetchResult = { installed: string[]; current: string[]; kept: string[]; needed: string[]; missing: string[] };

export async function fetchArt(o: FetchOpts = {}): Promise<FetchResult> {
  const repo = o.repo ?? REPO, lock = readLock(o.lockFile ?? LOCK), log = o.log ?? console.log, stamp = readStamp(repo);
  const r: FetchResult = { installed: [], current: [], kept: [], needed: [], missing: [] };
  for (const p of o.parts ?? PARTS) {
    const want = lock.parts[p.name];
    if (!want) { r.missing.push(p.name); log(`none   ${p.name.padEnd(18)} not in tools/art/lock.json (the Studio packs it: bun run art:pack)`); continue; }
    if (!o.force && stamp[p.name] === want.content) { r.current.push(p.name); log(`ok     ${p.name.padEnd(18)} ${want.files} files, ${mb(want.bytes)}`); continue; }
    if (!o.force && p.seed && stamp[p.name] && !o.reseed) { r.kept.push(p.name); log(`kept   ${p.name.padEnd(18)} seed installed at ${stamp[p.name]!.slice(0, 12)}; this machine's builders keep it (--reseed fetches ${want.content.slice(0, 12)})`); continue; }
    r.needed.push(p.name);
    log(`${o.check ? 'needs ' : 'fetch '} ${p.name.padEnd(18)} ${want.files} files, ${mb(want.bytes)} (download ${mb(want.archiveBytes)})`);
    if (o.check) continue;
    await install(p, lock, repo, log); r.installed.push(p.name);
  }
  return r;
}

async function install(p: Part, lock: Lock, repo: string, log: (s: string) => void) {
  const want = lock.parts[p.name]!, dl = join(stateDir(repo), 'download'), stage = join(stateDir(repo), `stage-${p.name}`), base = baseOf(p, repo);
  mkdirSync(dl, { recursive: true }); rmSync(stage, { recursive: true, force: true }); mkdirSync(stage, { recursive: true });
  const tgz = join(dl, want.asset), tsv = tgz.replace(/\.tar\.gz$/, '.tsv');
  get(lock, want.asset.replace(/\.tar\.gz$/, '.tsv'), tsv);
  const text = readFileSync(tsv, 'utf8'); if (sha256(text) !== want.content) throw new Error(`${p.name}: the manifest on the release is not the lock's (${want.content.slice(0, 12)})`);
  const m = fromRows(parseRows(text));
  if (!(existsSync(tgz) && (await streamSha(tgz)) === want.archiveSha256)) get(lock, want.asset, tgz, true);
  if ((await streamSha(tgz)) !== want.archiveSha256) throw new Error(`${p.name}: ${want.asset} does not match the lock's hash; nothing installed`);
  const x = Bun.spawnSync(['tar', '-xzf', tgz, '-C', stage], { stdout: 'pipe', stderr: 'pipe' });
  if (x.exitCode !== 0) throw new Error(`${p.name}: tar failed: ${x.stderr.toString().slice(0, 300)}`);
  const bad = m.rows.filter(row => !existsSync(join(stage, row.path)) || sha256(readFileSync(join(stage, row.path))) !== row.sha256);
  if (bad.length) throw new Error(`${p.name}: ${bad.length} files in the archive differ from its manifest (first ${bad[0]!.path}); nothing installed`);
  const before = installedRows(p.name, repo), now = new Set(m.rows.map(row => row.path));
  for (const row of m.rows) { const to = join(base, row.path); mkdirSync(dirname(to), { recursive: true }); move(join(stage, row.path), to); }
  let removed = 0; for (const row of before) if (!now.has(row.path) && existsSync(join(base, row.path))) { rmSync(join(base, row.path)); removed++; }
  writeStamp(p.name, m, repo);
  rmSync(stage, { recursive: true, force: true }); rmSync(tgz, { force: true }); rmSync(tsv, { force: true });
  log(`  in   ${m.rows.length} files${removed ? `, ${removed} removed` : ''} → ${base === repo ? '.' : base}`);
}

/** rename, or copy across volumes (the store beside the repo may be on another disk). */
function move(from: string, to: string) { try { renameSync(from, to); } catch (e) { if ((e as NodeJS.ErrnoException).code !== 'EXDEV') throw e; copyFileSync(from, to); rmSync(from); } }

/** An asset from the release (public: plain https, no login); SUBORNED_ART_RELEASE_DIR, a folder standing in for it. */
function get(lock: Lock, asset: string, to: string, progress = false) {
  const dir = localRelease();
  if (dir) { copyFileSync(join(dir, asset), to); return; }
  const r = Bun.spawnSync(['curl', '-fL', '--retry', '3', progress ? '-#' : '-sS', '-o', to, assetUrl(lock, asset)], { stdout: 'inherit', stderr: 'inherit' });
  if (r.exitCode !== 0) throw new Error(`download of ${asset} failed (curl exit ${r.exitCode}): ${assetUrl(lock, asset)}`);
}

if (import.meta.main) {
  const args = process.argv.slice(2);
  usageGate(args, { flags: ['--check', '--reseed', '--force'], valued: [{ flag: '--part', required: true }] }, `usage: bun run art:fetch [--check] [--reseed] [--force] [--part a,b]
  install the parts of the art pack this commit's tools/art/lock.json names and this machine does not have (from the GitHub release "art"); then bun run batch --sky
  --check    list what would be fetched; exit 1 if anything would; download nothing
  --reseed   fetch the seed parts again (installed once, then this machine's builders keep them)
  --force    fetch every part again, whatever is installed
  --part     only these parts (${PARTS.map(p => p.name).join(', ')})`, 'art:fetch');
  const at = args.indexOf('--part'), t0 = performance.now();
  try {
    const r = await fetchArt({ parts: pick(at >= 0 ? args[at + 1] : undefined), check: args.includes('--check'), reseed: args.includes('--reseed'), force: args.includes('--force') });
    const s = ((performance.now() - t0) / 1000).toFixed(0);
    if (args.includes('--check')) { console.log(r.needed.length ? `${r.needed.length} part(s) to fetch: bun run art:fetch (${s} s)` : `art up to date (${s} s)`); process.exit(r.needed.length || r.missing.length ? 1 : 0); }
    console.log(r.installed.length ? `installed ${r.installed.join(', ')} (${s} s); next: bun run batch --sky` : `art up to date (${s} s)`);
    if (r.missing.length) process.exit(1);
  } catch (e) { console.error(`art:fetch: ${(e as Error).message}`); process.exit(1); }
}
