// The briefing page's checks (state `page`, page.ts; Glyph's briefs/070/glyph-page-budget.md). Each rule has a failing example. A drawn page proves the rules one at a time;
// fixtures/page-130.* (build 0.2.130's briefing-day as shot, its photographs flattened) proves the verdict on the real page.
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { analyse, LIMIT, READING, type Element, type Manifest } from './budget';
import { lastRowWords, PAGE, pageParts, type PagePart } from './page';
import { decodePng, type Image } from './png';

const fx = (f: string) => join(import.meta.dir, 'fixtures', f);
type El = Element & { part?: PagePart };
const t = (part: PagePart, text: string, x: number, y: number, w: number, h: number, more: Partial<El> = {}): El => ({ kind: 'hud', part, text, x, y, w, h, ...more });
const img = (part: PagePart, text: string, x: number, y: number, w: number, h: number): El => ({ kind: 'image', part, text, x, y, w, h });
const pin = (n: number | string, x: number, y: number): El => ({ kind: 'pin', part: 'pin', n, x, y, w: 20, h: 20 });

/** A page laid out like 0.2.130's, every piece tagged and within Glyph's limits. */
function page(): El[] {
  const els: El[] = [
    { kind: 'paper', part: 'paper', x: 0, y: 0, w: 1920, h: 1080 },
    t('title', 'The Handover', 56, 52, 176, 22), t('hook', 'Vale has a file on you.', 56, 80, 471, 60), t('objective', 'Take him alive before it reaches Security.', 56, 146, 395, 30),
    t('place', 'Civic Records Office · Evening, 5:30', 1415, 52, 449, 22), t('heading', 'Target', 56, 200, 68, 14),
    img('target-photo', 'target photograph', 56, 222, 400, 330), t('target-name', 'Councillor Vale', 56, 568, 212, 40),
    t('target-fact', 'Holds the file on you.', 56, 614, 300, 24), t('target-fact', 'Comes out at 5:50 and leaves by car.', 56, 644, 300, 24),
    img('plan', 'plan', 56, 700, 400, 140), t('plan-label', 'Corner café', 76, 712, 70, 14), t('plan-label', 'Records Office', 300, 712, 90, 14),
    pin(1, 110, 770), pin(2, 340, 740), pin(3, 380, 770), pin(4, 340, 810), pin('car', 170, 740),
    pin(1, 56, 862), t('legend', 'Inez', 86, 862, 40, 20), pin(2, 256, 862), t('legend', 'The guard', 286, 862, 70, 20),
    pin(3, 56, 888), t('legend', 'The driver', 86, 888, 70, 20), pin(4, 256, 888), t('legend', 'Vale\'s car', 286, 888, 70, 20),
    pin('car', 56, 914), t('legend', 'The getaway car', 86, 914, 110, 20),
  ];
  const way = (y: number, name: string, when: string, lines: string[], n: number, caps: [string, string]) => {
    els.push(t('way-name', name, 500, y, 128, 38), t('way-when', when, 500, y + 44, 111, 30));
    lines.forEach((l, i) => els.push(t('way-text', l, 500, y + 90 + i * 34, 262, 25)));
    [784, 1188].forEach((x, i) => els.push(img('still', `still ${caps[i]}`, x, y + 6, 372, 238), pin(n + i, x + 10, y + 16), t('caption', caps[i]!, x + 6, y + 252, 120, 22)));
  };
  way(228, 'Front door', 'Before 5:50', ['Inez and the door guard are close.', 'Turn her to call him off the door.'], 1, ['Inez', 'The guard']);
  way(528, 'Pickup', 'From 6:10', ['His driver is short of cash.', 'Turn him before 5:40.'], 3, ['The driver', 'Vale\'s car']);
  els.push(img('still', 'still ending-getaway', 1584, 234, 286, 482), pin('car', 1594, 244), t('caption', 'The getaway car', 1590, 730, 114, 22),
    t('ending', 'Both ways end here: Vale in the getaway car as it leaves.', 1590, 756, 274, 44, { rows: ['Both ways end here: Vale in the getaway', 'car as it leaves.'] }));
  [['2:35', 'A rival car turns in'], ['5:30', 'Handover chime'], ['5:40', 'Last chance to turn the driver'], ['5:50', 'Vale leaves the Office'], ['6:10', 'Vale at his car']].forEach(([tm, l], i) => {
    const x = [500, 770, 1041, 1311, 1582][i]!; els.push(t('clock-time', tm!, x, 866, 43, 29), t('clock-label', l!, x, 900, 248, 20));
  });
  ['Scout', 'Fixer', 'Tech', 'Enforcer'].forEach((h, i) => {
    const x = 57 + i * 290; els.push(img('face', `face ${h}`, x, 993, 52, 52), t('cell-head', h, x + 71, 984, 70, 14), t('cell', ['Listens to Inez and the driver.', 'Turns Inez or the driver.', 'Loops the door camera.', 'Takes Vale at the car.'][i]!, x + 71, 1004, 212, 40, { rows: ['x x', 'x x'] }));
  });
  els.push(t('purse', '$100,000', 1250, 990, 122, 30), t('heat-head', 'Heat', 1252, 1026, 40, 18), t('heat', 'Unknown', 1300, 1026, 76, 18), t('begin', 'Begin', 1711, 995, 153, 48));
  return els;
}
/** The drawn page: cream paper, each text element's lines as dark strokes in the middle of its rows (ink, as the page's ink reader sees it), the pictures grey. */
function draw(els: El[], noise = false): Image {
  const W = 1920, H = 1080, data = new Uint8Array(W * H * 4);
  for (let i = 0; i < W * H; i++) { const v = noise ? 120 + ((i * 2654435761) >>> 0) % 120 : 228; data.set([v, v - 4, v - 16, 255], i * 4); }
  const fill = (x0: number, y0: number, x1: number, y1: number, v: number) => { for (let y = Math.max(0, y0); y < Math.min(H, y1); y++) for (let x = Math.max(0, x0); x < Math.min(W, x1); x++) data.set([v, v, v, 255], (y * W + x) * 4); };
  for (const e of els) if (e.kind === 'image') fill(e.x, e.y, e.x + e.w, e.y + e.h, 150);
  for (const e of els) if (e.kind === 'hud' && e.text) {
    const rows = e.rows?.length ?? 1, rh = e.h / rows;
    for (let r = 0; r < rows; r++) { const y0 = Math.round(e.y + r * rh + rh * 0.25), y1 = Math.round(e.y + r * rh + rh * 0.75); for (let x = e.x + 2; x < e.x + Math.min(e.w, 8 * (e.text.length + 1)) - 2; x += 4) fill(x, y0, x + 2, y1, 40); }
  }
  return { w: W, h: H, data };
}
const judge = (els: El[], o: { noise?: boolean } = {}) => analyse(draw(els, o.noise), { state: 'page', rects: els.filter(e => e.kind !== 'paper'), manifest: { state: 'page', elements: els } }); // the HUD layer: every piece's rect, the paper outside it
const fails = (els: El[], o?: { noise?: boolean }) => judge(els, o).failures.join(' | ');
const edit = (f: (els: El[]) => El[]) => f(page());

