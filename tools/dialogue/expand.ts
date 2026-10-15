// The expander (Fable, brief 012): voice tables + the people + the rules → each person's lines and scenes (suborned.lines/1).
// Pure and seeded: every choice is fnv1a32 of the seed and ids (the rulebook's hash), never Math.random, so the same inputs give the same bytes.
// A person gets, from the table of their voice group and their own data (position, temperament, need, ties, secret):
//   entries  every cell of their group the lookup finds for each situation they can be in (the most specific cell wins: need and secret
//            qualifiers beat the plain cell), each template filled; the Scout's read-outs about them; for a lover, the summons lines
//   scenes   Listen (with the partner their secret, need and ties choose), Watch (their group's); for a lover the call and the lovers' Watch
// What cannot be made is a gap, reported with the person and the place; the checker (check.ts) turns gaps into faults. Nothing is skipped silently.
import type { Group, Lexicon, Lines, PersonLines, SceneT, Voices } from '../../contracts/dialogue/voices.schema';
import type { People, Person } from '../../contracts/people/people.schema';
import { fnv1a32 } from '../../contracts/rules/rulebook.ref';
import { fill, givenName } from './fill';
import { groupOf, type DialogueRules, type PeopleRules, type Rulebook } from './rules';
import { keyOf, requiredFor, type Key } from './situations';

export type Cell = Group['cells'][number];
export type Merged = { groups: Map<string, Group>; lexicon?: Lexicon; scenes: SceneT[]; scopes: Record<string, string> };
export type Gap = { rule: string; where: string; message: string };
export type ExpandCtx = { people: People; rules: PeopleRules; dr: DialogueRules; rb: Rulebook; tables: Merged; seed: string };
/** What to expand. `groups`: the groups whose persons get their own lines, their Watch read-out and Watch scene. `scenes`: the Listen scenes, and for lovers the call and the lovers' Watch.
 *  `lexicon`: the Scout's Listen read-outs and the lovers' summons lines. Each is asked for only when its tables are part of the run. */
export type Parts = { groups: string[]; scenes: boolean; lexicon: boolean };
export const ALL = (dr: DialogueRules): Parts => ({ groups: Object.keys(dr.groups), scenes: true, lexicon: true });

/** One merged view of the scope files. A group, a block or a scene written twice is a problem, never silently the last one. */
export function merge(vs: Voices[]): { merged: Merged; problems: string[] } {
  const merged: Merged = { groups: new Map(), scenes: [], scopes: {} }, problems: string[] = [], seen = new Set<string>();
  for (const v of vs) {
    for (const g of v.groups) {
      if (merged.groups.has(g.group)) problems.push(`group ${g.group} is written by two files (${merged.scopes[g.group]} and ${v.scope})`);
      merged.groups.set(g.group, g); merged.scopes[g.group] = v.scope;
    }
    if (v.lexicon) { if (merged.lexicon) problems.push(`the lexicon is written by two files (the second is ${v.scope})`); merged.lexicon = v.lexicon; }
    for (const s of v.scenes) {
      const k = sceneId(s);
      if (seen.has(k)) problems.push(`scene ${k} is written twice (the second is in ${v.scope})`);
      seen.add(k); merged.scenes.push(s);
    }
  }
  return { merged, problems };
}
export const sceneId = (s: { verb: string; tie?: string; group?: string; temperament?: string }): string => [s.verb, s.tie ?? '-', s.group ?? '-', s.temperament ?? '-'].join('/');

/** The cell for a query: every field a cell names must equal the query's; the cell naming the most fields wins; file order breaks a tie
 *  (text.schema's `lookup`, with the qualifiers `need` and `secret`). */
export function findCell(cells: readonly Cell[], q: Key & { need?: string; secret?: string | null }): Cell | undefined {
  let best: Cell | undefined, bestScore = -1;
  for (const c of cells) {
    if (c.event !== q.event) continue;
    let score = 0, ok = true;
    for (const f of ['verb', 'outcome', 'temperament', 'need', 'secret'] as const) {
      const have = c[f];
      if (have === undefined) continue;
      if (have !== q[f]) { ok = false; break; }
      score++;
    }
    if (ok && score > bestScore) { best = c; bestScore = score; }
  }
  return best;
}
/** The plain cell of a key: the one a table must have whatever qualified cells it adds. */
export const plainCell = (cells: readonly Cell[], k: Key): Cell | undefined =>
  cells.find(c => c.event === k.event && c.verb === k.verb && c.outcome === k.outcome && c.temperament === k.temperament && c.need === undefined && c.secret === undefined);

export const findScene = (scenes: readonly SceneT[], q: { verb: string; tie?: string; group?: string; temperament?: string }): SceneT | undefined =>
  scenes.find(s => s.verb === q.verb && s.tie === q.tie && s.group === q.group && s.temperament === q.temperament);

/** The tie a person's Listen scene uses: the lover for an affair, then the kind their need points at, then the first kind of the rules' order; among several of a kind, the least hash.
 *  The ties a person holds are their own (a directed tie stands on its source); the partner is `with`. */
