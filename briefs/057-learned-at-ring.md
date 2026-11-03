# 057 · What the cell learned, at the ring (and three more readings from the 0.2.99 shots)

**State:** designed 2026-10-24 (Glyph), boards in `briefs/057/`. Nothing built. Next owner: Latch (spec below), then Glyph reviews the 2x crops. Inputs: Sky's three 0.2.99 shots, brief 044 (revised A), brief 054 `hud/hud29.spec.json` (mood icons, prediction marks, Watch absent once seen). Boards come from `python3 -I briefs/057/boards.py` and `cone.py` (they redraw pills from the game's own kit sprites and fonts, checked against Sky's real Pay pill: same plate, icons, spacing within 4 %).

Four parts: 1 learned at the ring · 2 what Reroute moves (and the other verbs with the same gap) · 3 quiet cones and the scan line · 4 the seal clock, the car's state and the fail line.

---

## 1 · Learned at the ring

### Sky
2026-10-24, on the ring over Inez: "wondering if we could show what's been learnt / new from previous action e.g. by scout listening?"

### What exists today (read from the code)
- **The sparkle on Pay** (`HudToolkit.Ring.cs:75`, cream four-point star at the wedge's outer edge) means **a way opened**: `host.Fresh(target, order)`, set by the landing detector (`HostDone.cs`, brief 043), true for 90 s or until the order is given. In Sky's shot it is Pay opening after the Watch taught the routine. It says "go here", never "because of what".
- **The Done chip** (brief 043) already lists the facts a landing gave for 4.2 s over the person, with the doer's face. It is gone by the time you open the ring.
- **Intel** holds the standing facts (need circle, temperament rounded square, secret tab; solid = heard, dashed = told, dotted `?` = unknown). The ring shows none of them. The dashed `?` at the end of Pay's line is the unknown temperament (outcome unknown, `Quote.Outcome == "?"`).
- Facts in play for the Handover (engine): `Person.KNeed / KTemperament / KSecret` (Listen), `KRoutine` (Watch), `Suspected` (told), `Quote.Mood / Likely / Hurt` (engine 0.29). Inez is Fearful, debt, secret affair with the guard.
- What each fact does (people.rules): need gates Help and Pressure; temperament settles the outcome mark on Pay, Help and Pressure (Fearful: Pay one step worse = $90k, Help two orders, Pressure folds free); the secret is Pressure's leverage (`Quote.Leverage`); mood moves the fit one step.

### Options (board `board-01-options.png`, over Sky's shot, after Listen on Inez)
| | Where the facts sit | New | Link to the verb | Cost |
|---|---|---|---|---|
| **A  Dossier tag** | the three Intel slots on a small plate hung on the hub's rim | the kit's unread dot on each | shape twins (blank silhouettes) on the wedge it priced | covers her feet in the hub; the twins are blank shapes, so you must match them by outline: weak link; duplicates Intel |
| **B  Reasons in the pill (pick)** | on each way's own price line: the facts its quote read, then its mark | the unread dot on the new fact; a dim struck old number when a number moved | the placement is the link (the fact is on the line it prices) | the pill grows by 15 to 60 px; at rest nothing changes |
| **C  Burst on the wedges** | only the facts the last act taught, on the wedge they priced | the unread dot | placement | nothing standing, so you forget them once seen; crowds the wedge (three tokens on Pressure) |

### Pick: B, reasons in the pill  (SUPERSEDED by 1b below: Sky, 2026-10-24, "on the action ring, not the price box which is already too long and hard to understand")
- It is the pillar made visible: knowing more makes a person cheaper, and the fact sits beside the price it changed. "Fearful" next to the half mark on Pay reads as the reason.
- It adds no widget and no new place to look. The ring is transient, so the rest budget is untouched. The action panel rows and the hover chip build their lines from the same `HudModel.PriceLine`, so one change reaches every surface.
- A and C stay as fallbacks (C if Sky finds the pills too long).
- Does not clash with the sparkle: **star (cream, on the wedge) = a way opened; dot (kit's unread dot, on a token) = a fact is new.** Different place, shape and colour; after Listen they sit together (Help: star on the wedge, dots on its tokens) and read as "go here, because of this".

### Spec for Latch (board `board-02-pick-states.png`, `board-03-pick-detail.png`)

**Tokens** (the Intel card's own slot shapes, so they are learned once)
- need: circle 15 px · temperament: rounded square 15 px, radius 4.5 · secret: tab 19 × 15, radius 4. Fill `SlotInk` (#0a2624, as `HudToolkit.Intel.cs`), 1.3 px cream rim (solid = heard; long dashes 3 on / 2 off = told; dotted + `?` = unknown, not drawn in B), icon cream 10 px.
- Icons, exactly Intel's mapping: need = `p.Need` (`debt`, `cash`, `housing`, `papers`, `kin`, `care`, `protection`, `advancement`); temperament = `p.Temperament` (`idealist`, `mercenary`, `fearful`, `loyal`); secret = `kin` for an affair, else `secret`. All in `icons/singles`.
- 4 px gap between tokens; the tail is `[tokens][mark]`, the mark being the existing `badges/mark_ok|half|no` (solid, once the temperament is known) or the existing dashed marks from 054 (a Watch's likely/worse). The dashed `?` stays when the temperament is unknown.
- Which tokens, per way (known only; the quote's inputs): Pay: temperament. Help: need, temperament. Pressure: secret, need, temperament. Ask, Watch, Listen, Loop, Reroute, Call: none.

**New**
- `markers/dot_unread` (kit, turquoise dot with a white ring, 14 px @1x) scaled to **6 px**, centred on the token's top-right corner, 0.8 px inside the rim so it never leaves the pill's plate (checked at 3 px per game px).
- Where the data comes from: the landing detector in `HostDone.cs` already diffs the engine's knowledge per role (`knownBefore`: sight, routine). Extend that diff to need / temperament / secret (read `Person.KNeed`, `KTemperament`, `KSecret`, `Quote.Mood`), and add `HashSet<string> factsNew` keyed `role|need|temperament|secret|mood`. Host-only, nothing hashed, replays untouched.
- **Clears** when the ring (or the action panel) has shown that token for **1.2 s of unscaled time** in total (accumulate across openings), or when a way that token prices is given. Never by a timer alone: a fact learned and never looked at stays new. The Done chip's 4.2 s is separate.
- The sparkle keeps its meaning and its 90 s.

**The price pill shows the change**
- The dashed `?` becomes the solid mark with the temperament token in front of it (board row 2). No number moves for Inez's Pay (90k was already the worst case).
- When a number does move (the driver, Mercenary: Pay 90k worst case → 70k), draw the **old value dim and struck** (Jost Regular 11.5, `--text-dim` #b0cdc7, a 1 px line through it) just before the new one, while the fact is new; it goes with the dot. Source for the old value: cache the last `PriceLine` fields shown per `role|verb`; strike a field only when the new quote differs and a fact in `factsNew` explains it.
- A way that opens because of a fact (Help, Pressure after Listen) just appears with its sparkle and its tokens; there is nothing to strike.

**Mood** reuses 054's marks: no token, the dashed likely/worse marks and the disc over the head stay as specified. After Listen the card is exact (054 §4.3) and the solid mark carries it.

**Sizes and checks** (kit rules): plate_dark at the pill's 30 px element; type Jost Regular for the verb word and Jost Bold for numbers, as built; no new colour (SlotInk, cream, turquoise from the kit sprite, `--text-dim`). The Pressure line is the longest: about 315 px at 1080p (Pay 290). The left pill of a ring grows leftwards, the right pill rightwards, as `RingFrame` already places them.

### Open and limits
- **Icon gap:** `kin` (adult and child) reads as family where Intel uses it for the affair. The kit has no lover or heart icon (`s_hurt` is a half heart for health). Say if you want one drawn (Glyph, kit icon, later).
- The boards redraw the wedge order for four wedges (Pressure, Pay, Help, Ask); the live order is `ContextRank`. The ring's left-hand pill needs about 290 px: Sky's shot is a crop, so the boards extend the canvas to the left.
- **No 2160 batch shot has a ring open** (`out/batch/shots` has none), so no board uses one. Latch: a batch shot of the ring on Inez after Listen would let me do the 2x review on a real frame.
- Numbers on the Help and Pressure pills are people.rules values plus an assumed 5 s walk, not engine quotes.

---

## 1b · Learned at the ring, second pass: the facts ON the ring, and a shorter pill

### Sky
2026-10-24: "I was talking about on the action ring, not the price box which is already too long and hard to understand." B (facts on the price line) is out. Brief: facts on the hub, rim or wedges; the kit's unread dot, distinct from the sparkle; icons only; no additions to the pill; propose a shorter pill that still shows the full price before commit.

### Options (board `board-09-ring-options.png`, over Sky's shot after Listen on Inez; detail `board-12-ring-detail.png`)
| | Where | Link to the verb | Cost |
|---|---|---|---|
| **A  Under the verb (pick)** | a caption row under each way's icon: the facts its quote read, then the outcome mark they give (the mark leaves the pill) | placement: the fact is on the wedge it prices | the verb icon lifts 8 px; the row is the slot the group cameos use today (`mid.y + 7`): when a group is selected, the cameos sit first and the row moves under them; Pressure's row is the widest (4 items, 61 px) |
| **B  On the hub** | the three Intel-shaped facts seated in the hub's rim, each toward the ways it changes (need on the Help/Pressure seam, secret under Pressure, temperament at the foot); the mark badges each verb icon | direction only: weak for temperament (it prices every way) and wedge order is dynamic (`ContextRank`) | covers the person's feet and head; the seam rule fails when the two ways are not neighbours |

### Pick: A, under the verb
- The facts sit where the eye already is when it reads the price: on the wedge. "Fearful" under Pay with the half mark reads as the reason; debt under Help reads as why Help is there.
- The pill loses the outcome mark and so gets shorter, not longer: the ring carries why and what, the pill carries cost.
- **Unread dot** = the kit's `markers/dot_unread`, 5.5 px at the token's top-right. **Sparkle** (cream four-point star, wedge's outer edge) = a way opened. Different shape, colour and place; after Listen both show, "go here" and "because of this".
- No new colour, no words. Tokens are the Intel card's shapes (circle need, rounded square temperament, tab secret) at 13 px with a 9 px icon (Intel's 15/10 scaled 0.9), `SlotInk`, 1.3 px cream rim, kit icons, as 1 specified (same mapping, same which-facts-per-way: Pay temperament; Help need + temperament; Pressure secret + need + temperament). Marks are the kit's `badges/mark_ok|half|no|unk` at 13 px. The unknown temperament is `mark_unk` (dashed ?) alone, as the pill showed it.
- Clears as in 1: 1.2 s of unscaled time shown on the ring, or the way given. The data source (`factsNew` from the landing detector) is unchanged.
- States: board `board-10-ring-pick-states.png` (before Listen, just landed, seen).

### The pill, shorter (board `board-11-pills.png`: today, the lit wedge, the other wedges)
Pay is 264 game px today. Rules (1 to 3 together take Pay from 264 to 209 px):
1. **The outcome mark leaves the pill** (it is under the verb now).
2. **The shut eye leaves** (nobody sees: the normal state, drawn near-invisible in ink on ink). The eye shows only open (a witness will see) or unknown.
3. **Alert is the siren alone** when the risk is 1 (the number only above 1, or the `≥2` form). Trail keeps its number.
4. **The other wedges show only $ and time** (the price you pay now); the lit wedge, the one under the cursor or the default `rs.Lit`, shows the line with Trail and Alert. Clicking commits the lit wedge, so the full price is on screen before commit.
Result: lit Pay 209 px (79 %), the others 140 px (53 %); Pressure 188 / 119; Ask 156 / 89. Alt (breakdown) is unchanged and shows the full line with every field and number.
- **Unusual shows**: a seen order (open eye) or `≥2` is the thing to read, so it stays; board row 6.
- **Considered and left out**: merging Trail and Alert into one number (different clocks: campaign evidence vs security now) and moving secondary costs to hover (a hover is what lighting the wedge already does).
- **Needs a yes from Aster/Motive/Fable**: rule 3 and rule 4 trim the line the GDD names ($, time, Trail, Alert) on unlit wedges, and hide Alert's number when it is 1. The text.json `price.verbs` field order is Fable's; Latch implements by level (`HudModel.PriceLine(..., level)`: full for the lit wedge and Alt, min for the rest).

### Spec for Latch
`HudToolkit.Ring.cs`: in `RingFrame`, lift the icon 8 px when a wedge has facts or a mark (the `group` lift is 6); add `w.Facts` (a row, the width of its items, centred at `mid + (0, 11)`) with tokens from `host.Facts(target, order)` (the per-way known facts from the same quote that gives `Quote.Outcome`), then the mark. The pill drops the `mark` field and, off the lit wedge, `trail`, `alert`, `eye` (kept when open or unknown). Sparkle unchanged.
Limits: boards are mock-ups over Sky's shot; the real ring's order is `ContextRank`, so the wedge that holds Pressure may not be at 12 o'clock; mood is not drawn (054's disc over the head stays); a 2160 shot of the real ring is still wanted for the 2x review.

---

## 2 · What is Reroute moving?

### Sky
0.2.99, the Tech's ring at the control cabinet: the pill reads "Reroute ⏱ 80 s ☝ +1". "not clear what we're rerouting here".

### The rule (checked)
`rulebook`: `reroute-s` 45 (the act) + the walk to the cabinet = the 80 s on the pill, `reroute-trail` 1, `reroute-away-s` 115. `Game.PatrolPose()`: the patrol robot sweeps between `patrol-a` (68, -44.8) and `patrol-b` (52, -44.8) in front of the Office door (`patrol-period-s` 100). While `PatrolAwayUntil > Tick` it stands at `patrol-reroute` (19.2, -47), the pavement by Rocket Lane about 45 m up the street, facing away, then returns (`patrol.returns`). The host's pill is `I("reroute","patrol",...)` with the label "Reroute the patrol", but the ring shows only the word "Reroute", and `RingFocus` stays empty (`Named == ""`) because there is only one such wedge, so nothing in the world is highlighted either.

### Pick (boards `board-04-reroute.png`, `board-05-remote-verbs.png`)
- **The rule:** when a way's object is not what the ring is open on (the Tech's ring is on the cabinet, the thing moved is the patrol), the pill wears **the object's cameo right after the verb word**: the kit's chrome ring (2 px #cbc6c3 over 1 px ink-deep), 18 px, the object's badge inside. The price line is unchanged.
- **Reroute:** cameo = `badges/signal_unit`, the patrol's badge the action panel already uses (`HudModel.Badge`). **Missing:** there is no cream-enamel robot cameo with the dot-matrix face (the `npc_*` family). Until Forma and Glyph make `badges/npc_patrol`, `signal_unit` stands in. Request: Forma (face art) then Glyph (disc).
- **Hover marks** while the Reroute wedge is the hovered or lit one (the same place `RingFocusFrame` draws the focus ring; set `RingSeg.Named = "patrol"` for reroute so `RingFocus` fires): cream over ink-deep, the cone language of 054 (cream 0.075 m over ink-deep 0.17 m at alpha 0.6), no new colour:
  - **where it is and sweeps now:** the patrol's ground ring (solid, hover-ring size) with its cameo above, and its sweep as a solid line between the two ends with a short cross-tick at each end. Draw the ends from `patrol-a` / `patrol-b` read from the site, never typed in, so Pulse's move of the north end follows.
  - **where it goes:** a dotted route (0.35 m on, 0.35 m off) from the sweep to the wait spot, a filled chevron every 3 m. Use the walk grid path if the host animates one, else the straight line.
  - **where it waits:** a dashed ellipse (the near-body ring's style, radius 0.9 m) with the cameo on it. When the wait spot is off screen (it usually is: 45 m), an **edge chip** at the screen edge: `plate_dark` 22 px with the cameo and the `pointer` icon rotated toward it.
  - Nothing else: no seconds on the world marks (the 115 s is the window marker's after it lands).
- **Placement in the board is indicative:** I could not project the real points (no camera matrix in the shot data); Latch projects them.

### Other verbs with the same gap
Checked every verb the cell's rings offer (`HandoverHost.cs` intents):
- **Reroute (worst):** no object, no focus.
- **Loop from the cabinet:** has the tag word (entrance / pickup / mast) and a focus ring on the camera, but "mast" is cryptic. Give it the camera cameo (`badges/signal_camera`) before the tag. Loop pressed on a camera itself needs none (the ring is on the object).
- **Call (Fixer on a turned Inez or the driver):** the ring is on the person, the thing moved is the door guard (or the pickup car). Cameo `badges/signal_guard` (Inez); for the driver, a `car` icon disc. The hover marks use the same grammar: the guard's route from the door to the café (solid where he stands, dotted where he goes, dashed ring where he waits).
- **No gap:** False tip (ring is on the rivals), Enter (on the door), Take Vale, Board, the four turns, takedowns (all on their object).
- **Colour flag:** the kit's `signal_*` badges are drawn in the signal colours (red arrow, amber shield and unit, violet diamond). They already ship in the action panel header, so they have a kit source, but on a pill they bring colour, which brief 044 forbids for kind and role. I used them because there is nothing neutral yet; the proper fix is the cream `npc_*`-style cameos for machines (the same missing asset). Aster: decide whether the interim stands.

---

## 3 · Quiet cones, and the sweeping line

### Sky
0.2.99, Office forecourt: "once watched its cone now permanently appears but we need to make it softer, kinda dominates and for camera there's some sweeping line, not sure what that's for".

### Why it is permanent
`HostSight.ShowSight` falls back to `ScoutWatch()` (the last Watch the Scout ordered, remembered in `lastWatch`) after the hover and the panel: the learned fan stays drawn for as long as that Watch is the last one. `HostViews.Build` draws it at `FillAlpha 0.38` flat plus a 0.085 m rim, 38 % red over the whole forecourt.

### Pick (board `board-06-quiet-cone.png`, `board-07-quiet-cone-detail.png`)
**Two strengths of one cone, red only, unlit and emissive, outside the grade, as now.**
- **Rest** (a learned cone the player is not looking at): fill **13 % at the lens fading linearly to 1 % at the far edge** (distance fade: a vertex alpha per fan vertex, hub 0.13, ring 0.01 by reach), rim **0.05 m at 55 %**, far edge still dashed (camera) or chevroned (guard). The paving, people, plates and markers read through it.
- **Strong** (when it matters): fill **34 % fading to 14 %** (the same distance fade), rim **0.085 m at 100 %**: what the fan looks like today, but fading. Eased in over 0.25 s and out over 0.6 s.
- **Strong when:** (1) hovered, or its Watch row, its ring wedge (`FocusWatcher`) or the Alt layer names it; (2) a selected or any cell agent stands inside it, or within 3 m of its edge heading in; (3) a running or ordered path crosses it (the order's walk target segment, or the agent's current path, against the fan polygon); (4) Alert ≥ 2, or the watcher is noticing something now (`Game.Detection()` Noticing for this watcher). Otherwise rest.
- **Stop the permanence:** keep the `ScoutWatch()` fallback only while the Scout is acting on that watcher (the busy Watch), and after that let the cone rest at the quiet strength. Learned cones are then always visible at rest strength (Sky said "permanently appears"; the rim is the memory), never full.
- Implementation notes for Latch: `FillAlpha` becomes per-vertex colour alpha on `d.Fill` (the fan mesh is already a triangle fan from the hub: hub alpha = a0, ring alpha = a1); the signal material must multiply vertex colour (check `host.SignalMat`; else three concentric steps at 13 / 7 / 2 % behave the same). One float `strength` per `FanDraw`, eased each frame; the rim quad width and alpha follow it (rim alpha through a vertex colour as well, or two rim materials).
- Guard cone: same two strengths. The guard's fan, 054's civilian wedge (cream rim, no fill) and the security ring are unchanged.

### The sweeping line: cut it
- It is `HostViews.Scan` / `ScanEdges` (brief 020): a bearing line that swings between the fan's reach edges with two fading ghosts, for the camera and the guard. A fixed camera does not pan, so it says "this camera turns", which is false, and Sky cannot read it.
- **Real meaning available:** the desk reads each camera every 20 s (`camera-scan-s`, `report.scan`), so a sighting reaches the desk at the next scan. I looked for a wordless, readable form of one slow pass per 20 s and do not recommend it: it asks the player to learn a clock to read a line, it adds motion to a frame that is already busy, and the same fact already has a home: the **"reported" tag's draining underline** is the time until the desk sees it (visible in Sky's shot). The scan stays there.
- **How camera and guard cones stay apart without it** (already built in `RimOf`): the camera is a hard-edged straight fan from the lens with a **dashed far edge**; the guard's cone is amber with **chevrons marching the way he faces** on the far edge; rivals' diamond ticks; a mast's heads are numbered. Nothing is lost.
- Delete `Scan`, `ScanEdges`, `d.Scan`, `A0`, `A1` and the three line renderers per fan (also three fewer draws per fan on the GPU Resident Drawer path, brief 045). Parked, not built: an optional one-shot pulse of the rim when the desk reads (log code for a scan), only if Sky asks for the desk to be visible.

### What else competes in that frame (a list for later, nothing redesigned)
1. The two stacked **"reported" tags** with their amber ring beside each (overlapping each other and the fan's edge).
2. The **"Camera dark" plate** (cameo, tick, words, a second small eye-and-cameo pill under it): two plates for one fact, clipped by the right edge in Sky's crop.
3. The **eye disc** (dark disc with an open eye) standing under the guard.
4. The **guard's head cameo** with its dashed red ring, the **speech line** ("This is the post. Reporting an incident.") wider than the cameo, two small badges (shield, signal) and the white leader lines to the guard.
5. The **Fire pill** and its robot ring at the top-left (the focus ring on the camera pole plus the pill's chrome tail).
6. The **ground rings** on the passers-by and the lit corner of the old fan's rim, which the new rest strength removes.


---

## 4 · The seal clock, the car's state, and a fail card that says what happened

### Sky
0.2.99, lost at 7:56. "District sealed. Sealed off before Vale reached the car." and, on the same card, "Vale reached the getaway car." Sky: "huh? I failed mission for shots heard but that was a long time ago..."

### Facts (read from the code)
- The seal starts when the response unit confirms at Alert 2 or more (gunfire: from the unit's arrival) and runs `seal-s` (145 s at the watchful level; `response.levels[].seal`: 180, 145, 145, 120, 90). The engine's `Lockdown` tick; `Game.cs:379`: `Tick >= Lockdown` fails `sealed` "before the getaway car leaves" (brief 018).
- The win is Vale in the car **when it leaves**, and leaving is the player's **Go**. `GoQuery`: refused `car-empty` when nobody is aboard (Vale counts), `car-held` (naming the holders) while a combatant holds the car (`fight.hold`, the exit lane). So "Vale reached the car" (chain `vale-in-getaway-car`) is true and the run still fails. Three states end on the same card line: nobody had boarded; Vale was aboard and Go was never given; Vale was aboard and the car was held.
- Today the seal time exists only in the feed card ("The district will be sealed at m:ss", `seal.starts`) and the response rows. No clock. The held marks already exist (`HudToolkit.Less.cs` ~1448-1545: the held lock with the holder count on the car marker, a no-entry disc on each holder with a dotted tether, the exit lane, the 8 m ring), but only with the final-fight `ring` and as a pulse after a refused Go (`HolderPulseAt`).
- I cannot tell from the card which of the three states Sky was in ("Final fight: the police, 5 against you" suggests held). Keel can read it from a replay.

### Board
`board-08-seal.png`: Sky's card before and after, the clock bar with the seal marker in three states, and the Go way ready and held.

### (1) The seal clock: a window marker under the clock bar
- **What:** the window marker of brief 044 (plate_dark at 38 px), hung under the top-right clock pill, right edge on x = 1795 at 1080p (left of the known-people discs), so it never covers the pill. Contents: `lock` icon 17 px (kit `icons/singles`), the seconds left as **m:ss** in Jost Bold 15.5 (the clock bar's own format, so 145 s reads 2:25), and the 2.5 px draining underline, turquoise-light, over the seal's whole span. **Amber text and underline, blinking, in the last 10 s** (the window marker's rule; `#f0a020`).
- **When:** from the tick `Lockdown` is set (`seal.starts`) to the car leaving or the fail. If the engine pulls the seal earlier (`seal < Lockdown`) the number follows (the host reads `Game.Lockdown` every frame); the underline's total is the span since the marker first showed or last moved (the `windowSpan` cache the other windows use in `HostDone.cs`).
- **Source:** `Game.Lockdown` (`int.MaxValue / 4` means none), `Game.Hz`, `Game.SealS`. Host-only, nothing hashed. Best added to `Windows()` (kind `seal`, thing `district`) so the marker, its close pop and the log row come from the one list the other windows use; Keel's call.
- **Cause on hover, as an icon:** a kit `disc_dark_24` in the chrome ring (30 px) left of the marker, holding one icon for the origin the engine already keeps (`sealOrigin`, `FailCause.Origin`): `fire` = the shooting (loud-fight), `eye_open` = a missed check-in or a camera (desk), `radio` = shots heard, the `signal_police` badge = an officer or robot reported it (response, confirmed). No sentence.
- **At set:** one pop on the clock bar (icon `lock`, no words) alongside the existing feed card; Chime may add a cue.

### (2) The car's state
- **Go ready:** once Vale is aboard (`Game.AnyInCar` counts `Vale == ValeAt.Car`), Go is the **first, lit way** on the car's ring and plate (`ContextRank`: Go first while the car holds anyone and is free) and its wedge carries the sparkle (the Board landing opens it: brief 043's `Fresh`). The pill is as built (`Go`, the left-behind cameos, `+1` if seen leaving). While the seal runs, the goal line reads Go (`car` icon and the doer's face) in place of "Take Vale alive."
- **Go shut (held):** the wedge shuts like other shut ways (struck, no colour; `goBlocked` already dims it to 45 %). The pill tail shows `lock` and **one police badge per holder** (`badges/signal_police` in chrome-ring cameos, from `CarHolders()`). The world shows the **same holders** with the marks that exist (no-entry disc, dotted tether, exit lane), drawn **steadily whenever the car is held and Vale is aboard or the seal is running**, not only in the final fight and not only after a refused press (the pulse on a refused Go stays). This is a change to the `ring` condition in that block; nothing new to draw.
- **Go never pressed:** parked unless Sky asks: after 30 s with Vale aboard, a free car and no Go, the Go wedge's sparkle repeats once and the marker's lock pulses once. The ready wedge leading the ring is the main fix.
- `car-empty` (nobody aboard): Go is not offered; Board is the next step.

### (3) The fail line: say what actually happened
**Where the line comes from.** `contracts/missions/handover/text.json` is hand-written, not generated (author Fable, schema `suborned.text/1`). The card's lines: `fails.sealed` (the sub-line, line 206), `fail.sealed` (the title, "District sealed.", line 1476) and `failcam.caption/tip[sealed][why]` (~line 1619). The story line "Vale reached the getaway car." comes from a hard-coded map in `HudModel.cs` ~390 (`vale-reaches-getaway-car`, a placeholder; the engine's chain code is `vale-in-getaway-car`), so that file is Latch's until Fable moves the words into text.json. **Fable edits text.json; Latch wires the keys and the story map; Keel supplies the car state.**

**Engine ask (Keel):** at `Fail("sealed")`, record the car's state in `FailCause` (a read, nothing hashed): `Car = "empty" | "ready" | "held"`, plus the holder ids when held (`CarHolders()` just before the fail; `carWasHeld` already tracks the last hold state).

**Words (plain copy: what happened, why, what to do), for Fable to polish:**
| car state | `fails.sealed` (the card) | `failcam.tip.sealed` (next time) | story line added to the debrief |
|---|---|---|---|
| held | "Sealed in. Police held the car, so it could not leave." | "Clear the officers at the car, then press Go." | "Police held it." after "Vale reached the getaway car." |
| ready | "Sealed in. Vale was in the car, but it had not left when security closed the district." | "Press Go as soon as Vale is aboard." | "Go was never given." |
| empty | "Sealed in. Vale had not reached the car when security closed the district." | "Get Vale to the car before the seal." | none (the story already says where he was) |
| unknown (fallback) | "Sealed in. The car had not left when security closed the district." | | |

The card line and the story line read the same `FailCause.Car`, so they cannot disagree again. The origin captions (`failcam.caption.sealed.*`, "Shots were heard. District sealed.") stay: they say what caused the seal (what Sky remembers); the new line says why it cost the run.

### Done-check and limits
- Board done; nothing built. The mock sits on the office-front batch frame; the marker (about 80 px wide at 1080p) clears the known-people discs by 8 px.
- Not verified: which car state Sky's run was in (needs the replay).
- Next owners: Latch (sections 1 and 2), Keel (`FailCause.Car`, optionally `seal` in `Windows()`), Fable (the table), Glyph (2x crop of the marker on a real frame).

---

## Done-check
- Boards in `briefs/057/`: `board-01-options.png`, `board-02-pick-states.png`, `board-03-pick-detail.png`, `board-04-reroute.png`, `board-05-remote-verbs.png`, `board-06-quiet-cone.png`, `board-07-quiet-cone-detail.png`, `board-08-seal.png`. Sources `kitdraw.py`, `boards.py`, `cone.py`, `seal.py`.
- Not done: nothing in Unity, sim or contracts was edited. The cone board recovers the fan from the picture (colour mask, gain match), so its edges carry a little artefact; the real result is Latch's.
- Next owner: Latch (all three specs), then Glyph (2x crop review on a real frame); Forma and Glyph (patrol cameo); Aster (the `signal_*` colour flag, the missing lover icon).

## Sky's calls (2026-10-24)
- §1: not on the price pill ("I was talking about on the action ring, not the price box which is already too long and hard to understand"). Glyph redoes it on the ring and proposes a shorter pill.
- §2: yes; route lines thin; after Reroute the new route stays drawn while it's in play; the robot follows the drawn path and moves smoothly.
- §3: "looks good, let's try it".
- §4: "better - lets try". The briefing card and the game start get a full redesign (brief 062).

---

## 5 · Kit job (Glyph, 2026-10-24): the lovers icon and the patrol cameo disc

Boards: `briefs/057/board-13-lovers.png`, `briefs/057/board-14-patrol-disc.png` (sources `briefs/057/lovers/`, `briefs/057/patrol/`).

**Lovers icon, pick B, named `lovers`.** Three candidates, drawn solid white on transparent, 64 px @2x, bbox 3..59 like the kit's singles (`draw.py`, supersampled 8x): A two hearts · B a couple (two busts) with a heart over them · C the couple cut into a heart. B says "two people" before "love", so it stays apart from `kin` (adult and child) and from `s_hurt` (the half heart for health); A is the fallback if B turns muddy at 10 px in the Intel tab (checked 10/12/14/16 px tinted cream, the 19 x 15 secret tab solid and dashed, and the Done chip line: B holds). Installed: `Kit/icons/singles/lovers@2x.png` (+ `.meta`, fresh GUID), `.i-lovers` in `kit.uss`.
- **Intel mapping is code, not data**, so nothing to wire: the affair icon is chosen in two places, `HostDone.cs:268` (`p.Secret == "affair" ? "kin" : "secret"`) and `HudToolkit.Intel.cs:204` (same expression). Latch: replace `"kin"` with `"lovers"` in both. `kin` stays for the family need.
- Like the later singles (restrain, mood_*), `lovers` has no `sprites.json` atlas rect; it is loaded by file name.

**Patrol cameo disc.** The `npc_*` badges share one 48 px bezel and cream enamel field, so the disc is the kit's own pixels: `disc.py template` rebuilds the empty disc from the five (brightest pixel of the five, the centre rebuilt from a degree-4 fit of the field's vertical gradient). Slot: centre (24,24), field r 17, glyph safe r 14.5, glyph box 29 px (9.5..38.5), ink #17302f. Two outputs from one face:
- `badges/npc_patrol@2x.png` (the whole badge with its bezel): where an npc_* disc shows as it is (known-people discs, action panel header). `.s-npc_patrol` is in `kit.uss`.
- `badges/npc_patrol_face@2x.png` (field only, full-bleed, no bezel): for `Cameo()`, which draws the chrome ring itself (2 px #cbc6c3 over 1 px ink-deep); using the full badge there would double the ring. This is the one the Reroute pill, wait-spot cameo and edge chip use.
- Both are installed with a **stand-in face** (an ink screen with a cream dot-matrix smile and a mast lamp, the in-game patrol's face) so the code can wire now. Forma's art drops in with `python3 -I briefs/057/patrol/disc.py build FACE.png` (a glyph: ink on cream, fitted to the 29 px box) or `build FACE.png --fill` (a portrait that fills the field like the cell's cameos), then `install`. If Forma's face is cream on cream, put it on the field as a dark screen (the kit has no second field colour; adding one needs Aster).
- Latch: `HudModel.Badge` patrol -> `badges/npc_patrol`; `HudToolkit.Reroute.cs` `RrCameo` and the edge chip `badges/signal_unit` -> `badges/npc_patrol_face`. This also takes amber off the pill (the colour flag in §2).
- Limits: the stand-in is mine, not Forma's; the `.meta` files are hand-written from a sibling's (Unity will rewrite them on import); no Unity compile was run (no C# or scene touched).
