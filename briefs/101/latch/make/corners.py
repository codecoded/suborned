"""Brief 101, Sky's mix (2026-11-08: "street labels top right but tokens bottom right"): how much of the screen the two corners take, today, in this build and in
Glyph's two round 2 mocks, measured the way tools/hud-budget reads a capture (the HUD layer alone, alpha >= 24, at 1920 x 1080: keylines and shadows count).
  top    the clock group: today the status pill, the people plate and the seal card (top centre); now the street-label stack (time, seal, lamps, Trail, money,
         the next key event, the faces); in the mocks the top right.
  bottom the news and the Book: today the crucial card and the Book key; now the lozenge, its roundel and tag and the Book roundel; in the mocks the bottom right.
  frame  the whole HUD layer (what tools/hud-budget calls coverage): it still carries the pieces this round does not restyle (the roster, the scope, the street).
A capture's pieces come from its parts.json (each rect 6 px wider all round, for keylines and shadows); the mocks have no parts, so they are read in fixed corner boxes.
Edge cameos inside a box are left out (they are the street layer's, not the corner's). Latch. Usage: python3 -I corners.py <shots dir> [out.json]"""
import json, os, sys
import numpy as np
from PIL import Image

R = '/Users/sky/Projects/suborned/'
W, H, ALPHA, PAD = 1920, 1080, 24, 6
SEAL_CARD = (744, 56, 1180, 180)  # today's 420 x 104 seal card, top centre (HudToolkit.Seal before brief 101), with its pop's room
MOCK_TOP, MOCK_BOTTOM = (1480, 0, 1920, 260), (1500, 950, 1920, 1080)
TODAY = {  # build 0.2.263 / 0.2.268 captures of the same moments (brief 100's shots; the batch's; Glyph's sources)
    'rest': (R + 'out/batch/shots/office-front-yaw210', 'no card'),  # 2:00 on route A at the Office front (yaw 210): the pill and the people plate; the Book key alone (its card left out, as Glyph's today-rest)
    'quiet': (R + 'briefs/100/latch/shots/why-quiet-radio', ''),
    'loud': (R + 'briefs/100/latch/shots/why-loud-police', ''),
    'busy': (R + 'out/scratch/glyph/101r2/src/fight-loud-body', ''),
}
NOW = {'rest': 't101-rest-b', 'quiet': 't101-quiet', 'loud': 't101-loud', 'busy': 't101-busy'}
MOCK = R + 'out/scratch/glyph/101r2/budget/'


def alpha(path):
    return np.array(Image.open(path).convert('RGBA'))[:, :, 3] >= ALPHA


def box_mask(boxes, cut=()):
    m = np.zeros((H, W), bool)
    for x0, y0, x1, y1 in boxes: m[max(0, y0):min(H, y1), max(0, x0):min(W, x1)] = True
    for x0, y0, x1, y1 in cut: m[max(0, y0):min(H, y1), max(0, x0):min(W, x1)] = False
    return m


def rects(parts, names):
    out = []
    for n in names:
        if n in parts: x, y, w, h = parts[n]; out.append((x - PAD, y - PAD, x + w + PAD, y + h + PAD))
    return out


def pct(a, m=None): return round(100.0 * float((a & m).sum() if m is not None else a.sum()) / (W * H), 2)


def capture(base, note=''):
    a = alpha(base + '.hud.png'); parts = json.load(open(base + '.parts.json'))
    edges = rects(parts, [k for k in parts if k.startswith('edge')])
    top = rects(parts, ['pill', 'clock', 'keys', 'seal'])
    if (a & box_mask([SEAL_CARD])).sum() > 2000 and 'clock' not in parts: top.append(SEAL_CARD)  # today's seal card is not in its parts
    bottom = rects(parts, ['book-key'] + ([] if note == 'no card' else ['crucial']))
    return {'frame': pct(a), 'top': pct(a, box_mask(top, edges)), 'bottom': pct(a, box_mask(bottom, edges))}


def mock(name):
    a = alpha(MOCK + name + '.hud.png')
    return {'frame': pct(a), 'top': pct(a, box_mask([MOCK_TOP])), 'bottom': pct(a, box_mask([MOCK_BOTTOM]))}


def main(shots, out=None):
    rows = {}
    for k in ['rest', 'quiet', 'loud', 'busy']:
        t = capture(*TODAY[k]); n = capture(os.path.join(shots, NOW[k])); s = mock('S-' + k); tk = mock('T-' + k)
        rows[k] = {'today': t, 'now': n, 'street': s, 'tokens': tk, 'mix': {'top': s['top'], 'bottom': tk['bottom']}}
    doc = {'method': __doc__.split('\n')[1].strip(), 'moments': NOW, 'today': {k: v[0].replace(R, '') for k, v in TODAY.items()}, 'rows': rows}
    print(f"{'':6} {'corners (top + bottom), % of the screen':>44}   {'whole frame':>40}")
    print(f"{'':6} {'today':>9} {'now':>9} {'S':>8} {'T':>8} {'mix':>8}   {'today':>9} {'now':>9} {'S':>8} {'T':>8}")
    for k, r in rows.items():
        c = lambda d: d['top'] + d['bottom']
        print(f"{k:6} {c(r['today']):9.2f} {c(r['now']):9.2f} {c(r['street']):8.2f} {c(r['tokens']):8.2f} {r['mix']['top'] + r['mix']['bottom']:8.2f}   "
              f"{r['today']['frame']:9.2f} {r['now']['frame']:9.2f} {r['street']['frame']:8.2f} {r['tokens']['frame']:8.2f}")
        print(f"{'':6} top {r['today']['top']:.2f} / {r['now']['top']:.2f} / S {r['street']['top']:.2f} / T {r['tokens']['top']:.2f}   bottom {r['today']['bottom']:.2f} / {r['now']['bottom']:.2f} / S {r['street']['bottom']:.2f} / T {r['tokens']['bottom']:.2f}")
    if out: json.dump(doc, open(out, 'w'), indent=1)


if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else None)
