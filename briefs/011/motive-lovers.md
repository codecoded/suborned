# 011 · Inez and the guard are lovers: the `lover` tie, the `secret`, and casting (Motive, 2026-10-15)

**Canon (Sky, via Aster).** "The guard comes to visit Inez when she calls him because you bribed / pressured her, and that's what makes him leave his post."

The Story spine (`~/Projects/the-suborned/GDD/Story/the-suborned-story-v0.1-2026-10-03.html`) says:
- **Inez:** "She is having an affair with the office guard, and you learn it by listening in at the café… the debt is her need (Help clears it, and she remembers it kindly), and the affair is her secret (Pressure threatens to expose it, which is cheap, and she resents it)."
- **The guard:** "Inez's lover. He takes a café stop before his shift."

**These are proposals only.** I didn't edit `people.rules.json`, the rulebook or the engine, for three reasons:
- **Loading would break.** Both files are loaded with unknown = reject. The people checker, `bun run people`, M-2 and Keel's loader would all reject a new tie kind or person field until their code reads it.
- **Promoted data would go stale.** The promoted cast table (`contracts/people/harbour-district.cast.json`) weights the need `secret`, which this brief retires.
- **Placement is ready.** The JSON below is written to drop in. Keel places it with the code; Pulse re-runs the cast. Every value is [S] and proposed, with its reason.

## Summary
1. **The `lover` tie.**
   - **Shape:** mutual, at most one per person, always secret. It forms where their routines meet: one stops at the other's work unit while they work.
   - **Introduces:** no.
   - **What it gives:** a turned contact's call summons their lover. The lover leaves what he is doing (his post), walks over, stays **60 s** and walks back.
   - **He refuses** if:
     - he is off the street;
     - he is more than **90 s'** walk away;
     - Alert is **≥ 2**;
     - he has seen the cell's violence;
     - he has already come once this shift.
   - **The visit ends at once** if Alert reaches 2.
2. **The `secret`.**
   - **What it is:** a new person field, 0 or 1 per person, from a closed list:
     - `affair` (requires a lover tie);
     - `record` (a past on file; replaces the retired need `secret`).
   - **How it's learned:** by Listen, or by a Watch that ends while the two lovers stand together. Learning the affair on one lover teaches it on both, along with the tie.
   - **What it does:** a known secret is **Pressure's leverage**.
     - Pressure works at its **proper step** for any temperament that doesn't refuse it: no wrong-approach line.
     - Pressure needs no pressable need.
     - A Loyal person pressed by exposure doesn't report it.
   - **Inez's Pressure:** $0, 35 s, **+3 T** (was +4), plus fear and resentment as before. The Pressure route drops from 8 T to 7.
3. **Casting.** The waitress role gains `tiedTo: [{kind: "lover", role: "guard"}]`, and M-2 checks that the summons can reach him in the front-door window. Pulse's cast tables form lover ties along regular ties, so a café server and a door guard who stops at her café can be lovers.
4. **GDD v0.22.1.** §04 and §07 lines are at the end.

---

## 1. The `lover` tie

### 1.1 Shape (people.rules.json `ties.kinds.lover`)
```json
"lover": {
  "direction": "both",
  "maxPerPerson": 1,
  "between": "routine-meet",
  "introduces": false,
  "secret": true,
  "maxShare": 0.15,
  "formsAlong": ["regular", "workmate"],
  "src": "Story spine v0.1 (Inez canon: an affair with the office guard, learned by listening at the café); Sky 2026-10-15 via Aster: the guard comes when she calls him, and that is what makes him leave his post",
  "proposed": true,
  "reason": "one per person [S]: an affair is one person. Always secret: a public partner is family. Mutual, so both carry it and both carry the secret. Between routine-meet: they must meet somewhere the rules can see, or the call has no walk to price. introduces false: an introduction is a public vouching; a secret lover who vouches exposes the affair, and the two leverage ties (debtor, rival) don't introduce either. maxShare 15 % of a district's simulated people [S]: common enough that most districts hold a pair to cast, rare enough to stay a discovery. formsAlong: the generator draws a lover first from a person's regular and workmate ties (the café regular and the server), so lovers sit where missions cast."
}
```
- **`betweenKinds.routine-meet`:** "either person's routine stops at the other's work unit while the other is on shift". This is the `routine-stop` test, read in both directions.
- **`perPerson` 1–8 counts the lover tie.** `onePerKindPerPair` stays, so a pair may be both regulars and lovers (the guard and Inez are).
- **Lover and family are exclusive for a pair:** a family partner is a public partner.

