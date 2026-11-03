// The audio cue file as the player reads it (Chime, brief 059; Splice, briefs/067/splice.md item 2). contracts/audio/cues.json (suborned.audio-cues/1) says which clip
// plays on which log code, at what volume, on which bus. The host (HostAudio.cs) reads it from StreamingAssets/sim/cues.json and, with no copy there, from the repo's
// contracts/ folder, and, with neither (a player on another Mac), from the values copied into its code (logged "audio cues: defaults"). tools/sim/unity.ts copies it into
// StreamingAssets/sim/ beside the text and the seats, after judging it here: host content, not an engine input, so the engine's inputs and hashes do not move.
import { existsSync } from 'node:fs';
import { join } from 'node:path';

export const CUES = 'contracts/audio/cues.json';
export const CUES_SCHEMA = 'contracts/audio/cues.schema.ts';
/** Where the host loads a clip from: Resources.Load<AudioClip>("Audio/rivals_coming") is <CLIP_ROOT>/Audio/rivals_coming.<ext>. */
export const CLIP_ROOT = 'unity/Assets/Suborned/Resources';
const CLIP_EXT = ['wav', 'ogg', 'mp3', 'aif', 'aiff'];

type Issue = { path: PropertyKey[]; message: string };
/** The zod schema's parse, as the module the sim step imports from the same source as the file (a commit's or the tree's) gives it. */
export type CuesParser = { safeParse(v: unknown): { success: true; data: unknown } | { success: false; error: { issues: Issue[] } } };
export type CuesJudged = { ok: boolean; problems: string[]; cues: number; loops: number; clips: string[] };

const depthOf = (v: unknown): number => Array.isArray(v) ? 1 + Math.max(0, ...v.map(depthOf)) : v && typeof v === 'object' ? 1 + Math.max(0, ...Object.values(v).map(depthOf)) : 0;

/** Judge the text of a cue file before it is shipped: it parses, nests within `maxDepth` (the host's JSON reader recurses per level), holds the schema, names only buses it
 *  has, and every clip it names is in the Unity project under `clipRoot` (a cue whose clip is missing would play nothing in the player and say so nowhere). The deeper
 *  cross-checks (clips.json hashes, import settings, one cue per meaning) are tools/audio/check.ts's, run by `bun test tools/audio`. */
export function judgeCues(text: string, schema: CuesParser, clipRoot: string, maxDepth: number): CuesJudged {
  const bad = (problems: string[]): CuesJudged => ({ ok: false, problems, cues: 0, loops: 0, clips: [] });
  let json: unknown;
  try { json = JSON.parse(text); } catch (e) { return bad([`${CUES} is not JSON: ${(e as Error).message}`]); }
  const d = depthOf(json);
  if (d > maxDepth) return bad([`${CUES} nests ${d} deep (> ${maxDepth})`]);
  const parsed = schema.safeParse(json);
  if (!parsed.success) return bad(parsed.error.issues.map(i => `${CUES} ${i.path.map(String).join('.')}: ${i.message}`));
  const c = parsed.data as { buses: Record<string, unknown>; cues: { id: string; clip: string; bus: string }[]; loops: { id: string; clip: string; bus: string }[] };
  const problems: string[] = [], clips = new Set<string>();
  for (const e of [...c.cues, ...c.loops]) {
    if (!(e.bus in c.buses)) problems.push(`${e.id}: bus ${e.bus} is not in buses`);
    clips.add(e.clip);
    if (!CLIP_EXT.some(x => existsSync(join(clipRoot, `${e.clip}.${x}`)))) problems.push(`${e.id}: clip ${e.clip} is not in ${clipRoot.replace(/^.*\/(unity\/)/, '$1')} (${CLIP_EXT.map(x => `${e.clip}.${x}`).join(', ')}): Resources.Load would find nothing`);
  }
  return { ok: !problems.length, problems, cues: c.cues.length, loops: c.loops.length, clips: [...clips].sort() };
}
