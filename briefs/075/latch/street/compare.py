"""Brief 075 golden s2-street (Latch): the street round's pictures. Usage: python3 -I briefs/075/latch/street/compare.py <repo root>
- flyby/grid-street.png: old reel / 075-F1 billboards (before the street) / s2-street now at 6, 20, 33, 50, 61.5 s (Flyby074 -lens on, the
  briefs/074/flyby/new-keys.json keys; frames in flyby/grid/, now = street-t*.png).
- golden/play/pair-*.png: play zoom 14 at 38 deg, day, the player's look (Glint075 -mode play): 075-F1 billboards before | s2-street now, with the
  mean absolute difference inside the Handover's civic block box (the café square and Office front, unchanged by the street) and over the whole frame."""
import sys, os, shutil, json
import numpy as np
from PIL import Image, ImageDraw, ImageFont
root = sys.argv[1]
B = lambda *p: os.path.join(root, 'briefs', *p)
try:
    font = ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial Bold.ttf', 26); small = ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial Bold.ttf', 20)
except OSError:
    font = small = ImageFont.load_default()
INK, CREAM = (17, 20, 26), (247, 244, 236)
TW, TH, G = 800, 450, 8
stills = os.path.join(root, 'out', 'scratch', 'latch', '075street', 'stills')
for t, f in (('6', 't06.0.png'), ('20', 't20.0.png'), ('33', 't33.0.png'), ('50', 't50.0.png'), ('61.5', 't61.5.png')):
    shutil.copyfile(os.path.join(stills, f), B('075', 'flyby', 'grid', f'street-t{t}.png'))
rows = [('6', 'boulevard (6 s)'), ('20', 'square (20 s)'), ('33', 'parade (33 s)'), ('50', 'skyport tower (50 s)'), ('61.5', 'skyline across the water (61.5 s)')]
cols = [('old', 'OLD reel'), ('billboards', '075-F1 billboards (before)'), ('street', 's2-street now (tram street)')]
head, lab = 52, 34
g = Image.new('RGB', (3 * TW + 4 * G, head + len(rows) * (lab + TH + G)), INK); d = ImageDraw.Draw(g)
for j, (_, t) in enumerate(cols): d.text((G + j * (TW + G) + 6, 14), t, fill=CREAM, font=font)
for i, (t, name) in enumerate(rows):
    y = head + i * (lab + TH + G); d.text((G + 6, y + 6), name, fill=(176, 205, 199), font=small)
    for j, (k, _) in enumerate(cols):
        g.paste(Image.open(B('075', 'flyby', 'grid', f'{k}-t{t}.png')).convert('RGB').resize((TW, TH), Image.LANCZOS), (G + j * (TW + G), y + lab))
g.save(B('075', 'flyby', 'grid-street.png')); print(B('075', 'flyby', 'grid-street.png'))

out = {}
for v in ('office-front-y030', 'office-front-y210', 'cafe-square-y030', 'cafe-square-y210'):
    a, b = B('075', 'shots', 'billboards', 'player', f'today-day-{v}.png'), B('075', 'golden', 'play', f'today-day-{v}.png')
    A, Bb = (np.asarray(Image.open(p).convert('RGB'), dtype=np.float32) for p in (a, b))
    h, w = A.shape[:2]; c = (slice(h // 4, 3 * h // 4), slice(w // 4, 3 * w // 4))  # the middle half: the block the view centres on
    diff = np.abs(A - Bb).mean(-1)
    out[v] = {'mad_frame': round(float(diff.mean()), 2), 'mad_centre': round(float(diff[c].mean()), 2), 'changed_px_pct': round(float((diff > 12).mean() * 100), 2)}
    L, R = (Image.open(p).convert('RGB').resize((960, 540), Image.LANCZOS) for p in (a, b))
    cv = Image.new('RGB', (1928, 540), INK); cv.paste(L, (0, 0)); cv.paste(R, (968, 0)); dd = ImageDraw.Draw(cv)
    for x, t in ((12, '075-F1 billboards (before)'), (980, 's2-street now')):
        dd.rectangle((x - 6, 8, x + dd.textlength(t, font=font) + 6, 44), fill=INK); dd.text((x, 12), t, fill=CREAM, font=font)
    t = f'{v} · mean diff {out[v]["mad_frame"]} (centre {out[v]["mad_centre"]})'
    dd.rectangle((6, 500, 12 + dd.textlength(t, font=font), 536), fill=INK); dd.text((12, 504), t, fill=CREAM, font=font)
    cv.save(B('075', 'golden', 'play', f'pair-{v}.png')); print(B('075', 'golden', 'play', f'pair-{v}.png'))
json.dump(out, open(B('075', 'golden', 'play', 'regression.json'), 'w'), indent=1); print(json.dumps(out, indent=1))
