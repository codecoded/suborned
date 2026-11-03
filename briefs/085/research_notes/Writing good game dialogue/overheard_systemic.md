# Overheard conversations and systemic dialogue in shipped games

How to read the evidence tags. Everything under "Cited Findings" is sourced; everything under "Inferences" is my analysis, not a developer's statement.

- **[P]** I extracted the primary text myself (the Ruskin slide notes; page numbers are PDF pages).
- **[F]** Page read through a fetch tool that relays through a summarising model. Quotes are as relayed. Recheck against the page before publishing any of them.
- **[S]** Search-result excerpt only. The full page was not read, so context is thinner.

Bottom line on line evidence: **I could verify few real lines from Hitman, Dishonored or Watch Dogs.** The pages most likely to hold them refused automated fetches or came back truncated (listed in the Gaps). I did not fill that hole from memory. Verified lines below come from Left 4 Dead 2, Team Fortress 2, Arkham City, the Thief series, Fallout: New Vegas and Skyrim. All "Suggested" wording for Suborned is mine.

## How do these games write eavesdropped talk: length, and making the fact stand out while sounding like people?

### Takeaway
The games with the clearest documented systems do not rely on the line alone to carry the fact. They mark the talkers, let the talk sound loose, then restate the fact in a game layer (objective prompt, marker, clue). No source publishes durations, so "how long" can only be estimated from a few short verified lines and from conversation counts.

### Cited Findings

