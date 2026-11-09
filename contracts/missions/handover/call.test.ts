// Brief 102 (engine 0.51.0, Sky's calls on the guard, 2026-11-09): the guard's end of the call in words. Option 1, the turned guard leaves his own door when called;
// step 4, nobody acts on a call after the cell's violence. A row or scene `about` the guard is his own call (HostCall.SummonAbout: the one called is the one who leaves);
// Inez's call reads the rows with no `about`, as before. The round's record is briefs/102/fable/guard-call.json; Sky reads briefs/102/fable/guard-call.html.
// Engine 0.52.0 (Keel's KnowsDown, Fable 2026-11-09): when he is down, her answer splits on whether she knows it; that round's record is briefs/102/fable/knows-down.json.
import { describe, expect, test } from 'bun:test';
import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { PeopleRules, allowedTemperaments } from '../../people/people.rules.schema.ts';
import { HAND_WORDS } from './speech.schema.ts';
import { TextFile, fillLock, lookup, lookupScene, playScene, scenesFor } from './text.schema.ts';

const root = join(import.meta.dir, '..', '..', '..');
const read = (p: string) => readFileSync(join(root, p), 'utf8');
const t = TextFile.parse(JSON.parse(read('contracts/missions/handover/text.json')));
const people = PeopleRules.parse(JSON.parse(read('contracts/people/people.rules.json')));
const CALL_CS = join(root, 'sim', 'Suborned.Sim', 'GameCall.cs');
/** The codes the engine can give his own call (GameCall.cs SummonRefusal from his end): the one called is the one who leaves, so his memory of the cell's violence is
 *  call-saw-violence, never summon-saw-violence, and summon-no-visit is CallAbsent's, never his while he keeps a post. */
const HIS = ['summon-busy', 'summon-came-once', 'summon-alert', 'summon-too-far', 'summon-off-street'];
/** What Inez can overhear of his refusal: within earshot of him only, so never when she is off the street (summon-off-street from his end is hers) or down. */
const HERS = ['summon-busy', 'summon-came-once', 'summon-alert', 'summon-too-far'];
const linesOf = (sc: { variants: { beats: ({ speaker: string; line: string } | { speaker: string; ref: string })[] }[] }) => sc.variants.flatMap((v) => v.beats.flatMap((b) => ('line' in b ? [b.line] : [])));
type Edit = { path: string; after: unknown; before?: unknown };
const raw = JSON.parse(read('contracts/missions/handover/text.json'));
const at = (path: string) => path.split('|').reduce<unknown>((o, k) => (o as Record<string, unknown> | undefined)?.[k], raw);
/** The call's round records, in order. A path a later round moved reads that round's words, where its `before` is the earlier round's `after`. */
const ROUNDS = ['briefs/102/fable/guard-call.json', 'briefs/102/fable/knows-down.json'].map((f) => (JSON.parse(read(f)) as { edits: Edit[] }).edits);
const since = (round: number, path: string, after: unknown) => ROUNDS.slice(round + 1).reduce((x, es) => es.find((e) => e.path === path && Bun.deepEquals(e.before, x))?.after ?? x, after);

