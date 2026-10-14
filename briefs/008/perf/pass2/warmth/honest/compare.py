# Glint's done-check for honest Editor frames (glint-ruling.md): new Editor vs player on Latch's compare.txt regions.
# Pass: every region within 2 levels per channel; sun paving R median >= 235; sun paving R-B within 2 of the player.
# Usage: python3 -I compare.py <dir with editor-old-home.png, editor-new-home.png, player-home.png>
import sys, numpy as np
from PIL import Image
d = sys.argv[1]
L = lambda n: np.asarray(Image.open(f"{d}/{n}").convert("RGB")).astype(float)
O, N, P = L("editor-old-home.png"), L("editor-new-home.png"), L("player-home.png")
regions = [("sun: square paving", (500, 560, 800, 700)), ("sun: cafe front", (300, 150, 700, 330)),
           ("shade: east block shadow on paving", (1500, 60, 1750, 260)), ("shade: east block face", (1800, 500, 1900, 1000)),
           ("whole frame", (0, 0, 1920, 1080))]
f = lambda v: "[" + ", ".join(f"{x:.1f}" for x in v) + "]"
ok = True
print("region (x0,y0)-(x1,y1) | old Editor mean RGB | new Editor mean RGB | player mean RGB | new - player | R-B old / new / player")
for name, (x0, y0, x1, y1) in regions:
    o, n, p = (A[y0:y1, x0:x1].reshape(-1, 3).mean(0) for A in (O, N, P))
    dlt = n - p
    within = bool(np.all(np.abs(dlt) <= 2.0))
    ok &= within
    print(f"{name} ({x0},{y0})-({x1},{y1}) | {f(o)} | {f(n)} | {f(p)} | {f(dlt)} {'ok' if within else 'OVER 2'} | {o[0]-o[2]:.1f} / {n[0]-n[2]:.1f} / {p[0]-p[2]:.1f}")
# "sun paving" for the median and R-B checks = Glint's plateau: the pixels the old Editor path clipped to R 228-230
m = (O[..., 0] >= 228) & (O[..., 0] <= 230)
med = lambda A: np.median(A[m][:, 0])
rb = lambda A: (A[m][:, 0] - A[m][:, 2]).mean()
c2 = med(N) >= 235; c3 = abs(rb(N) - rb(P)) <= 2
print(f"sun paving = the old Editor's R 228-230 plateau, {m.mean()*100:.1f} % of the frame")
print(f"sun paving R median: old {med(O):.0f}, new {med(N):.0f}, player {med(P):.0f} -> {'ok' if c2 else 'UNDER 235'} (>= 235)")
print(f"sun paving R-B: old {rb(O):.1f}, new {rb(N):.1f}, player {rb(P):.1f} -> new - player {rb(N)-rb(P):+.1f} {'ok' if c3 else 'OVER 2'}")
print(f"per-pixel mean |new - player| (R, G, B): {f(np.abs(N - P).reshape(-1, 3).mean(0))}; |old - player|: {f(np.abs(O - P).reshape(-1, 3).mean(0))}")
for name, A in (("old", O), ("new", N), ("player", P)):
    print(f"{name}: R >= 240 {np.mean(A[...,0] >= 240)*100:.1f} %, R >= 246 {np.mean(A[...,0] >= 246)*100:.1f} %, max R {A[...,0].max():.0f}")
print("PASS" if ok and c2 and c3 else "FAIL")
