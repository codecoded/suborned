"""Horizon band pieces (brief 007 C): what stands beyond the district edge, per Horizon/ concepts (pano_harbour,
harbour_edge, launch_day, planetrise, celestial). Low-poly on purpose: every piece is seen at 0.8-8 km under haze.
z = 0 at ground (spires, massifs, pads) or at the piece centre (planet, moon). Front -Y, no bevel, <= 3k tris each.
Placement rules live in contracts/world/harbour-district.grammar.md section 9 (rings, sectors, seeds, scales).
  Horizon_Spire_A  stepped deco tower, 3 tiers on a 56 x 56 base, 220 m, needle crown
  Horizon_Spire_B  saucer spire: 26 m shaft, saucer D 76 at 205 m, needle, 270 m
  Horizon_Spire_C  streamline slab 70 x 34 (r 12 corners), 180 m, speed-band notches, atom crown
  Horizon_Spire_D  needle trio on a 90 x 40 podium, 300 / 240 / 200 m
  Horizon_Massif   stepped podium 120 x 80 x 60 m with three 90-130 m towers (fills the mid ring)
  Horizon_RocketPad  gantry (4 legs, braces) 120 m with a 90 m rocket on a 40 m pad
  Horizon_RocketAscent  120 m rocket (pivot = tail), emissive flame, 440 m glowing exhaust column; placed at 300-900 m altitude
  Horizon_PlanetRinged  R 100 smooth UV sphere (64 x 32) wearing the old DecoKit gas-giant map, ring 1.3-2.2 R cut into the old ring's four bands, axis tilted 18 deg; scale to taste
  Horizon_Moon     R 100 smooth UV sphere (48 x 24) wearing the old DecoKit moon map; scale to taste (faded daytime moon, Glint adds the phase)
Planet and moon (Vela, 2026-10-27; Sky: "I really miss the brilliance of DecoKit"): the 40-segment spheres of hard flat colour bands are replaced by smooth spheres with
latitude-longitude UVs carrying the old DecoKit sky bodies' own maps (horizon_tex.py runs sky_sheet.py unchanged: pixel-identical to the old DK_Planet_Bands / DK_Planet_Ring /
DK_Moon). Matte textured Lit (mats.GROUND rows), one material per body; hand-built L1 / L2 at half and quarter segments so the UVs survive (a collapse would tear the seam)."""

import math

from geo import rrect
from kit import asset
import mats
from mats import GROUND, TEXTURES, accent, pal, register
from skyport_parts import band, ball
from structures import soft_extrude

for n, h in (  # round 3 (Sky: ring A read as "stacked grey boxes"): cream a step warmer than the haze #f3eee4, fins in a
             # deeper stone so the vertical rhythm survives the fog, teal faience bands, coral fins, gold crowns.
             # dE2000 to the nearest signal: Stone 23.6, Band 42.1, Shade 22.0, Coral 22.7, Gold 20.9 (the skyline crowns keep their gold, Sky 14 Oct: decided later from a far shot).
             ("Far_Stone", "#F2E3C6"), ("Far_Band", "#5CC0B5"), ("Far_Shade", "#E2CDAA"),
             ("Far_Coral", "#F4A08E"), ("Far_Gold", "#D0C848")):
    register("DK_" + n, "enamel", h)
register("DK_Enamel_Turquoise", "enamel", accent("turquoise"))
register("DK_Enamel_Flamingo", "enamel", accent("flamingo"))
register("DK_Exhaust_Flame", "emit", "#FFF1C4", 14.0)        # exhaust sheath, dE2000 24.6 from guard
register("DK_Exhaust_Core", "emit", "#FFFBF2", 24.0)   # white-hot core, 30.8
register("DK_Plume_Glow", "emit", "#FFF8EC", 2.2)    # sunlit exhaust, brighter than the haze, 29.6
register("DK_Plume_Shade", "stone", "#D9CFC0")       # shadow-side puffs, 24.4



