// The proof map's rows for the kit's pictograms (brief 101; Latch's take-control round, briefs/101/latch/tc.html "Open": two new singles counted as a kit change, so a full proof, though
// nothing but the new code named them). A pictogram (UI/Resources/Kit/icons/singles/<name>@2x.png, with its meta) shows only where something names it, so a row of its own must run
// every section, and name every shot, that the rows of what names it do; then, when the only code that names it is in the same change, that code's delta is the honest run. A
// pictogram with no row of its own stays on the kit's UI/** row, every section. What names a pictogram, as the tree shows it:
// - a string literal in the player's code (Runtime/**/*.cs, comments left out) that is its name, or icons/singles/<name>, either with a #variant;
// - a literal prefix joined to a value (`"eye_" + eye`, `$"mood_{m}"`): every pictogram it begins;
// - the words and the engine's data: a string value in contracts/missions/handover/*.json (the book's `icon`s) or StreamingAssets/sim/*.json that is its name; the first word of a
//   conversation card's row, which the slip looks up (HudToolkit.Card: CardSplit, CardIcon); a need or temperament of people.rules.json, which the Intel row shows as its own
//   pictogram (HudModel: NeedIcon, TempIcon);
// - the code's other lookups by a value (`"icons/singles/" +` one), each placed in LOOKUPS: a helper whose callers write the name, or the slip's words. One not placed fails the test.
import { describe, expect, test } from 'bun:test';
import { readdirSync, readFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { classify, loadMap, sectionIds, whatIf, type MapRow, type ProofMap } from './proofdelta';

const ROOT = resolve(import.meta.dir, '../..'), RUNTIME = 'unity/Assets/Suborned/Runtime/', SINGLES = 'unity/Assets/Suborned/UI/Resources/Kit/icons/singles/', CARD = 'Play/HudToolkit.Card.cs';
const m = loadMap(), ids = sectionIds(m);
/** The player's lookups by a value, each with where its names come from. */
const LOOKUPS: Record<string, string> = {
  'Play/HudToolkit.cs': 'Icon(e, atlas, tint): its callers write the name',
  'Play/HudToolkit.Street.cs': 'KeyIcon.Set(h, name): its callers write the name',
  'Play/HudToolkit.Less.cs': 'the price row\'s lead: an intent\'s Lead, written as literals, or HudModel.AtlasIcon\'s literals',
  'Play/HostWhy.Reports.cs': 'OnWayIcon(way)\'s literals, struck',
  [CARD]: 'the first word of a card\'s row: the card words',
};

type Lit = { text: string; joined: boolean };
/** A C# file's string literals, comments left out: each literal's text (an interpolated one's up to its first hole, which joins it to a value), and whether a value is joined to it. */
function literals(src: string): Lit[] {
  const code = src.replace(/@"(?:[^"]|"")*"|\$?"(?:[^"\\\n]|\\.)*"|'(?:[^'\\\n]|\\.)*'|\/\/[^\n]*|\/\*[\s\S]*?\*\//g, t => (t.startsWith('//') || t.startsWith('/*') ? ' ' : t));
  return [...code.matchAll(/(\$?)"((?:[^"\\\n]|\\.)*)"(\s*\+)?/g)].map(([, dollar, body, plus]) => {
    const hole = dollar ? body!.indexOf('{') : -1;
    return { text: hole >= 0 ? body!.slice(0, hole) : body!, joined: !!plus || hole >= 0 };
  });
}
const strings = (o: unknown): string[] => (typeof o === 'string' ? [o] : Array.isArray(o) ? o.flatMap(strings) : o && typeof o === 'object' ? Object.values(o).flatMap(strings) : []);

