"""Brief 097 (Vela; Redline's render audit item 1, Sky 2026-11-05 "yes"): baked vertex-colour ambient occlusion for the Handover zone's kit pieces.

Convention (DecoKit/Lit Cutaway and Complex Lit Cutaway, keyword _DK_VERTEX_AO): occlusion = vertex colour R, G = B = R, A = 1. The FBX holds the
occlusion value itself (linear data, no sRGB decode in the shader): export_kit writes the colour with colors_type SRGB, so the value goes through the
attribute's color_srgb accessor; vertex_ao_extra.py exports LINEAR (the 077 root's planting colours), so it goes through `color`. Either way the
file carries R = occlusion. Faces with planting or clear materials (NO_AO) keep their colour (the testbed leaves' albedo `Col`) or stay white.

  bake(objs, space, cut=True) -> (tris before, tris after)
    1. cut (L0 only): where a large flat face meets a contact (ground, cornice, band, riser: both ends of one of its edges occluded), add up to
       two cut lines parallel to that edge (CUTS from it) so the gradient sits near the contact; biggest faces first, within BUDGET (+15 %) of the
       piece's triangles. Warped n-gons the cuts leave are triangulated (Unity's FBX importer drops them).
    2. bake: per face corner, RAYS cosine-weighted rays over the corner's normal (fixed directions, turned per point by a hash of its position:
       deterministic) against every face of the piece plus a ground plane at z 0; a hit at distance t occludes 1 - t/DIST. raw = 1 - mean;
       out = MIN + (1 - MIN) * clamp((raw - LOW) / (OPEN - LOW)): open faces stay at exactly 1.0, the deepest contact at MIN. A corner
       samples INSET into its own face, so a vertex sitting in a joint or a wall reads the contact beside it, not the inside of the solid.
PIECES: the zone pieces export_kit bakes (opt-in by name; `--no-ao` turns it off for a run). EXTRA: the out/decokit-077 rows (vertex_ao_extra.py)."""
import math
import re

import bmesh
from mathutils import Vector
from mathutils.bvhtree import BVHTree

PIECES = frozenset({
    "RecordsEntrance", "RecordsEntrance_Official", "CivicCabinet", "CafeBay_v2", "Parasol_Testbed", "CafeSet_Tulip_Play",
    "CafeLamp_Testbed_Amber", "CafeLamp_Testbed_Coral", "CafeLamp_Testbed_Lilac", "CafeLamp_Testbed_Peach", "CafeLamp_Testbed_Teal",
    "AtomFountain_Testbed", "BoardingPavilion_TB_Square", "PlazaSphereLamp_Play", "Cypress_Play", "PottedTopiary_Play",
    "Kerb_Pearl_0", "Kerb_Pearl_1", "TreePit_Pearl", "HoverCamera",
    "PolicePost", "MastCamera", "CivicRobotDock", "Dumpster_Civic",  # brief 097 props (civic_props097.py)
    "Lockers_Play", "RecyclingBins_Play", "VendingMachine_Cola_Play", "VendingMachine_Snack_Play", "PhoneBooth_Play",  # brief 097 passes 2-3 (civic_props097b.py)
    "Hydrant_Play", "BikeRack_Play", "Bench_Play", "UtilityCabinet_Play", "PhoneBooth_Single", "BollardSphere",  # brief 097 full pass (civic_props097c.py)
    "Streetlamp_Play", "Bollard_Play", "SandwichBoard_Play", "Tile_Tactile_Play", "Roof_Tank_Play", "Roof_Mast_Play",  # final pass (civic_props097d.py)
    "AlleyWall_3", "AlleyWall_4", "AlleyWall_3D", "AlleyWall_4D",  # the zone's service walls: contact cuts and AO only
})
# The zone's 077 rows, and the other trough rows: their stone shares one object (and its colour attribute) with the testbed leaves, and the
# stone corners hold black there; baked, the stone reads AO wherever DK_Limestone takes the keyword.
EXTRA = ("RecordsTower", "CafeUpper_3", "CafeTerrace", "PlanterRun_4_A", "PlanterRun_4_B", "PlanterRun_4_C", "PlanterArc", "GlobeLamp_TB",
         "PlanterRun_2_A", "PlanterRun_2_B", "PlanterRun_2_C", "PlanterRun_Corner_A", "PlanterRun_Corner_B", "PlanterRun_Corner_C",
         "PlanterRun_End_A", "PlanterRun_End_B", "PlanterRun_End_C")
