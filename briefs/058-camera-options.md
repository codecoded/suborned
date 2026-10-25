# 058 · Cameras: Loop, Disable, Shoot

**State:** designed 2026-10-24 (Motive). Not built. Sky decides first; nothing in the rulebook, sim, Unity or GDD changes before that.
**Inputs:** Sky playing 0.2.99, 2026-10-24: "I think Tech should be able to disable the cameras - everyone can shoot them" and "it's getting annoying having him having to stand at the camera to loop it, and then accidentally have him walk away and then the camera active again". Sky's 0.2.99 forecourt shot (the pill on the entrance camera, the "Camera dark" plate). Engine 0.29.0, rulebook `cell.loop`, `values.health-camera`, `witness.cameraScan`, the response dial. Mission check c034 (38/38). Pulse `briefs/013/pulse-witnesses.md` §3.2, §5. My `briefs/027/motive/tuning.md` §9.2.
**Probes:** `out/scratch/motive/058/` (scripts from `make.py`, results `results.jsonl`), run with `suborned-sim batch` on engine 0.29.0.
**Next owners:** Sky (approve) → Keel (rulebook + engine) → Waymark (routes) → Glyph and Fable (ring, pill, plates, words) → Latch (port). The stopgap in §5 can go to Latch now.

---

## 1 · What exists today (read from the code and the runs)

- **Loop** (`cell.loop`): Tech only, at the cabinet (within `at-reach`), 62 s to set, +1 T when it lands. Then it holds for as long as the Tech stays within `tether-m` 2 m of the cabinet; one step further and the feed comes back (`Game.Tether`). While it holds, the Tech can't run, Watch or board. The window marker shows it with no end (`OpenWindow("loop", cam, -1)`).
  - In every route that loops, the loop ends when the Tech walks off at the Enforcer's Enter (route A: on 1:18.5, ends 2:54.2, Enter 2:53.4). Route G's body by the door is then found by the entrance camera 0.3 s later. That find isn't what puts G at Alert 1: with the camera blind to the end, the guard's missed check-in mark gives the same Alert 1 (§4, `hold-*` probes).
  - Hold lengths the routes use: A 1:36, A-pressure 0:46, G 1:19, G-fast 1:01, G-kill 2:46.
- **Shooting a camera** is live in the Handover engine, not only in the fight lab. It has been in both presets since engine 0.15.0 (`GameShootAnyone.cs`, `health-camera` 1, scenarios in `ScenariosFireMove.cs` and `ScenariosNotice.cs`).
  - One hit and the camera is dark for the rest of the mission.
  - It is gunfire: Alert ≥ 2 at once, the response, and `gunfire-trail` +2 once for that target, hit or miss. There is no hit or down Trail and no Standing (`GameCombat.cs:460`).
  - Every armed operative can do it, by a fire order or the driven agent's trigger. Weapons free never picks a camera, which is correct because it isn't a combatant. Follow fire does take one.
- **The HUD, from Sky's shot:**
  - **The pill is wrong for cameras.** "Fire · +2/hit Trail · Alert ≥2" comes from `HudModel.Less.cs:88`, which prints `+HitTrail/hit` for every fire target. A camera takes no hit Trail. Its price is the shooting's +2, once, hit or miss, and +0 if the cell has already fired at it. With one hit to drop it, the number happens to match, but "/hit" tells the player the wrong rule. The panel's fire row has the opposite fault: it shows no Trail at all for a camera (`HandoverHost.cs:873`).
  - **"Camera dark" with the Tech's face is the Loop's done plate** (`HostDone.cs:281`), not a shot-out camera. "Dark" is the wrong word, because a looped camera shows the desk a clean picture. The words are below (§6).
  - **Who can fire from where.** In aim mode (the right-hold pill), any armed driven agent can fire on a camera. On the ring and the panel, only the Enforcer can: he is the only one with a Fire knob, and the others' `FireIntent` covers combatants only, never cameras (`HandoverHost.cs:1052`).

## 2 · Three ways to deal with a camera

The idea behind it: a camera is watched by a person at a desk who looks at each screen every 20 s. Each way fools, blinds or breaks that one link, and each one shows the desk something different.

