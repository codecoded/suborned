# Brief 033: live portraits (concept)

**Owner:** Forma (concept) · **Asked by:** Sky, 2026-10-21 ("portraits act realtime to the players' actions: hurt, being shot...") · **Next:** Glyph (legibility, how it sits in the roster) then Latch (wiring), after Sky picks.

## Objective
The four roster portraits change expression in real time with what happens to that agent. Concept stage: one 3x3 sheet per cell member, the same person in nine states, same framing as today's round head-and-shoulders portrait. Style contract `character` type used for the person (reference = the front view of each turnaround in `briefs/020/people/sheets/`); no date in the prompts; Seedream 5.0 Pro via Runware, gouache-and-marker finish.

## Files (`briefs/033/portraits/`)
- `scout_states.png`, `fixer_states.png`, `tech_states.png`, `enforcer_states.png`: reading order = the table below (row 1: calm, focused, noticed; row 2: under fire, firing, hurt; row 3: badly hurt, downed, success).
- `strip_64px.png` (real size, 64 px medallions, 4 rows x 9 states), `strip_64px_x3.png` (the same, 3x nearest, to look at), `strip_128px.png`.
- `PROVENANCE.json`: every render with service, model, seed, cost, sha256, reference and full prompt. The `*_t1`/`_t2` files are the raw takes; `tech_states.png` is take 2 (take 1 kept for the record, see issues). `refs/` = the reference crops.
- Generator: `tools/figures/portraits_gen.py`.

## Production set (second pass, 2026-10-21)
`briefs/033/portraits/final/`: 36 portraits, `<agent>_<state>.png` at 64 px (the roster portrait size) and `<agent>_<state>@2x.png` at 128 px, RGBA, transparent outside the ring (the drawn thin ring is kept inside the cut); agents `scout fixer tech enforcer`; states `calm focused noticed under_fire firing hurt badly_hurt downed success`. 512 px masters in `final/master/`. The faces centre and scale are the same in all nine of an agent, so swaps need no re-centring. The 72 px inset ground of brief 031 can take the same files scaled (128 px @2x downsamples cleanly).
Fixes: Tech's hair and goggles now sit inside the ring (retake with the head at half the ring height); the Fixer's downed frame is inside the ring (tilt capped); colour matches the lineup (neutral light-grey ground, cool neutral light, clean black, ivory and chrome; sepia gone). `strip_64px.png` / `strip_64px_x3.png` / `strip_128px.png` are rebuilt from the final files. Sheets `*_states.png` are the chosen takes (scout, fixer, enforcer: t3; tech: t4); older takes stay for the record.
Tools: `tools/figures/portraits_gen.py` (prompt v2), `tools/figures/portraits_export.py` (ring detection, clip, export; no model call).

## States and engine triggers
Signals are the sim's own (Suborned.Sim: `Gun`, `Op`, `Game` reads). `op` = the agent (`Sim.Op.Name`); `gun = GunOf(op)`; `Hz` ticks per second (`T(s)`). Priority top to bottom; first true wins. A state holds >= 1 s (`T(1)`) before it can change to a lower-priority one, so it never flickers; a higher-priority one takes over at once. Latch/Keel confirm the exact accessors; nothing here needs a new rule except where marked.

| # | State | Trigger |
|---|---|---|
| 8 | downed | `gun.Down` (health 0, downed; stays through carry/capture until the mission ends) |
| 7 | badly hurt | not down and `gun.Health * 3 <= gun.Max` |
| 4 | under fire | `gun.ShotByTick >= 0` and `Tick - gun.ShotByTick <= T(2)` (someone fired on this agent in the last 2 s; hit or miss), or a panic order is on with a hostile in reach |
| 6 | hurt | not down and (`gun.Health * 3 < gun.Max * 2`, or a hit landed in the last 3 s: `gun.Health` fell since the last tick) |
| 5 | firing | `gun.Drawn` and a live fire order (`gun.Order != ""` with `gun.Target` not down), or a shot fired in the last ~1 s (`gun.NextShot` just moved) |
| 3 | noticed | `AttentionStage(w, op) >= 1` for any security watcher `w` (the max over watchers; the `Attention(w).op == op` read gives the same answer per watcher). Not "inside a cone": a cone trigger would reveal an unlearned cone's edge (Glyph, brief 031 cues section 9). The face changes at the same instant the attention inset opens; stage 2 and 3 keep this face (the ring cue carries the stage) |
| 9 | success | a 2 s beat on a landed act of this agent's: `Note` events `takedown.out` / `takedown.dead` (Enforcer, unseen), `loop.on` (Tech), `call.*` (Fixer's call lands), a turn landing, or a Watch/Listen reveal completing. Only shown from calm or focused (never over a hurt or higher state). Needs Latch to map the landing events to the agent (event carries the actor); a new `Note` field is the only addition, if the actor is not already there |
| 2 | focused | `op.Busy != null` with verb watch, listen, turn or call, or a timed act running (the HUD progress is `BusyStart..BusyEnd`) |
| 1 | calm | otherwise |

Health baseline: when nothing above 6 is true and the agent is under 2/3 health, show hurt (6) or badly hurt (7), never calm. Carried agents are `downed` until mission end; a body-carried "restrained" or "knocked out" agent is not an op state in the sim and shows `downed`.

