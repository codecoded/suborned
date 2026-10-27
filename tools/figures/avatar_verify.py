"""Read the installed People metas back and check the brief 069 avatar rule held (no Unity, no Blender: the metas only).
  python3 -I tools/figures/avatar_verify.py [<People dir>]      (default unity/Assets/Suborned/People; exits 1 on any miss)
Clips: Copy From Other Avatar (avatarSetup 2) from the reference clip's avatar (clips.json avatar.reference, fileID 9000000), or, for a clip whose
rig can't copy it (clips.rows.json avatar.rigDifferences), its own avatar with auto-mapping off. The reference and every own avatar: auto-mapping
off, the human map written, and the legs neutral in the written rest (each UpLeg->Leg and Leg->Foot straight down, Foot->ToeBase along the hips'
facing at the stated pitch, ToeBase->Toe_End level). Figures: their own avatar, auto-mapping off, legs neutral at their own flat pitch."""
import sys, os, re, glob, json, math
sys.path.append(os.path.dirname(os.path.abspath(__file__)))   # appended: tools/figures/inspect.py would shadow the stdlib one numpy imports
import numpy as np
import avatar as av
import unitymeta as um

HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(os.path.dirname(HERE))
P = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, 'unity/Assets/Suborned/People')
SPEC = json.load(open(os.path.join(HERE, 'clips.json'))); REF = SPEC['avatar']['reference']; REF_GUID = um.guid(f'Clips/{REF}.fbx')
num = r'(-?[0-9.e+-]+)'
ROW = re.compile(r'\n    - name: (.*)\n      parentName: (.*)\n      position: \{x: ' + num + ', y: ' + num + ', z: ' + num + r'\}\n      rotation: \{x: '
                 + num + ', y: ' + num + ', z: ' + num + ', w: ' + num + r'\}\n      scale: \{x: ' + num + ', y: ' + num + ', z: ' + num + r'\}')


def field(s, k):
    m = re.search(r'\n  ' + k + r': (.*)', s); return m.group(1) if m else None


def legs(s):
    """World leg directions from the written rest: worst angle of the shins and thighs from straight down, each foot's yaw from the hips' facing
    and its pitch, each toe's tilt (degrees)."""
    nodes = {}; order = []
    for r in ROW.findall(s):
        n = av.Node(r[0], nodes.get(r[1]) if r[1] else None)
        n.lpos = np.array([float(x) for x in r[2:5]]); n.lrot = np.array([float(x) for x in r[5:9]]); n.lscl = np.array([float(x) for x in r[9:12]])
        nodes[r[0]] = n; order.append(n)
    by = {k.split(':')[-1]: n for k, n in nodes.items()}
    lr = by['RightUpLeg'].position - by['LeftUpLeg'].position; lr[1] = 0; fwd = av.norm(np.cross(lr, av.UP))
    out = {'legDeg': 0.0, 'yawDeg': 0.0, 'pitchDeg': [], 'toeDeg': 0.0}
    for sd in ('Left', 'Right'):
        for a, b in (('UpLeg', 'Leg'), ('Leg', 'Foot')):
            out['legDeg'] = max(out['legDeg'], av.angle(by[sd + b].position - by[sd + a].position, -av.UP))
        d = by[sd + 'ToeBase'].position - by[sd + 'Foot'].position; h = d.copy(); h[1] = 0
        out['yawDeg'] = max(out['yawDeg'], av.angle(h, fwd)); out['pitchDeg'].append(math.degrees(math.atan2(-d[1], np.linalg.norm(h))))
        if sd + 'Toe_End' in by:
            t = by[sd + 'Toe_End'].position - by[sd + 'ToeBase'].position; out['toeDeg'] = max(out['toeDeg'], av.angle(t, fwd))
    return out, len(re.findall(r'\n    - boneName: ', s)), len(order)


def own_ok(s, pitch=None):
    if field(s, 'autoGenerateAvatarMappingIfUnspecified') != '0' or field(s, 'avatarSetup') != '1': return False, 'not an own avatar with auto-mapping off'
    g, nh, ns = legs(s)
    bad = g['legDeg'] > 0.05 or g['yawDeg'] > 0.05 or g['toeDeg'] > 0.05 or abs(g['pitchDeg'][0] - g['pitchDeg'][1]) > 0.05 or (pitch is not None and abs(g['pitchDeg'][0] - pitch) > 0.05)
    return (not bad and nh > 0), {'human': nh, 'skeleton': ns, 'legDeg': round(g['legDeg'], 4), 'yawDeg': round(g['yawDeg'], 4), 'toeDeg': round(g['toeDeg'], 4), 'pitchDeg': [round(x, 3) for x in g['pitchDeg']]}


rows = {r['name']: r for r in json.load(open(os.path.join(P, 'Clips', 'clips.rows.json')))}
res = {'clips': {'copied': [], 'own': [], 'reference': None, 'miss': {}}, 'figures': {'ok': [], 'miss': {}}}
for f in sorted(glob.glob(os.path.join(P, 'Clips', '*.fbx'))):
    n = os.path.basename(f)[:-4]; s = open(f + '.meta').read()
    if n == REF:
        ok, info = own_ok(s, SPEC['avatar']['footPitchDeg']); res['clips']['reference'] = info
        if not ok: res['clips']['miss'][n] = info
    elif field(s, 'avatarSetup') == '2':
        src = field(s, 'lastHumanDescriptionAvatarSource')
        if src == f'{{fileID: 9000000, guid: {REF_GUID}, type: 3}}': res['clips']['copied'].append(n)
        else: res['clips']['miss'][n] = f'copies {src}'
    elif n in rows and rows[n].get('avatar', {}).get('own'):
        ok, info = own_ok(s, SPEC['avatar']['footPitchDeg'])
        hips_parent = re.search(r'\n    - name: mixamorig:Hips\n      parentName: (.*)', s).group(1)
        if hips_parent != f'{n}(Clone)' and field(s, '  preserveHierarchy') != '1':   # brief 069 A: a written Armature above Hips needs Preserve Hierarchy
            ok = False; info = {**info, 'error': f'Hips under {hips_parent} without preserveHierarchy 1'} if isinstance(info, dict) else info
        if ok: res['clips']['own'].append(n)
        else: res['clips']['miss'][n] = info
    else: res['clips']['miss'][n] = f"avatarSetup {field(s, 'avatarSetup')}, not in clips.json" if n not in rows else 'no avatar rule written'
for f in sorted(glob.glob(os.path.join(P, 'Figures', '*', '*.fbx'))):
    n = os.path.basename(f)[:-4]; ok, info = own_ok(open(f + '.meta').read())
    if ok: res['figures']['ok'].append({'id': n, **info})
    else: res['figures']['miss'][n] = info
c = res['clips']; fg = res['figures']
summary = {'clipFiles': len(glob.glob(os.path.join(P, 'Clips', '*.fbx'))), 'copied': len(c['copied']), 'reference': 1 if c['reference'] and REF not in c['miss'] else 0,
           'ownNeutral': len(c['own']), 'clipMisses': len(c['miss']), 'figureFiles': len(glob.glob(os.path.join(P, 'Figures', '*', '*.fbx'))), 'figuresNeutral': len(fg['ok']), 'figureMisses': len(fg['miss'])}
print(json.dumps({'summary': summary, **res}, indent=1))
sys.exit(1 if c['miss'] or fg['miss'] else 0)
