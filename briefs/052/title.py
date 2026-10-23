"""Title lockup PNG (Glyph's lockup from briefs/047/layout/loading.json: small tracked THE over SUBORNED, Jost, cream #f7f4ec, 1 px brass #99784d rule the width of SUBORNED).
python3 -I title.py out.png [W H scale]   -> transparent RGBA; scale = ref-px multiplier at 1080p (loading screen = 1.0)."""
import sys, os
from PIL import Image, ImageDraw, ImageFont, ImageFilter
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '../..'))
F = ROOT + '/testbed/hud-style/fonts/'
def font(n, px): return ImageFont.truetype(F + n, max(1, round(px)))
def adv(f, text, i):
    nxt = text[i+1] if i + 1 < len(text) else ''
    return (f.getlength(text[i] + nxt) - f.getlength(nxt)) if nxt else f.getlength(text[i])
def width(text, f, tr): return sum(adv(f, text, i) + (tr if i + 1 < len(text) else 0) for i in range(len(text)))
def draw(d, x, base, text, f, fill, tr):
    for i, ch in enumerate(text):
        d.text((x, base), ch, font=f, fill=fill, anchor='ls'); x += adv(f, text, i) + tr
def render(W, H, k, cy=0.47):
    cream = (247, 244, 236); brass = (153, 120, 77); ink = (11, 22, 21)
    the_px, sub_px = 21 * k, 64 * k
    fT, fS = font('Jost-Regular-Tab.ttf', the_px), font('Jost-Medium-Tab.ttf', sub_px)
    trT, trS = 0.62 * the_px, 0.12 * sub_px
    wS = width('SUBORNED', fS, trS); wT = width('THE', fT, trT)
    lh = the_px * 0.72 + 6 * k + sub_px * 0.72 + 12 * k + max(2, k)
    x0 = (W - wS) / 2; y0 = H * cy - lh / 2
    base1 = y0 + the_px * 0.72; base2 = base1 + 6 * k + sub_px * 0.72; ry = base2 + 12 * k
    sh = Image.new('RGBA', (W, H), (0, 0, 0, 0)); sd = ImageDraw.Draw(sh)
    draw(sd, x0, base1 + 2 * k, 'THE', fT, ink + (90,), trT); draw(sd, x0, base2 + 2 * k, 'SUBORNED', fS, ink + (90,), trS)
    out = sh.filter(ImageFilter.GaussianBlur(7 * k))
    d = ImageDraw.Draw(out)
    draw(d, x0, base1, 'THE', fT, cream + (235,), trT); draw(d, x0, base2, 'SUBORNED', fS, cream + (255,), trS)
    d.rectangle([x0, ry, x0 + wS * 0.999, ry + max(2, round(1 * k))], fill=brass + (255,))
    return out
if __name__ == '__main__':
    a = sys.argv[1:]; W, H = (int(a[1]), int(a[2])) if len(a) > 2 else (1920, 1080); k = float(a[3]) if len(a) > 3 else 2.0
    render(W, H, k).save(a[0])
