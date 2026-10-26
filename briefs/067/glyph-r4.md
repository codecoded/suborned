# Glyph · batch 0.2.123 host round 4 review (2026-10-26)

Inputs: briefs/067/latch/r4/ (five frames at 1920x1080, manifests, parts, hud-budget.txt, proof123.json), cropped at 2x; HudToolkit.Less.cs (car ring, lane, tethers), HudToolkit.TargetRings.cs, HostFailCam.cs. Sky: "Simplify the visuals, it's way too crowded. Just having the agent facing who they're shooting should be enough to not need those lines everywhere."

Verdict: Latch's cut meets Sky's line for aim and fire. Nothing that says "who shoots whom" is a line any more, and the looks frames (310, 320) and the follow view are clean. What is left in the two fight frames is the car's hold furniture, and it is what crowds them. Two real faults to build in the pre-flight (F1, F2); the rest is rulings.

## 1 · Density, frame by frame

| Frame | Verdict |
|---|---|
| `r123-looks-310`, `-320` | Meets the line. Rivals read by the violet foot ring and their own look; no line on the bodies. Nothing to cut. |
| `r123-fight-ground` | Meets it. One cue set only: roster, goal, one bubble. But see **G1**. |
| `r123-fight-safe` (4:15) | Bodies are clean (facing + flash + red foot ring on the one aimed at). The crowd is the **hold ring (one huge ring across the Office roof) and the two lane sides** running 25 m over the roof. Fault F1 below. |
| `r123-fight-loud` (3:46) | Aim lines are gone; the pile at the car is still dense, and it has two real overlap faults (F2). |

Drawn over bodies that the body could carry:

