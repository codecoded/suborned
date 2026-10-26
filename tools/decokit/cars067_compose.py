"""Brief 067 (Vela): the boards for the rival sedan. PIL and numpy only (no Blender): lays the plates tools/decokit/cars067_plates.py rendered onto the street, labels them in the HUD's type,
and writes the pictures. Today's stand-in is Latch's (Sedan_Official x RivalTrim, one flat colour as HostViews.Rivals.cs writes it), the proposal is Sedan_Rival, beside Vale's official car
and the cell's getaway car.

python3 -I tools/decokit/cars067_compose.py --plates out/scratch/vela/067/final --out briefs/067/vela [--boards play30,play210,street,marks,close,eye,shape,two,hero] [--frame PATH]
  play30 / play210  true scale (1 px = 1 px of a 1080p play frame, 38.57 px/m), four cars by four headings of the nose, yaw 30 and 210
  street            the proposal and the stand-in laid into the game's own frame (briefs/041/build/0311/proof-1080/overhead-pickup.png: Vale's car at the bay, the road, the cell), 1 : 1
  marks             the same street with the rivals' violet marks drawn over the car as Glyph's 059 spec has them (approach line with diamond ticks, the rival ring): the body does not swallow them
  close             the fitted play angle (38 degrees) close, front three-quarter and rear three-quarter
  eye               street level (1.5 m, 38 mm), front and rear three-quarter
  shape             plan silhouettes and the value read: the cars as flat shapes, as grey, and squinted (a third of the size)
  hero              Sedan_Rival alone, large, front and rear three-quarter
  two               today's stand-in two ways: as Trim() runs (one flat colour) and as its comment means (each slot x RivalTrim)
Colours: road #a4b1b6 and pavement #ddc4aa as the game draws them (sampled from its frames); ink #17302f and cream #f7f4ec from the HUD kit; signal violet #8a4fd0 (style contract); type Jost Bold."""

import argparse
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[1]
sys.path.insert(0, str(HERE))
from deltae import lab  # noqa: E402

ROAD, INK, CREAM, DIM, VIOLET, INKDEEP = (164, 177, 182), (23, 48, 47), (247, 244, 236), (70, 92, 90), (138, 79, 208), (11, 22, 21)
FONT = next((p for p in (REPO / "out/concepts/ui-011-kit/fonts/Jost-Bold.ttf", Path("/System/Library/Fonts/Supplemental/Arial Bold.ttf"), Path("/System/Library/Fonts/Helvetica.ttc")) if p.exists()), None)
PPM, SIN = 38.57, math.sin(math.radians(38))
IDS = ["Sedan_Official", "Sedan_Getaway", "StandIn", "Sedan_Rival"]
NAME = {"Sedan_Official": "Vale's official car", "Sedan_Getaway": "the cell's getaway car", "StandIn": "today: the rival's stand-in", "Sedan_Rival": "proposed: Sedan_Rival"}
SUB = {"Sedan_Official": "deep teal, ivory roof", "Sedan_Getaway": "coral, seafoam roof", "StandIn": "the official car in one flat navy", "Sedan_Rival": "plum, ink-lacquer bubble, pearl stripes"}
PLUM = (72, 42, 90)
RIVAL_TRIM = (0.46, 0.43, 0.55)
# the catalogue's colours (decokit_materials.json / mats.py) as sRGB: body, roof
HEX = {"Sedan_Official": ("#1F6C67", "#EFE8D6"), "Sedan_Getaway": ("#FC9C84", "#72B5A7"), "Sedan_Rival": ("#482A5A", "#1B1723")}


def font(n):
    return ImageFont.truetype(str(FONT), n) if FONT else ImageFont.load_default()


def trim(h, m=RIVAL_TRIM):
    r, g, b = (int(h[i:i + 2], 16) for i in (1, 3, 5))
    return "#%02X%02X%02X" % (round(r * m[0]), round(g * m[1]), round(b * m[2]))


HEX["StandIn"] = (trim(HEX["Sedan_Official"][0]), trim(HEX["Sedan_Official"][0]))  # HostViews.Rivals.cs Trim(): the first slot's colour x RivalTrim, written to every slot of the one renderer


def grain(w, h, base=ROAD, sd=2.6, seed=7):
    """The road as the game draws it: flat powder grey-blue with a fine grain."""
    rng = np.random.default_rng(seed)
    a = np.empty((h, w, 3), np.float32)
    a[:] = base
    a += rng.normal(0, sd, (h, w, 1)).astype(np.float32)
    return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), "RGB")


