"""Collision and hit volumes of kit pieces (brief 055, Vela): the simple solids the sim's walk grid and occluders are built from, measured from the shipped mesh, never placed by hand.

Blender -b -P tools/decokit/collide.py -- <out json> <id>=<fbx path> [...]      (run by `bun tools/decokit/collide.ts`, which keeps contracts/world/kit.collide.json)

Frame: the kit's own (x across, y up, z front; metres from the piece's pivot; yaw and scale apply as the piece is placed). The FBX is imported with the DecoKit export axes and
turned into it the way the catalogue's bounds are (kit x = -Blender x, kit z = -Blender y), as tools/geometry/bounds.py does.

What is measured. The mesh surface is sampled every 4 cm and dropped into a plan grid (0.1 m; 0.25 m for pieces over 12 m) and 2.5 cm height bins. A column's occupied bins are the runs
of height something is drawn at; runs less than a step apart (0.35 m) are one, so a bench with legs and a booth with a roof are solid from foot to top, and an awning or a ring
2 m up over open ground is its own run, dropped. A cell that no surface reaches but that its neighbours close in (the inside of a kiosk or a basin) is filled with its walls' height. Cells with the
same run are merged into boxes; a footprint that is a disc becomes one cylinder. The result is a superset of the drawn solid at 10 cm, never a hand-placed shape.

Each volume: { shape: box | cylinder, min [x, y, z], max [x, y, z] (a cylinder: c [x, z], r, y0, y1), stand? } in metres rounded to the centimetre.
`stand` is true when the top of the volume is a drawn flat surface (60 % of its footprint within 3 cm of the top): a person can stand on it, if the step up to it is small enough. Volumes
come from the ground up to 1.9 m; anything above that (a canopy, palm fronds) is not a body or a shot. A piece that is wholly above 2.2 m (a bridge, a deck) keeps every volume and says
`elevated`.
How a walker reads a volume at floor height f: it blocks when y0 < f + 1.8 and y1 > f + 0.35, unless `stand` and y1 - f <= 0.35 (a step). A shot at height h is stopped when y0 <= h <= y1.
Thin open parts are not solids (Aster's ruling, brief 055: a ring, hoop, railing, wire, lattice or frond stops neither a walker nor a shot). They are found in the mesh before anything is
rasterised, part by part (open_parts): a palm or fern frond (or its stalk, by material: FROND) is always open, wherever it hangs (v8: a potted palm is its pot); any other part is open when it fills less than OPEN of its outline in every broad view, or is a rod that does not stand upright. Open parts are left out of the
volumes, and the piece lists a box around each in `thin` (`thin: true`; for information, nothing reads them as a blocker). A pole, a panel, a drum or a box is not open and stays.
Thin curved walls (opt-in per piece, CHAINED; the café's curved front): a ragged footprint keeps only its larger rectangles, which drops a one-cell-wide curve entirely (a glazed front on an arc was a free walk across
4 m of drawn wall). So after the volumes are made, every column that is blocked (a walker's body would meet it) but lies in no volume of its own height is gathered into a chain of boxes, one per
CHAIN (0.5 m) tile of plan, each the box of the standing surfaces (walls, not tops or awnings) measured in that tile, from the column's foot to its top. Nothing is placed by hand.
Deterministic: no random, no timestamps."""
import json
import math
import sys
from collections import deque
from pathlib import Path

import bpy
import numpy as np

import os
DEBUG = bool(os.environ.get('COLLIDE_DEBUG'))
ARGS = sys.argv[sys.argv.index("--") + 1:]
OUT = Path(ARGS[0]); JOBS = [a.split("=", 1) for a in ARGS[1:]]

