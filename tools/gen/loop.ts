// One generation run (brief 007 F): the builders propose in order, the objective applies and checks the candidate,
// Keel judges a candidate that passes the checks (rules.ts), and faults go back to their owner (≤ 3 rounds). A run
// can start from a rejected run's last candidate (continue.ts). Every call is recorded under runs/ (committed) and
// out/ (regenerated). Nothing here knows what is being generated: see plugin.ts.
import { mkdir } from 'node:fs/promises';
import { resolve } from 'node:path';
import { runAgent, type Call } from '../rag/agent';
import { CHUNKER, hash, loadCorpus } from '../rag/corpus';
import { writeJson } from '../rag/config';
import type { retrieve } from '../rag/retrieval';
import { DEFAULT_MAX_USD, SpendCeiling, spentUsd, withCeiling } from './ceiling';
import { ContinueRefused, loadParent, pinInherited, startFrom } from './continue';
import { replayParent } from './continue-parent';
import { crewOf, feedbackLine, handoffText } from './crew';
import { currentGdd } from './gdd';
import { gitState } from './gitstate';
import { candidateVersion, nextRun, outRoot, pad, runsRoot } from './paths';
import { pinKit } from './pin-kit';
import type { Fault, Member, Opts, Plugin, Round, Verdict } from './plugin';
import { PASS, previewInvoke } from './preview';
import { criticRuns, LOOP, silentFaults } from './rules';
import { tokensOf } from './tokens';

const j = (x: unknown) => JSON.stringify(x);

