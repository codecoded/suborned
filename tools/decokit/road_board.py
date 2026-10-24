"""The road board's renders (brief 028, Vela, 2026-10-22): a slice of the real world export, now vs proposed, at the game's cameras.

blender -b -P tools/decokit/road_board.py -- <out_dir> <view> <variant> <time> [--res 1920x1080] [--placements out/vela028-road/placements.json] [--world out/world/harbour-district.world.json]
  view     office-210 | office-30   the play camera (orthographic, size 14, pitch 38) at the Records Office front (baseline stop focus -160,-281), yaw 210 / 30
           orbit-third              third person (perspective, vertical fov 55, eye 3.24 m) down Orbit Walk, 124 m, the Handover's road, looking east from its west end
           boulevard-third          third person down the Boulevard (28 m, T1 tram in the middle), 700 m, looking south
  variant  now (the build as played: asphalt, gutter, painted dashes, pearl kerb in the capstone zone) | proposed (enamel, ribbon, pearl shoulder, pinstripe, lozenges, seams, no gutter, no dashes)
           enamel | asphalt (brief 055 road test: the two finish sets of the world export, same ribbon, shoulder and pinstripe; asphalt = dark carriageway, white edge lines, dashes; read from --world)
  time     day | night
Writes <view>-<variant>-<time>.png (beauty, display sRGB, no haze) and <view>-<variant>-<time>.depth.npy (distance from the camera, m) for road_board_post.py (haze, bloom, board).
Everything is read from out/world/harbour-district.world.json (the export the game builds from) and the placements.json of tools/world/roadfinish-dev.ts (the proposed road, same generator rules as the
world generator will run). Ground, plot slabs, kerbs, masses and kit pieces are placed as the Unity builder does (plan x, y = Blender X, Y; plan yaw t turns +X to (cos t, sin t); a kit piece's
Blender frame is rotated by t + pi, its KitRef scale is (x, y up, z) in Unity axes = Blender (x, z, y)). Buildings are masses plus their kit facade pieces; the procedural window skins are not drawn.
Lighting is the light.json sun (pitch 38, yaw 330, #ffe0d0), the trilight ambient and the haze colour; not the game's render pipeline (no ACES, SSAO or contact shade): now and proposed share it."""

import json
import math
import sys
from pathlib import Path

import bpy
import bmesh
import numpy as np
from mathutils import Euler, Matrix, Vector

sys.path.insert(0, str(Path(__file__).parent))
import kit  # noqa: E402
import mats  # noqa: E402

REPO = Path(__file__).resolve().parents[2]
A = sys.argv[sys.argv.index("--") + 1:]
OUT, VIEW, VARIANT, TIME = Path(A[0]), A[1], A[2], A[3]
RES = tuple(int(x) for x in (A[A.index("--res") + 1] if "--res" in A else "1920x1080").split("x"))
PLACE = Path(A[A.index("--placements") + 1] if "--placements" in A else REPO / "out/vela028-road/placements.json")
NIGHT = TIME == "night"
EVENING = TIME == "evening"                                                              # brief 048: the testbed's evening (low warm sun, the strip light on)
NOBRASS = "--nobrass" in A                                                              # brief 061: the road edge without its brass (no pavement pinstripe, no kerb crown, no ribbon hairlines); the strip-light lens stays
X061 = A[A.index("--x061") + 1] if "--x061" in A else None                              # brief 061: crossing mock-up direction A | B | C (tools/decokit/crossing061.py); the built crossing parts are replaced
STATE = A[A.index("--state") + 1] if "--state" in A else "walk"                          # walk | clear | stop
CAM = [float(v) for v in A[A.index("--cam") + 1].split(",")] if "--cam" in A else None  # eye_x,eye_y,yaw_deg,fov,eye_height[,pitch_deg]: a custom third-person view
PLAYAT = [float(v) for v in A[A.index("--play") + 1].split(",")] if "--play" in A else None  # focus_x,focus_y,yaw_deg,r[,z]: a custom play-camera view
BILLBOARD = float(A[A.index("--billboard") + 1]) if "--billboard" in A else None         # brief 061: draw the built SignalEye as Unity's CrossingKit.Dress leaves it: turned about the vertical to the export view's yaw (30), whatever the camera does
FOOT = "--footway" in A                                                                    # brief 061 (Datum's question): Satellite Street's mouth at Market Street as a continuous footway, a sketch
ROOFS = "--roofs" in A                                                                # draw the export's roof items (prims and the programme's kit piece)
TRACK_COL = A[A.index("--track") + 1] if "--track" in A else None                     # a track colour to try instead of look.json `track` (harbour.track #96979c, metallic .5)
CIVIC_ROOF = A[A.index("--civic-roof") + 1] if "--civic-roof" in A else None            # the top colour of the civic buildings (facade.json roof_e)
PROPOSED = VARIANT in ("proposed", "enamel", "asphalt")  # every variant but "now": no gutter, no painted dashes (the road finish is in the export's ground pieces)
OUT.mkdir(parents=True, exist_ok=True)

WORLD = Path(A[A.index("--world") + 1] if "--world" in A else REPO / "out/world/harbour-district.world.json")
W = json.loads(WORLD.read_text())
LOOK = json.loads((REPO / "unity/Assets/Suborned/Editor/World/look.json").read_text())
LIGHT = json.loads((REPO / "unity/Assets/Suborned/Editor/World/light.json").read_text())
P = json.loads(PLACE.read_text()) if PLACE.exists() else {"items": [], "runs": []}
PAV = 0.18

