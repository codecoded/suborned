# 039 B on engine 0.35.0: Motive's rulings on Keel's seven questions (2026-10-26)

**Inputs:** Keel's build (`sim/Suborned.Sim/GameCoverPin.cs`; rulebook `weapons.oneInput`, `focus`, `takeCover`, `pin`, `moves`; the evidence in `briefs/039/keel/`), my proposal (`briefs/039/proposal-2026-10-26.md`), Glyph's `b-final.md`, and GDD v0.28.0 §06. Sky's brief: the game is "too punishing", and fighting must be "a lot more fun".
**Probes:** all run on scratch roots (`out/scratch/motive/039-035/`): the tree's built 0.35.0 DLL, unchanged, run against copies of the rulebook. Nothing in `sim/`, the GDD or `unity/` was touched. The committed copies are in `briefs/039/motive/probe-0.35/`: range scripts, the three route variants, the scripts, and the tables.
Every value here is [S]. The one rule change (ruling 2) needs a GDD wording change, so it goes to Aster and Sky.

## 1. Drawn agents fight on their own from the spring (Keel's reading: yes)
- From the tick the final fight springs or goes loud, a drawn agent picks his own target (`FreeApplies`). GDD §06 already says so: he takes "whoever shoots at him, else the nearest combatant in reach", and a final-fight hostile is a combatant from the spring.
- Waiting for the first shot would hand every ambush a free volley. It would also make a drawn, prepared cell stand idle while the rivals walk to their posts, which is exactly the punishing opening Sky disliked.
- Before the final fight nothing changes: the fight is still on only from a shot by or at the cell. So a drawn agent never opens a loud run on his own.
- **For Keel and Fable (wording only):** with one input, `fire.free-off` ("the fight is off. Drawn agents hold their fire") still logs during the final fight. In `loud` it logs at 3:51.6, but the agents fire again the moment the rivals come into reach at 4:06.8. Under one input in the final fight, drop that line or reword it ("no one in reach").

## 2. Talk down: keep "one of the crew down"; change "hurt" to "hurt or pinned"
- **Measured.** In B-TALK-BLOCKER the condition that binds is "one down", not "hurt". The blocker is hurt at +5.9 s and the first rival drops at +7.0 s, so the Fixer talks him down at +7.1 s and the car is free at +7.2 s. In route-g-board the order is the other way round: a rival is down at +4.7 s, but the blocker, out of line and then in full cover, isn't hit until +7.7 s. That is about when a shot would free the car anyway. So "hurt" makes Talk down pointless on a blocker in cover, and that blocker is the one man it exists for.
- **Ruling.** A rival within 12 m, on a line, who is **hurt or pinned** (shot at in the last 2 s, hit or miss, or under Covering fire) breaks once one of his crew is down. "One down" stays, because it is why he listens, and it keeps Talk down from being an opener that cancels the fight ("the fight always comes"). With the change, a round that misses the blocker, or the Enforcer's Covering fire, sets up the shout. "Free the car without a kill" then holds: the car comes free without hitting the blocker at all.
- **For Keel:** `weapons.moves.talk.hurt` true → `"hurt-or-pinned"`, using the pin test (`PinLeftOf > 0`), and change the refusal `talk-not-hurt` → `talk-not-pressed`. Fable words the refusal: "he isn't hurt or under fire".
- **For Aster:** GDD §06's Talk down row, "a hurt rival", becomes "a hurt or pinned rival". This is a design wording change, so it goes to Sky with ruling 7.

## 3. Covering fire: a round every 0.5 s is right; the recharge starts at the order
- **0.5 s (`covering-fire-every-ticks` 5).** That is 8 rounds in 4 s, which reads as a spray beside the carbine's aimed 2.0 s. It is the host's kick and flash per round and Chime's burst. The rounds don't drive the pin: everyone within 3 m is pinned to the end of the Covering fire whatever the round rate. So the value only sets how it looks and sounds. No change.
- **Recharge from the order** (as built: `Spent` at the order). The same rule then covers all four moves ("ready again 20 s after it is given"). Mark and Steam also last 10 s and 8 s past their order, and the move disc's arc starts at the click, so what you see is what is true. The Enforcer's real downtime is 16 s, which is fine for the one fire he has while holding Vale. No change.

