# Dialogue craft and lore without exposition (brief 012, research strand 3)

Gathered 2026-10-16 for Fable (dialogue designer), Motive (temperament voices) and Keel (critic). Inputs read first: `briefs/011/fable-text.md`, `briefs/012/fable-dialogue-generator.md`, and the 740 spoken lines in `contracts/missions/handover/text.json`.

**On the sources.** Writers' own accounts are thinner online than their reputations suggest. Many GDC talks are paywalled, and much of the coverage is secondhand. Each claim below is marked as one of three kinds:

- a **developer's statement**, with a link;
- a **secondary report**: press or a fan site describing what a developer said;
- **(observation)**: my reading of the game itself, with no developer account behind it.

The rules drawn from each source are mine. They are written for our constraints:

- text, not voice;
- 1 to 12 words a line (checker D-2);
- tables by role × temperament × situation, expanded by seeded code;
- no gendered words in the tables (D-5);
- no AI at runtime.

---

## 0. What the current lines do well and badly (the measured baseline)

I counted the Handover's lines with a scratch script (`out/scratch/stats.ts`, not committed).

- **60% of all lines are exactly two sentences** (444 of 740), usually as "Fragment. Fragment." The rate is 77% for the driver, 72% for the guard and the rivals, and 90% for the watcher. That one rhythm runs through every voice, so the driver, the guard and Inez sound like the same writer in different hats. Only the Enforcer (33%) and the crowd (42%) escape it. This is the biggest single reason the lines feel written rather than spoken.
- **On-the-nose feeling words show up in the replies:**
  - "I'm still so frightened."
  - "You keep helping. I feel safer."
  - "Good. They trust us a little more."
  - "I hate this."

  Each one tells the player a feeling that the situation already shows.
- **The Loyal "report" reply has converged across roles.** "This goes in my log" is said by both the driver and Inez. In all, 31 lines are built on report/log/noted/book. The mechanic (a `suspicious` outcome) is right, but three different jobs say it in one voice.
- **The best writing in the file is in `scenes`, not `dialogue`.** In the overheard Watch on Inez, the guard says "Two minutes is plenty." and Inez answers "Hush, and drink it before it cools." Those two lines carry the affair, the routine and the timing without stating any of them. They are the house model: people doing their jobs, wanting something, saying less than they mean.

---

## 1. Conversation feel: what the games teach

### Disco Elysium (ZA/UM)