export function chooseTie(dr: DialogueRules, p: Person, byId: Map<string, Person>, seed: string): { kind: string; other?: Person } {
  const mine = p.ties.filter(t => byId.has(t.with));
  const of = (kind: string) => mine.filter(t => t.kind === kind)
    .sort((a, b) => fnv1a32(`${seed}|${p.id}|${kind}|${a.with}`) - fnv1a32(`${seed}|${p.id}|${kind}|${b.with}`) || (a.with < b.with ? -1 : 1))[0];
  const pick = (kind: string | undefined) => { const t = kind ? of(kind) : undefined; return t ? { kind: t.kind, other: byId.get(t.with)! } : undefined; };
  if (dr.listen.secretFirst && p.secret === 'affair') { const x = pick('lover'); if (x) return x; }
  const aligned = pick(dr.listen.tieByNeed[p.need]);
  if (aligned) return aligned;
  for (const kind of dr.listen.tieOrder) { const x = pick(kind); if (x) return x; }
  return { kind: 'none' };
}

/** What the cell calls the partner: their position's noun, or the rules' word for kin. */
export function otherNoun(c: ExpandCtx, p: Person, other: Person): string | undefined {
  if (p.ties.some(t => t.with === other.id && t.kind === 'family')) return c.dr.slots.familyNoun;
  return c.tables.lexicon?.positions.find(x => x.position === other.position)?.noun;
}

/** The slots a person's own words are filled from. A slot a person lacks is absent, never empty. */
export function slotsOf(c: ExpandCtx, p: Person, g: Group | undefined): Record<string, string | undefined> {
  const lex = c.tables.lexicon, unit = c.people.units.find(u => u.id === p.work?.unit);
  const pos = lex?.positions.find(x => x.position === p.position), need = lex?.needs.find(x => x.need === p.need);
  return {
    address: g ? g.address[fnv1a32(`${c.seed}|${p.id}|address`) % g.address.length] : undefined,
    job: pos?.job, noun: pos?.noun, work: unit ? `the ${unit.name}` : undefined,
    need: need?.noun, own: need?.own, met: need?.met,
  };
}

