# Who reads as the guard, at true size (Pulse, brief 067 I, re-run 2026-10-26 after Forma's Velvet Set and Latch's body figure).
# Forma's proof tools (tools/figures/proof_run.py: the play camera, the game's sun and grade, a real stretch of the road; blocks.py's measure) put every figure of the
# crowd, the cast, the cell and the crew in one row at 53 px, and this takes each one's upper block (12-50 % of its height), lower block (55-92 %) and head block (top 12 %),
# the mean colour of the figure's own pixels, and the CIE76 dE of each from the guard's and from the police's (the two figures that wear the duty suit's model).
#   python3 -I briefs/067/pulse/look-matrix.py <People dir> <yaw> <sun|shade> <heading|cam> id[,id...] [--ref guard,security]
# heading `cam` faces the camera as proof_run does (yaw + 160); a number is Unity degrees (0 north, 90 east), as Forma's clerk board (75). Stdout is a table; it changes nothing.
import sys, os, importlib.util, tempfile
import numpy as np

R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))
def load(name, path):
    sp = importlib.util.spec_from_file_location(name, path); m = importlib.util.module_from_spec(sp); sp.loader.exec_module(m); return m
pr = load('proof_run', os.path.join(R, 'tools/figures/proof_run.py')); pp = pr.pp   # tools/figures has an inspect.py: never on sys.path

def lab(rgb8):
    c = pp.srgb_to_lin(np.asarray(rgb8, float) / 255.0)
    M = np.array([[0.4124, 0.3576, 0.1805], [0.2126, 0.7152, 0.0722], [0.0193, 0.1192, 0.9505]]); xyz = c @ M.T / np.array([0.95047, 1.0, 1.08883])
    f = np.where(xyz > 0.008856, np.cbrt(xyz), 7.787 * xyz + 16 / 116); return np.array([116 * f[1] - 16, 500 * (f[0] - f[1]), 200 * (f[1] - f[2])])

def blocks(people, yaw, light, heading, ids, spacing=1.2, samples=64, h=150):
    figs = pr.parse_figs(','.join(i if heading == 'cam' else f'{i}={heading}' for i in ids)); n = len(figs)
    wd = tempfile.mkdtemp(prefix='pulse_look_')
    img = pr.run(figs, people, yaw, light, spacing, 4, h=h, workdir=wd, samples=samples)
    F = np.load(wd + '/p_figs.npy').astype(float); H, W = F.shape[:2]; ppm = pr.pp_ppm(); out = {}
    for i, (fid, hd, alb, rs, ps) in enumerate(figs):
        cx = W / 2 + (i - (n - 1) / 2) * spacing * ppm; x0, x1 = int(cx - 0.5 * spacing * ppm), int(cx + 0.5 * spacing * ppm)
        a = F[:, x0:x1, 3]; ys = np.nonzero(a.max(1) > 0.5)[0]
        if not len(ys): continue
        y0, y1 = ys.min(), ys.max(); hgt = y1 - y0 + 1; sub = img[:, x0:x1].astype(float); m = a > 0.5
        def block(r0, r1):
            mm = m.copy(); mm[:r0] = False; mm[r1:] = False
            return sub[mm].mean(0) if mm.any() else np.zeros(3)
        out[fid] = dict(up=block(y0 + int(0.12 * hgt), y0 + int(0.50 * hgt)), lo=block(y0 + int(0.55 * hgt), y1 - int(0.08 * hgt)), head=block(y0, y0 + int(0.12 * hgt)), h=hgt)
    return out

if __name__ == '__main__':
    args = [a for a in sys.argv[1:] if not a.startswith('--ref')]; refs = ['guard', 'security']
    for a in sys.argv[1:]:
        if a.startswith('--ref='): refs = a[6:].split(',')
    people, yaw, light, heading, ids = args[0], float(args[1]), args[2], args[3], args[4].split(',')
    B = blocks(people, yaw, light, heading, ids)
    print(f'# {light}, yaw {yaw:g}, heading {heading}; figure height {sorted(set(v["h"] for v in B.values()))} px; dE = CIE76 between block means (upper / lower / head); sum = upper + lower (Forma\'s)')
    for ref in refs:
        if ref not in B: continue
        r = B[ref]; rows = []
        for fid in ids:
            if fid == ref or fid not in B: continue
            b = B[fid]; du = np.linalg.norm(lab(b['up']) - lab(r['up'])); dl = np.linalg.norm(lab(b['lo']) - lab(r['lo'])); dh = np.linalg.norm(lab(b['head']) - lab(r['head']))
            rows.append((du + dl, fid, du, dl, dh))
        print(f'vs {ref}  (nearest first)')
        for s, fid, du, dl, dh in sorted(rows): print(f'  {fid:11s} upper {du:5.1f}  lower {dl:5.1f}  head {dh:5.1f}  sum {s:5.1f}')
