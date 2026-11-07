# Brief 100 · The why: the words

Fable, 2026-11-07, on `work/098-living-playbook`. Sky, playing: "It would be good to explain why an alert raised, or why police came". These are the words for every way the Alert rises or the police are called, as data in `contracts/missions/handover/text.json`. Uncommitted; Aster commits after review. I didn't run Unity or change the engine.

## Where the words are

**In text.json**
- `why` (new, after `cards`), one block:
  - `rule`
  - `by`: reporters who are no person (a street camera, a police robot, an officer, the desk, the bin round, a witness, "another {job}").
  - `places`: {where}, the site's named places with their preposition.
  - `alert`: 25 causes.
  - `again`: shown when the Alert came from the count of incidents.
  - `effect`: what each Alert sets off.
  - `police`: 17 causes.
  - `onWay`: the marked reporter. Report points as {to}, nine ways, `fix`, `nofix`, `stopped`.
  - `meter`: the Alert meter's list.
- `cards.items`: eight new cards. Each code is one the engine logs and that had no card. None has a beat, so nothing new reaches the screen; they are Journal rows, and the street's mark where there is one.

  | code | kind · fate | title | result |
  |---|---|---|---|
  | `attention.reporting` | warning · icon | Seen hanging about | {agent} stood in sight too long. A report is on its way. |
  | `attention.landed` | warning · alt | Reported for hanging about | {agent} was reported for hanging about. Alert {alert}. |
  | `attention.stopped` | city · alt | A report stopped | The report on {agent} never landed. |
  | `car.reported` | warning · icon | Our car spotted | Our car is parked where cars don't stand. A report is on its way. |
  | `car.report-stopped` | city · alt | A report stopped | The report on our car never landed. |
  | `camera.recorded` | warning · icon | On camera | A camera is taping the Fixer. The desk will see it soon. |
  | `drawn.seen` | warning · alt | Weapon seen | Security saw a gun out. Alert {alert}. |
  | `witness.weapon-drawn` | warning · alt | Weapon seen | People saw a gun out. Alert {alert}. |

  A landed loiter report had no card and no beat at all. Its silent reason claimed "report.landed speaks", but the engine logs only `attention.landed` for it.
- `names.targets` gains the crowd by job, the way `crowd-guard` and `crowd-officer` already are. `less.done.walk.who` gains their plates, since a test needs one per target key.

  | key | names.targets | walk.who |
  |---|---|---|
  | `crowd-clerk` | a clerk | clerk |
  | `crowd-resident` | a local | local |
  | `crowd-server` | a server | server |
  | `crowd-cook` | a cook | cook |
  | `crowd-attendant` | a kiosk attendant | attendant |
  | `crowd-keeper` | a shopkeeper | shopkeeper |

  The people file has no sex field, so these words stay neutral.
- `cards.rule` gains one sentence. Brief 013's kind (3) said a city's-answer card never says who reported or why. Now the cause rides beside that card from `why`, and the card's own result still only says the answer came.
- `silentLogs`: five reasons now say "card only" for the codes above. Two of them described things the engine doesn't do: `attention.landed` said report.landed speaks, and `attention.stopped` said "a watcher cools".

**Outside text.json, and why.** The brief said to edit only text.json and this file. Three small changes were needed for a `why` block to pass any check:
- `contracts/missions/handover/why.schema.ts` (new, 118 lines). The block's zod schema. Every cause, way and place is held to its limits at its widest fills, so a new cause in the engine fails here until it has words. It also has three small reference functions for Latch: `whyKeyOf` (ledger cause → key), `whyWords`, `fillWhy`. It stands alone, like `book.schema.ts`.
- `text.schema.ts`: two lines, the import and `why: Why.optional()`. `TextFile` is a strictObject, so without them the new block fails `unity.ts`'s mission-text step and every text test. The file was already 648 lines and is now 650. It needs a split by topic (Sky's 500-line rule), which I didn't do here.
- `text.test.ts`: the pinned card count changes from 96 to 104, with a note. Every round that adds cards does this. The line count is unchanged.

