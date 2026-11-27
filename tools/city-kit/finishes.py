"""Paint the shop interiors, enamel signs and material relief to one UV layout."""

from pathlib import Path
from math import cos, pi, sin
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageOps
from atlas_layout import REGIONS, SIZE, SURFACES

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
TARGET = ROOT / "unity/Assets/Suborned/Materials/Play/LanternKit"
FONTS = ROOT / "unity/Assets/Suborned/UI/Resources/Kit/fonts"
PAINT = {
    # sRGB base, metallic, smoothness, surface relief strength
    "stone": ((218, 207, 183), 0.01, 0.30, 0.17),
    "cream": ((235, 224, 201), 0.03, 0.40, 0.10),
    "jade": ((77, 126, 115), 0.04, 0.64, 0.52),
    "wine": ((124, 47, 49), 0.07, 0.67, 0.08),
    "nickel": ((165, 170, 160), 0.64, 0.62, 0.09),
    "bronze": ((157, 125, 78), 0.58, 0.57, 0.11),
    "shadow": ((61, 53, 44), 0.12, 0.37, 0.13),
    "soffit": ((166, 125, 76), 0.40, 0.49, 0.45),
    "glass": ((101, 131, 133), 0.23, 0.62, 0.02),
    "shade": ((104, 125, 120), 0.06, 0.45, 0.17),
    "plinth": ((143, 138, 123), 0.02, 0.40, 0.19),
    "roof": ((113, 124, 121), 0.24, 0.38, 0.18),
    "opal": ((240, 219, 169), 0.01, 0.52, 0.11),
    "leaf": ((67, 101, 72), 0.0, 0.32, 0.23),
    "petals": ((184, 77, 71), 0.0, 0.30, 0.1),
    "terracotta": ((146, 102, 77), 0.02, 0.40, 0.22),
}


def font(size, weight="Medium"):
    return ImageFont.truetype(str(FONTS / f"Jost-{weight}.ttf"), size)


def spaced(d, text, at, size, step, colour):
    f = font(size)
    length = sum(d.textlength(c, font=f) for c in text) + step * (len(text) - 1)
    x, y = at
    x -= length * 0.5
    for char in text:
        d.text((x, y), char, anchor="lm", font=f, fill=colour)
        x += d.textlength(char, font=f) + step


def sign(name, idx):
    shades = ["#3e4944", "#314c45", "#633c38", "#395456"]
    im = Image.new("RGB", (1024, 128), shades[idx])
    d = ImageDraw.Draw(im)
    for y in [9, 13, 114, 118]:
        d.line((5, y, 1018, y), fill=("#af9164" if y in (9, 118) else "#75684e"), width=2)
    for x in [23, 1001]:
        d.polygon([(x, 49), (x + 7, 64), (x, 79), (x - 7, 64)], fill="#bf9c69")
    spaced(d, name, (512, 63), 63, 7, "#eedeaf")
    return im


def clock():
    n = 512
    im = Image.new("RGB", (n, n), "#beaa83")
    d = ImageDraw.Draw(im)
    for rect, col in [(8, "#51473a"), (15, "#d7b987"), (23, "#f1dfb3"), (30, "#eadbb6")]:
        d.ellipse((rect, rect, n - rect, n - rect), fill=col)
    c = 256
    for i in range(60):
        a = (i / 60 - 0.25) * 2 * pi
        outer = 216
        inner = outer - (25 if i % 5 == 0 else 7)
        d.line(
            (c + inner * cos(a), c + inner * sin(a), c + outer * cos(a), c + outer * sin(a)),
            fill="#574c3d",
            width=5 if i % 5 == 0 else 2,
        )
    for txt, u, v in [("12", 256, 93), ("3", 424, 256), ("6", 256, 420), ("9", 88, 256)]:
        d.text((u, v), txt, anchor="mm", font=font(29), fill="#584c3e")
    for a, r, w in [(-2.49, 122, 10), (-0.68, 178, 7)]:
        dx, dy = cos(a), sin(a)
        d.polygon(
            [
                (c - dy * w, c + dx * w),
                (c + dx * r, c + dy * r),
                (c + dy * w, c - dx * w),
                (c - dx * 25, c - dy * 25),
            ],
            fill="#4d483e",
        )
    d.ellipse((246, 246, 266, 266), fill="#9e7848")
    return im


