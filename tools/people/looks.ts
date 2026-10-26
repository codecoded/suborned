// Who wears which figure (Pulse, brief 067 I; Sky on 0.2.122: "I see guard characters being used that aren't guards"). The host draws a person with a figure chosen from three tables:
// the role keys (unity/Assets/Suborned/Runtime/Play/HostViews.Figures.cs RoleFigure: the cell, the cast, the response, the rivals), the job families of the people file (PersonFigure:
// the same person always the same figure, by their id) and the pools the figures' manifest gives (tools/figures/roles.json `families`, `jobBlocks`). This is that choice, replayed
// from the people file `bun run people` writes (through sim/data/handover.people.json) and the figures' own data, and the rules the mapping is held to:
//   L-1 only security wears the P1 duty suit (the `guard` and `security` figures): no civilian, staff, clerk or rival is mapped to it, in a role key or a pool;
//   L-2 every role key has a figure, and every figure a pool names exists;
//   L-3 a role figure that is a recolour of a figure the crowd draws is named (the driver of D2, the rival of Worker D5, the officer of the guard): the model is shared, so the colour alone tells them apart.
// Pure, no model call. `bun tools/people/looks.ts [--csv <file>]` prints the audit (the table briefs/067/pulse.md carries); tools/people/test/looks.test.ts holds the rules and pins this mirror to the host's source.
import { readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';

const root = join(import.meta.dir, '..', '..');
const read = (f: string) => readFileSync(join(root, f), 'utf8');

export type FigureSpec = { id: string; role: string; label: string; kind?: string };
export type Klass = 'security' | 'cell' | 'cast' | 'staff' | 'civilian' | 'rival';
/** figures.json `role` → what the figure is to the player. */
export const KLASS: Record<string, Klass> = { guard: 'security', security: 'security', scout: 'cell', fixer: 'cell', tech: 'cell', enforcer: 'cell', waitress: 'cast', driver: 'cast', councillor: 'cast', 'waitress-staff': 'staff', townsperson: 'civilian', rival: 'rival' };

export type Mapping = { figures: Map<string, FigureSpec>; pools: Map<string, string[]>; variantOf: Record<string, string>; playViews: Record<string, unknown> };
/** The figures, their pools (as FigureBuild.Build makes them: `family/<first word>` for each family, each job block as written) and which is a recolour of which. */
export function loadMapping(): Mapping {
  const specs = (JSON.parse(read('tools/figures/figures.json')) as { figures: FigureSpec[] }).figures;
  const roles = JSON.parse(read('tools/figures/roles.json')) as { families: Record<string, string[]>; jobBlocks: Record<string, string[]>; variantOf: Record<string, string>; playViews: Record<string, unknown> };
  const pools = new Map<string, string[]>();
  for (const [k, v] of Object.entries(roles.families)) pools.set(`family/${k.split(' ')[0]}`, v);
  for (const [k, v] of Object.entries(roles.jobBlocks)) pools.set(k, v);
  return { figures: new Map(specs.map(f => [f.id, f])), pools, variantOf: roles.variantOf, playViews: roles.playViews };
}
/** The model a figure is made from: its own, or the one it is a recolour of (the officer of the guard, the driver of Commuter D2, the rival of Worker D5). */
export const modelOf = (m: Pick<Mapping, 'variantOf'>, id: string): string => { let x = id; for (let i = 0; i < 4 && m.variantOf[x]; i++) x = m.variantOf[x]!; return x; };
export const klassOf = (m: Mapping, id: string): Klass | undefined => { const f = m.figures.get(id); return f ? KLASS[f.role] : undefined; };

// ---- the host's choice, replayed (HostViews.Figures.cs, FigureLibrary.cs; tools/people/test/looks.test.ts pins each line of it to the source)
const isCrewId = (key: string) => /^rival-[1-5]$/.test(key);
/** HostViews.RoleFigure: the figure of a fixed view key, else null. */
export function roleFigure(key: string): string | null {
  const fixed: Record<string, string | null> = { 'op/scout': 'scout', 'op/fixer': 'fixer', 'op/tech': 'tech', 'op/enforcer': 'enforcer', waitress: 'waitress', guard: 'guard', driver: 'driver', councillor: 'councillor', patrol: null };
  if (key in fixed) return fixed[key]!;
  if (key.startsWith('security/') || key.startsWith('unit/')) return 'security';
  if (isCrewId(key) || key.startsWith('body/rival-')) return 'rival';
  return null;
}
/** HostViews.PersonFigure's family of a job. */
export const famOf = (job: string): string => job === 'server' || job === 'cook' || job === 'waitress' ? 'cafe' : job === 'clerk' || job === 'attendant' || job === 'councillor' ? 'office' : job === 'guard' ? 'guard' : job === 'driver' || job === 'official-driver' ? 'driver' : 'passer';
/** FigureLibrary.Pick: FNV-1a over the seed's UTF-16 units, modulo the pool's length. */
export const fnv = (s: string): number => { let h = 2166136261 >>> 0; for (let i = 0; i < s.length; i++) { h ^= s.charCodeAt(i); h = Math.imul(h, 16777619) >>> 0; } return h >>> 0; };
export const pick = (m: Pick<Mapping, 'pools'>, pool: string, seed: string): string | null => { const ids = m.pools.get(pool); return ids?.length ? ids[fnv(seed) % ids.length]! : null; };
/** The pool PersonFigure draws a job from: its job block when the pools have one by that name (`job/resident`), else its family. */
export const poolOf = (m: Pick<Mapping, 'pools'>, job: string): string => (m.pools.has(`job/${job}`) ? `job/${job}` : `family/${famOf(job)}`);
export const personFigure = (m: Pick<Mapping, 'pools'>, id: string, job: string, salt = ''): string | null => pick(m, `job/${job}`, id + salt) ?? pick(m, `family/${famOf(job)}`, id + salt);

export type Walker = { id: string; name: string; job: string; stops: [string, number, number, number][] };
export type Dressed = { index: number; id: string; name: string; job: string; figure: string; first: string; pool: string; place0: string; from0: number };
/** HostViews.DressCrowd: the walkers in the people file's order (the cast, played by their role, are not among them), each given the figure of their job, and none the same as another who sets out
 *  from the same place at the same time while the pool has another (the pick is re-seeded `#1`.. `#7`, still by the person's id). */
export function dressCrowd(m: Mapping, walkers: Walker[]): Dressed[] {
  const used = new Map<string, Set<string>>(), out: Dressed[] = [];
  walkers.forEach((w, index) => {
    const grp = w.stops.length ? `${w.stops[0]![0]}@${w.stops[0]![1] * 10}` : '';
    let set = used.get(grp); if (!set) used.set(grp, set = new Set());
    const first = personFigure(m, w.id, w.job)!; let fid = first;
    for (let t = 1; t < 8 && fid != null && grp.length > 0 && set.has(fid); t++) fid = personFigure(m, w.id, w.job, `#${t}`)!;
    set.add(fid);
    out.push({ index, id: w.id, name: w.name, job: w.job, figure: fid, first, pool: poolOf(m, w.job), place0: w.stops[0]?.[0] ?? '', from0: w.stops[0]?.[1] ?? 0 });
  });
  return out;
}

/** The people the engine plays by their role (the cast) and the rest it walks: the engine's people file and the site's cast. */
export function loadPeople() {
  const people = JSON.parse(read('sim/data/handover.people.json')) as { startsAt: number; people: (Walker & { work: number })[] };
  const site = JSON.parse(read('sim/data/handover.site.json')) as { cast: Record<string, { id: string; name: string }> };
  const cast = new Map(['waitress', 'guard', 'driver', 'councillor'].map(r => [site.cast[r]!.id, r]));
  const walkers = people.people.filter(p => !cast.has(p.id));
  return { people, cast, walkers };
}

/** Whether a job is security to the engine (Model.cs Person.Civilian: a guard, patrol officer, investigator or watcher, or a rival's own job, is no civilian). */
export const SECURITY_JOBS = ['guard', 'patrol-officer', 'investigator', 'watcher'];

export type Fault = { rule: 'L-1' | 'L-2' | 'L-3'; what: string };
/** The rules the mapping is held to, over the Handover's people. */
export function faults(m: Mapping, crowd: Dressed[]): Fault[] {
  const out: Fault[] = [];
  // L-1: the P1 duty suit is security's alone, in the role keys and in every pool a civilian draws from
  const P1 = (id: string) => modelOf(m, id) === 'guard';
  for (const key of ['op/scout', 'op/fixer', 'op/tech', 'op/enforcer', 'waitress', 'guard', 'driver', 'councillor', 'patrol', 'security/0', 'unit/officer', 'rival-1', 'body/rival-2']) {
    const f = roleFigure(key);
    if (f && P1(f) && !(key === 'guard' || key.startsWith('security/') || key.startsWith('unit/'))) out.push({ rule: 'L-1', what: `${key} wears ${f}, the duty suit` });
  }
  for (const [pool, ids] of m.pools) {
    const user = pool.startsWith('family/') ? pool.slice(7) : pool;
    for (const id of ids) if (P1(id) && user !== 'guard') out.push({ rule: 'L-1', what: `${pool} holds ${id}, the duty suit` });
  }
  for (const c of crowd) if (P1(c.figure) && !SECURITY_JOBS.includes(c.job)) out.push({ rule: 'L-1', what: `${c.id} (${c.job}) wears ${c.figure}, the duty suit` });
  for (const c of crowd) if (!P1(c.figure) && SECURITY_JOBS.includes(c.job)) out.push({ rule: 'L-1', what: `${c.id} (${c.job}) is security and wears ${c.figure}` });
  // L-2: a figure for every key, a figure in the library for every name
  for (const key of ['op/scout', 'op/fixer', 'op/tech', 'op/enforcer', 'waitress', 'guard', 'driver', 'councillor', 'security/0', 'unit/officer', 'rival-1']) { const f = roleFigure(key); if (!f || !m.figures.has(f)) out.push({ rule: 'L-2', what: `${key} has no figure (${f})` }); }
  for (const [pool, ids] of m.pools) for (const id of ids) if (!m.figures.has(id)) out.push({ rule: 'L-2', what: `${pool} names ${id}, which is no figure` });
  for (const c of crowd) if (!m.figures.has(c.figure)) out.push({ rule: 'L-2', what: `${c.id} (${c.job}) has no figure` });
  // L-3 is a fact, not a fault: see sharedModels
  return out;
}
/** The role figures that are recolours of a figure the crowd draws (L-3): the model is shared, so only the colour tells a driver from a passer in coral, a rival from a worker in yellow. */
export function sharedModels(m: Mapping): { role: string; model: string; crowd: string[] }[] {
  const out: { role: string; model: string; crowd: string[] }[] = [];
  for (const [id, f] of m.figures) {
    const klass = KLASS[f.role]; if (klass === 'civilian' || klass === 'staff') continue;
    const base = modelOf(m, id);
    if (base === id) continue;
    out.push({ role: id, model: base, crowd: [...m.pools].filter(([, ids]) => ids.includes(base)).map(([k]) => k) });
  }
  return out;
}

// ---- the audit
const label = (m: Mapping, id: string) => m.figures.get(id)?.label.split(' (')[0]?.split(': ')[0] ?? id;
export function audit(): string {
  const m = loadMapping(), { walkers, cast, people } = loadPeople(), crowd = dressCrowd(m, walkers), out: string[] = [];
  const by = (key: (c: Dressed) => string) => { const g = new Map<string, Dressed[]>(); for (const c of crowd) g.set(key(c), [...(g.get(key(c)) ?? []), c]); return [...g].sort(([a], [b]) => (a < b ? -1 : 1)); };
  out.push('the host\'s fixed keys (HostViews.RoleFigure):');
  for (const key of ['op/scout', 'op/fixer', 'op/tech', 'op/enforcer', 'waitress', 'guard', 'driver', 'councillor', 'security/<0..1>', 'unit/<officer>', 'hostile/<n> officers', 'rival-1..5', 'hostile/<n> rival', 'body/rival-<n>', 'patrol']) {
    const k = key.replace('<0..1>', '0').replace('<officer>', 'officer').replace('hostile/<n> officers', 'security/h').replace('rival-1..5', 'rival-1').replace('hostile/<n> rival', 'rival-2').replace('<n>', '3');
    const f = key.startsWith('hostile/<n> officers') ? 'security' : key === 'hostile/<n> rival' ? 'rival' : roleFigure(k);
    out.push(`  ${key.padEnd(22)} ${(f ?? '(a robot prefab, no human figure)').padEnd(10)} ${f ? `${KLASS[m.figures.get(f)!.role]} · model ${modelOf(m, f)} · ${label(m, f)}` : ''}`);
  }
  out.push('', `the ${crowd.length} walkers of the people file (${cast.size} cast people are played by their role), by job → figure (PersonFigure + DressCrowd):`);
  for (const [k, cs] of by(c => `${c.job} → ${c.figure}`)) out.push(`  ${k.padEnd(28)} ${String(cs.length).padStart(3)}  ${KLASS[m.figures.get(cs[0]!.figure)!.role]} · pool ${cs[0]!.pool}`);
  const moved = crowd.filter(c => c.figure !== c.first);
  out.push('', `${moved.length} of ${crowd.length} walkers wear another figure than their own pick, because someone who sets out from the same place at the same time had it (DressCrowd); a body is drawn with the plain pick (HostViews.Bodies).`);
  const f = faults(m, crowd);
  out.push('', f.length ? `rules: ${f.length} fault(s)\n${f.map(x => `  ${x.rule} ${x.what}`).join('\n')}` : 'rules L-1 and L-2: no fault: only guards wear the duty suit, and nobody who is a guard wears anything else');
  out.push('', 'L-3 shared models (a role figure that is a recolour of a figure the crowd draws):');
  for (const s of sharedModels(m)) out.push(`  ${s.role.padEnd(10)} is ${s.model}, which ${s.crowd.length ? 'the pools ' + s.crowd.join(', ') + ' draw' : 'no pool draws'}`);
  void people;
  return out.join('\n');
}
/** One row a person: who they are, the host's key, their figure and where they stand when the mission opens. */
export function csv(): string {
  const m = loadMapping(), { walkers, people } = loadPeople(), crowd = dressCrowd(m, walkers);
  const clock = (s: number) => `${s < 0 ? '-' : ''}${Math.floor(Math.abs(s) / 60)}:${String(Math.abs(s) % 60).padStart(2, '0')}`;
  const rows = ['index,id,name,job,host key,figure,class,model,pool,own pick,first stop on the street (place from-to in the mission clock)'];
  const byId = new Map(people.people.map(p => [p.id, p]));
  for (const c of crowd) {
    const p = byId.get(c.id)!, s = p.stops.find(x => x[3] === 1 && x[2] > 0 && x[1] < 510) ?? p.stops.find(x => x[3] === 1 && x[2] > 0);
    rows.push([c.index, c.id, c.name, c.job, `crowd/${c.index}`, c.figure, KLASS[m.figures.get(c.figure)!.role], modelOf(m, c.figure), c.pool, c.first, s ? `${s[0]} ${clock(s[1])} to ${clock(s[2])}` : 'inside, at home or off the sheet for the mission'].map(x => `"${x}"`).join(','));
  }
  return rows.join('\n') + '\n';
}
if (import.meta.main) {
  const i = process.argv.indexOf('--csv');
  if (i > 0) { writeFileSync(join(process.cwd(), process.argv[i + 1]!), csv()); console.log(`wrote ${process.argv[i + 1]}`); }
  else console.log(audit());
}
