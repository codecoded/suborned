"""Brief 104 (Vela): every robot model we have made, rendered the same way. One Blender job per view:
  Blender -b -P render.py -- --view studio|play --out DIR --faces DIR [--samples 64] [--only id,id]

Same light and backdrop for all: the DecoKit studio HDRI and sun of tools/decokit/render008.py, Cycles, AgX Punchy, a transparent film over a
shadow-catcher ground (compose.py lays every tile on one board colour). Every model stands at the world origin, its foot on z 0, centred on its
footprint, its front at -Y (kit pieces as built; glTF and FBX as imported), and is seen by ONE fixed orthographic camera per view, so every tile
has the same metres per pixel and the same ground point: true relative scale.
  studio  three-quarter front: yaw 35 deg off the front, 12 deg down, 200 px/m.
  play    the play camera: orthographic size 10 (half-height 10 m) on a 1080 px tall screen = 54 px/m, 38 deg down, yaw 30 deg off the front.
Kit robots are built by tools/decokit (kit.build: what the exporter writes); their face screens get the calm dot-matrix face (faces.py), as brief 031's
render did; in the game the engine draws those faces. Store models are their glTF files as made. Writes <id>.png and <id>.json (pixel bounds, ground point)."""

import json
import math
import os
import sys
from pathlib import Path

import bpy
from bpy_extras.object_utils import world_to_camera_view
from mathutils import Matrix, Vector

REPO = Path(__file__).resolve().parents[5]
STORE = Path(os.environ.get("SUBORNED_STORE", Path.home() / "Projects/the-suborned-store"))
os.environ.setdefault("DECOKIT_TEXDIRS_EXTRA", str(REPO / "out/decokit-008-src/Textures/derived"))
sys.path.insert(0, str(REPO / "tools/decokit"))
import kit  # noqa: E402

ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []


def arg(key, default):
    return type(default)(ARGS[ARGS.index(key) + 1]) if key in ARGS else default


MOB = STORE / "testbed-art/mobility"
# id -> (kind, source, face material on the kit piece or None)
MODELS = {
    "RobotButler": ("kit", "RobotButler", "DK_Face_Amber"),
    "RobotPorter": ("kit", "RobotPorter", "DK_Face_Amber"),
    "RobotWaiter": ("kit", "RobotWaiter", "DK_Face_Teal"),
    "RobotNanny": ("kit", "RobotNanny", "DK_Face_Teal"),
    "RobotShopkeeper": ("kit", "RobotShopkeeper", "DK_Face_Teal"),
    "PatrolRobot": ("kit", "PatrolRobot", "M_RobotFace"),
    "ServiceRobot": ("kit", "ServiceRobot", None),
    "CivicRobotDock": ("kit", "CivicRobotDock", None),
    "Concierge": ("file", MOB / "Robots/RFM_RobotConciergeV1.glb", None),
    "ParcelCourier": ("file", MOB / "Robots/RFM_RobotParcelCourierV1.glb", None),
    "StreetSweeper": ("file", MOB / "Robots/RFM_RobotStreetSweeperV1.glb", None),
    "TrackedRepair": ("file", MOB / "Robots/RFM_RobotTrackedRepairV1.glb", None),
    "RobotService": ("file", MOB / "Vending/RFM_RobotServiceV1.glb", None),
    "CivicRobot": ("file", STORE / "testbed-art/civic-street-props/civic-robot-v1.glb", None),
    "Person": ("file", STORE / "source-art/Citizens3D/crowd/workers_D2__body_0__tripo_3k_front.glb", None),
    "PorterLamp": ("lamp", "RobotPorter_Play", "DK_Face_Amber"),  # the response unit today
    "PatrolLamp": ("lamp", "PatrolRobot", "M_RobotFace"),  # the response unit proposed
    "DockPatrol": ("dock", ("CivicRobotDock", "PatrolRobot"), None),  # proposals: a robot standing in the square's dock
    "DockButler": ("dock", ("CivicRobotDock", "RobotButler"), None),  # the size figure, scaled to 1.75 m; compose.py flattens it to a silhouette
}
YAW = {"Person": -90.0}  # a file whose front is not -Y as imported: degrees about Z (the Tripo figure faces +X)
VIEWS = {  # yaw off the front, pitch down, px per metre, frame (w, h) in metres, the point the camera centres on
    "studio": dict(yaw=35.0, pitch=12.0, ppm=200.0, frame=(4.0, 3.2), target=(0.0, 0.0, 1.25)),
    "play": dict(yaw=30.0, pitch=38.0, ppm=54.0, frame=(4.4, 4.4), target=(0.0, 0.0, 1.2)),
}


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
        print("[104] GPU setup failed, CPU:", e)
    sc.cycles.samples, sc.cycles.use_denoising = samples, True
    sc.render.film_transparent = True
    sc.render.image_settings.file_format, sc.render.image_settings.color_mode = "PNG", "RGBA"
    sc.view_settings.view_transform, sc.view_settings.look = "AgX", "AgX - Punchy"
    sc.view_settings.exposure = 0.35
    w = bpy.data.worlds.new("R104")
    sc.world = w
    w.use_nodes = True
    nt = w.node_tree
    env, mp, tc = nt.nodes.new("ShaderNodeTexEnvironment"), nt.nodes.new("ShaderNodeMapping"), nt.nodes.new("ShaderNodeTexCoord")
    env.image = bpy.data.images.load(str(REPO / "out/decokit-008/HDRI/studio.exr"))
    nt.links.new(tc.outputs["Generated"], mp.inputs["Vector"])
    nt.links.new(mp.outputs["Vector"], env.inputs["Vector"])
    mp.inputs["Rotation"].default_value[2] = math.radians(20.0)
    nt.links.new(env.outputs["Color"], nt.nodes["Background"].inputs["Color"])
    bpy.ops.mesh.primitive_plane_add(size=60)
    ground = bpy.context.object
    ground.name = "Ground"
    ground.is_shadow_catcher = True
    sun = bpy.data.lights.new("Sun", "SUN")
    sun.energy, sun.angle, sun.color = 4.0, math.radians(4), (1.0, 0.93, 0.8)
    so = bpy.data.objects.new("Sun", sun)
    sc.collection.objects.link(so)
    so.rotation_euler = (math.radians(30), 0, math.radians(-38))  # render008's sun from the front-left, raised to 60 deg so shadows stay short beside the next tile
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    sc.collection.objects.link(cam)
    sc.camera = cam
    return cam


