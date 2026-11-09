"""Brief 104 (Vela): the robot board, 1000 px wide for a phone. python3 -I compose.py <renders_dir> <out.png>
<renders_dir> holds studio/ and play/ from render.py (tile PNGs + JSON). Concepts are read from the store and briefs/ as they are.
Studio rows share one scale (true relative size, the grey figure is a 1.75 m person); play rows are 1:1 pixels at the play camera
(zoom 10 on a 1080 px tall screen); the role section shows play crops at 2x. Numbers on the board match notes.md."""

import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, str(Path(__file__).resolve().parent))
from board_data import CONCEPTS, DECIDE, KIT_ROWS, MODELS, PLAY_ROWS, ROLES, STORE_ROWS  # noqa: E402

R, OUT = Path(sys.argv[1]), Path(sys.argv[2])
W, M = 1000, 24
CW = W - 2 * M
BG, INK, DIM, RULE, BAND = (242, 237, 226), (23, 48, 47), (96, 110, 106), (208, 199, 181), (228, 221, 205)
TEAL, CORAL, CREAM = (28, 120, 108), (170, 72, 50), (247, 244, 236)
FONT = "/System/Library/Fonts/Avenir Next.ttc"
STYLE = {"bold": 0, "demi": 2, "medium": 5, "regular": 7, "heavy": 8}


def font(style, size):
    return ImageFont.truetype(FONT, size, index=STYLE[style])


def tw(s, f):
    return f.getbbox(s)[2] if s else 0


def wrap(s, f, width):
    lines, cur = [], ""
    for word in s.split():
        t = (cur + " " + word).strip()
        if tw(t, f) <= width or not cur:
            cur = t
        else:
            lines.append(cur)
            cur = word
    return lines + ([cur] if cur else [])


class Board:
    def __init__(self):
        self.parts = []  # (height, draw(img, y))
        self.h = 0

    def add(self, h, fn):
        self.parts.append((self.h, fn))
        self.h += h

    def render(self):
        im = Image.new("RGB", (W, self.h), BG)
        for y, fn in self.parts:
            fn(im, y)
        return im


def tile(view, mid):
    return Image.open(R / view / f"{mid}.png").convert("RGBA"), json.loads((R / view / f"{mid}.json").read_text())


def clean(c, person=False):
    """Shadow-catcher pixels are black with partial alpha. Drop the faint sky-occlusion veil (alpha < 30) and keep the sun and contact shadows;
    the size figure becomes a flat grey silhouette."""
    a = np.asarray(c).astype(np.float32)
    shadow = a[..., :3].max(axis=2) < 24
    a[..., 3] = np.where(shadow, np.clip((a[..., 3] - 30) * 255 / 225, 0, 255), a[..., 3])
    if person:
        body = a[..., 3] > 200
        a[body, :3] = (150, 145, 136)
    return Image.fromarray(a.astype(np.uint8), "RGBA")


def cut(view, mid, pad, top, bottom, scale=1.0, feather=10):
    """The model's crop: its pixel bounds +pad left/right, rows top..bottom (tile pixels), scaled; shadow edges feathered."""
    im, meta = tile(view, mid)
    x0, _, x1, _ = meta["px_bounds"]
    box = (max(0, x0 - pad), top, min(im.width, x1 + pad), bottom)
    c = clean(im.crop(box), mid == "Person")
    a = np.asarray(c.getchannel("A")).astype(np.float32)
    ramp = np.minimum(1.0, np.minimum(np.arange(c.width), np.arange(c.width)[::-1]) / feather)
    c.putalpha(Image.fromarray((a * ramp[None, :]).astype(np.uint8)))
    if scale != 1.0:
        c = c.resize((max(1, round(c.width * scale)), max(1, round(c.height * scale))), Image.LANCZOS)
    ground_x = (meta["ground_px"][0] - box[0]) * scale
    return c, (meta["ground_px"][1] - top) * scale, ground_x


