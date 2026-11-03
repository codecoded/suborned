"""How dark and what colour each figure's clothing is, from the albedo atlas of a build (brief 067, Forma; Pulse's measure, briefs/067/pulse/albedo-stats.py, with the folder as an argument).
  python3 -I tools/figures/albedo_stats.py [--base <People/Figures dir>] [--json] [--check] [id ...]
Default base: the build (out/people-models/People/Figures), else the Unity copy (unity/Assets/Suborned/People/Figures). The atlas read is the 1024 albedo (or the nearest size).
Skin (hue 8-35, saturation .25-.65, value .35-.95) and the atlas's empty black are left out; what is left is the clothing. median V = HSV value of the median clothing pixel;
dark = share of clothing pixels under V .35; violet = hue 250-310, S > .25; amber = hue 20-70, S > .3, V > .3; navy = hue 200-250, S > .3, V < .5; mint = hue 140-190; silver = V > .72, S < .2.
--check holds the rival crew to Pulse's spec for brief 067 W1 (W1, briefs/067/pulse.md): median V >= 0.45, violet >= 0.40, dark <= 0.40, amber <= 0.01."""
import argparse, glob, json, os, sys
import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
RIVALS = ['rival', 'rival_b', 'rival_c', 'rival_d']
DEFAULT = ['guard', 'security', 'rival', 'rival_b', 'rival_c', 'rival_d', 'workers_d5', 'driver', 'd2m', 'd4', 'd4b', 'd5c', 'councillor', 'waitress', 'c6', 'd6', 'el1', 'sh2', 'enforcer', 'scout', 'fixer', 'tech']


def hsv(a):
    a = a / 255.0; mx = a.max(-1); mn = a.min(-1); d = mx - mn
    s = np.where(mx > 0, d / np.maximum(mx, 1e-9), 0); h = np.zeros_like(mx); r, g, b = a[..., 0], a[..., 1], a[..., 2]; m = d > 1e-9
    h = np.where(m & (mx == r), ((g - b) / np.maximum(d, 1e-9)) % 6, h); h = np.where(m & (mx == g), (b - r) / np.maximum(d, 1e-9) + 2, h); h = np.where(m & (mx == b), (r - g) / np.maximum(d, 1e-9) + 4, h)
    return h * 60, s, mx


def stats(path):
    a = np.asarray(Image.open(path).convert('RGB')).astype(float).reshape(-1, 3); h, s, v = hsv(a)
    skin = (h > 8) & (h < 35) & (s > .25) & (s < .65) & (v > .35) & (v < .95); keep = ~skin & (v > .06); k = keep.sum()
    sh = lambda m: float(((m) & keep).sum() / k)
    return {'medianV': float(np.median(v[keep])), 'dark': sh(v < .35), 'violet': sh((h > 250) & (h < 310) & (s > .25)), 'amber': sh((h >= 20) & (h <= 70) & (s > .3) & (v > .3)),
            'navy': sh((h > 200) & (h < 250) & (s > .3) & (v < .5)), 'mint': sh((h > 140) & (h < 190) & (s > .2) & (v > .5)), 'silver': sh((v > .72) & (s < .2))}


def albedo(base, fid):
    p = sorted(glob.glob(f'{base}/{fid}/Textures/*1024*albedo.jpg')) or sorted(glob.glob(f'{base}/{fid}/Textures/*albedo.jpg'))
    return p[0] if p else None


def default_base():
    for b in (os.path.join(ROOT, 'out/people-models/People/Figures'), os.path.join(ROOT, 'unity/Assets/Suborned/People/Figures')):
        if os.path.isdir(b): return b
    raise SystemExit('albedo_stats: no figures folder (build with bun tools/figures/run.ts)')


def check(rows):
    """The spec of Pulse's W1 for each rival colourway in rows; returns the list of faults."""
    out = []
    for f in RIVALS:
        r = rows.get(f)
        if r is None: out.append(f'{f}: not built'); continue
        if r['medianV'] < 0.45: out.append(f'{f}: median V {r["medianV"]:.2f} < 0.45')
        if r['violet'] < 0.40: out.append(f'{f}: violet {r["violet"]:.2f} < 0.40')
        if r['dark'] > 0.40: out.append(f'{f}: dark {r["dark"]:.2f} > 0.40')
        if r['amber'] > 0.01: out.append(f'{f}: amber {r["amber"]:.3f} > 0.01')
    return out


if __name__ == '__main__':
    ap = argparse.ArgumentParser(); ap.add_argument('ids', nargs='*'); ap.add_argument('--base'); ap.add_argument('--json', action='store_true'); ap.add_argument('--check', action='store_true')
    a = ap.parse_args(); base = a.base or default_base(); ids = a.ids or DEFAULT; rows = {}
    for f in ids:
        p = albedo(base, f)
        if p: rows[f] = stats(p)
    if a.json: print(json.dumps(rows, indent=1))
    else:
        print(f'base {base}'); print(f'{"figure":11s} {"median V":>8s} {"dark <.35":>9s} {"violet":>7s} {"amber":>6s} {"navy":>5s} {"mint":>5s} {"silver":>6s}   (shares of the clothing pixels)')
        for f in ids:
            r = rows.get(f)
            print(f'{f:11s} not found' if r is None else f'{f:11s} {r["medianV"]:8.2f} {r["dark"]:9.2f} {r["violet"]:7.2f} {r["amber"]:6.2f} {r["navy"]:5.2f} {r["mint"]:5.2f} {r["silver"]:6.2f}')
    if a.check:
        bad = check(rows)
        print('\nspec (brief 067 W1): ' + ('OK, every rival colourway inside it' if not bad else 'FAILS: ' + '; '.join(bad)))
        sys.exit(1 if bad else 0)
