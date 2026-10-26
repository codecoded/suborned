// Done-check for the Handover's words (Fable, brief 011): contracts/missions/handover/text.json parses as suborned.text/1, every engine
// verb, refusal and fail has text, every reply the rulebook can produce has a line for each role and temperament, every line fits (≤ 80
// characters, spoken lines 1–12 words), nobody uncast is named, the tone filters pass, and the pick is deterministic.
// The engine is read from source (sim/Suborned.Sim/*.cs) so a new refusal, fail or log code with no text fails here, not in play.
// Run: bun test contracts/missions/handover
import { describe, expect, test } from 'bun:test';
import { existsSync, readdirSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { shownWords } from '../../../tools/hud-budget/budget.ts';
import { Cast } from '../../people/cast.schema.ts';
import { checkPeople } from '../../../tools/people/check.ts';
import { expand } from '../../../tools/people/expand.ts';
import { readTexts, world } from '../../../tools/people/inputs.ts';
import { PeopleRules, TEMPERAMENTS, allowedTemperaments } from '../../people/people.rules.schema.ts';
import { Rulebook } from '../../rules/rulebook.schema.ts';
import * as R from '../../rules/rulebook.ref.ts';
import { AGENTS, BEAT_MAX, CARD_FATES, CONTROL_VERBS, FAILS, FATES, RETIRED_FAILS, HINT_MAX, MAX_LINE, MAX_WORDS, OUTCOMES, PEOPLE, PRICE_FIELDS, SHORT_MAX, TextFile, VERBS, WATCHED, cardResult, fillCard, fillLock, fillLockShort, holdersPhrase, roleOfId, lineKey, lookup, lookupScene, marked, pickIndex, pickLine, playScene, sceneKey, scenesFor, wordCount } from './text.schema.ts';

const root = join(import.meta.dir, '..', '..', '..');
const read = (p: string) => readFileSync(join(root, p), 'utf8');
const json = (p: string) => JSON.parse(read(p));
const raw = json('contracts/missions/handover/text.json');
const t = TextFile.parse(raw);
const rb = Rulebook.parse(json('contracts/rules/rulebook.json'));
const people = PeopleRules.parse(json('contracts/people/people.rules.json'));
const needsKinds = json('contracts/people/needs.rules.json') as { kinds: Record<string, unknown>; secretKinds: Record<string, unknown> };
const mission = json('contracts/missions/handover/mission.json') as { timeline: { at: string; what: string }[] };
const SIM = join(root, 'sim', 'Suborned.Sim');
const haveSim = existsSync(SIM);
const simFile = (f: string) => read(`sim/Suborned.Sim/${f}`);
const simAll = () => readdirSync(SIM).filter((f) => f.endsWith('.cs')).map((f) => simFile(f));

// ---------------------------------------------------------------- every string in the file, with what kind it is
type Str = { path: string; text: string; spoken: boolean; max?: number };
const strings: Str[] = [];
const push = (path: string, text: string, spoken = false, max?: number) => strings.push({ path, text, spoken, max });
{
  const b = t.briefing;
  b.card.forEach((s, i) => push(`briefing.card[${i}]`, s));
  push('briefing.objective', b.objective); push('briefing.win', b.win); push('briefing.clock.line', b.clock.line); push('briefing.purse.line', b.purse.line);
  push('briefing.hints.front-door', b.hints['front-door']); push('briefing.hints.pickup', b.hints.pickup); push('briefing.tracker', b.tracker);
  for (const [k, s] of Object.entries(b.fails)) push(`briefing.fails.${k}`, s);
  b.timeline.forEach((x, i) => push(`briefing.timeline[${i}]`, x.label));
  for (const [k, s] of Object.entries(b.cell)) push(`briefing.cell.${k}`, s);
  push('briefing.page.target', b.page.target); push('briefing.page.ending', b.page.ending);
  for (const [k, w] of Object.entries(b.page.ways)) { push(`briefing.page.ways.${k}.when`, w.when, false, 16); push(`briefing.page.ways.${k}.line`, w.line); }
  for (const [k, s] of Object.entries(b.page.cell)) push(`briefing.page.cell.${k}`, s);
  for (const [k, s] of Object.entries(b.page.heads.ways)) push(`briefing.page.heads.ways.${k}`, s, false, 16);
  for (const k of ['where', 'clock', 'heat'] as const) push(`briefing.page.heads.${k}`, b.page.heads[k], false, 16);
  for (const [k, s] of Object.entries(b.page.captions ?? {})) push(`briefing.page.captions.${k}`, s, false, 40);
  for (const [k, x] of Object.entries(t.targets)) {
    push(`targets.${k}.about`, x.about);
    if (x.watched) push(`targets.${k}.watched`, 'Learned: ' + x.watched); if (x.routine) push(`targets.${k}.routine`, 'Learned: ' + x.routine); if (x.heard) push(`targets.${k}.heard`, x.heard);
  }
  for (const [k, c] of Object.entries(t.cards.items)) { push(`cards.${k}.title`, c.title); for (const [v, r] of typeof c.result === 'string' ? [['', c.result] as const] : Object.entries(c.result ?? {})) push(`cards.${k}.result.${v}`, r); }
  { const d = t.debrief; const walkd = (path: string, o: unknown) => { if (path === 'debrief.rule') return; if (typeof o === 'string') push(path, o); else for (const [k, v] of Object.entries(o as object)) walkd(`${path}.${k}`, v); }; walkd('debrief', d); }
  t.scenes.items.forEach((sc, i) => { if (sc.title) push(`scenes[${i}].title`, sc.title); });
  for (const [k, a] of Object.entries(t.actions)) { push(`actions.${k}.does`, a.does); if (a.withTarget) push(`actions.${k}.withTarget`, a.withTarget); if (a.then) push(`actions.${k}.then`, a.then); for (const [r, s] of Object.entries(a.onTarget ?? {})) push(`actions.${k}.onTarget.${r}`, s); for (const [g, m] of Object.entries(a.onTargetBy ?? {})) for (const [r, x] of Object.entries(m)) push(`actions.${k}.onTargetBy.${g}.${r}`, x); for (const [w, x] of Object.entries(a.ways ?? {})) { push(`actions.${k}.ways.${w}.label`, x.label); push(`actions.${k}.ways.${w}.does`, x.does); } }
  t.scenes.items.forEach((sc, i) => {
    push(`scenes[${i}].result ${sceneKey(sc)}`, sc.result);
    sc.variants.forEach((v, j) => v.beats.forEach((b, k) => { if ('line' in b) push(`scenes[${i}].variants[${j}].beats[${k}] ${sceneKey(sc)}`, b.line, true); }));
  });
  for (const [id, h] of Object.entries(t.hints.items)) push(`hints.${id}`, h.line, false, HINT_MAX);
  for (const [k, x] of Object.entries(t.toasts)) push(`toasts.${k}`, x.line, false, HINT_MAX);
  push('goingLoud.captured', t.goingLoud.captured);
  for (const [k, x] of Object.entries(t.goingLoud.fails)) push(`goingLoud.fails.${k}`, x);
  for (const [k, x] of Object.entries(t.cancelCosts)) push(`cancelCosts.${k}`, x);
  const walk = (path: string, o: unknown) => { if (typeof o === 'string') push(path, o); else for (const [k, v] of Object.entries(o as object)) walk(`${path}.${k}`, v); };
  walk('activity', t.activity);
  for (const [code, l] of Object.entries(t.locks)) { push(`locks.${code}`, l.line); push(`locks.${code}.short`, l.short, false, SHORT_MAX); }
  // brief 014: the short forms are strings the player reads, so the same length, tone, name and key rules apply (brackets mark a ringed noun and are not text)
  const plain = (x: string) => x.replace(/<[A-Za-z]+>/g, 'k').replace(/[[\]]/g, ''); // a key token is one keycap
  b.timeline.forEach((x, i) => push(`briefing.timeline[${i}].short`, x.short, false, 16));
  for (const [id, h] of Object.entries(t.hints.items)) { push(`hints.${id}.short`, plain(h.short), false, 36); if (h.door) { push(`hints.${id}.door`, h.door.line, false, HINT_MAX); push(`hints.${id}.door.short`, plain(h.door.short), false, 36); } }
  for (const [k, c] of Object.entries(t.cards.items)) for (const [v, r] of typeof c.beat === 'string' ? [['', c.beat] as const] : Object.entries(c.beat ?? {})) push(`cards.${k}.beat.${v}`, r, false, BEAT_MAX);
  t.scenes.items.forEach((sc, i) => { if (sc.beat) push(`scenes[${i}].beat ${sceneKey(sc)}`, sc.beat, false, BEAT_MAX); });
  { const L = t.less;
    push('less.objective.take', plain(L.objective.take), false, 32); push('less.objective.bring', plain(L.objective.bring), false, 32); push('less.objective.carry', plain(L.objective.carry), false, 32); push('less.briefing.goal', plain(L.briefing.goal), false, 32);
    push('less.end.win', L.end.win, false, SHORT_MAX); push('less.end.withdraw', L.end.withdraw, false, SHORT_MAX);
    for (const [k, x] of Object.entries(L.end.fail)) push(`less.end.fail.${k}`, x, false, SHORT_MAX);
    for (const [k, x] of Object.entries(L.pop)) push(`less.pop.${k}`, x, false, SHORT_MAX);
    for (const [f, m] of Object.entries(L.failcam.caption)) for (const [w, x] of Object.entries(m)) push(`less.failcam.caption.${f}.${w}`, x, false, 56);
    for (const [f, m] of Object.entries(L.failcam.tip)) for (const [w, x] of Object.entries(m)) push(`less.failcam.tip.${f}.${w}`, x, false, 36);
    for (const [k, x] of Object.entries(L.cancel)) push(`less.cancel.${k}`, x, false, 20);
    { const D = L.done; for (const [k, x] of Object.entries(D.plate)) push(`less.done.plate.${k}`, x, false, 12); for (const [k, x] of Object.entries(D.gain)) push(`less.done.gain.${k}`, x.replace('{s}', '120'), false, 16);
      for (const [k, x] of Object.entries(D.close)) push(`less.done.close.${k}`, x, false, 16); for (const [k, x] of Object.entries(D.cancel.cost)) push(`less.done.cancel.cost.${k}`, x.replace('{s}', '120'), false, 16);
      for (const [k, x] of Object.entries(D.fire)) push(`less.done.fire.${k}`, x, false, 12);
      for (const [k, x] of Object.entries(D.run)) push(`less.done.run.${k}`, x); // engine 0.34.0: the Alt chip of a run, an Alt line
      push('less.done.walk.plate',D.walk.plate.replace('{who}', 'passer-by'), false, 16); for (const [k, x] of Object.entries(D.walk.who)) push(`less.done.walk.who.${k}`, x, false, 12); }
    for (const [k, x] of Object.entries(L.alt.outcome)) push(`less.alt.outcome.${k}`, x);
    for (const [k, x] of Object.entries(L.alt.mood)) push(`less.alt.mood.${k}`, x);
    for (const k of ['footer', 'footerHurt', 'footerBare'] as const) push(`less.mood.${k}`, L.mood[k], false, 44);
    for (const k of ['likely', 'hurt'] as const) push(`less.mood.${k}`, L.mood[k], false, 10);
    for (const [k, x] of Object.entries(L.sealed.line)) push(`less.sealed.line.${k}`, x);
    for (const [k, x] of Object.entries(L.sealed.tip)) push(`less.sealed.tip.${k}`, x, false, 36);
    for (const [k, x] of Object.entries(L.sealed.story)) push(`less.sealed.story.${k}`, x);
    for (const [g, m] of Object.entries({ need: L.intel.need, temperament: L.intel.temperament, secret: L.intel.secret, verb: L.intel.verb })) for (const [k, x] of Object.entries(m)) push(`less.intel.${g}.${k}`, x.replace('{name}', 'Inez'), false, 20); }
  t.dialogue.forEach((e, i) => e.lines.forEach((s, j) => push(`dialogue[${i}].lines[${j}] ${lineKey(e)}`, s, true)));
  // briefs 058, 059, 062 (GDD v0.25.0): the briefing's labels, and the words waiting for the engine (`pending`), held to the same limits as the live ones
  for (const k of ['begin', 'target'] as const) push(`less.briefing.${k}`, t.less.briefing[k], false, 24);
  for (const [k, x] of Object.entries(t.less.briefing.ways)) push(`less.briefing.ways.${k}`, x, false, 16);
  const pe = t.pending;
  if (pe) {
    for (const [c, l] of Object.entries(pe.locks)) { push(`pending.locks.${c}`, l.line); push(`pending.locks.${c}.short`, l.short, false, SHORT_MAX); }
    for (const [c, x] of Object.entries(pe.cards)) {
      push(`pending.cards.${c}.title`, x.title); for (const [v, r] of typeof x.result === 'string' ? [['', x.result] as const] : Object.entries(x.result ?? {})) push(`pending.cards.${c}.result.${v}`, r);
      for (const [v, r] of typeof x.beat === 'string' ? [['', x.beat] as const] : Object.entries(x.beat ?? {})) push(`pending.cards.${c}.beat.${v}`, r, false, BEAT_MAX);
    }
    pe.dialogue.forEach((e, i) => e.lines.forEach((l, j) => push(`pending.dialogue[${i}].lines[${j}] ${lineKey(e)}`, l, true)));
    for (const [k, a] of Object.entries(pe.actions)) { push(`pending.actions.${k}.does`, a.does); if (a.withTarget) push(`pending.actions.${k}.withTarget`, a.withTarget); if (a.then) push(`pending.actions.${k}.then`, a.then); for (const [r, x] of Object.entries(a.onTarget ?? {})) push(`pending.actions.${k}.onTarget.${r}`, x); }
    for (const [k, x] of Object.entries(pe.activity)) push(`pending.activity.${k}`, x);
    for (const [k, x] of Object.entries(pe.targets)) { if (x.about) push(`pending.targets.${k}.about`, x.about); if (x.watched) push(`pending.targets.${k}.watched`, 'Learned: ' + x.watched); }
    for (const [id, h] of Object.entries(pe.hints)) { push(`pending.hints.${id}`, h.line, false, HINT_MAX); push(`pending.hints.${id}.short`, h.short.replace(/[[\]]/g, ''), false, 36); }
  }
}
/** A line with the host's placeholders filled at their widest. */
const widest = (s: string) => s.replace(/\{who\}/g, 'the councillor').replace(/\{op\}/g, 'Enforcer').replace(/\{m\}/g, '13').replace(/\{usd\}/g, '$100,000').replace(/\{target\}/g, 'the entrance camera').replace(/\{tick\}/g, '10:30').replace(/\{need\}/g, 'protection').replace(/\{temperament\}/g, 'Mercenary').replace(/\{alert\}/g, '3').replace(/\{agent\}/g, 'the Enforcer').replace(/\{n\}/g, '12').replace(/\{clock\}/g, '10:30').replace(/\{trail\}/g, '30').replace(/\{hits\}/g, '12').replace(/\{cause\}/g, "the guard's trip to the café looks staged").replace(/\{list\}/g, 'Inez, who can introduce you at the café.')
  // brief 013's between-missions fields: the widest the host fills each (the same widths as contracts/dialogue debriefFill, with a short place name)
  .replace(/\{fact\}/g, "the Fixer's face").replace(/\{facts\}/g, "place, time and the Enforcer's face").replace(/\{place\}/g, 'the service lane').replace(/\{of\}/g, 'Dimitra Marchetti').replace(/\{move\}/g, 'bravery +1')
  .replace(/\{fear\}/g, 'fear +1').replace(/\{guards\}/g, '2').replace(/\{cameras\}/g, '2').replace(/\{missions\}/g, '3').replace(/\{kind\}/g, 'found killing').replace(/\{closes\}/g, 'when the body is found')
  .replace(/\{s\}/g, '99').replace(/\{holders\}/g, '2 rivals').replace(/\{mood\}/g, 'friendly').replace(/\{lever\}/g, 'pressure').replace(/\{fate\}/g, 'walked out').replace(/\{why\}/g, 'the district was sealed').replace(/\{held-at\}/g, 'the post').replace(/\{holds\}/g, '3 missions').replace(/\{rescue-until\}/g, '3 missions').replace(/\{by\}/g, 'A police officer')
  .replace(/\{district\}/g, 'Harbour').replace(/\{band\}/g, 'Neutral').replace(/\{from\}/g, '-3').replace(/\{to\}/g, '-3');

describe('the file', () => {
  test('parses as suborned.text/1 for the handover', () => {
    expect(t.mission).toBe('handover');
    expect(t.dialogue.length).toBeGreaterThan(200);
  });
  test('every line is at most 80 characters, placeholders filled at their widest', () => {
    const long = strings.filter((s) => widest(s.text).length > (s.max ?? MAX_LINE)).map((s) => `${s.path}: ${s.text} (${widest(s.text).length})`);
    expect(long).toEqual([]);
  });
  test('a spoken line is 1–12 words, has no stray spacing, and ends like a sentence', () => {
    const bad = strings.filter((s) => s.spoken).filter((s) => wordCount(s.text) < 1 || wordCount(s.text) > MAX_WORDS || s.text !== s.text.trim() || /\s{2}/.test(s.text) || !/[.!?]$/.test(s.text)).map((s) => `${s.path}: ${s.text}`);
    expect(bad).toEqual([]);
  });
  test('placeholders are only {who} {op} {m} {usd} {tick} {target} {need} {temperament} {alert} and the cards\' {agent} {n} {clock} {trail} {hits} {cause} {list}, and the between-missions lines\' {fact} {facts} {place} {of} {move} {fear} {guards} {cameras} {missions} {kind} {closes}, and the Standing line\'s {district} {band} {from} {to}, and brief 018\'s {holders} (locks) {fate} {why} {held-at} {holds} {rescue-until} {by} (the getaway lines)', () => {
    const bad = strings.filter((s) => /\{(?!who\}|op\}|m\}|usd\}|tick\}|target\}|need\}|temperament\}|alert\}|agent\}|n\}|clock\}|trail\}|hits\}|cause\}|list\}|fact\}|facts\}|place\}|of\}|move\}|fear\}|guards\}|cameras\}|missions\}|kind\}|closes\}|district\}|band\}|from\}|to\}|s\}|holders\}|mood\}|lever\}|fate\}|why\}|held-at\}|holds\}|rescue-until\}|by\})[^}]*\}/.test(s.text)).map((s) => `${s.path}: ${s.text}`);
    expect(bad).toEqual([]);
  });
  test('no two spoken entries repeat a line, and no entry repeats its own', () => {
    const seen = new Map<string, string>(), dup: string[] = [];
    for (const e of t.dialogue) {
      expect(new Set(e.lines).size).toBe(e.lines.length);
      for (const l of e.lines) { const k = lineKey(e); if (seen.has(l) && seen.get(l) !== k) dup.push(`"${l}" in ${seen.get(l)} and ${k}`); seen.set(l, k); }
    }
    expect(dup).toEqual([]);
  });
  test('no two entries share a key (the lookup would be ambiguous)', () => {
    const seen = new Set<string>(), dup: string[] = [];
    for (const e of t.dialogue) { const k = lineKey(e) + '|' + (e.about ?? ''); if (seen.has(k)) dup.push(k); seen.add(k); }
    expect(dup).toEqual([]);
  });
});

