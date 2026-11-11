"""Brief 104 · patrol robot concepts (Vela): the board, 1000 px wide for a phone.

    python3 -I board.py <cuts_dir> <robots_play_dir> <out.png>

Sections: my top three · the three in the civic square at play size (pasted into out/batch/shots/start-overhead-day.png, 1:1) ·
every design at play size (strip.py) · the three lineup sheets with each design's id. Sheets come from paid/104-patrol-robot/codex/.
"""
import json, sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, str(Path(__file__).resolve().parent))
import strip  # noqa: E402

ROOT = Path(__file__).resolve().parents[5]
SHEETS = ROOT / 'paid/104-patrol-robot/codex'
W, M = 1000, 24
CW = W - 2 * M
BG, INK, DIM, RULE = (242, 237, 226), (23, 48, 47), (96, 110, 106), (208, 199, 181)
TEAL, CORAL, CREAM = (28, 120, 108), (170, 72, 50), (247, 244, 236)
FONT = '/System/Library/Fonts/Avenir Next.ttc'
STYLE = {'bold': 0, 'demi': 2, 'medium': 5, 'regular': 7}

SHEET = {'A': ('A-civic.png', 'Friendly civic'), 'B': ('B-stern.png', 'Quietly stern'), 'C': ('C-signal.png', 'Signal and silhouette: alarm signals drawn lit')}
NAMES = {'A1': 'beetle', 'A2': 'stilt bird', 'A3': 'saucer', 'A4': 'unicycle', 'A5': 'rolling ball', 'A6': 'arch',
         'B1': 'car front', 'B2': 'pillar', 'B3': 'warden', 'B4': 'ticket walker', 'B5': 'cabinet', 'B6': 'manta',
         'C1': 'handle', 'C2': 'crest', 'C3': 'crescent', 'C4': 'deco tower', 'C5': 'zigzag', 'C6': 'hoop'}
TOP = [
    ('C2', 'Its alarm is its outline: a red crest that sweeps back, so you can see which way it faces from above, even unlit. Broad shoulders give it authority without armour, and its face sits high, where the play camera sees it.',
     'Two legs, an easy rig. The crest folds or shortens for the dock (drawn 2.0 m with it).'),
    ('B1', "Low and long, so it can't be mistaken for a person or the upright service robots. Grille face at the front, fin at the back, as plain as a 1950s car; the dark red fin is its alarm, unlit on its round.",
     'Narrow and shorten it for the dock (0.98 m wide inside). Four legs fit the crowd rig.'),
    ('C5', 'The zigzag crown is the clearest alarm shape on any sheet, and it reports on a telephone handset: by voice and wire, the way this city works.',
     'Narrow it to the dock. Six legs fit 22 bones only at two bones a leg.'),
]
SQUARE = {'crop': (480, 400, 1432, 850), 'spots': {'C2': (965, 615), 'B1': (1135, 585), 'C5': (1050, 752)}}


def font(style, size):
    return ImageFont.truetype(FONT, size, index=STYLE[style])


def wrap(s, f, width):
    lines, cur = [], ''
    for word in s.split():
        t = (cur + ' ' + word).strip()
        if f.getlength(t) <= width or not cur: cur = t
        else: lines.append(cur); cur = word
    return lines + ([cur] if cur else [])


def chip(d, cx, y, label, bg, f=None):
    f = f or font('bold', 13)
    w = f.getlength(label) + 14
    d.rounded_rectangle((cx - w / 2, y, cx + w / 2, y + 21), 10, fill=bg)
    d.text((cx - w / 2 + 7, y + 2), label, fill=CREAM, font=f)


