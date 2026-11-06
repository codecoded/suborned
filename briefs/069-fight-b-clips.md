# 069 · Clip requirement: fight B (cover and pin)

**For the 069 clip stage.** Added by main, 2026-10-26, on Sky's call: "we're using mixamo currently in 069 so this might be fine". The game plays fight B on today's stand-in clips (Forma's, `tools/figures/clips.json`) until 069's fresh clip set replaces them. 069's clip set must cover the same states, sourced cleanly with no per-clip special cases, and pass the same reads.

## Why
Sky picked fight option B, Cover and pin (brief 039). The body carries the fight; the street gets at most seven kinds of mark (`briefs/039/glyph/b-final.md`). So these states have to read from the pose alone at play zoom: 40–60 px tall at 1080p, zoom 14 at 38°.

## States
| state | what the body does | today's stand-in |
|---|---|---|
| in cover | kneels behind cover, head, face and gun hands over it | `cover_kneel` (Pistol_Kneeling_Idle), `cover_in`, `cover_out` |
| fires from cover | rises part way out of the kneel, both forearms straight out over the cover line, a kick per shot | `cover_fire` + `kick_add` |
| pinned in cover | kneeling legs kept, upper body curled with hands over the head (a hump with no face), a flinch per round, a peek as the 2 s pin runs out | `cover_pin` + `flinch_add` + `pin_peek_add` |
| pinned in the open | cowers, flinch per round | `cower` + `flinch_add` |

## Reads it must pass (Glyph's done-check, at true size)
- A pinned man behind a 0.95 m hedge is never gone: at least 10 px tall and about 170 px² visible, even pressed against it mid-flinch.
- Pinned in the open is at least 15 px below standing.
- Kneeling and pinned behind cover differ by outline (upright with a face against a wide hump with no face) and by motion (the drop, the flinch, the peek).
- Check from the four quarter-turn headings, not just yaw 30.

## Where the detail is
- `briefs/039/forma/pin-poses.md`: clip choices, the counts, and section D (host layers, parameters and cross-fades; these carry over).
- `briefs/039/glyph/b-final.md`: the visual spec.
- `briefs/039/forma/authored/`: today's sheet and loops, the baseline to beat.

The engine data (pin remaining, hit and shot events, kneel points) and the host's additive react and peek layers don't depend on the clips, so they carry over unchanged.
