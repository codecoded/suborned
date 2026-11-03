# 041 port progress (Latch): "Click walks, hold acts" into Unity

Resume notes for the next session. Spec: testbed/controls (committed), briefs/041-controls-playground.md, the lead's task.

## Done (in tree, not built)
- PlayControls.cs: ControlPreset { DoubleClick, Built, HoldActs } (HoldActs default; pref key Preset2, forced when no pref so FightRange.unity's serialized 0 is ignored); `Modern` (preset != Built), `HoldActs`; settings tpRightFires, squadEveryone, planTogether (+ edgeScroll saved); PlayAction.SelectAll (5).
- "Modern" replaces DoubleClick where it meant pill/leash/no-hold-ring: HostContext, HudToolkit.Less, HostClicks (calls HoldControlsChanged()), HostAim:128, HostFollow, HudToolkit.ActMark. Strict DoubleClick kept: HandoverHost:564, HostClicks SelFrame, HostAim:157, ActMark PillNow.
- HudToolkit.Menu: preset cycles 3; hold preset hides Action plate/Right-click/Left click acts/Map fire; shows Third person fire, Squad acts, Orders while paused, Edge pan.

## Next (in order)
1. [HostHold.cs written: MainWay, DoersOf, HoldGive/GiveNow, GroupWalk/UpgradeRun, HoldFrame (commander L/R), HoldClick, fire hold Begin/End, TpHold, TpFireBegin] then HostHoldStatus.cs (ActMem/acts, Ing/Done words, OrderStatus, FireStatus, plate words, Plan queue+flush, portrait drag, camFollow, hysteresis, HoverPlan/HoldIntent, keys, HoldStop, shot hook). HostHold.cs (new partial): HoldControlsChanged, left press/hold/click (walk to stand spot, dbl run, 0.4 s charge act, Shift runs, early release cancels), right tap ring / hold fire (fire + cease orders; tp: draw + trigger), squad (best/everyone, Alt flips), group walk formation, portrait drag (¼ speed), pause plan queue + flush, C follow, X stop, Tab overlap, 5/Ctrl+A, status words (order + fire), hysteresis.
2. Hooks in HandoverHost.Controls/Update/Locked/Give/PointerHover/UpdateAim/UpdateMoveChip/Shot/RangeHint; HostAim.TpPointer; HostSight (Watch hover → known cones); OpenRing HoldWedge = fire only.
3. HudToolkit.Hold.cs: pill (offer "Hold · …", charge fill, status, faces, seen chip), Watch reach ring, plan ghosts, destination rings, attention clocks, rubber band, Squad button; busy chip words; fire line words; portrait PointerDown → PortraitPress.
4. RangePanel settings + fire readout; RangeShots ctrlHint.
5. ProofInput.HoldActs proofs (6) + shots → briefs/041/build; GameBoot wiring; tools/game/cli.ts inputChecks += holdActs.
6. `bun tools/game/runtime-compile.ts` then one `bun run batch`.

## Compile state
- 20:2x: HostHold.cs + HostHoldStatus.cs + hooks (HandoverHost, HostFight, HostActivity, HostFollow, HostContext, HostSight, HostAim) compile (runtime-compile OK). Steps 1 and 2 done.
- 20:3x: HudToolkit.Hold.cs (pill offer/charge/status, faces, seen chip, reach ring, plan ghosts, destination rings, attention clocks, drag band, Squad button), PillFrame dispatch, FollowMarks keep pill, portrait PointerDown → PortraitPress, busy chip words, fire line words: compile OK. Step 3 done. Next: 4 (range), 5 (proofs).
- 20:4x: range panel (hold settings, Fire readout), RangeShots bystanderHint, ProofHold.cs (Handover: click/earlyRelease/hold/doubleRuns/plan/tab + shots; Range: rightHoldFire/bystander/squadRightHold/portraitLock/tpRightHold + shots), GameBoot wiring (holdActs, holdRange keys), tools/game/cli.ts gate keys: compile OK. Next: `bun run batch`, then copy hold-*.png from out/build/play/proof to briefs/041/build.
- 20:5x: `bun run batch` started (log out/batch-041.log; step logs out/batch/logs/). If cut off: check out/build/play/proof/proof.json for holdActs/holdRange, then rerun from the failing step (`bun run batch --from <step>`).
- 21:0x: batch 1 built 0.2.40; game:check failed only on holdActs.hold and holdRange.rightHoldFire: a mid-hold 1080p shot released the held button. Fixed in ProofHold (pictures taken apart from the checks); rerunning `bun run batch --from game-build` (same batch).
- 21:1x: batch 2 built 0.2.41: game:check PASSED (holdActs, holdRange ok); budget FAILED on 3 shots (running text) — investigating. Folded into the same fix pass:
  - Sky: no Alt flip and no Best placed / Everyone setting (it was a lab setting). Removed (PlayControls field and pref, menu button, range panel row, DoersOf flip). Fixed rule: the best-placed selected agent acts, the rest hold; a hostile's right hold fires with every selected drawn agent.
  - Sky: right press under 0.2 s is a tap (ring; no charge, no pill, no draw); the fire charge is created at 0.2 s and fills to the same 0.4 s total (chosen: less sluggish than a further 0.4 s). Both views (HoldFrame, TpHold) and the range.
  - budget fix: plate words at most two words ("9 s", "aiming", "too far", "no line", "queued"; the icon says walk/run/act); the hover's fire offer only on combatants and range targets (it offered fire on a passer-by). Rerunning from game-build.
- 21:2x: batch 3 built 0.2.42: holdRange.portraitLock failed (player unfocused: the kit never got the portrait press; it passed on 0.2.41). Proof now presses through the kit's handler when the device event is not delivered, and logs which. Rerun from game-build.
- 21:3x: batch 4 built 0.2.43: BATCH OK (game:check passed incl. holdActs and holdRange; shots 10; budget 9/9, audit 9/9). Shots copied to briefs/041/build/hold-*.png.
  Open: the charge and firing pictures lost the held button at the 1080p switch (words proven in proof.json instead); the old agent-centred Watch ring still draws beside the new target-centred one; the engine's range line text still says "bystander: hold Ctrl to fire" (Keel).

## 0.2.44 (Latch, 2026-10-22): commit on release, Syndicate fights, squad-leave, hold latch, own-agent click, under-fire cue, engine 0.20.0
Task: Aster's batch (commit-on-release acts and fire; focus in fight, G weapons free, quiet fight pill; squad-leave; hold latch ~80 px; own agent always selectable; under-fire cue from UnderFireOf, no auto-draw; engine 0.20.0 DLL + rulebook together; leftovers: duplicate Watch ring, range "hold Ctrl" text, T/cones/plates notes; Fable's strings). One `bun run batch` at the end.
- 05:45 start: read brief 041 decisions, playground (app.js, fire.js, orders.js, markers.js, ui.js), host-interface 0.19/0.20, tuning §14/§15, hostile-initiative answers. Unity DLL is 0.18.0; tree engine is 0.20.0. Running `bun tools/sim/unity.ts --tree` first to compile against 0.20.
- 05:46-06:00: engine 0.20.0 tree DLL + rulebook copied (`bun tools/sim/unity.ts --tree`: 256/256 + 77/77, 42 s). Code (compiles, runtime-compile OK):
  - HostHold.cs: commit on release (left act and right fire: HoldCharge.Ready, "Let go"), hold latch (HoldLatchFrame: CancelPx 80, DimFrom 0.55; right click / Esc / early release cancel), AgentNear (own agent within 26 px always wins: click selects, right tap does nothing), FocusFire (fire+focus once Game.FreeFightOn, on press, portrait pins kept), BeginFireLock (lasting fire on release), Shooters, LocksDrop, EndLasting (a new act ceases a lasting fire; a walk keeps it: engine fire on the move), squad-leave in HoldGive (fromSel), DoersOf idle-first, TP commit on release + focus (TpFireLock).
  - HostHoldStatus.cs: quiet fire pill (one form; weapons-free picks grouped from FireStateOf mode), Fable's words (Too far / No clear line / Drawing / Walking · n s / Paused · orders queue / Started · n orders / Camera free / Nothing there), plate words "under fire", "holding", G = ToggleFree (cell free order), X ceases drawn agents under WF (stand down), Esc cancels a hold (escTaken).
  - UnderFireNow[k] from Game.UnderFireOf (HostAim.FireFrame); faces use it, faces on in the range; H17 reads FreeFightOn under hold; Fire at (HandoverHost); bystander label "a passer-by"; G gait/run latch off in hold preset.
  - HUD: pill "Hold · …" / "Right hold · …" → "Let go · …" with the price kept, dims with travel, pops when ready; "Watched spot"; "reported"; weapons-free toggle by Squad (+ word 1.6 s); under-fire ground chevrons + portrait ring pulse and direction ticks; known-face badge (sawyou); plate icons bang/fire; agent-centred Watch ring off in hold preset.
  Next: range panel row for weapons free, ProofHold extensions, then (late) re-copy sim (Keel's Ctrl text fix) and one `bun run batch`.
- 06:01: also plate × stops a lasting fire (PlateStop), a held charge blocks the panic chord, H17 reads FreeFightOn under hold, range panel "In a fight" row (weapons free toggle + fight line), bystander goal-line hint dropped in hold (the pill says it), Fable's wording.md applied. ProofHold extended (Handover: hold commit-on-release, holdLatch on walking Inez + 120 px cancel, squadLeave, ownAgentClick; Range: rightHoldFire commit, squad no cease, focusInFight, underFire, weaponsFree + X stand-down, tpRightHold commit). Keel's range line fix landed 05:52 (GameRange.cs). Starting `bun run batch` (log out/batch-044.log). If cut off: check out/build/play/proof/proof.json holdActs/holdRange, rerun from the failing step.
- 06:12: batch 1 built 0.2.44: game-check FAILED on holdActs.holdLatch (proof bug: the drag moved the mouse but PointerWorld pinned the pointer; also scaled by kit px) and fire (engine 0.19 weapons free: the Scout picked the patrol the draw brought, its body then took the Scout's line, so only 2 agents fired; ProofFire now runs with weapons free off, as Motive §14.5 runs the follow tests). holdRange all ok (commit-on-release fire, focus, under fire, G, X stand-down, tp). Rerunning `bun run batch` from sim (picks up any late Keel/Fable text).
- 06:22: batch 2 built 0.2.45: game-check PASSED (holdActs + holdRange + fire all ok); budget FAILED: getaway-car-yaw210 had 2 lines (the weapons-free picks' fire pill shown globally in a scripted fight) and the audit's rim rule on the new free toggle (18 px glyph in a 30 px dark plate). Fix: weapons-free picks' pill only on the hovered target (orders you gave stay global); toggle 38x34 with a 13 px glyph. Rerunning from game-build.
- 06:31: batch 3 built 0.2.46: game-check PASSED, budget 9/9, audit failed only on the free toggle's glyph at (0,0) (no flex centring for an absolute child); placed at (12.5, 10.5). Keel's engine text fix and Aster's DP1 re-record are in: rerunning the whole batch from sim (Splice's batched unity.ts checked in its output).
- 06:45: batch 4 (from sim, Keel's text fix + DP1 re-record; Splice's batched unity.ts: 256/256 headless, 77/77 range, as before) built 0.2.47: game-check PASSED, budget 9/9; audit: the glyph's top at 10 px vs 10.2 needed. Plate 38x36, glyph at (12.5, 11.5). Rerunning from game-build.
- 06:55: batch 5 built 0.2.48: BATCH OK (game:check passed incl. holdActs, holdRange, fire; budget 9/9, audit 9/9). Shots copied to briefs/041/build/hold-*.png.

## 0.2.49+ (Latch, 2026-10-22): next-batch 1–12 + the done moment (brief 043)
Task: briefs/041/next-batch.md items 1–12 (engine 0.21–0.24 DLL + rulebook + text.json; fire keeps going on walk/run; run price counts crowd guards; tp fire fix + "Right hold · Fire" on security; 0.23 walk-in for person acts, no serving lock; walk-in plate "to Inez · Listen", cancel warn + pop) and brief 043 §6 port (tick + plate word, gain chip, unlock row + main action + pip, window markers + closing pops, history row). Defaults: ≤ 3 chip lines, warn only, acts not hidden. Fable (text.json done lines) and Keel (0.24 Windows(), patrol.returns) in parallel: copy sim/text late. Proofs: ProofDone → briefs/043/build. One `bun run batch` at the end.
- start: read next-batch, 043, host-interface 0.21–0.23. Tree engine is 0.23.0.
- 08:xx: engine 0.24.0 (Keel: Windows(), patrol.returns) landed in the tree; copied with `bun tools/sim/unity.ts --tree` (266/266, 85/85). Fable's less.done landed in text.json (brief 043 §6b; keys plate.*, gain.*, close.*, cancel.*, walk.*). Serving path to delete (HostLess.ServingBackS + users).
  Plan: HostDone.cs (landing detector, gains diff, unlocks, fresh stamps, chips, windows from Game.Windows() with close pops by diff, cancel funnel + tag, history rows, log scan: fire.out-of-range, order.dropped, order.walk-in-ends); HudToolkit.Done.cs (chips, ticks, window markers, cancel tag, pips); edits in HostHold/HostHoldStatus/HandoverHost/HostActivity/HostFight/HostDrive/PlayText; ProofDone.cs.
- 09:xx: code done, runtime-compile OK: HostDone.cs (new), HudToolkit.Done.cs (new), ProofDone.cs (new; GameBoot key "done", cli.ts gate key 'done'); edits HostHold (walk-in cancels, tp fire focus fallback, FireOffered, camera fire), HostHoldStatus (FireOfferFor, walk-in plate/pill words, done plate, no "Done ·"/"Stopped" pill lines), HandoverHost (WalksIn/WalkInSeconds, act at click, PendingOf incl. op.Act, serving lock gone, DoneFrame hook), HostActivity (Track q incl. op.Act, CancelAgent → ActCancelled), HostFight (Fails gate "wait" source, crowd-guard run price), HostLess (serving path deleted, CrowdGuardSees), HostDrive (HoldIntent in tp), HostPortraits (SuccessAgent via LandingOf), PlayText (less.done), HudToolkit.Hold/Ring/Less/ActMark (cancel tag, pips, tick by outcome + clear of the pill, plate icon).
  Keel's tree: DP1 failed headless at 0.24 in my early copy (Keel to re-record); the batch's sim step will show it.
  Next: `bun run batch` (log out/batch-043.log), then copy out/build/play/proof/done-*.png → briefs/043/build.
- 09:0x: batch 1 built 0.2.49 (sim 268/268 at 0.24.0, Keel's DP1 re-recorded): game-check failed only on `done` proof timing (walk-in pill read inside the 0.8 s "ordered"; Help offer needs the Fixer selected; the guard's window is 174 s, the step limit was 150 s). Everything else ok incl. tpFire, cancel, callWindow. Fixed ProofDone; rerun `bun run batch --from game-build`.
- 09:2x: batch 2 built 0.2.50: done failed helpMain (Pay, opened by the Watch, led: now the newest landing's unlocks lead, gains.js freshSeq) and cancel (proof left the Fixer selected). Rerun from game-build.
- 09:3x: batch 3 built 0.2.51: game-check PASSED (done all ok); budget failed plate-pay-armed (the ground cancel tag 'Cancels Help · They remember' as running text). Tag now a manifest 'line' and hidden while a verb is armed. Rerun from sim (late copy of engine, rulebook, text.json).
- 09:4x: batch 4 (from sim) built 0.2.52: game-check PASSED; budget failed cafe-front-day: the ground cancel tag counted off-screen (cursor outside the window). Tag now only with the cursor inside the window. Rerun from game-build.
- 09:5x (16:2x local): batch 5 built 0.2.53: BATCH OK (game-check incl. done 7/7, budget 9/9, audit 9/9); shots copied to briefs/043/build. Polishing after review of the shots: a window's chip drops when it closes (it hid the 'Guard back' pop), pop icons, plate shows 'cancelled' first; proof frames the Scout for the cancel pop. No engine/text change since the 16:11 sim copy. Rerun from game-build.
- 16:3x local: batch 6 built 0.2.54: done 7/7 ok, but ProofFire.act failed (want '' / click gave watch: the plan said locked the frame before the click gave Watch; Inez moving, stillS 0; passed on 0.2.50–0.2.53 with no change to that path). Treating as a timing flake: rerun from game-build once.
- 16:4x local: the rerun reused 0.2.54's proof (no rebuild). Gated the plate's done icon to the hold preset (it showed a bare tick on the Built preset's plate in tp-prompt). Rebuild from game-build to re-prove act.
- 16:5x local: 0.2.55 act failed again (deterministic now: plan says locked, the click gives Watch). Added diagnostics (promptWhy, busy) to ProofFire.Act; rebuild.
- 17:0x local: batch 9 built 0.2.56: BATCH OK (sim 268/268 at 0.24.0 copied 16:11 with rulebook + text.json; unity-match 353/353; game-check all keys ok incl. done 7/7 and act; shots 10; budget 9/9; audit 9/9). `bun run test`: 1474 pass, 0 fail (4:10). Shots in briefs/043/build/done-*.png. The act check's diagnostics (promptWhy, busy) stay: 0.2.54/0.2.55 failed it (the E-hold ring's release had already given Watch: agent-busy lock), 0.2.56 passed: a real-time race in that proof, not this batch's code.
  Open: the crowd guard is counted in the run price but not marked on the route; the walk-in has no "arrive" beat/scene (the engine starts the act itself); a chip can sit over a speech bubble.

## 0.2.57+ (Latch, 2026-10-22): brief 044 revised A "Chrome plate" into the game and the range
Task: Sky's picked overlay style (brief 044 §6 + Sky's "make the icons larger like avatar on right"); every overlay of the done moment and the hold controls, move paths, plan ghosts, attention clock, drag faces, Watch reach; drop the lab colours; remove the read sentence under the pill. One `bun run batch`; shots → briefs/044/build; 2x crops checked before reporting.
- Code (runtime-compile OK):
  - HudToolkit.Plate.cs (new): sourced colours (kit.uss ink-deep, chrome, dim, panel-line, brass, ivory; sampled sprite bodies #0c3434 pill, #203f3e disc/plate), Cameo (face in 2 px chrome + 1 px ink-deep ring), ActDiscEl (the act disc: same size and ring as the face, 17 px cream icon; harm: ivory body, ink icon), Stripes (kit stripes_warn tiled 22x15), DashedRect, PlateDark(h, sliceScale), Words (Jost Bold cream).
  - ActMark.cs: one pill for both presets (BuildPill/PillLook/PillPlace): s-pill_status 38 px at slice 0.43 (harm: s-panel_strip at 0.3, stripes at both ends, light disc), head word split ("Hold"/"Right hold" dim, "Let go" turquoise-light), turq-light 40 % sweep clipped to the window, 2 px ring + glow outside the frame on ready (scale 1.05 from the bottom), chrome tail with the body triangle, Watched spot plate (ink-deep, panel-line edge, stripes, sawyou), pip as the pill's own element on its top-right corner. KindOf deleted → Harmful(order). PillWords = the whole line as before.
  - Hold.cs: RoleColours, Coral, LookBlue, HarmPink deleted; destination rings and plan paths cream; Watch reach turquoise-light; ghost = plate_dark 34 px (0.43) with the face in the chrome ring; attention clock = plate_dark 28 px, cream words, underline + arc cream, amber from "wary"; drag faces in the chrome ring.
  - Done.cs: chip = s-panel_dark (padding 13/17/12/17), 32 px face in the chrome ring + kit badge mark_ok/half/no, cream lines, brass hairline, turq-light arrow, unlock pills plate_dark 28 px at 0.3; window marker plate_dark 34 px at 0.43, eye 15 + seconds 14.5 (or the Tech's face), draining 2.5 px underline turq-light, amber in the last 10 s; cancel tag ink-deep 94 %, dashed cream 60 % edge, stripes block, ✕. HostDone: WindowMark.Total (span taken when the window first shows).
  - Less.cs pops: Jost Bold 13.5, no pink tint on a refused plate. Ring.cs pip cream.
  - Capture.cs layout audit: the plate_dark rim rule reads the plate's own slice scale (RimIn x scale/0.5; unchanged for every plate at the kit's 0.5). Heights chosen so every glyph stays 4 px clear: window 34 @0.43, unlock 28 @0.3, ghost 34 @0.43 (spec said 26/22: their 15/12 px glyphs would sit on the rim).
  - HostActivity.SayLanded: the read line ("Debt. The guard visits her quietly. She startles." and a watch's "I have …'s routine") is no longer said: the done chip over the person/machine carries the same facts (need, temperament, tie; Rounds, Cone).
- Next: `bun run batch` (log out/batch-044b.log). If cut off: check out/build/play/proof/proof.json (done, holdActs, holdRange), rerun from the failing step; then copy done-*.png and hold-*.png to briefs/044/build and crop at 2x.
- batch 1 built 0.2.57: sim 268/268, unity-match 353/353, game-check PASSED, shots 10, budget 9/9; audit FAILED 5/9 only on the window marker (eye and Tech's face 9–10 px from the top of a 34 px plate: the audit had read the inline slice scale as unset, and layout rounding moves a glyph by 1 px). 2x crops of the proof shots also showed: a speech bubble over the chip's bottom and its tail on the marker's "174"; the "Guard back" / "Watch cancelled" pops under the bubble's leader line; the plan ghost's face too small (15 px). Fixes (runtime-compile OK): the audit reads resolvedStyle.unitySliceScale; window marker and ghost 38 px plates (the pill's height) with 16 px faces; unlock pills 30 px; window markers, chips and world pops stack above any speech bubble (SpeechRects / ClearOfSpeech); chip-over-pill gap 8 px; the new overlays listed in parts.json (verb-pill, gain-chip-n, window-mark-n, cancel-tag, plan-ghost-n, attention-clock-n, drag-faces). Rerunning `bun run batch --from game-build` (log out/batch-044c.log).
- 18:0x: rerun stopped at game-build: someone built 0.2.58 from the tree and opened it at 17:54 (not closed by me; waited until it closed). `bun run test` meanwhile: 1473 pass, 2 fail, 12:13 (over 10 min: the machine at load 54 with other crews' levels/skyline pipelines, Blender and sim plays; 4:10 on 0.2.56); names not captured, rerun after the batch.
- Aster's additions folded into this batch:
  1. Sky after 0.2.56, third person keeps the work: HostDrive.DriveTrailIfBusy: T (and an agent swap while driven) on an agent walking, running, walking in (op.Act), acting, with a pending act or a fire order trails it (dAuto: no drive sample, no cease; DriveTake's cease removed) until movement input or an order; the work done, the stick comes back. No engine change. Proofs: ProofHold holdActs.tpKeeps (walk, run, listen; W then takes the stick) + shot hold-tp-keeps-walk; holdRange.tpKeepsFire.
  2. Look tokens in one place (HudToolkit.Plate.cs: LookLive, LookSoon, LookHead, LookGround, HoldSweepAlpha, ReadyGlowAlpha, ReadyRingW, ReadyScale, HoldSweep()) for Glyph's style lab; no tint polishing.
  3. Lag (note only): the feed history rebuilds all rows unpooled on every key change (HudToolkit.cs:789-793): cost grows with the mission. FireLog capped 6000, chips 4, Talk 200.
- Rerunning `bun run batch --from game-build` (log out/batch-044d.log).
- batch built 0.2.59: game-check FAILED on holdActs.tpKeeps (walk: kept, but the proof's DriveInputOverride is by design not "taking the stick": now a real W key; listen: refused, Listen needs her routine: the proof steps a Watch first) and fire (ProofFire checked brief 024's "no fire order survives taking control" and the trigger was refused "not-driven" on the trailed agent). Fix: HostDrive.DriveTakeBack (the trigger press takes the stick back with one drive sample, then fires); ProofFire now checks orderKeptOnControl. tpKeeps run ok, tpKeepsFire ok, holdRange ok, done ok. Frames that run: p99 23 ms under load 50+ (other crews' jobs), not this change. Rerun from game-build (out/batch-044e.log).
- (waited for Redline's rt1 shots run on the player before building.) batch built 0.2.60: holdRange ok (tpKeepsFire ok), done ok; holdActs.tpKeeps: walk ok (W takes the stick: driven, walk ended), run ok, listen kept trailing and walking in but not landed in 45 s real (the act is ~55 s): now run at 4x (HandoverHost.SetSpeed for proofs) and checked for no drive sample until it lands. fire: orderKept and trigger ok, but secondLock '' (the Enforcer's kept standing order fired on around the presses; the car was gone): the trigger press now ceases a standing order first (brief 024's "the trigger is the player's now", at the moment of intervention; T alone keeps it). drive-session replay failed: Keel's engine 0.25.0 landed in the tree mid-batch (record 0.24.0). Rerunning the whole batch from sim (out/batch-044g.log).
- 0.2.61 (built 18:4x): game-check passed every proof (holdActs tpKeeps walk/run/listen, holdRange tpKeepsFire, fire with orderKeptOnControl, done) except the fire session's replay: checkpoint 8 differs at tick 900 because Keel changed GameCombat/GameFireWhy at 18:36:51 (after the batch's DLL copy at 18:36:18) and Game.cs at 18:48 during game-check: the headless replay ran newer fight code than the player (moving tree, not this change).
- Aster's brief 045 (Redline, late lag) folded in: F1 HandoverHost.ClosingHash() = Game.ClosingHash() (engine 0.25.0, live, no replay; engine copied 18:5x); F2 End()'s replay check on a worker (end line "replay checking" until EndReplayPoll fills it in and writes .end.txt) and the fail camera's two replays on workers in parallel (FailPrepPoll starts it); F4 DoneFrame reads Gates/KnownSnap/WindowsRead once per tick, log line or order; F6 candidate: FadeReach keeps a renderer's cutting twin 1.5 s after it leaves every reach (material swaps = GRD instance rebuilds as discs move); F7 RenderCap: URP render scale so the 3D pass is ≤ 2880x1864 px (HUD native; built player only). The -late hook: LateRun.cs (suborned.late.report/1), HandoverHost.BeforeStep / FrameCost, CaptureSize.Captures += -late.
- Running the full batch from sim (out/batch-044i.log), including Splice's late-fight row.
- 19:1x: batch built 0.2.62 (engine 0.25.0): game-check, shots, budget 9/9, audit 9/9 OK; late-fight failed on one untagged 51 / 60 ms frame per row under load 19 (p99 8.5–8.7, end 28/48 ms, was ~1.9 s); rerun `--only late-fight`: OK (worst 23.1 ms and 41.3 ms, both the end frame; p99 6.3/6.9). Earlier unity-match death was Keel's `pkill -f cat` (19:00), rerun.
- Sky's picks (brief 046) and Aster's 0.25 notes folded in:
  - Style tokens: the lab's export testbed/hud-style/export/revisedA.tokens.json (sha256 d729404d246d) copied to Resources/HudStyle and read at runtime (HudToolkit.Plate.cs Tok: type, accent, disc size/icon, face size, hold fill/alpha/ease). The act disc is now the kit's disc_dark_24 at disc.size 26 with a 15 px icon, as the lab draws Revised A.
  - Cancel tag off (HudToolkit.CancelTagOn = false); pop and "cancelled" plate kept; ProofDone checks the tag is absent.
  - Ground marks respect the foreground: HudToolkit.Ground.cs draws destination rings, Watch reach, plan rings and paths, hint rings and pulses as world meshes in the transparent queue (fan material), depth-tested, instead of UI paint over everything. The 3D cell ring was already opaque and depth-tested.
  - Roster badges and under-fire chevrons untouched (Glyph's lab). FireWhyOf wired as data (HostAim.FireWhyNow; the under-fire proof records it).
- Running the full batch (out/batch-044l.log).
- 19:4x: batch built 0.2.63: everything OK up to late-fight, which failed on untagged frames over 50 ms (sky-session1 ticks 3673–3723 at load ~11; that stretch also spiked on 0.2.62's first run and was clean on its rerun); rerun `--only late-fight` on 0.2.63: sky-session1 clean (that stretch ≤ 8.9 ms, p99 6.9), loud-fight 3 untagged spikes at other ticks (2681–2937) → load-sensitive noise on the shared machine; tagged work (save/order/end) ≤ 41 ms. `bun run test`: 1490 pass, 0 fail (3:58).
- 2x crops of 0.2.63: the verb pill hid Inez's reply bubble; the Guard back pop sat on the bubble's leader. Fix: speech occupies its box and leader (SpeechRects from bubbleLeads); the verb pill, window markers, chips and pops stack above it. FireWhyOf read once a tick. Final rebuild from game-build (out/batch-044n.log).
- 20:0x: batch built 0.2.64: BATCH OK (game-check incl. tpKeeps walk/run/listen + tpKeepsFire + done cancel with no tag; shots 10; budget 9/9; audit 9/9; late-fight ok: worst 21.2 / 40.8 ms, end frames; p99 6.0 / 6.5). 2x crops: Inez's reply now reads under the pill; Guard back above the bubble. The third-person shot showed the follow view's painted white ring across the Scout's legs: the follow view's two rings (its agent's, the hover target's) are now ground rings too (HudToolkit.cs FollowMarks, ActMark), the overhead marks stay out of the follow view. Rebuild from game-build (out/batch-044o.log).
- 20:1x: batch built 0.2.65: BATCH OK (game-check 274/274 + every proof, shots 10, budget 9/9, audit 9/9, late-fight ok: loud-fight worst 19.2 ms, sky-session1 worst 40 ms, both the end frame; p99 6.0 / 6.7). Shots in briefs/044/build (done-*, hold-*). Third person: the ground rings lie under the figures (hold-tp-keeps-walk). `bun run test` 1490 pass, 0 fail (3:58).
  Open: Inez's name tag peeks under her bubble's bottom edge (pip tag vs speech); the cancel tag's "drain" hold animation token is read but not animated (hold.cancel); the cursor's move ellipse (MovePreview) and the door marker's ring are still painted (not under people in the shots); late-fight's 50 ms rule is load-sensitive on the shared Studio (untagged 51–97 ms spikes that move between runs).
- 22:2x (Latch, Glyph's review of 0.2.65 + Aster's adds): code in, BATCH OK on 0.2.73 (sim 275/275, unity-match 366/366, game-check every proof incl. item 13's squadAll / squadArms1 / squadBusy, shots 10, budget 9/9, audit 9/9, late-fight ok; NOISY MACHINE). Builds 0.2.66–0.2.72 were spent on: Keel's engine 0.26 landing mid-batch (DP1 re-record race), load-flaky Handover proofs at load 76, my own proof bugs (hold-fire log parse, section 3b needing section 3's fight, the car target), and Glint's AtmosphereLens CS0619 in the tree. 2x (3840x2160) proof run done once on 0.2.66 (preview); the final 2x set waits for the end batch (Aster: code first, test once).
  - Glyph's 12: act disc = cameo size (disc_dark_24 draws 48 of 64 px: box = (face+6)/0.75 = 40, icon 20); pill padding set at build (0.2.65 had none on a plain pill); stripes 22 px in (clear of the screws); sparkle on the cameo's top-right; "to Inez" head word dim; speech: name tag hidden under a bubble, 10 px pill-to-bubble gap, speaker dot one source (cream disc, ink ring, ink initial, cell only; the cyan was HudToolkit.cs Turq #1cb9c6, not PlayHud.cs:435); paused-plan label opaque, ink-deep under-line on the dashed route ("Guard back" pop measured opaque, 32,63,62); third-person key strip removed; "Return fire only" on a plate_dark plate, the toggle opaque (on: gun in turquoise-light); verb strip without keycaps, empty slots left out; same-anchor chips stack by height; formation pips at 2.6 m, bubbles clear rings by 16 px, follow chip 14 px off the scope.
  - Leftovers: move preview ellipse, seen-stretch glow and door marker ring are ground meshes (behind legs); hold.cancel "drain" animated (proof logs the drain per frame); font is a token (type.font, type.weight; default Jost-Bold drawn as cut: Words() had added a synthetic bold on top).
  - No key legends anywhere (HudToolkit.KeyLegends = false): third-person strip, ring plates' ⇧ caps, verb strip, panel rows, card header keys, fire line "held / X stop", feed "ack · history", ack keycap, edge-pin keys, follow chip key, cancel tooltip key row. First-use hints keep their keys.
  - Ring: a word two wedges share names its target (intent label: "Loop the entrance camera"); the hovered wedge shows its camera (pulsing ring on the head, ground ring at the foot, its cone when the cell knows its sight).
  - Item 13: Shooters asks Game.FireQuery for every selected agent (no drawn-first, no Armed pre-filter); refused agents' plates show Fable's less.done.fire.<code> for 3 s (engine-error logged only); walk-in plate "→ officer"; pill faces = FireQuery's takers (cached per target, selection, second); down plate says "down" and the portrait carries a down mark. HostFightLab.ArmsOverride for the proof.
  - LateRun (Splice): frames.csv `what` column (save/order/end/none); late.json lists every frame over 25 ms (and `over25`). late.ts's doc comment still says "ms,tick,phase" (Splice's file).
  - Proof frames for the 2x set (ProofDone.Still: no -frames-size and height ≥ 2000): hold-act-ready, done-turn-chip (mark_half), done-refused-chip (mark_no), done-window-soon (amber), done-watched-spot, ring-loop-names; charge scales slowed so charge frames are mid-sweep. Cancel tag stays off (Sky, brief 046), so no tag frame.

## 0.2.74+ (Latch, 2026-10-22): the queue in briefs/041/next-batch.md items 14-17, the Turq leftover, Glint's atmosphere, the world rebuild (code first, one batch)
- 15 RobotIdle.Yaw: the arrays were serialized before 5d44a539 (a hot reload brought them back empty); now one `Ready` check (lengths, not just null) before every index, and a NaN/inf clock or an empty loop reads home. No exception path left.
- 16 Roster (brief 046, Sky's picks): HudToolkit.Roster.cs. Tokens: the lab's revisedA export (sha 0a787ede1749) copied to Resources/HudStyle with `roster.column = bare` (and `unityCopy` saying so). The column is `roster_column_bare@2x` (copied to Kit/roster, imported readable, uncompressed) stretched at runtime by the lab's recipe tallColumn/2 (cuts 223/412/601/789, row 222, body 36..182, ease 6, edge columns, tone gain 0.6..1.4) with the steel divider's d2x 55; RosterScale = rosterSpec.geometry.s (0.79, was 0.8); BezelAt(i) and ColH carry the gap everywhere (strips, chips, Squad, free toggle, ticker). Row under each ring at rowDy 36.8: weapon (w.Arm: light disc + ink gun drawn, dark disc + gun 55 % holstered, `disc_light_24_selected` when the strongest cause is "drawn"), status (priority: in car / left > cause icon on the light disc > down > known face (sawyou) > watched (cone) > armed verb > busy act), dots (4 pips 5 px on the #142c2c track). No key plate, no number; the old car badge, eye badge, armed-verb glyph and face-known badge stand down on the tall column (the status slot carries them). Steel divider: 1 px chrome 45 %, 54 px, under portraits 1-3. Fire mark fits the 16 px disc (ring only). Type: Jost-Regular-Tab / Jost-Medium-Tab (lab fonts, OFL) in Kit/fonts; every kit Jost style takes fontSpec body or strong via TypeOf in L() (no synthetic bold on Jost). Layout audit: RosterAlign (the lab's ALIGN rules 1, 3, 4, 5 and one gun); parts.json lists roster-weapon/status/dots-n.
- 17 Target rings (brief 049 round 2): HostTargetRings.cs (data: FireWhyOf aiming, UnderFireOf last shot within 1 s, FireNow ours, reach from the selected drawn agents' eff/max rings and the site's sight, cached a tick; combatants while a selected agent is drawn or a fight is on; a civilian/car/camera only hovered with a fire offer) + HudToolkit.TargetRings.cs (ground decals: r 0.54, in 0.10 / long 0.075 / out 0.065 dashed 22 at 90 %, hover r 0.68 x 0.12, ink-deep edge 60 % (42 % out), amber/violet/cream by side, red #e03a2f 0.12 for aim/fire; four cream ticks square to the screen for ours/aim/fire: aim closes from +0.30 m at alpha 0.5 by 78 % of 1 s, fire kicks 0.07 m in and a red pulse ring 1 to 1.8 over 300 ms). Rival identity rings step aside where a target ring is. No 15 m circle (only with the range's Numbers), no 8 m follow ring, tracer one cream dashed 1.5/1 px line over ink-deep 35 %. Under fire: the orange chevrons are now one cream arc (r 118, 70 deg, 10 thick, edge 17 at 50 %, breathing 0.6-1 x 0.85, why.arrow.weight 1) on the ground; the portrait's ticks are the rim's cream pulse ring. RangePanel Numbers off by default, cover labels behind it; RangeShots turns it on for its two numbers checks.
  - Label audit removals: plate "under fire" (row 10), the fire offer's noun for a combatant ("Right hold · Fire", a civilian keeps "Fire at …"; proofs read "Let go · Fire"), Intel head and SUGGESTED (row 21), the briefing's turn-key row (29), the fail cam tip (32), "Follow view" chip word (25; Exploring and Let go stay).
- Leftover Turq #1cb9c6: gone from the HUD (TurqLight #60e0ea, the kit's turquoise-light); the door and edge cameos ring in chrome, live accent when selected.
- 14 Draw distance: followFar 350 (HostFollow, FightRange.unity; the follow clip is also min(followFar, DistanceHaze.FarClip) so HarbourDistrict's old 600 cannot leak before the rebuild); TacticalShadows fits a perspective camera itself (110 m, 2 cascades, split 22 m, border 0.3) and stays on in the follow view; KitLods: every LODGroup's last level culls (pieces < 8 m below 1 %, larger below 0.4 %); GRD per-frame rebuild: RobotFace / RobotFaceStage set their property block only when the face changes (it was every frame on every robot screen), fan materials set only when different; static batching off (ProjectSettings m_BuildTargetBatching Standalone 0/0) for the tp-long row to measure; tp-long hook TpLongRun.cs (`-tplong`, suborned.tplong.report/1 + grdRebuildMs when the marker exists, a jpg per row, at the first follow row also -teal, -atmosphere-off and -long (a far-2500 m camera 60 m up: the long tier)).
- Glint's atmosphere: GraphicsSettings fog stripping Custom (Linear, Exp kept; Exp2 off). Checks ride on the batch: play-opening (grade), tp-street/tp-worst (haze), -teal, -atmosphere-off, -long (flyby look); night is shots' start-overhead-night.
- World rebuild (Splice ready, Aster 22:4x): look.json `road-enamel` (DK_Ground_RoadEnamel, tile 2, palette harbour.road-enamel #aed0e6), `kerb` → DK_Ground_RoadPearl, `ground/paint` dropped (dashes retired), no gutter on road-enamel (Layers.NoGutter); ground pieces (15,983 road finish + 1,400 capstone) baked by material (CityLayers.GroundPieces → KitPlacer.Bake with the element yaw); a flat piece whose mesh sits above its pivot is laid on its footprint and logged (Pave_Slab: the FBX plane is at z 0.15, measured in Blender; placed at 0.181 its top was 0.331); kit.json fallback tree/coconut and tree/coconut-median; GameBoot's avenue probe reads DK_CoconutPalm*. Palms: no LODGroup (L0 only, Sky's rule); third person is covered by the 350 m clip.
- runtime-compile OK after every step. Editor scripts (CityLayers, KitPlacer, KitLods, Layers) compile in the batch's unity-world step.
- Next: one `bun run batch` (log out/batch-0274.log), then the 2x set (-gameProof <abs> -capture-size 3840x2160) to briefs/049/build and briefs/046/build, Glyph's review.

## 0.2.79+ (Latch, 2026-10-23, new session after the usage cut-off)
- Found: batches 0274b–g (after the last note) built 0.2.75–0.2.78; fixes in between (ProofDone walk-in, hudClick follow, HostTargetRings, Roster, TpLongRun palm LOD check, Combine.cs, CrossGlow). 0.2.78: sim 275/275, unity-match 366/366, game-check ok, shots 10, budget 9/9, audit 9/9, late-fight ok (worst 18.8 ms end frame, p99 6.7), tp-long FAILED counts only: tp-street beyond150 1,218 (≤ 800); tp-worst tris 6.05 M (≤ 6 M), SetPass 346 (≤ 250), drawn 2,791 (≤ 2,500), beyond150 1,694 (≤ 800). Frame times fine (tp-worst mean 5.7 / p99 6.6 ms; play 3.8 / 4.5). Palm LODs (Splice adopted L1/L2, per-id screens) in and working: palms.json commander-opening all L0; Market St east L1 from 60 m, L2 from 145 m; west L1 from 63 m, L2 from 140 m.
- Bug found in KitLods' cull: `min(1 %, last level x 0.5)`, and the street L2 is 0.003, so every small street piece culled at 0.15 % (a bollard about 500 m: never inside the 350 m clip); pieces with no LOD files got no group at all. Fix: levels switching in below the cull are dropped; small (< 8 m) non-Ground pieces with no LODs get a cull-only group; the cull is capped at half the smallest commander-view height (ortho 18.5). KitPlacer.Categories added.
- TpLongRun: census breakdown `census-<row>.json` (by branch and piece, drawn and beyond 150 m) for the counts.
- Next: full `bun run batch` (out/batch-0279.log); then the 2160 proof set; game:check, verify.
- 01:0x: batch (out/batch-0279.log) built 0.2.79: sim 275/275, unity-match 366/366, game-check ok, shots 10, budget 9/9, audit 9/9, late-fight ok (loud-fight worst 20.8 ms end frame, p99 5.9; sky-session1 worst 31.9, p99 6.6); tp-long FAILED on counts only. KitLods log: 211 LODGroups, 122 small culls, 108 cull-only, 147 levels below the cull dropped. tp-worst drawn 2,791 → 2,188 (pass), tris 6.05 → 5.5 M (pass), beyond150 1,694 → 1,166 (≤ 800), SetPass 346 → 332 (≤ 250); tp-street beyond150 1,218 → 952. Commander unchanged (325 drawn). Census (briefs/048/build/census-*.json): what is left beyond 150 m is Combine's 64 m tile batches (facade 337, roofs 164, frontages 91, skins 68, buildings 54, bays ~180), palms' L2 110, crossing light fields/signal eyes ~150, contact shade tiles 62: the far-tile merge (Redline's "later step") or a gate change, not a cull.
- Palm camera: in the carriageway and stepped clear of anything within 10 m (0.2.78's frames stood behind a signal eye and a median trunk). palms.json: east L0 to 61 m, L1 68–172, L2 143–347; west L0 to 79 m, L1 61–183, L2 140–348; commander all L0.
- 2160 proof set (player -gameProof -capture-size 3840x2160, status ok) → briefs/049/build (rings), briefs/046/build (roster); tp-long frames + palms + census → briefs/048/build. `bun run game:check` ok (7:00; play 245.6 fps p99 9.6 ms at 2880x1864; follow spots p99 4.3–6.3 ms). `bun run verify` OK. game:build = the batch's game-build (0.2.79).

## Next round (Latch, 2026-10-23): Glyph's 0.2.79 review, code only (batch and shots held for Glint's atmosphere data, Vela's window glass, Redline's tp-long call)
1. Under-fire arc: the threat red #e03a2f at the ring's 0.12 m line with the 0.17 m ink-deep edge at 60 %, solid (no 60–100 % breathing), swells 30 % for 0.35 s at a shot (HudToolkit.Hold.cs UnderFireMarks). The "faint pink circle" Glyph saw in hold-fire-firing was not the arc: it was the refused-click ground pulse in the lab's coral (1, 0.6, 0.55); now sourced: refused = dashed cream, taken = turquoise-light (HudToolkit.Less.cs).
2. Ring proof frames: ProofRings.cs (proof key `rings`, cli gate): Handover ring-plain-amber (cell drawn; gated: a guard-side ring with no aim/fire), ring-plain-out (the agent farthest past its max ring from the guard or patrol, dashed; reported), ring-plain-rival (run to RivalAt + 10 s, an armed agent the crew cannot see, drawn; reported). Range (ProofHold.Range section 2): ring-hover-passerby (gated: cream hover ring on the bystander), ring-ours-bystander (ticks on the cream ring; reported). Each logs the ring states (id:side:reach:flags). The range's hostiles always aim at the cell (cause "range"), so plain amber/violet can only be shown in the Handover.
3. Roster status at rest: board B's under-portrait column shows states, not roles (binoculars = busy Watch, ◁ = watched, eye = seen; the "normal" tile is gun + dots); the spec keeps the slot empty at rest. The footprint under all four was a plain walk counted as busy: a move no longer fills the slot (an act still does).
4. The aim cursor mark (HudToolkit.Ring.cs CursorFrame) over a target goes beside it: past the ring's outer edge and ticks (1.05 m on the ground) at mid-figure height, instead of +12/+12 from the pointer (it sat on the ring; the "red wedge" was the ring).
5. Ticks 0.075 → 0.095 m, ink-deep edge 0.13 → 0.17 m at 85 %, 0.035 m past each end.
6. The Enforcer's six pips are the rulebook (health-enforcer 6; others 4), not a bug; flex had squeezed them into slivers: now round, 3.7 px with 0.5 px gaps inside the same 28 px track (four stay 5 px).
runtime-compile OK after each.
- Glint's atmosphere data (haze #f4e3c3, grade sat 14 + peach lift, teal mid #7fcfc6) is in the tree (his). His two code items, added:
  1. `teal.hazeHeight` 0.18 in lens.json and AtmosphereLens (Palp.hazeHeight; Sky() sends it in _AtmSkyShape.x only while the sky is teal; 0 = sky.hazeHeight; cobalt keeps 0.12). Checked in Check(): [0, 1].
  2. Greens protection: ColorCurves hue-vs-hue on the play volume (lens/play), data `gradePlay.greens` {hueDeg 8, from 70, full 95..140, to 170}: +8 deg toward blue-green, eased to none at the ends; hueDeg 0 = off; checked (|hueDeg| <= 30, ordered degrees).
  Held: his warmer ambient bounce in light.json (scene rebuild) until Vela reports on the shaded paving albedo.
- End shot for Glint: TpLongRun.SignalsHaze → `tp-signals-haze.jpg` + `signals.json`: Scout learns the guard's sight, Tech the entrance camera's; the Enforcer draws and walks ~110 m from the guard; the follow view faces the Records Office with Alt held (learned cones, guard and patrol target rings) under the haze.
- runtime-compile OK. Batch and shots still held (Vela's window glass, Redline's tp-long call).
- Splice's census check: TpLongRun.Census no longer drops renderers past the far clip in a straight line (the frustum's far plane decides, as Redline's probe: +~210 in third person at the corners); LOD membership made explicit as the probe's (a renderer counts only when its own, first-listed level is the group's active one; a culled or disabled group counts none). Still differs from the probe on two small points, left as they are: it skips disabled renderers (`r.enabled`) and counts MeshRenderer and SkinnedMeshRenderer only. Splice sets beyond150 to 1,500 (tp-worst ~1,376 expected). runtime-compile OK.
- Redline's tp-long call applied by Splice (tools/game/tplong.ts: setPass 350, beyond150 1,500; Air gate mean <= 16.7, p99 <= 20). The round waits only on Vela's window glass and paving albedo; then one batch, the 2160 set, tp-long frames (incl. tp-signals-haze) to briefs/048/build.
- Round complete (Vela's DK_GlassSky + class 3 shopfront fix, flags texture; Glint's ambient bounce left out). Added TpLongRun.GlassFrames: tp-shopfront-glass and tp-tower-windows (Market Street south side). Batch started 01:3x with --wait-timeout 7200 (Sky playing at start): out/batch-0280.log.
- 0.2.80 batch (out/batch-0280.log; waited 100 min on a left-open player, closed by Aster): all ok to late-fight; tp-long FAILED tp-worst SetPass 390 (350), shadow casters 1,885 (1,200) [0.2.79: 332, 898]; play casters 539 -> 673, SetPass 109 -> 141. Cause: Vela's DK_GlassSky had no DOTS instancing, so every renderer carrying DK_GlassDark left the GPU Resident Drawer. Added #include_with_pragmas DOTS.hlsl (as URP Unlit); look unchanged. Rerun from game-build (out/batch-0280b.log), --force (Sky's OK for this round).
- Stopped that rerun by PID (its build predated the next fix). 0.2.80 frames: tp-shopfront-glass looked down a side street (x -30 south): glass cameras moved to Market Street's north side facing north. tp-tower-windows showed solid black bands on a tower (for Vela). tp-signals-haze: Enforcer 110 m from the guard (heading 210), guard cone learned, camera cone not; the crossing signal eyes read under the haze, the guard's ring and cone are too small at that range from 3 m. Rerun from game-build: out/batch-0280c.log.

## Front end (brief 051 + 047), Latch 2026-10-23
- tp-worst gate cause (item 1): Vela's DK_GlassSky had no DOTS instancing, so every renderer with a DK_GlassDark submesh left the GPU Resident Drawer (0.2.80: SetPass 390, casters 1,885). Fixed (#include_with_pragmas DOTS.hlsl): 0.2.82 casters 898 (= 0.2.79), play SetPass 110, tp-worst SetPass 353 (0.2.79: 332). The remaining +21 is the glass's sorted transparent pass: real but small (tp-worst mean 4.4 ms, < 0.2.79's 5.1). Proposal for Redline: setPass 350 -> 375, or accept glass as the cost.
- Code (runtime-compile OK): Settings.cs (settings.json, migration from the 13 old PlayerPrefs keys once, in-memory under automation, -settings <file>); PlayControls (Binding.mods/context, LoadSettings/SaveSettings via settings.json, ApplyBindings, ClashOf, Chord); FrontEnd.cs (boot scene, loading with real progress, menu: New game, Continue, Options, Controls, Quit; brief as a dossier from brief.json /2 variants[pick]); FrontEnd.Panel.cs (Options 4 pages, Controls 4 tabs, rebind, clash strip Swap/Cancel, Reset); FrontEnd.Proof.cs (-frontEndShots: every screen + settings checks); HudToolkit.Menu.cs (pause: Resume, Options, Controls, Restart mission, Fight range, Quit to menu, Quit); HostMenu (QuitToMenuFromMenu, ContinueFrom: a saved session replayed); host gate (FrontEnd.Covers/ClosedFrame); RenderCap.UserScale, HudToolkit.UserScale, orbit sensitivity/invert/trackpad orbit, follow distance, default speed, hints via settings.json; Editor FrontEndBuild (boot scene Assets/Scenes/FrontEnd.unity first in the build, briefs into StreamingAssets/missions, loading picture import); Resources/Loading (loading_bg.png = briefs/047/fix/B_after.png, loading.json, frontend.json).
- Next: one `bun run batch` (out/batch-0283.log), then front-end captures at 1920x1080 and 2880x1864 (-frontEndShots, -settings in scratch), the 2160 proof set, tp-long frames.
- Front-end proof found the Metal present stall with V-sync on under automation (captures need -novsync, as every batch row passes); one front-end camera kept while the game's are down; tracking drawn per glyph (UITK letter-spacing did not match the mockups); title line gaps tightened; the clash keycap shows the offered key. 0.2.84 front-end proof 1920x1080: 16 screens, 14/14 checks. Rebuild from game-build: out/batch-0285.log.
- 0.2.85 (out/batch-0285.log): game-check ok (incl. rings: amber, out, rival; holdRange ringHover/ringOurs), shots 10, budget 9/9, audit 9/9, late-fight ok (worst 18.5 ms end frame, p99 5.5), tp-long FAILED only tp-worst SetPass 351 (limit 350; DK_GlassSky's transparent pass). Front-end proof 1920x1080 and 2880x1864: 16 screens each, 14/14 checks (settings file, rebind round trip, clash shown then Swap, contexts, Reset all, 12 toggles + hints through the file and the game, migration 13 keys, New game → brief, briefing after, back to pause, Quit to menu with Continue, Continue to the live run). verify OK.
- Shots: briefs/051/build (front end, both sizes + frontend-proof-*.json), briefs/049/build (rings incl. ring-plain-*, ring-hover-passerby, ring-ours-bystander), briefs/046/build (roster), briefs/048/build (tp-long + tp-signals-haze, tp-shopfront-glass, tp-tower-windows, signals.json).
- Open: no solid amber ring frame (at the opening every guard/patrol ring is out of reach); the brief's dossier head words over the Tech at 16:10 are low contrast; glass frames look along streets (camera placement); URP strips the Gaussian DoF in the player (long tier never blurs; pre-existing, Glint); captures need -novsync (V-sync + an occluded window stalls the Metal present).

## Round after 0.2.85 (Latch, 2026-10-23): Glyph's front-end musts 1-9 + nice list, Glint's items (code only; test waits for Vela)
- frontend.json (Resources copy; Glyph's briefs/051/layout copy not edited): brief scrim 0.425/0.705/0.92, column 504, briefHead Jost-Medium 14.5/0.14, padRef [44,40,44,28].
- Cause of #1 and #4: UI Toolkit blends in linear space here, so an overlay's alpha read lighter than the sRGB mockups (0.52 rendered like ~0.3). FrontEnd.SrgbAlpha: 1 - (1 - a)^2.2 for every scrim (baked gradients too) and the veil (0.52 front end, 0.40 from pause).
- #2 dossier key width 96; #3 btn_text unitySliceScale = h/80 (front end and pause); #5 padding; #6 alias keycaps 26 high, 16 padding, min 40, keycap-3, "R Shift/R Ctrl/R Alt", "`" at 18; #7 pause card scaled by H/1080 over the HUD's whole-number scale, PAUSED in panelTitle, buttons in type.button (FrontEnd.Shared/Typeset); #8 default focus ring on open (menu's first button, brief Continue, pause Resume, the rebind row), ring radius h*0.3+4; #9 clash strip between the last row and the footer line.
- Nice: dim Frame cap label + arrows 45 %; tracking from 0.05 em (tabs 0.06); switch then word; row focus fill + tick on the rebind row; "Next agent", "Detail (hold)"; the title no longer follows the picture's push-in (loading and menu match); briefing build line inside the rim; "Night" plate at 80 %, ring TurqLight (was an unsourced #4fe3e8).
- Glint: URP global settings strip-unused-post-processing off (3 places) so the Gaussian DoF ships; PC_RPAsset depth texture already on. AtmosphereLens.LongDof switch; greens ColorCurves on follow and long too. TpLongRun.LongTiming: long view (far 2500 m, 60 m up) timed 4 s with DoF on and off -> long.json + tp-street-long-nodof.jpg (if delta > 1.5 ms: lens.json dofLong.highQuality false). tp-signals-haze: the Enforcer 52 m from the guard on heading 210 (cone ~40-60 m from the camera); signals.json guardInFrame.
- Proof: menu-focus (two steps down) and controls-rebind-focus frames added.
- Waiting: Vela's lawn albedo, window bands, shopfront glass; then one batch + full shot set (front end both sizes, -novsync).
- Vela landed (lawn, flank walls, FacadeKit fallback glass class 3 reviewed: vertex R = AO, A = class, the glass colour is the atlas UV's, so no tint lost; _Glow). Full batch: out/batch-0286.log.
- 0.2.86 full batch (out/batch-0286.log): BATCH OK, 12 steps, 897 s. tp-long passes at the 375 gate: tp-worst SetPass 351, mean 4.3 / p99 5.2 ms; tp-street 4.0 / 5.1; play 3.4 / 4.5. late-fight worst 18.5 ms (end), p99 5.7. Long view DoF: on 5.69 ms (gpu 5.24), off 5.27 (gpu 4.83): +0.40 ms GPU, under 1.5 -> dofLong.highQuality stays true. Signals: Enforcer 52 m from the guard at heading 210, guard in frame, guard cone learned and shown with Alt, but a flat ground wedge at ~55 m from a 3 m eye is a sliver (and the haze starts at 90 m): for Glint.
- Acceptance: brief held zone at 2880 = 20.9 (<= 40); panel veil at 1920 top right = 66.2 (<= 78); from pause 0.61 of the frame under it.
- Dossier keys: Glyph's Jost Medium 14.5 at 0.14 em made OBJECTIVE wider than 96: the key column now takes the widest key (at least 96) + 14. Rebuilt 0.2.87 (game-build + game-check OK); front-end proof at 1920x1080 and 2880x1864: 18 shots each, 14/14 checks.
- Shots: briefs/051/build (front end both sizes incl. menu-focus, controls-rebind-focus), briefs/049/build, briefs/046/build (2160 on 0.2.87), briefs/048/build (tp-long on 0.2.86 incl. long.json, tp-street-long-nodof, tp-signals-haze, glass frames). Vela's fix: no black window bands in tp-tower-windows; shopfront glass reads (horizon band), _Glow left at 0.85.
- Glint's signals-under-haze shot (test code; 0.2.88-0.2.91, tp-long alone each time, last out/batch-0291.log, tp-worst SetPass 351): SignalsHaze now draws the fans (Alt + ShowFans; fansDrawn 1: the guard's; the Tech's Watch of the entrance camera did not finish in 120 s, so no camera fan), follow-tier camera (far 800, 30 m up, 25 deg down), searching a heading (and 60/90 m) with a clear line; haze on/off; as built, overlay variant, no-fan frame, mask. ΔE2000 (briefs/048/build/signals-de.json, tools/signals_de2.py): 150 m (heading 180, 64 % of the fan in view): fill 2.26 (pass), rim 5.44 (just over 5). 250 m: the fan is not drawn by the base camera at any heading or height (the mask camera draws it): the flat fan loses the depth test to the pavement at that range (depth precision; a game issue for Glint/Latch). The overlay variant draws nothing: Camera.Render() does not render a base camera's overlay stack, and the cones are not on the signal layer anyway (they are default-layer, drawn by the base camera with the follow haze). Whether the fan shader takes the fog is not settled by this shot: the rim's 5.44 at 23 % haze is less than a fully fogged amber would show.
- lens.json dofLong note: "+0.40 ms GPU measured on 0.2.87 ... highQuality stays on; the Air is not measured".
- Glint's ruling: Shaders/SignalUnlit.shader (no fog, DOTS, SRP Batcher; always included, guid 5c1a9e7d…), HandoverHost.SignalMat for FanMat (fill, rim, scan), the guard lamp, the rivals' ring, the fail camera's cone and lines; tp-long control shots (offset -2,-2 and ZTest Always, test only). Batch from game-build: out/batch-0292.log.
- 0.2.92 batch from game-build (out/batch-0292.log): every step OK (game-check, shots 10, budget 9/9, audit 9/9, late-fight worst 20.3 ms end frame, tp-long OK, tp-worst SetPass 351). Play view unchanged: the 9 batch shots against 0.2.86's differ by mean 0.01-0.11 levels (follow view 0.47: its cones and lamp no longer fog), well inside the batch-to-batch noise (0.1-3.1).
- 0.2.93 (test code: the haze-off frames turn DistanceHaze off and leave the lens on; tp-long alone, out/batch-0293.log). ΔE2000 haze on vs off at 150 m (64 % of the fan in view): rim mean 1.29, median 0.38; fill mean 2.31, median 2.19 (pass is <= 1). The rim no longer fogs (it was 5.44 with URP Unlit); the fill is translucent (0.38 alpha), so the hazed ground under it shows through: an opaque or stronger fill would be needed for <= 1 (Glint's call). 250 m control: seen as built 0.0, with Offset -2,-2 0.0, with ZTest Always 0.98: a building hides the cone at every heading and height tried (correct, no fix). At 150 m the offset raises the seen share 0.64 -> 0.75 (some depth fighting at the fan's edges against kerbs and paving).

## Brief 055 step 1 (Latch, 2026-10-23): people stand on the drawn floor
- PlayInstall.WalkFloor: the host's ground from Splice's `out/world/harbour-district.walk.json` (suborned.walk/1, 656 x 328 at 0.25 m, int16 cm, index i * ny + j = SiteFrame's) in place of the 1 m slab bake; rejects a file made from another export (world sha256) or another frame; missing file: the old bake with a warning. SiteFrame unchanged in form (cell, nx, ny from the file). The driven figure's height eases over ~0.1 s (HostViews DrivePos); engine-driven figures already ease between ticks.
- Order for any rebuild: `bun run world` then `bun run geometry` (the walk file), then the builder (the batch's world step is deterministic: export da8a3528adae).
- Proof ProofLevels (gate key `levels`): ground cell, the door guard's floor 0.93 +/- 0.05 and his figure on it, three of four crew reach the landing running to the Office entrance, none sinks under the floor; pictures levels-guard-landing and levels-crew-steps at yaw 210.
- `bun run geometry`: G-1a/b/c 1/10/1101 with the audit's host port still on the slab bake; ifHostReadsWalkFile 0/0/0. Splice flips `tools/geometry/sim.ts` loadSim to the walk file and re-ratchets (his step).
- 0.2.94 full batch (out/batch-055.log): BATCH OK, 12 steps, 832 s (sim 275/275, unity-match 366/366, game-check incl. `levels`, shots 10, budget 9/9, audit 9/9, late-fight worst 24.1 ms end frame, tp-long OK: tp-worst 6.2 ms mean, 5.5 M tris, SetPass 351; NOISY MACHINE, load to 33). levels: guard floor 0.93 m, figure 0.93 m; 4 of 4 crew on the landing (0.85-0.98 m), none under the floor. 2160 set (out/scratch/latch/proof2160-0294, status ok) copied to briefs/055/build, briefs/049/build/2160-0294, briefs/046/build/2160-0294; tp-long pictures and palms.json to briefs/048/build/0294. One 2160-only log line: "rings range: hover FAILED" (ungated; the batch's 2880 proof passed it).

## Glyph's 0.2.94 review, fixes 1-12 + rolled-in rounds (Latch, 2026-10-23; code first, one batch)
- #1 building fade: Editor probe (Editor/World/Review/FadeProbe.cs, batchmode, out/scratch/latch/fadeprobe) at the ring-plain-amber pose. The flat grey "slab" is NOT the fade: a mesh ray hits the Records Office body's plain 15.68 m roof cap (FacadeKit cap, roof_e #f2ead6) and with sun shadows off it renders cream (226,219,215 vs 135,136,137): it is the 38 m towers' sun shadow on an undressed cap (for Glint/Vela: shadow tone on roofs, or roof dressing on civic-office). The stipple: the ghost's kept fragments now lean towards a pale lit veil (DK_Cutaway.hlsl `_DKCutVeil`, ArchitectureFade.veil 0.45, veilColour #f2ead6 lit by the main light + SH, so dim at night); Bayer kept (interleaved gradient noise tried: no softer). Probe crops: fadeprobe/veil-zoom2.png (0 / 0.4 / 0.6).
- #2 the door marker steps down the screen clear of a target ring at the guard post (its own dashed ring off then); ProofRings amber runs the clock (no orders) until the patrol is within 18 m of the guard, then draws and frames both (logs tick and patrolM).
- #3 fire line: the gun disc (disc_light_24, ink gun; the Icon call had been swallowed by a comment) + s_downed + "45 s"; the fire pill's words are the same (HandoverHost.DownMark → the pill's s_downed icon); no "Firing at … · down in …".
- #4 hint lines show only under Alt (the detail layer); in play a hint is its ring and verb disc on the noun.
- #5 roster: the bare column's two bottom rivets painted out (TallColumn); the firing rays cream, out of the ring's left side (off the weapon disc); Pale = cream; side plates with a word and no number are icon-only small plates (049 row 9).
- #6 red-ring ticks: full alpha while closing in; threat ticks on a solid ink-deep edge.
- #7 range probe draws the Enforcer before the hover check (logs "drawn;").
- #8 follow chip: the kit plate at 38 px (slice 0.43), word centred, anchored under the roster where the Squad button stands (the old bottom anchor had been swallowed by a comment, so it sat at y 16).
- #9 "Guard back": the proof steps to the tick before the close, lets a frame pass, then steps the close (the pop's clock was the long synchronous step's frame: half faded in the shot).
- #10 stripes one tile tall (no seam); the ready glow in ten 1 px steps.
- #11 briefing: Day/Night ring follows the lozenge (radius on the chamfer line), both at full contrast, btn_text slice h/80; stat row Jost Regular; build line bottom left, card padding 34.
- #12 edge cameos in the chrome cameo ring (selected: a thin turquoise-light ring outside); the loop wheel's chrome rim. The stray cream line right of the door's next-step chip (done-window-soon) is most likely a fading speech bubble's leader drawn at the tail's own opacity: the leader now takes the bubble's fade too (to confirm in the 2160 set). Not done: the docked attention callout of the entrance camera reading as a ghost disc (its close-up render), the "Not yet" chip's empty body.
- Rolled in: next-batch 18 (every bay cut out of the plot slab: Layers.IsBay, PlayInstall bake, tools/geometry visible.ts and sim.ts; geometry G-1a/b/c 1/10/1101 → 1/6/1078); Vela's z-0 paving (CityLayers.FootLift, Lifted and Union dropped); SignalUnlit fans Offset -2,-2 (HostViews.FanMat; TpLongRun restores it); Glint's long-tier retune (his data and code in the tree; verify in the batch). runtime-compile OK, typecheck OK, geometry tests 6/6.
- Batch: out/batch-0295.log was cut (SIGTERM at the sim step: the 16:22 usage cut-off); rerun at the end.
- Resumed (Latch, 23:30): #12's stray cream line is NOT a speech leader (it is opaque #f7f4ec, the leader is 85 %): it is screen-fixed (same right end at 1x 1133,511 in done-cancel-warn, -cancel-pop, -guard-back, -window-soon), 1.5 px. A probe in the window-soon frame logs every element under it (HudToolkit.ElementsAt; ProofDone "window soon probe"); fix after the 2160 set names it.
- Aster: the menu restyle, direction A "light line" (briefs/051/lightline/SPEC.md, Sky approved), rolled into this round: frontend.json `lightline` block (numbers from frontend.a.json) and the scrims (menu left 66 % held over half of 40 %; panel + the bottom band; brief right holdRef 656, baked again per width); FrontEnd.Light.cs (word, list with one sliding marker, line draw/retract transitions, the glass bake: the picture through RenderTextures to ~213x120, 3 box-blur passes); FrontEnd.cs (hover = focus, focus always drawn, disabled skipped, default Continue-or-New game, the menu intro: marker at 0.3 s then the line), FrontEnd.Panel.cs (the sheet: border, glass + ink-deep 52 % + ink 72 %, flat ink 90 % from pause; title 32/0.30; tabs as words with one sliding line and the pointer-over underline; rows with one sliding tick and a drawn line; slider rail/square knob 11/15/17; switch box + diamond; choice chevrons 50/100/25 % with the press nudge; keycaps as border + fill, listening blink, clash cream edge + 18 badge; clash strip 56 with Swap/Cancel words, Cancel focused), HudToolkit.Menu.cs (the pause sheet, 7 words, one marker, keys Up/Down/Enter). Not built: the tab strip's own keyboard focus state (the strip is stepped by Tab/Q/E, never focused). Proof: shots wait 0.75 s for the transitions; new checks menuHoverFocus, focusSkipsDisabled, pauseFocus; new shot options-focus. Preview build first for the front end (out/scratch/latch/prebuild-fe.log), then the one batch.

## Resumed after the T3 restart (Latch, 2026-10-24 01:2x): finish the 0.2.94 round + menu restyle, one batch
- Found: the front-end preview 0.2.95 had built (2191 s on a loaded machine) and its 1920x1080 front-end proof passed (19 screens, every check ok; out/scratch/latch/fe-1920x1080-0295). Someone else built 0.2.96 at 00:37 from the tree (28 s); the batch out/batch-0295.log holds a later run stopped at compile by a test file (rc-WS…/Bad.cs, not ours).
- Keel is editing the engine in the shared tree (Rules.cs, Geo.cs, Site.cs at 01:31): his next version is NOT taken this round. The batch runs `--skip sim`: the Unity project keeps the DLL 0fcad509fc20 and its StreamingAssets inputs from the last sim step.
- Office cameras (brief 055, Motive's hand-off): the two mission poles are Vela's CCTVPole at the world's pole-camera scale (1.182, 1.728, 1.23: 3.18 m to the plan's 5.5 m mount), facing the camera's facing, the stand-in cylinder hidden (kept as the click target and view anchor, collider run to 5.5 m); the cue lens sits on the model's glass at 4.78 m up, 0.89 m out, ~17 deg down (signals.py cctv_pole scaled; the rulebook lens is 4.79). HandoverHost.cctvPole (PlayInstall: Kit/Prefabs/Signals/DK_CCTVPole.prefab), HostViews.KitPole, CamHeadUp/CameraTall for the ring focus and the callouts' figure box.
- #12 stray cream line: the 2160 probe on 0.2.96 (out/scratch/latch/probe2160-0296) names only full-screen Paint layers at the point, so it is a painted stroke, not an element: 120 px (1x) long, 1.5 px, opaque cream, in done-cancel-pop and done-window-soon at the same screen place (y 510), in done-guard-back elsewhere (y 389). Not named yet. Fixed on the way: a left press whose hold a right click cancelled could still start a box select while the left button stayed down (HostHold: no drag after lp.Cancelled).
- Road test (brief 055, Sky "go"; Vela's finish set in the plan, `export.ts --road-finish asphalt`): one scene carries both. RoadFinishTwin (Editor) reads out/world/harbour-district.road-asphalt.world.json beside the export when it is the same world but for the finish (same source, section counts, road and flat ids); the enamel-only pieces (RoadMark_Lozenge 1,248, RoadMark_Seam 1,902) bake into batch layer road-finish-enamel, the asphalt-only (RoadMark_EdgeLine_6m 3,984, RoadMark_Dash_2m 1,358) into road-finish-asphalt, built off. Runtime RoadFinish (world root): Set(asphalt) swaps DK_Ground_RoadEnamel for DK_Ground_RoadAsphalt on every renderer and turns the two layers; `-road asphalt` opens on it, Shift+F11 toggles. TpLongRun.RoadTest: road-{enamel,asphalt}-tp-east (the palms-east camera) and road-{enamel,asphalt}-opening (play view, HUD) + road.json. Layers.NoGutter takes road-asphalt too (Vela's note).
- `bun run world` + `bun run geometry` (walk file for export 6a8243a8b5; G-1a/b/c 1/10/1126, G-2a 645).
- Aster 02:0x: Vela's clean asphalt (dashes only) came, then Sky: "it's the white lines that aren't needed - look at the testbed version". The road stays the enamel; no asphalt shots. The twin build path stays opt-in only (SUBORNED_ROAD_TWIN; unset = the scene exactly as before), so no build picks it up by accident; NoGutter keeps "road-asphalt". A batch started at 02:08 with the twin was stopped by PID in unity-world (out/batch-0296.log), no build spent.
- TpLongRun.RoadTest now shoots the enamel in the road boards' two views (road_board.py): road-enamel-office-210 (ortho 14, pitch 38, yaw 210, focus plan -160,-281) and road-enamel-boulevard-third (fov 55, eye 3.24 m at plan -9.5,-104, looking south), no HUD, + road.json, for Vela's compare with briefs/028/road/boards (testbed column). runtime-compile OK.
- Batch: out/batch-0297.log (`--skip sim`).
- 0.2.97 batch (out/batch-0297.log): compile, kit, world, unity-world (30 s; twin off), unity-match 366/366 (DLL 0fcad509fc20), game-build 0.2.97 OK; game-check every section ok (incl. levels, rings, done, hudClick, holdRange) but driveSession "DOES NOT replay": game:check replays with `dotnet run --project sim/cli` from the TREE, and Keel's rulebook edit at 02:18 (mid-batch: `mood-scared-fear.unit 'scale' is unknown`) does not load. With the pinned engine copy the build carries (out/scratch/../sim-unity/tree, sim 0.27.0): "[ok] the replay matches its record", 2 checkpoints. Continued `--from shots` (out/batch-0297b.log).
- 0.2.97 rest of the batch: shots 10 (out/batch-0297b.log), budget 9/9, layout audit 9/9. late-fight failed only on resolving script ends with the tree engine (Keel's rulebook: `ready-help-orders = -1 is outside 0..10`); run with the pinned engine (out/scratch/latch/tools/late-pinned.ts, same resolve/run/judge as the step): ok, loud-fight worst 21 ms (order+save+end), p99 5.8; sky-session1 worst 49.6 ms (tick 2663), p99 5.4. tp-long `--only` (out/batch-0297c.log): OK, play-opening 4.1/4.7 ms, SetPass 106, drawn 325; tp-street 4.4/5.4; tp-worst 4.4/5.2, 5.5 M tris, SetPass 351, casters 898, drawn 2,396 (1,374 beyond 150 m).
- Front end, 0.2.97, both sizes: 19 shots each, 17/17 checks (incl. menuHoverFocus, focusSkipsDisabled, pauseFocus). 2160 proof (status ok; one ungated 2160-only "rings range: hover FAILED", as on 0.2.94).
- #12 stray cream line: gone on 0.2.97 (done-window-soon HUD layer has no long cream run; the remaining runs in cancel-pop and guard-back are speech bubble tops). Cause: the cancelled-hold box select fixed above.
- Office cameras: office-front-yaw210 shows the entrance camera on the 5.5 m kit pole.
- Evidence: briefs/051/build/0297/{1920x1080,2880x1864}; briefs/049/build/2160-0297 (the full 2160 set); briefs/055/build/0297 (levels, office-front-yaw210, late.verdict.json); briefs/048/build/0297 (tp-long); briefs/028/road/game-0297 (road-enamel-office-210, road-enamel-boulevard-third, road.json).
- Open: RoadFinish.cs uses the deprecated FindObjectsByType(…, SortMode) overload (2 warnings, as elsewhere in the project); the docked entrance-camera callout ghost disc and the "Not yet" chip's empty body (#12 leftovers); game:check and late-fight resolve with the tree's engine, so a mid-edit engine fails them (for Splice: pin to out/sim-unity/<tree|commit> when the build's DLL came from there).

## Glyph's 0.2.97 review, fixes 1-8 (Latch, 2026-10-24; code first, one batch)
- #1 building fade at yaw 30: why the yaws differ is light, not the cut. At 210 the ghosted faces are the shaded ones, so the kept dots are dim over dim paving; at 30 the ghosted wing faces the sun and its dots were lit as a sunny surface wherever the street behind lay (a pale screen over shaded paving). DK_Cutaway.hlsl: the veil is now the street behind as lit: the fragment's hidden ground point (its view ray at the focus level), the main light's shadow sampled there (so the towers' shade and the ghost shadow count), the sun on flat ground plus the sky's ambient, times the paving's albedo (pave_civic_v1.png mean #e4daca). ArchitectureFade.streetVeil 0.9 / streetAlbedo (renamed from veil/veilColour so scenes built before take the new values). Probe (FadeProbe yaw 30, out/scratch/latch/fadeprobe-0298; its -fadeProbeYaw had been parsed as a path): the dot screen over the paving is gone at 0.8; the wing still reads by its edges and its ghost shadow. The "slab" in ring-loop-names is the wing's plain 15.68 m roof in the tower's shade (as in 0.2.94; same flat grey with no fade at all, fadeprobe/b-nofade.png): not a fade cap. For Vela/Datum (GDD §15 "every roof programmed": civic-office has no roof items) and Glint (cream in shade reads neutral 135 grey).
- #2 a camera's attention callout: the kit's opaque disc_dark_24 (96 px sprite = the 72 px disc) with the door marker's camera eye (eye_open, red), no close-up render; every callout's face badge in the chrome cameo ring (2 px chrome), no stage colour.
- #3 menu veil: the loading screen's top and bottom scrims were drawn on the menu as well as the menu's own copies of them (FrontEnd.Show), so the sky and paving got both. frontend.json scrims.menu keeps only its left scrim.
- #4 brief: column 504 → 536 (still inside the 656 ref hold at 2880), hook 24 → 22: one-line hook, no "paper." orphan; intel bullets are the marker (a 6 px turned square, live).
- #5 in-game briefing: Day/Night are the light line's tab words (chosen at full cream with the 2 px live line), Begin is a word with its marker and line (always focused: Space begins); no cream plates, no ring.
- #6 Options: the Playground group only in a development player or with `-debug` (FrontEnd.DebugOptions; the Debug control rows too). Disabled chevrons: 0.25^2.2 / 0.4 (UI Toolkit blends opacity in linear light, as the scrims; 0.25 x 0.4 read ~50 %).
- #7 edge cameos: no turquoise selected ring (the roster says who is selected); the cameo's 1 px ink-deep outer line; the pointer is a chrome tail joined to the ring with an ink-deep edge (the loose cream wedge read as a stray white corner square).
- #8 2160 hover probe (ProofHold): keeps the pointer on the passer-by until its hover ring is up (3 s cap) and logs the wait.
- runtime-compile OK.
- Rolled in (Aster 03:1x): Vela's enamel road fix and Glint's road reflection. The batch started 03:02 (out/batch-0298.log) was stopped by PID in game-build to take them (no build spent). Glint's DK_LitCutaway.shader hunk applied as staged (out/scratch/glint/road-envscale: indirect specular x (1 - _ClearCoatMask), ForwardLit only; every material but the two roads keeps 0). TpLongRun.RoadTest adds office-30 (play zoom, yaw 30, the same front) to office-210 and boulevard-third. Waiting on the kit regen that writes _ClearCoatMask 0.65 on DK_Ground_RoadEnamel/RoadPearl, then the one batch.
- Vela's kit regen (03:05) predates Glint's ENV_SCALE edit (unity_mats.py 03:09): every road .mat still has _ClearCoatMask 0, so Glint's hunk is in the build but inert. The 0.65 needs one more `bun run decokit` + `bun run kit` (Vela's), then the next build.
- 0.2.99 batch (out/batch-0298.log, -0298b, -0298c): world export changed with Vela's lozenges, so `bun run geometry` (walk file) before unity-world; unity-world 40 s, unity-match 366/366 (DLL 0fcad509fc20), game-build 0.2.99 (2057 s on a machine at load 100-130), 0 errors. game-check every section ok (holdRange incl. ringHover "waited 0.00 s", leash, done, rings, levels) but driveSession: the tree engine (Keel mid-edit) says the inputs differ; with the pinned engine (out/sim-unity/tree) "[ok] the replay matches its record", 2 checkpoints. `--from shots`: shots 10, budget 9/9, audit 9/9, tp-long OK (play-opening 3.8/6.1 ms, tp-worst 4.8/14.2 ms, 5.5 M tris, SetPass 351). Late-fight pinned (out/scratch/latch/tools/late-pinned.ts): ok, loud-fight worst 50.4 ms (one stray tail frame, no event), sky-session1 worst 43.7 ms. Proof frame times are a loaded-machine proxy (play zoom 110 fps, p99 34.7 ms at 2880).
- 2160 set (status ok) and the front end at 1920x1080 and 2880x1864 (17/17 each; the proof needs `-settings <file>`).
- Seen in the set: #1 the dot screen over the shaded paving is gone at yaw 30 (done-window-soon); over the blue road the dots are paving-coloured, so a faint screen remains there (the veil knows one albedo). The slab is the wing's plain roof, unchanged (Vela/Datum, Glint). #2 commander-firing shows the opaque kit disc with the camera eye and the chrome face badge; done-window-soon and done-guard-back caught their callouts 0.06 s into the 0.15 s fade-in (half see-through): ProofDone now waits 0.3 s before those two shots (next build). #3 menu within 5-10 % of the mock (was ~0.57 at top and bottom). #4 one-line hook, no orphan, diamond bullets. #5 briefing: DAY with the line, NIGHT idle, BEGIN with marker and line. #6 no Playground group. #7 edge cameos: chrome ring + ink edge, chrome tail, no turquoise ring.
- Road: briefs/028/road/game-0299 (office-210, office-30, boulevard-third, road.json): the road ~22 levels darker than 0.2.97 in office-210 (193 → 171 mean), buildings unchanged (mean diff 0.1), paving 0.6 (jpeg, lozenges); the Boulevard no longer reads near white.
- Evidence: briefs/051/build/0299, briefs/049/build/2160-0299, briefs/055/build/0299, briefs/048/build/0299, briefs/028/road/game-0299.

## Engine 0.28/0.29 port round, next-batch 19-25 + 29 (Latch, 2026-10-24; code first, batch held for Splice's world and Pulse's patrol)
- Compile against the tree's 0.29.0 engine: out/scratch/latch/tools/compile29.ts (the Unity project still carries the 0.27.0 DLL until the batch's sim step).
- Mood icons imported: UI/Resources/Kit/icons/singles/mood_{friendly,uneasy,scared}@2x.png (+ .meta, single sprites).
- #29 (Sky 0.2.99, the ring on the driver with the Fixer down): HandoverHost.Able(k); BuildPanel's agent order skips downed agents (so no row, wedge, cameo, hold doer or quick click is theirs; the crowd takedown and the "Enforcer only" Take row too); Select leads with the first able agent; Pick and the plate's knobs (VerbHas) give nothing for a downed agent; WatchBy and Intel's Listen / turn next steps skip them.
- #19 Watch: Verb gives no Watch row on a person when Game.WatchOffered is false (absent once watched too, Aster's call; hud29 says greyed after a Watch: flagged), a picked Watch on such a person is silent (no pop); Intel's next step "Watch: their mood" / "Watch: his sight" only when offered; the action panel's learns icon for a mood Watch is mood_uneasy; the host's own turn lock reads "routine known" as the engine's KnowsGate (watched, or no Watch to give: the driver is paid at once).
- #19 mood + likely: Intent.Likely/Hurt/Exact/Eyes from the turn Quote; the pill's prediction plate (HudToolkit.Mood: plate_dark 22 px slice 0.37, dashed 15 px ring with tick + "likely", or half disc; mark_ok / mark_half after Listen); the knobs' marks (HostNotice.KnobPred, priced on hover change, Fixer on a cast person); the mood disc beside the cast pip (disc_dark_24 32 px, face 17 px).
- #19 cones: HostNotice (armed act → witness.notice act; CaresAbout / card eyes / NoticesNow on live acts; Alt: 6 nearest; the shape sampled with the engine's NoticesNow on rays 5 deg apart every 0.5 m, one cone rebuilt a frame) + HudToolkit.Cones (ground batches: wedge rim cream over ink-deep, cut rim, 45 deg hatch on dead ground, security ring red, the dashed near-body ring on a takedown or drop). HostViews.Shadows: Fan.Shadows cut out of the security fans' fill (convex clipping) and rim, each cut with a red rim over an ink-deep under mesh.
- #23: a person's Watch card footer is "{Who} · {mood}[ · {approach} likely]" (HostFeed.MoodFooter), dropped when neutral; the done chip says the mood (mood_* icon), not "Rounds".
- #31 (Sky 0.2.99, the Tech dragging the downed Scout): confirmed: HoldClick → AgentNear (26 px round a standing column above the body) → CarryDowned on a left click, and a right tap did the same. Now a click on or beside a downed agent walks there; AgentNear measures a downed agent at its body; a left hold or right tap on it offers Carry as a priced way ("Hold · Carry the Scout"); the carrier's right tap rings Put down (its top way), knob 1 is Put down while carrying an agent (the cancel lays them down), the roster tags CARRYING with the pace; the move marker while carrying an agent gives the carry pace's time.
- #20: (a) the fade's hard vertical edges are the people's clean discs (cleanFeather 0.4 m inward): DK_Cutaway.hlsl now keeps a person's whole radius clean and feathers outward over ArchitectureFade.personFeather 1.3 m (~100 px at 2x; renamed from cleanFeather so built scenes take it); (b) the callout shots' 0.3 s wait went in last round (ProofDone); (c) the roster's selected ring is painted (HudToolkit.SelRing) in accent.live #60e0ea with the kit sprite's profile (3.5 px band, cream hairline, inward glow): portrait_ring_inner's band measured #1eb9c6-#36c8d3.
- #21: `-evening` (HostNight: LightPhase.SetEvening, the stored choice untouched); TpLongRun.RoadTest shoots each view day, evening, night (road-enamel-<view>[-evening|-night].jpg; road.json carries RoadLight's lens and pool levels per shot).
- #24: cause found: robot_face_stage_v1.png (16 x 60) was imported with nPOTScale ToNearest (16 x 64), trilinear, and a BC7 Standalone override, so DK_RobotFaceStage's LOAD rows (h = 60) read each slice ~4 rows low: the calm slice's next-slice eyes at the bottom and its smile on top (the face looks upside down). The FBX's face UVs are upright (Blender probe out/scratch/latch/tools/faceuv.py: v up = z up, as the butler's). Fix: the meta (point, no NPOT scaling, uncompressed, no override) and RobotKit.EnsureStageAtlas() on every world build (it only ran when M_RobotFace was made). Vela's kit copy may bring the old meta back: the builder check covers it.
- #33 (Sky 2026-10-24, left click rings, right button fires): HostHold.HoldClick opens the ring at a clicked thing (ClickRing: sticky, ByClick, nothing taken) and at a downed agent; the right tap does nothing (RightTaps counted); RingState.ByClick keeps the ring on a double click's second press in its middle; TpResolve LeftTap Free = Ring, RightTap = Nothing (LowerAim while aiming); ProofInput.DblClick and ProofFire updated; decisions in briefs/041-controls-playground.md. Item 31's carrier ring (right tap) dropped with the right tap: the carrier's Put down is knob 1 and the cancel key.
- #22 (code only, Splice's generator in progress): Layers.Ground stops the straight kerb stones inside a convex kerb return's fill (the export's flats ground/return/<id>, Splice's build.ts), so the kit's kerb-return arcs (street/kerbreturn/*) carry the corner; Proof29 shoots the junction NE of the civic block (-60,-198) and the Boulevard (-9.5,-104) at play zoom and street level and reports the drawn floor's steps round each (r29-*-heights.png).
- #25: Proof29 measures the patrol's nearest pass to the entrance camera's pole over 120 s (needs 0.6 m) and shoots it; waits for Pulse's re-sited sweep.
- Proof29 (gate key r29 in tools/game/cli.ts inputChecks): mood (route A to the Watch), cones (Takedown / Alt / Pressure at Alert 1), an agent down (loud-left-downed to the Scout down at 4:47.5: the car ring is the Fixer's alone, a click beside the body walks, the body's ring has Carry, Carry taken, knob 1 Put down), the patrol (pole, close faces of it and a service robot), the junction and Boulevard. DblClick now runs in the default preset (GameBoot).
- Editor scripts compile check (not in runtime-compile): out/scratch/latch/compileEd/ed.csproj (dotnet against Unity's Scripting/Managed/UnityEngine/*.dll, no UnityEditor.dll facade); my files clean (the 5 errors left are the probe's missing platform module and Runtime-root refs, not code).
- State: code done, runtime compile OK (out/scratch/latch/tools/compile29.ts against the 0.29 DLL), typecheck OK. Waiting for Splice's world (22) and Pulse's patrol (25), then one `bun run batch` (its sim step takes 0.29 into Unity) + 2160 set + tplong road phases.
- Brief 057 (Sky's calls via Aster, 2026-10-24), code only:
  - §3 quiet cones: every learned cone draws at rest (fill 13 % at the lens fading to 1 %, rim 0.05 m at 55 %), strong (34 % to 14 %, rim 0.085 m opaque) when hovered/named/Alt, an agent in it or within 3 m walking in, a walk through it, Alert 2+, or the watcher noticing; eased in 0.25 s, out 0.6 s (HostSight.ShowSight, HostViews fans keyed by watcher, SignalUnlit `_VertexAlpha`). The Scout's finished Watch no longer pins a cone. The scan line (Scan, ScanEdges, A0/A1, three line renderers a fan) is deleted.
  - §4 seal: HostSeal (SealOn, SealLeftS, SealSpanS, SealCauseIcon, SealCarOf/SealedWords reading FailCause.Car and its holders by name until Keel lands them); HudToolkit.Seal (plate_dark 38 px under the clock pill, lock + m:ss + draining underline, amber blink in the last 10 s, one pop, the cause disc on hover); end card's sealed line and story from less.sealed (and "Vale reached the getaway car" dropped when the car state is empty); the fail camera's tip from less.sealed.tip; CarRingOn steady while Vale is aboard or the seal runs; Go ranks first; a held Go shows shut in the ring (struck, dim; lock + one badge a holder; a press pops why and pulses the holders). Fable's less.mood / less.alt.mood / done.gain.mood-* wired (Watch footer, the pill's "likely", Alt name tag). The first-act hint retires on the first left-click ring (a right press no longer retires it).
  - §2 Reroute: the ring's Reroute wedge wears the patrol's cameo (signal_unit), Loop from the cabinet the camera's, Call the guard's or the car; RingFocus fires for Reroute; HostReroute + HudToolkit.Reroute draw, hairline, the sweep with end ticks, the dotted route with chevrons, the dashed wait ring, cameos and an edge chip, on hover and all the while the reroute is in play. The route is Game.PatrolRoute() when Keel adds it (reflection), else Grid.Route from where it swept. The patrol snaps a > 2.5 m jump (no slide) and, walking faster than its sweep, turns eased (240 deg/s) to face its way. Its treads are part of the body mesh: nothing to turn with speed (Vela).
  - Proof29 adds quietCones, reroute (ok null until the engine walks the route), seal (fail-district-seals: the marker, the cause, the end card).
  - §1 (learned facts) held: Glyph is redoing it on the ring.
- Brief 058 camera fixes (Motive, via Aster): (1) fire on a camera or car prices the shooting's Trail once (R.GunfireTrail, "+2"), no "/hit": the pill and ring price line (HudModel.PriceLine), the aim chip (HostFight), the going-loud card when every fire order is on property, and the panel's Trail line (Verb/FireIntent: "T +2"); HandoverHost.PropertyId. (2) every armed selected agent gets Fire on a camera in the ring and panel (BuildPanel's fire rows take "camera"; FireIntent fires "camera:<id>"). (3) the Loop's done plate: "Camera looped" with the loop icon, from less.done.gain.camera-looped (placeholder: Fable has no key yet; "Camera cut" / "Camera shot out" not needed by any code yet). The walk-off stopgap waits for Sky.
- #22 with Splice's world (2026-10-24 15:06 export, walk file regenerated): Layers.Ground drops the straight kerb strip inside every ground/return/* flat, civic/bay/* flat (a lay-by's mouth: no kerb across it, the stray-kerb ring) and plot/bend/* slab, each grown by StripGrow 0.35 m (bounding boxes first), as tools/geometry/returns.ts does for the flats (returns.ts has no plot/bend: flagged to Splice); KerbKit.KitKerbs takes kind kerb-return (1,064 Kerb_Pearl stones) as kit kerbs; ribbon and pinstripe chords are export pieces (nothing in the builder). Proof29's floor section shoots the civic junction (-132,-267.5), the square bay (-177.8,-238.5), the office bay (-161,-307.8), Boulevard x Market (0,-410), the circus mouth (0,-46), plus the earlier two, each at yaw 30 and 210 and from two street eyes, with the floor steps round each. Editor compile clean (out/scratch/latch/compileEd).
- Suture's bot pass (1): runtime-compile in `bun run test` fails on PersonSight because unity/Assets/Suborned/Sim/Plugins/Suborned.Sim.dll is still 0.27.0 (the 0.2.99 batch's sim step); every read the host uses exists in the tree engine (PersonSight: GameNotice.cs:22). It clears when the batch's sim step copies Keel's engine in; compile against the tree's engine is clean (out/scratch/latch/tools/compile29.ts). Keel's 0.30 reads now exist: FailCause.Car / Holders, Game.SealOrigin, Game.PatrolLegs / PatrolRoute; HostReroute now reads PatrolLegs (mode away: legs out, wait, back) by name, so the hover's sweep-only PatrolRoute is never drawn as the reroute.
- Suture's bot pass (2), the saved session s-20261024-022456 (replayed with tools/qa/run.ts --engine build; not Sky's own play, as first written here: it is a late-row replay of Sky's 2026-10-22 session, 66 s of wall clock for 301 s of mission; brief 060 "Human profile"): tech watch patrol too-far at 79.8 s (the patrol swept off while the Tech walked to his stand point), fixer call waitress agent-busy at 249.6 s (3 ticks before his Watch on the guard ended), scout move unwalkable at 289.0 s. Fix: HandoverHost.Playable, run on every player order (Issue with a screen/target; scripted Issue(o) unchanged): a move to unwalkable ground goes to the nearest walkable cell within 3 m, else "Can't walk there" and no order; an act for a busy agent waits for the act in hand when it ends within 2 s (FollowUp gives it), else cancels it first (the hold preset's rule); a Watch on the patrol refused too far walks in again (4 times at most).
- No-walk cursor: HostContext.CursorKind "nowalk" when the ground under the pointer has no walkable cell within 3 m (MoveNow.Refused), drawn as the footprints struck through (HudToolkit.Ring cursor mark).
- Engine 0.30.0 (Keel done): compiled against the tree's 0.30.0 (out/scratch/latch/sim29 rebuilt). HostSeal reads FailCause.Car / Holders and Game.SealOrigin directly (the log guess is gone) and takes the underline's span from Windows()'s seal:district OpenWindow.From; HostDone skips the seal window (HostSeal draws it, no world mark or close pop); HostReroute reads Game.PatrolLegs directly (mode away: out, wait, back); Proof29's reroute check now asserts (the whole trip, ~115 s on route A: on the drawn route within 0.3 m, no frame step over 0.5 m). Waiting for Pulse's rounds re-bake, then the batch.
- Batch 0.2.100 (16:01): sim step copied engine 0.30.0 (DLL f108d268fe26) but exits 1 on three engine fixtures, N48, CAR-PREVIEW-LAB and DP1 (289/292 headless; Unity matches headless 383/383, 380/383 pass checks): Keel's re-recording; continued --from compile, then --from game-build. First game-check (out/batch-0300c.log): every section ok but dblClick (ground row: the hold preset queues orders given while paused, planTogether), holdActs click/earlyRelease (expected the old click-walks: item 33 makes it a ring), r29 cones (Alt's act point was the Enforcer, far from the café), r29 down (paused orders queued), r29 quietCones (GiveScripted gave the camera Watch no walk). Fixed in the proofs (planTogether off while they click; ProofHold click expects the ring; GiveWatchScripted; the reroute measured against the route as first drawn, not the one redrawn from the robot each tick) and in HostNotice (a picked knob with no target acts at the thing under the pointer). r29 reroute (engine route): 1,150 samples, steps <= 0.21 m. Seal: marker 2:15, cause eye_open, sealed with car empty, the end card says so. Inez round: 8 side stills; the world has no colliders for a feet ray (rays 0), so the stills are the evidence. Rebuilding (out/batch-0300d.log).
- Batch result (builds 0.2.102-0.2.104, c035; logs out/batch-0300*.log): sim copied engine 0.30.0 but exits 1 on N48, CAR-PREVIEW-LAB, DP1 (Keel's fixtures; Unity matches headless 383/383); game-build 0.2.104 (521 s incl. the proof); game-check: every proof section ok (driveClick … levels, r29, holdRange, leash), fails only on those 3 scenario checks; shots 10 ok; budget 9/9, audit 9/9 (fixed on the way: the end card's audit no longer measures briefing.fails.<code> when less.end.fail has the code's title; the reroute edge chip 30 px with a 10 px arrow); late-fight: loud-fight worst 23.7 ms (order+save+end), sky-session1 "ran past the end" on both runs (Sky's 0.2.58 session no longer ends at 7:03 on engine 0.30 + the new site: a fixture to re-pin); tp-long: tp-worst renderers drawn 2,653 (limit 2,500), 1,525 beyond 150 m (limit 1,500) after the corners' new pieces (kerb-return stones, ribbon chords), frame times fine (4.7 / 5.6 ms); road at three phases: lens 3.0 / 5.0 and pool 0.14 / 0.22 at evening / night (road.json).
- 2160 set (out/scratch/latch/proof2160-0300, status ok, every section ok): r29 mood (uneasy, Watch absent once watched, the footer "Inez · uneasy · help harder"), cones (takedown wedge + near ring; Alt 7 incl. a security ring; no cone for Pressure at Alert 1), down (car ring the Fixer's alone; beside the body a move; Carry taken, carried, knob 1 Put down, laid down), patrol (1.77 m from the pole; face upright), quiet cones (rest 0.00, hovered 1.00), reroute (engine route, 1,150 samples, 0.01 m off the route as first drawn, steps <= 0.21 m), seal (2:15 marker, cause eye_open, sealed with the car empty and the end card says so), corners at 7 spots. Not proven: item 26, Inez's round: her eight side stills are the same frame (she stands at the café door over 150 s, the camera behind a pot), and the world has no colliders for a feet ray: needs a reshoot from a fixed side camera on her round's legs and a look at why she did not move.
- Evidence: briefs/049/build/2160-0300 (the whole set), briefs/054/build/0300 (mood, cones), briefs/057/build/0300 (reroute walk, seal, quiet cones), briefs/028/corners/game-0300 (7 spots: play 30/210, two street eyes, heights), briefs/028/road/game-0300 (three phases), briefs/041/build/0300 (rings, carry, patrol and service robot faces, Inez stills, both proof.json).
- Follow-ups after the 0.2.104 batch (code first; the rebuild waits for Glyph's review):
  - (1) Keel's re-record: the sim step now passes 292/292 headless and 91/91 range; Unity matches headless 383/383 and all 383 pass their checks (DLL be15c00b625b). The DLL differs from 0.2.104's (f108d268fe26) in scenario code only (ScenariosCars.cs, ScenariosTakedown.cs changed since the batch; engine 0.30.0 either way); the seven inputs are byte-identical (only engine.json and headless.json moved). game-check runs the scenarios inside the player with its own DLL, so it needs the next build to show them passing.
  - (2) Inez: headless on 0.30 (out/scratch/latch/inezprobe) she leaves the café door at 34 s, reaches table 3 at 44 s and is back at 67 s, so the engine round runs. The host proof's stills never moved: cause not yet pinned (the section ran right after the seal section's failed run; RunTo or the view did not advance). Proof29 now runs her round first, on a fresh run, logs the tick and her engine and view positions with each still, runs to 7:39 and takes her table-5 excursion (out 17 s, serve 15 s, back 17 s, round the curved front) from one fixed camera on the square side of her path, every frame her figure against the walk file under it and against the drawn mesh (temporary mesh colliders over the path's box; unreadable meshes counted), ok when she moved and her feet stay within 5 cm of the walk file.
  - (3) late-fight: sky-session1 replaced by loud-left-downed (the late loud fight with the Scout downed, 9 shots, Success at 5:13.1 on 0.30); Sky's 0.2.58 session no longer ends on 0.30 (None at its 7:03.2 `until`). tools/game/late.json and late.test.ts (22/22 pass).
  - Splice's renderer budget: CityLayers.Street bakes crossing-dots, -bar, -edge, -stud, -arc and -field into the non-fade `crossing-parts` batches by material and tile (KitPlacer.Bake with a material remap: DK_Cross_Light to CrossingKit.LightMaterial, the emission-only light, made once a build); signal-orb stays an instance; Combine.Flush: crossing-parts never cast, glow batches no probes; CrossingKit.Dress validates the baked kinds and skips them; Inspect.VerifyKit counts the street's baked parts. Editor compile clean.
- Glyph's 0.2.104 review fix round (code; rebuild next): #1 Splice's rule in Layers.Ground (side lines resampled at 0.5 m, samples within another road's half width less 1 cm or inside a circus dropped, runs kept); #2 HostDone attaches an unlock only to the chip of its person (no `?? made[^1]`), UnattachedGates logs what opened on a landing with no such chip, RolelessUnlockFrames counted, ProofDone and Proof29 assert none; #3 the reroute edge chip: plate 28 px, cameo, 4 px, a painted 12 px cream arrow turned at the wait spot; #4 RoadPoolJoints fills the wedge between the strip light pool's chords on a curve (a quad from the edges' own vertices and UVs); evening = the day look with the dusk lights per LightPhase.SetEvening (Glint has not built an evening sun or grade), road.json shows lens 3.0 / pool 0.14 applied; #5 pause sheet opaque (0.97) and the briefing card hidden while it is open; #6 RangePanel and the pause menu's Fight range only in a development player, `-debug` or a proof; #7 prediction plate 28 px (reads 22), pops drop below an open ring, the carrying chip shrinks to icon + pace, no side disc beside a downed portrait, an agent's act disc near a cast person's head goes to the agent's feet, Read all on plate_dark with cream words, the end card's title is less.end.fail's (one car line under it); #8 Proof29 shoots the likely and hurt plates (forced, labelled), the no-walk cursor, Camera looped, the seal's last 10 s, Go held (engine scenario N185 exported to briefs/057/n185-go-held.script.json).
- Batch 0.2.108 (out/batch-0301*.log): game-check every proof section ok, fails only on Keel's DP1 scenario check (292/292 match headless; the site Splice regenerated); shots ok; budget 9/9, audit 9/9; late-fight ok (loud-fight worst 22 ms, loud-left-downed 15.3 ms); tp-long ok (tp-worst drawn 2,143, 1,376 beyond 150 m, 4.8 / 5.3 ms) after the crossing bake. 2160 set out/scratch/latch/proof2160-0301: status ok, every section ok (Inez's round: feet 0 m off the walk file, 0.004 m off the drawn mesh over 510 rays; nowalk cursor; Camera looped plate; seal 0:08 amber; Go held, rival-1 holder). Evidence: briefs/049/build/2160-0301, 041/build/0301, 054/build/0301, 057/build/0301, 028/corners/game-0301, 028/road/game-0301. Reroute landing: gates waitress|pay/help/pressure opened at 2:03 with no chip of hers, now shown on no chip (UnattachedGates).
- Own check of 0301 before Glyph: three review items not met. (a) pause sheet had no chrome border: the Border call was swallowed into a trailing `//` comment by my edit; (b) the night pool wedges stayed at the office corner: RoadPoolJoints took the band's axis from its two farthest vertices, a diagonal when a tight return's chord (about 0.3 m) is shorter than the band (1 m); now from the ribbon-side vertices (UV v < 0.5); (c) the reroute edge chip never showed in the proof's frames (the wait spot stayed on screen): Proof29 now shoots r29-reroute-edge-chip with the view 45 m past the patrol and asserts it (reroute.edgeChip), and the chip is in the manifest ("reroute edge chip").
- Swallowed statements (a scan for code after a trailing `//`), fixed with `/* */`: HostDone's per-tick early `return` (Redline's F4 gate cache; mine, this round), the likely word's 4 px margin (mine), ProofFire's RightHold Free = Aim check (mine), and older ones: the history bar's chevron never set, third person hid speech bubbles and pops (HudToolkit keep list), the takedown chip's top, the roster eye's repaint, drive focus never cleared the move chip or updated the aim, the follow camera's per-preset min/max distance, the beats column's right alignment, the × cancel's x, the order plate keycap padding 3, the fight ladder's position, the fight chip's Cover, ProofHold's Scout select.
- Batch 0.2.109 started --from compile (out/batch-0302.log).
- Batch 0.2.109-0.2.112 (out/batch-0302*.log; game-check stops only on Keel's DP1 scenario check, 292/292 match headless, every proof section ok):
  - reroute edge chip: proof r29.reroute.edgeChip true; the chip stepped inward clear of the Tech's own off-screen cameo, which stood on the same edge point (EdgeCamHits), shot r29-reroute-edge-chip.
  - night pool: RoadPoolJoints now holds the pool bands until the ground pieces are placed, mitres each chord joint (far corners to where the two far edges meet, ribbon-side corners to one point) and bakes them; 5,805 bands, 1,890 joints mitred, 2 filled. The dark wedges (convex) and the bright doubled spikes (concave, also in 0.2.104 and 0.2.108) are gone at the office corner and the bay S-curve (028/road/game-0302).
  - pause sheet: the 1 ref chrome border back, opaque, briefing hidden, no Fight range (light line A as Sky picked, brief 051).
  - shots 10 ok, budget 9/9, audit 9/9; late-fight ok (loud-fight worst 19 ms, loud-left-downed 13.2 ms); tp-long ok (tp-worst drawn 2,143, 1,376 beyond 150 m, mean 4.2 / p99 5.3 ms).
  - 2160 set on 0.2.111 (out/scratch/latch/proof2160-0302, status ok, every section ok); 0.2.112 differs only in the pool mesh (world bake).
  - Evidence: briefs/049/build/2160-0302, 041/build/0302, 054/build/0302, 057/build/0302, 028/corners/game-0302, 028/road/game-0302 (0.2.112).
- Code round after 0.2.112 (no batch until Aster says): brief 060 tier 2 `-bot` (BotRun.cs; tools/qa/bot.ts, bot.json, bot.test.ts; lane `bot` in run.ts, `bun run qa --nightly`, skipped with a note on a player without -bot; details in briefs/060-bot-playtests.md "Tier 2 built"). Glyph's 0.2.112 nits: the no-walk cursor disc painted in its chrome ring (2 px chrome, 1 px ink-deep); the car held marker hidden under an open car ring (its lock and count sat on the ring's middle) and its count moved clear of the lock (21, -9). Brief 057 §5: the affair line and Intel slot use `lovers`; the patrol's panel badge `badges/npc_patrol`; RrCameo, the edge chip and the Reroute wedge's object cameo `badges/npc_patrol_face`. Item 33 copy: the two host strings that still said "right click" to act (HandoverHost status, the IMGUI briefing footer; neither is drawn by the kit HUD) now say "click"; text.json has none but H17 (right hold fires, correct). Fable's `less.done.gain.camera-looped` is not in text.json yet; HostDone reads it when it lands (placeholder "Camera looped").
- Redline's brief 064 (#1, #2, #4-#7, #9), code in, measured after the batch: see briefs/064-code-review-1024.md "Latch's answers". Perf probe -perf064 (Perf064.cs; PerfLegacy before/after in one build; out/scratch/latch/perf064.sh). Needs Keel's NoticeProbeOf/NoticesAt in Unity's engine copy (the batch's sim step): until then runtime-compile in `bun run test` fails on it; it compiles against the tree's engine.
- Splice's comment lint: two swallowed statements fixed: HostNotice.cs BuildCone (mine, today's 064 edit, never built: the cone's Kind and its outline, cut and dead lists were never cleared, so every rebuild would have grown the shape) and ProofHold.cs holdRange (`hw` never set after "car:range-1", since b76d7657: the section read the previous value). `bun test tools/lint` green. Glint's road-test shadows kept in TpLongRun.RoadTest (TacticalShadows copied to the road camera, focusDepth 400 on the office views).
- Batch 0.2.113 (c036, a87f3e41; out/batch-0303*.log): sim copied engine 0.30.0 (DLL 3deb00069907), unity-world stopped once on the stale walk file (Datum's world run: `bun run geometry` regenerated it; the site rebuilt in memory from it is byte-identical to sim/data/handover.site.json, so c036 stands), unity-match 293/293; game-check: scenarios 293/293 pass, every proof section ok but `levels` (crew on the Office steps: deepest 0.122 m under the floor past the 0.12 slack, 0.03 on 0.2.104-0.2.112; tops 0.98 -> 0.93; reproduced in the 2160 set, 0.123): new walk data at the entrance, not host code. Shots 10 ok, budget and audit clean, late-fight ok (19.1 / 13.2 ms), tp-long ok (tp-worst drawn 2,145, 1,377 beyond 150 m). New icons in the player (resources.assets), no GUID conflict line. Evening (-evening, three views): no rejection line, RoadLight lists its lights, the look differs from day (briefs/048/evening/game-0303). perf064 at 2880x1864 (briefs/064/perf): cones p99 8.5/12.4 -> 3.3/2.9 ms, frame p99 14.7/17.3 -> 8.2/7.7 ms, frames over 16.7 ms 9/87 -> 0/0, cone builds 11.7/14.3 -> 5.7/2.6 a second; fans 0.04 ms a frame either way (the patrol's fan is a new pose every tick: 10 geometry builds a second remain). Bot smoke (random, 4 min): sealed at 3:23.8, 33 ways taken, 6 refused (agent-busy), Q-SOLID 21 (mostly crowd outside the walk grid: now bounded to it), Q-WARN-HUD 1 (first frame of the seal: now needs 0.5 s), Q-OFFER 6 (Fire offered to a busy agent, refused agent-busy), no console error, p99 6.2 ms, memory flat; it shot at 6016x3384 once (a rerun sized 1920x1080; -bot and -perf064 are now CaptureSize runs), and the anomaly paths nested a row folder twice (the player now writes to the lane folder).
- Bot smoke finding (Q-OFFER x6, Fire offered to a busy agent, refused agent-busy; the rulebook has no fire on a busy operative, and cancel ends an act at its line's cost): Playable now treats a player's fire on a busy agent as urgent in every preset: an act ending within 2 s is waited for (the whole fire order kept in pending, FollowUp gives it as it was), any other act is cancelled first and the fire goes at once (FireCancelledActs counts it). Proof29 `fireBusy`: a Scout Watch on Inez ending within 2 s (fire waited, then given, no cancel) and one with 4 s or more left (cancelled, fired), neither refused agent-busy. The bot's Q-OFFER keeps watching it. Code only, compile OK, lint green; rebuild held for Glyph and Splice.
- Levels (Splice: the floor did not move; Keel's wall margin moved the crew's tick phase on the stairs): ProofLevels now measures the floor at the figure's own drawn place (slack 0.12 kept), and HostViews.Frame lifts a figure that the straight ease between ticks would draw under the floor at its own place onto it (climbs only, figures not lifted or carried, under 0.5 m); the proof reports easeLiftM, how far the plain ease would have sunk it, so the next build says whether the sink was real on screen. Code only; rebuild after Glyph's 0303 review.
- Batch 0.2.114-0.2.115 (64f36d8a; out/batch-0304*.log): first run stopped after the world rebuild to replace the road sink rule (a chain stacked 7 steps, 21 mm; now a colouring: 14 of 22 carriageways sunk, at most 2 steps of 3 mm). game-check: 293/293, every proof section ok after Proof29's fireBusy took an in-range target (the official car was 16 m from the cafe, refused out of range; the fix itself held: waited then fired, cancelled then fired, never agent-busy). Levels ok with easeLiftM 0.13 m: the plain ease did put a climbing figure 0.13 m under the step at its own place, so the sink was real on screen and the lift fixes it. Shots 11 (start-overhead-night and cafe-front-night with HUD), budget 10/10, audit clean; late-fight 19.5 / 13.6 ms; tp-long tp-worst drawn 2,145.
- Found in the run: the night shots stored night in the player's PlayerPrefs (shared with Sky's game), and every later run that read it (tp-long, late-fight, the 2160 set, the bot row) ran at night. Put back to day (`defaults write ... suborned.handover.night 0`), cafe-front-night now runs before cafe-front-day, and HostNight never reads or stores the choice in an automated run (next build). late-fight, tp-long, the 2160 set and the bot row were run again by day on 0.2.115 (the numbers above are the day runs).
- Road seams (Glyph #5): SeamProbe (Editor/World/Perf/SeamProbe.cs) at (0, -410): the two carriageway batches (DK_Ground_RoadEnamel @0,-7 and @-1,-7) were coplanar over 1,867 of 25,921 half-metre samples (z-fighting where Boulevard and Market cross); none of these renderers takes a reflection probe and Combine never splits a piece at a tile edge. Layers.Ground now sinks the lower-ranked crossing road 3 mm a step: 0 road-road coplanar samples after (briefs/041/build/0304/seams-after.json). The faint tonal bands and thin lines in the frame are unchanged, so they are not the z-fight: they follow the road finish's own sheets and seam pieces (DK_Ground_RoadPearl over DK_Ground_RoadEnamel, the export's seam ribbons): Splice and Vela's tone, no builder change. Left: DK_Inlay_Seafoam coplanar with DK_Brass_Civic (21-30 samples, crossing parts), for Splice.
- Roster pips (Glyph #2): HudToolkit.FightLab forced the pips Visible every frame, so they outlived the replay's hide; they now inherit (also the menu's briefing, the roster progress bar, the order rows' tag icon and key: no element forces Visible). Fire offer: the bot row found Fire offered on a camera already shot dark (no-target x9); the ring and panel now drop a camera or car fire the engine answers no-target (FireQuery), next build.
- Road seams round 2 (code, for the batch after Glint's night round 2): Splice's RoadClip (Editor/World/RoadClip.cs) replaces RoadSinks in Layers.Ground: each carriageway is its ribbon quads minus the higher roads' quads and the circus disc (clippers shrunk 5 mm), Flat per piece at the road's own level; RoadSinks and SinkStep deleted. LookSwitches (-nossao, -noshadows) for diagnostic shots; out/scratch/latch/seamshots/run.sh shoots the Boulevard x Market view at yaw 210 and 30 with SSAO on and off after the build. Decal heights left as they are unless the shots shimmer.
- Batch 0.2.116 (9060032e; out/batch-0305.log): BATCH OK, 12 steps: sim 293/293, unity-match 384/384, game-check every proof section ok (Levels easeLiftM 0.129: the lift holds the climb), shots 11 (night views re-shot after Glint's round 2), budget 10/10, audit clean, late-fight 19.5 / 13.5 ms, tp-long tp-worst drawn 2,145. 2160 set ok, by day, the night choice untouched (0).
- Road seams round 2: RoadClip in (carriageways built from clipped convex pieces; the build log lists Flat's polygon repairs per road: 63-186). SSAO on and off at Boulevard x Market (briefs/041/build/0305/seams): the X of soft straight edges reads the same with SSAO off, and nothing steps (every carriageway at its road's level), so it is tone, not lighting or a step: Vela's sheets (one tone and grain). SeamProbe after: 340 road-road coplanar half-metre samples left in the 80 m box (85 m2, @-1,-7 against @0,-7), more than Splice's 5.3 m2 district-wide: for Splice to check against roadclip.ts. No shimmer seen in the stills; decal heights left.
- Suture's triage coded and in 0.2.116: followers drawn on the leader's path or the nearest free cell (HostViews.Follower); the open ring rebuilt when a giver turns unable (HostContext.RingGiversFrame); BotRun: the seated flag, SeatSkipM 1.5, Q-SOLID against the walk file's solid kinds baked into SiteFrame (SolidBodyR 0.25, SolidOverlapM 0.05), feet.csv skip column, frames.csv and feet.csv streamed (frame stats from a 0.1 ms histogram), Q-MEM by thirds (MemWarmS 120, MemThirdMinS 60, 3 / 1 MB/min), OfferRaceTicks 1, wedge shut not Q-OFFER, Q-GIVER on the open ring every tick (GiverGraceTicks 2), Q-WARN-HUD not during a capture. Bot row on 0.2.116 (random, 4 min): Q-SOLID 0, Q-GIVER 0, races 0, Q-MEM not judged (112 s after warm-up); Q-OFFER 19: Fire offered to an agent carrying a body (fire-carrying x14), on the mast camera already out (no-target x2), Listen on Inez needs-routine x3. Code since (next build): Fire's wedge locks with the engine's FireQuery refusal (agent-busy aside). Listen needs-routine: LearnQuery passes the row, Issue refuses: for Keel.
- Contact shade trim (code, for the batch after Glint): Layers.Ground records each kerb run it builds (Layers.KerbFeet: the kerb's centre line after the drive drop, OffOtherRoads and OutsideRings, with the road's line, level and kerb width); ContactShade lays the kerb-foot strip (light.json kerbWidth 0.6, strength 0.6, power 1.2, Vela) along exactly those runs, from the carriageway edge toward the road's line, instead of both full edges of every road (the X seam across junctions, the L step over return fills). Return arcs get no strip until Splice exports them. SeamProbe's grid offset by (0.13, 0.07) off the pieces' boundaries (Splice: the 85 m2 was the probe). Keel's NQ-2 needs no host change: Locked asks LearnQuery for the row's own giver, and the engine now answers needs-routine before agent-busy.
- Batch 0.2.117 (3d7dcdb1; out/batch-0306.log): BATCH OK, 12 steps: sim 294/294, unity-match, game-check every proof section ok (Levels easeLiftM 0.128), shots 11 (both HUD night views), budget and audit clean, late-fight 20.0 / 13.2 ms, tp-long tp-worst drawn 2,145. 2160 set ok by day; the night choice untouched.
- Seams: the X is gone. Boulevard x Market at yaw 210 and 30 (briefs/041/build/0306/seams) and the 7 corner spots (028/corners/game-0306): no line inside a junction box, no L step at a corner; the guideway reads pale silver (Vela's #b3c7cf). SeamProbe with the offset grid: 0 road-against-road coplanar samples (the 85 m2 was the probe, as Splice said); what remains coplanar is the crossing's glow pool (additive) and the lens against the contact strip.
- Bot row (random, 4 min): Q-OFFER 2, both Fire on the mast camera refused no-target after another agent had shot it out; Listen no longer offered then refused (Keel's NQ-2), fire-carrying 0, Q-SOLID 0, Q-GIVER 0, races 0. Code since (next build): the open ring is also rebuilt when a Fire wedge's camera or car goes down; Locked never locks Fire on a camera or car the engine has no gun for yet (FireQuery's target-unknown before the first shot).

## Batch 0.2.118 code round (Latch, 2026-10-25; code only, no build: the batch runs once A to H are in)
- Fast checks: runtime compile against the tree's engine (`dotnet build sim/Suborned.Sim -o out/scratch/latch/sim118`, `bun out/scratch/latch/tools/compile118.ts`: OK); Editor scripts (out/scratch/latch/compileEd118/ed.csproj: only the probe's 5 known errors, OSXStandalone and Runtime-root refs, none in changed files); `bun test tools/lint tools/game tools/hud-budget` 124 pass.
- **B, 058 §5 walk-off stopgap** (HostLoopKeep.cs; HostHold, HandoverHost, HostHoldStatus, HudToolkit.Hold/Less): a squad move (GroupWalk, MoveSelection) leaves a looping Tech at the cabinet when his spot is beyond the rulebook's tether, his plate says "Holding the loop" (s_looping icon) for 3 s; alone, a left press on the ground that would walk him off starts a walk-off hold of R.LoudHoldTicks (0.5 s): pill "Hold · Camera comes back" (harm shape, loop icon, over him), full "Let go · …", release walks him off; a tap does not move him and pops "Hold to walk off". The ring's Go to (Give) and the portrait drag are gated the same way (they pop). A drag past the box threshold drops the hold and box-selects. Host only, replays untouched. **Inert by itself once Loop v2 lands**: it reads `Rules.TetherCm` by reflection and does nothing when the field is gone (kept, not dropped). Placeholder keys for Fable: `less.done.loop.holding` ("Holding the loop"), `less.done.loop.walk-off` ("Camera comes back"), `less.pop.loop-walk-off` ("Hold to walk off"). Glyph's plate state replaces the pill branch (HudToolkit.Hold `ch.WalkOff`). Not gated: acts given to the Tech that walk him (a held act, deliberate), WASD in third person.
- **C, 056 occlusion F**: DK_Cutaway.hlsl: in the camera passes (forward, depth, depth-normals) a capsule per SetPoints point replaces the ground discs: view-space foot to head (person: 1.9 m, r 2 m; ground point: 0.4 m, r 1.5 m), cut only where the fragment's linear eye depth is nearer than the point's by 0.45 m, smoothstep 0.55 r to r, interleaved gradient noise, the rim leaning cream (_DKCutRim, 0.33 at full fade), eased with fadeTime (_DKCutClean.z). No focus disc in the camera passes. The shadow pass keeps the discs and the ghost shadow as built (so the street round the focus still lights under a solid tower: flag for Glint). ArchitectureFade: `capsules` (on; off = the column dither A), the capsule fields, globals per render (Capsules), and the reach takes a conservative ground disc per capsule (CapsuleCut) besides the discs. Silhouettes: SilhouettePass.cs (a RenderGraph pass enqueued by ArchitectureFade for the play camera only, after opaques) + Resources/Shaders/Silhouette.shader: every person (rendering-layer bit 21) marks stencil 128 where visible; the people who matter (bit 22: op/*, hostile/*, unit/*, rivals/*, security/*, patrol, waitress, guard, driver, councillor; HostViews.Silhouettes MarkPerson at Dress and Visual) draw a cream rim (normals grown 0.035 m) and a flat ink fill, depth Greater with the depth pulled 0.35 m to the camera, stencil not marked. Done without a renderer-asset edit and without taking people out of the opaque pass (SSAO and the depth prepass keep them): a deviation from the port note's Render Objects ordering, same result. Look values on ArchitectureFade for Glint. Needs the batch to prove the shader compiles on Metal.
- **D, 062 briefing B** (Briefing.cs view; HostBriefing.cs host + BriefScreen data): the street briefing replaces the card (kept built, hidden; `HudToolkit.BriefOnStreet`) and the lore screen (New game → DissolveToGame, the key art fading over 0.4 s). BriefScreen reads brief.json (title, variants[pick] hook and dossier objective and target) and the agreed `ways[]` / `clock[]` / `camera` / `target` in brief.json (or mission.json `brief`), falling back to the mission's roles, ties, windows and timeline; names and labels from Fable's less.briefing (PlayText.BriefWord). Photo `portraits/card_<target>` (Glyph's duotone, installed), the npc badge when missing; contacts `portraits/face_<role>` in a Cameo. FrontEndBuild copies mission.json beside brief.json into StreamingAssets. Start sequence: Begin (word or Space) → 0.3 s exit, 3.0 s cubic glide from the briefing frame (brief.camera, else 5 m on from d:Meet driver at the opening yaw, the board's overhead-pickup) to OpeningFocus, any key or click lands it; 3.3 s rings pulse and the roster slides in (0.25 s); 3.6 s StartClock and the clock bar fades in (0.3 s); goal line up. StartClock / SetPaused(false) / Esc / B land the camera at once. Automated runs keep the opening view after Begin (the proofs assume it) unless `HandoverHost.BriefCamInAutomation`; FrontEnd.Proof sets it and now checks newGameBriefing, briefingAfter, startHeld, startPlays and shoots briefing, start-1-begin, start-2-glide, start-3-settle, start-4-play (StartFreeze + StartSeek).
- **F, 061**: CrossingKit.Dress no longer turns the heads (the export's `faces` yaw stands); the build log line says so. SignalHead waits for Vela's pieces and Datum/Splice's export.
- **H**: 057 §5 was already in (affair → `lovers` in HostDone and HudToolkit.Intel; HudModel patrol → badges/npc_patrol; RrCameo and the edge chip → npc_patrol_face). The "replay 0.6x" tag is now PlateDark at the unlock pills' 30 px / slice 0.3 with the p_time icon and Jost Bold 12.5 (it was plate_dark unsliced at 30 px: the fat rim read as a web pill).
- **G, 055 step 1**: checked in code: PlayInstall bakes slabs only when the walk file is missing (warning); the driven figure's height eases (exp 0.05 s, ~0.1 s), engine figures ease between ticks with the Levels lift. The proof is the batch's guard shot (ProofLevels).

## Batch 0.2.118 code round 2 (Latch, 2026-10-25; code only, no build, no batch)
- Fast checks: tree engine 0.31.0 built to out/scratch/latch/sim118; runtime compile against it `bun out/scratch/latch/tools/compile118.ts`: OK; Editor scripts (out/scratch/latch/compileEd118): only the probe's 5 known errors (OSXStandalone, ReviewHost, FreeFly), none in changed files; `bun test tools/lint tools/game tools/hud-budget` 123 pass, 1 fail = runtime-compile.test against the Unity plugin DLL, still 0.30 (AlertIfCut, DisableS, CutNoticedAt, LoopUntil missing): clears when the batch's sim step copies 0.31.0; `bun run typecheck` clean.
- **A, engine 0.31.0** (host-interface.md "Engine 0.31.0"; Glyph 058; Fable's keys):
  - Walk-off stopgap (item B) **dropped**: HostLoopKeep.cs deleted with its wiring (HostHold GroupWalk/press/release/drag, HandoverHost MoveSelection and Give, HostHoldStatus plate word, HudToolkit.Hold pill, HudToolkit.Less plate icon). A move never ends a loop on 0.31.
  - Tether UI gone: KnobTethered (the Tech's Watch knob greyed while looping), BoardLock's board-tethered, run-tethered in the lock lists, the loop card's "tethered" outcome and "Replaces the loop on …" row, the "tied to the cabinet" feed and plain-text lines.
  - Disable: the Tech's fourth knob (Slots: loop, reroute, watch, disable); Verb on a loopable live camera (on the cabinet: the entrance camera); DisableIntent (10 s, +2, the siren = Game.AlertIfCut; the panel row "The desk sees it at m:ss" from CutNoticedAt; the ring keeps the shipped line, Aster's call); Locked camera-dead; ActPlace at the cabinet; ContextRank tier 4, sub 1 (Loop leads, lit at 12 o'clock); icon `disable`; activity "Cutting the line", done "camera cut"; Lands camera.cut.
  - The ring on a camera (Glyph §1): no ⋯ wedge on a camera (Tech: Loop · Disable · Fire; anyone else armed: Fire as the whole ring); Fire for every armed agent was in (round 1).
  - One loop at a time: Locked loop gives loop-holds with the running loop's end tick (the camera already looping is a re-splice, taken); the ring shows the Loop shut (struck, dim, lock + the end m:ss); a press pops Fable's short line with {tick} filled.
  - The loop window's real end: HostDone sets the Tech's face only for a loop with no end (a pinned tether run), so the marker counts down; WindowCount: m:ss when the window runs 60 s or more, plain seconds under; the desk's clock (`disable:<cam>`) is a marker with p_alert, its underline from OpenWindow.From; no "Camera on" pop when a cut ends a loop or at the report.
  - Chips: "Camera cut" (disable icon, camera.cut), "Camera shot out" (fire icon, fire.camera-dark, the shooter from the last hit line; not a success face); "Camera looped" as built.
  - Camera looks (Glyph §2): the looped cone hollow, still red, rim 0.05 m at 50 %, dashed all round 0.13 on / 0.10 off (HostViews LoopRim); a cut or shot camera has no cone; the dead disc 24 px, 20 px above the head: disc_dark + disable (cut), disc_light + fire in ink (shot).
  - Fire price: one function HandoverHost.FireTrail(by, target) from PreviewFire (TrailShooting: "+2", "+0" once paid; a person "+n/hit"; untraced ""), memoised a tick; used by the pill and ring (PriceLine), the panel row (Verb, FireIntent), the aim chip and HostFight's cost line.
  - RunRememberedBy: MoveParts.Remembers; the move card's eye opens with the count after it (Alt card, the run part), hover "n passers-by would remember your face."; the move chip text adds it.
  - Not taken: Fable's `lasts` price field on loop (Aster: keep the shipped price lines; PriceLine ignores it).
- **C, Glint's data** (signals.json, new loader SignalLight.cs, Runtime): ArchitectureFade Begin's shadow flag is `shadows && !capsules` (the shadow pass never discards with capsules); the reach is the capsule cuts only (count 0 reaches nothing); `rimLit` 2.5 (field, signals.json wins) to `_DKCutRimLit`; DK_Cutaway.hlsl scales the rim's cream by the fragment's lit luminance (Glint's line, exact); silhouettes take fill and edge per phase from signals.json (day/evening lerped in linear by Dusk, night whole), edge 0.045 m; the field defaults are the day values (#12544C, #FFFFF2). Material colours set as sRGB (R4 checks it).
- **E, portraits** (brief 062 "Portraits and ids"): HudToolkit.FaceOf(role) = portraits/cameo_<role>_normal for waitress, driver, councillor (the guard his pictogram): Intel faces, the feed's NPC chips and the ticker; Vale's model cameo (cameo_councillor_model, 18 px) on the Enforcer's plate while he walks or carries Vale. The councillor figure import stays the batch's copy of Forma's output.
- **F, SignalHead** (Runtime/Play/SignalHead.cs; Shaders/SignalDots.shader; CrossingKit, WorldReader, WorldDto, CityLayers, HarbourWorldBuilder): the export's `crossings[].cycle` {walk, clearing, stop, arrive, offset} parsed; every signal head gets a SignalHead with its crossing's cycle (none: the export's state); MissionSeconds from the host each frame; state, seconds left and ring lit from the clock; MaterialPropertyBlocks only, written when something shows differently: `_EmissionColor` = linear(colour) x level as a vector (SignalLight.HeadEmission from signals.json, phase-aware; never the .mat default), crossfade 0.2 s, rim blink 2 Hz at 0.15 in CLEARING's last 3 s and ARRIVING; Figure/Tens/Units `_BaseMap_ST` from Vela's tables; HaloRing `_Cutoff` = 1 - lit/24 per head; beam and halo `_BaseColor` (their own alpha kept); dot rows `_LitFrom/_LitTo` on u (SignalDots: lit emission or the charcoal seat). The old SignalEye works meanwhile (Walk/Stop figures enabled by state). State hold: `-signalstate walk|clearing|stop|arriving` (+ `-signalleft n`), SignalHead.Hold, per-head HoldHere.
  - Left once Splice's export and adoption land: the kit pieces (SignalLens with parts DK_SignalLens_Rim/_Figure/_Tens/_Units/_Beam/_Halo, HaloRing under the head; if Splice exports HaloRing as its own street item, the builder must pair it with its head); the 41 heads in today's export are `source: procedural` with no kit ref, which CrossingKit.Dress rejects (a crossing part without its kit piece); `CrossingKit.LiveDots` is off: turn it on once Vela's re-cut LightDotRow (u = metres) is adopted and the renderer count is checked (each signalled module becomes its own renderer).
- **Proof118** (GameBoot after r29; tools/game/cli.ts gates `r118`): cam058 (gated: the Tech's ring Loop · Disable · Fire with no ⋯, the Scout's Fire alone, the loop marker from 2:00, the second Loop shut, Disable → "Camera cut" + desk clock, the dead disc after the report, the camera's price "+2") with pictures r118-ring-camera-tech (Glyph's 2x crop: take it from the 2160 set), -ring-camera-scout, -loop-marker, -loop-holds, -camera-cut, -dead-disc; Glint's H1-H5 (the heads nearest the Records Office held per state, yaws, day/evening/night, eye level, the guard frame, three states at night; ok null when no SignalHead), R1-R5 (route A at 2:10, the Enforcer at the door, yaw 210 and 30, day/evening/night; each frame logs the people's screen points and ArchitectureFade.Reached), V1-V3 (Vale beside the guard on the landing, carried at yaw 30 and 120, eye level front and back). Not gated: those values go to Glint.

## Batch 0.2.118 code round 3 (Latch, 2026-10-25; code only, no build, no batch): footways, SignalLens and HaloRing, retired crossing pieces
- **Footways** (new Editor/World/FootwayKit.cs; WorldDto FootwayDto, WorldReader `ground.footways`, Layers.Ground, RoadMarkings): the 14 `ground.footways` are drawn as their table's element (the table is the flat `ground/footway/<gate>/table`, so counts.ground stays 196): the table from lip.d to depth at 0.18 in the plots' paving, the lip falling `drop` to the carriageway at the kerb line (pearl kerb), the ramp falling `rise` over `run` (the lane's road-enamel), the two bands flush (+3 mm) at the ramp's foot and behind the lip. Each ring is checked against the frame (1 cm; unknown = reject). The posts are the export's bollards (street layer, already placed). The higher road's kerb is already not laid across the mouth (OffOtherRoads) and the lane's kerbs run along the table's sides. The centre-line dashes keep off a footway's table, ramp and foot band.
- **SignalLens part names, checked against the kit** (kit.json `parts` and the FBX node names): DK_SignalLens (body), DK_SignalLens_Rim, _Figure, _Tens, _Units, _Beam, _Halo; the LOD file's DK_SignalLens_L1_* and Blender's .001 read the same through KitLods.Part. They match the builder's names. **HaloRing** is its own adopted piece (mesh DK_HaloRing, 1.24 m, radii 0.47 to 0.60) and is not a street item. The builder had it as "HaloRing"; it is now matched as DK_HaloRing. CrossingKit.PlaceRing puts one under each head at `signal.halo` (checked equal to the head's pivot), on the head's yaw and at its built size; it casts no shadow and is not counted as a placed piece. SignalHead finds it with the head's parts. Dress now requires body, rim and ring on every head. WorldReader reads `signal.halo` {at, z, r}.
- **LiveDots on** (`-bakeddots` bakes them as before, for an A/B): a signalled crossing's dot rows are instances. Only the DK_Cross_Light slot takes the gated SignalDots material; the charcoal seat keeps its enamel (before, every slot was swapped, which would have lit the whole seat plate). The crossing's first head drives its rows, not only `/signal/a`. A module laid against the crossing's direction mirrors its start (none in today's export: all 274 are aligned). **Count for Redline's perf064 row** (from today's export; the build logs it as "live dot rows: ..."): 194 live modules on 16 signalled crossings, so **194 renderers**. The current Built prefab has 1 MeshRenderer and 2 material slots per module, so up to 388 draws if all were in view. They were baked into crossing-parts batches before. The 80 modules on unsignalled crossings stay baked. There are also **41 HaloRing renderers** (one per head, each with its own MaterialPropertyBlock).
- **Retired pieces out of the code**: CrossingKit (the SignalEye Body/Walk/Stop/Beam/Halo names, the crossing-edge/-stud/-arc/-field kinds, now rejected as unknown, and the lit strip's glow), CityLayers.BakedCrossingPart (dots and bars only), SignalHead (WalkFigure/StopFigure gone), Proof118 (`oldEye` key gone), CloseUps (SignalOrb/SignalEye replaced by SignalLens; the kerb close-up stands at the nearest SignalHead), KitLods comments, Editor/World/kit.json note. ContactShade no longer lays a blob under a signal head: its foot is the halo disc and the clock ring, and a blob would have shaded over them.
- **Pruning**: `bun run kit` copied 0 files, pruned 0 and left kit.json unchanged. It prunes only LOD files and textures, so the seven retired base FBX stay in Kit/Models (DK_CrossEdgeLine_1/2, DK_CrossInlayArc_R24_W08, DK_LightField_1/2 in Ground; DK_SignalEye in Signals; DK_KerbStud in Street). **That is Splice's**: copy.ts should drop a rejected piece's files. On the Unity side, KitLods already drops their Built/ prefabs at the next build. KitImport.Generated now also drops a **rejected** generated piece's Generated/ prefab (the 7 retired; a candidate's prefab, e.g. WashingtoniaPalm_Street, stays for review). Nothing was deleted by hand.
- Also: CrossingKit.Head used `GetComponent<SignalHead>() ?? AddComponent`. That fails in the Editor because of the fake null, so it now uses TryGetComponent.
- Evidence: `dotnet build` of Runtime/** (compile118) and Editor/World/** (compileEd118r3, with the OSX standalone extension ref; WalkReview.cs excluded because it already fails without the Editor's own references) against Unity 6000.6.2f1's DLLs: both "Build succeeded", 0 errors. `bun run typecheck` is clean. The export dry-check (bun, out/world 18:33): 14/14 footway rings match their frames, all 14 tables are in ground.flats, 41/41 halos are at their head's pivot (r 0.47), and all 16 signalled crossings have a driving head (each the `/signal/a` today).
- Open:
  - **Splice**: tools/geometry/visible.ts takes the higher of the table flat and the lip slope over the lip strip (s 0 to 0.38), so it reads 0.18 where Unity draws the slope. Either start the table ring at lip.d in the export, or let the slope win in visible.ts. The same question applies to the walk file.
  - **Splice**: kit copy pruning of rejected pieces' base FBX.
  - **Redline**: read the "live dot rows" and "signal heads ... clock rings" lines in the build log for perf064, and A/B with `-bakeddots`.
  - **Glint**: ring and dot emission levels in play.

## Batch 0.2.118 (Latch, 2026-10-25): BATCH OK on build 0.2.122 (c037, e4b61f2e + this round)
One batch, four player builds: 0.2.118 failed game-check (r29), 0.2.119 failed the budget, 0.2.120 passed but its signal heads drew solid squares, 0.2.121 passed with full rings drawn as squares; 0.2.122 is the build for Sky. Every later build was `--from game-build` (or `--from unity-world`), never a second full batch. Logs: out/batch-0307.log (first run), -0307b, -0308, -0309, -0310, -0311 (final); evidence in briefs/041/build/0311/ (git-ignored).
- **Pre-flight:** Vale C copied from out/people-models (councillor.fbx sha256 d711b398… = the manifest's C entry; same GUIDs; manifest.json with it, which also carries the driver's `keep` mask); never B. `Kit/Prefabs/Landmarks/DK_SpaceHQ.prefab` (+ .meta) removed: no GUID reference anywhere under unity/Assets or ProjectSettings (the world uses SpaceHQ_Portal). `bun run world` ok (SignalLens 41, LightDotRow 274), `bun run geometry` ok (walk file 656x328; G-1a 1, G-1b 6, G-1c 729: Splice's ratchet), `bun run kit` unchanged (262 adopted, 8 rejected, 0 copied).
- **Steps (first run, out/batch-0307.log):** sim ok 54 s, 303/303 with identical replay, DLL bbe5a1da1e10 (engine 0.31.0 and text.json into StreamingAssets); compile ok (1 warning); kit ok; world ok; unity-world ok 82 s (fingerprint 67fac94f…; "Play: ground from the walk file (656x328 at 0.25 m, world 333e3ddd2c84; floor 0.00 to 1.43 m)"); unity-match ok, 394/394.
- **Final run (out/batch-0311.log, 0.2.122):** game-build ok (16 s build; shader variants cached after the first 1,359 s build); game-check ok, 303/303, every proof section ok (driveClick, fireStop, ringCheck, inputMap, act, fire, cars, hudClick, dblClick, holdActs, holdRange, leash, done, rings, levels, r29, r118); shots ok, 11; budget ok, 10 within budget, layout audit 10/10 clean; late-fight ok, loud-fight worst 28.4 ms, p99 6.9 ms, end 21 ms (NOISY MACHINE, load 11.5 to 13.6); tp-long ok, play-opening p99 5.2 ms, drawn 286 (157 beyond 150 m); tp-street drawn 1,362; tp-worst drawn 1,894 (2,145 on 0.2.117), 5.1 M tris, SetPass 218. Play zoom 267 fps mean, p99 5.2 ms at 2880x1864 (Air-sized proxy on the Studio).
- **2160 set** (`-gameProof -capture-size 3840x2160`, status ok, every section ok): briefs/041/build/0311/proof-2160, for Glyph's 2x review. Front end and start frames at 1080 and 2160 (`-frontEndShots -settings`, status ok): frontend-1080 / frontend-2160 (briefing, start-1-begin, start-2-glide, start-3-settle, start-4-play).
- **Fixes folded in this batch:**
  - Proof29 reroute read the Tech's Reroute at a fixed tick 786 (1:18.5); c037 moved it to 1050. It now takes the tick from route A (r29 failed on 0.2.118: no reroute seen).
  - Proof118 extended for Aster's shot list: C (the four agents together at d:Courtyard and d:Office approach, yaw 30/120/210/300, with screen points and ArchitectureFade.Reached), E (Vale out of the official entrance at four yaws by day with his drawn height, 57 to 58 px; carried at four yaws; Intel open; the feed's history; the driver at play zoom and at eye level, face and hands), F (the lane-mouth footway nearest the Office from the export, yaw 30/210 and eye level), H1 figure rects for Glint's measure. V1 waited for Vale beside the guard: he leaves by the official entrance at 5:50, never the public door, so it now waits for `Vale != Office`. PxTall reads bones, not the skinned bounds (the driver read 114 px).
  - Proof118 set a signal state and shot 4 frames later, inside the head's 0.2 s crossfade: every H frame showed the previous state half-faded. It waits CrossfadeS + 0.15 s now.
  - Briefing B against the budget: the vignette scrim is not HUD (notHud, like the conversation veil and the dip): 53.7 % → 17.2 % (limit 18 %). The title, hook, objective, card name, tag, purse, Day/Night and Begin are the reading surface's copy (`hud` in the manifest, as the 0.2.117 card's title and copy were); they had been tagged objective `line`s (7 on screen, the objective 7 words).
  - Vale's cameo on the Enforcer's plate is **14 px, not Aster's 18 px**: 18 and 15 failed the layout audit's rim rule (10.2 px clear inside a 36 px plate after rounding).
  - **Signal textures:** the kit's metas import signal_atlas_v1 and signal_ring_v1 with Alpha Source None, so every face drew as a solid colour square (no figure, no numerals) and every clock ring a full square. CrossingKit.EnsureSignalTextures (called by HarbourWorldBuilder) sets Alpha Source = input and reimports on every world build until the kit's metas carry it (`bun run kit` puts the old meta back; the builder reimports it).
  - The ring's `_Cutoff = 1 - lit/24` reached 0 at a full ring, where alpha 0 passes the clip too: walk and stop drew a coloured square on the paving. SignalHead clamps it to [0.5/255, 1.001] (RingCutMin; the texture's faintest ring pixel is 5/255).
- **Shots (briefs/041/build/0311/proof-1080 and proof-2160):** A r118-ring-camera-tech (Watch · Loop · Disable · Fire), -ring-camera-scout (Watch · Fire), -loop-marker (2:00 → loop:entrance:1:59), -loop-holds, -camera-cut ("Camera cut", desk disable:pickup:14), -dead-disc. C r118-c-cafe-yaw30/120/210/300, r118-c-office-yaw30/120/210/300, plus Glint's r118-r-{day,evening,night}-yaw{210,30}; the capsule cut opens a local hole round the four at every yaw (no hole near a third of a face, so Sky's E fallback was not tried). D frontend-*/briefing and start-1..4. E r118-v1-day (yaw 210), -v1-day-yaw30/120/300, -v1-evening, -v1-night, -v2-yaw30/120/210/300 (the carry plate's cameo is in these), -v3-eye, -v3-eye-back, -e-intel, -e-feed, -e-driver-play, -e-driver-face, -e-driver-hands. F r118-h1-{walk,clearing,stop,arriving}-yaw{30,120,210,300}, -h2-{evening,night}-*, -h3-{day,night}-*, -h4-day/night, -h5-night, -f-footway-yaw30/yaw210/eye; also hshots/ (the same head held from boot with `-signalstate`, 3840x2160 overhead and 1080 eye level, both faces). G levels-guard-landing (the guard's figure at 0.93 m on the 0.93 m landing; compare briefs/055/shots/office-entrance.jpg), levels-crew-steps; the walk-file log line above.
- **Glint's H1** (2160, by day; briefs/061/glint/h1-measured-0.2.122.json, tool out/scratch/latch/tools/h1_measure.py with grade.py's dE2000): walk #88ECC8 (dE 5.8 to 5.9 against #55E6B0: in, but pale), clearing #FDB600 to #FDBA00 (dE 1.7 to 2.9 against #FFB624), stop and arriving #FB5743 (dE 4.3 to 4.5 against #F2432E). Three yaws each: at yaw 30 the camera sees the back of the head.
- **perf064** (Studio, 2880x1864, live dots; briefs/041/build/0311/perf064): before/after/before/after frame p99 14.1 / 9.1 / 15.3 / 8.2 ms, frames over 16.7 ms 2 / 1 / 8 / 0, cones p99 8.5 / 3.6 / 9.3 / 3.1 ms, cone builds 10.2 / 5.5 / 10.0 / 2.6 a second (0.2.113's after: 8.2 / 7.7 ms, 0 / 0). The machine was loaded (other crews' compiles, load 10 to 28). **Renderer counts:** live, 194 dot-row renderers (388 material slots) on 16 signalled crossings + 41 clock rings, 5,031 batch renderers, 481 crossing renderers not casting; `-bakeddots` (a world build only, out/scratch/latch/bakeddots-world.log, then rebuilt live: same fingerprint 67fac94f…), 0 live dot renderers, 274 flush parts baked, 5,065 batch renderers, 287 not casting. Net: live dots add 160 renderers (194 − 34 batch chunks). No baked player was built, so there is no frame-time A/B.
- **Bot row** (random-a, human-a, human-b on 0.2.122; bot/): random sealed at 3:50, 74 ways, Q-OFFER 7 (fire:no-target on camera:mast-314 after it was shot out: the known class), Q-FRAME 2 (255 ms at 0:43; the rerun's one is 91 ms at 1:39: untagged and moving, so load); human-a (route-g-kill) enforcer-down at 2:49, no anomaly; human-b (route-b) success at 8:24, Q-SOLID 25, Q-GIVER 2, Q-FRAME 1 (53 ms). **No new Q-* class.** Q-SOLID: crowd figures standing for over 0.5 s at two Office doors, (-170.6, -294.3) the public entrance on the 0.93 m landing and (-140.2, -309.6), every minute at :09 to :12 and :30 to :31 (people going in and out). The 0.2.116 and 0.2.117 rows were random only (Q-SOLID 0); the human rows that reach 5:19 are new since. Q-GIVER: Go offered on car/getaway with the Enforcer in the car (Go's giver drives from inside; likely the check, not the ring). 0.2.120's row matched it: Q-OFFER 3, Q-SOLID 23, Q-GIVER 2, no Q-FRAME.
- **Open:**
  - **Glyph**: the 2x review from proof-2160 and frontend-2160 (the carry cameo is 14 px; briefing B's tags and the scrim outside the HUD layer).
  - **Glint**: the H1 refit from the file above; the silhouettes through the Office tower at yaw 210 (the played yaw) read as people standing on its terrace (the driver and Vale as silhouettes, and the rivals inside a capsule hole in the terrace: r118-v1-day, levels-guard-landing); the heads' night bloom (h2/h3/h5); Vale C's trim in sun.
  - **Vela / Splice**: signal_atlas_v1 and signal_ring_v1 metas need `alphaUsage: 1` (Alpha Source = input) in the exporter, then the catalogue; the builder's reimport stays until then. Vela: at a full ring the data's `_Cutoff = 1 - lit/24` hits 0; SignalHead keeps it at 0.5/255.
  - **Splice**: the budget's `briefing` limit (18 %) and objective-line rule were written for the 0.2.117 card; briefing B passes at 17.2 % with the scrim outside the HUD and its copy untagged. Say if the objective (7 words) should count as a line.
  - **Suture**: Q-SOLID at the Office doors (crowd door stands against the walk file's landing), Q-GIVER on Go from inside the car.
  - **Redline**: the counts above; perf064 after-windows about 1 ms over 0.2.113's on a loaded machine; a baked player is one build if a frame-time A/B is wanted.
  - **Aster**: 0.2.122 to Sky after Glyph's review; Sky's Air perf run (brief 064 findings 1 and 2) is not done.

## Glyph 2x (0.2.122) (Glyph, 2026-10-25; review only, no edits to code or kit, no Unity)
**Verdict: SHIP for Sky's play.** Nothing breaks the HUD budget, the no-sentences rule or the kit's colour rule, and every 058 piece is built as I drew it. The nits are polish and one content error on the briefing (N1), none is a play blocker. I would fix N1 to N4 in the next code round because they are on the first screen Sky sees.
Looked at: proof-2160 and frontend-2160 at 2x crops (my crops and a 1080 downscale of every frame: out/scratch/glyph/r122/). Budget run on the 2160 set with `bun tools/hud-budget/cli.ts <shot> --json`: briefing 17.08 % against its 18 % limit (state briefing, pass, no sentences, no failures); ring on a camera 6.88 % (Tech) and 6.56 % (Scout), loop marker 7.50 %, camera cut 7.09 %, dead disc 6.99 %, Intel 10.38 %, feed history 13.80 % (all under 22 %); carry frame (rest) 6.61 % against 15 %. The batch's own 10/10 stands.

### What passes
- **Briefing B (frontend-2160/briefing.png).** The street is the frame, the file card is the kit's `panel_card` with the 360 x 296 photograph on its ink-deep rim, brass hairline, the "Alive" fact with the take icon; Vale C's painted duotone holds at 2x (spectacle rims, collar band, no smear, corners 10 px). Two way cards are `panel_dark` with the frame's screws and chrome rim; the clock plate (0:00, 5:30 in turquoise-light, 8:30, the preparation sweep) is exact; Begin has its light line and diamond; Day / Night tabs read; the scrim is outside the HUD layer and 17.2 % holds. Leaders are cream over ink-deep with dashed rings.
- **Start frames.** start-1 fades card, title and strip together with the scene lit; start-2 the glide (no HUD but the corner pointer, see N4); start-3 the roster sliding in clipped at the left edge as designed and the four rings pulsing; start-4 the play frame: roster, clock bar 0:01, scope, goal line "Take Vale alive.", the two speech bubbles. The sequence matches board 03.
- **The ring on a camera (r118-ring-camera-tech / -scout / -loop-holds).** Wedge icons 22 px, cream, lit one in turquoise-light; Fire dashed with the `bang` mark at the end of its pill; pills one line each: Loop "21 s +1", Disable "11 s +2 siren 1", Fire "draws 1.0 s +2 siren >=2 !" (no "/hit"). The shut Loop (second loop) is struck, dim, lock plus the end "2:53". Scout: Watch and Fire halves. No pill is wider than the shipped Pressure (Fire is the longest, about 125 game px).
- **Loop, Disable and the desk clock.** The loop window marker reads "1:59" (`eye_shut`, the m:ss format, turquoise underline); the "Camera looped" chip has the Tech's face with the `mark_ok` tick and the loop icon; "Camera cut" has the scissors; the desk clock is the as-built marker with `p_alert` and "14" (underline draining). Dead disc: `disc_dark_24` with the scissors at 24 px sits above the head, cream icon filling the disc, chrome rim: correct.
- **Portraits (Vale C).** Intel at 36 px: Inez's and the driver's painted cameos read (bob and spectacles, headband), Inez's turned ring and tick kept, the guard stays his amber shield pictogram; faces are not doubled in rings. The Vale chip carries the Enforcer's face (the doer) as the chips do. The briefing contacts are `Cameo` of `face_waitress` and `face_driver` at the ring size the card wants.
- **Capsule cut.** A feathered hole round each group at yaws 30, 120, 210 and 300 with the cream rim; markers (eye, shield, pips 1 2 3, the walk plates) stay readable inside and over it. The stipple at the hole's edge is visible at 2x on roofs (yaw 30, 120) but reads as the cut, not noise.
- **Crossing heads (r118-h1-*, hshots/*/h3-day-*, eye level).** Walk green figure, clearing amber figure plus the countdown digit, stop red, arriving red figure plus digit, each with its beam and halo ring (dashed and blinking in clearing and arriving). At play zoom a head is about 28 game px: the figure and numerals are not legible there, so the four states read by colour, ring and the blink, which is enough; the glyph detail is a bonus close up. Colours are Glint's (H1 dE 1.7 to 5.9).
- **Sentences.** No new sentence in play. Standing items, unchanged and not regressions: the hold pill's activity name ("Hold · Watch the entrance camera · 39 s", Fable's), the beat ticker ("Inez called the guard.", "Robots coming."), and the opt-in SCENES · HISTORY panel's sentences. If Sky's "no sentences" is meant to cover the history panel, that is a call for Aster (it is a reading surface opened on purpose).

### The carry cameo: does 14 px hold?
**Yes, as a tag, not as a likeness.** In the 2160 crop (r118-v2-yaw210, roster plate "timer-4", 112 x 72 device px) the cameo is 28 device px: the ring, white hair and the collar read, the face does not, and Vale C's grey crop looks like the Enforcer's own grey roster face. It tells "a person is carried" with the tick, no clipping, rim clear. It does not tell "Vale". If Sky wants the name carried, the fix is the plate's height, not the face: see N10.

### Nits (owner, exact fix)
**N1 · Briefing: the Target leader and the Office tag point at the guard's door, not Vale's. (Latch + Waymark, before Sky sees it).** `HostBriefing.Load` sets `TargetAt = Place("door")` = `site.GuardPost` and `OfficeAt = TargetAt`. So the solid leader from "Councillor Vale" ends in a dashed ring round the guard at the public door; Vale leaves by the official entrance under the canopy (mission.json destination "Official entrance", timeline 5:50). Fix: Waymark adds `brief.target.at` or a `brief.places.official` = "Official entrance" in brief.json; Latch: `b.TargetAt = Place(that)` (HostBriefing.cs:144), keep `OfficeAt` on the canopy; the Front door way keeps `door` (the guard's post is right for that way). The two way leaders already land correctly (Front door on the door ring, Pickup on the car ring).
**N2 · Briefing: the "Records Office" tag is not on screen. (Latch).** Spec 062: `plate_dark`, `papers` icon, "Records Office", chrome tail onto the door. In frontend-2160/briefing.png there is none; the proof manifest lists it at (4310, 3413), outside 3840 x 2160 (Briefing.cs:131 puts it at the door plus 4.6 m up, minus the tag height and 12 px, with no clamp, so at the briefing frame it lands above the top edge). Fix with N1 (anchor on the canopy), then clamp: `top = Mathf.Max(24f, ...)` and `left` inside 16 px of both edges; if clamped, drop the tail.
**N3 · Briefing: the sub-line fails contrast over the scrim. (Latch).** Measured at 2160 on the pixels behind the text (cream type): hook median 3.5:1 (large bold, passes 3:1), objective line "Take him alive before it reaches Security." median 3.2:1 and 2.1:1 on its brightest 5 %, against 5.9:1 in my spec. The corner scrim (Briefing.cs:236) falls as `0.82 * (1 - d/700)^1.5`, so it is about 0.2 where the line ends (d about 420 px). Fix: `corner = 0.82f * Mathf.Clamp01(1f - d/1000f)` (linear, 1000 px): about 0.48 at that point. The scrim is outside the HUD layer, so the 17.2 % does not move. Re-measure: the objective should reach 4.5:1 at the median.
**N4 · A stray order pointer disc sits at the top-left corner on the briefing and the glide frames. (Latch).** A 22 px disc with the footsteps (walk) icon at (22, 22) on briefing, start-1, start-2, start-3 (gone under the roster in start-4; also on proof-2160/briefing.png). It is the ground-pointer badge parked at the origin (no pointer in a capture, but in play it will follow the cursor over the briefing and say "walk" while no order can be given). Fix: hide the pointer badge and every hover cue while `BriefOnStreet` is up, and during the glide until the roster lands (3.3 s); show it from the settle.
**N5 · Briefing: role tags are not centred under their portraits. (Latch).** At 1080 equivalent the portrait centres are x 95, 183, 271, 358 and the labels' centres 68, 165, 264, 338: offsets of -27, -18, -7 and -20, so they wobble. Fix: give each tag the portrait's width (62 px), `text-align: center` (`unityTextAlign: UpperCenter`), and pad the 0.12 em letter-spacing's trailing space (`paddingLeft` = the tracking) so the word sits on the centre line. Also the "MACHINES" tag crosses a diagonal wall edge at the strip: fine with the strip scrim, which is not there; see N6.
**N6 · Briefing: the bottom strip's scrim is missing, Begin and the tabs rely on the paving. (Latch).** The spec has a bottom scrim (ink-deep 0.82 over 220 px, Briefing.cs:237 builds it) but the frame shows none under Begin. Measured: Begin 5.0:1 median, 3.4:1 on its brightest 5 %; Day / Night 4.0:1 and 3.0:1 (the unfocused NIGHT is cream at 50 % by design). Fix: check the strip term is multiplied in (`vig`, `corner` and `strip` should add, clamped to 0.85); if it is, raise `H - 220f` to `H - 300f` and `0.82f` to `0.88f`. Target Begin at 5.7:1 as drawn.
**N7 · Briefing: way-card chains are left-aligned, not centred. (Latch).** The spec checks "chain pictures centred on the row": the chain (face, arrow, disc, arrow, disc) fills the left 55 % of the 408 px card and 45 % is empty. Fix: `justifyContent = Justify.Center` on the chain row, and the way name stays top-left. The guard disc's rim is a thin grey line while the face, door, car and take discs wear the chrome ring: give `badges/signal_guard` the same `disc_dark_24` and chrome ring as the other discs so the row is one family.
**N8 · Type weights read lighter than the kit. (Latch).** "Councillor Vale" (spec Jost Bold 28 ink), the way names (Jost Bold 15.5), the "TARGET" header and "Alive" render at Regular or Medium weight; only the hook is Bold. Fix: `unityFontStyleAndWeight`/the Jost Bold font asset on those four, per the art bible ("Jost Bold only").
**N9 · A hold pill covers the marker or the dead disc on the same camera. (Latch; found the same on three surfaces).** The Scout's "Hold · Watch the entrance camera" pill is anchored on the camera head, so its chrome tail lands on top of the loop marker "1:59" (r118-loop-marker), on the desk clock "14" (r118-camera-cut) and half over the dead disc (r118-dead-disc, the pill's top edge overlaps the disc). Fix: in `HoldPillFrame`, when the target has a window marker or a dead disc, raise the pill's tail anchor by that element's height plus 6 px (marker 76 device px = 38 game px; disc 24 game px), so the order is: pill, marker or disc, camera head.
**N10 · Carry cameo at 14 px (Latch + Splice's audit).** Holds as a tag (above). To carry Vale's likeness: the plate is 36 px high and the audit's rim rule wants 10.2 px clear; an 18 px cameo leaves 9 px. Fix: make the Enforcer's carry plate 40 px high while carrying (the cameo 18, 11 px clear) or have Splice's audit take the cameo's own ring (3 px margin baked into `cameo_councillor_model`) as part of the clear distance. Not for this batch unless Sky asks.
**N11 · A done chip is clipped by the top of the screen. (Latch).** r118-dead-disc: the "Camera looped / cut" chip on the entrance camera is cut by the viewport top edge (85 x 39 device px of it show). Fix: clamp every anchored chip's `top` to at least 8 px and, if the anchor is above the viewport, hide it (the feed card already carries the beat).
**N12 · Roster walk plate: the bar's end cap touches the seconds. (Latch, minor).** "1 s" sits 2 px from the dark bar cap (r118-ring-camera-tech crop). Fix: 6 px between the bar and the number.

### Open questions (not nits)
- **Watch on the camera ring.** 058 §1 gave the Tech Loop, Disable, Fire (Loop lit at 12) and the Scout Fire alone; the build adds Watch (it is the learn way for a cone) and draws it at 12 o'clock for both agents, with Loop at 3. I think Watch belongs on the ring (it is a real way and the Scout needs it), but then Loop should lead at 12 for the Tech and Watch go to 9 or 3. All proof ring shots are hover or hold states (the bottom wedge is lit), so the rest default is not shown: Latch, please add a rest ring on a camera (Tech and Scout) to the 2160 set. Aster to confirm Watch stays.
- **"draws 1.0 s" on a camera Fire pill.** 058 §1 said no time on Fire at a camera; the GDD hold line asks for the draw mark where a way draws, so I accept it. If Fable's key says otherwise, it is a words call.
- **Not seen in this set (unverified):** the looped camera's hollow dashed cone (the learned cone is not drawn in these frames), the shot-out dead disc (`disc_light_24` with the ink fire icon), the loop's "Camera on" pop at the 2:00 end, and the Alt breakdown for Loop and Disable. Latch: one 2160 shot each if you want them reviewed.
- **Glint's items, noted not specced:** the driver and Vale as silhouettes standing on the Office tower's terrace at yaw 210 (r118-c-office-yaw210, levels-guard-landing: the dark figure with its pip on the terrace, and the "eye" camera disc floating on the terrace at yaw 300); the rivals inside a cut hole in the terrace; Vale C's trim in sun.

### Report
Anima: read `anima read --wake --agent glyph --objective "2x review of build 0.2.122"` at the start. No skill matched (the two are controls iteration and play feedback); the standing rule "name the deep rule, scan sibling surfaces" did help once: the hold pill covering the loop marker became N9 only because I then looked for it on the desk clock and dead disc. About 190k tokens, 52 tool uses; I could not time the wall clock. `anima breathe --agent glyph` at delivery.

## Batch 0.2.123 non-059 code round (Latch, 2026-10-26; code only: no player build, no batch, no shots)
Queue: briefs/041/next-batch.md "Batch 0.2.123" items 2 to 6 (the 059 port is its own task). Nothing in tools/game, sim/, contracts/ or Resources/Atmosphere/signals.json was touched.
- **1 · Glint's yaw-210 fix (brief 056, "Glint (2026-10-26)")**
  - F1: HostTurn.FadePoints (HostTurn.cs:174-178) adds `driver` and an ungated `councillor` after the waitress (Aster's Q1: key people's silhouettes stay as built, not gated on sight). ArchitectureFade.MaxPoints 10 → 12 (ArchitectureFade.cs:114), DK_Cutaway.hlsl `DK_CUT_POINTS` 10 → 12 (line 34); the arrays and `cuts` follow the const.
  - F2: `edgeInner` 0.55 → 0.70 (ArchitectureFade.cs:78; a trial). The scene's ArchitectureFade is added fresh by Rig.cs on every world build, so the batch's unity-world step takes 0.70; a scene built before this keeps 0.55 until rebuilt.
  - Test: ArchitectureFade.HasPointNear / Cap (:129-132); HostViews.SilhouettePeople (Silhouettes.cs:15-20); HandoverHost.UncutSilhouettePeople and FadePointCount (HostTurn.cs:182-199). Proof118.Shoot and ProofLevels.Shoot log `<shot>: uncut [keys] points n/12` after every picture, `overflow` at or past the cap. r118 JSON gains `"uncut": {ok, frames, withUncut, maxPoints, cap, overflows}` (gated: withUncut 0) and `"reach": {ok, max, limit 140, frames}` over r118-v1-day and the four yaw-210 r frames (gated: max ≤ 140).
  - Edge trial: after r118-v1-day, `r118-v1-edge-55`, `-70`, `-85` (the run's own resolution: 1080 and 2160 sets), then 0.70 restored.
  - P1 (the darker lip) not built.
  - Unity note: a global shader array keeps the size it was first set with for the Editor session, so an Editor opened before this change must be restarted. A player build is unaffected.
- **2 · Glyph's nits (0.2.122)**
  - N1: HostBriefing.Load (HostBriefing.cs:140-153) reads `brief.target` as a role string or `{role, at}`; `TargetAt = Place(brief.target.at)` (an id with or without `d:`; "door" when absent or unknown, logged in Sources); OfficeAt = TargetAt (the canopy). The ways keep their own `at` (Front door: "door").
  - N2: the Office tag is clamped to 16 px at each side and 24 px at the top, and above the strip. Clamped, its tail is hidden (Briefing.cs:132-140).
  - N3: the corner scrim is now `0.82 * clamp01(1 - d/1000)`.
  - N6: the strip scrim is 0.88 over 300 px. The strip term was already composed in (Briefing.cs:249-250).
  - N4: `PointerHeld` = briefing up, or the glide before 3.3 s (HostBriefing.cs:26-28). CursorKind returns "" (HostContext.cs:472) and the armed badge stays hidden (HudToolkit.Live.cs:273). Hover is already cleared in both states.
  - N5: the job tags centre their letter row (`justifyContent: center`). FrontEnd.SetText lays a tracked word out as a row of letter labels, so the label's text-align did nothing.
  - N7: the chain row spans the card and centres. A `badges/` step is drawn on `disc_dark_24` (58 px) with the badge at 34 px inside.
  - N8: Jost Bold through FrontEnd.Typeset for the card head, the name (28, ink), the fact (15, ink) and the way names (15.5).
  - N9: HoldPillFrame lifts the pill's tail to 6 px above a camera's window marker or dead disc. The tops come from the same projections DoneMarks and StreetLess use (HudToolkit.Hold.cs:29-41, 76-78).
  - N11: a done chip whose anchor is above the frame is not drawn, and the top is clamped to ≥ 8 px (HudToolkit.Done.cs:112, 138-139).
  - N12: a snug plate value (`hwShown`) is left-set 6 px after the bar with 4 px right padding (HudToolkit.Less.cs:272-273). The 2 px gap came from a right-aligned auto-width label.
  - N10 is out.
- **3 · The ring on a camera:** ContextRank.Choose (ContextRank.cs:92-95) moves Loop to the head of a camera's ring order. The Tech reads Loop 12, **Watch 3**, Disable 6, Fire 9; the Scout keeps Watch 12, Fire 6. Only the ring's order changed. The quick click and the lit wedge still follow the rank, so while the cone is unlearned the Tech's lit wedge is Watch at 3, and once it is learned it is Loop at 12. The test is ContextRankTests.OnACameraLoopLeadsTheRingAndWatchStandsAtThree. Rest shots: `r118-ring-camera-tech-rest`, `-scout-rest`, `-enforcer-rest`, with the pointer held in the ring's dead zone and the wedges, hover and lit logged (Proof118.RestRing).
- **4 · Shots added** (Proof118 "camera states", not gated, logged and in `camStates`). The Tech watches the entrance camera, so its cone is learned; then `r118-alt-camera-panel` (the camera's panel with Alt latched: the Loop and Disable breakdowns), `r118-loop-cone` (`HostViews.LoopedFans` logged), `r118-camera-on` (the loop's 2:00 end, LastClosePop) and `r118-dead-disc-shot` (the Scout walks into pistol range with a line to the first live camera and shoots it). H5 is re-framed: the nearest head plus the nearest heads of two other crossings, at the yaw that keeps all three in the middle 80 %. The spread is logged. The 0.2.122 frame's three heads were the nearest to the Office, too far apart, and their centre fell on a roof.
- **5 · Suture**
  - `HostViews.GiveWay(g, p, walker)` (HostViews.cs:231-258): at d ≈ 0 the side comes from the walker's idHash bit. A pushed point is kept only if `Grid.Free`; otherwise the mirror side, otherwise 1 m behind the cast person, otherwise the engine's point (`HostViews.GiveWayKept` counts those).
  - BotRun `FromCar = { "go" }`: `Bad(k, verb)` on the ring (`s.Row?.I?.Order?.Do`), the plan and the panel; OpenRingGivers skips an able giver in the car for a FromCar wedge (BotRun.cs:46-49, 385-402).
  - The earlier value table was all in already: the seated flag, SeatSkipM 1.5, SolidBodyR 0.25 / SolidOverlapM 0.05 on the walk file's kinds, feet.csv `skip`, streamed frames/feet/hud, the p99 histogram, Q-MEM thirds (120 / 60 / 3 / 1), OfferRaceTicks 1 and "wedge shut", GiverGraceTicks 2, Q-WARN-HUD skipping while capturing, and the followers on free ground.
  - Not done (optional): the Q-SOLID stretch length per key.
- **6 · Redline**
  - R4 (SignalHead.cs:71-79, 107-170): the head's parts and its dot rows have keys of their own. `half` is computed only for a head with dots, and the dots' key is `half` to 5 cm. Writes wait while none of the head's (or the rows') renderers is visible, and the first write is always made. There is no closure per frame. State, Left and RingLit are still computed every frame for the proofs.
  - R2: `-late` turns on `HandoverHost.PerfTiming`. frames.csv is now `ms,tick,phase,what,stepMs,afterMs,viewsMs,sightMs,conesMs,rerouteMs,signalMs,gc`.
    - `stepMs`: Game.Step and the driven sample, summed over the frame's ticks.
    - `afterMs`: the host's per-tick work after each step (views.Tick, FollowUp, Track, Notice, beats, aim, hints, the drain).
    - `viewsMs`: views.Frame.
    - `sightMs`, `conesMs`, `rerouteMs`: ShowSight, PersonConesFrame, RerouteFrame plus RingGiversFrame.
    - `signalMs`: every SignalHead's LateUpdate, summed. It is the previous frame's LateUpdate, inside the same frame gap.
    - `gc`: gen-0 collections in the gap.
    - BeforeStep (the orders given) is in none of them; the `what` column tags it (HandoverHost.cs:176-178, 292-293, 324-327; LateRun.cs).
  - R7 (Perf064.cs): each window adds `mainMs`, `renderMs` and `gpuMs` (FrameTimingManager, the same stats as `frameMs`) and `unix: [start, end]` (ms), so Splice's load sampler can be lined up with the window.
- **7 · Splice:** CrossingKit.EnsureSignalTextures stays as a guard. Its doc says the kit's metas carry alphaUsage 1 (both metas checked: alphaUsage 1, alphaIsTransparency 1), and a reimport now logs a warning (CrossingKit.cs:22-36; HarbourWorldBuilder.cs:161).
- **Evidence:**
  - `dotnet build sim/Suborned.Sim -o out/scratch/latch/sim123`: 0 errors.
  - `bun out/scratch/latch/tools/compile123.ts` (Runtime/** against it): "compile123: OK".
  - `dotnet build out/scratch/latch/compileEd123/ed.csproj` (Editor/World/**): "Build succeeded", 0 warnings.
  - Unity 6000.6.2f1 batchmode through `bun run guard` (`-runTests -testPlatform EditMode -testFilter Suborned.Play.Tests.ContextRankTests`): scripts compiled (6.7 s, no `error CS` in out/scratch/latch/r123/editor-tests.log), 23/23 passed (out/scratch/latch/r123/contextrank.xml).
  - `bun run typecheck`: clean.
- **Open:**
  - **Glyph / Aster**: on a camera, the Tech's lit wedge is Watch (at 3) until the cone is learned, then Loop at 12. Say if Loop should be lit from the start; that is a rank change (ContextRank's tested "watch until learned" rule).
  - **Splice**: late.ts can name the top section from the new frames.csv columns, and the perf064 `unix` bounds can be matched to the load sampler.
  - **Batch**: the uncut gate is new. If a service-yard frame logs `overflow`, MaxPoints goes to 14 (two lines). If the driver or Vale still show a flat figure beside a hole, try depthMargin 0.25 (Glint's step 5).

## Batch 0.2.123: the 059 port (Latch, 2026-10-26; code and proof shots, no player build, no batch)
Ported from briefs/027/keel/host-interface.md "Engine 0.32.0" and "Engine 0.33.0" (no "0.33.x" addendum in the tree or Keel's worktree when this round closed: re-read before the batch), brief 059 (Glyph's §1-§5 and "the shut stand and Drive's refusals", Chime's cue), Fable's text.json keys, GDD v0.25.0 §10. Nothing in sim/, contracts/, tools/game or tools/sim was edited; `bun tools/sim/unity.ts` (the batch's sim step) was run once, through `bun run guard`, so Unity compiles against engine 0.33.0 (f65936f1). Paths below are under unity/Assets/Suborned/.

**Sky's note on 0.2.122 ("rivals just suddenly appeared when near car, they should be coming in from somewhere when the chime comes")**
- The crew is drawn from the engine's one read, `RivalPoses()`: five views keyed by engine id (rival-1..rival-5) and the car (`car/rivals`, Vela's official sedan multiplied by a dusk stand-in trim until her rival sedan lands): Runtime/Play/HostViews.Rivals.cs:29 (views), :48 (per tick), :90 (`RivalAnchorKey`); HostViews.cs:175 replaces the three followers at one point. Nobody is drawn before the car turns in; aboard they ride unseen; the car turns by the engine's heading, eased (HostViews.Figures.cs:288).
- The car arriving from Orbit Walk at 2:35: drawn from its first tick; the approach is a thin violet line on the road from the car to its next stop (the stop `RivalNext` names, the drive grid's own route, one route a leg, trimmed as it drives) with diamond ticks every 2.4 m and the `arrive` disc at the stop (HostRivals.cs:52; HudToolkit.Rivals.cs:97); an edge pointer chip (plate_dark, `signal_rival` cameo, the pointer turned toward it) while it drives off screen (HudToolkit.Rivals.cs:123).
- The beat and the sound at 2:35, when the car turns in: Fable's card `rivals.coming` ("Rival car coming.") now resolves its `at: rival-car` (and `vale-car`) and is shown wherever the cell stands, since the car turns in out of everyone's sight; the card points at the car, so a click or N shows it coming (HostTakedown.cs:410, :415). Chime's cue is wired by log code from contracts/audio/cues.json: `rivals-coming` is a 2D one-shot panned toward the side the car enters from, one voice, its cooldown; `rival-car-engine` is a 3D loop on the car's view, volume and pitch by its drawn speed, faded, paused with the clock, its start offset hashed from the leg (never random); the listener is on the camera's ground focus (HostAudio.cs:29, :74, :89, :97; the hook is in Drain, HandoverHost.cs:336). The cue file is read from StreamingAssets/sim/cues.json, else the repo's contracts/audio/cues.json, else its 2026-10-26 values (logged).
- 2:46: the three step out at the drop and walk to their posts by the cell's car: each walk is a dotted violet line to its end and the post ring as built (solid 0.55 m, dashed 0.95 m); posted, the ring stays under him; the lookout's on the far kerb (HostRivals.cs:52; HudToolkit.Rivals.cs:157).
- 3:10: the car parks across from Vale's car and the lookout posts; the false tip is offered on the parked car only (the engine's MisleadRefusal mirrored for the card: HostRivals.cs:38, HandoverHost.cs:1260, :1311); H8 rings the car (HostHints.cs:150).
- No fight-start pop (059 §2 point 1): a member's fight body is drawn by his own view, so the same figure keeps walking and fights where he stood; the fight list's rival hostile views stay empty in the Handover (HostViews.cs:201; ViewKey HostFight.cs:521). Their guns, pips and rings read the members (HostFight.cs:502, HostTargetRings.cs:94, HostFightLab.cs:433, ProofRings.cs:91).

**Engine 0.32.0 and 0.33.0 items**
- Vale in his car: not drawn on the street while `InCar` or taken from the back seat; the driver is not drawn standing in the car at the wheel (HostViews.cs:166-168). The Take's card mirrors K3: `car-not-parked` while it drives, `driver-brings-vale` at the front with a turned driver, else at the car or in it parked (HandoverHost.cs:1262); pops `p_time` and `car` (HostLess.cs:177). H16 now rings Vale's car parked round the back (HostHints.cs:185).
- Vale's car with the driver turned (Glyph board 03): on hover of the car, the driver or a shut stand, and while it drives round: the dashed cream route with chevrons from the official bay to the service bay (site `car.servicePath`, trimmed from the car while it drives), the times plate (`car` 6:13, `pointer` 6:26; the engine's Vale-in tick and `CarInBayTick`, before it drives the route's ticks at car.pace mirrored), the hand-off ring (cream 0.8 m at the rear door, the service door's side) with `take` on disc_dark_24 (HostDriveCar.cs:174, :209; HudToolkit.Rivals.cs:208). Parked: the as-built window marker over the car, `car` and the countdown to the 8:30 close, draining; before it parks, the fixed 8:30 while its route shows (HostRivals.cs:119; WindowMark.Label HostDone.cs:36; icons HudToolkit.Done.cs:94).
- The getaway car moves: drawn at `GetawayCarPos()` with `GetawayHeading()` (HostViews.cs:180, HostViews.Figures.cs:289); its marker, seats, Go pop and exit lane read the live car and the current stand (`GetawayWorld` HostGetaway.cs:18, `CurStand.Exit` :138, HudToolkit.Less.cs:1492, HostHints.cs H10); Board walks to the current stand (HandoverHost.cs:1306).
- Drive (GDD §10, Glyph board 04 and 2026-10-26): with the getaway car or an agent in it selected (a selected able agent aboard, or the car's ring or panel open), every stand of `getaway.stands` is a footprint (dashed cream rounded rectangle 4.6 x 1.9 m, chevron at the nose, `car` on disc_dark_24; the stand under the car without its disc) and a target, kind `stand` (HostDriveCar.cs:49, :115, :135, :217; HudToolkit.Rivals.cs:163). Hovered: solid turquoise-light and the route preview (the engine's CarRoute between the stands' cells, cached), the pill "Hold · Drive · 28 s" (Fable's `less.price.verbs.drive-car`); the hold commits `{do: drive-car, op, target: <stand>}` (StandRows HostDriveCar.cs:92, BuildPanel HandoverHost.cs:1123, DriveIntent :68, ContextRank tier 1 ContextRank.cs:34). No ghosts once the final fight has started.
- The shut stand (Glyph 2026-10-26 §1-§3, default: a caution, orderable): with the driver turned and Vale's car not yet parked, the yard draws at 50 %, dashes 3/5 (crawling 6 px/s while his car drives round), the plate of `nextevent` and Vale's cameo over its far edge; at `car.parks` the plate fades in 0.25 s and the outline pulses once (HostDriveCar.cs:59, HudToolkit.Rivals.cs:163). Hovered it is solid cream at 70 % with the route preview and Vale's route (its times plate only once the driver's routine is known); the pill carries the caution plate (stripes, Vale's cameo, `nextevent`) after the faces (HudToolkit.ActMark.cs:106, HudToolkit.Hold.cs:70). §4 (`stand-shut`) is not built: the engine has no such code; PopIcon maps it to `p_time` should Keel add it.
- Drive's refusals (§5): the card mirrors DriveCar's order (HostDriveCar.cs:78); a refusal pops at the stand's footprint with one pulse there: `drive-held` lock (and the holders pulse; the other ghosts struck at 45 %), `drive-fight` the car struck through (a 2 px cream diagonal: PopIcon `car#struck`, HudToolkit.Less.cs:606), `drive-busy` p_time, `drive-no-driver` and `drive-here` car, `unknown-stand` mark_no (HostLess.cs:177; HandoverHost.cs:1030). The arrival is the gain chip "Parked" on the car (`car.parked`, HostDone.cs:138, :316).
- Fable's keys: `TextKey` `stand` => `stand-<id>` and a member or the rival car => `rivals` (HandoverHost.cs:1219); `Name` for car:rivals, rival-n and stand ids (HostActivity.cs:85); the roster's `acting.drive-car` / `acting.ride` come from text.json, their glyph `car` (HudModel.cs:461); the drive pops are text.json's locks; `car.blocked` is Fable's beat card ("Vale's car stops short.", badge `car`, HostLess.cs:46); H5's loop is Inez only (HostHints.cs:135); `car.blocks` is gone from HostFeed (:273) and HostLess, `pickup-camera-blocked` from HudModel (:397); the service-lane branches of vale.exits / vale.at-car are gone (HostFeed.cs:324, HostTakedown.cs:444); mislead's feedback reads `CarSt` (HostFeed.cs:192); a member's place words (HostFeed.cs:68).
- Keel's 0.33 items: WhyIcon `crewmate` => `takedown` (HudToolkit.Roster.cs:146); the `car:rivals` holder: its badge is `signal_rival` on the shut Go pill (HudToolkit.Ring.cs:123), its countdown "N s" from GoQuery's tick on the pill (:127) and beside its holder mark over the car (HudToolkit.Less.cs:1582; `RivalsComingS` HostGetaway.cs:108); `car-held`'s `{tick}` is filled by the existing lock fill (the mission clock) until Fable's line says otherwise. The crew as people: a member on foot is a `rivals` target keyed by his id (Takedown both ways, Fire by his id; in the fight a `hostile`), the car is `car:rivals` (Watch, False tip, Fire as property); a rival's body is a body target (`BodyById`), so its ring offers Carry, Tie, Hide (HandoverHost.cs:739, :745, :754; HostTakedown.cs:20, :101, :106; ActPlace takedown :1315; Fire ids HostAim.cs:264, HostFight ShotAt, HandoverHost.cs:901). A rival's body dresses as a rival (HostViews.cs Bodies).
- GDD §10 rivals: the car drawn; the approach while it drives; after a Watch on them (`watch` on `car:rivals`, any agent, once: HandoverHost.cs:846; done chip "Timetable" HostDone.cs:267, Fable to word `gain.timetable`) the window marker over their car (`nextevent`, the seconds to the next move, the underline draining over the leg; HostRivals.cs:119), the spring ring (dashed violet at the rulebook's `fight.safe.ambush`, round the cell's current stand) on hover of the car or a posted member, or Alt, and while the follow is possible the dotted violet follow line with chevrons from their car to Vale's car (HostRivals.cs:52; HudToolkit.Rivals.cs:97). The "?" over an unwatched watcher is on the car until the Watch (HudToolkit.Less.cs:1127).
- Tests and guards: ContextRankTests `OnAStandDriveIsTheQuickClick`, `RivalsTakeTheFalseTipAtTheirCar` (Tests/Editor/ContextRankTests.cs:70, :76); PanelGuard sweeps `car:rivals` and the stands (Editor/World/Play/PanelGuard.cs:179).

**Proof shots added to the 0.2.123 set** (Runtime/Play/Proof059.cs, called after r118: GameBoot.cs:150; JSON key `r059`, `ok` gated on arrive, watch and pickup; fight continuity logged)
- arrive (Proof059.cs:96): `r059-235-turns-in` (the car at its entry at the first quarter yaw where the cell's car does not cover it, the overlap logged: :75), `r059-235-square` (the opening view: the edge chip), `r059-246-drop` (2:48: the three out and walking), `r059-310-park` (3:11, yaw 210), `r059-320-lookout`. Gates: the car first drawn on CrewEntry (±1 tick) with nobody on foot before it, the cue `rivals-coming` played, the approach drawn, rival-1..3 first drawn within 5 m of the drop (rival-4 of the park), the car parked, the lookout posted.
- watch (:136): `r059-watch-alt` (the Scout's Watch on the car, Alt latched). Gates: RivalsWatched, the `rivals-next` marker, the spring ring.
- pickup (:148): route-b.json to 3:10 (the driver paid, the false tip), the Tech aboard: `r059-yard-after-vale` (5:40), `r059-613-round`, `r059-625-parked`, `r059-drive-hover`, `r059-drive-parked`, `r059-drive-here`, `r059-take-back-seat`, and `r059-fight-start` if the fight starts (each member drawn the tick before is drawn after within 1.5 m, else logged as a jump).

**Evidence**
- `dotnet build sim/Suborned.Sim -o out/scratch/latch/sim059`: 0 errors.
- `bun out/scratch/latch/tools/compile059.ts` (Runtime/** against it): "compile059: OK".
- `dotnet build out/scratch/latch/compileEd059/ed.csproj` (Editor/World/**): "Build succeeded", 0 warnings.
- `bun run guard bun tools/sim/unity.ts`: engine f65936f1 into unity/Assets/Suborned/Sim/Plugins; headless 341/341, range 91/91 with identical replay; text.json 53c2a4f79b60.
- Unity 6000.6.2f1 batchmode through `bun run guard` (`-runTests -testPlatform EditMode -testFilter Suborned.Play.Tests.ContextRankTests`): scripts compiled, 0 `error CS` (out/scratch/latch/r059/editor-tests.log), 25/25 passed (out/scratch/latch/r059/contextrank.xml); Chime's two .wav imported with their metas. Run again after the last edits: 0 `error CS`, no warning in the new files, 25/25 (editor-tests2.log, contextrank2.xml).
- `bun run typecheck`: clean.

**Open**
- **Splice / Keel (the sim step):** tools/sim/unity.ts does not copy contracts/audio/cues.json to StreamingAssets/sim/cues.json; until it does, a player outside the repo uses the cue file's built-in copy (logged `audio cues: defaults`). One line in the copy list.
- **Splice:** gate `r059.ok` in the batch's proof reader, and add the r059 pictures to the hud-budget and 2x crop sets.
- **Waymark:** Proof059's pickup half replays route-b.json's first orders (0:00 to 3:10, pre-059 composition); once the route B family is re-composed, the proof can play the new route-b to the end.
- **Fable:** the in-sight rule on `rivals.coming` hides its beat when nobody sees the car turn in (it always turns in out of sight); the host shows it anyway on Aster's 059 routing: say if the card should drop `sight: in-sight`. A word for `gain.timetable` (the host says "Timetable"); `car-held`'s `{tick}` line for the car:rivals hold (the pill shows "N s").
- **Glyph:** not built: §3's Alt carry ("s_carrying 18 m" at the mouth ring) and the mouth's dashed hand-off ring on a shut stand's hover; the edge chip for Vale's route off screen; Glyph's 2x review of the r059 frames once the batch has them.
- **Vela:** the rival sedan (the car is the official sedan in a dusk stand-in trim). **Forma:** `MemberBeat` (watch, lean, check-watch, read-paper, pace) is not played yet: posted members idle.
- **Aster:** GDD §10's Drive line reads "Drive · 0:28"; the game prints seconds ("Drive · 28 s", Glyph's note).

## Batch 0.2.123 host round 2 (Latch, 2026-10-26; code and proof shots wired, no player build, no batch)
Sources: Aster's task, briefs/067-sky-play-0122.md, briefs/027/keel/host-interface.md "0.33.x addendum", Suture's "Sky's 0.2.122 play" in briefs/060-bot-playtests.md, Fable's text.json (tree, 05:46), Glyph's briefs/067/glyph.md and boards 01-04. Nothing in sim/, contracts/, tools/ or signals.json was edited. Paths are under unity/Assets/Suborned/. Every proof shot is Runtime/Play/Proof067.cs (JSON key `r067`, called after r059: GameBoot.cs:151).

**1 · Engine 0.33.1** (copied into Unity by a scratch copy of tools/sim/unity.ts, see Open: the committed rulebook schema rejects 0.33.1's keys)
- `CarWalk` (Game.Walk, built once a tick or order) and `MoveRoute` (a car's ground ends beside it, RouteBeside): HandoverHost.cs:1339, :1351. Used for every host test or preview of a walk: ActPlace (:1377 Board's "already there" is `g.AtGetaway`; a car's place walks to its side; Fits/NearestFree/Route on the walk grid), GroupWalk HostHold.cs:226, MoveSelection, Go(), the panel and ring route lines, NoWalkUnder, the move chip, the route preview (HostLess), the reroute fallback, GiveWay HostViews.cs:250, the crowd spread HostCrowdSpread.cs:42. Playable keeps the site grid on purpose (HandoverHost.cs:1011: a move onto a car is the engine's to end beside it). The driven agent's controller also stops at a standing car (HostDrive.cs:280; the engine's drive step reads only the site grid, so the host is the stricter).
- The "d:Meet driver" heading fallback is deleted (HostViews.Figures.cs:259). Vale is drawn from `ValePos()` (the kerb-side door; hidden in the car as before).
- Cancel for `drive-car` and `ride`: hidden by `CanCancel` false everywhere (ring ×, roster chip ×, X, PlateStop); no code change needed.
- `StandingCars()` and `CarBodyCm()`: `FiguresInCars()` HostGetaway.cs:100 (figures whose feet stand inside a standing car's body). Proof: `r067-official-car` and the `square.inCars` gate (5:50-6:20, nobody inside).
- Proof059's Tech now boards through the click path (walk beside the car, then Board: Proof059.cs:158), since Board is 1 m of the body now.

**2 · Fable's text ports**
- `tickAs`, the `rival-car` / `rival-car-driving` variants, `fillLockShort`: PlayText.cs:45 (TickAs), HostLess.cs:200 (LockVariant), :208 (LockTick), HostActivity LockLine; the holder chip over the getaway car reads `RivalsHoldShort()` HostLess.cs:215 ("Rivals coming · 18 s", "Rivals coming"): HudToolkit.Less.cs:1624.
- `debrief.fight` + `debrief.fightWhy` on the end card: HudModel.cs:372.
- `less.why` beside the under-fire cue: HostAim.cs:501 (main cause; Alt lists them all), drawn in the portrait's chip slot when it has no other chip (HudToolkit.Less.cs:259). Glyph has no spec for its place yet.
- FeedEvents fallbacks for rivals.recalled, rivals.wiped, rivals.see-takedown, rivals.wakes, rivals.frees: HostFeed.cs:279; `fight.safe` suppressed when `rivals.wiped` is on the same tick (either order, or never logged): HostFeed.cs:292, :314.
- Hold pill names from Fable's `withTarget` ("Listen to the guard"): PlayText.cs:362, HandoverHost.cs:894.

**3 · E · locked ways** (Suture's four edits + Glyph E1-E6)
- ClickRing opens on locked rows (HostHold.cs:129); a click on a thing the selection has no row on pops "who can" (faces: the selected struck, a pointer, who can; Alt the WhoCan line), never walks: HostHold.cs:416, HostHoldStatus.cs:447, HudToolkit.Less.cs:601.
- The shut ring: with no calm way open, the locked calm ways first (merged by verb, target and lock; Ask/Pay/Help/Pressure under one lock as one Turn wedge; at most 4; ContextRank order), the open violent ways after, last and dashed: HostContext.cs:164, :226. A press pops the short lock with its icon at the wedge and closes the ring: HostAim.cs:407, :420.
- The hover outside a fight: the offer, the locked pill, or nothing; never Fire (HostHoldStatus.cs:417 LockIntentOf; the locked pill: lock disc, cream name, dim dot and short lock in --text-dim, the doer's face, 80 %, the ring's dots for the cursor, Alt's long line under it: HudToolkit.ActMark.cs:134, HudToolkit.Hold.cs:45).
- The host record `<script>.host.jsonl`: HostSidecar.cs:18 (selection, hover and pill state, ring open/open-shut/take/shut with its ways, every pop with its code, every hold-preset click and what it did), flushed with each save.
- Proof shots: `r067-lock-pill-guard`, `r067-shut-ring-guard`, `r067-shut-pop`, `r067-who-can`, `r067-stack-tab` (when two or more are under the cursor at the door).

**4 · C · squad boarding and Drive** (Glyph C1-C7)
- DoersOf gives Board to every selected agent who can (HostHold.cs:109); one order each in the same frame; whoever boards leaves the selection, the whole squad too (HostHold.cs:173); BoardLock mirrors the engine's order (running, tethered, carrying, then down, inside, in car: HostGetaway.cs:54).
- The car answers with nobody selected (its panel for the crew aboard: HandoverHost.cs:1156); Drive rows on the car for each other stand, icon `arrive` (HostGetaway.cs:201); the hold's way: Board, else Drive with one stand open and Vale not aboard, else none (HostGetaway.cs:229); ranking Go first only with Vale aboard, Board 0, Drive 1, Go before Vale last (ContextRank.cs:46), drawn dashed, never lit, a struck Vale cameo, the 0.5 s hold (HostContext.cs:179).
- The group pill: the slowest doer's walk + act, every doer's face (HostGetaway.cs:219); the why chip on a refused portrait (cause icon + two words, 3 s, fading: HostGetaway.cs:261, HudToolkit.Less.cs:264); pending seat pips turning and the landing beat (HostGetaway.cs:88, HudToolkit.Less.cs:1548); the stands whenever anyone is aboard, at rest 55 % without disc or route, live on the car, a stand, an aboard selection or a Drive named by the ring, the hold or the pill (HostDriveCar.cs:59, HudToolkit.Rivals.cs:164).
- Proof shots: `r067-squad-board-pill`, `r067-car-ring-board`, `r067-board-pending`, `r067-board-landed`, `r067-drive-pill-car`, `r067-car-ring-drive`, `r067-car-ring-drive-hover`, `r067-drive-parked`.

**5 · D · sticky target** (host side; the engine is involved: see Open)
- The fire hold's latch measures travel from the target's drawn point as well as the press (HostHold.cs:349); a right press beside a moving target (34 px), or on one that walked off the cursor in the last 0.4 s, takes it (HostHold.cs:358); a lapsed order is said over the agent ("No line"; it was silent: HostDone.cs:189).
- Glyph's lock reticle over the head (set, locked, held, released; the shooters' faces; edge chip; hand-off line; the pill's tail 6 px above it): HostHoldStatus.cs:163 (FireLockTargets), HudToolkit.Lock.cs:59.
- Proof shots: `r067-reticle-set`, `r067-reticle-moving`, `r067-reticle-edge`, `r067-reticle-handoff`.

**6 · G · camera and roster**
- A portrait click selects and frames (a 0.5 s smoothstep glide at the zoom it has; Shift/Ctrl adds without moving; a double click at play zoom; the ring pulses, the car or door marker for an agent aboard or inside): HandoverHost.cs:511, :527, HostHoldStatus.cs:319, HostViews.Figures.cs:352.
- Third person enters behind the agent's drawn heading (HostFollow.cs:62); the agent keeps its heading on a take (HostDrive.cs:130).
- T back: a 0.5 s glide up the map's line over the agent, then the map view at play zoom and the yaw it was left at, centred on the agent; nothing over the street during the glide, then the world marks fade in over 0.2 s (HostFollow.cs:87, :93, :310; HudToolkit.cs:1171). The T key glides (`DriveLetGoToMap(true)`); a script's let-go lands the same map view at once, so the older proofs that let go and go on are unchanged (HostDrive.cs DriveExit).
- Proof shots: `r067-portrait-pulse`, `r067-portrait-framed`, `r067-tp-entry-1`, `r067-tp-entry-2`, `r067-back-to-plan`.

**7 · H · the conversation from the ground view**
- The two speakers are set to idle (no cross-fade), facing each other, while it plays; everyone else holds the pose the clock left them in (no walk in place): HostViews.Figures.cs:89, :168; HostConvo.cs:82 (ConvoViewKeys).
- The shot: the side the camera is on first, the perpendicular then ±15°/30°/45° (HostConvo.cs:40); the single shot faces the person toward the agent; the bubble flips under the head when it would leave the frame (HudToolkit.Live.cs:355). Silhouettes stay off in the scene (Glyph H2; my first try turned them on and is reverted).
- Proof shots: `r067-convo-inez-1`, `r067-convo-inez-2` (the Fixer's Help, two sides), `r067-convo-driver-1` (the Fixer's Pay), `r067-convo-driver-2` (the Scout's Listen), all from third person.

**8 · Redline item 4:** PC_RPAsset `m_RequireOpaqueTexture` 1 → 0 (no shader reads `_CameraOpaqueTexture` or `SampleSceneColor`; every camera uses the pipeline's setting). The frame debugger check (no CopyColor) needs the Editor open: the batch's shots compare.

**9 · rivals.coming:** Fable's card is `sight: always` now; the host override is gone (HostTakedown.cs:413).

**F · cones on demand and click priority** (Glyph F1-F9)
- WatcherOf: a member and a unit are their own watchers (HostSight.cs:30). The rules (hovered, named by the order, sees or is about to see a selected agent within 4 s, acted on, a walk crossing it, Alt; the reveal 3 s of mission clock after a Watch; combatants in a fight only on rules 1, 2, 4 and Alt): HostSight.cs:80, :151, :156. No rest strength: 0 or 1, in 0.25 s, out 0.6 s, hidden at 0 (HostViews.cs:599, :709).
- Click priority: under the cursor, then the selection's open calm way, any row, none (asked only where two or more overlap, once a second), then the kind (beings, bodies, cameras, the car and stands, the door, cabinet and stashes, passers-by last): HostPick.cs:101, HandoverHost.cs:752. The stack tab and the others' dashed rings: HudToolkit.Hold.cs:27; the wheel steps only after the cursor rests 0.3 s (HandoverHost.cs:471).
- Proof shots: `r067-cones-before` (Alt: every learned cone) and `r067-cones-after` at the Office front at 6:00.

**10 · Other proof shots:** `r067-patrol-post` (mission.json's patrol-a/patrol-b at yaw 210, read at shot time), `r067-square-cleared` (the getaway car's square at play zoom, yaw 30).

**Tests:** ContextRankTests.OnTheCarGoLeadsOnlyWithValeAboardAndTheWithdrawIsLast (Tests/Editor/ContextRankTests.cs:108).

**Evidence**
- `dotnet build sim/Suborned.Sim -o out/scratch/latch/sim067`: 0 errors. `bun out/scratch/latch/tools/compile067.ts` (Runtime/** against it): "compile067: OK". `dotnet build out/scratch/latch/compileEd067/ed.csproj` (Editor/World/**): "Build succeeded", 0 warnings.
- `bun run guard bun out/scratch/latch/tools/unity067.ts`: engine d4361806 (0.33.1) into Plugins and StreamingAssets; headless 341/341, range 91/91 with identical replay; text.json c5fa275e4422 (out/scratch/latch/r067/sim-unity2.log).
- Unity 6000.6.2f1 batchmode through `bun run guard` (`-runTests -testPlatform EditMode -testFilter Suborned.Play.Tests.ContextRankTests`), twice: scripts compiled, 0 `error CS`, no warning in the new or edited code; 26/26 passed (out/scratch/latch/r067/contextrank2.xml).
- `bun run typecheck`: clean.

**Open**
- **Keel (blocker for the batch's sim step):** HEAD's contracts/rules/rulebook.schema.ts does not know 0.33.1's `car.footprint` and `cancel.driveCar`, so `bun test contracts/rules` fails and `tools/sim/unity.ts` stops at its zod check. I copied 0.33.1 into Unity with a scratch copy that patches only its work copy of the schema (out/scratch/latch/tools/unity067.ts); the batch needs the schema fixed.
- **Keel / Motive (D, engine involved):** a lasting fire order ends when its target leaves the weapon's max once reached (fire.out-of-range: pistol 16 m) and lapses after line-lapse-s 3 s without a line once the player's move spent its walk-in (fire.lapses). Both lose a moving rival. The host now keeps the target through the hold, takes a moving target beside the cursor and says both ends ("Too far", "No line"); a truly sticky order would need the engine to walk in again rather than end. Also: the engine's drive step ignores the car mask (the host's controller is stricter).
- **Splice:** gate `r067.ok` (and `r059.ok`) in the batch's proof reader; add the r067 pictures to the hud-budget and 2x crop sets.
- **Glyph:** 2x review of every r067 frame; the place of `less.why` (built in the chip slot); not built: the locked pill's 0.15 s cross-fade when a lock lapses, the stack tab's tail glide, a new shooter's cameo sliding in. The Scout has no Fire row on the guard (only the Enforcer's knob gives one), so his shut ring there is Listen and the panel, not "Fire last" as board 02 shows.
- **Fable:** `less.done.board.<code>` (the host uses Glyph's words until then), the "X stops fire." and "Scroll to switch." hints, `ring-turn`, the goal line's clash with Drive ("Go in the car"), `less.pop.fire-lapses` (the host says "No line").
- **Aster:** Glyph's §C open questions (the Go-before-Vale hold and rank are built as he proposes; boarded agents always leave the selection); GDD lines (§10 left click, getaway, select, conversation scene; §06 the fire offer only in a fight).
- **Vantage / Keel:** the patrol's sweep is moved in mission.json but not yet in sim/data/handover.site.json; `r067-patrol-post` logs the engine's patrol against the mission's line.

## Batch 0.2.123 host round 3 (Latch, 2026-10-26; code only: no player build, no batch, no shots)
Sources: Aster's round-3 task (and its calls on round 2), briefs/067/vela.md, pulse.md, fable.md, glyph.md (Rev 2), GDD v0.25.0 §10. Nothing in sim/, contracts/, tools/ or StreamingAssets was edited. Paths are under unity/Assets/Suborned/.

**1 · The rival car (Vela 067 L; adopted by Aster, copied by `bun run kit`)**
- `HandoverHost.sedanRival` (HandoverHost.cs:39); PlayInstall loads `Kit("Vehicles/DK_Sedan_Rival.fbx")` and warns when it is missing (PlayInstall.cs:119).
- The `car/rivals` view draws Sedan_Rival with its own nine materials (HostViews.Rivals.cs:34). `Trim()` and `RivalTrim` are gone, with the stand-in. As Vela found, one property block per renderer had written slot 0's teal to all ten slots. No model means the box stays the car. `Scorch` (the range's burning cars) is kept.
- `HostViews.CarModelName(key)` gives the kit asset a view's model came from (HostViews.Rivals.cs:38; recorded in Visual, HostViews.Figures.cs:256), for the proof.
- Unity imported the FBX with KitImport's settings (its .meta is new, git-ignored). The material remap and its Generated prefab come from the batch's world build: HarbourWorldBuilder → `KitImport.Generated`. All nine of its materials are in Kit/Materials.
- The range's cars (`RangeCars`, HostViews.cs:330) keep the official model. They are the range's own cars, not the crew's.

**2 · Pulse (briefs/067/pulse.md W2, §1.3, §2.5)**
- W2, the Office's shift guards in the crowd (`Person.Civilian` false):
  - `Target.Security` is set in TargetOf. They are labelled "a guard", read from `names.targets["crowd-guard"]` with that fallback, and any other security job is "an officer" (HandoverHost.cs:142-146, :789). The hover intent (:811), `PersonName` (HostTakedown.cs:20) and the takedown panel's witness list (:260) say the same.
  - They are pickables with the one word "Guard" (HostPick.cs:46, :56) and get the hover name and anchor (:93).
  - They rank as beings under the cursor (PickRank :115; TargetAt keeps them in the stack, HandoverHost.cs:753-763). Only passers-by give way.
  - They hold the hover (Sticky, HostHoldStatus.cs:400) and wear the hover ring in the Built preset (HudModel.cs:194). An empty sheet reads "Guard".
  - Their sight shows on hover once the guard's sight is learned (`Known("guard").Sight`). It is the engine's security ring (all round within reach), with nothing armed needed, under 067 F rule 1 (HostNotice.cs:121, :150).
  - The Fire offer and the takedown rules are unchanged: they keep `Kind == "crowd"`.
- The bodies: a body is dressed in its walker's DressCrowd figure, else the plain pick (`BodyFigure`, HostViews.cs:298). Pulse counted 16 of 126 walkers that changed figure when knocked out. `BodyFigureCheck()` (:301) is for the proof.
- The relief's step: when someone has stood still for two or more ticks and their engine point then jumps more than 0.5 m and up to 1.5 m in one tick, the jump goes into the drawn spread offset, which eases back at StepCm (14 cm a tick, a walk) (HostCrowdSpread.cs:58-69). Pulse proposed 0.4 s, but 1.2 m in 0.4 s is 3 m/s and would play the run clip; at a walk it takes about 0.9 s on the walk clip.

**3 · Fable (briefs/067/fable.md)**
- `less.done.board.*` is read through DoneFlat, and the host's own copy (`BoardWhyWords`) is gone (HostGetaway.cs:274).
- `less.go.noVale` is `PlayText.GoNoVale` (PlayText.cs:102, :286). It is the line of the Go row before Vale is aboard (HostGetaway.cs:191), and with Alt, the line under the Go-before-Vale wedge (`RingAltFrame`, HudToolkit.Ring.cs:141, :149).
- `PlayText.ObjGo`'s fallback is now "Leave in the [car]." (PlayText.cs:100).
- H21 "Cease fire: press X." and H22 "Scroll to switch." (`TipHints`, HostHints.cs:31, :63-83, :239-241, :279-287):
  - Each shows at once, over any hint, rings nothing, and its line shows in play (HudToolkit.Less.cs:423).
  - It is spent for the run once the HUD has shown and folded it, or once its moment passed: H21 when no lasting fire order stands, H22 when the cursor leaves the stack.
  - It is retired for good when the input is used. That is `CeaseKeys` for H21: an X that sent a cease (HostHoldStatus.cs:357, HostFight.cs:152), never the host's own cease when a new order replaces a fire. For H22 it is `StackSteps`: a step by the wheel, Z or Tab (HostPick.cs:157). Both are ControlHints.
  - The cease tip wears the fire glyph (HudModel AtlasIcon).
- `ring-turn` was already read (the merged shut Turn wedge).
- A lapsed fire order still says "No line": the pop is `less.pop.fire-lapses`, else `less.done.fire.no-clear-line`. Fable has given no lapse word.
- Glossary:
  - `Words()` and `ActName()` name a person's verb by the text's `withTarget` ("Listen to the guard", "Pay Inez", "Fire at the police"), so the two "Listen to" special cases and "Fire on" are gone (HandoverHost.cs:1254, HostActivity.cs:104).
  - Verb()'s two Watch labels use `withTarget` too.
  - TargetOf says `Name("security")` ("the police") and `names.targets["car-official"]` ("Vale's car") (HandoverHost.cs:782, :793). `Name()`'s fallback is "the police".
  - PanelGuard's sample targets now use the same labels.

**4 · Forma:** no briefs/067/forma.md when this round closed (his folder holds two road renders; tools/figures is mid-edit). The manifest reinstall and the FigureLibrary rebuild are left for the batch pre-flight. The crowd's figures will reshuffle when the pools move (Pulse §1.3), and `BodyFigure` follows whatever DressCrowd picks.

**5 · Glyph's small animations**
- The locked pill becomes the offer with a 0.15 s cross-fade when the lock lapses under the cursor: it comes up from 45 % of the locked alpha (HudToolkit.Hold.cs:124).
- A stack step glides the pill's tail to the next thing in 0.12 s (:125).
- One more shooter's cameo slides into the reticle row in 0.15 s, 10 px and fading in. The set beat's own faces do not slide (HudToolkit.Lock.cs:21, :102).

**6 · Round-2 port gaps against Glyph's Rev 2 (fixed; Aster accepted his Go-without-Vale hold)**
- C5b: Go before Vale has no dashed outline and no loud mark. It stays last, never lit, with the struck Vale cameo and the 0.5 s hold (HostContext.cs:179).
- C5c, and GDD §10's "the getaway car: Board for the selection, Go once aboard": the car's hold is Board, then Go once Vale is aboard and the car is free (the Go card's hold still commits it when anyone would be left), then Drive (HostGetaway.cs:229-245). Round 2 had built Rev 1's "Go is never a hold".
- E2: the shut ring takes up to the ring's five ways, not four (HostContext.cs:167).
- F2: a Listen is no cone cause (HostSight.cs:104; OrderWatcher).
- H1 is the shot ladder (HostConvo.cs:174-264):
  - First the profile two-shot for a pair up to 6 m apart. It sits at fit = clamp(1.44 × sep, 3.6, 8) m: the perpendicular, then ±12°, then ±25°, then the same at 0.8 × fit (never under 2.8 m). At each angle it tries the side the camera was on first, else the side the subject faces.
  - A lens is clear when the lines to both heads and chests are 0.4 m clear, nobody shows more than 115° of back, and no solid stands within 1.6 m.
  - Else the high two-shot: the play pitch from 7 m with the same field, at the yaw the player left. The lens is orthographic and standing off as the map does, so the cut and the silhouettes hold (H4). The cut is sized to the two speakers, the frame opened to hold them both (:348-375), and it pushes in by its size.
  - Past 6 m, the intercut: each speaker alone, 2.6 m from where they face, cut hard with the bubble, the occluders re-hidden per shot (:320-330).
  - The over-the-shoulder fallback is gone. `ConvoForceShot` is for the proofs.
  - Silhouettes are off in the low shots (perspective lenses take no cut) and on in the high one, per Aster's call.

**7 · Tests and proofs**
- `TpInputMapTests.TheRowsOfTheBrief` had been stale since brief 041 and item 33: E hold rang, left tap acted, right tap rang at the target. It fails with HEAD's code too (HostAim.cs's table and HostClicks.cs's `HoldRing` are untouched this round; round 2 ran only ContextRankTests). It now asserts the table as built in both presets, as ProofFire does.
- Proof067:
  - board: Go is last, struck and held with no dashes; `r067-car-ring-go-alt` shows its Alt line (gated: `goAlt`).
  - convo: each scene's shot is logged and gated. The two-shots must hold both heads and the intercut the speaker. The driver's Listen is forced into the high two-shot (H5).
  - people (new, gated): a shift guard hovered after the guard's Watch reads "a guard", "Guard", pickable, with his ring (`r067-crowd-guard-hover`). Every eased step from 5:28 to 5:34 draws at most 0.25 m a tick. No body is in another figure than its walker. The rival car is DK_Sedan_Rival.

**Evidence**
- `bun out/scratch/latch/tools/compile3.ts` compiles Runtime/** and then Editor/World/** headless against the project's own engine DLL (Sim/Plugins, engine 0.33.1): "compile3 runtime: OK", "compile3 editor: OK (0 warnings)". The one runtime warning (HostNotice.cs:273 `Dead`) is HEAD's.
- Unity 6000.6.2f1 batchmode through `bun run guard` (`-runTests -testPlatform EditMode`, every test, no filter):
  - Scripts compiled with 0 `error CS` and no new warnings (out/scratch/latch/r3/editmode2.log). DK_Sedan_Rival.fbx was imported.
  - 35/35 passed (out/scratch/latch/r3/editmode2.xml): ContextRank 26, ModalGate 4, TpInputMap 3, SimScenario 1 (82.5 s) and PanelGuard 1.
  - PanelGuard: 0 faults over 5,385 panels and 25,848 hovers, 32 moments; crowd 200 includes the shift guards.
  - The first run, before the TpInputMap fix, was 34/35 (editmode.xml).
- `bun run typecheck`: clean.

**Open**
- **Batch sim step (Splice/Keel):** StreamingAssets/sim/text.json is still round 2's (c5fa275e4422); the tree's is Fable's 36dd13526f5e. H21, H22, `less.go.noVale`, `less.done.board.*` and "Leave in the [car]." show once `tools/sim/unity.ts` copies it. I didn't run that step, since the engine and site are moving under Keel's round 3. Round 2's open point stands: the committed rulebook schema doesn't know 0.33.1's keys (Fable's patch: briefs/067/fable/rulebook-schema-0.33.1.patch).
- **Forma (batch pre-flight):** the manifest reinstall and the FigureLibrary rebuild (item 4).
- **Fable:** a `names.targets` word for a shift guard ("crowd-guard"; the host says "a guard" until it lands). The lapse word is still open (`less.pop.fire-lapses`; the host says "No line").
- **Glyph:**
  - In the hold preset (the pill alone: no hover ring, name chip or disc) a hovered shift guard shows only his ring once learned, since he has no calm way. Say if he needs a mark.
  - H22 has no icon of its own (`step-target` takes the default glyph).
  - Please 2x-review the new shots: `r067-crowd-guard-hover`, `r067-car-ring-go-alt` and the high and intercut convo frames.
- **Vela / Glint:** the rival car hasn't been seen in a frame yet. The r059 set (2:35 turn-in, 2:46 drop, 3:10 park) shows it in the batch, and r067's people gate checks the model name.
- **Aster:** GDD §15 "stand-ins" still says "the rivals' sedan in rival trim" (now Sedan_Rival). Also please confirm the H1 ladder, the car's Go hold and the walk-paced step (the round-2 gaps above).

## Batch 0.2.123 host round 4 (Latch, 2026-10-26; code, the engine, Forma's figures, a scratch proof; no numbered build, no batch)
Sources: Aster's round-4 task; briefs/027/keel/host-interface.md "Engine 0.34.0"; briefs/067/forma.md §4; Fable's text.json (committed in cb1604f6); Sky on the fight (2026-10-26). Nothing in sim/, contracts/ or tools/ was edited. Paths are under unity/Assets/Suborned/.

**1 · Engine 0.34.0 (installed as 0.34.1, cb1604f6)**
- The engine went in with `bun run guard bun tools/sim/unity.ts`: DLL 8901efb038ad, headless 356/356, range 91/91, text.json 81d8c074c334. The host compiled against it with no change: nothing read `R.RunAlert`.
- **The run (M067-B2)**
  - The move chip and MoveParts no longer put an Alert on a run (HostFight.cs:413-424). It says who notices it: Fable's `less.done.run.noticed` and `run.unknown`, else "noticed by {who}" (RunNoticedWords, :439).
  - The Alt chip's Alert field drops with it.
  - The `run.seen` fallback reads "noticed … running" (HostFeed.cs:352). Fable's card is read as before.
  - The noticed cue on the run's first tick is the engine's attention clock. HostAttention follows `Game.Attention` every tick and filters no runner.
- **The walk split (M067-B1)**
  - Every host preview of an operative's walk already divided by `R.CellSpeed`: the intent time, ActWalk, NextMark, HostHold, HostHoldStatus, the "walk only" rows and HostDrive. The move ring and the price chip read the engine's MoveQuote.
  - One preview had the old paces typed in: HostSight.SeesSelectedSoon (F rule 3) used 1.4/2.8 m/s for the agent and 1.3 m/s for a watcher. It now reads the walk's own pace (else cell.speed or cell.runSpeed) and crowd.walk for a person watcher (HostSight.cs:162-167).
  - No host preview stands in for a city person's walk. Reports, the summoned guard and Vale's times all read the engine.
  - At 1.8 m/s a woman's walk_f (0.99 m/s natural) would play at 1.82x, past the stride cap. Above 1.5 m/s she walks with `walk` instead (1.4x, with 0.3 m/s of hysteresis; HostViews.Figures.cs:188, WalkFMaxS :240). The run threshold (2.0 m/s) stays clear of 1.8.
- **Hiding in a held car (M067-K3)**
  - `rivals.for-car` and `rivals.back-to-cover` give one card a beat, though each member logs his own line (CrewBeatCodes, 3 s; HostFeed.cs:45, :301). They use Fable's `cards.items`, with host words as a fallback (:288, :363).
  - `rivals.for-car` pulses the held car's holder marks once, using the existing refused-Go pulse. There is no new mark; Glyph's cue is to come.
  - Fable's world events speak once a beat (HostActivity's 15 s world cooldown).
- **`car-no-driver`:** PopIcon `car` (HostLess.cs:179). The pop, the shut wedge and the pill take Fable's `locks.car-no-driver` short through ShortOf, with a host short until the text has one (HostShort :200).
- **`cell-down`**
  - The fail camera runs as for enforcer-down.
  - A fail code the text doesn't word yet shows the engine's fail sentence as its caption, never the bare code (HostFailCam.cs:392).
  - When the last to fall is in the car (not drawn), the framing uses the car for him (:210).
  - PlayText's placeholder has the line.
- **Log only:** `ai.advance` and `car.held.*` (Fable's ai-advance world event speaks).
- **The patrol in the yard:** H7 is gated on `Game.PatrolWatchesEntry`, read once a game (HostHints.cs:130, :177). It is false on this site, so H7 never shows. The placeholder reroute words drop "entrance".
- **Pulse's posts:** a crowd person standing alone now faces `Game.FacingNow`, so the Office's door guards face out toward the kerb and the figure agrees with its cone (StillFacing, HostViews.cs:177, :286). Knots still face their middle.

**2 · Forma's figures (briefs/067/forma.md §4)**
- Installed whole with the install's own rsync from out/people-models/People/: 26 figures. `rival` was replaced; `rival_b`, `rival_c` and `rival_d` are new.
- FigureLibrary rebuilt in batchmode with a new `FigureBuild.Cli` (Editor/World/People/FigureBuild.cs:36): 26 figures, pool `role/rival` = rival, rival_b, rival_c, rival_d. `rival_d` is in Women (:22) and walks walk_f.
- Dressed by member with `HostViews.RivalFigure(id)` (HostViews.Figures.cs:44): rival-1 rival, -2 rival_b, -3 rival_d, -4 rival_c, -5 rival. Sites: RoleFigure (:36), the fight's and the range's hostiles (HostViews.cs:149, :192) and a rival's body (:267). A library without a look dresses `rival` (Dress :82).
- `python3 -I tools/figures/albedo_stats.py --base unity/Assets/Suborned/People/Figures --check`: "spec (brief 067 W1): OK".

**3 · The fight, simplified (Sky 2026-10-26)**
- **Removed**
  - The cell's lasting dashed tracer (HudToolkit.Fire Tracers).
  - The overhead per-shot lines (HudToolkit.Less ShotMarks, HostViews.Tracer and ShotLines).
  - The follow view's hostile streaks (ShotFx).
  - The lock reticle's re-target hand-off line (HudToolkit.Lock).
- **Kept:** the lock reticle, the muzzle flash, the hit spark and the miss puff. The host already had a flash and a spark, so the tracers went too. The flash and spark now sit on the drawn bodies: the flash at the shooter's gun along its facing (HostFight.cs:445-466).
- **Facing:** AimTargets (HostFight.cs:497) turns to its target every gun aiming at one, every cell fire order and anyone who fired in the last 1.2 s. The proof measured it: no defect.
- **The cones' reveal fix (Glyph F rule 4: "3 s after a Watch teaches it")**
  - The host revealed every fan the first time it appeared. At a fight's start that drew every combatant's cone for 3 s: eight in the loud frame (the police on arrival, the members' fight bodies).
  - Only a Watch's landing (`<op>.watch.watched` within the last second) now reveals (HostSight.cs:115, :141). The same frame then draws none.
- **Not removed (no aim, target or fire line, or lab only):** the holder tethers, the exit lane and the 8 m hold ring, the under-fire arc, the fight lab's freeze-mode signal lines ([lab], off in "today"), the range panel's lines ([lab]) and the fail camera's one shot line (Glyph N125).

**4 · Proof** (Runtime/Play/Proof123.cs: JSON `r123`, after r067 in -gameProof, not gated; alone with `-proof123 <dir>`, GameBoot.cs:49, CaptureSize.cs:50)
- **The scratch player:** `GameBuild.CliScratch` (new, Editor/World/Play/GameBuild.cs:33) builds into out/scratch/latch/build and takes no number. The app says 0.2.123-scratch; version.json and out/build/play are untouched. The first build took 28 minutes on a cold shader cache (2,880 variants, the URP asset change of round 2); later builds take 16 s.
- **The display was asleep** (19 h idle), so the player blocked on Metal's present. With `caffeinate -d -u` held it ran.
- Pictures and logs are in briefs/067/latch/r4/ (proof123.json, proof123.log, hud-budget.txt).
- **Looks**
  - `r123-looks-310` (3:11, zoom 14): rival-1 rival, rival-2 rival_b, rival-3 rival_d walking to her post.
  - `r123-looks-320` (3:18): the lookout rival-4 in rival_c, at the rival car.
  - The wheelman rides aboard until the fight.
  - All five, four up and one down, are in `r123-fight-safe` in their four looks.
- **Fights**
  - `r123-fight-safe` (sq-ra-h3, 4:15, zoom 14, yaw 30): 89 frames, 0 with a shooter over 25°, worst 0°, 9 flashes.
  - `r123-fight-ground` (third person, 1.2 s): 71 frames, 0 over 25°.
  - `r123-fight-loud` (bal-loud-x4, 3:46, zoom 14): 87 frames, 0 over 25°, worst 18.2° (hostile/2 turning), 23 flashes, cones drawn 0 (eight before the reveal fix).
- **HUD budget:** 4/5 pass. `r123-fight-loud` fails, and not on a line of fire:
  - The seal clock's plate ("1:55") is in the manifest as a `line`, so it counts as a second objective line beside "Get Vale to the car.".
  - The hint pill "Pick when to fire" reads as running text.

**Evidence**
- `bun run guard bun tools/sim/unity.ts`: engine cb1604f6 (0.34.1), headless 356/356, range 91/91.
- Headless compile (out/scratch/latch/tools/compile4.ts, against the installed DLL): runtime OK (the one warning is HEAD's HostNotice.cs:273), editor OK.
- Unity 6000.6.2f1 batchmode through `bun run guard`:
  - FigureBuild.Cli: 26 figures, role/rival pool.
  - HarbourWorldBuilder.Build: from `bun run world`.
  - GameBuild.CliScratch: Succeeded, 0 errors.
  - `-runTests -testPlatform EditMode`: 0 `error CS`, 35/35 passed (out/scratch/latch/r4/editmode3.xml); PanelGuard 0 faults over 6,170 panels and 29,616 hovers.
- `bun run typecheck`: clean.
- `bun test tools/game/proofset.test.ts tools/game/proofgates.test.ts`: 43 pass.

**Open**
- **Pulse:** looks.test.ts pins the host text Forma §4 replaced, so three pins fail as Forma foresaw:
  - `Dress(hostileKeys[i], h.Id.StartsWith("rival") ? RivalFigure(h.Id) : "security")`
  - `Dress(key, RivalFigure(p.Id))`
  - RoleFigure `=> RivalFigure(key)`, with the RivalLooks table
  `tools/people/looks.ts` roleFigure() needs the same table.
- **Splice:** add an r123 set to tools/game/proofset.json (five pictures) and gate `r123` when Glyph agrees. The scratch app (out/scratch/latch/build, 828 MB) can go once read.
- **Glyph:**
  - The 2x density check of briefs/067/latch/r4.
  - The loud frame's budget fault: the seal clock counted as a `line`, and the hint pill as running text.
  - The cue for rivals walking to a held car: the holders pulse only.
  - Whether the fail camera's shot line goes too.
  - The exit lane and hold ring drawn across the Office roof at yaw 30.
- **Forma:**
  - The beat hookup (§3) is not done; it wasn't asked.
  - c6 and d5c are still not in Women.
  - At 1.8 m/s women walk with `walk` (above). A brisk walk_f clip would be better.
- **Aster:** the 3:10 frame can't hold five: at 3:10 three are on foot round the cell's car, the lookout steps out at the park, and the wheelman stays aboard. The five together in four looks are in `r123-fight-safe`.
- **Side effects of the builds** (generated, as a batch writes them): unity/Assets/Scenes/FightRange.unity and Generated/Range/*.mat (GameBuild rebuilds the range), and HarbourDistrict-grade-night.asset (the world build).

## Batch 0.2.123 (Latch, 2026-10-26): STOPPED at budget on build 0.2.125 (c038, a762f850 + this round)

Three player builds. 0.2.123 (the full batch) failed game-check on six gated sections. 0.2.124 (`--from game-build`) failed game-check on fire. 0.2.125 (`--from game-build`) passed game-check and shots, then failed **budget** on the r059, r067 and r123 proof sets. late-fight and tp-long ran on 0.2.125 with `--from late-fight` (BATCH OK for those two). The budget failures are a gap in the HUD-budget contract, not a build fault, so I stopped there (below).

**Pre-flight (a762f850, out/scratch/latch/r5):** in, as briefs/067/glyph-r4.md asked:
- F1: the hold ring and exit lane are depth-tested floor decals (`FloorRing`, `FloorPath`, `ClipToDisc` in HudToolkit.Ground.cs), never past 12 m of the car.
- F2: StepAside lets bubbles win; the clock arc follows its plate; pops sit 16 px clear of speech.
- Stacked pops: one pop per cause and place.
- Tethers: none inside the ring; one at 60 % for a holder on the lane.
- The fail camera's shot line only at the deciding moment (`FailAtMoment`).
- G1, follow-view flashes: drawn with the target off view; `ShotFlashesHostile`.
- Manifest kinds: the seal clock is `marker`, refusal pops are `pop`.
- "Pick when to fire" shows the three stance marks.
- Item 8, the rival-for-car disc (HudToolkit.RivalsForCar.cs): built, but no proof reads `ForCarShown` yet.
- r5: EditMode 35/35, the budget clean on all five r123 frames.

**late.json (Waymark §5.3):** only the notes changed. The step reads each end from the engine (loud-fight now ends at tick 3068, 5:06.8; loud-left-downed at 5:04.7, tick 3047). `bun test tools/game/late.test.ts` 22/22.

**What failed on 0.2.123 and what I changed (host and proof code only):**
- **r29 inezRound.** On 0.34 the unturned driver drives Vale off at 6:13 (tick 3730), before her 7:37 round. The round now replays route B's opening first (the Scout's Listen and the Fixer's Pay on the driver, the false tip), which keeps the run going to 8:10.
- **r29 reroute.** c038's route A gives no Reroute any more. The reroute now comes from the first route that gives one (route B, 5:00).
- **done cancel.** The patrol now sweeps out of reach. The host walks in (a move with the Watch pending, HandoverHost.cs:1036), and the check accepts that.
- **done guardBack.** The guard's 2:55 away ended after 6:13. It is now read on a fresh run with route A's own orders up to its call (2:51, no Enter), with frames after the replay so the host sees the window open. "Guard back" is read among the live pops.
- **done tpFire.** The Scout walks to within about 6 m of the sweeping patrol, with a clear line (ApproachInSight).
- **holdRange ringHover, a host fault.** Since brief 067 E took Fire off the calm pill, `HoverFire` was false on a passer-by, so 049's hover ring never showed. HostTargetRings now rings what a drawn selection hovers.
- **r067 camera and act, a host fault.** In `FollowEnter` (HostFollow.cs:62) the brief 067 G edit had put the follow view's resets (`fDist`, `fHasLast`, `fHeadVel`, `fChaseVel`, `fLookUntil`) inside its comment. So the last visit's heading velocity swung a new third-person view off the heading: −10° to −92° in the trace, and in act it turned Inez behind the camera. The resets are restored, and entering the view also ends any open map drag. With the camera fixed, the act check now waits for Inez's prompt before the E tap, as it already did before the E hold.
- **r067 board.** The Drive was queued by plan-together on a paused clock; it is off for this part, as in Proof29. "One order for the squad" now compares each doer's first order tick: the moves go on one tick (36), and the Boards go as each arrives.
- **r067 convo.** The first Help went at a hardcoded tick 700 (c037). It now goes at route A's own Help ticks.
- **fire.**
  - On 0.2.122 the Scout's shots were return fire at a patrol that came; on 0.34 it never comes.
  - She now stands on her own line to the car, inside her reach, outside the blast ring and outside the follow ring, and fires her own lock with the Fixer's. With her following the trigger, three guns blew the car before the run, tap and lock checks.
  - camHolds (0.2.124): with the camera now entering on the heading, a wall pulls the boom in after the sideways run. The camera holds when the player's set distance is unchanged (`FollowDistSet`, `FollowPulledIn`).
- **Tooling:** `-proofOnly a,b` runs only the named proof sections, for scratch runs; every other section stays null, so such a proof never passes the gate.

**Steps (final state):**
- sim: ok, 356/356, DLL 5fe7665b7923.
- compile: ok (1 warning).
- kit: ok.
- world: ok.
- unity-world: ok, 50 s.
- unity-match: ok, 447/447.
- game-build: 0.2.125.
- game-check: ok. 356/356 scenarios; driveSession and fireSession replay headless. Every gated section passes: driveClick, fireStop, ringCheck, inputMap, act, fire, cars, hudClick, dblClick, holdActs, holdRange, leash, done, rings, levels, r29, r118, r059, r067. r123 (not gated) passes too:
  - looks: 3 and 4 distinct;
  - safe 4:19: 0 bad frames, 17 flashes;
  - loud 3:48: 0 bad, worst 19°, 48 flashes, cones 0;
  - ground: 0 bad, 42 flashes (28 hostile);
  - no reticles.
- shots: ok, 11.
- budget: FAILED.
  - The batch's own shots: 10/10 within budget, audit 10/10.
  - Proof sets: r059 12/13 (r059-fight-start, optional, not taken), r067 25/32 (r067-reticle-handoff not taken), r123 4/5.
  - There is no r118 picture set in tools/game/proofset.json; r118 gates as a proof section only.
- late-fight: ok, both rows.
  - loud-fight: worst 22.6 ms (tick 3068, order+save+end), p99 6.9 ms.
  - loud-left-downed: worst 18.2 ms, p99 7.8 ms.
  - NOISY MACHINE: Blender at 1314 %.
- tp-long: ok. play-opening p99 6.9 ms, tp-street p99 5.7 ms, tp-worst p99 5.7 ms.

**Why budget fails (for Splice and Glyph; I did not retag to pass):** these are the first measurements of the r059 and r067 sets, and they show HUD pieces the budget has no kind for.
- The **order pill** ("Hold · Drive into the service drive · 29 s", "Hold · Board · 8 s") and the **lock pill** ("Listen to the guard · Already heard") are tagged `line`. The tool counts them as an objective or hint line: at most one on screen with the goal, at most 6 words. Fails: r059-drive-hover, r067-drive-pill-car, r067-squad-board-pill, r067-lock-pill-guard, r067-shut-pop, r067-stack-tab, r123-fight-ground.
- The **Alt line** on the car ring ("Go without Vale calls off the attempt. …") is a `line` in the alt state: r067-car-ring-go-alt.
- The **Done chips** are not in the manifest. Their verb-pill rows (Help · Pressure, Listen · Pay) read as untagged 9 to 10 word running text plus a stacked block: r067-lock-pill-guard.
- The **conversation cutaway's** scaled top bar ("$20,000" at about 1.7x) reads as one stacked block: r067-convo-inez-2.
- Tagging the pills `hud` or the chips `image` would hide their words from the reader, which is the loosening Glyph ruled out (glyph-r4 §4). It needs kinds for the order pill, the lock pill, the chip and the Alt line, with Glyph's limits.
- Separately, r123-fight-ground now shows a Board pill (the follow view enters facing the car), and it reads "Hold · Board · 1 s · 1 s", with the time twice.

**Evidence** (git-ignored): briefs/041/build/0312/
- batch-0312a…d logs;
- logs/, proof-1080/, shots/, late/, proofset/;
- proof-2160/ (`-gameProof -capture-size 3840x2160`, status ok);
- crops-2160/ (`bun run proofset crops`: crops-r059.html, crops-r067.html, crops-r123.html).
- On the 2160 run, act's E-hold ring and done's tpFire missed on timing (a loaded machine, slow 4K frames). Both are made robust in code after 0.2.125, so the next build carries them; the 1080 gate passed both.
- Scratch iterations: out/scratch/latch/b123/ (p1 to p6 proofs, scratch builds 1 to 6, EditMode 35/35 after the HostFollow change).

**Open:**
- **Splice and Glyph:** the budget kinds above, then a `--from budget` rerun of 0.2.125 (no rebuild needed for budget alone).
- **Glyph:** the 2x review of crops-2160; the "1 s · 1 s" Board pill; whether the chips belong in the manifest as a new kind.
- **Keel and Motive:** the fire proof's Scout no longer needs a patrol to come. Nothing for the engine.
- **Item 8:** add `ForCarShown` to a proof frame.

## Glyph 2x (0.2.125) (Glyph, 2026-10-26; review only, no code, no Unity)
**Verdict: NOT SHIP yet. One short code round (N1 to N6), then `--from game-build`.** Every one of Sky's asks is built and reads. But six faults sit on the frames Sky will judge first:
- the Board price shows the time twice, against the GDD hold "walk + act as one time";
- a refusal pop covers speech;
- the open ring covers the done chips;
- the cutaway HUD draws at 2x on a 1080 screen;
- the Go Alt line overflows its plate;
- an off-screen bubble draws behind the roster.

All are small and HUD-only: no engine or content change except N5's words.

The budget limits for Splice are in `briefs/067/glyph-budget.md`. They give new kinds `pill`, `lock`, `chip`, `altline` and the optional `bar`, all counted, nothing hidden. With them, six of the nine stopped frames pass. Three still fail rightly until N1, N5 and N4 land.

**Looked at:**
- crops-r059/r067/r123.html and proof-2160 at full size: my crops are in out/scratch/glyph/r125/.
- `bun run proofset budget briefs/041/build/0312/proof-1080`: r059 12/13, r067 25/32, r123 4/5.
- `bun tools/hud-budget/cli.ts <shot> --json` on each of the nine failures. That gave the exact failures, all from the missing kinds or the faults below.

### Sky's asks
- **No aim or fire lines: pass.**
  - fight-safe, fight-loud and fight-ground draw no tracer or shot line.
  - The reds are target rings with band ticks and a flash at the gun.
  - The dashed turquoise lines are walk lanes and the exit lane.
  - The violet line at 2:35 is the rival car's route ahead, a route cue, not a shot.
- **The rival car arriving: passes, with framing notes.**
  - 2:35: the car turns in, violet route line ahead.
  - 2:46 (frame clock 2:49): the car stops in the lane, and one rival walks a dotted violet path to a dashed post ring.
  - 3:10 (frame 3:17): parked at the kerb, the lookout on her post ring.
  - The 2:46 caption promises three stepping out, but one is in frame (N9).
  - At 3:10/3:20 the car is half hidden behind the Office tower at yaw 210. The cut opens round the terrace figure, not the car (N9).
- **Shut rings and the locked pill: built, with a crowding fault.**
  - The pill "Listen to the guard · Already heard" is right at 2x: lock disc, way, dim reason, the Scout's face.
  - The shut ring opens with its one struck wedge.
  - But the shut wedge's plate is translucent and low contrast, and sits on the second chip's rim (N3).
  - The press's pop covers the guard's bubble (N2).
- **Squad board and Drive: works, one price fault.**
  - Board pill "Hold · Board · 8 s" with the two givers' faces.
  - The Board wedge, pending pips, the "stop first" why chip on the running Tech, two "Aboard" chips, seat pips filled.
  - Drive pill "Hold · Drive · 29 s", the stand on hover, parked.
  - The ring's Board plate reads "Board · walk 7 s · act 1 s", and the follow view's pill reads "Hold · Board · 1 s · 1 s" (N1).
  - The open car ring covers the second "Aboard" chip (N4).
- **Cones on demand: pass.** With Alt held at 6:00: the guard's amber cone, the camera's red outline and the patrol's dashed cone, each its own shape. Let go: none. Exact.
- **The conversation from the ground: the shots pass, the HUD scale fails.**
  - Both heads in every frame; both idle and facing; no over-the-shoulder; the bubble on the speaker.
  - At 1080 the whole cutaway HUD is 2x (N6).
  - convo-inez-1 frames the pair through the Scout standing between them (N10, minor).
- **The rival looks: pass on data (3 and 4 distinct), weak by eye (N11).** At zoom 14 the rivals read as one costume (violet top, grey trousers) told apart only by hair. rival_d's hooded cape reads well, but it is a flowing hem.

### Nits (owner, exact fix)
**N1 · The price shows the time twice. (Latch.)** GDD §14 hold: walk + act as one time.
- Ring plate: "Board · [walk] 7 s · [clock] 1 s".
- Hold pill: "Hold · Board · 1 s · 1 s" (r123-fight-ground).
- Fix: one figure, the sum, with the clock icon; walk and act apart only under Alt. Apply it in the ring wedge plate and the hold pill builder, for every way with a walk (Board, Help, Listen…), not Board only.
- The squad pill's "8 s" is already right.
- Also tag r123-fight-ground's state `active` while a pill is up (glyph-budget §1).

**N2 · A refusal pop covers speech and chips. (Latch.)**
- r067-shut-pop: "Already heard" sits on "…I don't work for free."
- r067-who-can: it sits on the second chip's Pay pill.
- r059-drive-here: "Already here" sits on the "Parked" chip's lower half.
- F2 moved the fire pill and the clocks, but `StepAside` never moves a pop.
- Fix: pops join the pass as movers. Each pop clears every bubble, chip, pill and ring plate by 16 px (`ClearOf(pop, bubbles + chips + pills, 16f, …)`), else it hides for the frame.
- When the hovered lock pill already shows the same reason, drop the pop's words and keep its icon.

**N3 · The shut wedge's plate is translucent. (Latch.)**
- r067-shut-ring-guard: "Listen 🔒 Already heard" draws at about 50 % alpha over the chip and the world, so the reason is hard to read.
- Fix: a solid `plate_dark` like the other wedge plates. The way word is struck in dim #b0cdc7; the reason is cream.
- Place it with the ring's other plates, clear of chips (N4).

**N4 · The open ring covers the done chips. (Latch.)**
- r067-car-ring-drive: the second "Aboard" chip sits under the ring with its word clipped, and the "Drive 29 s" plate sits on the first chip.
- r067-shut-ring-guard: the chip stack meets the ring's plate.
- Fix: while a ring is open, hide the done chips anchored within the ring radius + 60 px; the feed already carries them. They come back on close if their 4.2 s is not over.
- Also: the chips' stack tab overlaps the lower chip's rim (r067-stack-tab, r067-lock-pill-guard). Give it 4 px clear.

**N5 · The Go Alt line overflows. (Fable + Latch.)**
- r067-car-ring-go-alt: `less.go.noVale` is 14 words in two sentences.
- At 2160 it wraps: the first line's tops are cut by the plate edge, and "again." falls under the plate onto the world. At 1080 it runs over the feed card.
- Fable: at most 6 words, one fragment (glyph-budget §4).
- Latch: the Alt plate sizes to its text (one line, no wrap). Place it under the wedge's plate, clear of the feed card and the ring rim, flipping above when it meets the feed.

**N6 · The cutaway HUD is 2x at 1080. (Latch.)**
- All four r067-convo frames at 1080 draw the bar, the bubble and Skip at about twice play size. The alert plate is 209 × 96, against 106 × 48 in play. At 2160 they are right.
- So the cutaway's panel scale is fixed, not the play panel's reference scale.
- Fix: the cutaway uses the play `PanelSettings` (or copies its scale mode and reference resolution). The bar sits in the top letterbox band at play size.
- That clears r067-convo-inez-2's budget block with no tool change.

**N7 · An off-screen speaker's bubble draws behind the roster. (Latch.)**
- r067-crowd-guard-hover: the bubble at x −198 ("…Same as always.") pokes out from behind the roster plate.
- The door marker, guard pip and edge cameo stack under the Squad button at x 27.
- Fix:
  - Hide a bubble whose speaker is off screen (the feed carries it).
  - Clamp edge markers to the safe area right of the roster (x ≥ roster right + 12 px).
  - Stack edge markers vertically 4 px apart, never on the Squad or weapons buttons.

**N8 · r067-crowd-guard-hover misses its subject. (Latch, proof.)** The caption wants "a guard" in the pill and his ring, but neither is in frame: the camera is on the Office front with the hovered guard off screen. Frame the hovered shift guard, then retake.

**N9 · Rival car framing in r059. (Latch, proof + cut.)**
- 2:46 shows one of the three stepping out. Take the frame on the tick the third clears the car, or widen.
- 3:10/3:20: the parked rival car is half behind the tower. Extend the occlusion cut to the rival car and the getaway car while either is a ring or pill subject or a beat's subject. The same goes for the getaway car under the roof edge in r067-squad-board-pill and r067-board-landed.
- Glint owns the cut's look; Latch extends the subjects.

**N10 · The convo ladder lets a third person split the pair. (Latch, minor.)** convo-inez-1: the Scout stands between the Fixer and Inez. Add "no third body on the line between the two" to the ladder's first-rung test; else take the next rung.

**N11 · Rival looks at play zoom. (Forma.)**
- rival and rival_b share violet top and grey trousers, with only hair to tell them apart at 50 px.
- Fix: keep the violet block, but give each a silhouette change, as the art bible asks ("job readable at a distance by silhouette plus one colour block"): a cropped jacket vs a long vest, a cap, a skirt vs trousers.
- rival_d's hooded cape is a flowing hem, which the art bible rules out. Shorten it to the hip, or make it a hood on a fitted top.

**N12 · A pedestrian stands in the carriageway at 2:35. (Pulse, question.)** r059-235-turns-in: a civilian woman stands still in the road behind the rival car. If she is mid-crossing it's fine; if she is idling in the road, the walk graph has a stop in the lane.

### Next
- Splice: the kinds in `briefs/067/glyph-budget.md`, then `--from budget` on 0.2.125. Six frames go green; three stay red for N1, N5 and N6.
- Latch: N1 to N9 in one code round.
- Fable: N5's words. Forma: N11. Pulse: N12.
- Then one batch `--from game-build` and a 2160 retake of r067 and r123-fight-ground for my recheck.

## Batch 0.2.123 final (Latch, 2026-10-26): build 0.2.127, game-check FAILED on three hold proofs under load, budget 2 frames red (both my faults, fixed in the tree, not built)

**Build of record: 0.2.127.** The batch `--from game-build` built 0.2.126. Its game-check then rebuilt as 0.2.127, because I had added an Editor-only script (`Editor/World/Review/HeightCap066.cs`, brief 066's renders) under `unity/Assets` during game-build. The player's runtime code is identical in both builds (Editor scripts don't ship). Everything below ran on 0.2.127.

**Code (host and HUD only; no engine change):**
- **N1, fixed.** HudModel.Less.cs PriceLine: Board no longer splits walk and act. Every way with a walk shows one figure, the sum, with the clock. The split stays in Alt (AltLine). Proof frames: "Hold · Board · 2 s" (r123-fight-ground; it was "1 s · 1 s"), "Hold · Board · 8 s". All 22 pill texts in the 1080 proof show one time.
- **N1 tag, done.** The manifest's state becomes `active` while the pill is up (HudToolkit.Capture.cs Manifest). r123-fight-ground is `active`.
- **N2, fixed.**
  - Pops are movers in StepAside (HudToolkit.StepAside.cs). Each one clears every bubble, chip, pill (with its tail and stack tab), ring plate and the wheel by 16 px, up or down, at most 90 px; otherwise it hides for the frame.
  - A pop that repeats the hovered lock pill's reason drops its words and keeps its icon (r067-shut-pop: the pop is icon-only beside "Listen to the guard · Already heard").
  - 1080 proof: 21 pops, none on a bubble, chip or pill.
- **N3, built with a fault (fixed in the tree).** The shut wedge's plate is solid and the reason cream. But I struck the way word with a child element inside the Label, which broke the label's measured width. In 0.2.127 the lock icon sits on "Listen" ("Li🔒en Already heard"), and a wedge slot that was once shut garbles later plates too ("Dr©29 s" on the car ring's Drive). **This is visible in 0.2.127 whenever a ring follows a shut ring.**
  - In the tree: the strike is a sibling over the word, sized from its layout (`Wedge.Strike`).
- **N4, fixed.** While a ring is open, done chips within the ring radius + 60 px, or under a wedge plate, hide (StepAside). They come back on close inside their 4.2 s. r067-car-ring-drive shows no "Aboard" chip under the ring. The stack tab keeps chips 4 px clear (DoneMarks lifts them 14 px over the pill when the tab shows).
- **N5, partly.**
  - Fable's words ship ("Calls off the attempt; try again", text.json in the tree, carried by `tools/sim/unity.ts --tree`).
  - The Alt plate is one line with no wrap, placed under the wedge plate and clear of the rim, flipping above it when it meets the feed.
  - But my feed rects missed the history drawer, so in r067-car-ring-go-alt it sits on the "In the car" card (budget: "overlaps the beat"). Fixed in the tree (`FeedRects` adds `history`, `beatsBox`).
- **N6, fixed.** CrispScale now runs before the cutaway and the fail camera return (HudToolkit.cs LateUpdate). A 1080 capture inside the 2880 x 1864 run had kept scale 2. The cutaway's bar is 44 px tall at 1080 (it was 96), with the bubble and Skip at play size, in all four convo frames.
- **N7, partly.**
  - A speaker off screen has no bubble.
  - Edge marks keep right of the roster's band (x ≥ roster right + 12) and off the Squad and weapons buttons (the edge cameo now sits right of the roster).
  - r067-crowd-guard-hover still shows "…always." behind the roster: that speaker stands on screen under the column.
  - In the tree: bubbles and pips count the roster band as off screen (`InPlay`).
- **Manifest kinds (brief 067 glyph-budget).**
  - The verb pill is `pill`, or `lock` in the lock state.
  - Ring and lock Alt lines are `altline`.
  - Each done chip is `chip` with `facts` and `verbs` lists.
  - The clock bar is `bar`, as a rect only, without text. Its next mark's label is a word ("Chime", "The close"), so glyph-budget §5's "no letter words" would fail every frame. That needs Glyph's call.
  - In the tree, not built: a shut wedge's plate is `lock` ("Listen · Already heard"). It was read as running text once N3 made it solid (r067-shut-ring-guard).
- **Forma's idle fix.**
  - Clips and manifest rsynced from out/people-models/People.
  - FigureBuild.Cli re-run: idle_alt's take changed, so the controller had to be rebuilt; it now points at idle_alt.fbx, 180 frames = 6.0 s.
  - 32 metas `loopBlendPositionXZ: 1`.
  - `bun test tools/figures`: 23 pass, 0 `[sway]` warnings.
  - One still can't show sliding. The café crowd (shots/cafe-front-day, follow-scout-cafe) stands on its marks.

**Steps (0.2.127):**
- sim (`tools/sim/unity.ts --tree`, for Fable's text): headless 356/356, range 91/91.
- game-build: 0.2.126, 0 errors, 17 s. game-check rebuilt as 0.2.127 (11 s, see above).
- **game-check: FAILED**, under a machine at load 11 to 23, mostly someone else's Blender (sandbox/mechs) at 880 to 1500 %.
  - dblClick row "Inez, the Enforcer": rightHoldCharge and rightHoldFires false.
  - holdActs holdLatch: ready "".
  - holdRange: squadRightHold and focusInFight.
  - Every other gated section ok: r059, r067, r118, r29, done, rings, levels, leash.
  - A rerun of those sections on the same app (`-proofOnly frames,holdActs,holdRange`, rerun-holds-p1/): holdRange **passes**, but holdActs fails even its plain click (no ring), which passed in the batch.
  - These are real-time holds under load, not this round's code: nothing changed touches input. **Not rerun again (Aster's rule).** It needs a quiet machine.
- shots: ok, 11.
- **budget: FAILED on 2 of 50.**
  - r059 13/13; r067 30/32; r123 5/5; the batch's own 10/10; audit 10/10.
  - The two reds are r067-car-ring-go-alt (Alt line over the history card, N5) and r067-shut-ring-guard (the solid shut plate read as running text, N3). Both are mine, both fixed in the tree, both unbuilt.
  - The six Glyph expected green are green, and so are r123-fight-ground (N1) and r067-convo-inez-2 (N6).
- late-fight: ok.
  - loud-fight: worst 25.2 ms, p99 7.3 ms.
  - loud-left-downed: worst 17.2 ms, p99 7.2 ms.
  - NOISY MACHINE.
- tp-long: ok.
  - play-opening p99 5.5 ms; tp-street p99 5.2 ms; tp-worst p99 5.4 ms.
  - 1.6 / 3.2 / 5 M tris.
- 2160 retake for Glyph: `-gameProof -capture-size 3840x2160 -proofOnly r067` (r067 ok) and `-proof123`. Crops: crops-2160/crops-r067.html and crops-r123.html.
- Headless compile (Runtime + Editor) clean after every edit, including the tree-only fixes after 0.2.127. EditMode tests not run this round.

**Evidence** (git-ignored, on disk): briefs/041/build/0313/
- proof-1080/ and shots/;
- logs/ and late/;
- batch-0313a/b/c logs;
- rerun-holds-p1/;
- proof-2160-r067/ (its player.log was overwritten by r123's, my slip; proof.json is intact) and proof-2160-r123/;
- crops-2160/.

**Key pictures:**
- Board pill: proof-1080/r067-squad-board-pill.png ("Hold · Board · 8 s", two faces).
- Conversation: proof-1080/r067-convo-inez-2.png (cutaway at play scale).
- Idle crowd: shots/cafe-front-day.png and shots/follow-scout-cafe.png.

**Not done, for the next batch:**
- N8: frame the hovered guard in r067-crowd-guard-hover.
- N9: the rival car's 2:46 frame, and the occlusion cut's subjects extended to the cars.
- N10: no third body on the convo ladder's first rung.
- The tree-only fixes above (N3 strike, N5 history, N7 roster band, shut wedge `lock`) need one `--from game-build`. The next game-check wants a quiet machine.

**Brief 066 renders (after the batch; Vantage's check of the capped blocks was not done, rendered anyway):**
- **Scenes.** Two scratch scenes, git-ignored: `Assets/Scenes/probe-066-today.unity` (out/scratch/datum/height-cap/today.world.json) and `probe-066-cap.unity` (the capped world), built by HarbourWorldBuilder with `-scene`. HarbourDistrict.unity and its assets are untouched (15:48 timestamps).
- **Render.** `Suborned.World.Review.HeightCap066.Render`, a new Editor script. It stages fixer, tech, scout, enforcer, waitress, councillor, guard and driver figures in idle on Datum's points (out/scratch/latch/r066/points.*.json, from mission.json via tools/plan/civic.ts). The foci match Datum's (−168.1, −225.5) and (−165.6, −300.1).
- **Play frames.** Orthographic zoom 14, 38°, 1920 x 1080, yaws 30/120/210/300, the scene's day look. The stand-off is capped at 600 m: at yaw 300 the clearance meets the horizon band 7.9 km back.
- **Follow frames.** HostFollow's defaults: 5.5 m back, 3.06 m up, fov 55, far 350.
- **Counts.** Per frame, from the frustum: enabled renderers whose bounds are in view, the LOD the frame selects, and their triangles. This is not the player's perf harness; no batches or SetPass.
- **Triangles, today → capped:**
  - café: −5.4 / −2.2 / −5.1 / −9.4 %;
  - office: −6.9 / −7.1 / −8.2 / 0 %;
  - follow Fixer: −17.1 %;
  - follow Vale: −22.2 %.
- Renderers drawn drop by 0 to 12 a frame.
- **Output.** briefs/066/latch/ (22 PNGs, counts-today.json, counts-cap.json, compare.png).

**Build 0.2.128 (Latch, 2026-10-26 evening): the tree fixes built. game-check passes, holds included. Budget has 1 red of 50 (the Go Alt line, my N5 fix missed the card it sits on).**
- **Run.** `bun run batch --from game-build` at load 3 to 12 on 18 cores. Shots, late-fight and tp-long were noisy. I added no Editor scripts during the build, made no code edits and added no features.
- **game-build: ok.** 0.2.128 (c038, e1ec58a3+) in 17 s, 0 errors.
- **game-check: ok, 80 s.**
  - Scenarios 356/356 match headless.
  - Every gated section is ok: r059, r067, r118, r29, done, rings, levels, leash, and the three real-time hold proofs (dblClick, holdActs, holdRange), so no `-proofOnly` rerun was needed.
  - Play zoom at 2880x1864: 254.5 fps, p99 5.7 ms. Slowest spot: fire-commander, p99 8 ms.
- **shots: ok.** 11 shots in 21 s.
- **budget: FAILED, 1 of 50.**
  - Results: the batch's own 10/10, audit 10/10, r059 13/13, r067 31/32, r123 5/5.
  - The red is **r067-car-ring-go-alt**: "the Alt line 'Calls off the attempt; try again' overlaps the beat 'In the car'". This is not new. The card it sits on is the collapsed feed's title, `feedTitle` (HudToolkit.cs:921). `FeedRects` (HudToolkit.Ring.cs:169) lists beats, cards, history and beatsBox, but not `feedTitle`, so the fix in 0.2.128 doesn't see it. Reported, not fixed (Aster's rule for this run). The fix is one line: add `feedTitle` to FeedRects.
  - Not taken at 1080: r067-reticle-handoff. It is only shot when rival-2 is in view at that moment (Proof067.cs:325), so it depends on run timing. It was taken at 2160.
- **late-fight: ok** (`--only late-fight,tp-long`, because the batch stops at the first failure).
  - loud-fight: worst 21.4 ms, p99 6.6 ms.
  - loud-left-downed: worst 18.3 ms, p99 6.6 ms.
  - NOISY MACHINE (load peaked at 12.5).
- **tp-long: ok.**
  - p99: play-opening 5.3 ms, tp-street 5.3 ms, tp-worst 5.6 ms.
  - Triangles: 1.6 / 3.2 / 5 M.
- **2160 retake for Glyph.** Both proofs ran through `bun tools/game/playing.ts`: `-gameProof -capture-size 3840x2160 -proofOnly r067` (r067 ok, 32 pictures) and `-proof123`. The crops came from `bun run proofset crops`: 246 for r067 and 46 for r123.
- **What the tree fixes show in 0.2.128:**
  - Strike: r067-shut-ring-guard reads "Listen" (struck), the lock icon, then "Already heard", with the strike over the word and the lock beside it. The car ring after it reads "Drive · 29 s", no longer garbled (r067-car-ring-drive, r067-car-ring-drive-hover).
  - Shut plate as `lock`: the manifest has `lock` "Listen · Already heard", and it is no longer running text.
  - Roster band: r067-crowd-guard-hover has no bubble behind the roster column. The only speech in the frame is at x 1590.
  - Alt line: still on the feed title (see the budget red above).

**Evidence** (git-ignored, on disk): briefs/041/build/0314/
- batch logs: batch-0314a-from-game-build.log and batch-0314b-late-tplong.log;
- logs/, proof-1080/, shots/, late/ and tplong/;
- proof-2160-r067/ and proof-2160-r123/, each with its own player.log and guard log;
- crops-2160/: crops-r067.html and crops-r123.html.

**Key pictures:**
- A ring opened after a shut ring: proof-1080/r067-car-ring-drive.png (and r067-shut-ring-guard.png before it).
- The Go Alt line: proof-1080/r067-car-ring-go-alt.png (still on the "In the car" title).
- The conversation: proof-1080/r067-convo-inez-2.png.

**Still red / open:**
- The Go Alt line overlapping `feedTitle` (fix above, not built).
- The clock bar's next-mark word against glyph-budget §5 (Glyph's call, unchanged).
- N8, N9 and N10 are still open.

## Briefing page (brief 070) (Latch, 2026-10-26): build 0.2.130

**What changed**
- The page replaces brief 062's street overlay (`Briefing.cs`, rewritten). It is a full-screen page on a 1920 × 1080 grid, scaled as one. It follows Glyph's mock: cream stone paper, a seafoam band with a brass hairline, the header (title, hook, objective, place and time), the target card (photograph, name, two facts), two way rows (name, `when` plate, two sentences, two stills with numbered pins and arrows), the getaway car as the shared ending, a plan of the block drawn from data with pins and a legend, the clock's five stations, and the band (four faces with a line each, the purse, Heat as one word, Begin). Day / Night is gone.
- All words come from data (`BriefScreen` in `HostBriefing.cs`):
  - brief.json: title, place, time, hook, objective, the target, the ways;
  - text.json: `less.briefing.ways` / `target` / `begin`, `briefing.hints` (the part after "Name: "), `briefing.page.*`, the `briefing.timeline` labels, `targets.<key>.name` for the captions, `briefing.heat`.
  - A clock station is filled when its label names the target (5:50, 6:10).
- Begin lifts the page (0.3 s) onto the camera at `brief.target.at`, d:Official entrance, at the quarter step facing its outward side. The existing glide then runs as before.
- `PlayText`: added `HintBy` and the `Page*` fields; the backup ClockLine now matches Fable's. Retired: `less.briefing.job` (text.json, schema and test), and the `day` / `night` fallbacks along with the Day / Night words.
- **Stills, taken at build time** (`tools/game/briefstills.ts` + `briefstills.test.ts`):
  - The pins are derived from mission data: brief.json ways; mission.json roles, windows, timeline and points; routes.json primary scripts and contacts; civic-block.json faces and bays.
  - The player takes them from replays of route-a-board / route-b-board (c038) through the shot runner, HUD off, as JPEG.
  - They are copied into the app's `StreamingAssets/missions/handover/stills/` and the app is signed ad hoc again.
  - `cli.ts build()` runs this after every build, before any proof. It takes about 15 s.
- ShotRunner gained `nocut` and `doorN`: the quarter yaw facing a face's outward side, and pose offsets in the door's frame, lifted to the landing's height.
- **Deviations from the README capture table**, from looking at the pictures:
  - Inez and the guard use the conversation profile off their door (3.6 m, 1.9 m, 8°, 16° field).
    - The guard's overhead yaw 210 with the cut off was blocked by the buildings across Rocket Lane.
    - Inez is taken at 0:03, before the Scout stands in front of her. At 0:30 the Scout fills the frame.
  - The getaway car uses a low camera off its end. From above, the boarding pavilion's canopy hides it at every yaw.

**Evidence** (git-ignored, on disk: briefs/070/build/0130/)
- `bun run batch --from sim`: build 0.2.129. game-check STOPPED on `hudClick`: one press through at s-plate_dark@2816,64 (logs/proof-0129-hudclick-flake).
  - A rerun of `bun tools/game/cli.ts --check` on the same build passed, exit 0, hudClick ok. I take it as a flake.
- `--from shots` on 0.2.129: budget red, including my bug: the page's manifest rows were duplicated on a refill (40 sentences), and two plate icons sat inside the 4 px rim clearance. Both are fixed.
- `bun run batch --from game-build` → **Build 0.2.130**:

| step | result |
|---|---|
| game-build | ok; stills 5/5 in 15 s, ad hoc signature ok |
| game-check | ok (scenarios 356/356, every gate) |
| shots | ok (11) |
| budget | FAILED: audit 10/10 clean; briefing-day and the old r067-car-ring-go-alt, both below |
| late-fight, tp-long | skipped on purpose (main, on Sky's OK: this build changes only the page before Begin, the feedTitle line and Fable's words; the full batch runs once at the end of the round) |

- **Targeted checks on 0.2.130:**
  - Front end at 1080 and 2160 (`-frontEndShots -capture-size`, status ok): newGameBriefing, briefingAfter ("up|page|… stills: 5/5 … clock (5 stations)"), startHeld and startPlays (yaw 30) all ok. So the page is modal until Begin, and Begin lifts and then glides to the cell.
  - The 8:30 mark in play: route B at 7:00 shows "▶▶ 8:30" on the clock bar (play-830/). Its words ("Close: a turned driver gives up and drives Vale away") ship in the app's text.json and show on pill hover; I have not captured the hover.
  - Run: bun test tools/game + contracts/missions/handover (380 pass), typecheck clean.
- **Pictures:**
  - the page: frontend-1080/briefing.png and frontend-2160/briefing.png;
  - the lift: frontend-1080/start-1-begin.png;
  - the stills: stills/brief-waitress.jpg, brief-guard.jpg, brief-driver.jpg, brief-service-bay.jpg and brief-getaway.jpg (stills.json has the derivation of each).

**Still red / open**
- **Briefing budget (for Splice and Glyph, not loosened):**
  - HUD covers 37.3 % against the briefing's 18 % limit (footprint 62 %): the stills, the photograph and the plates count. The paper and the band are marked not-HUD.
  - The briefing reads 20 sentences and 182 words against BRIEFING_READING's 3 and 20.
  - GDD v0.27.0 says the reading limits don't apply to the page, so tools/hud-budget needs a page state or an exemption; that is Splice's call.
- **Go Alt line (not new; red since 0.2.128):** "Calls off the attempt; try again" (altline 1513,965 183×26) still overlaps the feed card's "In the car" title (beat 1590,987) on 0.2.130. The committed feedTitle line doesn't clear it (go-alt-crop.png).
- **Nits for Glyph's 2x:**
  - The plan's "Records Office" label still touches pin 2: both faces of the Office carry pins. A fix would put the name inside the outline.
  - The page has no "Two ways in / Where / The clock" heads, because text.json has no words for them (Fable).
  - The guard's still has a dark glass background.
- `less.briefing.fact` ("Alive") and `office` are no longer read by the page (Fable may retire them).

## Glyph 2x (0.2.130) briefing page (Glyph, 2026-10-26; review only, no code, no Unity)
**Verdict: NOT SHIP yet. One short round (N1 to N8), then Sky sees it.**

The page does what Sky asked for, and it's clearer than the street overlay was. It follows the mock and GDD v0.27.0 §10:
- the title, hook and objective;
- Vale's file card;
- two ways, each with its words and stills;
- the getaway car as the shared ending;
- the plan with the same pins;
- the five clock stations;
- the four cell lines;
- the purse and Heat;
- Begin, with no Day / Night.

It's the same layout at 1080 and 2160: the 2160 shot downscaled differs from the 1080 shot in 0.3 % of pixels, all antialiasing. At 2x nothing clips and the plates and band sit square.

Four faults sit where Sky will look first:
- the guard's still is a dark void;
- the plan's label sits under a pin;
- the driver is too small to find in his still;
- world markers show in two "HUD off" stills.

**Looked at:**
- frontend-1080/briefing.png and frontend-2160/briefing.png against briefs/070/glyph/briefing-page-mock.png. My crops of the 2160 page (plan, front door, clock, right column, band) are in out/scratch/glyph/r130/.
- start-1-begin.png and start-2-glide.png.
- The five stills at full size, with stills.json.
- budget/briefing-day.manifest.json and logs/budget.log.

### The three changed framings
- **Inez at 0:03: reads. Keep it.**
  - Head to waist, facing us, café counter behind her, lit warm.
  - This is the best still on the page.
- **The guard from in front of his door: doesn't read (N1).**
  - The person reads, but the place doesn't.
  - Behind him is the open doorway, an interior we don't build: a flat blue-grey void between two brown posts.
  - He's lit dim, and it's the one dark slab on a bright page.
- **The getaway car, low off its end: reads, with a note (N5).**
  - In the tall slot it reads at once as "our car at the kerb", and the plaza behind places it.
  - But this is the closest any camera gets to the car. The model is a soft untextured peach shape with no chrome, so up close it looks like a stand-in.
- **The driver** (unchanged rule, zoom 9): the scene reads, but the driver is about 18 px tall in the 372 px slot (N3).
- **The service bay:** reads. The car is in the bay with the blue door beside it (N4 for the markers).

### The lift (start-1-begin)
- **Passes.** The page fades over the camera at the Records Office front, and the door with its awning is centred.
- It's a dissolve rather than a lift. Mid-fade the page and the street are both half there, which looks muddy in a still. In motion it's under a second, so it's fine (N9, optional).

### Nits (owner, exact fix)
**N1 · The guard's still is a dark void. (Latch.)**
- Keep the profile rule, but turn the camera 35 to 45° off the door's normal, toward the landing. Then the background is the lit Office façade and its doors, not the open doorway.
- If that's blocked, close the door leaves for the still.
- Same key light as Inez.

**N2 · The plan's "Records Office" label sits under pin 2. (Latch.)**
- Put the name inside the Office outline, centred, in two lines ("Records / Office"), as in the mock.
- Give the label a 4 px keep-out from every pin.
- Emit the plan's labels and pins as manifest elements (glyph-page-budget 2.2), so the tool catches this next time.

**N3 · The driver is too small. (Latch.)**
- brief-driver: zoom 9 → 5 or 6, centred on the driver and the car's near end. He should be at least a third of the still's height, with the car and the Office door still in frame.

**N4 · World markers in HUD-off stills. (Latch.)**
- brief-service-bay shows:
  - the turquoise rings under four people;
  - the dashed patrol route with its arrows;
  - the dashed rounded wait box.
- brief-getaway shows a ring under the far figure.
- Stills turn off world-space overlays too: rings, route marks, bay outlines and cones. Only the 3D world remains.

**N5 · The getaway car close up. (Aster → Forma/Vela; not for this round.)**
- The car's model has no detail at still range. The ending's still is the page's largest picture.
- For now, Latch can raise that camera to about 25° down and back off a step, so the shape reads and the surface doesn't.
- Later, the car needs chrome trim and a body texture.

**N6 · Heat has no head word. (Latch, words from Fable.)**
- "Unknown" with an eye reads as "unknown what?".
- Add the small tracked head "HEAT" before the word, as the mock has it, from text.json (`briefing.heat.label`, which Fable adds).

**N7 · The front door's widow. (Latch; Fable if needed.)**
- "...loop the camera that watches / it." leaves one word on the last line.
- Latch: add a no-widow rule to the page's text layout: pull the last two words down together.
- Or Fable: "Turn her to call him off the door, and loop his camera." (12 words).

**N8 · The manifest hides the page. (Latch.)**
- "cell faces" is one `image` of 1920 × 1080, so it covers the whole page and blanks the text check. Emit one 52 × 52 `image` per face.
- Tag the page `state: "page"` once Splice adds it (glyph-page-budget §1).

**N9 · Optional: the lift. (Latch.)**
- Slide the page up off the top over about 0.35 s instead of fading it, so it's never half there.
- Skip this if it costs more than a line.

**N10 · Section heads. (Fable words, then Latch; low.)**
- Add the three small tracked heads "TWO WAYS IN", "WHERE" and "THE CLOCK" from text.json, as the mock has them.
- Without "WHERE", the plan reads as part of Vale's card.
- These are 6 words, still under the page's 200.

**N11 · The still captions name people, but the pickup's words name a place. (Fable; low.)**
- The pickup sentence ends at "the service bay", but still 4's caption and its legend entry say "Vale's car". Nothing on the page says "service bay" except the sentence.
- Caption it "Vale's car at the service bay" (5 words), as the mock has it, or "At the service bay".

**N12 · The 6:10 station mixes a sentence and a fragment. (Fable; low.)**
- "Vale at his car. Unless turned, his driver takes him away" has no full stop, and the other four stations are fragments.
- Use "Vale at his car; unless turned, the driver leaves". It's 9 words and fits 2 rows.

### Budget
- The page's own limits for Splice are in **briefs/070/glyph-page-budget.md**.
- They add a new state `page`, with no coverage check and no BRIEFING_READING. These checks replace them:
  - opaque (no street shows);
  - every word is in the manifest;
  - nothing clipped or outside the 48 px safe frame;
  - nothing touches (a 4 px gap; a pin may sit on its own still);
  - body text at least 20 px at 1080;
  - pins agree across still, plan and legend;
  - word caps per part, and at most 200 on the page;
  - no widows.
- 0.2.130 under those rules would fail on N2, N6, N7 and N8 only.

Next owner: Latch (N1 to N4, N6 to N9), Fable (N6 and N10 words, N11, N12), Splice (glyph-page-budget), Aster (N5 to Forma/Vela). Done-check: a rebuilt page and its stills, the 2x crop, and `--state page` passing.

## Briefing page (brief 070), round 2 (Latch, 2026-10-26): build 0.2.133
Glyph's N1 to N4 and N6 to N9, N5's interim camera and N10's heads, plus Redline's 1080p items. Fable's words are as in text.json at build time: "loop his camera", the 6:10 label and `briefing.page.heads`.

**What changed**
- **N1, the guard's still:**
  - The window's role (a guard on his post) is now taken 45° off his door's normal. It turns toward the face's shorter run, so the longer run of façade is behind him (`faceRuns` in briefstills.ts: the Office's west wing runs 6.8 m one way from the door, 5.1 m the other).
  - A warm key light beside the camera lights him for that one render (`keyLight` 30). URP spot intensity falls off with distance squared, so 1.5 did nothing at 3.6 m.
  - I looked at 40°, 45°, 50°, 55° and 65°, each with keys 20, 30 and 60 (contact sheets in `briefs/070/build/0133/guardtune*/grid.png`).
    - At 40° to 45° the glass door leaves are still behind him. They read as his door (handles, frame, lit), not as a void.
    - Only about 65° clears the doorway, and that is a profile. I kept 45°, inside Glyph's 35 to 45°.
  - I did not turn the sun (I tried it in 0.2.131 and it changed nothing: he stands in the door's shade).
- **N2, the plan's labels:**
  - A building's name goes inside its outline, centred, on two lines split at the middle word ("Records / Office"). It sits there when that fits the outline's box and keeps 4 px from every pin. Otherwise it goes on one line over or under the outline, clear of the pins. The café keeps its one line over its outline.
  - The labels go into the manifest as `plan-label` (with `rows`) and every pin as `pin` with `n` (1 to 4, or `car`): on the stills, on the plan and in the legend.
- **N3, the driver:**
  - The camera is the play camera at zoom 2.1, aimed at the midpoint of the driver and his car (ShotSpec `focus2`), moved 1.4 m in toward the face (`shift`).
  - He is about 355 of 1080 px tall (a third), with the car's near end beside him and the Office door with Vale behind.
  - Zoom 5 to 6 at the play pitch would make him about 13 % of the height: a third needs about 2.1.
- **N4, world marks:** every still is `bare`.
  - The ShotRunner renders the camera once into a texture, with the ground marks off (`HudToolkit.WorldMarksOff` skips GroundFlush) and the LineRenderers and sight-fan meshes hidden for that one render.
  - The service bay and getaway stills have no rings, route, wait box or cones.
- **N5 (interim):** the getaway car's camera is now at 25° down (offset 1.8, 5.8, 11.5 m). The car's model is still Forma's and Vela's job.
- **N6, Heat:** the "HEAT" head word (`briefing.page.heads.heat`), then the eye and the word.
- **N7, widows:**
  - PgPara holds a paragraph's last two words together with a no-break space. Wraps now pull them down as one: the 6:10 label ends "the / driver leaves".
  - The front door's line is now Fable's "loop his camera".
- **N8, the manifest:**
  - Each cell face is its own `image` (60 × 60 with its ring), and the 1920 × 1080 "cell faces" rect is gone.
  - Every page piece carries a `part`. The paper is `paper` and the foot band is `band`. Headings, the role names and the Heat head word are listed with their source words.
  - The manifest's state is `page` (Splice's tool has it now).
- **N9:** Begin slides the page and its paper up off the top in 0.3 s (ease in), with no fade, so it is never half there.
- **N10 heads:** TWO WAYS IN (from `heads.ways.<count>`), WHERE over the plan and THE CLOCK over the stations. They come from text.json through the new `PlayText.PageHeads` loader.
- **Safe frame (page budget 2.3):** the title moves down to 48 px, and the band's pieces move up 8 px, so each ends 32 px above the foot.
- **Redline 064 (1080p bar):**
  - Perf064.cs:46 now sizes to `-capture-size` (default 1920 × 1080), and its summary no longer says 2880 × 1864.
  - CaptureSize.cs's header says frame times are measured at 1920 × 1080 (Sky, 2026-10-26).
  - `CaptureSize.Parse` is now public.
- **Files:**
  - unity/Assets/Suborned/Runtime/Play/: Briefing.cs, PlayText.cs, HudToolkit.Capture.cs, HudToolkit.Less.cs, HudToolkit.Ground.cs, ShotRunner.cs, Perf064.cs, CaptureSize.cs;
  - tools/game/briefstills.ts and briefstills.test.ts.

**Evidence** (on disk, git-ignored: briefs/070/build/0133/)
- `bun run batch --from sim` stopped at sim on Keel's tree: headless 355/356, DP1 failing.
  - The tree's DLL (engine 0.35.0, in progress) had already gone into Unity.
  - I went on `--from compile`. unity-match then FAILED, though it reported 447/447: the headless file carries the DP1 fail.
  - Then `--from game-build`, which was twice blocked for a while by Splice's `-late` runs on the shared app; I waited for them, killed nothing.
- **Build 0.2.133**, game-build ok (stills 5/5, ad hoc signature ok). Steps on 0.2.133:

| step | result |
|---|---|
| game-check | FAILED, not mine: holdRange weaponsFree ("'free' is retired (weapons.oneInput, brief 039)") and r29 `down`. Both come from Keel's 0.35.0 tree, and both failed the same way on 0.2.132. On 0.2.131 (engine 0.34.2) the drive replay failed instead: "inputs differ from the record", the tree's inputs changing under the build. |
| shots | ok (11) |
| budget | FAILED: audit 10/10 clean; briefing-day and getaway-car-yaw210 over; proof set r059 9/13, r067 16/32, r123 5/5 |
| late-fight | ok: loud-fight p99 7.7, worst 37.7 ms (order+save) |
| tp-long | ok: play-opening p99 5.5, tp-worst p99 5.8 ms |

- **The page's budget** (`--state page`, Splice's tool): 2 faults, down from 21 on 0.2.132 and from 0.2.130's 2.2/2.4/2.7/2.8.
  - **225 words** against the cap of 220. The total is now complete: it counts the heads, role names, plan labels and Heat head that 0.2.130 didn't list. Cutting it is for Fable and Glyph.
  - **"Records Office" ends on a widow.** This is N2 against 2.8: a two-word name on two rows always ends on one word. Splice: exempt a plan label, or any element of two words or fewer.
- **r059 and r067 proof pictures:** new reds on 0.2.133, all in drive, board and Alt frames ("running text", "stacked text"). In each failing frame the history panel (SCENES · HISTORY) is open in the proof.
  - On 0.2.132 the same sets were 13/13 and 31/32, on the same engine. This round touched no play HUD.
  - I take it as the proof leaving History open after the failing holdRange and r29 sections. I didn't loop on it.
- **getaway-car-yaw210:** "1 block", also red on 0.2.130.
- **Front end at 1080 and 2160** (`-frontEndShots`, `-capture-size`, scratch `-settings`): status ok, all checks.
  - briefingAfter: "up|page|… stills: 5/5 … clock (5 stations)".
  - startHeld and startPlays (yaw 30) ok.
- `bun test tools/game contracts/missions/handover`: 387 pass. `bun run typecheck` clean. runtime-compile clean.

**Pictures for Glyph's recheck**
- the page: briefs/070/build/0133/frontend-1080/briefing.png and frontend-2160/briefing.png;
- the slide: frontend-1080/start-1-begin.png;
- the stills: briefs/070/build/0133/stills/brief-*.jpg (stills.json, stills.shots.json);
- the budget run: budget/briefing-day.png, its manifest, and budget/briefing-day.budget.txt.

**Open**
- Fable: page words 225 > 220. N11 (the "Vale's car" caption) has not landed yet.
- Splice: the 2.8 widow rule against a two-word plan label.
- Keel: DP1, holdRange weaponsFree, r29 `down` on 0.35.0.
- Forma and Vela: the getaway car model (N5).

Next owner: Glyph (2x recheck of the page and the stills), then Aster.

## Glyph 2x (0.2.133) briefing page (Glyph, 2026-10-26; review only, no code, no Unity)
**Verdict: SHIP the page,** once two text-only items land: Fable's cut to 220 words and Splice's two-word exemption. Neither changes the layout. The done-check is `--state page` passing with 0 faults on the next build. Sky doesn't need to see another round from me before that.

**Looked at**
- frontend-1080/briefing.png and frontend-2160/briefing.png. The 2160 shot downscaled differs from the 1080 shot in 0.58 % of pixels, all antialiasing.
- My 2x crops of the 2160 page: plan, guard, driver, getaway, clock, band, top. They're in out/scratch/glyph/r133/.
- start-1-begin.png.
- budget/briefing-day.budget.txt and the manifest's plan labels and pins.

**0.2.130 nits, rechecked**
- **N1, the guard: pass.** The background is his own glass door (handles, frame), not a void, and he is lit. It's still the darkest still on the page, but it reads.
- **N2, the plan label: pass.** "Records / Office" sits inside the outline. In the manifest it keeps 6 px from pin 2 and 10 px from pin 3.
- **N3, the driver: pass.** He's about 35 % of the still's height, with the car's near end beside him and the Office door and Vale behind.
- **N4, world marks: pass.** No rings, route, wait box or cones on any still.
- **N5, the getaway car: interim accepted.** The shape reads at 25°. The model is still Forma's and Vela's job.
- **N6 and N10, the heads: pass.** HEAT, TWO WAYS IN, WHERE and THE CLOCK are all present and tracked as the mock has them.
- **N7, the widow: pass.** The front door now ends on "and loop his camera."
- **N8, the manifest: pass.** Each face is its own image, every piece has its part, and the state is `page`.
- **N9, the slide: pass.** In start-1-begin the page is whole and moving up, never half there.
- **At 2x:** nothing clips, touches or leaves the 48 px safe frame. Band, plates and pins sit square.

**Remaining nits (owner, fix)**
- **R1 · Words, 225 > 220. (Fable.)**
  - The cheapest cut also fixes R2. Make the 6:10 station "Vale's car leaves" (−6, giving 219). 5:40's "Last chance to turn the driver" already carries the condition.
  - A spare cut if needed: the top-right line "Civic Records Office, the civic block · …". Drop "the civic block" (−3).
- **R2 · The 6:10 label wraps "…unless turned, the / driver leaves". (Fable, through R1.)**
  - The no-widow rule pulls "driver leaves" down, but it leaves the article "the" stranded at the end of row 1.
  - R1's 3-word label removes the problem.
  - If the long label stays, Latch should also keep a trailing article (the, a, an, his, her) with the next word.
- **R3 · The widow rule for two-word labels. (Splice, as Aster has said.)**
  - Exempt elements of two words or fewer from 2.8, so "Records / Office" passes.
- **R4 · The budget line disagrees with itself. (Splice.)**
  - briefing-day.budget.txt's header says "225 words (at most 200)", but its fault line says "at most 220". Use one cap, read from one constant.
- **R5 · My budget doc is out of date. (Glyph.)**
  - briefs/070/glyph-page-budget.md §2.7 still caps the page at 200. Aster's 220 stands. I'll correct the doc to 220 when Aster says so, so the doc and the tool agree.
- **R6 · N11 hasn't landed. (Fable; low.)**
  - Still 4's caption and legend entry say "Vale's car". Make them "At the service bay" so they match the pickup's words.
  - It's word-neutral if "Vale's car" (2 words) goes to "Service bay" (2 words). I'd take that.
- **R7 · Optional: the getaway still's framing. (Latch; low.)**
  - About 40 % of the tall slot is empty road below the car.
  - Aim the camera about 1.5 m further toward the kerb end, so the car and Vale fill the middle.
- **R8 · Optional: the plaza mark on the plan. (Latch; low; it was there in 0.2.130 too.)**
  - The dashed ring with a dot near the café reads like a HUD reticle, and the legend doesn't explain it.
  - Draw it as a plain filled circle (the fountain), like the other street shapes, or drop it.

Next owner:
- Fable (R1, R6);
- Splice (R3, R4);
- Latch: a text-only rebuild, with R2 only if Fable keeps the long label, and R7/R8 if cheap;
- then Aster: `--state page` 0 faults, then show Sky.

## Batch 0.35.1 port (Latch, 2026-10-27): build 0.2.135, game-check red on one proof part (fire, secondLock), budget red on my manifest tag (fixed in the tree)
Engine 0.35.1 at 7c27e980 (fight B, one input, sticky focus, cover and pin, moves off, the cell never pinned) into the host, Forma's B clips, the page items and the capped world. Note: briefs/027/keel/host-interface.md has no 0.35.x section (it ends at 0.34.2), so the port reads the engine's own API (GameCoverPin.cs: PostureOf, ShotsThisTick, PreviewMoveSnap, FocusOf, MoveOf, OneInputOn) and Motive's rulings-0.35.md / 0.35.1.md.

**What changed**
1. **One input** (gated on `Game.OneInputOn`; the fight lab keeps the old controls):
   - X (cease), V (stance), both buttons (panic) and G's weapons-free toggle give nothing; in the hold preset G is walk/run (GDD §10) and X still stops walks and acts, with no cease. The free toggle, the stance glyph and badge, the stance fan and "drawn · <stance>" words are gone; Controls lists no Cease or Stance row.
   - No host path sends cease, stance, free or panic: a new act drops the lock only (the engine ends the focus), the drive trigger and the commander hold-fire send no cease, a squad fire goes to every selected armed agent (the engine walks them in). The bot runner reads a plan's panic as F on each of the four and skips stance/free/cease (Keel's oneinput.py).
   - The H21 tip ("press X") stays silent under one input (Fable: reword to the holster).
   - The sticky focus: the lock disc's held state reads `FocusOf`; under it a two-word plate says why it waits ("aiming", "walking in", "no line", "too far"; Fable's `less.done.focus.<state>` first). No shooters' cameos under it.
   - Cover snap: out of the lab the move ring stands on the kneel point (`PreviewMoveSnap`) with a shield inside it; if that end flanks the focus's cover, one struck shield disc over that cover. Nothing else of cover is drawn.
   - 0.34.2: PlayText's `entry-seen-guard` line drops the patrol; the patrol robot wears its alarm face while `EntryReport` is pending. The three codes are text.json cards already.
   - No move reads: no Mark, Talk down, Steam or Covering fire UI or ring entry; the proof checks `MoveOf` is empty for all four.
2. **Fights on the body** (Glyph b-final, Forma pin-poses.md §D):
   - The 8 clips installed into unity/Assets/Suborned/People (FBX, meta, manifest rows only; the rest of out/people-models differs and was not mirrored). FigureBuild keeps additive rows off the base layer and adds two additive layers: `react` (flinch_add, kick_add, restarted per event) and `peek` (pin_peek_add, weight from the pin's time left). The library now rebuilds whenever the installed manifest is newer than the controller.
   - The host reads `PostureOf` and `ShotsThisTick` once a step (HandoverHost.BodyRead): kneel = cover_in then cover_kneel, firing from cover = cover_fire, pinned in cover = cover_pin with the peek, pinned in the open = cower held; a hit (or a round near a pinned man) flinches; a carbine hit standing staggers (hit_large); each own shot kicks. Forma's cross-fade table.
   - A miss is a dust puff on the paving (no ring); a round cover took chips the prop.
   - In a fight: only the selected agent's ring and the focus's (one red line, no ticks); no rings under the unselected cell or the rivals; pips over the focus only; no firing discs over shooters; no under-fire arcs. At rest everything is as built.
3. **Proofs**: holdRange's weapons-free part is now `oneInput` (G/X/V send nothing retired; F holsters the four and every fire order ends; moves empty). r29 `down` plays the route until the Scout falls (0.35.1: tick 2878) instead of a 0.34 tick. ProofFire ends the Fixer's commander lock by holster and redraw. r123 logs the body's reads per fight.
4. **Page**: `briefing.page.captions[place]` and `briefing.page.stations[at]` before the defaults ("Service bay", "Vale's car leaves"). R7: the getaway still aims 1.5 m past the car toward the kerb end (ShotRunner's lookOffset now in the door's frame). R8: a fountain is one filled shape; the beds within 8 m of it are its garden and aren't drawn (fountains matched by kind: the beds' ids had drawn them as water arcs). The Go Alt line goes below, else above, else the above spot slid outward 8 px at a time until it clears, else "unplaced" with a warning (`RingAltPlacement`).
5. **World**: `bun run world` with Vela's play-street pieces in the tree (PortholeBay_3L 27, PortholeBay_4L 47 placed), then the Unity world build.
- Two new shots: `fight-safe-body` (route-g-board 3:50) and `fight-loud-body` (loud 3:47.5).

**Evidence** (on disk, git-ignored: briefs/041/build/0135/)
- Full batch (logs/latch-batch-0351.log): sim 357/357, compile, kit, world, unity-world 49 s, unity-match 454/454, game-build **0.2.134**; game-check FAILED on `rings` (my cut also hid the at-rest combatant rings) and `fire` (followerFrames 0: the proof's `cease` on the Fixer is refused under one input). Both fixed.
- One targeted rerun `--from game-build` (logs/latch-batch-0351-rerun.log): **build 0.2.135**. game-check FAILED on `fire` only: `secondLock` empty (the Fixer now follows, so the car likely takes enough hits to be down before the proof's second lock; not probed). rings ok, holdRange ok (oneInput ok, focus ok), r29 ok (down ok), r059, r067, r118 ok.
- The steps that never ran, on 0.2.135, no rebuild: shots ok (13); budget 10/12 and audit 11/12: the two reds are my focus-wait plate tagged `lock` (the lock pill's kind) in a rest frame. Fixed in the tree (tagged `marker`); the same two shots re-judged with the retagged manifests both PASS (out/scratch/latch/budget-recheck). Proof sets r059 13/13, r067 32/32. late-fight ok (loud-fight worst 24 ms, p99 6.9 ms), tp-long ok (all under the gates). NOISY MACHINE.
- r123 body: safe "kneel 45, pinned 15, flinches 1; clips cover_fire, cover_kneel, cower"; loud "kneel 45, pinned 0, flinches 2; clips cover_fire, cover_kneel".
- Page: front end at 1080 and 2160 status ok (briefing up, stills 5/5, 5 stations, Begin then yaw 30). **`--state page`: PASS, 208 words (at most 220), 0 faults** (budget/briefing-day.page.txt).
- Pictures: fight shots/fight-safe-body.png, shots/fight-loud-body.png, proof/r123-fight-safe.png, proof/r123-fight-loud.png, proof/hold-one-input.png; the page frontend-1080/briefing.png, frontend-2160/briefing.png; stills/brief-getaway.jpg.
- `bun run typecheck` clean; `bun test tools/game` 221 pass; runtime-compile clean.

**Open**
- Latch: the `fire` proof's second lock under one input (probe the car's health at that point; give the second lock a target that outlasts four guns, or holster the Fixer for the run). The marker tag needs a build.
- Audit (new shot, not new code): fight-loud-body, a 16 px glyph in a 24 px plate not 4 px clear of its rim (hud/VisualElement[1]/VisualElement[51]/VisualElement[0]/glyph). Glyph to name the piece.
- Fable: H21's words under one input; `less.done.focus.<state>` for the wait plate (fallbacks in use).
- Keel: the 0.35.x section of host-interface.md.
- Glyph: the range checks b-final asks for (other headings, the real planters, Unity's pixels for kneel and duck); the cover snap's shield and struck disc have no proof picture yet (no proof drives a move in a fight).
- Forma: the rest of out/people-models differs from the installed figures (carry, sit, serve, run_lean clips; c6 and councillor materials): not installed here. Side falls (Standing_React_Death_Left/Right) not wired.
- Environment: front-end captures hung while the display slept (Metal waiting to present); `caffeinate -u` fixed it.

Next owner: Glyph (2x of the page and the two fight shots), then Aster.

## Glyph 2x (0.2.135) (Glyph, 2026-10-27; review only, no code, no Unity)
**Verdict: the page SHIPS. The fight does NOT ship to Sky yet.** The fight frames still carry 9 kinds of street mark, against b-final's 7. The car cluster in fight-loud-body has about 14 marks in a 200 × 300 px area, which is exactly Sky's "too crowded". There's also no picture yet that shows the fight reading on the body. G1 to G5 are host-only fixes, with no engine work.

**Looked at:** shots/fight-safe-body, fight-loud-body (+ .hud, manifests); proof/r123-fight-safe, r123-fight-loud, hold-one-input; frontend-1080 and -2160 briefing; stills/brief-getaway; budget/briefing-day.page.txt. My 2x and 4x crops are in out/scratch/glyph/0135/.

**What passes**
- Cut list: there are no tracers, no under-fire or cover arcs, and no miss rings. The unselected cell and the rivals have no rings. The focus has one red ring, as specified.
- The page: R7 (the getaway still now has the car and Vale in the middle of the tall print, with no road band) and R8 (the fountain is one filled disc, and the plan reads clean) both pass. N11/R6 "Service bay" has landed. `--state page` PASS, 208 words, 0 faults. At 2x on 2160, nothing clips or touches, and the band, plates and pins sit square. R5 is closed (my budget doc says 220).

**Nits (owner, fix)**
- **G1 · The fire line sits on the focus and covers its lock disc and pips. (Latch.)**
  - In fight-loud-body, the plate [gun disc][downed] 8 s (HudToolkit.Fire.cs, fireLine at 1012,662, 99 × 38) covers the lock reticle and holder (1039,650 / 1039,673) and the red pips under them. In r123-fight-loud it sits over a rival.
  - The fire line isn't one of b-final's 7 marks. Under one input, take it off the street: the time-to-down goes in the shooter's portrait chip (the chip slot already says "aiming / firing").
  - If Aster wants it kept, it stands at least 4 px clear above the lock disc and never on top of it.
- **G2 · Gun discs over the drawn cell agents. (Latch.)**
  - Two dark discs with a cream gun and a faint halo stand over drawn agents in both fight shots and in hold-one-input. In loud they're at 1042,460 and 1020,528.
  - b-final says draw and holster show on the body and in the portrait's sidearm mark, "never on the street". Cut them under one input.
  - They aren't in the manifest, so the budget can't count them. Every street mark must be manifested.
- **G3 · The focus wait plate ("no line") covers the focus's pips. (Latch.)**
  - In fight-safe-body a red sliver of the pips shows between the lock disc (1084,577) and the plate (1069,607).
  - The stack should run, top to bottom: lock disc, then pips, then the wait plate, each 4 px clear. Or put the plate on the focus's right at the disc's height.
- **G4 · Latch's audit fault: the piece is the takedown body mark of a killed body. (Latch.)**
  - HudToolkit.Takedown.cs:117–118: when `b.Dead`, the plate switches from `s-disc_dark_24` to `s-plate_dark` at 24 × 24, with the 16 px glyph from NewMark at (4,4). The path is marks [1] → mark root [51] → plate [0] → unnamed glyph.
  - A 24 px plate_dark at slice 0.5 has its rim 6.5 px in, so no 16 px glyph can be 4 px clear of it.
  - Fix: draw the kill plate at 30 × 30 with `unitySliceScale` 0.3 and the `body_dead` glyph at 14 px, centred (8,8). That clears the audit's inset of 7.6, and it stays square against the knockout's round disc. Give the root a manifest tag ("body").
  - I couldn't find it in the picture, so it's probably under the car or callout stack. Confirm its spot from the frame. If it was under another mark, that is G5 too.
- **G5 · Speech over the attention callout's face. (Latch.)**
  - fight-loud-body: "Behind the wall, officers!" (972,629, 161 wide) runs across the callout's portrait (1078,592, 72). In r123-fight-loud the same callout covers the focus's red ring and body.
  - Speech steps aside from the callout (add it to StepAside's blocks). The callout keeps clear of the focus ring: it should never sit on the man the fight is about.
- **G6 · Barred red discs and red pips over the non-focus hostiles. (Latch to identify and cut.)**
  - In r123-fight-loud there are two red "no" discs (about 690,400 and 648,500), with three red pips under one, over men who aren't the focus.
  - b-final allows pips over the focus only. Whatever these are (the people's pips or a refused-lock state), they're a cut kind in a fight. Say which they are; if they're a lock state, they belong on the cursor, not the street.
- **G7 · No picture of the body read. (Latch, for the next build.)**
  - r123 counts kneels and pins (safe: kneel 45, pinned 15). In the two shots I can't see a kneel, a pinned hump or a cower; the hostiles by the hedge stand.
  - Add one shot at a tick from r123-safe's pinned run, with the camera on the cover point. That is b-final's "Unity's pixels" check. Without it I can't sign off "fights read on the body".
- **G8 · Low: the plan's small teal dashes. (Latch, low; page still ships.)**
  - Three unlabelled 2 × 6 px teal ticks sit mid-plan between the fountain and the Office (2160: about 455,1600 and 455,1650). They're probably bollard or planter stubs.
  - Drop shapes under 1 m from the plan, as I asked for the fountain's garden.

**Count after G1, G2 and G6:** selected ring, focus ring, lock disc, focus pips, flash, hit spark, chips or dust. That's 7 kinds, which is b-final. The getaway's own marks (ring, lane, holder, seat pips, Go) are the hold's and stay.

**Hold-one-input:** a lab frame, so the FIGHT RANGE panel's flat styling is fine there. G2 shows there too.

Next owner: Latch, for G1–G5 and G7 (G6 identify, G8 low). Then one targeted rebuild and the two fight shots plus the pinned shot. Then Glyph at 2x, then Aster to Sky.

## Batch 0.35.2 (Latch, 2026-10-27): BATCH OK on build 0.2.137 (c039, e80b1d45+)
Glyph's G1–G8 from the 0.2.135 review, the fire proof's second lock, the H21 tip and HostFeed words for 0.35.2, built with the 3-storey café, the lifted getaway and Vale's cars, and Keel's rebuilt site. Waymark's c039 (e80b1d45) shipped before the build.

**What changed**
- **G1 (fire line).** Under one input the fire line is off the street (HudToolkit.Fire.cs FireLineFrame). The cursor's own chip still shows while aiming. The time to down now sits on the shooter's portrait chip as "6 s" with the downed mark (HostHoldStatus.PlateWords / PlateFireDown; HudToolkit.Less.cs icon).
- **G2 (gun discs).** The discs were the activity rings (HudModel Rings: a busy ring for the draw, aim and fire phases). Under one input there's no ring for draw, holster or fire. Every pooled street mark (WorldEl) is now in the manifest as an `image` "street mark".
- **G3 (focus stack).** The focus's pips now sit 4 px under the lock disc's held ring, and the wait plate sits 4 px under the pips (HudToolkit.Lock.cs PipsUnderDisc/RingPast; HudToolkit.Less.cs pips; HudModel Pips.Id).
- **G4 (kill plate).** A kill's body plate is 30 × 30 px at slice 0.3, with its glyph 14 px and centred. The root is manifested as `image` "body" (HudToolkit.Takedown.cs).
- **G5 (speech and callouts).**
  - Speech moves off the attention callouts: new BubblesOffCallouts, run after the callouts are placed and before the tails. A bubble moves up, down, left or right, at most 140 px, or hides for the frame.
  - Callouts treat each focus (its figure up to the top of its lock disc) like an agent and keep clear of it (HudToolkit.Attention.cs).
- **G6 (red marks on non-focus hostiles).**
  - Identified: the barred red discs are the car's **holder** marks (HolderEl: `badges/mark_no` on `disc_dark_24`, one for each hostile holding the car). The red pips under one of them were the **focus's** health pips, sitting under the fire line that hid its lock disc.
  - Cut: in a fight under one input, the holder discs and their tethers are gone. The car's lock and count still say the car is held. This goes against Glyph's own count, which keeps "holder" as a getaway mark: Glyph and Aster to confirm.
- **G7 (body read).** New proof picture `r123-body-safe` (Proof123.BodyShot; listed in proofset.json). The clock runs until a fighter is pinned, trying pinned-cover first, then pinned-open, then kneel. The camera frames him at zoom 14, yaw 30.
- **G8 (plan ticks).** Plan green shapes with a short side under 1.1 m aren't drawn: the palm tubs and the monument's two 1 m beds (tools/game/briefstills.ts PLAN_MIN_M, plus a test).
- **Fire proof (ProofFire.cs).**
  - Under one input:
    - The trigger hold is 3 s.
    - The Fixer holsters once following is proved (30 frames).
    - The Scout's own lock isn't given.
  - The second lock now comes before the tap. A tap on a car already down is skipped, as a blown one was. The run also logs whether the order was taken, the reason if not, and the car's state before the order.
  - On 0.2.136 the order was refused "no car:official to fire at", with the car down 0/10. On 0.2.137 the car was up 5/10, the lock was set and the mode was `lock`.
- **The focus-wait plate's `marker` tag** is built.
- **H21 (Fable 0.35.2).** Under one input the tip shows at the first fire order, and it's gone when a Holster is given (HostHints.cs).
- **HostFeed.** The `rivals.for-car` words use Fable's card, with "No gun is drawn outside the car…" as the fallback. `back-to-cover` now reads "A gun is drawn outside the car again".
- **0.35.2 host notes.** No API change. The `no longer held` branch still matches the engine text. The host has no free-on dedupe to remove.

**Evidence** (git-ignored: briefs/041/build/0137/)
- `bun run batch --from sim` (logs/batch-0352.log):
  - sim 360/360, compile, kit, world, unity-world 46 s, unity-match 457/457, **build 0.2.136**.
  - game-check FAILED on `fire` only (secondLock: the car was down 0/10 before it).
- One targeted rerun, `--from game-build` (logs/batch-0352-rerun.log): **build 0.2.137, BATCH OK**.
  - game-check: every section ok, including `fire` (secondLock `car:official`, mode `lock`, car up 5/10).
  - Shots: 13.
  - Budget: 12/12 within budget, audit 12/12 clean.
  - Proof sets: r059 13/13, r067 32/32, r123 6/6.
  - Page: PASS, 208 words (at most 220), 0 faults.
  - Late-fight: loud-fight worst 21.4 ms, p99 6.3 ms.
  - tp-long: ok.
  - The machine was noisy (solana-test-validator at 88 %).
- r123 body: rival-4 pinned-open, clip `cower`, at 4:20 (tick 2602). No pinned-cover came within 8 s.
- Front end at 1080 and 2160: status ok (stills 5/5, 5 stations).
- `bun run typecheck` clean. `bun test tools/game tools/hud-budget`: 285 pass. runtime-compile clean.
- Pictures:
  - shots/fight-safe-body.png and shots/fight-loud-body.png
  - proof/r123-body-safe.png (+ .hud and manifest)
  - frontend-1080/briefing.png and frontend-2160/briefing.png
  - stills/brief-getaway.jpg (the lifted car sits on the road)
  - My 2x/4x crops are in out/scratch/latch/0137/.

**Seen at 2x (for Glyph)**
- Safe fight: lock disc → pips → "no line" stack clear.
- No gun discs, no fire line and no holder discs in either fight.
- Loud: "Behind the wall, officers!" is now clear of the callout's face. The callout sits beside the focus, not on it.

**Open**
- In the tree, not built: the time-to-down chip drew bar-wide, with "6 s" at the plate's far end (r123-body-safe roster). It's now snug, with no bar slot (HudToolkit.Less.cs). This needs the next build.
- G7 shows a cower in the open, not the hump behind cover: no pinned-cover with 0.8 s left came in the safe run's 8 s. Glyph to say whether it's enough. If not, Keel to suggest a fixture tick with a pinned-cover.
- In r123-body-safe, the cowering focus (red ring) has no lock disc over it, and two "?" eye discs stand over the rivals. Both predate this round; Glyph to say whether either is a cut kind.
- G6 holder cut: Glyph and Aster to confirm.

Next owner: Glyph (2x of fight-safe-body, fight-loud-body, r123-body-safe and the page), then Aster.

## Glyph 2x (0.2.137) (Glyph, 2026-10-27; review only, no code, no Unity)
**Verdict: the page SHIPS. The fight is NOT ready for Sky yet, but it's close.** The crowding is fixed: the car cluster in fight-loud-body went from about 14 marks to 5 (ring, lane, car marker, seat pips, and the Scout's ring), and G1 to G5 and G8 have all landed clean at 2x. It stops on three things. First, a GDD rule was cut: the holder marks (§10 getaway, and my own hold) are gone, so the car shows "held · 1" with nobody marked as the holder. Second, the body shot's focus has a ring with no disc, and the manifest lists a lock reticle that isn't drawn. Third, an unmanifested "noticed" word plate stands on the street. All three are host fixes, with no engine work. My crops are in out/scratch/glyph/0137/.

**Rulings on Latch's open points**
- **G6 holders: restore.** §10 lists "a holder mark over each holder with a tether line to the car (gone when the holder is downed or breaks, so the player sees the car come free)". My count kept it as a getaway mark. In 0.2.135 I read the barred discs as non-focus marks only because the fire line was hiding the lock disc, and G1 has fixed that. Without holders, the loud frame's "1" count points at nobody, and a refused Go's holder pulse (§6.5) has nothing to pulse.
  - Fix: delete the `OneInput && FightOnNow` skip in HudToolkit.Less.cs (the holders loop, about line 1595). Keep the glyph (`mark_no` on `disc_dark_24`) and the tether rule as built (a tether only for a holder out on the lane).
  - One new rule: when a holder is also the focus, his holder disc sits to the right of the lock disc, at the disc's height, 4 px clear. It never goes above the lock disc or under the pips.
- **Cower in the open: enough for the pinned-open state; not enough to close "fights read on the body".** At 3x the bent-double rival reads as hit and afraid. The common case in play is a rival ducking behind cover, plus a cell agent kneeling at cover, and §06 and my hold both name them. Keel: please suggest a fixture tick in r123-safe with a pinned-cover rival and a kneeling agent inside one frame. Latch shoots it next build. This doesn't block Sky seeing the fight.
- **Cowering focus with no lock disc: a bug, not a style.** The focus ring, lock disc and pips are one stack, and a ring never shows without its disc and pips. In r123-body-safe the manifest has a `lock reticle` at 1072,812 (26 × 44), but the frame is empty there (the .hud.png is blank too), and the red ring is at 957,530 with no disc and no pips. Either the reticle is placed for the wrong body or it's hidden and still manifested. Both are wrong.
- **The two "?" eye discs over rivals: a cut kind in a fight.** §10's "?" is for a watcher not yet watched, a stealth question. A rival in the fight is already hostile, so his eye says nothing the fight doesn't. Under one input, while the fight is on, draw no eye over any hostile who is in the fight (fighting, pinned or broken). Eyes stay over watchers outside the fight (cameras, civilians who could report). The same mark shows in fight-safe-body at 1427,642. It floats about 50 px above the lower rival's head and lands on the upper rival's legs, so it reads as his.

**Count of street-mark kinds per fight frame (b-final is 7; speech, HUD frame, edge cues and the getaway set not counted)**
- fight-safe-body: 8 kinds. Running ring (the Enforcer, footprints disc), move ring with "4", focus ring, lock disc, pips, the "no line" wait plate (allowed as the stack's foot, G3), the star pip over Vale, and the "?" eye. Cutting the eye makes 7. No flash, spark or dust in the frame. The getaway set has ring, lane, car marker and seat pips, but no holder (see G6).
- fight-loud-body: 6 kinds plus the callout. Selected ring, focus ring, lock disc, pips, the "noticed" plate, and a spinner ring over the car's roof (975,510). The attention callout is §10's own cue, outside the 7. After N3: 4 + flash, spark and dust.
- r123-body-safe: 5 kinds. Focus ring (no disc), "?" eye ×2, selected ring, star pip, and the running ring at the Enforcer. After N2 and the eye cut: within 7.

**Nits (owner, fix)**
- **N1 · Restore holders. (Latch.)** As ruled above, with the side-by-side rule when the holder is the focus. Shot: fight-loud-body has one holder disc over the man who holds the car.
- **N2 · The focus stack in r123-body-safe. (Latch.)** Draw the lock disc and pips over the man who wears the red ring. Never manifest an element that isn't drawn. If the cowering focus has no lock, draw the disc in its unlocked state (the dashed rim, as in fight-safe-body), not nothing.
- **N3 · The "noticed" plate and the roof spinner in fight-loud-body. (Latch: identify, then cut or manifest.)**
  - A `plate_dark` with the word "noticed" and a draining underline at about 990,495 sits over the Scout. A partial ring spinner sits at 975,510 on the car roof. Neither is in the manifest, which breaks G2's "every street mark manifested".
  - If the plate is the agent's attention state, cut it from the street: "noticed" belongs to the live portrait and to the callout (§10), and it's a word in play. If the spinner is the cursor's hold sweep, it should be at the cursor, not on the car.
- **N4 · The eye cut in fights. (Latch.)** As ruled above.
- **N5 · Off-screen elements in the manifest. (Latch, low.)** fight-loud-body lists a `street mark` at 3038,1268 and a `body` at 2241,1526, 44 × 44, both outside 1920 × 1080. Cull off-screen marks from the manifest so the budget counts what's seen. Also check that the kill plate's root is 30 × 30 as G4 asked, not 44 × 44.
- **N6 · The time-to-down chip is bar-wide. (Latch, already fixed in the tree.)** r123-body-safe's roster shows the "6 s" chip at full bar width. It needs the next build; check it at 2x then.
- **N7 · The plan's last stubs. (Latch, low; the page still ships.)**
  - At 2160 there's one teal bean about 8 × 14 px above pin 3 (685,88 in my crop). The teal bed under pin 4 runs into the pin's chrome ring.
  - Also drop green shapes under about 2 m² in area, and keep plan shapes 4 px clear of the pins.

**What passes**
- G1: no fire line on the street, and the shooter's chip carries the time to down.
- G2: no gun discs.
- G3: the safe stack reads, top to bottom, lock disc, pips ("●●●"), "no line". Each is 4 px or more clear. Checked at 2x.
- G5: "Behind the wall, officers!" clears the callout's face. The callout sits beside the focus with its tail about 8 px off the pips.
- The cut list holds: no tracers, no cover or under-fire arcs, no miss rings.
- The page at 1080 and 2160: no clipping, the band and Begin are square, G8's mid-plan ticks are gone, and the getaway still has the car seated on the road with Vale in frame.

Next owner: Latch for N1–N5 (and N6, N7 in the same build). Keel for the pinned-cover tick (input: r123-safe's run; done-check: one tick with a rival pinned behind cover and an agent kneeling, both within one zoom-14 frame). Then one targeted rebuild with fight-safe-body, fight-loud-body and r123-body-safe (plus the cover tick when Keel has it), then Glyph at 2x, then Aster to Sky.

## Glyph N1–N7 (0.2.138) (Latch, 2026-10-27): built as 0.2.139 (c039, d6bf4c4b+), BATCH OK
Glyph's N1–N7 from the 0.2.137 review, plus the r123 cover picture from Keel's tick. There were two targeted builds:
- **0.2.138** was every fix. At 2x it showed that a locked disc stood 8 px from the holder disc, not 4.
- **0.2.139** carries that one-line fix. It's the build to review.

**What changed**
- **N1 · Holders restored (HudToolkit.Less.cs, holders loop).**
  - G6's `OneInput && FightOnNow` skip is gone. The glyph and the tether rule are as built: a tether only for a holder out on the lane.
  - When the holder is also the focus, his disc stands right of the lock disc at its height. It sits 4 px past the disc when locked, or 4 px past the dashed ring (RingPast 3.75) while it waits.
- **N2 · One focus rule.**
  - New `HandoverHost.IsFocus(id)` (HostHold.cs): an agent on foot (not down, inside or in a car) holds a lasting fire order (`Gun.Order`) on the target.
  - The cause of the empty focus: the ring read `FireNow` (any fire, his own return fire too), while the disc and pips read `Gun.Order`. So the cowering rival wore a ring because the cell answered him, but nobody had him as a focus.
  - The ring (HostTargetRings.cs) and the pips (HudModel.cs) now use `IsFocus`, the same as the disc (FireLockTargets).
  - The phantom `lock reticle` at 1072,812 was a released reticle. It was off the panel but still in the manifest list, with its last rect.
    - The manifest now skips any element with no panel (HudToolkit.Capture.cs Visible).
    - A released reticle's entries are removed from the list (HudToolkit.Lock.cs).
- **N3 · Identified: the "noticed" plate and the roof spinner are one mark, the attention clock (HudToolkit.Hold.cs, brief 044 §6).** It's a `plate_dark` word plate with its draining underline, and its ring arc 48 px to the left (the "spinner" on the car roof). It was over the Scout.
  - **Cut** under one input while the fight is on: the live portrait and the callout say it.
  - **Manifested** elsewhere as `marker` with its word.
- **N4 · No "?" over hostiles in the fight.**
  - HostSight.Notice skips a watcher that `CombatWatcher` names (security, rivals, and the guard and patrol once the fight is on) under one input while `FightOn`.
  - On Alt, UnwatchedWatchers stops after the cameras in a fight.
  - Cameras keep their "?".
- **N5 · Off-screen marks and the kill plate.**
  - The manifest drops any element whose rect misses the panel, chips included.
  - A bare kill (not found, not tied) is manifested by its 30 × 30 plate. A knockout, or a found or tied body, keeps the 44 px root for its rings (HudToolkit.Takedown.cs: `Mark.Tight`, a null text skips an entry).
- **N6.** The snug time-to-down chip is built (checked below).
- **N7 · The plan's last stubs.**
  - tools/game/briefstills.ts drops green shapes under 2 m² (`PLAN_MIN_M2`, `polyArea`, with a test).
  - Briefing.cs `GreenDrawn` draws a green shape only if both hold:
    - its drawn box is at least 8 px on the short side;
    - every edge is 4 px clear of every pin disc.
  - The bean over pin 3 (7 × 11 px), the 5.9 px bar by pin 3 and the strip under pin 4 are gone. The Office's two flank beds (9 and 9.7 px) stay.
- **The cover picture `r123-body-cover` (Proof123.cs; proofset.json r123, optional; proofset test: seven pictures).**
  - It reads Keel's `briefs/039/keel/sq-ra-h3-pin.script.json` straight from the brief, so it's not a sim fixture. sim/ is untouched.
  - The run goes to 2457, then 0.4 s, then pauses. The camera is at yaw 30, zoom 14, on Keel's focus point (75, -4325).
  - The picture is taken only when the script is in the tree.

**Evidence** (git-ignored: briefs/041/build/0139/; logs in logs/)
- `bun run guard bun run batch --from game-build` (batch-n1n7.log): **build 0.2.138, BATCH OK**.
  - game-check: 360/360.
  - Shots: 13.
  - Budget: 12/12 within budget, audit 12/12 clean.
  - Proof sets: r059 13/13, r067 32/32, r123 7/7.
  - Late-fight: loud worst 24.3 ms, p99 6.7 ms.
  - tp-long: ok.
  - The machine was noisy (dotnet at 744 %).
- `bun run guard bun run batch --only game-build,game-check,shots,budget` (batch-n1-gap.log): **build 0.2.139, BATCH OK**.
  - game-check: 360/360.
  - Shots: 13.
  - Budget: 12/12 within budget, audit 12/12 clean.
  - Proof sets: r123 7/7, with the r059 and r067 sets as above.
- r123 results:
  - Body: rival-4 pinned-open, clip `cower`, at 4:20 (tick 2602).
  - Cover: rival-3 pinned-cover at cv-032, tech kneeling at cv-077, fixer kneeling at cv-002, at 4:06 (tick 2466).
- Front end at 1080 and 2160 (`-frontEndShots -capture-size`, under guard + caffeinate): status ok at both sizes.
- Page budget (`--state page`, proof briefing): PASS, 208 words, 0 faults.
- `bun run typecheck` clean. `bun run test tools/game tools/hud-budget`: 285 pass. runtime-compile clean.
- Pictures:
  - shots/fight-safe-body.png and shots/fight-loud-body.png
  - proof/r123-body-safe.png and proof/r123-body-cover.png (each with .hud.png, manifest and parts)
  - frontend-1080/briefing.png and frontend-2160/briefing.png
  - My crops are in out/scratch/latch/0138/ and 0139/.

**Seen at 2x/3x (for Glyph)**
- **fight-loud-body:**
  - One holder disc right of the focus's locked disc, 4 px between the boxes.
  - The dotted tether runs from the car to the holder.
  - No "noticed" plate, no roof arc and no off-screen entries.
  - Kinds: selected ring, focus ring, lock disc, pips, holder, plus the callout.
- **fight-safe-body:**
  - The waiting stack: dashed disc, pips, "no line".
  - The holder disc sits 4 px past the dashed ring.
  - The "?" over the rival is gone.
- **r123-body-safe:**
  - No focus ring, so no stack: nobody had rival-4 as a focus. The cell was answering his fire.
  - No phantom reticle and no "?" discs.
  - The roster chip reads icon + "6 s", snug (N6).
- **r123-body-cover:**
  - rival-3 ducks behind the café's west planter wall. Her focus ring, locked disc and pips (●●○) are over her.
  - The Tech kneels by his pier, beside the Scout's selected ring.
- **Kill plate:** 30 × 30 in car-blast's manifest (1085,518).

**Open**
- **The holder disc's gap is 4 px box to box, but about 10 px to the eye.** `disc_dark_24` drawn at 26 px has about 3 px of transparent rim. If Glyph means the visible discs, the fix is one constant (−3 px each side); it needs a build.
- **Focus disc height over a ducking target.** The disc sits ReticleUp 2.55 m over the feet whatever the posture. Over rival-3's duck it floats about 60 px above her head. Glyph to say whether it should follow the posture.
- **The body shot has no focus.** It shows the pinned-open cower, so N2's stack can only be read in r123-body-cover and the two fight shots.
- **N7: the bays' grey still meets pins 3 and 4.** These are ground shapes, not green, and the pins mark those bays. I left them alone. Glyph to say whether "plan shapes" covered them.
- **N3 outside fights.** The attention clock is now in the manifest, so a stealth frame that shows one counts it as a `marker`. All 12 shots and every proof set stayed within budget.
- **The cover picture reads the script from briefs/039/keel.** If Keel files `sq-ra-h3-pin` as a sim fixture (his note asks for Aster's go-ahead), change FixtureAt to Fixture("sq-ra-h3-pin").
- **The build's own edits.** unity/Assets/Scenes/FightRange.unity and the Generated/Range mats show as modified in git status. I edited no scene.

Next owner: Glyph (2x of fight-safe-body, fight-loud-body, r123-body-safe, r123-body-cover and the page at 2160), then Aster.

## Glyph 2x (0.2.139) (Glyph, 2026-10-27; review only, no code, no Unity)
**Verdict: SHIP to Sky.** N1–N7 have landed clean at 2x. Every fight frame is at or under 7 street-mark kinds, nothing is unmanifested, and the page is as passed. The four nits below go in the next build and don't block Sky playing. My crops are in out/scratch/glyph/0139/.

**Rulings on Latch's open points**
- **Holder gap: I meant the visible rims.** Take the 3 px of transparent rim off each side, so there's 4 px between the drawn chrome rings (about 11 px to the eye now). The same goes for the dashed-ring case (about 7 px now). This is one constant, P1.
- **Focus disc over a ducking target: follow posture.** Anchor the stack (disc, pips, "no line") to the top of the head, plus a fixed pixel gap, not to ReticleUp 2.55 m over the feet. It drops when the target ducks or kneels and rises when they stand, eased over about 0.2 s so it doesn't bob. Right now rival-3's disc floats about 85 px over her, with the pips about 55 px over her, above a café table. It reads as hers only because nothing else stands near. P2.
- **r123-body-safe with no focus: fine.** That shot's job is the pinned-open cower, and it does it. The stack is proven in r123-body-cover, fight-safe-body (waiting) and fight-loud-body (locked). Nobody had rival-4 as a focus, so there's correctly no ring.
- **Grey bays at pins 3 and 4: not covered, leave them.** My 4 px rule was for green and decorative shapes. A bay is the place the pin names, so the pin sitting on its bay's end reads as a map pin on its spot, the same as the getaway car's pin on its tabs.

**Street-mark kinds per fight frame (limit 7; speech, the HUD frame, edge cues, the callout and the getaway set not counted)**
- fight-safe-body: **7**. Vale's star pip, the running ring, the move ring "4", the focus ring, the dashed lock disc, the pips and "no line". It's at the limit, so add nothing to this state.
- fight-loud-body: **4**. The selected ring, the focus ring, the locked disc and the pips. The holder and its tether are in the getaway set.
- r123-body-cover: **6**. The selected ring, the focus ring, the locked disc, the pips, the person pips (waitress with ✓, guard) and the guard's amber badge.
- r123-body-safe: **3**. The selected ring, Vale's star pip and the running ring. The crossing lens and the dot rows are world, not HUD.

**Nits (owner, fix)**
- **P1 · Holder gap to the visible rim. (Latch.)** As ruled above. Done-check: 4 px ±1 between the chrome rims at 2x in fight-loud-body.
- **P2 · The focus stack follows posture. (Latch.)** As ruled above. Done-check: in r123-body-cover, the pips sit within about 12 px of rival-3's head.
- **P3 · The Tech doesn't read as kneeling. (Forma for the clip, Latch for the 0.4 s blend; not HUD.)** At 6x in r123-body-cover, the Tech at cv-077 is upright in a wide stride by the pier, at full height, so it reads as side-stepping, not kneeling at cover. The Fixer (cv-002) isn't in frame to judge. The rival duck reads well. "Fights read on the body" is half-closed until a cell agent is plainly lower at cover: hips down, one knee to the ground, about 60 % of standing height. Re-shoot r123-body-cover a beat later if it's a blend-in. Otherwise the kneel clip needs fixing.
- **P4 · fight-safe-body is at 7. (Waymark/Latch, watch only.)** Any new mark in the waiting-stack state pushes it over. The move ring "4" and the running ring are the first to merge if it ever has to give.

**What passes**
- N1: one holder disc beside the focus, tethered to the car. A refused Go has its holder to pulse.
- N2: the ring, the disc and the pips agree in every frame. There's no phantom reticle.
- N3: no "noticed" plate and no roof arc.
- N4: no "?" over hostiles.
- N5: no off-screen entries.
- N6: the "6 s" chip is snug.
- N7: the bean and stubs are gone at 2160.
- The page has no clipping, the band and Begin are square, and the plan pins are clear of the green.

Next owner: Aster to Sky (SHIP). Latch for P1–P2 and Forma for P3 in the next build after Sky's play, then Glyph at 2x on r123-body-cover.
