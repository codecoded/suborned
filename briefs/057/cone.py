# Board for the quiet cone (brief 057, section 3). Run: python3 -I briefs/057/cone.py
# Sky's 0.2.99 shot of the Office forecourt: the camera's fan is recovered from the picture (colour mask, the fill's 38 % red taken back off),
# then redrawn the way the spec says: rest = a rim and a low fill that fades with distance; strong = the fill at 0.34 fading to 0.14, the rim full.
import sys, os, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import Image, ImageDraw, ImageFont
import numpy as np, cv2
R = "/Users/sky/Projects/suborned/"; OUT = R + "briefs/057/"; SC = R + "out/scratch/glyph/057/"
SHOT3 = "/Users/sky/.t3/userdata/attachments/6f2fb9e2-3d6f-4cb6-bf62-66a9d065638d-7e1a9ffa-2f36-430a-a32d-feffbd382f65.png"
RED = np.array([224, 58, 47], float)          # style contract signal_colours.cones, as HandoverHost.coneColour
BG = (23, 48, 47); INK = (247, 244, 236); DIM = (176, 205, 199)
APEX = (205, 722)
F = ImageFont.truetype(R + "testbed/hud-style/fonts/Jost-Medium-Tab.ttf", 26)
def fnt(n): return ImageFont.truetype(R + "testbed/hud-style/fonts/Jost-Medium-Tab.ttf", n)

im = Image.open(SHOT3).convert("RGB"); a = np.array(im).astype(float); hsv = cv2.cvtColor(np.array(im), cv2.COLOR_RGB2HSV).astype(int)
h, s, v = hsv[..., 0], hsv[..., 1], hsv[..., 2]
m = ((s > 104) & (h <= 15) & (v > 150)).astype("uint8") * 255
m = cv2.morphologyEx(m, cv2.MORPH_CLOSE, np.ones((9, 9), np.uint8)); m = cv2.morphologyEx(m, cv2.MORPH_OPEN, np.ones((5, 5), np.uint8))
n, lab, st, _ = cv2.connectedComponentsWithStats(m); big = 1 + np.argmax(st[1:, cv2.CC_STAT_AREA]); M0 = (lab == big).astype("uint8") * 255
# the clean outline: close the notches the trees and bins cut, then a simplified polygon (the fan is a convex-ish wedge)
Mc = cv2.morphologyEx(M0, cv2.MORPH_CLOSE, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (81, 81)))
cnts, _ = cv2.findContours(Mc, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_NONE); cnt0 = max(cnts, key=cv2.contourArea)
cnt = cv2.approxPolyDP(cnt0, 9, True)
outline = np.zeros_like(M0); cv2.drawContours(outline, [cnt], -1, 255, -1)
M = cv2.dilate(M0, np.ones((7, 7), np.uint8)); M[outline == 0] = 0       # where the fill really shows (holes = things standing on it)
# the old rim (saturated dark red) in a band round the outline, painted out from its neighbours
rim = ((s > 170) & (h <= 10) & (v > 110)).astype("uint8") * 255
edge = np.zeros_like(M0); cv2.drawContours(edge, [cnt], -1, 255, 25)
rim[edge == 0] = 0; rim = cv2.dilate(rim, np.ones((9, 9), np.uint8))
A0 = 0.38
# colour of the paving without the fan: the mean of the sunlit paving beside it, against the mean inside the fan (a channel gain; the fill is not a plain 38 % blend after the grade)
inner = cv2.erode(M, np.ones((25, 25), np.uint8)) > 0
side = (cv2.dilate(outline, np.ones((161, 161), np.uint8)) > 0) & (outline == 0) & (s > 55) & (s < 100) & (h >= 14) & (h <= 19) & (v > 170)
gain = np.array([a[..., c][side].mean() / a[..., c][inner].mean() for c in range(3)])
print("gain", gain.round(3), inner.sum(), side.sum())
base = np.clip(a * gain, 0, 255)
b8 = cv2.inpaint(base.astype("uint8"), rim, 6, cv2.INPAINT_TELEA); base = np.where((rim > 0)[..., None], b8.astype(float), base)
base[M == 0] = np.where((rim > 0)[M == 0][:, None], b8.astype(float)[M == 0], a[M == 0])
ys, xs = np.nonzero(M); d = np.hypot(xs - APEX[0], ys - APEX[1]); dmax = d.max()
D = np.hypot(*np.meshgrid(np.arange(a.shape[1]) - APEX[0], np.arange(a.shape[0]) - APEX[1]))
def render(alpha_hub, alpha_far, rim_w, rim_a, dash_far=True):
    al = alpha_hub + (alpha_far - alpha_hub) * np.clip(D / dmax, 0, 1)
    al = al * (M > 0)
    out = base * (1 - al[..., None]) + RED * al[..., None]
    out[(M == 0) & (rim == 0)] = a[(M == 0) & (rim == 0)]
    # the 'rim pixels' of the old fan outside M (a few px) stay painted out inside outline
    ov = Image.new("RGBA", (a.shape[1], a.shape[0]), (0, 0, 0, 0)); Q = 3
    big_ = Image.new("RGBA", (ov.width * Q, ov.height * Q), (0, 0, 0, 0)); dr = ImageDraw.Draw(big_)
    pts = []
    for i in range(len(cnt)):
        p0, p1 = cnt[i][0], cnt[(i + 1) % len(cnt)][0]
        for t in np.linspace(0, 1, max(2, int(math.hypot(*(p1 - p0)) / 25))): pts.append((int((p0[0] + (p1[0] - p0[0]) * t) * Q), int((p0[1] + (p1[1] - p0[1]) * t) * Q)))
    # arc length along the outline; the far edge = points farther than 0.92 of the reach from the lens: dashed 0.4 m on, 0.3 m off (1 m = 38.6 * 2.98 / ... px: 3 px per game px, 38.6 game px per m * 0.616 on screen; use 100 px per m along the edge)
    far = [math.hypot(p[0] / Q - APEX[0], p[1] / Q - APEX[1]) > 0.9 * dmax for p in pts]
    run = 0.0
    for i in range(len(pts)):
        p0, p1 = pts[i], pts[(i + 1) % len(pts)]; L = math.hypot(p1[0] - p0[0], p1[1] - p0[1]) / Q
        if far[i] and dash_far:
            on = (run % 70.0) < 40.0
            if on: dr.line((p0, p1), fill=(224, 58, 47, int(255 * rim_a)), width=int(rim_w * Q))
        else:
            dr.line((p0, p1), fill=(224, 58, 47, int(255 * rim_a)), width=int(rim_w * Q))
        run += L
    ov = big_.resize(ov.size, Image.LANCZOS)
    res = Image.fromarray(out.astype("uint8")).convert("RGBA"); res.alpha_composite(ov); return res.convert("RGB")
