"""Brief 101, round 3 (Glyph): the pieces still in the old kit, mocked in the built street style over real frames of 0.2.286 and 0.2.287.
The shared base: the real frames with their old pieces lifted (round 1's patch fill on the HUD layer's own pixels), and the street kit as
0.2.287 draws it (HudToolkit.Street.cs, Kit/street.uss): keyline labels, keyed icons, round tabs, cream-ringed faces, the fix chips' capsule.
Five rules, one per family of old pieces (notes.md):
  1 words in the street are street labels (no plate, chrome, screws or brass);
  2 what you click is a round tab, or a capsule when it has a word;
  3 time that runs is a ring round the thing (it drains for a window, fills for attention);
  4 every face is the built cream-ringed face;
  5 the bottom right holds the lozenge and the Book only.
Glyph. Mock only: nothing here ships."""
import sys, os, json, math
R = '/Users/sky/Projects/suborned/'
sys.path.insert(0, R + 'briefs/101/glyph/r2/make')
from r2_base import *  # keyed, tag, kicon, tab, live_face, ground_arc, holder, face, badge, shadow, disc_shadow, arc, patch_fill, Page, INK, PAGE, ...

OUT3 = R + 'briefs/101/glyph/r3/'; SCR3 = R + 'out/scratch/glyph/101r3/'
RED, AMBER_S, VIOLET_S = (0xe0, 0x3a, 0x2f, 255), (0xf0, 0xa0, 0x20, 255), (0x8a, 0x4f, 0xd0, 255)  # signal_colours (suborned.style.json)
BODY3 = (0x0c, 0x34, 0x34, 255); LINE3 = (0xc8, 0xe0, 0xda, 255); HAIR3 = (0xd8, 0xd3, 0xc4, 255)  # --book-body, --enamel-line, the page's hairline
SHOTS = {  # the real captures: build, moment
    'quiet': (R + 'out/scratch/latch/r4/shots-final/t101-quiet', '0.2.286', 'route G, 3:28.6: a guard radios in the knocked-out guard; the square camera on the Tech'),
    'window': (R + 'out/build/play/proof/done-call-window', '0.2.286', "2:59: the call lands, the guard leaves his door; the guard's away window runs"),
    'card': (R + 'out/batch/proofset/r123/r123-body-safe', '0.2.286', "4:27: Inez's card docked over the news (r123's proof)"),
    'ring': (R + 'out/scratch/latch/r4/proof-r101-final/r101-ring-plates', '0.2.287', 'the act ring on Inez (r101 proof)'),
    'next': (R + 'out/batch/proofset/r067/r067-official-car', '0.2.286', "5:21: the next event, Popovic exits 5:30"),
    'conv': (R + 'out/batch/proofset/r083/r085-conv-cards', '0.2.286', "Inez's card after the conversation, over her (r085's proof)"),
}


def real(key):
    p = SHOTS[key][0]; return Image.open(p + '.png').convert('RGB'), np.array(Image.open(p + '.hud.png').convert('RGBA'))[:, :, 3]


SIBCLONE = {}  # key: sibling: holes Poisson-blended from that capture of the same camera
FLAT = set()  # frames whose holes lie on flat shadowed ground: a smooth inpaint (a copied patch brought lit paving in)
WHOLE = {}  # key: [(box, sibling)]: a region copied whole from a sibling, HUD included (same clock stack, no old plate)
SIBS = {  # captures of the same camera (median difference 0-2 grey levels at 192 x 108): their street fills a lifted piece where they show no HUD
    'quiet': ['out/scratch/latch/r4/shots-final/t101-rest'],
    'window': ['out/build/play/proof/r085-guard-enforcer', 'out/build/play/proof/r085-guard-hover', 'out/build/play/proof/r085-guard-ring'],
    'ring': ['out/build/play/proof/hold-act-walking', 'out/build/play/proof/hold-act-offer', 'out/build/play/proof/hold-act-charge', 'out/build/play/proof/hold-squad-leave'],
    'conv': ['out/build/play/proof/r085-partner-walking', 'out/build/play/proof/r085-partner-talking'],
    'card': ['out/scratch/latch/098t/proof/r123-body-safe', 'out/scratch/latch/098j/proof/r123-body-safe'],  # the same moment before the card docked there (brief 098's proofs)
}


