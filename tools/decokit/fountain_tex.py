"""Atom fountain textures (brief 028, Vela): the testbed fountain's own maps at the resolution the testbed shipped, nothing resampled. Sources: store testbed-art/civic-fountain/*.webp (the
testbed's runtime originals; sha256 in art/fountain/PROVENANCE.json). No Blender: numpy and Pillow. python3 tools/decokit/fountain_tex.py [out_dir] (run.ts does).
  fountain_marble_v1(.png, _normal, _mask)   ambientCG Marble012 (CC0): 2048 base, 1024 normal, 1024 mask (R metallic 0, G occlusion = ORM R, A smoothness = 1 - ORM G)
  fountain_ceramic_v1(.png, _mask)           the teal glaze: 512 and 512
  fountain_polish_v1_normal, fountain_gold_v1_mask, fountain_silver_v1_mask   512: the shared buffing normal; masks with metallic = ORM B, smoothness = 1 - roughness (gold roughness x2 as the testbed's
                                             MeshStandardMaterial(roughness: 2) times the map, clamped; silver x1)
  fountain_mosaic_v1                         1024 pool mosaic          fountain_white_v1  4 x 4 white (the albedo slot of the metals, tinted in the .mat)"""
import os
import sys
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).parent))
from ground_tex import OUT, meta_text  # noqa: E402

S = Path(os.environ.get("SUBORNED_STORE_ART") or "~/Projects/the-suborned-store/testbed-art").expanduser() / "civic-fountain"


def rgb(n):
    return np.asarray(Image.open(S / n).convert("RGB"))


def mask(orm, rough_scale=1.0, metal=True):
    r = np.clip(orm[..., 1].astype(np.float32) * rough_scale, 0, 255)
    return np.dstack([orm[..., 2] if metal else np.zeros(orm.shape[:2], np.uint8), orm[..., 0], np.zeros(orm.shape[:2], np.uint8), (255 - r).astype(np.uint8)])


def save(name, arr, kind):
    im = Image.fromarray(arr.astype(np.uint8), "RGBA" if arr.shape[-1] == 4 else "RGB")
    im.save(OUT / name)
    (OUT / f"{name}.meta").write_text(meta_text(name, kind, im.size))
    print("[fountain-tex]", name, im.size)


def build():
    if not S.exists():
        print(f"[fountain-tex] no source at {S}: skipped")
        return
    OUT.mkdir(parents=True, exist_ok=True)
    save("fountain_marble_v1.png", rgb("marble-base.webp"), "a")
    save("fountain_marble_v1_normal.png", rgb("marble-normal.webp"), "n")
    save("fountain_marble_v1_mask.png", mask(rgb("marble-orm.webp"), 1.0, False), "m")
    save("fountain_ceramic_v1.png", rgb("ceramic-base.webp"), "a")
    save("fountain_ceramic_v1_mask.png", mask(rgb("ceramic-orm.webp"), 1.0, False), "m")
    save("fountain_polish_v1_normal.png", rgb("polish-normal.webp"), "n")
    save("fountain_gold_v1_mask.png", mask(rgb("polish-orm.webp"), 2.0), "m")
    save("fountain_silver_v1_mask.png", mask(rgb("polish-orm.webp"), 1.0), "m")
    save("fountain_mosaic_v1.png", rgb("pool-mosaic.webp"), "a")
    save("fountain_white_v1.png", np.full((4, 4, 3), 255, np.uint8), "a")


if __name__ == "__main__":
    build()
