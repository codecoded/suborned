# 102 · the cell goes quiet in play (Latch, 2026-11-08)

Build **0.2.281** (`out/build/play/The Suborned.app`), draw check ok.
- Its words are the committed ones; they equal 341ff4fc's apart from the `silent` marks, which the host doesn't read.
- The staged copy it replaced was from 17:59, older than Sky's "Turn driver by" (committed 18:13), so 0.2.280 most likely still said "Driver cutoff".

## What Sky hears and doesn't

**Still hears the cell** only in scenes with people:
- the Fixer in an approach (Ask, Pay, Help, Pressure: the opener, the exchange, the last word) and on the phone in a call;
- the Scout's lines in the Listen on Inez (the agreed conversation);
- the Enforcer's words as he takes Popovic, at the car or bringing him out of the Office, and now **Popovic's answer**, which never played before (below);
- the Fixer answering Popovic in the car at the win (the cliffhanger; Fable's `SCENE_ROWS` has it too).

**No longer hears** (no bubble, no Journal line, no ticker):
- any line when an order is given, the stock "On it." and "Heading to …" too;
- arrivals (they were Journal lines only);
- landings: the Tech's loop, cut and reroute, the Scout's Mislead report, the Enforcer's takedown;
- warnings: the Enforcer badly hurt, an agent down, rivals walking on our car, the rival car turning in, boxed in, police called, the district sealing, Popovic missed (6:30), going loud;
- street moments: the 5:10 chime, the 5:20 last chance, Popovic out (5:30) and at his car (5:50), the guard at, leaving and back from the café, the loop running out, the patrol back, the dead camera noticed, a car on fire, the fight calls (ambush, the blocker, the fight over, rivals wiped, recalled, drawn, running, closing in, back to cover), the win's cheers, the fail lines;
- the Listen with no conversation no longer opens on the Scout's "Close enough to hear": the pair talk and she listens.
- the Mislead's cutaway now plays only the rival's answer ("I'll check. Hold here."), without her "He bought it". A one-line cutaway: if Sky finds it odd, either no cutaway when the cell says nothing, or a real Mislead conversation (her tip, his answer) from Fable.

On the 45 saved runs the cell had 1,376 lines outside scenes; 49 are still said, all at the moments above. Route A: 31 before, 2 now (the Enforcer bringing Popovic out, the Fixer to Popovic in the car).

**The world's people speak exactly as before.** Inez, the guard, the driver, Popovic, police, rivals, the crowd and witnesses say the same lines at the same ticks. A quiet line still takes its speaker's turn at its moment, so nobody new speaks in a gap, and its pick is still made, so every later variant is unchanged. The host logic proves it on four routes: the same people's lines and cards, and the same run hash, with the old chatter on and off.

## The news is still told

| quiet line | what tells it now |
|---|---|
| order acks, arrivals | the act mark and walk line, the act ring as it starts, the portrait's state |
| loop, cut, reroute landings | the loop's window mark on the camera, the lens dark, Reroute's route marks; the patrol's own line; the card footer in the Journal |
| Mislead report | the rival's answer, the man walking off, the lookout card leaving |
| takedown landing | the body; "Knocked out" / "Dead" in the Journal |
| Enforcer badly hurt, an agent down | the news lozenge's hurt and down cards, the hurt face |
| rivals on our car, rival car, boxed in | the lozenge's for-car, rival-car and boxed cards (rivals' own lines stay) |
| police called, response sent | the lozenge's police card with its cause (brief 100), the police ticket |
| sealing, Popovic missed | the seal's orange lock and count; the amber underline under the clock |
| going loud | the going-loud card on the hold |
| 5:10, 5:20, 5:30, 5:50 | the next key event top right ("Turn driver by"), the chime's sound, the lozenge's Popovic-out card, Popovic's own line at his car |
| guard at / leaving / back from the café | the call's window mark over the guard and its "Guard back" pop, his cone; his and Inez's lines stay |
| loop out, patrol back | the window's "Camera on" / "Patrol back" pop, the cone back |
| dead camera noticed | the Alert or police card with its cause ("Robots coming") |
| car on fire | flames, smoke, the flicker |
| fight calls | the fire cues ("Firing at you", "Coming to you"), Go's state on both cars, the rivals' and police's lines; the fight cards in the Journal |
| win, fail | the end card and the debrief; the fail camera; the people's fail lines stay |

