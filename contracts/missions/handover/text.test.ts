// Done-check for the Handover's words (Fable, brief 011): contracts/missions/handover/text.json parses as suborned.text/1, every engine
// verb, refusal and fail has text, every reply the rulebook can produce has a line for each role and temperament, every line fits (≤ 80
// characters, spoken lines 1–12 words), nobody uncast is named, the tone filters pass, and the pick is deterministic.
// The engine is read from source (sim/Suborned.Sim/*.cs) so a new refusal, fail or log code with no text fails here, not in play.
// Run: bun test contracts/missions/handover
import { describe, expect, test } from 'bun:test';
import { existsSync, readdirSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { PeopleRules, TEMPERAMENTS, allowedTemperaments } from '../../people/people.rules.schema.ts';
import { Rulebook } from '../../rules/rulebook.schema.ts';
import * as R from '../../rules/rulebook.ref.ts';
import { AGENTS, FAILS, MAX_LINE, MAX_WORDS, OUTCOMES, PEOPLE, TextFile, VERBS, lineKey, lockOf, lookup, pickIndex, pickLine, wordCount } from './text.schema.ts';

const root = join(import.meta.dir, '..', '..', '..');
const read = (p: string) => readFileSync(join(root, p), 'utf8');
const json = (p: string) => JSON.parse(read(p));
const raw = json('contracts/missions/handover/text.json');
const t = TextFile.parse(raw);
const rb = Rulebook.parse(json('contracts/rules/rulebook.json'));
const people = PeopleRules.parse(json('contracts/people/people.rules.json'));
const mission = json('contracts/missions/handover/mission.json') as { timeline: { at: string; what: string }[] };
const SIM = join(root, 'sim', 'Suborned.Sim');
const haveSim = existsSync(SIM);
const simFile = (f: string) => read(`sim/Suborned.Sim/${f}`);
const simAll = () => readdirSync(SIM).filter((f) => f.endsWith('.cs')).map((f) => simFile(f));

// ---------------------------------------------------------------- every string in the file, with what kind it is
type Str = { path: string; text: string; spoken: boolean };
const strings: Str[] = [];
const push = (path: string, text: string, spoken = false) => strings.push({ path, text, spoken });
{
  const b = t.briefing;
  b.card.forEach((s, i) => push(`briefing.card[${i}]`, s));
  push('briefing.objective', b.objective); push('briefing.win', b.win); push('briefing.clock.line', b.clock.line); push('briefing.purse.line', b.purse.line);
  push('briefing.hints.front-door', b.hints['front-door']); push('briefing.hints.pickup', b.hints.pickup); push('briefing.tracker', b.tracker);
  for (const [k, s] of Object.entries(b.fails)) push(`briefing.fails.${k}`, s);
  b.timeline.forEach((x, i) => push(`briefing.timeline[${i}]`, x.label));
  for (const [k, s] of Object.entries(b.cell)) push(`briefing.cell.${k}`, s);
  for (const [k, x] of Object.entries(t.targets)) push(`targets.${k}.about`, x.about);
  for (const [k, a] of Object.entries(t.actions)) { push(`actions.${k}.does`, a.does); for (const [r, s] of Object.entries(a.onTarget ?? {})) push(`actions.${k}.onTarget.${r}`, s); }
  for (const [k, x] of Object.entries(t.cancelCosts)) push(`cancelCosts.${k}`, x);
  const walk = (path: string, o: unknown) => { if (typeof o === 'string') push(path, o); else for (const [k, v] of Object.entries(o as object)) walk(`${path}.${k}`, v); };
  walk('activity', t.activity);
  for (const l of t.locks) push(`locks.${l.code}`, l.line);
  t.dialogue.forEach((e, i) => e.lines.forEach((s, j) => push(`dialogue[${i}].lines[${j}] ${lineKey(e)}`, s, true)));
}
/** A line with the host's placeholders filled at their widest. */
const widest = (s: string) => s.replace(/\{who\}/g, 'the councillor').replace(/\{op\}/g, 'Enforcer').replace(/\{m\}/g, '13').replace(/\{usd\}/g, '$100,000');

describe('the file', () => {
  test('parses as suborned.text/1 for the handover', () => {
    expect(t.mission).toBe('handover');
    expect(t.dialogue.length).toBeGreaterThan(200);
  });
  test('every line is at most 80 characters, placeholders filled at their widest', () => {
    const long = strings.filter((s) => widest(s.text).length > MAX_LINE).map((s) => `${s.path}: ${s.text} (${widest(s.text).length})`);
    expect(long).toEqual([]);
  });
  test('a spoken line is 1–12 words, has no stray spacing, and ends like a sentence', () => {
    const bad = strings.filter((s) => s.spoken).filter((s) => wordCount(s.text) < 1 || wordCount(s.text) > MAX_WORDS || s.text !== s.text.trim() || /\s{2}/.test(s.text) || !/[.!?]$/.test(s.text)).map((s) => `${s.path}: ${s.text}`);
    expect(bad).toEqual([]);
  });
  test('placeholders are only {who} {op} {m} {usd}', () => {
    const bad = strings.filter((s) => /\{(?!who\}|op\}|m\}|usd\})[^}]*\}/.test(s.text)).map((s) => `${s.path}: ${s.text}`);
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
      'Return', 'Ctrl', 'I', 'F', 'Trail', 'Alert', 'Ask', 'Pay', 'Help', 'Pressure', 'Call', 'Watch', 'Listen', 'Loop', 'Reroute', 'Enter', 'Take', 'Fire', 'Halcyon']);
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
  test('the parked affair appears nowhere', () => {
    for (const s of strings) expect(s.text.toLowerCase(), s.path).not.toMatch(/affair|lover|sweetheart|romance|mistress/);
    expect(JSON.stringify(raw).toLowerCase()).not.toMatch(/affair|lover/);
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
  test('each action\'s target lines name targets the file describes, and its agents exist', () => {
    for (const [k, a] of Object.entries(t.actions)) {
      for (const r of Object.keys(a.onTarget ?? {})) expect(r in t.targets, `${k}.onTarget.${r}`).toBe(true);
      for (const ag of a.by) expect(AGENTS).toContain(ag);
    }
  });
  test('lock samples resolve to their own code (file order decides), and every lock names real verbs', () => {
    const bad: string[] = [];
    for (const l of t.locks) {
      for (const s of l.samples) { const got = lockOf(t, s)?.code; if (got !== l.code) bad.push(`"${s}" → ${got} (wanted ${l.code})`); }
      for (const v of l.verbs) if (v !== '*' && !(v in t.actions)) bad.push(`${l.code}: verb ${v}`);
      for (const m of l.match) new RegExp(m, 'i');
    }
    expect(bad).toEqual([]);
    expect(new Set(t.locks.map((l) => l.code)).size).toBe(t.locks.length);
  });
  test('every refusal the engine can return finds a lock (GameOrders.cs, GameCombat.cs)', () => {
    if (!haveSim) return;
    const SAMPLE: [RegExp, string][] = [[/o\.Op|Op\.Name|op\.Id/, 'scout'], [/p\.Role|\.Role/, 'waitress'], [/[rR]each \/ 100/, '4'], [/BusyEnd/, '4120'], [/o\.Do/, 'dance'],
      [/o\.To\b/, 'nowhere'], [/o\.Target/, 'scout'], [/o\.Stance/, 'sulk'], [/MaxCm/, '25'], [/\bt\b/, 'waitress']];
    const fill = (s: string) => s.replace(/\{([^}]*)\}/g, (_m, expr: string) => SAMPLE.find(([re]) => re.test(expr))?.[1] ?? 'x');
    const IGNORE = new Set(['needs x']); // `needs {Describe(c)}`: the next test runs every Describe case
    // the functions that return refusals; their literals after `return` / `Refused =` are the engine's words
    const region = (src: string, from: string, to?: string) => { const a = src.indexOf(from); if (a < 0) throw new Error(`${from} not found`); return src.slice(a, to ? src.indexOf(to, a) : undefined); };
    const orders = simFile('GameOrders.cs'), combat = simFile('GameCombat.cs');
    const regions = [region(orders, 'string Check(Order o)', 'void Land('), region(orders, 'public bool CanCall'), region(combat, 'string Draw(Op op, Order o)', 'void Captures()'),
      region(combat, 'public LoudCard GoingLoud', 'public MoveQuote QuoteMove'), region(combat, 'string RunRefusal')];
    if (existsSync(join(SIM, 'GameCancel.cs'))) regions.push(region(simFile('GameCancel.cs'), 'string Cancel(Op op)', 'void ReportBy'));
    const found: { file: string; text: string }[] = [];
    for (const block of regions) {
      for (const line of block.split('\n')) {
        const bare = line.replace(/\$?"(?:[^"\\]|\\.)*"/g, (m) => 'x'.repeat(m.length)); // find `return` outside any string
        const at = bare.search(/\breturn\b|Refused\s*=/);
        if (at < 0 || /^\s*\/\//.test(line)) continue;
        for (const m of line.slice(at).matchAll(/\$?"((?:[^"\\]|\\.)*)"/g)) {
          const text = fill(m[1]!);
          if (!/\s/.test(text) || IGNORE.has(text)) continue; // a refusal is a sentence; a code or a name is not
          found.push({ file: 'engine', text });
        }
      }
    }
    // the cancel order's refusals are the activity's CancelText when it can't be cancelled
    if (existsSync(join(SIM, 'GameCancel.cs'))) for (const line of simFile('GameCancel.cs').split('\n')) {
      if (!/CancelCost = "(down|keeps-vale|nothing|none)"/.test(line)) continue;
      const m = line.match(/CancelText = "([^"]+)"/);
      if (m) found.push({ file: 'GameCancel.cs', text: m[1]! });
    }
    expect(found.length).toBeGreaterThan(40); // the scan reads something
    const holes = found.filter((f) => !lockOf(t, f.text)).map((f) => `${f.file}: ${f.text}`);
    expect(holes).toEqual([]);
  });
  test('every "needs …" reason (GameOrders.Describe) finds a lock', () => {
    if (!haveSim) return;
    const src = simFile('GameOrders.cs');
    const block = src.slice(src.indexOf('static string Describe(Cond c)'), src.indexOf('bool Holds(Cond c, PCtx x)'));
    const reasons: string[] = [];
    for (const line of block.split('\n')) {
      const m = line.match(/^\s*"([a-z-]+)" =>\s*(.*?),?\s*$/);
      if (!m) continue;
      const body = m[2]!;
      const variants = /c\.B\("is"\) \?/.test(body) ? [...body.matchAll(/"([^"]*)"/g)].slice(1).map((x) => x[1]!) : [body.replace(/c\.S\("approach"\)/g, '"@A"').replace(/c\.S\("what"\)/g, '"routine"').replace(/"\s*\+\s*"/g, '').replace(/"\s*\+\s*/g, '').replace(/\s*\+\s*"/g, '').replace(/^"|"$/g, '')];
      for (const v of variants) reasons.push(...(v.includes('@A') ? (['ask', 'pay', 'help', 'pressure'] as const).filter((a) => people.approaches[a].requires !== 'nothing').map((a) => v.replace('@A', a)) : [v]));
    }
    expect(reasons.length).toBeGreaterThanOrEqual(8);
    const holes = reasons.map((r) => 'needs ' + r).filter((r) => !lockOf(t, r));
    expect(holes).toEqual([]);
  });
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
    for (const src of simAll()) for (const m of src.matchAll(/Note\("([a-z.\-]+)"/g)) codes.add(m[1]!);
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
