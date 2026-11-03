"""Brief 074-B quick check (Vela): one 3-storey and one 4-storey block (bld/lamplight-south-w/4 and /5, street neighbours on Lamplight South) drawn from today's world
export with their kit modules, at play zoom (ortho 28 m, pitch 38), yaw 210 and 30, in the base set and four colourways, with the materials as Unity will draw them:
DK_Limestone_Tex on the neutral panel sheet x its tint, the other four slots their colourway hex (tools/decokit/colourways.py via out/decokit/Data/colourways.json).
Each frame shows the 3-storey block in colourway A and the 4-storey in colourway B (street neighbours never share one).
Not the game's light or grade: Eevee, one sun from behind the camera's right (yaw + 50, so the faces the camera sees are lit), the DecoKit city HDRI (as 072's renders). The procedural masses stand in their wall colour, 1.25 m behind the building line (behind the modules' 1.1 m window cavity).

blender -b --factory-startup -P blocks074.py -- <out_dir> [--px 1400]
"""
import bpy, bmesh, json, math, sys, os, re, glob
from mathutils import Vector

A = sys.argv[sys.argv.index("--") + 1:]
OUT = A[0]; os.makedirs(OUT, exist_ok=True)
PX = int(A[A.index("--px") + 1]) if "--px" in A else 1400
REPO = "/Users/sky/Projects/suborned"
W = json.load(open(REPO + "/out/world/harbour-district.world.json"))
MATS = {m["name"]: m for m in json.load(open(REPO + "/out/decokit/decokit_materials.json"))["materials"]}
CW = json.load(open(REPO + "/out/decokit/Data/colourways.json"))
TEXREP = json.load(open(REPO + "/out/decokit-ground/colourways-tex.json"))
FAC = json.load(open(REPO + "/unity/Assets/Suborned/Editor/World/facade.json"))
KITP = {os.path.basename(p)[3:-4]: p for p in glob.glob(REPO + "/unity/Assets/Suborned/Kit/Models/**/*.fbx", recursive=True)}
TEXD = [REPO + "/out/decokit/Textures", REPO + "/unity/Assets/Suborned/Kit/Textures"]
HOSTS = ["bld/lamplight-south-w/4", "bld/lamplight-south-w/5"]   # 3 storeys, 4 storeys
PAIRS = [("base", "base"), ("lilac", "sky"), ("butter", "powder"), ("apricot", "mint"), ("blush", "seagreen")]
NMEAN = TEXREP["neutralMeanLinear"]
# 074-H: --soften S re-derives every family's wall with colourways.py at that WALL_SOFTEN (0 = the facade.json swatch); slots come from colourways.json
# (074-H: walls and plinth only, so band and fascia draw their base seafoam and teal). Frames are suffixed -s<S>.
SOFT = None
if "--soften" in A:
    SOFT = float(A[A.index("--soften") + 1])
    sys.path.insert(0, REPO + "/tools/decokit"); import colourways as _cwm
    for _f, _v in _cwm.family_colours(_cwm.old_cream(TEXREP["oldMeanLinear"]), SOFT).items(): CW["colours"][_f]["wall"] = _v["wall"]
SUFFIX = f"-s{SOFT:g}" if SOFT is not None else ""

def lin(c): return ((c + 0.055) / 1.055) ** 2.4 if c > 0.04045 else c / 12.92
def rgb(h): h = h.lstrip("#"); return tuple(lin(int(h[i:i + 2], 16) / 255) for i in (0, 2, 4))
def B(px, py, z=0.0): return Vector((-px, -py, z))

bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene
LIVE = bpy.data.collections.new("scene"); sc.collection.children.link(LIVE)
HIDDEN = bpy.data.collections.new("protos"); sc.collection.children.link(HIDDEN)
HIDDEN.hide_render = True

def tex_file(f):
    return next((os.path.join(d, f) for d in TEXD if os.path.exists(os.path.join(d, f))), None)

