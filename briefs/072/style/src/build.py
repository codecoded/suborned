"""Brief 072-B (Vela, scratch): today's style against the loosened proposal on the same play street, same geometry and light.

blender -b --factory-startup -P build.py -- <out_dir> [--px 1600] [--views a,b,c] [--only-before]

Scene (from the world export copy world.json, box x -235..-85, y -345..-155): the game's own kit FBXs (unity/Assets/Suborned/Kit/Models) at their export
placements (civic, facades, frontage signs, roofs, ground pieces, street items), and stand-ins for what Unity draws procedurally: building masses with
windows from the skins (the ProcFacade look: one flat swatch per part), shopfront boxes, roof parts, plots and roads as flat colour.
Kit materials come from decokit_materials.json (materials.today.json, copied); proc colours from facade.json and look.json (read only).
The scene is built once; 'before' renders with today's values, then the overrides in overrides.json recolour materials in place (no geometry change)
and 'after' renders with the same cameras and light. Not the game's lighting or grade: Eevee, one sun at the game's angle, the DecoKit city HDRI.
"""
import bpy, bmesh, json, math, sys, hashlib, re, os, glob
from mathutils import Vector, Matrix

A = sys.argv[sys.argv.index("--") + 1:]
OUT = A[0]; os.makedirs(OUT, exist_ok=True)
def opt(k, d): return type(d)(A[A.index(k) + 1]) if k in A else d
PX = opt("--px", 1600)
HERE = os.path.dirname(os.path.abspath(__file__))
REPO = "/Users/sky/Projects/suborned"
W = json.load(open(os.path.join(HERE, "world.json")))
MATS = {m["name"]: m for m in json.load(open(os.path.join(HERE, "materials.today.json")))["materials"]}
FAC = json.load(open(REPO + "/unity/Assets/Suborned/Editor/World/facade.json"))
LOOK = {k.split(".", 1)[1] if k.startswith("harbour.") else k: v["hex"] for k, v in json.load(open(REPO + "/unity/Assets/Suborned/Editor/World/look.json"))["palette"].items()}
CIV = json.load(open(REPO + "/contracts/world/civic-look.json"))
OVR = json.load(open(os.path.join(HERE, "overrides.json")))
KITP = {os.path.basename(p)[3:-4]: p for p in glob.glob(REPO + "/unity/Assets/Suborned/Kit/Models/**/*.fbx", recursive=True)}
TEXD = [REPO + "/unity/Assets/Suborned/Kit/Textures", REPO + "/out/decokit/Textures", REPO + "/out/decokit-ground"]
X0, X1, Y0, Y1 = -235, -55, -345, -150
inb = lambda p: X0 <= p[0] <= X1 and Y0 <= p[1] <= Y1

bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene

def lin(c): return ((c + 0.055) / 1.055) ** 2.4 if c > 0.04045 else c / 12.92
def rgb(h): h = h.lstrip("#"); return tuple(lin(int(h[i:i + 2], 16) / 255) for i in (0, 2, 4))
def B(px, py, z=0.0): return Vector((-px, -py, z))   # plan (Unity x, z) -> Blender: the FBX export's own axis map, so kit pieces need no extra turn

# ------------------------------------------------------------------ materials
BSDF = {}
def tex_file(f):
    for d in TEXD:
        p = os.path.join(d, f)
        if os.path.exists(p): return p
    return None

def principled(name):
    m = bpy.data.materials.new(name); m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]; BSDF[name] = (m, b); return m, b

def add_tex(m, b, f, scale, nonc=False, alpha=False, normal=None, world=False):
    nt = m.node_tree; p = tex_file(f)
    if not p: return None
    uv = nt.nodes.new("ShaderNodeTexCoord"); mp = nt.nodes.new("ShaderNodeMapping")
    mp.inputs["Scale"].default_value = (scale[0], scale[1], 1); nt.links.new(uv.outputs["Object" if world else "UV"], mp.inputs["Vector"])
    t = nt.nodes.new("ShaderNodeTexImage"); t.image = bpy.data.images.load(p, check_existing=True); nt.links.new(mp.outputs["Vector"], t.inputs["Vector"])
    mix = nt.nodes.new("ShaderNodeMix"); mix.data_type = "RGBA"; mix.blend_type = "MULTIPLY"; mix.inputs["Factor"].default_value = 1.0
    mix.name = "tint"; mix.inputs["B"].default_value = b.inputs["Base Color"].default_value[:]
    nt.links.new(t.outputs["Color"], mix.inputs["A"]); nt.links.new(mix.outputs["Result"], b.inputs["Base Color"])
    if alpha: nt.links.new(t.outputs["Alpha"], b.inputs["Alpha"])
    if normal and tex_file(normal):
        tn = nt.nodes.new("ShaderNodeTexImage"); tn.image = bpy.data.images.load(tex_file(normal), check_existing=True); tn.image.colorspace_settings.name = "Non-Color"
        nt.links.new(mp.outputs["Vector"], tn.inputs["Vector"]); nm = nt.nodes.new("ShaderNodeNormalMap"); nm.inputs["Strength"].default_value = 0.8
        nt.links.new(tn.outputs["Color"], nm.inputs["Color"]); nt.links.new(nm.outputs["Normal"], b.inputs["Normal"])
    return t

