#!/usr/bin/env python3
"""Brief 008 follow-up (14 Oct): derived textures for the civic pieces, no Blender. Writes into out/decokit-008-src/Textures/derived/
(the candidate texture dir the exporter searches first; the store stays read-only).

  banner_comet_seafoam_v1.png   the kit's comet banner (banner_comet_teal_v1.png: field #048A7C, ivory border and comet) with the field
                                recoloured to the civic seafoam #72B5A7. Every texel is mixed between the old field and the old ivory by
                                its red channel (field R 4, ivory R 247), then re-mixed between the new field and the same ivory: the
                                ivory comet, the border and the anti-aliased edges are untouched, the paper noise of the field is kept.
  flower_cluster_coralsoft_v1_alpha.png / flower_cluster_flamingo_v1_alpha.png   the district's coral flower card (flower_cluster_coral_v1_alpha.png, legacy
                                DecoKit Textures: a bush with red-orange and pink blooms) with the red-orange blooms moved to the civic coral #FC9C84 (pinks kept) or
                                to a flamingo magenta-pink (all blooms; hue, saturation and value shifted, each texel's own shading kept); leaves and alpha untouched.
                                The card's red-orange blooms sit 19.0 dE2000 from the cones colour (texture mean); these two clear 20 (overrides008.py measures).
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


FLOWER_SRC = Path.home() / "Projects/the-suborned/game/Assets/TheSuborned/Art/DecoKit/Textures/flower_cluster_coral_v1_alpha.png"
# per variant: the red-orange blooms' (hue deg, saturation, value) and the pink blooms' (None = left as they are). The flamingo card moves both to a magenta-pink
# (hue 322 / 312): at the flamingo hue 338 the card's mean (mostly leaf green plus bloom) stays 16 dE from the cones colour and the texture fails the gate.
FLOWERS = {"flower_cluster_coralsoft_v1_alpha.png": ((12.0, 0.47, 0.99), None), "flower_cluster_flamingo_v1_alpha.png": ((322.0, 0.58, 0.96), (312.0, 0.48, 0.96))}


def rgb_hsv(c):
    mx, mn = c.max(-1), c.min(-1)
    d = mx - mn
    h = np.zeros_like(mx)
    nz = d > 1e-9
    r, g, b = c[..., 0], c[..., 1], c[..., 2]
    h = np.where(nz & (mx == r), ((g - b) / np.where(nz, d, 1)) % 6, h)
    h = np.where(nz & (mx == g), (b - r) / np.where(nz, d, 1) + 2, h)
    h = np.where(nz & (mx == b), (r - g) / np.where(nz, d, 1) + 4, h)
    return h * 60.0, np.where(mx > 0, d / np.where(mx > 0, mx, 1), 0.0), mx


def hsv_rgb(h, s, v):
    k = lambda n: (n + h / 60.0) % 6  # noqa: E731
    f = lambda n: v - v * s * np.clip(np.minimum(k(n), 4 - k(n)), 0, 1)  # noqa: E731
    return np.stack([f(5), f(3), f(1)], -1)


def flowers():
    a = np.asarray(Image.open(FLOWER_SRC).convert("RGBA")).astype(float)
    rgb, alpha = a[..., :3] / 255.0, a[..., 3:]
    h, s, v = rgb_hsv(rgb)
    red = (s > 0.30) & (v > 0.35) & ((h <= 40.0) | (h >= 350.0)) & (alpha[..., 0] > 0)  # red-orange blooms; pinks (320-350) and leaves stay
    pink = (s > 0.25) & (v > 0.35) & (h >= 300.0) & (h < 350.0) & (alpha[..., 0] > 0)
    sr, vr, sp, vp = s[red].mean(), v[red].mean(), s[pink].mean(), v[pink].mean()
    for name, ((th, ts, tv), pk) in FLOWERS.items():
        ns = np.clip(ts * (0.55 + 0.45 * s / sr), 0.12, 0.85)
        nv = np.clip(tv * (0.62 + 0.38 * v / vr), 0.45, 1.0)
        out = np.where(red[..., None], hsv_rgb(np.full_like(h, th), ns, nv), rgb)
        if pk:
            out = np.where(pink[..., None], hsv_rgb(np.full_like(h, pk[0]), np.clip(pk[1] * (0.55 + 0.45 * s / sp), 0.12, 0.85), np.clip(pk[2] * (0.62 + 0.38 * v / vp), 0.45, 1.0)), out)
        img = Image.fromarray(np.clip(np.rint(np.concatenate([out * 255.0, alpha], axis=-1)), 0, 255).astype(np.uint8), "RGBA")
        img.save(DST / name, optimize=False)
        print(f"{name}: {int(red.sum())} bloom texels recoloured ({100 * red.sum() / (alpha[..., 0] > 0).sum():.1f}% of opaque), mean s {sr:.2f} v {vr:.2f} -> hue {th:.0f}")


def main():
    DST.mkdir(parents=True, exist_ok=True)
    flowers()
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
