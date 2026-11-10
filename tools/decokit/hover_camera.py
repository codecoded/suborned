"""The hovering street camera and its wall cradle (brief 096, Vela, 2026-11-05; Sky picked concept A3 "hovering": paid/096-street-camera/codex/A3-hovering.png,
the porthole eye of A-porthole-eye.png floating on a ring of light, docked in a wall cradle when dead). Replaces the pole camera's look (CCTVPole); the sim's camera sites stay.

HoverCamera   Signals   The head. Pivot = the body's turn centre (NOT the ground: the piece hovers, so the host places it at the camera site's lens height and turns it there).
                        Front -Y in Blender = Unity +Z (the lens looks along the instance's forward); up +Z = Unity +Y. Parts share the origin (kit convention), each its own renderer:
  DK_HoverCamera          the body: the ivory enamel teardrop with a seafoam swoosh on each flank, brass hairlines on the swoosh edges, the coral eye rim, the chrome bezel,
                          the lamp collar, four chrome screws, the belly pad and its chrome boss. DK_Eye_Ivory, DK_Eye_Seafoam, DK_Eye_Coral, DK_Brass_Civic, DK_Chrome.
  DK_HoverCamera_Lens     the big dark lens dome inside the rim: DK_Eye_Lens (emit kind, emission black as built): the host tints it for the attention stages (_EmissionColor / _BaseColor).
  DK_HoverCamera_Tally    the amber tally lamp on top: DK_Eye_Tally (emit): on = live, off (_EmissionColor black, _BaseColor dimmed) = dead; looped is the host's call (blink).
  DK_HoverCamera_Ring     the ring of light under the belly it floats on: DK_Eye_Ring (emit): off when dead (disable the renderer or emission black).
HoverCamera_Cradle  Signals  The wall cradle. Pivot = the bottom centre of the wall plate, its back face on the wall (y = 0), front -Y = Unity +Z (out of the wall), as DomeCamera.
                        A stepped seafoam and ivory wall plate with chrome screws and a brass hairline, a seafoam swan-neck arm, an ivory saddle the head's belly pad drops into.
                        DOCK = where the head's origin sits when docked (Blender frame); the head then faces out of the wall, level.

Materials are matte painted enamel (ceramic kind: satin, no clear coat; Sky: less shine), a little chrome; the lens alone is glossy. Sizes are final world size (scale 1).
The numbers the host needs are in DATA (written to briefs/096/vela/camera.md by hand, and printed by hover_camera_review.py)."""

import math

import mats
from geo import arc, dome
from kit import asset
from mats import pal, register
from signals import FRONT, bezel, puck, slab

register("DK_Eye_Ivory", "ceramic", "#F3E1CB")     # Glint 096: peach ivory toward the concept (H28-30; was the kit ivory #EFE8D6, which read khaki-grey in game light); matte (unity_mats.MATTE_096)
register("DK_Eye_Seafoam", "ceramic", "#7FBCA6")   # Glint 096: a lighter, slightly greener seafoam toward the concept's sage band (was the kit seafoam #72B5A7); matte
register("DK_Eye_Coral", "ceramic", "#E8735A")     # the concept's eye rim (#E86D4E): a deeper coral than accent_coral #fc9c84, which washes out on ivory at play zoom; matte (Glint 096, was #E2725B)
register("DK_Eye_Lens", "emit", "#10181E", 0.0)    # dark glass, glossy (emit kind: smoothness 0.8); emission off as built, the host drives it
register("DK_Eye_Tally", "emit", "#FF9A1F", 1.4)   # the tally lamp: amber (concept #FBA80B); Glint 096: x1.4 (was #FF9A14 x1.6) on a dark base (unity_mats.EMIT_BASE_096), since the sun on an orange base swamped the glow and it read yellow
register("DK_Eye_Ring", "emit", "#48D8E0", 1.6)    # the hover ring: turquoise (the HUD's #60E0EA family; Glint 096, was mint #5FE0C0 x1.8) on a dark base; the concept's warm white vanishes on cream stone
register("DK_Brass_Civic", "brass", mats.BRASS)    # hairline brass (civic_common registers the same row)

