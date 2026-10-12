"""Plaza family helpers (used by plaza.py): the shared facade elevation, glazing, comet banner, relief globe,
armillary sphere, parasol canopy and shrub cards.

Facade frame: modules face -Y with the facade plane at y = 0. Courses are swept along a plan path that keeps
the building on its left (structures.tube's a-axis points inward, b = z), so a straight bay and the curved
corner share one elevation profile and meet flush."""

import math

from mathutils import Vector

from geo import circle, fillet, ring
from mats import pal
from structures import along, sec, tube

Z_PLINTH, Z_SILL, Z_HEAD, Z_ROOF = 0.7, 1.3, 8.3, 10.3  # base course top, glazing sill / head, roof deck
Z_SOIL = 10.92  # parapet trough soil (lip at 11.1)
TROUGH = fillet([(-0.3, 10.25), (1.0, 10.25), (1.0, 11.0), (0.85, 11.0), (0.85, 10.7), (0.07, 10.7), (0.07, 11.1),
                 (-0.3, 11.1)], 0.18, n=4)  # rounded parapet with a planting trough in its top


# ----------------------------------------------------------------- mesh utilities

def uvmesh(s, verts, faces, uvs, mat, smooth=True):
    """Faces with explicit per-vertex UVs (kept by Shape.build like decals)."""
    bv = s._add(verts, faces, mat, smooth)
    idx = {v: i for i, v in enumerate(bv)}
    for f in {f for v in bv for f in v.link_faces}:
        f[s._uvkeep] = 1
        s._pending_uv.append((f, [uvs[idx[lp.vert]] for lp in f.loops]))


def uvface(s, pts, mat, uv):
    uvmesh(s, pts, [list(range(len(pts)))], uv, mat, smooth=False)


def offset(path, a, closed=False):
    """2D plan path moved by a along tube's inward side (left of travel), per-point averaged tangent."""
    P = [Vector((x, y, 0)) for x, y in path]
    n, out = len(P), []
    for i, p in enumerate(P):
        a0, b0 = (P[i - 1], P[(i + 1) % n]) if closed else (P[max(i - 1, 0)], P[min(i + 1, n - 1)])
        q = p + Vector((0, 0, 1)).cross((b0 - a0).normalized()).normalized() * a
        out.append((q.x, q.y))
    return out


def lift(path, z=0.0):
    return [(x, y, z) for x, y in path]


def ribbon(s, path, z0, z1, mat, closed=False):
    """Single-sided vertical strip along a plan path, facing outward (right of travel): glazing, interiors."""
    n = len(path)
    verts = lift(path, z0) + lift(path, z1)
    faces = [[i, (i + 1) % n, n + (i + 1) % n, n + i] for i in range(n if closed else n - 1)]
    s._add(verts, faces, mat, True)


def bead(s, path, a, b, r, mat, closed=False):
    tube(s, lift(path), circle(r, 10, a, b), mat, closed=closed)


def ball(s, c, r, mat, seg=24):
    with s.at(c):
        s.lathe([(r * math.sin(math.pi * k / 10), -r * math.cos(math.pi * k / 10)) for k in range(11)], mat, seg=seg)


# ----------------------------------------------------------------- facade elevation

def courses(s, path, closed=False):
    """Base course, sill string course, stacked teal bands with gold lines, parapet trough with soil."""
    tube(s, lift(path), sec(-0.14, 0.4, 0.0, Z_PLINTH, 0.07), pal.Limestone, closed=closed)
    tube(s, lift(path), sec(-0.1, 0.4, Z_SILL - 0.13, Z_SILL + 0.03, 0.05), pal.Limestone, closed=closed)
    tube(s, lift(path), sec(-0.13, 0.3, 8.9, 9.5, 0.05), pal.Enamel_Teal, closed=closed)
    tube(s, lift(path), sec(-0.1, 0.3, 9.72, 9.98, 0.04), pal.Enamel_Seafoam, closed=closed)
    for a, b in ((-0.12, 8.92), (-0.12, 9.48), (-0.03, 9.61)):
        bead(s, path, a, b, 0.028, pal.Gold, closed)
    tube(s, lift(path), TROUGH, pal.Limestone, closed=closed)
    tube(s, lift(path), sec(0.06, 0.86, 10.72, Z_SOIL), pal.Soil, closed=closed)