def sibling_fill(rgb, hole, key):
    """Hole pixels from the same camera's other captures, where those show no HUD (7 px clear of it). Each hole takes the sibling whose street
    best matches the ring of street round it (a ground ring or a passer-by differs between captures); returns the frame and what is left."""
    left = hole > 0; out = rgb.copy(); sibs = []
    for s in SIBS.get(key, []):
        h = np.array(Image.open(R + s + '.hud.png').convert('RGBA'))[:, :, 3]
        sibs.append((np.array(Image.open(R + s + '.png').convert('RGB')), cv2.dilate((h > 6).astype(np.uint8), np.ones((15, 15), np.uint8)) == 0))
    n, lab = cv2.connectedComponents(left.astype(np.uint8))
    for i in range(1, n):
        comp = lab == i; ring = (cv2.dilate(comp.astype(np.uint8), np.ones((17, 17), np.uint8)) > 0) & ~left
        order = sorted(sibs, key=lambda sc: float(np.abs(sc[0][ring & sc[1]].astype(float) - rgb[ring & sc[1]].astype(float)).mean()) if (ring & sc[1]).any() else 1e9)
        for im, clean in order: take = comp & left & clean; out[take] = im[take]; left &= ~take
    return out, left


CLONE = {'card': [((1470, 740, 1920, 950), (0, -204)), ((1400, 980, 1840, 1080), (-76, 26))]}  # holes with no sibling, by where they are: the clean
# plaza straight above the docked card; for the old lozenge's left end, the road and kerb further down-left along the kerb (its road edge measured at slope -0.348), its last rows
# from the screen's bottom edge reflected


