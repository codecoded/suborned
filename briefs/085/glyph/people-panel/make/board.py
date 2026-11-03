"""The phone boards: every mock full width, then its panel big enough to read on a phone."""
import sys
sys.path.insert(0, '.')
import make_options as MO, guide as G
from base import *
from kit import *
W = 1000


def crop(sh, box, width=W):
    world, L = sh; x0, y0, x1, y1 = box; s = L.s
    im = world.crop(box).resize(((x1 - x0) * s, (y1 - y0) * s), Image.LANCZOS).convert('RGBA'); im.alpha_composite(L.img.crop((x0 * s, y0 * s, x1 * s, y1 * s)))
    k = width / im.width; return im.resize((width, round(im.height * k)), Image.LANCZOS).convert('RGB')


def full(sh): return crop(sh, (0, 0, 1920, 1080))


def bar(title, sub=None, h=None, tag=None):
    f1, f2 = font('Jost-Bold', 58), font('Jost-Bold', 38); subs = sub if isinstance(sub, list) else [sub] if sub else []
    H = h or 92 + len(subs) * 52 + 16; im = Image.new('RGB', (W, H), BODY[:3]); d = ImageDraw.Draw(im); d.text((36, 22), title, font=f1, fill=CREAM)
    for i, t in enumerate(subs): d.text((36, 92 + i * 52), t, font=f2, fill=DIM)
    if tag:
        tw = f2.getlength(tag); d.rounded_rectangle((W - tw - 72, 26, W - 28, 88), 14, outline=TURQ, width=4); d.text((W - tw - 50, 34), tag, font=f2, fill=TURQ)
    return im


def label(t):
    f = font('Jost-Bold', 40); im = Image.new('RGB', (W, 72), INK[:3]); ImageDraw.Draw(im).text((36, 12), t, font=f, fill=CREAM); return im


def side(a, b, gap=10):
    h = max(a.height, b.height); im = Image.new('RGB', (a.width + b.width + gap, h), INK[:3]); im.paste(a, (0, 0)); im.paste(b, (a.width + gap, 0)); return im


def stack(parts, path):
    H = sum(p.height for p in parts); im = Image.new('RGB', (W, H), INK[:3]); y = 0
    for p in parts: im.paste(p, (0, y)); y += p.height
    im.save(path, optimize=True); print(path, im.size)


s = MO.shots
R, RB = (1540, 0, 1920, 370), (1540, 0, 1920, 420)
parts = [bar('People panel: pick one', ['Each shown at the start, then after a', 'Listen on Inez. Zoom any picture.'])]
parts += [bar('A · The dossier', ['Everyone open, all at once.', 'Most to read. Folds after your first Turn.']),
          label('Opening'), full(s['A-dossier-opening']), crop(s['A-dossier-opening'], (1550, 56, 1920, 520)),
          label('After Listen on Inez'), full(s['A-dossier-learned']), crop(s['A-dossier-learned'], (1550, 56, 1920, 520))]
parts += [bar('B · On the street', ['No panel. A face on each person, an arrow', 'for anyone off screen. Hover for the facts.']),
          label('Opening'), full(s['B-street-opening']), side(crop(s["B-street-opening"], (1085, 40, 1340, 170), 590), crop(s["B-street-opening"], (1720, 755, 1920, 935), 400)),
          label('Hover someone: their facts, no reading till then'), full(s['B-street-hover']), crop(s['B-street-hover'], (1370, 580, 1920, 935)),
          label('After Listen on Inez'), full(s['B-street-learned']), crop(s['B-street-learned'], (800, 225, 1190, 470))]
parts += [bar('C · Four faces, one card', ['Who matters at a glance. One card open.', 'A line ties the card to the person.'], tag='Pick this'),
          label('Opening'), full(s['C-rail-opening']), crop(s['C-rail-opening'], R),
          label('After Listen on Inez (Help hovered: its price)'), full(s['C-rail-learned']), crop(s['C-rail-learned'], RB),
          label('Click a face: the view goes to them'), full(s['C-rail-click-guard']), crop(s['C-rail-click-guard'], R)]
stack(parts, OUT + 'board.png')

g = G.shots
def steprow(n, say_box, line=(620, 1012, 1300, 1076)):
    return [crop(g[n], say_box), crop(g[n], line)]
LN = (620, 1012, 1300, 1076)
parts = [bar('A guided start', ['The clock waits. Each agent says what they', 'do. Your orders start together. Skip any time.'])]
parts += [bar('1 · Scout: hold left', ['Ring on Inez; the guard and driver lit on the', 'rail. Anyone works: it isn\'t a set route.']), full(g['guide-1-scout']), crop(g['guide-1-scout'], (0, 20, 480, 110)), crop(g['guide-1-scout'], LN)]
parts += [bar('2 · The order is taken', ['A real order. It waits as a ghost, with its', 'price. The card shows what\'s still hidden.']), full(g['guide-2-scout-taken']), crop(g['guide-2-scout-taken'], (1540, 0, 1920, 370))]
parts += [bar('3 · Fixer: left click opens the ring', ['Every way, its price or what it waits for.', 'Nothing is spent by opening it.']), full(g['guide-3-fixer']), crop(g['guide-3-fixer'], (0, 120, 480, 200)), crop(g['guide-3-fixer'], (610, 362, 1250, 590)), crop(g['guide-3-fixer'], LN)]
parts += [bar('4 · Tech: click the ground to walk', ['Points at the cabinet and the camera.', 'No nudge to Loop: it only lasts 2:00.']), full(g['guide-4-tech']), crop(g['guide-4-tech'], (0, 215, 480, 305)), crop(g['guide-4-tech'], LN)]
parts += [bar('5 · Enforcer: hold right, shown only', ['Both places he can take Vale are marked.', 'Nobody is asked to shoot on the first go.']), full(g['guide-5-enforcer']), crop(g['guide-5-enforcer'], (0, 310, 480, 400)), crop(g['guide-5-enforcer'], LN)]
parts += [bar('6 · Vale and the goal, then Start', ['Every order given starts on this one press.', 'The clock runs. Never shown again.']), full(g['guide-6-goal']), crop(g['guide-6-goal'], (1540, 0, 1920, 370)), crop(g['guide-6-goal'], (1520, 950, 1920, 1030))]
stack(parts, OUT + 'guide-board.png')
