# 061 · Crossings redesign

**Sky, 2026-10-24:** "don't think on the thin brass - if anything maybe remove it. I really don't like our pedestrian crossing design either - doesn't work and badly implemented. The lights are cool but seem to always be facing camera. I would have them change color, run a clock for showing state. Also there's way too many crossings."

**Owners:** Vela (look: brass, the signal, the surface; this part) · Datum (how many, where; the section below) · then Splice, Latch, Glint once Sky has chosen.
**Status:** design stage. Nothing applied: no plan edit, no kit regeneration, no Unity change. Sky picks from the boards; Vela's mock-up code is `tools/decokit/crossing061.py` (not in `kit.FAMILIES`, so `bun run decokit` never builds it) and the `--nobrass`, `--x061`, `--billboard` and `--footway` switches of `tools/decokit/road_board.py`.

## Vela · what the pictures are, and one blocker

**Concept art did not happen.** The brief asks for painted concepts from the Codex image CLI first. `python3 -I tools/paid/paid.py codex 061-crossings 1 ...` failed three times (ledger rows 1 to 3 in `paid/061-crossings/ledger.jsonl`, $0), and `codex exec "say hi"` fails the same way: `Encountered invalidated oauth token for user ... token_revoked`, HTTP 401. The Codex login on this Mac needs `codex login` by Sky. I did not touch it, and I did not use Runware (paid; needs Aster's go-ahead; the helper's Seedream 5.0 history is about $0.10 an image, so 6 images is about $0.60).

Instead the three directions are **3D mock-ups in the real world**: Blender frames of the actual export (`out/world/harbour-district.world.json`, 2026-10-24), at the game's cameras, at the Records Office crossing (`crossing/orbit-walk/2`). They show scale, readability at play zoom and what the lens really looks like against the street. They are not painted concepts and do not carry the painted look. If Sky wants paintings, the same three prompts are in `out/scratch/vela/061/prompts.sh`, ready to run once Codex is logged in again.

## Vela · do the orbs billboard? No, and what they do instead

Searched `unity/Assets/Suborned` for `SignalEye`, `signal-orb`, `billboard`, `LookAt`: nothing in `Runtime/` touches a crossing signal. The only code is `Editor/World/CrossingKit.cs`, `Dress` (lines 75 to 139), which runs **once, when the world is built**:

- it takes the export view's yaw (`w.View.Yaw`, 30) and turns each orb's body, Walk and Stop objects about the vertical so a face looks along that heading (`turn = SignedAngle(face, toCamera)`, folded to ±90 degrees);
- the world export already says better: `signal.faces` = [walk, walk + 180 degrees] and the kit `yaw` put the two faces along the walk. `Dress` overrides it;
- so the faces are **baked toward yaw 30 and stay there**. They are broadside at the opening camera (30) and at the Records Office front (210, the same line), and 60 degrees off at 120 and 300 after Q/E. They never track the camera and they never look at a kerb. That is the "always facing the camera" Sky sees, and it is why they look like billboards in third person and in the play shots;
- state is baked too: `signal.state` is `walk` in the plan, `Dress` switches the Walk object on and the Stop object off at build. Nothing changes at runtime. The Stop figure exists and is never shown.
- the figure is a 0.4 m pictogram on a 0.51 m plate: about 12 to 15 px at play zoom (38.6 px/m at 1080p).

Board: `briefs/061/crossings-today.png` (real game shots, then a Blender emulation of what `Dress` leaves, at the four Q/E yaws). I did not capture a Unity Editor shot at yaw 120: Latch can confirm in one frame. The fix for the orientation alone is to delete the turn.

## Vela · 1. The brass on the road edge

