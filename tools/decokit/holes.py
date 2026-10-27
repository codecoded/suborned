"""Holed wall slab for the hand-built far levels (brief 072-G item 4, Vela; Sky's "ok" to window holes in the low-detail shadow copies; Redline phase 2 F1).

The every-face modules cast their shadow from their L2 (Latch's ShadowProxies: a module's proxy is its L2 mesh, shadows only). A solid L2 slab stands in
front of every L0 window pocket, so every sunlit window read near-black. This slab keeps the L2 a plain box but opens it where L0 cuts a pocket: the front
face is split round the openings, each opening gets its four reveals back to the pocket depth, and the pocket is closed there by the window film quad (the L2's
own window, moved from the face into the pocket). Light through the opening then reaches L0's film (which stands in front of the L2 pocket back), and the
head and one jamb shade a strip, as L0's own reveals do.

The front face is cut by columns: an x span with no opening is one tall quad, an x span through openings is split at their heads and sills. No boolean, so
the far level stays a few quads per window (about 10 triangles: 8 of reveals, 2 of film, plus the split)."""


def _quad(s, pts, mat, normal):
    """One quad, wound so its normal points along `normal` (an axis vector)."""
    (ax, ay, az), (bx, by, bz), (cx, cy, cz) = pts[0], pts[1], pts[2]
    u, v = (bx - ax, by - ay, bz - az), (cx - ax, cy - ay, cz - az)
    n = (u[1] * v[2] - u[2] * v[1], u[2] * v[0] - u[0] * v[2], u[0] * v[1] - u[1] * v[0])
    if sum(a * b for a, b in zip(n, normal)) < 0:
        pts = pts[::-1]
    s.face(pts, mat)


def holed_slab(s, x0, x1, z0, z1, y0, y1, holes, body, depth):
    """Box x0..x1, y0 (front, the render face; the kit's front is -Y) .. y1 (back), z0..z1, open at `holes`: (xa, za, xb, zb, film, lining) rectangles
    that do not overlap, each a pocket from y0 back to `depth` (< y1) closed by a quad of `film`, its reveals in `lining` (None = body)."""
    for xa, za, xb, zb, *_ in holes:
        assert x0 <= xa < xb <= x1 and z0 <= za < zb <= z1, (xa, za, xb, zb)
    xs = sorted({x0, x1} | {h[0] for h in holes} | {h[2] for h in holes})
    for a, b in zip(xs, xs[1:]):
        cut = sorted((h[1], h[3]) for h in holes if h[0] <= a and h[2] >= b)
        zs = [z0] + [v for c in cut for v in c] + [z1]
        for za, zb in zip(zs[::2], zs[1::2]):
            if zb - za > 1e-6:
                _quad(s, [(a, y0, za), (b, y0, za), (b, y0, zb), (a, y0, zb)], body, (0, -1, 0))
    _quad(s, [(x0, y1, z0), (x1, y1, z0), (x1, y1, z1), (x0, y1, z1)], body, (0, 1, 0))
    _quad(s, [(x0, y0, z0), (x0, y1, z0), (x0, y1, z1), (x0, y0, z1)], body, (-1, 0, 0))
    _quad(s, [(x1, y0, z0), (x1, y1, z0), (x1, y1, z1), (x1, y0, z1)], body, (1, 0, 0))
    _quad(s, [(x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0)], body, (0, 0, -1))
    _quad(s, [(x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)], body, (0, 0, 1))
    for xa, za, xb, zb, film, lining in holes:
        m = lining or body
        _quad(s, [(xa, y0, za), (xa, depth, za), (xa, depth, zb), (xa, y0, zb)], m, (1, 0, 0))     # jambs face into the opening
        _quad(s, [(xb, y0, za), (xb, depth, za), (xb, depth, zb), (xb, y0, zb)], m, (-1, 0, 0))
        _quad(s, [(xa, y0, zb), (xb, y0, zb), (xb, depth, zb), (xa, depth, zb)], m, (0, 0, -1))   # head faces down
        _quad(s, [(xa, y0, za), (xb, y0, za), (xb, depth, za), (xa, depth, za)], m, (0, 0, 1))    # sill faces up
        _quad(s, [(xa, depth, za), (xb, depth, za), (xb, depth, zb), (xa, depth, zb)], film, (0, -1, 0))
