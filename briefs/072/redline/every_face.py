# Redline 072-D phase 1: every-face module census and per-view frustum counts, computed from the world export.
# Read-only on inputs. Run: python3 -I out/scratch/redline/every_face.py <world.json> <kit.json> <out.json>
# All counts here are COMPUTED (no Unity). Tris per new piece are ASSUMED from analogues until Vela's table (072-A).
import json, math, sys
import numpy as np

world_p, kit_p, out_p = sys.argv[1], sys.argv[2], sys.argv[3]
W = json.load(open(world_p)); K = json.load(open(kit_p))
kit = {a['id']: a for a in K['assets']}

# ---------- masses ----------
def ring_area(r):
    return 0.5 * sum(r[i][0] * r[(i + 1) % len(r)][1] - r[(i + 1) % len(r)][0] * r[i][1] for i in range(len(r)))

masses = []  # (id, ring np, base, top, kind)
for b in W['buildings']: masses.append((b['id'], np.array(b['ring'], float), b['base'], b['base'] + b['height'], 'bld'))
for c in W['civic']:
    if c.get('ring') and c.get('height'): masses.append((c['id'], np.array(c['ring'], float), c.get('base', 0), c.get('base', 0) + c['height'], 'civic'))
for m in W['megastructures']:
    for p in m['parts']: masses.append((p['id'], np.array(p['ring'], float), p['base'], p['base'] + p['height'], 'mega'))
bboxes = np.array([[r[:, 0].min(), r[:, 1].min(), r[:, 0].max(), r[:, 1].max()] for _, r, _, _, _ in masses])

def bld_of(i): return masses[i][0].rsplit('/', 1)[0]
def inside_any(px, py, pz, skip, skip_bld=False):
    """vectorised: for points (px,py,pz) whether any mass other than `skip` contains them (footprint and height)."""
    hit = np.zeros(px.shape, bool)
    x0, y0, x1, y1 = px.min(), py.min(), px.max(), py.max()
    cand = np.where((bboxes[:, 0] <= x1) & (bboxes[:, 2] >= x0) & (bboxes[:, 1] <= y1) & (bboxes[:, 3] >= y0))[0]
    for j in cand:
        if j == skip or (skip_bld and bld_of(j) == bld_of(skip)): continue
        _, r, base, top, _ = masses[j]
        zin = (pz >= base - 0.05) & (pz <= top + 0.05)
        if not zin.any(): continue
        # point in polygon (even-odd)
        inside = np.zeros(px.shape, bool)
        n = len(r)
        for i in range(n):
            ax, ay = r[i]; bx, by = r[(i + 1) % n]
            cond = ((ay > py) != (by > py))
            with np.errstate(divide='ignore', invalid='ignore'):
                xi = ax + (py - ay) * (bx - ax) / (by - ay)
            inside ^= cond & (px < xi)
        hit |= inside & zin
    return hit

# ---------- existing kit facades as covering volumes ----------
kitbox = []  # (a, t, n_in, L, depth_in, out, zb, zt)
for f in W['facades']:
    a = np.array(f['a'], float); b = np.array(f['b'], float); L = np.linalg.norm(b - a)
    if L < 0.1: continue
    k = kit.get(f['kit']['id']); sy = f['kit']['scale'][1]
    H = (k['size'][1] if k else 20) * sy
    kitbox.append((a, b, f.get('base', 0.18), f.get('base', 0.18) + H))

def covered_by_kit(px, py, pz, inward):
    """inward: unit normal pointing into the building for these face points. A point is under kit when it lies within 14.5 m
    behind a kit facade segment (on the same side) and below its top."""
    hit = np.zeros(px.shape, bool)
    for a, b, zb, zt in kitbox:
        t = (b - a); L = np.linalg.norm(t); t = t / L
        # the kit's inward normal: the building side. Use the face's own inward if parallel; else skip non-parallel kit
        if abs(t[0] * inward[1] - t[1] * inward[0]) < 0.9: continue  # kit edge not parallel to this face
        rx, ry = px - a[0], py - a[1]
        s = rx * t[0] + ry * t[1]
        dn = rx * inward[0] + ry * inward[1]  # depth into the building from the kit plane
        hit |= (s >= -0.5) & (s <= L + 0.5) & (dn >= -2.0) & (dn <= 14.5) & (pz >= zb - 0.3) & (pz <= zt + 0.3)
    return hit

