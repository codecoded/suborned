# Brief 073 · Atmosphere concepts: less shiny, bright and clean

Owner: Aster. Opened 2026-10-28. Sky: the world is "a little too shiny / bright / clean". "Perhaps we add some pastel / 1950s comic book style effect to the atmosphere? Get some more concept work done on our world that sets the tone."

Concept art first (CLAUDE.md, GDD §12/§15): look decisions come from concept art made from an open brief with the style contract's concept_prompt, shown to Sky as pictures. **No GDD, style-contract, grade or shader change for 073 until Sky picks from the board.**

## 073-A · Vela (concepts) · the board
- Tool: the Codex CLI image tool only ($0): `python3 -I tools/paid/paid.py codex 073-atmosphere <pass> --name N.png --prompt ... [--ref IMG]`, into paid/073-atmosphere/. No paid API calls.
- References: today's game shots (briefs/072/shots/g/), so the concepts are of our world; the 26 Sep paintings (briefs/008/concepts/) for the street; every prompt starts from contracts/style/suborned.style.json concept_prompt; people prompts carry no date.
- Directions (distinct; add one if a better one shows up): (1) pastel haze and soft air · (2) 1950s comic-book print: halftone or Ben-Day dots, ink outlines, flat cel shading, slightly off-register colour · (3) matte gouache, mid-century illustration · (4) lived-in, not showroom-clean, still optimistic.
- Per direction: the café square and the Records Office front by day at the play camera (high, about 38° down), one dusk or night, one wide skyline.
- Deliverable: briefs/073/board.html, self-contained (images embedded), grouped by direction, a one-line note each, and per direction what it would take in Unity (post effect, outline pass, material smoothness, grade, haze).
- Done-check: every direction has its frames; all images logged at $0 in the ledger; the html opens offline.

## 073-B · Glint and Redline · notes only
Glint adds a feasibility line per direction (lighting, grade, smoothness, haze, outline); Redline a rough frame cost per direction against 064 A5. Notes only: nothing is built.

Next: Aster shows Sky the board; Sky picks; then a GDD version and the build work.