def place_camera(cam, v):
    yaw, pitch = math.radians(v["yaw"]), math.radians(v["pitch"])
    d = Vector((-math.sin(yaw) * math.cos(pitch), -math.cos(yaw) * math.cos(pitch), math.sin(pitch)))  # from the target toward the camera: front-left, above
    t = Vector(v["target"])
    cam.location = t + d * 60
    cam.rotation_euler = (t - cam.location).to_track_quat("-Z", "Y").to_euler()
    cam.data.type, cam.data.clip_start, cam.data.clip_end = "ORTHO", 0.1, 200
    fw, fh = v["frame"]
    sc = bpy.context.scene
    sc.render.resolution_x, sc.render.resolution_y = round(fw * v["ppm"]), round(fh * v["ppm"])
    sc.render.resolution_percentage = 100
    cam.data.ortho_scale = max(fw, fh)  # Blender's ortho scale spans the longer side of the frame


def face_material(png):
    m = bpy.data.materials.new("Face_" + Path(png).stem)
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out, em, tx = nt.nodes.new("ShaderNodeOutputMaterial"), nt.nodes.new("ShaderNodeEmission"), nt.nodes.new("ShaderNodeTexImage")
    tx.image = bpy.data.images.load(str(png), check_existing=True)
    tx.interpolation = "Linear"
    em.inputs["Strength"].default_value = 2.0
    nt.links.new(tx.outputs["Color"], em.inputs["Color"])
    nt.links.new(em.outputs["Emission"], out.inputs["Surface"])
    return m


def import_file(path):
    before = set(bpy.data.objects)
    p = str(path)
    if p.lower().endswith((".glb", ".gltf")):
        bpy.ops.import_scene.gltf(filepath=p)
    else:
        bpy.ops.import_scene.fbx(filepath=p)
    new = [o for o in bpy.data.objects if o not in before]
    return new


def meshes_of(objs):
    return [o for o in objs if o.type == "MESH"]


def world_points(objs):
    dg = bpy.context.evaluated_depsgraph_get()
    pts = []
    for o in meshes_of(objs):
        ev = o.evaluated_get(dg)
        me = ev.to_mesh()
        pts += [ev.matrix_world @ v.co for v in me.vertices]
        ev.to_mesh_clear()
    return pts


def tri_count(objs):
    dg = bpy.context.evaluated_depsgraph_get()
    n = 0
    for o in meshes_of(objs):
        ev = o.evaluated_get(dg)
        me = ev.to_mesh()
        me.calc_loop_triangles()
        n += len(me.loop_triangles)
        ev.to_mesh_clear()
    return n


