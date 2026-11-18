"""The cell from Happy Mouse Labs' game bundles (brief 112; Sky, 2026-11-12: "replace our main cell with these characters and animations").
  blender -b --python-use-system-env -P tools/figures/cell_hml.py -- <cell_hml.json> <bundles dir> <out root> <id> [id ...]        ids: scout fixer tech enforcer
Run it through `bun tools/figures/cell_hml.ts`, which unzips the bundles, sets PYTHONHASHSEED=0 (the same FBX bytes every build), writes the manifest and installs.
<bundles dir>/<id>/ is the unzipped bundle (manifest.json + assets/.../model.glb). Writes <out root>/Figures/<id>/<id>.fbx (+ .meta, Materials, Textures) and <out root>/.cell.<id>.json.

Each figure is exported as the bundle has it: its own mesh (no decimation, no weld), its own textures (the glb's image bytes, no resize), its own skeleton (Generic in Unity,
no Humanoid retarget) and its own clips. What changes: the rig is baked to metres at object scale 1 (the bundle nests it under scaled empties), the Tech's bones lose their
`mixamorig` prefix (the host finds Head, Hips, RightShoulder and RightHand by name), each prop (pistol, carbine, visor) becomes one mesh skinned rigidly to its socket's bone
(named prop_<name>; the host shows it by state), and the clips are named after the shared controller's states (cell_hml.json) so FigureBuild.CellOverrides can swap them in.
The glTF metallic-roughness map is repacked as URP Lit's mask (R metallic, G occlusion, A smoothness); albedo and normal are copied byte for byte.
Authored here, from the figure's own clips: carry_shoulder and carry_shoulder_idle (its walk and idle with the right arm round the carried legs) and the seat offset of the sit clips.
"""
import sys, os, json, glob, hashlib, struct, re
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
import numpy as np
from mathutils import Vector, Matrix
from figlib import clean, load, tris, world_bounds
import unitymeta as um
import cell_hml_clips as cc

a = sys.argv[sys.argv.index('--') + 1:]
SPEC = json.load(open(a[0])); BUNDLES = os.path.abspath(a[1]); OUT = os.path.abspath(a[2]); IDS = a[3:]
FPS = 30; I4 = Matrix.Identity(4)
sha = lambda b: hashlib.sha256(b).hexdigest()
slug = lambda s: re.sub(r'[^a-z0-9]+', '_', s.lower()).strip('_')


# ------------------------------------------------------------------ the glb's own json and images (no re-encode)

def glb_parts(path):
    with open(path, 'rb') as f:
        f.read(12); n, _ = struct.unpack('<I4s', f.read(8)); js = json.loads(f.read(n)); n2, _ = struct.unpack('<I4s', f.read(8)); blob = f.read(n2)
    return js, blob


def image_bytes(js, blob, i):
    bv = js['bufferViews'][js['images'][i]['bufferView']]; o = bv.get('byteOffset', 0)
    return blob[o:o + bv['byteLength']]


def write_tex(rel, data, kind):
    """An image exactly as the glb holds it. Returns (rel with extension, its size in px)."""
    rel = f"{rel}.{'png' if data[:4] == bytes([0x89]) + b'PNG' else 'jpg'}"; dst = os.path.join(OUT, rel); os.makedirs(os.path.dirname(dst), exist_ok=True)
    open(dst, 'wb').write(data)
    img = bpy.data.images.load(dst, check_existing=False); px = max(img.size); bpy.data.images.remove(img)
    open(dst + '.meta', 'w').write(um.tex_meta(rel, kind, px))
    return rel, px


