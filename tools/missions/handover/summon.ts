// The lover summons' window (Sky 2026-10-15: Inez and the door guard are lovers; Motive briefs/011/motive-lovers.md; rulebook `summon`):
// the mission names who walks between which destinations (mission.json windows, rule "summon"); the rules say how long he stays and
// how far he will walk; the engine says how far the walk is (its grid route at its pace). One reader for the site's cast
// (tools/sim/site.ts), M-2 (overlay.ts) and the tests, so none of them restates a number.
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { build, engineAt, env } from '../check/engine';

const ROOT = resolve(import.meta.dir, '../../..');
type Win = { id: string; rule: string; walk: { from: string; to: string } };
type M = { destinations: { name: string; at: number[] }[]; windows?: Win[] };

/** The rulebook's summon values: stay, walk maximum (s), walking pace (cm/s, cell.speed). */
export function summonRules(file = resolve(ROOT, 'contracts/rules/rulebook.json')) {
  const r = JSON.parse(readFileSync(file, 'utf8')), v = (k: string) => r.values?.[k]?.v as number | undefined, ref = (x: unknown) => (typeof x === 'string' && x.startsWith('v:') ? v(x.slice(2)) : undefined);
  return { stayS: ref(r.summon?.stay), walkMaxS: ref(r.summon?.walkMax), paceCmS: ref(r.cell?.speed) };
}

/** The summoned lover's walk between two mission destinations, in seconds, as the engine walks it: the grid route the engine plays at
 *  its walking pace (sim CLI `probe`, the working tree's engine and site). One number for the engine, M-2 and the tests (Aster, Keel
 *  2026-10-15: the engine is the single source). Undefined when there is no dotnet, or the probe doesn't measure that pair. */
const probed = new Map<string, number>();
let probeRead = false;
export function engineWalkS(from: string, to: string): number | undefined {
  if (!probeRead) {
    probeRead = true;
    if (Bun.which('dotnet')) {
      const dll = build(engineAt('tree')), r = Bun.spawnSync(['dotnet', dll, 'probe'], { cwd: ROOT, env, stdout: 'pipe', stderr: 'pipe' });
      for (const l of r.stdout.toString().split('\n')) { const x = l.match(/^d:(.+?) → d:(.+?): [\d.]+ m ([\d.]+) s$/); if (x) probed.set(`${x[1]}|${x[2]}`, Number(x[3])); }
    }
  }
  return probed.get(`${from}|${to}`) ?? probed.get(`${to}|${from}`);
}

/** The summons' limits as castRoles' `summon` option (M-2 and the site cast alike): the rulebook's walk maximum, and the engine's walk
 *  between the window's destinations. */
export function summonOpts(_plan?: unknown, _d?: unknown, _civ?: unknown, m?: M) {
  const sr = summonRules();
  return { ...(sr.walkMaxS !== undefined ? { maxWalkS: sr.walkMaxS } : {}), walkS: (a: string, b: string) => (m?.windows?.some(x => (x.walk.from === a && x.walk.to === b) || (x.walk.from === b && x.walk.to === a)) ? engineWalkS(a, b) : undefined) };
}
