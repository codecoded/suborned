"""Brief 069 end-to-end: compose the static model check (models.png, models@2x_<id>.png, shoulders.png) from models.py's renders. System python (PIL).
  python3 tools/figures/charpipe/fit/models_sheet.py <e2e dir>"""
import sys, os
from PIL import Image, ImageDraw, ImageFont
D = sys.argv[1]; M = os.path.join(D, 'models'); F = ImageFont.truetype('/System/Library/Fonts/Helvetica.ttc', 22)
ids = ['e2e_coat', 'e2e_dress', 'e2e_guard', 'e2e_flamingo']; BG = (240, 240, 236)


def fit_h(im, h): return im.resize((int(im.width * h / im.height), h))


rows = []
for i in ids:
    ims = [fit_h(Image.open(os.path.join(M, f'{i}_{v}.png')).convert('RGB'), 600) for v in ('front', 'back', 'close_raw', 'close_fit')]
    r = Image.new('RGB', (sum(im.width for im in ims) + 30, 650), BG); x = 0
    for k, im in enumerate(ims): r.paste(im, (x, 44)); x += im.width + (10 if k in (1, 2) else 0)
    ImageDraw.Draw(r).text((10, 10), f'{i}  ·  front and back: raw generated mesh left, fitted figure right (A-pose rest, LOD0)  ·  3/4 close: raw, then fitted', fill=(20, 30, 30), font=F)
    rows.append(r)
W = max(r.width for r in rows); out = Image.new('RGB', (W, sum(r.height for r in rows)), BG); y = 0
for r in rows: out.paste(r, (0, y)); y += r.height
out.save(os.path.join(D, 'models.png'))
for i in ids:
    a = Image.open(os.path.join(M, f'{i}_close@2x_raw.png')).convert('RGB'); b = Image.open(os.path.join(M, f'{i}_close@2x_fit.png')).convert('RGB')
    t = Image.new('RGB', (a.width + b.width + 20, a.height + 50), BG); t.paste(a, (0, 50)); t.paste(b, (a.width + 20, 50))
    ImageDraw.Draw(t).text((10, 12), f'{i} at 2x: raw generated mesh (left), fitted figure (right)', fill=(20, 30, 30), font=F); t.save(os.path.join(D, f'models@2x_{i}.png'))
rows = []
for i in ids:
    ims = [Image.open(os.path.join(M, f'{i}_sh_{v}.png')).convert('RGB') for v in ('rest', 'armsdown', 'armsup')]
    r = Image.new('RGB', (sum(im.width for im in ims), ims[0].height + 44), BG); x = 0
    for im in ims: r.paste(im, (x, 44)); x += im.width
    ImageDraw.Draw(r).text((10, 10), f'{i}: fitted figure at rest (the A-pose it was generated in) · arms 40 deg down · arms up to horizontal', fill=(20, 30, 30), font=F); rows.append(r)
out = Image.new('RGB', (rows[0].width, sum(r.height for r in rows)), BG); y = 0
for r in rows: out.paste(r, (0, y)); y += r.height
out.save(os.path.join(D, 'shoulders.png')); print('ok')
