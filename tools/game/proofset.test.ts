// The proof set (tools/game/proofset.ts): the r059 and r067 pictures are in the HUD budget and the 2x crops (briefs/067/splice.md item 3 and follow-up 2). Each rule has a failing example.
// Run: bun test tools/game/proofset.test.ts
import { describe, expect, test } from 'bun:test';
import { existsSync, mkdirSync, mkdtempSync, readFileSync, readdirSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { encodePng } from '../hud-budget/png';
import { cropRects, downscale, faults, judgeProofBudget, loadProofSet, locate, pickSets, stageForBudget, STANDING, writeCrops, type BudgetRun } from './proofset';

const root = join(import.meta.dir, '..', '..'), read = (rel: string) => readFileSync(join(root, rel), 'utf8');
const live = loadProofSet(), r059 = live.sets.r059!;

/** A tiny proof folder: each named picture a flat PNG of `size` with the layers beside it (all, or what `skip` leaves out). */
function proof(names: string[], o: { size?: [number, number]; skip?: Record<string, string[]>; parts?: Record<string, number[]> } = {}): string {
  const dir = mkdtempSync(join(tmpdir(), 'proofset-')), [w, h] = o.size ?? [192, 108];
  const png = encodePng({ w, h, data: new Uint8Array(w * h * 4).fill(180) });
  for (const n of names) {
    const skip = o.skip?.[n] ?? [];
    writeFileSync(join(dir, `${n}.png`), png);
    if (!skip.includes('hud')) writeFileSync(join(dir, `${n}.hud.png`), png);
    if (!skip.includes('manifest')) writeFileSync(join(dir, `${n}.manifest.json`), JSON.stringify({ state: 'active', elements: [] }));
    writeFileSync(join(dir, `${n}.parts.json`), JSON.stringify(o.parts ?? { roster: [1, 1, 10, 40], pill: [100, 4, 60, 10] }));
  }
  return dir;
}
const must = r059.frames.filter(f => !f.optional).map(f => f.name), all = r059.frames.map(f => f.name);

describe('the set file', () => {
  test('r059 names the fourteen pictures of Proof059, one of them optional, none twice, each with a line saying what it shows', () => {
    expect(r059.section).toBe('r059');
    expect(all.length).toBe(14);
    expect(new Set(all).size).toBe(14);
    expect(r059.frames.filter(f => f.optional).map(f => f.name)).toEqual(['r059-fight-start']);
    for (const f of r059.frames) expect(f.what.length, f.name).toBeGreaterThan(20);
  });

  test('every picture is one Proof059.cs takes, and every r059 picture it takes is in the set (a renamed picture is a failing test, not one quietly dropped from the budget)', () => {
    const cs = read(r059.source), taken = new Set([...cs.matchAll(/"(r059-[a-z0-9-]+)"/g)].map(m => m[1]!));
    expect([...taken].sort()).toEqual([...all].sort());
  });

  test('a duplicate picture, an unknown key or a bad name is refused with the file named', () => {
    const dir = mkdtempSync(join(tmpdir(), 'proofset-file-'));
    const write = (j: unknown) => { const f = join(dir, 'p.json'); writeFileSync(f, JSON.stringify(j)); return f; };
    const f = { name: 'r059-a', what: 'a picture of something' };
    expect(() => loadProofSet(write({ schema: 'suborned.proofset/1', note: 'n', sets: { x: { section: 'x', source: 's', frames: [f, f] } } }))).toThrow(/lists r059-a twice/);
    expect(() => loadProofSet(write({ schema: 'suborned.proofset/1', note: 'n', sets: { x: { section: 'x', source: 's', frames: [{ ...f, surprise: 1 }] } } }))).toThrow(/p\.json/);
    expect(() => loadProofSet(write({ schema: 'suborned.proofset/1', note: 'n', sets: { x: { section: 'x', source: 's', frames: [{ ...f, name: 'R059 A' }] } } }))).toThrow(/kebab-case/);
    rmSync(dir, { recursive: true, force: true });
  });
});

describe('locate', () => {
  test('a whole proof has every picture with its layers at the size asked for; the optional one may be absent', () => {
    const dir = proof(must);
    try { const j = locate(dir, r059.frames, [192, 108]); expect(faults(j)).toEqual([]); expect(j.found.length).toBe(13); expect(j.optionalMissing).toEqual(['r059-fight-start']); } finally { rmSync(dir, { recursive: true, force: true }); }
  });

  test('a picture the proof did not take, one with no HUD layer, one with no manifest and one at the wrong size are each a fault with their name', () => {
    const dir = proof(must.slice(1), { skip: { [must[1]!]: ['hud'], [must[2]!]: ['manifest'] } });
    try {
      const f = faults(locate(dir, r059.frames, [1920, 1080])).join('\n');
      expect(f).toContain(`${must[0]}: not taken by the proof`);
      expect(f).toContain(`${must[1]}: no .hud.png beside it`);
      expect(f).toContain(`${must[2]}: no .manifest.json beside it`);
      expect(f).toContain(`${must[3]}: 192x108, wanted 1920x1080`);
    } finally { rmSync(dir, { recursive: true, force: true }); }
  });
});

describe('the HUD budget of the set (the batch\'s budget step)', () => {
  const pass = (names: string[]): BudgetRun => () => ({ code: 0, out: [...names.map(n => `PASS  ${n}.png`), `${names.length}/${names.length} within budget`].join('\n') });

  test('judged at 1920x1080 through tools/hud-budget over a folder of the pictures, their HUD layers and manifests, and nothing else', () => {
    const dir = proof(must, { size: [1920, 1080] }), stage = mkdtempSync(join(tmpdir(), 'stage-'));
    try {
      let seen: string[] = [];
      const r = judgeProofBudget(dir, 'r059', stage, d => { seen = readdirSync(d).sort(); return pass(must)(d); });
      expect(r.ok).toBe(true);
      expect(r.judged).toBe(13); expect(r.pass).toBe(13);
      expect(r.detail).toContain('13 of 13 pictures within budget');
      expect(r.detail).toContain('not taken this run: r059-fight-start');
      expect(seen.length).toBe(13 * 3);
      expect(seen.every(f => /\.(png|json)$/.test(f) && !f.endsWith('.parts.json'))).toBe(true);
    } finally { rmSync(dir, { recursive: true, force: true }); rmSync(stage, { recursive: true, force: true }); }
  });

  test('a picture over budget fails the set and names it with the tool\'s own words', () => {
    const dir = proof(must, { size: [1920, 1080] }), stage = mkdtempSync(join(tmpdir(), 'stage-'));
    try {
      const r = judgeProofBudget(dir, 'r059', stage, () => ({ code: 1, out: `PASS  a.png\nFAIL  ${must[3]}.png  active HUD 24.1% of the screen (limit 22%)\n      - HUD 24.1% is over 22%` }));
      expect(r.ok).toBe(false);
      expect(r.problems.join('\n')).toContain(`FAIL  ${must[3]}.png`);
    } finally { rmSync(dir, { recursive: true, force: true }); rmSync(stage, { recursive: true, force: true }); }
  });

  test('a tool that dies with no FAIL line is a failure too, not a pass', () => {
    const dir = proof(must, { size: [1920, 1080] }), stage = mkdtempSync(join(tmpdir(), 'stage-'));
    try { const r = judgeProofBudget(dir, 'r059', stage, () => ({ code: 2, out: 'no .png screenshots in x' })); expect(r.ok).toBe(false); expect(r.problems[0]).toContain('exited 2'); } finally { rmSync(dir, { recursive: true, force: true }); rmSync(stage, { recursive: true, force: true }); }
  });

  test('a missing picture, or a 2x one, is refused before the tool runs (the 1x budget does not read 2160 pictures)', () => {
    const stage = mkdtempSync(join(tmpdir(), 'stage-')), dir = proof(must.slice(1), { size: [1920, 1080] }), big = proof(must, { size: [3840, 2160] });
    try {
      let ran = false; const run: BudgetRun = () => { ran = true; return { code: 0, out: '' }; };
      const a = judgeProofBudget(dir, 'r059', stage, run); expect(a.ok).toBe(false); expect(a.problems.join()).toContain('not taken by the proof');
      const b = judgeProofBudget(big, 'r059', stage, run); expect(b.ok).toBe(false); expect(b.problems.join()).toContain('wanted 1920x1080');
      expect(ran).toBe(false);
    } finally { rmSync(dir, { recursive: true, force: true }); rmSync(big, { recursive: true, force: true }); rmSync(stage, { recursive: true, force: true }); }
  });

  test('the real tool judges a staged set: the same files a proof writes pass for a bare picture with a pill, and the stage is rebuilt each time', () => {
    const dir = proof(must, { size: [1920, 1080], parts: { pill: [1422, 14, 484, 44] } }), stage = mkdtempSync(join(tmpdir(), 'stage-'));
    try {
      writeFileSync(join(stage, 'stale.png'), 'x');
      stageForBudget(locate(dir, r059.frames).found, stage);
      expect(existsSync(join(stage, 'stale.png'))).toBe(false);
      expect(readdirSync(stage).length).toBe(13 * 3);
    } finally { rmSync(dir, { recursive: true, force: true }); rmSync(stage, { recursive: true, force: true }); }
  });
});

describe('the 2x crops', () => {
  test('cropRects: the middle of the picture first, then each part that is not standing furniture, padded and held to the picture', () => {
    const parts = { roster: [28, 28, 172, 831], 'portrait-1': [52, 69, 123, 123], scope: [28, 1868, 424, 264], pill: [2915, 28, 897, 88], 'window-mark-1': [736, 70, 148, 76], 'bubble-1': [246, -194, 450, 50], 'dot': [10, 10, 4, 4], 'edge-cameo-1': [3800, 2100, 80, 80] };
    const r = cropRects(parts, [3840, 2160]);
    expect(r.map(c => c.name)).toEqual(['middle', 'pill', 'window-mark-1', 'edge-cameo-1']);
    expect(r[0]!.rect).toEqual({ x: 1280, y: 720, w: 1280, h: 720 });
    expect(r[1]!.rect).toEqual({ x: 2891, y: 4, w: 945, h: 136 }); // pad 24 each side: x 2915-24, y 28-24
    for (const c of r) { expect(c.rect.x + c.rect.w).toBeLessThanOrEqual(3840); expect(c.rect.y + c.rect.h).toBeLessThanOrEqual(2160); expect(c.rect.x).toBeGreaterThanOrEqual(0); }
    for (const s of ['roster', 'portrait-4', 'roster-weapon-2', 'roster-dots-3', 'keys', 'scope', 'history', 'history-key']) expect(STANDING.test(s), s).toBe(true);
    for (const s of ['pill', 'gain-chip-1', 'edge-cameo-1', 'timer-3', 'window-mark-2', 'pip-guard']) expect(STANDING.test(s), s).toBe(false);
  });

  test('writeCrops cuts a proof of the set into PNGs and one sheet that shows them, naming each picture, what it shows and its state', () => {
    const dir = proof(must, { size: [384, 216], parts: { pill: [284, 2, 96, 9], roster: [2, 2, 18, 80] } }), out = mkdtempSync(join(tmpdir(), 'crops-'));
    try {
      const r = writeCrops(dir, 'r059', out, { size: [384, 216] });
      expect(r.problems).toEqual([]);
      expect(r.frames).toBe(13); expect(r.crops).toBe(39); // the whole picture at a quarter, the middle and the pill of each
      const html = readFileSync(r.sheet, 'utf8');
      expect(html).toContain('r059-235-turns-in'); expect(html).toContain('the rival car at its entry'); expect(html).toContain('<small>active</small>');
      for (const m of html.matchAll(/src="([^"]+)"/g)) { expect(m[1]).toMatch(/^r059-[a-z0-9-]+--[a-z0-9-]+\.png$/); expect(existsSync(join(out, m[1]!))).toBe(true); } // each image is a PNG beside the sheet, by name: no path, no web reference
      expect(existsSync(join(out, 'r059-235-turns-in--pill.png'))).toBe(true);
      expect(existsSync(join(out, 'r059-235-turns-in--frame.png'))).toBe(true);
    } finally { rmSync(dir, { recursive: true, force: true }); rmSync(out, { recursive: true, force: true }); }
  });

  test('downscale: a quarter-size thumbnail is the mean of each 4x4 block', () => {
    const data = new Uint8Array(8 * 8 * 4); for (let i = 0; i < 64; i++) { const v = i % 8 < 4 ? 0 : 200; data.set([v, v, v, 255], i * 4); }
    const t = downscale({ w: 8, h: 8, data }, 4);
    expect([t.w, t.h]).toEqual([2, 2]);
    expect([...t.data.subarray(0, 4)]).toEqual([0, 0, 0, 255]); expect([...t.data.subarray(4, 8)]).toEqual([200, 200, 200, 255]);
  });

  test('the 2160 set is held to 3840x2160: a 1080p proof is a fault on the sheet', () => {
    const dir = proof(must, { size: [192, 108] }), out = mkdtempSync(join(tmpdir(), 'crops-'));
    try { const r = writeCrops(dir, 'r059', out); expect(r.problems.join()).toContain('wanted 3840x2160'); mkdirSync(out, { recursive: true }); } finally { rmSync(dir, { recursive: true, force: true }); rmSync(out, { recursive: true, force: true }); }
  });
});

// ---------------------------------------------------------------- r067 (Proof067.cs, Sky's notes on 0.2.122)

const r067 = live.sets.r067!, all067 = r067.frames.map(f => f.name), must067 = r067.frames.filter(f => !f.optional).map(f => f.name);

describe('the r067 set', () => {
  test('names the thirty-two pictures of Proof067 (engine 0.44.0: one Help on Inez, so one convo-inez picture), two of them optional (the crowd and a second rival decide), none twice, each with a line saying what it shows', () => {
    expect(r067.section).toBe('r067');
    expect(all067.length).toBe(32);
    expect(new Set(all067).size).toBe(32);
    expect(r067.frames.filter(f => f.optional).map(f => f.name)).toEqual(['r067-stack-tab', 'r067-reticle-handoff']);
    for (const f of r067.frames) expect(f.what.length, f.name).toBeGreaterThan(20);
  });

  test('every picture is one Proof067.cs takes, and every r067 picture it takes is in the set (the third-person pair is named from its loop\'s pass: -1 and -2)', () => {
    const cs = read(r067.source), literal = [...cs.matchAll(/"(r067-[a-z0-9-]+)"/g)].map(m => m[1]!);
    const looped = [...cs.matchAll(/\$"(r067-[a-z0-9-]+-)\{pass \+ 1\}"/g)].flatMap(m => [1, 2].map(n => `${m[1]}${n}`));
    expect(cs).toContain('for (int pass = 0; pass < 2; pass++)'); // the pair is two passes; a third pass would be a third picture the set must name
    expect(looped).toEqual(['r067-tp-entry-1', 'r067-tp-entry-2']);
    expect([...new Set([...literal, ...looped])].sort()).toEqual([...all067].sort());
  });

  test('the two optional pictures are the two Proof067.cs takes only when the scene decides (the crowd at the door, a second rival in view)', () => {
    const cs = read(r067.source);
    expect(cs).toMatch(/if \(h\.HoverStackCount >= 2\) yield return Shoot\(h, dir, "r067-stack-tab"/); // "logged, not gated: the crowd decides"
    expect(cs).toMatch(/if \(r2\.HasValue\) \{.*"r067-reticle-handoff"/); // a second rival in view
  });

  test('crop names a part the proof writes (the roster) and only the pictures of the portrait pulse, the selection and the boarded squad take it', () => {
    expect(r067.frames.filter(f => f.crop).map(f => [f.name, f.crop])).toEqual([['r067-portrait-pulse', ['roster']], ['r067-portrait-framed', ['roster']], ['r067-board-pending', ['roster']], ['r067-board-landed', ['roster']]]);
    expect(live.sets.r059!.frames.some(f => f.crop)).toBe(false);
    expect(STANDING.test('roster')).toBe(true); // standing furniture, so left out of every other picture's crops
  });

  test('a crop that is empty, not a list of names or not a key of the schema is refused with the file named', () => {
    const dir = mkdtempSync(join(tmpdir(), 'proofset-file-')), f = join(dir, 'p.json');
    const load = (frame: object) => { writeFileSync(f, JSON.stringify({ schema: 'suborned.proofset/1', note: 'n', sets: { x: { section: 'x', source: 's', frames: [{ name: 'r067-a', what: 'a picture of something', ...frame }] } } })); return () => loadProofSet(f); };
    expect(load({ crop: ['roster'] })).not.toThrow();
    expect(load({ crop: [] })).toThrow(/p\.json/);
    expect(load({ crop: 'roster' })).toThrow(/p\.json/);
    expect(load({ crop: [1] })).toThrow(/p\.json/);
    expect(load({ cropped: ['roster'] })).toThrow(/p\.json/);
    rmSync(dir, { recursive: true, force: true });
  });
});

describe('every set judges the same way (r059, r067 and r123)', () => {
  for (const [name, set] of Object.entries(live.sets)) {
    const must = set.frames.filter(f => !f.optional).map(f => f.name), optional = set.frames.filter(f => f.optional).map(f => f.name);
    const pass = (names: string[]): BudgetRun => () => ({ code: 0, out: [...names.map(n => `PASS  ${n}.png`), `${names.length}/${names.length} within budget`].join('\n') });

    test(`${name}: a whole proof is judged at 1920x1080 over its pictures, their HUD layers and manifests, and passes; the pictures the run did not take are said`, () => {
      const dir = proof(must, { size: [1920, 1080] }), stage = mkdtempSync(join(tmpdir(), 'stage-'));
      try {
        let seen: string[] = [];
        const r = judgeProofBudget(dir, name, stage, d => { seen = readdirSync(d).sort(); return pass(must)(d); });
        expect(r.ok).toBe(true);
        expect(r.judged).toBe(must.length); expect(r.pass).toBe(must.length);
        expect(r.detail).toContain(`${must.length} of ${must.length} pictures within budget`);
        for (const o of optional) expect(r.detail).toContain(o);
        expect(seen.length).toBe(must.length * 3);
      } finally { rmSync(dir, { recursive: true, force: true }); rmSync(stage, { recursive: true, force: true }); }
    });

    test(`${name}: a picture the proof did not take, and one over budget, each fail the set with the picture named`, () => {
      const stage = mkdtempSync(join(tmpdir(), 'stage-')), dir = proof(must.slice(1), { size: [1920, 1080] }), whole = proof(must, { size: [1920, 1080] });
      try {
        let ran = false;
        const a = judgeProofBudget(dir, name, stage, () => { ran = true; return { code: 0, out: '' }; });
        expect(a.ok).toBe(false); expect(a.problems).toContain(`${must[0]}: not taken by the proof`); expect(ran).toBe(false);
        const b = judgeProofBudget(whole, name, stage, () => ({ code: 1, out: `FAIL  ${must[2]}.png  active HUD 24.1% of the screen (limit 22%)` }));
        expect(b.ok).toBe(false); expect(b.problems.join('\n')).toContain(`FAIL  ${must[2]}.png`);
      } finally { rmSync(dir, { recursive: true, force: true }); rmSync(whole, { recursive: true, force: true }); rmSync(stage, { recursive: true, force: true }); }
    });

    test(`${name}: the 2160 proof is cut into crops, one sheet names each picture and what it shows, and the pictures of a standing part have it cropped too`, () => {
      const dir = proof(must, { size: [384, 216], parts: { pill: [284, 2, 96, 9], roster: [2, 2, 18, 80] } }), out = mkdtempSync(join(tmpdir(), 'crops-'));
      try {
        const r = writeCrops(dir, name, out, { size: [384, 216] });
        expect(r.problems).toEqual([]);
        expect(r.frames).toBe(must.length);
        expect(r.crops).toBe(set.frames.filter(f => !f.optional).reduce((n, f) => n + 3 + (f.crop?.includes('roster') ? 1 : 0), 0)); // the picture at a quarter, its middle and its pill; the roster where the picture is of it
        const html = readFileSync(r.sheet, 'utf8');
        expect(html).toContain(`<title>2x crops · ${name}</title>`);
        for (const f of set.frames.filter(x => !x.optional)) { expect(html).toContain(f.name); expect(existsSync(join(out, `${f.name}--frame.png`)), f.name).toBe(true); expect(existsSync(join(out, `${f.name}--roster.png`)), f.name).toBe(!!f.crop?.includes('roster')); }
      } finally { rmSync(dir, { recursive: true, force: true }); rmSync(out, { recursive: true, force: true }); }
    });
  }
});

describe('cropRects: standing parts a picture is of', () => {
  const parts = { roster: [28, 28, 172, 831], 'portrait-2': [52, 69, 123, 123], scope: [28, 1868, 424, 264], pill: [2915, 28, 897, 88] };
  test('without `also` the standing parts are left out; with it the named ones are cropped, padded, and the others stay out', () => {
    expect(cropRects(parts, [3840, 2160]).map(c => c.name)).toEqual(['middle', 'pill']);
    expect(cropRects(parts, [3840, 2160], { also: ['roster'] }).map(c => c.name)).toEqual(['middle', 'roster', 'pill']);
    expect(cropRects(parts, [3840, 2160], { also: ['roster'] })[1]!.rect).toEqual({ x: 4, y: 4, w: 220, h: 879 }); // pad 24 each side, held to the picture
    expect(cropRects(parts, [3840, 2160], { also: ['keys'] }).map(c => c.name)).toEqual(['middle', 'pill']); // a part the picture lacks adds nothing
  });
});

describe('pickSets: which sets the command runs on a folder', () => {
  const folder = (names: string[]) => proof(names, { size: [192, 108] });
  test('every set the folder has a picture of, the others said as an older build\'s; a named set runs even when the folder lacks it (its pictures are then faults)', () => {
    const only67 = folder(must067.slice(0, 2)), both = folder([...must.slice(0, 1), ...must067.slice(0, 1)]), none = folder([]);
    try {
      expect(pickSets(only67)).toEqual({ run: ['r067'], skipped: ['r059', 'r123', 'r083'] });
      expect(pickSets(both)).toEqual({ run: ['r059', 'r067'], skipped: ['r123', 'r083'] });
      expect(pickSets(none)).toEqual({ run: [], skipped: ['r059', 'r067', 'r123', 'r083'] });
      expect(pickSets(none, 'r067')).toEqual({ run: ['r067'], skipped: [] });
      expect(() => pickSets(none, 'r118')).toThrow(/no set r118 in .*proofset\.json \(r059, r067, r123, r083\)/);
    } finally { for (const d of [only67, both, none]) rmSync(d, { recursive: true, force: true }); }
  });
});

describe('r123 (batch 0.2.123 host round 4): in the budget and the crops, not gated', () => {
  const r123 = live.sets.r123!, names = r123.frames.map(f => f.name);
  test('the seven pictures of Proof123, each one the source takes, and the source takes no other r123 picture', () => {
    expect(r123.section).toBe('r123'); expect(names).toEqual(['r123-looks-310', 'r123-looks-320', 'r123-fight-safe', 'r123-fight-loud', 'r123-fight-ground', 'r123-body-safe', 'r123-body-cover']);
    const cs = read(r123.source), taken = new Set([...cs.matchAll(/"(r123-[a-z0-9-]+)"/g)].map(m => m[1]!));
    expect([...taken].sort()).toEqual([...names].sort());
  });
  test('its section is not in the proof gates until Glyph agrees its density (Aster, 2026-10-26): the budget judges the pictures, the gate does not read r123', () => {
    expect(read('tools/game/proofgates.ts')).not.toMatch(/GATED = \[[^\]]*'r123'/);
    expect(live.note).toContain('NOT gated');
  });
  test('staged for the budget, a picture\'s refusal pops (its parts, not its manifest) are added to its manifest as `pop` elements, and the stage is still three files a picture', () => {
    const dir = proof(['r123-fight-loud'], { size: [192, 108], parts: { roster: [1, 1, 10, 40], 'pop-1': [20, 20, 50, 12], 'pop-2': [20, 20, 50, 12], 'pop-3': [20, -40, 50, 12] } }), stage = mkdtempSync(join(tmpdir(), 'stage-'));
    try {
      const j = locate(dir, r123.frames.filter(f => f.name === 'r123-fight-loud'));
      stageForBudget(j.found, stage);
      expect(readdirSync(stage).sort()).toEqual(['r123-fight-loud.hud.png', 'r123-fight-loud.manifest.json', 'r123-fight-loud.png']);
      const m = JSON.parse(readFileSync(join(stage, 'r123-fight-loud.manifest.json'), 'utf8'));
      expect(m.elements.filter((e: { kind: string }) => e.kind === 'pop')).toEqual([{ kind: 'pop', x: 20, y: 20, w: 50, h: 12 }, { kind: 'pop', x: 20, y: 20, w: 50, h: 12 }]); // the one above the picture is not on show
    } finally { rmSync(dir, { recursive: true, force: true }); rmSync(stage, { recursive: true, force: true }); }
  });
});
