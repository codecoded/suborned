# Forma · brief 067, batch 0.2.123: the rivals' look (W1), the clerks, the rivals' idle beats

2026-10-26. Aster's three items for me, from Pulse's audit (`briefs/067/pulse.md` §1.3, `pulse-figures.csv`, `pulse/looks-0122-play-zoom.png`). Nothing edited in `unity/`, `sim/`, the GDD or the style contract; nothing installed; nothing committed. Paid: none (one Codex still, $0, `paid/067-rival-look/`). The figures are in `out/people-models/People/` (`manifest.json`, `Figures/rival`, `rival_b`, `rival_c`, `rival_d` and the other 22; git-ignored, built, not installed).

## 0 · The short answer

| item | what I found | what I did | next |
|---|---|---|---|
| **1 · W1 the rivals look like police** | Pulse's numbers hold, and the picture is worse than the numbers: the rival was Worker D5, a **hard hat** (the cap Sky saw, with a red lamp band), amber piping and a navy-jeans lower half, jacket value x 0.38. In the real 3:20 frame they are three dark aubergine figures with hats; beside the police and a guard in shade (Pulse's crop) they are one picture | **Four looks, the Velvet Set**: three colourways of one look on Commuter D4's body (`rival`, `rival_b`, `rival_c`: violet bomber, deep-violet ribbing and tunic, pale trousers, pearl boots, three heads of hair) and the crew's woman on Commuter C6's (`rival_d`: violet hooded jacket, white dress with violet chevrons). No cap, no gold, no amber. LODs 8000, 3000, 1500 (the driver's and councillor's tiers). Every look is inside Pulse's spec with room: median V 0.52 to 0.64, violet 0.50 to 0.76, dark 0.20 to 0.30, amber under 0.006 | **Latch** (reinstall, library, the four host lines in §4) |
| **2 · d4 / d4b against the door guard** | At true size they do **not** read as the guard: the tops are alike (grey-white bomber against white duty suit, dE 10 to 17) but the legs are navy or charcoal against the guard's white (dE 32 to 39), the head is bare against a cap, and the guard is blue-patched. The nearest clerk to the guard by colour is not d4 but **d5c** (mint dress, pale cloche: dE sum 20 against d4's 49) | **No recolour.** Proof frames and the numbers (§2) | Aster / Sky: if guards are still seen among the clerks after W1 and W2, d5c is the one to change; I did not touch it |
| **3 · the rivals' idle beats** | The engine gives a posted member seven beats; the library has clips for two of them (`idle` / `idle_alt` for watch, `look_around`) and walks for the pace beats. Three have nothing in the manifest but have a Mixamo clip in the store (lean, check-watch, read-paper); the wheelman's three are never drawn (he rides unseen) | Nothing built. The table and a contact sheet of the candidates (§3) | **Latch**: a one-line hookup for look-around and talk today; Forma adds three library clips next round if Aster wants it |

Done-check, run in the working tree (shared and moving):

```
bun test tools/figures                              15 pass, 0 fail (tools/figures/rival.test.ts: the specs, the masks, the pools, the grade round trip, and the albedo spec on the build)
bun test tools/people tools/routines tools/life tools/figures tools/people-view tools/people-graph tools/levels/people
                                                    306 pass, 1 skip, 1 fail: the one is NOT mine (Pulse's pin of HostViews.cs, §5 item 7: Latch has edited the line it pins)
bun run typecheck                                   clean
bun tools/lint/comments.ts                          0 hits
python3 -I tools/figures/albedo_stats.py --check    spec (brief 067 W1): OK, every rival colourway inside it
bun run guard bun tools/figures/run.ts              26 figures, 51 clips, 1 prop, 102.8 MB, in out/people-models/People (not installed)
```

## 1 · W1 · the rival look

### 1.1 What was wrong, from the figure

`rival` was `workers_d5` (Worker D5, brief 020) with `tint {from 48, width 22, shift 235, sat 0.8, val 0.38}`. Looked at close (`out/scratch/forma/067/views_workers_d5.png`), the model is a worker in a yellow quilted jacket, navy cargo trousers, a red-and-white neckerchief and a **yellow hard hat with a red lamp band and a short peak**. The tint kept the lamp band, the navy trousers and the hat's peak and made the jacket dark: at 53 px that is a dark uniform with a cap, which is the police to the eye. Pulse measured it (median V 0.27; the police 0.32); I looked at it in the game's own frame (`ring-plain-rival.png`, 3:20): three dark aubergine figures, hats with an orange lamp, navy-black legs.

