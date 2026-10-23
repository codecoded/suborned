// bun run storage (brief 050): what the retention policy takes and what it never takes. A made-up tree, no disk walk: the nodes are built by hand.
import { describe, expect, test } from 'bun:test';
import { DAY, type Node } from './scan';
import { classify, DEFAULT, plan, type Tree } from './policy';
import { whoNames } from './refs';
import { mkdirSync, mkdtempSync, existsSync, utimesSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { prune } from './cli';

const NOW = Date.UTC(2026, 9, 22, 12);
const node = (path: string, bytes: number, ageDays: number, kids: Record<string, Node> = {}, names: string[] = [], list: Node['list'] = []): Node =>
  ({ path, bytes, files: 1, newest: NOW - ageDays * DAY, oldest: NOW - ageDays * DAY - DAY, kids: new Map(Object.entries(kids)), names: [...names, ...Object.keys(kids)], list });
const tree = (kids: Record<string, Node>, over: Partial<Tree> = {}): Tree => {
  const out = node('/r/out', 0, 0, kids), at = (rel: string) => { let n: Node | undefined = out; for (const part of rel.split('/').slice(1)) { n = n?.kids.get(part); if (!n) return undefined; } return rel === 'out' ? out : n; };
  return { root: '/r', now: NOW, out, at, refs: [], engines: new Set(), hasCommit: () => true, trackedFiles: new Set(), trackedDirs: new Set(), keeps: new Set(), images: [], imageNames: { paths: new Set(), names: new Set() }, unityLogs: [], ...over };
};
const rels = (t: Tree, only?: string[]) => plan(t, DEFAULT, only).units.map(u => u.rel).sort();
const held = (t: Tree, only?: string[]) => plan(t, DEFAULT, only).held.map(h => `${h.rel}:${h.reason}`).sort();

describe('classify', () => {
  test('agent folders, brief folders, model runs, the pipeline', () => {
    expect(classify('latch-0266').kind).toBe('agent-scratch'); expect(classify('keel038').kind).toBe('agent-scratch'); expect(classify('fable').kind).toBe('agent-scratch');
    expect(classify('044').kind).toBe('brief-scratch'); expect(classify('ivory-pistol-v4').kind).toBe('model-run'); expect(classify('enforcer-textured-v2').kind).toBe('model-run');
    expect(classify('world').kind).toBe('pipeline'); expect(classify('batch').kind).toBe('batch'); expect(classify('batch-044o.log').kind).toBe('log'); expect(classify('decokit-008').kind).toBe('decokit');
  });
  test('only a recorded run or contract pins; code and docs only mention', () => {
    expect(whoNames('runs/levels/harbour-district/r011/input.plan.json')).toBe('pin'); expect(whoNames('contracts/world/kit.json')).toBe('pin');
    expect(whoNames('tools/game/batch.ts')).toBe('code'); expect(whoNames('briefs/044/notes.md')).toBe('doc');
  });
});

describe('what the policy takes', () => {
  test('scratch older than the limit goes, newer stays, hot stays', () => {
    const t = tree({ keel030: node('/r/out/keel030', 100, 20), keel031: node('/r/out/keel031', 100, 5), keel032: node('/r/out/keel032', 100, 0.2) });
    expect(rels(t, ['scratch'])).toEqual(['out/keel030']);
  });
  test('a pin (a run names a path inside it) holds a folder that is otherwise old', () => {
    const t = tree({ keel030: node('/r/out/keel030', 100, 20) }, { refs: [{ path: 'out/keel030/x.json', who: 'pin', by: 'runs/levels/a/r1/input.json' }] });
    expect(rels(t, ['scratch'])).toEqual([]); expect(held(t, ['scratch'])).toEqual(['out/keel030:pinned']);
  });
  test('code and doc mentions do not hold it', () => {
    const t = tree({ keel030: node('/r/out/keel030', 100, 20) }, { refs: [{ path: 'out/keel030', who: 'code', by: 'tools/x.ts' }, { path: 'out/keel030', who: 'doc', by: 'briefs/1.md' }] });
    expect(rels(t, ['scratch'])).toEqual(['out/keel030']);
  });
  test('a KEEP file in the folder or above it holds it', () => {
    const t = tree({ keel030: node('/r/out/keel030', 100, 20), glint030: node('/r/out/glint030', 100, 20) }, { keeps: new Set(['out/keel030']) });
    expect(rels(t, ['scratch'])).toEqual(['out/glint030']);
    expect(rels(tree({ keel030: node('/r/out/keel030', 100, 20) }, { keeps: new Set(['out']) }), ['scratch'])).toEqual([]);
  });
  test('the live player stays; zips beyond the newest two go; perf builds go when old', () => {
    const zip = (n: string, age: number) => ({ path: `/r/out/build/play/${n}`, bytes: 10, mtime: NOW - age * DAY });
    const play = node('/r/out/build/play', 0, 0, {}, [], [zip('TheSuborned-0.2.1.zip', 9), zip('TheSuborned-0.2.5.zip', 8), zip('TheSuborned-0.2.9.zip', 6), zip('TheSuborned-0.2.13.zip', 5)]);
    const t = tree({ build: node('/r/out/build', 0, 0, { play, perf: node('/r/out/build/perf', 900, 9), 'perf-new': node('/r/out/build/perf-new', 900, 2) }) });
    expect(rels(t, ['build-zips'])).toEqual(['out/build/play/TheSuborned-0.2.1.zip', 'out/build/play/TheSuborned-0.2.5.zip']);
    expect(rels(t, ['build-old'])).toEqual(['out/build/perf']);
  });
  test('temp plays older than a day go; today\'s stay', () => {
    const f = (n: string, age: number) => ({ path: `/r/out/missions/plays/${n}`, bytes: 4, mtime: NOW - age * DAY });
    const t = tree({ missions: node('/r/out/missions', 0, 0, { plays: node('/r/out/missions/plays', 0, 0, {}, [], [f('a.json', 3), f('b.json', 0.1), f('c.json', 2)]) }) });
    const u = plan(t, DEFAULT, ['plays-tmp']).units[0]!;
    expect(u.paths.map(p => p.split('/').pop())).toEqual(['a.json', 'c.json']);
  });
  test('Unity Library copies and repo copies go; the project\'s own unity/Library is not under out/', () => {
    const t = tree({ 'old-world': node('/r/out/old-world', 0, 0, { unity: node('/r/out/old-world/unity', 0, 0, { Library: node('/r/out/old-world/unity/Library', 700, 4) }, ['Assets']) }), cmp: node('/r/out/cmp', 0, 0, { base: node('/r/out/cmp/base', 500, 3, {}, ['tools', 'briefs']) }) });
    expect(rels(t, ['unity-caches', 'repo-copies'])).toEqual(['out/cmp/base', 'out/old-world/unity/Library']);
  });
  test('engines: the tree, the newest six, a pinned one and one without a commit stay', () => {
    const e: Record<string, Node> = { tree: node('/r/out/missions/engine/tree', 5, 40) };
    for (let i = 0; i < 9; i++) e[`aaaa0000000${i}`] = node(`/r/out/missions/engine/aaaa0000000${i}`, 5, 20 + i);
    const t = tree({ missions: node('/r/out/missions', 0, 0, { engine: node('/r/out/missions/engine', 0, 0, e) }) }, { engines: new Set(['aaaa00000008']) });
    const p = plan(t, DEFAULT, ['engine-pins']);
    // newest six are 0..5; 6, 7, 8 are the candidates: 8 is pinned by a recorded run
    expect(p.units.map(u => u.rel.split('/').pop()).sort()).toEqual(['aaaa00000006', 'aaaa00000007']);
    expect(p.held.find(h => h.rel.endsWith('aaaa00000008'))!.reason).toBe('pinned');
  });
  test('a file git tracks inside is never taken', () => {
    const t = tree({ keel030: node('/r/out/keel030', 100, 20) }, { trackedDirs: new Set(['out/keel030']) });
    expect(rels(t, ['scratch'])).toEqual([]); expect(held(t, ['scratch'])).toEqual(['out/keel030:tracked']);
  });
  test('unity logs over the size limit go once a day old', () => {
    const t = tree({}, { unityLogs: [{ rel: 'unity/Logs/Editor.log', bytes: 900 << 20, mtime: NOW - 2 * DAY }, { rel: 'unity/Logs/upm.log', bytes: 1000, mtime: NOW - 2 * DAY }, { rel: 'unity/Logs/Fresh.log', bytes: 900 << 20, mtime: NOW }] });
    expect(rels(t, ['unity-logs'])).toEqual(['unity/Logs/Editor.log']);
  });
  test('ignored screenshots: old and unnamed go; named, read by a tool, or young stay', () => {
    const im = (rel: string, age: number) => ({ rel, bytes: 100, mtime: NOW - age * DAY });
    const t = tree({}, {
      images: [im('briefs/040/a.png', 30), im('briefs/040/named.png', 30), im('briefs/014/play/x.png', 30), im('briefs/040/young.png', 2)],
      imageNames: { paths: new Set(), names: new Set(['named.png']) },
    });
    const p = plan(t, DEFAULT, ['brief-images']);
    expect(p.units.flatMap(u => u.paths)).toEqual(['/r/briefs/040/a.png']);
    expect(p.held.reduce((a, h) => a + h.files, 0)).toBe(2);
  });
});

describe('--prune on a real folder', () => {
  const make = () => {
    const root = mkdtempSync(join(tmpdir(), 'storage-')), old = new Date(Date.now() - 30 * DAY);
    for (const [d, f] of [['out/keel030', 'a.txt'], ['out/keel031', 'b.txt'], ['out/glint030', 'c.txt']] as const) { mkdirSync(join(root, d), { recursive: true }); writeFileSync(join(root, d, f), 'x'.repeat(5000)); }
    for (const f of ['out/keel030/a.txt', 'out/glint030/c.txt']) utimesSync(join(root, f), old, old);
    writeFileSync(join(root, 'out/glint030/KEEP'), ''); utimesSync(join(root, 'out/glint030/KEEP'), old, old);
    return root;
  };
  test('a dry run deletes nothing', () => {
    const root = make(); prune(root, false, DEFAULT, ['scratch']);
    expect(existsSync(join(root, 'out/keel030/a.txt'))).toBe(true);
  });
  test('--yes deletes the old folder, keeps the fresh one and the KEEP one, and logs it', () => {
    const root = make(); expect(prune(root, true, DEFAULT, ['scratch'])).toBe(0);
    expect(existsSync(join(root, 'out/keel030'))).toBe(false);
    expect(existsSync(join(root, 'out/keel031/b.txt'))).toBe(true); expect(existsSync(join(root, 'out/glint030/c.txt'))).toBe(true);
    expect(existsSync(join(root, 'out/storage'))).toBe(true);
  });
});
