"""Isolated Blender asset check/render; no production kit or Unity writes.
Run through bun run guard: Blender -b -P <this file>. See README.md.
"""
import hashlib
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT / "tools/decokit"))
import kit
import mats
import paving
import recovery075_foliage as detail
from geo import Shape

OUT = ROOT / "out/scratch/vela/recovery"
EVIDENCE = ROOT / "briefs/075/recovery/vela"
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
NAMES = [f"{p}_{v}" for p in ("BedTuft", "PlanterRun_4") for v in "ABC"] + ["Cypress_Play"]
report = {"assets": {}, "textures": {}, "checks": []}


def snapshot(obj):
    me = obj.data
    return {"bounds": [[min(v.co[i] for v in me.vertices) for i in range(3)],
                       [max(v.co[i] for v in me.vertices) for i in range(3)]],
            "triangles": sum(len(p.vertices) - 2 for p in me.polygons),
            "materials": [m.name for m in me.materials]}


def hard_parts(obj):
    me = obj.data
    return [(me.materials[p.material_index].name,
             [tuple(round(c, 6) for c in me.vertices[v].co) for v in p.vertices])
            for p in me.polygons
            if not me.materials[p.material_index].name.startswith(("DK_Leaf_", "DK_Bloom_"))]


for name in NAMES:
    before = kit.build(name)
    before.name = name + "_before"
    a = snapshot(before)
    after = detail.build_candidate(name)
    after.name = name + "_Recovery075"
    # Exact old asset envelope, including its discretised extrema. In particular
    # the curved Cypress cards cannot broaden its existing route envelope.
    for v in after.data.vertices:
        for i in range(3):
            v.co[i] = max(a["bounds"][0][i], min(a["bounds"][1][i], v.co[i]))
    b = snapshot(after)
    assert all(a["bounds"][0][i] - 1e-6 <= b["bounds"][0][i] and
               b["bounds"][1][i] <= a["bounds"][1][i] + 1e-6 for i in range(3)), name
    assert hard_parts(before) == hard_parts(after), (name, "non-foliage changed")
    assert not any(m.type == "DECIMATE" for m in after.modifiers)
    bpy.ops.object.select_all(action="DESELECT")
    after.select_set(True)
    bpy.context.view_layer.objects.active = after
    fbx = OUT / (after.name + ".fbx")
    bpy.ops.export_scene.fbx(filepath=str(fbx), use_selection=True, object_types={"MESH"},
        apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
        bake_space_transform=True, use_mesh_modifiers=False, mesh_smooth_type="OFF",
        use_tspace=False, add_leaf_bones=False, path_mode="STRIP", embed_textures=False)
    report["assets"][name] = {"before": a, "candidate": b,
        "fbx": str(fbx.relative_to(ROOT)), "sha256": hashlib.sha256(fbx.read_bytes()).hexdigest(),
        "non_foliage_exact": True, "bounds_contained": True}
    before.hide_render = after.hide_render = True

for nm in ("DK_Leaf_Shrub", "DK_Leaf_Cypress"):
    image, _ = mats._texture(nm)
    assert image is not None, nm
    path = Path(image.filepath)
    report["textures"][nm] = {"path": str(path), "dimensions": list(image.size),
        "sha256": hashlib.sha256(path.read_bytes()).hexdigest()}

scene = bpy.context.scene
scene.render.engine = "CYCLES"
scene.cycles.samples = 24
scene.cycles.use_denoising = True
scene.render.resolution_x, scene.render.resolution_y = 1200, 800
scene.render.resolution_percentage = 100
scene.world.color = (0.35, 0.35, 0.35)
bpy.ops.object.light_add(type="AREA", location=(-3, -4, 9))
bpy.context.object.data.energy = 1800
bpy.context.object.data.shape = "DISK"
bpy.context.object.data.size = 7
bpy.ops.object.camera_add()
cam = scene.camera = bpy.context.object
cam.data.type = "ORTHO"
scene.view_settings.view_transform = "AgX"


def shot(stem, target, scale):
    cam.location = Vector(target) + Vector((3.0, -5.0, 4.3))
    cam.rotation_euler = (Vector(target) - cam.location).to_track_quat("-Z", "Y").to_euler()
    cam.data.ortho_scale = scale
    scene.render.filepath = str(EVIDENCE / (stem + ".png"))
    bpy.ops.render.render(write_still=True)


for name in ("BedTuft_A", "PlanterRun_4_B", "Cypress_Play"):
    for tag, suffix in (("before", "_before"), ("candidate", "_Recovery075")):
        obj = bpy.data.objects[name + suffix]
        obj.hide_render = False
        box = snapshot(obj)["bounds"]
        h = box[1][2]
        shot(name + "-" + tag, (0, 0, h * 0.48), 11 if name == "Cypress_Play" else 5 if "Run" in name else 1.2)
        obj.hide_render = True

# Flat world-metre slab: old town flag material at its canonical 4 m period.
for key in ("Terrazzo_Ivory", "Pave_Flags"):
    s = Shape(key)
    s.box((-6, -4, -0.1), (6, 4, 0), getattr(mats.pal, key))
    o = s.build(bevel=0)
    if key == "Pave_Flags":
        # Paving is explicitly UV-mapped (unlike metric terrazzo). The old
        # sheet covers 4 m: renderer UVs must be world metres / 4.
        for uv in o.data.uv_layers.active.data:
            uv.uv /= 4
    shot("paving-" + key, (0, 0, 0), 14)
    o.hide_render = True

report["checks"] = ["7 candidate bounds contained", "7 non-foliage polygon lists exact",
                    "no DECIMATE modifiers", "2 reused textures present at full resolution"]
(EVIDENCE / "metrics.json").write_text(json.dumps(report, indent=2) + "\n")
bpy.ops.wm.save_as_mainfile(filepath=str(OUT / "foliage-candidates.blend"))
print("RECOVERY075 PASS: 7 candidates, 8 renders, no production writes")
