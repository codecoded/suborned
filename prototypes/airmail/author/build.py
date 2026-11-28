"""Blender background export. Prints the verified triangle count for every authored LOD."""

from pathlib import Path
import argparse
import json
import sys

import bpy

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
from mesh import Part
from modules import PARTS


def raised_letters(s):

    chars = bpy.data.curves.new("Cast letters", "FONT")
    chars.body = "M E R I D I A N   A I R   P O S T"
    chars.size = 0.77
    chars.align_x = "CENTER"
    chars.resolution_u = 3 if s.level == 0 else 1
    chars.extrude = 0.025
    chars.bevel_depth = 0.005 if s.level == 0 else 0
    chars.bevel_resolution = 0
    ob = bpy.data.objects.new("Casting pattern", chars)
    bpy.context.collection.objects.link(ob)
    deps = bpy.context.evaluated_depsgraph_get()
    ev = ob.evaluated_get(deps)
    mesh = ev.to_mesh()
    mesh.calc_loop_triangles()
    w = max(v.co.x for v in mesh.vertices) - min(v.co.x for v in mesh.vertices)
    fac = 8.89 / w
    for tri in mesh.loop_triangles:
        pts = [mesh.vertices[i].co for i in tri.vertices]
        s.face([(v.x * fac, -1.036 - v.z, 0.37 + v.y) for v in pts], "ivory")
    ev.to_mesh_clear()
    bpy.data.objects.remove(ob, do_unlink=True)
    bpy.data.curves.remove(chars)


def material(maps):
    mat = bpy.data.materials.new("Meridian • glazed faience, stone and inlays")
    mat.use_nodes = True
    mat.use_backface_culling = True
    nt = mat.node_tree
    shader = nt.nodes.get("Principled BSDF")

    def texture(name, data=False):
        im = bpy.data.images.load(str(maps / (name + ".png")))
        im.colorspace_settings.name = "Non-Color" if data else "sRGB"
        im.pack()
        node = nt.nodes.new("ShaderNodeTexImage")
        node.image = im
        return node.outputs["Color"]

    nt.links.new(texture("base"), shader.inputs["Base Color"])
    normal = nt.nodes.new("ShaderNodeNormalMap")
    normal.inputs["Strength"].default_value = 0.8
    nt.links.new(texture("normal", True), normal.inputs["Color"])
    nt.links.new(normal.outputs["Normal"], shader.inputs["Normal"])
    packed = texture("orm", True)
    sep = nt.nodes.new("ShaderNodeSeparateColor")
    nt.links.new(packed, sep.inputs[0])
    for a, b in [("Green", "Roughness"), ("Blue", "Metallic")]:
        nt.links.new(sep.outputs[a], shader.inputs[b])
    group = bpy.data.node_groups.new("glTF Material Output", "ShaderNodeTree")
    group.interface.new_socket(name="Occlusion", in_out="INPUT", socket_type="NodeSocketFloat")
    oc = nt.nodes.new("ShaderNodeGroup")
    oc.node_tree = group
    nt.links.new(sep.outputs["Red"], oc.inputs["Occlusion"])
    nt.links.new(texture("light"), shader.inputs["Emission Color"])
    shader.inputs["Emission Strength"].default_value = 0.6
    return mat


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--maps", type=Path, required=True)
    parser.add_argument("--dest", type=Path, required=True)
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1 :])
    out = args.dest.resolve()
    out.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    mat = material(args.maps.resolve())
    counts = {}
    trays = []
    for title in ("Near", "Street", "Skyline"):
        tray = bpy.data.collections.new(title)
        bpy.context.scene.collection.children.link(tray)
        trays.append(tray)
    for piece, (name, fn) in enumerate(PARTS.items()):
        counts[name] = []
        for level in range(3):
            s = Part(name + "_L" + str(level), level, mat)
            fn(s)
            if name == "Marquee" and level < 2:
                raised_letters(s)
            ob, n = s.make()
            counts[name].append(n)
            # Origins stay local to each part. The packed source opens as three separated trays.
            ob.location = ((piece % 5) * 14, (piece // 5) * 18 + level * 64, 0)
            for c in list(ob.users_collection):
                c.objects.unlink(ob)
            trays[level].objects.link(ob)
            ob["part"] = name
            ob["level"] = level
            if name in ("RibbonBay", "DispatchBay") or name.endswith("Vault"):
                ob["sockets_gltf"] = json.dumps(
                    [{"kind": "bay", "at": [x, 0, 0], "out": [-1 if x < 0 else 1, 0, 0]} for x in (-3, 3)]
                )
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(out / "meridian.blend"))
    bpy.ops.export_scene.gltf(
        filepath=str(out / "meridian.glb"),
        export_format="GLB",
        export_extras=True,
        export_yup=True,
        export_animations=False,
        export_tangents=True,
    )
    (out / "triangles.json").write_text(json.dumps(counts, indent=2) + "\n")
    for name, levels in counts.items():
        if levels[0] > 8500 or levels[-1] > levels[0]:
            raise ValueError((name, levels))
    print("VERIFIED", counts)


if __name__ == "__main__":
    main()
