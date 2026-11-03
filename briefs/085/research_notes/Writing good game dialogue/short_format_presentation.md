# How to write and present short read-on-screen conversations

Evidence labels used throughout:
- **[PUBLISHED]**: a figure or quote from a page I read.
- **[SNIPPET]**: seen only in a search-result summary. I did not read the page, so check it before quoting.
- **[MEASURED]**: I counted it myself. The sample is stated.
- **[ESTIMATE]**: my arithmetic from other figures.

No game listed in the brief turned up a published line-length cap or scene-length rule. Every per-line number for a shipped game below comes from my own counts of fan or third-party transcripts.

## Do a 12-word line limit and about 8 to 9 lines per scene look right against shipped games?

### Takeaway
Total scene length looks right: the samples average 73 words, and the median Hades conversation in my sample is 74. The 12-word cap is close to right as a normal ceiling.

The clearer weakness is uniformity. 88% of sample lines run 7 to 10 words, and most are two very short sentences. Shipped games vary line length far more. So 8 to 9 lines should be a ceiling that varies by scene rather than a target, and the rule to add is a spread of line lengths.

### Cited Findings

**My counts**

| Sample | Unit | n | Median words | Mean | p90 | Max | Share at 12 words or fewer | Share at 5 words or fewer | Spread (SD / mean) |
|---|---|---|---|---|---|---|---|---|---|
| The Suborned, `scenes.json` | line | 50 | 9 | 8.7 | 10 | 11 | 100% | 6% | 0.19 |
| Into the Breach, pilots | single line | 1,158 | 8 | 8.1 | 13 | 25 | 87% | 27% | 0.48 |
| Disco Elysium, third-party export | line | 18,220 | 10 | 12.8 | 27 | 71 | 59% | 29% | 0.75 |
| Hades, conversations only | speech turn | 1,117 | 25 | 24.4 | 45 | 63 | 28% | 15% | 0.61 |

| Scene length | Lines or turns per scene | Words per scene |
|---|---|---|
| The Suborned (6 scenes) | 8 to 9 | 68 to 77, mean 73 |
| Hades (323 conversations) | median 3, mean 3.5, p90 6, max 10 | median 74, mean 84, p90 142, max 281 |
| Disco Elysium export (low confidence) | median 11, p90 14 | median 128 |

