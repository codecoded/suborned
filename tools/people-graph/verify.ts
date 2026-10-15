// Hold a loaded database to the JSON it was made from: every row it holds against the Model (read-back), and each canned query's answer against the TypeScript reference worked out from
// the people file (reference.ts). Read-only on the server. `bun run people:graph verify` runs it; the tests run it only when PEOPLE_GRAPH_LIVE=1.
import { sql } from '../rag/database';
import { rowCounts } from './load';
import type { Edge, Model, Node } from './model';
import { Rec } from './model';
import { readQuery, runQuery } from './queries';
import * as ref from './reference';
import type { Prepared } from './source';
import { TABLES } from './surql';

export type Check = { name: string; ok: boolean; detail?: string };

// ---- read-back
const RECORD = /^([a-z_]+):`?([^`]+?)`?$/;
const norm = (v: unknown): unknown => {
  if (v instanceof Rec) return `${v.table}:${v.id}`;
  if (typeof v === 'string') { const m = v.match(RECORD); return m && !v.includes(' ') ? `${m[1]}:${m[2]}` : v; }
  if (Array.isArray(v)) return v.map(norm);
  if (v && typeof v === 'object') return Object.fromEntries(Object.entries(v).filter(([, x]) => x !== undefined && x !== null).sort(([a], [b]) => ref.cmp(a, b)).map(([k, x]) => [k, norm(x)]));
  return v;
};
const rowOf = (x: Node | Edge) => norm('from' in x ? { id: new Rec(x.table, x.id), in: x.from, out: x.to, ...x.fields } : { id: new Rec(x.table, x.id), ...x.fields }) as Record<string, unknown>;

export async function readBack(db: string, m: Model): Promise<Check[]> {
  const checks: Check[] = [], have = await rowCounts(db), want = { ...m.counts.nodes, ...m.counts.edges } as Record<string, number>;
  const off = TABLES.filter(t => (have[t] ?? 0) !== (want[t] ?? 0));
  checks.push({ name: 'row counts per table', ok: !off.length, detail: off.length ? off.map(t => `${t}: database ${have[t]}, model ${want[t] ?? 0}`).join('; ') : `${Object.values(want).reduce((a, b) => a + b, 0)} rows in ${TABLES.length} tables` });
  const rows = await sql<Record<string, unknown>[]>(TABLES.map(t => `SELECT * FROM ${t};`).join('\n'), db);
  TABLES.forEach((t, i) => {
    const got = new Map((rows[i] ?? []).map(r => { const n = norm(r) as Record<string, unknown>; return [String(n.id), JSON.stringify(n)]; }));
    const exp = [...m.nodes, ...m.edges].filter(x => x.table === t).map(x => rowOf(x));
    const bad = exp.filter(e => got.get(String(e.id)) !== JSON.stringify(e));
    checks.push({ name: `rows of ${t} equal the model's`, ok: !bad.length, detail: bad.length ? `${bad.length} differ, e.g. ${JSON.stringify(bad[0])} against ${got.get(String(bad[0]!.id))}` : undefined });
  });
  return checks;
}

// ---- the canned queries against the reference
const same = (a: unknown, b: unknown) => JSON.stringify(a) === JSON.stringify(b);
const show = (x: unknown) => JSON.stringify(x).slice(0, 220);
const check = (name: string, ok: boolean, got: unknown, want: unknown): Check => ({ name, ok, detail: ok ? undefined : `database ${show(got)} against reference ${show(want)}` });
const ask = async (db: string, name: string, ...args: string[]) => (await runQuery(db, readQuery(name), args)) as any;
const mm = (t: string) => { const [h, m] = t.split(':').map(Number); return h! * 60 + m!; };

/** People worth asking about: the cast of every Handover role, and an even sample of the rest. */
export function sample(p: Prepared, n = 10): string[] {
  const ids = p.people.persons.map(x => x.id).sort(ref.cmp), pinned = p.extras.casts.flatMap(c => (c.pinned ? [c.pinned] : [])), step = Math.max(1, Math.floor(ids.length / n));
  return [...new Set([...pinned, ...ids.filter((_, i) => i % step === 0).slice(0, n)])];
}

