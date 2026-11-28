"""Bake metric surface tiles and the postal collection door. R/G carry slopes, B tone or AO, A roughness."""

from pathlib import Path
import argparse

import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageOps
from scipy.ndimage import distance_transform_edt, gaussian_filter


def grey(path):
    return np.array(ImageOps.grayscale(Image.open(path)), dtype=float) / 255


def pixels(values):
    return np.rint(np.clip(values, 0, 1) * 255).astype(np.uint8)


def field(rng, shape, sigma):
    cloud = gaussian_filter(rng.normal(size=shape), sigma, mode="wrap")
    return cloud / max(0.0001, cloud.std())


def slopes(height):
    dx = (np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)) * -0.5
    dy = (np.roll(height, -1, axis=0) - np.roll(height, 1, axis=0)) * 0.5
    scale = np.sqrt(1 + dx * dx + dy * dy)
    return 0.5 + 0.5 * dx / scale, 0.5 + 0.5 * dy / scale


def packed(path, rg, tone, rough):
    Image.fromarray(pixels(np.stack((*rg, tone, rough), axis=-1))).save(path, optimize=True)


def stone(src, dst):
    color = grey(src / "marble-diff.jpg")
    normal = np.asarray(Image.open(src / "marble-n.jpg").convert("RGB"), float) / 255
    rough = grey(src / "marble-rough.jpg")
    grain = color - gaussian_filter(color, 6, mode="wrap")
    # Keep the photographed calcite, small pits and sawn courses together across all channels.
    tone = 0.51 + (color - np.median(color)) * 1.75 + grain * 0.44
    packed(dst / "finish_stone.png", (normal[..., 0], normal[..., 1]), tone, 0.40 + 0.32 * rough)


def faience(rng, dst):
    h, w = 512, 1024
    y, x = np.mgrid[:h, :w]
    # Six 240 x 120 mm tiles per repeat. Rolled glaze stops at the recessed joint.
    row = y * 6 // h
    u = (x * 6 / w + 0.5 * (row % 2)) % 1
    v = (y * 6 / h) % 1
    cloud = field(rng, (h, w), 5)
    edge = np.minimum(np.minimum(u, 1 - u) * w / 6, np.minimum(v, 1 - v) * h / 6) + 0.27 * cloud
    glaze = np.clip((edge - 0.72) / 2.35, 0, 1)
    rolled = np.exp(-(((edge - 4.8) / 2.3) ** 2))
    col = np.floor(x * 6 / w + 0.5 * (row % 2)).astype(int) % 6
    dye = rng.uniform(-0.065, 0.065, (6, 6))[row, col]
    dimple = field(rng, (h, w), 1.05)
    body = 0.51 + dye + 0.010 * cloud + 0.005 * dimple - 0.07 * rolled
    tone = np.where(glaze < 0.6, 0.245 + 0.025 * dimple, body)
    rough = (0.73 + 0.035 * cloud) * (1 - glaze) + (0.235 + 0.037 * cloud + 0.018 * dimple) * glaze
    relief = 2.7 * glaze + 0.64 * rolled + 0.035 * dimple + 0.075 * cloud
    packed(dst / "finish_faience.png", slopes(relief), tone, rough)


def brass(rng, dst):
    shape = (512, 512)
    draw = field(rng, shape, (0.45, 13.0))
    wax = field(rng, shape, (6.0, 21.0))
    stipple = field(rng, shape, 0.55)
    # Drawn metal leaves lines of different lengths. A few end visibly within one stroke.
    score = np.zeros(shape)
    for _ in range(95):
        x, y = rng.integers(512, size=2)
        length = int(rng.integers(8, 140))
        slope = rng.uniform(-0.1, 0.10)
        k = np.arange(length)
        score[(y + np.rint(k * slope).astype(int)) % 512, (x + k) % 512] += np.sin(k * np.pi / length)
    relief = draw * 0.21 + stipple * 0.065 - score * 0.67
    packed(
        dst / "finish_metal.png",
        slopes(relief),
        0.50 + 0.059 * draw + 0.038 * wax - 0.08 * score,
        0.31 + 0.046 * draw + 0.044 * wax + 0.07 * score,
    )


