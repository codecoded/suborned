// Brief 085: the page Sky reads blind, and its key, kept apart. The page shows each scene's two versions as X and Y, side chosen at random per scene; the key holds which is which and the rhythm counts per arm.
//   bun tools/dialogue/blind-page.ts --a <run dir> --b <run dir> [--labels file]     prints neither the key nor any count per label. A key already on disk keeps its sides.
import { randomInt } from 'node:crypto';
import { resolve } from 'node:path';
import { root, writeJson } from '../rag/config';
import { lifted, loadJobs, tokens } from './exemplars/exemplars';

export type Scene = { id: string; title: string; beats: { speaker: string; line: string }[]; learned: string[]; opens: string[] };
export type Arm = 'A' | 'B';
export type Sides = Record<string, { X: Arm; Y: Arm }>;

const esc = (s: string) => s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
export const wordCount = (s: string) => s.trim().split(/\s+/).length;
/** One fair choice per scene, from the OS's generator: nothing in the code or the run decides it. */
export const pickSides = (ids: string[], coin: () => number = () => randomInt(2)): Sides => Object.fromEntries(ids.map(id => [id, coin() ? { X: 'A', Y: 'B' } : { X: 'B', Y: 'A' }]));

const version = (label: string, s: Scene) => `<section class="v"><h3>${label}</h3>${s.beats.map(b => `<div class="b"><span class="w">${esc(b.speaker)}</span><p>${esc(b.line)}</p></div>`).join('')}<div class="cards">${s.learned.map(x => `<span class="c">${esc(x)}</span>`).join('')}${s.opens.map(x => `<span class="c o">${esc(x)}</span>`).join('')}</div></section>`;

/** The page. Holds X and Y only: nothing about how a version was made, in the text or the markup. */
export function page(ids: string[], scenes: Record<Arm, Scene[]>, sides: Sides): string {
  const of = (arm: Arm, id: string) => scenes[arm].find(s => s.id === id)!;
  return `<!doctype html><html lang="en"><meta charset="utf-8"><title>Two scenes, two versions</title><meta name="viewport" content="width=device-width,initial-scale=1"><style>
*{box-sizing:border-box}body{margin:0;background:#fbf5e9;color:#26302f;font:17px/1.45 Georgia,'Times New Roman',serif}main{max-width:1120px;margin:auto;padding:28px 24px 60px}
h1{font-size:28px;margin:0 0 6px}h2{font-size:22px;margin:34px 0 10px;border-top:1px solid #b08d4a;padding-top:16px}h3{font:700 22px Helvetica,Arial,sans-serif;margin:0 0 12px}
.lead{color:#66706c;margin:0}.pair{display:grid;grid-template-columns:1fr 1fr;gap:20px}.v{background:#fffdf7;border:1px solid #e6dcc6;border-radius:10px;padding:16px 18px}
.b{margin:0 0 10px}.w{font:700 12.5px Helvetica,Arial,sans-serif;color:#66706c}.b p{margin:2px 0 0;background:#fff;border:1px solid #e6dcc6;border-radius:12px;padding:7px 12px;display:inline-block}
.cards{display:flex;flex-wrap:wrap;gap:6px;margin-top:14px}.c{font:13px Helvetica,Arial,sans-serif;background:#f1eadb;border-radius:999px;padding:3px 10px}.o{background:#8fcfc0;font-weight:700}
@media(max-width:820px){.pair{grid-template-columns:1fr}}</style><main><h1>Two scenes, two versions</h1><p class="lead">Each scene plays when the act lands, one bubble at a time, then the cards. Read X and Y, and say which is closer and why.</p>
${ids.map(id => `<h2>${esc(of('A', id).title)}</h2><div class="pair">${version('X', of(sides[id]!.X, id))}${version('Y', of(sides[id]!.Y, id))}</div>`).join('\n')}
</main></html>`;
}

