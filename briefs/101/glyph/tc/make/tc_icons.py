"""Brief 101, take control (Glyph): the two pictograms the kit lacks, in its icons/singles style (solid, white on clear, 128 px for the @2x
file, round joints, the kit's weight):
  take_control: two chevrons stepping down into the ground ring the cell stand in (seen at the play camera's tilt): down to the street, as
  this agent;
  map: a folded paper map with a pin: back to the map.
Tried and dropped on the 1x proof (out/scratch/glyph/101tc/icons/variants*.png): a chevron over a bust (read as a medal on a lanyard), an
arrow over a bust (a cross over a person at 1x), a curved swoop (muddled at 16 px), a bust in its ring (reads "selected", not "take control"),
a map with a route line (busy at 14 px), an arrow with a shaft into the ring and a solid triangle into it (both too close
to the kit's drop, "Put down", which the face's act disc can show beside the tab).
Writes the two PNGs to briefs/101/glyph/tc/icons/ for Latch to copy into Kit/icons/singles/ (Glyph never writes unity/Assets) and a proof
sheet at the HUD's sizes beside kit neighbours. Mock only."""
import os, sys
from PIL import Image, ImageDraw, ImageChops
R = '/Users/sky/Projects/suborned/'
sys.path.insert(0, R + 'briefs/085/glyph/people-panel/make')
from kit import Layer, sprite

ICONS = R + 'briefs/101/glyph/tc/icons/'; OUTI = R + 'out/scratch/glyph/101tc/icons/'
N, K = 128, 8  # the @2x canvas, drawn at 8x and reduced
U = N * K / 64.0  # design units: 64 across


def canvas(): return Image.new('L', (N * K, N * K), 0)


def save(m, name):
    for d in (ICONS, OUTI): os.makedirs(d, exist_ok=True)
    a = m.resize((N, N), Image.LANCZOS); out = Image.new('RGBA', (N, N), (255, 255, 255, 0)); out.putalpha(a)
    out.save(ICONS + name + '@2x.png'); out.save(OUTI + name + '@2x.png'); return out


def take_control():
    """Two chevrons stepping down into the ground ring (the ring the cell stand in, at the play camera's tilt): down to the street, as this agent."""
    u = U; m = canvas(); g = ImageDraw.Draw(m)
    g.ellipse((4 * u, 37 * u, 60 * u, 63 * u), fill=255); g.ellipse((12.5 * u, 43.5 * u, 51.5 * u, 56.5 * u), fill=0)  # the ground ring
    gap = canvas(); gg = ImageDraw.Draw(gap)  # a clear margin round the chevrons where they cross the ring's back
    for y0 in (2, 20): gg.line([(10 * u, y0 * u), (32 * u, (y0 + 20) * u), (54 * u, y0 * u)], fill=255, width=round(15 * u), joint='curve')
    m = ImageChops.subtract(m, gap); g = ImageDraw.Draw(m); w = 7.4 * u
    for y0 in (4, 22):
        pts = [(15 * u, y0 * u), (32 * u, (y0 + 16) * u), (49 * u, y0 * u)]; g.line(pts, fill=255, width=round(w), joint='curve')
        for x, y in (pts[0], pts[2]): g.ellipse((x - w / 2, y - w / 2, x + w / 2, y + w / 2), fill=255)
    return save(m, 'take_control')


def map_():
    u = U; m = canvas(); g = ImageDraw.Draw(m)
    top = [(5, 12), (22, 6), (42, 12), (59, 6)]; bot = [(5, 58), (22, 52), (42, 58), (59, 52)]
    for i in range(3):
        (x0, y0), (x1, y1) = top[i], top[i + 1]; (x2, y2), (x3, y3) = bot[i + 1], bot[i]
        g.polygon([(x0 * u, y0 * u), (x1 * u, y1 * u), (x2 * u, y2 * u), (x3 * u, y3 * u)], fill=255)
    gap = canvas(); gg = ImageDraw.Draw(gap)  # the folds
    for i in (1, 2): x = top[i][0]; gg.line([(x * u, (top[i][1] - 3) * u), (x * u, (bot[i][1] + 3) * u)], fill=255, width=round(3.4 * u))
    m = ImageChops.subtract(m, gap)
    pin = canvas(); gp = ImageDraw.Draw(pin); px, py, pr = 32 * u, 30 * u, 6.5 * u; gp.ellipse((px - pr, py - pr, px + pr, py + pr), fill=255)
    m = ImageChops.subtract(m, pin); g = ImageDraw.Draw(m); p2 = 2.6 * u; g.ellipse((px - p2, py - p2, px + p2, py + p2), fill=255)  # the pin: a dot in a ring
    return save(m, 'map')


def proof():
    """The two beside kit neighbours, cream on the street tab's body at the HUD's sizes (tabs 26, 30, 32 px; the pictogram at 0.52), 1x and 2x."""
    BODY = (0x0c, 0x34, 0x34, 255); PAGE = (0xf9, 0xf7, 0xee, 255); names = ['take_control', 'map', 'units', 'turn_view', 'eye_open', 'enter', 'drop']
    P = Layer(2, 560, 150); ImageDraw.Draw(P.img).rectangle((0, 0, 1120, 300), fill=(0xe9, 0xe2, 0xd2, 255))
    for i, n in enumerate(names):
        x = 16 + i * 76
        for j, d in enumerate((26, 30, 32)):
            y = 8 + j * 46; P.disc(x, y, d, fill=BODY, ring=PAGE, rw=2)
            src = Image.open(ICONS + n + '@2x.png') if n in ('take_control', 'map') else sprite('icons/singles/' + n)
            isz = d * .52; a = src.getchannel('A').resize((round(isz * 2), round(isz * 2)), Image.LANCZOS)
            t = Image.new('RGBA', a.size, PAGE[:3] + (0,)); t.putalpha(a); P.img.alpha_composite(t, (round((x + (d - isz) / 2) * 2), round((y + (d - isz) / 2) * 2)))
    P.img.save(OUTI + 'proof-2x.png'); P.img.resize((560, 150), Image.LANCZOS).save(OUTI + 'proof-1x.png')


if __name__ == '__main__':
    take_control(); map_(); proof(); print('icons ->', ICONS)
