"""CivicCabinet: the Tech's control cabinet (the old city's "Civic Service Cabinet C", v2), generated from its source.

Provenance (art/cabinet/PROVENANCE.json): v1 is a Meshy text-to-3D model (meshy-t2, task 01a0fe48-1730-72ad-a7f2-06bc3b08a77a, 15 credits, 3 Oct 2026, 5,981 triangles,
rear inferred from one closed front-left concept); v2 is the Blender hardware repair of it (art/cabinet/build_cabinet.v2.original.py, run on v1): the front of the shell is cut away and
rebuilt with a closed inner casing, door and spine skins, an oval brass-bezel gauge with a scale and a needle, a stepped rotary selector, a recessed latch and key slot, two lens
housings. This module is that script made a kit piece: same geometry, same atlas UV patches; the six materials map to four kit materials (atlas for the shell, door and spine;
InstrumentInk; AmberLens; SatinBrass on the district's gold), and the LODs are build_lods.py's (ratios 1 / 0.42 / 0.12 over the shell and raised hardware; the flat skins and the
watertight casing are never decimated).

1.05 m tall, 1.15 m wide, 0.58 m deep, pivot footprint centre on the ground, front -Y. Placed 0 times by the world until Splice adopts it for civic-control-cabinet."""

import math
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector

from kit import asset
from mats import GROUND, TEXTURES, pal, register

ART = Path(__file__).resolve().parent / "art/cabinet"
SOURCE = ART / "civic-service-cabinet-c-v1.glb"

register("DK_Cabinet_Atlas", "paving", "#FFFFFF")
register("DK_Cabinet_InstrumentInk", "paving", "#FFFFFF")
register("DK_Cabinet_AmberLens", "paving", "#FFFFFF")
register("DK_Cabinet_Brass", "paving", "#FFFFFF")
for _n in ("DK_Cabinet_Atlas", "DK_Cabinet_InstrumentInk", "DK_Cabinet_AmberLens"):
    TEXTURES.setdefault(_n, ("cabinet_atlas_v1.png", 1.0))
TEXTURES.setdefault("DK_Cabinet_Brass", ("cabinet_gold_v1.png", 1.0))
GROUND.setdefault("DK_Cabinet_Atlas", dict(albedo="cabinet_atlas_v1.png", normal="cabinet_atlas_v1_normal.png", mask="cabinet_atlas_v1_mask.png", tile=(1.0, 1.0), smoothness=1.0))
GROUND.setdefault("DK_Cabinet_InstrumentInk", dict(albedo="cabinet_atlas_v1.png", normal="cabinet_atlas_v1_normal.png", tile=(1.0, 1.0), smoothness=0.35))
GROUND.setdefault("DK_Cabinet_AmberLens", dict(albedo="cabinet_atlas_v1.png", normal="cabinet_atlas_v1_normal.png", tile=(1.0, 1.0), smoothness=0.78))
GROUND.setdefault("DK_Cabinet_Brass", dict(albedo="cabinet_gold_v1.png", normal="cabinet_gold_v1_normal.png", mask="cabinet_gold_v1_mask.png", tile=(1.0, 1.0), smoothness=1.0))

# atlas patches (u, top, w, h) as the original; the shell, blue door and pearl spine are the atlas material, the rest their own
PATCH = {"blue": (380 / 2048, 350 / 2048, 210 / 2048, 190 / 2048), "pearl": (1810 / 2048, 550 / 2048, 95 / 2048, 90 / 2048), "brass": (0, 0, 1, 1),
         "ink": (160 / 2048, 215 / 2048, 50 / 2048, 60 / 2048), "amber": (814 / 2048, 150 / 2048, 7 / 2048, 7 / 2048)}
MAT = {"blue": "Cabinet_Atlas", "pearl": "Cabinet_Atlas", "brass": "Cabinet_Brass", "ink": "Cabinet_InstrumentInk", "amber": "Cabinet_AmberLens", "shell": "Cabinet_Atlas"}
PROTECTED = {"Cabinet_ClosedInnerCasing", "Door_BlueSkin", "Controls_PearlSkin", "Door_RightGasket"}
SCALE = 1.05 / 0.9332430064678192


