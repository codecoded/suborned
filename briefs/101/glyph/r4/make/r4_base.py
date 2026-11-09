"""Brief 101, round 4 (Glyph): the 0.2.290 review's items 9 and 10, and the 460 px wrap for Sky, drawn in the built street kit over real
0.2.290 frames (copies with SHA256 in out/scratch/glyph/101r4/src/, a KEEP file beside them: the batch shots rotate and Latch is building 0.2.292).
 9   the docked callout's line: the street bubble beside the docked face, with a short stem to it and no face of its own (the dock is the face). It
     counts toward the two lines on screen; when a third would show, the oldest gives way, and a docked line that gives way takes its face with it.
 10  the verb plate, its fold tab, the armed plate, the strip and the cursor's armed disc: one verb row of built round tabs after the take-control tab.
     The armed verb wears the turquoise ring on the book's deep teal and its name as a street label under it. Keys show only on hover, in the soft ink.
     The cursor's armed verb is a keyed pictogram.
 wrap a bubble wraps at 460 px into at most two rows, at the break that makes the longer row shortest. A line two rows of 460 can't hold widens its
     bubble (never a third row).
Built on round 3's base (the lift and fills, the kit's pieces). Glyph. Mock only: nothing here ships."""
import sys, os, json, math
R = '/Users/sky/Projects/suborned/'
sys.path.insert(0, R + 'briefs/101/glyph/r3/make')
import r3_base
from r3_base import *  # lift, layer, cover_px, frame_cover, compose, label, tab3, capsule, face3, stem, tag, kicon, face, badge, shadow, ...

OUT4 = R + 'briefs/101/glyph/r4/'; SCR4 = R + 'out/scratch/glyph/101r4/'; SRC4 = SCR4 + 'src/'
r3_base.SCR3 = SCR4  # the lift's cache goes in this round's scratch
SHOTS.update({  # the real captures, all 0.2.290 (the proofs of 03:53 to 04:34 on 9 Nov, the batch's shot of 04:42 and Latch's t101 shot of 04:49)
    'fight': (SRC4 + 'r123-fight-safe', '0.2.290', "4:15 at the square (r123's fight proof): two bubbles and the guard's docked line"),
    'inset': (SRC4 + 'r085-inset-inez', '0.2.290', "2:39 (r085's proof): Inez's line said off screen, docked at the left edge"),
    'plate': (SRC4 + 'r29-cone-alt', '0.2.290', 'the Action plate on (r29 proof): the Enforcer with Knock out armed'),
    'seed': (SRC4 + 'seed-fire-alt', '0.2.290', 'the Action plate on, Alt held (the seed proof): the Enforcer, nothing armed'),
    'strip': (SRC4 + 'hold-watch-reach', '0.2.290', "the strip after Shift (the hold proof): the Scout's three verbs"),
    'line': (SRC4 + 'r123-body-safe', '0.2.290', "4:27 (r123's proof): the Fixer's 21-word line of the Help, 712 px on one row"),
    'call': (SRC4 + 't101-window', '0.2.290', "route A's call, 2:59: the Fixer's 17-word line, 590 px, ran to x 2120"),
})
SIBS.update({  # captures of the same camera (median difference 0-3 grey levels at 192 x 108): their street fills a lifted piece where they show no HUD
    'fight': [],  # its siblings are older builds whose road reads lighter (a ghost of the line's end): the frame's own street fills it
    'inset': ['out/scratch/latch/proof102b/r085-inset-inez', 'out/scratch/latch/098/proof/r085-inset-inez', 'out/scratch/latch/proof102/r085-inset-inez'],
    'plate': ['out/build/play/proof/r29-cone-pressure-a0', 'out/build/play/proof/r29-cone-takedown', 'out/scratch/latch/101/proof/r29-cone-alt'],
    'seed': ['out/build/play/proof/r085-guard-enforcer', 'out/build/play/proof/r085-guard-hover', 'out/build/play/proof/r085-guard-ring'],
    'strip': ['out/build/play/proof/r29-cone-rest', 'out/build/play/proof/r29-cone-strong'],
    'line': ['out/scratch/latch/r4/budget-probe/r123-body-safe', 'out/build/play/out/scratch/latch/cones100/proof/r123-body-safe', 'out/scratch/latch/098j/proof/r123-body-safe'],
    'call': [],
})
KITP = R + 'unity/Assets/Suborned/UI/Resources/Kit/portraits/'
for w in ('scout', 'fixer', 'tech', 'enforcer'): FACES['cameo_' + w] = KITP + f'cameo_{w}_normal@2x.png'; INSET['cameo_' + w] = .0
BADGE = {'rivals': 'signal_rival', 'guard': 'signal_guard', 'police': 'signal_police'}  # a speaker with no painted face: its badge on an ink disc (HudToolkit.BubbleArt)

