"""Brief 101, hover outline (Glyph): each outlined thing's mask on its real frame, so the mock draws the line the way Unity's edge pass would:
from the thing's own pixels, never a hand-drawn ring. GrabCut on a 4x crop (hint polygons for what is surely it and surely not), the HUD's
own pixels left unknown, then back to 1x as a soft mask. Two masks per thing:
  vis  : the pixels where it is the visible surface;
  full : vis plus where it stands behind something (hand polygons: the door behind the guard and the globe, the Fixer's feet behind the sphere),
         as Unity's mask pass draws it with no depth test.
Writes out/scratch/glyph/outline/masks/<frame>-<thing>-{vis,full}.png and a check sheet (magenta edge on a 4x crop). Glyph. Mock only."""
import os, sys, json
import numpy as np, cv2
from PIL import Image, ImageDraw

R = '/Users/sky/Projects/suborned/'; SCR = R + 'out/scratch/glyph/outline/'; SRC = SCR + 'src/'; MASKS = SCR + 'masks/'
FRAMES = {'G': 'r085-guard-hover', 'S': 'sky-frame', 'M': 'r101-mast-start'}  # G 0.2.3xx play zoom 10 at the Office door; S Sky's frame (2880 x 1864 crop); M the opening view

# thing: frame, crop box (x0, y0, x1, y1), sure-it polygons, surely-not polygons, the polygon it must stay inside, the hidden part's polygons (full only)
THINGS = {
    # G: r085-guard-hover, the Office's public door at play zoom 10
    'guard': dict(f='G', box=(936, 414, 986, 516), inside=[(946, 422), (972, 420), (976, 444), (975, 470), (970, 490), (968, 514), (950, 514), (949, 470), (946, 446)],
                  fg=[[(956, 426), (965, 426), (965, 436), (957, 436)], [(955, 442), (968, 442), (967, 470), (956, 470)], [(957, 472), (965, 472), (964, 486), (958, 486)]],
                  hidden=[[(955, 486), (966, 486), (965, 506), (962, 509), (958, 509), (956, 506)]]),  # his feet under the door marker's disc
    'globe': dict(f='G', box=(880, 135, 1022, 336), inside=[(905, 160), (930, 145), (965, 143), (990, 150), (1008, 170), (1018, 200), (1018, 240), (1012, 265), (1000, 285),
                  (992, 295), (992, 318), (980, 330), (945, 331), (915, 326), (905, 312), (912, 295), (920, 285), (900, 268), (888, 240), (888, 200)],
                  fg=[[(915, 180), (990, 180), (995, 260), (920, 265)], [(925, 300), (980, 300), (975, 322), (930, 322)]]),
    'fin': dict(f='G', poly=[(966, 326), (1047, 326), (1047, 524), (1035, 522), (1029, 500), (1019, 470), (1009, 440), (994, 400), (980, 360), (970, 330)]),
    'door': dict(f='G', poly=[(941, 447), (941, 140), (944, 122), (952, 111), (960, 106), (968, 109), (982, 119), (992, 130), (1000, 140), (1006, 150), (1012, 160), (1020, 180),
                 (1026, 200), (1031, 225), (1035, 250), (1038, 300), (1039, 330), (1039, 535), (1036, 536)], under=('globe', 'fin', 'guard')),  # the doorway: the proxy Latch draws
    'car': dict(f='G', box=(1694, 676, 1920, 846), inside=[(1700, 785), (1712, 770), (1745, 752), (1785, 738), (1802, 705), (1830, 698), (1890, 690), (1920, 680), (1920, 842),
                (1712, 840), (1700, 815)], bg=[[(1816, 824), (1860, 818), (1920, 806), (1920, 846), (1816, 846)], [(1745, 840), (1790, 838), (1790, 846), (1745, 846)]], fg=[[(1720, 790), (1780, 775), (1790, 800), (1730, 815)], [(1820, 712), (1900, 700), (1905, 725), (1825, 735)], [(1800, 760), (1900, 750), (1900, 800), (1800, 810)]]),
    'driver': dict(f='G', box=(1640, 748, 1696, 838), inside=[(1665, 752), (1683, 752), (1688, 770), (1690, 800), (1686, 812), (1690, 832), (1648, 834), (1648, 800), (1653, 775), (1660, 765)],
                   fg=[[(1668, 758), (1679, 758), (1679, 766), (1668, 766)], [(1660, 772), (1684, 772), (1683, 798), (1661, 798)], [(1658, 804), (1675, 804), (1674, 826), (1659, 826)]]),
    # S: Sky's frame (a 2880 x 1864 capture, 1.726 px to the 1080p px): the cell, the sphere sculpture, the lamp mast's head
    'scout': dict(f='S', box=(446, 772, 506, 932), inside=[(462, 776), (486, 776), (492, 790), (494, 806), (499, 815), (499, 850), (494, 862), (491, 880), (486, 898), (492, 912),
                  (489, 927), (462, 927), (462, 905), (467, 882), (458, 866), (453, 840), (453, 812), (458, 800)],
                  fg=[[(466, 785), (482, 785), (482, 800), (466, 800)], [(462, 815), (490, 815), (490, 850), (462, 850)], [(470, 870), (482, 870), (481, 895), (471, 895)]]),
    'tech': dict(f='S', box=(690, 708, 744, 864), inside=[(700, 713), (724, 713), (728, 728), (736, 745), (738, 790), (733, 812), (726, 822), (724, 845), (722, 850), (719, 852),
                 (712, 852), (706, 855), (700, 858), (696, 840), (697, 800), (696, 760), (698, 740)],
                 fg=[[(705, 720), (720, 720), (720, 735), (705, 735)], [(702, 750), (728, 750), (728, 800), (702, 800)], [(703, 812), (718, 812), (718, 840), (703, 840)]]),
    'fixer': dict(f='S', box=(670, 846, 722, 980), inside=[(684, 852), (712, 852), (716, 866), (712, 885), (710, 905), (708, 935), (706, 955), (703, 975), (680, 975), (678, 950),
                  (676, 920), (677, 890), (680, 866)],
                  fg=[[(688, 862), (705, 862), (705, 880), (688, 880)], [(683, 895), (703, 895), (703, 935), (683, 935)], [(686, 945), (700, 945), (699, 968), (687, 968)]],
                  sil=dict(box=(672, 966, 726, 1012))),  # her feet behind the sphere, drawn as the occlusion silhouette (teal fill, cream edge)
    'enforcer': dict(f='S', box=(948, 814, 1000, 976), inside=[(962, 820), (978, 820), (984, 832), (988, 848), (994, 870), (996, 905), (993, 935), (990, 950), (992, 972),
                     (958, 972), (960, 945), (955, 925), (952, 890), (954, 850), (958, 835)],
                     fg=[[(964, 826), (978, 826), (978, 842), (964, 842)], [(960, 855), (988, 855), (988, 925), (960, 925)], [(966, 940), (984, 940), (983, 962), (967, 962)]]),
    'sphere': dict(f='S', box=(546, 962, 792, 1204), inside=[(560, 1020), (600, 1010), (640, 1012), (662, 990), (690, 976), (740, 975), (770, 1000), (778, 1040), (765, 1080),
                   (735, 1100), (728, 1140), (722, 1185), (680, 1198), (620, 1192), (608, 1150), (606, 1118), (572, 1100), (553, 1070)],
                   fg=[[(580, 1040), (650, 1030), (680, 1080), (600, 1095)], [(690, 1000), (750, 1000), (760, 1060), (700, 1070)], [(625, 1120), (710, 1120), (705, 1175), (630, 1170)]]),
    'mast': dict(f='S', box=(104, 268, 244, 372), inside=[(118, 282), (150, 278), (185, 290), (195, 282), (232, 284), (238, 300), (234, 322), (224, 330), (198, 331), (190, 345),
                 (180, 366), (155, 369), (120, 352), (110, 320)], bg=[[(197, 333), (224, 333), (224, 372), (197, 372)]],
                 fg=[[(130, 300), (170, 300), (175, 340), (140, 345)], [(205, 292), (225, 292), (225, 315), (205, 315)]]),
}


