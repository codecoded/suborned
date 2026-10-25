import sys, json; sys.path.insert(0,'.')
from fit import *
ms = np.arange(0.6, 2.41, 0.05)
GROUPS = {'day': [PH['day']], 'evening': [PH['evening']], 'night': [PH['night']]}
res = {}
for k, hx in STATES.items():
    tgt = s2l(hx); h0, s0, _ = hsv(tgt)
    for ph, gs in GROUPS.items():
        best = (1e9,)
        for dh in np.arange(-8, 8.1, 1):
            for s in np.arange(0.45, 1.001, 0.025):
                tr = base((h0 + dh) % 360, s)
                for m in ms:
                    e = max(de2000(lab(g(tr * m)), lab(tgt)) for g in gs)
                    if e < best[0] - 1e-9: best = (e, dh, s, m)
        e, dh, s, m = best; tr = base((h0 + dh) % 360, s)
        # round to what the data will say: hex to 8 bit, level to 0.05
        hx8 = hexof(tr); tr8 = s2l(hx8); m = round(m / 0.05) * 0.05
        outs = [g(tr8 * m) for g in gs]
        e8 = max(de2000(lab(o), lab(tgt)) for o in outs)
        sens = max(max(de2000(lab(g(tr8 * m * f)), lab(tgt)) for g in gs) for f in (0.9, 1.1))
        res.setdefault(k, {})[ph] = dict(trim=hx8, level=round(m, 2), dE=round(e8, 2), dE_pm10=round(sens, 2), out=[hexof(o) for o in outs], peak=round(float((tr8*m).max()), 2))
        print('%-8s %-8s trim %s x%.2f  dE %.2f (+-10%% level: %.2f)  out %s  peak lin %.2f' % (k, ph, hx8, m, e8, sens, ','.join(hexof(o) for o in outs), (tr8*m).max()))
json.dump(res, open('final.json', 'w'), indent=1)
# state separation after the grade and against the cones
print()
for ph, gs in GROUPS.items():
    g = gs[0]; o = {k: g(s2l(res[k][ph]['trim']) * res[k][ph]['level']) for k in STATES}
    print(ph, 'walk-clearing %.0f clearing-stop %.0f walk-stop %.0f' % (de2000(lab(o['walk']),lab(o['clearing'])), de2000(lab(o['clearing']),lab(o['stop'])), de2000(lab(o['walk']),lab(o['stop']))),
          '| clearing vs guard #f0a020: %.1f' % de2000(lab(o['clearing']), lab(s2l('#f0a020'))), '| stop vs cone #e03a2f: %.1f' % de2000(lab(o['stop']), lab(s2l('#e03a2f'))),
          '| hue clearing %.0f guard %.0f' % (hsv(o['clearing'])[0], hsv(s2l('#f0a020'))[0]))
