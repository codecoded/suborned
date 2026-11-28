"""Working surfaces and printed matter for the Meridian delivery court, in metres."""

from pathlib import Path
import argparse
import json
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy.ndimage import gaussian_filter


def font(size, condensed=False):
    stem = "DIN Condensed Bold.ttf" if condensed else "Futura.ttc"
    candidates = [
        Path("/System/Library/Fonts/Supplemental") / stem,
        Path("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"),
    ]
    return ImageFont.truetype(next(p for p in candidates if p.exists()), size)


def print_sheet(out, rng):
    panels = [
        ("EVENING POST", "HARBOUR EDITION", (224, 214, 177), (44, 56, 54)),
        ("NIGHT FERRY", "SOUTH QUAYS   •   PIER 6", (66, 113, 112), (238, 213, 160)),
        ("PRESS CUT", "MERIDIAN   /   DISPATCH  03", (51, 79, 71), (230, 211, 155)),
        ("FRUIT RETURNS", "EMPTY CASES   /   THIS WAY", (174, 87, 58), (240, 219, 166)),
    ]
    sheet = Image.new("RGB", (1024, 1024))
    for i, (heading, sub, paper, ink) in enumerate(panels):
        im = Image.new("RGB", (512, 512), paper)
        d = ImageDraw.Draw(im)
        if i == 0:
            d.line((24, 27, 488, 27), fill=ink, width=5)
            d.text((256, 66), heading, font=font(64, True), fill=ink, anchor="mm")
            d.line((24, 102, 488, 102), fill=ink, width=3)
            d.text((256, 120), sub + "   •   TWO CENTS", font=font(21, True), fill=ink, anchor="mm")
            d.text((25, 143), "NEW QUAY RUN", font=font(45, True), fill=ink)
            d.rectangle((28, 210, 240, 359), fill=(111, 125, 108))
            for k in range(17):
                x = int(rng.integers(32, 224))
                h = int(rng.integers(12, 104))
                d.rectangle((x, 338 - h, x + int(rng.integers(4, 15)), 340), fill=(58, 75, 68))
            for y in range(346, 357, 3):
                d.line((30, y, 238, y), fill=paper, width=1)
            for col in range(3):
                for y in range(210 if col == 2 else 385, 482, 7):
                    x = 28 + col * 154
                    d.line((x, y, x + int(rng.integers(74, 140)), y), fill=ink, width=2)
        elif i == 1:
            d.ellipse((115, 56, 365, 302), fill=(213, 153, 94))
            for k in range(12):
                d.line((16, 237 + k * 10, 498, 233 + k * 4), fill=(151, 178, 151), width=4)
            d.polygon([(47, 281), (212, 245), (409, 240), (450, 246), (393, 269), (100, 299)], fill=ink)
            d.polygon([(168, 254), (193, 214), (310, 206), (351, 232)], fill=(44, 72, 74))
            d.text((256, 387), heading, font=font(76, True), fill=ink, anchor="mm")
            d.text((256, 452), sub, font=font(28, True), fill=ink, anchor="mm")
        else:
            for j in (12, 20):
                d.rounded_rectangle((j, j, 511 - j, 511 - j), 15, outline=ink, width=3)
            d.text((256, 170), "03" if i == 2 else "02", font=font(178, True), fill=ink, anchor="mm")
            d.text((256, 315), heading, font=font(72, True), fill=ink, anchor="mm")
            d.text((256, 394), sub, font=font(26, True), fill=ink, anchor="mm")
            for j in (433, 442):
                d.line((85, j, 426, j), fill=ink, width=2)
        a = np.array(im, dtype=float)
        grain = rng.normal(0, 2.7, (512, 512))
        fade = gaussian_filter(rng.normal(size=(512, 512)), 16)
        fade /= max(0.001, fade.std())
        a += grain[:, :, None] + fade[:, :, None] * 3.1
        if i < 2:
            for k in (105, 319):
                a[:, k : k + 2] = a[:, k : k + 2] * 0.77 + np.array(paper) * 0.23
        sheet.paste(Image.fromarray(np.uint8(np.clip(a, 0, 255))), ((i % 2) * 512, (i // 2) * 512))
    sheet.save(out / "yard_print.jpg", quality=92)


def blade(out, rng):
    size = (512, 768)
    im = Image.new("RGB", size, (51, 85, 81))
    d = ImageDraw.Draw(im)
    for inset in (14, 23):
        d.rounded_rectangle(
            (inset, inset, 511 - inset, 767 - inset), radius=59, outline=(204, 176, 124), width=4
        )
    d.ellipse((170, 65, 340, 233), fill=(202, 111, 73))
    d.polygon(
        [(67, 178), (225, 139), (425, 119), (362, 151), (261, 161), (176, 206), (223, 163)],
        fill=(242, 224, 176),
    )
    d.text((256, 287), "EVENING", font=font(84, True), anchor="mm", fill=(237, 214, 164))
    d.text((256, 390), "POST", font=font(151, True), anchor="mm", fill=(245, 225, 185))
    for row in (490, 499):
        d.line((74, row, 438, row), fill=(193, 118, 80), width=4)
    d.text((256, 559), "LATE EDITION", font=font(49, True), anchor="mm", fill=(228, 208, 168))
    d.text((256, 651), "PRINT WORKS", font=font(36, True), anchor="mm", fill=(203, 184, 135))
    tone = np.asarray(im, float)
    grain = gaussian_filter(rng.normal(size=(768, 512)), 1.2)
    tone += grain[:, :, None] * 8.0
    im = Image.fromarray(np.uint8(np.clip(tone, 0, 255)))
    im.save(out / "yard_blade.jpg", quality=91)


def contacts(district, out):
    city = json.loads(district.read_text())
    keys = ["p-z-harbour-lantern-papers-04", "p-z-harbour-meridian-court-02", "p-z-harbour-lantern-market-02"]
    people = {r["key"]: r for r in city["residents"]}
    ties = [t for t in city["ties"] if t["a"] in keys and t["b"] in keys]
    if not all(any(t["a"] == a and t["b"] == b for t in ties) for a, b in zip(keys, keys[1:])):
        raise ValueError("The published Harbour graph no longer connects the newsstand to the fruiterer.")
    out.write_text(
        json.dumps(
            {
                "graph": city["graph"],
                "people": [{n: people[k][n] for n in ("key", "name", "job")} for k in keys],
                "ties": ties,
            },
            indent=2,
        )
        + "\n"
    )


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("project", type=Path)
    args = parser.parse_args()
    lab = args.project.resolve()
    out = lab / "game/assets"
    rng = np.random.default_rng(6631)
    print_sheet(out, rng)
    blade(out, rng)
    contacts(lab.parents[1] / "sim/Suborned.Sim/District/harbour.json", lab / "game/court_contacts.json")
