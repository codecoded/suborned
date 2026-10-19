#!/usr/bin/env python3
"""Brief 020 B (café): the source art of the lit interior, no Blender. Writes into tools/decokit/art/cafe/ (committed; mats.ART_DIRS):

  cafe_back_bar_v1.png   the 9 Oct 2026 "Caprice Grand Café" back-bar painting (three arched niches, espresso machine, teal fluted counter front), the old monorepo's
                         game/Assets/TheSuborned/Art/Handover/Cafe/Cafe_back_bar.png, byte for byte (1376 x 768).
  cafe_mural_v1.png      the same café's botanical mural (lagoon, palms, bird-of-paradise, teal wainscot), Cafe_botanical_mural.png, byte for byte (1376 x 768).
  cafe_floor_v1.png      the floor: ivory and seafoam limestone checker with a fine grout line, 512 px = 2 x 2 cells = 1.2 m (cells of 0.6 m), tiled by the metre in
                         DK_Cafe_Floor (mats.METRIC_UV). Made here (numpy, seeded), so the same run gives the same bytes.
  PROVENANCE.json        source path, sha256 and what each file is.

Run: python3 -I tools/decokit/derive020.py   (needs the old repo at ~/Projects/the-suborned, or SUBORNED_OLD)"""

import hashlib
import json
import os
from pathlib import Path

import numpy as np
from PIL import Image

REPO = Path(__file__).resolve().parents[2]
DST = Path(__file__).resolve().parent / "art/cafe"
OLD = Path(os.path.expanduser(os.environ.get("SUBORNED_OLD", "~/Projects/the-suborned")))
SRC = OLD / "game/Assets/TheSuborned/Art/Handover/Cafe"
COPIES = {"cafe_back_bar_v1.png": "Cafe_back_bar.png", "cafe_mural_v1.png": "Cafe_botanical_mural.png"}
sha = lambda b: hashlib.sha256(b).hexdigest()  # noqa: E731


def floor():
    n, cell = 512, 256
    rng = np.random.default_rng(2026_10_18)
    ivory, sea = np.array([0xEE, 0xE4, 0xCC], float), np.array([0x9C, 0xC7, 0xB8], float)
    yy, xx = np.mgrid[0:n, 0:n]
    chk = ((xx // cell) + (yy // cell)) % 2
    img = np.where(chk[..., None] == 0, ivory, sea)
    # soft limestone mottling, tileable: low-frequency noise built from a wrapped small grid
    g = rng.normal(0, 1, (16, 16))
    big = np.kron(g, np.ones((n // 16, n // 16)))
    big = sum(np.roll(np.roll(big, i, 0), j, 1) for i in range(-8, 9, 4) for j in range(-8, 9, 4)) / 25.0
    img = img + big[..., None] * 3.0 + rng.normal(0, 1.2, (n, n, 1))
    # grout: a 2 px line on every cell edge, a shade darker
    edge = ((xx % cell) < 2) | ((yy % cell) < 2)
    img = np.where(edge[..., None], img * 0.86, img)
    return Image.fromarray(np.clip(img, 0, 255).astype(np.uint8), "RGB")


def main():
    DST.mkdir(parents=True, exist_ok=True)
    prov = {"note": "Brief 020 B, Vela. Source art of the café's lit interior. Stand-in art for the Handover café (the 9 Oct 'Caprice Grand Café' look).", "files": []}
    for name, old in COPIES.items():
        b = (SRC / old).read_bytes()
        (DST / name).write_bytes(b)
        prov["files"].append({"file": name, "source": f"~/Projects/the-suborned/game/Assets/TheSuborned/Art/Handover/Cafe/{old}", "sha256": sha(b), "bytes": len(b),
                              "service": "image generation in the old repo (9 Oct 2026); the old repo kept no record of the service, prompt or cost (the pilot artwork of HandoverCafeShell.cs: 'new back-bar and botanical-mural walls, no text, no logos')",
                              "use": "flat quads on the café's back wall (CafeInterior); never on the curved glass"})
    img = floor()
    img.save(DST / "cafe_floor_v1.png", optimize=False)
    b = (DST / "cafe_floor_v1.png").read_bytes()
    prov["files"].append({"file": "cafe_floor_v1.png", "source": "tools/decokit/derive020.py (numpy, seed 20261018)", "sha256": sha(b), "bytes": len(b), "service": "none: procedural", "cost": 0,
                          "use": "DK_Cafe_Floor, metric tiling 1.2 m (cells of 0.6 m)"})
    (DST / "PROVENANCE.json").write_text(json.dumps(prov, indent=1) + "\n")
    for f in prov["files"]:
        print(f["file"], f["sha256"][:12], f["bytes"])


if __name__ == "__main__":
    main()
