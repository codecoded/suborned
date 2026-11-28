"""Convert the adopted Harbour FBXs and their authored LODs into the postal player's trade library."""

import sys
from pathlib import Path
import bpy
from mathutils import Matrix

lab = Path(sys.argv[sys.argv.index("--") + 1]).resolve()
play = lab.parents[1] / "unity/Assets/Suborned"
props = ("FlowerBarrow", "ProduceStall", "LuggageTrolley", "PneumaticPost")
fronts = ("MarketBay", "TicketBay", "FloristBay")
bpy.ops.wm.read_factory_settings(use_empty=True)


def finish(name, base, glow):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    bs = nt.nodes.get("Principled BSDF")
    for path, socket in ((base, "Base Color"), (glow, "Emission Color")):
        t = nt.nodes.new("ShaderNodeTexImage")
        t.image = bpy.data.images.load(str(path), check_existing=True)
        t.image.name = "yard_" + path.stem.lower()
        nt.links.new(t.outputs["Color"], bs.inputs[socket])
    bs.inputs["Roughness"].default_value = 0.57
    bs.inputs["Emission Strength"].default_value = 0.48
    return mat


maps = play / "Materials/Play"
traders = finish("Harbour trade goods", maps / "StreetTradesColour.png", maps / "StreetTradesLights.png")
shops = finish(
    "Lantern shopfronts", maps / "LanternKit/LanternBase.jpg", maps / "LanternKit/LanternLight.png"
)
for name in props + fronts:
    file = (
        (play / "Scenery") / (name + ".fbx")
        if name in props
        else play / "Scenery/LanternKit" / (name + ".fbx")
    )
    previously = set(bpy.context.scene.objects)
    bpy.ops.import_scene.fbx(filepath=str(file))
    added = set(bpy.context.scene.objects) - previously
    for ob in sorted(added, key=lambda o: o.name):
        if ob.type != "MESH":
            continue
        suffix = ob.name.rsplit(" ", 1)[-1]
        level = {"Close": 0, "Street": 1, "Horizon": 2}[suffix]
        world = ob.matrix_world.copy()
        ob.parent = None
        ob.data.transform(world)
        ob.matrix_world = Matrix.Identity(4)
        ob.name = f"{name}_L{level}"
        ob.data.name = ob.name
        ob.data.materials.clear()
        ob.data.materials.append(traders if name in props else shops)
    for ob in added:
        if ob.type != "MESH":
            bpy.data.objects.remove(ob, do_unlink=True)
bpy.ops.export_scene.gltf(
    filepath=str(lab / "game/assets/yard_trades.glb"),
    export_format="GLB",
    export_texcoords=True,
    export_normals=True,
    export_apply=True,
    export_yup=True,
    export_cameras=False,
    export_lights=False,
)
