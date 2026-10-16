"""Glyph, brief 014 review round 2 (2026-10-16): kit art fixes.
Run: python3 -I briefs/014/glyph/scripts/round2_art.py  (needs numpy, opencv-python, Pillow; run from the repo root)
 1. roster_column: the 6 frame screws (4 corners, 2 mid-rail pairs) moved 6 px (@2x) in from the rails and 3 px from the cap rims, so no screw touches a rail or a cap.
 2. plate_verbs: rebuilt at the size it is drawn (96x74 @1x, 192x148 @2x): the old art had half bolts at the 9-slice split that smeared when the plate was
    stretched; now whole screws (the pill/roster screw family), centred on the plate, nothing in the stretched band, so the plate must be drawn at exactly 74 px tall.
 3. portrait_ring_inner: a selection ring that sits inside the bezel (over the photo), 78 px @1x / 156 px @2x, for the clip circle r 39.
Originals are in git (1f8e3e3)."""
import cv2, numpy as np, sys
from PIL import Image
KIT = "briefs/011/ui/kit/"

def load(p): return np.array(Image.open(KIT + p).convert("RGBA"))
def save(a, p): Image.fromarray(a).save(KIT + p, optimize=True)

def disc(h, w, cx, cy, r, feather=1.2):
    yy, xx = np.mgrid[0:h, 0:w]
    d = np.sqrt((xx - cx) ** 2 + (yy - cy) ** 2)
    return np.clip((r - d) / feather + 0.5, 0, 1)

def take_patch(img, cx, cy, r=8):
    n = 2 * r + 1
    return img[cy - r:cy + r + 1, cx - r:cx + r + 1].copy()

def paste_screw(img, patch, cx, cy, r_opaque=7.2):
    n = patch.shape[0]; r = n // 2
    m = disc(n, n, r, r, r_opaque + 0.8, 1.2)
    # the old screws overlapped the rails: drop teal rail pixels (saturated green, a screw is grey) and the cream rail edge on the rail side
    pr, pg, pb = [patch[..., i].astype(int) for i in range(3)]
    rail = ((pg - pr) > 30) | ((pr > 170) & (pg > 170) & (pb < pr - 18))
    m = (m * (1 - cv2.dilate(rail.astype(np.uint8), np.ones((3, 3), np.uint8)))).astype(float)[..., None]
    reg = img[cy - r:cy + r + 1, cx - r:cx + r + 1].astype(float)
    reg[..., :3] = reg[..., :3] * (1 - m) + patch[..., :3].astype(float) * m
    img[cy - r:cy + r + 1, cx - r:cx + r + 1] = np.clip(reg, 0, 255).astype(np.uint8)

def erase_disc(img, cx, cy, r, keep_rail_x=16, rail_src_dy=-16, rail_from=None, rail_to=None):
    """fill the old screw: the field from 18 px to the right (plain plate), the rail columns from the rail 16 px above/below"""
    h, w = img.shape[:2]
    out = img.copy()
    m = disc(h, w, cx, cy, r, 0.01) > 0.5
    ys, xs = np.where(m)
    for y, x in zip(ys, xs):
        side = 1 if cx < w / 2 else -1
        on_rail_col = (side == 1 and x < keep_rail_x) or (side == -1 and x > w - 1 - keep_rail_x)
        in_rail_rows = (rail_from is None or y >= rail_from) and (rail_to is None or y <= rail_to)
        if on_rail_col and in_rail_rows:
            out[y, x] = img[y + rail_src_dy, x]
        else:
            out[y, x] = img[y, x + side * 18]
    return out

def roster():
    a = load("roster/roster_column@2x.png")
    H, W = a.shape[:2]
    screws = [(19, 49), (199, 49), (19, 234), (199, 234), (19, 424), (199, 424), (19, 781), (199, 781)]
    # (199, 424) was a weaker match; the pair is mirrored, so take its x from 218 - 19
    patches = {}
    for (x, y) in screws:
        patches[(x, y)] = take_patch(a, x, y, 9)
    b = a.copy()
    for (x, y) in screws:
        # the top pair reads rail below, the others rail above
        dy = 16 if y < 100 else -16
        b = erase_disc(b, x, y, 9.5, rail_src_dy=dy, rail_from=49 if y < 100 else None, rail_to=780 if y > 700 else None)
    # the rails beside the two mid pairs: rewrite from a clean stretch of the same rail (they are uniform down their length)
    for cy in (234, 424):
        for x0, x1 in ((0, 18), (W - 18, W)):
            b[cy - 13:cy + 14, x0:x1] = a[150 - 13 + 13:150 + 14 + 13, x0:x1]
    for (x, y) in screws:
        nx = x + (6 if x < W / 2 else -6)
        ny = y + (3 if y < 100 else (-3 if y > 700 else 0))
        paste_screw(b, patches[(x, y)], nx, ny)
    save(b, "roster/roster_column@2x.png")
    s = cv2.resize(b, (109, 416), interpolation=cv2.INTER_AREA)
    save(s, "roster/roster_column@1x.png")

PLATE_H2 = 156   # 78 px @1x: knob row, numerals under it, and 5 px of field above and below (see the review)