def shell(rng, dst):
    n = 512
    y, x = np.mgrid[:n, :n]
    bay = x // 128
    u = (x + 0.5) % 128
    v = (y + (bay % 2) * 256 + 0.5) % 512
    # Raised folded seams with smaller cross laps and oil-canning between the clips.
    seam = np.exp(-(((u - 4.5) / 1.65) ** 2)) + 0.45 * np.exp(-(((u - 8) / 1.5) ** 2))
    lap = np.exp(-(((v - 7) / 1.7) ** 2))
    shallow = np.sin(u * np.pi / 128) ** 2 * np.cos(v * np.pi * 2 / 512)
    stain = field(rng, (n, n), (17.0, 4.0))
    cloud = field(rng, (n, n), 22.0)
    strip = np.array([0.00, 0.055, -0.025, 0.025])[bay]
    tone = 0.50 + strip + 0.037 * stain + 0.032 * cloud + 0.05 * shallow - 0.10 * seam
    depth = 2.8 * seam + 0.78 * lap + 2.2 * shallow + 0.055 * stain
    packed(dst / "finish_roof.png", slopes(depth), tone, 0.41 + 0.03 * cloud + 0.04 * stain - 0.12 * seam)


def nameplates(dst):
    gold = Image.new("L", (1024, 512))
    ivory = gold.copy()
    d, ink = ImageDraw.Draw(gold), ImageDraw.Draw(ivory)
    regular = next(
        str(p)
        for p in map(
            Path,
            [
                "/System/Library/Fonts/Supplemental/Futura.ttc",
                "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
            ],
        )
        if p.exists()
    )
    narrow = next(
        str(p)
        for p in map(
            Path,
            [
                "/System/Library/Fonts/Supplemental/DIN Condensed Bold.ttf",
                "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
            ],
        )
        if p.exists()
    )
    # A die-struck airmail roundel. Raised lines retain a continuous contour at the lens.
    for inset, width in ((11, 8), (26, 2), (49, 3), (58, 2)):
        d.ellipse((inset, inset, 511 - inset, 511 - inset), outline=255, width=width)
    for angle in range(0, 360, 5):
        a = np.deg2rad(angle)
        length = 8 if angle % 30 == 0 else 4
        d.line(
            (
                256 + 215 * np.sin(a),
                256 + 215 * np.cos(a),
                256 + (215 + length) * np.sin(a),
                256 + (215 + length) * np.cos(a),
            ),
            fill=255,
            width=2,
        )
    d.arc((133, 116, 385, 382), 198, 326, fill=255, width=5)
    d.arc((139, 119, 381, 379), 22, 126, fill=255, width=3)
    d.polygon(
        [(91, 253), (291, 224), (404, 174), (323, 254), (267, 275), (220, 336), (222, 273), (175, 282)],
        fill=255,
    )
    for j in range(3):
        d.line((104 + j * 9, 285 + j * 9, 190 + j * 2, 275 + j * 8), fill=255, width=4)
    d.text((255, 368), "AIR  POST", font=ImageFont.truetype(narrow, 22), fill=255, anchor="mm")
    # The collection card is a landscape plate. Art is pre-compensated for its 2:1 metal holder.
    card = Image.new("L", (1024, 512))
    paper = card.copy()
    edge, glyph = ImageDraw.Draw(card), ImageDraw.Draw(paper)
    edge.rounded_rectangle((19, 19, 1004, 492), radius=15, outline=255, width=6)
    edge.rounded_rectangle((31, 31, 992, 480), radius=10, outline=255, width=2)
    for x in (53, 970):
        for y in (55, 455):
            edge.ellipse((x - 7, y - 7, x + 7, y + 7), fill=255)
            edge.line((x - 5, y + 2, x + 5, y - 2), fill=0, width=2)
    glyph.text((512, 190), "LETTERS", font=ImageFont.truetype(regular, 128), fill=255, anchor="mm")
    edge.line((172, 284, 852, 284), fill=255, width=3)
    edge.text(
        (505, 363), "LAST COLLECTION    17.10", font=ImageFont.truetype(narrow, 55), fill=255, anchor="mm"
    )
    gold.paste(card.resize((512, 512), Image.Resampling.LANCZOS), (512, 0))
    ivory.paste(paper.resize((512, 512), Image.Resampling.LANCZOS), (512, 0))

    def signed(im):
        mask = np.array(im) > 112
        return 0.5 + (distance_transform_edt(mask) - distance_transform_edt(~mask)) / 10.0

    relief = gaussian_filter(np.maximum(np.array(gold), np.array(ivory)).astype(float) / 255, 0.82) * 1.4
    rt, up = slopes(relief)
    Image.fromarray(pixels(np.stack((signed(gold), signed(ivory), rt, up), axis=-1))).save(
        dst / "postal_plates.png", optimize=True
    )


