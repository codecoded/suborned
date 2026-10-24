"""contrast.py bare.png rects.json: per text item, WCAG contrast of its effective colour over the busiest part (p95 brightest pixel) and the median of the background under it."""
import sys, json, numpy as np
from PIL import Image
img = np.asarray(Image.open(sys.argv[1]).convert('RGB')).astype(np.float64) / 255
rects = json.load(open(sys.argv[2]))
def lin(c): return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
def L(rgb): r = lin(rgb); return 0.2126 * r[..., 0] + 0.7152 * r[..., 1] + 0.0722 * r[..., 2]
rows = []
for r in rects:
    x0, y0, x1, y1 = int(r['x']), int(r['y'] + r['h'] * 0.2), int(r['x'] + r['w']), int(r['y'] + r['h'] * 0.8)
    if x1 <= x0 or y1 <= y0 or r['w'] < 4: continue
    reg = img[y0:y1, x0:x1].reshape(-1, 3)
    if len(reg) == 0: continue
    lb = L(reg); order = np.argsort(lb); bgs = {'p50': reg[order[int(len(order) * .5)]], 'p95': reg[order[int(len(order) * .95)]]}
    fg = np.array(r['rgb']) / 255; a = r['a']
    out = {}
    for n, b in bgs.items():
        eff = fg * a + b * (1 - a); lf, lbk = L(eff), L(b)
        out[n] = (max(lf, lbk) + .05) / (min(lf, lbk) + .05)
    rows.append((r['text'], r['size'], a, out['p50'], out['p95']))
print(f"{'text':34s} {'px':>5s} {'alpha':>5s} {'cr p50':>7s} {'cr p95':>7s}")
for t, s, a, c50, c95 in rows: print(f"{t[:34]:34s} {s:5.1f} {a:5.2f} {c50:7.2f} {c95:7.2f}")
print('min p95', min(r[4] for r in rows if r[2] > .5) if rows else None)
