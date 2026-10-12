"""Space-age sky textures (target: Concepts/Horizon/sky_ab/celestial.png) -> Art/DecoKit/Textures/Sky/.
Soft airbrush finish: every field is low-frequency and smoothly graded; the sky shader does the lighting.

  DK_Planet_Bands.png   2048x1024 lat-long gas giant: cream / ochre / dusty-rose bands warped by flowing turbulence
  DK_Planet_Ring.png    1024x8 radial ring profile (inner -> outer): RGB tint, A density with gaps
  DK_Moon.png           1024x512 lat-long chalky moon: maria, craters, rays
  DK_Sky_Noise.png      512x512 tileable fbm (R low, G mid, B high, A ridged) for cirrus
  DK_Flare_Starburst.png 1024x1024 sun starburst: 16 tapered rays + soft core (lens flare image element)
  DK_Skyline_Far.png    8192x512 360-degree far-city band, elevation -1..+9 deg: three depth layers of towers, disc
                        towers, setbacks, spires and domes, softer and paler with distance; linear RGB stored as a
                        ratio to the haze (/ BAND_GAIN) so the shader tints it with its own horizon (DK_SpaceSky);
                        the east (world compass SEA_AZ) is open sea: no towers, only the horizon haze
  DK_Sea_Normal.png     512x512 tileable wind-wave slopes for DK_Ocean: RG = 0.5 + slope/2 (x, z), B = 1

python3 Tools/Blender/decokit/sky_sheet.py   (numpy + Pillow only; no Blender)
"""

from pathlib import Path

import numpy as np
from PIL import Image

OUT = Path(__file__).resolve().parents[3] / "Assets/TheSuborned/Art/DecoKit/Textures/Sky"
RNG = np.random.default_rng(1951)


def periodic_noise(n, beta, seed):
    """Tileable 1/f^beta noise (random-phase spectrum), normalised to 0..1."""
    r = np.random.default_rng(seed)
    f = np.fft.fftfreq(n)[:, None] ** 2 + np.fft.fftfreq(n)[None, :] ** 2
    f[0, 0] = 1
    spec = (r.normal(size=(n, n)) + 1j * r.normal(size=(n, n))) / f ** (beta / 2)
    spec[0, 0] = 0
    v = np.real(np.fft.ifft2(spec))
    return (v - v.min()) / (v.max() - v.min())


def wrap_sample(field, u, v):
    """Bilinear sample of a tileable field at fractional coords (wrapping)."""
    h, w = field.shape
    x, y = (u % 1) * w, (v % 1) * h
    x0, y0 = np.floor(x).astype(int), np.floor(y).astype(int)
    fx, fy = x - x0, y - y0
    x1, y1 = (x0 + 1) % w, (y0 + 1) % h
    x0, y0 = x0 % w, y0 % h
    return (field[y0, x0] * (1 - fx) * (1 - fy) + field[y0, x1] * fx * (1 - fy)
            + field[y1, x0] * (1 - fx) * fy + field[y1, x1] * fx * fy)


def save(name, rgb, alpha=None):
    OUT.mkdir(parents=True, exist_ok=True)
    a = np.clip(rgb, 0, 1)
    if alpha is not None:
        a = np.dstack([a, np.clip(alpha, 0, 1)])
    Image.fromarray((a * 255 + 0.5).astype(np.uint8)).save(OUT / name)
    print(f"[sky] {name} {a.shape[1]}x{a.shape[0]}")


def planet_bands(w=2048, h=1024):
    lon, lat = np.meshgrid(np.linspace(0, 1, w, endpoint=False), np.linspace(-1, 1, h))
    warp = periodic_noise(512, 2.6, 11) - 0.5
    fine = periodic_noise(512, 2.0, 12) - 0.5
    # flowing turbulence: latitude displaced by longitude-stretched noise, stronger near the band edges
    t = lat + 0.07 * wrap_sample(warp, lon * 2, lat * 0.9) + 0.02 * wrap_sample(fine, lon * 6, lat * 4)
    bands = (0.5 + 0.3 * np.sin(t * 8.5 + 0.6) + 0.14 * np.sin(t * 21.0 + 2.1) + 0.06 * np.sin(t * 47.0 + 0.3))
    pal = np.array([[0.93, 0.88, 0.78], [0.85, 0.70, 0.52], [0.80, 0.63, 0.60], [0.96, 0.93, 0.86],
                    [0.74, 0.76, 0.78]])  # cream, ochre, dusty rose, ivory, blue-grey
    k = np.clip(bands, 0, 0.999) * (len(pal) - 1)
    i, f = np.floor(k).astype(int), (k - np.floor(k))[..., None]
    f = f * f * (3 - 2 * f)
    rgb = pal[i] * (1 - f) + pal[np.minimum(i + 1, len(pal) - 1)] * f
    # a pale storm oval in the southern tropics
    d = ((lon - 0.62) / 0.05) ** 2 + ((lat + 0.36) / 0.035) ** 2
    rgb = rgb * (1 - 0.5 * np.exp(-d)[..., None]) + np.array([0.95, 0.80, 0.70]) * 0.5 * np.exp(-d)[..., None]
    polar = np.clip((np.abs(lat) - 0.78) / 0.22, 0, 1)[..., None]
    rgb = rgb * (1 - 0.35 * polar) + np.array([0.70, 0.74, 0.78]) * 0.35 * polar
    save("DK_Planet_Bands.png", rgb)


