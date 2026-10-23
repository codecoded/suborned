# 049 · Visual cues instead of range circles and labels

**State:** design and label audit done 2026-10-22 (Glyph); round 2 (target rings: red when they aim or fire at us, ticks only in a shot exchange) done 2026-10-22, below; variant A is the pick, B is retired (its ticks now mean "in an exchange"). Round 1 pictures moved to `briefs/049/round1/`. Owner: Glyph. Next owner: Latch (build), then Keel/Motive only if a rule number is wanted on the ring (none needed).

## Why
Sky, playing 0.2.65: "what's with all those large circles / lines - looks like we've copied from the fight lab. I know it's meant to be range - but looks terrible, we can just use ring marker on the NPCs to indicate range or not. Way too many labels going on explaining everything when we've not exhausted visual cues."

## 1. What draws what (read only; Latch is editing)
| Mark | Drawn by | Where it shows | Verdict |
|---|---|---|---|
| Big cyan circle, 15 m (the pistol's effective ring), 2 px, round every selected agent with a drawn weapon | `HandoverHost.cs:274` calls `HostViews.ShowRange` (`HostViews.cs:104`, a 64-point `LineRenderer`, radius `Gun.W.EffCm`) | **Handover and range**, every frame | Remove from the Handover. The one thing Sky saw. At play zoom it is 580 px across the screen's width |
| Black concentric floor rings (8 / 15 / 18 m) | the FightRange scene's floor | range only | Keep, range only |
| White/cyan lines agent to every target, each with a sentence ("75% · 14 m · hits next shot · new target: aims first", "out of range · 22 m · holstered · untrained") | `RangePanel.cs` (`Update` lines, `OnGUI` `Tag` labels; the words are `FireLine.Text`, `sim/Suborned.Sim/GameRange.cs:252-273`) | **range only**: the component is on `FightRange.unity` and nowhere in `HarbourDistrict`; gated by `h.rangeMode`; its `numbers` toggle starts **on** | Handover never draws it (checked: no `RangePanel` in the Handover scene or builder). Range: keep behind "Numbers", start it **off**, and put the cover labels ("low wall", "pillar", "box"; drawn even with Numbers off, `RangePanel.cs:~97`) behind it too |
| Dashed 8 m "follow ring" round the driven agent while aiming (third person) | `HudToolkit.Fire.cs` `Reticle()` | Handover, while aiming | Remove; the followers already wear their own 0.6 m ring |
| Watch reach ring round the hovered target | `HudToolkit.Hold.cs:114` | Handover, only on hover of a Watch | Leave; it is a place-to-stand, not a range read. Revisit after Sky plays |
| Tracer: three passes (ink 7.5 px, turquoise 7.5 px, cream 3.2 px) dashes marching | `HudToolkit.Fire.cs` `Tracers()` | Handover while firing | Thin to one quiet line (below) |
| Lab preset only: queued ghost lines, kite ghosts, hostile bars | `HostFightLab.cs` | only with `Game.Lab` (not the default) | Leave |

So the circle and the lab-style tracer are in the Handover; the lines and the sentences are the range's own. Whatever Sky played, the Handover must not draw any of it.

## 2. The replacement: a ring on the target, nothing else until the shot
Pictures (lab scene "Range cues 049", `bun run hud-style`, `#s=range-before|range-a|range-b|range-states`; no new tool, files added beside the lab: `testbed/hud-style/ranges.js`, `ranges.css`, `bg/figure-patrol.png`, three scene lines in `scenes.js`):
- `briefs/049/board-before-after.png` (before / after at play zoom, full frame and 1:1 crop)
- `briefs/049/states.png` (every ring state over pavement and asphalt, 2x)
- `briefs/049/after-a.png`, `after-b.png`, `before.png` (1080p), `crop2x-a.png`, `crop2x-b.png`
Figures are stand-ins (cut-outs of the city guard and a patrol officer from the 0.2.56 frames, mirrored or tinted); the ring sits under the figure, behind the legs.

**Rule.** When a selected agent has its weapon drawn (or a fight is on), anyone it could fire on and who is a combatant (police, response, guard, patrol, rivals) wears a ground ring. A civilian, a car or a camera wears one only while the cursor is on it (so the street does not fill with rings). No ring, no circle and no line otherwise.

**Shape says reach, colour says whose.**
| State | Drawing (width in metres on the ground, flattened like the cell's own ring) | When |
|---|---|---|
| Effective | solid ring r 0.54 m, 0.10 m line, ink-deep edge, four cream ticks (crosshair band ticks, 0.22 m out) | a selected agent is inside the weapon's effective ring and has a line (`FireLine.Band` point / eff) |
| Long | same ring, 0.075 m line, **no ticks** | inside the max ring only (can hit, poorly; `Band` long) |
| Out | **dashed** ring, 0.065 m line, 90 % alpha, ink edge 42 % | beyond the max ring |
| Under the cursor | r 0.68 m, 0.12 m line, ticks 0.28 m out (the pill already prices it) | the hovered target, in reach |
Colour = side, all sourced: guard and police amber #f0a020, rivals violet #8a4fd0 (style contract `signal_colours`), a civilian cream #f7f4ec; the cell stays cyan #60e0ea (selected 0.11 m, not selected 0.045 m, as `CellRings`). Drawn unlit on the signal layer like the rival ring, outside the post grade. Nothing needs a tick of text: the pill already carries the price and the shooters' faces.

**Fire line, if kept.** Only while a shot is being fired: one 1.5 px cream dashed line (9 on, 5 off, marching) over a 3.4 px ink-deep underlay at 35 %; driven agent 1.5 px, an agent returning fire 1 px; no glow, no turquoise pass. Hostile fire keeps its short solid streak. No text, ever. Nothing is drawn before the shot (the aim is the pill and the ring).

**Variants for Sky.**
- **A (recommended):** the four ticks are a fixed crosshair: "this one is in reach".
- **B:** the four ticks are the cell: one lit per agent that can hit it now (two lit, two dim in `after-b.png`). Says who, but it has to be learned and the pill's faces already say who. Pick B only if Sky wants it on the ground.

**For Latch.** (1) Stop calling `ShowRange` outside the range scene (`HandoverHost.cs:274`), and drop the 8 m follow ring. (2) One `LineRenderer` ring per combatant, widths as above in world metres, dashed by a tiled material for Out. (3) Replace the three-pass tracer by the one line. (4) `RangePanel`: `numbers` default off; cover labels inside it. No sim change: `FireLine.Band` / `Game.FireQuery` already give the three states.

## 3. Label audit (every word in play, Handover unless noted)
Sources: `contracts/missions/handover/text.json` (`less.*`, `hints.items`, `actions`), `HudToolkit*.cs`, `HostHoldStatus.cs`, `HudModel.Less.cs`, briefs 041 wording and 043/046. Verdicts: **keep** (nothing visual can carry it), **replace** (named cue), **remove**. I did not play the build; where I name a cue that does not exist yet it is a kit icon that does (`icons/singles`) unless marked new.

| # | Surface | Words today | Verdict |
|---|---|---|---|
| 1 | Verb pill, offer | `Hold · Watch the guard · 26 s` | **keep** the verb and the price line (the rule). Optional later: the kit's left/right mouse glyph for `Hold ·` / `Right hold ·`; the sweep already says hold |
| 2 | Pill price fields | `$40k`, `64 s`, `+1 Trail`, `Alert ≥2`, eye mark | **keep** (numbers with icons) |
| 3 | Pill, fire offer | `Right hold · Fire at an officer · +2/hit Trail · Alert ≥2` | **keep** verb and price; **remove** the noun ("an officer") when the target wears its ring and the tail points at it; keep it for a civilian (the warning) |
| 4 | Pill, firing | `Firing at an officer · down in 2 s`, `Aiming · 1 s`, `Drawing`, `Stopped`, `Officer down` | **replace**: gun disc + the number (`2 s`, with the kit `s_downed` icon for "down in"); the thin fire line and the roster plate say firing; "down" = the body and the ring gone; `Stopped` fades as the line does |
| 5 | Pill, blocked | `Too far`, `No clear line` | **replace on hover**: the dashed ring already says too far; no word until the press is refused, then one pop (row 24) |
| 6 | Pill head word | `Hold ·`, `Let go ·` | `Let go` **keep** (the only moment word); `Hold ·` see row 1 |
| 7 | Pill, `Watched spot` plate | `Watched spot` | **replace**: the stripe block and the `sawyou` icon without the words |
| 8 | Pill, walk-in plate | `to Inez · Listen · 14 s`, `→ Inez` | **keep** the verb and time; **replace** "to Inez" by the target's face badge (people discs already exist) |
| 9 | Roster plate beside a portrait | `9 s`, `aiming`, `firing`, `drawing`, `queued`, `too far`, `no line`, `under fire`, `done`, `turned`, `not yet`, `refused`, `reported`, `cancelled` | **keep** the number; **replace** every word: weapon slot (drawn / ray), the `mark_ok / half / no` badge for outcomes, `s_*` status icons; brief 046's "icons under the portrait" carries them |
| 10 | `Under fire` plate | `Under fire` | **remove**: the pulse ring on the portrait, the cause disc and the ground arrow (046) |
| 11 | Roster `Squad` plate | `Squad` | **replace** with the four-dot group glyph |
| 12 | Weapons-free toggle and tooltip | `Weapons free`, `Return fire only`, `Weapons free (G)` | **replace** with the stance icons (gun, gun with return arrow, lock); the one-word pop on change stays (row 24); `(G)` is a key legend: **remove** |
| 13 | Clock / status pill | `1:38`, `3:10`, `$100,000`, Trail, Alert pips | **keep** (numbers); `Next · ?` tooltip Alt only |
| 14 | Goal line | `Take [Vale] alive.` | **keep** (the one sentence the rules allow; nouns in marker colours) |
| 15 | Hint line (H1-H19) | `Turn [x]: right click.`, `Call [x]: right click.`, `Run to the [mark]: press G.`, `Turn the view: Q E`, `Fire at [x]: right click.` | **remove the key legends** from H3, H5, H12, H13, H17 (a legend means the controls are wrong); where the noun is ringed in the street the line adds nothing: **replace** with the pulse on that noun and the cursor glyph at first use. Keep a hint only where the street cannot say it (H7 Reroute the patrol, H8, H10, H18) |
| 16 | Story beats, lower right | `Listen · +4 0:55`, `Inez: Yes. Yes. And thank you.` | speech **keep**; city beats ("All quiet at the door.", "Guard back") **replace** by the icon pop `eye_open` + noun where one exists (043 has it) |
| 17 | Speech bubbles | spoken lines | **keep** (speech is the exception) |
| 18 | Done chip facts | `Rounds`, `Cone`, `Nothing new`, `Contact`, `Guard away`, `Camera blind`, `Patrol away`, `Aboard`, `Tied`, `Hidden`, `Wakes {s} s` | **replace** with learned icons (`routine`, `cone`, `eye_shut`, `s_carrying`, `restrain`, `stash`) + the outcome mark; keep a word only for `Wakes {s} s` (a number) and `Dead` |
| 19 | Cancel tag and pop | `Cancels Watch · 4 s lost`, `Watch cancelled · 5 s lost` | **replace** the verb by its icon + `close_x`; keep the number (`4 s`); `They remember` / `They report` as icon (`talker`) |
| 20 | Window marker | icon + seconds | **keep** (already no words) |
| 21 | Intel card | `INTEL`, name, role, `Loves {name}`, `A lover`, `A record`, `No secret`, `Leans on their secret`, `SUGGESTED` | name **keep**; secrets and needs **replace** with the kit icons (`secret`, `kin`, `secret_none`, `debt`); drop the section labels `INTEL`, `SUGGESTED` |
| 22 | Unknown person chip | `Unknown. Watch first.` | **replace** with `p_unknown` + `watch` icons |
| 23 | Alt layer | outcome words, split times | **keep** (it is the opt-in reading mode) |
| 24 | Refusal pops | `No way`, `No room`, `Unarmed`, `Not drawn`, `Nothing there`, `Never Vale`, `Select an agent`, `No clear line`, `Too far`, `Fight blocks car`, `Camera dark`, `Camera hit`, `Hold fire`, `Return fire`, `Fire at will`, `Passer-by`, `Hints on/off`, `Take control: T`, `Let go: T` | **keep** `No way`, `No room`, `Never Vale`, `Select an agent` (the reason has no icon); **replace** `Unarmed` / `Not drawn` (struck gun), `Too far` (dashed ring glyph + metres), `No clear line` (gun, blocked line), `Camera dark` / `Camera hit` (camera + `eye_shut`), the stance pops (stance icons), `Hints on/off` (hint icon on/off), `Fight blocks car` (`car` + `fire`); **remove** the keys in `Take control: T` / `Let go: T` |
| 25 | Camera and squad plates | `Camera follows` / `Camera free`, `Paused · orders queue`, `Started · 3 orders`, `Follow view` | **replace** with icon + number (camera lock, `pause`, `3`); `Follow view` **remove** (test mode) |
| 26 | Attention clock over an agent | `seen`, `noticed`, `wary`, `reported` | **replace**: the clock ring fill and the `sawyou` / `eye_open` icon say the stage; words on Alt only |
| 27 | Response pill | icons + counts + `{n} s` | **keep** |
| 28 | Going-loud card | `Going loud`, `The first shot cannot be undone.` | **keep** the title; **remove** the sentence (the hold ring and `bang` icon say it) |
| 29 | Briefing card | `MISSION BRIEFING`, `Time stops until you begin. Space pauses at any time.`, `turn the view.`, `Begin` | **keep** `Begin` and the goal line; **remove** the title, the two sentences and the key legend |
| 30 | Pause menu | `Paused`, twelve settings rows (`Controls:`, `Aim:`, `Right-click:` ...) | **keep** `Paused` and the rows a player sets; hide the lab rows (aim, action plate, right-click, map fire) from the player build |
| 31 | End card | tally (`m:ss`, `$`, `+Trail`), `Vale is in the car.`, standing band + arrow, buttons | **keep** (the result, once); the arrow carries Standing |
| 32 | Fail cam | caption `The guard saw Scout.`, tip `The guard watches the door.` | caption **keep** (once, says why); tip **remove** from play (the orbit lands on the cone; the debrief keeps it) |
| 33 | History (L) and debrief | full sentences | **keep** (opt-in reading) |
| 34 | People names over discs / edge pointers | `Inez`, `Vale`, `Guard` | **replace** with the face badge; the name on hover or Alt |
| 35 | Hostile bars (lab preset) | segments, no words | **keep** |
| 36 | Fight range only | the 40+ words per line, cover labels, spawn panel | **keep behind Numbers**, off by default; never in the Handover |
| 37 | Debug (F1) | engine, tick, log | **keep**, dev only |

Counts: 37 rows; words kept as words: price lines, goal line, speech, `Let go`, `Begin`, `Paused`, result and fail caption, a handful of refusals, the Alt/history/debrief layers. Everything else has a cue named or goes.

## Open
- **Sky:** A or B? Rings on civilians only under the cursor: right?
- I did not run the game; the board reproduces the old circle and the range's lines and words in the lab to compare, and its figures are stand-ins. The ring needs one look in the build over real figures (Glyph reviews the crop at 2x when Latch has it).
- Row 15 and row 24 change `text.json`: Fable. Row 29-30 change layout: Latch. Rows 9-12, 18-19, 26 use the 046 slots: Latch with the lab export.
- Range's cover labels and the Numbers default are Latch's change in `RangePanel.cs`.

## Next owner
Latch (build, from section 2 and the table); Fable (rows 15, 24, 28, 29 wording); Sky picks.

## Round 2 (Glyph, 2026-10-22): target rings, red when they aim or fire at us, crosshairs when in an exchange
Sky: "Think rings should be red when they're firing at you, and with crosshairs also when involved at being aimed or shooting. Otherwise they seem worth trying."

**Rule.** Same rings as section 2 (combatants only; a civilian, car or camera only under the cursor), but the ticks no longer mean "in reach". Priority fire > aim > ours > hover > in > long > out.
| State | Drawing (m on the ground) | Data |
|---|---|---|
| In / Long / Out / Hover | side colour (guard amber, rivals violet, civilian cream), solid 0.10 / solid 0.075 / dashed 0.065 / r 0.68 and 0.12; **no ticks** | `FireLine.Band`, cursor |
| Ours | the ring above, side colour kept, **four cream ticks**, still | the cell is aiming or firing at it |
| Aim (it aims at us) | **red** #e03a2f (the cones' red), line 0.12, solid even if out of reach, four cream ticks that **close in**: start 0.30 m further out at alpha 0.5, settle by 78 % of a 1 s loop | `FireWhyOf` / under-fire read names a target, no shot yet |
| Fire (it fires at us) | red as above, ticks tight, they **kick** 0.07 m in at each shot and one thin red ring leaves (scale 1 to 1.8, 300 ms, alpha 0.9 to 0) | a shot in the last 1 s |
A duel is red (the threat wins; our own aim is still the pill and the fire line). Colours are all sourced: the contract's cone red is the only new use; ticks stay cream (red ticks on a red ring were thin on asphalt: tried, dropped).

**Not a cone.** Same hue, told apart by shape and motion (GDD §10): a cone is a flat wedge metres long, fill 38 %, rim 0.085 m, shown only when learned or on Alt, no ticks, slow sweep. The red ring is a closed outline 1.1 m across at the feet, never filled, with four ticks that close in and kick. Board tile "A cone, for comparison".

**Under-fire arc (046): keep it, as the quiet second cue.** The red ring says who is shooting; it does not say whom. With two or more agents selected the arc on the agent (with the portrait's pulse ring and the cause disc) is the pairing, and it is the only cue when the shooter's ring is hidden or off screen. I tried to make it redundant (a crosshair turned to face its target): a four-tick crosshair is four-fold symmetric, so it cannot point; dropped. Finding: at the cell's real ring (r 0.42 m, 16 px) the arc is a 1.6 px hairline, behind the legs on a diagonal and weak on pale paving (`arc.png`); it never was loud. Weight 1.6 to 2 reads better (one number, `why.arrow.weight`); I left the default at 1 for Sky to judge in the build.

**Pictures** (`briefs/049/`, git-ignored): `board-target-rings.png` (the keeper: plain and aimed-at rings, red frames and a cone, play-zoom crops with and without arcs), `after-a.png` / `after-noarc.png` (1080p), `crop2x-a.png` / `crop2x-noarc.png`, `states.png` (plain and ours, pavement and asphalt), `red.png` (aim and fire frames frozen at 0, 150, 400, 900 ms, and a cone), `occlusion-4x.png` (ring behind the legs: back half hidden, front half over the ground), `arc.png` (arc at weight 1 and 2). Lab scenes: `range-a`, `range-noarc`, `range-states`, `range-red`, `range-arc` (`bun run hud-style`, `#p=revisedA&s=<scene>&z=1&bare=1`); the loop runs live in the lab, the boards freeze it. Files: `testbed/hud-style/{ranges.js, ranges.css, scenes.js, tokens.js (TARGET_RINGS), app.js (export), check.ts}`.
**Tokens export:** `out.targetRings` (schema `suborned.hud.targetrings/1`: colours with sources, the seven states, tick spec, motion, priority, notACone, underFireArc) in every `testbed/hud-style/export/*.tokens.json`, `hud.tokens.json` = Revised A. `bun run hud-style:check`: ALL OK, layout problems 0, new check per preset.

**For Latch (build).** One state per target from `FireWhyOf` (hostile aims or fired in the last 1 s) and the cell's aim phase; same `LineRenderer` ring, red by swapping the colour, four tick segments as children (not rotated), ticks and pulse animated by the loops in the tokens; ground decal under the figure as before. Nothing in sim, Unity or tools was edited.
**Open.** Stand-in figures, not the build; one look in the build at 2x. Sky: arc weight 1 or 1.6? Does a rival aiming at us wearing the cones' red (not violet) read right (I think yes: the threat beats the side)?
