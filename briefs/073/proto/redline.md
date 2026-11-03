# 073-B · Redline · frame cost per direction (2026-10-28, notes only)

All figures are **estimates** for the 15-inch Air M4, warm, 1920 × 1080, play zoom. Nothing here was run on the Air.

**Baseline.** 064 A5: warm mean ≤ 13.0 ms, p99 ≤ 16.7 ms. My phase-3 projection (briefs/072/redline/phase3.md §3) is **12.3 ms with 064 A3 items 1, 5 and 7** (range 11.0–13.7). Those three items are now in Latch's working tree: PC_RPAsset has `m_RequireDepthTexture: 0`, and TacticalShadows and HostFightLab are changed. They are uncommitted and not measured.
- Allowance after A3: 13.0 − 12.3 = **0.7 ms central**. The worst corner (13.7) has no allowance left.
- Until the Air sitting measures it, the rule is still **frame-neutral, ≤ +0.3 ms**.

**The model.** One full-screen read and write at 1080 (R11G11B10, 4 B/px) moves ~16.6 MB. At 120 GB/s that is 0.14 ms, about 0.15–0.25 ms in practice. A3's per-pixel items scale ×0.39 from 2880 × 1864 to 1080.

## The Studio column is noise above ~0.5 ms
Default and strong run **identical GPU work**. Print073.shader takes the same branches for both: ink runs whenever `inkLines > 0`, and all four halftone screens are computed whenever `halftone > 0`, with CMY mixed in by the uniform `halftoneColour`. Only uniform values differ. Strong's extra +1.8 ms over default is therefore not shader cost. The Studio is main-thread-bound, and the per-view editor grabs vary 9.7–21.0 ms within a single variant (print073.json `grabMsEditor`). Read the Studio deltas as "small, small, small", not as +0.4 / +0.9 / +2.7.

## Per direction
| direction | as prototyped or concepted | Air est. | done the cheap way | Air est. |
|---|---|---:|---|---:|
| **vintage print (lead)** | specular cap pass in HDR + print pass after post + the extra intermediate copy it forces; ink adds the depth texture | subtle **+0.4–0.7**; default = strong **+0.6–1.1** | grade (fade, warmth, highlight desat, ink-to-paper remap) folded into the existing LDR LUT (PC_RPAsset grading mode LDR, 32³; the uber pass already samples it): **0**. Matte chrome and glass by lower smoothness and reflection in the materials, not a screen cap: **0**. Paper grain + halftone from one baked tiling texture (RG8 256², 128 KB) in one after-post pass: **+0.2–0.35**. No ink | **0** (LUT + materials) to **+0.35** (with paper) |
| **comic book with outlines** | ink on every edge, cel shading, dot screen | depth-only edges (the prototype's Laplacian) **+0.6–1.1**. Clean comic lines need normals: a DepthNormals prepass re-draws every opaque, **+1–2.5** at play and more at tp-worst; inverted-hull outlines double the drawn triangles and draws of every outlined mesh | cel ramp in the lit shaders (ALU only, ~0) + the paper pass; lines only on figures and signals, never the city | **+0.2–0.4**; full outlines **+1.5–3.5**, over the allowance |
| **pastel haze** | soft air, lifted blacks, wider glow | — | DistanceHaze already fogs every shader (linear 90–350 m): colour and density changes cost **0**. Lift and soften in the LUT: **0**. Wider glow via A3 item 6 (bloom quarter) **saves 0.1–0.3**. Avoid a post aerial-perspective pass: it needs depth back, **+0.3–0.5** | **−0.3 to +0.05** |
| **matte gouache** | matte painted surfaces, soft light, painterly edges | painterly filter (Kuwahara or oil, 4 × (r+1)² taps at r 3–5) **+1.5–4** | lower smoothness, specular and reflection in the materials: **0**. LUT: **0**. Optional paper or brush texture through the same one-fetch pass: **+0.2–0.35**. No painterly filter | **0 to +0.35** |

Shared extras, any direction:
- **Painted cumulus sky dome:** a texture swap, 0.
- **A lower, warmer sun:** longer shadows put more casters inside each cascade. **+0–0.2**, not measured; the cascade spheres are the same.
- **Stencil skip for crossing heads, glows and cones** (Glint's open issue 1): one stencil test in the pass, ~0.

## The prototype's presets on the Air
- **Subtle** (+0.4–0.7) is the only preset that fits under 13.0 at the central 12.3. It takes most or all of the 0.7 allowance and breaks the ≤ +0.3 rule.
- **Default and strong** cost the same (+0.6–1.1) and would sit at 12.9–13.4.
- **None goes into an Air sitting as built.** The shader is found by name and is not in Resources, so `-print073` works only in the Editor. A player build would drop it silently.
- **Time the cheap production version instead, after Sky picks:** LUT + materials + the one paper pass, as one `perf-*.json` variant pair (off against on).

## A production version, the cheap way: ≤ +0.35 ms, frame-neutral at its low end
1. **The grade goes into the LUT** (free). The grade parts of the print pass are all per-colour, so they bake exactly. Night and evening grades need their own LUT values.
2. **The specular cap moves into the materials:** DK_Chrome, glass and enamels get lower smoothness, and reflection intensity drops per look (free). It stops clamping legitimate emissives at night (Glint's open issue 2) and drops one HDR full-screen pass (~0.15–0.25).
3. **Paper and halftone:** one 256² RG8 texture and one fetch, in one after-post pass enqueued before SignalsFeature so the signals stay on top (+0.2–0.35, including the intermediate copy).
   - Not URP's FilmGrain. It would cost ~0.03 inside the uber pass, but `PostProcessUtils.ConfigureFilmGrain` takes `GetRandomOffset2D()` every frame, so the grain swims and there is no halftone.
   - Folding the pass into the uber shader means forking URP's UberPost: package maintenance, not recommended for 0.2 ms.
4. **Ink lines held back:** no depth texture, so A3 item 5's saving (0.12–0.23 at 1080) is kept.

**Verdict:**
- **Vintage print, matte gouache and pastel haze can all be had at 0 to +0.35 ms** if the grade goes into the LUT and the matte finish into the materials.
- **Outlines are the only expensive part:** comic book is +0.6 at best, and up to +3.5 with normals.
- **The paper pass is the one real line item.** Budget it when the Air sitting gives the measured warm mean.

## Board lines (Aster, from the table above)
- vintage: as prototyped +0.4–0.7 ms (subtle) to +0.6–1.1 ms (default, strong) on the Air; the cheap production way (grade in the LUT, matte chrome and glass in the materials, one paper/halftone pass, no ink) 0 to +0.35 ms. The only one that fits the 0.7 ms left after A3 with room to spare.
- comic: depth-only outlines +0.6–1.1 ms; clean lines from normals or hulls +1.5–3.5 ms and they bring the depth texture back; without city outlines +0.2–0.4 ms, which is no longer comic.
- haze: −0.3 to +0.05 ms: existing fog, the LUT and a quarter-resolution bloom. Effectively free.
- gouache: a painterly filter +1.5–4 ms (over budget); done in the materials and the LUT 0 to +0.35 ms.
