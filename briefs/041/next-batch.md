# Next Unity batch (Latch), queued by Aster

**Current: "Batch 0.2.118" at the end of this file (2026-10-25).** Everything above it is the log of earlier batches, kept for reference.

1. Engine 0.22.0 dll + rulebook + text.json together (0.21: fire order ends out of range / on a new act; 0.22: only security notices a run, crowd guards raise Alert). Notes: briefs/027/keel/host-interface.md "Engine 0.21.0" and "0.22".
2. 0.21 host: keep the cease before an act's walk; on `fire.out-of-range` drop that agent's lock and show the pop ("Too far", text.json less.pop.fire-out-of-range).
3. Fire keeps going on walk and run (Sky 2026-10-22); a new act ends it. Playground matches (Glyph).
4. Run price must be true: the run chip's sight preview doesn't count crowd guards, so it can show Alert 0 when a guard will see the run. Count them (every order shows its full price before commit); Glyph/Latch decide whether to mark the guard on the route.
5. Engine 0.23.0 (Keel, in progress): acts on a moving person are taken, not refused; the agent walks to them and the act starts in reach (Sky 2026-10-22: "shouldn't block actions whilst Inez is moving about").
6. Sky's 0.2.48 play notes (to come).
7. Sky 0.2.48, third person: "I couldn't fire on the robot" (the Scout on the road at the kerb before the Records Office, the patrol ~10 m ahead; pill "Hold · Fire at the patrol · +2/hit Trail · Alert ≥2"). Keel 2026-10-22: not the engine. Scenario FL-KERB (sim/Suborned.Sim/ScenariosFireLine.cs) puts the driven Scout there: the line is clear (no kerb, bin, lamp or cabinet is a sight occluder), the going-loud card refuses nothing, the order TpFireLock sends is taken and she fires in 2 s. The host offered an act its holds don't give:
   - `HostDrive.DriveFocusFrame` sets `HoverIntent = IntentFor(best)` (the old quick-click `ContextIntent`; with only violent ways it returns Fire as a Ring act). The map's `PointerHover` uses `HoldIntent` when `HoldOn`. So the third-person pill said "Hold · Fire", and the left hold (`TpHold` → `MainOf` → `MainWay`, which skips fire) found no way and did nothing.
   - The right hold fires only at what is under the cursor, else a hostile or combatant ahead (`TpHostileAhead`). It never fires at the focus the pill names, so a calm patrol under the pill but not the cursor gets nothing.
   - Fix: in `DriveFocusFrame` use `HoldOn ? HoldIntent(best) : IntentFor(best)`, as `PointerHover` does. In `TpHold`'s right branch fall back to `DriveFocus` (`t == null || t.Kind == "op"` → `DriveFocus`) before `TpHostileAhead`, as the left branch does. The right tap's ring could do the same. Glyph/Fable: `HoldIntent` offers fire only on a hostile or combatant, so after this fix a calm patrol gets no fire pill even though the right hold fires on it. Decide whether the pill should offer "Right hold · Fire" on the patrol and other security (not a passer-by, contact or guard at rest).
   - Check, not seen by Sky: a right hold on a target whose card has `Fails` (Vale, the driver while Vale is out, the getaway car) reaches `Fight` with `holdCommits`, so the card is not skipped and the gate opens. But it opens with source "right" after the button is already up, so it backs off on the next `GateFrame` and that shot can never be committed. Open such a gate with `Source = "card"` (commit by holding on the card) or keep it open until the next press.
   - Done-check: in third person, focus the calm patrol with the cursor off it. Then the pill and the hold agree, the right hold fires, and the fire log shows `tp-fire`.
8. Aster's call on item 7's design question: the pill offers "Right hold · Fire at …" with its price on security too (guards, patrol robots, police, cameras), not only on hostiles and combatants: Sky tried to shoot a calm patrol, and the price (Trail, Alert) is the point. Passers-by still get no fire offer on hover (budget fix, 0.2.41); a right hold still fires on them.
9. Engine 0.23.0 (people acts walk in, Keel done): remove the host's own serving lock and the "back in n s" price swap; issue person acts at the click and draw the agent's walk-in; card time = WalkInS + the act's time (host-interface.md 0.23.0). Fable retires the dead "serving a table" line.