VIEWS = {
    "office-210": dict(kind="play", focus=(-160, -281), yaw=210, r=95),
    "office-30": dict(kind="play", focus=(-160, -281), yaw=30, r=95),
    "wing-30": dict(kind="play", focus=(-160, -294), z=9, yaw=30, r=70),      # brief 048 roof board (Vela): the Records Office wing's 15.68 m roof at the play camera, roof items drawn (--roofs)
    "wing-210": dict(kind="play", focus=(-160, -294), z=9, yaw=210, r=70),
    "orbit-third": dict(kind="third", eye=(-184.0, -265.0), yaw=90, far=450, fov=55),
    "boulevard-third": dict(kind="third", eye=(-9.5, -104.0), yaw=180, far=450, fov=55),
}
if CAM:
    VIEWS[VIEW] = dict(kind="third", eye=(CAM[0], CAM[1]), yaw=CAM[2], far=160, fov=CAM[3], eyez=CAM[4], pitch=CAM[5] if len(CAM) > 5 else 3.0)
if PLAYAT:
    VIEWS[VIEW] = dict(kind="play", focus=(PLAYAT[0], PLAYAT[1]), yaw=PLAYAT[2], r=PLAYAT[3], z=PLAYAT[4] if len(PLAYAT) > 4 else 0)
V = VIEWS[VIEW]


def hexrgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) / 255 for i in (0, 2, 4))


def lin(c):
    return tuple(x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c)


def pal(key):
    d = {**LOOK["palette"], **LIGHT["palette"]}
    return hexrgb(d[key]["hex"])


# ------------------------------------------------------------------ view culling
if V["kind"] == "play":
    fx, fy = V["focus"]
    def inside(p, pad=0.0):
        return abs(p[0] - fx) < V["r"] + pad and abs(p[1] - fy) < V["r"] + pad
else:
    ex, ey = V["eye"]
    hx, hy = math.sin(math.radians(V["yaw"])), math.cos(math.radians(V["yaw"]))
    def inside(p, pad=0.0):
        dx, dy = p[0] - ex, p[1] - ey
        d = math.hypot(dx, dy)
        if d > V["far"] + pad:
            return False
        if d < 12:
            return True
        return (dx * hx + dy * hy) / d > math.cos(math.radians(48))


def ring_inside(ring, pad=0.0):
    return any(inside(p, pad) for p in ring) or inside(((min(p[0] for p in ring) + max(p[0] for p in ring)) / 2, (min(p[1] for p in ring) + max(p[1] for p in ring)) / 2), pad)


# ------------------------------------------------------------------ materials
_MAT = {}


def flat(name, rgb, rough=0.6, metal=0.0, emit=None, alpha=None):
    if name in _MAT:
        return _MAT[name]
    m = bpy.data.materials.new(name)
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*lin(rgb), 1)
    b.inputs["Roughness"].default_value = rough
    b.inputs["Metallic"].default_value = metal
    if emit:
        b.inputs["Emission Color"].default_value = (*lin(emit[0]), 1)
        b.inputs["Emission Strength"].default_value = emit[1]
    _MAT[name] = m
    return m


def surf(name):
    """look.json surface -> a Blender material: the row's DK material when it names one the generator has, else its palette colour."""
    if name in _MAT:
        return _MAT[name]
    row = LOOK["surfaces"].get(name) or LOOK["surfaces"].get("limestone")
    mt = row.get("material")
    if mt and mt in mats.LIB:
        m = mats.get(mt)
    else:
        key = row["palette"]
        c = tuple(min(1.0, x * row.get("value", 1)) for x in pal(key)) if key in {**LOOK["palette"], **LIGHT["palette"]} else (0.8, 0.78, 0.72)
        m = flat("S_" + name.replace("/", "_"), c, rough=1 - row.get("smoothness", 0.3), metal=row.get("metallic", 0))
    _MAT[name] = m
    return m


def wall_mat(surface):
    """A building wall as ribbon windows: the surface's colour with a dark glass band every storey (3.6 m), the ground storey plain. A stand-in for the skins the Unity builder draws."""
    key = "W_" + surface
    if key in _MAT:
        return _MAT[key]
    row = LOOK["surfaces"].get(surface) or LOOK["surfaces"]["limestone"]
    base = tuple(min(1.0, x * row.get("value", 1)) for x in pal(row["palette"]))
    m = bpy.data.materials.new(key)
    nt = m.node_tree
    b = nt.nodes["Principled BSDF"]
    geo, sep, mod, ramp, mixc = (nt.nodes.new(t) for t in ("ShaderNodeNewGeometry", "ShaderNodeSeparateXYZ", "ShaderNodeMath", "ShaderNodeValToRGB", "ShaderNodeMix"))
    mix = mixc
    mix.data_type = "RGBA"
    mod.operation, mod.inputs[1].default_value = "MODULO", 3.6
    nrm = nt.nodes.new("ShaderNodeMath")
    nrm.operation, nrm.inputs[1].default_value = "MULTIPLY", 1 / 3.6
    nt.links.new(geo.outputs["Position"], sep.inputs["Vector"])
    nt.links.new(sep.outputs["Z"], mod.inputs[0])
    nt.links.new(mod.outputs["Value"], nrm.inputs[0])
    ramp.color_ramp.interpolation = "CONSTANT"
    e = ramp.color_ramp.elements
    e[0].position, e[0].color = 0.0, (0, 0, 0, 1)
    e[1].position, e[1].color = 0.30, (1, 1, 1, 1)
    e.new(0.68).color = (0, 0, 0, 1)
    nt.links.new(nrm.outputs["Value"], ramp.inputs["Fac"])
    mix.inputs[6].default_value = (*lin(base), 1)
    mix.inputs[7].default_value = (*lin((0.16, 0.36, 0.38)), 1)
    nt.links.new(ramp.outputs["Color"], mix.inputs[0])
    nt.links.new(mix.outputs[2], b.inputs["Base Color"])
    b.inputs["Roughness"].default_value = 0.55
    _MAT[key] = m
    return m


# ------------------------------------------------------------------ geometry helpers
SC = bpy.context.scene


def link(o):
    for k in [o, *o.children_recursive]:
        if k.name not in SC.collection.objects:
            SC.collection.objects.link(k)
    return o


