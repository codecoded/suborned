# 016 · Camera rotation

**Status (2026-10-17):** opened. Owner Latch. Glyph reviews the shots.

Sky, 2026-10-17: "yeah, camera rotation". Earlier, on 2026-10-16: "playing the game, I think camera rotation is important". This replaces the 2026-10-15 "everything but rotation" choice and the fixed limit "yaw 30" (CLAUDE.md, GDD §01). It goes into GDD v0.24. The camera stays above the street, never first person. Third person is still parked.

It is view only, so the engine, rules and replays are untouched.

## Proposed (Aster), to try in play; Sky may change it after playing
- **Q / E** rotate the view 90° about the screen centre's ground point, eased over about 0.35 s. Yaw takes 30, 120, 210 or 300. The opening view is still yaw 30. Snapping keeps every view one of four known angles, so the building fade, sightlines and checks stay testable.
- **Alt + middle-drag** rotates freely, and the view snaps to the nearest of the four on release. Latch checks this doesn't clash with the controls table and says if it does.
- **The minimap turns with the view** (heading-up), so up on the map is up on screen. A small north tick sits on the scope's rim.
- **Everything screen-anchored follows the view:** edge arrows, pins, pips, the move ring, chips and the cones (world-space, so already right).
- **The building fade** (`ArchitectureFade`) works from every angle: whatever stands between the camera and the selected agents, the target or the cursor fades.
- **Hint:** the hint system teaches Q/E once, early.

## Done-check
- Rotate through all four angles during route G. At each one:
  - the cell, Inez, the guard and Vale stay findable;
  - nothing important hides behind a building without fading;
  - the HUD budget and layout audit pass;
  - 60 fps holds at play zoom (check on the Studio; Sky confirms on the Air).
- Scenarios match and replays are identical (view only).
- Shots of the same moment at all four angles go to `briefs/016/`.
- Glyph reviews.
- Sky plays it.

## Glyph review (2026-10-18)

Reviewed at 5x: `a-*`, `act-*` and `b-*` at yaw 30, 120, 210 and 300, `h13-hint`, and the scope cropped from each angle (`briefs/016/glyph-review/scope-four-yaws-3x.png`). I did not play it, so the rotation motion and the easing are not reviewed.

**Verdict: pass, with four small fixes for Latch (1, 2, 3, 5) and two recommendations (4, 6).**

### 1 · Scope: heading-up works, the north wedge is too weak. Fix.
- Heading-up is right. The plot turns with the view at all four angles, the grid stays screen-aligned, and the cell numbers and the target frame turn with it.
- The north wedge is an 8 px cream triangle that floats about 12 px inside the frame.
  - At 5x it reads as a stray pointer.
  - At yaw 120 it sits in the rounded bottom-left corner and half touches the chrome.
  - Nothing tells a player it means north.
- Fix:
  - Seat it on the frame's inner rim: a cream wedge 11 px wide, base on the rim line, apex pointing out of the plot toward north, with a 2 px tick on the rim under it.
  - Slide it along the straight edges, never into the rounded corners (clamp 10 px from each corner).
  - No letter. The tick on the chrome is what says "this is the compass".
  - Keep the cream colour.

### 2 · Fade quality: good at the angles that matter. One fix.
- Records Office tower, yaw 30 and 300: the facade fades under the agents, the target and the cursor discs, and the door, the guard pip, Inez and the guard stay clear in both. The fade reads as a soft hole, not a cut. At yaw 120 and 210 the tower is behind or beside the action and nothing needs to fade.
- Defect: at yaw 300 (and a little at 30) the small cypress trees and lamp props inside the fade discs turn into ghost columns of dotted vertical bands. They no longer read as trees. Do not fade props shorter than 6 m, or draw them whole inside the disc and fade only the building meshes.

