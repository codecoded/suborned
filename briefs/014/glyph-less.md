# 014 A · Less reading: the play HUD under the six rules (Glyph, 2026-10-16)

Sheets: `briefs/014/glyph/less-grammar.jpg` (sections 1 to 6 below match its sections) and `briefs/014/glyph/less-moments.jpg` (the eleven 013 moments, before and after, plus the Alt layer as a twelfth). Per-moment captures for Latch to match: `briefs/014/glyph/after/NN.jpg` with `NN.hud.png` (the HUD layer alone) and `NN.manifest.json`. Eleven new monoline glyphs in `briefs/014/glyph/glyphs/` (@1x 32 px, @2x 64 px, white, tinted in USS like the atlas): `eye_open` `eye_shut` `eye_unk` `found` `bang` `routine` `cone` `car` `run` `arrive` `pointer`. Scratch that made the sheets: `briefs/014/glyph/scripts/` (reads `out/glyph013` and `out/glyph014`).

Built from: `briefs/014-less-reading.md` (Sky's six rules), `briefs/014/fable-less.md` (the words; I use Fable's strings and numbers, I wrote none), the 011 kit (round 4 v10 and 6b), `briefs/013/glyph-takedown-hud.md`, and the coordinator's two notes: the GTA V model, and Sky's "a minimap may be the key: knowing where Vale is located is not obvious".

Mock-up honesty: the "after" images sit on the 013 play shots with the old HUD and its world tags patched out; my patching smears the street in places (largest in the bottom right and over the old cards). People, bodies and units are the build's own, so a marker sits where the thing is. Prices, times and strings are Fable's and Motive's examples. Latch's real captures replace all of it.

## The idea in four lines
- **The street says it, the line says what to do, the scope remembers it.** Rules are shown by the learned cone, a ring, a lock, a "!", a pip; the one thing to do is one short imperative; what the cell knows and where lives on a small plot scope.
- **A number needs an icon, a verb needs a glyph, a sentence needs a mouth.** Nothing in play is a sentence unless a person says it (a bubble) or it is one of three short lines: the objective, the hint, a story beat.
- **At rest the screen is almost bare: 4.9% of it** (old HUD, same moment, 13.6% by bounding boxes; 34% with the takedown card open, now 8.8%).
- **Kit and look unchanged**: the Syndicate-style signage hotbar and enamel plates Sky picked (round 4 v10), the 013 glyph set, cones red, guards and police amber, rivals violet. New: eleven glyphs, no new panel style.

## 1 · At rest: what stays and how small
Measured on the 06 mock (screen 1726 x 1142): roster 1.6%, plate and busy chips 0.8%, status pill and two keys 1.0%, plot scope 1.5%, street marks 0.1%; total 4.9%.

