# 013 E · Takedowns in play: the HUD spec (Glyph, 2026-10-16)

Sheet: `briefs/013/glyph-takedown-hud.png` (3220 px wide; sections 1 to 10 match the numbers below). Glyph files: `briefs/013/glyph-takedown-hud/` (11 monoline glyphs, @1x 32 px and @2x 64 px, white, tinted in USS like the atlas). Everything else is the 011 kit (round 4 v10 and 6b): no new style, no new panels.

Built from: `briefs/013-takedowns.md`, `motive-takedowns.md` (rev 4), `waymark-handover.md` (c010), my own `briefs/011/ui/review-2026-10-16/` rules, `contracts/missions/handover/text.json` (all words come from there; I wrote none).

Mock-up honesty: backgrounds are Latch's HUD plates; bodies, robots and officers are capsule proxies (Forma and Vela draw the real ones); prices are Motive's and Waymark's examples (guard knocked out at 2:01, level S2), so the build fills them from `QuoteTakedown`.

## Rules that hold everywhere
- **Price before commit.** The chip is the short price; the card is the full price. Said once each (one fact, one place).
- **Unknown stays "?"** exactly where the engine says "?" (watcher, temperament, routine, servicing round, unlearned cone).
- **At most two icons over a person.** A report in transit replaces the cast icons for its length. A body has one marker and one optional second disc (tied).
- **Calm.** Hover is a ring. Stash plates, body markers (beyond the first), cones and numbers follow the review's three layers: contextual, or on Alt.
- **Signals reserved.** Nothing new is red, amber or violet. Held doors, bodies and stashes use ink, cream and teal. The only red is the kit's slashed "seen" pictogram on a chip or card row (the display exception).
- **Every state has a shape or motion cue** besides colour.

## 1 · The takedown row (Enforcer's plate, chip)
- **Knob 4 (the empty one) = Takedown** (icon `takedown`). Enter, Take, Fire keep 1 to 3. Check against the build: if the Enforcer's plate is not Enter, Take, Fire plus an empty 4th, put Takedown in the first empty knob and tell me.
- Carry, Put down, Hide and Tie are **not knobs**. The Fixer's plate is full, and four agents share them. They are rows in the right-click panel (on a body, a stash, or ground while carrying), and the plate swaps while carrying (§4).
- **Armed (4):** the name plate "Takedown" and two way pills under it: Knock out (`way_knock`, the default, turquoise outline when on) and Kill (`way_kill`). **4 again flips.** Kill is never the default. Esc or right-click on ground disarms.
- **Chip** (kit `price_chip`, lead icon = the way): `$` · time · Trail (if found) · Alert (if found). Final mark = the "seen" answer from `Seen.Seen`: `mark_ok` nobody known; `mark_unk` an unknown watcher; `mark_no` seen, and the Alert shown is `AlertIfSeen` (3).
- Seen (yes) puts up the existing **going-loud card at the aim**; the 0.5 s hold commits. No new confirm. A kill that nobody is known to see needs no extra confirm: the card and the chip are the price.
- Refusals are one pill at the click, with `text.json` lines (`takedown-vale`, `takedown-how`; armed target who sees you: `takedown.fails` preview "he sees you: a fight").
- Chip timing as the review: hover ring first, chip after 0.6 s, at once when armed.

