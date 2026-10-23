"""Front-end mockups for Latch (brief 051, Glyph). Reads ../../047/layout/loading.json (picture, title, crop maths) and frontend.json (this folder),
the kit sprites in unity/Assets/Suborned/UI/Resources/Kit, and draws every screen exactly as SPEC.md describes.
   python3 -I fe.py            # all screens at 1920x1080 and 2880x1864 into this folder
   python3 -I fe.py menu 1920 1080 out.png
Nothing here is shipped; it is the reference for the numbers."""
import json, os, sys, math
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '../../047/layout'))
import mock
from mock import hexrgb, font, tracked, tracked_w, base_canvas, title
from PIL import Image, ImageDraw, ImageFilter
import numpy as np

ROOT = mock.ROOT
KIT = ROOT + '/unity/Assets/Suborned/UI/Resources/Kit/'
LOAD = json.load(open(os.path.join(HERE, '../../047/layout/loading.json')))
FE = json.load(open(os.path.join(HERE, 'frontend.json')))
PIC = ROOT + '/briefs/047/fix/B_after.png'
GAME = ROOT + '/out/batch/shots/cafe-front-day.png'
C = {k: hexrgb(v) for k, v in FE['colors'].items()}
_cache = {}


def spr(name):
    if name not in _cache: _cache[name] = Image.open(KIT + name + '@2x.png').convert('RGBA')
    return _cache[name]


class Ctx:
    def __init__(self, img, g, W, H):
        self.img, self.g, self.W, self.H = img, g, W, H; self.k = H / FE['refHeight']
        self.d = ImageDraw.Draw(img, 'RGBA')
    def R(self, v): return v * self.k


def col(name, a=1.0):
    c = C[name] if name in C else hexrgb(name)
    return c + (round(255 * a),)