- **[MEASURED] Hades turns per conversation.** 2 turns: 122. 3: 74. 4: 55. 5: 36. 6: 15. 7: 12. 8: 5. 9: 3. 10: 1. So 78% have 4 turns or fewer, and 1.2% have 9 or more.
  - Sample: the conversation sections of the fan transcripts for [Nyx](https://hades.fandom.com/wiki/Nyx/Quotes_(Hades)), [Skelly](https://hades.fandom.com/wiki/Skelly/Quotes_(Hades)), Achilles, Hypnos and Dionysus, at the same wiki's `/Quotes_(Hades)` pages.
  - One-speaker lines are excluded; only blocks with 2 or more turns count.
  - Median words per turn: 32 for the other character, 15 for Zagreus.
  - Hades is fully voiced, so its long turns are not read under the same conditions as bubbles. Characters were not randomly chosen; Achilles and Nyx are talkative.
- **[MEASURED] Sentence length.** Hades sentences: median 7 words, mean 8.2. Suborned samples: median 4, mean 3.9. 84% of Suborned lines have two or more sentences, and 38% have three or more. Computed from the same samples and [scenes.json](/Users/sky/Projects/suborned/briefs/085/fable/scenes.json).
- **[MEASURED] Into the Breach.** Sample: the dialogue tables for 5 pilots on the fan wiki ([Ralph](https://intothebreach.fandom.com/wiki/Ralph_Karlsson), [Abe](https://intothebreach.fandom.com/wiki/Abe_Isamu), [Harold](https://intothebreach.fandom.com/wiki/Harold_Schmidt), Bethany, Henry). These are one-off lines triggered by events, not conversations, so they compare with a line and not with a scene. Name tokens counted as one word.
- **[MEASURED, low confidence] Disco Elysium.** Sample: a [third-party export](https://huggingface.co/datasets/allura-org/disco-elysium-conversations-raw/tree/main) of 1,742 "conversations" and 233,724 words.
  - That is well under the [million-plus words reported for Kurvitz's writing alone](https://www.gamestar.de/artikel/disco-elysium-rollenspiel-fortgesetzt,3350814.html) [SNIPPET], so it is filtered or cut up in a way I could not verify. Per-scene numbers are unreliable.
  - The line counts include player options ("You": 5,013 lines). Excluding "You" and the listed skill voices: median 13 words, mean 14.5, n = 9,894. The exclusion list missed at least one skill voice (Electrochemistry), so this is approximate.
  - The export is marked all rights reserved. I counted it in a temp folder and copied nothing into the repo.
- **[ESTIMATE] Hades overall.** About 9 words per recorded line, from [22,000+ lines](https://gdcvault.com/play/1026975/Breathing-Life-into-Greek-Myth) [SNIPPET] and 300,000+ words with over two-thirds dialogue. The word figures come from a [weak mirror page](https://perusweet.pages.dev/posts/hades-contains-more-words-than-the-iliad-and-odyssey-put-together) [SNIPPET]. This fits the counts above if short barks outnumber the longer conversation turns.
- **[PUBLISHED] Ingold warns against the over-correction.** He warns against rigid rules like "all dialogue has to be five words long". — [Game Developer, Jan 2022](https://www.gamedeveloper.com/design/how-to-remove-the-frustration-of-reading-in-games)
- **[PUBLISHED] 80 Days.** Snippets of about 100 words or less. — [Game Developer, Jan 2022](https://www.gamedeveloper.com/design/how-to-remove-the-frustration-of-reading-in-games). Sunless Skies splits location text so players usually face two or three paragraphs at a time (same source).
- **[SNIPPET] Heaven's Vault's "paced dialogue".** Each line must be short, and the effect comes from how lines [stack or chain](https://complicationsensue.blogspot.com/2020/10) (a writer's blog).

**Project context, all read in the repo**
- The GDD limit is 10 words per bubble, while the text contract allows 12. Sky said "allow it for now" (auto-memory note `bubble-word-limit`, 2026-10-23).
- Sky's earlier sizing pick was bubbles of 3 s with Listen at 42 s, about 14 lines (memory note `dialogue-reveals`, 2026-10-23). So 8 to 9 lines is shorter than one thing he has already chosen, though that pick was for lines playing during the act, not a cutaway.

### Inferences
- **12 words: right as the usual ceiling, slightly tight as an absolute one.** The Suborned averages 5.27 characters per word including spaces [MEASURED], so 12 words is about 63 characters. That fits two 40-character lines, with room up to about 15 words [ESTIMATE]. A rare 13 to 15-word line would break no readability guideline, but it needs Sky's OK because the contract and GDD limits are his to change.
- **The cap is not what's holding the lines back.** No sample line exceeds 11 words, and 36 of 50 are exactly 9 or 10. The writer's "aim for 10" is acting as a target, and writers fill targets.
- **The likely fault is rhythm.** Other samples have 2.5 to 4 times the spread and 2.5 to 5 times the share of very short lines. A typical sample line is two fragments of about four words: "Three payments. I'll catch up. I always catch up." This breaks `writer.md`'s own rule ("No two lines in a row with the same rhythm") at the level of length. Over 8 to 9 bubbles it may read as a telegram, and as one voice even when the diction differs.
- **Suggested rule change (my suggestion, not a shipped standard):** keep the cap at 12 and drop "aim for 10". Require every scene to have at least two lines of 4 words or fewer and at least one of 11 or 12. Merge fragments into one sentence wherever the person would say it in one breath.
- **8 to 9 lines: right as a ceiling, too uniform as a norm.** The samples spend the same words as Hades across about 2.5 to 3 times as many turns. That suits a bubble format, and it is defensible because the word total is unchanged. But every scene is the same size whatever its job. A sensible range is 4 to 6 for a routine or repeated act and 8 to 10 for a Listen or the capstone call. Those numbers are my judgement, not data.
- Sky's "too short" complaint came before these samples and concerned 3 to 6-line in-game scenes. Nobody has yet tested whether the samples fix it. The counts show size is no longer the obvious fault, not that he will like them.

### Gaps
- I found no published line-length or scene-length figure or cap from Supergiant, Subset, ZA/UM, Obsidian, Inkle or Paradox.
- No counts for Pentiment, Citizen Sleeper, Kentucky Route Zero, Night in the Woods, Heaven's Vault, 80 Days conversations, FTL, Frostpunk, XCOM 2 or Crusader Kings. I found no usable transcript for them in this pass.
- I could not confirm how Hades breaks long turns on screen, so its numbers are per voiced turn, not per text box.

## What do developers and data say about reading fatigue, skipping and readable speed?

### Takeaway
Hard data on skipping is scarce and old, but it points one way. People skip when text is optional or low-stakes and read when it matters. Readability guidelines allow more text per bubble than these samples use. At 3 s per bubble the top of the 12-word range is already too fast, so hold time should scale with length.

### Cited Findings

**Reading speed**
- **[SNIPPET] Silent reading.** Adults average 238 wpm for non-fiction and 260 wpm for fiction, across 190 studies and 18,573 participants. Oral reading averages 183 wpm. Typical range for fiction is 200 to 320 wpm. — [Brysbaert 2019, Journal of Memory and Language](https://www.Gwern.net/doc/psychology/linguistics/2019-brysbaert.pdf). The figures came from a search summary and matched across several results; I did not open the paper.

**Subtitle guidelines**
- **[PUBLISHED] Netflix English.** Up to 20 characters per second for adult programmes, 17 for children's. 42 characters per line, two lines maximum. — [Netflix Timed Text Style Guide](https://partnerhelp.netflixstudios.com/hc/en-us/articles/217350977-English-USA-Timed-Text-Style-Guide)
- **[SNIPPET] Duration.** A third-party checker gives Netflix's limits as [5/6 s minimum and 7 s maximum per cue](https://www.bookwriter.vip/subtitle-reading-speed-checker). The Netflix page I read does not state these.
- **[SNIPPET] BBC.** 160 to 180 wpm and about 0.3 s per word, with the line limit reported as 37 or 38 characters. Secondary sources only, such as [this blog](https://md-subs.com/blog/tis1-reading-speed) and [this guide](https://convertaudiototext.com/blog/subtitle-styling-best-practices); I can't say which figure came from which page. The BBC page blocked my fetch, and the summaries disagree on the line limit.
- **[PUBLISHED] Game Accessibility Guidelines.** No more than 40 characters per line, two lines per subtitle (sometimes three), text no smaller than 46 px at 1080p. Speaker tagged by name and colour is cited as a good example. — [Game Accessibility Guidelines](https://gameaccessibilityguidelines.com/if-any-subtitles-captions-are-used-present-them-in-a-clear-easy-to-read-way/)

**Research**
- **[PUBLISHED, abstract] Film subtitles tested at 12, 20 and 28 characters per second.** Comprehension fell as speed rose, and faster subtitles meant more incomplete reading and less rereading. — [Kruger, Wisniewska & Liao 2022, Applied Psycholinguistics](https://researchers.mq.edu.au/en/publications/why-subtitle-speed-matters-evidence-from-word-skipping-and-reread/). Not a game study.
- **[PUBLISHED] Game eye-tracking study, N = 64.** About 73 to 87% of subtitles were not fixated while players controlled a herding game, even with subtitles at 12 cps. Performance did not differ across instruction modes. The authors call it exploratory: a custom game, a young and experienced sample, and manual control probably inflated skipping. — [Lan, Liao, Kruger & Richardson 2025](https://pmc.ncbi.nlm.nih.gov/articles/PMC12452717/)

**Skipping data**
- **[PUBLISHED] Mass Effect 2 telemetry.** 15% of conversations skipped, higher in non-critical moments such as hub worlds and much lower at the climax. Casey Hudson said that if "80% or 90% of the lines" were skipped they would have to rethink the digital acting. — [GameBanshee summary of an IGN interview, Sept 2010](https://www.gamebanshee.com/news/99618-mass-effect-2-statistics-interview.html); the 15% is also in [Game Developer](https://www.gamedeveloper.com/game-platforms/bioware-gathers-i-mass-effect-2-i-metrics-for-player-behavior-cues). This is 16 years old, fully voiced and acted, and from a story RPG. It is the only hard public skip figure I found.

**Developers on reading**
- **[PUBLISHED] Jon Ingold (inkle).**
  - "if you give people at least half an excuse not to read something then they won't."
  - "People just need the right format that suits the mood that they're in and then they'll read happily."
  - "The presentation is hugely important. The editing is hugely important."
  - He contrasts the high time cost of a 300-page book with a social feed, where each item is quick and a refresh gives another chance.
  - All from [Game Developer, Jan 2022](https://www.gamedeveloper.com/design/how-to-remove-the-frustration-of-reading-in-games).
- **[PUBLISHED] Ingold on looks.** "people definitely see text before they read it", and a back-and-forth rhythm ("you say something, the game says something back") makes it less intimidating. — [Game Developer, Aug 2014](https://gamedeveloper.com/business/-i-80-days-i-building-the-perfect-text-adventure-for-mobile)
- **[PUBLISHED] Hannah Flynn (Failbetter).** "Text is therefore metered out in very small portions, which means players focus on it intently and savor it a lot." — [same 2022 article](https://www.gamedeveloper.com/design/how-to-remove-the-frustration-of-reading-in-games)
- **[PUBLISHED] Robert Kurvitz (Disco Elysium).** "Twitter was our, kind of, competition. As a writer, you want to write snappy, fast, evolving text." — [Gamer Matters, Jan 2020, citing GameSpot's Audio Logs](https://gamermatters.com/disco-elysiums-text-box-design-is-inspired-by-how-we-use-computers-and-twitter/)
- **[PUBLISHED] Scott Benson (Night in the Woods).** "You don't want to put up too much for people to read." Twitter "teaches you how to be concise and funny and how to be rhythmic in your writing, I think." — [Engadget, June 2014](https://www.engadget.com/2014/06/17/night-in-the-woods-snappy-dialogue-inspired-by-twitter/)

### Inferences
- **The two quotes from Sky mostly don't conflict.** According to the auto-memory note `less-reading`, "It's the reading, it's too much" was his answer about the HUD on 2026-10-16: card text, rules and labels. "Too short" was about conversations. The note `dialogue-reveals` already says speech is the exception to less text, and Fable's card in the agent GDD allows no sentences in play except people speaking. Worth confirming with Sky, because `writer.md` presents them as one tension.
- **What remains is a budget, and the samples fit it [ESTIMATE].**

| Measure | Silent reading (238 to 260 wpm) | Subtitle pace (160 to 180 wpm) |
|---|---|---|
| Average line, 8.7 words | 2.0 to 2.2 s | 2.9 to 3.3 s |
| Average scene, 73 words | 17 to 18 s | 24 to 27 s |
| Help route: Listen, Help, Help, Call, 288 words, 33 lines | 66 to 73 s | 96 to 108 s |
| Pressure route: Listen, Pressure, Call, 218 words, 25 lines | 50 to 55 s | 73 to 82 s |

  - Those route totals assume every scene is played and no others are.
  - The player also presses advance 33 or 25 times. Key presses may feel like reading load even when the word count is modest, and none of the sources measures that.
- **The 3 s bubble is too short at the top end [ESTIMATE].** GDD §10 says "up to 3 s each".
  - An average 46-character line at 3 s runs at 15 cps, which is comfortable.
  - A full 12-word line, about 63 characters, runs at 21 cps, just past Netflix's adult limit.
  - The longest sample, 65 characters, runs at 22 cps.
  - Suggestion: hold each bubble for characters divided by 15 to 17 per second, with a floor of about 1 s, and keep click-to-advance. That gives about 3 s for an average line, about 4 s for a full one and about 1 s for "Where, then?".
- **The stopped clock is doing real work.** The Lan study suggests players barely look at text while controlling something. Pausing the mission removes that competition, so scenes can carry more than barks can.
- **Expect skipping to follow stakes.** The Mass Effect 2 pattern (hub high, climax low) suggests letting the capstone call run long and keeping repeated acts very short. The GDD's default of "first time with each person" already fits this. Keep skipping free: Ingold's "half an excuse" means a scene that needs forcing has already failed.

### Gaps
- I found no data on how much players read in tactics or strategy games, no public skip telemetry newer than 2010, and no study of reading speed for game speech bubbles specifically.
- The BBC guideline was not directly readable, so the figures are secondary. I did not read the full text of Brysbaert or Kruger.
- One page claiming skip and retention percentages (a travis-ci.com mirror) cited nothing verifiable, and I left it out.

## How do strong games stage a text conversation in a cutaway, and how does staging carry what the words leave out?

### Takeaway
The best-documented techniques give the text itself character (Pentiment), keep the conversation shaped like a feed or exchange (Disco Elysium, inkle), and change the picture on nearly every line (visual novel practice). Primary sources are thin. The most useful finding for this project is a conflict in our own documents: the writer is told people are at work, but the GDD stages both people idle and planted.

### Cited Findings
- **[PUBLISHED, press description] Pentiment's text reveal.** "the text appears immediately in outlined characters", then fills in with painterly brushstrokes, so the player can take in the whole comment at once and follow it as it fills. The writer calls this cognitively settling. — [PCGamesN, Sept 2022](https://www.pcgamesn.com/pentiment/obsidian-rpg-game-ux)
- **[SNIPPET] Pentiment's typographic voice.** With no voice acting, a person's fonts convey how the protagonist reads them. At GDC 2025 Sawyer called this a ["typographic voice"](https://www.jikguard.com/news/pentiment-director-explains-how-going-all-in-on-fonts-helped-elevate-the-medieval-detective-rpg); that URL returned an unrelated page when I fetched it, so the wording is unverified. Also [Gameranx](https://gameranx.com/?p=399534), and [PC Gamer](https://pcgamer.com/how-pentiments-hand-crafted-fonts-give-pen-and-ink-a-voice) on printers' text appearing as blocks pressed into the bubble. Fetching the PC Gamer page returned only its headline.
- **[PUBLISHED] Disco Elysium.** Kurvitz on placing the text bottom right: "If you're using a computer, [the bottom right side of the screen] is where you look 60 percent of the time" (his claim, not a measurement I could trace). The article describes the column as tumbling upward like a Twitter feed. — [Gamer Matters, Jan 2020](https://gamermatters.com/disco-elysiums-text-box-design-is-inspired-by-how-we-use-computers-and-twitter/)
- **[SNIPPET] Disco Elysium's confrontation.** Kurvitz says characters insult you to yank you back when you drift, and that the most compelling dialogue is personal. This came from a search summary of coverage of his GameSpot Audio Logs interview, and I could not pin it to one page ([RPG Codex's post on that interview](https://rpgcodex.net/article.php?id=11309) is a starting point).
- **[SNIPPET, secondhand] Per-line expression.** A forum post relays Christine Love's Visual;Conference 2017 talk as saying sprites change on nearly every line, often subtly, with about 100 variations for one character. — [Lemmasoft](https://lemmasoft.renai.us/forums/viewtopic.php?p=529537). The page refused my fetch and I did not find the talk itself.
- **[SNIPPET, anecdote] The failure mode.** One player review says monotonous presentation, with no movement, reaction or pause in dialogue, makes them click through as fast as possible. A developer thread points to Coffee Talk's subtle movement and cutaways to details. Both are from itch.io ([post 1](https://itch.io/post/12400169), [post 2](https://itch.io/post/12396616)); I did not read them directly. These are opinions, not data.
- **[PUBLISHED] Failbetter.** Moved the reading panel to the centre of the screen so it takes up the most space, which made it easier to read. — [Game Developer, Jan 2022](https://www.gamedeveloper.com/design/how-to-remove-the-frustration-of-reading-in-games)
- **[PUBLISHED] The closest cousin, Tactical Breach Wizards.** No voice acting, simple cutscene animation, skippable. Much of its dialogue has "two characters facing each other on either side of a door", using the gameplay assets. — [Game Developer, Aug 2024](https://www.gamedeveloper.com/design/let-s-compare-and-contrast-the-narrative-design-of-tactical-breach-wizards-and-into-the-breach)
- **[PUBLISHED, repo] Current spec.** Hard cut, profile two-shot, slow 8% push-in, both people "set to idle, facing, feet planted", one bubble at a time for up to 3 s, click or Space advances, Esc or Skip ends. — [Agent GDD §10](/Users/sky/Projects/suborned/GDD/the-suborned-agent-gdd-v0.33.0-2026-10-30.md)

### Inferences
- **Fix the conflict between `writer.md` and §10.** The writer is told "People are at work while they talk. Plates, tables, the wheel, the clock", but the scene shows two idle figures. Words then have to carry what a picture could. A small action set would do much of this: glance at the tables, set down a plate, check the watch, look away, step back. The scene changes nothing the engine records, so this is cosmetic, though it is real animation work. Treat it as a question for Glyph and Forma.
- **Mark who reacts, not only who speaks.** The listener's reaction is what's missing from a one-bubble format. Staging can carry the turn: Inez looks up at "When I telephone" and looks away at "All right". The visual-novel advice points this way, though it comes from 2D sprites and is secondhand.
- **Let staging replace lines.** Candidates in the samples:
  - "He's looking again" could be a cut to Okafor looking.
  - "People can see this door" could be Inez's glance at the door.
  - A stamped card could be a held prop.
  - GDD §10's "never one person for the whole scene" still allows a one-beat insert. Note that `writer.md` puts Okafor on the Office door 75 m away, so an insert would be a separate shot and would need checking against what the player is allowed to know.
- **Avoid a slow letter-by-letter reveal.** Pentiment's whole-line-at-once approach respects fast readers, and Ingold says people judge the text's size first. No study compares the two methods, so this is a preference with a reason, not a finding.
- **Consider giving each person's bubble its own look.** Name and colour is the accessibility baseline. Pentiment shows that lettering can also carry class and temperament, which may suit Fable's "every line must sound like one particular person". Caution: the 46 px accessibility figure limits how decorative or small this can be.
- **Consider leaving the previous line faintly on screen.** A strict one-bubble view means an answer can lose its question, especially in a fast read. The Disco Elysium feed keeps context visible. It adds on-screen text, so it would need trying in the cutaway.
- **Use the `learned` card as the clarity backstop.** Sky's test is that the player knows what happened. A card after the last line can state it, which frees the last spoken line to stay in character. The Scout line "Bureau debt. The door guard's her man. Two ways in." and the card "Inez owes the Bureau. She's seeing the door guard." currently say the same thing, so one may be enough. Try it on Sky before cutting, since he asked for explicit clarity.

### Gaps
- I could not read primary sources for Hades' or Night in the Woods' staging, portraits or text animation. Kasavin and Korb's [GDC 2021 talk](https://gdcvault.com/play/1026975/Breathing-Life-into-Greek-Myth) and Benson's [GDC 2018 talk](https://80.lv/articles/gdc18-the-design-of-night-in-the-woods/?amp=1) are the next places to look.
- I found nothing usable on Kentucky Route Zero's text presentation.
- I found no controlled evidence on player advance against auto-advance, or typewriter against whole-line reveal, and no guidance on auto-advance tuning beyond the subtitle figures above.

## How do tactics and strategy games deliver narrative between actions without breaking the flow?

### Takeaway
There are two documented patterns: very short lines tied directly to what just happened on the board (Into the Breach), or short skippable character vignettes in the gaps between fights (Tactical Breach Wizards). The Suborned's stopped-clock scene at the moment an act lands is closer to the second, with the first's tie to game events. For the other strategy games named in the brief I found little or nothing on method.

### Cited Findings
- **[PUBLISHED] Into the Breach's delivery.** Sparse. Zone leaders give mechanical context and comment on success or failure. Pilots react to damage, kills and protecting civilians, and those barks hint at backstory and the wider world. The author also notes that the story ends without a real payoff, and that a bark approach is cheaper than plot. — [Game Developer, Aug 2024](https://www.gamedeveloper.com/design/let-s-compare-and-contrast-the-narrative-design-of-tactical-breach-wizards-and-into-the-breach)
- **[PUBLISHED] Tactical Breach Wizards' pacing.** Vignettes between missions give breathing room, and the author says he wants to continue the story, "I don't want to play the mission just to see the next puzzle." — [same article](https://www.gamedeveloper.com/design/let-s-compare-and-contrast-the-narrative-design-of-tactical-breach-wizards-and-into-the-breach)
- **[PUBLISHED] Tom Francis on repetition.** To keep jokes fresh he skipped "dialogue sequences while testing gameplay" and left time between writing and reviewing. On rereading: "I'd forgotten most of the jokes", so "if one makes me laugh then I know it was a great". — [Game Developer, Aug 2024](https://www.gamedeveloper.com/design/tactical-breach-wizards-shows-how-strategy-games-can-tickle-the-funny-bone)
- **[PUBLISHED] Narrative as reward.** Kasavin: "It was an explicit goal of our early development, to take the pain out of dying and having to restart". The article describes story hooks carried past death like gear, and reactivity as a long-standing goal. Not a tactics game, but a game where story falls between play actions. — [Game Developer, Feb 2020](https://www.gamedeveloper.com/design/how-supergiant-weaves-narrative-rewards-into-i-hades-i-cycle-of-perpetual-death)
- **[SNIPPET] Into the Breach's line system.** A Q&A repost describes writing from mission parameters and gameplay calls, with [name tokens and exclusion rules](https://steemit.steemapps.com/gaming/@badastroza/interesting-people-27-chris-avellone-on-into-the-breach) so a pilot doesn't cheer a kill on the turn another dies. The fetch hit a loading wall, so this is unverified. The wiki's event tables (`Death_Main`, `Death_Response`, `Gamestart_PostVictory`) [do show the event-keyed structure](https://intothebreach.fandom.com/wiki/Ralph_Karlsson).
- **[SNIPPET] Frostpunk.** Marta Fijak said its impact came [mostly in reflection after play](https://www.pcgamesinsider.biz/news/67933/11bits-marta-fijak-empathising-with-500-people-is-hard-in-frostpunk/) rather than in-the-moment empathy. 11 bit's scenario goal was [hard moral choices through play](https://www.invenglobal.com/articles/6528/igc-2018-empathize-with-numbers-the-process-of-frostpunk-scenario-writing). Nothing on text length.
- **[SNIPPET] XCOM 2.** A Firaxis producer says they [doubled down on narrative](https://www.gamespot.com/articles/xcom-2-how-firaxis-crafted-a-superior-sequel/1100-6434134/). No details on delivery.

### Inferences
- **Timing is the strongest thing in the current proposal.** The scene plays when an act lands and the outcome is known, so it is a payoff the player earned. Hades treats story as a reward between attempts, and the Tactical Breach Wizards author wants more story because it feels like a continuation. Both suggest the same placement. It does change the current GDD rule (at most 3 opening beats, no outcome), which `writer.md` says is Sky's call.
- **Tie the opening to the order just given.** Into the Breach's lines work because they answer the board. Scenes should open on the thing the player ordered, so no setup is needed.
- **Repetition is a design risk here.** Replays from a saved run, retries and seeded new games bring back the same scene. "First time with each person" handles this per person. Francis's skipping while testing is a useful habit for Sky's own review: read a scene cold, not after ten runs.
- **Into the Breach is not a model for scene length.** Its one-liners work because the game asks little of them. The same article calls its story's ending a let-down. Sky explicitly wants the player to understand the full conversation, so its line craft is useful and its overall quantity is not.

### Gaps
- I found nothing on method or lengths for Crusader Kings 3. Paradox's [event scripting](https://admin-forum.paradoxplaza.com/forum/developer-diary/crusader-kings-3-dev-diary-30-event-scripting.1397140) and [lifestyle events](https://admin-forum.paradoxplaza.com/forum/developer-diary/ck3-dev-diary-10-lifestyle-events.1312112/page-2) diaries exist, but the search showed no word guidance and I did not read them in full.
- FTL's event text, XCOM 2's radio and between-mission delivery, and Justin Ma's or Matthew Davis's own comments on story were not found in this pass.
- Search results for Citizen Sleeper showed nothing on length or presentation, and I did not read the [Game Developer piece](https://www.gamedeveloper.com/business/how-citizen-sleeper-was-inspired-by-tabletop-rpgs-and-gig-work) in full. A search summary quotes Martin saying prose gives descriptive space for interiority that dialogue doesn't; I could not pin that to one page, so it is unverified.

## How do you get character across in one or two short lines?

### Takeaway
The shipped examples get voice from a verbal habit, from a person's own angle on the same event, and from contrast between long and short turns. Nearly all of this is demonstrated in the lines rather than stated by developers. Avellone's pilots are the best-documented case, and no source I read explains the craft at the level of the line.

### Cited Findings
- **[PUBLISHED] Avellone on character.** "Ralph's a vet who's crumbled psyche-wise, so some of his fighting skills have atrophied" and "People mistrust Gana because on the surface, his behavior is strange for a robot". Character is planned as a condition, then written as behaviour. — [Kotaku, April 2018](https://kotaku.com/into-the-breach-tells-its-story-through-its-characters-1824159682)
- **[PUBLISHED] Example lines.**
  - Isaac, who stutters: "S-s-so many deaths, and I'm b-b-back as if nothing happened… w-w-will there ever be an end to this?"
  - Gana, on repair: "offending parts have been executed."
  - Both from [Kotaku](https://kotaku.com/into-the-breach-tells-its-story-through-its-characters-1824159682).
- **[PUBLISHED, fan wiki] Ralph's angle.** "This timeline'll be a Vek graveyard." "Knew this timeline would be my last..." "Don't forget what I taught you..." These are 6 to 7-word lines where every word comes from his view as an old hand. — [wiki](https://intothebreach.fandom.com/wiki/Ralph_Karlsson)
- **[SNIPPET] No canon, only snippets.** Avellone says players assemble the world from [snippets of conversation](https://steemit.steemapps.com/gaming/@badastroza/interesting-people-27-chris-avellone-on-into-the-breach). Unverified, for the reason given above.
- **[PUBLISHED, fan wiki] Hades' contrast.** Nyx's first scene runs long turn, long turn, long turn, then Zagreus: "Yes. We are." and Nyx: "Then go." Her repeat lines are as short as "You have returned." and "Hmm." — [Nyx quotes](https://hades.fandom.com/wiki/Nyx/Quotes_(Hades))
- **[PUBLISHED] Tom Francis's comedy.** His jokes aren't built as setup and punchline but as "being funny along the way". — [Game Developer, Aug 2024](https://www.gamedeveloper.com/design/tactical-breach-wizards-shows-how-strategy-games-can-tickle-the-funny-bone)
- **[PUBLISHED] Benson.** Concision and rhythm together: "concise and funny and... rhythmic". — [Engadget, June 2014](https://www.engadget.com/2014/06/17/night-in-the-woods-snappy-dialogue-inspired-by-twitter/)

### Inferences

Techniques for very short lines (my synthesis, shown against the samples):
1. **Give the person one verbal habit that survives truncation.** Zhou prices things and that works: "Heavy. Spell it out. I've been paid vague before." Inez's "Who's looking? Nobody's looking." also works, though the writer should watch that the habit isn't only repetition.
2. **Make the speaker's angle do the work, not an adjective.** Gana's "executed" tells you more than "cold robot" would. One unexpected word from the person's trade or fear can carry a line.
3. **Contrast length.** A short answer lands only after something longer. The Nyx scene does this and the samples mostly can't, since nearly all lines are the same size. This also points to the rule proposed above.
4. **Prefer one sentence with a clause over two fragments, when it is one thought.** "If the Office hears about Okafor, we're both finished." reads as one person thinking. It is among the more natural lines in the set, and it is a single sentence.
5. **One line, one job.** "Who's we? Knowing costs extra. I'll shift anyone watching my bay." has three jobs, and the last is a game-state update in his mouth. Consider splitting it or moving the last job to the `learned` card.
6. **Answer the wrong question.** `writer.md` already asks for this, and "Please. That's his post." does it. No source tests this, but Kurvitz's personal, confrontational approach is in the same spirit.

### Gaps
- I found no Kasavin statement on keeping lines short. The [But Why Tho interview](https://butwhytho.net/2021/08/13/interview-developing-hades-with-supergiant-games-greg-kasavin/) has its content in a video, and [the GDC talk](https://gdcvault.com/play/1026975/Breathing-Life-into-Greek-Myth) is login-gated. That talk is the most likely source and worth Sky's or Fable's time to watch.
- The original Rock Paper Shotgun interview with Avellone and PC Gamer's [Avellone, Ma and Davis video](https://pcgamer.com/the-pc-gamer-show-special-chris-avellone-justin-ma-and-matthew-davis-talk-into-the-breach) were not read.

## What makes a very short conversation feel complete instead of thin?

### Takeaway
Screenwriting and interactive-fiction advice agree: completeness comes from a conflict of wants and a changed situation at the end, not from length. A conversation can also feel complete because it ends early for a reason inside the scene.

### Cited Findings
- **[PUBLISHED] David Mamet's memo to the writers of The Unit, 19 Oct 2005.** Three questions: "WHO WANTS WHAT?", "WHAT HAPPENS IF HER DON'T GET IT?", "WHY NOW?". "EVERY SCENE MUST BE DRAMATIC." It warns against lines where one character tells another what the writer needs the audience to know, flagging "AS YOU KNOW" as a danger signal. — [SlashFilm reprint](https://www.slashfilm.com/a-letter-from-david-mamet-to-the-writers-of-the-unit/).
- **[PUBLISHED] Emily Short's scene planning.** "what information does the player have to have before leaving this scene? what decisions must occur here?" and "how can it end? where might it go next?" — [Mailbag: Writing for Versu, 2017](https://emshort.blog/2017/05/18/mailbag-writing-for-versu/)
- **[PUBLISHED] Ingold's ending early.** "Players who are able to work their way through every possible conversation topic will do so." He advises letting the person leave, get back to work or grow irritated, and says that conversations cut short mean "every conversation comes with its own in-built tension". Opening with a greeting and a few beats of unrelated talk "sets an expectation for the pace of the dialogue". — [Designing Investigate Conversations, Game Developer, Oct 2019](https://www.gamedeveloper.com/design/designing-investigate-conversations). He is writing about a branching game, so the reasoning carries over only partly to a fixed scene.
- **[PUBLISHED] Ingold's escalation in small steps.** "Approach the guy. Go a bit closer. Talk to him. Go a bit closer still," compared to blackjack, where "with every card you get, the same choice is now made riskier." — [Game Developer, Aug 2014](https://gamedeveloper.com/business/-i-80-days-i-building-the-perfect-text-adventure-for-mobile)

### Inferences

A practical test for a short scene, which is my synthesis and not a published rule. `writer.md` already covers most of the first three.
1. **Want against want.** Two people, each after something now.
2. **A fact arrives sideways, then is confirmed.**
3. **Something turns.** By the last line, a person's position is different and can't go back.
4. **The ending changes the player's state.** The last line or card says what's now true and what's now possible. Short's "how can it end? where might it go next?" is the same thing, and `learned` and `opens` already implement it.
5. **A reason it stops.** A customer, the chime, a glance at the door. This gives an exit without a closing quip.

My reading of the six samples against this test (judgement, not data):
- **Strongest endings:** `pressure-inez` ("I've had a good look at your face.") ends in a new threat that matches its card. `call-inez` ends on her "Don't you hurt him" and the Fixer's reply. The turn is strong, though the last line is the softer of the two.
- **`help-inez-1`** turns well on her trapped "Then take it back. No." and ends on a refusal that still takes the money.
- **`help-inez-2`** ends on "All right. Any trouble on the square, I'll warn you." That second sentence may read more like a rule readout than speech, and the card already says it.
- **`pay-driver`** has the weakest finish, for the reason given in the previous section.

Other points:
- **The escalation idea suits the sample shape.** With 8 to 9 bubbles, each line can raise the price of the next, as Ingold's blackjack does. Short bubbles help here because the player advances into each risk.
- **`writer.md`'s no-greeting rule and Ingold's small-talk advice aren't in conflict.** The rule bans form-like greetings. Two beats of being mid-task, like Milena's tube, do the same pace-setting.
- **Thin is probably not about word count.** Sky's earlier complaint names the fault: the dialogue "doesn't make sense to task and setup for the next reveal" (memory note `dialogue-reveals`). Hades covers real relationships at a median of 74 words per conversation, and the samples have that many. The fix for thin is what the words do.

### Gaps
- A search summary said Heaven's Vault's writing starts from each major scene's subtext, then builds lines from it. I could not pin this to a page, so it is unverified. It would be a useful source if found.
- I found no study testing what makes players rate short game scenes as complete or satisfying. This section rests on craft advice from screen and interactive-fiction writers.
- The scenes need a cold read by Sky. These notes can show the size and shape are defensible, not that the lines are good.
