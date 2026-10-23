# Glint's pass: ΔE2000 haze on vs off <= 1 for the rim and the fill, over the fan pixels really seen (as built differs from the no-fan frame);
# and the 250 m control: the share of the mask seen as built, with the depth offset, and with ZTest Always.
import sys, json, numpy as np
from PIL import Image
sys.path.insert(0, sys.argv[2]); from signals_de import lab, de2000  # noqa
d = sys.argv[1]
I = lambda n: np.asarray(Image.open(f'{d}/{n}.png').convert('RGB')).astype(int)
out = {}
for dist in (150, 250):
    mk = I(f'signals-{dist}-mask'); lum = mk.sum(-1); m = lum > 72; bare = I(f'signals-{dist}-bare-nohaze')
    rim = m & (lum >= 0.75 * lum[m].max()) if m.any() else m; fill = m & ~rim
    seen = lambda img: m & (np.abs(img - bare).sum(-1) > 18)
    nh = I(f'signals-{dist}-asbuilt-nohaze'); hz = I(f'signals-{dist}-asbuilt-haze'); s = seen(nh)
    row = {'maskPixels': int(m.sum()), 'seenShare': {'asbuilt': round(float(s.sum() / max(1, m.sum())), 3),
           'offset': round(float(seen(I(f'signals-{dist}-control-offset')).sum() / max(1, m.sum())), 3),
           'ztestAlways': round(float(seen(I(f'signals-{dist}-control-ztest-always')).sum() / max(1, m.sum())), 3)}}
    a = lab(Image.fromarray(hz.astype(np.uint8))); b = lab(Image.fromarray(nh.astype(np.uint8)))
    for part, pm in (('rim', rim & s), ('fill', fill & s)):
        if pm.sum() < 20: row[part] = None; continue
        mean = float(de2000(a[pm].mean(0), b[pm].mean(0)))
        row[part] = {'pixels': int(pm.sum()), 'meanColourDeltaE2000': round(mean, 2), 'pixelMedian': round(float(np.median(de2000(a[pm], b[pm]))), 2), 'pass': mean <= 1}
    out[dist] = row
print(json.dumps(out, indent=1))
