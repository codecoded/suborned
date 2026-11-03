# 039 · the square's open posts under fight B, on engine 0.35.2 (Motive, 2026-10-27)

**Question (Aster):** with fight B on (full cover ×250, partial ×500, hostiles pinned), ambush-1 and ambush-3 stand in the open with no cover within 8 m (Vantage: no valid covered spot keeps their job). Is the square's safe fight still fair and readable? If it isn't, what is the smallest fix?

**Inputs:** the tree's engine 0.35.2 (`sim/` untouched, built to `out/missions/engine/tree/bin`), today's `contracts/missions/handover/mission.json`, and the committed site (`sim/data/handover.site.json`, built from `civic-block.cover.json`). No rulebook overrides.

**Plans** (`briefs/039/motive/open-posts/plans/`):
- `ra-h3`: the tree's copy, already one-input.
- `route-a-board`: the route's plan.
- `rgk-h3`: 067's plan with its retired `panic` order recast as four `draw` orders at `fight.ambush`, the same way as `ra-h3`.

**Probes:** `open-posts/tools/ens.ts` and `table.py`, run through `bun run guard`. Results are in `open-posts/results/ens-<tag>.json`. All values are [S].

## Verdict: fair and readable. No rule or layout change
The safe fight on the bare square is fair as it stands. The open posts are a cost the rivals pay, and the player can see it: those rivals drop first. I recommend **leaving the posts open** and not adding a low piece. Nothing here goes to Sky unless he wants the square's fight to be harder.

## Numbers
There are two ensembles of 8 samples per plan:
- **op:** each operative's approach is 0–2 s late (as in 067).
- **jdraw:** the same, plus each operative presses F 0–2 s after the spring.

The op ensemble barely moves the fight, because the draws are pinned to the spring. That makes **jdraw the real fairness test.** A third set, **late3**, has every F 3 s late.

| plan | ensemble | wins | end | fight (spring to over) | open post's man down after the spring (rival-1) | ambush-3's man, who turns blocker (rival-2) | hits on the cell | op down |
|---|---|---|---|---|---|---|---|---|
| ra-h3 | op | 8/8 | 4:37.3–4:39.7 | 21.8–22.5 s | 15.7 s | 5.1 s | 1–2 | 0 |
| ra-h3 | jdraw | 8/8 | 4:37.2–4:41.1 | 21.3–25.4 s (med 22.5) | 8.0–16.9 s (med 15.7) | 5.1–8.6 s | 1–3 | 0 |
| ra-h3 | late3 | 8/8 | 4:35.9–4:37.6 | 20.4 s | 17.7 s | 7.2 s | 3 | 0 |
| route-a-board | op | 8/8 | 4:15.5–4:17.2 | board and go | 8.0 s | 6.5 s | 0 | 0 |
| route-a-board | jdraw | 8/8 | 4:15.5–4:18.4 | board and go | 8.0–9.9 s | 6.5–8.4 s | 0 | 0 |
| route-a-board | late3 | 8/8 | 4:18.3–4:20.0 | board and go | 11.0–12.5 s | 9.5 s | 2 | 0 |
| rgk-h3 | op | 8/8 | 5:47.0–5:49.0 | 20.4 s | 5.0 s | 6.5 s | 3 | 0 |
| rgk-h3 | jdraw | 8/8 | 5:47.0–5:53.7 | 20.4–25.6 s (med 22.0) | 5.0–6.1 s | 6.5–7.2 s | 2–4 | 0 |
| rgk-h3 | late3 | 8/8 | 5:40.7–5:42.7 | 14.1 s | 8.0 s | 9.5 s | 6 | 0 |

- **Wins where they were.** 067's rules-on numbers on the old engine were:
  - ra-h3: 4:37.3–4:41.2, fight 22 s. It wins at the same times and length today.
  - route-a-board: 4:15.5–4:17.2. It wins at the same times today.
  - rgk-h3: 5:56.9–5:58.9, fight 30 s. It now wins about 10 s earlier, at 5:47.0–5:49.0, with a 20 s fight.

  Standing is unchanged: 3 for fighting it out and 2 for board and go. Trail and Alert are unchanged too.
