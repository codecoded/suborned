# 010 · Review of Keel's rulebook, the 15 proposed values (Motive, 2026-10-15)

**Inputs**
- `contracts/rules/rulebook.json` (c5dfb2d, plus Keel's live brief 011 edits)
- `briefs/010/keel-rulebook.md`
- `contracts/people/people.rules.json`
- GDD v0.21.0: §02, §04–§06, §08
- my Dwarf Fortress pass, `briefs/010/research/df-for-the-suborned.md`
- brief 011's decisions:
  - Inez is an Idealist and Pay is the lesson;
  - the 15 values go ahead as [S], Sky can strike any.

**Each row answers four tests**
- **Price:** it changes a price or an outcome the player sees.
- **Glance:** a commander reads it in one line.
- **GDD:** it matches §04/§05 and the DF pass.
- **Purse:** the Handover lesson still holds.

## Verdicts

| # | Rule | Verdict | Value | Why |
|---|---|---|---|---|
| 1 | Q4 steps | **Accept [S]** | worse Pay +$20,000 · worse Pressure +1 T · worse Help +1 order | §04 says "Keel sets the numbers". Every temperament keeps a real choice. A Mercenary pays $70,000 by Pay, or $80,000 by Help ×2 at 100 s, which leaves them trusting. A refused order still costs its time and Trail and keeps the money: one rule the card can say. |
| 2 | Loyal turns on the 2nd Help | **Accept [S]** | +1 order | This is the smallest "repeated" and reads as "trust takes two Helps". Two Helps leave loyalty 2, which is §04's "Hero turning: loyal". Spreading the Helps across missions is a later T-test candidate, not now. |
| 3 | Mercenary prices | **Accept [S]** | Hero +$20,000 · Menace −$20,000, Pay only | The price lines read in one step size. In a Hero district Help ×2 ($80,000) beats an overcharged Pay ($90,000); in a Menace district Pay ($50,000) wins. That is exactly the §04 standing row: Hero is "slower, dearer, loyal", Menace is "faster, cheaper, betrayal risk". |
| 4 | Mercenary sells you | **Accept [S]**, plus one engine note | Menace district, loyalty none, resentment below max | The cause is visible: Menace band, the "none" loyalty word, a Mercenary. One Help fixes it. **Note for Keel:** sellers and Informers fire every break, so ambushes must not stack. One ambush per next mission, and the debrief names every contact who sold or informed (§06 fights are per mission; T6). |
| 5 | Standing scale | **Accept [S]** | start 0 · ±1 per witnessed incident · Hero ≥ +3 · Menace ≤ −3 | A meter with two marks reads well. The Handover district starts neutral, which the purse needs: a Menace start would refuse Inez outright. How fast it moves depends on the incident definition, under Other findings 4. |
| 6 | Reporting | **Change** the Idealist half; accept the Loyal half | Idealist in a Menace district reports while **loyalty ≤ none** (Keel had bravery > fear) | An unturned Idealist's fear never moves: a refused threat moves no scale, and the violence move is for contacts only. So Keel's "until a threat makes them keep quiet" can't happen. Worse, a turned Idealist Fighter (bravery 3) would report every Help you give them. "Owes you nothing" is the same line as #4, so the player learns one threshold. The Loyal half is accepted: they report the bribes and threats they don't respond to, and in a Menace district everything. |
| 7 | Loyalty gains | **Accept [S]**, plus a guard (applied) | Help +1 per order · volunteer +1 · a fold = Pressure's moves | Gratitude becomes a number the Talker test reads. **Guard:** Help needs the need still unmet (`need-met: false`). Without it, Help on a turned contact repeats within one mission and buys loyalty 3 for cash, and "clear a debt twice" is not readable. Now it is one Help per need, so one per mission per contact (Q2 replaces the need between missions). |
| 8 | Witnessing violence | **Accept [S]** | bravery −1, fear +1, resentment +1 | A helped Inez who sees violence becomes a Talker (1+1 > 0+1); a second time makes her an Informer. That is §06's "some Informer, more Talkers". It applies once per incident, as Trail does. |
| 9 | Memory | **Accept 8, change what is dropped first** | when full, drop the oldest `saw-act` or `told` entry; only when none is left, the oldest entry | With "oldest first", a busy witness forgets a refusal or a Help. The +$10,000 refusal line then vanishes from the card, and the brave lie stops, with no cause the player can see. The new rule: people forget what they saw before what you did to them. It is still the §05 "sightings go stale", and readable. This needs `rulebook.ref.ts` `apply()` and a test, so Keel makes it. |
| 10 | Word along ties | **Change** | `word-quiet-loyalty` **2 → 1** (obliged) | At 2, a once-helped Inez lies to investigators for you (the people rules' lie rule) yet tells her family the Fixer's face: a contradiction. At 1, one Help (which needs a Listen) quiets her, so the pillar pays in the first debrief. Family and workmate ties, one hop, between missions: accept (my K3). |
| 11 | Introductions | **Accept [S]** | known (routine, need, temperament: 94 s saved), not turned · 1 per contact per break | Turning for free compounds: each contact makes one introduction per break, so the network would double every mission and cash would stop gating it. The GDD's "introduces a friend (new contact)" needs rewording at the next version (Other findings 8). |
| 12 | Exposure | **Change** | exposed = an investigator questioned the contact this mission, **or** a watcher logged a cell operative within approach reach of the contact. Being seen doing the job is not exposure. | Keel's "a non-cell person saw it" breaks route A: the guard sees Inez spill the coffee by design, so every route A exposes her and the bravery ladder never climbs at street level. A job is an ordinary act and not a traceable one (§02), so its witnesses hold nothing against her. Both ways that remain are on screen (watcher cones and loops, the questioning icon), so the player can play against them. |
| 13 | Public Help | **Accept [S]** | a witnessed Help on a debt is `debt-cleared` (Hero, +1 T); a witnessed Help on cash is a payment (+1 T) | This is §04 "clear debts publicly" and §02 "visible payments". In the Handover it adds up to +1 T for Inez and +1 for the driver. Route A is already at T 3 (Help, loop, call), so the investigation band doesn't change. The card must show it as a risk (Other findings 2). |
| 14 | False lead | **Accept [S]** | the liar's own routine place farthest from the true place, at the fact's time | It's deterministic and it's a lie the liar could tell. One lie type, Adams' lesson. The player sees the wasted hop on the dotted lead line. |
| 15 | Ties lapsing | **Accept [S]** | workmate lapses like regular; a debtor tie lapses when the debt is met | Each lapse has a visible cause in the debrief. A met debt clears every debtor tie from that person; the need is all they owe. Pressure doesn't meet the need, so it keeps the creditor's leverage. |

**Flagged for Sky from the 15: none.** Each is a tuning value or the least reading of the GDD. The one real fork found sits outside the list, below.

## The Handover purse (run through `rulebook.ref.ts`, scratch `out/motive/purse-walk.ts`)
- **Informed route:** Watch, Listen, then Help Inez ($40,000 · 50 s · +1 T, turned) and Help the driver ($40,000). Total **$80,000, which fits $100,000.** ✓
- **Pressure on Inez:** 35 s, **+4 T**, and she becomes a Talker.
- **Pay on Inez:** refused. It is charged 50 s and +2 T and leaves a refusal. A later Help then costs $50,000 and +2 T; with the driver that is $90,000, which still fits.
- **The old inequality survives as the cash check.** With the driver turned first, $60,000 is left, and a Pay whose temperament line shows "?" needs $90,000 worst case, so the order can't be given.
- **But see the fork:** under today's rulebook Pay can never be blind.

## Fork for Sky: what reveals the temperament?
The rulebook follows GDD §04 ("until Watch or Listen reveals the temperament"), so `scout.watch` learns routine and temperament. Three things follow:
- Pay requires Watch, so Pay is never blind.
- After Watch, Inez's Pay card already reads **"refused"**.
- Listen requires Watch first, so Listen's temperament reveal is always redundant.

As a result, brief 011's "paying her blind fails and is remembered" can't happen. Keel's own test "Pay blind asks cash for the worst case" uses a state (routine known, temperament not) that no event produces.

| Option | Watch gives | Listen gives | The lesson |
|---|---|---|---|
| **A (recommended)** | routine | need + temperament | Pay after Watch shows "?" and asks $90,000 worst case. Inez refuses and remembers, and Listen turns her by Help. The "?" earns its place in the training mission, and "eyes give where and when, ears give who they are" reads at a glance. |
| B (GDD as written) | routine + temperament | need + temperament | The card says "refused" before you pay, so the lesson is read, not felt, and the "?" barely appears in Act I. |

Choosing A changes the §04 preview line ("until Listen reveals the temperament"), `scout.watch` (learn routine only), `preview.knownBy` (`scout.listen` only) and one Keel test.

## Other findings (not in the 15)
1. **Brief 011 says Pressure on Inez costs +3 T.** Under Sky's Q4 it costs +4 T: Pressure doesn't suit an Idealist, so it is one step worse. Fix the brief's wording; this is not a fork.
2. **The card must show the risks (§02: "$, time, T, A risk").** `report` (Alert) and witness Trail are not price lines today. The card needs two risk lines:
   - "reports this (+A)", with a "?" until the temperament is known;
   - "seen by N within 13 m: +1 T".

   Keel decides the fields, Glyph lays them out (brief 011 C).
3. **The Ask card in a neutral district shows "?"** although every temperament refuses Ask there. The "?" should appear only where the answer depends on the temperament in the current band. Keel: `readsTemperament` should test the fit table, not the condition kind.
4. **Incident definition.** Under "one act at one place and time", each witnessed shot is an incident. One loud Handover (3 shots seen) then makes the district Menace, and a contact watching a shoot-out takes the violence move per shot. My recommendation: a shoot-out at one place counts as one incident for standing, witness Trail and the violence move. The [B] per-shot +2 T and +1 A stay. Suture should watch the pace in T2.
5. **Need replacement must clear the need-met flag**, or the guard in #7 blocks Help for good.
6. **The Heat threshold is mine and still unset.** I'll set it with the §06 fight-scheduling event, not here: a value no rule reads fails the schema.
7. **`people.rules.json`:** now that v0.21.0 adopts P1–P5 (Q7), their `proposed` flags can be cleared. That's a later pass, once Splice is out of `tools/people/`.
8. **GDD text owed at the next version** (Sky to accept):
   - §04 Inez Pay row → refused (brief 011);
   - §04 "introduces a friend (new contact)" → "known: routine, need and temperament; turning still at their price";
   - the preview line, if Sky picks Option A.

## What changed, and what didn't land
- `contracts/people/people.rules.json`: `gdd` is now `the-suborned-agent-gdd-v0.21.0-2026-10-15.md`.
- `contracts/rules/rulebook.json`: `turn.help` needs `{ "k": "need-met", "is": false }` (#7). It is present after Keel's last write.
- **Not in the file:** #6, #10 and #12.
  - I applied them at about 02:40. Keel's whole-file write at 02:41 (adding the `cell`, `perception` and `security` sections) replaced them with the earlier values.
  - I didn't re-apply them, so I wouldn't race Keel on a file being written live.
  - My test edit for #6 is reverted, so `rulebook.test.ts` is net unchanged by me.
- #9 and the #4 note need code in `rulebook.ref.ts` or the engine, so they are Keel's to make.

**Patch for Keel (via Aster):**
1. `values.word-quiet-loyalty.v`: 2 → 1.
   - reason: "a person who owes the cell (obliged or more) keeps what they saw to themselves; one Help, which needs a Listen, is enough; at 2 a helped Inez would lie to investigators and still tell her family the Fixer's face. Obliged is also the line for reporting and selling."
   - `word.passes` reason: "trusting" → "obliged".
2. `reports[idealist-in-menace].when[2]` → `{ "k": "scale", "scale": "loyalty", "op": "<=", "than": "p:scales.loyalty.min" }`.
   - reason as in #6.
   - test line 168: the fear-1 case → `true`. Add a case: turned, bravery 3, loyalty 1, Help in a Menace district → `false`.
3. `exposure.rule` → "exposed when the city tied the contact to the cell this mission: an investigator questioned the contact, or a watcher logged a cell operative within approach reach (values.approach-reach-m) of the contact. Being seen doing the job is not exposure: a job is an ordinary act and no traceable act (§02)."
   - reason as in #12.
4. `memory.whenFull` → "drop the oldest saw-act or told entry (lowest time, then first written); only when none is held, the oldest entry". Change `rulebook.ref.ts` `apply()` to match, and add a test: a refusal survives 8 sightings.
5. `contact.informs` and `mercenary.sells`: add a `how` line: "one ambush per next mission however many contacts sell or inform; each one is a debrief line".
6. `need.replaced`: the replacement clears the need-met flag (Other findings 5).

## Evidence
- `bun test contracts`: 42 pass, 0 fail, against the file as it stands (Keel's values plus the #7 guard).
- `bun test tools/people`: 66 pass, 0 fail.
- `bun run typecheck`: exits 0.
- `bun out/motive/purse-walk.ts`: the figures in the purse section.

## Next owners
- **Aster:** relay the patch to Keel; send the fork and the GDD text in 8 to Sky; correct brief 011's +3 T.
- **Keel:** patch items 1–6, plus Other findings 3 and 4.
- **Glyph:** the two risk lines on the action card (Other findings 2).
- **Suture:** in T2, watch how fast standing moves once Keel settles the incident definition.
- **Motive (me):** the Heat threshold with the §06 fight event.
