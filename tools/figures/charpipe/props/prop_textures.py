"""Brief 069 props: a delivered long gun's maps repacked for URP Lit, per LOD (system python, PIL).
  python3 -I tools/figures/charpipe/props/prop_textures.py <prop>
props.json props.<prop>.source.maps and .material: albedo (sRGB) and normal (OpenGL +Y, Unity's convention) copied; URP's metallic map built:
R metallic (ORM blue), G occlusion (ORM red), A smoothness = max(1 - roughness (ORM green), clearcoat x clearcoatSmoothness). Clearcoat beyond
that and anisotropy have no place in URP Lit: dropped (props.json material.lost). Writes <prop>_LOD<k>_{albedo,normal,metal}.png + metas and
<prop>_LOD<k>.mat (+ meta) beside the FBX, and textures.json."""
import json, os, sys, hashlib
import numpy as np
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); FIG = os.path.dirname(os.path.dirname(HERE)); ROOT = os.path.dirname(os.path.dirname(FIG))
sys.path.append(FIG); import unitymeta as um
D = json.load(open(os.path.join(HERE, 'props.json'))); PID = sys.argv[1]; P = D['props'][PID]; M = P['source']['maps']
OUT = os.path.join(ROOT, 'out/scratch/forma/char-pipeline/props', PID, 'a'); REL = f'Props/{PID}'; rep = {}
for k, (size, px) in enumerate(zip(M['sizes'], M['px'])):
    src = lambda m: os.path.join(ROOT, M['pattern'].format(k=k, map=m, size=size))
    name = f'{PID}_LOD{k}'; files = {}
    for kind, m in (('albedo', 'BaseColor'), ('normal', 'NormalGL')):
        im = Image.open(src(m)).convert('RGB'); f = f'{name}_{kind}.png'; im.save(os.path.join(OUT, f), optimize=False)
        open(os.path.join(OUT, f) + '.meta', 'w').write(um.tex_meta(f'{REL}/{f}', kind, px)); files[kind] = f
    orm = np.asarray(Image.open(src('ORM')).convert('RGB')).astype(np.float32) / 255
    cc = np.asarray(Image.open(src('Clearcoat')).convert('L')).astype(np.float32) / 255
    sm = np.maximum(1 - orm[..., 1], cc * P['material']['clearcoatSmoothness'])
    mt = np.stack([orm[..., 2], orm[..., 0], np.zeros_like(sm), sm], -1)
    f = f'{name}_metal.png'; Image.fromarray((mt * 255 + 0.5).astype(np.uint8), 'RGBA').save(os.path.join(OUT, f), optimize=False)
    open(os.path.join(OUT, f) + '.meta', 'w').write(um.tex_meta(f'{REL}/{f}', 'data', px)); files['metal'] = f
    mrel = f'{REL}/{name}.mat'
    open(os.path.join(OUT, f'{name}.mat'), 'w').write(um.mat_text(name, base=f'{REL}/{files["albedo"]}', normal=f'{REL}/{files["normal"]}', metal=f'{REL}/{files["metal"]}', smoothness=1.0, metallic=1.0))
    open(os.path.join(OUT, f'{name}.mat') + '.meta', 'w').write(um.mat_meta(mrel))
    rep[name] = {'px': px, 'files': {kk: {'file': v, 'sha256': hashlib.sha256(open(os.path.join(OUT, v), 'rb').read()).hexdigest()} for kk, v in files.items()},
                 'smoothnessMean': round(float(sm.mean()), 3), 'clearcoatRaisedPct': round(float(((cc * P['material']['clearcoatSmoothness']) > 1 - orm[..., 1]).mean() * 100), 1)}
json.dump({'lost': P['material']['lost'], 'lods': rep}, open(os.path.join(OUT, 'textures.json'), 'w'), indent=1, sort_keys=True); print('TEX', json.dumps(rep)[:600])
