"""Silhouette tools: figure mask by grabCut seeded from a background-plane difference, black fills, per-figure split, pairwise IoU, 50 px thumbnails."""
import cv2, numpy as np
from PIL import Image

def mask_of(path, size=(1536, 1024)):
    im = cv2.imread(str(path)); im = cv2.resize(im, size, interpolation=cv2.INTER_AREA)
    H, W = im.shape[:2]
    # background plane: fit a quadratic to pixels along the border bands, per channel
    yy, xx = np.mgrid[0:H, 0:W]; xn, yn = xx / W, yy / H
    band = np.zeros((H, W), bool); band[:40] = band[-12:] = True; band[:, :30] = band[:, -30:] = True
    A = np.stack([np.ones_like(xn), xn, yn, xn * yn, xn ** 2, yn ** 2], -1)
    bg = np.zeros((H, W, 3), np.float32)
    for c in range(3):
        coef, *_ = np.linalg.lstsq(A[band], im[..., c][band].astype(float), rcond=None)
        bg[..., c] = A @ coef
    diff = np.linalg.norm(im.astype(np.float32) - bg, axis=2)
    gm = np.zeros((H, W), np.uint8) + cv2.GC_PR_BGD
    gm[diff > 28] = cv2.GC_PR_FGD
    gm[diff > 70] = cv2.GC_FGD
    gm[band & (diff < 18)] = cv2.GC_BGD
    ratio = im.astype(np.float32) / np.maximum(bg, 1)
    spread = ratio.max(2) - ratio.min(2); mr = ratio.mean(2)
    shadow = (yy > 0.78 * H) & (spread < 0.06) & (mr > 0.45) & (mr < 0.96) & (diff < 95)
    gm[shadow] = cv2.GC_BGD
    # ground shadow: darker than plane but lightly saturated and soft: mark probable bg low down
    bgd = np.zeros((1, 65), np.float64); fgd = np.zeros((1, 65), np.float64)
    cv2.grabCut(im, gm, None, bgd, fgd, 4, cv2.GC_INIT_WITH_MASK)
    m = ((gm == cv2.GC_FGD) | (gm == cv2.GC_PR_FGD)).astype(np.uint8)
    m = cv2.morphologyEx(m, cv2.MORPH_OPEN, np.ones((5, 5), np.uint8))
    m = cv2.morphologyEx(m, cv2.MORPH_CLOSE, np.ones((5, 5), np.uint8))
    n, lab, st, _ = cv2.connectedComponentsWithStats(m)
    keep = np.zeros_like(m)
    for i in range(1, n):
        if st[i, cv2.CC_STAT_AREA] > 4000: keep[lab == i] = 1
    return im, keep

def save_black(mask, out, bgv=235):
    img = np.full(mask.shape + (3,), bgv, np.uint8); img[mask > 0] = 18
    Image.fromarray(img).save(out)


PITCH = 0.788  # cos 38 deg: vertical foreshortening at the play camera (pitch 38)

def split4(mask):
    H, W = mask.shape
    col = cv2.GaussianBlur(mask.sum(0).astype(np.float32).reshape(1, -1), (0, 0), 6).ravel()
    cuts = [0]
    for f in (0.25, 0.5, 0.75):
        lo, hi = int(W * (f - 0.09)), int(W * (f + 0.09)); cuts.append(lo + int(np.argmin(col[lo:hi])))
    cuts.append(W)
    figs = []
    for a, b in zip(cuts[:-1], cuts[1:]):
        m = np.zeros_like(mask); m[:, a:b] = mask[:, a:b]
        ys, xs = np.nonzero(m)
        figs.append(m if len(ys) else m)
    return figs

def iou_pairs(figs):
    """true-scale overlap after aligning each figure's feet (lowest row) and horizontal centroid; lower = more distinct"""
    H, W = figs[0].shape
    al = []
    for m in figs:
        ys, xs = np.nonzero(m); dx = W // 2 - int(xs.mean()); dy = (H - 8) - ys.max()
        t = np.zeros_like(m); 
        M = np.float32([[1, 0, dx], [0, 1, dy]]); t = cv2.warpAffine(m, M, (W, H), flags=cv2.INTER_NEAREST)
        al.append(t > 0)
    out = {}
    for i in range(4):
        for j in range(i + 1, 4):
            out[f"{i}{j}"] = float((al[i] & al[j]).sum() / max(1, (al[i] | al[j]).sum()))
    return out

def thumbs(im, mask, figs, target=56):
    ys, xs = np.nonzero(mask); y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    sc = target / (PITCH * (y1 - y0))
    crop = im[y0:y1, x0:x1]; cm = mask[y0:y1, x0:x1]
    w, h = max(1, int(round(crop.shape[1] * sc))), max(1, int(round(crop.shape[0] * sc * PITCH)))
    col = cv2.resize(crop, (w, h), interpolation=cv2.INTER_AREA)
    m = cv2.resize(cm.astype(np.float32), (w, h), interpolation=cv2.INTER_AREA)
    blk = np.full((h, w, 3), 235, np.uint8); blk = (blk * (1 - m[..., None]) + 18 * m[..., None]).astype(np.uint8)
    colm = (col * m[..., None] + 235 * (1 - m[..., None])).astype(np.uint8)  # figure cut out on pale ground
    return cv2.cvtColor(colm, cv2.COLOR_BGR2RGB), cv2.cvtColor(blk, cv2.COLOR_BGR2RGB), (h, w)


def width_profile(m, n=80):
    ys, xs = np.nonzero(m); y0, y1 = ys.min(), ys.max() + 1; h = y1 - y0
    prof = []
    for i in range(n):
        a = y0 + int(i * h / n); b = y0 + int((i + 1) * h / n); rows = m[a:max(b, a + 1)]
        w = [np.ptp(np.nonzero(r)[0]) + 1 if r.any() else 0 for r in rows]
        prof.append(np.mean(w) / h)  # width relative to the figure's own height
    return np.array(prof)

def profile_dists(figs):
    ps = [width_profile(f) for f in figs]; out = {}
    for i in range(4):
        for j in range(i + 1, 4): out[f"{i}{j}"] = float(np.abs(ps[i] - ps[j]).mean())
    return out