export function expandPerson(c: ExpandCtx, p: Person, byId: Map<string, Person>, parts: Parts, gaps: Gap[]): PersonLines | undefined {
  const { dr, rules, rb, tables: T } = c, grp = groupOf(dr, p.position), temperament = p.temperament as PersonLines['temperament'];
  const where = (w = '') => `${p.id} (${p.position}, ${p.temperament})${w ? ' ' + w : ''}`;
  if (!grp) { gaps.push({ rule: 'D-1', where: where(), message: `position ${p.position} is in no voice group (unknown = reject)` }); return undefined; }
  const g = T.groups.get(grp), mine = parts.groups.includes(grp), slots = slotsOf(c, p, g), entries: PersonLines['entries'] = [], scenes: PersonLines['scenes'] = [];
  const filled = (templates: readonly string[], values: Record<string, string | undefined>, w: string): string[] => templates.map(t => {
    const f = fill(t, values);
    for (const m of new Set(f.missing)) gaps.push({ rule: 'D-3', where: where(w), message: `slot {${m}} has no value for this person` });
    return f.text;
  });
  const lover = p.ties.find(t => t.kind === 'lover' && byId.has(t.with)), loverP = lover ? byId.get(lover.with) : undefined;
  const sceneSlots = (other?: Person): Record<string, string | undefined> => ({
    focus: givenName(p.name), other: other && givenName(other.name), focus_noun: slots.noun, other_noun: other && otherNoun(c, p, other),
    work: slots.work, need: slots.need, own: slots.own, met: slots.met,
  });
  const play = (s: SceneT, other: Person | undefined, w: string): PersonLines['scenes'][number]['variants'] => s.variants.map(v => ({
    beats: v.beats.map(b => ({
      speaker: b.who === 'focus' ? p.id : b.who === 'other' ? (other ? other.id : 'bystander') : b.who === 'scout' ? 'scout' : 'bystander',
      line: filled([b.line], sceneSlots(other), w)[0]!,
    })),
  }));
  /** The Learned line, from the rules' words and the person's own data; `{temperament}` is left for the host (engine state). */
  const result = (tpl: string, other: Person | undefined, w: string): string => {
    const f = fill(tpl, { focus: givenName(p.name), other: other && givenName(other.name), other_noun: other && otherNoun(c, p, other), need_fact: dr.listen.needFacts[p.need] });
    for (const m of new Set(f.missing.filter(x => x !== 'temperament'))) gaps.push({ rule: 'D-3', where: where(w), message: `result slot {${m}} has no value for this person` });
    return f.text;
  };

  // ---- their own lines, and the Scout's Watch read-out
  if (g && mine) {
    for (const key of requiredFor(dr, rb, rules, grp, temperament)) {
      const cell = findCell(g.cells, { ...key, need: p.need, secret: p.secret });
      if (!cell) { gaps.push({ rule: 'D-1', where: where(keyOf(key)), message: 'the table has no cell for this' }); continue; }
      entries.push({ ...pickKey(key), speaker: 'person', lines: filled(cell.lines, slots, keyOf(key)) });
    }
    entries.push({ event: 'read', verb: 'watch', speaker: 'scout', lines: filled(g.reads.watch, slots, 'read/watch') });
    const w = findScene(T.scenes, { verb: 'watch', group: grp });
    if (!w) gaps.push({ rule: 'D-1', where: where(`watch/${grp}`), message: 'the tables have no Watch scene for this group' });
    else scenes.push({ verb: 'watch', tie: 'none', learns: ['routine'], result: result(dr.listen.results.watch, undefined, 'watch result'), variants: play(w, undefined, `watch/${grp}`) });
  }
  // ---- the Scout's Listen read-out, the lovers' summons
  if (parts.lexicon && T.lexicon) {
    const r = T.lexicon.reads.find(x => x.temperament === temperament);
    if (r) entries.push({ event: 'read', verb: 'listen', temperament, speaker: 'scout', lines: filled(r.lines, slots, 'read/listen') });
    if (loverP) for (const s of T.lexicon.summons) for (const side of ['caller', 'summoned'] as const) {
      entries.push({ event: 'summon', outcome: s.key, side, speaker: 'person', lines: filled(s[side], { other: givenName(loverP.name), other_noun: otherNoun(c, p, loverP) }, `summon/${s.key}`) });
    }
  }
  // ---- Listen, and for a lover the call and the lovers' Watch
  if (parts.scenes) {
    const tie = chooseTie(dr, p, byId, c.seed), s = findScene(T.scenes, { verb: 'listen', tie: tie.kind, temperament });
    if (!s) gaps.push({ rule: 'D-1', where: where(`listen/${tie.kind}`), message: 'the tables have no Listen scene for this tie and temperament' });
    else {
      const affair = p.secret === 'affair' && tie.kind === 'lover', record = p.secret === 'record';
      const res = dr.listen.results.listen + (affair ? dr.listen.results.secretAffair : record ? dr.listen.results.secretRecord : '');
      scenes.push({ verb: 'listen', tie: tie.kind as never, ...(tie.other ? { with: tie.other.id as never } : {}), learns: ['need', 'temperament', ...(affair || record ? ['secret' as const] : [])], result: result(res, tie.other, 'listen result'), variants: play(s, tie.other, `listen/${tie.kind}/${temperament}`) });
    }
    if (loverP) {
      const call = findScene(T.scenes, { verb: 'call', tie: 'lover', temperament }), wl = findScene(T.scenes, { verb: 'watch', tie: 'lover' });
      if (!call) gaps.push({ rule: 'D-1', where: where(`call/lover/${temperament}`), message: 'the tables have no call scene for a lover in this temperament' });
      else scenes.push({ verb: 'call', tie: 'lover', with: loverP.id as never, learns: [], result: result(dr.listen.results.call, loverP, 'call result'), variants: play(call, loverP, `call/lover/${temperament}`) });
      if (!wl) gaps.push({ rule: 'D-1', where: where('watch/lover'), message: 'the tables have no Watch scene for lovers' });
      else scenes.push({ verb: 'watch', tie: 'lover', with: loverP.id as never, learns: ['routine', 'secret'], result: result(dr.listen.results.watchLovers, loverP, 'watch result'), variants: play(wl, loverP, 'watch/lover') });
    }
  }
  return { id: p.id as never, group: grp, temperament, need: p.need, secret: p.secret ?? null, address: slots.address ?? '', entries, scenes };
}
const pickKey = (k: Key) => ({ event: k.event, ...(k.verb ? { verb: k.verb } : {}), ...(k.outcome ? { outcome: k.outcome } : {}), ...(k.temperament ? { temperament: k.temperament } : {}) });

export function expandAll(c: ExpandCtx, parts: Parts): { persons: PersonLines[]; gaps: Gap[] } {
  const byId = new Map(c.people.persons.map(p => [p.id, p])), gaps: Gap[] = [], persons: PersonLines[] = [];
  const wanted = (p: Person) => parts.scenes || parts.lexicon || parts.groups.includes(groupOf(c.dr, p.position) ?? '');
  for (const p of [...c.people.persons].sort((a, b) => (a.id < b.id ? -1 : 1))) {
    if (!wanted(p)) continue;
    const x = expandPerson(c, p, byId, parts, gaps);
    if (x) persons.push(x);
  }
  return { persons, gaps };
}

/** The suborned.lines/1 file for the persons expanded. `inputs` are the hashes the caller holds. */
export function linesFile(c: ExpandCtx, persons: PersonLines[], inputs: Lines['inputs'], plan: Lines['plan']): Lines {
  return { schema: 'suborned.lines/1', plan, seed: c.seed, inputs, persons, debrief: (c.tables.lexicon?.debrief ?? []).map(d => ({ id: d.id, lines: d.lines })) };
}
