"""The robot attention face (brief 031, Glyph's cue board section 4; Vela): the four stages of a watcher's attention drawn with the OLD city's dot-matrix glyphs.
python3 tools/decokit/robot_face_stage.py [out_dir] [--ascii]

Source of the shapes is art/robots/RobotGlyphs.txt, the old project's glyph table read verbatim (the same table robot_faces.py ships). Each stage is that table's own
eyes and mouths placed with the old RobotFace.Place rule on a 16 x 10 LED grid (Glyph's grid); nothing is redrawn except what the old table never had:

  stage        old expression                            added
  0 calm       Waiter neutral: `round` eyes + `smile`    -
  1 noticed    Porter surprise mood: `wide` eyes + `o`   -  (the turn is the old gaze: eyes jump 2 cells, `_Gaze`)
  2 wary       the old alarm eyes `angry_l` / `angry_r`  -  (lids slanting down toward the nose = brows down; no mouth, so it is not the old grit)
  3 reporting  (none: the old alarm was an angry glare)  `alarm`: Glyph's 12 x 10 warning triangle with a bar, the one new glyph (the old table is 8 x 8 and has no pictogram)
  blink        the old lid: eyes squeeze to the middle rows, then one bar (40 ms half, 70 ms bar, 40 ms half), the mouth stays

Kept out on purpose (the old table's, section 15 "no cartoon eyes"): heart, star, the wink.

Atlas robot_face_stage_v1.png: 16 x 60, RG8 (point filtered, no mips, linear). R = the eyes (the shader shifts them with _Gaze), G = everything else (mouth, pictogram).
Rows from the top, 10 a slice: 0 calm, 1 noticed, 2 wary, 3 reporting (Glyph's 16 x 40 in the same order), then 4 calm blink half, 5 calm blink shut."""

import json
import sys
from pathlib import Path

HERE = Path(__file__).parent
GLYPHS = HERE / "art/robots/RobotGlyphs.txt"
W, H = 16, 10
EYE_AT = (3.5, 2.0)    # the viewer's-right eye centre from the grid centre, cells (old rig table: eye = [x, y], left is the mirror)
MOUTH_AT = (0.0, -3.2)
ADDED = {  # glyphs the old table lacked. Glyph's cue board bitmap, verbatim (briefs/031/cues.html BM.alarm); 16 wide, 10 tall, so placed whole
    "alarm": ["................", ".......##.......", "......####......", "......#..#......", ".....#.##.#.....", ".....#.##.#.....",
              "....#..##..#....", "....#..##..#....", "...#...##...#...", "...##########..."],
}
# stage -> (left eye, right eye, mouth)
STAGES = [("calm", "round", "round", "smile"), ("noticed", "wide", "wide", "o"), ("wary", "angry_l", "angry_r", ""), ("reporting", "", "", "alarm")]
OLD_MAP = {"calm": "Waiter neutral (round + smile)", "noticed": "Porter surprise (wide + o)", "wary": "alarm eyes (angry_l / angry_r), mouth dark",
           "reporting": "new pictogram: the old alarm was an angry glare, kept as wary's eyes"}
HEX = {"phosphor": "#BFF3E6", "alarm": "#FF5A3C"}
HDR = {"phosphor": 1.6, "alarm": 2.2}
TIMING = dict(blinkEvery=[2.5, 5.5], blinkHalf=.04, blinkShut=.07, gazeCells=2, noticedSnap=.2, waryHeadSwayDeg=7, waryHeadSwayS=1.8, reportingOn=.3, reportingOff=.2,
              dropAfterS=2.0, note="blink and gaze are the old RobotFace.cs numbers; reporting is Glyph's 2 Hz; the sim is untouched, the display delay is Latch's")


def parse(text):
    glyphs, cur = {}, None
    for raw in text.split("\n"):
        line = raw.strip()
        if line.startswith("="):
            cur = line[1:].strip()
            glyphs[cur] = []
        elif line and not line.startswith(";") and cur:
            glyphs[cur].append(line)
    return glyphs


OLD = parse(GLYPHS.read_text())


def glyph(name):
    return ADDED.get(name) or OLD[name]


