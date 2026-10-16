"""Glyph: briefs/014/glyph/eyes.png, the three eye states at actual size (2x pixels) on the run chip and on the order-card price row, before and after.
Needs out/glyph013/lib.py (scratch, reads the 011 kit). Run from the repo root after eyes.py: python3 -I briefs/014/glyph/scripts/eyes_sheet.py"""
import sys, os
sys.path.insert(0, '/Users/sky/Projects/suborned/out/glyph013'); os.chdir('/Users/sky/Projects/suborned/out/glyph013')
import lib
from lib import *
R = '/Users/sky/Projects/suborned'
NEW = R + '/briefs/014/glyph/glyphs/'; OLD = '/tmp/glyph-r2/eyeold/'
SRC = {'new': NEW, 'old': OLD, 'disc': NEW}
_icon = lib.icon
def icon_from(src):
    def f(name):
        p = SRC[src] + name + '@2x.png'
        if name.startswith('eye_') and os.path.exists(p): return Image.open(p).convert('RGBA')
        q = NEW + name + '@2x.png'
        if os.path.exists(q): return Image.open(q).convert('RGBA')
        return _icon(name)
    return f
RED = (224, 58, 47, 255); CREAM = C['cream']
def run_chip(cv, x, y, state, src, secs='3 s'):
    lib.icon = icon_from(src)
    w, h = 300, 68
    shadow(cv, (x, y, x + w, y + h), r=14, a=60, off=4)
    panel(cv, 'panel_strip', x, y, w, h)
    put_icon(cv, 'run', x + 22, y + 17, 34, CREAM)
    d = ImageDraw.Draw(cv); d.rounded_rectangle((x + 74, y + 24, x + 74 + 100, y + 44), radius=10, fill=(10, 24, 24, 255), outline=(120, 150, 145, 255), width=2)
    d.rounded_rectangle((x + 76, y + 26, x + 76 + 36, y + 42), radius=8, fill=(247, 244, 236, 255))
    text(cv, x + 186, y + 35, secs, 'd', 15, 'Bold', 'cream', 'lm')
    col = RED if state == 'open' else CREAM
    if src == 'old':   # as shipped: the chrome disc, glyph 14 px in an 18 px box
        im = spr('disc_dark_24').resize((52, 52), Image.LANCZOS); cv.paste(im, (int(x + 252), int(y + 8)), im)
        put_icon(cv, 'eye_' + state, x + 252 + 6, y + 8 + 6, 40, col)
    elif src == 'disc':  # redrawn glyph, 18 px, on a flat ink disc (no chrome rim, no cream ring), 24 px: enclosed for hud-budget, never a button
        d2 = ImageDraw.Draw(cv); cx, cy = x + 262, y + 34
        d2.ellipse((cx - 24, cy - 24, cx + 24, cy + 24), fill=C['ink-deep'])
        put_icon(cv, 'eye_' + state, cx - 18, cy - 18, 36, col)
    else:              # 18 px at 1x, bare on the chip
        put_icon(cv, 'eye_' + state, x + 240, y + 16, 36, col)
def price_row(cv, x, y, state, src):
    lib.icon = icon_from(src)
    items = [('p_time', '7 s'), ('p_trail', '+4'), ('eye_' + state, '')]
    # lib.chip colours every icon cream; draw the chip, then recolour the open eye
    right = chip(cv, x, y, items)
    return right
def build():
    W, H = 1760, 880
    cv = Image.new('RGBA', (W, H), (205, 187, 160, 255))
    d = ImageDraw.Draw(cv)
    text(cv, 20, 24, 'Eyes at actual size (2x pixels; the 18 px glyph is 36 here).  Columns: as shipped | redrawn, bare | redrawn on a flat disc.', 'b', 15, 'Medium', 'ink', 'lm')
    states = ['unk', 'open', 'shut']; names = {'unk': 'unknown', 'open': 'open (seen)', 'shut': 'shut (unseen)'}
    for r, st in enumerate(states):
        y = 70 + r * 100
        text(cv, 20, y + 34, names[st], 'b', 14, 'Medium', 'ink', 'lm')
        run_chip(cv, 180, y, st, 'old'); run_chip(cv, 510, y, st, 'new'); run_chip(cv, 840, y, st, 'disc')
    for r, st in enumerate(states):
        y = 420 + r * 130
        text(cv, 20, y + 70, names[st], 'b', 14, 'Medium', 'ink', 'lm')
        price_row(cv, 176, y + 100, st, 'old'); price_row(cv, 506, y + 100, st, 'new')
    # tooltip lines
    text(cv, 1180, 90, 'Hover tooltip', 'd', 15, 'Bold', 'ink', 'lm')
    for i, (a, b) in enumerate([('unknown', 'Unknown. Watch first.'), ('open', 'Seen by <watcher>.'), ('shut', 'Unseen.')]):
        text(cv, 1180, 130 + i * 40, a + ':  ' + b, 'b', 14, 'Medium', 'ink', 'lm')
    cv.convert('RGB').save(R + '/briefs/014/glyph/eyes.png'); print('ok', cv.size)
build()
