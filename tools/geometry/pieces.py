"""Kit piece solids for `bun run geometry` (brief 055, Splice): the occupied height intervals of a kit piece on a plan grid.

Blender -b -P tools/geometry/pieces.py -- <out dir> <cell m> <id>=<fbx path>[|<mat>,<mat>...[|<mat>,<mat>...]] [...]

Brief 077: `|<materials>` names the see-through materials (kit.json seeThrough) the piece carries: their triangles are drawn but stop nothing, so the
solid is voxelised without them (the trough stands, its planting does not). The bounds (bx*, bz*, minY, maxY) stay the whole mesh's: they place the piece.
Brief 107: a second `|<materials>` names the light-only materials (kit.collide.json `light`, tools/decokit/collide_light.ts): light has no body, so their triangles are
left out entirely, bounds included (a glow strip 3 mm off a wall would otherwise turn the wall's crossing parity inside out). `tris` still counts the whole mesh.

Each FBX (the Unity copy of the piece, the very mesh the player draws) is imported with the DecoKit export axes (forward -Z, up Y), so the piece
stands in the frame the kit catalogue speaks: x across, y up, z front (Blender -Y becomes +Z, Blender +X becomes kit -X: the manifest's bounds
negate x, and Unity's importer does the same for handedness).

The mesh is voxelised: 0.125 m across, 0.05 m up, to CAP m. A voxel is inside when at least two of the three axis rays through it (x, y, z, crossing
parity) say so, so an open leaf or a lone glass pane is not a solid body, a platform with no underside still is. A lone unmatched surface is kept as a
thin sheet (a pane stays a wall), and a horizontal one at floor height with nothing under it is a floor and runs to the ground. The voxels are folded to
`cell` columns (any voxel of the 2 x 2 counts, so a thin post is not lost) and written as intervals:
<out>/<id>.json  { id, cell, x0, x1, z0, z1 (the grid), bx0, bx1, bz0, bz1 (the mesh's own bounds), nx, nz, minY, maxY, tris, cells: [[lo, hi, lo, hi ...] per cell, x-major] }, heights in metres from the piece's pivot.
Deterministic: no random, no timestamps, rounded to the centimetre.
"""
import json
import math
import sys
from pathlib import Path

import bpy
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ARGS = sys.argv[sys.argv.index("--") + 1:]
OUT = Path(ARGS[0]); CELL = float(ARGS[1]); JOBS = [(a.split("=", 1)[0], *(a.split("=", 1)[1].split("|", 2) + ["", ""])[:3]) for a in ARGS[2:]]
OUT.mkdir(parents=True, exist_ok=True)
CAP = 4.0     # nothing above this is stored (nothing here is walked under or shot through above 4 m)
DX, DZ = 0.125, 0.05
FLOOR_SHEET = 1.2  # a lone horizontal surface this low with nothing under it is a floor


def base_name(n):
    """A material's kit name: Blender's import suffix (.001) dropped."""
    import re
    return re.sub(r"\.\d{3}$", "", n)


n_light = [0]  # triangles of the last load left out as light


def load(path, skip=(), light=()):
    """All vertices and triangles, and the triangles that are solid (not on a see-through material); a light-only material's triangles are not loaded at all."""
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=path, axis_forward="-Z", axis_up="Y")
    verts, tris, solid = [], [], []
    n_light[0] = 0
    for o in bpy.context.scene.objects:
        if o.type != "MESH":
            continue
        m = o.matrix_world; me = o.data; me.calc_loop_triangles(); base = len(verts)
        names = [base_name(s.material.name) if s.material else "" for s in o.material_slots]
        verts += [m @ v.co for v in me.vertices]
        for t in me.loop_triangles:
            mat = names[min(t.material_index, len(names) - 1)] if names else ""
            if light and mat in light: n_light[0] += 1; continue
            tri = tuple(base + i for i in t.vertices); tris.append(tri)
            if not (names and mat in skip): solid.append(tri)
    if light:  # only the vertices the body uses: a light's own would widen the bounds that place the piece
        used = sorted({i for t in tris for i in t}); at = {i: k for k, i in enumerate(used)}
        verts = [verts[i] for i in used]; tris = [tuple(at[i] for i in t) for t in tris]; solid = [tuple(at[i] for i in t) for t in solid]
    return verts, tris, solid


def line_hits(bvh, o, d, length):
    """Distances along the line o + t d, 0 < t < length, where it crosses the mesh, in order (none when nothing of the piece is solid)."""
    out, t = [], 0.0
    if bvh is None:
        return out
    for _ in range(96):
        h = bvh.ray_cast(o + d * (t + 1e-4), d, length - t)
        if h[0] is None:
            break
        t = (h[0] - o).dot(d); out.append(t)
    return out


def span(a, b, d, n):
    """Voxel indices a..b (metres from the grid's start) cover: those whose centres lie inside, or, for a wall thinner than a voxel, the one it stands in."""
    i0 = max(0, math.ceil(a / d - 0.5)); i1 = min(n - 1, math.floor(b / d - 0.5))
    if i1 < i0:
        i0 = i1 = min(n - 1, max(0, int((a + b) / 2 / d)))
    return i0, i1


def intervals(ts):
    """Pairs of crossings -> inside spans; a leftover is a lone surface."""
    spans = [(ts[i], ts[i + 1]) for i in range(0, len(ts) - 1, 2)]
    lone = ts[-1] if len(ts) % 2 else None
    return spans, lone