DIST, MIN, LOW, OPEN, RAYS, EPS = 0.8, 0.35, 0.2, 0.9, 64, 2e-3  # [S] soft contact over 0.8 m; the floor; raw <= 0.2 is the floor, raw >= 0.9 open
LAYER = 0.05                                                      # a parallel surface this close above a face is a layer on it (a plate, band, ring), not an occluder
INSET = 0.04                                                      # a corner samples 4 cm into its face (never 25 % past its centre): not buried in a joint
CUTS, BUDGET, DARK = (0.18, 0.5), 0.15, 0.97           # cut offsets from the contact edge (m); +15 % L0 triangles; "occluded" below this
ATTR = "Col"
NO_AO = re.compile(r"Leaf|Petal|Bloom|Flower|GroundCover|CoreShrub|Soil|Glass|Lamp|Light|Jet|Foam|Water|Lens|Petiole")
CLEAR = re.compile(r"Glass|Water|Jet|Foam|Light")      # never occluders (see-through or moving)
_GOLD = math.pi * (3 - math.sqrt(5))
_DIRS = [(math.sqrt((i + 0.5) / RAYS) * math.cos(i * _GOLD), math.sqrt((i + 0.5) / RAYS) * math.sin(i * _GOLD), math.sqrt(1 - (i + 0.5) / RAYS))
         for i in range(RAYS)]


def _mat(me, i):
    m = me.materials[i] if i < len(me.materials) else None
    return m.name.split(".")[0] if m else ""


def tree_of(objs, ground=True):
    """One BVH over every occluding face of the piece (world space) and a ground plane at z 0 well past it."""
    verts, polys = [], []
    for o in objs:
        me, mw = o.data, o.matrix_world
        base = len(verts)
        verts += [mw @ v.co for v in me.vertices]
        polys += [[base + k for k in p.vertices] for p in me.polygons if not CLEAR.search(_mat(me, p.material_index))]
    if ground and verts:
        lo = Vector((min(v.x for v in verts), min(v.y for v in verts)))
        hi = Vector((max(v.x for v in verts), max(v.y for v in verts)))
        r, b = max((hi - lo).length, 10.0), len(verts)
        verts += [Vector((lo.x - r, lo.y - r, 0)), Vector((hi.x + r, lo.y - r, 0)), Vector((hi.x + r, hi.y + r, 0)), Vector((lo.x - r, hi.y + r, 0))]
        polys.append([b, b + 1, b + 2, b + 3])
    return BVHTree.FromPolygons(verts, polys, all_triangles=False, epsilon=0.0)


def sample(tree, co, n):
    """Occlusion at a point with normal n: 1.0 open, MIN at the deepest contact."""
    n = n.normalized()
    if n.length < 0.5:
        return 1.0
    t = Vector((1, 0, 0)) if abs(n.x) < 0.9 else Vector((0, 1, 0))
    t = (t - n * t.dot(n)).normalized()
    b = n.cross(t)
    h = (int(round(co.x * 1000)) * 73856093 ^ int(round(co.y * 1000)) * 19349663 ^ int(round(co.z * 1000)) * 83492791) & 0xFFFFFFFF
    a = h / 4294967296.0 * 2 * math.pi
    ca, sa = math.cos(a), math.sin(a)
    t, b = t * ca + b * sa, b * ca - t * sa
    o, occ = co + n * EPS, 0.0
    for x, y, z in _DIRS:
        d = t * x + b * y + n * z
        hit, start, far = tree.ray_cast(o, d, DIST), 0.0, DIST
        for _ in range(3):  # a layer laid on the face (inlay, band, decal plate: parallel, within LAYER) is skipped: the face under it is unseen
            if hit[0] is None or start + hit[3] > LAYER / max(z, 0.05) or abs(hit[1].dot(n)) < 0.9:
                break
            start += hit[3] + 1e-3
            hit = tree.ray_cast(o + d * start, d, far - start)
        if hit[0] is not None:
            occ += 1.0 - (start + hit[3]) / DIST
    raw = 1.0 - occ / RAYS
    return MIN + (1.0 - MIN) * max(0.0, min(1.0, (raw - LOW) / (OPEN - LOW)))