export async function generate<S>(p: Plugin<S>, o: Opts) {
  // first, before the run folder (untracked, so `dirty`) or any pin is written: r011 recorded dirty from a clean tree because this was read after
  const git = (o.git ?? gitState)();
  const subject = await p.open(o.input), id = subject.id, loop = o.loop ?? LOOP;
  if (o.continue && o.seed) {
    throw new ContinueRefused('a seeded run starts from the base plan; --seed and --continue cannot be combined');
  }
  // refuses before anything is written: a parent that does not replay is not a state to repair from
  const parent = o.continue ? await loadParent(p, id, o.continue) : undefined;
  if (o.continue) await replayParent(p, id, o.continue);
  const run = nextRun(id), runId = `r${pad(run)}`;
  const rdir = resolve(runsRoot(), id, runId), odir = resolve(outRoot(), id, runId), code = await p.code.hash();
  const max = Number.isFinite(o.rounds) ? Math.min(3, Math.max(1, Math.trunc(o.rounds!))) : 3;
  await mkdir(rdir, { recursive: true }); await mkdir(odir, { recursive: true });
  const { state: s, record } = await subject.pin(rdir);
  const kit = await pinKit(rdir);
  const continues = parent && await pinInherited(parent.inherited, parent.parentRun, rdir, record);
  const { builders, critic } = p, crew = crewOf(builders), owners = new Set(builders.map(b => b.owner));
  let corpusHash = '', keys = new Set<string>();
  const calls: Call[] = [], rounds: any[] = [], outs: Record<string, any> = {}, saved: Record<string, any> = {};
  const feedback: Record<string, string[]> = Object.fromEntries([...builders, critic].map(m => [m.key, []]));
  const previous: Record<string, unknown> = {};
  const label = o.preview ? '[PREVIEW, no model] ' : o.seed ? `[SEEDED ${o.seed}] ` : '';
  // read once, with the corpus below: a new GDD version in the shared tree must not change what the run records
  const maxUsd = o.maxUsd ?? DEFAULT_MAX_USD, gdd = currentGdd();
  // the corpus hash the first retrieval checked against the files; the run's later calls hold the index to it, and do
  // not read the GDD, the plans and the kit again (an edit in the shared tree must not abort a run halfway)
  let indexed: string | undefined;
  const retriever: typeof retrieve = async (query, limit, pinned) => {
    const r = await o.retriever(query, limit, pinned, indexed);
    indexed ??= r.corpusHash;
    return r;
  };
  const summary = (outcome: string, extra: object = {}) => ({
    run: runId, plan: id, date: new Date().toISOString(), objective: p.id, ...p.header, handoffMode: 'answered', loop,
    outcome, seeded: o.seed ?? false, ...record, ...kit, ...(continues ? { continues } : {}), gdd,
    commit: git.commit, dirty: git.dirty, live: o.live ?? false, maxUsd,
    ...(o.approvedBy ? { approvedBy: o.approvedBy } : {}),
    toolsHash: code, corpusHash, chunker: CHUNKER, embeddingModel: 'BAAI/bge-small-en-v1.5',
    retrieval: o.retrieval ?? 'hybrid (SurrealDB)',
    cards: Object.fromEntries(calls.map(c => [c.role, { source: c.card.source, sourceHash: c.card.sourceHash }])),
    rounds,
    cost: { usd: Math.round(calls.reduce((t, c) => t + (c.costUsd ?? 0), 0) * 1e4) / 1e4, calls: calls.length },
    calls: calls.map(c => ({
      id: c.id, objective: c.objective, role: c.role, model: c.model, attempt: c.attempt, status: c.status,
      costUsd: c.costUsd, seconds: c.seconds, ...(tokensOf(c.usage) ? { tokens: tokensOf(c.usage) } : {}),
      promptSha256: c.promptSha256, schemaSha256: c.schemaSha256, briefSha256: c.brief.sourceHash, effort: c.effort,
    })),
    ...extra,
  });

  const standIn = (m: Member) => {
    if (!p.standIn) throw new Error(`objective ${p.id} has no stand-in proposal for ${m.key}: a preview needs one`);
    return p.standIn(s, m.key);
  };

  try {
    const corpus = await loadCorpus();
    keys = new Set(corpus.map(c => c.key)); corpusHash = hash(JSON.stringify(corpus));
    let rerun = 0, lastFaults: Fault[] = [], outcome = 'REJECTED', finalVersion = '';
    if (parent) {
      // round 1 is a revision: the parent's last candidate stands where this run's earlier rounds would have left it
      const start = startFrom(parent.inherited, builders, keys);
      for (const m of builders) {
        if (!(m.key in parent.inherited.proposals)) continue;
        outs[m.key] = previous[m.key] = parent.inherited.proposals[m.key];
        saved[m.key] = parent.inherited.calls[m.key];
      }
      Object.assign(feedback, start.feedback); lastFaults = start.faults; rerun = start.rerun;
      const from = `${id}/${o.continue} (round ${parent.inherited.parent.round}, ${start.faults.length} open fault(s))`;
      const differs = continues!.sameInputs ? '' : "; NOTE: the inputs differ from the parent run's";
      console.log(`${label}continuing ${from}; round 1 reruns from ${builders[rerun]!.key}${differs}`);
    }
    for (let n = 1; n <= max; n++) {
      const version = candidateVersion(subject.version, run, n), rd = `${rdir}/round-${n}`, od = `${odir}/round-${n}`;
      await mkdir(rd, { recursive: true }); await mkdir(od, { recursive: true });
      const r: Round = { n, version, outs, ran: [], skipped: [], rd, od, odir }, reused: string[] = [];
      let seeded = false;
      const consult = async (m: Member, task: string) => {
        const extraQuery = p.query?.(s, r, m.key) ?? [], fb = feedback[m.key]!, first = calls.length;
        const obj = extraQuery.length
          ? { ...m.objective, query: [...[m.objective.query].flat(), ...extraQuery] } : m.objective;
        const handoffs = m === critic ? '' : handoffText(crew.pendingFor(m.key, outs, r.ran));
        const prior = previous[m.key] ? `\n\nYour previous proposal (JSON):\n${j(previous[m.key])}` : '';
        const lines = fb.map(f => '- ' + f).join('\n');
        const faultText = fb.length
          ? `\n\nCritic faults on it, to be fixed (passages they cite are pinned above):\n${lines}` : '';
        const text = `${task}${handoffs}${prior}${faultText}`;
        const invoke = o.preview
          ? previewInvoke(m === critic ? PASS : standIn(m)) : withCeiling(calls, maxUsd, m !== critic);
        const out = await runAgent(obj, text, m.schema, calls, odir, fb, retriever, invoke);
        const c = calls.at(-1)!, rec = {
          id: c.id, objective: c.objective, role: c.role, model: c.model, modelsUsed: c.modelsUsed, tools: c.tools,
          cliVersion: c.cliVersion, costUsd: c.costUsd, seconds: c.seconds, attempts: calls.length - first,
          card: { source: c.card.source, sourceHash: c.card.sourceHash }, brief: c.brief,
          promptSha256: c.promptSha256, schemaSha256: c.schemaSha256, effort: c.effort,
          ...(c.images ? { images: c.images } : {}), retrieval: c.retrieval, pinning: c.pinning,
        };
        return { out, rec };
      };
      for (const [i, m] of builders.entries()) {
        // e.g. a builder whose input cannot be built: no output this round
        if (p.skip?.(s, r, m.key)) { r.skipped.push(m.key); continue; }
        if (i >= rerun) {
          let { out, rec } = await consult(m, await p.task(s, r, m.key)) as { out: any; rec: any };
          if (o.seed && n === 1 && m.key === p.seeds?.agent[o.seed]) {
            const x = p.seeds.apply(s, r, o.seed, out, rec);
            console.log(`${label}${x.note}`);
            rec = { ...rec, seeded: o.seed, seedNote: x.note, original: out }; out = x.out; seeded = true;
          }
          outs[m.key] = out; saved[m.key] = rec; previous[m.key] = out; r.ran.push(m.key);
        } else reused.push(m.key);
        await writeJson(`${rd}/${m.key}.json`, outs[m.key]);
        const carried = reused.includes(m.key) ? { reused: true } : {};
        await writeJson(`${rd}/${m.key}.call.json`, { ...saved[m.key], ...carried });
        feedback[m.key] = [];
      }
      const ev = await p.assess(s, r); r.skipped.push(...(ev.skipped ?? []));
      console.log(`${label}round ${n} ${version}: ${ev.line}`);

      const built = Object.fromEntries(builders.map(m => [
        m.key, r.skipped.includes(m.key) ? undefined : { out: outs[m.key], call: saved[m.key] },
      ]));
      const cites = crew.citeFaults(built);
      const hand = crew.collectHandoffs(outs, r.ran), handFaults = crew.handoffFaults(hand, { outs, ran: r.ran });
      // loop 2: Keel judges a candidate that passed every deterministic check; any other goes straight back to its
      // owners (rules.ts). A preview shows the critic's prompt whatever the checks say.
      const judged = o.preview || criticRuns(loop, {
        checksOk: ev.ok, assessFaults: ev.faults.length, citeFaults: cites.length, handoffFaults: handFaults.length,
      });
      let verdict: Verdict | undefined;
      if (judged) {
        const k = await consult(critic, ev.criticTask(crew.citedBlock(built), lastFaults));
        verdict = k.out as Verdict;
        await writeJson(`${rd}/${critic.key}.json`, verdict); await writeJson(`${rd}/${critic.key}.call.json`, k.rec);
      }
      const faults: Fault[] = (verdict?.faults ?? []).map(f => ({
        ...f,
        // the critic's named owner wins; the rule's owner only when it is missing/invalid
        owner: owners.has(f.owner) ? f.owner : (p.ownersOf?.(f.rule) ?? [f.owner])[0]!,
        chunks: f.chunks.filter(c => keys.has(c)),
      }));
      const uncited = faults.filter(f => !f.chunks.length).length;
      for (const c of ev.failures) {
        if (faults.some(f => f.rule === c.rule)) continue;
        faults.push(...c.owners.map(owner => ({
          owner, rule: c.rule, fault: c.fault, fix: c.fix, chunks: [], synthesized: true as const,
        })));
      }
      faults.push(...ev.faults, ...cites, ...handFaults);
      // a critic that fails the candidate and names no fault must not send the builders back blind
      if (verdict?.verdict === 'fail' && !faults.length) faults.push(...silentFaults(builders, verdict.summary));
      const pass = ev.ok && verdict?.verdict === 'pass' && !faults.length;
      const gateRecord = loop >= 2 ? { criticRan: judged } : {};
      const assessRecord = loop >= 2 ? { assessFaults: ev.faults.length } : {};
      rounds.push({
        round: n, version, seeded, ran: r.ran, reused, skipped: r.skipped, ...ev.record,
        verdict: pass ? 'pass' : 'fail', keelVerdict: verdict?.verdict ?? null,
        ...gateRecord, citeFaults: cites.length, ...assessRecord, ...ev.more, handoffs: hand,
        handoffFaults: handFaults.length, faults: faults.length, uncitedFaults: uncited,
        faultOwners: [...new Set(faults.map(f => f.owner))],
      });
      await writeJson(`${rd}/faults.json`, faults);
      const keel = verdict ? `Keel ${verdict.verdict}` : 'Keel not called (the checks failed)';
      const to = pass ? 'ACCEPTED' : faults.length ? [...new Set(faults.map(f => f.owner))].join(', ') : 'rejected';
      const unciteds = uncited ? ` (${uncited} without a valid cited passage)` : '';
      console.log(`${label}round ${n}: ${keel}, ${faults.length} fault(s)${unciteds} → ${to}`);
      await writeJson(`${rdir}/run.json`, summary('INCOMPLETE'));
      finalVersion = version;
      if (pass) { outcome = 'ACCEPTED'; break; }
      lastFaults = faults;
      for (const f of faults) feedback[crew.keyOf(f.owner)!]!.push(feedbackLine(f));
      // from the earliest builder with faults; the last builder always runs again
      rerun = crew.rerunFrom(new Set(builders.filter(b => feedback[b.key]!.length).map(b => b.key)));
    }
    await writeJson(`${rdir}/run.json`, summary(outcome, { finalVersion }));
    console.log(`${label}${runId} ${outcome} (${finalVersion}) → ${rdir}`);
    return { runId, outcome, rdir, odir, finalVersion };
  } catch (e) {
    if (e instanceof SpendCeiling) { // not an error: the run stops before the call and can be continued
      const stopped = { reason: 'max-usd', maxUsd, spentUsd: spentUsd(calls), nextCallUsd: e.nextUsd };
      await writeJson(`${rdir}/run.json`, summary('INCOMPLETE', { stopped }));
      console.log(`${label}${runId} INCOMPLETE: ${e.message}`);
      return { runId, outcome: 'INCOMPLETE', rdir, odir, finalVersion: '' };
    }
    await writeJson(`${rdir}/run.json`, summary('ERROR', { error: String(e) }));
    throw e;
  } finally {
    if (!o.preview) {
      const made = git.commit ? ` (${git.commit.slice(0, 7)}${git.dirty ? ', dirty' : ''})` : '';
      p.code.record?.(code, `${p.id} run ${runId}${made}`);
    }
  }
}
