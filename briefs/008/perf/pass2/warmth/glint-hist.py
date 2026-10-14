import sys, numpy as np
from PIL import Image
d = sys.argv[1]
E = np.asarray(Image.open(d + "/editor-home.png").convert("RGB"))
P = np.asarray(Image.open(d + "/player-home.png").convert("RGB"))
for name, A in (("Editor", E), ("Player", P)):
    print("==", name)
    for ch, cn in enumerate("RGB"):
        h = np.bincount(A[..., ch].ravel(), minlength=256)
        top = np.argsort(h)[::-1][:6]
        print(f" {cn}: top values", [(int(v), round(h[v]/A[...,ch].size*100, 1)) for v in top], " max", A[..., ch].max())
# pixels at the Editor plateau: what do G,B look like and what does the player do there
m = (E[..., 0] >= 228) & (E[..., 0] <= 230)
print("\nEditor R in 228-230:", round(m.mean()*100, 1), "% of frame")
print(" Editor G,B mean/std in plateau", E[m][:,1].mean().round(1), E[m][:,1].std().round(1), E[m][:,2].mean().round(1), E[m][:,2].std().round(1))
print(" Player R in the same pixels: pct 5/25/50/75/95", np.percentile(P[m][:,0], [5,25,50,75,95]))
print(" Player G,B", P[m][:,1].mean().round(1), P[m][:,2].mean().round(1))
# in the plateau, how does player R relate to editor G (a tile-brightness proxy)?
g = E[m][:,1].astype(float); r = P[m][:,0].astype(float)
print(" corr(Player R, Editor G) in plateau:", np.corrcoef(g, r)[0,1].round(3))
for lo, hi in ((180,195),(195,200),(200,205),(205,210),(210,216)):
    s = (g >= lo) & (g < hi)
    if s.sum() > 1000: print(f"  Editor G {lo}-{hi}: n={s.sum():8d} Player R mean {r[s].mean():.1f}  Player G {P[m][s][:,1].mean():.1f}  Player B {P[m][s][:,2].mean():.1f}  Editor B {E[m][s][:,2].mean():.1f}")
