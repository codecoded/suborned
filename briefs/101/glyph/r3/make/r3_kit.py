"""Brief 101, round 3 (Glyph): the rest of the old kit, piece by piece. Each piece is lifted from its own real capture (0.2.286 or 0.2.287) and
drawn again in the built street kit; before and after are cropped at 2x side by side, by family, onto out/scratch/glyph/101r3/kit-<family>.png,
and each piece's screen share is measured as in r3_frames (make/coverage-kit.json). Glyph. Mock only."""
import os, sys, re
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from r3_base import *

for k, p in {'burning': 'out/build/play/proof/car-burning', 'busyprompt': 'out/build/play/proof/r083-busy-prompt', 'backplan': 'out/batch/proofset/r067/r067-back-to-plan',
             'shutpop': 'out/batch/proofset/r067/r067-shut-pop', 'rest': 'out/scratch/latch/r4/shots-final/t101-rest', 'office': 'out/batch/shots/office-front-yaw210',
             'round': 'out/batch/proofset/r059/r059-613-round', 'walkin': 'out/build/play/proof/done-walkin', 'aim': 'out/scratch/latch/r4/shots-final/t101-aim',
             'square': 'out/batch/proofset/r059/r059-235-square', 'reticle': 'out/batch/proofset/r067/r067-reticle-edge', 'queued': 'out/scratch/latch/r4/proof-r101-final/r101-queued-walk',
             'cones': 'out/batch/proofset/r067/r067-cones-before', 'busyaim': 'out/scratch/latch/r4/shots-final/t101-busy-aim'}.items():
    SHOTS[k] = (R + p, '0.2.287' if 'proof-r101-final' in p else '0.2.286', k)  # Latch's shots-final are 0.2.286 captures (r4.html); the r101 proof is 0.2.287's


WHOLE['cones'] = [((1640, 56, 1920, 160), 'out/batch/proofset/r067/r067-cones-after')]  # the clock stack under the old Service door plate: the next frame's HUD layer
FLAT.add('round')  # the reported clock sits on flat shadowed ground
SIBCLONE['cones'] = 'out/batch/proofset/r067/r067-cones-after'  # the next frame, cones off: about 10 levels less red (the cone's wash); blended, not copied
SIBS.update({  # captures of the same camera (median difference 0-4 grey levels at 192 x 108), another moment or state
    'burning': ['out/build/play/proof/car-blast'],
    'busyprompt': ['out/build/play/proof/r083-acting-not-selected', 'out/build/play/proof/r083-acting-selected'],
    'backplan': ['out/scratch/latch/101/proof/r067-back-to-plan', 'out/scratch/latch/101/proof3/r067-back-to-plan'],
    'shutpop': ['out/batch/proofset/r067/r067-lock-pill-guard', 'out/batch/proofset/r067/r067-shut-ring-guard', 'out/batch/proofset/r067/r067-stack-tab'],
    'walkin': ['out/build/play/proof/done-help-pip', 'out/build/play/proof/done-listen-chip'],  # cones: none (its next frame draws the cones over the facade)
    'queued': ['out/scratch/latch/101/b2/r101-queued-walk', 'out/scratch/latch/101/proof/r101-queued-walk'],
    'aim': ['out/build/play/proof/r085-guard-enforcer', 'out/build/play/proof/r085-guard-hover', 'out/build/play/proof/r085-guard-ring'],
    'square': ['out/scratch/latch/101/proof/r059-235-square', 'out/scratch/latch/101/proof3/r059-235-square'],
    'reticle': ['out/scratch/latch/098j/proof/r067-reticle-edge', 'out/scratch/latch/098t/proof/r067-reticle-edge', 'out/batch/proofset/r067/r067-stack-tab'],
    'busyaim': ['out/scratch/latch/r4/shots-final/t101-busy'],
    'office': ['out/batch/shots-prev2/office-front-yaw210', 'out/scratch/latch/101/shots/t101-loud'], 'rest': ['out/scratch/latch/101/shots/t101-loud', 'out/scratch/latch/101/shots/t101-quiet']})


