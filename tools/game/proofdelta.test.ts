// Delta proofs (brief 078): the table, the plan, the ledger and the gate at the CLI, all without Unity. Run: bun test tools/game/proofdelta.test.ts
import { describe, expect, test } from 'bun:test';
import { existsSync, mkdirSync, mkdtempSync, readdirSync, readFileSync, rmSync, utimesSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { GATED } from './proofgates';
import { comparedWords, proofSources, wordGaps, wordsOf } from './words';
import { zoneProblem } from './batch';
import { MISSION_FILE, missionParts, stillsKeyOf, hostGaps, hostReads, sectionReads, aloneLine, recordSteps, shotsGate, stepGate, changedPaths, classify, coverageGaps, emptyLedger, gateFile, ledgerFile, loadMap, mapHash, planFile, planLines, planProof, recordProof, readLedger, sectionIds, skipText, snapshot, snapshotOf, whatIf, writeLedger, writePlan, ROOT, type Ledger, type ProofMap } from './proofdelta';

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
    expect(ids.filter(i => !(GATED as readonly string[]).includes(i))).toEqual(['drive', 'r123', 'r101', 'r102']); // r101, r102: briefs 101 and 102's pictures for Sky, not gated
  });
  test('the engine, its data, the world export and the kit are full, as the brief names them', () => {
    for (const p of ['sim/data/rulebook.json', 'sim/Suborned.Sim/Game.cs', 'out/world/harbour-district.world.json', 'contracts/world/kit.json', 'tools/kit/catalogue.ts', 'unity/Assets/Suborned/Kit/a/b.fbx', 'unity/Assets/Suborned/Sim/Plugins/Suborned.Sim.dll', 'unity/Assets/StreamingAssets/sim/x.json', PLAY + 'GameBoot.cs']) expect(classify(m, p)?.run).toBe('full');
  });
  test('a path it cannot place has no row: a new tool or host file (brief 098: every path of today\'s tree has one, proofwhat.test.ts)', () => {
    for (const p of ['tools/mystery/x.ts', PLAY + 'HostNew.cs', 'unity/Assets/Suborned/Mystery/x.asset']) expect(classify(m, p)).toBeUndefined();
  });
  test('the project, the render pipeline, the shaders and the engine\'s loader are full; the settings and packages every step, the rest no engine, kit, world or scene step', () => {
    for (const p of ['unity/ProjectSettings/QualitySettings.asset', 'unity/Packages/manifest.json']) { expect(classify(m, p)?.run).toBe('full'); expect(classify(m, p)?.steps).toBeUndefined(); }
    for (const p of ['unity/Assets/Settings/PC_RPAsset.asset', 'unity/Assets/Suborned/Shaders/DecoKit/DK_Leaf.hlsl']) { expect(classify(m, p)?.run).toBe('full'); expect(classify(m, p)?.steps).toEqual(['late-fight', 'tp-long']); }
    expect(classify(m, PLAY + 'SimBundle.cs')?.steps).toEqual(['unity-match', 'scenarios']);
    expect(classify(m, 'sim/cli/Program.cs')).toMatchObject({ run: [], steps: ['sim', 'unity-match', 'scenarios', 'late-fight'] }); // the CLI: headless scenarios and the late rows' ends, never the player
  });
  test('tools no batch step imports are none; the late rows\' pins are that step\'s alone', () => {
    for (const p of ['tools/figures/build.py', 'tools/life/cli.ts', 'tools/dialogue/cli.ts', 'tools/missions/check/cli.ts', 'tools/game/hostlogic/HostLogic.cs', 'tools/game/hostlogic.ts']) expect(classify(m, p)?.run).toBe('none');
    const pin = classify(m, 'tools/game/pins/loud-fight.seed3.json'); expect(pin?.run).toEqual([]); expect(pin?.steps).toEqual(['late-fight']);
    expect(classify(m, 'tools/game/pins/loud.seed3.json')).toMatchObject({ run: [], shots: ['fight-loud-body'] }); // a shot's pin, not a late row's (shots.json fight-loud-body)
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

describe('the host rows', () => {
  const dir = join(ROOT, PLAY), sources = Object.fromEntries(readdirSync(dir).filter(f => f.endsWith('.cs')).map(f => [f, readFileSync(join(dir, f), 'utf8')]));
  test('holds to the sources: every host member a section reads is on the row of its file, and every Host*.cs has a row', () => {
    expect(hostGaps(m, sources)).toEqual([]);
  });
  test('reads are by section, through the proof\'s own helpers: leash reads the leash log, driveClick does not', () => {
    const r = sectionReads(sources);
    expect(r.get('leash')).toContain('LeashLog'); expect(r.get('driveClick')).not.toContain('LeashLog');
    expect(hostReads(sources, m.setup?.members).get('HostHold.cs')!.get('holdActs')).toContain('HoldCancels');
  });
  test('failing example: a section reading a host member its file\'s row lacks is a hole, and so is a host file with no row; a setup call is not counted, with its reason', () => {
    const src = {
      'GameBoot.cs': 'if (Only("holdActs")) yield return ProofHold.Handover(h, Dir, log, holdActs);\nif (Only("rings")) yield return ProofRings.Handover(h, Dir, log, rings);',
      'ProofHold.cs': '    class P\n    {\n        public static IEnumerator Handover(HandoverHost h) { var a = h.HoldCancels; Help(h); }\n        static void Help(HandoverHost h) { h.SetSpeed(1); }\n    }',
      'ProofRings.cs': '    class R\n    {\n        public static IEnumerator Handover(HandoverHost h) { h.SetPaused(true); }\n    }',
      'HostHold.cs': '    partial class HandoverHost\n    {\n        public int HoldCancels { get; private set; }\n    }',
      'HandoverHost.cs': '    partial class HandoverHost\n    {\n        public void SetSpeed(int x) {}\n        public void SetPaused(bool x) {}\n    }',
      'HostNew.cs': '    partial class HandoverHost\n    {\n    }',
    };
    const narrow: ProofMap = { ...m, setup: { why: 'for this test', members: ['SetPaused'] }, rows: [{ match: [PLAY + 'HostHold.cs'], run: ['r29'], why: 'for this test' }, { match: [PLAY + 'HandoverHost.cs'], run: ['holdActs'], why: 'for this test' }] };
    expect(hostGaps(narrow, src).sort()).toEqual(['HostNew.cs has no row', 'holdActs reads HoldCancels, held in HostHold.cs: its row lacks holdActs'].sort());
    expect(hostGaps({ ...narrow, setup: undefined }, src)).toContain('rings reads SetPaused, held in HandoverHost.cs: its row lacks rings');
  });
  test('the typical changes stay deltas: a host file, the words, a capture tool; a scene builder does not', () => {
    expect(whatIf(m, [PLAY + 'HostHold.cs']).only).toEqual(['dblClick', 'holdActs', 'r083', 'r101', 'holdRange']); // brief 101 step 6: r101's first shot holds the charge (HostHold.ChargeScale)
    expect(whatIf(m, ['contracts/missions/handover/text.json']).only).toEqual(['done', 'r083']);
    expect(classify(m, 'unity/Assets/Suborned/Editor/World/Review/Flyby.cs')?.run).toBe('none');
    expect(classify(m, 'unity/Assets/Suborned/Editor/World/Play/OldTownPlay.cs')?.run).toBe('full');
    expect(classify(m, 'unity/Assets/Suborned/Editor/OldTown/Swap/OldTownSwap.cs')?.run).toBe('full');
    expect(classify(m, 'unity/Assets/Suborned/Editor/World/HarbourWorldBuilder.cs')?.run).toBe('full');
    expect(classify(m, 'unity/Assets/Suborned/Editor/World/Review/After072.cs')?.run).toBe('full');
  });
  test('a scene builder rebuilds the scene and never the engine; the words copy the words and rebuild nothing; a host file runs no gated step', () => {
    const SB = 'unity/Assets/Suborned/Editor/World/HarbourWorldBuilder.cs', T = 'contracts/missions/handover/text.json', H = PLAY + 'HostHold.cs', files = { [SB]: 'a', [T]: 'a', [H]: 'a' };
    const l = recordSteps(emptyLedger(), snap(files), ['sim', 'kit', 'world', 'unity-world', 'unity-match', 'scenarios', 'late-fight', 'tp-long', 'words'], BUILD), go = (f: string, step: string) => stepGate(m, snap({ ...files, [f]: 'b' }), l, step).run;
    expect(['sim', 'kit', 'world', 'unity-world', 'unity-match', 'scenarios', 'late-fight', 'tp-long', 'words'].filter(s => go(SB, s))).toEqual(['unity-world', 'late-fight', 'tp-long']);
    expect(['sim', 'kit', 'world', 'unity-world', 'unity-match', 'scenarios', 'late-fight', 'tp-long', 'words'].filter(s => go(T, s))).toEqual(['words']);
    expect(['sim', 'kit', 'world', 'unity-world', 'unity-match', 'scenarios', 'late-fight', 'tp-long', 'words'].filter(s => go(H, s))).toEqual([]);
    // brief 098: the engine runs its own steps and the late rows' fights, never the kit, world, scene, words or draw-distance steps (it ran them all until 2026-11-08)
    expect(['sim', 'kit', 'world', 'unity-world', 'unity-match', 'scenarios', 'late-fight', 'tp-long', 'words'].filter(s => stepGate(m, snap({ ...files, 'sim/data/x.json': 'b' }), l, s).run)).toEqual(['sim', 'unity-match', 'scenarios', 'late-fight']);
    expect(['sim', 'kit', 'world', 'unity-world', 'unity-match', 'scenarios', 'late-fight', 'tp-long', 'words'].filter(s => stepGate(m, snap({ ...files, 'tools/mystery/x.ts': 'b' }), l, s).run)).toHaveLength(9); // failing example: a path no row places still runs them all
  });
  test('the stills stand through a host, HUD or words change; a figure, the light or the world takes them again', () => {
    const base = { [HOLD]: 'a', [PLAY + 'HostViews.Figures.cs']: 'a', [PLAY + 'HostHold.cs']: 'a', 'contracts/missions/handover/text.json': 'a', 'out/world/harbour-district.world.json': 'a' }, k = stillsKeyOf(snap(base));
    for (const f of [HOLD, PLAY + 'HostHold.cs', 'contracts/missions/handover/text.json']) expect(stillsKeyOf(snap({ ...base, [f]: 'b' }))).toBe(k);
    for (const f of [PLAY + 'HostViews.Figures.cs', 'out/world/harbour-district.world.json']) expect(stillsKeyOf(snap({ ...base, [f]: 'b' }))).not.toBe(k);
  });
});

describe('the words', () => {
  const text = JSON.parse(readFileSync(join(ROOT, 'contracts/missions/handover/text.json'), 'utf8')), pins = JSON.parse(readFileSync(join(ROOT, 'tools/game/proofwords.json'), 'utf8')).words;
  test('every word a proof compares is in text.json and on the list', () => {
    expect(wordGaps(pins, proofSources(), wordsOf(text))).toEqual([]);
  });
  test('failing example: a word that leaves text.json, and a new comparison not on the list', () => {
    const src = { 'ProofX.cs': 'if (a.Contains("Guard back")) b(); // a.Contains("Comment word")' };
    expect(comparedWords(src, 'Guard back\nComment word')).toEqual({ 'ProofX.cs': ['Guard back'] });
    expect(wordGaps({ 'ProofX.cs': ['Guard back'] }, src, 'other words')[0]).toContain('no longer in text.json');
    expect(wordGaps({}, src, 'Guard back')[0]).toContain('add it to tools/game/proofwords.json');
  });
});

describe('the old town\'s zone gate', () => {
  const ok = { ours: { kitRenderers: 906 }, swapped: { kitRenderers: 906 }, kitOnlyInOurs: ['R:DK_A,-20917,1860,-30170,M+N'], kitOnlyInSwapped: ['R:DK_A,-20917,1860,-30171,M+N'], materialCountsThatDiffer: [], oldRenderersInZoneByPath: { 'Deco Sky Traffic/Lane rooftop 70 m <- Deco Sky Traffic': 2 } };
  test('the zone as the last batch left it holds: 1 cm of rounding, sky traffic over it', () => { expect(zoneProblem(ok)).toBeNull(); });
  test('failing examples: a missing report, a renderer moved 3 cm or gone, another count, a differing material, an old building in the zone', () => {
    expect(zoneProblem(undefined)).toContain('no zone-regression');
    expect(zoneProblem({ ...ok, kitOnlyInSwapped: ['R:DK_A,-20917,1860,-30173,M+N'] })).toContain('moved or missing');
    expect(zoneProblem({ ...ok, kitOnlyInSwapped: [] })).toContain('moved or missing');
    expect(zoneProblem({ ...ok, swapped: { kitRenderers: 905 } })).toContain('kit renderers 906');
    expect(zoneProblem({ ...ok, materialCountsThatDiffer: ['DK_A 3->4'] })).toContain('materials differ');
    expect(zoneProblem({ ...ok, oldRenderersInZoneByPath: { 'Deco City/Tower <- Deco City': 1 } })).toContain('old renderers');
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
    expect(p.seconds).toBeLessThan(m.sections.reduce((a, s) => a + s.seconds, m.baseSeconds) * 0.6); // on the seconds measured on 0.2.168: this row is one of the dearer deltas, about half the proof
  });
  test('a full plan names the edit, not the kit files the batch\'s own steps rewrote', () => {
    const KIT = 'unity/Assets/Suborned/Kit/Models/a.fbx.meta', EDIT = 'unity/Assets/Suborned/Shaders/x.hlsl';
    expect(planProof(m, snap({ ...BASE, [KIT]: 'n', [EDIT]: 'n' }), passedAll(BASE)).why).toContain(EDIT);
    expect(planProof(m, snap({ ...BASE, [KIT]: 'n' }), passedAll(BASE)).why).toContain(KIT); // failing example: a real kit change alone still runs everything
    expect(planProof(m, snap({ ...BASE, [KIT]: 'n' }), passedAll(BASE)).mode).toBe('full');
  });
  test('the look rows: lens.json and the play look run r118 and every picture, and no engine or scene step', () => {
    for (const p of ['unity/Assets/Suborned/Resources/Atmosphere/lens.json', 'unity/Assets/Suborned/Runtime/OldTown/OldTownPlayLook.cs']) {
      const r = classify(m, p)!; expect(r.run).toEqual(['r118']); expect(r.shots).toBe('all'); expect(r.steps).toBeUndefined();
    }
    expect(classify(m, 'unity/Assets/Suborned/Runtime/OldTown/DKCutaway.cs')).toMatchObject({ run: ['r118', 'levels'], shots: 'all' }); // the old town's pieces in the played scene: the look and the cut-away
    expect(classify(m, 'tools/kit/kit.test.ts')?.run).toEqual([]); expect(classify(m, 'tools/kit/kit.test.ts')?.steps).toEqual([]);
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
    expect(whatIf(m, [PLAY + 'HudToolkit.Menu.cs']).only).toEqual(['r059', 'r067', 'r123', 'hudClick']); // a part no proof reads and r083's pictures don't show
    // brief 101: a part on every play frame (the lens, the clock stack) is in r083's judged picture too (r085-conv-cards), so a change to it runs r083 as well
    for (const f of ['HudToolkit.Scope.cs', 'HudToolkit.Clock.cs', 'HudToolkit.Faces.cs']) expect(whatIf(m, [PLAY + f]).only).toEqual(['r059', 'r067', 'r123', 'r083', 'hudClick']);
    // the street kit draws every piece, as the HUD's root does; a new host file no proof reaches runs no section, and says why
    expect(whatIf(m, [PLAY + 'HudToolkit.Street.cs']).mode).toBe('full'); expect(whatIf(m, [PLAY + 'HudToolkit.Tabs.cs']).mode).toBe('full');
    expect(whatIf(m, [PLAY + 'HostWays101.cs'])).toMatchObject({ mode: 'delta', only: [] });
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
  test('a section the gate does not judge passes by having run, so it does not run again on every delta; a gated one that says false does not', () => {
    const now = snap(BASE), full = planProof(m, now, emptyLedger());
    const after = recordProof(m, emptyLedger(), now, full, framesOf(ids, { r123: { ok: false }, r29: { ok: false } }), { driveSession: true, fireSession: true }, BUILD);
    expect(after.sections.r123).toBeDefined(); expect(after.sections.r29).toBeUndefined();
    expect(planProof(m, now, after).only).toEqual(['r29']);
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
  test('mission.json by what reads each field: a listen partner reaches only the sim step and, through its copy, r067 and r083; a destination the world and scene steps; never the kit or tp-long', () => {
    const M = MISSION_FILE, text = (o: object) => JSON.stringify({ note: 'n', points: [1], listenPartners: [1], destinations: [1], clock: { startsAt: '1' }, ...o }), base = missionParts(text({}));
    const now = (o: object) => missionParts(text(o)), go = (f: Record<string, string>, step: string) => stepGate(m, snap({ ...f, [M]: 'b' }), recordSteps(emptyLedger(), snap({ ...base, [M]: 'a' }), ['sim', 'kit', 'world', 'unity-world', 'unity-match', 'scenarios', 'late-fight', 'tp-long'], BUILD), step).run;
    const STEPS = ['sim', 'kit', 'world', 'unity-world', 'unity-match', 'scenarios', 'late-fight', 'tp-long'], ran = (f: Record<string, string>) => STEPS.filter(s => go(f, s));
    expect(ran(now({ listenPartners: [2] }))).toEqual(['sim']);
    expect(ran(now({ destinations: [2] }))).toEqual(['world', 'unity-world']); expect(ran(now({ clock: { startsAt: '2' } }))).toEqual(['world', 'unity-world']);
    expect(ran(now({ points: [2] }))).toEqual([]); expect(ran(now({ note: 'other' }))).toEqual([]);
    expect(now({ listenPartners: [2] })[`${M}#rest`]).toBe(base[`${M}#rest`]); 
    // a field outside the parts goes to rest
    expect(now({ fresh: 1 })[`${M}#rest`]).not.toBe(base[`${M}#rest`]);
    const copy = 'unity/Assets/StreamingAssets/sim/handover.listen-partners.json'; expect(whatIf(m, [copy]).only).toEqual(['r067', 'r083']); expect(whatIf(m, [`${M}#listenPartners`]).only).toEqual([]);
    expect(whatIf(m, [`${M}#points`]).only.sort()).toEqual(['cars', 'done', 'r067']);
    expect(classify(m, 'unity/Assets/StreamingAssets/sim/rulebook.json')?.run).toBe('full'); // failing example: another copy stays on the full row
  });
  test('a ledger from before the split: the whole file unchanged is no change, moved it counts', () => {
    const M = MISSION_FILE, parts = missionParts('{"points":[1]}'); expect(changedPaths({ [M]: 'a' }, { [M]: 'a', ...parts })).toEqual([]);
    expect(changedPaths({ [M]: 'a' }, { [M]: 'b', ...parts }).length).toBe(5);
  });
  test('the engine\'s copier runs its own step and the steps that check the DLL it builds (gates read the tree at the batch\'s start); what it writes is placed by its own rows', () => {
    expect(classify(m, 'tools/sim/unity.ts')).toMatchObject({ run: [], steps: ['sim', 'unity-match', 'scenarios', 'late-fight'] });
    expect(classify(m, 'unity/Assets/Suborned/Sim/Plugins/Suborned.Sim.dll')?.run).toBe('full');
  });
  test('the rules\' source runs the engine\'s steps and every shot, never the kit, world, scene or tp-long; no section by itself, the copy on the full row does that', () => {
    const f = 'contracts/rules/rulebook.json', base = { ...BASE, [f]: 'a' }, go = (step: string) => stepGate(m, snap({ ...base, [f]: 'b' }), recordSteps(emptyLedger(), snap(base), ['sim', 'kit', 'world', 'unity-world', 'unity-match', 'scenarios', 'late-fight', 'tp-long'], BUILD), step).run;
    expect(['sim', 'unity-match', 'scenarios', 'late-fight'].filter(go)).toEqual(['sim', 'unity-match', 'scenarios', 'late-fight']); expect(['kit', 'world', 'unity-world', 'tp-long'].filter(go)).toEqual([]);
    expect(classify(m, f)?.shots).toBe('all'); expect(classify(m, 'unity/Assets/StreamingAssets/sim/rulebook.json')?.run).toBe('full');
  });
  const seed = (files: Record<string, string>, steps = ['sim', 'unity-match', 'scenarios', 'late-fight', 'shots']) => recordSteps(emptyLedger(), snap(files), steps, BUILD);
  test('a HUD change skips the engine steps and the frame-time steps; they say why', () => {
    for (const step of ['sim', 'unity-match', 'scenarios', 'late-fight']) expect(stepGate(m, snap({ ...BASE, [HOLD]: 'b' }), seed(BASE), step)).toEqual({ run: false, why: 'nothing it reads changed since build 0.2.146' });
  });
  test('failing example: an engine change or a path the map cannot place runs them, and a step with no pass runs', () => {
    expect(stepGate(m, snap({ ...BASE, 'sim/data/rules.json': 'b' }), seed(BASE), 'sim')).toEqual({ run: true, why: 'sim/data/rules.json changed' });
    expect(stepGate(m, snap({ ...BASE, 'tools/mystery/x.ts': 'b' }), seed(BASE), 'late-fight').run).toBe(true);
    expect(stepGate(m, snap(BASE), seed(BASE, []), 'sim')).toEqual({ run: true, why: 'no recorded pass' });
  });
  test('a whole-game step that failed stays open and does not run again until something it reads changes; --full runs it; a pass closes it', () => {
    const open = recordSteps(seed(BASE, ['tp-long']), snap(BASE), [], BUILD, { step: 'tp-long', problem: 'tp-worst: triangles 6.9 M' });
    const host = stepGate(m, snap({ ...BASE, [HOLD]: 'b' }), open, 'tp-long');
    expect(host.run).toBe(false); expect(host.why).toContain('STILL OPEN'); expect(host.why).toContain('tp-worst');
    expect(stepGate(m, snap({ ...BASE, 'sim/data/rules.json': 'b' }), open, 'tp-long').run).toBe(false); // the engine: tp-long's draw counts are the world's (brief 098)
    expect(stepGate(m, snap({ ...BASE, 'out/world/harbour-district.world.json': 'b' }), open, 'tp-long').run).toBe(true);
    expect(stepGate(m, snap(BASE), open, 'tp-long', '--full').run).toBe(true);
    expect(recordSteps(open, snap(BASE), ['tp-long'], BUILD).stepFails).toEqual({});
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