def fill_holes(m):
    """A mask's holes filled (a highlight GrabCut called background is still the thing)."""
    h, w = m.shape; f = m.copy(); ff = np.zeros((h + 2, w + 2), np.uint8); cv2.floodFill(f, ff, (0, 0), 255)
    return m | cv2.bitwise_not(f)


PROOF = R + 'out/build/play/proof/'; SKY = '/Users/sky/.t3/userdata/attachments/fe96bb36-dcb7-42e2-9225-e84ac1db8f4f-dbc78ed8-6d49-4557-bfef-651308b640c3.png'


def fetch():
    """The frames, copied once into scratch (Latch's batch rewrites the proof folder); never overwritten."""
    os.makedirs(SRC, exist_ok=True); import shutil
    for n in ('r085-guard-hover', 'r085-guard-enforcer'):
        for ext in ('.png', '.hud.png', '.manifest.json'):
            if not os.path.exists(SRC + n + ext) and os.path.exists(PROOF + n + ext): shutil.copy2(PROOF + n + ext, SRC + n + ext)
    if not os.path.exists(SRC + 'sky-frame.png') and os.path.exists(SKY): shutil.copy2(SKY, SRC + 'sky-frame.png')


def poly_mask(shape, polys, ox=0, oy=0, s=1):
    m = Image.new('L', (shape[1], shape[0]), 0); d = ImageDraw.Draw(m)
    for p in polys: d.polygon([((x - ox) * s, (y - oy) * s) for x, y in p], fill=255)
    return np.array(m) > 0


