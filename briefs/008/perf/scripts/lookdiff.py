# B6 look check: per stop and switch, mean |diff| (0-255) and the share of pixels whose largest channel moves > 8/255 against as-was;
# sheets as-was | variant at 960x540 for the stops given. Usage: python3 lookdiff.py stops_dir out_dir
import os, re, sys, json
from PIL import Image, ImageChops, ImageDraw
import numpy as np
src, dst = sys.argv[1], sys.argv[2]; os.makedirs(dst, exist_ok=True)
shots = {}
for f in os.listdir(src):
    m = re.match(r'\d+-(.+?)-(as-was|base|ssao-depth|ssao-off|proxies|cascades-2|fixes-13|fixes-123|fade-off|shadows-off)\.png$', f)
    if m: shots.setdefault(m[1], {})[m[2]] = os.path.join(src, f)
out = {}
for stop, vs in shots.items():
    a = np.asarray(Image.open(vs['as-was']).convert('RGB')).astype(np.int16)
    for v, p in vs.items():
        if v == 'as-was': continue
        b = np.asarray(Image.open(p).convert('RGB')).astype(np.int16)
        d = np.abs(a - b)
        out[f'{stop}|{v}'] = {'meanAbs': round(float(d.mean()), 2), 'changed': round(float((d.max(axis=2) > 8).mean()), 4)}
json.dump(out, open(os.path.join(dst, 'lookdiff.json'), 'w'), indent=1)
for stop in shots:
    print(stop.ljust(14), '  '.join(f"{v}:{out[f'{stop}|{v}']['meanAbs']:.1f}/{100*out[f'{stop}|{v}']['changed']:.1f}%" for v in ('base','ssao-depth','proxies','cascades-2','fixes-13','ssao-off') if f'{stop}|{v}' in out))
for stop in shots:
    a = Image.open(shots[stop]['as-was']).convert('RGB'); b = Image.open(shots[stop]['base']).convert('RGB')
    sheet = Image.new('RGB', (1920, 540 + 24), 'white'); dr = ImageDraw.Draw(sheet)
    sheet.paste(a.resize((960, 540)), (0, 24)); sheet.paste(b.resize((960, 540)), (960, 24))
    dr.text((6, 5), f'{stop}: as-was (before B6)', fill='black'); dr.text((966, 5), f'{stop}: base (B6: SSAO from depth, 2 cascades, LOD1 shadow proxies)', fill='black')
    sheet.save(os.path.join(dst, f'{stop}-as-was-vs-base.jpg'), quality=90)
