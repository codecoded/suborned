# 009 · Crossings as this world builds them

**Status (2026-10-14):** concepts chosen; A and B starting; C waits for Latch's B6 runs (brief 008).

Goal (Sky, 2026-10-14): the crossings "should be more future sci… currently look standard zebra crossing". The world is an alternate history, not a forecast: "a 1950s world that discovered atomic power centuries ago" (brief 008, Era), so street technology is far ahead while its style stays 1950s: "Traffic lights would be something else, maybe floating, and same for road markings."

## Decisions (Sky, 2026-10-14)
- **The concepts in `briefs/009/concepts/` are the reference** (six frames from an open brief: the model designed the crossing, its signals and markings; prompts and hashes in `PROVENANCE.json`). Three earlier rounds were rejected: palette options for the landing slab, Qwen frames near-copying the Records Office painting, and a "1950s imagining 2000" round that came out as a styled zebra.
- **The floating signal "eyes"** ("I do like those eyes"): chrome orbs or small saucers hovering over the kerb at each end of a crossing, a round display on each face showing a walking figure, a thin light beam down to a glowing disc on the paving where people wait (`crossing-future-eye-2.jpg`, `crossing-future-play.jpg`, `crossing-future-play-2.jpg`).
- **Red is allowed on electronic displays** ("red is definitely ok for electronics displays"): the signal's stop pictogram may be red. An exception to the ΔE ≥ 20 signal-colour rule for small emissive display pictograms only, never a surface; the cones keep their own shape and motion cue. Owed: the next GDD version (§10, §15).
- **No zebra, no paint, no landing slab.** The crossing is light set flush in the road: glowing seafoam bars or rows of light dots in brass frames (`crossing-future-eye.jpg`, `crossing-future-eye-cafe.jpg`, `crossing-future-play-2.jpg`); brass dome studs along the kerb edge; seafoam inlay arcs in the paving where the crossing lands. Brass stays a frame line (SF-21).

## A · Vela · pieces
objective: the crossing's kit pieces in DecoKit, lean, at play-zoom budgets: the signal orb (chrome body, round display both faces with walk and stop pictograms as swappable emissive materials, hover height and the beam and halo disc as separate parts so Unity can light them), the kerb stud, the flush light-bar strip and a light-dot row in brass frames, the kerb inlay arc. Read the six concepts and pick one orb shape (sphere or saucer) with a reason; name the colours from the palette (seafoam glow, brass frame, chrome; red only on the stop pictogram).
done-check: candidates in `out/decokit-009/` with measured sizes and triangle counts, a sheet beside the concepts; nothing adopted.
evidence: `out/review/009-crossing-pieces.html`.
next owner: Splice (B), then Latch (C).

## B · Splice · generator
objective: crossings emit the new parts instead of zebra bars and landing slabs: light bars (or dots) across the carriageway at each crossing's existing strip, an orb at each kerb end and on refuges, studs along the kerb edge, inlay arcs; the `crossing` surface and the landing slab go. Everything placed from the plan, never by hand. Keep CD-1 (Handover keep-clear; an orb overhead is a canopy-class item, its beam and halo are on the ground), SF-20 (rewrite for the new parts), SF-21. Each signal carries a stable id so city life can switch it later.
done-check: `bun run plan`, Handover overlay, `bun test tools/world tools/plan`, `bun run world` exit 0; a count of crossings, orbs, studs; replay drift declared by cause.
next owner: Latch.

## C · Latch · Unity
objective: CrossingKit draws the new parts: emissive light bars, orb with its display (walk shown by default), beam and halo; rebuild; shots at play zoom and eye level beside `crossing-future-play.jpg` and `crossing-future-eye-2.jpg`; frame cost at the B6 stops (the orbs and glow must not cost the Air).
next owner: Sky walks a crossing.

## Later
- Pulse: signals switch walk and stop with traffic in city life (brief 005).
- Vela: the same world-tech test for the rest of the street (lamps, bus and tram stops, signs): 1950s style, centuries-ahead technology.
