# Brief 110 · the host side of "every order acts at once" (Latch, 2026-11-12)

Engine 0.55.0 (Keel, `briefs/110/keel/orders.md`). Sky:
- "If I order a listen command, then it should be actioned. No commands should be waiting. It either is accepted or not available."
- "Scout can always listen and watch"

**The build:** BUILD_LINE

## 1 · The three host waits, gone (Keel's audit)
All three were in `HandoverHost.cs`.
- **An act ordered while the agent's act in hand was nearly done.** With the act ending within 2 s, the host held the new order and gave it when the act landed (`Playable`, `pending`, `FollowUp`). Now the act in hand is cancelled, at its cancel line's price as before, and the new order goes now.
- **Fire, the same.** A Fire to an agent whose act ended within 2 s waited for it. Now the act is cancelled and the fire goes now, in every control preset.
- **A Watch with a passer-by across the line.** It waited up to 5 s for them to clear (`CrowdBlocked`, removed). Now it is given at once. If the engine refuses it (`no-clear-line`), the ring says why, as any refusal does.

**What stays:**
- **The walk to a place act.** The click walks the agent to the cabinet or the door, and the act follows on arrival. That walk is the order acting, not waiting.
- **The paused "plan together" queue.** That's the player's own pause, released on unpause.

## 2 · Listen without Watch, and the r085 rows
- **Milena under follow:** she steps out once the listener has settled by the person standing. Then she stays with her while the Listen runs, walking alongside at arm's length if she moves on. Her way back starts from where she is.
  - First r085 run: she walked out to where Inez stood, and Inez (no longer held) walked on, 9.6 m away by the scene.
- **The Listen hint (H2)** waited for a known routine. Now, once the cell has begun (H1 gone), it rings someone on the street whose need and temperament are unknown and who has no Listen on them, a watched one first.
  - Its words are unchanged.
  - Its `trigger` line in `text.json` says the new rule.
- **The person card's "Next"** says "Watch or Listen, either first" while nothing is known. Then it suggests the one still missing (Listen first, as it learns the need).
- **The Pay lock's fallback words** lose "(Scout)": only Pay asks for a routine now, and any agent can Watch. The words in `text.json` already said "Learn {who}'s routine first: Watch them."
- **Milena** (the friend who steps out to talk to the person being listened to) comes out only once the listener has settled by the person standing. She goes back if they walk on, since a Listen no longer holds anyone (`HostViews.Listen.cs`).
- **The "waiting" plate** (`HostHoldStatus`) can't show under follow. I kept it for a rulebook pinned before 0.55.0 (Keel's LS-OLD still plays the spot and its wait).
- **r085 rows** (`Proof085Beats.cs`):
  - `r085listenSpot`: now "under way, in reach of Inez, never waiting". It was "a spot it waited at".
  - The scene then plays on until the Scout has settled, so Milena can step out.
  - `r085scoutChair` and `r085chairStep`: the chair is the one the settle walks her to (`ListenSpotNow` during the act), from a Listen given with no Watch first. They no longer use `ListenSpotAhead`, which is always empty now.
  - Under 0.55.0 a settle takes a chair only where Inez stands with a free one in reach (Keel's LS-CHAIR accepts "seated, by a wall, or in reach where she stood"). So the proof gives the Listen at a run of moments in Inez's round (0, 15, 30 … 180 s), each on a fresh run, headless, until one settles in a chair. Then the drawn checks run: the walk into the seat, and sitting as a customer.
  - In 10 moments no chair came up: 4 settled by a wall, 6 never settled. So both rows say "no chair offered", with the moments tried.
  - **Sky's ruling:** the Scout prefers a chair, and the person listened to stays in earshot once she has reached them. Keel is changing the engine for that (0.56.0). The two rows should then pass on a real chair; they check the drawing as soon as one is offered.

## 3 · The proof fails fast (Aster: "an hour lost to a silent wait")
- **What happened:** the first r083 run on my 110 scratch build sat for an hour with nothing written.
  - The scratch app was one folder deeper than `out/build/play`.
  - `FightLab.Dir` walks up 7 folders looking for the repo; it found none.
  - `ProofBook.Route` threw on the missing route file. The exception ended the proof's coroutine chain, and the player waited.
- **Now (`GameProof.FailFast.cs`):**
  - **Before any section,** the inputs the sections read from the repo (the routes, the mission, `briefs/027`) must be found. If any is missing, `proof.json` says `error` and names every path it looked for, and the player quits (exit 2) within seconds.
  - **Any exception thrown out of a proof section** (a stack through `Suborned.Play.Proof*`, `GameProof` or `Shots*`) writes `proof.json` `error`, with the message and its first frames, and quits at once.
  - The repo search goes 10 folders up, so a scratch build under `out/scratch/<agent>/<dir>/` finds it.

## Clip
- `briefs/110/latch/clips/listen-follow.mp4` (30 fps, 30 s, `shots-listen.json` with `listen-inez.script.json`): the Scout waits on the terrace and is told to Listen to Inez at 0:30.
- She walks in at once and follows Inez round the café front as Inez serves (the camera keeps the Scout: `clip.track`, below).
- When Inez stands at a table the Scout settles a few metres off, side on, and Milena steps out to Inez.
- The clip tool gained `clip.track` (`shots.schema.ts`, `shots.ts`, `ShotRunner.cs`): the camera eases after the shot's `focus` every frame, so a clip can hold people who move.

## Checks
CHECKS

## Files
- `HandoverHost.cs` (the three waits), `HostHints.cs` (H2), `HudModel.cs` (Next), `PlayText.cs` (one fallback line), `HostViews.Listen.cs` (Milena)
- `Proof085Beats.cs` (rows), `GameBoot.cs` (two lines), `GameProof.FailFast.cs` (new), `HostFightLab.cs` (the search depth)
- `contracts/missions/handover/text.json` (H2's trigger line), `tools/game/proofmap.json` (`GameProof.FailFast.cs`)
- `briefs/110/latch/listen-inez.script.json`, `shots-listen.json`, this report
