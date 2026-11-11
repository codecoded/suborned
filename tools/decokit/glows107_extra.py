"""Brief 107 (Vela): the planter-foot glow (glows107.py DK_Glow_PlanterFoot) on the civic stone planters of the brief-097 extra root, as a new extra root.

bun run guard /Applications/Blender.app/Contents/MacOS/Blender -b -t 1 -P tools/decokit/glows107_extra.py -- [--src out/decokit-097-extra] [--dst out/decokit-107-extra] [--only a,b]
  Copies the whole src root to dst (every other row and file stays byte for byte), then for each planter row (glows107.PLANTERS, or --only):
  traces the foot of its stonework once on L0 (the bottom edges of the stone slots whose wall faces look out of the planter: a ray up from just
  outside must meet no stone or soil, the companion's soil included, and a ray out along the ground no stone within ENCLOSED, so a body standing
  inside its own plinth is skipped), and adds one object `<level object>_glow` to every level file: the same strip in each (a pool FOOT_W out on the
  paving and a wash up the wall, FOOT_H or the face's own height if lower, mitred at the joints, v 0 on the foot line), material DK_Glow_PlanterFoot,
  no vertex colour. The stone, soil and planting objects are re-exported as loaded (vertex_ao_extra's round trip: its --no-ao run proves it), their
  slots and order unchanged. The row gains the glow object (shadows off), the material, the tris, and `glow107` (strip tris, foot length, generator sha).
  Import: bun run kit --extra out/decokit-107-extra (in place of out/decokit-097-extra). Writes dst/glows107.json (per row: levels, strip tris, foot m)."""
import hashlib
import json
import math
import shutil
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import glows107 as G  # noqa: E402
import vertex_ao_extra as X  # noqa: E402  (load: import + transform apply + kit slot names; export: the kit's FBX settings, fixed header)

REPO = HERE.parents[1]
ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
opt = lambda k, d: ARGS[ARGS.index(k) + 1] if k in ARGS else d  # noqa: E731
SRC, DST = REPO / opt("--src", "out/decokit-097-extra"), REPO / opt("--dst", "out/decokit-107-extra")
ONLY = [n for n in opt("--only", ",".join(G.PLANTERS)).split(",") if n]
EPS_Z = 0.01
ENCLOSED = 0.2   # m: a foot with stone this close outside it is enclosed (PlanterRun's body, 0.58 wide, stands inside its 0.63 plinth)


def _key(v):
    return (round(v.x, 3), round(v.y, 3))


def foot(objs, extra_soil=()):
    """The planter's outer foot as edges (a, b, out, up, h): a, b on the ground line, `out` the wall's outward normal on the ground, `up` the wall's
    own rising direction (so the wash follows a battered wall), `h` the wash's height: FOOT_H, or less where the face it climbs is lower (a 0.10 m
    plinth). Traced on the stone slots of the main object."""
    stone = [o for o in objs if not o.name.endswith("_soil")]
    assert len(stone) == 1, [o.name for o in objs]
    o = stone[0]
    keep = {i for i, s in enumerate(o.material_slots) if s.material and s.material.name in G.STONE}
    bm = bmesh.new()
    bm.from_mesh(o.data)
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.material_index not in keep], context="FACES")
    bm.normal_update()
    zmin = min(v.co.z for v in bm.verts)
    # what closes the planter from above: its stone and soil (and the companion's soil), never the leaves that spill over the wall
    tb = bmesh.new()
    tb.from_mesh(o.data)
    bmesh.ops.delete(tb, geom=[f for f in tb.faces if f.material_index not in keep], context="FACES")
    for s in [x for x in objs if x.name.endswith("_soil")] + list(extra_soil):
        tb.from_mesh(s.data)
    tree = BVHTree.FromBMesh(tb)
    edges = []
    for e in bm.edges:
        a, b = e.verts
        if a.co.z > zmin + EPS_Z or b.co.z > zmin + EPS_Z or (a.co - b.co).length < 1e-4:
            continue
        walls = [f for f in e.link_faces if abs(f.normal.z) < 0.8]
        if not walls:
            continue
        f = max(walls, key=lambda f: f.calc_area())
        out = Vector((f.normal.x, f.normal.y, 0.0))
        if out.length < 1e-6:
            continue
        out.normalize()
        mid = (a.co + b.co) / 2
        hit = tree.ray_cast(Vector((mid.x, mid.y, zmin + 0.02)) + out * 0.25, Vector((0, 0, 1)), 50.0)
        if hit[0] is not None:   # the far side of this wall is inside the planter (an inner face, under the soil)
            continue
        hit = tree.ray_cast(Vector((mid.x, mid.y, zmin + 0.01)) + out * 0.002, out, ENCLOSED)
        if hit[0] is not None:   # stone stands just outside this foot (a body inside its own plinth): the plinth's foot is the one that shows
            continue
        d = (b.co - a.co).normalized()
        up = f.normal.cross(d)
        if up.z < 0:
            up = -up
        up = up.normalized() if up.z > 0.2 else Vector((0, 0, 1))
        h = min(G.FOOT_H, max(v.co.z for v in f.verts) - zmin)
        edges.append((a.co.copy(), b.co.copy(), out, up, h))
    bm.free()
    tb.free()
    return edges, zmin


