"""Brief 075 civic restyle (Vela): the civic block (Records Office, café, square) before and after briefs/075/civic/restyle.json, at play zoom and in a
golden-style street view. Renders only: no piece is generated, no FBX written (so kit.collide.json, the export and sim/data cannot move).

/Applications/Blender.app/Contents/MacOS/Blender -b -P tools/decokit/review075_civic.py -- <out_dir> <tag> [variant] [px]
  tag      before | after   (after applies restyle.json's remap, ground and overlays)
  variant  a key of restyle.json `variants` applied on top of after (e.g. cafe-stone); "-" for none
Places every kit piece of the export's civic group and the street items round it from out/world/harbour-district.world.json exactly as KitPlacer does
(plan x, y; z; Unity euler y = -yaw, which is Blender z = pi + yaw for an FBX exported -Z forward, Y up), the two plots as 0.18 m slabs, the roads at 0,
the neighbours as plain prisms (the same in both tags). Each slot is shaded from the Unity .mat it draws (Kit/Materials, else OldTownArt): _BaseColor x
_BaseMap (tiled by m_Scale on the meshes' world-metre UVs), the detail albedo as URP does (x 4.59 linear), _Metallic, _Smoothness, clear coat on the enamels,
alpha on the glass. Eevee, a warm sun and a teal sky: material and composition, not the game's grade or lens.
Play views: orthographic half-height 14 at pitch 38 (the game's play zoom), yaw 30 and 210. Street: fov 58 vertical, eye 9.7 m, as the 075 golden view.
Also writes <tag>-probe.json: the café wall faces and pier hits that place the overlays.
"""
import json
import math
import re
import sys
from pathlib import Path

import bpy
from mathutils import Vector

REPO = Path(__file__).resolve().parents[2]
A = sys.argv[sys.argv.index("--") + 1:]
OUT, TAG = Path(A[0]), A[1]
VARIANT = A[2] if len(A) > 2 and A[2] != "-" else None
PX = int(A[3]) if len(A) > 3 else 1920
OUT.mkdir(parents=True, exist_ok=True)
GEN = REPO / "out/decokit"
LEGACY = Path("~/Projects/the-suborned/game/Assets/TheSuborned/Art/DecoKit").expanduser()
UA = REPO / "unity/Assets/Suborned"
MAT_DIRS = [UA / "Kit/Materials", UA / "OldTownArt/DecoKit/Materials"]
TEX_DIRS = [UA / "Kit/Textures", UA / "OldTownArt/DecoKit/Textures"]
RESTYLE = json.loads((REPO / "briefs/075/civic/restyle.json").read_text())
W = json.loads((REPO / "out/world/harbour-district.world.json").read_text())
KIT = {a["id"]: a for a in json.loads((REPO / "contracts/world/kit.json").read_text())["assets"]}
WIN = (-262.0, -40.0, -382.0, -140.0)  # x0, x1, y0, y1: what is drawn
NEAR = (-200.0, -105.0, -330.0, -185.0)  # street items and kit drawn here
AFTER = TAG == "after"
TAG_NAME = TAG + (f"-alt-{VARIANT}" if VARIANT else "")
log = dict(tag=TAG, variant=VARIANT, world_sha=None, pieces={}, remapped={}, ground={}, overlays=[], probe={})


