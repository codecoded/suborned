"""Brief 104 · patrol robot concepts (Vela): the play-size strip.

    python3 -I strip.py <cuts_dir> <robots_play_dir> <out.png> [--x 1]

Every cut-out at play size beside a person on the civic square's paving. Play size: a 1.75 m person is about 82 px (play zoom 10 on a
1080 px screen); each sheet's grey silhouette is 1.75 m, so a sheet's cut-outs scale by 82 / silhouette px. The person is Inez as the
game draws her (a crop of out/batch/shots/cafe-front-day.png, 1:1). The first row is today's robots, rendered with the play camera
(briefs/104/vela/robots/make/render.py, 54 px per metre), so all rows share one scale. --x 2 draws everything twice as big.
"""
import json, sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[5]
PAVING = (219, 193, 160)
INK, DIM = (23, 48, 47), (96, 110, 106)
PERSON_PX = 82
FONT = '/System/Library/Fonts/Avenir Next.ttc'
SHEETS = [('A', 'Friendly civic'), ('B', 'Quietly stern'), ('C', 'Signal and silhouette')]
TODAY = [('PatrolRobot', 'patrol', 'today'), ('PorterLamp', 'porter', 'response'), ('RobotButler', 'butler', 'street'), ('RobotWaiter', 'waiter', 'street')]


def font(size, bold=False):
    return ImageFont.truetype(FONT, size, index=0 if bold else 5)


def inez(x=1):
    """Inez at play zoom with her own paving, feathered at the crop's edges so it melts into the strip's ground."""
    im = Image.open(ROOT / 'out/batch/shots/cafe-front-day.png').convert('RGB').crop((928, 451, 1002, 545))
    a = np.full((im.height, im.width), 255.0)
    f = 10
    ramp = lambda n: np.minimum(1, np.minimum(np.arange(n), np.arange(n)[::-1]) / f)
    a *= ramp(im.width)[None, :] * ramp(im.height)[:, None]
    c = im.convert('RGBA'); c.putalpha(Image.fromarray(a.astype(np.uint8)))
    c = c.resize((c.width * x, c.height * x), Image.LANCZOS) if x != 1 else c
    return c, 85 * x, 'Inez'


def today(play_dir, mid, x=1):
    """A play-camera render: crop to its pixel bounds plus its shadow; drop the faint sky-occlusion veil."""
    im = Image.open(play_dir / f'{mid}.png').convert('RGBA')
    meta = json.loads((play_dir / f'{mid}.json').read_text())
    a = np.asarray(im).astype(np.float32)
    shadow = a[..., :3].max(axis=2) < 24
    a[..., 3] = np.where(shadow, np.clip((a[..., 3] - 30) * 255 / 225, 0, 255) * 0.8, a[..., 3])
    c = Image.fromarray(a.astype(np.uint8), 'RGBA')
    x0, y0, x1, y1 = meta['px_bounds']
    gy = meta['ground_px'][1]
    box = (max(0, x0 - 8), max(0, y0 - 2), min(im.width, x1 + 8), min(im.height, int(max(y1, gy + 8) + 6)))
    c = c.crop(box)
    if x != 1: c = c.resize((c.width * x, c.height * x), Image.LANCZOS)
    return c, (gy - box[1]) * x


def cutouts(cuts, key, x=1):
    meta = json.loads((cuts / key / 'cuts.json').read_text())
    s = PERSON_PX * x / meta['silhouette_px']
    out = []
    for f in meta['figures']:
        if f['id'] == 'silhouette': continue
        im = Image.open(cuts / key / f"{f['id']}.png").convert('RGBA')
        im = im.resize((max(1, round(im.width * s)), max(1, round(im.height * s))), Image.LANCZOS)
        out.append((f['id'], im, f['feet_y_in_crop'] * s, f['height_m']))
    return out


def row(items, title, x=1, width=None, gap=None):
    """items: (label, sub, image, feet_y). One ground line; labels under each figure."""
    gap = gap or 30 * x
    lf, sf, tf = font(13 * x, True), font(11 * x), font(14 * x, True)
    top = max(fy for _, _, _, fy in items)
    below = max(4 * x, max(c.height - fy for _, _, c, fy in items) - 6 * x)   # labels clear the lowest drawn pixel (shadows, the porter's trolley)
    W = width or (gap + sum(im.width + gap for _, _, im, _ in items))
    H = int(top + 24 * x + below + 36 * x)
    im = Image.new('RGBA', (W, H), PAVING + (255,))
    d = ImageDraw.Draw(im)
    if width:
        gap = max(gap, (W - sum(c.width for _, _, c, _ in items)) // (len(items) + 1))
    gx = gap
    ground = int(top + 22 * x)
    for label, sub, c, fy in items:
        im.alpha_composite(c, (gx, int(ground - fy)))
        cx = gx + c.width // 2
        for t, f, col, dy in ((label, lf, INK, 0), (sub, sf, DIM, 15)):
            if not t: continue
            tw = d.textlength(t, font=f)
            d.text((cx - tw / 2, ground + below + dy * x), t, fill=col, font=f)
        gx += c.width + gap
    head = Image.new('RGBA', (W, 26 * x), (242, 237, 226, 255))
    ImageDraw.Draw(head).text((8 * x, 4 * x), title, fill=INK, font=tf)
    out = Image.new('RGBA', (W, head.height + H)); out.alpha_composite(head); out.alpha_composite(im, (0, head.height))
    return out


def build(cuts, play_dir, x=1, width=None):
    """The strip's rows stacked: today's robots, then sheets A, B and C."""
    p, pfy, plab = inez(x)
    rows = [row([(plab, 'a person', p, pfy)] + [(lab, sub, *today(play_dir, mid, x)) for mid, lab, sub in TODAY], "Today in the game", x, width)]
    for key, name in SHEETS:
        items = [(plab, 'a person', p, pfy)] + [(fid, f'{hm:.1f} m', im, fy) for fid, im, fy, hm in cutouts(cuts, key, x)]
        rows.append(row(items, f'{key} · {name}', x, width))
    W = max(r.width for r in rows)
    out = Image.new('RGBA', (W, sum(r.height for r in rows)), (242, 237, 226, 255))
    y = 0
    for r in rows:
        out.alpha_composite(r, (0, y)); y += r.height
    return out.convert('RGB')


def main(a):
    x = int(a[a.index('--x') + 1]) if '--x' in a else 1
    im = build(Path(a[0]), Path(a[1]), x)
    im.save(a[2]); print(f'{a[2]} {im.size}')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
