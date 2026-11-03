# The book's look, round two (brief 085)

Glyph, 2026-11-02. Sky on round one: "Dunno, doesn't shout retro future to me". Dossiers and stained paper are dropped. Open `board.png` on the phone first.

Mock only: no edits under `unity/Assets`, GDD or `text.json`. Nine stills through the free Codex route, $0, logged in `paid/085-book/ledger.jsonl`. Refs: a real game frame with its HUD, the city painting, the street furniture sheet and the face. No real-type proof this round.

**Judge feel and layout only.** I gave a word list, so little is invented, but the art gets some wrong:
- $40,000 sits beside Call; it belongs with Helped twice.
- The dots don't match her numbers (Loyal 2, Afraid 1, Bitter 0, Brave 0).
- "Where and when" heads the timeline; it's one thing she knows about us. The four times need what happened beside them.
- The art offers Listen on the guard; in the game it's locked until Watch.
- "Cleared" and "Door 2:54" are missing, and the clock reads 0:29 at a moment that's really 2:10.

## The ideas
| idea | shouts retro future in a thumbnail? | clear? | same game? | buildable? | size |
|---|---|---|---|---|---|
| **Turntable** (recommended) | yes | good | yes: real street, real figure | yes, hardest of the three | L |
| **Light page** (second) | fairly | best | yes: the street stays in view | yes, easiest | M |
| **Tomorrow poster** | loudest | good | no: flat print against a rendered street | yes | M + portraits |
| Atom (rejected for this page) | yes | worst | yes | hard | L+ |
| Console (rejected) | says old car | good | partly | yes | M |

- **Turntable:** my addition. A world's-fair stage with sweeping canopy and fins, the person as their full game figure on a chrome turntable, facts on enamel plaques. Sections are round chrome buttons on the rim.
- **Light page:** a chrome compact throws a page of light over the frozen street, framed by fins. Sections are keys on the compact.
- **Tomorrow poster:** a brand-new flat screen print: huge type, starbursts, boomerangs, a face in a disc.
- **Atom:** chrome model, people on tilting orbits. It says atomic age and network at once, but the type lies on curves, the facts wrap round rings, and it's the hardest to read. It may suit a people overview once there are real networks, not this page.
- **Console:** a 1957 dashboard with dials for trust. Clear and fun, but it says old car, not tomorrow, and a dashboard has no reason to be in this game.

## How each holds up
| | Inez's facts at phone width | moving between sections | someone mostly unknown |
|---|---|---|---|
| **Turntable** | Large plaques hold the facts well. The dots and times are the tight spots, so they need the same room as a plaque. The face is small; see below | Chrome buttons on the rim; the stage turns to the next person | Works: "?" plaques stay handsome (`turntable-unknown.png`) |
| **Light page** | Best of all; rows and plaques are large | Keys on the compact, small until enlarged | Works, but gets closer to a web card (`light-unknown.png`) |
| **Poster** | Good, but the swooshes limit word length | Printed shapes at the foot | Not drawn. Loud shapes around "?" could look silly |

## Why the turntable
- **It's the only one that looks like an exhibit from this world and shows the game.** Every other idea is a panel over the street; this one is a place.
- **It answers the scale question.** Everyone on the street already has a 3D figure, so there's no painting per person.
- **It held up on every page I tried.** The job page puts the clock on the rim (`turntable-job.png`), and the cell page puts all four on stage (`turntable-cell.png`), which suits the guided start's walk through each agent.
- **Same data, no new rules.** It reads what the first mock's notes list, and the clock stops as before.

**What could go wrong:**
- **The face is small.** Faces matter here, and the figure's is far smaller than a portrait's. A closer camera, or a small face disc on the name plate, would fix it.
- **Real figures are plainer.** The concept's Inez is prettier than our model, and our crowd is 1.5k to 4.5k triangles with 1K textures. Close up, a crowd figure may not hold. **This is the first thing to test:** one still of our real Inez and a crowd person on a stage.
- **Busy edges.** Lots of chrome against a bright street. It'll need the street dimmed or blurred behind it.
- **It's a big picture.** The stage covers most of the screen; a mid-sized one may lose little.

## Rough size
Estimates from how the HUD and conversation scenes are built; not tried in Unity.

| piece | size | what |
|---|---|---|
| Turntable, stills | L | painted stage and plaques; each figure captured at build time and shown as a picture |
| Turntable, turning live | + M | a small lit 3D stage, shown over the street; real rotation and idle figures |
| Light page | M | one device sprite, a translucent shape, plaques |
| Poster | M, + portraits | flat shapes are simple sprites, but it needs a flat portrait per person |

For the turntable PoC, the job and cell pages come first as before. I'd build it with stills and add the live turn only if Sky wants it after seeing them. The earlier sizes for panel C and the guide don't change, and neither depends on this.

**If Sky picks a direction:** I make the real-type proof next, on a blank stage plate, with a real capture of our Inez.
