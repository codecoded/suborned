// The data behind the roster viewer (`bun run people:view`): one cast expanded exactly as `bun run people` expands it (same inputs, same bytes), checked by the same
// checker (P-0..P-13, then M-2 as the CLI prints it), and laid out for the page: people with their sheets, the Handover's four roles, the tie graph (seeded layout), the
// map, the day, and, against another cast, who was added, removed or changed. Nothing here is a model call or a clock read: the same inputs give the same view.
import { resolve } from 'node:path';
import type { Cast } from '../../contracts/people/cast.schema';
import type { People, Person } from '../../contracts/people/people.schema';
import { peopleHash } from '../levels/people/hash';
import { byId, hasRegularTie, windowOf } from '../missions/handover/cast';
import { overlay } from '../missions/handover/overlay';
import { civic } from '../plan/civic';
import type { Check } from '../plan/checks';
import { derive } from '../plan/plan';
import { root } from '../rag/config';
import { sha256 } from '../gen/sha';
import { checkPeople } from '../people/check';
import { cafeOpenAt } from '../people/day';
import { expand, inShift, stopsAtWork, type Ctx } from '../people/expand';
import { readTexts, world, type World } from '../people/inputs';
import { castForNames } from '../people/roles';
import { withSecrets } from '../people/secrets';
import { requirements, stepWord, type Req } from '../people/review';
import { isTalker, type Rules } from '../people/rules';
import { atCafeSide } from '../people/scope';
import { approachRows, usdText, type ApproachRow, type Rulebook } from './fit';
import { GRAPH_H, GRAPH_W, layout } from './layout';
import { buildMap, S, type MapData } from './map';
import { loadSource, type RunInfo, type Source } from './source';

export const VIEW_SCHEMA = 'suborned.people.view/1';
/** The Handover's purse (GDD §04, hold line of §14 Keel/Waymark: $100,000). A contract file does not carry it yet, so it is named here and cited where it is used. */
export const PURSE_USD = 100000;

const hm = (m: number) => `${String(Math.floor(m / 60) % 24).padStart(2, '0')}:${String(Math.round(m) % 60).padStart(2, '0')}`;
const shiftText = (s: { from: number; to: number } | undefined) => (s ? `${hm(s.from)}-${hm(s.to)}` : '');
const SCALES = ['bravery', 'resentment', 'fear', 'loyalty'] as const;
const TIE_ORDER = ['family', 'lover', 'workmate', 'regular', 'debtor', 'rival'];

/** `by: 'them'`: a lover tie is met on the other person's routine (they stop by at this person's work), not this person's own. */
export type VTie = { kind: string; with: string; at?: string; by?: 'them' };
export type VPerson = {
  id: string; name: string; unit: string; area: string; trade: string; position: string; post: string | null; workPlace: string | null; shift: [number, number] | null; home: { plot: string; place: string };
  tem: string; need: string; needWeights: Record<string, number>; inst: string | null; secret: string | null;
  start: Record<(typeof SCALES)[number], number>; steps: Record<(typeof SCALES)[number], string>; talker: boolean; talkerSums: [number, number];
  ties: VTie[]; tiedBy: { kind: string; from: string }[]; variant: string; stops: [string, string, number, number][];
  knows: string[]; knownBy: string[]; role?: string; notes: string[]; fit: string;
  diff?: { status: 'added' | 'changed' | 'same'; fields: [string, string, string][] };
};
export type VRole = {
  id: string; what: string; employer: string; position: string; ok: boolean; why: string; pinned?: string; candidates: string[]; need?: string; approach?: string; regularOf?: string;
  window: { missionFrom: string; missionTo: string; dayFrom: number; dayTo: number; place?: string; near?: number }; leavesTo?: { at: string; by: string }; alone?: boolean; waitsFor?: string;
  tied: { text: string; ok: boolean }[]; reqs: Req[]; src: string; note: string; secret?: string;
};
export type Notice = { id: string; level: 'fail' | 'warn' | 'info'; title: string; detail: string; people?: string[]; src?: string };
export type View = {
  schema: typeof VIEW_SCHEMA; id: string; title: string;
  source: { kind: Source['kind']; label: string; file: string; castSha: string; promotedFrom?: string };
  against?: { kind: Source['kind']; label: string; id: string; castSha: string; run?: string };
  plan: { id: string; version: string }; scope: string; inputs: People['inputs'];
  run?: RunInfo; drift: { input: string; pinned: string; current: string; same: boolean }[];
  checks: { id: string; rule: string; value: string; limit: string; ok: boolean; detail: string; recorded?: boolean }[];
  rules: {
    steps: Record<string, string[]>; temperaments: Record<string, { name: string; respondsTo: string[]; refuses: string[]; reportsApproach: boolean; hero: string; menace: string }>;
    needs: Record<string, { metBy: string[]; help: string; requires: string }>; approaches: Record<string, { requires: string; usd: number; timeS: number; trail: number; leaves: string }>;
    ties: Record<string, { direction: string; introduces: boolean; between: string; secret?: boolean }>; secrets: Record<string, { requires: string; learnedBy: string[] }>; rulebook: boolean;
  };
  units: { id: string; name: string; trade: string; staff: number; hours: [number, number]; shifts: [number, number][]; watch: [number, number][]; people: number }[];
  rows: { key: string; why: string; positions: string; temperaments: { k: string; w: number }[]; needs: { k: string; w: number }[]; secrets: { k: string; w: number }[]; lovers: string[]; people: number }[];
  castNotes: string[];
  mission: { startsAt: string; startMin: number; end: number };
  places: Record<string, { name: string; kind: string; s: [number, number] | null }>;
  persons: VPerson[];
  fits: Record<string, ApproachRow[]>;
  roles: VRole[];
  counts: Record<'unit' | 'occupation' | 'position' | 'temperament' | 'need' | 'shift', { k: string; n: number }[]>;
  graph: { w: number; h: number; nodes: { id: string; x: number; y: number; r: number; deg: number }[]; edges: { a: string; b: string; kind: string; dir: boolean; at?: string }[]; components: string[][]; kinds: Record<string, number>; cell: string };
  map: MapData;
  notices: Notice[];
  gaps: string[]; genNotes: string[];
  compare?: {
    added: string[]; removed: string[]; changed: string[]; same: string[]; renamed: number; shared: number;
    dims: Record<string, { k: string; a: number; b: number }[]>;
    otherChecks: { id: string; ok: boolean }[];
    rows: { key: string; status: 'added' | 'removed' | 'changed' | 'same'; lines: string[] }[]; names: { given: [number, number]; family: [number, number]; kept: number }; gone: { id: string; name: string; unit: string; position: string; tem: string; need: string }[];
  };
  toolsHash: string;
};

