# 085: the capture clip, the live inset, the Pressure price

Glyph to Latch, 2026-10-31. Spec only. Numbers are panel px (`HudToolkit` scale k = round(screen height / 1080), so 1080p is 1920 x 1080 panel and the Air's native 2880 x 1864 is 1440 x 932). Everything below is view only and never reaches the engine.

Pictures here: `capture-clip-mock-1080.png`, `capture-clip-mock-crop.png` (kit parts composited on `office-front-yaw210`, not a game capture), `live-inset-crops-2x.png` (crops from existing proof shots).

## 1. The capture clip

**Family:** the clip is a wide version of the live inset (section 2), not a panel. Same ring, same corner cameo, same chevron, same click. I tried it in `panel_dark` first; it worked but read as one more HUD panel beside the scope, with no link to the insets Sky likes.

### Place and size
| | 1920 x 1080 panel | 1440 x 932 panel (Air native) |
|---|---|---|
| Picture | x 20, y 732, 320 x 180 | x 20, y 584, 320 x 180 |
| Share of the panel, with word plate | about 3 % | about 5 % |

- **Anchor:** left 20, picture bottom = scope top − 22. It hangs off the scope, so it holds at any size. Nothing else lives in the band between the Squad button (bottom 468) and the scope (top 934, or 786 on the Air); Sky's own shot has the people panel and feed on the right and no conflict.
- **Too short:** if the picture's top would come within 8 px of the Squad button (panel under about 830 tall), use 240 x 135, left 240, bottom 14, right of the scope. At 1280 x 720 that ends at x 480, clear of the goal line (x 546).
- **Keep-outs:** add its rect, grown by 14, to `HudRects` (`HudToolkit.Attention.cs:125`), the Edges `blocks` (`HudToolkit.Less.cs:1699`), `EdgePlace` (`HudToolkit.CuesEdge.cs:91`, which only knows the column) and the bubble check (`BubblesOffCallouts` or `InPlay`).
- **Budget:** the rest shot `office-front-yaw210` measures 6.5 % with `bun tools/hud-budget/cli.ts` (limit 15 %), so about 9.5 % with the clip by arithmetic; not measured with it in. Manifest kind `image`, text "capture clip".

### Frame and label
- **Picture:** corners r 12, clipped.
- **Ring:** 2 px cream `#f7f4ec` over a 4 px ink `#17302f` at 0.7, the medal's own strokes (`MedalRing`). The second, breathing cream ring (the medal's wary stage, 1.6 s) runs while the clip plays.
- **Corner cameo:** `cameo_enforcer_normal`, 28 px, chrome ring, lower right overhanging by 6, as the medal's badge.
- **Chevron:** the medal's dock chevron (cream, 2.6 px, over ink), 5 to 12 px outside the rim, on the side facing the exit door, re-aimed each frame, on screen or off. This is the "where".
- **Word:** `plate_dark` 26 px, sitting on the bottom rim, left 8. Jost Bold 13.5, cream: **Service door**, or **Public door** when the service door is held (the engine's rule, `g.DoorHeld(g.ExitDoor)`, `GameOrders.cs:319`). Both are the game's existing names in `text.json` `targets`. Goes in `text.json` through Fable. No other text.
- No kind colour, no new sprite.

### The moment
- **Start:** mission tick `BusyEnd − 102` (10.125 s at 10 Hz, rounded up). That's 27.9 s into a 38 s Enter, 57.9 s into a 68 s one.
- **Slave it to the mission clock, not to real time.** Video time = (mission time − start). Pause when the game pauses, `playbackSpeed = Speed`, reseek past 0.2 s of drift. Prepare the player when Enter lands, so the start doesn't hitch. If it isn't prepared 3 s before the end, skip the clip for this Enter.
- **Appear:** 0.15 s fade with one 1.05 pop over 0.4 s. In a calm corner the moving picture does the rest.
- **End:** the last frame is the Enforcer with Vale on his shoulder, beside the door (I read the frame at 9.9 s), so it cuts straight to him on the street at `enter.out`.
- **Once per Enter:** key it on the Enter's start tick. A restart clears it.

### After he's out
**Yes, the last frame is held: 2.5 s of mission time from `enter.out`, then a 0.3 s fade.** On `enter.out` the ring turns turquoise-light with the kit's ready glow and pops 1.05 once; the chevron then follows him. `enter.out` also starts the fight (`StartFight`), so it mustn't stay longer.

### The click: yes
Left click on the clip jumps the camera; never an order, and it consumes the click.
- Before he's out: `host.LookAt` the exit stand (`S.ValeService`, or `S.GuardPost` when the service door is held).
- During the hold: `host.CentreOn(OpId.Enforcer)`.
- Ground view: the clip plays and the chevron aims; a click leaves the view as T does, then jumps. Latch to confirm that path is clean.
- No yaw change: the player owns the turn. The silhouette rule should show him through a wall; **not checked at the service door at each yaw** (see done-check).

### Cases
| Case | What it does |
|---|---|
| In a fight | Plays the same, same size. Fights have overlays on the street, not in this band |
| Alert ≥ 2 | Same clip, later start (57.9 s). `alarm` is read once when Enter is given (`GameOrders.cs:769`), so Alert rising mid-Enter doesn't move `BusyEnd` |
| Scenes set to Never | Plays. It isn't a scene: it stops nothing, and the setting doesn't touch it |
| A conversation scene lands during it | Hidden with the rest of the HUD (`ConvoHud`), and frozen because the clock is. Returns on the same frame |
| Paused | Frozen on its frame, still clickable |
| Menu, fail camera, end card | Hidden; ends with the mission |
| Enter cancelled | Closes at once, no hold (0.15 s fade). He has no Vale, so the clip would be a lie |
| Taken down inside | Doesn't arise as far as I read: every targeting path I read skips `Inside`. I didn't read them all |
| Taken down during the hold | Closes at once |
| Both doors held | Holds the last frame, word **Doors held**, until `enter.out`. N85 says this block can't reach it in play |
| Vale routes with no Enter (the pickup) | No clip |

### Asset
Today's 480 x 270 is upscaled 1.33x at k = 2. Cut a 640 x 360 copy from `paid/065-capture-pip/final/capture_full_1344x768.mp4` with ffmpeg, audio track stripped (free, no paid call). `paid/` is git-ignored, so the copy has to go somewhere the build carries.

### Done-check (Latch)
Shots at 1080 and 2880 x 1864, cropped 2x, at: the start, mid-clip, `enter.out`, the hold. Two frames a moment apart differ. `enter.out` lands within 0.2 s of the last frame at Speed 1 and at top speed. Click jumps to the door at yaw 30 and 210, and he can be seen coming out at both. Same seed and orders give the same hash with the clip on and off. Next owner: Aster for the GDD line (brief 065 asks for it; §10 has none yet).

## 2. The bubble Sky likes: the live inset

**It's the attention callout's medallion, called the live inset here:** `Medal` in `HudToolkit.Attention.cs`, drawn from `HostViews.InsetFrame` (`HostViews.Attention.cs:205`). A small orthographic camera renders that one person, on their own layer on cream, into a 128 px render texture every frame while it's up, so what he sees is the real figure moving. Crops: `live-inset-crops-2x.png`, first two.

**When it was added:** first committed 2026-10-21 in `5d44a539`, built for brief 031. Sky asked for it himself that day: "do a sort of PiP kinda feature showing when something changes state" (`briefs/031-everyone-watches.md:283`). Worth telling him.

**What triggers it:** a security watcher (guard, officer, robot, camera) steps up a stage towards an agent (`HostAttention.StageUp`): noticed once per watcher per mission for 2 s, wary 3 s, reporting until the report lands (6 s cap) + 1 s. 72 px over the watcher on a stem; 56 px at the screen edge with a chevron when off screen or nothing fits; a click centres on the watcher. Up to four. Not civilians or rivals; a camera gets an eye disc instead of a picture.

**"The guard on a call":** probably the guard's reporting stage (dashed red ring, radio). Inez's call doesn't trigger an inset today, so it can't have been that. I can't prove which moment he saw.

### Does it survive this round?
| | Finding | Action |
|---|---|---|
| 084 edge icons | On-screen insets survive. **Off-screen ones don't:** `FightCues.Edge` hides the dock (`HudToolkit.Attention.cs:210`) for a still badge chip. The switch opens off in the Handover (`FightCues.Sync`), so 0.2.177's play had the dock | **If Sky promotes the edge icons, keep the live face:** the chip's face shows the inset's render texture for a callout's watcher. Don't promote line 210 as it stands |
| Conversations | Hidden during a scene, back after. But `Until` is real time, so a callout up when a scene starts can expire unseen | Add the scene's duration to `Until` while `ConvoOn`, as a view-only change. Small |
| Ground view | **Never shows** (`!host.Following`, line 176). So he saw these on the map | Leave it; ask only if Sky misses it there |

### Two bugs to fix so it holds (Latch)
- **The dock can sit under the HUD.** Dock placement clears only the roster column, though the 031 board also asked for scope, notes key and pill. Crops 3 and 4: in the proof shot `commander-firing` the clock bar covers a dock; in `r059-watch-alt` the dock at (1604, 963) is inside the feed's rect and can't be seen. Slide it along the edge clear of `HudRects`.
- **Two docks can overlap.** `want` ignores `claimed`, so two people in the same direction land on each other. Same fix.

### Mismatch, for Aster
GDD §10 says "off-screen watchers get none", and `briefs/031-everyone-watches.md:284` says the same ("no inset for now"). The build docks them, following the 031 board. Sky likes the behaviour he describes as off screen, so the GDD line should change to match the build. His call; not edited.

### Wider use: yes, narrowly
Sky's liking supports one new trigger, using the same element: **a named person off screen speaks a line that changes the plan.**

- **The pressed guard's arrival (Fable's second pass), the required one.** Held until Keel lands the rule, as Fable says. When `street[0].beats` plays and the speaker is off screen:
  - Open a docked inset of **the speaker alone**: Inez for her line, the guard for his. One at a time, not two: they're in the same direction.
  - Thin cream ring, no agent badge (no agent has been noticed; line 233 leaves a stale badge, so hide it when `ak < 0`), chevron, click centres.
  - Draw the line in the normal speech plate beside the inset, tail towards it, clear of the HUD. `Bubbles()` drops off-screen speakers at line 548, so this is the one exception, keyed on a docked inset for that speaker.
  - Each is held to read. Hers on arrival, his before he leaves; the 5 s stay is tight for both at one line each, so **his may run on as he runs**. Fable's "Inez warned him." beat still goes in the feed.
  - In view: ordinary bubbles, no inset.