type Tree = { code: Record<string, string>; data: Record<string, string[]>; cardWords: string[]; engineWords: string[] };
function tree(): Tree {
  const code = Object.fromEntries((readdirSync(join(ROOT, RUNTIME), { recursive: true }) as string[]).filter(f => f.endsWith('.cs')).map(f => [f, readFileSync(join(ROOT, RUNTIME, f), 'utf8')]));
  const json = (dir: string) => readdirSync(join(ROOT, dir)).filter(f => f.endsWith('.json')).map(f => [dir + f, strings(JSON.parse(readFileSync(join(ROOT, dir, f), 'utf8')))] as const);
  const data = Object.fromEntries([...json('contracts/missions/handover/'), ...json('unity/Assets/StreamingAssets/sim/')]);
  const text = JSON.parse(readFileSync(join(ROOT, 'contracts/missions/handover/text.json'), 'utf8')), first = (s: string) => s.split(' ')[0]!.toLowerCase();
  const label = (l: string) => { const i = l.indexOf(': '); return i > 0 && i <= 16 ? l.slice(0, i) : ''; }; // HudToolkit.Card CardSplit: a label of at most 16 characters before ": "
  const cardWords = (text.scenes.conversations.items as { learned?: string[]; opens?: string[] }[]).flatMap(c => [...(c.learned ?? []).map(label).filter(Boolean), ...(c.opens ?? []).map(l => label(l) || l)]).map(first);
  const rules = JSON.parse(readFileSync(join(ROOT, 'contracts/people/people.rules.json'), 'utf8')), engineWords = [...Object.keys(rules.needs.list), ...Object.keys(rules.temperaments)];
  return { code, data, cardWords, engineWords };
}
/** What names the pictogram `name`: repo paths, each a file whose row then decides where it shows. */
function namers(t: Tree, name: string): string[] {
  const out = new Set<string>(), bare = (s: string) => s.replace(/#.*$/, '');
  for (const [f, src] of Object.entries(t.code)) for (const l of literals(src)) {
    const s = bare(l.text);
    if (s === name || s === `icons/singles/${name}`) out.add(RUNTIME + f);
    if (l.joined && ((/^[a-z][a-z0-9]*_$/.test(l.text) && name.startsWith(l.text)) || (l.text.startsWith('icons/singles/') && l.text.length > 14 && name.startsWith(l.text.slice(14))))) out.add(RUNTIME + f);
  }
  for (const [f, vs] of Object.entries(t.data)) if (vs.includes(name)) out.add(f);
  if (t.cardWords.includes(name)) out.add(RUNTIME + CARD);
  if (t.engineWords.includes(name)) out.add(RUNTIME + 'Play/HudModel.cs');
  return [...out].sort();
}
/** The code that looks a pictogram up by a value no literal names: `"icons/singles/" +` one. */
const lookups = (t: Tree) => Object.entries(t.code).filter(([, src]) => literals(src).some(l => l.joined && l.text === 'icons/singles/')).map(([f]) => f).sort();
const sections = (r: MapRow | undefined) => (!r || r.run === 'none' ? [] : r.run === 'full' ? ids : r.run);
const shotsOf = (r: MapRow | undefined): 'all' | string[] => (!r || r.run === 'none' ? [] : r.run === 'full' || r.shots === 'all' ? 'all' : r.shots ?? []);
/** Each pictogram's row against the rows of what names it: what it lacks. */
function pictogramGaps(map: ProofMap, t: Tree, files: string[]): string[] {
  const gaps: string[] = [];
  for (const f of files) {
    const row = classify(map, SINGLES + f), meta = classify(map, SINGLES + f + '.meta'), mine = sections(row), shots = shotsOf(row), name = f.replace(/@2x\.png$/, '');
    if (row !== meta) gaps.push(`${f} and its meta sit on different rows`);
    for (const n of namers(t, name)) {
      const r = classify(map, n), lack = sections(r).filter(s => !mine.includes(s)), ns = shotsOf(r), lackShots = shots === 'all' ? [] : ns === 'all' ? ['all'] : ns.filter(s => !shots.includes(s));
      if (lack.length || lackShots.length) gaps.push(`${f}'s row lacks ${[...lack, ...lackShots.map(s => `shot ${s}`)].join(', ')} (${n.replace(RUNTIME, '')} names it)`);
    }
  }
  return gaps;
}

describe('a pictogram\'s row is the rows of what names it', () => {
  const t = tree(), files = readdirSync(join(ROOT, SINGLES)).filter(f => f.endsWith('@2x.png')).sort(), own = files.filter(f => classify(m, SINGLES + f)?.match.some(g => g.includes('icons/singles/')));
  test('the literals are read as the code writes them, comments left out', () => {
    expect(literals('Icon(c.Icon, "map", Cream); // "take_control"\n/* "x" */ var s = "eye_" + eye; var t = $"mood_{m}"; var u = @"a""b";'))
      .toEqual([{ text: 'map', joined: false }, { text: 'eye_', joined: true }, { text: 'mood_', joined: true }, { text: 'a', joined: false }, { text: 'b', joined: false }]);
  });
  test('the two take-control pictograms have their row, and only HudToolkit.TakeControl.cs names them in the code that draws (the traces\' "map" is a phase on the base\'s row)', () => {
    expect(own).toEqual(['map@2x.png', 'take_control@2x.png']);
    expect(namers(t, 'take_control')).toEqual([RUNTIME + 'Play/HudToolkit.TakeControl.cs']);
    expect(namers(t, 'map')).toEqual([RUNTIME + 'Play/HitchTrace096.cs', RUNTIME + 'Play/HudToolkit.TakeControl.cs', RUNTIME + 'Play/ViewTrace096.cs']);
    expect(sections(classify(m, RUNTIME + 'Play/HitchTrace096.cs'))).toEqual([]); expect(sections(classify(m, RUNTIME + 'Play/ViewTrace096.cs'))).toEqual([]);
  });
  test('every pictogram\'s row runs all that names it runs, and its meta sits on the same row', () => { expect(pictogramGaps(m, t, files)).toEqual([]); });
  test('every lookup by a value is placed, and every place is still one', () => { expect(lookups(t)).toEqual(Object.keys(LOOKUPS).sort()); });
  test('a change that adds them with the code that names them runs that code\'s delta; alone, they run its row; a new pictogram with no row of its own runs every section', () => {
    const code = RUNTIME + 'Play/HudToolkit.TakeControl.cs', icons = ['take_control@2x.png', 'take_control@2x.png.meta', 'map@2x.png', 'map@2x.png.meta'].map(f => SINGLES + f);
    const withCode = whatIf(m, [...icons, code]), alone = whatIf(m, [code]);
    expect(withCode.mode).toBe('delta'); expect(withCode.only).toEqual(alone.only); expect(alone.only).toEqual(['r059', 'r067', 'r123', 'r083', 'hudClick']);
    expect(whatIf(m, icons).only).toEqual(alone.only); expect(classify(m, icons[0]!)?.shots).toEqual(classify(m, code)?.shots);
    expect(whatIf(m, [SINGLES + 'zz_new@2x.png', SINGLES + 'zz_new@2x.png.meta']).mode).toBe('full');
  });
  test('failing examples: code on another row names "map"; the words name it; an eye family takes in a new eye; a lookup by a value nobody placed', () => {
    const plant = (code: Record<string, string>, data: Record<string, string[]> = {}) => pictogramGaps(m, { ...t, code: { ...t.code, ...code }, data: { ...t.data, ...data } }, ['map@2x.png']);
    expect(plant({ 'Play/HostDrive.cs': 'Icon(x, "icons/singles/map", Ink);' })).toEqual(["map@2x.png's row lacks driveClick, act, holdActs, done, fire, drive, leash, holdRange (Play/HostDrive.cs names it)"]);
    expect(plant({}, { 'contracts/missions/handover/text.json': ['map'] })).toEqual(["map@2x.png's row lacks done, shot briefing-day, shot office-front-yaw210 (contracts/missions/handover/text.json names it)"]);
    expect(namers(t, 'eye_new')).toContain(RUNTIME + 'Play/HudToolkit.Faces.cs'); expect(namers(t, 'debt')).toContain(RUNTIME + CARD); expect(namers(t, 'idealist')).toContain(RUNTIME + 'Play/HudModel.cs');
    expect(lookups({ ...t, code: { ...t.code, 'Play/HudToolkit.Ring.cs': 'Sp("icons/singles/" + word);' } })).toContain('Play/HudToolkit.Ring.cs');
  });
});