### 3 · H13 glyph: yes. Drawn.
- The line is words only ("Turn the view: Q / E."), and the other hint lines lead with a glyph or a portrait.
- New glyph: `briefs/014/glyph/glyphs/turn_view@1x.png` and `@2x.png`.
  - An open orbit ring with one arrowhead, round a small ground diamond.
  - White monoline in the 32 box, like the rest.
  - It reads as "turn", not "refresh" and not an eye.
  - Script: `briefs/014/glyph/scripts/turn_view.py`.
- Latch: re-run "Import Glyph's Kit" (it already reads that folder), then set the line to `[turn_view] Turn the view: [Q] [E]`.
  - Show Q and E as two keycaps in the kit's `plate_light` (the same ones the order plate uses), in place of the text "Q / E".
  - The hint stays one short imperative with the glyph at 18 px, the keycaps 22 px wide, a 40 px line as built.

### 4 · Edge arrows for the cell and key people: yes, quiet, and only when they matter
- Why: after a turn or a pan you can lose the cell completely, and the roster only tells you who, not where. Clicking a portrait or F1 to F4 recentres, so the arrows should be an aid, not a second map.
- Design, edge cameos (not arrows alone):
  - Each is a 24 px round cameo of the agent (the cameo sprites already exist) with its F number, on the screen edge, with a small cream wedge pointing at the agent. The cameo's rim is turquoise for the selected agent and cream for the others.
  - They are screen-anchored by projection, so they follow the view at every yaw.
  - They sit in the safe margin and stay clear of the roster, the pill, the scope and the plate; if a place is taken the cameo slides along the edge.
- When to show one (otherwise the edge fills with four faces all the time):
  - the selected agent, always, while off-screen;
  - any other agent that is busy (a timer is running), hurt, down or seen;
  - idle off-screen agents: none (their roster portrait already says ready).
- Known key people (Vale, Inez, the guard): their pin (the existing eye or role pip) turns into an edge pin only when the next plan mark depends on them within about 10 s, or they are the goal. Vale keeps his existing edge arrow.
- At most 4 on screen at once; click one to recentre on it.
- No numbers, no words. Latch can build the cameo edge pins from the same code as the arriving-unit arrows.

### 5 · Alt + middle-drag: keep Alt, delay the detail layer by 0.25 s. Do not use Ctrl.
- The flash is the detail layer appearing the instant Alt goes down, and it would do the same on any Alt chord. Showing the detail layer only after Alt has been held for about 0.25 s fixes it for every chord and stops a quick Alt tap from flickering the HUD. This also suits "hold Alt for the numbers" in the move preview.
- Ctrl is worse: on a Mac, Ctrl with a click is a right click.
- Note: a trackpad has no middle button. Q and E cover it; if Sky works on the Air's trackpad, a two-finger rotate or Ctrl + left-drag could join later (not asked for here).

### 6 · Other things seen
- The armed "Knock out" chip, the cones and the price chip stay upright and correct at all four angles (`act-*`).
- The HUD layout is unchanged by the turn.

### Show Sky (4 to 6)
1. `briefs/016/glyph-review/four-angles-a.png`: the same moment at the four angles.
2. `briefs/016/glyph-review/scope-four-yaws-3x.png`: the scope turning with the view (after fix 1).
3. `briefs/016/a-300.png`: the Records Office tower faded at yaw 300, door and agents readable (after fix 2).
4. `briefs/016/act-120.png`: an armed order with the cones turned.
5. `briefs/016/h13-hint.png`: the hint (after fix 3, with the glyph and keycaps).

## Glyph review, round 2 (2026-10-18)

Reviewed at 5x to 8x: the rotation fixes, `vale/01 to 04`, `vale/route-b-00 to 06`, `pick/01 to 04`, `plate/01 to 03` and `edge-cameos-120`. I did not play it, so the Vale logic, the picking behaviour and the folding motion are not reviewed, only how they look.

**Verdict: the rotation fixes pass. The Vale steps, picking and plate pass with eight defects for Latch (A to H below), all small.**

