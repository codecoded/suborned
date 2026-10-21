"""The cell as the old web testbed had it (brief 020, Forma, 2026-10-20; Sky: "the character models from testbed - because the animations on our ones are terrible").
  blender -b -P tools/figures/cell_generic.py -- <out root> <id> [id ...]        ids: scout tech enforcer fixer
Writes <out root>/Figures/<id>/<id>.fbx (+ .meta, Materials, Textures) and <out root>/.cell.<id>.json (manifest rows). Default out root: out/people-models/CellGeneric.

scout, tech, enforcer: the testbed's own models on their OWN skeletons, imported as GENERIC (no Humanoid avatar, no Unity retarget). Source = the uncompressed originals the
testbed's web glbs were made from (Citizens3D/rigged/scout_lod, tech_lod: LOD0 30k, LOD1 12k, LOD2 4.5k; characters/enforcer_G2/07_export/enforcer_G2.2k.glb: one 28.8k LOD, 11 clips),
so no mesh is decimated, welded, merged or re-skinned and no texture is downscaled or re-encoded (the image bytes are copied from the glb). The rig keeps its helper bones
(KneeHelper, headfront). The glb's own `walk` and `run` (and the enforcer's `idle`) are the clips played; they are re-timed to 30 fps and the repeated end frame is dropped.
Every other state the controller has (idle, crouch, carried, armed, knockout, ...) is the library clip retargeted ONCE here onto the figure's skeleton, so what Blender shows is
what Unity plays: world-space rotation change from the library rest pose, after swinging the figure's rest bone directions onto the library's (swing only; this is what makes an
A-pose rig agree with the library's T-pose, the thing Unity's Humanoid retarget got wrong), hips travel scaled by hip height. KneeHelper takes half the knee's rotation (as the testbed's own walk has it).
fixer: her existing P4-6 model and skeleton (out/people-models/People/Figures/fixer) re-exported as Generic with the library clips retargeted the same way.
"""
import sys, os, json, math, hashlib
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
from mathutils import Vector, Matrix, Quaternion
from figlib import clean, load, rigs, world_bounds, bake_to_meters, tris, limit_weights
import unitymeta as um

HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.normpath(os.path.join(HERE, '..', '..'))
STORE = os.path.expanduser(os.environ.get('SUBORNED_STORE', '~/Projects/the-suborned-store'))
PEOPLE = os.path.join(ROOT, 'out/people-models/People')
FPS = 30; H = 1.75
a = sys.argv[sys.argv.index('--') + 1:]; OUT = os.path.abspath(a[0]); IDS = a[1:]
CLIPS = {r['name']: r for r in json.load(open(f'{PEOPLE}/Clips/clips.rows.json'))}
SPEC = {c['name']: c for c in json.load(open(f'{HERE}/clips.json'))['clips']}

FIGS = {
    'scout': dict(label='Scout S1-2, the testbed model on its own rig (hooded jacket, shorts, harness)', role='scout', female=True,
                  lods=['source-art/Citizens3D/rigged/scout_lod/scout_S1-2_LOD0_30k.glb', 'source-art/Citizens3D/rigged/scout_lod/scout_S1-2_LOD1_12k.glb', 'source-art/Citizens3D/rigged/scout_lod/scout_S1-2_LOD2_4k5.glb'],
                  alias={'walk': ['walk', 'walk_f'], 'run': ['run', 'run_lean']}),
    'tech': dict(label='Tech F2-1, the testbed model on its own rig (vest, visor, cargo skirt-shorts)', role='tech', female=False,
                 lods=['source-art/Citizens3D/rigged/tech_lod/tech_F2-1_LOD0_30k.glb', 'source-art/Citizens3D/rigged/tech_lod/tech_F2-1_LOD1_12k.glb', 'source-art/Citizens3D/rigged/tech_lod/tech_F2-1_LOD2_4k5.glb'],
                 alias={'walk': ['walk', 'walk_f'], 'run': ['run', 'run_lean']}),
    'enforcer': dict(label='Enforcer G2, the testbed model on its own 65-bone rig (white coat, black suit, gold trim); one 28.8k LOD, as the testbed had it', role='enforcer', female=False,
                     lods=['characters/enforcer_G2/07_export/enforcer_G2.2k.glb'],
                     alias={'walk': ['walk'], 'run': ['run', 'run_lean'], 'idle': ['idle']}, extra_prefix='tb_'),
    'fixer': dict(label='Fixer P4-6 (her model and skeleton, re-exported Generic)', role='fixer', female=True, existing='Figures/fixer/fixer.fbx', alias={}),
}
MIX2OWN = {'Spine': 'Spine02', 'Spine1': 'Spine01', 'Spine2': 'Spine', 'Neck': 'neck', 'HeadTop_End': 'head_end'}   # the Meshy-style rig's spine names run the other way
HALF = {'LeftKneeHelper': 'LeftLeg', 'RightKneeHelper': 'RightLeg'}
sha = lambda b: hashlib.sha256(b).hexdigest()


