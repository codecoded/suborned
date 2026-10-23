"""The boulevard board (Vela, brief 028 "Palm LODs"): the real district from the third-person camera, every palm at L0 and with its distance level, hazed like the game.

blender -b -P tools/decokit/palm_lod_street.py -- <world.json> <out_dir> [view ...]     views: market market-w play-market tp-street tp-worst (default the first two: Market Street looking east from the west end and west from the east end, the longest clear avenue of palms; Redline's two poses stand in open squares)
World: out/world/harbour-district.world.json (buildings as cream masses, roads, medians, the 947 palms of CoconutPalm and CoconutPalm_Street at their placements and scales).
Camera = HostFollow: 55 degrees vertical, 3.1 m up, 5.5 m behind the agent, looking 8 m ahead at 1.35 m; far clip 350 m; haze linear 90 to 350 m to #ecd9b4 (haze.json), made by the
compositor from the mist pass in scene-linear light. Writes <view>_<mode>.png (mode = L0 | LOD | LODCOLOUR), <view>_levels.json (screen position and level of every palm in view), counts.json.
Switch distances (m at scale 1): SWITCH below.
"""
import json
import math
import sys
from pathlib import Path

import bmesh
import bpy
from bpy_extras.object_utils import world_to_camera_view
from mathutils import Euler, Vector

sys.path.insert(0, str(Path(__file__).parent))
import palm_lod_scene as S  # noqa: E402

A = sys.argv[sys.argv.index("--") + 1:]
WORLD, OUT = Path(A[0]), Path(A[1]); OUT.mkdir(parents=True, exist_ok=True)
VIEWS = A[2:] or ["market", "market-w"]
POSE = {"market": (-316.0, -416.0, 90.0), "market-w": (236.0, -404.0, 270.0), "tp-street": (-159.0, -252.5, 180.0), "tp-worst": (-169.0, -257.5, 90.0)}   # Redline's two views (brief 048): agent position and heading (Unity yaw, 0 = +z)
SWITCH = (60.0, 140.0)    # metres at scale 1 in third person; the same two screen heights (RSWITCH) are what the LODGroup carries
SIZE = {"CoconutPalm": 7.94, "CoconutPalm_Street": 10.65}   # LODGroup size = the largest side of the piece's bounds (m)
TAN = math.tan(math.radians(S.FOV / 2))
RSWITCH = {k: tuple(v / (2 * TAN * d) for d in SWITCH) for k, v in SIZE.items()}   # screen-relative height at which L1 and L2 take over
PLAY = {"play-market": (-250.0, -410.0)}                                          # play zoom: orthographic, half-height 14 m, pitch 38, yaw 30
RES = (1920, 1080)
lin = lambda c: ((c + 0.055) / 1.055) ** 2.4 if c > 0.04045 else c / 12.92  # noqa: E731

w = json.load(open(WORLD))
sc, cd, co = S.setup(RES, samples=24)
sc.render.film_transparent = False
sc.view_settings.exposure = -0.3
bpy.data.objects["sun"].data.energy = 2.4
# world: the haze colour is the sky
bgn = sc.world.node_tree.nodes["Background"]; bgn.inputs[0].default_value = (*[lin(c) for c in S.HAZE], 1); bgn.inputs[1].default_value = 1.0


def mat(name, rgb, rough=0.8):
    m = bpy.data.materials.new(name); m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]; b.inputs["Base Color"].default_value = (*[lin(c) for c in rgb], 1); b.inputs["Roughness"].default_value = rough
    return m


M_GROUND, M_ROAD, M_BLD, M_MED = mat("ground", (0.93, 0.89, 0.80)), mat("road", (0.52, 0.50, 0.50)), mat("bld", (0.95, 0.91, 0.82)), mat("med", (0.70, 0.74, 0.62))


def mesh_obj(name, m, build):
    bm = bmesh.new(); build(bm)
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free(); me.materials.append(m)
    o = bpy.data.objects.new(name, me); sc.collection.objects.link(o); return o


