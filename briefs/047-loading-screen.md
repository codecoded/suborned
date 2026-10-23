# Brief 047: loading screen key art (concept)

**Objective:** key art for the loading screen. Breathtaking, cinematic, 16:9, the cell in the foreground of the Harbour District at shift-change light, with calm space left for a title and a slim progress bar. Sky, 2026-10-22: "a proper loading screen ... use image gen to create something breathtaking."
**Status (2026-10-23):** Sky picked **B** (`fix/B_after.png`, Forma's fixed take): "can try B, easily replaceable". The picture must be a drop-in swap: one image file plus a small layout file, no code change to replace it.
**Owner:** Forma · **Next:** Sky picks (done: B); Glyph lays out title and progress bar in the HUD style lab; Latch builds it.
**Done-check:** 4 compositions x several takes, 2560x1440 (3.7 MP, the model's size limit), cell faces and costumes on-model, spend under the $5 cap, every call in PROVENANCE.json.
**Evidence:** `contact_sheet.png` (all 31 takes, picks outlined), `face_check_AB.png` (reference heads vs picks), `picks/`, `takes/`, `PROVENANCE.json`, `gen.py` (generator; reuses the old store's Runware client like `tools/figures/portraits_gen.py`).

## Compositions and picks (`picks/`)
| | Composition | Pick | Alt | Title space |
|---|---|---|---|---|
| A | The cell on the deck above the plaza at dusk, looking out at the harbour, SpaceHQ with its atom crown, coral sky rail | `A_pick_A_t7.png` (faces turned back to the viewer, best read of the four) | `A_alt_A_t5.png` (wider, calmer, cell in profile) | A7 sky upper right; A5 sky upper left |
| B | Low street-level hero at the café by the Records Office, the four walking at camera, long backlit shadows | `B_pick_B_t5.png` (Enforcer leads, centre; most cinematic) | `B_alt_B_t8.png` (four abreast, cleanest faces) | open sky above centre-right; paving band below |
| C | Aerial of the district at sunset with the cell on a rooftop corner | `C_pick_C_t7.png` (low sun, liner, sky rail, atom crown) | `C_alt_C_t6.png` (calmer sky, cell at the right edge) | upper-left haze is soft but cloudy; C6 has the clearest sky |
| D | The moment before the Handover: guard at the door, waitress about to stumble, councillor's car, patrol robot, camera on its post, the cell waiting | `D_pick_D_t7.png` (largest calm sky, upper left, cell small and left) | `D_alt_D_t6.png` (tighter, Enforcer arms crossed) | upper left sky, the biggest empty area of the set |

My overall order for a loading screen: **D7 or A7** first (D7 for the story and the most room for a title; A7 for scale and faces), then B5, then C7.

## On-model check
Scout (undercut, earpiece, ivory hooded jacket, harness), Fixer (ponytail, ivory-and-black pinstripe halter dress, long gloves), Tech (tied dreadlocks, goggles up, beard, black wrap top with ivory sash and cyan piping), Enforcer (grey hair and beard, tinted sunglasses, black-and-ivory panelled coat, chrome collar, white boots) hold in every take, side by side with the refs in `face_check_AB.png`. Fixer and Tech sometimes lose small trims (Tech's coat tails read as wide trousers; Fixer's dress length varies); the faces never drift. Faces in A and C are small, so the likeness reads by hair, headgear and costume.

## Method
Seedream 5.0 Pro (`bytedance:seedream@5.0-pro`) via Runware, reference images: a lineup of the four approved turnaround fronts (`refs/cell_lineup.png` from `briefs/020/people/sheets/`) plus three or four of the 26 Sep paintings and skyline sheets (`refs/`: city hero, Records Office, Inez cutaway, mission block, Exposition Palace, Power Spire, saucer spires). Prompts follow the style contract (`concept_prompt` city and avoid blocks, people couture line; no era date on the cell); the full text of each is in PROVENANCE.json.
Rounds: 1 (takes 1 to 3, one of A) first prompt; 2 (takes 4 to 6) after fixing what round 1 showed; 3 (takes 7, 8) reseeds, and A7/A8 add "the Enforcer and the Fixer glance back over their shoulders".

## Cost
31 logged calls at $0.106 to $0.109 each = **$3.31**. One more call (A take 6) was started and killed after it stalled for over ten minutes and never returned: not logged, worst case +$0.11, so at most $3.42 of the $5 cap. A 3840x2160 attempt was rejected by the API (4.6 MP maximum) before billing.

## Issues and what I learned
- The progress-bar wording in my first prompt made the model draw a progress bar into the picture (round 1, bottom edge). Fixed by asking only for "a calm empty area for later overlay" (rounds 2 and 3 have none). Round 1 stays in `takes/` for the record.
- The crowd is the weak point: the model drifts to period dress (hats, 1950s dresses, suits) on bystanders in the street frames (B, D especially), even with "bareheaded space-age couture" in the prompt. D7 has a few small hatless-but-conventional citizens; B5 has none visible. If Sky wants the crowd cleaner, a retouch pass on the picked take (not a new brief) is cheaper than more takes.
- The patrol robot reads as a friendly round robot in several takes; its face is a dot-matrix or grille panel in A7, A8, B8 and D7. The art bible forbids cartoon eyes: D6's robot has round eyes (a reason D6 is the alt, not the pick) and so do B5's and B7's at small size; a retouch or a reseed of B fixes it if Sky picks B.
- The camera on a post and the councillor's car appear in every take as the quiet undertone; the camera is a modern box CCTV (the model's habit), not a 1950s chrome hood. Acceptable at loading-screen scale; a retouch could hood it.
- SpaceHQ's crown is an atom on a needle mast in all takes (as the GDD says); in D7 the atom crowns a saucer tower rather than the flagship, because the Records Office frame has no room for it.
- Output is 2560x1440, which is "2K or larger" but the model's maximum; for a 4K screen Latch should upscale or scale with the title overlay in front.
- Nothing here touches Unity, the sim or the tools. No commits.
