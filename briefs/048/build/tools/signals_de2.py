# ΔE2000 haze on vs off over the fan pixels really seen (as built differs from the frame with no fan), split into the rim (bright in the mask:
# the opaque outline) and the fill (translucent); and the same for the overlay variant (where it differs from the no-fan frame).
import sys, json, numpy as np
from PIL import Image
sys.path.insert(0, sys.argv[2]); from signals_de import lab, de2000  # noqa
d = sys.argv[1]
I = lambda n: np.asarray(Image.open(f'{d}/{n}.png').convert('RGB')).astype(int)
out = {}
for dist in (150, 250):
    mk = I(f'signals-{dist}-mask'); lum = mk.sum(-1); m = lum > 72; bare = I(f'signals-{dist}-bare-nohaze')
    rim = m & (lum >= 0.75 * lum[m].max()) if m.any() else m; fill = m & ~rim
    row = {'maskPixels': int(m.sum())}
    for v in ('asbuilt', 'overlay'):
        nh = I(f'signals-{dist}-{v}-nohaze'); hz = I(f'signals-{dist}-{v}-haze'); seen = m & (np.abs(nh - bare).sum(-1) > 18)
        r = {'seenPixels': int(seen.sum())}
        for part, pm in (('rim', rim & seen), ('fill', fill & seen)):
            if pm.sum() < 20: r[part] = None; continue
            a = lab(Image.fromarray(hz.astype(np.uint8))); b = lab(Image.fromarray(nh.astype(np.uint8)))
            r[part] = {'pixels': int(pm.sum()), 'meanColourDeltaE2000': round(float(de2000(a[pm].mean(0), b[pm].mean(0))), 2), 'pixelMedian': round(float(np.median(de2000(a[pm], b[pm]))), 2)}
        row[v] = r
    out[dist] = row
print(json.dumps(out, indent=1))
