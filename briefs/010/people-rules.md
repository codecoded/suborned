# 010 A · People rules (Motive, 2026-10-15)

File: `contracts/people/people.rules.json` (`suborned.people.rules/1`). Schema: `people.rules.schema.ts`. Test: `people.rules.test.ts`. Anything not in the file is rejected. Each entry cites its GDD § in `src`. Where the GDD says nothing, the entry has `proposed: true` and a `reason`. Those entries are listed first because they are new design for Sky to accept or strike.

## Proposed (new design, Sky decides)
| rule | value | why | serves |
|---|---|---|---|
| fear scale | 0–3 integers | The Talker test names fear but the GDD gives it no scale. This range weighs it against bravery's four steps. | §04 talker |
| loyalty = to the cell | 0–3, everyone starts at 0 | If loyalty meant loyalty to an institution, the Office's loyal guard would cover for the cell when questioned. Institution loyalty belongs to the Loyal temperament instead. | §04 talker, temperament |
| Idealist starts at bravery 1 (Helper) | | §04 says an Idealist "volunteers free; bravery fast". | §04 |
| Fearful starts at fear 1 | | A Fearful person talks when questioned (1 > 0) until Help or a job makes them braver. | §04, §05 |
| institutions | employer · civic | A Loyal person is loyal to their work unit, or to the civic order if they have no work. Both can be checked from home and work data. | §04, §01 world |
| needs beyond debt/cash | housing (needs a home), papers, kin (needs a family tie), care (Help only), protection (needs a rival tie), advancement (needs work), secret | One closed list of 9. Every need is met by Help or Pressure, so knowing it changes the price. Where a need requires something, the checker can verify it in the data. | §04 pillar "subvert people", §07, §08 |
| tie limits | family ≤4 · workmate ≤3, same employer · regular ≤3 out / 12 in, customer's routine stops at the unit · debtor ≤2 owed / 6 in · rival ≤1, same trade or employer · 1–8 per person, no self, one per kind per pair | Each tie has a place the checker can verify. Introductions (§04) need someone to make them, so nobody has zero ties. Nobody is a hub. | §04, §08 |
| liar rule | not a Talker, helped, bravery ≥ 1 → false lead under Hero standing, otherwise nothing | §04 "brave helped contact lies". Standing decides which answer, so the result stays deterministic. | §04, §05 |
| positions | one keeper per unit; per category: server, cook, door, assistant, hand, … | The Handover casts a café "waitress" (server). §08 has a café owner (keeper). | brief 010 Fixed, §07, §08 |
| restricted temperaments | guard, patrol-officer, investigator: Loyal or Mercenary · councillor, watcher: not Idealist · pawnbroker, credit-bureau: not Idealist | A guard who could be helped across would undo §05's pressure. A creditor who is an Idealist forgives the debt. | §05, §07 |
| roles | resident, visitor, docker, clerk, guard, official-driver, councillor, watcher, patrol-officer, investigator | Simulated people who are not trade staff still need a temperament rule. | §05, §07, life.json routines |
| café = diner | | The 22 m corner café matches none of the 39 trades by name. The plan's corner rule assigns an anchor trade or a diner, and diner is open 6–24. | plan frontage.placement.corner |
| names | one pool per district · given + family, pattern `^\p{Lu}[\p{L}'-]{1,15}$` (no digits, suffixes or titles) · ≥ 60 given, ≥ 80 family · ≥ 6 origins from a closed list of 21, none over 25 % · full names unique · background people unnamed · avoid list of 1950s-Americana names | Makes §15's "no era date, mixed cast" checkable. | §15 people |
| pinned names | Inez (given, café server), Vale (family, councillor) | The GDD names them. The person cast in that role takes the name part. No other name is authored. | §07 |
| fear / loyalty steps (P1) | calm·uneasy·afraid·terrified / none·obliged·trusting·sworn | every number reads as a word (DF bands) | §04 |
| readBy (P2) | each carried field names the rule that reads it; a field with no reader is rejected | Legion's bios did nothing | §09 risk |
| mix (P3) | no temperament > 60 % of a cast row; units of ≥ 3 staff carry ≥ 2 temperaments [S] | culture baseline without sameness; two ways in per workplace | §09, §04 |
| introduces (P4) | family, workmate, regular introduce; debtor, rival do not | introductions come off a visible tie | §04, §08 |
| knowledge (P5) | face only; witnessed or told; cell unknown at start; a keeper knows workmates' and unit regulars' needs | DF witness rule; §08 tips checkable | §03, §05, §08 |
| background tier | carries only a routine | Background people walk, run and cheer only, so they have no state to approach or question. | life.json tiers, §06 |

## Grounded (GDD states it)
| rule | value | serves |
|---|---|---|
| person | routine + need + temperament + memory; exactly 1 need; memory starts empty (history, not backstory) | §04, §09 |
| temperaments | Idealist (Help, refuses Pay) · Mercenary (Pay) · Fearful (Pressure) · Loyal (Help over time, reports any approach); Hero/Menace lines verbatim | §04 |
| starting bravery / resentment | 0 = Informant / 0 = Compliant for all (Idealist: see proposed) | §04 ladders |
| scales | bravery 0–3 (Informant…Fighter) · resentment 0–2 (Compliant, Resentful, Informer) | §04 |
| approaches | ask 0 / 25 s / +1 T · pay 70,000 / 50 s / +2 · help 40,000 / 50 s / +1 · pressure 0 / 35 s / +3 (Inez reference prices) | §04 [B] |
| debt, cash needs | debt: help, pressure, pay · cash: help, pay | §04, §07 |
| Talker | fear + resentment > bravery + loyalty when questioned, strictly greater, deterministic; gives fixer-face, cell-car, place-time | §04 |
| trades | all 39 plan trades = the 39 life.json occupations, each with its category; all four temperaments unless restricted above | §09, §04 (Hero and Menace turn different halves of the city) |
| ≥ 2 temperaments per trade and role | | §09 risk: sameness |

## Checks the schema runs
- Shape is strict, so an unknown key is rejected.
- A proposed rule must have a reason.
- Starting values sit on their scales.
- Every need is met by an approach that requires the need to be known.
- Each category's positions start with exactly one keeper.
- Every role's employer and every pinned role, position and employer resolves.
- The name pattern compiles and the origin limits can be met together.

The test adds coverage against the plan and life.json, the Talker cases, and the Handover roles.

Evidence: `bun test contracts/people` passes 7 of 7. `bun run typecheck` exits 0. The two new files pass `tsc` on their own, since tsconfig only includes `tools/**`.
