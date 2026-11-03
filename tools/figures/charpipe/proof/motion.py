"""Walk and run on the figures, for the proof (brief 069): Blender pictures and a foot-slide check, not the pipeline. Unity's Humanoid is the real retarget
(Latch); this preview does the same thing the simple way: each bone takes the clip's world turn since its T-pose (both rigs rest in a T-pose), the hips the
clip's height change scaled by hip height. It measures foot slide as Latch's footslide.json does: the figure moved at its manifest speed, how fast does a
planted foot move over the ground (0 = no slide).
  blender -b -P proof/motion.py -- <CharPipe dir> <out dir> <clip,clip> <id,id,...> [--strips]"""
import bpy, sys, os, json, math
from mathutils import Vector, Matrix, Quaternion

a = sys.argv[sys.argv.index('--') + 1:]
CP, OUT, CLIPS, IDS = a[0], a[1], a[2].split(','), a[3].split(','); STRIPS = '--strips' in a
os.makedirs(OUT, exist_ok=True)
rows = {json.load(open(os.path.join(CP, 'rows', f)))['id']: json.load(open(os.path.join(CP, 'rows', f))) for f in os.listdir(os.path.join(CP, 'rows'))}
FEET = ('LeftToeBase', 'RightToeBase')


def bare(n): return n.split(':')[-1]


def load_clip(name):
    before = set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=os.path.join(CP, 'Clips', name + '.fbx'))
    c = next(o for o in bpy.data.objects if o not in before and o.type == 'ARMATURE')
    for o in [o for o in bpy.data.objects if o not in before and o is not c]: bpy.data.objects.remove(o, do_unlink=True)
    return c


def setup_pairs(fig, clip):
    fb = {b.name: b for b in fig.data.bones}; cb = {bare(b.name): b for b in clip.data.bones}
    def depth(b): return 0 if b.parent is None else 1 + depth(b.parent)
    order = [b.name for b in sorted(fig.data.bones, key=depth) if b.name in cb]
    return order, fb, cb


def pose_at(fig, clip, order, fb, cb, frame, rest_c, rest_f, ratio):
    sc = bpy.context.scene; sc.frame_set(frame)
    CW, FW = clip.matrix_world, fig.matrix_world; FWi = FW.inverted(); pose = {}
    cm = {bare(n): n for n in clip.pose.bones.keys()}
    for n in order:
        cp = CW @ clip.pose.bones[cm[n]].matrix
        D = cp.to_3x3() @ rest_c[n].to_3x3().inverted()
        rot_a = FWi.to_3x3() @ (D @ rest_f[n].to_3x3())
        par = fb[n].parent
        if par is None or par.name not in pose:
            head_a = FWi @ (rest_f[n].translation + (cp.translation - rest_c[n].translation) * ratio)
        else:
            head_a = (pose[par.name] @ (fb[par.name].matrix_local.inverted() @ fb[n].matrix_local)).translation
        M = Matrix.Translation(head_a) @ rot_a.to_4x4(); pose[n] = M
        basis = fb[n].matrix_local.inverted() @ M if (par is None or par.name not in pose) else \
            (fb[par.name].matrix_local.inverted() @ fb[n].matrix_local).inverted() @ pose[par.name].inverted() @ M
        pb = fig.pose.bones[n]; pb.rotation_mode = 'QUATERNION'; l, q, _ = basis.decompose(); pb.location = l; pb.rotation_quaternion = q
    bpy.context.view_layer.update()


