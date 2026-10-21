"""Testbed clips vs today's Mixamo clips on the same figure (brief 020, Forma, 2026-10-20). Blender, background:
  blender -b -P tools/figures/scratch/tv_render.py -- <fig> <testbed.glb (basisu stripped)> <People dir> <out dir> <nowWalk> <nowRun> <moveWalk> <moveRun>
Scratch, not part of the pipeline. Both sides are posed in Blender and drawn with the same clay, lights and cameras:
  testbed = the glb's own `walk` / `run` actions on its own rig (what testbed/src/player-character.js plays);
  now     = the figure FBX the game ships (Mixamo-named 23-bone skeleton) with the Mixamo library clip retargeted by armature-space rotation delta
            (a stand-in for Unity Humanoid retargeting; hips travel scaled by hip height).
Writes <out>/<fig>_<impl>_<clip>_<view>_<NN>.png (24 phases per cycle) and <out>/<fig>_metrics.json."""
import sys, os, json, math
import bpy
from mathutils import Vector, Matrix, Quaternion, Euler

a = sys.argv[sys.argv.index('--') + 1:]
FIG, GLB, PEOPLE, OUT = a[:4]
NOW = {'walk': a[4], 'run': a[5]}
os.makedirs(OUT, exist_ok=True)
NPH = 36
STORE_ASSETS = os.path.dirname(os.path.abspath(__file__))


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def arm_of(objs):
    return [o for o in objs if o.type == 'ARMATURE']


def bbox_z(meshes, rest=True):
    lo, hi = 1e9, -1e9
    dg = bpy.context.evaluated_depsgraph_get()
    for m in meshes:
        e = m.evaluated_get(dg)
        me = e.to_mesh()
        for v in me.vertices:
            w = e.matrix_world @ v.co
            lo = min(lo, w.z); hi = max(hi, w.z)
        e.to_mesh_clear()
    return lo, hi


def facing_to_plus_x(arm):
    """Rotate the armature object about Z so the figure faces +X (toes ahead of the heel)."""
    bpy.context.view_layer.update()
    foot = arm.data.bones['LeftFoot']; toe = arm.data.bones['LeftToeBase']
    d = arm.matrix_world.to_3x3() @ (toe.head_local - foot.head_local)
    ang = math.atan2(d.y, d.x)
    arm.rotation_mode = 'XYZ'
    arm.rotation_euler.z += -ang
    bpy.context.view_layer.update()


def clay(meshes):
    mat = bpy.data.materials.new('clay'); mat.diffuse_color = (0.82, 0.74, 0.66, 1)
    for m in meshes:
        m.data.materials.clear(); m.data.materials.append(mat)


def load_testbed():
    reset()
    bpy.ops.import_scene.gltf(filepath=GLB)
    for o in list(bpy.data.objects):
        if o.type == 'MESH' and o.name.startswith('Icosphere'): bpy.data.objects.remove(o, do_unlink=True)
    arm = arm_of(bpy.data.objects)[0]
    meshes = [o for o in bpy.data.objects if o.type == 'MESH']
    arm.animation_data_create()
    # rest height -> 1.75 m
    arm.data.pose_position = 'REST'; bpy.context.view_layer.update()
    lo, hi = bbox_z(meshes); k = 1.75 / (hi - lo)
    arm.data.pose_position = 'POSE'
    arm.scale = [s * k for s in arm.scale]
    bpy.context.view_layer.update()
    facing_to_plus_x(arm)
    clay(meshes)
    acts = {}
    for n in ('walk', 'run'):
        act = bpy.data.actions[n]; f0, f1 = act.frame_range
        acts[n] = (act, f0, int(round(f1 - f0)), bpy.context.scene.render.fps)  # N frames per cycle, key at f0+N repeats f0
    return arm, meshes, acts, {'heightRaw': hi - lo, 'scaleToMetres': k, 'bones': len(arm.data.bones), 'mesh': [m.name for m in meshes]}


