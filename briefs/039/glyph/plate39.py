# Glyph, brief 039 (2026-10-26): the clean plate of the opening square, and the figures laid into it.
# The 0.2.122 opening capture (base/start-overhead-day.png) has the cell standing in it. A second capture of the same square at the same zoom and yaw
# (out/batch/shots/cafe-front-day.png, panned by (-236, +283) px, the cell elsewhere) is an exact picture of the paving without them: the map camera is orthographic, so a pan is a pure translation.
# The cell, their rings and shadows (and the three walkers, who would have scattered) are taken out of the opening frame with that second capture; the figures are then rendered at the play camera
# by Forma's proof renderer (a copy, proof39.py: real figures, real library clips) and laid in with their cast shadows. A mock-up: not Unity's pixels, a relative read of what a pose looks like at 53 px.
import sys, os, math
sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/039/glyph")
import numpy as np, cv2
from PIL import Image, ImageFilter
import fig39 as f39
R = "/Users/sky/Projects/suborned/"
START = R + "briefs/039/glyph/base/start-overhead-day.png"
CAFE = R + "out/batch/shots/cafe-front-day.png"
TMP = R + "out/scratch/glyph/039/"
SHIFT = (-236.0, 283.0)        # a point (x, y) of the opening frame is at (x - 236, y + 283) in the café-front frame
PPM = 1080 / 28.0              # 38.571 px per metre across the ground
SINP = math.sin(math.radians(38.0))
YAW = 30.0

def _aligned_cafe():
    """the café-front frame resampled into the opening frame's coordinates (sub-pixel: the two frames differ by (0.0, -0.28) px from the integer pan), with a validity mask"""
    a = np.array(Image.open(START).convert("RGB")); b = np.array(Image.open(CAFE).convert("RGB"))
    ga = cv2.cvtColor(a, cv2.COLOR_RGB2GRAY).astype(np.float32); gb = cv2.cvtColor(b, cv2.COLOR_RGB2GRAY).astype(np.float32)
    # the whole pan as an affine: B(x - 236 + e, y + 283 + e') = A(x, y); refine on the long hedge, which is static and sharp
    x0, y0, x1, y1 = 600, 372, 1000, 470
    w = np.eye(2, 3, dtype=np.float32)
    _, w = cv2.findTransformECC(ga[y0:y1, x0:x1], gb[y0 + 283:y1 + 283, x0 - 236:x1 - 236].copy(), w, cv2.MOTION_TRANSLATION, (cv2.TERM_CRITERIA_EPS | cv2.TERM_CRITERIA_COUNT, 200, 1e-7), None, 5)
    M = np.array([[1, 0, SHIFT[0] + float(w[0, 2])], [0, 1, SHIFT[1] + float(w[1, 2])]], np.float32)     # A coords -> B coords
    al = cv2.warpAffine(b, M, (1920, 1080), flags=cv2.INTER_CUBIC | cv2.WARP_INVERSE_MAP, borderMode=cv2.BORDER_CONSTANT, borderValue=0)
    valid = cv2.warpAffine(np.full(b.shape[:2], 255, np.uint8), M, (1920, 1080), flags=cv2.INTER_NEAREST | cv2.WARP_INVERSE_MAP, borderValue=0)
    return a, al, valid

def _roof_mask(img):
    """the white roof plane at the left: low saturation, bright (the pavement is warm and saturated)"""
    hsv = cv2.cvtColor(img, cv2.COLOR_RGB2HSV); return ((hsv[..., 1] < 55) & (hsv[..., 2] > 195)).astype(np.uint8)

CELL_FEET = [(850, 596), (800, 652), (893, 657), (822, 731)]
WALKERS = [(1315, 325), (1350, 290), (1380, 325)]      # the three passers-by at the top right of the square

