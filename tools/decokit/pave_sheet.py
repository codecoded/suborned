"""Paving sheets: coursed slab layouts cut from stone swatches -> albedo / normal / mask at 1 mm texels.

python3 Tools/Blender/decokit/pave_sheet.py [--only pave_flags,gutter_setts,granite_kerb]
Sources: SourceArt/DecoKit/Textures/src/<swatch>.png (Tools/Concept/swatches.py). Writes Textures/derived/:
  <name>.png         sRGB albedo
  <name>_normal.png  OpenGL tangent normal (green up) from the composed height field
  <name>_mask.png    URP mask: R metallic 0, G occlusion, A smoothness (Unity reads G as AO, A as smoothness)
Sheets are periodic in both axes, so the kit maps 2 m modules onto quadrants by world-grid parity and the layout
runs on seamlessly. Every slab gets its own stone crop, flip, tone, tilt and height; joints, chamfers and grout are
geometry in the height field, so the flat module shades like the modelled one (a 2-tri quad instead of ~440).
"""

import os
import sys
from pathlib import Path

import numpy as np
from PIL import Image

STORE_ART = Path(os.environ.get("SUBORNED_STORE", "~/Projects/the-suborned-store")).expanduser() / "source-art/DecoKit/Textures"
SRC = Path(os.environ.get("DECOKIT_SWATCHES", STORE_ART / "src"))      # swatches: the store, read-only
DER = Path(os.environ.get("DECOKIT_DER") or sys.exit("pave_sheet: set DECOKIT_DER to the output dir (the store is read-only)"))
PPM = 1024  # texels per metre (1 mm)

# courses: (height m, slab length m, offset m, swatch index, gain), bottom (road / v = 0) to top, repeating.
SHEETS = {
    "pave_flags": dict(
        size=(4.0, 4.0), swatches=("limestone_cream", "limestone_cream_b"), swatch_m=1.0,
        courses=[(0.5, 1.0, 0.0, None, 1.0), (0.5, 1.0, 0.5, None, 1.0)],
        # joints and tone sized for the play camera (~2.6 cm per pixel): a 5 mm joint in #8F877A grout darkened its
        # pixel ~11 levels and vanished; 10 mm in #716A5E darkens ~37, so the stretcher bond reads from the air
        joint=0.010, chamfer=0.005, recess=0.005, grout="#716A5E", tone=0.10, hue=0.04, heave=0.35,
        drop=0.0006, crown=0.0, jitter=0.0, relief=0.00006, detail=0.8, rough=0.42, rough_var=0.04, cloud=0.02, seed=7),
    "gutter_setts": dict(
        size=(4.0, 0.375), swatches=("granite_sett", "granite_kerb"), swatch_m=0.5,
        courses=[(0.25, 0.4, 0.0, 1, 0.82), (0.125, 0.125, 0.0625, 0, 1.42)],  # road-side blocks, kerb-side setts
        joint=0.007, chamfer=0.009, recess=0.007, grout="#5E5A55", tone=0.07, hue=0.01, heave=1.5,
        drop=0.0015, crown=0.004, jitter=0.005, relief=0.0004, detail=1.0, rough=0.62, rough_var=0.05, cloud=0.03, seed=11,
        tint=(0.86, 0.99, 1.13)),  # granite reads blue-grey against warm stone (study setts: B/R 1.14)
    # facade cladding on the kit's 1 m box-projected UVs (v up the wall): the polished limestone read as flat render
    # from the play camera; A/B candidates, same stone as the walk. A 2 m period repeats every few blocks, so the
    # joints carry it and the per-block tone stays calm (at 0.07 the repeat showed as stripes)
    "facade_ashlar": dict(
        size=(2.0, 2.0), swatches=("limestone_cream", "limestone_cream_b"), swatch_m=1.0,
        courses=[(0.5, 1.0, 0.0, None, 1.0), (0.5, 1.0, 0.5, None, 1.0)],
        joint=0.007, chamfer=0.004, recess=0.004, grout="#8C8474", tone=0.02, hue=0.01, heave=0.0,
        drop=0.0003, crown=0.0, jitter=0.0, relief=0.00006, detail=0.8, rough=0.5, rough_var=0.04, cloud=0.03, seed=17),
    # brief 008 civic paving: the 26 Sep paintings' large pale slabs, 1 x 2 m in running bond, soft joints, warm and cool slabs
    # like travertine; read at play zoom (2.6 cm per pixel: a 14 mm joint in #7A7266 grout keeps the bond visible)
    "pave_civic": dict(
        size=(4.0, 4.0), swatches=("limestone_cream", "limestone_cream_b"), swatch_m=1.0,
        courses=[(1.0, 2.0, 0.0, None, 1.0), (1.0, 2.0, 1.0, None, 1.0)],
        joint=0.014, chamfer=0.007, recess=0.006, grout="#7A7266", tone=0.05, hue=0.018, heave=0.25,
        drop=0.0005, crown=0.0, jitter=0.0, relief=0.00005, detail=0.8, rough=0.38, rough_var=0.05, cloud=0.025, seed=29),
    "facade_panel": dict(  # one slab per course: a second swatch would band the courses
        size=(2.0, 2.0), swatches=("limestone_cream",), swatch_m=1.0,
        courses=[(1.0, 2.0, 0.0, None, 1.0)],
        joint=0.009, chamfer=0.005, recess=0.005, grout="#857D6E", tone=0.02, hue=0.01, heave=0.0,
        drop=0.0003, crown=0.0, jitter=0.0, relief=0.00006, detail=0.8, rough=0.45, rough_var=0.04, cloud=0.03, seed=19),
}
# plain tileable stone (metric tiling on modelled pieces): swatch, metres per tile, relief m, roughness
# + texels per metre, + (saturation, gain, rgb tint)
TILES = {"granite_kerb": ("granite_kerb", 0.5, 0.0003, 0.6, 2048, (1.0, 1.0, (0.96, 1.0, 1.07))),  # 0.5 mm: 1-4 mm crystals
         # mid grey under the cream walk (the value ladder's "mid road"): at 1.04 it sat one step off the paving
         # and the streets read as more pavement from the play camera
         "asphalt_fine": ("asphalt_fine", 1.5, 0.0008, 0.72, 1024, (0.5, 0.74, (1.0, 0.99, 0.97)))}


