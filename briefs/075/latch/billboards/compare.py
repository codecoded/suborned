"""Brief 075-F1 (Latch): the billboard round's pictures. Usage: python3 -I briefs/075/latch/billboards/compare.py <repo root>
- flyby/grid-billboards.png: old reel / 075-E stream / 075-F1 now at 6, 20, 33, 50, 61.5 s (frames in flyby/grid/, now = billboards-t*.png).
- shots/billboards/player-day-*.png: play zoom, the player's look (Glint075 harness, runtime lens + haze), day:
  café square and Office front at yaw 30 / 210: 075-E stream before | now (regression);
  bb-* (boards in view): posters off (every board on its base material = the canonical "mars" cell, in memory) | now (per-instance pick).
- shots/billboards/day-*.png: After072 harness (no runtime lens), stream | now."""
import sys, os, shutil
from PIL import Image, ImageDraw, ImageFont
root = sys.argv[1]
B = lambda *p: os.path.join(root, 'briefs', *p)
try:
    font = ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial Bold.ttf', 26); small = ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial Bold.ttf', 20)
except OSError:
    font = small = ImageFont.load_default()
INK, CREAM = (17, 20, 26), (247, 244, 236)
TW, TH, G = 800, 450, 8
stills = os.path.join(root, 'out', 'scratch', 'latch', '075f1', 'stills')
for t, f in (('6', 't06.0.png'), ('20', 't20.0.png'), ('33', 't33.0.png'), ('50', 't50.0.png'), ('61.5', 't61.5.png')):
    shutil.copyfile(os.path.join(stills, f), B('075', 'flyby', 'grid', f'billboards-t{t}.png'))
rows = [('6', 'boulevard (6 s)'), ('20', 'square (20 s)'), ('33', 'parade (33 s)'), ('50', 'skyport tower (50 s)'), ('61.5', 'skyline across the water (61.5 s)')]
cols = [('old', 'OLD reel'), ('stream', '075-E stream (before)'), ('billboards', '075-F1 now (billboards)')]
head, lab = 52, 34
g = Image.new('RGB', (3 * TW + 4 * G, head + len(rows) * (lab + TH + G)), INK); d = ImageDraw.Draw(g)
for j, (_, t) in enumerate(cols): d.text((G + j * (TW + G) + 6, 14), t, fill=CREAM, font=font)
for i, (t, name) in enumerate(rows):
    y = head + i * (lab + TH + G); d.text((G + 6, y + 6), name, fill=(176, 205, 199), font=small)
    for j, (k, _) in enumerate(cols):
        g.paste(Image.open(B('075', 'flyby', 'grid', f'{k}-t{t}.png')).convert('RGB').resize((TW, TH), Image.LANCZOS), (G + j * (TW + G), y + lab))
g.save(B('075', 'flyby', 'grid-billboards.png')); print(B('075', 'flyby', 'grid-billboards.png'))


def pair(a, b, la, lb, name, out):
    L, R = (Image.open(p).convert('RGB').resize((960, 540), Image.LANCZOS) for p in (a, b))
    c = Image.new('RGB', (1928, 540), INK); c.paste(L, (0, 0)); c.paste(R, (968, 0)); dd = ImageDraw.Draw(c)
    for x, t in ((12, la), (980, lb)):
        dd.rectangle((x - 6, 8, x + dd.textlength(t, font=font) + 6, 44), fill=INK); dd.text((x, 12), t, fill=CREAM, font=font)
    dd.rectangle((6, 500, 12 + dd.textlength(name, font=font), 536), fill=INK); dd.text((12, 504), name, fill=CREAM, font=font)
    c.save(out); print(out)


o = B('075', 'shots', 'billboards')
for v in ('office-front-y030', 'office-front-y210', 'cafe-square-y030', 'cafe-square-y210'):
    pair(B('075', 'shots', 'stream', 'player', f'today-day-{v}.png'), B('075', 'shots', 'billboards', 'player', f'today-day-{v}.png'),
         '075-E stream, player look (day)', '075-F1 now, player look (day)', v, os.path.join(o, f'player-day-{v}.png'))
for v in ('bb-rooftop-y120', 'bb-wall-y210', 'bb-wall-roof-y210', 'bb-pylons-y030'):
    pair(B('075', 'shots', 'billboards', 'player-off', f'today-day-{v}.png'), B('075', 'shots', 'billboards', 'player', f'today-day-{v}.png'),
         'no swap: every board "mars" (same build)', '075-F1 now: each board its pick', v, os.path.join(o, f'player-day-{v}.png'))
for v in ('wide-lamplight-y030', 'office-front-y030', 'office-front-y210', 'cell-open-y030', 'cell-open-y210'):
    pair(B('075', 'shots', 'stream', 'after072', v + '.png'), B('075', 'shots', 'billboards', 'after072', v + '.png'),
         '075-E stream (no runtime lens)', '075-F1 now (no runtime lens)', v, os.path.join(o, f'day-{v}.png'))
