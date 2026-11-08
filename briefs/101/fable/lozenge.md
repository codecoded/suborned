# 101 · Fable: the news lozenge's line fits its box

Fable, 2026-11-09, on `work/098-living-playbook`. Uncommitted; Aster commits. No Unity runtime code, scene, GDD or engine was touched. One host test file's three word pins follow the new words (Latch's `HostLogic.Why100.cs`, below). The sheet with bars: `briefs/101/fable/lozenge.html`.

Latch's r4: the lozenge, bottom right, draws a card's title over one line, `{short} {where} · m:ss`: the why block's short, the place the host adds after it, and the clock the card came at. That line is Jost Medium at 13 px in a 296 px box. The sweep's loiter why took 430 px and was cut off with an ellipsis.

**In short:**
- **The measure** (`tools/style-agent/short-lozenge.ts`): the font's own advance for every character (checked against `Jost-Medium.ttf` in a test), no kerning, so it errs 0.4 % wide (Latch's line: 431.7 px here, 430.0 in Unity). Every fill is taken at the widest the glossary gives it: a camera "the square's mast camera", a person "another shopkeeper" or "a kiosk attendant", an agent "the Enforcer". The place is taken at its room, and the clock at the widest, " · 4:44".
- **What the lozenge can show:** 48 why shorts (every Alert cause but the start, every police call but the seal; the report and the robots show their cause's short) and 11 card lines.
- **Before:** 34 of the 48 whys ran over the box at their widest. The worst was 524 px ("{who} found {about} knocked out"). The widest place was "by the control cabinet", 124 px. All 11 card lines already fit (widest 226 px: "8 health; cover him and Popovic · 4:44").
- **Now:** none over. The widest is 293.9 px ("an agent stayed too long at the service drive · 4:44"). After the hand round (below) it is 295.3 px ("Popovic told when let go at the service drive · 4:44"), and still none over. Latch's line now reads "an agent stayed too long beside the café · 2:46", 272.2 px. In every saved route and pin the host plays, the 40 lines it draws all fit; the widest is 291.9 px ("getaway car out of place at the service drive · 5:48").
- **Through the loop:** 38 shorts and 2 places rewritten, gpt-6.1-sol writing on Codex (Sky's ChatGPT plan), Claude Opus 5.5 judging on the subscription, approved as Sky, as in brief 102's sweep. Every family was accepted 10/10 with no findings and clean code checks. 1.55M tokens (gpt-6.1-sol 0.76M, Opus 0.78M). No paid API.
- **Titles and lines unchanged.** The line already says who saw what, whom and where, in full. It shows on hover in the Alert meter's list and in the Journal, and it is the debrief's timeline. So a short now says what happened at a glance, and names a person or a camera only where the box holds them at their widest.
- **Kept:** brief 102's bar (the rubric's PLAIN now fails AI-sounding words in a why too, with a code check: "confirmed" went), Sky's rows and the `hand` rows (no bark was touched; `apply` changed only these 40 strings), "the search goes on" and "at the Office door" (host tests read them word for word).
- **Then by hand (guide /22):** the five weakest shorts, rewritten to brief 102's bar and scored once by the Opus judge (5 and 7; every finding on one of the five, each line rewritten from it): "{who} said to drop it", "police call it a killing", "someone woke and told", "Popovic told when let go", "knocked-out body found". Their causes carry `hand` in `text.json`, so no run replaces them. See "The five by hand" below.

## The why shorts, at their widest

Before: today's short, with the widest place it could have then ("by the control cabinet"). Now: the new short, with the widest place it can have ("at the service drive"). Both with the widest fills and " · 4:44". The box is 296.

| `alert.seen` | {who} saw a takedown | 407.7 | **a takedown seen** | 248.9 |
| `alert.seen (vale)` | {who} saw Popovic taken | 422.7 | **Popovic seen taken** | 263.8 |
| `alert.carry` | {who} saw a body carried | 426.2 | **a body seen carried** | 267.3 |
| `alert.carry (vale)` | {who} saw Popovic led off | 429.2 | **Popovic seen led away** | 283.5 |
| `alert.dead` | {who} found {about} dead | 483.1 | **dead body found** | 251.7 |
| `alert.down` | {who} found {about} knocked out | 524.3 | **found knocked out** | 260.2 |
| `alert.woken` | {about} woke and reported it | 388.3 | **a victim woke, spoke up** | 291.3 |
| `alert.woken (vale)` | Popovic reported being taken | 339.3 | **Popovic spoke up** | 255.3 |
| `alert.noise` | {who} heard someone calling out | 439.0 | **someone calling out** | 267.8 |
| `alert.mark` | {about} missed a check-in | 328.0 | **a missed check-in** | 253.3 |
| `alert.mark (unknown)` | {about} was missing from the post | 375.7 | **post found empty** | 252.7 |
| `alert.missing` | {who} found {about} missing | 467.2 | **someone gone missing** | 284.5 |
| `alert.cut` | the desk saw {about} go dead | 314.0 | **{about} went dead** | 248.7 |
| `alert.approach` | {who} reported the Fixer's offer | 371.6 | **an offer was reported** | 276.7 |
| `alert.staff` | the Enforcer left without Popovic | 227.1 | kept | 227.1 |
| `alert.let-go` | Popovic reported being let go | 213.1 | kept | 213.1 |
| `alert.confirmed` | the police confirmed {about} dead | 419.6 | **police say a killing** | 258.9 |
| `alert.gunfire` | shots fired | 228.3 | kept | 212.9 |
| `alert.gunfire (other)` | {who} opened fire | 294.6 | kept | 279.1 |
| `alert.violence` | {who} saw {about} fired on | 457.7 | **someone saw a shooting** | 293.2 |
| `alert.violence (shot)` | {about} was fired on | 339.0 | **someone was fired on** | 279.1 |
| `alert.violence (blast)` | a car blew up | 246.3 | kept | 230.8 |
| `alert.drawn` | {who} saw {agent}'s gun | 447.7 | **{agent}'s gun seen** | 288.9 |
| `alert.drawn-people` | people saw {agent}'s gun | 342.8 | **people saw a gun** | 254.3 |
| `alert.warned` | {who} told {agent} to drop it | 380.6 | **told to drop the gun** | 268.1 |
| `alert.fought` | {about} saw the Enforcer coming | 369.2 | **{about} saw it coming** | 290.4 |
| `alert.loiter` | {who} saw {agent} stay too long | 489.6 | **an agent stayed too long** | 293.9 |
| `alert.recorded` | {who}: desk saw payment to {about} | 499.4 | **payment caught on tape** | 290.4 |
| `alert.recorded (pressure)` | {who}: desk saw threats against {about} | 518.5 | **threats caught on tape** | 280.0 |
| `alert.car` | {who} reported the car's unusual parking | 514.5 | **getaway car out of place** | 293.0 |
| `alert.entry` | the patrol saw the Enforcer enter | 355.9 | **the Enforcer was seen** | 279.1 |
| `alert.sighting` | {who} reported {agent} on sight | 490.6 | **agent reported on sight** | 288.0 |
| `police.heard` | shots heard | 234.5 | kept | 219.0 |
| `police.witness` | {who} saw the shooting | 412.9 | **the shooting was seen** | 279.2 |
| `police.shot-at` | {about} was fired on | 339.0 | **someone was fired on** | 279.1 |
| `police.patrol` | the patrol was fired on | 297.5 | kept | 282.0 |
| `police.rivals` | the fight with the rivals | 298.1 | kept | 282.6 |
| `police.gang` | a shooting nobody saw | 172.1 | **nobody saw who fired** | 167.2 |
| `police.drawn` | {who} saw {agent}'s gun | 447.7 | **{agent}'s gun seen** | 288.9 |
| `police.attack` | {about} called in an attack | 330.3 | **{about} fought back** | 279.6 |
| `police.post` | the guard's post went quiet | 324.4 | **the guard didn't call in** | 283.0 |
| `police.post (unknown)` | the guard was missed | 292.4 | **the post is empty** | 250.0 |
| `police.body` | a body was found | 270.1 | kept | 254.6 |
| `police.more` | the search goes on | 148.8 | kept | 148.8 |
| `police.cordon` | the district locks down | 168.0 | **the doors lock and exits close** | 209.6 |
| `police.loud` | shots earlier | 110.1 | **there were shots earlier** | 175.4 |
| `police.loud (alert)` | security was searching | 168.8 | **security was already searching** | 214.6 |
| `police.split` | Popovic is gone | 132.0 | **Popovic has left by car** | 170.1 |

## The places

Two changed. A place has 108.8 px, the width of "at the service drive", so the HUD's own stand names keep their words ("the square's bay", "the service drive").

| place | before | px | now | px |
|---|---|---:|---|---:|
| `cabinet` | by the control cabinet | 124.3 | **at the cabinet** | 77.1 |
| `service-drive` | on the service drive | 113.1 | **at the service drive** | 108.8 |

"the cabinet" is the HUD's own short name: the label of an act there already says it ("at the cabinet", `HostActivity.ActPlaceName`). The hover and the Intel keep "the control cabinet".

## The cards' own lines

Unchanged: all fit. "8 health; cover him and Popovic · 4:44" 226.0 px, "8 health: get him to cover · 4:44" 187.3, "the rest get clear by 9:59 · 4:44" 185.9, "three ran from their posts · 4:44" 185.8, "Go when the way is clear · 4:44" 184.3, "follows Popovic's car out · 4:44" 182.6, "their car across our way · 4:44" 176.7, "8 coming for Popovic · 4:44" 163.7, "off Orbit Walk · 4:44" 122.9, "official door · 4:44" 109.8. The police card's "here in {s}" stands apart on its ticket; the seen card's line is cleared by the host; the seal is never a card.

## The loop: guides /19, /20 and /21

- **/19** (`short-whys19.ts`): the four cause families write only the short. The title and the line are the item's facts, as is today's widest reading and its px. Each field carries its room in px (`lozenge`): 146.7 for a short with a place, 255.6 for one with none. The places get their own family, `why/places`, with 105 px each. The code check measures every line at its widest. The rubric: USEFUL (a short says what happened at a glance; the line has who, whom and where), TRUE (causes the player must tell apart read apart), PLAIN (no AI-sounding words, no rule of the game), SHORT (the box).
- **/20:** the /19 places run couldn't fit the HUD's stand names in 105 px ("at the square's bay" 107.7, "at the service drive" 108.8), and the judge rightly wouldn't rename them. So a place has 108.8 px, and a short with a place has 142.9 px. /19's runs rebuild with /19's room. The /19 accepted lines were applied first, so the /20 runs began from them and changed only what /20's room needed.
- **/21:** the /20 places run held at 9 for four rounds on one finding: "by the cabinet" against the hover's "the control cabinet". The family's facts never said the HUD already has the short name. /21 says so, for the places only. The rubric reads as at /20, so a /20 run could still continue under it.

| family | guide | result | scores | calls | tokens | run |
|---|---|---|---|---:|---:|---|
| `why/gunfire` | /19 | **accepted** | 7, 8, 10 | 6 | 126,505 | `2026-11-08T18-44-34.203Z-why-gunfire` |
| `why/police` | /19 | accepted, then superseded | 7, 9, 9, 10 | 8 | 118,570 | `2026-11-08T18-44-34.203Z-why-police` |
| `why/reported` | /19 | out of revisions | 7, 8, 9, 10 | 8 | 132,029 | `2026-11-08T18-44-34.203Z-why-reported` |
| `why/witnessed` | /19 | out of revisions | 7, 8, 8, 9 | 8 | 161,199 | `2026-11-08T18-44-34.203Z-why-witnessed` |
| `why/places` | /19 | out of revisions | 6, 7, 7, 7 | 8 | 170,796 | `2026-11-08T18-46-24.964Z-why-places` |
| `why/witnessed` | /19, continued | out of revisions | 9, 9, 10, 9 | 8 | 155,922 | `2026-11-08T18-48-50.778Z-why-witnessed` |
| `why/reported` | /19, continued | accepted, then superseded | 10 | 2 | 33,673 | `2026-11-08T18-48-50.779Z-why-reported` |
| `why/places` | /20 | out of revisions | 9, 9, 9, 9 | 8 | 166,164 | `2026-11-08T18-53-09.792Z-why-places` |
| `why/police` | /20 | out of revisions | 8, 9, 8, 9 | 8 | 120,092 | `2026-11-08T18-53-09.792Z-why-police` |
| `why/reported` | /20 | **accepted** | 7, 10 | 4 | 63,097 | `2026-11-08T18-53-09.792Z-why-reported` |
| `why/witnessed` | /20 | out of revisions | 6, 8, 9, 9 | 8 | 160,324 | `2026-11-08T18-53-09.792Z-why-witnessed` |
| `why/police` | /20, continued | **accepted** | 8, 10 | 4 | 60,296 | `2026-11-08T18-56-41.809Z-why-police` |
| `why/witnessed` | /20, continued | **accepted** | 10 | 2 | 38,718 | `2026-11-08T18-56-41.809Z-why-witnessed` |
| `why/places` | /21 | **accepted** | 10 | 2 | 39,704 | `2026-11-08T18-57-39.653Z-why-places` |

The bold runs are the ones in `text.json`. `why/gunfire`'s /19 lines all fit /20's room too (widest 140.1 of 142.9 px), so it wasn't rerun.

`apply` ran with Aster's restore list less the two families now accepted at /19 and later: `bark/ack-bodies,bark/arrive,bark/guard-call,bark/read-people,why/on-the-way`. Before it, `apply` failed on a cut it had already made (`bark/rivals`' `ai-gone/rivals`, gone since the sweep). A cut whose row is already gone is now skipped (`short-apply.ts`); the dry run then changed nothing.