def mesh_obj(name, verts, faces, mat, uv=None, mat2=None, mats_idx=None):
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    me.materials.append(mat)
    if mat2:
        me.materials.append(mat2)
    if mats_idx:
        for p, i in zip(me.polygons, mats_idx):
            p.material_index = i
    uvl = me.uv_layers.new()
    for poly in me.polygons:
        for li in poly.loop_indices:
            v = verts[me.loops[li].vertex_index]
            uvl.data[li].uv = (v[0], v[1]) if uv is None else uv(v, poly.normal)
    me.update()
    o = bpy.data.objects.new(name, me)
    SC.collection.objects.link(o)
    return o


def flat_poly(name, pts, z, mat):
    n = len(pts)
    return mesh_obj(name, [(p[0], p[1], z) for p in pts], [list(range(n))], mat)


def slab(name, ring, z0, z1, top, side):
    """A closed prism: the top face in `top`, the walls in `side`; walls' UV = (distance along, height) in metres."""
    n = len(ring)
    verts = [(p[0], p[1], z0) for p in ring] + [(p[0], p[1], z1) for p in ring]
    faces = [list(range(n, 2 * n))] + [[i, (i + 1) % n, n + (i + 1) % n, n + i] for i in range(n)]
    o = mesh_obj(name, verts, faces, top, mat2=side, mats_idx=[0] + [1] * n)
    return o


def strip(name, line, hw, z, mat, uv=None):
    """A flat strip of half width hw along a polyline."""
    L = [tuple(p) for p in line]
    left, right = [], []
    for i, p in enumerate(L):
        a, b = L[max(i - 1, 0)], L[min(i + 1, len(L) - 1)]
        dx, dy = b[0] - a[0], b[1] - a[1]
        d = math.hypot(dx, dy) or 1
        nx, ny = -dy / d, dx / d
        left.append((p[0] + nx * hw, p[1] + ny * hw, z))
        right.append((p[0] - nx * hw, p[1] - ny * hw, z))
    verts = left + right
    n = len(L)
    faces = [[i, i + 1, n + i + 1, n + i] for i in range(n - 1)]
    return mesh_obj(name, verts, faces, mat)


# ------------------------------------------------------------------ kit pieces, cached and linked
_PROTO = {}
_BAD = set()


def proto(name):
    if name in _BAD:
        return None
    if name not in _PROTO:
        try:
            o = kit.build(name)
            if NOBRASS and (name.startswith("Kerb_Pearl") or name.startswith("RoadRibbon")):
                drop_brass(o)
            for k in [o, *o.children_recursive]:
                for c in list(k.users_collection):
                    c.objects.unlink(k)
            _PROTO[name] = o
        except Exception as e:  # noqa: BLE001
            print("[board] kit piece", name, "failed:", repr(e)[:120])
            _BAD.add(name)
            return None
    return _PROTO[name]


def drop_brass(o):
    """Brief 061: delete every face in the brass slot of a kerb or ribbon piece (the kerb's crown, the ribbon's two hairlines)."""
    for k in [o, *o.children_recursive]:
        if k.type != "MESH":
            continue
        idx = [i for i, m in enumerate(k.data.materials) if m and m.name.startswith("DK_Brass")]
        if not idx:
            continue
        bm = bmesh.new()
        bm.from_mesh(k.data)
        bm.faces.ensure_lookup_table()
        bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.material_index in idx], context="FACES")
        bm.to_mesh(k.data)
        bm.free()