**What is still laid** (counts from today's export): three thin brass lines, none large.
1. `PaveEdge_Pinstripe_6m`: 5,129 pieces, 0.04 m, 0.5 m in from every kerb face, on the pavement.
2. The kerb crown: a 0.02 m brass box on top of `Kerb_Pearl_0/1` (`pave026.py` line 45); 1,485 kerb and kerb-return pieces.
3. The ribbon's two hairlines: `RoadRibbon_Straight_6m` and the two `Arc90` pieces carry a brass hairline each side of the lens (`road_ribbon.py`, `ribbon()`); 5,805 pieces.

The enamel set already dropped the ribbon's pearl shoulder; the lozenge hairline was dropped earlier. The crossing's own brass (studs, edge lines, arc and dot frames) is separate: it goes with the crossings (below), not with the road edge.

**Board:** `briefs/061/brass-before-after.png`. Play zoom (Records Office front, yaw 210), third person (the Boulevard) and the evening, before and after, with an enlarged kerb crop. With all three gone the strip-light lens is the whole edge. At play zoom in daylight the difference is small, which is the point: the brass was never carrying the edge; at evening the light line is cleaner.

**The exact plan change, for Datum (not applied until Sky sees the board):** only (1) is a plan key.
```
standards.streetscape.roads.finish.sets.enamel.drop:  ["shoulder"]  ->  ["shoulder", "pinstripe"]
```
(and its `why`: "brief 061, Sky 2026-10-24: no brass on the road edge"). `tools/plan/road-finish.ts` already accepts `pinstripe` in `drop`; the `asphalt` set drops it too, so the `pinstripe` key itself can be deleted in a later clean-up. The world export loses all 5,129 `road-pinstripe` ground pieces.
(2) and (3) are kit pieces, not plan keys: Vela edits `pave026.py` (drop the crown box) and `road_ribbon.py` (drop the two `_band(..., pal.Brass_Civic)` lines), then Splice re-adopts and `bun run kit` hashes. No generator change.
GDD: the art bible line "Brass is a thin line only (frames, beads, hairlines, SF-21)" stays true; if Sky confirms, §15's street-level brass list loses "road edge". Aster's call.

## Vela · 2. The crossing signal

### Three directions

All three face along the walk, change colour by state and run a clock; all three drop the brass edge lines, studs, seafoam arcs and the lit field. Each board has WALK, CLEARING (7 s left) and STOP at play zoom (day and evening) and at eye level.

| | head | clock | road light | read at play zoom | read at eye level | verdict |
|---|---|---|---|---|---|---|
| **A** `briefs/061/direction-A.png` | the floating eye: a chrome lens (a coin on edge) 2.55 m up, no pole, faces along the walk, a lit rim ring all round | two-digit numerals on both faces; a 24-segment ring in the paving at the beam's foot drains like a clock | the dot rows, lit in the state colour; in CLEARING they go dark from both kerbs toward the middle | strong: the rim, the ring in the paving and the dots all change colour | the figure and numerals read at 6 m; edge-on the rim ring still says the state | keeps what Sky likes ("I do like those eyes", "the lights are cool") |
| **B** `briefs/061/direction-B.png` | a 3.0 m finned ivory and chrome pylon, tall oval display | a 1950s station-clock dial whose sector drains | two continuous strip lights (like the ribbon) | weaker: thin lines; the pylon is small from above | the most legible head | reads as a present-day pole signal; 3 m of clutter at every kerb end |
| **C** `briefs/061/direction-C.png` | no tall head: a 1.0 m chrome lantern, a ring of 24 segments round its crown | the crown ring and a ring round a pictogram disc in the paving | one broad band of light across the road | loudest | the small lantern carries least | it is the lit field and the landing slab Sky already rejected |

### Pick: A

1. It is the only one that keeps what Sky said works: the floating eyes and the lights. The change is orientation, state and a clock, not a new object.
2. It reads at play zoom without the head: at 38.6 px/m a 0.9 m lens is 35 px, the figure about 17 px and a numeral 12 px, so the colour of the rim, the ring in the paving and the dots carry the state; the figure and numerals carry it close up and in third person.
3. It is the one that survives every camera. `briefs/061/pick-A-readability.png`: at the four Q/E yaws a crossing along an axis is at most 60 degrees off a face; edge-on (third person, free camera) the rim ring and the ring in the paving still say the state, and a face is not needed.
4. It is the cheapest: same piece count as today, no new pole, no 3 m pylon, no slab.
5. B is the better head and the worse street; C is the better road light and the wrong idea (a lit slab).

What I would take from the others: nothing. B's dial shows the clock as a sector; A already has that in the paving ring. I would keep the numerals (the 7-segment look is a 1950s clock) and the dial idea out.

### Spec, pick A (not built)

**Kit pieces** (pivot = the ground point under the head, faces on local -Y and +Y; local -Y looks across the road, +Y over the pavement, as the export places `SignalEye` today).
- `SignalLens` (replaces `SignalEye`): body and nozzle (chrome lens, 36-sided lathe, charcoal plates), a rim (own material slot, emissive), two face quads and four digit quads per face, `Beam` and `Halo` (glow parts as now). Target at most 700 triangles L0 and 250 L1 (the mock is about 1,000 with its figure polygons; the shipped faces are atlas quads). Faces on an emissive atlas: walking figure, standing figure, digits 0 to 9, 64 px a glyph. Parts share the origin, as `SignalEye`'s do.
- `HaloRing`: the 24-segment clock ring in the paving as one quad with radial UV, so Latch fills it by one property, not 24 objects.
- `LightDotRow_2` and `_1` re-cut without the brass frame ring (a charcoal enamel seat instead), UV x = metres along the row, so a per-module lit range can gate each dot.

**What goes** (counts from today's export): `CrossEdgeLine_1/2` (1,788 pieces; brass edge lines), `KerbStud` (768; they render as dark brown blobs), `CrossInlayArc_R24_W08` (370; big seafoam arcs, repeated), `LightField_1/2` (799; the lit field), the brass frame on the dot rows. Board: `briefs/061/pick-A-states-and-cycle.png`, top row, today against A.
Plan keys that would change if Sky picks A (Datum, later; not now): `crossings.strip.lights.edge`, `.field`, `studs`, `arc` removed; `signal` gains `cycle`; SF-20 and SF-21 and `tools/world/rules/crossing-parts.ts` are rewritten by Splice.

**States** (four; the board shows the faces tight): WALK mint #55E6B0, steady; CLEARING amber #FFB624, the figure walks and the numerals count down; STOP red #F2432E, standing figure; ARRIVING (the last 3 s of STOP), red, numerals 3 to 1, the ring fills, the rim blinks. The state does not rest on colour alone: figure (walking against standing), numerals and the lit-dot count differ too. Red on a small emissive display is allowed (§10, §15, Sky brief 009).

**The cycle** (runtime look only; the sim reads nothing from it): per crossing, W = kerb-to-kerb carriageway width crossed.
- WALK 6 s; CLEARING = ceil(W / 1.4 m/s); STOP = 14 s + CLEARING, its last 3 s ARRIVING. A 13 m crossing: 6 + 10 + 24 = 40 s; the 28 m Boulevard: 6 + 20 + 34 = 60 s.
- Phase = (mission seconds + offset(crossing id)) mod cycle, offset = a hash of the id. Deterministic from the mission clock: the same on every replay, nothing random, nothing at runtime AI. Not frame time.
- Animation, Latch's runtime: a `SignalHead` component per head reads the mission clock and writes a `MaterialPropertyBlock` (about 52 heads after Datum's cut, so no batching concern): `_EmissionColor` for the state, the atlas rects for the figure and digits, `_Fill` on the `HaloRing`, and per-module `_LitFrom/_LitTo` on the dot rows. Colour crossfades 0.2 s, the pictogram cuts, the rim blinks 2 Hz in the last 3 s of CLEARING and in ARRIVING. The heads are not turned at runtime: they keep the export's `faces` yaw. Delete the turn in `CrossingKit.Dress`.
- Glint's emissive: base black, emission only (as `CrossingKit.Lens` does now: the seafoam is emitted at 2.0x linear because past the Neutral shoulder the channels flatten to cream). Red and amber drift toward orange and yellow-white when pushed; the mock at 4.5x shows red going orange on the rim. Glint trims per state (walk, amber, red each their own multiplier), raises them at evening and night as `roadLens` is raised, and checks the amber against the guard's amber cone, which has its own shape and sits on the overlay camera.

**Risk, one for Pulse:** Datum notes every kerb signal across a tram track stops the tram (SF-20). If a tram is to wait for WALK, the cycle becomes sim data (Keel, plan `signal.cycle` by id) and not a look. As drawn it is look only. See question 5.

## Vela · Datum's question: a lane mouth as a continuous footway

Yes, I can draw it, as pavement: `briefs/061/footway-sketch.png`, Satellite Street's mouth on Market Street. One table in the plots' paving at kerb height (0.18) fills the mouth between the 6 m kerb returns; the lane's traffic rides up a 2.4 m ramp (1 in 13), over it and down a 0.38 m pearl lip to Market Street; two pearl threshold bands; the strip-light ribbon runs straight on along Market's kerb; the mouth's returns and their ribbon, and the lane's crossing parts, are gone. No signal, no light, no marking. It adds no crossings and no heads. It is a generator change (plot edge to table) plus one ramp piece, owned by Splice and Datum, not a kit piece of mine. I would say yes: it reads as a gate threshold and it is the cheapest way to take 156 walk crossings off the crossing count.

## Questions for Sky

Each with the board to show next to it.

1. **Remove all three bits of edge brass (the pavement pinstripe, the kerb crown, the ribbon's hairlines)?** My pick: yes; the lens alone is the edge. Show `briefs/061/brass-before-after.png`.
2. **Signal head: A (the floating eye, oriented, with state and clock), B (the pylon) or C (the lantern and the lit road)?** My pick: A. Show `briefs/061/direction-A.png`, then `direction-B.png` and `direction-C.png`.
3. **Are these 3D mock-ups enough, or do you want painted concepts?** Codex needs `codex login`; or Aster approves about $0.60 of Runware (6 images). Show `briefs/061/direction-A.png`.
4. **The clock: numerals on the faces plus the ring in the paving (A), and a fourth state, ARRIVING, so the last 3 s of STOP counts down to WALK?** My pick: yes to both; it is what lets the player see a window open. Show `briefs/061/pick-A-states-and-cycle.png`.
5. **Look only, or should trams and people obey the signal?** As drawn the cycle is look only. If trams stop for STOP (SF-20), it becomes sim data and Pulse's timetable has to carry it. My pick: look only for now. Show `briefs/061/pick-A-states-and-cycle.png`.
6. **Drop the seafoam inlay arcs, the brass studs and the lit field for good, as the A board does?** My pick: yes; the ring in the paving marks where to wait. Show `briefs/061/pick-A-states-and-cycle.png`, top row.
7. **Datum's lane mouths: a continuous footway (this sketch), yes or no?** My pick: yes. Show `briefs/061/footway-sketch.png`.
8. **Heads edge-on in third person and free camera read by their rim and ring, not by their face. Enough?** Show `briefs/061/pick-A-readability.png`, third row.

## Vela · next owners once Sky chooses
- **Datum:** the `enamel.drop` change above; later the `crossings.strip` keys.
- **Splice:** re-adopt the edited kerb and ribbon pieces; adopt `SignalLens`, `HaloRing`, re-cut `LightDotRow`; retire the retired pieces (`CrossEdgeLine`, `KerbStud`, `CrossInlayArc`, `LightField`); rewrite SF-20 and SF-21; export `crossings[].cycle` (`walk`, `clearing`, `stop`, `arrive`, `offset`) from the plan by id and width.
- **Latch:** delete the turn in `CrossingKit.Dress`; add `SignalHead` (clock, property blocks); Editor shot at yaw 120 of today's heads for the record.
- **Glint:** emission per state, evening and night multipliers, the amber check.
- **Done-check for Vela's pieces:** a sheet beside this one with measured triangles, then in-game shots at 30, 120, 210 and 300, and eye level, in all four states.

**Evidence:** Blender frames of the world export, `tools/decokit/road_board.py` (`--nobrass`, `--x061 A|B|C --state walk|clear|stop|arrive`, `--billboard 30`, `--footway`), pieces `tools/decokit/crossing061.py`; boards in `briefs/061/`; scratch in `out/scratch/vela/061/`. `tools/decokit/road_board_post.py` now reads the linear follow haze (schema haze/2). Counts: `python3 -I` over `out/world/harbour-district.world.json`. Not run: `bun run plan`, `bun run typecheck` (no plan or TypeScript change), Unity.

---

## How many crossings, and where (Datum, 2026-10-24) · proposal, not applied

Sky, 2026-10-24: "there's way too many crossings." This section covers the count and the sites. The crossing's look is Vela's (above). It is a change to the plan's `standards.streetscape.crossings.coverage.rules` and is **not applied**: it waits for Sky's OK. People still cross any road anywhere (standards.walking). A crossing is dressing and traffic calming. It is never the only way over.

**Pictures** (before | after, today's export with the proposed rules applied):
- `briefs/061/crossings-district.png`: the district.
- `briefs/061/crossings-civic.png`: the civic block and its approaches.

Made by `bun briefs/061/datum/walks.ts && bun briefs/061/datum/make.ts`. The numbers are in `briefs/061/datum/counts.json`.

### Count: 124 → 26

Today's export (`out/world/harbour-district.world.json`, 2026-10-24 14:47) has 124 crossings. The ~109 in brief 009 was the proposal's own count before the arms and kerb-return work.

| rule today | today | after | what happens |
|---|---|---|---|
| plot-edge ends (both ends of every district and access frontage) | 66 (36 access, 30 district) | 0 | rule off. 9 of these sites stay under the new rules below |
| tram stop, both platform ends | 20 | 10 | one end per stop |
| civic block's own (Vantage) | 8 | 2 | the Office axis, plus Starline east where Garden Walk lands |
| primary arms at junctions | 11 | 0 | rule off: the T2 stops are the primaries' crossings |
| Crown Circus island | 7 | 4 | one per road into the circus |
| Parade at Harbour Road (mall) | 2 | 1 | folded into `foot` |
| mid-block, district roads | 2 | 0 | rule off |
| stair-foot patches at station banks | 8 | 0 | rule off: none crosses a road (4 stand on the Parade or pier), and the station's bridge is the crossing |

| road class | today | after |
|---|---|---|
| primary (ring roads) | 26 | 7 (6 T2 stops, the Parade) |
| district | 39 | 12 (4 tram stops, 6 lane crossroads, 1 district junction, Garden Walk at Circus Road) |
| access lanes | 44 | 3 (Office axis, Garden Walk at Lamplight Lane and at Starline Street) |
| circus ring | 7 | 4 |
| stair-foot patches (no road) | 8 | 0 |

**After, by rule:**
- stops 10
- crossroads 6
- foot 4
- island 4
- junction 1
- civic 1

### The rule change (plan, `standards.streetscape.crossings.coverage.rules`)

```json
"rules": {
  "reach": 4,
  "edges": false,
  "stops": { "ends": 1, "toward": "junction" },
  "banks": false,
  "island": { "ring": "crown-circus", "per": "road", "side": "l" },
  "foot": { "kinds": ["mall", "walk"], "at": "axis" },
  "crossroads": { "class": ["district"], "arm": "before" },
  "junction": { "class": ["district"], "arm": "before", "stopWithin": 30 },
  "dedupe": { "mouth": 30, "bend": 20, "turn": 45 }
}
```

- **edges: false.** Plot edges give no crossing landings, on any road class. This is the rule behind 66 of the 124.
- **stops.** One crossing per tram stop, at the platform end nearer a junction with a district or access road, or nearer a footway's end. That is the end people walk in from. A corner where two primaries meet brings no one.
- **banks: false.** No patch at a station's stair foot.
- **island.** One crossing per road into Crown Circus, on its left footway line seen leaving the circus. That gives four radials: Boulevard, Crown Avenue, Circus Road, the Parade. It replaces 2 per mouth.
- **foot.** A mall or walk that meets or crosses a road continues across it with one crossing, on its axis or at the nearest free spot. This covers Garden Walk at Circus Road, Lamplight Lane and Starline Street (the station-to-café line), and the Parade at Harbour Road. It replaces `mall`.
- **crossroads.** Where access lanes meet a district road from both sides at one node, the district road gets one crossing, on the arm before the node. These are Rocket, Satellite, Dock W and Dock E on Market Street, Lamplight E on Circus Road, and Terrace on Crown Avenue.
- **junction.** Where two district roads meet, each road gets one crossing on its arm before the node, unless a tram stop's crossing of that road stands within 30 m. Today that is only Market Street at the Boulevard.
- **Removed:** `arms`, `mall` and `midBlock`. In `where`, these lines go: "every arm of a district or access junction", "both ends of every tram stop platform", "the foot of every station street bank", "every gate" and "mid-block on district roads". A gate is marked by its gate, not by a crossing.
- **Landing rule unchanged.** Every kerb crossing's near edge stands at least `kerbReturn.landings.clear` (3 m) past the kerb return's tangent point. The kept sites are where the generator puts them today, which already applies this. The civic block's corners keep `civic` as now.
- **Measurement caveat.** The sites were measured where today's generator puts them. Garden Walk's crossings stand 9 m east of the walk's axis, and `foot` would put them on the axis. That changes no number above by more than a few metres of detour.

### What's lost (desire lines that now walk > 30 m to a crossing, or cross unmarked)

Method: brief 009's desire lines on today's export. That is the 3 civic doors, 20 tram platforms and 4 stations (a station = its nearer stair bank), 245 walks and 687 road crossings. A walk crossing counts as served when a crossing of that road adds ≤ 30 m.

| roads | served today | served after |
|---|---|---|
| busy roads (primary, district, circus) | 318 of 336 (95 %) | 236 (70 %) |
| access lanes | 313 of 351 (89 %) | 90 (26 %) |

Today 64 of the 124 crossings serve no walk. After the change, 3 of 26 serve none.

The losses fall into four groups:
- **Lane mouths: 11 places, 156 walk crossings.** These are walks along a district road's pavement that step across a lane's mouth at a gate: Satellite Street and Lantern Lane at Market Street (72), Harbour Lane and Dock Road at Market Street (56), Rocket/Lantern at Market Street (12), Observatory and Lamplight at Circus Road (16). None is crossing a busy road (see question 1).
- **Inside the Records Precinct lanes: 6 places, 66 walk crossings.** These cross unmarked on quiet lanes:
  - Apron Road south of the Office (28; café to the T1 Comet and Records stops)
  - Satellite Street at Starline (14; Official entrance to T1 Circus and T2 Harbour)
  - Orbit Walk east of the axis (14; 8 → 41 m)
  - Rocket Lane (4), Lantern Lane south (4) and Lamplight Lane (2)
- **Busy roads, still within 60 m: 3 places, 31.**
  - Sunset Way at the T2 Rocket stop (20 walks, 16 → 44 m). The junction-end rule picks the south end, but these walks come in from the north (from the Records Precinct side).
  - Circus Road west of Garden Walk (9 walks, 11 → 32 m).
  - The Boulevard at T1 Records (2 walks, 9 → 46 m).
- **Busy roads, now long: 4 places, 9 walk crossings.**
  - Sunset Way at Market Street (5 walks, 8 → 188 m)
  - Market Street at Sunset Way (2 walks, 17 → 152 m)
  - Sunset Way at Circus Road (1 walk) and at Comet Row (1 walk)
  - These are the primary arms going. Each is a walk round the district's edge.

### Questions for Sky

1. **Lane mouths at the gates.** In a real city, the 156 walk crossings at lane mouths (the 14 gates) cross on a continuous footway: the pavement runs on across the lane's mouth at kerb height and cars give way. That would be the gate's threshold (Buchanan's "entrance to an environmental area"), drawn by Vela as pavement, not as a crossing. **My pick: yes.** It adds no crossings, and the gates read as gates.
2. **Tram stops, one end or both?** One end gives 26 crossings in total. Both ends gives 36, and wins back Sunset Way at the Rocket stop (16 m today, 44 m with one end). **My pick: one end.** The platform's own end is the crossing, and the stop still reads.

### Handoffs once Sky approves
- **Splice** (generator, `tools/world/gen/street/coverage.ts`, `roads.ts`, `crossings.ts`): build the rules above after the rounded-corner work lands.
  - Inputs: this section and `briefs/061/datum/make.ts`, which is the same rules over the export.
  - Done-check: the plan checker exits 0, the export has 26 crossings (or 36 if Sky picks both ends), and every kept kerb landing passes `kerbReturn.landings`.
  - With `edges` off, the civic block's partner landings (`dressing.crossings.at[].partner.kerb`) must still be placed.
- **Vantage** (civic block dressing): keep `x-orbit-axis-n/s` and `x-starline-e`. Drop `x-starline-w`, `x-rocket-n`, `x-garden-n`, `x-garden-s`, `x-pylon-n` and `x-pylon-s`, each with a reason in `civic-block.adjust.json`. The Handover mission and its routes name no crossing.
- **Waymark:** gates lose their crossing. Confirm that no seal or route reads one.
- **Pulse:** fewer kerb signals across tram track (on the T2 primaries only the Parade's stays). Check the timetables if trams stop at crossing signals.

## Sky's calls (2026-10-25)
Sky, 2026-10-25, on the outstanding-work summary that listed each pick: "Your recommendation seem good".
Look (Vela's questions): 1 remove all three bits of edge brass, **yes** · 2 signal head **A** · 4 numerals and the paving ring, plus ARRIVING, **yes** · 5 **look only** for now (trams and people don't obey it) · 6 drop the inlay arcs, studs and lit field, **yes** · 7 lane mouths as a continuous footway, **yes**. Not answered: 3 (mock-ups or painted concepts; the 3D mock-ups stand) and 8 (edge-on reading in third person; judged in play).
Count (Datum's questions): 1 lane mouths at the gates cross on a continuous footway, **yes** · 2 tram stops crossed at **one end** (26 crossings).
Next: Datum (plan coverage and keys), Vela (pieces), Splice (rules SF-20/SF-21, crossing-parts, world export), then Latch.
