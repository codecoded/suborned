# Dwarf Fortress for The Suborned: what to take (Motive, 2026-10-15)

**Inputs**
- the dossier `dwarf-fortress.md`
- my follow-ups `followups-2026-10-15.md`: Adams 2017 read in full, the Legion Census sources, and the DF Justice page re-checked
- my rules: `contracts/people/people.rules.json` and `briefs/010/people-rules.md`
- the GDD: §01 pillars and tiebreakers, §02, §04, §05, §08 and §09

**The four tests every row answers**
- **Price:** it changes a price or an outcome the player sees.
- **Build:** the checker can verify it before play.
- **Det:** the same entity gives the same result on every run.
- **Glance:** a commander reads it in one line.

**Verdicts:** TAKE = as DF does it · ADAPT = the idea, in our terms · SKIP = not for us, with the reason.

## Mechanics

| # | Mechanic (source) | Verdict | Our version | Price | Build | Det | Glance |
|---|---|---|---|---|---|---|---|
| 1 | **Bands, not sliders.** DF has 50 facets on a 0–100 scale, but 78% of people sit in the neutral band and get no text. | **TAKE** (already) | Each person has one temperament out of 4 (RimWorld's "few, loud" rather than DF's 50) and four small integer scales. Every value has a named step: fear and loyalty get names in edit P1. | ✔ Talker test, Accomplice/Fighter | ✔ | ✔ | ✔ |
| 2 | **Culture baseline plus deviation.** Values come from the civilisation, and the character sheet phrases them as departures from it. | **ADAPT** | The baseline is the cast row: an area × occupation's temperament weights. There are no per-person values: 33 values is rich, not readable. Edit P3 caps any one temperament at 60% of a row and requires two kinds of person at any unit of 3+ staff. The UI phrases deviation ("unusual for the docks"); that goes to Glyph and Fable. | ✔ a second way in at a second price | ✔ row and unit | ✔ | ✔ if phrased against the baseline |
| 3 | **Needs as decaying counters.** A need resets to +400 when met and falls with no floor. Weights are 1/2/5/10. Focus scales skill ±50%. | **SKIP** the counter · keep one clear need | Our need is a **lever**, priced by Help and Pressure (§04), not a mood. A counter fails three tests: DF's own stress is "invisible to the player until it starts to affect the dwarf", it needs per-tick state, and 30 needs is not readable. We keep DF's root (need from something checkable: `requires` home, work, family tie or rival tie). What replaces a met need is Sky Q2. | ✔ | ✔ | ✔ | ✔ |
| 4 | **Event → memory → core change.** 8 short-term slots, 8 long-term, a 1-in-3 chance per recall of becoming core, then a permanent facet change. | **ADAPT** | Memory is a log of acts done to this person: act, face, place, time, and whether witnessed. The ladders move **only** on listed events (+1 bravery per job done unexposed, +1 resentment per pressure or exposure, §04). There is no roll, no slots and no stress. DF's magenta "this changed them" line becomes the debrief cause line (§08 [B]). This is a runtime rule (Keel list, K1–K2). | ✔ Talker flips | ✔ empty at start | ✔ no roll | ✔ via the debrief |
| 5 | **Tie formation and decay.** Ties form through idle adjacency and compatibility. Friends fade. Grudges never fade. Corruption spreads along ties. | **ADAPT** | Ties form **at build time from place**: the `between` kinds (same employer, a routine stop, same trade). That is DF's proximity rule frozen into the routine. Introductions travel only along family, workmate and regular ties (edit P4). Decay is runtime: a regular tie lapses when the routine stops coming, and rival and family ties never decay (K4). SKIP compatibility scores and romance: richness no price reads. | ✔ who you reach next; the kin and protection needs | ✔ | ✔ | ✔ the tie list |
| 6a | **Knowledge only through witnesses.** A rumour starts with witnesses and nobody is omniscient (Adams 2017). | **TAKE** | Facts come from witnessing (§05: a clear sightline within 13 m) or from being told along a tie. At the start nobody knows the cell. The only thing a person starts knowing about others is the needs a keeper knows (§08 tips). Edit P5. | ✔ a turned keeper saves a 42 s Listen per person they know | ✔ derived from ties and positions | ✔ | ✔ "knows the needs of…" |
| 6b | **Rumour spread.** A rumour travels when a knower leaves the area, reaching other sites in days. Only identity lies are false. | **ADAPT** | Word passes along family and workmate ties **between missions only**, one hop, and is listed in the debrief (K3, Sky Q6). There are no false rumours except the lie rule's false lead: Adams found 85 edge cases from one lie type. | ✔ | ✔ counted per hop | ✔ | ✔ only if the debrief lists it |
| 7 | **Three identity states: alias, true name, face.** Stored per observer; they merge when a witness links a face to a name. | **ADAPT**: face only | The cell is known by **face** (§03: the Fixer is "the face witnesses remember"). The Talker's facts are a face, a car and a place-time. §05 investigators hop by place and time, not by name. True name: SKIP, since the cell are visitors from a nearby timeline and the city has nothing on file. Alias: [L], Sky Q1. Adams' face "just uses the ID number", so disguise is also [L]. | ✔ "Inez may identify Fixer" (§08) | ✔ | ✔ | ✔ |
| 8 | **Recruiters choose an approach and misjudge.** The villain picks bribe, fear, flattery, blackmail, ideology or revenge, and poor skill misreads the target. | **ADAPT** | The player is the recruiter. Misjudging means acting **before** Watch or Listen: Ask cold or Pay blind pays the knowledge tax (Inez: $70,000 vs $40,000). CK3 shows the fix for readability: price as additive line items ("Mercenary, Hero district: overcharges +$X") (K8). Whether the preview shows the *outcome* before the temperament is known is Sky Q3. Rival crews recruiting: [L]. | ✔ | ✔ the purse case checks it | ✔ | ✔ line items |
| 9a | **Interrogation, one step up the chain.** A correct arrest gives "a hint one step farther up". | **TAKE** (§02, §05) | One answer is one hop (60–90 s), place plus time. DF's fear *silences* witnesses about the villain ("too scared to identify Earthhells"). Ours makes them *talk* (Talker, §04). We keep the GDD's rule: one fear scale, one direction, readable > rich. | ✔ | ✔ | ✔ | ✔ the dotted lead line (§05) |
| 9b | **The free-questioning hole.** "Interrogating an innocent dwarf does not have any negative effects", so players question everyone. | **Already plugged** | On the player's side there is no free probe: Ask cold costs +1 T and leaves a refusal (+$10,000 later). On the city's side, a wasted hop costs the investigator 60–90 s, and that is the player's counterplay window (false tip, keep moving). One rule for Keel (K6): questioning someone with no fact yields nothing and still spends the hop. | ✔ | ✔ | ✔ | ✔ |
| 10 | **Detail only for named people.** Histfigs vs. abstract population. Legion "uprezzes" a person and their friends when touched. | **TAKE**, at build time | Two tiers already: simulated people carry state, background people carry a routine only. The tier is chosen at **build** time and never promoted at runtime: DF's offload and Legion's uprez both break replay. A mission pins a simulated person, never promotes one. Whether background people can be questioned as witnesses is Sky Q5. | ✔ | ✔ | ✔ | ✔ |
| 11 | **Per-entity streams vs one shared stream.** DF: "the historical events during world generation don't currently always happen the same way", and a parameter change reshuffles everything downstream. | **TAKE** (already) | The expander keys a stream per entity and stage (`tools/people/expand.ts`: `rng(\`${a.id}/need\`)`). A new stage takes a new key and never draws from an existing stream. P-9 checks byte-identical output twice. | n/a | ✔ | ✔ | n/a |
| 12 | **Watch Dogs: Legion's "bios that did nothing".** Recruits are picked by perk; traits "don't land". | **TAKE the lesson as a rule** | Edit P2: every field a simulated person carries names the rule that reads it, and the schema rejects a field with no reader. There are no bios: text is a light layer generated from state (§09). Legion's "a reason for each opinion" becomes our debrief cause line. | ✔ by construction | ✔ | ✔ | ✔ |