def lin(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


_tex_index = None


def tex(guid):
    global _tex_index
    if _tex_index is None:
        _tex_index = {}
        for d in TEX_DIRS:
            for meta in d.glob("*.meta"):
                m = re.search(r"^guid: ([0-9a-f]{32})", meta.read_text(errors="ignore"), re.M)
                if m:
                    _tex_index.setdefault(m.group(1), meta.with_suffix(""))
    return _tex_index.get(guid)


def mat_path(ref):
    """ref: a material name (DK_X) or a path under unity/Assets/Suborned."""
    if ref.endswith(".mat"):
        return UA / ref
    for d in MAT_DIRS:
        if (d / f"{ref}.mat").exists():
            return d / f"{ref}.mat"
    return None


def prop(t, key):
    m = re.search(rf"    - {key}:\n\s+m_Texture: \{{fileID: (\d+)(?:, guid: ([0-9a-f]{{32}}))?.*\n\s+m_Scale: \{{x: ([\d.e-]+), y: ([\d.e-]+)\}}", t)
    if not m or m.group(1) == "0" or not m.group(2):
        return None
    return tex(m.group(2)), float(m.group(3)), float(m.group(4))


def num(t, k, d):
    m = re.search(rf"    - {k}: ([\d.e-]+)\n", t)
    return float(m.group(1)) if m else d


def col(t, k):
    m = re.search(rf"    - {k}: \{{r: ([\d.e-]+), g: ([\d.e-]+), b: ([\d.e-]+), a: ([\d.e-]+)", t)
    return [float(x) for x in m.groups()] if m else None


_mats = {}


def unity_mat(ref):
    """A Blender material shaded as the Unity .mat draws (cached by ref)."""
    if ref in _mats:
        return _mats[ref]
    f = mat_path(ref)
    nm = bpy.data.materials.new(f"u_{Path(ref).stem}")
    nm.use_nodes = True
    nt = nm.node_tree
    b = nt.nodes["Principled BSDF"]
    if not f:
        b.inputs["Base Color"].default_value = (0.6, 0.6, 0.6, 1)
        _mats[ref] = nm
        return nm
    t = f.read_text()
    c = col(t, "_BaseColor") or [1, 1, 1, 1]
    rgb = [lin(x) for x in c[:3]]
    uv = nt.nodes.new("ShaderNodeTexCoord")
    out_c = None
    base = prop(t, "_BaseMap")
    tint = nt.nodes.new("ShaderNodeRGB")
    tint.outputs[0].default_value = (*rgb, 1)
    out_c = tint.outputs[0]
    if base and base[0] and base[0].exists():
        mp = nt.nodes.new("ShaderNodeMapping")
        mp.inputs["Scale"].default_value = (base[1], base[2], 1)
        nt.links.new(uv.outputs["UV"], mp.inputs["Vector"])
        im = nt.nodes.new("ShaderNodeTexImage")
        im.image = bpy.data.images.load(str(base[0]), check_existing=True)
        nt.links.new(mp.outputs[0], im.inputs[0])
        mul = nt.nodes.new("ShaderNodeMix")
        mul.data_type, mul.blend_type = "RGBA", "MULTIPLY"
        mul.inputs["Factor"].default_value = 1
        nt.links.new(out_c, mul.inputs[6])
        nt.links.new(im.outputs[0], mul.inputs[7])
        out_c = mul.outputs[2]
    det = prop(t, "_DetailAlbedoMap")
    if det and det[0] and det[0].exists() and "_DETAIL_MULX2" in t:
        mp = nt.nodes.new("ShaderNodeMapping")
        mp.inputs["Scale"].default_value = (det[1], det[2], 1)
        nt.links.new(uv.outputs["UV"], mp.inputs["Vector"])
        im = nt.nodes.new("ShaderNodeTexImage")
        im.image = bpy.data.images.load(str(det[0]), check_existing=True)
        nt.links.new(mp.outputs[0], im.inputs[0])
        k = nt.nodes.new("ShaderNodeMix")
        k.data_type, k.blend_type = "RGBA", "MULTIPLY"
        k.inputs["Factor"].default_value = 1
        nt.links.new(im.outputs[0], k.inputs[6])
        k.inputs[7].default_value = (4.5948, 4.5948, 4.5948, 1)
        mul = nt.nodes.new("ShaderNodeMix")
        mul.data_type, mul.blend_type = "RGBA", "MULTIPLY"
        mul.inputs["Factor"].default_value = float(num(t, "_DetailAlbedoMapScale", 1.0))
        nt.links.new(out_c, mul.inputs[6])
        nt.links.new(k.outputs[2], mul.inputs[7])
        out_c = mul.outputs[2]
    nt.links.new(out_c, b.inputs["Base Color"])
    b.inputs["Metallic"].default_value = num(t, "_Metallic", 0.0)
    b.inputs["Roughness"].default_value = max(0.02, 1 - num(t, "_Smoothness", 0.5))
    if num(t, "_ClearCoat", 0.0) > 0 or num(t, "_ClearCoatMask", 0.0) > 0:
        b.inputs["Coat Weight"].default_value = num(t, "_ClearCoatMask", 1.0)
        b.inputs["Coat Roughness"].default_value = max(0.02, 1 - num(t, "_ClearCoatSmoothness", 0.9))
    if num(t, "_Surface", 0.0) == 1:
        b.inputs["Alpha"].default_value = c[3]
        nm.surface_render_method = "BLENDED" if hasattr(nm, "surface_render_method") else None
    if "_EMISSION" in t:
        e = col(t, "_EmissionColor")
        if e and max(e[:3]) > 0:
            b.inputs["Emission Color"].default_value = (*[min(1, x) for x in e[:3]], 1)
            b.inputs["Emission Strength"].default_value = min(4.0, max(e[:3]))
    _mats[ref] = nm
    return nm


def flat_mat(name, rgb, rough=0.8):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*[lin(x) for x in rgb], 1)
    b.inputs["Roughness"].default_value = rough
    return m


