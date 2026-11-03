"""Brief 075 item 6 (Latch): 100% crops (no scaling) of the sky and a wall, the s6 reel as first encoded (CRF 18, tune none, BT.601
untagged) against the re-encode (CRF 14, tune grain, aq-mode 3, BT.709), plus the review master, from the same frame. Each crop is also
written 3x nearest-neighbour so the blocks are visible. Usage: python3 -I crops.py <before.mp4> <after.mp4> <master.mp4> <out dir> [frame]
Writes <out>/{sky,wall}-{before,after,master}.png, *-x3.png, pair-{sky,wall}-x3.png and crops.json (unique colours, std, bytes)."""
import sys, os, subprocess, json
import numpy as np
from PIL import Image, ImageDraw, ImageFont
before, after, master, out = sys.argv[1:5]; n = int(sys.argv[5]) if len(sys.argv) > 5 else 180
os.makedirs(out, exist_ok=True)
def frame(mp4):
    p = subprocess.run(['/opt/homebrew/bin/ffmpeg', '-v', 'error', '-i', mp4, '-vf', f'select=eq(n\\,{n})', '-frames:v', '1', '-f', 'rawvideo', '-pix_fmt', 'rgb24', '-'], capture_output=True, check=True).stdout
    return np.frombuffer(p, np.uint8).reshape(1080, 1920, 3)
CROPS = {'sky': (1200, 20, 320, 180), 'wall': (1250, 380, 320, 180)}
font = ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial Bold.ttf', 22)
rep = {'frame': n, 'files': {k: os.path.getsize(v) for k, v in (('before', before), ('after', after), ('master', master)) if os.path.exists(v)}, 'crops': {}}
F = {k: frame(v) for k, v in (('before', before), ('after', after), ('master', master)) if os.path.exists(v)}
for c, (x, y, w, h) in CROPS.items():
    tiles = []
    for k, a in F.items():
        t = a[y:y + h, x:x + w]; Image.fromarray(t).save(os.path.join(out, f'{c}-{k}.png'))
        big = Image.fromarray(t).resize((w * 3, h * 3), Image.NEAREST); big.save(os.path.join(out, f'{c}-{k}-x3.png')); tiles.append((k, big))
        rep['crops'][f'{c}-{k}'] = {'unique_colours': int(len(np.unique(t.reshape(-1, 3), axis=0))), 'std_rgb': [round(float(s), 2) for s in t.reshape(-1, 3).std(0)]}
    cv = Image.new('RGB', (len(tiles) * (w * 3 + 8), h * 3 + 40), (17, 20, 26)); d = ImageDraw.Draw(cv)
    for i, (k, big) in enumerate(tiles):
        cv.paste(big, (i * (w * 3 + 8), 40)); d.text((i * (w * 3 + 8) + 6, 8), {'before': 'before: CRF 18, 601 untagged', 'after': 'after: CRF 14 grain, 709', 'master': 'master: 4:4:4 CRF 8'}[k] + ' (100%, shown x3)', fill=(247, 244, 236), font=font)
    cv.save(os.path.join(out, f'pair-{c}-x3.png'))
json.dump(rep, open(os.path.join(out, 'crops.json'), 'w'), indent=1); print(json.dumps(rep, indent=1))
# the sky crops again at contrast x6 about their mean (banding shows as contour steps), and each encode's error against the master
if 'master' in F:
    x, y, w, h = CROPS['sky']; m = F['master'][y:y + h, x:x + w].astype(np.float32); mu = m.mean((0, 1))
    tiles = [(k, Image.fromarray(np.clip((F[k][y:y + h, x:x + w].astype(np.float32) - mu) * 6 + 128, 0, 255).astype(np.uint8)).resize((w * 3, h * 3), Image.NEAREST)) for k in F]
    cv = Image.new('RGB', (len(tiles) * (w * 3 + 8), h * 3 + 40), (17, 20, 26)); d = ImageDraw.Draw(cv)
    for i, (k, big) in enumerate(tiles): cv.paste(big, (i * (w * 3 + 8), 40)); d.text((i * (w * 3 + 8) + 6, 8), f'{k}: sky, contrast x6 (100%, shown x3)', fill=(247, 244, 236), font=font)
    cv.save(os.path.join(out, 'pair-sky-contrast6-x3.png'))
    rep['vs_master'] = {}
    for k in F:
        if k == 'master': continue
        e = {}
        for c, (x, y, w, h) in list(CROPS.items()) + [('frame', (0, 0, 1920, 1080))]:
            a, b = F[k][y:y + h, x:x + w].astype(np.float64), F['master'][y:y + h, x:x + w].astype(np.float64); mse = ((a - b) ** 2).mean()
            e[c] = {'mad': round(float(np.abs(a - b).mean()), 3), 'psnr_db': round(float(10 * np.log10(255 ** 2 / max(mse, 1e-9))), 2)}
        rep['vs_master'][k] = e
    json.dump(rep, open(os.path.join(out, 'crops.json'), 'w'), indent=1); print(json.dumps(rep['vs_master'], indent=1))
