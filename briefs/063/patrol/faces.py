# Forma, brief 063: patrol face art options, ink on transparent, 29 x 29 u canvas (Glyph's glyph box), for briefs/057/patrol/disc.py build FACE.png
import sys, os
sys.path.insert(0, '/Users/sky/Projects/suborned/briefs/057/patrol')
import disc
from PIL import Image, ImageDraw
INK = (23, 48, 47, 255); CREAM = (247, 244, 236, 255); K = 16; N = 29 * K
def canvas(): im = Image.new('RGBA', (N, N), (0, 0, 0, 0)); return im, ImageDraw.Draw(im)
def rr(d, x0, y0, x1, y1, r, fill): d.rounded_rectangle((x0 * K, y0 * K, x1 * K, y1 * K), radius=r * K, fill=fill)
def disc_(d, cx, cy, r, fill): d.ellipse(((cx - r) * K, (cy - r) * K, (cx + r) * K, (cy + r) * K), fill=fill)
def A():
    """A: stern screen. Ink head, mast lamp, side ear caps, two dot-bar eyes and a short flat dot mouth: a watcher that does not smile."""
    im, d = canvas()
    rr(d, 12.9, 3.5, 16.1, 9, 0.6, INK); disc_(d, 14.5, 3.0, 2.6, INK)
    disc_(d, 1.6, 17.5, 1.6, INK); disc_(d, 27.4, 17.5, 1.6, INK)
    rr(d, 2.6, 7.5, 26.4, 28.5, 5.2, INK)
    for x in (6.6, 10.2, 18.8, 22.4): disc_(d, x, 14.2, 1.5, CREAM)
    for x in (11.2, 14.5, 17.8): disc_(d, x, 22.6, 1.3, CREAM)
    return im
def B():
    """B: scan line. Ink head and mast, one row of dots with a larger middle dot (the sweep): reads as 'watching', not as a face."""
    im, d = canvas()
    rr(d, 12.9, 3.5, 16.1, 9, 0.6, INK); disc_(d, 14.5, 3.0, 2.6, INK)
    disc_(d, 1.6, 17.5, 1.6, INK); disc_(d, 27.4, 17.5, 1.6, INK)
    rr(d, 2.6, 7.5, 26.4, 28.5, 5.2, INK)
    for i in range(7): disc_(d, 5.9 + i * 2.95, 18.0, 2.2 if i == 3 else 1.3, CREAM)
    return im
def C():
    """C: officer. As A with a peaked brim over the screen (the patrol's cap), eyes lifted a little outward, no ear caps."""
    im, d = canvas()
    rr(d, 12.9, 2.2, 16.1, 7.5, 0.6, INK); disc_(d, 14.5, 2.1, 2.1, INK)
    rr(d, 1.0, 6.6, 28.0, 10.2, 1.7, INK)
    rr(d, 3.2, 9.0, 25.8, 28.5, 5.0, INK)
    for x, y in ((6.8, 15.4), (10.4, 14.4), (18.6, 14.4), (22.2, 15.4)): disc_(d, x, y, 1.45, CREAM)
    for x in (11.4, 14.5, 17.6): disc_(d, x, 23.0, 1.25, CREAM)
    return im
def previews(opts):
    from PIL import Image
    W = Image.new('RGB', (len(opts) * 300, 400), (30, 60, 58))
    for i, (k, f) in enumerate(opts.items()):
        p = f'/Users/sky/Projects/suborned/out/scratch/forma/063/patrol/face_{k}.png'; f().save(p)
        b = disc.build(p); fb = disc.build_face(p)
        W.paste(b.resize((192, 192), Image.NEAREST), (i * 300 + 10, 10), b.resize((192, 192), Image.NEAREST))
        W.paste(b, (i * 300 + 220, 10), b); W.paste(b.resize((24, 24), Image.LANCZOS), (i * 300 + 220, 70), b.resize((24, 24), Image.LANCZOS))
        W.paste(fb.resize((192, 192), Image.NEAREST), (i * 300 + 10, 210))
        W.paste(fb, (i * 300 + 220, 210))
        b.save(f'/Users/sky/Projects/suborned/out/scratch/forma/063/patrol/npc_patrol_{k}@2x.png'); fb.save(f'/Users/sky/Projects/suborned/out/scratch/forma/063/patrol/npc_patrol_face_{k}@2x.png')
    W.save('/Users/sky/Projects/suborned/out/scratch/forma/063/patrol/preview.png')
if __name__ == '__main__': previews({'A': A, 'B': B, 'C': C})
