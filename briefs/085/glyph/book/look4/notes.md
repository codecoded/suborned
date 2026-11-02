# The book's look, after looking at real games (brief 085)

Glyph, 2026-11-02. Sky: "Hmm. Thought B had some Promise. Look at likes of watchdogs and desperados and other games". Open `board.png` first; it shows all three side by side.

Mock only: no edits under `unity/Assets`, GDD or `text.json`. No image generator. Real type and data as `../notes.md`, same slot rule as `../look3/notes.md`, drawn by `make/` (`python3 l4_render.py`, `l4_sheets.py`, `l4_boards.py`).

"B" is ambiguous, so both are developed: **L** is round two's light page, **B** is round three's deck.

## Research

Reference boards, kept out of `briefs/`: `out/scratch/glyph/refs/refs-board-1.png` to `-4.png`. Other companies' images, reference only.

I opened 12 screenshots at full size (marked ●). The rest I saw only on contact sheets, so those readings are rougher. The catalogue is fan-compiled, and Steam shots are picked by the publishers.

| game | seen | what dominates | how much | moving between people and sections | how a person is shown | what stops it feeling like a web page | how its world comes through | many people |
|---|---|---|---|---|---|---|---|---|
| **Watch Dogs: Legion** | ● team, ● person, ● profiler | the 3D figure, posed, in the middle | moderate; a summary page, then a bio | word tabs on top; a grid of faces always on the left | full figure plus a face grid | the figure breaks the layout; big "NEW OPERATIVE" | London's look is in the figures; panels are plain tech blue | the team grid; a list of hundreds not seen |
| **Desperados III** | ● pause, ● mission info, ● loading; mission select on a sheet | pause: one big round map; mission info: the live world | very little | icon tabs on a small panel; a plain list | small painted portrait on the HUD | the world stays visible, or a drawn map fills the screen | the drawn map and frames, not the panels | fixed small cast. **No character or skills screen found:** the catalogue lists none, but I can't say the game has none |
| **XCOM 2** | ● abilities; others on a sheet | the figure on a round stage, in the base | dense panels both sides | back button; panels change | full figure | the figure and stage; angled panels | the base behind everything | soldier and unit lists are plain rows |
| **The Outer Worlds** | ● reputation, ● poster; others on a sheet | plain panels, figure at the right | moderate | two rows of word tabs | full figure at the edge | the figure; the gold frames | posters and loading art carry the retro future; menus don't | short lists |
| **Disco Elysium** | ● sheet; inventory on a sheet | a grid of painted cards, one enlarged | high, but ordered | icons at the foot | painted cards; a full figure on inventory | every card is a painting, and one comes forward | all of it, through the paintings | fixed set |
| **Phantom Doctrine** | ● agents; board and others on a sheet | panels over a dimmed room | busy | a list of faces; rooms along the foot | small portraits | little; the closest to our first mock | the room behind; corkboard elsewhere | a plain agent list, "7/25" |
| **Hades** | ● boons; others on a sheet | a panel, with the figure breaking over its edge | little | one choice at a time | a large painted figure | the overlap; heavy ornament | ornament everywhere | **codex screen not found** |
| Invisible Inc | sheet only | a large stylised agent, panel beside | moderate | not clear | stylised figure | the figure | strong single style | not seen |
| Atomic Heart | sheet only | flat red panel, picture cards | moderate | word tabs | no person shown | the poster-like art inside | Soviet retro future in a card's picture | not seen |

**Not found:** Persona 5 menus and confidant screen (Steam shows only play), Fallout's Pip-Boy (Steam shows other screens). Shadow Tactics' Steam shots have no menu. I didn't look at We Happy Few, BioShock, Sniper Elite, Hitman or Syndicate. MobyGames and the Game UI Database blocked downloads.

### What recurs
1. **The game's own person is the big picture.** Legion, XCOM and The Outer Worlds all do it, and Hades and Disco with paintings. Mine have mostly been a round face on a flat page.
2. **Faces stay on screen.** Legion's grid means the next person is one click from any person. Look3's three small faces were a weak version; the list shouldn't be the only way there.
3. **The panels are plain.** Style comes from what's in them or behind them. This suits our flat kit.
4. **Word tabs aren't the problem.** Legion and The Outer Worlds use them and don't read as web pages. The figure does that work, so "no tab row" wasn't the rule that mattered.
5. **A panel over the live world works** (Desperados' mission info, Legion's profiler). Legion's thin line joining person to card is the idea L takes.
6. **Retro future in other games lives in art, not layout.** The Outer Worlds' poster is a one-off picture, and its menus are plain. No game I saw gets it from a menu's shape.

### What doesn't suit us
- Legion's metadata ("Pathological liar", soup recipes): §09 rejects fields no rule reads. Its "Recent Events" is our "Between us", and that part is right.
- XCOM's and Phantom Doctrine's density: Sky's rule is less reading.
- Disco's and Hades' paintings per item: they don't scale to hundreds.
- Dark screens: this game is bright. Almost everything I saw is dark, so we have no direct model for a bright menu.

### Is the whole approach wrong?
**Partly, yes.** The slot rule holds up. But I've spent four rounds designing the frame, when the references put their effort into the subject. The retro future Sky wants is already in our street, and flat shapes can't replace it. Every option below does better simply by showing it.

## The options

