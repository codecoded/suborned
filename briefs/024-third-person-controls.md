# 024 · Map and ground: a hybrid prototype (third-person controls, awareness, squad wheel)

**Status (2026-10-19):** proposal for Sky. Author: Glyph. Asked by Sky, playing on the MacBook Air: "in third person view I want to a) rotate camera with trackpad, on drag, and b) control the player with wasd... proper player / camera controls that any 3rd person laptop / desktop game would have", then widened: "2.5D loses the beauty of the world... more Ghost Recon style gameplay could be better." Widened again by Aster: the overhead view stays as the planning and tactical map; you drop into one agent in third person to play the approach, Take or fight yourself; the other three carry out their orders. No GDD, code or data has been edited.

**This is a pillar-level change** (§5), so it needs Sky's yes before any GDD edit (v0.25: §01 fantasy line, §10 camera, §14 Latch, Keel, Glyph blocks) and before Aster splits the work. It stays a toggle: the overhead view remains the default and the only view the balance is measured in.

## Objective

The smallest version Sky can play on the Handover to judge the direction: one build where you press a key, drop from the map into one agent, walk to Inez or the pickup yourself, see who sees you, give the squad a quick order without leaving the ground, and press the key again to be back on the map. If it feels better than the 2.5D view, the next brief grows it. If not, it is thrown away at a small cost.

Starting point is `HostFollow.cs` (the T follow view: perspective camera behind the agent, boom pulled in by walls, props and passers-by hidden). What is new: free movement, a real orbit camera, awareness at eye level, the squad wheel and the map switch.

## 1 · Ground mode: the agent and the engine

**Recommendation: a kinematic controller that slides along the engine's own walk grid, not Unity's physics CharacterController.** The city has no colliders (HostFollow.cs says so; it indexes renderers to fake a boom). Building colliders from the grid would give two truths for "can I stand here". With `Grid.Free(p)` as the only collision, the controller can never stand where the engine refuses, and it needs no scene work.