# ----------------------------------------------------------------- image helpers

def blur(a, sigma):
    """Periodic gaussian (FFT); sigma in texels."""
    h, w = a.shape[:2]
    g = np.exp(-2 * (np.pi * sigma) ** 2 * (np.fft.fftfreq(h)[:, None] ** 2 + np.fft.rfftfreq(w)[None, :] ** 2))
    f = lambda x: np.fft.irfft2(np.fft.rfft2(x) * g, s=(h, w)).astype(np.float32)
    return f(a) if a.ndim == 2 else np.stack([f(a[..., c]) for c in range(a.shape[2])], -1)


def lum(a):
    return a[..., 0] * 0.2126 + a[..., 1] * 0.7152 + a[..., 2] * 0.0722


def tileable(a):
    """Separable variance-preserving cross-fade with a half-period roll: seams move to where the other copy rules."""
    for axis in (0, 1):
        n = a.shape[axis]
        w = np.sin(np.pi * (np.arange(n) + 0.5) / n) ** 2
        w = (w[:, None, None] if axis == 0 else w[None, :, None]).astype(np.float32)
        m = a.mean((0, 1), keepdims=True)
        b = np.roll(a, n // 2, axis)
        a = m + (w * (a - m) + (1 - w) * (b - m)) / np.sqrt(w ** 2 + (1 - w) ** 2)
    return a


def swatch(name, metres, ppm=PPM):
    """Swatch -> flattened (no lighting drift beyond ~0.3 m), tileable, resampled to PPM. Float sRGB 0..1."""
    im = Image.open(SRC / f"{name}.png").convert("RGB")
    n = round(metres * ppm)
    a = np.asarray(im.resize((n, n), Image.LANCZOS), np.float32) / 255
    l = lum(a)
    a *= (l.mean() / np.maximum(blur(l, 0.3 * ppm), 1e-3))[..., None]
    return tileable(a).clip(0, 1)


def srgb(hexstr):
    h = hexstr.lstrip("#")
    return np.array([int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)], np.float32)


def smooth(t):
    t = np.clip(t, 0, 1)
    return t * t * (3 - 2 * t)


def normal_from(h, ppm=PPM):
    """Height (m) -> OpenGL tangent normal; image row 0 is the top (v = 1), so +v is up the image."""
    px = 1 / ppm
    dx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) / (2 * px)
    dy = (np.roll(h, 1, 0) - np.roll(h, -1, 0)) / (2 * px)
    n = np.stack([-dx, -dy, np.ones_like(h)], -1)
    return n / np.linalg.norm(n, axis=-1, keepdims=True)


