"""Audition strip (brief 069, proof only): a clip on its own source rig, drawn as a capsule figure from the side, N poses across one cycle, the planted foot
marked; nothing retargeted. blender -b -P proof/clip_strip.py -- <clip.fbx> <out.png> [--n 8] [--px 220]"""
import bpy, sys, math, bmesh
from mathutils import Vector, Matrix
a = sys.argv[sys.argv.index('--') + 1:]
src, out = a[0], a[1]; N = int(a[a.index('--n') + 1]) if '--n' in a else 8; PX = int(a[a.index('--px') + 1]) if '--px' in a else 220
bpy.ops.wm.read_factory_settings(use_empty=True); sc = bpy.context.scene
bpy.ops.import_scene.fbx(filepath=src)
arm = next(o for o in bpy.data.objects if o.type == 'ARMATURE'); act = arm.animation_data.action
mat = bpy.data.materials.new('m'); mat.diffuse_color = (0.25, 0.45, 0.5, 1)
for b in arm.data.bones:
    if any(k in b.name for k in ('Thumb', 'Index', 'Middle', 'Ring', 'Pinky', 'End', 'Top')): continue
    L = b.length * arm.matrix_world.to_scale().x; r = 0.03 if 'Spine' in b.name or 'Hips' in b.name else 0.022
    bpy.ops.mesh.primitive_cylinder_add(radius=r, depth=L, vertices=8)
    c = bpy.context.active_object; c.data.materials.append(mat)
    c.parent = arm; c.parent_type = 'BONE'; c.parent_bone = b.name
    c.matrix_world = arm.matrix_world @ b.matrix_local @ Matrix.Translation((0, b.length / 2, 0)) @ Matrix.Rotation(math.radians(90), 4, 'X') @ Matrix.Diagonal((1 / arm.matrix_world.to_scale().x,) * 3 + (1,))
sc.render.engine = 'BLENDER_WORKBENCH'; sc.display.shading.light = 'STUDIO'; sc.display.shading.color_type = 'MATERIAL'
sc.render.resolution_x = PX; sc.render.resolution_y = int(PX * 1.3); sc.render.film_transparent = False
w = bpy.data.worlds.new('w'); sc.world = w; w.color = (0.95, 0.93, 0.89)
bpy.ops.mesh.primitive_plane_add(size=20); g = bpy.context.active_object; gm = bpy.data.materials.new('g'); gm.diffuse_color = (0.8, 0.78, 0.72, 1); g.data.materials.append(gm)
cam = bpy.data.objects.new('c', bpy.data.cameras.new('c')); sc.collection.objects.link(cam); sc.camera = cam
cam.data.type = 'ORTHO'; cam.data.ortho_scale = 2.4; cam.location = (6, 0, 0.95); cam.rotation_euler = (math.radians(90), 0, math.radians(90))
f0, f1 = act.frame_range
import os
paths = []
for i in range(N):
    sc.frame_set(int(round(f0 + (f1 - f0) * i / N)))
    sc.render.filepath = f'{out[:-4]}_{i:02d}.png'; bpy.ops.render.render(write_still=True); paths.append(sc.render.filepath)
print('STRIP', ' '.join(paths))
