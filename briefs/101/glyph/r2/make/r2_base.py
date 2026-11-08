"""The HUD sweep, round 2 (brief 101): the shared base. Lean first, by Sky's rule on numbers in play (2026-11-08): no hit chance anywhere, no
action durations; time shows only for key events in the world (the clock, the seal, police arriving, the rivals, the handover, a report
about to land, a loop running out); an order's price is money, Trail, Alert risk and what the street will do.
Built on round 1's base (frames lifted of today's HUD, faces, the kit's icons and type). Adds the busiest frame (the batch's fight-loud-body,
3:47.5 of the pinned loud run on seed 3), the keyline label (solid cream keyline: reads on paving, asphalt and palms alike), and the street
layer both directions share. Glyph. Mock only."""
import sys, os, json
R = '/Users/sky/Projects/suborned/'
sys.path.insert(0, R + 'briefs/101/glyph/make')
import h_base
from h_base import *  # Page, frame, face, badge, shadow, disc_shadow, arc, caps, patch_fill; INK, PAGE, ORANGE, DEEP, TURQ, AMBER, SOFT ...

OUT2 = R + 'briefs/101/glyph/r2/'; SCR2 = R + 'out/scratch/glyph/101r2/'; SRC = SCR2 + 'src/'
KEY = PAGE  # the keyline round ink type: the book's page cream
FRAMES['aim'] = (SRC + 'fight-loud-checkpoint', None, [])  # round 1's lifted frame stays cached; the scope reads this copy
LIVE = KIT + 'portraits/live/'


# ---------------------------------------------------------------- the busiest frame
def frame2(key):
    """Round 1's three frames as they were lifted; the busiest (fight-loud-body, 3:47.5) lifted the same way, with the seal card's paving
    rebuilt pixel by pixel from the nearest clean copy on the paving's own lattice ((37, 6) and (31, -11) px), so the brick joints carry on."""
    os.makedirs(SCR2 + 'frames', exist_ok=True); p = SCR2 + f'frames/{key}.png'
    if key not in ('busy', 'loud'): return frame(key)
    if os.path.exists(p): return Image.open(p).convert('RGB')
    if key == 'loud':  # the same camera as quiet, whose lift under the old roster is a whole facade (loud's busy chips left fragments there)
        a = np.array(frame('loud')).astype(float); b = np.array(frame('quiet')).astype(float)
        f = np.zeros(a.shape[:2], np.float32); f[0:446, 0:264] = 1; f = cv2.GaussianBlur(f, (0, 0), 4)[:, :, None]
        im = Image.fromarray((a * (1 - f) + b * f).astype(np.uint8)); im.save(p); return im
    src = SRC + 'fight-loud-body'; a = np.array(Image.open(src + '.png').convert('RGB'))[:, :, ::-1].copy()
    hud = (np.array(Image.open(src + '.hud.png').convert('RGBA'))[:, :, 3] > 12).astype(np.uint8) * 255
    m = cv2.dilate(hud, np.ones((9, 9), np.uint8)); a = patch_fill(a, m)
    rgb = np.array(Image.open(src + '.png').convert('RGB')); H, W = hud.shape
    bad = cv2.dilate(hud, np.ones((9, 9), np.uint8)) > 0; r, g, b = [rgb[:, :, i].astype(int) for i in range(3)]
    bad |= cv2.dilate(((b - r > 40) & (g - r > 30)).astype(np.uint8), np.ones((7, 7), np.uint8)) > 0  # the getaway ring's dashes
    for x0, y0, x1, y1 in [(880, 0, 930, 50), (900, 270, 1080, 430), (1320, 150, 1390, 270), (1360, 40, 1520, 190), (560, 120, 700, 270)]: bad[y0:y1, x0:x1] = True
    v1, v2 = np.array([37, 6]), np.array([31, -11])
    cands = sorted({tuple(k1 * v1 + k2 * v2) for k1 in range(-14, 15) for k2 in range(-14, 15)}, key=lambda s: s[0] ** 2 + s[1] ** 2)[1:]
    X0, Y0, X1, Y1 = 744, 56, 1180, 180; hole = np.zeros((H, W), bool); hole[Y0:Y1, X0:X1] = True; fill = rgb.copy()
    for y in range(Y0, Y1):
        for x in range(X0, X1):
            for dx, dy in cands:
                sx, sy = x + dx, y + dy
                if 0 <= sx < W and 0 <= sy < H and not bad[sy, sx] and not hole[sy, sx]: fill[y, x] = rgb[sy, sx]; break
    ring = np.zeros((H, W), bool); ring[Y0 - 10:Y1 + 10, X0 - 10:X1 + 10] = True; ring[Y0:Y1, X0:X1] = False; ring &= ~bad
    inner = np.zeros((H, W), bool); inner[Y0:Y1, X0:X1] = True; inner[Y0 + 10:Y1 - 10, X0 + 10:X1 - 10] = False
    fill = np.clip(fill.astype(float) + (rgb[ring].mean(0) - fill[inner].mean(0)), 0, 255)  # the local light
    f = np.zeros((H, W), np.float32); f[Y0 + 6:Y1 - 6, X0 + 6:X1 - 6] = 1; f = cv2.GaussianBlur(f, (0, 0), 5)[:, :, None]
    out = a[:, :, ::-1].astype(float) * (1 - f) + fill * f; im = Image.fromarray(out.astype(np.uint8)); im.save(p); return im


