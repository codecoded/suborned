"""Brief 104 (Vela): PatrolBot104's demo clip, rendered from the exported FBX, every part moved about its own pivot the way the game will drive it.
  Blender -b -P demo.py -- --out DIR --faces DIR [--passes close,play] [--frames 0:600 | 10,150,350] [--res 1920x1080] [--samples 16]

The timeline is demo_motion.py (patrol in, stop and settle, look round, alarm, draw and fire three shots, hit, stand down, roll away). Two passes over the same 20 s:
  close   a three-quarter camera (50 mm, 15 deg down, 4.3 m) that follows the robot, for detail
  play    the play camera: orthographic, 38 deg down, yaw 30, 54 px per metre at 1080 (zoom 10), easing after the robot; a 1.8 m figure stands nearby
EEVEE, warm afternoon sun, the kit's materials (mats.get) and its pale limestone pavement. Writes DIR/<pass>/####.png and DIR/<pass>.json (the robot's place in frame).
Signals are render-only stand-ins for what the game drives: the face (a texture per expression), the beacon's glass (an emission sweep round the dome, since the opaque
dome hides the spinning mirror), muzzle flashes (an emissive card and a light), the hit spark."""

import json
import math
import os
import sys
from pathlib import Path

import bpy
from bpy_extras.object_utils import world_to_camera_view
from mathutils import Matrix, Vector

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[4]
STORE = Path(os.environ.get("SUBORNED_STORE", Path.home() / "Projects/the-suborned-store"))
sys.path[:0] = [str(REPO / "tools/decokit"), str(HERE)]
import demo_motion as M  # noqa: E402
import kit  # noqa: E402
import mats  # noqa: E402

ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
FBX = REPO / "out/decokit/Models/Robots/DK_PatrolBot104.fbx"
PERSON = STORE / "source-art/Citizens3D/crowd/workers_D2__body_0__tripo_3k_front.glb"
FACE_FILE = {"calm": "calm", "curious": "curious", "alert": "alert", "focused": "cross", "hurt": "dazed", "off": "off"}  # faces.py's names
PERSON_AT = (-2.3, 1.9)
CLOSE = dict(yaw=35.0, pitch=15.0, dist=5.2, lens=50.0, z=0.62, follow=3.0)
DETAIL = dict(yaw=35.0, pitch=12.0, dist=1.6, lens=50.0, z=1.05, follow=6.0)  # a check pass on the beacon and the hatch, not in the clip
PLAY = dict(yaw=30.0, pitch=38.0, ppm=54.0, z=0.6, follow=1.2)


def arg(key, default):
    return type(default)(ARGS[ARGS.index(key) + 1]) if key in ARGS else default


def world_setup(samples, res):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE" if "BLENDER_EEVEE" in {e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items} else "BLENDER_EEVEE_NEXT"
    sc.eevee.taa_render_samples = samples
    sc.render.resolution_x, sc.render.resolution_y, sc.render.resolution_percentage = *res, 100
    sc.render.fps = M.FPS
    sc.render.image_settings.file_format, sc.render.image_settings.color_mode = "PNG", "RGB"
    sc.view_settings.view_transform, sc.view_settings.look = "AgX", "AgX - Punchy"
    sc.view_settings.exposure = -0.25
    w = bpy.data.worlds.new("Demo104")
    sc.world = w
    nt = w.node_tree
    bg = nt.nodes["Background"]
    env, mp, tc = nt.nodes.new("ShaderNodeTexEnvironment"), nt.nodes.new("ShaderNodeMapping"), nt.nodes.new("ShaderNodeTexCoord")
    env.image = bpy.data.images.load(str(REPO / "out/decokit-008/HDRI/studio.exr"))
    nt.links.new(tc.outputs["Generated"], mp.inputs["Vector"])
    nt.links.new(mp.outputs["Vector"], env.inputs["Vector"])
    mp.inputs["Rotation"].default_value[2] = math.radians(20.0)
    lp, mix, sky = nt.nodes.new("ShaderNodeLightPath"), nt.nodes.new("ShaderNodeMix"), nt.nodes.new("ShaderNodeRGB")
    mix.data_type = "RGBA"
    sky.outputs[0].default_value = (0.62, 0.74, 0.88, 1.0)  # what the camera sees past the paving: a pale afternoon sky
    nt.links.new(lp.outputs["Is Camera Ray"], mix.inputs["Factor"])
    nt.links.new(env.outputs["Color"], mix.inputs["A"])
    nt.links.new(sky.outputs["Color"], mix.inputs["B"])
    nt.links.new(mix.outputs["Result"], bg.inputs["Color"])
    bg.inputs["Strength"].default_value = 0.5
    sun = bpy.data.lights.new("Sun", "SUN")
    sun.energy, sun.angle, sun.color = 4.2, math.radians(3.5), (1.0, 0.84, 0.64)  # warm afternoon, soft-edged shadows
    so = bpy.data.objects.new("Sun", sun)
    sc.collection.objects.link(so)
    so.rotation_euler = (math.radians(55), 0, math.radians(60))  # 35 deg up, from the front-right of the camera side
    ground()
    bloom(sc)


