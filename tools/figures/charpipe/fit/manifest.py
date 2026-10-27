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
rows = []
for p in E['people']:
    fid = p['id']; st = json.load(open(os.path.join(OUT, f'{fid}_H.json')))
    chains = os.path.join(OUT, 'chains', 'chains_clips.json')
    rows.append({'id': fid, 'figure': f'{fid}_H.fbx', 'avatarT': st.get('avatarT'), 'props': f'{fid}_H_props.fbx' if os.path.exists(os.path.join(OUT, f'{fid}_H_props.fbx')) else None,
                 'texture': json.load(open(os.path.join(OUT, 'textures.json')))[fid]['file'], 'lodTris': st['lodTris'], 'deformBones': st['bones'],
                 'garmentRigs': st.get('garmentRigs', []), 'chainClips': json.load(open(chains)) if os.path.exists(chains) else None,
                 'hands': st['looseRules'].get('hands'), 'clipSpeedMs': speeds.get(fid, {})})
json.dump({'schema': 'suborned.charpipe.manifest/1', 'set': SET, 'rows': rows}, open(os.path.join(OUT, 'manifest.json'), 'w'), indent=1, sort_keys=True)
print(json.dumps([(r['id'], r['clipSpeedMs']) for r in rows], indent=0))
