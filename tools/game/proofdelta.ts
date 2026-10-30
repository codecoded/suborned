// Delta proofs (brief 078; Sky: "Surely we only need to test deltas"). The batch runs only the sections of the player's proof that a change can affect:
//   proofmap.json        the declared table, path glob → sections. A path it cannot place runs the whole proof.
//   snapshot()           every path the table does not call "none", by content: git blob ids (the working file's when it differs), sha256 for the ignored folders the app carries.
//                        By content, not `git diff <commit>` alone, because builds are made from dirty trees (0.2.146 was b00cace3+) and the shared tree moves under them; the commit is recorded to say which.
//   proofledger.json     per section, the build, commit and snapshot it last PASSED on, and a hash of the map rows that can name it. Lives beside the build (out/build/play): lost, the next proof is full.
//   planProof()          a section is skipped only with a pass on the ledger, the same map hash and no change since that pass that its rows name. Anything else runs. Pure, so the gate can re-derive it.
//   a skip is never a pass: judgeGates says "not run, unchanged since build N (commit)", and the gate takes the skip only from planProof over the ledger, never from the plan file's word.
// A fix loop runs just the failing section (CLAUDE.md, Delta only). Order can matter to this proof (0.2.145: r29 alone passed where the full run failed), so a pass alone after a failure in company is said, with what ran before it, and marked on the ledger (Pass.alone); it is not re-run in company unless asked. A delta run cannot catch what only the sections it skipped would have disturbed: --full, and any submission build, runs them all.
// usage: bun tools/game/proofdelta.ts plan                  what the next batch's proof would run
//        bun tools/game/proofdelta.ts what-if <path>...     what a change to these paths alone would run, as if every section had just passed
import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { join, resolve } from 'node:path';
import { z } from 'zod';
import { IGNORED_INPUTS, fingerprint, ignoredFiles } from './fingerprint';

export const ROOT = resolve(import.meta.dir, '../..');
export const MAP_FILE = join(ROOT, 'tools/game/proofmap.json');
/** What the app carries or is made from that git does not hold, read by content: the kit and the figures, and the world export. */
export const SNAPSHOT_IGNORED = [...IGNORED_INPUTS, 'unity/Assets/Suborned/Kit', 'unity/Assets/Suborned/People', 'out/world/harbour-district.world.json', 'out/world/harbour-district.walk.json'];

const Row = z.strictObject({ match: z.array(z.string().min(1)).min(1), run: z.union([z.literal('full'), z.literal('none'), z.array(z.string())]), why: z.string().min(8), shots: z.union([z.literal('all'), z.array(z.string().min(1))]).optional() });
const ProofMapFile = z.strictObject({ schema: z.literal('suborned.proofmap/1'), note: z.string(), baseSeconds: z.number(), sections: z.array(z.strictObject({ id: z.string(), seconds: z.number() })).min(1), rows: z.array(Row) });
export type MapRow = z.infer<typeof Row>;
export type ProofMap = z.infer<typeof ProofMapFile>;

export function loadMap(file = MAP_FILE): ProofMap {
  const p = ProofMapFile.safeParse(JSON.parse(readFileSync(file, 'utf8')));
  if (!p.success) throw new Error(`${file}: ${p.error.issues.map(i => `${i.path.join('.')}: ${i.message}`).join('; ')}`);
  const ids = p.data.sections.map(s => s.id);
  for (const r of p.data.rows) if (Array.isArray(r.run)) for (const s of r.run) if (!ids.includes(s)) throw new Error(`${file}: the row for ${r.match[0]} names a section the proof does not have: ${s}`);
  return p.data;
}
export const sectionIds = (m: ProofMap) => m.sections.map(s => s.id);

/** The first row whose glob matches; none: the path cannot be placed, which runs the whole proof. */
export function classify(m: ProofMap, path: string): MapRow | undefined { return m.rows.find(r => r.match.some(g => new Bun.Glob(g).match(path))); }

/** The rows that decide whether `section` may be skipped: its own, the "none" rows (a new one hides paths from it) and the "full" rows. A row edited for another section leaves this hash alone. */
export function mapHash(m: ProofMap, section: string): string {
  const rows = m.rows.filter(r => r.run === 'full' || r.run === 'none' || r.run.includes(section)).map(r => ({ match: r.match, run: r.run === 'full' || r.run === 'none' ? r.run : section }));
  return createHash('sha256').update(JSON.stringify(rows)).digest('hex').slice(0, 16);
}

