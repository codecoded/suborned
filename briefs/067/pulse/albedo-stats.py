"""Pulse, brief 067 I: how dark each figure's clothing is, from the albedo atlas Unity installs (unity/Assets/Suborned/People/Figures/<id>/Textures/<id>_body_1024_albedo.jpg).
Skin (hue 8-35, saturation .25-.65) and the atlas's empty black are left out; what is left is the clothing. median value = HSV V of the median clothing pixel (0 black, 1 full);
dark = share of clothing pixels under V .35. The guard, the police figure ("security") and the rival are the three that matter: at play zoom a figure under V .35 reads as a dark uniform.
python3 -I briefs/067/pulse/albedo-stats.py > briefs/067/pulse/albedo-stats.txt"""
import glob, numpy as np
from PIL import Image
base = '/Users/sky/Projects/suborned/unity/Assets/Suborned/People/Figures/'
names = ['guard', 'security', 'rival', 'workers_d5', 'driver', 'd2m', 'd4', 'd4b', 'd5c', 'councillor', 'waitress', 'cafe_w2', 'cafe_w3', 'cafe_w5', 'c6', 'd6', 'el1', 'sh2', 'enforcer', 'scout', 'fixer', 'tech']
def hsv(a):
    a = a / 255.0; mx = a.max(-1); mn = a.min(-1); d = mx - mn
    s = np.where(mx > 0, d / np.maximum(mx, 1e-9), 0); h = np.zeros_like(mx); r, g, b = a[..., 0], a[..., 1], a[..., 2]; m = d > 1e-9
    h = np.where(m & (mx == r), ((g - b) / np.maximum(d, 1e-9)) % 6, h); h = np.where(m & (mx == g), (b - r) / np.maximum(d, 1e-9) + 2, h); h = np.where(m & (mx == b), (r - g) / np.maximum(d, 1e-9) + 4, h)
    return h * 60, s, mx
print(f'{"figure":11s} {"median V":>8s} {"dark <.35":>9s} {"violet":>7s} {"amber":>6s} {"navy":>5s} {"mint":>5s} {"silver":>6s}   (shares of the clothing pixels; amber = hue 20-70, S > .3, V > .3)')
for n in names:
    p = sorted(glob.glob(f'{base}{n}/Textures/*1024*albedo.jpg'))
    if not p: print(f'{n:11s} not installed'); continue
    a = np.asarray(Image.open(p[0]).convert('RGB')).astype(float).reshape(-1, 3); h, s, v = hsv(a)
    skin = (h > 8) & (h < 35) & (s > .25) & (s < .65) & (v > .35) & (v < .95); keep = ~skin & (v > .06); k = keep.sum()
    sh = lambda m: ((m) & keep).sum() / k
    print(f'{n:11s} {np.median(v[keep]):8.2f} {sh(v < .35):9.2f} {sh((h > 250) & (h < 310) & (s > .25)):7.2f} {sh((h >= 20) & (h <= 70) & (s > .3) & (v > .3)):6.2f} {sh((h > 200) & (h < 250) & (s > .3) & (v < .5)):5.2f} {sh((h > 140) & (h < 190) & (s > .2) & (v > .5)):5.2f} {sh((v > .72) & (s < .2)):6.2f}')
