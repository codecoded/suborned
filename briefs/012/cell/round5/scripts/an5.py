"""Round 5 analysis, all measured on the painted lineups.
 1. dE2000 of what is actually painted against the signals (cones #e03a2f, guard #f0a020, rivals #8a4fd0): for every figure, non-skin pixels quantised to 4 bits per channel; nearest painted colour to each signal
    (colours holding >= 0.5% of the figure) and the share of figure pixels within dE 20 of each signal.
 2. Play-size strips: tallest figure 56 px after the 38 degree foreshortening, cut out on neutral ground, the pavement cream #f9efdc and the asphalt #a5a2a8; native size and 4x.
 3. Distinguishability at 50 px: black silhouettes (profile distance, IoU), colour separation between figures (dE of their dominant colour clusters, from the 50 px cut-outs), value (L) of upper and lower half."""
import json, cv2, numpy as np
from PIL import Image
from lib import *
from sil import *
from de import srgb2lab, de2000, hex2rgb, SIG

OUT = DEL / "strips"; OUT.mkdir(exist_ok=True); SIL = DEL / "silhouettes"; SIL.mkdir(exist_ok=True)
R4 = ROOT / "briefs/012/cell/round4/lineups"
ITEMS = [("a", DEL / "lineups/R5-lineup-a.png", "Round 5 a: Restrained"), ("b", DEL / "lineups/R5-lineup-b.png", "Round 5 b: Monochrome"), ("c", DEL / "lineups/R5-lineup-c.png", "Round 5 c: Flight deck"),
         ("R4-L1", R4 / "R4-lineup-L1.png", "Round 4 L1 (reference, rejected)"), ("R4-L4", R4 / "R4-lineup-L4.png", "Round 4 L4 (reference, rejected)")]
GROUNDS = {"neutral": (235, 235, 235), "cream pavement": (249, 239, 220), "lilac-grey asphalt": (165, 162, 168)}
SIGLAB = {k: srgb2lab(hex2rgb(v)) for k, v in SIG.items()}
ROLES = ("scout", "fixer", "tech", "enforcer")
hexof = lambda rgb: '#%02x%02x%02x' % tuple(int(round(v)) for v in rgb)

def skin_ref(im_bgr, fig):
    ys, xs = np.nonzero(fig); y0, y1 = ys.min(), ys.max(); h = y1 - y0
    rgb = im_bgr[..., ::-1].astype(np.float32); head = (fig > 0).copy(); head[:y0 + int(0.04 * h)] = 0; head[y0 + int(0.16 * h):] = 0
    hp = rgb[head > 0]; keep = srgb2lab(hp)[:, 0] > 35
    return srgb2lab(np.median(hp[keep], 0) if keep.sum() > 30 else np.array([180, 130, 100.0]))

def figure_pixels(im_bgr, fig):
    """non-skin, non-black-outline pixels of one figure, plus the pixel rows (for upper/lower halves)"""
    rgb = im_bgr[..., ::-1].astype(np.float32); fl = skin_ref(im_bgr, fig)
    ys, xs = np.nonzero(fig); px = rgb[ys, xs]; lab = srgb2lab(px)
    ok = (np.sqrt(((lab - fl) ** 2).sum(1)) > 18) & (lab[:, 0] > 8)
    return px[ok], lab[ok], ys[ok], ys.min(), ys.max()

_cache = {}
def de_hex(h, s):
    k = (h, s)
    if k not in _cache: _cache[k] = de2000(srgb2lab(hex2rgb(h)), SIGLAB[s])
    return _cache[k]