def set_col(name, hexstr):
    m, b = BSDF[name]; c = (*rgb(hexstr), 1)
    b.inputs["Base Color"].default_value = c
    t = m.node_tree.nodes.get("tint")
    if t: t.inputs["B"].default_value = c
    if b.inputs["Emission Strength"].default_value > 0 and not b.inputs["Emission Color"].is_linked: b.inputs["Emission Color"].default_value = c

KITMAT = {}
def kit_mat(raw, world=None):
    name = re.sub(r"\.\d{3}$", "", raw)
    key = name + ("#w" if world else "")
    if key in KITMAT: return KITMAT[key]
    e = MATS.get(name)
    m, b = principled(key)
    if e is None:
        b.inputs["Base Color"].default_value = (0.8, 0.8, 0.8, 1); KITMAT[key] = m; return m
    k = e["kind"]; col = e["color"]
    b.inputs["Base Color"].default_value = (*[lin(c) for c in col[:3]], 1)
    b.inputs["Roughness"].default_value = 1 - e["smoothness"]; b.inputs["Metallic"].default_value = e["metallic"]
    if e.get("coat"): b.inputs["Coat Weight"].default_value = e["coat"]; b.inputs["Coat Roughness"].default_value = 1 - e.get("coatSmoothness", 1)
    pbr = e.get("pbr")
    if pbr:
        t = world or pbr["tile"]
        add_tex(m, b, pbr["albedo"], (1 / t[0], 1 / t[1]), normal=pbr.get("normal"), world=bool(world)); b.inputs["Roughness"].default_value = 0.7
        if pbr.get("tint"): b.inputs["Base Color"].default_value = (*rgb(pbr["tint"]), 1); m.node_tree.nodes["tint"].inputs["B"].default_value = (*rgb(pbr["tint"]), 1)
    elif e.get("texture"):
        s = 1 / e["tileMetres"] if e.get("tileMetres") else 1.0
        add_tex(m, b, e["texture"], (s, s), alpha=k in ("cutout", "decal"), normal=e.get("normal") or None)
    if k in ("emit", "emitmap", "glow") and e.get("emission"):
        b.inputs["Emission Color"].default_value = b.inputs["Base Color"].default_value[:]; b.inputs["Emission Strength"].default_value = e["emission"] * 0.5
    film = e.get("film")
    if film:
        b.inputs["Base Color"].default_value = (*rgb(film["glaze"]), 1); b.inputs["Roughness"].default_value = 0.06
    a = e.get("alpha", 1.0)
    if k == "face": a = 0.0
    if k == "glow": a = min(a, 0.25)
    if a < 1.0: b.inputs["Alpha"].default_value = a; m.surface_render_method = "BLENDED"
    if k in ("cutout", "decal"): m.surface_render_method = "DITHERED"
    KITMAT[key] = m; return m

# proc swatches: one material per (swatch hex source, class); colours resolved through PAL (facade swatches + look roles)
PAL = dict(FAC["swatches"]); PAL.update({"look:" + k: v for k, v in LOOK.items()})
CLS = {"matte": (1 - FAC["materials"]["matte"], 0.0), "glossy": (1 - FAC["materials"]["glossy"], 0.0), "glass": (0.08, 0.0), "chrome": (0.15, 1.0), "brass": (0.4, 0.5)}
SWM = {}
def sw(name, cls="matte"):
    key = (name, cls)
    if key in SWM: return SWM[key]
    m, b = principled(f"SW_{name}_{cls}")
    b.inputs["Base Color"].default_value = (*rgb(PAL[name]), 1); r, me = CLS[cls]; b.inputs["Roughness"].default_value = r; b.inputs["Metallic"].default_value = me
    SWM[key] = m; return m

