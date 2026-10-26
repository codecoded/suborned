"""Brief 070 (Vela): the boards for the getaway car's lift, from cars070_plates.py's plates. python3 -I cars070_compose.py --plates DIR --out DIR [--still briefs/070/build/0133/stills/brief-getaway.jpg]

  1-briefing-still.png  the briefing page's getaway still: the game's own frame (Unity, build 0133), then today's car and the candidate at the same camera (Blender; road colour from the frame)
  2-play-zoom.png       the play camera at true scale (38.57 px per metre, 38 degrees, yaw 30), nose toward and away, 1 : 1 and the same pixels x3 (nearest)
  3-close.png           the play angle at 150 px per metre, front and rear three-quarter, today and the candidate
  4-street-level.png    1.5 m eye height, front and rear three-quarter
  5-vales-car.png       Vale's official car today and with the same lift (render-only, not catalogued)
Preview lighting (the game's sun, a warm flat ambient, a street sky for reflections, Khronos neutral): relative reads, not the Unity grade."""

import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

REPO = Path(__file__).resolve().parents[2]
args = sys.argv[1:]


def arg(k, d):
    return args[args.index(k) + 1] if k in args else d


PL, OUT = Path(arg("--plates", "out/scratch/vela/070/plates")), Path(arg("--out", "briefs/070/vela-getaway"))
STILL = Path(arg("--still", str(REPO / "briefs/070/build/0133/stills/brief-getaway.jpg")))
INK, CREAM, DIM, CORAL = (23, 48, 47), (247, 244, 236), (90, 110, 108), (252, 156, 132)
ROAD, STUDIO = (164, 177, 182), (225, 215, 200)  # the road sampled from the game's frames (#A4B1B6); the plates' warm studio floor
FONT = REPO / "out/concepts/ui-011-kit/fonts/Jost-Bold.ttf"


def font(n):
    try:
        return ImageFont.truetype(str(FONT), n)
    except OSError:
        return ImageFont.load_default()


def plate(name):
    return Image.open(PL / f"{name}.png").convert("RGBA")


def on(bg, im, seed=1):
    a = np.empty((im.height, im.width, 3), np.float32)
    a[:] = bg
    a += np.random.default_rng(seed).normal(0, 2.0, (im.height, im.width, 1)).astype(np.float32)
    c = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), "RGB").convert("RGBA")
    c.alpha_composite(im)
    return c.convert("RGB")


def header(w, title, sub):
    im = Image.new("RGB", (w, 96), CREAM)
    d = ImageDraw.Draw(im)
    d.text((24, 14), title, font=font(30), fill=INK)
    d.text((24, 56), sub, font=font(19), fill=DIM)
    d.rectangle((0, 93, w, 96), fill=INK)
    return im


def tab(sheet, x, y, w, text, hot=False):
    d = ImageDraw.Draw(sheet)
    d.rectangle((x, y, x + w - 1, y + 30), fill=CORAL if hot else INK)
    d.text((x + 12, y + 4), text, font=font(19), fill=INK if hot else CREAM)


