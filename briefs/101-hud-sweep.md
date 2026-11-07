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