STEP = 0.04        # surface sample spacing, m
BIN = 0.025        # height bin, m
HEAD = 1.8         # a person's height: runs closer than this are one solid
STEPUP = 0.35      # the most a person steps up
KEEP = 1.9         # volumes start below this (a person is 1.8 m; a shot is at 1.6 m at most)
MAXBOX = (16, 32, 64)  # volumes a piece may have (under 4 m across, under 12 m, larger) before the heights are coarsened
STAND = 1.3        # the highest flat top that is kept exact (a person stands on a landing, not on a shelf)
FLAT = 0.6         # share of a footprint that is a drawn flat top for `stand`
BIG = 12.0         # pieces larger than this use the coarse plan cell
FAR = 90.0         # nothing this large is a street object: bounds only
OPEN = 0.35        # a part that fills less than this of its outline in every broad view is a frame, not a body
PLANE = 0.3        # a view counts when the part is at least this across both ways
FROND = {"DK_Leaf_PalmFan", "DK_Leaf_PalmFeather", "DK_Petiole"}  # palm and fern fronds and their stalks: open to a walker and a shot, wherever they hang (brief 055, v8). Dense foliage (shrub, topiary, cypress) stays solid.
CHAINED = {"CafeBay_v2"}  # pieces whose curved thin walls are kept as a chain (opt-in: the slivers of every other piece are dropped on purpose, a curve is a few strips there). Add a piece here when a walk or a shot crosses a wall it draws.
CHAIN = 0.5        # the plan tile of a wall chain: a wall volume is never longer than this on either axis
CHAINMAX = 96      # wall-chain boxes a piece may add
THINBOX = 12       # thin parts listed per piece (boxes around them, for information)


def load(path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=path, axis_forward="-Z", axis_up="Y")
    verts, tris, frond = [], [], []
    for o in bpy.context.scene.objects:
        if o.type != "MESH":
            continue
        m = o.matrix_world; me = o.data; me.calc_loop_triangles(); base = len(verts)
        verts += [tuple(m @ v.co) for v in me.vertices]
        tris += [tuple(base + i for i in t.vertices) for t in me.loop_triangles]
        frond += [bool(me.materials) and t.material_index < len(me.materials) and me.materials[t.material_index].name in FROND for t in me.loop_triangles]
    V = np.array(verts, dtype=np.float64).reshape(-1, 3)
    V = np.stack([-V[:, 0], V[:, 2], -V[:, 1]], axis=1)  # kit frame: x, y up, z
    return V, np.array(tris, dtype=np.int64).reshape(-1, 3), np.array(frond, dtype=bool)


