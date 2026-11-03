# Forma, brief 063: cameo-framed bust of a game figure (the real model + its albedo), neutral light-grey ground like the roster portraits.
# usage: Blender -b --python cameo.py -- <name> <out.png> [albedo.png] [frame_m] [target_below_top_m]
import bpy, sys, math, mathutils, glob, os
a = sys.argv[sys.argv.index("--") + 1:]
name, out = a[0], a[1]
alb = a[2] if len(a) > 2 and a[2] != "-" else None
frame = float(a[3]) if len(a) > 3 else 0.50
head_alb = a[5] if len(a) > 5 else None
cut_below = float(a[6]) if len(a) > 6 else 0.235
below = float(a[4]) if len(a) > 4 else 0.12
F = os.environ.get("PEOPLE", "/Users/sky/Projects/suborned/unity/Assets/Suborned/People") + "/Figures/%s/" % name
YAW = math.radians(float(os.environ.get("YAW", "0")))   # camera turn round the figure (degrees; + = camera to the figure's left)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=F + "%s.fbx" % name)
objs = [o for o in bpy.context.scene.objects if o.type == "MESH"]
# keep the highest LOD only
lods = sorted([o for o in objs if "_LOD" in o.name], key=lambda o: o.name)
if lods:
    for o in lods[1:]: bpy.data.objects.remove(o, do_unlink=True)
    objs = [lods[0]]
if alb: path = alb
else:
    c = sorted(glob.glob(F + "Textures/%s_body_*_albedo.jpg" % name), key=lambda p: -int(os.path.basename(p).split("_")[-2]))
    path = c[0]
tex = bpy.data.images.load(path)
_mxz = max((o.matrix_world @ __import__("mathutils").Vector(v)).z for o in objs for v in o.bound_box)
CUTZ = _mxz - cut_below
for o in objs:
    mat = bpy.data.materials.new("m"); mat.use_nodes = True; nt = mat.node_tree
    for n in list(nt.nodes): nt.nodes.remove(n)
    t = nt.nodes.new("ShaderNodeTexImage"); t.image = tex
    b = nt.nodes.new("ShaderNodeBsdfPrincipled"); b.inputs["Roughness"].default_value = 0.75
    if "Specular IOR Level" in b.inputs: b.inputs["Specular IOR Level"].default_value = 0.2
    o_n = nt.nodes.new("ShaderNodeOutputMaterial")
    if head_alb:
        t2 = nt.nodes.new("ShaderNodeTexImage"); t2.image = bpy.data.images.load(head_alb)
        gp = nt.nodes.new("ShaderNodeNewGeometry"); sp = nt.nodes.new("ShaderNodeSeparateXYZ"); gt = nt.nodes.new("ShaderNodeMath"); gt.operation = "GREATER_THAN"
        gt.inputs[1].default_value = CUTZ
        mixn = nt.nodes.new("ShaderNodeMix"); mixn.data_type = "RGBA"
        nt.links.new(gp.outputs["Position"], sp.inputs[0]); nt.links.new(sp.outputs["Z"], gt.inputs[0]); nt.links.new(gt.outputs[0], mixn.inputs[0])
        nt.links.new(t.outputs["Color"], mixn.inputs[6]); nt.links.new(t2.outputs["Color"], mixn.inputs[7]); nt.links.new(mixn.outputs[2], b.inputs["Base Color"])
    else:
        nt.links.new(t.outputs["Color"], b.inputs["Base Color"])
    nt.links.new(b.outputs["BSDF"], o_n.inputs["Surface"])
    o.data.materials.clear(); o.data.materials.append(mat)
bpy.context.view_layer.update()
mn = mathutils.Vector((1e9,) * 3); mx = mathutils.Vector((-1e9,) * 3)
for o in objs:
    for v in o.bound_box:
        w = o.matrix_world @ mathutils.Vector(v); mn = mathutils.Vector(map(min, mn, w)); mx = mathutils.Vector(map(max, mx, w))
cx = (mn.x + mx.x) / 2; cy = (mn.y + mx.y) / 2
tz = mx.z - below
tgt = mathutils.Vector((cx, cy, tz))
dist = frame * 85 / 36
cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam")); bpy.context.scene.collection.objects.link(cam)
cam.data.lens = 85; cam.data.sensor_width = 36; cam.data.sensor_fit = "HORIZONTAL"
ELEV = math.radians(float(os.environ.get("ELEV", "0")))   # camera raised by this angle, looking at the target
cam.location = (cx + dist * math.cos(ELEV) * math.sin(YAW), cy - dist * math.cos(ELEV) * math.cos(YAW), tz + dist * math.sin(ELEV))
cam.rotation_euler = (tgt - cam.location).to_track_quat("-Z", "Y").to_euler(); bpy.context.scene.camera = cam
def light(loc, e, size, col=(1, 0.96, 0.9)):
    l = bpy.data.lights.new("l", "AREA"); l.energy = e; l.size = size; l.color = col
    o = bpy.data.objects.new("l", l); bpy.context.scene.collection.objects.link(o); o.location = loc
    o.rotation_euler = (tgt - o.location).to_track_quat("-Z", "Y").to_euler()
light((cx - 0.9, cy - 1.2, tz + 0.5), 55, 1.0)
light((cx + 1.0, cy - 0.9, tz + 0.1), 20, 1.0, (0.88, 0.95, 1))
light((cx, cy + 0.9, tz + 0.5), 25, 1.0, (1, 0.92, 0.85))
w = bpy.data.worlds.new("w"); w.use_nodes = True
w.node_tree.nodes["Background"].inputs[0].default_value = (0.55, 0.55, 0.55, 1); w.node_tree.nodes["Background"].inputs[1].default_value = 0.55
bpy.context.scene.world = w
sc = bpy.context.scene; sc.render.engine = "BLENDER_EEVEE"; sc.render.resolution_x = 1024; sc.render.resolution_y = 1024
sc.view_settings.view_transform = "Standard"; sc.view_settings.exposure = float(os.environ.get("EXPOSURE", "0"))
sc.render.film_transparent = True; sc.render.image_settings.color_mode = "RGBA"; sc.render.filepath = out
bpy.ops.render.render(write_still=True)
print("BOUNDS", mn, mx)