def bbox_centre(o):
    xs, ys = [], []
    for k in [o, *o.children_recursive]:
        if k.type == "MESH":
            for c in k.bound_box:
                v = k.matrix_world @ Vector(c)
                xs.append(v.x)
                ys.append(v.y)
    return ((min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2) if xs else (0, 0)


def put(name, at, yaw, z, scale=(1, 1, 1), centre=False, mat_swap=None):
    """Place a kit piece as the Unity builder does: `at` the pivot (or the footprint centre when centre=True)."""
    pr = proto(name)
    if pr is None:
        return None
    th = yaw + math.pi
    sx, sy, sz = scale[0], scale[2], scale[1]  # Unity (across, up, depth) -> Blender (x, y, z)
    loc = Vector((at[0], at[1], z))
    if centre:
        cx, cy = bbox_centre(pr)
        off = Vector((cx * sx, cy * sy, 0))
        off.rotate(Euler((0, 0, th)))
        loc = loc - off
    dup = pr.copy()
    kids = []
    for c in pr.children:
        k = c.copy()
        k.parent = dup
        kids.append(k)
    dup.location = loc
    dup.rotation_euler = Euler((0, 0, th))
    dup.scale = (sx, sy, sz)
    for o in [dup, *kids]:
        SC.collection.objects.link(o)
    if mat_swap:
        pass
    return dup


# ------------------------------------------------------------------ the world
def build_world():
    G = W["ground"]
    land = surf("pavement")
    land_o = mesh_obj("land", [(-3000, -3000, -0.03), (3000, -3000, -0.03), (3000, 3000, -0.03), (-3000, 3000, -0.03)], [[0, 1, 2, 3]], flat("land", hexrgb("#d9cdb4"), 0.9))
    road_mat = mats.get("DK_Ground_RoadAsphalt") if VARIANT == "asphalt" else mats.get("DK_Ground_RoadEnamel") if PROPOSED else mats.get("DK_Ground_Asphalt")
    kerb = mats.get("DK_Ground_Kerb")
    # roads
    for r in G["roads"]:
        if not any(inside(p, 30) for p in r["line"][::6]):
            continue
        # sample down: a strip every 4 m is plenty; keep the endpoints
        line = r["line"]
        pts = [line[0]] + [p for i, p in enumerate(line[1:-1]) if i % 2 == 0] + [line[-1]]
        strip("road/" + r["id"], pts, r["width"] / 2, 0.0, road_mat)
    for c in G["circuses"]:
        if inside(c["centre"], c["radius"] + 40):
            n = 96
            ring_out = [(c["centre"][0] + c["radius"] * math.cos(2 * math.pi * k / n), c["centre"][1] + c["radius"] * math.sin(2 * math.pi * k / n)) for k in range(n)]
            ring_in = [(c["centre"][0] + c["islandRadius"] * math.cos(2 * math.pi * k / n), c["centre"][1] + c["islandRadius"] * math.sin(2 * math.pi * k / n)) for k in range(n)]
            verts = [(p[0], p[1], 0.0) for p in ring_out + ring_in]
            faces = [[k, (k + 1) % n, n + (k + 1) % n, n + k] for k in range(n)]
            mesh_obj("circus", verts, faces, road_mat)
            slab("island", ring_in, 0.0, c["islandHeight"], surf("circus/island"), kerb)
    # T1's rails down the Boulevard (two tracks 1.6 m wide, 3.6 m apart, 4 cm proud): the carriageway's own furniture, the same in both variants
    track = flat("track-try", hexrgb(TRACK_COL), 0.8, 0.1) if TRACK_COL else surf("track")
    for t in W["transit"]["tram"]:
        if not any(inside(p, 20) for p in t["line"][::8]):
            continue
        for off in ([-t["spacing"] / 2, t["spacing"] / 2] if t.get("spacing") else [0.0]):
            ln = t["line"]
            pts = []
            for i, p in enumerate(ln):
                a, b = ln[max(i - 1, 0)], ln[min(i + 1, len(ln) - 1)]
                d = math.hypot(b[0] - a[0], b[1] - a[1]) or 1
                pts.append((p[0] - (b[1] - a[1]) / d * off, p[1] + (b[0] - a[0]) / d * off))
            strip("track", pts[::2] + ([pts[-1]] if len(pts) % 2 == 0 else []), t["width"] / 2, t["level"], track)
    # plot slabs and medians
    for p in G["plots"]:
        if ring_inside(p["ring"], 20):
            slab(p["id"], p["ring"], 0.0, p["height"], mats.get("DK_Ground_PaveLimestone"), kerb)
    for f in G["flats"]:  # brief 028 (Vela): the tram reservation's lawn track (export flats `reservation/*`, surface park), under the rails
        if f["id"].startswith("reservation/") and ring_inside(f["ring"], 20):
            flat_poly(f["id"], f["ring"], f["level"], flat("lawn", hexrgb("#64875A"), 0.85))  # look.json ground/median = DK_Ground_Lawn (mean #64875A)
    for m in G["medians"]:
        if ring_inside(m["ring"], 20):
            slab(m["id"], m["ring"], 0.0, m["height"], mats.get("DK_Ground_PaveLimestone"), kerb)
    # the carriageway's gutter (now) beside each kerb run, and the painted dashes: what Latch draws today
    if not PROPOSED:
        gut = mats.get("DK_Ground_Gutter")
        for r in P.get("runs", []):
            if not (inside(r["a"], 5) or inside(r["b"], 5)):
                continue
            ax, ay = r["a"]
            bx, by = r["b"]
            n = r["n"]
            L = math.hypot(bx - ax, by - ay)
            v = [(ax, ay, 0.003), (bx, by, 0.003), (bx + n[0] * 0.375, by + n[1] * 0.375, 0.003), (ax + n[0] * 0.375, ay + n[1] * 0.375, 0.003)]
            mesh_obj("gutter", v, [[0, 1, 2, 3]], gut, uv=lambda vv, nn, ax=ax, ay=ay, nx=n[0], ny=n[1], L=L: (((vv[0] - ax) * (-ny) + (vv[1] - ay) * nx), (vv[0] - ax) * nx + (vv[1] - ay) * ny))
        dashes(G)


def dashes(G):
    """Latch's RoadMarkings today (Editor/World/Kit/RoadMarkings.cs): 2 m on, 4 m off down the centre of every carriageway 7 to 17 m wide, 6 m clear of its ends, of other roads (+1.5 m), the circus and every crossing strip (+1 m)."""
    paint = flat("paint", hexrgb(LOOK["palette"]["road.paint"]["hex"]), 0.7)
    strips = [c["strip"] for c in W["crossings"]]
    def seg_d(p, a, b):
        ax, ay, bx, by = a[0], a[1], b[0], b[1]
        dx, dy = bx - ax, by - ay
        t = max(0, min(1, ((p[0] - ax) * dx + (p[1] - ay) * dy) / (dx * dx + dy * dy or 1)))
        return math.hypot(p[0] - ax - t * dx, p[1] - ay - t * dy)
    def line_d(p, line):
        return min(seg_d(p, line[i], line[i + 1]) for i in range(len(line) - 1))
    def in_strip(p):
        for s in strips:
            xs = [q[0] for q in s]
            ys = [q[1] for q in s]
            if min(xs) - 1 <= p[0] <= max(xs) + 1 and min(ys) - 1 <= p[1] <= max(ys) + 1:
                return True
        return False
    roads = G["roads"]
    for r in roads:
        if r["width"] < 7 or r["width"] > 17:
            continue
        line = r["line"]
        cum = [0.0]
        for i in range(1, len(line)):
            cum.append(cum[-1] + math.hypot(line[i][0] - line[i - 1][0], line[i][1] - line[i - 1][1]))
        total = cum[-1]

        def at(s):
            i = 1
            while i < len(line) - 1 and cum[i] < s:
                i += 1
            t = (s - cum[i - 1]) / ((cum[i] - cum[i - 1]) or 1)
            a, b = line[i - 1], line[i]
            return (a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t)
        s = 0.0
        while s + 2 <= total:
            if 6 <= s and s + 2 <= total - 6:
                a, b = at(s), at(s + 2)
                m = ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2)
                if inside(m, 3) and not any(o["id"] != r["id"] and line_d(m, o["line"]) < o["width"] / 2 + 1.5 for o in roads) \
                        and not any(math.hypot(m[0] - c["centre"][0], m[1] - c["centre"][1]) < c["radius"] + 1.5 for c in G["circuses"]) and not in_strip(m):
                    d = ((b[0] - a[0]) / 2, (b[1] - a[1]) / 2)
                    L = math.hypot(*d) or 1
                    nx, ny = -d[1] / L * 0.06, d[0] / L * 0.06
                    mesh_obj("dash", [(a[0] - nx, a[1] - ny, 0.0025), (b[0] - nx, b[1] - ny, 0.0025), (b[0] + nx, b[1] + ny, 0.0025), (a[0] + nx, a[1] + ny, 0.0025)], [[0, 1, 2, 3]], paint)
            s += 6


