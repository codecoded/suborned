# 011 · Play the Handover: one engine, played by the machine and by people

**Status (2026-10-15): briefed; work on branch `play/handover`.** E's test plan is done (056bf76). The rulebook is started (c5dfb2d).
- Done: the .NET SDK 10.0.401 is installed on the Studio (Homebrew `dotnet`).
- Running: Keel's rulebook (`contracts/rules/`), started under brief 010.
- Next: A (Keel) once the rulebook lands; C (Glyph) and the test plan in E start now.

Goal (Sky, 2026-10-15): "now we have the NPC basic structure, we need to think about how we play and test the missions." This brief covers GDD capstone weeks 2–3 (§11): rules in Unity, the Handover in the 3D world, cell control, contacts, security and rivals, and the first uncoached playtest.

## Fixed (Sky, 2026-10-15)
- **First play is in Unity:** the civic block, graybox, the generated cast as capsules, the play camera at zoom 14. There's no separate 2D client; the old top-down test already proved the rules.
- **The machine plays every mission through route scripts plus variations.** A mission ships its routes as order scripts. The headless engine plays them, then variations (each contact removed, each order delayed, the loud route), and the mission checker judges (§09):
  - ≥ 2 quiet routes win;
  - the loud route exists and costs more T;
  - every fail is reachable;
  - no single contact is essential;
  - an informed player finishes ≥ 1:00 before the closing event.

  A route-finding bot comes later [L].
- **One engine:** Keel's C# library (brief 005 A). Unity runs it live; `dotnet` runs it headless in tests and in the mission checker. It ticks at 10 Hz with integer state; a replay = start-state hashes + orders + checkpoint hashes; Unity re-simulates and never plays a recorded stream. It targets netstandard2.1 so Unity loads the same DLL, and its tests run on .NET 10.
- **The Handover only.** Generated missions (brief 003) stay on hold until it plays well.
- **Rules come only from the rulebook** (`contracts/rules/`, Keel) and the people rules (`contracts/people/people.rules.json`, Motive): unknown = reject. People come from `contracts/people/` and the accepted cast; the Handover's roles are pinned by id.

## Decisions (Sky, 2026-10-15, after Suture's QA and Keel's rulebook)
- **The Handover starts at 17:24:30 city time:** the 5:30 chime is the Office's real 17:30 shift end. Roles must be in place when the mission needs them (Waymark, 52f2a2d).
- **Inez is an Idealist, and Pay is the lesson.** Paying her blind fails and is remembered; once Listen has revealed her temperament and debt, Help ($40,000) fits the purse with the driver ($80,000), and Pressure is cheap but costs +4 T (one step worse on an Idealist). §04's training table row "Pay $70,000" becomes a refusal for this cast: owed in the next GDD version. The 28 scenarios that pay Inez now expect the refusal.
- **Only Listen reveals temperament** (and need); Watch reveals routine only. So paying after only Watching is a real gamble shown as "?" (Sky, on Motive's finding that Watch revealed temperament and made Pay never blind).
- **Keel's 15 proposed rulebook values go ahead as [S] starting values,** reviewed by Motive; Sky can strike any.
- **Redline's review** (`briefs/010/review/redline-2026-10-15.html`) is assigned: the people file and determinism to Splice, the district sampling to Pulse, Unity perf honesty to Latch.
- **Glint's ruling:** Editor shots clip at ARGB32 and the player is the true look. Latch makes previews honest before Glint retunes the light, and Sky sees the palette again on honest frames.

## A · Keel · the engine, headless
objective: `sim/` (C#), a Unity-free class library plus a test project.
- **Scenarios:** port the 28 scenarios of the top-down test. The sim is `~/Projects/the-suborned/testbed/reference/suborned-world-sim.cjs` and the tests are `~/Projects/game-source/docs/prototypes/suborned-world-sim.test.js`. Rebase them on the real civic block, the world export, the cast and the rulebook; each scenario keeps its intent, and positions are re-sited with a stated reason.
- **What the engine covers:**
  - the order format the GDD controls produce (§10: select/move, group, engage, fire, call, and every §03 verb);
  - people stepping their routines;
  - cameras, the guard, patrol, rivals and Vale's schedule;
  - witnesses and knowledge (K1–K9);
  - Trail and Alert;
  - the final fight branches (§07).
- **Interface:** a small CLI (`dotnet run --project sim/cli -- play <scenario|script>`) that prints the outcome, the event log and checksums. Bun calls it, so `bun test` can run it.

done-check: the 28 scenarios pass headless; the same orders give byte-identical checksums twice; a perf figure for one Handover run (sim-seconds per wall-second); `bun run typecheck` and the bun tests stay green.
next owner: Waymark (B), Latch (D).

## B · Waymark · route scripts and the mission checker
objective: the Handover's routes as order scripts in the engine's order format (`contracts/missions/handover/routes/`):
- route A (front door);
- route B (pickup);
- loud;
- at least one script for each fail condition.

The checker runner (`tools/missions/`) plays every script and its variations headless and applies the §09 conditions. It's a plug-in on the generation pipeline (brief 003's plan), so it can judge generated missions later.
done-check: route A and route B win; loud costs more T; every fail is reached; removing any one contact still leaves ≥ 2 quiet routes, or the checker names the contact the mission depends on; results are recorded and replay.
next owner: Suture.

## C · Glyph · HUD and action cards (concepts first)
objective: what the player sees, from the GDD and the real cast (Inez p-cafe-11, the guard p-records-office-02):
- the clock, with the tactical pause (T1 decides whether it stays) and Next Event;
- operative selection 1–4;
- the action card showing the full price ($, time, T, A risk) and "?" for an outcome not yet known;
- the dossier (known, suspected, planned);
- Trail, Alert and standing.

Use the Codex CLI image tool only (no Runware). Make 2–3 directions, then an annotated spec of the recommended one, with every element mapped to the engine or rulebook field it reads.
done-check: images and spec in `briefs/011/ui/` with `PROVENANCE.json`; Sky picks a direction.
next owner: Latch (D).

## D · Latch · the Unity host
objective: Unity runs the engine live in HarbourDistrict:
- the capsules move by the engine, not the snapshot;
- the GDD controls (§10) feed orders;
- Glyph's chosen HUD, built plainly;
- the tactical pause as a switch;
- every session logs its orders, so any play replays exactly.

done-check: the 28 scenarios pass in Unity (§11: "Unity must pass the same 28 scenarios before it's trusted"); route A played by hand matches route A's script outcome; 60 fps owed on the Air (Redline's harness, with people and the engine in); a player build Sky can run on the Air.
next owner: Suture (E), then Sky plays.

## E · Suture · test plan and playtests
objective, starting now:
- a test plan mapping the 28 scenarios, B's route scripts, and T1–T6 (§13) to what each needs from the build;
- the §11 gate checklist;
- the playtest protocol (6 new players by default, uncoached, what's logged).

After D: Sky's first play, then the first uncoached playtest.
done-check: `briefs/011/test-plan.md`; playtest reports in `briefs/011/playtests/`.

## Review
Redline reviews A's determinism and perf, and D's frame cost.

## Not now
Forma (figures), Fable (dialogue), Chime (cues; capstone week 5), and the payoff cut and debrief (week 4). Each waits until the thing it dresses plays.
