import sys, json, itertools, colorsys; sys.path.insert(0,'.')
from grade import *
import numpy as np
PH = {
 'day':      Grade(exposure=0.15, contrast=6,  saturation=36.9, shadows='#c7d1eb'),
 'evening':  Grade(exposure=0.30, contrast=10, saturation=38.9, filt='#fffaf4', shadows='#e4e4f6', highs='#fff8f0', temp=3, tint=1),
 'night':    Grade(exposure=0.20, contrast=8,  saturation=14.0, shadows='#6fa3a8'),   # night volume as light.json builds it
 'nightB':   Grade(exposure=0.20, contrast=8,  saturation=32.3, shadows='#6fa3a8'),   # if gradePlay's x1.16 saturation reaches the night volume too
}
STATES = {'walk': '#55E6B0', 'clearing': '#FFB624', 'stop': '#F2432E'}
def base(hue, sat):  # sRGB colour of value 1, as linear
    r, g, b = colorsys.hsv_to_rgb(hue / 360, sat, 1.0); return s2l('#%02x%02x%02x' % (round(r*255), round(g*255), round(b*255)))
def out(g, hue, sat, m): return g(base(hue, sat) * m)
def cost(g, tgt, hue, sat, m): return de2000(lab(out(g, hue, sat, m)), lab(tgt))
def fit(g, tgt, m_lo, m_hi, h0, s0):
    best = (1e9,)
    for m in np.arange(m_lo, m_hi + 1e-9, 0.1):
        for dh in np.arange(-24, 25, 3):
            for s in np.arange(max(0.3, s0 - 0.45), 1.0001, 0.05):
                c = cost(g, tgt, (h0 + dh) % 360, s, m)
                if c < best[0]: best = (c, (h0 + dh) % 360, s, m)
    # refine
    c, h, s, m = best; step = [2.0, 0.03, 0.05]
    for _ in range(40):
        imp = False
        for i, d in enumerate(step):
            for sg in (-1, 1):
                p = [h, s, m]; p[i] += sg * d; p[1] = min(max(p[1], 0.2), 1.0); p[2] = max(p[2], 0.1)
                cc = cost(g, tgt, p[0] % 360, p[1], p[2])
                if cc < c - 1e-6: c, h, s, m = cc, p[0] % 360, p[1], p[2]; imp = True
        if not imp: step = [d / 2 for d in step]
    return c, h, s, m
if __name__ == '__main__':
    lo = float(sys.argv[1]) if len(sys.argv) > 1 else 0.5
    hi = float(sys.argv[2]) if len(sys.argv) > 2 else 3.0
    for ph, g in PH.items():
        print(ph)
        for k, hx in STATES.items():
            tgt = s2l(hx); h0, s0, _ = hsv(tgt); c, h, s, m = fit(g, tgt, lo, hi, h0, s0)
            b = base(h, s); o = out(g, h, s, m)
            print('  %-8s dE %.2f  trim hue %.0f sat %.2f x%.2f  hex %s  lin max %.2f -> out %s  (untrimmed x2.0: %s)' % (k, c, h, s, m, hexof(b), (b*m).max(), hexof(o), hexof(g(s2l(hx)*2.0))))
