"""dE2000 of the cell accents against the reserved signals: (1) the named target hex per accent; (2) the dominant clusters actually painted in each Codex lineup."""
import numpy as np, sys, json
from PIL import Image
from lib import *

def srgb2lab(rgb):
    c = np.asarray(rgb, float) / 255.0
    c = np.where(c > 0.04045, ((c + 0.055) / 1.055) ** 2.4, c / 12.92)
    M = np.array([[0.4124564, 0.3575761, 0.1804375], [0.2126729, 0.7151522, 0.0721750], [0.0193339, 0.1191920, 0.9503041]])
    xyz = c @ M.T / np.array([0.95047, 1.0, 1.08883])
    f = np.where(xyz > 216 / 24389, np.cbrt(xyz), (24389 / 27 * xyz + 16) / 116)
    return np.stack([116 * f[..., 1] - 16, 500 * (f[..., 0] - f[..., 1]), 200 * (f[..., 1] - f[..., 2])], -1)

def de2000(l1, l2):
    L1, a1, b1 = l1; L2, a2, b2 = l2
    C1, C2 = np.hypot(a1, b1), np.hypot(a2, b2); Cb = (C1 + C2) / 2
    G = 0.5 * (1 - np.sqrt(Cb ** 7 / (Cb ** 7 + 25 ** 7)))
    a1p, a2p = (1 + G) * a1, (1 + G) * a2
    C1p, C2p = np.hypot(a1p, b1), np.hypot(a2p, b2)
    h1p = np.degrees(np.arctan2(b1, a1p)) % 360; h2p = np.degrees(np.arctan2(b2, a2p)) % 360
    dL = L2 - L1; dC = C2p - C1p
    dh = h2p - h1p
    if C1p * C2p == 0: dh = 0
    elif dh > 180: dh -= 360
    elif dh < -180: dh += 360
    dH = 2 * np.sqrt(C1p * C2p) * np.sin(np.radians(dh / 2))
    Lb = (L1 + L2) / 2; Cbp = (C1p + C2p) / 2
    if C1p * C2p == 0: hb = h1p + h2p
    elif abs(h1p - h2p) <= 180: hb = (h1p + h2p) / 2
    elif h1p + h2p < 360: hb = (h1p + h2p + 360) / 2
    else: hb = (h1p + h2p - 360) / 2
    T = 1 - 0.17 * np.cos(np.radians(hb - 30)) + 0.24 * np.cos(np.radians(2 * hb)) + 0.32 * np.cos(np.radians(3 * hb + 6)) - 0.20 * np.cos(np.radians(4 * hb - 63))
    dth = 30 * np.exp(-(((hb - 275) / 25) ** 2))
    Rc = 2 * np.sqrt(Cbp ** 7 / (Cbp ** 7 + 25 ** 7))
    Sl = 1 + 0.015 * (Lb - 50) ** 2 / np.sqrt(20 + (Lb - 50) ** 2); Sc = 1 + 0.045 * Cbp; Sh = 1 + 0.015 * Cbp * T
    Rt = -np.sin(np.radians(2 * dth)) * Rc
    return float(np.sqrt((dL / Sl) ** 2 + (dC / Sc) ** 2 + (dH / Sh) ** 2 + Rt * (dC / Sc) * (dH / Sh)))

def hex2rgb(h): h = h.lstrip('#'); return [int(h[i:i + 2], 16) for i in (0, 2, 4)]
SIG = {"cones": "#e03a2f", "guard": "#f0a020", "rivals": "#8a4fd0"}
def row(hexc):
    lab = srgb2lab(hex2rgb(hexc)); return {k: round(de2000(lab, srgb2lab(hex2rgb(v))), 1) for k, v in SIG.items()}

if __name__ == "__main__":
    out = {"candidates": {}, "painted": {}}
    for n, h in {"teal-black #12403f": "#12403f", "teal-black lighter #1d5a58": "#1d5a58", "burnt sienna #a0522d": "#a0522d", "sienna as painted-ish #8a4a30": "#8a4a30",
                 "coral contract #fc9c84": "#fc9c84", "bone #e8e4da": "#e8e4da", "charcoal #2b2d30": "#2b2d30", "seafoam deep teal #1f6c67": "#1f6c67",
                 "violet #6b4f8f (old violet_set)": "#6b4f8f"}.items():
        out["candidates"][n] = row(h)
    for acc in ("teal", "sienna", "white"):
        p = RAW / f"A-codex-lineup-{acc}.png"
        if not p.exists(): continue
        im = np.asarray(Image.open(p).convert("RGB").resize((768, 512))).reshape(-1, 3).astype(float)
        bg = np.median(im[:2000], axis=0)  # top rows are backdrop
        keep = im[np.linalg.norm(im - bg, axis=1) > 38]
        rng = np.random.default_rng(1); cen = keep[rng.choice(len(keep), 10, replace=False)]
        for _ in range(25):
            lab = np.argmin(((keep[:, None] - cen[None]) ** 2).sum(-1), 1)
            cen = np.array([keep[lab == k].mean(0) if (lab == k).any() else cen[k] for k in range(10)])
        w = np.bincount(lab, minlength=10) / len(keep)
        res = []
        for k in np.argsort(-w):
            hx = '#%02x%02x%02x' % tuple(int(round(v)) for v in cen[k]); res.append({"hex": hx, "share": round(float(w[k]), 3), **row(hx)})
        out["painted"][acc] = res
    print(json.dumps(out, indent=1))
    (SCR / "de2000.json").write_text(json.dumps(out, indent=1))
