"""Crossing signal textures (brief 061, Vela, 2026-10-25): the SignalLens face atlas and the HaloRing's clock-ring map. No Blender: numpy and Pillow, python3 tools/decokit/signal_tex.py [out_dir] (run.ts does).

  signal_atlas_v1.png   256 x 256 RGBA, a 4 x 4 grid of 64 px glyph cells, white glyph on clear (alpha = the glyph): the lens faces' emissive atlas. The state's colour is the material's emission
                        colour, set per head by Latch; the glyph is the emission map and its own alpha clip, so nothing outside a glyph draws.
        cell 0 walk_full   1 stand_full     the figure, 0.40 m of a 0.50 m face quad (WALK, STOP)
             2 walk_head   3 stand_head     the small figure over the numerals (CLEARING, ARRIVING), 0.17 m, centred 0.145 m above the plate's middle
             4..13 digits "0".."9"          seven-segment, hexagonal bars (a 1950s station clock), 52 px tall in the middle 40 px of the cell
             14 blank      15 dash          the tens digit under ten is blank
  signal_ring_v1.png    512 x 512 RGBA, the HaloRing: 24 wedge segments between r 0.47 m and 0.60 m on a 1.24 m square, white, alpha = 1 - (k + 0.5) / 24 for segment k clockwise from the top (k = 0 is the
                        first to go dark when the ring drains) and 0 outside. Alpha clip with _Cutoff = 1 - lit / 24 lights exactly `lit` segments: the one fill property.
Pictograms are drawn here, not in Blender, so the figure is a rounded 1950s pictogram at 64 px a glyph rather than a few flat polygons.
Both go to <out>/signal_*.png (+ .meta: clamp, coverage-preserving mips, alpha is transparency) and signal-tex.json (the cell table crossing_lens.py and the Unity data read)."""

import json
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

sys.path.insert(0, str(Path(__file__).parent))
from ground_tex import OUT, meta_text  # noqa: E402

from signal_cells import *  # noqa: E402,F401,F403  (the cell table, shared with crossing_lens.py)


# ---------------------------------------------------------------- pictograms (unit height, y up, origin about the middle)
def _limb(d, a, b, w, S, o):
    (ax, ay), (bx, by) = a, b
    P = lambda p: (o[0] + p[0] * S, o[1] - p[1] * S)  # noqa: E731
    d.line([P(a), P(b)], fill=255, width=max(1, round(w * S)))
    r = w * S / 2
    for p in (a, b):
        x, y = P(p)
        d.ellipse([x - r, y - r, x + r, y + r], fill=255)


def figure(kind, d, S, o):
    """Draw a pictogram of height ~1 (S px) about the point o (px, the figure's middle)."""
    P = lambda p: (o[0] + p[0] * S, o[1] - p[1] * S)  # noqa: E731
    if kind == "walk":
        hx, hy, hr = 0.05, 0.43, 0.088
        _limb(d, (0.03, 0.27), (-0.02, 0.0), 0.17, S, o)                       # torso
        for a, b, c in (((-0.01, 0.01), (0.10, -0.22), (0.13, -0.48)), ((-0.01, 0.01), (-0.12, -0.22), (-0.25, -0.45))):   # the two legs, one knee each
            _limb(d, a, b, 0.115, S, o)
            _limb(d, b, c, 0.105, S, o)
        _limb(d, (0.03, 0.27), (-0.15, 0.07), 0.085, S, o)                    # arms swing against the legs
        _limb(d, (0.03, 0.27), (0.20, 0.09), 0.085, S, o)
    else:
        hx, hy, hr = 0.0, 0.43, 0.088
        _limb(d, (0.0, 0.28), (0.0, 0.0), 0.21, S, o)
        _limb(d, (-0.065, -0.03), (-0.065, -0.48), 0.105, S, o)
        _limb(d, (0.065, -0.03), (0.065, -0.48), 0.105, S, o)
        _limb(d, (-0.19, 0.25), (-0.20, -0.10), 0.08, S, o)
        _limb(d, (0.19, 0.25), (0.20, -0.10), 0.08, S, o)
    x, y = P((hx, hy))
    r = hr * S
    d.ellipse([x - r, y - r, x + r, y + r], fill=255)


# ---------------------------------------------------------------- seven-segment digits (crossing061's hexagonal bars)
SEG = {"0": "abcdef", "1": "bc", "2": "abdeg", "3": "abcdg", "4": "bcfg", "5": "acdfg", "6": "acdefg", "7": "abc", "8": "abcdefg", "9": "abcdfg"}


def _hseg(cx, cy, L, t):
    return [(cx - L / 2, cy), (cx - L / 2 + t / 2, cy - t / 2), (cx + L / 2 - t / 2, cy - t / 2), (cx + L / 2, cy), (cx + L / 2 - t / 2, cy + t / 2), (cx - L / 2 + t / 2, cy + t / 2)]


def _vseg(cx, cy, L, t):
    return [(cx, cy - L / 2), (cx + t / 2, cy - L / 2 + t / 2), (cx + t / 2, cy + L / 2 - t / 2), (cx, cy + L / 2), (cx - t / 2, cy + L / 2 - t / 2), (cx - t / 2, cy - L / 2 + t / 2)]


