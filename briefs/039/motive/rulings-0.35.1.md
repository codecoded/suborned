# 039 on engine 0.35.1: Motive's rulings on Waymark's re-record (2026-10-27)

**Inputs:** Waymark's re-record (`briefs/039/waymark/`: `probes/cell-down-0.35.1.txt`, `fixtures059-0.34.2-vs-0.35.1.txt`, `check-0.35.1.txt`), the rulebook (`ai.intent` final "agent", `fight.unguarded`, K3 a in `GameUnguarded.cs`), and GDD v0.30.1 §06 and §07.
**Probes:** the tree's built 0.35.1 DLL, run from scratch scripts. Nothing in `sim/` changed. Evidence is in `briefs/039/motive/probe-0.35.1/` (README lists every probe). All values are [S].

## 1. fight-safe-cell-down: no natural losing play reaches cell-down on 0.35.1. Cover it with a scenario, not a route
**Measured on the square, the stand the route uses** (route-g-fast-board up to the spring, five rivals, no warning, the guard knocked out):
- **The four drawn, waiting in the open.** 165 waiting spots 5–17 m from the car, each at least 2.5 m from any cover point: 143 the cell wins the fight, 22 rivals-reach-vale, **0 cell-down**.
- **The four drawn, walking down the open lane into the planter run's guns** (the natural B mistake: no cover, toward men behind planters). 150 walks: 98 lose nobody, 42 lose one, 8 lose two, 1 loses three, **1 is cell-down** (`cell-down-the-one-in-150.json`). One play in 150 is fishing for a result, not a natural loss.
- **Staying to fight it out in the loud branch** (loud and loud-fight with the Board and Go dropped): sealed at 5:41.0, before the cell is down.
- **At the yard** the fight is 3 hostiles plus the guard on the cell's side. That is less likely still, so I didn't sweep it.

**Why:** this is the 0.35 fight doing what Sky asked. The floor (ops 5, Enforcer 8), the pin off the cell, and the rivals breaking once more than half are down (three of five) together mean a drawn cell outlasts the crew. The Enforcer alone breaks three rivals on the range.
**Ruling:**
- "The cell down" stays in §07's fail list. Bigger fights (act II at 8–15 hostiles, the loud police who never break) reach it, so it is a real rule.
- At the Handover its coverage moves from a route to Keel's scenario K3-CELL-DOWN, which already passes on 0.35.x. The mission check's "every fail is reached" should count cell-down as reached by that scenario, and the manifest should drop the fight-safe-cell-down route's expectation.
- Re-cast the route rather than fake a loss: the "the cell never draws" script Waymark composed becomes **fight-safe-unguarded**. Under ruling 2 it ends **rivals-reach-vale**. That is a natural losing play: you never draw, and they walk past you and take him.