def write_mask(rel, mr_path, rough, metal, occlusion):
    """glTF metallic-roughness (G roughness, B metallic; R occlusion when the material says so) -> URP Lit's mask: R metallic, G occlusion, A smoothness. Same size, PNG."""
    src = bpy.data.images.load(mr_path, check_existing=False); src.colorspace_settings.name = 'Non-Color'
    w, h = src.size; p = np.empty(w * h * 4, dtype=np.float32); src.pixels.foreach_get(p); p = p.reshape(-1, 4); bpy.data.images.remove(src)
    q = np.empty_like(p); q[:, 0] = np.clip(p[:, 2] * metal, 0, 1); q[:, 1] = p[:, 0] if occlusion else 1.0; q[:, 2] = 0.0; q[:, 3] = np.clip(1.0 - p[:, 1] * rough, 0, 1)
    out = bpy.data.images.new('mask', w, h, alpha=True); out.colorspace_settings.name = 'Non-Color'; out.alpha_mode = 'STRAIGHT'; out.pixels.foreach_set(q.ravel())
    rel += '.png'; dst = os.path.join(OUT, rel); out.filepath_raw = dst; out.file_format = 'PNG'; out.save(); bpy.data.images.remove(out)
    open(dst + '.meta', 'w').write(um.tex_meta(rel, 'data', max(w, h)))
    return rel, max(w, h), round(float(q[:, 3].mean()), 3), round(float(q[:, 0].mean()), 3)


def srgb(c): return c * 12.92 if c <= 0.0031308 else 1.055 * c ** (1 / 2.4) - 0.055


def materials(fid, js, blob, key_of):
    """One URP Lit .mat per glTF material in use. key_of: glTF material index -> material name in Unity (and in the FBX). Returns {name: rel}, manifest rows."""
    fdir = f'Figures/{fid}'; mats = {}; rows = []; done = {}
    def tex(i, key, suffix, kind):
        if i is None: return None
        img = js['textures'][i['index']]['source']
        if (img, suffix) not in done: done[(img, suffix)] = write_tex(f'{fdir}/Textures/{key}_{suffix}', image_bytes(js, blob, img), kind)
        return done[(img, suffix)]
    for gi, key in sorted(key_of.items()):
        m = js['materials'][gi]; pbr = m.get('pbrMetallicRoughness', {}); tx = {}; px = {}
        for suffix, kind, ref in (('albedo', 'albedo', pbr.get('baseColorTexture')), ('normal', 'normal', m.get('normalTexture'))):
            t = tex(ref, key, suffix, kind)
            if t: tx[suffix], px[suffix] = t
        rough = pbr.get('roughnessFactor', 1.0); metal = pbr.get('metallicFactor', 1.0); mask = None; smooth = round(1.0 - rough, 3)
        if 'metallicRoughnessTexture' in pbr:
            img = js['textures'][pbr['metallicRoughnessTexture']['index']]['source']; occ = m.get('occlusionTexture')
            same = occ is not None and js['textures'][occ['index']]['source'] == img
            tmp = os.path.join(OUT, fdir, 'Textures', f'.{key}_mr.tmp'); os.makedirs(os.path.dirname(tmp), exist_ok=True); open(tmp, 'wb').write(image_bytes(js, blob, img))
            mask = write_mask(f'{fdir}/Textures/{key}_mask', tmp, rough, metal, same); os.remove(tmp)
            tx['mask'], px['mask'] = mask[0], mask[1]; smooth = 1.0   # the map's alpha is the smoothness; _Smoothness scales it
        bc = pbr.get('baseColorFactor', [1, 1, 1, 1]); tint = tuple(round(srgb(c), 4) for c in bc[:3])
        text = um.mat_text(key, base=tx.get('albedo'), normal=tx.get('normal'), metal=tx.get('mask'), smoothness=smooth, tint=tint)
        if m.get('doubleSided'): text = text.replace('    - _Cull: 2\n', '    - _Cull: 0\n')   # the coat's lining, the hems: drawn from both sides, as the bundle has them
        mrel = f'{fdir}/Materials/{key}.mat'; os.makedirs(os.path.join(OUT, fdir, 'Materials'), exist_ok=True)
        open(os.path.join(OUT, mrel), 'w').write(text); open(os.path.join(OUT, mrel + '.meta'), 'w').write(um.mat_meta(mrel))
        mats[key] = mrel
        rows.append({'name': key, 'file': mrel, 'source': m.get('name'), 'doubleSided': bool(m.get('doubleSided')), 'tint': tint, 'textures': tx, 'texturePx': px,
                     'smoothness': {'mean': mask[2], 'from': 'map'} if mask else {'mean': smooth, 'from': 'factor'}, 'metallicMean': mask[3] if mask else 0.0})
    return mats, rows


