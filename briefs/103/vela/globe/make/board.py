"""Brief 103 (Vela): the globe board for Sky's phone, 1000 px wide, on the book's light page (as Glyph's boards). The pick first; Sky's frame and
the painting; today and options A, B and C at the two headings that see the Office's front (yaw 210, the play heading, and 300); A's finishes;
the finials question; the door with Glyph's hover line; what yaw 30 shows; the decisions; how it was made.
Tiles show the play size at 1080p (54 px a metre: the renders are at the Air's 93.2, scaled by S). Words are board_text.py's.

python3 briefs/103/vela/globe/make/board.py     (after render.py; reads out/scratch/vela/globe/renders, writes briefs/103/vela/globe/board.png)"""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[4]
sys.path.insert(0, str(HERE))
import board_text as T  # noqa: E402
from grade import grade  # noqa: E402

R = REPO / "out/scratch/vela/globe/renders"
OUT = HERE.parent / "board.png"
SKY = Path("/Users/sky/.t3/userdata/attachments/fe96bb36-dcb7-42e2-9225-e84ac1db8f4f-a44b9757-cbbe-41fc-b254-2f5b0045976c.png")
PAINT = REPO / "briefs/008/concepts/the-suborned-records-office-retrofuture-v2-2026-09-26.webp"
FONTS = REPO / "briefs/011/ui/kit/fonts"
W, M, G = 1000, 28, 16
TW = (W - 2 * M - G) // 2
PAGE, INK, DEEP, SOFT, HAIR = (249, 247, 238), (23, 48, 47), (31, 108, 103), (84, 98, 95), (214, 207, 190)
S = 54.0 / 93.2
FILES = {"Today": "today", "A": "a-nickel", "B": "b", "C": "c"}


def font(face, size):
    return ImageFont.truetype(str(FONTS / f"Jost-{face}.ttf"), size)


class Board:
    def __init__(self, h=12000):
        self.im = Image.new("RGB", (W, h), PAGE)
        self.d = ImageDraw.Draw(self.im)
        self.y = M

    def text(self, t, size=16, face="Medium", col=INK, x=M, w=W - 2 * M, lead=1.38, gap=6):
        f = font(face, size)
        line, lines = "", []
        for word in t.split(" "):
            trial = (line + " " + word).strip()
            if self.d.textlength(trial, font=f) > w and line:
                lines.append(line)
                line = word
            else:
                line = trial
        lines.append(line)
        for ln in lines:
            self.d.text((x, self.y), ln, font=f, fill=col)
            self.y += round(size * lead)
        self.y += gap

    def bullets(self, items, size=16, x=M, w=W - 2 * M):
        for t in items:
            self.d.ellipse((x + 2, self.y + size * 0.5, x + 9, self.y + size * 0.5 + 7), fill=DEEP)
            self.text(t, size=size, x=x + 20, w=w - 20, gap=4)

    def caps(self, t, col=DEEP, size=13):
        f = font("SemiBold", size)
        x = M
        for ch in t.upper():
            self.d.text((x, self.y), ch, font=f, fill=col)
            x += self.d.textlength(ch, font=f) + 1.8
        self.y += size + 9

    def head(self, cap, title, line=None):
        self.caps(cap)
        self.text(title, size=23, face="Bold", lead=1.2, gap=3)
        if line:
            self.text(line, size=16)

    def rule(self):
        self.y += 8
        self.d.line((M, self.y, W - M, self.y), fill=HAIR, width=1)
        self.y += 18

    def pics(self, items, w=TW, gap=G, x0=M):
        """Pictures side by side at width w, each (image, caption); y moves under the tallest caption."""
        x, bottom = x0, self.y
        for im, cap in items:
            h = round(im.height * w / im.width)
            self.im.paste(im.resize((w, h), Image.LANCZOS), (x, self.y))
            self.d.rectangle((x, self.y, x + w - 1, self.y + h - 1), outline=HAIR)
            yy = self.y + h + 6
            if cap:
                keep, self.y = self.y, yy
                self.text(cap, size=13.5, face="SemiBold", col=SOFT, x=x, w=w, lead=1.32, gap=0)
                yy, self.y = self.y, keep
            bottom = max(bottom, yy)
            x += w + gap
        self.y = bottom + 12

    def done(self):
        return self.im.crop((0, 0, W, self.y + M))


def render(name):
    return grade(Image.open(R / name))


def dil(m, k):
    out = m.copy()
    for dy in range(-k, k + 1):
        for dx in range(-k, k + 1):
            if dx * dx + dy * dy <= k * k:
                out |= np.roll(np.roll(m, dy, 0), dx, 1)
    return out


def door(variant, yaw):
    """The door's masks: (full, seen) booleans. full = the stand-in alone; seen = what of it shows past everything else."""
    f = np.asarray(Image.open(R / f"mask-full-{variant}-y{yaw:03d}.png").convert("RGBA")).astype(np.int16)
    s = np.asarray(Image.open(R / f"mask-seen-{variant}-y{yaw:03d}.png").convert("RGBA")).astype(np.int16)
    return (f[..., 3] > 127) & (f[..., 0] > 127), s[..., 0] > 127


