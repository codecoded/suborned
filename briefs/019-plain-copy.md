# Brief 019: plain copy, a pass over every player-facing line

Owner: Fable. Asked by Sky (2026-10-18): "while the loop holds" reached the HUD as a refusal ("I thought we had a copywriting skill for this kind of thing"). Source of the rules: the ux-copy skill, pasted below because crew agents cannot load it. Memory: plain-copy, less-reading.

Scope: everything the player reads in `contracts/missions/handover/text.json` (refusals, hints, order cards, story beats, pops, goal line, end card, debrief, Intel words). People's speech keeps its voice unless it carries rule jargon. **Every text key is unchanged**; the HUD picks up the new words with no code change, except one optional addition (see "Handoff", item 1).

## The five rules (apply to every line)

1. **Clear.** No design jargon. The player's words, not ours: "Busy looping", not "while the loop holds"; "A fight blocks the car", not "car held". No Trail, Alert or Standing unless that is a name the player has been shown on screen.
2. **Concise.** Pops 3 to 4 words. Hints 6 words or fewer. Alt lines one sentence. Cut before you add (Sky: "the reading, it's too much").
3. **Consistent.** One glossary, the same name for the same thing everywhere (below).
4. **Useful.** A refusal says what happened, why, and how to fix it ("Too far · walk within 4 m").
5. **Human.** Plain, warm, never cute. No exclamation marks.

## Glossary (one name per thing)