# ------------------------------------------------------------------ the rig, baked to metres at scale 1

def socket_bone(o):
    while o and o.parent_type != 'BONE': o = o.parent
    return o.parent_bone if o else None


def flatten(arm, skinned, props):
    """World transforms into the data: the armature (bones, and every location key by its scale), the skinned meshes, and each prop as one mesh skinned to its bone."""
    ad = arm.animation_data
    for tr in list(ad.nla_tracks): ad.nla_tracks.remove(tr)
    for act in bpy.data.actions: act.use_fake_user = True
    ad.action = None; arm.data.pose_position = 'REST'; bpy.context.view_layer.update()
    A = arm.matrix_world.copy(); s = A.to_scale().x
    held = {}
    for name, (meshes, bone) in props.items():
        for m in meshes:
            W = m.matrix_world.copy(); m.parent = None; m.data.transform(W); m.matrix_world = I4
            if W.determinant() < 0: m.data.flip_normals()
        bpy.ops.object.select_all(action='DESELECT')
        for m in meshes: m.select_set(True)
        bpy.context.view_layer.objects.active = meshes[0]
        if len(meshes) > 1: bpy.ops.object.join()
        o = meshes[0]; o.name = o.data.name = f'prop_{name}'; held[name] = (o, bone)
    for m in skinned:
        W = m.matrix_world.copy(); m.parent = None; m.data.transform(W); m.matrix_world = I4
        if m.data.shape_keys: m.shape_key_clear()   # the Scout's jacket shapes: only the bundle's fit tests key them, no game action does
    arm.parent = None; arm.matrix_world = A
    bpy.ops.object.select_all(action='DESELECT'); arm.select_set(True); bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='EDIT')
    for eb in arm.data.edit_bones: eb.transform(A)
    bpy.ops.object.mode_set(mode='OBJECT')
    arm.matrix_world = I4
    for act in bpy.data.actions:
        for fc in cc.fcurves(act):
            if fc.data_path.endswith('.location'):
                for kp in fc.keyframe_points: kp.co.y *= s; kp.handle_left.y *= s; kp.handle_right.y *= s
                fc.update()
    for m in skinned + [o for o, _ in held.values()]:
        m.parent = arm; m.matrix_parent_inverse = I4
    for m in skinned:
        for md in m.modifiers:
            if md.type == 'ARMATURE': md.object = arm
    for name, (o, bone) in held.items():
        for vg in list(o.vertex_groups): o.vertex_groups.remove(vg)
        o.vertex_groups.new(name=bone).add(list(range(len(o.data.vertices))), 1.0, 'REPLACE')
        md = o.modifiers.new('Armature', 'ARMATURE'); md.object = arm
    arm.data.pose_position = 'POSE'; bpy.context.view_layer.update()
    return s, {n: o for n, (o, _) in held.items()}