def planet_ring(w=1024):
    x = np.linspace(0, 1, w)
    dens = 0.55 + 0.25 * np.sin(x * 60) * np.sin(x * 23 + 1) + 0.2 * RNG.normal(size=w).cumsum() / np.sqrt(w)
    dens = np.convolve(dens, np.ones(9) / 9, mode="same")
    for c, wd, depth in ((0.58, 0.035, 0.92), (0.83, 0.012, 0.8), (0.3, 0.02, 0.5)):  # gaps
        dens *= 1 - depth * np.exp(-((x - c) / wd) ** 2)
    dens *= np.clip(x / 0.06, 0, 1) * np.clip((1 - x) / 0.04, 0, 1)
    tint = np.stack([0.95 - 0.08 * x, 0.90 - 0.1 * x, 0.82 - 0.06 * x], -1)
    rgb = np.repeat(tint[None], 8, 0)
    save("DK_Planet_Ring.png", rgb, np.repeat(np.clip(dens, 0, 1)[None], 8, 0))


def moon(w=1024, h=512):
    lon, lat = np.meshgrid(np.linspace(0, 1, w, endpoint=False), np.linspace(-1, 1, h))
    maria = wrap_sample(periodic_noise(256, 3.0, 21), lon * 2, lat * 0.5)
    alb = 0.78 - 0.22 * np.clip((maria - 0.52) / 0.2, 0, 1)
    alb = alb + 0.06 * (wrap_sample(periodic_noise(512, 1.6, 22), lon * 4, lat * 2) - 0.5)
    x, y, z = np.cos(lat * np.pi / 2) * np.cos(lon * 2 * np.pi), np.cos(lat * np.pi / 2) * np.sin(lon * 2 * np.pi), np.sin(lat * np.pi / 2)
    for _ in range(140):  # craters: bright rim, soft floor
        cl, cz = RNG.uniform(0, 2 * np.pi), RNG.uniform(-0.95, 0.95)
        c = np.array([np.sqrt(1 - cz * cz) * np.cos(cl), np.sqrt(1 - cz * cz) * np.sin(cl), cz])
        r = RNG.uniform(0.015, 0.09) ** 1.3 * 2.2
        d = np.arccos(np.clip(x * c[0] + y * c[1] + z * c[2], -1, 1)) / r
        alb = alb - 0.07 * np.exp(-(d / 0.8) ** 4) + 0.06 * np.exp(-((d - 1) / 0.12) ** 2)
    rgb = np.clip(alb, 0, 1)[..., None] * np.array([1.0, 0.97, 0.93])
    save("DK_Moon.png", rgb)


def sky_noise(n=512):
    lo, mid, hi = periodic_noise(n, 3.2, 31), periodic_noise(n, 2.4, 32), periodic_noise(n, 1.7, 33)
    ridge = 1 - np.abs(periodic_noise(n, 2.6, 34) * 2 - 1)
    save("DK_Sky_Noise.png", np.dstack([lo, mid, hi]), ridge ** 2)


def starburst(n=1024):
    y, x = (np.mgrid[0:n, 0:n] + 0.5) / n * 2 - 1
    r, a = np.hypot(x, y), np.arctan2(y, x)
    v = 0.9 * np.exp(-(r / 0.035) ** 2) + 0.35 * np.exp(-(r / 0.12) ** 2) + 0.08 * np.exp(-(r / 0.4) ** 2)
    for k in range(16):  # alternating long/short tapered rays
        ang = k * np.pi / 8 + RNG.uniform(-0.03, 0.03)
        length, width = (0.95, 0.010) if k % 2 == 0 else (0.55, 0.007)
        d = np.abs(np.angle(np.exp(1j * (a - ang))))
        v += 0.55 * np.exp(-(d * r / width) ** 2) * np.clip(1 - r / length, 0, 1) ** 1.6 * (r > 0.01)
    v *= np.clip((1 - r) / 0.05, 0, 1)
    save("DK_Flare_Starburst.png", np.repeat(np.clip(v, 0, 1)[..., None], 3, -1))