def tier(s, w, d, z0, z1, r=3.0, band_z=None, fins=3, stripe=False):
    """A deco tier: rounded plate, `fins` full-height pilaster fins per face in the deeper stone (the vertical rhythm that
    reads at 1 km), optional teal faience bands (horizontal, few) and a teal centre stripe per face, a gold coping."""
    poly = rrect(w, d, min(r, w / 2.2, d / 2.2), n=3)
    s.extrude(poly, z0, z1, pal.Far_Stone, smooth_sides=True)
    for z in band_z or ():
        s.extrude(rrect(w + 0.8, d + 0.8, min(r, w / 2.2) + 0.4, n=3), z, z + 2.2, pal.Far_Band, smooth_sides=True,
                  top=False, bottom=False)
    fw = max(1.2, min(w, d) * 0.05)
    for rot, half_len, half_dep in ((0, w / 2, d / 2), (90, d / 2, w / 2), (180, w / 2, d / 2), (270, d / 2, w / 2)):
        with s.at((0, 0, 0), (0, 0, rot)):
            for i in range(fins):
                x = (-0.6 + 1.2 * i / max(1, fins - 1)) * (half_len - min(r, w / 2.2)) if fins > 1 else 0.0
                s.box((x - fw / 2, -half_dep - 1.2, z0), (x + fw / 2, -half_dep + 0.1, z1 + 2.0), pal.Far_Shade)
            if stripe:
                s.box((-fw * 0.9, -half_dep - 0.5, z0 + 4.0), (fw * 0.9, -half_dep + 0.1, z1 - 4.0), pal.Far_Band)
    s.extrude(rrect(w + 0.6, d + 0.6, min(r, w / 2.2) + 0.3, n=3), z1 - 1.2, z1, pal.Far_Gold, smooth_sides=True, bottom=False)


def needle(s, z, h, r=2.0, mat=None):
    s.lathe([(r, z), (r * 0.25, z + h * 0.7), (0.0, z + h)], mat or pal.Chrome, seg=6, cap_bottom=False)


def crown_steps(s, w, z, h):
    """Ziggurat crown: three gold-edged setbacks and a lantern drum (AtomicAge exposition_palace, Blocks needle_trio)."""
    for k, f in enumerate((0.8, 0.6, 0.4)):
        ww = w * f
        s.extrude(rrect(ww, ww, ww * 0.18, n=2), z + h * 0.18 * k, z + h * 0.18 * (k + 1), pal.Far_Stone, smooth_sides=True)
        s.extrude(rrect(ww + 0.4, ww + 0.4, ww * 0.18 + 0.2, n=2), z + h * 0.18 * (k + 1) - 0.8, z + h * 0.18 * (k + 1),
                  pal.Far_Gold, smooth_sides=True, bottom=False)
    s.lathe([(w * 0.16, z + h * 0.54), (w * 0.16, z + h * 0.72), (w * 0.12, z + h * 0.78), (0.0, z + h * 0.8)], pal.Far_Gold, seg=8)


@asset("Horizon_Spire_A", "Horizon", bevel=0.0)
def spire_a(s):
    """Stepped deco tower (AtomicAge power_spire): three finned tiers, teal stripes, ziggurat crown, needle. 220 m + needle."""
    tier(s, 56, 56, 0, 70, 6, (66,), fins=4)
    tier(s, 40, 40, 70, 140, 5, (136,), fins=3, stripe=True)
    tier(s, 26, 26, 140, 192, 4, fins=3, stripe=True)
    crown_steps(s, 26, 192, 34)
    needle(s, 218, 52, 2.4)


@asset("Horizon_Spire_B", "Horizon", bevel=0.0)
def spire_b(s):
    """Saucer spire (Blocks saucer_spires): finned shaft, coral fins at the foot, teal-rimmed saucer with a gold band."""
    tier(s, 30, 26, 0, 60, 10, (56,), fins=3)
    tier(s, 24, 20, 60, 188, 8, fins=2, stripe=True)
    for rot in (0, 90, 180, 270):  # coral buttress fins at the foot
        with s.at((0, 0, 0), (0, 0, rot + 45)):
            with s.at((0, 0, 0), (90, 0, 0)):
                s.extrude([(10, 0), (30, 0), (14, 60), (10, 80)], -1.0, 1.0, pal.Far_Coral)
    s.lathe([(0.0, 188), (24, 192), (38, 200), (34, 207), (16, 211), (0.0, 213)], pal.Far_Stone, seg=24)
    s.lathe([(38.1, 198), (38.1, 202)], pal.Far_Band, seg=24, cap_bottom=False, cap_top=False)
    s.lathe([(30.0, 207.5), (24.0, 209.5)], pal.Far_Gold, seg=24, cap_bottom=False, cap_top=False)
    s.lathe([(8, 211), (5, 222), (0.0, 224)], pal.Glass, seg=12)
    needle(s, 224, 46, 1.5)