# ---------- tiling every exposed face ----------
BAY = 6.0; GROUND = 5.0; STOREY = 3.4; CROWN = 1.1
mods = []      # dict(piece, cls, c=[x,y], z0, z1, yaw, w, d)
corners = []
faces_out = 0; exposed_area = 0.0; kit_area = 0.0; party_area = 0.0; total_wall = 0.0
for mi, (mid, r, base, top, kind) in enumerate(masses):
    if kind != 'bld': continue
    if ring_area(r.tolist()) < 0: r = r[::-1]
    n = len(r); H = top - base
    face_bays = []
    for i in range(n):
        a = r[i]; b = r[(i + 1) % n]; t = b - a; L = float(np.linalg.norm(t))
        if L < 0.5: face_bays.append([]); continue
        t /= L; out = np.array([t[1], -t[0]])  # CCW ring: outward is the right-hand normal
        inward = -out
        ss = np.arange(0.5, L, 1.0); zz = np.arange(base + 0.5, top, 1.0)
        S, Z = np.meshgrid(ss, zz, indexing='ij')
        px = a[0] + t[0] * S + out[0] * 0.3; py = a[1] + t[1] * S + out[1] * 0.3
        party = inside_any(px.ravel(), py.ravel(), Z.ravel(), mi).reshape(S.shape)
        kitc = covered_by_kit(px.ravel(), py.ravel(), Z.ravel(), inward).reshape(S.shape) & ~party
        exp = ~party & ~kitc
        total_wall += S.size; party_area += party.sum(); kit_area += kitc.sum(); exposed_area += exp.sum()
        # service test: from the face, does another mass stand within 12 m at street level?
        mid_s = L / 2
        probe = np.arange(1.0, 12.5, 1.0)
        qx = a[0] + t[0] * mid_s + out[0] * probe; qy = a[1] + t[1] * mid_s + out[1] * probe
        zq = max(2.0, base + 2.0)  # at the face's own lowest storey, past its own building's other masses
        service = bool(inside_any(qx, qy, np.full(qx.shape, zq), mi, True).any())
        nb = max(1, int(round(L / BAY))) if L >= 3.0 else 0
        w = L / nb if nb else 0
        bays = []
        for k in range(nb):
            cols = (ss >= k * w) & (ss < (k + 1) * w)
            e = exp[cols]
            if e.size == 0 or e.mean() < 0.25: bays.append(None); continue
            zrows = np.where(e.any(axis=0))[0]
            z0 = float(zz[zrows.min()] - 0.5); z1 = float(zz[zrows.max()] + 0.5)
            c = a + t * (k + 0.5) * w
            bays.append((z0, z1))
            h = z1 - z0; fam = 'AlleyWall' if service else 'SideBay'
            yaw = math.atan2(out[1], out[0])
            def put(piece, cls, za, zb): mods.append(dict(piece=piece, cls=cls, c=[float(c[0]), float(c[1])], z0=za, z1=zb, yaw=yaw, w=w, d=1.5, mass=mid, face=i))
            ground = z0 <= 0.6
            if ground and h <= 17.5: put(f'{fam}_3', fam, z0, z1)
            elif ground and h <= 21.5: put(f'{fam}_4', fam, z0, z1)
            else:
                # stack: base (ground storey) when it starts at the street, mids, a top with the crown
                zc = z0
                if ground: put(f'{fam}Stack_Base' if fam == 'SideBay' else 'AlleyWall_4', fam, zc, zc + GROUND); zc += GROUND
                nm = max(0, int(round((z1 - zc - STOREY - CROWN) / STOREY)))
                for _ in range(nm): put(f'{fam}Stack_Mid', fam, zc, zc + STOREY); zc += STOREY
                put(f'{fam}Stack_Top', fam, zc, z1)
        face_bays.append(bays)
        faces_out += 1
    # corners: a vertex whose two faces both carry a bay at that end
    for i in range(n):
        fin = face_bays[i - 1] if n else []; fout = face_bays[i]
        if not fin or not fout or fin[-1] is None or fout[0] is None: continue
        z0 = max(fin[-1][0], fout[0][0]); z1 = min(fin[-1][1], fout[0][1])
        if z1 - z0 < 3: continue
        p0 = r[i - 1]; p1 = r[i]; p2 = r[(i + 1) % n]
        cr = (p1[0] - p0[0]) * (p2[1] - p1[1]) - (p1[1] - p0[1]) * (p2[0] - p1[0])
        conv = cr > 0
        typ = 'Out' if conv else 'In'
        h = z1 - z0
        def putc(piece, za, zb): corners.append(dict(piece=piece, cls='Corner', c=[float(p1[0]), float(p1[1])], z0=za, z1=zb, yaw=0.0, w=2.0, d=2.0, mass=mid))
        if z0 <= 0.6 and h <= 17.5: putc(f'CornerPier_{typ}_3', z0, z1)
        elif z0 <= 0.6 and h <= 21.5: putc(f'CornerPier_{typ}_4', z0, z1)
        else:
            zc = z0
            if z0 <= 0.6: putc(f'CornerPier_{typ}_4', zc, zc + 19.7); zc += 19.7
            while zc + STOREY <= z1 + 0.5: putc(f'CornerPier_{typ}_Mid', zc, zc + STOREY); zc += STOREY