**Also skipped**

| Mechanic | Why we skip it |
|---|---|
| Stress, mood and breakdowns | Our "breakdown" is the Informer step on the resentment ladder: deterministic, one cause, and it schedules an ambush (§04). |
| Dreams | The advancement need covers "attain rank" and has a price. |
| Grudge false reports (witnesses framing rivals) | That would be a second lie type. Adams found one lie type cost 85 edge cases. |
| Spies blowing cover through home ethics | No cover yet: alias is [L]. |

## 1. Proposed rule changes (made in `people.rules.json` and its schema, each `proposed: true` with a reason)

| id | Path | Value | Reason (our terms) | Enforced by |
|---|---|---|---|---|
| P1 | `scales.fear.steps`, `scales.loyalty.steps` | calm · uneasy · afraid · terrified / none · obliged · trusting · sworn | DF's bands: every number on a person reads as a word, as bravery and resentment already do. | schema (steps = max + 1) |
| P2 | `person.readBy.fields` | a reader for each carried field: name, temperament, need, ties, scales, memory, routine | Legion and DF ("effects still unknown"): a fact no rule reads is trivia. | schema: a carried field with no reader is rejected (tested) |
| P3 | `temperamentsBy.mix` | rowMaxShare 0.6 · unitMinDistinct 2 · whenStaffAtLeast 3 [S] | Culture baseline = the cast row; nobody *is* the baseline. Two ways in at every workplace (pillar: orchestrate). | schema (satisfiable); **P-rule needed, Splice** |
| P4 | `ties.kinds.*.introduces` | family, workmate, regular: true · debtor, rival: false | DF: corruption spreads along ties; Legion: uprez pulls in friends. §04 and §08 introductions come off a tie the player can already see. | schema (≥ 1 introduces) |
| P5 | `knowledge` | identity [face] · learnedBy [witnessed, told] · cellAtStart nothing · needsKnown: a keeper knows the needs of their workmates and of their unit's regulars | DF's witness rule and Adams' per-observer identity, cut to what §04 and §05 read. Makes §08 "café owner tips needs" checkable from the cast. | schema (positions resolve); **expander or view to derive it, Splice** |

