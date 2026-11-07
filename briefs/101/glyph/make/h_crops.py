"""The six moments cut from each direction's frames at 2x (the board's tiles and the 2x check), and today's pieces for the inventory."""
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from h_base import *

MOMENTS = [('seal', 'The seal warning', 'loud'), ('police', 'Police coming, with its why', 'loud'), ('top', 'The top bar mid-mission', 'quiet'),
           ('aim', 'A hover and aim on a target', 'aim'), ('feed', 'A feed card', 'quiet'), ('report', "The reporter's countdown", 'quiet')]
BOX = {'A': {'seal': (1452, 0, 1920, 440), 'police': (1440, 800, 1920, 1080), 'top': (1540, 0, 1920, 270), 'aim': (840, 60, 1380, 340), 'feed': (1450, 840, 1920, 1080), 'report': (790, 300, 1230, 540)},
       'B': {'seal': (1500, 0, 1920, 300), 'police': (1330, 920, 1920, 1080), 'top': (1250, 0, 1920, 150), 'aim': (850, 100, 1420, 380), 'feed': (1330, 920, 1920, 1080), 'report': (780, 290, 1270, 536)},
       'C': {'seal': (0, 0, 1920, 1080), 'police': (480, 200, 1920, 700), 'top': (1560, 0, 1920, 236), 'aim': (830, 130, 1310, 590), 'feed': (1560, 790, 1920, 920), 'report': (760, 320, 1170, 560)}}
FILE = {'A': 'A-page', 'B': 'B-sign', 'C': 'C-street'}
TODAY = {'seal': ('briefs/100/latch/shots/why-loud-police', (740, 58, 1180, 178)), 'police': ('briefs/100/latch/shots/why-loud-police', (1512, 925, 1912, 1072)),
         'top': ('briefs/100/latch/shots/why-quiet-radio', (1430, 6, 1912, 108)), 'aim': ('out/scratch/latch/cones100/proof/hold-fire-ready', (740, 380, 1180, 470)),
         'feed': ('briefs/100/latch/shots/why-quiet-radio', (1512, 935, 1912, 1072)), 'report': ('briefs/100/latch/shots/why-quiet-radio', (760, 360, 1040, 500))}


def stacked(im, boxes, gap=36):
    """C's police: where it happened and the police docked at the edge, each at 2x, stacked, joined by the dotted line between them."""
    parts = [im.crop(b).resize(((b[2] - b[0]) * 2, (b[3] - b[1]) * 2), Image.LANCZOS) for b in boxes]; w = max(p.width for p in parts)
    out = Image.new('RGB', (w, sum(p.height for p in parts) + gap), PAGE[:3]); y = 0
    for i, p in enumerate(parts):
        out.paste(p, (0 if i == 0 else w - p.width, y)); y += p.height + gap
    g = ImageDraw.Draw(out); y0 = parts[0].height
    for i in range(9): g.ellipse((w * .3 + i * w * .055 - 4, y0 + gap * (.2 + .6 * i / 8) - 4, w * .3 + i * w * .055 + 4, y0 + gap * (.2 + .6 * i / 8) + 4), fill=ORANGE[:3])
    return out


def cut(d, key):
    im = Image.open(OUT + FILE[d] + '-' + MOMENTS_KEY[key] + '.png'); x0, y0, x1, y1 = BOX[d][key]; c = im.crop((x0, y0, x1, y1))
    if (d, key) == ('C', 'police'):
        c = stacked(im, [(500, 214, 800, 286), (1600, 540, 1920, 660)]); c.save(OUT + f'crops/{d}-{key}.png'); return c
    z = 1 if (x1 - x0) > 1000 else 2; c = c.resize((c.width * z, c.height * z), Image.LANCZOS); os.makedirs(OUT + 'crops', exist_ok=True); c.save(OUT + f'crops/{d}-{key}.png'); return c


def cut_today(key):
    p, box = TODAY[key]; c = Image.open(R + p + '.png').convert('RGB').crop(box); c = c.resize((c.width * 2, c.height * 2), Image.LANCZOS); c.save(OUT + f'crops/today-{key}.png'); return c


MOMENTS_KEY = {k: f for k, _, f in MOMENTS}
if __name__ == '__main__':
    for d in 'ABC':
        tiles = [cut(d, k) for k, _, _ in MOMENTS]
        W = 2 * 980; rows = []
        for i in range(0, 6, 2):
            a, b = tiles[i], tiles[i + 1]; h = max(round(a.height * 960 / a.width), round(b.height * 960 / b.width))
            row = Image.new('RGB', (W, h), (30, 30, 30)); row.paste(a.resize((960, round(a.height * 960 / a.width))), (0, 0)); row.paste(b.resize((960, round(b.height * 960 / b.width))), (980, 0)); rows.append(row)
        sheet = Image.new('RGB', (W, sum(r.height for r in rows) + 40), (30, 30, 30)); y = 0
        for r in rows: sheet.paste(r, (0, y)); y += r.height + 20
        sheet.save(SCR + f'check-{d}.png')
    for k, _, _ in MOMENTS: cut_today(k)
    print('ok')