# ---------- piece cost table (ASSUMED from analogues; Vela's 072-A table replaces it) ----------
# L0 / L1 / L2 tris, materials, the analogue
COST = {
 'SideBay_3': (4322, 1054, 251, 11, 'PortholeBay_3L'),
 'SideBay_4': (5478, 1338, 319, 11, 'PortholeBay_4L'),
 'SideBayStack_Base': (2600, 640, 150, 11, 'PortholeBay_7 ground share'),
 'SideBayStack_Mid': (1100, 270, 65, 11, 'PortholeBay_7 per storey'),
 'SideBayStack_Top': (1400, 340, 80, 11, 'PortholeBay_7 top share'),
 'AlleyWall_3': (2900, 710, 170, 10, 'AlleyWall_6 x 0.5 (L1/L2 ratio as PortholeBay)'),
 'AlleyWall_4': (3600, 880, 210, 10, 'AlleyWall_6 x 0.62'),
 'AlleyWallStack_Mid': (900, 220, 55, 10, 'AlleyWall_6 per storey'),
 'AlleyWallStack_Top': (1100, 270, 65, 10, 'AlleyWall_6 top share'),
 'CornerPier_Out_3': (2400, 590, 140, 6, 'guess: quarter-round pier'),
 'CornerPier_Out_4': (2900, 710, 170, 6, 'guess'),
 'CornerPier_Out_Mid': (700, 170, 40, 6, 'guess'),
 'CornerPier_In_3': (900, 220, 55, 4, 'guess'),
 'CornerPier_In_4': (1100, 270, 65, 4, 'guess'),
 'CornerPier_In_Mid': (300, 75, 20, 4, 'guess'),
 'RecordsTower': (30000, 7400, 1800, 16, 'guess: Skyport_Tower class'),
}
# the Records Office as one landmark piece (072 RecordsTower), over the civic office masses' bounds; tris ASSUMED
rt = [m for m in masses if m[0].startswith('civic/civic-office')]
if rt:
    xs = np.concatenate([m[1][:, 0] for m in rt]); ys = np.concatenate([m[1][:, 1] for m in rt])
    landmark = dict(piece='RecordsTower', cls='Landmark', c=[float((xs.min() + xs.max()) / 2), float((ys.min() + ys.max()) / 2)], z0=0.0, z1=38.0,
                    yaw=math.pi / 2, w=float(ys.max() - ys.min()), d=float(xs.max() - xs.min()), mass='civic/civic-office')
    corners.append(landmark)
allm = mods + corners
from collections import Counter
census = Counter(m['piece'] for m in allm)
tris0 = sum(COST[m['piece']][0] for m in allm)