def glazing(s, x0, x1, y, z0, z1, nv=3, dz=1.4):
    """Tall window: dark glass film, gold frame, chrome mullions and transoms (facing -Y at depth y)."""
    s.face([(x0, y, z0), (x1, y, z0), (x1, y, z1), (x0, y, z1)], pal.GlassDark)
    for a, b in (((x0, y - 0.05, z0), (x1, y + 0.03, z0 + 0.09)), ((x0, y - 0.05, z1 - 0.09), (x1, y + 0.03, z1)),
                 ((x0, y - 0.05, z0), (x0 + 0.09, y + 0.03, z1)), ((x1 - 0.09, y - 0.05, z0), (x1, y + 0.03, z1))):
        s.box(a, b, pal.Gold)
    for k in range(1, nv):
        x = x0 + (x1 - x0) * k / nv
        s.box((x - 0.035, y - 0.07, z0), (x + 0.035, y + 0.02, z1), pal.Gold)
    z = z0 + dz
    while z < z1 - 0.3:
        s.box((x0, y - 0.045, z - 0.025), (x1, y + 0.01, z + 0.025), pal.Chrome)
        z += dz


def curved_glazing(s, path, z0, z1, a=0.36, step=1.2, dz=1.4, closed=False, interior=True):
    """Wrap-around glazing along a plan path: glass film, dark interior, square-to-curve mullions, transoms."""
    ribbon(s, offset(path, a, closed), z0, z1, pal.GlassDark, closed)
    if interior:
        ribbon(s, offset(path, a + 0.4, closed), z0, z1, pal.Enamel_Charcoal, closed)
    for p, t in along(path + path[:1] if closed else path, step, margin=0.0 if closed else step / 2):
        with s.at((p.x, p.y, 0), (0, 0, math.degrees(math.atan2(t.y, t.x)))):
            s.box((-0.035, a - 0.08, z0), (0.035, a + 0.03, z1), pal.Gold)
    for zz in (z0 + 0.04, z1 - 0.04):
        bead(s, path, a - 0.03, zz, 0.035, pal.Gold, closed)
    z = z0 + dz
    while z < z1 - 0.3:
        bead(s, path, a - 0.03, z, 0.022, pal.Chrome, closed)
        z += dz


def joints(s, x0, x1, z0, z1, y, course=0.8, block=1.6):
    """Ashlar joint lines (flush 2 cm strips) on a facade rectangle facing -Y."""
    w, y = 0.018, y - 0.003
    k, z = 0, z0 + course
    while z < z1 - 0.1:
        s.face([(x0, y, z - w / 2), (x1, y, z - w / 2), (x1, y, z + w / 2), (x0, y, z + w / 2)], pal.Limestone_Joint)
        k, z = k + 1, z + course
    for k in range(int((z1 - z0) / course) + 1):
        za, zb = z0 + k * course + w / 2, min(z0 + (k + 1) * course - w / 2, z1)
        x = x0 + block * (0.5 + 0.5 * (k % 2))
        while x < x1 - 0.2 and za < zb:
            s.face([(x - w / 2, y, za), (x + w / 2, y, za), (x + w / 2, y, zb), (x - w / 2, y, zb)],
                   pal.Limestone_Joint)
            x += block


def hedge(s, pts, z, size=0.62, seed=0):
    """Shrub cut-out clumps (crossed pal.Leaf_Shrub cards) at plan points: planted troughs and terraces."""
    uv = ((0, 0), (1, 0), (1, 1), (0, 1))
    for k, (x, y) in enumerate(pts):
        a = math.radians((k * 47 + seed * 29) % 180)
        sz = size * (0.8 + 0.4 * ((k * 5 + seed) % 7) / 6)
        for da in (0.0, math.pi / 2):
            dx, dy = math.cos(a + da) * sz / 2, math.sin(a + da) * sz / 2
            uvface(s, [(x - dx, y - dy, z - 0.12), (x + dx, y + dy, z - 0.12), (x + dx, y + dy, z + sz * 0.85),
                       (x - dx, y - dy, z + sz * 0.85)], pal.Leaf_Shrub, uv)