| | Loop | Disable | Shoot |
|---|---|---|---|
| who | Tech | Tech | anyone armed |
| where | the cabinet | the cabinet | anywhere in range, on a line |
| time | **20 s**, then he is free | **10 s** | down in n s (one hit) |
| lasts | **2:00**, then the live feed returns on its own | the rest of the mission | the rest of the mission |
| Trail | +1 | **+2** | +2 once (the shooting) |
| Alert | none | **at the desk's next scan: a missed check-in.** The incident raises Alert to at least 1, the dial sends its Alert 1 response to the camera, and they check it and stand down 1:00 later | **at once: Alert ≥ 2.** The response unit comes, the seal clock starts (2:25 at S2), the rivals come within 16 s, the patrol breaks to it, Enter takes +30 s, and the final fight is loud |
| what the desk sees | an empty street on a loop | a blank screen | a blank screen, after the shot everyone heard |
| the order line | `Loop · 20 s · +1 · back in 2:00` | `Disable · 10 s · +2 · ▲1 in 14 s` | `Fire · 2 s · +2 · ▲≥2` |

### Loop: set it and leave
- The Tech records a clean stretch of the view and splices it into the feed (20 s at the cabinet). The tape then plays on its own for 2:00, and the live feed comes back when it runs out. A looped tape keeps playing whether or not anyone stands by it, so it needs no tether.
- **The tether goes:** no `tether-m`, no `run-tethered`, no `board-tethered`, no Watch refused while looping.
- **One loop at a time on the cabinet.** A Loop on a second camera while one runs is refused, with the time left ("Entrance loop runs 1:12 more"). Looping the camera that is already looping re-splices it: 20 s, +1 T, and a fresh 2:00 from when it lands. No stacking.
- **Why 2:00:** it matches the guard's call window (30 + 60 + 30 s), so the player can lay one window over the other. It covers what every route needs with one loop, with slack: the longest need is G-kill's 1:33 (§4). At 90 s, G-kill would need two loops, and its 2 T would become 3.
- **Why 20 s rather than 62 s:** the 62 s was the tape plus the stand-and-wait. Without the hold, the price is the splice. 20 s is still the longest thing the Tech does at the cabinet apart from Reroute. It is still a real cost once the plant has to be timed (see route A in §4).
- **What the player sees:** the window marker counts down from 2:00 over the camera, as the guard's and the patrol's do (brief 043). The cone draws as looped. At 10 s left, the marker turns to the warning state. When the feed returns, the feed says "The entrance loop ends".

### Disable: permanent but noticed
- The Tech cuts the camera's line at the cabinet (10 s). The camera is dead for the rest of the mission.
- **It is noticed through an existing rule, not a new system.** A dead screen is a missed check-in, the same as a staff post that misses its mark (§05, rulebook `security.response`). At the desk's next scan of that screen (`camera-scan-s` 20 s, phase from the camera's id) plus `post-call-s` 3 s, the report lands. Then:
  - it counts as a reported incident, so Alert rises to at least `byIncident[n−1]`: 1 if it is the first incident of the mission, 2 if anything has already been reported;
  - the dial sends its response for the Alert reached to the camera. At Alert 1 on S2 that is 2 robots, the patrol robot first when it is on its sweep and not rerouted;
  - they check the spot, hold it as watchers, and stand down `stand-down-inquiry-s` 60 s after the report;
  - like a missed check-in, it is an empty post with no body, so it holds no door.
