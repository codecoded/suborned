import sys; sys.path.insert(0,'.')
from fit import *
def solve(g, tgt_hex, start=None, cap=None, iters=300):
    t = s2l(tgt_hex); x = (start if start is not None else t.copy()).astype(float)
    def f(x): return de2000(lab(g(np.maximum(x, 1e-5))), lab(t)) + (0 if cap is None else 5*max(0, x.max()-cap))
    best = f(x); step = 0.5
    for _ in range(iters):
        imp = False
        for i in range(3):
            for sg in (1, -1):
                y = x.copy(); y[i] = max(1e-5, y[i] * (1 + sg * step)) if x[i] > 1e-4 else y[i] + sg * step * 0.01
                y = np.maximum(y, 0.0); v = f(y)
                if v < best - 1e-6: best, x, imp = v, y, True
        if not imp: step *= 0.6
        if step < 1e-3: break
    return best, x
if __name__ == '__main__':
    targets = {'fill': '#17302f'}
    rims = {'day': '#eee8da', 'evening': '#f0e2cf', 'night': '#d7d9d0'}
    for ph, g in PH.items():
        e, x = solve(g, '#17302f'); print('fill', ph, 'dE %.2f' % e, 'pre', hexof(x), 'lin', x.round(4), '-> out', hexof(g(x)))
    for ph, g in PH.items():
        k = 'night' if ph.startswith('night') else ph
        e, x = solve(g, rims[k], start=s2l(rims[k])*1.5); print('rim', ph, rims[k], 'dE %.2f' % e, 'pre', hexof(x), 'lin', x.round(3), 'max %.2f' % x.max(), '-> out', hexof(g(x)))