def clean_plate():
    """the opening frame without the cell, their rings and shadows, and without the walkers: returns (uint8 H x W x 3)"""
    a, al, valid = _aligned_cafe()
    out = a.copy()
    d = np.abs(cv2.GaussianBlur(a.astype(np.float32), (0, 0), 0.8) - cv2.GaussianBlur(al.astype(np.float32), (0, 0), 0.8)).sum(axis=2)
    diff = (d > 42).astype(np.uint8)
    roof = cv2.dilate(_roof_mask(al), np.ones((7, 7), np.uint8))                              # keep the opening frame's own roof edge and its soft cut
    # the cell: everything that differs inside a box around them (figures, rings, long shadows), minus the roof
    box = np.zeros(diff.shape, np.uint8); box[500:790, 715:960] = 1
    m = (diff & box & (1 - roof)).astype(np.uint8)
    m = cv2.morphologyEx(m, cv2.MORPH_OPEN, np.ones((2, 2), np.uint8))
    n, lab, st, _ = cv2.connectedComponentsWithStats(m)
    keep = np.zeros_like(m)
    for i in range(1, n):
        if st[i, cv2.CC_STAT_AREA] >= 40: keep[lab == i] = 1
    keep = cv2.dilate(keep, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (11, 11)))
    keep = (keep & (1 - roof)).astype(np.uint8)
    # the walkers: only where the café-front frame is clean (it has the Scout and the Fixer on the path at x 1243..1285, y 250..317)
    wk = np.zeros(diff.shape, np.uint8); wk[250:372, 1240:1415] = 1
    ok = np.zeros(diff.shape, np.uint8); ok[:, 1290:] = 1; ok[322:, :] = 1
    wm = (diff & wk & ok).astype(np.uint8); wm = cv2.morphologyEx(wm, cv2.MORPH_OPEN, np.ones((2, 2), np.uint8))
    n2, lab2, st2, _ = cv2.connectedComponentsWithStats(wm); wm2 = np.zeros_like(wm)
    for i in range(1, n2):
        if st2[i, cv2.CC_STAT_AREA] >= 30: wm2[lab2 == i] = 1
    wm = cv2.dilate(wm2, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (11, 11))) & ok
    # what the roof exclusion left: slivers of the cell's cyan rings, and a stray speck
    cy = ((a[..., 2].astype(int) > 140) & (a[..., 1].astype(int) > 130) & (a[..., 0].astype(int) < 175) & (a[..., 2].astype(int) - a[..., 0].astype(int) > 25)).astype(np.uint8); cbox = np.zeros_like(cy); cbox[500:790, 715:960] = 1
    rm = cv2.dilate(cy & cbox, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (9, 9)))                       # slivers: filled from their own neighbourhood, not from the café frame (its roof edge is different there)
    sp = np.zeros_like(cy); sp[686:698, 826:841] = 1
    keep = np.maximum(keep, sp)
    allm = np.maximum(keep, wm).astype(np.float32)
    soft = cv2.GaussianBlur(allm, (0, 0), 1.6); soft = np.clip(soft * 1.25, 0, 1) * (valid > 0)
    res = (a.astype(np.float32) * (1 - soft[..., None]) + al.astype(np.float32) * soft[..., None])
    res = np.clip(res + 0.5, 0, 255).astype(np.uint8)
    res = cv2.inpaint(res, (rm & (1 - (keep > 0))).astype(np.uint8) * 255, 3, cv2.INPAINT_TELEA)
    return res, allm

if __name__ == "__main__":
    p, m = clean_plate(); Image.fromarray(p).save(TMP + "plate_clean.png"); Image.fromarray((m * 255).astype(np.uint8)).save(TMP + "plate_clean_mask.png")
    Image.fromarray(p).crop((700, 260, 1500, 820)).resize((1600, 1120), Image.LANCZOS).save(TMP + "plate_clean_crop.png"); print("ok")


# ----------------------------------------------------------------------------- figures at the play camera, laid into the plate
def heading_to(frm, to):
    """Unity heading (degrees, 0 = north, +90 = east) of a figure at game px `frm` facing game px `to`: the ground is foreshortened by sin 38 on screen, the yaw is 30"""
    sx = (to[0] - frm[0]) / PPM; sy = -(to[1] - frm[1]) / (PPM * SINP)                  # metres on the ground, right and up the screen
    r = (math.cos(math.radians(YAW)), -math.sin(math.radians(YAW))); fw = (math.sin(math.radians(YAW)), math.cos(math.radians(YAW)))
    e = sx * r[0] + sy * fw[0]; n = sx * r[1] + sy * fw[1]
    return math.degrees(math.atan2(e, n)) % 360.0

def screen_dir(heading, metres=1.0, rise=0.0):
    """game px offset of a point `metres` ahead of a figure facing `heading` and `rise` metres up"""
    w = (math.sin(math.radians(heading)), math.cos(math.radians(heading)))
    r = (math.cos(math.radians(YAW)), -math.sin(math.radians(YAW))); fw = (math.sin(math.radians(YAW)), math.cos(math.radians(YAW)))
    sx = (w[0] * r[0] + w[1] * r[1]) * metres; sy = (w[0] * fw[0] + w[1] * fw[1]) * metres
    return (sx * PPM, -sy * PPM * SINP - rise * math.cos(math.radians(38.0)) * PPM)

