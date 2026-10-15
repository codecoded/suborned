import json, numpy as np
from lib import *
from sil import *
from de import srgb2lab, hex2rgb
C = json.load(open(SCR / "colours4.json"))
out = {}
for key in ("L1", "L2", "L3", "L4"):
    im, m = mask_of(DEL / f"lineups/R4-lineup-{key}.png"); figs = split4(m); rgb = im[..., ::-1].astype(np.float32); res = {}
    for role, f in zip(("scout", "fixer", "tech", "enforcer"), figs):
        px = rgb[f > 0]; lab = srgb2lab(px)
        ref = np.median([hex2rgb(x['painted']) for k, x in C['painted'][role].items() if x and not k.endswith("hero-block")], axis=0)
        d = np.sqrt(((lab - srgb2lab(ref)) ** 2).sum(1)); res[role] = round(float((d < 22).mean()), 3)
    out[key] = res
json.dump(out, open(SCR / "hero_share.json", "w"), indent=1); print(out)
