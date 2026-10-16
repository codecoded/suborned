# 014 B · Less reading: the words (Fable, 2026-10-16)

Brief: `briefs/014-less-reading.md`. Sky: "It's the reading, it's too much." Mechanics are frozen: this changes what the HUD says and in what form, never what the engine does. Glyph's visual grammar is in `briefs/014/glyph-less.md` (parallel; not there when I wrote this, so every icon below is a request, named from the existing atlas where one exists). Latch builds from both.

## 0. The change in one page

- **Nothing is deleted.** Every long line in `contracts/missions/handover/text.json` stays, and is now the **Alt** line (hold Alt on a card, a chip, a locked row) or the **history** line (key L) or the **debrief**. What is new is a short form beside each long one, a `fate` on each feed card, and a `less` block. Latch can ship the short forms first and read the long ones only on Alt.
- **Sentences are gone from play, people are not.** What stays on screen at rest: speech bubbles at the speaker, icons, numbers, rings, one price line per way, the Next Step line, at most two story beats, one line on the end card.
- **A new rule I added (it follows from Sky's rule 4 and the GTA note): say it where the player is looking.** A story beat shows only when no bubble of that event's speech is on screen. Every one of the 12 beat cards already has people who speak it (`events.*.speakers`). If the Scout, Inez or the police are in view, their line is the beat; the narrator line appears only when the camera is somewhere else. This is why the beat list can be this small.
- **Rough count of what the player reads** (my hand count from `briefs/013/play/04-takedown-card.png`, takedown card + hint + three feed cards + ticker): about **270 words** at that moment. After: the price lines (3 verbs × about 3 words and 4 numbers), a 3-word Next Step, at most two beats of 2 to 3 words, one cell bubble: about **25 words**. `briefs/011/play/cast-r016-02-…` (Inez, four approaches): about 240 → about 20.
- **Static counts in the file** (`bun` script over text.json): 103 refusal lines 953 words → 226 words as pops; 11 hints 169 → 40; cancel costs 135 → 24; 52 feed cards → 12 beat cards of at most 4 words (9 distinct lines in the whole file).

## 1. Inventory: every text the play HUD shows, and its fate

Fates, as the brief has them: **speech** (a person talking, short, in the world) · **beat** (a feed line) · **icon** (icon, number, ring or mark; Glyph draws it) · **Alt** (full detail on demand; the history key L counts as Alt) · **debrief** · **cut**. I added one: **label** (a name or a word or two the HUD needs as it is: a title, a role, a verb name). Machine-readable at `less.fates` (by path) and `cards.items.*.fate`.

### 1.1 Action cards (right click panel)

| text | where | fate | becomes |
|---|---|---|---|
| card header: "Enforcer → The guard", job, need and temperament words ("café waitress · debt known · Fearful") | `HudModel` PanelSub, `targets.*.name` | **label** + **icon** | portrait, name, need icon and temperament icon (already drawn); the job is a two-word label |
| header "about" sentence ("Serves the café by the square. In debt, and close to the door guard.") | `targets.*.about` | **Alt** | shown on Alt or on the Intel row; never at rest (told/learned unchanged: only once the cell knows the person) |
| row title "Knock out the guard", "Pay Inez" | `actions.*.label`, `ways.*.label`, `withTarget` | **label** | the verb label is the first field of the price line (§2); the target is the header |
| row sentence ("Slow and quiet. They wake after a while and report it.", 342 words over 28 verbs, plus 43 `onTarget` lines) | `actions.*.does`, `onTarget`, `ways.*.does` | **Alt** | Alt only. Kept word for word |
| price row ($ · time · Trail · Alert, "walk 1 s") | `HudModel` PanelCard | **icon** | the one-line price (§2) |
| outcome words ("Works", "They would refuse", "Not yet: one more try", "? until the Scout learns who they are") | `HudModel.OutcomeWords` | **icon** (✓ ✗ ½ ?) + **Alt** | mark at the end of the line; the words are `less.alt.outcome` |
| "Before you act" block, 5 rows (seen by, from behind, people within 13 m, next check-in, if seen) | `HostTakedown.Before` | **icon** + **Alt** | the street already carries it: learned cones, the eye mark on the target, "!" over each witness, the clock pip over the guard (Sky's table). The rows are Alt, built as today |
| takedown rows: "Found: unit in ~16 s · holds the Office door ~1:10", "Never found: comes home, +4 T, a statement", "Wakes 4:07 · knows where and when, not your face", "Never talks · 1 tie", "Stops their report: it lands in 9 s" | `HostTakedown.TakedownIntent` | **Alt** (+ **debrief** for the never-found outcome, already `debrief.between`) | the wake ring on the body, the lock on the door when it happens, the "!" and dotted line to the phone. Alt keeps the numbers |
| loop rows ("Blinds: what the entrance camera sees", "Replaces the loop on …") | `HostTakedown.LoopIntent` | **Alt** | the camera's learned cone greys when looped; Alt keeps the line |
| stash rows ("Dumpster: 1 free · a search at Alert 2 within 30 m opens it", "Emptied at 2:10 (watched)") | `HostTakedown.BodyIntents` | **icon** + **Alt** | stash plate pips (Glyph §5 already) ; the sentence is Alt |
| walk line ("walks 4 m (3 s) to the café door, then") | `HandoverHost.OpenPanel` | **icon** | folded into `time` (walk + act), the route is the dotted path; Alt splits them |
| locked row: "Locked: busy until 1:10 (Watching Inez); C cancels it" + "To unlock: …" | `HudToolkit` Locked, Unlock | **icon** + **Alt** | the row is dimmed with its price line; the click gives the pop (§4); the long lock line (`locks.*.line`) is Alt on that row |
| note when nothing is possible: "Nothing here for the Scout. The Fixer can." | `HandoverHost.WhoCan` | **icon** | the Fixer's portrait pulses; pop "Fixer can" (`less.pop.who-can`) |
| "passer-by — not yet known to the cell" | `HandoverHost.OpenPanel` | **icon** | a "?" badge (`less.pop.passer-by` as the tooltip word) |
| "select an agent: F1–F4 (or click one)" | `HandoverHost.OpenPanel` | **cut** | the click already selects; pop `Select an agent` if it must say anything |
| key chips on rows (1 to 4, F1 to F4) | HudToolkit | **icon** | as built |

### 1.2 Prices, chips and the cursor

| text | where | fate | becomes |
|---|---|---|---|
| hover price chip ("Pay Inez   $90,000 · 50 s · +2 · 0 · Works") | `HudModel.ChipText` | **icon** | the price line (§2), lead icon = the verb; a way leads with `way_knock` or `way_kill` |
| move chip "Scout: walk 12 s · run 6 s · seen by the guard (Alert 2)", "can't walk there" | `HostFight.UpdateMoveChip` | **icon** | `› 12 s  » 6 s  👁 guard ▲2` (no agent name: the selection ring says who); a refusal is the pop `No way` |
| carry chip "Enforcer carrying: 5 s · Dumpster: 1 free · holds: Office door · seen on the way: nobody known" | `HostTakedown.CarryChip` | **icon** + **Alt** | `s_carrying 5 s · 👁 none`, the destination's stash plate and the would-hold locks on the doors; the sentence is Alt |
| aim chip "Guard · hits 3 in 4 · down in 5.5 s · 5 m effective · in cover · +2 T hit · +2 T down" | `HostFight.Aim` | **icon** | `down 5.5 s · ¾ · +2/hit`, cover as an icon; range band and the second Trail number are Alt. "CIVILIAN ·" prefix is the red slash mark |
| aim warning "Vale · never: shooting him fails the mission" | `HostFight.UpdateAim` | **icon** | `✗` on the crosshair and the pop `Never Vale` |
| body chip "wakes 4:07 · left 0:54" | `HostTakedown.StreetMarks` | **icon** | the wake ring that empties; the clock time is Alt |
| door "held ~1:10", "locked", preview "would hold ~1:00" | `HostTakedown.StreetMarks` | **icon** | the lock with its ring (Glyph §6); the times are Alt |
| response pill "RESPONSE · 2 officers · 3 robots · 3 coming" and its rows | `HostTakedown` ResponseLine | **icon** + **Alt** | unit icons with counts; the rows (On the street, Coming, More, Seal) are Alt (R) |
| edge chip "3 robots in 14 s" | `HostTakedown` Arrivals | **icon** | arrow, unit icon, count, seconds |
| cast icons over a person ("?", "$", "$?", "◆door") | `HudModel.Icons` | **icon** | as built (at most two) |

### 1.3 Feed cards (the SCENES stack) and the ticker

| text | where | fate | becomes |
|---|---|---|---|
| act card header and place ("Watch  Inez · café door  0:07", NEW, tick, K) | `HostFeed` | **cut** | the ring on the agent with its seconds (as built); nothing in the feed |
| "The result lands when the act finishes." | `HudToolkit`, `PlayHud` | **cut** | the ring |
| act card lines (the overheard scene, the approach scene, the call scene) | `scenes.items.*.variants` | **speech** | bubbles at the speakers (§6); the full scene is in the history |
| act card footer "Learned: Inez is in debt · Fearful · Secret: she's seeing the door guard." | `scenes.items.*.result`, `FooterOf` | **Alt** | the Intel row fills in (icons); Sky's table: "feed acks of what you just watched → nothing" |
| approach footers ("Inez is turned. Call Inez when you need the guard moved.", "Inez refused, and will remember it.", "Inez reported you. Alert is now 1.") | `scenes.items.*.result` | **icon** + **Alt** | ✓ ✗ ½ over the person, the Intel row, the Alert pip; the "!" over a person who reports |
| 52 cards for engine events | `cards.items` | per card, **`fate`** | **12 beat · 20 icon · 18 alt · 2 debrief** (the list is §3) |
| city scenes (chime, rivals arrive, guard at the café, someone raises the alarm / freezes / stops to help, Vale's driver is missing) | `scenes.items` kind city | **speech** | their first three beats as bubbles; one has a beat (§3) |
| cancel card ("Loop the entrance camera cancelled · The loop ends and the camera sees again.") | `HostFeed.FeedCancelled` | **cut** | the cancel mark tints when the cancel costs something (§1.4); the words are `cancelCosts` on Alt |
| going-loud feed card ("Alert 3 · police in 16 s · sealed at 8:30") | `HostFeed.FeedLoud` | **cut** | the pill and the response pill say it |
| fallback cards the host builds when text.json has none (`FeedEvents`, `EventResult`) | `HostFeed` | **cut** | every code the host knows now has a card in text.json; no host fallback card shows |
| "engine's own words" for a log code the text neither speaks nor silences | `HostActivity.SayLanded` | **cut** | never shown (F12 debug only). The test already fails on such a code |
| SCENES bar "SCENES 3 new · K ack · L history", NEW tags, "+16 more", "Read all", filters | `PlayHud`, `HudToolkit` | **cut** at rest | a small unread dot on the L key; the history opens on L with its filters and its lines (title + result, as today) |
| ticker under the roster ("It took the orders. It always does.") | `HudModel.Ticker` | **speech** | a bubble over the agent (§6) |

### 1.4 Plates (roster, hotbar, pill, objective)

| text | where | fate | becomes |
|---|---|---|---|
| strip: "TECH · BUSY", "ENFORCER · READY", "FIXER · HURT", state tags DOWN, CARRYING, RUNNING | `HudModel.Slot` | **cut** | portrait ring + icon (busy ring, carrying disc, down mark); the key F1 to F4 stays |
| strip line "Holding the loop", "Watching Inez", "Rerouting the patrol", "Inside the Office" (64 words of `activity`) | `activity.*` | **icon** + **Alt** | the verb icon with the target's badge and the seconds left; `activity` is the Alt tooltip |
| "Ready / Right click to give an order" | `HudToolkit` | **cut** | nothing; control teaching is the Next Step's job |
| "Hurt, still fighting", "Health 3 of 4", "Drawn · hold" | `HudModel.Slot` | **icon** | hit pips `3/4`, the drawn weapon and the stance badge (H, R, A) |
| queued line "then asks Inez" | `HudModel.Slot.Queued`, `actions.*.then` | **icon** | the verb icon at the end of the dotted route (as built) |
| carry pace "walk only · 1.4 m/s", "slow · 0.7 m/s" | `HudModel.Slot.Pace` | **icon** | `s_carrying` + `1.4 m/s` |
| cancel tooltip "Cancel loop — The loop ends and the camera sees again." | `cancelCosts` | **Alt** + **icon** | the ✕ is plain when free and tinted when costly; hover and Alt show the long line; `less.cancel` has a 1 to 3 word form for the tinted tooltip ("loop ends", "refund, remembered") |
| verb plate caption "Scout: Watch", "Enter / Take / Car" | `HudModel.VerbCaption` | **cut** | knob icons and keys; the name shows on hover |
| pill: next mark "Handover chime · 5:30", "? · 3:10" | `briefing.timeline`, `NextParts` | **icon** | mark icon and time; the word is `timeline[].short` ("Chime") on Alt; unknown stays "?" |
| pill: clock, $, Trail, Alert pips, PAUSE | HudToolkit | **icon** | as built |
| objective bar "Take Vale alive to the getaway car · closes 8:30" | `briefing.tracker`, `HudModel.Objective` | **beat** (as the objective line) | `less.objective` (§5): "Take [Vale] alive." then "Get [Vale] to the [car]."; the close lives on the pill |
| city line "17:24 · space pause · B briefing · L history · C cancel · N next" | `HudModel.City` | **Alt** | the key legend on Alt (key chips on every control) |
| Status string ("Running.", "Paused. Space starts…", "Scout selected", "engine fault …") | `HandoverHost.Status` | **cut** | debug only (F12). Not drawn by the Toolkit view; the IMGUI fallback draws it |
| Intel header "INTEL 3 people known" | `HudToolkit` | **icon** | `3/4` |

### 1.5 Hints and toasts

| text | fate | becomes |
|---|---|---|
| NEXT STEP header and the 11 hint lines (169 words) | **beat** (the one teaching line) | `hints.items.*.short`, 40 words in all (§5). The long line is Alt and the relearn key |
| "H hides hints" chip, ✕ | **icon** | as built |
| toast "Security saw someone run and is looking into it (Alert 1)." | **cut** | the watcher's "!" and speech ("You there! Walk, please."), the Alert pip. The toast text stays in the file (history) |
| "Hints on." / "Hints off. H brings them back." pops | **icon** | `less.pop.hints-on` / `hints-off` |

### 1.6 Intel

| text | where | fate | becomes |
|---|---|---|---|
| name, job ("café waitress", "Vale's driver", "door guard") | `HudModel.IntelRow` | **label** | kept (two words) |
| need icon, temperament icon, turned tick | `IntelRow` | **icon** | as built |
| "Next": "Watch: learn the routine", "Listen: learn the need", "Turn: Ask, Pay, Help or Pressure", "Call them when it counts", "Take him at his car" | `HudModel.Refresh` | **cut** | the verb's icon (watch, listen, turn, call, take) in the row's corner; the sentence is a tutorial and the Next Step teaches |
| "Secret: seeing the guard", "Secret: a record" | `IntelRow.Secret` | **icon** + **Alt** | a tie mark between Inez and the guard (the lover tie; only once known, as today); the sentence is Alt |
| "INTEL n people known" | | **icon** | `n/4` |

### 1.7 Briefing and the going-loud card

| text | where | fate | becomes |
|---|---|---|---|
| title "The Handover", "Harbour District · 17:24" | `briefing.title` | **label** | kept |
| situation, 5 sentences (57 words), Objective, Win, Clock, Purse lines, "Two ways in", "The cell" (4 lines), key footer | `briefing.card`, `objective`, `win`, `clock.line`, `purse.line`, `hints`, `cell` | **Alt** | B re-reads them. Before Begin: the goal line `less.briefing.goal`, three numbers (close 8:30, purse $100,000, Alert 0/3), four portraits with their one-word roles (`names.speakers`) |
| "Fail if" list (12 sentences, 103 words) | `briefing.fails` | **debrief** | said once, on the end screen, for the fail that happened |
| "YOU WIN / YOU LOSE", "Approach A: …", "Cell armed" box labels | `HudToolkit` | **cut** | the goal line and the portraits |
| going-loud card: GOING LOUD, Now, Who comes, You lose, Costs, "The first shot cannot be undone.", "Anyone downed is captured unless carried.", "Hold the button to commit · release or Esc to back off · the clock runs" | `HudModel.LoudCard`, `goingLoud.captured` | **icon** + **Alt** | a card of icons and numbers: loud mark, `▲3`, police `16 s`, rivals `40 s`, seal `8:30`, the lost allies as crossed icons (guard, Inez), Trail `+2/shot +2/hit +2/down`, a captured mark; the hold is the ring filling (0.5 s, no pause). The sentences are Alt; `goingLoud.captured` stays |

### 1.8 End card and debrief

| text | where | fate | becomes |
|---|---|---|---|
| headline "Vale is in the car" / "You withdrew" / first sentence of the fail line | `debrief.headline`, `briefing.fails` | **beat** (one line) | `less.end` (§3 end): "Vale is in the car.", "Vale got away.", "The guard saw you." |
| result sentence "The crew escapes with Vale alive." / the whole fail sentence | `debrief.result`, `briefing.fails` | **debrief** | first line of the Debrief screen |
| tally "Done at 3:39 · $0 spent · Trail 6" | `debrief.tally` | **icon** | numbers with icons, on the card |
| "Security is calm / raised / on high alert", "Investigators: none yet / a few leads / an investigation is open: {cause}" | `debrief.security`, `investigation`, `causes` | **debrief** | Debrief screen; the card shows the Alert pips and an investigation dial |
| route sentences, Final fight, Kept, between-missions lines | `debrief.route`, `fight`, `kept`, `assets`, `between` | **debrief** | Debrief screen as today. Checked the two Brief 013 tags Aster flagged (`guard-knocked-out`, `guard-killed`): "The Enforcer knocked the guard out." and "…killed the guard." are already one plain line each; left as they are |
| buttons Debrief, Replay (Return) | HudToolkit | **label** | as built |

### 1.9 Refusal pops

103 engine codes in `locks`, plus 17 the host builds itself. Every one is a pop at the click of **icon + a few words, the number as a number**, about 2 s, over the target as today (rules and the whole table in §4). Long lines: **Alt** on the locked row, and the history. Fate of the 103 long lines: Alt; of the 103 shorts: new.

### 1.10 Speech (kept)

438 dialogue entries, 974 lines, none longer than 12 words (the longest are 10 to 12). All **speech**. They already carry the story, the told/learned rule (who says what depends on what the cell has seen) and the tone filters. I changed none. How they are shown is §6.

## 2. The one-line price grammar

Data: `less.price` (order, per-verb fields, examples), tested in `text.test.ts`.

**A line is the verb's label, then only the fields that are not zero, in this fixed order, joined by " · ":**

| # | field | shows | hidden when |
|---|---|---|---|
| 1 | `name` | the verb or the way: Knock out, Kill, Pay | never |
| 2 | `usd` | `$90,000` (a range when the worst case is dearer) | the order is free |
| 3 | `time` | `n s`, **the walk plus the act** (Alt splits them; this is a change from today's card, where the walk is a second row) | the order lands at once (Call, Vale into the car) |
| 4 | `trail` | `+n` Trail traced | it traces nothing |
| 5 | `alert` | `▲n` the Alert it raises if it goes wrong (seen, reported, found, fired); `▲≥2` for gunfire | it raises none |
| 6 | `eye` | `👁 none`, `👁 ?`, or the watchers: who could see it | the order cannot be seen |
| 7 | `learns` | the icons a Watch or Listen teaches (routine, sight, need, temperament) | nothing is taught |
| 8 | `pace` | a carry's `m/s` | not carrying |
| 9 | `mark` | ✓ works · ✗ they would refuse · ½ not yet · ? unknown until the Scout has learned who they are | no answer to guess |

An unknown number is `?`, exactly where the engine says "?" (`Seen.Seen`, `Outcome`). The icon for each field is Glyph's; the characters above are shorthand. Unavailable rows show the same line dimmed. **Zero is silence on the line and shows on Alt**, so Sky's example holds (`Knock out · 7 s · +4 · 👁 none`); I add `▲1` because the engine prices an Alert if found and the rule is that every order shows its full price.

Fields each order shows (`less.price.verbs`; every verb has a list, all 28):

| order | line |
|---|---|
| Watch, Listen | name · time · learns |
| False tip | name · time · Trail |
| Ask, Pay, Help, Pressure | name · $ · time · Trail · ▲ · 👁 · mark |
| Call | name · Trail · mark (ready or not) |
| Loop camera, Reroute patrol | name · time · Trail |
| Enter the Office | name · time · 👁 · mark (seen = fail, so a seen entry shows ✗) |
| Take Vale | name · time · Trail |
| Vale into the car | name · mark (wins) |
| Fire | name · Trail (`+2/hit`) · ▲ (`▲≥2`); "until down" is the aim chip's `down 5.5 s` |
| Take down (Knock out, Kill) | name · time · Trail · ▲ · 👁 |
| Carry | name · time · pace |
| Put down, Hide body, Tie up | name · time |
| Walk, Run (the move chip) | name (› or ») · time · 👁 |
| Draw, Holster, Cease fire, Stance, Panic, Cancel, Withdraw | name only (no price today) |

Worked lines, with the engine's numbers from the screenshots (`less.price.examples`, tested):

```
Knock out · 7 s · +4 · ▲1 · 👁 none        (6 s act + 1 s walk)
Kill · 4 s · +8 · ▲2 · 👁 none
Ask · 25 s · +1 · ✗
Pay · $90,000 · 50 s · +2 · ✓
Help · $40,000 · 50 s · +1 · ½
Pressure · 35 s · +3 · ✓
Fire · +2/hit · ▲≥2
```

Alt shows, for the same order: the zero fields, the walk split, the `does` sentence, the person's answer in words (`less.alt.outcome`), and today's extra rows.

## 3. The story beats (Handover)

Rule: the feed shows **only beats** (`beat` on a card or a scene): at most **2 on screen, 4 s each**, then gone to the history (L). A beat shows **only when no bubble of its event's speech is on screen**. An identical line within 10 s is dropped. Numbers: `less.feed`. Nothing else is a card; no card shows what the cell was not told or did not see (the sight and needs rules on each card are unchanged).

| beat | from | sight |
|---|---|---|
| **Rivals here.** | `rivals.arrive`, scene `rivals-arrive` | in sight, as before |
| **Inez called the guard.** | the call scene on Inez (all four temperaments) | the cell's own order |
| **Vale is out.** | `vale.exits` | always |
| **Camera blind.** | `car.blocks` (the driver's car crosses the pickup camera) | always |
| **Reported.** | `report`, `report.landed` | the city's answer (the Alert pill moves with it) |
| **Police coming.** / **Robots coming.** | `alarm.dispatches` (robots only, or both), `security.called`, `fight.loud` | the city's answer |
| **District sealing.** | `seal.starts` | the city's answer |
| **Body found.** | `report.confirmed`, `stash.serviced`, `stash.opened` (found) | in sight, as before |

Nine distinct lines (Police coming and Robots coming are one beat in two forms). Sky's three examples are in; "Inez called him." is "Inez called the guard." (no gendered words in new text). Every line is four words or fewer. What I did **not** make a beat, and where it goes:

- *Vale at the car, the chime, last turn*: the pill's timeline mark moves (icon); the Scout says it (speech).
- *Guard at the café / back on the door*: the guard is in the street; the door-guard pip on the door shows the post. Inez's scene and the Scout's line carry it.
- *Missed check-in* (`post.missed`, needs the routine): the clock pip over the guard turns; the police say "Post four, no answer."
- *Doors locked, held*: the lock on the door. *Body wakes, tied, hidden, carried*: the body's marker and ring. *Report started / stopped*: the "!" and the dotted line. *Agent down*: the roster. *Loop ends*: the loop ring empties on the camera.
- *Statement, captured*: the debrief (`debrief.between`, `debrief.kept`).
- *Turned / refused / reported* after an approach: ✓ ✗ ½ over the person, the Intel row, the Alert pip, and their own reply (speech).
- *Takedown seen, runner seen, fight over, ambush, rivals heading in*: the witness's "!", the Alert pip and the speech ("You there! Walk, please.", "Shots! Get down!", "That's the last. Go.").

**The end line** (`less.end`, one line, GTA's "Mission failed. Vale escaped."): "Vale is in the car." · "You withdrew." · "Vale is dead." · "Vale got away." · "Rivals took Vale." · "District sealed." · "Enforcer down." · "The guard saw you." · "A camera saw you." · "The patrol saw you." · "A camera saw the take." · "Driver shot." · "The run is void." The sentence behind each is `briefing.fails`, shown in the debrief with the tally.

## 4. Refusals

Rule: **a refusal is one pop at the click, icon + at most four words, the number as a number.** It names no one (it sits on the thing it refuses) and has no second sentence. The target pulses as today. Long line: Alt on the locked row, and the history. Data: `locks.*.short`, tested (≤ 24 characters, ≤ 4 words, no sentence, keeps `{m}`, `{usd}`, `{tick}` where the long line had them, never `{who}` or `{op}`).

How a refusal shortens, by example:

| engine says | pop |
|---|---|
| "Too far from Inez. Walk within 4 m first." | **Too far · 4 m** |
| "Not enough cash for the worst case ($140,000). Try a cheaper approach." | **Short · $140,000** |
| "Needs Inez's routine first. Have the Scout Watch them." | **Watch first** |
| "Needs to know who Inez is first. Have the Scout Listen." | **Listen first** |
| "The Tech is tied to the cabinet while the loop holds. Wait for it to end." | **Tied to cabinet** |
| "Too late: the car leaves for the pickup at 5:50." | **Too late · 5:50** |
| "Police hold this door. Nobody goes in until they stand down." | **Held** |
| "Only the Scout can do this. Select the Scout." | **Scout only** |
| "He has already come once this shift. He will not leave his post twice." | **Came once** |
| "A civilian or a contact. Ctrl forces it, and the city will remember." | **Civilian · Ctrl** |

The family icon (Glyph's pick; I name the existing atlas glyph): reach `s_moving`; Watch first `watch`; Listen first `listen`; wrong agent the agent's portrait; money `pay`; a window `p_time`; the guns `fire`; bodies `s_carrying`; a held or locked door `door_held` / `lock`; "no" otherwise `mark_no`.

Every distinct pop, by family (all 103; `less.pop` has the 17 the host builds itself: No way, No room, Unarmed, Not drawn, Hold, Return, At will, Another carries, Nothing there, Never Vale, {op} can, Nothing here, Select an agent, Passer-by, Hints on, Hints off, No clear line):

| family | pop ← codes |
|---|---|
| Reach | **Too far · {m} m** ← `too-far`, `stand-at-cabinet`, `stand-near-rivals`, `stand-at-door`, `stand-beside-vale`, `stand-at-getaway`, `stand-beside-op`, `stand-at-stash`, `stand-beside-body` · **Out of range · {m} m** ← `out-of-range` · **Get closer** ← `needs-reach` |
| Needs knowledge | **Watch first** ← `needs-routine` · **Listen first** ← `needs-need`, `needs-temperament`, `no-leverage` · **Wrong need** ← `no-act-need`, `needs-need-is` · **Need met** ← `need-met` · **Refused before** ← `refusal-remembered` · **Passing by** ← `needs-tier` · **Not their kind** ← `needs-fit` · **Too big yet** ← `needs-scale` · **Not trusted here** ← `needs-standing` · **Trail too low** ← `needs-trail` |
| Wrong agent or state | **Scout only** ← `only-scout` · **Fixer only** ← `only-fixer` · **Tech only** ← `only-tech` · **Enforcer only** ← `only-enforcer` · **Down** ← `agent-down`, `person-down` · **Busy · {tick}** ← `agent-busy` · **Inside** ← `enforcer-inside` · **Not out yet** ← `person-away` · **Tied to cabinet** ← `run-tethered` · **Over** ← `mission-over` · **Run void** ← `engine-error` · **Not an order** ← `unknown-verb` |
| Money and windows | **Short · {usd}** ← `no-cash` · **Too late · {tick}** ← `driver-too-late`, `call-too-late` · **Wait · {tick}** ← `car-busy` · **Car gone** ← `car-gone` · **Away · {tick}** ← `patrol-away` |
| Turn and call | **Already ours** ← `already-turned` · **Can't turn Vale** ← `vale-not-contact` · **Not ours yet** ← `not-a-contact` · **Nothing to call** ← `nothing-to-call` · **Pick an approach** ← `turn-needs-approach` · **Guard down** ← `guard-down` · **Guard off post** ← `guard-not-at-door` · **Not now** ← `summon-busy` · **Came once** ← `summon-came-once` · **Alarm up** ← `summon-alert` · **Saw violence** ← `summon-saw-violence` · **Walk takes {tick}** ← `summon-too-far` · **Off the street** ← `summon-off-street` · **No post to leave** ← `summon-no-visit` |
| Camera, patrol, rivals, Vale, car | **Wrong camera** ← `loop-wrong-target` · **Loop holds** ← `loop-holds` · **Patrol down** ← `patrol-down` · **No rivals yet** ← `no-rivals` · **Vale not inside** ← `vale-not-inside` · **Vale not out** ← `vale-not-at-car` · **No Vale yet** ← `no-vale-yet` · **Fight on** ← `car-held` |
| Guns | **Unarmed** ← `no-weapon` · **Drawn** ← `already-drawn` · **Not drawn** ← `not-drawn` · **Hold, return, free** ← `stance-unknown` · **Whole cell** ← `panic-not-cell` · **No fight yet** ← `panic-no-fight` · **Nobody armed** ← `panic-nobody` · **Not while running** ← `fire-running` · **Hands full** ← `fire-carrying` · **No target** ← `no-target` · **Not the cell** ← `fire-on-cell` · **Not a target** ← `target-unknown` · **Civilian · Ctrl** ← `civilian-target` · **No clear line** ← `no-clear-line` |
| Bodies, carrying, cancel, doors | **Carrying Vale** ← `carry-vale` · **Hands full** ← `already-carrying` · **Nobody to lift** ← `carry-names-downed` · **Not yourself** ← `carry-self` · **Not down** ← `not-down`, `no-body` · **Being carried** ← `already-carried` · **Idle** ← `nothing-to-cancel` · **Walk Vale** ← `keeps-vale` · **Holstering** ← `holstering` · **Pick a way** ← `takedown-how` · **Not Vale** ← `takedown-vale` · **Hidden** ← `body-stashed` · **Not carrying** ← `not-carrying` · **Full** ← `stash-full` · **Dead** ← `body-dead` · **Tied** ← `already-restrained` · **Locked** ← `doors-locked` · **Held** ← `door-held` |
| Ground | **Off the map** ← `no-place` · **Blocked** ← `unwalkable` · **No way** ← `no-walk` · **No running** ← `run-carrying-vale`, `run-carrying-downed` |

## 5. Hints: the Next Step is the objective line

Sky's GTA note (the objective is one imperative, the key noun coloured as its marker, it fades, it returns only if the player strays; voiced people carry the story and the hints) maps onto two lines:

**Objective** (`less.objective`, replaces the bar): "Take [Vale] alive." until the Enforcer holds Vale, then "Get [Vale] to the [car]." The bracketed nouns take their marker colours. It shows for 6 s when the clock starts and when it changes, then fades. It reappears at the 5:30 chime and the 5:50 exit (the marks where a window opens), not otherwise. The close (8:30) is a number on the clock pill. It never explains and it is never a hint.

**Next Step** (`hints.items.*.short`): one imperative, **at most 6 words, 36 characters, one [noun]**, no agent named (the agent's portrait pulses with the verb icon at the line's left; today only H3 rings a portrait), no approach, no time, no sequence. Tested.

| | now (words) | short |
|---|---|---|
| H1 | The door has a guard. The Scout can Watch the people on the café terrace. (15) | Watch the [café terrace]. |
| H2 | Watching shows where and when. The Scout can Listen, close by, to learn who someone is. (16) | Listen to [Inez]. |
| H3 | The Fixer can turn this person. Hover an option to read its price. (13) | Turn [Inez]: right click. |
| H4 | A camera watches the public door. The Tech can Loop it from the control cabinet. (15) | Loop the camera at the [cabinet]. |
| H5 | Turned people act on a call. The Fixer can Call them: right click to see what they do. (18) | Call [Inez]: right click. |
| H6 | They refused, and they will remember. The Scout can Listen to learn who someone is. (15) | Listen to [Inez]. |
| H7 | A patrol sweeps this door. The Tech can Reroute it, and N shows it coming round. (16) | Reroute the [patrol]. |
| H8 | Another crew watches the pickup. The Scout can give them a false tip. (13) | Give the [rivals] a false tip. |
| H9 | Vale leaves by car. His driver waits at the pickup, and the Fixer can turn drivers. (16) | Turn [the driver]. |
| H10 | Who covers the Enforcer on the way to the getaway car? Anyone armed can Fire. (15) | Cover the [getaway car]. |
| H11 | A downed agent left behind is captured. Select another agent and right click them to carry them. (17) | Carry the [downed agent]. |

(`{who}` is filled by the host: "Inez", "the driver"; the line is the verb's own word and a target, so it introduces one verb, as Motive's rule 4 wants.) The story the old lines told ("the door has a guard", "Vale leaves by car") is told by the briefing's Alt and by the person's `about` on Alt; the Next Step only points.

**How long it shows and when it retires** (`less.hint`; UI parameters Glyph and Latch may tune):

1. It appears under Motive's unchanged triggers (8 s with no order, in priority order, one at a time).
2. It shows **6 s**, then fades to its ring in the street. It **returns once**, after 20 s more of idling, if the act is still undone. It shows **at most twice** per run, then stays a ring until the act.
3. It retires **for good on the first time the player gives the verb it teaches** (today only H3, H5, H7, H11 retire per profile; this extends it to all eleven). A returning player sees only the verbs they have never used. H still hides the layer; the long `line` is on Alt and on the relearn key.
4. It is the **only** teaching surface: no control is taught anywhere else (the old key text, "Right click to give an order", "select an agent: F1–F4" and the key legend in the city line are cut or Alt).

## 6. Speech: how the lines show (kept as they are)

- A line is a **bubble at the speaker**, no name tag (the speaker is the thing it sits on), the person's colour. Bubbles on screen: **2 at once**, **3 s** each (`less.feed`). A speaker off screen has no bubble; the beat takes over (§3).
- **Overheard (Watch, Listen) and approach scenes play every beat**: what the cell learns, and the Fixer's drama, are in them (the Inez and guard scene is how the player works out the secret). **Call and city scenes play their first 3 beats**; the rest are history.
- **Cell lines per order, at most two**: the `ack` when the order is taken, and the `done` when an act that matters lands (turn, call, take, enter, loop, reroute). `arrive` plays as audio and history only (it duplicates the ring starting); where an order has no walk, the ack and arrive no longer both show.
- The `read` line after a Watch or Listen is still the Scout's bubble; it is flavour, not the report (the Intel icons are the report).
- The ticker pill under the roster is gone; the history (L) keeps every line, with speaker names.
- Voices (Chime) say every line whether or not it shows; text in a bubble is the subtitle.

## 7. What changed in `contracts/missions/handover/text.json` (additive; nothing removed or reworded)

| key | new | the long line is now |
|---|---|---|
| `locks.*` (103) | `short` | `line` = Alt and history |
| `cards.items.*` (52) | `fate` (beat · icon · alt · debrief); `beat` on the 12 beat cards | `title` and `result` = history lines |
| `scenes.items` | `beat` on the 4 Inez call scenes and the rivals scene | `result` = Alt; beats = speech |
| `hints.items.*` (11) | `short` | `line` = Alt and relearn |
| `briefing.timeline[]` (6) | `short` | `label` = Alt |
| `less` (new block) | `fates` (every player-facing group by path), `price` (order, fields, per verb, examples), `feed`, `objective`, `briefing.goal`, `end` (win, withdraw, 12 fails), `pop` (17), `cancel` (15), `alt.outcome` (13), `hint` | |
| `hints.rules.short`, `cards.rule`, `scenes.rules.less` | one sentence each | |

Schema: `contracts/missions/handover/text.schema.ts` (`short` and `fate` and `less` are required; `beat` optional; `Less`, `FATES`, `PRICE_FIELDS`, `marked`). The Unity host ignores unknown keys, so the current build is unaffected until Latch reads them. `bun tools/sim/unity.ts` re-validates and re-copies the file to StreamingAssets (Latch or Keel; I did not run it).

## 8. Asks, open issues, next owner

**Glyph (names to agree; reuse the atlas where one exists):** eye mark in three states (open, shut, `?`) for the `eye` field and the street; `▲` Alert mark (`p_alert`); the Trail mark; pace; the "learns" icons (routine `p_time`, sight `watch`, need, temperament); a **clock pip** over the guard once the guard's routine is learned (replaces "Next check-in" and "missed one sends a robot"); the **speech bubble**; the **tinted ✕** (costly cancel); a "locked row" dim state; the 17 pop icons (families in §4). Nouns in the objective and hint lines are coloured as their markers (`[Vale]`, `[car]`, `[patrol]`): I need the marker colour per noun from you.

**Latch (host strings I could not move into text.json without touching code):** `HudModel.EndCard`, `LoudCard`, `OutcomeWords` (words now in `less.alt.outcome`), `HostTakedown.Before`, the Found/Never found/Wakes rows, `CarryChip`, `HostFight.Aim` and `UpdateMoveChip`, `HostFeed.FooterOf`, the pops in `HandoverHost`, `HostFight`, `HostHints` (their short forms are `less.pop`). Their long forms are Alt as they are. Everything is read-only over the engine: replays unchanged. Decisions that are Latch's: where the beat check "no bubble on screen" reads from; how `time` sums the walk (host arithmetic, not an engine number).

**Open for Sky or Aster:**
1. `time` as walk + act changes how a price reads (7 s, not 6 s plus "walk 1 s"). I think it is the honest price; say if you want the walk shown apart.
2. I made hints retire per verb for good (§5.3). That is a profile setting, not a rule; Motive may want a say.
3. `Reported.` can fire for a report the player caused and one the city made; the Alert pip and the "!" tell them apart. If that is not enough, it needs a second beat.
4. Told/learned: no beat or pop names a person the cell has not met or a fact it has not learned. `Inez called the guard.` is the cell's own order, and the closeness of Inez and the guard is already told in the briefing (`briefing.hints.front-door`); the Listen secret (the tie) is not named by any beat or pop.

## 9. Evidence

- `bun test contracts`: 155 pass, 0 fail (was 139; 16 new tests in `text.test.ts` under "brief 014", including one that no new short form uses a gendered word).
- `bun test tools/dialogue`: 48 pass, 0 fail.
- `bun run typecheck`: clean.
- `bun run verify`: **VERIFY OK**, 18 runs replayed, 1 superseded (expected). The schema change moved the dialogue code hash, so r017 to r019 first failed replay for want of a cause; I declared it in `tools/levels/dialogue/drift.json` (`bun tools/levels/dialogue/drift.ts declare …`): the shapes only widened, and `lookup`, `pickIndex`, `sceneKey` and every dialogue rule are unchanged, so every pick is the same.
- Files: `briefs/014/fable-less.md` (this), `contracts/missions/handover/text.json`, `text.schema.ts`, `text.test.ts`, `tools/levels/dialogue/drift.json`. Scratch: `out/less/apply.ts` (the first edit script; later small edits are in the diff) and `out/less/text.before.json` (the file before).

Next owner: Latch (build from this and `glyph-less.md`); Glyph for the icon names; Aster to ask Sky the three questions in §8.
