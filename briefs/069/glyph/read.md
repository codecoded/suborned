# Brief 069 · the play-zoom read (Glyph, 2026-10-26)

**What I judged:** Forma's Blender renders: `final/crowd.png` (true size, 1080p, zoom 14 at 38°, yaw 30, LOD1, idle frame 4), `crowd@3x.png`, `groups@3x.png`, `close50.png` and `walk.png`. I judged them against the 26 Sep paintings (`briefs/008/concepts/`), the character board (`commuters__spaceage_bold`, `security__spaceage_duty_2`, the cell concepts) and today's 17 crowd models in the same sheet. **Latch's Unity frames weren't there** (`out/scratch/latch/char-pipeline/` doesn't exist yet), so I haven't judged motion, foot slide or facing in a walk. The forward stoop in every sheet comes from Blender's one-frame retarget (proof.html §1 says so), so I've left it out of the verdict. Re-check it on Latch's frames.

At true size the figures stand about 45–50 px, inside the 40–60 px band. My crops at 3x (nearest neighbour) are in `crops@3x.png` in this folder.

## Verdict in one line
**Keep the pipeline, but don't ship this wardrobe.** On guards the read is clearly better than today's crowd, and on rivals it's level. On commuters it's worse against the concept art. Café staff fail. All 50 share one silhouette and are told apart by colour alone. The concept art does it with shape first (collars, helmets, capes, bags, A-line hems) and colour second.

## 1 · Job at a glance
- **Guards: they read.** The amber top is visible from the far side of a crowd. Today's guard and security models (grey-white and khaki) don't read as guards at all, so this is the biggest gain.
- **Rivals: they read.** The violet carries. That's level with today's four rival colourways, which already read violet and have a puffier jacket shape.
- **Commuters: no single read,** which is fine for a crowd. The trouble is that three of them, `cp_commuter_17`, `_18` and `_19`, wear solid yellow head to toe. At 1x they read as guards (crop A). The pinks and magentas come close to the light rivals. So the colour signal that makes guards and rivals work is leaking into the commuter pool.
- **Café staff are the weakest by far** (crop B). Peach and cream tops over white or seafoam legs on pale paving read as bare skin or underwear, not as a uniform. There's no apron, cap or tray shape, nothing that says café. Out of context I'd take them for commuters, or for unfinished figures.

## 2 · Silhouette and facing
- **The base body makes everyone the same shape.** The garments are mostly "paint" (body faces pushed out), so a top or trousers adds colour but no volume. Shoulders, hips and arms are identical across all 50. The only shape changes visible at 45 px are the skirt flare (small), the car coat (barely different from a tunic), the cap dome (2–3 px, white on a light head) and hair volume (afro and bob only).
- Nothing reaches out of the body line. The board's people are built from stand collars, dome and visor helmets, capes, transparent cases, umbrellas and A-line shifts. §15 says "silhouette from shoulders, packs and props". None of these are in the wardrobe yet.
- **Facing:** in the idle sheets you can just read it from the face-versus-hair dot on the head, but only because almost everyone faces the camera. Nothing on the body has a front and a back (no collar, bag strap, front panel or visor), so a figure seen from behind or the side at 45 px will read as front-ish. Check this on Latch's walking frames. I expect facing to be weak.

## 3 · Colour
- **Signals vs clothes: no confusion.** The guard amber and rival violet on the clothes take the sun and shadow (lit, shaded, in the grade). The unlit, flat signal markers with their own shape and motion stay distinct. There's no clash with the cones' red either: the orange-reds on commuters are lit cloth.
- **The commuter pool shares the guard's amber.** I sampled the lit tops in `close50.png`: `cp_guard_01` #ffc040, `cp_guard_05` #ffbb3d, `cp_commuter_18` #ffc34c. The guard and that commuter are the same colour. Under the sun the guard amber also lifts toward sunshine yellow, so `cp_commuter_17` (#ffffaa) sits in the same family. Keep yellow, gold and orange-gold out of the commuter pool. A slightly deeper guard amber (toward #f0a020 once lit) would also widen the gap.
- **Brass: no misuse.** I can't see any brass at all. The guard's "chrome line" is barely visible at 1x.
- **The style palette is mostly missing.** §15 asks for "bright coats on cream, white boots" and one colour block per person, with enamel accents. Most commuters wear one saturated colour head to toe (`_10`, `_13`, `_15`, `_17`, `_18`, `_21`), which reads as a bodysuit or onesie. Cream, ivory and seafoam turn up mainly on café staff, where they hurt the read. Chrome is absent. The white boots are right and should stay.
- **No internal value contrast.** Each figure is one or two flat fills with no trim, belt, collar or boots-to-legs step. Today's textured crowd has 3–4 values per figure, so it looks more like a person at 1x, even though it's noisier.

## 4 · Uniform but individual
- **Guards: one crew, different people. That's a pass.** Body, sex, skin, hair and cap vary under one amber top. One caution: the white over-tabard (`cp_guard_02`, `_04`, `_07`) splits the figure half white and half amber, which weakens the uniform read. Make the tabard ink or chrome-trimmed amber, or drop it.
- **Rivals: one crew, different cuts. That's a pass.** Shades range from lilac to deep purple, with jacket, tunic or coat, so they look like a set, not clones. Like everyone else, they have no rival-specific shape.

## 5 · Against today's crowd and the concept art
- **Against today's crowd:**
  - Guards: better.
  - Rivals: level.
  - Commuters: slightly cleaner at 1x (less noise) but poorer as people (flat bodysuits, one shape).
  - Café: worse.
- **Against the concept art: further away than today's crowd.** The paintings and board are about couture silhouettes on a cream ground: collars, helmets, capes, shifts, chrome accessories, a single bold block. These figures are mannequins in solid colour. Today's crowd at least carries jackets, boots and two-tone outfits that echo the board.
- **What to tell Sky:** the pipeline is the right foundation (one skeleton, fingers, one material, deterministic, cheap). The proof's wardrobe of nine parts isn't enough to replace today's crowd. It would be a step back for commuters and café until the three changes below land.

## 6 · The three changes that would most improve the read (ranked)
1. **Give each role a silhouette part the others never wear, and give everyone couture shape.**
   - Guard: a peaked or dome cap with a visor plus a shoulder yoke or duty belt.
   - Café: an apron and a pillbox or headband, with tray-ready arms.
   - Rivals: their own cut (cape-collar jacket or hood).
   - Commuters: off the board, stand collars, A-line shifts, cape collars, dome helmets, cases and bags.
   - Parts must add volume beyond the body line. Painted garments can't change the outline.
   - This one change fixes job, facing (collars and bags have a front) and the "everyone is one shape" problem.
2. **A colour rule per role:**
   - Commuters wear one saturated block on a cream, ivory or ink base, never head to toe. Exclude the guard hue window (yellow through orange-gold) and the rival hue window (violet through magenta) from the commuter pool.
   - Guards go a touch deeper (#f0a020 once lit), always with ivory or ink below.
   - Café get a defined uniform: seafoam with a white apron and a chrome line, never peach on peach. Peach is too close to skin.
3. **Internal contrast and trim:**
   - Turn on the mask shader the UVs already carry, so every figure gets at least one dark step (belt, boots or collar) and a chrome or ivory trim line.
   - Make the hat and hair read at LOD1, at least 3–4 px of contrasting value on the head, so facing survives at 45 px.
   - Then re-judge on Latch's Unity frames: walk, run and the side and back views.

---

## Unity frames (second read, after Latch's scratch scene)
**What I looked at:**
- in `out/scratch/latch/char-pipeline/frames/`: `new50/walk/new50_walk_010.png` and the side-by-side strips (walk, aim_pistol);
- `groups/{guard,rival}_group_{playzoom,close}.png`;
- `close/hands/cp_guard_02_LOD1_play.png`;
- `footslide.json`;
- one frame at 1 s from `video/new50_walk_playzoom_2x.mp4` and `old17_walk_playzoom_2x.mp4`, sampled with ffmpeg into `vid/`.

My crops in this folder:
- `u_walk_rows1-3@3x.png` and `u_walk_rows4-5@3x.png` (2x nearest neighbour);
- `u_guard_group_playzoom@4x.png` and `u_rival_group_playzoom@4x.png` (4x).

Latch's ground is flat cream, not the paving's blue-grey, so contrast here runs a little kinder or harsher than the street, depending on colour.

### Does facing read in motion?
- **Walking or running: yes, clearly.** The figures are upright in Unity (the Blender stoop is gone). The stride, the arm swing and the boot toes give direction at 45 px. In this the new figures match today's or beat them, because the flat fills keep the limbs crisp where today's textures blur.
- **Standing or aiming: weaker.** In the play-zoom group crops you can tell a back from a front by face skin against hair mass, but only just: the head is 5–6 px. Two figures read wrong:
  - the guard in the white over-tabard shows an all-white back, which reads as a different, non-guard person from behind;
  - the hatless rivals in a three-quarter back view are a violet block with a dark dot.

  Nothing on the torso marks front from back (collar, bag strap, visor). That's what change 1 adds.
- **aim_pistol:** at play zoom the new pose reads as a compact "arms up at the face" shape. It's about as legible as today's, and in the close frames the arms are raised oddly high. The open, splayed fingers Latch found aren't visible at play zoom (LOD1 draws a mitten). They matter for close-ups and third person, not this read.

### Does my verdict from the renders hold?
- **Yes, and slightly more favourable for motion.** In Unity's light the guards and rivals read even more strongly: amber and violet blocks with clean ink or ivory legs. The guard group and the rival group each read as one crew of different people (body, skin, hair and leg colour vary).
- **Commuters:** still one-colour bodysuits. The yellow commuters (`_17`, `_18`, `_19`) still pass for guards when walking (walk frame, row 2, figures 7–9 next to the guards in row 3).
- **Café staff:** still the weakest. Walking in a row on cream they are the palest, lowest-contrast figures in the frame, and they read as skin.
- **Silhouette:** in motion everyone is the same striding mannequin. The only shape variety is the skirt and the coat-tail. In the side view the coat-tail reads as a small tail, a welcome shape.
- **Bottom line:** keep the pipeline and redo the wardrobe. Better than today for guards. Level for rivals. Worse than the concept art for commuters and café staff.

### Anything new
- **Foot slide (Latch's finding):** it's visible at play zoom.
  - New walk: median 6.8 %, worst 23.9 %. Today's walk: about 3 %.
  - At 45 px a 24 % slide shows as skating.
  - The figure's look isn't the cause, but the fix belongs in the pipeline: scale the move speed or the clip speed by `humanScale`, or keep height within the clip's stride. Next owner: Forma and Latch, not me.
- **Painted colour borders are jagged.** The tabard and jacket edges are stair-stepped on the low proxy (`close/hands/cp_guard_02_LOD1_play.png`: the ink-to-amber edge zigzags across the chest). At play zoom this shows as 1–2 px of shimmer on guards and rivals while walking, and on rival belts as broken dark blocks (`rival_group_close.png`, the bottom-right rival).
- **Cap crowns poke through.** In the same close-up the white cap band cuts across the face. At play zoom it's a white flicker on the head, the exact pixels that carry facing.
- **Skirt back panels and coat hems** flare and open in the side view. At play zoom this reads as a small tail, which is harmless and even a useful facing cue. It's a defect only in close-ups.

### Do my three ranked changes still stand?
Yes, unchanged in order, with two additions:
1. **A role silhouette part, plus couture shape for everyone.** Unity makes this more urgent: in motion the only thing telling people apart is colour. Collars, bags and visors also fix facing when standing.
   - Add: **drop the white over-tabard, or make it amber or ink.** From behind it erases the guard read.
2. **A colour rule per role:** keep the guard and rival hue windows out of the commuter pool, and give café a seafoam uniform with a white apron.
3. **Contrast, trim and clean edges:**
   - Use the mask shader for a dark step and a trim line.
   - Add: **colour borders on edge loops, not painted across faces,** so they don't stair-step and shimmer.
   - Add: **cap and hat fit:** no crown through the hair.

**Separate from the look but blocking a ship:** foot slide that scales with height. It goes to Forma and Latch.
