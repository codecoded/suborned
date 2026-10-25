"""The brief 061 piece sheet (Vela, 2026-10-25): composes lens061_board.py's frames (road_board_post.py frame) and its tris.json into one picture beside the 061 boards.

python3 -I tools/decokit/lens061_sheet.py <frames_dir> <out.png> [--sha <build1.sha> <build2.sha>] [--manifest <decokit_manifest.json>]
Pillow only. Frames needed in <frames_dir>: face-{walk,clearing,stop,arriving}.png, edge-{walk,stop}.png, play-{walk,clearing,stop,arriving}.png, close-clearing.png, recut-{before,after}.png, tris.json."""

import json
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

A = sys.argv[1:]
F, OUT = Path(A[0]), Path(A[1])
BG, FG, DIM, TQ, LINE = (19, 44, 43), (236, 240, 232), (176, 205, 199), (96, 224, 234), (52, 88, 86)
W, M, GAP = 2400, 24, 14


def font(px):
    for p in ("/System/Library/Fonts/Supplemental/Arial Bold.ttf", "/System/Library/Fonts/Helvetica.ttc"):
        if Path(p).exists():
            return ImageFont.truetype(p, px)
    return ImageFont.load_default()


def wrap(d, text, f, maxw):
    out, line = [], ""
    for w in text.split(" "):
        t = (line + " " + w).strip()
        if d.textlength(t, font=f) <= maxw:
            line = t
        else:
            out.append(line)
            line = w
    return out + [line]


def label(im, text, size=20):
    d = ImageDraw.Draw(im)
    tw = d.textlength(text, font=font(size))
    d.rectangle([4, 4, 14 + tw + 8, 12 + size + 6], fill=(11, 22, 21))
    d.text((12, 10), text, font=font(size), fill=(255, 255, 255))
    return im


def panel(path, w, h=None, crop=None):
    im = Image.open(F / path).convert("RGB")
    if crop:
        im = im.crop(crop)
    if h:
        im = im.resize((w, h), Image.LANCZOS)
    else:
        im = im.resize((w, int(im.height * w / im.width)), Image.LANCZOS)
    return im


probe = ImageDraw.Draw(Image.new("RGB", (10, 10)))
blocks = []   # (kind, payload, height)


def text(lines, size, colour, gap=7):
    ls = []
    for t in lines:
        ls += wrap(probe, t, font(size), W - 2 * M)
    blocks.append(("text", (ls, size, colour, gap), len(ls) * (size + gap) + 4))


def row(panels, title=None, note=None):
    if title:
        text([title], 24, FG)
    if note:
        text([note], 18, DIM)
    n = len(panels)
    pw = (W - 2 * M - GAP * (n - 1)) // n
    ims = []
    for path, lab, kw in panels:
        im = panel(path, pw, kw.get("h"), kw.get("crop"))
        ims.append(label(im, lab) if lab else im)
    blocks.append(("row", ims, max(i.height for i in ims) + 10))


def table(head, rows, widths, title, note=None, size=19):
    if title:
        text([title], 24, FG)
    if note:
        text([note], 18, DIM)
    blocks.append(("table", (head, rows, widths, size), (len(rows) + 1) * (size + 14) + 14))


# ---------------------------------------------------------------------------------------------------- content
TR = json.loads((F / "tris.json").read_text())
text(["Pick A, the pieces built (brief 061, Vela, 2026-10-25)"], 34, FG)
text(["Sky 2026-10-25: pick A, numerals and the paving ring plus ARRIVING, look only, no brass on the road edge, drop the arcs, studs and lit field. These are the generator's own pieces (tools/decokit: crossing_lens.py, crossing_flat.py, "
      "pave026.py, road_ribbon.py), not mock-ups: every frame is the built object, driven the way Latch's SignalHead will drive it (emission colour, atlas offset, ring cutoff, the dots' u gate)."], 20, DIM)

