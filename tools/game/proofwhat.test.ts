// The proof map against the tree, the shots and the batch (brief 098, Splice): every path of today's tree has a row (on the L9 merge 353 changed paths had none, and the
// batch ran everything), the rows a shot's script or checkpoint sits on name that shot, the gated steps are the batch's, and an engine or data change runs only its steps.
import { describe, expect, test } from 'bun:test';
import { existsSync, mkdirSync, mkdtempSync, readFileSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { GATED_STEPS, KIT_DIR, KIT_SETTLED, ROOT, changedPaths, classify, kitFiles, loadMap, planLines, snapshot } from './proofdelta';
import { whatIfAll, whatLines } from './proofwhat';

const m = loadMap(), tracked = Bun.spawnSync(['git', 'ls-files', '-z'], { cwd: ROOT, stdout: 'pipe' }).stdout.toString().split('\0').filter(Boolean);
const FOLDER_META = m.rows[m.rows.length - 1]!;
const ran = (w: ReturnType<typeof whatIfAll>) => Object.entries(w.steps).filter(([, g]) => g.run).map(([s]) => s);

describe('the map places the tree', () => {
  test('every tracked path, and every file of the ignored folders this checkout has, has a row', () => {
    const snap = Object.keys(snapshot(ROOT, m).files);
    expect([...new Set([...tracked, ...snap])].filter(f => !classify(m, f))).toEqual([]);
  });
  test('the last row takes only a folder\'s meta (Unity marks it `folderAsset: yes`): a file\'s meta goes with its file\'s row', () => {
    expect(FOLDER_META.match).toEqual(['unity/**/*.meta']);
    const taken = tracked.filter(f => classify(m, f) === FOLDER_META);
    expect(taken).toContain('unity/Assets/Suborned.meta'); expect(taken).toContain('unity/Assets/Resources.meta'); // a folder's, even one whose folder is gone
    expect(taken.filter(f => !/^folderAsset: yes$/m.test(readFileSync(join(ROOT, f), 'utf8')))).toEqual([]);
  });
  test('failing example: a new file with no row is named, runs the whole proof and every step, and takes every shot', () => {
    const w = whatIfAll(m, ['tools/mystery/x.ts', 'unity/Assets/Suborned/Runtime/Play/HostHold.cs']);
    expect(w.unplaced).toEqual(['tools/mystery/x.ts']);
    expect(w.plan).toMatchObject({ mode: 'full', unplaced: ['tools/mystery/x.ts'] });
    expect(w.plan.why).toContain('the map cannot place'); expect(planLines(w.plan).join('\n')).toContain('cannot place: tools/mystery/x.ts');
    expect(ran(w)).toEqual([...GATED_STEPS]); expect(w.steps.sim!.why).toContain('tools/mystery/x.ts');
    expect(w.shots.names).toBe('all'); expect(whatLines(w).at(-1)).toBe('UNPLACED 1: tools/mystery/x.ts');
  });
});

describe('the shots\' own inputs', () => {
  const shots = (JSON.parse(readFileSync(join(ROOT, 'tools/game/shots.json'), 'utf8')) as { shots: { name: string; script?: string; checkpoint?: string }[] }).shots, names = shots.map(s => s.name);
  test('a route script, pinned script or checkpoint a shot starts from sits on a row that names the shot', () => {
    const gaps: string[] = [];
    for (const s of shots) for (const f of [s.script && (s.script.includes('/') ? s.script : `contracts/missions/handover/routes/${s.script}.json`), s.checkpoint && `briefs/027/checkpoints/${s.checkpoint}.script.json`].filter(Boolean) as string[]) {
      expect(existsSync(join(ROOT, f)), f).toBe(true);
      const r = classify(m, f); if (r?.run !== 'full' && r?.shots !== 'all' && !(Array.isArray(r?.shots) && r.shots.includes(s.name))) gaps.push(`${s.name}: ${f}'s row does not name it`);
    }
    expect(gaps).toEqual([]);
  });
  test('every shot a row names is in shots.json', () => {
    expect(m.rows.flatMap(r => Array.isArray(r.shots) ? r.shots : []).filter(n => !names.includes(n))).toEqual([]);
  });
});

describe('the batch\'s steps', () => {
  test('the gated steps are the batch\'s: each `gated(c, id)` in batch.ts', () => {
    const ids = [...readFileSync(join(ROOT, 'tools/game/batch.ts'), 'utf8').matchAll(/gated\(c, '([\w-]+)'\)/g)].map(x => x[1]!);
    expect([...new Set(ids)].sort()).toEqual([...GATED_STEPS].sort());
  });
  test('an engine change: the whole proof, every shot, the engine\'s steps and the late rows\' fights; never the words, kit, world, scene or draw-distance steps (all nine until 2026-11-08)', () => {
    const w = whatIfAll(m, ['sim/Suborned.Sim/GameBodies.cs']);
    expect(w.plan.mode).toBe('full'); expect(w.shots.names).toBe('all');
    expect(ran(w)).toEqual(['sim', 'unity-match', 'scenarios', 'late-fight']);
  });
  test('the engine\'s rule inputs and its CLI run the engine\'s steps alone; the proof runs on the copies the sim step writes', () => {
    for (const f of ['contracts/rules/population.json', 'contracts/people/people.rules.json', 'contracts/rules/rulebook.schema098b.ts', 'sim/cli/Dial.cs']) { const w = whatIfAll(m, [f]); expect(ran(w), f).toEqual(['sim', 'unity-match', 'scenarios', 'late-fight']); expect(w.plan.only, f).toEqual([]); }
    expect(whatIfAll(m, ['unity/Assets/StreamingAssets/sim/population.json']).plan.mode).toBe('full');
  });
  test('the data the batch never plays runs nothing: route scripts no shot plays, the adaptive plans, the people generator\'s inputs', () => {
    for (const f of ['contracts/missions/handover/routes/route-a.json', 'contracts/missions/handover/routes/plans/route-a.plan.json', 'contracts/missions/handover/adaptive/quiet-a.adaptive.json', 'contracts/people/harbour-district.cast.json']) {
      const w = whatIfAll(m, [f]); expect(w.unplaced, f).toEqual([]); expect(ran(w), f).toEqual([]); expect(w.plan, f).toMatchObject({ mode: 'delta', only: [] }); expect(w.shots.names, f).toEqual([]);
    }
  });
  test('a route script a shot plays retakes those shots and nothing else', () => {
    const w = whatIfAll(m, ['contracts/missions/handover/routes/route-g-board.json']);
    expect(ran(w)).toEqual([]); expect(w.plan.only).toEqual([]); expect(w.shots.names).toEqual(['fight-safe-body']);
  });
  test('a world source remakes the export and the scene, then the frame-time steps; never the engine, words or kit steps', () => {
    for (const f of ['contracts/world/harbour-district.plan.json', 'tools/plan/checks.ts', 'tools/world/build.ts']) expect(ran(whatIfAll(m, [f])), f).toEqual(['world', 'unity-world', 'late-fight', 'tp-long']);
    expect(ran(whatIfAll(m, ['contracts/world/kit.json']))).toEqual(['kit', 'world', 'unity-world', 'late-fight', 'tp-long']);
    expect(ran(whatIfAll(m, ['tools/world/gen/kitrefs.test.ts', 'tools/plan/checks.test.ts', 'tools/world/test/preremap.kit.json']))).toEqual([]); // tests and their fixtures: nothing (a world test ran everything until 2026-11-08)
  });
  test('the camera\'s stand-off runs the whole proof and every shot, and no gated step', () => {
    const w = whatIfAll(m, ['unity/Assets/Suborned/Runtime/Play/PlayStandOff.cs']);
    expect(w.plan.mode).toBe('full'); expect(ran(w)).toEqual([]); expect(w.shots.names).toBe('all');
  });
});

describe('the kit copy as Unity owns it (DK_Glass_Sheen.mat: the build rewrote it after the snapshot, a second full proof)', () => {
  const tree = () => { const r = mkdtempSync(join(tmpdir(), 'kitsnap-')); for (const [f, t] of Object.entries({ 'Materials/A.mat': 'unity wrote this', 'Materials/A.mat.meta': 'guid', 'Models/b.fbx': 'mesh', 'Models/b.fbx.meta': 'import', 'Prefabs/b.prefab.meta': 'unity made' })) { mkdirSync(join(r, KIT_DIR, f, '..'), { recursive: true }); writeFileSync(join(r, KIT_DIR, f), t); } return r; };
  test('a .mat or .meta the kit copied reads as its catalogue hash; one Unity made is left out; the rest by its bytes; no settled record, all by bytes', () => {
    const r = tree(), bytes = kitFiles(r);
    expect([...bytes.keys()].sort()).toHaveLength(5); expect([...bytes.values()].every(v => v.startsWith('sha256:'))).toBe(true);
    mkdirSync(join(r, KIT_SETTLED, '..'), { recursive: true }); writeFileSync(join(r, KIT_SETTLED), JSON.stringify({ 'Materials/A.mat': 'c1', 'Materials/A.mat.meta': 'c2', 'Models/b.fbx.meta': 'c3' }));
    const k = Object.fromEntries(kitFiles(r));
    expect(k).toMatchObject({ [`${KIT_DIR}/Materials/A.mat`]: 'kit:c1', [`${KIT_DIR}/Models/b.fbx.meta`]: 'kit:c3' });
    expect(k[`${KIT_DIR}/Models/b.fbx`]).toStartWith('sha256:'); expect(k[`${KIT_DIR}/Prefabs/b.prefab.meta`]).toBeUndefined();
    writeFileSync(join(r, KIT_DIR, 'Materials/A.mat'), 'unity wrote it again'); // the build's material pass
    expect(kitFiles(r).get(`${KIT_DIR}/Materials/A.mat`)).toBe('kit:c1');
  });
  test('a ledger that read them as bytes is the same files read the other way: not a change; a moved catalogue hash, a model or a catalogued file gone is', () => {
    const M = `${KIT_DIR}/Materials/A.mat`, P = `${KIT_DIR}/Prefabs/b.prefab.meta`, F = `${KIT_DIR}/Models/b.fbx`;
    expect(changedPaths({ [M]: 'sha256:x', [P]: 'sha256:y', [F]: 'sha256:z' }, { [M]: 'kit:c1', [F]: 'sha256:z' })).toEqual([]);
    expect(changedPaths({ [M]: 'kit:c1' }, { [M]: 'kit:c2' })).toEqual([M]);
    expect(changedPaths({ [M]: 'kit:c1' }, {})).toEqual([M]);
    expect(changedPaths({ [F]: 'sha256:z' }, { [F]: 'sha256:w' })).toEqual([F]);
    expect(changedPaths({}, { [P]: 'sha256:y' })).toEqual([P]); // a new file where the bytes are all there is (no settled record) still counts
  });
});