def painted_vs_signals(px):
    q = (px // 16).astype(np.int32); key = q[:, 0] * 256 + q[:, 1] * 16 + q[:, 2]
    uk, inv, cnt = np.unique(key, return_inverse=True, return_counts=True)
    cols = np.array([np.median(px[inv == i], 0) if cnt[i] > 1 else px[inv == i][0] for i in range(len(uk))])
    share = cnt / cnt.sum(); out = {}
    for s in SIG:
        d = np.array([de2000(srgb2lab(c), SIGLAB[s]) for c in cols])
        big = share >= 0.005
        i = np.argmin(np.where(big, d, 1e9))
        out[s] = dict(nearest_painted=hexof(cols[i]), dE=round(float(d[i]), 1), share_within_20=round(float(share[d < 20].sum()), 4), share_within_10=round(float(share[d < 10].sum()), 4))
    return out

def clusters(lab, px, k=6, seed=5):
    rng = np.random.default_rng(seed); sel = rng.choice(len(lab), min(len(lab), 20000), replace=False); X = lab[sel]
    cen = X[rng.choice(len(X), k, replace=False)]
    for _ in range(20):
        a = np.argmin(((X[:, None] - cen[None]) ** 2).sum(-1), 1); cen = np.array([X[a == t].mean(0) if (a == t).any() else cen[t] for t in range(k)])
    a = np.argmin(((lab[:, None, :] - cen[None]) ** 2).sum(-1), 1) if len(lab) < 6000 else np.concatenate([np.argmin(((lab[i:i + 6000, None, :] - cen[None]) ** 2).sum(-1), 1) for i in range(0, len(lab), 6000)])
    w = np.bincount(a, minlength=k) / len(a); out = []
    for t in np.argsort(-w)[:4]:
        if w[t] >= 0.04: out.append((hexof(np.median(px[a == t], 0)), float(w[t])))
    return out
def sd(A, B):
    ws = sum(w for _, w in A); return sum(w * min(de2000(srgb2lab(hex2rgb(h)), srgb2lab(hex2rgb(h2))) for h2, _ in B) for h, w in A) / ws

def cutout(im, mask, target=56):
    ys, xs = np.nonzero(mask); y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    sc = target / (PITCH * (y1 - y0)); crop = im[y0:y1, x0:x1]; cm = mask[y0:y1, x0:x1]
    w, h = max(1, int(round(crop.shape[1] * sc))), max(1, int(round(crop.shape[0] * sc * PITCH)))
    col = cv2.cvtColor(cv2.resize(crop, (w, h), interpolation=cv2.INTER_AREA), cv2.COLOR_BGR2RGB)
    m = cv2.resize(cm.astype(np.float32), (w, h), interpolation=cv2.INTER_AREA)
    return col.astype(np.float32), m

def on_ground(col, m, g, pad=6):
    h, w = m.shape; img = np.full((h + 2 * pad, w + 2 * pad, 3), g, np.float32)
    img[pad:pad + h, pad:pad + w] = col * m[..., None] + np.array(g, np.float32) * (1 - m[..., None]); return img.astype(np.uint8)

res = {}
for key, path, label in ITEMS:
    if not Path(path).exists(): print("missing", path); continue
    im, m = mask_of(path); figs = split4(m)
    # silhouettes
    iou = iou_pairs(figs); pdist = profile_dists(figs); save_black(m, SIL / f"{key}-black.png")
    col, mm = cutout(im, m)
    # strips: one band per ground, native size
    bands = [on_ground(col, mm, g) for g in GROUNDS.values()]
    blk = on_ground(np.full_like(col, 18), mm, GROUNDS["neutral"])
    strip = np.concatenate(bands + [blk], 0); Image.fromarray(strip).save(OUT / f"strip-{key}.png")
    Image.fromarray(strip).resize((strip.shape[1] * 4, strip.shape[0] * 4), Image.NEAREST).save(OUT / f"strip-{key}-x4.png")
    # per figure: painted colours vs signals, value of halves, 50 px clusters
    figs_out = {}; sig50 = []
    # 50 px figures (same mask split on the thumbnail's own columns)
    th_h, th_w = mm.shape; fig50 = []
    ys, xs = np.nonzero(m); x0 = xs.min(); sc = th_h / (ys.max() + 1 - ys.min())
    for f in figs:
        fy, fx = np.nonzero(f); a, b = int((fx.min() - x0) * (th_w / (xs.max() + 1 - x0))), int((fx.max() + 1 - x0) * (th_w / (xs.max() + 1 - x0)))
        mf = np.zeros_like(mm); mf[:, max(a, 0):min(b, th_w)] = mm[:, max(a, 0):min(b, th_w)]; fig50.append(mf)
    for role, f, f50 in zip(ROLES, figs, fig50):
        px, lab, yy, ymin, ymax = figure_pixels(im, f); mid = (ymin + ymax) / 2
        pv = painted_vs_signals(px)
        up, lo = lab[yy < mid][:, 0], lab[yy >= mid][:, 0]
        # 50 px clusters
        c50 = (col * (f50[..., None] > 0.9)).reshape(-1, 3); m50 = (f50.reshape(-1) > 0.9)
        p50 = col.reshape(-1, 3)[m50]; l50 = srgb2lab(p50); fl = skin_ref(im, f); keep = (np.sqrt(((l50 - fl) ** 2).sum(1)) > 18) & (l50[:, 0] > 8)
        cl = clusters(l50[keep], p50[keep]); sig50.append(cl)
        figs_out[role] = dict(signals=pv, L_upper=round(float(up.mean()), 1), L_lower=round(float(lo.mean()), 1), dominant=[(h, round(w, 3)) for h, w in clusters(lab, px)], dominant_50px=[(h, round(w, 3)) for h, w in cl])
    pair = {f"{ROLES[i]}-{ROLES[j]}": round(float(0.5 * (sd(sig50[i], sig50[j]) + sd(sig50[j], sig50[i]))), 1) for i in range(4) for j in range(i + 1, 4)}
    # figure vs ground contrast at 50 px: dominant colour of each figure to each ground
    gcon = {g: {role: round(float(min(de2000(srgb2lab(hex2rgb(h)), srgb2lab(list(gv))) for h, _ in sig50[i][:2])), 1) for i, role in enumerate(ROLES)} for g, gv in GROUNDS.items() if g != "neutral"}
    allpx = np.concatenate([figure_pixels(im, f)[0] for f in figs]); worst = {s: max(figs_out[r]["signals"][s]["share_within_20"] for r in ROLES) for s in SIG}
    mind = {s: min(figs_out[r]["signals"][s]["dE"] for r in ROLES) for s in SIG}
    res[key] = dict(label=label, figures=figs_out, colour_pair_dE_50px=pair, min_colour_pair=min(pair.values()), mean_colour_pair=round(float(np.mean(list(pair.values()))), 1),
                    min_dE_to_signal=mind, worst_share_within_20=worst, ground_contrast=gcon, mean_iou=round(float(np.mean(list(iou.values()))), 3),
                    mean_profile_dist=round(float(np.mean(list(pdist.values()))), 4), min_profile_dist=round(float(min(pdist.values())), 4), closest_pair=min(pdist, key=pdist.get),
                    strip=f"strips/strip-{key}.png", strip_size=list(strip.shape[:2]))
    print(f"{key:6} min dE to signals {mind} | share<20 worst {worst} | colour sep 50px min {res[key]['min_colour_pair']} mean {res[key]['mean_colour_pair']} | profile closest {res[key]['closest_pair']} {res[key]['min_profile_dist']}", flush=True)
(SCR / "analysis5.json").write_text(json.dumps(res, indent=1)); (DEL / "analysis5.json").write_text(json.dumps(res, indent=1))
