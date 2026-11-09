"""Vantage, brief 101 step 2: no head of the square's lamp mast sees the cell's starting stance or the waits by the car (the proof Aster asked for before the commit).
Run: python3 briefs/101/vantage/proof/proof.py (its output on the committed data: proof.txt beside it). numpy only.

The mast: sim/data/handover.takedown.json's mast-camera (the world's export after `bun run sim:site`), or --at x,y --yaw deg (plan) --step deg.
The site: sim/data/handover.site.json (walk grid, floors, occluders).
Cone: Geo.InCone exactly (integer maths, the facing scaled to 1024, C# truncating division; Site.cs: Half = reach * TanHalf[35] / 10000).
Line: sight.py's port of Game.SeesFrom / Site.Sight3 (lens = floor + 768; head = floor + 160 or chest = floor + 120; either clear is enough). No crowd.
Samples, block frame, integer cm (x south, y east):
  stance  every point of a 5 cm lattice within 2 m of each of the four marks (mission.json crew; GDD 07 'the cell's loose stance within 2 m'), free or not
  car     every point of a 10 cm lattice over x -2..18, y -53..-44 m: the start zone crew-line-getaway (the bay and its canopy) and the holds behind the car
  spots   the waits and holds the reference plans stand at (Vantage's proposal, section 2)
Prints: what each head sees of each set (must be none), the angle by which each set stands outside the nearest cone edge, and how far the mast turns either way
before any head sees any of it (the engine's rules, walls included)."""
import json, math, os, sys
import numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from sight import R, clear_lines, floor_at, LENS, EYE, CHEST, FREE, X0, Y0, CELL, NX, NY

REACH, HALF = 3000, 3000 * 7002 // 10000
HALF_DEG = math.degrees(math.atan2(HALF, REACH))


def tdiv(a, b):
    q = abs(a) // abs(b)
    return q if (a >= 0) == (b >= 0) else -q


def unit1024(f):
    flen = math.isqrt(f[0] * f[0] + f[1] * f[1])
    return tdiv(f[0] * 1024, flen), tdiv(f[1] * 1024, flen)


def in_cone(o, f, P):
    """Geo.InCone(o, f, REACH, HALF, near 0) for integer points P (n, 2) (int64 safe: |r| < 1e5, |f| = 1024)."""
    fx, fy = unit1024(f)
    rx = P[:, 0].astype(np.int64) - o[0]; ry = P[:, 1].astype(np.int64) - o[1]
    dot = rx * fx + ry * fy; ff = fx * fx + fy * fy
    cross = np.abs(rx * fy - ry * fx)
    return (dot >= 0) & (dot * dot <= REACH * REACH * ff) & (cross * REACH <= dot * HALF)


def off_edge_deg(o, fs, P):
    """Per point: degrees outside the nearest head's cone edge (negative = inside), the head index."""
    best = np.full(len(P), 1e9); which = np.zeros(len(P), int)
    for k, f in enumerate(fs):
        fx, fy = unit1024(f)
        rx = P[:, 0] - o[0]; ry = P[:, 1] - o[1]
        ang = np.degrees(np.arctan2(np.abs(rx * fy - ry * fx), rx * fx + ry * fy)) - HALF_DEG
        ang[np.hypot(rx, ry) > REACH] = 1e9  # beyond reach whichever way it turns: no edge to measure
        m = ang < best; best[m] = ang[m]; which[m] = k
    return best, which


def seen(o, fs, P):
    P = np.asarray(P, np.int64); cone = np.zeros(len(P), bool)
    for f in fs: cone |= in_cone(o, f, P)
    out = np.zeros(len(P), bool); idx = np.nonzero(cone)[0]
    if len(idx):
        za = floor_at(*o) + LENS; Q = P[idx].astype(float)
        fl = np.array([floor_at(int(x), int(y)) for x, y in P[idx]])
        out[idx] = clear_lines(o, za, Q, fl + EYE) | clear_lines(o, za, Q, fl + CHEST)
    return out


def facings_of(yaw_deg, heads, step):
    """tools/sim/takedown.ts dir(): a plan heading → block heading per mille."""
    out = []
    for k in range(heads):
        a = math.radians(yaw_deg + k * step); out.append((round(-math.sin(a) * 1000), round(math.cos(a) * 1000)))
    return out


def compass(f): return (math.degrees(math.atan2(f[1], -f[0])) + 360) % 360


