# Glyph · independent foliage appearance review · 2026-10-29

## Changed paths / disposition

Only this review was added. **Reject adoption; reject both proposed selections for a final Unity trial as supplied.** No A/B/C candidate currently earns another import/material round. This is an appearance decision, not a geometry or performance failure inferred from screenshots.

| Actual proposed selection | Trial decision | Reason |
|---|---|---|
| `civic/run-west-cafe/1` → `PlanterRun_4_B_Refine075.fbx` | **REJECT** | Calmer leaf colour does not fix the stacked leaf shelves or the inset/cut floral presentation. The modest play-scale gain does not justify importing this known unfinished selection. |
| `civic/civic-office-north/tuft-0` → `BedTuft_A_Refine075.fbx` | **REJECT** | Best of the isolated tuft studies, but its cream flower is crossed by a leaf spray; the canopy still reads as scalloped plates. At play size the gain is principally surface noise on a small mound. |

`civic/cyp-office-e` stays unchanged. No automatic substitution of PlanterRun A or C, no family approval by average. Geometry/sight gate stays **HOLD**; G-3a remains blocked at 59 > 58. No sim change to conceal meshes. Palms/topiary remain open and outside this correction.

## Evidence and appearance findings

Read AGENTS.md, CLAUDE.md, GDD §15, the current style contract, Vela's README/report/metrics/delta, and Latch's integration README. Used `view_image(detail="original")` on **36 adjacent full-resolution PNGs**, not embedded report thumbnails:

- All 22 final isolated studies in `../foliage-refine/`: six refined close variants; source/previous close for BedTuft_A and PlanterRun_4_B; both representatives source/previous/refined at play y30 and y210.
- `../latch-integration/`: paving and foliage versions of both trough-detail-y210 and office-plant-detail-y030; all four foliage play captures (café/office, y030/y210); paving café y030/y210 and office y210.
- Local references: `briefs/026/vela/testbed/cafe-prop-day.png`, `streamline-daylight-cafe.png`, and `briefs/075/oldtown/today-day-cafe-square-y030.png`.

**Close studies:** BedTuft_A source is an obvious smooth faceted dome with block blooms. Previous adds yellow/black mottling and inflated bead petals. Refined removes that harsh response and gives a more consistent civic green, but broad overlapping sprays make horizontal scalloped shelves. In `BedTuft_A-refined-close.png`, a green spray crosses the upper/right portion of the front cream bloom. The two salmon blooms are shallow badge-like discs. This is improved tone, not finished botanical form.

`BedTuft_B-refined-close.png` has foliage visibly cutting the pink petal faces near their centres; C's peach bloom is mostly buried. **Actual trough B also fails independently of the tuft names:** in `PlanterRun_4_B-refined-close.png`, counting shrubs from left, the fifth shrub's pale front bloom is interrupted by green at its upper edge; several rear blooms (notably third/sixth shrubs) read as partial strips inset behind the layered crown. B's two green tones are more coherent than previous, but the repeated mound/shelf rhythm remains. A trough retains that rhythm and disc flowers; C visibly buries multiple blooms under leaf plates. Neither is an approved alternate. There are no equivalent play-yaw comparisons for those A/C trough alternatives, or B/C tuft alternatives, in this evidence.

**Full play framing:** both yaw triplets show the trough as roughly a 150 px row, the tuft only about two dozen pixels wide. Refined preserves a tidier green mass and smaller flower marks; it does not acquire a substantially more convincing foliage silhouette. Previous's dark mottling is reduced, but source → refined is a modest gain, especially on the tuft. These neutral Cycles studies cannot establish Unity colour, lighting or scene readability.

**Actual Unity context:** paving → foliage trough detail shows the previous candidate replacing only one segment: yellow-edged, near-black textured mounds next to smooth bright-green source mounds. At café play y030 that segment becomes a noisy/darker stripe along the left boundary; at y210 it remains a small strip beside the road. The seafoam inlay junction and café door axis remain clearer, stronger cues. Refined material might reduce this discontinuity, but no refined Unity capture exists to prove it. The office detail frames the entrance, stairs, rail, banner and cypress, not a useful close proof of the selected north tuft; office y210 places the bed behind/alongside cypress in shadow. Do not pass the tuft on that detail camera's authority. People are hidden for these comparisons: separation from a 40–60 px moving character, its feet/facing and markers is **unproven**, even where door and inlay shapes read clearly.

**Reference comparison:** the historical `cafe-prop-day` also has coarse domes, so its existence cannot certify this foliage upgrade. `streamline-daylight-cafe` shows distinct pointed leaves, varied leaf directions, small blooms above a continuous planted edge, with the door and seating still legible. Refined's stacked circular sprays do not achieve that appearance. Different framing/light/layout prevents a controlled numerical quality comparison. The 075 `oldtown/` shot is useful historical civic context, not proof that refined assets were ever integrated.

## Cost and missing proof

Vela's proposal adds **6,356** plant triangles for trough B and **949** for tuft A versus actual Unity baseline: **+7,305**, with two proposed extra renderers and eleven plant material slots. This is lower than the previous proposal, still materially above baseline; slots are not measured draw calls. 80 rather than 48 cutout sprays can increase alpha/shadow work. No GPU-safety inference follows from fewer triangles.

Any later trial needs explicit review-only `DK_Leaf_Refine075_Civic[Dark]` materials: linear-light 75% original civic green + 25% full-resolution shrub RGB, source alpha threshold .5, roughness .85, and checked Unity specular/normal response. FBX alone does not transfer the Blender node graph; mapping to unchanged DK_Leaf_Shrub invalidates the colour comparison. Actual imported/placed 1 mm containment, UVs/normals, retained non-plant attributes and unchanged transforms/colliders/nav remain unproved for refined. Earlier imported bounds proof applies to earlier assets only. Motion alpha seams, shimmer, normals, shadow/overdraw cost, visible passes, orthographic LOD and target Air frame time remain unmeasured. No production adoption is approved.

Style-source mismatch for Aster: GDD §15 still lists the expanded 072 palette; `contracts/style/suborned.style.json` v0.24 explicitly withdraws it for 075. This review follows the winning style contract and existing civic greens, not the stale wider palette.

## One next step / owner

**Vela, via Aster:** correct the canopy arrangement on the existing proposed B trough and A tuft only: reposition/reorient the existing leaf sprays within the unchanged source envelope so they break the horizontal shelves and clear the existing bloom faces. Reuse the current texture, greens and blooms; no added density, decimation, new asset family or district redesign. Done-check: both close yaw30/210 views show intact blooms and no repeated horizontal leaf ledges, and full 1080p play yaw30/210 comparisons show a visible silhouette benefit. Return that focused correction to Glyph before spending Latch's Unity material/import round. Keep the current default-off baseline meanwhile.

Verification here was read-only inspection and source/report consistency review; no generator, Unity, model, build or performance test was run. No production edits, commits or paid spend.

Read-only Python verification: all 22 final render paths exist; SHA-256 matches metrics.json for all three recorded source scripts and both proposed FBXs. This confirms reviewed input identity, not rendering behaviour. `git diff --check -- briefs/075/recovery/foliage-appearance-review` exited 0 (new untracked review; not a behavioural check).
