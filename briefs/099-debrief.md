# 099 · The debrief: the end of a game, told like the book

Owner: main session (as Aster), opened 2026-11-07 on `work/098-living-playbook`.

## Sky (2026-11-07)
"I also want to work on the missing debriefing / game over - that should be a like our book in terms of presentation, nice, visually appealing with explainers and stats"

## Today
- **The sim already reports a debrief** (`sim/Suborned.Sim/Debrief.cs`, GDD §08):
  - public security, from the final Alert and any shots;
  - the investigation, from the Trail and the named causes;
  - contacts and assets;
  - the route summary, and the interventions beyond the route;
  - whether the run went loud.
- **The run's log holds the rest:** shots and hits, knockouts and kills, witnesses and reports, Standing moves with their reasons, the fights and how the street judged them, the car leaving and who saw it.
- **The game shows only an end card** (brief 096 §6, `HostEndWords.cs`): a headline, one plain line from the engine's cause, and the story after the car. There is no page, no stats and no "why".

## Objective
When a game ends, win or lose, a full page in the book's look (brief 088: the light page, a cream page with one big curve over the frozen street, faces overlapping its edge) tells the player:
- what happened, in plain words, with the real cause;
- what it cost and earned, as stats, each with a one-line explainer of why;
- what the city will do about it.

The player leaves knowing why they won or lost and what to try next.

## What the page could hold (for Glyph to lay out; Fable writes the words)
1. **The outcome:** a headline and the story to the end (today's end card words, kept: "Plain-English end cards").
2. **The run in numbers:**
   - time, cash spent and left;
   - Trail added;
   - the highest Alert;
   - Standing before and after;
   - shots fired, hits and hit rate;
   - knockouts and kills;
   - people turned, and how;
   - cameras looped, disabled or shot out;
   - witnesses and reports made.

   Each number gets an explainer ("Trail +4: the guard's body was found (+2); the entrance camera recorded the Fixer's payment (+1); …").
3. **The cell:** each agent's card: health at the end, shots and hits, takedowns, and the moment that mattered.
4. **The city's answer:**
   - the investigation (active or cold, and the leads it holds: faces seen, the car);
   - the district's Standing on its Hero ↔ Menace bar, with the reasons it moved;
   - who saw what, and who may talk;
   - what happens between missions (arrests, statements, word of mouth).
5. **The timeline:** the mission's beats on one strip (rivals in, the chime, Popovic out, the fight, the getaway), with the player's key acts on it.
6. **Ways on:** Watch the replay · Try again (same seed or a new one) · Back to the start.

## Rules
- Concept art first (CLAUDE.md): Glyph makes 2 or 3 page concepts in the book's look, from the Codex CLI's image generation (`tools/paid/paid.py codex`, $0). Sky picks before anything is built.
- Words are everyday ("Plain-English end cards"). No form or report tone.
- Every number shown has its reason one hover or one line away, in plain words.
- Clickable, never key-only. The page gives no orders; its ways on are buttons.
- It reads the run and never changes it: a replay is untouched.

## Owners
- Glyph: layout and concepts.
- Fable: words.
- Motive: what the page reports, and the between-missions effects, from the GDD §08 debrief.
- Latch: building it on the book's slots.
- Chime: the page's sound.

## Done-check
- Sky picks a concept.
- The page shows at every end: win, each fail cause and the final fight. It is built from the run's data on the book's slot template.
- A real capture holds up beside the picked concept.
- A saved run replays to the same hash with or without the page opened.

## Next owner
Glyph (concepts), then Sky.