type Gen = { src: Source; ctx: Ctx; people: People; gaps: string[]; notes: string[] };
const expandCast = (w: World, src: Source): Gen => { const ctx = w.ctx(src.cast), g = expand(ctx); return { src, ctx, people: g.people, gaps: g.gaps, notes: g.notes }; };

const rowKey = (p: Person, w: World) => (p.work ? `${p.work.area}/${p.work.trade}` : `${w.scope.homes.find(h => h.plot === p.home.plot)?.area ?? '?'}/resident`);
const unitOf = (p: Person) => p.work?.unit ?? 'resident';

/** What a person of one cast is, as the diff reads it: every field a person carries, as text. */
function flat(p: Person, rules: Rules): Record<string, string> {
  const o: Record<string, string> = {
    name: p.name, unit: unitOf(p), position: p.position, post: p.work?.post ?? '', place: p.work?.place ?? '', shift: shiftText(p.work?.shift), home: p.home.plot, temperament: p.temperament, need: p.need, institution: p.institution ?? '', secret: p.secret ?? '',
  };
  for (const s of SCALES) o[s] = `${stepWord(rules, s, p.start[s])} (${p.start[s]})`;
  o.ties = [...p.ties].sort((a, b) => (a.kind + a.with < b.kind + b.with ? -1 : 1)).map(t => `${t.kind}:${t.with}`).join(', ');
  o.variant = p.routine.variant;
  o.stops = p.routine.stops.map(s => `${hm(s.from)}-${hm(s.to)} ${s.doing}@${s.place}`).join(' · ');
  o.needWeights = Object.entries(p.needWeights).sort(([a], [b]) => (a < b ? -1 : 1)).map(([k, v]) => `${k} ${v}`).join(', ');
  return o;
}

function rowDiff(a: Cast['rows'][number] | undefined, b: Cast['rows'][number] | undefined): { status: 'added' | 'removed' | 'changed' | 'same'; lines: string[] } {
  if (!a) return { status: 'added', lines: [`new row: ${b!.why}`] };
  if (!b) return { status: 'removed', lines: ['the row is gone'] };
  const lines: string[] = [];
  const wmap = (xs: { k: string; w: number }[]) => new Map(xs.map(x => [x.k, x.w]));
  const cmp = (label: string, x: Map<string, number>, y: Map<string, number>) => {
    for (const k of [...new Set([...x.keys(), ...y.keys()])].sort()) if (x.get(k) !== y.get(k)) lines.push(`${label} ${k}: ${x.get(k) ?? '·'} → ${y.get(k) ?? '·'}`);
  };
  if (a.why !== b.why) lines.push(`why: "${a.why}" → "${b.why}"`);
  cmp('temperament', wmap(a.temperaments.map(t => ({ k: t.temperament, w: t.weight }))), wmap(b.temperaments.map(t => ({ k: t.temperament, w: t.weight }))));
  // needs as each position of the row is drawn from them (a weight may name positions), in whole percent, so a reshuffled list that draws the same shares reads as no change
  const eff = (r: Cast['rows'][number], pos: string) => { const ns = r.needs.filter(n => !n.positions || n.positions.includes(pos)), tot = ns.reduce((n, x) => n + x.weight, 0) || 1, m = new Map<string, number>(); for (const n of ns) m.set(n.need, (m.get(n.need) ?? 0) + Math.round((100 * n.weight) / tot)); return m; };
  for (const pos of [...new Set([...a.positions, ...b.positions].map(p => p.position))].sort()) cmp(`needs of ${pos} (%)`, eff(a, pos), eff(b, pos));
  const pk = (r: Cast['rows'][number]) => new Map(r.positions.map(p => [p.position, JSON.stringify({ w: p.weight, post: p.post, count: p.count, posts: p.posts, side: p.side, watch: p.watch })]));
  for (const k of [...new Set([...pk(a).keys(), ...pk(b).keys()])].sort()) if (pk(a).get(k) !== pk(b).get(k)) lines.push(`position ${k}: ${pk(a).get(k) ?? '·'} → ${pk(b).get(k) ?? '·'}`);
  const tk = (r: Cast['rows'][number]) => new Map(r.ties.map((t, i) => [`${t.kind}${t.with ? `→${t.with}` : ''}${t.positions ? `[${t.positions.join('/')}]` : ''}${t.at ? `@${t.at}` : ''}#${i}`, String(t.share)]));
  const ta = [...tk(a).entries()].map(([k, v]) => `${k.replace(/#\d+$/, '')} ${v}`).sort(), tb = [...tk(b).entries()].map(([k, v]) => `${k.replace(/#\d+$/, '')} ${v}`).sort();
  for (const t of ta) if (!tb.includes(t)) lines.push(`tie dropped: ${t}`);
  for (const t of tb) if (!ta.includes(t)) lines.push(`tie added: ${t}`);
  const sk = (r: Cast['rows'][number]) => new Map((r.secrets ?? []).map(s => [s.secret, s.share]));
  cmp('secret share', sk(a), sk(b));
  const rk = (r: Cast['rows'][number]) => new Map(r.routines.map(v => [v.id, JSON.stringify(v)]));
  for (const k of [...new Set([...rk(a).keys(), ...rk(b).keys()])].sort()) if (rk(a).get(k) !== rk(b).get(k)) lines.push(`routine ${k}: ${rk(a).has(k) ? (rk(b).has(k) ? 'changed' : 'removed') : 'added'}`);
  return { status: lines.length ? 'changed' : 'same', lines };
}

