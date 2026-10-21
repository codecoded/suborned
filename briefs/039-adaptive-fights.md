# 039 · Fights that demand attention

**State:** proposed, awaiting Sky. Owner: Aster. Next owners: Pulse (rival and response AI), Motive (numbers), then Keel, Waymark, Suture.

## Why
Sky, 2026-10-21: "As long as player has to be present and adaptive then how they play is fine, and they're not just doing only a few actions."

Waymark's check on engine 0.18.0 (run c027) found every Handover fight is a fixed recipe:

| route | orders (in the fight) | verdict |
|---|---|---|
| loud, loud-fast | 32 (12) | recipe: one timed input, locks within 0.5 s of the doors (37/48 robust with locks, 22/48 at will) |
| loud-fight | 34 (12) | recipe |
| A | 22 (1 panic order) | recipe, 48/48 |
| A-pressure, G, G-fast, G-kill | 17–21 (1) | recipe, 38/48 |
| B, B-block | 19–20 (1) | recipe, 45/48 |

- **Rivals in the loud routes:** the first shot (2:31) draws them in from the east street at 2:49. At 3:30 they stop at the Office's front door, facing away, and never act. "Rivals reach Vale" only counts the final-fight rivals, and the loud fight brings only police. That matches §07's loud line ("police… no allies"), but GDD §05 says the rivals want the same target.
- **Fight length:** the loud fight is decided in 4 s; quiet fights last 17–22 s. GDD §07 asks for 30–60 s.

## Proposals (GDD changes, Sky decides)
1. **Rivals join any fight they hear, going for Vale (Pulse):** a second front. Where they come from follows their seeded routine, and the Scout can learn it by watching.
2. **The response comes from where the report was made (Pulse):** the street nearest the phone that called it in, plus cordon units at the district's entry streets.
3. **Police press in (Pulse):** the café-wall police move up on the car instead of holding.
4. **Fight length (Motive):** reinforcement waves and ambush size tuned so fights last 30–60 s.

The test for each: the same orders no longer win regardless of what happens, and a player who watches and adapts does.

**Sky, 2026-10-21:** "Maybe. I need to play the game for a bit before confirming." Held until Sky has played build 0.2.32.
