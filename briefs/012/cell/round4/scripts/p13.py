"""Round 3b analysis: silhouettes (black fills, 50 px thumbs, profile distances) and garment colours per figure (as painted, at 50 px) with dE2000 to the signals, the city and each other."""
import json, cv2, numpy as np
from PIL import Image
from lib import *
from sil import *
from de import srgb2lab, de2000, row, hex2rgb

OUT = DEL / "silhouettes"; OUT.mkdir(exist_ok=True)
L1 = ROOT / "briefs/012/cell/round3/2_icons/lineups/R3-2-lineup-L1.png"
R3B = ROOT / "briefs/012/cell/round3b"
ITEMS = [("L1-round3-uniform", L1, "Round 3 L1 (white on charcoal, long coats)"), ("WB1-round3b-wardrobes", R3B / "lineups/R3b-lineup-WB1.png", "Round 3b WB1 (khaki, navy, moss, chestnut)"),
         ("R4-L1", DEL / "lineups/R4-lineup-L1.png", "Round 4 L1"), ("R4-L2", DEL / "lineups/R4-lineup-L2.png", "Round 4 L2"), ("R4-L3", DEL / "lineups/R4-lineup-L3.png", "Round 4 L3"), ("R4-L4-hero-block", DEL / "lineups/R4-lineup-L4.png", "Round 4 L4, hero colour as the main garment")]
def hexof(rgb): return '#%02x%02x%02x' % tuple(int(round(v)) for v in rgb)
def garment_colour(im_bgr, fig, k=7):
    """largest colour cluster of the figure after removing skin (colours near the face) and near-black shadow/outline pixels"""
    ys, xs = np.nonzero(fig); y0, y1 = ys.min(), ys.max(); h = y1 - y0
    rgb = im_bgr[..., ::-1].astype(np.float32)
    head = (fig > 0).copy(); head[:y0 + int(0.04 * h)] = 0; head[y0 + int(0.16 * h):] = 0
    hp = rgb[head > 0]; keep = srgb2lab(hp)[:, 0] > 35
    face = np.median(hp[keep], 0) if keep.sum() > 30 else np.array([180, 130, 100.0]); fl = srgb2lab(face)
    px = rgb[(fig > 0)]; lab = srgb2lab(px)
    ok = (np.sqrt(((lab - fl) ** 2).sum(1)) > 18) & (lab[:, 0] > 20)
    px, lab = px[ok], lab[ok]
    sel = np.random.default_rng(5).choice(len(lab), min(len(lab), 40000), replace=False); X = lab[sel]
    cen = X[np.random.default_rng(6).choice(len(X), k, replace=False)]
    for _ in range(20):
        a = np.argmin(((X[:, None] - cen[None]) ** 2).sum(-1), 1); cen = np.array([X[a == t].mean(0) if (a == t).any() else cen[t] for t in range(k)])
    a = np.argmin(((lab[:, None] - cen[None]) ** 2).sum(-1), 1) if len(lab) < 5000 else np.argmin(((lab[:, None, :] - cen[None]) ** 2).sum(-1), 1)
    w = np.bincount(a, minlength=k) / len(a); out = []
    for t in np.argsort(-w)[:4]:
        if w[t] < 0.04: continue
        out.append((hexof(np.median(px[a == t], 0)), float(w[t])))
    return out
res = {}; thumbs_rows = []
for key, path, label in ITEMS:
    im, m = mask_of(path); figs = split4(m)
    iou = iou_pairs(figs); pdist = profile_dists(figs)
    save_black(m, OUT / f"{key}-black.png")
    col, blk, hw = thumbs(im, m, figs)
    for nm, a in (("colour", col), ("black", blk)):
        Image.fromarray(a).save(OUT / f"{key}-thumb50-{nm}.png"); Image.fromarray(a).resize((a.shape[1] * 5, a.shape[0] * 5), Image.NEAREST).save(OUT / f"{key}-thumb50-{nm}-x5.png")
    sig = [garment_colour(im, f) for f in figs]; gc = [x[0][0] for x in sig]
    # colour separation at 50 px: downscaled figure, same method
    def sd(A, B):
        tot = 0.0; ws = sum(w for _, w in A)
        for h, w in A: tot += w * min(de2000(srgb2lab(hex2rgb(h)), srgb2lab(hex2rgb(h2))) for h2, _ in B)
        return tot / ws
    pair = {f"{i}{j}": round(float(0.5 * (sd(sig[i], sig[j]) + sd(sig[j], sig[i]))), 1) for i in range(4) for j in range(i + 1, 4)}
    res[key] = dict(label=label, garment=gc, signature=[[ (h, round(w, 3)) for h, w in x] for x in sig], colour_pair_dE=pair, min_colour_pair=min(pair.values()), mean_colour_pair=round(float(np.mean(list(pair.values()))), 1),
                    mean_iou=round(float(np.mean(list(iou.values()))), 3), max_iou=round(float(max(iou.values())), 3), mean_profile_dist=round(float(np.mean(list(pdist.values()))), 4), min_profile_dist=round(float(min(pdist.values())), 4), closest_pair=min(pdist, key=pdist.get), profile_dist={k: round(v, 4) for k, v in pdist.items()})
    thumbs_rows.append((col, blk))
rows = []
for col, blk in thumbs_rows:
    big = lambda a: np.array(Image.fromarray(a).resize((a.shape[1] * 3, a.shape[0] * 3), Image.NEAREST))
    rows.append(np.concatenate([big(col), np.full((col.shape[0] * 3, 24, 3), 235, np.uint8), big(blk)], 1))
Wd = max(r.shape[1] for r in rows); sheet = np.full((sum(r.shape[0] + 14 for r in rows) + 14, Wd + 20, 3), 235, np.uint8); y = 10
for r in rows: sheet[y:y + r.shape[0], 10:10 + r.shape[1]] = r; y += r.shape[0] + 14
Image.fromarray(sheet).save(OUT / "overview-50px-x3.png")
tk = {}
for role, pat in (("scout", "takes/R4-scout-*.png"), ("fixer", "takes/R4-fixer-*.png"), ("tech", "takes/R4-tech-*.png"), ("enforcer", "takes/R4-enforcer-*.png")):
    fl = sorted(DEL.glob(pat)); rowi = []
    for p in fl:
        im, m = mask_of(p, (1024, 1536)); b = np.full(m.shape + (3,), 235, np.uint8); b[m > 0] = 18; ys, xs = np.nonzero(m); b = b[max(0, ys.min() - 10):ys.max() + 10]
        rowi.append(cv2.resize(b, (int(b.shape[1] * 480 / b.shape[0]), 480), interpolation=cv2.INTER_AREA))
    Wt = sum(r.shape[1] for r in rowi); s = np.full((480, Wt, 3), 235, np.uint8); x = 0
    for r in rowi: s[:, x:x + r.shape[1]] = r; x += r.shape[1]
    Image.fromarray(s).save(OUT / f"takes-{role}-black.png"); tk[role] = [p.stem for p in fl]
(SCR / "analysis.json").write_text(json.dumps(dict(lineups=res, takes=tk), indent=1))
for k, v in res.items(): print(f"{k:24} garment {v['garment']} min colour pair {v['min_colour_pair']} mean {v['mean_colour_pair']} | profile mean {v['mean_profile_dist']} min {v['min_profile_dist']} ({v['closest_pair']}) | IoU {v['mean_iou']}")
