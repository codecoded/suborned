"""Glint, brief 061 (2026-10-26): refit the crossing heads' trim to the batch's measured pixels (0.2.122), with the grade model calibrated on them.
Input: the measured face-colour of each state, per phase, from Latch's 2160 frames (h1-measured-0.2.122.json for day; the same rect on the h2
evening and night frames for the other two, measured with the same rule: brightest pixel for walk, vividest for clearing and stop).
Why a calibration: grade.py has no bloom, and the day frames showed every state paler than the model said (walk +5.6 dE, stop +4.2, clearing +2.6;
night within 1.7). The cause is not one known term (the rim material is black albedo, so the sun cannot be it; the likely one is the day bloom's veil from
the sunlit paving), so three stand-ins are fitted per phase and the refit must hold under all three (minimax), so it does not depend on which is true:
  gain    effective = trim x level x G          (a multiplier on what the head emits)
  offset  effective = trim x (level + A)        (an additive wash of the state's own colour: the halo and beam laid over the face)
  veil    effective = trim x level + W x tint   (an additive warm-white wash: bloom of the lit surroundings; tint = light.bloomTint #fff0d6)
Run from this folder: python3 -I refit.py   (writes refit.json)."""
import sys, json; sys.path.insert(0, '.')
from fit import *
TINT = s2l('#fff0d6')
TARGET = STATES
# as shipped in signals.json before this refit: (colour, level); measured = what the 2160 frame shows
SHIPPED = {
 'day':     {'walk': ('#53FFB3', 1.30), 'clearing': ('#FF8C00', 1.90), 'stop': ('#FF5A59', 1.15)},
 'evening': {'walk': ('#39FFC6', 1.15), 'clearing': ('#FF8B06', 1.90), 'stop': ('#FF6766', 0.90)},
 'night':   {'walk': ('#00FFA9', 1.15), 'clearing': ('#FF8800', 1.75), 'stop': ('#FF4B46', 1.20)},
}
MEASURED = {
 'day':     {'walk': '#88ECC8', 'clearing': '#FDBA00', 'stop': '#FB5743'},
 'evening': {'walk': '#7CEAC6', 'clearing': '#FDBA00', 'stop': '#F45543'},
 'night':   {'walk': '#67EAB3', 'clearing': '#FDB600', 'stop': '#F54930'},
}
MODELS = {
 'gain':   lambda x, lvl, p: x * lvl * p,
 'offset': lambda x, lvl, p: x * (lvl + p),
 'veil':   lambda x, lvl, p: x * lvl + p * TINT,
}
P0 = {'gain': 1.0, 'offset': 0.0, 'veil': 0.0}
def nm1(f, x0, step, it=120):
    a, b = x0, x0 + step; fa, fb = f(a), f(b)
    for _ in range(it):
        if fb < fa: a, b, fa, fb = b, a, fb, fa
        c = a + (a - b) * 1.5; fc = f(c)
        if fc < fa: b, fb = a, fa; a, fa = c, fc
        else: step *= 0.6; b = a + step; fb = f(b)
    return a, fa
def calibrate():
    cal = {}
    for ph, st in SHIPPED.items():
        g = PH[ph]; cal[ph] = {}
        for name, fn in MODELS.items():
            def cost(p): return max(de2000(lab(g(np.maximum(fn(s2l(c), m, p), 0))), lab(s2l(MEASURED[ph][k]))) for k, (c, m) in st.items())
            p, f = nm1(cost, P0[name], 0.1); cal[ph][name] = (float(p), float(f))
    return cal
def refit(cal, ph, k, m_lo=0.4, m_hi=2.6):
    g = PH[ph]; tgt = s2l(TARGET[k]); h0, s0, _ = hsv(tgt); best = []
    for dh in np.arange(-8, 8.1, 2):
        for s in np.arange(0.45, 1.001, 0.05):
            tr = s2l(hexof(base((h0 + dh) % 360, s)))
            for m in np.arange(m_lo, m_hi + 1e-9, 0.05):
                e = [de2000(lab(g(np.maximum(fn(tr, m, cal[ph][n][0]), 0))), lab(tgt)) for n, fn in MODELS.items()]
                best.append((max(e), dh, s, m, tr))
    best.sort(key=lambda r: r[0]); top = [r for r in best if r[0] < best[0][0] + 0.25][:60]
    # among the near-best, the one least sensitive to the level being 10 percent off in either direction
    def sens(r):
        _, _, _, m, tr = r
        return max(max(de2000(lab(g(np.maximum(fn(tr, m * f, cal[ph][n][0]), 0))), lab(tgt)) for n, fn in MODELS.items()) for f in (0.9, 1.1))
    r = min(top, key=sens); e, dh, s, m, tr = r
    return hexof(tr), round(float(m), 2), {n: round(de2000(lab(g(np.maximum(fn(tr, m, cal[ph][n][0]), 0))), lab(tgt)), 2) for n, fn in MODELS.items()}, round(sens(r), 2), \
        {n: hexof(g(np.maximum(fn(tr, m, cal[ph][n][0]), 0))) for n, fn in MODELS.items()}
if __name__ == '__main__':
    cal = calibrate(); out = {'calibration': {}, 'refit': {}}
    print('calibration (the model stand-in fitted to the measured frames; worst-state dE of the stand-in against the measurement):')
    for ph in cal:
        out['calibration'][ph] = {n: {'p': round(p, 3), 'worstDE': round(f, 2)} for n, (p, f) in cal[ph].items()}
        print('  %-8s' % ph, '  '.join('%s %s -> %.2f' % (n, ('x%.3f' % p) if n == 'gain' else ('+%.3f level' % p) if n == 'offset' else ('+%.3f white' % p), f) for n, (p, f) in cal[ph].items()))
    print('as shipped: the measured colour against the board colour')
    for ph in SHIPPED:
        print('  %-8s' % ph, '  '.join('%s %.1f' % (k, de2000(lab(s2l(MEASURED[ph][k])), lab(s2l(TARGET[k])))) for k in TARGET))
    print('refit (trim, level; worst dE over the three stand-ins; +-10% level; the colour each stand-in predicts):')
    for ph in ('day', 'evening', 'night'):
        out['refit'][ph] = {}
        for k in TARGET:
            tr, m, es, sn, outs = refit(cal, ph, k)
            out['refit'][ph][k] = {'colour': tr, 'level': m, 'dE': es, 'dE_pm10': sn, 'shows': outs}
            print('  %-8s %-8s %s x%.2f  dE %s  worst %.2f  (+-10%%: %.2f)  shows %s' % (ph, k, tr, m, es, max(es.values()), sn, outs))
    json.dump(out, open('refit.json', 'w'), indent=1)
