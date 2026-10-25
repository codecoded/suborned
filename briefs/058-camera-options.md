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

## Glyph (2026-10-25)
Spec for Latch, in the Suborned HUD kit (no lab look; every piece is a kit sprite, a kit.uss colour or a kit icon). Boards, over the Oct 25 office-front capture at play zoom (zoom 14, 38 degrees, yaw 210), 2 px per game px, placement indicative (Latch projects): `briefs/058/board-01-ring-on-a-camera.png` · `board-02-camera-states.png` · `board-03-done-plates.png` · `board-04-walkoff-stopgap.png` · `board-05-price-lines.png`. Redraw with `python3 -I briefs/058/boards.py` (library `briefs/058/glib.py`; numbers in `briefs/058/budget.json`). Sky's calls above stand (2:00 loop, Disable an incident, stopgap now).

### 0 · Words: Fable's, none of mine
All from `contracts/missions/handover/text.json` (Fable's keys, already there): `less.price.verbs.loop` [name, time, trail] · `.disable` [name, time, trail, alert] · `.fire` [name, trail, alert] with `price.examples.fire-camera` ("Fire · +2 · ▲≥2": no time, never "/hit") · `less.done.gain.camera-looped` / `camera-cut` / `camera-shot` · `less.done.close.camera` ("Camera on", the pop when a loop ends) · `activity.holding.loop` ("Holding the loop": the plate's hover and Alt label; the plate itself shows icons) · `less.hold.loop-walk-off.short` ("Camera comes back", the hold pill's body; `.line` is its Alt). I wrote no sentence. Where a board prints a word it is one of these keys.

### 1 · The ring on a camera (board 01)
- **Who gets what.** `HandoverHost.RingState` for a `camera/*` target: the Tech: **Loop · Disable · Fire**; any other armed agent: **Fire** alone (so the ring shows even with one way: `n = 1`). Order by `ContextRank` with the harmless ways first and Fire last; Loop is the default `Lit` (12 o'clock). Weapons free never picks a camera (unchanged).
- **Wedges (HudToolkit.Ring.cs, as built).** n = 3 puts the wedges at 0°, 120°, 240° (Loop top, Disable lower right, Fire lower left); the gap is 2.2°. n = 1 is the whole ring: `a0 = 0, a1 = 359.99`, no gap (check the closed `Arc` draws; if not, two 180° arcs), the icon at 12 o'clock on the mid radius, the pill centred above (`RingFrame` already places a `|dir.x| <= 0.2` pill above). Icons 22 px at the mid radius (57): `loop`, `disable` (new, below), `fire`; cream, the lit one `--turquoise-light`. **Fire is `Violent`**: dashed outline (as built), the `bang` loud mark (14 px) at the end of its pill, and the 0.5 s hold fills the wedge from the middle out in turquoise-light at 75 % (board 01, middle). Loop and Disable are one click; a tap on Fire does nothing.
- **Price pills, one line each** (`HudModel.PriceLine`, fields in Fable's `price.order`): **Loop** time (walk + the 20 s act, e.g. 38 s) · Trail +1. **Disable** time (walk + the 10 s act, e.g. 28 s) · Trail +2 · the siren with 1 (Alert 1 on the first report, 2 on the second: the number comes from the engine's `byIncident`, never typed). **Fire at a camera or a car** Trail +2 · the siren with ≥2 · the loud mark: no time, no "/hit". Fire's Trail for a camera is `TrailShooting` once, 0 if the cell has already fired at it, so one function builds it from `PreviewFire`'s parts (`GameFightLab.cs:468`) for the ring pill, the panel row, the aim chip and `HostFight`'s cost line (§3.2 above). Fire at a person keeps "+n/hit" (it is per hit there).
- **Hover marks.** The hovered wedge names the camera: `RingFocusFrame` as built (the turquoise-light ring on the head, its ground ring at the foot). The cone it reads from is drawn at its state (§2).
- **Alt** (board 05, bottom two): walk split from act, every field including zeros and the eye ("none"); the same icons.
- **Widths** (game px, measured): Loop 148 · Disable 198 · Fire 132 · Alt Loop 295 · Alt Disable 308, against the shipped Pressure 315. Nothing here grows the ring.
- **Optional, not in Fable's keys, for Sky/Aster/Fable to say yes or no (board 05, rows 4 and 5):** Motive's order lines also carried the loop's run time and the desk's delay: `Loop · 38 s · +1 · [loop icon] 2:00` (206 px) and `Disable · 28 s · +2 · siren 1 · [nextevent, dim] 14 s` (244 px). They need two new price fields (`lasts`, `in`) and read as more to learn on the ring, against "less reading". I recommend the shipped lines: the 2:00 shows the moment it lands (chip and marker, §3), and the 14 s is the desk marker (§2). If Sky wants them on the ring, they are drawn and measured.

### 2 · What a camera looks like (board 02)
- **Live, learned** (a learned cone at rest): unchanged from brief 057 §3 (red, 13 % fading to 1 %, rim 55 %, dashed far edge).
- **Looped:** the same cone, **hollow and dashed all round**: no fill, rim `0.05 m` at 50 %, both side edges and the far edge dashed (5 px on, 4 px off at play zoom, i.e. 0.13 m on, 0.10 m off). Recorded, not live: own shape cue, still red, still drawn unlit and emissive outside the grade. In `HostViews` the fan gets a `Looped` bool; the existing `FillAlpha` goes to 0 and the two side rims take the far edge's dash. The marker on the head is the existing window marker (§3): `eye_shut` and the count.
- **Cut** (Disable): the cone is gone. A **dead disc** stays on the camera to the end of the mission: `markers/disc_dark_24` at 24 px, the new `icons/singles/disable` at 15 px in cream, centred 20 px above the head's projected point (head = pole top; the disc sits clear of the window marker, which stays where it is built, at the anchor +0.75 m). Until the desk reads the screen, a second marker: the **desk's clock**, the as-built window marker with the `p_alert` icon and the seconds to the desk's report (scan phase + `post-call-s`; the underline drains over the scan interval, amber and blinking in its last 10 s as built). At the report the clock goes; the disc stays; the feed beat is Fable's.
- **Shot out:** the cone is gone; the dead disc is `markers/disc_light_24` with `fire` in ink at 15 px (harm's own pair, the art bible's "harm reads by shape and the light disc"); no clock, because the gunfire already raised Alert. Persistent like the cut one.
- **Sources of state** (host, no engine change needed to draw): cut and shot are `CameraDark` with the cause recorded (Keel: `cut` vs `shot`, brief 058 §6); until that lands the host can tell them apart by which order darkened the camera.
- **Budget:** a dead disc is 24 × 24 px; at most three cameras; nothing at rest beyond that.

### 3 · The done plates and the 2:00 (board 03)
- **Loop** (`HostDone.cs:287`, already `gain.camera-looped`): the chip as built (panel_dark at slice 0.5, the Tech's face 32 px in the chrome ring with the tick badge `mark_ok`, one row: `loop` icon 16 px + "Camera looped" in Jost Bold 15), 4.2 s. **The 2:00 rides the window marker under it** (Fable's rule, kept): `OpenWindow("loop", cam, until)` now has a real `until`; `HostDone.cs:362` stops setting `m.Face` for the loop (the Tech's face stood for "no end"; there is an end now), so the marker shows the countdown like every other window.
- **Window marker label format:** m:ss when the window's `Total` is 60 s or longer (the loop: 2:00, 1:12, 0:09), plain seconds under that (the guard's and patrol's, the desk's: 14, 3). One format helper in `HudToolkit.Done.cs` where `Text(wn, w.Left.ToString())` is today. The underline and the amber-and-blink in the last 10 s are as built. The marker icon is `eye_shut` for a loop, `p_alert` for the desk's clock; its kind is `loop` or `desk`.
- **Disable:** chip with the Tech's face, `disable` icon + "Camera cut" (`gain.camera-cut`); the desk's clock marker under it (§2).
- **Shoot:** chip with the shooter's face, `fire` icon + "Camera shot out" (`gain.camera-shot`); no marker.
- **Never "Camera dark" for a loop.** When the loop ends the host pop is `close.camera` ("Camera on") as built.
- **Lines:** each chip is one row; a chip's second row is not used (the engine numbers live on the markers).

### 4 · The walk-off stopgap (board 04)
- **The Tech's plate.** The roster's activity plate (layout.json `timer-n`, 176 × 36 at the Tech's row; the board draws it 172 × 30 because the shot's own was taken out first) takes a **holding** state while the Tech holds a loop (`activity.holding.loop`): `plate_dark`, `s_looping` 15 px cream at the left (17 px in), a still **full** bar (track ink-deep at 63 %, 6 px, fill `--turquoise-light` at 59 %: nothing counts down, because the loop has no end until Loop v2), and the `lock` icon 14 px at the right (17 px in). No number, no word on the plate (the words are its hover and Alt label).
- **A squad move that leaves him:** the plate takes a 2 px `--turquoise-light` rim, 3 px outside the plate, for 0.6 s (ease out) once per squad move. His portrait and the rest of the roster do not change. A move given to him alone still walks him (the hold below).
- **The hold.** A move that would take the Tech off the cabinet is a harm-free **costly** order: it takes the 0.5 s hold the harmful orders use (the same `Charge` machinery), but draws on the plain `pill_status` (not the chamfered strip: it is a loss, not a harm). The pill, as built (`PillLook`): `disc_dark_24`-scaled disc with `loop` at the left, the dim head word ("Hold", then "Let go" in `--turquoise-light` when full), the body = `less.hold.loop-walk-off.short` ("Camera comes back"), the Tech's face at the end (24 px cameo); offer, charge (the turquoise-light sweep at 40 % inside the pill's window), ready (2 px ring, glow, scale 1.05). Anchor: the destination ring (the walk's solid cream ring, 0.5 m), the tail 22 px above it. A ground click has no `Target`: use the world point +1.5 m as `HoldPillFrame` does for a thing without a height. A tap does nothing; a held press walks him off and the camera comes back at once.
- **The cabinet:** where he stands keeps its solid ring (0.5 m), with the plan's dashed line to the destination as every ghost path (cream over ink-deep).

### 5 · The new kit asset
`unity/Assets/Suborned/UI/Resources/Kit/icons/singles/disable@2x.png` (+ `.meta`, new guid; 64 px, white on alpha like the other singles, scissors cutting at 45 degrees, solid, about 8 % stroke) and the same file with a 32 px `disable@1x.png` in `briefs/011/ui/kit/icons/singles/` (the kit source). `Icon(el, "disable", tint)` loads it by path; nothing else registers it. If the importer regenerates `kit.uss`, add `disable` to the icon list in `briefs/011/ui/kit` so `.i-disable` appears; the HUD does not read that class.

### 6 · HUD budget (tools/hud-budget rule: rest ≤ 15 %, 22 % with a card or the ring open, 30 % with Alt)
The capture's own HUD at rest is 6.66 % of the screen (`office-front-yaw210.hud.png`). Added on top: the ring open 8.27 % (Tech) and 7.90 % (one wedge); Fire held 8.27 %; a chip with its marker 7.15 %; the dead disc 6.67 to 6.69 %; the markers strip 7.09 %; the walk-off pill 7.22 % (offer, charge) and 7.64 % (ready, with its glow); the roster plate 6.66 to 6.71 %. The red cone's fill is a world mark (9.87 % counting it). Largest state 8.27 %: far inside every limit. At most two icons over a person: unchanged (the camera carries a disc and a marker).

### 7 · Done-check and open
- Done: the five boards; budget numbers; the icon installed (path above, `.meta` present). Not done: Unity (Latch), words (Fable has his), a 2160 shot of the ring open on a camera for the 2x review (nothing in `out/batch/shots` has one): Latch, please capture it, Glyph reviews the crop.
- Open: the optional price fields (§1); the dead disc's cause (cut vs shot) is host-only until Keel records it; the board's camera is the entrance one, the mast camera sits higher, so the dead disc's 20 px offset is "above the projected head" not a fixed number.
- **Next owner: Latch** (ring, states, chips, marker format, plate, hold pill); Fable has the words; Keel the `cut`/`shot` cause.

## Motive (2026-10-25): one robot at the entrance camera — accepted (a)
**Question (Aster, engine 0.31.0):** a Disable at the entrance camera brings only the patrol, not the 2 robots §2 said. That's because the block's one dock (`civic-robot-dock-315`, in the square) is 75.4 m from the camera and robots go only to places within 60 m of where they start (§05; `robot-reach-m` 60).
**Ruling: (a) accept.** The reach rule is the dial doing what it was designed to do, and it already has its own scenarios (N59, and the dock "too far from the bay" check in `ScenariosTakedown.cs`). Keel's DIS-REPORT already expects "the robots within reach, the patrol first". The "2 robots" in §2 and in §6 DIS-REPORT was my number and I was wrong: the dial's Alert-1 response at S2 is 2 robots *within reach*, and at the entrance camera only the patrol is in reach. Nothing Sky approved changes: his call was that Disable is an incident (it is, and it brings the dial's response to the camera). He never set the robot count.

**Measured** (engine 0.31.0, dll copied to scratch, sha256 `7d4623cf…`; rulebook `1bdcbdc3…`; scripts built from c036 routes). Both trees ran with `bun tools/game/playing.ts dotnet …/suborned-sim.dll batch …/scripts --jobs 2`, giving "9 played in 1.0 s; 0 not ok". The counterfactual tree differs only in `robot-reach-m` 100, which lets the dock robot come, so it gets the 2 robots §2 expected.

| script | 1 robot (live rule) | 2 robots (reach 100) |
|---|---|---|
| `route-a-disable` (Disable 0:16.5) | success 4:59.7 · 6 T · A 1 · Standing 3 | the same |
| `careless-g-disable` (Disable 0:16.5) | success 4:44.7 · 12 T · A 2 · loud final fight | the same |
| `route-a-disable-late` (Disable 2:14, after the Reroute) | 6 T · A 1 · **nobody comes** ("0 robots, 0 units") | 7 T: a dock robot sees the car leave (+1 `car-seen`) |
| references: `route-a-loop-retimed` / `route-g-loop-retimed` | 5 T · A 1 / 6 T · A 1 | the same |

- **route-a-disable:** the report lands at 0:37.6 and the patrol leaves its sweep at 0:41.6. It becomes wary of the Tech at the cabinet (1:01.6), stands down at 1:37.6 and is back on its sweep at 1:52, so the Reroute still lands at 2:03.5. Alert 1 had already come from the guard seeing the run at 0:02.8, so the cut adds no Alert. That matches §4's expected 6 T, A 1. A second robot from the dock (75 m at 0.85 m/s, about 90 s on foot) is called back by the stand-down before it matters.
- **careless-g-disable:** the patrol at the pole reports the Tech loitering (1:29.6: a found fault, no Trail). The guard's missed 4:00 mark is the second incident, giving Alert 2, the response unit and a loud fight. This matches §4's "worse". The dock robot in the reach-100 tree only notices the Scout and the Enforcer and reports nothing.
- **Per camera on this block (live rule):** entrance → the patrol only; pickup → the patrol only; mast-314 → 2 robots from the dock beside it. The dial reads the same everywhere. The count follows where the docks stand.

**Why not (b):**
- One robot changes no result on either route.
- Raising the reach or adding a dock near the forecourt only makes the late Disable dearer (+1 T) and puts a second robot where the player isn't looking.
- The robot that does come is the patrol the player has already watched on its sweep, so the response stays something they can see coming.

**Open, for others:**
- **Fable:** after the Reroute a Disable sends nobody, yet `camera.cut-reported` says "Someone is coming to look" and the Scout says "Robots coming". Those lines need a version for a dispatch of 0 (the engine logs the count in `alarm.dispatches`).
- **Waymark:** use the early Disable (0:16.5) for `route-a-disable`. The late one gives the same price, so it isn't a separate route.
- **Keel:** nothing for 059 from this ruling.
- Read §2's "2 robots" as "the dial's Alert-1 robots within 60 m (here, the patrol)". I've left §2's text as Sky approved it. The GDD §05 Disable line already says "the dial's response", so it needs no change.
- Probes: `out/scratch/motive/058b/` (`make.py`, `read.py`, `logs.py`; `base.jsonl`, `reach.jsonl`).

## Waymark (2026-10-25): routes on engine 0.31.0
Evidence: `briefs/058/waymark/route-retime-0.31.json` (every script against c036, the conditions, the slack). Record: c037, SHIPS, 41 scripts, 1711 plays, replays exactly. Re-recorded 2026-10-25 on batch 0.2.118's live site: rival-park held at (84.0, −31.0) in mission.json and `handover.site.json` (site sha256 a897dbdf…), after Splice's 061 round; the 059 data is in `handover.stands.json` and `tools/sim/stands.ts`, for Keel's 059 round. The first c037, taken with rival-park at (90.5, −31.0), is withdrawn.
- **Retimed, each to c036's end, Trail, Alert and Standing:**
  - route A and A-board plant the loop at 1:25 (live 1:45 to 3:45), then Reroute (patrol away 2:30 to 4:25). Rerouting first, as §6 says, brings the patrol back at 2:56.5, 3 s after the Enter: A-board's slack falls from 7 s to 3 s. This order keeps 7 s (the Inez limit), and 51 s from the wait by the door.
  - the loud family plants at 1:00, then Reroute (patrol away 2:05 to 4:00, as c036).
  - G and G-board plant at 1:00 (live 1:20 to 3:20).
  - G-kill and G-kill-board reroute at 1:25, not 1:46, and plant at 2:10 (live 2:30 to 4:30). Otherwise the Reroute holds the Tech through the 1:44 to 2:11 window.
  - fail-entry-seen-patrol plants at 2:40.
  - careless-guard-left-door-held: walking off no longer ends a loop. The Enforcer now enters at 3:12.6, after the loop has run out with the guard at the door.
- **Moved by the recompose, not retimed:** A-pressure and G-fast keep their order (loop, then Reroute), but the loop now holds the Tech for 20 s, not 62 s:
  - A-pressure-board 3:38.1 → 3:20.5;
  - G-fast-board 3:53 → 3:11;
  - same Trail, Alert and Standing.
- **New:**
  - `route-a-disable`: quiet, under A. Disable at 0:16.5, 4:59.7, 6 T, A 1.
  - `careless-g-disable`: Disable at 1:10, so it lands when G's loop does and the Tech isn't left loitering. Alert 2 in the final fight; the three are captured; 12 + 12 T.
  - `careless-loop-runs-out`: careless fault `timing`. The loop is planted first, so the Enter at 2:53.4 is seen by the camera.
- **rival-park stays at (84.0, −31.0) for this batch:** careless-scout-idles-pickup and fight-safe-rivals-reach-vale keep their wait at (82.5, −31.0), and fail-vale-leaves ends at Alert 1, all as c036. When Keel's 059 round moves the park to (90.5, −31.0), the Scout's Mislead from (82.5, −31.0) is refused (too far from the rivals) and fail-vale-leaves' Scout is out of the pickup camera's view (Alert 0): those three need re-siting then (tried on the withdrawn c037: wait at (85.0, −31.0), 5.5 m from the park).
- **Moved by the engine and site, not retimed:** careless-body-found, careless-enforcer-watches-guard, fail-enforcer-down and fight-loud-enforcer-down: same fail code and Alert, different times.
- **054 §6:** careless-takedown-seen-crowd is re-sited to 5:30.0, when the clerks face the door. Their reports land at 5:38 (Alert 3) and the Enter at 5:39 is refused.
