# 111 · Rules architecture: easier to add and change rules, with fast, targeted tests

Owner: Redline (code and performance reviewer), with Keel's engine as the main subject. Aster, 2026-11-12. Next owner: Aster, then Sky (picks the steps).

Sky, 2026-11-12: "Is our codebase well designed for a rules system? Could the architecture be much better suited for adding rules, changing them etc and have the test paths calculated quickly without worry to the game itself". Then "Yes" to a review that ends in a costed plan.

## What Aster saw (check it, don't take it as given)
- **The engine is good at the base:** a deterministic C# engine (sim/Suborned.Sim, about 185 files and 40k lines) runs outside Unity, and rules are judged by sim:test and the seed sweep.
- **The rulebook mixes data and prose:** contracts/rules/rulebook.json holds 547 values (dials) and 37 events with declarative needs (30 need kinds). But 34 sections carry their rule as prose, implemented by hand in the engine, so behaviour changes need engine rounds.
- **The whole rulebook is in the hash:** its text, src and reason notes included, so any edit moves every replay hash. DP1 is re-recorded and the cover, skyline and geometry digests churn every round.
- **Decisions live in the Unity host:** about 20k lines of Host*.cs. Brief 110 found three host-side order queues and a Watch-first hint, which is why HostLogic tests and the real-time r083 proof exist.
- **Tests pin exact timings:** Waymark's 42 fixed-timing route scripts. Act pace 50% changes 9 outcomes with no rule wrong.

## The review answers
1. **Where rules live today:** data, engine code, host code, or tests. A map with examples (Listen, Pay, the takedown, sight, the response), and what a typical rule change touches and costs in time.
2. **The target architecture**, as a staged migration and never a rewrite:
   - a semantic hash (only what the engine reads);
   - the host as a pure view and order sender (legality, refusal reasons, queues and hints all from the engine);
   - rule-dependency tracing, so a change re-runs only the plans, scenarios and proofs that read it;
   - verbs as data (needs, cost, time, effects) through one interpreter, with complex systems as code modules with declared dials;
   - outcome and band assertions instead of tick-pinned scripts and hash-pinned fixtures.
3. **Per step:** effort in agent-hours, risk (replay compatibility, determinism, save files), what it unlocks, how we'd know it worked (e.g. "a rulebook note edit moves no hash", "changing Listen re-runs N tests in under a minute"), and the order. The semantic hash comes first unless the review finds better.
4. **What NOT to change:** keep what's already right.

## Rules
- Review only: no code changes, no Unity, no builds, and nothing heavy while Sky plays (`bun run guard`). Light timing measurements are fine.
- Deliver `briefs/111/redline/plan.html`: one self-contained page in plain English for Sky, with the step table at the top. Add `briefs/111/redline/notes.md` for the crew.
- Don't commit; Aster commits.