# ------------------------------------------------------------------ kit protos (collection instances)
PROTO = {}; HIDDEN = bpy.data.collections.new("protos"); sc.collection.children.link(HIDDEN)
def proto(kid, swaps=None):
    key = (kid, tuple(sorted((swaps or {}).items())))
    if key in PROTO: return PROTO[key]
    p = KITP.get(kid)
    if not p: PROTO[key] = None; return None
    col = bpy.data.collections.new("P_" + kid + ("_s%d" % len(PROTO) if swaps else "")); HIDDEN.children.link(col)
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=p, axis_forward="-Z", axis_up="Y")
    new = [o for o in bpy.data.objects if o not in before]
    for o in new:
        for c in o.users_collection: c.objects.unlink(o)
        col.objects.link(o)
        if o.type == "MESH":
            for s in o.material_slots:
                if s.material:
                    n = re.sub(r"\.\d{3}$", "", s.material.name); n = (swaps or {}).get(n, n)
                    s.material = kit_mat(n)
    pts = [o.matrix_world @ Vector(c) for o in new if o.type == "MESH" for c in o.bound_box]
    minz = min(q.z for q in pts) if pts else 0.0
    PROTO[key] = (col, minz); return PROTO[key]

LIVE = bpy.data.collections.new("scene"); sc.collection.children.link(LIVE)
NPL = [0]
def place(kit, at, z, yaw=None, swaps=None, lowest=None):
    pr = proto(kit["id"], swaps)
    if not pr: return
    col, minz = pr; s = kit.get("scale", [1, 1, 1]); y = kit.get("yaw", 0.0) if yaw is None else yaw
    if lowest is not None: z = lowest - minz * s[1]
    e = bpy.data.objects.new("I_" + kit["id"], None); e.instance_type = "COLLECTION"; e.instance_collection = col
    e.location = B(at[0], at[1], z); e.rotation_euler = (0, 0, y); e.scale = (s[0], s[2], s[1]); LIVE.objects.link(e); NPL[0] += 1

# ------------------------------------------------------------------ mesh helpers (one bmesh per host, material slots by role)
class Mesh:
    def __init__(s, name, roles):
        s.name = name; s.roles = roles; s.bm = bmesh.new()
    def quad(s, pts, role):
        vs = [s.bm.verts.new(p) for p in pts]; f = s.bm.faces.new(vs); f.material_index = s.roles.index(role); return f
    def box(s, c, size, yaw, z0, role, top=None):
        hx, hy = size[0] / 2, size[1] / 2; cs, sn = math.cos(yaw), math.sin(yaw)
        cor = [(c[0] + cs * x - sn * y, c[1] + sn * x + cs * y) for x, y in ((-hx, -hy), (hx, -hy), (hx, hy), (-hx, hy))]
        s.prism(cor, z0, z0 + size[2], role, top or role)
    def prism(s, ring, z0, z1, role, top=None, cap=True):
        r = [B(x, y) for x, y in ring]
        if len(r) > 2 and r[0] == r[-1]: r = r[:-1]
        area = sum(r[i].x * r[(i + 1) % len(r)].y - r[(i + 1) % len(r)].x * r[i].y for i in range(len(r)))
        if area < 0: r = r[::-1]
        n = len(r)
        lo = [s.bm.verts.new((p.x, p.y, z0)) for p in r]; hi = [s.bm.verts.new((p.x, p.y, z1)) for p in r]
        for i in range(n):
            j = (i + 1) % n
            try: f = s.bm.faces.new((lo[i], lo[j], hi[j], hi[i])); f.material_index = s.roles.index(role)
            except ValueError: pass
        if cap:
            try: f = s.bm.faces.new(hi); f.material_index = s.roles.index(top or role)
            except ValueError: pass
    def cyl(s, c, d, z0, h, role, seg=16):
        ring = [(c[0] + d / 2 * math.cos(2 * math.pi * i / seg), c[1] + d / 2 * math.sin(2 * math.pi * i / seg)) for i in range(seg)]
        s.prism(ring, z0, z0 + h, role)
    def sphere(s, c, d, z0, role):
        bmesh.ops.create_uvsphere(s.bm, u_segments=16, v_segments=10, radius=d / 2, matrix=Matrix.Translation(B(c[0], c[1], z0 + d / 2)))
        for f in s.bm.faces:
            if f.material_index == 0 and not f.tag: f.material_index = s.roles.index(role); f.tag = True
    def done(s, slots):
        me = bpy.data.meshes.new(s.name); s.bm.to_mesh(me); s.bm.free()
        o = bpy.data.objects.new(s.name, me); LIVE.objects.link(o)
        for r in s.roles: me.materials.append(slots[r])
        for p in me.polygons: p.use_smooth = False
        return o

# ------------------------------------------------------------------ hosts (facade.json Fill, with an md5 die instead of the game's Dice)
def die(s, salt=""): return int(hashlib.md5((s + salt).encode()).hexdigest(), 16)
def host_of(mid):
    i = mid.rfind("/")
    return mid[:i] if i > 0 and mid[i:] in ("/podium", "/body", "/upper", "/lower") else mid