**One gap:** when our car leaves (route B's back way), the posted three rivals following it (`rivals.follow-car`) were told only by the Scout ("The three are coming after our car"). Now only the Journal's "The posted three follow" and the street show it: it's not a lozenge card. For Aster and Glyph: a for-car-style card, or that one moment back (`"rivals-follow-car"` in the switch).

## The switch

`unity/Assets/Suborned/Runtime/Play/HostQuiet.cs`: one rule, `CellQuiet`, called from `Say` (every line in the street goes through it) and from the one cutaway built from bark rows (Mislead, and the Listen opener when there is no conversation).
- `CellSpeaks` holds the moments that still speak, by `event/verb`: `arrive/ask`, `arrive/pay`, `arrive/help`, `arrive/pressure`, `done/turn`, `done/take`, `done/enter`, `cliffhanger`.
- To give a moment its line back, add its key: `"done/loop"`, `"enforcer-warning"`, `"rivals-follow-car"`. `"*"` brings the old chatter back, as the host logic's comparison does.
- The approach, call and Listen scenes play by their own paths and never meet the rule.
- `CellLines` records every line the rule met, said or quiet, for the checks.

## The rows that never played (for Fable)

Classified by the host's code paths and checked against a sweep of all 45 saved runs: the old chatter on, each order given the acknowledgement a click gives. There were 0 contradictions. Of the 257 agent rows in the copy the host read:
- **14 still speak** (the scenes): `arrive/{ask,pay,help,pressure}/fixer`, `done/turn/fixer` ×6, `ack/call/fixer` (the call scene's opener; the order's own ack is quiet), `done/take/enforcer`, `done/enter/enforcer`, `cliffhanger/fixer/unnamed`. That's Fable's `SCENE_ROWS` less the pending `named`.
- **136 played before and are quiet now.**
- **108 never played**: 103 live rows, plus the 4 parked moves and the pending named cliffhanger. That's more than Fable's "about 80":
  - reads, 31: silent since brief 044;
  - acks, 46:
    - move, run, cancel ×12: walks and stops have no voice;
    - holster, cease, stance ×12: the fight path's silent list;
    - **Go ×4**: `GoGive` gives it straight to the engine;
    - **withdraw, end**: no voiced path;
    - **panic ×4, volley ×4, field, spawn**: retired in the Handover with one input, fight lab only;
    - knockout, kill ×2: the takedown's way is never passed;
    - the 4 parked moves;
  - arrivals, 2: the struggle (`arrive` is an order-kind event, so `takedown.starts` is skipped), and **`arrive/mislead/scout`** (a Mislead on a rival is a walk-in, so it has no arrival line);
  - landings, 10: stash ×4 and restrain ×4 (no speech path), knockout and kill ×2;
  - queued behind other speakers, 13 exactly: `chime/fixer`, `last-turn/fixer`, `alarm/fixer`, `alarm/scout`, `going-loud/fixer`, `going-loud/scout`, `fight-loud/fixer`, `takedown-seen/fixer`, `takedown-seen/enforcer`, `response-sent/scout`, `guard-at-cafe/scout`, `guard-recalled/scout`, `win/enforcer`;
  - other dead paths, 5: **`camera-shot` ×2** (`fire.camera-*` never reaches the speech), **`camera-reported/scout/nobody-sent`** (no outcome passed), `agent-down/enforcer` (his down is a fail), **`withdraw/fixer`** (an end-kind event, skipped);
  - the pending `cliffhanger/fixer/named`.
- **People's rows found on the way:**
  - `taken/*` ×6, Popovic's answer at the take, never played: the host asked with no verb and the rows are keyed `take`/`enter`. It plays now.
  - Still queued behind the cell's turn, and never played: `win/councillor`, `close-warning/security`, `fail/security/closed-in`, `blocker/security`.
- **Keep the quiet rows' lines.** Fable's `silent` marks with the lines kept are safe. A quiet row takes its turn, so deleting a row or emptying its lines would start those four people's lines playing, a change to the people that should be chosen, not fallen into.

## Files

- New: `unity/Assets/Suborned/Runtime/Play/HostQuiet.cs` (+ `.meta`), `tools/game/hostlogic/HostLogic.Quiet102.cs`, this note.
- Changed:
  - `HostActivity.cs` 460 → 462: `Say` takes the moment and asks the rule; `Beat` and the stock line pass it; `WorldBeat` passes the act to Popovic's `taken`.
  - `HostConvo.Lines.cs` 145: the Mislead's and the Listen's bark rows through the rule.
  - `HostLess.cs` 374: `CellLines` reset with the run.
  - `Proof085Scenes.cs` 237 → 238: `r085streetWords` now wants the Scout quiet after the call (it wanted her line).
  - `tools/game/proofmap.json`:
    - HostQuiet placed with HostActivity;
    - `contracts/missions/handover/speech.schema.ts` (from 341ff4fc) placed with the words: unplaced, it failed the map's test and would have made the next batch's proof full.
- Scratch, not committed: `out/scratch/latch/sweep102/` (the sweep, its rows and the classifier), `inset102/` (the inset flow by seed), `proof102/` and `proof102b/` (the proof runs).

## Checks

- **Host logic 179/179**, 6 of them new (`Quiet102`):
  - on 8 routes the cell speaks only in scenes: no line in the log, a bubble or a card but a scene's;
  - with the old chatter on and off, the same people's lines, cards and run hash on 4 routes;
  - one key brings a moment back;
  - the scenes keep their voices, and Popovic answers at the take;
  - the Mislead keeps the rival's answer;
  - an order, the stock "On it." too, says nothing.
- **Proof sections**, the ones the map gives these files: `act`, `holdActs`, `r29`, `r118`, `r067` and `r083` ok.
  - `r083`'s `r085streetWords` now passes on the Scout staying quiet (2 of her lines kept quiet after the call).
  - On the first run one `r083` part, `r085insetRouteA`, saw the guard's first inset line but not his second. It passed on the rerun. In the host, the guard says the same two lines at the same ticks on 20 of 20 seeds with the rule on and off: the miss was the proof's real-time window, not this change.
- `bun run typecheck` clean.
- The map and runner tests 77/77; Fable's `speech.test.ts` and `text.test.ts` 179/179.
- Fable's `silent` marks agree with `CellSpeaks` exactly, with no row marked one way and played the other.
- **Not run:**
  - the shots and the HUD budget: the change only takes bubbles away;
  - late-fight, tp-long, the full proof: no engine, world or kit change. The batch's delta still asks for a full proof because of 098's geometry commit, made after the last ledger pass, not because of this change.
