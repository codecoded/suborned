// Delta proofs (brief 078): the table, the plan, the ledger and the gate at the CLI, all without Unity. Run: bun test tools/game/proofdelta.test.ts
import { describe, expect, test } from 'bun:test';
import { existsSync, mkdirSync, mkdtempSync, readdirSync, readFileSync, rmSync, utimesSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { GATED } from './proofgates';
import { aloneLine, recordSteps, shotsGate, stepGate, changedPaths, classify, coverageGaps, emptyLedger, gateFile, ledgerFile, loadMap, mapHash, planFile, planLines, planProof, recordProof, readLedger, sectionIds, skipText, snapshot, snapshotOf, whatIf, writeLedger, writePlan, ROOT, type Ledger, type ProofMap } from './proofdelta';

const m = loadMap(), ids = sectionIds(m), PLAY = 'unity/Assets/Suborned/Runtime/Play/';
const snap = (o: Record<string, string>) => snapshotOf(new Map(Object.entries(o)));
const BUILD = { version: '0.2.146', commit: 'b00cace3+', at: '2026-10-30' };
/** Every section passed on `files` at build 146. */
function passedAll(files: Record<string, string>): Ledger {
  const s = snap(files), l = emptyLedger(); l.snapshots[s.id] = s.files;
  for (const id of ids) l.sections[id] = { build: BUILD.version, commit: BUILD.commit, at: BUILD.at, snapshot: s.id, map: mapHash(m, id) };
  return l;
}
/** A proof's `frames` in which exactly these sections ran and passed (everything else null, as the player writes them). */
const framesOf = (ran: string[], over: Record<string, unknown> = {}) => ({ follow: [], driveSession: ran.includes('drive') ? '/x/drive.json' : null, ...Object.fromEntries(ids.filter(i => i !== 'drive').map(i => [i, ran.includes(i) ? { ok: true } : null])), ...over });
const HOLD = PLAY + 'HudToolkit.Hold.cs', BASE = { [HOLD]: 'a', 'sim/data/rules.json': 'a', 'tools/mystery/x.ts': 'a' };

describe('the table', () => {
  test('every gated section is one the map can run, but the input map, which the player always runs', () => {
    expect(GATED.filter(g => !ids.includes(g))).toEqual(['inputMap']);
    expect(ids.filter(i => !(GATED as readonly string[]).includes(i))).toEqual(['drive', 'r123']);
  });
  test('the engine, its data, the world export and the kit are full, as the brief names them', () => {
    for (const p of ['sim/data/rulebook.json', 'sim/Suborned.Sim/Game.cs', 'out/world/harbour-district.world.json', 'contracts/world/kit.json', 'tools/kit/cli.ts', 'unity/Assets/Suborned/Kit/a/b.fbx', 'unity/Assets/Suborned/Sim/Plugins/Suborned.Sim.dll', 'unity/Assets/StreamingAssets/sim/x.json', PLAY + 'GameBoot.cs']) expect(classify(m, p)?.run).toBe('full');
  });
  test('a path it cannot place has no row: the game\'s other code, assets and settings among them', () => {
    for (const p of ['tools/mystery/x.ts', PLAY + 'HandoverHost.cs', PLAY + 'HostHold.cs', 'unity/ProjectSettings/QualitySettings.asset', 'unity/Packages/manifest.json', 'unity/Assets/Settings/PC_RPAsset.asset', 'unity/Assets/Suborned/Shaders/DecoKit/DK_Leaf.hlsl', 'contracts/missions/handover/mission.json', 'sim/cli/Program.cs']) expect(classify(m, p)).toBeUndefined();
  });
  test('holds to the sources: every HudToolkit member a proof reads and every helper of another proof it calls has that proof\'s sections on its row', () => {
    const dir = join(ROOT, PLAY), sources = Object.fromEntries(readdirSync(dir).filter(f => f.endsWith('.cs')).map(f => [f, readFileSync(join(dir, f), 'utf8')]));
    expect(coverageGaps(m, sources)).toEqual([]);
  });
  test('failing example: a proof that reads a member of a part whose row lacks its section is a hole, and so is a helper call', () => {
    const sources = {
      'ProofHold.cs': 'class P { void X() { var a = HudToolkit.SealWords(); ProofRings.DrawAll(); } }',
      'ProofRings.cs': 'class R { public static void DrawAll() {} }',
      'HudToolkit.Seal.cs': 'partial class H { public static string SealWords() => ""; }',
    };
    const narrow: ProofMap = { ...m, rows: [{ match: [PLAY + 'ProofRings.cs'], run: ['rings'], why: 'for this test' }, { match: [PLAY + 'ProofHold.cs'], run: ['holdActs'], why: 'for this test' }, { match: [PLAY + 'HudToolkit.Seal.cs'], run: ['r29'], why: 'for this test' }] };
    expect(coverageGaps(narrow, sources)).toEqual(['ProofHold.cs (holdActs) reads HudToolkit.SealWords, held in HudToolkit.Seal.cs: its row lacks holdActs', 'ProofHold.cs (holdActs) calls ProofRings: the row of ProofRings.cs lacks holdActs']);
    expect(coverageGaps(narrow, { ...sources, 'ProofHold.cs': '// HudToolkit.SealWords() in a comment is no read\nclass P {}' })).toEqual([]);
    expect(coverageGaps(narrow, { 'GameBoot.cs': 'if (Only("r118")) yield return ProofHold.Run(h);' })).toEqual(['GameBoot runs r118 through ProofHold: the row of ProofHold.cs lacks r118']);
  });
});

describe('the plan', () => {
  test('no ledger: the whole proof', () => {
    const p = planProof(m, snap(BASE), emptyLedger());
    expect(p).toMatchObject({ mode: 'full', only: [], skips: {} }); expect(p.why).toContain('no section has a recorded pass');
  });
  test('nothing changed: only the base, and every section says not run, unchanged since its build; none says passed', () => {
    const p = planProof(m, snap(BASE), passedAll(BASE));
    expect(p).toMatchObject({ mode: 'delta', only: [] });
    expect(Object.keys(p.skips)).toEqual(ids);
    expect(p.skips.r118).toBe('not run, unchanged since build 0.2.146 (b00cace3+)');
    expect(planLines(p).join(' ')).not.toMatch(/passed/);
  });
  test('a HUD-only change runs the budgeted pictures, the click-through and what reads it, in the proof\'s order', () => {
    const p = planProof(m, snap({ ...BASE, [HOLD]: 'b' }), passedAll(BASE));
    expect(p.mode).toBe('delta');
    expect(p.only).toEqual(['holdActs', 'r059', 'r067', 'r123', 'r083', 'rings', 'hudClick', 'holdRange']);
    expect(p.reasons.holdActs).toBe(`${HOLD} changed`);
    expect(p.skips.r118).toContain('not run'); expect(p.skips.holdActs).toBeUndefined();
    expect(p.seconds).toBeLessThan(m.sections.reduce((a, s) => a + s.seconds, m.baseSeconds) / 2);
  });
  test('fails safe: a changed path the map cannot place runs the whole proof, and says which', () => {
    const p = planProof(m, snap({ ...BASE, 'tools/mystery/x.ts': 'b', [HOLD]: 'b' }), passedAll(BASE));
    expect(p).toMatchObject({ mode: 'full', only: [] }); expect(p.why).toContain('tools/mystery/x.ts');
  });
  test('fails safe: a new file nobody has placed is a change too, and so is a deleted one', () => {
    expect(planProof(m, snap({ ...BASE, [PLAY + 'HostNew.cs']: 'n' }), passedAll(BASE)).mode).toBe('full');
    const { 'tools/mystery/x.ts': _gone, ...rest } = BASE; expect(planProof(m, snap(rest), passedAll(BASE)).mode).toBe('full');
  });
  test('any change to the engine\'s data runs the whole proof', () => {
    const p = planProof(m, snap({ ...BASE, 'sim/data/rules.json': 'b' }), passedAll(BASE));
    expect(p.mode).toBe('full'); expect(p.why).toContain('sim/data/rules.json');
  });
  test('--full and a submission build force the whole proof whatever changed', () => {
    expect(planProof(m, snap(BASE), passedAll(BASE), { force: '--full' })).toMatchObject({ mode: 'full', why: '--full', force: '--full' });
  });
  test('a skip with no earlier pass runs the section', () => {
    const l = passedAll(BASE); delete l.sections.r118;
    const p = planProof(m, snap(BASE), l);
    expect(p.only).toEqual(['r118']); expect(p.skips.r118).toBeUndefined(); expect(p.reasons.r118).toBe('no recorded pass');
  });
  test('a pass recorded under other rows of the map runs the section; a row edited for another section leaves it alone', () => {
    const l = passedAll(BASE); l.sections.r118!.map = 'a-different-mapping';
    expect(planProof(m, snap(BASE), l).only).toEqual(['r118']);
    const edited: ProofMap = { ...m, rows: [...m.rows, { match: ['x/**'], run: ['r29'], why: 'another section' }] }, l2 = passedAll(BASE);
    expect(mapHash(edited, 'r118')).toBe(mapHash(m, 'r118')); expect(mapHash(edited, 'r29')).not.toBe(mapHash(m, 'r29'));
    expect(planProof(edited, snap(BASE), l2).only).toEqual(['r29']);
    const hide: ProofMap = { ...m, rows: [{ match: [HOLD], run: 'none', why: 'hides a path from every section' }, ...m.rows] };
    expect(planProof(hide, snap(BASE), passedAll(BASE)).mode).toBe('full'); // a new "none" row touches every section's rows
  });
  test('a pass whose snapshot is gone cannot prove anything: the section runs', () => {
    const l = passedAll(BASE); l.sections.r29!.snapshot = 'gone';
    expect(planProof(m, snap(BASE), l).only).toEqual(['r29']);
  });
  test('the table\'s what-if: documents run nothing but the base', () => {
    expect(whatIf(m, ['briefs/078/x.md', 'GDD/a.md', 'tools/game/batch.ts', 'tools/game/x.test.ts'])).toMatchObject({ mode: 'delta', only: [] });
    expect(whatIf(m, [PLAY + 'Proof29.cs']).only).toEqual(['holdActs', 'done', 'r29', 'rings', 'holdRange']);
    expect(whatIf(m, [PLAY + 'HudToolkit.Scope.cs']).only).toEqual(['r059', 'r067', 'r123', 'hudClick']);
  });
});

describe('the ledger', () => {
  test('only a section that ran can pass: a skipped one keeps the build it passed on, so a second delta still says build 146', () => {
    const now = snap({ ...BASE, [HOLD]: 'b' }), l = passedAll(BASE), plan = planProof(m, now, l);
    const after = recordProof(m, l, now, plan, framesOf(plan.only), {}, { ...BUILD, version: '0.2.147' });
    expect(after.sections.holdActs!.build).toBe('0.2.147');
    expect(after.sections.r118!.build).toBe('0.2.146');
    const next = planProof(m, snap({ ...BASE, [HOLD]: 'c' }), after);
    expect(next.skips.r118).toBe('not run, unchanged since build 0.2.146 (b00cace3+)'); expect(next.only).toContain('holdActs');
  });
  test('a skipped section\'s old snapshot survives the write; an unused one is dropped', () => {
    const now = snap({ ...BASE, [HOLD]: 'b' }), l = passedAll(BASE), plan = planProof(m, now, l), after = recordProof(m, l, now, plan, framesOf(plan.only), {}, BUILD);
    const f = join(mkdtempSync(join(tmpdir(), 'ledger-')), 'l.json'); writeLedger(f, { ...after, snapshots: { ...after.snapshots, junk: {} } });
    const back = readLedger(f);
    expect(Object.keys(back.snapshots).sort()).toEqual([now.id, snap(BASE).id].sort());
    expect(planProof(m, now, back).only).toEqual([]);
  });
  test('a section that fails loses its pass, and the next plan runs it alone', () => {
    const now = snap(BASE), full = planProof(m, now, emptyLedger());
    const after = recordProof(m, emptyLedger(), now, full, framesOf(ids, { r29: { ok: false } }), { driveSession: true, fireSession: true }, BUILD);
    expect(after.sections.r29).toBeUndefined(); expect(after.sections.r118!.build).toBe(BUILD.version);
    const p = planProof(m, now, after);
    expect(p.only).toEqual(['r29']); expect(p.reasons.r29).toBe('failed last time');
  });
  test('passing alone after failing in company is a pass, marked with what it ran without (the 0.2.145 case), and no whole proof is re-run for it', () => {
    const now = snap(BASE), full = planProof(m, now, emptyLedger());
    const failed = recordProof(m, emptyLedger(), now, full, framesOf(ids, { r29: { ok: false } }), { driveSession: true, fireSession: true }, BUILD);
    const alone = recordProof(m, failed, now, planProof(m, now, failed), framesOf(['r29']), {}, { ...BUILD, version: '0.2.147' });
    expect(alone.sections.r29!.build).toBe('0.2.147'); expect(alone.retry).toEqual({});
    expect(alone.sections.r29!.alone).toEqual({ build: '0.2.146', without: ['drive', 'driveClick', 'fireStop', 'ringCheck', 'act', 'dblClick', 'holdActs', 'done', 'levels'] });
    expect(aloneLine('r29', alone.sections.r29!)).toContain('--full checks it');
    expect(planProof(m, now, alone).only).toEqual([]);
    const inCompany = recordProof(m, failed, now, full, framesOf(ids), { driveSession: true, fireSession: true }, BUILD);
    expect(inCompany.sections.r29!.alone).toBeUndefined();
  });
  test('a section that failed alone owes nothing: nothing ran before it', () => {
    const now = snap(BASE), l = passedAll(BASE); delete l.sections.r29;
    const after = recordProof(m, l, now, planProof(m, now, l), framesOf([], { r29: { ok: false } }), {}, BUILD);
    expect(after.retry).toEqual({});
    const fixed = recordProof(m, after, now, planProof(m, now, after), framesOf(['r29']), {}, BUILD);
    expect(fixed.sections.r29).toBeDefined(); expect(fixed.sections.r29!.alone).toBeUndefined();
  });
  test('not reached (ok null) has no pass and no debt: it just runs next time', () => {
    const now = snap(BASE), after = recordProof(m, emptyLedger(), now, planProof(m, now, emptyLedger()), framesOf(ids, { fire: { ok: null } }), { driveSession: true, fireSession: true }, BUILD);
    expect(after.sections.fire).toBeUndefined(); expect(after.retry).toEqual({});
    expect(planProof(m, now, after).reasons.fire).toBe('no recorded pass');
  });
  test('the driven section passes only with its session replaying, the fire section only with its session', () => {
    const now = snap(BASE), bad = recordProof(m, emptyLedger(), now, planProof(m, now, emptyLedger()), framesOf(ids), { driveSession: false, fireSession: false }, BUILD);
    expect(bad.sections.drive).toBeUndefined(); expect(bad.sections.fire).toBeUndefined(); expect(bad.sections.cars).toBeDefined();
  });
});

describe('the snapshot', () => {
  const git = (cwd: string, ...a: string[]) => Bun.spawnSync(['git', '-c', 'user.name=t', '-c', 'user.email=t@t', ...a], { cwd, stdout: 'pipe', stderr: 'pipe' });
  test('by content: a dirty file moves it, an edit put back does not, an untracked file counts, a document is not read at all', () => {
    const d = mkdtempSync(join(tmpdir(), 'snap-'));
    mkdirSync(join(d, 'briefs')); mkdirSync(join(d, 'tools'));
    writeFileSync(join(d, 'tools/a.ts'), 'one'); writeFileSync(join(d, 'briefs/b.md'), 'doc'); writeFileSync(join(d, '.gitignore'), 'ignored/\n');
    git(d, 'init', '-q'); git(d, 'add', '.'); git(d, 'commit', '-qm', 'x');
    const s0 = snapshot(d, m);
    expect(Object.keys(s0.files).sort()).toEqual(['tools/a.ts']); // .gitignore and the brief are "none"
    writeFileSync(join(d, 'briefs/b.md'), 'doc 2'); writeFileSync(join(d, 'briefs/new.png'), 'evidence');
    expect(snapshot(d, m).id).toBe(s0.id);
    writeFileSync(join(d, 'tools/a.ts'), 'two');
    const s1 = snapshot(d, m); expect(changedPaths(s0.files, s1.files)).toEqual(['tools/a.ts']);
    writeFileSync(join(d, 'tools/a.ts'), 'one'); expect(snapshot(d, m).id).toBe(s0.id);
    writeFileSync(join(d, 'tools/n.ts'), 'new'); expect(changedPaths(s0.files, snapshot(d, m).files)).toEqual(['tools/n.ts']);
    rmSync(join(d, 'tools/n.ts')); rmSync(join(d, 'tools/a.ts')); expect(changedPaths(s0.files, snapshot(d, m).files)).toEqual(['tools/a.ts']);
    rmSync(d, { recursive: true, force: true });
  });
});

describe('the batch\'s steps (delta only)', () => {
  const seed = (files: Record<string, string>, steps = ['sim', 'unity-match', 'scenarios', 'late-fight', 'shots']) => recordSteps(emptyLedger(), snap(files), steps, BUILD);
  test('a HUD change skips the engine steps and the frame-time steps; they say why', () => {
    for (const step of ['sim', 'unity-match', 'scenarios', 'late-fight']) expect(stepGate(m, snap({ ...BASE, [HOLD]: 'b' }), seed(BASE), step)).toEqual({ run: false, why: 'engine, world and kit unchanged since build 0.2.146' });
  });
  test('failing example: an engine change or a path the map cannot place runs them, and a step with no pass runs', () => {
    expect(stepGate(m, snap({ ...BASE, 'sim/data/rules.json': 'b' }), seed(BASE), 'sim')).toEqual({ run: true, why: 'sim/data/rules.json changed' });
    expect(stepGate(m, snap({ ...BASE, 'tools/mystery/x.ts': 'b' }), seed(BASE), 'late-fight').run).toBe(true);
    expect(stepGate(m, snap(BASE), seed(BASE, []), 'sim')).toEqual({ run: true, why: 'no recorded pass' });
  });
  test('--full and --submission force every step', () => {
    expect(stepGate(m, snap(BASE), seed(BASE), 'sim', '--full')).toEqual({ run: true, why: '--full' });
    expect(shotsGate(m, snap(BASE), seed(BASE), '--full').names).toBe('all');
  });
  test('a step that failed keeps its old pass, so an engine change is still owed after a section\'s fix: the plan going delta does not lose it', () => {
    const l = seed(BASE, ['sim']), now = { ...BASE, 'sim/data/rules.json': 'b' };
    expect(stepGate(m, snap({ ...now, [HOLD]: 'c' }), l, 'late-fight').why).toBe('no recorded pass');
    expect(stepGate(m, snap({ ...now, [HOLD]: 'c' }), recordSteps(l, snap(BASE), ['late-fight'], BUILD), 'late-fight').run).toBe(true);
  });
  test('the shots a path can show in: the rows of what changed since the last shots pass', () => {
    expect(shotsGate(m, snap({ ...BASE, [HOLD]: 'b' }), seed(BASE)).names).toEqual(['plate-pay-armed']);
    const ring = PLAY + 'HudToolkit.Ring.cs', both = { ...BASE, [ring]: 'a' };
    expect(shotsGate(m, snap({ ...both, [HOLD]: 'b', [ring]: 'b' }), seed(both)).names).toEqual(expect.arrayContaining(['plate-pay-armed', 'office-front-yaw210', 'cafe-front-day']));
    expect(shotsGate(m, snap({ ...BASE, [PLAY + 'HudToolkit.cs']: 'n' }), seed(BASE)).names).toBe('all');
  });
  test('a proof file, a document, a bot script: no shots', () => {
    const f = PLAY + 'Proof067.cs', g = shotsGate(m, snap({ ...BASE, [f]: 'n' }), seed(BASE));
    expect(g.names).toEqual([]); expect(g.why).toContain('no HUD or view path changed');
  });
  test('failing example: an engine change or an unplaced path takes every shot, and no record takes every shot', () => {
    expect(shotsGate(m, snap({ ...BASE, 'sim/data/rules.json': 'b' }), seed(BASE)).names).toBe('all');
    expect(shotsGate(m, snap({ ...BASE, 'tools/mystery/x.ts': 'b' }), seed(BASE)).names).toBe('all');
    expect(shotsGate(m, snap(BASE), seed(BASE, [])).names).toBe('all');
  });
  test('every shot the map names is a row of shots.json, and every HUD part has shots', () => {
    const rows = JSON.parse(readFileSync(join(ROOT, 'tools/game/shots.json'), 'utf8')).shots.map((s: { name: string }) => s.name);
    for (const r of m.rows) if (Array.isArray(r.shots)) expect(r.shots.filter(n => !rows.includes(n))).toEqual([]);
    for (const r of m.rows) if (r.match.some(g => g.includes('HudToolkit'))) expect(r.shots).toBeDefined();
  });
  test('a skipped step\'s snapshot survives the write', () => {
    const f = join(mkdtempSync(join(tmpdir(), 'ledger-')), 'l.json'), l = seed(BASE, ['sim']); writeLedger(f, l);
    expect(stepGate(m, snap(BASE), readLedger(f), 'sim').run).toBe(false);
  });
});

// The gate, at the CLI: cli.ts --check over a scratch build folder, nothing but git really started (SUBORNED_GAME_SPAWN_STUB; the same harness as cli.test.ts).
describe('the gate at the CLI', () => {
  const sha = Bun.spawnSync(['git', 'rev-parse', '--short', 'HEAD'], { cwd: ROOT, stdout: 'pipe' }).stdout.toString().trim();
  function folder(frames: Record<string, unknown>, ledger: Ledger, files: Record<string, string>, writePlanLie?: (p: any) => any) {
    const d = join(ROOT, 'out/scratch/splice', `gate-${Math.random().toString(36).slice(2)}`);
    mkdirSync(join(d, 'proof'), { recursive: true }); mkdirSync(join(d, 'The Suborned.app/Contents/MacOS'), { recursive: true });
    writeFileSync(join(d, 'The Suborned.app/Contents/MacOS/The Suborned'), '');
    writeFileSync(join(d, 'build.json'), JSON.stringify({ result: 'Succeeded', sha, version: '0.2.147', dirty: false }));
    writeFileSync(join(d, 'proof/scenarios.json'), JSON.stringify({ matching: 1, scenarios: 1, passed: true, runtime: 'Mono', dllSha256: 'abc' }));
    writeFileSync(join(d, 'proof/proof.json'), JSON.stringify({ status: 'ok', screen: '1920x1080', mode: 'Windowed', hudScale: 1, framesAt: '2880x1864', frames: { fps: 300, p99Ms: 5, ...frames } }));
    const future = new Date(Date.now() + 86400000); utimesSync(join(d, 'build.json'), future, future); utimesSync(join(d, 'proof/proof.json'), new Date(future.getTime() + 1000), new Date(future.getTime() + 1000));
    writeLedger(ledgerFile(d), ledger);
    const s = snap(files), p = planProof(m, s, ledger); writePlan(d, writePlanLie ? writePlanLie(p) : p, s);
    return d;
  }
  const check = (d: string, ...more: string[]) => Bun.spawnSync([process.execPath, join(ROOT, 'tools/game/cli.ts'), '--check', '--reuse-proof', '--proof-plan', planFile(d), '--no-open', ...more], { cwd: ROOT, stdout: 'pipe', stderr: 'pipe', env: { ...process.env, SUBORNED_GAME_OUT: d, SUBORNED_GAME_SPAWN_STUB: '1' } });
  const delta = (over: Record<string, unknown> = {}) => { const { r059: _gone, ...rest } = framesOf(['r067'], { inputMap: { ok: true }, ...over }) as Record<string, unknown>; return rest; }; // no r059 key: the app is not asked for its audio cues here
  const FILES = { [PLAY + 'Proof067.cs']: 'a' };

  test('a delta proof whose other sections have earlier passes goes through; each says not run; r067 goes on the ledger at this build', () => {
    const was = { [PLAY + 'Proof067.cs']: 'old' }, l = passedAll(was), d = folder(delta(), l, FILES), r = check(d), out = r.stdout.toString();
    expect(r.exitCode).toBe(0);
    expect(out).toContain('game: r118: not run, unchanged since build 0.2.146 (b00cace3+)');
    expect(out).toContain('game: driveSession: not run, unchanged since build 0.2.146');
    expect(out).toContain('game: r067 ok');
    const after = readLedger(ledgerFile(d)), gate = JSON.parse(readFileSync(gateFile(d), 'utf8'));
    expect(after.sections.r067!.build).toBe('0.2.147'); expect(after.sections.r118!.build).toBe('0.2.146');
    expect(gate).toMatchObject({ mode: 'delta', failed: [] }); expect(gate.ran).toContain('r067'); expect(gate.skips.r118).toContain('not run');
    rmSync(d, { recursive: true, force: true });
  });
  test('--no-scenarios: a delta goes through with no scenario report and says so; failing example: without the flag, no report fails', () => {
    const was = { [PLAY + 'Proof067.cs']: 'old' }, mk = () => { const d = folder(delta(), passedAll(was), FILES); rmSync(join(d, 'proof/scenarios.json')); return d; };
    const a = mk(), ra = check(a, '--no-scenarios');
    expect(ra.exitCode).toBe(0); expect(ra.stdout.toString()).toContain('game: scenarios: not run, the engine is unchanged');
    const b = mk(), rb = check(b);
    expect(rb.exitCode).toBe(1); expect(rb.stdout.toString()).toContain('game: scenarios no report');
    rmSync(a, { recursive: true, force: true }); rmSync(b, { recursive: true, force: true });
  });
  test('failing example: a null section with no earlier pass fails the gate, even when the plan file says it was skipped', () => {
    const was = { [PLAY + 'Proof067.cs']: 'old' }, l = passedAll(was); delete l.sections.r118;
    const d = folder(delta(), l, FILES, p => ({ ...p, only: ['r29'], skips: { ...p.skips, r118: 'not run, unchanged since build 1 (a lie)' } })), r = check(d), out = r.stdout.toString();
    expect(r.exitCode).toBe(1);
    expect(out).toContain('game: r118 FAILED: the section is null');
    expect(out).not.toContain('a lie');
    rmSync(d, { recursive: true, force: true });
  });
  test('failing example: a section the proof ran and failed fails, loses its pass and is named in the gate file', () => {
    const was = { [PLAY + 'Proof067.cs']: 'old' }, d = folder(delta({ r067: { ok: false, cones: { ok: false } } }), passedAll(was), FILES), r = check(d);
    expect(r.exitCode).toBe(1);
    expect(readLedger(ledgerFile(d)).sections.r067).toBeUndefined();
    expect(JSON.parse(readFileSync(gateFile(d), 'utf8')).failed).toEqual(['r067']);
    rmSync(d, { recursive: true, force: true });
  });
  test('without a plan nothing changes: a proof with null sections fails as it always did, and no ledger is written', () => {
    const d = folder(delta(), passedAll(FILES), FILES); rmSync(ledgerFile(d)); rmSync(planFile(d));
    const r = Bun.spawnSync([process.execPath, join(ROOT, 'tools/game/cli.ts'), '--check', '--reuse-proof', '--no-open'], { cwd: ROOT, stdout: 'pipe', stderr: 'pipe', env: { ...process.env, SUBORNED_GAME_OUT: d, SUBORNED_GAME_SPAWN_STUB: '1' } });
    expect(r.exitCode).toBe(1); expect(r.stdout.toString()).toContain('game: r118 FAILED: the section is null');
    expect(existsSync(ledgerFile(d))).toBe(false);
    rmSync(d, { recursive: true, force: true });
  });
});
