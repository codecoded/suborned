# How well-regarded games architect systemic and reactive dialogue

Research strand 01 for brief 012 (the dialogue generator). Written 2026-10-16. The question is how shipped games and research systems **select, condition, remember and de-duplicate** lines, and what of that transfers to a generator where a model writes tables at build time and seeded code expands and picks them, with no AI at runtime and exact replay.

Where I could, I went to primary material: the GDC slide decks (Valve, Naughty Dog, Campo Santo, Supergiant), the shipped Hades Lua scripts, Halo 2's tag definitions, RimWorld's def XML, the Nemesis patent, and the CiF, Versu and Façade papers. Secondary sources are marked as such.

## What Suborned already has

This is the baseline the findings are measured against (`briefs/012/fable-dialogue-generator.md`, `contracts/dialogue/`, `tools/dialogue/lookup.ts`, `contracts/missions/handover/text.schema.ts`):

- **Key:** a line cell is keyed on `event × verb × outcome × temperament`, optionally qualified by `need` or `secret`. Cells sit inside one of 7 voice groups.
- **Selection:** `lookup()` keeps the entries whose fields all match and takes the one naming the most fields. This is Valve's "most specific wins". On a tie, the first entry in file order wins (strict `>`).
- **Variation:** `pickIndex(seed, key, n, count)` is `fnv1a32(seed|key|i) mod count`, bumped by one when it would repeat the previous use. The key carries the person's id.
- **Slots:** a closed set (`address job noun work need own met`, plus `focus other` in scenes), filled from the person at expansion time.
- **Not yet built:** state-banded qualifiers (bravery step, resentment, promoted, Hero/Menace band), memory written back by lines, essential vs flavour tiers, speaker selection for crowd reactions, conversation interruption semantics.

---

## 1. Valve: the Response Rules system (Half-Life 2 → Left 4 Dead 2, Portal 2, Dota 2)

