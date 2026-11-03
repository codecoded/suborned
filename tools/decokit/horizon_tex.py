"""Horizon planet and moon maps (Vela, 2026-10-27; Sky: "I really miss the brilliance of DecoKit"): the old DecoKit sky bodies, ported faithfully onto the mesh planets.
Source: sky_sheet.py (the old DecoKit's DK_SpaceSky textures: planet_bands, planet_ring, moon), run unchanged with the same seed and call order, so the pixels are the old
kit's (checked against the old repo's Art/DecoKit/Textures/Sky PNGs when they are on disk). Written beside the ground set (out/decokit-ground) with name-derived .meta.
  horizon_planet_bands_v1.png   2048 x 1024 lat-long gas giant (old DK_Planet_Bands): u = longitude, v = latitude (south pole v 0), wraps in u
  horizon_planet_ring_v1.png    1024 x 8 radial ring profile (old DK_Planet_Ring RGB; inner -> outer); the old alpha density is not shipped: horizon.py cuts the gaps as geometry
                                from the same profile (ring_runs) and the ring stays opaque
  horizon_moon_v1.png           1024 x 512 lat-long chalky moon (old DK_Moon): maria, craters, rim highlights
No Blender: numpy and Pillow. python3 tools/decokit/horizon_tex.py [out_dir] (run.ts does)."""
import hashlib
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).parent))
import sky_sheet  # noqa: E402
from ground_tex import OUT, meta_text  # noqa: E402

NAMES = {"DK_Planet_Bands.png": "horizon_planet_bands_v1.png", "DK_Planet_Ring.png": "horizon_planet_ring_v1.png", "DK_Moon.png": "horizon_moon_v1.png"}
OLD = Path("~/Projects/the-suborned/game/Assets/TheSuborned/Art/DecoKit/Textures/Sky").expanduser()
RING_PROFILE = OUT / "horizon_ring_profile.json"  # the ring's alpha density (1024 samples, inner -> outer): horizon.py reads it to cut the gaps


def save(name, rgb, alpha=None):
    """sky_sheet.save, redirected: RGB only into OUT under the new name (+ .meta); a ring's alpha goes to the profile json."""
    out = NAMES[name]
    a = (np.clip(rgb, 0, 1) * 255 + 0.5).astype(np.uint8)
    img = Image.fromarray(a)
    img.save(OUT / out)
    (OUT / f"{out}.meta").write_text(meta_text(out, "a", img.size))
    if alpha is not None:
        RING_PROFILE.write_text(json.dumps(dict(source="sky_sheet.planet_ring alpha (old DK_Planet_Ring A), inner -> outer", density=[round(float(x), 4) for x in np.clip(alpha, 0, 1)[0]])))
    print(f"[horizon-tex] {out} {img.size[0]}x{img.size[1]}")
    return a


def same_as_old(name, a):
    """Pixel check against the old kit's PNG (RGB) when the old repo is on disk: the port is faithful or it says so."""
    p = OLD / name
    if not p.exists():
        return "old repo not on disk"
    o = np.asarray(Image.open(p).convert("RGB"))
    if o.shape != a.shape:
        return f"size differs: old {o.shape}, new {a.shape}"
    d = np.abs(o.astype(int) - a.astype(int))
    return f"max |diff| {d.max()} (mean {d.mean():.4f})"


def build():
    OUT.mkdir(parents=True, exist_ok=True)
    got = {}
    sky_sheet.save = lambda name, rgb, alpha=None: got.__setitem__(name, save(name, rgb, alpha))
    sky_sheet.RNG = np.random.default_rng(1951)  # the old run's seed and order: bands (no RNG), ring, moon
    sky_sheet.planet_bands()
    sky_sheet.planet_ring()
    sky_sheet.moon()
    for name, a in got.items():
        print(f"[horizon-tex] {NAMES[name]} vs old {name}: {same_as_old(name, a)}; sha256 {hashlib.sha256((OUT / NAMES[name]).read_bytes()).hexdigest()[:16]}")


if __name__ == "__main__":
    build()