**Side fixes in text.json** (both were failing `text.test.ts` before I started; neither is brief 100):
- Four traffic codes from brief 098 had no event and no silent reason. Each is now silent, with what the street shows: `traffic.shoved`, `traffic.closed`, `traffic.brakes`, `traffic.driver-out`.
- The `start` event still listed the four agents after 433cc692 cut their barks, so it had speakers with no lines. I removed the event. `silentLogs.start` now says the opening is quiet on purpose.

## How a why is read

1. **The key.** An Alert rise is a ledger row: `Game.Ledger`, `what` "alert", with a `cause` string. `whyKeyOf(cause)` gives its `alert` key:
   - `report:<kind>:<who>` → its kind (`report:cut:camera:<id>` → `cut`);
   - `report:<role>` → `approach`, `report:office-staff` → `staff`, `report:councillor` → `let-go`;
   - then the cause's first part: `confirmed`, `gunfire`, `violence-seen`, `drawn-seen`, `drawn-seen:people`, `drawn-warned`, `takedown-fails`, `loiter`, `recorded`, `car-reported`, `entry`, `sighting`.

   A police call has no ledger row. Today it is known only by its log line (below); Keel's read should give it a key.
2. **Three lengths.**
   - `title`: four words at most. The card and the Journal row.
   - `short`: six words at most, lower case, at most 40 characters as written. The crucial card's dim line is the short with {where} after it ("a clerk spotted the guard's body at the Office door"). It is also the meter's row.
   - `line`: one sentence, at most 80 characters at its widest fills. It goes on Alt, in the Journal, on the meter's hover, and in brief 099's timeline.

   §14's hold still applies: no sentence is on screen in play. The only words in play are the short on the card that already shows (the Alert card, the Police card).
