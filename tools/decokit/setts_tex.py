"""Granite setts for the tram lanes (brief 075 golden view, Vela): DK_Ground_Setts, ground_setts_v1[_normal|_mask].png.

python3 tools/decokit/setts_tex.py [out_dir]     (no bpy; default out/decokit-ground beside the ground set: export_kit copies every ground_* file, mats.tex_path finds them).

Made here, seeded (no source sheet): small grey-blue granite setts (9 cm on the radius, ~10 cm along the arc) laid in a fan (segmental arc) bond, the European tram street's paving:
fans 1.0 m across, rows 0.5 m apart, alternate rows offset half a fan; each fan's setts run in rings round a centre above it, staggered ring to ring. The pattern tiles in 2 x 2 m
(two fans by four rows). Colour: the stone is a cool blue-grey that sits under the powder-blue road (#8FAFC1) without going railway dark, with a share of warm grey and fawn setts
for the old reel's warmth, a granite speckle in every sett, and sand-grey joints (8 mm). Mean re-centred on SETTS_HEX.
Maps (the ground set's packing, ground_tex.meta_text): albedo 1024 (1.95 mm a texel over the 2 m tile; play zoom is ~2.6 cm a pixel, so the fans and the per-sett tone read, the joints
fall into the mips as a darker line), normal 1024 (each sett's domed top and pitched edge), mask 512 (R metallic 0, G occlusion in the joints, A smoothness .30 worn tops to .08 joints).
Budget: 1024^2 + 1024^2 + 512^2 = 2.36 M texels, BC7/BC5 1 byte a texel, with mips 3.0 MiB. The .mat uses the same keywords as DK_Ground_Pavement (albedo, normal, mask; no detail
pair): no new shader variant.
"""

import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).parent))
from ground_tex import meta_text  # noqa: E402

REPO = Path(__file__).resolve().parents[2]
OUT = Path(sys.argv[1] if len(sys.argv) > 1 else REPO / "out/decokit-ground")

SETTS_HEX = "#8F9498"   # mean albedo: blue-grey granite, a step darker than the road's #8FAFC1 so the lane reads, far from the old bed's near-black
TILE = 2.0              # metres a repeat
N, M = 1024, 512        # albedo / normal px, mask px
FAN_W, ROW_H, FAN_R = 1.0, 0.5, 0.72   # fan period across, row pitch, fan radius
SETT = 0.09             # ring width (radial sett size); FAN_R / SETT = 8 rings
ALONG = 0.10            # target sett length along the arc
JOINT = 0.008           # joint width
STONES = [("#8E959C", 0.42), ("#979797", 0.24), ("#9D958B", 0.18), ("#868C92", 0.11), ("#A7A093", 0.05)]  # blue-grey, grey, warm grey, dark blue-grey, fawn
JOINT_HEX = "#837C71"   # sand-grey mortar


def s2l(c):
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def l2s(c):
    c = np.clip(c, 0, 1)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * c ** (1 / 2.4) - 0.055)


def hexrgb(h):
    h = h.lstrip("#")
    return np.array([int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)])


def _blur(a, sigma):
    n = a.shape[0]
    f = np.fft.fftfreq(n)
    k = np.exp(-2 * (np.pi * sigma) ** 2 * (f[:, None] ** 2 + f[None, :] ** 2))
    return np.fft.ifft2(np.fft.fft2(a) * k).real


def _hash(*ks):
    """Deterministic 0..1 per integer key tuple (vectorised)."""
    h = np.uint64(1469598103934665603)
    out = np.full(np.broadcast(*ks).shape, h, dtype=np.uint64)
    for k in ks:
        out ^= (np.asarray(k).astype(np.int64) & 0xFFFFFFFF).astype(np.uint64)
        out *= np.uint64(1099511628211)
        out ^= out >> np.uint64(29)
    return (out % np.uint64(1 << 24)).astype(np.float64) / (1 << 24)


