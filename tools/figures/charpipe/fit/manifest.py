"""Brief 069: one manifest row per built figure of an end-to-end set, for Latch's import: the files, LOD triangles, bones, and each clip's measured
planted-foot speed on this figure (animate.py footSpeedMs: crouch walk can't be predicted by humanScale, Latch's take 1).
  python3 tools/figures/charpipe/fit/manifest.py <set: e2e|fixer> <metrics.json ...>"""
import json, os, sys, glob
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(HERE))))
SET = sys.argv[1]; E = json.load(open(os.path.join(HERE, f'{SET}.json'))); OUT = os.path.join(ROOT, E['out'], 'a')
speeds = {}
for m in sys.argv[2:]:
    d = json.load(open(m)); ids = d.get('figureIds', {})
    for lab, v in d.get('footSpeedMs', {}).items():
        if ids.get(lab): speeds[ids[lab]] = v
import numpy as np


def crown(sex, mac):
    """the base body's standing height at these macros, from the set's MPFB basis (the linear model fit_body.py uses)."""
    z = np.load(os.path.join(ROOT, E['out'], 'basis', f'basis_{sex}.npz')); V = z['ref_base'].copy()
    for m in ('weight', 'muscle', 'proportions', 'height'):
        x = mac.get(m, 0.5); k = f'{m}_1_base' if x >= 0.5 else f'{m}_0_base'; V += abs(x - 0.5) / 0.5 * (z[k] - z['ref_base'])
    V = V[z['base_body']]; return float(V[:, 2].max() - V[:, 2].min())


ENG = E.get('engineSpeeds'); eng = {}
if ENG and os.path.exists(os.path.join(ROOT, ENG['path'])):
    d = json.load(open(os.path.join(ROOT, ENG['path']))); eng = d.get('figures', d)
rows = []
for p in E['people']:
    fid = p['id']; st = json.load(open(os.path.join(OUT, f'{fid}_H.json')))
    chains = os.path.join(OUT, 'chains', 'chains_clips.json')
    rows.append({'id': fid, 'figure': f'{fid}_H.fbx', 'avatarT': st.get('avatarT'), 'props': f'{fid}_H_props.fbx' if os.path.exists(os.path.join(OUT, f'{fid}_H_props.fbx')) else None,
                 'texture': json.load(open(os.path.join(OUT, 'textures.json')))[fid]['file'], 'lodTris': st['lodTris'], 'deformBones': st['bones'],
                 'garmentRigs': st.get('garmentRigs', []), 'chainClips': json.load(open(chains)) if st.get('garmentRigs') and os.path.exists(chains) else None,  # a parked rig's old clips never reach Latch
                 'hands': st['looseRules'].get('hands'), 'clipSpeedMs': dict(speeds.get(fid, {})), 'clipSpeedSource': {},
                 'bodyHeightM': st.get('bodyHeightM'), 'targetHeightM': st.get('targetHeightM'), 'heightClass': st.get('heightClass'),
                 'texPx': st.get('texPx'), 'kind': st.get('kind')})
    row = rows[-1]; row['clipSpeedSource'] = {c: 'blender (animate.py foot_speed)' for c in row['clipSpeedMs']}
    e = eng.get(fid + (ENG or {}).get('figureSuffix', ''), {}) if ENG else {}
    for c in (ENG or {}).get('clips', []):
        if c in e and 'plantedMs' in e[c]:
            # the engine measured this clip on the take-1 skeleton (height slider 0.5): scaled to the new skeleton by the standing heights' ratio
            old_mac = dict(st['macros'], height=0.5); k = crown(st['sex'], st['macros']) / crown(st['sex'], old_mac)
            row['clipSpeedMs'][c] = round(e[c]['plantedMs'] * k, 4)
            row['clipSpeedSource'][c] = f"Unity (Latch's import measure {e[c]['plantedMs']} m/s on the take-1 skeleton) x {k:.4f} (new / old standing height)"
json.dump({'schema': 'suborned.charpipe.manifest/1', 'set': SET, 'rows': rows}, open(os.path.join(OUT, 'manifest.json'), 'w'), indent=1, sort_keys=True)
print(json.dumps([(r['id'], r['clipSpeedMs']) for r in rows], indent=0))