def save(name, albedo, n, ao, smooth_map):
    DER.mkdir(parents=True, exist_ok=True)
    q = lambda x: (np.clip(x, 0, 1) * 255 + 0.5).astype(np.uint8)
    Image.fromarray(q(albedo), "RGB").save(DER / f"{name}_v1.png")
    Image.fromarray(q(n * 0.5 + 0.5), "RGB").save(DER / f"{name}_v1_normal.png")
    z = np.zeros_like(ao)
    Image.fromarray(q(np.stack([z, ao, z, smooth_map], -1)), "RGBA").save(DER / f"{name}_v1_mask.png")
    print(f"[pave] {name}_v1 {albedo.shape[1]}x{albedo.shape[0]} albedo mean {np.round(albedo.mean((0, 1)) * 255)} "
          f"smooth {smooth_map.mean():.2f} ao {ao.mean():.2f}")


# ----------------------------------------------------------------- layout

def layout(spec):
    """Per-texel slab id, slab-local (u, v) in m, slab half sizes, course swatch/gain. y is up (v = 0 at the bottom)."""
    wm, hm = spec["size"]
    W, H = round(wm * PPM), round(hm * PPM)
    period = sum(c[0] for c in spec["courses"])
    reps = round(hm / period)
    assert abs(reps * period - hm) < 1e-6, "course pattern must divide the sheet height"
    rows = []
    for r in range(reps):
        for i, (ch, cl, off, src, gain) in enumerate(spec["courses"]):
            assert abs(wm / cl - round(wm / cl)) < 1e-6, "slab length must divide the sheet width"
            rows.append((ch, cl, off, src, gain))
    y = hm - (np.arange(H) + 0.5) / PPM
    edges = np.cumsum([0.0] + [c[0] for c in rows])
    ci = np.clip(np.searchsorted(edges, y, side="right") - 1, 0, len(rows) - 1)
    ch, cl, off = (np.array([c[k] for c in rows], np.float32)[ci] for k in range(3))
    src = np.array([-1 if c[3] is None else c[3] for c in rows])[ci]
    gain = np.array([c[4] for c in rows], np.float32)[ci]
    cy = (edges[ci] + ch / 2).astype(np.float32)
    x = ((np.arange(W) + 0.5) / PPM).astype(np.float32)[None, :]
    xs = x - off[:, None]
    k = np.floor(xs / cl[:, None])
    u = xs - (k + 0.5) * cl[:, None]
    v = np.broadcast_to((y - cy)[:, None], u.shape)
    ncol = np.round(wm / cl).astype(np.int64)
    sid = ci[:, None].astype(np.int64) * 4096 + np.mod(k.astype(np.int64), ncol[:, None])
    return dict(W=W, H=H, sid=sid, u=u, v=v, hl=np.broadcast_to(cl[:, None] / 2, u.shape),
                hh=np.broadcast_to(ch[:, None] / 2, u.shape), src=np.broadcast_to(src[:, None], u.shape),
                gain=np.broadcast_to(gain[:, None], u.shape))


# ----------------------------------------------------------------- composition

