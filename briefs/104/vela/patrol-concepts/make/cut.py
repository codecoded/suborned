"""Brief 104 · patrol robot concepts (Vela): cut every figure out of a lineup sheet.

    python3 -I cut.py <sheet.png> <out_dir> <prefix> [--scale-m 1.75]

The sheets are drawn on plain warm paper with ink outlines, a grey human silhouette (1.75 m) at the far left and six robots in a row.
Paper is found by a flood fill from the border through paper-like and soft-shadow pixels; ink and colour stop it. What the fill can't
reach is figure. Soft contact shadows and a lit signal's halo become separate layers with partial alpha, so a cut-out sits on any
ground. Figures are split in 2D (parts can overlap in columns, see bodies()) and numbered left to right after the silhouette: <prefix>1..6. Writes <prefix><n>.png (RGBA,
the figure and its shadow) and cuts.json (each figure's box, feet row, height in px and in metres against the silhouette).
"""
import json, sys
from pathlib import Path

import cv2
import numpy as np


def arg(a, k, d=None):
    return a[a.index(k) + 1] if k in a else d


def masks(rgb):
    lab = cv2.cvtColor(rgb, cv2.COLOR_RGB2LAB).astype(np.float32)
    border = np.concatenate([lab[:10].reshape(-1, 3), lab[-10:].reshape(-1, 3), lab[:, :10].reshape(-1, 3), lab[:, -10:].reshape(-1, 3)])
    bg = np.median(border, 0)
    dL = lab[..., 0] - bg[0]
    dab = np.hypot(lab[..., 1] - bg[1], lab[..., 2] - bg[2])
    L, da = lab[..., 0], lab[..., 1] - bg[1]
    paper = (np.abs(dL) < 12) & (dab < 8)
    shadow = (dL <= -4) & (L > 172) & (dab < 13)                     # soft contact shadow: darker paper of the same hue (its dark core stays with the figure)
    glow = (L > 188) & (da >= 3) & (dab < 22)                        # a lit signal's red halo on the paper
    passable = paper | shadow | glow
    barrier = cv2.dilate((~passable).astype(np.uint8), np.ones((3, 3), np.uint8), iterations=2)
    n, lbl = cv2.connectedComponents((1 - barrier).astype(np.uint8), connectivity=4)
    edge = set(np.unique(np.concatenate([lbl[0], lbl[-1], lbl[:, 0], lbl[:, -1]]))) - {0}
    reach = np.isin(lbl, list(edge))
    # the barrier's own 2 px band round the figure: grow the reached paper back into passable pixels that touch it
    for _ in range(3):
        reach = reach | (cv2.dilate(reach.astype(np.uint8), np.ones((3, 3), np.uint8)).astype(bool) & passable)
    fig = ~reach
    # the scale figure is flat warm grey with no ink, so the shadow rule takes it: find it on its own in the sheet's left eighth
    h, w = L.shape
    sil = (np.abs(L - 150) < 22) & (dab > 5) & (dab < 15) & (lab[..., 2] < bg[2] - 5)
    sil[:, int(w * 0.125):] = False
    sil = cv2.morphologyEx(sil.astype(np.uint8), cv2.MORPH_CLOSE, np.ones((5, 5), np.uint8)).astype(bool)
    n, lbl, st, _ = cv2.connectedComponentsWithStats(sil.astype(np.uint8), connectivity=8)
    if n > 1:
        big = 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA]))
        sil = lbl == big
        fig = fig | sil
        reach = reach & ~sil
    n, lbl, st, _ = cv2.connectedComponentsWithStats(fig.astype(np.uint8), connectivity=8)
    keep = np.zeros(n, bool); keep[1:] = st[1:, cv2.CC_STAT_AREA] >= 150
    fig = keep[lbl]
    sh_a = np.where(reach & shadow, np.clip(-dL / (bg[0] - 150.0), 0, 1) * 0.85, 0.0)
    gl_a = np.where(reach & glow & ~shadow, np.clip(da / 22.0, 0, 0.75), 0.0)
    return fig, sil, sh_a, gl_a, bg