# ---------------------------------------------------------------- stage
bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene
sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items.keys() else "BLENDER_EEVEE"
sc.view_settings.view_transform = "Standard"
sc.render.resolution_x, sc.render.resolution_y = PX, PX * 9 // 16
if hasattr(sc.eevee, "use_shadows"):
    sc.eevee.use_shadows = True
if hasattr(sc.eevee, "use_raytracing"):
    sc.eevee.use_raytracing = True
sc.eevee.taa_render_samples = 32
wd = bpy.data.worlds.new("w")
sc.world = wd
wd.use_nodes = True
wd.node_tree.nodes["Background"].inputs[0].default_value = (0.42, 0.66, 0.70, 1)
wd.node_tree.nodes["Background"].inputs[1].default_value = 0.85
sun = bpy.data.lights.new("sun", "SUN")
sun.energy, sun.color, sun.angle = 3.4, (1.0, 0.95, 0.86), math.radians(1.5)
so = bpy.data.objects.new("sun", sun)
sc.collection.objects.link(so)
so.rotation_euler = (math.radians(48), 0, math.radians(-140))  # warm afternoon light from the south-west, long soft shadows

lib = bpy.data.collections.new("lib")
sc.collection.children.link(lib)
bpy.context.view_layer.layer_collection.children["lib"].exclude = True
_pieces = {}


def piece(pid):
    """The piece's L0 FBX imported once into its own collection under lib (excluded), materials shaded from the .mats."""
    if pid in _pieces:
        return _pieces[pid]
    a = KIT.get(pid)
    if not a:
        _pieces[pid] = None
        return None
    f = next((x for x in a["files"] if x["role"] == "fbx"), None)
    if not f:
        _pieces[pid] = None
        return None
    path = (GEN if f.get("root") == "generated" else LEGACY) / f["path"]
    if not path.exists():
        _pieces[pid] = None
        return None
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(path))
    new = [o for o in bpy.data.objects if o not in before]
    c = bpy.data.collections.new(f"k_{pid}")
    lib.children.link(c)
    for o in new:
        for uc in list(o.users_collection):
            uc.objects.unlink(o)
        c.objects.link(o)
        if o.type == "MESH":
            if re.search(r"_LOD[1-9]$|lod[1-9]", o.name, re.I):
                o.hide_render = True
            for s in o.material_slots:
                if s.material:
                    s.material = unity_mat(s.material.name.split(".")[0])
    _pieces[pid] = c
    log["pieces"][pid] = str(path.relative_to(REPO) if REPO in path.parents else path)
    return c


def put(pid, at, z, yaw, scale, name):
    c = piece(pid)
    if not c:
        return None
    e = bpy.data.objects.new(name, None)
    e.instance_type, e.instance_collection = "COLLECTION", c
    e.location = (at[0], at[1], z)
    e.rotation_euler = (0, 0, math.pi + yaw)  # KitPlacer: Unity euler y = -yaw; FBX -Z forward, Y up => Blender z = pi + yaw
    s = scale if isinstance(scale, (list, tuple)) else [scale] * 3
    e.scale = (s[0], s[2], s[1])
    sc.collection.objects.link(e)
    return e


def inside(p, box):
    return box[0] < p[0] < box[1] and box[2] < p[1] < box[3]


def prism(name, ring, z0, z1, mat, uv_scale=1.0):
    import bmesh
    bm = bmesh.new()
    pts = ring[:-1] if ring[0] == ring[-1] else ring
    bot = [bm.verts.new((x, y, z0)) for x, y in pts]
    top = [bm.verts.new((x, y, z1)) for x, y in pts]
    try:
        bm.faces.new(top)
    except ValueError:
        pass
    if z1 - z0 > 0.01:
        for i in range(len(pts)):
            j = (i + 1) % len(pts)
            try:
                bm.faces.new((bot[i], bot[j], top[j], top[i]))
            except ValueError:
                pass
    uvl = bm.loops.layers.uv.new()
    bm.normal_update()
    for fc in bm.faces:
        n = fc.normal
        for lp in fc.loops:
            co = lp.vert.co
            if abs(n.z) > 0.5:
                lp[uvl].uv = (co.x / uv_scale, co.y / uv_scale)
            else:
                lp[uvl].uv = ((co.x * -n.y + co.y * n.x) / uv_scale, co.z / uv_scale)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    me.materials.append(mat)
    o = bpy.data.objects.new(name, me)
    sc.collection.objects.link(o)
    return o