# ----------------------------------------------------------------- comet banner

def banner(s, x0, x1, y, zt, length, tip=0.6):
    """Hanging comet banner facing -Y: teal fabric slab, pal.Banner_Comet art front and back, V-cut foot, gold rod."""
    xm, zb, zv = (x0 + x1) / 2, zt - length, zt - length + tip
    zs = zt - (x1 - x0) * 1.5 * 0.7 * 1.3  # art v 0.3..1 at ~1.3x the sheet aspect; below it the plain field stretches
    outline = [(x0, zv), (xm, zb), (x1, zv), (x1, zt), (x0, zt)]
    with s.at((0, y, 0), (90, 0, 0)):
        s.extrude(outline, -0.012, 0.012, pal.Fabric_Banner)

    def v(z):
        return 0.3 + 0.7 * (z - zs) / (zt - zs) if z >= zs else 0.04 + 0.26 * (z - zb) / (zs - zb)
    for side, yy in ((1, y - 0.016), (-1, y + 0.016)):
        def u(x):
            return (x - x0) / (x1 - x0) if side > 0 else (x1 - x) / (x1 - x0)
        upper = [(x0, zs), (x1, zs), (x1, zt), (x0, zt)]
        lower = [(x0, zv), (xm, zb), (x1, zv), (x1, zs), (x0, zs)]
        for poly in (upper, lower):
            pts = poly if side > 0 else poly[::-1]
            uvface(s, [(x, yy, z) for x, z in pts], pal.Banner_Comet, [(u(x), v(z)) for x, z in pts])
    s.sweep([(x0 - 0.2, y, zt + 0.05), (x1 + 0.2, y, zt + 0.05)], 0.032, pal.Gold, seg=12, caps=False)
    for x in (x0 - 0.24, x1 + 0.24):
        ball(s, (x, y, zt + 0.05), 0.07, pal.Gold, seg=16)


# ----------------------------------------------------------------- globe and armillary

LAND = ((20, 5, 22, 36), (15, 50, 26, 12), (90, 50, 55, 20), (78, 20, 9, 12), (45, 24, 13, 11), (105, 15, 12, 12),
        (-100, 48, 32, 20), (-88, 22, 12, 9), (-60, -15, 17, 30), (135, -25, 18, 11), (-40, 72, 14, 8),
        (0, -86, 400, 13))


def land(lon, lat):
    f = -1.0
    for lo, la, rl, rt in LAND:
        d = (lon - lo + 180) % 360 - 180
        f = max(f, 1 - (d / rl) ** 2 - ((lat - la) / rt) ** 2)
    return f + 0.22 * math.sin(math.radians(lon * 5 + lat * 3)) * math.cos(math.radians(lat * 6 - lon * 2))


def globe(s, r, h=0.035, nlon=80, nlat=40, lon0=-15):
    """Relief globe: chrome seas, raised satin-steel continents, chrome graticule. Lon 0 faces -Y, axis +Z."""
    def pt(lon, lat, rr):
        cl = math.cos(math.radians(lat))
        lo = math.radians(lon)
        return rr * cl * math.sin(lo), -rr * cl * math.cos(lo), rr * math.sin(math.radians(lat))

    def rad(lon, lat):
        f = land(lon + lon0, lat)
        return r + h * min(1.0, max(0.0, (f + 0.08) / 0.16))
    verts, faces, cells = [pt(0, -90, r)], [], []
    for j in range(1, nlat):
        lat = -90 + 180 * j / nlat
        verts += [pt(360 * i / nlon, lat, rad(360 * i / nlon, lat)) for i in range(nlon)]
    verts.append(pt(0, 90, r))
    top = len(verts) - 1
    for j in range(nlat):
        for i in range(nlon):
            i1 = (i + 1) % nlon
            a, b = 1 + (j - 1) * nlon, 1 + j * nlon
            if j == 0:
                faces.append([0, b + i1, b + i])
            elif j == nlat - 1:
                faces.append([a + i, a + i1, top])
            else:
                faces.append([a + i, a + i1, b + i1, b + i])
            cells.append(land(360 * (i + 0.5) / nlon + lon0, -90 + 180 * (j + 0.5) / nlat) > 0)
    bv = s._add(verts, faces, pal.Chrome, True)
    steel = s._mi(pal.Steel)
    lookup = {frozenset(bv[k] for k in fc): c for fc, c in zip(faces, cells)}
    for f in {f for v in bv for f in v.link_faces}:
        if lookup.get(frozenset(f.verts)):
            f.material_index = steel
    rg = r + h * 0.6
    for lat in (-45, 0, 45):
        s.sweep([pt(360 * k / 48, lat, rg) for k in range(49)], 0.012, pal.Chrome, seg=10, caps=False)
    for lon in (0, 45, 90, 135):
        s.sweep([pt(lon, 360 * k / 48, rg) for k in range(49)], 0.012, pal.Chrome, seg=10, caps=False)


