# Keel: the turned guard's Call that can never succeed (proposal, 2026-11-09)

Recorded by Aster from Keel's report. This was reading only: no edits, at 8ae0719c0.

## Facts
- **Every call on the turned guard is refused, in every state.** `CallRefusal` (`sim/Suborned.Sim/GameOrders.cs:665`) runs: a contact → down → turned → driver (`nothing-to-call`) → lover → `SummonRefusal` (`:624`). Its first check, `lover != GuardP`, gives `summon-no-visit` (`:628`).
- **That is an engine gap, not a law about people.** The summon rule says any lover comes (`contracts/rules/rulebook.json:902`). Only the visit for a lover with a post (the guard) is modelled (`:906`).
- **`CanCall` has drifted from `CallQuery`.** Its driver branch (`:619`) says "ready" for a call the engine always refuses (`:672`), and `TheCallQueryAndIssueAgree` (`Tests.cs:209`) never compares the two.
- **The host offers Call on every turned person but Popovic,** partner or not (`HandoverHost.cs:813`, `:1152`). On the turned guard:
  - the ring shows a shut "No post to leave" wedge with his badge;
  - a quick click pops "Their lover has no post to leave…";
  - a click with a verb picked gives the order, and his refusal line names Inez;
  - Intel says "Call them when it counts" (`HudModel.cs:117`).

  Each of these gives away the lover before any Listen.
- **A turned guard buys almost nothing in the Handover:**
  - he has no call;
  - Enter still fails while he stands at his door (`GameOrders.cs:736`);
  - as staff he still radios what he sees (`GameTakedown.cs:369`, before the turned row at `:371`).

## Recommendation: (b), a law
**A Call is offered only where it has a use under the rules.**
- It's an engine read, `CallOffered(role)`, built like `WatchOffered` (`GameMood.cs:67`) on the same helper as `CallRefusal`, so the offer and the refusal can't drift.
- Only reasons that last the whole mission remove the offer: no lover, the driver's retired job, a visit the engine doesn't model.
- Reasons that pass keep the shut way as built, because they tell the player something true about now: busy, alarm, saw violence, came once, too far.
- It follows the GDD's rule for a way that would do nothing: "absent, not greyed" (§03 Watch, §04's rival crew).

**The refusal order stays as it is:** what stops the call for good comes before what stops it for now. No refusal code changes in any state, so no hash or replay moves.

**Before a Listen** (Motive's lovers design, §04's preview rule): the Call shows with its price, the outcome as "?", no badge of who would come, and refusal words that don't name them. The exception is where the briefing has already said it, as the Handover does for Inez.

## Work
| who | what |
|---|---|
| Keel | Move the call block into a new `GameCall.cs` (GameOrders.cs is 776 lines and may not grow). Add `CallOffered`. Make `CanCall` simply `CallQuery(role).ok`. Add an assertion in `TheCallQueryAndIssueAgree` and a new `ScenariosCallOffer.cs`. |
| Keel, with Motive's OK | One sentence in the summon rule (check first whether it feeds a pinned hash). |
| Latch | Swap the turned test at `HandoverHost.cs:813` and `:1152` for `CallOffered`. Show Intel's call line only where a Call is offered. Add a no-Call-row assertion in `HostLogic.Summons102.cs`. |
| Fable | `already-turned` (`text.json:491`) says "Call them", which is false for the guard and the driver. |
| Glyph | Confirm "absent". Decide what the Fixer's click on a turned contact with nothing to call shows: no rows, or "Already turned" locks. |
| Motive | Confirm the offer law and the "?" before a Listen. Decide whether a law on the caller's own state (step 4) should exist. |
| Sky | What turning the guard should buy. |
