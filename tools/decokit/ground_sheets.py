"""Ground sheets read from the 2.5D play camera (ortho 14 at 38 deg: ~2.6 cm per pixel), where 1 mm stone grain
averages away and only metre-scale structure carries: striped lawn, terrazzo laid in brass-divided panels, a road
surface with patch repairs, sealed cracks and oil stains.

python3 Tools/Blender/decokit/ground_sheets.py [--only lawn,terrazzo_ivory,terrazzo_seafoam,asphalt_road]
Sources: Textures/src/lawn_turf, lawn_clover, asphalt_fine (Tools/Concept/swatches.py); Textures/gen/terrazzo_*_v1.png.
Writes Textures/derived/<name>_v1.png (+ _normal for the lawn); copy them to Assets/TheSuborned/Art/DecoKit/Textures.
"""

import sys
from pathlib import Path

import numpy as np
from PIL import Image

from pave_sheet import DER, blur, lum, normal_from, smooth, srgb, swatch

GEN = Path(__file__).resolve().parents[3] / "SourceArt/DecoKit/Textures/gen"

# lawn: 8 m periodic sheet at 4 mm; turf and clover swatches 2 m; mown stripes 1 m, light ones yellower
LAWN = dict(metres=8.0, ppm=256, swatch_m=2.0, stripe_m=1.0, stripe=0.075, stripe_edge=0.06, clover=0.16,
            clover_blob_m=0.3, tone=0.05, relief=0.004, mean="#64844A", saturation=0.85, seed=5)
# road: 16 m periodic sheet at 8 mm (patches must not repeat along a block), graded like pave_sheet's asphalt_fine
ROAD = dict(metres=16.0, ppm=128, swatch_m=2.0, grade=(0.5, 0.74, (1.0, 0.99, 0.97)), tone=0.035, patches=4,
            patch_tone=0.05, seams=5, stains=9, relief=0.0008, seed=23)
# terrazzo: the kit's 1.5 m tile gets a brass divider strip on its edges (flush, so albedo only)
TERRAZZO = dict(strip_m=0.010, brass="#A8874E", line="#6B5A3C", line_m=0.0015)


def periodic_noise(n, sigma, rng):
    a = blur(rng.standard_normal((n, n)).astype(np.float32), sigma)
    return a / max(float(np.std(a)), 1e-6)


def tile_to(a, n, rng):
    """Tile a square swatch over n x n texels, each copy flipped at random (breaks the 2 m repeat)."""
    s = a.shape[0]
    reps = n // s
    rows = []
    for _ in range(reps):
        row = []
        for _ in range(reps):
            b = a[::-1] if rng.random() < .5 else a
            row.append(b[:, ::-1] if rng.random() < .5 else b)
        rows.append(np.concatenate(row, 1))
    return np.concatenate(rows, 0)


def lawn(spec=LAWN):
    rng = np.random.default_rng(spec["seed"])
    n, ppm = round(spec["metres"] * spec["ppm"]), spec["ppm"]
    turf = tile_to(swatch("lawn_turf", spec["swatch_m"], ppm), n, rng)
    clover = tile_to(swatch("lawn_clover", spec["swatch_m"], ppm), n, rng)
    clover *= turf.mean((0, 1)) / clover.mean((0, 1))  # drifts differ in texture, not in colour
    # clover in soft drifts: thresholded low-frequency noise at the target coverage
    blob = periodic_noise(n, spec["clover_blob_m"] * ppm, rng)
    cut = np.quantile(blob, 1 - spec["clover"])
    m = smooth((blob - cut) / 0.6)[..., None]
    a = turf * (1 - m) + clover * m
    # mown stripes along v: blades laid toward / away from the eye; light bands warmer, dark bands bluer
    y = (np.arange(n) + 0.5) / ppm
    ph = np.mod(y / spec["stripe_m"], 2.0)
    e = spec["stripe_edge"] / spec["stripe_m"]
    s = (smooth(ph / e) - smooth((ph - 1) / e)) * 2 - 1  # +1 light, -1 dark, soft edges
    s = s[:, None, None]
    a = a * (1 + spec["stripe"] * s) * (1 + 0.02 * s * np.array([1.0, 0.2, -1.2], np.float32))
    a = a * (1 + spec["tone"] * periodic_noise(n, 1.6 * ppm, rng))[..., None]
    g = lum(a)[..., None]
    a = g + (a - g) * spec["saturation"]
    a = a * srgb(spec["mean"]) / a.mean((0, 1))  # the palette's sage-leaning green, not a sports pitch
    l = lum(a)
    detail = (l - blur(l, 3)) / max(float(np.std(l)), 1e-3)
    save_rgb("lawn", a)
    nrm = normal_from(spec["relief"] * np.clip(detail, -3, 3), ppm)
    Image.fromarray(q(nrm * 0.5 + 0.5)).save(DER / "lawn_v1_normal.png")
    print(f"[ground] lawn_v1 {n}x{n} mean {np.round(a.mean((0, 1)) * 255)}")