def _build_objects():
    """The v2 repair script, in a scratch collection: returns [(object, material key)]. Names and numbers are build_cabinet.v2.original.py's."""
    scene = bpy.context.scene
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=str(SOURCE))
    body = next(o for o in bpy.data.objects if o not in before and o.type == "MESH")
    body.name = "Cabinet_OriginalShell"
    bpy.context.view_layer.update()
    body.data.transform(body.matrix_world)
    body.matrix_world.identity()
    body.data.materials.clear()
    created = [(body, "shell")]

    def activate(obj):
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj

    def map_surface(obj, key):
        obj.data.materials.clear()
        obj.data.materials.append(None)
        obj["mkey"] = key
        uv = obj.data.uv_layers.active or obj.data.uv_layers.new(name="UVMap")
        coords = [v.co for v in obj.data.vertices]
        lo = [min(v[i] for v in coords) for i in range(3)]
        hi = [max(v[i] for v in coords) for i in range(3)]
        u, top, w, h = PATCH[key]
        for p in obj.data.polygons:
            axes = sorted(range(3), key=lambda i: hi[i] - lo[i], reverse=True)
            a, b = (0, 2) if abs(p.normal.y) > .6 else axes[:2]
            for index in p.loop_indices:
                v = obj.data.vertices[obj.data.loops[index].vertex_index].co
                sa = (v[a] - lo[a]) / max(hi[a] - lo[a], 1e-8)
                ta = (v[b] - lo[b]) / max(hi[b] - lo[b], 1e-8)
                uv.data[index].uv = (u + sa * w, 1 - top - h + ta * h)
        return obj

    def mesh(name, verts, faces, key):
        data = bpy.data.meshes.new(name)
        data.from_pydata(verts, [], faces)
        data.update()
        obj = bpy.data.objects.new(name, data)
        scene.collection.objects.link(obj)
        bm = bmesh.new()
        bm.from_mesh(data)
        bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
        bm.to_mesh(data)
        bm.free()
        map_surface(obj, key)
        created.append((obj, key))
        return obj

    def box(name, center, dimensions, key, bevel=0):
        bpy.ops.mesh.primitive_cube_add(size=1, location=center)
        obj = bpy.context.object
        obj.name = name
        obj.dimensions = dimensions
        activate(obj)
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        if bevel:
            mod = obj.modifiers.new("Machined_edge", "BEVEL")
            mod.width, mod.segments = bevel, 2
            bpy.ops.object.modifier_apply(modifier=mod.name)
        map_surface(obj, key)
        created.append((obj, key))
        return obj

    def ellipse(rx, rz, count=32):
        return [(rx * math.cos(i * 2 * math.pi / count), rz * math.sin(i * 2 * math.pi / count)) for i in range(count)]

    def rounded_rect(rx, rz, radius, steps=4):
        pts = []
        for cx, cz, start in [(rx - radius, rz - radius, 0), (-rx + radius, rz - radius, 90), (-rx + radius, -rz + radius, 180), (rx - radius, -rz + radius, 270)]:
            for i in range(steps + 1):
                a = math.radians(start + i * 90 / steps)
                pts.append((cx + radius * math.cos(a), cz + radius * math.sin(a)))
        return pts

    def profile(name, center, loops, key, caps=False):
        cx, cz = center
        count = len(loops[0][0])
        verts = [(cx + x, y, cz + z) for contour, y in loops for x, z in contour]
        faces = []
        for j in range(len(loops) - 1):
            for i in range(count):
                a, b = j * count + i, j * count + (i + 1) % count
                faces.append((a, b, b + count, a + count))
        if caps:
            faces.extend([tuple(reversed(range(count))), tuple(range((len(loops) - 1) * count, len(loops) * count))])
        obj = mesh(name, verts, faces, key)
        for p in obj.data.polygons:
            p.use_smooth = len(p.vertices) == 4
        return obj

    def disc(name, x, z, rx, rz, back, front, key, count=32):
        return profile(name, (x, z), [(ellipse(rx, rz, count), back), (ellipse(rx, rz, count), front + .002), (ellipse(rx - .002, rz - .002, count), front)], key, caps=True)

    def text(name, content, x, z, size, y=-.258):
        curve = bpy.data.curves.new(name, "FONT")
        curve.body, curve.size, curve.align_x, curve.align_y, curve.resolution_u = content, size, "CENTER", "CENTER", 2
        obj = bpy.data.objects.new(name, curve)
        scene.collection.objects.link(obj)
        obj.location = (x, y, z)
        obj.rotation_euler = (math.pi / 2, 0, 0)
        activate(obj)
        bpy.ops.object.convert(target="MESH")
        obj = bpy.context.object
        map_surface(obj, "ink")
        created.append((obj, "ink"))
        return obj

    # the front of the Meshy shell is cut away (its rear stays), see the original's note on open boundaries
    bm = bmesh.new()
    bm.from_mesh(body.data)
    for axis, coordinate in [(0, -.414), (0, .434), (2, -.334), (2, .318), (1, -.190)]:
        point, normal = Vector(), Vector()
        point[axis] = coordinate
        normal[axis] = 1
        bmesh.ops.bisect_plane(bm, geom=list(bm.verts) + list(bm.edges) + list(bm.faces), dist=.0000001, plane_co=point, plane_no=normal)
    front = [f for f in bm.faces if -.414 < f.calc_center_median().x < .434 and -.334 < f.calc_center_median().z < .318 and f.calc_center_median().y < -.190]
    bmesh.ops.delete(bm, geom=front, context="FACES")
    bm.to_mesh(body.data)
    bm.free()
    body.data.update()
    map_key = "shell"
    body["mkey"] = map_key
    body.data.materials.append(None)

    box("Cabinet_ClosedInnerCasing", (.001, .000, -.008), (.894, .450, .720), "blue", .004)
    box("Door_BlueSkin", (-.107, -.223, -.010), (.618, .026, .648), "blue", .006)
    box("Controls_PearlSkin", (.320, -.220, -.0045), (.228, .026, .659), "pearl", .005)
    box("Door_RightGasket", (.203, -.230, -.010), (.005, .005, .637), "ink")

    gx, gz = .325, .218
    profile("Gauge_Bezel", (gx, gz), [(ellipse(.076, .050), -.233), (ellipse(.077, .051), -.240), (ellipse(.075, .049), -.255), (ellipse(.071, .045), -.259),
                                       (ellipse(.067, .041), -.256), (ellipse(.066, .040), -.244)], "brass")
    disc("Gauge_Gasket", gx, gz, .068, .042, -.243, -.251, "ink")
    disc("Gauge_Face", gx, gz, .065, .039, -.246, -.253, "pearl")
    for i in range(21):
        angle = math.radians(160 - i * 7)
        major = i % 5 == 0
        r1, r2 = (.034 if major else .037), .041
        dx, dz = math.cos(angle), math.sin(angle)
        p1 = (gx + dx * r1 * 1.4, -.256, gz - .019 + dz * r1)
        p2 = (gx + dx * r2 * 1.4, -.256, gz - .019 + dz * r2)
        tangent = Vector((-dz * .0007, 0, dx * .0007))
        a, b = Vector(p1), Vector(p2)
        mesh("Gauge_ScaleTick", [a - tangent, a + tangent, b + tangent, b - tangent], [(0, 1, 2, 3)], "ink")
    for label, angle in [("0", 160), ("150", 90), ("300", 20)]:
        a = math.radians(angle)
        text("Gauge_ScaleLabel", label, gx + math.cos(a) * .038, gz - .017 + math.sin(a) * .024, .007)
    text("Gauge_Units", "V", gx, gz - .007, .010)
    mesh("Gauge_Needle", [(gx - .003, -.260, gz - .022), (gx + .002, -.260, gz - .022), (gx + .020, -.260, gz + .015)], [(0, 1, 2)], "ink")
    disc("Gauge_Pivot", gx, gz - .022, .005, .005, -.257, -.261, "brass", 20)

    sx, sz = .325, .064
    profile("Selector_Bezel", (sx, sz), [(ellipse(.052, .052), -.233), (ellipse(.053, .053), -.239), (ellipse(.050, .050), -.246), (ellipse(.045, .045), -.248), (ellipse(.043, .043), -.242)], "brass")
    disc("Selector_Reveal", sx, sz, .044, .044, -.239, -.245, "ink")
    disc("Selector_Hub", sx, sz, .039, .039, -.243, -.251, "brass")
    profile("Selector_Paddle", (sx, sz), [(rounded_rect(.012, .035, .009), -.250), (rounded_rect(.013, .035, .009), -.262), (rounded_rect(.010, .032, .008), -.276)], "brass", caps=True)
    box("Selector_PearlIndex", (sx, -.277, sz + .025), (.004, .001, .009), "pearl", .001)
    for a in [45, 90, 135]:
        angle = math.radians(a)
        disc("Selector_IndexDot", sx + .060 * math.cos(angle), sz + .060 * math.sin(angle), .002, .002, -.233, -.235, "ink", 12)

    hx, hz = .143, -.030
    profile("Latch_Frame", (hx, hz), [(rounded_rect(.033, .073, .008), -.236), (rounded_rect(.033, .073, .008), -.245), (rounded_rect(.030, .070, .007), -.250),
                                      (rounded_rect(.025, .065, .006), -.249), (rounded_rect(.024, .064, .005), -.237)], "brass")
    profile("Latch_Recess", (hx, hz), [(rounded_rect(.024, .064, .005), -.237), (rounded_rect(.024, .064, .005), -.242)], "ink", caps=True)
    profile("Latch_Pull", (hx + .006, hz + .008), [(rounded_rect(.011, .049, .008), -.242), (rounded_rect(.011, .049, .008), -.259), (rounded_rect(.008, .046, .006), -.265)], "brass", caps=True)
    disc("Latch_KeyCylinder", hx, hz - .049, .007, .007, -.243, -.249, "brass", 24)
    box("Latch_KeySlot", (hx, -.251, hz - .049), (.002, .001, .007), "ink", .0003)

    for z, key, name in [(-.037, "amber", "Amber"), (-.101, "pearl", "Pearl")]:
        profile("Status_" + name + "_Rim", (.325, z), [(ellipse(.022, .022, 32), -.233), (ellipse(.022, .022, 32), -.242), (ellipse(.018, .018, 32), -.246)], "brass")
        disc("Status_" + name + "_Lens", .325, z, .017, .017, -.241, -.248, key, 32)
    return created


