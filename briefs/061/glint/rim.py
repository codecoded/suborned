import sys; sys.path.insert(0,'.')
from sil import *
cream = s2l('#f7f4ec')
W = {  # displayed wall tone (sRGB hex) -> the pre-grade colour that displays as it
 'day lit stone':   ('day',   '#ecdac5'), 'day shade stone': ('day', '#a99c91'), 'day lit seafoam': ('day', '#8fcfc0'),
 'day shade teal enamel': ('day', '#1f4a50'), 'night wall (ambient)': ('night', '#0c1a1f'), 'night wall in a lamp pool': ('night', '#7a6a55'),
}
lum = lambda c: float(c @ np.array([0.2126, 0.7152, 0.0722]))
def L(o): return lab(o)[0]
print('%-28s %6s | %-22s | %-22s | %-22s' % ('wall (displayed)', 'L*', 'now: lerp to cream .33', 'relative .33 (k 2.5)', 'relative .45 (k 2.5)'))
for nm, (ph, hx) in W.items():
    g = PH[ph]; e, pre = solve(g, hx, iters=200); d0 = g(pre); l0 = L(d0)
    cols = []
    for lean, rel, k in ((0.33, False, 0), (0.33, True, 2.5), (0.45, True, 2.5)):
        tgt = cream * (min(1.0, lum(pre) * k) if rel else 1.0)
        o = g(pre + (tgt - pre) * lean); cols.append('%s dL* %+.1f' % (hexof(o), L(o) - l0))
    print('%-28s %6.1f | %-22s | %-22s | %-22s' % (nm + ' ' + hexof(d0), l0, *cols))