### Rotation fixes: pass
- **Scope wedge** (`glyph-review/scope-four-yaws-3x.png`): the cream wedge now sits on the frame's inner rim and slides along the straight edges, clear of the corners, at all four yaws. It reads as a compass mark.
- **Props in the fade** (`a-300.png`): the cypresses and lamps inside the fade discs are drawn whole and clean. The ghost columns are gone.
- **H13** (`h13-hint.png`): glyph, "Turn the view:", and two Q and E keycaps. It reads in one look and the padding is even.
- **Edge cameos** (`edge-cameos-120.png`): 24 px round cameos with a wedge pointing at the agent, in turquoise for the selected agent and cream for the others. They sit clear of the roster and the scope. One note: the "F3" badge text is about 7 px, below the kit's 10.5 px floor for key labels. Make it 10 px and move it half a cameo outward.

### Defects for Latch
- **A. Vale's pin has three icons (`vale/route-b-00-take-too-early.png`).** The star disc, the shut eye and the "?" eye are all over him, and the eye pushes over the name tag, which reads "Valc". The rule is two icons at most over a person.
  - Fix: keep the star and one eye. The eye's state goes on the star's rim as a dashed or solid ring, or on the name tag.
  - Also put the name tag above the pips, not under them.
- **B. A pin covers a speech bubble (`vale/route-b-04-panel-take.png`).** The star disc sits on "Drive on. I'm expected.", which reads "I'v expected". Draw bubbles above world pins, or lift the pin 20 px while a bubble is up.
- **C. The goal line shows a plain ring for the car (`vale/03-out-with-vale-car-marker.png`).** "Get ▼ Vale to the ○ car." reads as the letter O. Use the `car` glyph in turquoise, the same as the knob and the ground marker.
- **D. The dashed ring on the first knob crosses its key number (`vale/01`, `vale/03`).** The ring is bigger than the knob and cuts the "1". Make it tight (bezel plus 3 px) with a 2 px gap to the numeral, and keep it off the plate's end screw.
- **E. The hover rim on a target is a double ring (`pick/01-hover-entrance-camera.png`).** Two ellipses sit offset from each other and read as an eye shape. Draw one ellipse. The one-word chip itself is fine.
- **F. The "Loop camera" chip is hidden behind a busy timer (`pick/02-target-tech-loop.png`).** F4's "28 s" timer pill covers the bottom of the chip and clips its text. Draw the chip above the timers, or move it 22 px up.
- **G. Edge pins touch the goal line (`pick/02-target-tech-loop.png`).** The star pin and a camera pin at the bottom edge crowd the "Take ▼ Vale alive." line. Keep edge pins 12 px clear of the goal line's rectangle and slide them along the edge.
- **H. The action panel opens over the target's feet (`vale/route-b-00`, `route-b-04`).** The panel's corner covers Vale's selection rings. Open it 16 px right and 10 px down, or put it on the side away from the target.

### Needs a note, not a fix
- `vale/02-inside-timer.png`: the small "→| 4 · 6 s" pill under the status pill is clear for the 6 s but the "4" has no icon saying what it counts. If it is people inside, use the people glyph; if it is something else, name it in a hover line.
- `vale/01`: the sleeping-"Z" disc and the door marker are two icons over the guard, which is within the limit. The dashed ring around both is wide and touches the Z. Shrink it to the door marker only.

### Ruling: the door marker shows only what the cell knows
Agreed. A marker that goes green before the cell has learned the watcher breaks told versus learned, and the player would be told something they have not earned.

Spec for Latch, two lines:
1. The door marker and H14 compute "open" from the cell's knowledge, not the engine's live check. They show "go" (turquoise dashed ring and the door glyph) only when every watcher that can see the door is learned and, by that knowledge, does not see it. If any such watcher is unlearned, the marker shows the unknown state (cream dashed ring with the `eye_unk` glyph) and the hover line is "Unknown. Watch first." If a learned watcher sees the door, it shows the watcher's eye and no "go".
2. The engine's check still decides what happens when the order runs, so acting on "?" can fail. A fact the cell was only told counts as unlearned for the "go" state; Aster or Sky can relax that.

