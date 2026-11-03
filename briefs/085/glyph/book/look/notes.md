# The book's look: four objects and one real proof (brief 085)

Glyph, 2026-11-02. Sky on the first mock: "Think needs a much better visual Layout, not so web page looking". Contents stand; the look started again from pictures. Open `board.png` on the phone first.

Mock only: no edits under `unity/Assets`, GDD or `text.json`. Nine stills through the free Codex route, $0, logged in `paid/085-book/ledger.jsonl`. The proof is drawn by `make/proof.py`; the data sources are in `../notes.md`.

## Read the concepts for layout and feel only
The image generator invented the words and some facts. "Safe flat", "Our courier", "Courage", "Discretion", "Turn Vale", "Get the key" and the rest aren't in the game, and neither are those scales or actions. Its Inez wears a dress she doesn't own, and its guard is a period policeman. None of that is proposed.

## The four objects
| idea | what it is | quick to read? | holds real facts? | moves between sections by |
|---|---|---|---|---|
| **A · Case file** (recommended) | an open dossier on a table | yes: big face, name, then slips | yes, room to spare | cut card tabs at the edge; photos of other people clipped in |
| **B · Wall** | photos and notes pinned to an enamel board | slowest: everything competes | yes, but crowded | enamel buttons on a brass rail |
| **C · Magazine** | a printed illustrated spread | fast and the most striking | yes | printed tabs, turning pages |
| **D · Machine** (second) | a microfilm reader and card file | face fast, facts cramped | barely: one small card | drawers with handles |

- **A** is the only one that stays clear on all three pages (`concept-A-case-file*.png`). Slips can grow, shrink or go missing, which suits people who are mostly "?". The cell and job versions hold up.
- **B** shows the network, which is the game's idea, but Inez's two ties don't need a wall. Thread over slips gets harder to read with more people. Worth another look when there are real networks.
- **C** is the prettiest, but it depends on a large painting for each person, with the art filling half the spread. It suits a mission's hero, not hundreds of people.
- **D** feels most of this world, and the street behind joins it to play. But the film screen takes half the space, and the card that's left is too small for real type at phone width. Its job and cell pages (`concept-D-machine-*.png`) already squeeze. A good idea for a smaller moment, such as a person's first reveal.

## The real proof: `proof-inez.png`
Inez at 2:10, on a blank Codex plate (`paid/085-book/codex/plate-case-file.png`). Real type, real facts, kit icons and plates. The face is the painting the game's small portrait comes from (`briefs/063/cameos/inez_A.png`).

**It survives real words, and it's clearer than the concept. It's also plainer, and I'd call it worse as a picture:**
- The concept's slips have torn edges, tape, differing paper and stamps. Mine are clean rectangles with a shadow and faint grain.
- The concept's light falls across everything. Mine falls only on the plate, so the slips look laid over it rather than lying in it.
- The concept's portrait has a sunny street behind her, which ties her to the world. The painting has a blank background.
- Okafor's still is a soft crop of a game model and sits badly beside the paintings. His small face is also a model render.
- The left page's clip and stamp work best. The right page still reads as neat cards in a grid. It needs more overlap and one thing allowed to dominate.

**What would close the gap:**
1. An artist pass, not code: six to eight painted paper slips with real edges, plus clips, tape and stamps.
2. Light baked into each slip, in the same direction as the plate.
3. A portrait with a background for each key person, and one painted guard.
4. A higher-resolution plate. This one is 1672 x 941 stretched to 1080, so it's soft already and would be softer on the Air's 2880 screen.

**Two things I'd change whatever else happens:**
- **Too old.** The concept and plate read as a worn antique: foxed paper, a dark wood table. This world is abundant, optimistic and centuries ahead. I'd repaint it as crisp cream card on a seafoam enamel desk, with chrome instead of old brass. I'd try that before building.
- **The street is gone.** A and the proof hide it completely. That's fine with the clock stopped, but the game's look disappears. A slice of the frozen street at the plate's edge could keep the link.

## Kept from what we agreed
- **No key legend.** No invented backstory.
- **Unknowns stay "?".** A mostly unknown person's page isn't drawn here. In this idea it would be a face with fewer, emptier slips, which should suit it, but that's untested.
- **Never one right way.** The job page's ways stay as they were.
- **The clock stops,** shown by the plate at the top right, with a close button. Those are the only two controls besides the tabs and clipped photos.
- About 70 words on the page, by the mock's own count, down from 129. The facts are the same; the section heads, the job line under the name and the hundreds inset are gone or shorter.

## Build size, honestly
This is from what I know of how our HUD is built. **I haven't tried any of it in Unity.**

| piece | earlier estimate | now |
|---|---|---|
| Job + cell PoC | S to M | **M to L** |
| Adding the person page | S to M | **M** |
| Whole book | L | **L to XL** |
| Art, on top | none | **M** (see below) |

What's easy: overlap is just absolute positioning, which the HUD already uses. Rotation is a style on the element.

What's harder:
- **Paper can't be drawn in code.** Each slip is a painted sprite, with its shadow and light baked in, nine-sliced or fixed in size. Stretching a nine-slice smears torn edges, so slips likely come in fixed sizes, which limits what they can hold.
- **Composed pages are placed by hand.** The first mock's rows flow. Here every slip has a fixed spot and a word cap. A longer name or fact needs its own layout rule, and a generated mission needs slots that still compose. The layout audit needs new cases.
- **Empty states.** A person with few facts mustn't leave holes, so each page needs arrangements for what's known and unknown. This is the largest unknown.
- **Rotated type** needs checking at small angles for softness, and at 2x. Rotated hit areas need checking too.
- **Portraits.** Four people now, and a campaign needs hundreds at about 500 px. Fine at build time, but it's a pipeline and a cost, and until it exists a person without a painting needs a fallback. This is the scale question Sky asked about.
- **Page turns.** A hard cut or a quick slide for the PoC. A real page-turn animation is extra.
- **Budget.** `page.ts` is written for the briefing, so the book needs its own checks.

## Recommendation
Go with **A, the case file**, repainted cleaner and brighter. Keep the PoC to **the job and cell pages** as before, and add the person page after option C.

The look costs about a size more than the first mock. If that's too much right now, the panel and guide still come first and don't depend on it.

## Assumptions
- Facts and times are the same as `../notes.md`, with the same open points (Keel on the numbers, Motive and Sky on showing the trust scales).
- The fourth tab, "The journal", is drawn by me to match the plate's three.
- "Turned" as a stamp is new; words stay draft for Fable.
- Coral (#d9553f) and blush are picked by eye from the concept. They aren't in the kit or the style contract, so they'd need adding if this goes ahead. The kit's colour rule is for the HUD; the briefing page already has its own paper colours.
