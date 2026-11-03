// The proof pictures the HUD budget judges and Glyph reviews at 2x (Splice, 2026-10-26; briefs/067/splice.md items 3 and follow-up 2). tools/game/proofset.json names them by section of
// the player's proof (r059: brief 059's rival car, Watch, Drive and Take; r067: Sky's notes on 0.2.122, the square, cones, camera, locked ways, Board and Drive, conversations, reticle). Three uses:
//   check   <proofdir> [--size 1920x1080]   each picture is there with its HUD layer (<name>.hud.png), manifest and parts, at that size; exit 1 if one is not
//   budget  <proofdir>                      the 1x pictures through tools/hud-budget (what `bun run batch` does in its budget step, judgeProofBudget below); exit 1 over budget
//   crops   <proof2160dir> [--out dir]      the 2160 proof (-gameProof -capture-size 3840x2160) cut for the 2x review: each frame at 1/4 for where things sit, its middle at its own pixels, and
//                                           each HUD part that is not standing furniture (the roster, the scope, the keys, the history) at its own pixels, the standing ones too where
//                                           the picture is of them (`crop` in the set file): PNGs and one html sheet beside them
// Every set the folder has pictures of is run (a set with none is an older build's: said, skipped); --set names one.
// usage: bun tools/game/proofset.ts <check|budget|crops> <dir> [--set r059|r067] [--size WxH] [--out dir]
import { copyFileSync, existsSync, mkdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { basename, join, resolve } from 'node:path';
import { z } from 'zod';
import { fromParts, type Manifest } from '../hud-budget/budget';
import { decodePng, encodePng, type Image } from '../hud-budget/png';
import { pngFileSize } from './shots';

export const ROOT = resolve(import.meta.dir, '../..');
export const PROOFSET_FILE = join(ROOT, 'tools/game/proofset.json');

const Frame = z.strictObject({ name: z.string().regex(/^[a-z0-9][a-z0-9-]*$/, 'kebab-case'), what: z.string().min(10), optional: z.boolean().optional(), crop: z.array(z.string().min(1)).min(1).optional() });
export const ProofSet = z.strictObject({
  schema: z.literal('suborned.proofset/1'), note: z.string(),
  sets: z.record(z.string(), z.strictObject({ section: z.string(), source: z.string(), frames: z.array(Frame).min(1) })),
});
export type Frame = z.infer<typeof Frame>;
export type ProofSet = z.infer<typeof ProofSet>;

export function loadProofSet(file = PROOFSET_FILE): ProofSet {
  const p = ProofSet.safeParse(JSON.parse(readFileSync(file, 'utf8')));
  if (!p.success) throw new Error(`${file}: ${p.error.issues.map(i => `${i.path.join('.')}: ${i.message}`).join('; ')}`);
  for (const [k, s] of Object.entries(p.data.sets)) { const names = s.frames.map(f => f.name), dup = names.find((n, i) => names.indexOf(n) !== i); if (dup) throw new Error(`${file}: set ${k} lists ${dup} twice`); }
  return p.data;
}

export type Located = { frame: Frame; png: string; hud?: string; manifest?: string; parts?: string; size?: [number, number] };
export type Judged = {
  found: Located[];
  /** pictures the set needs and the folder has not */
  missing: string[];
  /** optional pictures the folder has not (not a fault) */
  optionalMissing: string[];
  /** pictures without the HUD layer or the manifest the budget reads */
  incomplete: string[];
  /** pictures not at the size asked for */
  wrongSize: string[];
};

/** Where each picture of `frames` is in `dir`, and what is wrong with it. `size`: the capture size every picture must have (the 1x budget is defined at 1920x1080, the 2x review at 3840x2160). */
export function locate(dir: string, frames: Frame[], size?: [number, number]): Judged {
  const out: Judged = { found: [], missing: [], optionalMissing: [], incomplete: [], wrongSize: [] };
  for (const frame of frames) {
    const png = join(dir, `${frame.name}.png`);
    if (!existsSync(png)) { (frame.optional ? out.optionalMissing : out.missing).push(frame.name); continue; }
    const at = (ext: string) => (existsSync(join(dir, `${frame.name}${ext}`)) ? join(dir, `${frame.name}${ext}`) : undefined);
    const l: Located = { frame, png, hud: at('.hud.png'), manifest: at('.manifest.json'), parts: at('.parts.json'), size: pngFileSize(png) };
    if (!l.hud || !l.manifest) out.incomplete.push(`${frame.name}: no ${[!l.hud && '.hud.png', !l.manifest && '.manifest.json'].filter(Boolean).join(' or ')} beside it`);
    if (size && (!l.size || l.size[0] !== size[0] || l.size[1] !== size[1])) out.wrongSize.push(`${frame.name}: ${l.size ? l.size.join('x') : 'not a PNG'}, wanted ${size.join('x')}`);
    out.found.push(l);
  }
  return out;
}

/** The faults of a judged set, one line each; empty when it is whole. */
export const faults = (j: Judged): string[] => [...j.missing.map(n => `${n}: not taken by the proof`), ...j.incomplete, ...j.wrongSize];

// ---------------------------------------------------------------- the HUD budget (1x)

export type BudgetRun = (dir: string) => { code: number; out: string };
const runBudget: BudgetRun = dir => { const r = Bun.spawnSync(['bun', join(ROOT, 'tools/hud-budget/cli.ts'), dir], { cwd: ROOT, stdout: 'pipe', stderr: 'pipe' }); return { code: r.exitCode, out: r.stdout.toString() + r.stderr.toString() }; };

/** Copy each picture with its HUD layer and manifest (and the refusal pops its parts name, added to the manifest) into `out` (emptied first): the folder tools/hud-budget judges one picture at a time. */
export function stageForBudget(found: Located[], out: string): void {
  rmSync(out, { recursive: true, force: true }); mkdirSync(out, { recursive: true });
  for (const f of found) {
    for (const src of [f.png, f.hud]) if (src) copyFileSync(src, join(out, basename(src)));
    if (f.manifest) { // the manifest as the build wrote it, plus what its parts name that it does not (tools/hud-budget fromParts): the refusal pops (`pop-N`, one line of four words), the done chips (`gain-chip-N`, glyph-budget §3) and the clock bar (`pill`, one row at play size)
      const m = JSON.parse(readFileSync(f.manifest, 'utf8')) as Manifest, pops = f.parts && f.size ? fromParts(JSON.parse(readFileSync(f.parts, 'utf8')) as Record<string, number[]>, m, { w: f.size[0], h: f.size[1] }) : [];
      if (pops.length) writeFileSync(join(out, basename(f.manifest)), JSON.stringify({ ...m, elements: [...m.elements, ...pops] }, null, 1) + '\n'); else copyFileSync(f.manifest, join(out, basename(f.manifest)));
    }
  }
}

export type ProofBudget = { ok: boolean; detail: string; problems: string[]; judged: number; pass: number };
/** The batch's budget step for a proof set: every picture of the set that the proof took is at 1920x1080, has its HUD layer and manifest, and is within the HUD budget. `stage`: a scratch folder. */
export function judgeProofBudget(dir: string, set: string, stage: string, run: BudgetRun = runBudget, file = PROOFSET_FILE): ProofBudget {
  const s = loadProofSet(file).sets[set]; if (!s) throw new Error(`${file} has no set ${set}`);
  const j = locate(dir, s.frames, [1920, 1080]), bad = faults(j);
  if (bad.length) return { ok: false, detail: `proof set ${set}: ${bad.length} fault${bad.length === 1 ? '' : 's'}`, problems: bad, judged: 0, pass: 0 };
  stageForBudget(j.found, stage);
  const r = run(stage), lines = r.out.split('\n'), pass = lines.filter(l => l.startsWith('PASS')).length, over = lines.filter(l => l.startsWith('FAIL'));
  // a tool that fails with no FAIL line (no PNG found, a manifest it cannot read) is a failure too
  const problems = [...over.map(l => l.slice(0, 200)), ...(r.code !== 0 && !over.length ? [`tools/hud-budget exited ${r.code}: ${r.out.trim().split('\n').slice(-2).join(' / ').slice(0, 200)}`] : [])];
  const note = j.optionalMissing.length ? `; not taken this run: ${j.optionalMissing.join(', ')}` : '';
  return { ok: !problems.length && r.code === 0, detail: `proof set ${set}: ${pass} of ${j.found.length} pictures within budget${note}`, problems, judged: j.found.length, pass };
}

// ---------------------------------------------------------------- the 2x crops

/** Parts that stand on every frame (the roster and its rows, the scope, the keys, the history): not what a frame is taken to show. */
export const STANDING = /^(roster|portrait-\d+|roster-(weapon|dots)-\d+|keys|scope|history|history-key)$/;
export type Rect = { x: number; y: number; w: number; h: number };

/** The crops of one picture: its middle (the camera is put on the subject, so this is what the frame is of) and each part of `parts` ([x, y, w, h] at the picture's own pixels) that is not standing furniture, padded and held to the picture.
 *  `also`: standing parts this picture is of (the roster, for a picture of the portrait pulse), cropped here too. */
export function cropRects(parts: Record<string, number[]>, size: [number, number], o: { pad?: number; middle?: [number, number]; min?: number; also?: readonly string[] } = {}): { name: string; rect: Rect }[] {
  const [W, H] = size, pad = o.pad ?? Math.round(W / 160), [mw, mh] = o.middle ?? [Math.round(W / 3), Math.round(H / 3)], min = o.min ?? Math.round(W / 80);
  const clamp = (r: Rect): Rect => { const x = Math.max(0, Math.min(W - 1, Math.floor(r.x))), y = Math.max(0, Math.min(H - 1, Math.floor(r.y))); return { x, y, w: Math.max(1, Math.min(W - x, Math.ceil(r.w))), h: Math.max(1, Math.min(H - y, Math.ceil(r.h))) }; };
  const out: { name: string; rect: Rect }[] = [{ name: 'middle', rect: clamp({ x: (W - mw) / 2, y: (H - mh) / 2, w: mw, h: mh }) }];
  for (const [name, p] of Object.entries(parts)) {
    if ((STANDING.test(name) && !o.also?.includes(name)) || p.length !== 4 || p.some(v => !Number.isFinite(v))) continue;
    const [x, y, w, h] = p as [number, number, number, number];
    if (w < min || h < min || x + w < 0 || y + h < 0 || x > W || y > H) continue; // off screen (a bubble above the top edge) or too small to be a thing
    out.push({ name, rect: clamp({ x: x - pad, y: y - pad, w: w + 2 * pad, h: h + 2 * pad }) });
  }
  return out;
}

/** A picture at 1/k of its size, each pixel the mean of a k x k block (the frame as a thumbnail, for where the crops sit in it). */
export function downscale(im: Image, k: number): Image {
  const w = Math.floor(im.w / k), h = Math.floor(im.h / k), data = new Uint8Array(w * h * 4), n = k * k;
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    const sum = [0, 0, 0, 0];
    for (let dy = 0; dy < k; dy++) for (let dx = 0; dx < k; dx++) { const o = ((y * k + dy) * im.w + x * k + dx) * 4; for (let c = 0; c < 4; c++) sum[c]! += im.data[o + c]!; }
    for (let c = 0; c < 4; c++) data[(y * w + x) * 4 + c] = Math.round(sum[c]! / n);
  }
  return { w, h, data };
}

