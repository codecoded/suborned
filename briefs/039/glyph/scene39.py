# Glyph, brief 039 (2026-10-26): the one scene the three fight options are drawn on.
# Base = the 0.2.122 capture of the opening square at play zoom, yaw 30 (briefs/041/build/0311/shots/start-overhead-day.png, copied to base/); the cell stand where the capture has them.
# The three rivals are cut out of the 2026-10-18 capture of rivals on the same paving (out/latch/x/rivals.png) and stood where the fight needs them, a planter in
# front of two of them. Poses are stand-ins (Forma has no crouch, duck or lean yet); every mark on top is drawn from the kit by g39.py.
import sys, math
sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/067")
from g67 import *
import numpy as np, cv2

SQSHOT = R + "briefs/039/glyph/base/start-overhead-day.png"
SQHUD = R + "briefs/039/glyph/base/start-overhead-day.hud.png"
RIV = R + "out/latch/x/rivals.png"
TMP = R + "out/scratch/glyph/039/"

# game px (1920 x 1080) of what the capture already holds: the cell's feet, and their heads (top of the figure)
CELL = {"scout": (850, 596), "fixer": (800, 652), "tech": (893, 657), "enforcer": (822, 731)}
HEAD = {"scout": (848, 548), "fixer": (800, 605), "tech": (893, 612), "enforcer": (822, 678)}
FACE = {"scout": SCOUT, "fixer": FIXER, "tech": TECH, "enforcer": ENF}
# rivals: feet, head top
RIVS = {"r1": ((1318, 552), (1318, 486)), "r2": ((1185, 382), (1185, 318)), "r3": ((1110, 690), (1110, 626))}
# planters in front of r1 and r2 (polygons in game px, read off the capture)
PL_S = [(1213, 576), (1232, 522), (1250, 500), (1268, 492), (1306, 510), (1299, 530), (1278, 574), (1272, 598), (1244, 608), (1216, 598)]    # the west pill planter
PL_HEDGE = [(1110, 388), (1160, 376), (1200, 367), (1236, 358), (1240, 398), (1200, 406), (1160, 411), (1110, 418)]
RIV_H = 66.0           # a rival is about 66 game px tall in the 0.2.122 frames (the cell about 50)

def _cut(box, name, maxlum=None):
    """grabCut the figure out of the old capture; returns an RGBA array (cached on disk)"""
    import os
    p = TMP + "cutfull_%s.png" % name
    if os.path.exists(p): return Image.open(p).convert("RGBA")
    a = np.array(Image.open(RIV).convert("RGB")); x0, y0, x1, y1 = box; sub = a[y0:y1, x0:x1].copy(); h, w = sub.shape[:2]
    lum = 0.3 * sub[..., 0] + 0.59 * sub[..., 1] + 0.11 * sub[..., 2]; r, g, b = [sub[..., i].astype(int) for i in range(3)]
    ring = (b > 150) & (b - r > 55) & (b - g > 80); dark = (lum < 105) & (~ring)
    mask = np.full((h, w), cv2.GC_BGD, np.uint8); mask[cv2.dilate(dark.astype(np.uint8), np.ones((9, 9), np.uint8)) > 0] = cv2.GC_PR_FGD
    mask[cv2.erode(dark.astype(np.uint8), np.ones((3, 3), np.uint8)) > 0] = cv2.GC_FGD; mask[ring] = cv2.GC_BGD
    cyan = (r < 150) & (g > 165) & (b > 165); cyan = cv2.dilate(cyan.astype(np.uint8), np.ones((9, 9), np.uint8)) > 0   # the old capture's reach ring crosses one figure
    cv2.grabCut(sub, mask, None, np.zeros((1, 65)), np.zeros((1, 65)), 6, cv2.GC_INIT_WITH_MASK)
    fg = ((mask == cv2.GC_FGD) | (mask == cv2.GC_PR_FGD)).astype(np.uint8) * 255
    n, lab, st, _ = cv2.connectedComponentsWithStats(fg)
    if n > 1: fg = ((lab == 1 + np.argmax(st[1:, cv2.CC_STAT_AREA])) * 255).astype(np.uint8)
    if maxlum: fg[lum > maxlum] = 0                                          # drop the light bar of the old reach ring
    n, lab, st, _ = cv2.connectedComponentsWithStats(fg)
    if n > 1: fg = ((lab == 1 + np.argmax(st[1:, cv2.CC_STAT_AREA])) * 255).astype(np.uint8)
    fg = cv2.morphologyEx(fg, cv2.MORPH_CLOSE, np.ones((3, 3), np.uint8)); fg = cv2.erode(fg, np.ones((2, 2), np.uint8))
    band = (fg > 0) & (cv2.erode(fg, np.ones((5, 5), np.uint8)) == 0)         # the 2 px edge carries the old background's colour: take it from inside
    sub = cv2.inpaint(sub, band.astype(np.uint8) * 255, 3, cv2.INPAINT_TELEA)
    holes = (cyan & (fg > 0)).astype(np.uint8) * 255
    if holes.any(): sub = cv2.inpaint(sub, holes, 5, cv2.INPAINT_TELEA)
    im = Image.fromarray(np.dstack([sub, fg]), "RGBA"); im.save(p); return im