def struck_radio(P, cx, cy):
    """'Report stopped': the report's own disc (its way's pictogram), no longer orange, struck through, and the words."""
    tw = P.tw('Report stopped', 15.5, 'Jost-Bold'); x0 = cx - (24 + 6 + tw) / 2
    disc_shadow(P, x0, cy - 12, 24, dy=1.5, a=60, blur=2); P.disc(x0, cy - 12, 24, fill=INK, ring=PAGE, rw=1.8); P.icon('radio', x0 + 5, cy - 7, 14, col=PAGE)
    P.line([(x0 + 3, cy + 9), (x0 + 21, cy - 9)], col=INK, lw=4.6); P.line([(x0 + 3, cy + 9), (x0 + 21, cy - 9)], col=PAGE, lw=2.2)
    tag(P, x0 + 30, cy, 'Report stopped', 15.5)


def dots(P, x, y, n):
    for i in range(n): P.disc(x + i * 9 - 1.4, y - 4.4, 8.8, fill=KEY, ring=None); P.disc(x + i * 9, y - 3, 6, fill=INK, ring=None)


def stash(P, px, py, name, n, below=True):
    """A stash place: its pictogram, its name and its free room as dots, on a fine stem from its point; under the point when the clock's
    keep-out takes the space above it (the old plates sat over the clock's stats row)."""
    w = 22 + P.tw(name, 14.5, 'Jost-Bold') + 8 + n * 9; y = py + 30 if below else py - 30
    P.line([(px, py), (px, y - 11 if below else y + 11)], col=(0x0b, 0x16, 0x15, 130), lw=3.6); P.line([(px, py), (px, y - 11 if below else y + 11)], col=PAGE, lw=1.8)
    P.disc(px - 3, py - 3, 6, fill=PAGE, ring=INK, rw=1.2); x0 = px - w / 2; lw_ = label(P, x0, y, 'stash', name, size=14.5); dots(P, x0 + lw_ + 8, y, n)


def ghost(P, cx, cy, who):
    label(P, cx, cy, None, 'Walk', size=14.5, soft=True, face_=who, isize=20, anchor='m')


def hint(P, cx, cy):
    t = 'Walk, or leave its sight.'; w = 24 + 6 + 16 + 6 + P.tw(t, 16, 'Jost-Bold'); x0 = cx - w / 2
    face3(P, 'fixer', x0, cy - 12, 24, rw=2); label(P, x0 + 30, cy, 's_moving', t, size=16)


def pip(P, x, y, name, d=26):
    """A pip over a person: the person's kit badge cropped round, in the built cream ring over a soft shadow (the stem below it stays)."""
    im = Image.open(R + f'unity/Assets/Suborned/UI/Resources/Kit/badges/{name}@2x.png').convert('RGBA'); i = round(im.width * .1); im = im.crop((i, i, im.width - i, im.height - i))
    n = round(d * P.s); m = Image.new('L', (n * 3, n * 3), 0); ImageDraw.Draw(m).ellipse((0, 0, n * 3 - 1, n * 3 - 1), fill=255); s = im.resize((n, n), Image.LANCZOS)
    a = ImageChops.multiply(s.getchannel('A'), m.resize((n, n), Image.LANCZOS)); s.putalpha(a)
    disc_shadow(P, x, y, d, dy=2, a=60, blur=2.5); P.disc(x, y, d, fill=BODY3, ring=None); P.img.alpha_composite(s, (round(x * P.s), round(y * P.s))); P.disc(x, y, d, fill=None, ring=PAGE, rw=2.2)


def shooters(P):
    face3(P, 'fixer', 1109, 626, 25, rw=2); face3(P, 'tech', 1134, 626, 25, rw=2)


