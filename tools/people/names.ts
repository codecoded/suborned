// The city's names (brief 089, Pulse; Sky: "why can't they be a bit more normal? What does GTA do?"): real given and family names, drawn by rule
// from national frequency lists (contracts/people/names/<country>.json, their sources and licences in sources.json), never by a model.
//   country   a person, or a family, is drawn one country of rules names.lists.countries, by weight
//   sex       the body of the figure the host dresses the person in (tools/people/looks.ts: the host's own pick, replayed), so the name fits what the player sees
//   age       drawn in a band: by that figure (the elder), else by position (the councillor, residents), else the working default
//   given     from the country's list for their sex and decade of birth (ageReference − age), weighted by how common it is
//   family    from the same country's list, weighted the same way; everyone a family tie joins shares one family name and one country
//   never     the top `skipTop` of a list, a name on the ban list (names language models favour), the exclusions (the read-through), the avoid list, a pinned name
// Each draw is by the person's id (`<id>/age`, `<id>/name`) or the family's (its first member's id: `<family>/country`, `<family>/family`), so the same people get
// the same names, and a name draw moves nothing else. Pure; the texts it reads are hashed into the people file's inputs (`names`).
import type { Person } from '../../contracts/people/people.schema';
import { sha256 } from '../gen/sha';
import { makeBan, match, type Ban } from '../names/ban';
import { rng, type Rng } from '../world/gen/common';
import { dressCrowd, figureSex, FIGURES_FILE, mappingOf, ROLES_FILE, roleFigure, type Mapping } from './looks';
import type { Rules } from './rules';

type L = NonNullable<Rules['names']['lists']>;
type Entry = [string, number];
export type CountryList = { country: string; decades: number[]; given: Record<string, { f: Entry[]; m: Entry[] }>; family: Record<string, Entry[] | undefined>; /** an unranked list (Wikipedia's categories) has no top to skip */ ranked?: { given: boolean; family: boolean } };
export type NameTexts = Record<string, string>;
export type Lists = { rules: L; avoid: Set<string>; pattern: RegExp; countries: Map<string, CountryList>; ban: Ban; excluded: { part: Set<string>; full: Set<string> }; figures: Mapping; pinned: Set<string> };
export type Sex = 'f' | 'm';

/** The files the names are made from, in a fixed order: the country lists, their sources, the ban list, the exclusions and the figures (sex is the figure's body). */
export const nameFilesOf = (l: L): string[] => [...Object.keys(l.countries).sort().map(c => `${l.dir}/${c}.json`), `${l.dir}/sources.json`, l.banList, l.exclusions, FIGURES_FILE, ROLES_FILE];
/** The texts of those files, or undefined for rules without `names.lists` (a pinned older run: names from the cast's pool). */
export const readNameTexts = (rules: Rules, rd: (file: string) => string): NameTexts | undefined =>
  rules.names.lists ? Object.fromEntries(nameFilesOf(rules.names.lists).map(f => [f, rd(f)])) : undefined;
export const namesHash = (t: NameTexts) => sha256(Object.keys(t).sort().map(k => `${k}\n${t[k]}`).join('\n'));

export function parseLists(rules: Rules, t: NameTexts): Lists {
  const l = rules.names.lists!, countries = new Map<string, CountryList>();
  for (const c of Object.keys(l.countries).sort()) countries.set(c, JSON.parse(t[`${l.dir}/${c}.json`]!) as CountryList);
  const ex = JSON.parse(t[l.exclusions]!) as { names: { name?: string; full?: string }[] };
  return {
    rules: l, avoid: new Set(rules.names.avoid), pattern: new RegExp(rules.names.form.pattern, 'u'), countries,
    ban: makeBan(JSON.parse(t[l.banList]!), { allow: [], alsoWords: [] }, l.banList),
    excluded: { part: new Set(ex.names.flatMap(e => (e.name ? [e.name] : []))), full: new Set(ex.names.flatMap(e => (e.full ? [e.full] : []))) },
    figures: mappingOf(t[FIGURES_FILE]!, t[ROLES_FILE]!), pinned: new Set(rules.names.pinned.map(p => p.name)),
  };
}

/** A woman's form of a Polish family name drawn from the men's list: -ski, -cki, -dzki to -ska, -cka, -dzka; any other is the same for both. */
export const feminine = (name: string) => name.replace(/(sk|ck|dzk)i$/, '$1a');
/** The woman's form the lists bear (Poland): -ski, -cki, -dzki always to -a; an adjective in -y (Szczęsny, Biały) to -a when the women's list has that form, else as it is
 *  (Oleksy, Bury: not adjectives, the same for both); any other the same. */
