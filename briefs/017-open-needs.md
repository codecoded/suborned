# 017 · Open needs

**Status (2026-10-18):** proposal for Sky. Author Motive (systems). Asked by Aster after Sky's note on the Intel panel: "Needs can grow though, that's too small a list of our world, should be dynamic and not locked into a predefined". No code or data has been edited. If Sky accepts, the §04 needs line changes in GDD v0.24.

## Summary

- **Split each need into a kind and a record.**
  - **Kinds** are a short list of mechanics. They decide which approaches act on the need, what it requires, and how Help pays. There are 8 of them: today's list minus `secret`, which already moved to secrets in brief 011.
  - **Records** are the open part: "Pawn ticket", "Clearance order", "Brother held", "Nurse robot". There are as many as the world needs.
  - The model writes a district's records at build time, alongside its cast tables. Code gives each person one record and binds it to real people and places. The checker rejects anything it can't bind.
- **A record changes play only through two fields that rules read:**
  - `size`, which sets Help's cash price;
  - `holder`, the person or office that could meet the need without your money. If the cell has turned the holder, Help becomes a favour from them: no cash, a little more Trail.

  That is how learning the specific need opens a cheaper way in, and it uses the arc (one person → networks). Every other word in a record is text.
- **The label lives in the record.** Intel shows the kind's word while the need is only told ("Debt"). After Listen it shows the record's label ("Pawn ticket"). No new screens: Alt draws a line to the holder, and the Help chip says "via Rosa".
- **Secrets get the same shape.** `affair` stays derived from the lover tie. `record` secrets become catalogue records with their own labels.
- **The Handover's mechanics don't change.** Inez's record is of kind `debt` and the driver's of kind `cash`. Both are size standard ($40,000 Help) with the holder beyond the sheet, so no new route opens and the purse sum holds: $40,000 + $40,000 = $80,000, within the $100,000 purse.

## Where I push back on the suggested shape

1. **The model writes a catalogue, not a need per person.** Writing a need for each person means 10.6k model writes per district, and none of them would be checkable as a set. Instead:
   - the model writes about 20 to 40 records per area;
   - cast rows weight them the way they weight needs today;
   - the expander draws one per person from the person's own stream and binds its holder and subject.

   This is §09's split as it stands: the model writes tables, code expands them, the checker validates.
2. **Specifics only where a rule reads them.** "Size of a debt, who is owed" is right. "A cousin coming from the outer districts" is a label, not a specific. §09's risk rule says a field nothing reads is trivia (Legion's bios), so a record carries:
   - `size`, read by Help's price;
   - `holder`, read by the favour route;
   - `subject`, read by the tie and arrest events.

   Everything else is the label.
3. **Kinds stay a closed list that can be grown by data.** A new kind is a rules change: Motive proposes it and Sky accepts it, as any rule. The model can never invent one (unknown = reject). Once the engine has the vocabulary below, adding a kind needs no code.
4. **One need per person stays.** It is readable: one slot, one glyph. A second need would double what Listen teaches and halve its value.
5. **The world sets what people lack.** The city is abundant (§01), so most needs are not about money. They are about paper, permission, status, place and people: a stamp, a pass, a clearance order, a post, a relative held. The model's prompt leads with this.
6. **A finding from reading the rules.** Today each need's `help: cash | favour` is read by nothing: the engine loads only `metBy` and `requires`. The favour route gives that field its reader.

## Structure

**people.rules.json `needs`** (Motive): `list` becomes `kinds`. The tools read an absent `kinds` as the old `list`, so pinned runs replay.

```json
"kinds": {
  "debt":  { "metBy": ["help","pressure","pay"], "help": "cash",   "requires": "nothing",    "holders": ["trade:pawnbroker","trade:credit-bureau","tie:debtor"] },
  "cash":  { "metBy": ["help","pay"],            "help": "cash",   "requires": "nothing",    "holders": ["trade:travel-bureau"] },
  "papers":{ "metBy": ["help","pressure"],       "help": "favour", "requires": "nothing",    "holders": ["role:clerk","trade:employment-bureau","trade:hiring-hall"] },
  "kin":   { "metBy": ["help","pressure"],       "help": "favour", "requires": "family-tie", "subject": "tie:family", "holders": ["role:patrol-officer","role:clerk"] }
},
"sizes": { "small": "v:help-small-usd", "standard": 0, "large": "v:help-large-usd" }
```

The other kinds are `housing`, `care`, `protection` and `advancement`, in the same shape. A `holders` entry is a binding kind:
- `trade:` = the keeper of a unit of that trade in the district;
- `role:` = a person in that role at a civic employer;
- `tie:` = the person at the other end of that tie.