## 2. The stall: an undrawn cell beside a held car with Vale aboard. Rule: an undrawn agent doesn't guard the car
**Today:** rivals in the final fight fire only on an agent with a cause of his own (drawn, holding Vale; `ai.intent`, final "agent"). Hiding in a held car loses only when *nobody* stands outside it (K3 a), and a rival takes Vale only with *no* op within 6 m of the car. So a cell that stands by the car without drawing is never shot, never pressed, and the run idles to the 8:30 close with no outcome.
**Ruling (the simplest rule; it extends K3 a by one word):** in both tests, "an operative standing outside the car" means one who is **drawn** (gun out, or drawing). An undrawn agent standing by is no guard.
- **Then:** the rivals up and not broken (the blocker keeps his lane) walk to the held car. They fire as they walk, only at agents they have cause to fire on (none, if nobody draws). The first to reach the car takes Vale: **rivals-reach-vale**, the existing code, and the fail camera's existing story ("a rival reaches the car with nobody beside it"; Fable: "with nobody guarding it").
- **The player's answer is in his hand:** press F. A drawn agent outside sends them back to cover (K3 a's existing "back to cover"), and makes himself a foe.
- **No close rule is needed for this stall.** One run with no outcome remains: a cell that wins the fight but never presses Go, with the car free and Go lit. That is a script that stops issuing orders, not a play the rules have to answer. The mission check should treat a "none at 8:30" as a broken script (MC-0 already expects every route's outcome).
- **Why not a fail at the close:** a fail at the close is a timer the player never sees coming, and it can't say who did what. Under this rule the fail is something the player watches happen, and the rivals wanting Vale is already a §05 fact.
- **Checks for Keel:**
  - In the shipped wins the cell is drawn at the car whenever it is held with Vale aboard, so nothing should move.
  - The no-locks plays (`bal-rb-ff-nolocks`, `bal-rbb-ff-nolocks`: none at 8:30 on 0.35.1) become rivals-reach-vale.
  - fight-safe-unguarded ends rivals-reach-vale.
- **For Sky (through Aster):**
  - *What:* GDD §06 getaway's "while the car is held and no op stands outside it" and "with no op standing within 6 m of the car" each become "no **drawn** op".
  - *Why:* without it, a cell that never draws can't win or lose, and the run hangs.
  - *Options:* (a) this rule; (b) a fail at the 8:30 close, code "out-of-time"; (c) leave it.
  - *Recommendation:* (a). It needs no new fail code and no hidden timer, and the player can answer it with F.

## 3. The yard balance points: restate both
**What moved:** the floor and the pin off the cell keep the Enforcer up. In one input, "at will" means pressing F, and drawn agents fight on their own from the spring (rulings-0.35, ruling 1).

| play (route B at the yard, fixtures re-composed in one input) | 0.34.2 | 0.35.1 |
|---|---|---|
| rb-ff, Scout and Fixer press F at the spring, no focus | — | **success 7:32.2** |
| rbb-ff, the same | — | **success 7:35.4** |
| rb-ff, F only at the Board (7:25.3) | — | fight over 7:43.9; wins on a Go once free (the script's Go at 7:32.2 is refused, held) |
| rbb-ff, F only at the Board | — | **rivals-reach-vale 7:52.7** (rivals.for-car 7:43.8: all aboard a held car) |
| rbb-ff, focus 3.0 s late (BAL-YARD-FOCUS-LATE) | rivals-reach-vale 7:42.2, the Enforcer down 7:40.7 | **rivals-reach-vale 7:44.4, the Enforcer up** |
| rb-ff and rbb-ff, never drawn (no locks) | rivals-reach-vale 7:47.0 / 7:46.0 | none at 8:30 (the stall, ruling 2) |

- **M059-B7 ("at will, the carry is lost") is retired.** It was true of the old fight, where three sidearms on the carrying Enforcer downed him in 2–3 s, and that is the punishing fight 039 set out to end.
- **Restated B7:** *a cell that draws at the spring holds the carry at the yard, without a focus. Drawing late is still punished: draw only at the Board and board and go, and the rivals take Vale out of the held car (rivals-reach-vale 7:52.7). Never draw, and they take him unopposed (once ruling 2 lands).*
- **BAL-YARD-AT-WILL, for Keel:** replace the scenario's claim and checks.
  - The F-at-spring plays (fire orders dropped, `draw` for the Scout and Fixer at the spring) both win, logged as `r.Line` with their times: 7:32.2 and 7:35.4.
  - rbb-ff with F at the Board fails rivals-reach-vale with rivals.for-car: an `r.Is` on the code and the line; the time 7:52.7 as an `r.Line`.
  - Drop "the posted three shoot the carrying Enforcer down" and the "Enforcer down by 7:31" line.
  - The play files are in `probe-0.35.1/`; compose them into fixtures, as with `fixtures059`.
- **BAL-YARD-FOCUS-LATE, for Keel:** keep the substance (3 s late on board-and-go loses: nobody outside a held car, K3 a) and restate the mechanism.
  - Drop "down the Enforcer in it".
  - Check rivals-reach-vale plus rivals.for-car, with the Enforcer **up**, as an `r.Is`. The time 7:44.4 is an `r.Line`.
  - rb-ff late still wins on the scenario's Go-once-free loop, unchanged.

## Handoffs
- **Keel:**
  - The K3 a change: "outside" and "within 6 m" count drawn agents only (code and rulebook `fight.unguarded` wording). It waits for Sky's yes through Aster.
  - The BAL-YARD-AT-WILL and BAL-YARD-FOCUS-LATE restatements, from this file.
  - The mission check counts cell-down as reached by K3-CELL-DOWN.
  - Done when the check passes with the restated claims, the no-locks plays end rivals-reach-vale, and the route diff moves nothing else.
  - Next owner: Waymark.
- **Waymark:**
  - Re-cast fight-safe-cell-down as fight-safe-unguarded (the never-draws script; manifest: failure rivals-reach-vale) once Keel lands the change.
  - Drop cell-down from the route manifest.
  - Compose the two F-at-spring and the F-at-Board yard plays into fixtures.
  - Done when MC-0 is 42/42.
  - Next owner: Latch.
- **Aster:** the §06 wording question for Sky (ruling 2, with its options).
