"""Brief 031 (Vela): the robot attention face in 3D, one frame per stage. Blender -b -P render031face.py -- --faces briefs/031/robot-face/faces --dir briefs/031/robot-face/frames [--samples 48]
Builds PatrolRobot (proposed) and RobotWaiter (the old city's robot, the old glyph faces) through kit.build, swaps the clear face material for an emissive dot-matrix texture
(faces/<set>_<stage>.png from compose031face.py faces), and renders each at TRUE play scale (orthographic, pitch 38, yaw 30, 38.6 px/m, 320 x 320) and close (the head, 1 m).
Render-only: the Unity shader (DK_RobotFaceStage.shader) is the real face."""

import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).parent))
import render008 as R8  # noqa: E402
import kit  # noqa: E402

arg = R8.arg
STAGES = ["calm", "noticed", "wary", "reporting"]
# tag -> (piece, face material, play centre z, head z, close ortho scale m). Old robots come twice: -own (their LED colour) and -sea (the patrol robot's seafoam).
JOBS = {"PatrolRobot": ("PatrolRobot", "M_RobotFace", 0.62, 1.04, 1.0)}
for _p, _m, _z, _h, _c in (("RobotWaiter", "DK_Face_Teal", .65, 1.245, .5), ("RobotPorter", "DK_Face_Amber", .7, 1.02, .7), ("RobotShopkeeper", "DK_Face_Teal", .8, 1.4, .6), ("RobotButler", "DK_Face_Amber", .85, 1.6, .5)):
    for _k in ("own", "sea"):
        JOBS[f"{_p}-{_k}"] = (_p, _m, _z, _h, _c)


def face_material(png, strength=2.0):
    m = bpy.data.materials.new("Face_" + Path(png).stem)
    nt = m.node_tree
    nt.nodes.clear()
    out, em, tx = nt.nodes.new("ShaderNodeOutputMaterial"), nt.nodes.new("ShaderNodeEmission"), nt.nodes.new("ShaderNodeTexImage")
    tx.image = bpy.data.images.load(str(png), check_existing=True)
    tx.image.colorspace_settings.name = "sRGB"
    tx.interpolation = "Linear"
    em.inputs["Strength"].default_value = strength
    nt.links.new(tx.outputs["Color"], em.inputs["Color"])
    nt.links.new(em.outputs["Emission"], out.inputs["Surface"])
    return m


def main():
    faces, out = Path(arg("--faces", "briefs/031/robot-face/faces")).resolve(), Path(arg("--dir", "briefs/031/robot-face/frames")).resolve()
    out.mkdir(parents=True, exist_ok=True)
    kit.load()
    cam, ground = R8.studio(320, arg("--samples", 48))
    sc = bpy.context.scene
    yaw, pitch = math.radians(30), math.radians(38)
    d = Vector((-math.sin(yaw) * math.cos(pitch), -math.cos(yaw) * math.cos(pitch), math.sin(pitch)))
    for tag, (piece, mname, zc, zh, cs) in JOBS.items():
        obj = kit.build(piece)
        objs = [obj, *obj.children_recursive]
        for stage, st in enumerate(STAGES):
            fm = face_material(faces / f"{tag}_{st}.png")
            for o in objs:
                for i, slot in enumerate(o.material_slots):
                    if slot.material and (slot.material.name == mname or slot.material.name.startswith("Face_")):
                        slot.material = fm
            cam.data.type = "ORTHO"
            cam.data.clip_start, cam.data.clip_end = 0.1, 600
            for view, (px, scale, centre) in {"play": (320, 320 / 38.6, Vector((0, 0, zc))), "close": (800, cs, Vector((0, 0, zh)))}.items():
                sc.render.resolution_x = sc.render.resolution_y = px
                cam.data.ortho_scale = scale
                R8.look(cam, centre + d * 100, centre)
                sc.render.filepath = str(out / f"{tag}_{st}_{view}.png")
                bpy.ops.render.render(write_still=True)
        for o in reversed(objs):
            bpy.data.objects.remove(o, do_unlink=True)


main()