def _tris(bm):
    return sum(len(f.verts) - 2 for f in bm.faces)


def _flat(f):
    n, p0 = f.normal, f.verts[0].co
    return max(abs((v.co - p0).dot(n)) for v in f.verts) < 1e-3


def _convex(f):
    n, vs = f.normal, [v.co for v in f.verts]
    return all((vs[i] - vs[i - 1]).cross(vs[(i + 1) % len(vs)] - vs[i]).dot(n) > -1e-9 for i in range(len(vs)))


def _plans(obj, bm, tree):
    """Cut candidates for flat faces with an occluded edge: (priority, face index, planes) where planes is [(plane co, plane no)]
    (a quad: cuts parallel to its occluded edges) or [("inset", thickness)] (a convex n-gon: rings inset from its whole edge, which
    costs 2 triangles a side instead of slicing the n-gon and its neighbours)."""
    me, mw, out = obj.data, obj.matrix_world, []
    for f in bm.faces:
        if len(f.verts) < 3 or NO_AO.search(_mat(me, f.material_index)) or not _flat(f):
            continue
        area = f.calc_area()
        if area < 0.25:
            continue
        n, cuts, ngon = f.normal, [], len(f.verts) > 4
        if ngon and not _convex(f):
            continue
        cen = f.calc_center_median()
        for lp in f.loops:
            a, b = lp.vert.co, lp.link_loop_next.vert.co
            d = b - a
            if d.length < 0.05:
                continue
            inward = n.cross(d).normalized()
            ext = max((v.co - a).dot(inward) for v in f.verts)
            offs = [c for c in CUTS if c < ext - 0.1]
            if not offs:
                continue
            nw = (mw.to_3x3() @ n).normalized()
            pa, pb = mw @ (a + inward * 0.02 + d * 0.02), mw @ (b + inward * 0.02 - d * 0.02)
            if sample(tree, pa, nw) < DARK and sample(tree, pb, nw) < DARK:
                if ngon:
                    r = min((cen - l.vert.co).dot(n.cross(l.link_loop_next.vert.co - l.vert.co).normalized()) for l in f.loops)
                    cuts = [("inset", c) for c in CUTS if c < r - 0.1]
                    break
                cuts += [(a + inward * c, inward.copy()) for c in offs]
        if cuts:
            out.append((-area, f.index, cuts))
    out.sort(key=lambda r: (r[0], r[1]))
    return out


def _split_warped(bm, faces):
    warped = []
    for f in faces:
        if not f.is_valid or len(f.verts) <= 4:
            continue
        n, p0 = f.normal, f.verts[0].co
        tol = 1e-3 * max(1.0, math.sqrt(max(f.calc_area(), 0.0)))
        if max(abs((v.co - p0).dot(n)) for v in f.verts) > tol:
            warped.append(f)
    if warped:
        bmesh.ops.triangulate(bm, faces=warped, quad_method="BEAUTY", ngon_method="BEAUTY")


def _around(faces):
    return {g for f in faces if f.is_valid for v in f.verts for g in v.link_faces}


def _apply(me, plans):
    """A bmesh of `me` with `plans` cut; returns (bm, triangles each plan added)."""
    bm = bmesh.new()
    bm.from_mesh(me)
    bm.faces.ensure_lookup_table()
    faces, deltas = list(bm.faces), []
    for _, fi, planes in plans:
        if not faces[fi].is_valid:
            deltas.append(0)
            continue
        t0 = sum(len(f.verts) - 2 for f in _around([faces[fi]]))
        geom_faces = [faces[fi]]
        if planes[0][0] == "inset":
            done = 0.0
            for _, c in planes:  # the original face stays the inner one; each ring is the gap from the last
                bmesh.ops.inset_individual(bm, faces=[faces[fi]], thickness=c - done, depth=0.0, use_even_offset=True, use_interpolate=True, use_relative_offset=False)
                done = c
            deltas.append(2 * len(faces[fi].verts) * len(planes))  # each ring: one quad a side; the inner face keeps its triangles
            continue
        for co, no in planes:
            live = [f for f in geom_faces if f.is_valid]
            geom = live + list({e for f in live for e in f.edges}) + list({v for f in live for v in f.verts})
            ret = bmesh.ops.bisect_plane(bm, geom=geom, dist=1e-4, plane_co=co, plane_no=no, use_snap_center=False, clear_outer=False, clear_inner=False)
            geom_faces = list(dict.fromkeys(f for f in live + [g for g in ret["geom"] if isinstance(g, bmesh.types.BMFace)] if f.is_valid))
        _split_warped(bm, _around(geom_faces))
        deltas.append(sum(len(f.verts) - 2 for f in _around(geom_faces)) - t0)
    return bm, deltas