H14 follows the same rule: until the door is learned-open, the hint says to Watch first, not that the way is clear.

### Show Sky (5)
1. `briefs/016/glyph-review/four-angles-a.png`: the same moment at all four angles.
2. `briefs/016/glyph-review/scope-four-yaws-3x.png`: the scope and north wedge turning with the view.
3. `briefs/016/h13-hint.png`: the turn-the-view hint with its glyph and keycaps.
4. `briefs/016/vale/02-inside-timer.png`: the door progress ring and the inside timer (show after D).
5. `briefs/016/plate/03-folded-2-pressed.png`: the folded plate with the current verb and the Listen label.
Hold back `pick/02` and `vale/route-b-04` until F and B are fixed.

## Glyph review, round 3 (2026-10-18)

Reviewed at 4x to 8x: the rotation shots, `vale/01b`, `vale/02`, `plate/03`, `pick/02`, `pick/05` and `vale/route-b-04`. I did not play it.

**Verdict: PASS. Three small items remain for Latch, none blocking Sky's play.**

### Fixed since last round (checked)
- **A, three icons over Vale:** fixed. `route-b-04` shows the star and the name tag only, clean.
- **B, pin over a bubble:** fixed. "Drive on. I'm expected." now reads whole, drawn above the pin.
- **C and D:** the goal line, the knob ring and its numeral were already checked in the last pass and the knob "1" is now clear in `vale/01b` and `vale/02`.
- **E, hover rim:** fixed. One ellipse (`vale/01b`, `route-b-04`).
- **F:** the "Loop camera" chip is above the timer and fully readable. It still overlaps the timer pill's top edge by about 6 px (see item 1).
- **G, edge pins:** the star and camera pins at the bottom edge are 20 px clear of the goal line.
- **H, action panel:** it no longer covers the target's rings.
- **The units pill on `vale/02`:** now "→| ▮▮▮ 4 ⏱ 6 s", so the 4 has a people glyph.
- **Rotation set:** `scope-four-yaws-3x`, `a-300`, `act-120` and `h13-hint` are as approved. The fade shows no ghost props, the wedge sits on the rim, and the H13 line reads cleanly.

### The knowledge-based door marker: works
`vale/01b-door-unknown-hover.png`: the hover on the door when the watcher is unlearned shows the `eye_unk` disc inside a cream dashed ring, with the price chip ("door, 38 s, eye with ?") and the tooltip "Unknown. Watch first.". It is a clear "not yet", not a "go". That matches the ruling. The seen state (the watcher's eye, no ring) was not in the set; I will check it when a shot exists.

### Left for Latch
1. `pick/02-target-tech-loop.png`: the "Loop camera" chip overlaps the top edge of F4's timer pill. Lift the chip 6 px.
2. `vale/01b-door-unknown-hover.png`: "Unknown. Watch first." has about 3 px of padding at the sides and under the text. Give it 8 px sideways and 4 px above and below, like the other one-word chips.
3. Edge cameo F-number badges are still about 7 px (`act-120.png`, the "F3" cameo). Make them 10 px.

Note, not a defect: in `pick/05-listen-heard.png` the Watch and Listen rows are both locked with "?" values after the listen. That is correct for this state, but it makes the panel read as empty. Latch or Fable may want one open row to show what is possible.

### Final set to show Sky (6)
1. `briefs/016/glyph-review/four-angles-a.png`: the same moment at all four angles.
2. `briefs/016/glyph-review/scope-four-yaws-3x.png`: the scope and north wedge turning with the view.
3. `briefs/016/act-120.png`: an armed order at yaw 120, with the cones, the price chip and the edge cameos.
4. `briefs/016/h13-hint.png`: the turn-the-view hint with its glyph and keycaps.
5. `briefs/016/vale/01b-door-unknown-hover.png`: the door that is not yet known (after item 2).
6. `briefs/016/plate/03-folded-2-pressed.png`: the folded plate with the current verb and its label.