HAZE = np.array([246, 229, 195]) / 255
BAND_TOP, BAND_BOTTOM = 9.0, -1.0  # degrees of elevation at the strip's top and bottom rows
BAND_GAIN = 1.1  # the strip stores colour / (haze * gain); DK_SpaceSky multiplies by the sky's own horizon haze
SEA_AZ, SEA_FEATHER = (8.0, 172.0), 7.0  # world compass degrees (from +Z toward +X) the harbour opens onto; the strip's
                                          # u is atan2(x, z) / 2pi + 0.5, so its azimuth is the compass + 180


def soften(a, r):
    """Airbrush soft focus: three box passes (~gaussian, sigma ~ r) along both axes; x wraps, y clamps."""
    for axis, wrap in ((1, True), (0, False)):
        for _ in range(3):
            pad = [(0, 0)] * a.ndim
            pad[axis] = (r + 1, r)
            c = np.cumsum(np.pad(a, pad, mode="wrap" if wrap else "edge"), axis=axis)
            a = (np.take(c, range(2 * r + 1, c.shape[axis]), axis) - np.take(c, range(0, c.shape[axis] - 2 * r - 1), axis)) / (2 * r + 1)
    return a


def skyline_far(w=8192, h=512, ss=2):
    """A 360-degree far city, three depth layers dissolving into the horizon haze (target: sky_ab/celestial):
    each layer is painted flat, softened more the further it is, then composited far to near."""
    W, H = w * ss, h * ss
    px_deg_x, px_deg_y = W / 360.0, H / (BAND_TOP - BAND_BOTTOM)
    horizon = int(BAND_TOP * px_deg_y)
    density = periodic_noise(256, 3.0, 41)[0]  # clusters and sparse stretches around the compass
    cool = np.array([0.6, 0.68, 0.79])         # aerial-perspective blue of shaded faces
    small = lambda a: a.reshape(h, ss, w, ss, *a.shape[2:]).mean((1, 3))
    out_c, out_a = np.zeros((h, w, 3)), np.zeros((h, w))
    layers = (  # count, width deg, height deg, mix toward haze, blur px (far -> near)
        (700, (0.12, 0.6), (0.2, 1.3), 0.86, 3),
        (380, (0.2, 1.1), (0.3, 2.8), 0.68, 2),
        (150, (0.35, 2.0), (0.5, 6.0), 0.46, 1),
    )
    for count, (w0, w1), (h0, h1), mix, blur in layers:
        rgb, alpha = np.zeros((H, W, 3)), np.zeros((H, W))
        lit = HAZE * (1 + 0.07 * (1 - mix))
        shaded = HAZE * (1 - 0.5 * (1 - mix)) + cool * 0.5 * (1 - mix)

        def paint(m, ys, xs, side, v):
            col = np.where(side[..., None], lit, shaded)
            fog = np.clip(1 - v / 0.4, 0, 1)[..., None] * 0.6  # ground haze swallows the feet
            col = col * (1 - fog) + HAZE * fog
            xw = xs % W
            rgb[ys[m], xw[m]] = col[m]
            alpha[ys[m], xw[m]] = 1.0

        # the city carpet: a continuous run of low blocks of random width and height under each layer's towers
        edges = np.cumsum(RNG.uniform(w0, w1 * 1.5, int(360 / w0) + 2) * px_deg_x).astype(int)
        edges = np.concatenate([[0], edges[edges < W], [W]])
        block = np.searchsorted(edges, np.arange(W), side="right") - 1
        u_block = (np.arange(W) - edges[block]) / np.maximum(np.diff(edges)[block], 1)
        az = np.arange(W) / px_deg_x
        tall = h0 * (0.35 + 0.9 * RNG.uniform(size=len(edges)))[block] * (0.6 + 0.8 * density[(az / 360 * 256).astype(int) % 256])
        ys, xs = np.mgrid[horizon - int(h0 * 2.2 * px_deg_y):horizon, 0:W]
        v = (horizon - ys) / (tall[None, :] * px_deg_y)
        paint(v <= 1, ys, xs, np.broadcast_to(u_block < .6, ys.shape), v)
        placed = 0
        while placed < count:
            az = RNG.uniform(0, 360)
            if RNG.uniform() > 0.25 + 0.75 * density[int(az / 360 * 256) % 256]:
                continue
            placed += 1
            wd, ht = RNG.uniform(w0, w1), (h0 + (h1 - h0) * RNG.uniform() ** 2.4)
            wd = max(wd, ht * 0.14)  # no pins: slenderness at most ~7:1
            kind = RNG.choice(["box", "setback", "disc", "spire", "dome"], p=[.38, .26, .14, .1, .12])
            x0, x1 = int((az - wd / 2) * px_deg_x), int((az + wd / 2) * px_deg_x)
            top = horizon - int(ht * px_deg_y)
            ys, xs = np.mgrid[max(top - 40 * ss, 0):horizon, x0 - 30 * ss:x1 + 30 * ss]
            u = (xs - x0) / max(x1 - x0, 1)            # 0..1 across the body
            v = (horizon - ys) / max(horizon - top, 1)  # 0 at the ground, 1 at the roof
            if kind == "box":
                m = (u >= 0) & (u <= 1) & (v <= 1)
            elif kind == "setback":
                m = ((u >= 0) & (u <= 1) & (v <= .6)) | ((u >= .15) & (u <= .85) & (v <= .85)) | ((u >= .3) & (u <= .7) & (v <= 1))
            elif kind == "disc":  # shaft under a lens-shaped observation disc
                m = ((u >= .36) & (u <= .64) & (v <= .9)) | (((u - .5) / .75) ** 2 + ((v - .9) / .06) ** 2 <= 1)
            elif kind == "spire":
                m = (np.abs(u - .5) <= .5 * np.clip(1 - v, 0, 1) ** 1.2 + .04) & (v <= 1.1)
            else:
                m = ((u - .5) ** 2 / .25 + v ** 2 <= 1) & (v >= 0)
            if m.any():
                paint(m, ys, xs, u < .55, v)
        # soften premultiplied colour and coverage together, then lay this layer over the ones behind it
        c, a = soften(small(rgb * alpha[..., None]), blur), soften(small(alpha), blur)
        out_c, out_a = c + out_c * (1 - a[..., None]), a + out_a * (1 - a)
    compass = (np.arange(w) + .5) / w * 360 - 180  # strip column -> world compass
    land = np.clip(np.maximum(SEA_AZ[0] - compass, compass - SEA_AZ[1]) / SEA_FEATHER + .5, 0, 1)
    land = land * land * (3 - 2 * land)
    out_c, out_a = out_c * land[None, :, None], out_a * land[None, :]  # headlands taper into the open sea
    hz = int(BAND_TOP / (BAND_TOP - BAND_BOTTOM) * h)
    below = np.clip(1 - (np.arange(h - hz) / (h - hz - 1)) ** 3, 0, 1)  # haze under the horizon, fading out at -1 deg
    b = below[:, None]
    out_c[hz:] = HAZE * b[..., None] + out_c[hz:] * (1 - b[..., None])  # premultiplied: haze over what is there
    out_a[hz:] = b + out_a[hz:] * (1 - b)
    col = out_c / np.maximum(out_a, 1e-4)[..., None] / (HAZE * BAND_GAIN)
    save("DK_Skyline_Far.png", np.clip(col, 0, 1), out_a)


