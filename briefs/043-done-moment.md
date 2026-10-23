# 043 · The done moment: show that an act finished and what it gave

**State:** designed and prototyped, 2026-10-22. Owner: Glyph (interface-artist). Next owner: Sky tries it in `bun run controls` (Settings → Done moment); then Fable (words, below), Latch (port), Keel (one read), Chime (one cue).

## Why
Sky, playing 0.2.48 (the Handover): "it's not clear when a task has completed and what it gained." And: "that was annoying, cancelling a task I had ordered" (a new order silently dropped an act in progress). Sky's focus is the capstone, so everything here is built from the Handover's real acts.

## Objective
When an act lands, the player sees at a glance two things: **it finished** and **what it gave** (a fact, a contact, a window, Vale), plus **what it opened** (the act that is now worth doing). When an order would drop an act in progress, the player is told **before** they commit, and told again **when it happens**. No sentences, no key legends, nothing new at rest.

## Done-check
- Audit below: every Handover act has its gain and what the player sees today (done).
- A design inside Sky's rules (less reading, plain copy, HUD ≤ 15 % at rest, plates two words) (done, below).
- A playable prototype with the Handover's real gains on the fake acts, and headless checks (done: `bun testbed/controls/shoot-done.ts` 36 PASS, 0 FAIL; `bun testbed/controls/shoot.ts` 177 PASS, 0 FAIL, so "As built" is untouched).
- Port notes naming the engine codes that drive each piece (done, §6).
- Sky plays it and says which pieces stay (open).

## Evidence
- `bun testbed/controls/shoot-done.ts` → 36 PASS, 0 FAIL, page errors "", console problems none (≈ 60 s). Pictures: `testbed/controls/shots/done-1-watch-inez.png` … `done-13-take-vale.png` (the best three: `done-3-listen-inez.png`, `done-6-call-guard-window.png`, `done-11-cancel-pop.png`).
- `bun testbed/controls/shoot.ts` (the whole older suite) → 177 PASS, 0 FAIL, 61 fps. (An earlier baseline run the same morning showed 6 FAIL on the fire checks while the tree was being edited; they pass now. Not mine.)
- `dotnet run --project sim/cli -- play contracts/missions/handover/routes/route-a.json` and `route-b.json`, `careless-body-found.json`: the log codes in §2 are the ones the engine prints for each landing (replay identical).
- Nothing in Unity, sim or tools was edited.

---

## 1. What the game shows today when an act lands

Read from `HandoverHost` (HostActivity, HostHoldStatus, HostPortraits, HostLess, HostHold), `HudModel`, `HudToolkit.Hold/Intel/Less`, and `text.json`.