def bodies(fig, sil, ground, min_area=2500):
    """Split the figure mask into figures: big components above the ground band are the bodies (parts can overlap in columns, so no
    column cuts); every other figure pixel (feet, the shadow cores joining them, a detached tip) goes to the nearest body."""
    box = np.zeros_like(sil)                          # everything inside the silhouette's box is the silhouette (the paper between its legs too)
    if sil.any():
        ys, xs = np.nonzero(sil); box[ys.min():ys.max() + 1, xs.min():xs.max() + 1] = True
    up = fig & ~box; up[max(0, ground - 45):] = False
    up = cv2.erode(up.astype(np.uint8), np.ones((5, 5), np.uint8), iterations=2)   # break thin bridges (touching outlines, shadow cores)
    n, lbl, st, _ = cv2.connectedComponentsWithStats(up, connectivity=8)
    big = [k for k in range(1, n) if st[k, cv2.CC_STAT_AREA] >= min_area]
    marks = np.zeros(fig.shape, np.uint8)
    inside = lambda k: box[int(st[k, cv2.CC_STAT_TOP] + st[k, cv2.CC_STAT_HEIGHT] / 2), int(st[k, cv2.CC_STAT_LEFT] + st[k, cv2.CC_STAT_WIDTH] / 2)]
    order = sorted([k for k in big if not inside(k)], key=lambda k: st[k, cv2.CC_STAT_LEFT] + st[k, cv2.CC_STAT_WIDTH] / 2)
    for i, k in enumerate(order):
        marks[lbl == k] = i + 2
    marks[sil] = 1
    # grow the markers through the figure mask (geodesic), so a foot goes to the body it is joined to, not the nearest one
    owner = marks.copy()
    for _ in range(400):
        grown = cv2.dilate(owner, np.ones((3, 3), np.uint8))
        new = (owner == 0) & fig & (grown > 0)
        if not new.any(): break
        owner[new] = grown[new]
    left = fig & (owner == 0)                         # bits joined to nothing: the nearest marker takes them
    if left.any():
        src = np.where(marks > 0, 0, 255).astype(np.uint8)
        _, near = cv2.distanceTransformWithLabels(src, cv2.DIST_L2, 5, labelType=cv2.DIST_LABEL_PIXEL)
        ys, xs = np.nonzero(marks > 0)
        lut = np.zeros(near.max() + 1, np.uint8); lut[near[ys, xs]] = marks[ys, xs]
        owner[left] = lut[near[left]]
    out = [owner == 1] if sil.any() else []
    return out + [owner == i for i in range(2, len(order) + 2)]


def holes(rgb, fig, sil, bg, parts, low_share=0.33, red_share=0.25):
    """Paper seen through a figure (between legs, inside a signal's arch or hoop) is enclosed, so the fill can't reach it. Colour can't
    tell it from cream enamel (sheet A's cream is the paper's), so place decides: an enclosed paper patch in a figure's lower third,
    or one ringed by signal red, is a hole."""
    lab = cv2.cvtColor(rgb, cv2.COLOR_RGB2LAB).astype(np.float32)
    dL, dab = lab[..., 0] - bg[0], np.hypot(lab[..., 1] - bg[1], lab[..., 2] - bg[2])
    red = lab[..., 1] - bg[1] > 25
    glowy = (lab[..., 0] > 188) & (lab[..., 1] - bg[1] >= 3) & (dab < 22)
    tight = (((np.abs(dL) < 7) & (dab < 5) | glowy) & fig & ~sil).astype(np.uint8)
    tight = cv2.morphologyEx(tight, cv2.MORPH_OPEN, np.ones((3, 3), np.uint8))
    n, lbl, st, _ = cv2.connectedComponentsWithStats(tight, connectivity=4)
    spans = []
    for m in parts:
        ys = np.nonzero(m.any(1))[0]
        spans.append((ys.min(), ys.max()) if len(ys) else (0, 0))
    out = np.zeros_like(fig)
    for k in range(1, n):
        if st[k, cv2.CC_STAT_AREA] < 400: continue
        c = lbl == k
        x, y, w, h = st[k, :4]
        sub = (slice(max(0, y - 6), y + h + 6), slice(max(0, x - 6), x + w + 6))
        cc = c[sub]
        ring = cv2.dilate(cc.astype(np.uint8), np.ones((9, 9), np.uint8)).astype(bool) & ~cc
        reds = (ring & red[sub]).sum() / max(1, ring.sum())
        own = int(np.argmax([(c & m).sum() for m in parts]))
        top, feet = spans[own]
        low = y + h >= feet - low_share * (feet - top) and not glowy[c].mean() > 0.5   # a glow patch counts only when ringed by red
        if low or reds > red_share:
            out |= c
    loose = (np.abs(dL) < 14) & (dab < 10) & fig
    grow = cv2.dilate(out.astype(np.uint8), np.ones((5, 5), np.uint8), iterations=2).astype(bool)
    return out | (grow & loose)