def build_masses():
    for b in W["buildings"]:
        if ring_inside(b["ring"], 10) and b["height"] > 0.5:
            slab(b["id"], b["ring"], b["base"], b["base"] + b["height"], surf("roof"), wall_mat(b["surface"]))
    for c in W["civic"]:
        if c["ring"] and ring_inside(c["ring"], 10) and c.get("height", 0) > 0.05 and not c.get("kit"):
            top = flat("civic-roof", hexrgb(CIVIC_ROOF), 0.55) if (CIVIC_ROOF and c["height"] > 4 and c["surface"] == "limestone") else (surf("roof") if c["height"] > 4 else surf(c["surface"]))
            slab(c["id"], c["ring"], c["base"], c["base"] + c["height"], top, wall_mat(c["surface"]) if c["height"] > 4 else surf(c["surface"]))
    for f in W["facades"]:
        k = f.get("kit")
        if k and inside(k["at"], 10):
            put(k["id"], k["at"], k.get("yaw", 0), f["base"], tuple(k["scale"]), centre=True)
    for c in W["civic"]:
        k = c.get("kit")
        if k and k.get("at") and inside(k["at"], 10):
            put(k["id"], k["at"], k.get("yaw", 0), k.get("base", c["base"]), tuple(k["scale"]), centre=True)


ROOF_ITEM = {"column": ("#dfe3e5", 0.35, 0.9), "roof": ("#f3eadb", 0.0, 0.6), "deck": ("#efe3c8", 0.0, 0.6), "park": ("#5faa66", 0.0, 0.8), "pool": ("#6cc9d4", 0.0, 0.2),
             "sign": ("#e8609a", 0.0, 0.5), "awning": ("#e8609a", 0.0, 0.5), "landmark": ("#2f84e8", 0.0, 0.5), "water": ("#6cc9d4", 0.0, 0.2)}
ACCENTS = ["#f08a3c", "#f2c93a", "#1cb9c6", "#e8609a", "#2f84e8", "#8fe3c2"]  # facade.json: the host building's enamel pair (@A): any of the area's e_* swatches


def build_roofs():
    """facade.json roofItems: trim = the host's enamel (@A; the civic block's is seafoam), column = chrome, roof/deck = paper, park = a leaf green; kit pieces as the builder places them."""
    import hashlib
    for r in W["roofs"]:
        if not ring_inside([(r["c"][0] - 8, r["c"][1] - 8), (r["c"][0] + 8, r["c"][1] + 8)], 6):
            continue
        civic = r["building"].startswith("civic")
        acc = "#2a9d96" if civic else ACCENTS[int(hashlib.md5(r["id"].encode()).hexdigest(), 16) % len(ACCENTS)]
        for p in r["parts"]:
            if p["shape"] not in ("box", "cyl", "sphere"):
                continue
            col, met, rough = ROOF_ITEM.get(p["surface"], (acc, 0.0, 0.5)) if p["surface"] != "trim" else (acc, 0.0, 0.5)
            m = flat("ri_" + p["surface"] + (acc if p["surface"] == "trim" else ""), hexrgb(col), rough, met)
            a, b, h = p["size"]
            if p["shape"] == "box":
                bpy.ops.mesh.primitive_cube_add(size=1)
            elif p["shape"] == "cyl":
                bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=0.5, depth=1)
            else:
                bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=12, radius=0.5)
            o = bpy.context.active_object
            o.scale = (a, b, h)
            o.location = (p["c"][0], p["c"][1], p["base"] + h / 2)
            o.rotation_euler = Euler((0, 0, p["yaw"]))
            o.data.materials.append(m)
        k = r.get("kit")
        if k and k.get("at"):
            put(k["id"], k["at"], k.get("yaw", r["yaw"]), r["z"], tuple(k["scale"]))
        k = r.get("crown")
        if k and k.get("at"):
            put(k["id"], k["at"], k.get("yaw", 0), r["z"], tuple(k["scale"]))


def build_pieces():
    for s in W["ground"]["pieces"]:
        if NOBRASS and s["kind"] == "road-pinstripe":
            continue
        if FOOT and s["kind"] in ("kerb-return", "road-ribbon", "road-pinstripe") and -72.4 <= s["at"][0] <= -47.6 and -401.7 <= s["at"][1] <= -395.2:
            continue                                                                 # the mouth's kerb returns, and the ribbon and pinstripe that follow them, give way to the table
        if inside(s["at"], 6):
            put(s["kit"]["id"], s["at"], s["yaw"], s["z"], tuple(s["kit"]["scale"]))
    skip = {"flag-mast", "crossing"}
    lamps = []
    for s in W["street"]:
        k = s.get("kit")
        if not k or not inside(s["at"], 6) or s["kind"] in skip:
            continue
        yaw = k.get("yaw", s["yaw"])
        if FOOT and s.get("crossing") and -76 <= s["at"][0] <= -44 and -406 <= s["at"][1] <= -386:
            continue                                                                 # Datum's proposal: the lane mouth carries no crossing
        if BILLBOARD is not None and s["kind"] == "signal-orb":
            walk = s["signal"]["faces"][0]                                  # plan angle of face A's outward line (the walk)
            tgt = math.radians(90 - BILLBOARD)                              # the camera heading (sin yaw, cos yaw) as a plan angle
            d = (tgt - walk + math.pi / 2) % math.pi - math.pi / 2          # folded to (-90, 90]: either face may be the near one
            yaw = walk + d - math.pi / 2
        if X061 and s.get("crossing"):  # brief 061: the built crossing's studs, arcs, brass edges, field, dots and eyes give way to the candidate (heads only here; the road light is build_cross061)
            if s["kind"] == "signal-orb":
                put(f"X61{X061}_{STATE}", s["at"], yaw, s["z"], (1, 1, 1))
            continue
        put(k["id"], s["at"], yaw, s["z"], tuple(k["scale"]))
        if s["kind"] == "lamp":
            lamps.append(s)
    return lamps


