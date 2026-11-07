"""The end card lifted from the real getaway frame: the road copied two dash periods along the road, the kerb and pavement one or more steps
back along the kerb (both shifts run parallel to the kerb, so road lands on road and kerb on kerb), tone-matched and feathered. Glyph, brief 099."""
from PIL import Image
import numpy as np, cv2, sys
src, out = sys.argv[1], sys.argv[2]
a = np.array(Image.open(src + '.png').convert('RGB')).astype(np.float32)
m = (np.array(Image.open(src + '.hud.png').convert('RGBA'))[:, :, 3] > 12).astype(np.uint8) * 255
md = cv2.dilate(m, np.ones((31, 31), np.uint8)) > 0
card = np.zeros_like(md); card[110:400, 700:1210] = md[110:400, 700:1210]
H, W = md.shape; yy, xx = np.mgrid[0:H, 0:W]
kerb = (xx - 600) * 1.17
road = card & (yy < kerb - 12); side = card & ~road
o = a.copy()
ys, xs = np.where(road); sy, sx = np.clip(ys + 346, 0, H - 1), np.clip(xs + 326, 0, W - 1); o[ys, xs] = a[sy, sx]
ring = cv2.dilate(road.astype(np.uint8), np.ones((15, 15), np.uint8)).astype(bool) & ~md & (yy < kerb - 12)
o[road] += a[ring].mean(0) - o[road].mean(0)
ys, xs = np.where(side)
for y, x in zip(ys, xs):
    for k in range(1, 6):
        y2, x2 = y - 158 * k, x - 135 * k
        if y2 < 0 or x2 < 0: break
        if not md[y2, x2]: o[y, x] = a[y2, x2]; break
rest = (md & ~card).astype(np.uint8) * 255
bgr = cv2.inpaint(np.clip(o, 0, 255).astype(np.uint8)[:, :, ::-1].copy(), rest, 14, cv2.INPAINT_TELEA)[:, :, ::-1].astype(np.float32)
f = cv2.GaussianBlur(card.astype(np.float32), (0, 0), 2.5)[..., None]
res = np.where(card[..., None], bgr, a) * 0 + (bgr * f + a * (1 - f)) * card[..., None] + bgr * (~card[..., None])
Image.fromarray(np.clip(res, 0, 255).astype(np.uint8)).save(out)