- **Length.** Every fight it out lasts 14–26 s, inside §07's "up to 60 s".
- **How fast the open posts drop.**
  - ambush-3's man is the blocker. He leaves for the road and the planter by the kerb (cv-066) at the spring. He is down in 5–9.5 s, and every hit on him is at 562 ‰, the uncovered value.
  - ambush-1's man has nowhere to go. He steps out of the line in the open, and he is down in 5–10 s (board, rgk-h3). In ra-h3 the cell takes the blocker first, so he lasts 8–18 s.
  - The covered men are much harder to drop. rival-3 behind the café wall is never down in ra-h3, because the crew breaks after three downs first. The lookout and the wheelman drop at 20–25 s.
  - The order the player sees is: the open men go first, then the late ones, then the break.
- **Is it too easy?** It is no easier than before:
  - The cell takes 0–6 hits in a fight, and no operative goes down in 72 runs.
  - Even 3 s late on F wins every time. That is the price of Inez's warning, and the safe fight is meant to be the safe one.
  - The losing plays are still there and unchanged: never drawing ends rivals-reach-vale (fight-safe-unguarded), and Pressure with no warning brings all five rivals.

## The layout trade, measured, and why I don't recommend it
**Probe:** one low piece 1.05 m high and 3.25 m long, north–south at x 20.0, y −44.75..−41.5, between the two south posts and the car.
- Only its two car-facing cover points were added to a scratch site (`tools/mksite.py`).
- It isn't in the walk grid, so a body can step over it. That is the probe's one approximation.
- I ran it as full cover (q 740, ×250) and as partial cover (q 500, ×500), with the jdraw ensemble.

| plan | open (today) | one low piece, full | one low piece, partial |
|---|---|---|---|
| ra-h3 | 8/8, fight 21–25 s, 1–3 hits, 0 down | 8/8, **fight 29–37 s**, 4–8 hits, **1/8 an op down** | 8/8, fight 19–35 s, 1–5 hits, 0 down |
| route-a-board | 8/8, 0 hits | 8/8, 0 hits (unchanged) | 8/8 (unchanged) |
| rgk-h3 | 8/8, fight 20–26 s, 2–4 hits, 0 down | 8/8, fight 21–30 s, 5–8 hits, **2/8 the Tech down** | 8/8, fight 20–26 s, 3–5 hits, 0 down |
| ambush-1's man, down after the spring | 5–17 s | 13.5–18.9 s; the cell hits him at 75–140 ‰, 3–4 hits in 18–28 shots | 9–10.6 s |

- **Full cover** adds about 10 s to the fight, roughly triples the hits on the cell, and downs an operative in 3 of 16 fight-it-out runs. It is the punishing fight 039 set out to end. It also returns to the 067 finding: cover between the south posts and the car only moves the knife-edges.
- **Partial cover** costs little. But it buys only a slower ambush-1 man, about 3 s more, and the player gets nothing new to read.
- Both need a new object in the open square, the planters' place, which Sky has ruled out. Board and go doesn't feel either.

## No value fix needed
Nothing is unfair, so there is no rule to change. If Sky later wants the open men to last longer without new cover, the smallest lever is one I own, so it needs no layout:
- Give a hostile in the open with no cover in reach a 1 s settle before he fires.
- Or leave it alone. I recommend leaving it alone.

## Notes for others (no action unless asked)
- **Waymark:** `briefs/067/motive/plans/rgk-h3.plan.json` still carries the retired `panic` order. The one-input copy is `briefs/039/motive/open-posts/plans/rgk-h3.plan.json`. Composing it is a plan edit, not a rule change.
- **Vantage:** the 067 rule for dressing the square stands. Keep it low (0.9–1.2 m) and out of the line between the south posts and the car. Nothing tall within 20 m of the car's stand.

## Handoff
- **Inputs:** this file and `briefs/039/motive/open-posts/` (plans, tools, results).
- **Done-check:**
  - Aster accepts "open posts stand".
  - Vantage closes the cover-spot search for ambush-1 and ambush-3, with no layout change.
- **Next owner:** Aster. Sky only if he wants a harder square fight.
