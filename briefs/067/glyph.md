# Glyph · batch 0.2.123 interface spec (Sky's 0.2.122 play) · 2026-10-26

Spec only: no code, no Unity, no paid calls. Routing letters are brief 067's (C boarding and Drive · D sticky target · E locked ways · F cones on demand and click priority · H conversation scene from the ground · G camera and roster). Sizes are px at the 1080p reference (the kit scales it); metres are on the ground; "as built" means the host as read at `HostHold`, `HostContext`, `HostHoldStatus`, `HostGetaway`, `HostDriveCar`, `HostSight`, `HostViews.ShowFans`, `HostNotice`, `HostTargetRings`, `HostPick`, `HostConvo`, `HostFollow` and `HudToolkit.Hold/Ring/ActMark/Less/Rivals/Fire/TargetRings`. Boards are listed in §Boards at the end (drawn from the game's own kit, not from the build).

**No new kit asset and no new colour.** Every piece is a kit sprite, a `kit.uss` variable or a contract colour the HUD already uses: cream `#f7f4ec`, ink-deep `#0b1615`, dim `#b0cdc7` (`--text-dim`), chrome `#cbc6c3`, turquoise-light `#60e0ea` (live / pending), amber `#f0a020` only as the window marker's "running out". Signals stay as ruled: cones red, guard amber, rivals violet, each with its own shape or motion; nothing here brings a kind, harm or role colour.

**Rules kept:** no sentence in play (a refusal is an icon and ≤ 4 words; Alt gives the long line); faces and icons before words; one line per way; the HUD budget ≤ 15 % at rest (nothing below adds to rest); a cue never draws a cone the cell hasn't learned.