def generic_meta(rel, mats, clips, exposed):
    """A Generic rig's model meta: each clip a frame range of one of the FBX's takes (two states may share a take), looping where the bundle says so.
    Optimize Game Objects is on with `exposed` kept as transforms (what FigureBuild.Optimise would set on the cell: Head, Hips, RightShoulder), so the figure imports once."""
    body = ''
    for nm, take, n, loop in clips:
        body += f"""
    - serializedVersion: 19
      name: {nm}
      takeName: {take}
      internalID: 0
      firstFrame: 0
      lastFrame: {n}
      wrapMode: 0
      orientationOffsetY: 0
      level: 0
      cycleOffset: 0
      loop: 0
      hasAdditiveReferencePose: 0
      loopTime: {1 if loop else 0}
      loopBlend: 0
      loopBlendOrientation: 0
      loopBlendPositionY: 0
      loopBlendPositionXZ: 0
      keepOriginalOrientation: 1
      keepOriginalPositionY: 1
      keepOriginalPositionXZ: 1
      heightFromFeet: 0
      mirror: 0
      bodyMask: 01000000010000000100000001000000010000000100000001000000010000000100000001000000010000000100000001000000010000000100000001000000
      curves: []
      events: []
      transformMask: []
      maskType: 0
      maskSource: {{instanceID: 0}}
      additiveReferencePoseFrame: 0"""
    ext = ''
    for n, r in sorted(mats.items()):
        ext += f"  - first:\n      type: UnityEngine:Material\n      assembly: UnityEngine.CoreModule\n      name: {n}\n    second: {{fileID: 2100000, guid: {um.guid(r)}, type: 2}}\n"
    t = um._MODEL_HEAD.format(guid=um.guid(rel), externals=ext, clips=body, import_anim=1, avatar_setup=1).replace('animationType: 3', 'animationType: 2')
    return t.replace('optimizeGameObjects: 0', 'optimizeGameObjects: 1').replace('extraExposedTransformPaths: []', 'extraExposedTransformPaths:' + ''.join(f'\n    - {x}' for x in sorted(exposed)))


# ------------------------------------------------------------------ one figure

