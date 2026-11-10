# Centring: numbers, letters and icons in discs (Glyph, 2026-11-10)

Sky, playing 0.2.310: "another non centered icon". The frame shows the destination disc on the square: a cream disc with a turquoise ring (the selected agent) and a "1".

## The fault, measured

- **Sky's frame:** the disc spans x 29–82 and y 40–93 (54 px for an 18 px disc, ×3). The "1"'s ink centre is 3.5 px right and 5 px low, which is **1.2 px right and 1.7 px low** at 1080p. It sits right and low, not left: the "1"'s flag reads as weight on the left, but the stem is right of centre.
- **The batch's own frame** (`out/batch/shots/fight-safe-body.png`, the 18 × 18 street mark at 885,548): a selected "4" sits **1.5 px right and 2.0 px low**.

## The cause (one rule, broken in many places)

UI Toolkit lays out an absolutely placed child from **inside its parent's border** (Yoga's padding box). A child placed at `(box − size) / 2` inside a bordered disc therefore sits one border width right and one border width low. The destination label was placed at (0, 0) at full size inside a disc with a 1.6 px ring (0.8 px unselected): **+1.6 px each way** in theory, which matches both frames.

On top of that, a label centres its advance, not its ink. Jost Regular-Tab's "1" has its ink 0.055 em left of its advance centre (Medium 0.048; measured with fontTools on the kit's TTFs), so the "1" also sat 0.55 px left at 10 px.
- Other figures are under 0.02 em; "7" is +0.013.
- Capitals off by 0.02 em or more: D, K, L, P, R (up to +0.038) and J (−0.096).
- Vertically there's nothing to fix: Unity centres the line box (hhea 1070 / −375, middle 0.3475 em), and Jost's figures and capitals ink at 0–0.71 em (middle 0.35).
- Sprite art isn't a cause: `disc_dark_24`, `disc_dark_30`, `plate_light` and `plate_dark` are centred to within 0.25 px.

The brief 101 round 3 "round tabs" repeated the border fault everywhere they put a glyph in a 2 px-ringed disc. The scope's numbered blips were the one place done right: a label stretched to the padding box. That's the precedent I reused.

## The shared rule: `HudToolkit.Centre.cs` (new, 66 lines)

- `Middle(parent, w, h, name)` puts a w × h child in a holder stretched over the parent's padding box (left, right, top and bottom 0), centred by flex. It stays centred at **any** border width: a ring that thickens on selection or while a warning pulses, or a pooled street mark that gets a rim.
- `CentreNum(parent, style)` makes a label that fills the padding box, middle-centred. `SetNum(label, s)` sets its text and nudges a single character by its measured ink offset (the "1" moves +0.55 px).
- `BkMid(disc, d, border, …)` is the book's version for "?" in a hollow disc.

## Each badge

Offsets are at 1080p (book: its 1920 grid), + = right / low. All are now 0, apart from sub-pixel rounding.

