"""Cut the state sheets into production portraits (brief 033): python3 -I tools/figures/portraits_export.py <agent>=<sheet.png> ...
Finds each medallion's ring (Hough), re-centres it, clips to the ring (transparent outside) and writes briefs/033/portraits/final/<agent>_<state>.png (64 px, the roster portrait)
and <agent>_<state>@2x.png (128 px); a 512 px master goes to final/master/. Deterministic; no model call."""
import sys
from pathlib import Path
import cv2, numpy as np
from PIL import Image
OUT = Path(__file__).resolve().parents[2] / 'briefs/033/portraits'
STATES = ['calm', 'focused', 'noticed', 'under_fire', 'firing', 'hurt', 'badly_hurt', 'downed', 'success']
def circle(cell):
    g = cv2.GaussianBlur(cv2.cvtColor(cell, cv2.COLOR_RGB2GRAY), (5, 5), 0); h = g.shape[0]
    c = cv2.HoughCircles(g, cv2.HOUGH_GRADIENT, dp=1.5, minDist=h, param1=100, param2=40, minRadius=int(h * .36), maxRadius=int(h * .52))
    if c is None: raise SystemExit('no ring found')
    return c[0][0]
def run(agent, sheet):
    im = np.array(Image.open(sheet).convert('RGB')); n = im.shape[0] // 3; rows = []
    (OUT / 'final/master').mkdir(parents=True, exist_ok=True)
    for i, st in enumerate(STATES):
        r, c = divmod(i, 3); cell = im[r * n:(r + 1) * n, c * n:(c + 1) * n]
        x, y, rad = circle(cell); R = rad + 1.5   # keep the drawn ring line
        S = 4; M = 512 * S // 2
        pad = int(R) + 4; big = cv2.copyMakeBorder(cell, pad, pad, pad, pad, cv2.BORDER_REPLICATE)
        crop = big[int(round(y)) + pad - int(R) - 2: int(round(y)) + pad + int(R) + 2, int(round(x)) + pad - int(R) - 2: int(round(x)) + pad + int(R) + 2]
        d = Image.fromarray(crop).resize((2 * M // 1, 2 * M // 1), Image.LANCZOS)
        mask = Image.new('L', d.size, 0); sc = d.size[0] / (2 * (int(R) + 2)); cc = d.size[0] / 2
        from PIL import ImageDraw
        ImageDraw.Draw(mask).ellipse([cc - R * sc, cc - R * sc, cc + R * sc, cc + R * sc], fill=255)
        d = d.convert('RGBA'); d.putalpha(mask)
        d = d.crop((int(cc - R * sc), int(cc - R * sc), int(cc + R * sc) + 1, int(cc + R * sc) + 1))
        m = d.resize((512, 512), Image.LANCZOS); m.save(OUT / f'final/master/{agent}_{st}.png')
        m.resize((128, 128), Image.LANCZOS).save(OUT / f'final/{agent}_{st}@2x.png'); m.resize((64, 64), Image.LANCZOS).save(OUT / f'final/{agent}_{st}.png')
        rows.append(st)
    print(agent, 'ok', len(rows))
if __name__ == '__main__':
    for a in sys.argv[1:]:
        k, v = a.split('='); run(k, v)
