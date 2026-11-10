"""Brief 104 (Vela): PatrolBot104's review renders, from the exported FBX (or the builder) so the shots show the pivots the engine gets.
  Blender -b -P render.py -- --out DIR --faces DIR [--src fbx|kit] [--fbx PATH] [--samples 96] [--only shot,shot]

Same studio as the robot board (briefs/104/vela/robots/make/render.py): the DecoKit studio HDRI and sun, Cycles, AgX Punchy, transparent film over a shadow
catcher; compose.py lays the tiles out. Orthographic cameras, fixed metres per pixel per shot. Imported materials are swapped for the kit's own (mats.get), and
the screen shows a face texture from faces.py (render-only: the game swaps face materials, notes.md). Poses move parts about their own pivots only:
  ortho_front/left/back/right/top  the turnaround's five views, 400 px/m
  three_closed                     three-quarter front (35 deg off the front, 12 deg down), calm face
  three_open                       the same camera: the hatch open (-150 deg about its hinge), the sidearm out 0.11 m, the cross face, the body leaning 6 deg forward
                                   about the ball contact, the ball rolled 40 deg, the right arm raised and its hand bent
  hatch_open                       a close look at the open hatch and the sidearm (same pose), 1200 px/m
  hatch_front / hatch_side         the same, from nearly in front and from the robot's left
  play / play_person               the play camera (ortho size 10 on 1080 px = 54 px/m, 38 deg down, yaw 30), alone and beside a 1.8 m figure, the beacon lit
  play_ball                        the same, two robots: the kit's gunmetal ball (right) and the cream-halves variant (left: halves and band swapped), beacons lit
  beacon_unlit / beacon_lit        the beacon close (1400 px/m); lit = the glass's emission on (DK_PatrolBot104_Beacon x LIT) and a small red point light in the dome
Writes <shot>.png and pivots.json (each part's pivot as imported)."""

import json
import math
import os
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Vector

REPO = Path(__file__).resolve().parents[5]
STORE = Path(os.environ.get("SUBORNED_STORE", Path.home() / "Projects/the-suborned-store"))
sys.path.insert(0, str(REPO / "tools/decokit"))
import kit  # noqa: E402
import mats  # noqa: E402

ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
PERSON = STORE / "source-art/Citizens3D/crowd/workers_D2__body_0__tripo_3k_front.glb"


def arg(key, default):
    return type(default)(ARGS[ARGS.index(key) + 1]) if key in ARGS else default


# shot -> yaw off the front (deg; 90 = from the robot's left, Blender +x), pitch down, px per metre, frame (w, h) m, target, face, pose
SHOTS = {
    "ortho_front": (0.0, 0.0, 400.0, (1.1, 1.42), (0.0, 0.0, 0.67), "calm", False),
    "ortho_left": (90.0, 0.0, 400.0, (0.8, 1.42), (0.0, 0.0, 0.67), "calm", False),
    "ortho_back": (180.0, 0.0, 400.0, (1.1, 1.42), (0.0, 0.0, 0.67), "calm", False),
    "ortho_right": (-90.0, 0.0, 400.0, (0.8, 1.42), (0.0, 0.0, 0.67), "calm", False),
    "ortho_top": (0.0, 90.0, 400.0, (1.1, 0.7), (0.0, 0.0, 0.67), "calm", False),
    "three_closed": (35.0, 12.0, 500.0, (1.3, 1.5), (0.0, 0.0, 0.66), "calm", False),
    "three_open": (35.0, 12.0, 500.0, (1.3, 1.5), (0.0, -0.05, 0.66), "cross", True),
    "hatch_open": (28.0, 18.0, 1200.0, (0.6, 0.5), (0.0, -0.2, 0.64), "cross", True),
    "hatch_front": (12.0, 22.0, 1200.0, (0.45, 0.4), (0.0, -0.2, 0.6), "cross", True),
    "hatch_side": (80.0, 5.0, 1200.0, (0.45, 0.4), (0.0, -0.2, 0.62), "cross", True),
    "play": (30.0, 38.0, 54.0, (3.0, 2.4), (0.4, 0.0, 0.8), "calm", False),
    "play_person": (30.0, 38.0, 54.0, (3.0, 2.4), (0.4, 0.0, 0.8), "calm", False),
    "play_ball": (30.0, 38.0, 54.0, (3.6, 2.4), (0.1, 0.0, 0.8), "calm", False),
    "beacon_unlit": (30.0, 14.0, 1400.0, (0.34, 0.26), (0.0, 0.0, 1.165), "calm", False),
    "beacon_lit": (30.0, 14.0, 1400.0, (0.34, 0.26), (0.0, 0.0, 1.165), "calm", False),
}
LIT = {"play_person", "play_ball", "beacon_lit"}
LIT_EMIT = (1.0, 0.012, 0.0), 0.9  # the game drives this; a render-only stand-in for the alert state


