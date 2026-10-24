# 063 · Portraits: Vale, Inez, the driver, and the patrol cameo

**State:** concepts and renders done 2026-10-24 (Forma), Sky away. Nothing in Unity, the sim or contracts was edited; nothing installed in the kit. **Next owner:** Sky picks (table below), then Forma (Vale model, if approved), Glyph (briefing card, cameo disc, 2x crops), Latch (import).
**Asked by:** Aster for Sky, from brief 062 §7 (Vale has no face) and brief 057 §2 (`badges/npc_patrol`).
**Spend:** $0.353 on Runware (9 stills, openai:gpt-image@2, the cheapest still model on record, about $0.043 each; pass `063-portraits/concepts`, budget $2.00). Codex returned 401 ("workspace routing discovery unauthorized") on a probe even though `codex login status` says "Logged in using ChatGPT": that failed row is ledger row 1 of `paid/063-portraits/ledger.jsonl`, and every Runware row carries the note "Codex login revoked (401)". Nothing else was spent.

## What to pick (each pick has its board)
| Pick | Options | Board | My lean |
|---|---|---|---|
| **Vale** | A silver, ivory stand-collar · B white beard, chain of office · C spectacles, shoulder disc | `briefs/063/board-01-vale.png` | **B** (the black yoke and bright collar make a bib silhouette nobody else has, readable at 50 px); A if you want understatement |
| **Inez** | A seafoam, locket, glasses (matches the model) · B coral tabard, tray · C mint pillbox, curls | `briefs/063/board-02-inez.png` | **A** (the cameo and the figure already agree, no new model); C if the face matters more |
| **The driver** | A mint tunic, visor band (the model today) · B ink black, gold, peaked visor · C deep teal woman, pillbox | `briefs/063/board-03-driver.png` | **A** (no new model; B collides with the Enforcer's and security's black) |
| **Patrol face** | A stern screen · B scan line · C cap and brim | `briefs/063/board-05-patrol.png` | **C** (ties the robot to the capped officer the game draws); A as the plain one |
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
- **Driver:** ready, but the shipped model has a **defect**: `tools/figures/figures.json` tints hue 2 to 20 degrees (his coral uniform turned mint), and his lips, nose, cheek shadow and neck sit in that window too, so his face and **both hands are teal in the game** (`board-04-models.png`, "Driver as shipped"). The repaired cameo (`model_driver.png`) takes the head from the untinted source above the chin; the rest is the shipped tint. I did not touch `figures.json` or the Unity textures. Fix, if you want it (about 30 minutes, Forma then Latch re-imports the driver textures): give `apply_tint` a keep mask (the face UV island and the hands) and rebuild `driver` only: `blender -b -P tools/figures/build.py -- tools/figures/figures.json <People dir> driver`.
- **Councillor stand-in:** a young blond commuter (D4); not Vale. Not usable for the briefing.
- **Security:** the in-game **patrol is this figure** (`manifest.json` role `patrol` → `security`, the door guard P1 recoloured charcoal: a capped human officer), while brief 057 and the request call the patrol a robot with a dot-matrix face. GDD §04 says "patrol officers". Aster: which is true? The cameo below is a robot as asked; option C (cap and brim) keeps it consistent with the officer the player sees.

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

## 4 · Patrol cameo (done, preview only)
Three faces drawn as ink glyphs on the kit's cream enamel, the same treatment as `npc_driver`, `npc_waitress` and the others (a cream disc with the chrome bezel and an ink pictogram), fitted to Glyph's 29 px glyph box, with a mast lamp and dot-matrix dots, no cartoon eyes (the contract's robot rule).
- Face art for `python3 -I briefs/057/patrol/disc.py build FACE.png`: `briefs/063/patrol/face_{A,B,C}.png` (464 px, ink on transparent). Previews of the two outputs (full badge and face-only for `Cameo()`): `briefs/063/patrol/npc_patrol_{A,B,C}@2x.png`, `npc_patrol_face_{A,B,C}@2x.png`, made by importing Glyph's `disc.py` into my scratch; **nothing was written to `briefs/057/patrol/` or `Kit/badges/`**. Glyph installs the pick: `build briefs/063/patrol/face_C.png` then `install`.
- Legibility: all three hold at 24 px (1x); the dot rows are 2 px at 48 px (@2x). A reads from the two eye-bars, B from the scan row, C from the brim.
- Glyph's current stand-in (a smile) is installed in the kit already; mine are stern on purpose: a watcher.

## Evidence
- Spend and rows: `python3 -I tools/paid/paid.py status 063-portraits` → `$0.35 in 10 calls (1 failed, 0 blocked, 9 used)`, pass concepts $0.35 / $2.00; rollup refreshed in `tools/paid/spend.json`.
- Boards rebuilt by `python3 -I briefs/063/boards.py` (no model call); each viewed after building: no clipping, cameos are real 256 / 128 / 64 px. The 2x HUD crop check belongs to Glyph once a cameo is placed.
- Not done: the file-card photograph (360 x 296 rectangle, duotone) is Glyph's crop of the picked cameo master; the night/day look of the portraits; any change to the figures, the manifest or the kit.

## Open and asks
- **Sky:** the four picks above; OK for path 2 (Vale model, about $2 and 3 hours) and for the driver tint fix.
- **Aster:** patrol robot or officer (§2); the Codex login still gives 401.
- **Glyph:** place the patrol pick; decide the cameo ground (cream or roster grey) and run the 2x crop review.
- **Next owner:** Sky (picks), then Forma (model), Glyph, Latch.
