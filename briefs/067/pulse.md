# Pulse · brief 067, batch 0.2.123: NPC looks (I), stand points (L), the patrol (A: nothing)

2026-10-26. Items I and L of `briefs/067-sky-play-0122.md`; A skipped on Vantage's word (below). Nothing edited in `sim/`, `unity/`, `mission.json`, `text.json`.

## 0 · The short answer

| item | what I found | what I did | who is next |
|---|---|---|---|
| **I** NPC looks | **The mapping does not leak:** nobody but a guard (job `guard`, 15 in the crowd + the door guard) wears the `guard` figure, and nobody but the response's officers wears `security`; checked person by person over the people file and pinned to the host's source. **Three looks do read as security, or as the wrong thing, at play zoom:** (1) the **rivals** (dark violet uniform, cap, amber piping) are the police to the eye; (2) the Office's **15 crowd guards** are drawn as the door guard but named "a passer-by" and cannot be hovered; (3) two **guards sit at the café table in uniform from 0:00**. I cannot tell which one Sky meant, so all three are below with evidence and a fix each. | `tools/people/looks.ts` + test (the audit and its rules), `roles.json`: the driver's uniform-and-visor figure out of the civilians' pools, the stale councillor entry out; the audit table and a per-person CSV | **Forma** (a rival look), **Latch** (label the crowd guards; bodies), **Aster** (decide (2) and (3)) |
| **L** stand points | Four guards on one point at the official door from 5:11 and a relief on the door guard's own post from 5:19, exactly as Keel and Suture said (engine, idle run, before/after in §2) | the generator makes **waiting points beside each staffed door**; the relief waits there for the last minute of her walk (the cast guard's relief, for her shift); P-1, P-13 and P-16 hold it; the people extract, site sha and rounds regenerated. All 41 recorded routes play out identically; 6 of 56 unit scenarios move (table in §2.4) | **Keel** (re-pin; five asks), **Latch** (one polish) |
| **A** patrol | Vantage's file: "the sweep keeps the top-down test's shape, so Pulse's routine needs no change" | nothing. I did re-bake the rounds for the three planter modules his item K took out of the grid | Keel, Waymark |