def build_cross061():
    """Brief 061: the candidate's road light, laid along each crossing's walk (the same two lines at +-1.75 m, 0.5 m short of each kerb, as crossings.strip.lights; the clock drains from both kerbs toward the middle)."""
    import crossing061 as x
    st_ = "stop" if STATE == "arrive" else STATE
    on = {"walk": mats.get("DK_X61_Walk"), "clear": mats.get("DK_X61_Clear"), "stop": mats.get("DK_X61_Stop")}[st_]
    char = mats.get("DK_Enamel_Charcoal")
    frac = {"walk": 1.0, "clear": x.CLEAR_FRAC, "stop": 1.0}[st_]
    glow = {"walk": mats.get("DK_X61_HaloWalk"), "clear": mats.get("DK_X61_HaloClear"), "stop": mats.get("DK_X61_HaloStop")}[st_]
    def on_glow(a):
        key = f"X61F_{STATE}_{a:.2f}"
        if key not in _MAT:
            m = bpy.data.materials.new(key)
            b = m.node_tree.nodes["Principled BSDF"]
            col = {"walk": "#55E6B0", "clear": "#FFB624", "stop": "#F2432E"}[STATE]
            b.inputs["Base Color"].default_value = (*lin(hexrgb(col)), 1)
            b.inputs["Emission Color"].default_value = (*lin(hexrgb(col)), 1)
            b.inputs["Emission Strength"].default_value = 1.6 if STATE != "stop" else 1.1
            b.inputs["Alpha"].default_value = 0.52 * a * (0.8 if STATE == "stop" else 1.0)
            blended(m)
            _MAT[key] = m
        return _MAT[key]
    for c in W["crossings"]:
        a, b = c["line"]
        m = ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2)
        if not inside(m, 4):
            continue
        L = math.hypot(b[0] - a[0], b[1] - a[1])
        u = ((b[0] - a[0]) / L, (b[1] - a[1]) / L)
        n = (-u[1], u[0])
        z = 0.006
        s0, s1 = 0.5, L - 0.5
        span = s1 - s0
        lo, hi = s0 + span * (1 - frac) / 2, s1 - span * (1 - frac) / 2     # the lit stretch
        def P(sv, off):
            return (a[0] + u[0] * sv + n[0] * off, a[1] + u[1] * sv + n[1] * off)
        if X061 == "A":                                                       # dots d 0.22, pitch 0.5, a charcoal seat ring round each
            for off in (-1.75, 1.75):
                k = 0
                sv = s0 + 0.25
                while sv < s1:
                    lit = lo <= sv <= hi
                    cx, cy = P(sv, off)
                    for r_, mt, zz in ((0.15, char, z), (0.11, on if lit else char, z + 0.002)):
                        pts = [(cx + r_ * math.cos(t), cy + r_ * math.sin(t), zz) for t in [2 * math.pi * i / 14 for i in range(14)]]
                        mesh_obj("dot", pts, [list(range(14))], mt)
                    sv += 0.5
        elif X061 == "B":                                                     # continuous strip lights 0.14 wide, like the road ribbon's lens
            for off in (-1.75, 1.75):
                for (q0, q1, lit) in ((s0, lo, False), (lo, hi, True), (hi, s1, False)):
                    if q1 - q0 < 0.02:
                        continue
                    hw = 0.07 if lit else 0.03
                    pts = [(*P(q0, off - hw), z), (*P(q1, off - hw), z), (*P(q1, off + hw), z), (*P(q0, off + hw), z)]
                    mesh_obj("strip", pts, [[0, 1, 2, 3]], on if lit else char)
        elif X061 == "C":                                                     # one broad band of soft light across the road, 3.9 m wide, in 0.5 m cross-fade steps at the kerb ends and the lit front
            steps = 6
            for i in range(steps):
                f0, f1 = i / steps, (i + 1) / steps
                off0, off1 = -1.95 + 3.9 * f0, -1.95 + 3.9 * f1
                al = 1.0 - abs((f0 + f1) - 1.0) * 0.55                           # brighter along the middle of the band, softer toward its edges
                m_ = on_glow(al)
                pts = [(*P(lo, off0), z), (*P(hi, off0), z), (*P(hi, off1), z), (*P(lo, off1), z)]
                mesh_obj("field", pts, [[0, 1, 2, 3]], m_)


def build_footway():
    """Brief 061 sketch: Satellite Street's mouth on Market Street's north kerb (x -72 to -48 at the kerb line, 6 m returns, the lane 12 m wide) carried over as one raised table
    at pavement height (0.18), flush with the plots, in the plots' paving; the lane's traffic rides up a 2.4 m ramp (1:13) and down a 0.35 m pearl lip to Market Street; two pearl threshold bands; the
    road ribbon runs straight on along Market's kerb. No signal, no light, no crossing."""
    R, yk, yc = 6.0, -401.0, -395.0
    n = 14
    west = [(-72 + R * math.sin(math.pi / 2 * i / n), yc - R * math.cos(math.pi / 2 * i / n)) for i in range(n + 1)]
    east = [(-48 - R * math.sin(math.pi / 2 * i / n), yc - R * math.cos(math.pi / 2 * i / n)) for i in range(n + 1)]
    ring = (west + east[::-1])[::-1]
    pave, kerb, pearl, roadm = mats.get("DK_Ground_PaveLimestone"), mats.get("DK_Ground_Kerb"), mats.get("DK_Ground_RoadPearl"), mats.get("DK_Ground_RoadEnamel")
    slab("footway", ring, 0.0, PAV, pave, kerb)
    mesh_obj("ramp", [(-66, yc, PAV), (-54, yc, PAV), (-54, yc + 2.4, 0.0), (-66, yc + 2.4, 0.0)], [[0, 3, 2, 1]], pave)     # up from the lane (north) onto the table
    mesh_obj("lip", [(-72, yk, PAV), (-48, yk, PAV), (-48, yk - 0.38, 0.004), (-72, yk - 0.38, 0.004)], [[0, 1, 2, 3]], pearl)   # down to Market Street
    for y0, y1, zz in ((yc + 2.4 - 0.34, yc + 2.4, 0.004), (yc - 0.34, yc, PAV + 0.003)):
        mesh_obj("threshold", [(-66, y0, zz), (-54, y0, zz), (-54, y1, zz), (-66, y1, zz)], [[0, 1, 2, 3]], pearl)
    put("RoadRibbon_Straight_6m", (-60, yk - 0.5), 0.0, 0.0, (24.8 / 6, 1, 1))


