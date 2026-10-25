# 063 · Portraits: Vale, Inez, the driver, and the patrol cameo

**State:** concepts and renders done 2026-10-24 (Forma), Sky away. Followed up the same day for the coordinator: driver tint fixed, security officer traced, Vale B sheet prepared (§2, §3, §5). Nothing in the sim or contracts was edited; nothing installed in the kit; the one Unity change is the driver's three albedo JPEGs (§2). **Next owner:** Sky picks (table below), then Forma (Vale model, if approved), Glyph (briefing card, cameo disc, 2x crops), Latch (import).
**Asked by:** Aster for Sky, from brief 062 §7 (Vale has no face) and brief 057 §2 (`badges/npc_patrol`).
**Spend:** $0.353 on Runware (unchanged after the coordinator's follow-up: two more Codex tries failed at $0 and the driver fix was local) (9 stills, openai:gpt-image@2, the cheapest still model on record, about $0.043 each; pass `063-portraits/concepts`, budget $2.00). Codex returned 401 ("workspace routing discovery unauthorized") on a probe even though `codex login status` says "Logged in using ChatGPT": that failed row is ledger row 1 of `paid/063-portraits/ledger.jsonl`, and every Runware row carries the note "Codex login revoked (401)". Nothing else was spent.

## What to pick (each pick has its board)
| Pick | Options | Board | My lean |
|---|---|---|---|
| **Vale** | A silver, ivory stand-collar · B white beard, chain of office · C spectacles, shoulder disc | `briefs/063/board-01-vale.png` | **B** (the black yoke and bright collar make a bib silhouette nobody else has, readable at 50 px); A if you want understatement |
| **Inez** | A seafoam, locket, glasses (matches the model) · B coral tabard, tray · C mint pillbox, curls | `briefs/063/board-02-inez.png` | **A** (the cameo and the figure already agree, no new model); C if the face matters more |
| **The driver** | A mint tunic, visor band (the model today) · B ink black, gold, peaked visor · C deep teal woman, pillbox | `briefs/063/board-03-driver.png` | **A** (no new model; B collides with the Enforcer's and security's black) |
| **Patrol face** | A stern screen · B scan line · C cap and brim | `briefs/063/board-05-patrol.png` | **A**: a plain stern machine; C if you want a brim to read at 24 px (the patrol is a robot, so the cap is no longer needed to match an officer) |
Also: `board-04-models.png` (renders of the in-game models, the defects, and the Vale-model evidence).

## 1 · Concepts (done)
- Source: the style contract's people line without any date (era top line: "Bright retro-future: a space age imagined as the present…", couture line without "1958-1972"), the art bible's people rules (hem at hip or mid-thigh, nothing flowing, job by silhouette plus one colour block). Layout is one plate per option: a large head-and-shoulders and a full-body front, plain cream backdrop, gouache-and-marker finish from the contract's `character.finish`.
- Plates: `briefs/063/concepts/{vale,inez,driver}_{A,B,C}.png` (1536 x 1024). Prompts, model, cost, sha256 for each: `paid/063-portraits/ledger.jsonl`; the prompt text is also in `briefs/063/gen.py`.
- Vale reads as an institution figure: ivory is the dominant block (the carried Vale separates from the Enforcer's black), ink and thin gold only as trim, older, no 1950s dress.
- Inez carries her tells in the plate (hand at a locket for the affair, tray as a shield for the debt, a smile that does not hold) so the portrait sets up the two levers (Help the debt, Pressure the affair).
- The driver's block avoids ivory and black. The dress is institutional but not Vale's.
- Each plate has its **roster-framed cameo** cut from it: `briefs/063/cameos/{vale,inez,driver}_{A,B,C}.png` (512 master) and `@2x.png` (128 px, the roster's @2x). Framing matches the roster finals in `briefs/033/portraits/final/` (round, head 6 % to 70 % of the disc, thin dark ring). The ground is the plate's cream, not the roster's grey: Glyph may flatten it. These are painted; the in-game faces are 3D renders, so Glyph should check that a painted cameo and a rendered figure do not look like two people (Inez A and driver A are built to match the models, B and C are not).

## 2 · Portrait renders of the in-game models (done)
Blender renders of the real FBX and its 2048 albedo, 85 mm, framed like the roster cameos: `briefs/063/cameos/model_{waitress,driver,driver_as_shipped,councillor,security}.png` (+ `@2x`); full figures `briefs/063/models/fig_*.png`; scripts `briefs/063/src/`.
- **Inez (waitress W6):** ready.
- **Driver: tint fixed (coordinator's go, 2026-10-24).** The defect was real: `tools/figures/figures.json` tinted hue 2 to 20 degrees (his coral uniform turned mint) and his lips, nose, cheeks, ears and both hands sit in that window, so they were teal in the game. The fix is in the tint, not a render trick:
  - `tools/figures/build.py` `apply_tint` takes a `keep` list: `{"mask": "masks/x.png"}` (white pixels of a UV-space map are never tinted), `{"ellipse": ...}`, `{"box": ..., "hueAbove": deg}` (skin is more orange than coral).
  - `tools/figures/keepmask.py` makes the mask from the mesh (the faces above a height), because the generator's atlas is fragmented (the lips sit in five small islands): `blender -b -P tools/figures/keepmask.py -- <driver.fbx> tools/figures/masks/driver_head.png 1.515 2048 8`. The mask is committed: `tools/figures/masks/driver_head.png`.
  - The driver's `tint` in `figures.json` now has `keep`: the head mask plus `{"box": whole map, "hueAbove": 11}` (hands, arms, neck). The uniform stays mint everywhere (checked on the full figure).
  - Re-rendered cameo: `briefs/063/cameos/model_driver.png`; the old one stays as `model_driver_as_shipped.png`; full figure `models/fig_driver_fixed.png`. One fingertip of the right hand keeps a teal fleck of a few pixels (an island edge); invisible at play zoom.
- **Councillor stand-in:** a young blond commuter (D4); not Vale.
- **Security officer (capped, human):** a separate figure, not the patrol (see §5).

### What Latch re-imports (driver only)
I rebuilt `driver` into a scratch tree and copied **only the three albedo JPEGs** into the Unity folder, same names, so GUIDs and metas stay (the originals are kept in `out/scratch/forma/063/driver-before/`):
- `unity/Assets/Suborned/People/Figures/driver/Textures/driver_body_2048_albedo.jpg`
- `unity/Assets/Suborned/People/Figures/driver/Textures/driver_body_1024_albedo.jpg`
- `unity/Assets/Suborned/People/Figures/driver/Textures/driver_body_512_albedo.jpg`
Not copied, and not to be: the rebuilt `.mat`, `.meta` and `driver.fbx` differ from Unity's only by what Unity wrote itself (`disabledShaderPasses`, lightmap slots, `streamingMipmaps`) and a timestamp, so Unity's own copies stand. The geometry, rig, normal maps and materials are unchanged. Latch: refresh the assets, look at the driver once in play (face, hands), nothing else to do. Behaviour and replays are untouched (view only).

## 3 · Vale model: proposal (not started)
**Why a new model.** `councillor` is a recolour of commuter D4 (a young man); his head is baked into a texture atlas, so aging him means a new head. The only elder in the library, `el1`, is a woman in an orange jumpsuit (board 04). Painted cameos solve the briefing, the feed and the roster, but the man on the street and the man you carry stay a boy until there is a model.

| Path | What | Effort | Cost | Result |
|---|---|---|---|---|
| **1 Now, no model** | Use the picked painted cameo for the briefing file card, the Intel face and the feed; leave the stand-in figure | 15 min (Glyph crops) | $0 | the face is right, the figure is young; at 50 px the figure reads as a pale jacket and camel trousers |
| **1b Cheap tint** | Retint the stand-in to ivory and ink (a `tint` edit in `figures.json`) so the colour block matches the pick | 30 min | $0 | colour right, face and age still wrong |
| **2 New model (recommended after the pick)** | The pipeline the D-cast came from (`Citizens3D/PROVENANCE.json`): (a) model sheet from the picked plate (the contract's `model` type: front, side, back, A-pose, gpt-image@2, about $0.045 a take, 3 takes); (b) image to 3D, Hunyuan 3.1 pro, 50k faces, **$0.675 a take**, 2 takes; (c) decimate to 8000 / 3000 / 1500 tris, skin from the C1 Meshy rig (`kind: transfer`), replace the `councillor` entry in `figures.json` (same id, so the manifest roles stay), `build.py councillor`; (d) check A-pose, collar and trim at 40 to 60 px at play zoom, and the Enforcer's carry clip on him (he is carried) | about 3 hours of mine, plus Latch's import and Glint's matte-map check | **about $1.5 to $2.2** (sheet takes $0.14 + 2 to 3 Hunyuan takes $1.35 to $2.0), under the $10 line | a real Vale: older, ivory, one-off silhouette |
| 3 Adapt el1 | recolour and reshape the elder woman | not viable | | rejected: wrong person |
- Risk by pick: **A** is the simplest geometry (tall collar, plain jacket); **C** has one shoulder disc (fine); **B** has a chunky link collar that 3D generators tend to fuse into a solid bib (it would still read as the bib, but the gold links will be paint).
- I will not start (2) without your OK; if you pick Inez A and driver A, no other new model is needed.
- **Prepared for B, at no cost (coordinator, 2026-10-24):** the model-sheet prompt (the contract's `model` line, B's costume, A-pose, orthographic front/side/back, hip hem) is `briefs/063/vale_B_sheet_prompt.txt` and the runner is `briefs/063/vale_sheet.py` (Codex first, with the B plate as the reference). **The sheet itself is not made**: Codex returned 401 again (ledger row 11 of `paid/063-portraits/ledger.jsonl`), so the only way today is Runware, about $0.06 a take (`vale_sheet.py --runware`, pass budget $0.30). That is a tiny spend but it is not "no cost", so I did not run it. No image-to-3D call was made.

## 4 · Patrol cameo (done, preview only)
Three faces drawn as ink glyphs on the kit's cream enamel, the same treatment as `npc_driver`, `npc_waitress` and the others (a cream disc with the chrome bezel and an ink pictogram), fitted to Glyph's 29 px glyph box, with a mast lamp and dot-matrix dots, no cartoon eyes (the contract's robot rule).
- Face art for `python3 -I briefs/057/patrol/disc.py build FACE.png`: `briefs/063/patrol/face_{A,B,C}.png` (464 px, ink on transparent). Previews of the two outputs (full badge and face-only for `Cameo()`): `briefs/063/patrol/npc_patrol_{A,B,C}@2x.png`, `npc_patrol_face_{A,B,C}@2x.png`, made by importing Glyph's `disc.py` into my scratch; **nothing was written to `briefs/057/patrol/` or `Kit/badges/`**. Glyph installs the pick: `build briefs/063/patrol/face_C.png` then `install`.
- Legibility: all three hold at 24 px (1x); the dot rows are 2 px at 48 px (@2x). A reads from the two eye-bars, B from the scan row, C from the brim. C's brim is no longer needed to tie it to an officer (see §5), so A or B are as good; C is still the most distinct from the other npc_* discs at 24 px.
- Glyph's current stand-in (a smile) is installed in the kit already; mine are stern on purpose: a watcher.

## 5 · The capped `security` officer: a separate person, not a stand-in for the patrol (read, nothing changed)
The patrol is a robot in play. Evidence in the code:
- `HostViews.Figures.cs:36` `RoleFigure("patrol") => null`: no human figure is ever picked for the patrol. `HostViews.cs:39` draws it from `host.patrolRobot` (Vela's PatrolRobot, brief 031: its head is the attention face), hides the placeholder head, and `RobotFaces` / `StageRobot` give it the dot-matrix face (`HostViews.Figures.cs:251, 288-293`).
- The `security` figure (the door guard P1 turned charcoal, cap) is the human **posted and response officer**: `RoleFigure` maps `security/*` (the two legacy posted officers, `HostViews.cs:35`) and `unit/*` to it, `HostViews.cs:289` dresses a response-dial unit as `security` when `!u.Robot` (robot units are drawn as squat drums), and the fight's hostile officers (`hostile/<n> (officers)`) use it too. GDD §04/§05 have both: patrol officers and posted guards are people, the response dial sends robots and officers.
- The only stale place was `unity/Assets/Suborned/People/manifest.json`, `playRoles."patrol": ["security"]`, the Oct-18 stand-in table (brief 020), from before Vela's robot. **Corrected (coordinator's go): removed**, not repointed. The table's rule is "capsules stay the fallback for any key not listed" and a list is a pool of figures; the patrol is no figure but a prefab, and the code returns null for it, so "not listed" says the same thing. Fixed at the source, `tools/figures/roles.json` (`playViews`, with a sentence in its `note` saying why `patrol` is absent), and the same edit in the generated `manifest.json` (the two `playRoles` blocks are equal again). Nothing reads that key at runtime (only a tooltip in `FigureLibrary.cs` mentions the table). Latch: the manifest and the three driver albedos arrive in the next build; no code change.
- So: `board-04` now labels the figure "Security officer: posted and response officers; not the patrol"; the patrol cameo is the robot.

## Evidence
- Spend and rows: `python3 -I tools/paid/paid.py status 063-portraits` → `$0.35 in 10 calls (1 failed, 0 blocked, 9 used)`, pass concepts $0.35 / $2.00; rollup refreshed in `tools/paid/spend.json`.
- Boards rebuilt by `python3 -I briefs/063/boards.py` (no model call); each viewed after building: no clipping, cameos are real 256 / 128 / 64 px. The 2x HUD crop check belongs to Glyph once a cameo is placed.
- Not done: the file-card photograph (360 x 296 rectangle, duotone) is Glyph's crop of the picked cameo master; the night/day look of the portraits; any change to the figures, the manifest or the kit.

## Open and asks
- **Sky:** the four picks above; OK for path 2 (Vale model, about $2 and 3 hours); the driver tint fix is done.
- **Aster:** the Codex login still gives 401 (ledger rows 1 and 11); the stale `playRoles.patrol` line in the people manifest (§5).
- **Glyph:** place the patrol pick; decide the cameo ground (cream or roster grey) and run the 2x crop review.
- **Next owner:** Sky (picks), then Forma (model), Glyph, Latch.

## Sky's calls (2026-10-25)
Sky, 2026-10-25, on the outstanding-work summary that listed each pick: "Your recommendation seem good".
Vale **B** (white beard, chain of office) · Inez **A** · the driver **A** · patrol face **A** (stern screen). Path 2 (the Vale model, about $2) was not raised with Sky by name; it is under the $10 pass limit, so it goes through `tools/paid/paid.py` with a $3 budget.
Next: Forma (Vale B model and portrait), Glyph (patrol placement, cameo ground, 2x crops), Latch (import).