def cutouts():
    return {"A": _cut((715, 45, 810, 200), "A"), "C": _cut((930, 385, 1005, 515), "C")}

def _shadow(fr, im, feet):
    """a soft cast shadow to the lower left (the capture's sun), from the cutout's own silhouette; the cutout stands with its feet at the bottom centre"""
    a = np.array(im.split()[3]).astype(np.float32) / 255.0; h, w = a.shape; pad = int(h * 0.8)
    big = np.zeros((h + 2 * pad, w + 2 * pad), np.float32); big[pad:pad + h, pad:pad + w] = a
    fx, fy = pad + w / 2.0, pad + h - 1.0
    M = np.float32([[1, 0.62, 0], [0, -0.30, 0]]); M[:, 2] = np.array([fx, fy]) - M[:, :2] @ np.array([fx, fy])
    sh = cv2.warpAffine(big, M, (big.shape[1], big.shape[0]), flags=cv2.INTER_LINEAR); sh = cv2.GaussianBlur(sh, (0, 0), 2.4)
    layer = np.zeros((big.shape[0], big.shape[1], 4), np.uint8); layer[..., 0] = 62; layer[..., 1] = 46; layer[..., 2] = 38; layer[..., 3] = np.clip(sh * 125, 0, 255).astype(np.uint8)
    L = Image.fromarray(layer, "RGBA"); fr.base.alpha_composite(L, (int(feet[0] * S - fx), int(feet[1] * S - fy)))

def stand_rival(fr, cut, feet, poly=None, flip=False, shadow=True):
    """paste a rival (feet at game px), then put the planter back over its legs"""
    im = cut.copy(); bb = im.split()[3].getbbox(); im = im.crop(bb)
    if flip: im = im.transpose(Image.FLIP_LEFT_RIGHT)
    k = RIV_H * S / im.height; im = im.resize((int(im.width * k), int(im.height * k)), Image.LANCZOS)
    # soften the fringe
    a = im.split()[3].filter(ImageFilter.GaussianBlur(0.7)); im.putalpha(a)
    x = int(feet[0] * S - im.width / 2); y = int(feet[1] * S - im.height)
    keep = fr.base.copy() if poly else None
    if shadow: _shadow(fr, im, feet)
    fr.base.alpha_composite(im, (x, y))
    if poly:
        m = Image.new("L", fr.base.size, 0); ImageDraw.Draw(m).polygon([(px * S, py * S) for px, py in poly], fill=255)
        fr.base.paste(keep.convert("RGB"), (0, 0), m.filter(ImageFilter.GaussianBlur(0.8)))
    return (x, y, im.width, im.height)

def _clean_watcher(fr):
    """take the café watcher's dashed ring and disc out of the capture (they are the Scout's Watch mark, not part of a fight): inpaint the ring's cyan and the disc"""
    a = np.array(fr.base.convert("RGB")); k = S
    x0, y0, x1, y1 = [int(v * k) for v in (1000, 340, 1100, 415)]; sub = a[y0:y1, x0:x1].copy(); r, g, b = [sub[..., i].astype(int) for i in range(3)]
    m = ((r < 185) & (g > 150) & (b > 160) & (b - r > 18)).astype(np.uint8) * 255; m = cv2.dilate(m, np.ones((9, 9), np.uint8))
    cy, cx = int((379 - 340) * k), int((1040 - 1000) * k); yy, xx = np.ogrid[:sub.shape[0], :sub.shape[1]]; m[(yy - cy) ** 2 + (xx - cx) ** 2 < (17 * k) ** 2] = 255
    sub = cv2.inpaint(sub, m, 6, cv2.INPAINT_TELEA); a[y0:y1, x0:x1] = sub; fr.base = Image.fromarray(a).convert("RGBA")

def scene(blot_history=True):
    """the frame with the three rivals standing in it"""
    fr = Frame(SQSHOT)
    c = cutouts()
    stand_rival(fr, c["C"], RIVS["r1"][0])                                  # the blocker, east of the west pill planter, the cell on its other side
    stand_rival(fr, c["C"], RIVS["r2"][0], poly=PL_HEDGE, flip=True)        # behind the café hedge
    stand_rival(fr, c["A"], RIVS["r3"][0])                                  # in the open, firing
    return fr

if __name__ == "__main__":
    fr = scene(); im = fr.compose(); im.convert("RGB").crop((int(700 * S), int(300 * S), int(1560 * S), int(900 * S))).save(TMP + "scene_check.png"); print("ok")


def blank_frame(w_game, h_game, color=None):
    """a plain frame (for keys and diagrams on the board's own ground): the same layers and S as a capture, no street under it"""
    fr = Frame(SQSHOT); W, H = int(w_game * S), int(h_game * S)
    fr.base = Image.new("RGBA", (W, H), (color or BG) + (255,)); fr.world = Image.new("RGBA", (W, H), (0, 0, 0, 0)); fr.hud = Image.new("RGBA", (W, H), (0, 0, 0, 0)); return fr