| Layer | What it does | Where | The gap |
|---|---|---|---|
| Status pill "Done · learned their round" | 2.5 s, kind colour, the doer's face | `HostHoldStatus.OrderStatus` (Done branch), words `DoneOf` | **Only drawn when the pointer is on the target** (`HudToolkit.Hold.cs:72`, `h.Hover`). The player is usually looking elsewhere. It names the verb past tense, not the gain ("heard them" says nothing about what was heard). |
| Ring tick over the agent | ✓ for 1.2 s | `HudModel.cs:164` (`DoneSeconds`) | Says "this agent is free", names nothing, sits on the agent not on the thing that changed. |
| Plate under the portrait | `→ learns the routine` while acting (the engine's `Activity.OnDone`) | `HudModel.cs:141` | Good promise, but the plate **vanishes** the moment the act lands. |
| Portrait face | Success face 2 s | `HostPortraits.SuccessAgent` | A mood, not information. |
| Speech | The agent says the read ("I have Inez's routine.") as a bubble 3 s, or a beat | `HostActivity.SayLanded` (`Beat("read" …)`, `Beat("done" …)`) | Words, short-lived, only if the speaker is on screen; no number, no icon. |
| Intel panel | The slot's rim flips dashed → solid; the next-step chip re-sorts | `HudToolkit.Intel.cs:177` (`intelSig`) | Silent, and in a corner of the screen. |
| Newly open act | Nothing. Locks only speak when you try ("Listen first") | `text.json locks.*.short` | The player is never told Help is possible now. |
| Windows (Loop, Call, Reroute, False tip) | A bubble and the cone going dark | `loop.on`, `call.*`, `patrol.rerouted`, `rivals.misled` | No counter, no "it ended" mark (`loop.ends` only logs, `guard.back` only logs). |
| Cancel by a new order | **Silent** for a free cancel (`CloseActCard`); a costly one opens a feed card (`FeedCancelled`) | `HostHold.GiveNow:144`, `WalkNow:192`, `HostActivity.CancelAgent:130` | No warning before; no pop at the agent; the pill's "Stopped" is for X only, 1 s, hover only. |

## 2. Per act: what it gains, what it opens, what you see today

"Lands" is the engine log code the host already reads (`HostActivity.SayLanded`, `Lands`, `HostPortraits.SuccessAgent`). Gates are `text.json` locks (needs-routine, needs-need, needs-temperament, not-a-contact). Window lengths are the rulebook values.

| Act (who) | Lands on | Gains | Opens | Seen today |
|---|---|---|---|---|
| **Watch** a person (any agent) | `<agent>.watch.watched` + role | Routine: where they are and when (`KRoutine`); the guard also his sight (cone); lovers together → the secret | Listen (and with Listen, Pay) | Scout bubble "I have Inez's routine", ✓ ring, pill if hovering |
| **Watch** a camera or the patrol (Scout, Tech) | `<agent>.watch.watched` (no role) | Its cone (`Known(w).Sight`); a second Watch its timing (`.Routine`) | Timing for Loop and Enter; the cone is drawn on Alt | Bubble ("sight" or "routine" outcome) |
| **Listen** (Scout) | `scout.listen.heard` | Need (record label, e.g. "Bureau loan"), temperament, secret (Inez loves the guard, on both lovers) | Ask, Pay, Help, Pressure (as their gates allow) | Scout bubble with the temperament voice; Intel slots turn solid |
| **Ask / Pay / Help / Pressure** (Fixer) | `turn.<approach>.<outcome>` | **turned** → a contact (`Person.Turned`); **kept**; **not yet** (one more try); **refused** (that approach is shut on them, they remember); **suspicious** (they report: Alert) | Call (turned only); introductions between missions | Reply bubble in the person's voice, Fixer "done" beat |
| **Call** (Fixer) | `call.waitress` / `call.driver` | Inez: the guard leaves the door, reaches the café in 57 s, stays 60 s (`guard.at-cafe`, `guard.leaves-cafe`, `guard.back`): a **window**. Driver: the car crosses the pickup camera (`car.blocks` … `car.clears`): a **window** | Enter (needs guard away, camera blind, patrol off) | Confirm bubble, a scene |
| **Loop** (Tech) | `loop.on` (ends `loop.ends`) | The camera is blind **while the Tech stays at the cabinet** (no timer; held) | Enter | Bubble; the cone goes dark; nothing when it ends |
| **Reroute** (Tech) | `patrol.rerouted` | Patrol off its entrance sweep 115 s (`PatrolAwayUntil`), then it returns | Enter | Bubble |
| **False tip** (Scout) | `rivals.misled` | The rival crew sent away; 230 s, they may return (`RivalsDelayedUntil`) | The pickup is safe to take | Bubble |
| **Enter** (Enforcer) | `enter.in`, then `enter.out` | Vale and the Enforcer out at the service lane: **Vale** (`Vale = WithEnforcer`); or out without Vale | Board | Beat, Vale bubble |
| **Take Vale** (Enforcer) | `take.done` | **Vale** is with the Enforcer | Board | Vale bubble |
| **Board** (any) | `board.in`, `success` | The car leaves with Vale: the win | (the end card) | End card |
| **Take down** knock out / kill (Enforcer) | `takedown.out` / `takedown.dead` (+ `takedown.seen`) | A body on the ground (found later: Trail, Alert; wakes in 120 s if only out); the person off their post = a **window** | Carry, Stash, Restrain, Drop | Bubble if unseen; `report.*` if seen |
| **Carry / Stash / Restrain / Drop** (Enforcer) | `carry.body`, `stash.*`, `restrain.done`, `carry.down` | Body lifted, hidden, tied, laid down | Board with a carried Vale; hides the evidence | Beat |

Four kinds of gain, which is all the design has to carry: **a fact** (Watch, Listen), **a contact** (turn outcomes), **a window** (Call, Loop, Reroute, False tip, a takedown's empty post), **Vale** (Enter, Take, Board). Plus **what it opened**.

## 3. The design (inside Sky's rules)

Rules kept: no sentences in play (labels, numbers and icons only; a token is a one- or two-word fragment, the ones Intel already uses); one-line prices stay as they are; no key legend; plates two words; the rules show in the world; **nothing new at rest** (everything below is transient or bound to an open window); colour never carries it alone (every state has its own shape: ✓ filled, ½ dashed, ✗ outlined).

### 3.1 Finished: three marks, one at each place the eye might be
1. **On the agent**: a ✓ tick rises above them for 1.2 s (the ring tick that exists, now in the outcome's shape: ✓ done, ½ not yet, ✗ refused).
2. **On the portrait plate**: the plate stays 2.2 s with one word: `done`, `turned`, `not yet`, `refused`, `reported`. Today it disappears. The portrait gets one soft green pulse.
3. **On the thing that changed**: the **gain chip** (3.2), not hover-dependent. The old "Done · …" pill line is dropped when the chip is on.

### 3.2 What it gave: the gain chip
World-anchored over the thing that **changed** (the camera for a Loop done at the cabinet, the guard for Inez's Call), in the act's kind colour, 4.2 s, rises 10 px as it appears, fades out. Left: the doer's face with a ✓ (the face replaces any name, as in the pill). Right: **one line per fact**, icon + one or two words, at most three:

| Act | Chip lines (icon · word) | Unlock row |
|---|---|---|
| Watch Inez | clock · Rounds | → Listen |
| Watch the guard | clock · Rounds, eye · Cone (the cone pulses once in the street) | → Listen |
| Watch a camera | eye · Cone (the cone pulses once) | — |
| Listen Inez | coin · Bureau loan, person · Idealist, heart · Loves the guard | → Pay, Help, Pressure |
| Help Inez (first) | ½ · Not yet (dark, dashed outline) | — (Help stays) |
| Help Inez (second) | person · Contact | → Call |
| Pay refused | ✗ · Refused | ~~Pay~~ (struck: that way is shut) |
| Pressure, reported | siren · Reported (Alert +1) | — |
| Call Inez | eye-off · Guard away (on the guard, with a 60 counter, 3.3) | → Enter (once the camera is also dark) |
| Loop | eye-off · Camera dark (on the camera, with the Tech's face, 3.3) | → Enter |
| Take Vale | person · Vale | → Board |
| Knock out | body · Out, clock · Wakes | → Carry |
| Kill | body · Dead | → Carry |

The facts are Intel's own fragments (`text.json less.intel`: need record label, temperament, "Loves {name}") so the chip and the Intel panel never disagree. Only the verbs and two or three labels (Rounds, Cone, Contact, Guard away, Camera dark) are new (§4).

### 3.3 What it opened: the unlock row, the pip, the main action
- **Unlock row** at the bottom of the chip: an arrow, then the verbs that were shut and are open now, as small dark pills with their icon (at most three). A refused approach shows the same pill **struck through**: that way is closed.
- **Lit pip** (a small white sparkle) on the act's **pill** and on its **ring wedge**, for 90 s or until used. No text.
- **The newly opened act becomes the main action** on that person: after Watch, hovering Inez with the Scout offers Listen; after Listen, the Fixer is offered Help first (the approach that fits), then Pay, Pressure; after Help lands as a contact, Call. This is the same idea as Intel's next-step chip, so the hold keeps being "do the obvious next thing" (the engine's `ContextRank` decides in the game).

### 3.4 Windows: the part that is a clock
A window (the guard off the door, a camera blind, the patrol away) is a thing the player must time against. While it is open, a small **window marker** sits on the thing: icon plus a **number of seconds** (Call, Reroute, False tip) or **the doer's face** (a Loop: held while the Tech stays). In the last 10 s it blinks. When it closes it says so in one pop on the thing: `Camera on`, `Guard back`, and the cone returns. This is the pillar "does it open or close a window the player can see coming", made visible.

### 3.5 The history
One row per landing in the notes key's list (L): `0:49 [face] [icon] Inez · Bureau loan · Idealist · Loves the guard`. The notes key gets its unread dot. The row carries the same tokens as the chip, so the history is the chip kept.

### 3.6 Cancelling
- **Before you commit**: a **cancel tag** (dashed, dark, ✕ icon) above the order pill: `✕ Cancels Watch · 12 s lost`. It appears on the offer pill, on the hold-full pill, on the ring plate, and on **open ground** next to the cursor after it rests 0.3 s (a click on the ground is the commonest way to drop an act). The cost word is the engine's cancel cost (`text.json less.cancel`) as a number or a word: time spent (`12 s lost`) for Watch, Listen, Loop, Reroute, False tip; `remembered` for a turn (the money comes back, they remember your face); `they report` for a takedown; nothing for an act that has not started (still tagged: it is still a task you ordered).
- **When it happens**: a pop on the agent: `✕ Watch cancelled · 12 s lost` (dashed, dark), the plate says `cancelled`, a history row. X and a stop do the same.
- A free cancel is no longer silent, a costly one no longer hides in the feed.

### 3.7 Budget and sound
At rest: no change (the only persistent additions are an open window's marker and the history key). In action, at most one chip per landing, max four on screen, never over the plot scope. **Chime** (handoff, one cue each, not in this prototype): a short rising two-note for ✓ done, a flat single note for ½ / ✗, a dull tick for cancelled, a soft click when a window closes.

## 4. Words, for Fable (proposals; the playground uses them)
Plain copy applied: labels, never sentences; one word per thing; the facts reuse Intel's fragments.

| Where | Proposed | Note |
|---|---|---|
| Plate | `done` · `turned` · `not yet` · `refused` · `reported` · `cancelled` | one or two words |
| Chip: Watch person | `Rounds` | the text already says "rounds" (Learn Inez's rounds) |
| Chip: Watch camera or guard | `Cone` | the street already calls it a cone; the rulebook word "sight" stays out of the HUD |
| Chip: Listen | need record label · temperament fragment · `Loves {name}` | exactly `less.intel` and `needs.json`; nothing new |
| Chip: turn outcomes | `Contact` · `Not yet` · `Refused` · `Reported` | `Contact` is the arc's word; `Kept` (the engine's outcome when already turned) reads as `Contact` too |
| Chip: Call | `Guard away` (Inez) · `Camera blind` (driver) | |
| Chip: Loop | `Camera dark` | |
| Chip: Reroute, False tip (not in the prototype) | `Patrol away` · `Rivals away` | |
| Chip: Take, Enter | `Vale` | two words are never needed |
| Chip: takedown | `Out` + `Wakes` · `Dead` | |
| Window close | `Camera on` · `Guard back` · `Patrol back` | |
| Cancel tag | `Cancels {Verb}` · cost: `{n} s lost` / `remembered` / `they report` | the pill's own verb labels |
| Cancel pop | `{Verb} cancelled` | one word, Sky's own |
| Unlock row | the verb labels (`Listen`, `Help`, `Pressure`, `Pay`, `Call`, `Enter`, `Board`) | no new words |

Open for Fable: `remembered` (a turn's cancel cost) and `they report` may want a plainer word; `Contact` vs "Ally".

## 5. The prototype (`bun run controls`)
Settings → **Done moment**: `Gains` (default), `Gains + Handover order`, `As built` (0.2.48 exactly: the pill line, silent cancel).
- `Gains`: all acts stay open (the old behaviour of the street) and the done moment, unlock pips and cancel warnings are on.
- `Gains + Handover order`: also hides acts until their gate opens, in the Handover's order: Watch → Listen → Help / Pay / Pressure → Call; Loop + Call → Enter (the door); Take Vale → Board (use "Vale comes out"). The real game shows locked acts dimmed with "Listen first"; the prototype hides them to keep the ring quiet.
- Fake acts added with real prices: Listen (Scout, 42 s), Help (Fixer, $40,000, 50 s), Board (4 s); Call also on the driver. Outcomes are scripted stand-ins for the engine's `turn.<approach>.<outcome>`: Inez needs two Helps (Not yet, then Contact), Pay on her is refused, Pressure gets her to report; Pay on the driver turns him.
- Windows: Loop through the cabinet blinds the camera while the Tech stays (walk him away and it ends, `Camera on`); Call on Inez switches off the guard's cone for 60 s (counter, then `Guard back`).
- Cancel: with an agent mid-act, hover another target (the pill carries the tag) or rest the cursor on open ground (the tag follows it); click: the pop, the plate, the history row.
- History: the round button bottom right, or L.
Files: new `testbed/controls/gains.js` (all of it, one place), `shoot-done.ts`; small hooks in `orders.js` (finish, cancel, waysFor), `status.js`, `markers.js`, `ring.js`, `ui.js`, `cones.js`, `app.js`, `data.js`, `icons.js`, `style.css`, `index.html`; `shoot.ts` sets "As built" at its start so its old checks stand.

## 6. Port notes for Latch (which engine code drives each piece)
Everything is a view; no new engine state is needed except where noted. Replays are untouched.

| Piece | Trigger | Read |
|---|---|---|
| **Landing detector** (all pieces) | the log line (`g.Log` since the last frame), as `HostPortraits.FacesFrame` already scans: `SuccessAgent(g, line)` returns the agent for `<agent>.watch.watched`, `scout.listen.*`, `turn.*.turned`, `call.*`, `loop.on`, `patrol.rerouted`, `rivals.misled`, `take.done`, `enter.in`, `takedown.out/dead`. Extend it to also return for `turn.*.refused|not-yet|suspicious|kept`, `enter.out`, `board.in`, `stash.*`, `restrain.done`; keep one function so the tick, plate, chip and history cannot disagree. |
| **Gain tokens** | diff, not text: snapshot per person `KRoutine, KNeed, KTemperament, KSecret, Turned, KnownRefused.Count` and per watcher `Known(w).Sight/.Routine` before and after the landing (`Model.cs:27`; `GameSight.cs:41`). `HudToolkit.Intel.cs:177` already builds the same signature (`intelSig`). Words: `less.intel` (need/temperament/secret fragments; the need's own record `label` from `contracts/people/*.needs.json`), plus the labels in §4. Turn outcome is the last segment of the code (`turned`, `kept`, `refused`, `not-yet`, `suspicious`, with `report` on the same tick = suspicious, as `SayLanded` does). |
| **Chip anchor** | the person for Watch/Listen/turn (`Views.Find`/`TargetWorld`); **the thing that changed** otherwise: the camera for `loop.on`, the guard for `call.waitress`, the camera for `call.driver`/`car.blocks`, the patrol for `patrol.rerouted`, the rivals' car for `rivals.misled`, Vale for `take.done`/`enter.out`. Show it whether or not the pointer is there (this replaces the `h.Hover`-only pill line, `HudToolkit.Hold.cs:72`). Faces: the landing's agent. |
| **Unlock row** | gate diff, never listed by hand: ask `ScoutQuery`/`LearnQuery` (Watch, Listen), `CallQuery`, and the turn price rows (`Answer.code` = `needs-routine`, `needs-need`, `needs-temperament`, `not-a-contact`, …) for each verb on that person **before and after**; a verb whose answer went from a lock to ok is the unlock (the same queries `ContextRank`/the ring already ask). Names are the verbs' own labels. A refusal landing (`refused`) shows the approach struck: the lock `refusal-remembered` now applies. |
| **Lit pip / main action** | `ContextRank` already orders the offer; give it the 90 s "newly opened" bonus (a host-side stamp per person and verb) so the opened act leads; draw the pip on the pill (`HoldPillFrame`) and the ring wedge (`HudToolkit.Ring.cs`). Stamp cleared when the verb is given. |
| **Plate word** | keep the plate 2.2 s after the landing: `HudModel.cs:141` sets `s.Under` only while busy; add `s.Done = word` for 2.2 s from the landing detector (`done`, or the turn outcome word). Portrait pulse next to `Face.Success` (`HostPortraits`, `SuccessS`). |
| **Agent tick** | the existing ring beat (`HudModel.cs:164`, `DoneSeconds` 1.2): swap the glyph by outcome (✓ / ½ / ✗). |
| **Window marker** | while open: **Call Inez** `GuardAwayUntil` (tick; `guard.back`), **Call driver** `BlockedUntil` (`car.blocks`/`car.clears`), **Reroute** `PatrolAwayUntil`, **False tip** `RivalsDelayedUntil`, **Loop** `LoopOn` + `LoopCam` (no end tick: held; ends on `loop.ends`). Number = `(until − Tick) / Hz`; the Tech's face for the loop. Closing pop on the same codes (`guard.back`, `car.clears`, `loop.ends`; the patrol's return has **no log code today**, only `PatrolAwayUntil` passing). Open item for Keel: one read-only `Windows()` list (kind, thing, until-tick or -1 held) so the host does not read five fields, and a `patrol.returns` log. |
| **History row** | `HostFeed` cards already hold the beats; add the row from the same landing detector with the same tokens. Cancel row from `FeedCancelled` (also fire it for free cancels). |
| **Cancel tag (before)** | in `HoldPillFrame` and the ring plate: for the doers of the order, `Game.ActivityOf(k)`: `CanCancel` and `CancelCost` (`lost`, `aborted`, `released`, `staff-report`, `vale-reports`, `quiet`, `ends`) give the cost word (`text.json less.cancel`; use the spent time `(Tick − BusyStart) / Hz` for `lost`); a walking-to-act agent has `Phase` walking and `pending[k]`: tag without a cost. On open ground: the same for the selected agents when the pointer has rested 0.3 s and no target is under it. |
| **Cancel pop (after)** | `CancelAgent(k, …)` (`HostActivity.cs:130`) is the one funnel: `GiveNow` (`HostHold.cs:144`), `WalkNow` (`:192`), X (`PlateStop`), and the portrait drag all pass through it. Add the pop there (anchor the agent; reuse `m.Pops`/`PopKey`), the plate word `cancelled`, and the history row; today a free cancel calls `CloseActCard` and says nothing. |

Do **not** port: the playground's gating-by-hiding (the game shows locked verbs dimmed with their short lock line), the scripted turn outcomes, the history panel's placement (use the real notes drawer).

## 6b. Words: proposed → final (Fable, 2026-10-22)
Written to `contracts/missions/handover/text.json` under `less.done` (schema `Less.done`; tests in `text.test.ts`, "brief 043"). Keys the host reads: `plate.<outcome>`, `gain.<id>`, `close.<kind>`, `cancel.*`, `walk.*`. Rules kept: labels not sentences, plates ≤ 2 words, chip lines ≤ 2 words, no gendered words.

| Where | Proposed | Final | Key | Shows |
|---|---|---|---|---|
| Plate | `done` `turned` `not yet` `refused` `reported` `cancelled` | same; `kept` reads `turned`, the engine's `suspicious` reads `reported` | `less.done.plate.<engine outcome>` | portrait plate 2.2 s after a landing or a cancel |
| Chip: Watch person | `Rounds` | `Rounds` | `gain.rounds` | over the person |
| Chip: Watch camera, guard, patrol | `Cone` | `Cone` (`Rounds` on the second Watch of a machine) | `gain.cone`, `gain.rounds` | over the thing watched |
| Chip: Watch or Listen that teaches nothing | – | `Nothing new` | `gain.nothing-new` | over the person |
| Chip: Listen | need label, temperament, `Loves {name}` | unchanged: `less.intel` and the record's own label | `less.intel.*` | over the person |
| Chip: turn outcomes | `Contact` `Not yet` `Refused` `Reported` | same (`Contact` kept over "Ally": the arc's word, and it is what the Call needs) | `gain.contact` `not-yet` `refused` `reported` | over the person |
| Chip: Call | `Guard away` / `Camera blind` | same | `gain.guard-away` `camera-blind` | over the guard / the pickup camera |
| Chip: Loop | `Camera dark` | same | `gain.camera-dark` | over the camera |
| Chip: Reroute, False tip | `Patrol away` `Rivals away` | same | `gain.patrol-away` `rivals-away` | over the patrol / the rivals' car |
| Chip: Enter | `Vale` | `Inside` on `enter.in`; `Vale` on `enter.out` with Vale; `No Vale` on `enter.out` without | `gain.inside` `vale` `no-vale` | over the Enforcer |
| Chip: Take, Board | `Vale` | `Vale`; Board `Aboard` | `gain.vale` `aboard` | over Vale / the car |
| Chip: takedown | `Out` + `Wakes`, `Dead` | `Out`, `Wakes {s} s` (120), `Dead` | `gain.out` `wakes` `dead` | over the body |
| Chip: body work | – | `Carrying` `Hidden` `Tied` `Laid down` | `gain.carrying` `hidden` `tied` `laid-down` | over the body |
| Unlock row | the verb labels | the ring labels (`actions.ring-*`: Listen, Help, Pressure, Pay, Call, Enter, Board); a shut way is the same pill struck through | no new key | bottom of the chip |
| Window close | `Camera on` `Guard back` `Patrol back` | same, plus `Rivals back`; by marker kind (guard = `guard.back`, camera = `loop.ends` or `car.clears`, patrol = `patrol.returns`, rivals = `RivalsDelayedUntil` passing, no log code) | `close.guard` `camera` `patrol` `rivals` | pop on the thing |
| Cancel warning | `Cancels {Verb} · {n} s lost` / `remembered` / `they report` | `Cancels Listen · 12 s lost`; a turn: `Cancels Help · They remember`; a takedown: `Cancels Take down · They report`; free orders: `Cancels Walk` | `cancel.tag`, `tagCost`, `cost.<engine cancel cost>` | over the pill, plate or cursor |
| Cancel cost words | `{n} s lost`, `remembered`, `they report` | `{s} s lost`, `They remember` (plainer: your face is kept), `They report`, `Staff report`, `Vale reports`, `Loop ends` | `cancel.cost.lost` `aborted` `released` `staff-report` `vale-reports` `ends` | after the dot |
| Cancel pop | `{Verb} cancelled` | `Listen cancelled`, with the cost after a dot when there is one | `cancel.pop`, `popCost` | over the agent, 1.8 s |
| Walk-in plate | `to Inez · Listen` | plate `→ Inez` (one short word per target: `walk.who`); pill and Alt `to Inez · Listen` (target from `names.targets`, verb the ring label) | `walk.plate`, `walk.line`, `walk.who.*` | portrait plate / status pill while walking to act |
| `patrol.returns` (Keel, engine 0.24.0) | placeholder | event `patrol-returns` (tech, scout speak: "Patrol's back on its sweep. Mind the entrance."); the HUD pop is `close.patrol` | `events.patrol-returns`, dialogue | bubble + pop |
| Retired | `serving a table, back in {s} s` | gone from `locks.too-far.byTarget`, the schema and `fillLock` (engine 0.23 walks the agent up to her) | – | – |

Notes for Latch: (1) the walk-in plate replaces the `walking {s} s` time-only plate when the order is an act; the number still shows in the price line. (2) The host still has the serving code path (`HostLess.ServingBackS`, `HandoverHost` too-far "serving a table", `HudToolkit.Less` line 653) and reads `byTarget.serving`; with the key gone it falls back to the base `too-far` line, so it is safe to delete. (3) Not worded, no engine landing for them yet: the takedown's empty-post window and the Take-down `report.*` seen case (the existing `report.*` cards speak).

## 7. Open questions
- **Sky:** does the chip carry the right amount (two to three lines)? Should a new order on an agent mid-act be *protected* (a ground click needs a second click, or Shift, while a Watch or Listen has time in it) rather than only warned? The design warns and pops; protecting would be a rule change for Motive.
- **Sky:** Settings default is `Gains`. In the Handover you may prefer `Gains + Handover order` to feel the chain.
- ~~**Fable:** the words in §4~~ done, §6b (`They remember`, `They report`, `Contact` kept).
- **Keel:** `Windows()` read and a `patrol.returns` code (§6).
- **Chime:** the four cues in §3.7.
- **Latch (after Sky's pick):** the port in §6; the playground's order, chip lifetime (4.2 s), plate hold (2.2 s), pop (1.8 s) and fresh (90 s) are constants at the top of `gains.js`.
