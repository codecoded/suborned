# The hover outline, look B (brief 101): built

Latch, 2026-11-10. Sky picked Glyph's B on 2026-11-09 (`briefs/101/glyph/outline/notes.md`). Built as Glyph's spec says.

**Build:** 0.2.301, `out/build/play/The Suborned.app`. It was built from the scene of 0.2.300, so Vela's ball (brief 103) is not in it.

## What it does
- **What gets the line:**
  - whatever a left click would act on: a person, the cell, a camera's head, a car, the door, the cabinet, a stash, a body;
  - a passer-by or a passing car only when the selection has a way on them.
- **How it follows the pointer:**
  - Tab, Z and the wheel step it through an overlap;
  - an open ring keeps it on the ring's thing;
  - it holds the pill's 0.15 s on open ground;
  - fades are 0.1 s in and out, cross-fading.
- **The line:**
  - ink #17302f 1.5 px at 92 %, then cream #f7f4ec 1 px at 85 %, outside the edge;
  - scaled by screen height / 1080, the same at every zoom;
  - drawn after post (exact kit colours) and under the HUD;
  - map view only: no line in third person, the book, the start page, a conversation, the fail camera or the end.
- **Cover:** something nearer hides the line. A silhouetted person keeps the line round the silhouette, and only another person in front hides it.
- **Stand-ins:**
  - the door: an arched panel, from `civic_records.py` (PR 1.65 m, landing 0.75 m, spring 7.6 m) in the RecordsEntrance piece's frame;
  - the cabinet: the block record's box;
  - a service door: its leaf (ServiceDoors' sizes);
  - a bin: its own mesh;
  - the mast: its crown and pods, cut at the band under the crown.
- **Removed:**
  - the cream ring at a person's feet when there's no pill;
  - the dashed rings in a stack;
  - in classic controls, the ground ring and the ellipse.
- **Kept:** the stack dots, the name chip and the pill.

## Pictures (play zoom 10, 1920 × 1080, with 2x crops; `briefs/101/latch/outline/`)
- **The guard, the Enforcer selected:** `r085-outline-guard.png`, `-2x.png` ("Hold · Watch the guard").
- **40 px beside him, the door:** `r085-outline-door.png`, `-2x.png`. The line runs behind the globe and its stand; on the dark glass only the cream edge shows ("Hold · Enter the Office").
- **The Fixer, the Scout selected:** `r085-outline-cell.png`, `-2x.png`.
- **The lamp mast:** `r085-outline-mast.png`, `-2x.png`. The crown and pods get the line, the post doesn't ("Hold · Watch the mast camera").
- **The getaway car (a scratch shot):** `outline-car.png`, `-2x.png`.

## Checks
- **Compile:** runtime-compile ok; Unity player build ok (0 errors).
- **Proof on 0.2.301 (all sections):**
  - every section ok but r083;
  - the new rows pass: `r085outline` (on the guard and 40 px beside him the line is on the thing the pill names, faded in, drawn that frame), `r085outlineCell` and `r085outlineMast` (the line's box 264 px tall against the mast's 602 px at 2880 × 1864);
  - the proof holds the pointer on a world point, so it needs no focus.
- **r083's other failing parts (none from this change):**
  - `r085doorPrice`: a real click, and the player lost focus mid-run;
  - `bookAgentRows`: the check samples route A's orders at ticks divisible by 3. Since the route's re-plan only 1050 qualifies, so "two ticks seen" fails. A sampling fix in ProofBook, not focus;
  - `r085listenApart`: the scene took the "high" shot, not "profile", for the Scout seated 4 m from Inez at café table 6. No clear profile angle there.
- **Shots and budget:** 13/13 shots; 11/11 shots within the HUD budget; the layout audit 11/11 clean; the proof sets r059 14/14, r067 32/32, r123 7/7, r083 2/2.
- **Frame time on the Studio (Mac17,14, 2880 × 1864, the Office front, 5 s each):**

  | pointer | mean | p99 | GPU |
  |---|---|---|---|
  | off | 4.35, 4.39 ms | 8.11, 8.41 ms | 3.61, 3.72 ms |
  | on the door (the biggest box) | 4.46, 4.30 ms | 8.40, 8.01 ms | 3.65, 3.59 ms |

  The outline's cost is inside the noise. Redline checks the Air.

## Files
- **New:**
  - `unity/Assets/Suborned/Runtime/HoverOutlinePass.cs` (145 lines);
  - `Runtime/Play/HostOutline.cs` (125);
  - `Runtime/Play/HostViews.Outline.cs` (193);
  - `Runtime/Play/Proof085Outline.cs` (104);
  - `Resources/Shaders/HoverOutline.shader` (173);
  - `Resources/Shaders/HoverOutlineStandIn.shader` (25).
- **Changed:**
  - `HudToolkit.Less.cs` (1075 → 1070: the hover ring and the classic ellipse out);
  - `HudToolkit.Hold.cs` (318 → 319: the dashed rings out, `PillOn` for the proof);
  - `Proof085.cs` (+1 line: the outline proof);
  - `tools/game/proofmap.json` (one row: the outline's files → r083 and `plate-pay-armed`).

## Open
- **A focused run:** r083's `r085doorPrice` needs a focused player. Re-run r083 alone when the Studio is free.
- **Two non-focus failures:** `bookAgentRows` (a sampling fix in ProofBook) and `r085listenApart` (shot framing: Glyph and Aster to say whether "high" is right for a seated, apart Listen).
- **The police:** both officers share one target key ("security"), so the line always goes on the first officer's figure, as TargetWorld does.
- **GDD:** the §10 line Glyph proposed is Aster's.
