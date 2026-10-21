"""The testbed's atom fountain, ported at full detail (brief 028, Vela; Sky: "and the fountain from testbed....?", "any compression goes through me").

AtomFountain_Testbed: 6.6 m across, 4.08 m high (the testbed's `close` level, its finest: 41,888 triangles), pivot the pool centre on the ground. Two sources, replayed, not redrawn:
  basin     the testbed's own geometry (testbed/src/fountain-geometry.js, three 0.186.1) baked at its finest tessellation by art/fountain/dump_basin.mjs to art/fountain/basin-close.json (21,888 tris,
            vertices and UVs as three made them; three Y up -> Blender Z up by a pure rotation, winding kept): teal plinth and fluted drum, marble coping (32 carved blocks) and ledge, the
            champagne beads, the recessed waterline light ring, the pedestal ring, 12 jets with their nozzles and 12 landing ripples, and the pool water with its 12 mm meniscus at the pedestal.
  sculpture the store's source-art/CivicFountain/atom-sculpture-v1.blend, its `close` collection (20,000 tris): the silver globe and trumpet pedestal and the three gold orbits with their electrons.
            The orbits are movable parts (DK_AtomFountain_Testbed_Orbit0..2) about the vertical axis through the globe centre (2.33 m), turned by Latch at the testbed's rates (art/fountain/PROVENANCE.json).
Nothing decimated, re-meshed, re-baked or downscaled; the testbed's gameplay (18,288 tris) and near (32,556) levels are NOT ported (they are decimations: Sky reviews compression), no automatic LODs.
Materials (textures: fountain_tex.py): marble (ambientCG Marble012, CC0), teal ceramic, gold (the testbed's #F9E2A2 rods; brass is the street rule: a decision for Sky), silver, waterline light (emit
1.4 at night), the pool water (the testbed's mosaic on world UVs, 2 m; its animated ripple and caustic shader is NOT ported here: Glint/Latch), jets and ripples (glass).
Not carried: the testbed's clearcoat on marble and glaze (URP Lit has none: smoothness stands in), the environment probe, the night point and spot lights (Latch)."""

import hashlib
import json
import math
import os
from pathlib import Path

import bpy

from kit import REGISTRY, asset
from mats import GROUND, KIND, TEXTURES, pal, register

ART = Path(__file__).resolve().parent / "art/fountain"
BLEND = Path(os.environ.get("SUBORNED_STORE") or "~/Projects/the-suborned-store").expanduser() / "source-art/CivicFountain/atom-sculpture-v1.blend"
BLEND_SHA = "SEE_PROVENANCE"
BASIN = None

register("DK_Fountain_Marble", "paving", "#FFFFFF")
register("DK_Fountain_Ceramic", "paving", "#FFFFFF")
register("DK_Fountain_Gold", "paving", "#F9E2A2")
register("DK_Fountain_Silver", "paving", "#F5F5F5")
register("DK_Fountain_Water", "paving", "#7FB4BC")
register("DK_Fountain_Light", "emit", "#93E7E4", 1.4)
register("DK_Fountain_Jet", "sheen", "#BDE4E8")
register("DK_Fountain_Foam", "sheen", "#D3EDDF")
GROUND.setdefault("DK_Fountain_Marble", dict(albedo="fountain_marble_v1.png", normal="fountain_marble_v1_normal.png", mask="fountain_marble_v1_mask.png", tile=(1.0, 1.0), smoothness=1.0, bump=0.20))
GROUND.setdefault("DK_Fountain_Ceramic", dict(albedo="fountain_ceramic_v1.png", mask="fountain_ceramic_v1_mask.png", tile=(1.0, 1.0), smoothness=1.0))
GROUND.setdefault("DK_Fountain_Gold", dict(albedo="fountain_white_v1.png", normal="fountain_polish_v1_normal.png", mask="fountain_gold_v1_mask.png", tile=(1.0, 1.0), smoothness=1.0, bump=0.12, tint="#F9E2A2"))
GROUND.setdefault("DK_Fountain_Silver", dict(albedo="fountain_white_v1.png", normal="fountain_polish_v1_normal.png", mask="fountain_silver_v1_mask.png", tile=(1.0, 1.0), smoothness=1.0, bump=0.12, tint="#F5F5F5"))
GROUND.setdefault("DK_Fountain_Water", dict(albedo="fountain_mosaic_v1.png", tile=(1.0, 1.0), smoothness=0.9, tint="#7FB4BC"))
for _n, _t in (("DK_Fountain_Marble", "fountain_marble_v1.png"), ("DK_Fountain_Ceramic", "fountain_ceramic_v1.png"), ("DK_Fountain_Gold", "fountain_white_v1.png"),
               ("DK_Fountain_Silver", "fountain_white_v1.png"), ("DK_Fountain_Water", "fountain_mosaic_v1.png")):
    TEXTURES.setdefault(_n, (_t, 1.0))

