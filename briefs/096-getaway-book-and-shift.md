# 096 · The getaway, the book as the briefing, and the shift at 5:30

Owner: Aster. Opened 2026-11-04, from a tester's Linux play of 0.2.234 and Sky's notes the same day.

## Sky (2026-11-04)
- The tester "could get in the getaway car but then couldn't get out - since they entered without Vale" · "tester is still confused by getting out of the car. I think it needs to be also an action in the action ring".
- "we need an icon for the book - I hate keyboard shortcuts - people have to learn them".
- "I think we need to move the clock back to 5:30 for when popovic comes out" → asked 5:30 (the whole shift +40 s) or 5:00: **"5:30"**.
- "on linux the game start mission brief didn't load up everything properly. It's also out of date. And I think doesn't look great compared to the book design we now have" → asked whether the start briefing becomes the book's Job page, mocked first: **"yes"**.
- "the logs should be in the book, given time pauses there, users can see it. Which means the main feed bottom right of the screen should really only show crucial info that lasts until clicked on to dismiss (then shows the next etc)". On the crucial list: "not sure - but it's so fast right now I hardly bother reading it".
- On an end card ("Rivals took Popovic. The rival crew got to Popovic first.", seed 1562486586, after "Popovic reached the getaway car"): "does this seem fair?" → Aster: the rule held (fight.unguarded: a held car with Popovic aboard and nobody drawn outside loses), but the card blamed the wrong thing, the story stopped at the car, and guarding is taught nowhere. Sky: **"the End card needs to be plain english and clear but not too formal"**.
- "couldn't find a way for Fixer to pressure anyone" · "I thought pressure was only available when there with the enforcer?" (no such rule exists: §04 has Pressure as the Fixer's, needing the need known; open question below).

## Sky (2026-11-04, later): the tester's pickup run
The tester paid the driver, drove the getaway car to the service drive, then waited. The driver brought Popovic round, stopped at the drive's mouth (the drive was taken), the cell took Popovic, killed the rivals, boarded and drove off through Popovic's car, and won. Sky: "why didn't the entrance camera notice the bribe as they hadn't disabled it" · "the driver blocked off the service bay as the getaway car was there, but the getaway car just drove through it" · "what concerns me a little is how much they stood around ... shouldn't the patrol robot at the service bay have alerted them so turning that robot off would have at least occupied 30-45 seconds".
- **Bribes on camera, decided: "yeah, they should have been punished for not turning off camera".** A Pay (and Pressure) inside a live camera's view (not looped, not blinded) is recorded. Changes §04 private (brief 014, Sky 2026-10-16/17: turning is private except Pressure) for cameras only. Motive sets the price (Aster's draft: no Alert at sight; Trail; a lead on the Fixer's face for investigators; the camera's eye in the price before commit).
- **Box-in, decided: "yeah".** Build GDD §06 cars' clause (never built; GoQuery checks only empty, no driver, held): a standing car on another car's way out refuses its Drive and Go and names the car. The turned driver, finding the service drive taken, pulls in at the kerb clear of the drive's mouth, so his own car never traps the cell; the rivals' block of brief 082 uses the same rule.
- **The wait at the service drive (Aster's proposal, for Sky).** Brief 082's decided-not-built "a patrol could see it": the getaway car standing on the service drive is noticed by the yard's patrol robot on the loiter clock (attention [S]: report at 45 s at Heat 0-2), reported, Alert 1, a robot checks it. Answer: the Tech reroutes the patrol (30 s, lasts 1:55, so its timing matters) before parking there. Agents sitting in a parked car today keep no loiter clock (off the street). Note: the 5:30 shift adds 40 s to any early wait, so the wait has to be busy.

- Sky (2026-11-04): "with the camera loop and robot loop then that should make them busier, but means the scout doesn't need to do anything - but that's up to them - tester only knew how to do this because they played several times". So: no forced Scout job. But a cone is never drawn until learned (§10), so the bribe's camera risk shows as unknown in the price until the camera has been Watched; the patrol's sweep, once Watched, is what times the Reroute. First-time readability is objectives 2, 6, 7 (start page, end cards, the car card).

## Next (engine round 2, after the shift)
Motive writes the rules page (bribes on camera, box-in and the kerbside stop, the parked car noticed, and brief 082's rivals block); Sky reads it; Keel builds it in a worktree; Fable the words; Latch the marks.

## Done (main session, 2026-11-04, not yet in a build)
- **Step out on the car's ring**: one way for everyone aboard (or the selected aboard), 1 s, Popovic stays in; after Board and Drive, before a withdrawing Go (last); Go still leads once Popovic is aboard. Host only: the engine's move on a boarded operative (§06 Board). `HostGetaway.CarStepOutRows`, `HostHold.DoersOf`, `ContextRank` tier/sub; test `OnTheCarStepOutComesBeforeTheWithdraw`; hostlogic 90/90. Word "Step out" is a placeholder for Fable (`actions.step-out`).
- **Linux briefing blank prints**: `tools/game/briefstills.ts` placed stills only in a macOS bundle. `--place-only --app <TheSuborned.x86_64>` copies the Mac-taken set into a Linux build; README's Linux steps carry it; placed into `out/build/linux`. The tester's 0.2.234 archive is stale.

## Objectives
1. **The shift at 5:30** (Keel, engine round in its own worktree; Waymark the mission data; Aster the GDD version). Every time of the shift moves +40 s together: chime 4:30→5:10, driver's last turn 4:40→5:20, Popovic out 4:50→5:30, at his car 5:10→5:50, close 7:30→8:10. Rivals (2:35) and the guard's marks stay. Done when the site schedule, mission.json, brief.json and text.json agree, GDD §07 is a new version, the scenarios pass, and the route bands across seeds hold (brief 080: fairness, not one script).
2. **The briefing is the book's Job page** (Glyph mock first, Sky's eyes, then Latch). The start opens the book on Job with the clock stopped: target, two ways in with their conditions, the clock, the cell strip, Begin. One design and one data source for both, so times and rules are fixed once. Drop what's stale (the false tip as a cell line).
3. **The getaway in the book** (Fable words, data only): under each way, "Popovic is in the car" → "Go: he's out" → "the rest get clear before the seal"; one car line on the Cell page (Board, Step out, Drive, Go).
4. **The book gets an icon** (Glyph, Latch): a clickable book on the HUD; J stays as a second way. Glyph lists every other key-only feature (B, L, I, R, T, H…) for Sky.
5. **The feed and the Journal** (Glyph mock, Sky judges played): the Journal section holds the full log (today's L history) with time and place; the on-screen feed keeps only what changes what you do now, one card at a time, staying until clicked, "1 of n · next"; a "new" dot on the book icon. The crucial list is Glyph's proposal for Sky to judge in a build (Aster's draft: Alert up, seen, police called or the seal starting, rivals coming, an agent down, Popovic out, a contact turned or refused). GDD §10 changes once Sky has seen it.

6. **The end card says what really happened** (Fable words; Latch wires). Plain English, clear, not too formal. The headline and line come from the engine's own reason for the fail (each fail code has several causes: `rivals-reach-vale` is a beaten pickup, a tailing wheelman, a downed Enforcer, or an unguarded held car), never one generic line per code. The story runs to the end: after "Popovic reached the getaway car", why Go was refused (held by n rivals) and that nobody guarded the car. Every fail's card is reviewed the same way.
7. **Guarding the held car is visible before it costs** (Glyph, Latch; Fable words): when rivals start for the car (`Game.RivalsForCar`), a crucial feed card that stays until clicked ("Rivals moving on the car: get out and draw, or down them"); the car's ring says why Go is refused; the book's getaway lines add "while the car is held, someone guards it outside, gun drawn".

## Open (Sky)
- ~~Pressure~~ **Settled, Sky 2026-11-04: "leave for now if it's there after listen".** It is: before the Scout's Listen lands the Fixer's ring shows Pressure locked ("needs their need — Listen (Scout) first"); once it lands Pressure is open. Held by hostlogic `PressureIsOfferedOnceTheListenLands`. No rule change.
- The price of driving to the service drive (brief 082, decided, not built): Motive's rules page, then Keel.

## Next
Keel (1) and Glyph (2, 4, 5 mocks) in parallel; Fable (3 and the Step out word). Then one Mac build for Sky and a repackaged Linux build for the tester.

## Shift landed (Keel, 2026-11-04)
Branch `t3/delay-handover-shift` (worktree t3-5443ef4e): `9389787e` the shift (run c050; engine 394/394, C# 2810/2810; win bands at 200 seeds hold: quiet-a 100 %, quiet-b 91.5 %, quiet-g 79 %, thin-quiet 87 %, prepared-loud 75.5 %) and `c22a8484` GDD v0.38.0 (Aster; crew regenerated). Not yet merged into `work/096` (the round's branch): waits for Latch's edits to text.json. Known reds for Motive: quiet-a's final fight median 14 s misses B-7 (15–40 s) and B-11's floor; R-7 late window for quiet-g. quiet-g's knockout moves after the 4:00 mark (guard 74 now crosses the forecourt 2:10–4:10).
Left for the build: Latch retakes the briefing stills (or drops them with the Job page), and the hard-coded "7:30" in PlayText.cs and HudToolkit.cs; Fable: tools/dialogue/conversations-page.ts "a 7:30 mission"; Pulse: handover.rounds.json stale since 080.

## Sky's calls on the mocks and the rules page (2026-11-04): "all. recommended"
Glyph (briefs/096/glyph/index.html): (1) the start page drops the stills and plan, keeps the faces, hovering a way turns the street to its place · (2) the rivals' 2:35 shows from the start, on the start page and the book · (3) the Book button bottom right in the notes key's place, icon + "Book" · (4) a card leaves on its own only once it stops being true; the rest wait for a click · (5) no buttons for speed or next event · (6) "Menu" in the book's side menu, no HUD button · (7) queued cards most urgent first: seal, police, agent down, rivals for our car, Alert, seen, Popovic out, rival car, turned or refused, body found. With Glyph's key-only fixes: the pause glyph a button, turn buttons on the minimap rim, Alt's breakdown on hover, the "X stop" plate a Stop button.
Motive (briefs/096/motive/rules.html, values.json): (1) cameras record Pay and Pressure only; Help stays private · (2) a taped bribe: Alert 1 at the desk's next scan + 3 s, +1 T and a lead on the Fixer's face; a later Loop doesn't undo it · (3) the rivals' block waits for the split · (4) a block ends when a beaten crew's wheelman, still in the car, drives it off, else only by blowing the car up · (5) on the drive-round way the rival car blocks the drive's mouth; on the split, the cell's car at the square once the Enforcer leaves.
Open (Aster to bring options): route B's middle, about 3:10 to 5:30, has nothing to do; the wait has to be busy, not skipped. Aster's options: (1) the rival lookout a job for the cell (turn, mislead, or take down and stash him) instead of the turned driver warning him off; (2) Lukac reachable only late, a timed window; (3) thinning the three rivals posted round the cell's car before the pickup. Sky 2026-11-04 on (2): "don't like 2 - new players would never know about Lukac until late": dropped.

## Next
- Keel (worktree from work/096): bribes on camera, box-in (car-boxed, drive-boxed, stand-boxed), the kerbside stop, the parked car noticed; the bays' use field and the kerbside and block points (Vantage's data, Splice's site build); the quiet route B plan loops before paying and times the Reroute, plus paid-on-camera and park-early plans (Waymark's). Not the rivals' block (split).
- Latch (shared tree): Glyph's start page as the book's Job page, the Book button and key-only fixes, the slim feed and the Journal; then a Mac build for Sky and the Linux build repackaged for the tester.
- Fable after Latch: the words for the new UI and for Keel's rules.
- Aster: GDD v0.39.0 once both land (§04 private, §05 attention, §06 cars, §10 briefing, book, feed).

## Route B's middle (Sky, 2026-11-04)
"yeah, I think 1 and 3 make sense - they see your car, so they come, block you off and get out the car. And this makes something interesting - a player who's lost before, can shoot the rivals car and destroy it on the way as they come making fight easier - but that's for those who already know the level so earnt that knowledge".
So: (1) the rival lookout is the cell's job; (3) the rivals posted round the cell's car can be thinned before the pickup; the rival car, seeing the cell's car, comes and boxes it in (the box-in rule Keel is building) and its crew gets out; a car shot and destroyed on its way in makes the fight smaller. Knowledge earned by losing is fair here because the rival car is seen coming (2:35, on the clock from the start). Next: Motive with Pulse, a rules page, as before.

## Sky's calls on the rivals page (2026-11-04): "all recommended"
briefs/096/motive/rivals.html, rivals.json: (1) a shooting no security saw, with nobody near and only rivals hit, is filed as a gang shooting: Alert 2 and the Trail stand, the seal waits until the cell is seen · (2) the false tip on the rival car is retired; Mislead stays as the Scout's word to a rival on foot · (3) the crew has temperaments (Mercenary takes $30,000 and walks off; Loyal refuses and tells his crew; Listen shows which) · (4) the block on arrival on every route (A and G too). With Aster's addition: a crucial feed card when the rival lookout is posted watching Popovic's car, since ignoring him fails route B.
Next: once Keel's box-in round (bribes on camera, box-in, kerbside stop, parked car) lands, the rivals round: Pulse's routine as data, Keel's build with RIV-1 to RIV-30, Waymark's plans and bands; Fable, Glyph, Chime after.

## Sky (2026-11-05)
- Route B's squeeze under the block (0.41.0: B 45 %): **build the lend now** ("yes"), not a later close. Motive's addendum (briefs/096/motive/lend.html) also covers the veteran's shot (0 %) and route A's long final fight (50 s); then Keel builds it on `t3/rival-block-and-lookout`. 0.41.0 stays off `work/096` until then; 0.40.0 is merged.
- The seal leaves the slim feed ("sure"): its plate under the clock is the one countdown. Done in HostCrucial.cs (the Journal keeps the event).
- The tester waits ("wait") for the build with the camera, box-in, rivals and the lend.
- No Linux build this round ("don't need to build the linux"): the round ends in one Mac build for Sky.

## The lend built (Keel, 2026-11-05)
Branch `t3/lend-cell-car-engine-round` (was `t3/c800160e`; from `work/096`, the rivals round `t3/rival-block-and-lookout` merged in): engine 0.42.0, Motive's lend addendum: the
lend and two Go's behind `car.lend.split` (on), the police close in 60 s after Popovic is out, L10's drive-mouth block, F1-F3, V1-V2; the
lend plans and route-b's reference (c052). Report briefs/096/keel/report-lend.md; GDD text gdd-text-lend.md; bands bands-lend-after-200.txt.
Next: Motive (the misses and the prepared-loud/L6 conflict), Waymark (MC-0/11/13/14), Fable (words), Glyph and Latch (two Go's, the
countdown, the left-behind flags), Chime.

## Sky's calls on Motive's misses page (2026-11-05): all recommended
briefs/096/motive/misses.html, misses.json: (Q1) a quiet final fight may be as short as 10 s when preparation earns it: band 10–40 s · (Q2) the veteran's shot is fair at "Popovic out, the other three caught": V3 (the police count "fired" only for a shot they saw, or once the cell is placed) · (Q3) going loud on route B stays the hard way (30–40 %, a report-only line); the front door is the loud route. With the fixes that follow earlier calls: the police clock after vale.out 60 → 75 s, G1 (a Go waits for an agent climbing in), Waymark's thinned plan and late Go, bands B out ≥ 85 % with no ceiling and G ≥ 75 %.
Also (Sky, "yes, crucial card", after his Air run, seed 793475664, engine 0.39.0: the Enforcer shot down 2 s into a 4 s board carrying Popovic, the warning only an icon): the Enforcer badly hurt is a crucial card, first while he carries Popovic (65d8bbb7).
Next: Keel (worktree), engine 0.43.0 with Waymark's plans; Fable's words pass (running); then the Mac build. No Linux.

## Motive's misses built (Keel, 2026-11-05)
Branch `t3/7f898491` (from `work/096` at 7dad862c): engine 0.43.0. V3 (security.unclaimed.fired: "fired" counts only once the cell is placed),
the police close in 75 s after vale.out, G1 (car.go: a Go waits for a Board under way), F2's words, Pay on a rival needs a Watch first (Motive's
rivals page); host reads CrewActQuery, LentCarWatcher, GoWouldWait/GoWaiting. Waymark's plans (by Keel): the late Go on the countdown, the
thinned plan by a posted man, veteran-drive (17.5 %, report only); MC-0/11/13/14 green. Every L96 band in. Report briefs/096/keel/report-misses.md;
GDD text gdd-text-misses.md; bands bands-misses-after-200.txt; record c053.
Next: Latch (CrewActQuery in HostCrewTurn, the "+1 T if seen", G1's card), Fable (go.waits, go.missed), Glyph, Chime; Motive (veteran-drive).
