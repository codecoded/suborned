"""Batch Harbour's existing skyline LODs and sky vehicles into single-surface game meshes."""

import math
import re
import sys
from pathlib import Path

import bpy
from mathutils import Matrix

SOURCES = {
    "Needle": "Highrise/DK_Tower_Needle_L2",
    "Starburst": "Highrise/DK_Tower_Starburst_L2",
    "Terrace": "Highrise/DK_Tower_Terrace_L2",
    "Skyport": "Skyport/DK_Skyport_Tower_L1",
    "Lookout": "Landmarks/DK_DiscTower_L1",
    "Planetarium": "Landmarks/DK_SphereStation_L2",
    "StepSpire": "Horizon/DK_Horizon_Spire_A_L1",
    "CivicFins": "Horizon/DK_Horizon_Spire_C_L1",
    "RailHead": "Sky/DK_SkyRail_Car_Head",
    "RailCoach": "Sky/DK_SkyRail_Car_Mid",
    "RailFoot": "Sky/DK_SkyRail_Pylon_Foot",
    "RailColumn": "Sky/DK_SkyRail_Pylon_Shaft",
    "RailSaddle": "Sky/DK_SkyRail_Pylon_Head",
    "CoralCab": "Sky/DK_AirCar_Coral",
    "TealCab": "Sky/DK_AirCar_Teal",
}


def finish(material, kit):
    name = re.sub(r"\.\d+$", "", material.name)
    path = kit / "Materials" / (name + ".mat")
    rgb = (0.84, 0.82, 0.72)
    if path.is_file():
        source = path.read_text()
        m = re.search(
            r"- _BaseColor: \{r: ([\d.e+-]+), g: ([\d.e+-]+), b: ([\d.e+-]+)", source
        )
        if m:
            rgb = tuple(min(1.0, max(0.0, float(c))) for c in m.groups())
    n = name.lower()
    kind = 0
    if any(k in n for k in ("window", "glass", "pool_water")):
        rgb, kind = (0.33, 0.49, 0.57), 1
    elif any(k in n for k in ("gold", "brass")):
        rgb, kind = (0.90, 0.74, 0.44), 2
    elif any(k in n for k in ("chrome", "nickel", "brightwork")):
        rgb, kind = (0.77, 0.83, 0.82), 2
    elif any(k in n for k in ("lamp", "beacon", "headlight", "signal", "exhaust")):
        kind = 3
    elif any(k in n for k in ("limestone", "far_stone", "concrete")):
        rgb = (0.89, 0.84, 0.73)
    elif "far_shade" in n:
        rgb = (0.52, 0.64, 0.66)
    elif any(k in n for k in ("enamel", "hull", "teal", "coral", "ivory", "paint")):
        kind = 4
    return (*rgb, 1.0), kind


def build(kit, output):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    shared = bpy.data.materials.new("Harbour skyline finishes")
    shared.use_nodes = True
    shared.use_backface_culling = True
    colors = shared.node_tree.nodes.new("ShaderNodeVertexColor")
    colors.layer_name = "Harbour paint"
    shared.node_tree.links.new(
        colors.outputs["Color"],
        shared.node_tree.nodes.get("Principled BSDF").inputs["Base Color"],
    )
    products = []
    for title, stem in SOURCES.items():
        before = set(bpy.context.scene.objects)
        bpy.ops.import_scene.fbx(filepath=str(kit / "Models" / (stem + ".fbx")))
        imported = set(bpy.context.scene.objects) - before
        parts = [o for o in imported if o.type == "MESH"]
        helpers = [o for o in imported if o.type != "MESH"]
        for ob in parts:
            transform = ob.matrix_world.copy()
            ob.parent = None
            ob.matrix_world = Matrix.Identity(4)
            mesh = ob.data
            mesh.transform(transform)
            vehicle_budget = {
                "RailHead": 1400,
                "RailCoach": 1100,
                "CoralCab": 750,
                "TealCab": 750,
            }
            if title in vehicle_budget:
                mesh.calc_loop_triangles()
                limit = vehicle_budget[title] / len(parts)
                if len(mesh.loop_triangles) > limit:
                    bpy.context.view_layer.objects.active = ob
                    reduce = ob.modifiers.new("Horizon silhouette", "DECIMATE")
                    reduce.ratio = limit / len(mesh.loop_triangles)
                    bpy.ops.object.modifier_apply(modifier=reduce.name)
                    mesh = ob.data
            slots = [finish(m, kit) for m in mesh.materials]
            while mesh.uv_layers:
                mesh.uv_layers.remove(mesh.uv_layers[0])
            while mesh.color_attributes:
                mesh.color_attributes.remove(mesh.color_attributes[0])
            uv = mesh.uv_layers.new(name="Finish")
            facade = mesh.uv_layers.new(name="Tower coordinates")
            mesh.uv_layers.active_index = 0
            uv.active_render = True
            color = mesh.color_attributes.new(
                name="Harbour paint", type="BYTE_COLOR", domain="CORNER"
            )
            bounds = [
                [
                    min(v.co[i] for v in mesh.vertices),
                    max(v.co[i] for v in mesh.vertices),
                ]
                for i in range(3)
            ]
            cx, cy = [(low + high) * 0.5 for low, high in bounds[:2]]
            is_tower = title in {
                "Needle",
                "Starburst",
                "Terrace",
                "Skyport",
                "Lookout",
                "Planetarium",
                "StepSpire",
                "CivicFins",
            }
            for face in mesh.polygons:
                rgb, kind = slots[face.material_index]
                if is_tower and kind in (0, 4) and min(rgb[:3]) > 0.36:
                    kind = (
                        5
                        if title in {"Terrace", "Lookout", "Skyport", "Planetarium"}
                        else 6
                    )
                longitudes = []
                for corner in face.loop_indices:
                    co = mesh.vertices[mesh.loops[corner].vertex_index].co
                    longitudes.append(math.atan2(co.y - cy, co.x - cx) / math.tau + 0.5)
                    color.data[corner].color_srgb = rgb
                    uv.data[corner].uv = ((kind + 0.5) / 8, 0.5)
                seam = max(longitudes) - min(longitudes) > 0.5
                for corner, u in zip(face.loop_indices, longitudes):
                    z = mesh.vertices[mesh.loops[corner].vertex_index].co.z
                    height = (z - bounds[2][0]) / max(
                        0.001, bounds[2][1] - bounds[2][0]
                    )
                    facade.data[corner].uv = (
                        u + (1.0 if seam and u < 0.5 else 0.0),
                        1.0 - height,
                    )
                face.material_index = 0
            mesh.materials.clear()
            mesh.materials.append(shared)
        bpy.ops.object.select_all(action="DESELECT")
        for ob in parts:
            ob.select_set(True)
        bpy.context.view_layer.objects.active = parts[0]
        if len(parts) > 1:
            bpy.ops.object.join()
        obj = bpy.context.view_layer.objects.active
        obj.name = obj.data.name = title
        products.append(obj)
        for ob in helpers:
            bpy.data.objects.remove(ob, do_unlink=True)
    bpy.ops.object.select_all(action="DESELECT")
    for ob in products:
        ob.select_set(True)
    bpy.ops.export_scene.gltf(
        filepath=str(output),
        use_selection=True,
        export_animations=False,
        export_yup=True,
    )


if __name__ == "__main__":
    kit, output = map(Path, sys.argv[sys.argv.index("--") + 1 :])
    build(kit, output)
