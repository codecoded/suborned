"""Brief 101, take control (Glyph): a click for the ground view, mocked in the built street kit (Kit/street.uss; rounds 2 and 3's rules) over
real frames. One slot beside the selected agent's face, right of her pips, level with her face:
  rest    : on the map, a 30 px round tab with the take_control pictogram (the built Tab: the book's body, a 2 px cream ring, the pictogram at 0.52);
  hover   : its ring turquoise (the built Tab's hover), its word and key as a street label: "Take control · T"; a face you point at shows its own;
  control : in third person the same slot is the way back, a capsule (it has a word: rule 2) with the map pictogram and Fable's "Back to map";
            the Squad tab no longer doubles as it; over her head one keyed eye (who sees her: GDD §10's, not built), her act on her face's disc;
  back    : the capsule hovered: its inline turquoise, "T · Esc" in the soft ink.
Frames: 0.2.288's t101-quiet captured at 2x (route G, 3:28.6, the Scout selected) and 0.2.289's hold-tp-keeps-walk (third person on the
Scout, walking the plaza to the Office with the Enforcer, 0:01). The Squad tab stays Squad in both views (SelectAll keeps the driven agent). Per frame the HUD's share as tools/hud-budget reads a .hud.png (alpha >= 24 at 1080), before and after.
Writes briefs/101/glyph/tc/<state>.png, 2x copies and crops in out/scratch/glyph/101tc/, and make/coverage.json. Glyph. Mock only."""
import os, sys, json
import cv2
sys.path.insert(0, '/Users/sky/Projects/suborned/briefs/101/glyph/r3/make')
from r3_base import *  # Page, tab3, capsule, tag, kicon, face3, disc_shadow, shadow, lift, real, layer, compose, cover_px, frame_cover, old_px ...

OUTT = R + 'briefs/101/glyph/tc/'; SCRT = R + 'out/scratch/glyph/101tc/'; ICONS = OUTT + 'icons/'
HOT = (0x60, 0xe0, 0xea, 255)  # --book-turq: the built tabs' hover ring
MAP2X = R + 'out/scratch/latch/101r3/shots-288/t101-quiet-2x'  # 0.2.288, the HUD at panel scale 2
DRIVE = (R + 'out/build/play/proof/hold-tp-keeps-walk', '0.2.289', 'in third person on the Scout, walking the plaza to the Office, 0:01 (the hold proof)')
FACE = lambda i: (52, 52 + 86 * i)  # a face's centre on screen (HudToolkit.Faces: FaceScreen), 60 px across
TAB_D, TAB_X = 30, 100  # the slot: 30 px, its left edge 7 px right of the pips (x 86-93)
COV = {}


def ticon(P, name, x, y, size, col=PAGE):
    """One of this round's two pictograms (tc_icons.py), tinted, at (x, y) top left."""
    a = Image.open(ICONS + name + '@2x.png').getchannel('A').resize((round(size * P.s),) * 2, Image.LANCZOS)
    t = Image.new('RGBA', a.size, col[:3] + (0,)); t.putalpha(a); P.img.alpha_composite(t, (round(x * P.s), round(y * P.s)))


def tc_tab(P, i, hot=False):
    """The take-control tab beside face i, as the built Tab draws: the shadow disc 2 px down at 0.6, the body, a 2 px ring (turquoise hovered)."""
    cx, cy = FACE(i); x, y, d = TAB_X, cy - TAB_D / 2, TAB_D
    disc_shadow(P, x, y, d, dy=2, a=60, blur=2.5); P.disc(x, y, d, fill=BODY3, ring=HOT if hot else PAGE, rw=2)
    s = d * .52; ticon(P, 'take_control', x + (d - s) / 2, y + (d - s) / 2, s)
    return (x, y, d, d)


def tc_word(P, i, word='Take control', key='T'):
    """Its hover word: a street label (ink Jost Bold on the cream keyline), the key after it in the soft ink."""
    cx, cy = FACE(i); x = TAB_X + TAB_D + 8; w = tag(P, x, cy, word, 15); w2 = tag(P, x + w + 7, cy, f'· {key}', 15, fill=SOFT)
    return (x - 3, cy - 12, w + w2 + 13, 24)


