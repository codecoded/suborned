"""50 px play-size silhouette strips of the Tech's walk and run (and the team lineup at play scale). Usage: python3 -I tf_sil.py"""
import sys, json
sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/012/cell/round3/scripts")
import numpy as np, cv2
from PIL import Image, ImageDraw, ImageFont
import sil
D = "/Users/sky/Projects/suborned/briefs/012/cell/tech-final/"
PITCH = sil.PITCH
BG = 235


def figures(path):
    im0 = cv2.imread(path); H, W = im0.shape[:2]
    im, mask = sil.mask_of(path, size=(W, H))
    n, lab, st, _ = cv2.connectedComponentsWithStats(mask)
    comps = sorted([i for i in range(1, n) if st[i, cv2.CC_STAT_AREA] > 8000], key=lambda i: st[i, cv2.CC_STAT_LEFT])
    out = []
    for i in comps:
        x, y, w, h = st[i, 0], st[i, 1], st[i, 2], st[i, 3]
        out.append((im[y:y + h, x:x + w], (lab[y:y + h, x:x + w] == i).astype(np.uint8)))
    return out


def thumb(crop, m, sc):
    h0, w0 = m.shape
    w, h = max(1, round(w0 * sc)), max(1, round(h0 * sc * PITCH))
    col = cv2.resize(crop, (w, h), interpolation=cv2.INTER_AREA); mm = cv2.resize(m.astype(np.float32), (w, h), interpolation=cv2.INTER_AREA)
    return col, mm


def strip(path, target, tag):
    figs = figures(path); tallest = max(m.shape[0] for _, m in figs); sc = target / (PITCH * tallest)
    ths = [thumb(c, m, sc) for c, m in figs]
    Hh = max(t[1].shape[0] for t in ths); gap = 10; Wt = sum(t[1].shape[1] for t in ths) + gap * (len(ths) + 1)
    col = np.full((Hh + 12, Wt, 3), BG, np.uint8); blk = col.copy(); x = gap
    for c, m in ths:
        h, w = m.shape; y = Hh + 6 - h
        a = m[..., None]; col[y:y + h, x:x + w] = (cv2.cvtColor(c, cv2.COLOR_BGR2RGB) * a + col[y:y + h, x:x + w] * (1 - a)).astype(np.uint8)
        blk[y:y + h, x:x + w] = (18 * a + blk[y:y + h, x:x + w] * (1 - a)).astype(np.uint8); x += w + gap
    heights = [t[1].shape[0] for t in ths]
    return col, blk, heights


def save(arr, name, x5=True):
    im = Image.fromarray(arr); im.save(D + f"{name}.png")
    if x5: im.resize((im.width * 5, im.height * 5), Image.NEAREST).save(D + f"{name}-x5.png")


info = {}
for kind in ("walk", "run"):
    for view in ("side", "front"):
        col, blk, hs = strip(D + f"TF-strip-{kind}-{view}.jpg", 50, f"{kind}-{view}")
        save(col, f"sil50-{kind}-{view}-colour"); save(blk, f"sil50-{kind}-{view}-black")
        info[f"{kind}-{view}"] = dict(figure_heights_px=hs, size=list(col.shape[:2][::-1]))
        print(kind, view, hs, col.shape)
json.dump(info, open(D + "work/sil50.json", "w"), indent=1)