# ------------------------------------------------------------------ materials: one Blender material per (base material, colourway)
MAT = {}
def make(name, cw):
    key = (name, cw)
    if key in MAT: return MAT[key]
    e = MATS.get(name)
    m = bpy.data.materials.new(f"{name}__{cw}"); m.use_nodes = True; nt = m.node_tree; b = nt.nodes["Principled BSDF"]
    if e is None:
        b.inputs["Base Color"].default_value = (0.7, 0.7, 0.7, 1); MAT[key] = m; return m
    col = [lin(c) for c in e["color"][:3]]
    slot = CW["slots"].get(name)
    if slot and cw not in ("base", "civic"): col = list(rgb(CW["colours"][cw][slot]))
    b.inputs["Roughness"].default_value = 1 - e["smoothness"]; b.inputs["Metallic"].default_value = e["metallic"]
    if e.get("coat"): b.inputs["Coat Weight"].default_value = e["coat"]; b.inputs["Coat Roughness"].default_value = 1 - e.get("coatSmoothness", 1)
    b.inputs["Base Color"].default_value = (*col, 1)
    texf = e.get("texture")
    if texf and tex_file(texf):
        s = 1 / e["tileMetres"] if e.get("tileMetres") else 1.0
        uv = nt.nodes.new("ShaderNodeTexCoord"); mp = nt.nodes.new("ShaderNodeMapping"); mp.inputs["Scale"].default_value = (s, s, 1)
        nt.links.new(uv.outputs["UV"], mp.inputs["Vector"])
        t = nt.nodes.new("ShaderNodeTexImage"); t.image = bpy.data.images.load(tex_file(texf), check_existing=True); nt.links.new(mp.outputs["Vector"], t.inputs["Vector"])
        mix = nt.nodes.new("ShaderNodeMix"); mix.data_type = "RGBA"; mix.blend_type = "MULTIPLY"; mix.inputs["Factor"].default_value = 1.0
        tint = col
        if name == "DK_Limestone_Tex":   # the neutral sheet x the .mat's tint, as Unity multiplies them (linear)
            tint = TEXREP["baseTintLinear"] if cw in ("base", "civic") else [c / NMEAN for c in rgb(CW["colours"][cw]["wall"])]
        mix.inputs["B"].default_value = (*tint, 1)
        nt.links.new(t.outputs["Color"], mix.inputs["A"]); nt.links.new(mix.outputs["Result"], b.inputs["Base Color"])
        if e.get("kind") in ("cutout", "decal"): nt.links.new(t.outputs["Alpha"], b.inputs["Alpha"]); m.surface_render_method = "DITHERED"
        if e.get("normal") and tex_file(e["normal"]):
            tn = nt.nodes.new("ShaderNodeTexImage"); tn.image = bpy.data.images.load(tex_file(e["normal"]), check_existing=True); tn.image.colorspace_settings.name = "Non-Color"
            nt.links.new(mp.outputs["Vector"], tn.inputs["Vector"]); nm = nt.nodes.new("ShaderNodeNormalMap"); nm.inputs["Strength"].default_value = 0.8
            nt.links.new(tn.outputs["Color"], nm.inputs["Color"]); nt.links.new(nm.outputs["Normal"], b.inputs["Normal"])
    film = e.get("film")
    if film: b.inputs["Base Color"].default_value = (*rgb(film["glaze"]), 1); b.inputs["Roughness"].default_value = 0.06
    if e.get("kind") in ("emit", "emitmap") and e.get("emission"):
        b.inputs["Emission Color"].default_value = b.inputs["Base Color"].default_value[:]; b.inputs["Emission Strength"].default_value = e["emission"] * 0.5
    a = e.get("alpha", 1.0)
    if a < 1.0: b.inputs["Alpha"].default_value = a; m.surface_render_method = "BLENDED"
    MAT[key] = m; return m

# ------------------------------------------------------------------ kit protos per colourway (the same meshes, object-linked materials)
RAW = {}
def raw(kid):
    if kid in RAW: return RAW[kid]
    p = KITP.get(kid)
    if not p: RAW[kid] = None; return None
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=p, axis_forward="-Z", axis_up="Y")
    new = [o for o in bpy.data.objects if o not in before]
    col = bpy.data.collections.new("R_" + kid); HIDDEN.children.link(col)
    for o in new:
        for c in o.users_collection: c.objects.unlink(o)
        col.objects.link(o)
    pts = [o.matrix_world @ Vector(c) for o in new if o.type == "MESH" for c in o.bound_box]
    RAW[kid] = (col, min(q.z for q in pts) if pts else 0.0); return RAW[kid]