- **Red foot ring + four ticks (049): KEEP.** It is the target ring, a ground decal at the feet of whoever is fired or aimed at (`TargetRings`: threat red when `Fire` or `Aim`, cream ticks for ours). It is the body-borne replacement for the lines: facing says who the shooter is, the ring says who is the target, the 0.3 s pulse says a shot landed. It is depth-tested, it fills no space, and it is 2 to 3 per frame at most. Cutting it would leave only facing, and facing alone cannot tell a hostile who turned from a hostile who is aiming. It fits "too crowded"; no cut to propose to Sky.
- **Under-fire arc: KEEP** (70 degrees at the member's feet, only while shot at; none in these frames). It is the only cue for "from where". See G1.
- **Overhead pips above every figure and the gun discs (loud, three at once): leave** (roster/weapon status is a different brief); but the gun disc must not sit on a speech bubble (F2).
- **Dotted white tethers, holder discs and holder pips at the car: cut the tether inside the ring** (below, item 2).
- The attention callout (72 px portrait) covers two bodies in the loud pile. It is the one thing that should cover them; keep.

**F2 (loud, 2x crop, real faults).**
1. The speech bubble "One's heading for the road." has the gun disc over its first word, and the car-held plate (the "no-entry" chip at x 345 in the crop) is half hidden behind the "5 s" fire pill, with the "2" count badge pressed against it. A fight pill is a decision; a holder plate is background. Fix: speech bubbles draw above body discs; while a fire pill is within 60 px of a holder plate, the holder plate hides (its mark and count stay).
2. The `Pick when to fire` pill sits on top of the bubble, 3 px apart (see item 4).

## 2 · The lines Latch kept

- **Dotted tethers (holder to the car): cut the ones inside the ring; keep one only for a holder outside the ring.** Inside the ring, the ring already says "held here", the holder wears the no-entry disc, and the car's lock carries the count (the "2"). A cream dotted line from each one is the same information a fourth time, and in the loud frame four dotted lines cross the pile. A holder out on the lane is the case that needs a link; keep it there, 1 px thinner, at 60 %.
- **Exit lane and the 8 m hold ring over the Office roof at yaw 30: not acceptable as drawn (F1).** They are screen paints from ground points (`paints.Add`, no depth), so the part of the ring and lane behind the roof is drawn on top of it: a street marking hangs over the rooftop and reads as a line on the roof. It is the single biggest line in the safe frame. Ruling, in order:
  1. Preferred: move both to the ground decal batch as the target rings already are (`GroundOf` + `RingQuadsN` with dashes, `SegQuad` for the lane sides), depth-tested. The roof then hides them, and the occlusion cut (which discards the roof pixels round the car) is exactly where they show. Held = dashed (a static dash, the 0.6 s drift can go), free = solid at 55 %.
  2. Fallback if the decals cannot be done in the pre-flight: draw them only while the car is held or its Go card is up (not in the free, quiet state; the safe frame loses both), and fade each lane vertex to 0 beyond 12 m of the car.
  Either way the lane is never drawn past 12 m.
- **Fail camera's single shot line (N125): keep this batch.** One line, in a held replay (not play), only at the deciding moment, with nothing else drawn on the screen; N125 was raised because "enemy fire goes for the Enforcer" had no fire shown. Limit it to the deciding moment (`FailAtMoment`, about 1.2 s), not the whole replay. Re-test after 0.2.123: if the flash at the gun and the spark on the member read at the deciding frame, propose the cut to Sky; I would not take it out blind.

## 3 · Rivals walking to a held car (`rivals.for-car`)

One pulse of the existing holder marks is **not enough**: it lasts 1 s, on marks the player is probably not looking at, for a walk that lasts several seconds and ends with Vale taken. The feed card shows once. Needed, small, no new line:

- Each rival walking for the held car carries one icon over the head: the kit `car` icon on a `disc_dark_24` with a violet (#8a4fd0, the rival signal colour, the same as their foot ring) 2 px ring. At most two icons over a person: it replaces nothing and comes off when he arrives (`rivals.back-to-cover` or `rivals.recalled` turns it off).
- The car's lock marker takes the same violet ring while any rival is walking for it.
- No line from the rival to the car; the direction is the walking figure itself.
Latch can build this in the pre-flight (the same disc grammar as the pop icon), or it waits for the next batch. If it waits, the pulse stands and I will not call this batch a fault. A pass at it in the pre-flight is worth it only if F1 and F2 are done first.

## 4 · The loud frame's HUD budget fault

Both are real and in different places; splice should not loosen the tool for either.
- **"2 objective or hint lines": the seal clock (1:55) is not a line, so the fault is in the manifest.** The manifest tags the clock plate as `kind: "line"`; the tool is right to count a `line`. The clock is a numeral on a lock plate, like the top-bar clock, and tags `hud` (or `image` with text empty). Fix in the host's manifest, Latch. No tool change.
- **"1 line of running text": the pop `Pick when to fire` (text.json `stance-unknown.short`) is real running text.** Four words, not speech, not the goal. It breaks Sky's "no sentences in play except speech" and the budget's three-word rule. Fix: the pill shows the three stance icons from the roster's stance strip (hold, return, free) and no words; Alt or the long line carries the sentence. If a word is needed, Fable's `short` is two words at most. It is also the pill that overlaps the speech bubble (F2).
- "the objective or hint line runs over more than one line": that follows from the two above (the clock and the goal read as stacked); it should go with them. Re-run `bun run budget` after both.

## 5 · Gate the r123 frames?

Yes, in two parts.
- **Gate the numbers now** (`proof123.json`; they are the density rule in numbers): `fight.*.badFrames == 0` (no shooter over 25 degrees), `cones == 0` in the loud frame, `distinctLooks >= 3` in the looks and `>= 4` in the fights, `flashes > 0` for safe and loud, `reticles` empty unless a lock is the subject.
- **Gate the pictures with the budget** for `looks-310`, `looks-320`, `fight-safe`, `fight-ground` now (all pass). `fight-loud` joins the set when F2 and the two budget fixes are in; until then it is listed but not gated, so the batch does not go red on a fault already ruled. Add a fixed check to the budget tool in future: no long diagonal stroke on the HUD layer in a fight frame (it already ignores them as lettering, `budget.ts:135`; Splice can count them, so a returned aim line goes red).
- **G1 (ground view): the follow view's proof reports `flashes 0` over 71 frames while three shooters fire.** With the enemy streaks removed, the flash at the gun and the under-fire arc are the only cues that someone is shooting at you from the ground view. Latch confirms the flash draws in the follow view for hostile shooters; if it does not, that is a fault and not a density win.

## Hand-offs

- **Latch (pre-flight, only these):** F1 (ring and lane as depth-tested ground decals, else the fallback), F2 (bubble above discs; holder plate hides under a fire pill), tether inside the ring cut, clock tag `hud`, `Pick when to fire` as three stance icons, G1 check, fail shot line limited to the deciding moment. The rival-for-car cue in item 3 only if time.
- **Splice:** gate the numbers now and four frames; loud after fixes.
- **Fable:** `stance-unknown.short` two words if the pill ever shows one.
- **Aster:** nothing for Sky except the answer to 049 (the red ring and ticks stay; they are what lets the lines go).
