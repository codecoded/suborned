"""CivicCabinet textures (Vela, 2026-10-20): the control cabinet's atlas maps from art/cabinet/ (the old city's v2 runtime set, 1024 px), as the Unity maps the .mats read:
  cabinet_atlas_v1.png / _normal.png / _mask.png   albedo (sRGB), tangent normal, mask (R metallic, G occlusion 255, A smoothness = 1 - the source roughness)
  cabinet_gold_v1.png / _normal.png / _mask.png    the district's shared gold at 256 px; metallic 0.8 as the old cabinet's SatinBrass, roughness from its ORM green
glTF packs metallic-roughness as B and G of Image_1. No Blender: numpy and Pillow. python3 tools/decokit/cabinet_tex.py [out_dir] (run.ts does)."""

import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).parent))
from ground_tex import OUT, meta_text  # noqa: E402

ART = Path(__file__).resolve().parent / "art/cabinet"


def rgb(p):
    return np.asarray(Image.open(p).convert("RGB"))


def save(name, arr, kind, files):
    im = Image.fromarray(arr.astype(np.uint8), "RGBA" if arr.shape[-1] == 4 else "RGB")
    im.save(OUT / name, optimize=True)
    (OUT / f"{name}.meta").write_text(meta_text(name, kind, im.size))
    files.append(dict(file=name, size=list(im.size), kind=kind))


def build():
    OUT.mkdir(parents=True, exist_ok=True)
    files = []
    base, orm, nrm = rgb(ART / "atlas_0.png"), rgb(ART / "atlas_1.png"), rgb(ART / "atlas_2.png")
    save("cabinet_atlas_v1.png", base, "a", files)
    save("cabinet_atlas_v1_normal.png", nrm, "n", files)
    save("cabinet_atlas_v1_mask.png", np.dstack([orm[..., 2], np.full(orm.shape[:2], 255, np.uint8), np.zeros(orm.shape[:2], np.uint8), 255 - orm[..., 1]]), "m", files)
    gb, go, gn = rgb(ART / "gold_base.png"), rgb(ART / "gold_orm.png"), rgb(ART / "gold_normal.png")
    save("cabinet_gold_v1.png", gb, "a", files)
    save("cabinet_gold_v1_normal.png", gn, "n", files)
    save("cabinet_gold_v1_mask.png", np.dstack([np.full(go.shape[:2], 204, np.uint8), np.full(go.shape[:2], 255, np.uint8), np.zeros(go.shape[:2], np.uint8), 255 - go[..., 1]]), "m", files)
    (OUT / "cabinet-tex.json").write_text(json.dumps(dict(schema="suborned.cabinettex/1", files=files), indent=1))
    print(f"[cabinet-tex] {len(files)} files -> {OUT}")


if __name__ == "__main__":
    build()