export function womansForm(L: Lists, name: string): string {
  if (/(sk|ck|dzk)i$/.test(name)) return feminine(name);
  if (!name.endsWith('y')) return name;
  const women = memo(L, 'pl/women', () => L.countries.get('pl')?.family.f ?? []), has = (n: string) => women.some(([x]) => x === n), a = `${name.slice(0, -1)}a`;
  return has(a) || !has(name) ? a : name;
}
/** The family's own (men's) form of a Polish woman's surname, or the name itself. */
export const familyForm = (L: Lists, country: string | undefined, name: string) => (country === 'pl' ? familyPool(L, 'pl').find(([x]) => womansForm(L, x) === name)?.[0] ?? name : name);
/** Why a name may not be drawn, or undefined. */
export function refused(L: Lists, name: string): string | undefined {
  if (!L.pattern.test(name)) return 'fails the name pattern';
  if (L.avoid.has(name)) return 'on the avoid list';
  // only the named lists (strong, reported) exclude; the lookalike patterns would refuse ordinary surnames (Vidal, Varma, Vera), so they are a warning for the read-through (Aster, brief 089)
  if (match(L.ban, name).length) return `on the ban list (${match(L.ban, name)[0]!.why})`;
  if (L.excluded.part.has(name)) return 'on the exclusions';
  return undefined;
}
const cache = new WeakMap<Lists, Map<string, Entry[]>>();
const memo = (L: Lists, key: string, make: () => Entry[]) => { let m = cache.get(L); if (!m) cache.set(L, m = new Map()); let v = m.get(key); if (!v) m.set(key, v = make()); return v; };
const allowed = (L: Lists, n: string) => !refused(L, n) && !L.pinned.has(n);
const skipOf = (L: Lists, country: string, part: 'given' | 'family') => (L.countries.get(country)!.ranked?.[part] === false ? 0 : L.rules.skipTop);
const usable = (L: Lists, xs: Entry[], skip: number) => xs.slice(skip).filter(([n]) => allowed(L, n));
/** A list less its top `skipTop` (a ranked list): what a draw is weighted over. */
const listed = (xs: Entry[], skip: number) => xs.slice(skip);
/** The decade of birth a list is read at: ageReference − age, held to the decades the lists cover. */
export const decadeOf = (L: Lists, c: CountryList, age: number) => String(Math.min(Math.max(Math.floor((L.rules.ageReference - age) / 10) * 10, c.decades[0]!), c.decades.at(-1)!));
export const givenPool = (L: Lists, country: string, sex: Sex, decade: string) => memo(L, `g/${country}/${sex}/${decade}`, () => usable(L, L.countries.get(country)!.given[decade]![sex], skipOf(L, country, 'given')));
const givenList = (L: Lists, country: string, sex: Sex, decade: string) => memo(L, `gl/${country}/${sex}/${decade}`, () => listed(L.countries.get(country)!.given[decade]![sex], skipOf(L, country, 'given')));
/** A family's names: the list for any sex, or (Poland) the men's list, a woman carrying its woman's form; `key` a sub-list (the US census's shares: white, black, api). */
const famOf = (L: Lists, country: string, key: string) => { const f = L.countries.get(country)!.family; return (f[key] ?? f.any ?? f.m)!; };
export const familyPool = (L: Lists, country: string, key = 'any') => memo(L, `f/${country}/${key}`, () => usable(L, famOf(L, country, key), skipOf(L, country, 'family')));
const familyList = (L: Lists, country: string, key = 'any') => memo(L, `fl/${country}/${key}`, () => listed(famOf(L, country, key), skipOf(L, country, 'family')));
/** The heritage a figure shows (rules names.lists.heritage.figures), or undefined: drawn from every country. */
export const heritageOf = (L: Lists, figure: string | null | undefined) => (figure ? L.rules.heritage?.figures[figure] : undefined);
/** A family's heritage: the one its members' figures show, 'mixed' if they show several, undefined if none shows one. */
export function familyHeritage(L: Lists, members: string[], figures: Map<string, string | null>): string | undefined {
  const hs = new Set(members.map(id => heritageOf(L, figures.get(id))).filter((h): h is string => !!h));
  return hs.size > 1 ? 'mixed' : [...hs][0];
}
/** The countries a family of that heritage may be drawn from, and the US surname list it draws from there. */
export function countriesFor(L: Lists, heritage: string | undefined): string[] {
  const h = L.rules.heritage, all = Object.keys(L.rules.countries).sort();
  if (!h || !heritage) return all;
  return heritage === 'mixed' ? [...h.mixed.countries].sort() : (h.groups[heritage]?.countries ?? all);
}
export const familyKey = (L: Lists, country: string, heritage: string | undefined) => (country !== 'us' || !heritage ? 'any' : heritage === 'mixed' ? L.rules.heritage!.mixed.family : (L.rules.heritage!.groups[heritage]?.usFamily ?? 'any'));
/** A draw from a whole list (less its top), redrawn on the same stream while the name may not be drawn: an exclusion moves only the people who drew that name. */
const draw = (L: Lists, r: Rng, xs: Entry[]) => { for (let i = 0; i < 500; i++) { const n = r.weighted(xs, x => x[1])[0]; if (allowed(L, n)) return n; } return xs.find(([n]) => allowed(L, n))![0]; };

