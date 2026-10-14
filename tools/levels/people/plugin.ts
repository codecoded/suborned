// The people objective (brief 010 B), a plug-in of the generation engine (tools/gen), copied from the skyline template:
// Pulse proposes the cast tables for a scope of the city, tools/people expands them into individuals and the people checker
// (P-0..P-12) holds both to the people rules, Keel critiques a cast that passed, faults go back to Pulse. The model writes
// mixes; code makes the people. No model call is needed to run it end to end (--preview answers with a stand-in cast).
import { z } from 'zod';
import type { Fault, Plugin } from '../../gen/plugin';
import { writeJson } from '../../rag/config';
import { expand } from '../../people/expand';
import { readTexts, world, type Texts, type World } from '../../people/inputs';
import { standInCast } from '../../people/standin';
import { requirements } from '../../people/review';
import { SCOPES } from '../../people/scope';
import { pinPlan, readPlan } from '../pins';
import { recordPeople, peopleLedger, peopleLedgerFile } from './drift';
import { peopleHash } from './hash';
import { objectives } from './objectives';
import { pinInputs } from './pins';
import { peopleReplay } from './replay';
import { checkCast } from './cast';
import { agents, type PulseOut } from './schemas';
import { dayView, rulesView, scopeView } from './view';

type State = { texts: Texts; w: World; scope: string; rdir: string; planId: string };
const j = (x: unknown) => JSON.stringify(x);
const OBJECTIVE = 'People (brief 010): the cast tables of a scope of the city, from its places, trades and routines; accepted only when the people'
  + ' checker (P-rules: reach, one clear need, only what the rules allow, staff counts, keep-clear, the Handover\'s roles, ties, names, determinism) is clean and the critic passes.';

/** The cast as Keel reads it: per row the mix, and who the expansion made of it. */
export function castSummary(w: World, people: ReturnType<typeof expand>['people']) {
  const by = new Map<string, Record<string, number>>();
  for (const p of people.persons) {
    const k = p.work ? `${p.work.area}/${p.work.trade}` : `${w.scope.homes.find(h => h.plot === p.home.plot)?.area ?? '?'}/resident`;
    const m = by.get(k) ?? {}; m[p.temperament] = (m[p.temperament] ?? 0) + 1; by.set(k, m);
  }
  return [...by].map(([row, t]) => `${row}: ${Object.entries(t).map(([k, n]) => `${k} ${n}`).join(', ')}`).join('\n');
}

export const peoplePlugin: Plugin<State> = {
  id: 'people', defaultInput: 'contracts/world/harbour-district.plan.json', scopes: SCOPES,
  builders: [{ key: 'pulse', owner: objectives.pulse.owner, objective: objectives.pulse, schema: agents.pulse }],
  critic: { key: 'keel', owner: 'Keel', objective: objectives.keel, schema: agents.keel },
  header: { proposals: 'cast tables' },
  code: {
    label: 'tools/people (scope, expand, check, walk), tools/plan, contracts/people, tools/levels/people',
    hash: peopleHash, ledger: peopleLedger, ledgerFile: peopleLedgerFile,
    declare: 'bun tools/levels/people/drift.ts declare', moves: true, record: recordPeople,
  },
  async open(path, scope = SCOPES[0]) {
    if (!(SCOPES as readonly string[]).includes(scope)) throw new Error(`unknown scope ${scope}`);
    const plan = await readPlan(path), texts = await readTexts(path);
    const w = world(texts, scope);
    return { id: plan.id, version: plan.version, async pin(rdir) {
      const { record } = await pinPlan(plan, rdir);
      const inputs = await pinInputs(rdir, texts, plan.id);
      return { state: { texts, w, scope, rdir, planId: plan.id }, record: { ...record, ...inputs, scope } };
    } };
  },
  async task(s, _r, _key) {
    return `Task: propose the cast tables for the scope ${s.scope}.\n${scopeView(s.w)}\n${rulesView(s.w)}\n${dayView(s.w)}\n`
      + `The Handover mission is pinned; do not edit it. Output the full cast (schema suborned.cast/1, scope "${s.scope}").`;
  },
  async assess(s, r) {
    const out = r.outs.pulse as PulseOut, { checked, people } = checkCast(s.w, out.cast);
    const failed = checked.checks.filter(c => !c.ok);
    await writeJson(`${r.rd}/checks.json`, { plan: s.planId, version: r.version, scope: s.scope, ok: checked.ok, counts: checked.counts, checks: checked.checks, handover: checked.handover });
    await writeJson(`${r.od}/people.json`, people);
    return {
      ok: checked.ok, line: `checker ${checked.ok ? 'clean' : `FAIL ${failed.map(c => c.id).join(',')}`} (${checked.counts.persons} people, ${checked.counts.units} units)`,
      record: { checkerOk: checked.ok, failedChecks: failed.map(c => c.id), problems: [], persons: checked.counts.persons },
      failures: failed.map(c => ({ rule: c.id, owners: ['Pulse'], fault: `${c.rule}: ${c.value} (limit ${c.limit})${c.detail ? '; ' + c.detail : ''} [source: ${c.source}]`, fix: 'bring the cast tables inside the rule' })),
      faults: [] as Fault[],
      criticTask(cited, lastFaults) {
        const revision = r.n > 1 || lastFaults.length ? `\n\nThis is a revision. The faults from last round (JSON), to recheck:\n${j(lastFaults)}` : '';
        return `Task: judge the cast tables.\nObjective under test: ${OBJECTIVE}\n\n`
          + `Output schema (JSON): ${j(z.toJSONSchema(agents.pulse))}\n\n`
          + `${scopeView(s.w)}\n\nPeople checker report on the cast and the people it expands to:\n${checked.checks.map(c => `${c.ok ? 'ok  ' : 'FAIL'} ${c.id} ${c.rule}: ${c.value}${c.ok ? '' : ` | ${c.detail}`}`).join('\n')}\n\n`
          + `Who the expansion made (temperaments per row):\n${castSummary(s.w, people)}\n\nHandover cast requirements (Motive §3):\n${requirements(s.w.ctx(out.cast), people).map(q => `${q.ok ? 'met    ' : 'NOT MET'} ${q.need}: ${q.detail}`).join('\n')}\n\nHandover casting: ${checked.handover.map(h => `${h.role} ${h.ok ? 'ok' : 'NOT CAST'} (${h.why})`).join('; ')}\n\n`
          + `Candidate cast tables (JSON): ${j(out)}\n\n`
          + `What the builder retrieved and cites. Code has already checked that every cited id was retrieved by its own call: raise no fault for citation membership. Judge whether the claims are true to the cited text:\n${cited}${revision}`;
      },
    };
  },
  ownersOf: rule => (/^P-\d+$/.test(rule) ? ['Pulse'] : undefined),
  standIn: (s, key) => (key === 'pulse' ? { cast: standInCast(s.w.scope, s.w.rules), notes: [], sources: [] } satisfies PulseOut : undefined),
  replay: peopleReplay,
};
