# 039 B, cover and pin: the final visual spec (Glyph, 2026-10-26)

Sky picked B (Cover and pin), with three calls: F draws and holsters plus a right-hold focus; Covering fire while the Enforcer holds Vale; Talk down on the car blocker. The rule from the simplified board stands: **the body carries the fight, and the street gets as few marks as possible.** This file is what Latch and Keel build from. It replaces "Simplified" in `proposal-2026-10-26.md` wherever the two differ.

## The read: Forma's pose proof judged at true size
Inputs: `briefs/039/forma/pin-poses.md`, `pin-poses-sheet.png`, `pin-poses-counts.json`, and the four loops. I judged them at 1 px = 1 px of a 1080p frame (zoom 14 at 38°, yaw 30), from the sheet's true-size column and from the loops scaled back down from 3x.
- `b-final-stills-truesize-x3.png`: the 0.85 m hedge column at true size, enlarged 3x (nearest). Rows: kneel, pinned held, peek, fires, a round lands, then pinned in the open held and hit.
- `b-final-motion-truesize.png`: the loops at true size, every 0.25 s over 3.6 s, at the 0.85 m stand-off. Columns: in cover, pinned in cover, fires from cover.

| state | verdict | what carries it |
|---|---|---|
| in cover (kneel) | **pass** | Head, face and gun hands upright over the prop, 27–32 px visible at the stand-off. Against standing it is only 6–8 px lower at this heading, but the prop in front of him says "in cover", so height doesn't have to. |
| firing from cover | **pass** (in motion; close as a still) | A half-rise of 6–8 px that shows the chest and arms over the prop, a kick on each shot, and the muzzle flash on the same frame. As a still, the small pale figures (Scout and Fixer) are close to their kneel. The rise and the flash carry it, so the flash is required, not optional. |
| pinned in cover | **pass** | A different outline in the same place: a wide hump of jacket and elbows, with no face. Each round drops him (0.25 s) and flinches him, and the peek shows as the pin runs out. In the motion strip, the pinned frames separate from the kneel frames at a glance on all six figures. Even snug at 0.45 m he shows at least 174 px², so he never vanishes. |
| pinned in the open | **pass** | The build's `cower` is 13–18 px lower than standing, hunched, head down and held, and it stands back up at the pin's end. In B nobody kneels in the open (the kneel happens only at a cover point), so in the open the cower never has to be told from a kneel. |

**The weak pair (kneel against pinned behind cover, held still): pass, by outline.** Upright and narrow with a face shows against wide and low with a back and no face. In practice the pair is seldom seen as a still: a pin starts with the drop and renews with each flinch, so a held hump lasts at most 2 s after the last round, and its last 0.6 s is the peek. Read strength by figure, strongest first:
- rival (violet hump), enforcer (cream coat) and tech (red elbows on dark): strong;
- guard: good;
- scout and fixer (small, pale, patterned): weakest, but still a different outline.

The range must check three things the proof doesn't:
1. **Other headings.** The proof puts the shooter on the camera's side of the hedge (heading 195). With Q/E every quarter yaw happens, and a side-on pinned man behind a long hedge is the untested case. With the shooter on the far side, the prop is behind the man and he shows whole, which is the easy case.
2. **The real planters**, not the box stand-in. Shrub bumps should show more of him, not less.
3. **Unity's pixels**, with Glint's flash and chips and Chime's "Down" bark on the flinch frame.

## The chevron disc: none, for everyone
- **Rivals and police:** the pin is the player's own doing, and he watches the drop and flinch he caused. A disc would add up to a mark per hostile, which is the crowding Sky asked to remove.
- **The cell:** the roster already says it. The live portrait's "under fire" state is "fired on in the last 2 s" (§10). That is the pin's own window (§06: "shot at in the last 2 s"), so a pinned agent's portrait is already showing the under-fire still. It sits in the place the player looks to command his own people, and it adds nothing to the street. If Motive rules that the pin doesn't apply to the cell, the question goes away.
- **Fallback, only if the range fails the held hump for the cell:** the kit's dark disc with the down chevron (`disc_dark_24`, cream `chev_down`, no draining ring) beside the cell's own pinned agent, and nowhere else. Rivals never get it. The range decides this, not a guess now.

