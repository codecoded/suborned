# Glint, 008 light-honest: before/after sheet, ΔE table, signals census, evidence folder.
import sys, json, shutil, base64, io, os, numpy as np
sys.path.insert(0, '/Users/sky/Projects/suborned/out/glint-honest')
from PIL import Image, ImageDraw, ImageFont
import deltae as D
ROOT = '/Users/sky/Projects/suborned/out/glint-honest'; OUT = '/Users/sky/Projects/suborned/briefs/008/light-honest'
os.makedirs(OUT + '/frames', exist_ok=True)
F = lambda s, b=False: ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial Bold.ttf' if b else '/System/Library/Fonts/Supplemental/Arial.ttf', s)
P = lambda n: Image.open(f'{ROOT}/paint/the-suborned-{n}-2026-09-26.png').convert('RGB')
# ---- rows: (title, frame key, painting, crop box in painting (x0,y0,x1,y1), note)
rows = [
 ('Home frame (play, 14 at 38 deg)', 'play', 'gameplay-plan-retrofuture-v3', (0, 120, 1536, 984), 'painting: gameplay-plan v3 (the play-angle painting)'),
 ('Civic block in sun and shade (009-kerb)', '009-kerb', 'gameplay-plan-retrofuture-v3', (0, 380, 1100, 999), 'painting: gameplay-plan v3, tram road in shade, plaza in sun (crop)'),
 ('Cafe crossing (009-cafe)', '009-cafe', 'inez-cutaway-retrofuture-v3', (0, 80, 1536, 944), 'painting: inez cutaway v3, cafe bay and paving in sun'),
 ('Aerial of the civic block (perspective, 28 deg fov, pitch 40)', 'aerA', 'mission-block-retrofuture-v2', (0, 60, 1536, 924), 'painting: mission block v2 (aerial)'),
 ('Extra: skyline-sw (haze and sky untouched)', 'skyline-sw', 'city-hero-retrofuture-v2', None, 'painting: city hero v2'),
]
CW, CH = 800, 450; PAD = 10; HEAD = 34; CAP = 22
W = 3 * CW + 4 * PAD; H = len(rows) * (CH + HEAD + CAP + PAD) + PAD + 60
sheet = Image.new('RGB', (W, H), (22, 24, 28)); d = ImageDraw.Draw(sheet)
d.text((PAD, 10), 'Glint, 008 light-honest: paintings (26 Sep) | before (honest frame, light.json as of 8235449) | after (retuned light.json)', fill=(235, 235, 235), font=F(20, True))
d.text((PAD, 36), 'All frames: Capture (ARGBHalf, the player HDR chain), people visible, GPU Resident Drawer off. Measured numbers are in report.html.', fill=(170, 175, 180), font=F(14))
y = 62
for title, key, paint, box, note in rows:
    d.text((PAD, y + 6), title, fill=(255, 255, 255), font=F(18, True)); y += HEAD
    pim = P(paint)
    if box: pim = pim.crop(box)
    else: # hero painting is 1983x793: crop to 16:9 centred
        w, h = pim.size; nh = int(w * 9 / 16); pim = pim.crop((0, 0, w, min(h, nh))) if nh > h else pim.crop((0, 0, w, nh))
    cells = [pim, Image.open(f'{ROOT}/frames/before-{key}.png').convert('RGB'), Image.open(f'{ROOT}/frames/after-{key}.png').convert('RGB')]
    caps = [note, 'before', 'after']
    for i, (im, cap) in enumerate(zip(cells, caps)):
        im = im.resize((CW, int(CW * im.height / im.width)), Image.LANCZOS)
        if im.height > CH: im = im.crop((0, 0, CW, CH))
        x = PAD + i * (CW + PAD); sheet.paste(im, (x, y)); d.text((x, y + CH + 3), cap, fill=(200, 205, 210), font=F(13))
    y += CH + CAP + PAD
sheet.save(f'{OUT}/sheet.jpg', quality=90)
print('sheet', sheet.size)