def plate(root, name, view, yaw=None, heading=None):
    stem = f"{name}.{view}" + (f".y{int(yaw)}" if yaw is not None else "") + (f".h{int(heading)}" if heading is not None else "")
    return Image.open(Path(root) / f"{stem}.png").convert("RGBA")


def drop(canvas, im, cx, cy):
    """Alpha-over of `im` centred on (cx, cy)."""
    canvas.paste(im, (int(cx - im.width / 2), int(cy - im.height / 2)), im)


def label(d, xy, s, n=22, fill=INK, anchor="la"):
    d.text(xy, s, font=font(n), fill=fill, anchor=anchor)


def header(w, title, sub, h=96):
    im = Image.new("RGB", (w, h), CREAM)
    d = ImageDraw.Draw(im)
    label(d, (24, 14), title, 30)
    label(d, (24, 54), sub, 19, DIM)
    d.rectangle((0, h - 3, w, h), fill=INK)
    return im


def tabs(d, x0, w, y, names, size=19):
    for i, n in enumerate(names):
        d.rectangle((x0 + i * w, y, x0 + (i + 1) * w - 1, y + 27), fill=PLUM if n == "Sedan_Rival" else INK)
        label(d, (x0 + i * w + 14, y + 3), NAME[n], size, CREAM)


def arrow(d, c, ang, n=15, fill=INK):
    """A nose arrow centred at c for ground heading `ang` (the plates' headings: 0 right, 90 away from us), drawn with the camera's foreshortening."""
    a = math.radians(ang)
    vx, vy = math.cos(a), -math.sin(a) * SIN
    k = n / math.hypot(vx, vy)
    ux, uy = vx * k, vy * k
    tip, tail = (c[0] + ux, c[1] + uy), (c[0] - ux, c[1] - uy)
    d.line([tail, tip], fill=fill, width=3)
    ex, ey = ux / n, uy / n
    d.polygon([tip, (tip[0] - ex * 10 - ey * 6, tip[1] - ey * 10 + ex * 6), (tip[0] - ex * 10 + ey * 6, tip[1] - ey * 10 - ex * 6)], fill=fill)


def chip(d, xy, hexs, s=18):
    x, y = xy
    for k, h in enumerate(hexs):
        d.rectangle((x + k * (s + 4), y, x + k * (s + 4) + s, y + s), fill=h, outline=INK)