# ---------- views ----------
D2R = math.pi / 180
def aabb(m):
    c = np.array(m['c']); yaw = m['yaw']; t = np.array([-math.sin(yaw), math.cos(yaw)]); o = np.array([math.cos(yaw), math.sin(yaw)])
    hw, hd = m['w'] / 2, m['d'] / 2
    pts = [c + t * sx * hw + o * sd * hd for sx in (-1, 1) for sd in (-1, 1)]
    xs = [p[0] for p in pts]; ys = [p[1] for p in pts]
    # Unity: x = plan x, y = up, z = plan y
    return np.array([min(xs), m['z0'], min(ys)]), np.array([max(xs), m['z1'], max(ys)])

def size_of(m):
    return max(m['w'], m['d'], m['z1'] - m['z0'])

def plane_test(lo, hi, planes):
    for n, dd in planes:
        p = np.where(n >= 0, hi, lo)  # p-vertex
        if np.dot(n, p) + dd < 0: return False
    return True

def ortho_planes(F, yaw, zoom, aspect=16 / 9, pitch=38):
    y = yaw * D2R; p = pitch * D2R
    right = np.array([math.cos(y), 0, -math.sin(y)])
    up = np.array([math.sin(p) * math.sin(y), math.cos(p), math.sin(p) * math.cos(y)])
    hw, hh = zoom * aspect, zoom
    F = np.array(F, float)
    # n.x + d >= 0 inside
    return [(right, -np.dot(right, F) + hw), (-right, np.dot(right, F) + hw), (up, -np.dot(up, F) + hh), (-up, np.dot(up, F) + hh)]

def fwd_of(yaw, pitch):
    y = yaw * D2R; p = pitch * D2R
    return np.array([math.cos(p) * math.sin(y), -math.sin(p), math.cos(p) * math.cos(y)])

SUN_TO = -fwd_of(330, 38)  # toward the sun (light.json sun pitch 38, yaw 330)

def shadow_casters(F, yaw, zoom, standoff=160.0, aspect=16 / 9):
    """URP main-light caster cull, approximated: the TacticalShadows fit (2 cascades: air [0, s-60], street [s-60, s+40]); each cascade's
    culling sphere bounds its ortho frustum slice; a caster is anything whose bounding sphere meets the sphere swept toward the sun."""
    f = fwd_of(yaw, 38); F = np.array(F, float); cam = F - f * standoff
    hw, hh = zoom * aspect, zoom
    sph = []
    for d0, d1 in ((0.0, standoff - 60), (standoff - 60, standoff + 40)):
        cen = cam + f * (d0 + d1) / 2; rad = math.sqrt(hw * hw + hh * hh + ((d1 - d0) / 2) ** 2); sph.append((cen, rad))
    return sph

def is_caster(lo, hi, sph):
    c = (lo + hi) / 2; rho = np.linalg.norm(hi - lo) / 2
    for cen, rad in sph:
        v = c - cen; tt = np.dot(v, SUN_TO)
        if tt < -rad - rho: continue
        perp = np.linalg.norm(v - SUN_TO * tt)
        if perp <= rad + rho: return True
    return False

def persp_planes(cam, target, fov=55, aspect=16 / 9, far=350.0, near=0.2):
    f = target - cam; f /= np.linalg.norm(f)
    r = np.cross(np.array([0, 1, 0.0]), f); r /= np.linalg.norm(r)  # Unity left-handed: right = up x forward
    u = np.cross(f, r)
    th = math.tan(fov / 2 * D2R); tw = th * aspect
    planes = []
    for n in (f * th + u * -1 * 1, ):
        pass
    # side planes through the camera
    def pl(n): n = n / np.linalg.norm(n); return (n, -np.dot(n, cam))
    planes = [pl(f * th - u), pl(f * th + u), pl(f * tw - r), pl(f * tw + r), pl(f)]
    farp = (-f, np.dot(f, cam) + far)
    planes.append(farp)
    return planes, f

# LOD for perspective (Unity: relative height = size / (2 d tan(fov/2)) * lodBias)
def lod_persp(m, cam, lods, fov=55):
    lo, hi = aabb(m); c = (lo + hi) / 2; d = max(np.linalg.norm(c - cam), 0.01)
    rel = size_of(m) / (2 * d * math.tan(fov / 2 * D2R))
    for i, th in enumerate(lods):
        if rel >= th: return i, d
    return len(lods), d  # culled

