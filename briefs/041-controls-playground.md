# 041 · Controls playground (web)

**State:** open, 2026-10-21. Owner: Glyph (interface-artist). Next owner: Sky plays it; the picked preset goes to Latch for Unity.

## Why
Sky, 2026-10-21, after playing 0.2.34 and seeing the calm-ring mockup:

> "the hover ring I think needs more work. It can be color coded, more representative of the action. I don't quite understand why I see the walk icon and the loop action and the more action. All I wanted to see is what is the main action that would happen if I double click. I can't help but feel when over a actionable item, LMB triggers the default. The reason is, if it's a character, you can't walk onto their space anyways, if it's a prop, the action you want to do would make you walk there anyways if out of range. Or - it's double click to make that default action. It feels more definite. Right click then brings up the Ring - holding down feels unintuitive. How can we make this something we iterate fast over? We could do in web rather than Unity - just setup a very basic playtest scene - different presets / tabs I can then go through and play. Use a capsule / three.js."

## Sky's direction (to build as the lead preset)
- **Hover shows only the main action**: one marker, colour-coded by kind of act and with an icon that shows the act. No walk icon, no "…" or second icon on the ground.
- **Left click on something actionable does its default**, walking there first if out of reach. Left click on open ground walks.
- **Right click (a tap) opens the ring.** No hold-to-open anywhere.
- Alternative to compare: double-click does the default, single click selects or walks.

## The playground
- A web page in `testbed/controls/`, three.js (vendored), capsules and boxes, served by one command (`bun run controls`). No Unity, no sim; acts are fake timers with the Handover's real prices.
- A small street: the cell of four, the door guard, Inez, the driver and car, a camera, a cabinet, a door, a few walkers, a couple of police and rivals for fire.
- Both views: the commander view (pitch 38°, Q/E quarter turns) and third person.
- **Presets as tabs**, switchable live, each a whole scheme:
  1. **Sky's lead:** left click = default, right tap = ring, colour-coded single marker.
  2. **Double click:** double click = default, single = select or walk, right tap = ring.
  3. **0.2.34 as built**, for comparison.
  - Marker variants (colour/icon styles) and camera variants (0.2.34 snap vs the leash camera from brief 040) as their own toggles.
- A small on-screen log of what each click did, so misclicks are visible.
- Plain copy, no key legend; hints only in context.

## Done-check
- `bun run controls` opens the page; every preset playable with mouse and keyboard; switching presets needs no reload.
- Each act shows its verb on hover in one line; left/double click and the ring behave per preset.
- Runs at 60 fps on the Air in a browser.
- Iteration is cheap: a new preset is one small config object.

## Sky's pick, 2026-10-21
"ok, so, let's try verb pill, leash camera, double click." To Unity (Latch): the **Double click** preset, the **Verb pill** marker and the **Leash** camera, ported exactly as the playground plays them (`testbed/controls/data.js` and the marker and camera code beside it). Time display stays "Total" until Sky picks otherwise. Provisional until Sky has played them in the game.

## Sky, later 2026-10-21
- Added in the playground: squad selection (5, Ctrl/Cmd+A, Squad button, drag box; Shift/Ctrl toggle), group walk in formation, group acts (Best placed / Everyone), portrait drag, running (Shift in third person; double click on ground or Shift+click on the map), breakaway shown by the doer's face in the pill and a ground cue (Sky: no lifting, "that's daft").
- "avatar would be anywhere where the name appears in the pill": the doer's face replaces the agent's name in every pill.
- "experience is getting better now, starting to feel I have control of the squad and my actions."
- Running (Sky): single click walks, double click runs, for things and ground alike (new default tab "Click acts, double click runs"). In squad mode a non-default act runs with Shift ("I think that's ok"), and the per-agent action plate beside each portrait keeps its walk/run (feet/running) toggle, as in the game ("like it does currently"); no toggle inside the ring.
- The plate's walk/run toggle is "only for that action": it changes the current order's gait; the next order starts from walk unless given as a run.
- Firing (Sky): "what about hold down LMB to fire? because I have to admit, it's pretty easy to fire on someone right now." Playground: hold LMB to fire (charge ~0.4 s, keeps firing while held, release cancels); toggle for Kill and Knock out holding too; non-hostiles need Ctrl as well.
- Sky: Kill and Knock out need no hold ("accessed by Right click"), so they must never be a left-click default; bystanders need only the same LMB hold, no Ctrl ("it's enough"). Port to the game, range included, when the playground set moves to Unity.
- **Hold to fire adopted** (Sky, after playing it): "hold and fire feels good actually. Think that as a constant means the action element is easily ready and we can tune as we go now e.g. ammo / different weapons / outcomes etc." Hold LMB to fire is the fixed input; ammo, weapon types and outcomes are later tuning (Motive, Keel), not input changes.
- Sky, after hold-to-fire: "starting to feel holding down LMB for the quick action is always the case, and left click is walk there still." New default preset "Click walks, hold acts": click walks (double click runs), hold LMB does the main action (charge ~0.4 s), Shift+hold runs then acts, right tap opens the ring.
- Sky: "but then how do we decide to fire? Maybe hold down right click?" In "Click walks, hold acts": left mouse never harms (click walks, double click runs, hold does the main non-harmful act); right tap opens the ring, right hold fires (charge, keeps firing while held). Third person toggle: right hold fires vs right aims + left fires.

## To the game (Sky, 2026-10-21)
"this is all playing pretty well - lets move it into the game - make sure fight lab is updated." Port the **Click walks, hold acts** preset and everything around it to Unity (Latch), the fight range included. Building fade: Sky is "not into our main fade - was gonna come back to that": keep the game's fade as it is for now and don't port the playground's; a fade pass comes later.