def prism(bm, ring, z0, z1):
    vs0 = [bm.verts.new((x, y, z0)) for x, y in ring]; vs1 = [bm.verts.new((x, y, z1)) for x, y in ring]
    n = len(ring)
    for i in range(n):
        try:
            bm.faces.new((vs0[i], vs0[(i + 1) % n], vs1[(i + 1) % n], vs1[i]))
        except ValueError:
            pass
    try:
        bm.faces.new(vs1)
    except ValueError:
        pass


def ribbon(bm, line, width, z):
    pts = [Vector(p) for p in line]
    L, R = [], []
    for i, p in enumerate(pts):
        a, b = pts[max(i - 1, 0)], pts[min(i + 1, len(pts) - 1)]
        t = (b - a).normalized(); nrm = Vector((-t.y, t.x))
        L.append(bm.verts.new((*(p + nrm * width / 2), z))); R.append(bm.verts.new((*(p - nrm * width / 2), z)))
    for i in range(len(pts) - 1):
        bm.faces.new((L[i], R[i], R[i + 1], L[i + 1]))


gl = w["ground"]
mesh_obj("ground", M_GROUND, lambda bm: bm.faces.new([bm.verts.new((x, y, -0.03)) for x, y in ((-900, -1400), (900, -1400), (900, 900), (-900, 900))]))
mesh_obj("roads", M_ROAD, lambda bm: [ribbon(bm, r["line"], r["width"], 0.02) for r in gl["roads"]])
mesh_obj("medians", M_MED, lambda bm: [prism(bm, m["ring"], 0.0, m.get("height", 0.18)) for m in gl["medians"]])
mesh_obj("buildings", M_BLD, lambda bm: [prism(bm, b["ring"], b.get("base", 0), b["base"] + b["height"] if "base" in b else b["height"]) for b in w["buildings"]])

# palms: one mesh per level and piece, linked into an object each
PAL = [(s["at"][0], s["at"][1], s["kit"]["id"], s["kit"]["scale"][0], s["kit"].get("yaw", 0.0), s.get("z", 0.0)) for s in w["street"] if (s.get("kit") or {}).get("id") in S.palm_lod.PIECES]
meshes = {}
for name in S.palm_lod.PIECES:
    for lv in (0, 1, 2):
        o = S.make(name, lv); S.hide(o, True)
        # a piece is one object with one mesh; objects of children (none for the palms) would need the same
        meshes[(name, lv)] = o.data


def place(view, mode):
    for o in [o for o in bpy.data.objects if o.name.startswith("palm_")]:
        bpy.data.objects.remove(o)
    ortho = view in PLAY
    bpy.data.objects["buildings"].hide_render = ortho   # the game's architecture fade cuts the near masses in the play view
    if ortho:
        half = 14.0
        f, pch, yw = PLAY[view], math.radians(38), math.radians(30)
        tgt = Vector((f[0], f[1], 0)); dv = Vector((math.sin(yw) * math.cos(pch), -math.cos(yw) * math.cos(pch), math.sin(pch)))
        cam = tgt + dv * 400; co.location = cam; co.rotation_euler = (tgt - cam).to_track_quat("-Z", "Y").to_euler()
        sc.world.mist_settings.start = 1e6   # the play camera takes no haze (haze.json)
        cd.type = "ORTHO"; cd.sensor_fit = "VERTICAL"; cd.ortho_scale = 2 * half; cd.shift_x = cd.shift_y = 0; cd.clip_end = 900.0; cd.clip_start = 1.0
    else:
        sc.world.mist_settings.start = S.HAZE_START
        px, py, hd = POSE[view]
        th = math.radians(hd); fx, fy = math.sin(th), math.cos(th)
        # camera as HostFollow
        cam = Vector((px - fx * 5.5, py - fy * 5.5, S.EYE)); look = Vector((px + fx * 8, py + fy * 8, 1.35))
        co.location = cam; co.rotation_euler = (look - cam).to_track_quat("-Z", "Y").to_euler()
        cd.type = "PERSP"; cd.sensor_fit = "VERTICAL"; cd.angle_y = math.radians(S.FOV); cd.shift_x = cd.shift_y = 0; cd.clip_end = 350.0; cd.clip_start = 0.2
    bpy.context.view_layer.update()
    rows = []
    for i, (x, y, k, s, yaw, z) in enumerate(PAL):
        d = math.hypot(x - cam.x, y - cam.y)
        if ortho and math.hypot(x - f[0], y - f[1]) > 90:
            continue
        if not ortho and d > 380:
            continue
        r = SIZE[k] * s / (2 * half if ortho else 2 * TAN * d)   # screen-relative height, as Unity's LODGroup computes it
        lv = 0 if mode == "L0" else (0 if r >= RSWITCH[k][0] else (1 if r >= RSWITCH[k][1] else 2))
        o = bpy.data.objects.new(f"palm_{i}", meshes[(k, lv)]); o.location = (x, y, z); o.rotation_euler = (0, 0, -yaw); o.scale = (s, s, s)
        sc.collection.objects.link(o)
        tint = {0: (1.0, 0.45, 0.35), 1: (0.35, 0.9, 0.9), 2: (1.0, 0.95, 0.5)}[lv]
        p = world_to_camera_view(sc, co, Vector((x, y, z + 11 * s)))
        rows.append(dict(i=i, piece=k, level=lv, x=round(x, 1), y=round(y, 1), s=round(s, 3), d=round(d, 1), u=round(p.x, 4), v=round(p.y, 4), depth=round(p.z, 1), tris={"CoconutPalm": (6310, 1330, 514), "CoconutPalm_Street": (8230, 1330, 514)}[k][lv]))
    return rows


