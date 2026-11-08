"""The 2x crop check (art bible: every HUD piece cropped at 2x for clipping, overlap, centring and padding before Sky sees it), and the board's
close-ups. Writes out/scratch/glyph/101r2/check-<dir>-<moment>.png (all pieces of one frame on one sheet) and briefs/101/glyph/r2/crops/."""
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from r2_base import *

CORNERS = {'clock': (1480, 0, 1920, 250), 'roster': (0, 0, 200, 470), 'scope': (0, 870, 270, 1080), 'news': (1480, 960, 1920, 1080)}
STREET = {'quiet': [('report', (760, 330, 980, 560)), ('speech', (1020, 600, 1280, 740))], 'loud': [('dock', (1640, 480, 1920, 640)), ('speech', (980, 600, 1290, 740))],
          'aim': [('shot', (740, 140, 1110, 410))], 'rest': [],
          'busy': [('focus', (1000, 600, 1220, 820)), ('car', (900, 500, 1020, 580)), ('report', (1250, 80, 1420, 280)), ('speech', (620, 330, 1260, 740)), ('edge', (1820, 800, 1920, 900))]}
FILE = {'S': 'S', 'T': 'T', 'today': 'today'}


def sheet(d, m):
    im = Image.open(OUT2 + f'{FILE[d]}-{m}.png').convert('RGB'); tiles = [(k, im.crop(b)) for k, b in list(CORNERS.items()) + STREET[m]]
    tiles = [(k, t.resize((t.width * 2, t.height * 2), Image.LANCZOS)) for k, t in tiles]; W = 1800; x = y = 0; rowh = 0; pos = []
    for k, t in tiles:
        if x + t.width > W: x = 0; y += rowh + 16; rowh = 0
        pos.append((t, x, y)); x += t.width + 16; rowh = max(rowh, t.height)
    out = Image.new('RGB', (W, y + rowh), (40, 40, 40))
    for t, x, y in pos: out.paste(t, (x, y))
    out.save(SCR2 + f'check-{d}-{m}.png')


def close(d, m, key, box, z=2):
    im = Image.open(OUT2 + f'{FILE[d]}-{m}.png').convert('RGB').crop(box); im = im.resize((im.width * z, im.height * z), Image.LANCZOS)
    os.makedirs(OUT2 + 'crops', exist_ok=True); im.save(OUT2 + f'crops/{d}-{m}-{key}.png'); return im


if __name__ == '__main__':
    for d in ('S', 'T'):
        for m in ('rest', 'quiet', 'loud', 'aim', 'busy'): sheet(d, m)
    print('ok')
