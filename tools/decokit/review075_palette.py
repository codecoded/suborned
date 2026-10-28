"""Brief 075 old palette (Vela): before / after crops of the sphere, a building run and three cars, coloured from the Unity .mat files themselves.

/Applications/Blender.app/Contents/MacOS/Blender -b -P tools/decokit/review075_palette.py -- <out_dir> <tag> <mats_root> [px]
  tag        before | after (names the files and picks the run's colourways and the cars' paint keys below)
  mats_root  a decokit output root holding Materials/*.mat and Data/{colourways,livery,carpaint}.json (out/decokit, or a snapshot of it taken before the change)
Imports each piece's FBX as catalogued in contracts/world/kit.json and colours every slot flat from the .mat Unity draws: _BaseColor (sRGB) x the base map's mean
(linear) when it has one, _Metallic and _Smoothness as written. A colourway, livery or car paint swaps a slot's base material for its variant .mat, as Latch's
builder does. Eevee, a warm sun and a teal sky, 3/4 view: hue and area only, not the game's lighting or grade. Renders only (not a decokit run).
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
OUT, TAG, ROOT = Path(A[0]), A[1], Path(A[2])
PX = int(A[3]) if len(A) > 3 else 900
OUT.mkdir(parents=True, exist_ok=True)
GEN = REPO / "out/decokit"
LEGACY = Path("~/Projects/the-suborned/game/Assets/TheSuborned/Art/DecoKit").expanduser()

RUN = ["StreamCorner_4", "StreamBay_4", "StreamBay_3W", "StreamBay_4", "StreamBay_3W", "StreamBay_4", "StreamEnd_4"]  # one street wall, three buildings
FAMS = {"before": ["butter", "apricot", "lilac"], "after": ["soft-apricot", "boulevard", "soft-celadon"]}  # before: three s6 neighbours; after: the old high street's rhythm
BUILDING_OF = [0, 0, 0, 1, 1, 2, 2]
PAINTS = {"before": ["tropical-turquoise", "matador-red", "canary-yellow"], "after": ["ivory-turquoise", "ivory-tomato", "ivory-canary"]}

kit = json.loads((REPO / "contracts/world/kit.json").read_text())
assets = {a["id"]: a for a in kit["assets"]}
data = lambda n: json.loads((ROOT / "Data" / n).read_text())  # noqa: E731
cw, lv, cp = data("colourways.json"), data("livery.json"), data("carpaint.json")
pieces_lv = (kit.get("landmarks") or {}).get("pieces") or {}


def lin(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


_guid_tex, _means = {}, {}


def tex_of(guid):
    if guid in _guid_tex:
        return _guid_tex[guid]
    hit = None
    for r in (ROOT / "Textures", GEN / "Textures", LEGACY / "Textures", REPO / "out/decokit-ground"):
        if not r.exists():
            continue
        for meta in r.glob("*.meta"):
            if f"guid: {guid}" in meta.read_text(errors="ignore"):
                hit = meta.with_suffix("")
                break
        if hit:
            break
    _guid_tex[guid] = hit
    return hit


def tex_mean(p):
    if p not in _means:
        img = bpy.data.images.load(str(p))
        px = img.pixels[:]
        _means[p] = [lin(sum(px[i::4][::97]) / len(px[i::4][::97])) for i in range(3)]
        bpy.data.images.remove(img)
    return _means[p]


def mat_file(name):
    for r in (ROOT / "Materials", LEGACY / "Materials"):
        if (r / f"{name}.mat").exists():
            return r / f"{name}.mat"
    return None


def shade(name):
    """(linear rgb, metallic, smoothness) of a .mat as Unity reads it."""
    f = mat_file(name)
    if not f:
        return [0.6, 0.6, 0.6], 0.0, 0.5
    t = f.read_text()
    c = [float(x) for x in re.search(r"    - _BaseColor: \{r: ([\d.e-]+), g: ([\d.e-]+), b: ([\d.e-]+)", t).groups()]
    c = [lin(x) for x in c]
    g = re.search(r"    - _BaseMap:\n\s+m_Texture: \{fileID: 2800000, guid: ([0-9a-f]{32})", t)
    if g and tex_of(g.group(1)):
        c = [a * b for a, b in zip(c, tex_mean(tex_of(g.group(1))))]
    num = lambda k, d: float(m.group(1)) if (m := re.search(rf"    - {k}: ([\d.e-]+)\n", t)) else d  # noqa: E731
    return c, num("_Metallic", 0.0), num("_Smoothness", 0.5)


def colour(objs, swap):
    done = {}
    for o in objs:
        for slot in o.material_slots:
            if not slot.material:
                continue
            base = slot.material.name.split(".")[0]
            want = swap.get(base, base)
            if want not in done:
                c, met, sm = shade(want)
                nm = bpy.data.materials.new(f"r_{want}")
                nm.use_nodes = True
                b = nm.node_tree.nodes["Principled BSDF"]
                b.inputs["Base Color"].default_value = (*c, 1)
                b.inputs["Metallic"].default_value = met
                b.inputs["Roughness"].default_value = 1 - sm
                done[want] = nm
            slot.material = done[want]


def fbx_of(pid):
    f = next(x for x in assets[pid]["files"] if x["role"] == "fbx")
    return (GEN if f.get("root") == "generated" else LEGACY) / f["path"]


def stage():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items.keys() else "BLENDER_EEVEE"
    sc.view_settings.view_transform = "Standard"
    w = bpy.data.worlds.new("w")
    sc.world = w
    w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.42, 0.66, 0.70, 1)
    w.node_tree.nodes["Background"].inputs[1].default_value = 0.9
    sun = bpy.data.lights.new("sun", "SUN")
    sun.energy, sun.color = 3.2, (1.0, 0.95, 0.86)
    o = bpy.data.objects.new("sun", sun)
    sc.collection.objects.link(o)
    o.rotation_euler = (math.radians(50), 0, math.radians(-35))


def imported(pid, at=Vector((0, 0, 0)), yaw=0.0):
    before = set(bpy.context.scene.objects)
    bpy.ops.import_scene.fbx(filepath=str(fbx_of(pid)))
    new = [o for o in bpy.context.scene.objects if o not in before]
    roots = [o for o in new if o.parent is None]
    for r in roots:
        r.location = r.location + at
        r.rotation_euler.z += yaw
    bpy.context.view_layer.update()
    keep = [o for o in new if o.type == "MESH" and not re.search(r"_LOD[1-9]$|lod[1-9]", o.name, re.I)]
    for o in new:
        if o.type == "MESH" and o not in keep:
            o.hide_render = True
    return keep


def bounds(objs):
    pts = [o.matrix_world @ Vector(c) for o in objs for c in o.bound_box]
    lo = Vector([min(p[i] for p in pts) for i in range(3)])
    hi = Vector([max(p[i] for p in pts) for i in range(3)])
    return lo, hi


def shoot(objs, name, w, h, yaw=30, pit=18, k=2.4):
    sc = bpy.context.scene
    sc.render.resolution_x, sc.render.resolution_y = w, h
    lo, hi = bounds(objs)
    c, size = (lo + hi) / 2, hi - lo
    cam = bpy.data.cameras.new("cam")
    cam.lens = 50
    co = bpy.data.objects.new("cam", cam)
    sc.collection.objects.link(co)
    sc.camera = co
    d = max(size.z * w / h, max(size.x, size.y)) * k * max(1.0, h / w)
    y, p = math.radians(yaw), math.radians(pit)
    co.location = c + Vector((math.sin(y) * math.cos(p), -math.cos(y) * math.cos(p), math.sin(p))) * d
    co.rotation_euler = (c - co.location).to_track_quat("-Z", "Y").to_euler()
    sc.render.filepath = str(OUT / f"{name}_{TAG}.png")
    bpy.ops.render.render(write_still=True)
    print(f"[075 palette] {name} {TAG} -> {sc.render.filepath}")


log = dict(tag=TAG, root=str(ROOT), run=[], cars=[])

# 1. the sphere, with its livery (kit.json landmarks.pieces -> the root's livery.json materials)
stage()
objs = imported("SphereStation")
colour(objs, {b: lv["keys"][k]["material"] for b, k in pieces_lv.get("SphereStation", {}).items()})
log["sphere"] = {b: lv["keys"][k]["material"] for b, k in pieces_lv.get("SphereStation", {}).items()}
shoot(objs, "sphere", PX, PX)

# 2. a building run: three buildings in one street wall, each in a colourway
stage()
allo, x = [], 0.0
fams = FAMS[TAG]
for i, pid in enumerate(RUN):
    w = assets[pid]["size"][0]
    o = imported(pid, Vector((x + w / 2, 0, 0)))
    fam = fams[BUILDING_OF[i]]
    swap = {b: by[fam] for b, by in cw["materials"].items() if fam in by}
    colour(o, swap)
    log["run"].append(dict(piece=pid, colourway=fam, swap=swap))
    allo += o
    x += w
shoot(allo, "run", int(PX * 1.6), PX, yaw=20, pit=8, k=1.75)

# 3. three cars: the official sedan's body and roof slots in three paint keys
stage()
allo = []
for i, key in enumerate(PAINTS[TAG]):
    k = cp["keys"][key]
    o = imported("Sedan_Official", Vector((i * 6.0, 0, 0)))
    sl = cp["slots"]["Sedan_Official"]
    colour(o, {sl["body"]: k["body"], sl["roof"]: k["roof"]})
    log["cars"].append(dict(key=key, body=k["body"], roof=k["roof"], bodyHex=k.get("bodyHex"), roofHex=k.get("roofHex")))
    allo += o
shoot(allo, "cars", int(PX * 1.6), PX, yaw=35, pit=20, k=1.5)
(OUT / f"log_{TAG}.json").write_text(json.dumps(log, indent=1) + "\n")
