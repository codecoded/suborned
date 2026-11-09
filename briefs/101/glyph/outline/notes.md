# Hover outline: a light line on what you point at (brief 101)

Glyph, 2026-11-09. Open `board.png` first (1000 × 7913, for a phone).

This is a concept only. Nothing changed under `unity/Assets`, in the GDD or in `text.json`. Nothing was committed, and no paid or Codex call was made ($0). Unity, the batch and the game were not run.

The pictures are real frames with the outline drawn in:
- `r085-guard-enforcer` and `r085-guard-hover` from Latch's batch tonight (the Office's public door at play zoom 10, yaw 210). I copied them to `out/scratch/glyph/outline/src/` before the batch moved on.
- Sky's frame from play (2880 × 1864, HUD scale 2).

Each outline is drawn from the thing's real shape (a mask cut from the frame), the same way Unity's edge pass would draw it. None of them is a hand-drawn ring.

## My pick: B, a thin ink line with a cream edge

- **It means nothing else.**
  - Turquoise is the cell's colour: the selection ring and chevron, the cell's rings.
  - Cream is the edge the game draws round a person seen through cover (the occlusion silhouette).
  - Red, amber and violet are the signals.
- **It reads on any ground.** The ink shows on the cream city, and the fine cream edge shows on dark glass and asphalt. It's the street labels' own keyline, so it belongs with the HUD.
- **It's a little.** 1.5 px of ink and 1 px of cream at 1080p, fading in 0.1 s.
- **The door's line never runs round the guard.** Whatever stands in front of a thing hides its line.

There is one look for everything a click acts on. It only says which one; the pill still says what the click does.

## The three looks

| | A · lit edge | **B · ink keyline (pick)** | C · turquoise glow |
|---|---|---|---|
| colour | cream #f7f4ec, and the thing lifted 7 % | ink #17302f, then cream #f7f4ec | turquoise-light #60e0ea |
| width at 1080p | 1.5 px | 1.5 px ink + 1 px cream | 1.5 px + a 5 px glow |
| line or glow | a line inside the edge, on the thing's own pixels | a line outside the edge | outside, with a glow |
| fade | in 0.1 s | in and out 0.1 s | in 0.15 s |
| behind cover | hidden | hidden; goes round a person's silhouette | shows through at 35 % |

Where A and C trip:
- **A:** on the Fixer, whose feet are behind the sphere, the cream merges with her silhouette's cream edge. On light clothes the line fades.
- **C:** the hovered Fixer reads as selected, next to the Scout, who is.

## The four cases (look B)