# ---------------------------------------------------------------- the bubble (HudToolkit.Bubbles as built, and the wrap)
BUB = dict(h1=28, lead=18.6, r=14, face=22, size=15.5, left_face=32, left=12, right=12)  # Barlow Medium 15.5: ascent 1000, descent 200 a 1000, so 18.6 px a row
BUB['h2'] = round(BUB['h1'] + BUB['lead'])  # 47: a second row adds one line of the font
PHRASE = 40  # the wrap breaks after a comma or a full stop when that row is at most 40 px longer than the most balanced break ("It's yours, / Miss Nikolaou")
WRAP = 460  # the wrap: a bubble's box at most 460 px wide at 1080 (Glyph, r5 review), one row while it fits


def text_w(P, t): return P.tw(t, BUB['size'], 'Barlow-Medium')


def rows_for(P, t, wrap=None, face=True):
    """The rows a bubble shows: one, or (wrap) two: after a comma or a full stop when that costs at most PHRASE px over the most balanced break, else at
    the break that makes the longer row shortest. Returns (rows, the widest row's px)."""
    one = text_w(P, t); pad = (BUB['left_face'] if face else BUB['left']) + BUB['right']
    if not wrap or one + pad <= wrap: return [t], one
    ws = t.split(); cuts = []
    for i in range(1, len(ws)):
        a, b = ' '.join(ws[:i]), ' '.join(ws[i:]); cuts.append((max(text_w(P, a), text_w(P, b)), a[-1] in ',.;:?!…', [a, b]))
    best = min(c[0] for c in cuts); phrase = [c for c in cuts if c[1] and c[0] <= best + PHRASE]  # a phrase's end, if it costs at most PHRASE px
    m, _, rows = min(phrase or [c for c in cuts if c[0] == best], key=lambda c: c[0]); return rows, m


def bubble_box(P, t, wrap=None, face=True):
    rows, tw = rows_for(P, t, wrap, face); pad = (BUB['left_face'] if face else BUB['left']) + BUB['right']
    return rows, math.ceil(tw + pad), (BUB['h1'] if len(rows) == 1 else BUB['h2'])


def speaker_face(P, who, x, y):
    """The bubble's 22 px face: the cell's cameo, else the speaker's badge on an ink disc (HudToolkit.BubbleArt)."""
    if who in BADGE: P.disc(x, y, 22, fill=INK, ring=None); badge(P, BADGE[who], x + 1, y + 1, 20)
    else: face(P, 'cameo_' + who, x, y, 22, ring=None)


def bubble4(P, who, t, x0, y0, wrap=None, face_=True):
    """The built street bubble at (x0, y0) top left: the book's page cream, round ends (a 14 px corner on two rows), a soft shadow, the speaker's
    face at its left (none when docked), Barlow Medium 15.5 in the book's ink. Returns (x0, y0, w, h)."""
    rows, w, h = bubble_box(P, t, wrap, face_)
    shadow(P, x0, y0, w, h, BUB['r'], dy=2, a=46, blur=2.5); P.rrect(x0, y0, w, h, BUB['r'], fill=PAGE)
    if face_: speaker_face(P, who, x0 + 3, y0 + h / 2 - 11)
    tx = x0 + (BUB['left_face'] if face_ else BUB['left']); top = y0 + (h - BUB['lead'] * len(rows)) / 2
    for i, row in enumerate(rows): P.text(tx, top + BUB['lead'] * (i + .5) + 1, row, BUB['size'], col=INK, face='Barlow-Medium')
    P.words_add(t); return (x0, y0, w, h)