# ----------------------------------------------------------------------------- the head's measures (metres, Blender frame: forward = -Y, up = +Z)
R, TAIL, NOSE = 0.27, 0.78, 0.15          # body radius at the turn centre, tail length behind it, nose length in front of it (to the rim)
LENS_R, LENS_Z = 0.197, NOSE + 0.03       # lens dome radius; where its base sits along the forward axis (it bulges 5 cm more)
LAMP_Y = -0.03                            # the tally lamp's centre along y (3 cm forward of the turn centre)
PAD_Y, PAD_R = 0.06, 0.18                 # the belly pad's centre along y (6 cm behind the turn centre, clear of the rim) and radius
PAD = -0.328                              # the lowest point: the light ring's bottom
U = [0, .012, .035, .07, .12, .19, .235, .28, .33, .38, .43, .48, .53, .58, .63, .68, .73, .78, .825, .87, .905, .94, .97, 1.0]   # rings along the body, dense at the tail tip (brief 104: twice as many, the flanks were faceted)


def rad(z):
    """Body radius at forward position z (z < 0 behind the turn centre)."""
    if z <= 0:
        return R * max(0.0, 1 - (-z / TAIL) ** 2.0) ** 0.55
    return R - 0.02 * (z / NOSE) ** 2


def edges(z):
    """The seafoam swoosh's edges (radians about the forward axis, 0 = the right flank, + up) at forward position z: a point at the tail, widening to the rim."""
    s = ((z + TAIL) / (TAIL + NOSE)) ** 0.8
    return math.radians(-4 - 40 * s), math.radians(4 + 28 * s)


def _region(s, zs, a0, a1, n, mat):
    """One panel of the body between angle functions a0(z) < a1(z) (CCW about the forward axis), in the forward frame (local +Z = forward, +Y = up). Ring 0 is the tail pole."""
    verts, rings = [(0.0, 0.0, zs[0])], [[0]]
    for z in zs[1:]:
        lo, hi, r = a0(z), a1(z), rad(z)
        rings.append(list(range(len(verts), len(verts) + n + 1)))
        verts += [(r * math.cos(lo + (hi - lo) * k / n), r * math.sin(lo + (hi - lo) * k / n), z) for k in range(n + 1)]
    faces = [[0, rings[1][k + 1], rings[1][k]] for k in range(n)]
    faces += [[a[k], a[k + 1], b[k + 1], b[k]] for a, b in zip(rings[1:], rings[2:]) for k in range(n)]
    return s._add(verts, faces, mat, True)


def _screw(s, p, n):
    """A small chrome dome screw head at p, its axis along the outward normal n = (cos a, sin a, 0) of the current frame."""
    with s.at(p, (0, 0, math.degrees(math.atan2(n[1], n[0])))), s.at((0, 0, 0), (0, 90, 0)):
        s.lathe([(0.016, -0.004)] + dome(0.016, 0.0, 0.007, n=2)[1:], pal.Chrome, seg=6, cap_bottom=False)