def base2(key):
    P = Page(2)
    P.img = Image.new('RGBA', (3840, 2160), (0, 0, 0, 0)) if MODE['clear'] else frame2(key).resize((3840, 2160), Image.LANCZOS).convert('RGBA'); return P


LAYERS = {}  # clear mode: each frame's HUD layer alone (1920 x 1080 RGBA), for tools/hud-budget (r2_budget.py)


def save2(P, name):
    if MODE['clear']: LAYERS[name] = P.img.resize((1920, 1080), Image.LANCZOS); return None
    os.makedirs(OUT2, exist_ok=True); im = P.img.resize((1920, 1080), Image.LANCZOS).convert('RGB'); im.save(OUT2 + name + '.png'); return im


def cover(P):
    """The HUD alone, alpha >= 24, at 1920 x 1080 (tools/hud-budget HUD_ALPHA on a .hud.png): shadows and keylines count."""
    return float((np.array(P.img.resize((1920, 1080), Image.LANCZOS))[:, :, 3] >= 24).mean())


# ---------------------------------------------------------------- the keyline: ink on any ground
def keyed(P, m, ox, oy, fill, key=KEY, kw=2.6, sh=70):
    """Composite a mask (L, at P's scale) at (ox, oy) px: a soft shadow, a solid keyline kw px wide (at 1080p), then the fill."""
    s = P.s; k = max(1, round(kw * s)); pad = k + 6; W, H = m.width + 2 * pad, m.height + 2 * pad
    big = Image.new('L', (W, H), 0); big.paste(m, (pad, pad)); arr = np.array(big)
    ker = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (2 * k + 1, 2 * k + 1)); kl = Image.fromarray(cv2.dilate(arr, ker))
    if sh:
        t = Image.new('RGBA', (W, H), (0x0b, 0x16, 0x15, 0)); t.putalpha(ImageChops.offset(kl, 0, round(1.2 * s)).filter(ImageFilter.GaussianBlur(1.6 * s)).point(lambda v: v * sh // 255))
        P.img.alpha_composite(t, (ox - pad, oy - pad))
    if key:
        t = Image.new('RGBA', (W, H), key[:3] + (0,)); t.putalpha(kl.filter(ImageFilter.GaussianBlur(0.35 * s))); P.img.alpha_composite(t, (ox - pad, oy - pad))
    t = Image.new('RGBA', (W, H), fill[:3] + (0,)); t.putalpha(big); P.img.alpha_composite(t, (ox - pad, oy - pad))


def tag(P, x, y, t, size, fill=INK, key=KEY, kw=2.6, face_='Jost-Bold', anchor='lm', sh=70):
    """A word or number laid straight on the street: Jost with a solid keyline. Returns its width (px at 1080p)."""
    s = P.s; f = font(face_, round(size * s)); l, t0, r, b = f.getbbox(t, anchor=anchor); m = Image.new('L', (r - l + 4, b - t0 + 4), 0)
    ImageDraw.Draw(m).text((2 - l, 2 - t0), t, font=f, fill=255, anchor=anchor); keyed(P, m, round(x * s) + l - 2, round(y * s) + t0 - 2, fill, key, kw, sh)
    P.words_add(t); return (r - l) / s


def kicon(P, name, x, y, d, fill=INK, key=KEY, kw=2.2, sh=60, folder='icons/singles'):
    """A kit pictogram on the street with the same keyline."""
    s = P.s; n = round(d * s); a = sprite(folder + '/' + name).getchannel('A').resize((n, n), Image.LANCZOS); keyed(P, a, round(x * s), round(y * s), fill, key, kw, sh)


def lamps(P, x, y, n, d=13, gap=4, of=3):
    """Alert as lamps: lit orange, unlit cream rings; each with the ink keyline so they hold on any ground."""
    for i in range(of):
        cx = x + i * (d + gap); P.disc(cx - 1.6, y - d / 2 - 1.6, d + 3.2, fill=INK, ring=None)
        P.disc(cx, y - d / 2, d, fill=ORANGE if i < n else PAGE, ring=None)
    return of * d + (of - 1) * gap


def tab(P, x, y, d, icon, fill=BODY_, ring=PAGE, rw=2):
    """A round button (pause, turn, the eye, Book): the kit's deep body, a cream ring, a cream pictogram."""
    disc_shadow(P, x, y, d, a=60); P.disc(x, y, d, fill=fill, ring=ring, rw=rw); P.icon(icon, x + d * .24, y + d * .24, d * .52, col=CREAM)


def live_face(P, who, state, x, y, d, ring=PAGE, rw=2.5):
    """A cell member's live portrait (portraits/live/<who>_<state>), round."""
    FACES['_live'] = LIVE + f'{who}_{state}@2x.png'; INSET['_live'] = .06; face(P, '_live', x, y, d, ring=ring, rw=rw)


def lens(P, src, x, y, d, glass=INK, rim=PAGE, inline=None):
    """The plot scope as a round lens: the capture's own map (inside today's scope rim), its light strokes in cream and turquoise on the glass,
    clipped to the circle; a rim; with `inline`, the enamel sign's inner line."""
    s = P.s; a = np.array(Image.open(src).convert('RGB').crop((24, 944, 216, 1058))).astype(float)[:, 39:153]
    L = a @ [0.3, 0.59, 0.11]; n = round((d - 14) * s)
    al = Image.fromarray(np.clip((L - 50) / 110 * 255, 0, 255).astype(np.uint8)).resize((n, n), Image.LANCZOS)
    hue = Image.fromarray((np.clip((a[:, :, 2] - a[:, :, 0]) / 90, 0, 1) * 255).astype(np.uint8)).resize((n, n), Image.LANCZOS)
    disc_shadow(P, x, y, d, a=70); P.disc(x, y, d, fill=glass, ring=None)
    m = Image.new('L', (n, n), 0); ImageDraw.Draw(m).ellipse((0, 0, n - 1, n - 1), fill=255); al = ImageChops.multiply(al, m)
    t = Image.new('RGBA', (n, n), CREAM[:3] + (0,)); t.putalpha(al); col = Image.new('RGBA', (n, n), TURQ[:3] + (0,)); col.putalpha(ImageChops.multiply(al, hue))
    P.img.alpha_composite(t, (round((x + 7) * s), round((y + 7) * s))); P.img.alpha_composite(col, (round((x + 7) * s), round((y + 7) * s)))
    P.disc(x, y, d, fill=None, ring=rim, rw=2.2)
    if inline: P.disc(x + 5, y + 5, d - 10, fill=None, ring=inline, rw=1.2)


# ---------------------------------------------------------------- the street layer (both directions)
def bubble(P, who, t, x, y, stem=34, live=None):
    """Speech: a cream bubble at the speaker, the speaker's face (or badge) at its left, Barlow ink; a fine stem down to the head."""
    w = P.tw(t, 15.5, 'Barlow-Medium') + 44; x0 = x - w / 2; h = 28
    shadow(P, x0, y - h / 2, w, h, h / 2, dy=2, a=46, blur=2.5); P.rrect(x0, y - h / 2, w, h, h / 2, fill=PAGE)
    if who in ('police', 'rival', 'guard'): P.disc(x0 + 3, y - 11, 22, fill=INK, ring=None); badge(P, {'police': 'signal_police', 'rival': 'signal_rival', 'guard': 'signal_guard'}[who], x0 + 4, y - 10, 20)
    elif live: live_face(P, who, live, x0 + 3, y - 11, 22, ring=None)
    else: face(P, who, x0 + 3, y - 11, 22, ring=None)
    P.text(x0 + 32, y + 1, t, 15.5, col=INK, face='Barlow-Medium'); P.words_add(t)
    P.line([(x, y + h / 2), (x, y + h / 2 + stem)], col=PAGE, lw=2.2)


def ground_arc(P, cx, cy, rx, frac, col, lw, ratio=.42, full=None, gaps=0, start=-90):
    """A ring on the ground at the play camera's tilt: `frac` of it from the back, clockwise; `full` draws the whole ring faintly first;
    `gaps` > 0 breaks the drawn ring into that many arcs (the long-shot cue)."""
    s = P.s; k = 3; ry = rx * ratio; pad = lw * 2 + 6; W, H = round((rx + pad) * 2 * s * k), round((ry + pad) * 2 * s * k)
    big = Image.new('RGBA', (W, H), (0, 0, 0, 0)); g = ImageDraw.Draw(big); bb = (pad * s * k, pad * s * k, W - pad * s * k, H - pad * s * k)
    if full: g.ellipse(bb, outline=full, width=round(lw * .6 * s * k))
    if gaps:
        seg = 360 / gaps
        for i in range(gaps): g.arc(bb, start + i * seg + seg * .16, start + (i + 1) * seg - seg * .16, fill=col, width=round(lw * s * k))
    else: g.arc(bb, start, start + 360 * frac, fill=col, width=round(lw * s * k))
    out = big.resize((W // k, H // k), Image.LANCZOS); a = np.array(out.getchannel('A'))
    ker = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (round(2.2 * s) * 2 + 1,) * 2); kl = Image.fromarray(cv2.dilate(a, ker))
    t = Image.new('RGBA', out.size, INK[:3] + (0,)); t.putalpha(kl.point(lambda v: v * 150 // 255)); P.img.alpha_composite(t, (round((cx - rx - pad) * s), round((cy - ry - pad) * s)))
    P.img.alpha_composite(out, (round((cx - rx - pad) * s), round((cy - ry - pad) * s)))


def shield(P, x, y, d, part=False):
    """Cover as a shape: a small shield on the target's ring (whole: his cover counts; half-filled: counts in part)."""
    s = P.s; k = 4; n = round(d * s * k); m = Image.new('L', (n, round(n * 1.15)), 0); g = ImageDraw.Draw(m); w, h = m.size
    pts = [(w * .5, 0), (w, h * .16), (w * .94, h * .55), (w * .5, h), (w * .06, h * .55), (0, h * .16)]; g.polygon(pts, fill=255)
    m = m.resize((round(d * s), round(d * 1.15 * s)), Image.LANCZOS); keyed(P, m, round(x * s), round(y * s), AMBER if not part else PAGE, INK, 1.8, 50)
    if part: P.rrect(x + d * .5, y + 2, d * .42, d * 1.0, 1, fill=AMBER)


def crosshair(P, cx, cy, r=13, col=PAGE):
    """The cursor over a hostile: a crosshair ring with four ticks, ink keyline."""
    s = P.s; k = 4; n = round((r + 8) * 2 * s * k); m = Image.new('L', (n, n), 0); g = ImageDraw.Draw(m); o = n / 2; rr = r * s * k; lw = round(2.4 * s * k)
    g.ellipse((o - rr, o - rr, o + rr, o + rr), outline=255, width=lw)
    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)): g.line([(o + dx * (rr - 4 * s * k), o + dy * (rr - 4 * s * k)), (o + dx * (rr + 7 * s * k), o + dy * (rr + 7 * s * k))], fill=255, width=lw)
    m = m.resize((n // k, n // k), Image.LANCZOS); keyed(P, m, round(cx * s - m.width / 2), round(cy * s - m.height / 2), col, INK, 1.8, 50)


def pips(P, x, y, n, of, d=7, gap=3, col=AMBER):
    """Health as pips over the focus only (lit in the target's signal colour), ink keyline."""
    w = of * d + (of - 1) * gap; x0 = x - w / 2
    for i in range(of): P.disc(x0 + i * (d + gap) - 1.4, y - 1.4, d + 2.8, fill=INK, ring=None); P.disc(x0 + i * (d + gap), y, d, fill=col if i < n else PAGE, ring=None)


def focus(P, head, feet, hp=(3, 3), col=AMBER, cover_=None, long_=False, aim=False, holds=False):
    """A target under fire: one ring at his feet in his signal colour (whole: in range; broken into arcs: a long shot; a shield on its front
    when his cover counts), his health pips over his head (over the focus only), the holder mark above them if he holds our car. With `aim`,
    the player's cursor over him and the word Fire. Who fires at him is on the roster (the firing cue), not over him."""
    (hx, hy), (fx, fy) = head, feet; ground_arc(P, fx, fy, 30, 1, col, 3.6, gaps=3 if long_ else 0)
    if cover_: shield(P, fx - 8, fy + 6, 16, part=cover_ == 'part')
    pips(P, hx, hy - 22, *hp, col=col)
    if holds: holder(P, hx - 10, hy - 50)
    if aim:
        crosshair(P, hx + 28, hy + 30); tag(P, hx + 56, hy + 31, 'Fire', 17)


def cue(P, state, x, y, d):
    """The live portrait's cue layer (what the face can't carry at 60 px): under fire, a spiked ring; firing, rays at the sidearm."""
    cx, cy, r = x + d / 2, y + d / 2, d / 2 + 4
    if state == 'under_fire':
        for i in range(12):
            a = math.radians(i * 30 + 8); p0 = (cx + math.cos(a) * r, cy + math.sin(a) * r); p1 = (cx + math.cos(a) * (r + 7), cy + math.sin(a) * (r + 7))
            P.line([p0, p1], col=INK, lw=4.2); P.line([p0, p1], col=PAGE, lw=2.2)
    if state == 'firing':
        bx, by = x + d - 6, y + d - 8
        for a in (-20, 15, 50):
            t = math.radians(a); p0 = (bx + math.cos(t) * 14, by + math.sin(t) * 14); p1 = (bx + math.cos(t) * 22, by + math.sin(t) * 22)
            P.line([p0, p1], col=INK, lw=4.4); P.line([p0, p1], col=ORANGE, lw=2.4)


def car_marks(P, x, y, held, aboard, of=5):
    """The getaway car's state on the car: a lock and the holders' count while it is held, the five seat pips (filled as each boards)."""
    if held: kicon(P, 'lock', x, y - 9, 18, fill=INK); tag(P, x + 20, y + 1, str(held), 16)
    px = x - 4
    for i in range(of): P.disc(px + i * 11 - 1.4, y + 14 - 1.4, 10.8, fill=INK, ring=None); P.disc(px + i * 11, y + 14, 8, fill=TURQ if i < aboard else PAGE, ring=None)


def holder(P, x, y, d=20, col=AMBER):
    """A holder of our car: the holder mark (a struck disc) in his signal colour over his head."""
    P.disc(x - 1.5, y - 1.5, d + 3, fill=INK, ring=None); P.disc(x, y, d, fill=col, ring=None); P.line([(x + d * .25, y + d * .75), (x + d * .75, y + d * .25)], col=INK, lw=2.6)


def edge_face(P, who, x, y, d=38, side='r'):
    """Someone who matters, off screen: his face docked at the edge he is past, a chevron pointing out (Popovic, with the Enforcer)."""
    disc_shadow(P, x, y, d, a=70); face(P, who, x, y, d, ring=PAGE, rw=2.5)
    cy = y + d / 2; g = ImageDraw.Draw(P.img); s = P.s
    pts = [(x + d + 3, cy - 8), (x + d + 13, cy), (x + d + 3, cy + 8)] if side == 'r' else [(x - 3, cy - 8), (x - 13, cy), (x - 3, cy + 8)]
    g.polygon([(a * s, b * s) for a, b in pts], fill=INK)


def reporter(P, Rp):
    """A report about to land (a key event: its time shows): a ring at the reporter's feet draining orange, '!' and the seconds over him,
    the way he goes as three dots; hover would name what it brings (the hover state isn't drawn here)."""
    (hx, hy), (fx, fy) = Rp['head'], Rp['feet']; ground_arc(P, fx, fy, 22, Rp['left'] / Rp['of'], ORANGE, 4.2, full=PAGE[:3] + (210,))
    P.disc(hx - 12, hy - 40, 24, fill=ORANGE, ring=INK, rw=1.6); P.icon(Rp['way'], hx - 7, hy - 35, 14, col=CREAM)
    if Rp.get('side') == 'l': tag(P, hx - 18, hy - 27, f"{Rp['left']} s", 21, fill=ORANGE, key=INK, kw=1.7, anchor='rm')
    else: tag(P, hx + 18, hy - 27, f"{Rp['left']} s", 21, fill=ORANGE, key=INK, kw=1.7)
    if not Rp.get('dir'): return
    dx, dy = Rp['dir']
    for i in range(3): P.disc(fx + dx * (32 + i * 11) - 3, fy + dy * (32 + i * 11) - 3, 6, fill=ORANGE, ring=INK, rw=1.2)


# ---------------------------------------------------------------- the real data, by moment (the runs' own; sources in notes.md)
CELL = ['scout', 'fixer', 'tech', 'enforcer']
KNOWN = [('waitress', 1), ('guard', 1), ('driver', 0), ('councillor', 1)]  # the key people under the clock; 0 = not yet known. Illustrative
BUSY = {  # fight-loud-body: tools/game/pins/loud.seed3.json at tick 2275 (3:47.5), batch build 0.2.268; the sim log (engine 0.50.0) for the state
    'clock': '3:47', 'next': ('nextevent', 'Chime', '5:10'), 'cash': '$100,000', 'trail': 8, 'alert': 2,
    'seal': {'left': '1:54', 'at': '5:41', 'cause': 'fire'},
    'news': {'kind': 'police', 'title': 'Police coming', 'cause': 'fire', 'where': 'shots heard', 'when': '2:33', 'in': None, 'of': (1, 5)},  # that unit is already here (3:16): no count
    'live': {'scout': 'firing', 'fixer': 'under_fire', 'tech': 'firing', 'enforcer': 'focused'}, 'busy': {'enforcer': 's_carrying'},
    'hp': {'scout': (4, 5), 'fixer': (5, 5), 'tech': (5, 5), 'enforcer': (8, 8)}, 'sel': 'scout',
    'speech': [('rival', 'Block the street. Nobody leaves.', (764, 704)), ('scout', "One of them's heading for the road.", (1113, 361))],
    'car': {'at': (950, 533), 'held': 1, 'aboard': 0}, 'edge': ('councillor', (1866, 846)),
    # security-1, the blocker in cover at the planter run by the road: Fixer and Tech fire on him (weapons free picks at 3:47.5); the player's
    # right hold adds the Scout (her order at 3:48.7 is the rivals; here, the blocker, to free the car). Untraced in the final fight: no price.
    'focus': {'head': (1083, 706), 'feet': (1083, 772), 'hp': (3, 3), 'cover': 'whole', 'long': False, 'holds': True},
    # a report about to land: from 4:54.1 of the same run (p-records-office-12 walks to the Office door to report a body; lands 5:03.2)
    'report': {'head': (1345, 168), 'feet': (1345, 246), 'left': 9, 'of': 9, 'way': 'call', 'dir': (-.55, -.83), 'side': 'l'}}
LOUD2 = dict(LOUD, live={'scout': 'calm', 'fixer': 'focused', 'tech': 'calm', 'enforcer': 'firing'}, busy={'fixer': 'run'}, hp=LOUD['hp'],
             news={'kind': 'police', 'title': 'Police coming', 'cause': 'fire', 'where': 'shots heard', 'when': '2:33', 'in': '0:42', 'of': (1, 2)},
             speech=[('tech', 'It took the orders. It always does.', (1136, 640))])
QUIET2 = dict(QUIET, live={'scout': 'calm', 'fixer': 'calm', 'tech': 'focused', 'enforcer': 'calm'}, busy={},
              news={'kind': 'car', 'title': 'Our car boxed in', 'cause': 'car', 'where': 'their car across our way', 'when': '2:46', 'in': None, 'of': (1, 2)},
              speech=[('tech', "The tape's run out.", (1136, 640))],
              report2={'head': (867, 452), 'feet': (867, 522), 'left': 4, 'of': 5, 'way': 'radio', 'dir': (0, 0)})
AIM2 = dict(AIM, live={'scout': 'calm', 'fixer': 'calm', 'tech': 'calm', 'enforcer': 'focused'}, busy={'scout': 'run', 'fixer': 'run'},
            # the first shot: the street goes loud. Alert 2, police come, the district seals; Trail +6 (+2 fire, +2 hit, +2 down). No odds, no seconds.
            shot={'who': 'enforcer', 'at': 'guard', 'head': (912, 262), 'feet': (912, 332), 'from': (960, 492), 'alert': 2, 'trail': '+6', 'long': False})
