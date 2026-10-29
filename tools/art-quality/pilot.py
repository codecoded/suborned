"""Isolated material-quality pilot; no scene, mesh, kit or installed material writes.
python3 tools/art-quality/pilot.py [--out DIR]
Then: Blender -b -t 2 -P tools/art-quality/render.py -- DIR
Outputs Unity .mat/.meta and PNG/.meta candidates with deterministic GUIDs and source hashes.
"""
import argparse
import hashlib
import json
import re
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'tools/figures'))
import unitymeta as um


def sha(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()


def weave(n=512):
    y, x = np.mgrid[:n, :n]
    # Periodic warp/weft; neutral linear detail, no painted lighting or new hue.
    a = .5 + .018 * np.cos(2 * np.pi * x / 8) + .018 * np.cos(2 * np.pi * y / 8)
    return a


def leaf_detail(n=512):
    # Periodic overlapping leaf-sized ellipses, all height/detail (no colour tint).
    y, x = np.mgrid[:n, :n] / n
    h = np.zeros((n, n)); rng = np.random.default_rng(761)
    for _ in range(85):
        cx, cy = rng.random(2); a = rng.uniform(0, 2 * np.pi)
        dx, dy = (x - cx + .5) % 1 - .5, (y - cy + .5) % 1 - .5
        u, v = dx * np.cos(a) + dy * np.sin(a), -dx * np.sin(a) + dy * np.cos(a)
        q = (u / .075) ** 2 + (v / .035) ** 2
        h = np.maximum(h, np.maximum(0, 1 - q) ** .7)
    h -= h.mean()
    return np.clip(.5 + .09 * h / max(h.std(), 1e-6), .30, .70)


def normal(h, strength):
    dx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * strength
    dy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * strength
    n = np.dstack((-dx, dy, np.ones_like(h)))
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    return np.rint((n * .5 + .5) * 255).astype(np.uint8)


def generate(out):
    out = out.resolve()
    # Even an explicit output argument cannot overwrite installed assets/source.
    if not out.is_relative_to(ROOT / 'out/scratch'):
        raise ValueError('Pilot output must be under out/scratch; installation is a separate step')
    out.mkdir(parents=True, exist_ok=True)
    sources = []; maps = {}; variants = []
    def texture(name, array, kind):
        p = out / name; Image.fromarray(array).save(p)
        (out / (name + '.meta')).write_text(um.tex_meta('quality-pilot/' + name, kind, max(array.shape[:2])))
        maps[name] = {'path':str(p), 'sha256':sha(p), 'kind':kind, 'size':list(Image.open(p).size)}
        return p
    for key, h, bump in [('cloth', weave(), 1.4), ('leaf', leaf_detail(), 1.2)]:
        texture(key + '-detail.png', np.repeat(np.rint(h[...,None] * 255).astype(np.uint8), 3, axis=2), 'data')
        texture(key + '-normal.png', normal(h, bump), 'normal')
    def source(p):
        sources.append({'path':str(p.relative_to(ROOT)), 'sha256':sha(p)})
    def material(original, key, details=None, mask=None):
        source(original); t = original.read_text(); name = 'Pilot_' + key
        t = re.sub(r'm_Name: [^\n]*', 'm_Name: ' + name, t, count=1)
        def slot(s, file, scale=1):
            nonlocal t
            block = f'    - {s}:\n        m_Texture: {{fileID: 2800000, guid: {um.guid("quality-pilot/"+file)}, type: 3}}\n        m_Scale: {{x: {scale}, y: {scale}}}\n        m_Offset: {{x: 0, y: 0}}'
            t, count = re.subn(r'    - '+s+r':\n        m_Texture: [^\n]+\n        m_Scale: [^\n]+\n        m_Offset: [^\n]+', block, t)
            if count != 1:raise ValueError((original, s, count))
        kws=[]
        if details:
            layer, metres, bump = details
            slot('_DetailAlbedoMap', layer+'-detail.png', 1/metres)
            slot('_DetailNormalMap', layer+'-normal.png', 1/metres)
            t = re.sub(r'(- _DetailNormalMapScale: )[^\n]+', rf'\g<1>{bump}', t)
            kws.append('_DETAIL_MULX2')
        if mask:
            slot('_MetallicGlossMap', mask);slot('_OcclusionMap', mask)
            t=re.sub(r'(- _Smoothness: )[^\n]+', r'\g<1>1',t)
            kws += ['_METALLICSPECGLOSSMAP','_OCCLUSIONMAP']
        old=re.search(r'm_ValidKeywords:(.*?)(?=  m_InvalidKeywords:)',t,re.S)
        have=re.findall(r'- (_\w+)',old[1]);have=sorted(set(have+kws))
        t=t[:old.start()]+ 'm_ValidKeywords:\n'+''.join('  - '+k+'\n' for k in have)+t[old.end():]
        p=out/(name+'.mat');p.write_text(t);(out/(name+'.mat.meta')).write_text(um.mat_meta('quality-pilot/'+p.name))
        variants.append({'source':str(original), 'candidate':str(p), 'name':key})
    kit=ROOT/'unity/Assets/Suborned/Kit/Materials'
    for n in ['DK_Cloth_Cream','DK_Cloth_CoralSoft']:
        material(kit/(n+'.mat'),n,('cloth',.10,.18))
    for n in ['DK_Leaf_Civic','DK_Leaf_CivicDark']:
        material(kit/(n+'.mat'),n,('leaf',.35,.25))
    # The earlier Humanoid build already supplies a bounded mask on the same
    # Scout texture layout. Reuse it as a pilot, never infer skin from hue.
    people=ROOT/'unity/Assets/Suborned/People/Figures/scout'
    oldmask=people/'Textures/scout_body_2048_metal.png';source(oldmask)
    px=np.asarray(Image.open(oldmask).convert('RGBA').resize((512,512),Image.Resampling.BOX)).copy()
    px[...,0]=0;px[...,3]=np.clip(px[...,3],38,71)
    texture('scout-surface.png',px,'data')
    material(people/'Materials/scout_body_512.mat','scout_body_512',mask='scout-surface.png')
    manifest={'schema':'suborned.material-pilot/1','status':'candidate-not-installed', 'sources':sources,'maps':maps,'materials':variants,
        'meshPolicy':'unchanged; import current installed FBXs; no generation or decimation',
        'scope':'CafeBay_v2 awning cloth, PlanterRun_4_A leaf surfaces, Scout tactical-tier AO/smoothness response',
        'limits':['Blender approximates URP; game look and Air performance still require Unity verification.',
                  'Scout mask is an existing authored UV-layout mask, not semantic skin/leather segmentation.'],
        'estimatedAddedBCMiBWithMips':round(sum(max(m['size'])**2*4/3 for m in maps.values())/2**20,3)}
    (out/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    print(json.dumps({'output':str(out),'materials':len(variants),'maps':len(maps),'status':manifest['status']}))

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--out',type=Path,default=ROOT/'out/scratch/codex/material-pilot');args=p.parse_args();generate(args.out)
