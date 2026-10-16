# The dialogue generator's rule update (Fable, brief 012)

Status 2026-10-16: **a proposal for Sky. Nothing in `tools/`, `contracts/` or the GDD is changed by this file, and no live scope has been run.** It follows the three research strands in `briefs/012/research/dialogue/`: `01-systemic-architectures.md` (how systems pick, remember and de-duplicate lines), `02-overheard-and-barks.md` (overheard scenes and state barks), `03-craft-and-lore.md` (craft, lore, a 22-rule style guide). It is written against the generator as it stands: `briefs/012/fable-dialogue-generator.md`, `contracts/dialogue/` (`dialogue.rules.json`, `voices.schema.ts`), `tools/dialogue/` (`check.ts` D-0..D-12, `expand.ts`, `lookup.ts`, `situations.ts`) and the Handover's `contracts/missions/handover/text.schema.ts` (`lookup`, `pickIndex`).

Tags: **[F]** Fable builds it · **[K]** Keel (engine or rulebook) · **[L]** Latch (host port) · **[M]** Motive reviews the voices · **[P]** Pulse (watcher AI) · **[G]** Glyph (HUD) · **[SKY]** needs Sky's call. Numbers marked **[S]** are my starting values, to be tuned by playtest; no source gives a hard number for line length or how often lines fire (02, "What no source gives").

## 0. The change in one page

The generator today picks the most specific cell, then a hashed variant, and voices a person's replies, confirmation, questioning and gunfire. What the research says it lacks, and what this update adds:

| # | change | why (source) | touches | who |
|---|---|---|---|---|
| R1 | Ties are explicit: a fixed rank of qualifier families, never file order | Hades `pairs()` and Yarn node order break replay (01 anti-patterns); Valve ties are random only on purpose | `lookup`, `findCell`, D-13 | F, L |
| R2 | Three tiers, derived by the rules, never written by the model: `result`, `read`, `flavour`. Only flavour is budgeted, skipped or delayed | Hades Priority; Halo "flavor only" and skip fractions; TLOU priority levels (01 rule 2) | `dialogue.rules.json`, host | F, L |
| R3 | A seeded shuffle-bag replaces the hash-per-use picker for generated lines | TLOU, Valve, Hades, Ink (01 rule 4) | `lookup.ts` `bagIndex`, host | F, L |
| R4 | Who speaks and how often: co-speakers say different variants; a flavour budget; one flavour line per incident per person | Halo speaker types; Legion's attention system; Blacklist "incessant reminders"; Hades named cooldown (01 rules 5, 11, 12) | host, rules `budget` | L, SKY (numbers: Suture) |
| R5 | Memory-keyed cells: `last` and `times`, read from the sim's memory, never from what was said | Nemesis event record; Hades "which conversations have played is the state"; Firewatch; CiF SFDB (01 rules 7, 8) | schema, lookup, D-13, D-14 | F, K, L |
| R6 | State-banded cells: `bravery`, `resentment`, `standing` (and a Talker `fact`) | Wildermyth threshold + default; Legion's failure with situation-blind personas (01 rule 13) | schema, lookup, D-13 | F, M |
| R7 | Knowledge gating: a line may cite only what its speaker knows. `fact` cells for the Talker; recall words only where history is implied; need and secret supplied only once the cell knows them | Firewatch "Delilah would know things the player didn't"; CiF's global-truth simplification; Orkin "If the AI didn't say it, it didn't happen" (01 rule 9; 02 §5) | D-14, host | F, L |
| R8 | Scene beats: one `gist` beat, a need planted by slot, a rules-written title, a marker before the scene, a log after | Hitman (outsiders, three facts, one restatement); Desperados III "?" and hints log; Façade gist; Disco Elysium repeats key facts across voices (02 §1, §8, §14) | schema, D-17, host, HUD | F, L, G |
| R9 | Watcher state barks, derived from the engine's own state codes, with a step down; no bark may claim a state the engine lacks | Thief a1/a2/a3 + "back to calm"; Deus Ex's 18 bark types; MGS Evasion/Caution; Orkin (02 §2, §4, §5, §7) | `situations`, rules `states`, D-13 | F, K, P, SKY |
| R10 | Checker rules D-13..D-19 (qualifiers, knowledge words, feeling words, rhythm and stance, slot fuzz, scene planting, states) | 03 §5 style guide and §4 slot pitfalls; 02 | `check.ts` | F |
| R11 | An audit by simulation and a "why this line" trace; stable line ids | Short's thousands of playthroughs; TLOU rule logs; Firewatch loc-by-string failure (01 rules 14, 15) | `cli.ts --audit`, host log | F, L |
| R12 | The style guide (03's 22 rules, merged with 02's) with each rule marked checker, reviewer or Sky | 03 §5; 02 | §12 below | F, M |

Order of work and gating are in §13. The one paid step (a live run of `street`, `civic` and `scenes` with the new cells) is **new paid spend** and waits for Sky's go-ahead through Aster; everything before it is code and tests.

**Two lessons from this week govern how it ships.** (1) Every addition to a pinned contract (`dialogue.rules.json`, `voices.schema.ts`, `text.schema.ts`) must be optional in its schema, or the recorded runs r017..r019 stop replaying: on 2026-10-16 `cards` and `debrief` did exactly that to the Handover text, and the fix was to parse only the part the checker reads (`tools/levels/dialogue/replay.ts`, `ReferenceText`) and declare the drift. (2) Any edit to a file in `tools/levels/dialogue/hash.ts`'s `VERDICT_FILES` (the checker, the expander, the three contracts, `text.schema.ts`) moves the verdict hash: declare it with `bun tools/levels/dialogue/drift.ts declare "<cause>" --rules D-13,...` in the same step.