def rect_of_line(line, width):
    (x0, y0), (x1, y1) = line[0], line[-1]
    dx, dy = x1 - x0, y1 - y0
    L = math.hypot(dx, dy) or 1
    nx, ny = -dy / L * width / 2, dx / L * width / 2
    return [(x0 + nx, y0 + ny), (x1 + nx, y1 + ny), (x1 - nx, y1 - ny), (x0 - nx, y0 - ny)]


# ---------------------------------------------------------------- remap
remap = {}  # (piece, from) -> to (a .mat ref) or None (= the kit's)
if AFTER:
    for r in RESTYLE["remap"]:
        for p in r["pieces"]:
            remap[(p, r["from"])] = r["to"]
    if VARIANT:
        for r in RESTYLE["variants"][VARIANT]:
            for p in r["pieces"]:
                remap[(p, r["from"])] = r["to"]


def apply_remap(pid):
    c = piece(pid)
    if not c:
        return
    for o in c.all_objects:
        for s in o.material_slots:
            if not s.material:
                continue
            base = s.material.name[2:].split(".")[0]
            if (pid, base) in remap:
                to = remap[(pid, base)]
                if to:
                    s.material = unity_mat(to)
                    log["remapped"].setdefault(pid, {})[base] = to


# ---------------------------------------------------------------- ground
g = W["ground"]
asphalt = unity_mat("DK_Ground_RoadAsphalt") if mat_path("DK_Ground_RoadAsphalt") else flat_mat("asphalt", (0.62, 0.70, 0.74))
pavement = unity_mat("DK_Ground_Pavement") if mat_path("DK_Ground_Pavement") else flat_mat("pave", (0.92, 0.88, 0.80))
x0, x1, y0, y1 = WIN
prism("base-road", [(x0, y0), (x1, y0), (x1, y1), (x0, y1)], -0.05, 0.0, asphalt)
civ_ground = {r["plot"]: r for r in RESTYLE["ground"]}
for p in g["plots"]:
    r = p["ring"]
    cx, cy = sum(q[0] for q in r) / len(r), sum(q[1] for q in r) / len(r)
    if not inside((cx, cy), WIN):
        continue
    if p["surface"] in ("square", "civic"):
        if AFTER and p["id"] in civ_ground:
            m, t = unity_mat(civ_ground[p["id"]]["to"]), civ_ground[p["id"]]["tile"]
            log["ground"][p["id"]] = civ_ground[p["id"]]["to"]
        else:
            m, t = unity_mat("DK_Pave_Civic"), 4.0
        prism(p["id"], r, 0.0, 0.18, m, t)
    else:
        prism(p["id"], r, 0.0, 0.18, pavement, 1.0)
for f in g["flats"]:
    r = f["ring"]
    if inside(r[0], WIN) and f["surface"] == "road-asphalt":
        prism(f["id"], r, 0.0, 0.004, asphalt)
for d in g["drives"]:
    r = d["ring"]
    if inside(r[0], WIN):
        prism(d["id"], r, 0.0, 0.006, asphalt)
nb = flat_mat("neighbour", (0.93, 0.89, 0.80), 0.85)
for b in W["buildings"]:
    r = b["ring"]
    if inside(r[0], WIN):
        prism(b["id"], r, b.get("base", 0.0), b.get("base", 0.0) + b["height"], nb)

# ---------------------------------------------------------------- civic kit + street items
leaf, kerb = unity_mat("DK_Leaf_Civic"), unity_mat("DK_Limestone_Tex" if AFTER else "DK_Limestone")
n_civ = n_st = 0
for e in W["civic"]:
    k = e.get("kit")
    if e.get("hidden"):
        continue
    if k:
        put(k["id"], k["at"], k.get("base", e.get("base", 0.0)), k.get("yaw", 0.0), k.get("scale", 1), e["id"])
        n_civ += 1
    elif e["surface"] == "bed-kerb":
        prism(e["id"], e["ring"], e["base"], e["base"] + e["height"], kerb)
    elif e["surface"] == "bed-hedge":
        prism(e["id"], e["ring"], e["base"], e["base"] + e["height"], leaf)