def compose(name, spec):
    L = layout(spec)
    rng = np.random.default_rng(spec["seed"])
    uniq, inv = np.unique(L["sid"], return_inverse=True)
    inv = inv.reshape(L["sid"].shape)
    n = len(uniq)
    stack = np.stack([swatch(s, spec["swatch_m"]) for s in spec["swatches"]])  # (S, N, N, 3)
    ns = stack.shape[1]
    # per-slab draws
    pick = rng.integers(0, len(spec["swatches"]), n)
    flip = rng.choice([-1, 1], (n, 2))
    offs = rng.integers(0, ns, (n, 2))
    tone = 1 + spec["tone"] * np.clip(rng.standard_normal(n), -2, 2) / 2
    hue = np.clip(rng.standard_normal(n), -2, 2) / 2  # t in [-1, 1]: + warm (peach), - cool (sand-grey)
    tilt = np.radians(spec["heave"]) * np.clip(rng.standard_normal((n, 2)), -2, 2) / 2
    drop = spec["drop"] * np.clip(rng.standard_normal(n), -2, 2) / 2
    rvar = spec["rough_var"] * np.clip(rng.standard_normal(n), -2, 2) / 2
    jit = spec["jitter"] * rng.random((n, 4))  # hand-cut setts: each edge pulled in 0..jitter (left, right, bottom, top)

    u, v, hl, hh = L["u"], L["v"], L["hl"], L["hh"]
    s_idx = np.where(L["src"] >= 0, L["src"], pick[inv])
    sx = np.mod(offs[inv, 0] + np.round(flip[inv, 0] * u * PPM).astype(np.int64), ns)
    sy = np.mod(offs[inv, 1] - np.round(flip[inv, 1] * v * PPM).astype(np.int64), ns)
    stone = stack[s_idx, sy, sx]  # (H, W, 3)
    del sx, sy

    # detail: stone luminance high-pass drives micro relief + micro roughness; contrast calmed toward the mean
    sl = lum(stone)
    detail = (sl - blur(sl, 6)) / max(float(np.std(sl)), 1e-3)
    mean = stone.reshape(-1, 3).mean(0)
    stone = mean + (stone - mean) * spec["detail"]
    k = spec["hue"] / 0.02  # warm / cool vectors are the full shift at hue 0.02, t = +-1
    warm, cool = 1 + k * np.array([0.0, -0.025, -0.07], np.float32), 1 + k * np.array([-0.025, -0.015, 0.0], np.float32)
    t = hue[inv][..., None]
    tint = np.where(t > 0, 1 + t * (warm - 1), 1 - t * (cool - 1))
    cloud = blur(rng.standard_normal((L["H"], L["W"])).astype(np.float32), 0.06 * PPM)
    cloud *= spec["cloud"] / max(float(np.std(cloud)), 1e-6)
    albedo = stone * (tone[inv] * L["gain"] * (1 + cloud))[..., None] * tint * np.array(spec.get("tint", (1, 1, 1)), np.float32)
    del stone, tint

    # geometry: distance to the slab's joint edge, 45 deg chamfer, per-slab tilt / drop, sett crown, micro relief
    j = jit[inv]
    d = np.minimum(np.minimum(hl + u - j[..., 0], hl - u - j[..., 1]),
                   np.minimum(hh + v - j[..., 2], hh - v - j[..., 3])) - spec["joint"] / 2
    del j
    cov = np.clip(d * PPM + 0.5, 0, 1)  # antialiased slab coverage
    c = spec["chamfer"]
    ch = 0.75 * (c - np.clip(d, 0, c)) ** 2 / c  # rounded arris: 63 deg at the joint, tangent to the top at c
    crown = spec["crown"] * (1 - np.clip(u / hl, -1, 1) ** 2) * (1 - np.clip(v / hh, -1, 1) ** 2)
    slab_h = drop[inv] + np.tan(tilt[inv, 0]) * u + np.tan(tilt[inv, 1]) * v + crown - ch + spec["relief"] * detail
    grout_n = blur(rng.standard_normal((L["H"], L["W"])).astype(np.float32), 1.2)
    grout_n /= max(float(np.std(grout_n)), 1e-6)
    h = cov * slab_h + (1 - cov) * (-spec["recess"] + 0.0002 * grout_n)

    # grout albedo, a faint dust line along the arrises (clean, not dirty)
    g = srgb(spec["grout"]) * (1 + 0.05 * grout_n)[..., None]
    albedo *= (1 - 0.035 * np.exp(-np.maximum(d, 0) / 0.004))[..., None]
    albedo = cov[..., None] * albedo + (1 - cov[..., None]) * g

    cav = blur(h, 3) - h + 0.5 * (blur(h, 10) - h)
    ao = np.clip(1 - 180 * np.maximum(cav, 0), 0.45, 1)
    rough = cov * (spec["rough"] + rvar[inv] + 0.03 * np.clip(detail, -2, 2)) + (1 - cov) * 0.92
    save(name, albedo, normal_from(h), ao, 1 - np.clip(rough, 0.05, 0.98))


def tile(name, src, metres, relief, rough, ppm, grade=(1.0, 1.0, (1.0, 1.0, 1.0))):
    a = swatch(src, metres, ppm)
    g = lum(a)[..., None]
    a = ((g + (a - g) * grade[0]) * grade[1] * np.array(grade[2], np.float32)).clip(0, 1)
    l = lum(a)
    detail = (l - blur(l, 4)) / max(float(np.std(l)), 1e-3)
    h = relief * np.clip(detail, -3, 3)
    ao = np.clip(1 - 150 * np.maximum(blur(h, 3) - h, 0), 0.6, 1)
    save(name, a, normal_from(h, ppm), ao, np.full_like(l, 1 - rough) - 0.04 * np.clip(detail, -2, 2))


def main():
    only = sys.argv[sys.argv.index("--only") + 1].split(",") if "--only" in sys.argv else None
    for name, spec in SHEETS.items():
        if only is None or name in only:
            compose(name, spec)
    for name, args in TILES.items():
        if only is None or name in only:
            tile(name, *args)


if __name__ == "__main__":
    main()
