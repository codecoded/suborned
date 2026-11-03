# The book's look, round four: designed, not generated (brief 085)

Glyph, 2026-11-02. Sky on the last round: "Too busy now and not easy to be extensible". Open `board.png`, then `board-2.png`, on the phone.

Mock only: no edits under `unity/Assets`, GDD or `text.json`. No image generator used. Flat colour, Jost, kit icons, the real portrait paintings and a real frame. Drawn by `make/` (`python3 render.py`, then `python3 board.py`). Facts and sources are as `../notes.md`; words are draft for Fable.

## The options
- **A · The pole (recommended):** a cream field cut by one great curve. A thin pole stands in the frozen street and carries each section as a round sign.
- **C · The HUD, grown (second):** the same template on the kit's dark teal enamel, with the signage hotbar for sections. It's the most consistent with what Sky already likes, but at full screen it reads as a dark app.
- **B · The deck (rejected):** a cream card with side tabs looks like a web modal. Taking the card edges away leaves A. `B-deck-inez.png` is the only frame.

## My honest read
None of these shouts retro future as loud as round three, and that's the cost of calm. A reads as mid-century graphic design first: the cream and teal, the big disc, a Futura-like type, one curve. Sky should say whether that's enough.

`A-pole-inez-with-starburst.png` shows one louder step: a starburst on the current sign and a printed offset behind the portrait. It says atomic age faster from a thumbnail, but with the curve it **breaks the one-gesture limit**, so it's a question for Sky, not my pick.

The pole's discs are a side navigation in a sense. They aren't a tab row, but anyone calling it an app wouldn't be wrong. Faces bleeding off the corner and the huge name are what keep it from a web page.

## The slot rule
**A page is data in nine named slots, in a fixed order. Empty slots take no room. The layout never knows what the content is.**

| slot | holds | rule when it grows |
|---|---|---|
| hero | a painting, the bust, or a pictogram | always available; a person without a painting gets the flat bust |
| name | one line | the largest of three sizes that fits; steps down with the facts |
| state | one short word, orange pill | one only |
| facts | pictogram + up to 3 words | 1–4 in one column, 5–8 two, 9–12 three; the hero shrinks one step each time |
| ways | signs | beside the facts if they fit, else under the hero |
| scales | word + dots | one line |
| knows | face or pictogram + words | one line |
| track | one evenly spaced line | latest six; the journal holds the rest. Events and the clock use the same slot |
| others | up to 3 small faces, one click away | top right |

- **Lists:** up to 8 people, big discs; above 24, one big number per group, and a click opens that group.
- **A new section:** one entry (pictogram, word, page data). The pole re-spaces itself.
- **Proof:** `make/content.py` holds the pages with no positions, and the same code draws them all. `A-pole-sheet.png` and `C-hud-sheet.png` show Inez, 12 facts, mostly "?", an agent, the job, 8 people, 200 people and a new Cash section. Nothing is placed by hand.

**Where the rule isn't proven yet:**
- **More than 12 facts:** the code cuts them off. The plan is a "+n" disc, not drawn.
- **9 to 24 people:** the smaller grid isn't drawn.
- **A group over 24** (89 clerks) needs its own answer, probably sub-groups or paging. Not drawn. It's the weakest part of the 200 case.
- **The pole fits about 6 signs** at this size. A seventh needs smaller signs. C's hotbar holds more.
- **Long words:** I tested names, not a long fact against a narrow column in three-column layout.

**At phone width:** Inez, the job and "?" read at a glance. On the full frame the scales, "She knows" and track labels (24–25 px at 1080) and the 12-fact page are small, so they're fine on a PC but need the crop on a phone. The board includes one.

## Budget
Measured from the render script. Pages open full screen with the clock stopped, like the briefing, so HUD coverage doesn't apply.

| page | words A / C | quiet share A / C |
|---|---|---|
| Inez | 44 / 45 | 74 / 75 % |
| Job | 53 / 54 | 77 / 74 % |
| 12 facts | 66 / 67 | 79 / 75 % |
| Mostly "?" | 21 / 22 | 84 / 81 % |
| Agent | 24 / 25 | 78 / 78 % |
| 8 people | 18 / 19 | 84 / 80 % |
| 200 people | 19 / 20 | 87 / 83 % |
| Cash | 22 / 23 | 84 / 81 % |

Quiet share counts 16 px cells with no ink, and that includes flat parts of a portrait, so it flatters. By eye it clears the one-third floor. I didn't run `page.ts`: it's written for the briefing, and the book would need its own version. The slot rule's caps above would become those checks.

## Limits held
No chrome, bevels, plaques, canopies, railings or scenery; no tab row, equal columns of rows or footer bar. One gesture per screen: the curve in A, the big circle in C. Unknowns stay "?", no key legend, no backstory, and the clock shows stopped.

## Build size
Not tried in UI Toolkit; this is from how the HUD is built. It's far cheaper than the last two rounds, because flat shapes and flowing slots suit it.

| piece | size |
|---|---|
| Template, slots and rules | M |
| Job + cell PoC, on that template | S to M |
| Person page | S, once C's data is in |
| Lists and groups | S to M |
| Frozen street printed in teal tones (one grab with a colour ramp) | S |
| Art | S: the bust, section pictograms (units, lovers and take are stand-ins) |

Later: the hero runs 640 px wide, and the paintings are 512 px, so they're soft on the Air's screen. Three paintings exist; everyone else gets the bust unless painted, so nothing depends on a portrait pipeline. The guard needs one.

## Recommendation
**A, the pole, as the template.** PoC: the job and cell pages, as before. Panel C and the guide still come first and don't depend on this.

If Sky says it doesn't say retro future enough, the next thing to try is the starburst frame, and then loosening the one-gesture limit by one, not a new concept.

## Assumptions
- Clicking a way (Call) closes the book and opens that person's ring, so the price still shows first. The book never gives an order.
- Illustrative: the 200 people's group counts; the 12-fact Inez, a later campaign with fields rules do read (ladder, ties, memory acts, replaced need), though the campaign isn't built; the 8-person list uses real cast names plus Milena; Cash is only an example section. Okafor and Zhou stay as the open name mismatch.
- The orange is sampled from the café parasols, and isn't in the kit's HUD colour rule. The frame is real, with its HUD painted out and printed down to four tones.