describe('brief 102: the guard\'s end of the call', () => {
  test('his call scene in each temperament a guard can have: the Fixer asks, he says yes (his confirm), and nobody names where he goes', () => {
    const temps = allowedTemperaments(people, 'guard')!;
    expect([...temps].sort()).toEqual(['loyal', 'mercenary']);
    for (const temp of temps) {
      const sc = lookupScene(t, { kind: 'call', verb: 'call', about: 'guard', temperament: temp })!;
      expect(sc.about, temp).toBe('guard'); expect(sc.temperament, temp).toBe(temp);
      for (const v of sc.variants) {
        expect(new Set(v.beats.map((b) => b.speaker)), temp).toEqual(new Set(['fixer', 'guard']));
        expect(v.beats.some((b) => b.speaker === 'guard' && 'ref' in b && b.ref === 'confirm'), temp).toBe(true);
      }
      for (const l of linesOf(sc)) expect(l, temp).not.toMatch(/Inez|café|\bher\b|\bshe\b/i); // the Fixer may not know of her (turned by Pay or Help, no Listen)
      for (const n of [0, 1, 2]) for (const b of playScene(t, sc, 'seed-102', n).beats) expect(b.text.length, `${temp} ${n}`).toBeGreaterThan(0);
      expect(lookup(t, { event: 'confirm', verb: 'call', speaker: 'guard', temperament: temp })?.temperament, temp).toBe(temp);
    }
    expect(t.events.confirm!.speakers).toContain('guard'); expect(t.events.confirm!.log).toContain('call.guard');
  });
  test('his own call refused: every code the engine gives from his end has his words to the Fixer, and hers only for what she can overhear', () => {
    if (existsSync(CALL_CS)) {
      const src = readFileSync(CALL_CS, 'utf8');
      expect(src.indexOf('HoldsCellViolence(called)')).toBeGreaterThan(0);
      expect(src.indexOf('HoldsCellViolence(called)')).toBeLessThan(src.indexOf('HoldsCellViolence(leaves)')); // the one called is read first (step 4)
    }
    for (const c of HIS) {
      const e = lookup(t, { event: 'summon-refused', speaker: 'guard', outcome: c, about: 'guard' })!;
      expect(e.about, c).toBe('guard');
      for (const l of e.lines) expect(l, c).not.toMatch(/,\s*Inez\b|^Inez[,?]/); // said to the Fixer, never to her
    }
    for (const c of HERS) expect(lookup(t, { event: 'summon-refused', speaker: 'waitress', outcome: c, about: 'guard' })?.about, c).toBe('guard');
    const theirs = t.dialogue.filter((e) => e.event === 'summon-refused' && e.about === 'guard');
    expect(theirs.filter((e) => !(e.speaker === 'guard' ? HIS : HERS).includes(e.outcome ?? '')).map((e) => `${e.speaker} ${e.outcome}`)).toEqual([]);
    for (const c of [...HIS, 'summon-saw-violence', 'summon-no-visit']) for (const sp of ['waitress', 'guard']) // Inez's call reads the rows with no `about`, so its lines don't move
      expect(lookup(t, { event: 'summon-refused', speaker: sp, outcome: c })?.about, `${sp} ${c}`).toBeUndefined();
  });
  test('summon-busy holds either way: the lock names both of the pair, and his words give no reason he may not know', () => {
    expect(t.locks['summon-busy']!.line).toMatch(/^Inez or the guard is down/);
    for (const l of lookup(t, { event: 'summon-refused', speaker: 'guard', outcome: 'summon-busy', about: 'guard' })!.lines) expect(l).not.toMatch(/Inez|café|door|duty|down|fight|hurt/i);
  });
  test('step 4: call-saw-violence is a shut lock with a fact, no rule, and nothing rings: no line answers it', () => {
    const l = t.locks['call-saw-violence']!;
    expect(l.line).toContain('{who}'); expect(`${l.line} ${l.short}`).not.toMatch(/won't|help|again|after that|never|any more/i);
    expect(t.dialogue.filter((e) => e.outcome === 'call-saw-violence')).toEqual([]);
    expect(t.locks['summon-saw-violence']!.short).not.toBe(l.short); // a shut row on Inez never reads as if she saw what he saw
    expect(t.locks['summon-saw-violence']!.line).not.toMatch(/won't|come now/);
  });
  test('"He\'s not answering": Inez\'s call to a guard who is down and she doesn\'t know it, with its own lines', () => {
    const ev = t.events['summon-unanswered']!;
    expect(ev.speakers).toEqual(['waitress']); expect(ev.log).toBeUndefined();
    const e = lookup(t, { event: 'summon-unanswered', speaker: 'waitress' })!;
    expect(e.lines.length).toBe(2);
    for (const x of e.lines) expect(x).not.toMatch(/down|hurt|dead|knock|shot|tied|moment/i); // what she knows: only that nobody answers
  });
  test('"What have you lot done to him?": Inez\'s call to a guard who is down and she knows it (KnowsDown), with its own lines', () => {
    if (existsSync(CALL_CS)) {
      const src = readFileSync(CALL_CS, 'utf8');
      expect(src).toContain('public bool KnowsDown(string who, string of)'); // the read the host splits her two answers on
      expect(src.indexOf('HoldsCellViolence(called)')).toBeLessThan(src.indexOf('GuardDown || leaves.Down')); // a takedown or a shot she saw shuts the call first, so she never saw it done
    }
    const ev = t.events['summon-knows-down']!;
    expect(ev.speakers).toEqual(['waitress']); expect(ev.log).toBeUndefined();
    for (const id of ['summon-knows-down', 'summon-unanswered']) expect(t.events[id]!.note, id).toContain('KnowsDown');
    expect(t.events['summon-refused']!.trigger).toContain('summon-knows-down');
    const e = lookup(t, { event: 'summon-knows-down', speaker: 'waitress' })!;
    expect(e.lines.length).toBe(2);
    const others = [...lookup(t, { event: 'summon-unanswered', speaker: 'waitress' })!.lines, ...lookup(t, { event: 'summon-refused', speaker: 'waitress', outcome: 'summon-busy' })!.lines];
    for (const x of e.lines) {
      expect(others, x).not.toContain(x); // her other answers on the same card: he's down and she doesn't know it, or he's busy on his feet
      expect(x).not.toMatch(/answer/i); // she knows why he is silent
      expect(x).not.toMatch(/down|dead|kill|knock|shot|tied|hurt|carr|found|told|saw|seen/i); // true however she knows (found him, saw him carried, was told) and whatever his state
      expect(x).not.toMatch(/won't|can't|again|\bnever\b(?! mind)|any more|remember|forget/i); // no rule, nothing foretold ("never mind" waves the ask away)
    }
  });
  test('the lock form before a Listen: every summons refusal whose words name the pair has a tie-unknown form naming only {who}', () => {
    const pair = /Inez|guard|lover|\b(he|she|him|her|his|hers)\b/i;
    const summons = Object.entries(t.locks).filter(([c]) => c.startsWith('summon-') || c === 'call-saw-violence');
    expect(summons.length).toBeGreaterThanOrEqual(8);
    for (const [c, l] of summons) {
      const general = l.byTarget?.['tie-unknown'];
      if (!pair.test(`${l.line} ${l.short}`)) { expect(general, `${c}: its base names nobody, so it needs no second form`).toBeUndefined(); continue; }
      expect(general, c).toBeTruthy();
      expect(`${general!.line} ${general!.short}`, c).not.toMatch(pair);
      for (const n of ['{tick}', '{m}', '{usd}']) expect(general!.line.includes(n), `${c} ${n}`).toBe(l.line.includes(n));
      expect(fillLock(t, { code: c, tick: 950 }, { who: 'the driver', variant: 'tie-unknown', clock: () => '1:35' }), c).toBe(general!.line.replace('{who}', 'the driver').replace('{tick}', '1:35'));
    }
    expect(fillLock(t, { code: 'summon-busy' }, { who: 'Inez' })).toBe(t.locks['summon-busy']!.line); // the Handover's base words, until the host passes the variant
  });
  test('his visit at the café on his own call, and his warning on a pressed visit; today\'s host still plays Inez\'s scene first', () => {
    const own = lookupScene(t, { kind: 'city', event: 'guard-at-cafe', about: 'guard' })!;
    expect(own.about).toBe('guard');
    for (const l of linesOf(own)) expect(l).not.toMatch(/you said|couldn't wait|can't wait|what is it|you came/i); // she never sent for him
    for (const sp of ['waitress', 'guard']) for (const l of lookup(t, { event: 'guard-at-cafe', speaker: sp, about: 'guard' })!.lines) expect(l, sp).not.toMatch(/you said|couldn't wait|can't wait|what is it|you came/i);
    const warns = lookupScene(t, { kind: 'city', event: 'guard-at-cafe', outcome: 'he-warns' })!;
    expect(warns.outcome).toBe('he-warns');
    for (const v of warns.variants) { const first = v.beats[0]!; expect(first.speaker).toBe('guard'); expect('line' in first && /about us/.test(first.line)).toBe(true); }
    const all = scenesFor(t, 'city', 'guard-at-cafe');
    expect(all[0]!.about).toBeUndefined(); expect(all[0]!.outcome).toBeUndefined(); // HostFeed.CityScene plays the first whose sight holds
    expect(lookupScene(t, { kind: 'city', event: 'guard-at-cafe' })).toBe(all[0]!);
  });
  test('the debrief: his own call\'s route line, the page\'s note, the Trail row true from either end, and his kept lines', () => {
    expect(t.debrief.route['guard-goes-to-waitress']).toMatch(/Fixer called the guard/);
    const row = t.debrief.page!.trail.rows.call!.row;
    for (const who of ['Inez', 'the guard']) { const filled = row.replace('{who}', who); expect(filled).toContain(who); expect(filled).not.toMatch(/the guard .*the guard/); }
    expect(t.debrief.page!.how.answered).toBeTruthy(); expect(t.debrief.page!.how.answered).not.toMatch(/Inez/);
    for (const k of ['guard-may-identify-fixer', 'guard-may-be-questioned', 'guard-leaves-door-when-called']) expect(t.debrief.assets[k], k).toMatch(/^The guard, /);
  });
  test('the Call card on him names nobody he would go to, and his turned result says what he is for now', () => {
    expect(t.actions.call!.onTarget!.guard).toBeTruthy(); expect(t.actions.call!.onTarget!.guard).not.toMatch(/Inez|café/);
    const turned = t.scenes.items.filter((sc) => sc.kind === 'approach' && sc.about === 'guard' && sc.outcome === 'turned');
    expect(turned.length).toBe(4);
    for (const sc of turned) expect(sc.result).toBe('{who} is turned. Call {who} off the door.');
  });
  test('the record (guard-call.json): every row as written and marked by hand, every scene, every edit at its path, every kept placeholder', () => {
    const rec = JSON.parse(read('briefs/102/fable/guard-call.json')) as { rows: { event: string; speaker: string; outcome?: string; temperament?: string; about?: string; lines: string[] }[];
      scenes: unknown[]; edits: { path: string; after: unknown; before?: unknown }[]; kept: { path: string; words: unknown }[] };
    expect(rec.rows.length).toBe(14);
    for (const r of rec.rows) {
      const e = t.dialogue.find((x) => x.event === r.event && x.speaker === r.speaker && x.outcome === r.outcome && x.temperament === r.temperament && x.about === r.about)!;
      expect(e.lines, `${r.event} ${r.speaker} ${r.outcome ?? ''}`).toEqual(r.lines);
      expect(e.hand?.startsWith(HAND_WORDS)).toBe(true); expect(e.hand).toContain('guard-call.json');
    }
    for (const s of rec.scenes) expect(t.scenes.items).toContainEqual(s as never);
    for (const e of rec.edits) { expect(at(e.path), e.path).toEqual(since(0, e.path, e.after)); if (e.before !== undefined) expect(at(e.path), e.path).not.toEqual(e.before); }
    for (const k of rec.kept) expect(at(k.path), k.path).toEqual(k.words);
  });
  test('the record (knows-down.json): its row as written, read line by line and marked by hand, and every edit at its path', () => {
    const rec = JSON.parse(read('briefs/102/fable/knows-down.json')) as { rows: { event: string; speaker: string; lines: string[]; reads: { line: string }[] }[]; edits: Edit[] };
    expect(rec.rows.length).toBe(1);
    for (const r of rec.rows) {
      const e = t.dialogue.find((x) => x.event === r.event && x.speaker === r.speaker && !x.outcome && !x.about)!;
      expect(e.lines).toEqual(r.lines); expect(r.reads.map((x) => x.line)).toEqual(r.lines);
      expect(e.hand?.startsWith(HAND_WORDS)).toBe(true); expect(e.hand).toContain('knows-down.json');
    }
    for (const e of rec.edits) { expect(at(e.path), e.path).toEqual(since(1, e.path, e.after)); if (e.before !== undefined) expect(at(e.path), e.path).not.toEqual(e.before); }
  });
});
