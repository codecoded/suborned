"""Convert two of Harbour's citizens and its tram for the standalone material test."""

import bpy, sys
from pathlib import Path
from mathutils import Vector, Quaternion

arguments = sys.argv[sys.argv.index("--") + 1 :]
root, out = (Path(x).resolve() for x in arguments[:2])
out.mkdir(exist_ok=True, parents=True)
fig = root / "unity/Assets/Suborned/People"
models = root / "unity/Assets/Suborned/Kit/Models"


def fresh():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def fbx(path):
    before = set(bpy.context.scene.objects)
    bpy.ops.import_scene.fbx(filepath=str(path))
    return list(set(bpy.context.scene.objects) - before)


def glb(path, objects, animation=False):
    bpy.ops.object.select_all(action="DESELECT")
    for ob in objects:
        ob.select_set(True)
    bpy.ops.export_scene.gltf(
        filepath=str(path),
        export_format="GLB",
        use_selection=True,
        export_animations=animation,
        export_animation_mode="NLA_TRACKS",
        export_force_sampling=True,
    )


def citizen(id):
    fresh()
    loaded = fbx(fig / "Figures" / id / (id + ".fbx"))
    rig = next(o for o in loaded if o.type == "ARMATURE")
    body = next((o for o in loaded if o.type == "MESH" and "LOD1" in o.name), None)
    if body is None:
        body = next(o for o in loaded if o.name == id + "_LOD0")
        bpy.context.view_layer.objects.active = body
        simplify = body.modifiers.new("Street silhouette", "DECIMATE")
        triangles = sum(len(face.vertices) - 2 for face in body.data.polygons)
        simplify.ratio = min(1.0, 11000 / triangles)
        bpy.ops.object.modifier_move_to_index(modifier=simplify.name, index=0)
        bpy.ops.object.modifier_apply(modifier=simplify.name)
    mat = bpy.data.materials.new("Harbour resident " + id)
    mat.use_nodes = True
    mat.use_backface_culling = True
    shader = mat.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Roughness"].default_value = 0.72
    node = mat.node_tree.nodes.new("ShaderNodeTexImage")
    textures = fig / "Figures" / id / "Textures"
    texture = next(textures.glob("*512_albedo.jpg"), None)
    if texture:
        node.image = bpy.data.images.load(str(texture))
    else:
        node.image = bpy.data.images.load(str(textures / (id + "_body_albedo.jpg")))
        node.image.scale(1024, 1024)
        node.image.pack()
    mat.node_tree.links.new(node.outputs[0], shader.inputs["Base Color"])
    body.data.materials.clear()
    body.data.materials.append(mat)
    scene = bpy.context.scene
    scene.render.fps = 30
    rig.animation_data_clear()
    ad = rig.animation_data_create()
    bones = sorted(rig.pose.bones, key=lambda b: len(b.parent_recursive))
    aliases = {"Chest": "Spine2"}
    for side in ("Left", "Right"):
        for target, source in (
            ("UpperLeg", "UpLeg"),
            ("LowerLeg", "Leg"),
            ("Toes", "ToeBase"),
            ("UpperArm", "Arm"),
            ("LowerArm", "ForeArm"),
        ):
            aliases[side + target] = side + source
    for clip in ["idle", "walk", "push"] if id == "workers_d5" else ["idle", "walk"]:
        source_clip = "walk" if clip == "push" else clip
        source = fbx(fig / "Clips" / (source_clip + ".fbx"))
        src = next(o for o in source if o.type == "ARMATURE")
        act = src.animation_data.action
        lo, hi = map(int, act.frame_range)
        step = 3
        mapped = {b.name.split(":")[-1]: b for b in src.pose.bones}
        poses = []
        convert = rig.matrix_world.to_quaternion().inverted() @ src.matrix_world.to_quaternion()
        frames = sorted(set(range(lo, hi + 1, step)) | {hi})
        for frame in frames:
            scene.frame_set(frame)
            directions = {n: (convert @ (b.tail - b.head)).normalized() for n, b in mapped.items()}
            if clip == "push":
                to_rig = rig.matrix_world.to_quaternion().inverted()
                for side, spread in (("Left", 0.12), ("Right", -0.12)):
                    directions[side + "Arm"] = to_rig @ Vector((spread, -0.52, -0.85)).normalized()
                    directions[side + "ForeArm"] = to_rig @ Vector((0, -0.99, 0.12)).normalized()
            poses.append(directions)
        for o in source:
            bpy.data.objects.remove(o, do_unlink=True)
        action = bpy.data.actions.new(clip)
        ad.action = action
        for frame, dirs in zip(frames, poses):
            inherit = {}
            for b in bones:
                name = aliases.get(b.name, b.name)
                rest = b.bone.matrix_local.to_quaternion()
                parent = inherit[b.parent.name] if b.parent else Quaternion()
                base = parent @ rest
                rot = (
                    Vector((0, 1, 0)).rotation_difference(base.inverted() @ dirs[name])
                    if name in dirs
                    else Quaternion()
                )
                if name in ("Hips", "Head", "Neck", "Spine", "Spine1", "Spine2") or "Finger" in name:
                    rot = Quaternion()
                b.rotation_mode = "QUATERNION"
                rot.make_compatible(b.rotation_quaternion)
                b.rotation_quaternion = rot
                b.keyframe_insert("rotation_quaternion", frame=frame - lo + 1)
                inherit[b.name] = base @ rot @ rest.inverted()
        track = ad.nla_tracks.new()
        track.name = clip
        track.strips.new(clip, 1, action)
        track.mute = True
        ad.action = None
    for t in ad.nla_tracks:
        t.mute = False
    scene.frame_set(1)
    glb(out / (id + ".glb"), [rig, body], True)


def tram():
    fresh()
    objs = fbx(models / "Vehicles/DK_Tram_L1.fbx")
    for image in bpy.data.images:
        if "decals_transit_v1_alpha" in image.name:
            image.filepath = str(root / "unity/Assets/Suborned/Kit/Textures/decals_transit_v1_alpha.png")
            image.reload()
    for m in bpy.data.materials:
        n = m.name.lower()
        if not m.use_nodes:
            m.use_nodes = True
        p = m.node_tree.nodes.get("Principled BSDF")
        if not p:
            continue
        col = (0.67, 0.515, 0.32)
        met = 0.12
        rough = 0.40
        if "glass" in n:
            col = (0.072, 0.165, 0.172)
            met = 0.48
            rough = 0.21
        elif "coral" in n or "red" in n:
            col = (0.55, 0.075, 0.035)
            met = 0.24
        elif "hull" in n or "ivory" in n:
            col = (0.76, 0.69, 0.50)
        elif "chrome" in n or "steel" in n:
            col = (0.48, 0.5, 0.47)
            met = 0.85
            rough = 0.24
        elif "dark" in n or "iron" in n or "rubber" in n:
            col = (0.032, 0.04, 0.04)
        elif "cabin" in n:
            col = (0.105, 0.075, 0.049)
        elif "teal" in n or "seafoam" in n:
            col = (0.07, 0.31, 0.28)
        p.inputs["Base Color"].default_value = (*col, 1)
        p.inputs["Metallic"].default_value = met
        p.inputs["Roughness"].default_value = rough
    glb(out / "tram.glb", objs)


for name in arguments[2:] or ["c1", "c6"]:
    citizen(name)
if not arguments[2:]:
    tram()
