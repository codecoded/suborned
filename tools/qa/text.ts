// The player text the checks compare the engine against (contracts/missions/handover/text.json, Fable's): which fail codes have card text,
// which fail-camera captions exist, and which log codes reach the feed as a beat (fate "beat": a line on screen, brief 014).
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import type { TextTables } from './types';

export function textTables(json: any): TextTables {
  const fails = new Set<string>(Object.keys(json?.briefing?.fails ?? {}));
  const endFails = new Set<string>(Object.keys(json?.less?.end?.fail ?? {}));
  const failcam = (json?.less?.failcam?.caption ?? {}) as Record<string, Record<string, string>>;
  const beats = new Set<string>();
  for (const [code, card] of Object.entries<any>(json?.cards?.items ?? {})) if (card?.fate === 'beat') beats.add(code);
  return { fails, endFails, failcam, beats };
}

/** The repo's text (a root holding contracts/), or a text.json path (a built player's StreamingAssets/sim/text.json). */
export function loadText(rootOrFile: string): TextTables {
  const f = rootOrFile.endsWith('.json') ? rootOrFile : join(rootOrFile, 'contracts/missions/handover/text.json');
  return textTables(JSON.parse(readFileSync(f, 'utf8')));
}