def back_capsule(P, i, hot=False, word='Back to map', keys='T · Esc'):
    """The way back, in the tab's slot: a capsule 30 px tall (the fix chip's form: body, 1.4 px cream inline, turquoise hovered), the map, the word."""
    cx, cy = FACE(i); h = TAB_D; s = h * .52; tw = P.tw(word, 14, 'Jost-Bold'); w = 9 + s + 6 + tw + 13; x, y = TAB_X, cy - h / 2
    shadow(P, x, y, w, h, h / 2, dy=2, a=60, blur=2.5); P.rrect(x, y, w, h, h / 2, fill=BODY3)
    P.rrect(x + .8, y + .8, w - 1.6, h - 1.6, h / 2 - .8, line=HOT if hot else PAGE, lw=2.0 if hot else 1.4)
    ticon(P, 'map', x + 9, cy - s / 2, s); P.text(x + 9 + s + 6, cy + .5, word, 14, col=PAGE, face='Jost-Bold'); P.words_add(word)
    box = (x, y, w, h)
    if hot: kw = tag(P, x + w + 8, cy, keys, 14, fill=SOFT); box = (x, y, w + 8 + kw + 3, h)
    return box


def cursor(P, x, y):
    """The pointer, its tip at (x, y): white with an ink edge."""
    pts = [(0, 0), (0, 17), (4.2, 13.2), (7.2, 19.6), (10, 18.4), (7.1, 12.2), (12.4, 12.2)]; g = ImageDraw.Draw(P.img); s = P.s
    g.polygon([((x + a) * s, (y + b) * s) for a, b in pts], fill=INK, outline=INK, width=round(2.6 * s)); g.polygon([((x + a) * s, (y + b) * s) for a, b in pts], fill=(255, 255, 255, 255))


def sidearm(P, cx, cy, d=22):
    """The face's sidearm mark as built when holstered (HudToolkit.Faces: 22 px, the body, a 2 px cream ring, the gun at 13 px, 0.6)."""
    disc_shadow(P, cx - d / 2, cy - d / 2, d, dy=1.5, a=50, blur=2); P.disc(cx - d / 2, cy - d / 2, d, fill=BODY3, ring=PAGE, rw=2); P.icon('fire', cx - 8.5, cy - 8.5, 13, col=PAGE, alpha=.8)


def eye(P, cx, cy, state='shut', d=22):
    """Who sees her (GDD §10 ground view, not built): one keyed eye over the driven agent; open in the cones' red, shut and could-be in ink."""
    name = {'shut': 'eye_shut', 'open': 'eye_open', 'unk': 'eye_unk'}[state]; kicon(P, name, cx - d / 2, cy - d / 2, d, fill=RED if state == 'open' else INK)


def map_frame():
    return Image.open(MAP2X + '.png').convert('RGB'), np.array(Image.open(R + 'out/scratch/latch/101r3/shots-288/t101-quiet.hud.png').convert('RGBA'))[:, :, 3]


def save(name, im, L, rest, crops=()):
    """The frame at 1080 in the brief, the 2x frame and its crops in scratch; the HUD's share after (today's layer as left plus the new pieces)."""
    os.makedirs(SCRT, exist_ok=True); os.makedirs(OUTT, exist_ok=True)
    im.resize((1920, 1080), Image.LANCZOS).convert('RGB').save(OUTT + name + '.png'); im.convert('RGB').save(SCRT + name + '-2x.png')
    for cname, (x0, y0, x1, y1) in crops: im.crop((x0 * 2, y0 * 2, x1 * 2, y1 * 2)).convert('RGB').save(SCRT + f'{name}-{cname}-2x.png')
    return frame_cover(rest, L)


def rest_and_hover():
    im0, al = map_frame(); before = float((al >= 24).mean()); out = {}
    for state in ('rest', 'hover'):
        L = layer(); boxes = [('take control tab', tc_tab(L, 0, hot=state == 'hover'))]
        if state == 'hover': boxes.append(('take control word', tc_word(L, 0))); cursor(L, TAB_X + 19, FACE(0)[1] + 4)
        im = compose(im0, L); after = save(state, im, L, al, [('roster', (0, 0, 300, 420))])
        out[state] = dict(build='0.2.288', moment='route G, 3:28.6, the Scout selected (t101-quiet, captured at 2x)', before_pct=round(before * 100, 2), after_pct=round(after * 100, 2), new=boxes, layer=L, rest=al)
    # the pointer on another face: the Tech's own tab shows (one click takes him); his sidearm mark shows on hover, as built (the Scout's, copied)
    L = layer(); fx, fy = FACE(2); sidearm(L, fx - 23, fy + 21); tc_tab(L, 2, hot=True); tc_word(L, 2); cursor(L, TAB_X + 19, fy + 4)
    compose(im0, L).crop((0, 300, 600, 640)).convert('RGB').save(SCRT + 'hover-other-roster-2x.png')
    return out


