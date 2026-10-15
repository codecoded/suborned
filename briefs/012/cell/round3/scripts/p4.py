"""Round 3 part 4: black-fill silhouettes of every lineup, pairwise overlap, 50 px thumbnails (pitch-foreshortened like the 38 degree play camera)."""
import json, cv2, numpy as np
from pathlib import Path
from PIL import Image
from lib import *
from sil import *

OUT = DEL / "4_silhouettes"; OUT.mkdir(exist_ok=True)
items = [("A-white-baseline", LINEUP, "A set, white-on-charcoal (unchanged baseline)")]
for p in sorted((DEL / "1_tech_in_line").glob("R3-1-tech-*.png")): items.append((p.stem, p, "Option 1: " + p.stem.split("-", 4)[-1]))
for p in sorted((DEL / "2_icons/lineups").glob("R3-2-lineup-*.png")): items.append((p.stem, p, "Option 2: " + p.stem.split("-")[-1]))
NAMES = ["Scout", "Fixer", "Tech", "Enforcer"]
res = {}
strip = []
for key, path, label in items:
    im, m = mask_of(path)
    figs = split4(m)
    iou = iou_pairs(figs); pd_ = profile_dists(figs)
    save_black(m, OUT / f"{key}-black.png")
    col, blk, hw = thumbs(im, m, figs)
    Image.fromarray(col).save(OUT / f"{key}-thumb50-colour.png"); Image.fromarray(blk).save(OUT / f"{key}-thumb50-black.png")
    Image.fromarray(col).resize((col.shape[1] * 5, col.shape[0] * 5), Image.NEAREST).save(OUT / f"{key}-thumb50-colour-x5.png")
    Image.fromarray(blk).resize((blk.shape[1] * 5, blk.shape[0] * 5), Image.NEAREST).save(OUT / f"{key}-thumb50-black-x5.png")
    fh = [int(np.ptp(np.nonzero(f)[0])) + 1 for f in figs]; fw = [int(np.ptp(np.nonzero(f)[1])) + 1 for f in figs]
    res[key] = dict(label=label, file=str(path.relative_to(DEL)) if str(path).startswith(str(DEL)) else str(path), thumb_hw=list(hw), mean_iou=round(float(np.mean(list(iou.values()))), 3),
                    max_iou=round(float(max(iou.values())), 3), mean_profile_dist=round(float(np.mean(list(pd_.values()))), 4), min_profile_dist=round(float(min(pd_.values())), 4), closest_pair=min(pd_, key=pd_.get), profile_dist={k: round(v, 4) for k, v in pd_.items()}, worst_pair=max(iou, key=iou.get), iou={k: round(v, 3) for k, v in iou.items()},
                    fig_h_px=fh, fig_w_px=fw)
    strip.append((key, col, blk))
# overview: every lineup at 1x and 3x on a pale sheet
rows = []
for key, col, blk in strip:
    big = lambda a: np.array(Image.fromarray(a).resize((a.shape[1] * 3, a.shape[0] * 3), Image.NEAREST))
    rows.append(np.concatenate([big(col), np.full((col.shape[0] * 3, 24, 3), 235, np.uint8), big(blk)], 1))
Wd = max(r.shape[1] for r in rows)
sheet = np.full((sum(r.shape[0] + 14 for r in rows) + 14, Wd + 20, 3), 235, np.uint8); y = 10
for r in rows: sheet[y:y + r.shape[0], 10:10 + r.shape[1]] = r; y += r.shape[0] + 14
Image.fromarray(sheet).save(OUT / "overview-50px-x3.png")
# per-person: black fills of the icon takes (nameable from a shadow?)
tk = {}
for role in ("scout", "fixer", "tech", "enforcer"):
    fl = sorted((DEL / "2_icons").glob(f"R3-2-{role}-[1-5]-*.png"))
    row = []
    for p in fl:
        im, m = mask_of(p, (1024, 1536)); b = np.full(m.shape + (3,), 235, np.uint8); b[m > 0] = 18
        ys, xs = np.nonzero(m); b = b[max(0, ys.min() - 10):ys.max() + 10]; b = cv2.resize(b, (int(b.shape[1] * 520 / b.shape[0]), 520), interpolation=cv2.INTER_AREA); row.append(b)
    Wt = sum(r.shape[1] for r in row); s = np.full((520, Wt, 3), 235, np.uint8); x = 0
    for r in row: s[:, x:x + r.shape[1]] = r; x += r.shape[1]
    Image.fromarray(s).save(OUT / f"takes-{role}-black.png"); tk[role] = [p.stem for p in fl]
(SCR / "silhouettes.json").write_text(json.dumps(dict(lineups=res, takes=tk), indent=1))
for k, v in res.items(): print(f"{k:42} mean IoU {v['mean_iou']:.3f}  max {v['max_iou']:.3f} pair {v['worst_pair']}  profile dist mean {v['mean_profile_dist']:.4f} min {v['min_profile_dist']:.4f} pair {v['closest_pair']}")