**Revisions.** Rev 1 landed first and Latch started from it (E3, E4, the Board doers and `CarDriveRows` are already in the tree). **Rev 2 changes, in the order they bite:**
1. **H is rewritten.** Rev 1 confirmed the profile two-shot. Brief 033's captures of the build show the scene falling to one person alone and to over-the-shoulder; H1 is now a ladder (profile two-shot, high two-shot, intercut). The idle-and-facing work Latch has begun stands (H2).
2. **C5c: the hold on the car is Board, then Go once Vale is aboard, then Drive** (Rev 1 said Go is never a hold; the GDD's default is "Go once aboard" and `CarMainWay` already does it).
3. **C5b: Go before Vale has no dashed outline and no loud mark** (those are the violent ways'); Tier 6, never lit, a struck Vale cameo, the 0.5 s hold wedge. **Drive's icon is `arrive`** (ring wedge and pill disc).
4. **E2: up to the ring's five shut ways** (Rev 1 said four); **C4:** the refusal is the roster chip, not a world pop (§C4 says why).
5. F2: Listen is not a cone cause. H1: candidates ±12° and ±25° (not ±45°). C1: `Agent` never −1; done-checks for D, E, F; boards.

## Summary: what Latch builds, in order of weight

| # | item | what the player sees | host change it needs | reads |
|---|---|---|---|---|
| C | one order boards the squad; the car is the thing you command next | one pill and one ring wedge with every giver's face; seat pips with a pending state; a why chip (two words) on a refused portrait; Drive on the car's ring and hold; stand outlines whenever anyone is aboard | `DoersOf` takes every giver for Board; boarded leave the selection; the car's panel builds for the crew aboard | `Ops[k].InCar`, `pending[k]`, `BoardLock`, `AnyInCar`, `CarHolders` |
| D | the locked target stays visible while it moves | a lock reticle over the target's head with the shooters' faces, an edge chip off screen, a hand-off line on re-target | none (a view over the order that exists) | `Ops[k].Gun.Order`, `FireNow`, `FireOne`, `FireLocks` |
| E | locked ways show | a shut ring (never a silent walk), a locked pill on hover, Fire never the stand-in outside a fight, a faces pop when nobody selected has a way | `ClickRing`, `OpenRing`, `HoldIntent`, `HoldClick` (Suture's four edits) | `plan.Pn.Rows` (locked), `ContextRank` |
| F | cones on demand, and a click that lands | only the cones that matter now; Alt for all; nothing from police or rivals in a fight; a priority ladder under the cursor and a stack tab on the pill | `ShowSight` (no rest strength), `FanMattersNow`, `TargetAt` sort key, `StepStack` cue | `KnownFans`, `Detection`, `Hover`, `Ring`, `Charge` |
| H | the conversation from the ground | a ladder of shots that always holds both people (profile two-shot, high two-shot, intercut); both set to idle and turned to each other; no one-person scene, no over-the-shoulder | `ConvoFrameShot` ladder, idle and facing at the cut, silhouettes off in the low shots | `Convo`, `views`, `ConvoClear` |
| G | camera and roster | a ring pulse on a portrait click; an eased exit with the marks fading back | exit glide | `CentreOn`, `FollowExit` |

---

## C · Squad boarding and Drive

**One idea:** once agents are aboard, the *car* answers, with or without a selection. Three ways to the same three acts: **hold on the car** (the best way), **click the car** (its ring), **the stand outlines** (Drive only).

### C1 · What the host does (so the marks have data)
1. **Board goes to every giver, not the best placed.** `HostHold.DoersOf` picks one agent (`return new List<int>{ best }`) for every way; for `board` it returns every selected able agent whose `BoardLock(k).ok`, one `board` order each in the same tick. `BoardLock` must mirror the engine's `Board` tests in the engine's order (`GameGetaway.Board`: running, **tethered (the Tech while his loop holds: `board-tethered`)**, carrying a body, then the host's down and in-car); today it lacks tethered, so the Tech would be given an order the engine refuses. A walk is each agent's own; the pill's time is the **slowest** doer's walk + act (Board 1 s, 4 s carrying). `$`, `T`, `A` are zero, so the line carries the time only. `PortraitDrop` and the ring's wedge take the same path (both go through `HoldGive`).
2. **Whoever boards leaves the selection, even when the whole squad boards.** `HoldGive`'s "alone, or the whole squad goes: nobody leaves" must not apply to Board. Reason: the engine steps a boarded agent out on any move order (§06), and `GroupWalk` does not skip `InCar`, so a squad left selected would be stepped out by the next ground click. `Agent` keeps pointing at the first one aboard so the pill has a face (it must never become −1: `Array.IndexOf(Selected, true)` is −1 for an empty selection); an empty selection is fine (1–4, the portraits and Squad bring agents back).
3. **The car answers with nobody selected.** `BuildPanel` returns "Select an agent." for an empty selection, so the car's ring, hold and pill would be dead after a full-squad board. For the car (and a stand), build the panel for the selected able agents; if none, for the crew aboard (first aboard in cell order: Scout, Fixer, Tech, Enforcer). Go and Drive need no selection: any agent aboard gives them.
4. **Stand outlines show whenever anyone is aboard** (`Game.AnyInCar`, Vale counts), not only with an aboard agent selected (`DriverAgent()` today). The driver for the pill's face is the first selected able aboard, else the first aboard.
5. **ContextRank on the car:** Go first **only once Vale is aboard** (`Sub` go −1 only then) · Board 0 · Drive 1 · **Go before Vale is Tier 6** (after every calm way, never a candidate for the hold or the lit wedge; it still is not violent: no dash, no loud mark; the host marks it `How = "withdraw"`) (§C6). Drive is Tier 1 with Board, so without these sub-ranks a car with Board and Drive open ties and the hold falls to the ring.

### C2 · Group Board: what the hold, the ring and the pill show
- **Pill (hover, charge, ready):** the offer pill as built (`panels/pill_status`, 38 px, slice 0.43). `markers/disc_dark_24` with `car`, dim head "Hold", body `Board · 4 s`, then **a 24 px cameo for every doer** (cell order, overlapped 7 px, up to 4). The number is the slowest doer's walk + act. The sweep (turquoise-light at 40 %), "Let go" (turquoise-light), the 2 px ready ring and the 0.4 s commit are as built. Nobody is in the faces who the order won't take.
- **Ring:** one **Board** wedge for the group (the merge key is the order, so four agents' Board is already one wedge): `car` icon, the givers' cameos (14 px, 1 px apart) under the icon, pill `Board · 4 s` (`p_time`). What changes: taking it gives every giver the order.
- **Not a group member, no chip:** agents already aboard are skipped silently.
- **Going-loud card: none.** Board and Drive are calm: $0, 0 T, no Alert. The only card at the car stays the Go card (§C6).

### C3 · Seat pips, with a pending state
As built: five pips on a dark backing pill under the car marker (Vale 3.6 px radius, then Scout, Fixer, Tech, Enforcer 2.9 px, pitch 9 px). Three states:
| state | drawing | when |
|---|---|---|
| empty | cream ring, 1.2 px, over ink at 85 % (as built) | not aboard, no Board ordered |
| **pending** (new) | turquoise-light ring, 1.2 px, **dashed (3 dashes) rotating one turn in 1.5 s** | a Board is ordered and not landed: `pending[k]?.Do == "board"`, `Ops[k].Act?.Do == "board"` (walking in) or `Ops[k].Busy?.Do == "board"` |
| filled | turquoise-light disc (as built) | aboard (`Seats()`); on landing a **0.30 s beat**: the disc scales 1 → 1.6 → 1 and one ring leaves it (radius 1 → 2.2, alpha 0.8 → 0) |

So a squad board reads as three turning pips that fill one by one. The marker and pips show while any Board is pending as well as while anyone is aboard (as built: with the marker, the held lock or `AnyInCar`). On landing the portrait's status slot already takes the `car` badge (`UnderFrame`): unchanged, same frame.

### C4 · The why chip: a refused agent says why, in two words
- **Who:** each selected agent a Board (or a group Fire) order did not take, except one already aboard. **Where:** the roster's busy chip slot (`ColX + colW + 4`, centred on the portrait's row), the busy chip's own frame: `markers/plate_dark`, 36 px high, 16 px padding.
- **What:** the **cause's own kit icon** at 14 px cream (down → `s_downed`, inside → `s_inside`, hands full → `s_carrying`, looping → `s_looping`, running → `run`; the generic `refused` only for a code with no icon), 6 px, then the two words in Jost 12.5 cream. No bar, no seconds. About 100–120 px wide.
- **When:** from the order for **3 s** (`FireRefusalS`), fading out over 0.3 s; it gives way to the busy chip if the agent gets an order. One chip a portrait, never stacked.
- **Mismatch to fix (Aster, then Latch):** the words already exist for group fire (`FireRefusalWord`, GDD §06 "shows why on its portrait in two words") but never reach the screen: `HudToolkit.Less` shows a word-only plate as the icon alone (`hwWord`, my 049 row 9 ruling, "the plate keeps a number"). The cause icon plus two words restores what §06 says and keeps 049's icon-first rule. Use this one chip for both group Fire and group Board.
- Latch's first cut pops the short lock over the refused agent's head in the street (`BoardRefusals`). The street pop is the wrong place for it: the player's eye is on the car, and §06 says the portrait. Keep the pop only if it says the same two words; the chip is the spec.
- Words: Fable's list at the end (`less.done.board.*`).

### C5 · Drive is one obvious action
**5a · When the stand outlines show** (told and learned only, as 059 §1): whenever anyone is aboard, the final fight has not started (`FightStarted`), and the stand is not the one under the car. Two strengths:
- **Rest** (nobody selected is aboard, the car is not hovered or ringed, no Drive wedge lit): the footprint as built (4.6 × 1.9 m, radius 0.5 m, dashed 7 on / 6 off, 2.2 px over ink-deep 4.8 px at 60 %, a chevron at the nose) at **55 % alpha, no centre disc, no route**.
- **Live** (an aboard agent is selected, or the car or a stand is hovered, or the car's ring or panel is open, or a Drive wedge is lit): as built, full alpha, `markers/disc_dark_24` with `car` (24 px) at the centre. Rest ↔ live eases over 0.25 s.
- Held car: every stand struck at 45 % (as built). The yard before Vale's car is parked: the 059 "after Vale's car" caution (unchanged). Never in the final fight.

**5b · The car's ring.** Wedges in rank from 12 o'clock, at most 5 + ⋯:
1. **Go** only once Vale is aboard and the car is free: lit (as built).
2. **Board**, when a selected agent is outside and able (cameos of the givers).
3. **Drive**: one wedge for every open stand. Icon **`arrive`** (not `car`: Board, Go and Drive would be three identical cars on one ring; `HudModel.AtlasIcon` maps `drive-car` to `arrive`, which is also the pill's disc; the stand's centre disc keeps `car`, it is the place), the driver's cameo (14 px) under it, pill `Drive · 28 s` (`p_time`, seconds). With two or more open stands each wedge names its stand in Fable's target word (`Drive {stand}`, as the cabinet's "Loop the entrance camera" names its camera). **Hovering a Drive wedge lights that stand live and draws its route** (cream dashed 2.4 px, chevrons every 3 m): a hovered wedge sets that stand ghost's `Hover` exactly as a hover on the stand does.
4. **Go before Vale** (the withdraw): last, **never lit**, and after the word a **struck Vale cameo** (`badges/npc_councillor` 12 px in the chrome ring, a 2 px cream diagonal across it). No dashed outline and no loud mark: those are the violent ways' (Knock out, Kill, Fire), and Go is not loud. It takes the 0.5 s hold wedge (the fill from the middle out that a harmful wedge has) even when nobody would be left. Recommendation for Aster: today a lit Go heads the car's ring as soon as anyone is aboard (`Sub` go −1) and one click withdraws the cell; ending the attempt must be a deliberate hold.
5. ⋯ (the full panel, as built).

**5c · The hold on the car.** `MainWay` takes: **Board** when a selected agent is outside and able · else **Go** once Vale is aboard and the car is free (the GDD's default, "Go once aboard"; the Go card's 0.5 s hold still applies if anyone would be left; before Vale it is never a hold) · else **Drive** when exactly one stand is open and the car is free · else nothing (a click opens the ring; with two or more open stands the ring decides). The pill is the offer pill: `Hold · Board · 6 s` with every doer's face, `Hold · Go` with the left-behind line as built, or `Hold · Drive · 28 s` (the disc is `arrive`) and the driver's face. **While a Drive pill shows, that stand's footprint is drawn live with its route** (the same hover as the stand's), so "where" reads before the hold. Commit: the 0.5 s hold; no card, no confirmation.

**5d · The stand's own hold** (059 §3, unchanged): `Hold · Drive · 28 s` and the driver's face; the footprint solid turquoise-light; route preview.

**5e · While it drives:** the car marker and its pips follow the car (`GetawayWorld`); the destination footprint stays live and loses its disc on arrival; the driver's roster chip is the busy chip with the seconds (as for any timed act); the arrival is the `gain.parked` chip ("Parked", as built). No new mark.

### C6 · Go, the Go card, the goal line
- Go is the first, lit way **only with Vale aboard** (057 §4.2, now conditioned on Vale). The Go card is unchanged: one line, "Go" or "Go · leaves Tech (down), Scout" with a fate glyph per name, "+1 T" if seen leaving; the 0.5 s commit hold only when someone would be left (or, new, when Vale is not aboard: §C5 5b item 4). After a full-squad Board nobody is outside, so the card reads plain "Go".
- **Words clash (Fable):** the goal line "Drive off in the ▼car." (Vale aboard) now collides with the Drive order. Proposal below.

### C7 · Done-check (Latch, shots at 2x crop)
Squad of three selected at the square: hover the car (pill, three faces) · mid-walk (three pending pips, one filled) · landed (three filled, three `car` badges, selection empty) · the car's ring with Drive and the yard stand lit · the Drive pill on the car with the stand live · a Tech looping in the squad (his why chip: `s_looping` + two words, 3 s) · Vale aboard (Go lit first) and Vale not aboard (Go last, a struck Vale cameo, the 0.5 s hold).

---

## D · The sticky target cue

**Today:** a right hold gives a lasting fire order on a target id; it already follows the target (`Gun.Order == id`). What is lost is the player's eye: the 049 ring is a ground decal (depth-tested, hidden behind cover and legs) among a dozen rings in a fight. The cue below is a **screen-space mark over the head**, so cover never hides it, and it says who is locked on whom. The input question (X or hold RMB) stays for the fight proposal (M).

### D1 · The lock reticle (new)
- **Where:** the target's view head + 0.35 m, projected (a camera's head, a car's roof); a UI element (not depth-tested). 6 px above the fight's head bar; the verb pill's tail stands 6 px above the reticle (the rule the camera's window marker already uses). The crosshair is the glyph the roster's `stance_free` badge wears; the two never meet (one is on a portrait, one over a head).
- **Pieces:** `markers/disc_dark_24` drawn 26 px (the HUD's disc size) with `overlays/crosshair` 16 px cream inside it; under the disc a row of **the shooters' cameos**, 12 px (16 with the chrome ring), overlapped 4 px, up to 4, one per agent whose `Gun.Order == id`. Nothing else, no words.
- **States, by shape and motion only (no colour):**
| state | look | motion | when |
|---|---|---|---|
| **set** | solid | scale 1.8 → 1.0 over 0.18 s (ease-out) and one 3 px cream ring leaving it | the order lands (`FireLocks` add / a focus click) |
| **locked** | crosshair solid, full opacity | **still** (ours is still; threats close in or kick) | a shooter has line and range (`FireOne` phase aim or fire) |
| **held** | the disc at 70 %, the crosshair at 55 %, and a **dashed cream ring** (3 on / 3 off, 1.5 px) round the disc | the dashes turn 0.25 turn/s | the order stands but cannot shoot: drawing, walking in (`walk`), out of range, no line (`blocked`) |
| **released** | fades, scale 1.0 → 0.8 | 0.25 s | a new target, X, a new act, down, out of range (the "Too far" pop says so) |
- **The ground ring keeps its 049 "ours" ticks** (still). The reticle adds the head-height read; it never replaces the ring.
- **It follows the target 1:1** (anchored to the view each frame, no lag), through cover and crowds; the silhouette pass already draws the figure through walls.

### D2 · Off screen
A lock whose target is off screen gets an edge chip: `plate_dark` 22 px high, 52 wide, the lock disc (16 px) and the `pointer` icon (14 px) turned toward the target, at the screen edge on the line from the screen's centre; several stack 26 px apart. The same chip grammar as the rival car's (059 §1).

### D3 · How a re-target shows
- **Another target:** the old reticle releases (0.25 s) while the new one sets (0.18 s); a **hand-off line** joins them for 0.25 s: cream 1.6 px over ink-deep 4 px at 60 %, from the old reticle to the new, fading with the set. Cameos move with their agent.
- **The same target again:** one 0.25 s pulse of the reticle (1.0 → 1.15 → 1.0).
- **One more shooter:** its cameo slides into the row in 0.15 s.
- **Down:** the pips empty (as built), the reticle fades 0.4 s; the pill's beat ("Officer down", 1 s) stays.

### D4 · Reads (host only)
`Game.Ops[k].Gun.Order` (the engine's lasting order id: who is locked on what), `FireNow[k]` (target, mode, firing), `FireOne(k, id)` (phase: draw, walk, blocked, aim, fire → locked or held), `FireLocks` (set beat, focus clicks), `views.Find(ViewKey(id))` (anchor), `Game.GunOf(id).Down`. The pill keeps the live words (`FireStatus`: "Aiming · 1 s", "↓3 s").

### D5 · Hint for Sky's X-or-RMB confusion
Not a keycap on the fire line (Sky's rule). One hint at the first lock (Fable's list): "X stops fire." Retired for good once X is used.

### D6 · Done-check
The final fight at the square's car: a right hold on a rival with two agents selected (the reticle sets, two faces); the rival runs behind the planter (the reticle stays over his head, the silhouette shows him); he leaves range (held: the dashed ring) and comes back (locked); a right hold on the next rival (the hand-off line, one agent's face moves); the target goes off screen (the edge chip); X (released). Board 03.

---

## E · Locked ways show

**Three failures, one rule.** Suture's findings: a click where every way is locked opens no ring and walks the agent there; hover shows no pill; and with no calm way the pill offers only Fire. The rule: **a person or thing the selection can do anything on always answers, and never by walking.**

### E1 · What a left click does (new row in the table)
| under the click | the selection has | does |
|---|---|---|
| your own agent | | selects (as built) |
| a person or thing with ≥ 1 open way | | opens its ring, nothing taken (as built) |
| **every way locked** | locked rows only | **opens the shut ring (E2)**; no walk |
| **no row at all** (a cast person, a camera, a car) | nothing | **pops "who can" (E4)**; no ring, **no walk** |
| nobody in the cell has a row (scenery) | | walks there (as built) |
| a passer-by with no way | | the ground: walks (as built); with the Enforcer, his takedown ring |
| the ground | | walks; double click runs |

### E2 · The shut ring
- **Build:** the locked rows of the selected agents (each agent's own), deduplicated by verb, target and lock code (their cameos merged), ranked by `ContextRank.Compare` (Tier then Sub), the primary agent's first. Up to the ring's five ways + ⋯. **Ask, Pay, Help and Pressure that share one lock merge into one `Turn` wedge** (recommended: four identical "Listen first" wedges are noise) (`turn_person` icon, Fable's "Turn" word); different locks stay apart (Pay "Need up to $90,000", Ask "Refused before").
- **A wedge** is the existing shut wedge (Go-held, loop-holds): fill `Plate`, never lit, a 1.6 px cream strike across it, the verb's icon at 45 %, the pill at 75 %. The pill is: the word, then `lock` 12 px and **Fable's short lock in `--text-dim`**, placeholders filled by the host (`{tick}` as m:ss, `{m}` metres, `{usd}` dollars): "Call · Alarm up", "Listen to the guard · Already heard", "Take · Enforcer only". The wedge's cameo is the agent who has the way.
- **Nothing lit, nothing at 12 o'clock but the best-ranked shut way;** the dead zone keeps nothing. Esc, empty space or the middle closes it (as built).
- **A press on a shut wedge** pops its short lock with the lock's own icon at the wedge (`PopIcon(code)`, 2 s) and the ring closes: as the shut Go already does. Alt shows the long line.
- **The open ring is not widened.** Locked ways stay in ⋯ when something calm is open.
- **Violent ways come after shut calm ones.** If nothing calm is open, the shut calm ways take the slots before any open Knock out, Kill or Fire (which stay last and dashed), so a Fire wedge is never the whole ring while a calm way exists but is locked. Violent ways keep at least one slot.

### E3 · The locked pill (hover)
- **When:** no open calm way for the selection and at least one locked calm row. The way shown is the first of the **primary agent's** locked rows in `ContextRank` order (the agent a hold would have gone to; else the first agent with a locked row).
- **Frame:** the offer pill (`panels/pill_status`, 38 px, slice 0.43, the chrome tail) at **80 % opacity**; no sweep, no charge, no "Hold" head, no ready state, no prediction mark.
- **Contents:** `markers/disc_dark_24` with **`lock`** (cream), the verb and target in cream (the intent's `Label`: "Call Inez", "Listen to the guard", "Take Vale"), a dim dot, then the short lock in `--text-dim` ("Alarm up", "Already heard", "Enforcer only"); at the right end the **doer's cameo** (24 px).
- **Alt:** the long line (Fable's `line`) under the pill, on a `plate_dark`.
- **The cursor** takes the ring's three dots (a click opens the shut ring). The pill turns into the offer pill with a 0.15 s cross-fade if the lock lapses while hovered.

### E4 · No row at all: the "who can" pop
`plate_dark` 26 px, 2 s, `Feedback.Info`, at the click: **the selected agents' cameos (14 px) struck by a 2 px cream diagonal, a `pointer` (12 px), then the cameos of the agents who have a row there (up to 3)**. No words; Alt shows the existing `WhoCan` line. It replaces the sentence "Nothing here for the Tech. The Fixer can." (8 words, against the ≤ 4 rule). No hover pill in this case.

### E5 · Fire never stands in
Outside a fight (`!FightOnNow`) the hover pill is exactly one of: **the offer** (an open calm main way), **the locked pill** (E3), or **nothing**. It is never Fire: `HoldIntent`'s `FireOffered` branch runs only in a fight, on a hostile or a combatant, as built. Outside a fight Fire stays one right hold away (priced on the charge, as built) and on the ring, **last and dashed**. This narrows §06's "the fire offer shows on … security (guards, the patrol robot, police, cameras)" to "in a fight" (GDD line below).

### E6 · Reads and fixes (and the sidecar log)
`plan.Pn.Rows` (`Available == false`: `Lock.code`, `Reason`, `Unlock`), `ContextRank.Compare`, `Text.ShortLock(code)`, `LockLine`, `PopIcon(code)`. Suture's four edits stand (`ClickRing`, `OpenRing`, `HoldIntent` / `HoldPillFrame`, `HoldClick`: never `GroupWalk` to the feet of a thing with rows). The hold pill's "Listen the guard" is `Verb()`'s label; use the panel's name ("Listen to the guard", `Words()`). In Suture's sidecar (`ring: open|take|shut`, `pop: code`, `click: walk-from-thing`) the new states log as `ring:shut`, `pill:lock:<code>` and `pop:who-can`; `click: walk-from-thing` must stay 0 for any cast person or thing with a row.

### E7 · Done-check
Suture's: shot 05 (the Fixer on Inez at 4680) shows "Call Inez · Alarm up" on hover; a scripted left click there opens a ring with a shut Call and issues no Move; the Scout on the guard after 5:20.9 shows "Listen to the guard · Already heard" and a ring with Listen shut and Fire last and dashed; the Tech on the guard shows no pill and a click pops the faces (Scout, Fixer) and issues no Move. Board 02.

---

## F · Cones on demand, and click priority

### F1 · The rule
**A learned cone is drawn only when it matters now. Nothing is drawn "at rest".** The plot scope keeps every learned cone (the permanent, top-down memory); the street keeps the ones that matter; **Alt shows all** (held 0.25 s, as built). Deleted: the rest strength (fill 13 % → 1 %, rim 0.05 m at 55 %), and "Alert ≥ 2 lights every fan".

### F2 · When a learned fan draws (the host decides, once a tick per fan)
| # | a fan draws when | strength | stays after the cause | reads |
|---|---|---|---|---|
| 1 | it is the **hovered** thing's: a person (guard, officer, robot, rival member), a camera, the patrol, a rival car | strong | 0.15 s (the hover's hysteresis) | `Hover` → watcher id |
| 2 | the order being set **names** it: a ring wedge (`RingFocus`), the charge or pill on a Watch, Loop, Disable or Reroute, the open panel's target | strong | until the ring or charge closes, then 0.15 s | `RingFocus`, `Charge`, `HoverIntent`, `Panel.T` |
| 3 | it **can see** a selected agent now (the agent is inside its polygon and `Game.Detection()` names it), or **is about to** (below) | strong | 1.0 s | `InFan`, `NearFan`, `Detection()` |
| 4 | a selected agent is **acting on it**: a busy Watch, Loop, Disable or Reroute on that watcher; a **running loop** draws its camera's hollow, dashed "recorded" cone (058 §2) for the loop's 2:00; the patrol while it is away | strong; the loop's cone as 058 | while the act runs; **3 s after a Watch lands** (the reveal) | `Ops[k].Busy`, `LoopOn/LoopCam`, `PatrolAwayUntil` |
| 5 | **the walk being ordered or under way crosses it** (the pill's, the lit wedge's or the agent's own path ahead): the preview | strong | while ordered, + 1.0 s | `FanMatters` (as built, kept) |
| 6 | **Alt** | strong, all learned | 0.6 s out | `Peeking` |

**"About to see":** the host computes a time, not a distance. The agent is within `4 s × (the agent's speed + the watcher's speed)` of the fan's polygon (agent 0 / 1.4 / 2.8 m/s standing, walking, running; watcher 0 for a camera, its present speed for the patrol, guard, officers, robots and rivals), with a 3 m floor when the agent is walking toward it; or the agent's remaining walk enters the polygon within 4 s of walking. `NearFan` already measures to the polygon; the speeds are in `fHeadVel` / `op.WalkSpeed` and the units' walks.

### F3 · How a cone appears and leaves
In over **0.25 s** (ease-out) to the strong look as built (fill 34 % → 14 % by distance, rim 0.085 m at 100 %, each kind keeping its cue: camera hard fan with a dashed far edge, guard amber with chevrons, rivals violet with diamond ticks, a mast's heads numbered). Out over **0.6 s** (ease-in) after its linger (table). The floor is **0 (not drawn)**, so a cone that stops mattering leaves the street completely. A Watch's reveal comes in over 0.4 s and holds 3 s.

### F4 · With "only what the cell has learned shows a direction"
The rule is a **filter over `KnownFans()`, never a source**: a fan the cell hasn't learned has no polygon to draw, so "can see the agent" (rule 3) is read only over learned fans. An unlearned watcher noticing an agent keeps the "?" over its head and its attention cues (§05, as built). Police and rivals learn on arrival (rulebook `sight.kinds`), so their fans exist from then; the filter is what keeps them quiet until they matter.

### F5 · Police and rivals in a fight
While `FightOn` or any selected agent has drawn: the fans of combatant watchers (`security`, `rivals`, an engaged guard and patrol) **do not draw by rules 3–5** (the target rings of brief 049, drawn at their feet, already say who aims at whom); rules 1, 2, 4 and Alt still draw, so a hovered officer shows his cone on request. Cameras are not combatants and keep every rule.

### F6 · Third person
The same filter (the follow camera draws the fans as ground decals in perspective; the GDD's vertical curtain is not built). Pips over known watchers, the eye over the agent and the edge chevrons are cues, not cones: unchanged.

### F7 · Data fixes the rule needs (Latch)
`HostSight.WatcherOf` returns `null` for a `unit` target and `"rivals"` for a rival member (`Key` = `rival-N`), so hovering an officer, a robot or a crew member names no fan. Return `t.Key` for `unit` (view `unit/<id>`, watcher id = id) and for a member (`IsCrewId`). Remove `RestHub/RestFar/RestRim*` and the `g.Alert >= 2` term in `ShowSight`; the strength is 0 or 1, eased.

### F8 · Click priority in a crowded spot (the Office door at its busiest)
**Three principles:** what is *under* the cursor beats what is near it · what the selection can *act on* beats what it can't, beings before things · **a passer-by never covers a person, the door, a camera or the car.**

Sort the candidates by this key (first difference wins): `(not under the cursor, no open calm way, no row at all, kind rank, screen distance, ray distance)`.
- **Under the cursor** = the ray's hits; else the candidates inside the pick radius (28 px, as built). 
- **Open calm way / any row** = the selected agents' rows on it (`ContextFor`, only for the ≤ 4 within the radius, once per hover change).
- **Kind rank:** a being (person, officer, robot, rival, patrol, hostile) 0 · a body or a downed agent 1 · a camera 2 · the car or a stand 3 · the door marker, the cabinet, a stash 4 · a passer-by 9 (only when nothing else is within the radius; with the Enforcer, his takedown ring; else the ground).
- **Left click** keeps rule 0: your own standing agent within 26 px wins and selects. Hover keeps the 150 ms hysteresis (`Sticky`).
- **Pick shapes:** a being: the capsule (0.35 m, feet to head) · a camera: its head disc (24 px) and pole · the car: its ground footprint · the door: the marker disc (26 px), not the doorway's box · the cabinet and stash: their own body. The generous boxes of the door, cabinet and car are what let a passer-by in front of the door win today.

### F9 · How the stepping shows (wheel, Z, Tab)
- **The stack tab:** with two or more candidates under the cursor, a `plate_dark` **16 px high** seated on the pill's top edge, centred, overlapping the chrome 6 px: one dot per candidate (6 px, 9 px pitch), the active one **filled cream**, the others a 1.4 px cream ring (the dots as built in the old name chip, now on the pill).
- **The others are visible:** a dashed cream ring (0.5 m, 1.8 px over ink-deep, 60 %) at each other candidate's feet, so the player sees what the next step lands on; the active one wears its hover ring as built.
- **A step** (Z, Shift+Z back, Tab, the wheel): the pill's tail glides to the next thing in 0.12 s, the filled dot moves, the faces and price change with it.
- **The wheel zooms unless the stack tab is up and the cursor has rested 0.3 s on it**, then it steps (today it steps whenever `HoverStackCount > 1` and steals the zoom).
- One hint the first time a stack shows (Fable's list).

### F10 · Done-check
The Office front at 5:30 and at the busiest minute (the guard, the patrol, the entrance camera, the cabinet, the response and the rivals once they are out): no learned cone drawn at rest; hover the guard (his cone in over 0.25 s, out over 0.6 s after the cursor leaves); the Enforcer ordered across the camera's cone (it comes in with the order, leaves 1 s after); the Tech looping (the hollow dashed cone for the loop's 2:00); a Watch landing (3 s reveal); Alt (every learned cone); the final fight (rings only, no police or rival cones, a hovered officer shows his). A click on the busiest pavement: the door, the cabinet, the camera and a passer-by in the same 28 px, with the stack tab and a step. Board 03 (the tab).

---

## H · The conversation from the ground view

**Verdict: the §10 profile two-shot is right, and it works from the ground view, but the build rarely reaches it.** Brief 033's captures of the build (board 04, left) show the two shots Sky met: a pair more than 3 m apart is shot as **one person alone** (the other speaker is not in the frame; the bubble floats beside her), and when neither side is clear it falls to **over the shoulder** (the cell member's back fills a third of the frame, his face hidden, a palm in front). Neither is "agent and subject, faces visible, nothing occluding". So the answer is a **ladder of shots that always holds both people**, and people who are set to idle and turned to each other. It is a cutaway (hard cut, 0.12 s dip, back to exactly the view left, map or ground), so it does not care which view it left.

### H1 · The shot ladder (the first that works; view only, nothing the engine records)
1. **The profile two-shot** (§10), for a pair up to **6 m** apart. The lens on the pair's perpendicular, 1.9 m high, 8° down, 35° vertical field, a slow 8 % push-in over the scene, 6 % letterbox bars. Its distance is the frame's fit: `fit = clamp(1.44 × sep, 3.6, 8)` (the two centres span about 62 % of the width: at 1.5 m apart that is §10's 3.6 m, at 3 m it is 4.3 m, at 6 m it is 8 m; the build used `sep / 1.1 / tan(hfov / 2)` capped at 6 m and gave up beyond 3 m).
   - **Candidates, in order:** at `fit`, the perpendicular, then ±12°, then ±25° round the pair's middle; then the same five at `0.8 × fit` (never under 2.8 m). At an equal angle **the side the chase camera was on** (ground view: the player's left stays left across the cut), then the side the subject's face is toward. The angles stop at ±25° because a person turned to the other is 90° ∓ the angle from the lens: past 25° one of them shows more than 115° of back.
   - **A candidate is clear when** (a) the line from the lens to each head and each chest is 0.4 m clear of every solid (`ConvoClear`, as built), (b) each person's face is turned toward the lens by 115° or less (nobody shows his back), (c) nothing solid stands within 1.6 m of the lens. Props and people on those lines are hidden for the scene (as built: `ConvoHideOccluders`).
2. **The high two-shot** when no candidate is clear (the café door and the Office front are where this happens: a wall stands where the perpendicular lens would): **the play camera's own pitch (38°) at 7 m, the same 35° field**, centred on the pair, at the yaw the player left (the map's quarter-step, or the chase camera's). From above nothing stands between lens and pair, and the city's own occlusion language holds (§H4). Hard cut like the others.
3. **The intercut** for a pair more than 6 m apart (a call across the terrace, a Listen from 12 m): each speaker **alone, 2.6 m, from where they face**, cut with the bubble (the beat), so both are seen over the scene. Never one person for the whole scene. For a Listen the pair is the overheard speakers (as §10); the Scout, if he'd stand in frame, is hidden like any prop.
**The over-the-shoulder fallback goes**: it hides the agent's face and fills the frame with his back.

### H2 · The people: idle, facing, planted (Sky: "no walk, run or slide")
At the cut each figure is **set** (a pose, no 0.15 s cross-fade) to idle, feet planted, no root motion, and turned **to face the other** under the dip: the cell member to the subject, the subject to the cell member; both turn back at the cut out. Today the driven agent keeps the drive's gait a moment, and a person the paused clock holds mid-stride (Inez on her rounds) keeps animating `walk` on the spot: that is the slide. In the ground view the scene waits until the driven agent's speed is under 0.3 m/s (the act has begun, so he is standing).

### H3 · The bubble
At the speaker's head (`ConvoHead`, 1.8 m), **clamped to the frame minus the bars with a 4 % margin**, its tail flipping side if it would leave. One at a time, up to 3 s, as built. Kept on screen: the clock with its pause mark, the Alert pill, the bubble, the Skip keycap.

### H4 · The capsule cut and the silhouettes
| shot | architecture cut (capsule) | silhouettes (the flat cream-edged figures through what covers them) |
|---|---|---|
| 1 profile two-shot, 3 intercut (a low perspective lens) | **off** (a perspective lens is never cut; the build zeroes the disc, `radiusPerZoom 0.02`, `ghost 0`) | **off for the scene**, restored at the cut back (a render-feature toggle, view only): a cell member behind a facade would draw as a flat figure through the wall in a close-up |
| 2 high two-shot (the play pitch) | **on**, sized to the two speakers | **on**, as on the map |
Everything else on the street is frozen with the clock, and not drawn where it would cover the pair.

### H5 · Done-check
Inez (the Fixer, Help) and the driver (the Fixer, Pay) from the ground view at two headings each, plus a Listen and a Call: both feet planted and turned to each other, nothing between the lens and either face, the bubble inside the bars, no one-person scene and no back in the foreground; one scene forced into the high two-shot (a lens boxed in by walls); then the cut back to the chase camera at the same pose. Board 04.

---

## G · Camera and roster

**Small visuals only; the camera moves are Latch's** (portrait click selects and frames; third person enters behind the heading; the plan view returns centred at play zoom, same yaw).
1. **Portrait click = select + frame.** The camera glides to the agent (smoothstep, 0.5 s, at the current zoom). **Cue:** the agent's selected ring (`HostViews.CellRings`, the look of the kit's `overlays/ring_ground_selected`) pulses once as the glide starts, scale 1.0 → 1.3 → 1.0 and alpha 1 → 0.6 → 1 over 0.4 s, so the eye finds the agent at the glide's end. An agent aboard: the car marker pulses instead; inside the Office: the door marker. Shift or Ctrl click adds or toggles and does not move the camera (a group can't be framed at play zoom). A double click frames at **play zoom 14**.
2. **Third person in:** the swing behind the heading as built (0.5 s, no overshoot). No visual added.
3. **Back to the plan:** an eased glide (smoothstep, 0.5 s) up the same line the swing came down, **centred on the agent at play zoom 14, the same quarter-step yaw**, instead of the hard restore. **The world marks (`marks` layer) fade in over the last 0.2 s** so they don't pop in while the camera is still moving; no pill or ring is shown during the glide.
4. **No portrait cue.** The driven agent's portrait keeps its selected ring.

---

## HUD budget (peaks, % of 1080p; rest adds nothing)
| piece | px | % | when |
|---|---|---|---|
| why chip, up to 3 | 3 × 116 × 36 | 0.60 | 3.3 s after a refused group order |
| lock reticle with 3 cameos, up to 4 targets | 4 × (26 × 26 + 3 × 16 × 16) | 0.34 | a fire lock |
| edge chip | 52 × 22 | 0.06 each | a lock off screen |
| locked pill | about 300 × 38 | 0.55 | hover on a locked thing |
| who-can pop | 110 × 26 | 0.14 | 2 s after a click |
| stack tab | 60 × 16 | 0.05 | two things under the cursor |
| shut ring | as the ring | within the 22 % cap | a click on a locked thing |
| stand outlines at rest | world marks, not HUD | | anyone aboard |
All inside 15 % at rest, 22 % with a card or the ring, 30 % with Alt. Fewer cones at rest lowers the world's busy-ness; it is not a HUD measure.
**Measured on the boards** (the frame's own HUD plus the new marks, as `briefs/058/glib.py` `budget` counts it; baseline in brackets): the squad's Board pill 5.21 % (4.70) · the car's ring 6.02 % (4.70) · three why chips 4.96 % (4.70) · the locked pill 8.37 % (7.94) · the shut ring 9.14 % (7.94) · the guard's shut ring with Fire 8.81 % (7.40) · the who-can pop 7.48 % (7.40) · the lock reticle with three faces and the pill 7.97 % (7.76) · a re-target with an edge chip 4.82 % (4.70) · the stack tab on the pill 7.77 % (7.40). The 7.4 to 7.9 % baselines are Suture's frames, which carry the response and the bubbles.

## GDD lines for v0.26.0 (Aster's to place; replace, not add)
- **§10 cones:** "cones: learned only, drawn **only when they matter now**: hovered or named by a way, able to see a selected agent or about to (within 4 s), acted on by a selected agent, or on the path being ordered; **Alt shows every learned cone**; police and rivals in a fight leave the cones to the target rings; no sweep line; the plot scope keeps every learned cone."
- **§10 left click:** replace "a thing with no ways … walks there" with "a thing whose every way is locked opens its ring shut, each way with its lock; a thing the selection has no way on says who can; neither ever walks. A passer-by or scenery with no way is the ground."
- **§10 the getaway:** "one order on the car boards every selected agent who can, one Board each in the same tick; the car's pips fill as each lands (a turning pip while it walks in); a refused agent shows why in two words. Once anyone is aboard the car answers with no selection: Drive (one wedge a stand, the hold when one stand is open), Go first and lit only once Vale is aboard."
- **§10 conversation scene:** replace "a side two-shot, both in profile …" with "a profile two-shot of the two people (3.6 m from the line between them, more when apart, 1.9 m high, 8° down, 35° field), else the play camera's pitch at 7 m, else, for a pair more than 6 m apart, each speaker alone cut with the bubble; never one person for the whole scene, never over a shoulder. Played from the ground view as from the map: both people set to idle, facing, feet planted; the low shots stand down the architecture cut and the silhouettes."
- **§10 select:** "a portrait click selects and frames (a double click at play zoom)".
- **§06 group fire, last sentence of the offer:** "The fire offer shows on hostiles and combatants, and, in a fight, on security; outside a fight the pill never offers Fire (the ring lists it last, dashed, and the right hold still fires)."

## Hand-off
- **Latch:** C, D, E, F, H, G as above; E is Suture's four edits plus the shut ring and pill look; Drive on the car needs `CarRows` to add `DriveIntent` rows per open stand and `ContextRank.Sub` the ordering of C1.5. Check every state cropped at 2x before Sky sees it; I review the 2x.
- **Fable:** the words list.
- **Aster:** (1) the Go-before-Vale commit hold and ranking (§C5 5b item 4, §C6); (2) whether boarded agents always leave the selection (§C1.2), which an empty selection after a full-squad board makes visible; (3) the group-fire words that never reach the screen (§C4); (4) the GDD lines.
- **Suture:** the done-checks per section; the sidecar log is Latch's, outside the replay.

## Open issues
- The *stray ground click* risk (C1.2) is real with a group board: a move order on a boarded agent steps them out; `GroupWalk` does not skip `InCar`. The spec avoids it by taking boarded agents out of the selection; if Aster prefers them selected, `GroupWalk` must skip `InCar` and pop `car`.
- F8's sort key reads each candidate's rows (`ContextFor`, about 500 KB a preview per `Preview` call): bounded to the ≤ 4 candidates inside the radius, once per hover change.
- No frame of the real build exists for D's reticle, F's before/after or H's new shots; the boards are drawn from the kit over 0.2.122 frames and stand-ins (Suture's shots 01 and 05, `out/batch/shots`, brief 033's scene captures). A 2x crop review waits for Latch's frames.
- **F has no board:** the rule is a filter, not a new mark. The proof is Latch's: cones before and after at the Office front at the busiest minute (brief 067's list). The 0.2.122 frames show why (Suture's shots 01 and 02: long thin gold and red lines across the whole street from every learned watcher).
- H's ladder (the intercut and the high two-shot) is new behaviour for Latch and needs one look in the build at both headings; the frame-fit constants (1.44 × the separation, 62 % of the width) are starting values to tune in play.
- The brief's "going-loud card's role if any": none for Board or Drive; the Go card stays.

## Boards (`briefs/067/`, git-ignored; redraw with `python3 -I briefs/067/board0N.py`, library `g67.py` on `briefs/058/glib.py`)
- `board-01-squad-board-and-drive.png` (C): the squad's Board pill with three faces · the seat pips ordered, one landed, all landed · the why chips on the roster · the car's ring with Vale not aboard and with Vale aboard · the stand outlines at rest and live.
- `board-02-locked-ways.png` (E): the locked pill on Inez · her shut ring · the Scout on the guard once heard (locked pill, and the shut ring with Fire last and dashed) · the Tech's who-can pop.
- `board-03-lock-and-stack.png` (D and F): the lock reticle over a fight target with the pill above it · its five states · a re-target with the hand-off line and an off-screen chip · the stack tab on the pill with the others ringed.
- `board-04-conversation-from-the-ground.png` (H): brief 033's two as-built scene frames (one person alone; over the shoulder) and the plan and elevation of the ladder.
Frames: 0.2.122 captures (the square, the Office front, the final fight at the car, Suture's shots), the old pills blotted out where a new one stands; all HUD pieces redrawn from the kit's sprites, icons, badges and fonts at 2 px per game px. Placement is indicative (Latch projects from the engine). The looped, dashed and turning states are drawn at one instant.

## Words for Fable (≤ 4 words; yours to change)
- `less.done.board.<code>` (the why chip, 2 words): `agent-down` "down" · `enforcer-inside` "inside" · `board-running` "stop first" · `already-carrying` "hands full" · `board-tethered` "looping". (`in-car`: silent.) The existing `less.done.fire.*` table serves group fire.
- **Go before Vale** (Alt line only; the ring shows a struck Vale cameo): "Go without Vale ends the attempt."
- **Who can** (Alt line on the faces pop; the pop itself has no words): keep `WhoCan`'s "Nothing here for the Tech. The Fixer can."
- `ring-turn`: "Turn" (the merged shut wedge).
- **Listen's pill label:** "Listen to the guard" (the pill prints `Verb()`'s "Listen the guard").
- **Hints** (retire on first use): after the first fire lock, "X stops fire." (3) · after the first stack, "Scroll to switch." (3).
- **Goal-line clash:** "Drive off in the ▼car." now collides with Drive. Proposal: "Go in the ▼car." (4 with the marker) or "Leave in the ▼car.".
- **Lock shorts** the shut ring will print as written (check each reads alone): "Alarm up", "Already heard", "Already watched", "Listen first", "Watch first", "Refused before", "Need up to {usd}", "Enforcer only", "Blocked · down {holders}", "Away · {tick}", "Loop ends {tick}", "Busy looping".