PROTO = {}
def proto(kid, cw):
    key = (kid, cw)
    if key in PROTO: return PROTO[key]
    r = raw(kid)
    if not r: PROTO[key] = None; return None
    base, minz = r
    col = bpy.data.collections.new(f"P_{kid}_{cw}"); HIDDEN.children.link(col)
    for o in base.objects:
        c = o.copy(); col.objects.link(c)
        if c.type == "MESH":
            for s in c.material_slots:
                if s.material:
                    n = re.sub(r"\.\d{3}$", "", s.material.name); s.link = "OBJECT"; s.material = make(n, cw)
    PROTO[key] = (col, minz); return PROTO[key]

KITCAT = {a["id"]: a for a in json.load(open(REPO + "/contracts/world/kit.json"))["assets"]}
def place(kit, z, cw, coll):
    pr = proto(kit["id"], cw)
    if not pr: return
    col, minz = pr; s = kit.get("scale", [1, 1, 1])
    off = KITCAT.get(kit["id"], {}).get("pivotOffset")
    if off:   # `at` is the footprint centre; the pivot sits the (scaled, turned) offset in front of it
        t = kit.get("yaw", 0.0); ox, oz = off[0] * s[0], off[1] * s[2]
        kit = dict(kit, at=[kit["at"][0] - (ox * math.cos(t) + oz * math.sin(t)), kit["at"][1] - (-ox * math.sin(t) + oz * math.cos(t))])
    e = bpy.data.objects.new("I_" + kit["id"], None); e.instance_type = "COLLECTION"; e.instance_collection = col
    e.location = B(kit["at"][0], kit["at"][1], z); e.rotation_euler = (0, 0, kit.get("yaw", 0.0)); e.scale = (s[0], s[2], s[1]); coll.objects.link(e)

def flat(name, hexv, rough=0.8):
    m = bpy.data.materials.new(name); m.use_nodes = True; b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*rgb(hexv), 1); b.inputs["Roughness"].default_value = rough; return m

def prism(name, ring, z0, z1, mat_side, mat_top, inset=1.25, coll=None):
    bm = bmesh.new()
    r = [B(x, y) for x, y in ring]
    if len(r) > 2 and (r[0] - r[-1]).length < 1e-6: r = r[:-1]
    cx = sum(p.x for p in r) / len(r); cy = sum(p.y for p in r) / len(r)
    r = [Vector((p.x + (cx - p.x) * inset / max((Vector((cx, cy, 0)) - p).length, 1e-6), p.y + (cy - p.y) * inset / max((Vector((cx, cy, 0)) - p).length, 1e-6), 0)) for p in r]
    area = sum(r[i].x * r[(i + 1) % len(r)].y - r[(i + 1) % len(r)].x * r[i].y for i in range(len(r)))
    if area < 0: r = r[::-1]
    lo = [bm.verts.new((p.x, p.y, z0)) for p in r]; hi = [bm.verts.new((p.x, p.y, z1)) for p in r]
    for i in range(len(r)):
        j = (i + 1) % len(r); bm.faces.new((lo[i], lo[j], hi[j], hi[i])).material_index = 0
    bm.faces.new(hi).material_index = 1
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    o = bpy.data.objects.new(name, me); me.materials.append(mat_side); me.materials.append(mat_top); (coll or LIVE).objects.link(o)
    return o

# ------------------------------------------------------------------ scene: one collection per frame (the two blocks in their colourways), ground shared
BLD = {b["id"]: b for b in W["buildings"]}
FRAMES = {}
for i, (ca, cb) in enumerate(PAIRS):
    coll = bpy.data.collections.new(f"F{i}"); sc.collection.children.link(coll); FRAMES[i] = coll
    for host, cw in zip(HOSTS, (ca, cb)):
        fam = cw if cw != "base" else None
        wall = CW["colours"][fam]["wall"] if fam else TEXREP["baseTint"]
        roof = FAC["swatches"][FAC["families"][fam][2]] if fam else "#f3eadb"
        for f in W["facades"]:
            if f["building"] == host and f.get("kit"): place(f["kit"], f["base"], cw, coll)
        for b in [x for k, x in BLD.items() if k == host or k.startswith(host + "/")]:
            prism("M_" + b["id"] + str(i), b["ring"], b["base"], b["base"] + b["height"], flat(f"mass{i}{host}", wall), flat(f"roof{i}{host}", roof), coll=coll)
        for r in W["roofs"]:
            if r["building"] == host and r.get("kit"): k = r["kit"]; place(dict(k, at=k.get("at", r["c"])), k.get("base", r["z"]), "base", coll)
