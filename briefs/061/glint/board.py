"""Swatch board of the model: what the grade shows for the crossing states (board colour, Vela's untrimmed day levels, the trims), the guard and cone swatches,
and the silhouettes' fill and rim, by phase. Run from this folder: python3 -I board.py -> board-signals.png"""
import sys; sys.path.insert(0, '.')
from fit import *
from PIL import Image, ImageDraw, ImageFont
R = json.load(open('final.json')) if False else __import__('json').load(open('final.json'))
GR = {'day': PH['day'], 'evening': PH['evening'], 'night': PH['night']}
UNTRIM = {'walk': 2.0, 'clearing': 2.0, 'stop': 1.8}
W, H = 1800, 1120; im = Image.new('RGB', (W, H), '#17302f'); d = ImageDraw.Draw(im)
try: f = ImageFont.truetype('/System/Library/Fonts/Helvetica.ttc', 17); fs = ImageFont.truetype('/System/Library/Fonts/Helvetica.ttc', 14)
except Exception: f = fs = ImageFont.load_default()
def sw(x, y, hexv, label, w=130, h=62, dark=False):
    d.rectangle([x, y, x + w, y + h], fill=hexv, outline='#cbc6c3')
    for i, ln in enumerate(label.split('|')): d.text((x + 2, y + h + 3 + i * 16), ln, fill='#f7f4ec', font=fs)
d.text((20, 12), 'Crossing heads: what the play grade shows at the face centre (model, grade.py). Row 1 board colour, row 2 untrimmed as Vela built it, row 3 trimmed.', fill='#f7f4ec', font=f)
y0 = 50
for si, (k, hx) in enumerate(STATES.items()):
    d.text((20, y0 + si * 270 + 100), k, fill='#f7f4ec', font=f)
    for pi, (ph, g) in enumerate(GR.items()):
        x = 110 + pi * 560; y = y0 + si * 270
        d.text((x, y), ph, fill='#60e0ea', font=f)
        un = g(s2l(hx) * UNTRIM[k]); r = R[k][ph]; tr = g(s2l(r['trim']) * r['level'])
        sw(x, y + 24, hx, 'board|' + hx); sw(x + 175, y + 24, hexof(un), 'as built x%.1f|%s dE %.1f' % (UNTRIM[k], hexof(un), de2000(lab(un), lab(s2l(hx)))))
        sw(x + 350, y + 24, hexof(tr), 'trimmed x%.2f|%s dE %.1f' % (r['level'], hexof(tr), r['dE']))
        # pre-grade emission as it would sit unlit: the trim itself
        sw(x + 350, y + 120, r['trim'], 'pre-grade|' + r['trim'], h=40)
y = y0 + 3 * 270 + 60
d.text((20, y), 'Against the overlay-camera signals (exact, outside the grade): guard #f0a020, cones #e03a2f; heads shown by day', fill='#f7f4ec', font=f)
g = GR['day']; cl = g(s2l(R['clearing']['day']['trim']) * R['clearing']['day']['level']); st = g(s2l(R['stop']['day']['trim']) * R['stop']['day']['level'])
sw(20, y + 28, '#f0a020', 'guard cone|#f0a020'); sw(180, y + 28, hexof(cl), 'clearing head|%s dE %.1f' % (hexof(cl), de2000(lab(cl), lab(s2l('#f0a020')))))
sw(400, y + 28, '#e03a2f', 'cone|#e03a2f'); sw(560, y + 28, hexof(st), 'stop head|%s dE %.1f' % (hexof(st), de2000(lab(st), lab(s2l('#e03a2f')))))
fills = {'day': '#12544c', 'evening': '#3d514e', 'night': '#595e5c'}; edges = {'day': '#fffff2', 'evening': '#fffffd', 'night': '#ffffef'}
d.text((800, y - 24), 'Silhouettes: ink #17302f and cream #f7f4ec raw (left) vs fitted (right), as shown', fill='#f7f4ec', font=f)
for pi, (ph, g) in enumerate(GR.items()):
    x = 800 + pi * 330
    a = g(s2l('#17302f')); b = g(s2l(fills[ph])); c = g(s2l('#f7f4ec')); e = g(s2l(edges[ph]))
    sw(x, y + 28, hexof(a), ph + ' ink raw|' + hexof(a), w=100, h=40); sw(x + 110, y + 28, hexof(b), 'fitted|' + hexof(b), w=100, h=40)
    sw(x, y + 110, hexof(c), 'cream raw|' + hexof(c), w=100, h=30); sw(x + 110, y + 110, hexof(e), 'fitted|' + hexof(e), w=100, h=30)
im.save('board-signals.png'); print('board-signals.png', im.size)
