// bun run shots:shrink (brief 034): what it finds, what it leaves alone and why, the estimate, and (with macOS sips) that shrinking a file really makes it 1920 wide,
// keeps its modified time, and that jpg replaces the PNG. Nothing here touches a real screenshot: the files are made in a temp folder.
import { describe, expect, test } from 'bun:test';
import { existsSync, mkdirSync, mkdtempSync, statSync, utimesSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { encodePng } from '../hud-budget/png';
import { pngFileSize } from './shots';
import { findPngs, plan, report, shrinkOne } from './shrink';

const make = (file: string, w: number, h: number) => {
  const data = new Uint8Array(w * h * 4); for (let i = 0; i < data.length; i += 4) { data[i] = (i / 4) % 251; data[i + 1] = ((i / 4) >> 3) % 241; data[i + 2] = 90; data[i + 3] = 255; }
  mkdirSync(join(file, '..'), { recursive: true }); writeFileSync(file, encodePng({ w, h, data }));
};
const dir = mkdtempSync(join(tmpdir(), 'shrink-'));
make(join(dir, 'wide.png'), 2400, 1350);
make(join(dir, 'small.png'), 1920, 1080);
make(join(dir, 'tracked.png'), 2400, 1350);
make(join(dir, 'set.png'), 2400, 1350); make(join(dir, 'set.hud.png'), 2400, 1350); writeFileSync(join(dir, 'set.manifest.json'), '{}');
make(join(dir, 'atlas.png'), 2048, 2048);
make(join(dir, 'Textures/skin.png'), 2400, 1350); // a skipped folder

describe('what is shrunk and what is left alone', () => {
  const found = findPngs([dir], dir), p = plan(found, { maxW: 1920, tracked: new Set(['tracked.png']), jpg: false, base: dir });
  test('finds the PNGs with their sizes, not the skipped folder', () => {
    expect(found.map(f => f.file.replace(dir + '/', '')).sort()).toEqual(['atlas.png', 'set.hud.png', 'set.png', 'small.png', 'tracked.png', 'wide.png']);
    expect(found.find(f => f.file.endsWith('wide.png'))).toMatchObject({ w: 2400, h: 1350 });
  });
  test('a wide screenshot is shrunk; the rest are left, each for its reason', () => {
    expect(p.todo.map(t => t.file.replace(dir + '/', ''))).toEqual(['wide.png']);
    expect(p.todo[0]).toMatchObject({ newW: 1920, newH: 1080 });
    expect(p.kept.map(k => `${k.file.replace(dir + '/', '')}: ${k.why}`).sort()).toEqual(['set.hud.png: budget layer', 'set.png: budget set', 'tracked.png: committed']);
    expect(p.small).toBe(1); expect(p.other).toBe(1); // small.png; the square atlas
  });
  test('the report says what it would save, and that nothing changed is the tool\'s to say', () => {
    const t = report(p, { list: true, jpg: false, maxW: 1920, base: dir });
    expect(t).toContain('1 PNGs wider than 1920'); expect(t).toContain('2400x1350 → 1920x1080'); expect(t).toContain('1 committed'); expect(t).toContain('1 HUD layers and 1 shots that have one');
  });
});

describe.skipIf(!Bun.which('sips'))('shrinking for real (sips)', () => {
  test('a PNG becomes 1920 wide, smaller, with its modified time kept', () => {
    const f = join(dir, 'real.png'); make(f, 2400, 1350); const old = new Date(2024, 1, 2); utimesSync(f, old, old);
    const t = plan(findPngs([dir], dir).filter(x => x.file === f), { maxW: 1920, tracked: new Set(), jpg: false, base: dir }).todo[0]!;
    const before = statSync(f).size, after = shrinkOne(t, false);
    expect(pngFileSize(f)).toEqual([1920, 1080]); expect(after).toBeLessThan(before); expect(statSync(f).mtime.getTime()).toBe(old.getTime());
  });
  test('jpg replaces the PNG with a JPEG', () => {
    const f = join(dir, 'to-jpg.png'); make(f, 2400, 1350);
    const t = plan(findPngs([dir], dir).filter(x => x.file === f), { maxW: 1920, tracked: new Set(), jpg: true, base: dir }).todo[0]!;
    shrinkOne(t, true);
    expect(existsSync(f)).toBe(false); expect(existsSync(join(dir, 'to-jpg.jpg'))).toBe(true);
  });
  test('a file sips cannot read is an error, and the original is left', () => {
    const f = join(dir, 'broken.png'); const b = new Uint8Array(40); b.set([0x89, 0x50, 0x4e, 0x47, 13, 10, 26, 10, 0, 0, 0, 13, 0x49, 0x48, 0x44, 0x52]); new DataView(b.buffer).setUint32(16, 2400); new DataView(b.buffer).setUint32(20, 1350); writeFileSync(f, b);
    const t = plan(findPngs([dir], dir).filter(x => x.file === f), { maxW: 1920, tracked: new Set(), jpg: false, base: dir }).todo[0]!;
    expect(() => shrinkOne(t, false)).toThrow(/sips failed/); expect(statSync(f).size).toBe(40);
  });
});