- **+2 T:** a cut line is found and written up. It is physical evidence the investigators can pull, where a splice is only a gap on a tape (+1).
- **Why at the cabinet, not the camera:** the camera's line runs to the cabinet, so cutting it there is the real-world way in, and it keeps the Tech's machines in one place (§03). At the pole he would stand in the camera's own cone while he cut it.
- **What the player sees:**
  - the order line shows when the desk will notice ("▲1 in 14 s", read from the scan phase, as the engine reads a live camera's report);
  - the camera's cone goes out, and the camera's head shows a broken-eye mark;
  - when the report lands, the feed says "The desk sees a dead camera" and the robots walk to it on the street.
- **Why this is a real choice, not a better Loop:** Disable uses up the mission's first free incident. After it, the next report of anything (a found body, a woken guard, a missing person) is the second, which means Alert 2, with the search, the seal clock, Enter +30 s and the loud final fight. It also brings the robots to the very place you blinded, at a time you can see coming. So Disable is preparation: do it early, let the robots come and go, and the camera is gone for good. Loop is timing: plant it just before the act, and nobody ever knows.

### Shoot: instant and loud
- No change to the engine: anyone armed, one hit, dark for the rest of the mission, gunfire.
- The camera reports the shooter if he stood in its cone when he fired, so security then knows that face (rulebook `ai.intent` face).
- Its price is everything that comes with gunfire, which is right for the "it went wrong, blind it now" move.

## 3 · Everyone can shoot cameras

- **The rule holds** in the engine for every armed operative, in and out of a fight, and stays as it is. Nothing new is needed for the final fight, where cameras don't matter.
- **The HUD must close two gaps:**
  1. **The camera's ring offers Fire to every armed selected agent**, not only the Enforcer: the same harmful wedge with the 0.5 s hold. Glyph, kit colours only. The Tech's ring on a camera reads Loop · Disable · Fire. Anyone else's reads Fire, plus Watch where it is offered.
  2. **The price is right everywhere** (pill, panel row, aim chip, `HostFight` cost line). For a camera or a car: `+2` (the shooting) once, `+0` if the cell has already fired at that target, never `/hit`. The Alert part stays `▲≥2`. One function should build it from `PreviewFire`'s Trail parts (`TrailShooting`, `TrailHit`, `TrailDown`, already computed in `GameFightLab.cs:468`), so the pill and the engine can't drift apart again.
- Weapons free still never picks a camera. A camera is shot only on purpose.

## 4 · Handover balance

Measured on engine 0.29.0 with `suborned-sim batch` (§7). "Needs dark" is from the first act the camera must not see to Enter.

**What the camera has to miss, per route (c034, engine 0.29.0).** Ending the loop earlier (the Tech cancels it at tick X) shows the last moment the camera must be blind on each route. On every route that is the Enter, plus the takedown on the G routes. Any earlier end fails:
- route A: entry seen by camera at 2:53.4;
- G-fast: the takedown is seen, Alert 3, and the cell withdraws;
- G-kill: the dead guard is found (Alert 2) or the entry is seen.

**What permanent darkness alone buys:** nothing. Keeping the camera dark to the end with no report (the `hold-*` probes) gives every route the same result, time, Trail and Alert as c034. G's Alert 1 comes from the guard's missed check-in mark (`report:mark`, at 4:00 on G and G-kill, 2:30 on G-fast), not from the camera. So the price Disable adds (+1 T over Loop, and using up the first incident) is the whole difference between them.

| route (c034) | camera must be blind | Loop v2 as scripted (lands 0:36.5, ends 2:36.5) | Loop v2 retimed | Disable instead (early) | Shoot instead |
|---|---|---|---|---|---|
| **A** 4:59.7 · 5 T · A 1 | the Enforcer's wait beside the door (from 2:03.5) to Enter 2:53.4 | fails: ends 17 s before Enter | plant after Reroute (order 1:01.5, live 1:21.5–3:21.5): same time and Trail | **a real alternative:** 6 T, A 1. No later report on A, so the final fight stays safe. For a walking player it is A 0 → 1. | **fails** (probe `shoot-tech`): gunfire ends Inez's round, the patrol fights the shooter, and her call is refused ("security is searching: he won't leave"). Route A closes. |
| **A-pressure** 4:11.3 · 7 T | to Enter 2:03.6 | fits | — | as A | as A |
| **B** 8:07.9 · 3 T · A 0 | none: the turned driver hides the Take | — | — | +2 T, A 0 → 1, gains nothing | **fails** (probe `shoot-pickup-scout-b`): the draw is seen on the entrance camera (Alert 2) and the district seals at 3:21.9 |
| **G** 4:44.7 · 6 T · A 1 | takedown 2:31 to Enter 2:37 | fails: ends 0.5 s before Enter | plant ordered at 0:30 or later | **worse:** the 4:00 missed mark becomes the second incident, so Alert 2 and a loud final fight | loud |
| **G-fast** 4:26.2 · 6 T | takedown 2:12.5 to Enter 2:18.5 | fits | — | worse: the 2:30 mark becomes Alert 2 | loud |
| **G-kill** 6:11.7 · 2 T | takedown 2:31 to Enter 4:04 (1:33) | fails | plant ordered between 1:44 and 2:11 | worse: Alert 2 at 4:00, before Enter, so Enter takes 68 s and the final fight is loud | loud |
| **loud family** | Enter 2:37.7 | fails: 1.2 s short | as A | — | already loud |

**Verdict:**
- Loop v2 keeps every route's time, Trail and Alert once A, G, G-kill and the loud family plant later.
- Disable doesn't make Loop pointless. It is worse on every G route and on B, and it is an honest alternative only on A (+1 T, Alert 1, no timing).
- Shoot closes route A and route B outright, as loud should.
- **No route is trivialised and none opens** except `route-a-disable`. The pickup camera was already loopable (Sky's answer 6, 2026-10-16), and with no turned driver, a Take at the kerb is still seen by people.

## 5 · The stopgap for the walk-off (until §2 ships)

- **Where the walk-off comes from:** the Tech walks off the cabinet and the loop ends at once with no warning. That happens through a squad move with him selected, a click on the ground, or the move ring.
- **Stopgap, in the host only (no engine or rule change, replays untouched):**
  1. **A squad move leaves a Tech who holds a loop where he is.** His plate shows "Holding the loop". A move given to him alone still works.
  2. **A move that would take him off the cabinet takes the 0.5 s hold** that harmful orders already use, with the line "Camera comes back". A tap does nothing, and a held press walks him off.
- Owner: Latch (host), Glyph (the plate line and hold state), Fable (the words). This is worth doing now, because Loop v2 needs an engine release and route retimes.

## 6 · Handoff

### Keel: rulebook and engine (after Sky approves)
- **Values:**
  - `loop-s` 62 → **20**, still [S] proposed;
  - new `loop-run-s` **120**;
  - `tether-m` retired;
  - new `disable-s` **10**;
  - new `disable-trail` **2**;
  - `camera-scan-s` 20 and `post-call-s` 3 are read by Disable too.
- **`cell.loop`:** `{ s, trail, run, targets }`. A loop lands, runs `run`, and ends on its own (log `loop.ends`). Re-looping the same camera restarts `run` from the landing and charges `trail` again. A loop on another camera while one runs is refused `loop-holds`, with the tick it ends. `Game.Tether` and the refusals `run-tethered`, `board-tethered` and the tethered Watch go. `OpenWindow("loop", cam, until)` gets a real `until`. `cancel.loopHeld` goes, because there is no held loop to cancel. The Tech's cancel only stops a Loop still being set (`lost`).
- **New `cell.disable`:** `{ s, trail, targets: "v:cell.loop.targets" }`, Tech only, at the cabinet. When it lands, the camera is dead (the same `CameraDark` the shot sets, recorded as `cut`, not `shot`). The report is a missed check-in by the camera: it lands at the camera's next scan plus `post-call-s`, as a reported incident (`byIncident`). The dial dispatches its Alert response to the camera's place. It holds no scene. It is not gunfire, has no face and no Standing. Disabling a looping camera ends the loop. Disabling a dead camera is refused `camera-dead`.
- **Scenarios to add:**
  - **LOOP-RUN:** the loop lands, the Tech walks 30 m away, the camera stays looped until landing + 2:00, then sees again.
  - **LOOP-RE:** re-splicing at 1:50 runs to the new landing + 2:00, at +2 T in all.
  - **LOOP-ONE:** a second camera while one runs is refused, with the end tick.
  - **DIS-REPORT:** Disable at 0:20 → the report at the scan phase + 3 s → Alert 1 → 2 robots at the entrance camera → stand down 60 s later. The camera is dead to the end. On route G its missed mark at 4:00 is then Alert 2.
  - **DIS-SECOND:** Disable, then a knockout found → Alert 2.
  - **DIS-TWO:** two cameras disabled → Alert 2 at the second report.
  - **CAM-SHOT-PRICE:** a camera shot costs +2 T once, never hit Trail, and `PreviewFire` says the same.
  - **CAM-ANY:** Scout, Fixer and Tech each shoot the entrance camera dark.

### Waymark: routes
- **Retime** route A, A-board and the loud family: the Loop goes after Reroute (plant ordered at about 1:01, live 1:21 to 3:21, Enter at 2:53).
- G-kill: plant ordered between 1:44 and 2:11, so the loop lands before the takedown (2:31) and still runs at Enter (4:04).
- G: plant ordered at 0:30 or later (as scripted it would end 0.5 s before Enter). A-pressure and G-fast keep their order (§4).
- **New routes:**
  - `route-a-disable` (A with an early Disable in place of the Loop: expected 6 T, Alert 1, a quiet win counted under A);
  - `careless-g-disable` (G with Disable: the guard's missed mark becomes the second incident, Alert 2, loud);
  - `careless-loop-runs-out` (the loop ends before Enter → entry seen by camera).
- MC-1 should still count A, B and G as quiet wins.

### Glyph and Fable: HUD and words (plain copy: what, why, fix)
- **Ring on a camera:** Tech: Loop · Disable · Fire. Any other armed agent: Fire. Each is priced on one line, as in the table in §2.
- **Done plates:**
  - Loop: "Camera looped" with the 2:00 countdown;
  - Disable: "Camera cut";
  - Shoot: "Camera shot out";
  - never "Camera dark" for a loop.
- **Feed lines:**
  - "The entrance loop ends" (exists);
  - "The desk sees a dead camera. Robots are on the way.";
  - a refusal: "One loop at a time. The entrance loop runs 1:12 more."
- **Fix now:** the pill and the panel price for a camera or car shot (§3, 2).

### GDD lines that change (Aster, after Sky approves; I don't edit the GDD)
- §03 Tech row: verbs "Loop, Disable, Reroute"; "tethered while holding" goes; the all-ops line "while tethered" goes.
- §02 Time: "actions 3–62 s" becomes "3–50 s" (the longest is then Pay or Help, 50 s; Enter at Alert ≥ 2 stays 68 s, as §07 says).
- §05 cameras: a camera can be looped (2:00), disabled (a missed check-in at its next scan) or shot (gunfire); the running line's "while tethered" goes.
- §07 route A: "Tech loops entrance camera (1:02, tethered; …)" becomes "(20 s, runs 2:00)"; route G gains the Disable variant; the reference results row is re-recorded by Waymark.

## 7 · Probe log

- **Where:** `out/scratch/motive/058/` (git-ignored). `make.py` writes 52 scripts from c034's routes, and `read.py` reads `results.jsonl`.
- **The run:** `bun run guard --wait-timeout 3600 dotnet out/missions/engine/tree/bin/suborned-sim.dll batch out/scratch/motive/058/scripts --no-replay --jobs 2 --out out/scratch/motive/058/results.jsonl` → "batch: 52 played in 1.9 s on 2 threads, no replay check; 0 not ok". The first try waited out its 20 min guard while Sky played and ran nothing.
- **`shoot-{scout,fixer,tech,enforcer}`:** route A, with the Loop replaced by a fire order on `camera:entrance` at 0:16.5.
  - Tech: hit at 6 m, 0:18.5.
  - Fixer: walked in and hit at 14 m, 0:52.2.
  - Enforcer: hit at 27 m, 0:20.5.
  - Each is "+2 gunfire:camera:entrance" once, no hit Trail, Alert 2.
  - The Fixer's draw was also seen by people: +1 T and −1 Standing.
  - The Scout's order was refused because she was busy watching Inez. That is not a gap in the rule.
- **`shoot-pickup-scout-b`:** the Scout drops the pickup camera at 14 m on route B.
- **`end-<route>-<tick>`:** the Tech cancels the held loop at that tick, after his Reroute.
- **`hold-<route>`:** the Tech never leaves, so the camera stays blind with no report.

## 8 · Open questions for Sky

1. **How long does a loop run?** **Pick 2:00.** It matches the guard's call window and covers every route with one plant. At 1:30, G-kill needs two plants (2 T → 3 T).
2. **Does Disable count as an incident, or only as a found fault?** **Pick incident.** The `hold-*` probes show that darkness alone costs nothing on today's routes. As a found fault, Disable would be Loop with no timing for +1 T, and nobody would loop. As an incident, it uses up the free first report, and that is the choice.
3. **Ship the walk-off stopgap now (§5)?** **Pick yes.** It is host-only, changes no replay, and fixes what annoyed Sky while Loop v2 waits on Keel and Waymark.

## Sky's calls (2026-10-25)
Sky, 2026-10-25, on the outstanding-work summary that listed each pick: "Your recommendation seem good".
1. A loop runs **2:00**.
2. Disable counts as an **incident**.
3. Ship the walk-off stopgap (§5) **now**.
Next: Latch the stopgap (host only, next batch); Keel rulebook and engine; Waymark routes; Glyph and Fable ring, pill, plates and words; Latch the port; Aster the GDD lines (§7).
