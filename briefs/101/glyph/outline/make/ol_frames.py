"""Brief 101, hover outline (Glyph): every picture the board shows, drawn on real frames from the masks (ol_masks.py) with ol_draw.py.
Frames (copied first into out/scratch/glyph/outline/src/, since Latch's batch rewrites the proof folder):
  G2  r085-guard-enforcer: the Office's public door at play zoom 10, yaw 210, the Enforcer selected (brief 085's case: on the guard's body his
      ring, 40 px beside it the door's Enter). Same world pixels as r085-guard-hover (mean difference 1.5 grey levels), where the masks were cut.
  G   r085-guard-hover: the same moment with the Scout selected; used for the official car (it moved between the two captures).
  S   Sky's frame from play (2880 x 1864, HUD scale 2, play zoom about 9.3): the cell close together, the Fixer's feet behind the sphere drawn as
      the occlusion silhouette, the lamp mast's camera head with its "?".
Writes out/scratch/glyph/outline/frames/*.png and make/coverage.json. Glyph. Mock only."""
import os, sys, json
import numpy as np, cv2
from PIL import Image, ImageDraw
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import ol_draw as D

R = '/Users/sky/Projects/suborned/'; SCR = R + 'out/scratch/glyph/outline/'; SRC = SCR + 'src/'; MASKS = SCR + 'masks/'; OUTF = SCR + 'frames/'
MAKE = R + 'briefs/101/glyph/outline/make/'
KS = 1864 / 1080  # Sky's frame: screen px per 1080p px
PICK = 'B'


def mask(n): return np.array(Image.open(MASKS + n + '.png')).astype(np.float32) / 255


def rgb(n): return np.array(Image.open(SRC + n + '.png').convert('RGB'))


def hud(n): return np.array(Image.open(SRC + n + '.hud.png').convert('RGBA'))


def sky_hud(S):
    """Sky's frame has its HUD baked in; the chevron and the mast's '?' disc are kept on top by hand masks (the outline draws under the HUD)."""
    h = np.zeros(S.shape[:2] + (4,), np.uint8); im = Image.new('L', (S.shape[1], S.shape[0]), 0); d = ImageDraw.Draw(im)
    d.polygon([(446, 740), (500, 740), (473, 778)], fill=255); d.ellipse((183, 221, 231, 269), fill=255); h[:, :, 3] = np.array(im); return h


def draw(frame, h, full, style, k=1.0, front=None, cur=None, vis=None):
    """One thing hovered: its outline under the HUD, the pointer on top. Returns the frame and the outline layer."""
    st = D.STYLES[style] if isinstance(style, str) else style; L = D.layer(full, st, k, front)
    base = D.lift(frame, vis if vis is not None else full, st['lift']) if st.get('lift') else None
    out = D.compose(frame, [L], h, base)
    if cur: out = D.cursor(out, cur[0], cur[1], k)
    return out, L


def crop(img, box, z=1.0):
    x0, y0, x1, y1 = box; c = img[y0:y1, x0:x1]
    if z == 1: return c
    return cv2.resize(c, (round(c.shape[1] * z), round(c.shape[0] * z)), interpolation=cv2.INTER_AREA if z < 1 else cv2.INTER_LANCZOS4)


def save(name, img): os.makedirs(OUTF, exist_ok=True); Image.fromarray(img).save(OUTF + name + '.png')


def share(L, k=1.0):
    """The outline's share of a 1080p screen (alpha >= 24, as tools/hud-budget reads a layer), in %."""
    a = L[:, :, 3]
    if k != 1: a = cv2.resize(a, (round(a.shape[1] / k), round(a.shape[0] / k)), interpolation=cv2.INTER_AREA)
    return round(float((a >= 24 / 255).sum()) / (1920 * 1080) * 100, 3)


# ---------------------------------------------------------------- the things
def door_things():
    g, gv, d = mask('G-guard-full'), mask('G-guard-vis'), mask('G-door-full')
    front = np.maximum.reduce([mask('G-globe-vis'), mask('G-fin-full'), gv])  # the globe, the stone fin and the guard stand in front of the door
    return {'guard': (g, None, (980, 462), gv), 'door': (d, front, (1003, 470), mask('G-door-vis'))}


PLAY1 = (790, 96, 1240, 566)    # case 1 at play zoom, 1x
CLOSE1 = (918, 372, 1068, 547)  # case 1 close, 3x
LOOK = (922, 368, 1052, 547)    # the looks compared, 2x


def case1(cov):
    f, h = rgb('r085-guard-enforcer'), hud('r085-guard-enforcer'); things = door_things()
    for who, (full, front, cur, vis) in things.items():
        out, L = draw(f, h, full, PICK, 1.0, front, cur, vis); cov[f'door case, {who} hovered'] = share(L)
        save(f'c1-{who}-1x', crop(out, PLAY1)); save(f'c1-{who}-3x', crop(out, CLOSE1, 3))
        for st in 'ABC':
            o, _ = draw(f, h, full, st, 1.0, front, cur, vis); save(f'look-{st}-{who}', crop(o, LOOK, 2))
    save('c1-today-1x', crop(D.cursor(f.copy(), 980, 462), PLAY1))  # today: nothing on the thing (the pill, not in this capture, is the only cue)


