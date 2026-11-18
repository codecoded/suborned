// Brief pictures stay out of git (Sky, 2026-11-18: "keep concept art etc out of the repo"). .gitignore keeps every picture, video and sound of briefs/ on
// disk but the images a tool opens, each named by its own `!` line; a page over 1 MB carries its pictures, so it stays on disk too. briefs/media-manifest.tsv
// lists what lives on disk. This fails on a picture git tracks that .gitignore does not name, and on a tracked page in briefs/ over 1 MB.
import { describe, expect, test } from 'bun:test';
import { readFileSync, statSync } from 'node:fs';
import { join, resolve } from 'node:path';

const ROOT = resolve(import.meta.dir, '../..');
const git = (...a: string[]) => Bun.spawnSync(['git', ...a], { cwd: ROOT, stdout: 'pipe', stderr: 'pipe' }).stdout.toString();
const MEDIA = /\.(png|jpe?g|webp|gif|mp4|mov|wav|mp3|glb|fbx|blend)$/i;
const tracked = git('ls-files', '-z', '--', 'briefs').split('\0').filter(Boolean);

describe('briefs/ in git holds text, not pictures', () => {
  test('every picture git tracks in briefs/ is one a tool opens, named in .gitignore', () => {
    const media = tracked.filter(f => MEDIA.test(f));
    expect(media.length).toBeGreaterThan(0); // the art bible's sources at least
    // `-v -n --no-index` prints, for each path, the rule that decides it (`source:line:pattern<TAB>path`, `::<TAB>path` for none): a named input's is its `!` line
    const unnamed = git('check-ignore', '--no-index', '-v', '-n', '--', ...media).split('\n').filter(Boolean)
      .map(l => { const [rule, path] = l.split('\t'); return { pattern: rule!.split(':').slice(2).join(':'), path: path! }; })
      .filter(x => !x.pattern.startsWith('!')).map(x => x.path);
    expect(unnamed).toEqual([]);
  });

  test('no page in briefs/ over 1 MB (a page that size carries its pictures)', () => {
    expect(tracked.filter(f => f.endsWith('.html') && statSync(join(ROOT, f)).size > 1 << 20)).toEqual([]);
  });

  test('the manifest lists only what git no longer tracks', () => {
    const listed = readFileSync(join(ROOT, 'briefs/media-manifest.tsv'), 'utf8').split('\n').filter(l => l && !l.startsWith('#')).map(l => l.split('\t')[2]!);
    const held = new Set(tracked);
    expect(listed.length).toBeGreaterThan(0);
    expect(listed.filter(p => held.has(p))).toEqual([]);
  });
});