def run():
    bpy.ops.wm.read_factory_settings(use_empty=True); sc = bpy.context.scene
    res = []; strips = {}
    for clip_name in CLIPS:
        crow = next(c for c in json.load(open(os.path.join(CP, 'Clips/clips.rows.json')))['clips'] if c['name'] == clip_name)
        for fid in IDS:
            for o in list(bpy.data.objects): bpy.data.objects.remove(o, do_unlink=True)
            clip = load_clip(clip_name); act = clip.animation_data.action
            fps_clip = (sc.render.fps, sc.render.fps_base)      # importing the figure resets the scene to its own 24 fps: keep the clip's
            before = set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=os.path.join(CP, rows[fid]['fbxFull'].replace('CharPipe/', '')))
            sc.render.fps, sc.render.fps_base = fps_clip
            fig = next(o for o in bpy.data.objects if o not in before and o.type == 'ARMATURE')
            meshes = [o for o in bpy.data.objects if o not in before and o.type == 'MESH']
            if fig.animation_data: fig.animation_data_clear()
            order, fb, cb = setup_pairs(fig, clip)
            sc.frame_set(int(act.frame_range[0]))
            rest_c = {n: clip.matrix_world @ cb[n].matrix_local for n in order}; rest_f = {n: fig.matrix_world @ fb[n].matrix_local for n in order}
            ratio = rows[fid]['legLengthM'] / crow['sourceLegLengthM']
            speed = rows[fid]['speedsMs'][clip_name]
            f0, f1 = int(act.frame_range[0]), int(act.frame_range[1]); fps = sc.render.fps / sc.render.fps_base
            feet = {f: [] for f in FEET}
            for fr in range(f0, f1 + 1):
                pose_at(fig, clip, order, fb, cb, fr, rest_c, rest_f, ratio)
                t = (fr - f0) / fps
                for f in FEET:      # the figure walks toward its front (-Y) at its manifest speed: a planted foot should stand still on the ground
                    p = fig.matrix_world @ fig.pose.bones[f].head; feet[f].append(Vector((p.x, p.y - speed * t, p.z)))
            band = 0.03; v = []
            for f in FEET:
                zmin = min(p.z for p in feet[f])
                for i in range(1, len(feet[f])):
                    if feet[f][i].z <= zmin + band and feet[f][i - 1].z <= zmin + band: v.append((feet[f][i].y - feet[f][i - 1].y) * fps)
            v.sort(); med = v[len(v) // 2] if v else 0.0
            r = {'id': fid, 'clip': clip_name, 'heightM': rows[fid]['heightM'], 'legLengthM': rows[fid]['legLengthM'], 'speedMs': speed,
                 'plantedFootMs': round(med, 4), 'slidePct': round(100 * med / speed, 2), 'plantedSamples': len(v)}
            res.append(r); print('SLIDE', json.dumps(r))
            if STRIPS: strips[(clip_name, fid)] = render(fig, meshes, clip, order, fb, cb, rest_c, rest_f, ratio, f0, f1, clip_name, fid)
    json.dump({'schema': 'suborned.charpipe.footslide-preview/1', 'note': __doc__.split('\n')[0], 'rows': res}, open(os.path.join(OUT, 'footslide.json'), 'w'), indent=1)


def render(fig, meshes, clip, order, fb, cb, rest_c, rest_f, ratio, f0, f1, clip_name, fid):
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_EEVEE' if 'BLENDER_EEVEE' in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items.keys() else 'BLENDER_EEVEE_NEXT'
    if not sc.world:
        w = bpy.data.worlds.new('w'); w.use_nodes = True; w.node_tree.nodes['Background'].inputs['Color'].default_value = (0.93, 0.9, 0.85, 1); sc.world = w
        sl = bpy.data.lights.new('sun', 'SUN'); sl.energy = 3.2; so = bpy.data.objects.new('sun', sl); sc.collection.objects.link(so); so.rotation_euler = (math.radians(50), 0, math.radians(-35))
    mat = bpy.data.materials.get('pv')
    if not mat:
        mat = bpy.data.materials.new('pv'); mat.use_nodes = True; nt = mat.node_tree; bs = nt.nodes['Principled BSDF']; bs.inputs['Roughness'].default_value = 0.8
        tx = nt.nodes.new('ShaderNodeTexImage'); tx.image = bpy.data.images.load(os.path.join(CP, 'Shared/Textures/cp_palette_albedo.png')); tx.interpolation = 'Closest'; nt.links.new(tx.outputs['Color'], bs.inputs['Base Color'])
    for o in meshes:
        o.data.materials.clear(); o.data.materials.append(mat); o.hide_render = not o.name.endswith('_LOD0')
    cam = bpy.data.objects.get('cam') or bpy.data.objects.new('cam', bpy.data.cameras.new('cam'))
    if cam.name not in sc.collection.objects: sc.collection.objects.link(cam)
    sc.camera = cam; cam.data.type = 'ORTHO'; cam.data.ortho_scale = 2.2
    sc.render.resolution_x = 200; sc.render.resolution_y = 300
    cam.location = (6, 0, 1.0); cam.rotation_euler = (math.radians(90), 0, math.radians(90))     # the figure's left side
    paths = []
    n = 8
    for i in range(n):
        fr = f0 + round((f1 - f0) * i / n); pose_at(fig, clip, order, fb, cb, fr, rest_c, rest_f, ratio)
        cam.data.ortho_scale = 2.2; sc.render.resolution_x = 200; sc.render.resolution_y = 300
        cam.location = (6, 0, 1.0); cam.rotation_euler = (math.radians(90), 0, math.radians(90))
        p = os.path.join(OUT, f'side_{clip_name}_{fid}_{i}.png'); sc.render.filepath = p; bpy.ops.render.render(write_still=True); paths.append(p)
        # play zoom: orthographic, 38 degrees down, yaw 30, 1080 px over 28 m (a 1.75 m figure is 53 px), the figure walking across the screen
        cy, cp = math.radians(30), math.radians(38); d = Vector((math.sin(cy) * math.cos(cp), math.cos(cy) * math.cos(cp), -math.sin(cp)))
        fig.rotation_euler = (0, 0, math.radians(-60))
        H = 90; sc.render.resolution_x = 60; sc.render.resolution_y = H; cam.data.ortho_scale = H / (1080 / 28.0)
        cam.location = Vector((0, 0, 0.85)) - d * 50; cam.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
        p2 = os.path.join(OUT, f'play_{clip_name}_{fid}_{i}.png'); sc.render.filepath = p2; bpy.ops.render.render(write_still=True)
        fig.rotation_euler = (0, 0, 0)
    return paths


run()
