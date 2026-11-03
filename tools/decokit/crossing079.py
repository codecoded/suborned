"""Brief 079 note 2 (Vela, 2026-10-30; Sky: "the pedestrian lights look flat color, really ruin look of game"): the crossing's lights as real lights set in the road. ADOPTED 2026-10-30 (Splice): this is the default; DECOKIT_CROSS079=0 turns it
off. Latch applies the Unity handoff (briefs/079/crossing/unity-handoff/).

  DECOKIT_CROSS079=1 bun tools/decokit/run.ts --out out/decokit-079 --only LightDotRow_2,LightDotRow_1,HaloRing,SignalLens

Same ids, same pivots, same footprints the world binds (partkit.fitsPart reads kit.json `size`; kit.asset footprint= keeps it the stud row's, the light spilling past it is no part of it), same part names, same driving
(SignalHead: _EmissionColor, _LitFrom/_LitTo on u, _Cutoff; none of it changes).

LightDotRow_2 / _1   Each stud, outermost first, on the two slots the piece has always had:
  housing     DK_Cross_Housing (was DK_Enamel_Charcoal): a brushed nickel ring set in the road (Sky, 2026-10-30: "the lights should have metal ring around them, as they're embedded in the floor"), 0.34 m across, 8 cm round a 0.18 m lens (2 cm dark seam, 6 cm metal: about 3 px at play zoom; fitsPart allows 0.35):
              a dark seam at the asphalt, a lip 15 mm up, a shallow dish down to the lens, recessed 9 mm. Two bands of 12 quads; seam, wall and grain are cross_bezel_v1.png. Real sun and shadow, no sun specular (GDD s15: no brass in the carriageway, brief 072-G).
  light       DK_Cross_Light (the slot CrossingKit swaps; Shaders/SignalDots draws it): one mesh carrying the lens and its spill on the road. UV u = metres along the module of the stud's centre, as before (a stud is
              lit or dark, never cut). UV v is the fall-off, no longer metres across: 0 at the lens's centre, 1 at the lens's edge and the foot of the spill, 2 where the spill reaches nothing (r SPILL).
              So one float carries the whole light, with no second UV set and no texture.
HaloRing             The clock quad as before (same _Cutoff fill, signal_ring_v2.png: the band narrowed to 0.50..0.565 m, the segments apart and shaded across the band), now between two flush brushed nickel bezels, 4.5 and 3.5 cm: a lamp set in the paving.
SignalLens           Same body and faces, with a chrome lip round each display. Rim: DK_Signal_RimLens (the SignalDots profile: v 0 on the rim's centre line, 1 on its edges; hot line, deep edges). Beam: 12 sides, smooth normals (CrossGlow mode 4 fades it across
                     its width and down to the ground). Halo: the same disc grown to r 1.05 m, no longer a pale plate under a hard ring (CrossGlow mode 1 with _Absorb and _Fall)."""

import math
import os
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
from geo import circle  # noqa: E402
from kit import asset  # noqa: E402
from mats import TEXTURES, pal, register  # noqa: E402

ON = os.environ.get("DECOKIT_CROSS079") != "0"  # adopted 2026-10-30 (Sky: the metal ring; judged in the next build): the default. DECOKIT_CROSS079=0 builds the pieces as they were

# ----------------------------------------------------------------------------- the stud
PITCH = 0.5
BEZEL = os.environ.get("CROSS079_BEZEL", "8")     # review only: the widths compared on the sheet (cm round the lens, dark seam included); 8 is the pick: 4 is lost at play zoom, 7 reads, 9 starts to shrink the colour
LENS_R, HOUSING_R = {"4": (0.11, 0.15), "7": (0.10, 0.17), "8": (0.09, 0.17), "9": (0.085, 0.175)}[BEZEL]
RIDGE_IN, SPILL_R = 0.018, 0.34                   # the lip's crest stands this far in from the outer edge; the spills just meet between studs (0.5 m pitch): a row of lights, never a neon tube
Z_FOOT, Z_RIDGE, Z_LENS, Z_SPILL = 0.003, 0.015, 0.006, 0.003   # above the carriageway: the foot, the lip's crest 15 mm up, the lens 9 mm down inside it, the spill flat on the road
V_RIDGE = 0.30                                     # cross_bezel_v1.png's v at the crest: 0 the dark seam at the road, 1 the dark wall at the lens


