"""What grade.py can be checked against without a frame. (1) tools/decokit/deltae.py, the repo's dE2000: identical. (2) light.json's post 'honest'
note records an unlit swatch through the grade as 7.2 / 5.0 / 3.3 dE for cones / guard / rivals; that was measured under the NEUTRAL tonemapper
(ACES came in with 026 phase 1, 2026-10-19), so only the guard (5.0 against 4.8 here) is a fair comparison; the cones (10.0 here) and rivals (10.5)
are not. grade.py is Unity's shader code transcribed (LutBuilderHdr ColorGrade, Color.hlsl AcesTonemap); it is NOT yet calibrated against a captured
pixel. The batch's first signal frame is the calibration: sample the face centre, compare with `shows` in signals.json, refit with fit.py if off by
more than 6 dE. Run from this folder: python3 -I validate.py"""
import sys; sys.path.insert(0, '.'); sys.path.insert(0, '../../../tools/decokit')
import deltae
from fit import *
g = Grade(exposure=0.15, contrast=6, saturation=18, shadows='#c7d1eb')   # the grade of that measurement (play saturation x1.16 came later)
for n, h in (('cones', '#e03a2f'), ('guard', '#f0a020'), ('rivals', '#8a4fd0')):
    o = g(s2l(h)); print('%-6s %s through the grade -> %s  dE00 %.1f (Neutral-era record: cones 7.2, guard 5.0, rivals 3.3: only the guard is comparable)' % (n, h, hexof(o), de2000(lab(o), lab(s2l(h)))))
for a, b in (('#e03a2f', '#c30008'), ('#f0a020', '#db9900'), ('#FFB624', '#f0a020')):
    print('dE00 %s %s: here %.2f  deltae.py %.2f' % (a, b, de2000(lab(s2l(a)), lab(s2l(b))), deltae.de(a, b)))
# (3) a surface the record does know: light.json r026 (ACES): "lit paving about 235, shade paving about 160" (R of the sRGB); the paintings' lit paving (237, 220, 201).
g2 = Grade(exposure=0.15, contrast=6, saturation=36.9, shadows='#c7d1eb', lift=(0.04, 0.975, 0.90, 0.87))
sun = 1.8 * s2l('#ffe0d0'); up = 0.69 * s2l('#dedce0'); pav = s2l('#e4dacb')
print('civic paving (albedo #e4dacb), sun on flat ground: %s (record: R about 235, the paintings 237,220,201)' % hexof(g2(pav * (sun * 0.62 + up))))
# (4) the 0.2.122 calibration (Glint 2026-10-26, refit.py): the model against the measured frames, and signals.json against the calibrated model.
# The data's colour and level go through the three stand-ins for what the model lacks (gain, a wash of the state's own colour, a warm-white wash), each
# fitted to the 0.2.122 frames; the worst of the three is what the file's `dE` says. Exit 1 if a state's worst is over 3.5 dE (the batch's line is 6).
import json, os
from refit import *
cal = calibrate(); sig = json.load(open('../../../unity/Assets/Suborned/Resources/Atmosphere/signals.json'))['crossing']['states']; bad = 0
print('\n(4) 0.2.122 calibration: the uncalibrated model against the measured face colour (dE), then signals.json under the calibrated stand-ins')
for ph in ('day', 'evening', 'night'):
    g = PH[ph]
    was = {k: de2000(lab(g(s2l(c) * m)), lab(s2l(MEASURED[ph][k]))) for k, (c, m) in SHIPPED[ph].items()}
    print('  %-8s model-vs-measured of the 0.2.122 data: %s' % (ph, '  '.join('%s %.1f' % (k, v) for k, v in was.items())))
    for k in STATES:
        e = sig[k][ph]; tr = s2l(e['colour']); tgt = s2l(STATES[k])
        es = {n: de2000(lab(g(np.maximum(fn(tr, e['level'], cal[ph][n][0]), 0))), lab(tgt)) for n, fn in MODELS.items()}
        w = max(es.values()); ok = w <= 3.5 and w <= e['dE'] + 0.15; bad += not ok
        print('    %-8s %s x%.2f  worst %.2f (file says %.1f)  gain %.2f  wash-of-colour %.2f  white-wash %.2f  %s' % (k, e['colour'], e['level'], w, e['dE'], es['gain'], es['offset'], es['veil'], 'ok' if ok else 'CHECK'))
sys.exit(1 if bad else 0)
