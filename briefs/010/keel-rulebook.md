# 010 · The rulebook, first entries (Keel, 2026-10-15)

**Files** (all in `contracts/rules/`):
- `rulebook.json` (`suborned.rulebook/1`)
- `rulebook.schema.ts`: the zod schema, plus `checkRulebook()` against `people.rules.json`
- `rulebook.ref.ts`: a reference evaluator that runs the person-level rules straight from the data
- `rulebook.test.ts`: 28 cases

**What it holds:**
- 18 events, each with `needs` (conditions), `price` (line items), `outcomes` (tried in order; the last one has no condition, so every event resolves), `also` and `witnesses`.
- The tables those events read: `values`, `fit`, `reports`, `ladders`, `memory`, `exposure`, `traceable`, `standing`, `alert`, `preview`, `streams`, `phases` and `debrief`.
- Coverage:
  - the four approaches at Inez's prices;
  - Q4 (the wrong approach);
  - Q3 (the "?" on the card);
  - the Talker test and the lie;
  - K1–K9;
  - Q2's need replacement.

**What it doesn't redefine.** The scales, temperaments, needs, ties, Talker test, knowledge and base approach prices are read from `people.rules.json` through `p:` refs and condition kinds. The check rejects any people-owned key or section in the rulebook, and any temperament, approach, scale, need, tie or fact that `people.rules.json` doesn't define.

## What the C# engine (brief 011 A) needs from this file
1. **Load order.** Load `people.rules.json` first, then `rulebook.json`, and reject anything unknown.
   - Conditions are tagged unions on `k` (32 kinds) and effects on `do` (19 kinds). The tag is always the first key, so System.Text.Json polymorphism reads them without out-of-order metadata.
   - Implement exactly those kinds; any other is a load error.
2. **Numbers.** Every number is an integer, held either in `values` (`v:` ref) or in `people.rules.json` (`p:` ref). Resolve them once into a constants table.
   - Seconds become ticks at ×10.
   - Money is integer dollars; Alert runs 0–3; standing is a signed integer.
3. **Per-person state.**
   - the four scales, clamped to the people scales;
   - temperament, need, and a need-met flag;
   - turned;
   - memory: a ring of `memory-cap` entries `{act, by, place, tick, witnessed}` plus told facts, with the oldest dropped;
   - ties, which can lapse;
   - what the cell knows of the person: routine, need and temperament bits.
4. **Per-district and per-mission state.**
   - standing and campaign Trail;
   - Alert;
   - a set of incident ids: an incident adds witness Trail once, moves standing once and raises Alert once (my hold);
   - the leads held by investigators;
   - jobs and their exposure.
5. **Resolution.**
   - `needs` decide whether the order is valid.
   - The price lines that hold are charged per order. Commit needs cash for the worst case, which includes the "?" lines.
   - The first outcome whose conditions hold applies, then every `also` entry whose conditions hold, in order.
   - `witnesses` names the traceable act that `witness.sees` then fans out over people within 13 m with a 3D sightline. Both tiers add Trail and standing; only simulated people remember.
6. **Ordering.**
   - The mission-start and between-missions events run in the order `phases` gives.
   - Subjects are visited by person id, ordinal.
   - Investigators pick and follow leads by the ordering written in that event's `how`.
   - The debrief introduction is a player order, so a replay carries it.
7. **Streams.** Use FNV-1a 32 with mulberry32 and an integer draw, keyed `{person}/need/{mission}`. The test pins the expander's stream equality and the golden draws; port those cases as C# tests.
8. **From Splice's people export** (it lacks these today):
   - each person's cast-row need weights as integers per mille (or the row id);
   - each person's position;
   - `knowledge.needsKnown`;
   - the tier.
9. **From the world** (the sim does these, not this file): reach, the 3D sightline within 13 m, the `between` checks on ties after a promotion or arrest, the watcher logs, and the Call that makes a contact's job.
10. **Golden cases.** Every case in `rulebook.test.ts` is behaviour the engine must reproduce: Inez $40,000 / 50 s / +1 T, the purse, the refusal surcharge, the Q4 grid, the "?" card, the Talker and lie answers, memory and ladder clamping, and the need draws.

## Mismatches to report
- **§04's approach table is headed "Inez, training [B]", and its Pay row ($70,000) no longer holds for the cast Inez.** Two things combine:
  - the accepted cast pins Inez `p-cafe-11` as an Idealist with a debt;
  - Sky's Q4 says Pay to an Idealist fails.

  Help ($40,000) still turns her once, so the purse holds ($80,000 with the driver, who is also Idealist with a cash need). Pressure on her is one step worse: 35 s, +4 T. The top-down scenarios that pay Inez now expect a refusal.
  - **Recommendation:** keep it. Paying blind and being refused is the §04 lesson, made stronger.
  - **Alternative:** Sky re-casts Inez.
  - Either way, Suture's port of the 28 scenarios needs the decision.
- `people.rules.json` still cites GDD v0.20.1 (Motive's file).
- The top-down test allows Ask only before Watch. The rulebook allows Ask any time there is no refusal, because the Hero and Menace "volunteers / folds free" cases need it.

## Proposed, for Sky (each one is `proposed: true` with its reason in the file)
| # | Rule | Value |
|---|---|---|
| 1 | Q4 steps | worse Pay +$20,000; worse Pressure +1 T, one order; worse Help one more order (the GDD's own "Help ×2") |
| 2 | Loyal and Help | turns on the 2nd Help ("trust over time") |
| 3 | Mercenary prices | Hero +$20,000 (overcharges); Menace −$20,000 (discount) |
| 4 | Mercenary betrayal | a Mercenary contact in a Menace district at loyalty 0 sells a route, as an Informer does |
| 5 | Standing scale | signed integer from 0, ±1 per witnessed Hero or Menace incident; Hero band at +3 and above, Menace at −3 and below |
| 6 | Reporting | a Loyal person reports each approach they don't respond to; an Idealist in a Menace district reports while bravery > fear |
| 7 | Loyalty gains | Help +1 per order (gratitude); a volunteer +1; folding = Pressure's moves |
| 8 | Witnessing violence | a contact who witnesses it: bravery −1, fear +1, resentment +1 |
| 9 | Memory | 8 entries; the oldest is dropped, which is how "sightings go stale" happens |
| 10 | Word (K3) | passes along family and workmate ties, one hop, between missions; a contact at loyalty 2 (trusting) passes nothing |
| 11 | Introductions | the friend becomes known (Watch and Listen saved, 94 s), not turned; one per contact per break |
| 12 | Exposure | the job was seen, logged by a watcher, or the contact was questioned |
| 13 | Public Help | a witnessed Help on a debt is the Hero act "clear debts publicly"; on cash it is a payment |
| 14 | False lead | points to the liar's own routine place farthest from the true fact's place |
| 15 | Ties lapsing | workmate ties lapse as regular ties do; a debtor tie lapses when the debt is met |

**Evidence**
- `bun test contracts/rules`: 28 pass, 0 fail.
- `bun test contracts`: 42 pass.
- `bun run typecheck`: exits 0.

**Next owners**
- **Aster:** commit, then send questions 1–15 and the Inez Pay mismatch to Sky.
- **Splice:** add the export fields in point 8.
- **Keel:** brief 011 A, the engine, loading this file directly.
- **Motive:** review questions 1–15 as design.
