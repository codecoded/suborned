"""Vantage, brief 101: what a camera sees, by the engine's own rules re-done in numpy (ported for the square-camera proposal, briefs/101/vantage/square-camera.md section 6).
Cone: GameSight InHeads / Geo.InCone (a wedge: ahead <= reach, side <= ahead*half/reach, near 0), every head at once (proof.py has the exact integer form).
Line: Game.SeesFrom (lens = floor + mount-mast-camera-cm 768; target head = floor + 160 or chest = floor + 120; either clear is enough),
Site.Sight3 over sim/data/handover.site.json occluders (prisms z..z+h), the camera's own piece skipped. No crowd (the tests' crowd=false).
Block frame, integer cm: x south, y east. The repo root is four folders up (SUBORNED_ROOT overrides it).
`python3 sight.py` checks the port against Keel's N67 facts for the mast as it stood before brief 101 (block (-18.5, -15.5), heads E, NNW, SSW):
it sees Plaza east and East side, not the cafe door (the wall) nor the Office entrance (beyond 30 m)."""
import json, math
import numpy as np

import os
R = os.environ.get('SUBORNED_ROOT', os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '../../../..')) + '/')
S = json.load(open(R + 'sim/data/handover.site.json'))
G = S['grid']; X0, Y0, CELL, NX, NY = G['x0'], G['y0'], G['cell'], G['nx'], G['ny']
FREE = np.array([[c == '.' for c in row] for row in G['rows']], bool)  # [i = x index (south), j = y index (east)]
FLOOR = np.zeros((NX, NY), np.int32)
for i, row in enumerate(G['floor']):
    j = 0
    for v, n in zip(row[0::2], row[1::2]): FLOOR[i, j:j + n] = v; j += n
OCC = [(o['id'], o['z'], o['h'], np.array(o['poly'], float)) for o in S['occluders']]
LENS, EYE, CHEST, REACH = 768, 160, 120, 3000
HALF = REACH * 7002 // 10000  # TanHalf[35] as Site.cs: 2100


def cxy(i, j): return X0 + (i + 0.5) * CELL, Y0 + (j + 0.5) * CELL


