// Brief 085's blind test, offline: the exemplars' shape and proof, the lifted-words check, the card patch, the second arm's inputs and the page's blindness. No model, no index. Run: bun test tools/dialogue/test/blind.test.ts
import { describe, expect, test } from 'bun:test';
import { createHash } from 'node:crypto';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { JOBS, LAST_PD_YEAR, inSource, lifted, loadJobs, loadRules, loadSources, normalise, tokens } from '../exemplars/exemplars';
import { CARD_TO, cardAdapter, checkB, patchCard, scenesB, voiceMaterial, voicesB } from '../blind-arm';
import { brokenOff, page, pickSides, questions, rhythm, type Scene, type Sides } from '../blind-page';
import { check, specs } from '../conversations';

const root = join(import.meta.dir, '..', '..', '..');
const text = (p: string) => readFileSync(join(root, p), 'utf8');

describe('exemplars', () => {
  const jobs = loadJobs(), sources = loadSources();
  test('every job has 2 to 5 excerpts, each with a note on its moves', () => {
    expect(jobs.map(j => j.job)).toEqual([...JOBS]);
    for (const j of jobs) { expect(j.excerpts.length).toBeGreaterThanOrEqual(2); expect(j.excerpts.length).toBeLessThanOrEqual(5); }
  });
  test('every note quotes its own excerpt', () => {
    for (const e of jobs.flatMap(j => j.excerpts)) for (const m of e.moves) expect(normalise(e.text)).toContain(normalise(m.at));
  });
  test('every source is public domain in the US, a translation by its own date, and names its origin', () => {
    for (const s of sources) {
      expect(Math.max(s.published, s.translationPublished ?? 0)).toBeLessThanOrEqual(LAST_PD_YEAR);
      expect((s.translator === null) === (s.translationPublished === null)).toBe(true);
      expect(s.url).toStartWith('https://');
    }
  });
  test('the recorded proof matches the excerpts as they stand', () => {
    const proof = JSON.parse(text('tools/dialogue/exemplars/verified.json')) as { excerpts: { id: string; sha256: string }[] };
    const all = jobs.flatMap(j => j.excerpts);
    expect(proof.excerpts.map(e => e.id).sort()).toEqual(all.map(e => e.id).sort());
    for (const e of all) expect(proof.excerpts.find(p => p.id === e.id)!.sha256).toBe(createHash('sha256').update(normalise(e.text)).digest('hex'));
  });
  test('verbatim means verbatim: a line wrap passes, a changed word fails', () => {
    expect(inSource('Fairly so, yes.', 'KROGSTAD.\nFairly\n  so, yes. But')).toBe(true);
    expect(inSource('Fairly so, sure.', 'KROGSTAD.\nFairly so, yes.')).toBe(false);
    expect(inSource('They did it.', '_They_ did⁠ it.')).toBe(true);
  });
  test('speech rules cover the six speakers, the Fixer first and with a price', () => {
    const r = loadRules().speakers, ids = new Set(jobs.flatMap(j => j.excerpts.map(e => e.id)));
    expect(r.map(x => x.speaker)).toEqual(['Fixer', 'Inez', 'Scout', 'Milena', 'Zhou', 'Okafor']);
    expect(r[0]!.pays).toBeTruthy();
    for (const x of r) for (const id of x.movesFrom) expect(ids.has(id)).toBe(true);
  });
  test('the exemplars stay out of the RAG corpus', () => {
    expect(text('tools/rag/corpus.ts')).not.toContain('exemplars');
    expect(text('tools/rag/gdd-sources.ts')).not.toContain('exemplars');
  });
});

describe('lifted words', () => {
  test('tokens ignore case, curly apostrophes and punctuation', () => expect(tokens('It’s, “FINE”.')).toEqual(["it's", 'fine']));
  test('4 words pass, 5 fail, and a run across two bubbles counts', () => {
    const src = ['We can go to the river when it rains'];
    expect(lifted(['we can go to somewhere'], src)).toEqual([]);
    expect(lifted(['We can go to the shops'], src)).toEqual(['we can go to the']);
    expect(lifted(['Well, we can go', 'to the river.'], src).length).toBeGreaterThan(0);
  });
});

