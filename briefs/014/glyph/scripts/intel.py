"""Glyph: the Intel panel redesign mock (Sky, 2026-10-18: "this needs work"). Writes briefs/016/intel/intel.png (before | after | hover states | slot and chip states)
and briefs/016/intel/intel-on-shot.png (the panel on a real play shot). Needs out/glyph013/lib.py (reads the 011 kit). Run from the repo root: python3 -I briefs/014/glyph/scripts/intel.py
All numbers below are logical px (1x); the canvas is 2x, vector shapes are drawn at 4x and downsampled."""
import sys, os, math
sys.path.insert(0, '/Users/sky/Projects/suborned/out/glyph013'); os.chdir('/Users/sky/Projects/suborned/out/glyph013')
import lib
from lib import *
R = '/Users/sky/Projects/suborned'; KIT = R + '/briefs/011/ui/kit/'; G14 = R + '/briefs/014/glyph/glyphs/'
OUT = R + '/briefs/016/intel/'
CREAM = (247, 244, 236); TQ = (28, 185, 198); TQL = (96, 224, 234); INKD = (10, 38, 36); DIM = (150, 192, 186)
def gl(name):
    for d in (G14, KIT + 'icons/singles/'):
        p = d + name + '@2x.png'
        if os.path.exists(p): return Image.open(p).convert('RGBA')
    raise FileNotFoundError(name)
def ft(kind, size, w):
    p = f"{KIT}fonts/{'Jost' if kind == 'd' else 'Barlow'}-{w}.ttf"; return ImageFont.truetype(p, int(round(size * 2)))