def bloom(sc):
    """A light bloom in the compositor, so the beacon's beam and the muzzle flashes glow as they will with Unity's bloom."""
    ng = bpy.data.node_groups.new("Bloom104", "CompositorNodeTree")
    ng.interface.new_socket("Image", in_out="OUTPUT", socket_type="NodeSocketColor")
    rl, gl, out = ng.nodes.new("CompositorNodeRLayers"), ng.nodes.new("CompositorNodeGlare"), ng.nodes.new("NodeGroupOutput")
    gl.inputs["Type"].default_value = "Bloom"
    gl.inputs["Threshold"].default_value, gl.inputs["Strength"].default_value = 1.2, 0.6
    ng.links.new(rl.outputs["Image"], gl.inputs["Image"])
    ng.links.new(gl.outputs["Image"], out.inputs[0])
    sc.compositing_node_group = ng
    sc.render.use_compositing = True


def ground(size=140.0):
    """The pavement: one quad, UVs in world metres (the kit's ground rows tile by the metre)."""
    h = size / 2
    me = bpy.data.meshes.new("Pavement")
    me.from_pydata([(-h, -h, 0), (h, -h, 0), (h, h, 0), (-h, h, 0)], [], [(0, 1, 2, 3)])
    uv = me.uv_layers.new(name="UVMap")
    for lp in me.loops:
        x, y, _ = me.vertices[lp.vertex_index].co
        uv.data[lp.index].uv = (x, y)
    me.materials.append(mats.get("DK_Ground_Pavement"))
    bpy.context.scene.collection.objects.link(bpy.data.objects.new("Pavement", me))


def load_robot():
    """The FBX's parts at their pivots, with the kit's materials (as render.py). Returns {part: object}."""
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(FBX))
    objs = [o for o in bpy.data.objects if o not in before and o.type == "MESH"]
    for m in {sl.material for o in objs for sl in o.material_slots}:
        m.name = m.name.split(".")[0] + "_fbx"
    for o in objs:
        for sl in o.material_slots:
            sl.material = mats.get(sl.material.name.removesuffix("_fbx"))
    bpy.context.view_layer.update()
    return {("Body" if o.name == "DK_PatrolBot104" else o.name.replace("DK_PatrolBot104_", "")): o for o in objs}


def face_materials(faces):
    out = {}
    for name, file in FACE_FILE.items():
        m = bpy.data.materials.new("Face_" + name)
        b = m.node_tree.nodes["Principled BSDF"]
        tx = m.node_tree.nodes.new("ShaderNodeTexImage")
        tx.image = bpy.data.images.load(str(faces / f"{file}.png"), check_existing=True)
        b.inputs["Base Color"].default_value = (0.008, 0.007, 0.006, 1)
        b.inputs["Roughness"].default_value = 0.12
        b.inputs["Coat Weight"].default_value = 1.0
        b.inputs["Emission Strength"].default_value = 2.2
        m.node_tree.links.new(tx.outputs["Color"], b.inputs["Emission Color"])
        out[name] = m
    return out


