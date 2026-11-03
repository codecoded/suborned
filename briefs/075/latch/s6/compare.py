"""Brief 075 s6 (Latch): the remap's roofs, ground, props and S2 round. Usage: python3 -I briefs/075/latch/s6/compare.py <repo root> <stills dir> <play dir>
- flyby/grid-s6.png: old reel | s5-remap | now (s6) at 6, 20, 33, 50, 61.5 s, the grid-remap.png layout. now = Flyby074 -lens on -life on -lifeT0 71 through
  briefs/075/flyby/remap-keys.json (the old reel's flight). Frames in flyby/grid/s6-t*.png.
- remap/s6/play-*.png: the play-zoom views (Glint075 -mode play, day): circus wide, Orbit Gardens, harbour terrace, a rooftop, a district corner, the
  Skyport, and the Handover café square and Office front at yaw 30 and 210.
- remap/s6/pair-*.png + regression: each Handover view against golden/play (s3-life, the c043 state's zone) and against remap/frames (s5): mean absolute
  difference over the frame and its middle half, % of pixels off by more than 12.
- banding: the sky crop (rows above the skyline) of every flyby still and the golden frame: mean run of identical 8-bit pixels along a row (a banded
  gradient holds one value for tens of pixels; a dithered one changes every pixel or two), and the share of rows whose luminance only ever steps by 0 or 1.
Writes remap/s6/metrics.json."""
import sys, os, shutil, json
import numpy as np
from PIL import Image, ImageDraw, ImageFont
root, stills, play = sys.argv[1], sys.argv[2], sys.argv[3]
B = lambda *p: os.path.join(root, 'briefs', *p)
S6 = B('075', 'remap', 's6'); os.makedirs(S6, exist_ok=True)
font = ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial Bold.ttf', 26); small = ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial Bold.ttf', 20)
INK, CREAM = (17, 20, 26), (247, 244, 236)
TW, TH, G = 800, 450, 8
T = (('6', 't06.0.png'), ('20', 't20.0.png'), ('33', 't33.0.png'), ('50', 't50.0.png'), ('61.5', 't61.5.png'))
for t, f in T: shutil.copyfile(os.path.join(stills, f), B('075', 'flyby', 'grid', f's6-t{t}.png'))
rows = [('6', 'boulevard (6 s)'), ('20', 'circus (20 s)'), ('33', 'parade (33 s)'), ('50', 'skyport tower (50 s)'), ('61.5', 'skyline across the water (61.5 s)')]
cols = [('old', 'OLD reel'), ('remap', 's5-remap (before)'), ('s6', 'now: s6 (roofs, ground, props, S2)')]
head, lab = 52, 34
g = Image.new('RGB', (3 * TW + 4 * G, head + len(rows) * (lab + TH + G)), INK); d = ImageDraw.Draw(g)
for j, (_, t) in enumerate(cols): d.text((G + j * (TW + G) + 6, 14), t, fill=CREAM, font=font)
for i, (t, name) in enumerate(rows):
    y = head + i * (lab + TH + G); d.text((G + 6, y + 6), name, fill=(176, 205, 199), font=small)
    for j, (k, _) in enumerate(cols):
        g.paste(Image.open(B('075', 'flyby', 'grid', f'{k}-t{t}.png')).convert('RGB').resize((TW, TH), Image.LANCZOS), (G + j * (TW + G), y + lab))
g.save(B('075', 'flyby', 'grid-s6.png')); print(B('075', 'flyby', 'grid-s6.png'))

out = {'regression': {}, 'banding': {}}
for f in sorted(os.listdir(play)):
    if f.endswith('.png'): shutil.copyfile(os.path.join(play, f), os.path.join(S6, 'play-' + f.replace('today-day-', '')))
