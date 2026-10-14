import { describe, expect, setDefaultTimeout, test } from 'bun:test';
import { mkdtemp } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import type { Round } from '../../../gen/plugin';
import type { Plan } from '../../../plan/plan';
import { imagePath, sha256 } from '../../../rag/images';
import { root } from '../../../rag/config';
import { toolsHash } from '../../toolhash';
import { applySkyline, kitStatus } from '../apply';
import { BUILD_RULE, checkCandidate, ownersOf, reportText, stationPortalCheck } from '../check';
import { CONCEPTS_FILE, CONCEPTS_PIN, parseConcepts, pinConcepts, tileSet } from '../concepts';
import { GRAMMAR_FILE, sectionsOf } from '../grammar';
import { skylineFiles, skylineHash } from '../hash';
import { skyline } from '../plugin';
import { NO_SKYLINE } from '../schemas';
import { reached } from '../../test/imports';
import { KIT, datum, loadPlan, signature, site, stations, vantage } from './edits';

setDefaultTimeout(120000);
const plan = await loadPlan();
const block = await Bun.file(resolve(root, 'contracts/world/civic-block.json')).json();
const known = {
  tiles: tileSet(parseConcepts(await Bun.file(resolve(root, CONCEPTS_FILE)).text())),
  kit: kitStatus(await Bun.file(KIT).json()),
};

describe('skyline concepts', () => {
  test('the index names sheets that are on disk with the sha256 it records, and tile ids that are unique', async () => {
    const index = parseConcepts(await Bun.file(resolve(root, CONCEPTS_FILE)).text());
    const tiles = index.sheets.flatMap(s => s.tiles);
    expect(new Set(tiles).size).toBe(tiles.length); expect(tiles.length).toBeGreaterThanOrEqual(20);
    for (const s of index.sheets) {
      expect(sha256(await Bun.file(imagePath(s.path)).bytes()), s.path).toBe(s.sha256);
      expect(sha256(await Bun.file(resolve(root, s.provenance)).bytes()), s.provenance).toBe(s.provenanceSha256);
      expect(s.tiles.every(t => t.startsWith(`${s.id}/`)), s.id).toBe(true);
    }
  });

  test('pinning copies the index byte for byte and refuses a sheet that is not the one it names', async () => {
    const dir = await mkdtemp(join(tmpdir(), 'concepts-'));
    const { record } = await pinConcepts(dir);
    expect(await Bun.file(join(dir, CONCEPTS_PIN)).text()).toBe(await Bun.file(resolve(root, CONCEPTS_FILE)).text());
    expect(record.sheets.map(s => s.id)).toEqual(['blocks', 'atomic']);
    const wrong = join(dir, 'wrong.json'), index = parseConcepts(await Bun.file(resolve(root, CONCEPTS_FILE)).text());
    index.sheets[0]!.sha256 = '0'.repeat(64);
    await Bun.write(wrong, JSON.stringify(index));
    await expect(pinConcepts(dir, wrong)).rejects.toThrow('is not the one');
  });
});

describe('the concept index is read when a call is made, not when a replay loads the plug-ins (M3)', () => {
  test("the crew's pictures are a getter: importing the objectives reads nothing", async () => {
    const { objectives } = await import('../objectives');
    for (const member of Object.values(objectives)) {
      const d = Object.getOwnPropertyDescriptor(member, 'images');
      expect(typeof d?.get).toBe('function'); expect(d?.value).toBeUndefined();
    }
  });
  test('the pictures are the sheets of the pinned index when a call asks for them', async () => {
    const { objectives } = await import('../objectives');
    const index = parseConcepts(await Bun.file(resolve(root, CONCEPTS_FILE)).text());
    expect(objectives.datum.images).toEqual(index.sheets.map(s => s.path));
  });
});

describe('skyline grammar and code hash', () => {
  test('sections come out as written, in the order asked', async () => {
    const text = sectionsOf(await Bun.file(resolve(root, GRAMMAR_FILE)).text());
    const heads = text.split('\n').filter(l => l.startsWith('## ')).map(l => l.slice(0, 5));
    expect(heads).toEqual(['## 4.', '## 6.', '## 8.']);
    expect(text).toContain('Crown catalogue');
  });

  test('the code hash covers the checker, the world generator and the apply step, and no test', async () => {
    const files = skylineFiles();
    const needed = [
      'tools/plan/checks.ts', 'tools/levels/apply.ts', 'tools/world/gen/signatures.ts', 'tools/kit/fit.ts',
      'tools/levels/skyline/apply.ts',
    ];
    for (const f of needed) expect(files, f).toContain(f);
    expect(files.filter(f => f.endsWith('.test.ts'))).toEqual([]);
    expect(await skylineHash()).toMatch(/^[0-9a-f]{64}$/);
    expect(await skylineHash()).not.toBe(await toolsHash()); // transit's hash (the A04 runs' ledger) is its own
  });

  test('every file the verdict code imports, however far, is in the hash', async () => {
    const hashed = new Set(skylineFiles());
    const seen = await reached(['candidate', 'known', 'concepts'].map(f => `tools/levels/skyline/${f}.ts`));
    const covered = (f: string) => hashed.has(f) || f.startsWith('tools/plan/') || f.startsWith('tools/world/');
    const missing = [...seen].filter(f => !covered(f));
    expect(missing).toEqual([]);
    expect([...seen].length).toBeGreaterThan(30);
  });
});

