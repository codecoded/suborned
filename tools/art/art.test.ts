// The art pack (tools/art): pack on one machine, fetch on another, offline. A folder stands in for the GitHub release (SUBORNED_ART_RELEASE_DIR);
// two scratch repos stand in for the Studio and the Air, each with the store beside it.
import { afterAll, beforeAll, describe, expect, test } from 'bun:test';
import { existsSync, mkdirSync, mkdtempSync, readFileSync, readdirSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join } from 'node:path';
import { fetchArt } from './fetch';
import { pack } from './pack';
import { PARTS, readLock, readStamp } from './parts';

const T = mkdtempSync(join(tmpdir(), 'art-')), studio = join(T, 'studio/suborned'), air = join(T, 'air/suborned'), release = join(T, 'release'), lockFile = join(T, 'lock.json');
const parts = PARTS.filter(p => ['oldtown-generated', 'kit', 'people', 'cafe-plants'].includes(p.name)), quiet = { log: () => {} };
const put = (root: string, rel: string, text: string) => { mkdirSync(dirname(join(root, rel)), { recursive: true }); writeFileSync(join(root, rel), text); };
const git = (cwd: string, ...a: string[]) => Bun.spawnSync(['git', ...a], { cwd, stdout: 'pipe', stderr: 'pipe' });
const K = 'unity/Assets/Suborned';

beforeAll(() => {
  process.env.SUBORNED_ART_RELEASE_DIR = release;
  put(studio, `${K}/Kit/Models/a.fbx`, 'model a'); put(studio, `${K}/Kit/Models/a.fbx.meta`, 'guid: 1'); put(studio, `${K}/Kit.meta`, 'guid: k');
  put(studio, `${K}/Kit/Models/old.fbx`, 'goes away');
  put(studio, `${K}/Kit/Prefabs/Empty.meta`, 'fileFormatVersion: 2\nguid: e\nfolderAsset: yes\n'); mkdirSync(join(studio, `${K}/Kit/Prefabs/Empty`));
  put(studio, `${K}/People/Figures/c1/c1.fbx`, 'crowd'); put(studio, `${K}/People/Figures/scout/scout.fbx`, 'cell, in git');
  put(studio, `${K}/Resources/Fx097/fx.asset`, 'fx v1');
  put(dirname(studio), 'the-suborned-store/testbed-art/cafe-plants/palm.png', 'palm');
  git(studio, 'init', '-q'); git(studio, 'add', `${K}/People/Figures/scout/scout.fbx`);
  mkdirSync(air, { recursive: true }); git(air, 'init', '-q');
});
afterAll(() => { delete process.env.SUBORNED_ART_RELEASE_DIR; rmSync(T, { recursive: true, force: true }); });

describe('art pack', () => {
  test('pack uploads each part, its manifest beside it; git\'s own files stay out', async () => {
    const r = await pack({ repo: studio, lockFile, parts, ...quiet });
    expect(r.packed.sort()).toEqual(['cafe-plants', 'kit', 'oldtown-generated', 'people']);
    const lock = readLock(lockFile);
    expect(lock.parts.kit!.files).toBe(5); expect(lock.parts['oldtown-generated']!.seed).toBe(true);
    expect(readdirSync(release).sort()).toEqual(Object.values(lock.parts).flatMap(p => [p.asset, p.asset.replace(/\.tar\.gz$/, '.tsv')]).sort());
    const people = readFileSync(join(release, lock.parts.people!.asset.replace(/\.tar\.gz$/, '.tsv')), 'utf8');
    expect(people).toContain('People/Figures/c1/c1.fbx'); expect(people).not.toContain('scout');
    expect((await pack({ repo: studio, lockFile, parts, ...quiet })).packed).toEqual([]); // nothing moved: nothing uploaded
  });

  test('fetch installs every part on the other machine, the store beside the repo; again, it downloads nothing', async () => {
    const r = await fetchArt({ repo: air, lockFile, parts, ...quiet });
    expect(r.installed.sort()).toEqual(['cafe-plants', 'kit', 'oldtown-generated', 'people']);
    expect(readFileSync(join(air, `${K}/Kit/Models/a.fbx`), 'utf8')).toBe('model a');
    expect(readFileSync(join(dirname(air), 'the-suborned-store/testbed-art/cafe-plants/palm.png'), 'utf8')).toBe('palm');
    expect(existsSync(join(air, `${K}/People/Figures/scout/scout.fbx`))).toBe(false); // git brings that one
    expect(existsSync(join(air, `${K}/Kit/Prefabs/Empty`))).toBe(true); // an empty folder, from its meta
    expect((await fetchArt({ repo: air, lockFile, parts, ...quiet })).current.sort()).toEqual(['cafe-plants', 'kit', 'oldtown-generated', 'people']);
  });

  test('a moved part travels alone; a file it lost leaves the other machine too; a seed stays as its builders left it', async () => {
    put(studio, `${K}/Kit/Models/a.fbx`, 'model a, v2'); rmSync(join(studio, `${K}/Kit/Models/old.fbx`)); put(studio, `${K}/Resources/Fx097/fx.asset`, 'fx v2: a rebuild');
    const p = await pack({ repo: studio, lockFile, parts, ...quiet });
    expect(p.packed).toEqual(['kit']); expect(p.seeds).toEqual(['oldtown-generated']);
    const f = await fetchArt({ repo: air, lockFile, parts, ...quiet });
    expect(f.installed).toEqual(['kit']);
    expect(readFileSync(join(air, `${K}/Kit/Models/a.fbx`), 'utf8')).toBe('model a, v2');
    expect(existsSync(join(air, `${K}/Kit/Models/old.fbx`))).toBe(false);
    expect(readFileSync(join(air, `${K}/Resources/Fx097/fx.asset`), 'utf8')).toBe('fx v1');
    expect(readStamp(air).kit).toBe(readLock(lockFile).parts.kit!.content);
  });

  test('--check lists what is due and changes nothing', async () => {
    put(studio, `${K}/People/Figures/c1/c1.fbx`, 'crowd v2');
    expect((await pack({ repo: studio, lockFile, parts, check: true, ...quiet })).stale).toEqual(['people']);
    expect((await fetchArt({ repo: air, lockFile, parts, check: true, ...quiet })).needed).toEqual([]);
    await pack({ repo: studio, lockFile, parts, ...quiet });
    expect((await fetchArt({ repo: air, lockFile, parts, check: true, ...quiet })).needed).toEqual(['people']);
    expect(readFileSync(join(air, `${K}/People/Figures/c1/c1.fbx`), 'utf8')).toBe('crowd');
  });

  test('an archive that is not the lock\'s installs nothing', async () => {
    const a = readLock(lockFile).parts.people!.asset; writeFileSync(join(release, a), 'not the archive');
    await expect(fetchArt({ repo: air, lockFile, parts, ...quiet })).rejects.toThrow(/does not match the lock/);
    expect(readFileSync(join(air, `${K}/People/Figures/c1/c1.fbx`), 'utf8')).toBe('crowd');
  });

  test('packing where the art is not refuses', async () => {
    await expect(pack({ repo: air, lockFile: join(T, 'other.json'), parts: PARTS.filter(p => p.name === 'oldtown-art'), ...quiet })).rejects.toThrow(/no files/);
  });
});