@asset("Horizon_Spire_C", "Horizon", bevel=0.0)
def spire_c(s):
    """Twin streamline towers on a shared podium joined by a central fin (Blocks mixed_skyline, hanging_terraces), atom
    crown: no more one fat slab."""
    s.extrude(rrect(70, 34, 12, n=5), 0, 40, pal.Far_Stone, smooth_sides=True)
    for z in (14, 30):
        s.extrude(rrect(70.8, 34.8, 12.4, n=5), z, z + 2.4, pal.Far_Band, smooth_sides=True, top=False, bottom=False)
    for x, h in ((-17, 150), (17, 128)):
        with s.at((x, 0, 0)):
            s.extrude(rrect(26, 26, 12.9, n=5), 40, h, pal.Far_Stone, smooth_sides=True)
            for z in range(52, int(h) - 6, 14):  # speed bands wrap the round towers
                s.extrude(rrect(26.8, 26.8, 13.3, n=5), z, z + 1.8, pal.Far_Band, smooth_sides=True, top=False, bottom=False)
            s.extrude(rrect(27.2, 27.2, 13.5, n=5), h - 1.2, h, pal.Far_Gold, smooth_sides=True, bottom=False)
            s.extrude(rrect(16, 16, 7.9, n=4), h, h + 10, pal.Far_Shade, smooth_sides=True)
    with s.at((0, 0, 0), (90, 0, 0)):  # the central fin, coral, rising past both
        s.extrude([(-3, 40), (3, 40), (3, 160), (0, 176), (-3, 160)], -1.5, 1.5, pal.Far_Coral)
    ball(s, (-17, 0, 167), 6.0, pal.Chrome, seg=10, n=5)       # atom ball on the taller tower
    with s.at((-17, 0, 0)):
        s.lathe([(10.0, 166.6), (10.0, 167.4)], pal.Far_Gold, seg=16, cap_bottom=False, cap_top=False)  # its orbit
        needle(s, 172, 12, 1.0)
    with s.at((17, 0, 0)):
        needle(s, 138, 16, 1.2)


@asset("Horizon_Spire_D", "Horizon", bevel=0.0)
def spire_d(s):
    s.extrude(rrect(90, 40, 12, n=5), 0, 36, pal.Far_Stone, smooth_sides=True)
    s.extrude(rrect(90.8, 40.8, 12.4, n=5), 20, 24, pal.Far_Band, smooth_sides=True, top=False, bottom=False)
    for x, h, w in ((-26, 200, 16), (0, 300, 22), (28, 240, 18)):
        with s.at((x, 0, 0)):
            tier(s, w, w, 36, 36 + h * 0.62, 4, fins=2, stripe=(x == 0))
            tier(s, w * 0.72, w * 0.72, 36 + h * 0.62, 36 + h * 0.74, 3, fins=1)
            s.lathe([(w * 0.34, 36 + h * 0.74), (w * 0.18, 36 + h * 0.88), (0.0, 36 + h)], pal.Far_Stone, seg=8, cap_bottom=False)
            s.lathe([(w * 0.35, 36 + h * 0.74), (w * 0.33, 36 + h * 0.765)], pal.Far_Gold, seg=8, cap_bottom=False, cap_top=False)
            needle(s, 36 + h, 30, 1.2)


@asset("Horizon_Massif", "Horizon", bevel=0.0)
def massif(s):
    tier(s, 120, 80, 0, 36, 14, (12,), fins=5)
    tier(s, 90, 56, 36, 60, 10, (46,), fins=4)
    for x, y, w, h in ((-30, 0, 24, 130), (6, 10, 20, 100), (34, -6, 22, 90)):
        with s.at((x, y, 0)):
            tier(s, w, w, 60, 60 + h * 0.8, 4, fins=2, stripe=True)
            s.lathe([(w * 0.4, 60 + h * 0.8), (w * 0.2, 60 + h * 0.92), (0.0, 60 + h)], pal.Far_Stone, seg=8, cap_bottom=False)


def rocket(s, z0, h, r):
    """Ogive rocket: silver hull with turquoise band, three swept fins, nozzle."""
    prof = [(r * 0.55, z0), (r, z0 + h * 0.08), (r, z0 + h * 0.62), (r * 0.6, z0 + h * 0.82), (r * 0.2, z0 + h * 0.95),
            (0.0, z0 + h)]
    s.lathe(prof, pal.Chrome, seg=16, cap_bottom=True)
    band(s, r * 1.01, z0 + h * 0.5, z0 + h * 0.56, pal.Far_Band, 16)
    for k in range(3):
        a = 120 * k
        with s.at((0, 0, z0), (0, 0, a)):
            s.box((-0.4, r * 0.9, h * 0.0), (0.4, r * 2.2, h * 0.08), pal.Enamel_Flamingo)
            s.box((-0.4, r * 0.8, h * 0.08), (0.4, r * 1.0, h * 0.3), pal.Enamel_Flamingo)