def playsheet(root, yaw, out):
    W, H, M, HEAD, FOOT = 340, 240, 150, 120, 180
    rows = [(315, "toward us, right"), (225, "toward us, left"), (45, "away, right"), (135, "away, left")]
    sheet = Image.new("RGB", (M + W * 4, HEAD + H * 4 + FOOT), CREAM)
    sheet.paste(header(sheet.width, f"The rival's car at play zoom, yaw {yaw}", "1 px = 1 px of a 1080p play frame (14 at 38 degrees, 38.6 px per metre); the nose points as the arrow says", HEAD - 24), (0, 0))
    sheet.paste(grain(W * 4, H * 4), (M, HEAD))
    d = ImageDraw.Draw(sheet)
    for j, (hd, txt) in enumerate(rows):
        arrow(d, (M // 2, HEAD + j * H + H // 2 - 14), hd)
        label(d, (M // 2, HEAD + j * H + H // 2 + 22), txt, 15, fill=DIM, anchor="ma")
        for i, n in enumerate(IDS):
            drop(sheet, plate(root, n, "play", yaw, hd), M + i * W + W / 2, HEAD + j * H + H / 2)
    d = ImageDraw.Draw(sheet)
    tabs(d, M, W, HEAD - 28, IDS)
    y = HEAD + H * 4
    for i, n in enumerate(IDS):
        x = M + i * W + 14
        chip(d, (x, y + 14), HEX[n])
        label(d, (x + 50, y + 12), SUB[n], 15, DIM)
        b, r = lab(HEX[n][0])[0], lab(HEX[n][1])[0]
        label(d, (x, y + 52), f"value (L*)  body {b:.0f}  roof {r:.0f}  roof - body {r - b:+.0f}", 15, INK)
    label(d, (14, y + 12), "scale", 15, DIM)
    d.line([(14, y + 70), (14 + 2 * PPM, y + 70)], fill=INK, width=3)
    label(d, (14, y + 76), "2 m", 15, INK)
    pp = silhouette_keep(plate(root, "Person", "play", yaw, 0))
    c = pp.crop(pp.getbbox())
    sheet.paste(c, (M - 62, y + 22), c)
    label(d, (M - 78, y + 22 + c.height + 6), "1.75 m", 14, INK)
    sheet.save(out)


def toscreen(g, o):
    """Ground metres (along screen-right, along away-from-camera) to the play frame's pixels, about origin o."""
    return (o[0] + g[0] * PPM, o[1] - g[1] * PPM * SIN)


def marks(frame, car_px, heading, ss=3):
    """Glyph's 059 marks over the car, on `frame` (RGB): the approach line from the nose along its heading (violet 2.2 px over an ink-deep underlay, a diamond tick every 2.4 m) and the rival
    ring (solid 2.4 px at r 0.55 m, dashed at r 0.95 m) at a post beside it. Drawn flat at full colour: unlit, outside the grade."""
    W, H = frame.size
    ov = Image.new("RGBA", (W * ss, H * ss), (0, 0, 0, 0))
    d = ImageDraw.Draw(ov)
    s = lambda p: (p[0] * ss, p[1] * ss)  # noqa: E731
    a = math.radians(heading)
    dv = (math.cos(a), math.sin(a))
    line = [toscreen((dv[0] * (2.36 + k * 2.4), dv[1] * (2.36 + k * 2.4)), car_px) for k in range(0, 4)]  # the nose and three ticks (7 m): the spec shortens it as the car drives
    d.line([s(p) for p in line], fill=INKDEEP + (153,), width=int(4.8 * ss), joint="curve")
    d.line([s(p) for p in line], fill=VIOLET + (255,), width=int(2.2 * ss), joint="curve")
    for p in line[1:]:
        q = [(p[0], p[1] - 3.4), (p[0] + 3.4, p[1]), (p[0], p[1] + 3.4), (p[0] - 3.4, p[1])]
        d.polygon([s(x) for x in q], fill=VIOLET + (255,), outline=INKDEEP + (255,), width=ss)
    post = toscreen((-3.4 * dv[0] + 1.4, -3.4 * dv[1] - 2.2), car_px)  # a posted rival, a few metres behind the car and off its flank
    for r, w, dash in ((0.55, 2.4, False), (0.95, 1.8, True)):
        rx, ry = r * PPM, r * PPM * SIN
        n = 120
        for under in (True, False):
            for k in range(n):
                if dash and (k // 3) % 2:
                    continue
                seg = [s((post[0] + rx * math.cos(t), post[1] + ry * math.sin(t))) for t in (2 * math.pi * k / n, 2 * math.pi * (k + 1.4) / n)]
                d.line(seg, fill=INKDEEP + (153,) if under else VIOLET + (255,), width=int((w + 2.4 if under else w) * ss))
    ov = ov.resize((W, H), Image.LANCZOS)
    out = frame.convert("RGBA")
    out.alpha_composite(ov)
    return out.convert("RGB")


CAR_AT = (1306, 600)  # the footprint centre of the car across the road from Vale's, in overhead-pickup.png
CROP = (700, 150, 1700, 770)


def street(root, frame, car, with_marks=False):
    g = Image.open(frame).convert("RGB")
    p = plate(root, car, "play", 30, 30)
    g.paste(p, (CAR_AT[0] - p.width // 2, CAR_AT[1] - p.height // 2 - 17), p)  # the footprint centre sits 0.55 m x cos(38) x 38.57 = 17 px below the plate centre
    if with_marks:
        g = marks(g, CAR_AT, 30)
    return g.crop(CROP)


def pair_board(root, frame, out, title, sub, labels, with_marks):
    a, b = street(root, frame, "StandIn", with_marks), street(root, frame, "Sedan_Rival", with_marks)
    w, h = a.size
    board = Image.new("RGB", (w * 2 + 12, h + 150), CREAM)
    board.paste(header(board.width, title, sub, 96), (0, 0))
    d = ImageDraw.Draw(board)
    for k, (im, t) in enumerate(zip((a, b), labels)):
        board.paste(im, (k * (w + 12), 100))
        label(d, (k * (w + 12) + 14, 100 + h + 10), t, 22)
    board.save(out)


def close_board(root, out):
    W, H = 760, 520
    sheet = Image.new("RGB", (W * 4, 130 + H * 2), CREAM)
    sheet.paste(header(sheet.width, "Close: the fitted play angle", "38 degrees down, yaw 30; 140 px per metre; nose toward us on the top row, away on the bottom", 96), (0, 0))
    d = ImageDraw.Draw(sheet)
    tabs(d, 0, W, 100, IDS, 20)
    for i, n in enumerate(IDS):
        for j, hd in enumerate((315, 135)):
            bg = Image.new("RGB", (W, H), (222, 214, 198))
            p = plate(root, n, "close", 30, hd)
            bg.paste(p, (0, 0), p)
            sheet.paste(bg, (i * W, 130 + j * H))
    sheet.save(out)


def hero_board(root, out):
    """Sedan_Rival alone, large: the front three-quarter and the rear three-quarter at the play angle."""
    W, H = 1100, 1000
    import json
    m = next((a for a in json.loads((REPO / "out/decokit/decokit_manifest.json").read_text())["assets"] if a["name"] == "Sedan_Rival"), None)
    fact = f"{m['tris']:,} triangles, {m['size'][0]:.2f} x {m['size'][2]:.2f} x {m['size'][1]:.2f} m (the sedans' footprint)" if m else "the sedans' footprint"
    sheet = Image.new("RGB", (W * 2, 120 + H), CREAM)
    sheet.paste(header(sheet.width, "Sedan_Rival, the rival crew's car", f"candidate piece, {fact}; plum enamel, ink-lacquer bubble, pearl stripes, chrome; 38 degrees down, yaw 30, 215 px per metre", 96), (0, 0))
    for j, hd in enumerate((315, 135)):
        bg = Image.new("RGB", (W, H), (222, 214, 198))
        p = plate(root, "Sedan_Rival", "close", 30, hd)
        bg.paste(p, (0, 0), p)
        sheet.paste(bg, (j * W, 120))
    sheet.save(out)


def two_ways_board(root, tint, out):
    """Today's stand-in two ways: as HostViews.Rivals.cs Trim() runs (the first slot's colour in every slot: one flat navy) and as its comment means (each slot x RivalTrim), by Vale's car and the proposal."""
    W, H = 760, 520
    cols = [("Sedan_Official", "Vale's official car", root), ("StandIn", "today, as the code runs: one flat navy", root), ("StandInTint", "today, as the comment means: each slot x RivalTrim", tint), ("Sedan_Rival", "proposed: Sedan_Rival", root)]
    sheet = Image.new("RGB", (W * 4, 130 + H * 2), CREAM)
    sheet.paste(header(sheet.width, "Today's stand-in two ways", "Trim() writes one MaterialPropertyBlock per renderer and a block reaches every material slot: the sedan is one mesh with ten slots, so slot 0's colour (the body teal x RivalTrim) lands on the roof, glass and tyres too", 96), (0, 0))
    d = ImageDraw.Draw(sheet)
    for i, (n, t, r) in enumerate(cols):
        d.rectangle((i * W, 100, (i + 1) * W - 1, 128), fill=PLUM if n == "Sedan_Rival" else INK)
        label(d, (i * W + 14, 103), t, 19, CREAM)
        for j, hd in enumerate((315, 135)):
            bg = Image.new("RGB", (W, H), (222, 214, 198))
            p = plate(r, n, "close", 30, hd)
            bg.paste(p, (0, 0), p)
            sheet.paste(bg, (i * W, 130 + j * H))
    sheet.save(out)


def eye_board(root, out):
    W, H = 760, 420
    sheet = Image.new("RGB", (W * 4, 130 + H * 2), CREAM)
    sheet.paste(header(sheet.width, "Street level", "1.5 m eye height, 38 mm; front and rear three-quarter, the near side", 96), (0, 0))
    d = ImageDraw.Draw(sheet)
    tabs(d, 0, W, 100, IDS, 20)
    for i, n in enumerate(IDS):
        for j, tag in enumerate(("front", "rear")):
            p = Image.open(Path(root) / f"{n}.eye.{tag}.png").convert("RGBA")
            bg = Image.new("RGB", p.size, (222, 214, 198))
            bg.paste(p, (0, 0), p)
            sheet.paste(bg.resize((W, H), Image.LANCZOS), (i * W, 130 + j * H))
    sheet.save(out)


def silhouette_keep(im):
    """The object without its soft shadow: alpha above 235 kept as it was."""
    a = np.array(im)
    a[..., 3] = np.where(a[..., 3] > 235, a[..., 3], 0)
    return Image.fromarray(a, "RGBA")


def silhouette(im, fill=(20, 24, 28)):
    a = np.array(im)
    out = np.zeros(a.shape[:2] + (4,), np.uint8)
    out[..., :3] = fill
    out[..., 3] = np.where(a[..., 3] > 235, 255, 0)  # the car, not its soft shadow
    return Image.fromarray(out, "RGBA")


def shape_board(root, eye, out):
    """The plan outlines, then the play-zoom plates as flat shapes, as value only, and squinted."""
    W, H, M, HEAD, PLAN = 340, 240, 150, 120, 190
    sets = [(k, hd) for k in ("shape", "grey", "squint") for hd in (315, 135)]
    sheet = Image.new("RGB", (M + W * 4, HEAD + PLAN + H * len(sets) + 20), CREAM)
    sheet.paste(header(sheet.width, "Shape and value: does it still read without the hue?", "plan outline (120 px per metre); then at play zoom: flat shape, greyscale (value only), and squinted (a third of the size, blown back up)", HEAD - 24), (0, 0))
    bg = grain(W * 4, H * len(sets))
    sheet.paste(bg, (M, HEAD + PLAN))
    d = ImageDraw.Draw(sheet)
    tabs(d, M, W, HEAD - 28, IDS)
    label(d, (14, HEAD + PLAN // 2 - 12), "plan", 18)
    for i, n in enumerate(IDS):
        s = silhouette(Image.open(Path(eye) / f"{n}.top.png").convert("RGBA"))
        s = s.resize((W - 40, int(s.height * (W - 40) / s.width)), Image.LANCZOS)
        c = s.crop(s.getbbox())
        sheet.paste(c, (M + i * W + 20, HEAD + PLAN // 2 - c.height // 2), c)
    for r, (kind, hd) in enumerate(sets):
        y = HEAD + PLAN + r * H
        label(d, (14, y + H // 2 - 14), {"shape": "flat shape", "grey": "greyscale", "squint": "squinted"}[kind], 18)
        arrow(d, (M // 2, y + H // 2 + 28), hd)
        for i, n in enumerate(IDS):
            p = plate(root, n, "play", 30, hd)
            if kind == "shape":
                drop(sheet, silhouette(p), M + i * W + W / 2, y + H / 2)
                continue
            tile = bg.crop((i * W, r * H, (i + 1) * W, (r + 1) * H)).convert("RGBA")
            tile.alpha_composite(p, (W // 2 - p.width // 2, H // 2 - p.height // 2))
            t = tile.convert("L")
            if kind == "squint":
                t = t.resize((W // 3, H // 3), Image.BOX).resize((W, H), Image.BICUBIC)
            sheet.paste(t.convert("RGB"), (M + i * W, y))
    sheet.save(out)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--plates", required=True)
    ap.add_argument("--out", required=True)
    ap.add_argument("--boards", default="play30,play210,street,marks,close,eye,shape,two,hero")
    ap.add_argument("--frame", default=str(REPO / "briefs/041/build/0311/proof-1080/overhead-pickup.png"))
    a = ap.parse_args()
    out = Path(a.out)
    out.mkdir(parents=True, exist_ok=True)
    root = Path(a.plates)
    want = a.boards.split(",")
    if "play30" in want:
        playsheet(root / "play", 30, out / "1-play-zoom-yaw30.png")
    if "play210" in want:
        playsheet(root / "play", 210, out / "2-play-zoom-yaw210.png")
    if "street" in want:
        pair_board(root / "insitu", a.frame, out / "3-in-the-street.png", "The rival's car in the street: across the road from Vale's car, 3:10",
                   "the game's own frame (build 0.2.122, proof set 0311, yaw 30, play zoom), 1 : 1; the car at the kerb is Vale's; the four people are the cell",
                   ("today: the stand-in (the official car in one flat navy)", "proposed: Sedan_Rival"), False)
    if "marks" in want:
        pair_board(root / "insitu", a.frame, out / "4-with-the-rivals-marks.png", "With the rivals' violet marks over the car",
                   "the approach line and a rival ring as Glyph's 059 spec draws them (#8a4fd0, unlit, flat): the plum body does not swallow them and they do not tint it",
                   ("today: the stand-in", "proposed: Sedan_Rival"), True)
    if "close" in want:
        close_board(root / "close", out / "5-close.png")
    if "eye" in want:
        eye_board(root / "eye", out / "6-street-level.png")
    if "hero" in want:
        hero_board(root / "hero", out / "9-hero-sedan-rival.png")
    if "two" in want:
        two_ways_board(root / "close", root / "tint", out / "8-stand-in-two-ways.png")
    if "shape" in want:
        shape_board(root / "play", root / "eye", out / "7-shape-and-value.png")
    print("boards ->", out)


if __name__ == "__main__":
    main()