def street_bubble(P, who, t, head, gap=34, wrap=None, xmin=14, xmax=1906, keep_y=None):
    """A bubble over its speaker as the build places it: centred on the head's x, its bottom `gap` px over the head (`keep_y`: the bottom held where
    today's one-row bubble had it), slid inside [xmin, xmax] (Latch's clamp, 14 px in; xmax also keeps it off an edge face); the plain cream stem
    (BubbleTails: 2.2 px) from its bottom to the head, its foot kept 14 px inside the bubble's ends. Returns the box."""
    rows, w, h = bubble_box(P, t, wrap, True); bottom = keep_y if keep_y is not None else head[1] - gap
    x0 = min(max(head[0] - w / 2, xmin), xmax - w); y0 = bottom - h
    sx = min(max(head[0], x0 + 14), x0 + w - 14); P.line([(sx, bottom - 1), (head[0], head[1])], col=PAGE, lw=2.2)
    return bubble4(P, who, t, x0, y0, wrap)


def docked_bubble(P, t, dock, side, wrap=None, gap=14, dy=0):
    """Item 9: a line said off screen, beside its docked face (`dock` = centre, 56 px): the street bubble with no face of its own, on the side toward the
    screen's centre (`side` 'l' or 'r' of the dock), centred on the dock's height (`dy` moves it clear of the HUD), `gap` px off the rim; a short stem
    (the built Stem: cream 2.2 px on an ink keyline, no dot) from the bubble's end to the rim. Returns the box."""
    rows, w, h = bubble_box(P, t, wrap, False); cx, cy = dock; R_ = 28; y0 = cy - h / 2 + dy
    x0 = cx + R_ + gap if side == 'r' else cx - R_ - gap - w
    ex = x0 if side == 'r' else x0 + w; rx = cx + R_ - 1 if side == 'r' else cx - R_ + 1
    sy = min(max(cy, y0 + BUB['r']), y0 + h - BUB['r'])
    P.line([(ex, sy), (rx, cy)], col=(0x0b, 0x16, 0x15, 130), lw=4.0); P.line([(ex, sy), (rx, cy)], col=PAGE, lw=2.2)
    return bubble4(P, None, t, x0, y0, wrap, face_=False)


# ---------------------------------------------------------------- item 10: the verb row
TAKE = dict(x=100, d=30)  # the take-control tab (HudToolkit.TakeControl: x 100, 30 px, centred on the face)
ROW = dict(gap0=14, d=30, gap=6)  # the verb row: 14 px after the take-control tab, then 30 px tabs 6 px apart


def row_x(): return TAKE['x'] + TAKE['d'] + ROW['gap0']


def shift_key(P, x, cy, n, col=SOFT, size=13.5):
    """The Shift key as the plate drew it ('⇧3'; Jost has no ⇧, so the arrow is drawn), keyline type in `col`. Returns the width."""
    s = 1.0; a = 10.5 * s; pts = [(x + a / 2, cy - 6.5), (x + a, cy - .5), (x + a * .72, cy - .5), (x + a * .72, cy + 5.5), (x + a * .28, cy + 5.5), (x + a * .28, cy - .5), (x, cy - .5)]
    m = Image.new('L', (round(P.img.width), round(P.img.height)), 0)
    g = ImageDraw.Draw(m); g.polygon([(px * P.s, py * P.s) for px, py in pts], outline=255, width=round(1.7 * P.s))
    bb = m.getbbox(); keyed(P, m.crop(bb), bb[0], bb[1], col, KEY, 2.0, 50)
    return a + 2 + tag(P, x + a + 2, cy, str(n), size, fill=col, kw=2.4)