export type BuildOpts = { plan?: string; scope?: string; run?: string; cast?: string; against?: string };

export async function buildView(o: BuildOpts = {}): Promise<View> {
  const planFile = o.plan ?? 'contracts/world/harbour-district.plan.json';
  const texts = await readTexts(planFile), w = world(texts, o.scope ?? 'civic-block'), { scope, rules, plan } = w;
  const src = loadSource(plan.id, o.cast ? { cast: o.cast } : o.run ? { run: o.run } : 'promoted');
  const a = expandCast(w, src), P = a.people.persons, by = new Map(P.map(p => [p.id, p]));

  // the checker, as `bun run people` runs it; M-2 on these people, as the CLI prints it
  const block = JSON.parse(texts.block), civ = civic(plan.civic, block);
  const runChecks = (g: Gen): Check[] => {
    const ck = checkPeople(g.ctx, w.mission, w.plan, { people: g.people, gaps: g.gaps });
    const m = overlay(w.plan, block, g.people as never, JSON.parse(texts.mission)).civic!({ d: derive(w.plan), civ } as never).find(c => c.id === 'M-2')!;
    return [...ck.checks, m as Check];
  };
  const allChecks = runChecks(a);
  const recorded = new Map((src.run?.recorded ?? []).map(c => [c.id, c.ok]));
  const checks: View['checks'] = allChecks.map(c => ({ id: c.id, rule: c.rule, value: c.value, limit: c.limit, ok: c.ok, detail: (c.detail ?? '') + (c.id === 'M-2' && !c.ok && src.kind === 'promoted' ? ' (the promoted cast: shown, not enforced, as `bun run people` does)' : ''), ...(recorded.has(c.id) ? { recorded: recorded.get(c.id)! } : {}) }));

  // Keel's rulebook, read as plain JSON (it is Keel's, and moves)
  let rb: Rulebook;
  try { rb = JSON.parse(await Bun.file(resolve(root, 'contracts/rules/rulebook.json')).text()) as Rulebook; } catch { rb = undefined; }

  const unitName = (id: string) => (id === 'resident' ? 'Residents' : scope.units.find(u => u.id === id)?.name ?? id);
  const places: View['places'] = {};
  for (const p of a.people.places) places[p.id] = { name: p.name, kind: p.kind, s: S(p.at) };
  places.beyond = { name: 'beyond the sheet', kind: 'beyond', s: null };

  // the Handover's roles, cast as the names are given (play first), and what the cast row requires of each
  const casting = a.ctx.casting!, cast = castForNames(P, casting, rules), reqs = requirements(a.ctx, a.people);
  const roleOf = new Map<string, string>(); for (const c of cast) if (c.ok && c.pinned) roleOf.set(c.pinned, c.role);
  const know = new Map((a.people.knowledge?.needsKnown ?? []).map(k => [k.keeper, k.knows]));
  const knownBy = new Map<string, string[]>(); for (const [k, ids] of know) for (const id of ids) knownBy.set(id, [...(knownBy.get(id) ?? []), k]);
  const tiedBy = new Map<string, { kind: string; from: string }[]>();
  for (const p of P) for (const t of p.ties) if ((rules.ties.kinds as Record<string, { direction: string }>)[t.kind]?.direction !== 'both') tiedBy.set(t.with, [...(tiedBy.get(t.with) ?? []), { kind: t.kind, from: p.id }]);

  const cafeUnit = scope.cafeUnit;
  const fits: View['fits'] = {};
  const shutNotes = new Map<string, string[]>(), shut: string[] = [];
  const persons: VPerson[] = [...P].sort((x, y) => byId(x.id, y.id)).map(p => {
    const key = `${p.temperament}/${p.need}`; fits[key] ??= approachRows(rules, rb, p.temperament, p.need);
    const ns = a.notes.filter(n => n.startsWith(`${p.id}:`)).map(n => n.slice(p.id.length + 1).trim());
    for (const s of p.routine.stops) if (atCafeSide(scope, s.place, s.doing, ['pre-shift', 'evening', 'meal', 'errand']) && !cafeOpenAt(scope, s.from)) {
      const t = `${s.doing} stop at the café (${hm(s.from)}-${hm(s.to)}) while it is shut (open ${hm(60 * (scope.units.find(u => u.id === cafeUnit)?.hours[0] ?? 0))}-${hm(60 * (scope.units.find(u => u.id === cafeUnit)?.hours[1] ?? 24))})`;
      shutNotes.set(p.id, [...(shutNotes.get(p.id) ?? []), t]); shut.push(p.id);
    }
    const ties: VTie[] = p.ties.map(t => {
      const q = by.get(t.with), v: VTie = { kind: t.kind, with: t.with };
      // `at`: the stop of this person's day a regular tie stands on (the pre-shift stop at the café, an evening there): the rule P-7 reads
      if (t.kind === 'regular' && q?.work) {
        const s = p.routine.stops.find(x => x.doing !== 'home' && x.place !== 'beyond' && (x.place === q.work!.place || (q.work!.unit === cafeUnit && atCafeSide(scope, x.place, x.doing, ['pre-shift', 'evening']))) && (inShift(q.work!.shift, x.from) || inShift(q.work!.shift, Math.max(x.from, x.to - 1))));
        if (s) v.at = `${s.doing} ${hm(s.from)}-${hm(s.to)}`;
      }
      // a lover tie is met on either routine (rule P-14): this person's stop at the other's work while they work, else the other's at this person's
      if (t.kind === 'lover' && q) {
        const meet = (x: Person, y: Person) => x.routine.stops.find(s => s.doing !== 'home' && s.place !== 'beyond' && stopsAtWork(scope, [s], y));
        const mine = meet(p, q), theirs = mine ? undefined : meet(q, p), s = mine ?? theirs;
        if (s) { v.at = `${s.doing} ${hm(s.from)}-${hm(s.to)}`; if (theirs) v.by = 'them'; }
      }
      return v;
    }).sort((x, y) => TIE_ORDER.indexOf(x.kind) - TIE_ORDER.indexOf(y.kind) || byId(x.with, y.with));
    const sum = (ks: readonly (typeof SCALES)[number][]) => ks.reduce((n, k) => n + p.start[k], 0);
    return {
      id: p.id, name: p.name, unit: unitOf(p), area: p.work?.area ?? rowKey(p, w).split('/')[0]!, trade: p.work?.trade ?? 'resident', position: p.position, post: p.work?.post ?? null, workPlace: p.work?.place ?? null,
      shift: p.work ? [p.work.shift.from, p.work.shift.to] : null, home: p.home, tem: p.temperament, need: p.need, needWeights: p.needWeights, inst: p.institution, secret: p.secret ?? null,
      start: p.start, steps: Object.fromEntries(SCALES.map(s => [s, stepWord(rules, s, p.start[s])])) as VPerson['steps'], talker: isTalker(rules, p.start), talkerSums: [sum(rules.talker.test.left), sum(rules.talker.test.right)],
      ties, tiedBy: (tiedBy.get(p.id) ?? []).sort((x, y) => byId(x.from, y.from)), variant: p.routine.variant, stops: p.routine.stops.map(s => [s.place, s.doing, s.from, s.to] as [string, string, number, number]),
      knows: know.get(p.id) ?? [], knownBy: (knownBy.get(p.id) ?? []).sort(byId), ...(roleOf.has(p.id) ? { role: roleOf.get(p.id)! } : {}), notes: [...ns, ...(shutNotes.get(p.id) ?? [])], fit: key,
    };
  });
  const pById = new Map(persons.map(p => [p.id, p]));

  const roles: VRole[] = casting.mission.roles.map((r, i) => {
    const c = cast[i]!, pos = r.position ?? r.role!, [d0, d1] = windowOf(casting.mission, r);
    const p = c.pinned ? pById.get(c.pinned) : undefined, full = c.pinned ? by.get(c.pinned) : undefined;
    const tied: VRole['tied'] = [];
    if (p && r.regularOf) { const staff = new Set(P.filter(x => x.work?.employer === r.regularOf).map(x => x.id)), ts = p.ties.filter(t => t.kind === 'regular' && staff.has(t.with)); tied.push({ ok: hasRegularTie(full!, staff), text: `regular of ${r.regularOf}: ${ts.map(t => `${by.get(t.with)!.name}${t.at ? ` (${t.at})` : ''}`).join(', ') || 'no regular tie'}` }); }
    for (const t of r.tiedTo ?? []) {
      const other = cast.find(x => x.role === t.role), op = other?.pinned ? by.get(other.pinned) : undefined;
      const ties = p?.ties.filter(x => x.kind === t.kind) ?? [], hit = ties.find(x => x.with === op?.id) ?? ties.find(x => by.get(x.with)?.work && casting.mission.roles.find(q => q.id === t.role && (q.position ? by.get(x.with)!.work!.position === q.position : by.get(x.with)!.work!.role === q.role) && q.employer === by.get(x.with)!.work!.employer));
      tied.push({ ok: !!hit, text: `${t.kind} tie to the ${t.role}${hit ? `: ${by.get(hit.with)!.name} (${hit.with})` : ': none'}${t.cast ? ' (the one cast in that role)' : ''}` });
    }
    if (p && (r as { secret?: string }).secret) tied.push({ ok: full?.secret === (r as { secret?: string }).secret, text: `secret ${(r as { secret?: string }).secret}: ${full?.secret ?? 'none'}` });
    if (p && r.leavesTo) tied.push({ ok: c.ok, text: `leaves for ${r.leavesTo.at} by ${r.leavesTo.by}: his next stop after the window is there` });
    return {
      id: r.id, what: r.what, employer: r.employer, position: pos, ok: c.ok, why: c.why, ...(c.pinned ? { pinned: c.pinned } : {}), candidates: c.who, ...(r.need ? { need: r.need } : {}), ...(r.approach ? { approach: r.approach } : {}), ...(r.regularOf ? { regularOf: r.regularOf } : {}),
      window: { missionFrom: r.shift.from, missionTo: r.shift.to, dayFrom: d0, dayTo: d1, ...(r.at ? { place: r.at } : {}), ...(r.near !== undefined ? { near: r.near } : {}) },
      ...(r.leavesTo ? { leavesTo: r.leavesTo } : {}), ...(r.alone ? { alone: true } : {}), ...(r.waitsFor ? { waitsFor: r.waitsFor } : {}), tied, ...((r as { secret?: string }).secret ? { secret: (r as { secret?: string }).secret } : {}),
      reqs: reqs.filter(q => q.id.startsWith(`${r.id}-`)), src: (r as { src?: string }).src ?? '', note: (r as { note?: string }).note ?? '',
    };
  });

  // counts
  const tally = (f: (p: VPerson) => string) => { const m = new Map<string, number>(); for (const p of persons) m.set(f(p), (m.get(f(p)) ?? 0) + 1); return [...m].sort((x, y) => y[1] - x[1] || (x[0] < y[0] ? -1 : 1)).map(([k, n]) => ({ k, n })); };
  const counts: View['counts'] = {
    unit: tally(p => p.unit), occupation: tally(p => p.trade), position: tally(p => p.position), temperament: tally(p => p.tem), need: tally(p => p.need),
    shift: tally(p => (p.shift ? shiftText({ from: p.shift[0], to: p.shift[1] }) : 'no shift (resident)')),
  };

  // the tie graph: nodes by ties held and received, edges by kind, a seeded layout
  const kinds = rules.ties.kinds as Record<string, { direction: string; introduces: boolean; between: string; secret?: boolean }>;
  const edges: View['graph']['edges'] = [], seen = new Set<string>();
  for (const p of persons) for (const t of p.ties) {
    const dir = kinds[t.kind]?.direction !== 'both', k = dir ? `${t.kind}>${p.id}>${t.with}` : `${t.kind}|${[p.id, t.with].sort().join('|')}`;
    if (seen.has(k)) continue; seen.add(k);
    const [x, y] = !dir && p.id > t.with ? [t.with, p.id] : [p.id, t.with];
    edges.push({ a: x, b: y, kind: t.kind, dir, ...(t.at ? { at: t.at } : {}) });
  }
  const deg = new Map<string, number>(); for (const e of edges) { deg.set(e.a, (deg.get(e.a) ?? 0) + 1); deg.set(e.b, (deg.get(e.b) ?? 0) + 1); }
  const groups = ['cafe', 'records-office', ...scope.units.map(u => u.id).filter(u => u !== 'cafe' && u !== 'records-office'), 'resident'];
  const EW: Record<string, number> = { workmate: 1, family: 1.4, regular: 0.6, debtor: 0.5, rival: 0.3, lover: 1.2 };
  const pos = layout(persons.map(p => ({ id: p.id, group: groups.indexOf(p.unit) })), edges.map(e => ({ a: e.a, b: e.b, w: EW[e.kind] ?? 0.5 })));
  // components over every tie, either way
  const parent = new Map(persons.map(p => [p.id, p.id])); const find = (x: string): string => (parent.get(x) === x ? x : (parent.set(x, find(parent.get(x)!)), parent.get(x)!));
  for (const e of edges) parent.set(find(e.a), find(e.b));
  const comps = new Map<string, string[]>(); for (const p of persons) comps.set(find(p.id), [...(comps.get(find(p.id)) ?? []), p.id]);
  const components = [...comps.values()].sort((x, y) => y.length - x.length || byId(x[0]!, y[0]!));
  const kindCount: Record<string, number> = {}; for (const e of edges) kindCount[e.kind] = (kindCount[e.kind] ?? 0) + 1;
  const graph: View['graph'] = {
    w: GRAPH_W, h: GRAPH_H,
    nodes: persons.map(p => { const d = deg.get(p.id) ?? 0, [x, y] = pos.get(p.id)!; return { id: p.id, x, y, r: Math.round((4.2 + 1.5 * Math.sqrt(d)) * 10) / 10, deg: d }; }),
    edges, components, kinds: kindCount,
    cell: `The cell holds no ties at the start (rules knowledge.cellAtStart: ${a.people.knowledge?.cellAtStart ?? 'nothing'}; the cell is known by ${(a.people.knowledge?.identity ?? []).join('/')} only), so it is on the map at its start line and not in this graph.`,
  };

  const mission = casting.mission, startMin = a.ctx.atMin;
  const map = buildMap(plan, civ, scope, mission as never);

  // ---- the viewer's notices: what the people say of themselves that the checker does not read ----
  const notices: Notice[] = [];
  const say = (n: Notice) => notices.push(n);
  const planned = roles.filter(r => r.approach && r.pinned);
  const costs = planned.map(r => { const p = pById.get(r.pinned!)!, row = fits[p.fit]!.find(x => x.a === r.approach)!; return { r, p, row }; });
  for (const { r, p, row } of costs) if (row.fit !== 'suits') {
    const T = rules.temperaments[p.tem as keyof typeof rules.temperaments].name;
    say({ id: `fit-${r.id}`, level: 'warn', people: [p.id], title: `The ${r.id} (${p.name}) is ${T}: ${r.approach} is a wrong approach on them`,
      detail: `${row.fitWhy}. ${row.say} The Handover's plan for the ${r.id} is ${r.approach} (§07), at ${usdText(rules.approaches[r.approach as 'help'].usd)} for one.`, src: 'people.rules.json temperaments; rulebook fit (Sky Q4); mission.json roles' });
  }
  const helps = costs.filter(c => c.r.approach === 'help' && c.row.orders !== null);
  if (helps.length > 1) {
    const base = helps.reduce((n, c) => n + c.row.usd, 0), now = helps.reduce((n, c) => n + c.row.total.usd, 0);
    if (now !== base) say({ id: 'purse', level: now > PURSE_USD ? 'fail' : 'warn', people: helps.map(c => c.p.id), title: `Help on ${helps.map(c => c.r.id).join(' + ')} costs ${usdText(now)}, not ${usdText(base)}${now > PURSE_USD ? `: more than the ${usdText(PURSE_USD)} purse` : ''}`,
      detail: `${helps.map(c => `${c.r.id} ${c.p.name} (${c.p.tem}): ${c.row.orders} Help${c.row.orders === 1 ? '' : 's'} = ${usdText(c.row.total.usd)}`).join('; ')}. GDD §04: "help + driver = $80,000 fits [the $100,000 purse]"; the cast makes the second route dearer than the design says. M-2 passes because a wrong approach still works (only a refusal fails).`, src: 'GDD §04 purse; rulebook values worse-help-orders' });
  }
  for (const r of roles) { const p = r.pinned ? pById.get(r.pinned) : undefined; if (p?.talker) say({ id: `talker-${r.id}`, level: 'info', people: [p.id], title: `The ${r.id} (${p.name}) is a Talker from the start`, detail: `fear + resentment = ${p.talkerSums[0]} > bravery + loyalty = ${p.talkerSums[1]}: questioned, they give up what they know (rules talker).`, src: 'people.rules.json talker' }); }
  {
    // lovers (brief 011): the pairs, who they are and where they meet; and the Handover's waitress, who needs one
    const pairs = persons.filter(p => p.ties.some(t => t.kind === 'lover') && p.ties.find(t => t.kind === 'lover')!.with > p.id);
    if (pairs.length) say({ id: 'lovers', level: 'info', people: pairs.flatMap(p => [p.id, p.ties.find(t => t.kind === 'lover')!.with]).slice(0, 8), title: `${pairs.length} lover pair${pairs.length === 1 ? '' : 's'} (${P.filter(p => p.ties.some(t => t.kind === 'lover')).length} of ${P.length} people; the rules allow ${Math.round(((kinds.lover as { maxShare?: number } | undefined)?.maxShare ?? 1) * 100)}%)`,
      detail: `${pairs.slice(0, 6).map(p => { const q = pById.get(p.ties.find(t => t.kind === 'lover')!.with)!, t = p.ties.find(x => x.kind === 'lover')!; return `${p.name} (${p.position}) and ${q.name} (${q.position})${t.at ? `, met at their ${t.at}${t.by ? ' (they stop by)' : ''}` : ''}`; }).join(' · ')}. Nobody in the city is told a lover tie; the cell learns it with the affair (Listen, or a Watch that ends with the two together), and a turned contact's call summons the lover.`, src: 'people.rules.json ties.kinds.lover, secrets; briefs/011/motive-lovers.md' });
  }
  if (shut.length) {
    const ids = [...new Set(shut)].sort(byId);
    say({ id: 'shut-cafe', level: 'warn', people: ids, title: `${ids.length} ${ids.length === 1 ? 'person has' : 'people have'} a stop at the café while it is shut`,
      detail: `${ids.map(id => `${pById.get(id)!.name} (${id}): ${shutNotes.get(id)!.join('; ')}`).join(' · ')}. Keel's open question 2 (night-watch guards stop at a shut café): the checker does not read a unit's hours for a stop, and no rule says to drop the stop.`, src: 'life.json occupations hours; tools/people/day.ts cafeOpenAt' });
  }
  for (const row of a.src.cast.rows) {
    const claims = [...row.why.matchAll(/(\d+(?:\.\d+)?)\s*h\s+shifts?\b/gi)].map(m => Number(m[1]));
    if (!claims.length) continue;
    const us = scope.units.filter(u => `${u.area}/${u.trade}` === `${row.area}/${row.occupation}`);
    for (const u of us) {
      const lens = [...new Set(u.shifts.map(s => (((s.to - s.from) % 1440) + 1440) % 1440 / 60))];
      if (lens.some(l => claims.every(c => Math.abs(c - l) > 0.01))) say({ id: `shift-${u.id}`, level: 'warn', title: `${u.name}: the row says ${claims.map(c => `${c} h`).join('/')} shifts, the scope's shifts last ${lens.map(l => `${Math.floor(l)}:${String(Math.round((l % 1) * 60)).padStart(2, '0')}`).join('/')}`, detail: `"${row.why}" The scope tiles ${u.hours[0]}:00-${u.hours[1]}:00 into ${u.shifts.length} equal shifts (${u.shifts.map(s => shiftText(s)).join(', ')}); Keel's open question 1.`, src: 'tools/people/scope.ts shiftsOf' });
    }
  }
  if (src.run) {
    for (const c of checks) if (c.recorded !== undefined && c.recorded !== c.ok) say({ id: `recorded-${c.id}`, level: 'fail', title: `${c.id} was ${c.recorded ? 'ok' : 'failing'} in the run and is ${c.ok ? 'ok' : 'failing'} now`, detail: `${c.rule}: ${c.value}${c.detail ? ` (${c.detail})` : ''}` });
  }
  {
    // everyone who leaves one place in the same minute: a crowd that moves as one (the life file's handover event spreads leaving over a window; a routine that ends on the minute does not)
    const leave = new Map<string, string[]>();
    for (const p of P) for (const s of p.routine.stops) if (s.doing === 'work' && p.work && s.place === p.work.place) leave.set(`${s.place}@${s.to}`, [...(leave.get(`${s.place}@${s.to}`) ?? []), p.id]);
    const [key, ids] = [...leave].sort((x, y) => y[1].length - x[1].length || (x[0] < y[0] ? -1 : 1))[0] ?? ['', []];
    const win = (mission as { events?: { id: string; windowS?: number }[] }).events?.find(e => e.id === 'records-handover')?.windowS;
    if (ids.length >= 15) say({ id: 'crowd', level: 'info', people: ids.slice(0, 6), title: `${ids.length} people leave ${places[key.split('@')[0]!]?.name ?? key.split('@')[0]} in the same minute (${hm(Number(key.split('@')[1]))})`,
      detail: `They finish work at one minute and walk together, so the square fills as one crowd${win ? `; the mission's handover event gives leaving a ${win} s window, these routines do not spread across it` : ''}. Watch the map at ${hm(Number(key.split('@')[1]))}.`, src: 'mission.json events; routines' });
  }
  {
    // who can introduce whom among the four roles: a path along ties that introduce, each followed from the person who holds it (people.rules ties.kinds[k].introduces)
    const go = new Map<string, string[]>();
    for (const e of edges) if (kinds[e.kind]?.introduces) { go.set(e.a, [...(go.get(e.a) ?? []), e.b]); if (!e.dir) go.set(e.b, [...(go.get(e.b) ?? []), e.a]); }
    const hops = (a: string, b: string) => { const d = new Map([[a, 0]]), q = [a]; while (q.length) { const x = q.shift()!; for (const y of go.get(x) ?? []) if (!d.has(y)) { d.set(y, d.get(x)! + 1); q.push(y); } } return d.get(b); };
    const held = roles.filter(r => r.pinned), pairs: string[] = [];
    for (const x of held) for (const y of held) if (x !== y) { const h = hops(x.pinned!, y.pinned!); pairs.push(`${x.id} → ${y.id}: ${h === undefined ? 'no path' : `${h} introduction${h === 1 ? '' : 's'}`}`); }
    if (held.length > 1) say({ id: 'intro-roles', level: 'info', people: held.map(r => r.pinned!), title: 'Who can introduce whom among the Handover\'s four', detail: `${pairs.join(' · ')}. A turned contact introduces along ties that introduce, from the person who holds the tie (a regular tie is held by the customer, so a server cannot introduce their regulars).`, src: 'people.rules.json ties; rulebook contact.introduces' });
  }
  if (components.length > 1) say({ id: 'components', level: 'info', title: `The tie graph is ${components.length} separate groups`, detail: `${components.map(c => `${c.length} people (${c[0]}…)`).join(', ')}. An introduction cannot cross between groups.` });
  const pinnedCount = checks.filter(c => !c.ok).length;
  if (pinnedCount) say({ id: 'checks', level: 'fail', title: `${pinnedCount} check${pinnedCount === 1 ? '' : 's'} failing on these inputs`, detail: checks.filter(c => !c.ok).map(c => `${c.id}: ${c.detail || c.value}`).join(' · ') });
  if (a.gaps.length) say({ id: 'gaps', level: 'fail', title: `The expansion has ${a.gaps.length} gap${a.gaps.length === 1 ? '' : 's'}`, detail: a.gaps.slice(0, 8).join('; ') });
  const talkers = persons.filter(p => p.talker);
  say({ id: 'talkers', level: 'info', title: `${talkers.length} of ${persons.length} are Talkers at the start`, detail: `${talkers.length ? `by temperament ${[...new Set(talkers.map(p => p.tem))].map(t => `${t} ${talkers.filter(p => p.tem === t).length}`).join(', ')}` : 'nobody'}. A Talker gives the cell up when an investigator questions them.`, src: 'people.rules.json talker' });

  // ---- drift of the inputs a run was pinned to ----
  const cur = a.people.inputs as Record<string, string | undefined>, tools = await peopleHash();
  const drift: View['drift'] = (src.run?.pinned ?? []).map(x => ({ input: x.input, pinned: x.sha256, current: cur[x.input] ?? '', same: x.sha256 === cur[x.input] }));
  if (src.run?.toolsHash) drift.push({ input: 'people tools', pinned: src.run.toolsHash, current: tools, same: src.run.toolsHash === tools });
  const moved = drift.filter(d => !d.same).map(d => d.input);
  if (moved.length) say({ id: 'drift', level: 'warn', title: `Run ${src.run!.id} was pinned to other ${moved.join(', ')} than the repository has now`, detail: `This page expands the cast on the inputs now, as \`bun run people\` would. The run's own verdict (and its recorded checks) stand on its pinned inputs; the checks above are on the current ones.` });

  const units = scope.units.map(u => ({ id: u.id, name: u.name, trade: u.trade, staff: u.staffPerShift, hours: u.hours as [number, number], shifts: u.shifts.map(s => [s.from, s.to] as [number, number]), watch: (u.watch ?? []).map(s => [s.from, s.to] as [number, number]), people: persons.filter(p => p.unit === u.id).length }));
  // the tables as the rulebook reads them: the need `secret` is the row's record share (tools/people/secrets.ts)
  const rowsOut = withSecrets(a.src.cast, rules).rows.map(r => ({
    key: `${r.area}/${r.occupation}`, why: r.why, positions: r.positions.map(p => `${p.position} ${p.weight}${p.count ? ` ×${p.count}` : ''}`).join(', '),
    temperaments: r.temperaments.map(t => ({ k: t.temperament, w: t.weight })), needs: r.needs.map(n => ({ k: n.need + (n.positions ? `[${n.positions.join('/')}]` : ''), w: n.weight })),
    secrets: (r.secrets ?? []).map(s => ({ k: s.secret, w: s.share })), lovers: r.ties.filter(t => t.kind === 'lover').map(t => `${t.share}${t.with ? ` with a ${t.with}` : ''}${t.at ? ` at ${t.at}` : ''}${t.positions ? ` [${t.positions.join('/')}]` : ''}${t.posts ? ` at the ${t.posts.join('/')}` : ''}`),
    people: P.filter(p => rowKey(p, w) === `${r.area}/${r.occupation}`).length,
  }));

  const view: View = {
    schema: VIEW_SCHEMA, id: src.id, title: `The people of ${scope.id}: ${src.label}`,
    source: { kind: src.kind, label: src.label, file: src.file, castSha: src.castSha, ...(src.promotedFrom ? { promotedFrom: src.promotedFrom } : {}) },
    plan: { id: plan.id, version: plan.version }, scope: scope.id, inputs: a.people.inputs, ...(src.run ? { run: src.run } : {}), drift, checks,
    rules: {
      steps: Object.fromEntries(Object.entries(rules.scales).map(([k, v]) => [k, [...((v as { steps?: readonly string[] }).steps ?? [])]])),
      temperaments: Object.fromEntries(Object.entries(rules.temperaments).map(([k, t]) => [k, { name: t.name, respondsTo: [...t.respondsTo], refuses: [...t.refuses], reportsApproach: t.reportsApproach, hero: t.hero, menace: t.menace }])),
      needs: Object.fromEntries(Object.entries(rules.needs.list).map(([k, n]) => [k, { metBy: [...n.metBy], help: (n as { help: string }).help, requires: n.requires }])),
      approaches: Object.fromEntries((['ask', 'pay', 'help', 'pressure'] as const).map(k => [k, { ...rules.approaches[k] }])),
      ties: Object.fromEntries(Object.entries(kinds).map(([k, t]) => [k, { direction: t.direction, introduces: t.introduces, between: t.between, ...(t.secret ? { secret: true } : {}) }])), rulebook: !!rb,
      secrets: Object.fromEntries(Object.entries(rules.secrets?.list ?? {}).map(([k, s]) => [k, { requires: s.requires, learnedBy: [...s.learnedBy] }])),
    },
    units, rows: rowsOut, castNotes: src.run?.notes ?? [], mission: { startsAt: mission.clock.startsAt, startMin, end: Math.max(...roles.map(r => r.window.dayTo)) },
    places, persons, fits, roles, counts, graph, map, notices, gaps: a.gaps, genNotes: a.notes, toolsHash: tools,
  };

  // ---- against another cast ----
  const againstSpec = o.against === 'none' ? undefined : o.against ?? (src.kind === 'promoted' ? undefined : 'promoted');
  if (againstSpec) {
    let other: Source | undefined;
    try { other = loadSource(plan.id, againstSpec); } catch (e) { if (o.against) throw e; other = undefined; }
    if (other && other.castSha !== src.castSha) {
      const b = expandCast(w, other), Q = b.people.persons, bm = new Map(Q.map(p => [p.id, p]));
      view.against = { kind: other.kind, label: other.label, id: other.id, castSha: other.castSha, ...(other.run ? { run: other.run.id } : other.promotedFrom ? { run: other.promotedFrom } : {}) };
      const added: string[] = [], changed: string[] = [], same: string[] = [], removed = Q.filter(q => !by.has(q.id)).map(q => q.id).sort(byId);
      let renamed = 0, shared = 0;
      for (const p of persons) {
        const old = bm.get(p.id);
        if (!old) { p.diff = { status: 'added', fields: [] }; added.push(p.id); continue; }
        shared++;
        const fa = flat(old, rules), fb = flat(by.get(p.id)!, rules), fields = Object.keys(fb).filter(k => fa[k] !== fb[k]).map(k => [k, fa[k] ?? '', fb[k] ?? ''] as [string, string, string]);
        if (fa.name !== fb.name) renamed++;
        p.diff = { status: fields.length ? 'changed' : 'same', fields };
        (fields.length ? changed : same).push(p.id);
      }
      const dimOf = (people: Person[]) => ({
        unit: tallyOf(people, unitOf), occupation: tallyOf(people, p => p.work?.trade ?? 'resident'), position: tallyOf(people, p => p.position), temperament: tallyOf(people, p => p.temperament), need: tallyOf(people, p => p.need),
        shift: tallyOf(people, p => (p.work ? shiftText(p.work.shift) : 'no shift (resident)')),
      });
      const da = dimOf(Q), db = dimOf(P), dims: NonNullable<View['compare']>['dims'] = {};
      for (const d of Object.keys(db) as (keyof typeof db)[]) dims[d] = [...new Set([...Object.keys(da[d]), ...Object.keys(db[d])])].sort().map(k => ({ k, a: da[d][k] ?? 0, b: db[d][k] ?? 0 }));
      const rkeys = [...new Set([...other.cast.rows, ...src.cast.rows].map(r => `${r.area}/${r.occupation}`))].sort();
      const oc = withSecrets(other.cast, rules).rows, sc = withSecrets(src.cast, rules).rows;
      const rows = rkeys.map(key => ({ key, ...rowDiff(oc.find(r => `${r.area}/${r.occupation}` === key), sc.find(r => `${r.area}/${r.occupation}` === key)) }));
      const nm = (c: Cast, k: 'given' | 'family') => new Set(c.names[k].map(x => x.name));
      const kept = [...nm(src.cast, 'given')].filter(x => nm(other!.cast, 'given').has(x)).length + [...nm(src.cast, 'family')].filter(x => nm(other!.cast, 'family').has(x)).length;
      view.compare = {
        added, removed, changed, same, renamed, shared, dims, rows, otherChecks: runChecks(b).map(c => ({ id: c.id, ok: c.ok })), names: { given: [other.cast.names.given.length, src.cast.names.given.length], family: [other.cast.names.family.length, src.cast.names.family.length], kept },
        gone: removed.map(id => { const q = bm.get(id)!; return { id, name: q.name, unit: unitOf(q), position: q.position, tem: q.temperament, need: q.need }; }),
      };
      const worse = view.compare.otherChecks.filter(c => !c.ok).map(c => c.id), mine = new Map(checks.map(c => [c.id, c.ok])), fixed = worse.filter(id => mine.get(id));
      if (worse.length) view.notices.unshift({ id: 'against-checks', level: 'info', title: `Against ${other.id}: ${worse.join(', ')} fail${worse.length === 1 ? 's' : ''} there${fixed.length ? `; ${fixed.length === worse.length ? 'this cast passes' : `this cast passes ${fixed.join(', ')}`}` : ''}`, detail: `The check results on ${other.label.split(' (')[0]}: ${view.compare.otherChecks.map(c => `${c.id} ${c.ok ? 'ok' : 'FAIL'}`).join(', ')}. M-2 is the Handover's casting: ${worse.includes('M-2') ? 'the cast now promoted cannot cast the Handover (a driver, a guard and a councillor are missing); a cast that passes M-2 can' : 'see the check'}.` });
    }
  }
  return view;
}

const tallyOf = (people: Person[], f: (p: Person) => string) => { const m: Record<string, number> = {}; for (const p of people) m[f(p)] = (m[f(p)] ?? 0) + 1; return m; };
export { sha256 };
