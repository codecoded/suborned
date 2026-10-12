"""Streamline stroke font (pure Python: shared by signage.py's neon letters and signage_art.py's letter boards).

Each capital is a list of polylines in cap-height units (x right, y up, baseline 0, cap 1); closed loops start
mid-edge so offset outlines never seam on a corner. `layout` sets a word on a line or stacked in a column.
"""

G = {
    "A": [[(0, 0), (0, 1), (0.6, 1), (0.6, 0)], [(0, 0.48), (0.6, 0.48)]],
    "B": [[(0, 0), (0, 1), (0.46, 1), (0.54, 0.76), (0.46, 0.53), (0, 0.53)],
          [(0.46, 0.53), (0.6, 0.28), (0.52, 0), (0, 0)]],
    "C": [[(0.6, 1), (0, 1), (0, 0), (0.6, 0)]],
    "D": [[(0, 0), (0, 1), (0.36, 1), (0.6, 0.72), (0.6, 0.28), (0.36, 0), (0, 0)]],
    "E": [[(0.58, 1), (0, 1), (0, 0), (0.58, 0)], [(0, 0.5), (0.44, 0.5)]],
    "F": [[(0.58, 1), (0, 1), (0, 0)], [(0, 0.5), (0.44, 0.5)]],
    "G": [[(0.6, 1), (0, 1), (0, 0), (0.6, 0), (0.6, 0.46), (0.3, 0.46)]],
    "H": [[(0, 0), (0, 1)], [(0.6, 0), (0.6, 1)], [(0, 0.5), (0.6, 0.5)]],
    "I": [[(0, 0), (0, 1)]],
    "J": [[(0.56, 1), (0.56, 0), (0, 0), (0, 0.3)]],
    "K": [[(0, 0), (0, 1)], [(0.58, 1), (0.04, 0.48), (0.6, 0)]],
    "L": [[(0, 1), (0, 0), (0.54, 0)]],
    "M": [[(0, 0), (0, 1), (0.36, 0.56), (0.72, 1), (0.72, 0)]],
    "N": [[(0, 0), (0, 1), (0.6, 0), (0.6, 1)]],
    "O": [[(0.3, 0), (0.6, 0), (0.6, 1), (0, 1), (0, 0), (0.3, 0)]],
    "P": [[(0, 0), (0, 1), (0.6, 1), (0.6, 0.48), (0, 0.48)]],
    "R": [[(0, 0), (0, 1), (0.6, 1), (0.6, 0.5), (0, 0.5)], [(0.3, 0.5), (0.6, 0)]],
    "S": [[(0.6, 1), (0, 1), (0, 0.5), (0.6, 0.5), (0.6, 0), (0, 0)]],
    "T": [[(0, 1), (0.64, 1)], [(0.32, 1), (0.32, 0)]],
    "U": [[(0, 1), (0, 0), (0.6, 0), (0.6, 1)]],
    "V": [[(0, 1), (0.3, 0), (0.6, 1)]],
    "W": [[(0, 1), (0.16, 0), (0.36, 0.62), (0.56, 0), (0.72, 1)]],
    "X": [[(0, 0), (0.6, 1)], [(0, 1), (0.6, 0)]],
    "Y": [[(0, 1), (0.3, 0.52), (0.6, 1)], [(0.3, 0.52), (0.3, 0)]],
    "Z": [[(0, 1), (0.6, 1), (0, 0), (0.6, 0)]],
    "2": [[(0, 1), (0.6, 1), (0.6, 0.5), (0, 0.5), (0, 0), (0.6, 0)]],
    "&": [[(0.6, 0), (0.02, 0.62), (0.02, 1), (0.42, 1), (0.42, 0.62), (0, 0.3), (0, 0), (0.42, 0), (0.6, 0.22)]],
    "+": [[(0, 0.5), (0.5, 0.5)], [(0.25, 0.25), (0.25, 0.75)]],
    "-": [[(0, 0.5), (0.4, 0.5)]],
    ".": [[(0, 0), (0, 0.08)]],
    " ": [],
}


def rounded(stroke, r, n=4):
    """Quadratic-Bezier fillet (radius ~r) on every interior corner; closed loops start mid-edge (no seam corner)."""
    P = list(stroke)
    out = [P[0]]
    for a, p, b in zip(P, P[1:], P[2:]):
        d0, d1 = (p[0] - a[0], p[1] - a[1]), (b[0] - p[0], b[1] - p[1])
        l0, l1 = (d0[0] ** 2 + d0[1] ** 2) ** 0.5, (d1[0] ** 2 + d1[1] ** 2) ** 0.5
        t = min(r, l0 / 2, l1 / 2)
        A = (p[0] - d0[0] / l0 * t, p[1] - d0[1] / l0 * t)
        C = (p[0] + d1[0] / l1 * t, p[1] + d1[1] / l1 * t)
        out += [((1 - s) ** 2 * A[0] + 2 * (1 - s) * s * p[0] + s * s * C[0],
                 (1 - s) ** 2 * A[1] + 2 * (1 - s) * s * p[1] + s * s * C[1]) for s in (k / n for k in range(n + 1))]
    out = out + [P[-1]] if len(P) > 1 else out
    return [p for i, p in enumerate(out) if not i or abs(p[0] - out[i - 1][0]) + abs(p[1] - out[i - 1][1]) > 1e-6]


def width(ch):
    return max((x for st in G[ch] for x, _ in st), default=0.3)


def closed(stroke):
    return stroke[0] == stroke[-1]


def layout(word, gap=0.3, stack=False, lead=1.35):
    """[(strokes, (ox, oy))] for a word: on one line from x = 0 (stack=False) or centred in a column, top letter
    first, each lead below the last (stack=True). Returns (placed, extent) with extent = (w, h) in cap units."""
    placed, x = [], 0.0
    if stack:
        wmax = max(width(c) for c in word)
        for i, ch in enumerate(word):
            placed.append((G[ch], ((wmax - width(ch)) / 2, -i * lead)))
        return placed, (wmax, lead * (len(word) - 1) + 1)
    for ch in word:
        placed.append((G[ch], (x, 0.0)))
        x += width(ch) + gap
    return placed, (x - gap, 1.0)
