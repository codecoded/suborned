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
import { AGENTS, FAILS, HINT_MAX, MAX_LINE, MAX_WORDS, OUTCOMES, PEOPLE, TextFile, VERBS, fillLock, lineKey, lookup, pickIndex, pickLine, wordCount } from './text.schema.ts';

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
  for (const [k, x] of Object.entries(t.targets)) push(`targets.${k}.about`, x.about);
  for (const [k, a] of Object.entries(t.actions)) { push(`actions.${k}.does`, a.does); if (a.withTarget) push(`actions.${k}.withTarget`, a.withTarget); if (a.then) push(`actions.${k}.then`, a.then); for (const [r, s] of Object.entries(a.onTarget ?? {})) push(`actions.${k}.onTarget.${r}`, s); }
  for (const [id, h] of Object.entries(t.hints.items)) push(`hints.${id}`, h.line, false, HINT_MAX);
  for (const [k, x] of Object.entries(t.toasts)) push(`toasts.${k}`, x.line, false, HINT_MAX);
  push('goingLoud.captured', t.goingLoud.captured);
  for (const [k, x] of Object.entries(t.cancelCosts)) push(`cancelCosts.${k}`, x);
  const walk = (path: string, o: unknown) => { if (typeof o === 'string') push(path, o); else for (const [k, v] of Object.entries(o as object)) walk(`${path}.${k}`, v); };
  walk('activity', t.activity);
  for (const [code, l] of Object.entries(t.locks)) push(`locks.${code}`, l.line);
  t.dialogue.forEach((e, i) => e.lines.forEach((s, j) => push(`dialogue[${i}].lines[${j}] ${lineKey(e)}`, s, true)));
}
/** A line with the host's placeholders filled at their widest. */
const widest = (s: string) => s.replace(/\{who\}/g, 'the councillor').replace(/\{op\}/g, 'Enforcer').replace(/\{m\}/g, '13').replace(/\{usd\}/g, '$100,000').replace(/\{target\}/g, 'the entrance camera').replace(/\{tick\}/g, '10:30');

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
  test('placeholders are only {who} {op} {m} {usd} {tick} {target}', () => {
    const bad = strings.filter((s) => /\{(?!who\}|op\}|m\}|usd\}|tick\}|target\})[^}]*\}/.test(s.text)).map((s) => `${s.path}: ${s.text}`);
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
      'Return', 'Ctrl', 'I', 'F', 'N', 'Trail', 'Alert', 'Ask', 'Pay', 'Help', 'Pressure', 'Call', 'Watch', 'Listen', 'Loop', 'Reroute', 'Enter', 'Take', 'Fire', 'Carry', 'Halcyon']);
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
  test('fillLock fills the numbers and the names', () => {
    const clock = (tk: number) => `${Math.floor(tk / 600)}:${String(Math.floor(tk / 10) % 60).padStart(2, '0')}`;
    expect(fillLock(t, { code: 'too-far', m: 4 }, { who: 'Inez' })).toBe('Too far from Inez. Walk within 4 m first.');
    expect(fillLock(t, { code: 'stand-at-door', cm: 600 })).toContain('within 6 m');
    expect(fillLock(t, { code: 'no-cash', usd: 90000 })).toContain('$90,000');
    expect(fillLock(t, { code: 'agent-busy', tick: 1830 }, { op: 'Scout', clock })).toBe('Scout is busy until 3:03. Wait, or use another agent.');
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
  test('H1–H11 are all here, in priority order, with Motive\'s trigger, ring and gone-when copied word for word', () => {
    expect(items.map(([id]) => id)).toEqual(Array.from({ length: 11 }, (_, i) => `H${i + 1}`));
    expect(items.map(([, h]) => h.priority)).toEqual(items.map((_, i) => i + 1));
    if (!rows.size) return;
    expect([...rows.keys()]).toEqual(items.map(([id]) => id));
    for (const [id, h] of items) expect({ trigger: h.trigger, rings: h.rings, goneWhen: h.goneWhen }, id).toEqual(rows.get(id)!);
  });
  test('each hint fits 110 characters, names an agent, a verb and a target, and introduces only its own verb', () => {
    const word: Record<string, string> = { turn: 'turn', mislead: 'false tip' };
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
    if (howTo) for (const [k, v] of Object.entries(MEANS)) expect(howTo, `${k} (${v.action}) is bound in HOW-TO-PLAY.md`).toMatch(new RegExp(`\\*\\*[^*]*\\b${k}\\b[^*]*\\*\\*`));
  });
  test('H11 says how to carry: right click the downed agent with another selected', () => {
    const l = t.hints.items.H11!.line;
    expect(l).toMatch(/right click/i);
    expect(l).toMatch(/another agent/i);
    expect(l).not.toMatch(/\(C\)/);
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