describe('the page state (GDD v0.27.0 §10: outside play, the reading limits do not apply)', () => {
  test('page has no coverage limit and is a reading surface; the retired briefing keeps its old limit for the archived captures', () => {
    expect('page' in LIMIT).toBe(false); expect(LIMIT.briefing).toBe(18); expect(READING).toContain('page');
  });
  test('the drawn page passes every page rule, at under 200 words', () => {
    const r = judge(page()); expect(r.failures).toEqual([]); expect(r.pass).toBe(true); expect(r.limitPct).toBeNull(); expect(r.pageWords!).toBeLessThanOrEqual(PAGE.words);
    expect(r.warnings).toEqual([]);
  });
  test('a page without its manifest fails: every word on it is a manifest element', () => {
    expect(analyse(draw(page()), { state: 'page', rects: [{ x: 0, y: 0, w: 4, h: 4 }] }).failures.join()).toContain('needs its manifest');
  });
});

describe('the page rules, each with a failing example (glyph-page-budget §2)', () => {
  test('2.1 opaque: no paper and the street showing fails; the drawn paper without its rects passes as flat', () => {
    const noPaper = edit(e => e.filter(x => x.kind !== 'paper'));
    expect(fails(noPaper)).toBe(''); expect(fails(noPaper, { noise: true })).toContain('the page shows the street');
    expect(fails(edit(e => e.map(x => (x.kind === 'paper' ? { ...x, w: 1500 } : x))))).toContain('the page shows the street (its paper leaves');
  });
  test('2.2 listed: one image over the whole page hides it (0.2.130\'s "cell faces"); one face an image passes', () => {
    expect(fails(edit(e => [...e, img('image', 'cell faces', 0, 0, 1920, 1080)]))).toContain('the image "cell faces" at 0,0 1920x1080 holds other pieces');
    expect(fails(edit(e => [...e, img('image', 'cell faces', 50, 980, 1180, 70)]))).toContain('holds other pieces');
  });
  test('2.3 inside: a piece over the safe frame, a caption past its still, a child outside its parent', () => {
    expect(fails(edit(e => e.map(x => (x.part === 'title' ? { ...x, y: 30 } : x))))).toContain('the title "The Handover" at 56,30 176x22 is outside the safe frame (48 px');
    expect(fails(edit(e => e.map(x => (x.part === 'begin' ? { ...x, y: 1010 } : x))))).toContain('the begin "Begin" at 1711,1010 153x48 is outside the safe frame (32 px in from each edge, the foot band)');
    expect(fails(edit(e => e.map(x => (x.text === 'Inez' && x.part === 'caption' ? { ...x, w: 400 } : x))))).toContain('the caption "Inez" is clipped: it runs past its still');
    expect(fails(edit(e => [...e.map(x => (x.part === 'way-when' && x.text === 'From 6:10' ? { ...x, id: 'when2' } : x)), t('text', 'tag', 600, 580, 30, 14, { parent: 'when2' })]))).toContain('is clipped: it runs outside its parent "when2"');
  });
  test('2.4 apart: a plan label on a pin fails (0.2.130\'s "Records Office" under pin 2); a pin on its own still and labels on the plan do not', () => {
    expect(fails(edit(e => e.map(x => (x.text === 'Records Office' ? { ...x, x: 320, y: 742 } : x))))).toContain('the plan-label "Records Office" and the pin "" overlap');
    expect(fails(edit(e => e.map(x => (x.part === 'objective' ? { ...x, y: 112 } : x))))).toMatch(/the hook "Vale has a file on you\." and the objective .* (touch|overlap)/);
  });
  test('2.5 type size: a body line under 20 px fails; a heading no taller than the body fails', () => {
    expect(fails(edit(e => e.map(x => (x.part === 'clock-label' && x.text === 'Handover chime' ? { ...x, h: 16 } : x))))).toContain('the clock-label "Handover chime" is set at 16 px a line');
    expect(fails(edit(e => e.map(x => (x.part === 'way-name' && x.text === 'Pickup' ? { ...x, h: 24 } : x))))).toContain('the way-name "Pickup" is a heading no taller than the body');
  });
  test('2.6 pins: a pin missing on the plan, a still with no pin, pins out of way order, the ending without the car', () => {
    expect(fails(edit(e => e.filter(x => !(x.part === 'pin' && x.n === 3 && x.x === 380))))).toContain('pin 3 on a still, missing on the plan');
    expect(fails(edit(e => e.filter(x => !(x.part === 'pin' && x.n === 2 && x.x === 1198))))).toContain('the still "still The guard" carries 0 pins (exactly one)');
    expect(fails(edit(e => e.map(x => (x.part === 'pin' && x.x === 794 && x.n === 1 ? { ...x, n: 9 } : x))))).toContain('the stills\' pins read 9, 2, 3, 4 in way order (1..4)');
    expect(fails(edit(e => e.map(x => (x.part === 'pin' && x.x === 1594 ? { ...x, n: 5 } : x))))).toContain('the ending\'s pin is 5 (the car glyph)');
  });
  test('2.7 words: a 31-word way and a 201-word page fail; so do a missing Heat head word, a second purse and a two-sentence hook', () => {
    const long = 'Turn her to call him off the door and loop the camera that watches it, wait by the kerb until the guard walks off.'; // with the first line: 7 + 24 = 31
    expect(fails(edit(e => e.map(x => (x.text === 'Turn her to call him off the door.' ? { ...x, text: long, h: 75, rows: ['a b c', 'd e f', 'g h i'] } : x))))).toContain('the way "Front door" reads 31 words (at most 30)');
    const words = judge(page()).pageWords!, pad = PAGE.words + 1 - words, filler = t('text', Array.from({ length: pad }, () => 'word').join(' '), 1200, 812, 300, 20, { rows: ['a b', 'c d'] });
    expect(pad).toBeGreaterThan(0); expect(fails(edit(e => [...e, filler]))).toContain(`the page reads ${PAGE.words + 1} words (at most ${PAGE.words}`);
    expect(fails(edit(e => e.filter(x => x.part !== 'heat-head')))).toContain('the Heat head word is missing');
    expect(fails(edit(e => [...e, t('purse', '$5,000', 1400, 990, 90, 30)]))).toContain('2 purse figures (one)');
    expect(fails(edit(e => e.map(x => (x.part === 'hook' ? { ...x, text: 'Vale has a file. On you.' } : x))))).toContain('the hook is 2 sentences (at most 1)');
    expect(fails(edit(e => e.filter(x => !(x.part === 'clock-time' && x.text === '5:30'))))).toBe(''); // four stations are within 3 to 7
    expect(fails(edit(e => e.map(x => (x.part === 'clock-time' && x.text === '5:30' ? { ...x, text: '7:30' } : x))))).toContain('the clock\'s times do not ascend');
  });
  test('2.8 widows: a wrapped element whose last line is one word fails (0.2.130\'s "it.")', () => {
    expect(fails(edit(e => e.map(x => (x.text === 'Turn her to call him off the door.' ? { ...x, h: 50, rows: ['Turn her to call him off the', 'door.'] } : x))))).toContain('ends on a widow');
    expect(lastRowWords('Turn her to call him off the door, and loop the camera that watches it.', [{ x0: 500, x1: 741 }, { x0: 500, x1: 749 }, { x0: 500, x1: 513 }])).toBe(1);
    expect(lastRowWords('Turn him before 5:40, and he brings Vale round to the service bay and waits.', [{ x0: 500, x1: 711 }, { x0: 500, x1: 738 }, { x0: 500, x1: 605 }])).toBe(3);
  });
  test('2.8 widows: a label of two words or fewer is exempt ("Corner café", "Records Office" stacked on the plan); a three-word label stacked to a one-word line still fails', () => {
    const stack = (from: string, rows: string[]) => edit(e => e.map(x => (x.text === from ? { ...x, h: 30, rows } : x)));
    expect(fails(stack('Corner café', ['Corner', 'café']))).not.toContain('widow');
    expect(fails(stack('Records Office', ['Records', 'Office']))).not.toContain('widow');
    expect(fails(stack('The getaway car', ['The getaway', 'car']))).toContain('the legend "The getaway car" ends on a widow');
  });
});

