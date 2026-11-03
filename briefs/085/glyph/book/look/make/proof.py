"""The real proof of the case file: Inez at 2:10, true type and real data on a textless Codex plate. Glyph, brief 085. Words are draft for Fable."""
import sys, math, random
sys.path.insert(0, '/Users/sky/Projects/suborned/briefs/085/glyph/book/make')
from page import *
from PIL import ImageEnhance
R = '/Users/sky/Projects/suborned/'; OUT = R + 'briefs/085/glyph/book/look/'
PLATE = R + 'paid/085-book/codex/plate-case-file.png'
CARD, BLUSH, ENAMEL, CORAL = (0xf6, 0xf0, 0xe2, 255), (0xf6, 0xdc, 0xd0, 255), (0x1c, 0x5a, 0x5c, 255), (0xd9, 0x55, 0x3f, 255)
S = 2
random.seed(85)


class Sheet(Page):
    """One piece of paper: its own layer, drawn flat, then laid on the spread at an angle with a shadow."""
    def __init__(self, w, h, fill=CARD, grain=True):
        super().__init__(S, w, h)
        ImageDraw.Draw(self.img).rounded_rectangle((0, 0, w * S - 1, h * S - 1), 3 * S, fill=fill)
        self.fill_ = fill

    def on(self, spread, x, y, deg=0.0, lift=7):
        im = self.img
        g = Image.open(PLATE).convert('L').crop((180, 720, 700, 880)).resize(im.size, Image.BICUBIC); m = sum(g.resize((8, 8)).get_flattened_data()) / 64
        g = g.point(lambda v: max(0, min(255, int(250 + (v - m) * .5))))
        a0 = im.getchannel('A'); im = ImageChops.multiply(im.convert('RGB'), Image.merge('RGB', (g, g, g))).convert('RGBA'); im.putalpha(a0)
        if deg: im = im.rotate(deg, resample=Image.BICUBIC, expand=True)
        a = im.getchannel('A'); sh = Image.new('RGBA', im.size, (40, 25, 10, 0)); sh.putalpha(a.point(lambda v: int(v * .45)))
        pad = 24 * S; canvas = Image.new('RGBA', (im.width + 2 * pad, im.height + 2 * pad), (0, 0, 0, 0))
        canvas.alpha_composite(sh, (pad + 2 * S, pad + lift * S // 2)); canvas = canvas.filter(ImageFilter.GaussianBlur(lift * S * .6)); canvas.alpha_composite(im, (pad, pad))
        spread.img.alpha_composite(canvas, (round(x * S) - pad - (im.width - self.w * S) // 2, round(y * S) - pad - (im.height - self.h * S) // 2))
        spread.words += self.words


def clip(P, x, y, deg=-8, h=74):
    """A wire clip, drawn."""
    k = 3; W, H = 34, h; big = Image.new('RGBA', (W * S * k, H * S * k), (0, 0, 0, 0)); g = ImageDraw.Draw(big); n = S * k; wd = 3 * n
    col = (0xa8, 0x8a, 0x5a, 255)
    g.rounded_rectangle((3 * n, 2 * n, 31 * n, (H - 3) * n), 13 * n, outline=col, width=wd)
    g.rounded_rectangle((10 * n, 14 * n, 25 * n, (H - 14) * n), 7 * n, outline=(0xc8, 0xac, 0x78, 255), width=wd)
    im = big.resize((W * S, H * S), Image.LANCZOS).rotate(deg, resample=Image.BICUBIC, expand=True)
    P.img.alpha_composite(im, (round(x * S), round(y * S)))


def stamp(P, x, y, t, size=62, deg=9):
    """A rubber stamp in coral ink, with the worn edge a stamp has."""
    w = P.tw(t, size, 'Jost-Bold') + 44; h = size + 34; k = Layer(S, round(w) + 8, round(h) + 8)
    g = ImageDraw.Draw(k.img); g.rounded_rectangle((4 * S, 4 * S, (w + 4) * S, (h + 4) * S), 6 * S, outline=CORAL, width=5 * S)
    k.text(w / 2 + 4, h / 2 + 3, t, size, col=CORAL, face='Jost-Bold', anchor='mm')
    a = k.img.getchannel('A'); noise = Image.effect_noise(a.size, 60).point(lambda v: 255 if v > 92 else 150); k.img.putalpha(ImageChops.multiply(a, noise))
    im = k.img.rotate(deg, resample=Image.BICUBIC, expand=True); P.img.alpha_composite(im, (round(x * S), round(y * S)))


def photo(src, w, h, focus=None):
    """A print: cream border, the picture filling it."""
    b = 12; sh = Sheet(w, h, fill=(0xfa, 0xf7, 0xee, 255)); im = src.convert('RGB'); tw, th = w - 2 * b, h - 2 * b; ar = tw / th
    iw, ih = im.size
    if iw / ih > ar: nw = int(ih * ar); x0 = (iw - nw) // 2; im = im.crop((x0, 0, x0 + nw, ih))
    else: nh = int(iw / ar); y0 = int((ih - nh) * (focus if focus is not None else .5)); im = im.crop((0, y0, iw, y0 + nh))
    sh.paste(im.convert('RGBA').resize((tw * S, th * S), Image.LANCZOS), b, b); return sh


def round_icon(P, icon, x, y, d, fill=ENAMEL, col=CREAM):
    P.disc(x, y, d, fill=fill, ring=None); P.icon(icon, x + d * .2, y + d * .2, d * .6, col=col)


def spread():
    base = Image.open(PLATE).convert('RGB'); base = ImageEnhance.Color(base).enhance(.86); base = base.resize((1920 * S, 1080 * S), Image.LANCZOS)
    P = Page(S); P.img = base.convert('RGBA'); return P


def tabs(P, active):
    # the plate's three cut tabs, and a fourth cut from the third
    k = 3; W, H = 110, 250; big = Image.new('RGBA', (W * S * k, H * S * k), (0, 0, 0, 0)); g = ImageDraw.Draw(big); n = S * k
    g.polygon([(0, 8 * n), (60 * n, 26 * n), (92 * n, 54 * n), (100 * n, 90 * n), (100 * n, 180 * n), (96 * n, 214 * n), (50 * n, 244 * n), (0, 248 * n)], fill=(0xb7, 0xbd, 0xae, 255))
    t = big.resize((W * S, H * S), Image.LANCZOS); sh = Image.new('RGBA', t.size, (30, 18, 8, 0)); sh.putalpha(t.getchannel('A').point(lambda v: v // 2)); sh = sh.filter(ImageFilter.GaussianBlur(5 * S))
    P.img.alpha_composite(sh, (1776 * S, 776 * S)); P.img.alpha_composite(t, (1770 * S, 770 * S))
    for name, cy, cx in (('Your cell', 175, 1776), ('The job', 408, 1806), ('The people', 646, 1830), ('The journal', 888, 1826)):
        on = name == active; size = 30 if on else 26; k = Layer(S, 240, 54); k.text(120, 27, name, size, col=INK if not on else INK_DEEP, face='Jost-Bold', anchor='mm')
        im = k.img.rotate(-90, expand=True); P.img.alpha_composite(im, (round((cx - 27) * S), round((cy - 120) * S))); P.words_add(name)
        if on:
            ImageDraw.Draw(P.img).rounded_rectangle(((cx + 28) * S, (cy - 80) * S, (cx + 32) * S, (cy + 80) * S), 2 * S, fill=INK)


def chrome_bits(P, clock):
    x = 1644; w = P.plate(x, 8, clock, 'pause', h=38, size=19)
    P.art('buttons/btn_close_normal', x + w + 10, 8, 38)


def inez():
    P = spread(); tabs(P, 'The people')
    # the big print, the name, the stamp
    src = Image.open(R + 'briefs/063/cameos/inez_A.png').convert('RGB'); i = 92; src = src.crop((i, i - 20, 512 - i, 512 - i - 20))
    ph = photo(src, 530, 660); ph.on(P, 132, 40, 1.6, lift=10)
    clip(P, 196, 10, 8, 88)
    nm = Sheet(570, 280, fill=(0xfa, 0xf6, 0xea, 255)); nm.put(32, 100, 'Inez', 150, face='Jost-Bold'); nm.put(36, 206, 'café waitress', 38, col=SOFT); nm.on(P, 112, 716, -1.0, lift=8)
    # who else is in the file, peeking out: click to turn to them
    for j, (role, name, deg) in enumerate((('guard', 'Okafor', -3), ('driver', 'Driver', 2), ('councillor', 'Vale', -1.5))):
        f = {'guard': 'model_security', 'driver': 'driver_A', 'councillor': 'vale_C'}[role]; im = Image.open(R + f'briefs/063/cameos/{f}.png').convert('RGB'); c = 72; im = im.crop((c, c, 512 - c, 512 - c))
        s = photo(im, 176, 200); s.on(P, 696, 66 + j * 305, deg, lift=6)
        lab = Sheet(136, 40, fill=(0xfa, 0xf6, 0xea, 255)); lab.put(68, 20, name, 25, face='Jost-SemiBold', anchor='mm'); lab.on(P, 716, 66 + j * 305 + 176, deg, lift=3)
    # the facts, the guard's snapshot clipped over them
    fx = Sheet(470, 240)
    for k, (ic, w, note) in enumerate((('debt', 'Bureau loan', 'cleared'), ('fearful', 'Fearful', None), ('lovers', 'Loves Okafor', None))):
        y = 18 + k * 72; round_icon(fx, ic, 22, y, 58, fill=CORAL if k == 2 else ENAMEL); fx.put(96, y + 29, w, 33, face='Jost-Bold')
        if note: fx.put(96 + fx.tw(w, 33, 'Jost-Bold') + 12, y + 31, note, 24, col=SOFT)
    fx.on(P, 950, 44, -.9)
    gs = Image.open(SHEET).convert('RGB').crop((1234, 240, 1555, 466)); ph2 = photo(gs, 268, 216); ph2.on(P, 1436, 40, -2.4); clip(P, 1456, 14, -8, 70)
    # how she was turned
    hp = Sheet(380, 150, fill=BLUSH); round_icon(hp, 'help', 20, 44, 62, fill=CORAL); hp.put(98, 42, 'Helped twice', 25, face='Jost-SemiBold'); hp.put(98, 94, '$40,000', 50, face='Jost-Bold', col=(0xb8, 0x3f, 0x2c, 255)); hp.on(P, 956, 300, 1.1)
    # what she'll do
    cl = Sheet(372, 160, fill=ENAMEL); cl.icon('call', 22, 30, 72); cl.put(112, 50, 'Call', 54, face='Jost-Bold', col=CREAM); cl.plate(216, 32, 'Door 2:54', 'p_time', h=38, size=18)
    cl.wrap(112, 98, 'Draws the guard off the door. Timing matters.', 246, size=21, col=DIM, lead=26); cl.on(P, 1336, 272, -1.0)
    # how far we trust her
    tr = Sheet(400, 276)
    for k, (w, v, n, st) in enumerate((('Loyal', 2, 3, 'trusting'), ('Afraid', 1, 3, 'uneasy'), ('Bitter', 0, 2, 'compliant'), ('Brave', 0, 3, 'informant'))):
        y = 34 + k * 44; tr.put(22, y, w, 26, face='Jost-Bold')
        for d in range(n):
            if d < v: tr.disc(112 + d * 26, y - 10, 20, fill=ENAMEL, ring=None)
            else: tr.disc(112 + d * 26, y - 10, 20, fill=None, ring=SOFT, rw=1.5)
        tr.put(206, y, st, 24, col=SOFT)
    tr.nine('plate_dark', 20, 208, 360, 50); tr.icon('talker', 34, 222, 22); tr.text(64, 232, 'Questioned, she keeps quiet', 20); tr.words_add('Questioned, she keeps quiet')
    tr.on(P, 948, 462, .6)
    # what she knows about us
    kn = Sheet(350, 214); kn.put(22, 34, 'She knows', 25, face='Jost-Bold', col=TEAL); kn.hair(22, 328, 56)
    kn.face('fixer', 22, 70, 58, rw=2); kn.put(92, 98, "The Fixer's face", 26, face='Jost-SemiBold')
    round_icon(kn, 'p_time', 22, 140, 58); kn.put(92, 168, 'Where and when', 26, face='Jost-SemiBold'); kn.on(P, 1364, 450, -1.4)
    clip(P, 1672, 432, -6, 64)
    # between us, the strip across the foot
    st = Sheet(770, 224, fill=(0xfa, 0xf6, 0xea, 255)); st.put(26, 34, 'Between us', 25, face='Jost-Bold', col=TEAL)
    xs = [86, 268, 450, 634]; st.line([(xs[0], 84), (xs[-1], 84)], col=INK, lw=2.5)
    for x, (t, who, ic, w1, last) in zip(xs, (('0:29', 'scout', 'watch', 'Watched', 0), ('0:59', 'scout', 'listen', 'Listened', 0), ('1:35', 'fixer', 'help', 'Not ready', 0), ('2:10', 'fixer', 'help', 'Turned', 1))):
        round_icon(st, ic, x - 28, 56, 56, fill=CORAL if last else ENAMEL); st.face(who, x + 12, 88, 30, rw=1.4)
        st.put(x, 148, w1, 25, face='Jost-Bold', anchor='mm', col=(0xb8, 0x3f, 0x2c, 255) if last else INK); st.put(x, 182, t, 23, anchor='mm', col=SOFT)
    st.on(P, 950, 780, -.5, lift=8)
    stamp(P, 346, 610, 'TURNED', size=74, deg=10)
    chrome_bits(P, '2:10')
    return P


if __name__ == '__main__':
    P = inez(); P.img.resize((1920, 1080), Image.LANCZOS).convert('RGB').save(OUT + 'proof-inez.png'); print('words', P.words)