def studio(samples):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    try:
        prefs = bpy.context.preferences.addons["cycles"].preferences
        prefs.compute_device_type = "METAL"
        prefs.get_devices()
        for d in prefs.devices:
            d.use = True
        sc.cycles.device = "GPU"
    except Exception as e:  # noqa: BLE001
        print("[104bot] GPU setup failed, CPU:", e)
    sc.cycles.samples, sc.cycles.use_denoising = samples, True
    sc.render.film_transparent = True
    sc.render.image_settings.file_format, sc.render.image_settings.color_mode = "PNG", "RGBA"
    sc.view_settings.view_transform, sc.view_settings.look = "AgX", "AgX - Punchy"
    sc.view_settings.exposure = 0.35
    w = bpy.data.worlds.new("R104")
    sc.world = w
    nt = w.node_tree
    env, mp, tc = nt.nodes.new("ShaderNodeTexEnvironment"), nt.nodes.new("ShaderNodeMapping"), nt.nodes.new("ShaderNodeTexCoord")
    env.image = bpy.data.images.load(str(REPO / "out/decokit-008/HDRI/studio.exr"))
    nt.links.new(tc.outputs["Generated"], mp.inputs["Vector"])
    nt.links.new(mp.outputs["Vector"], env.inputs["Vector"])
    mp.inputs["Rotation"].default_value[2] = math.radians(20.0)
    nt.links.new(env.outputs["Color"], nt.nodes["Background"].inputs["Color"])
    bpy.ops.mesh.primitive_plane_add(size=60)
    bpy.context.object.is_shadow_catcher = True
    sun = bpy.data.lights.new("Sun", "SUN")
    sun.energy, sun.angle, sun.color = 4.0, math.radians(4), (1.0, 0.93, 0.8)
    so = bpy.data.objects.new("Sun", sun)
    sc.collection.objects.link(so)
    so.rotation_euler = (math.radians(30), 0, math.radians(-38))
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    sc.collection.objects.link(cam)
    sc.camera = cam
    return cam


def place_camera(cam, yaw, pitch, ppm, frame, target):
    yaw, pitch = math.radians(yaw), math.radians(pitch)
    d = Vector((math.sin(yaw) * math.cos(pitch), -math.cos(yaw) * math.cos(pitch), math.sin(pitch)))  # toward the camera; yaw + = toward Blender +x
    t = Vector(target)
    cam.location = t + d * 30
    up = Vector((0, 1, 0)) if abs(pitch - math.pi / 2) < 1e-6 else Vector((0, 0, 1))
    z = d.normalized()
    x = up.cross(z).normalized()
    cam.matrix_world = Matrix((x, z.cross(x), z)).transposed().to_4x4()
    cam.location = t + d * 30
    cam.data.type, cam.data.clip_start, cam.data.clip_end = "ORTHO", 0.1, 100
    sc = bpy.context.scene
    sc.render.resolution_x, sc.render.resolution_y = round(frame[0] * ppm), round(frame[1] * ppm)
    sc.render.resolution_percentage = 100
    cam.data.ortho_scale = max(frame)


def face_material(png):
    m = bpy.data.materials.new("Face_" + Path(png).stem)
    nt = m.node_tree
    b = nt.nodes["Principled BSDF"]
    tx = nt.nodes.new("ShaderNodeTexImage")
    tx.image = bpy.data.images.load(str(png), check_existing=True)
    b.inputs["Base Color"].default_value = (0.008, 0.007, 0.006, 1)
    b.inputs["Roughness"].default_value = 0.12
    b.inputs["Coat Weight"].default_value = 1.0
    b.inputs["Emission Strength"].default_value = 2.2
    nt.links.new(tx.outputs["Color"], b.inputs["Emission Color"])
    return m


def load(src, fbx):
    """The robot's objects as siblings at their pivots, with the kit's materials. Returns {part: object} (Body for the root)."""
    if src == "kit":
        root = kit.build("PatrolBot104")
        objs = [root, *root.children_recursive]
        bpy.context.view_layer.update()
        for o in objs[1:]:
            mw = o.matrix_world.copy()
            o.parent = None
            o.matrix_world = mw
    else:
        before = set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=str(fbx))
        objs = [o for o in bpy.data.objects if o not in before and o.type == "MESH"]
        for m in {sl.material for o in objs for sl in o.material_slots}:
            m.name = m.name.split(".")[0] + "_fbx"  # free the kit's names, then put the kit's own materials in the slots
        for o in objs:
            for sl in o.material_slots:
                sl.material = mats.get(sl.material.name.removesuffix("_fbx"))
    bpy.context.view_layer.update()
    name = lambda o: o.name.split(".")[0]  # noqa: E731  (a second import is suffixed .001)
    return {("Body" if name(o) == "DK_PatrolBot104" else name(o).replace("DK_PatrolBot104_", "")): o for o in objs}


def about(objs, pivot, axis, deg):
    """Turn objects about a world point (each keeps its own pivot as part of its transform)."""
    m = Matrix.Translation(pivot) @ Matrix.Rotation(math.radians(deg), 4, axis) @ Matrix.Translation(-Vector(pivot))
    for o in objs:
        o.matrix_world = m @ o.matrix_world
    bpy.context.view_layer.update()