def tint(mask, color):
    out = Image.new('RGBA', mask.size, color[:3] + (0,)); a = mask if len(color) < 4 else mask.point(lambda v: v * color[3] // 255)
    out.putalpha(a); return out


def rrect(c, box, r, fill=None, fa=1.0, line=None, la=1.0, lw=0.0):
    """Anti-aliased rounded rectangle (4x supersample) blended on the canvas."""
    SS = 4; x0, y0, x1, y1 = box; ix, iy = int(math.floor(x0)) - 2, int(math.floor(y0)) - 2
    w, h = int(math.ceil(x1)) + 2 - ix, int(math.ceil(y1)) + 2 - iy
    def mask(inset):
        m = Image.new('L', (w * SS, h * SS), 0); md = ImageDraw.Draw(m)
        md.rounded_rectangle([(x0 - ix + inset) * SS, (y0 - iy + inset) * SS, (x1 - ix - inset) * SS - 1, (y1 - iy - inset) * SS - 1], max(0, (r - inset)) * SS, fill=255)
        return m.resize((w, h), Image.LANCZOS)
    if fill is not None:
        c.img.alpha_composite(tint(mask(0), col(fill, fa) if isinstance(fill, str) else fill + (round(255 * fa),)), (ix, iy))
    if line is not None and lw > 0:
        outer, inner = np.asarray(mask(0)).astype(np.int16), np.asarray(mask(lw)).astype(np.int16)
        ring = Image.fromarray(np.clip(outer - inner, 0, 255).astype(np.uint8))
        c.img.alpha_composite(tint(ring, col(line, la) if isinstance(line, str) else line + (round(255 * la),)), (ix, iy))


def circle(c, cx, cy, r, fill, fa=1.0):
    rrect(c, (cx - r, cy - r, cx + r, cy + r), r, fill=fill, fa=fa)


def nine(c, name, sl2x, x, y, w, h, s):
    """Nine-slice the 2x sprite into (x, y, w, h) device px; s = device px per 2x sprite px (the slices keep their proportions)."""
    im = spr(name); sw, sh = im.size; x, y, w, h = round(x), round(y), round(w), round(h)
    l = t = rr = b = sl2x if isinstance(sl2x, int) else None
    if isinstance(sl2x, (list, tuple)): l, t, rr, b = sl2x
    dl, dt, dr, db = [max(1, round(v * s)) for v in (l, t, rr, b)]
    dl = min(dl, w // 2); dr = min(dr, w - dl); dt = min(dt, h // 2); db = min(db, h - dt)
    sx = [0, l, sw - rr, sw]; sy = [0, t, sh - b, sh]; dx = [0, dl, w - dr, w]; dy = [0, dt, h - db, h]
    out = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    for j in range(3):
        for i in range(3):
            dw_, dh_ = dx[i + 1] - dx[i], dy[j + 1] - dy[j]
            if dw_ <= 0 or dh_ <= 0: continue
            piece = im.crop((sx[i], sy[j], sx[i + 1], sy[j + 1])).resize((dw_, dh_), Image.LANCZOS)
            out.alpha_composite(piece, (dx[i], dy[j]))
    c.img.alpha_composite(out, (x, y)); return out


def icon(c, name, cx, cy, size, color, rot=0, a=1.0):
    m = spr('icons/singles/' + name).split()[3].resize((round(size), round(size)), Image.LANCZOS)
    if rot: m = m.rotate(rot, resample=Image.BICUBIC, expand=False)
    c.img.alpha_composite(tint(m, col(color, a) if isinstance(color, str) else color), (round(cx - size / 2), round(cy - size / 2)))


def sprite_at(c, name, cx, cy, size, a=1.0):
    im = spr(name).resize((round(size), round(size)), Image.LANCZOS)
    if a < 1: im.putalpha(im.split()[3].point(lambda v: int(v * a)))
    c.img.alpha_composite(im, (round(cx - size / 2), round(cy - size / 2)))


def draw_ss(c, x, base, s, fname, px, track_px, color, anchor='l'):
    """Text at 4x on a mask, then down: exact sub-pixel letter spacing and kerning (PIL rounds each glyph's x otherwise)."""
    SS = 4; f = font(fname, px * SS); tr = track_px * SS
    if tr == 0:
        w = f.getlength(s)
    else:
        w = tracked_w(s, f, tr)
    asc = int(px * 1.1); desc = int(px * 0.4); pad = int(px * 0.3)
    X = x if anchor == 'l' else x - w / SS if anchor == 'r' else x - w / SS / 2
    ix = int(math.floor(X)) - pad; iy = int(round(base)) - asc
    W_ = int(w / SS) + 2 * pad + 2; H_ = asc + desc
    m = Image.new('L', (W_ * SS, H_ * SS), 0); md = ImageDraw.Draw(m); ox = (X - ix) * SS; oy = (base - iy) * SS
    if tr == 0: md.text((ox, oy), s, font=f, fill=255, anchor='ls')
    else: tracked(md, (ox, oy), s, f, 255, tr, 'ls')
    m = m.resize((W_, H_), Image.LANCZOS)
    c.img.alpha_composite(tint(m, color), (ix, iy)); return w / SS


def text(c, x, cy, s, style, anchor='l', color=None, a=None):
    st = FE['type'][style] if isinstance(style, str) else style
    px = st['sizeRef'] * c.k; tr = st.get('trackingEm', 0) * px
    if st.get('uppercase'): s = s.upper()
    colr = col(color or st['color'], a if a is not None else 1.0)
    return draw_ss(c, x, cy + px * 0.36, s, st['font'], px, tr, colr, anchor)


def textw(c, s, style):
    st = FE['type'][style]; px = st['sizeRef'] * c.k; f = font(st['font'], px * 4); tr = st.get('trackingEm', 0) * px * 4
    s = s.upper() if st.get('uppercase') else s
    return (tracked_w(s, f, tr) if tr else f.getlength(s)) / 4


# ---------- parts ----------
def button(c, x, y, w, h, label, state='normal', style='button'):
    """The kit's cream text button (nine-slice, height-driven). state: normal | hover | focus | disabled."""
    B = FE['button']; name = B['sprites']['hover' if state in ('hover', 'focus') else 'disabled' if state == 'disabled' else 'normal']
    s = h / spr(name).height
    if state == 'focus':
        ring = max(1, round(c.R(B['focus']['ringRef']))); gl = round(c.R(B['focus']['glowRef'])); pad = ring + gl * 2
        lay = Image.new('RGBA', (round(w) + 2 * pad, round(h) + 2 * pad), (0, 0, 0, 0))
        tmp = Ctx(lay, None, lay.width, lay.height); tmp.k = c.k; nine(tmp, name, B['slice2x'], pad, pad, w, h, s)
        al = lay.split()[3]; dil = al.filter(ImageFilter.MaxFilter(2 * ring + 1)); glow = dil.filter(ImageFilter.GaussianBlur(gl)).point(lambda v: int(v * B['focus']['glowAlpha']))
        live = col(B['focus']['color'])[:3]
        c.img.alpha_composite(tint(glow, live), (round(x) - pad, round(y) - pad)); c.img.alpha_composite(tint(dil, live), (round(x) - pad, round(y) - pad))
    nine(c, name, B['slice2x'], x, y, w, h, s)
    text(c, x + w / 2, y + h / 2 - c.R(0.5), label, style, 'c', a=B['disabledLabelAlpha'] if state == 'disabled' else None)


def plate(c, x, y, w, h, label, active, style='tab'):
    T = FE['panel']['tabSprites']; name = T['active' if active else 'idle']
    nine(c, name, T['slice2x'], x, y, w, h, h / spr(name).height)
    st = dict(FE['type'][style]); text(c, x + w / 2, y + h / 2, label, st, 'c', color=st['activeColor'] if active else None)


def keycap(c, cx_right, cy, label, kind='primary', ring=False, badge=None):
    K = FE['controls']['keycap']; h = c.R(K['h'] if kind == 'primary' else K['altH']); name = K['sprite'] if kind == 'primary' else K['alt']
    st = dict(FE['type']['keycap']);
    if kind != 'primary': st['sizeRef'] = 12.5; st['color'] = K['altTextColor']
    tw = textw(c, label, 'keycap') * (st['sizeRef'] / FE['type']['keycap']['sizeRef']); w = max(c.R(K['minW'] if kind == 'primary' else 40), tw + 2 * c.R(K['padX']))
    x = cx_right - w; y = cy - h / 2
    if ring:
        rrect(c, (x - c.R(3), y - c.R(3), x + w + c.R(3), y + h + c.R(3)), c.R(8), fill=None, line='live', la=1, lw=c.R(K['listening']['ringRef']))
        nine(c, K['alt'], K['slice2x'], x, y, w, h, h / spr(K['alt']).height); text(c, x + w / 2, cy, label, dict(st, color='textDim', sizeRef=13), 'c')
    else:
        nine(c, name, K['slice2x'], x, y, w, h, h / spr(name).height); text(c, x + w / 2, cy - c.R(0.5), label, st, 'c')
    if badge: sprite_at(c, badge, x + w - c.R(1), y + c.R(1), c.R(FE['controlsPage']['clashStrip']['keyBadgeRef']))
    return x


def slider(c, xr, cy, v, label):
    S = FE['controls']['slider']; w = c.R(S['width']); x1 = xr - c.R(S['valueW']); x0 = x1 - w; th = c.R(S['trackH'])
    rrect(c, (x0, cy - th / 2, x1, cy + th / 2), th / 2, fill=S['trackColor'], fa=S['trackAlpha'], line=S['rim'], la=S['rimAlpha'], lw=max(1, c.R(1)))
    fx = x0 + (x1 - x0) * v; rrect(c, (x0, cy - th / 2, fx, cy + th / 2), th / 2, fill=S['fill'])
    sprite_at(c, S['knob'], fx, cy, c.R(S['knobRef'])); text(c, xr, cy, label, 'rowValue', 'r')


def switch(c, xr, cy, on, word=None):
    S = FE['controls']['switch']; wd = c.R(S['wordW']); x1 = xr - wd; w, h = c.R(S['w']), c.R(S['h']); x0 = x1 - w
    rrect(c, (x0, cy - h / 2, x1, cy + h / 2), h / 2, fill=S['onFill'] if on else S['offFill'], fa=S['onFillAlpha'] if on else S['offFillAlpha'], line=S['rim'], la=S['rimAlpha'], lw=max(1, c.R(1.5)))
    kr = c.R(S['knobRef']); kx = x1 - h / 2 if on else x0 + h / 2
    sprite_at(c, S['knob'], kx, cy, kr, a=1.0 if on else 0.85); text(c, xr, cy, word or ('On' if on else 'Off'), 'rowValue', 'r', color=None if on else 'textDim')


def choice(c, xr, cy, value, dim=False):
    S = FE['controls']['choice']; w = c.R(S['w']); x0 = xr - w; cr = c.R(S['chevRef'])
    a = 0.45 if dim else 1.0
    tw = textw(c, value, 'rowValue'); mid = x0 + w / 2; gap = c.R(S['chevGapRef'])
    icon(c, 'chev_down', mid - tw / 2 - gap - cr / 2, cy, cr, 'cream' if not dim else 'textDim', rot=-S['chevRotate'][0], a=a)   # USS rotate(90deg) clockwise: down -> left
    icon(c, 'chev_down', mid + tw / 2 + gap + cr / 2, cy, cr, 'cream' if not dim else 'textDim', rot=-S['chevRotate'][1], a=a)
    text(c, mid, cy, value, 'rowValue', 'c', color='textDim' if dim else None)


# ---------- screens ----------
def build_line(c, dark_bg=True):
    B = LOAD['build']; R = c.R
    c.d.text((c.W - R(B['rightRef']), c.H - R(B['bottomRef']) + R(B['sizeRef']) + R(2) - R(14)), 'Build 0.2.80 · c023 · sim 0.8.2 · 8bf8ce6', font=font(B['font'], R(B['sizeRef'])),
             fill=hexrgb(B['color']) + (round(255 * B['alpha']),), anchor='rs')


def put_title(c):
    ov = Image.new('RGBA', (c.W, c.H), (0, 0, 0, 0)); title(LOAD, ov, c.W, c.H, c.g); c.img.alpha_composite(ov)


def new(W, H, scrims, pic=PIC, focus=None):
    cfg = LOAD if focus is None else dict(LOAD, image=dict(LOAD['image'], focus=focus))
    img, g = base_canvas(cfg, pic, W, H, scrims=scrims); return Ctx(img, g, W, H)


def screen_menu(W, H, saved=True, focus=0, hover=None):
    c = new(W, H, FE['scrims']['menu']); put_title(c); R = c.R; M = FE['menu']
    items = [('continue', 'Continue')] if saved else []
    items += [('new', 'New game'), ('options', 'Options'), ('controls', 'Controls'), ('quit', 'Quit')]
    n = len(items); bh, gap = R(M['heightRef']), R(M['gapRef']); total = n * bh + (n - 1) * gap
    y0 = c.H - R(M['bottomRef']) - total
    for i, (key, lab) in enumerate(items):
        state = 'focus' if i == focus else 'hover' if i == hover else 'normal'
        button(c, R(M['leftRef']), y0 + i * (bh + gap), R(M['widthRef']), bh, lab, state, 'menuButton')
    build_line(c); return c.img


def panel_frame(c, title_text, tabs, active):
    P = FE['panel']; R = c.R; pw, ph = R(P['widthRef']), min(R(P['heightRef']), c.H - 2 * R(48))
    x, y = (c.W - pw) / 2, (c.H - ph) / 2
    nine(c, P['sprite'], P['slice2x'], x, y, pw, ph, c.k / 2)
    pad = P['padRef']; ix0, ix1 = x + R(pad[0]), x + pw - R(pad[2])
    text(c, ix0, y + R(P['titleY']), title_text, 'panelTitle', 'l')
    cs = P['closeInset']; sprite_at(c, 'buttons/btn_close_normal', x + pw - R(cs) - R(P['closeRef']) / 2 + R(6), y + R(P['titleY']) , R(P['closeRef']))
    tx = ix0
    for i, t in enumerate(tabs):
        w = textw(c, t, 'tab') + 2 * R(P['tabPadX']); plate(c, tx, y + R(P['tabsY']), w, R(P['tabH']), t, i == active); tx += w + R(P['tabGap'])
    line_y = y + R(P['bodyTop'] - 12)
    c.d.line([(ix0, line_y), (ix1, line_y)], fill=col('panelLine', 0.22), width=max(1, round(R(1))))
    return x, y, pw, ph, ix0, ix1


def footer(c, x, y, pw, ph, ix0, ix1, reset_label='Reset page'):
    F = FE['panel']['footer']; R = c.R; fy = y + ph - R(F['lineY'])
    c.d.line([(ix0, fy), (ix1, fy)], fill=col('panelLine', 0.22), width=max(1, round(R(1))))
    by = y + ph - R(F['bottomInset']) - R(F['backH'])
    button(c, ix1 - R(F['backW']), by, R(F['backW']), R(F['backH']), 'Back', 'normal')
    rh = R(F['resetH']); ry = by + (R(F['backH']) - rh) / 2
    plate(c, ix0, ry, R(F['resetW']), rh, reset_label, False, 'tab')


def rows(c, x, y, ix0, ix1, items, focus=None):
    P = FE['panel']; R = c.R; cy = y + R(P['bodyTop']); xr = ix1 - R(P['controlRight'])
    for n, it in enumerate(items):
        if it[0] == 'group':
            cy += R(P['groupH']) * (0.62 if n == 0 else 1.0) if False else 0
            gy = cy + R(P['groupH']) / 2 + (R(4) if n else R(-2)); text(c, ix0 + R(P['labelX']), gy, it[1], 'group', 'l'); cy += R(P['groupH']) - (R(8) if n == 0 else 0); continue
        kind, label, val = it[0], it[1], it[2]; extra = it[3] if len(it) > 3 else {}
        rh = R(P['rowH']); rc = cy + rh / 2
        if focus == label:
            rrect(c, (ix0, cy + R(2), ix1, cy + rh - R(2)), R(P['rowFocus']['radiusRef']), fill='inkDeep', fa=P['rowFocus']['alpha'])
            rrect(c, (ix0, cy + R(8), ix0 + R(P['rowFocus']['tickRef']), cy + rh - R(8)), R(1.5), fill='live')
        dim = extra.get('dim', False)
        text(c, ix0 + R(P['labelX'])        , rc, label, 'rowLabel', 'l', color='textDim' if dim else None)
        if kind == 'slider': slider(c, xr, rc, val[0], val[1])
        elif kind == 'switch': switch(c, xr, rc, val)
        elif kind == 'choice': choice(c, xr, rc, val, dim)
        if n < len(items) - 1: c.d.line([(ix0, cy + rh), (ix1, cy + rh)], fill=col('panelLine', 0.14), width=max(1, round(R(1))))
        cy += rh


OPTIONS = {
    'Display': [
        ('choice', 'Window mode', 'Borderless'), ('choice', 'Resolution', '2880 x 1864'), ('slider', 'Render scale', (1.0, '100%')),
        ('choice', 'Quality', 'Balanced'), ('switch', 'V-sync', True), ('choice', 'Frame cap', '60', {'dim': True}), ('slider', 'HUD scale', (0.3, '100%')),
    ],
    'Audio': [
        ('slider', 'Master', (0.8, '80')), ('slider', 'Music', (0.55, '55')), ('slider', 'Effects', (0.8, '80')), ('slider', 'Voices', (0.9, '90')),
    ],
    'Camera': [
        ('group', 'Commander view'), ('slider', 'Pan speed', (0.5, '18 m/s')), ('switch', 'Edge pan', False), ('slider', 'Orbit sensitivity', (0.45, '1.0')),
        ('switch', 'Invert orbit', False), ('switch', 'Trackpad drag orbit', False),
        ('group', 'Third person'), ('choice', 'Camera distance', 'Default'), ('switch', 'Camera settle', True),
    ],
    'Gameplay': [
        ('group', 'Orders'), ('choice', 'Click scheme', 'Click walks, hold acts'), ('choice', 'Orders while paused', 'Start together'), ('switch', 'Hints', True),
        ('choice', 'Default game speed', '×1'), ('choice', 'Conversations', 'First time'),
        ('group', 'Fights'), ('choice', 'Agents near you', 'Fire with you'),
        ('group', 'Playground'), ('choice', 'Third person: right button', 'Right hold fires'), ('choice', 'Third person: aim', 'Hold'),
    ],
}
FOCUS = {'Display': 'Render scale', 'Audio': 'Music', 'Camera': 'Edge pan', 'Gameplay': 'Hints'}


def screen_options(W, H, page='Display'):
    c = new(W, H, FE['scrims']['panel']); tabs = list(OPTIONS)
    x, y, pw, ph, ix0, ix1 = panel_frame(c, 'Options', tabs, tabs.index(page))
    rows(c, x, y, ix0, ix1, OPTIONS[page], FOCUS[page]); footer(c, x, y, pw, ph, ix0, ix1); return c.img


CONTROLS = {
    'Command': ([('Select agent 1', '1', 'F1'), ('Select agent 2', '2', 'F2'), ('Select agent 3', '3', 'F3'), ('Select agent 4', '4', 'F4'), ('Select all', '5', None),
                 ('Next agent', 'Tab', None), ('Add to group', 'Shift', None), ('Pick action 1', 'Shift 1', None), ('Pick action 2', 'Shift 2', None),
                 ('Pick action 3', 'Shift 3', None), ('Pick action 4', 'Shift 4', None), ('Cancel', 'C', None), ('Walk or run', 'G', None)],
                [('Pause', 'Space', None), ('Faster', '=', None), ('Slower', '-', None), ('Next event', 'N', None), ('Briefing', 'B', None), ('History', 'L', None),
                 ('Intel', 'I', None), ('Response list', 'R', None), ('Hints', 'H', None), ('Acknowledge', 'K', None), ('Detail (hold)', 'Alt', None),
                 ('All strips', '`', None), ('Play again', 'Return', None)]),
    'Camera': ([('Pan up', 'W', 'Up'), ('Pan down', 'S', 'Down'), ('Pan left', 'A', 'Left'), ('Pan right', 'D', 'Right')],
               [('Turn left', 'Q', None), ('Turn right', 'E', None), ('Free turn (hold)', 'Alt', None), ('Follow view', 'T', None)]),
    'Fights': ([('Draw or holster', 'F', None), ('Cease fire', 'X', None), ('Stance', 'V', None)], [('Force fire (hold)', 'Ctrl', None), ('Target mode', 'Y', None), ('Step target', 'Z', None)]),
    'Third person': ([('Act', 'E', None)], []),
}


def screen_controls(W, H, page='Command', listen=None, clash=None):
    c = new(W, H, FE['scrims']['panel']); R = c.R; tabs = list(CONTROLS); CP = FE['controlsPage']
    x, y, pw, ph, ix0, ix1 = panel_frame(c, 'Controls', tabs, tabs.index(page))
    gap = R(CP['columnGapRef']); cw = (ix1 - ix0 - gap) / 2; top = y + R(FE['panel']['bodyTop'] - 4)
    for ci, colrows in enumerate(CONTROLS[page]):
        cx0 = ix0 + ci * (cw + gap); cx1 = cx0 + cw
        for ri, (lab, key, alt) in enumerate(colrows):
            ry = top + ri * R(CP['rowH']); rc = ry + R(CP['rowH']) / 2; hot = listen == lab; bad = clash and lab in clash['rows']
            if hot: rrect(c, (cx0 - R(4), ry + R(2), cx1 + R(4), ry + R(CP['rowH']) - R(2)), R(6), fill='inkDeep', fa=FE['panel']['rowFocus']['alpha'])
            text(c, cx0 + R(CP['labelX']), rc, lab, 'rowLabel', 'l')
            shown = clash['key'] if clash and lab == clash['target'] else key
            kx = keycap(c, cx1 - R(6), rc, 'Press a key' if (hot and not clash) else shown, ring=hot and not clash, badge='badges/mark_half' if bad else None)
            if alt and not hot: keycap(c, kx - R(8), rc, alt, 'alt')
            c.d.line([(cx0, ry + R(CP['rowH'])), (cx1, ry + R(CP['rowH']))], fill=col('panelLine', 0.12), width=max(1, round(R(1))))
    if clash:
        S = CP['clashStrip']; sh = R(S['h']); sy = y + ph - R(FE['panel']['footer']['lineY']) - sh - R(16); sw = ix1 - ix0
        nine(c, S['sprite'], S['slice2x'], ix0, sy, sw, sh, sh / spr(S['sprite']).height)
        sprite_at(c, S['mark'], ix0 + R(40), sy + sh / 2, R(S['markRef']))
        text(c, ix0 + R(64), sy + sh / 2, clash['text'], 'rowLabel', 'l')
        vx = ix1 - R(30)
        for v in reversed(S['verbs']):
            w = textw(c, v, 'tab') + 2 * R(22); plate(c, vx - w, sy + (sh - R(30)) / 2, w, R(30), v, v == 'Swap'); vx -= w + R(10)
    footer(c, x, y, pw, ph, ix0, ix1, 'Reset all'); return c.img


BRIEF = {
    'title': 'The Handover', 'place': 'Civic Records Office, Harbour District', 'time': '17:24 · shift change',
    'paragraphs': [
        "At the end of the day Councillor Vale signs the clearance order for this block. Until it is stamped the block stays open. Once it is, the block is sealed and everyone inside it is counted, one by one, onto paper.",
        "You will take him alive. A dead councillor brings an inquest. A living one brings a favour, and the city has never refused a favour to a man it believes it still owns.",
        "Every door here has a person behind it. The waitress at the corner café knows the guard's habits; the driver knows Vale's. The cameras watch, but only people read what they see. Find who is tired, in debt or afraid, and ask them for something small.",
        "The surface is bright and the city smiles. Under it, everything is written down, and someone is always counting. Leave nothing they can count.",
    ]}


def wrap(s, f, width):
    words, lines, cur = s.split(), [], ''
    for w in words:
        t = (cur + ' ' + w).strip()
        if f.getlength(t) <= width: cur = t
        else: lines.append(cur); cur = w
    return lines + [cur]


def screen_brief(W, H):
    c = new(W, H, FE['scrims']['brief'], focus=FE['brief']['imageFocus']); R = c.R; B = FE['brief']['column']; T = FE['type']
    cw = R(B['widthRef']); x0 = W - R(B['rightRef']) - cw; y = R(B['topRef'])
    text(c, x0, y + R(21), BRIEF['title'], 'briefTitle', 'l')
    y += R(42 * 0.72) + R(34)
    rule = FE['brief']['headRule']; y += R(rule['gapAboveRef'] - 14)
    c.d.rectangle([x0, y, x0 + cw, y + max(1, R(rule['heightRef']))], fill=col(rule['color']))
    y += R(rule['gapBelowRef'])
    for head, val in (('Place', BRIEF['place']), ('Time', BRIEF['time'])):
        st = T['briefHead']; hw = text(c, x0, y + R(8), head, 'briefHead', 'l'); text(c, x0 + R(76), y + R(8), val, dict(st, color=st['valueColor'], uppercase=False, trackingEm=0.04, sizeRef=15.5), 'l'); y += R(26)
    y += R(22); bb = T['briefBody']; f = font(bb['font'], R(bb['sizeRef'])); words = 0
    for p in BRIEF['paragraphs']:
        words += len(p.split())
        for ln in wrap(p, f, cw):
            draw_ss(c, x0, y + R(bb['sizeRef']) * 0.9, ln, bb['font'], R(bb['sizeRef']), 0, col(bb['color'])); y += R(bb['lineRef'])
        y += R(bb['paragraphGapRef'])
    bw, bh = R(FE['brief']['button']['w']), R(FE['brief']['button']['h'])
    button(c, x0 + cw - bw, H - R(B['bottomRef']) - bh, bw, bh, FE['brief']['button']['label'], 'focus')
    c.words, c.textBottom = words, y; screen_brief.last = (words, y / c.k)
    return c.img


def screen_pause(W, H, focus=0):
    img, g = base_canvas(LOAD, GAME, W, H, scrims=[]); c = Ctx(img, g, W, H); R = c.R; P = FE['pause']
    labels = ['Resume', 'Options', 'Controls', 'Restart mission', 'Fight range', 'Quit to menu', 'Quit']
    pw = R(P['widthRef']); pad = P['padRef']; bh, gap = R(P['buttonH']), R(P['gapRef'])
    title_h = R(26 * 0.72); ph = R(pad[1]) + title_h + R(P['titleGapRef']) + len(labels) * bh + (len(labels) - 1) * gap + R(22) + R(14) + R(pad[3])
    x, y = (W - pw) / 2, H * P['topFrac']
    nine(c, P['panel'], 44, x, y, pw, ph, c.k / 2)
    text(c, x + pw / 2, y + R(pad[1]) + title_h / 2, 'Paused', 'panelTitle', 'c')
    by = y + R(pad[1]) + title_h + R(P['titleGapRef'])
    for i, l in enumerate(labels): button(c, x + (pw - R(P['buttonW'])) / 2, by + i * (bh + gap), R(P['buttonW']), bh, l, 'focus' if i == focus else 'normal');
    by_end = by + len(labels) * bh + (len(labels) - 1) * gap
    c.d.text((x + pw / 2, by_end + R(22)), 'Build 0.2.80 · c023 · sim 0.8.2 · 8bf8ce6', font=font('Jost-Regular-Tab', R(11.5)), fill=col('textDim', 0.85), anchor='ms')
    return c.img


SCREENS = {
    'menu': lambda W, H: screen_menu(W, H), 'menu-nosave': lambda W, H: screen_menu(W, H, saved=False),
    'options-display': lambda W, H: screen_options(W, H, 'Display'), 'options-audio': lambda W, H: screen_options(W, H, 'Audio'),
    'options-camera': lambda W, H: screen_options(W, H, 'Camera'), 'options-gameplay': lambda W, H: screen_options(W, H, 'Gameplay'),
    'controls-command': lambda W, H: screen_controls(W, H, 'Command', listen='Cancel'),
    'controls-clash': lambda W, H: screen_controls(W, H, 'Camera', listen='Follow view', clash=dict(key='E', target='Follow view', rows=('Follow view', 'Turn right'), text='E already turns the view right')),
    'brief': lambda W, H: screen_brief(W, H), 'pause': lambda W, H: screen_pause(W, H),
}

if __name__ == '__main__':
    if len(sys.argv) > 1:
        SCREENS[sys.argv[1]](int(sys.argv[2]), int(sys.argv[3])).convert('RGB').save(sys.argv[4])
    else:
        for name, fn in SCREENS.items():
            for W, H in ((1920, 1080), (2880, 1864)):
                fn(W, H).convert('RGB').save(os.path.join(HERE, f'{name}-{W}x{H}.png')); print(name, W, H)
        print('brief words', screen_brief.last)
