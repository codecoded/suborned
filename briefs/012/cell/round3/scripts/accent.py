"""Painted personal accents in the icon takes vs the signals (dE2000) and vs target hex."""
import numpy as np, json
from PIL import Image
from de import *
from lib import *
SPEC = {"scout": ("#4f7a4a", 70, 170, 0.12, 0.10, 0.62), "fixer": ("#74483a", 8, 28, 0.35, 0.18, 0.50),
        "tech": ("#a9d9e0", 168, 205, 0.12, 0.55, 1.0), "enforcer": ("#1f3a5f", 205, 240, 0.35, 0.18, 0.50)}
def meas(path, spec):
    _, h0, h1, s0, v0, v1 = spec
    im = np.asarray(Image.open(path).convert("RGB").resize((512, 768))).reshape(-1, 3) / 255.0
    mx = im.max(1); mn = im.min(1); d = np.maximum(mx - mn, 1e-6); s = (mx - mn) / np.maximum(mx, 1e-6); r, g, b = im.T
    h = np.where(mx == r, ((g - b) / d) % 6, np.where(mx == g, (b - r) / d + 2, (r - g) / d + 4)) * 60
    m = (h >= h0) & (h <= h1) & (s >= s0) & (mx >= v0) & (mx <= v1)
    if m.sum() < 80: return None
    med = np.median(im[m], 0) * 255; hx = '#%02x%02x%02x' % tuple(int(v) for v in med)
    return dict(share=round(float(m.mean()), 4), painted=hx, **row(hx))
if __name__ == "__main__":
    out = {}
    for role, spec in SPEC.items():
        out[role] = {"target": spec[0], "target_dE": row(spec[0]), "takes": {}}
        for p in sorted((DEL / "2_icons").glob(f"R3-2-{role}-*.png")):
            out[role]["takes"][p.stem] = meas(p, spec)
    (SCR / "accent_dE.json").write_text(json.dumps(out, indent=1)); print(json.dumps(out, indent=1))
