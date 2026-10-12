"""Derive engine maps from generated source images (SourceArt/DecoKit/Textures/gen -> derived).

python3 Tools/Blender/decokit/derive_maps.py
  sheet / cutout (textures.json kind): key the flat white background to alpha -> <stem>_alpha.png
      (soft matte from distance-to-white, then colour decontamination so edges carry no white halo)
  tile: height from luminance -> <stem>_normal.png (OpenGL, green up), subtle strength per material
"""

import json
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
SPEC = json.loads((Path(__file__).parent / "textures.json").read_text())
GEN = ROOT / "SourceArt/DecoKit/Textures/gen"
DER = ROOT / "SourceArt/DecoKit/Textures/derived"
NORMAL_STRENGTH = {"terrazzo": 1.5, "limestone": 1.0, "glazed": 0.6, "brushed": 0.8, "asphalt": 3.0, "awning": 1.2}


def push_pull(rgb, w):
    """Fill low-weight texels with coverage-weighted colour from coarser mips (keeps w=1 texels exact)."""
    levels = []
    c, ww = rgb * w[..., None], w
    while min(ww.shape) > 1:
        levels.append((c, ww))
        h, v = ww.shape[0] + ww.shape[0] % 2, ww.shape[1] + ww.shape[1] % 2
        c = np.pad(c, ((0, h - c.shape[0]), (0, v - c.shape[1]), (0, 0)), mode="edge")
        ww = np.pad(ww, ((0, h - ww.shape[0]), (0, v - ww.shape[1])), mode="edge")
        c = c.reshape(h // 2, 2, v // 2, 2, 3).mean((1, 3))
        ww = ww.reshape(h // 2, 2, v // 2, 2).mean((1, 3))
    out = c / np.maximum(ww, 1e-6)[..., None]
    for c, ww in reversed(levels):
        up = np.repeat(np.repeat(out, 2, 0), 2, 1)[: ww.shape[0], : ww.shape[1]]
        out = c + (1 - ww)[..., None] * up
    return out


def key_white(src, dst, lo=18.0, hi=60.0):
    a = np.asarray(Image.open(src).convert("RGB")).astype(np.float32)
    dist = np.linalg.norm(255.0 - a, axis=2)  # distance from pure white
    alpha = np.clip((dist - lo) / (hi - lo), 0, 1)
    safe = np.maximum(alpha, 1e-3)[..., None]
    fg = np.clip((a - 255.0 * (1 - safe)) / safe, 0, 255)  # un-premultiply against the white matte
    fg = push_pull(fg, np.clip((alpha - 0.3) / 0.4, 0, 1))  # no white under the cutoff edge at distance
    out = np.dstack([fg, alpha * 255]).astype(np.uint8)
    Image.fromarray(out, "RGBA").save(dst)
    return float(alpha.mean())


def normal_from_height(src, dst, strength):
    g = np.asarray(Image.open(src).convert("L")).astype(np.float32) / 255.0
    dx = (np.roll(g, -1, 1) - np.roll(g, 1, 1)) * 0.5 * strength  # wraps: tileable stays tileable
    dy = (np.roll(g, -1, 0) - np.roll(g, 1, 0)) * 0.5 * strength
    n = np.dstack([-dx * 8, dy * 8, np.ones_like(g)])
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    Image.fromarray(((n * 0.5 + 0.5) * 255).astype(np.uint8), "RGB").save(dst)


def main():
    DER.mkdir(parents=True, exist_ok=True)
    for t in SPEC["textures"]:
        src = GEN / t["file"]
        if not src.exists():
            print(f"[derive] missing {t['file']}")
            continue
        stem = src.stem
        if t["kind"] in ("sheet", "cutout"):  # "art" is full-bleed and stays opaque
            cover = key_white(src, DER / f"{stem}_alpha.png")
            print(f"[derive] {stem}_alpha.png coverage={cover:.2f}")
        if t["kind"] == "tile":
            k = next((v for key, v in NORMAL_STRENGTH.items() if stem.startswith(key)), 1.0)
            normal_from_height(src, DER / f"{stem}_normal.png", k)
            print(f"[derive] {stem}_normal.png strength={k}")


main()