def build(fid):
    spec = SPEC['figures'][fid]; clean(); sc = bpy.context.scene; sc.render.fps = FPS
    root = f'{BUNDLES}/{fid}'; man = json.load(open(f'{root}/manifest.json'))
    asset = [x for x in man['assets'] if 'gamePrep' in x][0]; actions = {x['name']: x for x in asset['gamePrep']['actions']}
    glb = glob.glob(f'{root}/assets/*/models/rig-exports/*/model.glb')[0]; js, blob = glb_parts(glb)
    prop_names = {p['id']: SPEC['props'][p['name']] for p in json.load(open(glb + '.attachments.json'))['document']['props']}
    new = load(glb); arm = [o for o in new if o.type == 'ARMATURE'][0]; arm.name = 'Armature'
    for b in arm.data.bones:
        if b.name.startswith('mixamorig'): b.name = b.name[len('mixamorig'):].lstrip(':')
    skinned = [o for o in new if o.type == 'MESH' and any(m.type == 'ARMATURE' for m in o.modifiers)]
    props = {}
    for o in new:
        pid = o.get('hmlPropId')
        if pid: props[prop_names[pid]] = ([c for c in o.children_recursive if c.type == 'MESH'], socket_bone(o))
    # glTF material index of every Blender material in use (the importer keeps the glTF names; a duplicate gets .001)
    gl_names = [m.get('name') for m in js['materials']]; key_of = {}; label = {}
    def gl_index(mat):
        n = mat.name if mat.name in gl_names else re.sub(r'\.\d{3}$', '', mat.name)
        return gl_names.index(n)
    for name, (meshes, _) in props.items():
        for m in meshes:
            for slot in m.material_slots: label[gl_index(slot.material)] = name
    body = [gl_index(s.material) for m in sorted(skinned, key=lambda o: -len(o.data.polygons)) for s in m.material_slots]
    for gi in dict.fromkeys(body): label.setdefault(gi, 'body' if gi == body[0] else f'part{list(dict.fromkeys(body)).index(gi)}')
    key_of = {gi: f'{fid}_{lb}' for gi, lb in label.items()}
    for o in skinned + [m for ms, _ in props.values() for m in ms]:
        for slot in o.material_slots:
            k = key_of[gl_index(slot.material)]; slot.material = bpy.data.materials.get(k) or bpy.data.materials.new(k)
    mats, mat_rows = materials(fid, js, blob, key_of)
    scale, held = flatten(arm, skinned, props)
    keep = {arm, *skinned, *held.values()}
    for o in [o for o in bpy.data.objects if o not in keep]: bpy.data.objects.remove(o, do_unlink=True)   # the bundle's scale and socket empties
    names = {}
    for m in skinned: names[m] = f'{fid}_LOD0' if len(skinned) == 1 else f'{fid}_{slug(m.name)[:28]}_LOD0'
    for m, n in names.items(): m.name = m.data.name = n
    arm.data.pose_position = 'REST'; bpy.context.view_layer.update(); mn, mx = world_bounds(skinned); arm.data.pose_position = 'POSE'
    # ---- clips: one take per bundle action in use, the states as ranges of them
    want = dict(SPEC['states']); want.update(SPEC['extras']); want.update(SPEC['byFigure'].get(fid, {}))
    takes = {}; clips = []; rows = []; missing = []
    for state, aname in want.items():
        if isinstance(aname, dict):   # a clip of the bundle that is not one of its game actions: {"clip": name, "loop": bool}
            act_row = {'clip': aname['clip'], 'review': {'loop': aname.get('loop', True)}, 'propIds': []}; tk = 'clip_' + slug(aname['clip']); aname = None
        else:
            act_row = actions.get(aname); tk = slug(aname) if state not in SPEC['seconds'] else state
        if not act_row: missing.append(state); continue
        if tk not in takes: takes[tk] = cc.prep(bpy.data.actions[act_row['clip']], 'take_' + tk, FPS, SPEC['seconds'].get(state))
        act, n = takes[tk]; loop = bool(act_row.get('review', {}).get('loop')) and state not in SPEC['hold']
        clips.append((state, act.name, n, loop))
        rows.append({'name': state, 'action': aname, 'clip': act_row['clip'], 'take': tk, 'frames': n, 'fps': FPS, 'loop': loop,
                     'props': sorted(prop_names[p] for p in act_row.get('propIds', []))})
    by_state = {r['name']: r for r in rows}; rest = cc.soles_rest(arm)
    # feet on the floor: every standing take, before anything is authored from it (the lying, seated and car takes keep the bundle's height)
    off = {by_state[s]['take'] for s in SPEC['groundSkip'] if s in by_state}; lifts = {}
    for tk, (act, n) in takes.items():
        if tk in off: continue
        lift = cc.ground(arm, act, n, rest)
        if lift: lifts[tk] = lift
    for state, au in SPEC['authored'].items():
        donor = by_state[au['donor']]; src, n = takes[donor['take']]
        act = cc.carry(arm, src, 'take_' + state, n); takes[state] = (act, n); clips.append((state, act.name, n, True))
        rows.append({'name': state, 'action': None, 'authoredFrom': donor['name'], 'take': state, 'frames': n, 'fps': FPS, 'loop': True, 'props': donor['props']})
    seat = SPEC['seat']; H = mx.z - mn.z; seat_log = {}
    target = Vector((0.0, -seat['hipJointsForward'], seat['cushionUp'] + seat['hipJointsAboveCushionAt175'] * H / 1.75))   # Blender: the figure faces -Y, so behind the chair's point is +Y
    for tk in dict.fromkeys(r['take'] for r in rows if r['name'] in seat['states']):
        states = [r['name'] for r in rows if r['take'] == tk]; act, n = takes[tk]
        at = 'last' if 'stand_to_sit' in states else 'first' if 'sit_to_stand' in states else 'mean'
        seat_log[tk] = cc.seat(arm, act, n, target, at, rest)
    speeds = {}
    for state, tk, n, loop in clips:
        if state in ('walk', 'walk_f', 'run', 'run_lean', 'walk_armed', 'run_armed', 'crouch_walk', 'carry_shoulder'): speeds[state] = cc.own_speed(arm, bpy.data.actions[tk], n, FPS)
    ground = cc.lowest(arm, skinned, takes[by_state['idle']['take']][0])
    for act in list(bpy.data.actions):
        if not act.name.startswith('take_'): bpy.data.actions.remove(act)
    # ---- export
    arm.animation_data.action = None
    bpy.ops.object.select_all(action='DESELECT'); arm.select_set(True)
    for o in skinned + list(held.values()): o.select_set(True)
    bpy.context.view_layer.objects.active = arm
    fbx_rel = f'Figures/{fid}/{fid}.fbx'; fbx = os.path.join(OUT, fbx_rel); cc.pin_fbx()
    bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True, object_types={'ARMATURE', 'MESH'}, use_mesh_modifiers=False, add_leaf_bones=False, bake_anim=True,
                             bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False, bake_anim_force_startend_keying=False, bake_anim_simplify_factor=0.0, bake_anim_step=1.0,
                             axis_forward='-Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', bake_space_transform=False, primary_bone_axis='Y',
                             secondary_bone_axis='X', armature_nodetype='NULL', mesh_smooth_type='FACE', use_tspace=False, path_mode='STRIP', embed_textures=False)
    stacks = {t.split('|')[-1]: t for t in cc.fbx_takes(fbx)}   # Blender names a take '<object>|<action>'
    lost = sorted({c[1] for c in clips} - set(stacks))
    if lost: raise RuntimeError(f'{fid}: takes missing from the FBX: {lost}')
    def path(name):
        b = arm.data.bones[name]; parts = []
        while b: parts.append(b.name); b = b.parent
        return 'Armature/' + '/'.join(reversed(parts))
    exposed = [path(n) for n in ('Head', 'Hips', 'RightShoulder')]   # the bones the host reads (HostViews.Bone): the attention cue and look-at, a carried body's seat
    open(fbx + '.meta', 'w').write(generic_meta(fbx_rel, mats, [(nm, stacks[tk], n, loop) for nm, tk, n, loop in clips], exposed))
    prop_rows = [{'name': n, 'node': o.name, 'bone': props[n][1], 'tris': tris(o), 'always': all(n in r['props'] for r in rows),
                  'states': sorted(r['name'] for r in rows if n in r['props'])} for n, o in sorted(held.items())]
    row = {'id': fid, 'role': fid, 'label': spec['label'], 'fbx': fbx_rel, 'sha256': sha(open(fbx, 'rb').read()),
           'bundle': {'file': spec['bundle'], 'sha256': spec['sha256'], 'glb': os.path.relpath(glb, root), 'glbSha256': sha(open(glb, 'rb').read()), 'rigVersionId': asset['gamePrep'].get('rigVersionId')},
           'rig': {'type': 'generic', 'bones': len(arm.data.bones), 'root': 'Armature/' + [b.name for b in arm.data.bones if not b.parent][0], 'bakedScale': round(scale, 4), 'exposed': exposed},
           'heightM': round(H, 3), 'restBounds': [[round(x, 3) for x in mn], [round(x, 3) for x in mx]], 'idleGroundM': ground, 'female': spec['female'],
           'lods': [{'name': n, 'tris': tris(m)} for m, n in names.items()], 'props': prop_rows, 'materials': mat_rows, 'clips': rows, 'clipSpeedsMs': speeds,
           'groundLiftM': lifts, 'seat': {'hipJointsTarget': [round(x, 3) for x in target], 'takes': seat_log}, 'missing': missing, 'lodScreenHeights': [0.012]}
    json.dump(row, open(os.path.join(OUT, f'.cell.{fid}.json'), 'w'), indent=1)
    print('DONE', fid, 'height', row['heightM'], 'tris', sum(l['tris'] for l in row['lods']), 'clips', len(clips), 'takes', len(takes), 'speeds', speeds, 'ground', ground, 'missing', missing)


for fid in IDS:
    print('BUILD', fid); build(fid)
