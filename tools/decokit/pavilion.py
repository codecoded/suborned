"""The testbed's boarding pavilion, ported at full detail (brief 028, Vela; Sky: "the car park stand / bay isn't as good as testbed", "any compression goes through me").

Source: the testbed's signed-off pavilion B (Meshy v1, untouched: 28,190 triangles, one mesh, 4096 px base and normal, 2048 px emissive and metal-rough maps) with the four recessed downlight
fittings the testbed appended (boarding-pavilion-illuminated: pavilion_integrated_lights.mjs, copied to art/pavilion/pavilion_integrated_lights.original.mjs; fixtures.json beside it).
The body is the store's source glb, imported here and fitted to a 12 m canopy exactly as the testbed fitted it (civic-boarding-pavilion.js: scale 12 / length, centred on the footprint, ground at
z 0). The fittings are rebuilt from the same script's own numbers: a torus bezel (R 0.14, r 0.025, 4 x 16) and an opal disc (r 0.116, 16) at the stored fixture positions (x -3.15, -1.05, 1.05,
3.15, depth 1.2, heights from fixtures.json), 576 triangles, all UVs on the same pearl patch (0.43567, 0.89358, +-0.006). Nothing is decimated, re-meshed, re-baked or downscaled: the testbed's
LOD1 (12,000) and LOD2 (5,999) are decimations of this mesh and are NOT ported (Sky reviews any compression); no automatic LODs either (export_kit nolod).
Dropped without loss: the emissive map (all black, max 8/255: Meshy's, never lit) and the glTF 'doubleSided' flag (the .mat is single-sided like the other kit pieces).

Pieces (native frame: long along X, front toward -Y [Unity +Z], pivot footprint centre on the ground, as BoardingPavilion_Square_v2):
  BoardingPavilion_TB_Square   scale 0.70 -> 8.4 m x 2.14 m x 2.59 m: the testbed's getaway shelter (civicGetawayPavilionPlacement scale .70) = the block's square bay, structure square-boarding-pavilion
  BoardingPavilion_TB_Office   scale 1.25 -> 15.0 m x 3.83 m x 4.62 m, turned 90 degrees so it runs along Y (as BoardingPavilion_Office): the testbed's official-bay shelter (civicPavilionPlacement scale 1.25)
Both carry their own four supports (the testbed's colliders: two at +-4.60 x scale): the block's PavilionPier_* / pavilion-support structures stand where they do, so Splice drops those two when it places these.
Textures: pavilion_tex.py (full resolution). Night: the lenses are DK_Pavilion_Opal (emit, the testbed's night 3.2); Latch's night scalar takes it to day."""

import hashlib
import json
import math
import os
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Vector

from kit import asset
from mats import GROUND, TEXTURES, pal, register

ART = Path(__file__).resolve().parent / "art/pavilion"
STORE = Path(os.environ.get("SUBORNED_STORE_ART") or "~/Projects/the-suborned-store/testbed-art").expanduser()
SOURCE = STORE / "boarding-pavilion/source/boarding-pavilion-b.glb"
SHA = "7552d622d2a71b1885f69eb82c48956d76bd8d100a784ccab0631fca181ccd13"
FIX = json.loads((ART / "fixtures.testbed.json").read_text())["fixturePositions"]

register("DK_Pavilion_Atlas", "paving", "#FFFFFF")
register("DK_Pavilion_Opal", "emit", "#FFE3AF", 3.2)
TEXTURES.setdefault("DK_Pavilion_Atlas", ("pavilion_atlas_v1.png", 1.0))
GROUND.setdefault("DK_Pavilion_Atlas", dict(albedo="pavilion_atlas_v1.png", normal="pavilion_atlas_v1_normal.png", mask="pavilion_atlas_v1_mask.png", tile=(1.0, 1.0), smoothness=1.0))

_BODY = {}


def _body():
    """Triangles of the source, fitted to 12 m: (verts [(x, y, z)], tris [(i, j, k)], uvs [[(u, v)] x3], smooth []). Cached."""
    if _BODY:
        return _BODY
    if hashlib.sha256(SOURCE.read_bytes()).hexdigest() != SHA:
        raise RuntimeError(f"{SOURCE} is not the pavilion source of art/pavilion/PROVENANCE.json")
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=str(SOURCE))
    new = [o for o in bpy.data.objects if o not in before]
    body = next(o for o in new if o.type == "MESH")
    bpy.context.view_layer.update()
    me = body.data
    me.transform(body.matrix_world)
    xs = [v.co.x for v in me.vertices]
    ys = [v.co.y for v in me.vertices]
    zs = [v.co.z for v in me.vertices]
    sx, sy = max(xs) - min(xs), max(ys) - min(ys)
    assert sx >= sy, "the source is long along X (the testbed's rotate branch is not needed)"
    k = 12.0 / sx
    cx, cy, z0 = (max(xs) + min(xs)) / 2, (max(ys) + min(ys)) / 2, min(zs)
    verts = [((v.co.x - cx) * k, (v.co.y - cy) * k, (v.co.z - z0) * k) for v in me.vertices]
    uvl = me.uv_layers.active
    tris, uvs, smooth = [], [], []
    for p in me.polygons:
        assert len(p.vertices) == 3
        tris.append(tuple(p.vertices))
        uvs.append([tuple(uvl.data[li].uv) for li in p.loop_indices])
        smooth.append(p.use_smooth)
    for o in new:
        bpy.data.objects.remove(o, do_unlink=True)
    _BODY.update(verts=verts, tris=tris, uvs=uvs, smooth=smooth)
    return _BODY


