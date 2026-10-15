// Where the graph's people come from, expanded exactly as `bun run people` expands them (tools/people/cli.ts): the promoted cast (held to its run first), a people run's accepted
// cast (--run r015), or a cast file (--cast). Then the model, its hash and the database it loads into. No server is touched here.
import { existsSync, readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { Cast } from '../../contracts/people/cast.schema';
import type { People } from '../../contracts/people/people.schema';
import { sha256 } from '../gen/sha';
import { checkPeople } from '../people/check';
import { expand } from '../people/expand';
import { castHandover, type MissionIn } from '../people/handover';
import { readTexts, world, type World } from '../people/inputs';
import { castFileOf, checkPromotion } from '../people/promoted';
import { root } from '../rag/config';
import { buildModel, SCHEMA_VERSION, type Extras, type Model, type RoleCast, type RoleIn } from './model';

export const PLAN_FILE = 'contracts/world/harbour-district.plan.json';
export const SCHEMA_FILE = 'tools/people-graph/schema.surql';
export const peopleDb = (hash: string) => `p_${hash.slice(0, 16)}`;

export type CastSource = { kind: 'promoted' | 'run' | 'file'; label: string; cast: Cast; problems: string[] };
export type SourceOptions = { run?: string; cast?: string; plan?: string };

/** The accepted cast of a people run: the last round that ran Pulse (a round that reused its cast has no pulse.json of its own), as runs/levels/<plan>/<run>/round-N/pulse.json holds it. */
export function runCast(planId: string, run: string, base: string = root): { cast: unknown; label: string; outcome: string; file: string } {
  if (!/^r\d{3}$/.test(run)) throw new Error(`--run takes a run id like r015, not ${JSON.stringify(run)}`);
  const dir = `runs/levels/${planId}/${run}`, rd = (f: string) => resolve(base, f);
  if (!existsSync(rd(`${dir}/run.json`))) throw new Error(`no run ${run}: ${dir}/run.json is missing`);
  const rec = JSON.parse(readFileSync(rd(`${dir}/run.json`), 'utf8')) as { outcome?: string; rounds?: unknown[] };
  let round = rec.rounds?.length ?? 0;
  while (round > 0 && !existsSync(rd(`${dir}/round-${round}/pulse.json`))) round--;
  if (!round) throw new Error(`run ${run} has no round with a pulse.json`);
  const file = `${dir}/round-${round}/pulse.json`, cast = (JSON.parse(readFileSync(rd(file), 'utf8')) as { cast?: unknown }).cast;
  if (!cast) throw new Error(`${file} has no cast`);
  return { cast, label: `run ${run} round ${round} (${rec.outcome ?? 'no outcome'})`, outcome: rec.outcome ?? '', file };
}

export function castSource(planId: string, o: SourceOptions, base: string = root): CastSource {
  if (o.run && o.cast) throw new Error('--run and --cast are two sources: give one');
  if (o.cast) {
    const file = resolve(base, o.cast);
    if (!existsSync(file)) throw new Error(`no cast file ${o.cast}`);
    return { kind: 'file', label: `cast ${o.cast}`, cast: Cast.parse(JSON.parse(readFileSync(file, 'utf8'))), problems: [] };
  }
  if (o.run) {
    const r = runCast(planId, o.run, base);
    return { kind: 'run', label: r.label, cast: Cast.parse(r.cast), problems: r.outcome === 'ACCEPTED' ? [] : [`run ${o.run} is ${r.outcome || 'not accepted'}: only an ACCEPTED run's cast is promotable`] };
  }
  const promo = checkPromotion(planId, base);
  if (!promo.ok) return { kind: 'promoted', label: promo.label, cast: Cast.parse(JSON.parse(readFileSync(resolve(base, castFileOf(planId)), 'utf8'))), problems: promo.problems.map(p => `promoted cast: ${p}`) };
  return { kind: 'promoted', label: `promoted ${promo.label}`, cast: Cast.parse(JSON.parse(promo.castText!)), problems: [] };
}

/** The Handover's roles cast from these people, as the names are cast (tools/people/roles.ts castForNames), and which conditions held: all (play), the rules' (rules), or place alone for a role the rules cannot cast. */
export function castRolesOf(people: People, w: World, ctx: { casting?: { scope: World['scope']; mission: MissionIn; plan: { civic: unknown } } }): { casts: RoleCast[]; mode: Extras['castMode'] } {
  const c = ctx.casting!, run = (opts?: { play: true }, rules = true) => castHandover(people.persons, c.scope, c.mission, c.plan, rules ? w.rules : undefined, opts);
  const play = run({ play: true });
  if (play.every(x => x.ok)) return { casts: play, mode: 'play' };
  const strict = run();
  if (strict.every(x => x.ok)) return { casts: strict, mode: 'rules' };
  const place = run(undefined, false);
  return { casts: strict.map((x, i) => (x.ok ? x : place[i]!)), mode: 'place' };
}

export type Prepared = {
  source: CastSource; w: World; people: People; model: Model; hash: string; db: string; checks: { ok: boolean; failed: string[]; gaps: string[]; notes: number };
  inputs: People['inputs']; plan: string; extras: Extras;
};

export async function prepare(o: SourceOptions = {}): Promise<Prepared> {
  const texts = await readTexts(o.plan ?? PLAN_FILE), w = world(texts, 'civic-block'), source = castSource(w.plan.id, o);
  const ctx = w.ctx(source.cast), gen = expand(ctx), checked = checkPeople(ctx, w.mission, w.plan, gen);
  const { casts, mode } = castRolesOf(gen.people, w, ctx);
  const extras: Extras = {
    units: w.scope.units.map(u => ({ id: u.id, door: u.door, official: u.official, bay: u.bay })), cafeUnit: w.scope.cafeUnit, cafeSide: [...w.scope.cafeSide],
    sets: w.scope.sets.map(s => ({ id: s.id, seats: s.seats.map(x => x.place) })), roles: w.mission.roles as RoleIn[], casts, castMode: mode,
  };
  const model = buildModel(gen.people, w.rules, extras);
  const schema = await Bun.file(resolve(root, SCHEMA_FILE)).text(), hash = hashOf(model, schema);
  return {
    source, w, people: gen.people, model, hash, db: peopleDb(hash), inputs: gen.people.inputs, plan: `${w.plan.id} ${w.plan.version}`, extras,
    checks: { ok: checked.ok, failed: checked.checks.filter(c => !c.ok).map(c => `${c.id} ${c.rule}: ${c.value}`), gaps: gen.gaps, notes: gen.notes.length },
  };
}

/** One database per people hash: the model (people, rules lookups, places, roles and who is cast) and the schema it is loaded under, with its version. Another expander that makes the same people loads the same graph. */
export const hashOf = (model: Model, schemaText: string) => sha256(JSON.stringify({ v: SCHEMA_VERSION, schema: sha256(schemaText), model }));