def beacon_sweep():
    """DK_PatrolBot104_Beacon's emission turned into a beam sweeping round the dome: strength = on * (0.15 + 10 * max(0, cos(angle - phase))^4) about the dome's centre.
    Returns the nodes the frames drive (centre, phase, on)."""
    m = bpy.data.materials["DK_PatrolBot104_Beacon"]
    nt = m.node_tree
    b = nt.nodes["Principled BSDF"]
    b.inputs["Emission Color"].default_value = (1.0, 0.035, 0.0, 1.0)
    n = lambda kind, op=None: (lambda x: (setattr(x, "operation", op) if op else None, x)[1])(nt.nodes.new(kind))  # noqa: E731
    geo, sub, sep = n("ShaderNodeNewGeometry"), n("ShaderNodeVectorMath", "SUBTRACT"), n("ShaderNodeSeparateXYZ")
    at, ph, cs = n("ShaderNodeMath", "ARCTAN2"), n("ShaderNodeMath", "SUBTRACT"), n("ShaderNodeMath", "COSINE")
    mx, pw, ma, on = n("ShaderNodeMath", "MAXIMUM"), n("ShaderNodeMath", "POWER"), n("ShaderNodeMath", "MULTIPLY_ADD"), n("ShaderNodeMath", "MULTIPLY")
    nt.links.new(geo.outputs["Position"], sub.inputs[0])
    nt.links.new(sub.outputs[0], sep.inputs[0])
    nt.links.new(sep.outputs["Y"], at.inputs[0])
    nt.links.new(sep.outputs["X"], at.inputs[1])
    nt.links.new(at.outputs[0], ph.inputs[0])
    nt.links.new(ph.outputs[0], cs.inputs[0])
    nt.links.new(cs.outputs[0], mx.inputs[0])
    mx.inputs[1].default_value = 0.0
    nt.links.new(mx.outputs[0], pw.inputs[0])
    pw.inputs[1].default_value = 4.0
    nt.links.new(pw.outputs[0], ma.inputs[0])
    ma.inputs[1].default_value, ma.inputs[2].default_value = 10.0, 0.15
    nt.links.new(ma.outputs[0], on.inputs[0])
    nt.links.new(on.outputs[0], b.inputs["Emission Strength"])
    return dict(centre=sub.inputs[1], phase=ph.inputs[1], on=on.inputs[1])


def light(name, kind, energy, colour, size=0.03):
    lt = bpy.data.lights.new(name, kind)
    lt.energy, lt.color, lt.shadow_soft_size = energy, colour, size
    o = bpy.data.objects.new(name, lt)
    bpy.context.scene.collection.objects.link(o)
    return o


def glow(name, colour, strength):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8, radius=1.0)
    o = bpy.context.object
    o.name = name
    m = bpy.data.materials.new(name)
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*colour, 1)
    b.inputs["Emission Color"].default_value = (*colour, 1)
    b.inputs["Emission Strength"].default_value = strength
    o.data.materials.append(m)
    o.visible_shadow = False
    return o


def person():
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=str(PERSON))
    objs = [o for o in bpy.data.objects if o not in before]
    roots = [o for o in objs if o.parent not in objs]
    bpy.context.view_layer.update()
    pts = [o.matrix_world @ v.co for o in objs if o.type == "MESH" for v in o.data.vertices]
    lo, hi = Vector([min(q[i] for q in pts) for i in range(3)]), Vector([max(q[i] for q in pts) for i in range(3)])
    face = math.atan2(-PERSON_AT[1], -PERSON_AT[0]) + math.pi / 2  # its front (-y as imported) turned to look at the robot's stop
    m = Matrix.Translation((*PERSON_AT, 0.0)) @ Matrix.Rotation(face, 4, "Z") @ Matrix.Scale(1.8 / (hi.z - lo.z), 4) @ Matrix.Translation(-Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, lo.z)))
    for r in roots:
        r.matrix_world = m @ r.matrix_world


def about(pivot, axis, ang):
    return Matrix.Translation(pivot) @ Matrix.Rotation(ang, 4, axis) @ Matrix.Translation(-Vector(pivot))