export function cropImage(im: Image, r: Rect): Image {
  const data = new Uint8Array(r.w * r.h * 4);
  for (let y = 0; y < r.h; y++) data.set(im.data.subarray(((r.y + y) * im.w + r.x) * 4, ((r.y + y) * im.w + r.x + r.w) * 4), y * r.w * 4);
  return { w: r.w, h: r.h, data };
}

const esc = (s: string) => s.replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' })[c]!);

/** Cut the 2160 proof's pictures of a set into crops and one html sheet that shows them (the PNGs beside it, linked by name: at their own pixels they run to 50 MB for a set, too much to embed). Returns the sheet's path and what it holds. */
export function writeCrops(dir: string, set: string, out: string, o: { size?: [number, number]; file?: string } = {}): { sheet: string; frames: number; crops: number; problems: string[] } {
  const file = o.file ?? PROOFSET_FILE, s = loadProofSet(file).sets[set]; if (!s) throw new Error(`${file} has no set ${set}`);
  const j = locate(dir, s.frames, o.size ?? [3840, 2160]), problems = faults(j);
  mkdirSync(out, { recursive: true });
  let crops = 0;
  const cards = j.found.map(f => {
    const im = decodePng(readFileSync(f.png)), parts = f.parts ? JSON.parse(readFileSync(f.parts, 'utf8')) as Record<string, number[]> : {};
    const state = f.manifest ? (JSON.parse(readFileSync(f.manifest, 'utf8')) as { state?: string }).state ?? '' : '';
    const tile = (name: string, png: Uint8Array, shown: number, caption: string) => { const file = `${f.frame.name}--${name}.png`; writeFileSync(join(out, file), png); crops++; return `<figure><img src="${file}" width="${shown}" alt="${esc(name)}"><figcaption>${esc(name)} · ${esc(caption)}</figcaption></figure>`; };
    const thumb = downscale(im, 4); // the whole frame at a quarter of its size, first: where the crops sit
    const tiles = tile('frame', encodePng(thumb), thumb.w, `the whole picture at 1/4 (${im.w}×${im.h})`) + cropRects(parts, [im.w, im.h], { also: f.frame.crop }).map(c => {
      const cut = cropImage(im, c.rect); // at the picture's own pixels: the 2x review is a look at these
      return tile(c.name, encodePng(cut), Math.min(cut.w, 640), `${c.rect.w}×${c.rect.h} at ${c.rect.x},${c.rect.y}`);
    }).join('');
    return `<section><h2>${esc(f.frame.name)}${state ? ` <small>${esc(state)}</small>` : ''}</h2><p>${esc(f.frame.what)}</p><div class="row">${tiles}</div><p class="path">${esc(f.png)}</p></section>`;
  });
  const html = `<!doctype html><meta charset="utf-8"><title>2x crops · ${esc(set)}</title><style>body{font:14px/1.4 system-ui;margin:24px;background:#fbf7ef;color:#1d2b2b}h2{margin:28px 0 4px}h2 small{font-weight:400;color:#6b7a78}.row{display:flex;flex-wrap:wrap;gap:12px;align-items:flex-start}figure{margin:0}img{display:block;border:1px solid #c9c2b2;image-rendering:pixelated}figcaption,.path{font-size:12px;color:#6b7a78}</style><h1>2x crops · set ${esc(set)} · ${j.found.length} pictures</h1><p>${esc(loadProofSet(file).note)}</p>${problems.length ? `<p><b>Faults:</b> ${problems.map(esc).join('; ')}</p>` : ''}${cards.join('')}`;
  const sheet = join(out, `crops-${set}.html`); writeFileSync(sheet, html);
  return { sheet, frames: j.found.length, crops, problems };
}