def cut(objs, tree, budget):
    """Cut lines near contacts on the largest flat faces of objs, adding at most `budget` triangles in all (warped n-gons the cuts leave in
    neighbours are triangulated). A dry run prices each face's cuts; the biggest faces that fit are cut. Returns triangles added."""
    added = 0
    for obj in objs:
        me = obj.data
        before = sum(len(p.vertices) - 2 for p in me.polygons)
        probe = bmesh.new()
        probe.from_mesh(me)
        plans = _plans(obj, probe, tree)
        probe.free()
        if not plans:
            continue
        bm, deltas = _apply(me, plans)
        bm.free()
        chosen, room = [], budget - added
        for plan, d in zip(plans, deltas):
            if 0 < d <= room:
                chosen.append(plan)
                room -= d
        while chosen:
            bm, _ = _apply(me, chosen)
            gained = _tris(bm) - before
            if added + gained <= budget:
                bm.to_mesh(me)
                me.update()
                bm.free()
                added += gained
                break
            bm.free()
            chosen = chosen[:max(0, len(chosen) - max(1, len(chosen) // 10))]
    return added


def write(objs, tree, space):
    """Bake per corner into the colour attribute ATTR (or the mesh's existing one: planting faces keep theirs)."""
    acc = "color_srgb" if space == "SRGB" else "color"
    for o in objs:
        me, mw = o.data, o.matrix_world
        rot = mw.to_3x3()
        if all(NO_AO.search(_mat(me, p.material_index)) for p in me.polygons):
            continue  # a planting, soil or glass-only object keeps its mesh as it was (no colour added)
        attr =me.color_attributes.get(ATTR) or (me.color_attributes[0] if len(me.color_attributes) else None)
        fresh = attr is None
        if fresh:
            attr = me.color_attributes.new(ATTR, "BYTE_COLOR", "CORNER")
        cols = [0.0] * (len(attr.data) * 4)
        attr.data.foreach_get(acc, cols)
        if fresh:
            cols = [1.0] * len(cols)
        cn = me.corner_normals
        cache = {}
        for p in me.polygons:
            if NO_AO.search(_mat(me, p.material_index)):
                continue
            mid = mw @ p.center
            for li in p.loop_indices:
                vi = me.loops[li].vertex_index
                co, n = mw @ me.vertices[vi].co, (rot @ cn[li].vector)
                to = mid - co
                co = co + to * min(0.25, INSET / to.length) if to.length > 1e-6 else co
                key = (round(co.x, 4), round(co.y, 4), round(co.z, 4), round(n.x, 3), round(n.y, 3), round(n.z, 3))
                ao = cache.get(key)
                if ao is None:
                    ao = cache[key] = round(sample(tree, co, n), 4)
                k = (li if attr.domain == "CORNER" else vi) * 4
                cols[k:k + 4] = (ao, ao, ao, 1.0)
        attr.data.foreach_set(acc, cols)
        me.color_attributes.active_color = attr
        me.color_attributes.render_color_index = me.color_attributes.find(attr.name)


def bake(objs, space="SRGB", cut_contacts=True, separate=False):
    """Cut (optional) and bake AO on a piece's objects. Returns (tris before, tris after).
    separate: each object is baked against itself and the ground only (export_kit's objects are the root and its movable parts,
    geo.Shape.part: a turning orbit or camera head must not carry the shade of where it stood at build time)."""
    objs = [o for o in objs if o.type == "MESH"]
    before = sum(len(p.vertices) - 2 for o in objs for p in o.data.polygons)
    budget = int(before * BUDGET)
    for group in ([[o] for o in objs] if separate else [objs]):
        tree = tree_of(group)
        if cut_contacts:
            budget -= cut(group, tree, budget)
        write(group, tree, space)
    after = sum(len(p.vertices) - 2 for o in objs for p in o.data.polygons)
    return before, after
