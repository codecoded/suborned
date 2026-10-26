"""Check pictures for a fit (brief 069 fit test): the normalised source in x-ray with the fitted body, front and side, workbench.
  blender.sh -P tools/figures/charpipe/fit/check_fit.py -- <blend> <out png prefix>"""
import bpy, sys, math
a = sys.argv[sys.argv.index('--') + 1:]
bpy.ops.wm.open_mainfile(filepath=a[0])
sc = bpy.context.scene; sc.render.engine = 'BLENDER_WORKBENCH'; sc.display.shading.color_type = 'OBJECT'
sc.display.shading.show_xray = True; sc.display.shading.xray_alpha = 0.35; sc.render.resolution_x = 500; sc.render.resolution_y = 700
for o in bpy.data.objects:
    if o.type == 'MESH': o.color = (0.95, 0.6, 0.3, 1) if ('fit' in o.name or 'body' in o.name) else (0.4, 0.6, 0.9, 1)
cam = bpy.data.objects.new('c', bpy.data.cameras.new('c')); sc.collection.objects.link(cam); sc.camera = cam; cam.data.type = 'ORTHO'; cam.data.ortho_scale = 2.0
for name, loc, rot in (('front', (0, -6, 0.9), (90, 0, 0)), ('side', (6, 0, 0.9), (90, 0, 90))):
    cam.location = loc; cam.rotation_euler = [math.radians(r) for r in rot]; sc.render.filepath = a[1] + '_' + name + '.png'; bpy.ops.render.render(write_still=True)