def poster(maritime):
    w, h = 256, 512
    im = Image.new("RGB", (w, h), "#e3d0a0")
    d = ImageDraw.Draw(im)
    d.rectangle((13, 14, 243, 494), outline="#9a7656", width=3)
    d.rectangle((22, 24, 234, 347), fill="#73a3ac" if maritime else "#7c9a9e")
    d.ellipse((124, 49, 213, 138), fill="#edc47f")
    d.polygon([(22, 226), (75, 166), (99, 194), (138, 153), (231, 243), (233, 315), (22, 313)], fill="#789d96")
    if maritime:
        d.rectangle((22, 239, 233, 347), fill="#477474")
        for j in range(6):
            d.line((25, 269 + j * 12, 80 + j * 18, 268 + j * 12), fill="#97aba0", width=2)
        d.polygon([(44, 251), (175, 241), (212, 224), (183, 260), (73, 272)], fill="#e8d7af")
        d.polygon([(78, 246), (88, 229), (166, 222), (180, 238)], fill="#a85044")
        d.polygon([(99, 229), (111, 218), (152, 217), (159, 225)], fill="#314e4d")
    else:
        d.polygon(
            [(22, 316), (61, 236), (86, 260), (135, 176), (180, 252), (233, 207), (233, 348), (22, 348)],
            fill="#587774",
        )
        for off, top in [(0, 237), (23, 212), (54, 253), (89, 228)]:
            d.polygon(
                [(35 + off, 338), (38 + off, top), (44 + off, top - 19), (46 + off, top), (50 + off, 338)],
                fill="#d9c69e",
            )
        d.polygon(
            [
                (39, 174),
                (134, 143),
                (168, 98),
                (182, 92),
                (169, 132),
                (212, 119),
                (221, 106),
                (226, 106),
                (223, 127),
                (150, 151),
                (104, 190),
                (85, 193),
                (112, 158),
            ],
            fill="#f1deb7",
        )
    spaced(d, "THE STRAITS" if maritime else "AEROLINES", (128, 382), 27, 1, "#334d4a")
    spaced(d, "COASTAL PASSAGE" if maritime else "MERIDIAN AIR", (128, 420), 17, 2, "#874b3e")
    for y, l in [(457, 74), (464, 107), (471, 55)]:
        d.line((128 - l / 2, y, 128 + l / 2, y), fill="#987e5e", width=2)
    return im