def load_now():
    reset()
    bpy.ops.import_scene.fbx(filepath=f'{PEOPLE}/Figures/{FIG}/{FIG}.fbx')
    arm = arm_of(bpy.data.objects)[0]
    meshes = [o for o in bpy.data.objects if o.type == 'MESH' and o.name.endswith('_LOD0')]
    for o in list(bpy.data.objects):
        if o.type == 'MESH' and o not in meshes and not o.name.endswith('_LOD0'): bpy.data.objects.remove(o, do_unlink=True)
    # keep the visor/other parts of LOD0 if any (names ending _LOD0)
    clay(meshes)
    return arm, meshes


def retarget(fig_arm, clip_file, name):
    """Bake the Mixamo clip onto fig_arm; returns (action, f0, N, fps)."""
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=clip_file)
    new = [o for o in bpy.data.objects if o not in before]
    carm = [o for o in new if o.type == 'ARMATURE'][0]
    act = carm.animation_data.action
    f0, f1 = int(act.frame_range[0]), int(act.frame_range[1]); N = f1 - f0 + 1
    sc = bpy.context.scene; fps = sc.render.fps
    # rest data (armature space) keyed by bare name
    def bare(n): return n.split(':')[-1]
    cb = {bare(b.name): b for b in carm.data.bones}
    fb = {b.name: b for b in fig_arm.data.bones}
    def depth(b): return 0 if b.parent is None else 1 + depth(b.parent)
    order = [b.name for b in sorted(fig_arm.data.bones, key=depth)]
    cmap = {bare(x): x for x in carm.pose.bones.keys()}
    CW = carm.matrix_world; FW = fig_arm.matrix_world
    c_rest = {n: CW @ cb[n].matrix_local for n in order}                   # world
    f_rest_a = {n: fb[n].matrix_local.copy() for n in order}               # fig armature space
    f_rest_w = {n: FW @ f_rest_a[n] for n in order}
    # Unity Humanoid measures from the avatar's T-pose, so an A-pose rig is first swung to the clip's rest directions (swing only), bone by bone
    Cn = {}
    for n in order:
        kids = [k.name for k in fb[n].children if k.name in cb and k.name in fb]
        if kids:
            k = [x for x in kids if x in ('Spine', 'Spine1', 'Spine2', 'LeftLeg', 'RightLeg', 'LeftForeArm', 'RightForeArm', 'LeftHand', 'RightHand', 'LeftFoot', 'RightFoot', 'Head', 'Neck', 'LeftArm', 'RightArm', 'LeftToeBase', 'RightToeBase', 'LeftUpLeg', 'RightUpLeg')]
            k = (k or kids)[0] if not (n == 'Hips') else 'Spine'
            df = (f_rest_w[k].translation - f_rest_w[n].translation).normalized()
            dc = (c_rest[k].translation - c_rest[n].translation).normalized()
            Cn[n] = df.rotation_difference(dc).to_matrix()
        else:
            Cn[n] = Cn[fb[n].parent.name] if fb[n].parent else Matrix.Identity(3)
    ratio = f_rest_w['Hips'].translation.z / c_rest['Hips'].translation.z
    new_act = bpy.data.actions.new(name)
    fig_arm.animation_data_create(); fig_arm.animation_data.action = new_act
    for pb in fig_arm.pose.bones: pb.rotation_mode = 'QUATERNION'
    FWi = FW.inverted()
    for i in range(N + 1):
        src = f0 + (i % N)
        sc.frame_set(src)
        pose = {}
        cpw = {n: CW @ carm.pose.bones[cmap[n]].matrix for n in order}
        for n in order:
            D = cpw[n].to_3x3() @ c_rest[n].to_3x3().inverted()
            rot_w = D @ (Matrix.Identity(3) if os.environ.get('TV_NOCN') else Cn[n]) @ f_rest_w[n].to_3x3()
            rot_a = FWi.to_3x3() @ rot_w
            par = fb[n].parent
            if par is None:
                dw = cpw[n].translation - c_rest[n].translation
                head_w = f_rest_w[n].translation + dw * ratio
                head_a = FWi @ head_w
            else:
                rel = f_rest_a[par.name].inverted() @ f_rest_a[n]
                head_a = (pose[par.name] @ rel).translation
            M = Matrix.Translation(head_a) @ rot_a.to_4x4()
            pose[n] = M
            if par is None: basis = f_rest_a[n].inverted() @ M
            else:
                rel = f_rest_a[par.name].inverted() @ f_rest_a[n]
                basis = rel.inverted() @ pose[par.name].inverted() @ M
            pb = fig_arm.pose.bones[n]
            loc, q, _ = basis.decompose()
            pb.location = loc; pb.rotation_quaternion = q
            pb.keyframe_insert('location', frame=1 + i); pb.keyframe_insert('rotation_quaternion', frame=1 + i)
    if os.environ.get('TV_DEBUG'):
        fig_arm.animation_data.action = new_act
        sc.frame_set(11); bpy.context.view_layer.update()
        for n in ('LeftUpLeg', 'LeftArm', 'LeftFoot'):
            print('DBG', n, 'slot', fig_arm.animation_data.action_slot, 'evaluated vs want', [round(x, 3) for x in fig_arm.pose.bones[n].matrix.to_quaternion()], [round(x, 3) for x in pose[n].to_quaternion()] if False else '')
    for o in new: bpy.data.objects.remove(o, do_unlink=True)
    return new_act, 1, N, fps