def grid(cells, cols, title, sub, out, pad=0):
    """cells: [(image, tab text, hot)], laid in rows of `cols`, each under its tab."""
    cw, ch = max(c[0].width for c in cells), max(c[0].height for c in cells)
    rows = (len(cells) + cols - 1) // cols
    W = cols * cw + (cols - 1) * pad
    sheet = Image.new("RGB", (W, 96 + rows * (ch + 31)), CREAM)
    sheet.paste(header(W, title, sub), (0, 0))
    for i, (im, text, hot) in enumerate(cells):
        x, y = (i % cols) * (cw + pad), 96 + (i // cols) * (ch + 31)
        tab(sheet, x, y, cw, text, hot)
        sheet.paste(im, (x, y + 31))
    sheet.save(OUT / out, optimize=True)
    print(f"[C70] {out} {sheet.size}")


def bbox_union(names, margin):
    boxes = [plate(n).getchannel("A").point(lambda a: 255 if a > 250 else 0).getbbox() for n in names]
    x0, y0 = min(b[0] for b in boxes) - margin, min(b[1] for b in boxes) - margin
    x1, y1 = max(b[2] for b in boxes) + margin, max(b[3] for b in boxes) + margin
    return x0, y0, x1, y1


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    B, A, O, O2 = "Sedan_Getaway", "Sedan_Getaway_v2", "Sedan_Official", "Sedan_Official_v2"
    # 1: the still. The game's frame cropped round its car; the renders cropped round theirs at the same size (the same camera, so the same scale)
    box = bbox_union([f"{B}.still", f"{A}.still"], 70)
    w, h = box[2] - box[0], box[3] - box[1]
    game = Image.open(STILL).convert("RGB")
    gx, gy = 985 - w // 2, 545 - h // 2  # the game's car: centre about (985, 545) in brief-getaway.jpg
    cells = [(game.crop((gx, gy, gx + w, gy + h)), "the game today (Unity, build 0133)", False)]
    cells += [(on(ROAD, plate(f"{n}.still")).crop(box), t, hot) for n, t, hot in ((B, "today: Sedan_Getaway (preview light)", False), (A, "candidate: Sedan_Getaway_v2", True))]
    grid(cells, 3, "The getaway car at the briefing still's framing", "1080p crops at 1 : 1; ShotRunner car pose (1.8, 5.8, 11.5) m, 32 deg; left the game, right two renders in one light", "1-briefing-still.png", 8)
    # 2: play zoom, true scale, then x3
    cells = []
    for n, t, hot in ((B, "today", False), (A, "candidate", True)):
        for hd, hw in ((315, "nose toward"), (135, "nose away")):
            cells.append((on(ROAD, plate(f"{n}.play.h{hd}"), hd), f"{t}, {hw}, 1 : 1", hot))
    big = [(im.resize((im.width * 3, im.height * 3), Image.NEAREST), t.replace("1 : 1", "x3"), hot) for im, t, hot in cells]
    small = [(Image.new("RGB", (900, 660), CREAM), "", False) for _ in cells]
    for (im, t, hot), s in zip(cells, small):
        s[0].paste(im, (300, 220))
    grid([(s[0], t, hot) for s, (im, t, hot) in zip(small, cells)] + big, 4, "At play zoom", "orthographic, 38 degrees down, yaw 30, 38.57 px per metre (zoom 14 at 1080p): the car is about 70 px long. Top row 1 : 1 in a 900 px cell, bottom the same pixels x3", "2-play-zoom.png", 8)
    # 3, 4: close and street level
    grid([(on(STUDIO, plate(f"{n}.close.h{hd}")), f"{t}, {'front' if hd == 315 else 'rear'} three-quarter", hot) for n, t, hot in ((B, "today", False), (A, "candidate", True)) for hd in (315, 135)],
         2, "Close, at the play angle", "38 degrees down, yaw 30, 150 px per metre", "3-close.png", 8)
    grid([(on(STUDIO, plate(f"{n}.eye.{k}")), f"{t}, {k} three-quarter", hot) for n, t, hot in ((B, "today", False), (A, "candidate", True)) for k in ("front", "rear")],
         2, "Street level", "1.5 m eye height, 38 mm, 6.6 m out", "4-street-level.png", 8)
    # 5: Vale's car
    cells = [(on(STUDIO, plate(f"{n}.close.h{hd}")), f"{t}, {'front' if hd == 315 else 'rear'}", hot) for n, t, hot in ((O, "Vale's car today", False), (O2, "Vale's car, the same lift (render-only)", True)) for hd in (315, 135)]
    grid(cells, 2, "Vale's official car with the same lift", "the same builder (cars070.sedan_v2) in deep teal with an ivory roof and flash; not catalogued: a candidate only if Aster asks", "5-vales-car.png", 8)


main()
