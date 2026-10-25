"""Brief 063 Vale C, Glint's matte check as numbers (no Unity, no render): the model's albedo (out/people-models/People/Figures/councillor, after
`bun tools/figures/run.ts --only councillor`) lit by the day sun and ambient as light.json and lens.json set them, through the play grade.
Run from the repo root's copy of this folder: python3 -I vale.py (reads ../../../out/people-models)."""
import sys; sys.path.insert(0, '.')
from fit import *
from PIL import Image
im = Image.open('../../../out/people-models/People/Figures/councillor/Textures/councillor_body_2048_albedo.jpg').convert('RGB').resize((512, 512))
px = np.asarray(im, float).reshape(-1, 3) / 255; lin = np.where(px <= 0.04045, px / 12.92, ((px + 0.055) / 1.055) ** 2.4); Y = lin @ np.array([0.2126, 0.7152, 0.0722])
m = Y > 0.003; bright = m & (Y > np.percentile(Y[m], 80)); dark = m & (Y < 0.02)
iv, bk, gold = lin[bright].mean(0), lin[dark].mean(0), s2l('#916b41')
print('albedo: ivory %s (lin %s), black plates %s, gold trim ~%s (the brightest fifth, the darkest, the saturated trim)' % (hexof(iv), iv.round(2), hexof(bk), '#916b41'))
sun = 1.8 * s2l('#ffe0d0'); amb_eq = 0.71 * s2l('#faeadc'); amb_up = 0.69 * s2l('#dedce0')
G = Grade(exposure=0.15, contrast=6, saturation=36.9, shadows='#c7d1eb', lift=(0.04, 0.975, 0.90, 0.87))   # play grade with its lift
lit = {'top, in sun': sun * 0.62 + amb_up, 'wall facing the sun': sun * 0.79 + amb_eq, 'wall in shade (the Records Office front)': amb_eq * 0.9}
print('%-42s %-8s %-8s %-8s %-10s' % ('lit as (day)', 'ivory', 'gold', 'black', 'black+spec'))
for k, v in lit.items(): print('%-42s %-8s %-8s %-8s %-10s' % (k, hexof(G(iv * v)), hexof(G(gold * v)), hexof(G(bk * v)), hexof(G(bk * v + 0.010))))
st = s2l('#e4dacb')
print('dE00 ivory vs stone of the same albedo as the street paving: sun wall %.1f, shade wall %.1f' % (de2000(lab(G(iv * lit['wall facing the sun'])), lab(G(st * lit['wall facing the sun']))), de2000(lab(G(iv * lit['wall in shade (the Records Office front)'])), lab(G(st * lit['wall in shade (the Records Office front)'])))))
print('albedo scales on the ivory (linear) and dE00 to the stone (sun / shade):')
for t in ((0.95, 0.92, 0.86), (0.92, 0.86, 0.76), (0.9, 0.82, 0.7)):
    t = np.array(t); a, b = G(iv * t * lit['wall facing the sun']), G(iv * t * lit['wall in shade (the Records Office front)'])
    print('  ', t, hexof(a), hexof(b), '%.1f / %.1f' % (de2000(lab(a), lab(G(st * lit['wall facing the sun']))), de2000(lab(b), lab(G(st * lit['wall in shade (the Records Office front)'])))))