`beyond` means nobody on the sheet holds it.

**Catalogue** `contracts/people/<district>.needs.json` (`suborned.needs/1`): written by the model, checked by code, promoted with Sky's yes, like the cast. Inez's own record is shown here:

```json
"bureau-loan": { "kind": "debt", "label": "Bureau loan", "size": "standard", "holder": "trade:credit-bureau",
                 "acts": ["I","II","III"], "why": "small loans against wages; cafe staff borrow between shifts" }
```

A record may narrow its kind's `metBy` but never widen it. An optional `arisesFrom` names a between-missions event that hands the record out (open question 4).

**Cast rows:** `needs[].need` names a record id. A kind id is still accepted and means the kind's default record (label = Fable's current word), so the promoted cast expands byte for byte until a new run writes records.

**A person in the export:** `need` = the record id, plus `needBind: { holder: "<person or unit id>" | "beyond", subject: "<person id>" | null }`. Mission roles keep `need: "debt"` as a kind. A role may also pin a record and a binding: the Handover pins both of its roles' holders as `beyond`.

## Examples

| kind | record (label) | size | holder | what knowing it opens |
|---|---|---|---|---|
| debt | Pawn ticket | small | pawnbroker in the block | Help for $20,000, or free from a turned pawnbroker |
| debt | Bureau loan | standard | credit bureau (Handover: beyond) | Help for $40,000: Inez, unchanged |
| debt | Friend's loan | small | the debtor tie's lender | turn the lender, the debt is forgiven; `debtor.lapses` reads it |
| cash | Lean month | standard | none | cash only: the driver, unchanged |
| cash | Liner fare | large | travel bureau | Help for $80,000, or a ticket from a turned agent |
| papers | Concourse pass | — | a records clerk | a turned clerk stamps it |
| housing | Clearance order | — | a records clerk | the Handover's own cause (Vale signed the order), as a need |
| kin | Brother held | — | patrol officer; subject: a family tie | a turned officer loses the file; if the tie ends, the need is replaced |
| care | Nurse robot | — | robot repair; subject: a family tie | Help only, as care is today |
| protection | Leaned on | — | the rival tie | the riskier way: Pressure the rival (public, +3 T) and the need is met |
| advancement | Day shift | — | their unit's keeper | turn the keeper, the post follows |

Secrets: "Loves {name}" stays from the lover tie. Records such as "Old conviction" (holder: patrol officer) and "Forged licence" (holder: records clerk) carry a label and a holder. The holder's reader comes in phase 3.

## How it plays

- **Listen** teaches the record: its label, its size and its holder. A keeper's tip and a turned contact's debrief teach the same (§08). A briefing's suspected need stays at the kind.
- **Help's price** is the approach base, plus a size line: small −$20,000, large +$40,000 [S, proposed].
- **The favour route** is open when the holder is turned and has not given a favour this mission. It changes Help's price lines:
  - usd: the cash price drops to $0;
  - Trail: +1 more, because the favour leaves paper an investigator can pull;
  - the holder logs a job, so their bravery can grow (§04).

  It is one favour per holder per mission [S], so a single clerk can't settle every papers need.
- **Pressure and Pay** act by kind, as today. A record may only remove an approach.
- **Readable:**
  - the need slot's glyph is the kind's, so there are no new icons;
  - the label is one or two words;
  - the price chip shows the size or "via {holder}";
  - Alt draws the line to the holder in the street.

  Nothing is added to the HUD budget. It also helps T7: contacts are memorable by what they needed.
- **Live with it:**
  - a met need is replaced between missions from the person's row's records (`need.replaced`, unchanged except that it draws records);
  - a record whose holder or subject leaves the city (tie lapses, arrest) is replaced and named in the debrief.

## Checker rules (people checker, new N-rules; Pulse runs them, Motive owns the rules)