def place(rows, at):
    """Origin cell (x, y-from-bottom) of a glyph centred `at` cells from the grid centre: the old RobotFace.Place; at.x < 0 is the mirror, cell for cell."""
    w, h = max(len(r) for r in rows), len(rows)
    x = int((W * .5 + abs(at[0]) - w * .5 + .5) // 1)
    if at[0] < 0:
        x = W - w - x
    return x, int((H * .5 + at[1] - h * .5 + .5) // 1)


def stamp(grid, name, at, lid=0):
    """Light the glyph's cells in grid[row from top][col]. lid: 0 open, 1 half, 2 shut to a bar (the old shader's Glyph(): half keeps the middle rows)."""
    if not name:
        return
    rows = glyph(name)
    if name in ADDED:
        x0, y0 = 0, 0  # the pictogram fills the grid
    else:
        x0, y0 = place(rows, at)
    h = len(rows)
    mid = (h - 1) * .5
    for ry, row in enumerate(rows):
        yb = h - 1 - ry                                  # row from the glyph's bottom
        if lid == 1 and abs(yb - mid) > max((mid + .5) * .5, .5):
            continue
        if lid == 2:
            if yb != int(mid // 1):
                continue
            row = "".join("#" if any(r[i] == "#" for r in rows if i < len(r)) else "." for i in range(len(row)))
        for cx, ch in enumerate(row):
            if ch == "#":
                gx, gy = x0 + cx, H - 1 - (y0 + yb)
                if 0 <= gx < W and 0 <= gy < H:
                    grid[gy][gx] = 1


def grids(stage, lid=0, gaze=0):
    _, l, r, m = STAGES[stage]
    eyes = [[0] * W for _ in range(H)]
    rest = [[0] * W for _ in range(H)]
    stamp(eyes, l, (-EYE_AT[0], EYE_AT[1]), lid=lid)  # mirrored position only: the old shader draws the glyph as written, angry_l is already the left one
    stamp(eyes, r, EYE_AT, lid=lid)
    stamp(rest, m, MOUTH_AT)
    if gaze:
        eyes = [[row[x - gaze] if 0 <= x - gaze < W else 0 for x in range(W)] for row in eyes]
    return eyes, rest


def lit(stage, lid=0, gaze=0):
    e, r = grids(stage, lid, gaze)
    return [[1 if e[y][x] or r[y][x] else 0 for x in range(W)] for y in range(H)]


def ascii_of(g):
    return "\n".join("".join("#" if c else "." for c in row) for row in g)


SLICES = [(s, 0, 0) for s in range(4)] + [(0, 1, 0), (0, 2, 0)]  # stage, lid, gaze for each atlas slice


def atlas():
    import numpy as np
    img = np.zeros((H * len(SLICES), W, 2), np.uint8)
    for i, (s, lid, gz) in enumerate(SLICES):
        e, r = grids(s, lid, gz)
        for y in range(H):
            for x in range(W):
                img[i * H + y, x, 0] = 255 if e[y][x] else 0
                img[i * H + y, x, 1] = 255 if r[y][x] else 0
    return img


def dots(g, px=12, col="#BFF3E6", bg="#0D1F1D", ghost=.05):
    """A render of a lit grid as round LEDs (the old shader's dot, radius .36 of a cell, unlit cells at the ghost level)."""
    from PIL import Image, ImageDraw
    k = 4
    im = Image.new("RGB", (W * px * k, H * px * k), bg)
    d = ImageDraw.Draw(im)
    c = tuple(int(col[i:i + 2], 16) for i in (1, 3, 5))
    b = tuple(int(bg[i:i + 2], 16) for i in (1, 3, 5))
    gc = tuple(int(b[j] + (c[j] - b[j]) * ghost) for j in range(3))
    r = .36 * px * k
    for y in range(H):
        for x in range(W):
            cx, cy = (x + .5) * px * k, (y + .5) * px * k
            d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=c if g[y][x] else gc)
    return im.resize((W * px, H * px), Image.LANCZOS)


def build(out):
    import numpy as np
    from PIL import Image
    sys.path.insert(0, str(HERE))
    from ground_tex import meta_text
    out = Path(out)
    out.mkdir(parents=True, exist_ok=True)
    img = atlas()
    name = "robot_face_stage_v1.png"
    Image.fromarray(np.dstack([img, np.zeros(img.shape[:2], np.uint8)]), "RGB").save(out / name, optimize=True)
    meta = meta_text(name, "m", (W, H * len(SLICES)))
    meta = meta.replace("enableMipMap: 1", "enableMipMap: 0").replace("sRGBTexture: 0", "sRGBTexture: 0")
    (out / f"{name}.meta").write_text(meta)
    data = dict(schema="suborned.robotfacestage/1", piece="PatrolRobot", material="M_RobotFace", property="_Stage", grid=[W, H],
                atlas=dict(file=name, size=[W, H * len(SLICES)], channels="R eyes (shifted by _Gaze), G mouth and pictogram", filter="point", mips=False, linear=True,
                           slicesFromTop=["calm", "noticed", "wary", "reporting", "calm blink half", "calm blink shut"], sliceRows=H),
                colour={k: dict(hex=HEX[k], hdr=HDR[k]) for k in HEX}, stages={STAGES[i][0]: dict(stage=i, eyes=[STAGES[i][1], STAGES[i][2]], mouth=STAGES[i][3], fromOld=OLD_MAP[STAGES[i][0]],
                                                                                                  colour="alarm" if i == 3 else "phosphor", bitmap=ascii_of(lit(i)).split("\n")) for i in range(4)},
                properties={"_Stage": "0..3, per robot (property block)", "_Gaze": "-1, 0, +1: eyes jump toward the agent by timing.gazeCells (stage 1 and 2)",
                            "_Blink": "0 open, 1 half, 2 bar: stage 0 only, driven by the shader from time (old schedule), or by script"},
                timing=TIMING, oldGlyphsUsed=sorted({g for s in STAGES for g in s[1:] if g in OLD}), glyphsAdded=sorted(ADDED), glyphsHeld=["heart", "star", "wink"])
    (out / "robot-face-stage.json").write_text(json.dumps(data, indent=1) + "\n")
    return data


if __name__ == "__main__":
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    if "--ascii" in sys.argv:
        for i in range(4):
            print(f"-- {STAGES[i][0]}\n{ascii_of(lit(i))}\n")
        for lid in (1, 2):
            print(f"-- blink {lid}\n{ascii_of(lit(0, lid))}\n")
        print("-- noticed, gaze +2\n" + ascii_of(lit(1, 0, 2)))
        sys.exit(0)
    import os
    out = args[0] if args else os.environ.get("DECOKIT_GROUND") or HERE.parents[1] / "out/decokit-ground"
    d = build(out)
    print(f"[robot-face-stage] {len(SLICES)} slices -> {out}/robot_face_stage_v1.png, robot-face-stage.json")