FR_AREA = {}
for f in W["frontages"]: FR_AREA.setdefault(f["host"], []).append(f["area"])
HOSTS = {}
def host(hid, ovr=None):
    fam_tab, areas = FAC["families"], FAC["areas"]
    a = None
    for k, v in FAC["hostAreas"].items():
        if hid == k or hid.startswith(k + "/"): a = v
    if a is None:
        fa = FR_AREA.get(hid)
        a = max(set(fa), key=fa.count) if fa else next((x for x in areas if x in hid), "records" if hid.startswith("civic") else "harbour")
    if a not in areas: a = "harbour"
    fams = areas[a]["families"]; tot = sum(fams.values()); r = die(hid) % tot
    for name, wgt in fams.items():
        if r < wgt: fam = fam_tab[name]; break
        r -= wgt
    en = areas[a]["enamel"]; ia = die(hid, "a") % len(en); ib_ = (ia + 1 + die(hid, "b") % max(1, len(en) - 1)) % len(en)
    roof = FAC["roofs"][die(hid, "r") % len(FAC["roofs"])] if die(hid, "q") % 3 == 0 else fam[2]
    return dict(area=a, wall=fam[0], plinth=fam[1], roof=roof, A=en[ia], B=en[ib_], trim=areas[a].get("trim", "white"))

ROLES = ["wall", "plinth", "roof", "A", "B", "trim", "glass", "chrome", "white", "acc", "glass_t"]
HOSTMESH = []   # (object, host id, skin accent) for the after pass
def slots_for(h, acc):
    return {"wall": sw(h["wall"]), "plinth": sw(h["plinth"], "glossy"), "roof": sw(h["roof"]), "A": sw(h["A"], "glossy"), "B": sw(h["B"], "glossy"),
            "trim": sw(h["trim"], "brass" if h["trim"] in ("civic_trim", "gold") else "matte"), "glass": sw(FAC["glass"][0], "glass"), "chrome": sw("chrome", "chrome"),
            "white": sw("white"), "acc": sw("look:" + acc if acc else h["A"], "glossy"), "glass_t": sw("glass_t", "glass")}

SKIN = {}
for s_ in W["skins"]:
    for f in s_["faces"]: SKIN.setdefault(f["mass"], []).append((s_, f))
KITFACE = [(f["a"], f["b"], f["base"] + 5.65 + (f["storeys"] - 1) * 3.6) for f in W["facades"] if f.get("kit") and inb(f["kit"]["at"])]
def kit_covered(a, b, z):
    ax, ay = a; bx, by = b; L = math.hypot(bx - ax, by - ay) or 1; ux, uy = (bx - ax) / L, (by - ay) / L
    for (ka, kb, top) in KITFACE:
        if z > top: continue
        d1 = abs((ka[0] - ax) * uy - (ka[1] - ay) * ux); d2 = abs((kb[0] - ax) * uy - (kb[1] - ay) * ux)
        if d1 < 2.5 and d2 < 2.5:
            t1 = (ka[0] - ax) * ux + (ka[1] - ay) * uy; t2 = (kb[0] - ax) * ux + (kb[1] - ay) * uy
            if max(t1, t2) > 0.5 and min(t1, t2) < L - 0.5: return True
    return False

def wallframe(a, b, ring):
    ax, ay = a; bx, by = b; L = math.hypot(bx - ax, by - ay); ux, uy = (bx - ax) / L, (by - ay) / L
    nx, ny = uy, -ux
    cx = sum(p[0] for p in ring) / len(ring); cy = sum(p[1] for p in ring) / len(ring)
    if (ax + bx) / 2 - cx > 0 and nx < 0 or 0: pass
    if ((ax + bx) / 2 - cx) * nx + ((ay + by) / 2 - cy) * ny < 0: nx, ny = -nx, -ny
    P = lambda s, z, d: B(ax + ux * s + nx * d, ay + uy * s + ny * d, z)
    return L, P

def rect(M, P, s0, s1, z0, z1, d, role):
    M.quad([P(s0, z0, d), P(s1, z0, d), P(s1, z1, d), P(s0, z1, d)], role)