def layout(n):
    """Per pixel (metres over the 2 m tile): sett id (fan col, row, ring, index), distance to the nearest joint centre (m)."""
    px = TILE / n
    ys, xs = np.mgrid[0:n, 0:n]
    x, y = (xs + 0.5) * px, (ys + 0.5) * px
    best_j = np.full((n, n), -10**6, np.int64)
    col = np.zeros((n, n), np.int64)
    dx = np.zeros((n, n)); dy = np.zeros((n, n))
    j0 = int(np.floor(-FAN_R / ROW_H)) - 1
    j1 = int(np.ceil((TILE + FAN_R) / ROW_H)) + 1
    for j in range(j0, j1 + 1):  # the highest row whose fan holds the point wins (later rows lie over earlier ones)
        off = (j % 2) * FAN_W / 2
        i = np.round((x - off) / FAN_W)
        cx, cy = i * FAN_W + off, j * ROW_H
        ddx, ddy = x - cx, y - cy
        inside = ddx ** 2 + ddy ** 2 <= FAN_R ** 2
        take = inside & (j > best_j)
        best_j = np.where(take, j, best_j); col = np.where(take, i, col)
        dx = np.where(take, ddx, dx); dy = np.where(take, ddy, dy)
    assert (best_j > -10**6).all(), "fan radius leaves gaps"
    # distance to the boundary of any higher-row fan (where this fan's setts are cut off)
    cut = np.full((n, n), 9.0)
    for dj in (1, 2):
        j = best_j + dj
        off = (j % 2) * FAN_W / 2
        cyj = j * ROW_H
        cy0 = best_j * ROW_H
        cx0 = col * FAN_W + (best_j % 2) * FAN_W / 2
        px_, py_ = cx0 + dx, cy0 + dy
        for di in (-1, 0, 1):
            cx = np.round((px_ - off) / FAN_W) * FAN_W + off + di * FAN_W
            d = np.abs(np.hypot(px_ - cx, py_ - cyj) - FAN_R)
            cut = np.minimum(cut, d)
    # polar about the fan's centre, angle from straight down (the fan opens downward: -y)
    r = np.hypot(dx, dy)
    th = np.arctan2(dx, -dy)
    ring = np.minimum(np.floor(r / SETT), FAN_R / SETT - 1).astype(np.int64)
    rm = (ring + 0.5) * SETT
    dth = ALONG / rm
    stag = 0.5 * (ring % 2)
    a = th / dth + stag
    idx = np.floor(a).astype(np.int64)
    d_rad = np.minimum(r - ring * SETT, (ring + 1) * SETT - r)
    d_arc = np.minimum(a - idx, idx + 1 - a) * dth * r
    d_out = FAN_R - r
    dist = np.minimum.reduce([d_rad, d_arc, d_out, cut])
    jrow = np.mod(best_j, int(round(TILE / ROW_H)))  # four rows a tile
    jcol = np.mod(col, int(round(TILE / FAN_W)))
    return (jcol, jrow, ring, idx), dist, px


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    rng = np.random.default_rng(75)
    ids, dist, px = layout(N)
    u = _hash(*ids, 1)
    v = _hash(*ids, 2)
    w = _hash(*ids, 3)
    # stone class per sett
    cum = np.cumsum([p for _, p in STONES]); cum /= cum[-1]
    cls = np.searchsorted(cum, u)
    stone = np.stack([s2l(hexrgb(h)) for h, _ in STONES])[cls]
    stone *= (0.95 + 0.10 * v)[..., None]                                       # per-sett tone
    grain = rng.standard_normal((N, N))
    speck = (_blur(grain, 0.7) / 0.3)                                           # granite speckle, ~2-4 mm
    speck = np.clip(speck, -3, 3)
    stone *= (1 + 0.07 * speck + 0.06 * (_blur(rng.standard_normal((N, N)), 1.6) / 0.17).clip(-3, 3) * 0.3)[..., None]
    dark = (rng.random((N, N)) < 0.035) * 0.35                                  # black mica flecks
    stone *= (1 - _blur(dark, 0.6) * 1.5).clip(0.55, 1)[..., None]
    jw = JOINT / 2
    edge = np.clip((dist - jw) / 0.008, 0, 1)                                    # 0 in the joint .. 1 on the sett top (8 mm arris)
    top = edge ** 0.6
    mortar = s2l(hexrgb(JOINT_HEX)) * (0.9 + 0.2 * _blur(rng.random((N, N)), 3.0)[..., None])
    lin = mortar * (1 - top[..., None]) + stone * (0.86 + 0.14 * top)[..., None] * top[..., None]
    # tone drift over metres (worn lanes), periodic
    drift = _blur(rng.standard_normal((N, N)), 90.0); drift /= drift.std() + 1e-9
    lin *= (1 + 0.035 * drift)[..., None]
    mean = lin.reshape(-1, 3).mean(0)
    lin = lin / mean * s2l(hexrgb(SETTS_HEX))
    Image.fromarray((l2s(lin) * 255 + 0.5).astype(np.uint8)).save(OUT / "ground_setts_v1.png")
    # height: domed sett top, pitched arris, tilted a little per sett; normal from it (same convention as road_tex tileteal)
    dome = np.clip(dist / (SETT / 2), 0, 1)
    h = (0.6 * top + 0.4 * top * dome ** 0.5) * 6.0                             # in texels-ish, scaled for the gradient
    h += top * (w - 0.5) * 1.2
    gx, gy = np.gradient(h)
    nrm = np.dstack([-gy * 0.35, -gx * 0.35, np.ones_like(h)])
    nrm /= np.linalg.norm(nrm, axis=2, keepdims=True)
    Image.fromarray(((nrm * 0.5 + 0.5) * 255 + 0.5).astype(np.uint8)).save(OUT / "ground_setts_v1_normal.png")
    # mask at M px
    ids_m, dist_m, _ = layout(M)
    top_m = np.clip((dist_m - jw) / 0.008, 0, 1) ** 0.6
    wear = _hash(*ids_m, 4)
    mk = np.zeros((M, M, 4), np.uint8)
    mk[..., 1] = (255 * (0.55 + 0.45 * top_m)).astype(np.uint8)                 # occlusion in the joints
    mk[..., 3] = (255 * (0.08 + top_m * (0.18 + 0.12 * wear))).astype(np.uint8)  # smoothness: joints .08, tops .26-.38
    Image.fromarray(mk, "RGBA").save(OUT / "ground_setts_v1_mask.png")
    for suffix, kind, p in (("", "a", N), ("_normal", "n", N), ("_mask", "m", M)):
        (OUT / f"ground_setts_v1{suffix}.png.meta").write_text(meta_text(f"ground_setts_v1{suffix}.png", kind, (p, p)))
    t = N * N * 2 + M * M
    m = l2s(s2l(np.asarray(Image.open(OUT / "ground_setts_v1.png").convert("RGB")).astype(np.float32) / 255).reshape(-1, 3).mean(0))
    rep = {"setts": dict(source="generated (seed 75): fan bond granite setts", brief="075 golden", target=SETTS_HEX,
                         meanAfter="#%02X%02X%02X" % tuple(int(round(c * 255)) for c in m), tile=[TILE, TILE], fan=dict(width=FAN_W, row=ROW_H, radius=FAN_R),
                         sett=dict(radial=SETT, along=ALONG, joint=JOINT), px=dict(albedo=N, normal=N, mask=M), texels=t, bc7MiBWithMips=round(t * 4 / 3 / 2 ** 20, 2))}
    (OUT / "setts.json").write_text(json.dumps(rep, indent=1))
    print("[setts_tex]", OUT, rep["setts"]["meanAfter"], rep["setts"]["bc7MiBWithMips"], "MiB")


if __name__ == "__main__":
    main()
