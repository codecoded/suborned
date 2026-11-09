# Pressure under the mast: the ruling

Brief 101 · Motive (systems) · 2026-11-09 · **ruling only**: nothing in `sim/`, `contracts/rules/`, the GDD or Unity has changed.

What I worked from: Vantage's `briefs/101/vantage/square-camera.md` (option B, Sky's pick in `briefs/101-hud-sweep.md`), GDD v0.39.0 §02, §04, §05, §07, §09 and §13, the rulebook, engine 0.51.0, and the last full bands run, c063 (15 plans × 200 seeds; rows in `out/missions/handover/bands/rows.jsonl`).

## The short answer

1. **Pressure under the mast: keep the price.** +1 Trail and the desk's report (Alert 1) is the on-camera law we already have, not a new price. The move changes how often it applies (Inez is in the mast's view 8 % of the mission today, 92 % under B), not what it costs. It isn't a trap: the mast is the most visible thing in the first frame, the card shows the price before the click, Alert 1 doesn't stop the Call, and the Tech's loop first is a fair counter for the same Trail.
2. **The posted rivals: fair.** A Pay is a Pay and a takedown is a takedown, whoever it's done to. The seed decides which man stands under the mast; a Watch shows it, the cards show the price, and there's always an answer.
3. **The bands: keep every one.** No number moves. thin-quiet should drop into its band from above (it's over its ceiling today). Add Vantage's start-stance line, and hold §09's "no quiet route has a loiterer reported" for the prepared quiet plans.
4. **One consequence the note doesn't list.** When the Fixer stands beside Inez after she's turned, the mast's log ties her to the cell (the exposure law). Help still isn't taped, but between missions a helped Inez grows resentful instead of braver, and a pressured one becomes an Informer, unless the mast is looped across the turn. That's fair under the same law, but Keel's N18 has to expect it, and Sky should know.

---

## 1. Pressure on Inez under the mast

### The law it falls under
§05 "on camera" (rulebook `camera`, engine 0.40.0; Sky 2026-11-04, "all. recommended"):
- A live camera that sees the Fixer or the person being turned at any second of a Pay or a Pressure records it.
- The price: +1 T, no Standing. When its desk sees the tape (at the next scan + 3 s) it counts as a found fault and as an incident. The dial sends its robots to the spot (two at S2), and the investigators get a lead with the Fixer's face.
- A loop, a cut or a shot afterwards doesn't undo it. Ask, Help and Listen are never taped.

Under B none of that changes. Only the odds of it applying do.

### What it costs, on Inez

| | Help | Pressure, mast live | Pressure, mast looped first |
|---|---|---|---|
| $ | 40,000 | 0 | 0 |
| time | 35 s | 25 s | 25 s, plus the Tech's walk and the 20 s Loop |
| Trail | +1 | +2 (+3 without a Watch), +1 taped: **+3** | +2, +1 for the loop: **+3** |
| Alert | 0 | **1**, at the desk's next scan + 3 s | 0 |
| also | | two robots to her spot; the Fixer's face on tape | the entrance loop waits until about 3:05 |
| the guard's window | 2:54 | 1:39 (she warns him) | 1:39 |

(After a Watch she reads uneasy, so Pressure is "ready" and costs one Trail less: +2.)

### Is it a trap? No
- **The player can see it coming.**
  - The mast stands 3–8 m from the four in the middle of the opening frame, with a "?". Three of the four can Watch it without taking a step.
  - **Watched:** before the click, the Pressure card adds the tape's +1 to its Trail and the Alert to its Alert (`HostTape.cs`), and says "{camera} would tape it: Alert {alert} in {s} s, +{trail} T".
  - **Not watched yet:** the card shows "A ?→1" and says "A camera might tape this: Watch {camera} to be sure".
- **The player can survive it.**
  - A taped Pressure uses up the cell's one free incident. It doesn't end the route.
  - At Alert 1 the guard still answers the Call (`summon-alert-max` 1).
  - A loiterer report is a found fault, not an incident: `GameAttention.cs` raises Alert without counting one. So a taped Pressure plus the Fixer reported for waiting at the café is still Alert 1.
  - Today's run shows this already. A camera tapes thin-quiet's Pressure on 85 of 200 seeds. Those seeds win 89.4 %; the rest win 93.0 %.
- **The real price is what comes next.** After the tape, any second incident makes it Alert 2: a missed check-in, a found body, a second tape. At Alert 2 the Call is refused and the final fight is loud. The card shows this too: its Alert reads the incident count, so it says ▲2 if an incident has already landed.
- **There's an answer:** loop the mast first (below), or Help, which is never taped.
- **Help and Pressure still split the way they should.** Listen then Help stays the clean way; Pressure stays the free, fast, dirty one. B makes the difference plainer, and that is why brief 096 made the law.

### The Tech's loop first is the fair counter
- **What it costs:** the same +1 T as the tape. What it buys is silence: no Alert, no robots, no face on file. That's the trade the law was written for (rulebook `camera`, `reason`: "looping first is worth its own +1 T").
- **It fits at a person's pace.**
  - The Tech reaches the cabinet in about 25 s, so the loop runs from about 0:45 to 2:45.
  - The Scout's Watch (from the start, 17 m), the Listen and the Pressure all fit inside it, with a minute to spare.
  - What it costs in time is the one loop: the entrance loop can't start before about 3:05.
  - Vantage's numbers: a call at 2:35 keeps the pressured guard away until about 4:14. An Enter (38 s) from about 3:05 then has about 30 s spare. Waymark measures it. Pressure isn't a reference route, so the 30 s window rule doesn't bind it, but the margin should be there.
- **The loop has to cover the whole act.** One live tick means a tape, and the card says "loop ends m:ss" when the loop runs out mid-act.
- **Disable isn't a counter here.** Under B, a Disable brings the dial's robots to the mast, which means to the cell's start and the car, for +2 T and Alert 1. That's dearer than the tape. It's only worth it for a cell that wants the mast dead for good.

### Does the price show before the click?
Yes, with one gap:
- A watched mast's card adds the tape's Trail and Alert.
- An unwatched mast's card ("?") raises the Alert as "?→1" and says "A camera might tape this", but leaves the +1 T out of the Trail. §10 says the "?" card carries the Trail too.
- Under B this is the card most first-time players will see. The fix: show the tape's Trail as uncertain on the "?" card, as the Alert already is (Glyph's look, Latch's `HostTape.cs`). It's a small change.

### No value changes
No dial moves. If Sky ever wants taped acts to cost less or more, the dials are general ones, not the mast's:
- `witness-trail`: every incident's Trail;
- `camera.alert`: every tape's Alert;
- `camera.approaches`: which acts get taped.

---

## 2. The posted rivals at posts 2, 4 and 5

Fair, under the same laws, with no change:
- **A Pay is a Pay.** Since engine 0.41.0, a crew member paid in a live camera's view is taped like anyone (`GameCameraRecord.cs`).
  - The tape reads either face, so the Fixer standing out of view doesn't help when the man is in it.
  - Its robots go to his post, which is beside our car.
- **A takedown a camera sees is reported, and lands at Alert 3** (lockdown, and the district seal starts). §07 already says so. Before the commit, the takedown card shows the camera's eye and ▲3: its "seen by" counts every camera (T9: "takedowns cost what they show").
- **The seed decides who stands under the mast, and the player can read it.**
  - The posts are set once, at the start. A Watch on the crew shows where each man will go, a Watch on the mast shows its cones, and the cards show the price.
  - The seed changes which answer fits, never whether there is one:
    - **On route B:** loop the mast first. The Tech's loop is free there: the Help on the driver needs no loop, and the Reroute isn't a loop.
    - **On route A:** the one loop belongs to the entrance camera. Use Mislead (never taped), timed so the man is away when the crew springs, or go for the men at posts 1 and 3, who are out of the mast's view.
- **Prepared-loud isn't affected.** It pays its man at the mouth of the service drive, far from the mast.
- **Keel, one proof is missing.** The camera scenarios prove the tape's card on Inez and on the driver, never on the crew. Add one: rival-2 posted under the mast, the Pay card shows the tape before the commit, the record lands, and the robots come to his post.

---

## 3. What Waymark holds the bands to

**Keep every band as written.** A band says what a way of playing should earn. Moving the mast changes how a plan has to read the street, not what any way of playing deserves. A miss after the move is a report to me, never a reason to move the number or bend a plan to fit (Sky, brief 080: "shouldn't be so rigid").

| line | band | today (c063) | after B |
|---|---|---|---|
| B-1 quiet-a | ≥ 90 % | 99 % | **Keep.** Help, Listen and the Call are never taped, so a plan that reads the mast loses nothing. The Fixer waits behind the mast. The Scout watches Inez from the start (17 m). The Fixer calls from the wait: a Call has no reach (`GameCall.cs` checks no distance), so there's no walk in for it, and no log beside a turned Inez (§4). |
| §09's line "no quiet route has a loiterer reported" | ship line | a loiterer reported on 81 of 200 quiet-a seeds | **Hold it at 0 of 200 for every prepared quiet plan.** Sky's card in brief 101 was this kind of report. |
| L96 A | 90–97 % | 99 % (over) | Keep. Not B's to move. |
| B-2 thin-quiet | 60–90 % | 91.5 % (over) | **Keep.** Expect it to drop into the band: the tape lands on nearly every seed instead of 85 of 200, and taped seeds win 89.4 % today. Keep the plan thin: no Watch or loop of the mast, no switch to Help, waits where they are. If it lands under 60, report the cause before anything moves. |
| B-3 careless | ≤ 30 % | 0 % | Keep. |
| B-4 prepared-loud | 50–75 % real loud wins | 54.5 % | **Keep.** Its Inez work is Help (never taped), its waits move with route A's, and its rival Pay is on the service drive. It sits only 4.5 points over its floor, so read it carefully. |
| B-6 | ≥ 40 points over unprepared loud | 54.5 against 0 | Keep. |
| B-8 | whole cell idle ≤ 45 s (p95) | quiet-a 30 s · prepared-loud 15 s | **Keep.** Moving a wait behind the mast changes where one agent waits, not whether all four are idle. (quiet-g's 82 s miss has its own cause.) |
| L96 B-thinned | out within 3 points of quiet-b; all four out ≥ quiet-b + 10 | out 100 against 100; all out 64 against 32.5 | **Keep.** R96 B-thinned was retired with engine 0.42.0. On seeds where rival-2 posts under the mast, the plan does what a player reading the Pay card does: the Tech loops the mast, then the Fixer pays. Not: skip the Pay on those seeds, pin his post, or move the band. If the plan can't read the card's tape mark, Keel adds that one condition. The mark is on the card, so a player can see it (brief 080: plans act on what a player can see). |

- **A new rigid line (Vantage's, agreed):** no camera head sees any point of the cell's seeded start stance (each mark ± 2 m), on any seed. The patrol keeps its first-15-seconds rule. This is the seeded start's own limit (§07; brief 080: "nobody starts where a watcher already sees the cell's opening") for a watcher that now stands 3–8 m away.
- **Two report lines, no band:**
  - thin-quiet: the share of seeds where its Pressure is taped (expect ≥ 90 %);
  - B-thinned: the share of seeds where rival-2 posts under the mast, and how often the plan looped first.
- **The saved route scripts** (route-a, route-a-pressure(-board), route-a-disable, route-b-thin, careless-thin-all) are replay tests, not bands. Re-record them on the new site. An outcome that moves because the mast now tapes or sees it is expected, not a bug to tune away.
- **Every other line** (B-5, B-7, B-9 to B-16, the rest of L96) keeps its band. None of them reads the mast.

---

## 4. Not in the note: the mast's log exposes Inez

- **The law.**
  - Rulebook `exposure`: a contact's job is exposed when "a watcher logged a cell operative within approach reach (4 m) of the contact".
  - Every live watcher logs every agent in its sight once a second, act or no act (§05 watchers; engine `Watchers()` in `GameInvestigation.cs`).
  - Between missions, an exposed job moves resentment +1 instead of bravery +1 (`ladder.job`).
- **What happens under B.**
  - When a Help or a Pressure lands, Inez counts as turned, and the Fixer is still within 4 m of her.
  - She is in the mast's view 92 % of the time, so unless the mast is looped, it logs that moment on nearly every seed.
  - A helped Inez then goes from compliant to resentful, instead of from informant to helper.
  - A pressured Inez takes the Pressure's own step and the exposure's: she becomes an Informer.
  - Inside the mission nothing changes. Help is still never taped.
- **My reading: fair, under the same law, with the same counter.**
  - The Help card's eye already opens for the mast: its "seen by" counts every camera (`GameSight.cs`, `SeenAt`).
  - A loop across the turn's landing prevents the exposure.
  - It's the first pillar at work: knowing the mast's cones keeps Inez loyal.
  - No law change.
- **Who it touches:**
  - **Keel:** N18 expects Inez to be exposed only when the investigators question her, but the engine also counts the log. Expect N18 to fail after the move, and make it expect what the engine does: exposed if questioned or logged.
  - **Waymark:** the Call needs no walk in (§3), so it adds no log.
  - **Fable:** the host shows no ladder line today. When it does, the line names the camera as the cause.
  - **Sky, via Aster:** under B, the café camera makes the clean route cost Inez's goodwill unless the mast is looped across the Help. If Sky would rather a private Help never exposed anyone, that's a change to the exposure law, not a value. I recommend keeping the law.

---

## 5. Handoffs

1. **Keel**
   - Inputs: §2's missing proof (a Pay on a crew member under the mast), §4's N18, and the mast's facts in N67, N110 and 096 after the world merge.
   - Done-check: `bun run sim:test` passes, with the crew-Pay tape scenario and N18 reading the log.
   - Next: Waymark.
2. **Waymark**
   - Inputs: the §3 table, the start-stance rigid line, the two report lines, and the Call from the wait.
   - Done-check: `bun run guard bun run missions:check bands handover` reports every line above.
   - Next: Motive, to read any miss.
3. **Glyph and Latch**
   - Inputs: §1's gap (an unwatched mast's card leaves out the tape's Trail).
   - Done-check: the Pressure card on an unwatched mast shows the tape's +1 T as uncertain, the same way it shows the Alert.
   - Next: Fable, if the words change.
4. **Fable**
   - Inputs: §4.
   - Done-check: nothing today; the ladder line names the camera once the debrief shows ladders.
5. **Aster**
   - Inputs: §4's last point, for Sky.
   - Next: Sky.

## What I read and ran
- **Read:**
  - Vantage's proposal and brief 101's decision.
  - GDD v0.39.0 §02, §04, §05, §07, §09 and §13.
  - Rulebook: `camera`, `privacy`, `exposure`, `alert`, `summon`, `ladders`, and the turning and `ladder.job` events.
  - Engine 0.51.0: `GameCameraRecord.cs`, `GameCall.cs`, `GameAttention.cs`, `GameInvestigation.cs`, `GameSight.cs`.
  - The host's `HostTape.cs`, the checker's `bands.ts`, and the adaptive plans.
- **Numbers:**
  - `runs/missions/handover/c063/run.json`, and its rows in `out/missions/handover/bands/rows.jsonl` (engine 0.51.0, 200 seeds a plan).
  - Split by the `camera.recorded` and `attention.reporting` marks, with a throwaway read-only Python script.
- **Checks:** none run, because nothing but this file changed.