def armillary(s, c, R):
    """Armillary sphere: three chrome meridian straps, equator, tilted gold ecliptic, gold polar axis, core sphere."""
    with s.at(c):
        for az in (0, 60, 120):
            with s.at((0, 0, 0), (90, 0, az)):
                s.lathe(ring(R - 0.05, R, -0.12, 0.12), pal.Chrome, seg=56, cap_bottom=False, cap_top=False)
        s.lathe(ring(R - 0.11, R - 0.06, -0.13, 0.13), pal.Chrome, seg=56, cap_bottom=False, cap_top=False)
        with s.at((0, 0, 0), (0, 23.5, 0)):
            s.lathe(ring(R - 0.18, R - 0.13, -0.09, 0.09), pal.Gold, seg=56, cap_bottom=False, cap_top=False)
        with s.at((0, 0, 0), (-28, 0, 0)):
            s.lathe([(0, -R - 0.18), (0.035, -R - 0.18), (0.035, R + 0.18), (0, R + 0.18)], pal.Gold, seg=12)
            for z in (-R - 0.18, R + 0.18):
                ball(s, (0, 0, z), 0.07, pal.Gold, seg=16)
        ball(s, (0, 0, 0), 0.24, pal.Chrome, seg=28)


# ----------------------------------------------------------------- parasol

def parasol(s, R=1.35, z_apex=2.42, z_edge=2.0, n=8, drop=0.17):
    """Scalloped 8-panel canopy in pal.Awning_Teal: each panel's UVs sit inside one teal or cream stripe of the
    sheet (1.2 m tile), so panels alternate like the reference with the fabric weave kept. Double-sided."""
    tile, stripe = 1.2, (0.0625, 0.1875)  # texture u of a teal / a cream stripe centre
    nr, na = 4, 4
    for i in range(n):
        th0 = 2 * math.pi * i / n
        uc = stripe[i % 2]
        verts, uvs = [], []
        for jr in range(nr + 1):
            t = jr / nr
            for ja in range(na + 1):
                w = ja / na
                ph = th0 + 2 * math.pi / n * w
                sc = math.sin(math.pi * w)
                rr = max(0.02, R * t * (1 - 0.07 * sc))
                z = z_apex - (z_apex - z_edge) * t ** 1.25 - 0.045 * sc * t
                verts.append((rr * math.cos(ph), rr * math.sin(ph), z))
                uvs.append(((uc + 0.09 * (w - 0.5) * 0.9) * tile, t * 0.9 * tile))
        # valance: hem drops with one scallop per panel
        for ja in range(na + 1):
            x, y, z = verts[nr * (na + 1) + ja]
            w = ja / na
            verts.append((x * 1.004, y * 1.004, z - drop + 0.06 * (1 - math.sin(math.pi * w))))
            uvs.append(((uc + 0.09 * (w - 0.5) * 0.9) * tile, 1.0 * tile))
        faces = [[jr * (na + 1) + ja, jr * (na + 1) + ja + 1, (jr + 1) * (na + 1) + ja + 1, (jr + 1) * (na + 1) + ja]
                 for jr in range(nr + 1) for ja in range(na)]
        uvmesh(s, verts, faces, uvs, pal.Awning_Teal)
        under = [(x, y, z - 0.012) for x, y, z in verts]
        uvmesh(s, under, [f[::-1] for f in faces], uvs, pal.Awning_Teal)
