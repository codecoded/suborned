# Brief 024, "Revision: calm ring, clear default".
# Takes Sky's two screenshots (before-ring.png, before-hover.png, copied beside this file) and paints out the
# old plates, tags and icons so the new marks can be drawn on the same world. Retouch only: nothing here is game art.
#   python3 -I briefs/024/calm-ring/retouch.py     (needs opencv + numpy)
import cv2, numpy as np, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from joints import joints   # puts the tile joints back through a filled hole
here = os.path.dirname(os.path.abspath(__file__))
rng = np.random.default_rng(7)

def fill(im, rects, radius=8, feather=2.0):
    """Telea fill of rectangles, plus grain matched to the surround, feathered into the original."""
    H, W = im.shape[:2]
    g = cv2.cvtColor(im, cv2.COLOR_BGR2GRAY).astype(np.float32)
    hp = g - cv2.GaussianBlur(g, (0, 0), 2.0)
    out = im.copy()
    for rc in rects:
        if isinstance(rc, np.ndarray): m = rc
        else:
            x0, y0, x1, y1 = rc
            m = np.zeros((H, W), np.uint8); m[y0:y1, x0:x1] = 255
        r = cv2.inpaint(im, m, radius, cv2.INPAINT_TELEA)
        band = cv2.dilate(m, np.ones((61, 61), np.uint8)) & ~cv2.dilate(m, np.ones((13, 13), np.uint8))
        v = hp[band > 0]
        sd = 1.4826 * np.median(np.abs(v - np.median(v)))
        n = rng.normal(0, 1, (H, W)).astype(np.float32)
        n = cv2.GaussianBlur(n, (0, 0), 0.9); n = n / n.std() * sd
        rr = r.astype(np.float32) + n[:, :, None]
        a = cv2.GaussianBlur(cv2.dilate(m, np.ones((5, 5), np.uint8)).astype(np.float32) / 255, (0, 0), feather)
        a = np.clip(a * 1.5, 0, 1)[:, :, None]
        out = (out.astype(np.float32) * (1 - a) + rr * a)
        out = np.clip(out, 0, 255).astype(np.uint8)
        im = out
    return out

# ---- moment 1: the ring on the driver (2000 x 1070)
im = cv2.imread(os.path.join(here, "before-ring.png"))
H, W = im.shape[:2]
# History panel, first row: the old Knockout plate hid its first letters, icon and bar. Row 2 is the same row 85 px lower: copy it up.
im[402:456, 1292:1442] = im[487:541, 1292:1442].copy()
plates = [
    (594, 118, 854, 181),    # Take plate and its "driver" tag
    (880, 230, 1140, 276),   # Watch plate (the new plate covers it)
    (880, 402, 1290, 449),   # Knockout plate, world part
    (476, 514, 972, 560),    # Kill plate
    (58, 401, 572, 451),     # Fire plate
]
hole = np.zeros((H, W), np.uint8)
for x0, y0, x1, y1 in plates: hole[y0:y1, x0:x1] = 255
out = fill(im, plates)
# pavement only: the left half, away from the ring, the car and the panel
for x0, y0, x1, y1 in [(58, 401, 572, 451), (476, 514, 972, 560)]:
    sub = np.zeros_like(hole); sub[y0:y1, x0:x1] = 255
    out = joints(out, sub, (0, 330, 1000, 700), dark=5)
cv2.imwrite(os.path.join(here, "base-ring.png"), out)

# ---- moment 2: hover on the cabinet (1188 x 692)
im = cv2.imread(os.path.join(here, "before-hover.png"))
H, W = im.shape[:2]
# the "Cabinet" tag sat on the cabinet's two faces: stretch the row just under it back up
im[360:424, 278:433] = im[424:425, 278:433]
# the old ground ring is redrawn by the new marker: take its pale stroke (and soft edge) out first
white = (im[:, :, :].min(axis=2) > 190).astype(np.uint8) * 255
region = np.zeros((H, W), np.uint8); region[420:600, 300:540] = 255
ell = cv2.dilate(cv2.bitwise_and(white, region), np.ones((13, 13), np.uint8))
tagrest = np.zeros((H, W), np.uint8); tagrest[360:424, 433:440] = 255
hole = ell.copy()
for x0, y0, x1, y1 in [(466, 327, 1064, 460),   # the plate and its tail
                       (312, 494, 540, 602)]:   # the three small icons on the ring
    hole[y0:y1, x0:x1] = 255
hole = cv2.bitwise_or(hole, tagrest)
rects = [hole]
out = fill(im, rects, radius=7)
out = joints(out, hole, (440, 300, 1188, 692), dark=9)
cv2.imwrite(os.path.join(here, "base-hover.png"), out)
print("ok")