## 1. What the research settles, in eight lines

1. **Most specific wins, general falls back, and the general always exists** (Valve, TLOU, Firewatch, Wildermyth, CK3). We already do this; the gap is ties and states.
2. **Essential and flavour are different queues** (Hades, Halo). Our replies carry rulebook results; they are never skipped.
3. **A shuffle-bag, not a coin flip** (TLOU, Valve, Hades). Today's picker can alternate A B A B and starve C.
4. **History is the state** (Hades, Nemesis, Firewatch). Memory-keyed lines need no new engine state; the rulebook already records `{act, by, place, time, witnessed}`.
5. **Knowledge is set by the act that conveys it** (Firewatch, CiF). Our Learned fact lands when the act lands; our lines may cite only what the speaker has in memory.
6. **Plant a fact through outsiders talking shop, and restate it once in the system's voice** (Hitman). Our restatement is the Learned line.
7. **Voice every step of a watcher's ladder, including the step down, and claim only states the engine has** (Thief, Deus Ex, FEAR). Suborned has no per-watcher suspicion ladder: Alert is mission-wide and never falls within a mission (`rulebook.alert.rule`), so there is no honest "curious" bark. What the engine does have is `alerted, advance, engage, hold` plus `guardReturn` and `patrolResume`.
8. **Specific lines are memorable, so spend them where they mean something** (Blacklist's three tiers; Legion's situation-blind personas). Qualified cells carry the memory, the need and the state; the plain cell is the quiet default.

## 2. Table schema changes (`suborned.voices/1`, additive and optional)

Nothing the model writes changes shape except by new optional fields; `r019` (the accepted lexicon run) and every stand-in stay valid. The model writes **less** than 01 might suggest: tiers, priorities, titles, budgets, qualifier ranks and the state list are all rules, not table fields (01's anti-pattern: judge a table shape by how often the model fails it).

### 2.1 Cell: new optional qualifiers

All enums are imported from the rules files, as `SECRETS` already is, so an unknown value is rejected.

| field | values | from | applies to | mode |
|---|---|---|---|---|
| `last` | the Fixer-facing memory acts: `ask refusal pay help pressure job told saw-act saw-gunfire saw-violence saw-weapon-drawn` | `rulebook.memory.acts` keys | `reply/*`, `questioned/*`, `confirm/call` | truth |
| `times` | `once`, `again` (the same act remembered twice or more) | memory entries | only with `last` | truth |
| `fact` | `fixer-face`, `cell-car`, `place-time` | `people.rules` `talker.facts` | `questioned/gives` | truth |
| `bravery` | `informant helper accomplice fighter` | `people.rules` `scales.bravery.steps` | `reply/*`, `confirm/call` | colour |
| `resentment` | `compliant resentful informer` | `scales.resentment.steps` | `reply/*`, `questioned/*` | colour |
| `standing` | `hero`, `menace` | the district's band | `reply/*` | colour |

Existing qualifiers `need` and `secret` stay (truth). **Truth qualifiers replace** the plain cell's lines (the plain line would be false or blind: "who are you?" to someone who remembers you). **Colour qualifiers also replace** in v1, with tight caps; adding their lines to the plain pool instead (TLOU's dummy criterion, 01 §2) is a later option, taken only if the audit (R11) shows starved variants. Reason to wait: the bag then needs a pool that changes as state changes, and nothing yet shows it is needed.

A qualified cell always keeps `temperament`: the temperament is the voice (03 rule 6), and a memory line that drops it would override the voice exactly when the person has the most to say.

**Caps (replace the single `qualifiedCellsPerGroup: 12`):** need and secret 12 as now, `last`/`times` 10, colour 8, `fact` 3, per group. At 3 variants each that is at most about 100 extra lines a group on a base of 39 cells (about 117 lines): up to double. The base cost is the number in `situations.ts`: 251 required cells over 7 groups, 130 people, the clerk group alone speaking for 58 of them. The caps are ceilings, not targets; see §7.4 for the first batch.

### 2.2 Cell: nothing else

No `once`, `odds`, `skip`, `weight`, `priority` or `cooldown` in a cell. 01 rule 6 wants once-only to be first-class, but in this game it is a property of a situation, so it lives in the rules (§5.3). Giving the model a weight to tune is the anti-pattern 01 names ("hand-tuned global weights as the main control").

### 2.3 Beat: `gist`

`Beat` gains `gist: true` (optional). Exactly one beat per scene variant carries it, at index 1 or later, and it is the beat that conveys what the scene teaches (§6).

### 2.4 Scene: nothing else yet

