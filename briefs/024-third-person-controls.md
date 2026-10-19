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
| **Recentre** | **Z** swings behind the agent (0.3 s). No auto-recentre while keys are held: with camera-relative steering it makes you walk in a circle on W+D, and PC games do not do it. Two exceptions: the swoop in starts behind the agent facing where the map faced, and while only an engine walk drives the agent, the camera trails after 1 s without look input. |
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
- **Out of reach:** the prompt shows greyed with the metres to go (the engine's `too-far` carries them). **No auto-walk in ground mode: you walk.** The "walk and act as one time" line is a map-mode price; on the ground the walk is yours and free.
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

## Evidence

`briefs/024/` (shots, frame times, the replay hash, the key-table test output, `research/notes.md`, Sky's three answers).

## Next owner

Sky approves or changes this. Then Aster splits it:
- **Keel** first: the pose input, its sanity rules, its replay record, the 1x lock (§1).
- **Latch**: controller, camera, switch, wheel, perspective goal markers.
- **Glyph**: awareness art (eye, pips, edge markers, cone curtain), the strip, the wheel, the chip. **Glint**: the curtain outside the grade.
- **Motive**: speeds and the notice ramp question. **Fable**: the strip words and H19. **Chime**: the notice cue. **Suture**: the done-check. **Redline**: curtain and boom cost.