| rule | what it checks |
|---|---|
| N-1 | the record's kind exists (unknown = reject) |
| N-2 | holder and subject use a binding kind that their kind allows, and bind for every person who draws the record; otherwise expansion fails, as P-0 does today |
| N-3 | the label: 1–2 words, ≤ 20 characters, no sentence, no digits, dates or era words, no gendered words, no names (names come only from bindings), unique in the district |
| N-4 | tone: Fable's list and §06's content line. Children are never a subject; care and grief records are never pressable |
| N-5 | `metBy` ⊆ the kind's; every temperament in each row can still reach a record (today's `needFits`) |
| N-6 | a row that offers ≥ 2 records to a position offers ≥ 2 that differ in size, holder or `metBy`, so Listen always tells something (Pulse's 010 finding: every server in debt) |
| N-7 | no orphan records; every act has records in every area |
| N-8 | the Handover pins: Inez = debt, standard, beyond; the driver = cash, standard, beyond (M-2) |

## What each owner changes

| owner | change | done-check |
|---|---|---|
| Motive | `needs.kinds`, sizes, binding kinds and values in people.rules.json; the rulebook's size and favour price lines; GDD §04 text for v0.24 | rules schema tests pass; Keel accepts |
| Pulse | catalogue schema; cast `need` → record; `expand.ts` draws records and binds holder and subject on the person's stream; N-1…N-8 in `check.ts`; records in the levels people prompt (world-scarcity lead, why per record); a migration like `secrets.ts`, so pinned rules and the promoted cast replay unchanged | `bun run people` exits 0; the promoted cast's persons hash is unchanged under pinned rules |
| Keel | Person carries record, kind, size, holder and subject. `need-is` reads the kind (ids unchanged). New vocabulary, once: `need-size`, `holder-turned`, a `favour` memory act and `give-need` (phase 3). `need.replaced` draws records. After that, kinds and records are data | 475/475 engine; the Handover scenarios byte-identical |
| Fable | `less.intel.need` becomes the per-kind fallback (8 words; `secret` goes). Record labels live in the catalogue. Debrief lines by kind with `{label}` and `{holder}`. The test becomes "every kind has a fallback, every record a valid label" (N-3 and N-4 words) | text tests pass |
| Latch | Intel shows the kind word when told and the record label when learned. Alt line to the holder. Help chip "via {holder}" | 143/143 Unity match; HUD budget and layout audit pass |
| Waymark | roles keep `need` as a kind and may pin a record and binding. The mission checker counts favour routes as routes (none essential) | the Handover ships unchanged |
| Glyph | none: the kind glyphs are today's need glyphs | — |
| Suture | a scenario per route: told → learned label; favour route price; size lines | scenarios pass |

**Phases.**

| phase | what | done when |
|---|---|---|
| 1 | structure only: kinds, a default record per kind, the label from the record | every number is identical |
| 2 | size and holder, the favour route, and area catalogues from the model | the model runs inside Pulse's area runs, about +15% tokens on the ~$15 estimate. Needs Aster's go-ahead before any spend |
| 3 | `arisesFrom` needs, acts, holders for secret records | — |

## Open questions for Sky

1. **Are records that differ only in their words allowed?** I recommend yes: it is the world's texture. N-6 makes sure each row still has records that differ in play.
2. **Do kinds stay a closed list grown by rules changes, or should the model be able to propose kinds for you to accept?** I recommend that you accept new kinds, as rules.
3. **One need per person: keep it?** I recommend keeping it.
4. **Needs born of the cell's harm:**
   - a found killing could leave a family in need ("Funeral costs", Help only);
   - an arrest could leave "Brother held".

   Are these in, and Help-only? They are strong for "live with it" and dark for the tone line.
5. **The favour route's price:** $0, +1 Trail and one favour per holder per mission, with the holder's job counted. Is that right as a starting value?
6. **The Handover's labels:**
   - keep "Debt" and "Cash";
   - or let Listen show Inez's "Bureau loan" and the driver's "Lean month" (Fable's words, mechanics unchanged).

Sources read: GDD v0.23.0 §04, §08, §09, §13; `contracts/people/people.rules.json`, `people.rules.schema.ts`, `people.schema.ts`, `harbour-district.cast.json`; `contracts/rules/rulebook.json` (turn.*, need.replaced, debtor.lapses, streams); `sim/Suborned.Sim/Rules.cs`, `Model.cs`; `tools/people/expand.ts`, `secrets.ts`; `briefs/010/pulse-district-plan.md`; `contracts/missions/handover/mission.json` and `text.json` (`less.intel`); `HudToolkit.Intel.cs`.


## Sky's answers (2026-10-18: "I agree with your views")
1. Records that differ only in wording: **no**. Each record must differ in size, holder or kind.
2. New need kinds: **the model may propose, Sky accepts** through a rules change. The model never adds a kind by itself.
3. One need per person: **yes, for now**.
4. Needs born of the cell's own harm: **yes, in phase 3**, Help-only.
5. Favour route: **free, +1 Trail, once per holder per mission** to start; tune by playtest.
6. Inez and the driver show their **specific record labels after Listen**.

Phase 1 (structure only, every number unchanged) starts now. Phase 2's model spend comes back to Sky first.
