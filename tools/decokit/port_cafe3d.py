"""Port the 2.5D testbed's café furniture and room contents to replayable geometry (brief 026, Vela).

blender -b -P tools/decokit/port_cafe3d.py          writes tools/decokit/art/cafe3d/parts.json and PROVENANCE.json

Sources (read-only, ~/Projects/the-suborned-store/testbed-art/, sha256 in PROVENANCE.json):
  cafe-table-set/cafe-furniture-pbr-v2.glb   the four live furniture templates the testbed instanced (cafe-table-kit.js): chair 520 tris, table 758, parasol 3008, lamp body 600
  cafe-3d/cafe-3d.glb                        the café model (cafe-3d.blend exported): only the room CONTENTS are taken, never the shell, so the Oct 9 shell (C-0) is untouched:
                                             every mesh of the lounge furniture atlas, booths, walnut cabinetry and back shelves, register, opal luminaires, pendants and rods, interior plants, amber cove and
                                             teal mood emitters, the painted counter front; and, from the petrol-enamel and chrome batches, only the loose parts under 1.2 m across (planter pots, rod
                                             collars): the long runs are the testbed's shell trim.
Geometry is baked to world (metres, Z up, as the glb imports into Blender, which is the testbed's frame: X east, Y north, floor z 0.99), triangulated, vertices merged by (position, uv), written
at 1 mm. Nothing is edited: cafe_testbed.py moves it into CafeInterior's frame (floor 0.04, back wall 10.9) as one rigid offset.
Materials: the glb's own base colours (linear -> sRGB hex), metallic, roughness and emission; the furniture atlas and the counter art are image materials (textures: tex_cafe3d.py)."""

import hashlib
import json
from pathlib import Path

import bmesh
import bpy

STORE = Path("~/Projects/the-suborned-store/testbed-art").expanduser()
OUT = Path(__file__).resolve().parent / "art/cafe3d"
SRC = {"furniture": STORE / "cafe-table-set/cafe-furniture-pbr-v2.glb", "cafe": STORE / "cafe-3d/cafe-3d.glb"}
CONTENT = ("shared live PBR.001", "petrol velvet booths", "walnut cabinetry", "register black bakelite", "warm opal luminaires", "warm pendant emitter", "interior glossy foliage",
           "amber cove emitter", "teal mood emitter", "pilot counter enamel")
LOOSE_ONLY = ("petrol enamel", "polished chrome")
MAX_LOOSE = 1.2


def srgb(c):
    return [round(1.055 * x ** (1 / 2.4) - 0.055 if x > 0.0031308 else 12.92 * x, 4) for x in c[:3]]


def hexs(c):
    return "#%02X%02X%02X" % tuple(round(max(0, min(1, v)) * 255) for v in srgb(c))


def material(m):
    b = m.node_tree.nodes["Principled BSDF"]
    img = next((n.image.name for n in m.node_tree.nodes if n.type == "TEX_IMAGE" and n.label == "BASE COLOR" and n.image), None)
    es = b.inputs["Emission Strength"].default_value
    return dict(name=m.name, hex=hexs(b.inputs["Base Color"].default_value), metallic=round(b.inputs["Metallic"].default_value, 3), rough=round(b.inputs["Roughness"].default_value, 3),
                emit=(hexs(b.inputs["Emission Color"].default_value), round(es, 3)) if es > 0 else None, image=img)


def components(bm):
    seen, comps = set(), []
    for f in bm.faces:
        if f in seen:
            continue
        stack, cur = [f], []
        seen.add(f)
        while stack:
            g = stack.pop()
            cur.append(g)
            for e in g.edges:
                for h in e.link_faces:
                    if h not in seen:
                        seen.add(h)
                        stack.append(h)
        comps.append(cur)
    return comps