for pid, path, skipped, lit in JOBS:
    verts, tris, solid_tris = load(path, set(m for m in skipped.split(",") if m), set(m for m in lit.split(",") if m))
    bvh = BVHTree.FromPolygons(verts, solid_tris) if solid_tris else None
    xs = [v.x for v in verts]; ys = [v.y for v in verts]; zs = [v.z for v in verts]
    bx0, bx1, by0, by1, bz0 = min(xs), max(xs), min(ys), max(ys), min(zs)
    bz1 = min(max(zs), CAP)
    nx = max(1, math.ceil((bx1 - bx0) / DX)); ny = max(1, math.ceil((by1 - by0) / DX)); nz = max(1, math.ceil((bz1 - bz0) / DZ))
    ins = np.zeros((3, nx, ny, nz), dtype=bool); sheet = np.zeros((nx, ny, nz), dtype=bool)
    cx = lambda i: bx0 + (i + 0.5) * DX
    cy = lambda j: by0 + (j + 0.5) * DX
    cz = lambda k: bz0 + (k + 0.5) * DZ
    ex, ey, ez = Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))
    pad = 0.01
    # x rays
    for j in range(ny):
        for k in range(nz):
            o = Vector((bx0 - pad, cy(j), cz(k))); ts = line_hits(bvh, o, ex, (bx1 - bx0) + 2 * pad)
            sp, lone = intervals(ts)
            for a, b in sp:
                i0, i1 = span(o.x + a - bx0, o.x + b - bx0, DX, nx)
                ins[0, i0:i1 + 1, j, k] = True
            if lone is not None: sheet[min(nx - 1, max(0, int((o.x + lone - bx0) / DX))), j, k] = True
    # y rays
    for i in range(nx):
        for k in range(nz):
            o = Vector((cx(i), by0 - pad, cz(k))); ts = line_hits(bvh, o, ey, (by1 - by0) + 2 * pad)
            sp, lone = intervals(ts)
            for a, b in sp:
                j0, j1 = span(o.y + a - by0, o.y + b - by0, DX, ny)
                ins[1, i, j0:j1 + 1, k] = True
            if lone is not None: sheet[i, min(ny - 1, max(0, int((o.y + lone - by0) / DX))), k] = True
    # z rays (from below, so the lowest crossing is the first)
    top = max(zs) + 0.2
    for i in range(nx):
        for j in range(ny):
            o = Vector((cx(i), cy(j), bz0 - pad)); ts = line_hits(bvh, o, ez, (top - bz0) + 2 * pad)
            sp, lone = intervals(ts)
            for a, b in sp:
                k0, k1 = span(o.z + a - bz0, o.z + b - bz0, DZ, nz)
                ins[2, i, j, k0:k1 + 1] = True
            if lone is not None:
                zl = o.z + lone
                if len(ts) == 1 and zl - bz0 <= FLOOR_SHEET: sheet[i, j, 0:min(nz, int((zl - bz0) / DZ) + 1)] = True  # a floor with no underside
                elif zl - bz0 < CAP: sheet[i, j, min(nz - 1, max(0, int((zl - bz0) / DZ)))] = True
    solid = (ins.sum(axis=0) >= 2) | sheet
    # fold to cell columns, in kit axes: kit x = -Blender x, kit z = -Blender y
    fold = max(1, round(CELL / DX))
    cnx = max(1, math.ceil(nx / fold)); cny = max(1, math.ceil(ny / fold))
    padded = np.zeros((cnx * fold, cny * fold, nz), dtype=bool); padded[:nx, :ny, :] = solid
    col = padded.reshape(cnx, fold, cny, fold, nz).any(axis=(1, 3))  # [cx, cy, z] in Blender axes
    ux0, ux1 = -bx1, -bx0; uz0, uz1 = -by1, -by0
    kx = cnx; kz = cny
    cells = []
    for i in range(kx):
        for j in range(kz):
            c = col[cnx - 1 - i, cny - 1 - j, :]  # flip both: kit axes run opposite to Blender's
            iv, k = [], 0
            while k < nz:
                if c[k]:
                    k0 = k
                    while k < nz and c[k]: k += 1
                    iv += [round(bz0 + k0 * DZ, 2), round(bz0 + k * DZ, 2)]
                else: k += 1
            cells.append(iv)
    # kit-frame bounds are those of the folded grid, anchored at the kit-x/z minimum (the grid's flipped edge lies on the far side of the fold)
    gx0 = ux1 - kx * CELL; gz0 = uz1 - kz * CELL  # fold cells are anchored at the Blender minimum, i.e. the kit maximum
    (OUT / f"{pid}.json").write_text(json.dumps(dict(id=pid, cell=CELL, x0=round(gx0, 4), x1=round(ux1, 4), z0=round(gz0, 4), z1=round(uz1, 4), bx0=round(ux0, 4), bx1=round(ux1, 4), bz0=round(uz0, 4), bz1=round(uz1, 4), nx=kx, nz=kz, minY=round(min(zs), 4), maxY=round(max(zs), 4), tris=len(tris) + n_light[0], **({"seeThrough": sorted(set(m for m in skipped.split(",") if m)), "solidTris": len(solid_tris)} if skipped else {}), cells=cells), separators=(",", ":")))
    print(f"piece {pid}: {kx}x{kz} cells x {nz} layers, {len(tris)} tris")