def sky_things():
    fix_f, fix_lit = mask('S-fixer-full'), None
    return {'fixer': (fix_f, None, (697, 905)), 'tech': (mask('S-tech-vis'), mask('S-fixer-vis'), (716, 792)),  # the Fixer's head stands in front of the Tech's boots
            'mast': (mask('S-mast-vis'), None, (150, 322)), 'scout': (mask('S-scout-vis'), None, (476, 845)), 'enforcer': (mask('S-enforcer-vis'), None, (975, 890))}


CELL = (380, 690, 1060, 1076)   # Sky's frame: the cell (shown at 1080p scale)
CELLC = (610, 700, 790, 1012)   # close, native
HIDE = (624, 836, 774, 1030)    # the Fixer behind the sphere, native
MASTV = (0, 0, 640, 560)        # the mast's corner at 1080p scale
MASTC = (70, 190, 290, 400)     # the mast's head, native


def case2to4(cov):
    S = rgb('sky-frame'); h = sky_hud(S); T = sky_things()
    for who in ('fixer', 'tech', 'mast'):
        full, front, cur = T[who]; out, L = draw(S, h, full, PICK, KS, front, cur); cov[f"Sky's frame, {who} hovered"] = share(L, KS)
        if who != 'mast': save(f'c2-{who}-1080', crop(out, CELL, 1 / KS)); save(f'c2-{who}-close', crop(out, CELLC, 1.25))
        else: save('c4-mast-1080', crop(out, MASTV, 1 / KS)); save('c4-mast-close', crop(out, MASTC, 1.5))
        if who == 'fixer':
            save('c3-wrap', crop(out, HIDE, 2))
            for st in 'AC': o, _ = draw(S, h, full, st, KS, front, cur); save(f'trip-{st}-fixer', crop(o, HIDE, 2)); save(f'trip-{st}-cell', crop(o, CELL, 1 / KS))
    # the other rule for what covers her: her lit body only, the line hidden where the sphere stands in front (it stops at the cover)
    lit = mask('S-fixer-lit') if os.path.exists(MASKS + 'S-fixer-lit.png') else None
    if lit is not None:
        out, _ = draw(S, h, lit, PICK, KS, mask('S-sphere-vis'), T['fixer'][2]); save('c3-stop', crop(out, HIDE, 2))
    save('c2-today-1080', crop(D.cursor(S.copy(), 697, 905, KS), CELL, 1 / KS))


CAR1 = (1560, 610, 1920, 860)   # the official car at play zoom, 1x
CARC = (1676, 664, 1920, 852)   # close, 2x


def car(cov):
    f, h = rgb('r085-guard-hover'), hud('r085-guard-hover'); full = mask('G-car-vis')
    out, L = draw(f, h, full, PICK, 1.0, None, (1812, 768)); cov['the official car hovered'] = share(L)
    save('c4-car-1x', crop(out, CAR1)); save('c4-car-2x', crop(out, CARC, 2))


def widths():
    """The pick at 1.5 px (top) and at 2 px (below), true size: the guard, the door, the Fixer."""
    f, h = rgb('r085-guard-enforcer'), hud('r085-guard-enforcer'); T = door_things(); S = rgb('sky-frame'); hs = sky_hud(S); rows = []
    for w in (1.5, 2.0):
        st = dict(D.STYLES[PICK], w=w); t = []
        for who in ('guard', 'door'): full, front, cur, vis = T[who]; t.append(crop(draw(f, h, full, st, 1.0, front, cur, vis)[0], (850, 300, 1100, 560)))
        t.append(crop(draw(S, hs, mask('S-fixer-full'), st, KS, None, (697, 905))[0], CELL, 1 / KS)[:, 60:310])
        hh = max(x.shape[0] for x in t); sep = np.full((hh, 8, 3), 249, np.uint8)
        t = [np.concatenate([x, np.full((hh - x.shape[0], x.shape[1], 3), 249, np.uint8)], 0) for x in t]; rows.append(np.concatenate([t[0], sep, t[1], sep, t[2]], 1))
    save('w-compare', np.concatenate([rows[0], np.full((10, rows[0].shape[1], 3), 249, np.uint8), rows[1]], 0))


def main():
    cov = {}; case1(cov); case2to4(cov); car(cov); widths()
    doc = dict(method='the outline layer alone at 1080p, alpha >= 24 (tools/hud-budget\'s threshold), in % of the screen; look ' + PICK, share=cov)
    json.dump(doc, open(MAKE + 'coverage.json', 'w'), indent=1)
    for k, v in cov.items(): print(f'{k:34} {v:.3f} %')


if __name__ == '__main__':
    main()
