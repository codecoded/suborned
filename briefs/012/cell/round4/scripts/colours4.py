"""Round 4 colour tables: hero colours and the white/silver/charcoal base vs signals and city; mutual hero separation; painted hero colours measured from the takes."""
import json, re, numpy as np
from PIL import Image
from de import *
from lib import *
import p11
CITY = {"cream #f9efdc": "#f9efdc", "peach #f9e2cc": "#f9e2cc", "mint #8fe3c2": "#8fe3c2", "seafoam #72b5a7": "#72b5a7", "civic teal #1f6c67": "#1f6c67", "turquoise #1cb9c6": "#1cb9c6", "cobalt #2f84e8": "#2f84e8",
        "flamingo #e8609a": "#e8609a", "sunshine #fdeb8c": "#fdeb8c", "asphalt #a5a2a8": "#a5a2a8", "park #a6e29c": "#a6e29c", "lagoon #3d8d93": "#3d8d93", "coral #fc9c84": "#fc9c84"}
def dd(a, b): return de2000(srgb2lab(hex2rgb(a)), srgb2lab(hex2rgb(b)))
def cm(h): return min((round(dd(h, c), 1), n) for n, c in CITY.items())
hx = lambda s: re.search(r"#([0-9a-fA-F]{6})", s).group(0)
HERO = {r: hx(p["hero"]) for r, p in p11.PEOPLE.items()}
BASE = {"cool white #f2f4f6": "#f2f4f6", "brushed silver #c8ccd0": "#c8ccd0", "charcoal #2b2d30": "#2b2d30", "lamp aqua #8fe3e0": "#8fe3e0"}
out = dict(hero={}, base={}, mutual={}, painted={})
for r, h in HERO.items():
    out["hero"][r] = dict(hex=h, **row(h), city_min=cm(h)[0], city_nearest=cm(h)[1], L=round(float(srgb2lab(hex2rgb(h))[0]), 1))
for n, h in BASE.items(): out["base"][n] = dict(hex=h, **row(h), city_min=cm(h)[0], city_nearest=cm(h)[1])
names = list(HERO)
out["mutual"] = {f"{a}-{b}": round(dd(HERO[a], HERO[b]), 1) for i, a in enumerate(names) for b in names[i + 1:]}
out["lamp_vs_hero"] = {r: round(dd("#8fe3e0", h), 1) for r, h in HERO.items()}
RANGE = dict(scout=(55, 100, 0.45, 0.45), fixer=(325, 355, 0.25, 0.65), tech=(140, 175, 0.55, 0.30), enforcer=(185, 210, 0.60, 0.45))
def meas(path, role):
    h0, h1, s0, v0 = RANGE[role]
    im = np.asarray(Image.open(path).convert("RGB").resize((512, 768))).reshape(-1, 3) / 255.0
    mx = im.max(1); mn = im.min(1); d = np.maximum(mx - mn, 1e-6); s = (mx - mn) / np.maximum(mx, 1e-6); r, g, b = im.T
    h = np.where(mx == r, ((g - b) / d) % 6, np.where(mx == g, (b - r) / d + 2, (r - g) / d + 4)) * 60
    m = (h >= h0) & (h <= h1) & (s >= s0) & (mx >= v0)
    if m.sum() < 100: return None
    med = np.median(im[m], 0) * 255; hxx = '#%02x%02x%02x' % tuple(int(v) for v in med)
    return dict(share=round(float(m.mean()), 4), painted=hxx, **row(hxx), city_min=cm(hxx)[0], target_dE=round(dd(hxx, HERO[role]), 1))
for role in HERO:
    out["painted"][role] = {p.stem: meas(p, role) for p in sorted((DEL / "takes").glob(f"R4-{role}-*.png"))}
(SCR / "colours4.json").write_text(json.dumps(out, indent=1))
print(json.dumps(out["hero"], indent=0)); print(out["mutual"]); print(out["lamp_vs_hero"])
for r, v in out["painted"].items():
    for k, x in v.items(): print(k, x)
