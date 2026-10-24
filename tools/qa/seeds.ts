// Brief 060 (Suture): the mutation seeds. The Handover's route scripts (contracts/missions/handover/routes, Waymark's; their expected
// outcome and kind from routes.json) and Sky's saved sessions: the handover-*.script.json files over 8 KB in the built player's play folder
// (the smaller ones are proof sessions, unless Sky played them). A session that ended (its record's outcome is not "none") plays on to the mission's close, so a
// changed engine can finish it; a session Sky left mid-run keeps its own end.
// Most of those files are automation, not Sky (2026-10-25: of 176, 121 are one tp-long proof shape and ~40 late-row replays). Every one stays a
// mutation seed, since automation still exercises the rules, tagged `by` sky or automation with the real-time test (human-measure.ts skySessions):
// only Sky's count as "an order Sky gave" (Aster 2026-10-25).
// The 8 KB floor keeps out automation's tiny proof sessions only; a session of Sky's is a seed at any size.
import { existsSync, readdirSync, readFileSync, statSync } from 'node:fs';
import { homedir } from 'node:os';
import { basename, join } from 'node:path';
import type { Script, Seed } from './types';
import { readSessions, skySessions } from './human-measure';

export const ROUTES_DIR = 'contracts/missions/handover/routes';
export const SESSIONS_DIR = join(homedir(), 'Library/Application Support/com.Unity-Technologies.com.unity.template.urp-blank/play');
export const SESSION_MIN_BYTES = 8 * 1024;

export function routeSeeds(root: string): Seed[] {
  const dir = join(root, ROUTES_DIR);
  const meta = new Map<string, any>();
  try { for (const s of JSON.parse(readFileSync(join(dir, 'routes.json'), 'utf8')).scripts ?? []) meta.set(s.file, s); } catch { /* no routes.json: no expectations */ }
  const out: Seed[] = [];
  for (const f of readdirSync(dir).filter(f => f.endsWith('.json')).sort()) {
    const text = readFileSync(join(dir, f), 'utf8'); if (!text.includes('suborned.sim.script/1')) continue;
    const m = meta.get(f);
    out.push({ id: f.replace(/\.json$/, ''), kind: 'route', file: join(dir, f), script: JSON.parse(text) as Script, expects: m?.expects, routeKind: m?.kind });
  }
  return out;
}

export function sessionSeeds(dir = SESSIONS_DIR, minBytes = SESSION_MIN_BYTES): Seed[] {
  if (!existsSync(dir)) return [];
  const out: Seed[] = [], sky = new Set(skySessions(readSessions([dir])).sky.map(s => s.file));
  for (const f of readdirSync(dir).filter(f => /^handover-.*\.script\.json$/.test(f)).sort()) {
    const p = join(dir, f); if (statSync(p).size < minBytes && !sky.has(p)) continue; // the size floor is for automation: Sky's own play is a seed at any size (Aster 2026-10-25)
    let script: Script; try { script = JSON.parse(readFileSync(p, 'utf8')) as Script; } catch { continue; }
    if (script.schema !== 'suborned.sim.script/1' || script.mission !== 'handover') continue;
    const ended = !!script.expect?.outcome && script.expect.outcome !== 'none';
    if (ended) script = { ...script, until: 0 };
    out.push({ id: 's-' + basename(f, '.script.json').replace(/^handover-/, ''), kind: 'session', file: p, script, routeKind: 'session', by: sky.has(p) ? 'sky' : 'automation' });
  }
  return out;
}