export async function verifyQueries(db: string, p: Prepared): Promise<Check[]> {
  const out: Check[] = [], r: ref.Ref = { people: p.people, rules: p.w.rules, extras: p.extras }, people = sample(p);
  const ids = p.people.persons.map(x => x.id).sort(ref.cmp);

  // who-can-introduce: the role people pairwise, and an even spread of pairs
  const pins = p.extras.casts.flatMap(c => (c.pinned ? [c.pinned] : [])), pairs: [string, string][] = [];
  for (const a of pins) for (const b of pins) if (a < b) pairs.push([a, b]);
  for (let i = 0; i < ids.length; i += 5) pairs.push([ids[i]!, ids[(i * 7 + 13) % ids.length]!]);
  const bad: string[] = []; let unreachable = 0, longest = 0;
  for (const [a, b] of pairs) {
    const got = await ask(db, 'who-can-introduce', a, b), want = ref.introHops(r, a, b);
    if (want === undefined) unreachable++; else longest = Math.max(longest, want);
    const steps = got.steps as { from: string; to: string; via: { kind: string }[] }[];
    const chain = steps.length ? [steps[0]!.from, ...steps.map(s => s.to)] : [];
    const ok = (got.hops ?? undefined) === want && steps.length === (want ?? 0) && (!steps.length || (chain[0] === a && chain.at(-1) === b))
      && steps.every((s, i) => (i === 0 || steps[i - 1]!.to === s.from) && same(s.via.map(v => v.kind).sort(), ref.introKinds(r, s.from, s.to)) && ref.introKinds(r, s.from, s.to).length > 0);
    if (!ok) bad.push(`${a} -> ${b}: database ${show({ hops: got.hops, chain })}, reference ${want}`);
  }
  out.push({ name: `who-can-introduce: ${pairs.length} pairs (longest chain ${longest}, ${unreachable} unreachable) equal the reference's shortest introducing chains`, ok: !bad.length, detail: bad.slice(0, 3).join('; ') });

  // regulars-of
  const places = [...new Set([...p.extras.units.flatMap(u => [u.door, u.official, u.bay]).filter((x): x is string => !!x), p.extras.cafeSide[0]!])];
  const askedAt: (string | undefined)[] = [undefined, '09:13', '17:35'];
  const badR: string[] = []; let regs = 0;
  for (const place of places) for (const at of askedAt) {
    const got = (await ask(db, 'regulars-of', place, ...(at ? ['at', at] : []))) as { id: string; regular_of: string[]; stops: { place: string; from_min: number; to_min: number; doing: string }[] }[];
    const want = ref.regularsOf(r, place, at ? mm(at) : undefined), g = got.map(x => ({ id: x.id, regular_of: x.regular_of, stops: x.stops.map(s => `${s.place}@${s.from_min}-${s.to_min}:${s.doing}`).sort(ref.cmp) }));
    regs += want.length;
    if (!same(g, want)) badR.push(`${place} ${at ?? 'any time'}: database ${show(g)}, reference ${show(want)}`);
  }
  out.push({ name: `regulars-of: ${places.length} places x ${askedAt.length} times (${regs} regulars) equal the reference`, ok: !badR.length, detail: badR[0] });

  // who-is-near
  const badN: string[] = []; let near = 0, asked = 0;
  const spots = [p.extras.units.find(u => u.id === p.extras.cafeUnit)!.door!, p.extras.cafeSide[0]!, p.extras.units.find(u => u.official)!.official!];
  for (const place of spots) for (const at of ['07:30', '09:15', '17:35']) for (const radius of [13, 4]) {
    const got = (await ask(db, 'who-is-near', place, at, String(radius))) as { person: string; place: string; metres: number; doing: string; from: string; to: string }[];
    const want = ref.whoIsNear(r, place, mm(at), radius), key = (x: { person: string; place: string }) => `${x.person}@${x.place}`;
    near += want.length; asked++;
    const g = [...got].sort((a, b) => ref.cmp(key(a), key(b))), w = [...want].sort((a, b) => ref.cmp(key(a), key(b)));
    const ok = g.length === w.length && g.every((x, i) => key(x) === key(w[i]!) && x.doing === w[i]!.doing && mm(x.from) === w[i]!.from && mm(x.to) === w[i]!.to && Math.abs(x.metres - w[i]!.metres) <= 0.06)
      && got.every((x, i) => i === 0 || got[i - 1]!.metres <= x.metres);
    if (!ok) badN.push(`${place} ${at} ${radius} m: database ${show(got.map(key))}, reference ${show(want.map(key))}`);
  }
  out.push({ name: `who-is-near: ${asked} place/time/radius questions (${near} stops, nearest first) equal the reference`, ok: !badN.length, detail: badN[0] });

  // guards-who-are-cafe-regulars
  {
    const got = ((await ask(db, 'guards-who-are-cafe-regulars')) as { id: string; regular_of: { staff: string }[] }[]).map(x => ({ id: x.id, staff: x.regular_of.map(s => s.staff).sort(ref.cmp) }));
    const want = ref.regularsOfUnit(r, 'guard', p.extras.cafeUnit);
    out.push(check(`guards-who-are-cafe-regulars: ${want.length} guards equal the reference`, same(got, want) && want.length > 0, got, want));
  }

  // needs-known-by-keeper: every keeper, and one person who is not
  {
    const keepers = (p.people.knowledge?.needsKnown ?? []).map(k => k.keeper), bad2: string[] = []; let n = 0;
    for (const k of [...keepers, ids.find(i => !keepers.includes(i))!]) {
      const got = ((await ask(db, 'needs-known-by-keeper', k)) as { id: string; need: string; temperament: string; via: string[] }[]).map(x => ({ id: x.id, need: x.need, temperament: x.temperament, via: x.via }));
      const want = ref.needsKnownBy(r, k); n += want.length;
      if (!same(got, want)) bad2.push(`${k}: database ${show(got)}, reference ${show(want)}`);
    }
    out.push({ name: `needs-known-by-keeper: ${keepers.length} keepers (${n} needs known) and a non-keeper equal the reference`, ok: !bad2.length, detail: bad2[0] });
  }

  // role-fit
  {
    type Fit = { role: string; people: { id: string; pinned: boolean; need: string; temperament: string; need_met: boolean; approach_ok: boolean; regular_ok: boolean; ties: { kind: string; with: string; mutual: boolean; holds: boolean }[] }[] };
    const got = ((await ask(db, 'role-fit')) as Fit[]).map(x => ({ role: x.role, people: x.people.map(q => ({ id: q.id, pinned: q.pinned, need: q.need, temperament: q.temperament, need_met: q.need_met, approach_ok: q.approach_ok, regular_ok: q.regular_ok,
      ties: q.ties.map(t => `${t.kind}${t.mutual ? '=' : t.holds ? '>' : '<'}${t.with}`).sort(ref.cmp) })) }));
    const want = ref.roleFit(r);
    out.push(check(`role-fit: ${want.length} roles, ${want.reduce((n, x) => n + x.people.length, 0)} cast people with their ties equal the reference`, same(got, want), got, want));
  }

  // neighbourhood and graph-view
  {
    const bad3: string[] = [], bad4: string[] = [];
    for (const id of people) {
      const got = await ask(db, 'neighbourhood', id), want = ref.neighbourhood(r, id);
      const g = { hop1: got.hop1.map((x: { id: string }) => x.id), hop2: got.hop2.map((x: { id: string }) => x.id) };
      if (!same(g, want) || got.person.id !== id) bad3.push(`${id}: database ${show(g)}, reference ${show(want)}`);
      for (const hops of [1, 2]) {
        const view = (await ask(db, 'graph-view', id, String(hops))) as { id: string; in?: string }[];
        const nodes = new Set(view.filter(x => !x.in && x.id.startsWith('person:')).map(x => x.id.replace(/^person:`?|`$/g, '')));
        const exp = new Set([id, ...want.hop1, ...(hops === 2 ? want.hop2 : [])]);
        if (!same([...nodes].sort(ref.cmp), [...exp].sort(ref.cmp))) bad4.push(`${id} ${hops} hop(s): ${nodes.size} people against ${exp.size}`);
      }
    }
    out.push({ name: `neighbourhood: ${people.length} people, 1 and 2 hops, equal the reference`, ok: !bad3.length, detail: bad3[0] });
    out.push({ name: 'graph-view: the same people, at 1 and 2 hops, as plain records', ok: !bad4.length, detail: bad4[0] });
  }

  // counts
  {
    const c = await ask(db, 'counts'), m = p.model.counts;
    const want = { people: m.nodes.person, edges: m.edges, ties: Object.entries(m.ties).map(([kind, n]) => ({ kind, n })) };
    out.push(check('counts: people, edges per table and ties per kind equal the model', c.people === want.people && same(c.edges, want.edges) && same(c.ties, want.ties), c, want));
  }
  return out;
}

export async function verify(db: string, p: Prepared): Promise<Check[]> {
  return [...(await readBack(db, p.model)), ...(await verifyQueries(db, p))];
}
