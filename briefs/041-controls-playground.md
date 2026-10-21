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
