"""The road board's post (brief 028, Vela, 2026-10-22): from road_board.py's linear frames to pictures, and the board.

python3 -I tools/decokit/road_board_post.py frame <dir> <tag> [--haze]          <tag>.lin.npy (+ .depth.npy) -> <tag>.png: distance haze (--haze, third person only), bloom, ACES, grade
python3 -I tools/decokit/road_board_post.py board <out.png> <title> <label:img> ...  side-by-side panels with labels (the testbed | now | proposed boards)

The grade is light.json's post (ACES, post-exposure +0.15 EV, contrast 6, saturation 18, bloom threshold 1.1 intensity 0.22 scatter 0.6) as far as a numpy pass can say it; the haze is Glint's brief 048
(exp2, farClip 450 m, reach 0.95: density sqrt(-ln(1 - reach)) / farClip, fog colour light.haze by day and night.haze at night, perspective cameras only; the play camera takes none)."""

import json
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

REPO = Path(__file__).resolve().parents[2]
LIGHT = json.loads((REPO / "unity/Assets/Suborned/Editor/World/light.json").read_text())
HAZE = json.loads((REPO / "unity/Assets/Suborned/Resources/Atmosphere/haze.json").read_text())


def hexlin(h):
    h = h.lstrip("#")
    c = np.array([int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)], np.float32)
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def blur(a, r):
    """A gaussian-ish blur by three box passes of radius r (px) along each axis."""
    out = a
    for _ in range(3):
        for ax in (0, 1):
            k = int(r) * 2 + 1
            pad = [(0, 0)] * out.ndim
            pad[ax] = (int(r) + 1, int(r))
            c = np.cumsum(np.pad(out, pad, mode="edge"), axis=ax, dtype=np.float64)
            n = out.shape[ax]
            out = ((np.take(c, range(k, k + n), axis=ax) - np.take(c, range(0, n), axis=ax)) / k).astype(np.float32)
    return out


def aces(x):
    return np.clip((x * (2.51 * x + 0.03)) / (x * (2.43 * x + 0.59) + 0.14), 0, 1)


def srgb(c):
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * np.power(np.maximum(c, 0), 1 / 2.4) - 0.055)


def frame(d, tag, haze):
    d = Path(d)
    lin = np.load(d / f"{tag}.lin.npy")
    night = tag.endswith("night")
    if haze:
        dep = np.load(d / f"{tag}.depth.npy")
        k = math.sqrt(-math.log(1 - HAZE["ground"]["reach"])) / HAZE["ground"]["farClip"]
        f = np.where(np.isfinite(dep), 1 - np.exp(-((k * np.where(np.isfinite(dep), dep, 0)) ** 2)), 0.0)[..., None].astype(np.float32)
        hz = hexlin(LIGHT["palette"]["night.haze" if night else "light.haze"]["hex"])
        lin = lin * (1 - f) + hz * f
    post = LIGHT["post"]
    thr, inten = post["bloom"]["threshold"], post["bloom"]["intensity"]
    bright = np.maximum(lin - thr * 0.55, 0)  # the game blooms HDR above 1.1 after exposure; this frame is not exposed the same, so the knee sits lower
    if night:
        bright = np.maximum(lin - 0.45, 0)
    glow = blur(bright, 14) * (inten * (2.2 if night else 1.0)) + blur(bright, 40) * (inten * 0.6)
    x = (lin + glow) * (2 ** post["colour"]["postExposure"])
    y = aces(x)
    g = srgb(y)
    luma = g @ np.array([0.2126, 0.7152, 0.0722], np.float32)
    sat = 1 + post["colour"]["saturation"] / 100
    g = luma[..., None] + (g - luma[..., None]) * sat
    con = 1 + post["colour"]["contrast"] / 100
    g = np.clip((g - 0.5) * con + 0.5, 0, 1)
    Image.fromarray((g * 255 + 0.5).astype(np.uint8)).save(d / f"{tag}.png")
    print("[post]", d / f"{tag}.png", "mean", np.round(g.reshape(-1, 3).mean(0) * 255).astype(int))


def font(px, bold=True):
    for p in ("/System/Library/Fonts/Supplemental/Arial Bold.ttf", "/System/Library/Fonts/Helvetica.ttc", "/Library/Fonts/Arial.ttf"):
        if Path(p).exists():
            try:
                return ImageFont.truetype(p, px)
            except OSError:
                pass
    return ImageFont.load_default()


def board(out, title, panels, width=3000, gap=16, head=64, label=40, rows=None):
    """panels: [(label, path)] laid in one row (or `rows` rows) of equal panels."""
    rows = rows or 1
    per = math.ceil(len(panels) / rows)
    pw = (width - gap * (per + 1)) // per
    ims = []
    for lab, p in panels:
        im = Image.open(p).convert("RGB")
        ims.append((lab, im.resize((pw, round(im.height * pw / im.width)), Image.LANCZOS)))
    ph = max(i.height for _, i in ims)
    H = head + rows * (ph + label + gap) + gap
    B = Image.new("RGB", (width, H), (23, 48, 47))
    dr = ImageDraw.Draw(B)
    dr.text((gap + 6, 16), title, font=font(30), fill=(247, 244, 236))
    for k, (lab, im) in enumerate(ims):
        r, c = divmod(k, per)
        x = gap + c * (pw + gap)
        y = head + r * (ph + label + gap)
        dr.text((x + 4, y + 6), lab, font=font(24), fill=(176, 205, 199))
        B.paste(im, (x, y + label))
    B.save(out)
    print("[post] board", out, B.size)


if __name__ == "__main__":
    a = sys.argv[1:]
    if a[0] == "frame":
        frame(a[1], a[2], "--haze" in a)
    elif a[0] == "board":
        rows = int(a[a.index("--rows") + 1]) if "--rows" in a else 1
        args = [x for i, x in enumerate(a) if x != "--rows" and (i == 0 or a[i - 1] != "--rows")]
        board(args[1], args[2], [tuple(x.split("=", 1)) for x in args[3:]], rows=rows)