All five edits are scoped to the generated cast: temperament mix, ties, starting state, and what a person knows. Nothing at runtime.

**Runtime rules for Keel's rulebook and the GDD** (not in the file)

| id | Rule | Serves |
|---|---|---|
| K1 | A memory entry is {act, by face, place, time, witnessed}, written only by listed acts. Capped per person [S]. | §04 memory, §08 debrief |
| K2 | The ladders move only on a closed event list (job done unexposed → +1 bravery; pressure or exposure → +1 resentment; threat → +1 fear). No roll. | §04 [S] |
| K3 | Word passes along family and workmate ties between missions, one hop, and is listed in the debrief. | §01 information moves through people |
| K4 | Tie decay: a regular tie lapses when the routine stops visiting; family and rival ties never decay. | §04 |
| K5 | A turned contact introduces only along `introduces` ties; one introduction per contact between missions [S]. | §04, §08 |
| K6 | Questioning someone who holds no fact yields nothing and still spends the 60–90 s hop. | §05 |
| K7 | The debrief lists every give-up with its cause ("X gave up the Fixer's face because fear 2 + resentment 1 > bravery 1"). | §08 [B], T3 |
| K8 | The order preview shows price as additive line items by temperament × standing (CK3). | §02 full price |
| K9 | A turned keeper hands over the needs in `knowledge.needsKnown` at mission start. | §08 economy |

