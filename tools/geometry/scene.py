"""In-game-angle pictures for `bun run geometry` (brief 055, Splice): clay renders of the placed kit pieces, the people the host stands at their host height, and the floor they should stand on.

Blender -b -P tools/geometry/scene.py -- <spec.json>

spec: { shots: [ { out, target: [x, y, z], yaw, pitch, scale (ortho m across), w, h,
        pieces: [ { fbx, at: [x, y], yaw, scale: [sx, sy, sz], z0, color } ],        kit pieces as Unity places them (plan frame, x east, y north)
        prisms: [ { ring: [[x, y] ...], base, top, color } ],                         masses and boxes
        people: [ { at: [x, y], z, color } ],                                         a 1.8 m person standing on z (the height the host gives)
        marks:  [ { at: [x, y], z, color } ],                                         a flat disc on the floor that is drawn there
        wire:   [ { ring, base, top, color } ] } ] }                                  an outline (the engine's occluders)
Camera as the game's: compass yaw (0 = north, clockwise), pitch down, orthographic. Workbench, no textures: shape and height only.
"""
import json
import math
import sys
from pathlib import Path

import bpy
import bmesh
from mathutils import Matrix, Vector

SPEC = json.loads(Path(sys.argv[sys.argv.index("--") + 1]).read_text())
_cache = {}


def hexcol(h):
    h = h.lstrip("#"); return tuple(int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)) + (1.0,)


_mats = {}


def paint(o, col):
    """One flat material per colour (Workbench draws its viewport colour); replaces whatever the FBX brought."""
    key = col if isinstance(col, str) else str(col)
    m = _mats.get(key)
    if m is None or m.name not in bpy.data.materials:
        m = bpy.data.materials.new("m" + key); m.diffuse_color = hexcol(col); _mats[key] = m
    o.data.materials.clear(); o.data.materials.append(m)


def reset(shot):
    _mats.clear()
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_WORKBENCH"
    sc.render.resolution_x, sc.render.resolution_y = shot.get("w", 1280), shot.get("h", 960)
    sc.display.shading.light = "STUDIO"; sc.display.shading.color_type = "MATERIAL"; sc.display.shading.show_cavity = True
    sc.display.shading.show_shadows = True
    sc.world = bpy.data.worlds.new("w"); sc.world.color = (0.86, 0.9, 0.93)
    sc.render.film_transparent = False
    sc.view_settings.view_transform = "Standard"
    return sc


def piece(fbx, at, yaw, scale, z0, color):
    """Import the FBX; its Blender frame -> plan: kit x = -bx, kit z = -by (see pieces.py), plan = at + R(yaw) (kit x sx, kit z sz), height = z0 + bz sy."""
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=fbx, axis_forward="-Z", axis_up="Y")
    new = [o for o in bpy.data.objects if o not in before and o.type == "MESH"]
    sx, sy, sz = scale; c, s = math.cos(yaw), math.sin(yaw)
    # plan_x = at.x - sx c bx + sz s by ; plan_y = at.y - sx s bx - sz c by ; z = z0 + sy bz
    m = Matrix(((-sx * c, sz * s, 0, at[0]), (-sx * s, -sz * c, 0, at[1]), (0, 0, sy, z0), (0, 0, 0, 1)))
    for o in bpy.data.objects:
        if o in before: continue
        if o.type == "MESH":
            o.matrix_world = m @ o.matrix_world; paint(o, color)
        elif o.type != "EMPTY" or True:
            pass
    for o in list(bpy.data.objects):
        if o not in before and o.type != "MESH": bpy.data.objects.remove(o)


def prism(ring, base, top, color):
    me = bpy.data.meshes.new("p"); bm = bmesh.new()
    lo = [bm.verts.new((x, y, base)) for x, y in ring]; hi = [bm.verts.new((x, y, top)) for x, y in ring]
    try:
        bm.faces.new(lo[::-1]); bm.faces.new(hi)
    except ValueError:
        pass
    n = len(ring)
    for i in range(n):
        try: bm.faces.new((lo[i], lo[(i + 1) % n], hi[(i + 1) % n], hi[i]))
        except ValueError: pass
    bm.to_mesh(me); bm.free(); o = bpy.data.objects.new("p", me); bpy.context.scene.collection.objects.link(o); paint(o, color); return o


def capsule(at, z, color, h=1.8, r=0.3):
    bpy.ops.mesh.primitive_cylinder_add(radius=r, depth=h - 2 * r, location=(at[0], at[1], z + h / 2)); paint(bpy.context.active_object, color)
    for dz in (h / 2 - r, -(h / 2 - r)):
        bpy.ops.mesh.primitive_uv_sphere_add(radius=r, location=(at[0], at[1], z + h / 2 + dz)); paint(bpy.context.active_object, color)


def disc(at, z, color, r=0.45):
    bpy.ops.mesh.primitive_cylinder_add(radius=r, depth=0.02, location=(at[0], at[1], z + 0.01)); paint(bpy.context.active_object, color)


for shot in SPEC["shots"]:
    sc = reset(shot)
    for p in shot.get("pieces", []): piece(p["fbx"], p["at"], p["yaw"], p["scale"], p["z0"], p.get("color", "#d8cfc0"))
    for p in shot.get("prisms", []): prism(p["ring"], p["base"], p["top"], p.get("color", "#cfc6b6"))
    for p in shot.get("wire", []):
        w = prism(p["ring"], p["base"], p["top"], p.get("color", "#2a6fd0")); md = w.modifiers.new("w", "WIREFRAME"); md.thickness = 0.12; md.use_boundary = True
    for p in shot.get("marks", []): disc(p["at"], p["z"], p.get("color", "#2a9d5c"))
    for p in shot.get("people", []): capsule(p["at"], p["z"], p.get("color", "#d23a2a"))
    t = Vector(shot["target"]); yaw = math.radians(shot["yaw"]); pitch = math.radians(shot["pitch"])
    f = Vector((math.sin(yaw) * math.cos(pitch), math.cos(yaw) * math.cos(pitch), -math.sin(pitch)))
    cam = bpy.data.cameras.new("c"); cam.type = "ORTHO"; cam.ortho_scale = shot["scale"]; cam.clip_start = 0.1; cam.clip_end = 400
    co = bpy.data.objects.new("c", cam); sc.collection.objects.link(co); sc.camera = co
    co.location = t - f * 120; co.rotation_euler = f.to_track_quat("-Z", "Y").to_euler()
    sc.render.filepath = shot["out"]; sc.render.image_settings.file_format = "JPEG"; sc.render.image_settings.quality = 88
    bpy.ops.render.render(write_still=True)
    print("shot", shot["out"])