### 1.2 Secret: who knows the tie
- **Nobody in the city is told it.** It never travels along ties as news (§04 knowledge), and a keeper's tip (§08) gives needs, never secrets.
- **The cell learns it** in two ways:
  - **Listen on either lover.** It reveals need, temperament and secret, and the secret carries the tie.
  - **Watch on either lover**, completing while the two stand within **`together-m` 4 m** of each other.
- **One fact, both ends:** learning it on one lover marks the tie and the `affair` secret known on both.
- **Until it is known,** the tie doesn't show on the dossier's tie list or in any card. A call card to a turned contact whose lover is unknown reads "calls someone · ?" (§1.3). That is the §04 preview rule: the price always shows, and the outcome shows "?" until it is known.

### 1.3 What it lets the holder do: the summons (rulebook event `contact.summon`)
A turned contact who is called sends for their lover. **The call's effect is read from the caller's ties, not from the mission.** That is the systems-over-scripts point: any turned contact with an eligible lover is a lure, in every generated mission.

```json
{
  "id": "contact.summon",
  "by": "contact",
  "phase": "mission",
  "tag": "S",
  "src": "Story spine v0.1; Sky 2026-10-15 (the guard leaves his post because Inez calls him); §07 route A (walks, stays 60 s, returns); §03 Fixer calls contacts in on cue",
  "needs": [
    { "k": "turned", "is": true },
    { "k": "called", "is": true },
    { "k": "tie", "kind": "lover", "as": "lover" },
    { "k": "on-street", "who": "lover" },
    { "k": "walk-s", "from": "lover", "to": "caller", "max": "v:summon-walk-max-s" },
    { "k": "alert", "max": "v:summon-alert-max" },
    { "k": "free", "who": "lover" },
    { "k": "memory", "who": "lover", "act": ["saw-gunfire", "saw-violence", "saw-weapon-drawn"], "by": "cell", "has": false },
    { "k": "count", "what": "summoned", "who": "lover", "per": "shift", "max": "v:summons-per-shift" }
  ],
  "price": [{ "line": "base", "trail": "v:call-trail" }],
  "outcomes": [
    { "id": "comes", "when": [], "changes": [
      { "do": "leave", "who": "lover", "from": "where-they-are" },
      { "do": "walk", "who": "lover", "to": "caller", "pace": "walk" },
      { "do": "stay", "who": "lover", "s": "v:lover-stay-s" },
      { "do": "walk", "who": "lover", "to": "left-from", "pace": "walk" },
      { "do": "remember", "who": "caller", "act": "job" },
      { "do": "count", "what": "summoned", "who": "lover" }
    ] }
  ],
  "also": [{ "when": [{ "k": "alert", "min": 2 }], "changes": [{ "do": "end-visit", "who": "lover", "pace": "run" }] }]
}
```
The condition and effect kinds above are written for Keel to name and type. Where one already exists (`turned`, `memory`, `remember`), use it; the rest are new.

**When the lover refuses**, the order is refused before it is given and costs nothing (house rule). The card names the reason:

| refusal (card) | why |
|---|---|
| "he isn't on the street" | off-street (home, inside); there's no one to walk over |
| "too far: n s away" | over `summon-walk-max-s` |
| "security is searching: he won't leave" | Alert ≥ `summon-alert-max` + 1 |
| "he's busy" | downed, held, engaged in a fight, or already away |
| "he saw what you did" | he holds a memory of the cell's gunfire, violence or drawn weapon |
| "he came once already" | `summons-per-shift` reached |

**The visit:**
- **The route:** he walks from where he is to the caller at walking pace, stays `lover-stay-s`, and walks back to the place he left.
- **On shift or off,** he comes if he is on the street. On shift he leaves his post empty, which is the use.
- **It ends at once** if Alert reaches 2: he runs back at his kind's run speed (a guard: security's 220 cm/s [B]).
- **It isn't a cell act:** no witness Trail, no standing. The call keeps its `call-trail` +1 [B].
- **Nothing new for an empty post:** an empty post the visit leaves is seen as the engine sees it today. Entry while he is back is "entry seen by guard"; a guard downed while away misses his check-in.