def _into(s, created, ratio):
    """Copy the scratch objects into the Shape (authoring Y-front, metres, bottom origin: the original's final scale and offset), then delete them."""
    for obj, key in created:
        if ratio < 1 and obj.name not in PROTECTED:
            mod = obj.modifiers.new("Runtime_Simplification", "DECIMATE")
            mod.ratio, mod.use_collapse_triangulate = ratio, True
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    for obj, key in created:
        me = bpy.data.meshes.new_from_object(obj.evaluated_get(dg), depsgraph=dg) if obj.modifiers else obj.data
        mat = getattr(pal, MAT[key])
        uvl = me.uv_layers.active
        vmap = {}
        for p in me.polygons:
            vs = []
            for vi in p.vertices:
                if vi not in vmap:
                    c = obj.matrix_world @ me.vertices[vi].co
                    vmap[vi] = s.bm.verts.new(((c.x) * SCALE, (c.y - .00253300368785858) * SCALE, (c.z + .4696179926395416) * SCALE))
                vs.append(vmap[vi])
            try:
                f = s.bm.faces.new(vs)
            except ValueError:
                continue
            f.material_index = s._mi(mat)
            f.smooth = p.use_smooth
            f[s._uvkeep] = 1
            s._pending_uv.append((f, [tuple(uvl.data[li].uv) for li in p.loop_indices]))
        if me is not obj.data:
            bpy.data.meshes.remove(me)
    for obj in [o for o in bpy.data.objects if o.get("mkey") is not None or o.name.startswith(("Sketchfab", "Meshy", "Cabinet_", "Door_", "Controls_", "Gauge_", "Selector_", "Latch_", "Status_", "Cube"))]:
        bpy.data.objects.remove(obj, do_unlink=True)


def _cabinet(s, ratio=1.0):
    _into(s, _build_objects(), ratio)


@asset("CivicCabinet", "Booths", bevel=0.0, near=lambda s: _cabinet(s, 0.42), far=lambda s: _cabinet(s, 0.12))
def civic_cabinet(s):
    _cabinet(s)