# (family, piece, capture, box lifted, crop shown, draw) — the box is the old piece; several pieces of one capture share its lift
KIT = [
    ('Pops: the icon and its words on the street', 'No such order', 'burning', (539, 433, 685, 477), (500, 425, 720, 490), lambda P: pop(P, 612, 455, None, 'No such order', badge_='mark_no')),
    ('Pops: the icon and its words on the street', 'Click again to stop', 'busyprompt', (879, 357, 1048, 401), (850, 340, 1080, 420), lambda P: pop(P, 963, 379, 'pointer', 'Click again to stop')),
    ('Pops: the icon and its words on the street', 'Take control: T (a key in the words: Fable)', 'backplan', (888, 405, 1032, 449), (850, 390, 1070, 470), lambda P: pop(P, 960, 427, None, 'Take control: T', badge_='mark_no')),
    ('Pops: the icon and its words on the street', 'a shut way, no words', 'shutpop', (1454, 666, 1516, 710), (1420, 630, 1550, 720), lambda P: pop(P, 1485, 688, None, '', badge_='mark_no')),
    ('Pops: the icon and its words on the street', 'Report stopped (on the reporter, illustrative)', 'quiet', (862, 386, 947, 424), (820, 360, 1000, 450), lambda P: struck_radio(P, 912, 404)),
    ('Window markers: a ring that drains, the time beside it', '55 s', 'rest', (926, 378, 995, 424), (880, 360, 1040, 440), lambda P: window_mark(P, 942, 401, 55, 180)),
    ('Window markers: a ring that drains, the time beside it', 'the last 10 s (illustrative)', 'rest', None, (880, 360, 1040, 440), lambda P: window_mark(P, 942, 401, 8, 180)),
    ('Window markers: a ring that drains, the time beside it', 'a loop the Tech holds (illustrative)', 'rest', None, (880, 360, 1040, 440), lambda P: window_mark(P, 942, 401, -1, 180, who='tech')),
    ('Window markers: a ring that drains, the time beside it', '1:45', 'office', (919, 378, 1001, 424), (880, 360, 1040, 440), lambda P: window_mark(P, 938, 401, 105, 180)),
    ('Attention clocks: a ring that fills, the stage beside it', 'wary', 'burning', (553, 399, 653, 435), (520, 380, 690, 450), lambda P: attention(P, 568, 417, .78, 'wary')),
    ('Attention clocks: a ring that fills, the stage beside it', 'reported', 'round', (1538, 362, 1638, 398), (1500, 340, 1680, 420), lambda P: (stem(P, 1596.5, 360, 400, dot=False), attention(P, 1553, 380, 1.0, 'reported'))),  # the bubble's stem ran behind the old plate
    ('Done chips: the face and its ticks, the facts as labels, the ways as capsules', 'Uneasy › Listen, Pay', 'walkin', (842, 222, 1077, 318), (800, 200, 1110, 330), lambda P: done_chip(P, 960, 236, 'fixer', 'ok', [('mood_uneasy', 'Uneasy')], verbs=[('listen', 'Listen', False), ('pay', 'Pay', False)])),
    ('Edge chips: a round tab and the ink chevron, as the edge faces', 'reroute (off the top)', 'aim', (1414, 22, 1476, 60), (1380, 0, 1510, 80), lambda P: edge_disc(P, 1432, 30, 26, icon='reroute', side='u')),
    ('Edge chips: a round tab and the ink chevron, as the edge faces', 'the rival car (off the right)', 'square', (1840, 481, 1900, 511), (1780, 450, 1920, 540), lambda P: edge_disc(P, 1864, 483, 26, badge_='signal_rival', side='r')),
    ('Edge chips: a round tab and the ink chevron, as the edge faces', 'the lock (off the top)', 'reticle', (97, 9, 157, 39), (70, 0, 190, 70), lambda P: edge_disc(P, 115, 16, 24, icon='lock', side='u')),
    ('Plan ghosts, stash plates and the hint line: street labels', 'a queued walk', 'queued', (1191, 255, 1275, 301), (1150, 235, 1310, 320), lambda P: ghost(P, 1233, 278, 'scout')),
    ('Plan ghosts, stash plates and the hint line: street labels', 'a queued walk', 'queued', (502, 393, 586, 439), (460, 375, 630, 455), lambda P: ghost(P, 544, 416, 'tech')),
    ('Plan ghosts, stash plates and the hint line: street labels', 'stash: Dumpster, Service door', 'cones', (1532, 54, 1813, 160), (1500, 40, 1840, 230), lambda P: (stash(P, 1630, 131, 'Dumpster', 1), stash(P, 1678, 157, 'Service door', 2))),
    ('Plan ghosts, stash plates and the hint line: street labels', 'the hint line', 'cones', (843, 1022, 1077, 1070), (800, 1000, 1120, 1080), lambda P: hint(P, 960, 1046)),
    ("Faces: the built cream ring for every chrome one", "the shooters' faces", 'busyaim', (1104, 620, 1175, 658), (990, 600, 1190, 680), shooters),
    ("Faces: the built cream ring for every chrome one", 'a pip over a person (the driver)', 'aim', (1655, 702, 1685, 732), (1600, 670, 1740, 770), lambda P: pip(P, 1659, 706, 'npc_driver', d=22)),  # the old disc's visible size inside its 26 px box
]


