# Glint, brief 048: do the signals survive the haze? Each signal colour (unlit, as drawn) fogged to d metres with the chosen profile, in linear light,
# against its own fogged surroundings (stone, haze) and against the unfogged colour: CIE76 dE to itself, hue shift, and contrast against the haze.
import numpy as np, colorsys
from PIL import Image, ImageDraw, ImageFont
FAR, REACH = 350.0, 0.97
K = np.sqrt(-np.log(1 - REACH)) / FAR
HAZE = np.array([0xf3, 0xe8, 0xd4]) / 255
SIG = {'cones red': (0.878, 0.227, 0.184), 'guard amber': (0.941, 0.627, 0.125), 'rivals violet': (0.541, 0.31, 0.816), 'cell turquoise': (0x60/255, 0xe0/255, 0xea/255)}
def lin(c): c = np.asarray(c, float); return np.where(c <= .04045, c / 12.92, ((c + .055) / 1.055) ** 2.4)
def srgb(c): c = np.asarray(c, float); return np.where(c <= .0031308, c * 12.92, 1.055 * np.clip(c, 0, None) ** (1 / 2.4) - .055)
def lab(c):
    l = lin(c); M = np.array([[.4124, .3576, .1805], [.2126, .7152, .0722], [.0193, .1192, .9505]]); xyz = M @ l / np.array([.95047, 1, 1.08883])
    f = np.where(xyz > .008856, np.cbrt(xyz), 7.787 * xyz + 16 / 116); return np.array([116 * f[1] - 16, 500 * (f[0] - f[1]), 200 * (f[1] - f[2])])
def fog(c, d): f = 1 - np.exp(-(d * K) ** 2); return srgb(lin(c) * (1 - f) + lin(HAZE) * f), f
def Y(c): return float(np.array([.2126, .7152, .0722]) @ lin(c))
DS = (0, 20, 40, 80, 150, 250, 350)
rows = []
print('density', round(K, 5))
print('signal            ' + ''.join(f'{d:>13}m' for d in DS))
for n, c in SIG.items():
    out = []
    for d in DS:
        fc, f = fog(c, d); de = np.linalg.norm(lab(fc) - lab(c)); h0 = colorsys.rgb_to_hsv(*c)[0] * 360; h1 = colorsys.rgb_to_hsv(*fc)[0] * 360
        cr = (max(Y(fc), Y(HAZE)) + .05) / (min(Y(fc), Y(HAZE)) + .05)
        out.append(f'dE{de:5.1f} dH{(h1 - h0):4.0f} c{cr:3.1f}')
    print(f'{n:<16}', '  '.join(out))
# picture
W = 1400; img = Image.new('RGB', (W, 330), (243, 232, 212)); dr = ImageDraw.Draw(img)
ft = ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial Bold.ttf', 20)
stone = np.array([0.90, 0.80, 0.66])
for j, d in enumerate(DS):
    dr.text((190 + j * 170, 8), f'{d} m' + (f'  {1 - np.exp(-(d * K) ** 2):.0%}' if d else ''), fill=(23, 48, 47), font=ft)
    bg, _ = fog(stone, d)
    dr.rectangle([180 + j * 170, 36, 180 + j * 170 + 160, 320], fill=tuple(int(v * 255) for v in bg))
for i, (n, c) in enumerate(SIG.items()):
    dr.text((10, 70 + i * 68), n, fill=(23, 48, 47), font=ft)
    for j, d in enumerate(DS):
        fc, _ = fog(c, d); x = 195 + j * 170; y = 60 + i * 68
        dr.ellipse([x, y, x + 52, y + 52], fill=tuple(int(v * 255) for v in fc))
        dr.rectangle([x + 70, y + 14, x + 140, y + 38], fill=tuple(int(v * 255) for v in fc))
img.save('signals-under-haze.png')