def window_face(M, P, L, bays, rows, sk, z_cov):
    n = max(1, len(bays)); bw = L / n; g = sk["glass"]
    for z0 in rows:
        if z_cov(z0): continue
        for i, ch in enumerate(bays):
            s0 = i * bw
            if ch == "R":
                rect(M, P, s0 + 0.3, s0 + bw - 0.3, z0 + g["sill"], z0 + g["head"], 0.03, "glass")
                for k in range(1, max(1, int((bw - 0.6) / g["mullion"]))):
                    x = s0 + 0.3 + k * g["mullion"]; rect(M, P, x - 0.03, x + 0.03, z0 + g["sill"], z0 + g["head"], 0.05, "chrome")
                rect(M, P, s0, s0 + bw, z0 + g["sill"] - 0.18, z0 + g["sill"], 0.05, "acc")
            elif ch == "P":
                for k in range(2):
                    c = s0 + bw * (k + 0.5) / 2; zc = z0 + 1.75
                    rect(M, P, c - 0.75, c + 0.75, zc - 0.75, zc + 0.75, 0.02, "acc")
                    seg = 12; pts = [P(c + 0.45 * math.cos(2 * math.pi * j / seg), zc + 0.45 * math.sin(2 * math.pi * j / seg), 0.04) for j in range(seg)]
                    f = M.bm.faces.new([M.bm.verts.new(p) for p in pts]); f.material_index = M.roles.index("glass")
                    pts = [P(c + 0.55 * math.cos(2 * math.pi * j / seg), zc + 0.55 * math.sin(2 * math.pi * j / seg), 0.035) for j in range(seg)]
                    f = M.bm.faces.new([M.bm.verts.new(p) for p in pts]); f.material_index = M.roles.index("trim")
            elif ch == "B":
                rect(M, P, s0 + 0.6, s0 + bw - 0.6, z0 + 0.15, z0 + g["head"], 0.03, "glass")
                q = [P(s0 + 0.5, z0, 0), P(s0 + bw - 0.5, z0, 0), P(s0 + bw - 0.5, z0, 1.2), P(s0 + 0.5, z0, 1.2)]; M.quad(q, "white")
                rect(M, P, s0 + 0.5, s0 + bw - 0.5, z0, z0 + 1.0, 1.2, "acc")

def grand_face(M, P, L, base, top):
    # civic 'grand' stand-in: piers, tall glazed bays framed in enamel A, spandrels in B, a brass hairline per storey
    n = max(1, round(L / 3.9)); bw = L / n
    zs = [base + 0.5] + [5.83 + k * 3.6 for k in range(int((top - 5.83) / 3.6) + 1)]
    zs = [z for z in zs if z < top - 1.0]
    for zi, z0 in enumerate(zs):
        z1 = (zs[zi + 1] if zi + 1 < len(zs) else top - 0.8)
        for i in range(n):
            s0 = i * bw
            rect(M, P, s0 + 0.55, s0 + bw - 0.55, z0 + 0.5, z1 - 0.35, 0.02, "A")
            rect(M, P, s0 + 0.65, s0 + bw - 0.65, z0 + 0.6, z1 - 0.45, 0.04, "glass")
            rect(M, P, s0 + bw / 2 - 0.03, s0 + bw / 2 + 0.03, z0 + 0.6, z1 - 0.45, 0.05, "trim")
        rect(M, P, 0, L, z1 - 0.35, z1 - 0.05, 0.03, "B")
        rect(M, P, 0, L, z1 - 0.05, z1, 0.05, "trim")

def building(m, civic=False):
    hid = host_of(m["id"]); h = host(hid); HOSTS[hid] = h
    sk_faces = SKIN.get(m["id"], [])
    acc = sk_faces[0][0].get("accent") if sk_faces else None
    M = Mesh(m["id"], ROLES)
    base, top = m["base"], m["base"] + m["height"]
    M.prism(m["ring"], base, top, "wall", "roof")
    ring = m["ring"]
    if base < 1:
        M.prism([(p[0], p[1]) for p in ring], base, base + 0.5, "plinth", "plinth", cap=False)
    for (sk, f) in sk_faces:
        L, P = wallframe(f["a"], f["b"], ring)
        if L < 1: continue
        window_face(M, P, L, f["bays"], f["rows"], sk, lambda z0: kit_covered(f["a"], f["b"], z0 + 1))
        rect(M, P, 0, L, top - 0.6, top, 0.06, "acc")
    if civic:
        r = ring[:-1] if ring[0] == ring[-1] else ring
        for i in range(len(r)):
            a, b = r[i], r[(i + 1) % len(r)]
            L, P = wallframe(a, b, r)
            if L > 3: grand_face(M, P, L, base, top)
    o = M.done(slots_for(h, acc)); HOSTMESH.append((o, hid, acc))

print("building masses ...")
for m in W["buildings"]:
    if any(inb(p) for p in m["ring"]): building(m)
