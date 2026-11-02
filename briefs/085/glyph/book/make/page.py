"""The book's paper: the briefing page's look (briefs/070, GDD §10) with the kit's plates, on the people-panel mock's primitives. Glyph, brief 085."""
import sys
sys.path.insert(0, '/Users/sky/Projects/suborned/briefs/085/glyph/people-panel/make')
from kit import *
from people import slot, pill
PAPER, PRINT, BAND = (228, 224, 211, 255), (247, 244, 236, 255), (12, 52, 52, 255)
SEA = (0x72, 0xb5, 0xa7, 255); TEAL = (31, 108, 103, 255); SOFT = (0x4e, 0x62, 0x5f, 255); SHEET = '/Users/sky/Projects/suborned/out/batch/shots-prev2/briefing-day.png'
BODY_PX = 21
FINE = {'scout': 'scout', 'fixer': 'fixer', 'tech': 'tech', 'enforcer': 'enforcer'}


class Page(Layer):
    words = 0
    def words_add(self, t): self.words += len([w for w in t.replace('·', ' ').split() if any(c.isalnum() for c in w)])

    def put(self, x, y, t, size=BODY_PX, col=INK, face='Jost-Medium', anchor='lm'):
        self.text(x, y, t, size, col=col, face=face, anchor=anchor); self.words_add(t)

    def head(self, x, y, t, col=TEAL, size=16.5):
        """Spaced capitals, as the briefing's section heads."""
        cx = x
        for ch in t.upper():
            self.text(cx, y, ch, size, col=col, face='Jost-SemiBold'); cx += self.tw(ch, size, 'Jost-SemiBold') + 3
        self.words_add(t); return cx

    def wrap(self, x, y, t, w, size=BODY_PX, col=INK, face='Jost-Medium', lead=None):
        lead = lead or size * 1.27; line, n = '', 0
        for word in t.split():
            trial = (line + ' ' + word).strip()
            if self.tw(trial, size, face) > w and line:
                self.text(x, y + n * lead, line, size, col=col, face=face); n += 1; line = word
            else: line = trial
        self.text(x, y + n * lead, line, size, col=col, face=face); self.words_add(t)
        return y + (n + 1) * lead

    def hair(self, x0, x1, y): self.line([(x0, y), (x1, y)], col=BRASS, lw=1)

    def picture(self, im, x, y, w, h, border=5):
        """A print: the cream border and soft edge of the briefing's stills."""
        self.rrect(x - border, y - border, w + 2 * border, h + 2 * border, 3, fill=PRINT, line=(0xd8, 0xd3, 0xc4, 255), lw=1)
        ph = im.convert('RGBA').resize((round(w * self.s), round(h * self.s)), Image.LANCZOS); self.paste(ph, x, y)

    def still(self, box): return Image.open(SHEET).convert('RGB').crop(box)

    def disc_icon(self, icon, x, y, d, shut=False):
        self.art('markers/disc_dark_30', x, y, d); self.icon('lock' if shut else icon, x + d * .2, y + d * .2, d * .6)

    def plate(self, x, y, t, icon=None, h=32, size=15.5, col=CREAM, face=None):
        w = 13 + (20 if icon else 0) + (26 if face else 0) + self.tw(t, size) + 13
        self.nine('plate_dark', x, y, w, h); cx = x + 12
        if face: self.face(face, cx - 2, y + 5, h - 10, rw=1.2); cx += 26
        if icon: self.icon(icon, cx, y + (h - 16) / 2, 16, col=col); cx += 20
        self.text(cx, y + h / 2 - .5, t, size, col=col); self.words_add(t); return w

    def chrome(self, tab, clock, cash, trail='0', back='Back to the street'):
        """Paper, seafoam band, the tabs, a brass hairline; the deep-teal foot with the stopped clock and the way back, as the briefing's Begin."""
        ImageDraw.Draw(self.img).rectangle((0, 0, self.w * self.s, self.h * self.s), fill=PAPER)
        ImageDraw.Draw(self.img).rectangle((0, 0, self.w * self.s, 7 * self.s), fill=SEA)
        cx = 56
        for name in ('Your cell', 'The people', 'The job', 'The journal'):
            on = name == tab; self.text(cx, 58, name, 34 if on else 25, col=INK if on else SOFT, face='Jost-Bold' if on else 'Jost-Medium'); self.words_add(name)
            w = self.tw(name, 34 if on else 25, 'Jost-Bold' if on else 'Jost-Medium')
            if on: ImageDraw.Draw(self.img).rectangle((cx * self.s, 80 * self.s, (cx + w) * self.s, 83 * self.s), fill=TEAL)
            cx += w + 42
        self.hair(56, 1864, 96)
        self.text(1864, 58, 'The Handover', 21, col=SOFT, anchor='rm', face='Jost-Medium')
        ImageDraw.Draw(self.img).rectangle((0, 1016 * self.s, self.w * self.s, 1080 * self.s), fill=BAND)
        px = self.plate(56, 1032, clock, 'pause', h=34, size=17) + 66
        px += self.plate(px, 1032, cash, 'p_cash', h=34, size=17) + 10
        self.plate(px, 1032, trail, 'p_trail', h=34, size=17)
        t = back.upper(); cx = 1864
        size = 19; w = sum(self.tw(c, size, 'Jost-SemiBold') + 4 for c in t); x0 = 1864 - w
        cx = x0
        for c in t: self.text(cx, 1048, c, size, col=CREAM, face='Jost-SemiBold'); cx += self.tw(c, size, 'Jost-SemiBold') + 4
        self.words_add(back)
        ImageDraw.Draw(self.img).rectangle((x0 * self.s, 1064 * self.s, 1864 * self.s, 1066 * self.s), fill=TURQ)
        g = ImageDraw.Draw(self.img); s = self.s; k = x0 - 22
        g.polygon([(k * s, 1042 * s), ((k + 7) * s, 1049 * s), (k * s, 1056 * s), ((k - 7) * s, 1049 * s)], fill=TURQ)