# ------------------------------------------------------------------ actions

def fcurves(act):
    for l in act.layers:
        for s in l.strips:
            for cb in s.channelbags:
                for fc in cb.fcurves: yield fc


def depth(b): return 0 if b.parent is None else 1 + depth(b.parent)


def new_action(arm, name):
    act = bpy.data.actions.new(name); arm.animation_data_create(); arm.animation_data.action = act
    for pb in arm.pose.bones: pb.rotation_mode = 'QUATERNION'
    return act


def key(pb, frame, loc, q, with_loc):
    pb.rotation_quaternion = q
    pb.keyframe_insert('rotation_quaternion', frame=frame)
    if with_loc:
        pb.location = loc; pb.keyframe_insert('location', frame=frame)


def resample_own(arm, src_act, out_name, loop, kloc):
    """The glb's own action at 30 fps from frame 1; a looping clip drops its repeated end frame. kloc scales the location keys (rig units -> metres)."""
    sc = bpy.context.scene; f0, f1 = src_act.frame_range; sfps = 24.0
    span = f1 - f0; N = int(round(span / sfps * FPS)) + (0 if loop else 1)
    act = new_action(arm, out_name); tmp = {}
    for i in range(N):
        t = (i / N * span) if loop else (i / (N - 1) * span)
        arm.animation_data.action = src_act; fr = f0 + t
        sc.frame_set(int(math.floor(fr)), subframe=fr - math.floor(fr)); bpy.context.view_layer.update()
        tmp[i] = {pb.name: (pb.location.copy() * kloc, pb.rotation_quaternion.copy()) for pb in arm.pose.bones}
    arm.animation_data.action = act
    for i in range(N):
        for pb in arm.pose.bones:
            loc, q = tmp[i][pb.name]; key(pb, 1 + i, loc, q, with_loc=(pb.parent is None) or loc.length > 1e-6)
    act.frame_range = (1, N)
    return act, N


# ------------------------------------------------------------------ retarget of a library clip