## The five by hand (guide /22)

These five passed the loop's judge, but they were the weakest. After the round, Aster asked for them by hand, to brief 102's bar:
- **"told to drop the gun"** (`alert.warned`) had no subject, so at a glance it could read as an order to the player.
- **"police say a killing"** (`alert.confirmed`) was awkward.
- **"a victim woke, spoke up"** and **"Popovic spoke up"** (`alert.woken` and its Popovic variant): two clipped verbs, and "Popovic spoke up" leaned on the line's "reported being taken after he was let go".
- **"found knocked out"** (`alert.down`) had no subject either.

Each was briefed from the engine first. Only the guard or an officer gives the gun warning (`GameIntent.TodayWarns`; the host names them "the guard" and "an officer"). A confirmed killing raises the Alert only when the police check a body that a passer-by reported as knocked out (passers-by report every body as down: `GameTakedown.KindOf`), or a post they come to (`GameResponse.Confirm`). A woken report is the victim's own once they come round. The Popovic variant is his own report once a seen Take is let go (`GameCancel`).

Scored once by Motive on Claude Opus 5.5, approved as Sky, as brief 102's hand rounds were: the family as it plays, the hand shorts in place of the round's. `why/witnessed` scored **5** with 4 findings (`runs/style-agent/short/2026-11-08T19-24-15.250Z-why-witnessed`), `why/gunfire` **7** with 2 (`2026-11-08T19-24-19.150Z-why-gunfire`). Every finding was on one of the five, none on a line the round had left alone, and the code checks were clean. The judge would have put the round's five back (its VOICE rule keeps today's words where they fit), but the brief is that those five are weak. So each was rewritten from what its finding found wrong, and not scored again. Widths are at the widest fills, with " at the service drive · 4:44" after them, in the 296 px box.