describe('the second arm', () => {
  const base = { name: 'dialogue-designer', model: 'opus', source: 's', body: 'x, bubbles ≤ 10 words; y', text: 'x, bubbles ≤ 10 words; y' };
  test('the card patch changes that one limit, for this arm, and leaves other cards alone', async () => {
    expect(patchCard(base).body).toBe(`x, ${CARD_TO}; y`);
    expect(patchCard({ ...base, name: 'systems-designer' }).body).toBe(base.body);
    expect(() => patchCard({ ...base, body: 'none' })).toThrow();
    const real = await cardAdapter.loadCard('dialogue-designer');
    expect(real.body).toContain(CARD_TO); expect(real.body).not.toContain('bubbles ≤ 10 words');
    expect(text('.claude/agents/dialogue-designer.md')).toContain('bubbles ≤ 10 words');
  });
  test('the brief drops what Sky\'s calls and the report remove, and keeps the limits', () => {
    const w = text('briefs/085/fable/writer-b.md'), e = text('briefs/085/fable/editor-b.md');
    for (const gone of ['aim for 10', 'One sentence or two short ones', 'Clarity beats subtlety', 'What goes wrong today']) expect(w).not.toContain(gone);
    for (const gone of ['Better one strong line', 'aim for 10', 'Clarity beats subtlety', 'place, number or deadline']) expect(e).not.toContain(gone);
    expect(w).toContain('A line is at most 12 words');
    expect(w).toContain('more than one way to win anyone over');
  });
  test('the sheets are bare: no scripted phrasing, no counted options, no "lender"', () => {
    for (const s of scenesB) {
      for (const gone of ['Let it land', 'in a line', 'two ways', 'Two ways', 'lender', 'takes no pleasure']) expect(s.sheet).not.toContain(gone);
      expect(s.sheet).toMatch(/wants:/); expect(s.sheet).toMatch(/Fears:/); expect(s.sheet).toMatch(/Stake:/); expect(s.sheet).toMatch(/cause of/);
    }
    expect(voicesB('waitress')).not.toContain('lender');
    expect(text('contracts/missions/handover/text.json')).toContain('the lender');
  });
  test('the voice material holds each scene\'s rules and excerpts, and no other speaker', () => {
    const v = voiceMaterial(scenesB[0]!, ['Inez', 'Milena', 'Scout']);
    expect(v).toContain('Refuses to say'); expect(v).toContain('[gaev-anchovies]'); expect(v).not.toContain('Zhou\n');
  });
  test('both arms run under the control\'s beat limits, speakers and required facts', () => {
    expect(scenesB.map(s => s.id)).toEqual(['listen-inez', 'pressure-inez']);
    for (const s of scenesB) expect(specs.some(x => x.id === s.id)).toBe(true);
  });
  test('checkB is check() and the overlap, and the overlap catches a lifted line', () => {
    const s = specs.find(x => x.id === 'pressure-inez')!;
    const clean = { beats: [{ speaker: 'Inez', line: 'Tell the Bureau.' }, { speaker: 'Fixer', line: 'Call him over.' }, { speaker: 'Inez', line: 'Fine.' }, { speaker: 'Fixer', line: 'Good.' }], learned: [], opens: [] };
    expect(checkB(s, clean)).toEqual(check(s, clean));
    const robbed = { ...clean, beats: [...clean.beats, { speaker: 'Fixer', line: 'You will be so kind as to call.' }] };
    expect(checkB(s, robbed).some(f => f.startsWith('run of 5 words'))).toBe(true);
  });
});

describe('the blind page', () => {
  const scene = (id: string, tag: string): Scene => ({ id, title: 'Listen to Inez', beats: [{ speaker: 'Inez', line: `${tag} line?` }, { speaker: 'Milena', line: 'One two three four five six seven eight nine.' }, { speaker: 'Inez', line: 'Wait, I—' }], learned: ['Owes the Bureau.'], opens: ['Help'] });
  const a = [scene('s', 'alpha')], b = [scene('s', 'beta')];
  test('the sides are independent coin tosses over both outcomes', () => {
    expect(pickSides(['s'], () => 1).s).toEqual({ X: 'A', Y: 'B' });
    expect(pickSides(['s'], () => 0).s).toEqual({ X: 'B', Y: 'A' });
    expect(Object.keys(pickSides(['p', 'q']))).toEqual(['p', 'q']);
  });
  test('a page gives away nothing: same markup either way, no arm words, no counts', () => {
    const sides: Sides = { s: { X: 'A', Y: 'B' } }, flip: Sides = { s: { X: 'B', Y: 'A' } };
    const one = page(['s'], { A: a, B: b }, sides), two = page(['s'], { A: a, B: b }, flip);
    expect(one.indexOf('alpha')).toBeLessThan(one.indexOf('beta'));
    expect(two.indexOf('beta')).toBeLessThan(two.indexOf('alpha'));
    expect(one.replace('alpha', '#').replace('beta', '@').length).toBe(two.replace('beta', '#').replace('alpha', '@').length);
    for (const w of [/\barm\b/i, /control/i, /writer-b/i, /exemplar/i, /classic/i, /blind/i, /key/i]) expect(one).not.toMatch(w);
    expect(one).toContain('<h3>X</h3>'); expect(one).toContain('Owes the Bureau.');
  });
  test('the rhythm counts', () => {
    const r = rhythm(a, ['no overlap here at all today']);
    expect(r.lines).toBe(3); expect(r.at9or10).toBe(1); expect(r.brokenOff).toBe(1); expect(r.questions).toBe(1);
    expect(r.perSpeaker.Milena!.mean).toBe(9); expect(r.overlap5).toEqual([]);
    expect(r.straightAnswersByReader).toBe(null);
    expect(rhythm(a, [], { 's|alpha line?': false }).straightAnswersByReader).toBe(0);
    expect(questions(a[0]!)[0]!.replyNotAQuestion).toBe(true);
    expect(brokenOff('And then—')).toBe(true); expect(brokenOff('And then.')).toBe(false);
  });
});