def samples(V, T, step):
    """Points on every triangle, no further apart than `step` along its longest edge. Yields (n, 3) chunks."""
    P0, P1, P2 = V[T[:, 0]], V[T[:, 1]], V[T[:, 2]]
    e = np.maximum.reduce([np.linalg.norm(P1 - P0, axis=1), np.linalg.norm(P2 - P1, axis=1), np.linalg.norm(P0 - P2, axis=1)])
    n = np.clip(np.ceil(e / step).astype(np.int64), 1, 600)
    for k in np.unique(n):
        idx = np.nonzero(n == k)[0]
        ij = np.array([(i, j) for i in range(k + 1) for j in range(k + 1 - i)], dtype=np.float64) / k
        per = max(1, 4_000_000 // len(ij))
        for s in range(0, len(idx), per):
            t = idx[s:s + per]
            a, b = ij[:, 0][None, :, None], ij[:, 1][None, :, None]
            pts = P0[t][:, None, :] + a * (P1[t] - P0[t])[:, None, :] + b * (P2[t] - P0[t])[:, None, :]
            yield pts.reshape(-1, 3)


def runs_of(bins):
    """[(first, last)] of consecutive ints in a sorted array."""
    out, s, p = [], bins[0], bins[0]
    for b in bins[1:]:
        if b != p + 1:
            out.append((s, p)); s = b
        p = b
    out.append((s, p))
    return out


def solid_runs(bins):
    """Runs of occupied height bins, those closer than a step joined (a run higher than that is its own: a ring over a basin is not part of the basin)."""
    merged = []
    for s, e in runs_of(bins):
        if merged and (s - merged[-1][1]) * BIN < STEPUP:
            merged[-1] = (merged[-1][0], e)
        else:
            merged.append((s, e))
    return merged


def rects(cells):
    """Greedy rectangles over a set of (i, j) cells -> [(i0, j0, i1, j1)] (inclusive)."""
    left, out = set(cells), []
    for i, j in sorted(cells):
        if (i, j) not in left:
            continue
        j1 = j
        while (i, j1 + 1) in left:
            j1 += 1
        i1 = i
        while all((i1 + 1, jj) in left for jj in range(j, j1 + 1)):
            i1 += 1
        for ii in range(i, i1 + 1):
            for jj in range(j, j1 + 1):
                left.discard((ii, jj))
        out.append((i, j, i1, j1))
    return out


def neighbours(P, A, B, fill_new, rounds):
    """Dilate a plan mask by `rounds` cells, new cells taking the lowest foot and the highest top of the neighbours they grew from."""
    P, A, B = P.copy(), A.copy(), B.copy()
    for _ in range(rounds):
        nA, nB, nP = np.full(A.shape, np.inf), np.full(B.shape, -np.inf), np.zeros(P.shape, bool)
        for di, dj in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            sP = np.roll(P, (di, dj), axis=(0, 1)); sA = np.roll(A, (di, dj), axis=(0, 1)); sB = np.roll(B, (di, dj), axis=(0, 1))
            if di:
                (sP[:1, :] if di > 0 else sP[-1:, :]).fill(False)
            if dj:
                (sP[:, :1] if dj > 0 else sP[:, -1:]).fill(False)
            nP |= sP; nA = np.where(sP, np.minimum(nA, sA), nA); nB = np.where(sP, np.maximum(nB, sB), nB)
        new = nP & ~P
        P = P | new; A = np.where(new, nA, A); B = np.where(new, nB, B)
    return P, A, B


def eroded(P, rounds):
    for _ in range(rounds):
        Q = P.copy()
        Q[1:, :] &= P[:-1, :]; Q[:-1, :] &= P[1:, :]; Q[:, 1:] &= P[:, :-1]; Q[:, :-1] &= P[:, 1:]
        Q[0, :] = False; Q[-1, :] = False; Q[:, 0] = False; Q[:, -1] = False
        P = Q
    return P


def components(mask):
    """4-connected components of a plan mask -> [[(i, j)]], in scan order."""
    seen, out = np.zeros(mask.shape, bool), []
    for i, j in zip(*np.nonzero(mask)):
        if seen[i, j]:
            continue
        comp, q = [], deque([(int(i), int(j))]); seen[i, j] = True
        while q:
            a, b = q.popleft(); comp.append((a, b))
            for di, dj in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                n = (a + di, b + dj)
                if 0 <= n[0] < mask.shape[0] and 0 <= n[1] < mask.shape[1] and mask[n] and not seen[n]:
                    seen[n] = True; q.append(n)
        out.append(comp)
    return out


def joined(vols):
    """Boxes that touch along a whole face and are the same height, foot and kind are one box."""
    vols = list(vols)
    again = True
    while again:
        again = False
        for a in range(len(vols)):
            for b in range(a + 1, len(vols)):
                u, v = vols[a], vols[b]
                if u["shape"] != "box" or v["shape"] != "box" or abs(u["min"][1] - v["min"][1]) > 0.011 or abs(u["max"][1] - v["max"][1]) > 0.011 or u.get("stand") != v.get("stand"):
                    continue
                for ax, o in ((0, 2), (2, 0)):
                    if abs(u["min"][o] - v["min"][o]) < 0.011 and abs(u["max"][o] - v["max"][o]) < 0.011 and (abs(u["max"][ax] - v["min"][ax]) < 0.011 or abs(v["max"][ax] - u["min"][ax]) < 0.011):
                        w = {"shape": "box", "min": [min(u["min"][k], v["min"][k]) for k in range(3)], "max": [max(u["max"][k], v["max"][k]) for k in range(3)]}
                        if u.get("stand"):
                            w["stand"] = True
                        vols[a] = w; del vols[b]; again = True
                        break
                if again:
                    break
            if again:
                break
    return vols


def open_parts(V, T, frond):
    """Thin open parts of the mesh, found by welded mesh part (triangles that share corners): a boolean per triangle, and the boxes of the parts taken.
    A part is open when it is a frame, not a body: seen square-on it fills less than OPEN of its outline in every view that is at least PLANE across (a ring, a hoop, a lattice, a railing, a wire, a
    frond), or it is a rod (thin in two directions) that does not stand upright (a rail, a wire, a strut). An upright rod (a pole) and anything with a filled face (a panel, a drum, a box) stay."""
    key = np.round(V * 1000).astype(np.int64)
    _, inv = np.unique(key, axis=0, return_inverse=True); inv = inv.reshape(-1)
    lab = np.arange(int(inv.max()) + 1)
    u = np.concatenate([inv[T[:, 0]], inv[T[:, 1]]]); v = np.concatenate([inv[T[:, 1]], inv[T[:, 2]]])
    while True:  # smallest corner number of each welded part, by hooking and jumping
        lu, lv = lab[u], lab[v]; m = np.minimum(lu, lv)
        new = lab.copy(); np.minimum.at(new, lu, m); np.minimum.at(new, lv, m)
        while True:
            jump = new[new]
            if (jump == new).all():
                break
            new = jump
        if (new == lab).all():
            break
        lab = new
    part = lab[inv[T[:, 0]]]
    order = np.argsort(part, kind="stable"); sp = part[order]
    cuts = np.nonzero(np.diff(sp))[0] + 1
    drop = frond.copy(); boxes = []
    for idx in np.split(order, cuts):
        if len(idx) < 12:
            continue  # a bead or a stud: too small to be a frame
        pts = np.concatenate(list(samples(V, T[idx], 0.08)))  # a view's cells are 10 cm: a sample every 8 cm fills them
        lo, hi = pts.min(axis=0), pts.max(axis=0); ext = hi - lo
        planes = []
        for ax in range(3):
            o = [k for k in range(3) if k != ax]
            if ext[o[0]] >= PLANE and ext[o[1]] >= PLANE:
                c = max(0.1, float(max(ext[o])) / 80)
                w = int(math.floor(ext[o[0]] / c + 1e-9)) + 1; h = int(math.floor(ext[o[1]] / c + 1e-9)) + 1
                k = np.floor((pts[:, o[0]] - lo[o[0]]) / c).astype(np.int64) * h + np.floor((pts[:, o[1]] - lo[o[1]]) / c).astype(np.int64)
                planes.append(len(np.unique(k)) / (w * h))
        if planes:
            is_open = max(planes) < OPEN
        else:  # a rod, or a bead: a pole stands, a bead is small, a rail, a wire or a strut is long and does not stand
            is_open = max(ext) >= PLANE and not (ext[1] >= STEPUP and ext[1] >= 2 * max(ext[0], ext[2]))
        if frond[idx].mean() > 0.5:
            is_open = True  # a frond or its stalk: open however full the card is drawn
        if is_open:
            drop[idx] = True
            boxes.append({"shape": "box", "thin": True, "min": [round(float(c), 2) for c in lo], "max": [round(float(c), 2) for c in hi]})
    boxes.sort(key=lambda v: -((v["max"][0] - v["min"][0]) * (v["max"][2] - v["min"][2]) * (v["max"][1] - v["min"][1]) + 1e-6 * v["max"][1]))
    return drop, boxes[:THINBOX]


def wall_chain(V, T, blocked, A, B, lo, hi, cell, nx, nz, vols):
    """Boxes for the blocked columns that no volume covers at their own height (see the module note): [{shape: box, min, max}], at most CHAINMAX."""
    ci = lo[0] + (np.arange(nx) + 0.5) * cell; cj = lo[2] + (np.arange(nz) + 0.5) * cell
    X, Z = np.meshgrid(ci, cj, indexing="ij")
    held = np.zeros((nx, nz), bool)
    for v in vols:
        if v["shape"] == "box":
            if v["max"][1] < 0.35:
                continue
            inside = (X >= v["min"][0] - 0.011) & (X <= v["max"][0] + 0.011) & (Z >= v["min"][2] - 0.011) & (Z <= v["max"][2] + 0.011)
            y0, y1 = v["min"][1], v["max"][1]
        else:
            inside = (X - v["c"][0]) ** 2 + (Z - v["c"][1]) ** 2 <= (v["r"] + 0.011) ** 2
            y0, y1 = v["y0"], v["y1"]
        held |= inside & (y0 <= A + 0.1) & (y1 >= B - 0.1)
    unc = blocked & ~held
    if not unc.any():
        return []
    P0, P1, P2 = V[T[:, 0]], V[T[:, 1]], V[T[:, 2]]
    nrm = np.cross(P1 - P0, P2 - P0); nl = np.linalg.norm(nrm, axis=1)
    side = np.nonzero((nl > 1e-12) & (np.abs(nrm[:, 1]) < 0.5 * nl))[0]  # walls, not tops, treads or an awning
    tx, tz = int(math.ceil((hi[0] - lo[0]) / CHAIN)) + 1, int(math.ceil((hi[2] - lo[2]) / CHAIN)) + 1
    mnx, mxx = np.full((tx, tz), np.inf), np.full((tx, tz), -np.inf)
    mnz, mxz = np.full((tx, tz), np.inf), np.full((tx, tz), -np.inf)
    for pts in samples(V, T[side], STEP):
        ix = np.clip(((pts[:, 0] - lo[0]) / cell).astype(np.int64), 0, nx - 1)
        iz = np.clip(((pts[:, 2] - lo[2]) / cell).astype(np.int64), 0, nz - 1)
        ok = unc[ix, iz]
        if not ok.any():
            continue
        p = pts[ok]
        kx = np.clip(((p[:, 0] - lo[0]) / CHAIN).astype(np.int64), 0, tx - 1); kz = np.clip(((p[:, 2] - lo[2]) / CHAIN).astype(np.int64), 0, tz - 1)
        np.minimum.at(mnx, (kx, kz), p[:, 0]); np.maximum.at(mxx, (kx, kz), p[:, 0])
        np.minimum.at(mnz, (kx, kz), p[:, 2]); np.maximum.at(mxz, (kx, kz), p[:, 2])
    r2 = lambda v: round(float(v), 2)  # noqa: E731
    out = []
    for a, b in zip(*np.nonzero(np.isfinite(mnx))):
        ci_ = np.clip(((np.array([mnx[a, b], mxx[a, b]]) - lo[0]) / cell).astype(np.int64), 0, nx - 1)
        cj_ = np.clip(((np.array([mnz[a, b], mxz[a, b]]) - lo[2]) / cell).astype(np.int64), 0, nz - 1)
        sel = unc[ci_[0]:ci_[1] + 1, cj_[0]:cj_[1] + 1]
        ya = A[ci_[0]:ci_[1] + 1, cj_[0]:cj_[1] + 1][sel]; yb = B[ci_[0]:ci_[1] + 1, cj_[0]:cj_[1] + 1][sel]
        if not len(ya):
            continue
        if max(mxx[a, b] - mnx[a, b], mxz[a, b] - mnz[a, b]) < 0.03:
            continue  # a point where two tiles meet
        out.append({"shape": "box", "min": [r2(mnx[a, b]), r2(ya.min()), r2(mnz[a, b])], "max": [r2(mxx[a, b]), r2(yb.max()), r2(mxz[a, b])]})
    return out[:CHAINMAX]


def measure(pid, path):
    V, T, frond = load(path)
    if not len(V):
        return {"error": "no mesh"}
    lo, hi = V.min(axis=0), V.max(axis=0)
    r2 = lambda v: round(float(v), 2)  # noqa: E731
    res = {"bounds": [[r2(c) for c in lo], [r2(c) for c in hi]], "tris": int(len(T))}
    if max(hi[0] - lo[0], hi[2] - lo[2], hi[1] - lo[1]) > FAR:
        res.update({"class": "far", "volumes": []})
        return res
    elevated = lo[1] >= KEEP
    drop, thin = open_parts(V, T, frond)
    T = T[~drop]  # the bounds above are of the whole piece; the volumes are of what is solid
    cell = 0.25 if max(hi[0] - lo[0], hi[2] - lo[2]) > BIG else 0.1
    nx, nz = int(math.ceil((hi[0] - lo[0]) / cell)) + 1, int(math.ceil((hi[2] - lo[2]) / cell)) + 1
    ny = int(math.ceil((hi[1] - lo[1]) / BIN)) + 2
    y0 = lo[1]  # bins are measured from the lowest point; the volumes are written back in metres from the pivot
    occ = np.zeros((nx, nz, ny), dtype=bool)
    # horizontal faces: where a drawn flat top is (samples of the up-facing triangles, per cell and height bin)
    P0, P1, P2 = V[T[:, 0]], V[T[:, 1]], V[T[:, 2]]
    nrm = np.cross(P1 - P0, P2 - P0); nl = np.linalg.norm(nrm, axis=1)
    up = (nl > 1e-12) & (np.abs(nrm[:, 1]) > 0.97 * nl)  # either winding: the kit frame mirrors the import & (np.ptp(np.stack([P0[:, 1], P1[:, 1], P2[:, 1]]), axis=0) < 0.02)
    topc = {}
    def fill(sub_idx, flat):
        for pts in samples(V, T[sub_idx], STEP):
            ix = np.clip(((pts[:, 0] - lo[0]) / cell).astype(np.int64), 0, nx - 1)
            iz = np.clip(((pts[:, 2] - lo[2]) / cell).astype(np.int64), 0, nz - 1)
            iy = np.clip(np.floor((pts[:, 1] - y0) / BIN + 0.5).astype(np.int64), 0, ny - 1)
            occ[ix, iz, iy] = True
            if flat:
                k = (ix * nz + iz) * ny + iy
                u, c = np.unique(k, return_counts=True)
                for kk, cc in zip(u.tolist(), c.tolist()):
                    topc[kk] = topc.get(kk, 0) + cc
    fill(np.arange(len(T)), False)
    if up.any():
        fill(np.nonzero(up)[0], True)
    need = max(2, int(0.45 * (cell / STEP) ** 2))  # samples of an up-facing triangle that make a cell's top a drawn flat
    P = np.zeros((nx, nz), bool); A = np.zeros((nx, nz)); B = np.zeros((nx, nz)); FL = np.zeros((nx, nz), bool)
    for i, j in zip(*np.nonzero(occ.any(axis=2))):
        runs = [(y0 + a * BIN, y0 + b * BIN) for a, b in solid_runs(np.nonzero(occ[i, j])[0])]
        runs = [(a, b) for a, b in runs if b >= 0.04 and (elevated or a < KEEP)]  # not a floor decal, not overhead
        if not runs:
            continue
        P[i, j] = True; A[i, j] = min(a for a, _ in runs); B[i, j] = max(b for _, b in runs)
        top = int(round((B[i, j] - y0) / BIN))
        FL[i, j] = sum(topc.get((i * nz + j) * ny + tb, 0) for tb in (top - 1, top, top + 1) if 0 <= tb < ny) >= need
    # close small gaps (slats, legs), then fill what the walls close in (the inside of a kiosk or a basin)
    K = 2 if cell < 0.2 else 1
    if K:
        pad = np.pad(P, K); apad, bpad = np.pad(A, K), np.pad(B, K)
        P2, A2, B2 = neighbours(pad, apad, bpad, True, K)
        P3 = pad | eroded(P2, K)
        keep = P3 & ~pad
        Pn = P3
        An = np.where(keep, A2, apad); Bn = np.where(keep, B2, bpad)
        P, A, B = Pn[K:-K, K:-K], An[K:-K, K:-K], Bn[K:-K, K:-K]
        FL = FL & P
    blocked = P & (B >= STEPUP) & (A <= HEAD)
    if blocked.any():
        outside = np.zeros((nx + 2, nz + 2), bool); free = np.pad(~blocked, 1, constant_values=True)
        q = deque([(0, 0)]); outside[0, 0] = True
        while q:
            a, b = q.popleft()
            for di, dj in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                n = (a + di, b + dj)
                if 0 <= n[0] < nx + 2 and 0 <= n[1] < nz + 2 and free[n] and not outside[n]:
                    outside[n] = True; q.append(n)
        inner = ~outside[1:-1, 1:-1] & ~blocked
        for _ in range(64):
            if not inner.any():
                break
            Pm = P & blocked
            Pn, An, Bn = neighbours(Pm, np.where(Pm, A, 0), np.where(Pm, B, 0), True, 1)
            got = inner & Pn & ~Pm
            if not got.any():
                break
            P = P | got; A = np.where(got, An, A); B = np.where(got, Bn, B); blocked = blocked | got; inner = inner & ~got
    # levels: the tops that are drawn flat stay exact, the rest are rounded up to Q; a level's footprint is every column that reaches it
    flat_levels = []
    if FL.any():
        vals = sorted(float(v) for v in B[FL & P])
        grp = [[vals[0]]]
        for v in vals[1:]:
            if v - grp[-1][-1] <= 0.03:
                grp[-1].append(v)
            else:
                grp.append([v])
        flat_levels = [float(np.median(g)) for g in grp if len(g) >= (12 if cell < 0.2 else 3) and np.median(g) <= STAND]  # a flat top to stand on: a real area, not above shoulder height
    out_vols = []
    span = max(hi[0] - lo[0], hi[2] - lo[2])
    cap = MAXBOX[0] if span < 4 else MAXBOX[1] if span < 12 else MAXBOX[2]
    cells_by_top = sorted(zip(*np.nonzero(P)), key=lambda c: -B[c])
    for slack in (1.0, 2.0, 4.0, 1000.0):
        # levels, tallest first: a column joins the lowest level that is above it by no more than the slack (a fraction of the level's height), else it makes a new level at its own top;
        # a drawn flat top is its own level, exact
        levels, L = [], np.zeros((nx, nz))
        for c in cells_by_top:
            b = float(B[c])
            fl = [f for f in flat_levels if abs(b - f) <= 0.04]
            if fl and FL[c]:
                L[c] = fl[0]
                if fl[0] not in levels:
                    levels.append(fl[0])
                continue
            fit = [v for v in levels if v >= b - 1e-9 and v - b <= slack * max(0.2, 0.25 * v)]
            if fit:
                L[c] = min(fit)
            else:
                levels.append(b); L[c] = b
        vols = []
        for lv in sorted(set(round(float(v), 4) for v in L[P])):
            reach = P & (L >= lv - 1e-4)
            own = P & (np.abs(L - lv) < 1e-4)

            def emit(comp, circ, mask):
                ii, jj = [c[0] for c in comp], [c[1] for c in comp]
                i0, i1, j0, j1 = min(ii), max(ii), min(jj), max(jj)
                ya = min(A[c] for c in comp)
                if lv - ya < 0.02:
                    return  # every column here hangs above this level and has its own volume higher up: nothing stands at this height
                fill = len(comp) / ((i1 - i0 + 1) * (j1 - j0 + 1))
                if circ or fill >= 0.72:
                    parts = [(i0, j0, i1, j1)]  # a round or nearly full footprint is one volume
                else:  # the larger rectangles of a ragged one: slivers under 3 % of it are dropped (a curve is a few strips, not a staircase)
                    rs = sorted(rects(set(comp)), key=lambda r: -((r[2] - r[0] + 1) * (r[3] - r[1] + 1)))
                    parts = [r for r in rs if (r[2] - r[0] + 1) * (r[3] - r[1] + 1) >= 0.03 * len(comp)][:12] or rs[:1]
                for (a0, b0, a1, b1) in parts:
                    mine = [(a, b) for a in range(a0, a1 + 1) for b in range(b0, b1 + 1) if own[a, b]]
                    st = bool(mine) and sum(1 for c in mine if FL[c]) / len(mine) >= FLAT
                    if circ:
                        cx = lo[0] + (i0 + i1 + 1) * cell / 2; cz = lo[2] + (j0 + j1 + 1) * cell / 2
                        v = {"shape": "cylinder", "c": [r2(cx), r2(cz)], "r": r2(math.sqrt(len(comp) * cell * cell / math.pi)), "y0": r2(ya), "y1": r2(lv)}
                    else:
                        mn = [lo[0] + a0 * cell, ya, lo[2] + b0 * cell]
                        mx = [min(lo[0] + (a1 + 1) * cell, hi[0]), lv, min(lo[2] + (b1 + 1) * cell, hi[2])]
                        v = {"shape": "box", "min": [r2(c) for c in mn], "max": [r2(c) for c in mx]}
                    if st:
                        v["stand"] = True
                    if v["shape"] == "box" and v["max"][1] - v["min"][1] < 0.1 and v["max"][1] < STEPUP and not st:
                        continue  # a trim or a lip, not a solid
                    vols.append(v)
            for comp in components(reach):  # a level's footprint is every column that reaches it, so a tall part stands on the lower volume it grows from
                ii, jj = [c[0] for c in comp], [c[1] for c in comp]
                w, h = (max(ii) - min(ii) + 1) * cell, (max(jj) - min(jj) + 1) * cell
                circ = len(comp) >= 9 and abs(w - h) <= 0.15 * max(w, h) and 0.70 <= len(comp) / ((max(ii) - min(ii) + 1) * (max(jj) - min(jj) + 1)) <= 0.86
                if circ:  # a disc has empty corners: a square with a hole in it is not one
                    cs = set(comp); ni, nj = max(ii) - min(ii) + 1, max(jj) - min(jj) + 1; ki, kj = max(1, ni // 5), max(1, nj // 5)
                    corner = [(min(ii) + di, min(jj) + dj) for di in range(ki) for dj in range(kj)] + [(max(ii) - di, min(jj) + dj) for di in range(ki) for dj in range(kj)] + \
                             [(min(ii) + di, max(jj) - dj) for di in range(ki) for dj in range(kj)] + [(max(ii) - di, max(jj) - dj) for di in range(ki) for dj in range(kj)]
                    circ = sum(1 for c in corner if c in cs) / len(corner) < 0.35
                emit(comp, circ, reach)
        out_vols = joined(vols)
        if DEBUG: print('DEBUG', pid, 'slack', slack, 'vols', len(out_vols), 'cap', cap, file=sys.stderr)
        if len(out_vols) <= cap:
            break
    out_vols = out_vols[:cap * 2]
    chain = wall_chain(V, T, blocked, A, B, lo, hi, cell, nx, nz, out_vols) if pid in CHAINED else []
    out_vols += chain
    res["class"] = "elevated" if elevated else ("flat" if not out_vols else "solid")
    res["cell"] = cell
    res["volumes"] = out_vols
    res["thin"] = thin
    if chain:
        res["chain"] = len(chain)
    return res


out = {}
for pid, path in JOBS:
    try:
        out[pid] = measure(pid, path)
    except Exception as e:  # a piece that will not measure is reported, not fatal
        out[pid] = {"error": f"{type(e).__name__}: {e}"[:160]}
OUT.write_text(json.dumps(out, separators=(",", ":"), sort_keys=True))
print(f"collide: {len(out)} pieces, {sum(len(v.get('volumes', [])) for v in out.values())} volumes")