1. **The guard in the Office door.** The Enforcer is selected (brief 085's case).
   - On the guard's body and 20 px round it, a click opens the guard's ring ("Hold · Watch the guard").
   - 40 px to the right, it opens the door's ring ("Hold · Enter the Office").
   - Today only the pill's words tell them apart. Now the one that would open lights up.
   - The door's line passes behind the guard, the globe and the stone fin.
   - Shown at play zoom 1x and close at 3x. The pill shows in play but isn't in this capture.
2. **One of the cell among the others.** Sky's frame at 1080p size, with the pointer on the Fixer, then on the Tech. Her head overlaps his boots, so his line stops there.
3. **Partly hidden.** The Fixer's feet are behind the sphere, and the game already draws them through it as the silhouette.
   - The line goes round the silhouette too, so she reads as one person.
   - The other choice stops the line at the sphere, which cuts her in two. The board shows both.
4. **Not a person.**
   - The lamp mast's camera: the line goes round its head (the pods and the glass dome), not the post.
   - The official car: on the dark road only the cream edge shows, and that's enough.

## What gets it, and what it replaces

**What gets it:**
- Whatever a left click acts on: a person with a ring, a camera, a car, the door, the cabinet, a stash, a body or a downed agent.
- **The cell too.** A click on one of them selects her, and in Sky's frame that's exactly where you need to see whose click it is.
- Never a passer-by, unless the selection has a way on them.

**How it behaves:**
- Tab, Z or the wheel step through an overlap, and the outline moves with each step.
- While a ring is open, its thing keeps the outline, so you can see whose ring it is.
- The colour carries no meaning: one look for every thing and every agent.

| piece today | with the outline |
|---|---|
| the cream ring at a person's feet when there's no pill (brief 085, Fable A6 and A7) | goes: the outline is that cue now |
| the dashed rings at the feet of the other things under the pointer (brief 067 F9) | go: the outline marks the one a click hits |
| the dots on the pill (brief 067 F9) | stay: they say how many things are there, which the outline doesn't |
| the pill (the way and its price) | stays |
| classic controls: the cream ground ring and ellipse / the name chip | go / stays |

## For Latch, when Sky says yes

| what | spec |
|---|---|
| line | ink #17302f at 1.5 px and 92 %, then cream #f7f4ec at 1 px and 85 %; both outside the thing's edge |
| size | 1080p px × screen height / 1080 (2.6 + 1.7 px on the Air at 2880 × 1864); the same at every zoom |
| fade | in 0.1 s, out 0.1 s. A new thing fades in as the old fades out. It holds 0.15 s when the pointer leaves to open ground (the pill's stick). |
| cover | The line sits at the thing's depth and is hidden where something nearer stands. The exception is a person the silhouette pass draws through cover: their line goes round the silhouette too. |
| order | After the world, under the HUD. Never on the book, the start page or a conversation scene. Map view only for now (the silhouettes are play-camera only too). |
| shape | The thing's own renderers: a figure, the sedan model (not its click box), a camera's head. The door, the cabinet and stashes have no mesh of their own (`HostViews.Marker`), so they use a stand-in drawn only into the mask. For the door it's a flat arched panel the size of the Office's public doorway (Vela's facade gives the size); for a stash, its own door leaf (`ServiceDoors`) or bin. |

**Route.** This is the standard URP route, built the way `SilhouettePass` is (brief 056 F, from code, with no renderer-asset edit):
1. The host puts the hovered thing's renderers on a rendering-layer bit. 1 << 23 is free; 20 to 22 are the fade and the silhouettes.
2. A RenderGraph raster pass after the opaques draws them, with an unlit override, into a small mask with their own depth.
3. A full-screen pass, cut to the thing's screen box plus 8 px, draws the line from the mask's distance and compares the thing's depth with the scene's.

UI Toolkit draws the HUD after it. The stand-ins' shader has only a mask pass, so the main passes skip them.

**Cost.** One extra draw of one thing, plus a pass over its box only. On the Air a figure's box is about 70 × 170 px and the door's about 190 × 760. That's well under 0.1 ms of GPU at 2880 × 1864, and nothing while nothing is hovered. Redline should confirm it on the Air.

**Proof, delta only:**
- r085's two hovers (on the guard, and 40 px beside him) gain the captures `r085-outline-guard` and `r085-outline-door`.
- A row checks that the outline is on the thing the pill names.
- The look needs no new capture, since r085's frames are already at play zoom 10.

About a day of work, with its proof.

## For Sky

1. **Which look?**
   - What: B, the ink keyline; C, the turquoise glow; or A, the cream lit edge.
   - Why it matters: each colour already means something in the game except B's.
   - My recommendation: B. C is the strongest but reads as the cell's own colour. A merges with the edge of a person seen through cover.
2. **Is it a little?**
   - What: 1.5 px of ink and 1 px of cream at 1080p. The board shows it at true size beside 2 px.
   - My recommendation: keep 1.5. 2 px reads too, but heavier; it's the step up if 1.5 gets lost in play.
3. **Into the GDD?**
   - What: a yes adds one line to §10: "what a left click would act on wears a thin ink outline".
   - Who: Aster writes it as a new GDD version, then runs `bun run crew`.

## Evidence

- **Shapes:** `make/ol_masks.py` cuts each thing's mask with GrabCut on a 4x crop, using hint shapes. The door's mask is the doorway's shape. The Fixer's mask is her lit body plus the silhouette the game drew. `out/scratch/glyph/outline/masks/chk-*.png` show each edge in magenta.
- **Lines:** `make/ol_draw.py` draws each line from the mask's distance at 4x, then scales it down: the same sum the edge pass does. The HUD is laid back over the line from the capture's own pixels, because the `.hud.png` stores tinted sprites untinted.
- **Screen share:** the line's share while something is hovered, read at alpha ≥ 24 like tools/hud-budget (`make/coverage.json`):

  | thing | share |
  |---|---|
  | the guard | 0.032 % |
  | the Tech | 0.033 % |
  | the mast | 0.037 % |
  | the Fixer | 0.038 % |
  | the door | 0.086 % |
  | the car | 0.087 % |

  The line is world art like the rings, so the HUD budget doesn't count it.
- **Run:** `python3 -I make/ol_masks.py`, then `ol_frames.py`, then `ol_board.py`. The first copies the frames into scratch if they're missing and never overwrites them.
- **Files:** all under 500 lines; the largest is `make/ol_masks.py` at 156.