def chip(d, x, y, label, bg, f=None):
    f = f or font("bold", 12)
    w = tw(label, f) + 12
    d.rounded_rectangle((x, y, x + w, y + 19), 9, fill=bg)
    d.text((x + 6, y + 2), label, fill=CREAM, font=f)
    return w


def heading(b, title, sub=None):
    def draw(im, y):
        d = ImageDraw.Draw(im)
        d.line((M, y + 8, W - M, y + 8), fill=RULE, width=2)
        d.text((M, y + 16), title, fill=INK, font=font("bold", 24))
        if sub:
            for i, ln in enumerate(wrap(sub, font("regular", 16), CW)):
                d.text((M, y + 52 + i * 21), ln, fill=DIM, font=font("regular", 16))
    n = len(wrap(sub, font("regular", 16), CW)) if sub else 0
    b.add(58 + n * 21, draw)


def studio_rows(b, rows, ppm):
    k = ppm / 200.0
    for ids in rows:
        cuts = []
        for mid in ids:
            meta = tile("studio", mid)[1]
            cuts.append(cut("studio", mid, 24, 0, round(meta["ground_px"][1] + 40), k))
        top = min(tile("studio", mid)[1]["px_bounds"][1] for mid in ids) * k
        gy = max(c[1] for c in cuts)
        slots = [max(c[0].width, 112) for c in cuts]
        gap = max(4, (CW - sum(slots)) // max(1, len(ids) - 1)) if len(ids) > 1 else 0
        gap = min(gap, 28)
        x0 = M + (CW - sum(slots) - gap * (len(ids) - 1)) // 2
        img_h = round(gy - top + 30)
        label_h = 112

        def draw(im, y, cuts=cuts, slots=slots, gap=gap, x0=x0, ids=ids, top=top, gy=gy):
            d = ImageDraw.Draw(im)
            x = x0
            for (c, cy, cx), sw, mid in zip(cuts, slots, ids):
                ox = x + (sw - c.width) // 2
                im.paste(c, (ox, round(y - top + gy - cy + 8)), c)
                info = MODELS[mid]
                ly = round(y + gy - top + 36)
                cxm = x + sw // 2
                if info.get("n"):
                    tag = f"{info['n']}"
                    d.ellipse((x + 2, ly - 2, x + 26, ly + 22), fill=INK)
                    d.text((x + 14 - tw(tag, font("bold", 15)) // 2, ly + 1), tag, fill=CREAM, font=font("bold", 15))
                    d.text((x + 31, ly), info["short"], fill=INK, font=font("bold", 16))
                else:
                    d.text((cxm - tw(info["short"], font("demi", 15)) // 2, ly), info["short"], fill=DIM, font=font("demi", 15))
                    x += sw + gap
                    continue
                d.text((x + 2, ly + 25), f"{info['h']} · {info['tris']}", fill=DIM, font=font("medium", 14))
                fk = font("demi", 14)
                d.text((x + 2, ly + 45), info["face"], fill=TEAL if info["dots"] else DIM if info["dots"] is None else CORAL, font=fk)
                cy2 = ly + 68
                for t in info.get("today", []):
                    if cy2 > ly + 92:
                        break
                    chip(d, x + 2, cy2, t, INK)
                    cy2 += 22
                x += sw + gap
        b.add(img_h + label_h, draw)


def legend(b, ids):
    f, fb = font("regular", 14), font("bold", 14)
    lines = []
    for mid in ids:
        info = MODELS[mid]
        if not info.get("n"):
            continue
        for i, ln in enumerate(wrap(info["where"], f, CW - 30)):
            lines.append((info["n"] if i == 0 else "", ln))

    def draw(im, y):
        d = ImageDraw.Draw(im)
        for i, (n, ln) in enumerate(lines):
            if n:
                d.text((M, y + i * 19), str(n), fill=INK, font=fb)
            d.text((M + 28, y + i * 19), ln, fill=DIM, font=f)
    b.add(len(lines) * 19 + 14, draw)


def play_rows(b):
    for ids in PLAY_ROWS:
        cuts = [cut("play", mid, 5, 0, 220) for mid in ids]
        widths = [c[0].width for c in cuts]
        gap = min(14, (CW - sum(widths)) // max(1, len(ids) - 1))
        x0 = M + (CW - sum(widths) - gap * (len(ids) - 1)) // 2
        top = min(tile("play", mid)[1]["px_bounds"][1] for mid in ids)
        low = max(tile("play", mid)[1]["px_bounds"][3] for mid in ids)  # one camera: every crop shares the ground, so they paste at one row

        def draw(im, y, cuts=cuts, gap=gap, x0=x0, ids=ids, top=top, low=low):
            d = ImageDraw.Draw(im)
            d.rectangle((0, y, W, y + low - top + 34), fill=BAND)
            x = x0
            for (c, cy, cx), mid in zip(cuts, ids):
                im.paste(c, (x, y - top + 6), c)
                n = str(MODELS[mid].get("n") or "1.75 m")
                f = font("bold", 14) if MODELS[mid].get("n") else font("medium", 12)
                d.text((x + (c.width - tw(n, f)) // 2, y + low - top + 12), n, fill=INK, font=f)
                x += c.width + gap
        b.add(low - top + 46, draw)


def concepts(b):
    cols, gap = 3, 16
    cw = (CW - gap * (cols - 1)) // cols
    ch = round(cw * 2 / 3)
    f = font("regular", 13)
    for r in range(0, len(CONCEPTS), cols):
        row = CONCEPTS[r:r + cols]
        caps = [wrap(c[1], f, cw) for c in row]
        nh = max(len(c) for c in caps)

        def draw(im, y, row=row, caps=caps):
            d = ImageDraw.Draw(im)
            for i, ((path, _, crop), cap) in enumerate(zip(row, caps)):
                x = M + i * (cw + gap)
                src = Image.open(path).convert("RGB")
                if crop:
                    w, h = src.size
                    src = src.crop((round(crop[0] * w), round(crop[1] * h), round(crop[2] * w), round(crop[3] * h)))
                src.thumbnail((cw, ch), Image.LANCZOS)
                d.rectangle((x, y, x + cw, y + ch), fill=(250, 248, 242))
                im.paste(src, (x + (cw - src.width) // 2, y + (ch - src.height) // 2))
                for j, ln in enumerate(cap):
                    d.text((x, y + ch + 4 + j * 17), ln, fill=DIM, font=f)
        b.add(ch + 12 + nh * 17, draw)


def role(b, r):
    fb, f = font("bold", 20), font("regular", 15)
    panels = []
    for label, mids, name, colour in r["panels"]:
        cs = []
        for mid in mids:
            _, y0, _, y1 = tile("play", mid)[1]["px_bounds"]
            c, cy, _ = cut("play", mid, 6, max(0, y0 - 8), y1 + 8, r.get("zoom", 2.0), feather=8)
            cs.append((c, cy))
        panels.append((label, cs, name, colour))
    what = wrap(r["what"], f, CW)
    why = wrap(r["why"], f, CW)
    G = max(cy for p in panels for _, cy in p[1])  # one ground line for every crop in the role
    img_h = round(max(G - cy + c.height for p in panels for c, cy in p[1]))

    def draw(im, y):
        d = ImageDraw.Draw(im)
        d.text((M, y), r["title"], fill=INK, font=fb)
        for i, ln in enumerate(what):
            d.text((M, y + 30 + i * 20), ln, fill=DIM, font=f)
        y2 = y + 34 + len(what) * 20
        x = M
        for label, cs, name, colour in panels:
            pw = max(sum(c.width for c, _ in cs) + 4 * (len(cs) - 1), 120)
            chip(d, x, y2, label, colour)
            cx = x + (pw - sum(c.width for c, _ in cs) - 4 * (len(cs) - 1)) // 2
            for c, cy in cs:
                im.paste(c, (cx, round(y2 + 24 + G - cy)), c)
                cx += c.width + 4
            for j, ln in enumerate(wrap(name, font("demi", 14), pw)[:2]):
                d.text((x, y2 + 30 + img_h + j * 18), ln, fill=INK, font=font("demi", 14))
            x += pw + 26
        y3 = y2 + 34 + img_h + 2 * 18 + 6
        for i, ln in enumerate(why):
            d.text((M, y3 + i * 20), ln, fill=INK, font=f)
    b.add(34 + len(what) * 20 + 34 + img_h + 2 * 18 + 6 + len(why) * 20 + 26, draw)


def main():
    b = Board()

    def header(im, y):
        d = ImageDraw.Draw(im)
        d.text((M, y + 18), "Every robot model we've made", fill=INK, font=font("bold", 34))
        d.text((M, y + 64), "Brief 104 · Vela · 10 Nov 2026 · Sky picks one per role (section E)", fill=DIM, font=font("medium", 17))
        for i, ln in enumerate(wrap("Same light for every model. Sections A and B: three-quarter front, true relative size; the grey figure is a 1.75 m person. "
                                    "Section C: the play camera (zoom 10, 38° down), real pixels at 1080p. Faces: teal = dot-matrix face (GDD §15), coral = not.", font("regular", 15), CW)):
            d.text((M, y + 94 + i * 20), ln, fill=INK, font=font("regular", 15))
    b.add(160, header)
    ppm = 112
    heading(b, "A · In the game's kit (DecoKit, this repo)", "unity/Assets/Suborned/Kit, built by tools/decokit. The old monorepo holds the same designs (older exports).")
    studio_rows(b, KIT_ROWS, ppm)
    legend(b, [m for row in KIT_ROWS for m in row])
    heading(b, "B · In the Suborned store (never in the game)", "~/Projects/the-suborned-store/testbed-art. Sizes as made; the old 2.5D testbed drew 9 at 1.75 m and 10 at 1.5 m.")
    studio_rows(b, STORE_ROWS, ppm)
    legend(b, [m for row in STORE_ROWS for m in row])
    heading(b, "C · At the play camera, real size", "As on a 1080p screen at play zoom: the person is about 82 px tall. Numbers as above.")
    play_rows(b)
    heading(b, "D · Robot concepts", "The store's sheets and this repo's briefs. Of these, only the service sheet (1–5) and briefs 031 and 097 (6, 8) became models.")
    concepts(b)
    heading(b, "E · Per role: today and proposed", "Play camera at 2x, so faces can be judged on a phone.")
    for r in ROLES:
        role(b, r)

    def decide(im, y):
        d = ImageDraw.Draw(im)
        d.line((M, y + 8, W - M, y + 8), fill=RULE, width=2)
        d.text((M, y + 16), "Sky decides", fill=INK, font=font("bold", 24))
        yy = y + 54
        for i, t in enumerate(DECIDE):
            lines = wrap(t, font("regular", 15), CW - 30)
            d.text((M, yy), f"{i + 1}", fill=INK, font=font("bold", 15))
            for ln in lines:
                d.text((M + 26, yy), ln, fill=INK, font=font("regular", 15))
                yy += 20
            yy += 8
        d.text((M, yy + 6), "Full paths, counts and how to remake the board: briefs/104/vela/robots/notes.md", fill=DIM, font=font("regular", 14))
    n = sum(len(wrap(t, font("regular", 15), CW - 30)) for t in DECIDE)
    b.add(54 + n * 20 + len(DECIDE) * 8 + 44, decide)
    im = b.render()
    OUT.parent.mkdir(parents=True, exist_ok=True)
    im.save(OUT, optimize=True)
    print(f"[compose] {im.size[0]} x {im.size[1]} -> {OUT}")


main()
