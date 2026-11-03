"""Car enamel surface detail (brief 070, Vela; Glyph on the briefing's getaway still: "the car model has no detail and looks like a stand-in"): the sedans' paint is one flat colour, so in a
close still the body reads as plastic. This makes one shared detail pair at car scale, laid over the car enamels by `DK_CarEnamel_*` (mats.CAR_DETAIL: same hex as the base enamel, clear coat):

  car_enamel_detail_v1.png         512 x 512 detail albedo, LINEAR (URP multiplies x2: 128 is neutral): a fine metallic flake (2 mm), a faint 3 cm mottling of the paint film and a soft 25 cm
                                   cloudiness, so a big panel is never one flat value. Isotropic and seamless: no seams, no gradient, so it holds on every face of the 1 m box-projected UVs
                                   (the car pieces' UVs are world metres; a top face maps (x, y), a side (y, z)).
  car_enamel_detail_v1_normal.png  orange peel: the clear coat's 4 to 8 mm ripple, as a tangent normal (the material's bump is CAR_DETAIL["bump"]); it is what makes a highlight on the
                                   hood break up the way a painted car's does.

Tile 1 m (2 mm a texel). 0.5 MiB with mips. No colour: the hue is the material's. python3 tools/decokit/car_tex.py [out_dir] (run.ts runs it before Blender; export_kit copies car_*)."""

import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).parent))
from ground_tex import OUT, meta_text, normal_from_height  # noqa: E402

W = H = 512
TILE = 1.0


def pnoise(h, w, cut_px, rng):
    """Periodic low-pass noise (unit sd): white noise through a Gaussian in the Fourier domain, wavelength about cut_px texels (robot_tex.py's)."""
    fy, fx = np.fft.fftfreq(h)[:, None], np.fft.fftfreq(w)[None, :]
    a = np.real(np.fft.ifft2(np.fft.fft2(rng.standard_normal((h, w))) * np.exp(-(fx * fx + fy * fy) * (cut_px * np.pi) ** 2 / 2)))
    return (a - a.mean()) / max(a.std(), 1e-9)


def build():
    rng = np.random.default_rng(2070)
    flake = pnoise(H, W, 0.7, rng)       # 2 mm metallic flake
    film = pnoise(H, W, 15, rng)         # 3 cm mottling of the paint film
    cloud = pnoise(H, W, 125, rng)       # 25 cm cloudiness: a door is never one flat value
    peel = pnoise(H, W, 3.0, rng)        # 6 mm orange peel
    lum = 1 + .02 * flake + .01 * film + .012 * cloud  # sd about 2.5 percent: a first pass at .03 cloud read as a blotchy roof in the close plates
    alb = np.clip(.5 * lum, 0, 1)
    height = peel * .8 + film * .2
    height = (height - height.min()) / (height.max() - height.min())
    nrm = normal_from_height(height, 2.2)
    OUT.mkdir(parents=True, exist_ok=True)
    files = []
    for name, arr, kind in (("car_enamel_detail_v1.png", np.dstack([alb] * 3), "da"), ("car_enamel_detail_v1_normal.png", nrm, "n")):
        Image.fromarray((np.clip(arr, 0, 1) * 255 + .5).astype(np.uint8), "RGB").save(OUT / name, optimize=True)
        (OUT / f"{name}.meta").write_text(meta_text(name, kind, (W, H)))
        files.append(dict(file=name, size=[W, H], kind=kind, bc7MiB=round(W * H * 4 / 3 / 2 ** 20, 2)))
    stats = dict(albedoSdPercent=round(float(lum.std() * 100), 2), meanLevel=round(float(alb.mean() * 255 * 2), 1))
    (OUT / "car-tex.json").write_text(json.dumps(dict(schema="suborned.cartex/1", tileMetres=TILE, files=files, totalMiB=round(sum(f["bc7MiB"] for f in files), 2), stats=stats), indent=1))
    print(f"[car-tex] {sum(f['bc7MiB'] for f in files):.2f} MiB, albedo sd {stats['albedoSdPercent']} percent, mean {stats['meanLevel']} (256 = neutral x2) -> {OUT}")


if __name__ == "__main__":
    build()
