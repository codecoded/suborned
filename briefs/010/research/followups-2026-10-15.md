# Follow-ups to the Dwarf Fortress dossier (Motive, 2026-10-15)

These are the primary sources I read to check the dossier (`dwarf-fortress.md`), with the points `df-for-the-suborned.md` relies on. Quotes are exact and kept short.

## 1. Tarn Adams, "Secret Identities in Dwarf Fortress" (AIIDE-17 EXAG workshop, pp. 22–25)
Source: https://cdn.aaai.org/ojs/12963/12963-52-16480-1-2-20201228.pdf (landing page https://ojs.aaai.org/index.php/AIIDE/article/view/12963). I read all four pages.

What the dossier already had is confirmed:
- **Three relationship states.** By secret identity only ("they would not recognize them on the street"), true name, and visual. In the visual state they "store the number of times they've met, the time of the last meeting, and any reputation information". The states combine "if the subject or an eyewitness links the visual identification to a name".
- **The game stays honest.** "Incidents involving the spy store the true identity of the spy, in order to keep the game honest, but the player never sees this data". Reports carry "the current identity (if known) and a visual identification (if seen)".
- **No false rumours.** "aside from the deception involved in the identity itself, no rumors in the game can be false, so we don't have to worry about contradictions".

New points the dossier lacked, and the ones I use:
- **A face is only an id.** "Visual identity profiles are as simple as possible right now— it just uses the ID number of the person in question. This doesn't respect appearance or clothing". Matching witnesses on clothing would make the justice system "much more interesting. Spies would have to concern themselves more with proper hairstyles and proper dress." Our use: a face is the cell member's id, nothing more. Disguise stays [L].
- **Aliases link only through witnesses.** "two identities cannot become permanently linked without a visual identifier or true name". They link only temporarily, through eyewitnesses who know several aliases, "until incidents time out (when some information is culled)". Our use: an alias without a face is weak evidence, which supports keeping face as the one identity state for now.
- **Opinion depends on context.** "Do you have a matching identity profile? How much do you know about the identity?" Friendship can outweigh known guilt: "even a known spy status cannot overwhelm positive feelings of friendship". Our use: the lie rule (a brave, helped contact covers for you) is the same idea, stated as one deterministic rule.
- **Lies cost.** "Secret identities are one specific form of lie, and that alone required significant work". There are 85 known edge cases. Our use: one lie type only, the lie rule's false lead. No general false rumours.
- **Readability.** "these secret identities aren't fully engaging from the player perspective". The player mainly notices them "when the game announces that one of their dwarven citizens has given up information to a now-departed traveler". Our use: the debrief has to report each give-up with its cause (§08 [B]), or the system is invisible.

## 2. Watch Dogs: Legion's Census
- **Game Developer, Tommy Thompson (Dec 2020):** https://www.gamedeveloper.com/design/how-watch-dogs-legion-s-play-as-anyone-simulation-works
  - Background people are "background fodder", deleted when far away. People the player engages are uprezzed "along with relevant family and friends".
  - Memories are "permanent" and "propagate to their enemies and friends". A hospitalised person changes their family's schedules.
  - Affinity is not tracked by area. The team "generates a reason for each opinion".
  - Some people hate DedSec so much that "you can never recruit them again"; they can trigger revenge missions on a cooldown.
  - Admitted limits: some bio details are writer flavour ("almost all of it, as much as we could possibly make real, is real"). Fully procedural missions were "hard to get that to the level of narrative quality". Characters sometimes come into scenes by "dice roll". The team had to avoid both "procedural soup" and stereotypes.
- **Ubisoft News, Christopher Dragert (Mar 2021):** https://news.ubisoft.com/en-us/article/4po3S9Pwp1YcgBmGPmQxAh/watch-dogs-legion-the-tools-that-built-london
  - There is a generation layer and a simulation layer, and "memories and affinity actually propagate across relationships".
  - Procedural generation "requires massive tuning to align the output with game-design goals".
- **GDC 2021, Dragert, "Census: The Systemic Backbone…":** https://www.gdcvault.com/play/1027018/Census-The-Systemic-Backbone-Behind. The talk itself is behind the Vault paywall, so I read only the abstract. It covers memories and recasting, recruitment and the relational database.
- **Criticism.** Reviews agree recruits blur together: you pick them by perk, not personality, and many traits don't land. Sources: [Wikipedia](https://en.wikipedia.org/wiki/Watch_Dogs:_Legion), [Inverse](https://inverse.com/gaming/watch-dogs-legion-review), [Push Square](https://pushsquare.com/reviews/ps4/watch_dogs_legion), [Den of Geek](https://www.denofgeek.com/?p=630710). The [PC Gamer](https://www.pcgamer.com/au/all-of-my-watch-dogs-legion-recruits-are-massive-disappointments) body text could not be fetched; only its headline was available.

## 3. Dwarf Fortress Wiki, Justice (current namespace)
Source: https://dwarffortresswiki.org/index.php/Justice
- I re-checked "Interrogating an innocent dwarf does not have any negative effects"; the only cost is "lost work time".
- "Witnesses will also sometimes make false reports to try and frame dwarves they have grudges against."
- The page does not say whether interrogation reveals only one link up the chain. The "one step farther up the chain" wording is from the 2018 dev log, not the shipped game.