## 2. GDD questions for Sky

| Q | Question | My recommendation |
|---|---|---|
| Q1 | Should the Fixer work under an alias, so rumours about "the harbour man" link to a face only through a witness? | **Not now ([L]).** Face only. An alias adds an identity state but no price until disguise exists. |
| Q2 | A need is met (Inez's debt is cleared). What does she need next? | **Between missions, draw a new need from her own stream (id + mission number).** A turned contact tells you it free in the debrief; anyone else needs Listen again. |
| Q3 | Before the temperament is known, does the preview show whether an approach will work? | **Price always; outcome only once the temperament is known (shown as "?" otherwise).** This is DF's misjudging made readable, and it pays for Watch and Listen (pillar: subvert people). |
| Q4 | What does an approach a temperament doesn't respond to do (Help on a Mercenary, Pay on a Fearful)? | **It works at one step worse (one more Help, or a higher price, set by Keel). A refused approach fails and leaves a remembered refusal.** This needs pinning down before the Handover can say which Inez is castable. |
| Q5 | Background people witness acts. Can investigators question them? | **No.** They add T, Alert and standing (§05 crowds) but carry no state, so investigators question simulated people only. Crowds hide you but still leave a Trail. Test with T4. |
| Q6 | Should memory propagate along ties (Legion), e.g. helping Inez warms her family? | **Only as word (K3): facts, not opinion.** Standing stays "witnessed acts only" (§04). |
| Q7 | Fold P1–P5 into §04 and §09 at the next GDD version? | **Yes**, as [S]: named fear and loyalty steps, the mix cap, the readBy rule, introductions along ties, and the knowledge start state. |

## 3. What the first civic-block run should carry (for Pulse's cast tables)

| Item | Why | Stand-in today (`bun run people`, 126 people) |
|---|---|---|
| Rows: café (diner: keeper, server, cook), Records Office (clerk, guard, official-driver, councillor), residents above, visitors | brief 010 C | present |
| Café server row: weight **debt** and a temperament that one Help turns (Idealist; others per Q4) | §07: Inez is in debt and is helped for $40,000. The purse (help + driver = $80,000) only fits if Help works once. | ✗ Inez came out Mercenary with an advancement need; M-2 pins her by name and position only (**Waymark**) |
| Driver row: weight **cash**, Help-once temperament | §07, turned for $40,000 | check |
| Guard row: Loyal or Mercenary, `preShift: cafe`, and a regular-tie share into the café | §05 "café stop pre-shift"; route A's call to the guard rests on it | ✗ 0 of 6 guards hold a regular tie into the café |
| Café keeper knows the needs of their workmates and regulars | P5, §08 tips | 9 needs known (a strong payoff: watch it in T2) |
| Mix: no temperament over 60% of a row; units of 3+ staff carry 2 or more | P3 | 4 of 4 units pass |
| Review page `010-cast.html` shows each scale as its step word, the "can introduce" ties, and "knows the needs of…" | P1, P4, P5, glance test | to add (Pulse, Splice) |
| No bios, values or dreams; the `why` text stays ≤ 300 characters and is about the place | P2, §09 | as schema |

**Evidence**
- `bun test contracts/people tools/people tools/rag tools/missions`: 74 pass, 0 fail.
- `bun run typecheck` exits 0.
- `bun run people --out out/motive/people.json`: P-0..P-9 ok, 126 people.

**Handoffs**
- **Splice:** P-rules for P3 (row and unit) and P5 (derive `needsKnown`). Chunk `knowledge`, `mix`, `introduces` and `readBy` in `tools/rag/people-corpus.ts` so Pulse retrieves them.
- **Waymark:** M-2 should require the server's need (debt) and the driver's need (cash).
- **Keel:** K1–K9.
- **Aster → Sky:** Q1–Q7.
