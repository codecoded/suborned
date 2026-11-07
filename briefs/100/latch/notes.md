# Brief 100 · the why: Latch's build notes

Latch, 2026-11-07, on `work/098-living-playbook` (uncommitted; Aster commits after review). Sky, playing: "It would be good to explain why an alert raised, or why police came". The words are Fable's `why` block in text.json, read by key: no line is copied into code.

## What the host does

**The why of every rise and call** (`HostWhy.cs`, `HostWhy.Names.cs`, `PlayText.Why.cs`)
- Once a tick (from `CrucialTrack`, before its cards), one row for each new `Game.Ledger` "alert" row and each police call in the log.
- **An Alert rise**: keyed by the ledger cause through `PlayText.WhyKeyOf` (a port of why.schema.ts's `whyKeyOf`). The fills come from the host's own reads, never the engine's sentences:
  - the report that landed on that tick (`Game.Reps`, the loiterer, car and camera reports, the cut screens, the entry report, compared with the last tick: `HostWhy.Reports.cs`);
  - the round that raised gunfire (`Game.ShotsThisTick`): the cell's shot is "{agent} fired at {about}", anyone else's is variant `other`;
  - the act's place (`TdAct.Scene`), or the reporter's or subject's place when the report was made;
  - who of a takedown's watchers noticed it on that tick (`TdAct.Noticed`, compared with the last tick), for a security watcher who lands it with no report in transit.
- **A police call**: keyed from structured state on the tick its line was logged:
  - the live seal origin (`Game.SealOrigin`, its `Tick`): gunfire heard, seen, at a civilian, at the patrol or at the rivals; an attack; a drawn gun; an empty post;
  - a new scene hold (a body found);
  - `GangPending`; `alarm.dispatches` at Alert 1 with no unit called (robots);
  - the codes for more police, the lockdown, the loud fight, the split and the seal.
  - `report` and `robots` take {cause} from the rise of the same tick, else from the report that landed.
- **Fills**:
  - civilians by job (`PersonName` now reads `names.targets["crowd-" + job]` before `crowd`: "a clerk", "a local");
  - `by.another` when reporter and subject share a job plate ("another guard spotted the guard lying");
  - {where} is the nearest of `why.places` within 13 m, else "nearby";
  - {agent} is "the" + the job word ("the Enforcer").
  - A text left with a fill the host couldn't fill (or filled empty) is dropped, never shown half-done.
- **Orders**: an order given between steps logs its consequences at its own tick (the failed takedown's "the guard calls in an attack"). Every read uses the line's or row's own tick, not the clock now.
- **No cause, no words**: a cause Fable has no key for keeps its row (its time and Alert in the meter) with no words.
- **Where the why shows**:
  - The **Alert card** keeps "Alert up" and its lamps; its dim line is the cause's short and place ("shots fired at the Office door · 2:33"). A click on the line pans to where it happened.
  - The **Police card** takes the call's own title ("Police coming", "Robots coming", "More police", "Police coming in", "Lockdown"); its line is the cause, then the card's own "here in {s}"; a click pans to the cause's place. When several calls share a tick, the card takes the one that says why they come (a body found), not the lockdown it brings.
  - The **Journal** row of both cards takes the cause in a sentence, `again` when the count of reports raised it, and what the Alert set off (`effect`). The city's-answer cards keep their own results (brief 013): the why rides beside them.

**The report on its way** (`HostWhy.Reports.cs`, `HudToolkit.OnWay.cs`)
- Every report still to land is a mark, rebuilt each tick from the same lists.
- Ways: radio, walk or run (to the report point the walker heads for: {to}), look (an Idealist checks first: no seconds yet), tape (a camera's record or a watcher's tape), dead screen (a camera the Tech cut), robot (the patrol or the bin round ringing the desk), tell.
- Sky's call: a camera's tape is marked in flight only for a camera the cell has learned (`Game.Known(camera).Sight`).
- In the street, over whoever carries it: a small plate_dark (30 px, the seen count's plate) with the way's icon and the seconds to its landing. Reporters at one spot (clerks setting off together) stack their plates, none hidden. While the cell can still stop it, the plate wears the HUD's act ring (turquoise-light, 1.5 px); a tape or a dead screen has none.
- Hovering the plate, or the reporter's head under it, shows Fable's plate over its fix line ("Going to the Office door · 8 s" / "Stop them before it lands").
- The plates take no clicks, so the reporter stays clickable.
- A marked report that never lands pops `onWay.stopped.plate` ("Report stopped") where the reporter is, its line kept for Alt.
- A takedown stops a report the moment it starts (the reporter can't report while being taken down). So even a staff radio (5 s) can be stopped by an Enforcer close enough.

**The meter remembers** (`HudToolkit.Why.cs`)
- The pill's three Alert lamps take a hit box: the pill's height, 6 px either side. A hover shows the list; a click pins it (a click on the lamps or the list's head folds it).
- Under the pill, as the response drawer (panel_dark, 318 wide):
  - the head ("Why it's Alert 2", or "Nobody has raised the alarm");
  - up to six rows, newest first, `when` (dim) over `what`. Every rise is listed. A police call is listed unless it only repeats the rise of its tick or the call before it.
  - A click on a row pans there; the hovered row's sentence shows under the rows.
- Shots can pin it (`why: true` in shots.json, wired as `eye`). Such a frame is judged by the HUD budget as the history was (`log`): a reading surface the player opened.

## Verified

- **Host check** (plain dotnet, `bun tools/game/hostlogic.ts Why100`): 8/8.
  - Fable's key port.
  - Every `why.places` id is the site's own.
  - Civilians are named by job.
  - The loud pin: every rise is the ledger's own and worded; the calls say why; the cards carry their cause; the meter is newest first with every rise. The engine alone, from the pin's start and orders, ends on the host's closing hash, and the saved run replays.
  - Every route and pin on its own seed (45 runs: 43 rises, 107 calls): rows match the ledger, nothing throws, nothing is half-filled.
  - The loud pin's walkers, looker and radios are marked with Fable's plates. Every marked tape is a learned camera's.
  - The quiet radio counts 5 to 1 and lands as "another guard spotted the guard lying at the Office door".
  - The stopped radio never lands, pops once, and the run keeps its hash.
- The loud pin's rows match Fable's worked example line for line:
  - 2:33 Alert 2 "shots fired at the Office door";
  - 2:33 Police coming, "shots heard at the Office door";
  - the seal's cause "shots fired", sealing at 5:41;
  - 2:44 "a body was found at the Office door";
  - 3:45 "shots earlier";
  - 3:56 and 4:36 More police.
- The quiet runs are scripts here: `route-g-radio.json` and `route-g-radio-stopped.json` (route G's knockout, the Enforcer kept by the door). In the stopped run the takedown is itself on the entrance camera's tape, so the Alert still rises at 3:34, and the meter says so ("the entrance camera saw a takedown at the Office steps").
- The rows the check reads are written to `out/batch/hostlogic/why100/`.

**In the player** (scratch build 0.2.263-scratch, `out/scratch/latch/100/player`; shots by `briefs/100/latch/shots.json`, written to `briefs/100/latch/shots/`, git-ignored):
- `why-loud-police.png` (2:34): the Police card, "Police coming → shots heard at the Office door · here in ? · 2:33".
- `why-loud-meter.png` (4:39): the meter's list, from "4:36 · More police / the search goes on" down to "2:33 · Alert 2 / shots fired at the Office door".
- `why-loud-walkers.png` (4:55): the clerks walking to the Office door, two stacked "7 s" plates with the act ring.
- `why-quiet-radio.png` (3:28.6): the shift guard's radio plate, "4 s", ringed, over his own "!" cue.
- `why-quiet-radio-lands.png` (3:33): it landed. "Police coming → a body was found at the Office door"; the Alert card is queued behind it (police rank first).
- `why-quiet-stopped.png` (3:31): "✓ Report stopped" at the Enforcer's takedown.
- `why-quiet-stopped-meter.png` (3:36): the Alert rose anyway from the entrance camera's tape, and the list says so ("the entrance camera saw a takedown at the Office steps").
- Clips (frames, 10 fps):
  - `why-loud-clip/` (160 frames): the 2:33 shot and its cards coming up.
  - `why-quiet-clip/` (200 frames): the radio counting down, the Enforcer running in, Report stopped.
- **Checks**:
  - Build: 0 errors.
  - Shots: 9/9, no exceptions in the player log; layout audit 7/7 clean.
  - HUD budget: 4/7. The three that fail do so only on "2 card panels on screen": the seal plate is registered as a card, as the crucial card is. The last batch's own `fight-loud-body` fails the same, so it predates this round (brief 101's sweep has the seal card).
- **Also run**: host logic 157/157 (149 before, plus 8), typecheck clean, the TS tests these edits touch 275/275 (proof map, shots, names, text, host logic, compile), the `words` step.

## Gaps for Keel (a clean alert-history read after law L9)

1. **Police calls carry no cause of their own.** The host keys a call from the live seal origin on its tick and from a new scene hold. A call that moves no seal can't be keyed and shows as a bare "report": a gang shooting filed earlier, or a call whose arrival is no earlier.
2. **`alarm.dispatches`: robots or police** is only in its sentence. The host infers robots from Alert < 2 and no unit called yet.
3. **No log line for `drawn-warned` or a Menace `sighting`.** They raise the Alert, and the host words them from the ledger alone. The drop-it warning (2 s to holster) has no read of its own, so it isn't marked on its way.
4. **A camera's drawn-weapon report** ("The watchers on the camera report a drawn weapon") doesn't say which camera. The ledger cause and the seal origin do, so the host names it; the log sentence still doesn't.
5. **`report:<kind>:<victim>` doesn't name the reporter.**
   - The host finds them by watching `Game.Reps` change phase.
   - A security watcher lands its own sighting with no report in transit (`SecuritySees`). For that the host watches `TdAct.Noticed`, a sorted set, so it compares it with the last tick's.
   - A "landed by" in the history read would end both.
6. **`gunfire:<x>` names the target or the shooter.** The host reads `ShotsThisTick` on the rise's tick to tell them apart. A cause that names both would be cleaner.
7. **A downed staff member's next check-in** has no public read, so Fable's `checkin` way ("Check-in due · {s} s") is never marked.
8. **Reports with no `report.started`** (a woken victim, a missing person reported by staff, a told Loyal friend, a hidden victim's noise heard by staff): closed for the host, which reads `Game.Reps` directly and marks them all.
9. **The tick a row happened.** An order given between steps logs its consequences at its own tick and is read at the next. The host now uses each line's own tick. A history read with a tick per row needs no such care.

## Open for Glyph (the look, after brief 099)

- The **Police card** now takes the call's own title. The **Alert card** keeps "Alert up" and its lamps, with the cause as its dim line. That line can now wrap to a second line, and the card grows with it.
- The **meter's list** hangs under the pill like the response drawer and covers Intel while open. Six rows; the hovered row's sentence under them.
- **The reporter's mark**: the way icons (radio, s_moving, run, eye_open, watch, disable, call, s_calling); the act ring for "can still stop it"; the 26 px plate placed 2.6 m over a person and 1.2 m over a camera.
  - The hover tip is not in the HUD manifest: a hover read, like the pill's Next tip. No budget kind fits two lines.
  - Off screen there is no mark.
- **"Report stopped"** uses the refusal pop at the reporter (check icon, Ok tone).
- **"here in ?"**: the Police card's own line shows "?" until the response's arrival is known. With the cause ahead of it, it now wraps ("… · here / in ? · 2:33").
- **The budget's one-card rule**: the seal plate and a crucial card are both "card" panels, so any loud frame fails it (brief 101).
- **A camera's tape plate** sits 1.2 m over the camera's view root, under its head.
- **The seal plate's hover** could show the seal's cause. The host keeps it (rows with `What` "seal"), but it isn't wired to `HudToolkit.Seal.cs`.

## Open for Fable (the words)

- **`cut`**: the short reads oddly with a place ("the desk saw the entrance camera go dead by the control cabinet"), so the host shows `cut` without {where}, as the worked example does.
- **`report` and `robots`**: their short is "{cause}", the rise of the same tick. So the meter skips such a call when a rise shares its tick; the card still shows it.
- **The seal's {cause}** for an attack or an empty post is the rise of the same tick ("the guard saw the Enforcer coming").
- **The marks**: `onWay.line` and `stopped.line` are kept for Alt and the hover. In play the plate shows only its icon and seconds, and Fable's plate words show on hover.
- **The `checkin` way** isn't used yet (gap 7).
- **Re-scoring**: the keys are read live from text.json. A re-scored line shows from the next copy (`bun tools/game/words.ts`); a new key needs a host case only if it is a new cause.

## Side fixes

- `PlayText.cs` was 615 lines. Its debrief reading moved to `PlayText.Cause.cs` (`ReadDebrief`); it is now 601, with one line for `ReadWhy`.
- `HostTraffic.cs:19`: a tooltip said "Vale's car", a banned name. It failed the `words` step's name check, so text.json couldn't reach StreamingAssets. It now says "the official car".
- `ShotRunner.cs`:
  - A clip now plays its script on: the orders still to come are given at their ticks through `HandoverHost.BeforeStep`. Before, a clip stopped feeding orders at its first frame, so the quiet clip's stop never happened.
  - Its note was wrong: the host steps by real frame time (about 26 ms a frame while frames are written), not 1/fps. A 20 s clip covers about 5 s of game.
- `tools/game/shots.schema.ts`, `shots.ts`: the `why` option.
- `tools/game/proofmap.json`: one row for the new files (the crucial card's sections and pictures).