MAT = {"stone": "Fountain_Marble", "teal": "Fountain_Ceramic", "champagne": "Fountain_Gold", "light": "Fountain_Light", "jets": "Fountain_Jet", "foam": "Fountain_Foam", "water": "Fountain_Water"}


def _push(s, v, tris, uv, mat, smooth=True, map_xyz=True, uvs=None):
    """Triangles into a Shape with their own UVs. v: flat three-frame xyz when map_xyz (x, y up, z) -> Blender (x, -z, y); else already Blender."""
    verts = []
    vm = {}
    for n, t in enumerate(tris):
        vs = []
        for vi in t:
            if vi not in vm:
                p = v[vi]
                p = (p[0], -p[2], p[1]) if map_xyz else tuple(p)
                vm[vi] = s.bm.verts.new(p)
            vs.append(vm[vi])
        f = s.bm.faces.new(vs)
        f.material_index = s._mi(mat)
        f.smooth = smooth
        f[s._uvkeep] = 1
        s._pending_uv.append((f, [tuple(uvs[n][k]) for k in range(3)]))


def _basin(s):
    global BASIN
    BASIN = BASIN or json.loads((ART / "basin-close.json").read_text())
    for kind, d in BASIN["kinds"].items():
        v = [tuple(d["v"][i:i + 3]) for i in range(0, len(d["v"]), 3)]
        uv = [tuple(d["uv"][i:i + 2]) for i in range(0, len(d["uv"]), 2)]
        t = [tuple(d["t"][i:i + 3]) for i in range(0, len(d["t"]), 3)]
        if kind == "water":  # the testbed's shader samples the mosaic at world xz / 2 (floorUV): the same, as mesh UVs
            uvs = [[(v[i][0] * 0.5, v[i][2] * 0.5) for i in tri] for tri in t]
        else:
            uvs = [[uv[i] for i in tri] for tri in t]
        _push(s, v, t, None, getattr(pal, MAT[kind]), True, True, uvs)


def _sculpture(s):
    if not BLEND.exists():
        raise RuntimeError(f"{BLEND} missing")
    names = [f"Silver globe and pedestal.002"] + [f"Gold orbit {i}.002" for i in range(3)]
    with bpy.data.libraries.load(str(BLEND), link=False) as (src, dst):
        dst.objects = [n for n in src.objects if n in names]
    objs = {o.name: o for o in dst.objects}
    for name in names:
        o = objs[name]
        me = o.data
        bpy.context.scene.collection.objects.link(o)
        bpy.context.view_layer.update()
        mw = o.matrix_world
        uvl = me.uv_layers.active
        verts = [tuple(mw @ v.co) for v in me.vertices]
        tris, uvs, sm = [], [], []
        for p in me.polygons:
            vs = list(p.vertices)
            lis = list(p.loop_indices)
            for k in range(1, len(vs) - 1):
                tris.append((vs[0], vs[k], vs[k + 1]))
                uvs.append([tuple(uvl.data[lis[j]].uv) if uvl else (0.0, 0.0) for j in (0, k, k + 1)])
                sm.append(p.use_smooth)
        bpy.data.objects.remove(o, do_unlink=True)
        silver = name.startswith("Silver")
        target = s if silver else None
        if silver:
            _push(s, verts, tris, None, pal.Fountain_Silver, True, False, uvs)
        else:
            with s.part(f"Orbit{name.split()[2][0]}") as p:
                _push(p, verts, tris, None, pal.Fountain_Gold, True, False, uvs)


@asset("AtomFountain_Testbed", "Plaza", bevel=0.0)
def atom_fountain_testbed(s):
    _basin(s)
    _sculpture(s)


REGISTRY["AtomFountain_Testbed"]["nolod"] = True