def build_road_finish():
    for it in P["items"]:
        if inside(it["at"], 8):
            put(it["kit"]["id"], it["at"], it["yaw"], it["z"], tuple(it["kit"]["scale"]))


# ------------------------------------------------------------------ light, sky, camera
def make_world():
    w = bpy.data.worlds.new("w")
    w.use_nodes = True
    nt = w.node_tree
    for n in list(nt.nodes):
        nt.nodes.remove(n)
    tc, sep, ramp, bg, out = (nt.nodes.new(t) for t in ("ShaderNodeTexCoord", "ShaderNodeSeparateXYZ", "ShaderNodeValToRGB", "ShaderNodeBackground", "ShaderNodeOutputWorld"))
    nt.links.new(tc.outputs["Generated"], sep.inputs["Vector"])
    haze = hexrgb("#f1c9a2") if EVENING else pal("night.haze") if NIGHT else pal("light.haze")
    mid = pal("night.skyMid") if NIGHT else pal("light.skyMid")
    zen = pal("night.zenith") if NIGHT else pal("light.zenith")
    # Generated coordinates of the world run 0..1 over the direction cube: z is (dir.z + 1) / 2
    e = ramp.color_ramp.elements
    e[0].position, e[0].color = 0.50, (*lin(haze), 1)
    e[1].position, e[1].color = 1.0, (*lin(zen), 1)
    e.new(0.62).color = (*lin(mid), 1)
    nt.links.new(sep.outputs["Z"], ramp.inputs["Fac"])
    nt.links.new(ramp.outputs["Color"], bg.inputs["Color"])
    bg.inputs["Strength"].default_value = 1.0
    amb, lp, mix = nt.nodes.new("ShaderNodeBackground"), nt.nodes.new("ShaderNodeLightPath"), nt.nodes.new("ShaderNodeMixShader")
    a = pal("night.ambientEquator") if NIGHT else pal("light.ambientEquator")
    amb.inputs["Color"].default_value = (*lin(a), 1)
    amb.inputs["Strength"].default_value = 0.055 if NIGHT else 0.2 if EVENING else 0.34
    nt.links.new(lp.outputs["Is Camera Ray"], mix.inputs["Fac"])
    nt.links.new(amb.outputs["Background"], mix.inputs[1])
    nt.links.new(bg.outputs["Background"], mix.inputs[2])
    nt.links.new(mix.outputs["Shader"], out.inputs["Surface"])
    SC.world = w


def sun():
    s = bpy.data.lights.new("sun", "SUN")
    if NIGHT:
        s.energy, s.color = 0.5, pal("night.moon")
        pitch, yaw = 52, 40
    elif EVENING:
        s.energy, s.color = 2.0, hexrgb("#ffae72")
        pitch, yaw = 16, LIGHT["sun"]["yaw"]
    else:
        s.energy, s.color = 2.3, pal("light.sun")
        pitch, yaw = LIGHT["sun"]["pitch"], LIGHT["sun"]["yaw"]
    s.angle = math.radians(2.0)
    o = bpy.data.objects.new("sun", s)
    p, y = math.radians(pitch), math.radians(yaw)
    d = Vector((math.sin(y) * math.cos(p), math.cos(y) * math.cos(p), -math.sin(p)))  # the way the light travels, plan x, y, up
    o.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
    SC.collection.objects.link(o)


def camera():
    cd = bpy.data.cameras.new("cam")
    co = bpy.data.objects.new("cam", cd)
    SC.collection.objects.link(co)
    SC.camera = co
    cd.clip_start = 0.2
    if V["kind"] == "play":
        fx, fy = V["focus"]
        p, y = math.radians(38), math.radians(V["yaw"])
        fwd = Vector((math.sin(y) * math.cos(p), math.cos(y) * math.cos(p), -math.sin(p)))
        co.location = Vector((fx, fy, V.get("z", 0))) - fwd * 160
        cd.type = "ORTHO"
        cd.ortho_scale = 2 * 14 * RES[0] / RES[1]
        cd.clip_end = 400
    else:
        ex, ey = V["eye"]
        pitch = math.radians(V.get("pitch", 7.2))
        y = math.radians(V["yaw"])
        fwd = Vector((math.sin(y) * math.cos(pitch), math.cos(y) * math.cos(pitch), -math.sin(pitch)))
        co.location = Vector((ex, ey, PAV + V.get("eyez", 3.24)))
        cd.type = "PERSP"
        cd.sensor_fit = "VERTICAL"
        cd.angle_y = math.radians(V["fov"])
        cd.clip_end = V["far"] + 50
    co.rotation_euler = fwd.to_track_quat("-Z", "Y").to_euler()


def night_lights(lamps):
    cols = pal("night.lampStreet")
    k = 0
    for s in lamps:
        if not inside(s["at"], 0) or k > 70:
            continue
        l = bpy.data.lights.new("lamp", "POINT")
        l.color, l.energy, l.shadow_soft_size = cols, 260, 0.25
        o = bpy.data.objects.new("lamp", l)
        o.location = (s["at"][0], s["at"][1], s["z"] + max(s["size"][2] * 0.92, 4.5))
        SC.collection.objects.link(o)
        k += 1


