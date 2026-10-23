# The Suborned · agent art bible

The short version of GDD §15 ART, given to every agent who makes or builds anything seen: Glyph, Vela, Forma, Glint, Latch. Sources, which win on conflict: GDD §15 and `contracts/style/suborned.style.json`. Judge work against pictures (the concept board, the 26 Sep paintings in `briefs/008/concepts/`, the game as built), not against this prose.

## The look in one breath
Bright retro-future: a 1950s world that discovered atomic power centuries ago. 1950s style, technology centuries ahead, no thinking machines or networks. Optimistic surface, coercive underneath. Design test for every object: does it read as 1950s style built with centuries-ahead tech? Streamline, Googie, space age; no old-city stone or brick, no zebra crossings, no present-day street items.

## World
- **City:** cream and peach limestone under a warm hazy sun; saturated enamel accents (cobalt, flamingo, sunshine, mint, turquoise) on panels, awnings, signs and trim, never whole buildings; chrome fixtures; palms and cypress; long soft shadows.
- **Civic block:** cream stone, seafoam and deep teal enamel bands, chrome spheres, banners, planted terraces, pale paving with teal bands.
- **Brass is a thin line only** (frames, beads, hairlines, SF-21), never a large surface. Gold only on skyline crowns.
- **Materials:** honed limestone, granite kerbs, vitreous enamel panels, chrome and brushed silver, white vinyl, clear acrylic, pale asphalt.
- **Character over triangles:** keep silhouette and chrome on props; never slim a prop until it loses its character.

## People and robots
- **The city's people** wear space-age couture as street clothing, never 1950s period dress. They're a mixed cast, with job readable at a distance by silhouette plus one colour block. No long coats or flowing hems (they're hard to animate).
- **Security and institutions:** sleeker; ivory, ink black, gold trim, chrome couture, armour accents.
- **The cell** are visitors from a nearby timeline: sharper and cleaner, matte technical fabrics, fitted layers. No deco, gold trim or 1950s tailoring. They pass in the street and stand out at 50 px.
- **Robots** are 1950s style and clockwork-smart, with dot-matrix display faces, one personality prop each, and no cartoon eyes.

## Signals
Cones are red, the guard amber, the rivals violet. They are drawn unlit and emissive, outside the post grade, each with its own shape or motion cue. They don't own their colours: red, amber and violet are free for the world.

## HUD and overlays
- **Simple, Syndicate-style, built for real time.** Less reading: no sentences in play except speech, icons on characters, one-line prices, world markers.
- **The as-built kit is the reference:** the round 4 v10 "signage" kit (`briefs/011/ui/round4/`), the roster in the left column, the top-right clock bar and the feed card.
- **Never ship a prototype's look.** Playgrounds and mockups (`testbed/controls`) test behaviour with flat web styling. Port the behaviour, then restyle it in the kit before it reaches the game (Sky, 2026-10-22: "those overlays look awful - they're from the control lab").
- **Check every HUD piece cropped at 2x** for clipping, overlap, centring and padding before Sky sees it. Glyph reviews it.
- **Kit details (brief 044, direction A "Chrome plate", revised 2026-10-22 after Sky's pick: no kind colours; pictures `briefs/044/`, board `board-revised-a.png`).**
- **Colour rule: no colour on the HUD without a source** in a kit sprite, `kit.uss`, or the style contract's colour where the HUD already shows it. A new overlay never brings its own palette; kind, harm and role never get a colour of their own (the lab's, and the first A's, blue, green, coral and role rings were dropped for this).
- **Frames are the kit's own nine-slice sprites, never CSS fills:** `pill_status` (the order pill, 38 px tall, slice scale 0.43), `panel_dark` (chip, 0.5), `panel_strip` (harm, 0.3, as drawn: no tint), `plate_dark` (counter, pop, unlock pills, 26 px). Chrome rim, deep-teal body, corner screws; one chrome tail (18 × 11 px, body triangle #0c3434, the sprite's body) points at the thing.
- **Colours (all sourced):** ink #17302f, ink-deep #0b1615, cream #f7f4ec (all type), dim #b0cdc7 (`--text-dim`, the "Hold ·" head word), chrome #cbc6c3 (tail, face rings), panel line #d2e6e2 (`--panel-line`), turquoise-light #60e0ea (`--turquoise-light`: hold sweep at 40 %, "Let go" word and 2 px ring and glow, window underline, unlock arrow), brass #99784d as a 1 px hairline only, amber #f0a020 (the contract's guard amber, the HUD's own "running out" colour) only for the window underline's last 10 s. Nothing else.
- **Kind reads by its icon on a kit disc the same size as the face cameo** (Sky, 2026-10-22: "make the icons larger like avatar on right"): `markers/disc_dark_24` scaled to the cameo, chrome ring like the cameo's, the icon bright cream and filling most of the disc (never grey on dark); harm/fire uses `disc_light_24` (ink icon). No bar, no kind colour. Faces are cameos in a chrome ring (2 px #cbc6c3, 1 px ink-deep), never a role colour.
- **Type:** Jost Bold only: 15.5 pill, 15 chip lines, 14.5 tabular counter, 13.5 tag and pop, 12.5 unlock pills. Barlow Medium is speech only. **Icons:** kit `icons/singles` (solid pictograms, 64 px @2x, stroke about 8 %), drawn 12–16 px, tinted cream; no outline icons.
- **Pill states:** offer = disc, words, faces (and a "Watched spot" plate with the kit's `stripes_warn` block); hold = turquoise-light sweep (alpha 0.4) inside the window; ready = "Let go" in turquoise-light, 2 px ring, glow, scale 1.05; fire = `panel_strip` (chamfered corners), a `stripes_warn` block at each end, the light disc with the fire icon. Harm reads by shape (chamfer, stripes, light disc), never by colour.
- **Chip** (above the thing, 4.2 s): `panel_dark`, no bar, 32 px face with the kit's `mark_ok` / `mark_half` / `mark_no` badge, one icon + word per fact (max 3), brass hairline, then ›, small `plate_dark` verb pills (a shut way struck through). **Window marker:** `plate_dark`, eye icon + seconds (or the Tech's face), 2.5 px draining underline, blinks amber at 10 s. **Cancel tag:** ink-deep plate, dashed cream 1.5 px edge, 9 px `stripes_warn` block, ✕ + words. **Pop:** solid `plate_dark`, icon + one line.
- **Don't:** flat web pills or white 2 px borders; the lab's #4aa8ff, #4cc35f, #ff4d7d; any kind, harm or role colour (bar, disc, ring or fill); darkening a sprite into a new colour; brass as a fill; a new colour for cones, guard or rivals; any sentence; ship before the 2x crop check.