describe('build 0.2.130\'s briefing page (fixtures/page-130: briefing-day as shot, photographs flattened)', () => {
  const shot = decodePng(readFileSync(fx('page-130.png'))), hud = decodePng(readFileSync(fx('page-130.hud.png'))), man = JSON.parse(readFileSync(fx('page-130.manifest.json'), 'utf8')) as Manifest;
  const r = analyse(shot, { state: 'page', hud, manifest: man }), f = r.failures;
  test('shot as `briefing` (the retired street rule) it fails coverage and the reading cap; as `page` neither applies', () => {
    expect(man.state).toBe('briefing');
    const old = analyse(shot, { state: 'briefing', hud, manifest: man }).failures.join(' | '); expect(old).toContain('limit 18% briefing'); expect(old).toContain('the briefing reads 20 sentences');
    expect(f.join()).not.toMatch(/HUD covers|the briefing reads|running text|stacked text/); expect(r.limitPct).toBeNull();
  });
  test('its parts are read from text and layout: title, hook, objective, two ways, five stations, four cell lines, the ending, the heat word', () => {
    const p = pageParts(man.elements, 1920, 1080), of = (k: PagePart) => p.filter(e => e.part === k).map(e => e.text);
    expect(of('title')).toEqual(['The Handover']); expect(of('hook')).toEqual(['Vale has a file on you.']); expect(of('objective')).toEqual(['Take him alive before it reaches Security.']);
    expect(of('way-name')).toEqual(['Front door', 'Pickup']); expect(of('way-when')).toEqual(['Before 5:50', 'From 6:10']); expect(of('way-text').length).toBe(4);
    expect(of('clock-time').length).toBe(5); expect(of('clock-label').length).toBe(5); expect(of('cell').length).toBe(4); expect(of('caption').length).toBe(5);
    expect(of('ending')).toEqual(['Both ways end here: Vale in the getaway car as it leaves.']); expect(of('target-name')).toEqual(['Councillor Vale']); expect(of('target-fact').length).toBe(2);
    expect(of('legend').length).toBe(5); expect(of('heat')).toEqual(['Unknown']); expect(of('purse')).toEqual(['$100,000']); expect(of('begin')).toEqual(['Begin']); expect(of('text')).toEqual([]);
  });
  test('Glyph\'s four faults are named: the full-frame "cell faces" image, the missing Heat head word, the front door\'s widow; the plan\'s words are unlisted until the host emits them', () => {
    expect(f.join()).toContain('the image "cell faces" at 0,0 1920x1080 holds other pieces'); expect(f.join()).toContain('the Heat head word is missing');
    expect(f.filter(x => /widow/.test(x))).toEqual(['the way-text "Turn her to call him off the doo" ends on a widow: its last line is one word']);
    expect(f.filter(x => /^unlisted words at (76|309),73[13]/.test(x)).length).toBe(2); // "Corner café" and "Records Office" on the plan
  });
  test('and the faults the spec\'s numbers did not foresee, real on the page as built: unlisted headings, the safe frame (214 words is within the 220 cap, Aster 2026-10-26)', () => {
    expect(f.filter(x => /^unlisted words at 56,207/.test(x)).length).toBe(1); // TARGET
    expect(f.filter(x => /^unlisted words at \d+,997/.test(x)).length).toBe(4); // SCOUT, FIXER, TECH, ENFORCER
    expect(f.filter(x => /outside the safe frame/.test(x)).length).toBe(7); // the title 2 px high; the four cell lines, Unknown and Begin under the foot band's 32 px
    expect(r.pageWords).toBe(214); expect(f.join()).not.toContain('the page reads');
    expect(f.length).toBe(17); // and nothing else: no overlap, type-size, clock or way-cap fault on 0.2.130
  });
});