# ground: pavement and road around
gm = bpy.data.meshes.new("ground"); bm = bmesh.new()
for (x0, x1, y0, y1, z) in ((-280, -160, -200, -80, 0.0),):
    vs = [bm.verts.new(B(x, y, z)) for x, y in ((x0, y0), (x1, y0), (x1, y1), (x0, y1))]; bm.faces.new(vs)
bm.to_mesh(gm); bm.free(); go = bpy.data.objects.new("ground", gm); gm.materials.append(flat("pave", "#d9d2c4")); sc.collection.objects.link(go)
road = bpy.data.meshes.new("road"); bm = bmesh.new()
vs = [bm.verts.new(B(x, y, 0.01)) for x, y in ((-280, -129.0), (-160, -129.0), (-160, -119.0), (-280, -119.0))]; bm.faces.new(vs)
bm.to_mesh(road); bm.free(); ro = bpy.data.objects.new("road", road); road.materials.append(flat("road", "#9fb6cf", 0.5)); sc.collection.objects.link(ro)

# ------------------------------------------------------------------ light, world, camera (072's)
eng = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
sc.render.engine = "BLENDER_EEVEE" if "BLENDER_EEVEE" in eng else "BLENDER_EEVEE_NEXT"
for k, v in (("taa_render_samples", 48), ("use_shadows", True), ("use_raytracing", True)):
    if hasattr(sc.eevee, k): setattr(sc.eevee, k, v)
sc.render.resolution_x, sc.render.resolution_y = PX, round(PX * 9 / 16)
sc.view_settings.view_transform = "AgX"; sc.view_settings.exposure = -0.2
wd = bpy.data.worlds.new("w"); sc.world = wd; wd.use_nodes = True; nt = wd.node_tree
env = nt.nodes.new("ShaderNodeTexEnvironment"); env.image = bpy.data.images.load(REPO + "/out/decokit/HDRI/city.exr")
bg = nt.nodes["Background"]; nt.links.new(env.outputs["Color"], bg.inputs["Color"]); bg.inputs["Strength"].default_value = 0.55
sun = bpy.data.lights.new("sun", "SUN"); sun.energy = 6.0; sun.color = rgb("#ffe9bc"); sun.angle = math.radians(1.2)
so = bpy.data.objects.new("sun", sun); sc.collection.objects.link(so)
def unity_fwd(pitch, yaw):
    p, y = math.radians(pitch), math.radians(yaw)
    return Vector((-math.sin(y) * math.cos(p), -math.cos(y) * math.cos(p), -math.sin(p)))
so.rotation_euler = unity_fwd(38, 330).to_track_quat("-Z", "Y").to_euler()
cd = bpy.data.cameras.new("cam"); co = bpy.data.objects.new("cam", cd); sc.collection.objects.link(co); sc.camera = co
cd.type = "ORTHO"; cd.sensor_fit = "VERTICAL"; cd.ortho_scale = 28.0; cd.clip_start = 1; cd.clip_end = 1000
YAWS = [int(v) for v in A[A.index("--yaws") + 1].split(",")] if "--yaws" in A else [210, 30]
ONLY = [int(v) for v in A[A.index("--frames") + 1].split(",")] if "--frames" in A else list(range(len(PAIRS)))
for yaw in YAWS:
    f = unity_fwd(38, yaw); co.location = B(-220, -140, 4) - f * 160; so.rotation_euler = unity_fwd(42, yaw + 50).to_track_quat("-Z", "Y").to_euler(); co.rotation_euler = f.to_track_quat("-Z", "Y").to_euler()
    for i, (ca, cb) in enumerate(PAIRS):
        if i not in ONLY: continue
        for j, c in FRAMES.items(): c.hide_render = j != i
        sc.render.filepath = os.path.join(OUT, f"blocks-{yaw}-{i}-{ca}-{cb}{SUFFIX}.png"); bpy.ops.render.render(write_still=True); print("wrote", sc.render.filepath)
print("DONE"); sys.exit(0)