**Sources:** Elan Ruskin, "AI-driven Dynamic Dialog through Fuzzy Pattern Matching", GDC 2012. The speaker-note slides are at [media.steampowered.com](https://media.steampowered.com/apps/valve/2012/GDC2012_Ruskin_Elan_DynamicDialog.pdf) and the session at [GDC Vault](https://www.gdcvault.com/play/1015528/AI-driven-Dynamic-Dialog-through). The script format is documented on the [Valve Developer Wiki](https://developer.valvesoftware.com/wiki/Response_System) (read via archive.org). Emily Short's [write-up](https://emshort.blog/2012/03/16/gdc-2012-talk-on-dynamic-dialogue/) is secondary.

**Data model.**
- A *fact* (or context) is a key–value pair. A *query* is "a pile of those glued together": the event's own facts, the speaker's live state, the speaker's persistent memory and the world's state, merged into one flat dictionary (slides 49–59).
- Two keys are always present: `concept` (what kind of line: "saw enemy", "TalkIdle") and `who` (which voice).
- A *criterion* tests one fact. A *rule* is a tuple of criteria that must all pass; a rule that mentions a fact missing from the query rejects (slide 62).
- A rule points at a *response group*: a list of variants with playback flags.
- In the shipped script format: `criterion <name> <key> <value> [weight N] [required]`, and `rule { criteria … response … [matchonce] [applyContext …] }`.

**Selection rule.**
- "The scoring function that worked best for us was the simplest one imaginable – the number of criteria in a rule. The more criteria a rule has, the more specific it is" (slide 62).
- The rule with the highest score plays. Rules that tie are "all appropriate so you can choose randomly between them for variety" (slide 64).
- The Source 2013 implementation adds per-criterion weights, with the concept usually weighted 5, and `required` flags (wiki).
- When nothing matches, nothing is said.

**Memory and state.**
- "Then" rules "can also write context back to the character or the world. This context is appended to the next query performed by the character. Creates memory" (slide 37).
- The swamp example writes two facts: that the line has played, and that Nick is speaking for the next few seconds. The second fact expires on its own, which keeps two halves of a running gag from playing too close together (slide 68).
- Ruskin's summary: you need "a way for a matched response to write state back to the world, to create memory and turing-completeness" (slide 126).

**Repetition control.**
- By default a response group plays every variant before repeating any (`permitrepeats` lifts this).
- `sequential` plays variants in listed order. `norepeat` disables the group once it is used up.
- Per response: `speakonce`, `delay` (a speaker cooldown), `respeakdelay` (concept cooldown), `odds` (a chance to say nothing), and `weight` (wiki).
- Coach's lines about specific survivors carry "an additional 'if random number is less than 30' criteria so they don't get overplayed" (slide 76).

**Relationships and who speaks.**
- A `then <target> <concept>` followup sends a new concept to another character once the line ends.
- The target can be `any` or `all` within earshot. Each listener queries its own rules and the best score speaks. "Coach coaches – that's who he is": his specific rules outscore the others' generic ones, and if he is absent the generic fallback still fires (slides 73–76).
- Rivalries and per-character jabs (TF2's "the spy is… a double agent!") are just rules with a criterion on the other party.

**What went wrong.**
- Ruskin's closing regret: "Storing individual variables to remember which lines were said, and having to add criteria on the rules to prevent them matching twice, is lame. It would have been better to have some way for a response to remove itself from the database after playing" (slide 142).
- His tooling lesson: a visual drag-and-drop tool he built was never used, because the L4D and Portal writers wanted a FoxPro database (slides 94–95).

**What transfers.**
- Followups are re-queried when the callback fires, not when the first line starts. If a zombie arrives mid-line, `IsNotInDanger` fails and "the conversation self-terminates because the criteria for its existence are no longer true. You don't need any kind of explicit interruption mechanism" (slides 77–78). For our scenes: evaluate each beat against current state, not the state when the scene began.
- Our `lookup` is already Valve's rule. Make once-only a first-class flag rather than ad-hoc facts.
- Make every query a flat, closed fact dictionary (people rules + rulebook + sim state), so new qualified cells never need new code. Our "unknown = reject" fits well: the fact names are a closed vocabulary.

---

## 2. Naughty Dog: Uncharted → The Last of Us

**Source:** Jason Gregory, "Context-Aware Character Dialog in The Last of Us", GDC 2014. The speaker-note slides are at [gameenginebook.com](https://gameenginebook.com/resources/Gregory_Jason_ContextAwareDialog.pdf) and the session at [GDC Vault](https://www.gdcvault.com/play/1020951/A-Context-Aware-Character-Dialog).

**Data model.**
- A *logical line* (`'line-out-of-ammo`) maps per character to N *physical lines*.
- Hunters share 8 voice actors, assigned "in a recognizable way" by head mesh, so a returning face keeps its voice.
- Conversations are *segments*, one logical line each. A segment holds rule blocks, and each block carries `:rule`, `:line` and `:next-seg`.
- Facts live in several dictionaries: global, per faction, per character, and per request (the *context*, which always holds `'speaker` and optionally `'listener`).
- Criteria come in only three types: existence, int/symbol equality, and float range.

**Selection rule.**
- The most criteria wins, "as per Valve's system". Ties are random.
- Authors add `(:dummy 2)` criteria to equalise counts and force a random pick between a specific and a general line.
- Unlike Valve, rules are evaluated only when code or script requests a line (event-driven, not polled).

**Memory and state.**
- `:on-play` side effects write facts. Examples: doing something differently the first time, getting frustrated after the player repeats something 3 times, "updating number of times saw a dead body, getting progressively more freaked out".
- A procedural `seconds-since` operator turns stored timestamps into durations.

**Repetition control.**
- "Pre-shuffle the physical lines. Then play them back sequentially (in shuffled order). Re-shuffle when list is exhausted. Make sure not to repeat last-played line as the first line of the new shuffle."
- Optional per-variant probabilities, auto-normalised.

**Priority and interruption.**
- Fixed levels: 5 death, 4 hit reactions, 3 IGCs, 2 AI chatter, 1 efforts.
- A lower-priority request while speaking is rejected. A higher one interrupts.
- A conversation holds its priority for every participant, even one waiting for a turn, and "we usually want the entire conversation to be interrupted, not just the one character".
- Interrupted lines simply stop ("worked for us").

**Who speaks.** `'speaker` and `'listener` are magic dictionary ids, so one rule serves any pair. In a two-person exchange the roles "ping-pong" by swapping the two context values. Up to three listeners are supported, and the next speaker can itself be chosen by rule.

**What transfers.**
- The dummy criterion is the cleanest way to say "this qualified cell should *share* with the plain cell rather than replace it". That is useful for qualifier cells that should add flavour without starving the general lines.
- A shuffle-bag with the boundary rule is a deterministic, replayable improvement on our hash-per-use `pickIndex` (see synthesis rule 4).
- Whole-conversation priority is what our Listen and Watch scenes need. The scene, not the beat, owns the priority.
- Debug tooling is cited as essential: a log of which rules passed or failed and why, filterable by character. Ours is the checker plus a "why this line" trace (see rule 14).

---

## 3. Halo: combat dialogue as a typed vocalization table

**Sources:** Butcher and Griesemer, "The Illusion of Intelligence" ([slides](https://www.jmeiners.com/shamans/papers/ai/the_illusion_of_intelligence.pdf)). Halo 2's `ai_dialogue_globals` tag layout, as reverse-engineered by the modding community ([layout XML](https://github.com/General-101/Halo-Asset-Blender-Development-Toolset/blob/HEAD/io_scene_halo/file_tag/tag_interface/layouts/h2/ai_dialogue_globals.xml)). The engine's character vocalization properties ([reconstructed header](https://github.com/twist84/ManagedDonkey/blob/HEAD/game/source/ai/character_definitions.hpp)).

**Halo CE (slide 23).**
- Combat dialogue comes "from decisions and stimuli": hurt, death, saw enemy, throw grenade, seek cover.
- Requests run at "hundreds per second" and are filtered by "priority, context, uniqueness, relevance". The game picks a random dialogue type, and nearby characters can reply.
- Scale: 57 events, 166 dialogue types, 12 speaking characters, 5,147 recorded lines.
- Crucially, it was "used for flavor only".

**Halo 2 data model.** Each *vocalization* carries:
- a `priority` from an enum: none < recall < idle < comment < idle_response < postcombat < combat < status < respond < warn < act < react < involuntary < scream < scripted < death;
- flags `immediate`, `interrupt` and `cancel low priority`;
- `max combat status`;
- delays: `sound repetition delay` ("minimum delay time between playing the same permutation", in *minutes*), `repeat delay` (the same vocalization, in seconds), `post voc. delay` (speech suppressed in the speaker afterwards) and `allowable queue delay`;
- `weight`, `skip fraction` and `player skip fraction`;
- speaker and listener emotions;
- `responses` typed friend, enemy, joint or peer, each naming the follow-up vocalization.

**Selection rule.**
- *Patterns* map ~120 `dialogue type`s (sighted_new, found_unit_retreating, retreat_from_dead_leader, postcombat_inspect_body, betray, forgive…) to vocalizations.
- Patterns are qualified by a `speaker type` (subject, cause, friend, target, enemy, vehicle, squad, leader, clump…), boolean conditions (subject visible, friends present, cause is player…), hostility, damage type, danger level, attitude, actor types and spatial relation.
- **Who speaks** is therefore a first-class field. The same event is voiced differently by the victim, the attacker, a friend of the victim or the squad leader.
- A per-character `skip fraction` multiplies the global skip below priority `react`; above `react` it multiplies the speaker's *weight relative to other characters in the scene*. The header comment explains the intent: "if you hurt the arbiter… he's still going to yell at you, no matter what".

**What went wrong.** Nothing is documented. The design choice to note is that Halo kept dialogue *non-essential*, so skipping, odds and per-character silence were safe.

**What transfers.**
- A Suborned refusal, a confirmation, or a Talker giving you up carries rule information. It must never be subject to a skip fraction or odds. Only world flavour (`saw-gunfire`, `gunfire`, idle chatter) may be.
- For witnessed events, "who speaks" should be a role relative to the event (victim, actor, tie of the victim, bystander, security) chosen by score, not by whoever polls first.
- Repetition is separated by grain: per permutation over minutes, per vocalization over seconds, per speaker as a post-line silence. That maps to our per-key, per-person and district-level recency (rule 5).

---

## 4. Splinter Cell (Conviction, Blacklist)

I found no primary source on Ubisoft Toronto's bark system. The relevant [GDC 2014 talk](https://www.gdcvault.com/play/1020436/Modeling-AI-Perception-and-Awareness) is on perception, not dialogue. Two secondary observations still apply:

- An [accessibility review](https://caniplaythat.com/2013/08/23/disability-game-review-splinter-cell-blacklist/) notes that guard state was conveyed mostly by barks, so players who could not hear them lost information.
- [PC Gamer](https://www.pcgamer.com/uk/splinter-cell-blacklist-trailer-reveals-fishers-abilities-mostly-sneaking-and-shooting) noted a guard's "incessant reminders about the airfield".

**Transfer:** a bark that carries state must also exist as a state signal (our HUD pill, cones and feed). A per-guard line needs a cooldown longer than the patrol loop.

---

## 5. Firewatch (Campo Santo)

**Source:** Patrick Ewing and William Armstrong, "Do You Copy? The dialog system in Firewatch", GDC 2017. The speaker-note slides are at [GDC Vault media](https://media.gdcvault.com/gdc2017/Presentations/Armstrong_Do_you_copy.pdf) and the session at [GDC Vault](https://www.gdcvault.com/play/1024415/Do-You-Copy-Dialog-System). They cite Ruskin and Gregory as prior art and built on them.

**Data model.**
- `[Event Name]+[Target]+[Sender]+[Boolean tests] -> Responses`. Example: `OnTossed + BeerCan + Player + (LinePlayed == FALSE) -> Say "F@#$ it, I'm not the maid."`
- Facts are float comparisons on *blackboards* scoped to the game object, the player, the day, or global.
- A dialog response plays speech, shows a caption and sets a fact. A dialog-tree response fires other events and previews the choices valid in the current state.

**Selection rule.** "Sort events by number of requirements. Take first matching. Most specific response is best." "Padding lets design prioritize as needed": dummy requirements, as in TLOU.

**Memory: what the player has seen.**
- Delilah's lines about Julia branch on what Delilah has been told.
- "You're not one of those guys who is building the 'Great Plan'…" plays only when Delilah knows nothing about Julia. "What does she have?" plays when she knows Julia is unwell but not why.
- "All these different conversation lines have the same basic start, and we just drill down through game state until we find the most perfect line we have for your playthrough." Choices from the Twine prologue are written to the blackboard and select names later.

**What went wrong.**
- Interruptions: "if thread didn't finish then internal state could be wrong. Delilah would know things the player didn't." The fix was "Crit Path conversations, that couldn't be halted".
- Ref-counted start and stop events for player control broke when a branch skipped a stop.
- "New content could often disrupt old content. Events interrupting events— new meanings given to older facts; thousands of permutations to test."
- Their "Further Work" asks for a stateful *Conversation* type that sets its state on enter and exit, "clears all state" when interrupted, and knows "the state that should exist per line".
- Localisation keyed by the English string broke on every typo fix. They needed stable line IDs.

**What transfers.**
- **Knowledge is set by the line that conveys it, and only if the line completed.** This is exactly our Listen → Learned flow. The Learned fact must be written at a defined beat (the gist point, see Façade), never at scene start.
- Stable line IDs: replay logs and any future localisation should record `{personId, cellKey, variantIndex}`, never text.

---

## 6. Hades (Supergiant Games)

**Sources:** Kasavin and Korb, "Breathing Life into Greek Myth", GDC 2021 ([slides](https://media.gdcvault.com/GDC+2021/33103_Korb.pdf), [session](https://www.gdcvault.com/play/1026975/Breathing-Life-into-Greek-Myth)). The shipped game's Lua scripts, mirrored on GitHub ([`Narrative.lua`, `RoomManager.lua`, `AudioScripts.lua`, `NPCData.lua`](https://github.com/crazytuzi/LuaFramework/tree/HEAD/Hades)), quoted below by function. The slides point to People Make Games' video "The System Behind Hades' Astounding Dialogue" for more ([Kasavin endorsing it](https://x.com/kasavin/status/1329487797607043073)).

**Scale.** More than 22,000 lines. Writing was done "primarily in Google Sheets or directly into game data via Sublime Text", with "tons of variations for 'barks' w/ specific context". The slides' own list of improvements includes "improved priority system on story events".

**Data model.**
- Each NPC has `InteractTextLineSets` (story and essential), `RepeatableTextLineSets` (flavour), and `GiftTextLineSets` (the relationship track).
- Each set is a named conversation. It carries flags (`PlayOnce`, `PlayOnceThisRun`, `Priority`, `SuperPriority`, `UseableOffSource`) and requirements.
- Counted across the NPC data, the most-used requirement keys are `RequiredTextLines` (1,403), `RequiredFalseTextLines` (704), `RequiredFalseFlags` (437), `RequiredAnyTextLines` (373), `RequiredMinNPCInteractions` (231), `RequiredMinElapsedTime`, `RequiredFalseTextLinesThisRun`, `RequiredMinCompletedRuns`, `RequiredLastKilledByUnits` and `RequiredGiftLevel`. `IsGameStateEligible` checks over 500 requirement kinds.
- The dominant gate is **"which conversations have already played"**. History *is* the state.

**Selection rule** (`GetRandomEligibleTextLines`):
- Drop sets already completed this run.
- Keep the eligible ones.
- If any `SuperPriority` set is eligible, pick one at random. Otherwise pick from `Priority`. Otherwise pick at random from eligible sets **not yet in `PlayedRandomTextLines`**. If all have played, clear that record and pick from all eligible.
- `CheckAvailableTextLines` tries `InteractTextLineSets` first and falls back to `RepeatableTextLineSets` only when no story set is eligible. That is the essential-versus-flavour queue.
- About 400 sets are marked `Priority` and 99 `SuperPriority`.

**Repetition control.**
- `PlayOnce` (ever) and `PlayOnceThisRun`.
- The `PlayedRandomTextLines` cycle: use all before repeating.
- `UseableOffSource`: once the NPC's conversation for this run is done, they stop offering one. This gives the forum-observed "one conversation per NPC per run" pacing.
- Barks (`IsVoiceLineEligible`) add named shared cooldowns (`CooldownName = "SaidHelloRecently", CooldownTime = 80` shared across many cues), `ChanceToPlayAgain` (after first play), `SuccessiveChanceToPlay`, `PlayOnceFromTableThisRun`, and `RequiredPlayedThisRoom`.

**How relationships unlock lines.**
- The gift track (`GiftTextLineSets`, played via `PlayFirstEligibleTextLines`) is an ordered chain gated by gift level (`RequiredGiftLevel`) and by the previous gift line (`RequiredTextLines`).
- Interaction lines gate on `RequiredMinNPCInteractions` and on *other characters'* conversations (`RequiredTextLines = { "AchillesAboutPatroclus01" }`), so a relationship's story threads across speakers.
- Partner conversations (`line.Partner`) need both NPCs present.

**What went wrong.**
- Players reported important lines arriving late or not at all, because story sets compete in a random pool until marked `Priority`.
- `PlayFirstEligibleTextLines` iterates a Lua table with `pairs()`, whose order is unspecified. "First eligible" is therefore not well defined. Harmless in Hades; a replay bug for us.

**What transfers.**
- **Two queues and three tiers**, made explicit: essential-urgent, essential, and flavour. Flavour is only reached when no essential content is eligible.
- **"Already said" as the main gate**, with an ever-record and a per-run record.
- **Named shared cooldowns** across many lines ("SaidHelloRecently") for people who share a voice group.
- **Every "first" must be over an ordered list.**

---

## 7. Watch Dogs: Legion ("play as anyone")

**Sources:** Ubisoft, ["How Watch Dogs: Legion Told A Story Without A Main Character"](https://news.ubisoft.com/en-us/article/7b1jdUkg7ctAgg2NqW4tsy/how-watch-dogs-legion-told-a-story-without-a-main-character). Game Developer, ["How Watch Dogs: Legion's 'Play as Anyone' Simulation Works"](https://www.gamedeveloper.com/design/how-watch-dogs-legion-s-play-as-anyone-simulation-works) (Liz England, Jurie Horneman, Martin Walsh). Christopher Dragert, ["Census: The Systemic Backbone Behind Play As Anyone"](https://www.gdcvault.com/play/1027018/Census-The-Systemic-Backbone-Behind), GDC 2021 (abstract only).

**Data model.**
- *Census* generates citizens from tags (gender, fashion, occupation, income, personality) by growing a "spider-web" of facts from an anchor fact, under constraints that avoid both "procedural soup" and full stereotypes.
- Uprezzed citizens get 24-hour schedules and links to family, friends and coworkers.
- Dialogue is written per **persona**, "much more abstract concepts of a personality and a voice actor wrapped together" (England). "Every line of dialogue has to be written and recorded 20 times" (Horneman), one per persona.
- Example personas: a policeman's "much obliged", an activist's "appreciate it, fam", a grandmother's "thank you, dearie". Formant modulation stretches 20 recordings further (Walsh).

**Memory and relationships.**
- Contacts and their kin are "recast" into later missions. Citizens with enough hate for DedSec trigger revenge missions, gated by timers and counters.
- Opinions carry generated reasons (the victim likes you more, the officer less).
- Recruited heroes do not remember. Lead writer Hennessy: "we have to assume the characters are goldfish, because they only live in the moment", and "The player carries the memory."

**What went wrong.**
- "Characters like this can't carry a story on their own", so authored anchors (Bagley, Nigel Cass) carry opinion and continuity.
- Personas cannot hold opinions that might contradict procedural data.
- Too many NPC reactions overwhelmed players, so the team added a "player attention system" and cooldowns.
- Text insertion and gender agreement across about 14 languages were "harder than I expected".
- Critics: the bios "don't show up in cutscenes or in the field", and shared lines sounded written for one vigilante archetype ([CGM](https://www.cgmagonline.com/review/watch-dogs-legion-pc-review/)). The pitched voice pool became noticeable after a few hours ([Swordchomp](https://swordchomp.com/articles-feed/2020/11/16/review-watch-dogs-legion)). "Minimal personality among your army" ([TechRadar](https://www.techradar.com/reviews/hands-on-watch-dogs-legion-review)).

**What transfers.** This is the closest analogue to our voice group × temperament tables, and the failure is specific: **persona lines that ignore the person's situation read as generic.** Our need, secret and tie qualifiers are the remedy, but only if they fire often enough to be heard. Two more lessons:

- Keep the authored anchors (the Handover cast) as the story spine.
- Budget reactions per unit time (an attention system), because a simulated city produces more line requests than a player can absorb.

---

## 8. Middle-earth: Shadow of Mordor / Shadow of War (the Nemesis system)

**Primary source:** US Patent 10,926,179 B2, ["Nemesis characters, nemesis forts, social vendettas and followers in computer games"](https://patents.google.com/patent/US10926179B2/en). Secondary: [GameSpot](https://www.gamespot.com/articles/shadow-of-mordor-s-nemesis-system-was-inspired-by/1100-6423740/) on hand-written "crafted pockets", and reviews.

**Data model.**
- Each encounter writes a *faction event record* for the NPC. The faction manager "may associate a dialog identifier with the event record, based on the NPC action". Records are queued per faction member until a *showdown*: "an encounter between an NPC character and a player avatar that includes dialog".

**Selection rule.**
- At a showdown, the dialogue manager walks the stored record to locate the clip for that prior event.
- Examples from the patent: fled → "My retreat was just strategy before! This time I'll end you!"; killed the player → "Thought I killed you once already!"; burnt → "Thought I'd burned to a crisp? You're the one who's gonna burn!"
- Two more layers exist:
  - recent player activity not involving this orc (killed my bodyguard; many stealth kills → "You're getting a reputation for sneaking about");
  - current context (both alone → "So we fight alone. And you'll die alone!"; ambush after hunting).

**Memory.** The record persists with the orc ("the dialog of the non-player character will evolve as the character survives and accretes additional history"), alongside visible scars and promotion.

**Reception.** It is widely praised: "across dozens of hours of play, I didn't hear the same line of orc dialogue twice" ([Windows Central](https://www.windowscentral.com/middle-earth-shadow-war-xbox-one-review)). It also drew "same-y orc commanders" complaints over long play ([PCWorld](https://www.pcworld.com/article/3231219/middle-earth-shadow-of-war-review-impressions-pc.html)).

**What transfers.**
- **Key lines on the outcome of the last encounter with this person**, a small closed enum of past events, plus a separate "what I've heard about you" layer keyed on district-wide acts. For us those are Standing (Hero/Menace), Trail and witnessed acts.
- **Caution, not legal advice:** this patent's claims centre on NPC hierarchy, power centres and parameter changes after encounters. If Suborned adds promotion or rank among civilians alongside remembered taunts, Sky should know the patent exists.

---

## 9. Storylets and quality-based narrative: Fallen London, Sunless Sea, Emily Short

**Sources:** Emily Short, ["Beyond Branching: Quality-Based, Salience-Based, and Waypoint Narrative Structures"](https://emshort.blog/2016/04/12/beyond-branching-quality-based-and-salience-based-narrative-structures/). Alexis Kennedy, ["I've stopped talking about quality-based narrative"](https://weatherfactory.biz/qbn-to-resource-narratives/). Failbetter, ["New narrative structures"](https://www.failbettergames.com/news/new-narrative-structures). The Fallen London wiki's [Card Frequency](https://fallenlondon.wiki/wiki/Card_Frequency) page.

**Data model.**
- A *storylet* is a short unit of content with prerequisites on *qualities*: numeric variables for inventory, skills, story progress, menaces and relationships.
- Its branches change qualities.
- Fallen London's opportunity deck draws storylets as cards weighted by frequency: Rarerest 1, Rare 10, Unusual 20, Very Infrequent 50, Infrequent 80, Standard 100, Frequent 200, Abundant 500, Ubiquitous 1000.

**Selection rule.**
- QBN: everything whose gates pass is *available*, and the player chooses. Over the deck, availability is weighted random.
- Salience-based (Short): the system picks "the most applicable" piece. The most specific match wins, ties are random, and broad defaults always exist. "The author is never committed to having uniform coverage for every possible situation."
- Waypoint (Short's *Glass*): NPCs pathfind across a topic graph, and used transitions are weighted negatively.

**Memory.**
- Qualities are the memory. Failbetter's *Carousel* uses a timing quality that each storylet raises, locking old content and opening new, then resets.
- Kennedy's later critique: treating every variable alike "erases too many potentially useful distinctions". He prefers *resource narrative* where resources are scarce, reproducible and fungible.

**What went wrong.**
- QBN "tends to be uninteresting until there are a fair number of storylets in the database". The available list overwhelms at scale. Progress tracking is by hand. Players forget the causal chain from acquiring a quality to using it (Short).
- With salience, "setting the numerical weights is hard"; Short suggests "a little / a medium amount / a lot" as constants, and running thousands of randomised playthroughs to find unused or overused sequences.

**What transfers.**
- **Prefer specificity counting plus a few named tiers over tuned weights.**
- **Distinguish kinds of state** (Kennedy): a temperament is not a need is not a Trail count. Our closed slot and qualifier vocabulary already does this. Keep it typed.
- **Simulate many seeds and count**, as a checker step.

---

## 10. Wildermyth: generated heroes speaking authored comics

**Sources:** [Modding variant text](https://wildermyth.com/wiki/Modding_variant_text) and the [Writer's Guide](https://wildermyth.com/wiki/Writer%27s_Guide) on the official wiki.

**Data model.**
- Events cast heroes into named roles (`hero`, `volunteer`…).
- Lines inline their variants:
  - by personality: `<hero.bookish/loner/80:No, I'm reading/No, leave me alone/No>`. N personality stats and a threshold give N+1 lines; the last line is the fallback, used when no listed stat is highest and over the threshold;
  - by relationship: `<hero.rfln.volunteer:No, you idiot/No/No, sweetheart/No>` gives rival / friend / lover / neutral, with `rivalLevel`, `friendLevel` and `loverLevel` across 5 intensity levels.
- Each hero gets three generated history lines (Origin, Anecdote, Motivation) that set stats and seed about 3 hooks.

**Memory and state.** "History lines cannot be queried, but can apply aspects, which can be queried." Story memory lives in typed aspects and hooks, never in text.

**What went wrong.** The guide lists "number of personality-specific lines" as an open question. Writers face the coverage-versus-cost trade.

**What transfers.**
- **Relationship-kind splits inside one cell**, with a neutral fallback, map directly onto our tie kinds (lover, workmate, family, debtor, rival, regular).
- **"Text is never state."** This matches our rule that the Learned line is written by the rules from data, not by the model.
- The **stat threshold plus fallback** is how a state-banded qualifier (bravery step, resentment) should read: the band cell applies only past a threshold, else the plain cell.

---

## 11. Dwarf Fortress: thoughts, memories, legends

**Sources:** DF wiki, [Memory (thought)](https://dwarffortresswiki.org/Memory_(thought)) and [Legends](https://dwarffortresswiki.org/index.php/Legends). Tarn Adams, "Emergent Narrative in Dwarf Fortress", in Short and Adams (eds.), [*Procedural Storytelling in Game Design*](https://www.routledge.com/Procedural-Storytelling-in-Game-Design-1st-Edition/Short-Adams/p/book/9781138595309) (2019), not fetched.

**Data model.**
- Each dwarf has **8 short-term and 8 long-term memory slots**, plus unlimited core memories.
- Thoughts are grouped by category, and "only the strongest emotion in each category" gets a slot.
- After a year a short-term memory tries to promote to long-term. A revisited long-term memory has a 1-in-3 chance of becoming core, which can permanently change values and traits.
- Revisiting produces *distinct phrasing*: "felt satisfied remembering work" versus "felt satisfied at work".
- Legends is an event log per historical figure, site and artifact, rendered as prose. With "Reveal All Historical Events" off, history is hidden until an adventurer learns it from people, items or books.

**What transfers.**
- **Bounded, categorised per-person memory with strength-based eviction** is deterministic, cheap and naturally limits what a person can bring up.
- **"Remembering" is a distinct situation from "experiencing"**, and it needs its own cells.
- **History gated by who has heard it** is our premise ("information moves through people").

---

## 12. RimWorld: interaction defs, RulePack grammar, social log

**Sources:** Core def XML ([`Interactions_Social.xml`](https://github.com/RimWorld-zh/RimWorld-Core/blob/HEAD/Core/Defs/InteractionDefs/Interactions_Social.xml), [`Thoughts_Memory_Social.xml`](https://github.com/RimWorld-zh/RimWorld-Core/blob/HEAD/Core/Defs/ThoughtDefs/Thoughts_Memory_Social.xml)). The resolver's behaviour is read from a community decompile of [`GrammarResolver.cs`](https://github.com/lumin3000/rwsource/blob/HEAD/Verse.Grammar/GrammarResolver.cs); treat it as descriptive only.

**Data model.**
- An `InteractionDef` (Chitchat, DeepTalk, Slight, Insult, KindWords…) names a worker, the thoughts it leaves on each side, and a grammar for the social-log line. Example: `r_logentry->[INITIATOR_nameDef] [talkedabout] [TalkTopicLight] with [RECIPIENT_nameDef].`, with `talkedabout(p=4)->chatted about`, `talkedabout->gabbed about`, and so on.
- Rules may carry `(p=…)` weights, constant constraints on context values (equality and numeric comparisons), `tag`/`requiredTag`, and a priority.

**Selection rule.**
- *Which interaction happens* is weighted by relationship: Chitchat 1.0, DeepTalk `0.075 × compatibility curve`, Slight `0.02 × NegativeInteractionChanceFactor` (opinion and traits).
- *Which words*: among rules for a keyword, drop those failing constraints or tags, keep only the **highest priority**, then pick weighted-random with weight `base / (uses + 1)` (`SelectionWeight`). Unresolvable branches are marked and backtracked, with a hard loop cap of 1000.

**Memory and repetition.**
- The interaction leaves *memories* with diminishing stacking. Insulted: −15 opinion for 20 days, `stackLimitForSameOtherPawn` 10, `stackedEffectMultiplier 0.9`. Chitchat is cumulative but capped (`maxCumulatedOpinionOffset 10`).
- Repetition of *consequence* is damped, not just repetition of words.

**What transfers.**
- **Relationship should change which situation occurs, not only which words.** In Suborned that is the sim's job (Motive and Keel), and the words follow.
- **Priority filter, then use-decayed weight**: a clean, deterministic sub-expansion rule if we ever let templates nest (address terms, job nouns).

---

## 13. Crusader Kings III: scripted, character-scoped localisation

**Sources:** CK3 wiki, [Customizable localization](https://ck3.paradoxwikis.com/Customizable_localization) and [Localization](https://ck3.paradoxwikis.com/Localization).

**Data model.**
- Loc strings call data functions on scopes: `[ROOT.Char.GetLadyLord|U]`, `[ROOT.Char.GetSheHe|L]`, `[TRAIT.GetName( CHARACTER.Self )]`.
- *Customizable localization* entries have a `type` (character, title, faith, scheme, secret…) and a list of `text = { trigger = {…} localization_key = … }` blocks.
- Event descriptions use `first_valid = { triggered_desc = { limit … desc … } }`.

**Selection rule.**
- `random_valid = no` takes the first valid block (ordered priority). `yes` picks a random valid block each invocation.
- A block with `fallback = yes` is used when none is valid, "even though its own trigger is not valid".
- Keys can be overridden one at a time.

**What transfers.**
- **Ordered first-valid for priority, with an explicit fallback** is the simplest legible model when specificity counting is not wanted (debrief lines, summons).
- Scopes give one word list per role (`ROOT`, `SCOPE`), like our `focus` and `other`.
- Grammatical-feature functions (`GetSheHe`) are how CK3 handles gender. Our D-5 bans gendered words outright. That is simpler and avoids the agreement problem WDL hit, at a cost to naturalness. The brief already flags this as a decision for Sky.

---

## 14. Comme il Faut and Prom Week

**Source:** McCoy, Treanor, Samuel, Reed, Mateas and Wardrip-Fruin, ["Social Story Worlds With Comme il Faut"](https://cs.uky.edu/~sgware/reading/papers/mccoy2014cif.pdf), IEEE TCIAIG 2014. Also [AIIDE 2011](https://ojs.aaai.org/index.php/AIIDE/article/view/12454).

**Data model.**
- *Social exchanges* (with intents such as start friendship or start rivalry) have preconditions, initiator and responder *influence rule sets*, and accept or reject *instantiations*.
- Each instantiation is a hand-authored dialogue performance with its own preconditions and effects.
- Every performed instantiation is logged in the **social facts database (SFDB)** with a time, a *performance-realisation string* ("%i% made scandalous remarks about %rp% mother", renderable from any point of view: "you stole my watch" or "Alan stole Bill's watch"), and author-defined **labels** ("risque", "cruel").
- Backstory is pre-seeded as SFDB events with negative timestamps.

**Selection rule.**
- Volition (summed influence rules) picks the exchange.
- Among instantiations whose preconditions hold, "the most salient instantiation is selected". Salience correlates with the complexity of the precondition, and "which instantiations have recently been chosen" also counts.
- *Microtheories* group rules under a shared precondition so that large rule sets stay manageable.

**How history changes lines.**
- Templates can query the history: "You really expect me to accept your offer after %SFDB_(cruel, i, r, 7)%?" becomes "…after you made scandalous remarks about my mother?" The arguments are label, initiator, responder, and window in turns.
- `%CKB_((i,"likes"),(r,"dislikes"),"lame")%` pulls a cultural item meeting those conditions.

**What went wrong.** "CiF does not simulate hidden information: all events that occur are assumed to be immediately known about by all characters." Content is "difficult to author for", because any instantiation can appear in many social contexts.

**What transfers. This is the single most useful pattern for Suborned's history.**
- Every rulebook event the sim records (paid, pressured, refused, questioned, gave you up, saw gunfire) should carry **labels** and a **point-of-view realisation template** in the lexicon, written once by the model per event kind.
- A cell line can then carry a history slot that the expander or host fills from the person's own record, within a window.
- Unlike CiF, the record must be **per person's knowledge**, not global truth.

---

## 15. Versu (Richard Evans, Emily Short)

**Sources:** Evans and Short, ["Versu—A Simulationist Storytelling System"](https://www.cs.uky.edu/~sgware/reading/papers/evans2014versu.pdf), IEEE TCIAIG 2014. The [Praxish](https://ojs.aaai.org/index.php/AIIDE/article/view/27537) reconstruction (AIIDE 2023).

**Data model.**
- Concurrent *social practices* (a dinner, a conversation) offer affordances. Agents choose by utility over their desires.
- State is in *exclusion logic*, for example `brown.relationship.lucy.evaluation.attractive!40`.
- Relationships are **role evaluations**, stored as `Agent.relationship.Evaluated.role.Role!Value!Explanation`.

**Memory and how history changes lines.**
- "Characters remember the reason for these evaluations" ("Mr. Darcy is a bad suitor because he was rather rude at the dance").
- They make evaluations public when prompted, and **pass them on** ("Mr. Wickham tells Elizabeth that Darcy is dishonorable and she believes him"). Evaluations change the *tone* of the same action ("the Bingley sisters are less effusive").
- The UI shows each character's mood portrait, and clicking it says *why* (whom the emotion is about and what caused it).
- Text individuality comes through per-character overrides of shared actions: Brown swaggers, Lady Catherine hobbles.

**What transfers.**
- **Store the reason with every attitude**, so lines and the dossier can cite it. This matches our Learned and Talker needs.
- **Gossip as a first-class transfer of evaluations** is the Talker mechanic expressed in data.
- **Per-group overrides of a shared action** is precisely voice group × situation.
- **Expose state in the UI so lines are legible.** Our HUD Intel already does this.

---

## 16. Façade (Mateas and Stern)

**Source:** ["Structuring Content in the Façade Interactive Drama Architecture"](https://cdn.aaai.org/AIIDE/2005/AIIDE05-016.pdf), AIIDE 2005.

**Data model.**
- About 2,500 *joint dialogue behaviours* (jdbs), each a 1–5 line exchange.
- About 66% sit in 27 *beats*; a run sees about 15 beats and at most 25% of the content.
- Each beat has canonical *beat goals* plus *beat mix-ins*, which are reactions to the player's discourse acts.
- About 20 *global mix-in* progressions (marriage, the wedding photo, the brass bull…) have three tiers each, advanced by the player referring to them.

**Interruption semantics.**
- A global mix-in stops a beat goal mid-performance.
- If the beat goal had not passed its **gist point** ("an author-determined point in a beat goal's jdbs"), it must replay afterwards using "short, alternate uninterruptible dialog".
- On return, a **reestablish** jdb plays ("So, I was going to say, about Italy…").
- Mix-ins are written "as slightly tangential topics that are being jutted into the flow" and are causally independent of beats.

**What transfers.**
- **Gist point** is the missing concept in our scene shape. A Listen scene's Learned fact should fire only once the gist beat is reached. An interrupted scene before the gist teaches nothing and may replay; after the gist it counts.
- Interjections must be written to be causally independent of the scenes they can cut into.

---

## 17. Tools: Ink, Yarn Spinner, Tracery

**Ink** ([Writing with Ink](https://github.com/inkle/ink/blob/HEAD/Documentation/WritingWithInk.md)).
- Every knot and stitch has an automatic **read count** (`{seen_clue > 3}`), plus `TURNS_SINCE(-> knot)`.
- Alternatives: sequences (stopping), `&` cycles, `~` shuffles, `shuffle once`, `shuffle stopping`, `once`.
- `SEED_RANDOM(n)` makes runs reproducible.
- **Semantics given to you:** per-node visit memory for free, and declarative variation policies per block.

**Yarn Spinner** ([line groups](https://docs.yarnspinner.dev/write-yarn-scripts/scripting-fundamentals/line-groups), [node groups](https://docs.yarnspinner.dev/write-yarn-scripts/advanced-scripting/node-groups), [saliency](https://docs.yarnspinner.dev/write-yarn-scripts/advanced-scripting/saliency)).
- `=>` line groups and `when:` node groups (storylets) with `once`, `always` and expression conditions.
- Complexity score: `always` = 0, `once` +1, each expression = operators + 1.
- Strategies: First, Best, Best Least Recently Viewed, and **Random Best Least Recently Viewed** (the default). The "least recently viewed" count sorts *before* score, so freshness beats specificity among passing items.
- Custom strategies split a read-only `QueryBestContent` (which "should not change any state") from `ContentWasSelected` (which updates counts). Node-group order across files is "not defined".

**Tracery** ([repo](https://github.com/galaxykate/tracery); Compton, Kybartas and Mateas, "Tracery: An Author-Focused Generative Text Tool", ICIDS 2015).
- A grammar of symbol → list of expansions (`#animal.capitalize#`, `.a`, `.s`).
- *Actions* such as `[hero:#name#]` bind a symbol for the rest of an expansion, so one generated hero stays consistent.
- Selection is uniform random with no memory.
- **Semantics given to you:** compositional variation and within-text consistency. No conditions, no history, no repetition control.

**What transfers.**
- **Separate query from commit**, Yarn's split. It matches the engine's own `CallQuery` ("answers before the click exactly as Issue would"). The host should be able to ask "what would this person say?" for previews without advancing any counter.
- **Put the variation policy on the cell** (stopping, cycle, shuffle, once), as Ink does.
- **Never depend on undefined ordering.**
- Tracery-style binding is what our slots are. Keep them flat; don't let the model write nested grammars.

---

## Comparison at a glance

| system | line keyed on | winner | memory | repetition | relationships and history |
|---|---|---|---|---|---|
| Valve | concept + who + any facts | most criteria, ties random | rules write facts (with expiry) | use-all-before-repeat, speakonce, delays, odds | rules on the other party; followups to any/all, best score speaks |
| TLOU | logical line + context dict (speaker, listener) | most criteria; dummy criteria to share | on-play writes facts | shuffle-bag, no repeat at seam | ping-pong speaker/listener roles |
| Halo 2 | dialogue type × speaker type × conditions | priority enum, weights, skip fractions | — | per-permutation (minutes), per-vocalization, post-speech delays | friend/enemy/joint/peer responses |
| Firewatch | event + target + sender + blackboard tests | sort by requirement count, first match | blackboards scoped object/player/day/global | LinePlayed facts | what Delilah has been told |
| Hades | named conversation + requirements | SuperPriority > Priority > unplayed > replay | ever-record and per-run record | PlayOnce, cycle, named cooldowns | gift level, NPC interactions, other NPCs' lines |
| WD Legion | line × 20 personas | persona from traits | goldfish (player remembers) | attention system, cooldowns | recast kin, revenge missions |
| Nemesis | showdown × prior-event dialog id | event record lookup | per-orc event records | — | last encounter, recent acts, context |
| CiF / Prom Week | exchange × instantiation preconditions | most complex precondition, recency | SFDB with labels and POV strings | recency in salience | `%SFDB_(label,i,r,window)%` |
| Wildermyth | role-cast event; inline splits | stat threshold, fallback | aspects and hooks (not text) | — | rival/friend/lover/neutral × 5 levels |
| RimWorld | interaction def → grammar | max priority, then weight/(uses+1) | thoughts with stack limits | in-expansion use decay | interaction weight from opinion/compatibility |
| CK3 | scope + custom loc blocks | first_valid or random_valid, fallback | game state | random_valid | scope functions |
| Yarn | line/node group + conditions | strategy (least-recent, then complexity) | visited counts, once | least recently viewed | variables |

---

## Synthesis: the architectural rules worth adopting, ranked

Each rule names its sources and what it would mean in `tools/dialogue` or the host. They are ranked by how much they protect replay and readability first, then by how much variety they buy per byte of table.

1. **Specificity counting stays the core; ties are broken by a seeded hash, never by file order.** *(Valve slides 62–64; TLOU; Firewatch; CiF; Yarn "Best")* Our `lookup` takes the first of equal scores. Make ties explicit: either a checker rule that equal-score cells for one query cannot co-exist unless they are marked to share (TLOU's dummy criterion, Firewatch's padding), or a seeded pick among them. Never use Lua-`pairs()`-style or cross-file order (Hades `PlayFirstEligibleTextLines`, Yarn node groups across files).

2. **Every line carries an explicit tier: essential-urgent, essential or flavour, and only flavour may be skipped, given odds or rate-limited away.** *(Hades SuperPriority/Priority/Repeatable; TLOU priority levels; Halo priority enum and "flavor only")* Replies, confirmations, questioning outcomes, the Talker's give-up and the Learned line are essential: they carry rulebook results. `saw-gunfire`, `gunfire`, reads and idle chatter are flavour. The checker should reject any skip or odds on an essential cell. The host should never drop an essential line for a cooldown; it may delay it.

3. **A fallback cell always exists, and qualifiers only add.** *(Valve global rules; CK3 `fallback = yes`; Wildermyth's N+1th line; Short: "never committed to uniform coverage")* D-0 and D-1 already guarantee the plain cell. Extend the same rule to every future band: a bravery-step, resentment, promoted or Hero/Menace cell is optional, and its absence falls back to the plain cell without error.

4. **Variation uses a seeded shuffle-bag per key: every variant before any repeat, and no repeat across the reshuffle seam.** *(TLOU; Valve's default "use all before repeating"; Hades `PlayedRandomTextLines`; Ink shuffle; Yarn least-recently-viewed)* Today's `pickIndex` hashes each use independently and only blocks an immediate repeat, so with 3 variants it can alternate A B A B and starve C. Replacement: the c-th cycle is a Fisher–Yates permutation seeded by `fnv1a32(seed|key|c)`, use n takes `perm[floor(n/count)][n mod count]`, and the first element of a cycle may not equal the last of the previous one. It stays O(count), pure and replayable, and Latch can port it as easily as the current function.

5. **Recency at three grains: per person and key, per voice group across the district, and per speaker after speaking.** *(Halo sound repetition delay in minutes, repeat delay, post-voc delay; Hades named shared cooldowns; Valve `respeakdelay` and `delay`; WDL's audible voice pool)* The person-id key stops two people speaking in unison but not the district hearing one template from ten mouths. Add a group-level recency key (cell key *without* person id) counted in sim time. The picker should prefer the bag element least recently heard district-wide among the next k. Everything is a function of the replayed event stream, so it stays deterministic.

6. **Once-only and per-run-only are first-class flags on cells and scenes, not facts the author must check.** *(Ruskin's closing regret, slide 142; Hades `PlayOnce`/`PlayOnceThisRun`; Valve `speakonce`/`matchonce`; Yarn and Ink `once`)* Scenes (the lovers' Watch, a first Listen) and some confirmations should be `once`. The host keeps an ever-record and a per-mission record, keyed by stable line IDs.

7. **Lines write facts back, and later lines query them.** *(Valve "then" contexts with expiry, slides 37 and 68; TLOU `:on-play`; Firewatch dialog response "setting a fact"; Hades `TextLinesRecord` as the dominant gate)* Minimum set:
   - `heard/<person>/<cellKey>` (counts);
   - `told/<person>/<fact>` (what the cell learned from this person);
   - `spoke-with/<person>` (contact count).
   
   Cells can then qualify on `repeat` ("you asked me that already") or on contact count (Hades `RequiredMinNPCInteractions`). These are sim facts in the replay, never runtime randomness.

8. **History is referenced through labelled event records with point-of-view realisation strings, within a window.** *(CiF SFDB and `%SFDB_(label,i,r,n)%`; Nemesis event record → dialog id; Versu evaluation with explanation; DF legends)* The lexicon scope gets one realisation per rulebook event kind, in first, second and third person ("you leaned on me at the café" / "they leaned on Inez"). A closed history slot (for example `{last}`) in cells and debrief lines is filled from the speaker's own most recent qualifying record, by label and window. The model writes the realisations once at build time. Code chooses the record. This is how "history, not backstory" (GDD §14) becomes audible.

9. **Knowledge gating: a line may cite a fact only if the speaker knows it.** *(Firewatch "Delilah would know things the player didn't"; CiF's admitted global-knowledge simplification; DF legends revealed by people; Versu gossip)* Extend D-8 from build time (no need in cold Ask or Pay) to the host. History slots and qualified cells resolve against the speaker's knowledge record, which Keel's rulebook already models as who saw what. That makes Talkers and witnesses coherent.

10. **Conversations are first-class: they have a gist beat, own their priority, and are re-queried beat by beat.** *(Façade gist point and reestablish; TLOU whole-conversation priority and interruption; Valve followups queried at callback time; Firewatch "Conversation" type in Further Work)* For scenes:
    - add `gist` (the beat index after which the Learned fact counts);
    - optionally add `resume` beats;
    - evaluate each beat's conditions (Alert, both people present, focus not fled) when it is due;
    - a failed condition ends the scene. Before the gist nothing is learned; after it the fact lands.
    
    All of this is deterministic given the sim.

11. **Who speaks is chosen by role relative to the event and by score, not by order.** *(Halo speaker types subject, cause, friend, target, squad, leader; Valve `any`/`all` followups where the best match speaks; TLOU speaker/listener ping-pong)* For crowd events (`saw-gunfire`, a guard falls, a Talker questioned in public), candidates are people within sight or earshot. Score each by role (tie to the victim > security > bystander) plus cell specificity, take the best, tie-break by seed, and cap at k speakers.

12. **A reaction budget per time window, spent by tier.** *(WD Legion "player attention system" and cooldowns; Halo hundreds of requests per second filtered; Splinter Cell "incessant reminders")* The scene feed and speech bubbles get a budget, for example at most N flavour lines per 10 s of sim time. Essential lines bypass the budget; flavour queues briefly then drops. The budget counts in sim time, so replays match.

13. **Personas need situational hooks, or they read as one voice; story weight sits with authored anchors.** *(WD Legion reviews and "goldfish" admission; Wildermyth hooks; Versu per-character overrides)*
    - Measure how often a played line is *qualified* (need, secret, tie, history) versus plain, per group, over simulated runs.
    - Set a floor, for example ≥ 25% of a person's essential lines qualified once their need is known.
    - Spend model budget on qualified cells and history slots before more plain variants.
    - Keep the Handover cast and their `text.json` as the anchor.

14. **Audit by simulation, not just by schema.** *(Short: thousands of randomised playthroughs; CiF trace and n-gram analysis; Firewatch "thousands of permutations"; TLOU rule-pass logs)* Add a checker pass over many seeds of the people sim that reports, per group:
    - variants never heard;
    - the most-repeated template per in-game hour, district-wide;
    - the share of qualified lines;
    - essential lines dropped (should be 0).
    
    Make the "why this line" trace (query facts, candidate cells, scores, winner, bag position) a host debug overlay, the way Naughty Dog filtered by character.

15. **Stable line identity, closed and typed facts, and text that is never state.** *(Firewatch loc-by-English failure; Ruskin's flat fact dictionary and interned symbols; TLOU three criterion types; Wildermyth "history lines cannot be queried"; Kennedy on undistinguished qualities)*
    - Each expanded line has an ID (`personId/cellKey/variant`) and replay logs record IDs.
    - Facts are a closed, typed namespace from the rules files (unknown = reject).
    - Criteria are limited to existence, equality and integer range.
    - Nothing ever parses generated text back into state.

---

## Anti-patterns

- **Undefined ordering in "first match".** Hades iterates Lua tables with `pairs()`, and Yarn's node-group order across files is undefined. Either breaks replay.
- **Ad-hoc "already said" variables** that every rule must remember to check. This is Ruskin's own regret; use first-class once and per-run flags.
- **Global-truth dialogue.** Lines citing facts the speaker could not know: CiF's admitted simplification, and Firewatch's interrupted threads leaving Delilah knowing too much.
- **Odds and skip on information-bearing lines.** Halo's skip fractions were fine only because its combat talk was "flavor only".
- **Persona-only voicing** that ignores the person's situation. WD Legion's recruits sounded alike, and their bios never reached the field.
- **Unbudgeted reactivity.** WD Legion overwhelmed players until an attention system and cooldowns went in; Blacklist's guard repeated the airfield line incessantly.
- **Hand-tuned global weights as the main control.** Short notes they are hard to set. Prefer counting criteria, plus a few named tiers and a stated tie rule.
- **Interrupts without gist and resume semantics.** Firewatch's broken state and Façade's need for alternate uninterruptible and reestablish lines.
- **Text as state.** Querying or parsing rendered lines; Wildermyth forbids it by design.
- **Keying by display string.** Firewatch broke its localisation on every English typo fix.
- **Tools built for an imagined writer.** Ruskin's unused visual tool, and Firewatch's graph editor. Our "writer" is a model plus the checker. The equivalent mistake is a table shape the model fills badly, so judge the shape by checker failure rates per round.
- **Small pools stretched across many speakers.** WD Legion's pitched voices became audible; Shadow of War's commanders were called "same-y" over long play. The text equivalent is one template heard from many mouths, which rule 5 addresses.