def main():
    lifts, rows = {}, []
    for key in dict.fromkeys(k for _, _, k, _, _, _ in KIT):
        boxes = [b for _, _, k, b, _, _ in KIT if k == key and b]; lifts[key] = lift(key, boxes)
    fams = {}
    for fam, name, key, box, crop, draw in KIT:
        frame_im, _ = lifts[key]; Q = layer(); draw(Q); A_ = layer()  # the crop shows the capture's other new pieces too (never another variant of this one)
        for _, _, k2, b2, c2, d2 in KIT:
            if k2 == key and b2 and c2 != crop: d2(A_)
        A_.img.alpha_composite(Q.img); im = compose(frame_im, A_).convert('RGB'); before, _ = real(key)
        x0, y0, x1, y1 = crop; A = before.crop(crop).resize(((x1 - x0) * 2, (y1 - y0) * 2), Image.LANCZOS); B = im.crop((x0 * 2, y0 * 2, x1 * 2, y1 * 2))
        os.makedirs(SCR3 + 'pairs', exist_ok=True); n_ = len(rows); A.save(SCR3 + f'pairs/{n_:02d}-before.png'); B.save(SCR3 + f'pairs/{n_:02d}-after.png')
        fams.setdefault(fam, []).append((name, A, B)); rows.append(dict(n=len(rows), family=fam, piece=name, capture=key, build=SHOTS[key][1], old_px=old_px(key, box) if box else None, new_px=cover_px(Q)))
    os.makedirs(SCR3, exist_ok=True); json.dump(rows, open(OUT3 + 'make/coverage-kit.json', 'w'), indent=1)
    for fam, items in fams.items():
        Wd = 980; y = 0; tiles = []
        for name, A, B in items:
            s = min(1.0, (Wd - 30) / 2 / A.width); a2 = A.resize((round(A.width * s), round(A.height * s)), Image.LANCZOS); b2 = B.resize(a2.size, Image.LANCZOS)
            tiles.append((name, a2, b2)); y += 34 + a2.height + 18
        sh = Image.new('RGB', (Wd, y), (249, 247, 238)); d = ImageDraw.Draw(sh); y = 0
        for name, a2, b2 in tiles:
            d.text((10, y + 8), name, font=font('Jost-SemiBold', 19), fill=(23, 48, 47)); sh.paste(a2, (10, y + 34)); sh.paste(b2, (10 + a2.width + 10, y + 34)); y += 34 + a2.height + 18
        sh.save(SCR3 + 'kit-' + re.sub(r'\W+', '-', fam.split(':')[0].lower()).strip('-') + '.png')
    for r in rows: print(f"{r['piece'][:44]:44} {r['capture']:10} old {r['old_px']} new {r['new_px']}")


if __name__ == '__main__':
    main()
