"""Boarding pavilion textures (brief 028, Vela): the testbed pavilion's own maps at full resolution, no downscale, from the store's source glb (sha256 in art/pavilion/PROVENANCE.json).
  pavilion_atlas_v1.png          base colour, 4096 x 4096 (the glb's Image_0 jpeg, decoded unchanged)
  pavilion_atlas_v1_normal.png   normal, 4096 x 4096 (the glb's png, byte-identical pixels)
  pavilion_atlas_v1_mask.png     2048 x 2048: R metallic (glTF B), G occlusion (glTF R), A smoothness = 1 - roughness (glTF G): the glb's Image_1, repacked, no resample
The glb's emissive map (Image_3, 2048, max 8/255) is black and is not exported. No Blender: numpy and Pillow. python3 tools/decokit/pavilion_tex.py [out_dir] (run.ts does)."""
import io
import json
import os
import struct
import sys
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).parent))
from ground_tex import OUT, meta_text  # noqa: E402

SRC = Path(os.environ.get("SUBORNED_STORE_ART") or "~/Projects/the-suborned-store/testbed-art").expanduser() / "boarding-pavilion/source/boarding-pavilion-b.glb"


def images():
    b = SRC.read_bytes()
    n, _ = struct.unpack("<II", b[12:20])
    j = json.loads(b[20:20 + n])
    off = 20 + n + 8
    out = {}
    for im in j["images"]:
        bv = j["bufferViews"][im["bufferView"]]
        out[im["name"]] = Image.open(io.BytesIO(b[off + bv.get("byteOffset", 0):off + bv.get("byteOffset", 0) + bv["byteLength"]])).convert("RGB")
    return out


def build():
    if not SRC.exists():
        print(f"[pavilion-tex] no source at {SRC}: skipped")
        return
    OUT.mkdir(parents=True, exist_ok=True)
    im = images()
    orm = np.asarray(im["Image_1"])
    mask = np.dstack([orm[..., 2], orm[..., 0], np.zeros(orm.shape[:2], np.uint8), 255 - orm[..., 1]])
    for name, img, kind in (("pavilion_atlas_v1.png", im["Image_0"], "a"), ("pavilion_atlas_v1_normal.png", im["normal"], "n"), ("pavilion_atlas_v1_mask.png", Image.fromarray(mask, "RGBA"), "m")):
        img.save(OUT / name)
        (OUT / f"{name}.meta").write_text(meta_text(name, kind, img.size))
        print("[pavilion-tex]", name, img.size)


if __name__ == "__main__":
    build()
