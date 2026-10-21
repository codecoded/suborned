"""Textures of the ported testbed café furniture (brief 026, Vela). No bpy: python3 tools/decokit/tex_cafe3d.py [ground_dir]

furniture atlas (store testbed-art/cafe-table-set/furniture-{base,normal,orm}.png, 2048 x 1024) -> <ground_dir>/ground_cafetb_v1.png (2048 x 1024), _normal (2048 x 1024), _mask (2048 x 1024:
R metallic, G occlusion, A smoothness = 1 - roughness; the cloth sheen map is dropped, URP Lit has none), each with its .meta (ground_tex.meta_text); default dir out/decokit-ground, so export_kit copies them.
The counter art is the glb's embedded 'surface-atlas-v1' (1254 px): port_cafe3d.py saves it to tools/decokit/art/cafe/cafe_surface_tb_v1.png at its own 1254 px (the lit-art route of the other café paintings)."""

import hashlib
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).parent))
from ground_tex import meta_text  # noqa: E402

REPO = Path(__file__).resolve().parents[2]
STORE = Path("~/Projects/the-suborned-store/testbed-art").expanduser()
OUT = Path(sys.argv[1]) if len(sys.argv) > 1 else REPO / "out/decokit-ground"


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    t = STORE / "cafe-table-set"
    Image.open(t / "furniture-base.png").convert("RGB").save(OUT / "ground_cafetb_v1.png")
    Image.open(t / "furniture-normal.png").convert("RGB").save(OUT / "ground_cafetb_v1_normal.png")
    orm = np.asarray(Image.open(t / "furniture-orm.png").convert("RGB")).astype(np.float32) / 255
    mask = np.zeros((1024, 2048, 4), np.uint8)
    mask[..., 0] = (orm[..., 2] * 255).astype(np.uint8)
    mask[..., 1] = (orm[..., 0] * 255).astype(np.uint8)
    mask[..., 3] = ((1 - orm[..., 1]) * 255).astype(np.uint8)
    Image.fromarray(mask, "RGBA").save(OUT / "ground_cafetb_v1_mask.png")
    for suffix, kind, size in (("", "a", (2048, 1024)), ("_normal", "n", (2048, 1024)), ("_mask", "m", (2048, 1024))):
        (OUT / f"ground_cafetb_v1{suffix}.png.meta").write_text(meta_text(f"ground_cafetb_v1{suffix}.png", kind, size))
    print("[tex_cafe3d]", OUT)


main()