def clone_fill(bgr, hole, offs):
    """Holes filled with a clean region of the same frame (by where each hole is: `offs` [(box, (dx, dy))]), blended in the gradient domain
    (cv2.seamlessClone), so a shadow or a warmer patch at the edge carries in smoothly; past the screen's edge the source reflects. Returns the
    frame and the holes it did not take."""
    n, lab, st, cen = cv2.connectedComponentsWithStats((hole > 0).astype(np.uint8)); rest = hole.copy(); H, W = hole.shape
    B = 8 + max(max(abs(dx), abs(dy)) for _, (dx, dy) in offs)
    big = cv2.copyMakeBorder(bgr, B, B, B, B, cv2.BORDER_REFLECT)
    for i in range(1, n):
        cx, cy = cen[i]; off = next((o for (x0, y0, x1, y1), o in offs if x0 <= cx <= x1 and y0 <= cy <= y1), None)
        if off is None or st[i, cv2.CC_STAT_AREA] < 60: continue
        dx, dy = off; src = big[B + dy:B + dy + H, B + dx:B + dx + W]; m = ((lab == i) * 255).astype(np.uint8); x, y, w, h = st[i, :4]
        m[0, :] = m[-1, :] = 0; m[:, 0] = m[:, -1] = 0
        bgr = cv2.seamlessClone(np.ascontiguousarray(src), bgr, m, (int(x + w // 2), int(y + h // 2)), cv2.NORMAL_CLONE); rest[lab == i] = 0
    return bgr, rest


def lift(key, boxes, keep=()):
    """The frame with the old pieces in `boxes` lifted (the HUD layer's pixels inside each box, dilated), filled from the same camera's other captures
    (SIBS), else a clean region of the same frame (CLONE) or the nearby street (round 1's patch fill), and the HUD layer's alpha with them removed (what stays of
    today's HUD, for the coverage). `keep` boxes are never touched (a built piece beside it)."""
    os.makedirs(SCR3 + 'frames', exist_ok=True); im, al = real(key); hole = np.zeros(al.shape, np.uint8)
    for x0, y0, x1, y1 in boxes: hole[y0:y1, x0:x1] = (al[y0:y1, x0:x1] > 3).astype(np.uint8) * 255  # the soft fringe too (it bled into a blend)
    yy, xx = np.mgrid[0:al.shape[0], 0:al.shape[1]]
    def unkeep(m):
        for k in keep:
            if len(k) == 3: m[(xx - k[0]) ** 2 + (yy - k[1]) ** 2 <= k[2] ** 2] = 0
            else: m[k[1]:k[3], k[0]:k[2]] = 0
    unkeep(hole); rest = al.copy(); rest[hole > 0] = 0
    tag_ = f"{key}-{abs(hash((tuple(boxes), tuple(keep), repr(CLONE.get(key)), tuple(SIBS.get(key, ())), repr(WHOLE.get(key)), key in FLAT, SIBCLONE.get(key)))) % 10 ** 8}"; p = SCR3 + f'frames/{tag_}.png'  # the fill's settings are in the key
    for (x0, y0, x1, y1), s_ in WHOLE.get(key, []): rest[y0:y1, x0:x1] = np.maximum(rest[y0:y1, x0:x1], np.array(Image.open(R + s_ + '.hud.png').convert('RGBA'))[y0:y1, x0:x1, 3])
    if os.path.exists(p): return Image.open(p).convert('RGB'), rest
    m = cv2.dilate(hole, np.ones((7, 7), np.uint8)); unkeep(m)
    if key in SIBCLONE:  # the sibling's street Poisson-blended in, hole by hole: a smooth tint between the two (a cone's wash) is absorbed at the seams
        a = np.array(im)[:, :, ::-1].copy(); sib = np.array(Image.open(R + SIBCLONE[key] + '.png').convert('RGB'))[:, :, ::-1].copy()
        n_, lab_, st_, _ = cv2.connectedComponentsWithStats((m > 0).astype(np.uint8))
        for i in range(1, n_):
            mk = ((lab_ == i) * 255).astype(np.uint8); mk[0, :] = mk[-1, :] = 0; mk[:, 0] = mk[:, -1] = 0; ys_, xs_ = np.where(mk > 0)
            if len(ys_): a = cv2.seamlessClone(sib, a, mk, (int((xs_.min() + xs_.max() + 1) // 2), int((ys_.min() + ys_.max() + 1) // 2)), cv2.NORMAL_CLONE)
        rgb, left = a[:, :, ::-1].copy(), np.zeros(m.shape, bool)
    else: rgb, left = sibling_fill(np.array(im), m, key)
    a = rgb[:, :, ::-1].copy()
    r_, g_, b_ = [rgb[:, :, i].astype(int) for i in range(3)]; was = np.array(im).astype(int); filled = (m > 0) & ~left
    sig = filled & ((((b_ - r_ > 40) & (g_ - r_ > 30)) | ((r_ - g_ > 90) & (r_ - b_ > 90))) & ~(((was[:, :, 2] - was[:, :, 0] > 40) & (was[:, :, 1] - was[:, :, 0] > 30)) | ((was[:, :, 0] - was[:, :, 1] > 90) & (was[:, :, 0] - was[:, :, 2] > 90))))
    left = (left | (cv2.dilate(sig.astype(np.uint8), np.ones((5, 5), np.uint8)) > 0) & (m > 0)).astype(np.uint8) * 255  # a sibling's ground ring or cone line is not this moment's street
    if left.any() and key in CLONE: a, left = clone_fill(a, left, CLONE[key])
    if left.any(): a = cv2.inpaint(a, left, 7, cv2.INPAINT_NS) if key in FLAT else patch_fill(a, left, D=240, pad=10, avoid=cv2.dilate((al > 8).astype(np.uint8) * 255, np.ones((9, 9), np.uint8)))  # never copy the HUD's own pixels
    for (x0, y0, x1, y1), s_ in WHOLE.get(key, []):  # a sibling's HUD layer laid over the filled holes (the built clock stack under an old plate)
        hl = np.array(Image.open(R + s_ + '.hud.png').convert('RGBA'))[y0:y1, x0:x1].astype(float); f = hl[:, :, 3:] / 255 * (m[y0:y1, x0:x1, None] > 0)
        a[y0:y1, x0:x1] = (a[y0:y1, x0:x1] * (1 - f) + hl[:, :, 2::-1] * f).astype(np.uint8); rest[y0:y1, x0:x1] = np.maximum(rest[y0:y1, x0:x1], hl[:, :, 3].astype(np.uint8))
    out = Image.fromarray(a[:, :, ::-1]); out.save(p); return out, rest


def layer():
    """A clear 2x layer for the new pieces (Page's drawing at 3840 x 2160)."""
    P = Page(2); P.img = Image.new('RGBA', (3840, 2160), (0, 0, 0, 0)); return P


def cover_px(P):
    """The new pieces' screen share at 1080 (alpha >= 24 on the HUD layer alone, as tools/hud-budget reads a .hud.png): pixel count."""
    return int((np.array(P.img.resize((1920, 1080), Image.LANCZOS))[:, :, 3] >= 24).sum())


def frame_cover(rest, P):
    """The whole frame's HUD share after the change: what stays of today's layer plus the new pieces (alpha >= 24 at 1080)."""
    a = np.array(P.img.resize((1920, 1080), Image.LANCZOS))[:, :, 3]; return float(((rest >= 24) | (a >= 24)).mean())


def old_px(key, box, pad=4):
    """An old piece's pixels on today's HUD layer (alpha >= 24 inside its box, a little out for the shadow)."""
    _, al = real(key); x0, y0, x1, y1 = box; return int((al[max(0, y0 - pad):y1 + pad, max(0, x0 - pad):x1 + pad] >= 24).sum())


def compose(frame_im, P):
    base = frame_im.resize((3840, 2160), Image.LANCZOS).convert('RGBA'); base.alpha_composite(P.img); return base


# ---------------------------------------------------------------- rule 1: words in the street
def label(P, x, y, icon, words, size=15.5, hot=False, soft=False, anchor='l', face_=None, isize=16):
    """A street label: a keyed pictogram (or a face) and its words in keyline Jost; urgent: orange on an ink keyline; soft: the head word's soft ink.
    (x, y) is the left end's middle (anchor 'l'), the centre ('m') or the right end ('r'). Returns the width at 1080."""
    fill = ORANGE if hot else SOFT if soft else INK; key = INK if hot else KEY; kw = 2.2 if hot else 2.6
    iw = (isize + 6) if (icon or face_) else 0; ww = P.tw(words, size, 'Jost-Bold') if words else 0; w = iw + ww
    x0 = x - (w if anchor == 'r' else w / 2 if anchor == 'm' else 0)
    if face_: disc_shadow(P, x0, y - isize / 2, isize, dy=1.5, a=60, blur=2); face(P, face_, x0, y - isize / 2, isize, ring=PAGE, rw=2)
    elif icon: kicon(P, icon, x0, y - isize / 2, isize, fill=fill, key=key, kw=2.0)
    if words: tag(P, x0 + iw, y, words, size, fill=fill, key=key, kw=kw)
    return w


def timed_disc(P, cx, cy, d, frac, hot, icon=None, who=None):
    """A round tab whose own cream ring is a clock (the built tabs' 2 px ring): `frac` of it from the top clockwise, the rest the body's ink;
    orange while it runs out. Its pictogram in cream, or a face."""
    x, y = cx - d / 2, cy - d / 2; disc_shadow(P, x, y, d, dy=2, a=60, blur=2.5); P.disc(x, y, d, fill=BODY3, ring=None)
    if who: face(P, who, x + 2.5, y + 2.5, d - 5, ring=None)
    elif icon: P.icon(icon, x + d * .22, y + d * .22, d * .56, col=PAGE)
    arc(P, cx, cy, d / 2 - 1.3, 0, 360, INK, 2.6)
    if frac > 0: arc(P, cx, cy, d / 2 - 1.3, 0, 360 * min(1.0, frac), ORANGE if hot else PAGE, 2.6)


def window_mark(P, cx, cy, left, total, icon='eye_shut', who=None, clock=None):
    """A window running out (the guard away, a camera blind or looped, the rivals' next move, Popovic's car): the window's pictogram on a round
    tab whose cream ring drains with it, the seconds beside it as a street label (m:ss past a minute); the last 10 s orange on ink, ring and
    figure (the seal's colour). Held open by an agent (a loop the Tech holds): the ring stays whole and the holder's face stands for the time."""
    hot = 0 <= left <= 10; frac = 1.0 if left < 0 else max(0.0, left / max(1, total)); timed_disc(P, cx, cy, 26, frac, hot, icon=icon)
    if who: face3(P, who, cx + 17, cy - 10, 20, rw=2); return
    t = clock or (f'{left // 60}:{left % 60:02d}' if left >= 60 else f'{left} s')
    tag(P, cx + 19, cy + 1, t, 17, fill=ORANGE if hot else INK, key=INK if hot else KEY, kw=2.2 if hot else 2.6)


def attention(P, cx, cy, frac, word):
    """A watcher's attention on our agent (the attention clock): the open eye on a round tab whose cream ring fills toward the report, the stage
    word beside it; from 'wary' the ring and the word turn orange on ink (the HUD's running-out colour, as the seal)."""
    hot = word in ('wary', 'reported'); timed_disc(P, cx, cy, 24, frac, hot, icon='eye_open')
    tag(P, cx + 18, cy + 1, word, 14.5, fill=ORANGE if hot else INK, key=INK if hot else KEY, kw=2.2 if hot else 2.6)


def pop(P, cx, cy, icon, words, badge_=None):
    """A pop (a refusal, a window closing, a report stopped): the pictogram and at most four words as a street label, centred on the thing.
    `badge_` puts a kit badge (mark_no ...) in place of the pictogram."""
    if badge_:
        w = 20 + P.tw(words, 15.5, 'Jost-Bold'); x0 = cx - w / 2; P.disc(x0 - 1.5, cy - 9.5, 19, fill=INK, ring=None); badge(P, badge_, x0, cy - 8, 16)
        tag(P, x0 + 20, cy, words, 15.5); return w
    return label(P, cx, cy, icon, words, anchor='m')


# ---------------------------------------------------------------- rule 2: what you click
def tab3(P, x, y, d, icon, col=None, ring=PAGE, rw=2):
    """A round tab (the built pause and turn buttons): the book's body, a 2 px cream ring, the pictogram at half its size, a soft shadow."""
    disc_shadow(P, x, y, d, dy=2, a=60, blur=2.5); P.disc(x, y, d, fill=BODY3, ring=ring, rw=rw); P.icon(icon, x + d * .24, y + d * .24, d * .52, col=col or PAGE)


def capsule(P, x, y, icon, word, struck=False, h=24, face_=None, live=False):
    """A way you can click that has a word (the fix chips of 0.2.287's news card): the book's body, a 1.4 px cream inline (turquoise while it acts,
    as the built fix chip with an agent), the doer's face and/or a cream pictogram, the word (Jost Bold 12.5); a shut way struck through.
    (x, y) top left. Returns its width."""
    tw = P.tw(word, 12.5, 'Jost-Bold') if word else 0; iw = (18 if face_ else 0) + (15 if icon else 0) + (3 if face_ and icon else 0)
    w = (6 if face_ else 9) + iw + (5 if word and iw else 0) + tw + (10 if word else 8)
    shadow(P, x, y, w, h, h / 2, dy=1.5, a=50, blur=1.6); P.rrect(x, y, w, h, h / 2, fill=BODY3)
    P.rrect(x + .7, y + .7, w - 1.4, h - 1.4, h / 2 - .7, line=TURQ if live else PAGE, lw=1.4); cx = x + (6 if face_ else 9)
    if face_: face(P, face_, cx - 2, y + h / 2 - 9, 18, ring=PAGE, rw=1.2); cx += 18 + (3 if icon else 0)
    if icon: P.icon(icon, cx, y + h / 2 - 6.5, 13, col=PAGE); cx += 15
    if word: P.text(cx + (5 if iw else 0), y + h / 2 + .5, word, 12.5, col=PAGE, face='Jost-Bold')
    if struck: P.line([(x + 6, y + h / 2 + 1), (x + w - 6, y + h / 2 + 1)], col=PAGE, lw=1.6)
    return w


def chevron(P, x, cy, side='r', col=INK):
    g = ImageDraw.Draw(P.img); s = P.s
    pts = [(x, cy - 8), (x + 10, cy), (x, cy + 8)] if side == 'r' else [(x, cy - 8), (x - 10, cy), (x, cy + 8)] if side == 'l' else \
          [(x - 8, cy), (x, cy - 10), (x + 8, cy)] if side == 'u' else [(x - 8, cy), (x, cy + 10), (x + 8, cy)]
    g.polygon([(a * s, b * s) for a, b in pts], fill=col)


def edge_disc(P, x, y, d, icon=None, badge_=None, side='r'):
    """Something off screen that isn't a face (a reroute, the rival car, a lock): a round tab with its pictogram or badge at the screen's edge,
    an ink chevron pointing out (the built edge faces' chevron)."""
    if badge_: disc_shadow(P, x, y, d, dy=2, a=60, blur=2.5); P.disc(x, y, d, fill=BODY3, ring=PAGE, rw=2); badge(P, badge_, x + d * .18, y + d * .18, d * .64)
    else: tab3(P, x, y, d, icon)
    cy = y + d / 2
    if side == 'r': chevron(P, x + d + 3, cy, 'r')
    elif side == 'l': chevron(P, x - 3, cy, 'l')
    elif side == 'u': chevron(P, x + d / 2, y - 3, 'u')
    else: chevron(P, x + d / 2, y + d + 3, 'd')


# ---------------------------------------------------------------- rule 4: faces
def face3(P, who, x, y, d, badge_=None, rw=2.5):
    """The built face: a cream ring over a soft shadow; `badge_` (mark_ok, mark_half, mark_no) at its lower right."""
    disc_shadow(P, x, y, d, dy=2, a=60, blur=2.5); face(P, who, x, y, d, ring=PAGE, rw=rw)
    if badge_: bd = round(d * .42); P.disc(x + d - bd + 2 - 1.2, y + d - bd + 2 - 1.2, bd + 2.4, fill=PAGE, ring=None); badge(P, badge_, x + d - bd + 2, y + d - bd + 2, bd)


def stem(P, x, y0, y1, dot=True):
    """The fine cream stem from a piece down to the head it belongs to (the bubbles' stem), on an ink keyline."""
    P.line([(x, y0), (x, y1)], col=(0x0b, 0x16, 0x15, 130), lw=4.0); P.line([(x, y0), (x, y1)], col=PAGE, lw=2.2)
    if dot: P.disc(x - 2.6, y1 - 2.6, 5.2, fill=PAGE, ring=INK, rw=1.2)


# ---------------------------------------------------------------- the composite pieces
def done_chip(P, cx, y, who, tone, facts, verbs=()):
    """What an act gave, over the thing that changed, 4.2 s: the doer's face with the tone's badge, one keyed pictogram and word per fact (at most
    three), then the ways it opened as capsules (a shut one struck). Centred on cx; y is the face's top. Returns (x0, y0, x1, y1)."""
    d = 30; fw = [16 + 6 + P.tw(t, 15, 'Jost-Bold') for _, t in facts]; vw = [9 + 15 + 5 + P.tw(w, 12.5, 'Jost-Bold') + 10 for _, w, _ in verbs]
    body = max(fw + [sum(vw) + 6 * max(0, len(vw) - 1) + (14 if vw else 0)]); w = d + 10 + body; x0 = cx - w / 2
    face3(P, who, x0, y, d, badge_={'ok': 'mark_ok', 'half': 'mark_half', 'no': 'mark_no'}.get(tone))
    rows = len(facts) + (1 if verbs else 0); ty = y + d / 2 - (rows - 1) * 13
    for icon, t in facts: label(P, x0 + d + 10, ty, icon, t, size=15); ty += 26
    if verbs:
        vx = x0 + d + 10; tag(P, vx, ty, '›', 15); vx += 14
        for icon, word, shut in verbs: vx += capsule(P, vx, ty - 12, icon, word, struck=shut) + 6
    return (x0, y - 4, x0 + w, y + max(d, rows * 26))


def callout(P, cx, cy, d, inner, agent=None, ticks=0, to=None, red_eye=False):
    """The close-up callout (§9 A) in the built kit: the watcher's face (or a camera's eye in the cones' red) in a cream ring over a soft shadow,
    the watched agent's face at its lower right with 1-3 ticks for the stage, a fine stem down to the watcher's head (no chrome tail)."""
    x, y = cx - d / 2, cy - d / 2
    if to: stem(P, cx, y + d - 2, to, dot=True)
    disc_shadow(P, x, y, d, dy=3, a=70, blur=3.5)
    if red_eye: P.disc(x, y, d, fill=BODY3, ring=PAGE, rw=2.5); P.icon('eye_open', x + d * .2, y + d * .2, d * .6, col=RED)
    else: face(P, inner, x, y, d, ring=PAGE, rw=2.5)
    if agent:
        ad = round(d * .4); bx, by = x + d - ad + 4, y + d - ad + 2; face3(P, agent, bx, by, ad, rw=2)
        for i in range(ticks): a0 = 20 + i * 34; arc(P, bx + ad / 2, by + ad / 2, ad / 2 + 3.4, a0, a0 + 26, INK, 4.6); arc(P, bx + ad / 2, by + ad / 2, ad / 2 + 3.4, a0, a0 + 26, ORANGE, 2.6)


def slip(P, x, y, who, name, learned, opens, to=None, new='New'):
    """A conversation's card, folded into the street: a slip of the book's page (cream, ink Jost, the hairline), the person's face breaking its
    top-left corner, each learned row led by its pictogram (Fable's label word becomes the icon; the rest is Fable's line as written), then the ways
    it opened as capsules beside their words, "New" in the book's deep teal. A fine stem to her head. (x, y) top left. Returns its box."""
    def split(t):
        k, _, rest = t.partition(':'); return k.strip().lower(), rest.strip()
    rows_l = [split(t) for t in learned]; rows_o = [split(t) for t in opens]; d = 44; lh = 23; ox = 22; ts = 15
    wl = [22 + P.tw(r, ts, 'Jost-Medium') for _, r in rows_l]; wo = [9 + 15 + 5 + P.tw(k.capitalize(), 12.5, 'Jost-Bold') + 10 + 8 + P.tw(r, ts, 'Jost-Medium') for k, r in rows_o]
    w = ox + max(wl + wo + [P.tw(name, 13.5, 'Jost-SemiBold') + 40]) + 16 + 30; h = 14 + 15 + lh * len(rows_l) + (10 if rows_o else 0) + lh * len(rows_o) + 6
    if to: stem(P, to[0], y + h - 2, to[1])
    shadow(P, x, y, w, h, 14, dy=3, a=64, blur=4); P.rrect(x, y, w, h, 14, fill=PAGE)
    face3(P, who, x - 14, y - 16, d); tx = x + d - 6 + 4
    P.text(tx, y + 12, name, 13.5, col=SOFT, face='Jost-SemiBold'); P.words_add(name); ty = y + 14 + 17
    for k, r in rows_l: P.icon(k, tx, ty - 8, 16, col=INK); P.text(tx + 22, ty, r, ts, col=INK, face='Jost-Medium'); P.words_add(r); ty += lh
    if rows_o:
        P.line([(tx, ty - 7), (x + w - 18, ty - 7)], col=HAIR3, lw=1.2); ty += 4
        P.text(x + 10, ty + 1, new, 12.5, col=DEEP, face='Jost-Bold')
        for k, r in rows_o:
            cw = capsule(P, tx, ty - 11, k, k.capitalize(), h=22); P.text(tx + cw + 8, ty + .5, r, ts, col=INK, face='Jost-Medium'); P.words_add(r); ty += lh
    return (x - 14, y - 16, x + w, y + h)


def next_row(P, right, cy, word, clock, unknown=False):
    """The next key event as built (HudToolkit.Clock): its icon, its name and its time, right-aligned. Returns the row's width."""
    tw = P.tw(clock, 17, 'Jost-Bold'); ww = P.tw(word, 15, 'Jost-SemiBold') if word else 0
    x = right - tw; tag(P, x, cy, clock, 17)
    if word: x -= 6 + ww; tag(P, x, cy, word, 15, face_='Jost-SemiBold')
    x -= 6 + 18; kicon(P, 'nextevent', x, cy - 9, 18, fill=DEEP)
    return right - x
