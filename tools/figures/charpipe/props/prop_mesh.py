"""Brief 069 props: a held prop's reference sheet -> multi-view crops -> Tripo 3.1 on Runware (textured, PBR), through tools/paid/paid.py.
  python3 -I tools/figures/charpipe/props/prop_mesh.py crops <prop>     # $0: the view crops (out/scratch/forma/char-pipeline/props/<prop>/crops/)
  python3 -I tools/figures/charpipe/props/prop_mesh.py open <prop>      # open the pass (budget from props.json; refuses to raise)
  python3 -I tools/figures/charpipe/props/prop_mesh.py mesh <prop> [--take N]
Crops: each sheet cell is cut against the sheet's flat ground, the object recentred in a cell-sized square of that ground at the sheet's one scale
(the views keep their relative size: no view is fitted to its own frame), then 2x to 1024 px. Views go in props.json mesh.views order, which is
Tripo's [front, left, back, right]."""
import json, os, sys, base64
import numpy as np
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(HERE))))
sys.path.insert(0, os.path.join(ROOT, 'tools/paid'))
D = json.load(open(os.path.join(HERE, 'props.json')))
SCR = os.path.join(ROOT, 'out/scratch/forma/char-pipeline/props')


def central(occ, gap=8):
    """of the runs of occupied columns (or rows) split by gaps of at least `gap` empty ones, the run nearest the cell's centre"""
    idx = np.nonzero(occ)[0]; runs = []; st = idx[0]
    for a, b in zip(idx[:-1], idx[1:]):
        if b - a > gap: runs.append((st, a)); st = b
    runs.append((st, idx[-1])); c = len(occ) / 2
    r0, r1 = min(runs, key=lambda r: 0 if r[0] <= c <= r[1] else min(abs(r[0] - c), abs(r[1] - c)))
    keep = np.zeros_like(occ); keep[r0:r1 + 1] = True; return keep


def crops(pid):
    P = D['props'][pid]; C = P['sheetCells']; out = os.path.join(SCR, pid, 'crops'); os.makedirs(out, exist_ok=True)
    a = np.asarray(Image.open(os.path.join(ROOT, P['sheet'])).convert('RGB')).astype(np.int16); h, w, _ = a.shape
    ch, cw = h // C['rows'], w // C['cols']; side = max(ch, cw); rep = {}
    for v in P['mesh']['views']:
        r, c = C['views'][v]; cell = a[r * ch:(r + 1) * ch, c * cw:(c + 1) * cw]
        ground = np.median(np.concatenate([cell[:6].reshape(-1, 3), cell[-6:].reshape(-1, 3), cell[:, :6].reshape(-1, 3), cell[:, -6:].reshape(-1, 3)]), 0)
        m = np.abs(cell - ground).max(2) > 14
        m = m & central(m.any(0))[None, :]; m = m & central(m.any(1))[:, None]   # the view at the cell's centre only (a neighbour can spill in)
        ys, xs = np.nonzero(m); y0, y1, x0, x1 = ys.min(), ys.max(), xs.min(), xs.max()
        canvas = np.empty((side, side, 3), np.int16); canvas[:] = ground
        oy = (side - (y1 - y0 + 1)) // 2; ox = (side - (x1 - x0 + 1)) // 2
        canvas[oy:oy + y1 - y0 + 1, ox:ox + x1 - x0 + 1] = cell[y0:y1 + 1, x0:x1 + 1]
        im = Image.fromarray(canvas.clip(0, 255).astype(np.uint8)).resize((side * 2, side * 2), Image.LANCZOS)
        im.save(os.path.join(out, f'{pid}_{v}.png')); rep[v] = {'cell': [r, c], 'bboxPx': [int(x1 - x0 + 1), int(y1 - y0 + 1)], 'ground': [int(x) for x in ground]}
    json.dump(rep, open(os.path.join(out, 'crops.json'), 'w'), indent=1, sort_keys=True); print('crops', rep)


def uri(p): return 'data:image/png;base64,' + base64.b64encode(open(p, 'rb').read()).decode()


def mesh(pid, take):
    import paid
    P = D['props'][pid]; M = P['mesh']; out = os.path.join(SCR, pid, 'crops')
    files = [os.path.join(out, f'{pid}_{v}.png') for v in M['views']]
    p = paid.Pass(M['job'], M['pass'])
    p.mesh({'model': M['model'], 'inputs': {'images': [uri(f) for f in files]}, 'settings': dict(M['settings']), 'includeCost': True},
           name=f'{pid}_t{take}.glb', subdir='meshes', est=M['estUsd'], prompt=f"image-to-3D, views {M['views']} as Tripo's [front, left, back, right], from {P['sheet']}",
           refs_log=files, note=f"take {take}; {P['label']}", used_in=['briefs/069-character-pipeline.md'])


if __name__ == '__main__':
    a = sys.argv[1:]
    if a[0] == 'crops': crops(a[1])
    elif a[0] == 'open':
        import paid; M = D['props'][a[1]]['mesh']
        paid.open_pass(M['job'], M['pass'], M['budgetUsd'], note="Sky OK'd 2026-10-27 (via Aster): the cell's pistol, design 4, one Tripo 3.1 take at about $0.60, budget $1.00; no retry above budget, no other provider")
    elif a[0] == 'mesh': mesh(a[1], int(a[a.index('--take') + 1]) if '--take' in a else 1)