/** The band a person's age is drawn in. */
export const bandOf = (l: L, figure: string | null, position: string) => (figure && l.ages.byFigure[figure]) || l.ages.byPosition[position] || l.ages.default;
/** The figure the host dresses each person in (HostViews: the cast by their role, everyone else DressCrowd over the walkers in id order), as the engine's people file
 *  would give them: the first stop's place and its mission second (tools/sim/people.ts). */
export function figuresOf(L: Lists, persons: Person[], castRole: Map<string, string>, startsAtS: number): Map<string, string | null> {
  const out = new Map<string, string | null>(), walkers = [...persons].sort((a, b) => (a.id < b.id ? -1 : 1)).filter(p => !castRole.has(p.id));
  for (const [id, role] of castRole) out.set(id, roleFigure(role));
  const job = (p: Person) => (p.work ? (p.work.role ?? p.work.position ?? 'staff') : 'resident');
  const dressed = dressCrowd(L.figures, walkers.map(p => ({ id: p.id, name: '', job: job(p), stops: p.routine.stops.slice(0, 1).map((s): [string, number, number, number] => [s.place, s.from * 60 - startsAtS, s.to * 60 - startsAtS, 0]) })));
  for (const d of dressed) out.set(d.id, d.figure);
  return out;
}
/** The families the family ties make (union of every family tie, either way), each as its members in id order; a person without one is a family of one. */
export function familiesOf(persons: Person[]): string[][] {
  const up = new Map(persons.map(p => [p.id, p.id])), root = (x: string): string => { let r = x; while (up.get(r) !== r) r = up.get(r)!; up.set(x, r); return r; };
  for (const p of persons) for (const t of p.ties) if (t.kind === 'family' && up.has(t.with)) { const a = root(p.id), b = root(t.with); if (a !== b) up.set(a < b ? b : a, a < b ? a : b); }
  const by = new Map<string, string[]>();
  for (const p of [...persons].sort((a, b) => (a.id < b.id ? -1 : 1))) { const r = root(p.id); by.set(r, [...(by.get(r) ?? []), p.id]); }
  return [...by.values()];
}