## 2 · The takedown card (right click a person with the Enforcer selected)
The kit action panel, three cards, in this order:
1. **Before you act** (the same for both ways): `Seen` rows (known watchers' answers, one line), `Aware` ("from behind" ✓ / "he sees you: a fight" / ?), `People` (within 13 m: name and verb, "?" until the temperament is known, "reports in n s"), `NextMark` (absent until a Watch of his routine), and `AlertIfSeen` with `PoliceInS`.
2. **Knock out** (key 1): `does` text; price row `$ · S · TrailIfFound · AlertIfFound`; "Found: unit in ~PoliceInS s · holds the Office door ~HoldS" (door icon `door_held`; names from `HoldsDoors`); "Never found: comes home, +4 T, a statement" (`IfNeverFound`); "Wakes `WakesAt` · knows where and when, not your face (or: and your face, if `Aware`)".
3. **Kill** (key 2): the same rows; found = police + search 30 m + Menace; never found = missing, +2 T, the area hardens, the case stays open; "Never talks · n ties (names if known)" (`Ties`).
- **Hover a way's card:** each door in `HoldsDoors` shows the hollow dashed lock "would hold ~1:00" (§6). Nothing is drawn for a door the body could not be seen from.
- Vale gets no card, only the refusal pill.

## 3 · Body markers
- **One 24 px marker per body over the torso.** Knocked out: round disc, glyph `body_out`, a ring that fills to `WakeAt` (120 s) and pulses (double ring) in the last 15 s. Dead: **square plate**, glyph `body_dead`, no ring. Tied: second small disc, glyph `restrain`. Found (a report about it has landed): dashed outer ring.
- The wake time is the cell's own fact, so the ring needs no Watch. Hover: chip "wakes 4:01 · left 0:54".
- Dead is shown plainly: the body lies where it fell, flat and slightly desaturated (Forma). No blood, no outline, no colour change.
- Markers show for bodies the cell made or saw. Alt shows all, including a hidden one the cell knows about (then the marker sits on the stash plate, §5).
- Signal check: ink, cream, turquoise ring only.

## 4 · Carry
- **Strip:** state "CARRYING", icon `s_carrying`, line 1 "Carrying the guard", line 2 the pace: "walk only · 1.4 m/s" (Enforcer) or "slow · 0.7 m/s" (others). During the 3 s lift the normal progress bar and cancel show.
- **Plate swap:** while an agent carries, knobs 1 and 2 become Put down (`drop`) and Hide (`stash`, nearest fitting stash, with the price chip); the others dim. Not carrying: unchanged.
- **Over the head:** one disc (`s_carrying`). The body is drawn across the carrier's shoulders.
- **Route on a ground hold (0.3 s, as a walk):** dotted route. Dots inside a learned cone are ringed (hatch cue, no new colour). Unlearned cone: plain dots and "?" on the chip. The chip (`s_carrying` lead): time, Alert; footer: destination kind ("Open ground: found if seen (?)" or "Dumpster: 1 free"), doors it holds (`DoorsInSightOf`, "out of sight of doors: holds none" or "holds: Office door"), and "seen on the way: who, n s".

## 5 · Stashes and quiet places
- **When:** while any agent carries, on hover, and with Alt. Never idle.
- **Plate:** icon `stash`, name, pips (Dumpster 1, service door 3; filled pip = a body inside), leader to a diamond on the object.
- **Behind a building (D1, D2):** drawn after the post grade, over the fade, **hollow with a dashed leader** (no line of sight). Still hoverable and clickable; the fade is left alone.
- **Hover with a body carried:** chip (time 5 s or 10 s, Alert "?" until watched) and a footer: free room, "emptied at m:ss" once the round is watched ("?" before), "a search at Alert 2 within 30 m opens it". While a unit opens one, the plate shows a 10 s ring.

## 6 · Held door
- Before the unit arrives: nothing on the door; the unit chip says "police in ~n s".
- At the scene: lock disc (`lock`) over the door, a ring counting to the stand-down, tape (kit `stripes_warn` hatch) on the pavement at the foot. At Alert 3: lock and "locked", no ring.
- An Enter or exit through it is refused with one pill ("Police hold this door. Nobody goes in or out."); the Enter chip carries `mark_no`.
- Preview on the takedown card: hollow dashed lock "would hold ~1:00" (§2).

## 7 · A report in transit
- Drawn only while an agent sees the person (the `sight: in-sight`, `at: reporter` rule of text.json). Out of sight: nothing on the street; the feed card and the Alert lamps say it when it lands.
- One disc over the head: handset (`call`) walking to a call box, `radio` standing, `watch` going to look first; a ring runs to `LandAt`. The walker also gets a dotted line to the call box (the investigators' dotted-lead grammar, in cream).
- **Stopping it:** take him down before it lands. The takedown chip on him says "Stops his report: it lands in n s". Radio reporters have no line and nothing to stop.

## 8 · The swarm
- **Response pill** under the status pill, collapsed like Intel; it exists only once a report has landed: "RESPONSE · 2 officers · 3 robots · 3 coming". Click or R opens it: On the street, Coming (n and seconds), More (+1 unit every 40 s, up to the cap), Seal.
- **Street badges:** robots `signal_guard`, officers `signal_police`. Units within 2.5 m merge into one badge with a count (x2, x3); Alt splits them.
- **Arrival chip** at the screen edge the units come from: arrow, count, seconds ("3 robots in 14 s"). It goes when they enter view.

## 9 · Loop targets
- Right click the cabinet (or a camera): one card per target: entrance, pickup, mast (`camera:mast-314`). Price row, "Blinds: ..." from what the Scout has learned ("?" before a Watch), and "Replaces the loop on the entrance camera" (one loop at a time, `LoopCam`).
- The mast draws three fans from one pole, a number plate on each rim (head 1, then `Fan.More` heads 2 and 3). One loop greys all three; the camera badge greys with them (kit rule).
- Numbers on the mock are placeholders: `QuoteLoop` fills them.

## Assets
- 11 new glyphs, all in `glyph-takedown-hud/`: `takedown`, `way_knock`, `way_kill`, `body_out`, `body_dead`, `stash`, `restrain`, `drop`, `radio`, `units`, `door_held`. Import as atlas singles. The dead plate is the kit `disc_dark_24` look in a rounded-square; Latch can use `plate_dark` rounded or ask me for a sprite.
- Reused: `chip_price`, `panel_card`, `panel_dark`, `panel_slot`, `plate_verbs`, `plate_dark`, `ring_*`, `disc_dark_24`, `stripes_warn`, `signal_*`, `mark_*`, `pip_*`, `s_carrying`, `lock`, `call`, `watch`.

## Asks and open issues
1. **Keel, through Aster:** the host needs read access to:
   - the held door's time to stand-down (`DoorHeld` is a bool; `lastAlarm` is private): something like `HeldUntil(doorId)`;
   - stash occupancy (the pip count);
   - the servicing round time once watched;
   - the carry path's sight (who sees which stretch and for how many seconds: the "seen on the way" line).
   Until they exist, the footer lines say "?" and the ring shows full.
2. **Latch:** the Enforcer plate layout (item 1) and whether the Fixer's carry row fits the panel (it does by the kit rows).
3. **Sky (small):** is a kill with nobody known watching fine with no extra confirm? The card and chip already show the price, and the going-loud card covers seen. I recommend no confirm.
4. Dock default and finish stay Sky's open pick; right-click orders stay provisional until he plays.

## Done-check for Latch
- Arm 4, hover a person: the chip shows `$0`, 6 s, +4, 1, and a tick when the camera is looped and the patrol is away; "?" when not watched.
- Right click the guard: three cards, doors named from `HoldsDoors`.
- Knock out the guard with nobody seeing: a round marker with a filling ring; carry him to R2: strip says "CARRYING", plate swaps, route dots ring inside the cone.
- Leave him at the door with the camera live: the door shows the lock, ring and tape once the unit stands there; Enter shows the refusal pill.
- Screenshot each against the sheet.

## Next owner
Latch (build), with Keel's accessors (ask 1). Forma for the prone pose, Vela for the Dumpster and the door.
