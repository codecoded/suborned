# Forma, brief 063: figure lineup of the built FBXs (LOD0 + albedo), flat grey ground; usage: Blender -b -P lineup.py -- <People dir> <out.png> <yaw_deg> <id> [id ...]
import bpy, sys, math, mathutils, glob, os
a = sys.argv[sys.argv.index('--') + 1:]; P, out, yaw = a[0], a[1], float(a[2]); ids = a[3:]
PLAY = yaw >= 1000   # play zoom: orthographic, zoom 14 (28 m high at 1080 px), pitch 38, yaw = value - 1000 (the game's opening yaw is 30)
if PLAY: yaw -= 1000
bpy.ops.wm.read_factory_settings(use_empty=True)
def load(spec, x):
    fid, _, pd = spec.partition('=')
    F = f'{pd or P}/Figures/{fid}/'
    before = set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=F + fid + '.fbx')
    new = [o for o in bpy.data.objects if o not in before]; nn = [o.name for o in new]
    meshes = [o for o in new if o.type == 'MESH']
    lods = sorted([o for o in meshes if '_LOD' in o.name], key=lambda o: o.name)
    for o in lods[1:]: bpy.data.objects.remove(o, do_unlink=True)
    keep = lods[:1] or meshes
    c = sorted(glob.glob(F + f'Textures/{fid}_body_*_albedo.[jp][pn]g'), key=lambda p: -os.path.getsize(p))
    tex = bpy.data.images.load(c[0])
    for o in keep:
        m = bpy.data.materials.new('m'); m.use_nodes = True; nt = m.node_tree
        for n in list(nt.nodes): nt.nodes.remove(n)
        t = nt.nodes.new('ShaderNodeTexImage'); t.image = tex; b = nt.nodes.new('ShaderNodeBsdfPrincipled'); b.inputs['Roughness'].default_value = 0.75
        ot = nt.nodes.new('ShaderNodeOutputMaterial'); nt.links.new(t.outputs['Color'], b.inputs['Base Color']); nt.links.new(b.outputs['BSDF'], ot.inputs['Surface'])
        o.data.materials.clear(); o.data.materials.append(m)
    new = [bpy.data.objects[k] for k in nn if k in bpy.data.objects]
    root = [o for o in new if o.parent is None and o.type == 'ARMATURE'] or [o for o in new if o.parent is None]
    for r in root: r.location.x += x
for i, f in enumerate(ids): load(f, (i - (len(ids) - 1) / 2) * 0.9)
n = len(ids); tgt = mathutils.Vector((0, 0, 0.9))
cam = bpy.data.objects.new('c', bpy.data.cameras.new('c')); bpy.context.scene.collection.objects.link(cam)
cam.data.lens = 85; cam.data.sensor_width = 36; d = (n * 0.9 + 0.8) / 0.4235
r = math.radians(yaw); cam.location = (d * math.sin(r), -d * math.cos(r), 0.9 + (0 if yaw == 0 else 0.0))
cam.rotation_euler = (tgt - cam.location).to_track_quat('-Z', 'Y').to_euler(); bpy.context.scene.camera = cam
if PLAY:
    # heading 0 of a figure faces -Y in Blender (the FBX axes); the game camera at yaw 30 looks at figures that face the camera: camera sits on the figures' front side, 38 deg above the horizon
    el = math.radians(38); ay = math.radians(yaw)
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = 28.0 * 1.0 * (1500 / 1080) if False else 28.0 * 1920 / 1080
    cam.location = (math.sin(ay) * math.cos(el) * 60, -math.cos(ay) * math.cos(el) * 60, 0.9 + math.sin(el) * 60)
    cam.rotation_euler = (tgt - cam.location).to_track_quat('-Z', 'Y').to_euler()
def L(loc, e):
    l = bpy.data.lights.new('l', 'AREA'); l.energy = e; l.size = 4; o = bpy.data.objects.new('l', l); bpy.context.scene.collection.objects.link(o); o.location = loc; o.rotation_euler = (tgt - o.location).to_track_quat('-Z', 'Y').to_euler()
L((-3, -5, 4), 1500); L((4, -4, 2), 500); L((0, 5, 4), 400)
w = bpy.data.worlds.new('w'); w.use_nodes = True; w.node_tree.nodes['Background'].inputs[0].default_value = (.62, .62, .62, 1); w.node_tree.nodes['Background'].inputs[1].default_value = .9; bpy.context.scene.world = w
sc = bpy.context.scene; sc.render.engine = 'BLENDER_EEVEE'; sc.render.resolution_x, sc.render.resolution_y = (1920, 1080) if PLAY else (200 * n + 100, 560); sc.view_settings.view_transform = 'Standard'
sc.render.filepath = out; bpy.ops.render.render(write_still=True)