def build(mid, faces):
    kind, src, face = MODELS[mid]
    if kind == "dock":  # the square's dock with a robot standing in it: on the charging pad (z 0.165), its back short of the column (y 0.38)
        dock, _ = build(src[0], faces)
        bot, _ = build(src[1], faces)
        pts = world_points(bot)
        lo = Vector([min(p[i] for p in pts) for i in range(3)])
        hi = Vector([max(p[i] for p in pts) for i in range(3)])
        off = Vector((-(lo.x + hi.x) / 2, 0.38 - hi.y, 0.165 - lo.z))
        for r in [o for o in bot if o.parent is None or o.parent not in bot]:
            r.location = r.location + off
        bpy.context.view_layer.update()
        objs = dock + bot
        pts = world_points(objs)
        return objs, [round(max(p[i] for p in pts) - min(p[i] for p in pts), 3) for i in range(3)]
    if kind in ("kit", "lamp"):
        root = kit.build(src)
        objs = [root, *root.children_recursive]
        if face:
            fm = face_material(faces / f"{src.removesuffix('_Play')}.png")
            for o in objs:
                for slot in o.material_slots:
                    if slot.material and slot.material.name == face:
                        slot.material = fm
        if kind == "lamp":  # the response unit as the game draws it (HostViews.Drum): a 0.22 m signal sphere in the cones' red, 0.12 m above the top, over the pivot
            top = max(p.z for p in world_points(objs))
            bpy.ops.mesh.primitive_uv_sphere_add(radius=0.11, location=(0, 0, top + 0.12), segments=24, ring_count=12)
            lamp = bpy.context.object
            lm = bpy.data.materials.new("Signal_Cones")
            lm.use_nodes = True
            nt = lm.node_tree
            nt.nodes.clear()
            em, out = nt.nodes.new("ShaderNodeEmission"), nt.nodes.new("ShaderNodeOutputMaterial")
            em.inputs["Color"].default_value = (0.745, 0.042, 0.028, 1)  # #e03a2f (style contract signal_colours.cones), linear
            em.inputs["Strength"].default_value = 1.0
            nt.links.new(em.outputs["Emission"], out.inputs["Surface"])
            lamp.data.materials.append(lm)
            objs.append(lamp)
    else:
        objs = import_file(src)
    roots = [o for o in objs if o.parent is None or o.parent not in objs]
    bpy.context.view_layer.update()
    pts = world_points(objs)
    lo = Vector([min(p[i] for p in pts) for i in range(3)])
    hi = Vector([max(p[i] for p in pts) for i in range(3)])
    k = 1.75 / (hi.z - lo.z) if mid == "Person" else 1.0  # the size figure: 1.75 m
    if k != 1.0 or mid in YAW:  # about the world origin: turn to face -Y, then scale
        m = Matrix.Rotation(math.radians(YAW.get(mid, 0.0)), 4, "Z") @ Matrix.Scale(k, 4)
        for r in roots:
            r.matrix_world = m @ r.matrix_world
        bpy.context.view_layer.update()
        pts = world_points(objs)
        lo = Vector([min(p[i] for p in pts) for i in range(3)])
        hi = Vector([max(p[i] for p in pts) for i in range(3)])
    off = Vector((-(lo.x + hi.x) / 2, -(lo.y + hi.y) / 2, -lo.z))
    for r in roots:
        r.location = r.location + off
    bpy.context.view_layer.update()
    return objs, [round(hi[i] - lo[i], 3) for i in range(3)]


def px_bounds(cam, objs):
    sc = bpy.context.scene
    W, H = sc.render.resolution_x, sc.render.resolution_y
    xs, ys = [], []
    for p in world_points(objs):
        c = world_to_camera_view(sc, cam, p)
        xs.append(c.x * W)
        ys.append((1 - c.y) * H)
    g = world_to_camera_view(sc, cam, Vector((0, 0, 0)))
    return [math.floor(min(xs)), math.floor(min(ys)), math.ceil(max(xs)), math.ceil(max(ys))], [round(g.x * W, 2), round((1 - g.y) * H, 2)]


def main():
    view = arg("--view", "studio")
    v = VIEWS[view]
    out, faces = Path(arg("--out", "")).resolve(), Path(arg("--faces", "")).resolve()
    out.mkdir(parents=True, exist_ok=True)
    only = [x for x in arg("--only", "").split(",") if x]
    kit.load()
    cam = studio(arg("--samples", 64))
    place_camera(cam, v)
    for mid in (only or list(MODELS)):
        objs, size = build(mid, faces)
        bpy.context.scene.render.filepath = str(out / f"{mid}.png")
        bpy.ops.render.render(write_still=True)
        bounds, ground = px_bounds(cam, objs)
        meta = dict(id=mid, view=view, ppm=v["ppm"], yaw=v["yaw"], pitch=v["pitch"], size_xyz_m=size, tris_as_rendered=tri_count(objs), px_bounds=bounds, ground_px=ground,
                    resolution=[bpy.context.scene.render.resolution_x, bpy.context.scene.render.resolution_y])
        (out / f"{mid}.json").write_text(json.dumps(meta) + "\n")
        print(f"[104] {view} {mid} size {size} bounds {bounds}")
        for name in [o.name for o in objs]:
            if name in bpy.data.objects:
                bpy.data.objects.remove(bpy.data.objects[name], do_unlink=True)


main()