@asset("Horizon_RocketPad", "Horizon", bevel=0.0)
def rocket_pad(s):
    s.lathe([(26, 0), (26, 3), (24, 4)], pal.Far_Stone, seg=24, cap_bottom=False)
    s.extrude(rrect(14, 14, 2, n=2), 3, 40, pal.Far_Stone, smooth_sides=True)  # service plinth
    rocket(s, 40, 90, 7.0)
    for sx in (-1, 1):  # gantry: two lattice masts
        x = sx * 15.0
        for dy in (-3.0, 3.0):
            s.box((x - 0.8, dy - 0.8, 3), (x + 0.8, dy + 0.8, 120), pal.Enamel_Ivory)
        for z in range(10, 120, 14):  # cross braces
            s.box((x - 0.4, -3.0, z), (x + 0.4, 3.0, z + 0.7), pal.Enamel_Ivory)
            s.box((x - 0.4, -3.0, z + 7), (x + 0.4, 3.0, z + 7.7), pal.Enamel_Ivory)
    for z in (62, 92):  # swing arms
        s.box((-15, -0.7, z), (-6.5, 0.7, z + 1.0), pal.Enamel_Flamingo)
        s.box((6.5, -0.7, z), (15, 0.7, z + 1.0), pal.Enamel_Flamingo)
    s.box((-15.8, -3.8, 118), (15.8, 3.8, 120), pal.Chrome)