export type Snapshot = { id: string; files: Record<string, string> };
export const snapshotOf = (files: Map<string, string>): Snapshot => ({ id: fingerprint(files).slice(0, 16), files: Object.fromEntries(files) });
/** The tree by content, leaving out what the table says the player cannot read (so the evidence under briefs/ is not even hashed). */
export function snapshot(root = ROOT, m = loadMap()): Snapshot {
  const git = (a: string[], input?: string) => { const r = Bun.spawnSync(['git', ...a], { cwd: root, stdin: input === undefined ? 'ignore' : Buffer.from(input), stdout: 'pipe', stderr: 'ignore' }); return r.stdout.toString(); };
  const wanted = new Map<string, boolean>(), keep = (f: string) => { let k = wanted.get(f); if (k === undefined) { k = classify(m, f)?.run !== 'none'; wanted.set(f, k); } return k; };
  const files = new Map<string, string>();
  for (const l of git(['ls-files', '-s', '-z']).split('\0').filter(Boolean)) { const x = l.match(/^\d+ ([0-9a-f]+) \d\t(.+)$/s); if (x && keep(x[2]!)) files.set(x[2]!, x[1]!); }
  const dirty = [...git(['diff-files', '--name-only', '-z']).split('\0'), ...git(['ls-files', '-o', '--exclude-standard', '-z']).split('\0')].filter(f => f && keep(f));
  const live = dirty.filter(f => existsSync(join(root, f)));
  for (const f of dirty) if (!live.includes(f)) files.delete(f);
  if (live.length) { const ids = git(['hash-object', '--stdin-paths'], live.join('\n') + '\n').trim().split('\n'); live.forEach((f, i) => files.set(f, ids[i]!)); }
  for (const item of SNAPSHOT_IGNORED) {
    const p = join(root, item);
    if (!existsSync(p)) continue;
    if (item.endsWith('.json')) files.set(item, 'sha256:' + createHash('sha256').update(readFileSync(p)).digest('hex'));
    else for (const [f, h] of ignoredFiles(root, [item])) files.set(f, h);
  }
  return snapshotOf(files);
}
export function changedPaths(was: Record<string, string>, now: Record<string, string>): string[] {
  return [...new Set([...Object.keys(was), ...Object.keys(now)])].filter(f => was[f] !== now[f]).sort();
}

// ---------------------------------------------------------------- the ledger

export type Pass = { build: string; commit: string; at: string; snapshot: string; map: string; /** passed without what ran before it when it failed (build, those sections): order or timing; --full checks it */ alone?: { build: string; without: string[] } };
/** A section that failed in company: `ran` is what ran with it. */
export type Retry = { build: string; ran: string[] };
/** A batch step that ran and passed, or the check's scenarios: on what tree. */
export type StepPass = { build: string; commit: string; snapshot: string };
export type Ledger = { schema: 'suborned.proofledger/1'; sections: Record<string, Pass>; retry: Record<string, Retry>; steps: Record<string, StepPass>; snapshots: Record<string, Record<string, string>> };
export const emptyLedger = (): Ledger => ({ schema: 'suborned.proofledger/1', sections: {}, retry: {}, steps: {}, snapshots: {} });
export function readLedger(file: string): Ledger {
  try { const j = JSON.parse(readFileSync(file, 'utf8')); return j.schema === 'suborned.proofledger/1' ? { ...emptyLedger(), ...j } : emptyLedger(); } catch { return emptyLedger(); }
}
export function writeLedger(file: string, l: Ledger): void {
  const used = new Set([...Object.values(l.sections), ...Object.values(l.steps)].map(p => p.snapshot));
  writeFileSync(file, JSON.stringify({ ...l, snapshots: Object.fromEntries(Object.entries(l.snapshots).filter(([id]) => used.has(id))) }) + '\n');
}
/** The words a gate line and the screen use for a skip. Never "passed": it was not run. */
export const skipText = (p: Pass) => `not run, unchanged since build ${p.build} (${p.commit})`;

// ---------------------------------------------------------------- the plan

