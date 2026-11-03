"""Brief 075 remap (Latch): the remap round's pictures. Usage: python3 -I briefs/075/latch/remap/compare.py <repo root> <stills dir>
- flyby/grid-remap.png: old reel / s4-oldtown / now (the old town remapped) at 6, 20, 33, 50, 61.5 s. now = Flyby074 -lens on -life on -lifeT0 71
  through briefs/075/flyby/remap-keys.json (the old reel's own flight, plan y = old z - 206); s4 flew briefs/074/flyby/new-keys.json. Frames in flyby/grid/, now = remap-t*.png.
- remap/frames/pair-*.png + remap/frames/regression.json: the Handover café square and Office front at play zoom 14, pitch 38, yaw 30 and 210, day
  (Glint075 -mode play, no life): golden/play (s3-life) before | now, mean absolute difference over the frame and its middle half, % of pixels off by more than 12."""
import sys, os, shutil, json
import numpy as np
from PIL import Image, ImageDraw, ImageFont
root, stills = sys.argv[1], sys.argv[2]
B = lambda *p: os.path.join(root, 'briefs', *p)
font = ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial Bold.ttf', 26); small = ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial Bold.ttf', 20)
INK, CREAM = (17, 20, 26), (247, 244, 236)
TW, TH, G = 800, 450, 8
for t, f in (('6', 't06.0.png'), ('20', 't20.0.png'), ('33', 't33.0.png'), ('50', 't50.0.png'), ('61.5', 't61.5.png')):
    shutil.copyfile(os.path.join(stills, f), B('075', 'flyby', 'grid', f'remap-t{t}.png'))
rows = [('6', 'boulevard (6 s)'), ('20', 'circus (20 s)'), ('33', 'parade (33 s)'), ('50', 'skyport tower (50 s)'), ('61.5', 'skyline across the water (61.5 s)')]
cols = [('old', 'OLD reel'), ('oldtown', 's4-oldtown (074 flight)'), ('remap', 'now: the old town remapped (old flight)')]
head, lab = 52, 34
g = Image.new('RGB', (3 * TW + 4 * G, head + len(rows) * (lab + TH + G)), INK); d = ImageDraw.Draw(g)
for j, (_, t) in enumerate(cols): d.text((G + j * (TW + G) + 6, 14), t, fill=CREAM, font=font)
for i, (t, name) in enumerate(rows):
    y = head + i * (lab + TH + G); d.text((G + 6, y + 6), name, fill=(176, 205, 199), font=small)
    for j, (k, _) in enumerate(cols):
        g.paste(Image.open(B('075', 'flyby', 'grid', f'{k}-t{t}.png')).convert('RGB').resize((TW, TH), Image.LANCZOS), (G + j * (TW + G), y + lab))
g.save(B('075', 'flyby', 'grid-remap.png')); print(B('075', 'flyby', 'grid-remap.png'))

out = {}
for v in ('office-front-y030', 'office-front-y210', 'cafe-square-y030', 'cafe-square-y210'):
    a, b = B('075', 'golden', 'play', f'today-day-{v}.png'), B('075', 'remap', 'frames', f'today-day-{v}.png')
    A, Bb = (np.asarray(Image.open(p).convert('RGB'), dtype=np.float32) for p in (a, b))
    h, w = A.shape[:2]; c = (slice(h // 4, 3 * h // 4), slice(w // 4, 3 * w // 4))
    diff = np.abs(A - Bb).mean(-1)
    out[v] = {'mad_frame': round(float(diff.mean()), 2), 'mad_centre': round(float(diff[c].mean()), 2), 'changed_px_pct': round(float((diff > 12).mean() * 100), 2)}
    L, R = (Image.open(p).convert('RGB').resize((960, 540), Image.LANCZOS) for p in (a, b))
    cv = Image.new('RGB', (1928, 540), INK); cv.paste(L, (0, 0)); cv.paste(R, (968, 0)); dd = ImageDraw.Draw(cv)
    for x, t in ((12, 's3-life (before)'), (980, 'now: remap')):
        dd.rectangle((x - 6, 8, x + dd.textlength(t, font=font) + 6, 44), fill=INK); dd.text((x, 12), t, fill=CREAM, font=font)
    t = f'{v} · mean diff {out[v]["mad_frame"]} (centre {out[v]["mad_centre"]})'
    dd.rectangle((6, 500, 12 + dd.textlength(t, font=font), 536), fill=INK); dd.text((12, 504), t, fill=CREAM, font=font)
    cv.save(B('075', 'remap', 'frames', f'pair-{v}.png'))
json.dump(out, open(B('075', 'remap', 'frames', 'regression.json'), 'w'), indent=1); print(json.dumps(out, indent=1))
