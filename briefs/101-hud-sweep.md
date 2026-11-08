# 101 · The HUD sweep: from lazy to the book's level

Owner: main session (as Aster), opened 2026-11-07 on `work/098-living-playbook`.

## Sky (2026-11-07, playing)
On the seal card ("District seals in 3:05 · Security is closing the exits. · Get Popovic into the car and leave." on a dark plate with a thin bar): "really hate this card and design. Lot of our HUD UI has become lazy looking. Do a sweep to see how we can massively improve it"

## What Sky likes, already in the game
- **The book's light page** (brief 088, `briefs/085/glyph/book/look4/L-light-inez.png`): a cream page with one big curve over the frozen street, painted faces breaking its edge, bold dark type with icons, round tabs, a timeline.
- **The start page** in the same look.
- **The debrief concept A** (brief 099), which Sky picked.

## Objective
In play, the HUD reads as part of the same designed game as the book. It is a retro-future voice with hierarchy and character, not a stack of generic dark plates and small sentences. It stays readable at play zoom and on the Air, and it stays fast.

## The sweep (Glyph)
1. **Inventory every HUD piece in play,** each with a frank critique: what is lazy, what works, and why. It covers at least:
   - the top bar (clock, cash, Trail, Alert pips);
   - the roster;
   - the minimap and its buttons (the new eye);
   - the feed and its cards: warnings like the seal, police coming, the why lines (brief 100);
   - the hold and aim pill and its plates;
   - the act ring and verb plate;
   - the speech bubbles;
   - the refusal pops;
   - the reporter's countdown plate;
   - the count badges;
   - the dial panel;
   - the Book key;
   - the end card, now becoming the debrief.

   Use real captures: the latest shots under `out/scratch/latch/` (`cones100`, brief 100's), `out/batch/shots`, and today's builds. Ask Aster for new HUD shots if a state is missing.
2. **Two or three directions for a HUD language** that belongs with the book, each with a clear idea, not a recolour. Each one mocked on the same key moments, with real type and real data, as the book's rounds were. A painted mood image from the Codex CLI at $0 may help a direction.
   - the seal warning;
   - police coming with its why;
   - the top bar mid-mission;
   - a hover and aim on a target;
   - a feed card;
   - the reporter's countdown.
3. **A board for Sky's phone,** with your recommendation and what it would cost to build: which pieces change, in what order, and what stays.

## Rules
- Concept art first: Sky picks before anything is built.
- The pillars hold:
  - readable > rich;
  - signals keep their colours and stay readable (cones red, guard amber, rivals violet, drawn unlit);
  - clickable, never key-only;
  - plain everyday words;
  - nothing in play that the player can't use.
- Performance: the Air at play zoom stays at 60 fps.
- Mock only in this round: no edits to `unity/Assets`, the GDD or `text.json`.

## Next owner
Glyph (the sweep and directions), then Sky. Latch builds the picked direction, piece by piece.

## Sky on round 1 (2026-11-08)
Sky was shown the three directions (A the page's edge, B enamel signs, C in the street) at rest, quiet, loud and aiming:
- "A just covers too much of the screen. And that's not even a busy example you're showing"
- "Why is their stats about 75% etc showing when firing?"

**Round 2 (Glyph), mock only:**
1. **Lean first.** Cover less than today's HUD at rest (4.8 %), and stay low at the busiest moment. Measure the way `tools/hud-budget` reads a capture.
2. **Mock the busiest real moment, not a calm one.** For example: the final fight with the seal running, police coming, a reporter on the way, agents firing under orders, a bubble or two, and the player aiming. Use real captures, and ask Aster for any that are missing.
3. **No percentages and no stopwatches on a shot.** A shot shows what the street will do about it (it goes loud: Alert, police), never the hit chance as a number. That's brief 098: the order card reads the scene, it is never a stopwatch. A non-numeric cue may hint at a long shot, if one is needed at all. The hit rate stays in the debrief's numbers.
4. **Two directions at most, both lean.**
   - C refined: in the street, made readable over any ground.
   - A minimal one: B's small enamel tokens only where something must sit in a corner, such as the clock and the seal.
   - A mix of the two is welcome.

   Each is shown at the same four moments plus the busiest, with a coverage table that includes today's HUD.

## Sky's rule on numbers in play (2026-11-08)
Sky, after round 1: "I agree" (no hit chance in play). Then: "Playing the game the complex strategy element is just little bit." and "Don't look at how long something takes because I don't have a choice with the action so it's irrelevant as a player. Only thing that feels time is important are the key events that happen".
- **No hit chance anywhere in play:** not the price's odds line, not the Alt breakdown, not the odds chip. The hit rate stays in the debrief's numbers.
- **No action durations in play:** seconds to down ("down in about 4 s", "45 s"), walk times ("Walk · 12 s"), an act's length, a price's time.
- **Time stays only for key events in the world:** the clock, the seal's countdown, police arriving, the rivals, the handover, a report about to land, a loop running out.
- **An order's price** shows money, Trail, Alert risk and what the street will do (it goes loud), and no longer the time. The GDD's "full price" rule changes at v0.40.0.

Latch removes these from today's HUD now. Glyph's round 2 mocks the new HUD by the same rule.


## Sky's pick (2026-11-08)
"Corner tokens": round 2's direction 2, Glyph's pick (`briefs/101/glyph/r2/`, `T-*.png`).
- **The tokens:** small flat enamel tokens only for what is glanced at constantly: the clock, the seal as an orange roundel, the news and the Book. Everything else is drawn on people and places in the street.
- **Screen share:** 3.0 % at rest and 5.1 % at the busiest moment, against today's 4.6 % and 10.0 %.

**Build order (Latch, each piece shot at 2x and checked on its own):**
1. The kit.
2. The clock token and the seal roundel. This retires the status pill and the seal card.
3. The news lozenge and the Book roundel.

Then stop for Sky to see a real capture. After that come:

4. The roster.
5. The scope.
6. The street layer.

Fable writes the new labels ("Goes loud") and retires the keys brief 101's removal left unused. The GDD's HUD kit (§15) changes once Sky has seen the real capture.

## Sky changes the pick (2026-11-08)
"Actually I think I prefer street labels": round 2's direction 1 (`briefs/101/glyph/r2/S-*.png`), not Corner tokens.
- **The look:** almost nothing in the corners. Labels are drawn where things happen, in outlined ink that reads over any ground, with faces on stems and rings at people's feet. Screen share is 2.5 % at rest and 4.1 % at the busiest moment.
- **Same build order:**
  1. The kit (keyline text, ink styles, icons).
  2. The clock and the seal in the street style.
  3. The news and the Book.

  Then a real capture for Sky, then the roster, the scope and the street layer. Latch and Fable were redirected mid-round.

## Sky refines the pick: a mix (2026-11-08)
"I think street labels top right but tokens bottom right perhaps".
- **Top right, Street labels' style:** the clock and the seal as outlined ink labels (`S-*.png`).
- **Bottom right, Corner tokens:** the news as an enamel lozenge with its badge roundel, and the Book as a roundel (`T-*.png`).

The "perhaps" leaves it to the real capture: Latch shoots rest, the seal with police, and the busiest moment beside both mocks' corners, with coverage.


## Steps 4–6 built (0.2.287), and Sky's word (2026-11-09)
- **The roster, the scope and the street layer are in.** Coverage matches the mocks: 3.0 % at rest and 4.2 % at the busiest moment, against today's 4.6 % and 10.0 %.
- **The reporter's ring:** Sky says "Fine" to keeping it cream on ink, not orange. The play grade turns an orange ground mark into the cones' red. The orange stays on the disc and its count.
- **Left:**
  - the news lozenge's why lines must fit the box, about 40 characters (Fable);
  - the next-event label gets its own limit (Glyph and Splice);
  - Inez's person card beside the lozenge (Glyph);
  - the pieces still in the old kit, such as window markers, pops and attention clocks (Glyph's mock, then Latch);
  - three lines the host plays at the wrong moment (Latch).

  Then one build.

## Round 3 approved (2026-11-09)
Sky: "yes" to Glyph's round 3 (`briefs/101/glyph/r3/`).
- **Build order (Latch):**
  1. Inez's card as a slip on her.
  2. The next-event row tagged `next`.
  3. Timed tabs for window markers and attention clocks.
  4. Pops.
  5. Done chips.
  6. The act ring.
  7. The callout.
  8. A sweep of the rest.

  Plus the lozenge's hover showing the card's full line. Then one build.
- **Splice:** the budget's `next` kind (a name of at most 3 words and 16 characters, one m:ss).
- **Fable:** the "Take control: T" pop names the click, not a key.

## Round 3 built (0.2.290), and taking control (2026-11-09)
- **Built:** Latch's 0.2.290 holds every round 3 piece. Coverage is within 0.1 % of Glyph's frames: quiet 3.71 %, the away window 3.76 %, Inez's slip 4.97 %, the act ring 3.86 %. Report: `briefs/101/latch/r5.html`.
- **Splice:** the budget sums two stacked speech bubbles into one line (r123-fight-ground). It should count each bubble.
- **The "Take control: T" pop** is gone, and no click takes control. Glyph's mock is at `briefs/101/glyph/tc/` (a round tab beside the selected agent's face, "Back to map" in the same slot while in control, and the GDD's eye over the driven agent).
- **Sky, on Esc as a way back to the map:** "esc is always pause menu". Esc never leaves the ground view; the button and T do.
- **Open with Sky:** a button and the eye, the button alone, or lab-only (T switched off for players).