TODAY_FAC = (0.2, 0.07)  # kit.json lodCategories.facades
def today_lods(m):
    s = size_of(m); cull = min(0.004 if s >= 8 else 0.01, 0.5 * s / 37.0)
    return [0.2, 0.07, cull]
PROPOSED_D = (45.0, 120.0, 330.0)  # metres: L1, L2, cull (the 350 m haze clip less a margin)
def proposed_lods(m, fov=55):
    s = size_of(m); k = 2 * math.tan(fov / 2 * D2R)
    return [s / (k * D) for D in PROPOSED_D]

def costs(m, lvl):
    c = COST[m['piece']]
    return 0 if lvl >= 3 else c[lvl]

FOCI = {
 'office-front': [-160, 0, -281], 'cell-open': [-170.5, 0, -227.5],
 'home': [-159, 0, -232.5], 'cafe-square': [-159, 0, -221.5], 'heaviest': [-60, 0, -200], 'parade': [212, 0, 0],
 'crown-circus': [0, 0, 0], 'north-edge': [-220, 0, 280], 'harbour-quay': [321, 9.43, -506.5],
}
res = {'inputs': {'world': world_p, 'kit': kit_p}, 'census': dict(census), 'modules': len(allm), 'tris_l0_total': tris0,
       'wall_m2': {'total': float(total_wall), 'party': float(party_area), 'kit': float(kit_area), 'exposed': float(exposed_area)}}
boxes = [(m, *aabb(m)) for m in allm]
kitf = []
for f in W['facades']:
    a = np.array(f['a']); b = np.array(f['b']); k = kit.get(f['kit']['id']); H = k['size'][1] * f['kit']['scale'][1] if k else 20
    kitf.append((f['kit']['id'], np.array([min(a[0], b[0]) - 1, f.get('base', 0.18), min(a[1], b[1]) - 1]), np.array([max(a[0], b[0]) + 1, f.get('base', 0.18) + H, max(a[1], b[1]) + 1]), (k['tris'] if k else 0)))

views = []
for name, F in FOCI.items():
    for yaw in (30, 120, 210, 300):
        for zoom in (14, 18.5):
            P = ortho_planes(F, yaw, zoom)
            sph = shadow_casters(F, yaw, zoom)
            vis = [m for m, lo, hi in boxes if plane_test(lo, hi, P)]
            cast = [m for m, lo, hi in boxes if is_caster(lo, hi, sph)]
            kv = [x for x in kitf if plane_test(x[1], x[2], P)]
            # ortho LOD: rel = size / (2 zoom), constant over the screen
            def lvl_o(m, lods):
                rel = size_of(m) / (2 * zoom)
                for i, th in enumerate(lods):
                    if rel >= th: return i
                return len(lods)
            t_today = sum(costs(m, lvl_o(m, today_lods(m))) for m in vis)
            t_prop = sum(costs(m, lvl_o(m, proposed_lods(m))) for m in vis)
            l0 = sum(COST[m['piece']][0] for m in vis)
            notl0_today = sum(1 for m in vis if lvl_o(m, today_lods(m)) > 0)
            notl0_prop = sum(1 for m in vis if lvl_o(m, proposed_lods(m)) > 0)
            pieces = sorted({m['piece'] for m in vis})
            draws = sum(COST[p][3] for p in pieces)
            cast_l0 = sum(COST[m['piece']][0] for m in cast); cast_l1 = sum(COST[m['piece']][1] for m in cast); cast_l2 = sum(COST[m['piece']][2] for m in cast)
            cols = len({(m['mass'], m.get('face'), round(m['c'][0], 1), round(m['c'][1], 1)) for m in vis})
            fh = np.array([math.sin(yaw * D2R), math.cos(yaw * D2R)])  # camera heading on plan (Unity x, z = plan x, y)
            def faces_cam(m):
                if m['cls'] in ('Corner', 'Landmark'): return True
                n = np.array([math.cos(m['yaw']), math.sin(m['yaw'])])
                return float(np.dot(n, -fh)) > -0.05
            front = [m for m in vis if faces_cam(m)]
            views.append(dict(front_modules=len(front), front_tris_l0=sum(COST[m['piece']][0] for m in front), view=f'{name}@{yaw}', zoom=zoom, modules=len(vis), tris_l0=l0, tris_today_lods=t_today, tris_proposed_lods=t_prop,
                              below_l0_today=notl0_today, below_l0_proposed=notl0_prop, pieces=len(pieces), draws_instanced=draws,
                              casters=len(cast), caster_tris_l0=cast_l0, caster_tris_l1_proxy=cast_l1, caster_tris_l2_proxy=cast_l2, columns=cols,
                              kit_facades_in_view=len(kv), kit_facade_tris_l0=sum(x[3] for x in kv)))