### 1.4 Values (rulebook `values`, all [S], proposed)
| id | v | unit | reason |
|---|---|---|---|
| lover-stay-s | 60 | s | §07: "stays 60 s"; Waymark's window keeps 60 s. One value for every lover in every mission; the mission reads it, never restates it. |
| summon-walk-max-s | 90 | s | Keeps a summons local, about one block (≤ 126 m at 1.4 m/s). The Handover's 47 s fits. It bounds the absence at ≤ 4 min, so no generated mission gets an endless lure. A lover across the district doesn't drop everything. |
| summon-alert-max | 1 | alert | He answers through an inquiry (Alert 1: someone ran) but not a search (Alert 2: security staff stay at their posts, civilians scatter). It ties the lure to *play quiet*, as the Story spine's quiet capstone does. |
| summons-per-shift | 1 | count | "He can't be missed twice." It also stops call-spam lure loops. The Handover calls once. |
| together-m | 4 | m | Watching "them together": the mission's `near` 4 m and `approach-reach-m` 4 m. |

---

## 2. The `secret`

### 2.1 Shape (people.rules.json: a new top-level `secrets`, plus `person.secret`)
```json
"secrets": {
  "rule": "0 or 1 secret per simulated person, from this closed list. A secret is a fact the person hides, separate from their need: the need is what they want (Help meets it), the secret is what they fear known (Pressure threatens it). Learned only by the cell, only by Listen, or by a Watch that ends while two lovers stand together (together-m). Never told along ties, never in a keeper's tips. Read by turn.pressure: a known secret is Pressure's leverage.",
  "perPerson": { "min": 0, "max": 1 },
  "list": {
    "affair": {
      "requires": "lover-tie",
      "learnedBy": ["scout.listen", "scout.watch-together"],
      "src": "Story spine v0.1: Inez's affair with the guard is her secret; Pressure threatens to expose it",
      "proposed": true,
      "reason": "every person with a lover tie holds `affair`, and nobody else does: the tie is the secret, so the checker can see it."
    },
    "record": {
      "requires": "nothing",
      "learnedBy": ["scout.listen"],
      "src": "§01 world: records are physical; the retired need `secret` (people.rules needs.list.secret)",
      "proposed": true,
      "reason": "a past on file (a conviction, a false paper, a hidden debt) that a word in the right office could surface. It carries the generator's existing secrets over when the need `secret` retires, so no district loses its pressable people."
    }
  },
  "proposed": true
}
```
- **`person.is`** becomes `["routine", "need", "temperament", "memory", "secret?"]`.
- **`person.readBy.fields.secret`:** "§04 Pressure's leverage when known (turn.pressure); `affair` also the lover tie's summons (contact.summon)". A field nothing reads is rejected, and this one has two readers.
- **`tiers.simulated.carries`** adds `secret`. Background people carry none.
- **`requiresKinds`** adds `"lover-tie": "a lover tie"`.

**Retire the need `secret`.** One person could otherwise have a need "secret" and a secret: two things with one name.
- **For Pulse:** the cast rows' `secret` need weight moves to the `record` secret share. The person's need is drawn from the row's other weights. Then re-run `bun run people` and the promoted table goes back to Sky.
- **Vale's fixture** (need `secret`) gets the secret `record` and a need drawn from his stream. Nothing in the mission reads Vale's need.

### 2.2 What a known secret does: Pressure's leverage
**For Keel: rulebook `turn.pressure` changes.**
- **`needs`:** the pair `requires: pressure` and `need-met-by: pressure` becomes **either** (need known *and* need-met-by pressure) **or** (secret known).
  - So exposure works on a person whose need Pressure can't lean on, such as a driver who needs cash.
- **Price line `wrong-approach`:** adds `{ "k": "known", "what": "secret", "is": false }`. With the secret known, there is no wrong-approach +1 T: **Pressure works at its proper step for any temperament that doesn't refuse it.**
  - Fit `refused` still fails. The §04 Menace rows (an Idealist or a Loyal person in a Menace district refuses every approach) are unchanged, because the standing rows win.
- **`also: reports`:** adds `{ "k": "known", "what": "secret", "is": false }`. **A Loyal person pressed by exposure doesn't report it:** reporting it would expose them.
- **A new outcome line, `leverage`,** on the card: "exposure: their secret". This lets Glyph and Fable say "Threaten to expose Inez's secret", as text.json already does.
- **Effects are unchanged:**
  - fear +1, resentment +1 (`move on pressure`);
  - `remember pressure` with the Fixer's face (can identify);
  - the witnessed `threat` act.
  - "She resents it" is the resentment step.

