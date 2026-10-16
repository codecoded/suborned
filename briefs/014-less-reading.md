# 014 · Less reading

**Status (2026-10-16):** opened. A (Glyph) and B (Fable) run in parallel; C (Latch) builds from both.

Goal (Sky, 2026-10-16, after playing route G): "Those huds are getting out of control again, taking up all the screen space." Asked what was too much: "It's the reading, it's too much." The systems and the planning stay. What changes is how they are shown: in the world, not in text. Hitman and Shadow Tactics show deep rules by letting the player watch people behave; we were showing them as Invisible Inc–style panels in real time.

**Mechanics are frozen.** This brief changes the HUD, the markers and the text only. The engine, the rulebook and the routes don't change, and replays stay identical.

## Rules (Sky approved them as "a good starting point")
1. **No sentences during play, except people speaking.** Dialogue stays, as short speech in the world: Inez, the guard, the cell's terse lines. Everything else is an icon, a number, a ring or a line.
2. **Rules show in the street, not on cards:**

   | today | instead |
   |---|---|
   | seen by / from behind | the learned cone, plus an eye mark on the target: open, shut, or ? |
   | wake time | a ring on the body that empties |
   | found → door held | a lock appears on the door when it happens |
   | witness temperament → report | a "!" over the witness and a dotted line to the phone they are walking to |
   | check-in marks | a clock pip over the guard, once his routine is learned |
   | feed acks of what you just watched | nothing |

3. **Choosing an order is one line per way,** e.g. `Knock out · 6 s · +4 · 👁 none`. Holding Alt shows the full breakdown. The debrief explains consequences afterwards.
4. **The feed shows story beats only** ("Inez called him.", "Body found.", "Police coming."). At most two on screen, gone in a few seconds. The history sits behind a key.
5. **A HUD budget, checked on every build:** at rest, at most about 15% of the screen is covered, and there are no sentences. The check works on screenshots.
6. **A new mechanic says how it reads without text** before it is built.
7. **A minimap** (Sky: "Minimap may be the key though. For example knowing where Vale is located is not obvious."). As in GTA, the map carries the persistent state:
   - the cell, Vale and the key people as blips;
   - learned cones and routes;
   - held doors, bodies and stashes;
   - reports in transit and arriving units.

   Edge arrows on the main view point to Vale and anything urgent. Told and learned apply: Vale's last known place has an age, and an exact position shows only while he is seen. Its look is of the period (a radar scope, a plotting board).
8. **The objective is one line** (GTA): a short imperative whose key noun is coloured to match its marker. It fades, and comes back only when the player strays. History moves behind a key.

9. **Who people are** (Sky: "no idea who the waitress is").
   - Stand-ins are tinted by job, and the people the cell knows get a pip over the head that matches their minimap blip.
   - The café's staff come off the door: five servers stood on the single `cafe-entrance` point all mission.
   - Sky: "only need 1 waitress outside at the cafe … but can have customers … people come and go throughout the day, shows the city being alive early on". One server outside (Inez in the Handover window); the rest work inside, undrawn. Customers come and go by their routines, and passers-by keep the street alive. Pulse does it by rule, through a new people run that Sky promotes, and Waymark re-checks the routes.

10. **Turning is private** (Sky, 2026-10-16: "seeing someone being turned isn't a concern. That's too much, would consider the conversation private, unless it's pressure"). A rule change made alongside this brief:
    - Ask, Pay, Help and Listen give onlookers nothing: no witness facts, no witness Trail, no Standing, no word passed on.
    - Pressure stays witnessable.
    - The approach's own Trail and price stay.

    Keel builds it as a rulebook flag per verb, then Waymark re-records the Handover. GDD §04 changes with the next version.
11. **Standing leans Hero** (Sky, 2026-10-16):
    - "if you help in a good way then can move to hero". The helped person tells their ties.
    - "Hero is kinda the default, if you're not seen then everyone assumes you're good (it's that kinda world)". Only bad acts that are seen or found move a district toward Menace.

    Motive writes the numbers, and Keel builds them. Motive's `briefs/014/motive-standing.md` is approved by Sky ("sure", 2026-10-16): districts start at +2, and Standing drifts back to +2 from either side when nothing moves it.
12. **The cell must start in view** (Sky: "can we move the start position of the cell - you can't see them! That's a very bad start"). The crew's start points in `mission.json` stand in a 1.2 m line at (1–5, −49.7), out of the opening view. Waymark moves them to a visible, plausible place, and the camera opens framed on the cell.

## Parts
- **A · Glyph (look and budget):**
  - redesign the play HUD to these rules: roster, clock and status, order plate, takedown and action cards, markers;
  - the world-marker set from the table;
  - the Alt layer;
  - a budget checker that measures HUD coverage and finds text blocks on a screenshot (Splice can help with the tool).

  Output: `briefs/014/glyph-less.md`, a mockup sheet of the same moments as `briefs/013/play/` before and after, and the checker.
- **B · Fable (words):** an inventory of every text the play HUD shows today, each sorted into one of:
  - keep as speech;
  - becomes a story beat;
  - becomes an icon or number;
  - moves to Alt;
  - moves to the debrief;
  - cut.

  Then the one-line price grammar for every order, and the story-beat list.

  Output: `briefs/014/fable-less.md`, and `text.json` changes, kept separate from the current text until C lands.
- **C · Latch (build):** build A and B. Take before and after shots of the same moments as `briefs/013/play/` into `briefs/014/play/`. Scenarios match and replay unchanged. The budget check passes.

Done-check: Sky plays route A and route G and says the reading is down to what it should be. Every play screenshot passes the budget. Replays are identical.
