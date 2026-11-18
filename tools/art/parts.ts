// The art pack (Sky, 2026-11-18: "I just need my air to be able to build latest"; "2gb is a lot in git"): the build's art that git does not hold and no
// step rebuilds from the repo, in parts, each a .tar.gz on the GitHub release `art`, named by its content. tools/art/lock.json (committed) says which
// content each part has at this commit; `bun run art:pack` (the machine with the art: the Studio) uploads a part whose content moved and writes the lock;
// `bun run art:fetch` (any other machine: the Air) installs a part whose installed content differs from the lock. out/art/ holds each machine's stamp.
// A seed part is what the builders write themselves (the old town's Generated meshes and materials, some only ever copied from the old repo): installed
// once, then each machine's own world rebuild keeps it, so a rebuild on the Studio does not mean a new 1.5 GB upload. `--reseed` packs or fetches it again.
import { createHash } from 'node:crypto';
import { existsSync, lstatSync, mkdirSync, readdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, join, relative, resolve } from 'node:path';

export const REPO = resolve(import.meta.dir, '../..');
export const LOCK = join(REPO, 'tools/art/lock.json');
export const TAG = 'art', SLUG = 'codecoded/suborned';

export type Part = { name: string; why: string; root: 'repo' | 'store'; include: string[]; exclude?: string[]; seed?: boolean };
const S = 'unity/Assets/Suborned';
const withMeta = (...dirs: string[]) => dirs.flatMap(d => [d, d + '.meta']);
export const PARTS: Part[] = [
  { name: 'oldtown-art', root: 'repo', why: 'the old builders\' DecoKit art, copied from the old repo (brief 075), and the mobility pieces',
    include: withMeta(`${S}/OldTownArt`), exclude: withMeta(`${S}/OldTownArt/DecoKit/Generated`, `${S}/OldTownArt/Swap`) },
  { name: 'oldtown-generated', root: 'repo', seed: true, why: 'what the builders write: the old town\'s meshes, materials and probes (some only ever copied from the old repo), the swap\'s, the effects\' and the foliage review\'s',
    include: withMeta(`${S}/OldTownArt/DecoKit/Generated`, `${S}/OldTownArt/Swap`, `${S}/Resources/Fx097`, `${S}/Resources/Juice109`, `${S}/Generated/OldTownFoliage075`, `${S}/Generated/Review075`) },
  { name: 'kit', root: 'repo', why: 'the adopted DecoKit pieces as `bun run kit` copies them (contracts/world/kit.json), with the metas and materials Unity made', include: withMeta(`${S}/Kit`) },
  { name: 'people', root: 'repo', why: 'the crowd figures, clips, props and manifest `bun tools/figures/run.ts --install` writes (the cell\'s four are in git)', include: withMeta(`${S}/People`) },
  { name: 'cafe-plants', root: 'store', why: 'the café planting the old town swap reads beside the repo (PlantingSwap077, LeafShader077, Glint077: <repo>/../the-suborned-store)',
    include: ['the-suborned-store/testbed-art/cafe-plants'] },
];

/** Where a part's paths start: the repo, or the folder that holds it (the store sits beside the repo). */
export const baseOf = (p: Part, repo = REPO) => (p.root === 'repo' ? repo : dirname(repo));

export type Row = { sha256: string; bytes: number; path: string };
export type Manifest = { rows: Row[]; text: string; content: string; bytes: number };
export type LockPart = { content: string; files: number; bytes: number; asset: string; archiveSha256: string; archiveBytes: number; seed?: boolean };
export type Lock = { schema: 'suborned.art-lock/1'; release: { slug: string; tag: string }; parts: Record<string, LockPart> };

export const sha256 = (b: Uint8Array | string) => createHash('sha256').update(b).digest('hex');
export const fileSha = (abs: string) => sha256(readFileSync(abs));
export const assetName = (name: string, content: string) => `${name}-${content.slice(0, 12)}.tar.gz`;
const JUNK = (n: string) => n === '.DS_Store' || n.endsWith('~');