for m in W["civic"]:
    if not any(inb(p) for p in m["ring"]): continue
    if m.get("kit"):
        place(m["kit"], m["kit"]["at"], 0, swaps=m.get("materials"), lowest=m["kit"].get("base", m["base"]))
    elif m["surface"] == "limestone": building(m, civic=True)
    else:
        M = Mesh(m["id"], ["s"]); M.prism(m["ring"], m["base"], m["base"] + m["height"], "s")
        srf = {"bed-kerb": "DK_Limestone", "bed-hedge": "DK_Leaf_CivicDark"}.get(m["surface"])
        M.done({"s": kit_mat(srf) if srf else sw("look:" + m["surface"] if "look:" + m["surface"] in PAL else "white")})

print("kit facades, roofs, street, ground pieces ...")
for f in W["facades"]:
    if f.get("kit") and inb(f["kit"]["at"]): place(f["kit"], f["kit"]["at"], f["base"])
for r in W["roofs"]:
    if not inb(r["c"]): continue
    if r.get("kit"):
        k = r["kit"]; place(k, k.get("at", r["c"]), k.get("base", r["z"]))
    else:
        M = Mesh(r["id"], ["roof", "trim", "column", "park", "deck"])
        for p in r.get("parts", []):
            role = p["surface"] if p["surface"] in M.roles else "roof"
            if p["shape"] == "box": M.box(p["c"], p["size"], p.get("yaw", 0), p["base"], role)
            elif p["shape"] == "cyl": M.cyl(p["c"], p["size"][0], p["base"], p["size"][2], role)
            else: M.sphere(p["c"], p["size"][0], p["base"], role)
        h = HOSTS.get(r["building"]) or host(r["building"])
        M.done({"roof": sw("look:roof"), "trim": sw(h["A"], "glossy"), "column": sw("chrome", "chrome"), "park": sw("leaf_b"), "deck": sw("cream_w")})
for s_ in W["street"]:
    if s_.get("kit") and inb(s_["at"]): place(s_["kit"], s_["at"], s_.get("z", 0.18), yaw=s_.get("kit", {}).get("yaw", s_.get("yaw", 0)))
for g in W["ground"]["pieces"]:
    if g.get("kit") and inb(g["at"]): place(g["kit"], g["at"], g.get("z", 0), yaw=g["kit"].get("yaw", g.get("yaw", 0)))

print("frontages ...")
TR = FAC["trades"]
for f in W["frontages"]:
    if not inb(f["a"]) or f.get("facade"): continue
    t = TR.get(f["trade"], FAC["tradeDefault"])
    M = Mesh(f["id"], ["glass", "t0", "t1", "awn", "trim"])
    fr = f["front"]; M.box(fr["c"], [fr["size"][0], fr["size"][1], 0.5], fr["yaw"], fr["base"], "t0")
    M.box(fr["c"], [fr["size"][0] - 0.2, fr["size"][1] - 0.05, 2.9], fr["yaw"], fr["base"] + 0.5, "glass")
    M.box(fr["c"], [fr["size"][0], fr["size"][1], fr["size"][2] - 3.4], fr["yaw"], fr["base"] + 3.4, "t0")
    if f.get("canopy"): c = f["canopy"]; M.box(c["c"], c["size"], c["yaw"], c["base"], "awn")
    if f.get("sign"): c = f["sign"]; M.box(c["c"], c["size"], c["yaw"], c["base"], "t1")
    if f.get("signKit"): place(f["signKit"], f["signKit"].get("at", f["sign"]["c"] if f.get("sign") else f["a"]), f["signKit"].get("base", 4.0))
    M.done({"glass": sw("glass_dark", "glass"), "t0": sw(t[0], "glossy"), "t1": sw(t[1], "glossy"), "awn": sw(t[0], "matte"), "trim": sw("chrome", "chrome")})

print("ground ...")
G = Mesh("ground", ["pavement", "road", "square", "civic", "median", "mixed", "residential", "open", "bay"])
G.prism([(X0 - 60, Y0 - 60), (X1 + 60, Y0 - 60), (X1 + 60, Y1 + 60), (X0 - 60, Y1 + 60)], -0.05, -0.03, "pavement")
for r in W["ground"]["roads"]:
    ln = r["line"]; hw = r["width"] / 2
    for i in range(len(ln) - 1):
        a, b = ln[i], ln[i + 1]
        if not (inb(a) or inb(b)): continue
        L = math.hypot(b[0] - a[0], b[1] - a[1]) or 1; nx, ny = -(b[1] - a[1]) / L * hw, (b[0] - a[0]) / L * hw
        ex, ey = (b[0] - a[0]) / L * 0.3, (b[1] - a[1]) / L * 0.3
        G.prism([(a[0] - ex + nx, a[1] - ey + ny), (b[0] + ex + nx, b[1] + ey + ny), (b[0] + ex - nx, b[1] + ey - ny), (a[0] - ex - nx, a[1] - ey - ny)], -0.03, 0.0, "road")
