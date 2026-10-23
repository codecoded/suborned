"""Palm LOD metrics (Vela, brief 028): numbers for "does the switch show" from the sweep renders.  python3 tools/decokit/palm_lod_metrics.py <eval_dir> [json_out]

For each piece, level, distance and azimuth: the palm composited on a cream building colour, hazed like the game (linear 90 to 350 m to #ecd9b4, in linear light), compared with L0 at the same view.
  sil   soft silhouette error: sum |a0 - ak| / sum a0 (alpha, 0..1)
  pix   share of the palm's pixels (either alpha over 0.1) that differ from L0 by more than 12/255 in any channel, after haze: what the eye could catch as a change
  mad   mean absolute difference over those pixels, in 0..255
  h     the palm's height in panel pixels (1864 px tall)
"""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

BG = np.array([0.93, 0.88, 0.78])      # cream building / haze-lit wall, sRGB
HAZE = np.array([0xEC, 0xD9, 0xB4]) / 255.0
lin = lambda c: np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)  # noqa: E731
srgb = lambda c: np.where(c <= 0.0031308, c * 12.92, 1.055 * np.clip(c, 0, None) ** (1 / 2.4) - 0.055)  # noqa: E731


def fog(d):
    return min(1.0, max(0.0, (d - 90.0) / 260.0))


def blur(img, s=2.0):
    """Gaussian blur, sigma s px: what the eye integrates when the detail is a few pixels."""
    r = int(3 * s) + 1
    k = np.exp(-0.5 * (np.arange(-r, r + 1) / s) ** 2); k /= k.sum()
    out = img
    for ax in (0, 1):
        pad = [(0, 0)] * out.ndim; pad[ax] = (r, r)
        o = np.pad(out, pad, mode="edge")
        out = sum(k[i] * np.take(o, range(i, i + out.shape[ax]), axis=ax) for i in range(2 * r + 1))
    return out


def load(path):
    im = np.asarray(Image.open(path).convert("RGBA")).astype(np.float64) / 255.0
    return im[..., :3], im[..., 3]


def comp(rgb, a, d):
    """Palm over BG, then haze (premultiplied: blender writes straight alpha in PNG)."""
    f = fog(d)
    bg = lin(BG); hz = lin(HAZE)
    c = lin(rgb) * a[..., None] + bg * (1 - a[..., None])
    c = c * (1 - f) + hz * f
    return srgb(c)


def run(ev, out=None):
    ev = Path(ev); rows = []
    files = sorted(ev.glob("*_L0_d*_az*.png"))
    for f0 in files:
        name, rest = f0.name.split("_L0_")
        m_ = __import__("re").match(r"d(\d+)_az(\d+)", rest); d, az = int(m_.group(1)), int(m_.group(2))
        rgb0, a0 = load(f0)
        c0 = comp(rgb0, a0, d)
        ys = np.where(a0.max(axis=1) > 0.1)[0]
        h = int(ys.max() - ys.min() + 1) if len(ys) else 0
        for lv in sorted({int(x.name.split('_L')[-1].split('_')[0]) for x in ev.glob(f'{name}_L*_d{d}_az{az}.png')} - {0}):
            fk = ev / f"{name}_L{lv}_d{d}_az{az}.png"
            if not fk.exists():
                continue
            rgbk, ak = load(fk)
            ck = comp(rgbk, ak, d)
            sil = float(np.abs(a0 - ak).sum() / max(a0.sum(), 1e-9))
            m = (a0 > 0.1) | (ak > 0.1)
            diff = np.abs(c0 - ck).max(axis=2) * 255
            pix = float((diff[m] > 12).mean()) if m.any() else 0.0
            mad = float(diff[m].mean()) if m.any() else 0.0
            db = np.abs(blur(c0) - blur(ck)).max(axis=2) * 255
            pixb = float((db[m] > 8).mean()) if m.any() else 0.0
            tone = float(np.abs(c0[a0 > 0.5].mean(axis=0) - ck[ak > 0.5].mean(axis=0)).max() * 255) if (a0 > 0.5).any() and (ak > 0.5).any() else 0.0
            cov = float(ak.sum() / max(a0.sum(), 1e-9))
            rows.append(dict(piece=name, level=lv, d=d, az=az, sil=round(sil, 4), pix=round(pix, 4), pixb=round(pixb, 4), mad=round(mad, 2), tone=round(tone, 2), cov=round(cov, 3), h=h, fog=round(fog(d), 3)))
    if out:
        Path(out).write_text(json.dumps(rows, indent=1))
    return rows


if __name__ == "__main__":
    rows = run(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else None)
    import collections
    agg = collections.defaultdict(list)
    for r in rows:
        agg[(r["piece"], r["level"], r["d"])].append(r)
    print(f"{'piece':20s} L   d   h(px)  fog  sil%  pix%  blur%  mad  tone  cover")
    for k in sorted(agg):
        v = agg[k]
        print(f"{k[0]:20s} L{k[1]} {k[2]:4d} {int(np.mean([r['h'] for r in v])):5d}  {v[0]['fog']:.2f}  {100*np.mean([r['sil'] for r in v]):5.1f} {100*np.mean([r['pix'] for r in v]):5.1f} {100*np.mean([r['pixb'] for r in v]):5.1f} {np.mean([r['mad'] for r in v]):5.1f} {np.mean([r['tone'] for r in v]):5.1f} {np.mean([r['cov'] for r in v]):5.2f}")
