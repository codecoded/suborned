"""Robot enamel surface detail (brief 026 round 2, Vela; Glint's trace of the flat Waiter): the old robots' bodies read at a pixel spread of about 35 and ours about 12, because the
new enamel is one flat colour. The old city had no robot texture to port (its robot materials are the same plain DK_Enamel_* rows; the grain, patina and dark base came from its
16-19k triangle models and light), so this makes one shared detail pair at robot scale, laid over the enamels by `DK_RobotEnamel_*` (same hex, smoothness .68, clear coat):

  robot_enamel_detail_v1.png         256 x 1024 detail albedo, LINEAR (URP multiplies x2: 128 is neutral): fine grain, patina blotches (2 to 12 cm), a hairline vertical seam at u = 0,
                                     and a base darkening along v (the robot UVs run v = height in metres over a 2 m tile): 24 percent darker at the floor, back to neutral by 0.6 m
  robot_enamel_detail_v1_normal.png  the grain and the seam groove as a tangent normal (the material scale is 0.3)

Tile 0.5 m across (u) and 2 m up (v) so both axes sit at 2 mm a texel. 0.7 MiB with mips. python3 tools/decokit/robot_tex.py [out_dir]"""

import json
import os
import sys
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).parent))
from ground_tex import OUT, meta_text, normal_from_height  # noqa: E402

W, H = 256, 1024
TILE = (0.5, 2.0)


def pnoise(h, w, cut_px, rng):
    """Periodic low-pass noise (unit sd): white noise through a Gaussian in the Fourier domain, wavelength about cut_px texels."""
    fy, fx = np.fft.fftfreq(h)[:, None], np.fft.fftfreq(w)[None, :]
    a = np.real(np.fft.ifft2(np.fft.fft2(rng.standard_normal((h, w))) * np.exp(-(fx * fx + fy * fy) * (cut_px * np.pi) ** 2 / 2)))
    return (a - a.mean()) / max(a.std(), 1e-9)


def build():
    rng = np.random.default_rng(2026)
    fine = pnoise(H, W, 1.6, rng)        # 3 mm grain
    mid = pnoise(H, W, 22, rng)          # 4 cm mottling (round 2b: 3 mm grain at .05 read as fur on a 0.5 m body, so it is now .02)
    big = pnoise(H, W, 90, rng)          # 18 cm patina
    lum = 1 + .02 * fine + .04 * mid + .055 * big
    z = (1 - np.arange(H) / (H - 1)) * TILE[1]                                   # metres up the robot at each row (row 0 = top = v 1)
    ramp = np.clip((z - .08) / (.62 - .08), 0, 1)
    base = .76 + .24 * (ramp * ramp * (3 - 2 * ramp))                           # 0.76 at the floor, 1.0 from 0.62 m up
    base = np.where(z < .08, .70 + .06 * z / .08, base)                          # the last 8 cm under the skirt: darker still
    lum = lum * base[:, None]
    seam = np.zeros(W)
    seam[0], seam[1], seam[-1] = .12, .05, .05
    lum = lum * (1 - seam[None, :])
    alb = np.clip(.5 * lum, 0, 1)
    height = fine * .08 + mid * .6 + big * .32
    height = (height - height.min()) / (height.max() - height.min())
    height[:, 0] -= .35                                                           # the seam groove
    height = np.clip(height, 0, 1)
    nrm = normal_from_height(height, 1.1)
    OUT.mkdir(parents=True, exist_ok=True)
    files = []
    for name, arr, kind in (("robot_enamel_detail_v1.png", np.dstack([alb] * 3), "da"), ("robot_enamel_detail_v1_normal.png", nrm, "n")):
        Image.fromarray((np.clip(arr, 0, 1) * 255 + .5).astype(np.uint8), "RGB").save(OUT / name, optimize=True)
        (OUT / f"{name}.meta").write_text(meta_text(name, kind, (W, H)))
        files.append(dict(file=name, size=[W, H], kind=kind, bc7MiB=round(W * H * 4 / 3 / 2 ** 20, 2)))
    stats = dict(albedoSdPercent=round(float(lum.std() * 100), 1), baseFloor=float(base.min()), meanLevel=round(float(alb.mean() * 255 * 2), 1))
    (OUT / "robot-tex.json").write_text(json.dumps(dict(schema="suborned.robottex/1", tileMetres=list(TILE), files=files, totalMiB=round(sum(f["bc7MiB"] for f in files), 2), stats=stats), indent=1))
    print(f"[robot-tex] {sum(f['bc7MiB'] for f in files):.2f} MiB, albedo sd {stats['albedoSdPercent']} percent, floor factor {stats['baseFloor']:.2f} -> {OUT}")


if __name__ == "__main__":
    build()