def digit(ch, d, h, o):
    """A digit h px tall (before supersample: callers pass px * SS) about o, y up on the page = minus y."""
    w, t = 0.58 * h, 0.17 * h
    Lh, Lv = w - t * 0.62, h / 2 - t * 0.62
    pos = {"a": (0, h / 2 - t / 2, "h"), "g": (0, 0, "h"), "d": (0, -h / 2 + t / 2, "h"), "f": (-w / 2 + t / 2, h / 4, "v"), "b": (w / 2 - t / 2, h / 4, "v"), "e": (-w / 2 + t / 2, -h / 4, "v"), "c": (w / 2 - t / 2, -h / 4, "v")}
    for sg in SEG[ch]:
        x, y, ori = pos[sg]
        poly = _hseg(x, y, Lh, t * 0.92) if ori == "h" else _vseg(x, y, Lv, t * 0.92)
        d.polygon([(o[0] + px, o[1] - py) for px, py in poly], fill=255)


def glyph(name):
    """One 64 px cell as a uint8 alpha plane (antialiased by supersampling)."""
    big = Image.new("L", (CELL * SS, CELL * SS), 0)
    d = ImageDraw.Draw(big)
    mid = CELL * SS / 2
    px = CELL / FACE_M                                                      # glyph px per metre of the face
    if name in ("walk_full", "stand_full"):
        figure(name.split("_")[0], d, 0.40 * px * SS, (mid, mid - 0.0 * SS))
    elif name in ("walk_head", "stand_head"):
        figure(name.split("_")[0], d, 0.17 * px * SS, (mid, mid - 0.145 * px * SS))
    elif name.isdigit():
        digit(name, d, DIGIT_PX_H * SS, (mid, mid))
    elif name == "dash":
        d.polygon([(mid - 9 * SS, mid - 3.5 * SS), (mid + 9 * SS, mid - 3.5 * SS), (mid + 9 * SS, mid + 3.5 * SS), (mid - 9 * SS, mid + 3.5 * SS)], fill=255)
    return np.asarray(big.resize((CELL, CELL), Image.BOX))


def atlas():
    a = np.zeros((ATLAS, ATLAS), np.uint8)
    for name in CELLS:
        (c, r), _ = cell_rect(name)
        a[r * CELL:(r + 1) * CELL, c * CELL:(c + 1) * CELL] = glyph(name)
    return np.dstack([np.full_like(a, 255), np.full_like(a, 255), np.full_like(a, 255), a])


def ring():
    """The clock ring: segment k (clockwise from the top) holds alpha round(255 * (1 - (k + 0.5) / 24)); outside the ring and in the gaps 0. Hard edges (no antialias): the alpha is a threshold map."""
    n = RING_PX
    ys, xs = np.mgrid[0:n, 0:n]
    x = (xs + 0.5) / n * 2 * RING_HALF - RING_HALF
    y = RING_HALF - (ys + 0.5) / n * 2 * RING_HALF
    r = np.hypot(x, y)
    clock = np.degrees(np.arctan2(x, y)) % 360.0                           # degrees clockwise from the top
    k = np.floor(clock / (360.0 / RING_N)).astype(int) % RING_N
    within = clock - k * (360.0 / RING_N)
    on = (r >= RING_R0) & (r <= RING_R1) & (within >= RING_GAP) & (within <= 360.0 / RING_N - RING_GAP)
    alpha = np.where(on, np.round(255 * (1 - (k + 0.5) / RING_N)), 0).astype(np.uint8)
    return np.dstack([np.full_like(alpha, 255)] * 3 + [alpha])


def meta(name, size, coverage):
    t = meta_text(name, "a", size)
    for old, new in (("wrapU: 0", "wrapU: 1"), ("wrapV: 0", "wrapV: 1"), ("alphaIsTransparency: 0", "alphaIsTransparency: 1")) + ((("mipMapsPreserveCoverage: 0", "mipMapsPreserveCoverage: 1"),) if coverage else ()):
        assert old in t, old
        t = t.replace(old, new)
    return t


def build():
    OUT.mkdir(parents=True, exist_ok=True)
    files = []
    for name, arr, cov in (("signal_atlas_v1.png", atlas(), True), ("signal_ring_v1.png", ring(), False)):
        im = Image.fromarray(arr, "RGBA")
        im.save(OUT / name, optimize=True)
        (OUT / f"{name}.meta").write_text(meta(name, im.size, cov))
        files.append(dict(file=name, size=list(im.size)))
    cells = {}
    for name in CELLS:
        (c, r), rect = cell_rect(name)
        cells[name] = dict(col=c, row=r, rect=[round(v, 6) for v in rect])
    (OUT / "signal-tex.json").write_text(json.dumps(dict(schema="suborned.signaltex/1", cell=CELL, grid=GRID, files=files, cells=cells,
                                                         ring=dict(n=RING_N, r0=RING_R0, r1=RING_R1, half=RING_HALF, px=RING_PX, gapDeg=RING_GAP)), indent=1))
    print(f"[signal-tex] {len(files)} files -> {OUT}")


if __name__ == "__main__":
    build()