Names the player has been shown on screen (kept): **Trail** (the T counter on the status pill; "Trail" on the end tally), **Alert** (the 0 to 3 count; "Alert 0 of 3" on the brief card). Standing is never shown by that name (the debrief shows the district's band: Hero, Neutral, Menace), so it does not appear in any line.

| Thing | Say | Do not say |
|---|---|---|
| The four people you command | **the cell**; each one an **agent**: Scout, Fixer, Tech, Enforcer | operative, op, asset, unit |
| What agents do | Watch, Listen, False tip, Ask, Pay, Help, Pressure, Call, Loop camera, Reroute patrol, Enter the Office, Take Vale, Fire, Draw weapon, Holster, Cease fire, Carry, Take down (Knock out, Kill), Put down, Hide body, Tie up, Walk, Run, Cancel, Withdraw | extract (retires into Board and Go), tether, hold, engage |
| The four ways to turn someone | **approach**: Ask, Pay, Help, Pressure | tier, leverage, vector |
| A person you won | **turned**; you **Call** them and they act | contact, asset |
| What the Scout learns | **routine** (Watch), **need** and **temperament** (Listen), **what a camera sees** | sight tier, profile |
| Where things are | **the Office**, **the café**, **the public door**, **the control cabinet**, **the pickup** (where Vale's car waits), **the service lane**, **the square** | the bay (in speech it stays; people say it), the feed, the desk |
| Cars | **the getaway car** (ours; "the car" on its own means this one), **Vale's car** (his, with the driver) | extraction car, held car |
| Cameras | **entrance camera**, **pickup camera**, **mast camera**; a person watches each one live. **Loop** = the camera replays an empty minute. A **cone** is the wedge a camera, patrol or guard sees along (drawn in the street) | feed, lens (speech only), sight |
| Security | **Security** (all the watchers together), **the police** (officers and robots sent when the alarm goes up), **the patrol** (the clockwork patrol), **the rivals** (the rival crew) | response unit, hostile (except the Fire target, where it is the on-screen name) |
| People down | **down** (an agent hit), **knocked out**, **dead**, **tied**, **hidden** (in a bin or behind the service door) | incapacitated, stashed, restrained |
| Busy | **busy** (an agent mid-order), **looping** (the Tech keeping a camera looped) | tethered, locked to the cabinet |
| Being stopped | **a fight blocks the car**; **stop the loop** | car held, loop holds |
| Cash | **cash**, **$** (the brief card's label "Purse" stays; it is the card's own) | funds, budget |
| Evidence and alarm | **Trail** (+n on a price), **Alert** (1 to 3) | heat, noise, exposure |
| The end of the run | **Vale is in the car**, **withdrew**, **failed** | extracted, aborted, terminated |

Arriving with brief 018 (Keel is building the engine side; **no lines written yet**), so the words are fixed now:

| Term | Meaning, in the player's words | Use it as |
|---|---|---|
| **the car** | the getaway car | "the car", never "the vehicle" |
| **Board** | get into the getaway car (1 s; 4 s when carrying someone). Replaces "Vale into the car" | order name, "Board 1 s" |
| **Go** | the car leaves now with whoever is inside; this ends the mission | order name, "Go" |
| **left behind** | an agent who is not in the car when it leaves | "left behind" |
| **captured** | a left-behind agent the city takes: down, or still in the fight, or the district sealed | "captured" (already used) |
| **walked out** | a left-behind agent who gets away on foot | "walked out" (not "escaped", "released") |

## Flagged lines (plain wording that might change the meaning)

1. **`locks.run-tethered.line`** is Sky's exact words: "…Stop the loop to move him." The refusal also fires for Watch; the pop (`Busy looping`) covers both, the Alt names only moving. Fine to leave; say if you want "to move or Watch".
2. **`locks.only-scout`** is one engine code for three cases: a person asked to be watched or listened to by a non-Scout (pop "Scout only"), a camera or the patrol asked to be watched by the Fixer or Enforcer (Sky's "Scout or Tech"), and a false tip by a non-Scout. A text file can hold only one line per key, so the base line is the person case and the machine case is a new optional `byTarget.machine` on the same lock. **Until Latch reads it, the Fixer or Enforcer on a camera sees "Scout only", which is untrue (the Tech can).** Handoff 1.
3. **`briefing.purse.line`**: "learning what they need costs less" became "Watching and listening are free". True now (they cost time, $0); wrong if a later mission prices Listen. Motive to confirm.
4. **`cards.items.carry.body.result.other`**: "Only the Enforcer carries at a walk" became "The Enforcer is faster". The rulebook has the untrained carry pace at 0.7 m/s; confirm the Enforcer's is higher.
5. **`locks.needs-standing`**: now "The district doesn't trust you enough yet." It gives no fix, because the fix (acts seen, word of mouth) is not one order the player can give. The rule's owner (Motive) should say what the player can do.
6. **`debrief.assets.driver-alters-pickups`**: "who can alter pickups" is now "who can change a pickup". The asset's meaning is not obvious from either; Motive to say what the driver does in a later mission and I will write it.
7. **`debrief.between.grief` and `.worry`** (not changed): the host fills `{move}` and `{fear}` with "bravery +1" and "fear +1" (the samples in `contracts/dialogue/dialogue.rules.json` `debriefFill`). Those are stat words. They need plain values from the code that fills them ("braver", "more afraid"). Handoff 2.
8. **Trail and Alert stay** in prices, tallies and results ("Alert 2", "+2 Trail"), because both are on screen. I only removed them where they were unclear ("several acts left Trail pointing to the cell" is now "several acts point back to the cell").
9. **"Stop the loop"** is Sky's word; the button that does it is **Cancel**. Cancel's own card says "The loop ends and the camera sees again", so it reads right. Not changed.
10. **`targets.security`** is now "the police" (was "the response unit"), which matches the speaker name "Police". The target also covers robots; the `about` line says so.
11. **Speech** stays in voice. One line carried rule jargon and changed: Inez, loyal, "Help is still an approach. I must log it." is now "Kind or not, I have to write it down." Left as speech on purpose: "bay" (Scout, Driver, Tech), "cone" (Scout, Tech), "Contact." (police, soldiers' word), "Alert. Alert." (the patrol).

## What changed, before → after

### Refusals (`locks`; Alt is the long line, pop is the short)

| Where | Before | After |
|---|---|---|
| `enforcer-inside` Alt | The Enforcer is inside the Office. Wait at the service lane for the Enforcer. | The Enforcer is inside the Office. Wait for him at the service lane. |
| `enforcer-inside` pop | Inside | Inside the Office |
| `guard-down` Alt | The guard is down. There is nobody left to draw from the door. | The guard is down, so there is no one to call off the door. |
| `run-tethered` Alt | The Tech is keeping the camera looped. Cancel the loop to move or Watch. | The Tech is keeping the camera looped. Stop the loop to move him. |
| `only-scout` Alt | Use the Scout for this. The Tech can also Watch cameras and the patrol. | Only the Scout can watch or listen to people. |
| `only-scout` pop | Use the Scout | Scout only |
| `already-turned` Alt | {who} is already turned. Help keeps them on side; Call puts them to work. | {who} is already turned. Call them, or Help to keep them on side. |
| `driver-too-late` Alt | Too late to turn the driver. The cutoff was {tick}. | Too late to turn the driver. The last chance was {tick}. |
| `car-busy` Alt | Vale's car is still moving until {tick}. Call again then. | Vale's car is still moving. Call again at {tick}. |
| `loop-wrong-target` Alt | The cabinet loops the entrance camera only. | The cabinet can't loop that. Pick a camera. |
| `loop-wrong-target` pop | Wrong camera | Can't loop that |
| `loop-holds` Alt | The camera is already looping. Nothing more to do. | A camera is already looped. Only one loop at a time. |
| `already-drawn` Alt | The weapon is already out. Give a fire order. | The weapon is already out. Fire on a hostile. |
| `civilian-target` Alt | That's a civilian or a contact. Hold Ctrl to fire anyway; the city remembers. | A civilian or someone you've turned. Ctrl fires anyway, and the city remembers. |
| `carry-names-downed` Alt | Carry needs a downed agent to lift. | Pick a downed agent or a body to carry. |
| `carry-names-downed` pop | Nobody to lift | Nothing to carry |
| `needs-standing` Alt | The district doesn't see you that way yet. | The district doesn't trust you enough yet. |
| `needs-scale` Alt | {who} isn't ready for this yet. | {who} isn't ready for a request this big. |
| `needs-scale` pop | Too big yet | Not ready yet |
| `body-stashed` Alt | {who} is hidden or already carried. Not on open ground. | {who} is hidden or already being carried. |
| `doors-locked` Alt | Lockdown: the Office doors are locked. Nobody goes in. | The Office is locked down. Nobody can go in. |
| `only-scout` machine case (new, optional `byTarget.machine`) Alt | (none) | Only the Scout or the Tech can watch cameras and robots. |
| `only-scout` machine case (new) pop | (none) | Scout or Tech |

Reviewed and kept: the other 89 refusals already say what, why and how to fix. Every pop is 3 to 4 words (the numbered ones count the number as a word).

### Hints (`hints.items`; the short form shown at rest is already 6 words or fewer for all 17, so only the Alt line moved)

| Where | Before | After |
|---|---|---|
| `hints.items.H14.line` | The way is clear. The Enforcer can Enter and goes off screen. Seen on the way in, it fails. | The way is clear. The Enforcer can go in, but if he is seen, it fails. |
| `hints.items.H16.line` | The driver is turned and Vale is at the car. The Enforcer can Take Vale. Anyone watching ruins it. | The driver is turned and Vale is at the car. The Enforcer can Take Vale. Don't be seen. |

### Order cards (`actions`, `cancelCosts`, and the result line of each scene)

| Where | Before | After |
|---|---|---|
| `actions.loop.does` | Make the entrance camera repeat one minute. The Tech can't leave until it ends. | Loop a camera for a minute. The Tech must stay at the cabinet. |
| `actions.loop.onTarget.cabinet` | Work the cabinet: the entrance camera repeats one minute. The Tech stays here. | Loop a camera from the cabinet for a minute. The Tech stays here. |
| `actions.loop.onTarget.camera-entrance` | Make this camera repeat one minute. The Tech stays at the cabinet until it ends. | Loop this camera for a minute. The Tech stays at the cabinet. |
| `cancelCosts.lost` | The time is lost and nothing happens. A lift, drop, hiding or knot isn't done. | The time is lost and nothing happens. A lift, drop, hiding or tying isn't done. |
| `actions.watch.onTargetBy.tech.camera-pickup` | Study the lens on the bay: its cone, its blind side. Needs a clear line. | Study the lens on the pickup: its cone, its blind side. Needs a clear line. |
| scenes (1×) | A passer-by is calling it in. Expect Alert to rise. | A passer-by is calling it in. Alert will rise. |
| scenes (1×) | The Office is changing shift. The driver is ready at the bay. | The Office is changing shift. The driver is ready at the pickup. |
| scenes (11×) | {who} is turned. Call {who} when you need the guard moved. | {who} is turned. Call {who} to pull the guard off the door. |
| scenes (11×) | {who} is turned. Call {who} when you need the pickup camera blinded. | {who} is turned. Call {who} to blind the pickup camera. |
| scenes (8×) | {who} is warming to you but is not yet turned. | {who} is warming to you, but isn't turned yet. Try once more. |
| `actions.loop.onTarget.camera-pickup` (new) | (none) | Loop this camera for a minute. The Tech stays at the cabinet. |
| `actions.loop.onTarget.camera-mast-314` (new) | (none) | Loop this camera for a minute. The Tech stays at the cabinet. |

The engine already loops the entrance camera, the pickup camera and the city's own cameras (rulebook `cell.loop.targets`), so the old "the entrance camera only" line was out of date; `locks.loop-wrong-target` is fixed in the refusals table.

### Mast camera (Sky's note: name, Scout and Tech Watch lines)

Already there: `names.targets.camera-mast-314` "the square's mast camera", `targets.camera-mast-314` (name "Mast camera", about, watched), `actions.watch.onTarget` and `onTargetBy.tech`. Missing, now added: the Scout's and the Tech's spoken read when a Watch on the mast camera lands (`dialogue`, event `read`, verb `watch`, about `camera-mast-314`), three lines each, and `camera-mast-314` in `WATCHED` in `text.schema.ts` (without it the schema refused the entries).

- Scout: "Mast camera. It covers the square." / "It turns between three views. I have the square." / "The mast pans. The square is its whole job."
- Tech: "Mast camera. Three views, one lens." / "It pans the square. I have the turns." / "One lens on a slow pan. I know its views."

### Targets (what the cell is told or learns)

| Where | Before | After |
|---|---|---|
| `names.targets.security` | the response unit | the police |
| `targets.security.name` | Response unit | Police |
| `targets.camera-entrance.about` | On a pole by the public door. A person watches the feed. | On a pole by the public door. A person watches it live. |
| `targets.camera-pickup.about` | On a pole by the bay. A person watches the feed. | On a pole by the pickup. A person watches it live. |
| `targets.camera-mast-314.about` | On a mast over the square. A person watches the feed. | On a mast over the square. A person watches it live. |
| `targets.camera-pickup.watched` | Its cone covers Vale's car at the bay. It never turns. | Its cone covers Vale's car at the pickup. It never turns. |
| `targets.driver.about` | Vale's official driver. Waits at the bay. Short of cash, they say. | Vale's official driver. Waits at the pickup. Short of cash, they say. |
| `targets.driver.watched` | Waits at the bay with the car, and moves it when Vale comes out. | Waits at the pickup with the car, and moves it when Vale comes out. |
| `targets.guard.watched` | Holds the door and the steps, and leaves them for his coffee. | Stands at the door and steps, and leaves them for his coffee. |
| `targets.waitress.heard` | In debt, and {temperament}. The door guard is her secret. | In debt, and {temperament}. Her secret is the door guard. |

### Briefing card

| Where | Before | After |
|---|---|---|
| `briefing.purse.line` | Cash: $100,000. Paying people costs a lot; learning what they need costs less. | Cash: $100,000. Paying people costs the most. Watching and listening are free. |
| `briefing.fails.engine-error` | Something broke. This run doesn't count. | Something broke. This run doesn't count. Start again. |
| `briefing.cell.enforcer` | Enforcer: goes in, takes Vale, takes people down. Only the Enforcer carries Vale. | Enforcer: goes in, takes Vale, takes people down. Only he carries Vale. |
| `briefing.cell.fixer` | Fixer: turns people (Ask, Pay, Help, Pressure) and Calls them to act. | Fixer: turns people (Ask, Pay, Help, Pressure) and can Call them to act. |
| `briefing.fails.sealed` (end card; wrapped to 3 lines at 340 px, Latch's layout audit) | The district was sealed off before Vale reached our car. | Sealed off before Vale reached the car. |


Also done for the coordinator: `briefing.cell.enforcer` was 81 characters (the schema maximum is 80) and broke every test in `tools/levels/dialogue/test/dialogue.test.ts`; it is now 71.

### Feed cards (`cards.items`: the title and result players see in the history and on Alt)

| Where | Before | After |
|---|---|---|
| `cards.items.report.landed.title` | A report reached the desk | Security got a report |
| `cards.items.report.landed.result` | The desk has it. Alert {alert}. | Security has it. Alert {alert}. |
| `cards.items.post.missed.title` | A missed check-in | The post went quiet |
| `cards.items.response.reinforces.title` | Another unit | More police |
| `cards.items.response.reinforces.result` | Another unit is coming from the north street. | More police are coming from the north street. |
| `cards.items.run.seen.title` | Runner seen | Seen running |
| `cards.items.carry.body.result.other` | {agent} is carrying {who}, slowly. Only the Enforcer carries at a walk. | {agent} is carrying {who}, slowly. The Enforcer is faster. |

Test pin from this table: a card title may not carry a hyphen (the test reads it as an id), so "A missed check-in" became "The post went quiet".

### Story beats, pops, goal line, end card, Intel

Reviewed, **no change needed**: every card and scene `beat` (18 beats, all 2 to 4 words), `less.pop` (18), the goal line (`briefing.tracker`, `less.briefing.goal`, the three `less.objective` lines), the end card (`less.end` win, withdraw and 12 fails, the tally), and the Intel words (`less.intel`: 8 needs, 4 temperaments, 4 secrets, 3 verbs). They were already fragments in the player's words. Refusal pops are in the refusals table above.

### Debrief

| Where | Before | After |
|---|---|---|
| `debrief.causes.several-interventions` | several acts left Trail pointing to the cell | several acts point back to the cell |
| `debrief.causes.few-leads` | a little Trail | a few small traces |
| `debrief.assets.driver-alters-pickups` | The driver, who can alter pickups. | The driver, who can change a pickup. |

Reviewed and kept: headline, result, tally, security and investigation lines, the route and standing lines ("Strangers started a street fight."). The standing line "{district}: {band} {to}, was {from}" shows signed numbers beside Hero, Neutral or Menace; readable with the icon, but the best candidate for a Glyph pass.

## Counts (lines changed)

| Area | Lines changed | Reviewed |
|---|---|---|
| Refusals | 21 (16 Alt, 5 pop) + 2 new | 105 |
| Hints | 2 (Alt) | 17 long + 17 short |
| Order cards | 5 + 2 new, and 32 scene result lines (5 distinct) | 28 verbs, 106 scenes |
| Targets | 10 | 18 |
| Briefing | 4 | 48 |
| Feed cards | 7 | 52 |
| Beats, pops, goal line, end card, Intel | 0 | 18, 18, 5, 14, 19 |
| Debrief | 3 | 70 |
| Speech | 1 changed, 6 new (mast camera) | 446 dialogue entries |
| Total | 85 changed (the 32 scene result lines are 5 distinct strings), 10 new | |

## Tests and files

- `contracts/missions/handover/text.json`: the edits above.
- `contracts/missions/handover/text.schema.ts`: `Lock.byTarget` (optional, additive) and `camera-mast-314` in `WATCHED`.
- `contracts/missions/handover/text.test.ts`: the pins that held old wording now hold the new, and nothing else. Five were already stale before this pass (the file did not load at HEAD, so they had not run): the summon-too-far fill, the going-loud loss line, the fight.loud card, the timeline mark "Last chance" (the test said "Last turn", which reads as rotating the view), and the car-held pop and refusal (the test pinned "Car held"). One is mine: H14 (no "off screen", now "if he is seen, it fails"). Also cleared by this pass: 15 lines over 80 characters, "Calls" capitalised mid-sentence in the Fixer's role line, and the mast camera's Watch line (the schema did not list it).
- Word budget: no pop or hint short grew; the HUD budget check in `bun run verify` reports all 45 shots within budget.

## Handoff

1. **Latch** (C#, one line in `HostLess.ShortOf` and the Alt lookup): when the code is `only-scout` and the order's target is a camera or the patrol, read `locks['only-scout'].byTarget.machine` (`line` for Alt, `short` for the pop) instead of the base. Or **Keel**: return a second code for the machine case and I add a key (the text test pins the lock keys to the engine's codes, so this needs both at once).
2. **Motive / Keel**: plain values for `{move}` and `{fear}` in the between-missions debrief ("bravery +1" is a stat).
3. **Brief 018**: when Keel's engine lands I write Board, Go, left-behind and captured lines against the glossary above (H11 reword, the Go card, captured and walked-out flags, the end-card fate lines). Not written now.
4. **Whoever finishes last on the rulebook** (Keel, Pulse): `bun run verify` is not clean until the dialogue and people drift ledgers declare the current state (`bun tools/levels/dialogue/drift.ts declare "<what changed>" --rules none`). My part of that hash is the two edits to `text.schema.ts`. Also `tools/levels/dialogue/test/dialogue.test.ts` (lexicon scope, D-11) now needs `debriefFill` widths in `contracts/dialogue/dialogue.rules.json` for the rulebook's new debrief fields `fate`, `held-at`, `holds`, `rescue-until`, `by` (Keel's brief 018 additions); not mine to guess.

## Part 2: the getaway words (brief 018, engine 0.7.0)

Written on the same rules and glossary. `extract` is gone; Board and Go replace it. Keys that moved: actions `board` and `go`; locks `board-running`, `board-tethered`, `in-car`, `car-empty` added, `no-vale-yet` removed, `car-held` and `stand-at-getaway` now belong to Go and Board.

| Where | Line |
|---|---|
| action `board` (row, Alt) | Board · Get into the getaway car, with anyone you carry. · on the car: Get in the car, with anyone you carry. In the car you can't fire. |
| action `go` (row, Alt) | Go · Drive off with whoever is in the car. The rest stay behind. · on the car: Leave now. Anyone outside the car stays behind. A hostile near it blocks it. |
| `car-held` pop / Alt | Blocked · down {holders} / The car is blocked by {holders}. Down them, or drive them off, then Go. |
| `car-empty` pop / Alt | Car is empty / Nobody is in the car. Board first. |
| `board-running` pop / Alt | Stop running first / {op} is running. Stop first, then Board. |
| `board-tethered` pop / Alt | Busy looping / The Tech is keeping the camera looped. Stop the loop to Board. |
| `in-car` pop / Alt | In the car / That agent is in the car. Walk them out first. |
| `stand-at-getaway` pop / Alt | Too far · {m} m / Too far from the car. Walk within {m} m, then Board. |
| Go card (`less.go`) | Go (nobody left) · Go · leaves {list} · "Tech (down), Scout" · +{trail} T if seen · roster flag "Left behind" · glyph Alt "{who} would be captured." or "{who} would walk out." · brackets: down, fighting, sealed |
| goal line | Drive off in the [car]. (new `less.objective.go` and step `go`, after "Get [Vale] to the [car].") |
| hint H11 (Alt) | Anyone not in the car stays behind. Select another agent and right click a downed one to carry them. |
| hint H15 (verb now `board`) | Board the [car] with Vale. · Alt: Vale is out. The Enforcer can Board the car with Vale. |
| hint H18 (new, priority -5) | Go: drive off in the [car]. · Alt: Vale is in the car. Any agent can Go: right click the car. Anyone outside stays behind. |
| going-loud card | Anyone left behind in a fight is captured. |
| briefing | card: We take him alive, get him into our getaway car and drive off. · win: Vale in the getaway car when it leaves. · target Getaway car: Our car. Get everyone in, then Go. |
| feed cards | `fight.blocker` (beat "One takes the road."), `car.seen`, `board.in`, `board.out`, `walked` |
| speech | acks for Board and Go (4 agents each); the blocker heard from the Scout ("One's heading for the road."), the rivals ("Take the road. Hold the car.") and police ("Unit one, take the road."). Silent on purpose: `board.in`, `board.out`, `walked`, `car.seen` (the pip, the debrief and the card say them); `withdraw.car` plays the Fixer's existing withdraw lines |
| debrief `left-behind` | {who} was left behind and {fate}: {why}. · fate: captured, walked out · why: down when the car left, the fight was still on, the district was sealed, the street was calm |
| debrief `captive` | {who}: talks in {holds} · rescue within {rescue-until} · at {held-at} (a one-glance row; the cameo and cuffs glyph say "captured", and nothing else in the debrief says where, so the place stays, last) · {held-at} default "the post" (8 characters at most) · {holds} and {rescue-until} carry their unit ("2 missions") |
| debrief `getaway.holdsOut` (second `captive` line) | {who}: holding out · rescue within {rescue-until} · at {held-at} · the host picks it instead of `between.captive` when the custody projection says the captive does not talk inside the rescue window (Keel's `TalksAt` is 0) |
| debrief `car-seen` (Sky's answer 5) | {by} saw the car leave. Investigators know it now. Trail +{trail}. · by: A police officer, A police robot, The patrol |
| `dialogue.rules.json` `debriefFill` | added widths for `fate` (walked out), `held-at` (the police post), `holds` (2), `rescue-until` (3), `by` (A police officer); `why` widened to "the district was sealed" |

**How `{holders}` is filled** (reference code in `text.schema.ts`: `holdersPhrase`, `roleOfId`; `Answer.ids` carries the engine's comma list): one holder is "a rival", "a robot", "police", "patrol" or "the guard"; several of one role "2 rivals", "2 robots", "police", "patrol"; mixed roles "hostiles". Each is at most 10 characters so the pop fits in 24.

**Flagged**
- *Fight "holds" is not a word the player sees.* The text says "blocked", never "held", and "down them, or drive them off" (rivals break at half, which drives them off).
- *"Talks in 2 missions"* is the engine's `holds` (missions held before the questioning they talk at). When the Talker test never passes inside the rescue window `holds` is the whole window, so the line would promise a talk the engine does not project; the second line, `debrief.getaway.holdsOut`, covers it: the host picks it when `TalksAt` is 0.
- *The Go preview calls a left-behind agent "captured" or "walks out"*, and the debrief says "walked out". One tense each, same meaning.
- *H18 and the Go goal line need triggers in code* (Latch). H11's trigger in Motive's table is unchanged (the test pins it word for word); Latch widens it to "anyone the Go card would leave" per brief 018 §6.6, and Motive should update the row.
- *`extract` stays in `DIALOGUE_VERBS` only*, so the texts pinned in recorded runs r017 to r019 still parse on replay. No live line uses it.

**Handoff to Latch**: port `holdersPhrase`, read `locks.car-held` with `Answer.ids` for the pop and Alt, read `locks['only-scout'].byTarget.machine`, the `board` and `go` action ids, `less.go`, `less.objective.go`, `debrief.getaway` for the three new lines, and the new cards. `PlayText.cs` still seeds `extract` in its fallbacks.

**Evidence**: `bun test contracts/missions/handover` 115 pass; `bun test tools/levels/dialogue/test/dialogue.test.ts` 9 pass; `bun run verify` OK (18 runs replayed, 1 superseded, 45 play shots within HUD budget, typecheck clean) after declaring the dialogue drift for `text.schema.ts` twice (`tools/levels/dialogue/drift.json`).

**Added after Keel's last codes:** lock `fire-carrying-vale` (pop "Hands full · Vale"; Alt "The Enforcer is carrying Vale. Get Vale in the car, and let the others fire."); log `car.free` is an icon card ("The car is free" / "Nothing blocks the car now. Go when you are ready."; the street shows it by the holder marks going), and `blocker.out` is silent on purpose.
