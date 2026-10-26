# colour blocks of figures at true size: the mean colour of the upper and the lower half of each, and how far each is from the guard (CIE76 dE). python3 -I blocks.py <people> <yaw> <light> id[=heading] ...
import sys, os, importlib.util, tempfile, json, subprocess
import numpy as np
R = '/Users/sky/Projects/suborned/'
def load(name, path):
    sp = importlib.util.spec_from_file_location(name, path); m = importlib.util.module_from_spec(sp); sp.loader.exec_module(m); return m
pr = load('proof_run', R + 'tools/figures/proof_run.py'); pp = pr.pp
people, yaw, light = sys.argv[1], float(sys.argv[2]), sys.argv[3]
figs = pr.parse_figs(','.join(sys.argv[4:])); n = len(figs); spacing = 1.2
wd = tempfile.mkdtemp(prefix='blocks_')
img = pr.run(figs, people, yaw, light, spacing, 4, h=150, workdir=wd, samples=64)
F = np.load(wd + '/p_figs.npy').astype(float); H, W = F.shape[:2]
def lab(rgb8):
    c = pp.srgb_to_lin(np.asarray(rgb8, float) / 255.0)
    M = np.array([[0.4124, 0.3576, 0.1805], [0.2126, 0.7152, 0.0722], [0.0193, 0.1192, 0.9505]]); xyz = c @ M.T / np.array([0.95047, 1.0, 1.08883])
    f = np.where(xyz > 0.008856, np.cbrt(xyz), 7.787 * xyz + 16 / 116); return np.array([116 * f[1] - 16, 500 * (f[0] - f[1]), 200 * (f[1] - f[2])])
rows = []
ppm = pr.pp_ppm()
for i, (fid, hd, alb, rs, ps) in enumerate(figs):
    cx = W / 2 + (i - (n - 1) / 2) * spacing * ppm
    x0, x1 = int(cx - 0.5 * spacing * ppm), int(cx + 0.5 * spacing * ppm)
    a = F[:, x0:x1, 3]; ys = np.nonzero(a.max(1) > 0.5)[0]
    if not len(ys): continue
    y0, y1 = ys.min(), ys.max(); hgt = y1 - y0 + 1
    sub = img[:, x0:x1].astype(float); m = a > 0.5
    def block(r0, r1):
        mm = m.copy(); mm[:r0] = False; mm[r1:] = False
        return sub[mm].mean(0) if mm.any() else np.zeros(3)
    up = block(y0 + int(0.12 * hgt), y0 + int(0.50 * hgt)); lo = block(y0 + int(0.55 * hgt), y1 - int(0.08 * hgt)); head = block(y0, y0 + int(0.12 * hgt))
    rows.append((fid, up, lo, head, hgt))
g = next(r for r in rows if r[0] == 'guard') if any(r[0] == 'guard' for r in rows) else None
print(f'{light} yaw {yaw:g}: figure height px', [r[4] for r in rows][:3])
print(f'{"figure":12s} {"upper rgb":>16s} {"lower rgb":>16s} {"head rgb":>16s}  dE upper  dE lower  dE sum (vs guard)')
for fid, up, lo, hd, h in rows:
    s = ''
    if g is not None:
        du = np.linalg.norm(lab(up) - lab(g[1])); dl = np.linalg.norm(lab(lo) - lab(g[2])); s = f'{du:8.1f} {dl:8.1f} {du + dl:8.1f}'
    print(f'{fid:12s} {str(tuple(up.round().astype(int))):>16s} {str(tuple(lo.round().astype(int))):>16s} {str(tuple(hd.round().astype(int))):>16s}  {s}')