class Retargeter:
    """Library clip -> `dst` armature, by bone name (own spine names mapped, helpers derived)."""
    def __init__(self, dst):
        self.dst = dst; DW = dst.matrix_world; self.DW = DW
        self.bones = {b.name: b for b in dst.data.bones}
        self.rest_a = {n: b.matrix_local.copy() for n, b in self.bones.items()}
        self.rest_w = {n: DW @ m for n, m in self.rest_a.items()}
        self.meshy = 'Spine02' in self.bones
        self.own2mix = {v: k for k, v in MIX2OWN.items()} if self.meshy else {}

    def mix(self, n): return self.own2mix.get(n, n)

    def run(self, clip_path, name, loop):
        sc = bpy.context.scene
        before = set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=clip_path)
        new = [o for o in bpy.data.objects if o not in before]
        carm = [o for o in new if o.type == 'ARMATURE'][0]
        cact = carm.animation_data.action; f0, f1 = int(cact.frame_range[0]), int(cact.frame_range[1]); N = f1 - f0 + 1
        cb = {b.name.split(':')[-1]: b for b in carm.data.bones}; cp = {k.split(':')[-1]: k for k in carm.pose.bones.keys()}
        dst, DW = self.dst, self.DW
        names = [n for n in sorted(self.bones, key=lambda n: depth(self.bones[n])) if self.mix(n) in cb and n not in HALF]
        helpers = [n for n in self.bones if n in HALF or n not in names]
        CW = carm.matrix_world
        def facing(M, bones, a, b):
            d = M.to_3x3() @ (bones[b].head_local - bones[a].head_local); d.z = 0; return d.normalized()
        fc = facing(CW, cb, 'LeftFoot', 'LeftToeBase'); fd = facing(DW, self.bones, self.mix_to_own('LeftFoot'), self.mix_to_own('LeftToeBase'))
        R = fc.rotation_difference(fd).to_matrix(); R4 = R.to_4x4()
        c_rest = {n: R4 @ (CW @ cb[self.mix(n)].matrix_local) for n in names}
        par = {n: (self.bones[n].parent.name if self.bones[n].parent and self.bones[n].parent.name in names else None) for n in names}
        kids = {n: [k for k in names if par[k] == n] for n in names}
        Cn = {}
        for n in names:
            if kids[n]:
                k = max(kids[n], key=lambda x: (self.rest_w[x].translation - self.rest_w[n].translation).length)
                df = (c_rest[k].translation - c_rest[n].translation).normalized(); dd = (self.rest_w[k].translation - self.rest_w[n].translation).normalized()
                # swing the figure's rest bone onto the clip's rest direction
                Cn[n] = dd.rotation_difference(df).to_matrix()
            else: Cn[n] = Cn[par[n]] if par[n] else Matrix.Identity(3)
        ratio = self.rest_w['Hips'].translation.z / c_rest['Hips'].translation.z
        sc.render.fps = FPS
        act = new_action(dst, name); DWi = DW.inverted(); poses = {}
        order = names
        samples = []
        for i in range(N):
            sc.frame_set(f0 + i); bpy.context.view_layer.update()
            samples.append({n: R4 @ (CW @ carm.pose.bones[cp[self.mix(n)]].matrix) for n in names})
        for i, S in enumerate(samples):
            pose = {}
            for n in order:
                D = S[n].to_3x3() @ c_rest[n].to_3x3().inverted()
                rot_a = DWi.to_3x3() @ (D @ Cn[n] @ self.rest_w[n].to_3x3())
                p = par[n]
                if p is None:
                    dw = S[n].translation - c_rest[n].translation
                    head_a = DWi @ (self.rest_w[n].translation + dw * ratio)
                else:
                    head_a = (pose[p] @ (self.rest_a[p].inverted() @ self.rest_a[n])).translation
                M = Matrix.Translation(head_a) @ rot_a.to_4x4(); pose[n] = M
                basis = self.rest_a[n].inverted() @ M if p is None else (self.rest_a[p].inverted() @ self.rest_a[n]).inverted() @ pose[p].inverted() @ M
                loc, q, _ = basis.decompose(); pb = dst.pose.bones[n]
                key(pb, 1 + i, loc, q, with_loc=(p is None))
            for h in helpers:
                pb = dst.pose.bones[h]
                if h in HALF:
                    leg = dst.pose.bones[HALF[h]].rotation_quaternion
                    q = Quaternion().slerp(leg, 0.5)
                else: q = Quaternion()
                key(pb, 1 + i, Vector(), q, with_loc=False)
        act.frame_range = (1, N)
        for o in new: bpy.data.objects.remove(o, do_unlink=True)
        bpy.data.actions.remove(cact)
        return act, N

    def mix_to_own(self, n): return MIX2OWN.get(n, n) if self.meshy else n