- **Move** each frame: desired velocity = camera-relative WASD, diagonals normalised. Try the full step; if `Free` fails, slide along x then y. Heights come from `frame.Ground` (deck 9.43 m, concourse 24 m work as today).
- **Smooth:** 0.12 s to speed, 0.08 s to stop, the model turns to its heading at up to 540 degrees a second, walk and run clips from the stand-ins (brief 020).
- **Speed: the rulebook's, for the prototype.** Walk 1.4 m/s, run 2.8 m/s (`op-speed-cm-s`, `op-run-speed-cm-s`). So every "n s" price and every schedule stays true. Expect 1.4 m/s to feel slow at 5.5 m behind; if Sky says so, Motive lifts the pace for everyone (the clock's truth), never only for the agent you hold.
- **Shift** held runs, **G** latches run (as today's gait toggle). Carrying Vale, nobody runs (existing refusal pop).
- **Who moves:** the possessed agent, one at a time. Entering the mode collapses the selection to that agent. Shift-click and Shift+F-key group adds are off in ground mode.
- **Taking the stick over an engine order** (they walked there from the map): input cancels the walk and the act they walked for (free: nothing is paid until the act starts, same as a new click today). While an engine walk is the only thing happening, the controller follows the engine's position and the camera trails behind the heading.
- **Busy** (a Listen, a Loop, an Enter under way): the controller freezes and input is refused with the existing `agent-busy` pop, never a silent cancel. C or the cross cancels, as now. Boarded, inside, down, carried: input refused with the engine's own code.
- **Pause (Space):** input is ignored while paused; the camera still orbits; the pause pill pulses. **Speed is locked at 1x in ground mode** (and Next Event is off): at 2x or 8x the engine steps several ticks per frame and would read one pose for all of them.

### For Keel (scoping now), what Glyph needs from the engine

1. **Pose in:** per step the host sets the possessed op's position (cm). Recorded as an input stream beside the orders (tick, op, x, y, sent only when it changes), so a replay is exact: same orders plus same poses gives the same result.
2. **Sanity:** refuse a pose beyond run speed times one tick plus slack, off the grid, or for an op that is down, inside, carried or busy; the engine may also *set* the pose (after Enter, in the car, knocked down) and the host obeys.
3. **Same reads from the pose:** reach (`too-far`), Detection, SeenAt, cover, fight hit rates, "noticed where an act would be".
4. **No new rules needed for the first play.** A notice ramp (a short time before a watcher counts you) is the one rule that would make ground play fair, see §3; it is a question for Motive, not a blocker.

For Latch: the possessed agent's drawn transform is the controller's, not the engine's 10 Hz interpolated view (that would add 0.1 s of lag and a jitter).

## 2 · The camera (trackpad first, mouse second)

Standard third-person PC (Witcher 3, Elden Ring, Fortnite, WoW) with one deliberate change: the cursor stays free, because the pointer is how you read prices and target.

| | Recommendation |
|---|---|
| **Orbit** | **Click-drag**: hold the left button (trackpad: press and slide) on anything that is not the HUD, past 6 px, and the camera orbits the agent's chest (1.5 m). Reads `mouse.delta`, not position, so a drag never stops at the screen edge. Mouse: right-drag and middle-drag orbit too (a right press that does not move 6 px still acts, on release). Arrow keys orbit (left and right yaw, up and down pitch) for one-handed play. Alt + middle-drag still works. |
| **Left button, otherwise** | A click without a drag on another agent swaps to them; on the ground it does nothing (click-to-move is off in ground mode: walking is the keys). Box select is off. |
| **Free-look option** | `controls.lookMode = Drag (default) or Locked`: cursor hidden and locked, a centre reticle, Alt frees the cursor. About 20 lines; Sky may flip it after playing. If Drag feels wrong on the trackpad, this is the first thing to try. |
| **Pitch** | 4 to 70 degrees above the horizon, default 16 (matches today's height at 5.5 m). Never below 0.5 m over the ground at the camera, from `frame.Ground`, which also covers the decks. |
| **Zoom** | Two-finger scroll or wheel: distance 2.5 to 14 m, default 5.5. Proportional (each unit scales the distance by about 6 %), eased 0.12 s. **Fix, found in the audit:** today's wheel code uses only the sign of the scroll (0.75 m a call), and a trackpad sends many small events a second, so one flick hits the end stop. Horizontal two-finger scroll is ignored (no accidental orbit). Pinch is not offered: Unity's Input System does not surface the Mac pinch; a native plugin is a later question. |
| **Collision** | Keep HostFollow's boom (indexed solids, 0.4 m margin, in at once, back out eased 4 m/s), now a 0.35 m sphere, plus the ground clamp above. Keep the existing hiding: props up to 6 m wide on the line or within 1.6 m of the camera, and passers-by covering 12 % of the agent. Wall behind you: pull in, never up. |
| **Sensitivity** | Yaw 0.25 and pitch 0.18 degrees per pixel at 1080p (scaled by screen height), a 0.5x to 2x slider each. Trackpad and mouse share it. |
| **Inversion** | Mouse-look semantics by default (drag right turns the view right; drag up looks up). Invert X and Invert Y switches; scroll follows the system's natural-scroll setting, plus an Invert zoom switch (Latch checks the Air's direction on first run). |
| **Recentre** | **Superseded (Sky, 2026-10-20, see Decisions):** the camera is a chase camera. As the agent moves it swings behind its heading (eased, about 0.5 s, no overshoot) at a constant distance and pitch; standing still it holds where it is. A drag-orbit or a Q/E turn is a look-around that eases back behind the agent about 1 s after it ends. Zoom is the player's setting and does not drift; the boom pulls in only when a wall really blocks the line, then backs out. (Was: Z recentres, no auto-recentre while keys are held.) |
| **Settings** | A "Camera" row in the Esc menu: the sliders, the two inversions, lookMode. Stored in `PlayControls`. |

## 3 · Ground-level readability: who sees you

Ground cones do not read at eye level: a flat polygon seen from 16 degrees is a sliver. Hitman and Ghost Recon read it with markers on the people and the screen edge, not with floor shapes (sources in `briefs/024/research/notes.md`; Hitman: a white marker means they can start to notice you, a yellow arrow fills as suspicion builds, full means seen). The engine already answers every one of these questions: `Detection()`, `SeesNow(watcher, p)`, `SeenAt(p)`, `ConeFan(watcher)` and the "?" of `sight.unknown`. Nothing is invented; it is drawn from the possessed agent's pose.

**The rule that keeps "cones are the Scout's skill":** only what the cell has learned shows a direction. A watcher that could see you and is not known shows as "?" on your own eye, with no pip and no edge marker (that would reveal where it stands).

**What a watcher "sees" means** (engine today): a walk is not noticed, an act or a run is, where it is done. So the eye is the price of acting here, ambient and free to read.

Prototype set, in priority order:

1. **Your eye** (an icon over the agent's head: one of the two allowed): shut = an act or run here is unseen, open = seen, "?" = an unknown watcher could. From `SeenAt` at the agent's position, refreshed 4 times a second. This is also the run chip's eye, so Shift shows its price before it commits.
2. **A pip over each known watcher that covers you** (guard, patrol, camera, rivals): the kind's own shape and motion cue (lens, eye with chevron and so on), drawn unlit and emissive, outside the post grade, each with its own colour as the style contract says. Shown within 30 m. Two per watcher at most, as for people.
3. **An edge marker** for a known watcher that sees you and is off screen or behind the camera: a chevron on the screen rim pointing the way (rotated by the camera yaw), up to 3, fading in 0.3 s.
4. **The learned cone as a curtain.** The one learned cone the Scout is showing (and all with Alt) gets, besides the ground fill, an emissive 1.2 m vertical ribbon on its rim from `ConeFan` (`Stop` gives the dashed far edge, near circle and blocked edges). A vertical rim reads from eye level where a flat fill does not.
5. **The plot scope** stays the top-down truth and turns heading-up with the camera (it reads the camera's own yaw, so it follows a free turn; Latch verifies there is no snap to the four).
6. **Roster:** each of the other three keeps its own eye on its portrait, so you see the squad being seen without looking at them. The three off screen keep their edge cameos (brief 016); the follow view hides these today.
7. **When someone notices you:** Alert pill as now, plus a 0.3 s rim pulse and one cue for Chime. No sentences.

**Question for Motive (not a blocker):** the engine has no notice ramp. Hitman's fill is what makes eye-level stealth fair; without it, stepping into a cone edge is instantly "seen". For the first play keep the engine as it is and see if Sky asks for it. If yes: a 0.5 to 1.5 s ramp scaled by distance and by what you are doing, shown as the pip's fill.

## 4 · Acting on a person from the ground

The overhead view's act path stays, so the price rule is the same one:

- **Focus:** the thing under the cursor, else the nearest valid target in front within reach (`ActReachCm`), cycled with **Tab** when a verb is picked (the existing target mode). It gets a ring on the ground and the **price chip** at its head: the existing one-line hover chip (verb, seconds, `$`, Trail, Alert; the "found" glyph on takedown numbers). Alt for the breakdown, as now.
- **E acts on the focus.** A verb picked (1 to 4) fires at once, the existing fast path. None picked: E opens the action panel, one line per way, each priced. Right-click does the same at the cursor.
- **Out of reach:** **Superseded (Sky, 2026-10-20, see Decisions):** a right-click act (or, with the lab, fire) on a target out of reach walks the agent there and acts, exactly as on the map (the engine's walk-and-act), with the map's price line (the walk's seconds) on hover. For fire, the engine's preview has no walk-in for a driven agent (a driven agent is never walked), so the ghost and the seconds appear once the walk starts; a hover forecast needs a Keel read (PreviewFire as if released). WASD during that walk takes the stick back and drops the walk and its act; the chase camera trails the walk. (Was: no auto-walk in ground mode, you walk.)
- **Holds stay holds.** The going-loud card and a seen takedown commit on a 0.5 s hold; E becomes one more source (`"e"` beside `"right"`, `"left"`, `"both"` in `HostFight`), the ring fills at the target.
- **Fights:** right-click or E on a hostile fires as today; hit rates stay deterministic ("down in n s"). Free movement matters because the engine now reads your position for cover and range. Stance V, draw F, cease X, cancel C are unchanged. There is no aiming skill, no crouch, no cover button in this prototype.

## 5 · The squad

The other three keep doing what they were ordered, in real time, as in the map view. An agent with no order holds.

**Command wheel: hold Q.** A radial of four, opened by holding, chosen by direction (trackpad or mouse delta, the cursor warps to the centre) or by 1 to 4, committed on release; Esc or release in the centre cancels. **The clock pauses while it is held** (the pause-menu pattern: restore what it was) so a quick order is never punished.

| Segment | Does | One-line price |
|---|---|---|
| Hold | stops the other three's walks (the engine's walk-only `cancel`); an act under way is never cancelled by it | free |
| Come | all three walk to spots round you (the snap and 0.9 m spacing `MoveSelection` already uses), never run | the slowest one's walk in seconds |
| Go there | all three walk to the ground point at the cursor or reticle | same |
| Fall back | the panic order, only while a fight is on | its card, as now |

- **Group only, in the prototype.** Orders for a particular person (Listen, Turn, Loop) are given from the map, where the full panel and prices live. That is the split: the map plans, the ground executes.
- Both-buttons panic is off in ground mode (it would fire while you drag-orbit and press the other button); Fall back is where it lives.
- **Next, not now:** Wildlands' Sync Shot: mark a target, the squad acts. Here: with a focus, the wheel shows who can do what to it (each segment a portrait and its best verb, one priced line). A held "on my mark" for prepared acts would put Time into the player's hand, but the engine starts acts at once today, so it is a Motive and Keel question.

## 6 · Switching map and ground

**Sky, 2026-10-20:** the view changes only when the player switches it (T today; M is not bound). Nothing switches it automatically: not a freeze, target mode, a fight springing, the engine ending a drive, an agent going down, a restart or the fail camera (it plays in the view the player is in). See Decisions.

- **M** (and **T**, kept) toggles. **Map to ground:** drops into the selected agent (the lead of the selection, else the Scout), 0.6 s swoop down from where the map is looking (the existing swing). The chip beside the scope becomes **Ground [M]** and is clickable (trackpad). **Ground to map:** 0.6 s up; the map opens at its last zoom and yaw, **centred on the agent you held**, not where you left it.
- **Swap agent without the map:** **Tab** and **Shift+Tab**, F1 to F4 (on a stock MacBook those need Fn, a standing audit finding, so Tab and the portraits are the way) or a click on a squad member. Control moves at once; the one you left holds, or finishes its order. The camera blends 0.45 s, or dips to black for 0.12 s past 30 m.
- A run opens on the map (the briefing, the cell in view), and H19 teaches M once.
- The map keeps its real-time default, Space pauses. A `controls.mapPauses` switch (off) lets Sky try it paused.

## 7 · Keys

Audit of `PlayControls.cs`, `HandoverHost.Controls`, `HostFollow`, `HostTurn`, `HostPick`, `HostFight`. "Ground" is the proposal. Everything not listed is unchanged and mode-free.

| Input | Map today | Ground |
|---|---|---|
| W A S D | pan | **move**, camera-relative |
| Arrows | pan | **orbit** (held) |
| Q | turn view a quarter | **hold: squad wheel** |
| E | turn view a quarter | **act on the focus** |
| Z | free | **recentre behind** |
| Shift | add to group | **hold: run** (adds off; Shift+Tab backwards, Shift+H unchanged) |
| Tab | step stack / next target / next agent | same order; Shift+Tab backwards |
| F1 to F4 | select agent | **swap** to that agent (Fn on a Mac laptop) |
| 1 to 4 | verb knobs; panel keys | same; also the wheel's segments |
| Space | pause | same; movement ignored while paused |
| T, M | follow view / (free) | **toggle map and ground** |
| Esc | panel, briefing, verb, history, then the menu | same; never leaves ground mode |
| Alt | Peek; with middle-drag, free turn | Peek; free orbit with middle-drag |
| Ctrl | force fire (with right click) | same (on a Mac, Ctrl-click is a right click at the OS: check on the Air) |
| G, C, X, F, V, R, I, L, H, K, B, ` | gait, cancel, cease, draw, stance, response, intel, log, hints, ack, briefing, strips | same |
| = and minus, N | speed, next event | **off** (locked at 1x) |
| Left drag past 6 px | box select | **orbit** |
| Left click | select; move | swap agent; nothing on ground |
| Right click, press | act | act (on release if it did not drag) |
| Both buttons | panic | off (wheel: Fall back) |
| Wheel | zoom; **also steps overlapping things** | zoom only (the stack steps with Tab; found in the audit: today the wheel does both in the follow view) |

An on-screen **key strip** shows on entering and folds after 6 s or at the first move: keycaps with one word each, no sentence: `WASD move` `Shift run` `E act` `Q squad` `drag look` `scroll zoom` `Z behind` `M map`. It returns while Alt is held. Fable owns the words, H19 the first-time hint. It counts against the HUD budget (≤ 15 % at rest) and is one row.

## 8 · The HUD in ground mode

- **Kept:** roster, order plate (the held agent's verbs), status pill, known-people discs, plot scope, notes key (Intel, history), the action panel, price chip, speech and pops. At most two icons over a person.
- **Added:** your eye, watcher pips, edge markers, the focus ring and prompt, key strip, mode chip, the lens mark on the held agent's portrait.
- **Rebuilt for perspective:** the follow view hides every orthographic world mark today (rings, pins, the move ring, edge cameos, **the Vale step markers**). The Handover cannot be played without the goal. Replace them with: a ground ring plus a **light beam** at the goal (door knob, Vale, the car) with the distance in metres, no words, from the existing goal-line state. This is the most Latch work in the HUD.
- **Pillar HUD holds:** every order still shows its full price first, one line per way; Alt for the breakdown; no sentences except speech.

## 9 · What this means for the pillars (for Sky to judge)

This puts a second fantasy next to "commander": you are also the person in the street. **Commander > avatar** (the tiebreaker) would become "commander and avatar, the map plans and the ground executes". What holds: orders and prices are unchanged (an act is still an order with a price), the engine decides outcomes (hit rates, noticing, reach), and the replay still reproduces everything because your steps are recorded as inputs. What strains: **preparation > improvisation**, because walking round a guard by hand is improvising, and reflex could decide what the plan used to. Two things limit it: the awareness cues only show what the cell has learned (so the Scout's Watch becomes the way to read the street from the ground, which strengthens the Scout), and there is no aiming skill. One agent in your hand also costs you the other three's attention, which is the Ghost Recon squad tension. **People > powers, orchestrate the city** gain: you see the windows (the guard's door, Inez's round) with your own eyes. **Recommendation:** approve it as an experimental toggle for three Handover plays, judged on three questions: does the street look and feel better than the overhead view, did "who sees me" read without a cone, did the wheel replace trips back to the map. Decide after that whether the GDD changes.

## Prototype scope

In: §1 to §8 on the Handover, routes A and B. Out: crouch, cover, prone, aimed fire, climbing, vehicles, gamepad, pinch zoom, per-agent wheel orders, Sync Shot, the notice ramp, other missions.

## Done-check

1. **Plays through:** Sky completes route A and route B holding an agent for at least half the mission, using map, wheel and ground at least once each, with no dead end (no marker, no way back to the map).
2. **Deterministic:** a recorded ground session (poses plus orders) replays hash-identical in the Unity match; the existing 143/143 scenarios are untouched; a Keel scenario shows a pose-fed walk and an order-fed walk give the same `SeenAt`.
3. **Controls:** a scripted test with the camera at yaw 30, 120, 210, 300: W goes the way the camera looks, S back, diagonals the same speed as straights, Shift gives 2x; slides along a wall rather than stopping; never leaves `Free`; pose stream at most 10 a second. Key table: no input bound twice in one mode (a unit test over `PlayControls` and the ground overrides).
4. **Camera:** shots at pitch 4, 16, 45, 70 at the café front, the pickup and the fight; no clip into the ground or a wall; a flick of the trackpad scroll does not hit the end stop.
5. **Readability:** uncoached, ground HUD at rest, ask "who can see you?", "where is the door?", "can you do this here?"; at least 5 of 6 answer from the eye, pips, edge markers and the beam (a T10-style check for Suture). Cone curtains pass Glint's grade exclusion check.
6. **Budget and speed:** HUD budget ≤ 15 % at rest and 22 % with a card open, key strip included, from `bun run verify`; frame times at the same spots as brief 016's follow proof, 60 fps at play zoom on the Air.
7. **Sky plays it on the Air with the trackpad only, then once with a mouse,** and answers the three questions in §9.

## Decisions

- **2026-10-20, Sky (third-person play):** "the auto camera switching is really annoying and the camera should swing behind the player as they move, not have them have to drag and rotate, try keep it constant distance and angle."
  - No automatic switch between map and ground. Removed in the host: the restart's return to the map (`HandoverHost.Begin`), the return to the map whenever a drive ended (`HostDrive.DriveExit`: a freeze or target mode letting go, the engine ending the drive, the agent going down or inside; now a plain follow view; only T goes back to the map), and the fail camera's exit to the map and its own camera path while the ground view is on (`HostFailCam`).
  - A chase camera on the ground (`HostFollow`): see §2 Recentre. Replaces "no auto-recentre".
- **2026-10-20, Sky:** "a player if they right click action whilst in third person, should move towards the target automatically - not stop." Out of reach while driven, the act walks in and acts as on the map; WASD cancels (§4). Replaces "No auto-walk in ground mode".
- Evidence: `briefs/024/chase-curve.mp4` (the Scout walking an S-curve in the ground view, the camera behind her throughout).

## Evidence

`briefs/024/` (shots, frame times, the replay hash, the key-table test output, `research/notes.md`, Sky's three answers).

## Next owner

Sky approves or changes this. Then Aster splits it:
- **Keel** first: the pose input, its sanity rules, its replay record, the 1x lock (§1).
- **Latch**: controller, camera, switch, wheel, perspective goal markers.
- **Glyph**: awareness art (eye, pips, edge markers, cone curtain), the strip, the wheel, the chip. **Glint**: the curtain outside the grade.
- **Motive**: speeds and the notice ramp question. **Fable**: the strip words and H19. **Chime**: the notice cue. **Suture**: the done-check. **Redline**: curtain and boom cost.

## Revision: action-game controls (2026-10-21)

**Sky:** "yeah, think so - just find E for actions kinda odd, vs mouse buttons but if it's the norm wire it in." Asked: third-person controls the way action games do them (GTA, The Witcher): a prompt on whatever you face, act with E, hold E for the options, right mouse aims, left mouse fires. Commander view: untouched. This revision is the third-person chase camera only, and it supersedes the third-person parts of §4 and §7 above where they differ. Author: Glyph. Mockup: `briefs/024/action-controls/index.html` (1920 x 1080, keys 1 to 2 to 3 or the buttons switch prompt, hold-E ring, aim; rendered checks `shot-1..3.png`; rebuild with `bun briefs/024/action-controls/build.ts`). Sources: `briefs/024/research/action-defaults.md` (community key guides, not checked in each game).

### Is E the norm? Mostly.

| PC default | Interact | Options | Aim | Fire | Draw / holster | Sprint |
|---|---|---|---|---|---|---|
| GTA V | E context, F vehicles | hold Tab: weapon wheel | RMB | LMB | aiming draws; wheel | Shift |
| Witcher 3 | E | Q sign; RMB parry | (RMB parry) | LMB attack | 1, 2 | Shift |
| Red Dead 2 | E (guides split E / F) | hold Tab: wheel | RMB | LMB | aiming draws | Shift |
| Cyberpunk 2077 | F (fixed) | | RMB | LMB | Alt | Shift |
| Ghost Recon Breakpoint | E | | RMB | LMB | not listed | Shift |
| Mass Effect (Legendary) | Space (E in ME1) | | RMB | LMB | | |

Aim RMB, fire LMB and sprint Shift are universal. Interact is E in four of six, F in two, Space in one: E is the plurality, not a law, so it is the default and rebindable. Nobody uses "hold E for options" as a standard (the wheel sits on Tab); that part is ours, and it is safe because the prompt shows it. The lesson from Red Dead 2 (tap and hold on one button, "inconsistent", see 038) shapes the rules below.

### Scheme (third person only)

The cursor stays free (it is how the prompt follows what you point at). Prompt target = the thing under the cursor, else the nearest valid one in front within 8 m (Z steps it). WASD, Shift, G, F, X, C, V, Q-hold wheel, 1 to 4, Tab and the arrows are as in §7 and brief 030; only the rows below are new or changed.

| Input | Does |
|---|---|
| **E, tap** (act on release, under 0.22 s) | Take the prompted way, if the three checks of brief 030 pass (one clear best, no "?" outcome, not harmful). If one fails, the prompt shows the dots and E opens the ring instead. It never guesses. |
| **E, hold 0.22 s** | The options ring on the target (brief 030 ring, same wedges and order). Release over a wedge takes it; a harmful wedge fills over 0.5 s under the pointer and only takes if still held when full. |
| **Left click on the prompted target** (setting `controls.clickActs`, default **On**) | Exactly E: tap acts, hold 0.22 s opens the ring. Only on the prompted target; a click elsewhere does what it does today (swap agent, nothing on the ground). |
| **Left drag past 6 px** | Orbit look-around (kept: the trackpad has no middle button). |
| **Right button, hold 0.22 s** | **Aim**: draws the gun if holstered (price "draws 0.6 s" on the chip), cursor becomes the reticle with its band ticks, the agent turns to face it. Release lowers the aim; the gun stays drawn until F or the engine's auto-holster. `controls.aimMode` Hold (default) / Toggle (for the trackpad's two-finger click). |
| **Left button while aiming** | **Fire** at the reticle. A hostile of a fight fires on press (down in n s). Anything not hostile (guard, civilian, patrol, rivals with no fight): hold 0.5 s, the reticle ring fills, release early cancels, and the pill carries the loud mark. Never on Vale. |
| **Left button, not aiming** | Never fires. (No hidden "hip fire"; a hostile's prompt says Aim, with the RMB cap.) |
| **Right button, tap** (under 0.22 s, under 6 px, not aiming) | The ring at the target, as on the map (brief 038: right click always opens the ring). The chosen way walks in if out of reach and acts, without stopping. On the ground: stops the current walk, as today. |
| **Middle drag, arrows** | Look-around, unchanged. |
| **Esc, right tap on empty ground** | Close the ring, cancel a walk that has not started its act. |

**Mouse binding: left click on the prompted target.** Why: it is the natural mouse twin of E (point, click), it needs no new button, it cannot clash with aim and fire because fire exists only while the right button is held, and it works on a trackpad (tap-to-click) where a middle or side button does not exist. The 6 px and 0.22 s rules already used for orbit and the ring keep a click from becoming a drag. It is one setting (`clickActs`), not a second scheme: the prompt always shows both caps ("E" and the mouse glyph) when it is on, and one cap when it is off. Rejected: middle mouse (no trackpad, and it orbits), side buttons (not on every mouse), right button (aim).

### Rules (Sky's, with the numbers Latch needs)

1. **The prompt says what the act will do.** One line over the target, 34 px high at 1080p: key caps, icon, one word, then the price in the panel's order (walk plus act as one time, `$`, Trail, Alert, the eye or "found" glyph). No sentence. The dots at its end mean more ways are one hold away. Alt gives the breakdown. The same line is the reticle's chip while aiming. The ground ring marks the target. Same ranking as `IntentFor`, so the prompt and the act agree.
2. **Harm is never the default.** Kill, Knockout, Pressure, Fire at a non-hostile and any Fire with a holstered gun are never lit and never taken by E or a click. Kill and Fire at a non-hostile need the 0.5 s hold (the going-loud card's hold, the same ring-fill art); a Knockout that a watcher would see takes it too (`SeenGate`); Pressure is a plain pick on its dashed wedge. A drawn gun does not change this: a click on a civilian never fires.
3. **Firing stops a walking agent; a running one keeps going.** While the trigger is down at walk pace, movement input is ignored and the agent plants (it resumes on release if the keys are still held). Running (Shift or G latched) keeps moving, with reduced accuracy: starting value hit chance x 0.6 (Motive tunes), shown at once by wider band ticks and a longer "down n s". Keel: the drive sample's run flag feeds the hit rate, so replays stay exact.
4. **The camera never switches view by itself, and never changes distance or pitch.** Aiming, the prompt, the ring and firing do not move it (no over-the-shoulder, no zoom, no pitch change). As the agent moves it swings behind the travel heading (eased about 0.5 s, constant distance and pitch). It follows the travel heading, not the aim facing, so turning to aim does not swing it. Standing still it holds. A look-around eases back about 1 s after it ends.
5. **Right-clicking a target in third person walks the agent to it, without stopping.** The ring opens at the target; a way that is out of reach starts the walk and the act as one order (one time on the line, the move ring on the ground). E on a prompted target beyond reach (within 8 m) does the same. Any WASD input takes the stick back and drops the walk and the act.
6. **WASD keeps working after any click.** No pointer event, ring, panel, portrait click or HUD button may keep keyboard focus: Latch blurs the focused UI element after every pointer-up and when a ring or panel closes, and the movement input never reads UI focus. While a ring is open WASD still moves the agent (the ring stays on its target).

### Key strip and HUD

One row, one word per cap: `WASD move` `Shift run` `E act` `hold E options` `RMB aim` `LMB fire` `F draw` (and `Q squad` `M map` on the existing strip). HUD at rest adds only the prompt over the focused target and the ground ring; reticle and ring exist only while in use. Budget: at rest 15 % or less, ring open 22 % or less (`bun run verify`).

### Done-check (Latch builds against this; Suture runs it)

1. **One meaning per input per state:** a unit test over the third-person input map: E tap, E hold, left tap, left hold, left drag, left while aiming, right tap, right hold, middle drag, each resolves to exactly one result for each of: free, aiming, ring open, walking, running, carrying Vale.
2. **Prompt equals act:** for each target kind in brief 030's table, the prompt's word and price equal what E (and a left click, with `clickActs` On) then does; if a check fails the prompt shows the dots and E opens the ring.
3. **No blind harm:** scripted pointer, 20 E taps, 20 left clicks and 20 aim-and-click presses on a guard, a civilian, Vale and a door, holstered and drawn: zero Kill, Knockout, Pressure or non-hostile Fire without the hold.
4. **Fire and movement:** LMB while aiming and walking stops the agent; running does not and the hit chance shown drops; a recorded session replays hash-identical.
5. **Camera:** with aim, prompt, ring and firing, camera distance and pitch stay within 1 % of their values; no view switch; on moving, yaw settles behind the heading within 0.6 s.
6. **Right tap and walk:** a right tap on an out-of-reach target opens the ring; the chosen way walks without a pause and acts; any WASD press cancels both.
7. **WASD after clicks:** after each of a HUD button click, a portrait click, a ring open and close, a right tap, a left click on a prompt and Esc, W moves the agent within one frame.
8. **Setting:** with `clickActs` Off a left tap on the prompted target does nothing and the prompt shows only the E cap; E is unchanged. The act key is rebindable.
9. **Budget and look:** HUD at rest 15 % or less and ring open 22 % or less; prompt, ring and reticle match `briefs/024/action-controls/` at 1080p (Glyph reviews a 2x crop of each).
10. **Sky plays** one Handover in third person using E and left click only, then once with aim and fire, and says whether he reached for the wrong one.

### Questions for Sky

1. Left click acts by default (recommended, you prefer the mouse), or E only until you ask?
2. Keep the right-button tap (ring at the target, walks in) next to hold-to-aim? It is the one tap/hold pair on a button, which Red Dead 2 players complain about. The alternative is to drop it in third person, since E and left click do the same; I would only do that if it misfires in your play.

**Next owner:** Latch (unity-engineer): input map, prompt, reticle, aim state, fire gating. Keel: the run flag in the hit rate. Motive: the 0.6 starting value. Fable: the key-strip words (`options`). Suture: the done-check.

### Firing (Sky, 2026-10-21)

**Sky:** "I think if visually, they keep shooting and it's clear then maybe ok, though I always imagined more a GTA firefight, so you have to keep shooting yourself, complete control, that's how I was playing our game like, but that's also because it's not clear I was firing at the agent (or that it was 'on')." Today drawing sets the agent to fire at will, and a fire order runs until the target is down, X, or the fight ends, with nothing on screen to say so. Mockup tabs 4 (Firing) and 5 (Stopped), `briefs/024/action-controls/`.

**1 · Firing is visible, whatever the control model (must).** Drawn as light, outside the post grade, with a shape cue (not a colour) for who is firing:
- **Tracer:** from the muzzle to the target while a fire order runs, a bright dashed line moving toward the target (own side: cream core, turquoise edge). A driven agent's tracer is thick; an agent returning fire has a thin one. Muzzle flash and a hit spark per shot. Hostile fire reads by shape: a solid long streak, not dashes.
- **Firing mark on the agent** (one of its two icons: the gun with rays, over the head) **and on its portrait** (the sidearm mark becomes the filled gun with rays and a pulsing ring). An agent holding to return fire wears the gun with a back arrow (dashed ring on the portrait).
- **One line** on the marker, at the reticle or target: `Firing at the guard`, then `down in n s`, then the controls (`held`, and the `X stop` cap). Plain: it names who and how long, no sentence beyond that.
- **A clear stop:** on release, X, the target down or the fight ending, the tracer cuts that frame, the marks go, and the line reads `Stopped` for 1 s (with "gun still drawn" until F or the auto-holster). Never a silent end.

**2 · Control model, to try in play (recommendation).**
- **Driven agent in third person: GTA-style.** It fires only while LMB is held (aiming); a tap fires one shot. No fire order is left behind. Taking control of an agent drops its standing fire order; leaving control returns it to "return fire".
- **Commander view: I would not make hold-to-fire the default.** The commander's hand must stay free for the other three, and right button already carries tap = ring, hold = sweep and the going-loud hold, so a held-to-fire right click adds a third meaning. Keep the click giving the lasting order (now visible with a tracer, mark and stop) and add `controls.commanderHoldFire` (off) so Sky can try Syndicate-style hold-to-fire in one play. If Sky prefers hold as the default, it works on a hostile (no ring there), so it does not collide with the ring.
- **Agents not controlled: return fire only when shot at** (the GDD's stance `return`), with the return mark and thin tracer showing it. Panic still sets at will.

**For Keel:** a new logged input, `trigger` (op, target or aim point, on/off, tick), beside the drive samples, so a replay is exact. While on, the op shoots on its weapon's cadence at that target; a tap is on-then-off and fires one shot if the cadence allows (no queue). The per-person hit accumulator, Trail per person hit and the "first shot on a new target 1.0 s" are unchanged. Taking control cancels a fire order; the default stance of a draw must be `return` (the GDD says so; confirm the engine does not set at will). GDD §06 says running cancels fire; Sky's rule is that a runner keeps firing at reduced accuracy: Keel to reconcile (Sky's rule wins).

**For Motive:** pacing does not change if the cadence stays (pistol 1.5 s, carbine 2.0 s): a held button and a lasting order fire the same shots, and "down in n s" becomes the fastest case, not a promise. The risk is feel: a GTA firefight is faster than a shot every 1.5 s, so Sky may find hold-to-fire sluggish; if so, shorten the cadence for everyone (the clock's truth), never only for the driven agent, and retest fight sizes. The same applies to the walk-then-fire modifier (x 0.5) and the run penalty (x 0.6 here).

**Done-check additions:** a recorded hold, tap and release session replays hash-identical; no fire order survives taking control; the tracer, mark, line and Stopped appear within one frame of the engine's own start and stop (read from the engine's fire state, never inferred); a returning agent shows its mark only while it is shooting or has been shot at.

**Question for Sky:** hold-to-fire as the third-person default (recommended), and lasting orders kept in the commander view with the `commanderHoldFire` setting off?

## Sky on firing, 2026-10-21 (after the action-controls mockup)
Sky: "well it needs a clear animation when the player fires, which we don't have right now - so need some placeholder, that might be enough. And so if auto / locked fire is on, then all agents near would fire same target if not engaged, otherwise they fire same time you, again if not engaged. For instance, I could have one agent lock on a officer, and another on another, whilst I control my own player realtime."

Aster's reading (Sky to correct in play):
- **Fire animation:** a placeholder aim-and-shoot clip (raise, recoil per shot, lower) on every cell figure, with a muzzle flash, so firing reads without the HUD. Owner: Forma (clip), Latch (wiring, flash).
- **Lock:** a lasting fire order on a target ("locked"). A locked agent is engaged, keeps its own target and ignores your trigger. Several agents can each lock a different target while you drive one in real time.
- **Agents near you, not engaged, follow your fire:**
  - **Follow setting on (locked follow):** when you open fire, they lock your target and keep firing on it.
  - **Follow setting off:** they fire at your target only while you are firing, and stop when you stop.
- An agent being shot at returns fire on its own shooter, which counts as engaged.
- Open for Motive: what "near" means, whether a joining agent pays the aim-new wait, and what four guns on one target does to the fight table.
