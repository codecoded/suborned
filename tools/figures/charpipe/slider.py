"""The slider strip (brief 069): a master (.blend from build.py --master) drawn at the reference and at each slider end, front view, one tile each.
  blender -b <Master/x.blend> -P tools/figures/charpipe/slider.py -- <palette png> <out prefix> [--px 260]"""
import bpy, sys, os, math
from mathutils import Vector
a = sys.argv[sys.argv.index('--') + 1:]; PAL, OUT = a[0], a[1]; PX = int(a[a.index('--px') + 1]) if '--px' in a else 260
sc = bpy.context.scene
sc.render.engine = 'BLENDER_EEVEE' if 'BLENDER_EEVEE' in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items.keys() else 'BLENDER_EEVEE_NEXT'
sc.render.resolution_x = PX; sc.render.resolution_y = int(PX * 1.7); sc.view_settings.view_transform = 'Standard'
w = bpy.data.worlds.new('w'); w.use_nodes = True; w.node_tree.nodes['Background'].inputs['Color'].default_value = (0.93, 0.9, 0.85, 1); sc.world = w
sl = bpy.data.lights.new('sun', 'SUN'); sl.energy = 3.2; so = bpy.data.objects.new('sun', sl); sc.collection.objects.link(so); so.rotation_euler = (math.radians(50), 0, math.radians(-35))
o = next(x for x in bpy.data.objects if x.type == 'MESH')
m = bpy.data.materials.new('pv'); m.use_nodes = True; nt = m.node_tree; bs = nt.nodes['Principled BSDF']; bs.inputs['Roughness'].default_value = 0.8
tx = nt.nodes.new('ShaderNodeTexImage'); tx.image = bpy.data.images.load(PAL); tx.interpolation = 'Closest'; nt.links.new(tx.outputs['Color'], bs.inputs['Base Color'])
o.data.materials.clear(); o.data.materials.append(m)
cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
cam.data.type = 'ORTHO'; cam.data.ortho_scale = 2.25; cam.location = (0, -6, 1.0); cam.rotation_euler = (math.radians(90), 0, 0)
keys = o.data.shape_keys.key_blocks
for i, k in enumerate(['ref'] + [k.name for k in keys[1:]]):
    for kb in keys[1:]: kb.value = 1.0 if kb.name == k else 0.0
    sc.render.filepath = f'{OUT}_{i:02d}_{k}.png'; bpy.ops.render.render(write_still=True)
print('SLIDER', len(keys))