def print_maps():
    TARGET.mkdir(parents=True, exist_ok=True)
    albedo = Image.new("RGB", (SIZE, SIZE))
    normals = Image.new("RGB", (SIZE, SIZE), (128, 128, 255))
    mask = Image.new("RGBA", (SIZE, SIZE), (0, 255, 0, 85))
    glow = Image.new("RGB", (SIZE, SIZE))

    def fill(sheet, slot, img):
        x, y, r, b = REGIONS[slot]
        sheet.paste(img.resize((r - x, b - y), Image.Resampling.LANCZOS), (x, y))

    art = Image.open(HERE / "workrooms.jpg").convert("RGB")
    w, h = art.size
    for i, key in enumerate(("bureau", "flowers", "studio", "produce")):
        x, y = i % 2 * w // 2, i // 2 * h // 2
        room = ImageOps.fit(art.crop((x, y, x + w // 2, y + h // 2)), (1024, 512))
        fill(albedo, key, room)
        rgb = np.asarray(room).astype(float) / 255
        luma = rgb @ np.array([0.2126, 0.7152, 0.0722])
        warm = np.maximum(0, rgb[:, :, 0] - rgb[:, :, 2])
        power = 0.13 + 0.38 * np.clip((luma - 0.26) * 1.4, 0, 0.85) + 0.08 * warm
        fill(glow, key, Image.fromarray((rgb * power[:, :, None] * 255).clip(0, 255).astype("uint8")))
        v = np.zeros((512, 1024, 4), dtype=np.uint8)
        v[:, :, 0] = 3
        v[:, :, 1] = 218 + 30 * luma
        v[:, :, 3] = 71 + 24 * luma
        fill(mask, key, Image.fromarray(v))
    for idx, (key, words) in enumerate(
        [
            ("exchange_name", "LANTERN EXCHANGE"),
            ("flower_name", "FLORIST"),
            ("market_name", "MARKET HALL"),
            ("passage_name", "MERIDIAN TICKETS"),
        ]
    ):
        im = sign(words, idx)
        fill(albedo, key, im)
        a = np.asarray(im)
        light = np.clip((a[:, :, 0].astype(float) - 175) / 65, 0, 1)
        fill(glow, key, Image.fromarray((light[:, :, None] * np.array([108, 82, 47])).astype("uint8")))
        fill(mask, key, Image.new("RGBA", im.size, (75, 250, 0, 135)))
    for key, im in [("clock", clock()), ("air_poster", poster(False)), ("sea_poster", poster(True))]:
        fill(albedo, key, im)
        if key == "clock":
            fill(glow, key, im.point(lambda p: int(p * 0.31)))
    rng = np.random.default_rng(1129)
    yy, xx = np.mgrid[0:256, 0:256]
    for key in SURFACES:
        base, metal, gloss, strength = PAINT[key]
        grain = rng.normal(0, 1.0, (256, 256))
        soft = (
            np.asarray(
                Image.fromarray(rng.integers(0, 255, (16, 16), dtype=np.uint8)).resize(
                    (256, 256), Image.Resampling.BICUBIC
                )
            ).astype(float)
            / 255
            - 0.5
        )
        height = grain * 0.13
        pigment = 2.5 * soft + grain * 0.55
        occlusion = np.ones_like(height)
        if key in ("stone", "cream", "plinth", "terracotta"):
            seam = (yy < 3) | ((xx < 2) & (yy < 126)) | ((abs(xx - 128) < 1) & (yy >= 126)) | (abs(yy - 127) < 1)
            if key in ("stone", "plinth"):
                height -= seam * 2.1
                pigment -= seam * 9
                occlusion -= seam * 0.10
            pigment += soft * 6 + grain * 1.1
        if key == "jade":
            wave = 0.5 + 0.5 * np.cos(2 * pi * xx / 32)
            height += wave * 8
            pigment += wave * 8 - 4
            occlusion -= 0.09 * (1 - wave)
        if key == "soffit":
            row = yy // 16
            r = ((xx + (row % 2) * 8) % 16 - 8) ** 2 + (yy % 16 - 8) ** 2
            hole = np.clip((13 - r) * 0.25, 0, 1)
            height -= hole * 4
            pigment -= hole * 53
            occlusion -= hole * 0.48
        if key in ("bronze", "nickel"):
            pigment += 0.8 * np.sin(yy * 1.83) + grain * 0.8
        if key == "roof":
            lip = np.clip(2 - abs(xx % 64 - 4), 0, 1)
            height += lip * 3
            pigment += lip * 8
            occlusion -= lip * 0.09
        if key == "shade":
            rib = yy % 32
            height += (rib - 16) * 0.15
            pigment += np.where(rib < 3, -18, (rib - 16) * 0.42)
        if key == "glass":
            pigment += 9 * (1 - yy / 255) + 10 * soft
        if key in ("leaf", "petals"):
            edge = abs(2 * xx / 255 - 1)
            pigment += 9 * (1 - edge) + 9 * soft - 6 * edge
            height += 4 * np.exp(-abs(xx - 128) / 3)
        color = np.clip(np.array(base)[None, None, :] + pigment[:, :, None], 0, 255).astype("uint8")
        fill(albedo, key, Image.fromarray(color))
        if key in ("glass", "shade"):
            room = ImageOps.fit(art.crop((0, h // 2, w // 2, h)), (256, 256))
            room = np.asarray(room).astype(float)
            # Muted room values and horizontal reflected sky distinguish a higher sash from a shop display.
            reflection = np.clip(0.58 - 0.27 * yy / 255 + 0.10 * soft, 0, 1)
            tint = room * np.array([0.51, 0.58, 0.61]) + np.array([16, 25, 29])
            tint = tint * (1 - reflection[:, :, None]) + np.array([102, 133, 139]) * reflection[:, :, None]
            if key == "shade":
                slat = yy % 17
                for channel in range(3):
                    tint[:, :, channel] = np.where(
                        slat < 2, tint[:, :, channel] * 0.63, tint[:, :, channel] + (16 - slat) * 0.46
                    )
            fill(albedo, key, Image.fromarray(np.clip(tint, 0, 255).astype("uint8")))
        dy, dx = np.gradient(height)
        v = np.stack((-dx * strength, dy * strength, np.ones_like(dx)), axis=2)
        v /= np.linalg.norm(v, axis=2)[:, :, None]
        fill(normals, key, Image.fromarray(np.round((v * 0.5 + 0.5) * 255).astype("uint8")))
        p = np.zeros((256, 256, 4), dtype=np.uint8)
        p[:, :, 0] = round(255 * metal)
        p[:, :, 1] = np.clip(occlusion * 255, 0, 255)
        p[:, :, 3] = np.clip(gloss * 255 + soft * 11 + grain * 1.6, 0, 255)
        fill(mask, key, Image.fromarray(p))
        if key == "opal":
            fill(glow, key, Image.new("RGB", (256, 256), (230, 184, 107)))
    albedo.save(TARGET / "LanternBase.jpg", quality=94, subsampling=0, optimize=True)
    for sheet, key, sz in [(normals, "Normal", 1024), (mask, "Mask", 512), (glow, "Light", 512)]:
        sheet.resize((sz, sz), Image.Resampling.LANCZOS).save(TARGET / ("Lantern" + key + ".png"), optimize=True)


if __name__ == "__main__":
    print_maps()