def strip(edges, zmin, name):
    """One mesh: per foot edge a pool quad on the paving and a wash quad up the wall, joined at shared corners with mitred offsets; UV v 0 on the foot."""
    outs, ups, hs = {}, {}, {}
    for a, b, out, up, h in edges:
        for p in (a, b):
            outs.setdefault(_key(p), []).append(out)
            ups.setdefault(_key(p), []).append(up)
            hs[_key(p)] = min(hs.get(_key(p), h), h)

    def mitre(k):
        n = sum(outs[k], Vector())
        if n.length < 1e-6:
            return outs[k][0], 1.0
        n.normalize()
        return n, min(2.0, 1.0 / max(0.5, min(n.dot(o) for o in outs[k])))

    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    vid = {}

    def vert(k, p, layer):
        if (k, layer) not in vid:
            n, s = mitre(k)
            u = sum(ups[k], Vector()).normalized()
            base = Vector((p.x, p.y, zmin))
            if layer == "foot":
                q = base + n * G.PROUD + Vector((0, 0, G.LIFT))
            elif layer == "pool":
                q = base + n * (G.PROUD + G.FOOT_W * s) + Vector((0, 0, G.LIFT))
            else:  # wash: up the wall face (FOOT_H, or the face's height), just off it
                q = base + n * G.PROUD + u * (hs[k] / max(u.z, 0.2))
            vid[(k, layer)] = bm.verts.new(q)
        return vid[(k, layer)]

    run = {}
    for a, b, *_ in edges:   # u: metres along the foot (the shader reads only v)
        run[_key(b)] = run.get(_key(a), 0.0) + (b - a).length
    for a, b, *_ in edges:
        ka, kb = _key(a), _key(b)
        ua, ub = run.get(ka, 0.0), run.get(ka, 0.0) + (b - a).length
        for layer in ("pool", "wash"):
            q = [vert(ka, a, "foot"), vert(kb, b, "foot"), vert(kb, b, layer), vert(ka, a, layer)]
            if len({id(x) for x in q}) < 4:
                continue
            try:
                f = bm.faces.new(q)
            except ValueError:   # the same quad twice (a doubled edge in the stone)
                continue
            for lp, (u, v) in zip(f.loops, ((ua, 0.0), (ub, 0.0), (ub, 1.0), (ua, 1.0))):
                lp[uv].uv = (u, v)
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    for p in me.polygons:   # flat: the strip faces up (pool) or out (wash); the shader is double-sided either way
        p.use_smooth = False
    mat = bpy.data.materials.get(G.FOOT) or bpy.data.materials.new(G.FOOT)
    me.materials.append(mat)
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    return ob


def companion_soil(n):
    rel = G.COMPANION.get(n)
    if not rel:
        return []
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(SRC / rel))
    new = [o for o in bpy.data.objects if o not in before and o.type == "MESH"]
    for o in new:
        o.select_set(True)
        bpy.context.view_layer.objects.active = o
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    return [o for o in new if o.name.split(".")[0].endswith("_soil")]


def main():
    man = json.loads((SRC / "decokit_manifest.json").read_text())
    rows = {r["name"]: r for r in man["assets"]}
    shutil.copytree(SRC, DST, dirs_exist_ok=True)
    gen = {f: hashlib.sha256((HERE / f).read_bytes()).hexdigest() for f in ("glows107.py", "glows107_extra.py")}
    report = []
    for n in ONLY:
        row = rows[n]
        f0 = row["files"][0]
        levels = [f0] + row.get("lods", [])
        objs = X.load(SRC / f0["file"])
        edges, zmin = foot(objs, companion_soil(n))
        foot_m = sum((b - a).length for a, b, *_ in edges)
        assert edges, f"{n}: no outer foot found"
        per = []
        for k, lv in enumerate(levels):
            objs = X.load(SRC / lv["file"])
            main_obj = next(o for o in objs if not o.name.endswith("_soil"))
            g = strip(edges, zmin, main_obj.name + "_glow")
            st = len(g.data.polygons)
            t = X.tris(objs + [g])
            lv.update(X.export(objs + [g], DST / lv["file"]), tris=t)
            per.append(dict(level=k, file=lv["file"], tris=t, glow_tris=st))
            print(f"[DK-107] {n} level {k}: +{st} glow tris -> {t} ({lv['file']})")
            if k == 0:
                row["tris"] = t
                obs = f0.get("objects")
                if obs is not None and not any(o["name"] == g.name for o in obs):
                    obs.append(dict(name=g.name, materials=[G.FOOT], shadows="off"))
        if G.FOOT not in row["materials"]:
            row["materials"] = row["materials"] + [G.FOOT]
        row["glow107"] = dict(brief="107", material=G.FOOT, foot_m=round(foot_m, 2), edges=len(edges), glow_tris=per[0]["glow_tris"], generator=gen)
        report.append(dict(name=n, foot_m=round(foot_m, 2), edges=len(edges), levels=per))
    (DST / "decokit_manifest.json").write_text(json.dumps(man, indent=1))
    (DST / "glows107.json").write_text(json.dumps({"brief": "107", "material": G.FOOT, "w": G.FOOT_W, "h": G.FOOT_H, "pieces": report}, indent=1))
    print(f"[DK-107] {len(report)} rows -> {DST}")


if __name__ == "__main__":  # review scripts import foot / strip
    main()