def sorting_rooms(src, dst):
    """Keep the original paintings at source resolution. Alpha describes the distance behind each window."""
    art = Image.open(src / "workrooms.jpg").convert("RGB")
    if art.size != (1024, 1024):
        raise ValueError("The depth masks are registered to the approved four workroom paintings.")
    # Projected outlines in the approved art. The desk, lamps, globe and nearest lockers sit forward.
    fixtures = [
        [
            (
                0.13,
                [
                    (0.284, 0.649),
                    (0.397, 0.601),
                    (0.605, 0.599),
                    (0.710, 0.648),
                    (0.693, 0.946),
                    (0.288, 0.948),
                ],
            ),
            (0.40, [(0.362, 0.572), (0.43, 0.480), (0.586, 0.477), (0.625, 0.57)]),
            (0.14, [(0.351, 0.032), (0.602, 0.032), (0.592, 0.095), (0.490, 0.139), (0.373, 0.099)]),
        ],
        [
            (
                0.23,
                [
                    (0.007, 0.576),
                    (0.268, 0.565),
                    (0.738, 0.565),
                    (0.986, 0.578),
                    (0.977, 0.851),
                    (0.012, 0.851),
                ],
            ),
            (
                0.36,
                [
                    (0.514, 0.09),
                    (0.799, 0.11),
                    (0.795, 0.423),
                    (0.511, 0.445),
                    (0.208, 0.426),
                    (0.211, 0.117),
                ],
            ),
        ],
        [
            (
                0.12,
                [
                    (0.273, 0.671),
                    (0.396, 0.615),
                    (0.619, 0.615),
                    (0.738, 0.668),
                    (0.711, 0.904),
                    (0.293, 0.895),
                ],
            ),
            (
                0.15,
                [(0.259, 0.542), (0.286, 0.468), (0.363, 0.46), (0.377, 0.535), (0.348, 0.6), (0.294, 0.6)],
            ),
            (
                0.16,
                [(0.69, 0.541), (0.70, 0.462), (0.77, 0.457), (0.797, 0.52), (0.78, 0.58), (0.733, 0.606)],
            ),
            (
                0.21,
                [
                    (0.337, 0.596),
                    (0.459, 0.527),
                    (0.651, 0.508),
                    (0.711, 0.548),
                    (0.61, 0.607),
                    (0.405, 0.62),
                ],
            ),
        ],
        [
            (0.20, [(0.004, 0.012), (0.095, 0.075), (0.09, 0.910), (0.015, 0.943)]),
            (0.20, [(0.913, 0.079), (0.995, 0.017), (0.986, 0.942), (0.92, 0.912)]),
        ],
    ]
    page = Image.new("L", art.size)
    y, x = np.mgrid[:512, :512] / 511.0
    edge = np.maximum(abs(x - 0.5) * 2, abs(y - 0.48) / np.where(y > 0.48, 0.52, 0.48))
    for i, shapes in enumerate(fixtures):
        depth = Image.fromarray(pixels(0.96 - 0.82 * np.clip(edge, 0, 1) ** 1.25))
        pen = ImageDraw.Draw(depth)
        for distance, polygon in shapes:
            pen.polygon([(round(u * 511), round(v * 511)) for u, v in polygon], fill=round(distance * 255))
        depth = Image.fromarray(np.uint8(gaussian_filter(np.asarray(depth, float), 2.1)))
        page.paste(depth, ((i % 2) * 512, (i // 2) * 512))
    art.putalpha(page)
    art.save(dst / "sorting_rooms.png", optimize=True)


def collection_door(rng, dst):
    w, h = 512, 1024
    shape = (h, w)
    coat = Image.new("RGB", (w, h), (56, 116, 111))
    elev = Image.new("L", (w, h), 138)
    polished = Image.new("L", (w, h), 0)
    finish = Image.new("L", (w, h), 97)
    layers = [ImageDraw.Draw(im) for im in (coat, elev, polished, finish)]

    def form(bounds, radius, albedo, height, metal, rough, line=0):
        for d, value in zip(layers, (albedo, height, metal, rough)):
            if line:
                d.rounded_rectangle(bounds, radius, outline=value, width=line)
            else:
                d.rounded_rectangle(bounds, radius, fill=value)

    brass = (162, 127, 77)
    dark = (39, 71, 68)
    # Recessed service hatch, folded edges, two hinge pins and the collection lock.
    form((88, 612, 424, 958), 22, dark, 104, 6, 137)
    form((91, 616, 420, 954), 20, (63, 127, 119), 147, 1, 88)
    form((103, 628, 408, 939), 15, (47, 103, 98), 139, 4, 111, 3)
    for top in (656, 854):
        form((96, top, 111, top + 43), 5, brass, 172, 226, 79)
        for pin in (top + 9, top + 31):
            form((96, pin, 111, pin + 2), 1, dark, 133, 180, 101)
    form((366, 757, 391, 795), 12, brass, 169, 234, 77)
    form((376, 768, 381, 785), 2, (32, 43, 42), 96, 76, 104)
    # The slot fits between the existing cast postal seal and the collection-time holder.
    form((102, 325, 410, 380), 9, (94, 78, 50), 150, 230, 92)
    form((106, 327, 406, 375), 7, brass, 166, 237, 69)
    form((122, 336, 390, 364), 5, (22, 35, 33), 89, 41, 134)
    form((124, 337, 388, 346), 4, (104, 95, 69), 152, 199, 74)
    form((126, 361, 386, 367), 2, (207, 169, 101), 174, 242, 53)
    for x in (114, 398):
        for y in (336, 366):
            form((x - 3, y - 3, x + 3, y + 3), 3, (113, 102, 78), 156, 220, 92)
            layers[0].line((x - 2, y + 1, x + 2, y - 1), fill=(43, 54, 48), width=1)
    # Vitreous enamel sign and restrained signwriter's stripes on the access panel.
    form((142, 688, 359, 752), 6, (32, 65, 64), 150, 5, 83)
    form((145, 691, 356, 749), 5, brass, 152, 173, 90, 2)
    regular = next(
        str(p)
        for p in map(
            Path,
            [
                "/System/Library/Fonts/Supplemental/DIN Condensed Bold.ttf",
                "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
            ],
        )
        if p.exists()
    )

    def lettering(x, y, word, size, color):
        layers[0].text((x, y), word, font=ImageFont.truetype(regular, size), anchor="mm", fill=color)

    lettering(251, 710, "HARBOUR DISTRICT", 22, (232, 216, 173))
    lettering(251, 735, "POSTAL ENGINEERING", 13, (191, 176, 134))
    lettering(252, 873, "M   /   04", 23, (193, 185, 143))
    for side in (-1, 1):
        for offset in (0, 9):
            xx = 253 + side * (99 + offset)
            layers[0].line(
                [(xx, 817), (xx - side * 57, 817), (xx - side * 73, 833)], fill=(139, 160, 130), width=2
            )
    y, x = np.mgrid[:h, :w]
    tone = field(rng, shape, 13) * 1.45 + field(rng, shape, 1) * 1.15
    hands = np.exp(-(((x - 334) / 54) ** 2) - ((y - 369) / 15) ** 2) + 0.70 * np.exp(
        -(((x - 306) / 63) ** 2) - ((y - 782) / 38) ** 2
    )
    color = np.asarray(coat, float) + tone[..., None] + hands[..., None] * np.array([7, 5, 2])
    color_map = np.dstack((np.uint8(np.clip(color, 0, 255)), np.asarray(polished)))
    Image.fromarray(color_map).save(dst / "collection_coat.png", optimize=True)
    relief = gaussian_filter(np.asarray(elev, float), 0.85) * 0.075 + field(rng, shape, 1.4) * 0.095
    recess = np.clip((138 - np.asarray(elev, float)) / 55, 0, 1)
    packed(
        dst / "collection_finish.png",
        slopes(relief),
        1.0 - 0.28 * recess,
        np.asarray(finish, float) / 255 + tone * 0.006 - hands * 0.12,
    )


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("out", type=Path)
    args = parser.parse_args()
    args.out.mkdir(parents=True, exist_ok=True)
    rng = np.random.default_rng(2425)
    stone(args.source, args.out)
    faience(rng, args.out)
    brass(rng, args.out)
    shell(rng, args.out)
    nameplates(args.out)
    sorting_rooms(args.source, args.out)
    collection_door(rng, args.out)