def frame(key): return np.array(Image.open(SRC + FRAMES[key] + '.png').convert('RGB'))


def hud_alpha(key):
    p = SRC + FRAMES[key] + '.hud.png'
    return np.array(Image.open(p).convert('RGBA'))[:, :, 3] if os.path.exists(p) else np.zeros(frame(key).shape[:2], np.uint8)


def cut(name, t, it=8, s=4):
    """GrabCut at s x on the box; returns the visible mask at 1x (float 0..1, full frame size)."""
    im = frame(t['f']); H, W = im.shape[:2]; x0, y0, x1, y1 = t['box']
    crop = cv2.resize(im[y0:y1, x0:x1][:, :, ::-1], ((x1 - x0) * s, (y1 - y0) * s), interpolation=cv2.INTER_CUBIC)
    lab = np.full(crop.shape[:2], cv2.GC_BGD, np.uint8)
    inside = poly_mask(crop.shape, [t['inside']], x0, y0, s); lab[inside] = cv2.GC_PR_FGD
    for p in t.get('bg', []): lab[poly_mask(crop.shape, [p], x0, y0, s)] = cv2.GC_BGD
    for p in t.get('fg', []): lab[poly_mask(crop.shape, [p], x0, y0, s) & inside] = cv2.GC_FGD
    hud = cv2.resize(hud_alpha(t['f'])[y0:y1, x0:x1], (crop.shape[1], crop.shape[0]), interpolation=cv2.INTER_NEAREST) > 40
    lab[hud & inside] = cv2.GC_PR_FGD; lab[hud & ~inside] = cv2.GC_BGD
    bgd, fgd = np.zeros((1, 65), np.float64), np.zeros((1, 65), np.float64)
    cv2.grabCut(crop, lab, None, bgd, fgd, it, cv2.GC_INIT_WITH_MASK)
    m = ((lab == cv2.GC_FGD) | (lab == cv2.GC_PR_FGD)).astype(np.uint8) * 255
    n, cc, st, _ = cv2.connectedComponentsWithStats(m)  # one piece (the biggest), holes filled
    if n > 2: keep = 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA])); m = ((cc == keep) * 255).astype(np.uint8)
    m = cv2.morphologyEx(m, cv2.MORPH_CLOSE, np.ones((5, 5), np.uint8)); m = fill_holes(m)
    small = cv2.resize(m.astype(np.float32) / 255, (x1 - x0, y1 - y0), interpolation=cv2.INTER_AREA)
    out = np.zeros((H, W), np.float32); out[y0:y1, x0:x1] = small; return out


