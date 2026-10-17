// Done-check for the Handover's words (Fable, brief 011): contracts/missions/handover/text.json parses as suborned.text/1, every engine
// verb, refusal and fail has text, every reply the rulebook can produce has a line for each role and temperament, every line fits (≤ 80
// characters, spoken lines 1–12 words), nobody uncast is named, the tone filters pass, and the pick is deterministic.
// The engine is read from source (sim/Suborned.Sim/*.cs) so a new refusal, fail or log code with no text fails here, not in play.
// Run: bun test contracts/missions/handover
import { describe, expect, test } from 'bun:test';
import { existsSync, readdirSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { Cast } from '../../people/cast.schema.ts';
import { checkPeople } from '../../../tools/people/check.ts';
import { expand } from '../../../tools/people/expand.ts';
import { readTexts, world } from '../../../tools/people/inputs.ts';
import { PeopleRules, TEMPERAMENTS, allowedTemperaments } from '../../people/people.rules.schema.ts';
import { Rulebook } from '../../rules/rulebook.schema.ts';
import * as R from '../../rules/rulebook.ref.ts';
import { AGENTS, BEAT_MAX, CARD_FATES, FAILS, FATES, HINT_MAX, MAX_LINE, MAX_WORDS, OUTCOMES, PEOPLE, PRICE_FIELDS, SHORT_MAX, TextFile, VERBS, WATCHED, cardResult, fillCard, fillLock, lineKey, lookup, lookupScene, marked, pickIndex, pickLine, playScene, sceneKey, scenesFor, wordCount } from './text.schema.ts';

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
  for (const [k, x] of Object.entries(t.cancelCosts)) push(`cancelCosts.${k}`, x);
  const walk = (path: string, o: unknown) => { if (typeof o === 'string') push(path, o); else for (const [k, v] of Object.entries(o as object)) walk(`${path}.${k}`, v); };
  walk('activity', t.activity);
  for (const [code, l] of Object.entries(t.locks)) { push(`locks.${code}`, l.line); push(`locks.${code}.short`, l.short, false, SHORT_MAX); }
  // brief 014: the short forms are strings the player reads, so the same length, tone, name and key rules apply (brackets mark a ringed noun and are not text)
  const plain = (x: string) => x.replace(/[[\]]/g, '');
  b.timeline.forEach((x, i) => push(`briefing.timeline[${i}].short`, x.short, false, 16));
  for (const [id, h] of Object.entries(t.hints.items)) push(`hints.${id}.short`, plain(h.short), false, 36);
  for (const [k, c] of Object.entries(t.cards.items)) for (const [v, r] of typeof c.beat === 'string' ? [['', c.beat] as const] : Object.entries(c.beat ?? {})) push(`cards.${k}.beat.${v}`, r, false, BEAT_MAX);
  t.scenes.items.forEach((sc, i) => { if (sc.beat) push(`scenes[${i}].beat ${sceneKey(sc)}`, sc.beat, false, BEAT_MAX); });
  { const L = t.less;
    push('less.objective.take', plain(L.objective.take), false, 32); push('less.objective.bring', plain(L.objective.bring), false, 32); push('less.objective.carry', plain(L.objective.carry), false, 32); push('less.briefing.goal', plain(L.briefing.goal), false, 32);
    push('less.end.win', L.end.win, false, SHORT_MAX); push('less.end.withdraw', L.end.withdraw, false, SHORT_MAX);
    for (const [k, x] of Object.entries(L.end.fail)) push(`less.end.fail.${k}`, x, false, SHORT_MAX);
    for (const [k, x] of Object.entries(L.pop)) push(`less.pop.${k}`, x, false, SHORT_MAX);
    for (const [k, x] of Object.entries(L.cancel)) push(`less.cancel.${k}`, x, false, 20);
    for (const [k, x] of Object.entries(L.alt.outcome)) push(`less.alt.outcome.${k}`, x);
    for (const [g, m] of Object.entries({ need: L.intel.need, temperament: L.intel.temperament, secret: L.intel.secret, verb: L.intel.verb })) for (const [k, x] of Object.entries(m)) push(`less.intel.${g}.${k}`, x.replace('{name}', 'Inez'), false, 20); }
  t.dialogue.forEach((e, i) => e.lines.forEach((s, j) => push(`dialogue[${i}].lines[${j}] ${lineKey(e)}`, s, true)));
}
/** A line with the host's placeholders filled at their widest. */
const widest = (s: string) => s.replace(/\{who\}/g, 'the councillor').replace(/\{op\}/g, 'Enforcer').replace(/\{m\}/g, '13').replace(/\{usd\}/g, '$100,000').replace(/\{target\}/g, 'the entrance camera').replace(/\{tick\}/g, '10:30').replace(/\{need\}/g, 'protection').replace(/\{temperament\}/g, 'Mercenary').replace(/\{alert\}/g, '3').replace(/\{agent\}/g, 'the Enforcer').replace(/\{n\}/g, '12').replace(/\{clock\}/g, '10:30').replace(/\{trail\}/g, '30').replace(/\{hits\}/g, '12').replace(/\{cause\}/g, "the guard's trip to the café looks staged").replace(/\{list\}/g, 'Inez, who can introduce you at the café.')
  // brief 013's between-missions fields: the widest the host fills each (the same widths as contracts/dialogue debriefFill, with a short place name)
  .replace(/\{fact\}/g, "the Fixer's face").replace(/\{facts\}/g, "place, time and the Enforcer's face").replace(/\{place\}/g, 'the service lane').replace(/\{of\}/g, 'Dimitra Marchetti').replace(/\{move\}/g, 'bravery +1')
  .replace(/\{fear\}/g, 'fear +1').replace(/\{guards\}/g, '2').replace(/\{cameras\}/g, '2').replace(/\{missions\}/g, '3').replace(/\{kind\}/g, 'found killing').replace(/\{closes\}/g, 'when the body is found')
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
  test('placeholders are only {who} {op} {m} {usd} {tick} {target} {need} {temperament} {alert} and the cards\' {agent} {n} {clock} {trail} {hits} {cause} {list}, and the between-missions lines\' {fact} {facts} {place} {of} {move} {fear} {guards} {cameras} {missions} {kind} {closes}, and the Standing line\'s {district} {band} {from} {to}', () => {
    const bad = strings.filter((s) => /\{(?!who\}|op\}|m\}|usd\}|tick\}|target\}|need\}|temperament\}|alert\}|agent\}|n\}|clock\}|trail\}|hits\}|cause\}|list\}|fact\}|facts\}|place\}|of\}|move\}|fear\}|guards\}|cameras\}|missions\}|kind\}|closes\}|district\}|band\}|from\}|to\})[^}]*\}/.test(s.text)).map((s) => `${s.path}: ${s.text}`);
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
      'Return', 'Ctrl', 'I', 'F', 'G', 'N', 'Q', 'E', 'Trail', 'Alert', 'Ask', 'Pay', 'Help', 'Pressure', 'Call', 'Watch', 'Listen', 'Loop', 'Reroute', 'Enter', 'Take', 'Fire', 'Carry', 'Halcyon']);
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
    expect(t.briefing.card[0]).toMatch(/^Councillor Vale signed the clearance order/);
    expect(t.briefing.card.join(' ')).toMatch(/alive/);
  });
  test('the hints name the two known approaches', () => {
    expect(t.briefing.hints['front-door']).toMatch(/Inez/);
    expect(t.briefing.hints['front-door']).toMatch(/guard/); // she is close to the door guard (the Story spine)
    expect(t.briefing.hints.pickup).toMatch(/driver/);
  });
});

