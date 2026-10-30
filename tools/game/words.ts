// The mission's words on their own (brief 078): a change to text.json reaches the player with no engine build. contracts/missions/handover/text.json against its schema (the same check tools/sim/unity.ts makes), text.test.ts,
// the words the proofs compare (tools/game/proofwords.json), then the same bytes into unity/Assets/StreamingAssets/sim/text.json. Host content, never an engine input: engine.json does not hash it.
// usage: bun tools/game/words.ts    (the batch's `words` step; exit 1 on any failure)
import { mkdirSync, readdirSync, readFileSync, writeFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { TextFile } from '../../contracts/missions/handover/text.schema';

export const ROOT = resolve(import.meta.dir, '../..');
const TEXT = join(ROOT, 'contracts/missions/handover/text.json'), PLAY = join(ROOT, 'unity/Assets/Suborned/Runtime/Play'), PINS = join(ROOT, 'tools/game/proofwords.json');
const COMPARE = /(?:Contains|StartsWith|EndsWith|==|!=|IndexOf)\s*\(?\s*"([A-Z][^"{}]{2,})"/g;

/** Every string in text.json, one per line. */
export function wordsOf(text: unknown): string { const out: string[] = []; const walk = (v: unknown) => { if (typeof v === 'string') out.push(v); else if (v && typeof v === 'object') for (const x of Object.values(v)) walk(x); }; walk(text); return out.join('\n'); }
/** The capitalised words a proof compares that text.json holds: proof file → words. Lower-case literals are engine codes and state names, not words. */
export function comparedWords(sources: Record<string, string>, words: string): Record<string, string[]> {
  const out: Record<string, string[]> = {};
  for (const [f, t] of Object.entries(sources).sort()) {
    const hits = new Set<string>();
    for (const m of t.replace(/\/\/\/?[^\n]*/g, '').matchAll(COMPARE)) if (words.includes(m[1]!)) hits.add(m[1]!);
    if (hits.size) out[f] = [...hits].sort();
  }
  return out;
}
/** What moved against the pinned list: a word a proof compares that left text.json fails, naming the proof; a new comparison must be pinned. */
export function wordGaps(pins: Record<string, string[]>, sources: Record<string, string>, words: string): string[] {
  const now = comparedWords(sources, words), gaps: string[] = [];
  for (const [f, list] of Object.entries(pins)) for (const w of list) {
    if (!words.includes(w)) gaps.push(`${f} compares "${w}", which is no longer in text.json: change the proof or the word (the section would fail in the player)`);
    else if (!now[f]?.includes(w)) gaps.push(`${f} no longer compares "${w}": take it off tools/game/proofwords.json`);
  }
  for (const [f, list] of Object.entries(now)) for (const w of list) if (!pins[f]?.includes(w)) gaps.push(`${f} compares "${w}" from text.json: add it to tools/game/proofwords.json`);
  return gaps;
}
export const proofSources = () => Object.fromEntries(readdirSync(PLAY).filter(f => /^Proof\w+\.cs$/.test(f)).map(f => [f, readFileSync(join(PLAY, f), 'utf8')]));

if (import.meta.main) {
  const t0 = performance.now(), body = readFileSync(TEXT), json = JSON.parse(body.toString('utf8')), r = TextFile.safeParse(json);
  if (!r.success) { console.error(`text.json fails its schema:\n${r.error.message}`); process.exit(1); }
  const gaps = wordGaps(JSON.parse(readFileSync(PINS, 'utf8')).words, proofSources(), wordsOf(json));
  if (gaps.length) { console.error(gaps.join('\n')); process.exit(1); }
  const test = Bun.spawnSync([process.execPath, 'test', 'contracts/missions/handover/text.test.ts'], { cwd: ROOT, stdout: 'pipe', stderr: 'pipe' });
  if (test.exitCode !== 0) { console.error(test.stderr.toString().split('\n').slice(-30).join('\n')); process.exit(1); }
  const dir = join(ROOT, 'unity/Assets/StreamingAssets/sim'); mkdirSync(dir, { recursive: true }); writeFileSync(join(dir, 'text.json'), body);
  console.log(`words: schema, text.test.ts and the proofs' words pass; text.json into StreamingAssets/sim in ${((performance.now() - t0) / 1000).toFixed(1)} s`);
}
