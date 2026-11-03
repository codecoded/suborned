import sys; sys.path.insert(0,'.')
from fit import *
ms = np.arange(0.6, 2.41, 0.05)
def best_m(gs, tr, tgt):
    r = [(max(de2000(lab(g(tr*m)), lab(tgt)) for g in gs), m) for m in ms]; return min(r)
for k, hx in STATES.items():
    tgt = s2l(hx); h0, s0, _ = hsv(tgt); res = []
    for dh in np.arange(-8, 8.1, 1):
        for s in np.arange(0.45, 1.001, 0.025):
            tr = base((h0 + dh) % 360, s)
            tot = 0; det = []
            for gs in ([PH['day']], [PH['evening']], [PH['night'], PH['nightB']]):
                e, m = best_m(gs, tr, tgt); tot = max(tot, e); det.append((round(e,2), round(m,2)))
            res.append((tot, dh, s, det))
    res.sort(key=lambda r: r[0])
    for r in res[:3]:
        print(k, 'max dE %.2f hue %+d sat %.3f trim %s' % (r[0], r[1], r[2], hexof(base((h0+r[1])%360, r[2]))), 'day/evening/night (dE, m):', r[3])
