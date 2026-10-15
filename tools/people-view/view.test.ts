// The roster viewer (`bun run people:view`): deterministic (same inputs, same bytes), complete (every person is in the roster, the graph and the map, and in the data the
// scripts read), self-contained (no network), and right about the rules it shows. The page is checked by parsing it; a headless Chrome pass runs when Chrome is installed.
import { beforeAll, describe, expect, test } from 'bun:test';
import { existsSync } from 'node:fs';
import { sha256 } from '../gen/sha';
import { buildView, VIEW_SCHEMA, type View } from './data';
import { approachRows } from './fit';
import { layout } from './layout';
import { render } from './page';
import { parseRules } from '../people/rules';
import { readFileSync } from 'node:fs';

let view: View, html: string;
const dataOf = (h: string) => JSON.parse(h.match(/<script type="application\/json" id="data">([\s\S]*?)<\/script>/)![1]!) as View & { ui: unknown };
const count = (h: string, re: RegExp) => [...h.matchAll(re)].map(m => m[1]!);

beforeAll(async () => { view = await buildView({ run: 'r015' }); html = render(view); });

describe('lovers and secrets (brief 011)', () => {
  let fx: View, fxHtml: string;
  beforeAll(async () => { fx = await buildView({ cast: 'tools/people/test/handover-fix.cast.json', against: 'none' }); fxHtml = render(fx); });
  test('the fixture passes every check, P-14 and P-15 among them', () => {
    expect(fx.checks.filter(c => !c.ok).map(c => `${c.id} ${c.detail}`)).toEqual([]);
    expect(fx.checks.map(c => c.id)).toContain('P-14'); expect(fx.checks.map(c => c.id)).toContain('P-15');
  });
  test('each lover carries the affair and a lover tie with the stop it is met on; the rules list the secrets and flag the tie secret', () => {
    const L = fx.persons.filter(p => p.ties.some(t => t.kind === 'lover'));
    expect(L.length).toBeGreaterThanOrEqual(2);
    for (const p of L) { expect(p.secret).toBe('affair'); expect(p.ties.find(t => t.kind === 'lover')!.at).toMatch(/^[a-z-]+ \d\d:\d\d-\d\d:\d\d$/); }
    expect(fx.rules.ties.lover!.secret).toBe(true); expect(Object.keys(fx.rules.secrets).sort()).toEqual(['affair', 'record']);
    expect(fx.graph.kinds.lover).toBeGreaterThanOrEqual(1);
  });
  test('the page shows the secret section, the lover edge style and the table\'s lover pattern', () => {
    expect(fxHtml).toContain('Secret <small>'); expect(fxHtml).toContain('.e.lover'); expect(fxHtml).toContain('secretchip'); expect(fxHtml).toMatch(/lover ties: /);
    expect(dataOf(fxHtml).ui).toBeDefined();
  });
  test('the rows show the old need secret as the record share', () => {
    expect(fx.rows.every(r => !r.needs.some(n => n.k.startsWith('secret')))).toBe(true);
    expect(fx.rows.some(r => r.secrets.some(s => s.k === 'record'))).toBe(true);
  });
});

describe('the data', () => {
  test('is the same bytes twice', async () => {
    const again = render(await buildView({ run: 'r015' }));
    expect(sha256(again)).toBe(sha256(html));
  });
  test('is the expansion `bun run people` makes: 130 people, all checks on the current inputs', () => {
    expect(view.schema).toBe(VIEW_SCHEMA);
    expect(view.persons.length).toBe(130);
    expect(view.checks.map(c => c.id)).toEqual([...Array(16).keys()].map(i => `P-${i}`).concat('M-2'));
    // r015's tables carry no lover pattern: under the lovers' canon (brief 011) the waitress role cannot be cast, so P-6 and M-2 name her and nothing else fails, until a repair run writes the pair
    const bad = view.checks.filter(c => !c.ok);
    expect(bad.map(c => c.id).filter(id => !['P-6', 'M-2'].includes(id))).toEqual([]);
    for (const c of bad) expect(c.detail).toMatch(/waitress/);
    expect(view.run?.outcome).toBe('ACCEPTED');
    expect(view.run?.keel.questions.length).toBe(3);
  });
  test('the four Handover roles are cast and each names a person in the data', () => {
    expect(view.roles.map(r => r.id)).toEqual(['waitress', 'driver', 'guard', 'councillor']);
    for (const r of view.roles) { expect(r.ok).toBe(true); expect(view.persons.find(p => p.id === r.pinned)?.role).toBe(r.id); }
  });
  test('a person carries every scale as its step word and an approach table read from the rules', () => {
    const rules = parseRules(readFileSync('contracts/people/people.rules.json', 'utf8'));
    for (const p of view.persons) {
      for (const s of ['bravery', 'resentment', 'fear', 'loyalty'] as const) expect(p.steps[s]).toBe(view.rules.steps[s]![p.start[s]]!);
      const rows = view.fits[p.fit]!;
      expect(rows.map(r => r.a)).toEqual(['ask', 'pay', 'help', 'pressure']);
      const T = rules.temperaments[p.tem as keyof typeof rules.temperaments];
      for (const r of rows.filter(x => x.a !== 'ask')) expect(r.fit).toBe((T.respondsTo as readonly string[]).includes(r.a) ? 'suits' : (T.refuses as readonly string[]).includes(r.a) ? 'refused' : 'worse');
    }
  });
});