| cause | the round's | px | Opus read | its finding | **now, by hand** | px |
|---|---|---:|---|---|---|---:|
| `alert.warned` | told to drop the gun | 268.1 | an agent told to holster | "an agent" names nobody by role, and "holster" breaks the match with the line's "drop it" | **{who} said to drop it** | 292.4 |
| `alert.confirmed` | police say a killing | 258.9 | police call it murder | the glossary and this cause's line say "killing"; "murder" is a new word with a crime-drama tone | **police call it a killing** | 270.9 |
| `alert.woken` | a victim woke, spoke up | 291.3 | a victim woke and talked | "talked" points to the Talker rule; "victim" is a police-log word | **someone woke and told** | 289.5 |
| `alert.woken` (vale) | Popovic spoke up | 255.3 | Popovic, let go, talked | the doubled commas are slow at a glance; "talked" again | **Popovic told when let go** | 295.3 |
| `alert.down` | found knocked out | 260.2 | a knockout victim found | "knockout" for the glossary's "knocked out"; "victim" again | **knocked-out body found** | 291.8 |

- **"{who} said to drop it":** the subject is whoever gave the warning, by the name the player knows them by: "the guard said to drop it", "an officer said to drop it". The words are the cause's own title and line ("Told to drop it", "{who} told {agent} to drop it."). The agent's role doesn't fit with a place ("the Enforcer told to holster" is 153 px of 142.9).
- **"police call it a killing":** Sky's shape ("Police call it murder"), with the word the glossary and the line use.
- **"someone woke and told":** "woke" is the cause's own title's word ("Woke and reported it"). The line's "came round" doesn't fit with "and told" ("someone came round, told" is 153.3 px of 142.9). "Told" covers the guard's radio and anyone else's walk to a report point.
- **"Popovic told when let go":** both facts, in one clause with no commas, as the base reads. It is the tightest at 295.3 px of 296 (Unity draws it about 0.4 % narrower).
- **"knocked-out body found":** "knocked out" and "a body" are the game's words ("Carry: Lift a downed agent or a body", "a body seen carried"). It reads as "dead body found" does, apart by the one word that matters. "Someone found knocked out" is 162.7 px of 142.9.