def _radial(s, inner, outer, z_in, z_out, n, mat, u, v_in, v_out, cx):
    """Rings of (x, y) about cx, with per-vertex UV (u, v). inner None = a fan to the centre."""
    ring = lambda r, z: [(cx + r * math.cos(2 * math.pi * k / n), r * math.sin(2 * math.pi * k / n), z) for k in range(n)]  # noqa: E731
    if inner is None:
        verts, vs = [(cx, 0.0, z_in)] + ring(outer, z_out), [v_in] + [v_out] * n
        faces = [[0, 1 + k, 1 + (k + 1) % n] for k in range(n)]
    else:
        verts, vs = ring(inner, z_in) + ring(outer, z_out), [v_in] * n + [v_out] * n
        faces = [[k, n + k, n + (k + 1) % n, (k + 1) % n] for k in range(n)]
    bv = s._add(verts, faces, mat, True)
    uv = dict(zip(bv, vs))
    for f in {f for v in bv for f in v.link_faces}:
        f[s._uvkeep] = 1
        s._pending_uv.append((f, [(u, uv[lp.vert]) for lp in f.loops]))


def _band(s, x, r_a, z_a, v_a, r_b, z_b, v_b, n, mat):
    """One smooth lathe band about x with explicit UV: u round the ring (0..1, the grain), v across it. Going outside in, faces up."""
    ring = lambda r, z: [(x + r * math.cos(2 * math.pi * k / n), r * math.sin(2 * math.pi * k / n), z) for k in range(n)]  # noqa: E731
    verts = ring(r_a, z_a) + ring(r_b, z_b)
    faces = [[k, (k + 1) % n, n + (k + 1) % n, n + k] for k in range(n)]
    bv = s._add(verts, faces, mat, True)
    idx = {v: i for i, v in enumerate(bv)}
    for f in {f for v in bv for f in v.link_faces}:
        ks = [idx[lp.vert] % n for lp in f.loops]
        wrap = max(ks) - min(ks) > 1
        f[s._uvkeep] = 1
        s._pending_uv.append((f, [((k + (n if wrap and k == 0 else 0)) / n, v_a if idx[lp.vert] < n else v_b) for k, lp in zip(ks, f.loops)]))


def studs(s, count, lod):
    """The stud set into the road: a seam, a lip, a shallow brushed dish falling to the recessed lens. The seam, the wall and the grain are cross_bezel_v1.png on the same two bands; the lip and the dish take the sun and the shadow on their own normals."""
    hi = lod == 0
    n = 12 if hi else 8
    half = count * PITCH / 2
    centres = [(i - (count - 1) / 2) * PITCH for i in range(count)]
    for x in centres:   # the spills first: a lens drawn after a neighbour's spill stays on top (both are one draw, in this order)
        _radial(s, HOUSING_R, SPILL_R, Z_SPILL, Z_SPILL, n, pal.Cross_Light, x + half, 1.0, 2.0, x)
    for x in centres:
        _radial(s, None, LENS_R, Z_LENS, Z_LENS, n, pal.Cross_Light, x + half, 0.0, 1.0, x)
        if hi:
            _band(s, x, HOUSING_R, Z_FOOT, 0.0, HOUSING_R - RIDGE_IN, Z_RIDGE, V_RIDGE, n, pal.Cross_Housing)
            _band(s, x, HOUSING_R - RIDGE_IN, Z_RIDGE, V_RIDGE, LENS_R, Z_LENS, 1.0, n, pal.Cross_Housing)
        else:
            _band(s, x, HOUSING_R, Z_FOOT, 0.0, LENS_R, Z_LENS, 1.0, n, pal.Cross_Housing)


