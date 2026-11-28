"""Generate the shared Air Post colour, tangent normal, ORM and night emission pages."""

from pathlib import Path
import argparse
import math

import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageOps

SIZE = 2048
CELL = 256
BLEED = 4
ROWS = [
    ["limestone", "terracotta", "jade", "nickel", "bronze", "charcoal", "ivory", "teal"],
    ["flutes", "tile", "rattan", "ribglass", "window", "foliage", "roof", "lamp"],
    ["stamp", "clock", "post", "air", "tickets", "dispatch", "meridian", "closed"],
    ["room0", "room1", "room2", "room3", "poster", "faience", "bloom", "iron"],
]
SLOT = {name: (i, j) for j, row in enumerate(ROWS) for i, name in enumerate(row)}
COLORS = [
    (217, 204, 174),
    (158, 67, 44),
    (62, 132, 125),
    (166, 166, 150),
    (151, 108, 55),
    (39, 47, 46),
    (231, 216, 182),
    (29, 77, 78),
    (194, 180, 148),
    (63, 117, 109),
    (143, 105, 64),
    (95, 141, 132),
    (49, 76, 75),
    (142, 114, 78),
    (112, 125, 114),
    (255, 218, 146),
]
BRASS = (222, 177, 108)
NAVY = (32, 67, 72)


def font(size, narrow=False):
    candidates = (["/System/Library/Fonts/Supplemental/DIN Condensed Bold.ttf"] if narrow else []) + [
        "/System/Library/Fonts/Supplemental/Futura.ttc",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
    ]
    return ImageFont.truetype(next(p for p in candidates if Path(p).exists()), size)


def label(d, xy, text, pts, color, narrow=False):
    d.text(xy, text, font=font(pts, narrow), fill=color, anchor="mm", stroke_width=0)