/** Which sets a command runs on `dir`: the one asked for with --set (it must be in the file), or every set the folder has at least one picture of. A set with no picture in the folder is
 *  an older build's: said and skipped, not a fault (the batch does the same by the proof's sections). */
export function pickSets(dir: string, requested?: string, file = PROOFSET_FILE): { run: string[]; skipped: string[] } {
  const sets = loadProofSet(file).sets;
  if (requested) { if (!sets[requested]) throw new Error(`no set ${requested} in ${file} (${Object.keys(sets).join(', ')})`); return { run: [requested], skipped: [] }; }
  const has = (n: string) => sets[n]!.frames.some(f => existsSync(join(dir, `${f.name}.png`)));
  const names = Object.keys(sets);
  return { run: names.filter(has), skipped: names.filter(n => !has(n)) };
}

if (import.meta.main) {
  const args = process.argv.slice(2), at = (k: string) => { const i = args.indexOf(k); return i >= 0 ? args[i + 1] : undefined; };
  const cmd = args[0], dir = args[1] && !args[1].startsWith('--') ? resolve(args[1]) : undefined;
  if (!['check', 'budget', 'crops'].includes(cmd ?? '') || !dir) { console.error('usage: bun tools/game/proofset.ts <check|budget|crops> <proof dir> [--set r059|r067: default every set the folder has pictures of] [--size WxH] [--out dir]'); process.exit(2); }
  let picked: ReturnType<typeof pickSets>;
  try { picked = pickSets(dir, at('--set')); } catch (e) { console.error((e as Error).message); process.exit(2); }
  for (const s of picked.skipped) console.log(`--    set ${s}: no picture of it in ${dir} (an older build)`);
  if (!picked.run.length) { console.log(`FAIL  no picture of any set (${Object.keys(loadProofSet().sets).join(', ')}) in ${dir}`); process.exit(1); }
  let code = 0;
  for (const set of picked.run) {
    const proof = loadProofSet().sets[set]!;
    if (cmd === 'check') {
      const sz = (at('--size') ?? '1920x1080').split('x').map(Number) as [number, number], j = locate(dir, proof.frames, sz), bad = faults(j);
      for (const f of j.found) console.log(`ok    ${f.frame.name}  ${f.size?.join('x') ?? '?'}  ${f.hud && f.manifest ? 'HUD layer and manifest' : 'NO HUD layer or manifest'}`);
      for (const n of j.optionalMissing) console.log(`--    ${n}  (optional: not taken this run)`);
      for (const b of bad) console.log(`FAIL  ${b}`);
      console.log(`${j.found.length} of ${proof.frames.length} pictures of set ${set} in ${dir}${bad.length ? `; ${bad.length} fault${bad.length === 1 ? '' : 's'}` : ''}`);
      if (bad.length) code = 1;
    } else if (cmd === 'budget') {
      const r = judgeProofBudget(dir, set, join(ROOT, 'out/scratch/splice/proofset', set));
      console.log(r.detail); for (const p of r.problems) console.log(`FAIL  ${p}`);
      if (!r.ok) code = 1;
    } else {
      const r = writeCrops(dir, set, resolve(at('--out') ?? join(ROOT, 'out/scratch/splice/proofset', `crops-${set}`)));
      for (const p of r.problems) console.log(`FAIL  ${p}`);
      console.log(`${r.crops} crops of ${r.frames} pictures → ${r.sheet}`);
      if (r.problems.length) code = 1;
    }
  }
  process.exit(code);
}
