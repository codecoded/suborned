"""Plan sections of the proxy trousers: a horizontal slice through both legs, seen from above, per pose and variant. Overlap of the two sections = magenta.
Usage: python3 -I tf_plan.py <verts.npz> <outdir>"""
import sys, json
import numpy as np
from PIL import Image, ImageDraw, ImageFont

npz = np.load(sys.argv[1]); out = sys.argv[2]
faces = {s: npz[f"faces|{s}"] for s in "RL"}
PPM = 420  # px per metre
X0, X1, Y0, Y1 = -0.62, 0.62, -0.62, 0.62
W, H = int((X1 - X0) * PPM), int((Y1 - Y0) * PPM)
CACHE = {}


def tris(F):
    return np.concatenate([F[:, [0, 1, 2]], F[:, [0, 2, 3]]])


def segments(V, F, z):
    T = tris(F); P = V[T]; zz = P[:, :, 2] - z
    segs = []
    for tri, d in zip(P, zz):
        pts = []
        for a, b in ((0, 1), (1, 2), (2, 0)):
            if (d[a] > 0) != (d[b] > 0):
                t = d[a] / (d[a] - d[b]); q = tri[a] + t * (tri[b] - tri[a]); pts.append(q[:2])
        if len(pts) == 2: segs.append((pts[0], pts[1]))
    return segs


XS = X0 + (np.arange(W) + 0.5) / PPM
YS = Y1 - (np.arange(H) + 0.5) / PPM
GX, GY = np.meshgrid(XS, YS)


def fill(segs):
    cnt = np.zeros((H, W), np.int32)
    for p, q in segs:
        (x1, y1), (x2, y2) = p, q
        if y1 == y2: continue
        cond = ((y1 <= GY) != (y2 <= GY))
        xi = x1 + (GY - y1) * (x2 - x1) / (y2 - y1)
        cnt += (cond & (GX < xi)).astype(np.int32)
    return (cnt % 2) == 1


def px(p): return ((p[0] - X0) * PPM, (Y1 - p[1]) * PPM)


def section(vn, pn, z):
    key = (vn, pn, round(z, 3))
    if key in CACHE: return CACHE[key]
    masks, segl = {}, {}
    for s in "RL":
        V = npz[f"{vn}|{pn}|{s}"]; sg = segments(V, faces[s], z); segl[s] = sg; masks[s] = fill(sg) if sg else np.zeros((H, W), bool)
    CACHE[key] = (masks, segl); return CACHE[key]


def best_z(pn, vn="AD", lo=0.36, hi=0.76):
    best = (0, 0.45)
    for z in np.arange(lo, hi, 0.04):
        m, _ = section(vn, pn, z); a = (m["R"] & m["L"]).sum() / PPM ** 2
        if a > best[0]: best = (a, z)
    return best[1]


def draw(vn, pn, z, label):
    m, segl = section(vn, pn, z)
    img = np.full((H, W, 3), 246, np.uint8)
    img[m["R"]] = (176, 178, 184); img[m["L"]] = (176, 178, 184)
    ov = m["R"] & m["L"]; img[ov] = (255, 43, 208)
    im = Image.fromarray(img); d = ImageDraw.Draw(im)
    for s in "RL":
        for p, q in segl[s]: d.line([px(p), px(q)], fill=(40, 42, 46), width=3)
    d.line([px((0, -0.6)), px((0, 0.6))], fill=(200, 200, 205), width=1)
    d.polygon([px((0, 0.58)), px((-0.03, 0.52)), px((0.03, 0.52))], fill=(120, 120, 128))
    area = ov.sum() / PPM ** 2 * 1e4
    f = ImageFont.truetype("/System/Library/Fonts/Helvetica.ttc", 19)
    d.text((10, 8), label[0], fill=(20, 20, 20), font=f); d.text((10, 32), label[1], fill=(90, 90, 90), font=f)
    d.text((10, H - 30), f"overlap {area:.0f} cm2" if area > 0.5 else "no overlap", fill=(190, 0, 150) if area > 0.5 else (20, 110, 60), font=f)
    return im, area


rows = [("stand", "standing, feet 0.25 m apart"), ("walk_pass", "walking, legs passing"), ("run_pass", "running, legs passing")]
res, sheets = {}, []
for pn, ptxt in rows:
    z = float(best_z(pn)); res[pn] = {"z": round(z, 2)}
    ims = []
    for vn, vl in (("AD", "as drawn"), ("FX10", "inner side narrowed"), ("BELL", "round 4 bell shorts")):
        zz = z if vn != "BELL" else float(best_z(pn, "BELL", 0.60, 1.0))      # the bell's hem is on the thigh: slice it where it is
        im, a = draw(vn, pn, zz, (vl, f"{ptxt}, slice at z = {zz:.2f} m")); ims.append(im); res[pn][vn] = round(float(a), 1); res[pn][vn + "_z"] = round(zz, 2)
    sheets.append(ims)
sw, sh = sheets[0][0].size
sheet = Image.new("RGB", (sw * 3 + 8, sh * len(sheets) + 4 * (len(sheets) - 1)), "white")
for r, ims in enumerate(sheets):
    for c, im in enumerate(ims): sheet.paste(im, (c * (sw + 4), r * (sh + 4)))
sheet.save(f"{out}/plan-sections.png"); json.dump(res, open(f"{out}/plan-sections.json", "w"), indent=1); print(res, sheet.size)