**A note on Aster's wording.** Aster wrote "at its proper step, rather than one step worse, on a Fearful person". A Fearful person already responds to Pressure, so it is at its proper step for them anyway. The leverage matters for everyone else: Inez (an Idealist), a Mercenary, a Loyal guard. That is the reading above. For a Fearful person, a known secret adds only the "needs no pressable need" and "doesn't report" parts.

### 2.3 Inez's prices, consistent with §04 and my addendum
| Inez (Idealist, cast fixture) | needs | $ | time | T | leaves |
|---|---|---|---|---|---|
| Help (clear debt) | need known (Listen) | 40,000 | 50 s | +1 | gratitude; she warns 10 s early in the safe fight |
| Pressure by exposure | secret known (the same Listen) | 0 | 35 s | **+3** (was +4: the wrong-approach step is gone) | fear, resentment; she can identify the Fixer; no warning |
| Pay | routine known (Watch) | — | — | — | refused: an Idealist refuses bribes, and remembers |

**The Trail ledgers that follow:**
- **Route A + Pressure:** 8 T → **7 T** (loop 1, threat witnessed 1, Pressure 3, call 1, reroute 1).
- **Loud:** 9 T after the addendum's two changes. The quiet ugly route stays 2 T under loud.
- **Route A by Help:** unchanged (5 T).
- **"Cheap" is Sky's word:** Pressure is $0 and 15 s faster than Help. Its cost is the person: fear and resentment make her a Talker, she can name the Fixer's face, and she gives no warning.

**Knowing more lowers the price (pillar 1).** The same 42 s Listen that finds the debt finds the affair. The informed player sees both approaches priced honestly: Help at $40,000 and +1 T, Pressure at $0 and +3 T without the wrong-approach step.

### 2.4 The guard can be pressed too (a consequence, for Waymark)
**Listening to Inez tells the cell the guard's secret as well** (one fact, both ends). The guard is Loyal (Help suits him, so Pressure is one step worse, and he reports any approach). With the secret known, Pressure on him works at its proper step and he doesn't report it.
- **Today, turning the guard buys nothing in the mission:** his role has no job, and a turned guard is only a guard who won't report.
- **If Waymark gives a turned guard the generic job "step off post when called"** [L], that is a third way past the door, still through the affair. It fits "everyone can be turned more than one way", and it eases MC-4, since route A then wouldn't hang on Inez alone.
- **That is Waymark's call.** These rules allow it and don't require it.

---

## 3. Casting (Waymark owns `mission.json`; what the role needs)
**The waitress role** (`roles[id=waitress]`) gains:
```json
"tiedTo": [{ "kind": "lover", "role": "guard" }],
"secret": "affair"
```
- `secret: "affair"` is implied by the tie. List it so M-2 and the review read the canon from the role.
- `need: "debt"` and `approach: "help"` stay as they are: the debt is her need, and Help is the reference route. Add `"approaches": ["help", "pressure"]` if the role lists the routes it supports. Pressure now works by the secret, not the debt.

**The guard role** keeps `regularOf: "cafe"` and `tiedTo: [{kind: "regular", role: "waitress"}]`: that is his café stop, and how they met. The lover tie is mutual, so listing it on the waitress is enough. M-2 checks it from both ends.

**M-2 must also check that the summons can happen in the front-door window.** M-2 reads every value from `contact.summon` and the rulebook values, never from the role:
- the guard's walk from his post (Office entrance) to the waitress's place (Café door) is ≤ `summon-walk-max-s`. Today it is 47 s, against 90.
- both are on the street throughout the window, 1:52–5:30.
- the guard is one person on the door (`alone`, as today).

**`windows.front-door`** should read its numbers from the rule:
- `stayS` = `lover-stay-s` (60);
- `walk.s` = the walk-graph time (47).
- That keeps the 2:34 window, now derived. If the walk ever exceeds 90 s, M-2 fails: the city no longer supports the lure.

**The cast (Pulse):**
- **The pair must exist:** M-2 needs a lover tie between a café server in place at the café door and a records-office door guard who is a regular of that café.
- **Where it forms:** with `formsAlong: ["regular", ...]`, the generator draws lovers from regular ties first, so a server and a guard who stops at her café is the likely pair.
- **Guarantee, without hand-placing anyone:** the food × server and civic × guard rows give a lover rate high enough that the district holds at least one such pair. The people checker then reports it (a new P-rule: "every mission role's tie is castable").
- **If the seed yields none,** M-2 fails loudly, as it does today for any uncastable role. Casting never invents a tie.