for fl in W["ground"]["flats"]:
    if fl["id"] != "ground/land" and any(inb(p) for p in fl["ring"]) and fl["surface"] in G.roles:
        G.prism(fl["ring"], -0.03, fl.get("level", 0) + 0.005, fl["surface"])
for p in W["ground"]["plots"]:
    if any(inb(q) for q in p["ring"]): G.prism(p["ring"], 0.0, 0.18, p["surface"] if p["surface"] in G.roles else "pavement")
for p in W["ground"]["medians"]:
    if any(inb(q) for q in p["ring"]): G.prism(p["ring"], 0.0, 0.18, "median")
gs = lambda k: sw("look:" + k)
pave = kit_mat("DK_Ground_Pavement", world=(4, 4)); civ = kit_mat("DK_Pave_Civic", world=(4, 4))
ROAD = kit_mat("DK_Ground_RoadEnamel", world=(2, 2))
GROUND_OBJ = G.done({"pavement": pave, "road": ROAD, "square": civ, "civic": civ, "median": gs("median"), "mixed": pave, "residential": pave, "open": pave, "bay": gs("bay")})
print("placed instances", NPL[0], "protos", len(PROTO))

# ------------------------------------------------------------------ light, world, cameras
eng = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
sc.render.engine = "BLENDER_EEVEE" if "BLENDER_EEVEE" in eng else "BLENDER_EEVEE_NEXT"
ee = sc.eevee
for k, v in (("taa_render_samples", 64), ("use_shadows", True), ("use_raytracing", True), ("shadow_ray_count", 2), ("shadow_step_count", 8)):
    if hasattr(ee, k): setattr(ee, k, v)
sc.render.resolution_x, sc.render.resolution_y = PX, round(PX * 9 / 16)
sc.view_settings.view_transform = "AgX"; sc.view_settings.look = "AgX - Base Contrast" if "AgX - Base Contrast" in [i.identifier for i in sc.view_settings.bl_rna.properties["look"].enum_items] else "None"
sc.view_settings.exposure = -0.2
wd = bpy.data.worlds.new("w"); sc.world = wd; wd.use_nodes = True; nt = wd.node_tree
env = nt.nodes.new("ShaderNodeTexEnvironment"); env.image = bpy.data.images.load(REPO + "/out/decokit/HDRI/city.exr")
bg = nt.nodes["Background"]; nt.links.new(env.outputs["Color"], bg.inputs["Color"]); bg.inputs["Strength"].default_value = 0.55
# the camera sees a plain sky gradient (haze to sky), the lighting and reflections come from the HDRI
sky = nt.nodes.new("ShaderNodeBackground"); grad = nt.nodes.new("ShaderNodeValToRGB"); tc = nt.nodes.new("ShaderNodeTexCoord"); sep = nt.nodes.new("ShaderNodeSeparateXYZ")
nt.links.new(tc.outputs["Generated"], sep.inputs["Vector"]); nt.links.new(sep.outputs["Z"], grad.inputs["Fac"])
grad.color_ramp.elements[0].color = (*rgb("#f3e8d4"), 1); grad.color_ramp.elements[1].color = (*rgb("#86cff2"), 1); grad.color_ramp.elements[0].position = 0.0; grad.color_ramp.elements[1].position = 0.35
nt.links.new(grad.outputs["Color"], sky.inputs["Color"]); sky.inputs["Strength"].default_value = 1.0
lp = nt.nodes.new("ShaderNodeLightPath"); mixs = nt.nodes.new("ShaderNodeMixShader")
nt.links.new(lp.outputs["Is Camera Ray"], mixs.inputs["Fac"]); nt.links.new(bg.outputs["Background"], mixs.inputs[1]); nt.links.new(sky.outputs["Background"], mixs.inputs[2])
nt.links.new(mixs.outputs["Shader"], nt.nodes["World Output"].inputs["Surface"])
sun = bpy.data.lights.new("sun", "SUN"); sun.energy = 6.0; sun.color = rgb("#ffe9bc"); sun.angle = math.radians(1.2)
so = bpy.data.objects.new("sun", sun); LIVE.objects.link(so)
p, y = math.radians(38), math.radians(330)
d = Vector((-math.sin(y) * math.cos(p), -math.cos(y) * math.cos(p), -math.sin(p)))   # Unity forward (sin y cos p, -sin p, cos y cos p) through the axis map
so.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
cd = bpy.data.cameras.new("cam"); co = bpy.data.objects.new("cam", cd); LIVE.objects.link(co); sc.camera = co

