"""Copy the Mixamo library clips the game uses into People/Clips with a Humanoid .meta, and measure each (frames, fps, natural speed of the
treadmill). Blender: blender -b -P tools/figures/clips.py -- <clips.json> <People dir>. Writes People/Clips/clips.rows.json.
The library files are clip-only FBX (Mixamo skeleton), in place (Hips do not travel), 30 fps."""
import sys, os, json, shutil, hashlib
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from figlib import *
import unitymeta as um
import author_clips

STORE = os.path.expanduser(os.environ.get('SUBORNED_STORE', '~/Projects/the-suborned-store')) + '/source-art'
a = args(); spec = json.load(open(a[0])); out = a[1]; rows = []
os.makedirs(f'{out}/Clips', exist_ok=True)
for c in spec['clips']:
    if 'author' in c:   # posed here, on the donor clip's skeleton (tools/figures/author_clips.py); the donor is the recorded source
        os.makedirs(f'{out}/.authored', exist_ok=True); src = f"{out}/.authored/{c['name']}.fbx"
        (author_clips.make_testbed if c.get('kind') == 'testbed' else author_clips.make_carry if c.get('kind') in ('carry', 'tray') else author_clips.make_pinned if c.get('kind') in ('pinned', 'seated') else author_clips.make)(c, STORE, src); c['src'] = c['donor']
    else:
        src = c['src'] if os.path.isabs(c['src']) else os.path.join(STORE, c['src'])
    clean(); new = load(src); arm, _ = rigs(new)
    act = arm.animation_data.action; arm.animation_data.action = act
    f0, f1 = int(act.frame_range[0]), int(act.frame_range[1]); sc = bpy.context.scene
    # natural speed: while a foot is planted (within 4 cm of the lowest the clip ever puts a foot) it runs backwards, in the hips' frame, at the body's speed
    P = {s: [] for s in ('Left', 'Right')}
    for fn in range(f0, f1 + 1):
        sc.frame_set(fn)
        for s in P: P[s].append(arm.matrix_world @ arm.pose.bones[f'mixamorig:{s}Foot'].matrix.translation)
    ground = min(p.z for s in P for p in P[s]); sp = []
    for s in P:
        for k in range(1, len(P[s])):
            a_, b_ = P[s][k - 1], P[s][k]
            if a_.z <= ground + 0.04 and b_.z <= ground + 0.04: sp.append(Vector((b_.x - a_.x, b_.y - a_.y, 0)).length * sc.render.fps)
    sp.sort(); nat = round(sp[len(sp) // 2], 2) if len(sp) > 3 else 0.0
    name = c['name']; rel = f'Clips/{name}.fbx'; dst = os.path.join(out, rel)
    shutil.copyfile(src, dst)
    open(dst + '.meta', 'w').write(um.clip_meta(rel, name, 'mixamo.com', 0, f1 - f0, c.get('loop', False), feet=c.get('feet', False)))
    rows.append({'name': name, 'file': rel, 'source': c['src'], 'sha256': hashlib.sha256(open(dst, 'rb').read()).hexdigest(), 'frames': f1 - f0 + 1,
                 'fps': sc.render.fps, 'seconds': round((f1 - f0 + 1) / sc.render.fps, 3), 'loop': c.get('loop', False), 'naturalSpeedMs': nat if c.get('loco') else None, 'use': c.get('use', '')})
    if 'author' in c: rows[-1]['authoredBy'] = c['author']
    if 'glb' in c: rows[-1]['retargetedFrom'] = c['glb'] + ':' + c['clip']; rows[-1]['retargetedFromSha256'] = hashlib.sha256(open(os.path.join(STORE, '..', c['glb']), 'rb').read()).hexdigest()
    if 'pin' in c: rows[-1]['pinnedTo'] = c['pin']; rows[-1]['pinnedToSha256'] = hashlib.sha256(open(os.path.join(STORE, c['pin']), 'rb').read()).hexdigest(); rows[-1]['seat'] = c.get('seat')
    print('CLIP', name, rows[-1]['frames'], 'f', 'speed', nat)
json.dump(rows, open(f'{out}/Clips/clips.rows.json', 'w'), indent=1)
