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
