"""Stage 0 (brief 069 fit test): MPFB samples per sex -> a linear body model the fit can evaluate in a millisecond.
  out/scratch/forma/char-pipeline/blender.sh -P tools/figures/charpipe/fit/basis.py
Writes fit-test/basis/basis_<sex>.npz: bone names, parents, and for the ref body and each macro at 0 and 1: proxy vertices, basemesh vertices
(all 19,158, helpers included, with a body mask), bone heads and tails; plus the ref body's faces and 52-bone weights for the proxy and the basemesh."""
import os, sys, importlib.util, json
import numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
L = importlib.util.module_from_spec(s := importlib.util.spec_from_file_location('fitlib', os.path.join(os.path.dirname(os.path.abspath(__file__)), 'lib.py'))); s.loader.exec_module(L)

out = L.ensure(os.path.join(L.SCR, 'basis'))
for sex in ('m', 'f'):
    samples = [('ref', {})] + [(f'{m}_{e}', {m: float(e)}) for m in L.D['basis']['macros'] for e in (0, 1)]
    arrays = {}
    for name, mac in samples:
        L.reset(); arm, b, px = L.human(mac, sex)
        names = list(L.KEEP)
        H, T, par = L.bones_of(arm, names)
        arrays[f'{name}_proxy'] = L.eval_co(px, armature=False); arrays[f'{name}_base'] = L.eval_co(b, armature=False)
        arrays[f'{name}_H'] = H; arrays[f'{name}_T'] = T
        if name == 'ref':
            arrays['names'] = np.array(names); arrays['par'] = np.array(par)
            arrays['proxy_tris'] = L.tris_of(px); arrays['base_tris'] = L.tris_of(b)
            arrays['proxy_W'] = L.weights_of(px, names); arrays['base_W'] = L.weights_of(b, names)
            body = b.vertex_groups['body'].index
            arrays['base_body'] = np.array([any(g.group == body and g.weight > 0.5 for g in v.groups) for v in b.data.vertices])
            arrays['proxy_faces_n'] = np.array([len(p.vertices) for p in px.data.polygons])
        bb = arrays[f'{name}_base'][arrays['base_body']]; L.log(sex, name, 'height', round(float(np.ptp(bb[:, 2])), 3), 'width', round(float(np.ptp(bb[:, 0])), 3))
    np.savez_compressed(os.path.join(out, f'basis_{sex}.npz'), **arrays)
    L.log('basis', sex, 'samples', len(samples))