@asset("Horizon_RocketAscent", "Horizon", bevel=0.0)
def rocket_ascent(s):
    """Brief 007 round 2 (ascents lost in the haze): a 120 m rocket (pivot = tail), an emissive flame (white-hot core in
    a sunshine-cream sheath, 46 m) and a 440 m exhaust column of glowing puffs that billows out to r 62, brighter than
    the haze #f3e8d4 so the fog mix leaves it reading as a bright stroke; a few unlit warm-grey puffs on the shadow
    side give it volume. Unlit-safe: DK_Exhaust_Flame / DK_Exhaust_Core / DK_Plume_Glow / DK_Plume_Shade all >= dE2000 24 from
    every signal colour."""
    rocket(s, 0, 120, 9.5)
    s.lathe([(0.0, -46.0), (2.6, -34.0), (6.0, -14.0), (6.8, -4.0), (5.2, 0.6)], pal.Exhaust_Flame, seg=12, cap_top=True)
    s.lathe([(0.0, -30.0), (1.6, -20.0), (3.6, -6.0), (3.4, 0.8)], pal.Exhaust_Core, seg=10, cap_top=True)
    n = 26
    for k in range(n):
        t = k / (n - 1)
        r = 9.0 + 53.0 * t ** 0.75
        shade = k % 4 == 3
        side = (1 if (k // 4) % 2 else -1) if shade else 0
        with s.at((r * (0.2 * side + 0.1 * math.sin(k * 2.3)), -34.0 * t * t + r * 0.08 * math.cos(k * 1.7), -40 - 440 * t)):
            s.lathe([(0.0, -r), (r * 0.7, -r * 0.7), (r, 0.0), (r * 0.7, r * 0.7), (0.0, r)],
                    pal.Plume_Shade if shade else pal.Plume_Glow, seg=10)


# The sky bodies' maps (horizon_tex.py, beside the ground set): matte textured Lit, UV 0..1 over the whole body (tile 1).
for _n, _f, _sm in (("DK_Horizon_Planet", "horizon_planet_bands_v1.png", 0.15), ("DK_Horizon_Ring", "horizon_planet_ring_v1.png", 0.15), ("DK_Horizon_Moon", "horizon_moon_v1.png", 0.05)):
    register(_n, "ground", "#FFFFFF")
    TEXTURES.setdefault(_n, (_f, 1.0))
    GROUND.setdefault(_n, dict(albedo=_f, tile=(1.0, 1.0), smoothness=_sm))

RING_R0, RING_R1, RING_CUT = 130.0, 220.0, 0.3  # the old ring profile spans R0..R1 (inner -> outer); density at or under CUT is a gap


def ring_runs():
    """The ring's bands as (r0, r1) from the old ring alpha (horizon_tex.py writes the profile): the old kit drew the gaps as transparency, here they are geometry."""
    import json
    p = mats.GROUND_DIR / "horizon_ring_profile.json"
    if not p.exists():
        raise SystemExit(f"[horizon] no ring profile at {p}: run python3 tools/decokit/horizon_tex.py (bun run decokit does)")
    d = json.loads(p.read_text())["density"]
    n, runs, s = len(d), [], None
    for i, x in enumerate(list(d) + [0.0]):
        if x > RING_CUT and s is None:
            s = i
        elif x <= RING_CUT and s is not None:
            runs.append((RING_R0 + (RING_R1 - RING_R0) * s / n, RING_R0 + (RING_R1 - RING_R0) * i / n))
            s = None
    return runs


def uv_sphere(s, R, nu, nv, mat):
    """Smooth UV sphere about the local origin: one vertex per pole and nu per ring (no seam split, so the normals are continuous), lat-long UVs per loop
    (u = longitude 0..1, wrapping to 1 on the last column; v = latitude, south pole 0). Pole loops take their face's mid-u, kept half a texel off the edge rows."""
    verts = [(0.0, 0.0, -R)]
    for j in range(1, nv):
        th = math.pi * j / nv
        z, r = -R * math.cos(th), R * math.sin(th)
        verts += [(r * math.cos(2 * math.pi * k / nu), r * math.sin(2 * math.pi * k / nu), z) for k in range(nu)]
    verts.append((0.0, 0.0, R))
    top = len(verts) - 1
    ring = lambda j, k: 1 + (j - 1) * nu + k % nu  # noqa: E731
    faces, cells = [], []
    edge = 0.5 / 1024
    for k in range(nu):
        u0, u1, um = k / nu, (k + 1) / nu, (k + 0.5) / nu
        faces.append([0, ring(1, k + 1), ring(1, k)])
        cells.append([(um, edge), (u1, 1 / nv), (u0, 1 / nv)])
        for j in range(1, nv - 1):
            faces.append([ring(j, k), ring(j, k + 1), ring(j + 1, k + 1), ring(j + 1, k)])
            cells.append([(u0, j / nv), (u1, j / nv), (u1, (j + 1) / nv), (u0, (j + 1) / nv)])
        faces.append([ring(nv - 1, k), ring(nv - 1, k + 1), top])
        cells.append([(u0, (nv - 1) / nv), (u1, (nv - 1) / nv), (um, 1 - edge)])
    _uv_mesh(s, verts, faces, cells, mat, smooth=True)


def annulus(s, r0, r1, z, nu, mat, up):
    """A flat ring face (top if up, else bottom) with radial UVs: u = where it sits on the old ring profile (R0 inner -> R1 outer), v mid-strip."""
    u = lambda r: (r - RING_R0) / (RING_R1 - RING_R0)  # noqa: E731
    verts = [(r * math.cos(2 * math.pi * k / nu), r * math.sin(2 * math.pi * k / nu), z) for r in (r0, r1) for k in range(nu)]
    faces, cells = [], []
    for k in range(nu):
        k1 = (k + 1) % nu
        f = [k, k1, nu + k1, nu + k]
        c = [(u(r0), 0.5), (u(r0), 0.5), (u(r1), 0.5), (u(r1), 0.5)]
        faces.append(f if up else f[::-1])
        cells.append(c if up else c[::-1])
    _uv_mesh(s, verts, faces, cells, mat, smooth=False)


def _uv_mesh(s, verts, faces, cells, mat, smooth):
    """Faces with their own UVs (per loop, in face order), kept through Shape.build's box projection."""
    bv = s._add(verts, faces, mat, smooth)
    for f, c in zip(faces, cells):
        bf = s.bm.faces.get([bv[i] for i in f]) or s.bm.faces.get([bv[i] for i in reversed(f)])
        if bf is None:
            continue
        uv = {bv[i]: cc for i, cc in zip(f, c)}
        bf[s._uvkeep] = 1
        s._pending_uv.append((bf, [uv[lp.vert] for lp in bf.loops]))


def _planet(s, nu, nv, ring_nu):
    with s.at((0, 0, 0), (18, 0, 0)):
        uv_sphere(s, 100.0, nu, nv, pal.Horizon_Planet)
        for r0, r1 in ring_runs():
            annulus(s, r0, r1, 0.8, ring_nu, pal.Horizon_Ring, True)
            annulus(s, r0, r1, -0.8, ring_nu, pal.Horizon_Ring, False)


@asset("Horizon_PlanetRinged", "Horizon", bevel=0.0, near=lambda s: _planet(s, 32, 16, 64), far=lambda s: _planet(s, 16, 8, 32))
def planet_ringed(s):
    _planet(s, 64, 32, 128)


@asset("Horizon_Moon", "Horizon", bevel=0.0, near=lambda s: uv_sphere(s, 100.0, 24, 12, pal.Horizon_Moon), far=lambda s: uv_sphere(s, 100.0, 12, 6, pal.Horizon_Moon))
def moon(s):
    uv_sphere(s, 100.0, 48, 24, pal.Horizon_Moon)