# ----------------------------------------------------------------------------- the clock ring
RING_Z, BEZEL_Z = 0.005, 0.007
BEZELS = ((0.455, 0.50), (0.565, 0.60))   # 4.5 and 3.5 cm of flush metal; the plan's clearReach stays 0.60 (parts.ts RING_OUTER); the lit band (signal_tex079.py) is 0.50 to 0.565


def ring(s, lod):
    import crossing_lens  # noqa: F401
    import signal_cells as st
    h = st.RING_HALF
    s.decal([(-h, -h, RING_Z), (h, -h, RING_Z), (h, h, RING_Z), (-h, h, RING_Z)], pal.Signal_Ring, ((1, 1), (0, 1), (0, 0), (1, 0)), lift=0.0)
    n = 32 if lod == 0 else 16
    for r0, r1 in BEZELS:
        _band(s, 0.0, r1, BEZEL_Z, 0.02, r0, BEZEL_Z, 0.98, n, pal.Cross_Housing)


# ----------------------------------------------------------------------------- the head
POOL_R = 1.05


def lens(s, lod):
    import crossing_lens as cl
    hi = lod == 0
    seg = 36 if hi else 18
    ZC, RB, HT, PR = cl.ZC, cl.RB, cl.HT, cl.PR
    body = [(PR, -HT), (0.385, -0.115), (RB, 0.0), (0.385, 0.115), (PR, HT)] if hi else [(PR, -HT), (RB, 0.0), (PR, HT)]
    with s.at((0, 0, ZC), cl.FACES[0]):
        s.lathe(body, pal.Chrome, seg=seg, cap_bottom=False, cap_top=False)
    prof = ((RB - 0.02, -0.055, 1.0), (RB + 0.035, 0.0, 0.0), (RB - 0.02, 0.055, 1.0))   # the rim's own (r, z, v): the shader's fall-off runs across it
    with s.part("Rim") as r, s.at((0, 0, ZC), cl.FACES[0]):
        ang = lambda k: 2 * math.pi * k / seg  # noqa: E731
        verts = [(p[0] * math.cos(ang(k)), p[0] * math.sin(ang(k)), p[1]) for p in prof for k in range(seg)]
        vs = [p[2] for p in prof for _ in range(seg)]
        faces = [[j * seg + k, j * seg + (k + 1) % seg, (j + 1) * seg + (k + 1) % seg, (j + 1) * seg + k] for j in range(2) for k in range(seg)]
        bv = r._add(verts, faces, pal.Signal_RimLens, True)
        uv = dict(zip(bv, vs))
        for f in {f for v in bv for f in v.link_faces}:
            f[r._uvkeep] = 1
            r._pending_uv.append((f, [(0.0, uv[lp.vert]) for lp in f.loops]))
    with s.at((0, 0, ZC)):
        s.lathe([(0.07, RB - 0.02), (0.04, RB + 0.035), (0, RB + 0.1)] if hi else [(0.07, RB - 0.02), (0, RB + 0.1)], pal.Chrome, seg=12 if hi else 8, cap_bottom=False)
        s.lathe([(0.045, -(RB + 0.075)), (0.05, -(RB + 0.045)), (0.075, -RB), (0.12, -(RB - 0.10))] if hi else [(0.045, -(RB + 0.075)), (0.12, -(RB - 0.10))],
                pal.Chrome, seg=12 if hi else 8, cap_bottom=False, cap_top=False)
    for rot in cl.FACES:
        with s.at((0, 0, ZC), rot):
            s.lathe([(PR, HT), (0, HT)], pal.Enamel_Charcoal, seg=seg, cap_bottom=False)
            if hi:   # the display's metal bezel: a chrome lip proud of the plate, in the body's own slot
                s.lathe([(PR + 0.035, HT - 0.012), (PR + 0.012, HT + 0.014), (PR - 0.004, HT)], pal.Chrome, seg=20, cap_bottom=False, cap_top=False)
    z = HT + cl.QZ
    for part, name in (("Figure", "walk_full"), ("Tens", "blank"), ("Units", "blank")):
        for rot in cl.FACES:
            with s.at((0, 0, ZC), rot), s.part(part) as p:
                if part == "Figure":
                    cl._quad(p, 0.0, 0.0, cl.FIG_Q, cl.FIG_Q, z, pal.Signal_Atlas, name)
                else:
                    cl._quad(p, -cl.DIG_X if part == "Tens" else cl.DIG_X, cl.DIG_Y, cl.DIG_W, cl.DIG_H, z + 0.001, pal.Signal_Atlas, name)
    zb, n = 0.012, 12 if hi else 6
    with s.part("Beam") as b:
        b.sheet(lambda u, v: ((0.17 - 0.125 * v) * math.cos(2 * math.pi * u), (0.17 - 0.125 * v) * math.sin(2 * math.pi * u), zb + (cl.BEAM_TIP - zb) * v), n, 1, pal.Cross_Beam)
    with s.part("Halo") as h:
        h.face([(x, y, 0.005) for x, y in circle(POOL_R, 16 if hi else 8)], pal.Cross_Halo)


