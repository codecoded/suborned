"""Probe candidate inputs: size, verts, materials, texture size, and a front+side workbench render each (brief 069 fit test)."""
import bpy, sys, os, json, math
from mathutils import Vector
A = sys.argv[sys.argv.index('--') + 1:]; OUT = A[0]; files = A[1:]
res = {}
for f in files:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=f)
    ms = [o for o in bpy.data.objects if o.type == 'MESH']
    dg = bpy.context.evaluated_depsgraph_get()
    pts = [o.matrix_world @ v.co for o in ms for v in o.data.vertices]
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts))); hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    imgs = [(i.name, tuple(i.size)) for i in bpy.data.images]
    arms = [o.name for o in bpy.data.objects if o.type == 'ARMATURE']
    key = os.path.basename(f)
    res[key] = {'meshes': [(o.name, len(o.data.vertices), len(o.data.polygons), [m.name for m in o.data.materials]) for o in ms], 'lo': list(lo), 'hi': list(hi), 'images': imgs, 'armatures': arms}
    sc = bpy.context.scene; sc.render.engine = 'BLENDER_EEVEE' if 'BLENDER_EEVEE' in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items] else 'BLENDER_EEVEE_NEXT'
    sc.render.resolution_x = 800; sc.render.resolution_y = 800
    w = bpy.data.worlds.new('w'); sc.world = w; w.use_nodes = True; w.node_tree.nodes['Background'].inputs[1].default_value = 1.0; w.node_tree.nodes['Background'].inputs[0].default_value = (0.8, 0.8, 0.8, 1)
    cam = bpy.data.objects.new('c', bpy.data.cameras.new('c')); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.type = 'ORTHO'; c = (lo + hi) / 2; s = max(hi - lo) * 1.05; cam.data.ortho_scale = s
    # glTF import: +Y up -> Blender Z up; front of character faces +Y in glTF? render -Y and +X views
    for name, d, rot in (('front', Vector((0, -1, 0)), (90, 0, 0)), ('side', Vector((1, 0, 0)), (90, 0, 90))):
        cam.location = c - d * -10 if False else c + Vector((0, -10, 0)) if name == 'front' else c + Vector((10, 0, 0))
        cam.rotation_euler = tuple(math.radians(r) for r in rot)
        sc.render.filepath = os.path.join(OUT, key.replace('.glb', '') + '_' + name + '.png'); bpy.ops.render.render(write_still=True)
json.dump(res, open(os.path.join(OUT, 'probe.json'), 'w'), indent=1)