def hover_line(base, variant, yaw):
    """Glyph's pick B on the door: 1.5 px ink outside the stand-in's edge, then 1 px cream (at 1080p; 2.6 and 1.7 px at the renders' density),
    hidden where something stands in front of the door's edge (the line shows only beside door that shows)."""
    full, seen = door(variant, yaw)
    near = dil(seen, 6)
    ink = dil(full, 3) & ~full & near
    cream = dil(full, 5) & ~dil(full, 3) & near
    a = np.asarray(base).copy()
    a[cream] = (247, 244, 236)
    a[ink] = (23, 48, 47)
    return Image.fromarray(a)


def crop(im, yaw, top=None, bottom=None):
    """The same metres wide at both headings: TW board px = TW / S render px, centred."""
    w, h = im.size
    cw = round(TW / S)
    x0 = max(0, (w - cw) // 2)
    t = top if top is not None else (150 if yaw == 210 else 120)
    b = bottom if bottom is not None else t + 1000
    return im.crop((x0, t, x0 + cw, min(h, b)))


def scaled(im, k=S):
    return im.resize((round(im.width * k), round(im.height * k)), Image.LANCZOS)


def main():
    b = Board()
    b.caps(T.HEAD["caps"])
    b.text(T.HEAD["title"], size=31, face="Bold", lead=1.15, gap=8)
    b.text(T.HEAD["sub"], size=15.5, col=SOFT)
    b.y += 6
    y0 = b.y
    b.y += 16
    b.text(T.PICK["title"], size=22, face="Bold", x=M + 18, w=W - 2 * M - 36, lead=1.2, gap=6)
    b.bullets(T.PICK["why"], size=16, x=M + 18, w=W - 2 * M - 36)
    b.y += 8
    b.d.rounded_rectangle((M, y0, W - M, b.y), 12, outline=DEEP, width=2)
    b.y += 22
    # where it comes from
    b.caps("Where it comes from")
    sky = Image.open(SKY).convert("RGB").crop((0, 10, 890, 953))                  # without the screenshot's borders
    paint = Image.open(PAINT).convert("RGB").crop((640, 170, 1080, 640))         # the ball, its columns and the canopy below
    b.pics([(sky, T.REF["sky"]), (paint, T.REF["paint"])])
    b.rule()
    # the four
    shares = {}
    for key, title, line in T.ROWS:
        b.head(key if key == "Today" else f"Option {key}", title, line)
        items = []
        for yaw in (210, 300):
            v = FILES[key]
            full, seen = door(v, yaw)
            shares[f"{key}@{yaw}"] = round(float(seen.sum()) / float(full.sum()), 3)
            items.append((scaled(crop(render(f"{v}-y{yaw:03d}.png"), yaw)), T.YAWCAP[yaw]))
        b.pics(items)
        b.rule()
    # A's finishes
    b.head("Option A · the finish", T.MATS["title"], T.MATS["line"])
    w4 = (W - 2 * M - 3 * 12) // 4
    b.pics([(render(f"mat-{k}-y210.png").crop((0, 0, 480, 600)), cap) for k, cap in T.MATS["caps"]], w=w4, gap=12)
    b.rule()
    # the finials
    b.head("A question", T.FINIALS["title"], T.FINIALS["line"])
    b.pics([(scaled(crop(render(f"{v}-y210.png"), 210, 60, 760)), cap) for v, cap in T.FINIALS["caps"]])
    b.rule()
    # the door, with the hover line
    b.head("The door", T.DOOR["title"], T.DOOR["line"])
    items = []
    for key in ("Today", T.PICK["key"]):
        v = FILES[key]
        im = hover_line(render(f"{v}-y210.png"), v, 210)
        items.append((scaled(crop(im, 210, 330, 1070)), T.DOOR["caps"][key]))
    b.pics(items)
    b.rule()
    # yaw 30
    b.caps("Yaw 30 and 120")
    y30 = render("today-y030.png")
    keep = b.y
    sw = 300
    small = y30.resize((sw, round(y30.height * sw / y30.width)), Image.LANCZOS)
    b.im.paste(small, (M, b.y))
    b.d.rectangle((M, b.y, M + sw - 1, b.y + small.height - 1), outline=HAIR)
    b.text(T.YAW30["title"], size=21, face="Bold", x=M + sw + 20, w=W - 2 * M - sw - 20, lead=1.2, gap=4)
    b.text(T.YAW30["line"], size=16, x=M + sw + 20, w=W - 2 * M - sw - 20)
    b.y = max(b.y, keep + small.height) + 12
    b.rule()
    # decisions
    b.caps("For Sky to decide")
    for i, (q, body) in enumerate(T.DECIDE, 1):
        b.text(f"{i}. {q}", size=18, face="Bold", lead=1.25, gap=2)
        b.text(body, size=15.5, x=M + 22, w=W - 2 * M - 22)
    b.rule()
    for t in T.FOOT:
        b.text(t, size=13.5, col=SOFT, lead=1.34, gap=5)
    im = b.done()
    im.save(OUT)
    (REPO / "out/scratch/vela/globe/doors.json").write_text(json.dumps(shares, indent=1) + "\n")
    print(f"[103] board {OUT} {im.size}; door share shown: {shares}")


if __name__ == "__main__":
    main()