def unity_fwd(pitch, yaw):
    p, y = math.radians(pitch), math.radians(yaw)
    return Vector((-math.sin(y) * math.cos(p), -math.cos(y) * math.cos(p), -math.sin(p)))

VIEWS = {
    "cafe-play-30": dict(kind="play", focus=(-159, -222), yaw=30),
    "office-play-210": dict(kind="play", focus=(-160, -281), yaw=210),
    "district-play-30": dict(kind="play", focus=(-104, -268), yaw=30),
    "office-street": dict(kind="eye", pos=(-156.425, -253.53, 3.24), pitch=7.219, yaw=186, fov=55),
    "cafe-close": dict(kind="eye", pos=(-149.0, -236.5, 2.2), pitch=6.0, yaw=325, fov=50),
}
SEL = opt("--views", ",".join(VIEWS)).split(",")
def shoot(vn, tag):
    v = VIEWS[vn]
    if v["kind"] == "play":
        f = unity_fwd(38, v["yaw"]); t = B(v["focus"][0], v["focus"][1], 0)
        cd.type = "ORTHO"; cd.sensor_fit = "VERTICAL"; cd.ortho_scale = 28.0; cd.clip_start = 1; cd.clip_end = 1000
        co.location = t - f * 160
    else:
        f = unity_fwd(v["pitch"], v["yaw"]); cd.type = "PERSP"; cd.sensor_fit = "VERTICAL"; cd.angle_y = math.radians(v["fov"]); cd.clip_start = 0.2; cd.clip_end = 900
        co.location = B(v["pos"][0], v["pos"][1], v["pos"][2])
    co.rotation_euler = f.to_track_quat("-Z", "Y").to_euler()
    sc.render.filepath = os.path.join(OUT, f"{vn}__{tag}.png"); bpy.ops.render.render(write_still=True)
    print("wrote", sc.render.filepath)

for vn in SEL: shoot(vn, "before")
if "--only-before" in A: sys.exit(0)

# ------------------------------------------------------------------ the loosened values: recolour in place, same objects, same light
for name, o in OVR.get("kit", {}).items():
    if name not in BSDF: continue
    m, b = BSDF[name]
    if "hex" in o: set_col(name, o["hex"])
    if "metallic" in o: b.inputs["Metallic"].default_value = o["metallic"]
    if "smoothness" in o: b.inputs["Roughness"].default_value = 1 - o["smoothness"]
    if "alpha" in o: b.inputs["Alpha"].default_value = o["alpha"]
PAL.update(OVR.get("swatches", {})); PAL.update({"look:" + k: v for k, v in OVR.get("look", {}).items()})
CLS.update({"matte": (1 - OVR.get("facadeMaterials", FAC["materials"])["matte"], 0.0), "glossy": (1 - OVR.get("facadeMaterials", FAC["materials"])["glossy"], 0.0)})
for (nm, cls), m in SWM.items():
    b = m.node_tree.nodes["Principled BSDF"]; b.inputs["Base Color"].default_value = (*rgb(PAL[nm]), 1)
    r, me = CLS[cls]; b.inputs["Roughness"].default_value = r; b.inputs["Metallic"].default_value = me
# area enamel additions: a host switches A or B to an added colour only when its own die lands on one (others keep theirs)
add = OVR.get("areaEnamelAdd", {}); rep = OVR.get("areaEnamelReplace", {})
for (o, hid, acc) in HOSTMESH:
    h = HOSTS[hid]; extra = add.get(h["area"], []); rp = rep.get(h["area"], {})
    if not extra and not rp: continue
    en = FAC["areas"][h["area"]]["enamel"]; n = len(en) + len(extra)
    h2 = {k: rp.get(v, v) if k in ("A", "B", "plinth") else v for k, v in h.items()}
    for k in ("A", "B"):
        r = die(hid, "x" + k) % n
        if r >= len(en): h2[k] = extra[r - len(en)]
    sl = slots_for(h2, acc)
    for i, role in enumerate(ROLES): o.data.materials[i] = sl[role]
for vn in SEL: shoot(vn, "after")
# optional third pass: the road back to DecoKit asphalt (a separate choice; materials only)
plus = OVR.get("plus")
if plus and "--plus" in A:
    PAL.update({"look:" + k: v for k, v in plus.get("look", {}).items()})
    for (nm, cls), m in SWM.items():
        m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (*rgb(PAL[nm]), 1)
    if plus.get("road"): GROUND_OBJ.data.materials[1] = kit_mat(plus["road"], world=(4, 4))
    for name, hx in plus.get("kitTint", {}).items():
        if name in BSDF: set_col(name, hx)
    for vn in SEL: shoot(vn, "plus")
print("DONE")