GDD v0.25 notes (Aster, for Sky's review): §05 running (only security notices; rivals and "people within 13 m" out); §06 final-fight target order (per agent, trial); S22 / §13 "no action at a distance" now means "acts start only in reach; the agent walks in".
10. Walk-in visibility (Waymark, 0.23 check): a new order given while an agent walks in cancels the act silently. Show it: the plate reads "to Inez · Listen" while walking in; on `order.walk-in-ends` from a new order, a one-line pop (Fable: e.g. "Listen cancelled"); the card time is walk + act (item 9). Queueing a next order behind an act is Sky's call (asked 2026-10-22).
11. Sky 2026-10-22: "yes, that was annoying, cancelling a task I had ordered". A new order on a busy agent must never cancel its act unseen: the cancel shows on the plate and as a pop (item 10), and the pill over the new target says it will cancel ("Cancels Listen") before you let go.
12. Sky 2026-10-22: "it's not clear when a task has completed and what it gained." Glyph is designing the done moment (playground first, brief 043); port the pick.
13. **Squad fire: some selected agents don't fire** (Sky playing 0.2.65: "I had the whole squad selected but some of my agents weren't firing at the target e.g. the Scout"). Keel 2026-10-22. There are four causes, and two are the host's. The evidence is the engine scenarios QF-* (`sim/Suborned.Sim/ScenariosSquadFire.cs`) plus three of Sky's saved sessions from 2026-10-22 (`…/com.Unity-Technologies.com.unity.template.urp-blank/play/handover-20261022-{201505,202219,202331}.script.json`). They replay identically on 0.25.0, and they were probed agent by agent with `FireQuery`.
   - **(a) Host: holstered agents get no order.** `HostHold.Shooters` gives "every selected armed agent who is drawn, else all of them" (the playground's `fire.js shooters` rule, ported as written). If any selected agent is drawn, a holstered one gets nothing. Weapons free never draws, so it never fires. QF-HOST: four selected, Enforcer and Fixer drawn, Scout and Tech holstered. Only the drawn two get the order, and the Scout and Tech fire 0 shots in 8 s. QF-ALL is the same set-up with the order given to all four: all four fire (drawn at 1.1 s, the holstered Scout at 2.0 s, the Tech from 21 m walks in and fires at 4.8 s). In session 201505 at 6:40.9, the order on police-5 went to Tech and Enforcer (both drawn) while the Scout was alive and holstered. This is consistent with (a), but the selection isn't recorded.
   - **(b) Engine: a focus never walks in.** In a fight the right click is a focus, a lock that never walks in (tuning.md §11/§14). A 16 m sidearm out of reach holds, fires nothing and lapses after 3 s, while the Enforcer's 30 m carbine fires on. Session 202219 at 1:43.3: a focus on police-3 at 25–27 m. Fixer and Tech fire 0 shots and log `fire.lapses` at 1:46.4; the Enforcer fires 3. The same happens at 1:15.8 and at 1:36.3 (Tech). QF-FOCUS reproduces it with the Scout 20 m off.
   - **(c) Engine 0.21.0: a lasting order ends once its target passes the max ring.** That gives `fire.out-of-range` and "Too far". Session 201505 at 6:40.9: the Tech's order at 15.7 m ended 0.2 s later, because police-5 stepped past 16 m. Session 202219 at 1:31.5: Fixer and Tech. Three of the four guns are 16 m sidearms, so "the Enforcer keeps firing, the others stop" is the usual picture.
   - **(d) Down agents.** In session 202219 the Scout was down from 0:48.2 (the guard). Every later order correctly went to the three left standing. The plate must make "down" unmistakable.
   - **Rule (Aster's default; the code shows no reason against it):** a squad fire order is an explicit player order, so every selected agent who can fire does. A holstered agent draws: that is the player's choice, and the going-loud card still applies. One out of range walks in. One with no line walks to one. One who can't fire shows why on its plate in two words. Can't means unarmed, carrying, walking Vale, busy with an act you didn't cancel, in the car or inside. Out of range or no line counts only when there is no walk in.
   - **Engine (Keel, done, 0.26.0):** `Game.FireQuery(op, target, force, focus, out walk)` gives `Issue`'s answer for a fire order before it is given (briefs/027/keel/host-interface.md "Engine 0.26.0"). Read-only, and no hash moves.
   - **Host (Latch):**
     1. `Shooters` (hold and focus): ask `FireQuery` for every selected agent and give the order to each one it takes. Drop the drawn-first filter and the `Armed` pre-filter, because `FireQuery` says `no-weapon`. Keep the portrait-pin exclusion for a focus and the driven agent in third person.
     2. Issue each one with `quietRefusal: true`. The plate shows each refused agent's reason (Fable's two words: e.g. `no-weapon` "no gun", `fire-carrying` "carrying", `fire-carrying-vale` "has Vale", `agent-busy` "busy", `in-car` "in car", `enforcer-inside` "inside", `out-of-range` "too far", `no-clear-line` "no line"). Pop once at the click only when nobody takes it.
     3. The pill's faces (`FireShooters`) are the agents `FireQuery` takes.
     4. While an agent walks in for a fire order (`Op.CloseWalk`), its plate reads "→ officer", not "too far" (`FireOne` returns "Too far" during the walk in).
     5. Take the 0.26.0 dll. The rulebook and text are unchanged.
   - **Open for Motive (today preset, so Sky's OK is needed):** (b) Should a focus out of reach walk in as a fire order does? I recommend yes, so a click never silently does nothing. The host can do it now by sending a plain `fire` to agents `FireQuery(focus: false)` says would walk, with no engine change. (c) Should an order whose target steps just past the ring while the agent stands still walk in again instead of ending? Sky's "until out of range" was about the agent walking or running away.
   - **Done-check (Latch):** on the range, select all four with Enforcer and Fixer drawn, Scout and Tech holstered, and the Tech about 21 m off. A right hold on an officer must log `hold-fire 0,1,2,3`, and all four must fire. With Arms 1, the Fixer and Tech plates must read "no gun" and the Scout must draw. With the Scout watching Inez, its plate must read "busy" and the rest must fire.
14. Draw distance (brief 048, Redline's ranked plan; Aster's call on far clip): third-person far clip 350 m with Glint's haze retuned to it (450 m only once LODs and shadows are done); third-person shadow fit 100–120 m, 2 cascades, first split ~20–25 m, last ~30 % faded (Glint's sign-off: 80 m would show a hard line; use 120 m if far-tower shadows are lost; Redline counts both); followFar 350 in HostFollow.cs:29 and the serialized followFar in HarbourDistrict.unity and FightRange.unity, equal to DistanceHaze.FarClip (haze.json farClip 350, reach 0.97); small kit pieces get a cull level instead of "never culled" (measured 6,081 → 3,882 renderers, 14 % faster); fix the GPU Resident Drawer's per-frame rebuild (045 F6; keep the drawer on), try static batching off; the shot runner hook for Splice's `tp-long` row (camera heading + draw counters). Palm LODs wait for Sky (compression rule).
15. `RobotIdle.Yaw` throws an exception every frame (1,029,629 times on 2026-10-20; unity/Logs/Editor.log reached 919 MB): fix the bug (brief 050, Splice).
16. Roster (Sky's picks, brief 046): icons under each portrait (recipe tallColumn/2), steel divider without screws, column sprite `roster_column_bare` (B: 4 outer rivets, no stripes; testbed/hud-style/export/sprites/), one weapon icon (light disc drawn / dark 55 % holstered; "drawn" cause rides on it), status slot per the lab's slot spec, no number tabs, no key numbers; font Jost Regular with tabular digits (lab fonts/ export). Use the tokens export but override the column to `bare` (the export defaults to `rivetless`). Splice's alignment check rules come with it.
17. Target rings and visual cues (brief 049 "Round 2", Sky: "rings should be red when they're firing at you, and with crosshairs also when involved at being aimed or shooting"): replace the 15 m range circle and the follow ring outside the range with per-target ground rings (side colour; cream crosshair ticks when aimed at by the cell; red ring with closing ticks while aiming at the cell; red + kick + pulse while firing at the cell), thin quiet tracer, the under-fire arc (default weight 1, Sky to judge 1 vs 1.6), RangePanel Numbers off by default; label audit removals (all key legends). Rings are ground decals behind legs. Glyph 2x-checks in the build.

## Queued for the next build round (Aster, 2026-10-23)
- SignalUnlit: add `Offset -2, -2` (at 150 m the share of the cone in view goes 0.64 → 0.75; kerbs and paving win on depth precision; the 250 m case is real occlusion, so no further fix). Zero cost. Roll it into the next game change rather than a build of its own.
- Signal fill ΔE 2.2 (haze on/off) accepted: the fill is ~38% alpha, so the hazed ground shows through it. The rim is ~0.4 median. No opaque fill.
- Needs text, for Keel at the next engine export (Motive, 2026-10-23): rewrite the old-story src/reason in `contracts/people/needs.rules.json` (~34–36) and the "Clearance order" record in `harbour-district.needs.json` (~29). Keep the id `clearance-order` and the label. Then `bun run people` → `bun run sim:site` → `bun run sim:unity` → record a new mission check. People come out identical; only hashes change.
18. Bays drawn sunk (brief 055, Datum): the square and office bays are carriageway-level lay-bys in the block (points at −0.18) but Layers.Ground only cuts the plot slab around bays a drive reaches, so they draw at 0.18. Cut the slab (and the floor model) around every bay. Aster's call: follow the block (sunk), not flush.
19. Engine 0.28/0.29 port round (after the current 0.2.97 fix round): host reads per briefs/027/keel/host-interface.md 0.28.0 and 0.29.0 — civilian cones on hover only where they matter; the new Watch offer (refused nothing-to-see → no Watch shown) and mood "likely" on the price card; sight shadows on the fan (Fan.Shadows); the near-body ring at a civilian's feet only when the order being set up would leave a body (Glyph draws it, brief 054 §9.1).
20. From Glyph's review of 0.2.99: feather the building fade's two vertical edges over ~100 px at 2x (the café wall in hold-act-ready); the camera callout shots wait 0.3 s (opaque disc check); roster selected ring vs --turquoise-light (looks ~#22c8d8). Roll into the 0.28/0.29 port round with Vela's road/roof and Glint's lighting changes.
21. Road strip light (brief 048, Glint): verify RoadLight.cs + roadlight.json (lens 1.4 / 3.0 / 5.0 and pool 0 / 0.14 / 0.22 by phase; runtime material copies only) in game; add a `-evening` arg beside `-night` in HostNight.cs calling LightPhase.SetEvening(); shoot the Boulevard and the Office front day / evening / night; check the rails (#6f7a72) against the lawn track at play zoom (Glint suggests track.value 0.82 if they merge — Vela's call). New files need their .meta on import.

22. Rounded pavement corners (brief 028, Sky 2026-10-24: "curvy and smooth, not harsh lines"): Datum's `streetscape.kerbReturn` radii and Splice's rounded plot slabs, kerb and road-finish sweep. Rebuild the world, shoot a junction near the civic block and the Boulevard at play zoom and third person; check the walk heights and collision round the arcs.

23. Repeat Watch in the history (Sky 2026-10-24, 0.2.99 screenshot: "Watch · Inez · café door — Learned: where Inez goes, and when." at 0:11 and again at 0:50). Before engine 0.29 a second Watch on Inez was still accepted (it could catch the lovers' secret), and the card's footer is fixed text from text.json, so it claimed the same learning twice. With item 19 the 0.29 Watch is refused once seen (row absent, brief 054 §1) and the footer becomes "{who} · {mood}" (brief 054 line 332). Check in the build: a second Watch on Inez can't be ordered, and no scene card's footer says "Learned" for something the cell already knew (if a scene plays with nothing new, the footer is dropped).

24. Patrol robot's face upside down (Sky 2026-10-24, 0.2.99): the dot-matrix expression reads inverted on the patrol robot. Check RobotFace.cs (glyph row order, the face screen's UV or the screen transform on the patrol model) against the other robots, fix at the source (runtime or Vela's piece; tell Vela if it's the model), shoot the patrol face close in third person next to a service robot.
25. Patrol walks through the entrance camera's pole (Sky 2026-10-24): patrol-b (52, -44.8) sits 1 m past the pole (53, -44.5) on the same line, so every sweep passes through it. Pulse re-sites the sweep's north end (mission.json + sim data) and adds a check that no patrol or walker leg passes through a solid; Latch: in game, the patrol turns short of the pole and walks round nothing solid.

26. Inez on an invisible ledge (Sky 2026-10-24, 0.2.99: "walking on an invisible ledge sometimes"). Splice is tracing her round's legs through the walk file against the drawn floor and fixing it at the source. Latch: once that lands, shoot her full round in third person from the side and check her feet stay on what's drawn the whole way.

27. Stray kerbs on the road (Sky 2026-10-24, 0.2.99 third person near the civic block: "some odd curb / road layout going on here"): a ring of kerb stones lying on the carriageway with a step partway along a run, and a dark slot along the pavement edge. Splice is locating and fixing it in the generator (likely the kerbs round sunk bays). Latch: reshoot the same spot in third person after the rebuild.

28. Reroute says what it moves (Sky 2026-10-24, 0.2.99: "not clear what we're rerouting here", pill "Reroute 80 s +1" at the cabinet). Glyph is adding it to brief 057: the patrol's cameo on the pill, and on wedge hover the current sweep and the new route and wait spot drawn in the world. Port after Sky's pick.

29. A down agent is offered on the ring (Sky 2026-10-24, 0.2.99: Listen/Pay/Ask on the driver with the downed Fixer's cameo; "shouldn't even be an option"). A down, carried or taken agent is never a giver (filter where Givers is built: ring, hold, verbs 1–4, panel, Intel next step, portrait drop, WatchBy); an able agent's cameo if one can; else the way is absent (hud29 notOffered). Shot: the ring with the selected agent down.

30. Watched cones too strong, and the camera's sweep line (Sky 2026-10-24, 0.2.99: "once watched it's cone now permanently appears but we need to make it softer, kinda dominates and for camera there's some sweeping line, not sure what that's for"). Glyph designs it in brief 057: cones quiet by default, at full strength only when it matters; the sweep dropped or given a real meaning (the desk's 20 s scan). Port after Sky's pick.

31. Unintended carry (Sky 2026-10-24, 0.2.99: "I've asked tec to move but he's dragging the downed scout"). A move click near a downed ally became a carry (HostFight.CarryDowned on a right click), and every later move kept carrying at half pace. A move never becomes a carry; Carry and Put down are explicit ways on the ring and hold; while carrying, the plate reads "Carrying Scout" and Put down is one obvious way; a move while carrying keeps carrying and shows the half-pace time. Shot: a move beside the body (no carry), then Carry and Put down.

32. The seal took Sky by surprise (2026-10-24, 0.2.99, failed at 7:56: "District sealed. Sealed off before Vale reached the car." while the card says Vale reached the car; Sky: "I failed for shots heard but that was a long time ago"). The engine's fail is "the car hasn't left" (Go needed, refused while police hold the car), the seal runs 145 s from the unit's confirmation, and its clock is only in a feed card. Glyph specs in brief 057: the seal countdown in the clock bar (window-marker style), Go as a clear way with holders ringed when refused, and a true fail line. Latch ports after Sky's pick.

33. Controls trial (Sky 2026-10-24: "left click brings up the action ring. Right click then doesn't do anything ... makes RMB more relevant for firing only"): left click on a target opens the ring (no quick-click take); left click on ground walks; left hold acts as now; right tap does nothing (target or ground); right hold fires (0.2 s kept for now); third person consistent. Proofs updated; playground follows later.

Aster's calls on Latch's code round (2026-10-24): Watch after a Watch stays **absent** (Sky 2026-10-23: "If a player being watched has no impact then we just drop that watched action"; supersedes the 10-18 greyed row; Glyph's hud29 spec to follow). Put down = knob 1 + cancel key is fine. Fable is rewording the "right click" hints (H3, H5, H11, H17, H18), the Watch footer and mood words, and the sealed fail lines as text.json keys; Latch wires the keys before the batch.

34. Brief 057 §2–§4 approved by Sky (2026-10-24): Reroute with thin route lines, the new route drawn while in play, the robot on the sim's path and moving smoothly; quiet cones, no sweep; seal clock, Go state, true fail lines (Keel: FailCause.Car, Debrief fix; Fable's keys in text.json). §1 held for Glyph's ring version.

Item 25 update (Pulse, 2026-10-24): patrol-a (68, −45.9), patrol-b (54.2, −43.3): diagonal 14.2 m sweep, min gap 0.75 m (pole 1.19 m); the old line went through the pole and grazed the east service cabinet. sim/data/handover.site.json regenerated. Pending: the one leg rule in Splice's geometry audit (legs.ts is its input); tools/sim/site.ts:211 patrol.src text (Keel); the full mission check and c035 re-record once the world settles (Sky was playing; no --force).

Latch's 057 §2–§4 code is in (2026-10-24). Waiting on Keel: FailCause.Car + holders, the seal's origin read, and the engine walking the reroute (PatrolPose jumps today) with Game.PatrolRoute(). Aster: the coloured signal_* badges stand as interim cameos until the cream patrol cameo exists. Parked for Vela: the patrol's treads are in its body mesh, so they can't move with speed (split them or scroll the texture).

35. Camera fire fixes (brief 058, Motive): the camera/car fire pill shows +2 Trail once (not "/hit"), plus a Trail line on the action panel; Fire on a camera offered to every armed agent (only the Enforcer had it from the ring/panel); the Loop plate reads "Camera looped", not "Camera dark". The walk-off stopgap waits for Sky.
Item 35 done in code (Latch). Fable: add `less.done.gain.camera-looped` ("Camera looped") to text.json; the code uses a placeholder until then.

Next round (from Splice's G-3b, 2026-10-24): 7 café parasol "stems" block sight at 1.6 m where no stem is drawn (Parasol_Testbed has canopy only at 2.5–3 m): Keel to drop occluders under 0.6 m wide (as the cover rule does) or Vela to model the stem; the civic robot's occluder is wider than its body (Keel/Forma); plot-04 has a 6.25 m outline gap at block (-45.4, -34) (Vantage/Vela). Waitress door-point legs: the café door point sits inside the front solid in the raster (Vantage door move or Vela/Keel volume gap).
c035 recorded (Keel, 2026-10-24; ships 38/38, replay OK). Next round with the sight fixes: `bun run cover` (Pulse) then c036; fix `missions:rerecord` silently dropping the note when called with no argument (Splice: require the note); glazed civic bays stop sight (N48's witness behind the café glass): should glass block sight? (Motive, Vela; Sky's call).
Sky 2026-10-24: "Can take some liberties for performance e.g looking through glass isn't needed." Glass blocks sight like any solid; no see-through glazing rule. N48's new count stands. Drop the glass question.
Site regenerated again (Splice, kerb strips out of junction boxes): c035's inputs moved; c036 after the 0.2.105 batch, with bun run cover (Pulse).

0.2.112 shipped to Sky (Glyph's 2x check: ship, 2026-10-24). Nits for the next round: junction NE sand triangle and brass kinks on the arc (Splice); no-walk cursor disc lacks its chrome ring; held-Go lock touches the car pip and badge (Latch); seal and known-people plates slightly translucent (make opaque if Sky finds them busy); cream npc_patrol face (Forma); lover icon (Aster: commission from Glyph); evening sun and grade (Glint).

0.2.113: Glyph "ship" (2026-10-24). Next batch (optional fixes): two stray cream arcs in the road at the circus mouth (Splice); roster pip dots float when portraits hide in replay (Latch); night: no HUD night shots, walls crush to #020100, consider a deep teal floor (Glint, Latch); faceted brass/kerb on junction-NE arc in third person (Splice); faint road-sheet seams (Splice), sky-rail guideway taupe #777262 (Vela). Plus Latch's held code: Fire on a busy agent, bot harness fixes, the Levels check against the eased position.

0.2.115: Glyph "ship the build; night not ready to offer" (2026-10-24). Night round 2 (Glint, Latch): lift ground/furniture, people rim, night-grade the café cutaway, dim the kerb hairline, warm the violet lamp pool. Road seams (Splice + Latch): depth bias or render order instead of the 3 mm sink, SSAO test; sheet tone (Vela). Bot findings for Suture: Carry by an agent that went down before the click; Q-SOLID crowd near the café at start and officers/rivals on paths; Q-MEM 12 MB/min over 3 min.
Next build: Keel's LearnQuery fix (NQ-2): host asks LearnQuery for the actual giver (Listen = Scout). Rebuild and rerun the bot row.

0.2.117: Glyph SHIP (2026-10-25). Next round (small): the 'replay 0.6x' tag is a flat web pill (fail-getaway-lost.png): Latch restyles on plate_dark; faint 1 px hairlines at junctions (Splice, optional); guideway reads flat paper-grey, maybe a touch of chrome (Vela, optional).

## Batch 0.2.118 (Aster, 2026-10-25): one batch, code first, test once

From Sky's calls of 2026-10-25 (briefs 054, 056, 058, 059, 061, 062, 063) and GDD v0.25.0. Latch codes through this list now with fast checks only (runtime-compile, unit tests). The full batch (`bun run batch`, game-check, shots, Glyph's 2x review, the bot row) runs **once**, when items A to H are in. No build before then. Not while Sky is playing (`bun run batch` waits).

**A. Engine release (Keel, in progress): 054 final rows, 058 Loop v2 and Disable, the needs text.** The batch's sim step copies the dll. Host port from briefs/027/keel/host-interface.md (the new version):
- the ring on a camera: Tech gets Loop · Disable · Fire; any other armed agent gets Fire;
- plates "Camera looped" with the 2:00 countdown, "Camera cut", "Camera shot out";
- the one-loop refusal, with the time left;
- the tether UI and its refusals removed.
Words: Fable's keys. Look: Glyph's 058 spec, in the brief. Waymark retimes routes A, A-board, G, G-kill and the loud family, adds `route-a-disable`, `careless-g-disable` and `careless-loop-runs-out`, and re-records the check (c037) before the batch's game-check.
**B. 058 walk-off stopgap (Latch, coding now).** Host only:
- a squad move leaves a looping Tech in place, with the plate "Holding the loop";
- a move that would take him off the cabinet needs the 0.5 s hold ("Camera comes back").
If A's Loop v2 lands in this batch, a walk-off no longer ends a loop. The stopgap then has nothing to guard: drop it, or keep it inert. Say which in port-progress.
**C. 056 occlusion F (Latch, coding now).** The screen-space capsule cut and the silhouette pass. Port notes are in briefs/056-occlusion.md. Silhouettes show for the cell, hostiles and key people. The rim stays until Sky sees it. Glint tunes the rim's look. Glyph reviews the 2x crops.
Shots: four agents close together in front of the café and the Office at yaw 30, 120, 210 and 300. Sky's E fallback is tried in play only if holes cover over a third of a building's face.
**D. 062 briefing B, "on the street" (Latch, coding now).** Briefing.cs replaces the card and the lore screen. Begin glides the camera to the cell (3.0 s, skippable), the HUD enters in §5's order, and the clock starts when the roster settles. New game goes straight to the briefing.
Inputs:
- Fable: the hook and objective words.
- Waymark: `brief.ways[]` and `brief.clock[]` in the mission data (queued with his 058 round).
- Forma and Glyph: Vale C's portrait (being redone, item E).
- Glyph: the file-card duotone.
The briefing can't ship with the badge fallback: it waits for the portrait.
**E. 063 portraits and figures.**
- The driver's three albedo JPEGs and the manifest change are already in the tree: refresh and look at the driver once in play (face, hands).
- **Vale is C, not B** (Sky's revised call, end of brief 063). **C has landed** (Forma, 2026-10-25; brief 063 "Vale C model (2026-10-25)"; LODs 7988/2992/1500; the pass is at $1.35 of $3). Copy `out/people-models/People/Figures/councillor/` and `manifest.json` into `unity/Assets/Suborned/People/` (same names and GUIDs), then look at him once in play at about 60 px at the four yaws and in the Enforcer's carry. Never import B (it is kept in briefs/063/vale_B_figures_entry.txt and out/scratch/forma/063/).
- Faces: the model's cameo goes on the carried figure; the painted crop goes on the briefing card, the Intel face and the feed. That split was decided for B, whose model face didn't hold at card size. Glyph redoes all four councillor portraits from C and checks whether C's model face holds at card size; if it does, the split goes.
- Glint: the matte-map check on Vale, whose chain plates are flat, albedo only.
- Patrol face A: Glyph installs it under the same file names.
**F. 061 crossings.**
- Delete the turn in CrossingKit.Dress (Latch, coding now).
- Then `SignalHead` (Latch): it reads the mission clock and writes the state, numerals, ring fill and dot ranges per head with a MaterialPropertyBlock, using the export's `crossings[].cycle`. A look only: the sim reads nothing.
- Inputs: Datum's plan keys (in progress), Vela's pieces (in progress), then Splice's rules and export (26 crossings, continuous footways at lane mouths, the edge brass gone). Glint sets the emission per state.
- Shots: the Records Office crossing in all four states at yaw 30, 120, 210 and 300 and at eye level, plus a lane-mouth footway.
**G. 055 step 1 close-out.** PlayInstall.WalkFloor is in.
- Prove it in the build. The log says the ground comes from the walk file. The door guard stands on the Office landing, matching briefs/055/shots/office-entrance.jpg. Steps ease.
- Then Splice flips `tools/geometry` to the walk file and re-ratchets G-1a/b/c toward 0. The rest of 055's order is in briefs/055-levels-collision.md, "Aster's fix order".
**H. Small.**
- 057 §5: the lovers icon for the affair, and the patrol badges (npc_patrol and npc_patrol_face).
- 0.2.117: the "replay 0.6x" tag on plate_dark.
**I. 059 (route B through the service drive, the rival car arriving): in only if it lands before A to H are done.**
- Chain: Pulse's routine, Motive's prices and Vantage's points (all in progress), then Splice's site export, Keel's K1–K11, Waymark's re-compose, Glyph's spec (in progress), then Latch's port. The port covers the rival car and its five figures, the official car's drive, Drive on the getaway car, and the markers.
- Until then, mission.test.ts fails on two asserts by design: §07's new schedule against mission.json's timeline, and route A's figures. Waymark lands the mission.json strings with Keel's 059 engine.
- If 059 misses this batch, it opens the next one. The batch doesn't wait.

**Batch done-check:**
- `bun run batch` OK: sim, unity-match, game-check, shots, budget and audit, late-fight, tp-long.
- Shots: C, D (frames 1 to 5 of the start), E, F and G.
- Glyph says "ship" on the 2x crops.
- The bot row (`-bot`, human-a and human-b) shows no new Q-* class.
- The perf064 row on the Studio.
- One build to Sky.

**Needs Sky, not in the batch:** the Air perf run for brief 064 findings 1 and 2 (perf064 at play zoom, warm, on the 15-inch MacBook Air M4).

**Fable's words are in (2026-10-25):** text.json, text.schema.ts, text.test.ts and brief.json; text.test passes 137/137.
- 058's keys are written live against engine 0.31.0. `less.pop.camera-dark` now reads "Camera shot out". A camera shot is priced "+2 · ▲≥2" with no /hit. The loop lock shows its end tick.
- 059's codes that don't exist yet sit in text.json's `pending` block, some marked guess:true. Keel confirms or renames them in his 059 round. Drive needs its own engine id, because `drive` is already Take control.
- 062: `less.briefing` holds the job words (Eyes, People, Machines, Force), the labels and the ways (front-door, pickup). Glyph and Latch agree the `brief.ways[]` ids with these keys. Waymark makes briefing.timeline match mission.json in his round.
- Latch, with the 059 port (item I): drop `Game.PickupService` in HostTakedown.cs:424 and HostFeed.cs:315–316.
- The feed is at its 10-beat cap. If Motive retires the driver's Call, "Camera blind." frees a slot for car.parks.

**Vela's 061 pieces are built (2026-10-25):**
- New family `crossing_lens`: SignalLens (Rim/Figure/Tens/Units/Beam/Halo), HaloRing (fills by `_Cutoff`), atlases `signal_atlas_v1` and `signal_ring_v1`, the dot rows re-cut without brass, Kerb_Pearl without its crown, RoadRibbon without its hairlines.
- Look data: Data/signal-lens.json (a copy is at briefs/061/vela-signal-lens-data.json).
- Sheet: briefs/061/pick-A-pieces-built.png.
Item F for Latch, then:
- SignalHead, with per-head ring `_Cutoff`.
- A `_LitFrom/_LitTo` gate on u for the dot rows (stock URP Lit has none).
- `_BaseMap_ST` face tables.
- The turn deleted in CrossingKit.Dress (coding now).
- Glint fills evening and night emission (null in the data) and trims red and amber per state.
- Aster's call on the sizes Vela flagged (LightDotRow 1.8 × 0.30 and 0.8 × 0.30, SignalLens 0.95 × 0.94 × 3.085, no longer within partkit's 0.05 m): the plan's part sizes are re-fitted to the built pieces, not the reverse. The plan sizes are Datum's edit; Splice then adopts, tags, exports and retires the old pieces. The 1,562 dot-row parts and 228 heads stay procedural until then.

**Glyph's specs are in (2026-10-25):**
- 058 (item A): brief 058 "Glyph (2026-10-25)", boards briefs/058/board-01..05, new icon `icons/singles/disable@2x.png`.
- 059 (item I): brief 059 "Glyph (2026-10-25)", boards briefs/059/board-01..04.
- Patrol face A is installed under the same badge names.
- The cameo ground is roster grey #b3b0ad.
Aster's calls on Glyph's open points:
- Keep the shipped price lines; Motive's extra fields are not added.
- Drive's time prints as "28 s".
- The getaway Drive gets its own key and engine id (Keel names the id in his 059 round, Fable keys it).
- The cut-or-shot camera disc reads Keel's record once it exists.

**Glyph's portraits are installed (2026-10-25).** They are in `Kit/portraits/` (card_councillor, face_* and cameo_*); the reads are listed in brief 062 under "Portraits and ids". Inez A and the driver A stand. The four councillor files are Vale B's and are being redone from C under the same names, so the reads don't change. Latch, items D and E:
- `FaceOf(role)` → portraits/cameo_<role>_normal for the waitress, driver and councillor, at HudToolkit.Intel.cs:192, HudToolkit.cs:1262 and the ticker at :1138. The guard keeps badges/signal_guard.
- Briefing.cs: `Sp("portraits/card_councillor")` at 360×296, with a 3 px ink-deep rim and radius 13; the way contacts use `Cameo(portraits/face_<contact>)`.
- Aster's call: `cameo_councillor_model` at 18 px on the Enforcer's carrying plate (HudToolkit.Less.cs:265–275), which is where the model's face is read.
- CastBadge stays on the 11–14 px reads.
- Way ids front-door and pickup match text.json. Waymark's brief.ways[] and clock (0:00 / 5:30 / 8:30) are written into brief 062; Waymark moves them into the mission data in his round.

**Glint's lighting data is in (2026-10-25):** unity/Assets/Suborned/Resources/Atmosphere/signals.json (suborned.signallight/1). Sections are in briefs 056, 061 and 063; evidence in briefs/061/glint/. Latch, items C and F:
- ArchitectureFade: the Begin() shadow flag becomes `shadows && !capsules`, and LateUpdate builds its reach from the capsule cuts only (about 5 m, not 11).
- The rim scales cream by the wall's lit luminance (rimLit 2.5).
- Silhouettes read their per-phase colours from signals.json; the edge goes from 0.035 to 0.045 m.
- SignalHead reads signals.json, with a way to hold a state for shots.
- Shots H1–H5, R1–R5 and V1–V3 (the list is in brief 056's Glint section). H1's measured values go back to Glint for a refit; R4 measures the silhouette's colour space; H4 checks the head's amber against the guard cone (ΔE 6.0–6.4, kept apart by shape and place).
- Vale C: his ivory is ΔE 2.4–2.6 from the Office stone. He separates by his black collar, disc, gloves and trousers. His gold trim reads orange (#C87F1B) in sun: note it in the play check, Forma's to fix only if Sky minds.
- Bloom isn't modelled: if the heads bloom, refit at 0.8.

**Vela's lens patch is in (2026-10-25):** crossing_lens.py writes Glint's trimmed day values and the evening and night levels; Data/signal-lens.json matches signals.json.
- For Splice's kit adoption: the changed hashes are listed in Vela's report (signal-lens.json, DK_Signal_Rim, Atlas and Ring .mat, DK_SignalLens and _L1, DK_HaloRing, decokit_materials.json). A bare `bun run decokit` doesn't build SignalLens or HaloRing until the plan orders them: Splice adds the order.
- Latch: SignalHead must supply `_EmissionColor` per state by MaterialPropertyBlock (linear colour × level). The .mat default is written gamma-style by unity_mats.py, so nothing may read it.

**Engine 0.31.0 is in (Keel, 2026-10-25).** 303/303 scenarios pass and replay exactly; host notes are in host-interface.md "Engine 0.31.0".
- Item B is **dropped**: a move no longer ends a loop, so the walk-off stopgap has nothing to guard. Latch removes HostLoopKeep in round 2.
- Item A's port is Latch's round 2.
- Waymark retimes the 13 moved scripts and re-records (c037) before the batch's game-check.
- Motive rules whether Disable at the entrance camera may send only the patrol (the dock robots are out of the 60 m reach).

**Fable's 0.31 words check (2026-10-25):** every new code is covered.
- loop.ends reads "The tape has stopped." and fires on a run-out or a Disable cut; a re-splice logs loop.on.
- New close word: less.done.close disable, "Desk reports".
- The eye mark counts RunRememberedBy.
- The tether lines stay only for pinned runs; the stopgap keys are retired.
Latch:
- Remove HostLoopKeep and its placeholder keys.
- Never read activity.holding.loop or cancelCosts.ends on the live rulebook.
- Show the "Desk reports" pop when the Disable marker closes.

**Disable that sends nobody (Motive's ruling (a), Fable's words, 2026-10-25):** one robot at the entrance camera is the reach rule working; neither route moves. Latch, item A:
- HostTakedown.cs CardPick bug: `t.Contains(" 0 unit") ? "robots" : "both"` matches "0 robots, 0 units", so a dispatch of nobody shows "Robots coming". Test the none case first (0 robots and 0 units).
- A camera.cut-reported case reads the same-tick alarm.dispatches line: "none" picks "No one is near enough to look." and queries the Scout line with the outcome nobody-sent.
- The counts are cumulative, so a later report with a robot already out reads as sent.
- Regenerate StreamingAssets/sim/text.json (git-ignored) in the batch.

**Latch's round 2 is coded (2026-10-25):** A (the 0.31 port, stopgap deleted), C (Glint's data through Runtime/SignalLight.cs), E (portrait reads and the 18 px carry cameo) and F (SignalHead.cs, Shaders/SignalDots.shader; `-signalstate` and `-signalleft` hold a state). Proof118.cs is gated as r118 in tools/game/cli.ts. Before the batch can run:
- Splice's adoption and export (running). Answers owed to Latch: the part names DK_SignalLens_Rim/_Figure/_Tens/_Units/_Beam/_Halo; whether HaloRing is exported on its own (then the builder pairs it with its head) or only under its head; the re-cut dots adopted, so Latch can turn on `CrossingKit.LiveDots`. Until SignalLens is adopted, the world build stops on the 41 procedural heads, by design.
- Waymark's c037 on 0.31.0.
- The batch's sim step copies 0.31.0 (the one tools/game failure clears with it).
- The Vale C copy (item E).
Redline: count renderers once LiveDots is on (each signalled module is its own renderer) and read it in the perf064 row.

## After batch 0.2.118 is built and shipped (not mid-batch)
**Package upgrade (Latch; Sky 2026-10-25: "Yep. Can wait").** In unity/Packages/manifest.json, upgrade `com.unity.pipeline` from 0.7.0-exp.1 to 0.9.1-exp.1, to match the Unity CLI (now 1.0.0-beta.14; Sky's other project already uses 0.9.1-exp.1).
Done-check:
- the Editor reimports with 0 console errors;
- the Unity MCP tools still answer (editor status, compile, console, screenshot);
- the upgrade is committed alone, as its own checkpoint.

**Latch's round 3 is coded (2026-10-25):**
- FootwayKit.cs draws the 14 footways.
- The SignalLens parts are verified. The HaloRing name mismatch is fixed (`DK_HaloRing`); a head now fails the build without its body, rim and ring.
- LiveDots is on (`-bakeddots` for A/B), and the round-2 seat-plate bug is fixed.
- Retired-piece references are removed.
- Dry check: 14/14 footways, 41/41 halos, 16/16 signalled crossings, 274 dot rows.
Order before the batch: Splice's revert (running) → Splice's lip and prune fix (below) → Waymark re-records c037 on the final site → the batch.
- Aster's call on the footway lip: **the drawn slope wins.** tools/geometry/visible.ts and the walk file follow what Unity draws over the 0.38 m lip strip, not the higher of table and lip. If that moves the sim site, it moves before Waymark's record.
- The kit tool prunes rejected pieces' model files (the seven retired FBX are still in Kit/Models). Latch's KitImport/KitPlacer prefab cleanup stays unless Splice wants it in the kit tool instead.
- Redline: 194 live dot renderers (up to 388 draws, previously batched) plus 41 rings. Compare the build log's "live dot rows" and "clock rings" against `-bakeddots` on the perf064 row.
- Glint: ContactShade no longer lays a blob under signal heads. Check ring and dot brightness in the shots.

**Splice's lip and prune are in (2026-10-25):**
- The footway lip follows the drawn slope in tools/geometry/visible.ts. No number moves (all 14 footways are outside the site window), the sim site is unchanged, and c037 stands.
- The kit tool prunes rejected pieces: 8 FBX are removed (the seven crossing pieces and SpaceHQ, rejected since 10-13; the world uses SpaceHQ_Portal) and the orphan DK_Cross_Stop/Walk.mat. Unity reimports once.
- Latch: Prefabs/Landmarks/DK_SpaceHQ.prefab is still on disk. KitPlacer only collects rejected pieces with source `generated`, so remove it too in the batch, and confirm that nothing references it.
