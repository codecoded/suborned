// The walk file against the world export it was made from (tools/game/walkfile.ts; briefs/067/splice.md J). Run: bun test tools/game/walkfile.test.ts
import { expect, test } from 'bun:test';
import { createHash } from 'node:crypto';
import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { walkFileState, WALK_FILE, WORLD_FILE } from './walkfile';

const sha = (t: string) => createHash('sha256').update(t).digest('hex');
function root(o: { world?: string; walk?: string }): string {
  const r = mkdtempSync(join(tmpdir(), 'walkfile-'));
  mkdirSync(join(r, 'out/world'), { recursive: true });
  if (o.world !== undefined) writeFileSync(join(r, WORLD_FILE), o.world);
  if (o.walk !== undefined) writeFileSync(join(r, WALK_FILE), o.walk);
  return r;
}

test('a walk file made from this export is fresh, and says which', () => {
  const r = root({ world: '{"civic":[1]}', walk: JSON.stringify({ schema: 'suborned.walk/1', world: sha('{"civic":[1]}') }) });
  try { expect(walkFileState(r)).toEqual({ fresh: true, why: `made from world ${sha('{"civic":[1]}').slice(0, 12)}` }); } finally { rmSync(r, { recursive: true, force: true }); }
});

test('an export that changed since the walk file was made (a prop gone from the block) leaves it stale, naming both', () => {
  const r = root({ world: '{"civic":[]}', walk: JSON.stringify({ world: sha('{"civic":[1]}') }) });
  try { const s = walkFileState(r); expect(s.fresh).toBe(false); expect(s.why).toBe(`made from world ${sha('{"civic":[1]}').slice(0, 12)}, the export is ${sha('{"civic":[]}').slice(0, 12)}`); } finally { rmSync(r, { recursive: true, force: true }); }
});

test('no export, no walk file, or a walk file that is not JSON or names no world is not fresh', () => {
  for (const [o, why] of [[{}, 'no world export'], [{ world: '{}' }, 'no walk file'], [{ world: '{}', walk: 'nope' }, 'the walk file is not JSON'], [{ world: '{}', walk: '{}' }, `made from world nothing, the export is ${sha('{}').slice(0, 12)}`]] as const) {
    const r = root(o); try { expect(walkFileState(r)).toEqual({ fresh: false, why }); } finally { rmSync(r, { recursive: true, force: true }); }
  }
});