def _fittings():
    """The testbed's 4 bezels and 4 lenses in the 12 m frame, mapped three (x, y up, z) -> Blender (x, -z, y). Returns [(kind, verts, tris, uv)]. Triangles wound outward."""
    out = []
    u0, v0 = .43567, 1 - .89358  # the pearl patch of the atlas (glTF v flipped for Blender)
    for x, h, z in FIX:
        # TorusGeometry(.14, .025, 4, 16), rotateX(pi/2), moved to (x, h, z)
        R, r, rad, tub = .14, .025, 4, 16
        tv, tu = [], []
        for j in range(rad + 1):
            for i in range(tub + 1):
                u, v = i / tub * 2 * math.pi, j / rad * 2 * math.pi
                px, py, pz = (R + r * math.cos(v)) * math.cos(u), (R + r * math.cos(v)) * math.sin(u), r * math.sin(v)
                tx, ty, tz = px + x, -pz + h, py + z  # rotateX(pi/2): y' = -z, z' = y
                tv.append((tx, -tz, ty))
                tu.append((u0 + (i / tub - .5) * .012, v0 - (j / rad - .5) * .012))
        tt = []
        for j in range(1, rad + 1):
            for i in range(1, tub + 1):
                a, b, c, d = (tub + 1) * j + i - 1, (tub + 1) * (j - 1) + i - 1, (tub + 1) * (j - 1) + i, (tub + 1) * j + i
                tt += [(a, b, d), (b, c, d)]
        cen = Vector((x, -z, h))
        fixed = []
        for t in tt:  # outward from the tube's own axis ring
            p = [Vector(tv[i]) for i in t]
            n = (p[1] - p[0]).cross(p[2] - p[0])
            mid = (p[0] + p[1] + p[2]) / 3
            ring = Vector((mid.x - cen.x, mid.y - cen.y, 0)).normalized() * R + cen  # nearest point on the core circle
            fixed.append(t if n.dot(mid - ring) >= 0 else (t[0], t[2], t[1]))
        out.append(("bezel", tv, fixed, tu))
        # CircleGeometry(.116, 16), facing down, at h - .008
        rr, seg = .116, 16
        # three: circle in the xy plane, rotateX(pi/2): (cx, cy, 0) -> (cx, 0, cy): ring point z' = sin; Blender y = -z'
        cv = [(x, -z, h - .008)] + [(x + rr * math.cos(i / seg * 2 * math.pi), -(z + rr * math.sin(i / seg * 2 * math.pi)), h - .008) for i in range(seg + 1)]
        cu = [(u0, v0)] + [(u0 + (math.cos(i / seg * 2 * math.pi) / 2) * .012, v0 + (math.sin(i / seg * 2 * math.pi) / 2) * .012) for i in range(seg + 1)]
        ct = [(1 + i, 2 + i, 0) for i in range(seg)]
        ct = [t if (Vector(cv[t[1]]) - Vector(cv[t[0]])).cross(Vector(cv[t[2]]) - Vector(cv[t[0]])).z < 0 else (t[0], t[2], t[1]) for t in ct]  # face down
        out.append(("lens", cv, ct, cu))
    return out


def _push(s, verts, tris, uvs, mat, smooth, k=1.0, rot=0.0):
    """Add triangles to the Shape with their own UVs; a vertex can carry only one UV per face corner, so UVs ride on the face (s._pending_uv)."""
    c, sn = math.cos(rot), math.sin(rot)
    vm = {}
    dropped = 0
    for n, t in enumerate(tris):
        vs = []
        for vi in t:
            if vi not in vm:
                x, y, z = verts[vi]
                vm[vi] = s.bm.verts.new((k * (x * c - y * sn), k * (x * sn + y * c), k * z))
            vs.append(vm[vi])
        try:
            f = s.bm.faces.new(vs)
        except ValueError:
            dropped += 1
            continue
        f.material_index = s._mi(mat)
        f.smooth = smooth[n] if isinstance(smooth, list) else smooth
        f[s._uvkeep] = 1
        s._pending_uv.append((f, [tuple(u) for u in uvs[n]]))
    if dropped:
        raise RuntimeError(f"{dropped} faces dropped (duplicate triangles) in {s.name}")


def _pavilion(s, k, rot):
    b = _body()
    _push(s, b["verts"], b["tris"], b["uvs"], pal.Pavilion_Atlas, b["smooth"], k, rot)
    for kind, v, t, u in _fittings():
        _push(s, v, t, [[u[i] for i in tri] for tri in t], pal.Brass_Civic if kind == "bezel" else pal.Pavilion_Opal, kind == "bezel", k, rot)


@asset("BoardingPavilion_TB_Square", "Plaza", bevel=0.0)
def tb_square(s):
    _pavilion(s, 0.70, 0.0)


@asset("BoardingPavilion_TB_Office", "Plaza", bevel=0.0)
def tb_office(s):
    _pavilion(s, 1.25, math.pi / 2)


from kit import REGISTRY  # noqa: E402

for _n in ("BoardingPavilion_TB_Square", "BoardingPavilion_TB_Office"):
    REGISTRY[_n]["nolod"] = True  # no decimated LODs: compression goes through Sky