before = im
quiet = render(0.13, 0.01, 3.4, 0.55)
strong = render(0.34, 0.14, 6.0, 1.0)
def cap(img, title, sub, w=1000):
    sc = w / img.width; r = img.resize((w, int(img.height * sc)), Image.LANCZOS); c = Image.new("RGB", (w, r.height + 80), BG)
    d = ImageDraw.Draw(c); d.text((0, 0), title, font=fnt(28), fill=INK); d.text((0, 38), sub, font=fnt(19), fill=DIM); c.paste(r, (0, 80)); return c
panels = [cap(before, "Today, 0.2.99 (Sky's shot)", "a watched camera's fan: flat 38 % fill, 6 px rim, permanent"),
          cap(quiet, "Rest: rim and a fading fill", "fill 13 % at the lens to 1 % at the far edge, rim 3.4 px at 55 %, far edge dashed"),
          cap(strong, "Strong: when it matters", "fill 34 % to 14 %, rim 6 px at 100 %: hovered, someone in or about to enter, a path crossing, an alert")]
W = sum(p.width for p in panels) + 30 * 4; H = max(p.height for p in panels) + 110
out = Image.new("RGB", (W, H), BG); d = ImageDraw.Draw(out)
d.text((30, 20), "057 - quieter watched cones: before and after", font=fnt(36), fill=INK)
d.text((30, 66), "Sky's shot recovered under the fan (the 38 % red taken back off), then redrawn at the new strengths. Red only; no new colour. Trees, bins, people and plates still stand on top.", font=fnt(20), fill=DIM)
x = 30
for p in panels: out.paste(p, (x, 110)); x += p.width + 30
out.save(OUT + "board-06-quiet-cone.png"); print("board-06", out.size)
# a 2x detail: the lens end and the dashed far edge, at rest, for the clipping and legibility check
def det(img, box, title):
    c = img.crop(box).resize(((box[2] - box[0]) * 2 // 2, (box[3] - box[1]) * 2 // 2), Image.LANCZOS); return c
dets = [det(quiet, (100, 600, 800, 1100), ""), det(quiet, (900, 1500, 1749, 2000), "")]
dd = Image.new("RGB", (sum(x.width for x in dets) + 90, max(x.height for x in dets) + 110), BG); q = ImageDraw.Draw(dd)
q.text((30, 20), "Rest strength, native crops: the lens end (left) and the dashed far edge (right)", font=fnt(28), fill=INK)
xx = 30
for x_ in dets: dd.paste(x_, (xx, 80)); xx += x_.width + 30
dd.save(OUT + "board-07-quiet-cone-detail.png"); print("board-07", dd.size)
