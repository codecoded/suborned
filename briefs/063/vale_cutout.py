import cv2, numpy as np, sys
src, out, x0, x1 = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4])
im = cv2.imread(src)[:, x0:x1]
H, W = im.shape[:2]
lab = cv2.cvtColor(im, cv2.COLOR_BGR2LAB).astype(np.float32)
# background: per-row median of the outer 25 px both sides
bgcol = np.median(np.concatenate([lab[:, :25], lab[:, -25:]], axis=1), axis=1)  # H x 3
d = np.linalg.norm(lab - bgcol[:, None, :], axis=2)
fg = (d > 14).astype(np.uint8)
fg = cv2.morphologyEx(fg, cv2.MORPH_CLOSE, np.ones((5,5),np.uint8))
n, lbl, st, _ = cv2.connectedComponentsWithStats(fg)
big = 1 + np.argmax(st[1:, cv2.CC_STAT_AREA])
fg = (lbl == big).astype(np.uint8)
# fill holes
inv = 1 - fg; n, lbl, st, _ = cv2.connectedComponentsWithStats(inv)
for i in range(1, n):
    x, y, w, h, a = st[i]
    if x > 0 and y > 0 and x + w < W and y + h < H: fg[lbl == i] = 1
alpha = cv2.GaussianBlur(fg.astype(np.float32), (3,3), 0)
ys, xs = np.where(fg > 0); print('bbox', xs.min(), xs.max(), ys.min(), ys.max())
rgba = np.dstack([im, (alpha*255).astype(np.uint8)])
# crop to bbox with margin, place on 1024x2048 canvas, figure ~ 92% of height
x0b, x1b, y0b, y1b = xs.min(), xs.max(), ys.min(), ys.max()
fig = rgba[y0b:y1b+1, x0b:x1b+1]
CH, CW = 2048, 1024
s = (CH*0.94) / fig.shape[0]
fig = cv2.resize(fig, None, fx=s, fy=s, interpolation=cv2.INTER_LANCZOS4)
canvas = np.zeros((CH, CW, 4), np.uint8)
ox = (CW - fig.shape[1])//2; oy = (CH - fig.shape[0])//2
canvas[oy:oy+fig.shape[0], ox:ox+fig.shape[1]] = fig
canvas[..., :3][canvas[...,3]==0] = 0
cv2.imwrite(out, canvas); print(out, canvas.shape)
