#!/usr/bin/env python3
"""Brief 008 follow-up (14 Oct): derived textures for the civic pieces, no Blender. Writes into out/decokit-008-src/Textures/derived/
(the candidate texture dir the exporter searches first; the store stays read-only).

  banner_comet_seafoam_v1.png   the kit's comet banner (banner_comet_teal_v1.png: field #048A7C, ivory border and comet) with the field
                                recoloured to the civic seafoam #72B5A7. Every texel is mixed between the old field and the old ivory by
                                its red channel (field R 4, ivory R 247), then re-mixed between the new field and the same ivory: the
                                ivory comet, the border and the anti-aliased edges are untouched, the paper noise of the field is kept.
Run: python3 tools/decokit/derive008.py        Deterministic: the same input gives the same PNG bytes."""

import os
from pathlib import Path

import numpy as np
from PIL import Image

REPO = Path(__file__).resolve().parents[2]
STORE = Path(os.environ.get("SUBORNED_STORE", "~/Projects/the-suborned-store")).expanduser()
SRC = Path(os.environ.get("DECOKIT_SOURCEART") or STORE / "source-art/DecoKit") / "Textures/gen/banner_comet_teal_v1.png"
DST = REPO / "out/decokit-008-src/Textures/derived"
FIELD_OLD = np.array([4.0, 138.0, 124.0])
FIELD_NEW = np.array([0x72, 0xB5, 0xA7], dtype=float)


def main():
    im = Image.open(SRC).convert("RGBA")
    a = np.asarray(im).astype(float)
    rgb, alpha = a[..., :3], a[..., 3:]
    ivory = np.median(rgb[rgb[..., 0] > 240].reshape(-1, 3), axis=0)  # the comet's own ivory
    t = np.clip((rgb[..., 0] - FIELD_OLD[0]) / (ivory[0] - FIELD_OLD[0]), 0.0, 1.0)[..., None]
    # the field's own grain: texel minus the field mean, scaled to the new field's brightness
    grain = (rgb - (FIELD_OLD + t * (ivory - FIELD_OLD))) * (FIELD_NEW.mean() / FIELD_OLD.mean()) * 0.5
    out = FIELD_NEW + t * (ivory - FIELD_NEW) + grain * (1 - t)
    img = Image.fromarray(np.clip(np.rint(np.concatenate([out, alpha], axis=-1)), 0, 255).astype(np.uint8), "RGBA")
    DST.mkdir(parents=True, exist_ok=True)
    img.save(DST / "banner_comet_seafoam_v1.png", optimize=False)
    sample = np.asarray(img)[400, 100, :3]
    print(f"banner_comet_seafoam_v1.png: ivory {ivory.astype(int).tolist()}, field texel {sample.tolist()} (target {FIELD_NEW.astype(int).tolist()})")


if __name__ == "__main__":
    main()