for s in W["street"]:
    k = s.get("kit")
    if not k or not inside(s["at"], NEAR):
        continue
    put(k["id"], k.get("at", s["at"]), k.get("base", s.get("z", 0.0)), k.get("yaw", s.get("yaw", 0.0)), k.get("scale", 1), s["id"])
    n_st += 1
for pid in list(_pieces):
    if any(p == pid for p, _ in remap):
        apply_remap(pid)
log["counts"] = dict(civic_kit=n_civ, street_kit=n_st, pieces=len(_pieces))

# ---------------------------------------------------------------- probe: café wall faces and piers (places the overlays)
bpy.context.view_layer.update()
dg = bpy.context.evaluated_depsgraph_get()


def hit(o, d):
    ok, loc, nrm, idx, ob, mw = sc.ray_cast(dg, Vector(o), Vector(d).normalized())
    if not ok:
        return None
    mn = None
    try:
        mn = ob.material_slots[ob.data.polygons[idx].material_index].material.name
    except Exception:
        pass
    return dict(at=[round(loc.x, 3), round(loc.y, 3), round(loc.z, 3)], mat=mn, obj=ob.name)


pr = log["probe"]
for side, x, dx in (("west", -171.5, 1), ("east", -146.5, -1)):  # from just outside the café's side walls, so nothing else is hit
    for yy in (-209.0, -211.0, -213.0):
        pr[f"cafe-{side}-wall-y{yy:g}"] = {f"{z / 2:g}": (lambda h: h and [h["at"][0], h["mat"]])(hit((x, yy, z / 2), (dx, 0, 0))) for z in range(9, 34)}
pr["cafe-front"] = []
for i in range(0, 221):
    xx = -170.0 + i * 0.1
    h = hit((xx, -232.0, 2.9), (0, 1, 0))
    if h:
        pr["cafe-front"].append([round(xx, 2), h["at"][1], (h["mat"] or "")])

# ---------------------------------------------------------------- overlays
if AFTER:
    for ov in RESTYLE["overlays"]:
        e = put(ov["piece"], ov["at"], ov["z"], ov["yaw"], ov.get("scale", 1), ov["id"])
        log["overlays"].append(dict(id=ov["id"], placed=bool(e)))

# ---------------------------------------------------------------- cameras
cam = bpy.data.cameras.new("cam")
co = bpy.data.objects.new("cam", cam)
sc.collection.objects.link(co)
sc.camera = co


def play(name, tx, ty, yaw):
    """The game's play view: orthographic half-height 14 at pitch 38; Unity euler (38, yaw): forward = (sin yaw, cos yaw) in plan."""
    yw, pc = math.radians(yaw), math.radians(38)
    fwd = Vector((math.sin(yw) * math.cos(pc), math.cos(yw) * math.cos(pc), -math.sin(pc)))
    tgt = Vector((tx, ty, 0.18))
    co.location = tgt - fwd * 300
    co.rotation_euler = fwd.to_track_quat("-Z", "Y").to_euler()
    cam.type, cam.sensor_fit, cam.ortho_scale, cam.clip_start, cam.clip_end = "ORTHO", "VERTICAL", 28.0, 1.0, 900.0
    shoot(name)


def street(name, eye, look):
    co.location = Vector(eye)
    co.rotation_euler = (Vector(look) - Vector(eye)).to_track_quat("-Z", "Y").to_euler()
    cam.type, cam.sensor_fit, cam.angle_y, cam.clip_start, cam.clip_end = "PERSP", "VERTICAL", math.radians(58), 0.2, 900.0
    shoot(name)


def shoot(name):
    sc.render.filepath = str(OUT / f"{name}-{TAG_NAME}.png")
    bpy.ops.render.render(write_still=True)
    print(f"[075 civic] {name} {TAG_NAME} -> {sc.render.filepath}")


VIEWS = RESTYLE.get("views") or {}
play("play-cafe-square-y030", -160.0, -233.0, 30)
play("play-office-front-y210", -160.0, -279.0, 210)
play("play-office-entrances-y030", -168.0, -300.0, 30)
street("street-orbit-walk", (-108.0, -268.5, 9.72), (-208.0, -258.0, 10.85))
(OUT / f"{TAG_NAME}-probe.json").write_text(json.dumps(log, indent=1) + "\n")
print(f"[075 civic] done {TAG_NAME}: {log['counts']}")
