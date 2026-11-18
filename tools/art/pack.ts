// usage: bun run art:pack [--check] [--reseed] [--part a,b]: on the machine with the art (the Studio), hash each part of the art pack (tools/art/parts.ts)
// and upload a part whose content moved to the GitHub release `art`: <part>-<content>.tar.gz and its manifest <part>-<content>.tsv; then write
// tools/art/lock.json (commit it: a pull tells the other machines what to fetch). A part already on the release at that content is not uploaded again.
// --check: say what would be packed and exit 1 if anything would, uploading nothing. A seed part (tools/art/parts.ts) is packed once; --reseed packs it again.
import { copyFileSync, existsSync, mkdirSync, rmSync, statSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { usageGate } from '../gen/flags';
import { assetName, baseOf, localRelease, manifest, mb, PARTS, pick, readLock, REPO, LOCK, stateDir, trackedFiles, writeLock, writeStamp, type Lock, type Part } from './parts';

const LIMIT = 2 * 1024 ** 3 - 1; // a GitHub release asset is under 2 GiB

export type PackOpts = { repo?: string; lockFile?: string; parts?: Part[]; check?: boolean; reseed?: boolean; log?: (s: string) => void };
export type PackResult = { packed: string[]; unchanged: string[]; seeds: string[]; stale: string[] };

/** Hash every part; pack and upload the ones whose content moved (or, with `check`, only list them). */
export async function pack(o: PackOpts = {}): Promise<PackResult> {
  const repo = o.repo ?? REPO, lockFile = o.lockFile ?? LOCK, log = o.log ?? console.log, lock = readLock(lockFile), tracked = trackedFiles(repo);
  const r: PackResult = { packed: [], unchanged: [], seeds: [], stale: [] };
  for (const p of o.parts ?? PARTS) {
    const was = lock.parts[p.name];
    if (p.seed && was && !o.reseed) { r.seeds.push(p.name); log(`seed   ${p.name.padEnd(18)} packed at ${was.content.slice(0, 12)} (${mb(was.bytes)}); each machine's builders keep it (--reseed packs it again)`); continue; }
    const m = manifest(p, repo, tracked);
    if (!m.rows.length) throw new Error(`${p.name}: no files under ${p.include[0]} on this machine; pack on the machine that has the art (fetch here: bun run art:fetch)`);
    if (was?.content === m.content) { r.unchanged.push(p.name); writeStamp(p.name, m, repo); log(`same   ${p.name.padEnd(18)} ${m.rows.length} files, ${mb(m.bytes)}`); continue; }
    r.stale.push(p.name);
    log(`${o.check ? 'stale ' : 'pack  '} ${p.name.padEnd(18)} ${m.rows.length} files, ${mb(m.bytes)}${was ? ` (was ${was.content.slice(0, 12)})` : ' (new)'}`);
    if (o.check) continue;
    const dir = join(stateDir(repo), 'build'); mkdirSync(dir, { recursive: true });
    const asset = assetName(p.name, m.content), tgz = join(dir, asset), tsv = tgz.replace(/\.tar\.gz$/, '.tsv'), list = join(dir, `${p.name}.list`);
    writeFileSync(tsv, m.text); writeFileSync(list, m.rows.map(x => x.path).join('\0') + '\0');
    run(['tar', '-czf', tgz, '-C', baseOf(p, repo), '--null', '-T', list], `tar ${p.name}`);
    const archiveBytes = statSync(tgz).size, archiveSha256 = await streamSha(tgz);
    if (archiveBytes > LIMIT) throw new Error(`${p.name}: ${mb(archiveBytes)} is over GitHub's 2 GiB for one release file; split the part in tools/art/parts.ts`);
    upload(lock, [tgz, tsv], log);
    lock.parts[p.name] = { content: m.content, files: m.rows.length, bytes: m.bytes, asset, archiveSha256, archiveBytes, ...(p.seed ? { seed: true } : {}) };
    writeLock(lock, lockFile); writeStamp(p.name, m, repo); r.packed.push(p.name);
    rmSync(tgz, { force: true }); rmSync(tsv, { force: true }); rmSync(list, { force: true });
    log(`  up   ${asset} (${mb(archiveBytes)})`);
  }
  return r;
}

export async function streamSha(file: string): Promise<string> {
  const h = new Bun.CryptoHasher('sha256');
  for await (const chunk of Bun.file(file).stream()) h.update(chunk);
  return h.digest('hex');
}

function run(cmd: string[], what: string, inherit = false) {
  const r = Bun.spawnSync(cmd, { stdout: inherit ? 'inherit' : 'pipe', stderr: inherit ? 'inherit' : 'pipe' });
  if (r.exitCode !== 0) throw new Error(`${what} failed (exit ${r.exitCode}): ${r.stderr?.toString().trim().slice(0, 400) ?? ''}`);
  return r.stdout?.toString() ?? '';
}

/** Onto the release (made the first time, as a pre-release); SUBORNED_ART_RELEASE_DIR: copied into that folder instead. */
function upload(lock: Lock, files: string[], log: (s: string) => void) {
  const dir = localRelease();
  if (dir) { mkdirSync(dir, { recursive: true }); for (const f of files) copyFileSync(f, join(dir, f.split('/').pop()!)); return; }
  const { slug, tag } = lock.release;
  if (Bun.spawnSync(['gh', 'release', 'view', tag, '-R', slug], { stdout: 'pipe', stderr: 'pipe' }).exitCode !== 0) {
    log(`  making the release ${tag} on ${slug}`);
    run(['gh', 'release', 'create', tag, '-R', slug, '--prerelease', '--title', 'Art pack (build inputs)', '--notes',
      'The art the game build reads that git does not hold (tools/art/parts.ts). tools/art/lock.json names the files each commit needs; `bun run art:fetch` installs them.'], 'gh release create');
  }
  run(['gh', 'release', 'upload', tag, '-R', slug, '--clobber', ...files], 'gh release upload', true);
}

if (import.meta.main) {
  const args = process.argv.slice(2);
  usageGate(args, { flags: ['--check', '--reseed'], valued: [{ flag: '--part', required: true }] }, `usage: bun run art:pack [--check] [--reseed] [--part a,b]
  hash each part of the art pack and upload those whose content moved to the GitHub release "art", then write tools/art/lock.json (commit it)
  --check    list what would be packed; exit 1 if anything would; upload nothing
  --reseed   pack the seed parts again (they are packed once: each machine's builders keep them)
  --part     only these parts (${PARTS.map(p => p.name).join(', ')})`, 'art:pack');
  const at = args.indexOf('--part'), t0 = performance.now();
  try {
    const r = await pack({ parts: pick(at >= 0 ? args[at + 1] : undefined), check: args.includes('--check'), reseed: args.includes('--reseed') });
    const s = ((performance.now() - t0) / 1000).toFixed(0);
    if (args.includes('--check')) { console.log(r.stale.length ? `${r.stale.length} part(s) to pack: bun run art:pack (${s} s)` : `art pack up to date (${s} s)`); process.exit(r.stale.length ? 1 : 0); }
    console.log(r.packed.length ? `packed ${r.packed.join(', ')} (${s} s); commit tools/art/lock.json` : `nothing to pack (${s} s)`);
  } catch (e) { console.error(`art:pack: ${(e as Error).message}`); process.exit(1); }
}