def bake(o, keep=None):
    bm = bmesh.new()
    bm.from_mesh(o.data)
    bm.transform(o.matrix_world)
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    uvl = bm.loops.layers.uv.active
    faces = bm.faces
    if keep:
        faces = [f for c in components(bm) if keep(c) for f in c]
    key, verts, uvs, tris = {}, [], [], []
    for f in faces:
        t = []
        for lp in f.loops:
            p, uv = lp.vert.co, (lp[uvl].uv if uvl else (0, 0))
            k = (round(p.x, 3), round(p.y, 3), round(p.z, 3), round(uv[0], 4), round(uv[1], 4))
            if k not in key:
                key[k] = len(verts)
                verts.append(list(k[:3]))
                uvs.append(list(k[3:]))
            t.append(key[k])
        tris.append(t)
    bm.free()
    return dict(v=verts, uv=uvs, t=tris)


def load(path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(path))
    return [o for o in bpy.data.objects if o.type == "MESH"]


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    out = dict(furniture={}, room=[], materials={})
    for o in load(SRC["furniture"]):
        d = bake(o)
        d["material"] = o.data.materials[0].name
        out["furniture"][o.name] = d
        out["materials"][d["material"]] = material(o.data.materials[0])
    for o in load(SRC["cafe"]):
        if not o.name.startswith("room "):
            continue
        base = o.name[len("room Cafe "):]
        if base in CONTENT:
            keep = None
        elif base in LOOSE_ONLY:
            def keep(c):
                vs = [v.co for f in c for v in f.verts]
                ext = max(max(v[i] for v in vs) - min(v[i] for v in vs) for i in (0, 1))
                return ext < MAX_LOOSE and min(v[2] for v in vs) > 0.9
        else:
            continue
        d = bake(o, keep)
        if not d["t"]:
            continue
        d["name"], d["material"] = base, o.data.materials[0].name
        out["room"].append(d)
        out["materials"][d["material"]] = material(o.data.materials[0])
    # the counter art: the glb's own surface atlas (1254 px) at its own size, to the café art dir with a provenance row
    img = bpy.data.images.get("surface-atlas-v1")
    art = OUT.parent / "cafe"
    if img:
        img.filepath_raw = str(art / "cafe_surface_tb_v1.png")
        img.file_format = "PNG"
        img.save()
        pj = art / "PROVENANCE.json"
        pr = json.loads(pj.read_text())
        pr["files"] = [f for f in pr["files"] if f["file"] != "cafe_surface_tb_v1.png"] + [dict(
            file="cafe_surface_tb_v1.png", source="~/Projects/the-suborned-store/testbed-art/cafe-3d/cafe-3d.glb (embedded image surface-atlas-v1, 1254 px, kept at its own size)",
            sha256=hashlib.sha256((art / "cafe_surface_tb_v1.png").read_bytes()).hexdigest(), bytes=(art / "cafe_surface_tb_v1.png").stat().st_size,
            service="the 2.5D testbed's café pilot artwork (testbed-art/cafe-pilot, prompts there); brief 026 port by tools/decokit/port_cafe3d.py", use="the painted counter front (CafeInterior_Testbed), lit art")]
        pj.write_text(json.dumps(pr, indent=1))
    tri = sum(len(p["t"]) for p in out["furniture"].values()) + sum(len(p["t"]) for p in out["room"])
    (OUT / "parts.json").write_text(json.dumps(out, separators=(",", ":")))
    prov = dict(note="Brief 026, Vela. Geometry of the 2.5D testbed's café furniture and room contents, baked from the store's glb files by tools/decokit/port_cafe3d.py (no edits). Original procedural geometry made for The Suborned; no external asset library (the sources' own provenance).",
                sources={k: dict(path=str(v).replace(str(Path.home()), "~"), sha256=hashlib.sha256(v.read_bytes()).hexdigest(), bytes=v.stat().st_size) for k, v in SRC.items()},
                parts_json=dict(sha256=hashlib.sha256((OUT / "parts.json").read_bytes()).hexdigest(), bytes=(OUT / "parts.json").stat().st_size),
                triangles=dict(furniture={k: len(v["t"]) for k, v in out["furniture"].items()}, room={p["name"]: len(p["t"]) for p in out["room"]}, total=tri),
                filters=dict(content=list(CONTENT), loose_only=list(LOOSE_ONLY), max_loose_m=MAX_LOOSE))
    (OUT / "PROVENANCE.json").write_text(json.dumps(prov, indent=1))
    print("[port_cafe3d] triangles", tri, prov["triangles"])


main()
