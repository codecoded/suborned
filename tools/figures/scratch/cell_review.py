"""Render takes of a built cell FBX (Blender, background): blender -b -P cell_review.py -- <fbx> <out dir> <take,take,...> [phases=6] [view=close|side|play]
Imports the FBX the way Unity would read it (each take = one action), clay material, same cameras as tv_render.py. One PNG per take: phases left to right."""
import bpy, sys, os, math
from mathutils import Vector
a = sys.argv[sys.argv.index('--') + 1:]; FBX, OUT, TAKES = a[0], a[1], a[2].split(','); NPH = int(a[3]) if len(a) > 3 else 6; VIEW = a[4] if len(a) > 4 else 'close'
os.makedirs(OUT, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=FBX)
arm = [o for o in bpy.data.objects if o.type == 'ARMATURE'][0]
meshes = [o for o in bpy.data.objects if o.type == 'MESH' and o.name.endswith('_LOD0')]
for o in [o for o in bpy.data.objects if o.type == 'MESH' and o not in meshes]: bpy.data.objects.remove(o, do_unlink=True)
sc = bpy.context.scene; sc.render.engine = 'BLENDER_WORKBENCH'
sh = sc.display.shading; sh.light = 'STUDIO'; sh.color_type = 'SINGLE'; sh.single_color = (0.82, 0.74, 0.66); sh.show_cavity = True; sh.show_object_outline = True
sc.display.light_direction = (0.4, -0.6, 0.7); sc.view_settings.view_transform = 'Standard'
sc.world = bpy.data.worlds.new('w'); sc.world.color = (0.93, 0.91, 0.86)
VIEWS = {'close': (12, 35, 2.6), 'side': (0, 0, 2.6), 'play': (38, 35, 5.56)}
pitch, az, ortho = VIEWS[VIEW]; px = 260 if VIEW != 'play' else 200
cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
cam.data.type = 'ORTHO'; cam.data.ortho_scale = ortho; sc.render.resolution_x = sc.render.resolution_y = px
p, z = math.radians(pitch), math.radians(az); d = Vector((math.sin(z) * math.cos(p), -math.cos(z) * math.cos(p), math.sin(p)))
acts = {a_.name.split('|')[-1]: a_ for a_ in bpy.data.actions}
# face the figure along +X for the side view: toes ahead of heel
foot = arm.data.bones['LeftFoot'].head_local; toe = arm.data.bones['LeftToeBase'].head_local
f = arm.matrix_world.to_3x3() @ (toe - foot); arm.rotation_euler.z = -math.atan2(f.y, f.x)
for take in TAKES:
    act = acts[take]; arm.animation_data_create(); arm.animation_data.action = act
    f0, f1 = act.frame_range
    for i in range(NPH):
        sc.frame_set(int(f0 + (f1 - f0) * i / NPH)); bpy.context.view_layer.update()
        hips = arm.matrix_world @ arm.pose.bones['Hips'].matrix.translation
        t = Vector((0, 0, 0.9)); cam.location = t + d * 30; cam.rotation_euler = (-d).to_track_quat('-Z', 'Y').to_euler()
        sc.render.filepath = f'{OUT}/{take}_{i}.png'; bpy.ops.render.render(write_still=True)
print('REVIEW done')