def floor_at(x, y):
    i, j = int((x - X0) // CELL), int((y - Y0) // CELL)
    return int(FLOOR[i, j]) if 0 <= i < NX and 0 <= j < NY else 0


def facings(yaw_deg, heads=3, step=120):
    """The generator's heads: plan angle yaw + k*step (plan x east, y north) → block vectors (x south, y east), ×1000 as the sim data."""
    out = []
    for k in range(heads):
        a = math.radians(yaw_deg + k * step); px, py = math.cos(a), math.sin(a)
        out.append((round(-py * 1000), round(px * 1000)))
    return out


def in_heads(at, fs, P):
    """P: (n,2) block cm. True where any head's wedge holds the point (Geo.InCone, near 0)."""
    rx, ry = P[:, 0] - at[0], P[:, 1] - at[1]; ok = np.zeros(len(P), bool)
    for fx, fy in fs:
        L = math.hypot(fx, fy); ux, uy = fx / L, fy / L
        ahead = rx * ux + ry * uy; side = np.abs(rx * uy - ry * ux)
        ok |= (ahead >= 0) & (ahead <= REACH) & (side * REACH <= ahead * HALF)
    return ok


def _seg_hits_poly(ax, ay, bx, by, poly):
    """Vectorised: does each segment (a→b) cross the polygon's edges or lie inside it."""
    n = len(ax); hit = np.zeros(n, bool)
    px, py = poly[:, 0], poly[:, 1]; qx, qy = np.roll(px, -1), np.roll(py, -1)
    for k in range(len(px)):
        # segment-segment intersection, inclusive
        d1x, d1y = bx - ax, by - ay; d2x, d2y = qx[k] - px[k], qy[k] - py[k]
        den = d1x * d2y - d1y * d2x
        with np.errstate(divide='ignore', invalid='ignore'):
            t = ((px[k] - ax) * d2y - (py[k] - ay) * d2x) / den
            u = ((px[k] - ax) * d1y - (py[k] - ay) * d1x) / den
        hit |= (den != 0) & (t >= 0) & (t <= 1) & (u >= 0) & (u <= 1)
    # a inside (ray cast)
    inside = np.zeros(n, bool)
    for k in range(len(px)):
        c = ((py[k] > ay) != (qy[k] > ay)) & (ax < (qx[k] - px[k]) * (ay - py[k]) / np.where(qy[k] - py[k] == 0, 1e-9, qy[k] - py[k]) + px[k])
        inside ^= c
    return hit | inside


def _contains(poly, x, y):
    px, py = poly[:, 0], poly[:, 1]; qx, qy = np.roll(px, -1), np.roll(py, -1); c = False
    for k in range(len(px)):
        if (py[k] > y) != (qy[k] > y) and x < (qx[k] - px[k]) * (y - py[k]) / (qy[k] - py[k]) + px[k]: c = not c
    return c


def clear_lines(at, za, P, zb):
    """Site.Sight3 for many targets at heights zb (array): True = no occluder prism blocks the line."""
    ok = np.ones(len(P), bool); ax0, ay0 = float(at[0]), float(at[1])
    for oid, z, h, poly in OCC:
        if _contains(poly, ax0, ay0): continue  # own: the camera's own piece never blocks it
        top = z + h
        lo = np.minimum(za, zb); hi = np.maximum(za, zb)
        cand = ok & ~((hi < z) | (lo > top))
        x0, y0, x1, y1 = poly[:, 0].min(), poly[:, 1].min(), poly[:, 0].max(), poly[:, 1].max()
        cand &= ~((np.maximum(ax0, P[:, 0]) < x0) | (np.minimum(ax0, P[:, 0]) > x1) | (np.maximum(ay0, P[:, 1]) < y0) | (np.minimum(ay0, P[:, 1]) > y1))
        if not cand.any(): continue
        idx = np.nonzero(cand)[0]; bz = zb[idx].astype(float); D = np.abs(bz - za)
        up = bz > za
        tl = np.where(up, z - za, za - top).astype(float); th = np.where(up, top - za, za - z).astype(float)
        flat = D == 0
        tl = np.clip(tl, 0, None); th = np.minimum(th, D)
        keep = flat | (tl <= th)
        Dn = np.where(flat, 1.0, D); tl = np.where(flat, 0.0, tl); th = np.where(flat, 1.0, th)
        bx, by = P[idx, 0], P[idx, 1]
        Ax = ax0 + (bx - ax0) * tl / Dn; Ay = ay0 + (by - ay0) * tl / Dn
        Bx = ax0 + (bx - ax0) * th / Dn; By = ay0 + (by - ay0) * th / Dn
        hit = keep & _seg_hits_poly(Ax, Ay, Bx, By, poly)
        ok[idx[hit]] = False
    return ok


def sees(at, fs, P, mount=LENS):
    """CameraWatching(crowd: false) for points P (block cm): in a head's wedge and a clear line to the head or the chest."""
    P = np.asarray(P, float); za = floor_at(*at) + mount
    fl = np.array([floor_at(x, y) for x, y in P]); cone = in_heads(at, fs, P)
    out = np.zeros(len(P), bool); idx = np.nonzero(cone)[0]
    if len(idx):
        Q = P[idx]; head = clear_lines(at, za, Q, fl[idx] + EYE)
        chest = clear_lines(at, za, Q, fl[idx] + CHEST)
        out[idx] = head | chest
    return out


def free_cells(at, r=REACH + 100):
    ii, jj = np.nonzero(FREE); xs = X0 + (ii + 0.5) * CELL; ys = Y0 + (jj + 0.5) * CELL
    m = (np.abs(xs - at[0]) <= r) & (np.abs(ys - at[1]) <= r)
    return np.stack([xs[m], ys[m]], 1)


PLACES = S['places']


def stand(p):
    """The engine's stand (Site.cs): a place's own stand, else the point itself on a free cell, else the nearest free cell centre."""
    if isinstance(p, str): pl = PLACES[p]; return tuple(pl.get('stand') or pl['at']) if 'stand' in pl else stand(tuple(pl['at']))
    x, y = p; i, j = int((x - X0) // CELL), int((y - Y0) // CELL)
    if 0 <= i < NX and 0 <= j < NY and FREE[i, j]: return (x, y)
    best = None
    for r in range(0, 20):
        for di in range(-r, r + 1):
            for dj in range(-r, r + 1):
                a, b = i + di, j + dj
                if 0 <= a < NX and 0 <= b < NY and FREE[a, b]:
                    cx, cy = cxy(a, b); d = (cx - x) ** 2 + (cy - y) ** 2
                    if best is None or d < best[0]: best = (d, (cx, cy))
        if best: return best[1]
    return p


if __name__ == '__main__':
    import sys
    at = (-1850, -1550); fs = [(0, 1000), (-866, -500), (866, -500)]
    assert facings(0) == fs, facings(0)
    m = json.load(open(R + 'contracts/missions/handover/mission.json')); D = {d['name']: d['at'] for d in m['destinations']}
    for name in ['Plaza east', 'East side', 'Café door', 'Office entrance', 'Courtyard', 'Under parasol', 'Between tables', 'Fountain', 'Plaza west']:
        p = stand((D[name][0] * 100, D[name][1] * 100)); print(f'{name:16s} stand {p} seen={bool(sees(at, fs, [p])[0])}')


def all_cells(at, r=REACH + 100):
    """Every grid cell centre near the mast, walkable or not (for drawing: the cone lies on the ground under props too)."""
    ii, jj = np.mgrid[0:NX, 0:NY]; xs = (X0 + (ii + 0.5) * CELL).ravel(); ys = (Y0 + (jj + 0.5) * CELL).ravel()
    m = (np.abs(xs - at[0]) <= r) & (np.abs(ys - at[1]) <= r)
    return np.stack([xs[m], ys[m]], 1)