- **Accounts (secondary):** Robert Kurvitz wanted players to stop noticing they were reading, avoiding "chunks of expository lines". Dialogue was "aggressive and personal", in a Twitter-like feed, with key facts repeated by different skills ([RPG Codex](https://rpgcodex.net/article.php?id=11309), [Wikipedia](https://en.wikipedia.org/wiki/Disco_Elysium)). Helen Hindpere: "we were writing too much" and had to "stay in the quest's word limit" ([GamesHub](https://www.gameshub.com/news/features/disco-elysium-narrative-writing-process-18597/)).
- **(observation)** Each skill is a goal with a register: Drama calls you "sire", Half Light speaks in alarm, Electrochemistry only wants. Kim Kitsuragi deflects with procedure (the notebook) rather than arguing ([Wikipedia](https://en.wikipedia.org/wiki/Kim_Kitsuragi)).
- **Rules for us:**
  1. A voice is a goal plus a register, not a word list. Mercenary wants the price, Fearful wants it over, Loyal wants it on record, Idealist wants it to mean something.
  2. A feed of short lines works when each line is a move, not a report.
  3. Kim's deflection is our Loyal guard: he takes a note rather than saying "I disapprove". "Unusual help. I'll note the time." is already right.

### Oxenfree (Night School Studio)

- **Developer accounts:** Adam Hines on interruption: "you just put a ton of bookmarks in the conversation… it's just a lot of writing", and "The freedom of being able to skip it is worth so much more than" any joke ([Game Developer](https://www.gamedeveloper.com/design/designing-the-drunken-dialogue-puzzles-in-night-school-studio-s-i-afterparty-i-)). Interrupted talk resumes later, which needs "many different versions of the same lines" ([interview](https://newsreports.com/poltergeists-and-teen-drama-oxenfree-interview-with-adam-hines/)). The aim was "individual people with interesting vocal tics and cadences", never "too theatrical or too polished"; characters "stop, back up, and try again" to show their state ([Crossplay](https://www.crossplay.news/p/how-the-developers-of-oxenfree-managed)). Sean Krankel's test: "What is the player doing while we have them?" ([GDC Vault](https://gdcvault.com/play/1024270/contactUs)).
- **Rules for us:**
  1. **Front-load the meaning.** The feed collapses an exchange to its last beat and players click away, so a line's first three words carry its point and the last beat stands alone.
  2. **Self-repair is the Fearful tell**, in place of feeling words: "I... no. Leave me out of it." beats "I'm still so frightened."
  3. **Individual tics over group dialect.** Write how *this* waitress talks, not how 1950s waitresses talk.

### Firewatch (Campo Santo)

- **Accounts:** Sean Vanaman: "sometimes I'll just have them say 'OK,' but I spend lots of time directing them so they know what weight that 'OK' carries" ([GamesBeat](https://gamesbeat.com/?p=114178)). Silence is almost always an option, on a timer ([MobyGames](https://www.mobygames.com/game/76982)).
- **(observation)** Henry and Delilah each dodge their own wound with a joke; the player sees the wound in what they won't discuss.
- **Rules for us:** In text, only the setup can load a one-word line. In the call scene Inez says "Please, come over. Quickly. Quietly." and the guard answers "Five minutes.", and the shortness is the devotion. Refusal and silence are replies too: "Hm." or "Not here." is a legitimate entry.

### Kentucky Route Zero (Cardboard Computer)

- **Accounts (secondary):** Jake Elliott framed choices as shaping how the player understands the past and the characters, not what happens next ([Haywire](https://haywiremag.com/columns/opened-world-kentucky-route-zero-act-iii/)).
- **(observation)** Institutions (the Bureau of Reclaimed Spaces, the power company) speak a deadpan, absurdly polite register that carries the debt politics with no speech about capitalism.
- **Rules for us:** The city's institutions are characters, and their politeness is the critique. Lines can be expressive rather than decisive: the Fixer's reaction shows who the Fixer is without changing the outcome.

### Night in the Woods (Infinite Fall)

- **Accounts:** Scott Benson wrote "almost all" of the dialogue: "the characters are us, and people we've known" ([Cliqist](https://cliqist.com/2017/03/29/night-in-the-woods-curious-cat-qa-scott-benson/)).
- **(observation)** The town's decline is never explained; it shows in jobs (Bea running the family hardware shop instead of college). Characters talk past each other, and the key line is often a change of subject.
- **Rules for us:** Work is the cheapest worldbuilding: trouble said in job nouns (tables, bay, door, shift), not system nouns (debt, need). Changing the subject is subtext.

### Pentiment (Obsidian)

- **Accounts:** Typefaces carry class: educated hands are quick and clean, less educated ones slow and misspelled, printers' speech is set as type ([GameBanshee, from PC Gamer](https://www.gamebanshee.com/96xr8), [Lettermatic](https://lettermatic.com/custom/pentiment)). A font is Andreas's *perception* and changes with it: Baron Lorenz moves from cursive to Andreas's own hand. Hannah Kennedy: fonts give "different flavours of voices" ([PCGamesN](https://www.pcgamesn.com/pentiment/art-fonts-story)).
- **Rules for us:**
  1. With no voice acting, syntax is our typeface: officials in complete passive sentences, working people in clipped active ones, the cell in bare fragments slightly foreign to the period.
  2. Register shifts when a relationship does: in `kept` cells the guard drops from "citizen" to no address at all.
  3. **Glyph, note:** a distinct type treatment for the state's voice would do in the HUD what Pentiment does in ink.

### Planescape: Torment (Black Isle)

- **Accounts:** Chris Avellone starts an NPC from "what you want the player to 'feel' in that location" ([Q&A repost](https://www.starwarsrp.net/ams/qa-with-planescape-torments-writer-npc-creation-and-more.838)); keeps "a list of key words and situations" that make a companion "pipe up" ([GameBanshee](https://www.gamebanshee.com/ku9rq)); and on theme: "You don't want to hammer it home too much" ([GameBanshee](https://www.gamebanshee.com/kahp)).
- **(observation)** Sigil's cant ("berk", "cutter", "the dark of it") is rationed: locals use it, the Nameless One mostly doesn't.
- **Rules for us:** A per-role trigger list is the "pipe up" list (the Tech on wiring and fixed routines: "It took the orders. It always does."). Setting vocabulary belongs to locals, not the cell; invented institutional terms (the Office, the list, the marked block) do the job fake-period slang would.

### Baldur's Gate 3 (Larian)

- **Accounts (secondary):** Adam Smith and Chrystal Ding described a huge reactivity matrix; the companions began as one-line pitches ([Yahoo/PC Gamer](https://malaysia.news.yahoo.com/larian-gave-baldurs-gate-3-022539159.html)). Players found walking banter thin ([Steam](https://steamcommunity.com/app/1086940/discussions/0/4140565152677965582)).
- **Rules for us:** Start a voice from a one-line pitch and keep it. React to the place and the act, not the plot: the Scout's "The square looks ordinary. That's the trouble." is that kind of line. Short approval tags ("Astarion approves") do more than long banter (observation).

### The Last of Us (Naughty Dog)

- **Accounts (secondary):** Druckmann and Straley "used music rather than dialogue to express emotions" ([TheSixthAxis, "Less is more"](https://www.thesixthaxis.com/?p=185590)). Druckmann kept a scene of Joel coldly shutting Ellie down because her vulnerability is the point; Ashley Johnson improvised a shove in it ([commentary summary](https://vgfacts.com/game/thelastofusparti)).
- **(observation)** Optional conversations are opt-in. Ellie's bad puns recur, and the bond grows in Joel's tiny reactions, never a declaration.
- **Rules for us:** If an action or system shows it, cut the line. The spill shows the guard leaving, so his line is about the coffee ("Nobody's hurt. Just coffee."). Bonds show as small repeated rituals.

### Mass Effect (BioWare)

- **(observation; no developer account found)** The wheel shows a paraphrase and the spoken line differs, just as our `actions[verb].does` differs from `dialogue`. Overheard Citadel conversations build the world without involving the player ([PopMatters](https://www.popmatters.com/122221-building-worlds-with-words-in-mass-effect-2-2496178897.html)). The elcor prefix their tone ("With sincere enthusiasm: …"); the hanar say "this one". A grammar habit is a culture.
- **Rules for us:** The card says what it costs; the line says who is speaking. The clockwork patrol never says "I", only the passive, which tells the player what robots are here (clockwork, not minds) without a codex entry.

### Citizen Sleeper (Jump Over The Age)

- **Accounts:** Gareth Damian Martin wanted a game "about what it's like to try to build a life in capitalism", about labour and "being exposed to risk" ([Inverse](https://inverse.com/gaming/citizen-sleeper-gareth-martin-interview)).
- **(observation)** People speak plainly about shifts, debts and favours; the corporation that owns your body is felt through contracts, never villain speeches.
- **Rule for us:** precarity speaks in quantities and time ("my shift", "the rest", "by Friday"), which our Mercenary and Fearful cells can use.

### Hades (Supergiant)

- **Accounts:** More than 21,000 voiced lines, with Hypnos alone giving 75 responses to how you died ([Nintendo Wire, Supergiant infographic](https://nintendowire.com/news/2020/12/30/hades-has-over-300000-words-of-voiced-dialogue-heres-a-handy-breakdown-of-who-speaks-most-and-least/)). Greg Kasavin and Darren Korb's GDC talk covered "finding the unique voices" ([GDC Vault](https://gdcvault.com/play/1026975/Breathing-Life-into-Greek-Myth)).
- **(observation)** Lines are one to three short sentences about the specific thing that just happened, with steady tics (Hypnos oblivious, Thanatos curt).
- **Rules for us:** Specificity beats volume. Spend variants where repetition is highest: acks, `run-seen`, the patrol and `crowd-state`.

---

## 2. Linguistic and screenwriting principles, applied

### Grice's maxims, and flouting them

- **The theory:** Grice's cooperative principle has four maxims ([overview](https://en.wikipedia.org/wiki/Cooperative_principle)):
  - Quantity: say as much as needed, no more;
  - Quality: say what you believe true;
  - Relation: be relevant;
  - Manner: be clear and brief.

  Characters who obey all four sound like manuals. Implicature, meaning that reaches the listener without being said, comes from *flouting* a maxim on purpose, so the listener has to infer why.
- **Mapping to our temperaments.** This is the most generator-friendly idea in the strand, because each temperament can own one flout:
  - **Mercenary flouts Relation.** Whatever you ask, the answer is about price. Example: you ask a favour, they say "Favours cost. Ask again with money."
  - **Fearful flouts Quantity.** Too much, repeated, or broken off: "Okay. Okay. I'll call him."
  - **Loyal flouts Manner by hiding in procedure.** They answer a human question with a form: "Name and address?"
  - **Idealist flouts Relation the other way.** They answer the question behind the question: "What's it for?"
- **The state flouts Quality.** "Walking is encouraged" means "running is punished". Everyone knows it, and that shared knowledge is the coercion.

### Subtext and the iceberg

- **The idea:** Hemingway's iceberg principle: if the writer knows what is left out, the reader will feel it ([overview](https://en.wikipedia.org/wiki/Iceberg_theory)). Lucas Pope applies it in games: keep dialogue sparse and "rely on the player's imagination to fill in the blanks". The family screen is "a few dots with some text" so players project onto it ([Game Developer](https://www.gamedeveloper.com/design/designing-the-bleak-genius-of-i-papers-please-i-)). Tynan Sylvester (RimWorld) calls it apophenia: players supply "the meaning and pathos" if the simulation shows only the "moving balls" ([Stanford GDT](https://gdt.stanford.edu/how-do-you-create-apophenia/)).
- **Rule:** every person's line should point at one fact it does not state. The lovers' secret is ideal: "Not now, Inez. I'm on duty." says it all if the player has Listened, and nothing if they haven't.

### Turn-taking, interruption, overlap, ellipsis

- **The idea:** Conversation analysis (Sacks, Schegloff and Jefferson) shows that real talk is built from **adjacency pairs** (question and answer, offer and acceptance or refusal) and that speakers predict when a turn will end ([turn-taking](https://en.wikipedia.org/wiki/Turn-taking), [adjacency pairs](https://en.wikipedia.org/wiki/Adjacency_pairs)). The second part of a pair can be *dispreferred*: a refusal, or a question back. Dispreferred seconds come with delay, hedges and accounts, so a refusal is longer than an acceptance. Ellipsis is normal: the answer to "Where's the car?" is "The bay.", not "The car is at the bay."
- **Rules:**
  - A `refused` or `suspicious` reply gets a hedge or an account.
  - A `turned` reply can be one beat.
  - Never repeat the question's words back in full.
  - Interruption in text is an em dash or a break-off ("I... no.").
  - In scenes, let one speaker finish the other's line now and then. That is how the lovers' intimacy can show.

### Register and code-switching

- **The idea:** Brown and Levinson's politeness theory: *negative politeness* (deference, impersonal forms, the passive, apology) lets the powerful impose while appearing not to ([overview](https://en.wikipedia.org/wiki/Politeness_theory)). Orwell, in "Politics and the English Language", described how official euphemism and the passive voice hide an agent: "pacification" for burning villages ([text](https://www.orwellfoundation.com/the-orwell-foundation/orwell/essays-and-other-works/politics-and-the-english-language/)).
- **The rule this gives us:**
  - **The city's voice is agentless, passive and grateful in advance:**
    - "Running is reported." (by whom?)
    - "Thank you for walking."
    - "Your cooperation is appreciated."
  - **People code-switch** ([code-switching](https://en.wikipedia.org/wiki/Code-switching)). The guard uses officialese at the door ("State your business or move along.") and plain speech with Inez ("Not now, Inez. I'm on duty."). The gap between his two registers is his character.

### Idiolect

- **The idea:** an idiolect is a person's own way of using the language ([overview](https://en.wikipedia.org/wiki/Idiolect)). In games it is a short list:
  - one or two pet phrases;
  - one syntactic habit;
  - one field of vocabulary.

  Wildermyth builds this into its text tokens. `<hero.bookish/loner/80:No, I'm reading/No, leave me alone/No>` gives the bookish hero and the loner their own refusal, and everyone else the plain "No" ([Wildermyth wiki](https://wildermyth.com/wiki/Modding_variant_text)).
- **Rule:** the fallback is the plainest line, and flavour lives in the qualified variants.

### Answering a question with a question

- **The rule:** It is a dispreferred second that takes back control, so use it where power is contested. Pressure replies are the place:
  - "You'd tell the Office about us?" (Idealist Inez)
  - "Threatening an official's driver?" (Loyal driver)
- **The limit:** Don't use it for `turned`, because the player needs a clear yes.

### "As you know, Bob"

- **The idea:** Characters telling each other what both already know, for the audience's benefit ([TV Tropes](https://tvtropes.org/pmwiki/pmwiki.php/Main/AsYouKnow)). David Mamet's memo to the writers of *The Unit*:
  - "THE CAMERA CAN DO THE EXPLAINING FOR YOU";
  - every scene must answer "WHO WANTS WHAT?";
  - scenes of two people "TALKING ABOUT A THIRD" are dead.

  ([memo excerpts](https://nofilmschool.com/2010/10/david-mamet-drama-a-memo-the-unit-writers))
- **Rules for us:**
  - The briefing and the action cards are our camera: they explain, so lines needn't.
  - The cell never tells each other the plan. "Nobody gets hurt today. That is the plan." is Bob.
  - Inez never tells the guard "you're the door guard and I'm in debt".

### Why written game lines sound stilted, and the fixes

Elmore Leonard's tenth rule: "Try to leave out the part that readers tend to skip". His summary rule: "If it sounds like writing, I rewrite it" ([reprint](https://www.writingclasses.com/toolbox/tips-masters/elmore-leonard-10-rules-for-good-writing)). The stilted patterns, each with its fix:

| symptom | in our file | fix |
|---|---|---|
| Complete sentences with every subject | "I'll be reporting this, I'm afraid." | Drop subjects where the speaker is obvious ("Reporting this. Sorry.") or keep one for status (officials keep it) |
| Names in every line | Guard: "Not again tonight, Inez." is fine; repeating the name in every summons line is not | Use a name once per exchange, at the opening, or for intimacy or threat |
| On-the-nose feeling | "You keep helping. I feel safer." | Show it in behaviour or a change of plan: "I've started sleeping again." |
| Restating the event | "You paid my debt? I'm yours." | React to what the event caused, not to the event |
| One house rhythm | 60% two-fragment lines | Mix lengths by speaker: one word, one long run-on, a question, a broken-off line |
| The mechanic in the mouth | "Good. They trust us a little more." | Leave the mechanic to the result line and HUD; the person speaks the world |

---

## 3. Lore delivery in retro-futures and dystopias

### Fallout (Interplay / Bethesda)

- **(observation, with secondary support)** The 1950s atomic optimism is carried by three things:
  - corporate and state slogans (Vault-Tec's "Prepare for the future!");
  - posters and adverts in the world;
  - robot servants with butler politeness.

  All of it sits against brutal outcomes ([GameBanshee essay](https://www.gamebanshee.com/kx6qw)). The joke works because the ad voice never admits anything has happened.
- **Rule:** the optimistic surface lives in **artefacts and institutions**, never in people's mouths as sincere speech. If a person quotes a slogan, it is irony or fear.

### BioShock (Irrational)

- **Accounts:** Ken Levine said the main storytelling is "what you see", that "every AI should tell a story", and that diaries tell you about places you haven't seen yet ([GameBanshee Q&A](https://gamebanshee.com/fh4c)).
- **(observation)**
  - Audio logs are people talking to someone else, or to themselves, about a problem. They are never addressed to the player.
  - Ryan's PA broadcasts and the ads ("No gods or kings. Only man.") are the city's voice.
  - The logs are the people's voice, and the gap between the two is the plot.
- **Rule:** overheard material is addressed to someone in the world. Our `scenes` already do this (the guard and Inez talk to each other, never to the Scout). Keep it that way in generation.

### We Happy Few (Compulsion)

- **(observation, with secondary support)**
  - It is the closest model to our city: an alternate 1960s England kept cheerful by a drug (Joy) and by TV host Uncle Jack's constant broadcasts.
  - Non-conformists are "Downers", a cosy word for an enemy of the state ([Twinfinite](https://twinfinite.net/features/we-happy-few-is-much-different-bioshock/)).
  - The coercion is in the *diminutives and the cheer*: constables and townsfolk turn on you for not smiling.
- **Rule:** the state names its enemies with mild, friendly words. Our patrol says "citizen", not "suspect". A proposed lexicon line: an offender is "unsettled" or "out of step", never "criminal". **This is a proposal for Sky; it is not canon.**

### Atomic Heart (Mundfish)

- **(secondary)** It builds a Soviet techno-utopia on "real Soviet propaganda imagery", then lets its service robots turn ([TheSixthAxis preview](https://www.thesixthaxis.com/2023/01/17/atomic-heart-preview-how-to-solve-a-problem-like-evil-soviet-robots/)).
- **The warning:** reviewers singled out the English protagonist's "decidedly 21st Century phrases" in a 1955 setting as immersion-breaking ([KWTX review](https://www.kwtx.com/2023/02/20/atomic-heart-1950s-soviets-making-robo-apocalypse-review/)).
- **Rule:** our cell are *meant* to sound off-period, so they must be off-period in a controlled, consistent way (rule 12 below). The Atomic Heart failure is modern idiom leaking in at random.

### Papers, Please (Lucas Pope)

- **Accounts:** "The bureaucratic structure is something that fits naturally." Bulletins were "basically ignored" when someone was talking, so Pope kept them apart; the state stays "fairly generic" ([Game Developer](https://www.gamedeveloper.com/design/designing-the-bleak-genius-of-i-papers-please-i-)).
- **(observation)** The state's voice is the stamp, the bulletin and "Glory to Arstotzka", a slogan everyone must say back. Entrants speak in fragments and pleas.
- **Rules:** Never put a rule and a human line on screen together; the player reads one and skips the other, so the feed should not stack a toast on a reply. A ritual phrase that must be returned is the cheapest way to show coercion.

### Dark Souls, Outer Wilds, Return of the Obra Dinn

- **Dark Souls.** Miyazaki leaves interpretation to players, calling it "in some ways an incomplete game" they complete (secondary, [PC Gamer](https://pcgamer.com/miyazaki-says-dark-souls-hides-no-more-secrets); the childhood-reading story is unconfirmed). Item text: what the thing is, then one sentence of who used it. **Rule:** `targets[].about` follows that form, function then one human fact, as "Door guard. Takes his coffee at Inez's café before he goes off shift." already does.
- **Outer Wilds (observation).** Nomai texts are named colleagues disagreeing and joking; lore is a by-product of shop talk. **Rule:** overheard scenes are shop talk with a problem in them.
- **Return of the Obra Dinn.** Accents "provide gameplay-critical clues" ([CogConnected](https://cogconnected.com/2018/05/developer-gives-update-return-obra-dinn/)). **Rule:** in text, idiolect is the accent. Each role should be identifiable from one line with the speaker label hidden: a good checker test for Keel.

### Disco Elysium's world

- **(observation)**
  - Revachol's history arrives through people's grievances and through objects the skills notice.
  - It never comes as a lecture. Even the political "visions" are a character's obsessions, not a narrator's.
- **Rule:** lore enters only through somebody's interest in it.

### 3.4 How the city talks, versus how people talk

Proposed registers, for the GDC §15 style contract to adopt or reject. **The coined terms are proposals awaiting Sky.**

| speaker | grammar | vocabulary | example (existing or proposed) |
|---|---|---|---|
| Clockwork patrol | no "I"; passive; present tense; thanks in advance; repeats verbatim | citizen, route, sweep, encouraged, reported, thank you | "Citizen. Walking is encouraged. Running is reported." (existing) |
| Signage, notices, chime (proposed) | imperative softened to a wish; numbers spelled out | punctuality, order, the Office, handover | "Handover at the half hour. Thank you for your punctuality." |
| Watcher (a person on a handset) | log syntax: place, then what was seen | camera two, the steps, log it | "Camera two. Runner at the steps." (existing) |
| Guard on duty | full officialese, second person | state your business, the Office, post | "State your business or move along." (existing) |
| Guard off duty, to Inez | short, plain, warm | first name, minutes | "Two minutes is plenty." (existing scene) |
| Working people | clipped active sentences; job nouns | tables, bay, shift, lender, rent | "Car at the bay. Engine warm." (Scout reading the driver) |
| Officials (Vale) | subordinate clauses, titles, threats as procedure | the Council, minutes, answer for | "You will answer to the Council for this." (existing) |
| The cell | fragments; no honorifics; plain modern syntax with no period courtesy | functional nouns | "Looped. I'm tied here." (existing) |

**Robot phrases.** A clockwork patrol should have a *closed phrasebook* of 15 to 25 phrases, reused exactly. This is the one voice where repetition is in character: verbatim repeats are how the player learns it is a machine. Exempt the patrol from D-7 (no repeats) *across events*, and keep variety only inside a cell.

---

## 4. Generated dialogue that still sounds human

- **Kate Compton (Tracery).** "10,000 bowls of oatmeal": endless outputs that are mathematically different and perceived as the same ([Emily Short](https://emshort.blog/2016/09/21/bowls-of-oatmeal-and-text-generation/), [Tracery paper](https://publications.graphics.tudelft.nl/rails/active_storage/blobs/redirect/eyJfcmFpbHMiOnsibWVzc2FnZSI6IkJBaHBBZzBJIiwiZXhwIjpudWxsLCJwdXIiOiJibG9iX2lkIn19--7d151c9ad24dc4d070b4e3720f9fa152c1865016/F978-3-319-27036-4_14.pdf)). **Rule:** variants in a cell differ in **stance** (deflect, bargain, comply, threaten), not synonyms. "Count it twice. Then I'm yours." and "Cash is kind. Consider it done." both bargain; the third should ask for more.
- **Emily Short.** The test is whether generation "is connected to anything mechanical"; reflected state should be "juicier" than "Cowgirl: ON"; procgen is "often at least as labor-intensive" (same post). **Rule:** every slot carries state the player cares about (need, tie, workplace), never a decorative adjective. The Learned result line is our "Cowgirl: ON"; the person's line is the juicy version.
- **Bruno Dias (Voyageur).** "Loose sentences and phrases that the game would recombine later", each working "within a myriad different contexts", never "a lesser option that competes with fully authored material" ([sub-Q](https://sub-q.com/making-interactive-fiction-using-generative-prose/)). **Rule:** generated people get their own context (street life, their ties) and the Handover's people stay the bar (D-12).
- **Elan Ruskin (Valve).** The most specific matching rule wins, general rules fall back, exchanges break off if a speaker is attacked: "a strong impression of situational awareness" ([summary](https://emshort.blog/2012/03/16/gdc-2012-talk-on-dynamic-dialogue/), [GDC Vault](https://www.gdcvault.com/play/1015528/AI-driven-Dynamic-Dialog-through)). That is our `lookup()`. **Rule:** spend qualified cells where context is richest (a need, a secret, a tie).
- **RimWorld (Ludeon).** The GrammarResolver fills `{PAWN_pronoun}`, `{PAWN_possessive}` and `{PAWN_objective}`, and makes a "best guess" for bare symbols ([wiki](https://rimworldwiki.com/wiki/Modding_Tutorials/GrammarResolver)). Our tables ban gendered words, so write lines that need no pronoun at all.
- **Wildermyth (Worldwalker).** Personality variants with a plain default (`<hero.bookish/loner/80:…/…/No>`), relationship variants (rival, friend, lover, neutral), and pre-written scenes recast with your heroes ([wiki](https://wildermyth.com/wiki/Modding_variant_text)). A modder warns that gender needs "text splits for every single pronoun" ([Steam](https://steamcommunity.com/sharedfiles/filedetails/comments/2533737342)). **Rule:** our tie-kind scenes are its relationship tokens; the plain default must suit every temperament; avoid pronouns rather than split them.

### Slot pitfalls, and templates that survive any filler

The "Needs debt." failure comes from a template like `"Needs {need}."` filled with a noun that wants an article or a verb. Rules for every template:

1. **Slots are whole noun phrases, filled from lexicon forms, never built in the template.**
   - Use `{own}` ("my debt", "my rent"), `{met}` ("the debt's gone") and `{noun}` ("the lender").
   - Never write `"my {need}"` or `"{need}'s"`.
   - The lexicon already has `noun`, `own` and `met`; enforce that templates use only those.
2. **No articles, plurals or verbs agree across a slot.**
   - "a {job}" breaks on "an usher".
   - "{noun} is" breaks on "the twins".

   Put the slot where agreement doesn't matter: at the end of a clause, after a preposition, or as a vocative.
3. **Test each template with the shortest filler, the longest filler and a plural.** Add a fuzz check to D-3: expand every template with all lexicon forms and run the D-2 length check and a tiny agreement check (no "a" before a vowel, no "is" after a plural form).
4. **One slot per line, two at most.** Lines with three slots read like forms being filled in, which is the state's voice, not a person's.
5. **The slot is never the point of the line.**
   - "Lender's man came by. Said I'm square." works for any need if `{met}` replaces the second sentence.
   - "{met}. Nobody does that free. What's it for?" survives any filler because the filler is a complete statement on its own.
6. **No address term where the line is already intimate.** Address (`{address}`) only at the start of an approach line, never inside a reply.

---

## 5. Style guide: 22 rules for The Suborned's generated dialogue (ranked)

Each rule can be applied by a checker or a reviewer. Sources are in brackets.

**Tier 1: readability and truth**

1. **The outcome reads at a glance.** A reply's first three words, plus the result line, tell the player yes, no, not yet or reported. Subtext rides on a legible answer, never replaces it. Turned: "Fine. Where's the car?" Reported: "That goes to the Office." [GDD readable > rich; Pope; Hines]
2. **Who wants what.** Every person line moves toward what the speaker wants now (the price, an end to it, the record, a reason), never reports their state: "Favours cost. Ask again with money." [Mamet]
3. **No words for the speaker's own feelings** (frightened, safer, trust, hate, grateful). Show it as behaviour, repetition or a change of plan: "I've started sleeping again." Checker candidate: a D-5 word list on `reply` and `confirm` cells. [Leonard; Druckmann and Straley; section 0]
4. **React to what the event caused, not the event.** Not "You paid my debt?" but "Lender says I'm square." [Leonard; Mamet]
5. **No "as you know".** Nobody tells someone what both know; the briefing, cards and result lines explain. [Mamet; TV Tropes]

**Tier 2: temperament and role**

6. **One flouted maxim per temperament**, the generator's voice key: Mercenary turns everything to price (relation); Fearful says too much or breaks off (quantity, with self-repair); Loyal answers in procedure (manner); Idealist answers the question behind the question. [Grice; Hines]
7. **A closed tic set per temperament, once per exchange at most.** Mercenary: numbers, "deal", "the rest", conditionals. Fearful: "please", doubled words, broken-off starts, hurry. Loyal: "I'll have to", the passive, the book, the owner or the Office. Idealist: "we", "the block", "people", "what's it for". [Avellone; Wildermyth]
8. **Roles speak their work.** Each voice group owns 8 to 15 job nouns (counter: tables, the till; driver: the bay, the lane; clerk: forms, stamps; security: the post, the round), and needs are said through them: "Short this week. The rent won't wait." [Avellone; Night in the Woods; Citizen Sleeper]
9. **Break the house rhythm.** No group above 40% "Fragment. Fragment." lines (60% today); each cell's variants use at least two shapes (one beat, a question, a run-on, a break-off). Checker candidate. [section 0; Hines]
10. **The plainest line is the default.** The unqualified cell holds a line any temperament could say; flavour lives in qualified cells. [Wildermyth; Ruskin]
11. **Variants differ in stance, not synonyms**: at least two different moves per three variants. [Compton; Short]

**Tier 3: the state's voice versus people**

12. **The city speaks agentless, passive and grateful in advance.** No "I", "citizen", thanks before compliance, orders as encouragements; the coercion is the politeness: "Citizen. Please lower the weapon. Thank you for complying." [Brown and Levinson; Orwell; We Happy Few; Papers, Please]
13. **Machines repeat; people vary.** The patrol's closed phrasebook repeats verbatim (exempt from D-7 across events); people never repeat a line in a run. [Mass Effect; Fallout]
14. **The optimistic surface lives in artefacts and institutions** (signs, chimes, robots). A person quoting a slogan is being ironic or afraid. [Fallout; BioShock; We Happy Few]
15. **People code-switch by listener**: officialese to strangers, plain speech to their tie ("State your business." vs "Not now, Inez."). An addressee flag gives the guard both. [code-switching]

**Tier 4: the cell as visitors**

16. **The cell sound off-period, consistently, never at random:** no period courtesies ("I'm afraid", sir or madam), no honorifics or slang, flatter syntax, bare function nouns ("the camera", "the car") where locals name the institution ("the Office's camera", "the Councillor's car"), and never anachronistic tech (the `avoid` list stands). [Pentiment; Atomic Heart; GDD]
17. **The cell never explain the plan to each other.** Acks are intent or attitude; the Enforcer stays at one to four words. [Mamet; Hades]
18. **One flash of the other timeline per mission, at most**: a cell member misjudges a custom once (an over-tip, surprise that a camera needs a person to watch it). A seasoning event, not a cell. **Proposal for Sky.**

**Tier 5: lore and templates**

19. **Every person line points at one unstated fact** (a tie, a need, the list), and lore enters only through someone's interest in it: "Half my street's on that list." [Hemingway; Pope; Sylvester; Outer Wilds]
20. **Overheard scenes are addressed to someone in the world, never the player.** [BioShock; the Handover `scenes`]
21. **Templates survive any filler**: whole lexicon noun phrases, no agreement across a slot, two slots at most, no pronouns, the slot never the point, tested with the shortest filler, the longest and a plural. [RimWorld; Wildermyth; Dias]
22. **Names are rare**: address terms only at an approach's opening beat, once per exchange; given names only between tied people. [section 2; D-4]

---

## 6. Before and after: eight lines from `text.json`

All rewrites meet D-2 (12 words or fewer, 80 characters or fewer). None uses a gendered word, a digit or a temperament label. The "template" column shows how the line generalises for the generator.

**1. Fixer, `start`**
- **Before:** "Nobody gets hurt today. That is the plan."
- **After:** "If anyone draws today, it means I've failed."
- **Why:** The before tells the cell their own plan (rule 5). The after keeps the non-violence and adds the Fixer's steel and stake (rules 2 and 17).

**2. Fixer, `ack/help`**
- **Before:** "Their trouble becomes ours."
- **After:** "What do they owe, and to whom? Leave it with me."
- **Why:** The before is a slogan, and slogans belong to the city (rule 14). The after shows the Fixer working (rule 2) with a question-then-answer rhythm (rule 9).

**3. Fixer, `done/turn/kept`**
- **Before:** "Good. They trust us a little more."
- **After:** "They waved me over this time."
- **Why:** The before puts the mechanic in the mouth (rule 3). The after shows trust as behaviour; the result line still says "kept".

**4. Inez (Idealist), `reply/help/turned`**
- **Before:** "You paid my debt? I'm yours. Say when."
- **After:** "Lender says I'm square. Nobody does that free. What's it for?"
- **Template:** "{met}. Nobody does that free. What's it for?"
- **Why:** The before restates the event and says "I'm yours" on the nose (rule 4). In the after, the Idealist flouts Relation upward (rule 6). The template works with any `met` form ("The rent's paid", "The fine's gone").
- **Check against rule 1:** a turned reply ending in a question could read as hesitation. Pair it with the turned result line, or keep "What's it for?" only in the second variant.

**5. Driver (Idealist), `reply/ask/turned`**
- **Before:** "If it keeps people in their homes, yes."
- **After:** "Half my street's on that list. Where's the car going?"
- **Why:** The before states the theme (Avellone: don't hammer it). The after brings in the clearance list as a personal fact (rule 19), uses the driver's work noun (rule 8), and ends with a clear working yes (rule 1). It also sets up the cliffhanger's "next list".

**6. Guard (Loyal), `reply/help/kept`**
- **Before:** "You've earned my respect, citizen."
- **After:** "Twice now. I've stopped writing you down."
- **Why:** The before is formal praise with a feeling noun. The after keeps the Loyal procedure tic (the log, rule 7) and turns it into warmth: the register drops because the relationship has (rule 15, the Pentiment shift).

**7. Patrol, `ai-state/engage`**
- **Before:** "Armed citizen. Engaging."
- **After:** "Citizen. Please lower the weapon. Thank you for complying."
- **Why:** "Engaging" is modern military jargon from a human soldier. The after is agentless and grateful in advance (rule 12), and the thanks before compliance is the coercion. It is reusable verbatim (rule 13).

**8. Vale, `win`**
- **Before:** "Do you know who I am?"
- **After:** "There will be minutes of this. Typed, in triplicate."
- **Why:** The before is a stock line any film villain could say. The after is the paper-records world under stress: an official reaching for procedure (Loyal manner, rules 6 and 19), pompous and frightened at once.

Other lines worth fixing, in priority order:

- The crowd's "Nothing to see. Move along." This is a police line in a civilian's mouth, so it breaks rule 15.
- Every Loyal suspicious reply that reads "This goes in my log". Give each role its own record:
  - counter: "the owner";
  - driver: "the motor pool";
  - guard: "the Office".
- Inez (Fearful): "Some of it. I'm still so frightened." A better version: "Some of it. The man still comes Thursdays."

---

## Summary of proposals that need a decision

These are not mine or Fable's to settle:

- **For Sky:**
  - the patrol's closed phrasebook and its exemption from D-7 across events;
  - a city lexicon for offenders ("unsettled", "out of step");
  - the once-a-mission flash of the other timeline;
  - whether the HUD should give the state's voice its own type treatment (Glyph).
- **For Fable and Keel (checker candidates):**
  - a feeling-word list on replies;
  - a per-group rhythm-shape ratio (40% at most);
  - a slot-agreement fuzz test in D-3;
  - a "hidden speaker" test: can a reader tell the role from one line?