class Board:
    def __init__(self):
        self.parts, self.h = [], 0

    def add(self, h, fn):
        self.parts.append((self.h, fn)); self.h += h

    def text(self, lines, f, col, lh, x=M, top=0, bottom=0):
        def draw(im, y):
            d = ImageDraw.Draw(im)
            for i, t in enumerate(lines): d.text((x, y + top + i * lh), t, fill=col, font=f)
        self.add(top + len(lines) * lh + bottom, draw)

    def image(self, im, x=M, top=0, bottom=0):
        self.add(top + im.height + bottom, lambda c, y: c.paste(im, (x, y + top)))

    def heading(self, title, sub=None):
        self.add(14, lambda im, y: ImageDraw.Draw(im).line((M, y + 6, W - M, y + 6), fill=RULE, width=2))
        self.text([title], font('bold', 24), INK, 32, top=6)
        if sub: self.text(wrap(sub, font('medium', 15), CW), font('medium', 15), DIM, 21, bottom=8)

    def render(self):
        im = Image.new('RGB', (W, self.h + M), BG)
        for y, fn in self.parts: fn(im, y)
        return im


def cuts_meta(cuts, key):
    return json.loads((cuts / key / 'cuts.json').read_text())


def figure(cuts, fid):
    return next(f for f in cuts_meta(cuts, fid[0])['figures'] if f['id'] == fid)


def paper(sheet):
    """The sheet's paper colour: the median of its border."""
    import numpy as np
    a = np.asarray(sheet.convert('RGB'))
    edge = np.concatenate([a[:8].reshape(-1, 3), a[-8:].reshape(-1, 3), a[:, :8].reshape(-1, 3), a[:, -8:].reshape(-1, 3)])
    return tuple(int(v) for v in np.median(edge, 0))


