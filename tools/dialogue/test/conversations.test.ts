// Brief 085: Sky's read of the blind test. A person says what they'd say; game facts stay on the cards. No model calls.
import { describe, expect, test } from 'bun:test';
import { check, priorScenes, specs } from '../conversations';

const spec = (id: string) => specs.find(s => s.id === id)!;
const scene = (lines: string[], s = spec('pressure-inez')) => ({
  beats: lines.map((line, i) => ({ speaker: s.speakers[i % s.speakers.length]!, line })),
  learned: ['Inez knows the Fixer\'s face.'], opens: ['Call Inez'],
});
const plain = ['The Office could hear, you know.', 'What do you want from me?', 'When I telephone, call him over here.', 'A few minutes, and then you leave us be?'];

describe('what a person would say', () => {
  test('naming the fear fails the check', () => {
    const s = spec('pressure-inez');
    expect(check(s, scene(plain))).toEqual([]);
    expect(check(s, scene([...plain.slice(0, 3), "I'm too scared to say no."])).join()).toContain('names the fear');
  });
  test('a polite "I\'m afraid" passes', () => {
    expect(check(spec('pressure-inez'), scene([...plain.slice(0, 3), "I'm afraid the Office could hear."]))).toEqual([]);
  });
  test('what the game does not do fails the check', () => {
    const s = spec('help-inez-2'), bad = (line: string) => check(s, scene([...plain.slice(0, 3), line], s)).join();
    expect(bad('Look under my saucer, Inez.')).toContain("isn't in the game");
    expect(bad('Please, take my tips.')).toContain("isn't in the game");
    expect(bad("He'd stand with you about a minute.")).toContain('a few minutes');
  });
  test('every sheet says what the player sees, and the guard is away a few minutes', () => {
    for (const s of specs) { expect(s.sheet).toContain('What the player sees'); expect(s.sheet).not.toMatch(/offers an envelope|Evening|about a minute|for a minute/); }
    for (const id of ['help-inez-2', 'pressure-inez', 'call-inez']) { expect(spec(id).sheet).toContain('about three minutes'); expect(spec(id).sheet).toContain('She never tells him anyone knows'); }
  });
  test('scenes follow the story: each lists only earlier scenes', () => {
    specs.forEach((s, i) => { for (const a of s.after ?? []) expect(specs.findIndex(x => x.id === a)).toBeLessThan(i); });
    expect(spec('call-inez').after).toContain('help-inez-2');
  });
  test('every scene with Inez carries who she is', () => {
    const withInez = specs.filter(s => s.speakers.includes('Inez'));
    expect(withInez.length).toBe(6);
    for (const s of withInez) expect(s.sheet).toContain('never threatens anyone');
  });
  test('the call after Pressure: she gives a reason, then tells him; only this sheet lets her', () => {
    const s = spec('call-inez-pressure');
    expect(s.after).toEqual(['listen-inez', 'pressure-inez']);
    expect(s.sheet).not.toContain('She never tells him anyone knows');
    expect(s.sheet).toContain('someone knows about the two of them');
    expect(s.sheet).toContain("can't be said on the line");
    const lines = ['It is time.', 'He is on the door.', 'The shift is long, love.', 'Come over, it will not wait.'];
    expect(check(s, scene(lines, s)).join()).toContain('never says: someone knows');
    expect(check(s, scene([...lines, 'Someone knows, and looked suspicious.', "I'm going back to my door."], s))).toEqual([]);
  });
  test('with --only, a scene still knows what the player read: from this run, else what Sky agreed', () => {
    const s = spec('call-inez-pressure'), got = priorScenes(s, []);
    expect(got.map(x => x.id)).toEqual(['listen-inez', 'pressure-inez']);
    expect(got[1]!.title).toBe('Pressure Inez');
    expect(got[1]!.beats[0]).toEqual({ speaker: 'Inez', line: 'Is it the Bureau? I know I\'m behind.' });
    const mine = { id: 'pressure-inez', title: 'mine', beats: [{ speaker: 'Inez', line: 'written this run' }] };
    expect(priorScenes(s, [mine])[1]).toEqual(mine);
    expect(priorScenes({}, [])).toEqual([]);
  });
  test('the sheets keep game facts out of speech and never count the ways', () => {
    expect(spec('pressure-inez').sheet).toContain('nobody says it aloud');
    expect(spec('pressure-inez').sheet).not.toContain('Let it land');
    expect(spec('listen-inez').sheet).not.toContain('two ways');
  });
});