def main(a):
    sheet, out, prefix = Path(a[0]), Path(a[1]), a[2]
    scale_m = float(arg(a, '--scale-m', 1.75))
    out.mkdir(parents=True, exist_ok=True)
    rgb = cv2.cvtColor(cv2.imread(str(sheet)), cv2.COLOR_BGR2RGB)
    h, w = rgb.shape[:2]
    fig, sil, sh_a, gl_a, bg = masks(rgb)
    ground = int(np.nonzero(sil.any(1))[0].max()) if sil.any() else int(np.percentile(np.nonzero(fig)[0], 97))
    parts = bodies(fig, sil, ground)
    if len(parts) != 7:
        print(f'{sheet.name}: {len(parts)} figures, expected 7 (silhouette + 6)', file=sys.stderr)
    hole = holes(rgb, fig, sil, bg, parts)
    parts = [m & ~hole for m in parts]
    spans = [(int(np.nonzero(m.any(0))[0].min()), int(np.nonzero(m.any(0))[0].max()) + 1) for m in parts]
    figs = []
    for i, m in enumerate(parts):
        ys, xs_ = np.nonzero(m)
        if not len(ys): continue
        top, feet = int(ys.min()), int(ys.max())
        bx0, bx1 = int(xs_.min()), int(xs_.max()) + 1
        # shadow: in this figure's columns (a little wider) and from its lower half down to 60 px under its feet
        sx0, sx1 = max(0, bx0 - 40), min(w, bx1 + 40)
        if i + 1 < len(spans): sx1 = min(sx1, max(bx1, spans[i + 1][0]))
        if i > 0: sx0 = max(sx0, min(bx0, spans[i - 1][1]))
        sy0, sy1 = (top + feet) // 2, min(h, feet + 60)
        sh = np.zeros_like(sh_a); sh[sy0:sy1, sx0:sx1] = sh_a[sy0:sy1, sx0:sx1]
        gl = np.zeros_like(gl_a); gl[max(0, top - 40):feet, sx0:sx1] = gl_a[max(0, top - 40):feet, sx0:sx1]
        cx0, cx1, cy0, cy1 = sx0, sx1, max(0, top - 40), sy1
        rgba = np.zeros((cy1 - cy0, cx1 - cx0, 4), np.float32)
        fm = m[cy0:cy1, cx0:cx1]
        sa, ga = sh[cy0:cy1, cx0:cx1], gl[cy0:cy1, cx0:cx1]
        halo = np.where((ga > sa)[..., None], np.array([236, 64, 44], np.float32), np.array([58, 44, 34], np.float32))
        rgba[..., :3] = np.where(fm[..., None], rgb[cy0:cy1, cx0:cx1], halo)
        rgba[..., 3] = np.where(fm, 1.0, np.maximum(sa, ga)) * 255
        # trim columns with nothing in them
        used = np.nonzero(rgba[..., 3].max(0) > 8)[0]
        rgba = rgba[:, used.min():used.max() + 1]
        cx0 += int(used.min())
        name = 'silhouette' if i == 0 else f'{prefix}{i}'
        cv2.imwrite(str(out / f'{name}.png'), cv2.cvtColor(rgba.astype(np.uint8), cv2.COLOR_RGBA2BGRA))
        figs.append({'id': name, 'box': [bx0, top, bx1, feet + 1], 'crop': [cx0, cy0, cx0 + rgba.shape[1], cy1], 'feet_y_in_crop': feet - cy0,
                     'centre_x_in_crop': (bx0 + bx1) / 2 - cx0, 'height_px': feet - top + 1, 'width_px': bx1 - bx0})
    sil = figs[0]['height_px'] if figs and figs[0]['id'] == 'silhouette' else None
    for f in figs:
        f['height_m'] = round(f['height_px'] / sil * scale_m, 2) if sil else None
        f['width_m'] = round(f['width_px'] / sil * scale_m, 2) if sil else None
    meta = {'sheet': str(sheet), 'size': [w, h], 'paper_lab': [round(float(v), 1) for v in bg], 'silhouette_px': sil, 'scale_m': scale_m, 'figures': figs}
    (out / 'cuts.json').write_text(json.dumps(meta, indent=1))
    dbg = (rgb * 0.35 + np.array([40, 90, 230]) * 0.65).astype(np.uint8)
    tints = [(255, 255, 255), (255, 120, 120), (120, 255, 120), (255, 255, 120), (120, 255, 255), (255, 120, 255), (255, 180, 90)]
    for i, m in enumerate(parts):
        dbg[m] = (rgb[m] * 0.6 + np.array(tints[i % len(tints)]) * 0.4).astype(np.uint8)
    for f in figs:
        x0, y0, x1, y1 = f['box']; cv2.rectangle(dbg, (x0, y0), (x1, y1), (255, 0, 0), 2)
    cv2.imwrite(str(out / 'debug.jpg'), cv2.cvtColor(dbg, cv2.COLOR_RGB2BGR), [cv2.IMWRITE_JPEG_QUALITY, 80])
    for f in figs:
        print(f"{f['id']:>10}  h {f['height_px']:4d} px  {f['height_m']} m  w {f['width_m']} m")
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