res['play'] = views

# ground (perspective) views: tp rows. Scout start block (-1.6, -44.3) -> plan (-174.3, -228.4); walk offsets in plan metres
def tp(name, walk, heading):
    P = np.array([-174.3 + walk[0], 0.18, -228.4 + walk[1]])
    h = heading * D2R; fw = np.array([math.sin(h), 0, math.cos(h)])
    cam = P - fw * 5.5 + np.array([0, 1.3 + 0.32 * 5.5, 0]); tgt = P + fw * 8 + np.array([0, 1.35, 0])
    planes, f = persp_planes(cam, tgt)
    vis = [(m, lo, hi) for m, lo, hi in boxes if plane_test(lo, hi, planes)]
    out = dict(view=name, camera=cam.round(1).tolist(), modules_in_frustum=len(vis))
    for label, fn in (('L0 only', lambda m: [1e9, 1e9, 1e9][:0] or [0.0, 0.0, 0.0]), ('today', today_lods), ('proposed', proposed_lods)):
        tri = 0; n = [0, 0, 0, 0]; bands = {'<45': 0, '45-120': 0, '120-200': 0, '200-350': 0}
        for m, lo, hi in vis:
            if label == 'L0 only': lvl = 0; d = np.linalg.norm((lo + hi) / 2 - cam)
            else: lvl, d = lod_persp(m, cam, fn(m))
            n[lvl] += 1; t = costs(m, lvl); tri += t
            bands['<45' if d < 45 else '45-120' if d < 120 else '120-200' if d < 200 else '200-350'] += t
        out[label] = dict(tris=tri, by_level={'L0': n[0], 'L1': n[1], 'L2': n[2], 'culled': n[3]}, tris_by_distance=bands)
    near = [(m, lo, hi) for m, lo, hi in vis if np.linalg.norm((lo + hi) / 2 - cam) < 150]
    far = [(m, lo, hi) for m, lo, hi in vis if np.linalg.norm((lo + hi) / 2 - cam) >= 150]
    out['renderers_per_module'] = dict(near150=len(near), far150=len(far))
    out['columns'] = dict(near150=len({(m['mass'], m.get('face'), round(m['c'][0], 1), round(m['c'][1], 1)) for m, _, _ in near}),
                          far150=len({(m['mass'], m.get('face'), round(m['c'][0], 1), round(m['c'][1], 1)) for m, _, _ in far}))
    out['hlod_cells32_far150'] = len({(int((lo[0] + hi[0]) / 64 // 1), int((lo[2] + hi[2]) / 64 // 1)) for m, lo, hi in far})
    pieces = sorted({m['piece'] for m, _, _ in vis}); out['draws_instanced_upper'] = sum(COST[p][3] for p in pieces) * 3
    return out
res['ground'] = [tp('tp-street', [0, -20], 180), tp('tp-worst', [-10, -25], 90)]
# whole district totals per LOD
res['columns_total'] = len({(m['mass'], m.get('face'), round(m['c'][0], 1), round(m['c'][1], 1)) for m in allm})
res['district'] = {lvl: sum(COST[m['piece']][i] for m in allm) for i, lvl in enumerate(('L0', 'L1', 'L2'))}
res['cost_table_assumed'] = {k: dict(L0=v[0], L1=v[1], L2=v[2], mats=v[3], analogue=v[4]) for k, v in COST.items()}
res['kit_facades_existing'] = len(W['facades'])
json.dump(res, open(out_p, 'w'), indent=1)
print(json.dumps({k: res[k] for k in ('census', 'modules', 'tris_l0_total', 'wall_m2', 'district')}, indent=1))