PW, PAD, HEAD, ROW, GAP, FOOT = 280, 26, 40, 62, 6, 26
class Cv:
    def __init__(self, w, h):
        self.w, self.h = w, h; self.base = Image.new('RGBA', (w * 2, h * 2), (0, 0, 0, 0)); self.vec = Image.new('RGBA', (w * 4, h * 4), (0, 0, 0, 0)); self.top = Image.new('RGBA', (w * 2, h * 2), (0, 0, 0, 0))
    def flush_vec(self):
        v = self.vec.resize((self.w * 2, self.h * 2), Image.LANCZOS); self.base.alpha_composite(v); self.vec = Image.new('RGBA', (self.w * 4, self.h * 4), (0, 0, 0, 0))
    def v(self): return ImageDraw.Draw(self.vec)
    def disc(self, cx, cy, r, fill=None, outline=None, ow=1.2, dash=None):
        d = self.v(); k = 4
        if fill: d.ellipse(((cx - r) * k, (cy - r) * k, (cx + r) * k, (cy + r) * k), fill=fill)
        if outline and not dash: d.ellipse(((cx - r) * k, (cy - r) * k, (cx + r) * k, (cy + r) * k), outline=outline, width=int(ow * k))
        if outline and dash:
            n = 14
            for i in range(n):
                a0 = 360 * i / n; a1 = a0 + 360 / n * 0.55
                d.arc(((cx - r) * k, (cy - r) * k, (cx + r) * k, (cy + r) * k), a0, a1, fill=outline, width=int(ow * k))
    def rrect(self, x0, y0, x1, y1, r, fill=None, outline=None, ow=1.2, dash=False):
        d = self.v(); k = 4; box = (x0 * k, y0 * k, x1 * k, y1 * k)
        if fill: d.rounded_rectangle(box, radius=r * k, fill=fill)
        if outline and not dash: d.rounded_rectangle(box, radius=r * k, outline=outline, width=int(ow * k))
        if outline and dash:
            # dashed rounded rect: draw the outline, then cut gaps
            lay = Image.new('RGBA', self.vec.size, (0, 0, 0, 0)); ld = ImageDraw.Draw(lay); ld.rounded_rectangle(box, radius=r * k, outline=outline, width=int(ow * k))
            mask = Image.new('L', self.vec.size, 255); md = ImageDraw.Draw(mask)
            cx, cy = (x0 + x1) / 2 * k, (y0 + y1) / 2 * k; n = 16
            for i in range(n):
                a = 2 * math.pi * (i + 0.78) / n; md.line((cx, cy, cx + math.cos(a) * 200 * k, cy + math.sin(a) * 200 * k), fill=0, width=int(1.7 * k))
            lay.putalpha(ImageChops.multiply(lay.getchannel('A'), mask)); self.vec.alpha_composite(lay)
    def tri(self, pts, fill):
        self.v().polygon([(x * 4, y * 4) for x, y in pts], fill=fill)
    def icon(self, name, cx, cy, size, col=CREAM, alpha=255):
        im = tint(gl(name), col + (255,)).resize((int(size * 2), int(size * 2)), Image.LANCZOS)
        if alpha < 255: im.putalpha(im.getchannel('A').point(lambda v: v * alpha // 255))
        self.flush_vec(); self.base.alpha_composite(im, (int(cx * 2 - size), int(cy * 2 - size)))
    def img(self, im, cx, cy, size):
        im = im.resize((int(size * 2), int(size * 2)), Image.LANCZOS); self.flush_vec(); self.base.alpha_composite(im, (int(cx * 2 - size), int(cy * 2 - size)))
    def text(self, x, y, s, kind, size, w, col, anchor='ls'):
        self.flush_vec(); ImageDraw.Draw(self.base).text((x * 2, y * 2), s, font=ft(kind, size, w), fill=col + (255,), anchor=anchor)
    def tw(self, s, kind, size, w): return ft(kind, size, w).getlength(s) / 2
    def nine(self, name, x, y, w, h):
        self.flush_vec(); p = nine(name, w * 2, h * 2); self.base.alpha_composite(p, (int(x * 2), int(y * 2)))
ROWS = [
    dict(name='Inez', job='café waitress', npc='npc_waitress', need=('debt', 'L'), temp=None, ties=None, nxt=('listen', 'fixer', True), first=True),
    dict(name='The driver', job="Vale's driver", npc='npc_driver', need=('cash', 'L'), temp=('mercenary', 'T'), ties=None, nxt=('watch', 'scout', False)),
    dict(name='The guard', job='door guard', npc='npc_watcher', need=None, temp=('loyal', 'L'), ties=('secret', 'L'), nxt=('listen', 'fixer', False)),
]
def slot(c, kind, cx, cy, fact, hover=False):
    """kind need|temp|ties; fact None (unknown) or (glyph, 'L'|'T')"""
    w = 30 if kind == 'ties' else 24; h = 24
    x0, y0, x1, y1 = cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2
    shape = (lambda **kw: c.disc(cx, cy, 12, **kw)) if kind == 'need' else (lambda **kw: c.rrect(x0, y0, x1, y1, 6 if kind == 'temp' else 7, **kw))
    if fact is None:
        shape(fill=(14, 46, 44, 255), outline=CREAM + (120,), ow=1.2, dash=True)
        c.text(cx, cy + 4.4, '?', 'd', 12.5, 'Bold', (190, 214, 209), 'ms')
    else:
        g, st = fact
        shape(fill=INKD + (255,), outline=(CREAM + (235,)) if st == 'L' else (CREAM + (200,)), ow=1.3, dash=(st == 'T'))
        c.icon(g, cx, cy, 15, CREAM, 255 if st == 'L' else 200)
    if hover:
        if kind == 'need': c.disc(cx, cy, 14.4, outline=TQL + (255,), ow=1.4)
        else: c.rrect(x0 - 2.4, y0 - 2.4, x1 + 2.4, y1 + 2.4, 8, outline=TQL + (255,), ow=1.4)
def chip(c, x, y, verb, agent, acting):
    w, h = 56, 26
    c.rrect(x, y, x + w, y + h, 13, fill=(10, 38, 36, 255), outline=(TQL + (255,)) if acting else (CREAM + (170,)), ow=1.6 if acting else 1.2)
    cm = Image.open(KIT + f'portraits/cameo_{agent}_normal@2x.png').convert('RGBA'); m = Image.new('L', cm.size, 0); ImageDraw.Draw(m).ellipse((0, 0, cm.size[0] - 1, cm.size[1] - 1), fill=255); cm.putalpha(m)
    c.img(cm, x + 15, y + 13, 18)
    c.disc(x + 15, y + 13, 9.4, outline=CREAM + (200,), ow=0.9)
    c.icon(verb, x + 38, y + 13, 15, TQL if acting else CREAM)
def tip(c, cx, y, glyph, word):
    w = c.tw(word, 'd', 12, 'Bold') + 14 + 8 + 14 + 4
    x = cx - w / 2; c.rrect(x, y, x + w, y + 24, 8, fill=(8, 34, 32, 255), outline=CREAM + (230,), ow=1.3)
    c.icon(glyph, x + 15, y + 12, 13, TQL); c.text(x + 28, y + 16.5, word, 'd', 12, 'Bold', CREAM, 'ls')
def panel(hover=None):
    """hover = (row, slot) to ring a slot and show its tip; ('chip', row) for the chip"""
    H = HEAD + len(ROWS) * ROW + (len(ROWS) - 1) * GAP + FOOT + (34 if hover else 0)
    M = 18; c = Cv(PW + 2 * M, H + 2 * M)
    # drop shadow
    sh = Image.new('RGBA', c.base.size, (0, 0, 0, 0)); ImageDraw.Draw(sh).rounded_rectangle((M * 2, (M + 4) * 2, (M + PW) * 2, (M + H - (34 if hover else 0) + 4) * 2), radius=36, fill=(10, 30, 30, 90)); sh = sh.filter(ImageFilter.GaussianBlur(14)); c.base.alpha_composite(sh)
    ph = H - (34 if hover else 0)
    c.nine('panel_dark', M, M, PW, ph)
    c.text(M + PAD + 2, M + 25, 'INTEL', 'd', 11.5, 'Bold', (190, 222, 216), 'ls')
    c.icon('chev_up', M + PW - PAD - 8, M + 21, 12, (190, 222, 216))
    tipinfo = None
    for i, r in enumerate(ROWS):
        x, y = M + PAD, M + HEAD + i * (ROW + GAP); w = PW - 2 * PAD
        c.rrect(x, y, x + w, y + ROW, 9, fill=(32, 88, 84, 255) if r.get('first') else (22, 66, 63, 255), outline=(110, 175, 168, 110), ow=1.0)
        if r.get('first'): c.rrect(x, y + 10, x + 3.4, y + ROW - 10, 1.7, fill=TQ + (255,))
        npc = Image.open(KIT + f"badges/{r['npc']}@2x.png").convert('RGBA'); c.img(npc, x + 26, y + 31, 36)
        c.text(x + 52, y + 21, r['name'], 'd', 13.5, 'Bold', CREAM, 'ls')
        nx = x + 52 + c.tw(r['name'], 'd', 13.5, 'Bold')
        c.text(nx + 5, y + 21, '·  ' + r['job'], 'b', 12, 'Medium', DIM, 'ls')
        sy = y + 44
        slots = [('need', x + 52 + 12, r['need']), ('temp', x + 52 + 12 + 30, r['temp']), ('ties', x + 52 + 12 + 60 + 3, r['ties'])]
        for k, (kind, sx, fact) in enumerate(slots):
            hv = hover is not None and hover[0] == i and hover[1] == kind
            slot(c, kind, sx, sy, fact, hv)
            if hv: tipinfo = (sx, sy, fact, kind)
        if r['nxt']:
            verb, agent, acting = r['nxt']; chip(c, x + w - 10 - 56, sy - 13, verb, agent, acting)
            if hover is not None and hover[0] == 'chip' and hover[1] == i: tipinfo = (x + w - 10 - 28, sy, ('chip', verb), 'chip')
    if tipinfo:
        sx, sy, fact, kind = tipinfo
        if kind == 'chip': tip(c, sx, sy + 17, fact[1], 'Listen' if fact[1] == 'listen' else 'Watch')
        elif fact is None: tip(c, sx, sy + 17, {'need': 'listen', 'temp': 'listen', 'ties': 'listen'}[kind], 'Listen')
        else: tip(c, sx, sy + 17, fact[0], {'debt': 'Debt', 'cash': 'Cash', 'mercenary': 'For hire', 'loyal': 'Loyal', 'secret': 'Her guard'}[fact[0]])
    c.flush_vec(); return c.base.resize((c.w * 2, c.h * 2)), (M, PW, ph)
def build():
    os.makedirs(OUT, exist_ok=True)
    a, _ = panel(); b, _ = panel((0, 'temp')); b2, _ = panel((1, 'need')); b3, _ = panel(('chip', 1))
    before = Image.open('/Users/sky/.t3/userdata/attachments/6f2fb9e2-3d6f-4cb6-bf62-66a9d065638d-f50e8299-6928-4a94-911c-9e2156ccc8f2.png').convert('RGBA')
    W = 3 * b.width + 160; y2 = max(before.height, a.height) + 120
    sheet = Image.new('RGBA', (W, y2 + b.height + 260), (36, 52, 50, 255)); d = ImageDraw.Draw(sheet)
    f = ImageFont.truetype(KIT + 'fonts/Jost-Bold.ttf', 30); fb = ImageFont.truetype(KIT + 'fonts/Barlow-Medium.ttf', 24)
    d.text((40, 20), 'Before', font=f, fill=CREAM + (255,)); sheet.alpha_composite(before, (40, 70))
    d.text((120 + before.width, 20), 'After (2x)', font=f, fill=CREAM + (255,)); sheet.alpha_composite(a, (100 + before.width, 50))
    d.text((60, y2 + b.height + 80), 'One card per person, in the order the plan needs them.\nSlots, left to right: need (round), temperament (square), ties or secret (tab). Solid rim = learned. Dashed rim = told. Dashed with ? = not known yet.\nChip = who acts (their cameo) and the verb. Turquoise: that agent is selected, a click gives the order. Cream: a click selects that agent.\nThe turquoise bar on the first card marks the person the plan needs next.', font=fb, fill=(190, 222, 216, 255), spacing=10)
    d.text((40, y2), 'Hover: an unknown slot says how to learn it | a learned slot gives its fragment | the chip names the verb', font=f, fill=CREAM + (255,))
    for k, im in enumerate((b, b2, b3)): sheet.alpha_composite(im, (40 + k * (b.width + 40), y2 + 50))
    sheet.convert('RGB').save(OUT + 'intel.png'); print('sheet', sheet.size)
    shot = Image.open(R + '/briefs/016/a-030.png').convert('RGBA'); s2 = shot.resize((shot.width * 2, shot.height * 2), Image.LANCZOS)
    pa, (M, PWd, ph) = panel(); px = (shot.width - 14 - PW - M) * 2; py = (108 - M) * 2
    s2.alpha_composite(pa, (px, py)); out = s2.resize(shot.size, Image.LANCZOS); out.convert('RGB').save(OUT + 'intel-on-shot.png')
    s2.crop((px - 80, py - 80, px + pa.width + 80, py + pa.height + 80)).convert('RGB').save(OUT + 'intel-on-shot-2x.png'); print('shot ok')
if __name__ == '__main__': build()
