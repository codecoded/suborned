"""Brief 079 note 2 (Vela): signal_ring_v2.png, the HaloRing's clock map for the lit-in-the-paving ring (crossing079.py; candidate). numpy and Pillow, no Blender: python3 -I tools/decokit/signal_tex079.py [out_dir].
The contract is signal_ring_v1's: alpha = 1 - (k + 0.5) / 24 for segment k clockwise from the top, 0 elsewhere, hard (a threshold map): `_Cutoff = 1 - lit / 24` lights exactly `lit` segments.
What changes: the band is narrower (0.50 to 0.565 m, the metal bezels take the rest), the segments stand further apart (3 degrees either side) so they read as lamps, and RGB is no longer white: the emission map falls from 1 on the
band's centre line to 0.42 on its edges. Bled outward into the clear pixels so bilinear never samples black at a hard alpha edge."""

import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).parent))
import signal_tex as st  # noqa: E402

R0, R1, GAP, EDGE = 0.50, 0.565, 3.0, 0.5


def ring():
    n = st.RING_PX
    ys, xs = np.mgrid[0:n, 0:n]
    x = (xs + 0.5) / n * 2 * st.RING_HALF - st.RING_HALF
    y = st.RING_HALF - (ys + 0.5) / n * 2 * st.RING_HALF
    r = np.hypot(x, y)
    clock = np.degrees(np.arctan2(x, y)) % 360.0
    k = np.floor(clock / (360.0 / st.RING_N)).astype(int) % st.RING_N
    within = clock - k * (360.0 / st.RING_N)
    on = (r >= R0) & (r <= R1) & (within >= GAP) & (within <= 360.0 / st.RING_N - GAP)
    alpha = np.where(on, np.round(255 * (1 - (k + 0.5) / st.RING_N)), 0).astype(np.uint8)
    t = np.clip(np.abs(r - (R0 + R1) / 2) / ((R1 - R0) / 2), 0, 1)
    tang = np.clip(np.abs(within - 180.0 / st.RING_N) / (180.0 / st.RING_N - GAP), 0, 1)
    prof = EDGE + (1 - EDGE) * (1 - t ** 2) * (1 - 0.35 * tang ** 4)
    rgb = np.round(255 * np.clip(prof, 0, 1) ** (1 / 2.2)).astype(np.uint8)   # sRGB-encoded map: the profile is linear
    return np.dstack([rgb, rgb, rgb, alpha])


def bezel():
    """cross_bezel_v1.png, 256 x 64: the metal of a ring set in the ground, on crossing079's bands. u runs round the ring, v across it: v 0 to 0.24 the dark seam at the road, 0.24 to 0.40 the bright lip, then the brushed dish (fine lines running round the ring, deterministic), v 0.84 to 1 the dark wall at the lens."""
    h, w = 64, 256
    rng = np.random.default_rng(79)
    v = ((np.arange(h) + 0.5) / h)[:, None]
    streak = np.repeat(rng.normal(0, 1, (h, 1)), w, axis=1)
    grain = rng.normal(0, 1, (h, w))
    grain = (grain + np.roll(grain, 1, 1) + np.roll(grain, 2, 1) + np.roll(grain, 3, 1)) / 2     # drawn out round the ring; np.roll keeps it seamless
    body = 0.66 + 0.035 * streak + 0.018 * grain
    lip = np.clip((v - 0.22) / 0.05, 0, 1) * np.clip((0.42 - v) / 0.10, 0, 1)
    wall = np.clip((v - 0.84) / 0.12, 0, 1)
    seam = np.clip((0.26 - v) / 0.06, 0, 1)
    lin = body * (1 + 0.35 * lip) * (1 - 0.82 * wall) * (1 - 0.95 * seam)
    tint = np.array([0.93, 0.97, 1.0])                                                            # nickel, a touch cool: apart from the warm road and the paving
    s = np.clip(lin[..., None] * tint, 0, 1) ** (1 / 2.2)
    a = np.full((h, w, 1), 1.0)
    return np.round(255 * np.concatenate([s, a], axis=2)).astype(np.uint8)


def build():
    out = Path(sys.argv[1]) if len(sys.argv) > 1 else st.OUT
    out.mkdir(parents=True, exist_ok=True)
    for name, arr, alpha in (("signal_ring_v2.png", ring(), True), ("cross_bezel_v1.png", bezel(), False)):
        im = Image.fromarray(arr, "RGBA")
        im.save(out / name, optimize=True)
        (out / f"{name}.meta").write_text(st.meta(name, im.size, False) if alpha else st.meta_text(name, "a", im.size))
    print(f"[signal-tex-079] {name} -> {out}")


if __name__ == "__main__":
    build()
