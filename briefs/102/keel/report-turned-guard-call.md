# Brief 102 · Keel's report: the turned guard's Call, engine 0.51.0 (2026-11-09)

Branch `t3/turned-guard-call` (from `work/098-living-playbook` at 08974feb4). Sky's calls on the guard (brief 102, "Sky's calls on the
guard"); Motive's spec, `briefs/102/motive/turned-guard.md` §1.1-1.4 and §2.2; my proposal, `call-offer.md`.

## What changed (engine 0.51.0)

- **The split.** The call block left `GameOrders.cs` (776 → 683 lines) for a new `GameCall.cs` (166 lines): `CallOffered`, `CanCall`,
  `CallQuery`, `CallRefusal`, `SummonRefusal`, `Call`, `Summon`, and the pair helpers.
- **The offer law.** `CallOffered(role)`: a turned contact whose call has a use under the rules. Built like `WatchOffered`, on the same
  helper as the refusal (`CallAbsent`: the driver's retired job and no lover give `nothing-to-call`, a pair where neither keeps a post gives
  `summon-no-visit`). Absent: the turned driver, an untied contact, anyone not turned, Popovic. Offered: Inez and, under option 1, the
  turned guard. What has happened (down, busy, the alarm, a memory of violence, came once, too far) keeps the row, shut with its code.
  `CanCall(role)` is now `CallQuery(role).ok`: the driver's old "ready" drift is gone.
- **Step 4.** A clause at the top of `SummonRefusal`: nobody acts on a call while they hold a memory of the cell's gunfire, violence or drawn
  weapon. It reads the one called first (new code **`call-saw-violence`**), before every check on the one who leaves. The same test reads the
  one who leaves (`summon-saw-violence`, as before). A told entry (act `told`) is no such memory, and neither is fear.
- **Option 1.** The visit reads the pair (`VisitPair`): the one who keeps a post leaves it (today only the door guard's door is modelled),
  the one visited is met at her post (brief 022). So a call on the turned guard sends him to Inez's post for a minute and back: the window
  her call gives (1736 ticks, 2:53.6, on the Handover start; the pressed one if either of them remembers a Pressure). The once-a-shift
  count sits on the one who leaves, whichever of them is called. Either of them down is `summon-busy`. Log `call.guard`, chain
  `guard-goes-to-waitress`, `LureCalled` (output only).
- **The debrief.** A turned guard counts as a contact, with a kept line: `guard-may-identify-fixer` (pressured), `guard-may-be-questioned`
  (his trip made, already traced as `staged-distraction`), else `guard-leaves-door-when-called`. When the call that drew him off was his
  own, the route line is `guard-goes-to-waitress`, not `waitress-pulls-guard-away`, and Inez's turn and call no longer count as the route's.
- **Host reads (Latch), read-only, unhashed:** `CallOffered(role)`, `CallLeaver(role)` (who would leave a post: "guard" from either end, ""
  where there is no visit), `CallWouldBePressed(role)` (the short visit, read from either end). `GuardCallWindows(role)` now answers for
  the guard's own call too, and returns null when the one called holds a memory of violence.
- **The rulebook, in one round** (`summon.rule`, `src`, `reason`): option 1's "the one who leaves" in place of "the lover", `perShift`
  counted on the one who leaves, the short visit from either end, the step 4 law, Motive's §1.2 offer sentence (with "a visit the engine
  plays" for "a lover the engine sends on a visit", which option 1 makes true from either end), and the fix to the retired camera-block
  sentence. No value moved.
- **text.json** (placeholders for Fable, as 0.42.0 did): the lock `call-saw-violence` ("{who} saw what the cell did." / "Saw violence"),
  the route line `guard-goes-to-waitress` and the three kept lines. The text test requires a line for every code and debrief id.

## Evidence

- Behaviour: `suborned-sim hashes --start-text keel-102` before and after: 0 of 517 scenarios and route scripts moved; 12 new.
- `suborned-sim check`: 484/484 scenarios, each with its replay.
- `bun run sim:test`: 3374 passed, 0 failed (145 s wall).
- New `ScenariosCallOffer.cs` (12 scenarios); `TheCallQueryAndIssueAgree` asserts `CanCall` and the offer against the query on two more
  scenarios (both ends, and the guard's own memory); new `AToldEntryIsNoMemoryOfViolence`.
- DP1's drive session re-recorded (its orders unchanged; the inputs hash moved).
- **Mission record c063** on the tree (engine source sha256 2bbb7ef1…, the committed one): HOLDS, the same 15 red as c062, so
  `KNOWN_RED` is unchanged, and no condition's measured value moved. All 26 plays keep their outcome, end tick and Trail; every
  checkpoint hash moved with the inputs hash (the summon rule's words). Compose: the 19 route scripts compose unchanged; the same 8
  don't, as on c062 (Waymark's plans), so `missions:rerecord` stops at its compose step and the record ran as its second step, as in 0.50.0.
- `bun tools/lint/comments.ts`: 0 hits. `bun run typecheck`: clean.
- TS: text, rulebook, dialogue, style-agent hand tests green; `tools/routines/rounds.test.ts` "recorded routes that touch Inez" fails, as it
  does at HEAD (Pulse's, already on 0.50.0's list).

## Not as written, and why

- **"She was only told of it"** is an xunit test, not a scenario. Nothing in the Handover tells Inez in play (her two workmates work
  inside; the guard radios rather than tells), and a scenario must replay from its start and orders, which hold no memory. The test writes
  the entry exactly as `GameTakedown.Tell` does. The real-play neighbour, C102-UNSEEN (the guard knocked out out of her sight), is a scenario.
- **"She saw her lover killed"** is gunfire, not a takedown. Away from his door the guard senses anyone within 3 m (`perception.near`), so
  by the rules a takedown at the café always fails into a fight.
- **"The lover's acts"** read `saw-gunfire`, `saw-violence` and `saw-weapon-drawn` only, so a seen takedown, the spec's first case, would not
  have counted. One shared test now adds `saw-takedown` and `attacked-aware` (a takedown seen coming, with the Enforcer's face) at both ends.
  For the guard as the one who leaves this changes nothing: a takedown he sees engages him first (`summon-busy`), and no run moved.

## For the next owners

- **Latch:** the `CallOffered` swap (`HandoverHost.cs:813`, `:1152`); Intel's call line only where it is offered; nothing rings for
  `call-saw-violence` (`FeedSummonRefused` and `TieHears`); the call scene between the Fixer and the guard (log `call.guard`). `DoorClock`'s
  Call branch reads only the target's memory: under option 1 it should read `CallWouldBePressed`. `GuardCallWindows("guard")` now gives a
  window, so the guard's Pressure card shows a door clock.
- **Fable:** the lock's words for `call-saw-violence`; the shut lines from the guard's end (`summon-busy` now also means "she's down" when he
  is the one called); the kept lines and `guard-goes-to-waitress`; text.test.ts expands `call.` to `waitress` only, so `call.guard` needs its
  events or a silent entry.
- **Suture or Splice:** a `CLAIMS` row for `guard-goes-to-waitress` in `tools/qa/checks/outcome.ts` (backed by `call.guard` and
  `guard.at-cafe`); without it the bot report gives an info finding.
- **Waymark:** the guard's end as a new variant of route A, across seeds.
- **Aster (GDD catch-up):** the call laws as built: the offer, step 4 (with the takedown counted), option 1 from either end, and the debrief's
  contact.
