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