def pose_open(p):
    piv = lambda n: p[n].matrix_world.translation.copy()  # noqa: E731
    about([p["Ball"]], piv("Ball"), "X", 40.0)                                    # rolled
    about([p["ChestPanel"]], piv("ChestPanel"), "X", -150.0)                     # hinged open about its top edge (world axes: the import carries an axis turn)
    p["Gadget"].matrix_world = Matrix.Translation((0.0, -0.11, 0.0)) @ p["Gadget"].matrix_world  # slid out on its rail
    bpy.context.view_layer.update()
    about([p["ArmR"], p["ForearmR"]], piv("ArmR"), Vector((0.0, 1.0, 0.0)), 55.0)  # the right arm (Blender -x) raised at the shoulder
    about([p["ForearmR"]], piv("ForearmR"), Vector((0.0, 1.0, 0.0)), 25.0)        # its hand bent at the elbow
    body = [o for n, o in p.items() if n != "Ball"]
    about(body, piv("Body"), "X", 6.0)                                           # the whole body leans forward about the ball contact


def light_beacon(p):
    """The alert state, render-only: the beacon glass emits, and a small red light in the dome spills on the crown and the cage."""
    m = bpy.data.materials["DK_PatrolBot104_Beacon"]
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Emission Color"].default_value = (*LIT_EMIT[0], 1.0)
    b.inputs["Emission Strength"].default_value = LIT_EMIT[1]
    for o in [o for o in bpy.data.objects if o.name.endswith("BeaconLamp")]:
        lt = bpy.data.lights.new("BeaconLight", "POINT")
        lt.energy, lt.color, lt.shadow_soft_size = 6.0, (1.0, 0.2, 0.1), 0.03
        lo = bpy.data.objects.new("BeaconLight", lt)
        bpy.context.scene.collection.objects.link(lo)
        lo.location = o.matrix_world.translation
        o.hide_render = True  # inside the opaque dome; the light stands in for it
    for o in [o for o in bpy.data.objects if o.name.endswith("_Beacon")]:
        o.visible_shadow = False  # so the light inside reaches the crown (in Unity, a light on the beacon)


def person():
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=str(PERSON))
    objs = [o for o in bpy.data.objects if o not in before]
    roots = [o for o in objs if o.parent not in objs]
    bpy.context.view_layer.update()
    pts = [o.matrix_world @ v.co for o in objs if o.type == "MESH" for v in o.data.vertices]
    lo, hi = Vector([min(q[i] for q in pts) for i in range(3)]), Vector([max(q[i] for q in pts) for i in range(3)])
    k = 1.8 / (hi.z - lo.z)
    m = Matrix.Translation((0.95, 0.15, 0.0)) @ Matrix.Rotation(math.radians(-90), 4, "Z") @ Matrix.Scale(k, 4) @ Matrix.Translation(-Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, lo.z)))
    for r in roots:
        r.matrix_world = m @ r.matrix_world
    return objs


def main():
    out, faces = Path(arg("--out", "")).resolve(), Path(arg("--faces", "")).resolve()
    out.mkdir(parents=True, exist_ok=True)
    only = [x for x in arg("--only", "").split(",") if x] or list(SHOTS)
    src, fbx = arg("--src", "fbx"), Path(arg("--fbx", str(REPO / "out/decokit/Models/Robots/DK_PatrolBot104.fbx")))
    kit.load()
    mats.register_robot_enamels()
    for shot in only:
        yaw, pitch, ppm, frame, target, face, opened = SHOTS[shot]
        cam = studio(arg("--samples", 96))
        p = load(src, fbx)
        if shot == only[0] or shot == "ortho_front":
            (out / "pivots.json").write_text(json.dumps({n: [round(c, 4) for c in o.matrix_world.translation] for n, o in sorted(p.items())}, indent=1) + "\n")
        p["Screen"].material_slots[0].material = face_material(faces / f"{face}.png")
        if opened:
            pose_open(p)
        if shot in ("play_person", "play_ball"):
            person()
        if shot == "play_ball":  # the variant: the ball's halves cream, its band gunmetal, 0.7 m to the left
            q = load(src, fbx)
            q["Screen"].material_slots[0].material = face_material(faces / f"{face}.png")
            swap = {"DK_Gunmetal_Warm": "DK_RobotEnamel_Cream", "DK_RobotEnamel_Cream": "DK_Gunmetal_Warm"}
            for sl in q["Ball"].material_slots:
                sl.material = mats.get(swap.get(sl.material.name, sl.material.name))
            for o in q.values():
                o.matrix_world = Matrix.Translation((-0.7, 0.0, 0.0)) @ o.matrix_world
        if shot in LIT:
            light_beacon(p)
        if pitch >= 90.0:  # the top view: no ground shadow under the plan
            bpy.data.objects["Plane"].hide_render = True
        place_camera(cam, yaw, pitch, ppm, frame, target)
        bpy.context.scene.render.filepath = str(out / f"{shot}.png")
        bpy.ops.render.render(write_still=True)
        print(f"[104bot] {shot} -> {out / f'{shot}.png'}")


main()