export type Plan = {
  schema: 'suborned.proofplan/1'; mode: 'full' | 'delta'; why: string;
  /** delta: the sections to run, in the proof's order (empty: only the proof's base) */
  only: string[];
  /** section → skipText; a full proof has none */
  skips: Record<string, string>;
  /** section → why it runs */
  reasons: Record<string, string>;
  seconds: number; snapshot: string; force?: string;
};
const secondsOf = (m: ProofMap, ids: string[]) => m.baseSeconds + m.sections.filter(s => ids.includes(s.id)).reduce((a, s) => a + s.seconds, 0);
const fullPlan = (m: ProofMap, why: string, snap: string, force?: string): Plan => ({ schema: 'suborned.proofplan/1', mode: 'full', why, only: [], skips: {}, reasons: {}, seconds: secondsOf(m, sectionIds(m)), snapshot: snap, ...(force ? { force } : {}) });
/** The sections that ran before `s` in the proof's order, of those in `ran`. */
const before = (m: ProofMap, s: string, ran: string[]) => { const ids = sectionIds(m); return ids.slice(0, ids.indexOf(s)).filter(x => ran.includes(x)); };

export function planProof(m: ProofMap, snap: Snapshot, l: Ledger, o: { force?: string } = {}): Plan {
  if (o.force) return fullPlan(m, o.force, snap.id, o.force);
  const ids = sectionIds(m), run = new Map<string, string>(), skips: Record<string, string> = {};
  if (!Object.keys(l.sections).length) return fullPlan(m, 'no section has a recorded pass yet', snap.id);
  for (const s of ids) {
    const p = l.sections[s];
    if (!p) { run.set(s, l.retry[s] ? 'failed last time' : 'no recorded pass'); continue; }
    if (p.map !== mapHash(m, s)) { run.set(s, 'its rows in the map changed since its pass'); continue; }
    const was = l.snapshots[p.snapshot];
    if (!was) { run.set(s, 'the snapshot of its pass is gone'); continue; }
    let hit: string | undefined;
    for (const f of changedPaths(was, snap.files)) {
      const row = classify(m, f);
      if (!row) return fullPlan(m, `${f} changed and the map cannot place it`, snap.id);
      if (row.run === 'full') return fullPlan(m, `${f} changed: ${row.why}`, snap.id);
      if (row.run !== 'none' && row.run.includes(s)) { hit = f; break; }
    }
    if (hit) run.set(s, `${hit} changed`); else skips[s] = skipText(p);
  }
  if (run.size === ids.length) return fullPlan(m, 'every section has to run', snap.id);
  const only = ids.filter(s => run.has(s));
  return { schema: 'suborned.proofplan/1', mode: 'delta', why: only.length ? `${only.length} of ${ids.length} sections touched by what changed` : 'nothing the proof reads changed', only, skips, reasons: Object.fromEntries(run), seconds: secondsOf(m, only), snapshot: snap.id };
}

// ---------------------------------------------------------------- the batch's steps (brief 078; Sky: "delta only")

/** A step whose answer depends on the engine, the world or the kit, or that is a whole-game measure (sim, kit, world, unity-world, unity-match, the check's scenarios, late-fight, tp-long).
 *  It runs with no recorded pass, or when anything marked full or unplaced changed since that pass, or when forced; a change the map places in sections cannot affect it. */
export function stepGate(m: ProofMap, snap: Snapshot, l: Ledger, step: string, force?: string): { run: boolean; why: string } {
  if (force) return { run: true, why: force };
  const p = l.steps[step], was = p && l.snapshots[p.snapshot];
  if (!p || !was) return { run: true, why: 'no recorded pass' };
  for (const f of changedPaths(was, snap.files)) { const r = classify(m, f); if (!r || r.run === 'full') return { run: true, why: `${f} changed` }; }
  return { run: false, why: `engine, world and kit unchanged since build ${p.build}` };
}
/** The shots a change can show in: what the rows of the paths changed since the last shots pass name. Anything full or unplaced, or no recorded pass: all of them. */
export function shotsGate(m: ProofMap, snap: Snapshot, l: Ledger, force?: string): { names: 'all' | string[]; why: string } {
  if (force) return { names: 'all', why: force };
  const p = l.steps.shots, was = p && l.snapshots[p.snapshot];
  if (!p || !was) return { names: 'all', why: 'no recorded pass' };
  const names = new Set<string>(); let by = '';
  for (const f of changedPaths(was, snap.files)) {
    const r = classify(m, f);
    if (!r || r.run === 'full' || r.shots === 'all') return { names: 'all', why: `${f} changed` };
    if (r.shots?.length) { for (const n of r.shots) names.add(n); by ||= f; }
  }
  return names.size ? { names: [...names], why: `${by} changed` } : { names: [], why: `no HUD or view path changed since build ${p.build}` };
}
/** Steps that passed in this batch, on the tree its player was built from. */
export function recordSteps(l: Ledger, snap: Snapshot, steps: string[], build: { version: string; commit: string }): Ledger {
  return steps.length ? { ...l, steps: { ...l.steps, ...Object.fromEntries(steps.map(s => [s, { build: build.version, commit: build.commit, snapshot: snap.id }])) }, snapshots: { ...l.snapshots, [snap.id]: snap.files } } : l;
}