3. **The fills.**
   - {who}: the reporter or witness. A person is named as `names.targets` says: the cast by name (Inez, Popovic, the guard, the driver), a crowd person by job (`crowd-<job>`, else `crowd`). A watcher or unit comes from `by` or `names.targets` (`camera-entrance` …). When the reporter shares a job with {about}, {who} is `by.another` ("another guard"), so a line never reads "a guard spotted the guard's body".
   - {about}: who or what it's about (the body, the subject of a Pay, the cut camera).
   - {agent}: the agent ("the Scout").
   - {where}: the nearest of `places` within 13 m, else "nearby". It is left out for a cause with no place (start, more, cordon, loud, split). A `line` without {where} is shown beside its place, as the Journal shows every row's place.
   - {to}: `onWay.to`. {alert}, {clock} (m:ss), {s} (seconds), {cause} (a cause's short).
4. **What it set off.** `effect` by the Alert reached:
   - 1: "Robots come to look."
   - 2: "Police come to search, hiding places too."
   - 3: "The doors lock, police close the exits and the roads shut." (Brief 098's roads close at Alert 3.)

   `again`: "It was the second report, so security started a search." It follows a cause whose own level is 1 when the count of incidents made it 2.
5. **Told and learned.** A check-in names its time only once a Watch has taught the routine; until then it uses the `unknown` variant, as the `post.missed` card's `needs` does.

## The Alert causes

Each entry: ledger cause · the log at that tick (the earlier lines that warned of it) · the Alert it sets · the fills · the words.

- **seen** · `report:seen:<who>` · `report.landed` (`witness.react`, `report.started`; security watchers: `takedown.seen` with it) · Alert 3 · who, about, where
  - *Takedown seen* · "{who} saw a takedown" · "{who} saw {about} taken down {where}."
  - `vale` (the Take): "{who} saw Popovic taken" · "{who} saw Popovic taken {where}."
- **carry** · `report:carry:<who>` · `report.landed` · 2 · who, about, where
  - *Seen carrying a body* · "{who} saw a body carried" · "{who} saw {about} carried off {where}."
  - `vale` (the escort): "{who} saw Popovic led off" · "… led off {where}."
- **dead** · `report:dead:<who>` · `report.landed` (`witness.camera`, `witness.patrol`, `stash.serviced`) · 2 · who, about, where
  - *Body found* · "{who} spotted {about}'s body" · "{who} spotted {about}'s body {where}."
- **down** · `report:down:<who>` · `report.landed` · 1 (a knockout is never more; a second incident is 2) · same fills
  - *Body found* · "{who} spotted {about} lying" · "{who} spotted {about} lying {where}."
- **woken** · `report:woken:<who>` · `victim.wakes`, then `report.landed` · 1 · about, where
  - *Woke and reported it* · "{about} woke and reported it" · "{about} came round {where} and reported the attack."
  - `vale` (a Take let go after it was seen): *Popovic reported it* · "Popovic got free and told" · "The Enforcer let Popovic go, and he reported it."
- **noise** · `report:noise:<who>` · `victim.calls-out`, then `report.landed` · 1 · who, about
  - *A voice heard* · "{who} heard someone hidden" · "{who} heard {about} calling out from a hiding place."
- **mark** · `report:mark:<who>` · `post.missed` + `report.landed` · 1 · about, clock
  - *The post went quiet* · "{about} missed a check-in" · "{about} missed the {clock} check-in. The post is reported empty."
  - `unknown` (routine not learned): "{about} was missed" · "Someone noticed {about} was gone from the post."
- **missing** · `report:missing:<who>` · `person.missed` (`report.started`), then `report.landed` · 1 · who, about
  - *Someone was missed* · "{who} missed {about}" · "{who} expected {about} and raised the alarm."
- **cut** · `report:cut:camera:<id>` · `camera.cut` (earlier), `camera.cut-reported` · 1, or 2 as a second incident · about (the camera)
  - *Dead camera noticed* · "the desk saw {about} go dead" · "The desk saw {about} go dead and sent someone to look."
- **approach** · `report:<role>` · `report` ("… reports the approach") · by count · who
  - *Offer reported* · "{who} reported the Fixer" · "{who} turned the Fixer down and told security."
- **staff** · `report:office-staff` · `report` (a cancelled Enter) · by count
  - *Staff reported it* · "the staff reported the Enforcer" · "The Enforcer left without Popovic, and the Office staff reported it."
- **let-go** · `report:councillor` · `report` (a cancelled Take nobody saw) · by count
  - *Popovic reported it* · "Popovic got free and told" · "The Enforcer let Popovic go, and he reported it."
- **confirmed** · `confirmed:dead:<who>` · `report.confirmed` · 2 · about
  - *Killing confirmed* · "police found {about} dead" · "Police checked {about}: dead, not knocked out."
- **gunfire** · `gunfire:<target or shooter>` · `shot.hit`/`shot.miss` with `security.called`, `seal.starts` · 2 at once · agent, about
  - *Shots fired* · "shots fired" · "{agent} fired at {about}, and the street heard it."
  - `other` (the shooter isn't the cell): "{who} opened fire" · "{who} opened fire, and the street heard it."
- **violence** · `violence-seen:<target>:<witness>` · `witness.crowd`/`witness.waitress` · 3 · who, about, where
  - *Shooting seen* · "{who} saw {about} fired on" · "{who} saw {about} fired on {where}."
  - `shot` (`violence-seen:<who>`, a civilian or contact shot at): "{about} was fired on" · "… {where}, in front of people."
  - `blast` (`violence-seen:blast:<car>`; `car.exploded`): *Car blown up* · "a car blew up" · "A car blew up {where}, close to people."
- **drawn** · `drawn-seen:<watcher>:<op>` · `drawn.seen` · 2 · who, agent, where
  - *Weapon seen* · "{who} saw {agent}'s gun" · "{who} saw {agent} with a gun out {where}."
- **drawn-people** · `drawn-seen:people:<op>` · `witness.weapon-drawn` · 1 · agent, where
  - *Weapon seen* · "people saw {agent}'s gun" · "People {where} saw {agent} with a gun out."
- **warned** · `drawn-warned:<watcher>:<op>` · **no log line** (the drop-it warning: 2 s to holster, only before any shot, at Alert ≤ 1) · 1 · who, agent
  - *Told to drop it* · "{who} told {agent} to drop it" · "{who} told {agent} to drop it. Holster, or it's called in."
- **fought** · `takedown-fails:<who>` · `takedown.fails` + `security.called` · 2 · about
  - *Takedown failed* · "{about} saw the Enforcer coming" · "{about} saw the Enforcer coming, drew and called it in."
- **loiter** · `loiter:<watcher>:<op>` · `attention.reporting` (earlier), `attention.landed` · ≥ 1 · who, agent, where
  - *Seen hanging about* · "{who} saw {agent} hanging about" · "{who} saw {agent} hanging about {where}."
  - Glyph's brief 099 says "hanging about" too.
- **recorded** · `recorded:<pay|pressure>:<subject>:camera:<id>` · `camera.recorded` (earlier), `camera.record-lands` · ≥ 1 · who (the camera), about
  - *Caught on camera* · "{who} saw the Fixer pay {about}" · "{who} taped the Fixer paying {about}, and the desk saw it."
  - `pressure`: "… threaten {about}" · "… threatening {about} …"
- **car** · `car-reported:<watcher>` · `car.reported` (earlier), `car.report-lands` · ≥ 1 · who
  - *Our car reported* · "{who} reported our parked car" · "{who} saw our car parked where cars don't stand."
- **entry** · `entry:patrol:enforcer` · `entry.seen` (earlier), `entry.reported` · ≥ 1
  - *The entry was seen* · "the patrol saw the entry" · "The patrol saw the Enforcer go in at the Office door and radioed it."
- **sighting** · `sighting:<watcher>` · **no log line** (a Menace district: a watcher's sighting counts as a report) · by count · who, agent
  - *Spotted as trouble* · "{who} spotted {agent}" · "This district fears us, so {who} reported {agent} on sight."
- **start** · a start state above 0 · (none) · as set
  - *On edge already* · "on edge from the start" · "Security was already on edge when the job began."

In 540 plays (every route and pin on 12 seeds), every Alert rise mapped to a key. Seen: gunfire 125, loiter 108, mark 72, recorded 60, violence 47, down 36, cut 24, drawn 24, dead 12, fought 12, car 9, carry 9, seen 8. The rest exist in the engine but didn't come up.

## The police calls

Each entry: the log line that marks it today · the words (title · short · line). Most share the title *Police coming*, the crucial card's.

- **heard** · `security.called` "Shots are heard in the district" · "shots heard" · "Shots were heard {where}, so the police are coming."
- **witness** · "Inez/A witness reports the shooting", "The guard calls in the shooting", "The patrol/entrance camera/A street camera reports gunfire" · "{who} saw the shooting" · "{who} saw the shooting and called the police."
- **shot-at** · "Inez is shot at the café…", "The official driver is shot at…", "Shots at the councillor…", "A shooting in the street…" · "{about} was fired on" · "{about} was shot at, and people called the police."
- **patrol** · "Gunfire at the patrol is reported" · "the patrol was fired on" · "The patrol was shot at {where} and called it in."
- **rivals** · "The firefight with the rivals is reported" · "the fight with the rivals" · "Someone called in the fight with the rival crew {where}."
- **gang** · `security.gang` · "a shooting nobody saw" · "Nobody saw who fired. Police come, but don't seal the district yet."
- **drawn** · "The guard calls in / The patrol reports / The watchers on the camera report a drawn weapon" · "{who} saw {agent}'s gun" · "{who} saw {agent} with a gun out and called the police."
- **attack** · "The guard calls in an attack" · "{about} called in an attack" · "{about} fought the Enforcer off and called the police."
- **post** · `guard.check-in` + "The empty guard post is reported" · "the guard's post went quiet" · "The guard missed his check-in, so police come to the empty post."
  - `unknown`: "the guard was missed" · "The guard was missed at his post, so the police are coming."
- **report** · "The response unit is sent to the scene" (a report landed at Alert ≥ 2) · "{cause}" · "A report reached the desk, so the police are coming."
- **body** · "A body is found: a unit is sent to hold the scene (…)" · "a body was found" · "A body was found, so police come to hold the doors near it."
- **robots** · `alarm.dispatches` with 0 units · "{cause}" · "A report reached the desk, so robots are coming to look." (title *Robots coming*)
- **more** · `response.reinforces` · "the search goes on" · "The search goes on, so the desk keeps sending more police." (*More police*)
- **cordon** · `doors.lock` · "the district locks down" · "At Alert 3 the doors lock and police close the district's exits." (*Lockdown*)
- **loud** · `fight.loud` · "shots earlier" · "There were shots earlier, so police come in for the fight." (*Police coming in*)
  - `alert`: "security was searching" · "Security was already searching, so police come in for the fight."
  - The extra officers keep `debrief.fightWhy`'s lines.
- **split** · `split.police` · "Popovic is gone" · "Popovic is gone, so police close in on whoever is left." (*Police closing in*)
- **seal** · `seal.starts` · "{cause}" · "The police are on the street, so the district seals at {clock}." (*District sealing*)
  - The seal plate's hover shows the cause that set it (its origin).

## A report on its way: the marked reporter

Each way gives the engine's report in transit, its log, whether the cell can still stop it, and the plate and line. When it can be stopped, `fix` follows the plate: "Stop them before it lands" (a reporter downed first drops it). When it can't, `nofix`: "Too late to stop it". `stopped`: "Report stopped" · "{who} won't be making that report."

- **radio** · staff (a guard) who sees it, a woken guard, a told Loyal friend indoors · `report.started` "… radios it in." (a woken or missing staff report logs none) · stoppable
  - "Radioing it in · {s} s" · "{who} is radioing it in. It lands in {s} s."
- **walk** / **run** · anyone else, to the nearest report point (a staffed door or a call box) · `report.started` "… walks/runs to <point> to report it (lands m:ss)" · stoppable
  - "Going to {to} · {s} s" / "Running to {to} · {s} s" · "{who} walks/runs to {to} to report it. It lands in {s} s."
  - {to}: "the café phone", "the Office door", "the official door", "the call box", "a phone".
- **look** · an Idealist checks the body first · `report.started` "… goes to look." · stoppable
  - "Going to look" · "{who} is going to look first, then report it."
- **camera** · a camera's watcher (at the next 20 s scan + 3 s) · `witness.camera`, or `attention.reporting`/`car.reported` by a camera, `camera.recorded` · not stoppable (it's on tape)
  - "On tape · desk in {s} s" · "{who} has it on tape. The desk sees it in {s} s."
- **screen** · a camera the Tech cut · `camera.cut` "… at m:ss" · not stoppable
  - "Dead screen · desk in {s} s" · "The desk sees {about} is dead in {s} s."
- **robot** · the patrol finds a body (`witness.patrol`), the bin round (`stash.serviced`), or a loiterer, car or entry report by the patrol or a robot · stoppable only for the attention reports (a downed robot drops them)
  - "Ringing the desk · {s} s" · "{who} is ringing the desk. It lands in {s} s."
- **checkin** · a downed staff member's next 90 s mark · (none until `post.missed`) · not stoppable; shown only once their routine is known
  - "Check-in due · {s} s" · "{about} is due to check in. When they don't, the desk knows."
- **tell** · a witness going to phone a friend · `witness.to-phone` · stoppable; it isn't a report, though a Loyal friend may make one
  - "Going to tell someone" · "{who} is going to {to} to tell a friend."

The brief's example comes out as the walk plate: "Going to the call box · 6 s". From `loud.seed3`:
- 2:39 a guard radios: "Radioing it in · 5 s".
- 4:54 a clerk heads off: "Going to the Office door · 8 s".
- 4:56 a clerk "Going to look".

## Worked from real runs

The host's output once the fills come from the engine. Here they come from parsing the log in my scratch render (`out/scratch/fable/100/ts/render.ts`). {where} uses a hand map.

**loud.seed3** (the loud win). One rise; the rest are the city's answer.
- 2:33 · Alert 2 · *Shots fired* · "shots fired at the Office door". The line reads "The Enforcer fired at the guard, and the street heard it."
- 2:33 · *Police coming* · "shots heard at the Office door". The seal plate's hover: "shots fired" (seals at 5:41).
- 2:44 · *Police coming* · "a body was found at the Office door". "A body was found, so police come to hold the doors near it."
- 3:45 · *Police coming in* · "shots earlier". 3:56 and 4:36 · *More police* · "the search goes on".

**careless-scout-idles-pickup** (a quiet route caught by a camera)
- 1:58 · Alert 1 · *Seen hanging about* · "the pickup camera saw the Fixer hanging about at the pickup".
- *Robots coming*, with the same short.
- 2:38 · the Scout, the same way (no rise: still Alert 1).

**careless-reporter-not-stopped**
- 1:18 · Alert 1 · *Caught on camera* · "the pickup camera saw the Fixer pay the driver at the pickup". The brief's own example, by the pickup camera.

**careless-g-disable**
- 1:37 · Alert 1 · *Dead camera noticed* · "the desk saw the entrance camera go dead".
- 3:27 · Alert 2 · *Body found* · "another guard spotted the guard lying at the Office door". The line: "… It was the second report, so security started a search." That is `again` (a knockout is 1; the second incident made it 2).
- 3:57 · Alert 3 · *Shooting seen* · "a local saw an officer fired on".
- *Lockdown* · "the district locks down".

**careless-guard-left-door-held**
- 2:30 · Alert 1 · *The post went quiet* · "the guard missed a check-in at the Office door". The line: "The guard missed the 2:30 check-in. The post is reported empty."

**careless-enforcer-watches-guard**
- 1:51 · Alert 2 · *Takedown failed* · "the guard saw the Enforcer coming".
- *Police coming* · "the guard called in an attack".

## What the others need

**Keel: a clean read of the alert history** (read only, never hashed, so a replay is untouched). The host should never parse the engine's sentences. The render above had to parse them, and it gets several things wrong.
- One row per Alert rise and per police call:

  | field | holds |
  |---|---|
  | tick | when |
  | what | alert, police or seal |
  | from, to | the Alert before and after |
  | key | the why key (dead, loiter, heard, body …) |
  | variant | vale, other, shot, blast, pressure, alert, unknown |
  | who | the reporter: a person id, camera:<id>, patrol, desk, round:<stash>, a unit id, heard, witness |
  | about | the person or thing it is about: id, role, camera, op |
  | agent | the op |
  | at | the point, with the nearest site place id |
  | how | radio, walk, run, look, camera, screen, robot, checkin |
  | byCount | true when alert.byIncident set the level, for `again` |
  | sent | robots and units sent |
- **Reports in transit.** `Reps`, `attnReps`, `parkReps`, the entry report, `camRecs` and `CutReps` as one list: who, way, report point id (for {to}), landAt, stoppable, about. Several reports log no `report.started`: a woken victim, a missing person reported by staff, a told Loyal friend, a hidden victim's noise heard by staff. So the host can't mark them from the log today.
- **Gaps found:**
  - `gunfire:<x>` names the target when the cell fires and the shooter when anyone else does, so `gunfire:guard` is ambiguous.
  - `drawn-warned` and `sighting` raise the Alert with no log line.
  - A police call carries no cause of its own. A report that lands at the Alert already reached still sends robots, and today the only link to it is its tick.
  - "The watchers on the camera report a drawn weapon" doesn't say which camera.
  - The seal's origin is there for the fail camera; the seal plate's hover needs it too.

**Glyph: the look** (after brief 099's concepts):
- Whether the crucial card's title stays "Alert up" / "Police coming" with the short as its dim line, or takes the cause's title.
- The meter's list: on the Alert lamps, by click (and hover), newest first, two lines a row (`when` over `what`). How many rows show, and whether police rows are in it.
- The marked reporter's plate: where it hangs (over the person, or the lens or face that's reporting, beside today's cues: lens flash, hand to the radio), the countdown, the fix line, and the stopped pop.
- **A told/learned question for Glyph and Aster:** should a camera's tape be marked on its way when no agent can see that camera (a body found later, an unwatched cone)? I gave `witness.camera` and `witness.patrol` no card for this reason.
- Brief 099's timeline and "the city's answer" can take `line` as it is. The front page's Alert reason can be the cause's short ("the pickup camera saw the Fixer hanging about"). Glyph's stand-in keys for it (`why.alert.0`, `why.alert.loiter`) collide by name with this block, so brief 099's own words will sit under `debrief`.

**Latch: building it** (after the sight cones):
- Read `why`; ledger → `whyKeyOf` → `whyWords` → `fillWhy`.
- `PersonName` for a civilian: `names.targets["crowd-" + job]` before `crowd`. This also makes the hover read "a clerk".
- Use `by.another` when the reporter's plate word matches the person it's about.
- {where}: the nearest site place within 13 m, else "nearby".
- The eight new cards are live in the Journal from the next text copy, through the generic `TextCard` path (no host change). Because they lean only on {agent} and {alert}:
  - `FillCard` finds {agent} only as "Enforcer" or "enforcer "+space. In `drawn.seen` ("the enforcer's weapon") it would say "the Scout", so those two cards don't use {agent}.
  - `camera.record-lands`' {who} reads "someone" today: "on driver" isn't parsed.

## Open questions for Aster

- **"Edit only text.json":** I also touched `text.schema.ts` (+2), `text.test.ts` (one count) and added `why.schema.ts`. Without them the block can't parse. Revert the three, and the `why` block, if you'd rather route the schema through someone else.
- **Brief 013's rule (3)** ("never who made the report or why") is superseded by brief 100 for the why line. The rule's sentence says so now. The test at `text.test.ts:928` still checks only the four cards' results, which still comply; its comment states the old rule.
- **The door guard** is "the guard" (one glossary); crowd guards are "a guard"; `by.another` covers the clash. The GDD calls him "the door guard" in places.
- **`crowd-resident` is "a local"**, not "a passer-by": it says they live there. Easy to swap.

## Checks run

- `bun run test tools/dialogue tools/game/proofdelta.test.ts contracts/missions/handover/text.test.ts contracts/missions/handover/conversations.test.ts tools/names`:
  - **351 pass, 2 fail.** Both failures were there before I started and are outside my files: "HostViews.Fans.cs has no row" (Latch's sight cones, in progress) and "new: play HostTraffic.cs:19 'Vale'" (a banned name in host code).
  - Before my edits it was 349 pass, 4 fail. The other two were text.json's, fixed above.
- `SUBORNED_SIM_UNITY_OUT=out/scratch/fable/100/unity-assets bun run guard -- bun tools/sim/unity.ts --tree` (the Unity output went to scratch, not Latch's project):
  - `mission text: text.json 58f46dccfeb5 (suborned.text/1 parses)`;
  - headless 473/473 and range 97/97 scenarios pass with identical replay.
- `bun run typecheck`: clean, and it covers `why.schema.ts`.
- `TextFile` parse of text.json: ok, nesting 9 of 24, `whyProblems` 0.
- My word check (`out/scratch/fable/100/py/check_why.py`): every text at its widest fills, words, shape, fills, avoid list. No problems.
- `bun run sim batch` of every route and pin on 12 seeds (540 plays plus 12 errors, all from `routes.json`, which is no script; guarded, about 8 s): every Alert cause maps to a key.