def mad(a, b):
    A, Bb = (np.asarray(Image.open(p).convert('RGB'), dtype=np.float32) for p in (a, b))
    h, w = A.shape[:2]; c = (slice(h // 4, 3 * h // 4), slice(w // 4, 3 * w // 4)); diff = np.abs(A - Bb).mean(-1)
    return {'mad_frame': round(float(diff.mean()), 2), 'mad_centre': round(float(diff[c].mean()), 2), 'changed_px_pct': round(float((diff > 12).mean() * 100), 2)}
for v in ('office-front-y030', 'office-front-y210', 'cafe-square-y030', 'cafe-square-y210'):
    now = os.path.join(play, f'today-day-{v}.png')
    base, s5 = B('075', 'golden', 'play', f'today-day-{v}.png'), B('075', 'remap', 'frames', f'today-day-{v}.png')
    out['regression'][v] = {'vs_s3_life_c043': mad(base, now), 'vs_s5_remap': mad(s5, now)}
    L, R = (Image.open(p).convert('RGB').resize((960, 540), Image.LANCZOS) for p in (base, now))
    cv = Image.new('RGB', (1928, 540), INK); cv.paste(L, (0, 0)); cv.paste(R, (968, 0)); dd = ImageDraw.Draw(cv)
    for x, t in ((12, 's3-life (c043 zone, before)'), (980, 'now: s6')):
        dd.rectangle((x - 6, 8, x + dd.textlength(t, font=font) + 6, 44), fill=INK); dd.text((x, 12), t, fill=CREAM, font=font)
    r = out['regression'][v]['vs_s3_life_c043']; t = f'{v} · mean diff {r["mad_frame"]} (centre {r["mad_centre"]})'
    dd.rectangle((6, 500, 12 + dd.textlength(t, font=font), 536), fill=INK); dd.text((12, 504), t, fill=CREAM, font=font)
    cv.save(os.path.join(S6, f'pair-{v}.png'))

def band(p, sky_rows):
    a = np.asarray(Image.open(p).convert('RGB')).astype(np.int16)
    s = a[:sky_rows]
    same = np.all(s[:, 1:] == s[:, :-1], axis=-1)  # horizontal neighbours identical
    run = 1.0 / max(1e-6, 1.0 - same.mean())       # mean run length of identical pixels
    lum = (0.2126 * s[..., 0] + 0.7152 * s[..., 1] + 0.0722 * s[..., 2])
    vs = np.abs(np.diff(np.round(lum).astype(np.int16), axis=0))  # vertical steps down the gradient
    return {'sky_rows': sky_rows, 'mean_run_px': round(float(run), 2), 'identical_neighbours_pct': round(float(same.mean() * 100), 1), 'vertical_steps_le1_pct': round(float((vs <= 1).mean() * 100), 1)}
for t, f in T:
    out['banding'][f'flyby-t{t}'] = band(os.path.join(stills, f), 160)
    out['banding'][f'flyby-t{t}-s5'] = band(B('075', 'flyby', 'grid', f'remap-t{t}.png'), 160)
gold = B('075', 'golden', 's6-oldtown.png')
if os.path.exists(gold):
    out['banding']['golden-s6'] = band(gold, 120); out['banding']['golden-s5'] = band(B('075', 'golden', 's5-remap.png'), 120)
    # a 4x contrast crop of the sky, s5 | s6 (banding shows as contour lines)
    def crop(p):
        a = np.asarray(Image.open(p).convert('RGB')).astype(np.float32)[:200, 600:1320]; m = a.mean((0, 1)); return Image.fromarray(np.clip((a - m) * 4 + 128, 0, 255).astype(np.uint8))
    cv = Image.new('RGB', (1448, 240), INK); cv.paste(crop(B('075', 'golden', 's5-remap.png')), (0, 40)); cv.paste(crop(gold), (728, 40)); dd = ImageDraw.Draw(cv)
    dd.text((8, 8), 's5-remap sky, contrast x4', fill=CREAM, font=small); dd.text((736, 8), 's6 sky, contrast x4 (dithering on)', fill=CREAM, font=small)
    cv.save(os.path.join(S6, 'sky-contrast.png'))
    steps = json.load(open(B('075', 'golden', 'steps.json'))); steps = steps if isinstance(steps, list) else steps['steps']
    out['golden'] = {s['step']: s.get('stats') for s in steps if s['step'] in ('s5-remap', 's6-oldtown')}
json.dump(out, open(os.path.join(S6, 'metrics.json'), 'w'), indent=1); print(json.dumps(out, indent=1))
