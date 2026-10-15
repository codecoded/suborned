"""dE2000 tables for the four wardrobes: target dominant and accent vs signals and vs the city; painted clusters; pairwise separation between people (targets and as painted, at 50 px)."""
import json, numpy as np, cv2
from PIL import Image
from de import *
from lib import *
from sil import mask_of
import p7

TARGET = {r: (hx(p["dom"]), hx(p["acc"])) for r, p in []} if False else None
import re
def hx(s): return re.search(r"#([0-9a-fA-F]{6})", s).group(0)
TG = {r: dict(dom=hx(p["dom"]), acc=hx(p["acc"])) for r, p in p7.PEOPLE_B.items()}
TG_A = {r: dict(dom=hx(p["dom"]), acc=hx(p["acc"])) for r, p in p7.PEOPLE.items()}
CITY = {"cream #f9efdc": "#f9efdc", "peach #f9e2cc": "#f9e2cc", "mint #8fe3c2": "#8fe3c2", "seafoam #72b5a7": "#72b5a7", "civic teal #1f6c67": "#1f6c67", "turquoise #1cb9c6": "#1cb9c6",
        "cobalt #2f84e8": "#2f84e8", "flamingo #e8609a": "#e8609a", "sunshine #fdeb8c": "#fdeb8c", "asphalt #a5a2a8": "#a5a2a8", "park #a6e29c": "#a6e29c", "lagoon #3d8d93": "#3d8d93"}
def dd(a, b): return de2000(srgb2lab(hex2rgb(a)), srgb2lab(hex2rgb(b)))
def city_min(h): return min((round(dd(h, c), 1), n) for n, c in CITY.items())
out = dict(targets={}, mutual={}, painted={}, targets_A={}, mutual_A={}, painted_A={})
for r, t in TG.items():
    out["targets"][r] = {k: dict(hex=h, **row(h), city_min=city_min(h)[0], city_nearest=city_min(h)[1], L=round(float(srgb2lab(hex2rgb(h))[0]), 1)) for k, h in t.items()}
names = list(TG)
out["mutual"] = {f"{a}-{b}": round(dd(TG[a]["dom"], TG[b]["dom"]), 1) for i, a in enumerate(names) for b in names[i + 1:]}
# painted clusters on the four tops (skin removed)
TOP = dict(scout="wardrobes_B/R3b-wB-scout-1-trail-runner", fixer="wardrobes/R3b-w-fixer-1-knit-tunic", tech="wardrobes_B/R3b-wB-tech-2-canvas-apron", enforcer="wardrobes_B/R3b-wB-enforcer-2-lacquer-yoke")
TOP_A = dict(scout="wardrobes/R3b-w-scout-1-trail-runner", fixer="wardrobes/R3b-w-fixer-1-knit-tunic", tech="wardrobes/R3b-w-tech-2-canvas-apron", enforcer="wardrobes/R3b-w-enforcer-2-lacquer-yoke")
def clusters(path, k=6):
    im, m = mask_of(path, (1024, 1536)); px = im[m > 0].astype(np.float32)
    ycc = cv2.cvtColor(px.reshape(-1, 1, 3).astype(np.uint8), cv2.COLOR_BGR2YCrCb).reshape(-1, 3)
    skin = (ycc[:, 1] > 135) & (ycc[:, 1] < 175) & (ycc[:, 2] > 85) & (ycc[:, 2] < 130) & (ycc[:, 0] > 60)
    px = px[~skin][:, ::-1]  # RGB, skin removed (a crude test: it also removes warm browns, so brown wardrobes are under-counted)
    rng = np.random.default_rng(3); cen = px[rng.choice(len(px), k, replace=False)]
    for _ in range(25):
        lab = np.argmin(((px[:, None] - cen[None]) ** 2).sum(-1), 1); cen = np.array([px[lab == j].mean(0) if (lab == j).any() else cen[j] for j in range(k)])
    w = np.bincount(lab, minlength=k) / len(px); res = []
    for j in np.argsort(-w)[:4]:
        h = '#%02x%02x%02x' % tuple(int(round(v)) for v in cen[j]); res.append(dict(hex=h, share=round(float(w[j]), 3), **row(h), city_min=city_min(h)[0]))
    return res
for r, stem in TOP.items(): out["painted"][r] = clusters(DEL / f"{stem}.png")
for r, stem in TOP_A.items(): out["painted_A"][r] = clusters(DEL / f"{stem}.png")
for r, t in TG_A.items(): out["targets_A"][r] = {k: dict(hex=h, **row(h), city_min=city_min(h)[0], city_nearest=city_min(h)[1], L=round(float(srgb2lab(hex2rgb(h))[0]), 1)) for k, h in t.items()}
out["mutual_A"] = {f"{a}-{b}": round(dd(TG_A[a]["dom"], TG_A[b]["dom"]), 1) for i, a in enumerate(names) for b in names[i + 1:]}
(SCR / "colours.json").write_text(json.dumps(out, indent=1))
print("mutual B", out["mutual"]); print("mutual A", out["mutual_A"])
for r, c in out["painted"].items(): print(r, [(x["hex"], x["share"], x["cones"], x["guard"], x["rivals"], x["city_min"]) for x in c])