Done-check, all run in the working tree (shared and moving: other agents' edits are in it), in the order of §4: `bun run people` ok (P-0..P-16, N-1..N-8, M-2); the sim files regenerated (`sim:site` twice, `rounds` both copies, `seats`); `bun test tools/people tools/routines tools/life tools/figures tools/people-view tools/people-graph tools/levels/people`: **292 pass, 1 skip, 0 fail** (16 files; `tools/people/` alone 182); `bun run typecheck` clean; `bun run drift:auto` declared the people and skyline ledgers (6/6 recorded runs replay as at HEAD); `bun run verify` **VERIFY OK**. The tests that are red in the tree belong to other agents' in-flight work (§5). Heavy runs through `bun run guard`; nothing committed.

---

## 1 · I · NPC looks

### 1.1 How the host picks a figure (read, not edited)

Three tables, nothing else (`unity/Assets/Suborned/Runtime/Play/HostViews.Figures.cs`, `HostViews.cs`, `FigureLibrary.cs`):

1. **A fixed key** (`RoleFigure`, l. 32): `op/scout|fixer|tech|enforcer`, `waitress`, `guard`, `driver`, `councillor`; `security/*` and `unit/*` → `security`; a crew id (`rival-1..5`) and `body/rival-*` → `rival`; `patrol` → none (Vela's robot prefab).
2. **A job** (`PersonFigure`, l. 41, for every `crowd/<n>` and every body): `fam` = café (server, cook, waitress) · office (clerk, attendant, councillor) · guard · driver · passer (everything else, the keepers too); `lib.Pick("job/<job>")` else `lib.Pick("family/<fam>")`; the pick is FNV-1a over the person's id mod the pool's length (`FigureLibrary.Pick`, l. 52). `DressCrowd` (l. 52) re-picks (`#1`..`#7`) for walkers who set out from the same place at the same time; a body takes the plain pick (`HostViews.cs` l. 279).
3. **The pools** are `tools/figures/roles.json` `families` and `jobBlocks`, built into the library by `FigureBuild.Build`. `roles.json` `playViews` is documentation: nothing reads it at run time.

I replayed all three over the people file (`tools/people/looks.ts`): the 126 walkers of `sim/data/handover.people.json` (the 4 cast people are played by their role, not by the crowd) and every key. `bun tools/people/looks.ts` prints it; `bun tools/people/looks.ts --csv briefs/067/pulse-figures.csv` writes one row per person (id, name, job, host key, figure, class, model, pool, own pick, first stop on the street). `tools/people/test/looks.test.ts` pins every line of the replay to the C# text, so a host change fails there instead of leaving this table stale.

### 1.2 The audit

**The fixed keys**

| host key | figure | what it wears (figures.json) | class | the guard's look? |
|---|---|---|---|---|
| `op/scout` | `scout` | Scout S1-2: ivory hooded jacket, black harness, shorts | cell | no |
| `op/fixer` | `fixer` | Fixer P4-6: ivory and black panelled dress, halter, ponytail | cell | no |
| `op/tech` | `tech` | Tech F2-1: vest, cargo skirt-shorts, cyan visor | cell | no |
| `op/enforcer` | `enforcer` | Enforcer G2: cream coat, black suit, gold trim | cell | no |
| `waitress` (Inez) | `waitress` | W6: green uniform, apron, glasses | cast | no |
| `guard` (the door guard) | `guard` | P1: white and blue duty suit, peaked cap | **security** | **yes, his own** |
| `driver` | `driver` | D2: coral uniform turned mint, chrome visor helmet | cast | no |
| `councillor` (Vale) | `councillor` | Vale C: ivory tunic-jacket, ink collar band, black shoulder disc | cast | no |
| `security/0..1`, `unit/<officer>`, `hostile/<n>` officers | `security` | P1 with the blue turned charcoal and the greys darkened ×0.45 (blue becomes olive-gold trim), cream cap | **security** | **yes: the police wear the guard's model, darker** (by the rule) |
| `rival-1..5`, `hostile/<n>` rivals, `body/rival-*` | `rival` | Worker D5: yellow jacket turned dark violet (value ×0.38), jeans, **cap** | rival | **no by mapping; yes to the eye (W1)** |
| `patrol` | a robot | Vela's PatrolRobot, stage face | machine | n/a |
| `unit/<robot>` | a robot | RobotPorter in a drum with a lamp | machine | n/a |

**The 126 walkers, by job** (pool as the host draws it; `looks.ts` L-1/L-2 hold: only guards wear the duty suit, every guard wears it)

| job | n | figures | class | verdict |
|---|---|---|---|---|
| guard | 15 | `guard` ×15 (`family/guard`) | security | right look; wrong treatment (W2) |
| clerk | 58 | `d4` ×29 (pale grey bomber, navy jumpsuit), `d5c` ×29 (mint chevron dress, white boots) (`family/office`) | civilian | fine; `d4` is the nearest civilian to the guard's value blocks, no cap, no pads |
| attendant | 4 | `d4` ×2, `d5c` ×2 | civilian | fine |
| server, cook | 13 + 9 | `cafe_w2` ×4, `cafe_w3` ×9, `cafe_w5` ×9 (`family/cafe`) | staff | fine |
| keeper | 3 | `d4b`, `d5c`, `d6` (`family/passer`; **was `d2m` ×2 and `el1`**: two of three keepers drew a uniform and visor helmet) | civilian | fixed (W3) |
| resident | 24 | `c6` ×3, `d4` ×2, `d4b` ×3, `d5c` ×2, `d6` ×4, `el1` ×4, `sh2` ×3, `workers_d5` ×3 (`job/resident`; was with `d2m` ×2) | civilian | fine |

**Who is on the street at the opening** (stops at 0:00): 9 people: guards 29 (official door), 73 and 74 (café table 1), residents `d4b` ×2, `d4`, `c6`, and the two kiosk attendants off the block. From the chime: up to 5 guards on the street at once (2 on posts, 3 leaving), and 12 clerks coming out of the public door in the first seconds.

### 1.3 Who wears the wrong look, why, and the fix

**W1 · the rivals read as police (a missing figure). Forma.** At play zoom the five rivals (`rival`) and the police (`security`) are one picture: dark uniform, cap, amber piping; the violet ring against the amber ring is the whole difference (`briefs/067/pulse/looks-0122-play-zoom.png`, crops from Sky's 0.2.122 shots 01, 06 and 04 at the same scale). The figure explains it. `rival` is Worker D5 with `tint {from 48, width 22, shift 235, sat 0.8, val 0.38}`: the yellow jacket goes violet **and dark**, the amber collar and cuff stripes (3 % of the clothing; hues the tint does not reach) stay, the cap stays. Measured on the installed albedo atlases (`briefs/067/pulse/albedo-stats.py`, `.txt`):

| figure | median value | clothing under V 0.35 | amber / olive-gold trim |
|---|---|---|---|
| `guard` | 0.47 | 30 % | 0 % |
| `security` | 0.32 | 68 % | 21 % |
| **`rival`** | **0.27** | **86 %** | **3 %** |
| `d4` / `d4b` (clerks, residents) | 0.27 / 0.24 | 69 % / 81 % | 2 % / 0 % |

The rival is the darkest figure that carries a cap, darker than the police. Sky's 0.2.122 notes "rivals just suddenly appeared when near car" and "guard characters being used that aren't guards" fit three dark capped figures standing about a car.
**Forma must make** (replaces `rival`; a new model is best, since Worker D5 is also worn by 3 residents in yellow):
- jacket violet, `#6b4f8f` (art bible violet_set) to the lighter half of `#8A4FD0`, **mid value: median albedo V ≥ 0.45** and ≥ 40 % of the clothing pixels violet;
- **no brimmed cap**: bare head with hair, or a knit cap in the jacket's violet (the cap belongs to the guard and the police);
- **no gold or amber**: stripes in the jacket's own lighter violet or chrome (the `amber` column below 1 %; it is 3 % now);
- **trousers pale** (ivory or light grey, V ≥ 0.6), not navy or charcoal;
- three colourways for five people, so the crew is a crew and not clones;
- done when `python3 -I briefs/067/pulse/albedo-stats.py` shows median V ≥ 0.45, violet ≥ 0.40, dark ≤ 0.40, amber ≤ 0.01, and a true-size (53 px) row beside `guard`, `security`, `driver` reads "the violet crew" with the ring off; Latch proves it on the 3:10 frame. Vela's `Sedan_Rival` takes its plum from the old dark jacket (`#422F42`, dE 9.4); it stays a quieter plum, no change needed.

**W2 · the Office's 15 other guards look like the door guard and are "a passer-by" (a shared default in the host). Latch (Glyph if the cue changes).** They are `crowd/<n>` views: `HandoverHost.cs` l. 785 labels every crowd person "a passer-by" and l. 807 gives the hover that word, `ContextRank.cs` l. 97 gives a crowd person no way and no ring, `HostPick.cs` l. 37 keeps them out of the markers, and no cone is drawn for them, while the engine treats them as security (`Person.Civilian` is false for job `guard`; they witness all round within 13 m and keep check-in marks). ten of them walk out of the official door (at 5:30, 6:30 and 7:30) and across to the café in the duty suit, so after the chime the square fills with guards the player can neither read nor click, one of them standing beside the one guard who has a ring. This is the likeliest reading of "guards that aren't guards" and of "hovering over the door guard didn't show any ring" (the relief 73 stood 0.7 m from him, §2).
**Latch must change:** a crowd person with `!Person.Civilian` is labelled "a guard" (`TargetOf`, l. 785, and the hover, l. 807), is a pickable with the one-word name "Guard" (`HostPick.cs` l. 37), and shows the guard's cone once learned, as the door guard's does; the Fire offer rules stay as they are. If Sky would rather they not read as guards off duty, that is a figure (W-option below), not a label.

**W3 · keepers drew the driver's twin (a mapping default). Fixed, Pulse.** The host has no `keeper` family, so the café's keeper and the kiosk keepers drew from the passer pool, which held `d2m`: coral uniform, chrome visor helmet, the driver's own model. L-3: a role figure shares its model with the crowd only where it must. `d2m` is out of `family/passer`, `job/resident` and `job/service` (`tools/figures/roles.json`); the driver is the only one in that uniform now; the keepers are `d4b`, `d5c`, `d6`.

**W4 · stale data (fixed, Pulse).** `roles.json` `variantOf.councillor = "d4"` is a leftover: Vale C has his own model since brief 063, and the installed manifest says "recolour of d4". Removed. (`roles.json` `missing[]` still says "Councillor Vale: no design and no model": Forma's to refresh.)

**W5 · two guards at the café in uniform from 0:00 (the cast row, not a fault). Decision for Aster.** 73 and 74 are the watch's guards: their pre-shift stop at the café is the cast row's (`guard-cafe-square`, every guard is a regular of a server), so from 0:00 to 1:30 / 3:30 two guard-uniform figures sit at table 1 among civilians, in the opening view. By the rules they are security (within 13 m on a clear line they would witness the Fixer's Pressure on Inez). Options: **(a)** leave: real security, drawn as such; **(b)** an off-duty look: the same two guard models in civilian couture, chosen when the stop is a café seat: Forma (2–3 colourways, no cap, no pads), Keel (the extract carries `doing` per stop, or the pre-shift flag), Latch (`PersonFigure` by it). I recommend (a) after W1 and W2: with the label and the cone they read as guards, which is what they are.

**Nobody else.** No clerk, café staff, keeper, resident or rival is mapped to a duty-suit figure, by key or by pool; the nearest civilian read is the clerks' `d4` (silver bomber over navy), told from the guard by the cap, the pads and the belt. Not changed. **Residual risk, for the proof frame:** at 53 px in the Office's shadow the cap and the pads are 3 to 5 px (the crops in `looks-0122-play-zoom.png` show it), so a `d4` clerk (grey top over navy) and the door guard are close silhouettes, though the albedo says they differ (median value 0.27 against 0.47). Latch's 5:30 proof frame should show `guard`, `d4`, `d4b` and `d5c` side by side at true size with the ring off; if a clerk and the guard still read as one, Forma recolours `d4` and `d4b` away from grey-over-navy (the coral or mint the `d5c` uses already reads as its own crowd). That is a figure change, not a mapping one: `d4` is also the residents' and cinema's look, so I did not make it on a guess.

**One more host fault, Latch.** A body is dressed with the plain pick, ignoring `DressCrowd`'s re-pick (`HostViews.cs` l. 279): 16 of the 126 walkers change figure when knocked out (a clerk in `d4` lies down as `d5c`). Take the walker's figure: `walkerIndex.TryGetValue(p, out var wi) && figs.TryGetValue(crowdKeys[wi], out var wf) ? wf.id : PersonFigure(p)`.

**Latch after Forma:** reinstall `People/manifest.json` (`tools/figures/run.ts --install`: pools without `d2m` in the passers', no councillor `variantOf`) and rebuild the FigureLibrary; the crowd's figures reshuffle (the pool lengths moved), the same person still always the same figure.

---

## 2 · L · stand points

### 2.1 The finding, measured

Keel's own probe (`dotnet sim/cli/bin/Release/net10.0/suborned-sim.dll cars`, "security people on one spot", read each second of an idle run):

| | before | after |
|---|---|---|
| official entrance (7625, −3000) | **5:11.0 to 5:30.0, at most 4**: 29 + 39 + 61 + 74 | from 5:30.0 to 5:30.0 (one reading): the change-over instant, §2.5 Keel 2 |
| public door (6400, −4125) | **5:19.0 to 6:13.0, at most 2**: the cast guard + 73 | none |

The engine puts everyone at a stop on the stop's point, and a person walks a routine leg 0 to 12 % faster than its timing and waits at its end (`crowd-pace-spread-pct`, my own rule of brief 023): a relief due at the minute the holder leaves is on his post early (74 from 5:11, 73 from 5:19), and the cast guard, whom the engine plays by his role, never leaves his door at 5:30, so the relief stood on him for the rest of the mission (it ends at 6:13 in an idle run because the driver drives Vale away then). The host drew them apart (`GiveWay`, 0.7 m); the engine did not.

### 2.2 The rule

*A post is one person's.* Whoever relieves a staffed door of a civic employer waits at a **waiting point beside it** for the last minute of her walk, the minute before her shift, and steps on as the shift begins; where a cast person holds the post through the mission (the Handover plays the door guard past his shift's end), she **stands beside it for her whole shift** and never takes it. Only for posts the cast row names in `posts` (the one door guard, the one at the official entrance): an older table that puts a whole shift on a door still does, as before. The walk is timed to the post as it always was, so every other stop of her day is where it was; the minute at the point is the last minute of the walk, and P-1 holds a walk into a waiting minute to one minute less.

### 2.3 What I built (paths)

| path | change |
|---|---|
| `contracts/people/people.schema.ts` | `Place.beside` (optional): the staffed door a waiting point stands beside |
| `tools/people/seats.ts` | `POST_STAND`, `postStandingPoints`: along the building's face, either side of the door, 1.2 m apart, within 3.6 m, free ground, 0.45 m off the wall (a guard stands 0.75 m off it at both doors), the plan's body clearance off ground solids |
| `tools/people/scope.ts` | `scope.postStands` (door and official entrance of each civic unit): `office-entrance-stand-00/01`, `official-entrance-stand-00/01`; in the door's own keep-clear zone and no other, in no bay; `scope.missionMin` (life.json `clock.mission.windowMin`, 12) |
| `tools/people/posts.ts` (new) | `heldPlaces` (the doors a mission role holds, by `at` and `near`), `inMission`, `earlyMin`, `PACE_SPREAD_PCT` (read from `rulebook.json`) |
| `tools/people/day.ts` | `Wait`; `workerCycle(…, wait)`: a relief's last minute before the shift is at the point (`work` stop, then the post at the shift's start); a held relief's `work` stops are at the point. No draw added or moved |
| `tools/people/expand.ts` | `namesPost`; picks the first free waiting point by `Holds`; a held relief's `work.place` is the point |
| `tools/people/check.ts` | **P-1** a walk into the waiting minute needs one minute less; **P-13** a waiting point is beside a staffed door, within reach, on its own zone's ground; **P-16** counts a civic post's relief with her early walk, and holds a cast person's post through the mission; a gap if every point is held |
| `tools/sim/people.ts` | faces: a waiting point faces the **kerb** (out of the door: 9 m west of the public door, 4 m south of the official entrance) |
| `sim/data/handover.people.json`, `handover.site.json` (the people's sha), `handover.takedown.json`, `sim/data/handover.rounds.json`, `briefs/022/handover.rounds.json` | regenerated (`bun run people`, `sim:site`, `rounds`); `contracts/people/handover.seats.json` is unchanged (no café stop moved) |
| `tools/levels/people/drift.json`, `tools/levels/skyline/drift.json` | `bun run drift:auto` declared the new hashes (none of the 5 people runs or r011 replays differently from HEAD) |

**The data** (block cm, engine frame; plan metres in the people file):

| place | at | faces | who |
|---|---|---|---|
| `office-entrance-stand-00` | (6520, −4125), 1.2 m south of the door | (5, −900): west, toward Rocket Lane | **73**, the watch's door guard: arrives 5:19, stands there for her whole shift |
| `office-entrance-stand-01` | (6280, −4125), 1.2 m north | (−5, −900) | 75, the next watch, 1:29 |
| `official-entrance-stand-00` | (7625, −3120), 1.2 m west along the face | (400, −5): south, toward Apron Road | **74**: arrives 4:18, waits 4:30 to 5:30, on the post from 5:30; 76 at 1:29 |
| `official-entrance-stand-01` | (7625, −2880), 1.2 m east | (400, 5) | (free) |

All four are free cells of the site's grid, off every standing car's ground (`suborned-sim cars` lists none of them). **Six people's days moved and nobody else's** (a stop-by-stop comparison of `sim/data/handover.people.json` against HEAD's), and only by a minute at a point (day clock): 01 (the cast guard) and 29 at 9:29, 74 at 17:29 (4:30 to 5:30 of the mission), 75 and 76 at 1:29, and 73's shifts at her point. Every café stop, seat, leave time and tie is HEAD's byte for byte.

### 2.4 Evidence

- **The generator's own rule fails without the points.** `tools/people/test/posts.test.ts` ("without waiting points…") expands the cast with `postStands = {}` and P-16 reports `… both drawn at official-entrance` and `… holds it through the mission` (the cast guard and 73 at `office-entrance`). With them: `bun run people` P-1, P-13, P-16 ok ("5 places": the Office's two doors are counted now), M-2 ok (the guard is still p-records-office-01).
- **Engine, idle run** (`out/scratch/pulse/probe-trace`): 74 reaches (7625, −3120) at 4:18 and is there until 5:30, on the post (7625, −3000) from 5:30.0 and alone; 29 is on the post until 5:30 and 6 m away at 5:40; 73 reaches (6520, −4125) at 5:19 and the cast guard stays on (6400, −4125). Marks: 74's first mark is 5:30.0, then 7:00.0, as before.
- **The file the engine reads** holds it too: `posts.test.ts` replays every stop of `sim/data/handover.people.json` with the crowd pace's early arrival and finds no two people at a post at once, and nobody but the cast guard at the public door in the mission; the waiting points are in `places`, facing `kerb`; the engine resolves them to (5, −900), (−5, −900), (400, −5), (400, 5).
- **No recorded route moved.** All 41 scripts of `runs/missions/handover/c037/routes/` played on this tree's inputs with the old people file and with the new: outcome, time, cash, Trail, Alert and shots identical in every one (route A Success 4:59.7, route G 4:44.7, the loud ones, every careless and fail twin); only the start and final hashes differ, as the inputs do. Route B withdraws at 8:08 on both: the 059 / patrol re-compose Vantage and Motive name, not this.
- **Scenarios** (`suborned-sim check --no-replay`, 56 that read the Office's guards, HEAD's site, rulebook and rounds, the people file the only difference):

| the people file | scenarios that fail |
|---|---|
| HEAD's | none, 56 / 56 |
| **this change** | **N52, N53, N84, N128, BAL-LOUD-BREAK, DP1** |
| first try: the café stop a minute earlier | those and N47, N55, N75, N87 (9 + DP1) |
| second try: the relief takes the post a minute after the change | S14 (the cast guard's marks move), N60–N64, N81, N82, N87, N84, DP1 |

  What each is: **DP1** pins the start hash, so any input change moves it. **N84**: 73's radio lands at 5:05.1, not 5:06.1: her last 1.2 m differ. **N52, N128, BAL-LOUD-BREAK**: 74's walk to the official door is now 2 minutes (the minute at the point is the last minute of the walk), not 3, so at 3:30 to 3:50 she is on the north side of the Office where the G-hide's carry and the loud variants' carry of Vale cross, and she radios it ("74 sees a body carried", 3:30.4 in N52; 3:49.2 in N128 and BAL-LOUD-BREAK); in HEAD she was a third of the way along then. **N53**: 74 stands 1.2 m farther from the Dumpster than the post was and no longer hears the victim at 4:40 within 20 m (in HEAD her noise report lands at 4:46.4). None is a fault of the points; each is a quiet carry that now crosses a walker one minute earlier. Keel re-pins, or Waymark moves the carry; Pulse has no cheaper way (the last minute of her walk has to come from somewhere).
- `bun run verify`: VERIFY OK (18 runs replayed, overlay 142/142, promoted cast matches its run, typecheck clean).
- Tests: `tools/people/test/posts.test.ts` (13: the points beside both doors in their own zone and no bay, who waits where, the cast hold, P-16 on its example, the engine's file) and `looks.test.ts` (10). `day.test.ts` ("one guard at the public door every minute") now counts a guard at a waiting point as at the door; `walk.test.ts` uses the kerb runs that stay (Vantage's item K took `run-sq-south` and `run-sq-west` out).

### 2.5 For Keel, Latch and Aster

**Keel**
1. Re-record: the people extract, the site's `cast.sha256` and the rounds moved, so every start hash moves; the 41 routes' outcomes did not (above); six scenarios are in the table. The rounds bake lost 36 cm from the guard's walk to Inez (`walkCm` 8042 → 8006 and so on, the three planter modules), and 4 of Inez's reference poses moved, by 27 cm at most; the engine's file and the candidate are the same bytes again (`DP1` is the only scenario that reads that alone).
2. Your `cars` probe still prints "from 5:30.0 to 5:30.0, at most 4" at the official entrance: it is the change-over tick (29 leaves, 39 and 61 come out of the door, 74 steps on), nobody stands. Read a one-reading span as no overlap, or count only runs of two seconds or more.
3. **73 has no check-in marks** (`PostOf` reads stops at a staffed door's place; she stands beside one). Before she had them from 5:30. If Motive wants them, the extract can carry `beside` and `PostOf` can read a waiting point as its post.
4. **The `door` facing is a quarter turn off at the Office.** `ResolveStopFaces` takes `entrance.stand − entrance.at`, and `stand` is the nearest free cell 25 cm away: (25, 0) at the public door (the door faces west), (0, 25) at the official entrance (it faces south), (55, 25) at the café. So the crowd guards on the posts (29, 74) look along the wall, not out. `kerb` gives the outward directions ((−25, −900), (400, −25)). One line in `tools/sim/people.ts` (write `kerb` for the civic doors) fixes it; it changes who the post guards see, so it is yours and Motive's call, and I did not make it.
5. `suborned-sim play S10` does not finish in 90 s on the current tree, with my people file and with the old one; on HEAD's data with my people file it passes in 0.7 s. So it is another of today's inputs (the site's interim patrol points, I think), not this change.

**Latch**: when a relief steps from her point onto the post at the change (5:30:00) the engine moves her 1.2 m in one tick; ease a standing person's jump of 1.5 m or less over 0.4 s so it does not read as a snap. `GiveWay` no longer has anything to do for 73. The W2 label, the bodies, and the manifest after Forma (§1.3).

**Aster**, GDD v0.26.0, proposed words (§14 Pulse hold and §09's people row): "a door is one person's post: whoever relieves it waits at a standing point beside it and steps on at the change; a post a cast person holds through the mission is never taken, and its relief stands beside it for her shift." And §15 people: "the rivals are the one crew the art keeps apart from security: violet, no cap, no gold."

---

## 3 · A · the patrol

Vantage (`briefs/067/vantage.md`): "The sweep keeps the top-down test's shape, so Pulse's routine needs no change… no routine change is needed; the crowd and rounds walk on a grid that has lost three planter modules." So nothing for the patrol. The second half I did: `bun run rounds` re-baked on the grid without the modules (§2.5, Keel 1), `sim/data/handover.rounds.json` and its candidate are the same bytes again, and `tools/routines` is green.

## 4 · Evidence (commands)

```
bun run people                       ok P-0..P-16, N-1..N-8, M-2; 130 people
bun run seats; bun run rounds --out sim/data/handover.rounds.json; bun run rounds
bun run guard bun run sim:site       people extract, site, takedown, provisional
bun tools/people/looks.ts            the audit; --csv briefs/067/pulse-figures.csv (126 rows)
bun run drift:auto                   people and skyline declared; verify
bun run verify                       VERIFY OK
bun run typecheck                    clean
bun test tools/people tools/routines tools/life tools/figures tools/people-view tools/people-graph tools/levels/people
```
Scratch (kept): `out/scratch/pulse/` (`probe-stand`, `probe-trace`, `probe-face`, `probe-looks`: engine probes over the built DLL; `before/`: the files as they were). Pictures and measures: `briefs/067/pulse/` (`looks-0122-play-zoom.png`, `albedo-stats.py`, `albedo-stats.txt`), `briefs/067/pulse-figures.csv`.

## 5 · Open issues

- **The tree is shared and moving.** The sim files are a snapshot of everyone's inputs at the time I ran `sim:site`; the site now carries Vantage's interim patrol points with the old facing (his file says `tools/sim/site.ts` `patrol.facing` must go to `[0, -100]`). Whoever lands last runs, in order: `bun run people` · `bun run sim:site` · `bun run rounds --out sim/data/handover.rounds.json` · `bun run rounds` · `bun run seats` · `bun run cover` · `bun run sim:site` · `bun run drift:auto` · `bun run verify`. My code changes the people hash again with every edit, so `drift:auto` is the last step before `verify`.
- **Red tests that are not mine** (they fail with the people file I had before too): `tools/missions/handover/mission.test.ts` PLACES lock and `tools/world/rules/walking.test.ts` (29 points, not 28): Vantage's moves, Splice re-pins; `tools/sim/sim.test.ts` route A (S10): the hang above; `tools/missions/check/check.test.ts` route B: the 059 re-compose.
- **Not decided:** W2's fix needs Latch; W5 needs Aster; the `door` facing (Keel 4) needs Motive.

## 6 · Next owner

| to | what | done when |
|---|---|---|
| **Forma** | W1: the rival look (spec above); refresh `roles.json` `missing[]`'s Vale line; `d2m` may leave the build for the crowd (only the driver draws it); if Latch's proof frame still shows a clerk as a guard, recolour `d4` and `d4b` (§1.3 Nobody else) | `albedo-stats.py` rival row inside the spec; a 53 px row `guard`, `security`, `rival`, `d4`, `d5c` with the ring off reads as five crews |
| **Latch** | label and pick the crowd guards (W2); the body's figure; manifest and library after Forma; the 1.2 m step ease | a hover on guard 73 says "Guard"; a knocked-out `d4` clerk stays `d4`; proof frame at 3:10 and 5:30 |
| **Keel** | the five items in §2.5; re-record | `suborned-sim cars` prints no overlap longer than one reading; hashes re-recorded |
| **Aster** | GDD words; decide W5 | v0.26.0 |