def own_speed(arm, act, N):
    """Natural ground speed (m/s) of a loco clip: the planted foot's median speed in mid-stance (foot within 2 cm of its lowest; clips.py uses 4 cm, which under-reads runs with few stance frames)."""
    sc = bpy.context.scene; arm.animation_data.action = act
    P = {s: [] for s in ('Left', 'Right')}
    for i in range(N):
        sc.frame_set(1 + i); bpy.context.view_layer.update()
        for s in P: P[s].append(arm.matrix_world @ arm.pose.bones[s + 'Foot'].matrix.translation)   # the clips are in place (as clips.py measures them)
    ground = min(p.z for s in P for p in P[s]); sp = []
    for s in P:
        for k in range(1, len(P[s])):
            a_, b_ = P[s][k - 1], P[s][k]
            if a_.z <= ground + 0.02 and b_.z <= ground + 0.02: sp.append(Vector((b_.x - a_.x, b_.y - a_.y, 0)).length * FPS)
    sp.sort(); return round(sp[len(sp) // 2], 2) if len(sp) > 3 else 0.0


# ------------------------------------------------------------------ materials

def upstream_image(sock):
    seen, stack = set(), [sock]
    while stack:
        s = stack.pop()
        for l in s.links:
            n = l.from_node
            if n.type == 'TEX_IMAGE' and n.image: return n.image
            if n.name in seen: continue
            seen.add(n.name); stack += [i for i in n.inputs if i.is_linked]
    return None


def maps_of(mat):
    pb = next(n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    out = {'albedo': upstream_image(pb.inputs['Base Color']), 'normal': upstream_image(pb.inputs['Normal']), 'emissive': upstream_image(pb.inputs['Emission Color'])}
    out['emission_strength'] = float(pb.inputs['Emission Strength'].default_value) if 'Emission Strength' in pb.inputs else 1.0
    out['blend'] = pb.inputs['Alpha'].is_linked or getattr(mat, 'blend_method', 'OPAQUE') in ('BLEND', 'HASHED')
    return out


def image_bytes(img):
    if img.packed_file: return bytes(img.packed_file.data)
    return open(bpy.path.abspath(img.filepath), 'rb').read()


def write_raw(rel, img, kind):
    """The image exactly as the glb holds it (no resize, no re-encode)."""
    b = image_bytes(img); ext = 'png' if b[:4] == b'\x89PNG' else 'jpg'
    rel = f'{rel}.{ext}'; dst = os.path.join(OUT, rel); os.makedirs(os.path.dirname(dst), exist_ok=True)
    open(dst, 'wb').write(b); sz = img.size[0]
    open(dst + '.meta', 'w').write(um.tex_meta(rel, kind, sz))
    return rel, sz


# ------------------------------------------------------------------ one figure

def build(fid, spec):
    clean(); fdir = f'Figures/{fid}'; mats = {}; mat_rows = []
    own_acts = {}
    kloc = 1.0
    if spec.get('existing'):
        bpy.ops.import_scene.fbx(filepath=f'{PEOPLE}/{spec["existing"]}')
        arm = [o for o in bpy.data.objects if o.type == 'ARMATURE'][0]
        objs = sorted([o for o in bpy.data.objects if o.type == 'MESH'], key=lambda o: o.name)
        # her materials and textures are the existing ones, copied as they are
        import shutil
        for sub in ('Materials', 'Textures'):
            d = os.path.join(OUT, fdir, sub); shutil.rmtree(d, ignore_errors=True); shutil.copytree(f'{PEOPLE}/{fdir}/{sub}', d)
        mats = {os.path.basename(p)[:-4]: f'{fdir}/Materials/{os.path.basename(p)}' for p in os.listdir(f'{PEOPLE}/{fdir}/Materials') for p in [p] if p.endswith('.mat')}
        lod_rows = [{'name': o.name, 'tris': tris(o), 'tex': None, 'source': 'unchanged from the Humanoid build'} for o in objs]
    else:
        src0 = os.path.join(STORE, spec['lods'][0])
        before = set(bpy.data.objects); bpy.ops.import_scene.gltf(filepath=src0)
        new = [o for o in bpy.data.objects if o not in before]
        arm = [o for o in new if o.type == 'ARMATURE'][0]; arm.name = 'Armature'
        for o in list(new):
            if o.type == 'MESH' and o.name.startswith('Icosphere'): bpy.data.objects.remove(o, do_unlink=True)
        m0 = [o for o in bpy.data.objects if o.type == 'MESH']
        ascale = arm.matrix_world.to_scale().x
        mn, mx = world_bounds(m0) if False else (None, None)
        arm.data.pose_position = 'REST'; bpy.context.view_layer.update(); mn, mx = world_bounds(m0); arm.data.pose_position = 'POSE'
        scale = H / (mx.z - mn.z); shift = Vector((0, 0, -mn.z * scale)); kloc = ascale * scale
        # the glb's own actions, kept (the source rig, in the source's units) before the rig is baked to metres
        srcacts = {a_.name: a_ for a_ in bpy.data.actions}
        for nm_, a_ in srcacts.items(): a_.name = 'src_' + nm_
        rest = {}
        arm.animation_data_create()
        # LOD0 mesh(es)
        arm.animation_data.action = None
        bake_to_meters(arm, m0, scale, shift)
        by_lod = {0: m0}
        for i, rel in enumerate(spec['lods'][1:], 1):
            b2 = set(bpy.data.objects); acts_before = set(bpy.data.actions); bpy.ops.import_scene.gltf(filepath=os.path.join(STORE, rel))
            n2 = [o for o in bpy.data.objects if o not in b2]
            a2 = [o for o in n2 if o.type == 'ARMATURE'][0]; ms = [o for o in n2 if o.type == 'MESH' and not o.name.startswith('Icosphere')]
            for o in [o for o in n2 if o.type == 'MESH' and o.name.startswith('Icosphere')]: bpy.data.objects.remove(o, do_unlink=True)
            T = Matrix.Translation(shift) @ Matrix.Scale(scale, 4)
            for mm in ms:
                mm.data.transform(T @ mm.matrix_world); mm.parent = None; mm.matrix_world = Matrix.Identity(4)
                mm.parent = arm; mm.matrix_parent_inverse = Matrix.Identity(4)
                for md in mm.modifiers:
                    if md.type == 'ARMATURE': md.object = arm
            bpy.data.objects.remove(a2, do_unlink=True); by_lod[i] = ms
            for a_ in [a_ for a_ in bpy.data.actions if a_ not in acts_before]: bpy.data.actions.remove(a_)   # the LOD file's own copy of walk/run: same clips, not needed twice
        # materials per LOD (the image bytes as the glb holds them), then one object per LOD
        objs = []; lod_rows = []
        for i in range(len(spec['lods'])):
            ms = by_lod[i]; n = 0
            for mm in ms:
                for si, slot in enumerate(mm.material_slots):
                    mp = maps_of(slot.material); names = ' '.join((mp[k].name.lower() if mp[k] else '') for k in ('albedo', 'emissive'))
                    label = 'visor' if 'visor' in names or mm.name == 'Glasses' else ('body' if n == 0 else f'part{n}')
                    if label != 'visor': n += 1
                    key_ = f'{fid}_{label}_L{i}'; tx = {}
                    if mp['albedo']: tx['albedo'] = write_raw(f'{fdir}/Textures/{key_}_albedo', mp['albedo'], 'albedo')
                    if mp['normal']: tx['normal'] = write_raw(f'{fdir}/Textures/{key_}_normal', mp['normal'], 'normal')
                    if mp['emissive']: tx['emissive'] = write_raw(f'{fdir}/Textures/{key_}_emissive', mp['emissive'], 'emissive')
                    mrel = f'{fdir}/Materials/{key_}.mat'; os.makedirs(os.path.join(OUT, fdir, 'Materials'), exist_ok=True)
                    open(os.path.join(OUT, mrel), 'w').write(um.mat_text(key_, base=tx.get('albedo', (None,))[0], normal=tx.get('normal', (None,))[0], metal=None,
                                                                         emissive=tx.get('emissive', (None,))[0], emission=mp['emission_strength'], smoothness=0.75 if label == 'visor' else 0.15,
                                                                         metallic=0.0, transparent=bool(mp['blend'] and label == 'visor')))
                    open(os.path.join(OUT, mrel + '.meta'), 'w').write(um.mat_meta(mrel))
                    mats[key_] = mrel; mat_rows.append({'name': key_, 'file': mrel, 'lod': i, 'label': label, 'textures': {k: v[0] for k, v in tx.items()}, 'texturePx': {k: v[1] for k, v in tx.items()}})
                    slot.material = bpy.data.materials.get(key_) or bpy.data.materials.new(key_)
            if len(ms) > 1:
                bpy.ops.object.select_all(action='DESELECT')
                for mm in ms: mm.select_set(True)
                bpy.context.view_layer.objects.active = ms[0]; bpy.ops.object.join()
            o = bpy.context.view_layer.objects.active if len(ms) > 1 else ms[0]
            o.name = f'{fid}_LOD{i}'; o.data.name = o.name; objs.append(o)
            lod_rows.append({'name': o.name, 'tris': tris(o), 'tex': max([px for r in mat_rows if r['lod'] == i for px in r['texturePx'].values()] or [0]), 'source': spec['lods'][i]})
        limit_weights(objs, 4)
        # own clips
        for nm in ('walk', 'run') + (('idle',) if fid == 'enforcer' else ()):
            act, N = resample_own(arm, srcacts[nm], nm, loop=True, kloc=kloc); own_acts[nm] = (act, N)
        if spec.get('extra_prefix'):
            for nm, a_ in srcacts.items():
                if nm in own_acts: continue
                act, N = resample_own(arm, a_, spec['extra_prefix'] + nm, loop=False, kloc=kloc); own_acts[spec['extra_prefix'] + nm] = (act, N)
    if arm.animation_data: arm.animation_data.action = None
    for a_ in list(bpy.data.actions):
        if a_.name.startswith('src_'): bpy.data.actions.remove(a_)   # the glb's own copies: resampled above, not exported twice
    # ---- the library clips, retargeted once
    rt = Retargeter(arm); out_acts = {}; alias_src = {v for k in spec['alias'] for v in [k]}
    for nm, (act, N) in own_acts.items():
        loop = not nm.startswith('tb_') or False
        out_acts[nm] = (act, N, nm not in [] and not nm.startswith('tb_'))
    for k, names_ in spec['alias'].items():
        for nm in names_:
            if nm == k: continue
            act, N = out_acts[k][0], out_acts[k][1]
            dup = act.copy(); dup.name = nm; out_acts[nm] = (dup, N, True)
    for nm, row in CLIPS.items():
        if nm in out_acts: continue
        src = os.path.join(PEOPLE, row['file']); act, N = rt.run(src, nm, row['loop']); out_acts[nm] = (act, N, bool(row['loop']))
    speeds = {}
    for nm, (act, N, loop) in out_acts.items():
        if nm.startswith('tb_'): continue
        if SPEC.get(nm, {}).get('loco') or nm in ('walk', 'run', 'walk_f', 'run_lean'):
            speeds[nm] = own_speed(arm, act, N)
    # ---- export
    arm.animation_data.action = None
    bpy.ops.object.select_all(action='DESELECT'); arm.select_set(True)
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = arm
    sc = bpy.context.scene; sc.render.fps = FPS
    for nm, (act, N, loop) in out_acts.items(): act.use_fake_user = True
    fbx_rel = f'{fdir}/{fid}.fbx'; fbx = os.path.join(OUT, fbx_rel); os.makedirs(os.path.dirname(fbx), exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True, object_types={'ARMATURE', 'MESH'}, use_mesh_modifiers=False, add_leaf_bones=False, bake_anim=True,
                             bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False, bake_anim_force_startend_keying=False, bake_anim_simplify_factor=0.0, bake_anim_step=1.0,
                             axis_forward='-Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', bake_space_transform=False, primary_bone_axis='Y',
                             secondary_bone_axis='X', armature_nodetype='NULL', mesh_smooth_type='FACE', use_tspace=True, path_mode='STRIP', embed_textures=False)
    clips_meta = [(nm, N, loop) for nm, (act, N, loop) in out_acts.items()]
    open(fbx + '.meta', 'w').write(generic_meta(fbx_rel, mats, clips_meta))
    mn2, mx2 = world_bounds([objs[0]])
    row = {'id': fid, 'role': spec['role'], 'label': spec['label'], 'standIn': True, 'fbx': fbx_rel, 'sha256': sha(open(fbx, 'rb').read()),
           'rig': {'type': 'generic', 'bones': len(arm.data.bones), 'names': 'own (Mixamo names; the Meshy-style rig has Spine02/Spine01/Spine and neck)', 'root': 'Armature/Hips'},
           'heightM': H, 'female': spec['female'], 'lods': lod_rows, 'materials': mat_rows, 'clips': [{'name': nm, 'frames': N, 'fps': FPS, 'loop': loop, 'ownClip': nm in own_acts or any(nm in v for v in spec['alias'].values())} for nm, (act, N, loop) in out_acts.items()],
           'clipSpeedsMs': speeds, 'lodScreenHeights': {3: [0.18, 0.08, 0.012], 2: [0.08, 0.012], 1: [0.012]}[len(objs)]}
    json.dump(row, open(os.path.join(OUT, f'.cell.{fid}.json'), 'w'), indent=1)
    print('DONE', fid, row['heightM'], [(l['name'], l['tris']) for l in lod_rows], len(out_acts), 'clips', speeds)


def generic_meta(rel, mats, clips):
    body = ''
    for nm, N, loop in clips:
        lp = 1 if loop else 0
        body += f"""
    - serializedVersion: 19
      name: {nm}
      takeName: {nm}
      internalID: 0
      firstFrame: 0
      lastFrame: {N - 1}
      wrapMode: 0
      orientationOffsetY: 0
      level: 0
      cycleOffset: 0
      loop: 0
      hasAdditiveReferencePose: 0
      loopTime: {lp}
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
    ext = ""
    for n, r in sorted(mats.items()):
        ext += f"  - first:\n      type: UnityEngine:Material\n      assembly: UnityEngine.CoreModule\n      name: {n}\n    second: {{fileID: 2100000, guid: {um.guid(r)}, type: 2}}\n"
    t = um._MODEL_HEAD.format(guid=um.guid(rel), externals=ext, clips=body, import_anim=1, avatar_setup=1)
    t = t.replace('animationType: 3', 'animationType: 2')
    if not ext: t = t.replace("  externalObjects:\n  materials:", "  externalObjects: {}\n  materials:")
    return t


for fid in IDS:
    print('BUILD', fid); build(fid, FIGS[fid])