def tune_road_materials():
    lens = bpy.data.materials.get("DK_Road_Lens")
    if lens:
        b = lens.node_tree.nodes["Principled BSDF"]
        b.inputs["Emission Strength"].default_value = 5.0 if NIGHT else 3.0 if EVENING else 1.4  # road_ribbon.LENS_EMISSION: the .mat is the day value; the lighting rig raises it at evening and night (light.json emissive.night.roadLens)
    pool = bpy.data.materials.get("DK_Road_Pool")
    if pool:
        pool_gradient(pool, 0.75 if NIGHT else 0.45 if EVENING else 0.0)
    for n, a in (("DK_Cross_Beam", 0.28), ("DK_Cross_Halo", 0.4), ("DK_Cross_Pool", 0.4)):
        g = bpy.data.materials.get(n)
        if g:
            g.node_tree.nodes["Principled BSDF"].inputs["Alpha"].default_value = a * (0.9 if NIGHT else 0.5)
            blended(g)


def pool_gradient(m, k):
    """The glow pool as Latch's shader draws it: alpha fades across the quad (UV v 0 at the lens to 1 at the outer edge)."""
    nt = m.node_tree
    b = nt.nodes["Principled BSDF"]
    uv, sep, inv, sq, mul = (nt.nodes.new(t) for t in ("ShaderNodeTexCoord", "ShaderNodeSeparateXYZ", "ShaderNodeMath", "ShaderNodeMath", "ShaderNodeMath"))
    inv.operation, inv.inputs[0].default_value = "SUBTRACT", 1.0
    sq.operation, sq.inputs[1].default_value = "POWER", 2.2
    mul.operation, mul.inputs[1].default_value = "MULTIPLY", k
    nt.links.new(uv.outputs["UV"], sep.inputs["Vector"])
    nt.links.new(sep.outputs["Y"], inv.inputs[1])
    nt.links.new(inv.outputs["Value"], sq.inputs[0])
    nt.links.new(sq.outputs["Value"], mul.inputs[0])
    nt.links.new(mul.outputs["Value"], b.inputs["Alpha"])
    blended(m)


def blended(m):
    if hasattr(m, "surface_render_method"):
        m.surface_render_method = "BLENDED"
    m.use_backface_culling = False


def shoot(name):
    """Render and read back the float pixels (linear, view transform Raw) as (H, W, 3); the EXR is kept only long enough to read."""
    SC.render.image_settings.file_format = "OPEN_EXR"
    SC.render.image_settings.color_depth = "32"
    SC.render.image_settings.color_mode = "RGBA"
    base = OUT / ("_tmp_" + name)
    SC.render.filepath = str(base)
    bpy.ops.render.render(write_still=True)
    f = Path(str(base) + ".exr")
    img = bpy.data.images.load(str(f))
    a = np.array(img.pixels[:], dtype=np.float32).reshape(RES[1], RES[0], 4)[::-1]
    bpy.data.images.remove(img)
    f.unlink()
    return a


def depth_pass(name):
    """Distance from the camera: a second render in which every surface emits it."""
    mt = bpy.data.materials.new("depth")
    nt = mt.node_tree
    for n in list(nt.nodes):
        nt.nodes.remove(n)
    geo, vec, ln, em, out = nt.nodes.new("ShaderNodeNewGeometry"), nt.nodes.new("ShaderNodeVectorMath"), nt.nodes.new("ShaderNodeVectorMath"), nt.nodes.new("ShaderNodeEmission"), nt.nodes.new("ShaderNodeOutputMaterial")
    vec.operation = "SUBTRACT"
    cam = SC.camera.location
    vec.inputs[1].default_value = (cam.x, cam.y, cam.z)
    ln.operation = "LENGTH"
    sc2 = nt.nodes.new("ShaderNodeMath")
    sc2.operation, sc2.inputs[1].default_value = "MULTIPLY", 1.0 / 100.0
    nt.links.new(geo.outputs["Position"], vec.inputs[0])
    nt.links.new(vec.outputs["Vector"], ln.inputs[0])
    nt.links.new(ln.outputs["Value"], sc2.inputs[0])
    nt.links.new(sc2.outputs["Value"], em.inputs["Color"])
    nt.links.new(em.outputs["Emission"], out.inputs["Surface"])
    SC.view_layers[0].material_override = mt
    SC.render.filter_size = 0.0
    SC.render.film_transparent = True
    a = shoot("depth")
    SC.render.film_transparent = False
    d = a[..., 0] * 100.0
    d[a[..., 3] < 0.5] = np.inf  # sky: no surface
    np.save(str(OUT / name), d)
    SC.view_layers[0].material_override = None


def main():
    global SC
    bpy.ops.wm.read_factory_settings(use_empty=True)
    SC = bpy.context.scene
    kit.load()
    if X061:
        import crossing061  # noqa: F401  (brief 061: registers the candidate signal pieces and their light materials)
    mats.register_robot_enamels()
    SC.render.engine = "BLENDER_EEVEE"
    SC.render.resolution_x, SC.render.resolution_y = RES
    SC.view_settings.view_transform = "Raw"
    make_world()
    sun()
    build_world()
    build_masses()
    if ROOFS:
        build_roofs()
    lamps = build_pieces()
    if PROPOSED:
        build_road_finish()
    if X061:
        build_cross061()
    if FOOT:
        build_footway()
    tune_road_materials()
    if NIGHT:
        night_lights(lamps)
    camera()
    tag = f"{VIEW}-{VARIANT}-{TIME}"
    np.save(str(OUT / f"{tag}.lin.npy"), shoot("beauty")[..., :3])
    depth_pass(f"{tag}.depth.npy")
    print("[board] wrote", tag, "objects", len(SC.objects), "protos", len(_PROTO), "bad", sorted(_BAD))


main()