| piece | size at 1080p | what it carries | what is gone |
|---|---|---|---|
| **Roster** | the kit column scaled 0.8: 90 x 350 | four portraits, health pips, the selected ring, F1 to F4 | the 235 x 95 strips (TECH · BUSY, "Holding the loop", Health 3 of 4) |
| **Busy chip** | 96 x 24, right of its portrait, only while busy | the verb's glyph, a bar, the seconds left (`loop` 31 s, `s_moving` 23 s). Icon only when it has no end ("holding the loop"). Carry: `s_carrying` and `1.4 m/s` | role tag, state word, sentence, cancel button (the ✕ stays on hover) |
| **Order plate** | the kit's 229 x 54, beside the selected portrait | four knobs, keys 1 to 4. Armed: one name plate (way glyph and name) and two round keys for Knock out and Kill; 4 again flips | the caption under it ("Enter / Take / Car"), the two long way pills |
| **Status pill** | about 290 x 40, top right | clock, pause as a glyph, next mark (icon and time, "?" when unknown), $, Trail, Alert lamps. Fable keeps the next mark visible: it is the window the player can see coming | "PAUSED", "closes 8:30" (the close is the objective's), the Intel and Response pills |
| **Two keys** | two 24 px discs under the pill | people known `3/4` (I), history (L, as in the 013 build; H stays "hide hints") with an unread dot | the Intel header, the SCENES bar |
| **Plot scope** | 212 x 132, bottom left | section 5 | |
| **Goal line** | 330 x 30, bottom centre, 6 s then gone | section 6 | the NEXT STEP box, the objective bar |

Contextual, fading: $ and T dim to 55% after 4 s without change and return on change, hover or Alt. The goal line, beats and bubbles come and go. The roster, plate, clock and scope never fade.

## 2 · Order and action cards: one line per way
**Right click** opens one narrow sheet (350 px, about 2.5% of the screen, was 16%): a header (the acting agent's cameo, an arrow, the target's badge, one word) and a row per way. A row is the kit's ivory plate: glyph, name (at most two words, 12 letters), price line, key.

**The price line is Fable's grammar, drawn.** Fields in his fixed order, zero fields hidden, unknown is "?":

| # | field | icon | form |
|---|---|---|---|
| 1 | name | the verb's glyph | `Knock out` (text label) |
| 2 | $ | `p_cash` | `90,000` |
| 3 | time | `p_time` | `7 s`, walk + act |
| 4 | Trail | `p_trail` | `+4`, `+2/hit` |
| 5 | Alert | `p_alert` (the siren; Fable's ▲) | `1`, `≥2` |
| 6 | eye | `eye_open` / `eye_shut` / `eye_unk` in a round badge | seen / nobody / ? |
| 7 | learns | `routine`, `cone`, plus the need and temperament glyphs already in the atlas | a Watch or Listen shows what it teaches |
| 8 | pace | `s_carrying` | `1.4 m/s` |
| 9 | answer | `mark_ok` `mark_no` `mark_half` `mark_unk` | works, refuse, not yet, unknown until learned |

Eye states have a shape each, not colour alone: open (almond with a pupil, drawn red: the small display pictogram the art bible allows), shut (a lid with lashes), unknown (dashed disc, a "?" in the eye). The eye is a round badge so it reads as an icon, never as text, at 16 px.

**One addition to Fable's grammar, yours to keep or drop.** The engine prices Trail and Alert *if the body is found*, not for certain. The 013 chip had no way to say so. I draw the `found` glyph (a dashed ring with a body in it, the same dashed ring the street puts on a found body) once before the numbers that only happen if found: `Knock out · 7 s · (found) +4 · 1 · eye`. Takedown rows only; every other order is certain. If Sky finds it noisy, drop the glyph and the line is exactly Fable's.

Other states:
- **Hover chip** = the same line, led by the way's glyph (`way_knock`, `way_kill`, `s_carrying`), tail at the cursor, 0.6 s on hover and at once when armed.
- **Hover a row**: its preview is drawn in the street (the dashed lock on each door it would hold, a dashed ring on the target). No sentence.
- **Locked row**: the whole row at 45%, a `lock` for its glyph, its price still shown. The click gives the refusal pop; the long line is Alt.
- **Tinted cancel**: ✕ plain when free; when the cancel costs something it is a turquoise ✕ in a dashed ring (the one highlight colour). The 1-to-3-word cost is the hover tooltip (`less.cancel`).
- **Price before commit** holds everywhere: chip, row, run price (`› 12 s  » 6 s  eye ▲`), going-loud card.

## 3 · The world markers that replace text
All draw at play zoom (characters 40 to 60 px). Round markers are 22 px at 1080p, rims 2 px; they read at actual size (section 3 of the sheet shows each at 1x and 2x). At most two icons over a person. Signal colours mark their watcher: red, amber and violet on an eye or pip belong to a camera, guard or rival, and to cones. Other HUD marks are ink, cream, turquoise. (Signals no longer own their colours in the world: Sky, 2026-10-16.)

| marker | where | states and cues | replaces |
|---|---|---|---|
| **Eye mark** | over a watcher; over the target while an order is armed or hovered | open (almond and pupil), shut (lid and lashes), unknown (dashed rim, "?" eye). Rim = whose eye: amber guard or police, red camera, violet rival, cream for a civilian. Over the target: shut = from behind, open = he sees you | "Seen by: …", "From behind: …", `Seen.Seen` text |
| **Wake ring** | on a knocked-out body | 22 px disc with `body_out`, a ring that **empties** to the wake (120 s), a second ring in the last 15 s (motion); square plate `body_dead`, no ring (dead); small second disc `restrain` (tied); dashed outer ring (found: a report has landed) | "wakes 4:07 · left 0:54", "Knocked out", "Body lifted" |
| **Lock** | on a held door, once it happens | 24 px `lock` disc, a ring counting to the stand-down, a thin tape at the foot (kit `stripes_warn`, cropped to a band); hollow dashed lock = "would hold" preview; shakes on a refused Enter | "found → door held", "held ~1:09", "Police hold this door…" |
| **"!" and a dotted line** | over a witness walking to a phone | 22 px `bang` disc with a ring to the landing; a cream dotted line to a small `call` disc at the phone, dots travelling toward it (motion); a radio reporter has the disc and no line (nothing to stop) | "witness temperament → report", "reports in n s" |
| **Clock pip** | over a guard whose routine is learned | 18 px face, amber rim, a cream wedge = time left to his next check-in; dashes pulse inside 10 s. Shown when armed, hovered or on Alt | "Next check-in: 2:30. A missed one sends a robot." |
| **"?"** | over a watcher not yet watched | dashed rim in the watcher's signal colour, `p_unknown` inside | "Seen by: ?", "Blinds: ? until the Scout watches it" |
| **Cones** | as today | red camera, amber guard, violet rival, grey when looped, far edge dashed; unlit, outside the post grade. Learned only; Alt shows all | no change |
| **Objective marks** | the person or place that matters | inverted triangle over a person (turquoise, ink edge); a ground corona (`ring_ground_hint`) at a place; an edge arrow at the screen edge when it is off screen | the objective bar's place information |
| **Stash plate** | on a bin or service door, while carrying, on hover, on Alt | 18 px disc `stash` with pips (filled = a body inside); hollow dashed when behind a building; name on hover | "Dumpster: 1 free · …" |
| **Carry disc, unit badges** | over the carrier; where units stand | `s_carrying` disc; `signal_police` / `signal_guard` badges, merged within 2.5 m with a count | strip text, "2 officers · 3 robots" |

Everything stays hoverable for its Alt line. A marker that appears because something happened (lock, "!", found ring) comes in over 0.3 s so the player sees it arrive.

## 4 · The Alt layer
Hold Alt. It shows while held; nothing sticks.
- **In the world**: every learned mark at once (cones, eye marks, clock pips, wake rings, found rings, locks, stash plates, call boxes, unit badges split into singles); a name tag on every person the cell knows (a name or a role, two words, Jost 12; guards ringed amber); "?" on the rest.
- **On a card**: under each way, one line of icon-and-number fragments, at most five words: `units 16 s · door 1:10 · wakes 4:07 · report 22 s`. Fable owns the exact fragments; zeros are included.
- **On the pill and roster**: the Intel count, history key, health pips as numbers, every busy agent's seconds, the key legend (replaces the "17:24 · space pause…" line).
- **Long text is a second step**: with Alt held, hover a row and its old sentence (`does`, `line`) shows once beside it, as a tooltip. That is where today's text lives; it is never at rest.
- Budget: up to 30% of the screen, running text limited to fragments.

## 5 · The plot scope (minimap)
A core surface, as Sky asked: the persistent state moves off the screen and onto it, and it is the reason everything else can be small.

- **Look**: a plotting scope. The kit's dark enamel frame (`panel_dark`) round a glass of ink-deep, a faint grid, the block's footprint in dull teal, the two buildings in seafoam, the fountain and the bays. Period instrument, no map UI. A slow sweep arm refreshes the blips (cosmetic).
- **Size and corner**: 212 x 132 at 1080p, bottom left under the roster: **1.5% of the screen**. It counts in the HUD coverage; it is 0.15 of the 10% I would like to hold at rest.
- **Orientation**: rotated to the camera's fixed yaw, so up on the scope is up on the street. No compass needed.
- **What it shows, and only what the cell knows (told and learned)**:
  - the cell: four discs numbered 1 to 4, the selected one ringed turquoise;
  - **Vale**: a dashed ring round his last known place with a "?" inside; the ring **grows with the age** of the information. An exact disc with his badge only while an agent sees him. The briefing gives the Office as his place, so the ring starts large and tightens as the cell learns;
  - key people: Inez, the driver as kit badges once met; a guard as an amber dot once watched. A watcher not yet watched is not drawn;
  - learned cones (red camera, violet rival), grey when looped, the guard's cone in amber, and the patrol's route as a dotted amber line once learned;
  - held doors (`lock`), bodies (`body_out` / `body_dead`), stashes (a diamond), reports in transit (`bang` with its dotted line to the phone), units arriving (an amber arrow from the edge they come from, with their badge and count);
  - the main view as a white trapezoid; three level pips on the right (street, deck, sky concourse) and the scope draws the selected agent's level.
- **Edge pointers on the main view**: a round arrow at the screen edge for Vale (turquoise, his badge) and for anything urgent (units, amber, with a count). Present while the thing is off screen; they fade as it enters.
- **Interaction, recommended: click or drag pans the main camera; clicking a cell disc selects that agent (double click centres); right click does nothing.** No move orders from the scope: an order needs the street's price chip, the run price and the cones, and an order given on a map would hide all three and break "every order shows its full price first". It also never gives a position the cell does not know.
- **Accessibility**: every blip has a shape (disc, number, diamond, lock, arrow, ring) as well as a colour. The scope scales with the HUD scale setting.

## 6 · Lines in the street
**Goal line** (the objective and the Next Step hint share one place, one at a time; bottom centre; 6 s, then fades). Fable's text: one imperative, at most six words, one bracketed noun per marker. I draw: a dark plate, the words in cream, each bracketed noun in its **marker colour** plus a **tiny shape** (a triangle for a person, a ring for a place), so colour is never alone. The hint also carries the agent's portrait (pulsing, dashed ring) and the verb's glyph at the left: no agent is named.
- *Take ▼Vale alive.* · *Get ▼Vale to the ○car.* · *Watch the ○café terrace.* · *Reroute the ▼patrol.* · *Give the ▼rivals a false tip.*
- **Noun colours** (Fable asked): people the cell goes after (Vale, Inez, the driver), places (car, cabinet, café terrace, getaway car): turquoise-light `#60E0EA`, the one highlight; a guard, the patrol: amber `#F0A020`; rivals: violet `#8A4FD0`; cameras: red `#E03A2F`. On the HUD, these colours keep meaning their watcher.
- Reappears at the 5:30 chime and the 5:50 exit, and while the clock pill is hovered.

**Story beats** (Fable's nine lines): a dark plate at the lower right, 34 px high, a leading badge (`signal_rival`, `npc_waitress`, `npc_councillor`, `signal_camera`, `call`, `signal_police` / `signal_guard`, `lock`, `body_out`), at most two stacked, 4 s each, a third pushes the oldest out at once, a repeat within 10 s is dropped. History: the **L key** opens a left drawer (the old SCENES history, as a list: time, badge, line); an unread dot on the key. At rest nothing else of it shows.

**Speech bubbles**: a cream plate (`plate_light`), ink text, one line, a tail to the speaker's head, 3 s, two at once, no name tag. The rim says who: none for a civilian, amber for guards and police, violet for rivals, turquoise for the cell. The cell's own lines are bubbles over the agent (not a plate), and a speaker off screen has no bubble (the beat speaks for them). Cap: 10 words, two lines.

**Refusals**: one pop at the click, `chip_price` with the family glyph, at most four words, the number as a number, 2 s; a locked door's lock shakes. Families and glyphs: reach `s_moving`, "Watch first" `watch`, "Listen first" `listen`, wrong agent that agent's cameo, money `pay`, a window `p_time`, guns `fire`, bodies `s_carrying`, doors `lock` or `door_held`, otherwise a slashed circle (`mark_no`, the kit's one red display mark).

**The going-loud card without words**: `panel_dark_modal`, a row of glyph and number (Alert 3, police `16 s`, rivals `40 s`, the seal `8:30`, Trail `+2` per shot, per hit, per down), the allies lost as crossed badges, "captured" as crossed `restrain`, and the 0.5 s hold is a ring filling round the fire key. No pause. The sentences are Alt.

**End line**: one line and two buttons (Debrief, Replay): "Vale is in the car." The result, the tally, the security notes and the investigation are the debrief.

**Cut at rest**: strips, the NEXT STEP box and the objective bar as sentences, the Intel and Response pills (now icons), the SCENES stack, the verb captions, the ticker, "Right click to give an order", NEW tags, cancel cards, the going-loud feed card.

## 7 · The budget checker
`bun tools/hud-budget/cli.ts <shot.png | folder> [--state rest|active|alt] [--hud f.png | --world f.png | --rects f.json] [--manifest f.json] [--debug out.png] [--json]`
Exit 1 when any shot fails. I built it; Splice can adopt it into `bun run verify` (and add a `budget` script; I did not touch `package.json`).

**How it tells HUD from world** (best first):
1. `--hud`: an RGBA capture of the HUD layer alone (UI Toolkit drawn to a transparent RenderTexture, the same frame). Alpha at or above 24/255 is HUD. Exact. This is what I ask Latch for.
2. `--world`: the same frame captured with the HUD hidden; any pixel that differs is HUD (then closed by 1 px).
3. `--rects`: a list of screen rectangles (the UI tree's `worldBound`). Coarse (bounding boxes), used for the 013 shots.

**What it fails** (per shot, by state):
- HUD above **15% of the screen at rest** (22% with an order card open, 30% with Alt held); a warning above 10% at rest. The footprint (bounding boxes of the HUD's parts) is reported beside the pixel share.
- **Any line of running text that is not whitelisted**: text-height strokes inside the HUD mask are chained into runs and cut into words; a run of three or more words (about 14 letters) fails, and two or more stacked lines (4+ words) fail as a block. Icons, bars and rims are separated out (long straight runs, square tall stroke groups). Labels of one or two words and numbers pass.
- A **manifest** (`<shot>.manifest.json`: `{ "state": "rest", "elements": [{ "kind": "speech|beat|line|image", "x", "y", "w", "h", "text" }] }`, in screen pixels) whitelists what a screenshot cannot tell from a rule printed on a card: `speech` (a bubble: at most 2 lines, 10 words), `beat` (at most 2 on screen, 1 line, 6 words), `line` (the objective or the hint: 1 on screen, 6 words, 1 line), `image` (portraits and the scope: art with no text by construction, so its strokes are not read). Without a manifest every running line fails, which is the safe answer.

**Evidence** (`out/glyph014/before.json`, `after.json`; the same numbers are printed on the moments sheet):

| moment | before: HUD, text lines/blocks | after: HUD, text lines/blocks |
|---|---|---|
| 1 loop targets | 27.9%, 19 / 14 | 9.3%, 0 / 0 |
| 2 Watch | 14.1%, 6 / 3 | 5.4%, 0 / 0 |
| 3 knockout price | 19.8%, 12 / 5 | 6.0%, 0 / 0 |
| 4 takedown card | 34.2%, 28 / 9 | 8.8%, 0 / 0 |
| 5 takedown | 18.1%, 8 / 5 | 5.1%, 0 / 0 |
| 6 body | 13.6%, 4 / 3 | 4.9%, 0 / 0 |
| 7 carry | 15.5%, 4 / 2 | 5.6%, 0 / 0 |
| 8 hidden | 14.2%, 4 / 2 | 5.7%, 0 / 0 |
| 9 end card | 18.4%, 4 / 5 | 7.9%, 0 / 0 |
| 10 held door | 14.6%, 5 / 2 | 5.8%, 0 / 0 |
| 11 enter refused | 15.0%, 5 / 2 | 5.5%, 0 / 0 |
| 12 Alt (new) | | 10.8%, 0 / 0 |

Before: 0 of 11 within budget (rectangles overcount a little; every one fails on text). After: 12 of 12.

**Limits, honestly**: it is a heuristic over pixels, not OCR. Word counts come from gaps and widths, so it reads a three-word label as two sometimes and a long number string as text never; it was tuned on Jost and Barlow at 13 to 15 px and 24 px titles on a 1726 x 1142 capture (it scales with height). A glyph that is wide, short and not enclosed (a bare eye) reads as a word, which is why the eye sits in a round badge. When a new HUD element trips it, fix the element (a gap, a badge) before touching the threshold; tests in `tools/hud-budget/budget.test.ts` (18, `bun test tools/hud-budget`) pin the verdicts on fixtures: the 013 card and carry shots fail, the 014 redesign passes.

Files: `tools/hud-budget/{png,budget,cli}.ts`, `budget.test.ts`, `fixtures/`. `bun run typecheck` clean.

## 8 · Answers to Fable's asks
| ask | answer |
|---|---|
| eye mark, three states | `eye_open` / `eye_shut` / `eye_unk` (new), round badge, rim = the watcher's signal colour |
| ▲ Alert | `p_alert` (existing siren); Trail `p_trail`; time `p_time`; cash `p_cash` |
| pace | `s_carrying` + `1.4 m/s` |
| learns | `routine` (new: a clock with a loop), `cone` (new: a wedge), plus the existing need and temperament glyphs |
| clock pip | section 3 |
| speech bubble | section 6 |
| tinted ✕ | section 2 |
| locked row dim | section 2 |
| 17 pop icons | families in section 6; the table in `fable-less.md` §4 maps each; I drew ten as examples |
| marker colour per noun | section 6 |
| `[Vale]`, `[car]` in the objective | triangle for people, ring for places; turquoise |

## 9 · Where I differ from Fable, and what Sky should decide
1. **Found glyph in takedown prices** (section 2). Mine adds one symbol to say "if found". Drop it and nothing else changes.
2. **Eye "none"**: Fable writes `👁 none`; the shut-eye badge is the same fact. The line has no word.
3. **Where beats sit**: lower right, as the old feed was, so the eye does not move. The goal line is bottom centre (GTA's place). If Sky prefers the beats to rise from the bottom centre too, it is one anchor.
4. **Next mark stays in the pill** (Fable's call); I had proposed hiding it until 60 s out. It costs nothing and it is the orchestration pillar.
5. **Minimap interaction**: pan and select only. If Sky wants move orders from the scope, the price chip must follow the pointer to it; I advise against.
6. **Not drawn** (the kit has them, nothing in the 011 or 013 spec changes): the fight HUD (tracers, aim chip `down 5.5 s · ¾ · +2/hit`, stance badges on the portrait, the 13 m ring), health pips and downed state on the roster, the Intel panel and history drawer list. They follow the same rules: icon, number, ring.

## 10 · Handoff to Latch (C)
Inputs: this file, `fable-less.md`, the two sheets, `glyph/after/*`.
- Build the pieces in sections 1 to 6 on the existing kit sprites; import the 11 new glyphs as atlas singles; the marker sizes and colours are on the sheet.
- **For the checker, give me two things per capture**: the HUD layer alone as `NN.hud.png` (RGBA, transparent where there is no HUD; one flag that hides the world and renders the UI to a transparent target), and `NN.manifest.json` (a list of the Toolkit elements with their `worldBound` and kind; bubbles are `speech`, story beats `beat`, the goal line `line`, portraits and the scope `image`).
- Take the same eleven moments into `briefs/014/play/` plus the Alt layer; run `bun tools/hud-budget/cli.ts briefs/014/play` (needs `NN.hud.png` and `NN.manifest.json` beside each shot, and the state in the manifest or the file name).
- Done-check (mine): every capture passes; no panel shows a sentence; the scope shows only what the engine says the cell knows; replays unchanged.

Next owner: Latch (build), Splice (verify hook, optional), Sky (section 9).

## Eyes, revised (Sky, 2026-10-16: "what's the question mark")
The old unknown badge (a "?" in a ring-like disc) read as a help button at chip size and lost the eye. Redrawn: `eye_open`, `eye_shut`, `eye_unk` in `briefs/014/glyph/glyphs/` (@1x 32, @2x 64, white monoline; script `briefs/014/glyph/scripts/eyes.py`; sheet `briefs/014/glyph/eyes.png`, script `eyes_sheet.py`). The eye shape spans at least 28 of the 32 box in all three.

| State | Shape | Colour in code | Hover line |
|---|---|---|---|
| open | wide almond, solid iris, three rays over the lid | red (small display pictogram; unchanged) | "Seen by <watcher>." |
| shut | the lid line only, curved down, with three lashes | cream | "Unseen." |
| unknown | the same almond, an empty iris ring, a small "?" tucked in the top-right corner outside the almond | cream | "Unknown. Watch first." |

Use rules: draw the glyph at 18 px at 1x (14 px minimum). Drop the chrome disc (`s-disc_dark_24`) and its rim: a rimmed disc reads as a button, which was the "help" look. Either the bare glyph on the chip or, if the hud-budget wants it enclosed, a flat ink-deep disc 24 px with no rim and no ring. The eye is never inside a "?" disc and never next to the hint "?" disc. The hover line is one line, one sentence: "Unknown. Watch first." / "Seen by Guard." (the watcher's name or role, as today) / "Unseen." No colour carries the state alone: shape does.

## Move preview: the ground answers (Sky, 2026-10-16: the chip everywhere the mouse goes is annoying)
Sheet: `briefs/014/glyph/move-preview.png` (two variants, five states each, on the real cafe-after play shot, rings close up at the bottom; script `briefs/014/glyph/scripts/move_preview.py`; references and what is unverified in `briefs/014/research/move-preview.md`). Everything over the street in the sheet is drawn by the script, the build's own Alt chip is the one real piece. No Unity edits.

**Recommendation: A, "the ring", with the exposed-route glow and without the cone pulse.** One ring is the cursor on the ground: Sky's colour-coded ring with a symbol, plus the walk time as a clock fill. It replaces the chip for the whole walk, so the screen stays bare while the mouse moves. B is kept on the sheet as the comparison: it spends the same information on more screen (a pulsing cone, a pip, a street pulse), which is right for the Scout's first look and wrong for every mouse move.

### A · the ring
Geometry: a ground ellipse at the cursor, 64 x 39 px at play zoom (about 1.6 m across, the ground circle at the 38 degree pitch, ratio 0.616). Rim 4.6 px, an ink under-stroke 8 px so it holds on pale stone, a dark inner disc, the symbol an upright 25 px glyph (billboard, not flattened). The clock: the rim fills clockwise from 12 o'clock with the walk time, a full ring = 60 s; the unfilled rim stays as a faint track. No digits, no words anywhere.

| State | Ring | Symbol | Why this colour |
|---|---|---|---|
| Safe (nothing sees the walk, nothing unknown) | turquoise-light `#60e0ea`, solid | `s_moving` footprints | the cell's own colour ("go"); dE2000 46 or more from every signal |
| Seen | the seeing watcher's own signal colour (camera red, guard amber, rival violet), solid | `eye_open` | it is that watcher seeing you, so the ring is the cone's colour and the eye is the link; a signal colour here is a use, not a new meaning |
| Unknown (a watcher on the route has not been watched) | cream `#f7f4ec`, dashed track | `eye_unk` | dashed is the kit's "not known yet"; cream is 30 dE or more from amber |
| Raises Alert (a run, a found fault) | ink `#12302e` rim with a second cream rim outside it | `p_alert` lamp | the one dark ring on a pale street, 36 dE or more from every signal; it needs no new hue |

Rules:
- Colour never carries a state alone: eye, dashed rim, second rim and lamp each differ in shape.
- If two watchers would see the walk, the ring takes the more serious colour (camera, then guard, then rival); each watcher's stretch of the route glows in its own colour, so both show.
- Alert wins the symbol slot over the eye (the lamp), and the ring keeps the double rim; the route glow still shows who sees.
- **Seen: keep the route glow, drop the cone pulse.** The glow answers "where on my walk", which the ring cannot; the learned cones are already on the street, so a pulse adds motion on every mouse move and nothing new. The glow is a soft band of the watcher's colour under the route dots, only on the stretch inside the cone, pulsing once a second at most.
- **Unknown:** the unwatched watcher carries a small "?" disc over his head while the preview is up (A), faint; the Scout's Watch is the way to clear it. B pulses two dashed rings at his feet as well.
- **Alt held:** the numbers as today (walk, run, eye, Alert) in the existing chip, placed beside the ring, tail to the ring; Alt released, the chip goes.
- Route: dotted, cream with an ink dot under it; a run is drawn with tighter dots. The route and glow appear after the cursor rests 0.25 s; the ring is immediate.
- Not reachable ground: no ring, the cursor stays the cursor.

### B · the ground answers (the comparison)
A plain cream ring with a turquoise clock fill and nothing inside. Seen: the cone brightens and echoes its edge, the exposed stretch glows. Unknown: a small "?" pip on the ring's upper right, a faint "?" pulse on the unwatched watcher. Alert: a lamp pip on the ring's upper right. Alt: the chip.

### Checks
- Reserved colours: turquoise-light 46 dE or more, cream 30 dE or more, ink 36 dE or more from cone, guard and rival (CIEDE2000, my calculation; the signals are the contract's `#e03a2f`, `#f0a020`, `#8a4fd0`). Seen uses the signals themselves, on purpose.
- Budget: one ring and at most one glow, no text, nothing a reader would count as a word. Latch should run `tools/hud-budget` on a real capture.
- Glyphs: all exist already (`s_moving`, `eye_open`, `eye_unk`, `p_alert`, `run`); no new art.
- Build note for Latch: ring as a ground decal (an arc shader for the clock fill, an ellipse mask), the symbol a world-space billboard; the glow a soft ribbon along the route polyline clipped to each learned cone.
- Against the references (search notes in the research file): Desperados III shows a cone on demand and zones inside it, Invisible Inc. marks the watched tiles, Shadow Tactics holds a key for the cones; none puts a sentence in the move preview. Ours adds the single ring and the watcher's colour.

## Intel panel, redesigned (Glyph, 2026-10-18; Sky: "this needs work")
Mock: `briefs/016/intel/intel.png` (before, after at 2x, three hover states), `briefs/016/intel/intel-on-shot.png` (on a real play shot; `intel-on-shot-2x.png` close up). Script: `briefs/014/glyph/scripts/intel.py`. No new art: it uses the existing `npc_*` badges, the need, temperament and `secret` glyphs, `listen`, `watch`, the cameo portraits, `chev_up` and `panel_dark`.

**The problem it fixes.** Names sat against the screws, name and role were spread across an empty panel, and the three icons (need with a dotted ring, a "?" disc, a lone ear or binoculars) had no readable meaning.

### Layout (all px at 1080p, sized to content)
- **Panel:** `panel_dark`, 280 wide, anchored top right as now (right 14, top 108). Height = 40 header + 62 per card + 6 between cards + 26 bottom. Three people is 264 px. Padding: 26 px each side, so content clears the corner screws (the screws end about 20 px in).
- **Header:** "INTEL" (Jost 11.5 bold caps, `#bee0d8`) at x 28, and a `chev_up` fold at the right. No subtitle.
- **Card:** a plate 228 x 62, radius 9, fill `#164240`, 1 px rim `#6eafa8` at 43% opacity. The first card (the person the plan needs next) is a little lighter (`#20585a`) and has a 3.4 px turquoise bar down its left edge. That is the only "priority" cue.
- **Inside a card** (offsets from the card's top left):
  - role badge: the `npc_*` badge at 36 px, centre (26, 31);
  - line 1: name (Jost 13.5 bold, cream) with a baseline at y 21 and x 52, then " ·  role" (Barlow 12, `#96c0ba`) on the same line;
  - line 2: the three fact slots and the chip, centred on y 44.
- **Slots**, left to right at centres x 64, 94, 127; each 24 px high:
  1. **need**, a circle (`debt`, `cash`, `housing`, ...);
  2. **temperament**, a rounded square (`idealist`, `mercenary`, `fearful`, `loyal`);
  3. **ties or secret**, a landscape tab 30 x 24 (`secret`, `kin`).
  - The frame shape is the label, and the order is the same on every card.
- **Slot states** (shape and rim, never colour alone):
  - learned: ink fill `#0a2624`, solid cream rim 1.3 px, cream glyph 15 px;
  - told: the same with a dashed rim, glyph at 80%;
  - unknown: dashed rim, a quiet bold "?" (Jost 12.5, 70% cream), no glyph.
  - Never use the old dotted-ring "?" disc.
- **Turned:** the badge gets a 2 px turquoise ring and a `mark_ok` at its lower right, and the chip is replaced by nothing (all done). The slots stay.
- **Next-step chip**, right-aligned (10 px from the card's right), 56 x 26, radius 13:
  - content: the acting agent's cameo (18 px, ring) and the verb glyph (15 px: `listen` or `watch`);
  - turquoise rim and glyph: the right agent is selected, a click gives the order;
  - cream rim: a click selects the right agent (and a second click gives the order);
  - no text on the chip.

### Hover (one fragment, one line)
Tooltip: a dark chip 24 px tall, cream 1.3 px rim, radius 8, glyph 13 px then Jost 12 bold, under the slot, drawn above everything. The slot gets a turquoise ring (+2.4 px).
- **learned slot:** its fact as a fragment, two words at most ("Debt", "Cash", "For hire", "Loyal", "Her guard"), from the cast data. No sentence.
- **told slot:** the same fragment; the dashed rim already says it was told, so no extra word.
- **unknown slot:** how to learn it, as the verb glyph and one word: "Listen" (temperament, need, ties by talk) or "Watch" (routine, who they meet). The verb comes from the rulebook's learn source for that slot.
- **chip:** the verb word ("Listen" or "Watch").

### Interaction
- Click the chip: the right agent selected, order given (as the action panel does), else it selects that agent. Click a slot: the same as its row's chip. Click the badge or name: centre the camera on the person if known on the map.
- Hit areas: slots and chip at least 28 x 28 (pad the 24 px frames).
- Keyboard: Tab walks the cards, Enter does the chip.

### Sort
By relevance to the plan, from the mission's order list (the person the next plan mark depends on first), then people with an unknown slot a cheap verb can fill, then the rest. The order is recomputed when a fact is learned, not every frame, so rows do not jump under the cursor.

### Budget and checks
- Words on screen: the three names and roles per card and the header; no sentences. Facts are glyphs. Run `tools/hud-budget` on a real capture.
- Signal colours: none used. Turquoise is the cell's highlight colour.
- Max four cards (the known people); a fifth scrolls inside the panel, never grows it past 360 px.
