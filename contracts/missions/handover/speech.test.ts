// Brief 102 (Sky 2026-11-08): the agents go quiet in play; their voices stay only in scenes with people, and the world's people keep talking.
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { SCENE_ROWS, SILENT, speaks } from './speech.schema.ts';
import { AGENTS, lineKey, TextFile } from './text.schema.ts';

const t = TextFile.parse(JSON.parse(readFileSync(join(import.meta.dir, 'text.json'), 'utf8')));

describe('brief 102: who speaks in play', () => {
  test('brief 102: the agents speak only in scenes with people; every other agent row is silent with its reason, and nobody else\'s is', () => {
    const agent = (e: { speaker: string }) => (AGENTS as readonly string[]).includes(e.speaker);
    const bad: string[] = [];
    for (const e of t.dialogue) {
      if (e.parked) continue;
      const k = lineKey(e), scene = (SCENE_ROWS as readonly string[]).includes(k);
      if (!agent(e) && e.silent) bad.push(`${k}: the world's people keep talking`);
      if (agent(e) && scene && e.silent) bad.push(`${k}: a scene line, so it speaks`);
      if (agent(e) && !scene && !e.pending && !e.silent) bad.push(`${k}: an agent outside a scene goes quiet`);
      if (e.silent && !e.silent.startsWith(`${SILENT}; `)) bad.push(`${k}: a silent row says why after the rule`);
    }
    expect(bad).toEqual([]);
    for (const k of SCENE_ROWS) expect(t.dialogue.some((e) => lineKey(e) === k), k).toBe(true);
    expect(t.dialogue.filter((e) => agent(e) && speaks(e)).map(lineKey).sort()).toEqual(SCENE_ROWS.filter((k) => !k.endsWith('/named')).sort());
  });
});
