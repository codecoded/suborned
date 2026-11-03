"""Stage 3 · clips (brief 069). Blender side.
  blender -b --factory-startup -P tools/figures/charpipe/pipeline/clips.py -- audit  <out audit.json> [gait ...]
  blender -b --factory-startup -P tools/figures/charpipe/pipeline/clips.py -- export <out CharPipe dir>
Input: data/clips.json (sources with licences, the audition, the picks). Output: audit.json (per candidate, measured as sourced) and, for the picks,
CharPipe/Clips/<name>.fbx (the source file copied byte for byte) + .meta (Humanoid, avatar from the clip's own Mixamo rig, the clip's root motion kept) and
CharPipe/Clips/clips.rows.json. Nothing here edits a clip: a candidate that fails a check (loop doesn't close, planted foot slips, no forward travel for a
locomotion clip) is rejected, not repaired. Root motion is what keeps the feet from sliding: Unity normalises a Humanoid clip's root motion by the clip's own
avatar and plays it scaled by the figure's humanScale, so the figure's speed is clip averageSpeed x humanScale, for every body, with no table to tune."""
import bpy, json, os, sys, math, hashlib, shutil, importlib.util
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__)); CP = os.path.dirname(HERE); ROOT = os.path.dirname(os.path.dirname(os.path.dirname(CP)))
DATA = json.load(open(os.path.join(CP, 'data/clips.json')))
_sp = importlib.util.spec_from_file_location('unitymeta', os.path.join(ROOT, 'tools/figures/unitymeta.py')); um = importlib.util.module_from_spec(_sp); _sp.loader.exec_module(um)
FEET = ('LeftToeBase', 'RightToeBase')


def src_path(ref):
    kind, name = ref.split(':', 1); s = DATA['sources'][kind]
    p = os.path.expanduser(s['path']); p = p if os.path.isabs(p) else os.path.join(ROOT, p)
    return kind, (os.path.join(p, name) if kind == 'mixamo' else p), name


def load(ref):
    """the clip's armature with its action active, and a bone-name map to Mixamo names."""
    bpy.ops.wm.read_factory_settings(use_empty=True)
    kind, path, name = src_path(ref)
    bpy.ops.import_scene.fbx(filepath=path)
    arm = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
    if kind == 'ual':
        act = bpy.data.actions[[a.name for a in bpy.data.actions if a.name.split('|')[-1] == name][0]]
        arm.animation_data.action = act
        if hasattr(arm.animation_data, 'action_slot') and arm.animation_data.action_slot is None and len(act.slots): arm.animation_data.action_slot = act.slots[0]
        m = {'Hips': 'DEF-hips', 'LeftToeBase': 'DEF-toe.L', 'RightToeBase': 'DEF-toe.R', 'LeftFoot': 'DEF-foot.L', 'RightFoot': 'DEF-foot.R', 'Head': 'DEF-head',
             'LeftUpLeg': 'DEF-thigh.L', 'RightUpLeg': 'DEF-thigh.R', 'LeftLeg': 'DEF-shin.L', 'RightLeg': 'DEF-shin.R', 'Neck': 'DEF-neck'}
    else:
        act = arm.animation_data.action
        m = {n: 'mixamorig:' + n for n in ('Hips', 'LeftToeBase', 'RightToeBase', 'LeftFoot', 'RightFoot', 'Head', 'Neck', 'LeftUpLeg', 'RightUpLeg', 'LeftLeg', 'RightLeg')}
    return arm, act, m, path


def world(arm, bone): return arm.matrix_world @ arm.pose.bones[bone].head


def leg_length(arm, m):
    """thigh + shin in the rest pose, both legs averaged: a Humanoid retarget copies joint angles, so a stride scales with the leg's own length (not the
    hip height: MPFB's hips sit higher over the hip joints than Mixamo's, which made hip height read 11-19 % off). pipeline/figure measures the same."""
    W = arm.matrix_world; B = arm.data.bones; t = 0.0
    for s in ('Left', 'Right'):
        up, kn, an = (W @ B[m[s + 'UpLeg']].head_local, W @ B[m[s + 'Leg']].head_local, W @ B[m[s + 'Foot']].head_local)
        t += (kn - up).length + (an - kn).length
    return t / 2


