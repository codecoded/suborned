# Pulse · brief 067, batch 0.2.123: NPC looks (I), stand points (L), the patrol (A: nothing)

2026-10-26. Items I and L of `briefs/067-sky-play-0122.md`; A skipped on Vantage's word (below). Nothing edited in `sim/`, `unity/`, `mission.json`, `text.json`.

*Second pass, same day (Forma's Velvet Set and Latch's body figure and "a guard" label): the mirror and pins are brought to the tree and the looks audit is re-run in **§7**. It closes §1.3 W1, W2 and the bodies fault, with two conditions (7.5): the install, and the host's one rival look.*

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

---

## 7 · Second pass, 2026-10-26: the mirror and the pins brought to the host and to Forma's crew, the audit re-run

Aster's follow-up. Forma built the rivals' Velvet Set (`rival`, `rival_b`, `rival_c`, `rival_d`, pool `role/rival`: `briefs/067/forma.md`) and Latch changed the host's body figure (`HostViews.BodyFigure`: a knocked-out walker keeps their figure) and named the crowd's guards "a guard" (`HandoverHost.CrowdSecurityName`). My `looks.ts` mirror and its pin of `HostViews.cs` disagreed with the tree (the one red test). Both are brought to the tree as it is now, and the audit is run again. Nothing edited in `sim/`, `tools/sim`, `unity/` or `tools/figures`; `sim/data` not regenerated; nothing committed. This closes §1.3 W1, W2 and "one more host fault" (bodies), on the two conditions in 7.5 (the install, and the host's one rival look).

### 7.0 · The short answer

| claim | result | evidence |
|---|---|---|
| **Only real security wears the guard's look** | **Holds.** L-1 has 0 faults both ways over the 126 walkers, every fixed key and every pool. The 15 crowd guards wear `guard` and the hover names them "a guard"; nobody else wears it or is named so. A knocked-out walker keeps their figure (0 of 126 change clothes; the plain pick would have changed 16). The test now lists every `Dress(`, `RoleFigure(` and figure `Pick(` call in `HostViews*.cs`, so a new place that dresses a view fails there instead of going unmirrored | `pulse/looks-audit.txt` |
| **The rivals meet my spec** | **Forma's build does; the game does not yet.** Median V 0.52 to 0.64, violet 0.50 to 0.76, dark 0.20 to 0.30, amber 0.003 to 0.006 (bright amber or gold 0.0001), navy 0.002 to 0.003, trousers V 0.65 to 0.71, no brimmed cap. But the Unity install still holds the old rival (V 0.27, amber 0.030, navy 0.28, hard hat), and the host draws all five rivals as `rival`: one look (7.5, 1) | `pulse/albedo-stats-velvet.txt`, 7.2 |
| **No clerk reads as a guard** | **Holds at true size.** `d4` (grey bomber, navy legs, bare head), `d4b` and `d5c` (mint dress, cloche, bare legs, white boots) are told from the guard (white limbs banded in navy, blue-grey peaked cap) in every view I looked at: both camera yaws, sun and shade, two headings. The two nearest by colour are **`c6` and `d5c`**: a watch list, not a fault (7.3) | `pulse/look-matrix.txt`, `look-matrix-summary.txt`, two boards |

### 7.1 · What changed (paths)

| path | change |
|---|---|
| `tools/people/looks.ts` | The mirror follows the host: `bodyFigure` (`Bodies` and `BodyFigure`: a crew member's body is `rival`, a walker's is the figure `DressCrowd` gave them, the cast's their role's, anyone else the plain pick), `hostileFigure`, `rangeBystanderFigure` (the range's passers, from `family/passer`), `crowdLabel` (the hover's names), `personFigureOf` (a role first), `crewPlan` (Forma's table by member). New rule **L-4**: the crew's looks are the crew's (pool `role/rival` holds figures of role rival and no other, none a recolour of the duty suit's model, none in another pool or on a walker, and the host's crew keys and rival bodies are drawn in one of them). The audit prints the hover's name for each job, "0 of 126 change clothes", the crew's four looks beside what the host draws, and the range's passers. The CSV has one new last column, "named in the hover"; the first 11 columns of all 126 rows are byte-identical to the committed CSV |
| `tools/people/test/looks.test.ts` | 10 tests become 15. New pins: the exact list of `Dress(` calls (8) and figure `Pick(` calls (3) and `RoleFigure(` calls (2) in `HostViews*.cs`, read with comments stripped; `BodyFigure` and the walker index it needs; the two lines that name a crowd person (`CrowdSecurityName`, `TargetOf`), with the check that everyone drawn in the duty suit is named a guard and nobody else is; the crew's views exist before the fixed keys are dressed; L-4 on seven failing examples; the crew's table by member as documentation (three different looks for the three posted, all in the pool, the host reading none of it). Forma's one edit of this file (L-3) is kept |
| `briefs/067/pulse-figures.csv` | regenerated (the new column only) |
| `briefs/067/pulse/` | `look-matrix.py` (renders the figures at true size through Forma's `proof_run.py`, unchanged, and measures upper, lower and head colour blocks against the guard and the police), `run-look-matrix.sh` and `look-matrix.txt` (8 views for the build, 8 for the install; a re-render is byte-identical), `look-matrix-summary.py` and `.txt` (the ranking), `looks-audit.txt`, `albedo-stats-velvet.txt`, `mutate-looks-test.sh` (below), and the pictures (`looks-velvet-near-guard-yaw210@3x.png`, `…yaw30@3x.png`, `rival_d-hood-up-forma-render.png`; images are git-ignored, on disk) |

**The pins bite.** `sh briefs/067/pulse/mutate-looks-test.sh` reads the same test from a copy of the host's source with one change each: a new unmirrored `Dress(` call; bodies back to `PersonFigure(p)`; the crowd guard named a passer-by again; `RoleFigure` given Forma's `RivalFigure`; a rival's body given it; the range's passers drawn from `family/guard`; a hostile officer dressed `guard`. Each fails the test that names it (1 or 2 tests each); the unchanged copy passes 15 of 15. Exit 0.

### 7.2 · The rivals against the spec

On Forma's build (`out/people-models/People`, 26 figures, not installed), by `tools/figures/albedo_stats.py` (the measure of §1.3, unchanged) and three checks of my own:

| figure | median V (≥ .45) | violet (≥ .40) | dark (≤ .40) | amber (≤ .01) | bright amber or gold | navy | head | trousers or dress |
|---|---|---|---|---|---|---|---|---|
| `rival` | **0.545** | **0.507** | 0.289 | 0.0055 | 0.0001 | 0.002 | bare, dark hair | ivory, median V 0.68 |
| `rival_b` | **0.522** | **0.498** | 0.300 | 0.0048 | 0.0001 | 0.003 | bare, platinum | pale grey, V 0.65 |
| `rival_c` | **0.639** | **0.512** | 0.208 | 0.0048 | 0.0001 | 0.002 | bare, auburn | pearl, V 0.71 |
| `rival_d` | **0.537** | **0.764** | 0.196 | 0.0029 | 0.0000 | 0.002 | hood in the jacket's violet, no brim | no trousers: white dress, lilac chevrons, bare legs, white boots |
| old `rival` (**installed**) | 0.267 | 0.412 | 0.856 | 0.0298 | 0.0144 | 0.277 | hard hat | navy jeans |
| `guard` / `security` | 0.475 / 0.318 | 0 / 0 | 0.303 / 0.679 | 0.003 / 0.211 | 0 / 0.0006 | 0.248 / 0 | cap | |

- **Amber.** Forma's last tint darkened the deep browns of skin and hair that my rule (hue 20 to 70, S above .3, V above .3) was counting. I checked that this is not a way round the rule: the amber-band texels that remain have median colour (86, 68, 53), hue 29, which is brown shade; bright amber or gold (V from .55) is 0.0001 of the clothing, against the old rival's 0.0144. The police's 0.211 is their olive-gold trim, which is the thing the crew must not have.
- **Trousers.** Measured under Forma's leg masks (`d4_legs` less `d4_boots`), clothing texels only: median V 0.68, 0.65, 0.71 against my floor of 0.6. The woman has no trousers; the clause's point, no navy or charcoal legs, holds (navy 0.002).
- **No brimmed cap, and the hood (Forma's question).** The three D4 looks are bare-headed (the boards, `forma/rival-concept-and-figures.png`). `rival_d`'s hood is up, soft and rounded, brimless, in the jacket's own violet (`pulse/rival_d-hood-up-forma-render.png`, Forma's front, side and back): the "knit cap in the jacket's violet" my spec allowed. **Accepted.** Her `figures.json` label says "bare head (the hood is down)", which the picture contradicts: the hood is up. Forma to correct the words; the figure is right.
- **From the police.** The old rival was 25 dE from the police (upper plus lower colour blocks, shade, yaw 30), nearer than `d4` (33) or `d5c` (53); the new four are 61 to 72 from them and 45 to 55 from the guard (`look-matrix-summary.txt`). In the true-size boards (Forma's `rival-rows-before-after.png` and mine) they read as a violet crew in the sun and in shade, at both camera yaws.

### 7.3 · Clerks against the guard

- **By the mapping:** the office pool is `d5c` and `d4` (the civic job block adds `d4b`); the 58 clerks are `d4` ×29 and `d5c` ×29; none is the duty suit's model, in the people file or in any pool.
- **At true size** (the play camera, the game's sun and grade, Forma's `proof_run.py`; yaw 210, the Office front, and yaw 30, the opening; sun and shade; each figure facing the camera and at heading 75): the guard is the only figure with white limbs banded in navy under a blue-grey peaked cap. `d4` is a grey-white bomber over solid navy legs with a bare head; `d4b` a peach bomber over charcoal; `d5c` a mint dress with a pale cloche, bare legs and white boots. None is mistakable for him in any of the 8 views, nor is `c6` (grey hooded jacket over a white and orange patterned dress, bare legs).
- **By colour** (`look-matrix-summary.txt`; each figure's smallest dE from the guard over the 8 views, upper plus lower block, then with the head block added): `c6` 18 and **28**, `d5c` 20 and **38**, `d4` 18 (its navy legs against the guard's panelled legs, in shade, one view) and 51, `cafe_w2` 20 and 57, `fixer` 24 and 53, `enforcer` 26 and 52, the rest further; the crew 45 to 55 and 70 to 83. With the head block counted (where a cap or a hood shows), **`c6` and `d5c` stand apart from the rest**, which are all 48 or more: both share the guard's pale value and a pale head.
- **What the number can't do.** It sees colour, not silhouette (a hood against a cap, a dress against trousers). The old rival sat 25 from the police, which I judged a confusion in Sky's frames (§1.3 W1), and the clerks and `c6` sit 18 to 20 from the guard in their nearest view, which I do not judge one in the boards; so I claim no threshold from it. The claim rests on the boards; the number only says who to watch.
- **If Sky still sees guards among the clerks or residents:** `d5c` (29 clerks, 2 attendants, 2 residents, 1 keeper) and `c6` (3 residents) are the two to recolour, one tint each (a darker or coral cloche, a coloured hood), as Forma said of `d5c`; both change several people's look, so I have not asked for it. The check is Latch's 5:30 frame at the Office with the rings off: `guard`, `d4`, `d5c` and `c6` side by side at true size.

### 7.4 · Evidence (commands, run in the shared tree)

```
bun tools/people/looks.ts > briefs/067/pulse/looks-audit.txt        rules L-1, L-2, L-4: no fault; 126 walkers; 16 re-picked by DressCrowd, 0 change clothes in a body
bun tools/people/looks.ts --csv briefs/067/pulse-figures.csv        126 rows, first 11 columns byte-identical to before
bun test tools/people tools/figures                                 273 pass, 1 skip (people-graph live server), 0 fail; 13 files (the filter also takes tools/people-view and tools/people-graph)
bun test tools/people/test/looks.test.ts                            15 pass
bun test tools/figures                                              15 pass (rival.test.ts: its albedo spec is read from the build, the first figures folder it finds)
bun run typecheck                                                   clean (exit 0)
bun tools/lint/comments.ts                                          0 hits
python3 -I tools/figures/albedo_stats.py --base out/people-models/People/Figures --check    spec OK, every rival colourway inside it
python3 -I tools/figures/albedo_stats.py --base unity/Assets/Suborned/People/Figures        the old rival: V 0.27, amber 0.03, navy 0.28
sh briefs/067/pulse/mutate-looks-test.sh                            control passes (15), 7 of 7 changes caught
bun run guard sh briefs/067/pulse/run-look-matrix.sh                look-matrix.txt (16 renders, under a minute, byte-identical on a second run)
python3 -I briefs/067/pulse/look-matrix-summary.py                  look-matrix-summary.txt
bun run guard python3 -I tools/figures/proof_run.py --people out/people-models/People --yaw 210 / 30 --light both ...    the two boards
```

Not run: Unity. What this says of the host is the C# text pinned, not a frame; Latch's proof frames and `Proof067`'s `r067 people` line (which should report 16 walkers re-picked and 0 bodies changed, as the mirror does) remain the check in the game.

### 7.5 · Open issues

1. **The game still draws the old rival, and then one look. Latch.** The Unity install holds the old `rival` (`unity/Assets/Suborned/People/manifest.json`: 23 figures, no `role/rival`); Forma's build is 26. Install the whole build (`bun tools/figures/run.ts --install`, or `forma.md` §4's rsync), rebuild the FigureLibrary, and add `rival_d` to `FigureBuild.Women` (line 22) so she walks with `walk_f`. Then the host: `RoleFigure` returns `rival` for every crew key and rival body, and the hostile and body sites say `"rival"` too, so all five are the mid-violet bomber, ivory trousers and dark hair: five clones. My W1 asked for three colourways for five people; the figures have them, the game will not until Latch adds Forma's `RivalFigure` by member number (`forma.md` §4: one helper, four sites). When he does, the pin on `RoleFigure` and the list of `Dress(` calls fail, and I re-mirror (`crewPlan` already reads the table from `roles.json`).
2. **`figures.json`, `rival_d`'s label: "the hood is down".** It is up (7.2). Forma, one word; the audit prints that label, so it shows there until changed.
3. **W5, off-duty guards in uniform at the café table from 0:00 (73 and 74), still Aster's** (§1.3). They are real security, named and drawn as such; whether they should look off duty is a figure the guard model does not have.
4. **`looks.ts` is inside three verdict hashes.** Every non-test `.ts` under `tools/people` is in the transit, skyline and people tool hashes (`tools/levels/toolhash.ts`, `…/people/hash.ts`, `…/skyline/hash.ts`), though no verdict reads this audit, so any edit of it (this one included) moves them: with it the skyline hash goes `3064b3434ff2` to `0734db5326a4` and the people hash `5b41afe605a3` to `7e8a66c24b29` (one file listed each, `tools/people/looks.ts`; dialogue's `b4c40a103058` stays). I did not run `drift:auto`: before my edit `bun run drift:auto --check` at HEAD already refused all three ledgers, and it refuses the same three, the same runs, after it (skyline r011, people r012 to r016, dialogue r017 to r019: "a person declares a change that moves a rule", `files changed since HEAD: none listed`), and that is a person's declaration, not mine to make mid-round. The audit is my file: I can move it out of `tools/people` so it stops entering the hash (the paths `forma.md`, §1 and the test headers quote would change). Aster's call.
5. **The engine-dependent tests.** `tools/people-view`, `day` and `trust` build the working tree's engine for M-2's measured walk. Two of my runs of `bun test tools/people tools/figures` had failures in them (5 besides the pin I was asked to fix, before my edit, and 5 after); the second run's output shows why: `dotnet build sim/cli failed: ScenariosFightLabKite.cs(164,21) CS0128 …`, Keel's file mid-edit (it was changed a minute before). When the tree compiled they passed (those three files 73 of 73, then the whole set: 273 pass). A red there while `sim/` is mid-edit is his tree, not the looks; `git status` showed 8 `sim/` sources of his modified at that minute (more by the end of this pass).
6. **The crew shares bodies with the crowd** (Forma §5 item 1: D4 is the clerks', C6 three residents'). The colour alone tells a rival from a clerk (violet over pale, against grey over navy), L-3 names it, and the numbers above hold; new bodies are Aster's call on Forma's $0.675 a take.
7. **Red tests in the tree that are not this work** (from a wider run, `bun test tools/levels tools/drift-auto tools/lint tools/gen`: 192 pass, 3 fail). `tools/drift-auto/drift-auto.test.ts` ×2 ("nothing changed: nothing to declare", "a harmless edit"): the committed drift ledgers do not declare the committed code. I built the test's scratch copy from a pristine `HEAD` archive (no edit of mine in it) and `drift:auto --check` refuses the same runs with the same skyline hash `3064b3434ff2` (item 4). `tools/levels/skyline/test/fixture.test.ts` ("the kit digest fixture lists every piece kit.json has adopted"): `Sedan_Rival` is adopted in `contracts/world/kit.json` (Vela's, brief 067 L) and `kit.digest.json` is behind; the failure prints the regenerate command. Splice or Vela.

### 7.6 · Next owner

| to | what | done when |
|---|---|---|
| **Latch** | install Forma's build and rebuild the library; `Women` += `rival_d`; `RivalFigure` by member in the four sites if the crew is to be a crew; the 3:10 and 5:30 proof frames (the crew round the car; the guard beside `d4`, `d5c`, `c6`, rings off) | `albedo_stats.py --base unity/Assets/Suborned/People/Figures --check` prints OK; three different violet looks round the car; no clerk or resident reads as the guard |
| **Forma** | correct `rival_d`'s label; if the frame still shows a pale clerk or resident as the guard, tint `d5c` and `c6` | `bun test tools/figures` green |
| **Pulse** | re-mirror when the host reads the crew's table (the pins fail when it does) | `bun test tools/people` green |
| **Keel** | at the end of the engine round: `bun run people` and `sim:site` as planned; whoever lands last then runs `drift:auto` and `verify` (the people tool hash moved with this edit as it did with my last; 7.5, 4) | VERIFY OK |
| **Aster** | W5; the GDD words in §2.5 (the rivals "violet, no cap, no gold"); new bodies for the crew or not | decision logged |

---

## 8 · Third pass, 2026-10-26: the mirror brought to host round 4 (the crew by member, the crowd's facing), the audit re-run

Aster's follow-up after Latch's host round 4 (`briefs/041/port-progress.md`). As Forma foresaw (§7.5, 1), three pins failed because the host's text moved: `RoleFigure` for a crew key, the two hostile sites and a rival's body now say `RivalFigure(...)`. The mirror and the pins are brought to the tree as it stands. Nothing edited in `tools/missions`, `contracts/`, `sim/`, `unity/` or `tools/figures`; `sim/data` not regenerated; nothing committed.

### 8.1 · The short answer

| claim | result | evidence |
|---|---|---|
| **Only security wears the guard's look** | **Holds.** L-1 0 faults over the 126 walkers, every fixed key and every pool; the 15 crowd guards wear `guard` and are named "a guard"; 0 of 126 change clothes when knocked out | `pulse/looks-audit.txt` |
| **The five rivals in four violet looks, none reading as police** | **Holds, in the install now.** `albedo_stats.py --base unity/Assets/Suborned/People/Figures --check`: "spec (brief 067 W1): OK" (the install is 26 figures, `rival` V 0.55, amber 0.00; the old rival is gone). By member: rival-1 `rival`, -2 `rival_b`, -3 `rival_d`, -4 `rival_c`, -5 `rival` (four looks for five people; Forma's table the same). The look matrix on the install is the build's, number for number. Nearest to the police by colour (upper plus lower): `rival_b` 61, `rival_d` 63, `rival_c` 71, `rival` 72, against 19 to 33 for the clerks and cafe staff; to the guard 45 to 55 | `pulse/look-matrix.txt` part B, `look-matrix-summary.txt` |
| **No clerk as a guard** | **Holds.** The 58 clerks are `d4` and `d5c`, the 2 attendants likewise; none is the duty suit's model, in the people file or in any pool; the hover names only the 15 duty-suit people "a guard". The same two to watch as in §7.3, `c6` and `d5c` (28 and 38.5 with the head block): unchanged, no fault | `pulse/looks-audit.txt`, `look-matrix-summary.txt` |

### 8.2 · What changed (paths)

| path | change |
|---|---|
| `tools/people/looks.ts` | `RIVAL_LOOKS` and `rivalFigure(id)` mirror `HostViews.RivalLooks` / `RivalFigure` (the trailing digits of the id, 1 when none, wrapping at 5). `roleFigure` gives a crew key and `body/rival-<n>` its number's look; `hostileFigure` gives a `rival…` hostile the look of his number (the range's rival-9 is `rival_c`, rival-12 `rival_b`), any other `security`. L-4 now also fails when a rival hostile or body is drawn outside the pool, when a member is drawn differently alive, down and in a fight, when the five wear fewer than four looks (or fewer than the pool holds), when rival-1..4 do not wear four different looks, or when Forma's `playViews` table and the host's differ. The audit prints the table by member and the body and hostile looks beside it. `crewPlan` is no longer documentation: the host reads the same table. The crowd's facing is not mirrored (the audit dresses people, not turns them): it is pinned |
| `tools/people/test/looks.test.ts` | 15 tests become 17, all green. The three stale pins are re-written to the new text (`RoleFigure ... => RivalFigure(key)`, the two `Dress(hostileKeys[i], h.Id.StartsWith("rival") ? RivalFigure(h.Id) : "security")` sites, `Dress(key, RivalFigure(p.Id))`); the list of `Dress(` calls follows. New: **RivalFigure** (the four lines of the helper, the table `{ rival, rival_b, rival_d, rival_c, rival }`, ids with and without digits, the five wearing every look of the pool and none outside it, the library fallback `rival_` to `rival`, `rival_d` in `FigureBuild.Women` so she walks `walk_f`, and the four `RivalFigure(` call sites); **the crowd faces the way the engine says** (the order SpreadFace then StillFacing, the standing-only guard, the `Game.FacingNow` call and its source line in `GameNotice.cs`); the last test (the crew's table) now holds the table equal to the host's, four looks, the lookout and the wheelman not the same, every member the same alive, down and in a fight, and `rival_d` on C6's body. `figures.Find`/`lib.Find` is two calls (the fallback and the lookup), pinned |
| `briefs/067/pulse/looks-audit.txt` | re-made |
| `briefs/067/pulse/run-look-matrix.sh`, `look-matrix.txt`, `look-matrix-summary.txt` | part B now measures the install's full set (24 figures) against the guard and the police; re-run (the build's part A is byte-identical to before; B matches A) |
| `briefs/067/pulse/mutate-looks-test.sh` | m4, m5 and m7 re-aimed at the new text (the crew back to one look, a rival's body back to `"rival"`, a hostile officer as the door guard); m8 (the table's order changed), m9 (the engine facing taken out), m10 (the library fallback taken out) and m11 (a rival hostile in the police look) added |
| `briefs/067/pulse-figures.csv` | regenerated, byte-identical (the crowd did not change) |

### 8.3 · Evidence

```
bun test tools/people tools/figures                                  275 pass, 1 skip (people-graph live server), 0 fail
bun test tools/people/test/looks.test.ts                             17 pass
bun run typecheck                                                    clean
bun tools/people/looks.ts > briefs/067/pulse/looks-audit.txt         rules L-1, L-2 and L-4: no fault; 126 walkers; 0 change clothes in a body
bun tools/people/looks.ts --csv briefs/067/pulse-figures.csv         byte-identical
python3 -I tools/figures/albedo_stats.py --base unity/Assets/Suborned/People/Figures --check    spec (brief 067 W1): OK
bun run guard sh briefs/067/pulse/run-look-matrix.sh                 wrote look-matrix.txt (87 s)
python3 -I briefs/067/pulse/look-matrix-summary.py                   look-matrix-summary.txt
sh briefs/067/pulse/mutate-looks-test.sh                             control passes (17); 11 of 11 changes caught
bun tools/lint/comments.ts                                           0 hits
```

The first run of `bun test tools/people tools/figures` in this pass showed one "(unnamed)" failure besides the four pins (the tests that build the working tree's engine, §7.5, 5); it did not repeat once the pins were fixed and the tree's `sim/` compiled.

### 8.4 · Open issues

1. **`figures.json`, `rival_d`'s label still says "the hood is down"** (§7.5, 2; the audit prints it). It is up. Forma, one word.
2. **The range's bystanders include `workers_d5`** (hard hat, amber 0.40, navy 0.32: the old rival's body, a passer's look in `family/passer`; `b-4` of the fight range). No walker of the Handover wears it, and the rule that holds is about security and the crew, so it is no fault; but in the range, beside police, it reads like them (the very look that made the old rival read as police). Latch or Forma, if Sky tries the range: take it out of `family/passer`, or leave it for the range.
3. **The crowd's facing is pinned by text, not seen.** The pins say the standing-alone rule is in the host; whether the Office's door guards now face the kerb in a frame is Latch's proof (`r123-looks`), not this audit.
4. **Tool hashes.** Editing `looks.ts` moves the same three verdict hashes as in §7.5, 4 (skyline, people, and the transit's); I did not run `drift:auto`, which refused at HEAD before this edit too. Whoever lands last declares it.
5. **W5 (off-duty guards, 73 and 74) and new bodies for the crew** remain Aster's (§7.5, 3 and 6).

### 8.5 · Next owner

| to | what | done when |
|---|---|---|
| **Forma** | correct `rival_d`'s label | `bun test tools/figures` green |
| **Latch** | if Sky plays the range, decide `workers_d5` among the passers; the 3:10 and 5:30 frames with rings off (the crew by member beside the guard, `d4`, `d5c`, `c6`) | frame read by Glyph |
| **Aster** | W5; new bodies or not | decision logged |
| **Pulse** | re-mirror when `RivalLooks`, `RoleFigure` or a `Dress(` site moves again | `bun test tools/people` green |