def road(spec=ROAD):
    rng = np.random.default_rng(spec["seed"])
    n, ppm = round(spec["metres"] * spec["ppm"]), spec["ppm"]
    a = tile_to(swatch("asphalt_fine", spec["swatch_m"], ppm), n, rng)
    sat, gain, tint = spec["grade"]
    g = lum(a)[..., None]
    a = (g + (a - g) * sat) * gain * np.array(tint, np.float32)
    a = a * (1 + spec["tone"] * periodic_noise(n, 2.0 * ppm, rng))[..., None]
    yy, xx = np.mgrid[0:n, 0:n].astype(np.float32) / ppm
    wrap = lambda d: d - spec["metres"] * np.round(d / spec["metres"])  # periodic offsets
    shade = np.ones((n, n), np.float32)
    for _ in range(spec["patches"]):  # relaid rectangles: a flat tone step with a dark sealed edge
        cx, cy = rng.random(2) * spec["metres"]
        hw, hh = rng.uniform(0.6, 1.8), rng.uniform(0.5, 1.2)
        d = np.maximum(np.abs(wrap(xx - cx)) - hw, np.abs(wrap(yy - cy)) - hh)
        inside = np.clip(-d * ppm + 0.5, 0, 1)
        shade *= 1 + rng.choice([-1, 1]) * spec["patch_tone"] * rng.uniform(0.5, 1) * inside
        shade *= 1 - 0.18 * np.clip(1 - np.abs(d) / 0.012, 0, 1)
    for _ in range(spec["seams"]):  # sealed cracks: meandering 10 mm tar lines
        x, y, h = *(rng.random(2) * spec["metres"]), rng.uniform(0, 2 * np.pi)
        for _ in range(int(rng.uniform(1.5, 4) * ppm / 4)):
            h += rng.normal(0, 0.18)
            x, y = x + np.cos(h) * 4 / ppm, y + np.sin(h) * 4 / ppm
            i, j = int(y * ppm) % n, int(x * ppm) % n
            shade[i - 1:i + 1, j - 1:j + 1] *= 0.72
    for _ in range(spec["stains"]):  # oil drips: soft dark blobs
        cx, cy = rng.random(2) * spec["metres"]
        r = rng.uniform(0.12, 0.35)
        shade *= 1 - 0.1 * np.exp(-(wrap(xx - cx) ** 2 + wrap(yy - cy) ** 2) / (2 * r * r))
    a = a * shade[..., None]
    l = lum(a)
    detail = (l - blur(l, 2)) / max(float(np.std(l)), 1e-3)
    save_rgb("asphalt_road", a)
    Image.fromarray(q(normal_from(spec["relief"] * np.clip(detail, -3, 3), ppm) * 0.5 + 0.5)).save(
        DER / "asphalt_road_v1_normal.png")
    print(f"[ground] asphalt_road_v1 {n}x{n} mean {np.round(a.mean((0, 1)) * 255)}")


def terrazzo(name, spec=TERRAZZO):
    src = np.asarray(Image.open(GEN / f"{name}_v1.png").convert("RGB"), np.float32) / 255
    n = src.shape[0]
    ppm = n / 1.5
    x = (np.arange(n) + 0.5) / ppm
    d = np.minimum(x, 1.5 - x)  # distance to the tile edge (the strip straddles it: half on each side)
    dist = np.minimum(d[:, None], d[None, :])
    half = spec["strip_m"] / 2
    strip = np.clip((half - dist) * ppm + 0.5, 0, 1)[..., None]
    line = np.clip((spec["line_m"] - np.abs(dist - half)) * ppm + 0.5, 0, 1)[..., None]
    a = src * (1 - strip) + srgb(spec["brass"]) * strip
    a = a * (1 - line) + srgb(spec["line"]) * line
    save_rgb(name, a)
    print(f"[ground] {name}_v1 {n}x{n} brass strip {spec['strip_m'] * 1000:.0f} mm every 1.5 m")


def q(x):
    return (np.clip(x, 0, 1) * 255 + 0.5).astype(np.uint8)


def save_rgb(name, a):
    DER.mkdir(parents=True, exist_ok=True)
    Image.fromarray(q(a), "RGB").save(DER / f"{name}_v1.png")


def main():
    only = sys.argv[sys.argv.index("--only") + 1].split(",") if "--only" in sys.argv else None
    jobs = {"lawn": lawn, "asphalt_road": road, "terrazzo_ivory": lambda: terrazzo("terrazzo_ivory"),
            "terrazzo_seafoam": lambda: terrazzo("terrazzo_seafoam")}
    for k, f in jobs.items():
        if only is None or k in only:
            f()


if __name__ == "__main__":
    main()