| Badge | Before | Cause | Fix |
|---|---|---|---|
| **Destination disc** (the one Sky saw) | selected +1.6 / +1.6 (a "1": +1.05 / +1.6); unselected +0.8 / +0.8 | label at (0, 0, 18, 18) inside the 1.6 / 0.8 ring; the "1"'s ink | `HudToolkit.Less.cs:717-718` CentreNum + SetNum |
| Street marks' glyph (pooled `WorldEl`) | +border whenever a caller adds a ring | glyph at 0.2w inside the border | `HudToolkit.Less.cs:590, 594` holder via Middle; the callers that placed it themselves drop left/top: `:743` (dead camera, was right), `:838` (move preview, was right) |
| **Hint verb tab** on the street | +2 / +2 | 13.5 px glyph at 6.24 inside a 2 px ring | `HudToolkit.Less.cs:657` |
| **Eye disc** with a coloured rim | +1.2 / +1.2 (only when the rim isn't cream) | glyph at 3, 3; rim 1.2 | `HudToolkit.Less.cs:849` |
| **Rival stop** tab | +2 / +2 | glyph at 5.3 inside 2 px | `HudToolkit.Rivals.cs:102` |
| **Rival stand** disc | +2 / +2 | glyph at 5.76 inside 2 px | `HudToolkit.Rivals.cs:180` |
| **Hand-off take** tab | +2 / +2 | glyph at 5.3 inside 2 px | `HudToolkit.Rivals.cs:214` |
| **Rival going for the car** (car glyph) | +2 / +2 | glyph at 6.24 inside the inner tab's 2 px | `HudToolkit.RivalsForCar.cs:22` |
| **Known-face badge** (the left-dock roster) | +1.5 / +1.5 | icon at 2, 2 inside 1.5 | `HudToolkit.Hold.cs:312` |
| **Police disc** on the loud pill | +1.0 / +1.0 | badge at 2, 2 inside 1.5 | `HudToolkit.Pill.cs:70` |
| **In-car badge** (car glyph and cuffs) | +1.1–1.3 on the street faces; +1.4–1.6 on the old row | its ring (1.4 / 1.6) is added at run time, after the glyph was placed for none | `HudToolkit.Faces.cs:64`, `HudToolkit.cs:472` |
| **Act mark's holstered gun** | +1.4 / +1.4 | icon at 3.5 inside 1.4 | `HudToolkit.ActMark.cs:97` |
| **Fail marker** (eye or shot) | +2 / +2 | icon at 5 inside the 2 px red ring | `HudToolkit.cs:629` |
| Objective dot | +1.4 / +1.4 | dot at 3.3 inside 1.4 | `HudToolkit.cs:261` |
| Armed glyph (old row) | +1.6 / +1.6 (street faces: 0) | glyph at 3.4 inside 1.6 | `HudToolkit.cs:480`, `HudToolkit.Faces.cs:65` |
| Sidearm mark (old row) | 0 at rest; +1.0 while the warning pulses (2.6 px); −0.2 at 1.4 | hand-compensated for 1.6 only | `HudToolkit.cs:468` (Middle) |
| Face sidearm, status and known badge | 0 (hand-compensated) | same pattern, compensated by hand | `HudToolkit.Faces.cs:54, 60, 71` → Middle, so a thicker warning ring can't move them |
| Ring focus halo | +0.6 / +0.6 | halo at −3.6 inside a 2.6 ring | `HudToolkit.Ring.cs:181` (−4.2) |
| Scope's numbered blips | "1" −0.47 px | ink offset only (already stretched) | `HudToolkit.Scope.cs:114, 195` SetNum |
| Mast heads' number plates | "1" −0.58 px | ink offset (sprite plate, no border) | `HudToolkit.Takedown.cs:212` SetNum |
| Roster key plate (old row) | "1" −0.58 px | ink offset | `HudToolkit.cs:1068` SetNum |
| **Book "?"**: start-page needs | +1.5 / +1.8 | placed from the disc's outer edge at 0.3d, left-aligned; 1.8 ring | `HudToolkit.Book.cs:219` BkMid |
| **Book "?"**: facts | +1.9 / +1.8 | same, at 0.33d | `HudToolkit.Book.cs:159` BkMid |
| **Book "?"**: debrief ticks | +1.6 / 0 | same; its vertical had been nudged −2 by hand | `HudToolkit.BookDebrief.cs:28` BkMid |

### Checked and fine (left as they are)

- **Already centred:** the dead-camera disc; Intel's badge, tick and slots; the take-control and verb tab faces (`Tabs.cs`, sized to the inner box); the edge faces; the pops' badges and the on-way discs (flex-centred).
- **Sprite discs** without a border: the act disc, the walk mark, the target discs and the cursor mark.
- **Keycaps** (hint keys, card keys, edge keys): sprite plates with flex-grow labels. Their letters sit within 0.4 px, so `SetNum` isn't applied to them. If a key ever shows a lone "J" or "1", call `SetNum` there.

## Proof after the next build

- **Destination disc:** batch `fight-safe-body` (the street mark at 885,548 should measure 0 / 0) and Sky's square view; also r067-reticle-set and r123-looks-310 (walk orders in frame).
- **Scope blips:** every batch shot's plot scope.
- **Rival stand, stop and hand-off tabs:** r059-235-square and r059-625-parked. Rival-for-car: no proof frames it yet.
- **Police disc** on the loud pill: r123-fight-loud.
- **Book "?":** the debrief captures (`DebriefShots`) for the ticks; the start page for needs and facts.
- **Not in any proof yet:** the hint verb tab, the coloured-rim eye, the known-face badge, the fail marker, the in-car badge and the act mark's gun. They are layout maths only, so a play look is enough.
- **Measuring:** `python3 briefs/101/glyph/ink.py <png> x y w h` measures ink against a manifest rect. Worth folding into the HUD budget as a centring check if this comes back a third time.

Checks run: the runtime compiles clean against Unity's assemblies (`out/scratch/glyph/compile`); host logic 202/202. Views only: no order, tick or engine input changes, so replays stay exact.

## "Get out" on the car (Aster / Sky, 2026-11-10)

Sky read the car's way out as "Move" (drive the car) and got everyone out by mistake.

**Where "Move" came from:** `HostContext.RingWord` names a wedge by the engine verb. Step out's verb is `move`, and text.json has no `ring-move`, so the wedge fell through to the capitalised key: **"Move"**. The order card (`HudModel.ShortName`) did the same through the move's label, so it said **"Walk"**. The hover used the intent's own label ("Step out").

**Fixes:**
- `contracts/missions/handover/text.json:354`: `step-out` label "Step out" → **"Get out"**. `:3415`: the held-Go line "Step out and draw · or down them" → "Get out and draw · or down them", so it names the same click. The StreamingAssets copy is updated to match; the export writes the same file.
- `HostContext.cs:264-265` (in place; the file is over 500 lines): a step-out wedge reads its own action's label. The car's Drive now reads `ring-drive` (before, it fell to a hard-coded "Drive").
- `HudModel.Less.cs:48-50`: the card names a step-out by its own label. Board and Go read their text labels instead of hard-coded words.
- `HostLent.cs:120, 124`: the fallback word is "Get out", and the way carries its text's "does" line.

**Other car verbs:** Board, Go and Drive read `ring-board`, `ring-go` and `ring-drive` on the ring, and their own labels on the card. None falls back now.

**Tests:**
- `contracts/missions/handover/text.test.ts:2027` now expects "Get out"; text tests 178/178.
- `tools/game/hostlogic/HostLogic.Lend096.cs` pins the ring word, card name and hover label of the step-out ("Get out"), plus Go's and Board's ring words; Lend 9/9.

**Proof gap:** no proof frames the car's ring with an agent aboard. Suggest one after r067-board-landed: the car's ring with the Scout aboard, reading Board · Drive · Get out · Go.

Fable owns the player's words, so they should confirm "Get out" in their next pass. It's Sky's word, 7 characters, under the ring's 8.
