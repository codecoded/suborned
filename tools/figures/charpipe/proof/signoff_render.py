"""Sign-off renders (brief 069): each job of signoff_plan.py made by the pipeline's own code (build.py: MPFB body, the reference T-pose, the rig, the
wardrobe on LOD0) and drawn front, three-quarter or at play zoom. Arms are lowered into a gentle stance for the picture only (the rest pose stays the
T-pose). blender -b --factory-startup -P proof/signoff_render.py -- <jobs.json> <CharPipe dir> <out dir> <chunk> <of>"""
import bpy, sys, os, json, math, importlib.util
from mathutils import Vector

a = sys.argv[sys.argv.index('--') + 1:]
JOBS, CP, OUT, K, NCH = a[0], a[1], a[2], int(a[3]), int(a[4])
HERE = os.path.dirname(os.path.abspath(__file__))
_sp = importlib.util.spec_from_file_location('cpbuild', os.path.join(os.path.dirname(HERE), 'build.py')); B = importlib.util.module_from_spec(_sp); _sp.loader.exec_module(B)
ward = json.load(open(os.path.join(os.path.dirname(HERE), 'wardrobe.json'))); _, index = B.palette()
jobs = json.load(open(JOBS))['jobs']; jobs = [j for i, j in enumerate(jobs) if i % NCH == K]
os.makedirs(OUT, exist_ok=True)


def stage():
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_EEVEE' if 'BLENDER_EEVEE' in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items.keys() else 'BLENDER_EEVEE_NEXT'
    sc.view_settings.view_transform = 'Standard'
    w = bpy.data.worlds.new('w'); w.use_nodes = True; w.node_tree.nodes['Background'].inputs['Color'].default_value = (0.93, 0.9, 0.85, 1); w.node_tree.nodes['Background'].inputs['Strength'].default_value = 0.9; sc.world = w
    sl = bpy.data.lights.new('sun', 'SUN'); sl.energy = 3.2; sl.color = (1.0, 0.92, 0.85); so = bpy.data.objects.new('sun', sl); sc.collection.objects.link(so); so.rotation_euler = (math.radians(50), 0, math.radians(-35))
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam; cam.data.type = 'ORTHO'
    m = bpy.data.materials.new('pv'); m.use_nodes = True; nt = m.node_tree; bs = nt.nodes['Principled BSDF']; bs.inputs['Roughness'].default_value = 0.8
    tx = nt.nodes.new('ShaderNodeTexImage'); tx.image = bpy.data.images.load(os.path.join(CP, 'Shared/Textures/cp_palette_albedo.png')); tx.interpolation = 'Closest'; nt.links.new(tx.outputs['Color'], bs.inputs['Base Color'])
    return sc, cam, m


def stance(arm):
    B.ctx(arm, 'POSE'); P = arm.pose.bones
    for side, sx in (('Left', 1.0), ('Right', -1.0)):
        B.aim(P[side + 'Arm'], Vector((sx * 0.3, 0.05, -0.95)))
        for b in ('ForeArm', 'Hand'): B.aim(P[side + b], Vector((sx * 0.18, -0.12, -0.97)))
    bpy.ops.object.mode_set(mode='OBJECT')


for j in jobs:
    B.reset()
    arm, (p0, p1), eyes = B.make_human(j['macros'], j['sex'], B.reference(CP)); B.prune(arm, [p0, p1], B.KEEP)
    bpy.data.objects.remove(p1, do_unlink=True)
    L, Bn, hs = B.landmarks(arm, eyes, p0)
    person = {'parts': j['parts'], 'base': j['base'], 'buzz': j.get('buzz')}
    B.dress(p0, arm, Bn, L, hs, eyes, person, ward, 0, j['colours'], index)
    tris = B.tris(p0); stance(arm)
    sc, cam, mat = stage()
    p0.data.materials.clear(); p0.data.materials.append(mat)
    for v in j['views']:
        if v == 'play':
            cy, cp = math.radians(30), math.radians(38); d = Vector((math.sin(cy) * math.cos(cp), math.cos(cy) * math.cos(cp), -math.sin(cp)))
            arm.rotation_euler = (0, 0, math.radians(180 - 210)); H = 90
            sc.render.resolution_x = 60; sc.render.resolution_y = H; cam.data.ortho_scale = H / (1080 / 28.0)
            cam.location = Vector((0, 0, 0.85)) - d * 50; cam.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
        else:
            yaw = 0 if v == 'front' else 35
            arm.rotation_euler = (0, 0, 0); r = math.radians(yaw)
            sc.render.resolution_x = 240; sc.render.resolution_y = 330; cam.data.ortho_scale = 2.25
            cam.location = (6 * math.sin(r), -6 * math.cos(r), 1.0); cam.rotation_euler = (math.radians(90), 0, r)
        sc.render.filepath = os.path.join(OUT, f"{j['key']}__{v}.png"); bpy.ops.render.render(write_still=True)
    json.dump({'key': j['key'], 'tris': tris, 'heightM': round(max(v.co.z for v in p0.data.vertices), 3)}, open(os.path.join(OUT, j['key'] + '.json'), 'w'))
    print('DONE', j['key'], tris)