export type Pins = Map<string, { given?: string; family?: string; country?: string }>;
/** Name every person (sex, age, country, given and family name). `pins`: the pinned name parts by person (rules names.pinned, landed by role). */
export function assignNames(L: Lists, rules: Rules, persons: Person[], pins: Pins, castRole: Map<string, string>, startsAtS: number): string[] {
  const gaps: string[] = [], byId = new Map(persons.map(p => [p.id, p])), figures = figuresOf(L, persons, castRole, startsAtS), used = new Set<string>();
  const countries = Object.entries(L.rules.countries).sort(([a], [b]) => (a < b ? -1 : 1));
  // countries by quota (no country over its share of the people, rules maxOriginShare): the families with a pinned country first, then the rest, largest first and
  // otherwise in the order of a hash of their id, each to the country furthest below its weighted share of the people so far (ties by the family's own draw)
  const fams = familiesOf(persons), pinnedCountry = (m: string[]) => { const pinned = m.find(id => pins.get(id)?.family); return (pinned && pins.get(pinned)!.country) || m.map(id => pins.get(id)?.country).find(Boolean); };
  const W = countries.reduce((n, [, c]) => n + c.weight, 0), have = new Map(countries.map(([c]) => [c, 0])), countryOf = new Map<string, string>();
  // brief 089 follow-up: a family is drawn among the countries of the heritage its figures show (the narrowest first, so they get their places before the open ones)
  const her = new Map(fams.map(m => [m[0]!, familyHeritage(L, m, figures)])), cand = new Map(fams.map(m => [m[0]!, countriesFor(L, her.get(m[0]!))]));
  const order = [...fams].sort((a, b) => Number(!pinnedCountry(a)) - Number(!pinnedCountry(b)) || cand.get(a[0]!)!.length - cand.get(b[0]!)!.length || b.length - a.length || rng(`${a[0]}/country`).f() - rng(`${b[0]}/country`).f() || (a[0]! < b[0]! ? -1 : 1));
  for (const m of order) {
    const r = rng(`${m[0]}/country`), tie = new Map(countries.map(([c]) => [c, r.f()])), ok = cand.get(m[0]!)!;
    const c = pinnedCountry(m) ?? [...countries].filter(([c]) => ok.includes(c)).sort(([a, x], [b, y]) => ((have.get(b)! + m.length) / (y.weight / W) - (have.get(a)! + m.length) / (x.weight / W)) * -1 || tie.get(a)! - tie.get(b)!)[0]![0];
    countryOf.set(m[0]!, c); have.set(c, (have.get(c) ?? 0) + m.length);
  }
  for (const members of fams) {
    const fid = members[0]!, pinned = members.find(id => pins.get(id)?.family), country = countryOf.get(fid)!;
    if (!L.countries.has(country)) { gaps.push(`${fid}: country ${country} has no list`); continue; }
    const family = pinned ? pins.get(pinned)!.family! : draw(L, rng(`${fid}/family`), familyList(L, country, familyKey(L, country, her.get(fid))));
    for (const id of members) {
      const p = byId.get(id)! as Person & { sex?: Sex; age?: number; origin?: string }, pin = pins.get(id), figure = figures.get(id) ?? null;
      const [lo, hi] = bandOf(L.rules, figure, p.position), sex = figureSex(figure ?? ''), age = rng(`${id}/age`).int(lo, hi);
      const own = pin?.family ?? family, surname = country === 'pl' && sex === 'f' && !pin?.family ? womansForm(L, own) : own;
      const pool = givenList(L, country, sex, decadeOf(L, L.countries.get(country)!, age)), r = rng(`${id}/name`);
      let full = '';
      for (let tries = 0; ; tries++) {
        full = `${pin?.given ?? draw(L, r, pool)} ${surname}`;
        if ((!used.has(full) && !L.excluded.full.has(full)) || pin?.given || tries > 200) break;
      }
      if (used.has(full)) gaps.push(`${id}: no unused name for a ${sex} of ${country} born in the ${decadeOf(L, L.countries.get(country)!, age)}s in family ${surname}`);
      used.add(full); p.name = full; p.sex = sex; p.age = age; p.origin = country;
    }
  }
  return gaps;
}

