"""Brief 097 (Vela): before/after renders of the baked vertex AO (vertex_ao.py). Workbench, studio light, no matcap, no Workbench cavity or
shadow: the only shade beyond the studio light is the vertex colour (R = occlusion).

/Applications/Blender.app/Contents/MacOS/Blender -b -P tools/decokit/review097_ao.py -- <out.png> <file.fbx> [yaw] [px] [flat] [--linear]
  yaw: degrees round the piece from its front (-Y), default 30 (the play camera's opening yaw); the camera looks down 38 degrees, orthographic,
  framed on the piece's bounds. flat: FLAT lighting (the AO values alone)."""
import math
import sys

import bpy
from mathutils import Vector

A = sys.argv[sys.argv.index("--") + 1:]
OUT, FBX = A[0], A[1]
YAW = float(A[2]) if len(A) > 2 else 30.0
PX = int(A[3]) if len(A) > 3 else 1100
FLAT = len(A) > 4 and A[4] == "flat"

bpy.ops.wm.read_factory_settings(use_empty=True)
LIN = "--linear" in sys.argv  # a file exported LINEAR (the 077 root); export_kit's files are SRGB: either way the file value is the occlusion
bpy.ops.import_scene.fbx(filepath=FBX, colors_type="LINEAR" if LIN else "SRGB")
objs = [o for o in bpy.data.objects if o.type == "MESH"]
for o in objs:  # shown as Unity applies it: the file value multiplies linear light
    ca = o.data.color_attributes
    if ca:
        src, vals = ca[0], [0.0] * (len(ca[0].data) * 4)
        src.data.foreach_get("color" if LIN else "color_srgb", vals)
        show = ca.new("show", "FLOAT_COLOR", src.domain)
        show.data.foreach_set("color", vals)
        ca.active_color = show
    else:  # no colour: white (no AO), never the material colour Workbench falls back to
        ca.active_color = ca.new("show", "FLOAT_COLOR", "CORNER")
        ca.active_color.data.foreach_set("color", [1.0] * (len(ca.active_color.data) * 4))
bpy.context.view_layer.update()
pts = [o.matrix_world @ Vector(c) for o in objs for c in o.bound_box]
lo = Vector([min(p[i] for p in pts) for i in range(3)])
hi = Vector([max(p[i] for p in pts) for i in range(3)])
mid, size = (lo + hi) / 2, (hi - lo).length

bpy.ops.mesh.primitive_plane_add(size=max(size * 3, 4), location=(mid.x, mid.y, 0))  # the pavement (no colour: white)

sc = bpy.context.scene
sc.render.engine = "BLENDER_WORKBENCH"
sc.render.resolution_x, sc.render.resolution_y = PX, int(PX * 0.75)
sh = sc.display.shading
sh.light = "FLAT" if FLAT else "STUDIO"
sh.color_type = "VERTEX"
sh.show_cavity = False
sh.show_shadows = False
sh.show_specular_highlight = False
sc.view_settings.view_transform = "Standard"
sc.render.film_transparent = False
sc.world = bpy.data.worlds.new("w")
sc.world.color = (0.82, 0.84, 0.86)

yaw, pitch = math.radians(YAW), math.radians(38)
d = Vector((math.sin(yaw) * math.cos(pitch), -math.cos(yaw) * math.cos(pitch), math.sin(pitch)))
cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
sc.collection.objects.link(cam)
cam.data.type = "ORTHO"
cam.data.ortho_scale = size * 1.08
cam.location = mid + d * size * 3
cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
cam.data.clip_end = size * 10
sc.camera = cam
sc.render.filepath = OUT
bpy.ops.render.render(write_still=True)