describe('the page is complete', () => {
  const ids = () => view.persons.map(p => p.id).sort();
  test('every person is in the roster, once', () => expect(count(html, /<tr tabindex="0" data-p="([^"]+)"/g).sort()).toEqual(ids()));
  test('every person is a node of the graph, once', () => expect(count(html, /<g class="n [^"]*" data-p="([^"]+)"/g).sort()).toEqual(ids()));
  test('every person is a dot on the map, once', () => expect(count(html, /<circle class="dot [^"]*" data-p="([^"]+)"/g).sort()).toEqual(ids()));
  test('every person is in the data the scripts read', () => expect(dataOf(html).persons.map(p => p.id).sort()).toEqual(ids()));
  test('every tie is an edge, and every edge joins two people', () => {
    const d = dataOf(html), by = new Set(d.persons.map(p => p.id)), kinds = d.rules.ties;
    const want = new Set<string>();
    for (const p of d.persons) for (const t of p.ties) want.add(kinds[t.kind]!.direction === 'both' ? `${t.kind}|${[p.id, t.with].sort().join('|')}` : `${t.kind}>${p.id}>${t.with}`);
    const have = new Set(d.graph.edges.map(e => (kinds[e.kind]!.direction === 'both' ? `${e.kind}|${[e.a, e.b].sort().join('|')}` : `${e.kind}>${e.a}>${e.b}`)));
    expect(have).toEqual(want);
    for (const e of d.graph.edges) { expect(by.has(e.a)).toBe(true); expect(by.has(e.b)).toBe(true); }
    expect(count(html, /<line class="e [^"]*" data-i="(\d+)"/g).length).toBe(d.graph.edges.length);
  });
  test('the map draws the block: buildings, doors, café tables and chairs, bays, a crossing, the mission points', () => {
    for (const id of ['L-buildings', 'L-doors', 'L-seats', 'L-bays', 'L-crossing', 'L-points', 'L-dots']) expect(html).toContain(`id="${id}"`);
    expect(view.map.polys.filter(p => p.layer === 'buildings').length).toBeGreaterThanOrEqual(2);
    expect(view.map.chairs.length).toBeGreaterThanOrEqual(8);
    expect(view.map.marks.filter(m => m.kind === 'dest').length).toBeGreaterThan(10);
  });
  test('the graph layout is seeded: the same people give the same nodes, all inside the frame', () => {
    const d = dataOf(html);
    for (const n of d.graph.nodes) { expect(n.x).toBeGreaterThanOrEqual(0); expect(n.x).toBeLessThanOrEqual(d.graph.w); expect(n.y).toBeGreaterThanOrEqual(0); expect(n.y).toBeLessThanOrEqual(d.graph.h); }
    const nodes = d.persons.map((p, i) => ({ id: p.id, group: i % 3 })), edges = d.graph.edges.map(e => ({ a: e.a, b: e.b, w: 1 }));
    expect([...layout(nodes, edges)]).toEqual([...layout(nodes, edges)]);
  });
  test('the page is self-contained: no network, no link, no external script or style', () => {
    const noData = html.replace(/<script type="application\/json" id="data">[\s\S]*?<\/script>/, '');
    expect(noData).not.toMatch(/<link\b|<script[^>]*\ssrc=|@import|url\((?!#)|<img\b|<iframe\b/i);
    expect([...noData.matchAll(/https?:\/\/[^\s"'<>)]+/g)].map(m => m[0]).filter(u => u !== 'http://www.w3.org/2000/svg')).toEqual([]);
    expect(html).not.toMatch(/<\/script>[^<]*<script[^>]*src/);
  });
});

describe('against another cast', () => {
  test('r015 against the promoted cast: every id is added, changed or the same, and every diff names real fields', () => {
    const c = view.compare!;
    expect(view.against?.id).toBe('promoted');
    expect(c.added.length + c.changed.length + c.same.length).toBe(view.persons.length);
    expect(c.removed).toEqual([]);
    expect(c.added.sort()).toEqual(view.persons.filter(p => p.diff?.status === 'added').map(p => p.id).sort());
    for (const p of view.persons.filter(x => x.diff?.status === 'changed')) for (const [f, a, b] of p.diff!.fields) { expect(f.length).toBeGreaterThan(0); expect(a).not.toBe(b); }
    for (const [dim, xs] of Object.entries(c.dims)) { expect(xs.reduce((n, x) => n + x.b, 0)).toBe(view.persons.length); expect(dim.length).toBeGreaterThan(0); }
    expect(html).toContain('data-tab="compare"');
  });
  test('the promoted cast alone has no comparison', async () => {
    const v = await buildView({});
    expect(v.compare).toBeUndefined();
    expect(render(v)).not.toContain('data-tab="compare"');
  });
  test('--against none drops it', async () => expect((await buildView({ run: 'r015', against: 'none' })).compare).toBeUndefined());
});

describe('what the rules say of an approach', () => {
  const rules = parseRules(readFileSync('contracts/people/people.rules.json', 'utf8')), rb = JSON.parse(readFileSync('contracts/rules/rulebook.json', 'utf8'));
  test('Help on a fearful person works one step worse (one more Help), not refused; Pressure suits them', () => {
    const rows = approachRows(rules, rb, 'fearful', 'debt'), help = rows.find(r => r.a === 'help')!, pressure = rows.find(r => r.a === 'pressure')!;
    expect(help.fit).toBe('worse'); expect(help.works).toBe(true); expect(help.orders).toBe(2); expect(help.total.usd).toBe(80000);
    expect(pressure.fit).toBe('suits');
  });
  test('an Idealist refuses Pay; a Loyal person takes a second Help; Ask is refused cold', () => {
    expect(approachRows(rules, rb, 'idealist', 'debt').find(r => r.a === 'pay')!.fit).toBe('refused');
    expect(approachRows(rules, rb, 'loyal', 'debt').find(r => r.a === 'help')!.orders).toBe(2);
    expect(approachRows(rules, rb, 'mercenary', 'cash').find(r => r.a === 'ask')!.fit).toBe('refused');
  });
  test('Help does not act on a need only Pressure leans on', () => {
    const r = approachRows(rules, rb, 'idealist', 'care').find(x => x.a === 'pressure')!;
    expect(r.needActs).toBe(false); expect(r.works).toBe(false);
  });
});

const CHROME = process.env.CHROME ?? '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome';
describe.skipIf(!existsSync(CHROME))('in a headless browser', () => {
  test('the scripts run: the roster, graph and map are drawn with every person, and the map places them', async () => {
    const file = '/tmp/people-view-test.html';
    await Bun.write(file, html);
    const p = Bun.spawnSync([CHROME, '--headless=new', '--disable-gpu', '--virtual-time-budget=4000', '--dump-dom', `file://${file}#map?t=63000`], { stdout: 'pipe', stderr: 'ignore' });
    const dom = new TextDecoder().decode(p.stdout);
    expect(dom).toContain('data-ready="1"');
    expect(count(dom, /<tr tabindex="0" data-p="([^"]+)"/g).length).toBe(130);
    expect(count(dom, /<g class="n [^"]*" data-p="([^"]+)"/g).length).toBe(130);
    const live = dom.match(/<span>Drawn on the block<\/span><b>(\d+)<\/b>/);
    expect(live).not.toBeNull();
    const shown = [...dom.matchAll(/<circle class="dot [^"]*" data-p="[^"]+" r="[^"]*" cx="[^"]*" cy="[^"]*"(?! hidden)/g)].length;
    expect(shown).toBe(Number(live![1]));
  }, 30000);
});
