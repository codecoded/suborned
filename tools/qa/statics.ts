// Brief 060 §3 (Splice): the static checks a QA pass runs beside the plays, so one report says what the world gets wrong as well as what the plays do:
//   the plan checker (bun run plan), the world rules on the generated world (the street, crossings, road finish, KR-2 kerb returns and KR-3 "a kerb stands only where pavement meets carriageway": no
//   stone across a lay-by's mouth) and the geometry audit against its ratchet (bun run geometry --check: G-1 the floor, G-2 the walk, G-3 sight, G-1d the legs people walk).
// Nothing here fails a pass: a failing rule is a finding with its id, its owner's name where the rule says one, and the change since the last pass (new, fixed). Read-only: it writes nothing.
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { check } from '../plan/checks';
import { civic } from '../plan/civic';
import { derive, type Plan } from '../plan/plan';
import { worldChecks } from '../plan/world-rules';
import { ROOT } from '../missions/check/engine';

export type Static = { id: string; group: string; ok: boolean; value: string; detail?: string; /** what the rule counts against its ratchet (geometry only) */ ratchet?: number };
export type Statics = { seconds: number; checks: number; failed: Static[]; /** informational rows worth a line in every report */ info: Static[]; geometry: Static[] };

const INFO = new Set(['KR-2/n', 'RF-3/n', 'KR-3', 'KR-2', 'KR-1']);
const json = (p: string) => JSON.parse(readFileSync(join(ROOT, p), 'utf8'));

export function runStatics(say: (m: string) => void = () => {}): Statics {
  const t0 = performance.now(), plan: Plan = json('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, json(plan.civic.block));
  const rows: Static[] = [];
  for (const c of check(plan, d, civ).checks) rows.push({ id: c.id, group: c.group, ok: c.ok, value: String(c.value).slice(0, 160), ...(c.detail ? { detail: c.detail.slice(0, 600) } : {}) });
  say('qa: plan checks done');
  for (const c of worldChecks(plan, d, civ)) rows.push({ id: c.id, group: c.group, ok: c.ok, value: String(c.value).slice(0, 160), ...(c.detail ? { detail: c.detail.slice(0, 600) } : {}) });
  say('qa: world rules done');
  // the geometry audit reads the world export on disk (bun run world); `--check` prints one line per rule and exits 1 over its ratchet, which is a finding here, not a failure
  const geometry: Static[] = [];
  const r = Bun.spawnSync(['bun', 'tools/geometry/cli.ts', '--check'], { cwd: ROOT, stdout: 'pipe', stderr: 'pipe' });
  for (const l of r.stdout.toString().split('\n')) {
    const m = /^(ok  |FAIL) (\S+) (\d+) (\S+) \(ratchet (\d+)|^(ok  |FAIL) (\S+) (\d+) (\S+) \(no ratchet/.exec(l); if (!m) continue;
    const ok = (m[1] ?? m[6])!.trim() === 'ok', id = (m[2] ?? m[7])!, n = Number(m[3] ?? m[8]), unit = (m[4] ?? m[9])!;
    geometry.push({ id, group: 'Geometry', ok, value: `${n} ${unit}`, ...(m[5] !== undefined ? { ratchet: Number(m[5]) } : {}) });
  }
  if (!geometry.length) geometry.push({ id: 'geometry', group: 'Geometry', ok: false, value: 'no output', detail: (r.stderr.toString() || r.stdout.toString()).slice(0, 300) });
  say('qa: geometry done');
  const all = [...rows, ...geometry];
  return { seconds: (performance.now() - t0) / 1000, checks: all.length, failed: all.filter(x => !x.ok), info: rows.filter(x => INFO.has(x.id)), geometry };
}