def compositor():
    sc.render.use_compositing = True
    sc.view_layers[0].use_pass_mist = True
    sc.world.mist_settings.start = S.HAZE_START; sc.world.mist_settings.depth = S.HAZE_END - S.HAZE_START; sc.world.mist_settings.falloff = "LINEAR"
    ng = bpy.data.node_groups.new("Comp", "CompositorNodeTree"); sc.compositing_node_group = ng
    ng.interface.new_socket("Image", in_out="OUTPUT", socket_type="NodeSocketColor")
    rl = ng.nodes.new("CompositorNodeRLayers"); go = ng.nodes.new("NodeGroupOutput")
    mx = ng.nodes.new("ShaderNodeMix"); mx.data_type = "RGBA"
    hz = ng.nodes.new("CompositorNodeRGB"); hz.outputs[0].default_value = (*[lin(c) for c in S.HAZE], 1)
    ng.links.new(rl.outputs["Mist"], mx.inputs["Factor"] if "Factor" in mx.inputs else mx.inputs[0])
    ins = [i for i in mx.inputs if i.name in ("A", "B") and i.type == "RGBA"]
    ng.links.new(rl.outputs["Image"], ins[0]); ng.links.new(hz.outputs[0], ins[1])
    out = [o for o in mx.outputs if o.type == "RGBA"][0]
    ng.links.new(out, go.inputs[0])


compositor()
counts = {"screen_relative_switch": {k: [round(x, 4) for x in v] for k, v in RSWITCH.items()}, "size_m": SIZE}
for view in VIEWS:
    for mode in ("L0", "LOD"):
        rows = place(view, mode)
        vis = [r for r in rows if 0 < r["u"] < 1 and 0 < r["v"] < 1.2 and (view in PLAY or 0 < r["depth"] < 350)]
        counts[f"{view}_{mode}"] = dict(palms=len(vis), levels={str(l): sum(1 for r in vis if r["level"] == l) for l in (0, 1, 2)}, tris=sum(r["tris"] for r in vis))
        if mode == "LOD":
            (OUT / f"{view}_levels.json").write_text(json.dumps(dict(camera=[round(co.location.x, 1), round(co.location.y, 1)], heading=None if view in PLAY else POSE[view][2], palms=vis)))
        sc.render.filepath = str(OUT / f"{view}_{mode}.png")
        bpy.ops.render.render(write_still=True)
        print("[street]", view, mode, counts[f"{view}_{mode}"])
(OUT / "counts.json").write_text(json.dumps(counts, indent=1))