In `text.json` each of the four causes carries `hand` (`why.schema.ts` `WHY_HAND`; the host reads only title, short, line and variants). From guide /22, a writer run leaves a marked cause out of its items and gives its shorts in the family's context. `apply` holds it, as it holds a hand row of the dialogue (`short-apply.ts`). The hand edits, with what Opus read and said, are in `briefs/101/fable/hand-whys.json`. The sheet is `briefs/101/fable/lozenge.html` ("The five by hand").

## Host tests (Latch's file, three pins)

`tools/game/hostlogic/HostLogic.Why100.cs` read three of the old shorts. Each pin now follows the new words, and its test still checks the same thing:
- The guard's attack call: `r.Short == "the guard fought back"` (was "the guard called in an attack").
- The seal's cause on `fail-enforcer-down`: `r.Short == "the Enforcer's gun seen"` (was "the guard saw the Enforcer's gun").
- The shift guard's radio (`by.another`): the ShortWhere still ends " at the Office door". "Another guard … the guard … at the Office door." is now read on the rise's line, which keeps who and whom.

## Open, and next owners

- **Glyph:** confirm the box. 296 px is Latch's audit. A guide's families are built from the box and the place room (`LOZENGE`, `placeRoom` in `short-lozenge.ts`), so a different number is a new guide version and a rerun of the families it moves.
- **Latch (next round): a hover on the lozenge that shows the card's full line.** Today hovering the lozenge only tints its ring (`HudToolkit.Crucial.cs:29`, `RingColour(crLozInline, TurqLight)`). The line shows only in the Alert meter's list and in the Journal. The shorts no longer say who saw it, whom it was about or which agent: "knocked-out body found" names neither the finder nor the one found, and "{who} said to drop it" names the watcher, not the agent. With the hover, the reason is always one hover away. Inputs: each card's `Why` row already has its `Line` (`HostWhy.cs`). Done-check: hovering any Alert or Police card on the lozenge shows the cause's line, filled, and the line is gone on leave. Next owner: Glyph checks it against the corner's look.
- **Aster / Sky (not in this round's five):** `alert.confirmed`'s title and line still say "confirmed" ("Killing confirmed", "The police checked {about} and confirmed a killing."), a word on Sky's AI-sounding list. The /19 families leave titles and lines as facts, so no run touches them. If Sky wants them gone, by hand: "Police find a killing" and "The police checked {about} and found it was a killing." `alert.warned`'s line also ends on a rule of the game ("Holster, or it's called in.").
- **Latch:** the three pins above are in your file. Host logic is 186/186 with them.
- **Aster:** the restore list for the next `apply` is `bark/ack-bodies,bark/arrive,bark/guard-call,bark/read-people,why/on-the-way`.

## Checks

The loop round:
- **Export** (`bun run guard bun tools/sim/unity.ts --tree`): headless 472/472 and range 99/99 replay identically. `text.json` parses, and the player's copy matches the working tree's.
- **`bun run hostlogic`:** 186 pass, 0 fail, on the exported words.
- **`bun test tools/style-agent contracts/missions/handover tools/dialogue`:** 364/364.
- **`bun run typecheck`:** clean.
- **Every saved style-agent run rebuilds exactly:** 179 (24 conversations; short lines /1–/18 141, /19 7, /20 6, /21 1).
- **Scratch:** `out/scratch/fable/lozenge/` (the measure, the sheet, the run tables, the logs).

The five by hand (guide /22):
- **Export** (`bun run guard bun tools/sim/unity.ts --tree`): headless 472/472 and range 99/99 replay identically. `text.json` 79cee672de76 parses, and the player's copy (`unity/Assets/StreamingAssets/sim/text.json`) is the same file.
- **`bun run hostlogic`:** 186 pass, 0 fail, on the exported words. No host test reads the five.
- **`bun run test tools/style-agent contracts/missions/handover tools/dialogue`** (the same files, the parallel runner): 368/368, the four new ones in `hand-whys.test.ts` among them.
- **`bun run typecheck`:** clean.
- **Every saved style-agent run rebuilds exactly:** 181 (24 conversations; short lines /1–/18 141, /19 7, /20 6, /21 1, /22 2).
- **`apply --dry`** with the restore list above: nothing changes; the four hand causes are held ("why down, why woken, why confirmed, why warned").
- **Scratch:** `out/scratch/fable/lozenge-hand/` (the measure, the edits, the logs).