if ON:
    import crossing_lens

    register("DK_Cross_Housing", "brass", "#FFFFFF")   # brushed nickel: the `brass` shading row (metallic 0.6, smoothness 0.6), the sun's specular off (unity_mats.SUN_OFF), cross_bezel_v1.png as its colour
    TEXTURES["DK_Cross_Housing"] = ("cross_bezel_v1.png", 1.0)
    register("DK_Signal_RimLens", "glow", crossing_lens.EMISSION["walk"]["colour"]["day"], crossing_lens.EMISSION_DAY["walk"], 1.0)   # no .mat from the kit: Materials/Glow/DK_Signal_RimLens.mat, Shaders/SignalDots
    TEXTURES["DK_Signal_Ring"] = ("signal_ring_v2.png", 1.0)

    @asset("LightDotRow_2", "Ground", bevel=0.0, near=lambda s: studs(s, 4, 1), footprint=(1.5 + 2 * HOUSING_R, 2 * HOUSING_R))
    def light_dot_row_2(s):
        """2 m row: four lit studs at the 0.5 m pitch (lens 0.22 m, nickel bead housing 0.30 m, light spill to r 0.42 m on the road)."""
        studs(s, 4, 0)

    @asset("LightDotRow_1", "Ground", bevel=0.0, near=lambda s: studs(s, 2, 1), footprint=(0.5 + 2 * HOUSING_R, 2 * HOUSING_R))
    def light_dot_row_1(s):
        """1 m end module of LightDotRow_2: two studs, the same pitch and UVs."""
        studs(s, 2, 0)

    @asset("HaloRing", "Ground", bevel=0.0, near=lambda s: ring(s, 1))
    def halo_ring(s):
        """The clock quad between two nickel bezels. Segment 0 at local -Y as before; place it with the head's own transform."""
        ring(s, 0)

    @asset("SignalLens", "Signals", bevel=0.0, near=lambda s: lens(s, 1), footprint=(0.95, 0.94))
    def signal_lens(s):
        lens(s, 0)

    _data = crossing_lens.data

    def data079():
        d = _data()
        d["brief079"] = "candidate: DECOKIT_CROSS079=1 (tools/decokit/crossing079.py)"
        d["rim"]["material"] = "DK_Signal_RimLens"
        d["ring"]["texture"] = "Textures/signal_ring_v2.png"
        d["ring"]["radii"] = [0.50, 0.565]
        d["dots"]["uv"] = "u = metres from the module's -X end to the stud's centre (the same for every vertex of a stud and its spill, so a stud is lit or dark, never cut); v = the fall-off: 0 at the lens centre, 1 at the lens edge and the spill's foot, 2 at the spill's end"
        d["dots"]["gate"] = "a stud and its spill are lit when _LitFrom <= u <= _LitTo; a dark stud shows dark glass in its nickel housing, and spills nothing"
        return d

    crossing_lens.data = data079
