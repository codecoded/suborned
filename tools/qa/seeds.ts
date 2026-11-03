// Brief 060 (Suture): the mutation seeds. The Handover's route scripts (contracts/missions/handover/routes, Waymark's; their expected
// outcome and kind from routes.json) and Sky's saved sessions: the handover-*.script.json files over 8 KB in the built player's play folder
// (the smaller ones are proof sessions, unless Sky played them). A session that ended (its record's outcome is not "none") plays on to the mission's close, so a
// changed engine can finish it; a session Sky left mid-run keeps its own end.
// Most of those files are automation, not Sky (2026-10-25: of 176, 121 are one tp-long proof shape and ~40 late-row replays). Every one stays a
// mutation seed, since automation still exercises the rules, tagged `by` sky or automation with the real-time test (human-measure.ts skySessions):
// only Sky's count as "an order Sky gave" (Aster 2026-10-25).
// The 8 KB floor keeps out automation's tiny proof sessions only; a session of Sky's is a seed at any size.
// The player's play folder is Application.persistentDataPath/play, and it has moved with the player's names: DefaultCompany/The Suborned since
// the player of 2026-10-24 04:01 (company and product), the Unity template's bundle id before it (17 to 24 October), DefaultCompany/unity for two
// sessions on 17 October. SESSIONS_DIR is the folder of today's player, and reading it reads all three (sessionFolders), so Sky's sessions on
// either side of a move stay seeds and stay measured (Suture 2026-10-26: from 04:01 on 24 October the pass and the measure read a folder the
// player no longer wrote: 2,960 sessions, 48 seeds, 9 of them Sky's, were never played).
import { existsSync, readdirSync, readFileSync, statSync } from 'node:fs';
import { homedir } from 'node:os';
import { basename, join } from 'node:path';
import type { Script, Seed } from './types';
import { readSessions, skySessions } from './human-measure';

export const ROUTES_DIR = 'contracts/missions/handover/routes';
const APP_SUPPORT = join(homedir(), 'Library/Application Support');
/** Today's player's play folder (Application.persistentDataPath/play). */
export const SESSIONS_DIR = join(APP_SUPPORT, 'DefaultCompany/The Suborned/play');
/** The folders earlier players saved in, newest first. */
export const EARLIER_SESSIONS_DIRS = [join(APP_SUPPORT, 'com.Unity-Technologies.com.unity.template.urp-blank/play'), join(APP_SUPPORT, 'DefaultCompany/unity/play')];
/** The folders a sessions folder stands for: today's player's brings the ones earlier players saved in; any other folder is itself. */
export const sessionFolders = (dir: string): string[] => (dir === SESSIONS_DIR ? [SESSIONS_DIR, ...EARLIER_SESSIONS_DIRS] : [dir]);
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

/** The session seeds of a folder (sessionFolders: the player's own brings its earlier ones) or of a list of folders, in time order (a seed's id is
 *  its save time, so `--max-sessions` keeps the newest); a name in two folders is one seed, the first folder's. */
export function sessionSeeds(dir: string | string[] = SESSIONS_DIR, minBytes = SESSION_MIN_BYTES): Seed[] {
  const dirs = (typeof dir === 'string' ? sessionFolders(dir) : dir).filter(d => existsSync(d)); if (!dirs.length) return [];
  const out: Seed[] = [], ids = new Set<string>(), sky = new Set(skySessions(readSessions(dirs)).sky.map(s => s.file)); // whose: over every folder at once (a Continue may copy across a move)
  for (const d of dirs) for (const f of readdirSync(d).filter(f => /^handover-.*\.script\.json$/.test(f)).sort()) {
    const p = join(d, f); if (statSync(p).size < minBytes && !sky.has(p)) continue; // the size floor is for automation: Sky's own play is a seed at any size (Aster 2026-10-25)
    let script: Script; try { script = JSON.parse(readFileSync(p, 'utf8')) as Script; } catch { continue; }
    if (script.schema !== 'suborned.sim.script/1' || script.mission !== 'handover') continue;
    const ended = !!script.expect?.outcome && script.expect.outcome !== 'none';
    if (ended) script = { ...script, until: 0 };
    const id = 's-' + basename(f, '.script.json').replace(/^handover-/, ''); if (ids.has(id)) continue; ids.add(id);
    out.push({ id, kind: 'session', file: p, script, routeKind: 'session', by: sky.has(p) ? 'sky' : 'automation' });
  }
  return out.sort((a, b) => a.id.localeCompare(b.id));
}