A `need` qualifier on a scene (a debt-specific lovers' scene, say) would let a scene imply a need by content. It would cost 8 needs × 6 ties × 4 temperaments. Not now; §6.2 explains why a scene may carry the need only by slot.

### 2.5 Lines (`suborned.lines/1`): entries carry their qualifiers

State-banded and memory-keyed cells cannot be resolved when the person is expanded (the state does not exist yet). The expander emits, per situation, the plain entry (resolved for the person's own `need` and `secret`, as now) **plus one entry per state-qualified cell** that the person's group and temperament have, with the qualifier fields on the entry. `LineEntry` gains the same optional fields (`last times fact bravery resentment standing`), each plus a stable `id`: `personId/cellKey/qualifiers` (R11). The host's query supplies the person's current values; the lookup counts matching fields as today and breaks ties by rank (§3). A Handover entry has none of these fields, so the Handover is unchanged.

### 2.6 Lexicon: no change in v1

History realisation strings with a `{last}` slot (CiF's `%SFDB_(label,i,r,n)%`, 01 rule 8) would need the host to fill a slot at play time. Precedent exists (`{temperament}` in results), but v1 does without: a memory-keyed cell **is** the realisation, because every line in it already knows which act it follows ("Asked once already. Dearer now."). This keeps D-3 (no slot left in a filled line) true without exception. A `{last}` slot is phase 2, and needs sim-time words ("this shift", "last visit") from Keel.

### 2.7 Rules file: new optional sections

`dialogue.rules.json` gains `tiers`, `qualifiers` (the rank, caps and where each applies), `budget`, `states`, `feelings`, `recall`, `shapes`. Each is optional in `dialogue.rules.schema.ts` until the recorded runs retire (see the two lessons, §0).

## 3. Picking

### 3.1 Lookup: score as now, ties by rank

`lookup` (and the expander's `findCell`) keeps "the entry naming the most fields wins". New: when two entries score equally, **the higher rank of their qualifier families wins**; file order is never used. Two entries with the same qualifier set are one cell written twice, which D-0 already rejects.

Rank, highest first **[S, for Motive's review]**: `fact`, `last`, `secret`, `need`, `bravery`, `resentment`, `standing`. `times` is not ranked: it only refines `last`. Reason: truth before colour; what the person was told by an act before what they want; what they hide before what they want, because a known secret is the leverage Pressure reads (`people.rules` `secrets`).

The rank lives in `dialogue.rules.json` `qualifiers.rank`. A cell may carry several qualifiers; the specificity count rises with each, so a cell naming `last` and `bravery` beats one naming only `last`.

### 3.2 Variation: `bagIndex`

For generated lines only (the Handover keeps `pickIndex` until Latch and Aster agree to move it; one picker in the host is the better end state). Pure function of `(seed, key, n, count)`, O(n) like today's, no host state beyond the use count:

```
cycle(c)  = the indices 0..count-1 ordered by (fnv1a32(seed|key|c|i), i)
actual(0) = cycle(0)
actual(c) = cycle(c), and if count > 1 and actual(c)[0] == last(actual(c-1)):
              move that first element to the end            // never a repeat across the seam
bagIndex(n) = actual(floor(n / count))[n mod count]
```

For `count = 2` this alternates; for 3 or 4 every variant is heard once before any repeats and never twice in a row. The key carries the person's id (as now), so two people on one table are out of step. Test vectors: a fixed set in the same shape as `text.schema.ts` `pick.vectors`, plus property tests: every window of `count` uses holds each index once; no equal neighbours over 200 uses; stable across calls.

### 3.3 Query and commit are separate

The host asks "what would this person say?" without advancing the counter (previews, the Intel panel, a call's `CallQuery`), and commits the use when the line is shown. This is Yarn's `QueryBestContent` / `ContentWasSelected` split (01 §17). Lines that were previewed and never shown do not use up a variant.

### 3.4 Specific, then generic (Blacklist): deferred

Blacklist cycles its most specific barks first and drops to larger generic pools so the player never hears a memorable line twice (02 §3). The district-wide version would need a mission-long record of which qualified cells have been heard. Our approaches are rare (five to fifteen a mission), so I expect little repetition of a qualified template within a mission. **The audit (R11) decides**: if it reports a qualified template spoken more than twice in a simulated mission, add a per-mission "heard" set so the first speaker gets the qualified line and the next falls to the plain cell.

## 4. Priority tiers, derived

`dialogue.rules.json` `tiers` maps every event to a tier. Unknown events are rejected (D-13), the way unknown positions are.

| tier | events | guarantee |
|---|---|---|
| `result` | `reply/*`, `confirm/call`, `questioned/*`, `summon` | carries a rulebook result. Shown at the act's landing, never skipped, never given odds, never dropped for a cooldown or budget; may be delayed by at most the card's own beat time |
| `read` | the Scout's reads (`read/watch`, `read/listen`), the Learned line, scene results, the debrief | tells the player what they now know; same guarantee |
| `flavour` | `saw-gunfire`, `gunfire`, every `state/*` bark, `crowd-state`, idle talk | may be skipped, rate-limited, queued briefly and dropped |

Halo kept dialogue non-essential, so skip fractions were safe there; ours are safe only on flavour (01 §3). Priority when two lines compete for one speaker or one card: `result` over `read` over `flavour`; a higher tier replaces a lower one in progress; a scene owns the priority of all its beats and is interrupted whole, not beat by beat (TLOU, 01 §2). A bark that carries state must also exist as a state signal on the HUD (pill, cones, feed card), because players who cannot hear or read it lose the information (Blacklist's accessibility review, 02 §3; Dishonored read alert from a meter, 02 §6).

## 5. Who speaks, how often, and what is said once

### 5.1 Speaker choice for a crowd event

For `saw-gunfire`, `gunfire` and the crowd states: candidates are the simulated people within sight or earshot (rulebook `witness-range-m`, `crowd.scatter`). Score by role relative to the event (the person the event happened to or whose tie it did, then security, then the nearest bystander), then break ties by `fnv1a32(seed|incident|person)`. At most **3** people speak flavour per incident **[S]**. The rest are silent. This is Halo's speaker types and Valve's "best score speaks" (01 §1, §3), made deterministic.

### 5.2 Co-speakers differ

The k-th speaker of one incident on one table uses variant `(bagIndex + k) mod count`. While k ≤ count they never say the same thing. With the cap of 3 and cells of 2 to 4 variants, a 2-variant cell can repeat between the first and third speaker; D-16 pushes flavour cells to 3 variants.

### 5.3 Once

The rules list the situations that are once per person per incident or per mission: `saw-gunfire` and `gunfire` once per incident per person (the rulebook already counts an incident once, `alert.rule`); the lovers' `recalled` summons once per mission. This is Ruskin's closing regret made first-class (01 §1) without a table field.

### 5.4 Budget

`budget` **[S]**, flavour only, counted in sim time so replays match:

- card-wide: at most 3 flavour lines per 10 s; excess queues up to 2 s and is dropped;
- per person: at least 60 s between two flavour lines (Hades's named cooldown `SaidHelloRecently` is 80 s, the one number 01 found for barks);
- per incident: the cap of §5.1.

`result` and `read` bypass the budget. The numbers are for Suture's playtest to tune; I have no source for them beyond the Hades cooldown.

## 6. Scene beats (overheard, call, watch)

How a scene reaches the player today: the card opens when the act starts, beats arrive as the act progresses (a call, a beat every 1.5 s), and the Learned line lands with the act (`HostFeed.cs`). The engine teaches at the act's end, so a cancelled Listen shows the beats so far and teaches nothing. That is why this update does not adopt Façade's resume and reestablish beats (01 §16): nothing is lost by a cut that a reshow would repair.

### 6.1 `gist`

Exactly one beat per variant is the `gist`: the beat where the fact the scene teaches is said. It sits at index 1 or later (never the opening beat: a scene needs a setting before its point). Uses: the collapsed feed card shows the gist beat, not the last (03 on Oxenfree: the feed collapses an exchange and players click away, so the point must survive being the only beat read); the history row quotes it; the Learned line restates it in the system's voice (Hitman's one-sentence restatement, 02 §1). **[L, G]** to do the card and the row.

### 6.2 What a Listen scene may carry

A generated Listen scene is written by tie kind × temperament, so it **cannot know the person's need**. A scene that implied a debt by content ("the lender's man is round") would be false for the next person with the same tie and a different need. Therefore:

- the scene carries the need **only through the slots** `{need} {own} {met}`, and every Listen variant plants one in a beat at or before the gist (Hitman: the exchange names the thing; the system restates it), inside a line that is not the slot alone ("Couldn't sleep. {own} again.", not "{own}.");
- the scene's content words stay need-agnostic: no literal need noun (the accepted lexicon's `noun` forms: "debt", "rent", ...) outside a slot (D-17);
- the Scout's read-out (`lexicon.reads`) states the need plainly after the scene, and so does the Intel panel: three voices, as Disco Elysium repeats key facts (02 §14). That is what lets the scene itself stay oblique and in the house style (03 rules 2, 4, 19).

This resolves a real tension: Hitman's overheard exchange names its facts, strand 03 wants subtext. Both hold if the exchange names the *thing* (the slot) and withholds the *feeling*.

### 6.3 Planting and shape

- Two voices where a tie gives a partner; a named colleague, customer or passer-by where it does not (D-10 as now). Outsiders and service workers talk shop; a scene about a third person is dead (Mamet, 03 §2; Hitman's outsiders, 02 §1).
- 3 to 6 beats, aim 4 (Hitman, Blacklist and FEAR examples run 2 to 5 lines; no source gives a number, 02). Each beat 12 words or fewer as now; Dishonored's rule is that anything that stops play must be shorter still, and a Listen does not stop play, so the limit stands.
- Variants differ in **stance**, not synonyms (03 rule 11; D-16).
- A scene is addressed to someone in the world, never to the player (BioShock, 03 §3; the Handover's own scenes).
- The fact is never planted on a beat that is only a restatement of the previous one (Mamet's "as you know").

### 6.4 Marker before, log after

Mark a scene before it plays and write its result to a log, so the player never has to re-listen (Desperados III's "?" over listenable civilians and its hints log, 02 §8). The history rows already log results; the marker is a **[G]** question: a "can be heard" mark on a person whose need is unknown. A rules-written `title` ("Overheard: Inez and the door guard", from the tie kind and the two names) opens the card, so the scene is named before beat 1. Titles are code, in `dialogue.rules.json` `listen.titles`, never the model's.

### 6.5 Missing a scene

A missed or cancelled scene must cost time or a worse route, never a restart (Black Flag Resynced, 02 §12). It already does: the Listen is a priced act, the need can be learned again, and the mission checker requires no single contact to be essential (GDD §09).

## 7. Memory lines

### 7.1 What is read

The sim's memory entry: `{act, by, place, time, witnessed}`, cap `memory-cap`, oldest saw-act dropped first (`rulebook.memory`). The host reads, for each person, their **latest** entry whose `by` is the Fixer or whose act is a witnessed one, and the count of that act. **Lines do not write sim state.** Valve and Naughty Dog let a played line write a fact (01 rules 7); here only a `remember` change writes memory (rulebook), because text is host content and the same orders must give the same result whatever was said. The host keeps only presentation counters (the bag's `n`).

### 7.2 Two layers (Nemesis)

- **Personal**: `last` (+ `times`): what happened between this person and the Fixer.
- **District**: `standing` (hero or menace) and `last: told | saw-gunfire | saw-violence | saw-weapon-drawn | saw-act`: what the person has heard or seen. The rulebook already has both, as `told` entries (one hop, never retold) and witnessed acts.

Nemesis keyed its lines on the last encounter and, separately, on recent player activity (01 §8). Same two layers. **Patent caution [SKY]:** US 10,926,179 claims NPC hierarchy, power centres and parameter changes after encounters. If Suborned ever gives civilians rank (the GDD's "promoted" between missions) alongside remembered taunts, Sky should know the patent exists.

### 7.3 Reachable memory cells are derived, not typed

`replyCells` in `situations.ts` already walks the rulebook (4 steps for Help, 2 otherwise) per temperament and band. Extend that walk to record the memory after each step; every reply reachable with `last` set and `times: again` is a **memory cell the tables may add**, and D-13 rejects a memory cell the walk cannot reach (a dead cell, as D-0 does). Nothing about the set is typed by hand. **[K]** to confirm the derivation matches `rulebook.reads` (`helped`, `refused`).

### 7.4 First batch (to keep the model's budget small)

Not required by the checker; asked for in the first live run, per group and temperament, in this order, stopping at the cap:

1. `reply/ask` with `last: refusal` (the GDD's remembered refusal: "later offers +$10,000, +1 T"): the one memory every player causes.
2. `reply/help` with `last: help, times: again` (gratitude; the kept chain).
3. `reply/pressure` with `last: pressure, times: again` (fear and resentment grow).
4. `reply/*` with `last: saw-violence` or `saw-gunfire` (a witness who answers differently; feeds Standing).
5. `reply/*` with `last: told` (heard of the cell along a tie, never met: "I've heard about you").

Target once memory exists **[S]**: at least 25% of a person's `result` lines are qualified (01 rule 13). The audit reports it; below the floor, spend model budget on qualified cells before more plain variants (01: Legion's recruits read as one voice because persona lines ignored the person's situation).

### 7.5 Form

A memory line reacts to what the act **caused**, not to the act (03 rule 4: "Lender says I'm square", not "You paid my debt?"); it does not restate the event; it uses no feeling word (R10); it is in the temperament's flouted maxim (03 rule 6). Illustrations, not table text:

| cell | line |
|---|---|
| `reply/ask/refused`, Mercenary, `last: refusal` | "Back again? Same answer, dearer." |
| `reply/ask/refused`, Loyal, `last: refusal` | "It's entered. Please don't make it two." |
| `reply/help/kept`, Idealist, `last: help, times: again` | "Twice now. Nobody keeps count of that." |

## 8. Knowledge gating

The research case (Firewatch, CiF, Dwarf Fortress legends, 01 rule 9): a line may cite a fact only if its speaker could know it. Suborned already models who saw what; the lines must follow it.

1. **Build time** (extends D-8): a cold Ask or a Pay surfaces no need; need slots only in the replies that follow a known need (`reply/help`, `reply/pressure`, `questioned`).
2. **The host supplies `need` and `secret` to the query only when the cell knows them** (Intel/`knownBy`). A person never names their own need to a Fixer who has not learned it, even though the expander knows it. **[L, K]**
3. **Recall words** (`again`, `already`, `last time`, `before`, `remember`, `once more`, ...) are allowed only in cells whose key implies history: `kept` and `confirm`, and any cell with `last`. The plain `turned`, `refused`, `not-yet` and `suspicious` replies can be a first meeting (a first Help can even be `not-yet`), so they may not recall (D-14).
4. **The Talker gives only a fact it holds.** `questioned/gives` is qualified by `fact` (`fixer-face`, `cell-car`, `place-time`); the plain `gives` cell may name no fact noun (D-14), because it cannot know which fact it gives. The host supplies `fact` from the person's memory (`rulebook.memory.acts[*].facts`, `faceFacts`). `lies` and `nothing` never name one. Illustrations: "A grey car at the kerb. Twice." · "I'd know the face again." · "Near the steps, about the chime."
5. **The cell is known by face only** (GDD §04 knowledge). No line uses the Fixer's name or the cell's names: `tone.signal` already rejects the game's words; `address` is the only way a person names the Fixer.
6. **A bark claims only a state the engine has** (§9): Orkin's "I need reinforcements" with no mechanism.
7. **A scene may not teach what its verb cannot**: a Watch scene teaches the routine and no need (D-3 already forbids need slots there).

## 9. Watcher state barks

### 9.1 What exists and what does not

Suborned has no per-watcher suspicion ladder; Alert is mission-wide and rises only. The engine's watcher states are the hostile machine (`routine, alerted, advance, engage, hold`), the returns (`ai.guardReturn`, `ai.patrolResume`), the crowd's (`startled, fleeing, sheltered, returning, back`), and the incident events (`run-seen`, `drawn-seen`, `alarm`). The Handover hand-writes lines for its roles (`ai-state`, `run-seen`, `drawn-seen`, `alarm`, `crowd-state`); the generator writes none. Generated missions will have generated guards, patrols and watchers who need them.

### 9.2 Taxonomy, mapped to the research

New event `state`, with `outcome` the engine state. The list is derived from the engine's codes and drift-tested like the summons (`summonCodesFrom`), never typed; `dialogue.rules.json` `states` names, for each, the groups that speak it and the source code.

| outcome | engine source | speakers | research precedent | exists |
|---|---|---|---|---|
| `noticed` | a found fault (`cell.runAlert`, Handover `run-seen`) | security | Thief a1; Deus Ex PreAttackSighting | yes |
| `drawn` | a weapon drawn in sight (`drawn-seen`) | security, crowd | Thief a3; Deus Ex Surprise | yes |
| `alarm` | gunfire raises Alert (`alert.gunfire`) | security | Deus Ex GoingForAlarm; Thief "passing it on" | yes |
| `alerted` `advance` `engage` `hold` | the hostile machine (`ai`) | security | Thief a2/a3; FEAR | yes (Handover `ai-state`) |
| `return` | `ai.guardReturn`, `ai.patrolResume` | security | Thief "back to calm"; Blacklist "Guess it was nothing"; MGS Caution | engine yes, **line no** |
| `startled` `fleeing` `sheltered` `returning` `back` | `crowd` | every non-security group; Fearful differs (`startleFearful`) | Deus Ex Surprise; Blacklist | yes (Handover `crowd-state`) |
| `tampered` | a loop or reroute noticed (Tech) | security (watcher) | Deus Ex Futz | **needs [P], [K]** |
| `pass-on` | a witness reports to security | security | Thief `rint/rbod`; MGSV "radio their findings" | **needs [K]** (`witness.sees`, `contact.informs`) |

Deus Ex's other types (Idle, Gore, OnFire, OutOfAmmo, CriticalDamage, TearGas, Alliance) have no Suborned counterpart and are not proposed. The rows marked "needs" are not in the first batch; the step down (`return`) is the one the research most insists on (02 rule 7) and the one the engine already supports.

### 9.3 Form

- **One fact per bark** (Dansky), clear before clever (Anderson): "Walking, please. Running gets noted."
- **A question and an answer** when two watchers are present, to show a state change was noticed (Orkin, Walsh, Thief's companion lines): "Did you hear that?" / "Wind. Back to your post." That is a scene (`watch/<group>` or a new `state` scene), not a single cell.
- **The step down closes the alarm in the same register**, and names the cause it dismisses ("Just the wind.") (Thief). The patrol's step down is procedure, not relief.
- **Flavour, so budgeted** (§5.4): a bark is a signal only if the HUD also shows the state.
- **The machine patrol is not a person.** Its closed phrasebook of 15 to 25 phrases, reused verbatim, is the proposal in 03 §3.4. **[SKY]**: it needs a `machine` voice group outside the people rules and an exemption from D-7 across events.

## 10. Checker rules D-13..D-19

Each has an id, a source, a failing example in `tools/dialogue/test/` and, where it counts, a calibration against the Handover text (the reference for **coverage** by D-12, not for style: 03 §0 measured its house rhythm as the thing to break). Calibration, counted with a scratch script on the 189 person lines of `reply confirm questioned saw-gunfire gunfire` in `text.json`:

- two-sentence "Fragment. Fragment." lines: waitress 68%, driver 77%, guard 77%, crowd 40% (03 measured 60% across all 740 lines);
- feeling-word hits: 7 of 189 (3.7%): "I'm still so frightened", "You keep helping. I feel safer.", "I don't trust it", "I hate this", plus false positives ("love" as an endearment; "I'm afraid" as the period courtesy);
- scene beats: 53% two-fragment, 33% one-beat.

| id | rule | check | calibration / threshold |
|---|---|---|---|
| D-13 | **Qualifiers**: every qualifier value is in its source list (unknown = reject); `times` only with `last`; every qualified cell has its plain cell; a memory or `fact` cell is reachable per the rulebook walk (§7.3); caps (§2.1); every event has a tier; every `state` outcome is in the engine's codes | structural | no calibration; failing examples per clause |
| D-14 | **Knowledge words**: recall words only in cells that imply history; no fact noun in a plain `questioned/gives`; no line names the Fixer | word lists in `rules.recall`, `rules.factNouns` | lists start small (above); the lexicon's own nouns are excluded; reviewer sample for the rest |
| D-15 | **Feeling words**: the speaker's own feeling named in a `result` or scene line | hard list: `frightened terrified scared safer grateful thankful relieved ashamed hate`; soft list (at most 1 per 25 lines per group): `afraid trust happy angry worried nervous proud glad` | catches 6 of the 7 Handover hits (4 distinct lines hard or soft, `I'm afraid` twice); "love" is excluded as address; `I'm afraid` is soft by design (03 rule 3) |
| D-16 | **Rhythm and stance**: per group, no one shape (one beat, two fragments, question, run-on, break-off) above 40% of its filled lines; a cell of 3 or more variants uses at least two shapes and at least two different opening words; flavour cells have 3 or 4 variants | shape classifier (the scratch script's, in `tools/dialogue/shape.ts`) | the Handover would fail at 68 to 77%; that is the point (03 rule 9). **[S]** 40% from 03 |
| D-17 | **Scene planting and gist**: exactly one `gist` per variant at index 1 or later; the focus (and the partner on a tie) has spoken by the gist; every Listen variant plants a need slot at or before the gist, in a line that is not the slot alone; no literal need noun outside a slot in a Listen beat | structural + the accepted lexicon's `noun` forms | scene stand-ins updated; `lexicon` r019 unaffected |
| D-18 | **Slot fuzz** (03 §4): expand every template with the shortest, the longest and a plural-looking form of each slot it uses; run D-2 on all; no "a" before a vowel or "an" before a consonant, no doubled article ("the the"), no `my {own}`, `{need}'s` or `a {noun}`; at most two slots a line | fuzz | 03: "the slot is never the point" |
| D-19 | **Pick properties**: `bagIndex` serves every variant once per window of `count`, never equal neighbours, stable; co-speakers differ up to `count` | property test over 200 uses × seeds | unit test, not a table check |

D-12 (the Handover's reference lines matched in key and variants) stays as it is.

Ordering by cost and protection: D-13 and D-19 are structural and cheap and should land first; D-14 to D-16 need the first live tables to tune their lists and thresholds; D-17 and D-18 are mechanical.

## 11. Audit and trace (R11)

- **Stable line identity.** Every expanded entry has an id `personId/cellKey/qualifiers`; a replay log records `{id, variant, tier, shown | dropped}`, never text (Firewatch broke its localisation by keying on English strings, 01 §5). Facts stay a closed, typed set from the rules files; nothing ever parses generated text back into state (Wildermyth, 01 rule 15).
- **`bun run dialogue --audit [--uses 24]`** (offline, deterministic): per group, variants never reached in N uses of a key; seam repeats (must be 0); the share of reachable memory and state cells with no qualified cell; the share of a person's `result` lines that are qualified; the most-repeated normalised template per simulated mission across people (the "one template, ten mouths" measure, 01 rule 5); `result` and `read` lines dropped (must be 0). Failing thresholds make the audit exit 1.
- **A "why this line" trace** in the host: the query's facts, the candidate cells with scores and ranks, the winner, the bag position (Naughty Dog filtered by character, 01 §2). **[L]**

## 12. The style guide

03 §5 gives 22 rules. 02 adds nine. Each is marked **C** (checker, with the D-id), **R** (reviewer: Motive for voice, Keel for rules; a sampled review per run as GDD §09 says) or **S** (needs Sky). Examples are in 03 §6.

**Readability and truth**
1. The outcome reads at a glance: a reply's first three words and the result line say yes, no, not yet or reported. **R** (03)
2. Who wants what: every person line moves toward what the speaker wants now. **R** (03)
3. No words for the speaker's own feelings; show behaviour, repetition, a change of plan. **C D-15** (03)
4. React to what the event caused, not the event. **R** (03)
5. No "as you know": the briefing and cards are the camera. **R** (03)
6. **One fact per line; the fact in the first three words or in the gist beat.** **R** (Dansky; Oxenfree via 03)
7. **Clear before clever.** A line must still be readable the fifth time. **R** (Anderson; Pagliarulo; Dansky)

**Temperament and role**
8. One flouted maxim per temperament: Mercenary turns everything to price, Fearful says too much or breaks off, Loyal answers in procedure, Idealist answers the question behind the question. **R** (03; Grice)
9. A closed tic set per temperament, once per exchange. **R** (03)
10. Roles speak their work: 8 to 15 job nouns a group, needs said through them. **R** (03)
11. Break the house rhythm. **C D-16** (03)
12. The plainest line is the default; flavour lives in qualified cells. **C D-13** (03; Wildermyth)
13. Variants differ in stance, not synonyms. **C D-16** (03; Compton)

**The state's voice and the people's**
14. The city speaks agentless, passive and grateful in advance. **R** (03)
15. Machines repeat, people vary: the patrol's closed phrasebook. **S** (03 §3.4)
16. The optimistic surface lives in artefacts and institutions; a person quoting a slogan is ironic or afraid. **R** (03)
17. People code-switch by listener: officialese to strangers, plain speech to their tie. **R** (03)
18. A city lexicon for offenders ("unsettled", "out of step"). **S** (03, not canon)

**The cell as visitors**
19. The cell sounds off-period consistently, never at random. **R** (03)
20. The cell never explains the plan to each other. **R** (03)
21. One flash of the other timeline per mission at most. **S** (03)

**Lore, templates and scenes**
22. Every person line points at one unstated fact (a tie, a need, the list). **R** (03)
23. Overheard material is addressed to someone in the world, never the player. **R** (03; BioShock)
24. Templates survive any filler: whole lexicon noun phrases, no agreement across a slot, two slots at most, no pronouns, the slot never the point. **C D-18** (03)
25. Names are rare: an address term at an approach's opening beat; given names only between tied people. **C D-4** (03)
26. **A scene's exchange names the thing (a slot) and withholds the feeling; the Scout's read-out and the Learned line say it plainly.** **C D-17** (Hitman; Disco Elysium)
27. **A bark is a signal the HUD also shows; a bark claims only a state the engine has.** **C D-13** (Orkin; Blacklist's accessibility review)
28. **The step down has its own line and names what it dismisses.** **R** (Thief; MGS Caution)
29. **Questions and answers show a state change was noticed; a lone bark states it.** **R** (Orkin; Walsh)
30. **A memory line reacts to what the act caused; it recalls only where history is implied.** **C D-14** (Nemesis; Hades)
31. **Lore through interest, missable and paid on replay**: a Watch scene may carry one line of the block's marked-for-clearance list; nobody explains it. **R** (Dishonored's Serkonan line; Outer Wilds, 03)

Kept as in the generator now: no gendered word (D-5), the tone lists, signal words, no digits, civilians never the payload, ≤ 12 words and ≤ 80 characters. The pronoun question waits on Sky; write lines that need no pronoun and no gendered noun.

## 13. Rollout, gating, evidence

| step | work | needs | evidence |
|---|---|---|---|
| 1 | `bagIndex`, rank ties, tiers in the rules; property and vector tests | nothing paid; Latch ports after Sky's yes | `bun test tools/dialogue`; typecheck; `bun run verify` OK (with a drift declaration) |
| 2 | schema fields (optional), `LineEntry` qualifiers and ids, the expander emitting qualified entries; stand-ins extended | step 1 | 79+ tests green; Handover text unchanged; r017..r019 replay OK |
| 3 | D-13, D-14, D-15, D-16, D-17, D-18, D-19 with failing examples; calibration printout against `text.json` | step 2 | each rule's failing example; `bun run dialogue --standin --handover` exit status |
| 4 | the reachable-memory derivation (`replyCells` extended); `states` derived and drift-tested | Keel confirms the derivation | a test that every derived cell has a rule |
| 5 | `--audit` | step 3 | the audit's report on the stand-ins |
| 6 | **live runs** of `scenes`, `street`, `civic` with the new asks; accepted tables promoted with Sky's yes | **new paid spend; Sky via Aster**; Keel critic, Motive reviews temperament voices | runs on disk, replay OK, promotion record |
| 7 | host: query/commit, trace, collapsed gist card, marker | Latch, Glyph | play test |

The lexicon (`r019`) needs no re-run: nothing in this update changes its blocks. The second-order cost of step 6 is the model budget: the first batch asks for about 10 memory cells, 3 `fact` cells and up to 8 colour cells a group on top of the base 39. Roughly +50% lines on the base, not double, and the caps stop it.

## 14. What I did not take, and why

- **Façade's resume and reestablish beats**: nothing is cut mid-scene that a reshow would repair (§6).
- **Halo's skip fractions and per-character odds** on `result` lines: they carry rule information (§4).
- **Hand-tuned weights** (Valve per-criterion weights, Fallen London frequency tiers): counting fields plus a stated rank and three tiers is enough, as Short suggests (01 §9).
- **Lines writing facts back** (Valve, TLOU): text is host content, memory is the sim's (§7.1).
- **Wildermyth's inline markup** and Tracery nesting: tables stay flat; the model fills a form, the code fills the slots.
- **Dwarf Fortress memory slots**: the rulebook already caps memory with its own eviction rule.
- **A per-watcher suspicion ladder with Thief-style a1/a2/a3 barks**: the engine has no such ladder, and a bark may not invent one. If Sky wants one, it is a design change for Motive, Keel and Pulse, not for the dialogue.
- **Disco Elysium's invisible passive checks** ("you'll never know that's important"): every result line here is shown.
- **A `{last}` history slot** (CiF): phase 2 (§2.6).

## 15. For Sky (and what each answer unlocks)

1. **Go-ahead for the live runs in step 6** (new paid spend; through Aster). Nothing before it is paid.
2. **Pronouns**: still waiting on Sky from item 3 of the earlier follow-ups; the no-gendered-words rule stands and every rule above keeps it.
3. **Watcher state barks for generated guards, patrols and watchers** (§9): yes or not now. Yes adds about 13 cells in `security` and 5 (the crowd states) in each other group, and asks Pulse and Keel for the `return`, `tampered` and `pass-on` signals.
4. **The patrol's closed phrasebook and its D-7 exemption** (a `machine` group), 03 §3.4.
5. **A city lexicon for offenders** ("unsettled", "out of step"), and **the once-a-mission flash of the other timeline** (03, not canon).
6. **A "can be heard" marker on a person with an unknown need** and **a guidance dial** (Hitman's Full / Minimal / Off): the dial would turn the Learned restatement and the marker down for veterans, and is the answer to Hitman's "too much guidance" criticism (02 §1). **[G]**
7. **One picker for the Handover and the generator** (the Handover moves to `bagIndex`): low risk, no replay hash moves; recommended; Aster to confirm with Latch.
8. **The Nemesis patent**, if civilians ever get rank (§7.2).
9. **State typography for the city's voice** in the HUD (Pentiment's lesson, 03): Glyph.

## 16. Handoffs

- **Keel**: confirm the reachable-memory derivation (§7.3); expose the read the host needs per person (latest Fixer-facing act and count, scale steps, standing band, held facts, whether the cell knows the need and secret), read-only, no side effects; the engine codes behind `states` (§9.2); whether `tampered` and `pass-on` exist.
- **Latch**: `bagIndex`, rank ties, tiers and budget, query/commit, the trace, the collapsed gist card, and the line ids in the replay log.
- **Motive**: review the qualifier rank (§3.1), the first batch of memory cells (§7.4) and the temperament voices.
- **Pulse**: which watcher returns, tampering and reporting are real states.
- **Glyph**: the marker, the collapsed card, the type treatment for the city's voice.
- **Suture**: tune the budget and the thresholds by playtest; run the audit on the live tables.
- **Aster**: the paid-spend question, and whether the Handover moves to the new picker.

Evidence for this file: no command changes behaviour; the figures in §10 come from a scratch count of `contracts/missions/handover/text.json` (script in `out/scratch/shapes.py`, not committed) and the cell and person counts in §2.1 from `tools/dialogue/situations.ts` over the promoted cast (`out/scratch/cells.ts`).