def set_phase(arm, act, f0, N, p):
    arm.animation_data.action = act
    fr = f0 + p * N
    bpy.context.scene.frame_set(int(math.floor(fr)), subframe=fr - math.floor(fr))
    bpy.context.view_layer.update()


def W(arm, name):
    if name not in arm.pose.bones: name = {n.lower(): n for n in arm.pose.bones.keys()}[name.lower()]
    return arm.matrix_world @ arm.pose.bones[name].matrix.translation


def metrics(arm, act, f0, N, fps, label):
    S = 96
    pts = []
    for i in range(S):
        set_phase(arm, act, f0, N, i / S)
        d = {n: W(arm, n) for n in ('Hips', 'LeftFoot', 'RightFoot', 'LeftToeBase', 'RightToeBase', 'LeftHand', 'RightHand', 'LeftArm', 'RightArm', 'LeftForeArm', 'RightForeArm', 'LeftLeg', 'RightLeg', 'LeftUpLeg', 'RightUpLeg', 'Neck', 'Head', 'Spine')}
        pts.append(d)
    dur = N / fps
    ground = min(min(p[s + 'Foot'].z for s in ('Left', 'Right')) for p in pts)
    sp = []
    for s in ('Left', 'Right'):
        for k in range(S):
            a, b = pts[k - 1], pts[k]
            ha = Vector((a[s + 'Foot'].x - a['Hips'].x, 0, 0)); hb = Vector((b[s + 'Foot'].x - b['Hips'].x, 0, 0))
            if a[s + 'Foot'].z <= ground + 0.04 and b[s + 'Foot'].z <= ground + 0.04:
                sp.append(-(hb.x - ha.x) / (dur / S))
    sp.sort(); v = sp[len(sp) // 2] if len(sp) > 3 else 0.0
    slide = sum(abs(x - v) for x in sp) / max(1, len(sp))
    both_off = sum(1 for p in pts if all(p[s + 'Foot'].z > ground + 0.04 for s in ('Left', 'Right')))
    def rng(f): vals = [f(p) for p in pts]; return max(vals) - min(vals)
    def ang(p, s, A, B, C):  # interior angle at B
        u = p[s + A] - p[s + B]; w = p[s + C] - p[s + B]
        return math.degrees(u.angle(w))
    knee = [min(ang(p, s, 'UpLeg', 'Leg', 'Foot') for s in ('Left', 'Right')) for p in pts]
    elbow = [min(ang(p, s, 'Arm', 'ForeArm', 'Hand') for s in ('Left', 'Right')) for p in pts]
    lean = sum(math.degrees(math.atan2((p['Neck'] - p['Hips']).x, (p['Neck'] - p['Hips']).z)) for p in pts) / S
    # arm swing: angle of shoulder->hand vector forward/back about the sideways axis, relative to hips frame
    def swing(s): return [math.degrees(math.atan2((p[s + 'Hand'] - p[s + 'Arm']).x, -(p[s + 'Hand'] - p[s + 'Arm']).z)) for p in pts]
    sw = max(max(swing(s)) - min(swing(s)) for s in ('Left', 'Right'))
    legsw = max(max(math.degrees(math.atan2((p[s + 'Foot'] - p[s + 'UpLeg']).x, -(p[s + 'Foot'] - p[s + 'UpLeg']).z)) for p in pts) - min(math.degrees(math.atan2((p[s + 'Foot'] - p[s + 'UpLeg']).x, -(p[s + 'Foot'] - p[s + 'UpLeg']).z)) for p in pts) for s in ('Left', 'Right'))
    foot_lift = max(max(p[s + 'Foot'].z for p in pts) - ground for s in ('Left', 'Right'))
    handz = rng(lambda p: p['LeftHand'].x - p['Hips'].x)
    return {'label': label, 'frames': N, 'fps': fps, 'cycleS': round(dur, 3), 'stepsPerS': round(2 / dur, 2), 'naturalSpeedMs': round(v, 2),
            'strideM': round(v * dur, 2), 'stepM': round(v * dur / 2, 2), 'footSlideCmS': round(slide * 100, 1), 'flightFramesPct': round(100 * both_off / S),
            'hipBobCm': round(rng(lambda p: p['Hips'].z) * 100, 1), 'hipSwayCm': round(rng(lambda p: p['Hips'].y) * 100, 1),
            'armSwingDeg': round(sw, 0), 'legSwingDeg': round(legsw, 0), 'kneeMinDeg': round(min(knee), 0), 'elbowMinDeg': round(min(elbow), 0),
            'footLiftCm': round(foot_lift * 100, 1), 'torsoLeanDeg': round(lean, 1)}


def setup_render():
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_WORKBENCH'
    sh = sc.display.shading
    sh.light = 'STUDIO'; sh.color_type = 'MATERIAL'; sh.show_cavity = True; sh.cavity_type = 'BOTH'
    sh.show_object_outline = True; sh.object_outline_color = (0.1, 0.08, 0.08)
    sh.show_shadows = True; sh.shadow_intensity = 0.4
    sc.display.light_direction = (0.4, -0.6, 0.7)
    sc.view_settings.view_transform = 'Standard'
    sc.render.image_settings.file_format = 'PNG'
    sc.world = bpy.data.worlds.new('w'); sc.world.color = (0.93, 0.91, 0.86)
    sc.render.film_transparent = False
    # ground with 0.5 m cross lines
    n = 512
    px = [[(236 / 255, 230 / 255, 214 / 255, 1)] * n for _ in range(n)]
    def put(x, y, c): px[y % n][x % n] = (c[0] / 255, c[1] / 255, c[2] / 255, 1)
    for i, x0 in enumerate(range(0, n, n // 4)):
        w = 5 if i == 0 else 3; c = (110, 98, 82) if i == 0 else (170, 158, 140)
        for dx in range(w):
            for y in range(n): put(x0 + dx - w // 2, y, c)
    for y0 in (0, n // 2):
        for dy in range(3):
            for x in range(n): put(x, y0 + dy - 1, (170, 158, 140))
    im = bpy.data.images.new('ground_t', n, n); flat = []
    for row in px: flat += [v for c in row for v in c]
    im.pixels.foreach_set(flat); im.pack()
    bpy.ops.mesh.primitive_plane_add(size=60, location=(0, 0, 0)); g = bpy.context.object; g.name = 'ground'
    uv = g.data.uv_layers.active.data
    for l in uv: l.uv = (l.uv[0] * 30, l.uv[1] * 30)  # 60 m / 30 = 2 m tile
    m = bpy.data.materials.new('ground'); m.use_nodes = True
    t = m.node_tree.nodes.new('ShaderNodeTexImage'); t.image = im; t.extension = 'REPEAT'
    m.node_tree.links.new(t.outputs['Color'], m.node_tree.nodes['Principled BSDF'].inputs['Base Color'])
    g.data.materials.append(m)
    sc.display.shading.color_type = 'TEXTURE'
    # materials: the figure's clay uses diffuse_color, so texture mode needs the figure to be single-colour -> set per-object via overrides
    return g


VIEWS = {  # name: (pitch deg above horizon, azimuth deg from side-on toward the front, ortho metres across, px)
    'play': (38, 35, 200 / 36.0, 200),
    'close': (12, 35, 2.4, 400),
    'side': (0, 0, 2.4, 400),
}


def camera(view, target):
    pitch, az, ortho, px = VIEWS[view]
    sc = bpy.context.scene
    cam = bpy.data.objects.get('cam') or bpy.data.objects.new('cam', bpy.data.cameras.new('cam'))
    if 'cam' not in [o.name for o in sc.collection.objects]: sc.collection.objects.link(cam)
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = ortho; cam.data.clip_end = 200
    p, z = math.radians(pitch), math.radians(az)
    d = Vector((math.sin(z) * math.cos(p), -math.cos(z) * math.cos(p), math.sin(p)))
    cam.location = target + d * 40
    cam.rotation_euler = (-d).to_track_quat('-Z', 'Y').to_euler()
    sc.camera = cam
    sc.render.resolution_x = sc.render.resolution_y = px
    sc.render.resolution_percentage = 100


def shoot(arm, meshes, act, f0, N, fps, speed, tag, ground_obj, feet_lift):
    sc = bpy.context.scene
    dur = N / fps
    for i in range(NPH):
        p = i / NPH
        set_phase(arm, act, f0, N, p)
        arm.location.x = speed * p * dur
        arm.location.z = feet_lift
        bpy.context.view_layer.update()
        target = Vector((arm.location.x, 0, 0.9))
        for v in VIEWS:
            camera(v, target if v != 'play' else Vector((arm.location.x, 0, 0.8)))
            sc.render.filepath = f'{OUT}/{tag}_{v}_{i:02d}.png'
            bpy.ops.render.render(write_still=True)


def clay_overrides(meshes):
    # texture shading for the ground, flat clay for figures: give figure materials a 1x1 clay image
    im = bpy.data.images.new('clay_img', 4, 4); im.pixels.foreach_set([212 / 255, 190 / 255, 168 / 255, 1] * 16); im.pack()
    for m in meshes:
        mat = bpy.data.materials.new('clay_t'); mat.use_nodes = True
        t = mat.node_tree.nodes.new('ShaderNodeTexImage'); t.image = im
        mat.node_tree.links.new(t.outputs['Color'], mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'])
        m.data.materials.clear(); m.data.materials.append(mat)


results = {'fig': FIG}
for impl in ('testbed', 'now'):
    if impl == 'testbed':
        arm, meshes, acts, info = load_testbed(); results['testbedInfo'] = info
    else:
        arm, meshes = load_now(); acts = {}
        for k in ('walk', 'run'):
            arm.rotation_euler.z = 0
            acts[k] = retarget(arm, f'{PEOPLE}/Clips/{NOW[k]}.fbx', f'now_{k}')
            acts[k] = (acts[k][0], acts[k][1], acts[k][2], acts[k][3])
    if impl == 'now': facing_to_plus_x(arm)
    # ground-clearance: lowest foot over the cycle sits on z=0
    g = setup_render(); clay_overrides(meshes)
    for k in ('walk', 'run'):
        act, f0, N, fps = acts[k]
        arm.location = (0, 0, 0); bpy.context.view_layer.update()
        m = metrics(arm, act, f0, N, fps, f'{impl} {k}' + (f' ({NOW[k]})' if impl == 'now' else ''))
        # lift so the lowest ankle-to-toe sits at the ground: measure lowest toe/foot bone head z over the cycle
        lows = []
        for i in range(48):
            set_phase(arm, act, f0, N, i / 48)
            lows.append(min(W(arm, n).z for n in ('LeftToeBase', 'RightToeBase', 'LeftFoot', 'RightFoot')))
        lift = -min(lows) + 0.03
        m['groundLiftM'] = round(lift, 3)
        results.setdefault(impl, {})[k] = m
        print('METRIC', FIG, json.dumps(m))
        shoot(arm, meshes, act, f0, N, fps, m['naturalSpeedMs'], f'{FIG}_{impl}_{k}', g, lift)
json.dump(results, open(f'{OUT}/{FIG}_metrics.json', 'w'), indent=1)
print('DONE', FIG)
