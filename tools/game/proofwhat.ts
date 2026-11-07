// What a change would run in `bun run batch`, read from the proof map alone (brief 098, Splice; CLAUDE.md "Delta only": a small change reaches Sky in about 5 minutes).
// The proof's sections (planProof), the batch's gated steps (stepGate) and the shots (shotsGate), as if everything had just passed on the tree before the change; and the
// paths the map cannot place, named: one of them still runs the whole proof and every step (the fail-safe), never silently.
// usage: bun tools/game/proofwhat.ts diff <from> <to> [--map <proofmap.json>]   a commit range (git diff --name-only; mission.json by the fields that moved)
//        bun tools/game/proofwhat.ts what-if <path>... [--map <file>]           these paths alone
//        bun tools/game/proofwhat.ts unplaced [--map <file>]                     every path of the tree's snapshot no row places (exit 1 if any)
import { resolve } from 'node:path';
import { GATED_STEPS, MISSION_FILE, ROOT, classify, emptyLedger, loadMap, mapHash, missionParts, planLines, planProof, sectionIds, shotsGate, snapshot, stepGate, unplacedIn, type Plan, type ProofMap } from './proofdelta';

export type WhatIf = { plan: Plan; steps: Record<string, { run: boolean; why: string }>; shots: { names: 'all' | string[]; why: string }; unplaced: string[] };
/** What a change to `paths` alone would run, as if every section, step and the shots had just passed on the tree before it. */
export function whatIfAll(m: ProofMap, paths: string[]): WhatIf {
  const l = emptyLedger(), now = { id: 'now', files: Object.fromEntries(paths.map(p => [p, 'now'])) };
  l.snapshots.s = Object.fromEntries(paths.map(p => [p, 'was']));
  for (const s of sectionIds(m)) l.sections[s] = { build: '0.2.N', commit: 'HEAD', at: '', snapshot: 's', map: mapHash(m, s) };
  for (const st of [...GATED_STEPS, 'shots']) l.steps[st] = { build: '0.2.N', commit: 'HEAD', snapshot: 's' };
  return { plan: planProof(m, now, l), steps: Object.fromEntries(GATED_STEPS.map(st => [st, stepGate(m, now, l, st)])), shots: shotsGate(m, now, l), unplaced: unplacedIn(m, paths) };
}
const git = (args: string[]) => { const r = Bun.spawnSync(['git', ...args], { cwd: ROOT, stdout: 'pipe', stderr: 'pipe' }); if (r.exitCode !== 0) throw new Error(`git ${args.join(' ')}: ${r.stderr.toString().trim()}`); return r.stdout.toString(); };
const at = (rev: string, path: string) => { const r = Bun.spawnSync(['git', 'show', `${rev}:${path}`], { cwd: ROOT, stdout: 'pipe', stderr: 'ignore' }); return r.exitCode === 0 ? r.stdout.toString() : undefined; };
/** The paths a commit range changed, as the snapshot reads them: mission.json also by the fields that moved (proofdelta.ts missionParts). */
export function rangePaths(from: string, to: string): string[] {
  const paths = git(['diff', '--name-only', from, to]).split('\n').filter(Boolean);
  if (!paths.includes(MISSION_FILE)) return paths;
  const a = missionParts(at(from, MISSION_FILE) ?? ''), b = missionParts(at(to, MISSION_FILE) ?? '');
  return [...paths, ...Object.keys(b).filter(k => a[k] !== b[k])];
}
/** The screen's lines for a what-if. */
export function whatLines(w: WhatIf): string[] {
  const run = Object.entries(w.steps).filter(([, g]) => g.run), skip = Object.entries(w.steps).filter(([, g]) => !g.run).map(([s]) => s);
  return [...planLines(w.plan), `steps: ${run.length ? run.map(([s]) => s).join(', ') : 'none of the gated steps'}${skip.length ? `; not run: ${skip.join(', ')}` : ''}`,
    ...[...Map.groupBy(run, ([, g]) => g.why)].map(([why, ss]) => `  ${ss.map(([s]) => s).join(', ')}: ${why}`), `shots: ${w.shots.names === 'all' ? 'all' : w.shots.names.length ? w.shots.names.join(', ') : 'none'} (${w.shots.why})`,
    ...(w.unplaced.length ? [`UNPLACED ${w.unplaced.length}: ${w.unplaced.slice(0, 20).join(', ')}${w.unplaced.length > 20 ? ` (+${w.unplaced.length - 20} more)` : ''}`] : [])];
}

if (import.meta.main) {
  const args = process.argv.slice(2), i = args.indexOf('--map'), map = i >= 0 ? args.splice(i, 2)[1] : undefined, [cmd, ...rest] = args;
  const m = loadMap(map ? resolve(map) : undefined);
  if (cmd === 'diff' && rest.length === 2) {
    const paths = rangePaths(rest[0]!, rest[1]!), w = whatIfAll(m, paths);
    console.log(`${rest[0]}..${rest[1]}: ${paths.length} paths, ${paths.length - w.unplaced.length} placed, ${w.unplaced.length} unplaced${map ? ` (map ${map})` : ''}`);
    for (const l of whatLines(w)) console.log(l);
  } else if (cmd === 'what-if' && rest.length) for (const l of whatLines(whatIfAll(m, rest))) console.log(l);
  else if (cmd === 'unplaced') {
    const s = snapshot(ROOT, m), un = Object.keys(s.files).filter(f => !classify(m, f)).sort();
    for (const f of un) console.log(f);
    console.error(`${un.length} of ${Object.keys(s.files).length} snapshot paths have no row in the proof map`);
    process.exit(un.length ? 1 : 0);
  } else { console.error('usage: bun tools/game/proofwhat.ts diff <from> <to> | what-if <path>... | unplaced   [--map <proofmap.json>]'); process.exit(2); }
}
