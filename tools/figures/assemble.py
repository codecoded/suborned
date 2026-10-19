"""Write People/manifest.json (suborned.people-models/1) from the build's rows, the clip rows, the specs and roles.json. System python, no bpy.
python3 -I tools/figures/assemble.py <People dir> [<blender version>]"""
import json, os, sys, hashlib, glob, datetime
here = os.path.dirname(os.path.abspath(__file__))
out = sys.argv[1]; blender = sys.argv[2] if len(sys.argv) > 2 else ''
STORE = os.path.expanduser(os.environ.get('SUBORNED_STORE', '~/Projects/the-suborned-store')) + '/source-art'
sha = lambda p: hashlib.sha256(open(p, 'rb').read()).hexdigest()
specs = {f['id']: f for f in json.load(open(f'{here}/figures.json'))['figures']}
roles = json.load(open(f'{here}/roles.json'))
rows = {}
for p in sorted(glob.glob(f'{out}/.rows.*.json'), key=os.path.getmtime):   # the newest build of a figure wins
    for r in json.load(open(p)): rows[r['id']] = r
clips = json.load(open(f'{out}/Clips/clips.rows.json'))
cspec = {c['name']: c for c in json.load(open(f'{here}/clips.json'))['clips']}
speeds = json.load(open(f'{here}/speeds.json'))['speeds']
for c in clips:
    if c['name'] in speeds: c['naturalSpeedMs'] = speeds[c['name']]; c['naturalSpeedSource'] = 'Unity planted-foot median, scout'
for c in clips: c['sourceSha256'] = sha(os.path.normpath(os.path.join(STORE, c['source']))); c['use'] = cspec[c['name']]['use']

def src_files(spec):
    paths = [l['src'] for l in spec['lods'] if 'src' in l] + ([spec['donor']] if spec.get('donor') else [])
    out_ = []
    for p in dict.fromkeys(paths):
        ap = os.path.normpath(os.path.join(STORE, p))
        out_.append({'path': p, 'sha256': sha(ap) if os.path.exists(ap) else None})
    return out_

props = []
for pr in json.load(open(f'{out}/.props.rows.json')) if os.path.exists(f'{out}/.props.rows.json') else []:
    pr = dict(pr); pr['files'] = [{'path': os.path.relpath(os.path.join(rt, f), out), 'sha256': sha(os.path.join(rt, f))} for rt, _, fs in os.walk(f"{out}/Props/{pr['id']}") for f in sorted(fs) if not f.endswith('.meta')]
    props.append(pr)

figs = []
for fid in specs:
    r = rows[fid]; spec = specs[fid]
    base = f'{out}/Figures/{fid}'
    files = []
    for root, _, fs in os.walk(base):
        for f in sorted(fs):
            if f.endswith('.meta'): continue
            p = os.path.join(root, f); files.append({'path': os.path.relpath(p, out), 'sha256': sha(p), 'bytes': os.path.getsize(p)})
    r = dict(r); r['files'] = sorted(files, key=lambda x: x['path']); r['sources'] = src_files(spec)
    if fid in roles['variantOf']: r['variantOf'] = roles['variantOf'][fid]
    if spec.get('tint'): r['tint'] = spec['tint']
    n = len(r['lods']); r['lodScreenHeights'] = roles['lodScreen'][str(n)]
    r['sha256'] = sha(f"{out}/{r['fbx']}")
    figs.append(r)
total = {'figures': len(figs), 'clips': len(clips), 'props': len(props), 'bytes': sum(f['bytes'] for r in figs for f in r['files']) + sum(os.path.getsize(f"{out}/{c['file']}") for c in clips)}
manifest = {
    'schema': 'suborned.people-models/1', 'standIn': True, 'brief': 'briefs/020-stand-in-visuals.md', 'owner': 'Forma', 'for': 'Latch',
    'built': {'by': 'tools/figures (build.py, clips.py, assemble.py)', 'blender': blender, 'specs': ['tools/figures/figures.json', 'tools/figures/clips.json', 'tools/figures/roles.json', 'tools/figures/seat.json']},
    'conventions': {
        'unit': 'metres, FBX unit scale 1 (file units cm, Unity useFileScale on)', 'axes': 'Y up, figure faces +Z', 'origin': 'feet on y = 0, centred on x and z', 'standingHeightM': 1.75,
        'rig': 'Humanoid, Mixamo bone names without prefix (Hips, Spine, Spine1, Spine2, Neck, Head, Left/RightShoulder, Arm, ForeArm, Hand, UpLeg, Leg, Foot, ToeBase). 22 mapped bones for the Meshy-style rigs (no fingers), 52 for the Enforcer (fingers). Avatar: Create From This Model, set in each .fbx.meta; Unity maps it unaided (checked: valid, human).',
        'lods': 'one FBX per figure holds the skeleton and every LOD as a skinned mesh named <id>_LOD0.. ; Unity builds the LODGroup from the names. Skin weights: 4 per vertex.',
        'materials': 'URP Lit .mat per texture set, bound by name in the .fbx.meta. Matte, as the world is (Glint, briefs/020/figures-render.md): the maps are albedo (sRGB) and normal for the 1024 and 2048 sets, plus on the cell and cast a metallic map (R metallic 0..0.6 only on real trim, G occlusion, A smoothness 0.15..0.28 on cloth, skin and hair and up to 0.55 on trim; the map is 1024 px even under a 2048 material). The guard family (guard, security, cafe_w5) keeps metal only on strips narrower than 15 px at 1024 (shoulder edges, belt, eyelets, boot caps): panels are R 0 and A <= 0.16, so the white and navy cloth is matte in the follow view. The 512 LOD (what play zoom draws) and every crowd material have no metallic map: _Metallic 0, _Smoothness 0.15. The Tech visor is _Smoothness 0.75 with an emission map; his albedo blacks are lifted (a black outfit keeps its shape in shade).',
        'clips': 'People/Clips/<name>.fbx: Mixamo library files copied as they are (clip only, 30 fps, in place), each with a Humanoid .meta and one clip named <name>. Any Humanoid figure plays any clip. loop flags are set in the meta; root motion is not baked into the pose, so leave Apply Root Motion off and move the object from the sim.',
        'tintedVariants': 'security, driver, councillor, rival and fixer are recolours of another model: only the albedo changed (hue window, skin and hair outside it).'},
    'playRoles': roles['playViews'], 'families': roles['families'], 'jobBlocks': roles['jobBlocks'],
    'missing': roles['missing'], 'forLatch': roles['forLatch'], 'pulledFromPools': roles.get('pulledFromPools', {}),
    'seat': json.load(open(f'{here}/seat.json')),
    'totals': total, 'figures': figs, 'clips': clips, 'props': props,
}
json.dump(manifest, open(f'{out}/manifest.json', 'w'), indent=1)
print('manifest', total)
