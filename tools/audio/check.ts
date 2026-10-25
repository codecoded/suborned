// The audio checker: contracts/audio/cues.json against the clips on disk and clips.json. Returns the failures (empty = ok).
import { createHash } from 'node:crypto';
import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { Cues } from '../../contracts/audio/cues.schema.ts';
import { root } from './synth.ts';

export function check(): string[] {
  const errs: string[] = [], j = (p: string) => JSON.parse(readFileSync(join(root, p), 'utf8'));
  const parsed = Cues.safeParse(j('contracts/audio/cues.json'));
  if (!parsed.success) return parsed.error.issues.map(i => `cues.json ${i.path.join('.')}: ${i.message}`);
  const cues = parsed.data, clips = j('contracts/audio/clips.json').clips as Record<string, any>;
  const byFile = new Map(Object.entries(clips).map(([k, v]) => [v.file as string, [k, v] as const]));
  const ids = new Set<string>();
  for (const [name, c] of Object.entries(clips)) {
    const p = join(root, c.path);
    if (!existsSync(p)) { errs.push(`${name}: ${c.path} missing`); continue; }
    if (createHash('sha256').update(readFileSync(p)).digest('hex') !== c.sha256) errs.push(`${name}: sha256 differs from clips.json (run bun tools/audio/cli.ts)`);
    if (!existsSync(p + '.meta')) errs.push(`${name}: no .meta (import settings)`);
    else { const m = readFileSync(p + '.meta', 'utf8'); if (!m.includes('forceToMono: 1') || !m.includes('compressionFormat: 0') || !m.includes(`3D: ${c.threeD ? 1 : 0}`)) errs.push(`${name}: .meta settings are not PCM, mono, 3D=${c.threeD}`); }
    if (c.peakDb > -1) errs.push(`${name}: peak ${c.peakDb} dBFS is over -1`);
    if (c.loop && c.seamRatio > 2) errs.push(`${name}: loop seam ratio ${c.seamRatio} (> 2: it will click)`);
  }
  const buses = new Set(Object.keys(cues.buses));
  const claimedCodes = new Map<string, string>();
  for (const e of [...cues.cues, ...cues.loops]) {
    if (ids.has(e.id)) errs.push(`${e.id}: duplicate id`); ids.add(e.id);
    if (!buses.has(e.bus)) errs.push(`${e.id}: bus ${e.bus} is not in buses`);
    const c = byFile.get(e.clip); if (!c) { errs.push(`${e.id}: clip ${e.clip} is not in clips.json`); continue; }
    if (e.spatial.blend > 0 && !c[1].threeD) errs.push(`${e.id}: 3D playback of a clip imported as 2D`);
    if ('on' in e) { const o = claimedCodes.get(e.on.log); if (o) errs.push(`${e.id}: log ${e.on.log} is already ${o}'s (one cue per meaning)`); claimedCodes.set(e.on.log, e.id); }
  }
  for (const cu of cues.cues) for (const d of cu.doesNotPlayOn) if (claimedCodes.has(d)) errs.push(`${cu.id}: says it does not play on ${d}, which another cue claims`);
  for (const [b, v] of Object.entries(cues.buses)) if (v.parent && !buses.has(v.parent)) errs.push(`bus ${b}: parent ${v.parent} unknown`);
  const made = new Set(cues.family.rows.filter(r => r.status === 'made').map(r => r.id));
  for (const cu of cues.cues) if (!made.has(cu.id)) errs.push(`${cu.id}: not marked made in the family table`);
  return errs;
}
