# The dialogue generator (Fable, brief 012)

Status 2026-10-16: built and tested offline; no live call yet. `bun run dialogue --standin --handover` runs it end to end; `bun run levels dialogue --scope <scope>` is the model run. Owner Fable; Keel critic; Motive reviews temperament voices; Latch ports `tools/dialogue/lookup.ts`.

## What A3 did, and what carries over

A3 (`assignment-03-agent-crew/src/crew.ts`, `schemas.ts`, `checker.ts`): after Motive wrote 4 contacts, the dialogue-designer wrote `{personId, situation, line}` for each person, 7 situations (Ask, Pay, Help, Pressure, refusal, confirm_call, questioned). The checker (`checkDialogue`) only asked that every person had a non-empty line per approach, refusal, questioned and (if called) confirm_call. Keel reviewed, faults went back to the owner, 3 rounds. Never ported (003 on hold).

Carries over: the handoff shape (tables → checker → Keel → revise, owner-routed faults); the seven situations (Ask/Pay/Help/Pressure/refusal = the replies, confirm_call = `confirm/call`, questioned); presence as the first rule (D-0, D-1). Does not carry: the model writing a line per person (126 people; not replayable per person; nothing about tone, length, names, secrets, ties), and the mission-bound cast.

## What is new

The model writes **tables** by voice group × temperament × situation, plus one lexicon and the scenes between tied people. Seeded code **expands** them into each person's lines and scenes, filling slots from who they are. The Handover's `text.json` is untouched and is the bar: its person lines are the fixture tables, and a bound-people comparison (`--handover`) must match or beat them.

- Rules: `contracts/dialogue/dialogue.rules.json` (`suborned.dialogue.rules/1`). 7 voice groups cover every position and role of the people rules once each (counter, workshop, resident, clerk, security, driver, official); 4 scopes: `lexicon`, `scenes`, `street` (counter, workshop, resident), `civic` (clerk, security, driver, official).
- Tables: `contracts/dialogue/voices.schema.ts`, `suborned.voices/1`, one file per scope. Expansion: `suborned.lines/1`, per person, in the shape of text.json `dialogue` entries (speaker = the person), so the host's `lookup` and `pickIndex` serve it (`tools/dialogue/lookup.ts` is the reference, with test vectors).

## Schema of the tables

| block | what the model writes |
|---|---|
| `groups[].cells` | `event` `verb` `outcome` `temperament` + 2–4 `lines`. Required: every rulebook reply (30: approach × temperament × outcome, derived with `rulebook.ref.ts`, `suspicious` when the approach is reported), `confirm/call` per temperament, `questioned/gives|lies|nothing`, `saw-gunfire`, `gunfire`. Optional qualified cells: `need` or `secret`, only on replies and questioning (≤ 12 per group) |
| `groups[]` | `voice`, `address` (3–6 terms), `reads.watch` (the Scout's Watch read-out) |
| `scenes` | listen × tie kind (or none) × temperament (28), call × lover × temperament (4), watch × lover (1), watch × group. 2–3 variants of 3–6 beats; `who`: focus, other, bystander, scout |
| `lexicon` | each need as `noun` / `own` / `met`; each position as `job` and `noun`; the Scout's Listen read-outs per temperament; the debrief lines (one set per rulebook `debrief.lines` id, carrying every field as a `{slot}`); the lovers' summons lines (7 engine codes + `recalled`, a `caller` and a `summoned` end) |

Slots (closed, per context in the rules): cell `address job noun work need own met`; scene `focus other work need own met` (a Watch surfaces no need); the lovers' summons `other`. Need slots only in replies to Help and Pressure, questioning, scenes and read-outs.

## Checker rules (`tools/dialogue/check.ts`, D-0..D-12; each has a failing example in the tests)

D-0 every situation has a cell and only those; a scope writes only its blocks · D-1 every person speaks (unknown position = reject) · D-2 filled lines ≤ 80 chars, spoken 1–12 words, sentence ends, 2–4 variants · D-3 slots allowed, filled, none left · D-4 no names in tables; a scene names only its two people · D-5 tone: Handover avoid/profanity lists, rating (explicit, gore), civilians never the payload, **no gendered word** · D-6 signal words (the cell's names, temperament labels, digits, names awaiting Sky) · D-7 no repeats · D-8 knowledge: a cold Ask or Pay surfaces no need; qualifiers name real needs and secrets · D-9 expansion byte-identical twice and valid as `suborned.lines/1` · D-10 scene shape · D-11 lexicon complete (needs, positions, temperaments, debrief fields, summons keys) · D-12 the Handover's reference lines matched in key and variants.

Not carried to generation: the engine's refusal codes, fail codes and log codes (`locks`, `briefing.fails`, `silentLogs`). They are mission-wide, not per person, and stay in `text.json` under its own test. The person-facing codes (replies, summons, debrief ids) are derived from the rulebook and engine and drift-tested.

## How lines are picked per person

Code expansion (`tools/dialogue/expand.ts`): the person's group is the one that covers their position; for each situation their temperament can be in, the cell is the most specific the lookup finds (need and secret qualifiers beat the plain cell); every template is filled from the person (address = `fnv1a32(seed|id|address)` mod the group's terms; job and noun from the lexicon by position; workplace from their unit; need forms from the lexicon). At play time the host picks `pickIndex(seed, key, n, count)` with the person's id in the key, so two people with one table do not say the same thing at once, and no variant repeats back to back.

## Scenes between tied people

Per person, from their own ties (a directed tie stands on its source; the partner is `with`): Listen uses the lover for the affair, then the tie kind their need points at (debt→debtor, kin→family, protection→rival, advancement→workmate…), then workmate, regular, family, debtor, rival, else a bystander; among several of a kind the least `fnv1a32(seed|id|kind|partner)`. The scene template is by tie × the focus's temperament; `{other}` is the partner by given name, `{other_noun}` their post ("the guard") or "a relative" for kin. A lover also gets the call and the lovers' Watch, and the summons lines in both ends. The Learned line is written by the rules from the person's data (need, `{temperament}` left for the host, the secret for an affair or a record), never by the model. Approach scenes (the Fixer and the contact) are not generated: the host composes them from `exchange`, the person's reply and the Fixer's reaction.

## Runs it needs

Run order: `lexicon` first, promote it (`bun tools/dialogue/promote.ts <run> --scope lexicon --by "Sky, <date>" --why "…"`), then `scenes`, `street`, `civic` in any order (each fills its slots from the promoted lexicon). Four accepted runs make the district's voice. Runs land in `runs/levels/harbour-district/rNNN`, replay offline, and pin people, rules, rulebook, Handover text and lexicon. Model calls are opt-in; tests use stand-ins.

Open: Hero/Menace band variants and state-banded cells (bravery step, resentment, promoted) are the next qualifiers; the lookup already supports more of them. The Handover's hand-written lines gender people ("I'll call him over"); the generator's bar does not (decision for Sky: keep it).