def plate():
    old = load("panels/plate_verbs@2x.png")          # 192 x 108, slices 54/53/54/53 at @2x (original in git, 1f8e3e3)
    W = old.shape[1]
    ys0 = np.arange(35, 58)
    clean = old.copy()
    for x0, x1 in ((24, 58), (134, 168)):             # the old half-bolts (rows 41..52, x 28..53 / 138..163) and their shadow
        for x in range(x0, x1):
            top, bot = clean[34, x].astype(float), clean[58, x].astype(float)
            for y in range(35, 58):
                t = (y - 34) / 24.0
                clean[y, x] = np.clip(top * (1 - t) + bot * t, 0, 255).astype(np.uint8)
    # round 3: the old end caps are round, and rows 52..55 are the extreme rows (row 53 is an outlier), so repeating a row left two seams in the
    # rim. Keep rows 0..51 and 56..107 and morph from row 51 to row 56 over the middle band: no repeated row, no seam.
    top = clean[:52]; bot = clean[56:]
    n = PLATE_H2 - 104
    t = ((np.arange(n) + 1) / (n + 1))[:, None, None]
    mid = np.clip(clean[51].astype(float)[None] * (1 - t) + clean[56].astype(float)[None] * t, 0, 255).astype(np.uint8)
    new = np.concatenate([top, mid, bot])
    # whole screws, the pill's screw family at 1.25x, centred on the plate
    pill = load("panels/pill_status@2x.png")
    sc = take_patch(pill, 35, 14, 9)
    big = cv2.resize(sc, (23, 23), interpolation=cv2.INTER_CUBIC)
    r = big.shape[0] // 2
    cy = PLATE_H2 // 2
    for cx in (41, W - 41):
        reg = new[cy - r:cy + r + 1, cx - r:cx + r + 1].astype(float)
        m = disc(2 * r + 1, 2 * r + 1, r, r, 8.6, 1.4)[..., None]
        reg[..., :3] = reg[..., :3] * (1 - m) + big[..., :3].astype(float) * m
        new[cy - r:cy + r + 1, cx - r:cx + r + 1] = np.clip(reg, 0, 255).astype(np.uint8)
    save(new, "panels/plate_verbs@2x.png")
    save(cv2.resize(new, (W // 2, PLATE_H2 // 2), interpolation=cv2.INTER_AREA), "panels/plate_verbs@1x.png")

def ring_inner():
    """Selection ring that lives inside the bezel: 78 px (r 39) at @1x. Outer band turquoise, a cream line inside it, a soft glow inward. Draw it over
    the photo and its scrim, under the health pips and the key badge; clip it to the same circle as the photo."""
    S = 4; N = 156 * S; c = N / 2.0
    yy, xx = np.mgrid[0:N, 0:N]
    r = np.sqrt((xx + 0.5 - c) ** 2 + (yy + 0.5 - c) ** 2) / S      # radius in @2x px, edge at 78
    def a(lo, hi, f=0.6): return np.clip((r - lo) / f + 0.5, 0, 1) * np.clip((hi - r) / f + 0.5, 0, 1)
    turq = np.array([28, 185, 198], float); light = np.array([96, 224, 234], float); cream = np.array([247, 244, 236], float)
    rgb = np.zeros((N, N, 3)); al = np.zeros((N, N))
    # glow, inside (r 58..69), turquoise-light fading to nothing
    g = np.clip((r - 58) / 11.0, 0, 1) ** 1.6 * (r < 69.5) * 0.42
    rgb[:] = light; al = np.maximum(al, g)
    # cream line (r 68.6..70.4), 0.9
    ln = a(68.6, 70.4) * 0.92
    rgb = rgb * (1 - ln[..., None]) + cream * ln[..., None]; al = np.maximum(al, ln)
    # band (r 70.4..78+), turquoise, a little lighter toward the inner edge
    t = np.clip((r - 70.4) / 7.6, 0, 1)
    bandc = light * (1 - t[..., None]) * 0.45 + turq * (1 - (1 - t[..., None]) * 0.45)
    bd = np.clip((r - 70.4) / 0.6 + 0.5, 0, 1) * (r < 79)
    rgb = rgb * (1 - bd[..., None]) + bandc * bd[..., None]; al = np.maximum(al, bd)
    out = np.dstack([rgb, al * 255]).astype(np.float32)
    # premultiply for the downscale so the edge does not fringe
    pm = out.copy(); pm[..., :3] *= (out[..., 3:4] / 255.0)
    pm = cv2.resize(pm, (156, 156), interpolation=cv2.INTER_AREA)
    alpha = pm[..., 3:4]; col = np.where(alpha > 0.5, pm[..., :3] / np.maximum(alpha / 255.0, 1e-3), 0)
    res = np.clip(np.dstack([col, alpha]), 0, 255).astype(np.uint8)
    save(res, "portraits/portrait_ring_inner@2x.png")
    save(cv2.resize(res, (78, 78), interpolation=cv2.INTER_AREA), "portraits/portrait_ring_inner@1x.png")

if __name__ == "__main__":
    which = sys.argv[1:] or ["roster"]
    if "roster" in which: roster()
    if "plate" in which: plate()
    if "ring" in which: ring_inner()
