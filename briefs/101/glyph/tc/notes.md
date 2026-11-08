# Take control: a click for the ground view (brief 101)

Glyph, 2026-11-09. Open `board.png` first (1000 × 4410, for a phone).

This is a mock only. Nothing changed under `unity/Assets`, in the GDD or in `text.json`. Nothing was committed, and no paid or Codex call was made ($0). The pictures are real frames with the new pieces drawn in the built street kit (`Kit/street.uss`, the built `Tab`, rounds 2 and 3's rules):
- `out/scratch/latch/101r3/shots-288/t101-quiet-2x`: 0.2.288, captured at 2x;
- `out/build/play/proof/hold-tp-keeps-walk`: 0.2.289.

## My call: a button

- The ground view already plays in every build, and T is the only way in. `FollowKey` has no lab gate, and the old pop advertised it. That makes it a key-only feature in play.
- The other way to satisfy Sky's rule is to gate T behind the lab switch. That takes away a view Sky uses (briefs 083 and 096 are full of Sky's notes on it).
- The button costs about a day for Latch, including its proofs. On screen it adds 0.05 % at rest, 0.12 % hovered and 0.14 % in third person.
- Ship the eye with it (see 3). Without it, you can't tell from the street whether anyone sees you.
- Giving it a button puts the ground view in the game. That's Sky's call and a new GDD version, since §10 and §11 still mark the view [lab].

## The design: one slot beside the selected face

| state | what shows |
|---|---|
| at rest, on the map | A 30 px round tab (the built `Tab`) right of the selected agent's pips, level with her face. It shows beside the agent T would take (`FollowWho`) and hides while she's down, inside or in a car. |
| hover | The ring turns turquoise (the built `Tab`'s hover). Fable's "Take control" appears as a street label, with "· T" after it in soft ink. Pointing at any other face shows that face's own tab, and one click takes control of that agent. |
| in control | The same slot becomes a capsule (rule 2: it has a word), holding the map and Fable's "Back to map". The Squad tab stays Squad in both views (`SelectAll` already keeps the driven agent). Over her head is one keyed eye. The selection chevron goes while she's driven. Her act stays on her face's disc. |
| the way back | Click the capsule: the 0.5 s glide back to the map (built), centred on her at play zoom and the yaw you left, with her order still running. T does the same. So does Esc once nothing else is open, which changes brief 041's "Esc stays the pause menu": Sky's yes needed. |

Why beside the face, not on it:
- All four corners of a face are taken: the known badge, the watcher's eye, the sidearm and the act disc.
- Making it the Squad slot again would keep one tab meaning two things. That's today's "Back to map" on the units icon.
- The scope's rim holds the view controls, but the scope already takes clicks and drags, and it doesn't say who you'd take.

**The eye** is GDD §10's "who sees you", which isn't built yet:
- **shut** (ink): an act or run here is unseen;
- **open**, in the cones' red: it would be seen;
- **?** (ink): a watcher the cell hasn't learned could see it.

It's drawn keyed, 24 px, where the chevron was. Its data is the "seen" read the ring's price rows already show, taken for where she stands, at most twice a second, like the roster's run eye (`HostLess.RunEye`). The frame shows "?" as illustration (0:01, nothing learned yet).

## Pictograms

`icons/take_control@2x.png` and `icons/map@2x.png` are 128 px, white on clear, like the kit's singles. `make/tc_icons.py` redraws them exactly.

The PNGs are git-ignored (`briefs/**/*.png`). Latch copies them into `Kit/icons/singles/` from disk, or Aster adds them to the keep list.

Five drawings were dropped on the 1x proof (`out/scratch/glyph/101tc/icons/variants*.png`):
- a chevron over a bust (it read as a medal);
- an arrow over a bust (a cross);
- a swoop (muddled);
- a bust in its ring (it read as "selected");
- an arrow or a triangle into the ring (both too close to the kit's `drop`, "Put down", which the act disc can show right beside the tab).

## For Latch, when Sky says yes

| # | piece | files | size |
|---|---|---|---|
| 1 | The tab beside the face: when it shows, its click (`TakeInSquad` + `DriveEnter`, as T), its hover word (`actions.drive.label` and the key from the bindings table). The face's label row starts after it. | `HudToolkit.Faces.cs`, `HostDrive.cs` | S |
| 2 | In third person the slot is the capsule (`BackToMap`; word: `actions.drive.ways.release`). The Squad tab's click is `SelectAll` in both views, and its third-person word goes. | `HudToolkit.Faces.cs`, `HudToolkit.Hold.cs` (lines 183, 187) | S |
| 3 | Esc in third person: after what's open closes, back to the map; the pause menu stays on the map. One branch before `OpenMenu`. | `HandoverHost.cs` line 443 | XS |
| 4 | The two pictograms into `Kit/icons/singles/` | Kit | XS |
| 5 | The eye over the driven agent; no chevron while driven | `HostLess.cs`, the HUD marks | S |
| 6 | Proof083's `backToMapButton` reads the capsule, not `SquadBtnScreen`. A new row checks that the tab takes control of that face's agent with nothing cancelled. Manifest names: "take control tab", "back to map capsule". | `Proof083.cs`, `HudToolkit.Capture.cs` | S |

Run the delta only: r083, r085, drive, holdActs (the `hold-tp-*` frames) and the shots. Fable has no new words to write; `take-control` ("Take control: T") retires.

## What the lab view still lacks for the real game

- **Who sees you, beyond the eye:** pips over known watchers within 30 m, an edge marker for one off screen, and the learned cone as a vertical curtain (§10). T14 also asks for a "goal beam", which nothing specifies. That needs a spec (Glyph with Waymark) before T14 can run.
- **Balance:** §10 says the map is the only view the game is balanced in. Driving keeps the paces and 1× speed, so prices hold. But no one has played a plan walked from the street, and T14 has never run.
- **Frame rate:** the bar is 60 fps on the Air at street level too. The last street-level readings are the Studio's 223–227 fps, GPU 3.9 ms (`out/scratch/latch/098j`, `098t` proofs). There's no recent Air reading at street level.
- **The ring at her feet:** up close the cell ring is about 220 px across. It's world art, so the HUD budget doesn't see it. It could thin while she's driven (optional; my look).

## Evidence

- **The project's reader on the mocks:** `python3 -I make/tc_budget.py` writes each frame's HUD layer and manifest and runs `bun tools/hud-budget/cli.ts out/scratch/glyph/101tc/budget`. Result: **4/4 within budget** (`make/budget.txt`).
- **Today's two frames through the same reader:** 2/2 within budget.

| frame | today | mocked |
|---|---|---|
| rest (0.2.288) | 3.70 % | 3.75 % |
| hover (0.2.288) | 3.70 % | 3.82 % |
| control (0.2.289) | 2.44 % | 2.58 % |
| back, hovered (0.2.289) | 2.44 % | 2.61 % |

- **The 2x crop check** (`out/scratch/glyph/101tc/*-2x.png`). I fixed these:
  - The first in-control frame (`drive-e-inez`) lifted badly: smears where the act mark and the Squad tab were. I swapped it for the walking frame. The old "Back to map · T" word there is recovered from the capture's own HUD layer, and only its opaque strokes are inpainted, at alpha 0.6 or more (at 0.85 a pink speck showed).
  - The chevron is filled from the paving 40 px to its right.
  - The Tech's sidearm mark was a copied square that carried the Scout's ring. It's now drawn as built.
- **Files:** all under 500 lines; the largest is `make/tc_frames.py` at 151.