def sea_normal(n=512):
    """Wind waves: a height field with a spectrum peaked across the wind (x) and a long crest spread, as slopes."""
    r = np.random.default_rng(77)
    kx, kz = np.fft.fftfreq(n)[None, :] * n, np.fft.fftfreq(n)[:, None] * n
    k = np.hypot(kx, kz) + 1e-6
    spread = (0.35 + 0.65 * np.abs(kx / k)) ** 3            # crests run mostly along z
    amp = spread * np.exp(-(8 / k) ** 2) / k ** 1.8          # little energy below k 8 (no tile-scale swell)
    amp[0, 0] = 0
    height = np.real(np.fft.ifft2((r.normal(size=(n, n)) + 1j * r.normal(size=(n, n))) * amp))
    gx = np.roll(height, -1, 1) - np.roll(height, 1, 1)
    gz = np.roll(height, -1, 0) - np.roll(height, 1, 0)
    scale = 0.5 / np.percentile(np.abs(np.concatenate([gx.ravel(), gz.ravel()])), 99.5)
    save("DK_Sea_Normal.png", np.dstack([0.5 + gx * scale, 0.5 + gz * scale, np.ones((n, n))]))


if __name__ == "__main__":
    planet_bands()
    planet_ring()
    moon()
    sky_noise()
    starburst()
    skyline_far()
    sea_normal()