def audit_one(ref):
    arm, act, m, path = load(ref)
    sc = bpy.context.scene; fps = sc.render.fps / sc.render.fps_base
    f0, f1 = int(round(act.frame_range[0])), int(round(act.frame_range[1]))
    frames = list(range(f0, f1 + 1))
    hips, feet, rots, leans = [], {f: [] for f in FEET}, [], []
    for fr in frames:
        sc.frame_set(fr)
        hips.append(world(arm, m['Hips']).copy())
        nk = world(arm, m['Neck']) - world(arm, m['Hips']); leans.append(math.degrees(math.atan2(math.hypot(nk.x, nk.y), nk.z)))
        for f in FEET: feet[f].append(world(arm, m[f]).copy())
        if fr in (f0, f1): rots.append({b.name: b.matrix_basis.to_quaternion().copy() for b in arm.pose.bones})
    dur = (f1 - f0) / fps
    travel = Vector((hips[-1].x - hips[0].x, hips[-1].y - hips[0].y))
    rest_hip = leg_length(arm, m)
    height = hips[0].z - min(min(p.z for p in feet[f]) for f in FEET)       # hip height over the soles, for the check's scale
    # loop: every bone's local turn, first frame against last (the root's own travel left out)
    close = max(math.degrees(rots[0][n].rotation_difference(rots[1][n]).angle) for n in rots[0] if n != m['Hips'])
    close = max(close, math.degrees(rots[0][m['Hips']].rotation_difference(rots[1][m['Hips']]).angle))
    hips_dz = abs(hips[-1].z - hips[0].z)
    # planted feet: frames where a foot is within contactBandM of its lowest. In place, a planted foot moves straight back at the walking speed: its median speed
    # is the stride (strideMs); how much that speed varies while the foot is down (CV) and how far it wanders sideways say whether it would skate.
    c = DATA['checks']; band = c['contactBandM']; v_all, side = [], []
    fwd = None
    for f in FEET:
        zmin = min(p.z for p in feet[f])
        for i in range(1, len(frames)):
            a, b = feet[f][i - 1], feet[f][i]
            if a.z <= zmin + band and b.z <= zmin + band:
                d = Vector((b.x - a.x, b.y - a.y)) * fps; v_all.append(d)
    if v_all:
        mean = sum(v_all, Vector((0, 0))) / len(v_all); fwd = mean.normalized() if mean.length > 1e-6 else Vector((0, 1))
        sp = sorted(v.dot(fwd) for v in v_all); med = sp[len(sp) // 2]
        cv = 100 * (sum((x - med) ** 2 for x in sp) / len(sp)) ** 0.5 / med if med > 1e-6 else 999.0
        lat = Vector((-fwd.y, fwd.x)); side_drift = max(abs(sum(v.dot(lat) for v in v_all[:k]) / fps) for k in range(1, len(v_all) + 1))
    else: med, cv, side_drift = 0.0, 999.0, 999.0
    r = {'ref': ref, 'file': os.path.basename(path), 'sha256': hashlib.sha256(open(path, 'rb').read()).hexdigest(), 'fps': fps, 'frames': [f0, f1],
         'seconds': round(dur, 3), 'hipsTravelM': round(travel.length, 3), 'hipHeightM': round(height, 3), 'legLengthM': round(rest_hip, 4),
         'loopCloseDeg': round(close, 2), 'loopHipsDzM': round(hips_dz, 4), 'strideMs': round(med, 3), 'stridePerLegM': round(med / rest_hip, 4) if rest_hip else None,
         'plantedSpeedCvPct': round(cv, 1), 'plantedSideDriftM': round(side_drift, 3), 'contactSamples': len(v_all),
         'cadenceHz': round(count_steps(feet, frames, fps, band) / dur, 2) if dur else 0,
         'hipBobM': round(max(p.z for p in hips) - min(p.z for p in hips), 3), 'trunkLeanDeg': round(sum(leans) / len(leans), 1)}
    fails = []
    if r['loopCloseDeg'] > c['loopCloseDeg']: fails.append(f"loop doesn't close: {r['loopCloseDeg']} deg")
    if r['loopHipsDzM'] > c['loopCloseHipsM']: fails.append(f"hips end {r['loopHipsDzM']} m off their start height")
    if r['hipsTravelM'] > c['inPlaceMaxTravelM']: fails.append(f"not in place: the hips travel {r['hipsTravelM']} m")
    if r['plantedSpeedCvPct'] > c['plantedSpeedCvPct']: fails.append(f"planted foot unsteady: speed varies {r['plantedSpeedCvPct']} % while down")
    if r['plantedSideDriftM'] > c['plantedSideDriftM']: fails.append(f"planted foot wanders sideways {r['plantedSideDriftM']} m")
    r['fails'] = fails; r['pass'] = not fails
    return r


def count_steps(feet, frames, fps, band):
    n = 0
    for f in FEET:
        zmin = min(p.z for p in feet[f]); down = [p.z <= zmin + band for p in feet[f]]
        n += sum(1 for i in range(1, len(down)) if down[i] and not down[i - 1])
    return n


def audit(out, gaits):
    rows = []
    for g in (gaits or sorted(DATA['audition'])):
        for ref in DATA['audition'][g]:
            try: r = audit_one(ref)
            except Exception as e: r = {'ref': ref, 'pass': False, 'fails': [f'could not read: {e}']}
            r['gait'] = g; rows.append(r); print('AUDIT', g, ref, 'PASS' if r['pass'] else 'FAIL', r.get('fails'))
    json.dump({'schema': 'suborned.charpipe.clip-audit/1', 'checks': DATA['checks'], 'rows': rows}, open(out, 'w'), indent=1)


REF_BONES = ['Spine', 'Spine1', 'Spine2', 'Neck', 'Head'] + [s + b for s in ('Left', 'Right') for b in ('Shoulder', 'Arm', 'ForeArm', 'Hand', 'UpLeg', 'Leg')] + \
            [s + 'Hand' + f + str(i) for s in ('Left', 'Right') for f in ('Thumb', 'Index', 'Middle', 'Ring', 'Pinky') for i in (1, 2, 3)]


def rest_dirs(ref):
    """the rig's rest direction per bone (world, the figure facing -Y, Z up), Mixamo names without the prefix."""
    arm, act, m, path = load(ref); W = arm.matrix_world; out = {}
    for n in REF_BONES:
        b = arm.data.bones.get('mixamorig:' + n)
        if b: d = ((W @ b.tail_local) - (W @ b.head_local)).normalized(); out[n] = [round(x, 6) for x in d]
    return out


def export(out):
    d = os.path.join(out, 'Clips'); os.makedirs(d, exist_ok=True); rows = []
    ref = rest_dirs(DATA['skeletonReference']['source'])
    json.dump({'schema': 'suborned.charpipe.reference-tpose/1', 'source': DATA['skeletonReference']['source'], 'directions': ref}, open(os.path.join(d, 'reference_tpose.json'), 'w'), indent=1, sort_keys=True)
    for name, c in sorted(DATA['clips'].items()):
        r = audit_one(c['source'])
        if not r['pass']: raise SystemExit(f"CLIP FAIL {name} ({c['source']}): {r['fails']}")
        kind, path, _ = src_path(c['source'])
        mine = rest_dirs(c['source']) if kind == 'mixamo' else {}
        off = max((math.degrees(Vector(mine[n]).angle(Vector(ref[n]))) for n in ref if n in mine), default=999.0)
        if off > DATA['skeletonReference']['checkDeg']: raise SystemExit(f'CLIP FAIL {name}: its rig rests {off:.1f} deg away from the skeleton reference')
        if kind != 'mixamo': raise SystemExit(f'CLIP FAIL {name}: only Mixamo-rigged sources go to Unity unchanged (its Humanoid maps their names unaided)')
        rel = f'Clips/{name}.fbx'; shutil.copyfile(path, os.path.join(out, rel))
        open(os.path.join(out, rel) + '.meta', 'w').write(um.clip_meta('CharPipe/' + rel, name, DATA['sources'][kind]['take'], r['frames'][0], r['frames'][1], c.get('loop', True), feet=True, bake_xz=False))
        rows.append({'name': name, 'file': 'CharPipe/' + rel, 'source': c['source'], 'licence': DATA['sources'][kind]['licence'], 'sha256': r['sha256'], 'loop': c.get('loop', True),
                     'seconds': r['seconds'], 'strideMs': r['strideMs'], 'sourceLegLengthM': r['legLengthM'],
                     'checks': {k: r[k] for k in ('loopCloseDeg', 'loopHipsDzM', 'plantedSpeedCvPct', 'plantedSideDriftM', 'cadenceHz')},
                     'speed': 'in place: move a figure at strideMs x (its legLengthM / sourceLegLengthM): the stride of a retarget that copies joint angles scales with the leg (thigh + shin, both measured on the rest rigs)'})
        print('CLIP', name, r['seconds'], r['strideMs'], r['plantedSpeedCvPct'])
    json.dump({'schema': 'suborned.charpipe.clips-rows/1', 'clips': rows}, open(os.path.join(d, 'clips.rows.json'), 'w'), indent=1, sort_keys=True)


a = sys.argv[sys.argv.index('--') + 1:]
if a[0] == 'audit': audit(a[1], a[2:])
elif a[0] == 'export': export(a[1])