/** Every file of a part (paths relative to its base), sorted; git's own files left out (they come with a pull), and what `exclude` names. */
export function listPart(p: Part, repo = REPO, tracked: Set<string> = trackedFiles(repo)): string[] {
  const base = baseOf(p, repo), out: string[] = [], skip = (rel: string) => (p.exclude ?? []).some(x => rel === x || rel.startsWith(x + '/'));
  const walk = (rel: string) => {
    const abs = join(base, rel); if (!existsSync(abs) || skip(rel)) return;
    const st = lstatSync(abs);
    if (st.isDirectory()) { for (const n of readdirSync(abs)) if (!JUNK(n)) walk(rel + '/' + n); return; }
    if (st.isFile() && !(p.root === 'repo' && tracked.has(rel))) out.push(rel);
  };
  for (const i of p.include) walk(i);
  return out.sort();
}

/** The part's manifest: one `sha256 TAB bytes TAB path` line a file; its content hash names the part's archive. */
export function manifest(p: Part, repo = REPO, tracked?: Set<string>): Manifest {
  const base = baseOf(p, repo), rows = listPart(p, repo, tracked).map(path => { const b = readFileSync(join(base, path)); return { sha256: sha256(b), bytes: b.length, path }; });
  return fromRows(rows);
}
export function fromRows(rows: Row[]): Manifest {
  const text = rows.map(r => `${r.sha256}\t${r.bytes}\t${r.path}\n`).join('');
  return { rows, text, content: sha256(text), bytes: rows.reduce((s, r) => s + r.bytes, 0) };
}
export const parseRows = (text: string): Row[] => text.split('\n').filter(Boolean).map(l => { const [sha, bytes, path] = l.split('\t'); return { sha256: sha!, bytes: Number(bytes), path: path! }; });

export function trackedFiles(repo = REPO): Set<string> {
  const r = Bun.spawnSync(['git', 'ls-files', '-z', '--', 'unity'], { cwd: repo, stdout: 'pipe', stderr: 'pipe' });
  return new Set(r.stdout.toString().split('\0').filter(Boolean));
}

export const readLock = (file = LOCK): Lock => existsSync(file) ? JSON.parse(readFileSync(file, 'utf8')) : { schema: 'suborned.art-lock/1', release: { slug: SLUG, tag: TAG }, parts: {} };
export const writeLock = (lock: Lock, file = LOCK) => { mkdirSync(dirname(file), { recursive: true }); writeFileSync(file, JSON.stringify(lock, null, 1) + '\n'); };

/** This machine's installed parts: out/art/installed.json (part -> content) and out/art/<part>.tsv (its manifest, so the next install removes what left). */
export const stateDir = (repo = REPO) => join(repo, 'out/art');
export function readStamp(repo = REPO): Record<string, string> { const f = join(stateDir(repo), 'installed.json'); return existsSync(f) ? JSON.parse(readFileSync(f, 'utf8')) : {}; }
export function writeStamp(name: string, m: Manifest, repo = REPO) {
  const d = stateDir(repo); mkdirSync(d, { recursive: true });
  const s = readStamp(repo); s[name] = m.content;
  writeFileSync(join(d, 'installed.json'), JSON.stringify(s, Object.keys(s).sort(), 1) + '\n'); writeFileSync(join(d, `${name}.tsv`), m.text);
}
export function installedRows(name: string, repo = REPO): Row[] { const f = join(stateDir(repo), `${name}.tsv`); return existsSync(f) ? parseRows(readFileSync(f, 'utf8')) : []; }

/** The release: GitHub by default; SUBORNED_ART_RELEASE_DIR, a folder standing in for it (the tests). */
export const localRelease = () => process.env.SUBORNED_ART_RELEASE_DIR;
export const assetUrl = (lock: Lock, asset: string) => `https://github.com/${lock.release.slug}/releases/download/${lock.release.tag}/${asset}`;

export const mb = (b: number) => `${(b / 1048576).toFixed(b < 10 * 1048576 ? 1 : 0)} MB`;
export const pick = (names: string | undefined) => {
  if (!names) return PARTS;
  const want = names.split(','), bad = want.filter(n => !PARTS.some(p => p.name === n));
  if (bad.length) throw new Error(`no part ${bad.join(', ')}; parts: ${PARTS.map(p => p.name).join(', ')}`);
  return PARTS.filter(p => want.includes(p.name));
};
export const rel = (from: string, to: string) => relative(from, to) || '.';