## What the sheets show, and what holds at 64 px
Same face, hair and costume in all nine for all four (checked side by side at 64 px). At real size the states that read are: downed (head slumps, eyes shut), badly hurt (ashen face, blood), under fire (head ducked, shoulders up), firing (bared teeth, head turned), success (a smile). Calm, focused and noticed differ mostly in the eyes and head angle and are close at 64 px; hurt vs. under fire are close too. So the face is not enough on its own at that size.

## Note for Glyph and Latch: stills or layers
Recommendation: **swap stills**, nine per agent (36 images), cropped to the round ground, with a ~0.15 s cross-fade. It is the form the concept came out in, needs no rig, and the faces stay consistent since each sheet is one generation. Layered parts (a fixed head with swapped eye/brow/mouth layers) would give smoother blends but cannot reproduce the head ducks and slumps that carry the strongest states; not worth it at 64 px.
Add world-style cues Glyph owns so legibility does not rest on the face alone: a thin ring tint or pulse for under fire and for noticed (shape or motion, not a colour only, per §15 signals), a health pip row, and a downed slash. Keep text off the portrait.
Production images need: bg-removed or ring-cropped 256 px stills, a consistent centre and scale (the sheets keep head position steady; crops by thirds work as is), and a check by Glyph at 64 and 96 px on the real HUD.

## Issues
- (fixed in the production pass) Tech's hair and goggles crossed the ring.
- Tech take 1 lightened the skin in "downed"; take 2 added a same-skin-tone instruction and holds it (pallor shown as an ashen cast).
- Enforcer's sunglasses hide his eyes in most states, so he leans on brow, jaw and head; the downed portrait lets them slip. Tech's goggles stay up in all states (Sky may want them to drop over the eyes when firing: a cheap add).
- (fixed) The first sheets were warm sepia; the production set matches the lineup's neutral colour.
- (fixed) Fixer's downed frame ran past the ring.
- Remaining: the ground grey differs a little between agents (each sheet is its own generation); Glyph may flatten it to one disc colour behind the cut-out. Tech's goggles stay up in all states.
- Blood and bruising are shown (kills are shown, §06); tone is graphic but not gory. Sky to confirm for the badly-hurt state.

## Note from Glyph (2026-10-21)
Drive the "noticed" portrait from the engine's `AttentionStage ≥ 1` for that agent, not from "inside a watcher's cone": a cone trigger would reveal an unlearned cone's edge the moment the agent steps in. The portrait then changes at the same instant as the attention inset (brief 031 cues, section 9).

## Sky (2026-10-21)
"Seems right I guess": nine stills per agent with a short cross-fade, and the blood level in the hurt/downed frames, are approved for production. Forma does the second pass (fix the ring overflows, match the lineup's colour), then Glyph adds the cues the faces can't carry at 64 px, then Latch.

## Glyph (2026-10-21): the cues the faces cannot carry
Board: `briefs/033/cues.html` (uses Forma's Tech finals; the other three agents use the same layer when their finals land). One vector layer over each 64 px portrait, one shape or motion per state: orbit arc (focused), ripple plus the attention eye badge (noticed; driven by `AttentionStage`, same badge as the brief 031 callout), spiked jittering ring (under fire), rays at the sidearm (firing), cracked ring with a 2 px shake and a flashing lost pip (hurt), dashed beating ring with pulsing pips (badly hurt), a slash with a grey face and empty pips (downed), a single ring sweep (success). Pips: 4 (Scout, Fixer, Tech) or 6 (Enforcer). Calm draws nothing; no added HUD at rest. Latch builds it; no choice left for Sky.
- **Sky (2026-10-21):** "not into the downed ones, and the focused ones. Redo downed ones for now and I can get a feel for the others in play." Forma redoes the four downed states (options for Sky); focused waits until Sky has played.

## Downed redo (Sky, 2026-10-21: "not into the downed ones, and the focused ones")
Only the four downed stills are redone for now; focused stays until Sky has played. `briefs/033/portraits/downed-options.png`: three directions per agent, plus a 64 px column. (a) eyes closed, still, head upright, no blood, desaturated; (b) slumped but clean, eyes half shut, mouth slightly open, cool blue-grey tint; (c) out cold, head resting, a thin diagonal band of pale light across the eyes and cheek. Raw rows: `<agent>_downed_options_t5.png` (prompt in PROVENANCE.json). Nothing under `final/` changed yet: on Sky's pick, the chosen frame is cut with `portraits_export`-style ring detection into `<agent>_downed.png` and `@2x`. Note (a) is the calmest at 64 px but the least "down" (it can read as a neutral face); (b) reads as slumped; (c) is the strongest in a crowd of faces but the light band hides the eyes on the Enforcer's glasses.
- **Downed pick (Sky, 2026-10-21):** option (b), slumped but clean, desaturated.

Sky picked (b), "and we saturate it". Read by the coordinator as desaturate: `final/<agent>_downed.png` (64) and `@2x` (128), master in `final/master/`, are option (b)'s pose with the colour drained to 18 % (a trace stays so skin and chrome read), cut with a radius refined from the ring's own edge profile (no pale fringe: rim brightness measured even, 90-150 of 255 all round). No new render (grade by code, `$0`). If Sky meant more colour instead, the ungraded (b) cuts are in `final/downed-ungraded/` (same names), ready to swap.