def head(s):
    import bmesh
    zs = [-TAIL + (TAIL + NOSE) * u for u in U]
    lo, hi = (lambda z: edges(z)[0]), (lambda z: edges(z)[1])
    with s.at((0, 0, 0), FRONT):   # the forward frame: local +Z = forward (-Y), local +Y = up
        # Brief 104 (Sky on the mast's heads, "this ugly is poorly done"): the brass hairlines on the swoosh's edges were 3-sided tubes laid chord to chord over
        # the curved skin, so they sank in and out of it and the baked AO drew dark teeth beside them. They are now 12 mm strips of the skin itself (w), crisp.
        w = lambda z: min(0.012 / max(rad(z), 0.02), 0.25 * (hi(z) - lo(z)))  # noqa: E731
        bands = [(lo, lambda z: lo(z) + w(z), 1, pal.Brass_Civic), (lambda z: lo(z) + w(z), lambda z: hi(z) - w(z), 6, pal.Eye_Seafoam),
                 (lambda z: hi(z) - w(z), hi, 1, pal.Brass_Civic), (hi, lambda z: math.pi - hi(z), 16, pal.Eye_Ivory),                      # right flank, the back
                 (lambda z: math.pi - hi(z), lambda z: math.pi - hi(z) + w(z), 1, pal.Brass_Civic),
                 (lambda z: math.pi - hi(z) + w(z), lambda z: math.pi - lo(z) - w(z), 6, pal.Eye_Seafoam),
                 (lambda z: math.pi - lo(z) - w(z), lambda z: math.pi - lo(z), 1, pal.Brass_Civic),
                 (lambda z: math.pi - lo(z), lambda z: 2 * math.pi + lo(z), 16, pal.Eye_Ivory)]                                            # left flank, the belly
        bv = []
        for a0, a1, n, mat in bands:
            bv += _region(s, zs, a0, a1, n, mat)
        bmesh.ops.remove_doubles(s.bm, verts=bv, dist=1e-6)   # one smooth skin: the panels share their seams and the tail pole
        for side in (1, -1):   # two chrome screws on each flank, in the swoosh
            for z in (-0.42, 0.04):
                a = sum(edges(z)) / 2
                a = a if side == 1 else math.pi - a
                _screw(s, (rad(z) * math.cos(a), rad(z) * math.sin(a), z), (math.cos(a), math.sin(a), 0.0))
        zf = NOSE
        s.lathe([(rad(zf) - 0.006, zf - 0.014), (0.265, zf + 0.008), (0.264, zf + 0.034), (0.24, zf + 0.052), (0.203, zf + 0.046)], pal.Eye_Coral, seg=30, cap_bottom=False, cap_top=False)                  # the coral eye rim
        s.lathe([(0.205, zf + 0.046), (0.2, zf + 0.036), (0.19, zf + 0.03)], pal.Chrome, seg=30,
                cap_bottom=False, cap_top=False)                                                                                    # the chrome bezel
        s.lathe([(rad(zf), zf), (0, zf + 0.02)], pal.Enamel_Charcoal, seg=12, cap_bottom=False, cap_top=False)                      # the lens well's back
        with s.part("Lens") as p:
            p.lathe(dome(LENS_R, LENS_Z, 0.05, n=4), pal.Eye_Lens, seg=30, cap_bottom=False)                                         # the big dark lens
            p.lathe(dome(0.092, LENS_Z + 0.036, 0.04, n=3), pal.Eye_Lens, seg=20, cap_bottom=False)                                  # its inner element, the pupil, standing proud
        s.lathe([(0.088, LENS_Z + 0.034), (0.1, LENS_Z + 0.044), (0.108, LENS_Z + 0.04), (0.11, LENS_Z + 0.03)], pal.Chrome, seg=20,
                cap_bottom=False, cap_top=False)                                                                                    # the chrome iris ring between them
    zt = rad(-LAMP_Y) - 0.012   # the back's height under the lamp (forward position -LAMP_Y)
    with s.at((0, LAMP_Y, zt)):
        s.lathe([(0.112, -0.02), (0.112, 0.004), (0.104, 0.012), (0.09, 0.016), (0.086, 0.034)], pal.Chrome, seg=20,
                cap_bottom=False, cap_top=False)                                                                                    # the lamp's chrome collar
        with s.part("Tally") as t:
            t.lathe([(0.086, 0.026)] + dome(0.086, 0.03, 0.085, n=4)[1:], pal.Eye_Tally, seg=16, cap_bottom=False)                   # the amber tally lamp
    with s.at((0, PAD_Y, 0)):
        s.lathe([(PAD_R - 0.024, -0.274)] + arc(PAD_R - 0.018, -0.255, 0.018, -90, 0, 2) + [(PAD_R, -0.17)], pal.Eye_Ivory, seg=28,
                cap_bottom=False, cap_top=False)                                                                                    # the belly pad
        s.lathe([(0.086, -0.279), (PAD_R - 0.024, -0.274)], pal.Enamel_Charcoal, seg=28, cap_bottom=False, cap_top=False)            # the ring's seat (faces down)
        s.lathe([(0.0, -0.284), (0.07, -0.284), (0.086, -0.279)], pal.Chrome, seg=16, cap_bottom=False, cap_top=False)               # the pad's chrome boss
        with s.part("Ring") as g:   # the ring of light it floats on: a flat ring 1.6 cm under the pad, wider than the pad so its near side shows under the body from the play camera
            g.lathe([(0.13, -0.31), (0.135, -0.32), (0.19, PAD), (0.245, -0.32), (0.25, -0.31), (0.19, -0.3), (0.13, -0.31)], pal.Eye_Ring, seg=28, cap_bottom=False, cap_top=False)


@asset("HoverCamera", "Signals", bevel=0.0)
def hover_camera(s):
    head(s)


# ----------------------------------------------------------------------------- the cradle
PW, PH, PT = 0.36, 0.64, 0.032            # wall plate width, height, thickness (the seafoam back plate)
DOCK = (0.0, -0.90, 0.47)                 # the head's origin when docked: 0.90 m out of the wall (its tail tip clears the ribs by 4 cm), 0.47 m above the plate's foot, facing out
SADDLE = DOCK[2] + PAD + 0.004            # the saddle's top: the ring rests 4 mm into it
ARM = [(0, -0.09, 0.09), (0, -0.25, 0.075), (0, -0.45, 0.07), (0, -0.62, 0.076), (0, -0.75, 0.095), (0, DOCK[1] + PAD_Y, SADDLE - 0.07)]