def design_crop(cuts, fid, box_w, box_h):
    """The design's cut-out (with its shadow and glow) on its sheet's paper, fitted to box_w × box_h: no neighbours, no seams."""
    bg = paper(Image.open(SHEETS / SHEET[fid[0]][0]))
    c = Image.open(cuts / fid[0] / f'{fid}.png').convert('RGBA')
    s = min((box_w - 20) / c.width, (box_h - 12) / c.height)
    c = c.resize((round(c.width * s), round(c.height * s)), Image.LANCZOS)
    out = Image.new('RGBA', (box_w, box_h), bg + (255,))
    out.alpha_composite(c, ((box_w - c.width) // 2, box_h - c.height - 6))
    return out.convert('RGB')


def top_three(b, cuts):
    col_w, gap, img_h = 300, (CW - 3 * 300) // 2, 330
    fb, fw, fn = font('bold', 19), font('medium', 15), font('regular', 14)
    blocks = []
    for fid, why, needs in TOP:
        f = figure(cuts, fid)
        blocks.append((design_crop(cuts, fid, col_w, img_h), f'{fid} · {NAMES[fid]}', f"drawn {f['height_m']:.1f} m tall",
                       wrap(why, fw, col_w), wrap(needs, fn, col_w)))
    h = img_h + 12 + 28 + 22 + max(len(w) for *_, w, _ in blocks) * 21 + 8 + max(len(n) for *_, n in blocks) * 19 + 16

    def draw(im, y):
        d = ImageDraw.Draw(im)
        for i, (img, title, size, why, needs) in enumerate(blocks):
            x = M + i * (col_w + gap)
            im.paste(img, (x, y)); yy = y + img_h + 12
            d.text((x, yy), title, fill=TEAL, font=fb); yy += 28
            d.text((x, yy), size, fill=DIM, font=fn); yy += 22
            for t in why: d.text((x, yy), t, fill=INK, font=fw); yy += 21
            yy = y + img_h + 12 + 28 + 22 + max(len(w) for *_, w, _ in blocks) * 21 + 8
            for t in needs: d.text((x, yy), t, fill=DIM, font=fn); yy += 19
    b.add(h, draw)


def square(cuts):
    """The top three pasted into the civic square as the game draws it at play zoom, 1:1, next to the cell (a person is about 82 px)."""
    shot = Image.open(ROOT / 'out/batch/shots/start-overhead-day.png').convert('RGBA')
    tags = []
    for fid, (fx, fy) in SQUARE['spots'].items():
        meta = cuts_meta(cuts, fid[0]); s = strip.PERSON_PX / meta['silhouette_px']; f = figure(cuts, fid)
        im = Image.open(cuts / fid[0] / f'{fid}.png').convert('RGBA')
        im = im.resize((round(im.width * s), round(im.height * s)), Image.LANCZOS)
        x, y = int(fx - f['centre_x_in_crop'] * s), int(fy - f['feet_y_in_crop'] * s)
        shot.alpha_composite(im, (x, y))
        tags.append((fid, fx, y + round(36 * s) - 26))
    x0, y0, x1, y1 = SQUARE['crop']
    out = shot.crop(SQUARE['crop']).convert('RGB')
    d = ImageDraw.Draw(out)
    for fid, fx, ty in tags: chip(d, fx - x0, max(4, ty - y0), fid, TEAL)
    return out


def sheet_with_ids(cuts, key):
    sheet = Image.open(SHEETS / SHEET[key][0]).convert('RGB')
    figs = cuts_meta(cuts, key)['figures']
    s = CW / sheet.width
    y0 = max(0, min(f['box'][1] for f in figs) - round(44 / s))       # trim the empty paper: room for the chips above, a margin below
    y1 = min(sheet.height, max(f['box'][3] for f in figs) + round(24 / s))
    im = sheet.crop((0, y0, sheet.width, y1)).resize((CW, round((y1 - y0) * s)), Image.LANCZOS)
    d = ImageDraw.Draw(im)
    picks = {fid for fid, *_ in TOP}
    for f in figs:
        if f['id'] == 'silhouette': continue
        x0, top, x1, _ = f['box']
        chip(d, (x0 + x1) / 2 * s, max(4, (top - y0) * s - 28), f"{f['id']} {NAMES[f['id']]}", TEAL if f['id'] in picks else INK, font('bold', 12))
    return im


def main(a):
    cuts, play, out = Path(a[0]), Path(a[1]), Path(a[2])
    b = Board()
    b.text(['Patrol robot · concepts'], font('bold', 34), INK, 44, top=M)
    b.text(wrap('Brief 104 · Vela · 2026-11-10. Sky asked for concept work on a real patrol robot. Three lineup sheets of six designs each, '
                'from an open brief: the robot\'s jobs and facts, not a design. Codex image tool, 3 images, $0.', font('medium', 16), CW),
           font('medium', 16), DIM, 23, bottom=10)
    b.heading('My top three', 'Picked by what survives at play zoom: an outline no person or service robot has, a face whose direction reads '
                               'from above, and an alarm signal with its own shape. Green chips mark them on the sheets below.')
    top_three(b, cuts)
    b.heading('In the square at play size', 'The three pasted into the civic square as the game draws it at play zoom, 1:1, beside the cell '
                                            '(a person is about 82 px tall). C2 and C5 are drawn with their alarm signal lit; B1 is on its round, signal dark.')
    b.image(square(cuts), bottom=10)
    b.heading('Every design at play size', 'Cut from the sheets and scaled so a 1.75 m person is 82 px, beside Inez as the game draws her. '
                                           'The first row is today\'s robots, rendered with the play camera. Heights are as drawn, against the sheets\' 1.75 m figure.')
    b.image(strip.build(cuts, play, 1, CW), bottom=10)
    for key, (_, name) in SHEET.items():
        b.heading(f'Sheet {key} · {name}', None)
        b.image(sheet_with_ids(cuts, key), top=4, bottom=10)
    b.heading('Next, once Sky picks', None)
    b.text(wrap('1. A turnaround sheet of the pick (front, side, back, top), with its four dot-matrix faces (calm, noticed, wary, reporting), '
                'its alarm signal dark and lit, and a check from the 38° play camera.', font('medium', 15), CW)
           + wrap('2. Then the 3D piece in the DecoKit: the dot-matrix face the engine drives, sized for the 0.98 m dock and to the crowd budget '
                  '(about 12k triangles, 22 bones).', font('medium', 15), CW), font('medium', 15), INK, 21, bottom=6)
    im = b.render()
    out.parent.mkdir(parents=True, exist_ok=True)
    im.save(out, optimize=True)
    print(f'{out} {im.size}')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