def lattice(c, r, step):
    out = []
    for dx in range(-r, r + 1, step):
        for dy in range(-r, r + 1, step):
            if dx * dx + dy * dy <= r * r: out.append((c[0] + dx, c[1] + dy))
    return np.array(out, np.int64)


def rect(x0, x1, y0, y1, step):
    xs = np.arange(x0, x1 + 1, step); ys = np.arange(y0, y1 + 1, step); X, Y = np.meshgrid(xs, ys)
    return np.stack([X.ravel(), Y.ravel()], 1).astype(np.int64)


def free(P):
    i = (P[:, 0] - X0) // CELL; j = (P[:, 1] - Y0) // CELL; ok = (i >= 0) & (i < NX) & (j >= 0) & (j < NY)
    m = np.zeros(len(P), bool); m[ok] = FREE[i[ok], j[ok]]; return m


def main(argv):
    arg = dict(zip(argv[0::2], argv[1::2]))
    if '--at' in arg:
        at = tuple(int(round(float(v) * 100)) for v in arg['--at'].split(','))
        fs = facings_of(float(arg['--yaw']), 3, float(arg.get('--step', '60'))); src = 'command line'
    else:
        t = json.load(open(R + 'sim/data/handover.takedown.json'))
        w = next(x for x in t['watchers'] if x['kind'] == 'mast-camera'); at = tuple(w['at']); fs = [tuple(f) for f in w['facings']]
        src = f"sim/data/handover.takedown.json {w['id']}"
    m = json.load(open(R + 'contracts/missions/handover/mission.json'))
    marks = {c['name']: (round(c['at'][0] * 100), round(c['at'][1] * 100)) for c in m['crew']}
    sets = {f'stance {k}': lattice(v, 200, 5) for k, v in marks.items()}
    sets['car area'] = rect(-200, 1800, -5300, -4400, 10)
    spots = {'wait (7.25,-50.25)': (725, -5025), 'wait (7.25,-46.25)': (725, -4625), 'wait (6.75,-46.25)': (675, -4625), 'wait (10.25,-46.25)': (1025, -4625),
             'wait (8.5,-48.2)': (850, -4820), 'hold (12.5,-52.5)': (1250, -5250), 'hold (17.5,-52.5)': (1750, -5250), 'hold (15,-52)': (1500, -5200)}
    sets['car spots'] = np.array(list(spots.values()), np.int64)
    print(f'mast {src}: at {at} cm, lens {floor_at(*at) + LENS} cm; heads ' + ', '.join(f'{f} (compass {compass(f):.1f})' for f in fs) + f'; cone ±{HALF_DEG:.3f}, reach {REACH} cm')
    print(f'  watched: compass {(compass(fs[-1]) - HALF_DEG) % 360:.1f} round through north to {(compass(fs[0]) + HALF_DEG) % 360:.1f}; behind the lenses: the rest')
    worst = (1e9, None)
    for name, P in sets.items():
        s = seen(at, fs, P); off, k = off_edge_deg(at, fs, P.astype(float)); i = int(np.argmin(off))
        fr = free(P)
        far = np.hypot(P[:, 0] - at[0], P[:, 1] - at[1]).max() / 100
        edge = f'nearest cone edge {off[i]:.2f} deg outside (head {k[i]}, point ({P[i][0] / 100:.2f}, {P[i][1] / 100:.2f}) m, {math.hypot(P[i][0] - at[0], P[i][1] - at[1]) / 100:.2f} m off)' if off[i] < 1e8 else 'all beyond reach'
        print(f'  {name:16s} {len(P):6d} points ({int(fr.sum())} on free cells), up to {far:.1f} m off: seen {int(s.sum())}; {edge}')
        if off[i] < worst[0]: worst = (off[i], name)
    print(f'  margin: {worst[0]:.2f} deg ({worst[1]})')
    allP = np.concatenate(list(sets.values()))
    yaw0 = math.degrees(math.atan2(-fs[0][0], fs[0][1]))  # the first head's plan angle
    step = (math.degrees(math.atan2(-fs[1][0], fs[1][1])) - yaw0) % 360
    for sign, label in ((1, 'counterclockwise'), (-1, 'clockwise')):
        d = 0.0
        while d < 60:
            d += 0.05
            if seen(at, facings_of(yaw0 + sign * d, len(fs), step), allP).any(): break
        print(f'  turned {label}: ' + (f'first seen at {d:.2f} deg' if d < 60 else 'nothing seen within 60 deg') + " (the engine's rules, walls included)")


if __name__ == '__main__':
    main(sys.argv[1:])