class Robot:
    def __init__(self, faces):
        self.p = load_robot()
        self.rest = {n: o.matrix_world.copy() for n, o in self.p.items()}
        self.piv = {n: m.translation.copy() for n, m in self.rest.items()}
        g = self.p["Gadget"]
        vs = [g.matrix_world @ v.co for v in g.data.vertices]
        front = min(v.y for v in vs)
        tip = [v for v in vs if v.y < front + 0.004]
        self.tip = Vector((sum(v.x for v in tip) / len(tip), front, sum(v.z for v in tip) / len(tip)))  # the barrel's muzzle, at rest
        self.faces = face_materials(faces)
        self.sweep = beacon_sweep()
        for o in self.p.values():
            if o.name.endswith("_Beacon"):
                o.visible_shadow = False  # so the dome's light reaches the crown (in Unity, a light on the beacon)
        self.lamp = light("BeaconLamp", "POINT", 0.0, (1.0, 0.15, 0.08))
        self.beam = light("BeaconBeam", "SPOT", 0.0, (1.0, 0.1, 0.05), 0.02)
        self.beam.data.spot_size, self.beam.data.spot_blend = math.radians(50), 0.6
        self.flash, self.flash_light = glow("Flash", (1.0, 0.72, 0.32), 40.0), light("FlashLight", "POINT", 0.0, (1.0, 0.75, 0.4))
        self.spark, self.spark_light = glow("Spark", (1.0, 0.9, 0.6), 30.0), light("SparkLight", "POINT", 0.0, (1.0, 0.85, 0.5))

    def pose(self, s):
        yb = math.radians(s["yaw_body"])
        tx, ty = s["tilt"]
        t = math.hypot(tx, ty)
        lean = about((0, 0, 0.4), Vector((-ty, tx, 0)).normalized(), t) if t > 1e-6 else Matrix()
        W = Matrix.Translation((*s["pos"], 0)) @ lean @ Matrix.Rotation(yb, 4, "Z")
        p, piv = self.p, self.piv
        for side, sgn in (("L", -1.0), ("R", 1.0)):  # raise = out to the side (about the robot's forward axis), swing = fore and aft, the arms in opposite phase
            L = about(piv["Arm" + side], "X", -sgn * s["arm_swing"]) @ about(piv["Arm" + side], "Y", sgn * s["arm_raise"])
            for n in ("Arm" + side, "Forearm" + side):
                p[n].matrix_world = W @ L @ self.rest[n]
        p["ChestPanel"].matrix_world = W @ about(piv["ChestPanel"], "X", -math.radians(s["panel"])) @ self.rest["ChestPanel"]
        p["Gadget"].matrix_world = W @ Matrix.Translation((0, -s["slide"], 0)) @ self.rest["Gadget"]
        p["BeaconLamp"].matrix_world = W @ about(piv["BeaconLamp"], "Z", s["phase"]) @ self.rest["BeaconLamp"]
        for n in ("Body", "Beacon", "Screen"):
            p[n].matrix_world = W @ self.rest[n]
        Wb = Matrix.Translation((*s["pos"], 0)) @ Matrix.Rotation(math.radians(s["yaw_ball"]), 4, "Z") @ about(piv["Ball"], "X", s["roll"])
        p["Ball"].matrix_world = Wb @ self.rest["Ball"]
        p["Screen"].material_slots[0].material = self.faces[s["face"]]
        # beacon: the sweep turns with the body (phase is relative to the robot's heading)
        centre = W @ piv["BeaconLamp"]
        ang = s["phase"] + yb - math.pi / 2
        self.sweep["centre"].default_value = centre
        self.sweep["phase"].default_value = math.atan2(math.sin(ang), math.cos(ang))
        self.sweep["on"].default_value = 1.0 if s["beacon"] else 0.0
        self.lamp.location = centre
        self.lamp.data.energy = 1.0 if s["beacon"] else 0.0
        self.beam.matrix_world = Matrix.Translation(centre) @ Matrix.Rotation(ang, 4, "Z") @ Matrix.Rotation(math.radians(-75), 4, "Y")  # points out, 15 deg down
        self.beam.data.energy = 250.0 if s["beacon"] else 0.0
        # muzzle flash: a stretched emissive card in front of the muzzle, two frames, and a light
        k = 1.0 if s["f"] in M.SHOTS else 0.7  # the first frame full, the second dying
        muzzle = self.tip + Vector((0, -s["slide"], 0))
        self.flash.matrix_world = W @ Matrix.Translation(muzzle + Vector((0, -0.07 * k, 0))) @ Matrix.Diagonal((0.032 * k, 0.09 * k, 0.032 * k, 1))
        self.flash.hide_render = not s["flash"]
        self.flash_light.location = W @ (muzzle + Vector((0, -0.05, 0)))
        self.flash_light.data.energy = 12.0 * k if s["flash"] else 0.0
        hit = W @ Vector((-0.27, -0.10, 0.86))  # its right shoulder
        sk = 1.0 - (s["f"] - M.HIT) / 3.0 if s["spark"] else 0.0
        self.spark.matrix_world = Matrix.Translation(hit) @ Matrix.Scale(0.06 * sk + 1e-4, 4)
        self.spark.hide_render = not s["spark"]
        self.spark_light.location = hit + Vector((-0.1, -0.1, 0))
        self.spark_light.data.energy = 40.0 * sk
        bpy.context.view_layer.update()
        return W


