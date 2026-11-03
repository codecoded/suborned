"""Brief 075 (Latch): the rebuild's pictures. Usage: python3 -I briefs/075/latch/compare.py <repo root>
- flyby/grid.png: old reel / 074 new (colourways on) / 075 new at 6, 20, 33, 50, 61.5 s (frames in flyby/grid/).
- shots/compare/*.png: play zoom, before | after. day-*: 074 before (After072 harness, no runtime lens) against 075 (same harness);
  player-day-* / player-night-*: the player's look (Glint075 harness, runtime lens + haze) on the 074 scene (briefs/075/grade/today) against 075."""
import sys, os
from PIL import Image, ImageDraw, ImageFont
root = sys.argv[1]
B = lambda *p: os.path.join(root, 'briefs', *p)
try:
    font = ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial Bold.ttf', 26); small = ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial Bold.ttf', 20)
except OSError:
    font = small = ImageFont.load_default()
INK, CREAM = (17, 20, 26), (247, 244, 236)
# grid
TW, TH, G = 800, 450, 8
rows = [('6', 'boulevard (6 s)'), ('20', 'square: Crown Circus / the old circus (20 s)'), ('33', 'parade (33 s)'), ('50', 'skyport tower (50 s)'), ('61.5', 'skyline across the water (61.5 s)')]
cols = [('old', 'OLD reel'), ('074', '074 NEW (colourways on)'), ('new', '075 NEW (A, B, C 1-2, 074-H)')]
head, lab = 52, 34
g = Image.new('RGB', (3 * TW + 4 * G, head + len(rows) * (lab + TH + G)), INK); d = ImageDraw.Draw(g)
for j, (_, t) in enumerate(cols): d.text((G + j * (TW + G) + 6, 14), t, fill=CREAM, font=font)
for i, (t, name) in enumerate(rows):
    y = head + i * (lab + TH + G); d.text((G + 6, y + 6), name, fill=(176, 205, 199), font=small)
    for j, (k, _) in enumerate(cols):
        g.paste(Image.open(B('075', 'flyby', 'grid', f'{k}-t{t}.png')).convert('RGB').resize((TW, TH), Image.LANCZOS), (G + j * (TW + G), y + lab))
g.save(B('075', 'flyby', 'grid.png')); print(B('075', 'flyby', 'grid.png'))
# play-zoom compare
def pair(a, b, la, lb, name, out):
    L, R = (Image.open(p).convert('RGB').resize((960, 540), Image.LANCZOS) for p in (a, b))
    c = Image.new('RGB', (1928, 540), INK); c.paste(L, (0, 0)); c.paste(R, (968, 0)); dd = ImageDraw.Draw(c)
    for x, t in ((12, la), (980, lb)):
        dd.rectangle((x - 6, 8, x + dd.textlength(t, font=font) + 6, 44), fill=INK); dd.text((x, 12), t, fill=CREAM, font=font)
    dd.rectangle((6, 500, 12 + dd.textlength(name, font=font), 536), fill=INK); dd.text((12, 504), name, fill=CREAM, font=font)
    c.save(out); print(out)
o = B('075', 'shots', 'compare')
for v in ('office-front-y030', 'office-front-y210', 'cell-open-y030', 'cell-open-y210', 'wide-lamplight-y030'):
    pair(B('074', 'shots', 'before', v + '.png'), B('075', 'shots', 'after', v + '.png'), '074 before (no runtime lens)', '075 (no runtime lens)', v, os.path.join(o, f'day-{v}.png'))
for ph in ('day', 'night'):
    for v in ('office-front-y030', 'office-front-y210', 'cafe-square-y030', 'cafe-square-y210'):
        pair(B('075', 'grade', 'today', f'today-{ph}-{v}.png'), B('075', 'shots', 'player', f'today-{ph}-{v}.png'), f'074 scene, player look ({ph})', f'075 rebuild, player look ({ph})', v, os.path.join(o, f'player-{ph}-{v}.png'))
