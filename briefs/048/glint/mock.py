# Glint, brief 048: a MOCK of the third-person haze over Sky's build 0.2.65 screenshot (no render was possible: Latch owns the editor).
# Depth is estimated: the ground plane analytically (camera ~2.5 m up, 55 deg vertical fov, horizon y 385) and per-column bands for the
# standing masses. min(band, ground(y)) is the depth of each pixel. Fog is applied in linear light toward the haze colour, as URP does.
import sys, math
import numpy as np
from PIL import Image, ImageDraw, ImageFont

SRC = sys.argv[1]; OUT = sys.argv[2]
im = np.asarray(Image.open(SRC).convert('RGB')).astype(np.float32) / 255.0
H, W, _ = im.shape
YH, F, CAMH = 385.0, 697.0, 2.5
HAZE = np.array([0xf3, 0xe8, 0xd4], np.float32) / 255.0

def lin(c): return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
def srgb(c): return np.where(c <= 0.0031308, c * 12.92, 1.055 * np.clip(c, 0, None) ** (1 / 2.4) - 0.055)

ys, xs = np.mgrid[0:H, 0:W].astype(np.float32)
ground = np.where(ys > YH + 1.5, CAMH * F / np.maximum(ys - YH, 1.5), 1e5)
band = np.full((H, W), 1e5, np.float32)
def box(x0, x1, y0, y1, d):
    band[int(y0):int(y1), int(x0):int(x1)] = d
box(0, 395, 0, 400, 55); box(0, 640, 400, H, 22)
box(395, 560, 0, 400, 130); box(560, 740, 0, 400, 150)
box(740, 1180, 0, 432, 45)
box(1180, 1340, 0, 430, 110); box(1180, 1262, 0, 285, 380)        # palms, the needle
box(1340, 1560, 0, 400, 240)
box(1560, W, 0, H, 17)
# soften the box edges (a mock, not a model): a horizontal blur of the band depth
k = 41; ker = np.ones(k, np.float32) / k
pad = np.pad(band, ((0, 0), (k // 2, k // 2)), mode='edge')
cs = np.cumsum(np.pad(pad, ((0, 0), (1, 0))), axis=1); band = ((cs[:, k:] - cs[:, :-k]) / k).astype(np.float32)
depth = np.minimum(band, ground)
# the road end: the far buildings stand on the ground, so ground limits them near their feet
sky = (im[..., 2] >= im[..., 0] - 0.005) & (im[..., 2] >= im[..., 1] - 0.005) & (im[..., 2] > 0.70) & (ys < YH)

def fog_factor(d, kind, p):
    if kind in ('exp2', 'exp2b', 'exp2c'): return 1 - np.exp(-(d * p) ** 2)
    if kind == 'lin': return np.clip((d - p[0]) / (p[1] - p[0]), 0, 1)

def render(kind, p):
    f = fog_factor(depth, kind, p)[..., None]
    f = np.where(sky[..., None], 0.0, f)
    out = srgb(lin(im) * (1 - f) + lin(HAZE) * f)
    # the sky already carries the haze at the horizon (SubornedSky: haze to mid over sin(elev) 0..0.16); rebuild it below 0.16
    e = np.sin(np.arctan(np.maximum(YH - ys, 0) / F))
    t = np.clip(e / 0.16, 0, 1); t = t * t * (3 - 2 * t)
    skyc = srgb(lin(HAZE) * (1 - t[..., None]) + lin(im) * t[..., None])
    out = np.where(sky[..., None], skyc, out)
    return out, f[..., 0]

def label(a, text):
    img = Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8)); d = ImageDraw.Draw(img)
    try: ft = ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial Bold.ttf', 26)
    except Exception: ft = ImageFont.load_default()
    d.rectangle([10, H - 46, 10 + 15 * len(text), H - 12], fill=(23, 48, 47)); d.text((18, H - 44), text, fill=(247, 244, 236), font=ft)
    return img

before, _ = render('exp2', 0.0)
# "before": the screenshot untouched (sky rebuilt identically is skipped)
before = im
# far clip 350 m (Aster, brief 048): density = sqrt(-ln(1 - reach)) / farClip, reach 0.97
D350 = math.sqrt(-math.log(1 - 0.97)) / 350
cands = {
  'exp2 0.0029 (far clip 600)': ('exp2', 0.0029),
  'exp2 0.00385 (far clip 450, reach 0.95)': ('exp2b', 0.00385),
  'exp2 %.5f (far clip 350, reach 0.97)' % D350: ('exp2c', D350),
  'linear 80 to 600': ('lin', (80, 600)),
}
Image.fromarray((before * 255).astype(np.uint8)).save(OUT + '/before.png')
for name, (k, p) in cands.items():
    a, f = render(k, p)
    label(a, name + '  MOCK').save(OUT + '/after-' + k + '.png')
    if k == 'exp2c': chosen = a; chosenf = f
    if k == 'exp2b': a450 = a
# depth debug and fog map
Image.fromarray((np.clip(depth / 400, 0, 1) * 255).astype(np.uint8)).save(OUT + '/mock-depth.png')
# fog table
print('d   far450(0.00385)  far350(%.5f)' % D350)
for d in (40, 60, 80, 100, 120, 150, 200, 250, 300, 350, 450):
    print(d, round(float(fog_factor(np.float32(d), 'exp2b', 0.00385)), 3), round(float(fog_factor(np.float32(d), 'exp2c', D350)), 3))
# side-by-side
b = Image.fromarray((before * 255).astype(np.uint8)); a = label(chosen, 'AFTER (mock): haze, far clip 350 m, density %.5f' % D350); 
sheet = Image.new('RGB', (W, H * 2 + 8), (23, 48, 47)); sheet.paste(label(before, 'BEFORE: build 0.2.65 as played'), (0, 0)); sheet.paste(a, (0, H + 8))
sheet.save(OUT + '/before-after.png')
