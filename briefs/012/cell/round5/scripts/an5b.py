"""Round 5: the same signal check on every single take (not only the lineups): nearest painted colour and share within dE 20 of cone red, guard amber, rival violet."""
import json, numpy as np
from lib import *
from sil import mask_of
from de import srgb2lab, de2000, hex2rgb, SIG
SIGLAB = {k: srgb2lab(hex2rgb(v)) for k, v in SIG.items()}
hexof = lambda rgb: '#%02x%02x%02x' % tuple(int(round(v)) for v in rgb)
def skin_ref(im, fig):
    ys, xs = np.nonzero(fig); y0, y1 = ys.min(), ys.max(); h = y1 - y0
    rgb = im[..., ::-1].astype(np.float32); head = (fig > 0).copy(); head[:y0 + int(0.04 * h)] = 0; head[y0 + int(0.16 * h):] = 0
    hp = rgb[head > 0]; keep = srgb2lab(hp)[:, 0] > 35
    return srgb2lab(np.median(hp[keep], 0) if keep.sum() > 30 else np.array([180, 130, 100.0]))
def check(path):
    im, m = mask_of(path, (1024, 1536)); rgb = im[..., ::-1].astype(np.float32); fl = skin_ref(im, m)
    ys, xs = np.nonzero(m); px = rgb[ys, xs]; lab = srgb2lab(px); ok = (np.sqrt(((lab - fl) ** 2).sum(1)) > 18) & (lab[:, 0] > 8); px = px[ok]
    q = (px // 16).astype(np.int32); key = q[:, 0] * 256 + q[:, 1] * 16 + q[:, 2]
    uk, inv, cnt = np.unique(key, return_inverse=True, return_counts=True)
    cols = np.array([px[inv == i].mean(0) for i in range(len(uk))]); share = cnt / cnt.sum(); out = {}
    for s in SIG:
        d = np.array([de2000(srgb2lab(c), SIGLAB[s]) for c in cols]); big = share >= 0.005; i = np.argmin(np.where(big, d, 1e9))
        out[s] = dict(nearest_painted=hexof(cols[i]), dE=round(float(d[i]), 1), share_within_20=round(float(share[d < 20].sum()), 4))
    return out
res = {}
for d in "abc":
    for r in ("scout", "fixer", "tech", "enforcer"):
        res[f"{d}-{r}"] = check(DEL / f"takes/R5-{d}-{r}.png"); print(d, r, {s: (v['dE'], v['share_within_20']) for s, v in res[f'{d}-{r}'].items()}, flush=True)
(DEL / "takes5.json").write_text(json.dumps(res, indent=1))