## 4. Mark's "next move": enough, with one host rule
- The end of the walk he is on, or nothing while he stands: that is enough for 0.35. A standing man is where he is, and the brackets already say he is marked. When he breaks, his walk's end is his crew's car, so it shows on its own.
- **Host only (Glyph, Latch):** hold a shown point at least 1.0 s, and replace it only when the new end is more than 1.5 m from the old one. Police re-plan every 1 s, and a point that flickers is worse than none. This is a read, so it doesn't touch replays.
- A man in the open is a valid Mark (Glyph's question 3). His next move still shows, and the Mark costs the Scout 20 s, so no extra refusal code is needed.

## 5. Steam: no grate or manhole on the site qualifies. One heat-main grille per car stand, placed by rule
- **Measured.** I checked the 66 ground pieces the world places (50 gutter drains, 16 manholes; `out/world/harbour-district.world.json`, turned into the block frame). Only one lies within 4 m of any line from the square car's posts to its sides: `manhole-7` at (1100, -5300), 5.4 m from the car. It covers 4–5 of the 16 points round the car, and only at the car's lane end. At the service drive, none covers any. Making existing pieces heat mains would therefore give Steam nothing to do. It would also make every manhole read as a verb.
- **Rule [S]: a heat main is its own street piece** (a round grille, so it reads differently from a manhole: Vela and Glyph). Each mission car stand gets one, on the pavement and wholly clear of any bay, drive or carriageway (its centre at least 0.6 m from their edges and 1 m from any solid), 3.5–6.0 m from the car's centre. Of those spots, it takes the one whose 4 m screen crosses the most lines from the stand's ambush and police points to a 3 m ring round the car. Ties go to the spot nearer the car, then lower x, then lower y. Existing drains and manholes stay ordinary. The rule is a system: a new stand gets its grille from its own posts.
- **The list for the Handover** (block frame, cm; for Vantage to confirm on the drawn pavement, then for Keel's site generator once Datum's height cap regenerates `sim:site`):

| id | stand | at (cm) | from the car | car-side lines screened | floor |
|---|---|---|---|---|---|
| `heat-main-square` | square (car 850, -4820) | **(350, -4620)** | 5.4 m | 118 / 144: all 16 from ambush-2, ambush-4 and police-1 to police-4; 12 from ambush-5; 5 from ambush-1 and ambush-3 (the planter run) | pavement 0, 0.9 m off squareBay, clear of the boarding pavilion |
| `heat-main-service` | service-drive (car 6935, -1500) | **(6685, -1250)** | 3.5 m | 105 / 144: all 16 from svc-ambush-2 and -3; 8–13 from the rest | pavement 0, 0.6 m off serviceBay and the drive |

- My first picks failed the clearance tests: (550, -4620) sits inside `solid:civic/square-boarding-pavilion#1`, and (6585, -1350) sits on serviceBay's edge. The rule's tests above are what rule them out.
- **What it buys:** during the Enforcer's 4 s Board carrying Vale, 8 s of steam hides most of the car's sides from the posts. Steam blocks the cell's own lines too, so it is a trade: nobody shoots the blocker through it. Steam doesn't free the car either, since the hold goes by position, not by line.
- The Enforcer's walk to the car gets no grille yet. If playtest wants one, the same rule applies, with the walk's midpoint in place of the car.

## 6. Balance: which of the 10 moved routes are the rules working, and which need a value change
**Finding: 8 of the 10 come from one value, the cell's pin (`weapons.pin.cell`). The other 2 are the new rules working, and the fix is to re-record them.** Evidence: `probe-0.35/diff-pincell-on-vs-off.txt` (0.35.0 with the pin on the cell against off, the same one-input scripts) and `probe-0.35/diff-0342-vs-nocell.txt` (0.34.2 as recorded against 0.35.0 with it off).

| route | 0.35.0, cell pinned | cell not pinned | verdict |
|---|---|---|---|
| loud, loud-fight, loud-left-downed, loud-left-standing | enforcer-down 4:28.9 | **win** 5:01.7–5:06.8, health lost 8–30 % (0.34.2: 11–44 %) | value: the police shoot first, so the Fixer at 1 m fires at 375 ‰, not 750. security-2 outlives the volley and the Fixer is down at 3:50.7. The Scout alone can't break the rivals, and four of them drop the Enforcer between 4:25.7 and 4:28.9. |
| route-b, careless-reporter-not-stopped, careless-scout-idles-pickup | none at 8:30 | **win** 7:32.2, 26 % (0.34.2: 33 %) | value: the Fixer's 13 m shot at 7:25.0 lands at 281 ‰, not 562 |
| route-b-board | rivals-reach-vale 7:41.8 | **win** 7:35.4, 26 % (0.34.2: 33 %) | value, same cause |
| route-g-board | rivals-reach-vale 4:08.5 | rivals-reach-vale 4:08.5 | **rules working.** The blocker was out of line for the first 2 s, not saved by cover. The script then re-targets the three to rival-1 at 3:47.6, which was recorded on 0.34 when the blocker was already down. With that one order dropped, the sticky focus downs the blocker at 3:56.7 and the run **wins at 3:59.6 (either pin setting); with the Board 4 s later, 4:03.6.** Mark on him changes nothing, since the line is the issue. Re-record with the focus held. |
| fight-safe-cell-down | none at 8:30 (cell 26 %) | none (cell 8 %) | **rules working.** Its premise, "draw and hold fire", went with the stance: drawn agents now fight and win. A cell that never draws is still shot down: on the range, four holstered against three rivals are down at 13.0, 13.0, 17.5 and 20.5 s, either pin setting. Re-record as "the cell never draws". |

- With the cell not pinned, 0.35.0 against 0.34.2: **only those 2 outcomes move.** Health lost falls on almost every win: route-a 22 → 8 %, route-a-pressure-board 50 → 21 %, loud-left-standing 22 → 8 %, route-g-kill-board 33 → 26 %. It rises only on route-g and careless-g-disable (16 → 21 %). That answers "too punishing" in the direction Sky asked.
- The scenario check (`suborned-sim check`), with the cell not pinned (scratch root, the tree's DLL): 357 / 358 ok. The one failure is DP1, the recorded drive session, whose start hash moves with any rulebook byte (it passes on the unchanged copy). Keel re-records it with the value change (`suborned-sim drive-session`).

## 7. Should the cell be pinned too? Recommendation: no (`weapons.pin.cell` false)
The pin should be the player's tool, not a tax on him. Hostiles open every ambush and every loud fight, so with the pin on the cell the cell is pinned from the first round to the last and fights the whole fight at half. B's middle (pin, flank, Mark, cover) still works with the pin on hostiles only.

| the same fight (range, open layout, weapons free, no orders) | cell pinned | cell not pinned |
|---|---|---|
| the Enforcer alone drawn, 3 rivals at 7 m | **Enforcer down at 7.0 s**, no rival down. Then the holstered three are down by 20.5 s (25 hits taken) | 2 rivals down, **the crew breaks at 8.1 s**, Enforcer up (7 hits taken) |
| 4 against 3 rivals at 7 m | broken at 6.6 s, 3 hits taken | broken at 5.1 s, 3 hits taken |
| 4 against 5 rivals at 7 m | broken at 14.1 s, **15 hits taken (65 % of the cell's 23 health)** | broken at 8.1 s, 10 hits taken (43 %) |
| 4 against 4 officers at 15 m | all down at 13.3 s, 12 hits taken (52 %) | all down at 8.2 s, 8 hits taken (35 %) |
| 4 against 5 rivals at 15 m | 2 down, **not broken at 60 s** | broken at 41.1 s |
| the Handover's 42 routes, outcomes against 0.34.2 | 10 move, 8 wins lost | 2 move, both re-recordings |

- **What "on" would buy:** a symmetric rule, and a reason for the cell to take cover against suppression as well as hits. Cover already gives that reason (full ×250, partial ×500), so it adds nothing B needs. It costs about 1.6× the damage taken and up to 1.7× the fight length on the range, and it turns a lone Enforcer from a 2-kill win into a cell wipe.
- **What the player sees with it off:** the portrait's "under fire" state (Glyph's b-final, the same 2 s window) stays as the warning, with no accuracy cost. The first pin a player lands on a hostile reads as his own tool: the duck, the whizz, the stalled advance.
- **A middle way, not measured, [lab] only if Sky wants it:** pin the cell only while it stands in the open (a kneeling agent in cover inside its arc isn't pinned). That rewards the snap to cover. It would need engine code and new numbers, so it is not for 0.35.
- If Sky says yes to the cell pin, nothing else here changes, but Waymark must re-record all 10 routes and the four loud plans must change. In that case they need a second agent on the Office front, because the Fixer at 1 m loses half his volley.

## Value changes for Keel (in this order)
1. `weapons.pin.cell` true → **false** (Motive's [S] call under GDD §06; Sky can overrule through Main). Re-record DP1.
2. `weapons.moves.talk.hurt` true → **"hurt-or-pinned"**, with refusal code `talk-not-pressed` (a rule change; waits for Aster to put the §06 wording to Sky).
3. The `fire.free-off` line during the final fight under one input: drop it or reword it (Fable).
4. Heat-main grilles: none in 0.35. The site generator change waits for Datum's height cap and Vantage's confirmation of the two points above.
No change: `pin-s` 2, `pin-mod` 500, `move-ready-s` 20 (from the order), `covering-fire-every-ticks` 5, `covering-fire-s` 4, `covering-fire-r-m` 3, `mark-*`, `talk-reach-m` 12, `talk-needs-down` 1, `steam-*`, health ops 5 and Enforcer 8.

## Handoffs
- **Keel:** inputs, changes 1–3 above. Done when the check passes 358/358 (DP1 re-recorded), B-TALK and B-TALK-BLOCKER pass with "hurt or pinned" (add a case where a pinned, unhurt blocker is talked down), and the route diff of 0.35.0 against 0.34.2 moves only route-g-board and fight-safe-cell-down. Next owner: Waymark.
- **Waymark:** re-record route-g-board (the three keep the focus on the blocker: drop the 3:47.6 re-target; `probe-0.35/routes/g-board-hold.json` wins at 3:59.6) and fight-safe-cell-down (the cell never draws), then the rest under one input. Done when every route plays to its intended outcome on 0.35.x. Next owner: Latch.
- **Vantage:** confirm `heat-main-square` (350, -4620) and `heat-main-service` (6685, -1250) on the drawn pavement, or move each within the rule. Next owner: Keel (site generator, after Datum's `sim:site`).
- **Aster:** the §06 Talk down wording ("hurt or pinned"), and the cell-pin question for Sky with the table in ruling 7.
- **Latch:** ports 0.35.x after Waymark. Glyph and Latch get the host rule for Mark's point (ruling 4).

Repeat the probes: make roots `base` and `nocell` under one folder, each holding `contracts/rules/rulebook.json` (nocell: `"cell": false` on line 713 only, the formatting kept), `contracts/people/people.rules.json` and `sim/data/`, with the range scripts in `scripts/`. Then run `python3 -I scripts/run.py base,nocell` for the range table, and `bun tools/route-diff/cli.ts --a <dll> --a-root base --b <dll> --b-root nocell --a-scripts <one-input routes> --b-scripts <same> --all` for the routes (the one-input routes come from Keel's `briefs/039/keel/oneinput.py`).
