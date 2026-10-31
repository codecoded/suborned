// Brief 085: Sky's read of the blind test. A person says what they'd say; game facts stay on the cards. No model calls.
import { describe, expect, test } from 'bun:test';
import { check, specs } from '../conversations';

const spec = (id: string) => specs.find(s => s.id === id)!;
const scene = (lines: string[], s = spec('pressure-inez')) => ({
  beats: lines.map((line, i) => ({ speaker: s.speakers[i % s.speakers.length]!, line })),
  learned: ['Inez knows the Fixer\'s face.'], opens: ['Call Inez'],
});
const plain = ['The Office could hear, you know.', 'What do you want from me?', 'When I telephone, call him over here.', 'One minute, and then you leave us be?'];

describe('what a person would say', () => {
  test('naming the fear fails the check', () => {
    const s = spec('pressure-inez');
    expect(check(s, scene(plain))).toEqual([]);
    expect(check(s, scene([...plain.slice(0, 3), "I'm too scared to say no."])).join()).toContain('names the fear');
  });
  test('a polite "I\'m afraid" passes', () => {
    expect(check(spec('pressure-inez'), scene([...plain.slice(0, 3), "I'm afraid the Office could hear."]))).toEqual([]);
  });
  test('every scene with Inez carries who she is', () => {
    const withInez = specs.filter(s => s.speakers.includes('Inez'));
    expect(withInez.length).toBe(5);
    for (const s of withInez) expect(s.sheet).toContain('never threatens anyone');
  });
  test('the sheets keep game facts out of speech and never count the ways', () => {
    expect(spec('pressure-inez').sheet).toContain('nobody says it aloud');
    expect(spec('pressure-inez').sheet).not.toContain('Let it land');
    expect(spec('listen-inez').sheet).not.toContain('two ways');
  });
});