**Hitman (2016 onward)**
- "Most Opportunities begin when 47 overhears NPCs discussing sensitive information." — [Game Critics](https://gamecritics.com/corey-motley/hitmans-golden-opportunities/) [F]
- A Hitman 2 review says the Opportunities were renamed Mission Stories, and that stories are typically revealed by eavesdropping on conversations. — [Gaming Pastime](https://gamingpastime.com/?p=25463) [S]
- A Hitman 2 Legacy Paris walkthrough shows the pattern as play: wait near the woman marked by the Mission Story waypoint, she walks onto the balcony to start a phone call, listen in, follow, then investigate a laptop to update the story. — [TrueAchievements](https://www.trueachievements.com/game/HITMAN-2/walkthrough/11) [S; page returned 403]
- A forum poster's characterisation: NPCs "casually" describe ways people could accidentally die or the routines of their bosses. This is a player's view, not IO's. — [Hitman Forum](https://www.hitmanforum.com/t/are-hitman-npcs-dialogues-distinctive/5071) [F]
- Kotaku followed one background man through his phone calls. The article paraphrases them: frustrated with customer service, angry at a video rental shop over an overdue movie, avoiding a fight about scones, marvelling at the weather. It calls them "unusually well-written". — [Kotaku](https://kotaku.com/the-hidden-life-of-a-random-hitman-npc-1773218201) [F]
- On Absolution's one-sided calls, Kotaku's writer wonders: "I'm not sure what it takes to make a one-sided cell conversation sound so convincing (maybe you write the whole exchange, then leave half out?)". That is a critic's guess, not a stated IO method. — [Kotaku](https://kotaku.com/hitman-absolutions-overheard-dialogue-is-goofy-great-5962344) [F]
- Blogger Martin Nerurkar contrasts Absolution with Splinter Cell and Deus Ex, where he says two guards usually just chat about the world. In Absolution the talk seems to span a group, which "operates on a script that is probably kept until the player triggers an alarm". He says he does not know the actual mechanics. — [Game Developer](https://www.gamedeveloper.com/design/the-hitman-and-individual-ai-personalities) [F]

**Desperados III and the real-time tactics genre (closest match to Suborned's camera)**
- Creative director Dominik Abé: "In the 'Civil Zones' you can also listen in on conversations of NPCs to get hints on new gameplay options." — [Game Developer](https://gamedeveloper.com/design/designing-the-real-time-stealth-and-combat-of-i-desperados-iii-i-) [F]
- Mission 10 has "a total of six conversations between bandits in different parts of New Orleans to be overheard". Talkers are marked by "the question marks above their heads". Three are in public areas and three behind forbidden lines, where a disguise is needed. — [GamePressure](https://www.gamepressure.com/desperados-iii/get-a-disguise-and-eavesdrop-on-convo/z0e335) [F]
- The clue is split. One group gives suspects' aliases; the other shows whether a suspect got a letter about a Wanted Man. The guide's example: the Hawk was wanted, and under that alias was O'neill from Marina 9. Solving it earns a badge and names the right suspect. Skipping it means checking further suspects until you find the right one. So listening is a shortcut, not a gate. — [GamePressure](https://www.gamepressure.com/desperados-iii/get-a-disguise-and-eavesdrop-on-convo/z0e335) [F]

**Thief**
- Verified guard and servant lines (quote page, relayed):
  - Thief II guard argument: "How dare you defile the name of someone so good and virtuous as the Lady Van Vernon." / "Oh ho! Mighty fine words coming from a knock-kneed, inbred pageboy such as yourself."
  - Thief II, Benny: "Anyways, I work better when I'm drunk. It makes me fearless!"
  - Deadly Shadows guard: "Oh...these godforsaken boots! Why can't I wear my own shoes to work?"
  - Deadly Shadows guard: "I'll be home soon, drinking good dark ale, nibbling on a nice leg of mutton, forgetting about this place...."
  - Source for all: [Wikiquote](https://en.wikiquote.org/wiki/Thief_(series)) [F]. That page lists no idle guard lines for The Dark Project.
- Eavesdropping and reading notes can give useful hints. — [Gaming Pastime review](https://gamingpastime.com/?p=24308) [S]. Another critic says overheard talk and peeked-at notes are not essential to the plot but build the voyeuristic atmosphere. — [Electron Dance](https://www.electrondance.com/?p=6453) [S; the exact article was not pinned down from this result]

**Deus Ex**
- Original Deus Ex tooling separates barks from conversations by how they play. A bark is "non-blocking" and "plays in PoV mode without stopping the game". Barks are "triggered by the AI routines, such as Idle, Searching, Attacking, Critical Damage, etc." and conventionally live in one shared `AIBarks.con`. — [PlanetDeusEx ConEdit help](https://gamespy-archives.quaddicted.com/sites/www.planetdeusex.com/dx1/help/editing/ion_conedit/index.html) [F]
- In Human Revolution, a password guide says you can learn passwords by reading email and notes and by eavesdropping on conversations characters have while you are not around. — [Almar's Guides](https://almarsguides.com/computer/games/deusexhumanrevolution/passwords/picus/) [S]

**Dishonored**
- The Heart, pointed at a person, reveals that person's secrets. — [Dishonored Wiki](https://dishonored.fandom.com/wiki/The_Heart) [S; page returned 402]. Kirk Hamilton finds it more engaging than the books and notes, and that it shares backstory "in a much more organic" way, and tells "an enigmatic story about the place or person you're observing". — [Kotaku](https://kotaku.com/as-you-play-dishonored-dont-forget-to-listen-to-your-h-5952275) [F]
- A Dishonored 2 guide says listening to NPCs' and guards' secrets can reveal useful information on key targets. — [Twinfinite](https://twinfinite.net/?p=235254) [S]

**Watch Dogs**
- Telephone Tapping is a free passive Profiler ability for listening to others' conversations, for mission intel or plain nosiness. — [Watch Dogs Wiki](https://watchdogs.fandom.com/wiki/Telephone_Tapping) [S]
- Lead writer Kevin Shortt: every person has a scannable profile, "all systemically generated", because there were too many to write by hand. — [GamesBeat](https://gamesbeat.com/ubisoft-writer-finally-gets-to-spill-some-secrets-of-the-watch-dogs-story-interview/2) [S; page returned 403]
- Watch Dogs: Legion wrote each line in 20 persona voices, first 10, doubled for London's accents and dialects. — [Game Developer on Hennessy's GDC 2023 talk](https://gamedeveloper.com/gdc2023/how-i-watch-dogs-legion-i-made-ubisoft-rethink-its-storytelling) [F]

**Sleeping Dogs**
- Its documented eavesdropping is a job, not a scene. "Listening In" gives a two-and-a-half-minute window to plant a bug in a marked air vent, through a minigame. Hacked camera feeds later show on Wei's apartment TV, with triad activity marked. — [Prima Games](https://primagames.com/eguides/sleeping-dogs-eguide/walkthrough/main-story-walkthrough/listening-in) [S]; [SuperCheats](https://www.supercheats.com/guides/sleeping-dogs/police-cases) [S]

### Inferences
- **Line length.** Verified barks run short: Left 4 Dead 2's "I can feel my feet growing fungus" is 7 words, the New Vegas line 11, the Thief boots line 12, the Skyrim arrow line 16. Suborned's 12-word bubble ceiling fits this range, and `listen-inez` sits comfortably inside it (5 to 10 words per beat).
- **Scene length.** `listen-inez` is 9 beats and 77 words, by my count. At an assumed 2.5 to 3 words per second that is about 25 to 30 seconds. I found no shipped benchmark to compare it with.
- **What the examples suggest about standing out.** The pattern I see is three layers: loose talk, a marker on the talkers, then a plain restatement outside the fiction. `listen-inez` already has this: the talk, the Scout line, then `learned` and `opens`. Because of that, the talk does not have to carry the fact alone, and can stay evasive. This is my reading across games, not a stated rule from any studio.
- **What works in `listen-inez`.** Inez's denial ("Who's looking? Nobody's looking.") gives up the secret by refusing it, and "He crosses the whole square for one coffee" states the affair as a concrete behaviour. Both are facts delivered as character, which is the virtue the Thief and Hitman examples are praised for.
- **Risks in `listen-inez`.**
  - The two threes ("three this month" notices, "Three payments" behind) count different things, and could blur at bubble speed.
  - "Bureau" is unexplained until the Scout's "Bureau debt", which is fine only if the Scout line always plays.
  - **"Two ways in" counts the options.** Everyone can be turned more than one way and nothing should imply one right approach, so a closed count works against that, and it duplicates the UI's job. Suggested (mine): the Scout gives facts only, e.g. "Bureau debt. And the door guard's her man." and the `opens` list carries the options.
- **Split clues suit a top-down game.** Desperados III's two-group clue spreads eavesdropping across the map and makes the Scout's positioning the decision, while the "skip it and check more suspects" route keeps it from being the one right way.
- **Write the unheard half.** Kotaku's guess about one-sided calls is worth trying for Suborned's telephone scenes, but it is only a guess.

### Gaps
- **No verified Mission Story trigger lines.** Hitman Wiki, Giant Bomb's "Ambient Effusion" dialogue thread and the Hitman Forum "all dialogues" project are the leads. The wiki returned 402 and Giant Bomb 403; the forum page held only an admittedly misremembered line, which I excluded.
- **No verified Dishonored guard or Heart lines.** The wiki was blocked; the GamesRadar Heart article came back truncated. Search results did not back any Weeper or whale-oil line, or a Pratchett link.
- **No verified Watch Dogs call or profile lines.** GamesRadar's "[10 funniest privacy invasions](https://gamesradar.com/10-funniest-privacy-invasions-watch-dogs)" is the best lead, but came back truncated.
- **No duration data** for any game's overheard scenes.
- **Commandos, Shadow Tactics and Invisible Inc:** I found nothing on written eavesdropping. Treat that as "not found", not as "they don't do it".
- **Writer attribution:** nothing tied specific people to Thief guard lines or Dishonored guard talk. No IO Interactive or Arkane primary talk on writing overheard lines turned up.
- **Pre-release Watch Dogs figures:** a search summary reported 3,000+ character kits and 400 to 500 recorded lines per citizen, from a 2014 podcast summary. I could not pin the source, and these are pre-release claims, so do not cite them as shipped numbers. Possible source: [Gameranx](https://gameranx.com/updates/id/21241/article/watch-dogs-10-new-confirmed-facts/).

## How do games signal "you just learned something you can use"?

### Takeaway
The documented patterns are: mark the talkers before the talk, restate the result as a short verb-first prompt, and let players choose how much guidance they get. Hitman's three notification settings are the best-documented example. Critics' main caution is that a full prompt tells the player exactly what to do, which is the risk for "no one right approach".

### Cited Findings
- **Selectable guidance.** Hitman (2016) has three notification styles. The first "turns the Opportunities system off completely". The second "lets players know when an opportunity is unveiling" and gives the next task, but "does not give any HUD waypoints". The last "includes onscreen waypoints for each step". — [Game Critics](https://gamecritics.com/corey-motley/hitmans-golden-opportunities/) [F]
- **Verb-first prompt, picked from a menu.** Pick the "be a model" Opportunity in the pause menu and the game sets a waypoint. "The onscreen prompt might say 'steal an outfit'." — [Game Critics](https://gamecritics.com/corey-motley/hitmans-golden-opportunities/) [F]
- **The critique.** "IO's new Hitman shows players exactly how to successfully fulfill every step of an Opportunity." The article says older games left players guessing. A reader adds: "Personally I don't want the game telling me how to get stuf done." — [Game Critics](https://gamecritics.com/corey-motley/hitmans-golden-opportunities/) [F]
- **A failure state exists, at least as reported.** A Steam user reports "Intel Lost" showing on Sapienza's "The Good Son" and suspects a bug. One player's report, not confirmation of the design. — [Steam discussion](https://steamcommunity.com/app/863550/discussions/0/1640918469754649507) [S]
- **Marker on the talkers.** Desperados III uses question marks above heads, and a guide advises turning on the enemy's field-of-view display to get close without raising an alarm. — [GamePressure](https://www.gamepressure.com/desperados-iii/get-a-disguise-and-eavesdrop-on-convo/z0e335) [F]
- **The companion's voice.** Fan commentary notes Diana tells 47 when a target first crosses his field of vision, and that some players dislike losing the challenge of spotting targets. — [TV Tropes](https://www.tvtropes.org/pmwiki/pmwiki.php/YMMV/Hitman2016) [S]
- **The system flags it, the player decides.** A Watch Dogs preview describes a phone conversation triggering a "crime probability" warning, with Aiden choosing whether to follow it up. — [PC Gamer preview](https://pcgamer.com/watch-dogs-preview-big-brother-is-watching-but-then-so-are-you) [S; the outlet is my best attribution from the result set, not confirmed]
- **Spoken state as a prompt.** Ruskin: "Having characters announce such state on their own is an important prompt to players about actions they may need to take." — [Ruskin, GDC 2012, PDF p.20](https://cdn.fastly.steamstatic.com/apps/valve/2012/GDC2012_Ruskin_Elan_DynamicDialog.pdf) [P]
- **Plain state talk is a recognised option.** Ruskin's example is Arkham City's TYGER guards: "Target has been sighted!" "Converging on last known location!" "Enemy object found! Initiating search!" "Scanning in dark areas!" He calls them "so clear and straightforward". — [Ruskin, PDF pp.10-11](https://cdn.fastly.steamstatic.com/apps/valve/2012/GDC2012_Ruskin_Elan_DynamicDialog.pdf) [P]

### Inferences
- **For Suborned, the Scout's line is the "Diana" layer** and `opens` is the "steal an outfit" layer. Keep their jobs apart: the Scout says what is true; the option says what you could do, with its price.
- **Word options as acts, not as a morality label or a solution.** "Pay off her loan" and "Threaten to expose them" follow Hitman's verb-first form and fit Sky's one-line-price note. The category words "Help" and "Pressure" do some moral sorting for the player, which may be fine or not; worth testing.
- **Hitman's "shows every step" problem is a warning for Suborned.** Opening an option is one thing; a chain of step prompts would read as a single right route. Suborned's price-before-commit rule gives the player a real decision that Hitman's step prompts don't.
- **The moment of opening needs an in-world anchor.** Since the camera sits above the street and the speech is in bubbles, a marker on the talker when the fact lands (rather than only a side-panel entry) would tie the option to a person. This is my proposal; I did not find a shipped example to cite for it beyond Desperados III's pre-talk markers.

### Gaps
- Exact Hitman on-screen wording when a story is discovered or updated was not verified (the wiki and walkthrough pages were blocked). From memory Diana voices some of these updates, but I could not confirm it, so treat that as unverified.
- No user research or telemetry on which signal works best. Everything here is designer practice and critics' opinion.
- Crusader Kings III's secrets and hooks (a secret you learn becomes a lever on a person) may be a close structural match to Suborned's debt and affair. I did not research it; it is only a lead.

## How do bark systems and rule-based dialogue work, and what do they teach about short, fresh lines?

### Takeaway
Ruskin's Valve system is simple enough to copy at build time: facts go in, the rule with the most matching criteria wins, and rules write memory back. Freshness comes from specificity, memory and cooldowns rather than from raw line count. Hades shows the same logic at scale, though its priority system is only documented second-hand here.

### Cited Findings

**Valve (Ruskin, GDC 2012), all [P] from the [slides](https://cdn.fastly.steamstatic.com/apps/valve/2012/GDC2012_Ruskin_Elan_DynamicDialog.pdf)**
- **Rule shape.** A rule is criteria that must all be true; if one is false or mentions a fact not in the query, the rule rejects (p.62).
- **Scoring.** "The scoring function that worked best for us was the simplest one imaginable – the number of criteria in a rule. The more criteria a rule has, the more specific it is." (p.62) General lines remain as fallbacks; if Coach has no specific line for a new survivor, "it would automatically fall back to the general ones" (p.76).
- **Worked rule.** Nick's "TalkIdle" rule requires: three survivors present, speaker Nick, "This line hasn't been said already", swamp map, start area (p.67). Each character polls itself every few seconds for idle lines (p.67).
- **Memory.** "Then" rules write context back: "Creates memory, the ability to pick subsequent lines based on what happened before." (p.37) A `seen_barrels` value makes the second sighting match a different rule (pp.35-36). Facts can expire automatically "to prevent two successive bits of a running gag from being played too close together" (p.68).
- **Conversation as chained lookups.** When one character finishes, it dispatches a follow-up concept; the next speaker queries for a reply (pp.32, 69-70). Branches are cheap: "Just by adding a couple of rules and recording some voice, you can add new outcomes, or merge paths back together." (p.72)
- **Verified lines from that branch:** "I can feel my feet growing fungus", "This swamp is just a cesspool for disease." (p.71)
- **Earshot.** A follow-up can go to all nearby characters, and the best-scoring reply speaks (pp.73-75). Character comes from this: Coach has specific lines for helping others, so he tends to win. "Coach coaches – that's who he is." (p.76)
- **Random gate as a throttle.** Coach's specific lines carry an extra "if random number is less than 30" criterion "so they don't get overplayed" (p.76).
- **Other verified barks:** "Need a dispenser here!" (p.19), "We must stop tiny cart!" (p.21).
- **Scale and authorship.** The series had "over ten thousand lines" (p.45). A new line about an object is "Four lines of text – one of them is the name of the vox file to play" (p.31). Dota's writer works in a spreadsheet, with a row per line and a column per criterion, exported by macro (p.93).
- **Passers-by.** Barks can "create the feeling of a living world". Ruskin contrasts townspeople who remember what you did with those who "said the same canned line over and over" (p.14).

**Hades**
- Over 22,000 lines. — [GDC Vault](https://gdcvault.com/play/1026975/Breathing-Life-into-Greek-Myth) [S]; "over 305k words". — [GoNintendo](https://gonintendo.com/stories/374811-supergiant-shares-voiceover-stats-for-hades-reveals-characters-w) [F]
- Kasavin: "Reactivity has always been a goal of our narrative design." The game checks mid-run states, such as meeting a character with health below a threshold, and plays one of several pre-written events (author's paraphrase). — [Game Developer](https://gamedeveloper.com/design/how-supergiant-weaves-narrative-rewards-into-i-hades-i-cycle-of-perpetual-death) [F]
- Kasavin's thread describes the mindset as asking what these characters would talk about and notice, then adding those interactions. With no visualisation of event connections, it got trickier to manage as content grew. — [GameSpot](https://gamespot.com/articles/hades-writer-explains-script-philosophy-after-player-discovers-incredible-detail/1100-6482650/) [S; page returned 403]
- A modder reports a `MinRunsSinceAnyTextLines` field gating how many runs must pass before a line plays ("Some were as high as 20 runs!"), and that a lowered line triggers next chance "unless some other line had priority over it". The modder also adjusted priorities on repeatable and story lines. This is second-hand, from a Nexus mod page. — [Nexus Mods, Hades](https://www.nexusmods.com/hades/mods/47) [S; page returned 403, and which of the mods /47, /89, /93 said this is unresolved]

**Bethesda and Ubisoft**
- Skyrim's guard lines are mostly gated by one condition. "Keep your arrows in their quiver, archer." needs Archery 30 and suitable gear. "I know Thieves Guild armor when I see it. You're not fooling anyone." needs that armour worn. "Wait... I know you." needs a bounty in that hold. "Ugh. That armor stinks of death. You been grave robbing?" needs Ancient Nord Armor. By contrast, "I used to be an adventurer like you. Then I took an arrow in the knee..." has requirement "None". — [UESP](https://en.uesp.net/wiki/Skyrim:Guard_Dialogue) [F]
- At Ubisoft Toronto, narrative designer Brandon Hennessy said of Legion's early prototypes that they "have all of the narrative energy of a tug of war where neither side is fully holding the rope", because "We [the writers] can't know a character's age, gender, sexuality, occupation, etc." He also said: "Introducing a single authored character into a big sea of procedural characters makes them cohere a bit." — [Game Developer](https://gamedeveloper.com/gdc2023/how-i-watch-dogs-legion-i-made-ubisoft-rethink-its-storytelling) [F]

**Firaxis:** I found nothing citable.

### Inferences
- **Build-time fits, runtime stays deterministic.** Suborned has no AI at runtime, and Ruskin's model needs none: a generator writes rules and lines at build time, and the sim picks by facts. Any "random under 30" gate must draw from the run seed so a saved run replays exactly.
- **Rules could be the unit the dialogue generator writes.** A plausible Suborned rule: speaker, listener, need kind (debt), who else is present, heard-before flag, Alert, Standing. Add a general fallback for each need kind so a generated person is never silent.
- **A more specific line ought to name something the player can check.** Skyrim's armour lines are remembered because they notice you. For Suborned: a generated line that names a real place, shift or person in that run should outrank a generic debt line, which is what criteria-count scoring would do by default.
- **Hennessy's complaint is the risk for generated people.** Writers who can't know who is speaking write mush. A generator should receive the specifics (Inez, waitress, Bureau, Okafor) as facts. His "single authored character" point supports the Handover being a hand-tuned reference that generated cast around it can lean on.
- **Interruption and memory cost little.** Ruskin's chained follow-ups give both without any conversation entity, which matters for files and checks kept small.

### Gaps
- The Hades talk itself and any published description of its priority system were not read. The GDC Vault recording is the primary source. Field names above rest on one modder's note.
- No Firaxis bark practice found. Ubisoft coverage is limited to Watch Dogs and Legion.
- Ruskin's slides give no measured effect of these techniques on player perception.

## How do games keep eavesdropping interesting on repeat listens or when the player arrives partway?

### Takeaway
Repetition is the documented failure, and its documented cures are memory, cooldowns, branching, and randomising content between restarts. I found **no source on the arrive-partway problem directly**, only adjacent techniques: start on approach, require staying to the end, and segment speech so it can break cleanly.

### Cited Findings
- **The problem stated.** L4D is played many times: "if you hear the exact same canned lines at the exact same points over and over, it becomes dull very quickly." — [Ruskin, PDF p.39](https://cdn.fastly.steamstatic.com/apps/valve/2012/GDC2012_Ruskin_Elan_DynamicDialog.pdf) [P]
- **Variety by branch.** "Each time the game plays out, it'll randomly select a different change, a different experience". — [Ruskin, PDF p.71](https://cdn.fastly.steamstatic.com/apps/valve/2012/GDC2012_Ruskin_Elan_DynamicDialog.pdf) [P]
- **Clean interruption.** Follow-ups are looked up when the next line starts, so if a zombie attacks Ellis "the conversation self-terminates because the criteria for its existence are no longer true". — [Ruskin, PDF p.78](https://cdn.fastly.steamstatic.com/apps/valve/2012/GDC2012_Ruskin_Elan_DynamicDialog.pdf) [P]
- **Segmenting.** "It's also worthwhile to cut up long monologues by a single character into short pieces", as a simple way to bail out of a long speech or let another character break in. — [Ruskin, PDF p.79](https://cdn.fastly.steamstatic.com/apps/valve/2012/GDC2012_Ruskin_Elan_DynamicDialog.pdf) [P]
- **Variety between restarts.** In Hitman the game randomly picks that man's caller from several possibilities each time the level restarts, so each restart shows a different part of his day. — [Kotaku](https://kotaku.com/the-hidden-life-of-a-random-hitman-npc-1773218201) [F]
- **Start on approach.** In Thief Gold, two guards (Rob and Abe) at the Tower of Water entrance begin a conversation as soon as you approach. — [The Spoiler walkthrough](https://the-spoiler.com/ACTION/Looking.glass/thief.3/thief15.htm) [S; which of the walkthrough's pages this came from is not pinned]
- **Stay to the end.** Desperados III's guide says to listen until the conversation ends to get the clues. — [GamePressure](https://www.gamepressure.com/desperados-iii/get-a-disguise-and-eavesdrop-on-convo/z0e335) [F]
- **Long cooldowns.** Hades lines can be gated by runs since last spoken. — [Nexus Mods, second-hand](https://www.nexusmods.com/hades/mods/47) [S]
- **Failure on record.**
  - New Vegas: "Patrolling the Mojave almost makes you wish for a nuclear winter" became a meme, with an early spread dated 2 November 2010, because troopers repeat the stock line frequently when the player approaches. — [Know Your Meme](https://knowyourmeme.com/memes/patrolling-the-mojave) [F]
  - One mod made such lines play once a day; another nukes the speaker. — [Nexus Mods listing](https://www.nexusmods.com/newvegas/mods/62756/) [S; which mod did what is not pinned]; [PCGamesN](https://www.pcgamesn.com/fallout-new-vegas/meme-mod) [S]
  - Skyrim's arrow line is the same case: no requirement. — [UESP](https://en.uesp.net/wiki/Skyrim:Guard_Dialogue) [F]
- **Open player question.** On Hitman, a forum poster asks: "do NPCs stand in silence once they've run out of dialogue?" and whether they "start again from their first conversation". It goes unanswered there. — [Hitman Forum](https://www.hitmanforum.com/t/are-hitman-npcs-dialogues-distinctive/5071) [F]

### Inferences
- **Suborned's repeat case is not L4D's.** Each new game rolls its own seed, so the same *facts* recur across a career or retry, not within a pass. Within a run the main repeat is the player lingering or returning. The heard-before flag covers that; a short "they're past this now" variant covers the second listen.
- **Arrive-partway: make each fact stand alone.** `listen-inez` happens to split well: beats 1-4 carry the debt, beats 5-8 the affair. Anyone arriving at beat 5 hears "Ask your friend" with no debt context, yet the affair still lands. Suggested design (mine): tag each beat with the fact it delivers, and award only what was heard. The Scout then says only that, so a late arrival gets one fact and one option, and can come back for the other. This also rewards the Scout's timing, and avoids an all-or-nothing "Intel Lost".
- **Replay the scene, not the clock.** A seeded loop that restarts the scene when the two are next together, with a different opener, is cheaper than many full scenes. Ruskin's branches can merge back (p.72), so only the openers need variety while the fact-bearing beats stay fixed.
- **Fact beats should be interruptible.** Ruskin's self-terminating chain fits Alert changes. If Alert rises mid-scene, the talkers stop, and the partial result stands.
- **Budget variety by how often a line is heard.** The New Vegas and Skyrim failures are short, flavourful, unconditional and frequent. The funnier the line, the sooner repetition shows it up, so jokes need the tightest gating. That is my inference from those two cases.

### Gaps
- **No shipped game or developer talk found that addresses partial arrival directly.** How Hitman treats a player who walks in mid-call was not verified.
- Whether Hitman loops or stops a Mission Story conversation is an open question in the forum thread above; no answer found.
- Desperados III's behaviour if you leave mid-conversation is not stated in the guide.

## How do comedy and character in idle talk make the world lived-in, and how does this help or hurt clarity?

### Takeaway
The critics I could read (Kotaku, PC Gamer) credit humour and mundane worries with making background people feel real, even with no plot content. The documented harm is repetition. The harm to clarity, noise around a real clue, is my analysis: I found no evidence measuring it. My conclusion is that character and fact work best when the same line does both.

### Cited Findings
- **Mundane worries read as people.** Kotaku's Hitman man's scones and overdue-rental troubles, "unusually well-written", and the writer found them surprisingly relatable. — [Kotaku](https://kotaku.com/the-hidden-life-of-a-random-hitman-npc-1773218201) [F]
- **Performance and fun.** Of Absolution's crowds, Kotaku calls the chatter "among the most convincing I've ever heard in a game" and says the clips "demonstrate how much fun the writers and voice actors had with the material". — [Kotaku](https://kotaku.com/hitman-absolutions-overheard-dialogue-is-goofy-great-5962344) [F]
- **Humanising the ignorable.** PC Gamer describes an Absolution exchange between rookie cop Fizano and his sergeant that "adds a degree of humanity to characters that could otherwise be ignored", and describes earlier games' overheard talk as guards and civilians arguing or fixating on sex. — [PC Gamer](https://www.pcgamer.com/hitman-absolution-preview/) [S]
- **Thief's idle lines** cover boots, ale and mutton, a drunk claiming to work better, and a pompous argument that ends in an insult (lines above). — [Wikiquote](https://en.wikiquote.org/wiki/Thief_(series)) [F]
- **New Vegas's trooper line** became a meme through frequent repetition. — [Know Your Meme](https://knowyourmeme.com/memes/patrolling-the-mojave) [F]
- **Dishonored's City Watch:** a fan-wiki-style source in my search results says memes often revolve around its repetitive dialogue. — [TV Tropes](https://tvtropes.org/pmwiki/pmwiki.php/YMMV/Dishonored) [S; the exact source among the results is not pinned]
- **Traits should be legible enough to predict.** Nerurkar proposes that NPCs have traits like patience or aggression that dialogue reveals, so that players can predict reactions, and says "characters have to be painted with broad strokes" so players grasp them fast. This is a blogger's proposal, not IO's method. — [Game Developer](https://www.gamedeveloper.com/design/the-hitman-and-individual-ai-personalities) [F]
- **Character from who answers.** Ruskin's Coach (see above) gets his personality from which rules he wins. "So all of this creates character for Coach, and an interaction between the survivors!" — [Ruskin, PDF p.76](https://cdn.fastly.steamstatic.com/apps/valve/2012/GDC2012_Ruskin_Elan_DynamicDialog.pdf) [P]
- **Story through remarks on the world.** L4D "tells its story through the environment", and characters express who they are by remarking on it: "That's a chance for running gags too." — [Ruskin, PDF p.44](https://cdn.fastly.steamstatic.com/apps/valve/2012/GDC2012_Ruskin_Elan_DynamicDialog.pdf) [P]

### Inferences
- **Help:** mundane talk sets a baseline. The player learns these people are not plot devices, and a real secret then feels overheard instead of delivered. Sky's "subvert people" pillar depends on the people feeling like people.
- **Hurt:** with bubbles on screen at a distance, every word costs attention. Sky has already said the reading is too much, so idle chatter competes directly with the fact. Spoken chatter in a first-person game can fade into the background, but a text bubble asks to be read, so marked versus unmarked talk likely matters more for Suborned than for Thief or Hitman.
- **Character in the trooper line:** New Vegas's complaint carries world and character together (bored soldiers, a bad posting), which is why it is worth imitating in shape, if not in frequency.
- **Let the joke carry the fact where possible.** "He crosses the whole square for one coffee" is the best line in `listen-inez` for this reason: small, human, faintly funny, and it *is* the evidence. Thief's boots tell you nothing to act on; that line tells you where a guard goes.
- **Keep comedy out of the Scout's line and the options.** Evidence that the plain layer should be plain is limited to Arkham's TYGER example, but it is consistent with Suborned's plain-copy rule.
- **Tone caution.** Thief's and Hitman's comedy can coexist with cruelty in the player's own actions. Suborned's scenes end in leverage over a frightened waitress, so a joke at Inez's expense would change how Pressure feels. That cuts either way, and is a call for Sky.
- **Temperament as readable fact.** Nerurkar's "broad strokes" idea suits Suborned's temperament and Talker rules. How someone talks could hint how they'd respond when turned, as long as it doesn't signal one correct approach. This is untested.

### Gaps
- No playtest data or developer statement found on whether comedic idle talk reduces players' uptake of real clues.
- PC Gamer's "Hitman: Absolution goons get 2000 pages of dialogue" could be useful on scale; only [the headline](https://www.pcgamer.com/hitman-absolution-goons-get-2000-pages-of-dialogue) came through, so the figure's source is unverified.
- Fallout: New Vegas's writers' intent for the trooper barks was not found. Only the outcome is documented.