row([("face-walk.png", "WALK", {}), ("face-clearing.png", "CLEARING, 7 s left", {}), ("face-stop.png", "STOP", {}), ("face-arriving.png", "ARRIVING, 3", {})],
    "The four states on the head: SignalLens face A, 6.3 m, fov 13",
    "Colour, figure and numerals each differ, so the state never rests on colour alone: WALK is the walking figure, CLEARING the small walking figure over the seconds left, STOP the standing figure, ARRIVING the small standing figure over 3, 2, 1 with the rim blinking. "
    "WALK and STOP differ by the figure, CLEARING and ARRIVING by the figure, STOP and ARRIVING by the numerals and the rim (checked in crossing_lens.py: the build stops if two states differ by colour alone).")

row([("edge-walk.png", "edge-on, WALK", {}), ("edge-stop.png", "edge-on, STOP", {}), ("close-clearing.png", "CLEARING, tight: a dot row draining from the kerb, 17 of 24 ring segments left", {"h": 640})],
    "Edge-on, and close at the kerb",
    "Seen edge-on (third person, the free camera) the lit rim still says the state; the beam and the ring in the paving carry it down. The dot rows lose their lit dots from both kerbs toward the middle, and a dark dot keeps its charcoal enamel seat. (Open question 8 is judged in play.)")

row([("play-walk.png", "WALK", {"crop": (560, 220, 1260, 640)}), ("play-clearing.png", "CLEARING", {"crop": (560, 220, 1260, 640)}),
     ("play-stop.png", "STOP", {"crop": (560, 220, 1260, 640)}), ("play-arriving.png", "ARRIVING", {"crop": (560, 220, 1260, 640)})],
    "At the play camera (zoom 14, pitch 38, yaw 30; a 13 m carriageway, one head and one ring at each kerb, two dot rows)",
    "Crops of 1920 x 1080 frames at the game's scale (38.6 px/m): the lens is about 34 px, the ring in the paving and the dots carry the state when the face is too small to read. Day, no shadow contact or grade beyond the board's ACES and bloom; Glint trims the emission.")

row([("recut-before.png", "before: the committed pieces", {}), ("recut-after.png", "after: re-cut, no brass", {})],
    "The road edge and the dot rows, before and after (the far kerb at the play camera, 2.8 m half-height)",
    "Before: brass crown line on Kerb_Pearl, two brass hairlines either side of the ribbon lens, brass rings and two hairlines on the dot rows, the old orb with brass bezels. After: the pearl kerb, the strip-light lens alone, light dots on charcoal enamel seats, the lens with its lit rim and the clock ring in the paving. "
    "The pavement pinstripe is a plan key (Datum), not drawn in either frame.")

fmt = lambda lv, t: str(lv[t]["tris"]) if t in lv else "-"  # noqa: E731
rows = []
for n, old, note in (("SignalLens", "SignalEye", "650 of 700 L0, 234 of 250 L1; 6 parts + body"), ("HaloRing", None, "one quad, two triangles"),
                     ("LightDotRow_2", "LightDotRow_2_Old", "seat + light per dot, no brass; UV x = metres along"), ("LightDotRow_1", "LightDotRow_1_Old", "1 m end module"),
                     ("Kerb_Pearl_0", "Kerb_Pearl_0_Old", "the 12-triangle brass crown box dropped"), ("Kerb_Pearl_1", "Kerb_Pearl_1_Old", "same"),
                     ("RoadRibbon_Straight_6m", "RoadRibbon_Straight_6m_Old", "the two brass hairlines dropped; pool starts at the lens edge"),
                     ("RoadRibbon_Arc90_R4", "RoadRibbon_Arc90_R4_Old", "same"), ("RoadRibbon_Arc90_R8", "RoadRibbon_Arc90_R8_Old", "same")):
    a, b = TR[n], (TR[old] if old else {})
    rows.append([n, (old.replace("_Old", "") + (" (replaced)" if old == "SignalEye" else "") if old else "new"), f"{fmt(b, 'L0')} / {fmt(b, 'L1')}" if old else "-", f"{fmt(a, 'L0')} / {fmt(a, 'L1')}", note])