/** P-8 on people named from the lists: every name drawn by the rules, families sharing theirs, the pins where the Handover casts them. */
export function checkNames(L: Lists, rules: Rules, persons: Person[], pinsAt: { pin: Rules['names']['pinned'][number]; who?: string; skipped?: boolean }[], castRole: Map<string, string>, startsAtS: number): string[] {
  const bad: string[] = [], n = rules.names, figures = figuresOf(L, persons, castRole, startsAtS), pinned = new Map<string, Set<string>>();
  for (const { pin, who, skipped } of pinsAt) { if (skipped) continue; if (!who) bad.push(`pinned ${pin.part} name ${pin.name}: nobody to carry it`); else pinned.set(who, new Set([...(pinned.get(who) ?? []), pin.name])); }
  // the lists: enough to draw from, after the skips
  for (const [c, list] of L.countries) {
    if (!n.origins.includes(c)) bad.push(`country ${c} is not one of the rules' origins`);
    for (const d of Object.keys(list.given)) for (const s of ['f', 'm'] as const) if (givenPool(L, c, s, d).length < n.minGiven) bad.push(`${c} ${s} born ${d}s: ${givenPool(L, c, s, d).length} given names to draw from, rules ${n.minGiven}`);
    if (familyPool(L, c).length < n.minFamily) bad.push(`${c}: ${familyPool(L, c).length} family names to draw from, rules ${n.minFamily}`);
  }
  const full = new Set<string>(), share = new Map<string, number>(), famOfId = new Map<string, string>(), herOf = new Map<string, string | undefined>();
  for (const m of familiesOf(persons)) { const h = familyHeritage(L, m, figures); for (const id of m) { famOfId.set(id, m[0]!); herOf.set(id, h); } }
  for (const p of persons as (Person & { sex?: Sex; age?: number; origin?: string })[]) {
    const [g, ...f] = p.name.split(' '), c = p.origin, list = c ? L.countries.get(c) : undefined, mine = pinned.get(p.id) ?? new Set();
    if (!g || f.length !== 1) { bad.push(`${p.id}: ${p.name} is not a given and a family name`); continue; }
    if (!list || p.age === undefined || !p.sex) { bad.push(`${p.id}: no country, age or sex`); continue; }
    share.set(c!, (share.get(c!) ?? 0) + 1);
    if (p.sex !== figureSex(figures.get(p.id) ?? '')) bad.push(`${p.id}: ${p.sex}, but the host dresses them in ${figures.get(p.id)}`);
    const [lo, hi] = bandOf(L.rules, figures.get(p.id) ?? null, p.position);
    if (p.age < lo || p.age > hi) bad.push(`${p.id}: age ${p.age} is outside the band ${lo}-${hi}`);
    if (!mine.has(g) && !givenPool(L, c!, p.sex, decadeOf(L, list, p.age)).some(([x]) => x === g)) bad.push(`${p.id}: ${g} is not a drawable ${p.sex} given name of ${c} for the ${decadeOf(L, list, p.age)}s`);
    // the country fits the heritage the family's figures show (a pinned family keeps its pin's)
    const kinPinned = persons.some(q => famOfId.get(q.id) === famOfId.get(p.id) && pinned.has(q.id));
    if (!kinPinned && !countriesFor(L, herOf.get(p.id)).includes(c!)) bad.push(`${p.id}: ${c}, but the family's figures show ${herOf.get(p.id)} (${countriesFor(L, herOf.get(p.id)).join(', ')})`);
    const fam = f[0]!, base = familyPool(L, c!, familyKey(L, c!, herOf.get(p.id))).some(([x]) => x === fam || (c === 'pl' && p.sex === 'f' && womansForm(L, x) === fam));
    if (!base && !pinned.has(p.id) && !persons.some(q => pinned.get(q.id)?.has(fam))) bad.push(`${p.id}: ${fam} is not a drawable family name of ${c}`);
    if (full.has(p.name)) bad.push(`${p.id}: ${p.name} is used twice`);
    full.add(p.name);
  }
  if (share.size < n.minOrigins) bad.push(`the people come from ${share.size} countries, rules ${n.minOrigins}`);
  for (const [c, k] of share) if (k / persons.length > n.maxOriginShare) bad.push(`${c} is ${Math.round((100 * k) / persons.length)}% of the people, limit ${Math.round(n.maxOriginShare * 100)}%`);
  // families: one family name and one country (a pinned surname is its own and its family's)
  const byId = new Map(persons.map(p => [p.id, p as Person & { origin?: string; sex?: Sex }]));
  for (const members of familiesOf(persons)) {
    const fams = new Set(members.map(id => byId.get(id)!).map(p => familyForm(L, p.origin, p.name.split(' ')[1]!)));
    const cs = new Set(members.map(id => byId.get(id)!.origin));
    if (fams.size > 1 && !members.some(id => pinned.has(id))) bad.push(`family ${members.join(', ')}: ${fams.size} family names (${[...fams].join(', ')})`);
    if (cs.size > 1 && !members.some(id => pinned.has(id))) bad.push(`family ${members.join(', ')}: ${cs.size} countries`);
  }
  // a pinned part is carried once (a given name), or only within its person's family (a family name)
  for (const { pin, who } of pinsAt) {
    if (!who) continue;
    const at = byId.get(who)!, part = (p: Person) => p.name.split(' ')[pin.part === 'given' ? 0 : 1];
    if (part(at) !== pin.name) bad.push(`pinned ${pin.part} name ${pin.name} is not on ${who} (${at.name})`);
    const kin = new Set(familiesOf(persons).find(m => m.includes(who)) ?? [who]);
    for (const p of persons) if (p.id !== who && part(p) === pin.name && (pin.part === 'given' || !kin.has(p.id))) bad.push(`${p.id} (${p.name}) also carries the pinned ${pin.part} name ${pin.name}`);
  }
  return bad;
}