**For Keel:**
- **Replace the hard-coded lure.** The waitress's call is hard-coded to lure the guard (`Call` → `LureTick`, `walkT`, `stayT`). It becomes `contact.summon` read from the cast's ties, with `walkT` from the grid route and `stayT` from `lover-stay-s`.
- **The summons reaches whoever the tie names.** The driver's call (the camera block) is a role job, not a summons, and stays as it is.
- **Card and dossier:**
  - the call card shows "calls her lover (the guard): he leaves the door ~2:34" once the tie is known, and "calls someone · ?" before;
  - the dossier's tie list shows the lover tie only when known.

---

## 4. GDD wording for v0.22.1
**§04 PEOPLE**
- **person:** "person = routine + need + temperament + memory, and sometimes a secret. Knowledge lowers price; standing decides who listens."
- **approaches table, Pressure row:** requires "need or secret known"; leaves "fear; resentment grows; can identify Fixer". Under the table: "Pressure on a known secret (exposure) works at its proper step for any temperament that doesn't refuse it, and a Loyal person pressed by exposure doesn't report it."
- **needs:** delete "· secret" from the list.
- **New line:** "secret [S]: 0–1 per person: affair (holds a lover tie) · record (a past on file). Learned only by Listen, or by Watching two lovers together. Never told along ties. A known secret is Pressure's leverage."
- **ties:** add "· lover ≤ 1 (always secret; their routines meet; doesn't introduce)".
- **New line:** "lover [S]: a turned contact's call summons their lover, if he is on the street, ≤ 90 s away, Alert ≤ 1, hasn't seen the cell's violence and hasn't come this shift. He leaves his post, walks over, stays 60 s, walks back. Alert 2 ends the visit."
- **knowledge:** append "Secrets are never told: a keeper tips needs, not secrets."

**§07 HANDOVER**
- **cast:** "Vale · Inez (café waitress, in debt; the door guard's lover) · guard (Inez's lover; café stop before his shift) · official driver (needs cash) · rival crew · cameras · patrol."
- **casting:** append "The waitress role requires a lover tie to the door guard, within the summons' 90 s."
- **route A:** "Scout watches café, listens to Inez (1:02): her debt, and her affair with the guard → Fixer helps Inez ($40,000), or presses her with the affair ($0, +3 T: she fears and resents it, and can name the Fixer) → Tech loops entrance camera (1:02, tethered; optional patrol reroute) → Fixer calls Inez: her lover leaves his post to see her: walks 47 s, stays 60 s, walks back 47 s = 2:34 window [payoff cut] → Enforcer enters (38 s; 68 s at Alert ≥ 2), exits service door with Vale, to car."
- **safe fight:** unchanged: "helped Inez warns 10 s early". The Story spine's "a pressured Inez may identify the Fixer" is already §04's Pressure.

**§09:** the people row's "occupation × temperament × need × relationships × routine" gains "× secret".

## 5. Open
1. **Whether a turned guard gets a "step off post" job:** §2.4, Waymark's call. I recommend [L], after the capstone holds with Inez.
2. **`maxShare` 15 % and the cast rows' lover rates:** Pulse tunes them to the seed. The test is that M-2 casts the Handover on the promoted district.
3. **Whether the `record` secret should also have a summons-like use** (a clerk who can lose a file): [L]. Today Pressure's leverage is its only reader, and that is enough to keep it.

## Next owners
- **Aster:** take §4 to Sky for v0.22.1, and relay §1.4, §2.2 and §3's Keel items to Keel.
- **Keel:**
  - the `lover` tie, the `secrets` section and `person.secret` in people.rules.json, with their loaders and schema;
  - `contact.summon`, replacing the hard-coded lure;
  - `turn.pressure`'s leverage;
  - the five values;
  - re-run the scenarios. Expected: the Pressure scenarios lose the wrong-approach +1 T, and route A's timings are unchanged at 47 + 60 + 47.
- **Pulse:**
  - retire the need `secret` in the cast tables, and add the `record` share and the lover rates with `formsAlong`;
  - re-run `bun run people` and the fixture;
  - the new P-rules (lover mutual, ≤ 1, routine-meet, affair ⇔ lover).
- **Waymark:**
  - the waitress `tiedTo` and `secret`;
  - the front-door window read from the rule;
  - the M-2 summons check;
  - re-record c003. Expected: the Pressure route 7 T.
- **Fable:**
  - the refusal lines (§1.3);
  - "calls someone · ?";
  - the exposure line, which text.json already has.
- **Glyph:** the lover tie on the dossier, shown only when known.