def graphics(name, image):
    d = ImageDraw.Draw(image)
    if name == "window":
        pane = image.copy()
        for j in range(4):
            a = j * 64
            tint = [(57, 73, 71), (70, 85, 80), (48, 68, 70), (81, 91, 81)][j]
            d.rectangle((a + 3, 4, a + 61, 247), fill=tint)
            stop = [87, 128, 54, 101][j]
            for y in range(8, stop, 5):
                d.line((a + 4, y, a + 60, y), fill=(125, 132, 116))
            for k in range(2):
                d.rectangle((a + 4 + k * 49, 73, a + 11 + k * 49, 240), fill=(111, 101, 79))
            d.line((a, 0, a, 256), fill=(42, 56, 55), width=5)
            d.rectangle((a + 4, 184, a + 61, 188), fill=(49, 57, 52))
        image.paste(Image.blend(image, pane, 0.24))
    elif name == "clock":
        d.ellipse((7, 7, 249, 249), fill=(227, 212, 174), outline=(96, 74, 45), width=7)
        for n in range(60):
            a = n * math.tau / 60
            u, v = math.sin(a), -math.cos(a)
            k = 92 if n % 5 == 0 else 104
            d.line(
                (128 + k * u, 128 + k * v, 128 + 111 * u, 128 + 111 * v),
                fill=NAVY,
                width=5 if n % 5 == 0 else 2,
            )
        for n in (12, 3, 6, 9):
            a = n * math.tau / 12
            label(d, (128 + 78 * math.sin(a), 128 - 78 * math.cos(a)), str(n), 24, NAVY, True)
        d.line((99, 108, 128, 128, 155, 62), fill=NAVY, width=7)
        d.ellipse((122, 122, 134, 134), fill=BRASS)
    elif name == "stamp":
        d.ellipse((19, 19, 237, 237), outline=BRASS, width=6)
        d.ellipse((31, 31, 225, 225), outline=BRASS, width=2)
        d.arc((69, 70, 185, 186), 0, 360, fill=BRASS, width=4)
        d.polygon([(46, 124), (150, 108), (201, 90), (159, 132), (114, 160), (121, 138)], fill=BRASS)
        for i in range(3):
            d.line((49 + i * 6, 142 + i * 6, 105, 138 + i * 3), fill=BRASS, width=3)
    elif name == "poster":
        d.rectangle((9, 9, 246, 246), outline=BRASS, width=3)
        d.ellipse((86, 35, 176, 125), fill=(215, 145, 83))
        for i in range(8):
            d.line((21, 170 + i * 4, 239, 103 + i * 5), fill=(77 + i * 6, 138 + i * 3, 130), width=3)
        d.polygon(
            [(30, 142), (183, 83), (213, 85), (172, 104), (133, 112), (103, 151), (101, 121)],
            fill=(230, 222, 184),
        )
        label(d, (128, 200), "PACIFIC", 31, (233, 218, 177))
        label(d, (128, 229), "NIGHT MAIL", 14, BRASS)
    elif name in ("post", "air", "tickets", "dispatch", "meridian", "closed"):
        words = {
            "post": ("POSTES", "PNEUMATIQUES"),
            "air": ("AIR MAIL", "HARBOUR DISTRICT"),
            "tickets": ("TICKETS", "RESERVATIONS"),
            "dispatch": ("DISPATCH", "PARCELS & LETTERS"),
            "meridian": ("MERIDIAN", "AIR POST"),
            "closed": ("LETTERS", "COLLECTION  17.10"),
        }[name]
        d.line((13, 51, 243, 51), fill=BRASS, width=3)
        d.line((33, 58, 223, 58), fill=BRASS, width=1)
        label(d, (128, 116), words[0], min(59, 366 // len(words[0])), (244, 230, 192), True)
        label(d, (128, 170), words[1], min(19, 224 // len(words[1])), BRASS)
        for y in (195, 200):
            d.line((50, y, 206, y), fill=BRASS, width=1)


def build(args):
    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    ref = Path(args.source)
    rooms = Image.open(args.rooms).convert("RGB")
    chip = np.asarray(ImageOps.grayscale(Image.open(ref / "terrazzo.jpg")).resize((256, 256)), dtype=float)
    chip = (chip - chip.mean()) / max(1, chip.std())
    rng = np.random.default_rng(922)
    y, x = np.mgrid[:256, :256]
    pages = {
        s: Image.new("RGB", (SIZE, 1024), c)
        for s, c in {
            "base": (143, 131, 110),
            "normal": (128, 128, 255),
            "orm": (255, 180, 5),
            "light": (0, 0, 0),
        }.items()
    }
    for n, (name, (col, row)) in enumerate(SLOT.items()):
        rgb = np.array(COLORS[n] if n < len(COLORS) else NAVY, dtype=float)
        grain = rng.normal(0, 1, (256, 256))
        height = np.zeros_like(grain)
        tone = grain * 0.75
        rough, metal = 0.65, 0.0
        light = np.zeros((256, 256, 3), dtype=float)
        if name == "foliage":
            vein = (
                np.exp(-np.abs(x - 127) / 3) * 0.61
                + np.exp(-np.abs((y + np.abs(x - 127) * 0.56) % 36 - 18) / 2.0) * 0.18
            )
            grad = np.sin(x / 255 * math.pi) * 0.65 + y / 255 * 0.16 + vein
            rgb = np.array([23, 52, 30]) + grad[:, :, None] * np.array([42, 56, 19])
            tone = grain * 1.6
            height = vein * 2
            rough = 0.53
        elif name == "bloom":
            rgb = np.array([155, 48, 32]) + ((y / 255.0) ** 2)[:, :, None] * np.array([78, 75, 19])
            tone = grain * 0.75
            rough = 0.62
        elif name in ("limestone", "ivory", "flutes"):
            tone = chip * 3.3 + grain * 0.9 + np.sin(y / 11 + x / 29) * 1.7
            rough = 0.56
            height = chip * 0.24
            if name == "flutes":
                grooves = np.cos(x * 2 * math.pi / 32)
                height += grooves * 9
                tone += grooves * 9 - 5
                rough = 0.44
            elif name == "limestone":
                course = y // 64
                joint_x = (x + (course % 2) * 51) % 103
                joint_y = y % 64
                dist = np.minimum.reduce((joint_x, 102 - joint_x, joint_y, 63 - joint_y))
                tone += np.asarray([1.6, -1.8, 0.5, -0.7])[course] - 10 * np.exp(-dist / 1.3)
                height -= 2.4 * np.exp(-dist / 0.92)
                rough = 0.61
        elif name == "roof":
            seam = np.minimum(x % 64, 63 - x % 64)
            fold = np.exp(-(((seam - 3) / 1.55) ** 2))
            shade = np.asarray([2.4, -0.6, 1.8, -3.0])[x // 64]
            tone = shade + grain * 1.4 + 1.7 * np.cos(y / 13) + fold * 14
            height = 3.5 * fold
            rough = 0.43
            metal = 0.58
        elif name in ("nickel", "bronze", "iron"):
            tone = grain * 0.9 + rng.normal(0, 2, (256, 1))
            rough = 0.29 if name != "iron" else 0.55
            metal = 0.87
        elif name in ("tile", "faience", "jade", "teal", "terracotta"):
            if name == "faience":
                rgb = np.array([45, 112, 108])
            seams = np.minimum(x % 64, 63 - x % 64) if name in ("tile", "faience") else np.ones_like(x) * 15
            joint = (
                np.minimum(seams, np.minimum(y % 128, 127 - y % 128))
                if name in ("tile", "faience")
                else seams
            )
            height = np.clip(joint, 0, 5) * 0.8
            tone += np.where(joint < 2, -22, 0) + 3 * np.sin(x / 37) * np.cos(y / 44)
            rough = 0.25
        elif name in ("window", "ribglass", "shutter", "rattan"):
            height = 4 * np.cos((x if name != "shutter" else y) * math.tau / (16 if name != "rattan" else 8))
            tone += height * 1.7
            if name == "window":
                tone += np.linspace(-18, 28, 256)[:, None] + np.sin(x / 24) * 4
            rough = 0.21 if name in ("ribglass", "window") else 0.56
            metal = 0.36 if name == "window" else 0.14
        elif name == "lamp":
            rough = 0.35
            light[:] = [215, 164, 88]
        base = np.clip(rgb + tone[:, :, None], 0, 255).astype("uint8")
        im = Image.fromarray(base)
        if name.startswith("room"):
            i = int(name[-1])
            w, h = rooms.size
            cx = i % 2
            cy = i // 2
            im = ImageOps.fit(
                rooms.crop((cx * w // 2, cy * h // 2, (cx + 1) * w // 2, (cy + 1) * h // 2)), (256, 256)
            )
            a = np.asarray(im).astype(float)
            light = a * 0.085
            rough = 0.42
            metal = 0.03
            im = Image.fromarray(np.clip(a * 0.61, 0, 255).astype("uint8"))
        graphics(name, im)
        if name in ("clock", "stamp", "air", "post", "tickets", "meridian", "dispatch", "closed", "poster"):
            a = np.asarray(im).astype(float)
            brighter = np.clip((a.mean(2) - 85) / 95, 0, 1)
            height += brighter * 1.9
            light = a * (brighter[:, :, None] * 0.22)
            rough = 0.37
            metal = 0.15
        gx = np.gradient(height, axis=1) * 0.78
        gy = np.gradient(height, axis=0) * 0.78
        normal = np.stack([-gx, gy, np.ones_like(gx)], 2)
        normal /= np.linalg.norm(normal, axis=2)[:, :, None]
        occ = np.clip(246 + np.minimum(0, height) * 1.4, 180, 255)
        orm = np.stack((occ, np.clip(rough * 255 + grain * 2, 1, 255), np.full_like(grain, metal * 255)), 2)
        tiles = {
            "base": im,
            "normal": Image.fromarray(((normal * 0.5 + 0.5) * 255).astype("uint8")),
            "orm": Image.fromarray(orm.astype("uint8")),
            "light": Image.fromarray(np.clip(light, 0, 255).astype("uint8")),
        }
        for kind, tile in tiles.items():
            inner = tile.resize((CELL - BLEED * 2, CELL - BLEED * 2))
            pad = np.pad(np.asarray(inner), ((BLEED, BLEED), (BLEED, BLEED), (0, 0)), mode="edge")
            pages[kind].paste(Image.fromarray(pad), (col * CELL, row * CELL))
    for name, im in pages.items():
        im.save(out / (name + ".png"), optimize=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", required=True)
    parser.add_argument("--rooms", required=True)
    parser.add_argument("--out", required=True)
    build(parser.parse_args())