| | L · Light page (recommended) | C · Close-up (second) | B · Deck |
|---|---|---|---|
| what | the real street in its own colours; a cream page with one big curve; the person's face overlaps its edge; the signal eye over them, joined by a thin line | the frozen scene camera on their real figure; a teal plate with one curve; sections on the curve; a large TURNED | real cards: the figure card, a facts card under it, faces dealt as cards, sections as piles, over the street printed dark |
| retro future in a thumbnail | yes, from the street | somewhat; the café | least; reads as a card game |
| calm | calmest | calm, but strong | some clutter; slight tilts |
| web-like? | closest, saved by the street and overlap | no | no |
| same game? | most | yes | least: the street is hidden |
| holds real facts at phone width | yes; scales and "She knows" need the crop | yes, same | yes, same |
| weakness | a page over a city may still be "a modal"; the door frames are duller | shows our models' limits | the second card has spare space; 12 facts get small |

### Why L
- It's the most retro-future because it keeps our own city on screen, in full colour, with the clock stopped.
- It teaches where the person is. A new player sees Inez on the street, tied to her card.
- It's the cheapest to build, since the world is already there.
- It keeps the painted faces Sky has already seen and adds nothing to the look's upkeep.

If Sky meant the deck, it's ready to judge: `board-4-deck.png`. It no longer looks like a modal. I'd try it with the real street behind it before giving up on it.

### What I'd be wary of
- **L:** with a generated mission, the person may be off screen or indoors. The view has to move to them first (FindPerson exists). Vale gets no eye on his page, because he's inside.
- **C:** real figures close up. Inez holds up at this size; her face is rougher than the painting. Okafor is dark at his door. **Vale's frame is a composite**, his model render on a blurred Office frame, because there's no close shot of him. A crowd figure (1.5k to 4.5k triangles) hasn't been tried this close, and it's the real risk. Upscaled here to 1.2x or more, so a real capture would be a little sharper.
- **Bust:** Okafor as a flat bust is weak next to the paintings. He needs a painted face in any option.

## Extensibility: same rule, now with fit rules
The `*-sheet.png` frames show each option holding Inez, the job, 12 facts, mostly "?", an agent, 8 people, 200 people and a new Cash section, from one content file with no positions.

Added this round, because real words broke the look3 rules:
- **Facts:** the size steps down until the longest row fits its column.
- **Ways:** signs wrap.
- **Track:** labels step down to fit.

Same gaps as look3: more than 12 facts, 9 to 24 people, and groups over 24 aren't drawn.

## Evidence
- Words per page (render script): 19 to 67 in all three. Inez 45, job 53.
- Phone: I looked at `board-2-light.png` and the frames at reduced size, plus Inez at full size in all three, and the reference boards 1 and 3. I didn't check every board end to end. Boards are 1000 px wide and 2300 to 2700 tall.
- No budget tool run: pages are full screen with the clock stopped, and `page.ts` is written for the briefing.

## Frames: what's real
- L: real play frames with the HUD painted out. Smudges remain where agents and bubbles were removed, for example around Inez's door. The small marks on the door and bottom edge are real HUD left in. The door frame's soft circle is the game's own occlusion cut.
- C and B: real scene captures, with subtitles painted out, plus the Vale composite above. Scout has a small smear low in frame.
- Times and facts are as `../notes.md`, with the same open points for Keel, and for Motive and Sky on showing trust.
- Illustrative: the 200's counts, the 12-fact Inez, Cash. Section pictograms are stand-ins. The orange is sampled from the café parasols and isn't in the kit's rule.

## Build size
Not tried in Unity; estimated from how the HUD and scenes are built.

| piece | size |
|---|---|
| Template, slots, fit rules | M |
| L: frozen live street, the curve, the line, ring and eye on a person | S on top |
| C: reuse the scene camera for a held shot; flat plate | M on top; more if crowd figures need work |
| B: card sprites, piles, tilts; rotated text checks | M on top |
| PoC, job and cell pages, in L | M in all |
| Art: Okafor's face, section pictograms | S |

Panel C and the guide still come first and don't depend on this.

## Sources
Screens from [Interface In Game](https://interfaceingame.com/) (fan-compiled):
- Legion: [team](https://interfaceingame.com/wp-content/uploads/watch-dogs-legion/watch-dogs-legion-character-4.png), [person](https://interfaceingame.com/wp-content/uploads/watch-dogs-legion/watch-dogs-legion-character.png), [profiler](https://interfaceingame.com/wp-content/uploads/watch-dogs-legion/watch-dogs-legion-hud-7.png)
- Desperados III: [pause](https://interfaceingame.com/wp-content/uploads/desperados-iii/desperados-iii-pause.png), [mission info](https://interfaceingame.com/wp-content/uploads/desperados-iii/desperados-iii-quest.png), [loading](https://interfaceingame.com/wp-content/uploads/desperados-iii/desperados-iii-loading-3.png)
- XCOM 2: [abilities](https://interfaceingame.com/wp-content/uploads/xcom-2/xcom-2-abilities.png)
- Hades: [boons](https://interfaceingame.com/wp-content/uploads/hades/hades-boons-of-hermes.jpg)
- The Outer Worlds: [poster](https://interfaceingame.com/wp-content/uploads/the-outer-worlds/the-outer-worlds-loading-3.png), [reputation](https://interfaceingame.com/wp-content/uploads/the-outer-worlds/the-outer-worlds-reputation-2.png)

Steam store screenshots:
- [Disco Elysium sheet](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/632470/ss_9125a718ee9ba85386ae5d4eb820f3266073fc97.1920x1080.jpg)
- [Phantom Doctrine agents](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/559100/ss_7c41882eb1632e24e664d4ee56c647b2fe540465.1920x1080.jpg)

Contact-sheet URLs are in `out/scratch/glyph/look4/iig-src.json` and `steam-src.json`, and the full-size set in `out/scratch/glyph/refs/sources.json`.