describe('skyline checks', () => {
  test('a candidate the generator cannot build is a failed rule with the reason, owned by Datum', async () => {
    const broken = { ...plan, plots: null } as unknown as Plan;
    const c = await checkCandidate(broken, block, { kit: KIT });
    expect(c.ok).toBe(false); expect(c.checks.map(x => x.id)).toEqual([BUILD_RULE]); expect(c.error).toBeTruthy();
    expect(ownersOf(BUILD_RULE)).toEqual(['Datum']);
    expect(reportText(c, ['x'])).toContain('could not be built');
  });

  test("rules go to the owner who can fix them: stations' own to Vantage, shared to both, the rest to Datum", () => {
    expect(ownersOf('S-S2')).toEqual(['Vantage']); expect(ownersOf('N-P2')).toEqual(['Vantage']);
    for (const r of ['N-R1', 'L-5', 'L-7', 'SF-13', 'W-2', 'W-3', 'SK-3']) expect(ownersOf(r), r).toEqual(['Datum', 'Vantage']);
    for (const r of ['SG-1', 'SK-1', 'SK-2', 'P-1', 'S-C1', 'B-3', 'MS-2']) expect(ownersOf(r), r).toEqual(['Datum']);
  });

  test('the critic reads failed rules and the skyline groups in full, other passes by id', async () => {
    const c = await checkCandidate(plan, block, { kit: KIT }), text = reportText(c, []);
    expect(c.ok).toBe(true);
    expect(text).toContain('PASS SK-1 '); expect(text).toContain('PASS P-1 ');
    expect(text).toMatch(/PASS \d+ more rules: .*\bB-1\b/);
    expect(text.length).toBeLessThan(20000);
  });
});

describe('skyline hooks', () => {
  const NONE = undefined as never; // the hooks under test read no state
  const round = (outs: Round['outs']) => ({ n: 1, outs } as Round);
  test('retrieval for Vantage and Keel names what Datum proposed; Datum asks nothing extra', () => {
    const d = datum({
      signatures: [signature(plan, 'ocean-liner')], towerSites: [site(plan, 'crescent-terrace')],
    });
    expect(skyline.query!(NONE, round({ datum: d }), 'datum')).toEqual([]);
    for (const key of ['vantage', 'keel']) {
      expect(skyline.query!(NONE, round({ datum: d }), key)).toEqual([
        'signature ocean-liner plan', 'tower site crescent-terrace plan',
      ]);
    }
    expect(skyline.query!(NONE, round({ datum: NO_SKYLINE }), 'keel')).toEqual([]);
  });

  test('a preview answers for Datum and Vantage with empty proposals and for nobody else', () => {
    expect(skyline.standIn!(NONE, 'datum')).toBe(NO_SKYLINE);
    expect(skyline.standIn!(NONE, 'vantage')).toMatchObject({ stops: [] });
    expect(skyline.standIn!(NONE, 'keel')).toBeUndefined();
  });
});

describe('a station label (SK-3, brief 007 F)', () => {
  // `in` names the structure a station stands inside, and the world builds the station from it (decks only, the structure
  // is the roof). The Gardens station (x -160, open air over Circus Road) labelled inside SpaceHQ (ground x 56-136) used to
  // pass every rule: it always has a way to the street, so the label could be false. SK-3 compares the label with the
  // structure's declared portal on the station's line.
  const labelled = (id: string, named: string) =>
    stations(plan).map(s => (s.id === id ? { ...s, in: named as 'signatures/space-hq' } : s));
  const check = async (stops: ReturnType<typeof stations>) => {
    const a = applySkyline(plan, { datum: datum({}), vantage: vantage({ stops }) }, '0.3.0-test.1', known);
    expect(a.problems).toEqual([]);
    return checkCandidate(a.plan, block, { kit: KIT });
  };

  test('Gardens labelled "inside SpaceHQ" fails SK-3, and nothing else', async () => {
    const c = await check(labelled('gardens', 'signatures/space-hq'));
    expect(c.checks.filter(x => !x.ok).map(x => x.id)).toEqual(['SK-3']);
    expect(c.checks.find(x => x.id === 'SK-3')!.detail).toContain('S1/gardens at -160, 0 is labelled in signatures/space-hq, outside its portal');
    expect(ownersOf('SK-3')).toEqual(['Datum', 'Vantage']);
    expect(c.checks.filter(x => ['L-5', 'SF-13', 'W-2'].includes(x.id)).every(x => x.ok)).toBe(true); // the holes it slipped through
  });

  test('Harbour labelled "inside the Skyport tower" fails SK-3; the plan\'s own labels pass', async () => {
    const c = await check(labelled('harbour', 'megastructures/skyport-tower'));
    expect(c.checks.find(x => x.id === 'SK-3')!.detail).toContain('S1/harbour at 340, 0 is labelled in megastructures/skyport-tower');
    const own = (await checkCandidate(plan, block, { kit: KIT })).checks.find(x => x.id === 'SK-3')!;
    expect([own.ok, own.value]).toEqual([true, '2/2']);
  });

  test('a station inside a structure that declares no portal on its line fails SK-3', () => {
    const bad = structuredClone(plan);
    (bad.skyrail[0]!.stations![0] as { in?: string }).in = 'megastructures/no-such-tower';
    expect(stationPortalCheck(bad).detail).toContain('which declares no portal on S1/trunk');
  });
});