def follow(states, rate):
    """A critically damped follow of the robot's position (the camera's target), frame by frame."""
    out, x, v = [], Vector((*states[0]["pos"], 0)), Vector((0, 0, 0))
    for s in states:
        goal = Vector((*s["pos"], 0))
        for _ in range(4):
            dt = 1 / M.FPS / 4
            v += (rate * rate * (goal - x) - 2 * rate * v) * dt
            x += v * dt
        out.append(x.copy())
    return out


def aim(cam, target, yaw, pitch, dist):
    yaw, pitch = math.radians(yaw), math.radians(pitch)
    d = Vector((math.sin(yaw) * math.cos(pitch), -math.cos(yaw) * math.cos(pitch), math.sin(pitch)))
    cam.location = target + d * dist
    cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()


def frames_arg():
    spec = arg("--frames", f"0:{M.FRAMES}")
    if ":" in spec:
        a, b = spec.split(":")
        return list(range(int(a), int(b)))
    return [int(x) for x in spec.split(",")]


def main():
    out, faces = Path(arg("--out", "")).resolve(), Path(arg("--faces", "")).resolve()
    res = tuple(int(x) for x in arg("--res", "1920x1080").split("x"))
    kit.load()
    mats.register_robot_enamels()
    world_setup(arg("--samples", 16), res)
    robot = Robot(faces)
    person()
    states = M.build()
    sc = bpy.context.scene
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    sc.collection.objects.link(cam)
    sc.camera = cam
    cam.data.clip_start, cam.data.clip_end = 0.05, 200
    for name in arg("--passes", "close,play").split(","):
        cfg = {"close": CLOSE, "play": PLAY, "detail": DETAIL}[name]
        track = follow(states, cfg["follow"])
        if name != "play":
            cam.data.type, cam.data.lens = "PERSP", cfg["lens"]
        else:
            cam.data.type, cam.data.ortho_scale = "ORTHO", 1080 / cfg["ppm"] * 16 / 9  # 20 m tall on 1080 px, framed 16:9
        (out / name).mkdir(parents=True, exist_ok=True)
        where = {}
        for f in frames_arg():
            W = robot.pose(states[f])
            aim(cam, track[f] + Vector((0, 0, cfg["z"])), cfg["yaw"], cfg["pitch"], cfg.get("dist", 40.0))
            bpy.context.view_layer.update()
            c, top = world_to_camera_view(sc, cam, W @ Vector((0, 0, 0.62))), world_to_camera_view(sc, cam, W @ Vector((0, 0, 1.27)))
            foot = world_to_camera_view(sc, cam, W @ Vector((0, 0, 0)))
            where[f] = dict(x=c.x, y=1 - c.y, px_tall=(top.y - foot.y) * res[1])
            sc.render.filepath = str(out / name / f"{f:04d}.png")
            bpy.ops.render.render(write_still=True)
        (out / f"{name}.json").write_text(json.dumps(where))
        print(f"[demo104] {name}: {len(where)} frames -> {out / name}")


main()