/** What a change to `paths` alone would run, as if every section had just passed on the same tree (the table's answer, for `what-if` and the tests). */
export function whatIf(m: ProofMap, paths: string[]): Plan {
  const base = Object.fromEntries(paths.map(p => [p, 'was'])), now = Object.fromEntries(paths.map(p => [p, 'now']));
  const l = emptyLedger(); l.snapshots.s = base;
  for (const s of sectionIds(m)) l.sections[s] = { build: '0.2.N', commit: 'HEAD', at: '', snapshot: 's', map: mapHash(m, s) };
  return planProof(m, { id: 'now', files: now }, l);
}

// ---------------------------------------------------------------- the proof's answer on the ledger

type Frames = Record<string, any> | null | undefined;
/** Did the proof run this section? A section it did not run is null; `drive` has no object of its own, its saved session says. */
export const ranIn = (frames: Frames, s: string) => s === 'drive' ? frames?.driveSession != null : frames?.[s] != null;
export type Replays = { driveSession?: boolean; fireSession?: boolean };
const passedIn = (frames: Frames, s: string, r: Replays) => s === 'drive' ? ranIn(frames, s) && r.driveSession === true : frames?.[s]?.ok === true && (s !== 'fire' || r.fireSession === true);

/** Record what the proof said against the snapshot the player was built from. Only a section that ran can pass; a section that ran and did not pass loses its pass. A pass after a failure in
 *  company without what ran before it is marked `alone`. A section "not reached" (ok null) is reported by the gate and has no pass: it just runs next time. */
export function recordProof(m: ProofMap, l: Ledger, snap: Snapshot, plan: Plan, frames: Frames, replays: Replays, build: { version: string; commit: string; at: string }): Ledger {
  const out: Ledger = { ...l, sections: { ...l.sections }, retry: { ...l.retry }, snapshots: { ...l.snapshots, [snap.id]: snap.files } }, ids = sectionIds(m);
  const ran = ids.filter(s => ranIn(frames, s));
  for (const s of ran) {
    const r = out.retry[s], ok = passedIn(frames, s, replays), mine = before(m, s, ran), owed = r ? before(m, s, r.ran) : [];
    if (ok) {
      delete out.retry[s];
      const without = owed.filter(x => !mine.includes(x));
      out.sections[s] = { build: build.version, commit: build.commit, at: build.at, snapshot: snap.id, map: mapHash(m, s), ...(without.length ? { alone: { build: r!.build, without } } : {}) };
      continue;
    }
    delete out.sections[s];
    if (frames?.[s]?.ok === null || frames?.[s]?.ok === undefined && s !== 'drive') continue; // not reached
    if (mine.length) out.retry[s] = { build: build.version, ran };
  }
  return out;
}
/** The gate's words for a section that passed without what ran before it when it failed: the 0.2.145 case. */
export const aloneLine = (s: string, p: Pass) => `game: ${s} passed without ${p.alone!.without.length} section${p.alone!.without.length === 1 ? '' : 's'} that ran before it when it failed on build ${p.alone!.build}: order or timing; bun run batch --full checks it`;