def poly_soft(t, s=4):
    """A hand shape (the door's proxy, the stone fin) at 1x, anti-aliased."""
    im = frame(t['f']); H, W = im.shape[:2]; m = poly_mask((H * s, W * s), [t['poly']], 0, 0, s).astype(np.float32)
    return cv2.resize(m, (W, H), interpolation=cv2.INTER_AREA)


def silhouette(t):
    """The occlusion silhouette the game already draws over what covers her (the deep-teal fill #12544c and its cream rim): those pixels."""
    im = frame(t['f']).astype(int); x0, y0, x1, y1 = t['sil']['box']; c = im[y0:y1, x0:x1]; r, g, b = c[:, :, 0], c[:, :, 1], c[:, :, 2]
    teal = (abs(r - 30) < 14) & (abs(g - 58) < 14) & (abs(b - 55) < 14); cream = (r > 190) & (g > 185) & (b > 190) & (b >= g - 4)  # as drawn here: #1e3a37, rim #d8d0d7
    m = (teal | cream).astype(np.uint8) * 255; n, cc, st, _ = cv2.connectedComponentsWithStats(m)
    if n > 2: keep = 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA])); m = ((cc == keep) * 255).astype(np.uint8)
    m = cv2.morphologyEx(m, cv2.MORPH_CLOSE, np.ones((3, 3), np.uint8)); out = np.zeros(im.shape[:2], np.float32); out[y0:y1, x0:x1] = m / 255; return out


def build(names=None):
    os.makedirs(MASKS, exist_ok=True); done = {}
    for name, t in THINGS.items():
        if names and name not in names: continue
        vis = poly_soft(t) if 'poly' in t else cut(name, t)
        if t.get('sil'):  # her lit body alone is kept too (the other rule: the line stops at the cover)
            Image.fromarray((vis * 255).astype(np.uint8)).save(MASKS + f"{t['f']}-{name}-lit.png"); vis = np.maximum(vis, cv2.GaussianBlur(silhouette(t), (0, 0), 0.5))
        full = vis.copy()
        for o in t.get('under', ()): vis = vis * (1 - done[o][1]) if o in done else vis  # what stands in front of it
        if t.get('hidden'):
            hm = poly_mask(vis.shape, t['hidden']).astype(np.float32); hm = cv2.GaussianBlur(hm, (0, 0), 0.6); full = np.maximum(full, hm)
        key = f"{t['f']}-{name}"
        Image.fromarray((vis * 255).astype(np.uint8)).save(MASKS + key + '-vis.png'); Image.fromarray((full * 255).astype(np.uint8)).save(MASKS + key + '-full.png')
        done[name] = (t, vis, full)
    return done


def bbox(p): xs, ys = zip(*p); return (int(min(xs)), int(min(ys)), int(max(xs)) + 1, int(max(ys)) + 1)


def check(done, out=SCR + 'masks/check.png', s=4):
    tiles = []
    for name, (t, vis, full) in done.items():
        im = frame(t['f']); x0, y0, x1, y1 = t['box'] if 'box' in t else bbox(t['poly']); pad = 6; x0, y0, x1, y1 = max(0, x0 - pad), max(0, y0 - pad), x1 + pad, y1 + pad
        c = cv2.resize(im[y0:y1, x0:x1], ((x1 - x0) * s, (y1 - y0) * s), interpolation=cv2.INTER_NEAREST)
        for m, col in ((full, (0, 200, 255)), (vis, (255, 0, 255))):
            mm = cv2.resize((m[y0:y1, x0:x1] > .5).astype(np.uint8), (c.shape[1], c.shape[0]), interpolation=cv2.INTER_NEAREST)
            e = cv2.morphologyEx(mm, cv2.MORPH_GRADIENT, np.ones((3, 3), np.uint8)) > 0; c[e] = col
        tiles.append(Image.fromarray(c))
    W = sum(t.width for t in tiles) + 10 * len(tiles); H = max(t.height for t in tiles); sheet = Image.new('RGB', (W, H), (255, 255, 255)); x = 0
    for t in tiles: sheet.paste(t, (x, 0)); x += t.width + 10
    sheet.save(out); print('check', sheet.size)


if __name__ == '__main__':
    fetch(); d = build(sys.argv[1:] or None); check(d)