class Fig:
    """one figure (or a carrier with the carried): id, library clip (None = a People clip by name), frame, heading, feet in game px"""
    def __init__(self, fid, clip, frame, feet, heading=None, face=None, lib=True, label=None, extra=None):
        self.fid, self.clip, self.frame, self.feet = fid, clip, frame, feet
        self.heading = heading if heading is not None else (heading_to(feet, face) if face else 210.0)
        self.lib, self.label, self.extra = lib, label, extra or []
        self.bbox = None; self.head = None; self.chest = None; self.dims = None; self.area = None                                     # filled when laid: alpha bounding box in game px (x0, y0, x1, y1), the top of the head (x, y)

def lay(plate, fg, clean=None, occluders=(), size=260, samples=96):
    """render one figure (and any `extra` figures attached to it) alone at its place and composite it into plate (uint8 H x W x 3, edited in place) with its cast shadow.
    occluders: polygons in game px (a hedge, a planter) whose pixels are restored from `clean` over the figure afterwards, so the prop stands in front of him.
    Sets fg.bbox (alpha bounding box, game px) and fg.head (the top of his head: x of the highest rows, y of the top)."""
    clip = f39.lib(fg.clip) if fg.lib else fg.clip
    figs = [dict(id=fg.fid, heading=fg.heading, x=0.0, y=0.0, clip=clip, frame=fg.frame)]
    for e in fg.extra:
        e = dict(e); lib_ = e.pop('lib', False); bone = e.pop('bone', 'RightShoulder'); hips = e.pop('hips', [0, 0, 0])
        e['clip'] = f39.lib(e['clip']) if lib_ else e['clip']; e.update(x=0.0, y=0.0, attach=dict(to=0, bone=bone, hips=hips)); figs.append(e)
    cx, cy = int(round(fg.feet[0])), int(round(fg.feet[1])); x0, y0 = cx - size // 2, cy - size // 2
    bg = plate[y0:y0 + size, x0:x0 + size]
    im, pref = f39.render(figs, size, size, look=(0, 0, 0), ppm=PPM, samples=samples, bg=bg)
    F = f39.pp.load_pass(pref, "figs"); alpha = np.clip(F[..., 3], 0, 1)
    fg.area = int((alpha > 0.5).sum())
    ys, xs = np.where(alpha > 0.15)
    if len(xs):
        fg.bbox = (int(x0 + xs.min()), int(y0 + ys.min()), int(x0 + xs.max() + 1), int(y0 + ys.max() + 1))
        top = ys.min(); sel = xs[ys < top + 5]; fg.head = (float(x0 + sel.mean()), float(y0 + top))
        h_ = ys.max() - ys.min() + 1; band = (ys >= top + 0.25 * h_) & (ys <= top + 0.55 * h_)
        fg.chest = (float(x0 + xs[band].mean()), float(y0 + ys[band].mean())) if band.any() else (float(x0 + xs.mean()), float(y0 + ys.mean()))
    try:
        import json as _j; fg.dims = _j.load(open(pref + "_dims.json"))[0]
    except Exception: fg.dims = None
    G = f39.pp.load_pass(pref, "ground"); S = f39.pp.load_pass(pref, "shadow")
    ratio = (S[..., :3] / np.maximum(G[..., :3], 1e-4)).mean(-1)
    ratio = np.where(cv2.dilate((alpha > 0.02).astype(np.uint8), np.ones((5, 5), np.uint8)) > 0, 1.0, ratio)       # not under the figure's own holdout
    shade = np.clip((1.0 - cv2.GaussianBlur(ratio.astype(np.float32), (0, 0), 0.8)) * 6.0, 0, 1)
    m = np.clip(np.maximum(alpha, shade), 0, 1)
    out = (plate[y0:y0 + size, x0:x0 + size].astype(np.float32) * (1 - m[..., None]) + im.astype(np.float32) * m[..., None])
    region = np.clip(out + 0.5, 0, 255).astype(np.uint8)
    for poly in occluders:
        pm = np.zeros((1080, 1920), np.uint8); cv2.fillPoly(pm, [np.array(poly, np.int32)], 255); pm = cv2.GaussianBlur(pm, (0, 0), 0.7)[y0:y0 + size, x0:x0 + size].astype(np.float32) / 255.0
        region = np.clip(region.astype(np.float32) * (1 - pm[..., None]) + clean[y0:y0 + size, x0:x0 + size].astype(np.float32) * pm[..., None] + 0.5, 0, 255).astype(np.uint8)
    plate[y0:y0 + size, x0:x0 + size] = region
    return plate