def unlift(src, word_box, chev_box):
    """The control frame without the Squad slot's old word and the chevron over her head. Under the word, the street is recovered from the
    capture and its own HUD layer (world = (frame - hud * a) / (1 - a) where the HUD is not opaque) and only the opaque strokes are inpainted
    (thin, so the facade's band and mullions carry on); the chevron is copied over from the paving 40 px to its right. Returns the frame and
    what stays of today's HUD alpha."""
    png = np.array(Image.open(src + '.png').convert('RGB')).astype(float); hud = np.array(Image.open(src + '.hud.png').convert('RGBA')).astype(float)
    a = hud[:, :, 3:] / 255.0; rest = hud[:, :, 3].astype(np.uint8).copy(); x0, y0, x1, y1 = word_box; sub = (slice(y0, y1), slice(x0, x1))
    w = (png[sub] - hud[sub][:, :, :3] * a[sub]) / np.maximum(1e-3, 1 - a[sub]); ok = a[sub][:, :, 0] <= 0.6
    reg = png[sub].copy(); reg[ok] = np.clip(w[ok], 0, 255); png[sub] = reg; rest[sub] = 0
    m = np.zeros(rest.shape, np.uint8); m[sub] = cv2.dilate((~ok).astype(np.uint8) * 255, np.ones((3, 3), np.uint8))
    out = cv2.inpaint(png.astype(np.uint8)[:, :, ::-1].copy(), m, 4, cv2.INPAINT_NS)
    cx0, cy0, cx1, cy1 = chev_box; mc = np.zeros(rest.shape, np.uint8); mc[cy0:cy1, cx0:cx1] = (hud[cy0:cy1, cx0:cx1, 3] > 3).astype(np.uint8) * 255
    mc = cv2.dilate(mc, np.ones((7, 7), np.uint8)); f = cv2.GaussianBlur((mc > 0).astype(np.float32), (0, 0), 1.2)[:, :, None]
    out = (out * (1 - f) + np.roll(out, -40, axis=1) * f).astype(np.uint8); rest[cy0 - 4:cy1 + 4, cx0 - 4:cx1 + 4] = 0
    return Image.fromarray(out[:, :, ::-1]), rest


def control():
    word, chev = (74, 370, 186, 402), (944, 588, 978, 616)
    frame_im, rest = unlift(DRIVE[0], word, chev); al = np.array(Image.open(DRIVE[0] + '.hud.png').convert('RGBA'))[:, :, 3]; before = float((al >= 24).mean()); out = {}
    for state in ('control', 'back'):
        L = layer(); boxes = [('back to map capsule', back_capsule(L, 0, hot=state == 'back'))]
        eye(L, 960, 600, 'unk', d=24); boxes.append(('sight eye over her', (948, 588, 24, 24)))
        if state == 'back': cursor(L, TAB_X + 62, FACE(0)[1] + 4)
        im = compose(frame_im, L); after = save(state, im, L, rest, [('roster', (0, 0, 300, 420)), ('head', (840, 520, 1080, 720))])
        out[state] = dict(build=DRIVE[1], moment=DRIVE[2], before_pct=round(before * 100, 2), after_pct=round(after * 100, 2), new=boxes, layer=L, rest=rest,
                          lifted={'Squad slot word "Back to map · T"': word, 'selection chevron over her head': chev})
    return out


def eyes_strip():
    """The eye's three states over the frame's own paving, at 2x: unseen (shut, ink), seen (open, red), could be (?, ink)."""
    im = Image.open(DRIVE[0] + '.png').convert('RGB').crop((300, 760, 600, 820)).resize((600, 120), Image.LANCZOS)
    P = Page(2, 300, 60); P.img = im.convert('RGBA')
    for k, st in enumerate(('shut', 'open', 'unk')): eye(P, 50 + k * 100, 30, st, d=24)
    P.img.convert('RGB').save(SCRT + 'eyes-2x.png')


def main():
    res = {**rest_and_hover(), **control()}; eyes_strip()
    doc = dict(method='HUD layer alone, alpha >= 24 at 1920 x 1080 (tools/hud-budget); before: the capture .hud.png; after: what stays of it plus the new pieces',
               frames={k: {a: b for a, b in v.items() if a not in ('layer', 'rest')} for k, v in res.items()},
               pieces={k: [(n, round(cover_px_box(v['layer'], b) / 20736, 3)) for n, b in v['new']] for k, v in res.items()})
    for k, v in res.items(): print(f"{k:8} {v['build']}  HUD {v['before_pct']:.2f} % -> {v['after_pct']:.2f} %")
    json.dump(doc, open(OUTT + 'make/coverage.json', 'w'), indent=1); COV.update(res); return res


def cover_px_box(L, box):
    """A new piece's share: the layer's alpha >= 24 at 1080 inside its box (+3 px for the shadow)."""
    x, y, w, h = box; a = np.array(L.img.resize((1920, 1080), Image.LANCZOS))[:, :, 3]
    return int((a[max(0, round(y) - 3):round(y + h) + 3, max(0, round(x) - 3):round(x + w) + 3] >= 24).sum())


if __name__ == '__main__':
    main()