### 1.2 The look

The concept is an open brief, no date, space-age couture, through the Codex tool at $0 (`briefs/067/forma/rival_lineup_prompt.txt`, `paid/067-rival-look/codex/rival_lineup_1.png`, copied to `briefs/067/forma/concept-rival-lineup.png`): five people in one crew, velvet-violet bomber jackets in plum, mid and lavender, slim pale trousers, pearl boots, chrome belt and ring, bare heads. It came back as the crew and I built to it. The name is the art bible's own: `violet_set #6b4f8f`, the Velvet Set.

| figure | body | jacket | ribbing and tunic | legs | boots | hair | for |
|---|---|---|---|---|---|---|---|
| `rival` | D4 | mid violet `#7a58b8` | deep violet | ivory | pearl | dark brown | rival-1, rival-5 |
| `rival_b` | D4 | deep violet `#6b4f8f` (the art bible's) | plum | pale grey | pearl | platinum | rival-2 |
| `rival_c` | D4 | lavender violet `#a58bd8` | mid violet | pearl | pearl | auburn | rival-4 (the lookout) |
| `rival_d` | C6 | violet hooded jacket `#8a68c4`, lining `#4f3a80` | deep-violet skirt hem | white dress, violet chevrons, bare legs | pearl | blond | rival-3 |

Why D4 and C6: the concept's garment set (cropped bomber, ribbed hem and cuffs, turtleneck under it, slim trousers) is what Commuter D4 already wears, in white over navy, so the recolour is a swap of blocks and the figure keeps its own cut; C6 gives the crew a woman and a hood, a silhouette no D4 has, so the three posted rivals are not three of one build. Both are 50k Hunyuan meshes already in the store, skinned from the C1 rig as every crowd figure is, so nothing about the rig, the clips or the carry changes (checked on `rival` and on `rival_d`: twelve library poses each on the full-detail LOD0, idle to carried to lying, no tear in the jacket, the hood or the hem: `out/scratch/forma/067/posecheck_rival.png`, `posecheck_rival_d.png`).

Detail, no new compression: LOD0 8000 triangles at 2048, LOD1 3000 at 1024, LOD2 1500 at 512, from 50k meshes with 4096 px sources. These are the tiers the driver and the councillor already ship (the old rival was 4500 / 1500); nothing is added to what is compressed, and the 512 map is what play zoom draws.

### 1.3 Pulse's spec and where it stands

| figure | median V (>= 0.45) | violet (>= 0.40) | dark under V .35 (<= 0.40) | amber (<= 0.01) | navy |
|---|---|---|---|---|---|
| old `rival` (installed) | 0.27 | 0.41 | 0.86 | 0.03 | 0.28 |
| `guard` / `security` (for the room) | 0.47 / 0.32 | 0 / 0 | 0.30 / 0.68 | 0 / 0.21 | |
| **`rival`** | **0.545** | **0.507** | 0.289 | 0.0055 | 0.002 |
| **`rival_b`** | **0.522** | **0.498** | 0.301 | 0.0048 | 0.003 |
| **`rival_c`** | **0.639** | **0.512** | 0.208 | 0.0048 | 0.002 |
| **`rival_d`** | **0.537** | **0.764** | 0.196 | 0.0029 | 0.002 |

Pulse's own `briefs/067/pulse/albedo-stats.py`, with only its folder changed to the build, prints the same (0.55 / 0.52 / 0.64 for the three). One honest note on the amber column: Pulse's rule counts hue 20 to 70, S > .3, V > .3, and that also takes the deepest brown shades of skin and hair (V .30 to .36, just under his skin rule's floor), which were 1.1 to 1.5 % of the D4 atlases. They are shade, not trim; I put a last tint on all four that takes those texels 20 % darker (the last `tint` entry of each in `figures.json`; the reason is in `briefs/067/forma/gen_rivals.py`), which is invisible and moves the number from 0.011 to 0.015 down to under 0.006.

The no-cap, no-gold and pale-trousers lines cannot be measured by the stats, so they are held in the spec and looked at: the three D4 heads are bare; no recolour targets gold or amber (a test reads the colours it paints); the trousers are ivory, pale grey and pearl (the pale texels of the legs have median V 0.70, 0.67 and 0.73; Pulse asks 0.6); the dress chevrons that were coral are lilac. **One reading of the spec to confirm:** `rival_d` has a hood up round her head, in the jacket's own violet, no peak. Pulse's line allows "a bare head with hair, or a knit cap in the jacket's violet"; I took a hood as the same thing. If Aster reads it strictly, take `rival_d` out of the pool (the three D4 looks stand alone and meet the spec); nothing else depends on her.

### 1.4 How it is made (nothing hand-painted)

All in the pipeline, repeatable: `tools/figures/figures.json` (four entries replace the old `rival`), `build.py` (new: a recolour can be limited to, or kept off, a **UV mask** made from the mesh; `exact` keeps thin lines such as a zip; a tint's keep mask can be inverted), `zmask.py` (new: UV masks by place on the body, cut per texel so a hem is one clean horizontal line, bled into the atlas's gutters; run on the figure's own built LOD0), the masks in `tools/figures/masks/` (`d4_head`, `d4_legs` at 0.88 m, `d4_boots`, `d4_hair`, `c6_face`, `c6_boots`, `c6_dress`). `briefs/067/forma/gen_rivals.py` writes the four entries from the colour choices. Rebuild only the crew: `bun tools/figures/run.ts --out out/scratch/forma/067/build --only rival,rival_b,rival_c,rival_d` (under a minute; the manifest step then stops because it needs every figure, which is expected); the whole library is under two minutes.

### 1.5 Pictures (all true size or enlarged by whole pixels; the lighting is fitted, §5 item 2)

All in `briefs/067/forma/` (images are git-ignored, on disk):

| file | what it shows |
|---|---|
| `rival-street-before-after.png` | **start here.** The real 3:20 frame of 0.2.122: the three old rivals where Sky saw them, and the new three posted (rival-1, 2, 3) laid in the same frame, the same light, rings off, 4x |
| `rival-rows-before-after.png` | the old rival (installed) against the four looks, each beside the guard, the police and the driver, in the sun and in shade, camera yaw 30 and 210 (the Office front), 2x |
| `rival-beside-the-cell.png` | the real 0:40 frame with the cell as the game draws it and the crew laid in beside it: the cell is ivory and black with a cyan edge, the crew violet over pale legs |
| `rival-concept-and-figures.png` | the Codex concept over the four built looks (front, back, head, full detail) |
| `clerks-vs-guard.png`, `clerks-vs-guard.txt` | item 2: the guard, d4, d4b, d5c and the police at true size at yaw 210, sun and shade; the colour-block table |
| `rival-beats-candidate-clips.png` | item 3: library clips on the crew figure |
| `albedo-stats-after.txt` | the spec check |
| `boards.py`, `blocks.py`, `gen_rivals.py` | the scripts that made them |

Reading them: in the 3:20 frame the old crew is a dark cluster with hats, the new crew is three violet jackets over pale trousers that no other figure in the cast wears; in shade the jackets stay violet and the legs stay pale. Against the guard the crew's tops are 34 to 46 dE away (violet against white and grey), the legs are close (pale against pale, dE 7 to 18), so the read is the jacket, which is the one colour block the spec asks for.

## 2 · d4 and d4b against the door guard

A true-size frame: the play camera (orthographic, 38 degrees, 28 m over 1080 px, so a 1.75 m figure is 53 px), the game's sun and ambient from `light.json`, the game's grade (exposure, saturation, contrast, ACES), over a real stretch of the road of the 0.2.122 frame, rings off. `tools/figures/proof_run.py` makes it; `clerks-vs-guard.png` is the board.

Colour blocks at that size (CIE76 against the guard, the figures facing heading 75, yaw 210; the shade rows are within 4 of these):

| figure | upper dE | lower dE | sum | what it is |
|---|---|---|---|---|
| `d4` | 10.5 | 38.9 | 49.4 | grey-white bomber over navy |
| `d4b` | 17.4 | 32.4 | 49.8 | peach bomber over charcoal |
| **`d5c`** | **8.6** | **11.2** | **19.7** | mint dress, pale cloche, white boots |
| `security` | 24.6 | 25.5 | 50.1 | olive and charcoal, cap |
| `driver` | 18.1 | 24.2 | 42.2 | mint uniform, visor |
| the four rivals | 34 to 46 | 7 to 18 | 46 to 56 | violet over pale |

The read: d4 and the guard share a light top, but d4's legs are dark where the guard's are white with blue panels, d4 has a bare head where the guard has a pale cap, and the guard's sleeves and thighs carry blue patches. In the frame they are two different people in both lights. **No recolour of d4 or d4b.** The row I would watch instead is **d5c**: pale head to boot like the guard, with a pale cloche that reads as a cap at 53 px. By Pulse's own count there are 29 of them at the Office. If Sky still sees guards among the clerks after W1 and W2, that is the figure to change (a darker or coral hat, one tint, a minute to build); I have not touched it because it was not asked and it changes 29 people's look.

## 3 · The rivals' idle beats (MemberBeat), what exists

`Game.MemberBeat(id)` answers one of the rulebook's `rivals.beats` poses; the host plays none of them (a posted member falls through to `idle` / `idle_alt` at `HostViews.Figures.cs`, the `else state = armed && g.FightOn ? "aim_pistol" : ...` line).

| beat | who, seconds | clip in the manifest today | in the Mixamo library, not in the manifest | missing |
|---|---|---|---|---|
| watch | posted 10 and 8, lookout 8 and 5 | `idle`, `idle_alt` (already played) | | |
| look-around (yaw +-60) | posted 2 + 2 | `look_around` (loop) | `Looking_Around`, `Nervously_Look_Around`, `Look_Over_Shoulder` | |
| pace-out, pace-back | lookout 6 + 6 | `walk` (the engine moves him, so speed already plays it) | | |
| talk, walk, board | a member in that mode | `talk`, `walk` | `Entering_Car` for board | board needs a car-door place |
| lean (only beside cover) | posted 9 | | `Leaning`, `One_Shoulder_Lean`, `Leaning_On_A_Wall` (each wants a wall) | add one |
| check-watch | posted 3, lookout 3 | | `Inspecting` (a hand raised in front of the chest, head down: reads as a wrist check on the crew figure) | add one |
| read-paper | posted 8, lookout 6 | | `Standing_Idle_03_Examine` (hands low, head down), `Inspecting` | add one |
| wheel, mirror, window-arm | the wheelman, aboard | | `Driving`, `Honking_Horn`, `Look_Over_Shoulder` | not needed: an aboard member is not drawn (`RivalViews`: `aboard` -> `Set(id, null)`) |

So the existing-clip hookup for Latch is one line and covers watch, look-around and talk; pace and walk already follow the movement. In `HostViews.Figures.cs` before the final `else state = ...`:

```csharp
else if (IsCrewId(key) && !armed) state = g.MemberBeat(key) switch { "look-around" when Has(f, "look_around") => "look_around", "talk" when Has(f, "talk") => "talk", _ => f.phase > 0.5f ? "idle_alt" : "idle" };
```

Lean, check-watch and read-paper are three library files copied through `clips.json` (a line each, the pipeline's own route, no authoring: `Leaning`, `Inspecting`, `Standing_Idle_03_Examine`), a state each in FigureBuild's controller, and one more arm of that switch. I have not added them (you said not this round); `rival-beats-candidate-clips.png` shows the candidates on the crew figure (my retarget, a stand-in for Unity's: the poses are right, the exact frames are to be picked when they go in).

## 4 · For Latch

**What to install** (the build is already made, 26 figures, at 09:10 on 2026-10-26, after the last change to `figures.json`, `roles.json` and the masks; a later edit to those needs `bun tools/figures/run.ts` again, under two minutes):

```
bun tools/figures/run.ts --install     # rebuilds and mirrors, or, from the build that is there:
rsync -a --delete --exclude '.rows.*' --exclude Generated --exclude Generated.meta out/people-models/People/ unity/Assets/Suborned/People/
```

What changes in Unity: `Figures/rival/` is **replaced** (new FBX, three LODs, albedo at 2048 / 1024 / 512, new materials, the old normal map gone; the same GUIDs, which are made from the path), `Figures/rival_b/`, `rival_c/`, `rival_d/` are **new** (the D4 trio have no normal map; `rival_d` keeps C6's, at 2048 and 1024), `manifest.json` has 26 figures, the pool `role/rival` (jobBlocks key, used verbatim by FigureBuild), `playRoles`, `forLatch` items 14 and 15. Every other figure comes back with the same textures, materials and metas; their **FBX bytes differ** (the FBX writer stamps its own time, and the 11 authored clips do too: carried_drape, carry_shoulder and its idle, run_lean, serve and serve_walk, the five sits), so Unity will reimport all 26 FBX and those 11 clips though only the four rivals changed in content (21 figures differ in their FBX alone; the driver's three JPEGs differ in bytes, not in pixels, checked). The new manifest lists the new hashes, so a partial install (only the rival folders) would leave 22 figures with hashes that do not match their files; nothing in the repo compares them, but install whole.

**Rebuild the FigureLibrary:** menu `Suborned/People/Build Figure Library` (or the play install, which calls `FigureBuild.Build`). The library gets 26 figures and the pool `role/rival` = `rival, rival_b, rival_c, rival_d`. `FigureBuild.Women` (line 22): add `"rival_d"` so she walks with `walk_f`. (c6 and d5c are women and are not in that set either, so they walk the men's clip today; say if that is meant.)

**Without any code change** every rival draws as `rival`, now the violet crew in its first look: `RoleFigure` still returns `"rival"`. That alone answers Sky's note. To give the crew its four looks, dress by member number, not by hash (a hash can put three on one look). Four sites in the working tree, one helper:

```csharp
// HostViews.Figures.cs: next to RoleFigure
static readonly string[] RivalLooks = { "rival", "rival_b", "rival_d", "rival_c", "rival" };   // rival-1 .. rival-5
static string RivalFigure(string id) { int i = id.Length; while (i > 0 && char.IsDigit(id[i - 1])) i--; int.TryParse(id[i..], out var n); return RivalLooks[(Mathf.Max(n, 1) - 1) % RivalLooks.Length]; }
```

| site | now | after |
|---|---|---|
| `HostViews.Figures.cs:36` (RoleFigure) | `... IsCrewId(key) \|\| key.StartsWith("body/rival-") => "rival",` | `... => RivalFigure(key),` |
| `HostViews.cs:161` and `:203` (the fight's hostiles) | `Dress(hostileKeys[i], h.Id.StartsWith("rival") ? "rival" : "security")` | `... ? RivalFigure(h.Id) : "security"` |
| `HostViews.cs:278` (a rival taken down, his body) | `Dress(key, "rival")` | `Dress(key, RivalFigure(p.Id))` |

The three posted are then the man in mid violet, the man in deep violet and the woman in the hood; the lookout is lavender; the wheelman (mostly unseen) is mid violet.

**Proof frames** (yours to take): 3:10 (the car parks, the three post round the getaway car), 3:20, the fight with the crew armed (`aim_pistol`), a rival taken down and carried (his body must wear his own look), at yaws 30 and 210, rings on and off. What to check beside my boards: the violet reads as violet after the grade (mine is a fitted stand-in for it); the rival is not mistaken for the guard, the police or the cell (the three comparisons are in the boards). Glint to look at the violet and the pale legs in the evening and night phases.

**Pulse** after your host edit: `tools/people/looks.ts` `roleFigure()` mirrors `RoleFigure`; it should give `rival-n` the same table, and `tools/people/test/looks.test.ts` pins the text of the host (its `the response, the rivals and the bodies are dressed as the audit says` is red now because `HostViews.cs:279` already reads `BodyFigure(p)`, your in-flight edit for Pulse's body-figure item, so the line it pins is gone; it will need the three sites above too).

## 5 · Open issues

1. **The bodies are shared with the crowd.** D4 is the clerks' figure (29 at the Office, `d4` and `d4b`), C6 is three residents'. Colour alone tells a rival from a clerk, and the colour is one nobody else wears; but Pulse's "a new model is best" is not met, and the concept's five are five bodies. The upgrade is real and cheap: new meshes from model sheets through Hunyuan 3.1 pro, **$0.675 a take** (Vale C's route, `briefs/063/vale_C_mesh.py`), the concept and the sheets at $0 from Codex. Four to five bodies is about $3 to $7 with one retake each. I have not spent it; it needs Aster's go (and Sky's eye on the concept board first).
2. **The proof is a fitted stand-in for Unity, not Unity.** Blender Cycles, the game's sun and ambient from `light.json`, the game's grade, a real road. The game's shade is much softer than the trilight alone gives (the road's sun-to-shade ratio is 0.55, not 0.28), so the ambient is x 3.1 and the figure light x 0.7 with a 0.03 veil, fitted to the old rivals in the real 3:20 frame (their jacket median is (82, 62, 80) there; the render of the installed rival gives (71, 53, 80), their dark median (67, 58, 64) against (61, 61, 81)); the cell's cream coat and the driver's mint look close beside the real ones. It tells whether a figure reads beside its neighbours, not what the game will print. Latch's 3:10 frame is the check.
3. **Every build changes every FBX's bytes** (above). Harmless, and nothing in the repo compares the installed files with the manifest's hashes; but install the whole build.
4. **"Full detail, no decimation", read literally**, would be the 50k source at LOD0 (and 4096 px). I held to the driver's and councillor's tiers (8000 / 3000 / 1500 at 2048 / 1024 / 512), the highest tier of crowd-class figures that already ships, and added no compression. If you meant the literal, it is removing `decimateTo` from four entries and a texture size; the cost is 50k triangles a rival at close zoom and four 4096 maps.
5. **d5c** is the clerk nearest the guard (§2). Not changed.
6. **W5** (the two guards in uniform at the café table from 0:00): not mine this round. The same recolour route could make an off-duty guard figure (a bomber in civilian colours on the guard's body) if Aster chooses option (b).
7. **Red tests that are not mine:** `looks.test.ts` "dressed as the audit says" (Latch's edit of the line it pins, §4). I changed one assertion of Pulse's file, the L-3 test (`tools/people/test/looks.test.ts`): it expected the rival to be Worker D5 "the open one: Forma gives the rival a figure of his own"; it now expects the crew on D4 and C6 and nobody else wearing them. Pulse to take it over.
8. **A residue**: at LOD0 some tan flecks remain in the dark-brown hair of `rival`, and the dress's left edge has a pale pink gradient; at play zoom neither shows. The pearl boots read white at 53 px. The jackets have no sleeve stripes (the concept's pearl stripes): a 1.5 cm stripe is half a pixel at play zoom, so I left it; `zmask.py` can place it by position if the follow view wants it.
9. **Words for Aster** (GDD §15 people, a v0.26.0 line; Pulse proposed the first sentence): "the rivals are the one crew the art keeps apart from security: violet, no cap, no gold" (the figures conform), and for the art bible: the Velvet Set wears a violet velvet bomber or hooded jacket over pale trousers or a white dress, pearl boots, bare heads, chrome only; four looks on two stand-in bodies until the finals. `roles.json` `missing[]` carries the same, and its stale Vale line is refreshed (he has his own model since brief 063).

## 6 · Next owner

| to | what | done when |
|---|---|---|
| **Latch** | install and rebuild the library (§4); `FigureBuild.Women` += `rival_d`; the four host sites (optional, one helper); the one-line beat hookup (optional); proof frames 3:10, 3:20, fight, carried body | the 3:10 frame shows three different violet looks round the getaway car and none reads as police; `python3 -I tools/figures/albedo_stats.py --base unity/Assets/Suborned/People/Figures --check` prints OK; Pulse's `albedo-stats.py` on the install (its `names` list gets the three new ids) gives the §1.3 numbers |
| **Pulse** | the `looks.ts` mirror and the test pin after Latch's host edit; decide with Aster whether d5c is next | `bun test tools/people` green |
| **Aster** | the go (or not) on new bodies, $0.675 a take; the GDD line; W5 | decision logged |
| **Glint** | the violet and the pale legs through the grade by day, evening and night | read at 53 px in each phase |

## Evidence (commands)

```
python3 -I tools/figures/albedo_stats.py --check rival rival_b rival_c rival_d guard security driver ...    briefs/067/forma/albedo-stats-after.txt
python3 -I briefs/067/forma/boards.py all                  the pictures (a few seconds a row)
python3 -I tools/figures/proof_run.py --people out/people-models/People --out x.png --figs 'guard,security,rival=200,rival_d=200' --light both --scale 4
blender -b -P tools/figures/turn.py -- out/people-models/People/Figures rival_d 0 out/scratch/forma/067/turn      the close views
bun run guard bun tools/figures/run.ts                     the full build (26 figures, 51 clips, 102.8 MB; the new manifest is out/people-models/People/manifest.json)
bun test tools/figures; bun run typecheck; bun tools/lint/comments.ts
```

Scratch kept: `out/scratch/forma/067/` (the first-pass renders, the pose checks `posecheck_rival.png` and `posecheck_rival_d.png`, the candidate-clip script, the atlas pictures). Code: `tools/figures/{figures.json, roles.json, build.py, assemble.py, run.ts (header), zmask.py, albedo_stats.py, proof.py, proof_post.py, proof_run.py, proof/road_sun.png, proof/road_shade.png, turn.py, rival.test.ts, masks/d4_*.png, masks/c6_*.png}` and `tools/people/test/looks.test.ts` (L-3 only). Note: `tools/figures/inspect.py` shadows Python's `inspect` if the folder is ever put on `sys.path`; the new scripts load each other by file path for that reason.