export const planFile = (dir: string) => join(dir, 'proofplan.json');
export const ledgerFile = (dir: string) => join(dir, 'proofledger.json');
export const gateFile = (dir: string) => join(dir, 'proofgate.json');
export type PlanFile = Plan & { files: Record<string, string> };
export function writePlan(dir: string, plan: Plan, snap: Snapshot): void { writeFileSync(planFile(dir), JSON.stringify({ ...plan, files: snap.files }) + '\n'); }
export function readPlan(file: string): { plan: Plan; snap: Snapshot } {
  const j = JSON.parse(readFileSync(file, 'utf8')) as PlanFile, { files, ...plan } = j;
  return { plan, snap: { id: plan.snapshot, files } };
}
/** One line of a plan, for the batch and the what-if. */
export function planLines(p: Plan): string[] {
  const m = Math.round(p.seconds / 6) / 10;
  if (p.mode === 'full') return [`proof: FULL, about ${m} min: ${p.why}`];
  const by = Object.keys(p.skips).length;
  return [`proof: delta, about ${m} min: ${p.why}`, `  runs: the base${p.only.length ? `, ${p.only.join(', ')}` : ' only'}`, ...(by ? [`  not run: ${by} section${by === 1 ? '' : 's'} (${Object.keys(p.skips).join(', ')}); ${Object.values(p.skips)[0]}`] : [])];
}

// ---------------------------------------------------------------- the table against the sources

export type Sources = Record<string, string>;
const PLAY = 'unity/Assets/Suborned/Runtime/Play/';
/** Holes in the table: a proof that reads a HudToolkit member, or calls another proof's helper, must have its sections on the row of the file that holds it. `sources`: file name → text, of Play/. */
export function coverageGaps(m: ProofMap, sources: Sources): string[] {
  const rowOf = (f: string) => { const r = classify(m, PLAY + f)?.run; return Array.isArray(r) ? r : r === 'full' ? sectionIds(m) : []; }, gaps: string[] = [];
  const noComments = (t: string) => t.replace(/\/\/\/?[^\n]*/g, '');
  const proofs = Object.keys(sources).filter(f => /^Proof\w+\.cs$/.test(f)), parts = Object.keys(sources).filter(f => /^HudToolkit[\w.]*\.cs$/.test(f));
  // each proof's own sections, as GameBoot runs them: `if (Only("r29")) yield return Proof29.Run(`
  for (const [, s, q] of noComments(sources['GameBoot.cs'] ?? '').matchAll(/Only\("(\w+)"\)\) yield return (Proof\w+)\./g)) if (!rowOf(`${q}.cs`).includes(s!)) gaps.push(`GameBoot runs ${s} through ${q}: the row of ${q}.cs lacks ${s}`);
  for (const p of proofs) {
    const mine = rowOf(p), text = noComments(sources[p]!);
    for (const x of new Set([...text.matchAll(/\bHudToolkit\.([A-Z]\w+)\b(?!\.cs)/g)].map(a => a[1]!))) {
      const home = parts.find(f => new RegExp(`static[^=(;\\n]*\\b${x}\\b`).test(noComments(sources[f]!)));
      if (home) for (const s of mine) if (!rowOf(home).includes(s)) gaps.push(`${p} (${s}) reads HudToolkit.${x}, held in ${home}: its row lacks ${s}`);
    }
    for (const q of new Set([...text.matchAll(/\b(Proof(?:29|059|067|118|123|[A-Z][a-z]+))\.[A-Z]\w+/g)].map(a => a[1]!))) {
      const f = `${q}.cs`;
      if (f !== p && sources[f]) for (const s of mine) if (!rowOf(f).includes(s)) gaps.push(`${p} (${s}) calls ${q}: the row of ${f} lacks ${s}`);
    }
  }
  return gaps;
}

if (import.meta.main) {
  const [cmd, ...rest] = process.argv.slice(2), m = loadMap(), dir = process.env.SUBORNED_GAME_OUT ? resolve(process.env.SUBORNED_GAME_OUT) : join(ROOT, 'out/build/play');
  if (cmd === 'plan') { const t0 = performance.now(), s = snapshot(ROOT, m), p = planProof(m, s, readLedger(ledgerFile(dir))); for (const l of planLines(p)) console.log(l); console.log(`  (${Object.keys(s.files).length} paths read in ${Math.round(performance.now() - t0)} ms)`); }
  else if (cmd === 'what-if' && rest.length) for (const l of planLines(whatIf(m, rest))) console.log(l);
  else { console.error('usage: bun tools/game/proofdelta.ts plan | what-if <path>...'); process.exit(2); }
}