const sd = (xs: number[]) => { const m = xs.reduce((a, b) => a + b, 0) / xs.length; return Math.sqrt(xs.reduce((n, x) => n + (x - m) ** 2, 0) / xs.length); };
const round = (x: number) => Math.round(x * 100) / 100;
export const brokenOff = (line: string) => /(—|–|--|-|…|\.\.\.)["”]?\s*$/.test(line);

/** A question, what came next, and whether the next line is at least a reply that isn't itself a question. A code count; whether a reply is a straight answer needs a reader, so that's a separate label. */
export const questions = (s: Scene) => s.beats.flatMap((b, i) => b.line.trim().endsWith('?') ? [{ scene: s.id, line: b.line, next: s.beats[i + 1]?.line ?? null, sameSpeakerNext: s.beats[i + 1]?.speaker === b.speaker, replyNotAQuestion: !!s.beats[i + 1] && s.beats[i + 1]!.speaker !== b.speaker && !s.beats[i + 1]!.line.trim().endsWith('?') }] : []);

export type Labels = Record<string, boolean>;
/** The rhythm counts the study asked for. `labels`: the reader's yes or no on each question, keyed "scene|line". */
export function rhythm(scenes: Scene[], excerpts: string[], labels: Labels = {}) {
  const beats = scenes.flatMap(s => s.beats), lens = beats.map(b => wordCount(b.line));
  const speakers = [...new Set(beats.map(b => b.speaker))];
  const qs = scenes.flatMap(questions).map(q => ({ ...q, straightByReader: labels[`${q.scene}|${q.line}`] ?? null }));
  return {
    lines: beats.length, words: lens.reduce((a, b) => a + b, 0), mean: round(lens.reduce((a, b) => a + b, 0) / lens.length), sd: round(sd(lens)),
    at9or10: lens.filter(n => n === 9 || n === 10).length, atMost5: lens.filter(n => n <= 5).length, at12: lens.filter(n => n === 12).length, over12: lens.filter(n => n > 12).length,
    perSpeaker: Object.fromEntries(speakers.map(w => { const l = beats.filter(b => b.speaker === w).map(b => wordCount(b.line)); return [w, { lines: l.length, mean: round(l.reduce((a, b) => a + b, 0) / l.length), sd: round(sd(l)), min: Math.min(...l), max: Math.max(...l) }]; })),
    questions: qs.length, repliesNotAQuestion: qs.filter(q => q.replyNotAQuestion).length, straightAnswersByReader: qs.every(q => q.straightByReader !== null) ? qs.filter(q => q.straightByReader).length : null, questionList: qs,
    brokenOff: beats.filter(b => brokenOff(b.line)).length,
    overlap5: scenes.flatMap(s => lifted(s.beats.map(b => b.line), excerpts).map(g => `${s.id}: ${g}`)),
    wordsChecked: tokens(beats.map(b => b.line).join(' ')).length,
  };
}

const IDS = ['listen-inez', 'pressure-inez'];
if (import.meta.main) {
  const args = Bun.argv.slice(2), arg = (k: string) => { const i = args.indexOf(k); return i >= 0 ? args[i + 1] : undefined; };
  const dirs: Record<Arm, string> = { A: arg('--a')!, B: arg('--b')! };
  if (!dirs.A || !dirs.B) throw new Error('--a and --b are required');
  const json = async (p: string) => JSON.parse(await Bun.file(resolve(root, p)).text());
  const load = (arm: Arm) => Promise.all(IDS.map(async id => json(`${dirs[arm]}/${id}/final.json`) as Promise<Scene & { faults: string[] }>));
  const scenes: Record<Arm, (Scene & { faults: string[] })[]> = { A: await load('A'), B: await load('B') };
  const keyFile = 'briefs/085/fable/blind-key.json', pageFile = 'briefs/085/conversations-blind.html';
  const old = (await Bun.file(resolve(root, keyFile)).exists()) ? await json(keyFile) : undefined;
  const sides: Sides = old?.sides ?? pickSides(IDS);
  const labels: Labels = arg('--labels') ? await json(arg('--labels')!) : {};
  const excerpts = loadJobs().flatMap(j => j.excerpts.map(e => e.text));
  type Rec = { id: string; modelsUsed: string[]; schemaSha256: string; status: string; costUsd: number | null };
  const callsOf = async (arm: Arm): Promise<Rec[]> => (await Promise.all([...IDS, 'review'].map(d => json(`${dirs[arm]}/${d}/calls.json`) as Promise<Rec[]>))).flat();
  const arms = await Promise.all((['A', 'B'] as Arm[]).map(async a => [a, { run: await json(`${dirs[a]}/run.json`), calls: await callsOf(a) }] as const));
  await Bun.write(resolve(root, pageFile), page(IDS, scenes, sides));
  await writeJson(keyFile, {
    note: 'The key to briefs/085/conversations-blind.html. Hold until Sky has read. Arm A is the control (writer.md, editor.md, specs); arm B is writer-b.md, editor-b.md, blind-arm.ts. Questions are lines that end in a question mark, as in the report. straightByReader is Fable reading the lines, not blind: true when the next line honestly answers what was asked.',
    sides, runs: dirs,
    arms: Object.fromEntries(arms.map(([a, { run, calls }]) => [a, {
      modelCalls: calls.length, rejected: calls.filter(c => c.status !== 'accepted').length, models: [...new Set(calls.flatMap(c => c.modelsUsed))].sort(), schemas: [...new Set(calls.map(c => c.schemaSha256))].sort(),
      faults: run.faults, reviewFaults: run.reviewFaults, reportedCostUsd: round(calls.reduce((n, c) => n + (c.costUsd ?? 0), 0)), rhythm: rhythm(scenes[a], excerpts, labels),
    }])),
  });
  console.log(`${pageFile} written; key in ${keyFile}`);
}