def cradle(s):
    with s.at((0, 0, PH / 2), FRONT):    # local XY = the wall (x across, y up), local +Z out of the wall (-Y)
        slab(s, PW, PH, 0.09, 0.0, PT, pal.Eye_Seafoam, e=0.008, n=4, m=2)
        for k, (w, h, r) in enumerate(((0.29, 0.57, 0.07), (0.25, 0.52, 0.06), (0.21, 0.47, 0.05))):   # the stepped ivory ribs (A-porthole-eye's bracket)
            slab(s, w, h, r, PT - 0.004 + 0.016 * k, PT + 0.016 * (k + 1), pal.Eye_Ivory, e=0.005, n=3, m=1)
        bezel(s, PW - 0.012, PH - 0.012, 0.084, 0.006, PT - 0.002, PT + 0.004, pal.Brass_Civic, n=3)           # the brass hairline round the plate
        for x, y in ((0, 0.302), (0, -0.302), (0.163, 0), (-0.163, 0)):                                           # chrome screws on the seafoam border
            with s.at((x, y, PT)):
                s.lathe(puck(0.012, -0.004, 0.006, 0.004, n=1), pal.Chrome, seg=8)
        with s.at((0, ARM[0][2] - PH / 2, PT + 0.048)):                                                            # the arm's chrome boss on the ribs
            s.lathe(puck(0.06, -0.01, 0.03, 0.01, n=2), pal.Chrome, seg=16)
    s.sweep(ARM, [0.042, 0.042, 0.04, 0.038, 0.036, 0.035, 0.034], pal.Eye_Seafoam, seg=10)                      # the arm, out along the wall's normal and up into the saddle
    with s.at((0, -0.3, 0.073), FRONT):                                                                            # a chrome collar on the arm
        s.lathe([(0.044, -0.014), (0.049, -0.008), (0.049, 0.008), (0.044, 0.014)], pal.Chrome, seg=10, cap_bottom=False, cap_top=False)
    with s.at((DOCK[0], DOCK[1] + PAD_Y, SADDLE)):   # the saddle under the belly: a shallow ivory dish the (unlit) ring settles into, seafoam underneath, a chrome stem into the arm
        s.lathe([(0.03, -0.07), (0.06, -0.045), (0.2, -0.03), (0.28, -0.016)], pal.Eye_Seafoam, seg=28, cap_bottom=False, cap_top=False)
        s.lathe([(0.28, -0.016), (0.282, 0.004), (0.27, 0.014), (0.255, 0.01), (0.24, -0.004), (0.0, -0.006)], pal.Eye_Ivory, seg=28, cap_bottom=False, cap_top=False)
        s.lathe([(0.0, -0.12), (0.035, -0.12), (0.035, -0.06)], pal.Chrome, seg=10, cap_bottom=False, cap_top=False)


@asset("HoverCamera_Cradle", "Signals", bevel=0.0)
def hover_camera_cradle(s):
    cradle(s)


# What the host reads (Unity axes: x = -Blender x, y = Blender z, z = -Blender y). Kept here so the report and the review print one source.
TOP = rad(-LAMP_Y) - 0.012 + 0.115
DATA = dict(
    head=dict(piece="HoverCamera", pivot="the body's turn centre (hover point), not the ground", forward="+Z", length=round(TAIL + NOSE + 0.105, 3), width=round(2 * 0.267, 3),
              lensCentre=[0.0, 0.0, round(LENS_Z + 0.05, 3)], tallyTop=[0.0, round(TOP, 3), round(-LAMP_Y, 3)], ringCentre=[0.0, round(PAD, 3), round(-PAD_Y, 3)], lowest=round(PAD, 3), ringRadius=[0.13, 0.25],
              parts=dict(Lens="DK_HoverCamera_Lens", Tally="DK_HoverCamera_Tally", Ring="DK_HoverCamera_Ring"),
              dead=dict(tally="#6B4A1E", ring="#5E6B6A", note="_EmissionColor black and _BaseColor these, on DK_HoverCamera_Tally / _Ring: the ring stays drawn so a docked head rests on it")),
    cradle=dict(piece="HoverCamera_Cradle", pivot="bottom centre of the wall plate, back face on the wall", forward="+Z (out of the wall)",
                dock=[DOCK[0], DOCK[2], -DOCK[1]], plate=[PW, PH]),
)