def verb_row(P, cy, icons, armed=None, hover=None, shut=(), names=None, keys=None):
    """The verb row beside the selected face: one built round tab (30 px) per verb the agent has, 6 px apart, starting 14 px after the take-control tab.
    Armed: the book's deep teal body and the turquoise-light ring (2.2 px), its name as a street label 6 px under it. Hovered: the built hover ring and its
    name with its key after it in the soft ink (the take-control tab's way). A shut verb at 55 %. Returns the row's right end."""
    x = row_x(); d = ROW['d']; y = cy - d / 2; out = x
    for i, ic in enumerate(icons):
        tx = x + i * (d + ROW['gap']); out = tx + d
        if i == armed: disc_shadow(P, tx, y, d, dy=2, a=60, blur=2.5); P.disc(tx, y, d, fill=DEEP, ring=TURQ, rw=2.2); P.icon(ic, tx + d * .24, y + d * .24, d * .52, col=PAGE)
        elif i == hover: disc_shadow(P, tx, y, d, dy=2, a=60, blur=2.5); P.disc(tx, y, d, fill=BODY3, ring=TURQ, rw=2); P.icon(ic, tx + d * .24, y + d * .24, d * .52, col=PAGE)
        else:
            Q = layer(); tab3(Q, tx, y, d, ic)
            if i in shut: Q.img.putalpha(Q.img.getchannel('A').point(lambda v: v * 55 // 100))
            P.img.alpha_composite(Q.img)
    k = hover if hover is not None else armed
    if k is not None and names:
        cx = x + k * (d + ROW['gap']) + d / 2; ly = cy + d / 2 + 6 + 8; nm = names[k]
        if hover is not None and keys:
            w = P.tw(nm, 13.5, 'Jost-Bold') + 6 + P.tw('·', 13.5, 'Jost-Bold') + 6 + 12.5 + P.tw(str(keys[k]), 13.5, 'Jost-Bold'); lx = cx - w / 2
            lx += tag(P, lx, ly, nm, 13.5) + 6; lx += tag(P, lx, ly, '·', 13.5, fill=SOFT) + 6; shift_key(P, lx, ly, keys[k])
        else: tag(P, cx, ly, nm, 13.5, anchor='mm')
    return out


def tab_badge(P, x, y, d, name):
    """An outcome's kit badge (mark_ok, mark_half, mark_no) at a tab's lower right on the cream disc, as the built face carries it (rule 4: the cream disc is
    for the outcome badge only)."""
    bd = round(d * .42); P.disc(x + d - bd + 2 - 1.2, y + d - bd + 2 - 1.2, bd + 2.4, fill=PAGE, ring=None); badge(P, name, x + d - bd + 2, y + d - bd + 2, bd)


def take_tab(P, cy):
    """The take-control tab as Latch builds it (Sky's pick, 2026-11-09): the built round tab, 30 px, at x 100, centred on the selected face."""
    tab3(P, TAKE['x'], cy - TAKE['d'] / 2, TAKE['d'], 'take_control')


def cursor_badge(P, x, y, icon):
    """The armed verb at the cursor (14 px right of and 16 px under its tip): the verb's pictogram keyed in the book's ink, 18 px; no disc (a disc means a click)."""
    kicon(P, icon, x, y, 18, fill=INK)


def edge_face(P, who, x, y, d=30, ring=TURQ):
    """An agent's edge face as built (a cameo in its ring): redrawn where a lifted bubble hid its top half."""
    disc_shadow(P, x, y, d, dy=2, a=60, blur=2.5); face(P, 'cameo_' + who, x, y, d, ring=ring, rw=3)


def thin_fill(im, rest, key, boxes):
    """A stem's few pixels (a 2.2 px line): inpainted from either side (cv2 NS, 4 px), so what stands behind it (a red post) keeps its shape where a
    copied patch smeared it; taken out of what stays of today's HUD alpha."""
    a = np.array(im)[:, :, ::-1].copy(); _, al = real(key); m = np.zeros(al.shape, np.uint8)
    for x0, y0, x1, y1 in boxes: m[y0:y1, x0:x1] = (al[y0:y1, x0:x1] > 3).astype(np.uint8) * 255
    m = cv2.dilate(m, np.ones((3, 3), np.uint8)); a = cv2.inpaint(a, m, 4, cv2.INPAINT_NS); rest = rest.copy(); rest[m > 0] = 0
    return Image.fromarray(a[:, :, ::-1]), rest


# ---------------------------------------------------------------- coverage, as tools/hud-budget reads a .hud.png
def frame_alpha(rest, P):
    """The frame's whole HUD alpha after the change: what stays of today's layer, the new pieces over it (1920 x 1080)."""
    a = np.array(P.img.resize((1920, 1080), Image.LANCZOS))[:, :, 3].astype(np.uint8); return np.maximum(rest, a)