- **Recommended, for Sky's yes: the ordinary call's lines**, `guard-lured` and `guard-at-cafe`, when off screen and no scene is playing. Same code, and it would give him what he remembers: the guard on a call in a bubble.
- **The capture clip** matches the family, as in section 1.
- **Not further.** Not every off-screen event, not the cell (their edge cameos and roster cover them), not the crowd. Scarcity is part of why it lands.

Note 9's feed rework doesn't touch any of this.

## 3. The Pressure price at rest

**Add one price field: `Door 1:40`.** Text only, in the normal field style, after the Trail field, in `HudModel.PriceLine`. The line at rest then reads:

> Pressure · 25 s · +3 · **Door 1:40** · eye · mark

- **Where:** the same list drives the ring wedge, hover chip, hold pill and panel row, so all of them get it at once.
- **When:** the Pressure way on Inez, and also the Call way on Inez once she's been pressured, since that's when the plan is timed. Hidden once the guard is down or already called. Help then Pressure counts, as in Motive's rule.
- **Why this one:** it costs one short field, and it's the only way that says what you lose in the same glance as the price. A number with no word would read as another duration beside 25 s. A warning icon says something's wrong but not what.
- **Why not "3:00" on Help too:** the fields only show what's not zero, and three more characters on every wedge costs more than it teaches.
- **Words:** exactly `Door 1:40`. Time from the engine rounded to 10 s when Keel exposes a read (Motive measured 1:38.9 on 500 seeds); text until then. Add `door` to `less.price.fields` and to `FieldOrder`, or the panel's columns drop it. Fable's long line stays on Alt and in the panel.
- **Gate:** same as Fable's: off until Keel lands the rule and Aster says so. It doesn't give away the affair: Motive notes that Pressure needs the Listen that teaches it.
- **Done-check:** 2x crop of the Inez ring with it in; the wedge's pill must not clip (`HostContext.cs:213` allows about 230 px past the wedges, not measured with this field).

## 4. A bystander past a conversation

They read as the street carrying on, at their normal walk with no fade or marker, passing in front of the speakers if that's where they're going; I need nothing for it beyond GDD §10's rule (the scene already hides all HUD marks).

## Evidence and limits
- Read: the code at the lines above, `briefs/031/cues.html`, Motive's and Fable's notes, the clip's frames and metadata (480 x 270, 24 fps, 10.125 s).
- Run: `bun tools/hud-budget/cli.ts out/batch/shots/office-front-yaw210.png --state rest` → PASS 6.5 %. `git log` for the dates.
- **Not run:** Unity, or any build. The inset's motion is from the code plus Sky's word. Figures use `AnimatorCullingMode.CullCompletely` (`HostViews.Figures.cs:97`); an inset camera should count as seeing them, so a docked figure should still animate, but I haven't seen one do so. Latch's two-frame check settles it.
