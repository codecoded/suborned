# Dwarf Fortress: how it generates and simulates people

Research dossier for brief 010 (people). Audience: systems designers building a deterministic, build-time-generated city cast. Written 2026-10-15.

**How to read this.** Numbers come mostly from the community Dwarf Fortress Wiki (current namespace, labelled v53.16 at time of fetch). Several of its pages carry a "migrated from v0.47, may be inaccurate" banner; those are marked **[older/unverified]**. Tarn Adams' own words come from his dev logs, papers, and interviews and are marked as quotes. Where I could not find a primary source for something often repeated (for example a "history, not backstory" slogan), I say so.

**Version landmarks** (from Bay 12 dev logs and release notes):
- 0.40.01 (7 Jul 2014), the "world activation" release: emotions start replacing old thoughts, and dwarves get life dreams ([dev 2014](https://www.bay12games.com/dwarves/dev_2014.html), [Kill Screen](https://www.killscreen.com/dwarves-can-dream-now-dwarf-fortress/)).
- 0.40.14 (Oct 2014): thoughts become "personality-dependent emotion/circumstance pairs", and happiness becomes a longer-term stress level ([release 0.40.14](https://dwarffortresswiki.org/index.php/Release_information/0.40.14)).
- 0.42.01 (1 Dec 2015): needs, taverns, temples, libraries, visitors and citizenship petitions ([dev 2015](https://www.bay12games.com/dwarves/dev_2015.html)).
- 0.47.01 (Jan 2020), the "villains" release: intrigue, corruption, interrogation and plots ([Intrigue](https://dwarffortresswiki.org/index.php/Intrigue), [dev 2018](https://www.bay12games.com/dwarves/dev_2018.html), [dev 2019](https://www.bay12games.com/dwarves/dev_2019.html)).
- v50 (6 Dec 2022): Steam/itch release with Kitfox. It has a new UI and graphics, and the simulation underneath is unchanged ([Game Rant](https://gamerant.com/dwarf-fortress-interview-evolution-after-release/)).

**Design principles in Tarn's words.** These frame everything below. All quotes are from "Simulation Principles from Dwarf Fortress", *Game AI Pro 2*, ch. 41 ([PDF](http://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter41_Simulation_Principles_from_Dwarf_Fortress.pdf)):
1. "Don't overplan your model": "get something running as soon as you can and then work from there".
2. "Break down and understand the system": biomes come out of separate temperature, rainfall, elevation and drainage fields. They are never placed directly.
3. "Don't overcomplicate": "There's no reason to have 50 variables modeling one aspect of a game's behavior if they don't all have a meaningful impact on the game. **Operate at the level of what the player sees or one layer below.**"
4. "Base your model on real-world analogs."

Elsewhere he says: "Create actors with motivations, and let them go" ([Game Developer 2008](https://www.gamedeveloper.com/design/interview-the-making-of-dwarf-fortress)).

---

## 1. Personality: facets, values, dreams

### Facets (traits)
- **There are 50 facets, each scored 0–100.** Each is a bipolar pair, for example LOVE_PROPENSITY (quick to love / slow to love), ANGER_PROPENSITY, BRAVERY, TRUST, GREED, PRIVACY, VENGEFUL, ASSERTIVENESS, GREGARIOUSNESS, DUTIFULNESS and ALTRUISM ([Personality trait](https://dwarffortresswiki.org/index.php/Personality_trait)).
- **Display bands and how often each occurs:**

  | Band | Score | Share of people |
  |---|---|---|
  | Highest | 91–100 | 0.4% |
  | Very high | 76–90 | 2% |
  | High | 61–75 | 8.5% |
  | Neutral | 40–60 | 78% |
  | Low | 25–39 | 8.5% |
  | Very low | 10–24 | 2% |
  | Lowest | 0–9 | 0.4% |

  Only scores outside 40–60 produce a sentence on the character sheet. So **about 78% of people show nothing for any given facet**. A typical person's description is a handful of notable traits, never 50 lines. This is the main readability device ([Personality trait](https://dwarffortresswiki.org/index.php/Personality_trait)).
- **Origin.** In earlier versions the model had 30 facets taken from the NEO PI-R test. Tarn said, "I like having the thirty different facets of the personalities" ([DF Talk #8](https://www.bay12games.com/media/df_talk_8_transcript.html), pre-2014). A Stanford interview summary says he took them from NEO PI-R "without much knowledge of the underlying theory" ([HASTAC/Stanford, via search summary](https://dhs.stanford.edu/algorithmic-literacy/an-interview-with-tarn-adams/), page unreachable when checked). The current set is 50. I could not pin down which release expanded it **[uncertain]**.
- **Generation.** Each species has a per-species distribution set by the `PERSONALITY` creature token: a minimum, median and maximum per facet. Dwarves have a GREED median of 55; goblins have an ALTRUISM median of 25, capped at 50 ([Personality trait](https://dwarffortresswiki.org/index.php/Personality_trait)). The wiki does not document inheritance from parents. I found no evidence that facets are inherited **[unknown]**.
- **Facets matter more than flavour in these places:**
  - **Breakdowns.** Very high STRESS_VULNERABILITY gives a 50% catatonia chance under stress. Very high ANGER makes tantrums and berserk more likely. Very high DEPRESSION makes melancholy more likely. Very high ANXIETY makes "stumbling obliviously" and "stark raving mad" more likely.
  - **Fleeing.** Civilians with low VIOLENT flee danger.
  - **Which social skills a person can learn:** Consoler, Pacifier, Intimidator, Flatterer, Comedian, Conversationalist, Persuader.
  - **Worldgen war declarations.** High ANGER, CONFIDENCE, AMBITION, THOUGHTLESSNESS and EXCITEMENT_SEEKING make wars more likely.
  - **Needs.** See §2.
  - **Relationship compatibility.** See §4.

  The wiki itself says "many gameplay effects of facets are still unknown" ([Personality trait](https://dwarffortresswiki.org/index.php/Personality_trait)). That is an honest measure of how many are flavour.
- **Facets were once used only at the extremes.** Before 2014 Tarn said: "the personality only starts being used in the extreme cases with low happiness … if a dwarf gets angry easily and has a low happiness number they might throw a tantrum" ([DF Talk #8](https://www.bay12games.com/media/df_talk_8_transcript.html)). Facets now also filter every emotion (§3).

### Values (beliefs)
- **There are 33 values, each scored −50 to +50.** They are LAW, LOYALTY, FAMILY, FRIENDSHIP, POWER, TRUTH, CUNNING, ELOQUENCE, FAIRNESS, DECORUM, TRADITION, ARTWORK, COOPERATION, INDEPENDENCE, STOICISM, INTROSPECTION, SELF_CONTROL, TRANQUILITY, HARMONY, MERRIMENT, CRAFTSMANSHIP, MARTIAL_PROWESS, SKILL, HARD_WORK, SACRIFICE, COMPETITION, PERSEVERANCE, LEISURE_TIME, COMMERCE, ROMANCE, NATURE, PEACE and KNOWLEDGE ([Personality value](https://dwarffortresswiki.org/index.php/Personality_value)).
- **Bands** mirror the facets: −10…+10 is neutral (78%), and ±41–50 is "absolutism" or "abhorrence" (0.4% each).
- **Two layers: the culture sets a baseline and the individual can deviate.** Civilizations set values through the `VALUE` entity token (dwarves prize craftsmanship). Any value that differs significantly from the person's culture is highlighted on the sheet. The sheet literally reads "Like others in his culture, he holds craftsdwarfship to be of the highest ideals", and personal values are phrased as departures from the culture ([Personality value](https://dwarffortresswiki.org/index.php/Personality_value), [Thoughts and preferences](https://dwarffortresswiki.org/Thoughts_and_preferences)). Human civilizations get randomized value sets per civilization ([dev 2015, 03/20](https://www.bay12games.com/dwarves/dev_2015.html)).
- **What values drive:**
  - needs (§2)
  - which social skills a person can learn: high POWER allows Intimidator, high TRUTH blocks Liar
  - specific thoughts: NATURE makes tree felling and butchery upsetting
  - small gates: low KNOWLEDGE means a person cannot become a scholar
- **How values change:**
  - Memories alter them over time (§3).
  - In adventure mode, winning arguments shifts them **[flagged unverified on wiki]**.
  - Philosophical works and poetry can pass values to readers ([dev 2015, 07/25](https://www.bay12games.com/dwarves/dev_2015.html)).
  - New residents do not take on their new civilization's values immediately (dev 2015, 05/15).

### Dreams (goals)
- **One goal per creature,** shown under Values ([Personality goal](https://dwarffortresswiki.org/index.php/Personality_goal)). The tokens are START_A_FAMILY, RULE_THE_WORLD, CREATE_A_GREAT_WORK_OF_ART, CRAFT_A_MASTERWORK, BRING_PEACE_TO_THE_WORLD, BECOME_A_LEGENDARY_WARRIOR, MASTER_A_SKILL, FALL_IN_LOVE, SEE_THE_GREAT_NATURAL_SITES, IMMORTALITY, MAKE_A_GREAT_DISCOVERY, ATTAIN_RANK_IN_SOCIETY, BATHE_WORLD_IN_CHAOS, plus STAY_ALIVE and MAINTAIN_ENTITY_STATUS (undocumented).
- **Only a few are checkable:**
  - Masterwork or artifact made: realizes the art and masterwork dreams.
  - Legendary skill reached: realizes MASTER_A_SKILL.
  - Child born: realizes START_A_FAMILY.
  - Becoming a baron: realizes ATTAIN_RANK_IN_SOCIETY.

  Realizing a dream gives a strong happy thought and adds "and this dream was realized" to the description. The dream does not steer daily behaviour in fortress mode. In 2014 Tarn wrote that people "cannot yet pursue dreams of taking over the world" ([dev 2014, 07/07](https://www.bay12games.com/dwarves/dev_2014.html)). In worldgen, IMMORTALITY drives would-be necromancers, and the wiki infers it from high PRIDE, VANITY, AMBITION, ENVY and ANXIETY with low BRAVERY **[inferred from string dump]**.

### In play versus flavour
- **Load-bearing:** the extreme bands of a few facets (stress vulnerability, anger, depression, anxiety, bravery and violent), values and facets as **inputs to needs**, and facet compatibility for relationships.
- **Mostly flavour:** dreams (outcome only), most mid-range facets and most values.

**Mechanic in one line:** each person gets 50 bipolar facets (0–100, centred on 50) drawn from a species distribution and 33 values (−50…+50) drawn from their culture with personal deviation, and only the 22% outside the neutral band are ever described or matter much.

---

## 2. Needs

- **Needs come from personality.** "Personalities and values lead to needs which can be met by various actions in both modes" ([dev 2015, 12/01](https://www.bay12games.com/dwarves/dev_2015.html)). Each need has a **weight of 1, 2, 5 or 10**, set by how extreme the linked facet or value is ([Need](https://dwarffortresswiki.org/index.php/Need)).
- **The need list and the trait each comes from** ([Need](https://dwarffortresswiki.org/index.php/Need)):
  - socialize: GREGARIOUSNESS
  - drink alcohol: IMMODERATION / SELF_CONTROL
  - pray: one need per deity, scaled by devotion
  - stay occupied: HARD_WORK / ACTIVITY_LEVEL
  - be creative: ART_INCLINED
  - excitement: EXCITEMENT_SEEKING
  - learn: KNOWLEDGE / CURIOUS
  - family: disabled since 53.16
  - friends: FRIENDSHIP
  - eloquence: ELOQUENCE
  - tradition: TRADITION
  - introspection: INTROSPECTION
  - merriment: MERRIMENT / HUMOR
  - craft: CRAFTSMANSHIP
  - martial practice: MARTIAL_PROWESS
  - practise a skill: SKILL
  - leisure: LEISURE_TIME
  - romance: ROMANCE / LOVE_PROPENSITY
  - see animals: NATURE
  - see a great beast: EXCITEMENT_SEEKING / NATURE / CURIOUS
  - acquire objects: GREED / COMMERCE
  - good meals: IMMODERATION
  - fight: VIOLENT
  - cause trouble: HARMONY / DISCORD
  - argue: FRIENDLINESS
  - be extravagant: IMMODESTY
  - wander: NATURE / ACTIVITY_LEVEL
  - help someone: ALTRUISM / SACRIFICE
  - think abstractly: ABSTRACT_INCLINED
  - admire art: ARTWORK
- **How unmet needs accumulate.** Each need has a counter. **Meeting the need resets it to +400**, and it then decays without a floor. The bands are:

  | Band | Counter |
  |---|---|
  | Unfettered | 400…300 |
  | Level-headed | 299…200 |
  | Untroubled | 199…100 |
  | Not distracted | 99…−999 |
  | Unfocused | −1,000…−9,999 |
  | Distracted | −10,000…−99,999 |
  | Badly distracted | ≤ −100,000 |

  To stay above −10,000 a need must be met about **every 2 years at weight 1, every year at weight 2, every 6 months at weight 5, and every 3 months at weight 10**. Needs pause while a unit is away on a mission ([Need](https://dwarffortresswiki.org/index.php/Need)).
- **Focus** is a weighted ratio of met to unmet needs. 100% is neutral. **140% or more is "very focused"; 60% or less is "badly distracted".** Focus scales effective skill by **±50%**. Tarn: "a theoretical skill range of 50% to 150% but a much smaller real effect" ([dev 2015, 04/12](https://www.bay12games.com/dwarves/dev_2015.html)). Focus is separate from mood, but long-unmet needs also produce bad thoughts and therefore stress.
- **Behaviour.** Needs replaced fixed "breaks and parties": "specific breaks and parties have been replaced by taverns and performances and needs and inebriation" ([dev 2015, 12/01](https://www.bay12games.com/dwarves/dev_2015.html)).
  - Idle dwarves go to taverns, temples and libraries to meet needs (dev 2015, 01/21).
  - One action can meet several needs (04/12).
  - Need-satisfying jobs run at low priority and can be cancelled for real work ([Need](https://dwarffortresswiki.org/index.php/Need)).
- **Personality shows up as visible acts.** Avaricious and extravagant dwarves swipe and wear trinkets, and quarrelsome dwarves pick arguments (dev 2015, 10/28).

**Mechanic in one line:** traits produce a short list of weighted needs, each a counter that resets to +400 when met and falls without limit, and the weighted share of met versus unmet needs sets focus (±50% skill) and leaks into stress.

---

## 3. Emotions, thoughts and memories

### From event to emotion
- **A thought is an event (circumstance) plus an emotion,** and personality picks the emotion. The 2014 rewrite turned thoughts into "personality-dependent emotion/circumstance pairs". The emotion count went from 16 to 119, with the explicit goal of making everything visible as text ([dev 2014, 09/26 and 10/25](https://www.bay12games.com/dwarves/dev_2014.html)). The wiki now counts 170 emotions defined and about 130 in use ([Emotion](https://dwarffortresswiki.org/index.php/Emotion)) **[older/unverified]**.
- **One event can fire many emotions.** A relative's death can produce "horror, terror, fear, shock, grief, and rage" at once; "Personality then determines how they're handled" ([dev 2014, 02/15](https://www.bay12games.com/dwarves/dev_2014.html)).
- **Each emotion has a divider from −8 to +8.** Stress change = strength ÷ divider, and negative dividers relieve stress. Rage, fear and terror are 1, the strongest. Anger is 2, anxiety 4, annoyance 8. Adoration is −1 and acceptance −8. Apathy is 0, which has no effect. Weak emotions (|divider| of 2, 4 or 8) cannot push a person into the extreme mood buckets ([Emotion](https://dwarffortresswiki.org/index.php/Emotion)) **[older/unverified]**.
- **Strong emotions act directly.** Terror makes a person flee, rage makes them enraged, and anguish makes them lie on the ground crying ([Emotion](https://dwarffortresswiki.org/index.php/Emotion)).

### Stress
- **There are two accumulators** ([Stress](https://dwarffortresswiki.org/index.php/Stress), **[older, v0.47]**):
  - **Short-term:** −100,000…+100,000. It drives the displayed mood, from ecstatic to miserable.
  - **Long-term:** −50,000…+120,000. Labels: "stressed" at +25,000, "haggard" at +50,000, "harrowed" at +100,000. It rises at most about 20,160 per year and falls at most about 43,564 per year.
- **Personality modulates the rates.** BRAVERY sets how fast stress builds, STRESS_VULNERABILITY how much it takes to break, and ANXIETY how fast it dissipates.
- **Tarn deliberately damped spirals.** He tuned stressors "so stability could change over time" and added "an extra smoothing layer … to reduce sudden spirals after a single catastrophe" (dev 2014, 10/02, paraphrased in the log digest). Stress falls faster the longer a dwarf goes without stressors (11/17). This answers his earlier complaint about "tantrum spirals - stuff that are side effects of the system" ([DF Talk #8](https://www.bay12games.com/media/df_talk_8_transcript.html)).
- **A breakdown needs both short-term and long-term stress to be high.** The forms are:
  - tantrum: hits people and breaks things
  - depression
  - "stumbling obliviously"
  - catatonia, for very high stress vulnerability
  - berserk
  - permanent insanity, for harrowed dwarves after a further shock
- **Dwarves without a temper get "tantrum analogs"**, other visible warning signs (dev 2014, 10/05). Before a breakdown, stressed dwarves cancel jobs to "space out", which is the visible warning ([Thoughts and preferences](https://dwarffortresswiki.org/Thoughts_and_preferences)).
- **Leaders relieve stress.** Stressed dwarves "yell at or cry on 'someone in charge'": the mayor or expedition leader is a social stress sink ([Noble](https://dwarffortresswiki.org/index.php/Noble)).

### Memory
Source: [Memory (thought)](https://dwarffortresswiki.org/index.php/Memory_(thought)) **[older/unverified]**.
- **8 short-term slots, 8 long-term slots and unlimited core memories.**
- **Short-term slots:**
  - A new thought takes an empty slot. If there is none, it overwrites the weakest.
  - Only one memory per emotion **group** is kept; the stronger wins.
  - At most 7 affect mood at once.
- **Promotion to long-term:** a memory that **survives one year** in short-term is promoted. It overwrites a weaker same-group long-term memory, or the weakest one if no same-group memory exists.
- **Long-term memories** are "retained forever" and **recur periodically**. Each recall re-fires the emotion, so one event keeps producing stress or relief for years. In a sample of 300 dwarves, the median number of revisited memories was 9.
- **Core memories change people.** Each recall of an eligible long-term memory has a **1 in 3 chance of becoming core**. Core memories are rarely recalled, but the **facet and value changes they cause are permanent**. These changes show in magenta on the sheet. Many negative memories "may change to acceptance" over time ([Stress](https://dwarffortresswiki.org/index.php/Stress)).
- **People get inured.** A hidden "combat hardness" (desensitization) rises with seeing gore and death. The sheet reads "getting used to tragedy" up to "doesn't really care about anything anymore". Its effect on other stress is untested ([Stress](https://dwarffortresswiki.org/index.php/Stress), [Thoughts and preferences](https://dwarffortresswiki.org/Thoughts_and_preferences)). The thought "didn't feel anything after seeing a dog's dead body" is the visible result ([Thought](https://dwarffortresswiki.org/index.php/Thought)).

**Mechanic in one line:** each event becomes a personality-chosen emotion whose strength ÷ divider moves short- and long-term stress, the strongest memories survive a year to become long-term and recur, and recalled memories can turn core and permanently rewrite facets and values.

---

## 4. Relationships

### Fortress mode (how ties form now)
Source: [Relationship](https://dwarffortresswiki.org/index.php/Relationship).
- **Ties form through proximity and idleness.** Two dwarves must be idle and orthogonally adjacent to start chatting; others can then join. Each chat updates a rank driven mainly by **personality compatibility**. A gap on a relationship facet (one person above 60, the other below 40) breeds a grudge. Likes and skills hardly matter.
- **Thresholds:**

  | Rank or value | Result |
  |---|---|
  | ~15 | friendship or grudge forms |
  | ~40 | romantically inclined friends become lovers |
  | 50 | lovers can marry |
  | +50…74 | friend |
  | +75 | close friend |
  | +100 | kindred spirit |
  | −50…−74 | disliked |
  | −75…−99 | hated |
  | −100 | pure hate |
  | 30 conversations without friendship | long-term acquaintance |

- **Trust is fixed at the first interaction** from the TRUST facet and never changes in chat.
- **Lovers** must be adults, within an age gap of max(10, younger age ÷ 2), of compatible orientation, and not close kin.
- **Decay.** Friends and acquaintances fade without contact. Above **50 non-kin relationships**, passing acquaintances with no contact in the past year are pruned at new year, sometimes on one side only. **Grudges are never forgotten**, though disliked and hated values can improve.
- **Effects:**
  - Making a friend gives a happy thought; a friend's death gives grief.
  - Forming a grudge is unhappy, and so is the grudgee's death.
  - **Talking to someone you hold a grudge against can add 700+ stress a day.**
  - A larger network of friends and family raises happiness but makes deaths hit harder.
- **Family** (parents, children, siblings) is stored as kinship, not as love or trust variables, and relatives need not live on site. The starting seven are mutual acquaintances or friends, occasionally with a grudge.
- **Grudges feed justice.** Witnesses "sometimes make false reports to try and frame dwarves they have grudges against" ([Justice](https://dwarffortresswiki.org/index.php/Justice)).

### Worldgen and the villains layer (how history makes ties)
- **Ties were added because plots needed them.** Tarn noted in 2018 that "there are no friendships in world generation", so villains had nothing to exploit. He added **childhood friendships** and **war buddies** (war buddies "are more likely to cross site boundaries"). He called romantic rejection and jealousy "quick-to-do villain-ready vectors" ([dev 2018, 08/18, 09/08](https://www.bay12games.com/dwarves/dev_2018.html)). **Travelling merchant companies** were added to "facilitate more cross-civ link formation" (10/16).
- **Conversation uses five relationship variables:** "'loyalty', 'trust/distrust', 'fear', 'love/hate', and 'respect'", alongside reputation and personality ([dev 2019, 10/02](https://www.bay12games.com/dwarves/dev_2019.html)).
- **Revenge runs through ties.** Villains recruit by "promising revenge", and characters with a dead relative or a grudge are targets ([Villain](https://dwarffortresswiki.org/index.php/Villain)).
- **Before 2014 family barely existed in the mind.** Tarn: "they really don't in general have a handle on their parents or their brother", and he refused "ten thousand different entities, one for each family" ([DF Talk #8](https://www.bay12games.com/media/df_talk_8_transcript.html)). Family is now stored kinship, still not a full entity.

**Mechanic in one line:** idle adjacency runs repeated chats whose personality-compatibility score crosses fixed thresholds (15 friend or grudge, 40 lover, 50 spouse), friendships decay while grudges never do, and the ties then carry grief, false accusations and recruitment by revenge.

---

## 5. History and world generation

- **The pipeline:**
  1. elevation fractal
  2. temperature, rainfall, drainage, vegetation and salinity fields
  3. biomes classified from those fields
  4. erosion, then rivers
  5. plants, animals and fantasy creatures
  6. "the simulation of civilization begins … settlements are raised, trade routes are formed, and wars are waged"
  7. it stops at a set year and play starts

  Sources: [Game AI Pro 2 ch. 41](http://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter41_Simulation_Principles_from_Dwarf_Fortress.pdf), [World generation](https://dwarffortresswiki.org/index.php/World_generation).
- **History is a log of a simulation, not authored backstory.** Tarn calls history generation "a giant zero-player strategy game going on with somewhat loose turn rules and bad AI (but thousands of agents)", and "history is just a record of that". He adds: "Procedurally generating stories by recording a log of a simulation is a valid enough approach" ([Game Developer 2019](https://www.gamedeveloper.com/design/q-a-dissecting-the-development-of-i-dwarf-fortress-i-with-creator-tarn-adams)). Also: "We never really wanted to write a plot" and "Create actors with motivations, and let them go" ([Game Developer 2008](https://www.gamedeveloper.com/design/interview-the-making-of-dwarf-fortress)). *I found no primary source for the exact slogan "history, not backstory". These quotes are the closest statements of that philosophy.*
- **Historical figures ("histfigs") are the explicitly tracked minority.** "Most of the population is handled abstractly due to computation and memory constraints" ([Historical figure](https://dwarffortresswiki.org/index.php/Historical_figure)).
  - **Who becomes a histfig in worldgen:** nobles and their entourage, megabeasts, titans, demons, villains (necromancers, vampires), crime bosses, and animals that killed members of a civilization.
  - **Who becomes a histfig in play:** every fortress citizen on arrival, the children of histfig mothers, named or trained animals, and anyone named you meet in adventure mode.
  - **Unnamed NPCs** are "a complete blank slate".
  - **The starting seven are "void dwarves", generated from nothing:** your first people have no past.
- **Scale knobs** ([Advanced world generation](https://dwarffortresswiki.org/index.php/Advanced_world_generation), [World generation](https://dwarffortresswiki.org/index.php/World_generation)):
  - `TOTAL_CIV_POPULATION` is a soft cap of up to 100,000 live histfigs. It only blocks births and is shared by sites controlled.
  - `SITE_CAP` goes up to 100,000.
  - `END_YEAR` runs from 2 to 10,000.
  - On a medium world there are 8–80 civilizations, depending on setting.
  - "Too long a history often leads to civilizations dying out."
- **Legends mode** browses every histfig, site, artifact, civilization and event, cross-linked. It exports XML of up to about 1 GB on large worlds. With "reveal all historical events" off, history stays hidden until adventurers learn it by talking, reading and finding artifacts ([Legends](https://dwarffortresswiki.org/index.php/Legends)).
- **How the past produces present people:**
  - Migrants, nobles, visitors and diplomats are existing histfigs.
  - Relatives live at other sites.
  - Positions and succession come from the entity's government.
  - Villain networks, criminal organizations and their corrupt officials (§7) are formed **during worldgen** and are already in place when play begins ([Intrigue](https://dwarffortresswiki.org/index.php/Intrigue)).
  - Artifacts carry their owners' and thieves' histories. Statues and engravings depict world history: improvements "reference the history of the world, the history of the site" ([Stack Overflow podcast 2021](https://stackoverflow.blog/2021/09/21/podcast-377-you-dont-need-a-math-phd-to-play-dwarf-fortress-just-to-code-it/)).
  - History continues **after** worldgen, off-screen, at "around two weeks per session start", and how closely that matches worldgen is unclear ([World generation](https://dwarffortresswiki.org/index.php/World_generation)).
- **Worldgen example (dev 2018, 12/24):** a network run by "Earthhells the Bewildering". One recruit failed to corrupt a broker, earned enough trust to run his own network, was flipped, and became a dwarven agent. Another recruiter brought in "no fewer than eight new members". "A fired bookkeeper was too scared to identify Earthhells", so fear protected the head ([dev 2018](https://www.bay12games.com/dwarves/dev_2018.html)). Every present-day grudge in a story like this is a logged event, not a written line.

**Mechanic in one line:** run a coarse multi-agent strategy sim for N years over a small tracked population of named figures, keep the event log as "history", and draw present-day people, positions, grudges and plots from that log.

---

## 6. Jobs, schedules and roles

- **Labour is pulled, not scheduled.** Jobs come from designations, zones, workshops and manager work orders. An idle dwarf with the right labour enabled takes the job, unless eating, sleeping or drinking ([Labor](https://dwarffortresswiki.org/index.php/Labor)). Work details are set to everyone, only selected dwarves, or nobody. Mining, woodcutting and hunting each need an exclusive tool. **There is no clock schedule:** behaviour is job availability plus needs (§2) plus sleep, food and drink.
- **The main loop is staggered by tick interval** ([Game Developer 2019](https://www.gamedeveloper.com/design/q-a-dissecting-the-development-of-i-dwarf-fortress-i-with-creator-tarn-adams)):
  - every tick: creature AI, fluids, projectiles
  - every 10 ticks: seasons advance
  - every 50 ticks: taverns, temples and libraries refresh
  - every 100 ticks: job assignment, strange moods, rotting items
  - every 1,000 ticks: deleted objects freed
- **Occupations** (2015) are location-bound roles rather than labours: tavern keeper, performer, scribe and scholar ([dev 2015, 05/18](https://www.bay12games.com/dwarves/dev_2015.html)).
- **Positions (nobles)** ([Noble](https://dwarffortresswiki.org/index.php/Noble)):
  - The **expedition leader** appoints manager, bookkeeper, broker, chief medical dwarf, militia commander and sheriff.
  - At **population 50 a mayor is elected** automatically, "mostly outside of the player's control". The captain of the guard and dungeon master then become available.
  - **Baron / count / duke** (defaults): population 20 plus 100k / 200k / 300k created wealth.
  - Each position has **room requirements**, for example a duke needs grand quarters, a grand dining room, an opulent throne room and a tomb.
  - Nobles make **demands and mandates** (baron or mayor: 2 demands, 1 mandate; duke: 5 and 3). Unmet mandates can bring punishment for "oath-breaking".
- **Corrupt office.** Officials may embezzle or take bribes and then become "a target for both law enforcement and blackmail". Example from the log: a chief chef "began scheming a bit extra out of the kitchen budget" ([dev 2018, 11/28](https://www.bay12games.com/dwarves/dev_2018.html)).

**Mechanic in one line:** daily behaviour is a priority pull (an available job matching an enabled labour, otherwise the most pressing need, otherwise idle socializing) evaluated on staggered ticks, with positions adding offices, rooms, demands and, in v0.47+, temptation to corruption.

---

## 7. Social interaction with outsiders: spies, corruption, interrogation, rumours

This is the most relevant section for The Suborned.

### Information moves only through people
- **Nobody is omniscient.** "As creatures in the game are not omniscient, rumors passed among civilizations and individuals regarding current item locations are key" (Tarn Adams, "Secret Identities in Dwarf Fortress", AIIDE 2017 EXAG workshop, [AAAI](https://ojs.aaai.org/index.php/AIIDE/article/view/12963), [PDF](https://cdn.aaai.org/ojs/12963/12963-52-16480-1-2-20201228.pdf)).
- **A rumour is knowledge of a specific historical event.** It starts with **witnesses** and spreads when hearers pass it on. **False rumours do not spread**, with one exception: secret identities, where only the identity is wrong ([Rumor](https://dwarffortresswiki.org/index.php/Rumor)).
- **Spread is local first, then outward.** A rumour starts spreading when a knower is offloaded, i.e. leaves the loaded area. It reaches nearby settlements "within a day or two, sometimes up to a week", scaled by importance. "Nobody knows whether a dead character is really dead unless they saw the body."
- **Containment is physical.** To contain a crime you kill or remove witnesses before they offload and hide the bodies. Unnamed NPCs know nothing unless told, and each retelling gives them a fresh copy ([Rumor](https://dwarffortresswiki.org/index.php/Rumor)).
- **Knowledge is graded.** Artifact location knowledge has six levels, from holding it to having no idea. It fades over weeks and years, while **reputation outlasts the incident**. It is merged by timestamp at four tiers: individual, site government, site culture and civilization ([Rumor](https://dwarffortresswiki.org/index.php/Rumor)).
- **In fortress mode, news arrives by people:**
  - the yearly outpost liaison (count depends on how eventful the year was)
  - diplomats
  - **tavern visitors, but only when served a drink by an assigned tavern keeper**, usually one rumour at a time ([Rumor](https://dwarffortresswiki.org/index.php/Rumor), [Diplomat](https://dwarffortresswiki.org/index.php/Diplomat))

### Identity model (from the AAAI paper)
This part is directly reusable.
- **Spies carry a full cover:** "the old secret identities were extended to include a cover profession, objects of worship, belief systems, and cultural/government affiliations". Covers include prophet, poet, pilgrim, peddler and petty criminal. **Spies are picked "according to the possibility of fitting in."**
- **The game stays honest, the player sees only reports.** "Incidents involving the spy store the true identity … but the player never sees this data and can only interact with rumors and eyewitness accounts of incidents, which now store the current identity (if known) and a visual identification (if seen)."
- **Each relationship has three states:**
  1. **Alias only**: you can talk about "Urist the bruiser" but would not recognise them on the street.
  2. **True name known.**
  3. **Visual**: you store "the number of times they've met, the time of the last meeting, and any reputation information".

  These states merge when a subject or eyewitness links a face to a name.
- **Opinion is computed from context, not from an ID.** "What do you think of somebody?" pulls the profile matching the context: seen in person, heard by name, or affiliation known. Example: Aliz hears "Urist (an alias) stole an amulet from Kogan". If Kogan is hated, Aliz may think well of Urist. Unless Aliz knows Urist is a goblin spy, the goblins get no blame. A known spy status can still lose to friendship, romance or kinship.
- **Spies can blow their own cover.** They keep their home ethics ("goblins … kill outsiders on sight" had to be special-cased), so a spy "might blow their cover with an inopportune opinion". False prophets give off-canon prophecies that "tip off the astute player".
- **Lies are added one type at a time.** The game cannot lie except through aliases. Asked to point to themself, a spy says "that person is nearby"; there were 85 known edge cases of this kind. Tarn on general lies: keeping every version "and let them sort out what they actually think on an as-needed basis" raises "memory and processor concerns … among potentially tens of thousands of individuals", so "types of lies will continue to be added … one by one, rather than just allowing rumors to hold incorrect data".

### Villains, corruption and recruitment (v0.47+)
- **Networks are built from agreements.** "Make villains enthusiastic network builders". Plots run through agreements with "at least two parties" and "a specific goal". The whole chain is "there to be recounted by a captured agent" ([dev 2018, 07/21](https://www.bay12games.com/dwarves/dev_2018.html)).
- **Recruitment techniques** ([Villain](https://dwarffortresswiki.org/index.php/Villain), [dev 2019, 04/11](https://www.bay12games.com/dwarves/dev_2019.html)):
  - intimidation (works better if the target already **fears** the villain)
  - asserting rank
  - blackmail (against corrupt officials or secrets)
  - flattery (works better if the target **trusts** the villain)
  - religious sympathy
  - promising revenge
  - direct bribery (artifacts, positions, a share of tribute)
  - ideological alignment, **checked against the target's values**
  - loyalty also factors in

  The agent "chooses a technique based on whichever one they think will provide the best outcome". With poor intrigue skill, weak judging of intent or no insider, **it misjudges, by design**. "The more skilled villains will approach targets that they have a better chance of swaying" ([dev 2019, 11/13](https://www.bay12games.com/dwarves/dev_2019.html)).
- **Cutouts and handlers.** Villains use handlers so they never meet assassins. "Important and duty-bound" handlers send messages abstractly, while others must travel (dev 2019, 06/27). Plots can pass from position holders to criminal groups through intermediaries.
- **Some villains break their promises.** "Grateful villains may share power with useful assets, but others break their promises" ([Villain](https://dwarffortresswiki.org/index.php/Villain)).
- **Corruption inside the fortress.** Agents arrive as visitors and try to corrupt residents. Success compares the initiator's and the target's social skills, "much like interrogation". There is **no sign** of intent unless an interrogation succeeds, and they may use pseudonyms ([Intrigue](https://dwarffortresswiki.org/index.php/Intrigue)). A corrupted bookkeeper "can attempt to compromise their friends, family and other position holders", so **corruption spreads along ties** ([dev 2018, 07/21](https://www.bay12games.com/dwarves/dev_2018.html)).

### Counter-intelligence and interrogation
- **Civilizations investigate in worldgen.** They track "the evidence they've collected from witnesses, interrogation and surveillance", which "gives them leads on further steps" ([dev 2019, 04/26](https://www.bay12games.com/dwarves/dev_2019.html)).
- **Fortress-mode interrogation** ([Justice](https://dwarffortresswiki.org/index.php/Justice)):
  - It needs at least one open case. The sheriff or captain of the guard questions the suspect.
  - **Success = the interrogator's social skills against the subject's** (no published threshold).
  - Success yields a confession to crimes or to membership of an organization, "possibly naming other members".
  - Results fill an Intelligence screen split into actors, organizations and plots, with graph views ([dev 2019, 08/21](https://www.bay12games.com/dwarves/dev_2019.html)).
  - A correct arrest gives "a hint one step farther up the chain". A wrong accusation "will upset everybody in the fortress" ([dev 2018, 07/21](https://www.bay12games.com/dwarves/dev_2018.html)).
  - **The shipped game is more lenient: "Interrogating an innocent dwarf does not have any negative effects"**, so players interrogate everyone ([Justice](https://dwarffortresswiki.org/index.php/Justice)). That is a known design hole to avoid.
- **Adventure-mode interrogation** ([dev 2019, 10/16](https://www.bay12games.com/dwarves/dev_2019.html), [Intrigue](https://dwarffortresswiki.org/index.php/Intrigue)):
  - Tactics attach to each utterance and resolve "like aimed attacks": persuade, flatter, joke, pacify, intimidate. "Successful intimidation breaks their composure", and composure decides whether the subject answers.
  - **A preview of likely outcomes** is shown, its accuracy set by the player's judging-intent skill.
  - Players fight a subject at "brawl" escalation to make them afraid, then ask.
  - Intelligent undead criminals can be interrogated after death.
- **Visitors and spies in the fortress** ([Visitor](https://dwarffortresswiki.org/index.php/Visitor)):
  - Visitors come to taverns, temples and libraries: about 3 per location at first, more if the place is popular and safe. The default visitor cap is 100.
  - They can **petition** for residency; citizenship follows after about 2 years.
  - Agents pose as visitors. **Tells** include names in quotation marks, a profession that does not match their gear, and missing skills.
  - Interrogation can fail with a weak interrogator, but the player can convict anyway.

**Mechanic in one line:** every fact is an event that people witnessed and carry, spreading person to person and site to site; identities are stored per observer in three states (alias, true name, face); recruiters pick bribe, fear, flattery, blackmail, ideology or revenge by reading the target's ties and values (and can misread them); interrogation contests social skills and yields one link up the chain.

---

## 8. Determinism and seeds

- **There are four seeds:** `SEED` (world), `HISTORY_SEED`, `NAME_SEED` and `CREATURE_SEED`. Each is enabled separately, and any left disabled is random. By default they come from the date and time, and they are logged in `gamelog.txt` ([Advanced world generation](https://dwarffortresswiki.org/index.php/Advanced_world_generation)).
- **Geography is reproducible but history is not.** The same seeds **plus the same parameters** should give the same geography. But any parameter that changes how many random numbers are drawn (DIM, ELEVATION, RAINFALL, …) changes everything downstream. In the wiki's words: "the historical events during world generation don't currently always happen the same way" (Bug 6934). Seeds "are not sufficient to duplicate a world without the rest of the world generation parameters", and the game does not output the full parameter set. Seeds also break across versions ([Importing and exporting worlds](https://dwarffortresswiki.org/index.php/Importing_and_exporting_worlds), [Advanced world generation](https://dwarffortresswiki.org/index.php/Advanced_world_generation)).
- **Play does not replay.** Fortress and adventure play has no replay or input-recording facility; the save file is the only persistence. Events after worldgen cannot be recreated from seeds ([Importing and exporting worlds](https://dwarffortresswiki.org/index.php/Importing_and_exporting_worlds)). I found no Tarn statement on deterministic play **[absence of evidence]**.
- **Lesson for us.** DF shows the failure mode: one shared RNG stream means any upstream change reshuffles every later draw. Our "seeded by id" approach (an independent stream per entity and stage) avoids that.

**Mechanic in one line:** DF seeds geography reliably but not history, has no play replay, and shares one RNG stream, so a small parameter change rewrites the whole world.

---

## 9. Readability problems and what v50 did

- **The problem is admitted.**
  - Tarn set out in 2014 to make the emotional state legible: "having a better system would let you look at the dwarf and not just say 'hi, I'm happy' or 'hi, I'm sad'" ([DF Talk #8](https://www.bay12games.com/media/df_talk_8_transcript.html)). The emotion rewrite aimed to make "everything visible as text" ([dev 2014, 09/26](https://www.bay12games.com/dwarves/dev_2014.html)).
  - The wiki still says many facet effects "are still unknown" ([Personality trait](https://dwarffortresswiki.org/index.php/Personality_trait)).
  - **Stress is invisible to the player until it starts to affect the dwarf**, then shows as a flashing red down-arrow ([Thoughts and preferences](https://dwarffortresswiki.org/Thoughts_and_preferences)).
  - Players have long used third-party tools (Dwarf Therapist, DFHack) to see attributes, traits and happiness as numbers and sort by them ([Dwarf Therapist](https://dwarffortresswiki.org/index.php/Dwarf_Therapist)).
  - Fortress interrogation, the intrigue that existed and its plot list were called "still somewhat unfinished" ([Intrigue](https://dwarffortresswiki.org/index.php/Intrigue)).
  - Tarn conceded that the spy system "isn't fully engaging from the player perspective", and that players may only notice it when "one of their dwarven citizens has given up information to a now-departed traveler" ([AAAI paper](https://cdn.aaai.org/ojs/12963/12963-52-16480-1-2-20201228.pdf)).
- **Legends overgenerate.** James Ryan argues that raw simulation logs "will almost always lack story structure" and need a curator, whether a human or a second system ([Emily Short on Ryan](https://emshort.blog/2019/05/21/curating-simulated-storyworlds-james-ryan/)).
- **What the design already does for readability:**
  - **Only out-of-band traits are described,** and in prose.
  - Values are phrased **relative to the person's culture**.
  - Thoughts are written as **sentences with a cause**: "He was uneasy after seeing a human's dead body".
  - Thoughts are colour-coded: blue or green positive, red or yellow strong negative, brown mild negative, grey neutral, pink memory.
  - Personality-changing memories are shown in magenta.
- **What v50 (Steam, 2022) did.** It is a presentation rewrite; the simulation is the same ("The switch that lets you go between Classic and graphical is just swapping out some glyphs").
  - **Tooltips everywhere** and a clickable mouse UI ([Game Rant](https://gamerant.com/dwarf-fortress-interview-evolution-after-release/)).
  - **A unit sheet with tabs:** Overview, Thoughts (recent and memories), Personality (traits, values, preferences, needs), Relations, Groups, Skills and others ([Thoughts and preferences](https://dwarffortresswiki.org/Thoughts_and_preferences)).
  - A **citizens list sortable by stress** ([Unit list](https://dwarffortresswiki.org/index.php/Unit_list)).
  - A tutorial. A UX specialist was brought in "later on", which Tarn calls "not a mistake we're planning to make with adventure mode" ([Game Developer 2023](https://www.gamedeveloper.com/programming/how-tarn-adams-upgraded-and-optimized-dwarf-fortress-for-its-official-steam-release)).
  - Tarn on the balance: "an emergent narrative engine requires both … the simulation needs to be giving back in consistent yet surprising ways" (same source).

**Mechanic in one line:** keep the numbers hidden but write every out-of-band trait, value deviation and thought as a sentence with its cause, and give players sortable summaries (stress, needs) so they don't have to read 50 sliders per person.

---

## 10. Performance and scale

- **A two-tier population.** Histfigs are tracked individually with family, titles, kills, relationships and knowledge. Everyone else is abstract site population. Leaving a site "offloads" its units, which are later "re-initialized from historical figure data", so wounds heal and hunger resets ([Historical figure](https://dwarffortresswiki.org/index.php/Historical_figure)). Off-map histfigs die of age at any time in the year; on-map ones only at year boundaries.
- **The histfig budget is a cap.** `TOTAL_CIV_POPULATION` is up to 100,000 live histfigs. Larger populations slow generation and can cause "a permanent, unavoidable FPS drop" ([Advanced world generation](https://dwarffortresswiki.org/index.php/Advanced_world_generation), [Maximizing framerate](https://dwarffortresswiki.org/index.php/Maximizing_framerate)). Long histories cost memory and storage, not per-tick time. Tarn's target for worldgen: "it has to crank a year every two seconds if it can, even when 20,000 people are running around" ([Game Rant](https://gamerant.com/dwarf-fortress-interview-evolution-after-release/)).
- **Pruning:**
  - acquaintances above 50 relationships are dropped (§4)
  - artifact knowledge fades
  - "reputations are calculated dynamically until incidents time out (when some information is culled)" ([AAAI paper](https://cdn.aaai.org/ojs/12963/12963-52-16480-1-2-20201228.pdf))
  - unimportant histfigs can be culled
- **Abstraction off-screen.** Important handlers "send messages abstractly over the same period" instead of walking ([dev 2019, 06/27](https://www.bay12games.com/dwarves/dev_2019.html)). Post-worldgen history advances in coarse steps.
- **Active units are the cost.** Unit turns are over 60% of frame time in large forts. **Line-of-sight is "the slowest part of the game by a wide margin"**, checked only within 26 tiles. Pathfinding is under 10%. Disabling temperature can double FPS ([Maximizing framerate](https://dwarffortresswiki.org/index.php/Maximizing_framerate) **[older/unverified]**).
- **Staggered ticks** (§6) and dirty flags: "not every tile is necessarily checked every turn" ([Game Developer 2019](https://www.gamedeveloper.com/design/q-a-dissecting-the-development-of-i-dwarf-fortress-i-with-creator-tarn-adams)). Tarn's toolkit: "when to use flags, when to use event timers, when to use dirty buffers" ([Stack Overflow podcast](https://stackoverflow.blog/2021/09/21/podcast-377-you-dont-need-a-math-phd-to-play-dwarf-fortress-just-to-code-it/)).

**Mechanic in one line:** simulate the few named people individually and everyone else as counts, re-instantiate detail from stored records when needed, cap and prune relationships and knowledge, and stagger expensive checks over ticks, with perception (line of sight) as the real cost.

---

## 11. Other games worth contrasting

### Watch Dogs: Legion (Ubisoft Toronto, 2020): the closest premise
- **Census** is a "knowledge base" that "fill[s] in all of the blanks in a plausible way", for example a bartender's likely income and home ([Game Developer 2020](https://www.gamedeveloper.com/design/how-watch-dogs-legion-s-play-as-anyone-simulation-works)). Its GDC talk is by Christopher Dragert ([GDC Vault 2021](https://www.gdcvault.com/play/1027018/Census-The-Systemic-Backbone-Behind)).
- **Two layers:** a generation layer making "internally consistent" demographic profiles, and a simulation layer that makes people persistent ([Ubisoft News](https://news.ubisoft.com/en-us/article/4po3S9Pwp1YcgBmGPmQxAh/watch-dogs-legion-the-tools-that-built-london)).
- **Generated from tags:** gender, fashion, occupation, income, personality (including how they respond to insults), subculture, hobbies and relationships. The method is a "spider-web" in which one visible fact implies more facts. Recruits' traits follow "geo-like location properties and … census data about who lives there and why … and what jobs were available there" ([Stevivor](https://stevivor.com/features/interviews/watch-dogs-legion-ubisofts-sean-crooks-details-changes-to-play-as-anyone/)).
- **Lazy promotion ("uprezzing") in three tiers:**
  1. spawned background people, deleted when far away
  2. "fully uprezzed" people, stored with backstory, DedSec affinity, relationships and a **24-hour schedule of roughly one-hour activities that repeats "until something changes"**
  3. storage for relatives and incidental contacts

  Interacting with someone promotes them **and their connections**.
- **Ties propagate effects.** "Those memories propagate to their enemies and friends." Hospitalizing someone changes their family's schedules. Player actions are stored and explain later opinions, and some people "hate Dedsec so much that you can never recruit them again". Recruitment missions are assembled at runtime from the recruit's backstory, for example "the nearest pub with a server". Known contacts cluster geographically, so you meet people you know (Walsh calls it "the anecdote factory").
- **Criticism.** Reviewers found the bios didn't carry into how recruits act or speak: "none of that will translate over to how these characters act in cutscenes or their personalities out in the field" ([CGMagazine](https://www.cgmagonline.com/reviews/watch-dogs-legion-pc-review/)). Recruits felt interchangeable once you knew which perks mattered, and recruiting had little risk ([Gamer Journalist](https://gamerjournalist.com/watch-dogs-legion-review-pc/), [Inverse](https://inverse.com/gaming/watch-dogs-legion-review)).
- **Lesson.** Generated depth that no mechanic reads becomes trivia. Every generated fact should change a price, an approach or an outcome.

### Crusader Kings III (Paradox, 2020): traits, stress, secrets and hooks
- **Stress runs 0–400.** It rises when a character **acts against their own traits**: a Compassionate character executing someone, an Honest one scheming, a Shy one courting. It also rises from the death of someone close.
- **Thresholds at 100, 200 and 300 trigger mental breaks**, at most once every 5 years. A break offers a choice: shed stress by gaining a coping trait (+20% stress loss each, stacking), or take more stress. At 300+ the outcomes include Murderer, Lunatic and suicide ([CK3 wiki: Stress](https://ck3.paradoxwikis.com/Stress)).
- **Secrets become hooks.** Secrets come from crimes or shunned acts and are found by events or the spymaster. **Blackmail** turns a Shunned secret into a **weak hook** (one use, usually an acceptance bonus) and a Criminal secret into a **strong hook** (forces most actions, reusable on a 5-year cooldown). A refused blackmail exposes the secret ([CK3 wiki: Hooks](https://ck3.paradoxwikis.com/Hooks)).
- **Scheme agents** accept based on opinion of the schemer and on personality:
  - Greedy: −35 if there is nothing to gain, but bribes count double
  - Cowardly: −100 "too dangerous"
  - Compassionate: −40 "immoral"
  - Vengeful: +40 against someone who slighted them

  Closeness to the target adds success: spouse +30, food taster +20, friend +20. **Discovery chance on execution = 100 − Secrecy**, plus a monthly breach chance ([CK3 wiki: Schemes](https://ck3.paradoxwikis.com/Schemes)).
- **Lesson.** Small, legible, additive modifiers ("−40 immoral") make personality-driven recruitment readable as a **price**. That is close to our "every order shows its full price" rule.

### RimWorld (Ludeon, 2018): few traits, loud effects
- **1–3 traits per adult**, drawn from **spectra** (one per axis, e.g. Sanguine +12 … Depressive −12 base mood) with mutual exclusions. Each trait has blunt numeric effects: Greedy −8 mood with no bedroom, Kind gives +15 opinion, Industrious gets −20 opinion from Lazy pawns ([RimWorld wiki: Traits](https://rimworldwiki.com/wiki/Traits)).
- **Mental breaks** come at fixed mood thresholds: minor below 35%, major below 20%, extreme below 5%. The extreme breaks include berserk and catatonia. After a break, catharsis gives +40 mood for 3 days ([RimWorld wiki: Mental break](https://rimworldwiki.com/wiki/Mental_break)).
- **The storyteller is a director, not a cast.** Cassandra, Phoebe and Randy pick incidents using wealth, colonist count, time since the last big event and recent deaths. Population-adding events taper with colonist count: the factor falls from 8.0 at 0 colonists to 0 at 12 ([RimWorld wiki: AI Storytellers](https://rimworldwiki.com/wiki/AI_Storytellers)).
- **Lesson.** Three readable traits with visible numbers beat 50 hidden facets for a commander's eye. RimWorld chose readable over rich; DF chose rich over readable.

**Mechanic in one line:** Legion generates lazily from census tags and propagates memory along ties but lets depth go unused; CK3 turns personality into readable price modifiers and secrets into hooks; RimWorld keeps traits to three with loud numbers.

---

## Synthesis: what to borrow, and what to avoid

Notes for Motive and Pulse. These are not decisions.

**Borrow**
1. **Bands, not sliders.** Use a wide neutral band that produces no text and describe only out-of-band traits (§1). It fits "readable > rich" and the brief's small integer scales.
2. **A culture baseline plus personal deviation**, phrased as deviation ("unlike others at the docks, …") (§1).
3. **Needs derived from traits, each with a weight and one counter.** Meeting a need resets the counter and an unmet need decays. This makes needs a closed list the checker can verify (§2).
4. **Witness-only knowledge.** A fact exists for a person only if they saw it or were told by someone who knew it. This is DF's rumour rule and matches "information moves through people" (§7).
5. **Identity in three states (alias / name / face) stored per observer.** This lets the cell operate under covers and lets investigators build a case from partial reports (§7).
6. **Recruitment technique chosen against the target's values and ties** (fear, trust, revenge, ideology, bribe, blackmail), with recruitment able to travel along a turned person's ties (§7).
7. **Interrogation yields one link up the chain**, and fear can stop a witness naming the head ("too scared to identify Earthhells") (§7). This is close to our Talker test.
8. **Lazy detail.** Store people as records and re-instantiate detail when needed (DF offloading, Legion uprezzing) (§10).

**Avoid**
1. **Free interrogation.** In DF, questioning an innocent costs nothing, so players question everyone (§7).
2. **A shared RNG stream.** In DF, one parameter change reshuffles history (§8).
3. **Generated facts no rule reads.** This is Legion's criticism and DF's "effects still unknown" (§1, §11).
4. **Unbounded memory or lie systems.** Tarn added lie types one by one for memory and CPU reasons (§7).

---

## Sources

**Primary: Tarn Adams / Bay 12**
- Tarn Adams, "Secret Identities in Dwarf Fortress", AIIDE 2017 EXAG workshop (AAAI): https://ojs.aaai.org/index.php/AIIDE/article/view/12963 · PDF https://cdn.aaai.org/ojs/12963/12963-52-16480-1-2-20201228.pdf
- Tarn Adams, "Simulation Principles from Dwarf Fortress", *Game AI Pro 2* ch. 41 (2015): http://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter41_Simulation_Principles_from_Dwarf_Fortress.pdf
- Tarn Adams, "Emergent Narrative in Dwarf Fortress", in Short & Adams (eds.), *Procedural Storytelling in Game Design* (CRC, 2019). The chapter's existence is confirmed; I did not read its text: https://www.routledge.com/Procedural-Storytelling-in-Game-Design-1st-Edition/Short-Adams/p/book/9781138595309
- Bay 12 dev log 2014: https://www.bay12games.com/dwarves/dev_2014.html
- Bay 12 dev log 2015: https://www.bay12games.com/dwarves/dev_2015.html
- Bay 12 dev log 2018: https://www.bay12games.com/dwarves/dev_2018.html
- Bay 12 dev log 2019: https://www.bay12games.com/dwarves/dev_2019.html
- Dwarf Fortress Talk #8 transcript (pre-2014): https://www.bay12games.com/media/df_talk_8_transcript.html
- Bay 12 links page (talks and interviews index): https://bay12games.com/dwarves/links.html

**Interviews**
- Game Developer, "Interview: The Making of Dwarf Fortress" (John Harris, 2008): https://www.gamedeveloper.com/design/interview-the-making-of-dwarf-fortress
- Game Developer, "Q&A: Dissecting the development of Dwarf Fortress" (2019): https://www.gamedeveloper.com/design/q-a-dissecting-the-development-of-i-dwarf-fortress-i-with-creator-tarn-adams
- Game Developer, "How Tarn Adams upgraded and optimized Dwarf Fortress for its official Steam release" (2023): https://www.gamedeveloper.com/programming/how-tarn-adams-upgraded-and-optimized-dwarf-fortress-for-its-official-steam-release
- Game Rant interview (Nov 2022): https://gamerant.com/dwarf-fortress-interview-evolution-after-release/
- Stack Overflow podcast 377 (2021): https://stackoverflow.blog/2021/09/21/podcast-377-you-dont-need-a-math-phd-to-play-dwarf-fortress-just-to-code-it/
- HASTAC/Stanford interview (2010 repost; unreachable when checked, cited via search summary): https://dhs.stanford.edu/algorithmic-literacy/an-interview-with-tarn-adams/
- Kill Screen, "Dwarves can dream now" (2014): https://www.killscreen.com/dwarves-can-dream-now-dwarf-fortress/
- PC Gamer, GDC 2025 talk coverage (headline quote only; body not retrievable): https://www.pcgamer.com/games/sim/dwarf-fortress-dwarves-are-more-human-than-human-creator-says-theyre-allowed-to-embody-bigger-emotions-theyre-allowed-to-make-bigger-mistakes-theyre-allowed-to-do-anything/

**Dwarf Fortress Wiki (current namespace unless noted)**
- Personality trait: https://dwarffortresswiki.org/index.php/Personality_trait
- Personality value: https://dwarffortresswiki.org/index.php/Personality_value
- Personality goal: https://dwarffortresswiki.org/index.php/Personality_goal
- Need: https://dwarffortresswiki.org/index.php/Need
- Emotion: https://dwarffortresswiki.org/index.php/Emotion
- Stress (v0.47-derived): https://dwarffortresswiki.org/index.php/Stress
- Memory (thought): https://dwarffortresswiki.org/index.php/Memory_(thought)
- Thought: https://dwarffortresswiki.org/index.php/Thought
- Thoughts and preferences: https://dwarffortresswiki.org/Thoughts_and_preferences
- Relationship: https://dwarffortresswiki.org/index.php/Relationship
- Historical figure: https://dwarffortresswiki.org/index.php/Historical_figure
- World generation: https://dwarffortresswiki.org/index.php/World_generation
- Advanced world generation: https://dwarffortresswiki.org/index.php/Advanced_world_generation
- Importing and exporting worlds: https://dwarffortresswiki.org/index.php/Importing_and_exporting_worlds
- Legends: https://dwarffortresswiki.org/index.php/Legends
- Labor: https://dwarffortresswiki.org/index.php/Labor
- Noble: https://dwarffortresswiki.org/index.php/Noble
- Diplomat: https://dwarffortresswiki.org/index.php/Diplomat
- Visitor: https://dwarffortresswiki.org/index.php/Visitor
- Rumor: https://dwarffortresswiki.org/index.php/Rumor
- Intrigue: https://dwarffortresswiki.org/index.php/Intrigue
- Villain: https://dwarffortresswiki.org/index.php/Villain
- Justice: https://dwarffortresswiki.org/index.php/Justice
- Maximizing framerate (v0.47-derived): https://dwarffortresswiki.org/index.php/Maximizing_framerate
- Unit list: https://dwarffortresswiki.org/index.php/Unit_list
- Dwarf Therapist: https://dwarffortresswiki.org/index.php/Dwarf_Therapist
- Release information 0.40.14: https://dwarffortresswiki.org/index.php/Release_information/0.40.14

**Criticism and analysis**
- Emily Short on James Ryan, *Curating Simulated Storyworlds*: https://emshort.blog/2019/05/21/curating-simulated-storyworlds-james-ryan/

**Comparison games**
- Game Developer, "How Watch Dogs: Legion's 'Play as Anyone' Simulation Works" (Tommy Thompson, 2020): https://www.gamedeveloper.com/design/how-watch-dogs-legion-s-play-as-anyone-simulation-works
- GDC Vault, "Census: The Systemic Backbone Behind Play As Anyone in Watch Dogs: Legion" (Christopher Dragert, 2021): https://www.gdcvault.com/play/1027018/Census-The-Systemic-Backbone-Behind
- Ubisoft News, "Watch Dogs: Legion – The Tools That Built London": https://news.ubisoft.com/en-us/article/4po3S9Pwp1YcgBmGPmQxAh/watch-dogs-legion-the-tools-that-built-london
- Stevivor, Sean Crooks interview: https://stevivor.com/features/interviews/watch-dogs-legion-ubisofts-sean-crooks-details-changes-to-play-as-anyone/
- Reviews: CGMagazine https://www.cgmagonline.com/reviews/watch-dogs-legion-pc-review/ · Gamer Journalist https://gamerjournalist.com/watch-dogs-legion-review-pc/ · Inverse https://inverse.com/gaming/watch-dogs-legion-review
- CK3 wiki, Stress: https://ck3.paradoxwikis.com/Stress
- CK3 wiki, Hooks: https://ck3.paradoxwikis.com/Hooks
- CK3 wiki, Schemes: https://ck3.paradoxwikis.com/Schemes
- RimWorld wiki, Traits: https://rimworldwiki.com/wiki/Traits
- RimWorld wiki, Mental break: https://rimworldwiki.com/wiki/Mental_break
- RimWorld wiki, AI Storytellers: https://rimworldwiki.com/wiki/AI_Storytellers