describe('tone and names', () => {
  const all = strings.map((s) => s.text.toLowerCase());
  test('no modern speech or period-slang overload', () => {
    const hits: string[] = [];
    for (const w of t.tone.avoid) { const re = new RegExp(`(^|[^a-z])${w.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}($|[^a-z])`); strings.forEach((s) => { if (re.test(s.text.toLowerCase())) hits.push(`${w}: ${s.path}`); }); }
    expect(hits).toEqual([]);
  });
  test('no strong profanity (mild is allowed)', () => {
    const hits: string[] = [];
    for (const w of t.tone.profanity) for (const s of strings) if (new RegExp(`\\b${w}`).test(s.text.toLowerCase())) hits.push(`${w}: ${s.path}`);
    expect(hits).toEqual([]);
    expect(all.some((s) => /\bdamn\b/.test(s))).toBe(true); // the mild word is in use, so the filter is not silently over-strict
  });
  test('civilians are never the payload: no line asks for a human shield, a suicide or a rigged person', () => {
    const hits = strings.filter((s) => /human shield|suicide|strapped|blow yourself|detonat|sacrifice/i.test(s.text)).map((s) => s.path);
    expect(hits).toEqual([]);
  });
  test('only Inez and Vale are named; no one else from the city\'s name pool', () => {
    const cast = json('contracts/people/harbour-district.cast.json') as { names: { given: { name: string }[]; family: { name: string }[] } };
    const pool = new Set([...cast.names.given, ...cast.names.family].map((n) => n.name));
    const named = new Set<string>(t.names.named);
    expect([...named].sort()).toEqual(['Inez', 'Vale']);
    const hits: string[] = [];
    for (const s of strings) for (const w of s.text.match(/[\p{L}'-]+/gu) ?? []) if (pool.has(w) && !named.has(w)) hits.push(`${w}: ${s.path}`);
    expect(hits).toEqual([]);
  });
  test('no capitalised word mid-sentence other than the cast, the places, the verbs and the agents', () => {
    const ok = new Set(['Inez', 'Vale', 'Scout', 'Fixer', 'Tech', 'Enforcer', 'Councillor', 'Council', 'Office', 'Records', 'Civic', 'Police', 'Handover',
      'Return', 'Ctrl', 'I', 'F', 'G', 'N', 'Q', 'E', 'X', 'Trail', 'Alert', 'Ask', 'Pay', 'Help', 'Pressure', 'Call', 'Watch', 'Listen', 'Loop', 'Reroute', 'Enter', 'Take', 'Fire', 'Carry', 'Board', 'Go', 'Kill', 'Halcyon', 'Security', 'Disable', 'Drive', 'Orbit', 'Walk']); // Disable and Drive: briefs 058, 059; Orbit Walk: the street the rival car turns in from (brief 059); X: the cease-fire key H21 teaches (brief 067)
    const hits: string[] = [];
    for (const s of strings) {
      const toks = s.text.split(/\s+/);
      toks.forEach((tok, i) => {
        const w = tok.replace(/^[^\p{L}]+|[^\p{L}]+$/gu, '');
        if (!w || !/^\p{Lu}/u.test(w)) return;
        const prev = i === 0 ? '.' : toks[i - 1]!;
        if (/[.!?:·]$/.test(prev) || (i === 0)) return; // sentence start
        if (!ok.has(w.replace(/'(s|m|ve|ll|d|re)$/, ''))) hits.push(`${w}: ${s.path}`);
      });
    }
    expect(hits).toEqual([]);
  });
  test('Halcyon appears only in lines marked pending Sky, and an unnamed cliffhanger exists for each', () => {
    for (const e of t.dialogue) if (e.lines.some((l) => /halcyon/i.test(l))) expect(e.pending).toBeTruthy();
    for (const s of strings) if (!s.spoken) expect(/halcyon/i.test(s.text)).toBe(false);
    for (const e of t.dialogue.filter((x) => x.pending)) expect(e.outcome).toBe('named');
    const keys = (o: string) => t.dialogue.filter((e) => e.event === 'cliffhanger' && e.outcome === o).map((e) => `${e.speaker}/${e.temperament ?? ''}`).sort();
    expect(keys('named')).toEqual(keys('unnamed'));
    expect(keys('named').length).toBe(4);
  });
});

describe('brief 043: the done moment\'s words', () => {
  const D = t.less.done;
  const words = (x: string) => x.replace(/\{[a-z]+\}/g, '').split(/\s+/).filter((w) => /\p{L}/u.test(w)).length;
  test('a plate word is one or two words, for every outcome the engine gives and for done and cancelled', () => {
    for (const k of [...OUTCOMES, 'done', 'cancelled']) expect(D.plate[k], k).toBeTruthy();
    for (const [k, x] of Object.entries(D.plate)) { expect(words(x), k).toBeLessThanOrEqual(2); expect(x, k).toBe(x.toLowerCase()); expect(x, k).not.toMatch(/[.!?]/); }
  });
  test('a squad fire order that skips an agent says why in two words, for every refusal FireQuery gives (engine 0.26.0)', () => {
    const codes = ['mission-over', 'agent-down', 'enforcer-inside', 'agent-busy', 'in-car', 'no-weapon', 'fire-carrying', 'fire-carrying-vale', 'no-target', 'fire-on-cell', 'target-unknown', 'civilian-target', 'out-of-range', 'no-clear-line', 'engine-error'];
    for (const k of codes) { const x = D.fire[k]; expect(x, k).toBeTruthy(); expect(words(x), k).toBeLessThanOrEqual(2); expect(x, k).not.toMatch(/[.!?,{}]/); }
    expect(Object.keys(D.fire).sort()).toEqual([...codes].sort());
  });
  test('a gain chip line is one to three words, a label and not a sentence; a window close is two', () => {
    for (const [k, x] of Object.entries(D.gain)) expect(words(x), k).toBeLessThanOrEqual(3); // brief 058: "Camera shot out" is three
    for (const [k, x] of [...Object.entries(D.gain).filter(([k]) => k !== 'camera-shot'), ...Object.entries(D.close)]) { expect(words(x), k).toBeLessThanOrEqual(2 + (x.includes('{s}') ? 1 : 0)); expect(x, k).not.toMatch(/[.!?,]/); }
    for (const k of ['guard', 'camera', 'patrol', 'rivals', 'disable']) expect(D.close[k], k).toBeTruthy();
  });
  test('the cancel tag and pop fit once filled at their widest, and every costly cancel cost has a word', () => {
    const fill = (x: string, cost: string) => x.replace('{verb}', 'Pressure').replace('{cost}', cost).replace('{s}', '120');
    const wide = Math.max(...Object.values(D.cancel.cost).map((c) => fill(c, '').length));
    expect(fill(D.cancel.tag, '')).toBe('Cancels Pressure'); expect(fill(D.cancel.pop, '')).toBe('Pressure cancelled');
    for (const [k, c] of Object.entries(D.cancel.cost)) { expect(Object.keys(t.cancelCosts), k).toContain(k); expect(fill(D.cancel.tagCost, fill(c, '')).length, k).toBeLessThanOrEqual(36); expect(fill(D.cancel.popCost, fill(c, '')).length, k).toBeLessThanOrEqual(36); }
    expect(wide).toBeLessThanOrEqual(16);
    expect(fill(D.cancel.tagCost, fill(D.cancel.cost.lost!, ''))).toBe('Cancels Pressure · 120 s lost');
  });
  test('the walk-in plate names every cast target and the line reads with the ring label', () => {
    for (const k of Object.keys(t.names.targets)) if (!/^(door|security|hostile|stash-|stand-)/.test(k)) expect(D.walk.who[k], k).toBeTruthy(); // a stand is driven to from inside the car: no walk-in plate
    expect(D.walk.line.replace('{target}', t.names.targets.waitress!).replace('{verb}', 'Listen')).toBe('to Inez · Listen');
    expect(D.walk.plate.replace('{who}', D.walk.who.waitress!)).toBe('→ Inez');
  });
});

describe('times and numbers agree with the mission', () => {
  test('every m:ss in the words is a mark of the mission timeline', () => {
    const marks = new Set(mission.timeline.map((x) => x.at));
    const bad: string[] = [];
    for (const s of strings) for (const m of s.text.match(/\b\d{1,2}:\d{2}\b/g) ?? []) if (!marks.has(m)) bad.push(`${m}: ${s.path}`);
    expect(bad).toEqual([]);
  });
  test('the status pill\'s timeline is the mission\'s, and the close is its last mark', () => {
    expect(t.briefing.timeline.map((x) => x.at)).toEqual(mission.timeline.map((x) => x.at).filter((a) => a !== '0:00'));
    expect(t.briefing.clock.close).toBe(mission.timeline.at(-1)!.at);
    expect(t.briefing.tracker).toContain(t.briefing.clock.close);
  });
  test('the purse is the engine\'s', () => {
    const site = json('sim/data/handover.site.json') as { purse: number };
    expect(t.briefing.purse.usd).toBe(site.purse);
    expect(t.briefing.purse.line).toContain('$' + site.purse.toLocaleString('en-US'));
  });
  test('the brief card is 3–5 sentences, opens on the order, and names the objective', () => {
    expect(t.briefing.card.length).toBeGreaterThanOrEqual(3);
    expect(t.briefing.card.length).toBeLessThanOrEqual(5);
    expect(t.briefing.card[0]).toMatch(/^Councillor Vale has a file on us/);
    expect(t.briefing.card.join(' ')).toMatch(/alive/);
  });
  test('the briefing page (brief 070): two sentences a way, the five clock stations, a line for each face, nothing about Day or Night', () => {
    const p = t.briefing.page; const sentences = (x: string) => x.split(/(?<=[.?])\s+/).filter(Boolean).length;
    const bj = json('contracts/missions/handover/brief.json') as { ways: { id: string }[] };
    expect(Object.keys(p.ways).sort()).toEqual(bj.ways.map((w) => w.id).sort());
    for (const [id, w] of Object.entries(p.ways)) {
      const hint = t.briefing.hints[id as 'front-door' | 'pickup'];
      expect(hint, id).toMatch(/^[A-Z][a-z]+( [a-z]+)?: /); expect(sentences(hint.split(': ', 2)[1]!) + sentences(w.line), id).toBeLessThanOrEqual(2);
    }
    expect(p.clock).toEqual(['2:35', '5:30', '5:40', '5:50', '6:10']); // Sky, brief 070: five stations
    const marks = t.briefing.timeline.map((x) => x.at); expect(p.clock.every((a) => marks.includes(a))).toBe(true);
    expect([...p.clock].sort((a, b) => marks.indexOf(a) - marks.indexOf(b))).toEqual(p.clock);
    expect(Object.keys(p.cell).sort()).toEqual([...AGENTS].sort());
    expect(JSON.stringify(t.briefing).replace(/"rule":"[^"]*"/g, '')).not.toMatch(/\b(day|night)\b/i);
  });
  test('the briefing page\'s heads (brief 070, Latch on 0.2.130): one for the mission\'s count of ways, from the data; Where, The clock, Heat; a few plain words, no sentence', () => {
    const h = t.briefing.page.heads; const bj = json('contracts/missions/handover/brief.json') as { ways: { id: string }[] };
    expect(h.ways[String(bj.ways.length)]).toBe('Two ways in');
    for (const [n, x] of Object.entries(h.ways)) expect(x, n).toMatch(/^[A-Z][a-z]+ ways? in$/);
    expect(h.ways['1']).toBe('One way in');
    expect([h.where, h.clock, h.heat]).toEqual(['Where', 'The clock', 'Heat']);
    for (const x of [...Object.values(h.ways), h.where, h.clock, h.heat]) { expect(x.split(/\s+/).length, x).toBeLessThanOrEqual(4); expect(x, x).not.toMatch(/[.!?:]/); }
  });
  test('the briefing page\'s words (Glyph N7, N11, N12 on 0.2.130): no stranded word, the pickup still says where, the 6:10 station is one fragment', () => {
    const p = t.briefing.page; const m = json('contracts/missions/handover/mission.json') as { points: { id: string }[]; destinations: Record<string, unknown> };
    expect(p.ways['front-door'].line).toBe('Turn her to call him off the door, and loop his camera.');
    expect(p.ways['front-door'].line).not.toMatch(/\s\S{1,3}\.$/); // never ends on a lone short word ("it.")
    expect(p.captions).toEqual({ 'service-bay': "Vale's car at the service bay" });
    for (const k of Object.keys(p.captions ?? {})) expect(m.points.some((x) => x.id === k) || k in m.destinations, k).toBe(true);
    expect(p.ways.pickup.line).toMatch(/service bay/); expect(p.captions!['service-bay']).toMatch(/service bay$/);
    const at = t.briefing.timeline.find((x) => x.at === '6:10')!.label;
    expect(at).toBe('Vale at his car; unless turned, the driver leaves'); expect(at).not.toMatch(/\.\s/);
  });
  test('the clock is true whether or not the driver is turned (brief 070): unturned he leaves at 6:10, turned he waits until 8:30', () => {
    const c = t.briefing.clock.line; expect(c).toMatch(/6:10/); expect(c).toMatch(/8:30/); expect(c).toMatch(/turn/);
    expect(t.briefing.card.at(-1)).toMatch(/6:10.*8:30.*turn/);
    const at = (m: string) => t.briefing.timeline.find((x) => x.at === m)!.label;
    expect(at('6:10')).toMatch(/[Uu]nless turned/); expect(at('8:30')).toMatch(/turned driver/);
    expect(JSON.stringify(t.briefing)).not.toMatch(/At 8:30 Vale's driver|the driver drives Vale away/);
  });
  test('the hints name the two known approaches', () => {
    expect(t.briefing.hints['front-door']).toMatch(/Inez/);
    expect(t.briefing.hints['front-door']).toMatch(/guard/); // she is close to the door guard (the Story spine)
    expect(t.briefing.hints.pickup).toMatch(/driver/);
  });
});

describe('verbs and refusals', () => {
  test('every panel verb has an action, a label, an agent and an engine order', () => {
    expect(Object.keys(t.actions).filter((k) => !k.startsWith('ring-')).sort()).toEqual([...VERBS, ...CONTROL_VERBS].sort());
    for (const [k, a] of Object.entries(t.actions)) if (k.startsWith('ring-')) expect(a.label.length, k).toBeLessThanOrEqual(8); // brief 030: one word on a wedge
    for (const a of Object.values(t.actions)) { expect(a.label.length).toBeGreaterThan(0); expect(a.by.length).toBeGreaterThan(0); }
  });
  test('every rulebook verb is an action: scout events, turn approaches, the cell\'s verbs', () => {
    const ids = new Set<string>();
    for (const e of rb.events) {
      if (e.id.startsWith('scout.')) ids.add(e.id.slice(6));
      if (e.id.startsWith('turn.')) ids.add(e.id.slice(5));
      if (e.id === 'contact.job') ids.add('call');
      if (e.id === 'cell.watch') ids.add('watch');
    }
    for (const k of Object.keys(rb.cell)) if (['loop', 'reroute', 'mislead', 'call', 'enter', 'carry', 'take'].includes(k)) ids.add(k);
    expect([...ids].filter((i) => !(i in t.actions))).toEqual([]);
    expect(ids.size).toBeGreaterThanOrEqual(13);
  });
  test('every order the engine takes is an action (GameOrders.Check)', () => {
    if (!haveSim) return;
    const src = simFile('GameOrders.cs');
    const check = src.slice(src.indexOf('string Check(Order o)'), src.indexOf('void Engage('));
    const dos = [...check.matchAll(/case "([a-z]+)":/g), ...check.matchAll(/o\.Do == "([a-z]+)"/g)].map((m) => m[1]!);
    dos.push('panic');
    const engineDos = new Set(Object.values(t.actions).map((a) => a.engine.do));
    expect(dos.filter((d) => !engineDos.has(d))).toEqual([]);
    expect(new Set(dos).size).toBeGreaterThanOrEqual(19);
    for (const d of ['withdraw', 'cancel', 'panic']) expect(dos, d).toContain(d);
    // the approaches
    const approaches = Object.entries(t.actions).filter(([k, a]) => !k.startsWith('ring-') && a.engine.do === 'turn').map(([, a]) => a).map((a) => a.engine.approach).sort();
    expect(approaches).toEqual(['ask', 'help', 'pay', 'pressure']);
  });
  test('brief 013: the takedown has a way for each `how` the engine takes (knockout, kill), and the body orders are for any agent but the takedown is the Enforcer\'s', () => {
    if (haveSim && existsSync(join(SIM, 'GameTakedown.cs'))) {
      const src = simFile('GameTakedown.cs');
      const hows = [...src.slice(src.indexOf('string Takedown(Op op, Order o)')).matchAll(/o\.How != "([a-z]+)"/g)].map((m) => m[1]!).sort();
      expect(hows).toEqual(['kill', 'knockout']);
      expect(Object.keys(t.actions.takedown!.ways ?? {}).sort()).toEqual(hows);
    }
    expect(t.actions.takedown!.by).toEqual(['enforcer']);
    for (const v of ['drop', 'stash', 'restrain', 'carry'] as const) expect(t.actions[v]!.by.slice().sort(), v).toEqual([...AGENTS].sort());
  });
  test('each action\'s target lines name targets the file describes, and its agents exist', () => {
    for (const [k, a] of Object.entries(t.actions)) {
      for (const r of Object.keys(a.onTarget ?? {})) expect(r in t.targets, `${k}.onTarget.${r}`).toBe(true);
      for (const ag of a.by) expect(AGENTS).toContain(ag);
    }
  });
  const engineCodes = (): string[] => {
    const m = simFile('Model.cs');
    const list = m.slice(m.indexOf('public static readonly string[] Codes'), m.indexOf('};', m.indexOf('public static readonly string[] Codes')));
    return [...list.matchAll(/"([a-z-]+)"/g)].map((x) => x[1]!);
  };
  test('text.json has a line for every refusal code the engine gives, and no other (Answer.Codes)', () => {
    if (!haveSim) return;
    const codes = engineCodes();
    expect(codes.length).toBeGreaterThanOrEqual(80);
    // brief 054 §7 (engine 0.28.0): nothing-to-see is never shown (the host draws no Watch row), so it has no line
    const NEVER_SHOWN = ['nothing-to-see'];
    expect(Object.keys(t.locks).sort()).toEqual([...codes].filter((c) => !NEVER_SHOWN.includes(c)).sort());
  });
  test('every code the engine can raise is in that list, and every lock names real verbs', () => {
    if (!haveSim) return;
    const codes = new Set(engineCodes());
    const used = new Set<string>();
    for (const src of simAll()) for (const m of src.matchAll(/\bNo\("([a-z-]*[a-z])"/g)) used.add(m[1]!); // `No("needs-" + kind)` builds its code; those are in the list
    expect([...used].filter((c) => !codes.has(c))).toEqual([]);
    expect(used.size).toBeGreaterThanOrEqual(60);
    for (const [c, l] of Object.entries(t.locks)) for (const v of l.verbs) expect(v === '*' || v in t.actions, `${c}: ${v}`).toBe(true);
  });
  test('a line carries exactly the numbers the engine sends with its code (m or cm → {m}, usd, tick)', () => {
    if (!haveSim) return;
    const sent = new Map<string, Set<string>>();
    for (const src of simAll()) for (const line of src.split('\n')) {
      for (const m of line.matchAll(/\bNo\("([a-z-]+)",[^;]*?(?:,\s*(m|cm|usd|tick): )/g)) {
        const f = new Set(sent.get(m[1]!) ?? []);
        for (const x of line.slice(m.index).matchAll(/[,(]\s*(m|cm|usd|tick): /g)) f.add(x[1] === 'cm' ? 'm' : x[1]!);
        sent.set(m[1]!, f);
      }
    }
    // `return No(code, why, tick: code == "summon-too-far" ? walk : -1)` sends the walk with that one code (a duration in ticks)
    expect(simAll().join('\n')).toContain('tick: code == "summon-too-far"');
    sent.set('summon-too-far', new Set(['tick']));
    expect(sent.size).toBeGreaterThanOrEqual(10);
    const bad: string[] = [];
    for (const [c, l] of Object.entries(t.locks)) {
      // a lock's variants (byTarget) are worded for the cases the engine sends the number with; the base line is the case it does not (car-held: tick only when car:rivals holds the car)
      const uses = new Set([l.line, ...Object.values(l.byTarget ?? {}).map((v) => v.line)].flatMap((x) => [...x.matchAll(/\{(m|usd|tick)\}/g)].map((y) => y[1]!)));
      const send = sent.get(c) ?? new Set<string>();
      for (const u of uses) if (!send.has(u)) bad.push(`${c}: line uses {${u}} but the engine does not send it`);
      for (const f of send) if (!uses.has(f)) bad.push(`${c}: the engine sends ${f} and the line ignores it`);
    }
    expect(bad).toEqual([]);
  });
  test('every summons refusal has Inez\'s answer and the guard\'s, in lines that carry no numbers', () => {
    if (!haveSim) return;
    const summon = engineCodes().filter((c) => c.startsWith('summon-'));
    expect(summon.length).toBeGreaterThanOrEqual(7);
    for (const c of summon) for (const sp of ['waitress', 'guard']) expect(lookup(t, { event: 'summon-refused', speaker: sp, outcome: c })?.outcome, `${sp} ${c}`).toBe(c);
    const stray = t.dialogue.filter((e) => e.event === 'summon-refused' && !summon.includes(e.outcome ?? '')).map(lineKey);
    expect(stray).toEqual([]);
    for (const c of summon) expect(c in t.locks, c).toBe(true);
  });
  test('fillLock fills the numbers and the names', () => {
    const clock = (tk: number) => `${Math.floor(tk / 600)}:${String(Math.floor(tk / 10) % 60).padStart(2, '0')}`;
    expect(fillLock(t, { code: 'too-far', m: 4 }, { who: 'Inez' })).toBe('Too far from Inez. Walk within 4 m first.');
    expect(fillLock(t, { code: 'stand-at-door', cm: 600 })).toContain('within 6 m');
    expect(fillLock(t, { code: 'no-cash', usd: 90000 })).toContain('$90,000');
    expect(fillLock(t, { code: 'agent-busy', tick: 1830 }, { op: 'Scout', clock })).toBe('Scout is busy until 3:03. Wait, or use another agent.');
    expect(fillLock(t, { code: 'summon-too-far', tick: 950 }, { clock })).toBe("Too far: the guard's walk to Inez would take 1:35. The guard won't come.");
    expect(fillLock(t, { code: 'nope' })).toBeUndefined();
  });
  const dotnet = Bun.which('dotnet');
  test.skipIf(!dotnet || !haveSim)('text.json matches `suborned-sim codes` (the built engine)', () => {
    const env = { ...process.env, DOTNET_CLI_TELEMETRY_OPTOUT: '1', DOTNET_NOLOGO: '1', ...(process.env.DOTNET_ROOT || !existsSync('/opt/homebrew/opt/dotnet/libexec') ? {} : { DOTNET_ROOT: '/opt/homebrew/opt/dotnet/libexec' }) };
    const r = Bun.spawnSync(['dotnet', 'run', '--project', 'sim/cli', '--', 'codes'], { cwd: root, env, stdout: 'pipe', stderr: 'pipe' });
    expect(r.exitCode, new TextDecoder().decode(r.stderr)).toBe(0);
    const codes = JSON.parse(new TextDecoder().decode(r.stdout)) as string[];
    // brief 054 §7 (engine 0.28.0): nothing-to-see is never shown (the host draws no Watch row), so it has no line
    const NEVER_SHOWN = ['nothing-to-see'];
    expect(Object.keys(t.locks).sort()).toEqual([...codes].filter((c) => !NEVER_SHOWN.includes(c)).sort());
  }, 120000);
  test('every fail code the engine can return has plain words and a spoken line', () => {
    if (!haveSim) return;
    const codes = new Set<string>(['engine-error']);
    for (const src of simAll()) for (const m of src.matchAll(/Fail\("([a-z-]+)"/g)) codes.add(m[1]!);
    // a retired fail stays in the engine only for rulebooks pinned before its change (engine 0.34.2: entry-seen-patrol); the live text words none
    for (const c of RETIRED_FAILS) { codes.delete(c); expect(c in t.briefing.fails, c).toBe(false); expect(lookup(t, { event: 'fail', speaker: 'fixer', outcome: c }), c).toBeFalsy(); }
    expect([...codes].sort()).toEqual([...FAILS].sort());
    for (const c of codes) expect(t.briefing.fails[c], c).toBeTruthy();
    expect(Object.keys(t.briefing.fails).sort()).toEqual([...FAILS, 'withdraw'].sort());
    for (const c of codes.values()) if (c !== 'engine-error') expect(lookup(t, { event: 'fail', speaker: 'fixer', outcome: c }), `fail ${c}`).toBeTruthy();
  });
  test('every engine log code is spoken by an event or silent on purpose', () => {
    if (!haveSim) return;
    const codes = new Set<string>();
    for (const src of simAll()) {
        for (const m of src.matchAll(/Note\("([a-z.\-]+)"/g)) codes.add(m[1]!);
        for (const m of src.matchAll(/Note\([^"()]*\?\s*"([a-z.\-]+)"\s*:\s*"([a-z.\-]+)"/g)) { codes.add(m[1]!); codes.add(m[2]!); } // Note(hit ? "shot.hit" : "shot.miss", …)
      }
    // Note("call." + p.Role, …) builds the lovers' call code: expand it to the roles whose call can succeed. Brief 059 (engine 0.32.0): the driver's Call is retired (CallRefusal: nothing-to-call), so only Inez's call logs
    expect(simAll().join('\n')).toContain('Note("call." + p.Role');
    if (codes.delete('call.')) for (const r of ['waitress']) codes.add('call.' + r);
    for (const c of ['scout.watch.watched', 'tech.watch.watched', 'fixer.watch.watched', 'enforcer.watch.watched', 'scout.listen.heard', 'scout.down', 'fixer.down', 'tech.down', 'enforcer.down']) codes.add(c);
    const spoken = new Set(Object.values(t.events).flatMap((e) => e.log ?? []));
    const covers = (code: string, set: Iterable<string>) => [...set].some((p) => (p.endsWith('.') ? code.startsWith(p) : code === p));
    const silent = Object.keys(t.silentLogs);
    const holes = [...codes].filter((c) => !covers(c, spoken) && !covers(c, silent));
    expect(holes).toEqual([]);
    // an event's log code must exist in the engine (no typos), apart from the dynamic ones named above
    const literal = new Set([...codes]);
    const missing = [...spoken].filter((c) => !literal.has(c) && !c.endsWith('.'));
    expect(missing).toEqual([]);
  });
});

describe('training hints, the run toast, the going-loud card', () => {
  const motive = join(root, 'briefs', '011', 'motive-fights.md');
  const rows = new Map<string, { trigger: string; rings: string; goneWhen: string }>();
  if (existsSync(motive)) for (const m of read('briefs/011/motive-fights.md').matchAll(/^\| (H\d+) \| (.*?) \| (.*?) \| (.*?) \| (.*?) \|$/gm)) rows.set(m[1]!, { trigger: m[2]!, rings: m[4]!, goneWhen: m[5]! });
  const items = Object.entries(t.hints.items);
  const motiveIds = (id: string) => Number(id.slice(1)) <= 11;
  test('H1–H22 are all here, H1–H12 in priority order, H13 (turn the view) ahead of them at 0, H17 (the fight hint) ahead of H13 and H14–H16 (the end steps) ahead of H17, H18 (Go, brief 018), H19 (noticed, brief 031) and H20 (Drive the car round, brief 059) ahead of all, and the control tips H21 and H22 (brief 067) ahead of every one of those; H1–H11 carry Motive\'s trigger, ring and gone-when word for word (H12 is brief 014\'s, H13 brief 016\'s, H14–H17 Aster\'s end steps and fight hint, not in Motive\'s table)', () => {
    expect(items.map(([id]) => id)).toEqual(Array.from({ length: 22 }, (_, i) => `H${i + 1}`));
    expect(items.slice(0, 12).map(([, h]) => h.priority)).toEqual(Array.from({ length: 12 }, (_, i) => i + 1));
    expect(t.hints.items.H13!.priority).toBe(0);
    const end = ['H14', 'H15', 'H16', 'H18', 'H19', 'H20'].map((id) => t.hints.items[id]!.priority);
    for (const p of end) expect(p).toBeLessThan(t.hints.items.H17!.priority); // they win over the fight hint and every flavour hint, the view turn included, when their trigger holds
    expect(t.hints.items.H17!.priority).toBeLessThan(t.hints.items.H13!.priority); // a shot to take beats a view to turn
    expect(new Set([...end, t.hints.items.H17!.priority, t.hints.items.H13!.priority]).size).toBe(8); // no tie: the host's sort is not stable
    if (!rows.size) return;
    expect([...rows.keys()]).toEqual(items.filter(([id]) => motiveIds(id)).map(([id]) => id));
    for (const [id, h] of items.filter(([id]) => motiveIds(id))) expect({ trigger: h.trigger, rings: h.rings, goneWhen: h.goneWhen }, id).toEqual(rows.get(id)!);
  });
  test('each hint fits 110 characters, names an agent, a verb and a target, and introduces only its own verb', () => {
    const word: Record<string, string> = { turn: 'turn', mislead: 'false tip', move: 'run', 'turn-view': 'turn', 'step-target': 'switch', 'drive-car': 'drive' };
    const labels = ['Watch', 'Listen', 'Loop', 'Call', 'Reroute', 'Fire', 'Carry', 'Enter', 'Take', 'Board', 'Go', 'Drive'];
    const bad: string[] = [];
    for (const [id, h] of items) {
      const l = h.line;
      if (l.length > HINT_MAX) bad.push(`${id}: ${l.length} characters`);
      const named = h.agent === 'any' ? /\bagent\b/i.test(l) : new RegExp(`\\b${h.agent}\\b`, 'i').test(l);
      if (!named) bad.push(`${id}: names no agent`);
      if (id !== 'H19' && !l.toLowerCase().includes(word[h.verb] ?? h.verb)) bad.push(`${id}: names no verb (${h.verb})`);
      const own = (word[h.verb] ?? h.verb).toLowerCase();
      for (const lab of labels) if (new RegExp(`\\b${lab}\\b`).test(l) && lab.toLowerCase() !== own) bad.push(`${id}: also introduces ${lab}`);
      if (!h.target.length) bad.push(`${id}: no target`);
    }
    expect(bad).toEqual([]);
  });
  test('no hint names an approach, a time or a sequence (it never solves the plan)', () => {
    const bad = items.filter(([, h]) => /\b(ask|pay|paying|help|helping|pressure|pressing|first|then|before|after|next|finally|until|once|\d)\b|\d/i.test(h.line)).map(([id, h]) => `${id}: ${h.line}`);
    expect(bad).toEqual([]);
  });
  test('the run toast and the loss line exist; the loss line says what Motive asked', () => {
    expect(t.toasts['run-seen']!.line).toMatch(/run/i);
    // engine 0.34.0 (brief 067, Motive M067-B2): a run is noticed, never an Alert on its own, so the toast says so and names no Alert
    expect(t.toasts['run-seen']!.line).toMatch(/noticed/);
    expect(t.toasts['run-seen']!.line).not.toMatch(/Alert/);
    expect(t.goingLoud.captured.toLowerCase()).toBe('anyone left behind in a fight is captured.');
  });
});

describe('keys named in the words are the game\'s (briefs/011/play/HOW-TO-PLAY.md)', () => {
  // A key a line names must be bound to what the line means. Anything not listed here must not appear.
  const MEANS: Record<string, { action: string; where: string[] }> = {
    N: { action: 'next event', where: ['hints.H7'] },
    Q: { action: 'turn the view left', where: ['hints.H13'] },
    E: { action: 'turn the view right', where: ['hints.H13'] },
    F: { action: 'draw or holster', where: ['locks.not-drawn'] },
    Ctrl: { action: 'force fire', where: ['locks.civilian-target'] },
    Return: { action: 'play again', where: ['locks.mission-over'] },
    X: { action: 'cease fire', where: ['hints.H21'] }, // brief 067: the one place the file teaches X (PlayControls binds PlayAction.Cease to Key.X)
  };
  const howTo = existsSync(join(root, 'briefs/011/play/HOW-TO-PLAY.md')) ? read('briefs/011/play/HOW-TO-PLAY.md') : '';
  const KEY = /\((?:[A-Z]|F\d+)\)|\bCtrl\b|\bShift-|\bAlt\b|\bEsc\b|\bTab\b|\bPress (?:Return|Space|Esc|Tab)\b|\bF\d{1,2}\b|\b[NCXVLBHFQWER]\b(?=[ ,.;:)]|$)/g;
  test('every key a line names is one the game binds to what the line means, and no line names C for carry', () => {
    const named: { key: string; path: string }[] = [];
    for (const s of strings) for (const m of s.text.matchAll(KEY)) named.push({ key: m[0].replace(/[()]/g, '').replace(/^Press /, ''), path: s.path });
    const bad = named.filter((n) => !MEANS[n.key] || !MEANS[n.key]!.where.some((w) => n.path.startsWith(w))).map((n) => `${n.key}: ${n.path}`);
    expect(bad).toEqual([]);
    expect(named.length).toBeGreaterThanOrEqual(4); // the scan finds the keys that are there
    if (howTo) for (const [k, v] of Object.entries(MEANS).filter(([k]) => k !== 'Q' && k !== 'E')) expect(howTo, `${k} (${v.action}) is bound in HOW-TO-PLAY.md`).toMatch(new RegExp(`\\*\\*[^*]*\\b${k}\\b[^*]*\\*\\*`));
  });
  test('H11 says how to carry: click the downed agent with another selected, then pick Carry (controls trial 2026-10-24: a left click opens the ring)', () => {
    const l = t.hints.items.H11!.line;
    expect(l).toMatch(/\bclick\b/i);
    expect(l).not.toMatch(/right/i);
    expect(l).toMatch(/Carry/);
    expect(l).toMatch(/another agent/i);
    expect(l).not.toMatch(/\(C\)/);
  });
});

describe('scenes', () => {
  const items = t.scenes.items;
  const roleKey: Record<string, string> = { waitress: 'diner', driver: 'official-driver', guard: 'guard', councillor: 'councillor' };
  const beatsOf = (sc: (typeof items)[number]) => sc.variants.flatMap((v) => v.beats);
  const textOf = (sc: (typeof items)[number], seed = 'a', n = 0) => playScene(t, sc, seed, n).beats.map((b) => b.text).join(' ');
  test('no two scenes share a key', () => {
    const seen = new Set<string>(), dup: string[] = [];
    for (const sc of items) { const k = sceneKey(sc); if (seen.has(k)) dup.push(k); seen.add(k); }
    expect(dup).toEqual([]);
  });
  test('Watch has a scene for each role; Listen for each role and each temperament it can have', () => {
    const bad: string[] = [];
    for (const role of PEOPLE) {
      const w = lookupScene(t, { kind: 'overheard', verb: 'watch', about: role });
      if (!w || w.about !== role) bad.push(`watch ${role}`); else expect(w.learns).toEqual(['routine']);
      for (const temp of allowedTemperaments(people, roleKey[role]!)!) {
        const l = lookupScene(t, { kind: 'overheard', verb: 'listen', about: role, temperament: temp });
        if (!l || l.temperament !== temp || l.about !== role) bad.push(`listen ${role} ${temp}`); else { expect(l.learns).toContain('need'); expect(l.learns).toContain('temperament'); }
      }
    }
    expect(bad).toEqual([]);
  });
  test('every approach the rulebook can produce has a scene for each role the temperament allows, ending on the contact\'s reply and the Fixer\'s reaction', () => {
    const bad: string[] = [];
    for (const cell of replyCells()) {
      const [verb, temp, outcome] = cell.split('/') as [string, (typeof TEMPERAMENTS)[number], string];
      for (const role of ['waitress', 'driver', 'guard']) {
        if (!allowedTemperaments(people, roleKey[role]!)!.includes(temp)) continue;
        const sc = items.find((x) => x.kind === 'approach' && x.verb === verb && x.about === role && x.outcome === outcome && x.temperament === temp);
        if (!sc) { bad.push(`${verb} ${role} ${temp} ${outcome}`); continue; }
        for (const v of sc.variants) {
          const last = v.beats.slice(-2);
          if (!('ref' in last[0]!) || last[0]!.ref !== 'reply' || last[0]!.speaker !== role || !('ref' in last[1]!) || last[1]!.ref !== 'done' || last[1]!.speaker !== 'fixer') bad.push(`${verb} ${role} ${temp} ${outcome}: wrong ending`);
          if (v.beats[0]!.speaker !== 'fixer') bad.push(`${verb} ${role} ${temp} ${outcome}: the Fixer opens`);
        }
      }
    }
    expect(bad).toEqual([]);
    const dead = items.filter((x) => x.kind === 'approach').filter((x) => !replyCells().has(`${x.verb}/${x.temperament}/${x.outcome}`)).map(sceneKey);
    expect(dead).toEqual([]);
  });
  test('the call: Inez calls the guard over and he leaves his post, in all four voices; the driver has no call (brief 059, engine 0.32.0: it blinded the pickup camera, now retired)', () => {
    for (const role of ['waitress']) for (const temp of TEMPERAMENTS) {
      const sc = lookupScene(t, { kind: 'call', verb: 'call', about: role, temperament: temp });
      expect(sc?.temperament, `${role} ${temp}`).toBe(temp);
    }
    for (const sc of items.filter((x) => x.kind === 'call' && x.about === 'waitress')) {
      expect(sc.result).toMatch(/guard/);
      for (const v of sc.variants) { const who = new Set(v.beats.map((b) => b.speaker)); expect(who.has('waitress') && who.has('guard')).toBe(true); }
    }
    expect(items.filter((x) => x.kind === 'call' && x.about !== 'waitress').map(sceneKey)).toEqual([]);
    expect(lookupScene(t, { kind: 'call', verb: 'call', about: 'driver', temperament: 'loyal' })?.about).not.toBe('driver');
    expect(JSON.stringify(t.actions.call)).not.toMatch(/driver|camera/);
    // a turned driver is told what he does now, not that the Fixer can call him to blind a camera
    for (const sc of items.filter((x) => x.kind === 'approach' && x.about === 'driver' && x.outcome === 'turned')) { expect(sc.result, sceneKey(sc)).not.toMatch(/\bCall\b|camera|blind/); expect(sc.result, sceneKey(sc)).toMatch(/round the back/); }
    // the Fixer is refused a Call on him in plain words (rulebook: nothing-to-call)
    expect(t.locks['nothing-to-call']!.verbs).toContain('call');
  });
  test('Inez and the door guard (the Story spine): Listen on Inez overhears them, in every temperament and variant, and the Learned line says what was learned', () => {
    for (const temp of TEMPERAMENTS) {
      const sc = lookupScene(t, { kind: 'overheard', verb: 'listen', about: 'waitress', temperament: temp })!;
      expect(sc.learns).toContain('secret');
      expect(sc.result).toBe('Learned: Inez is in debt · {temperament} · Secret: she\'s seeing the door guard.');
      for (const v of sc.variants) { const who = new Set(v.beats.map((b) => b.speaker)); expect(who.has('waitress') && who.has('guard'), temp).toBe(true); }
    }
    const text = (verb: string, outcome: string) => items.filter((x) => x.kind === 'approach' && x.verb === verb && x.about === 'waitress' && x.outcome === outcome).map((x) => beatsOf(x).flatMap((b) => ('line' in b ? [b.line] : [])).join(' ')).join(' ');
    expect(text('pressure', 'turned')).toMatch(/guard/); // Pressure threatens to expose the secret
    expect(text('help', 'turned')).toMatch(/debt|owe|lender/); // Help clears the debt
    expect(text('pay', 'turned')).not.toMatch(/guard/);
  });
  test('Watch and Listen scenes have the target in them; every scene has 2–4 variants with no repeated line, and its variants differ', () => {
    for (const sc of items) {
      if (sc.kind === 'overheard') for (const v of sc.variants) expect(v.beats.some((b) => b.speaker === sc.about), sceneKey(sc)).toBe(true);
      expect(new Set(sc.variants.map((v) => JSON.stringify(v))).size, sceneKey(sc)).toBe(sc.variants.length);
      for (const v of sc.variants) { const lines = v.beats.map((b) => ('line' in b ? b.line : b.ref + b.speaker)); expect(new Set(lines).size, sceneKey(sc)).toBe(lines.length); }
    }
  });
  test('city scenes play with a world event, say whether they are seen anywhere or only within sight, and the chime has both', () => {
    for (const sc of items.filter((x) => x.kind === 'city')) { expect(sc.event! in t.events, sc.event).toBe(true); expect(sc.sight).toBeTruthy(); }
    for (const e of ['rivals-arrive', 'chime', 'guard-at-cafe']) expect(scenesFor(t, 'city', e).length, e).toBeGreaterThanOrEqual(1);
    expect(scenesFor(t, 'city', 'chime').map((x) => x.sight).sort()).toEqual(['always', 'in-sight']);
    expect(scenesFor(t, 'city', 'rivals-arrive')[0]!.sight).toBe('in-sight');
    expect(items.filter((x) => x.kind !== 'city').every((x) => x.sight === undefined)).toBe(true);
  });
  test('every scene plays: each ref finds a dialogue line, nothing is empty, and it is deterministic and seed-dependent', () => {
    for (const sc of items) {
      for (const n of [0, 1, 2]) for (const p of [playScene(t, sc, 'seed-1', n)]) {
        expect(p.beats.length).toBeGreaterThanOrEqual(3);
        for (const b of p.beats) expect(b.text.length, sceneKey(sc)).toBeGreaterThan(0);
        expect(p.result.length).toBeGreaterThan(0);
      }
      expect(playScene(t, sc, 's', 1)).toEqual(playScene(t, sc, 's', 1));
    }
    const sc = lookupScene(t, { kind: 'overheard', verb: 'listen', about: 'waitress', temperament: 'fearful' })!;
    const seen = new Set(Array.from({ length: 12 }, (_, n) => textOf(sc, 'seed-9', n)));
    expect(seen.size).toBe(sc.variants.length); // every variant is reached; none repeats back to back is the pick's rule
  });
  test('the scene results state what the player knows or has, in plain words', () => {
    for (const sc of items) expect(sc.result, sceneKey(sc)).toMatch(/^[A-Z{]/);
    expect(items.find((x) => x.kind === 'overheard' && x.verb === 'watch')!.result).toMatch(/^Learned:/);
    expect(items.filter((x) => x.kind === 'approach' && x.outcome === 'turned').every((x) => /turned/.test(x.result))).toBe(true);
  });
});

describe('labels with a target', () => {
  test('every verb that takes a target has a with-target label and a queued clause that carry {target}', () => {
    const bad: string[] = [];
    for (const [k, a] of Object.entries(t.actions)) {
      const takes = Object.keys(a.onTarget ?? {}).length > 0 || ['fire', 'carry', 'move', 'run'].includes(k);
      if (takes && (!a.withTarget || !a.then)) bad.push(`${k}: missing`);
      for (const f of [a.withTarget, a.then]) if (f !== undefined && !f.includes('{target}')) bad.push(`${k}: ${f}`);
      if (!takes && (a.withTarget || a.then)) bad.push(`${k}: takes no target`);
    }
    expect(bad).toEqual([]);
  });
  test('every target key an action names is one the file can say in a sentence', () => {
    for (const a of Object.values(t.actions)) for (const r of Object.keys(a.onTarget ?? {})) expect(r in t.names.targets, r).toBe(true);
    for (const a of Object.values(t.actions)) for (const [g, m] of Object.entries(a.onTargetBy ?? {})) for (const r of Object.keys(m)) { expect(r in t.names.targets, r).toBe(true); expect(a.by as string[], g).toContain(g); }
    for (const k of Object.keys(t.targets)) expect(k in t.names.targets, k).toBe(true);
  });
});

describe('the hostile AI', () => {
  test('every AI state the engine has is spoken by ai-state or silent on purpose', () => {
    if (!haveSim) return;
    const model = simFile('Model.cs');
    const states = model.match(/public string St = "";\s*\/\/ ([a-z, ]+)/)?.[1]?.split(',').map((x) => x.trim()).filter(Boolean) ?? [];
    expect(states.length).toBeGreaterThanOrEqual(7);
    const ai = simFile('GameAi.cs');
    for (const m of ai.matchAll(/\.St = "([a-z]+)"/g)) expect(states, `St = ${m[1]}`).toContain(m[1]!);
    const spoken = new Set(t.dialogue.filter((e) => e.event === 'ai-state').map((e) => e.outcome));
    expect(states.filter((x) => !spoken.has(x) && !(x in t.silentStates))).toEqual([]);
    expect([...spoken].filter((x) => !states.includes(x!))).toEqual([]);
    expect(Object.keys(t.silentStates).filter((x) => spoken.has(x))).toEqual([]);
  });
  test('rivals, police, the guard ally and the patrol each speak every state that is spoken', () => {
    const spokenStates = ['alerted', 'advance', 'engage', 'hold'];
    for (const sp of t.events['ai-state']!.speakers) for (const st of spokenStates) expect(lookup(t, { event: 'ai-state', speaker: sp, outcome: st }), `${sp} ${st}`).toBeTruthy();
  });
});

describe('cancel and activity', () => {
  const cancel = json('contracts/rules/rulebook.json').cancel as Record<string, string | boolean>;
  test('every cancel line the rulebook and the engine can name has words', () => {
    const ids = new Set<string>();
    for (const [k, v] of Object.entries(cancel)) if (typeof v === 'string' && !['rule', 'tag', 'src', 'reason'].includes(k)) ids.add(v);
    if (haveSim && existsSync(join(SIM, 'GameCancel.cs'))) {
      const src = simFile('GameCancel.cs');
      for (const m of src.matchAll(/CancelCost = "([a-z-]+)"/g)) ids.add(m[1]!);
      const line = src.slice(src.indexOf('CancelLine(Op op, string verb)'), src.indexOf('string Cancel(Op op)'));
      for (const m of line.matchAll(/\("([a-z-]+)", \$?"/g)) ids.add(m[1]!);
    }
    expect(ids.size).toBeGreaterThanOrEqual(10);
    expect([...ids].filter((i) => !(i in t.cancelCosts))).toEqual([]);
    expect(Object.keys(t.cancelCosts).filter((k) => !ids.has(k))).toEqual([]); // no words for a cost nothing returns
  });
  test('the HUD has words for every verb an agent can be busy with (rulebook cancel verbs)', () => {
    const verbs = Object.keys(cancel).filter((k) => !['rule', 'tag', 'src', 'reason', 'proposed', 'loopHeld', 'carrying'].includes(k));
    const have = new Set<string>(['move', 'fire', 'draw', 'holster', ...Object.keys(t.activity.acting)]);
    const kebab = (v: string) => v.replace(/[A-Z]/g, (c) => '-' + c.toLowerCase()); // the rulebook's `driveCar` is the order `drive-car` (engine 0.33.1)
    expect(verbs.filter((v) => v !== 'turn' && !have.has(kebab(v)))).toEqual([]);
    for (const a of ['ask', 'pay', 'help', 'pressure']) expect(a in t.activity.acting, a).toBe(true);
  });
});

// ---- the replies: derived from the rulebook, not typed here
/** Every (approach, temperament, outcome) the rulebook can produce in any band; `suspicious` when the approach is reported. */
function replyCells(): Set<string> {
  const cells = new Set<string>();
  const ALL = new Set<R.Know>(['routine', 'need', 'temperament']);
  for (const temp of TEMPERAMENTS) for (const band of ['hero', 'neutral', 'menace'] as const) for (const a of ['ask', 'pay', 'help', 'pressure']) {
    let s: R.Subject = { id: 'x', tier: 'simulated', temperament: temp, need: 'debt', scales: { ...people.temperaments[temp].start }, memory: [], turned: false };
    const evt = R.event(rb, `turn.${a}`);
    for (let i = 0; i < 4; i++) {
      const r = R.resolve(rb, people, evt, { subject: s, band, knows: ALL, given: { reach: true } as never });
      const reported = r.changes.some((c) => c.do === 'report');
      cells.add(`${a}/${temp}/${reported ? 'suspicious' : r.outcome}`);
      s = R.apply(rb, people, s, r.changes, { by: 'fixer', place: 'p', time: 10 + i, witnessed: false });
      if (a !== 'help' && i >= 1) break;
    }
  }
  return cells;
}

describe('the dialogue', () => {
  const ev = t.events;
  test('every entry is a known event, spoken by someone allowed to, about a verb the event allows, with 2–4 variants', () => {
    const bad: string[] = [];
    for (const e of t.dialogue) {
      const d = ev[e.event];
      if (!d) { bad.push(`${lineKey(e)}: unknown event`); continue; }
      if (!d.speakers.includes(e.speaker)) bad.push(`${lineKey(e)}: ${e.speaker} may not speak ${e.event}`);
      if (e.verb && d.verbs && !d.verbs.includes(e.verb)) bad.push(`${lineKey(e)}: ${e.verb} is not a verb of ${e.event}`);
      if (e.lines.length < 2 || e.lines.length > 4) bad.push(`${lineKey(e)}: ${e.lines.length} variants`);
    }
    expect(bad).toEqual([]);
  });
  test('every event has a line for every speaker it lists', () => {
    const bad: string[] = [];
    for (const [id, d] of Object.entries(ev)) for (const sp of d.speakers) if (!t.dialogue.some((e) => e.event === id && e.speaker === sp)) bad.push(`${id}: no ${sp}`);
    expect(bad).toEqual([]);
  });
  test('every order is acknowledged by each agent who can give it; arrival and landing where the order has them', () => {
    const bad: string[] = [];
    for (const v of ev.ack!.verbs!) for (const ag of t.actions[v as (typeof VERBS)[number]]!.by) if (!lookup(t, { event: 'ack', verb: v, speaker: ag })) bad.push(`ack ${v}/${ag}`);
    for (const v of ev.arrive!.verbs!) for (const ag of t.actions[v as (typeof VERBS)[number]]!.by) if (!lookup(t, { event: 'arrive', verb: v, speaker: ag })) bad.push(`arrive ${v}/${ag}`);
    for (const v of ev.done!.verbs!) {
      if (v === 'turn') { for (const o of OUTCOMES) if (!lookup(t, { event: 'done', verb: 'turn', speaker: 'fixer', outcome: o })) bad.push(`done turn/${o}`); continue; }
      for (const ag of t.actions[v as (typeof VERBS)[number]]!.by) if (!lookup(t, { event: 'done', verb: v, speaker: ag })) bad.push(`done ${v}/${ag}`);
    }
    expect(bad).toEqual([]);
    expect(ev.ack!.verbs!.length).toBe(VERBS.length);
  });
  test('the exchange of every verb plays events that exist, starting with the order\'s acknowledgement', () => {
    for (const v of VERBS) {
      const x = t.exchange[v];
      expect(x, v).toBeTruthy();
      expect(x![0]).toBe('ack');
      for (const id of x!) expect(id in ev, `${v}: ${id}`).toBe(true);
    }
  });
  test('people speak only in temperaments their role allows (people.rules temperamentsBy)', () => {
    const roleKey: Record<string, string> = { waitress: 'diner', driver: 'official-driver', guard: 'guard', councillor: 'councillor' };
    const bad: string[] = [];
    for (const e of t.dialogue) {
      if (!e.temperament) continue;
      const role = e.speaker === 'councillor' || e.speaker in roleKey ? e.speaker : e.about;
      if (!role) { bad.push(`${lineKey(e)}: a temperament with no person`); continue; }
      const allow = allowedTemperaments(people, roleKey[role]!);
      if (!allow?.includes(e.temperament)) bad.push(`${lineKey(e)}: ${role} may not be ${e.temperament}`);
    }
    expect(bad).toEqual([]);
  });

  test('every reply the rulebook can produce has a line from each Handover role the temperament allows', () => {
    const cells = replyCells();
    expect(cells.size).toBeGreaterThanOrEqual(30);
    const roleKey: Record<string, string> = { waitress: 'diner', driver: 'official-driver', guard: 'guard' };
    const bad: string[] = [];
    for (const cell of cells) {
      const [verb, temp, outcome] = cell.split('/') as [string, (typeof TEMPERAMENTS)[number], string];
      for (const role of ['waitress', 'driver', 'guard']) {
        if (!allowedTemperaments(people, roleKey[role]!)!.includes(temp)) continue;
        const e = lookup(t, { event: 'reply', verb, speaker: role, outcome, temperament: temp });
        if (!e || e.verb !== verb || e.outcome !== outcome || e.temperament !== temp) bad.push(`${verb} ${role} ${temp} ${outcome}`);
      }
    }
    expect(bad).toEqual([]);
  });
  test('every reply entry is a cell the rulebook can produce (no dead lines)', () => {
    const cells = replyCells();
    const dead = t.dialogue.filter((e) => e.event === 'reply').filter((e) => !cells.has(`${e.verb}/${e.temperament}/${e.outcome}`)).map(lineKey);
    expect(dead).toEqual([]);
  });
  test('the Scout reads each role (Watch) and each role and temperament it can have (Listen); contacts confirm calls; Vale answers a take', () => {
    const roleKey: Record<string, string> = { waitress: 'diner', driver: 'official-driver', guard: 'guard', councillor: 'councillor' };
    const bad: string[] = [];
    for (const role of PEOPLE) {
      if (!lookup(t, { event: 'read', verb: 'watch', speaker: 'scout', about: role })) bad.push(`watch ${role}`);
      for (const temp of allowedTemperaments(people, roleKey[role]!)!) {
        const e = lookup(t, { event: 'read', verb: 'listen', speaker: 'scout', about: role, temperament: temp });
        if (!e || e.temperament !== temp || e.about !== role) bad.push(`listen ${role} ${temp}`);
      }
    }
    for (const role of ['waitress']) for (const temp of TEMPERAMENTS) if (lookup(t, { event: 'confirm', verb: 'call', speaker: role, temperament: temp })?.temperament !== temp) bad.push(`confirm ${role} ${temp}`);
    // brief 059: the driver confirms no call (he has none): no line, and the event does not list him
    expect(t.dialogue.filter((e) => e.event === 'confirm' && e.speaker === 'driver')).toEqual([]);
    expect(t.events.confirm!.speakers).toEqual(['waitress']); expect(t.events.confirm!.log).toEqual(['call.waitress']);
    for (const verb of ['take', 'enter']) for (const temp of allowedTemperaments(people, 'councillor')!) if (lookup(t, { event: 'taken', verb, speaker: 'councillor', temperament: temp })?.temperament !== temp) bad.push(`taken ${verb} ${temp}`);
    expect(bad).toEqual([]);
  });
  test('the brief\'s named moments exist: rivals\' arrival, guard noticing, alarm, running noticed, going loud, fight start, win, fails, cliffhanger', () => {
    const need: [string, string][] = [['rivals-arrive', 'rivals'], ['drawn-seen', 'guard'], ['run-seen', 'guard'], ['run-seen', 'patrol'], ['alarm', 'security'], ['going-loud', 'enforcer'],
      ['fight-loud', 'security'], ['fight-safe', 'rivals'], ['fight-warned', 'waitress'], ['win', 'fixer'], ['win', 'councillor'], ['cliffhanger', 'councillor'], ['guard-at-cafe', 'waitress']];
    for (const [event, speaker] of need) expect(lookup(t, { event, speaker, outcome: event === 'cliffhanger' ? 'unnamed' : undefined, temperament: event === 'cliffhanger' ? 'loyal' : undefined }), `${event}/${speaker}`).toBeTruthy();
  });
});

describe('what the cell was told, what it watched, what it heard (targets)', () => {
  const persons = ['waitress', 'driver', 'guard', 'councillor'] as const;
  test('every person has a told line, what a Watch teaches and what a Listen teaches; the Listen line carries the need and temperament unless it is Inez\'s', () => {
    for (const k of persons) {
      const x = t.targets[k]!;
      expect(x.about.length, k).toBeGreaterThan(0); expect(x.watched, k).toBeTruthy(); expect(x.heard, k).toBeTruthy();
      if (k !== 'waitress') expect(x.heard!).toMatch(/\{need\}.*\{temperament\}/);
      else expect(x.heard!).toContain('{temperament}');
    }
  });
  test('the told lines hold nothing only a Watch or Listen teaches (GDD §04: knowledge is witnessed or told)', () => {
    expect(t.targets.waitress!.about).not.toMatch(/debt|secret|lover|seeing|affair/i);
    for (const k of ['camera-entrance', 'camera-pickup']) expect(t.targets[k]!.about, k).not.toMatch(/\bsees?\b|cone|cover|point|face/i);
    expect(t.targets.patrol!.about).not.toMatch(/cone|door|reach|round/i);
    for (const k of persons) expect(t.targets[k]!.about, k).not.toMatch(/\{/);
  });
  test('each thing a Scout can Watch besides a person has a watched line (≤ 70, it follows "Learned: "), the patrol also its rounds', () => {
    for (const k of WATCHED) { expect(t.targets[k]!.watched, k).toBeTruthy(); expect(t.targets[k]!.watched!.length).toBeLessThanOrEqual(70); }
    expect(t.targets.patrol!.routine).toBeTruthy();
    for (const k of Object.keys(t.targets)) if (!(WATCHED as readonly string[]).includes(k) && !persons.includes(k as (typeof persons)[number])) { expect(t.targets[k]!.watched, k).toBeUndefined(); expect(t.targets[k]!.heard, k).toBeUndefined(); }
  });
  test('the Scout reads a camera and the patrol when it watches them (the patrol twice: its sight, then its rounds), and Watch has a card line for each', () => {
    for (const k of ['camera-entrance', 'camera-pickup']) expect(lookup(t, { event: 'read', verb: 'watch', speaker: 'scout', about: k })?.about as string, k).toBe(k);
    for (const o of ['sight', 'routine']) { const e = lookup(t, { event: 'read', verb: 'watch', speaker: 'scout', about: 'patrol', outcome: o }); expect(e?.outcome, o).toBe(o); expect(e?.about as string).toBe('patrol'); }
    for (const k of WATCHED) expect(t.actions.watch!.onTarget![k], k).toBeTruthy();
  });
  test('the refusals a Watch of a camera or the patrol can get are Watch refusals (cones are the Scout\'s skill)', () => {
    for (const c of ['no-clear-line', 'patrol-down', 'target-unknown', 'too-far']) expect(t.locks[c]!.verbs, c).toContain('watch');
    expect(t.locks['target-unknown']!.verbs).toContain('listen');
    expect(t.locks['patrol-down']!.line).toMatch(/watch/i);
    expect(t.locks['no-clear-line']!.line).toContain('{who}');
  });
});

describe('feed cards for engine events no scene covers', () => {
  const items = t.cards.items;
  const logCodes = (): Set<string> => {
    const codes = new Set<string>();
    for (const src of simAll()) for (const m of src.matchAll(/Note\("([a-z.\-]+)"/g)) codes.add(m[1]!);
    for (const c of ['scout.down', 'fixer.down', 'tech.down']) codes.add(c);
    return codes;
  };
  test('every card is for a code the engine logs; a record result names how the host picks its key', () => {
    if (!haveSim) return;
    const codes = logCodes();
    expect(Object.keys(items).filter((c) => !codes.has(c))).toEqual([]);
    for (const [c, x] of Object.entries(items)) if (x.result && typeof x.result !== 'string') { expect(x.pick, c).toBeTruthy(); expect(Object.keys(x.result).length, c).toBeGreaterThanOrEqual(2); }
    for (const [c, x] of Object.entries(items)) if (typeof x.result === 'string' || x.result === undefined) expect(x.pick, c).toBeUndefined();
  });
  test('every code the host makes a fallback card of has its words here (Unity HostFeed.FeedEvents)', () => {
    const f = 'unity/Assets/Suborned/Runtime/Play/HostFeed.cs';
    if (!existsSync(join(root, f))) return;
    const src = read(f); const a = src.indexOf('FeedEvents = new()'); if (a < 0) return;
    const body = src.slice(a, src.indexOf('};', a));
    const codes = [...body.matchAll(/\["([a-z.\-]+)"\]\s*=\s*\("/g)].map((m) => m[1]!);
    expect(codes.length).toBeGreaterThan(15);
    // brief 059 (engine 0.32.0): car.blocks is retired with the driver's Call (Motive M059-6.1). The host keeps its fallback until Latch's port;
    // it may list only codes that have words here or are retired in the engine too, so a retired code that comes back fails here, with no words for it.
    const RETIRED = ['car.blocks'];
    expect(codes.filter((c) => !(c in items) && !RETIRED.includes(c))).toEqual([]);
    for (const c of RETIRED) { expect(c in items, `${c} has no card`).toBe(false); if (haveSim) expect(logCodes().has(c), `${c} is still logged by the engine`).toBe(false); expect(c in t.silentLogs, `${c} is not a silent log`).toBe(false); }
  });
  test('what replaced car.blocks: the driver has no Call, and the car stops are worded (brief 059)', () => {
    // the camera is no longer blinded by a call: the lock says so, and the two stops of Vale's car (service bay, mouth of the drive) have cards
    expect(t.locks['nothing-to-call']!.line).toMatch(/nothing to do/); expect(t.locks['nothing-to-call']!.short).toBe('Nothing to call');
    for (const c of ['car.parks', 'car.blocked']) expect(items[c], c).toBeTruthy();
    expect(items['car.parks']!.result).toMatch(/back seat/); expect(items['car.blocked']!.result).toMatch(/service drive/);
    expect(JSON.stringify(t.cards)).not.toMatch(/blind/i);
  });
  test('titles name the event as the player would and are not ids; results carry the engine\'s numbers, never its sentence', () => {
    for (const [c, x] of Object.entries(items)) {
      expect(x.title, c).not.toMatch(/[a-z]+-[a-z]+|\./);
      for (const r of typeof x.result === 'string' ? [x.result] : Object.values(x.result ?? {})) { expect(r, c).not.toMatch(/found fault|\btick\b|operative|engine/i); expect(r).toMatch(/^[A-Z{]/); }
    }
    expect(items.report!.result as string).toContain('{who}'); expect(items.report!.result as string).toContain('{alert}');
    // engine 0.34.0 (brief 067, Motive M067-B2): a run is noticed and noted, never an Alert on its own, so its card names who saw whom and no Alert
    expect(items['run.seen']!.result as string).toContain('{who}'); expect(items['run.seen']!.result as string).toContain('{agent}'); expect(items['run.seen']!.result as string).not.toMatch(/\{alert\}|Alert/);
    expect(items['fight.loud']!.result as string).toMatch(/\{n\}.*\{clock\}/);
    expect(items['fight.safe']!.result as string).toContain('{n}');
    expect(items.captured!.result as string).toContain('{agent}');
  });
  test('cards a person would only know in sight say so, as their scenes do; the one the cell hears anywhere says so too', () => {
    expect(items['rivals.arrive']!.sight).toBe('in-sight'); expect(items['guard.at-cafe']!.sight).toBe('in-sight');
    // the two cards of the city scenes (their place is the scene's); and brief 013's cards, which name where the check looks (`at`). Nothing else is in sight only.
    const SCENE_CARDS = ['rivals.arrive', 'guard.at-cafe'];
    // brief 067 (Aster, from Sky on 0.2.122): the rival car turning in at 2:35 is heard wherever the cell stands, so its card is `always`; its `at` only says what the card points at
    const HEARD_AT: Record<string, string> = { 'rivals.coming': 'rival-car' };
    const SEEN_AT: Record<string, string> = { 'rivals.drop': 'rival-car', 'rivals.follows': 'rival-car', 'rivals.back': 'rival-car', 'rivals.warned': 'rival-lookout', 'rivals.no-warning': 'rival-lookout', 'rivals.wakes': 'victim', 'rivals.frees': 'victim',
      'rivals.at-vale': 'vale-car', 'car.blocked': 'vale-car', 'restrain.freed': 'victim', 'victim.wakes': 'victim', 'victim.statement': 'victim', 'victim.calls-out': 'stash', 'report.started': 'reporter', 'report.stopped': 'reporter',
      'report.unfounded': 'scene', 'report.confirmed': 'scene', 'person.missed': 'person', 'guard.back-to-post': 'guard', 'alarm.reassigns': 'unit', 'doors.lock': 'door', 'patrol.alarm': 'patrol', 'entry.seen': 'patrol', 'entry.stopped': 'patrol',
      'patrol.stands-down': 'patrol', 'stash.serviced': 'stash', 'stash.opened': 'stash' };
    for (const [c, x] of Object.entries(items)) {
      if (x.sight) expect([...SCENE_CARDS, ...Object.keys(SEEN_AT), ...Object.keys(HEARD_AT)], c).toContain(c);
      expect(x.at !== undefined, `${c}: \`at\` goes with \`sight\``).toBe(x.sight !== undefined && !SCENE_CARDS.includes(c));
    }
    for (const [c, at] of Object.entries(SEEN_AT)) { expect(items[c]?.sight, c).toBe('in-sight'); expect(items[c]?.at as string | undefined, c).toBe(at); }
    for (const [c, at] of Object.entries(HEARD_AT)) { expect(items[c]?.sight, c).toBe('always'); expect(items[c]?.at as string | undefined, c).toBe(at); }
  });
  test('brief 013: the cell\'s own acts and the city\'s answers to an Alert have no sight rule; what happens at a place does (the told/learned rule)', () => {
    // the cell\'s own acts: the agent is there. The city\'s answers: the Alert pill and the street show them, so a card says that it came, never who made the report or why.
    for (const c of ['takedown.starts', 'takedown.fails', 'takedown.dead', 'takedown.out', 'takedown.seen', 'carry.body', 'carry.down', 'stash.', 'restrain.done', 'report.landed', 'seal.starts', 'alarm.dispatches', 'response.reinforces', 'rivals.recalled', 'rivals.wiped', 'rivals.see-takedown']) expect(items[c]?.sight, c).toBeUndefined();
    // a cause only a Watch can teach: the card waits for the routine
    expect(items['post.missed']!.needs).toBe('routine');
    expect(Object.entries(items).filter(([, x]) => x.needs).map(([c]) => c)).toEqual(['post.missed']);
    // the city\'s answers never name the reporter or the cause
    for (const c of ['report.landed', 'alarm.dispatches', 'response.reinforces', 'seal.starts']) for (const r of typeof items[c]!.result === 'string' ? [items[c]!.result as string] : Object.values(items[c]!.result as Record<string, string>)) expect(r, c).not.toMatch(/\{who\}|reported|saw|because/i);
    // every code of a stash is one card keyed by the prefix the engine builds
    expect(Object.keys(items['stash.']!.result as object).sort()).toEqual(['container', 'door']);
  });
  test('cardResult and fillCard fill the placeholders and capitalise the start', () => {
    expect(fillCard(cardResult(t, 'run.seen')!, { who: 'the entrance camera', agent: 'the Scout', alert: 1 })).toBe('The entrance camera noticed the Scout running.');
    expect(fillCard(cardResult(t, 'fight.loud')!, { n: 5, clock: '7:10' })).toBe('5 officers, from the north street. Police seal the district at 7:10.');
    expect(cardResult(t, 'vale.exits')).toBe('Walking to his car.'); expect(cardResult(t, 'vale.exits', 'service')).toBe('Walking to his car.'); // brief 059: one row, no service branch
  });
  test('every city scene has a title the player can read', () => {
    for (const sc of t.scenes.items.filter((x) => x.kind === 'city')) { expect(sc.title, sceneKey(sc)).toBeTruthy(); expect(sc.title).not.toMatch(/-/); }
  });
});

describe('the end card\'s words (debrief ids)', () => {
  const d = t.debrief;
  test('every id sim Debrief.cs can give has a sentence: route, assets, investigation causes', () => {
    if (!haveSim) return;
    const src = simFile('Debrief.cs');
    const states = new Set(['knocked-out']); // engine state values Debrief.cs compares against (g.RouteGuard), not debrief ids
    const ids = new Set([...src.matchAll(/"([a-z]+(?:-[a-z]+)+)"/g)].map((m) => m[1]!).filter((i) => !states.has(i)));
    const have = new Set([...Object.keys(d.route), ...Object.keys(d.assets), ...Object.keys(d.causes)]);
    expect([...ids].filter((i) => !have.has(i))).toEqual([]);
    // an id here that the engine cannot give is dead text
    expect([...have].filter((i) => !ids.has(i) && i !== 'named-witness')).toEqual([]);
    expect(simAll().join('\n')).toContain('"named-witness:"');
    expect(d.causes['named-witness']).toContain('{who}');
  });
  test('the three public-security states, the three investigation states and both final fights are worded', () => {
    expect(Object.keys(d.security).sort()).toEqual(['calm', 'high', 'raised']);
    expect(Object.keys(d.investigation).sort()).toEqual(['active', 'activeBecause', 'early', 'little']);
    expect(d.investigation.activeBecause).toContain('{cause}');
    expect(d.fight.loud).toMatch(/\{n\}.*\{hits\}/); expect(d.fight.safe).toMatch(/\{n\}.*\{hits\}/);
    expect(d.tally).toMatch(/\{clock\}.*\{usd\}.*\{trail\}/);
  });
  test('no end-card line shows an id, a hash, a path or the engine\'s words', () => {
    const own = strings.filter((s) => s.path.startsWith('debrief.')).filter((s) => !s.path.startsWith('debrief.rule'));
    expect(own.length).toBeGreaterThan(30);
    expect(own.filter((s) => /[a-z]+-[a-z]+-[a-z]+|\bhash\b|replay|out\/play|\btick\b|operative|engine/i.test(s.text)).map((s) => `${s.path}: ${s.text}`)).toEqual([]);
  });
  test('every route and asset line is one plain sentence; the kept line lists them', () => {
    for (const [k, v] of [...Object.entries(d.route), ...Object.entries(d.assets)]) expect(v, k).toMatch(/^[A-Z][^.!?]*[.!?]$/);
    expect(d.kept).toContain('{list}');
  });
  test('brief 013: a line for each between-missions id a takedown adds (rulebook debrief.lines), carrying every field the rulebook lists for it and no other', () => {
    const lines = json('contracts/rules/rulebook.json').debrief.lines as Record<string, { fields: string[] }>;
    const mine = ['volunteer', 'statement', 'missing', 'grief', 'worry', 'area-watch', 'case', 'left-behind', 'captive', 'car-seen'];
    expect(Object.keys(d.between)).toEqual(mine);
    for (const id of mine) {
      expect(lines[id], `${id} is a rulebook debrief line`).toBeTruthy();
      const slots = [...d.between[id]!.matchAll(/\{([a-z-]+)\}/g)].map((m) => m[1]!);
      expect([...new Set(slots)].sort(), id).toEqual([...lines[id]!.fields].sort());
    }
    expect(fillCard(d.between['missing']!, { who: 'Sunita Ferrer', place: 'the service lane', trail: 6 })).toBe('Sunita Ferrer is missing near the service lane. Case open, Trail 6.');
    // the captive who never talks inside the window has its own line, with the same fields less {holds}
    expect([...new Set([...d.getaway.holdsOut.matchAll(/\{([a-z-]+)\}/g)].map((m) => m[1]!))].sort()).toEqual(['held-at', 'rescue-until', 'who']);
    // a line states the fact and its cause plainly: no gore, no verdict on a person
    for (const id of mine) expect(d.between[id], id).not.toMatch(/blood|corpse|murder|killer|victim/i);
  });
  test('brief 014: the Standing line has a cause sentence for every rulebook standing cause and a word for every band; the word causes are rumour and name no one but {who}', () => {
    const rb = json('contracts/rules/rulebook.json');
    expect(rb.debrief.lines['standing'].fields).toEqual(['district', 'from', 'to', 'band', 'cause', 'who']);
    const s = d.standing;
    expect(Object.keys(s.causes).sort()).toEqual(Object.keys(rb.standing.causes).sort());
    expect(Object.keys(s.bands).sort()).toEqual(['hero', 'menace', 'neutral']);
    for (const [id, line] of Object.entries(s.causes)) {
      const slots = [...line.matchAll(/\{([a-z]+)\}/g)].map((m) => m[1]!);
      expect(slots, id).toEqual(id.startsWith('word-') ? ['who'] : []);
      expect(line, id).toMatch(/^[A-Z][^]*[.!?]$/);
      if (id.startsWith('word-')) expect(line, id).toMatch(/^Word is /);
      expect(line, id).not.toMatch(/\b(he|she|him|her|his|hers|man|woman|boy|girl)\b/i);
      expect(line, id).not.toMatch(/blood|corpse|murder|killer|victim/i);
    }
    // the line carries the Standing fields; the cause comes in filled, and the widest line (a named teller) fits
    expect([...new Set([...s.line.matchAll(/\{([a-z]+)\}/g)].map((m) => m[1]!))].sort()).toEqual(['band', 'cause', 'district', 'from', 'to']);
    const cause = fillCard(s.causes['word-knocked-out']!, { who: 'Dimitra Marchetti' });
    expect(fillCard(s.line, { district: 'Harbour', band: s.bands.neutral, from: '+2', to: '+1', cause })).toBe('Harbour: Neutral +1, was +2. Word is a stranger knocked Dimitra Marchetti out.');
    expect(fillCard(s.line, { district: 'Harbour', band: s.bands.hero, from: '+2', to: '+3', cause: fillCard(s.causes['word-helped']!, { who: 'Inez' }) })).toBe('Harbour: Hero +3, was +2. Word is a stranger helped Inez.');
    for (const c of Object.values(s.causes)) expect(fillCard(s.line, { district: 'Harbour', band: 'Neutral', from: '-3', to: '-3', cause: fillCard(c, { who: 'Dimitra Marchetti' }) }).length).toBeLessThanOrEqual(MAX_LINE);
  });
  test('the headline for a win, a withdrawal and a bare failure; a fail with words in the briefing takes its first sentence', () => {
    expect(d.headline.win).toBe('Vale is in the car');
    for (const f of FAILS) if (f !== 'engine-error') expect(t.briefing.fails[f], f).toBeTruthy();
    expect(fillCard(d.tally, { clock: '7:12', usd: 62000, trail: 3 })).toBe('Done at 7:12 · $62,000 spent · Trail 3');
  });
});

describe('lookup and pick', () => {
  test('the most specific entry wins, and a query needing a field the entry omits still finds it', () => {
    const a = lookup(t, { event: 'reply', verb: 'help', speaker: 'waitress', outcome: 'turned', temperament: 'idealist' })!;
    expect(a.temperament).toBe('idealist');
    const fixed = { dialogue: [{ event: 'x', speaker: 'scout', lines: ['A.', 'B.'] }, { event: 'x', speaker: 'scout', outcome: 'o', lines: ['C.', 'D.'] }] } as never;
    expect(lookup(fixed, { event: 'x', speaker: 'scout', outcome: 'o', temperament: 'loyal' })!.lines[0]).toBe('C.');
    expect(lookup(fixed, { event: 'x', speaker: 'scout', outcome: 'p' })!.lines[0]).toBe('A.');
    expect(lookup(fixed, { event: 'y', speaker: 'scout' })).toBeUndefined();
  });
  test('the pick vectors in the file are what the reference function gives', () => {
    for (const v of t.pick.vectors) expect(pickIndex(v.seed, v.key, v.n, v.count), JSON.stringify(v)).toBe(v.index);
    expect(t.pick.vectors.length).toBeGreaterThanOrEqual(6);
  });
  test('the pick is deterministic, depends on the seed, never repeats a variant back to back, and uses every variant', () => {
    const q = { event: 'ack', verb: 'pay', speaker: 'fixer' };
    const run = (seed: string) => Array.from({ length: 24 }, (_, n) => pickLine(t, seed, q, n));
    expect(run('s1')).toEqual(run('s1'));
    expect(run('s1')).not.toEqual(run('s2'));
    const r = run('s1');
    for (let i = 1; i < r.length; i++) expect(r[i]).not.toBe(r[i - 1]);
    expect(new Set(r).size).toBe(3);
    expect(r.every((x) => typeof x === 'string')).toBe(true);
  });
  test('the pick for a key is independent of other keys (same key, same n, same seed, same answer)', () => {
    const a = pickIndex('s', 'a/b', 3, 4), b = pickIndex('s', 'a/b', 3, 4);
    expect(a).toBe(b);
    expect(R.fnv1a32('s|a/b|0')).toBeGreaterThanOrEqual(0);
  });
});

// ---------------------------------------------------------------- brief 014: less reading (Sky, 2026-10-16: "It's the reading, it's too much")
describe('brief 014: the short forms, the price line, the beats', () => {
  const L = t.less;
  const NUM = /\{(m|usd|tick)\}/g;
  const nums = (x: string) => [...x.matchAll(NUM)].map((m) => m[1]!).sort();
  /** A fragment: no sentence break inside, no comma-spliced second sentence, at most `max` words not counting numbers and marks. */
  const words = (x: string) => x.replace(/\{[a-z]+\}/g, '').replace(/[·▲👁✓✗½[\]]/gu, ' ').split(/\s+/).filter((w) => /\p{L}/u.test(w)).length;

  test('every refusal has a pop of a few words that keeps the numbers the long line carries and names no one', () => {
    const bad: string[] = [];
    for (const [code, l] of Object.entries(t.locks)) {
      if (l.short.length > SHORT_MAX) bad.push(`${code}: ${l.short.length} characters`);
      if (words(l.short) > 4) bad.push(`${code}: "${l.short}" is more than four words`);
      if (/[.!?]/.test(l.short)) bad.push(`${code}: "${l.short}" is a sentence`);
      if (/\{who\}|\{op\}/.test(l.short)) bad.push(`${code}: names who or what (the pop sits on it)`);
      if (nums(l.short).join() !== nums(l.line).join()) bad.push(`${code}: numbers ${nums(l.short)} vs ${nums(l.line)}`);
      if (l.short.length >= l.line.length) bad.push(`${code}: not shorter than its line`);
    }
    expect(bad).toEqual([]);
  });
  test('the distance refusals all show the distance as a number', () => {
    const stands = Object.keys(t.locks).filter((c) => /^stand-|^too-far$|^out-of-range$/.test(c));
    expect(stands.length).toBeGreaterThanOrEqual(10);
    for (const c of stands) expect(t.locks[c]!.short, c).toContain('{m} m');
  });
  test('every card has a fate; only a beat card has a beat; its words are at most four and a fragment', () => {
    const bad: string[] = [];
    for (const [code, c] of Object.entries(t.cards.items)) {
      if (!CARD_FATES.includes(c.fate)) bad.push(`${code}: fate ${c.fate}`);
      if ((c.fate === 'beat') !== (c.beat !== undefined)) bad.push(`${code}: fate ${c.fate} and beat ${JSON.stringify(c.beat)}`);
      const lines = typeof c.beat === 'string' ? [c.beat] : Object.values(c.beat ?? {});
      for (const x of lines) { if (words(x) > 4) bad.push(`${code}: "${x}" is more than four words`); if (!/^[A-Z][^.!?,;:]*\.$/.test(x)) bad.push(`${code}: "${x}" is not one short sentence`); }
      if (typeof c.beat === 'object') for (const k of Object.keys(c.beat)) if (typeof c.result !== 'object' || !(k in c.result)) bad.push(`${code}: beat key ${k} is no result key`);
    }
    expect(bad).toEqual([]);
  });
  test('the beats are few: at most ten distinct lines in the feed, and the ones Sky named are there', () => {
    const lines = new Set<string>();
    for (const c of Object.values(t.cards.items)) for (const x of typeof c.beat === 'string' ? [c.beat] : Object.values(c.beat ?? {})) lines.add(x);
    for (const sc of t.scenes.items) if (sc.beat) lines.add(sc.beat);
    expect(lines.size).toBeLessThanOrEqual(10);
    for (const x of ['Inez called the guard.', 'Body found.', 'Police coming.']) expect(lines.has(x), x).toBe(true);
    expect(L.feed.maxOnScreen).toBeLessThanOrEqual(2);
  });
  test('a scene beat belongs to a call or a city scene; the call beat is Inez\'s, the one the capstone turns on', () => {
    const withBeat = t.scenes.items.filter((sc) => sc.beat);
    expect(withBeat.length).toBeGreaterThan(0);
    for (const sc of withBeat) expect(['call', 'city']).toContain(sc.kind);
    const call = t.scenes.items.filter((sc) => sc.kind === 'call' && sc.about === 'waitress');
    expect(call.length).toBeGreaterThanOrEqual(4);
    for (const sc of call) expect(sc.beat).toBe('Inez called the guard.');
  });
  test('the Fixer\'s done lines on a turn are the Fixer\'s own terse report-back: first person or plain report, no gendered word, none that narrates for the Fixer', () => {
    const GENDER = /\b(he|she|him|her|hers|his|himself|herself|man|men|woman|women|boy|girl|sir|madam|lady|lord)\b/i;
    const NARRATION = /\b(will be remembered|was noticed|and they'll remember|remembered)\b|^A kindness/i;
    const rows = t.dialogue.filter((d) => d.event === 'done' && d.verb === 'turn');
    expect(rows.length).toBe(5);
    const bad: string[] = [];
    for (const d of rows) for (const l of d.lines) { if (GENDER.test(l)) bad.push(`gendered: ${l}`); if (NARRATION.test(l)) bad.push(`narration: ${l}`); }
    expect(bad).toEqual([]);
    expect(rows.find((d) => d.outcome === 'refused')!.lines[0]).toBe("They said no. They'll remember me.");
  });
  test('H12 teaches the run toggle: it names the key the game binds (G), the one place a key is taught', () => {
    const h = t.hints.items.H12!;
    expect(h.verb).toBe('move');
    expect(h.agent).toBe('any');
    expect(h.line).toMatch(/\bG\b/);
    expect(h.short).toMatch(/\bG\b/);
    expect(h.short).toBe('Run to the [mark]: press G.');
    expect(marked(h.short)).toEqual(['mark']);
    // the same key as the game's (PlayControls: PlayAction.Gait on Key.G)
    const controls = join(root, 'unity/Assets/Suborned/Runtime/Play/PlayControls.cs');
    if (existsSync(controls)) expect(read('unity/Assets/Suborned/Runtime/Play/PlayControls.cs')).toMatch(/PlayAction\.Gait,\s*keys = new\[\] \{ Key\.G \}/);
  });
  test('H13 teaches the view turn in two causes: the door the walls hide (Q twice, rings the door) and the 0:20 timer (Q / E); the keys are tokens the host draws as keycaps, the game\'s own keys, retired for good', () => {
    const h = t.hints.items.H13!;
    expect(h.verb).toBe('turn-view');
    expect(h.agent).toBe('any');
    expect(h.priority).toBe(0);
    expect(h.short).toBe('Turn the view: <TurnLeft> <TurnRight>'); // the timer form: nothing in the street is ringed; the host draws the glyph, the words, then Q and E as keycaps
    expect(marked(h.short)).toEqual([]);
    expect(h.door!.short).toBe('Turn twice: <TurnLeft> <TurnLeft>'); // the door form: two quarter turns reach the Office front from the opening view (yaw 30 to 210); the ring on the door marker points, so no noun
    expect(marked(h.door!.short)).toEqual([]);
    expect(shownWords(h.door!.short)).toBe(4); // keycaps counted, as the HUD budget counts them
    expect(shownWords(h.short)).toBe(5);
    expect(h.door!.line).toMatch(/\bQ twice\b/);
    expect(h.door!.line).toMatch(/public door/);
    expect(h.trigger).toMatch(/hides it/);
    expect(h.rings).toMatch(/door/);
    expect(h.line).toMatch(/\bQ\b/);
    expect(h.line).toMatch(/\bE\b/);
    expect(h.line).toMatch(/\bagent\b/i);
    expect(h.trigger).toMatch(/0:20/);
    expect(h.trigger).toMatch(/never turned/);
    expect(h.goneWhen).toMatch(/for good/);
    const controls = join(root, 'unity/Assets/Suborned/Runtime/Play/PlayControls.cs');
    if (existsSync(controls)) {
      const src = read('unity/Assets/Suborned/Runtime/Play/PlayControls.cs');
      expect(src).toMatch(/PlayAction\.TurnLeft,\s*keys = new\[\] \{ Key\.Q \}/);
      expect(src).toMatch(/PlayAction\.TurnRight,\s*keys = new\[\] \{ Key\.E \}/);
    }
  });
  test('a beat keeps its card\'s sight and needs rules (the cell is never told what it did not see): the cards that said in-sight still do', () => {
    for (const [code, c] of Object.entries(t.cards.items)) if (c.fate === 'beat' && /found|confirmed|opened|serviced/.test(code)) expect(c.sight, code).toBe('in-sight');
    expect(t.cards.items['rivals.arrive']!.sight).toBe('in-sight');
    expect(t.cards.items['post.missed']!.needs).toBe('routine');
    expect(t.cards.items['post.missed']!.fate).not.toBe('beat'); // a missed check in is a clock pip over the guard, once the routine is learned
  });
  test('no new short form uses a gendered word', () => {
    const GENDER = /\b(he|she|him|her|hers|his|himself|herself|man|men|woman|women|boy|girl|sir|madam|lady|lord)\b/i;
    const mine = strings.filter((s) => /^(locks\.[a-z-]+\.short|hints\.H\d+\.(door\.)?short|briefing\.timeline\[\d\]\.short|cards\..*\.beat|scenes\[\d+\]\.beat|less\.)/.test(s.path));
    expect(mine.length).toBeGreaterThan(150);
    expect(JSON.stringify(t.less).match(GENDER)).toBeNull(); // the rules, fates and grammar too
    expect(mine.filter((s) => GENDER.test(s.text)).map((s) => `${s.path}: ${s.text}`)).toEqual([]);
  });
  test('the Next Step is one imperative with one ringed noun, at most six words, no agent, no approach, no time and no sequence', () => {
    const lead: Record<string, RegExp> = { enter: /^Go in\b/, board: /^Board\b/, go: /^Go:/, take: /^Take\b/, move: /^(Run|Walk)\b/, watch: /^Watch\b/, listen: /^Listen\b/, turn: /^(Turn|Call)\b/, loop: /^Loop\b/, call: /^Call\b/, reroute: /^Reroute\b/, mislead: /^Give\b.*false tip/, fire: /^(Cover|Fire at)\b/, carry: /^Carry\b/, 'drive-car': /^Drive\b/, 'turn-view': /^Turn (the view|twice)\b/, cease: /^Cease fire: press X\.$/, 'step-target': /^Scroll to switch\.$/ };
    const bad: string[] = [];
    const forms = Object.entries(t.hints.items).flatMap(([id, h]) => [{ id, h, short: h.short, line: h.line, ring: id === 'H13' || id === 'H19' || id === 'H21' || id === 'H22' ? 0 : 1 }, ...(h.door ? [{ id: `${id}.door`, h, short: h.door.short, line: h.door.line, ring: 0 }] : [])]);
    for (const { id, h, short, line, ring } of forms) {
      const bare = short.replace(/<[A-Za-z]+>/g, ''); // for the other rules, a key token is no word
      const n = marked(short);
      if (n.length !== ring) bad.push(`${id}: ${n.length} ringed nouns`); // both H13 forms ring no noun in the pop (the view is no thing in the street; the door's ring is the marker itself)
      if (shownWords(short) > 6) bad.push(`${id}: more than six words, a keycap counting as one (the HUD budget's count)`);
      if (!(lead[h.verb] ?? /^$/).test(short)) bad.push(`${id}: "${short}" does not lead with its verb (${h.verb})`);
      if (/\b(Scout|Fixer|Tech|Enforcer|Ask|Pay|Help|Pressure|first|then|before|after|next|until|once)\b|\d/.test(bare)) bad.push(`${id}: names an agent, an approach, a time or a sequence`);
      if (short.length >= line.length) bad.push(`${id}: not shorter than its line`);
      expect(nums(bare), id).toEqual([]);
    }
    expect(bad).toEqual([]);
  });
  test('the objective is one imperative with a ringed noun for each thing that has a marker, and the briefing goal says the same', () => {
    expect(marked(L.objective.take)).toEqual(['Vale']);
    expect(marked(L.objective.bring)).toEqual(['Vale']);
    expect(marked(L.objective.carry)).toEqual(['Vale', 'car']);
    expect(marked(L.briefing.goal)).toEqual(['Vale', 'car']);
    for (const x of [L.objective.take, L.objective.bring, L.objective.carry]) expect(words(x)).toBeLessThanOrEqual(5);
    expect(L.objective.take).toBe('Take [Vale] alive.');
    expect(L.objective.bring).toBe('Bring [Vale] out.');
    expect(L.objective.carry).toBe('Get [Vale] to the [car].');
  });
  test('the goal line progresses take, bring, carry: each step names a line and the state the host reads', () => {
    expect(L.objective.steps.map((x) => x.key)).toEqual(['take', 'bring', 'carry', 'go']);
    for (const x of L.objective.steps) { expect(L.objective[x.key], x.key).toBeTruthy(); expect(x.when.length, x.key).toBeGreaterThan(20); }
    expect(L.objective.steps[1]!.when).toMatch(/Enter/);
    expect(L.objective.steps[2]!.when).toMatch(/Vale is with the Enforcer/);
  });
  test('H14–H16 teach the end steps: Enter at the open door, Board at the car, Take at the pickup; each for the Enforcer, each short form leads with its verb', () => {
    const [a, b, c] = [t.hints.items.H14!, t.hints.items.H15!, t.hints.items.H16!];
    expect([a.verb, b.verb, c.verb]).toEqual(['enter', 'board', 'take']);
    for (const h of [a, b, c]) { expect(h.agent).toBe('enforcer'); expect(h.line).toMatch(/\bEnforcer\b/); expect(h.priority).toBeLessThan(0); expect(marked(h.short).length).toBe(1); expect(words(h.short)).toBeLessThanOrEqual(6); }
    expect(a.short).toBe('Go in by the [door].');
    expect(b.short).toBe('Board the [car] with Vale.');
    expect(c.short).toBe('Take [Vale] from the back seat.');
    // H14's Alt line says that being seen on the way in fails (plain words, brief 019)
    expect(a.line).toMatch(/if he is seen, it fails/);
    // the triggers say what Latch builds
    expect(a.trigger).toMatch(/guard/); expect(a.trigger).toMatch(/camera/); expect(a.trigger).toMatch(/Vale is in the Office/);
    expect(b.trigger).toMatch(/come out with Vale/);
    expect(c.trigger).toMatch(/driver is turned/); expect(c.trigger).toMatch(/Vale's car is parked/); expect(c.trigger).toMatch(/service bay/);
    // the rings are things the street has markers for
    expect(a.rings).toBe('the public door'); expect(b.rings).toBe('the getaway car');
    // a hint's verb is one the action panel gives, and the enforcer gives it
    for (const h of [a, b, c]) expect(t.actions[h.verb as keyof typeof t.actions]!.by).toContain('enforcer');
  });
  test('H17 teaches the fire order in a fight: hold right click on a hostile, for any armed agent, behind H15 so the end step wins while a fight holds the car', () => {
    const h = t.hints.items.H17!;
    expect(h.verb).toBe('fire');
    expect(h.agent).toBe('any');
    expect(h.short).toBe('Fire at [{who}]: hold right.');
    expect(marked(h.short)).toEqual(['{who}']);
    expect(words(h.short)).toBeLessThanOrEqual(6);
    expect(h.line).toMatch(/hold right click/);
    expect(h.line).toMatch(/\bagent/);
    expect(h.priority).toBeGreaterThan(t.hints.items.H15!.priority);
    expect(h.priority).toBeLessThan(0);
    expect(h.trigger).toMatch(/other than the Enforcer/);
    expect(h.goneWhen).toMatch(/fires/); expect(h.goneWhen).toMatch(/fight ends/);
    expect(h.rings).toBe('the hostile');
    expect(t.actions.fire!.by.length).toBeGreaterThan(1); // more than the Enforcer can fire
  });
  test('the car marker chip reads less.pop.car-held (a pop of three words); Go on a held car reads the car-held refusal, which names the holders (brief 018)', () => {
    expect(L.pop['car-held']).toBe('Fight blocks car');
    expect(t.locks['car-held']!.short).toBe('Blocked · down {holders}');
    expect(t.locks['car-held']!.line).toContain('{holders}');
    expect(t.locks['car-held']!.verbs).toEqual(['go']);
    expect(fillLock(t, { code: 'car-held', ids: 'rival-1' }, {})).toBe('The car is blocked by a rival. Down them, or drive them off, then Go.');
    expect(fillLock(t, { code: 'car-held', ids: 'officer-1,officer-2,rival-1' }, {})).toContain('blocked by hostiles');
    expect(fillLock(t, { code: 'car-held', ids: 'robot-1,robot-2' }, {})).toContain('blocked by 2 robots');
  });
  test('engine 0.33.0 (brief 059): car-held with the rival car among the holders counts down to its first step-out; with no step-out yet it only says the car is driving in', () => {
    const left = (tk: number) => `${Math.max(0, Math.ceil((tk - 1000) / 10))} s`;
    const clock = (tk: number) => `${Math.floor(tk / 600)}:${String(Math.floor(tk / 10) % 60).padStart(2, '0')}`;
    const lk = t.locks['car-held']!;
    expect(lk.tickAs).toBe('left');
    expect(Object.keys(lk.byTarget ?? {}).sort()).toEqual(['rival-car', 'rival-car-driving']);
    expect(lk.byTarget!['rival-car']!.line).toContain('{tick}'); expect(lk.byTarget!['rival-car']!.short).toContain('{tick}');
    expect(lk.byTarget!['rival-car-driving']!.line).not.toContain('{tick}'); expect(lk.byTarget!['rival-car-driving']!.short).not.toContain('{tick}');
    expect(lk.line).not.toContain('{tick}'); expect(lk.short).not.toContain('{tick}');
    const a = { code: 'car-held', ids: 'car:rivals', tick: 1180 };
    expect(fillLock(t, a, { left })).toBe('The rival car is coming. Its crew is out in 18 s. Down them, then Go.');
    expect(fillLockShort(t, a, { left })).toBe('Rivals coming · 18 s');
    expect(fillLockShort(t, a, { clock })).toBe('Rivals coming · 1:58'); // no `left` given: the mission clock, still a word the player can read
    expect(fillLock(t, { code: 'car-held', ids: 'car:rivals,rival-3', tick: 1180 }, { left })).toContain('rival car is coming');
    expect(fillLockShort(t, { code: 'car-held', ids: 'car:rivals', tick: -1 })).toBe('Rivals coming');
    expect(fillLock(t, { code: 'car-held', ids: 'car:rivals', tick: -1 })).toBe('The rival car is still driving in. Wait for its crew, down them, then Go.');
    // without the rival car the base line and its short answer as before, whatever the tick
    expect(fillLockShort(t, { code: 'car-held', ids: 'rival-3' })).toBe('Blocked · down a rival');
    expect(fillLock(t, { code: 'car-held', ids: 'rival-3', tick: -1 })).toBe('The car is blocked by a rival. Down them, or drive them off, then Go.');
    // the fail card's "{holders} held the car" says a rival when the rival car held it
    expect(holdersPhrase(t, 'car:rivals')).toBe('a rival');
    expect(roleOfId('car:rivals')).toBe('rival');
    // a variant's pop fits the pop
    for (const [c, l] of Object.entries(t.locks)) for (const v of Object.values(l.byTarget ?? {})) expect(v.short.length, c).toBeLessThanOrEqual(SHORT_MAX);
  });
  test('engine 0.33.0 (brief 059): every id of the final fight\'s why has a clause, and every fire cause a word', () => {
    if (!haveSim) return;
    const why = new Set<string>();
    for (const src of simAll()) for (const m of src.matchAll(/FightWhy\.Add\("([a-z0-9-]+)"/g)) why.add(m[1]!);
    expect(why.size).toBeGreaterThanOrEqual(9);
    expect(Object.keys(t.debrief.fightWhy).sort()).toEqual([...why].sort());
    for (const [k, v] of Object.entries(t.debrief.fightWhy)) expect(v, k).toMatch(/^[A-Z][^.!?]*\.$/);
    for (const id of ['rivals-recalled', 'crew-destroyed']) expect(t.debrief.fightWhy[id], id).toBeTruthy();
    const f = simFile('GameFireWhy.cs');
    const order = f.slice(f.indexOf('FireCauseOrder = {'), f.indexOf('};', f.indexOf('FireCauseOrder = {')));
    const causes = [...order.matchAll(/"([a-z]+)"/g)].map((m) => m[1]!);
    expect(causes).toContain('crewmate');
    expect(Object.keys(L.why)).toEqual(causes); // the engine's order, strongest first
    for (const [k, v] of Object.entries(L.why)) { expect(v.split(' ').length, k).toBeLessThanOrEqual(3); expect(v, k).not.toMatch(/[.!?]/); }
  });
  test('engine 0.33.0 (brief 059): the crew\'s six new log codes are worded or silent with a reason, and the beats stay at the cap', () => {
    for (const c of ['rivals.recalled', 'rivals.wiped', 'rivals.see-takedown']) {
      expect(t.cards.items[c], c).toBeTruthy();
      expect(Object.values(t.events).some((e) => e.log?.includes(c)), `${c} is spoken by an event`).toBe(true);
    }
    for (const c of ['rivals.wakes', 'rivals.frees']) { expect(t.cards.items[c], c).toBeTruthy(); expect(t.silentLogs[c], c).toMatch(/card only/); }
    expect(t.silentLogs['rivals.gone']).toMatch(/silent/);
    expect(t.cards.items['rivals.wakes']!.result).not.toMatch(/report|statement/); // a crewmate makes none (rivals.peopleRule)
    for (const e of ['rivals-recalled', 'rivals-wiped', 'rivals-saw-takedown']) for (const sp of t.events[e]!.speakers) expect(lookup(t, { event: e, speaker: sp }), `${e} ${sp}`).toBeTruthy();
    for (const c of ['rivals.recalled', 'rivals.wiped', 'rivals.see-takedown', 'rivals.wakes', 'rivals.frees']) expect(t.cards.items[c]!.fate, c).not.toBe('beat'); // the feed is at ten beats: none of these takes one
  });
  test('the price line: a canonical field order, a list for every verb that starts with the name and keeps that order', () => {
    expect(L.price.order).toEqual([...PRICE_FIELDS]);
    expect(Object.keys(L.price.verbs).sort()).toEqual([...VERBS].sort());
    expect(Object.keys(L.price.fields).sort()).toEqual([...PRICE_FIELDS].sort());
    for (const [v, fs] of Object.entries(L.price.verbs)) {
      expect(fs[0], v).toBe('name');
      const at = fs.map((f) => PRICE_FIELDS.indexOf(f));
      expect(at, v).toEqual([...at].sort((a, b) => a - b));
      expect(new Set(fs).size, v).toBe(fs.length);
    }
    for (const v of ['ask', 'pay', 'help', 'pressure']) expect(L.price.verbs[v as (typeof VERBS)[number]]).toEqual(L.price.verbs.pay);
    // every field a verb's card prices is on its line: the engine's price is ($, time, Trail, Alert risk); an order that traces, spends or risks shows it
    for (const v of ['pay', 'help', 'ask', 'pressure', 'takedown', 'fire']) { expect(L.price.verbs[v as (typeof VERBS)[number]]).toContain('trail'); expect(L.price.verbs[v as (typeof VERBS)[number]]).toContain('alert'); }
    for (const v of ['pay', 'help']) expect(L.price.verbs[v as (typeof VERBS)[number]]).toContain('usd');
    expect(L.price.verbs.takedown).toContain('eye');
  });
  test('the price examples follow the grammar: a verb or way label, then fields in order, no zero field', () => {
    const labels = new Set<string>();
    for (const a of Object.values(t.actions)) { labels.add(a.label); for (const w of Object.values(a.ways ?? {})) labels.add(w.label); }
    // compact money (brief 014): $90k, $1.5k, under $1,000 as it is, a range $40k–60k; whole dollars are for the status pill and the debrief
    const COMPACT = /^\$(\d{1,3}|\d{1,3}(\.\d)?k)(–\d{1,3}(\.\d)?k?)?$/;
    const kind = (f: string) => /^\$/.test(f) ? 'usd' : /^\d+ s$/.test(f) ? 'time' : /^\+\d+(\/hit)?$/.test(f) ? 'trail' : /^▲/.test(f) ? 'alert' : /^\d+:\d\d$/.test(f) ? 'lasts' : /^👁/u.test(f) ? 'eye' : /^[✓✗½?]$/u.test(f) ? 'mark' : 'unknown';
    expect(Object.keys(L.price.examples).sort()).toEqual(['ask', 'board', 'disable', 'drive-car', 'fire', 'fire-camera', 'go', 'help', 'kill', 'knockout', 'loop', 'pay', 'pressure']); // loop and disable: brief 058; fire-camera: a camera's shot is priced once, never per hit
    for (const [k, ex] of Object.entries(L.price.examples)) {
      const parts = ex.split(' · ');
      expect(labels.has(parts[0]!), `${k}: ${parts[0]}`).toBe(true);
      const ks = parts.slice(1).map(kind);
      expect(ks, ex).not.toContain('unknown');
      for (const p of parts.filter((x) => /^\$/.test(x))) expect(p, `${k}: money on a price line is compact`).toMatch(COMPACT);
      expect(ex, `${k}: no whole-dollar thousands on a price line`).not.toMatch(/\d,\d{3}/);
      const at = ks.map((x) => PRICE_FIELDS.indexOf(x as (typeof PRICE_FIELDS)[number]));
      expect(at, ex).toEqual([...at].sort((a, b) => a - b));
      expect(parts.some((p) => /^\$0$|^\+0$|^0 s$/.test(p)), `${ex} shows a zero`).toBe(false);
      expect(ex.length, ex).toBeLessThanOrEqual(40);
    }
    expect(L.price.examples.knockout).toBe('Knock out · 7 s · +4 · ▲1 · 👁 none');
    expect(L.price.examples.pay).toBe('Pay · $90k · 50 s · +2 · ✓');
    expect(L.price.examples.help).toBe('Help · $40k · 50 s · +1 · ½');
    expect(L.price.fields.usd.shows).toContain('$90k');
    expect(L.price.fields.usd.shows).toContain('$40k–60k');
  });
  test('the end card is one line: a headline for each fail code, and for a win and a withdrawal', () => {
    expect(Object.keys(L.end.fail).sort()).toEqual(Object.keys(t.briefing.fails).sort());
    for (const [k, x] of Object.entries(L.end.fail)) expect(x, k).toMatch(/^[A-Z][^.!?]*\.$/);
    expect(L.end.win).toBe('Vale is in the car.');
  });
  test('cancel costs and pops are a few words; every cancel cost has one', () => {
    expect(Object.keys(L.cancel).sort()).toEqual(Object.keys(t.cancelCosts).sort());
    for (const [k, x] of [...Object.entries(L.cancel), ...Object.entries(L.pop)]) { expect(words(x), k).toBeLessThanOrEqual(3); expect(x, k).not.toMatch(/[.!?]/); }
    // the host builds these refusals itself today: HandoverHost (Pop), HostFight, HostTakedown
    for (const id of ['no-way', 'no-room', 'unarmed', 'not-drawn', 'other-carries', 'nothing-there', 'never-vale', 'who-can', 'nothing-here', 'select-agent']) expect(L.pop[id], id).toBeTruthy();
  });
  test('brief 019: the fail camera has a caption of at most six words and a tip of at most six for every way a run can fail (Cause.fail and why), and the same keys in both', () => {
    const F = L.failcam;
    const ways: Record<string, string[]> = {
      'entry-seen-guard': ['seen'], 'entry-seen-camera': ['seen'], 'abduction-seen-camera': ['seen'],
      'rivals-reach-vale': ['beat-to-pickup', 'downed-enforcer', 'reached-at-close', 'reached-car'], 'vale-leaves': ['too-late'], 'vale-dead': ['killed', 'shot'],
      'enforcer-down': ['shot'], 'cell-down': ['shot'], 'driver-shot': ['shot'], 'getaway-lost': ['wrecked'], 'sealed': ['response', 'confirmed', 'loud-fight', 'desk', 'heard'], 'engine-error': ['error'] };
    expect(Object.keys(F.caption).sort()).toEqual(Object.keys(ways).sort());
    for (const [f, ws] of Object.entries(ways)) expect(Object.keys(F.caption[f]!).sort(), f).toEqual([...ws].sort());
    for (const f of FAILS) expect(f in F.caption, f).toBe(true);
    // tips: every way but a broken engine
    for (const [f, ws] of Object.entries(ways)) if (f !== 'engine-error') expect(Object.keys(F.tip[f]!).sort(), f).toEqual([...ws].sort());
    expect('engine-error' in F.tip).toBe(false);
    // words once filled (the HUD line limit is six): {by} is two words ("The guard") except where a camera or a police officer can be the culprit (three)
    const three = new Set(['entry-seen-camera.seen', 'abduction-seen-camera.seen', 'sealed.response', 'sealed.confirmed', 'enforcer-down.shot', 'cell-down.shot', 'driver-shot.shot', 'vale-dead.shot']);
    const bad: string[] = [];
    for (const [f, m] of Object.entries(F.caption)) for (const [w, x] of Object.entries(m)) {
      const filled = x.replace(/\{by\}/g, three.has(`${f}.${w}`) ? 'The entrance camera' : 'The guard').replace(/\{agent\}/g, 'the Enforcer');
      if (words(filled) > 6) bad.push(`${f}.${w}: ${words(filled)} words: ${filled}`);
      if (/!/.test(x)) bad.push(`${f}.${w}: exclamation`);
    }
    for (const [f, m] of Object.entries(F.tip)) for (const [w, x] of Object.entries(m)) if (words(x) > 6) bad.push(`tip ${f}.${w}: ${words(x)} words`);
    expect(bad).toEqual([]);
    for (const k of ['guard', 'patrol', 'officer', 'robot', 'rival', 'rivals', 'person', 'car']) expect(F.by[k], k).toBeTruthy();
  });
  test('brief 043: the serving-a-table refusal is retired (engine 0.23 walks the agent up to the server)', () => {
    expect(t.locks['too-far']!.byTarget?.['serving' as 'machine']).toBeUndefined();
    expect(fillLock(t, { code: 'too-far', m: 4 }, { who: 'Inez' })).toBe('Too far from Inez. Walk within 4 m first.');
    expect(JSON.stringify(t.locks)).not.toMatch(/serving/);
  });
  test('the timeline marks have a word for Alt', () => {
    // brief 059 (engine 0.32.0, GDD v0.25.0 §07): the rival car's entry (2:35) and drop (2:46) are marks; 3:10 is its park (Keel's drafts, Fable words them)
    expect(t.briefing.timeline.map((x) => x.short)).toEqual(['Rivals in', 'Rivals post', 'Rivals park', 'Chime', 'Last chance', 'Vale out', 'Vale at car', 'Close']);
  });
  test('every player-facing group of the file has a fate, and the fates are the brief\'s', () => {
    for (const f of Object.values(L.fates)) expect(FATES).toContain(f);
    for (const g of ['briefing', 'targets', 'actions', 'cancelCosts', 'activity', 'locks', 'cards', 'debrief', 'hints', 'toasts', 'goingLoud', 'dialogue', 'scenes'])
      expect(Object.keys(L.fates).some((p) => p === g || p.startsWith(g + '.')), g).toBe(true);
    expect(L.fates.dialogue).toBe('speech');
    expect(L.fates['toasts.run-seen']).toBe('cut');
    expect(L.fates['briefing.fails']).toBe('debrief');
  });
  test('nothing was deleted: the long lines are still there for Alt and the debrief', () => {
    for (const [v, a] of Object.entries(t.actions)) expect(a.does.length, v).toBeGreaterThan(10);
    for (const l of Object.values(t.locks)) expect(l.line.length).toBeGreaterThan(l.short.length);
    for (const h of Object.values(t.hints.items)) expect(h.line.length).toBeGreaterThan(h.short.length);
    expect(Object.keys(t.cards.items).length).toBe(80); // 58 + camera.cut and camera.cut-reported (brief 058, engine 0.31.0), less car.blocks (brief 059, engine 0.32.0: the driver's Call is retired, Motive M059-6.1), plus the eleven worded codes of the rival car and Vale's car (rivals.coming, drop, follow-car, follows, back, warned, no-warning, at-vale; car.parks, car.blocked; board.missed), plus the five of the rival crew as people and the floor (rivals.recalled, wiped, see-takedown, wakes, frees; engine 0.33.0), plus the two of the held car (rivals.for-car, rivals.back-to-cover; engine 0.34.0, which the host already makes fallback cards of), plus the three of the patrol's report of the Enter (entry.seen, entry.reported, entry.stopped; engine 0.34.2)
    expect(t.briefing.card.length).toBeGreaterThanOrEqual(3);
    expect(t.toasts['run-seen']!.line).toMatch(/noticed/);
  });
});

// ---------------------------------------------------------------- controls trial 2026-10-24, brief 054 mood words, brief 057 sealed line
const words = (x: string) => x.replace(/\{[a-z]+\}/g, '').replace(/[·▲👁✓✗½[\]]/gu, ' ').split(/\s+/).filter((w) => /\p{L}/u.test(w)).length;
describe('controls trial 2026-10-24: no line teaches the old mapping (right click opens the ring)', () => {
  test('no hint, tutorial or first-act line says right click or right tap except H17, which teaches the right hold that fires', () => {
    const bad = strings.filter((s) => /right[- ](click|tap)|right-click/i.test(s.text) && !s.path.startsWith('hints.H17')).map((s) => `${s.path}: ${s.text}`);
    expect(bad).toEqual([]);
    expect(JSON.stringify(t.hints.items.H17)).toMatch(/hold right/);
    expect(t.less.hint.firstAct).toBe('Click a thing to act.');
  });
  test('the click hints say click, and ringed nouns stay one each', () => {
    for (const id of ['H3', 'H5']) { const h = t.hints.items[id]!; expect(h.short, id).toMatch(/^(Turn|Call) \[\{who\}\]: click\.$/); expect(h.line, id).toMatch(/\bClick\b/); expect(marked(h.short), id).toEqual(['{who}']); expect(words(h.short), id).toBeLessThanOrEqual(6); }
    expect(t.hints.items.H18!.line).toMatch(/Click the car/);
    expect(t.hints.items.H18!.line).not.toMatch(/right/i);
  });
});
describe('brief 054: the mood words', () => {
  const M = t.less.mood;
  test('the footer is "{who} · {mood} · {lever} likely"; hurt and bare forms drop what is missing; the done chip words are the capitalised moods', () => {
    expect(M.footer).toBe('{who} · {mood} · {lever} likely');
    expect(M.footerHurt).toContain('{lever} ' + M.hurt);
    expect(M.footerBare).toBe('{who} · {mood}');
    for (const f of [M.footer, M.footerHurt, M.footerBare]) { expect(f).toContain('{who}'); expect(f).toContain('{mood}'); }
    for (const m of ['friendly', 'uneasy', 'scared'] as const) {
      expect(M.word[m]).toBe(m);
      expect(t.less.done.gain[`mood-${m}`], m).toBe(m[0]!.toUpperCase() + m.slice(1));
      expect(words(t.less.alt.mood[m]), m).toBeGreaterThan(3);
      expect(t.less.alt.mood[m]!.startsWith(t.less.done.gain[`mood-${m}`]!), m).toBe(true);
    }
    expect(Object.keys(M.lever).sort()).toEqual(['help', 'pay', 'pressure']);
  });
  test('the Alt mood lines agree with the rulebook shift: every approach a mood moves is named once, up as easier and down as harder', () => {
    const shift = (rb as unknown as { mood: { shift: Record<string, Record<string, number>> } }).mood.shift;
    for (const [m, line] of Object.entries(t.less.alt.mood)) for (const [ap, step] of Object.entries(shift[m]!)) {
      const w = ap[0]!.toUpperCase() + ap.slice(1);
      if (step > 0) expect(line, `${m} ${ap}`).toMatch(new RegExp(`${w}( and \\w+)? comes? easier|\\b${w} comes easier`));
      if (step < 0) expect(line, `${m} ${ap}`).toMatch(new RegExp(`${w}( and \\w+)? comes? harder|\\b${w} comes harder`));
    }
  });
});
describe('brief 057 section 4: the sealed fail line says what happened by the car\'s state', () => {
  const S = t.less.sealed;
  test('the unknown line is the briefing line and says the engine\'s fail; no line says Vale did not reach a car he reached', () => {
    expect(S.line.unknown).toBe(t.briefing.fails.sealed);
    expect(S.line.unknown).toMatch(/car hadn't left when security sealed the district/);
    expect(JSON.stringify(t.briefing.fails)).not.toMatch(/before Vale reached/);
    expect(S.line.held).toContain('{holders}');
    expect(S.line.ready).toMatch(/Vale was in the car/); expect(S.line.ready).toMatch(/Go/);
    expect(S.line.empty).toMatch(/Vale wasn't in the car/);
    expect(S.story.ready).toBe('Go was never given.');
    expect(S.story.held).toContain('{holders}');
  });
  test('the tips are at most six words and name Go where Go is the fix', () => {
    for (const [k, x] of Object.entries(S.tip)) { expect(words(x), k).toBeLessThanOrEqual(6); expect(x, k).toMatch(/[.]$/); }
    expect(S.tip.held).toMatch(/Go/); expect(S.tip.ready).toMatch(/Go/);
  });
});

// ---------------------------------------------------------------- brief 016: Intel fragments (Fable, 2026-10-18)
describe('brief 016: the Intel panel\'s fragments cover every need, temperament and secret the cast and the rules can produce', () => {
  const I = t.less.intel;
  const cast = json('contracts/people/harbour-district.cast.json') as { rows: { needs: { need: string }[]; temperaments: { temperament: string }[]; secrets?: { secret: string }[] }[] };
  const promo = json('contracts/people/harbour-district.cast.promotion.json') as { cast: { file: string } };
  const GENDER = /\b(he|she|him|her|hers|his|himself|herself|man|men|woman|women|boy|girl|sir|madam|lady|lord)\b/i;
  const needsRule = Object.keys(needsKinds.kinds); // brief 017: the kinds of needs.rules.json (the retired `secret` is not one)
  const tempRule = Object.keys(people.temperaments);
  const secretRule = Object.keys(needsKinds.secretKinds);
  const missing = (have: Record<string, string>, want: Iterable<string>) => [...new Set(want)].filter((k) => !(k in have)).sort();
  const words = (x: string) => x.replace(/\{[a-z]+\}/g, '').split(/\s+/).filter((w) => /\p{L}/u.test(w)).length;

  test('the cast file is the one promoted', () => { expect(promo.cast.file).toBe('contracts/people/harbour-district.cast.json'); });
  test('every need the rules list or a cast table gives has a fragment', () => {
    expect(missing(I.need, needsRule)).toEqual([]);
    expect(missing(I.need, cast.rows.flatMap((r) => r.needs.map((n) => n.need)))).toEqual([]);
  });
  test('every temperament the rules list or a cast table gives has a fragment', () => {
    expect(missing(I.temperament, tempRule)).toEqual([]);
    expect(missing(I.temperament, TEMPERAMENTS)).toEqual([]);
    expect(missing(I.temperament, cast.rows.flatMap((r) => r.temperaments.map((n) => n.temperament)))).toEqual([]);
  });
  test('every secret the rules list or a cast table gives has a fragment, plus the named lover and "none"', () => {
    expect(missing(I.secret, secretRule)).toEqual([]);
    expect(missing(I.secret, cast.rows.flatMap((r) => (r.secrets ?? []).map((n) => n.secret)))).toEqual([]);
    expect(I.secret['affair-named']).toContain('{name}');
    expect(I.secret.none).toBeDefined();
  });
  test('the verbs the slots and chip use have a word', () => { expect(missing(I.verb, ['watch', 'listen', 'turn'])).toEqual([]); });
  test('nothing here is stale: a fragment names a value the rules still have', () => {
    const extra = (have: Record<string, string>, ok: string[]) => Object.keys(have).filter((k) => !ok.includes(k));
    expect(extra(I.need, needsRule)).toEqual([]);
    expect(extra(I.temperament, tempRule)).toEqual([]);
    expect(extra(I.secret, [...secretRule, 'affair-named', 'none'])).toEqual([]);
  });
  test('each is a fragment: one or two words, short, no sentence, no gendered word, none repeated within a slot', () => {
    const bad: string[] = [];
    for (const [slot, m] of Object.entries({ need: I.need, temperament: I.temperament, secret: I.secret, verb: I.verb })) {
      const seen = new Set<string>();
      for (const [k, x] of Object.entries(m)) {
        if (words(x) < 1 || words(x) > 2) bad.push(`${slot}.${k}: "${x}" is not one or two words`);
        if (/[.!?,;:]/.test(x)) bad.push(`${slot}.${k}: "${x}" is a sentence`);
        if (GENDER.test(x)) bad.push(`${slot}.${k}: "${x}" is gendered`);
        if (x.replace('{name}', 'Bartholomew').length > 20) bad.push(`${slot}.${k}: "${x}" is long`);
        if (seen.has(x)) bad.push(`${slot}.${k}: "${x}" repeats`);
        seen.add(x);
      }
    }
    expect(bad).toEqual([]);
  });
  test('brief 017: no record\'s name lives here: need and secret hold one fallback per kind and nothing else', () => {
    expect(Object.keys(I.need).sort()).toEqual(Object.keys(needsKinds.kinds).sort());
    expect(I.need).not.toHaveProperty('secret');
    expect(Object.keys(I.secret).sort()).toEqual([...Object.keys(needsKinds.secretKinds), 'affair-named', 'none'].sort());
  });
  test('brief 017: every catalogue record has a valid label (N-3 and N-4 of the people checker, not a copy)', async () => {
    const w = world(await readTexts('contracts/world/harbour-district.plan.json'), 'civic-block');
    const cast = Cast.parse(json('contracts/people/harbour-district.cast.json'));
    const ctx = w.ctx(cast), gen = expand(ctx);
    const cat = w.needs!.catalogue;
    expect(cat.needs.length + cat.secrets.length).toBeGreaterThan(0);
    for (const r of [...cat.needs, ...cat.secrets]) expect(r.label.trim().length).toBeGreaterThan(0);
    const res = checkPeople(ctx, w.mission, w.plan, { people: gen.people, gaps: [] });
    const nr = res.checks.filter((x) => x.id === 'N-3' || x.id === 'N-4');
    expect(nr.map((x) => x.id)).toEqual(['N-3', 'N-4']);
    expect(nr.filter((x) => !x.ok)).toEqual([]);
  });
});

// ---------------------------------------------------------------- briefs 058, 059, 062 (GDD v0.25.0): cameras, the pickup and rivals, the briefing
describe('briefs 058, 059, 062: cameras, the pickup and rivals, the briefing', () => {
  const L = t.less;
  const words = (x: string) => x.replace(/\{[a-z-]+\}/g, '').replace(/[·▲👁✓✗½[\]]/gu, ' ').split(/\s+/).filter((w) => /\p{L}/u.test(w)).length;
  const GENDER = /\b(he|she|him|her|hers|his|himself|herself|man|men|woman|women|boy|girl|sir|madam|lady|lord)\b/i;
  test('a looped camera is never dark: the Loop, Disable and Shoot chips each have their own words (brief 058)', () => {
    expect(L.done.gain['camera-looped']).toBe('Camera looped'); expect(L.done.gain['camera-cut']).toBe('Camera cut'); expect(L.done.gain['camera-shot']).toBe('Camera shot out');
    expect(L.done.gain['camera-dark']).toBeUndefined();
    expect(L.pop['camera-looped']).toBe('Camera looped'); expect(L.pop['camera-dark']).toBe('Camera shot out');
    for (const [k, x] of Object.entries(L.done.gain)) expect(x, k).not.toMatch(/\bdark\b/i);
  });
  test('Loop v2 has no tether and no walk-off hold: the stopgap block is retired (brief 058, engine 0.31.0)', () => {
    expect((t.less as Record<string, unknown>).hold).toBeUndefined();
    expect(Object.keys(t.less.fates)).not.toContain('less.hold');
    // the tether's words stay only for a rulebook pinned with cell.loop.tether (the engine still lists the codes); the live rulebook has none
    expect(Object.keys((json('contracts/rules/rulebook.json') as { cell: { loop: Record<string, unknown> } }).cell.loop)).not.toContain('tether');
    expect(t.activity.holding.loop).toBe('Holding the loop');
    expect(t.locks['run-tethered']).toBeTruthy(); expect(t.locks['board-tethered']).toBeTruthy();
  });
  test('the loop ending is worded for a run-out and a cut alike: never claims the camera sees again', () => {
    for (const e of t.dialogue.filter((d) => d.event === 'loop-ends')) for (const x of e.lines) expect(x, x).not.toMatch(/live|sees|entrance/i);
    expect(t.cards.items['loop.ends']!.result).not.toMatch(/sees|live/i);
    expect(t.less.done.close['disable']).toBe('Desk reports');
  });
  test('Disable is a whole verb: action, ring word, exchange, activity, price line, refusals, cards, an event and the Tech\'s lines (engine 0.31.0)', () => {
    expect(t.actions.disable!.by).toEqual(['tech']); expect(t.actions['ring-disable']!.label.length).toBeLessThanOrEqual(8);
    expect(t.exchange.disable).toEqual(['ack', 'arrive', 'done']);
    expect(L.price.verbs.disable).toEqual(['name', 'time', 'trail', 'alert']); expect(L.price.examples.disable).toBe('Disable · 10 s · +2 · ▲1 in 14 s'); expect(L.price.examples.loop).toBe('Loop · 20 s · +1 · 2:00'); expect(L.price.verbs.loop).toEqual(['name', 'time', 'trail', 'lasts']);
    expect(t.locks['camera-dead']!.verbs).toEqual(['loop', 'disable']); expect(t.locks['disable-wrong-target']!.verbs).toEqual(['disable']);
    expect(t.locks['loop-holds']!.line).toContain('{tick}'); expect(t.locks['loop-holds']!.short).toContain('{tick}');
    expect(t.cards.items['camera.cut']!.fate).toBe('icon'); expect(t.cards.items['camera.cut-reported']!.title).toBe('The desk sees a dead camera');
    for (const sp of ['tech']) for (const ev of ['ack', 'arrive', 'done']) expect(lookup(t, { event: ev, verb: 'disable', speaker: sp }), `${ev} disable ${sp}`).toBeTruthy();
    for (const sp of t.events['camera-reported']!.speakers) expect(lookup(t, { event: 'camera-reported', speaker: sp }), sp).toBeTruthy();
  });
  test('a report that sends nobody has its own words: the card keys, the beat and the Scout (brief 058, Motive 2026-10-25)', () => {
    const cut = t.cards.items['camera.cut-reported']!, disp = t.cards.items['alarm.dispatches']!;
    expect(Object.keys(cut.result as Record<string, string>).sort()).toEqual(['none', 'sent']); expect(cut.pick).toMatch(/0 robots and 0 units/);
    expect(Object.keys(disp.result as Record<string, string>).sort()).toEqual(['both', 'none', 'robots']);
    expect(Object.keys(disp.beat as Record<string, string>).sort()).toEqual(['both', 'robots']); // the feed holds at most ten beats: a dispatch of 0 shows no beat (the Scout speaks, the card is on Alt)
    expect((disp.result as Record<string, string>).none).not.toContain('{clock}');
    const none = lookup(t, { event: 'camera-reported', speaker: 'scout', outcome: 'nobody-sent' })!, sent = lookup(t, { event: 'camera-reported', speaker: 'scout' })!;
    expect(none.outcome).toBe('nobody-sent'); expect(sent.outcome).toBeUndefined();
    for (const l of none.lines) { expect(l, l).not.toMatch(/coming|robots? (is|are) (coming|on)/i); expect(l.split(/\s+/).length, l).toBeLessThanOrEqual(MAX_WORDS); }
    expect(none.lines.length).toBeGreaterThanOrEqual(2);
  });
  test('a camera shot is priced once, never per hit (brief 058 section 3)', () => {
    expect(L.price.examples['fire-camera']).toBe('Fire · +2 · ▲≥2'); expect(L.price.examples['fire-camera']).not.toContain('/hit');
    for (const c of ['camera-entrance', 'camera-pickup', 'camera-mast-314']) expect(t.actions.fire!.onTarget![c], c).toMatch(/Gunfire raises Alert at once/);
  });
  test('route B: the turned driver brings Vale round the back, and the feed rows have no service branch (brief 059)', () => {
    expect(t.actions.pay!.onTarget!.driver).toContain('round the back'); expect(t.actions.help!.onTarget!.driver).toContain('round the back');
    for (const c of ['vale.exits', 'vale.at-car', 'car.leaves']) { expect(typeof t.cards.items[c]!.result, c).toBe('string'); expect(t.cards.items[c]!.pick, c).toBeUndefined(); }
    expect(JSON.stringify(t)).not.toMatch(/Game\.PickupService/);
    expect(t.events['car-leaves']!.trigger).toMatch(/round the back/);
  });
  test('the Drive (brief 059, engine 0.32.0): the order, its stands named by the site, its refusals, its roster words, its price line and its two hints', () => {
    const a = t.actions['drive-car']!;
    expect(a.engine.do).toBe('drive-car'); expect(a.by.slice().sort()).toEqual([...AGENTS].sort()); expect(a.label).toBe('Drive'); expect(t.actions['ring-drive']!.engine.do).toBe('drive-car');
    expect(t.actions.drive!.label).toBe('Take control'); // the control keeps its name; the order has its own id
    // a stand's text key is `stand-` plus the site's getaway.stands id (the host's TextKey, like camera-<id> and car-<id>)
    const site = json('sim/data/handover.site.json') as { getaway?: { stands?: { id: string }[] } };
    const ids = (site.getaway?.stands ?? []).map((x) => x.id);
    if (ids.length) expect(Object.keys(a.onTarget!).sort()).toEqual(ids.map((i) => `stand-${i}`).sort());
    for (const k of Object.keys(a.onTarget!)) { expect(t.names.targets[k], k).toBeTruthy(); expect(t.targets[k]!.name, k).toBeTruthy(); }
    expect(t.exchange['drive-car']).toEqual(['ack']);
    for (const ag of AGENTS) expect(lookup(t, { event: 'ack', verb: 'drive-car', speaker: ag }), ag).toBeTruthy();
    expect(t.activity.acting['drive-car']).toBe('Driving the car'); expect(t.activity.acting.ride).toBeTruthy(); // the driver's busy order is drive-car, everyone else's is ride
    expect(L.price.verbs['drive-car']).toEqual(['name', 'time']); expect(L.price.examples['drive-car']).toBe('Drive · 28 s'); // $0, 0 Trail, no Alert: nothing but the seconds
    // each refusal says the fix in a few words (Glyph's draft, Fable 2026-10-26)
    const shorts = Object.fromEntries(['drive-held', 'drive-no-driver', 'drive-busy', 'drive-here', 'drive-fight', 'unknown-stand'].map((c) => [c, t.locks[c]!.short]));
    expect(shorts).toEqual({ 'drive-held': 'Blocked · down {holders}', 'drive-no-driver': 'Board first', 'drive-busy': 'Car busy', 'drive-here': 'Already here', 'drive-fight': 'Fight or Go', 'unknown-stand': 'Not a stand' });
    expect(t.locks['stand-shut']).toBeUndefined(); // the early Drive to the yard is not refused (GDD section 07): the mark is a caution, the engine has no such code
    // the beat when his car stops short
    expect(t.cards.items['car.blocked']!.beat).toBe('Vale\'s car stops short.'); expect(wordCount(t.cards.items['car.blocked']!.beat as string)).toBeLessThanOrEqual(BEAT_MAX);
    // H20 is offered ahead of H16 (the Take) while our car is still at the square
    const [h16, h20] = [t.hints.items.H16!, t.hints.items.H20!];
    expect(h20.verb).toBe('drive-car'); expect(h20.priority).toBeLessThan(h16.priority); expect(h20.short).toBe('Drive the [car] round the back.'); expect(h20.rings).toBe('the getaway car');
  });
  test('a Watch on the rival car teaches the crew, and a Listen on the driver the tail fact (brief 059)', () => {
    expect(WATCHED).toContain('rivals'); expect(t.targets.rivals!.watched).toMatch(/Five in one car/); expect(t.actions.watch!.onTarget!.rivals).toBeTruthy();
    for (const sp of ['scout', 'tech'] as const) expect(lookup(t, { event: 'read', verb: 'watch', speaker: sp, about: 'rivals' }), sp).toBeTruthy();
    const tail = t.events['driver-tail']!;
    expect(tail.speakers).toEqual(['driver']); expect(tail.verbs).toEqual(['listen']); expect(tail.log).toBeUndefined(); // no engine log code: the host plays it with the Listen's scene
    const e = t.dialogue.find((x) => x.event === 'driver-tail')!;
    expect(e.speaker).toBe('driver'); expect(e.lines.some((l) => /tailed us all week/.test(l))).toBe(true);
  });
  test('pending: every entry names its brief and says whether its code is a guess; none duplicates a live lock, card, event or action it does not replace', () => {
    const pe = t.pending!;
    expect(pe).toBeTruthy();
    for (const [k, x] of [...Object.entries(pe.locks), ...Object.entries(pe.cards), ...Object.entries(pe.events), ...Object.entries(pe.actions), ...Object.entries(pe.hints), ...Object.entries(pe.targets)] as [string, { brief: string; guess: boolean }][]) {
      expect(x.brief.length, k).toBeGreaterThan(8); expect(typeof x.guess, k).toBe('boolean');
    }
    for (const e of pe.dialogue) expect(e.brief.length).toBeGreaterThan(8);
    for (const c of Object.keys(pe.locks)) expect(t.locks[c], `lock ${c} is already live`).toBeUndefined();
    for (const [c, x] of Object.entries(pe.cards)) if (!x.replaces) expect(t.cards.items[c], `card ${c} is already live`).toBeUndefined(); else expect(t.cards.items[c], `card ${c} has nothing to replace`).toBeTruthy();
    for (const c of Object.keys(pe.events)) expect(t.events[c], `event ${c} is already live`).toBeUndefined();
    for (const c of Object.keys(pe.actions)) expect(t.actions[c], `action ${c} is already live`).toBeUndefined();
    for (const [c, x] of Object.entries(pe.hints)) if (!x.replaces) expect(t.hints.items[c]).toBeUndefined(); else expect(t.hints.items[c]).toBeTruthy();
    // every pending event's speakers have a line, and every pending dialogue entry belongs to a live or pending event
    const events = new Set([...Object.keys(t.events), ...Object.keys(pe.events)]);
    for (const e of pe.dialogue) expect(events.has(e.event), e.event).toBe(true);
    for (const [c, ev] of Object.entries(pe.events)) for (const sp of ev.speakers) expect(pe.dialogue.some((e) => e.event === c && e.speaker === sp), `${c} ${sp}`).toBe(true);
    // the beats stay at most ten once the pending cards land
    const beats = new Set<string>();
    const live = { ...t.cards.items };
    for (const [c, x] of Object.entries(pe.cards)) live[c] = x as never;
    for (const c of Object.values(live)) for (const x of typeof c.beat === 'string' ? [c.beat] : Object.values(c.beat ?? {})) beats.add(x);
    for (const sc of t.scenes.items) if (sc.beat && sc.event !== 'rivals-arrive') beats.add(sc.beat); // the rivals-arrive scene loses its beat when rivals.coming lands
    expect(beats.size).toBeLessThanOrEqual(10);
    // the pending short forms use no gendered word
    for (const [c, x] of Object.entries(pe.locks)) expect(x.short, c).not.toMatch(GENDER);
    for (const [c, x] of Object.entries(pe.cards)) if (typeof x.beat === 'string') expect(x.beat, c).not.toMatch(GENDER);
  });
  test('pending locks follow the pop grammar: a few words, no sentence, the numbers the line carries, shorter than the line', () => {
    for (const [c, l] of Object.entries(t.pending!.locks)) {
      expect(l.short.length, c).toBeLessThanOrEqual(SHORT_MAX); expect(words(l.short), c).toBeLessThanOrEqual(4); expect(l.short, c).not.toMatch(/[.!?]/);
      expect(l.short.length, c).toBeLessThan(l.line.length); expect(/\{(m|usd|tick)\}/.test(l.short), c).toBe(/\{(m|usd|tick)\}/.test(l.line));
    }
  });
  test('the briefing (brief 062): Begin and the file card\'s labels; the cell strip\'s jobs and Day / Night retired (brief 070)', () => {
    const b = L.briefing;
    expect(b as Record<string, unknown>).not.toHaveProperty('job'); // brief 070: the page says one line per face (briefing.page.cell)
    expect([b.begin, b.target]).toEqual(['Begin', 'Target']);
    for (const k of ['fact', 'office']) expect(b as Record<string, unknown>, k).not.toHaveProperty(k); // brief 070: the page reads neither (Latch)
    expect(b as Record<string, unknown>).not.toHaveProperty('day'); expect(b as Record<string, unknown>).not.toHaveProperty('night'); // brief 070 (Sky): night is not a toggle
    expect(Object.keys(b.ways).sort()).toEqual(['front-door', 'pickup']);
    expect(JSON.stringify(b)).not.toMatch(GENDER);
  });
  test('brief.json: the briefing\'s hook and objective are short, plain and agree with the win (brief 062 section 4)', () => {
    const bj = json('contracts/missions/handover/brief.json') as { pick: string; variants: Record<string, { hook: string; dossier: { objective: string } }> };
    const v = bj.variants[bj.pick]!;
    expect(v.hook).toBe('Vale has a file on you.'); expect(v.dossier.objective).toBe('Take him alive before it reaches Security.');
    expect(wordCount(v.hook)).toBeLessThanOrEqual(8); expect(wordCount(v.dossier.objective)).toBeLessThanOrEqual(8);
    for (const x of [v.hook, v.dossier.objective]) { expect(x.length).toBeLessThanOrEqual(MAX_LINE); expect(x).not.toMatch(/\b(hack|network|database|digital|computer|trail|alert)\b/i); }
  });
});

// ---------------------------------------------------------------- brief 067 (batch 0.2.123): engine 0.33.1, the 2:35 beat heard anywhere, the Watch on the rivals, verbs with a target
describe('brief 067: engine 0.33.1, the beat heard anywhere, the Watch on the rivals, verbs with a target', () => {
  const L = t.less;
  const words = (x: string) => x.replace(/\{[a-z-]+\}/g, '').replace(/[·▲👁✓✗½[\]]/gu, ' ').split(/\s+/).filter((w) => /\p{L}/u.test(w)).length;
  const rulebook = json('contracts/rules/rulebook.json') as { cancel: Record<string, unknown>; values: Record<string, { v: number }> };
  test('engine 0.33.1: a Drive under way is no cancel; the refusal says what, why and the fix, and the cost says the car drives on', () => {
    const lk = t.locks['car-driving']!;
    expect(lk.verbs).toEqual(['cancel']);
    expect(lk.short).toBe('Car driving · wait'); expect(words(lk.short)).toBeLessThanOrEqual(4); expect(lk.short.length).toBeLessThanOrEqual(SHORT_MAX);
    expect(lk.line).not.toContain('{'); // the engine sends no number with it
    expect(fillLock(t, { code: 'car-driving' })).toBe("The car is driving and can't be stopped. Wait until it parks.");
    expect(t.cancelCosts['drives-on']).toBe('The car drives on to where it was sent, with everyone aboard.');
    expect(L.cancel['drives-on']).toBe('car drives on'); expect(words(L.cancel['drives-on']!)).toBeLessThanOrEqual(3);
    // it is no cancel at all, so the cancel chip has no tag or pop for it
    expect(Object.keys(L.done.cancel.cost)).not.toContain('drives-on');
    // the driver and the riders keep their roster words while it drives
    expect(t.activity.acting['drive-car']).toBeTruthy(); expect(t.activity.acting.ride).toBeTruthy();
    // the rulebook names the cost for a Drive, and the engine turns that cost into this refusal
    expect(rulebook.cancel.driveCar).toBe('drives-on');
    if (haveSim) expect(simFile('GameCancel.cs')).toMatch(/a\.CancelCost == "drives-on" \? "car-driving"/);
  });
  test('engine 0.33.1: Board is "at the car" within car-reach-cm of its body, so the refusal says "of it" and shows 1 m', () => {
    const reach = rulebook.values['car-reach-cm']!.v;
    expect(reach).toBe(100);
    expect(fillLock(t, { code: 'stand-at-getaway', cm: reach })).toBe('Too far from the car. Walk within 1 m of it, then Board.');
    expect(fillLockShort(t, { code: 'stand-at-getaway', cm: reach })).toBe('Too far · 1 m');
    if (haveSim) expect(simFile('GameGetaway.cs')).toMatch(/No\("stand-at-getaway", "[^"]*", cm: R\.CarReachCm\)/);
  });
  test('the 2:35 beat is heard wherever the cell stands (Sky on 0.2.122): the card is always and still points at the car, the Scout speaks of what he hears, the drop and the park stay in sight', () => {
    const c = t.cards.items['rivals.coming']!;
    expect(c.sight).toBe('always'); expect(c.at).toBe('rival-car'); expect(c.fate).toBe('beat'); expect(c.beat).toBe('Rival car coming.');
    expect(t.cards.items['rivals.drop']!.sight).toBe('in-sight'); expect(t.cards.items['rivals.arrive']!.sight).toBe('in-sight');
    // a spoken line follows its card's sight rule, so with the card shown anywhere the Scout may not claim to see the car
    const e = lookup(t, { event: 'rivals-coming', speaker: 'scout' })!;
    expect(e.lines.length).toBeGreaterThanOrEqual(2);
    for (const l of e.lines) { expect(l, l).toMatch(/Orbit Walk/); expect(l, l).not.toMatch(/\b(see|sees|saw|seen|sight|look|looks|unfamiliar|there)\b/i); }
    expect(t.events['rivals-coming']!.speakers).toEqual(['scout']); expect(t.events['rivals-coming']!.clock).toBe('2:35');
    expect(t.events['rivals-coming']!.note).toMatch(/wherever the cell stands/);
    // the sound is a clock cue heard anywhere (Chime's cues.json), on the same log code
    if (existsSync(join(root, 'contracts/audio/cues.json'))) {
      const cues = json('contracts/audio/cues.json') as { cues: { id: string; on: { log: string }; spatial: { blend: number } }[] };
      const cue = cues.cues.find((x) => x.on.log === 'rivals.coming');
      expect(cue, 'a cue on rivals.coming').toBeTruthy(); expect(cue!.spatial.blend).toBe(0);
    }
  });
  test('a Watch on the rival crew has its done chip: a bare noun on the car, as Rounds is on the patrol', () => {
    expect(L.done.gain.timetable).toBe('Timetable'); expect(L.done.gain.rounds).toBe('Rounds');
    expect(t.actions.watch!.onTargetBy!.tech!.rivals).toMatch(/timetable/); // the Tech's card already calls it that
  });
  test('a verb with a target reads as English with every name its target can have (Suture 2026-10-26: the hold pill said "Listen the guard")', () => {
    // verbs that take their target straight after them; every other verb that takes one carries its preposition in the template, so "{label} {target}" is never the label
    const DIRECT = ['watch', 'ask', 'pay', 'help', 'pressure', 'call', 'loop', 'disable', 'reroute', 'take', 'board', 'carry', 'takedown', 'restrain'];
    const PREP: Record<string, string> = { listen: 'to', enter: 'by', go: 'in', 'drive-car': 'into', fire: 'at', volley: 'at', stash: 'in', move: 'to', run: 'to' };
    const PREPS = ['to', 'at', 'in', 'by', 'into', 'on', 'for', 'with', 'from', 'behind', 'about', 'of'];
    const before = (s: string) => s.slice(0, s.indexOf(' {target}')).split(' ').at(-1)!; // the word just before {target}
    const fill = (verb: string, key: string) => t.actions[verb]!.withTarget!.replace('{target}', t.names.targets[key]!);
    const bad: string[] = [];
    for (const [k, a] of Object.entries(t.actions)) {
      if (!a.withTarget) continue;
      const [w, th] = [a.withTarget, a.then!];
      if (k === 'mislead') { if (w !== 'Give {target} a false tip' || th !== 'gives {target} a false tip') bad.push(`${k}: "${w}" / "${th}"`); continue; } // the one verb that takes its target in the middle
      if (DIRECT.includes(k)) { for (const f of [w, th]) if (PREPS.includes(before(f))) bad.push(`${k}: "${f}" puts a preposition before the target`); }
      else if (k in PREP) { for (const f of [w, th]) if (before(f) !== PREP[k]) bad.push(`${k}: "${f}" must read "… ${PREP[k]} {target}"`); if (w === `${a.label} {target}`) bad.push(`${k}: the label and the name run together`); }
      else bad.push(`${k}: "${w}" is a verb with a target that this test has not classified (direct, or with its preposition)`);
      for (const key of Object.keys(a.onTarget ?? {})) {
        const out = w.replace('{target}', t.names.targets[key]!);
        if (/\s{2}|^\s|\s$|[.!?,:]$/.test(out) || !/^[A-Z]/.test(out)) bad.push(`${k} on ${key}: "${out}"`);
      }
    }
    expect(bad).toEqual([]);
    // a target's name never starts with a preposition or a verb: it follows one
    for (const [k, n] of Object.entries(t.names.targets)) expect(n, k).toMatch(/^(the |a |an |Vale|Inez)/);
    // what the pill and the panel print, word for word
    expect(fill('listen', 'guard')).toBe('Listen to the guard'); expect(fill('listen', 'waitress')).toBe('Listen to Inez'); expect(fill('listen', 'councillor')).toBe('Listen to Vale');
    expect(fill('watch', 'guard')).toBe('Watch the guard'); expect(fill('watch', 'rivals')).toBe('Watch the rival crew');
    expect(fill('pressure', 'waitress')).toBe('Pressure Inez'); expect(fill('pay', 'driver')).toBe('Pay the driver');
    expect(fill('fire', 'hostile')).toBe('Fire at a hostile'); expect(fill('enter', 'door')).toBe('Go in by the public door');
    expect(fill('drive-car', 'stand-square')).toBe("Drive into the square's bay"); expect(fill('board', 'car-getaway')).toBe('Board the getaway car');
    // the pill takes its name from here, not from the verb's label with the target after it
    expect(L.price.fields.name!.shows).toMatch(/withTarget/);
  });
  test('Glyph\'s boards (batch 0.2.123): the shut ring and the locked pill print these twelve shorts as written, and each reads alone', () => {
    const want: Record<string, string> = {
      'summon-alert': 'Alarm up', 'already-heard': 'Already heard', 'already-watched': 'Already watched', 'needs-need': 'Listen first', 'needs-routine': 'Watch first', 'refusal-remembered': 'Refused before',
      'no-cash': 'Need up to {usd}', 'only-enforcer': 'Enforcer only', 'car-held': 'Blocked · down {holders}', 'patrol-away': 'Away · {tick}', 'loop-holds': 'Loop ends {tick}', 'run-tethered': 'Busy looping' };
    for (const [c, short] of Object.entries(want)) {
      expect(t.locks[c]!.short, c).toBe(short);
      const filled = short.replace('{usd}', '$90,000').replace('{tick}', '3:51').replace('{holders}', '2 rivals');
      expect(filled.length, c).toBeLessThanOrEqual(SHORT_MAX); expect(filled, c).not.toMatch(/[.!?]/); expect(filled, c).not.toMatch(/\b(he|she|him|her|his)\b/i);
    }
    // the merged wedge of Ask, Pay, Help and Pressure shares one lock: its one word, on the ring, for a Fixer
    expect(t.actions['ring-turn']!.label).toBe('Turn'); expect(t.actions['ring-turn']!.by).toEqual(['fixer']);
    for (const a of ['ask', 'pay', 'help', 'pressure']) expect(t.actions[`ring-${a}`]!.engine.do).toBe(t.actions['ring-turn']!.engine.do);
  });
  test('Glyph C4: a squad Board that skips an agent says why on the portrait in the same two words as a squad Fire, for every refusal a Board can meet', () => {
    const B = L.done.board;
    // Glyph\'s five, as written
    expect({ down: B['agent-down'], inside: B['enforcer-inside'], running: B['board-running'], carrying: B['already-carrying'], looping: B['board-tethered'] }).toEqual({ down: 'down', inside: 'inside', running: 'stop first', carrying: 'hands full', looping: 'looping' });
    // every code a Board can be refused with: the engine's pre-checks, Board itself and the host's BoardLock; an agent already aboard is skipped silently
    const codes = ['mission-over', 'agent-down', 'enforcer-inside', 'agent-busy', 'board-running', 'board-tethered', 'already-carrying', 'stand-at-getaway', 'engine-error'];
    expect(Object.keys(B).sort()).toEqual([...codes].sort());
    expect('in-car' in B).toBe(false);
    for (const [k, x] of Object.entries(B)) { expect(words(x), k).toBeLessThanOrEqual(2); expect(x, k).not.toMatch(/[.!?,{}]/); expect(x, k).toBe(x.toLowerCase()); }
    // the same cause, the same word, in both tables
    for (const k of Object.keys(B)) if (k in L.done.fire && k !== 'engine-error') expect(B[k], k).toBe(L.done.fire[k]);
    if (haveSim) {
      const model = simFile('Model.cs'), list = model.slice(model.indexOf('public static readonly string[] Codes'), model.indexOf('};', model.indexOf('public static readonly string[] Codes')));
      const engine = new Set([...list.matchAll(/"([a-z-]+)"/g)].map((x) => x[1]!));
      for (const c of codes) expect(engine.has(c), `${c} is an engine code`).toBe(true);
      const board = simFile('GameGetaway.cs'), fn = board.slice(board.indexOf('string Board(Op op, Order o)'), board.indexOf('void LandBoard('));
      for (const c of ['board-running', 'board-tethered', 'already-carrying', 'stand-at-getaway']) expect(fn, c).toContain(`No("${c}"`);
    }
  });
  test('Go keeps one name for one thing now that Drive is an order: the car leaves, it does not "drive off"; Go before Vale is a withdraw and says so', () => {
    const go = t.actions.go!;
    expect(go.does).toMatch(/^Leave with whoever is in the car/); expect(go.withTarget).toBe('Leave in {target}'); expect(go.then).toBe('leaves in {target}');
    expect(L.objective.go).toBe('Leave in the [car].'); expect(marked(L.objective.go)).toEqual(['car']);
    expect(t.hints.items.H18!.short).toBe('Go: leave in the [car].'); expect(marked(t.hints.items.H18!.short)).toEqual(['car']);
    // no label or goal line the player reads starts "Drive off": the Drive order owns the word (story and speech keep their own voice)
    for (const [path, x] of [['actions.go.does', go.does], ['actions.go.withTarget', go.withTarget!], ['less.objective.go', L.objective.go], ['hints.H18.short', t.hints.items.H18!.short], ['hints.H18.line', t.hints.items.H18!.line]] as const) expect(x, path).not.toMatch(/\bdrive off\b/i);
    expect(L.go.noVale).toBe('Calls off the attempt; try again'); expect(words(L.go.noVale)).toBeLessThanOrEqual(6); expect(L.go.noVale).not.toMatch(/[.!?]/); // Glyph budget `altline`: one line, a fragment
    expect(L.go.noVale.length).toBeLessThanOrEqual(MAX_LINE); expect(t.actions.withdraw!.does).toMatch(/Call off the attempt/);
    if (haveSim) expect(simFile('GameGetaway.cs')).toMatch(/Result = Outcome\.Withdrawn; Withdrew\(true\); Chain\.Add\("car-leaves-without-vale"\)/); // the engine: a Go without Vale is the cell withdrawing
  });
  test('the control tips (Glyph D5, F9): X stops fire, the wheel switches; each is shown at once, once, rings nothing, and is the one place its input is taught', () => {
    const [h21, h22] = [t.hints.items.H21!, t.hints.items.H22!];
    expect([h21.short, h22.short]).toEqual(['Cease fire: press X.', 'Scroll to switch.']);
    expect([h21.verb, h22.verb]).toEqual(['cease', 'step-target']); expect(h21.agent).toBe('any'); expect(h22.agent).toBe('any');
    expect(t.actions.cease!.label).toBe('Cease fire'); // the game's own name for the verb the key gives
    expect([h21.priority, h22.priority]).toEqual([-8, -9]);
    for (const h of [h21, h22]) { expect(words(h.short)).toBeLessThanOrEqual(6); expect(marked(h.short)).toEqual([]); expect(h.rings).toMatch(/^nothing/); expect(h.trigger).toMatch(/at once/); expect(h.goneWhen).toMatch(/retired for good/); }
    for (const h of Object.entries(t.hints.items).filter(([id]) => !['H21', 'H22'].includes(id)).map(([, x]) => x)) { expect(h21.priority).toBeLessThan(h.priority); expect(h22.priority).toBeLessThan(h.priority); }
    expect(h21.line).toMatch(/\bX\b/); expect(h22.line).not.toMatch(/\b[A-Z]\b(?=[ ,.;:)]|$)/); // only the X hint names a key
    expect(t.hints.rules.tips).toMatch(/H21/); expect(t.hints.rules.tips).toMatch(/H22/);
    // the key is the game's own: X is Cease in the controls and in HOW-TO-PLAY
    const controls = join(root, 'unity/Assets/Suborned/Runtime/Play/PlayControls.cs');
    if (existsSync(controls)) expect(read('unity/Assets/Suborned/Runtime/Play/PlayControls.cs')).toMatch(/PlayAction\.Cease,\s*keys = new\[\] \{ Key\.X \}/);
  });
});

// ---------------------------------------------------------------- brief 067 (batch 0.2.123, round 2): engine 0.34.x
describe('brief 067, engine 0.34.x: Go needs a driver, the cell down, the rivals walk to a held car, the patrol in the yard, a run is noticed, three words the host made up', () => {
  const L = t.less;
  const wc = (x: string) => x.replace(/\{[a-z-]+\}/g, '').replace(/[·▲👁✓✗½[\]]/gu, ' ').split(/\s+/).filter((w) => /\p{L}/u.test(w)).length;
  test('Go needs a driver: car-no-driver says what, and the fix, in a pop of three words; the engine gives it from Go and from the Go card', () => {
    const lk = t.locks['car-no-driver']!;
    expect(lk.verbs).toEqual(['go']);
    expect(lk.line).toBe("Nobody aboard can drive. Board someone who isn't down, then Go."); // "down" is the glossary's word for a downed agent
    expect(lk.short).toBe('Nobody can drive'); expect(wc(lk.short)).toBeLessThanOrEqual(4);
    expect(lk.line).not.toContain('{'); // the engine sends no number with it
    expect(fillLock(t, { code: 'car-no-driver' })).toBe(lk.line); expect(fillLockShort(t, { code: 'car-no-driver' })).toBe('Nobody can drive');
    if (haveSim) expect((simFile('GameGetaway.cs').match(/No\("car-no-driver"/g) ?? []).length).toBeGreaterThanOrEqual(2); // Go and GoQuery
  });
  test('the whole cell down is a fail with words wherever a fail has them: the sentence, the end card, the fail camera and two voices', () => {
    expect(FAILS).toContain('cell-down');
    expect(t.briefing.fails['cell-down']).toBe('The whole cell is down. Nobody is left to get Vale out.');
    expect(L.end.fail['cell-down']).toBe('Whole cell down.');
    expect(L.failcam.caption['cell-down']).toEqual({ shot: '{by} shot {agent}.' }); // why shot: the shooter first, then the operative, as for enforcer-down
    expect(L.failcam.tip['cell-down']).toEqual({ shot: 'Keep someone outside the car.' });
    for (const sp of ['fixer', 'scout'] as const) expect(lookup(t, { event: 'fail', speaker: sp, outcome: 'cell-down' })?.outcome, sp).toBe('cell-down');
    if (haveSim) expect(simAll().join('\n')).toContain('Fail("cell-down"');
  });
  test('the rivals walk to a held car with nobody outside it, go back to cover when someone is, and a rival with nobody in reach advances: each log code is an event, the rival and the Scout speak, and no line names the Alert', () => {
    for (const [log, id] of [['rivals.for-car', 'rivals-for-car'], ['rivals.back-to-cover', 'rivals-back-to-cover'], ['ai.advance', 'ai-advance']] as const) {
      const ev = t.events[id]!;
      expect(ev.log, id).toEqual([log]); expect(ev.speakers, id).toEqual(['rivals', 'scout']);
      for (const sp of ev.speakers) {
        const e = lookup(t, { event: id, speaker: sp });
        expect(e?.event, `${id}/${sp}`).toBe(id); expect(e!.lines.length, `${id}/${sp}`).toBeGreaterThanOrEqual(2);
        for (const l of e!.lines) expect(l, l).not.toMatch(/alert/i);
      }
      if (haveSim) expect(simAll().join('\n'), log).toContain(`Note("${log}"`);
    }
    // the Scout says what is wrong when the rivals come: nobody is outside the car
    for (const l of lookup(t, { event: 'rivals-for-car', speaker: 'scout' })!.lines) expect(l, l).toMatch(/outside|unguarded|get out/i);
    // the host already makes a fallback card of both codes (HostFeed.FeedEvents), so the text carries their words: what, why and the fix, no sight rule (the cell is in the car), no beat (the feed is at its ten)
    const fc = t.cards.items['rivals.for-car']!, bc = t.cards.items['rivals.back-to-cover']!;
    expect(fc.title).toBe('Rivals going for Vale'); expect(fc.kind).toBe('warning'); expect(fc.fate).toBe('icon');
    expect(fc.result).toBe('Nobody is standing outside the car, so they come for Vale. Walk someone out.'); // "walk them out": locks.in-car's word for leaving the car
    expect(bc.title).toBe('Rivals back in cover'); expect(bc.fate).toBe('alt');
    expect(bc.result).toBe('They fall back to cover: someone is standing outside, or nothing blocks the car.'); // "blocks", never "held" (GDD section 10)
    for (const c of [fc, bc]) { expect(c.sight).toBeUndefined(); expect(c.beat).toBeUndefined(); expect(JSON.stringify(c)).not.toMatch(/\bheld\b/i); }
  });
  test('car.held.<how> is log only: one silent prefix with its reason, spoken by no event and no card', () => {
    expect(t.silentLogs['car.held.']).toMatch(/log only/);
    expect(Object.values(t.events).some((e) => (e.log ?? []).some((c) => c.startsWith('car.held')))).toBe(false);
    expect(Object.keys(t.cards.items).some((c) => c.startsWith('car.held'))).toBe(false);
    if (haveSim) { const g = simFile('GameGetaway.cs'); expect(g).toContain('Note("car.held." + how'); for (const how of ['blocker', 'press', 'advance', 'answer', 'post']) expect(g, how).toContain(`"${how}"`); }
  });
  test('the patrol sweeps the service yard (brief 067 A): no line puts its sweep on the square, the steps or the public door', () => {
    const P = t.targets.patrol!;
    expect(P.about).toMatch(/service yard/); expect(P.about).not.toMatch(/square|door/i); // a told line: no cone, door or reach (GDD section 04)
    expect(P.watched).toMatch(/yard/); expect(P.watched).toMatch(/service door/);
    expect(P.routine).toMatch(/service door/); expect(P.routine).not.toMatch(/between sweeps/);
    expect(t.hints.items.H7!.line).toMatch(/service yard/);
    expect(t.events['patrol-returns']!.trigger).not.toMatch(/entrance/);
    // every line about the patrol (its own text, or in an entry or scene about it) says the yard: none puts it on the front
    const bad = strings.filter((s) => !s.path.startsWith('pending')).filter((s) => /\bpatrol\b/i.test(s.text) || /patrol/.test(s.path))
      .filter((s) => /\b(entrance|square|steps|forecourt)\b|sweeps? (this|past) (the )?door|due round|between sweeps/i.test(s.text)).map((s) => `${s.path}: ${s.text}`);
    expect(bad).toEqual([]);
    // engine 0.34.2 (Sky 2026-10-26, GDD v0.28.0 section 07): the patrol seeing the Enter is no fail. Its fail words are gone, and it is a report: the patrol radios it, it lands, or it is stopped
    for (const k of ['entry-seen-patrol', ...RETIRED_FAILS]) { expect(k in L.failcam.caption, k).toBe(false); expect(k in L.failcam.tip, k).toBe(false); expect(k in L.end.fail, k).toBe(false); expect(k in t.briefing.fails, k).toBe(false); }
    expect(t.dialogue.filter((e) => e.event === 'fail' && e.outcome === 'entry-seen-patrol')).toEqual([]);
    const C = t.cards.items;
    expect(C['entry.seen']).toMatchObject({ fate: 'icon', sight: 'in-sight', at: 'patrol', result: 'The patrol is radioing it in.' });
    expect(C['entry.reported']).toMatchObject({ fate: 'beat', beat: C['report.landed']!.beat, result: C['report.landed']!.result }); // the city's answer: no new beat, never who or why
    expect(C['entry.stopped']).toMatchObject({ fate: 'alt', result: String(C['report.stopped']!.result).replace('{who}', 'The patrol') });
    for (const c of ['entry.seen', 'entry.reported', 'entry.stopped']) expect(t.silentLogs[c], c).toMatch(/card only/);
  });
  test('a run is noticed, never an Alert on its own (Motive M067-B2): no line says running raises the Alert; the card, the toast and the hint say who noticed and what to do', () => {
    const hits = strings.filter((s) => !s.path.startsWith('pending')).filter((s) => /\b(runs?|running|runner)\b/i.test(s.text) && /\balert\b/i.test(s.text)).map((s) => s.path);
    expect(hits).toEqual(['actions.run.does', 'hints.H12']); // the Run verb and the hint that teaches it: both say a run raises no Alert
    for (const x of [t.actions.run!.does, t.hints.items.H12!.line]) expect(x).toMatch(/raises no Alert/);
    expect(t.cards.items['run.seen']!.result).toBe('{who} noticed {agent} running.');
    expect(t.events['run-seen']!.trigger).not.toMatch(/Alert 1|a found fault \(/);
    expect(t.toasts['run-seen']!.line).toBe('A watcher noticed an agent running. Walk, or keep out of its sight, to stay quiet.');
    // H19 teaches the notice cue once, and a runner meets it too: Walk, where "Keep moving" would tell a runner to run on
    const h = t.hints.items.H19!;
    expect(h.line).toMatch(/standing still or running/); expect(h.short).toBe('Walk, or leave its sight.');
    expect(JSON.stringify(h)).not.toMatch(/Keep moving/);
    // the patrol's phrase is "noted": a first sighting is a note, and only a long run in sight is reported
    expect(t.tone.voices.patrol).toMatch(/Running is noted/);
    expect(t.dialogue.filter((e) => e.event === 'run-seen').flatMap((e) => e.lines).join(' ')).not.toMatch(/reported/);
    expect(t.silentLogs['attention.noticed']).toMatch(/runner/);
    // the price line of a run keeps name, time and eye: no Alert field
    expect(L.price.verbs.run).toEqual(['name', 'time', 'eye']); expect(L.price.verbs.move).toEqual(['name', 'time', 'eye']);
  });
  test('the Alt chip of a run says who would notice it, in the words of the rule (a run is noticed, never an Alert), and the host reads that key', () => {
    expect(L.done.run).toEqual({ noticed: 'noticed by {who}', unknown: 'noticed by: ?' });
    expect(JSON.stringify(L.done.run)).not.toMatch(/seen by|alert/i);
    expect(L.done.run.noticed).toContain('{who}'); expect(L.done.run.unknown).not.toContain('{'); // {who} is the watchers as the host names them
    const fight = 'unity/Assets/Suborned/Runtime/Play/HostFight.cs';
    if (existsSync(join(root, fight))) expect(read(fight)).toContain('DoneWord("run.noticed"');
  });
  test('three words the host made up are the text\'s now: a crowd guard, a crowd officer and a fire order that lapsed', () => {
    expect(t.names.targets['crowd-guard']).toBe('a guard'); expect(t.names.targets['crowd-officer']).toBe('an officer');
    expect(L.done.walk.who['crowd-guard']).toBe('guard'); expect(L.done.walk.who['crowd-officer']).toBe('officer');
    expect(L.pop['fire-lapses']).toBe('Lost the target'); expect(wc(L.pop['fire-lapses']!)).toBeLessThanOrEqual(3);
    expect(L.pop['door-seen']).toBe('Seen there'); // HostValeSteps.PopKey("door-seen") had no word, so the pop would have printed its key
    // the host reads these keys (Latch): TargetName("crowd-guard") and ("crowd-officer"), and Pops["fire-lapses"] when a lasting fire order lapses
    const host = 'unity/Assets/Suborned/Runtime/Play/HandoverHost.cs', done = 'unity/Assets/Suborned/Runtime/Play/HostDone.cs';
    if (existsSync(join(root, host))) { const h = read(host); expect(h).toContain('"crowd-guard"'); expect(h).toContain('"crowd-officer"'); }
    if (existsSync(join(root, done))) expect(read(done)).toContain('"fire-lapses"');
    const steps = 'unity/Assets/Suborned/Runtime/Play/HostValeSteps.cs';
    if (existsSync(join(root, steps))) expect(read(steps)).toContain('PopKey("door-seen"');
    if (haveSim) expect(simAll().join('\n')).toContain('Note("fire.lapses"');
  });
});