describe('verbs and refusals', () => {
  test('every panel verb has an action, a label, an agent and an engine order', () => {
    expect(Object.keys(t.actions).sort()).toEqual([...VERBS].sort());
    for (const a of Object.values(t.actions)) { expect(a.label.length).toBeGreaterThan(0); expect(a.by.length).toBeGreaterThan(0); }
  });
  test('every rulebook verb is an action: scout events, turn approaches, the cell\'s verbs', () => {
    const ids = new Set<string>();
    for (const e of rb.events) {
      if (e.id.startsWith('scout.')) ids.add(e.id.slice(6));
      if (e.id.startsWith('turn.')) ids.add(e.id.slice(5));
      if (e.id === 'contact.job') ids.add('call');
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
    const approaches = Object.values(t.actions).filter((a) => a.engine.do === 'turn').map((a) => a.engine.approach).sort();
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
    expect(Object.keys(t.locks).sort()).toEqual([...codes].sort());
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
      const uses = new Set([...l.line.matchAll(/\{(m|usd|tick)\}/g)].map((x) => x[1]!));
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
    expect(fillLock(t, { code: 'summon-too-far', tick: 950 }, { clock })).toBe('Too far: his walk to her would take 1:35. He will not come.');
    expect(fillLock(t, { code: 'nope' })).toBeUndefined();
  });
  const dotnet = Bun.which('dotnet');
  test.skipIf(!dotnet || !haveSim)('text.json matches `suborned-sim codes` (the built engine)', () => {
    const env = { ...process.env, DOTNET_CLI_TELEMETRY_OPTOUT: '1', DOTNET_NOLOGO: '1', ...(process.env.DOTNET_ROOT || !existsSync('/opt/homebrew/opt/dotnet/libexec') ? {} : { DOTNET_ROOT: '/opt/homebrew/opt/dotnet/libexec' }) };
    const r = Bun.spawnSync(['dotnet', 'run', '--project', 'sim/cli', '--', 'codes'], { cwd: root, env, stdout: 'pipe', stderr: 'pipe' });
    expect(r.exitCode, new TextDecoder().decode(r.stderr)).toBe(0);
    const codes = JSON.parse(new TextDecoder().decode(r.stdout)) as string[];
    expect(Object.keys(t.locks).sort()).toEqual([...codes].sort());
  }, 120000);
  test('every fail code the engine can return has plain words and a spoken line', () => {
    if (!haveSim) return;
    const codes = new Set<string>(['engine-error']);
    for (const src of simAll()) for (const m of src.matchAll(/Fail\("([a-z-]+)"/g)) codes.add(m[1]!);
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
    // Note("call." + p.Role, …) builds the lovers' call code: expand it to the roles whose call can succeed (the driver's own call logs "call.driver" literally)
    expect(simAll().join('\n')).toContain('Note("call." + p.Role');
    if (codes.delete('call.')) for (const r of ['waitress', 'driver']) codes.add('call.' + r);
    for (const c of ['scout.watch.watched', 'scout.listen.heard', 'scout.down', 'fixer.down', 'tech.down', 'enforcer.down']) codes.add(c);
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
  test('H1–H17 are all here, H1–H12 in priority order, H13 (turn the view) ahead of them at 0, H17 (the fight hint) ahead of H13 and H14–H16 (the end steps) ahead of H17; H1–H11 carry Motive\'s trigger, ring and gone-when word for word (H12 is brief 014\'s, H13 brief 016\'s, H14–H17 Aster\'s end steps and fight hint, not in Motive\'s table)', () => {
    expect(items.map(([id]) => id)).toEqual(Array.from({ length: 17 }, (_, i) => `H${i + 1}`));
    expect(items.slice(0, 12).map(([, h]) => h.priority)).toEqual(Array.from({ length: 12 }, (_, i) => i + 1));
    expect(t.hints.items.H13!.priority).toBe(0);
    const end = ['H14', 'H15', 'H16'].map((id) => t.hints.items[id]!.priority);
    for (const p of end) expect(p).toBeLessThan(t.hints.items.H17!.priority); // they win over the fight hint and every flavour hint, the view turn included, when their trigger holds
    expect(t.hints.items.H17!.priority).toBeLessThan(t.hints.items.H13!.priority); // a shot to take beats a view to turn
    expect(new Set([...end, t.hints.items.H17!.priority, t.hints.items.H13!.priority]).size).toBe(5); // no tie: the host's sort is not stable
    if (!rows.size) return;
    expect([...rows.keys()]).toEqual(items.filter(([id]) => motiveIds(id)).map(([id]) => id));
    for (const [id, h] of items.filter(([id]) => motiveIds(id))) expect({ trigger: h.trigger, rings: h.rings, goneWhen: h.goneWhen }, id).toEqual(rows.get(id)!);
  });
  test('each hint fits 110 characters, names an agent, a verb and a target, and introduces only its own verb', () => {
    const word: Record<string, string> = { turn: 'turn', mislead: 'false tip', move: 'run', 'turn-view': 'turn', extract: 'into the car' };
    const labels = ['Watch', 'Listen', 'Loop', 'Call', 'Reroute', 'Fire', 'Carry', 'Enter', 'Take'];
    const bad: string[] = [];
    for (const [id, h] of items) {
      const l = h.line;
      if (l.length > HINT_MAX) bad.push(`${id}: ${l.length} characters`);
      const named = h.agent === 'any' ? /\bagent\b/i.test(l) : new RegExp(`\\b${h.agent}\\b`, 'i').test(l);
      if (!named) bad.push(`${id}: names no agent`);
      if (!l.toLowerCase().includes(word[h.verb] ?? h.verb)) bad.push(`${id}: names no verb (${h.verb})`);
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
    expect(t.toasts['run-seen']!.line).toContain('Alert 1');
    expect(t.goingLoud.captured.toLowerCase()).toBe('anyone downed is captured unless carried.');
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
  test('H11 says how to carry: right click the downed agent with another selected', () => {
    const l = t.hints.items.H11!.line;
    expect(l).toMatch(/right click/i);
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
  test('the call: Inez calls the guard over and he leaves his post; the driver blinds the camera; each in all four voices', () => {
    for (const role of ['waitress', 'driver']) for (const temp of TEMPERAMENTS) {
      const sc = lookupScene(t, { kind: 'call', verb: 'call', about: role, temperament: temp });
      expect(sc?.temperament, `${role} ${temp}`).toBe(temp);
    }
    for (const sc of items.filter((x) => x.kind === 'call' && x.about === 'waitress')) {
      expect(sc.result).toMatch(/guard/);
      for (const v of sc.variants) { const who = new Set(v.beats.map((b) => b.speaker)); expect(who.has('waitress') && who.has('guard')).toBe(true); }
    }
    for (const sc of items.filter((x) => x.kind === 'call' && x.about === 'driver')) expect(sc.result).toMatch(/camera/);
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
    expect(verbs.filter((v) => v !== 'turn' && !have.has(v))).toEqual([]);
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
    for (const role of ['waitress', 'driver']) for (const temp of TEMPERAMENTS) if (lookup(t, { event: 'confirm', verb: 'call', speaker: role, temperament: temp })?.temperament !== temp) bad.push(`confirm ${role} ${temp}`);
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
    expect(codes.filter((c) => !(c in items))).toEqual([]);
  });
  test('titles name the event as the player would and are not ids; results carry the engine\'s numbers, never its sentence', () => {
    for (const [c, x] of Object.entries(items)) {
      expect(x.title, c).not.toMatch(/[a-z]+-[a-z]+|\./);
      for (const r of typeof x.result === 'string' ? [x.result] : Object.values(x.result ?? {})) { expect(r, c).not.toMatch(/found fault|\btick\b|operative|engine/i); expect(r).toMatch(/^[A-Z{]/); }
    }
    for (const c of ['run.seen', 'report']) { expect(items[c]!.result as string).toContain('{who}'); expect(items[c]!.result as string).toContain('{alert}'); }
    expect(items['fight.loud']!.result as string).toMatch(/\{n\}.*\{clock\}/);
    expect(items['fight.safe']!.result as string).toContain('{n}');
    expect(items.captured!.result as string).toContain('{agent}');
  });
  test('cards a person would only know in sight say so, as their scenes do', () => {
    expect(items['rivals.arrive']!.sight).toBe('in-sight'); expect(items['guard.at-cafe']!.sight).toBe('in-sight');
    // the two cards of the city scenes (their place is the scene's); and brief 013's cards, which name where the check looks (`at`). Nothing else is in sight only.
    const SCENE_CARDS = ['rivals.arrive', 'guard.at-cafe'];
    const SEEN_AT: Record<string, string> = { 'restrain.freed': 'victim', 'victim.wakes': 'victim', 'victim.statement': 'victim', 'victim.calls-out': 'stash', 'report.started': 'reporter', 'report.stopped': 'reporter',
      'report.unfounded': 'scene', 'report.confirmed': 'scene', 'person.missed': 'person', 'guard.back-to-post': 'guard', 'alarm.reassigns': 'unit', 'doors.lock': 'door', 'patrol.alarm': 'patrol',
      'patrol.stands-down': 'patrol', 'stash.serviced': 'stash', 'stash.opened': 'stash' };
    for (const [c, x] of Object.entries(items)) {
      if (x.sight) expect([...SCENE_CARDS, ...Object.keys(SEEN_AT)], c).toContain(c);
      expect(x.at !== undefined, `${c}: \`at\` goes with \`sight\``).toBe(x.sight !== undefined && !SCENE_CARDS.includes(c));
    }
    for (const [c, at] of Object.entries(SEEN_AT)) { expect(items[c]?.sight, c).toBe('in-sight'); expect(items[c]?.at as string | undefined, c).toBe(at); }
  });
  test('brief 013: the cell\'s own acts and the city\'s answers to an Alert have no sight rule; what happens at a place does (the told/learned rule)', () => {
    // the cell\'s own acts: the agent is there. The city\'s answers: the Alert pill and the street show them, so a card says that it came, never who made the report or why.
    for (const c of ['takedown.starts', 'takedown.fails', 'takedown.dead', 'takedown.out', 'takedown.seen', 'carry.body', 'carry.down', 'stash.', 'restrain.done', 'report.landed', 'seal.starts', 'alarm.dispatches', 'response.reinforces']) expect(items[c]?.sight, c).toBeUndefined();
    // a cause only a Watch can teach: the card waits for the routine
    expect(items['post.missed']!.needs).toBe('routine');
    expect(Object.entries(items).filter(([, x]) => x.needs).map(([c]) => c)).toEqual(['post.missed']);
    // the city\'s answers never name the reporter or the cause
    for (const c of ['report.landed', 'alarm.dispatches', 'response.reinforces', 'seal.starts']) for (const r of typeof items[c]!.result === 'string' ? [items[c]!.result as string] : Object.values(items[c]!.result as Record<string, string>)) expect(r, c).not.toMatch(/\{who\}|reported|saw|because/i);
    // every code of a stash is one card keyed by the prefix the engine builds
    expect(Object.keys(items['stash.']!.result as object).sort()).toEqual(['container', 'door']);
  });
  test('cardResult and fillCard fill the placeholders and capitalise the start', () => {
    expect(fillCard(cardResult(t, 'run.seen')!, { who: 'the entrance camera', agent: 'the Scout', alert: 1 })).toBe('The entrance camera saw the Scout run. Alert 1.');
    expect(fillCard(cardResult(t, 'fight.loud')!, { n: 5, clock: '7:10' })).toBe('5 officers, from the north street. The district seals at 7:10.');
    expect(cardResult(t, 'vale.exits', 'service')).toBe('Walking to the service lane.'); expect(cardResult(t, 'vale.exits')).toBeUndefined();
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
    const mine = ['volunteer', 'statement', 'missing', 'grief', 'worry', 'area-watch', 'case'];
    expect(Object.keys(d.between)).toEqual(mine);
    for (const id of mine) {
      expect(lines[id], `${id} is a rulebook debrief line`).toBeTruthy();
      const slots = [...d.between[id]!.matchAll(/\{([a-z]+)\}/g)].map((m) => m[1]!);
      expect([...new Set(slots)].sort(), id).toEqual([...lines[id]!.fields].sort());
    }
    expect(fillCard(d.between['missing']!, { who: 'Sunita Ferrer', place: 'the service lane', trail: 6 })).toBe('Sunita Ferrer is missing near the service lane. Case open, Trail 6.');
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
  test('H13 teaches the view turn: Q / E in the short form (the host swaps them for keycaps), the game\'s own keys, retired for good by turning', () => {
    const h = t.hints.items.H13!;
    expect(h.verb).toBe('turn-view');
    expect(h.agent).toBe('any');
    expect(h.priority).toBe(0);
    expect(h.short).toBe('Turn the view: Q / E.');
    expect(marked(h.short)).toEqual([]); // nothing in the street is ringed; the host draws the glyph, the words, then Q and E as keycaps
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
    const mine = strings.filter((s) => /^(locks\.[a-z-]+\.short|hints\.H\d+\.short|briefing\.timeline\[\d\]\.short|cards\..*\.beat|scenes\[\d+\]\.beat|less\.)/.test(s.path));
    expect(mine.length).toBeGreaterThan(150);
    expect(JSON.stringify(t.less).match(GENDER)).toBeNull(); // the rules, fates and grammar too
    expect(mine.filter((s) => GENDER.test(s.text)).map((s) => `${s.path}: ${s.text}`)).toEqual([]);
  });
  test('the Next Step is one imperative with one ringed noun, at most six words, no agent, no approach, no time and no sequence', () => {
    const lead: Record<string, RegExp> = { enter: /^Go in\b/, extract: /^Put\b/, take: /^Take\b/, move: /^Run\b/, watch: /^Watch\b/, listen: /^Listen\b/, turn: /^(Turn|Call)\b/, loop: /^Loop\b/, call: /^Call\b/, reroute: /^Reroute\b/, mislead: /^Give\b.*false tip/, fire: /^(Cover|Fire at)\b/, carry: /^Carry\b/, 'turn-view': /^Turn the view\b/ };
    const bad: string[] = [];
    for (const [id, h] of Object.entries(t.hints.items)) {
      const n = marked(h.short);
      if (n.length !== (id === 'H13' ? 0 : 1)) bad.push(`${id}: ${n.length} ringed nouns`); // H13 rings nothing: the view is no thing in the street
      if (words(h.short) > 6) bad.push(`${id}: more than six words`);
      if (!(lead[h.verb] ?? /^$/).test(h.short)) bad.push(`${id}: "${h.short}" does not lead with its verb (${h.verb})`);
      if (/\b(Scout|Fixer|Tech|Enforcer|Ask|Pay|Help|Pressure|first|then|before|after|next|until|once)\b|\d/.test(h.short)) bad.push(`${id}: names an agent, an approach, a time or a sequence`);
      if (h.short.length >= h.line.length) bad.push(`${id}: not shorter than its line`);
      expect(nums(h.short), id).toEqual([]);
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
    expect(L.objective.steps.map((x) => x.key)).toEqual(['take', 'bring', 'carry']);
    for (const x of L.objective.steps) { expect(L.objective[x.key], x.key).toBeTruthy(); expect(x.when.length, x.key).toBeGreaterThan(20); }
    expect(L.objective.steps[1]!.when).toMatch(/Enter/);
    expect(L.objective.steps[2]!.when).toMatch(/Vale is with the Enforcer/);
  });
  test('H14–H16 teach the end steps: Enter at the open door, Extract at the car, Take at the pickup; each for the Enforcer, each short form leads with its verb', () => {
    const [a, b, c] = [t.hints.items.H14!, t.hints.items.H15!, t.hints.items.H16!];
    expect([a.verb, b.verb, c.verb]).toEqual(['enter', 'extract', 'take']);
    for (const h of [a, b, c]) { expect(h.agent).toBe('enforcer'); expect(h.line).toMatch(/\bEnforcer\b/); expect(h.priority).toBeLessThan(0); expect(marked(h.short).length).toBe(1); expect(words(h.short)).toBeLessThanOrEqual(6); }
    expect(a.short).toBe('Go in by the [door].');
    expect(b.short).toBe('Put Vale in the [car].');
    expect(c.short).toBe('Take [Vale] at the pickup.');
    // H14's Alt line explains the off-screen time and that being seen on the way in fails
    expect(a.line).toMatch(/off screen/);
    expect(a.line).toMatch(/[Ss]een on the way in, it fails/);
    // the triggers say what Latch builds
    expect(a.trigger).toMatch(/guard/); expect(a.trigger).toMatch(/camera/); expect(a.trigger).toMatch(/Vale is in the Office/);
    expect(b.trigger).toMatch(/come out with Vale/);
    expect(c.trigger).toMatch(/driver is turned/); expect(c.trigger).toMatch(/pickup/);
    // the rings are things the street has markers for
    expect(a.rings).toBe('the public door'); expect(b.rings).toBe('the getaway car');
    // a hint's verb is one the action panel gives, and the enforcer gives it
    for (const h of [a, b, c]) expect(t.actions[h.verb as keyof typeof t.actions]!.by).toContain('enforcer');
  });
  test('H17 teaches the fire order in a fight: right click a hostile, for any armed agent, behind H15 so the end step wins while a fight holds the car', () => {
    const h = t.hints.items.H17!;
    expect(h.verb).toBe('fire');
    expect(h.agent).toBe('any');
    expect(h.short).toBe('Fire at [{who}]: right click.');
    expect(marked(h.short)).toEqual(['{who}']);
    expect(words(h.short)).toBeLessThanOrEqual(6);
    expect(h.line).toMatch(/right click/);
    expect(h.line).toMatch(/\bagent/);
    expect(h.priority).toBeGreaterThan(t.hints.items.H15!.priority);
    expect(h.priority).toBeLessThan(0);
    expect(h.trigger).toMatch(/other than the Enforcer/);
    expect(h.goneWhen).toMatch(/fires/); expect(h.goneWhen).toMatch(/fight ends/);
    expect(h.rings).toBe('the hostile');
    expect(t.actions.fire!.by.length).toBeGreaterThan(1); // more than the Enforcer can fire
  });
  test('the car marker chip reads less.pop.car-held (a pop of two words); the Enforcer\'s Extract row reads the car-held refusal\'s short', () => {
    expect(L.pop['car-held']).toBe('Car held');
    expect(t.locks['car-held']!.short).toBe('Fight on');
    expect(t.locks['car-held']!.verbs).toEqual(['extract']);
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
    const kind = (f: string) => /^\$/.test(f) ? 'usd' : /^\d+ s$/.test(f) ? 'time' : /^\+\d+(\/hit)?$/.test(f) ? 'trail' : /^▲/.test(f) ? 'alert' : /^👁/u.test(f) ? 'eye' : /^[✓✗½?]$/u.test(f) ? 'mark' : 'unknown';
    expect(Object.keys(L.price.examples).sort()).toEqual(['ask', 'fire', 'help', 'kill', 'knockout', 'pay', 'pressure']);
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
  test('the timeline marks have a word for Alt', () => {
    expect(t.briefing.timeline.map((x) => x.short)).toEqual(['Rivals', 'Chime', 'Last turn', 'Vale out', 'Vale at car', 'Close']);
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
    expect(Object.keys(t.cards.items).length).toBe(52);
    expect(t.briefing.card.length).toBeGreaterThanOrEqual(3);
    expect(t.toasts['run-seen']!.line).toMatch(/Alert 1/);
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