table(["piece", "was", "before L0 / L1", "after L0 / L1", "what changed"], rows, [330, 330, 250, 250, 1160],
      "Measured triangles (built objects, evaluated; python3 + Blender, tris.json)", "SignalLens by part, L0: " + ", ".join(f"{k.replace('DK_SignalLens', 'body').replace('body_', '')} {v['tris']}" for k, v in TR["SignalLens"]["L0"]["objects"].items())
      + ". L1 parts: " + ", ".join(f"{k.replace('DK_SignalLens', 'body').replace('body_', '')} {v['tris']}" for k, v in TR["SignalLens"]["L1"]["objects"].items()) + ".")

man = {}
if "--manifest" in A:
    man = {a["name"]: a for a in json.loads(Path(A[A.index("--manifest") + 1]).read_text())["assets"]}
ret = [("SignalEye", "replaced by SignalLens + HaloRing"), ("KerbStud", "brass studs"), ("CrossEdgeLine_1", "brass edge line, 1 m"), ("CrossEdgeLine_2", "brass edge line, 2 m"),
       ("CrossInlayArc_R24_W08", "seafoam inlay arc"), ("LightField_1", "lit field, 1 m"), ("LightField_2", "lit field, 2 m")]
table(["marked for retirement", "why", "manifest `retired`"], [[n, w, "marked" if man.get(n, {}).get("retired") else ("-" if man else "(no manifest)")] for n, w in ret], [420, 900, 1000],
      "Marked for retirement (builders kept until Splice has re-cut the export)", None, 19)

foot = ["Frames: tools/decokit/lens061_board.py (Blender, Eevee, the light.json sun and ambient), road_board_post.py frame (ACES, bloom), composed by tools/decokit/lens061_sheet.py. Emission here is the data file's day value (walk 2.0, clearing 2.0, stop and arriving 1.8, linear); Glint trims them and the evening and night multipliers."]
if "--sha" in A:
    i = A.index("--sha")
    a, b = Path(A[i + 1]).read_text().splitlines(), Path(A[i + 2]).read_text().splitlines()
    foot.append(f"Determinism: `bun run decokit --only <pieces>` built twice into two folders: {len(a)} files (FBX, LOD, .mat, textures, data, manifest) with identical sha256: {'yes' if a == b else 'NO'}.")
text(foot, 17, DIM)

# ---------------------------------------------------------------------------------------------------- layout
H = M + sum(b[2] + 8 for b in blocks) + M
im = Image.new("RGB", (W, H), BG)
d = ImageDraw.Draw(im)
y = M
for kind, payload, h in blocks:
    if kind == "text":
        ls, size, col, gap = payload
        for ln in ls:
            d.text((M, y), ln, font=font(size), fill=col)
            y += size + gap
        y += 4
    elif kind == "row":
        x = M
        pw = (W - 2 * M - GAP * (len(payload) - 1)) // len(payload)
        for t in payload:
            im.paste(t, (x, y))
            x += pw + GAP
        y += h
    else:
        head, rows_, widths, size = payload
        x = M
        d.rectangle([M, y, W - M, y + (len(rows_) + 1) * (size + 14)], fill=(11, 22, 21))
        for r, rw in enumerate([head] + rows_):
            xx = M + 10
            for c, (cell, wd) in enumerate(zip(rw, widths)):
                d.text((xx, y + 7 + r * (size + 14)), cell, font=font(size), fill=TQ if r == 0 else FG if c == 0 else DIM)
                xx += wd
            d.line([M, y + (r + 1) * (size + 14), W - M, y + (r + 1) * (size + 14)], fill=LINE)
        y += h
    y += 8
OUT.parent.mkdir(parents=True, exist_ok=True)
im.save(OUT)
print(OUT, im.size)