## What stays on the street in B
At rest, outside a fight: nothing new.

In a fight, at most this:

| mark | when | look |
|---|---|---|
| selected agent's ring | as built | the built 0.1 m band, unchanged |
| focus ring | a right-hold focus stands | one red line under the focus, no ticks |
| lock disc | the same | the built 067 D lock disc over the focus; no cameos under it |
| focus health pips | the same | pips over the focus only; nobody else wears pips in a fight |
| muzzle flash | each shot | Glint; on the kick frame |
| hit spark | a round hits a body | Glint; with the flinch or stagger |
| stone chips / dust | a round hits cover, or misses on paving | Glint; leaf rustle on a hedge (Chime) |

That makes 7 kinds and, in the board's moment, 8 marks, against 39 today.

**Cut, and not coming back in B:**
- the tracer per shot and the rivals' streaks;
- the under-fire arcs and the cover arcs;
- the rings under the unselected cell and under the rivals;
- the firing discs over shooters;
- the carry disc (Vale shows on the Enforcer's shoulder);
- the pin discs;
- the stance tags;
- the miss ring (a dust tick instead);
- pips over everyone but the focus.

**The three calls, on the body and the roster, never on the street:**
- **F draw and holster:** the body draws or holsters (the draw 1.0 s, holster 0.5 s clips), and the portrait's sidearm mark goes hollow or filled, as built. The right hold sets the focus marks above, and the cursor is the crosshair with its shield badge (counts, half, struck), as built in §10.
- **Covering fire (Enforcer, also while holding Vale):** before the order, the 3 m pinned area shows as a ground ring on the move's ring or hold preview only, like any order's price. While it runs (4 s), there is no area mark: the Enforcer fires one-handed (Forma's clip, a flash per shot), and everyone inside ducks (the pin poses). The roster's move disc shows it running, then cooling.
- **Talk down on the car blocker:** ordered from the blocker's ring (the move first and lit), as built. When it lands, he breaks on the body (Forma's break: hands down, walks off the lane), and the getaway's holder mark leaves him, as built. No new mark.

## When cover marks show
Never at rest, never over a rival's cover, and never just because a fight is on. They show in exactly two moments:
1. **While a move is being ordered for a drawn agent in a fight** (the cursor on the ground, before release). The move ring snaps to the kneel point when the end lands within 1.5 m of a cover point within 10 m of him (§06), and a snapped ring carries the kit's shield glyph inside it. If that end flanks the focus's cover, his cover wears one struck shield disc (`disc_dark_24`, cream shield, struck). That is at most 2 marks, and both go on release or when the cursor leaves the ground.
2. **On hover over a hostile:** the crosshair's shield badge (counts, half or struck), as built. It sits on the cursor, not the street.

Mark (the Scout's move) uses the same struck shield disc over the marked man's cover for its 10 s, and nothing else.

## For Forma (authoring, section C of `pin-poses.md`)
Pass: author `cover_pin`, `cover_fire`, `flinch_add`, `kick_add` and `pin_peek_add` as listed. One ask: in `cover_fire`, keep both forearms straight out over the prop line (the two-handed `aim_pistol` reach) so the rise reads on the small pale figures. The kneel point stays on the far side of the prop from the shooter, so the pinned hump always shows the back.

## For Keel and Latch
- **Keel** (engine rules for B) supplies three things: the pin state with its remaining time per person (to drive the peek weight), the kneel point per cover point facing the shooter, and the move-snap test (end within 1.5 m of a cover point within 10 m of the agent) so the host can show the snapped ring.
- **Latch** hosts the additive layer, installs A to C after his current build, and draws only the street marks in the table above. He removes the marks in the cut list from the fight path and colours nothing new (§15 colour rule: the red focus ring and the kit discs are already sourced).

## Open
- Aster: the GDD changes the simplified board listed (§10 loses the tracer per shot, the miss ring and pips over everyone; Motive's cover rule 3 becomes "cover marks while the selected agent moves near cover"), plus "no pin mark: the under-fire portrait is the cell's pin".
- Motive: whether the pin applies to the cell.
- Latch: does a ground ring draw over cover? If the prop hides the focus ring, the lock disc alone marks him, which is enough.
