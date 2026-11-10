"""Brief 101 (Vela, 2026-11-09; Sky picked B, briefs/101/vantage/square-camera.md): the square's lamp mast. The terrace's sphere lamp grows into the
square's camera mast: the city lights and watches its square from the same standard.

MastCamera_Lamp   Signals   The lamp at its own heights (PlazaSphereLamp_Play is the store PlazaSphereLamp, restore026): the teal sleeve, the chrome
                            pole, the arms at 2.42 and 3.80 m with the café's green comet banner between them, the glow ring and the 0.45 m chrome globe
                            at 4.36 m, now on a honed limestone plinth with a brass hairline. The chrome column carries on through the globe, with seafoam
                            bands, to a chrome crown (bowl, seafoam band, drum, brass hairline, domed cap, ball finial) at 7.4-8.2 m. Three cameras fan
                            out of the crown 60 deg apart on chrome spokes, 15 deg nose down: ivory streamline bodies tapered to the spoke, a deep teal
                            stripe along the top that widens into a deep teal lens hood (charcoal inside, a sunshade reaching further at the top), a
                            chrome bezel and a dark lens, each lens centre 7.68 m up (the engine's mast lens, GDD §05 sight). Deep teal, not seafoam,
                            on the cameras: seafoam vanished against the café's seafoam-and-white glazing behind them at the opening view.
  Pivot   the column's foot on the pavement (z 0). The footprint is not centred on it: the banner reaches kit -X (Blender +X), the cameras the front.
  Heads   camera k looks along kit (Unity) +X turned k x 60 deg toward kit +Z: kit (cos 60k, 0, sin 60k), Blender angle 180 + 60k (kit x = -Blender x,
          kit z = -Blender y). The world export's facings are yaw + k * headStep (tools/world/gen/street/quiet.ts) and a street piece stands at its
          item's yaw (KitPlacer.Place: kit +X -> plan (cos yaw, sin yaw)), so with the square's head step 60 camera k looks along facings[k] at any yaw.
          At B the item's yaw is plan -30 deg (Vantage's yaw 330; block yawDeg 240 through civic/dress.ts planYaw, lamp-terrace-w's today): the
          cameras look ESE, ENE and N, and the arms and banner hang where today's lamp has them (plan 150 deg, broadside to the opening camera).
  Parts   DK_MastCamera_Lamp_Lens0 / _Lens1 / _Lens2: head k's dark lens (DK_Eye_Lens, emission black as built). The host tints the head that sees for
          the attention stages (dim, glow, pulse, flash: §15 robots line, HostViews.Attention CamCue), as it does DK_HoverCamera_Lens. LENS below
          gives each lens centre and look direction in kit axes; tools/decokit/review101_mast.py measures them from the export.
Materials: the palette only, no new material: DK_Limestone, DK_Brass_Civic (hairlines), DK_Chrome, DK_Enamel_Teal (the lamp's sleeve, the cameras' stripes
and hoods), DK_Enamel_Seafoam (the column's and crown's bands), DK_Enamel_Ivory (camera bodies), DK_Lamp (glow ring), DK_Banner_Comet (the banner),
DK_Enamel_Charcoal (hood linings, lens wells), DK_Eye_Lens. Solids are declared: the plinth, the sleeve and the column to 7.40 m, never the crown, the
cameras or the banner, so no lens stands inside its own volume."""

import math

import bmesh

import hover_camera  # noqa: F401  (registers DK_Eye_Lens: the camera family's lens, which the host drives)
from civic_props097 import rband, rloft, ring_prof
from geo import dome, rrect
from kit import asset
from mats import pal
from signals import ball, band, banner, puck

# ----------------------------------------------------------------------------- the lamp, at PlazaSphereLamp's heights
BASE = 0.14                                   # the limestone plinth's top; the lamp's sleeve and pole stand on it
POLE_R = 0.072                                # the chrome pole under the lamp head (the lamp's 0.065, a little stouter for an 8 m standard)
GLOBE_Z, GLOBE_R = 4.36, 0.225                # the chrome globe, as the lamp's
ARMS = ((3.80, 0.66, 0.016), (2.42, 0.66, 0.013))   # (height, reach along Blender +X, tube radius): the banner hangs between them
SHAFT = (4.70, 0.068, 7.42, 0.058)            # the column above the globe: (z0, r0, z1, r1)
BANDS = (5.60, 6.60)                          # seafoam bands on the column (and one under the crown)
# ----------------------------------------------------------------------------- the crown and its cameras
STEP, TILT = 60.0, 15.0                       # degrees between heads (the plan's square head step), nose-down tilt
CAM_S = 1.15                                  # the cameras' scale in their frame (built at 1, drawn 15 % up so the crown reads at play zoom)
TAIL0 = 0.32                                  # camera frame (+Z forward from the crown's centre along the tilted axis, before CAM_S): the tail's tip
LENS_T = 0.50                                 # the lens centre in front of the tail's tip (camera frame)
LENS_D = (TAIL0 + LENS_T) * CAM_S             # the lens centre's distance from the crown's centre along the axis (0.94 m)
HUB_Z, DRUM_R = round(7.68 + LENS_D * math.sin(math.radians(TILT)), 3), 0.175   # the crown's centre, so every lens centre stands at 7.68 m; its drum
PODS = [(k, 180.0 + STEP * k) for k in range(3)]   # head k, its Blender angle


# ----------------------------------------------------------------------------- the heads (brief 104: Sky, "this ugly is poorly done"; was a tapered ivory petal under a flared hood)
# Camera frame, before CAM_S: +Z forward along the look from the crown's centre, +Y up. A streamline housing of a rounded-box section (a superellipse, so it
# reads as a housing, not a bulb), a rounded back with a chrome trim ring, a deep teal sunshade visor riding above it and overhanging the lens, a deep teal
# hood round the lens (straight, no flare) with a brass hairline where it meets the body. Every coloured edge is an edge of its own solid, never a seam in
# the skin, so the enamel stays crisp at play zoom.
HA, HB, HP, HN = 0.140, 0.134, 2.7, 32        # the housing's half-width and half-height, the superellipse's power, points round it
BACK, FRONT_Z = TAIL0 + 0.005, TAIL0 + 0.42   # the housing's back (just behind the spoke's tail collar) and where the hood takes over
MOUTH = TAIL0 + 0.545                         # the hood's mouth (the lens sits 7 cm inside it)
VIS = (TAIL0 + 0.05, TAIL0 + 0.615, 0.016, 0.011, math.radians(41))   # the visor: from, to (its overhang), its underside over the housing, thickness, half-angle


def sect(sx, sy=None, dy=0.0, n=HN):
    """The housing's section scaled by sx (and sy), lifted dy: a superellipse, CCW from the right flank."""
    sy = sx if sy is None else sy
    out = []
    for k in range(n):
        a = 2 * math.pi * k / n
        c, s_ = math.cos(a), math.sin(a)
        out.append((sx * HA * math.copysign(abs(c) ** (2 / HP), c), sy * HB * math.copysign(abs(s_) ** (2 / HP), s_) + dy))
    return out


def grown(d, n=HN):
    """The section grown d metres all round (near enough a normal offset on this round box)."""
    return sect((HA + d) / HA, (HB + d) / HB, n=n)


def arcband(lo, hi, a, n=12):
    """A slice of the section grown lo..hi over the top, half-angle a either side of up: a curved plate's section, CCW (outer right to left, inner back)."""
    def pt(d, t):
        c, s_ = math.cos(t), math.sin(t)
        return ((HA + d) * math.copysign(abs(c) ** (2 / HP), c), (HB + d) * math.copysign(abs(s_) ** (2 / HP), s_))
    ts = [math.pi / 2 - a + 2 * a * k / n for k in range(n + 1)]
    return [pt(hi, t) for t in ts] + [pt(lo, t) for t in reversed(ts)]


def housing(s):
    """The ivory housing, its rounded back and the chrome trim ring behind its middle."""
    back = [(0.46, 0.0), (0.62, 0.006), (0.75, 0.018), (0.86, 0.038), (0.94, 0.066), (0.985, 0.1), (1.0, 0.135)]                     # a streamlined back
    secs = [(sect(k), BACK + dz) for k, dz in back] + [(sect(1.0), BACK + z) for z in (0.25, 0.34)] + [(sect(1.0), FRONT_Z + 0.012)]
    s.loft(secs, pal.Enamel_Ivory, cap_top=False)
    trim = BACK + 0.16
    s.loft([(grown(-0.001), trim - 0.008), (grown(0.005), trim - 0.005), (grown(0.005), trim + 0.005), (grown(-0.001), trim + 0.008)], pal.Chrome,
           cap_bottom=False, cap_top=False)                                                                                           # chrome trim ring


def hood(s):
    """The deep teal hood: out of the housing at the brass hairline, straight to a rounded lip; charcoal inside, down to the lens's chrome bezel."""
    z0 = FRONT_Z
    s.loft([(grown(0.003), z0 - 0.012), (grown(0.007), z0 - 0.008), (grown(0.007), z0 + 0.002), (grown(0.003), z0 + 0.006)], pal.Brass_Civic,
           cap_bottom=False, cap_top=False)                                                                                           # brass hairline
    outer = [(grown(0.0), z0), (grown(0.010), z0 + 0.012), (grown(0.012), MOUTH - 0.018), (grown(0.010), MOUTH - 0.005), (grown(0.004), MOUTH)]
    inner = [(grown(0.004), MOUTH), (grown(-0.004), MOUTH - 0.004), (grown(-0.006), MOUTH - 0.016), (grown(-0.006), TAIL0 + 0.482)]
    bv = s.loft(outer, pal.Enamel_Teal, cap_bottom=False, cap_top=False)
    bv += s.loft(inner, pal.Enamel_Charcoal, cap_bottom=False, cap_top=False)
    bv += s.loft([(grown(-0.006), TAIL0 + 0.482), ([(0.124 * math.cos(2 * math.pi * k / HN), 0.124 * math.sin(2 * math.pi * k / HN)) for k in range(HN)],
                  TAIL0 + 0.478)], pal.Enamel_Charcoal, cap_bottom=False, cap_top=False)                                              # the well's face
    bmesh.ops.remove_doubles(s.bm, verts=bv, dist=1e-6)


def visor(s):
    """The sunshade: a deep teal enamel plate riding the housing's top, its back rounded off, overhanging the hood's mouth by 7 cm."""
    z0, z1, lo, th, a = VIS                                                                                                            # lo clears the hood's crown (+12 mm)
    plan = [(z0, 0.16), (z0 + 0.03, 0.42), (z0 + 0.07, 0.68), (z0 + 0.12, 0.88), (z0 + 0.18, 1.0), (z1 - 0.024, 1.0), (z1 - 0.008, 0.9), (z1, 0.74)]   # a swept tail
    secs = [(arcband(lo, lo + th, a * w), z) for z, w in plan]
    s.loft(secs, pal.Enamel_Teal, cap_bottom=False, cap_top=False)
    m = len(secs[0][0])                                                                                                                # its ends as quads (a curved n-gon
    for (sec, z), back in ((secs[0], True), (secs[-1], False)):                                                                        # can triangulate badly in Unity)
        for i in range(m // 2 - 1):
            q = [(*sec[i], z), (*sec[i + 1], z), (*sec[m - 2 - i], z), (*sec[m - 1 - i], z)]
            s.face(q[::-1] if back else q, pal.Enamel_Teal, smooth=False)


def camera(s, k):
    """Head k's camera in its own frame: local +Z forward along the look (tilted), +Y up, the origin at the crown's centre."""
    s.sweep([(0, 0, 0.1), (0, 0, BACK + 0.004)], 0.03, pal.Chrome, seg=14)                                                          # the chrome spoke
    wall = DRUM_R / math.cos(math.radians(TILT)) / CAM_S                                                                             # the drum's wall on the axis
    s.lathe(ring_prof(0.028, 0.048, wall - 0.008, wall + 0.03), pal.Chrome, seg=16, cap_bottom=False, cap_top=False)                # its collar at the drum
    s.lathe([(0.03, BACK - 0.034), (0.052, BACK - 0.028), (0.064, BACK - 0.014), (0.067, BACK + 0.006)], pal.Chrome, seg=20,
            cap_bottom=False, cap_top=False)                                                                                         # the chrome tail cap
    housing(s)
    hood(s)
    visor(s)
    s.lathe([(0.124, TAIL0 + 0.462), (0.0, TAIL0 + 0.478)], pal.Enamel_Charcoal, seg=24, cap_bottom=False, cap_top=False)           # the lens well
    s.lathe([(0.121, TAIL0 + 0.466), (0.121, TAIL0 + 0.484), (0.116, TAIL0 + 0.492), (0.109, TAIL0 + 0.488)], pal.Chrome, seg=32,
            cap_bottom=False, cap_top=False)                                                                                         # the chrome bezel
    with s.part(f"Lens{k}") as p:
        p.lathe(dome(0.112, TAIL0 + 0.476, 0.05, n=4), pal.Eye_Lens, seg=24, cap_bottom=False)                                     # the dark lens
        p.lathe(dome(0.047, TAIL0 + 0.51, 0.025, n=3), pal.Eye_Lens, seg=14, cap_bottom=False)                                     # its inner element


def lamp(s):
    """The terrace's sphere lamp at its own heights (signals.plaza_sphere_lamp, the store piece the zone draws), on the limestone plinth."""
    rloft(s, [(0.58, 0.58, 0.1, 0.0), (0.58, 0.58, 0.1, 0.1), (0.565, 0.565, 0.095, 0.125), (0.53, 0.53, 0.085, BASE)], pal.Limestone, n=4)  # honed plinth
    rband(s, 0.58, 0.58, 0.1, 0.082, 0.094, pal.Brass_Civic, n=4, lip=0.004)                                                       # brass hairline
    sq = lambda w, r=0.03: rrect(w, w, r, 3)  # noqa: E731
    s.loft([(sq(0.45, 0.05), BASE - 0.004), (sq(0.47, 0.06), BASE + 0.01), (sq(0.47, 0.06), BASE + 0.04), (sq(0.44, 0.05), BASE + 0.055)], pal.Chrome)
    s.loft([(sq(0.42), BASE + 0.045), (sq(0.42), BASE + 0.09), (sq(0.34), BASE + 0.73)], pal.Enamel_Teal, cap_bottom=False, cap_top=False)  # teal sleeve
    s.loft([(sq(0.43), BASE + 0.08), (sq(0.43), BASE + 0.115)], pal.Chrome, cap_bottom=False, cap_top=False)
    s.loft([(sq(0.34), BASE + 0.71), (sq(0.38), BASE + 0.72), (sq(0.38), BASE + 0.75), (sq(0.34), BASE + 0.78), (sq(0.22, 0.05), BASE + 0.81),
            (sq(0.2, 0.05), BASE + 0.82)], pal.Chrome)
    s.lathe([(0.11, BASE + 0.81), (0.11, BASE + 0.83), (0.09, BASE + 0.86), (0.08, BASE + 0.92), (0.076, BASE + 0.95), (POLE_R, 3.95)],
            pal.Chrome, seg=24, cap_bottom=False, cap_top=False)                                                                    # the chrome pole
    band(s, POLE_R + 0.002, POLE_R + 0.016, 1.785, 1.815, pal.Chrome, seg=24)
    s.lathe([(POLE_R - 0.004, 3.93), (0.085, 3.95), (0.085, 4.0), (0.078, 4.01), (0.078, 4.03), (0.085, 4.04), (0.085, 4.07), (0.06, 4.09), (0.045, 4.095),
             (0.045, 4.14), (0, 4.14)], pal.Chrome, seg=24, cap_bottom=False)                                                       # the lamp head's collar
    s.lathe(ring_prof(0.045, 0.1, 4.1, 4.125), pal.Lamp, seg=24, cap_bottom=False, cap_top=False)                                  # the glow ring
    ball(s, GLOBE_R, pal.Chrome, seg=40, n=16, z=GLOBE_Z)                                                                            # the chrome globe
    for z, x1, r in ARMS:
        band(s, POLE_R - 0.002, POLE_R + 0.012, z - 0.03, z + 0.03, pal.Chrome, seg=20)
        s.sweep([(0.07, 0, z), (x1, 0, z)], r, pal.Chrome, seg=10)
        with s.at((x1 + r, 0, 0)):
            ball(s, r * 1.9, pal.Chrome, seg=12, n=6, z=z)
    banner(s, 0.13, 0.63, 3.8, 1.38, pal.Banner_Comet)                                                                             # the café's comet banner


def column(s):
    """The chrome column through the globe to the crown, with seafoam bands."""
    z0, r0, z1, r1 = SHAFT
    r_at = lambda z: r0 + (r1 - r0) * (z - z0) / (z1 - z0)  # noqa: E731
    s.lathe([(0.05, GLOBE_Z + GLOBE_R - 0.02), (0.064, GLOBE_Z + GLOBE_R + 0.01), (0.072, 4.64), (r0, z0), (r1, z1)], pal.Chrome, seg=20,
            cap_bottom=False, cap_top=False)
    s.lathe(ring_prof(0.066, 0.082, 4.6, 4.66), pal.Chrome, seg=20, cap_bottom=False, cap_top=False)                               # collar on the globe
    for z in BANDS:
        s.lathe(ring_prof(r_at(z) - 0.002, r_at(z) + 0.014, z - 0.04, z + 0.04), pal.Enamel_Seafoam, seg=20, cap_bottom=False, cap_top=False)
    s.lathe(ring_prof(r1 - 0.002, r1 + 0.018, 7.34, 7.46), pal.Enamel_Seafoam, seg=20, cap_bottom=False, cap_top=False)            # under the crown


def crown(s):
    """The chrome crown the cameras' spokes leave from: a flared bowl with a seafoam band, the drum, a brass hairline and a chrome bead, a seafoam enamel
    cap sloping to a brass ring, a chrome dome and a ball finial (brief 104: the cap was a bare chrome dome that read as glass from the play camera)."""
    s.lathe([(0.056, 7.44), (0.064, 7.52), (0.08, 7.6), (0.115, 7.68), (0.155, 7.75), (0.172, 7.8), (DRUM_R, 7.83), (DRUM_R, 7.955)], pal.Chrome, seg=40,
            cap_bottom=False, cap_top=False)
    s.lathe(ring_prof(0.158, 0.172, 7.755, 7.79), pal.Enamel_Seafoam, seg=40, cap_bottom=False, cap_top=False)                    # seafoam band on the bowl
    s.lathe(ring_prof(DRUM_R - 0.001, DRUM_R + 0.004, 7.932, 7.942), pal.Brass_Civic, seg=40, cap_bottom=False, cap_top=False)    # brass hairline
    s.lathe([(DRUM_R, 7.955), (DRUM_R + 0.007, 7.96), (DRUM_R + 0.007, 7.972), (DRUM_R - 0.004, 7.98)], pal.Chrome, seg=40,
            cap_bottom=False, cap_top=False)                                                                                         # the chrome bead
    s.lathe([(DRUM_R - 0.004, 7.98), (0.15, 7.995), (0.125, 8.012)], pal.Enamel_Seafoam, seg=40, cap_bottom=False, cap_top=False)  # the seafoam cap
    s.lathe(ring_prof(0.118, 0.128, 8.008, 8.022), pal.Brass_Civic, seg=32, cap_bottom=False, cap_top=False)                      # its brass ring
    s.lathe([(0.12, 8.012)] + dome(0.12, 8.018, 0.062, n=6)[1:], pal.Chrome, seg=32, cap_bottom=False)                              # the chrome dome
    s.lathe(puck(0.026, 8.07, 8.085, 0.006, n=1), pal.Chrome, seg=16)                                                                # finial collar
    s.lathe([(0.016, 8.08), (0.014, 8.12)], pal.Chrome, seg=12, cap_bottom=False, cap_top=False)                                     # finial stem
    ball(s, 0.04, pal.Chrome, seg=20, n=10, z=8.155)                                                                                 # finial ball


@asset("MastCamera_Lamp", "Signals", bevel=0.0)
def mast_camera_lamp(s):
    lamp(s)
    column(s)
    crown(s)
    for k, phi in PODS:
        with s.at((0.0, 0.0, HUB_Z), (90 + TILT, 0, phi - 270), scale=CAM_S):   # local +Z -> Blender angle phi, TILT below the horizontal; +Y up (tipped forward)
            camera(s, k)
    s.solid((-0.29, -0.29, 0.0), (0.29, 0.29, BASE))             # the plinth
    s.solid((-0.235, -0.235, BASE), (0.235, 0.235, BASE + 0.82))  # the sleeve and its cap
    s.solid((-0.08, -0.08, BASE + 0.82), (0.08, 0.08, 7.40))      # the column (the globe, banner, crown and pods are left out)


# What the host reads, in kit axes (Unity: x = -Blender x, y = Blender z, z = -Blender y), from the pivot at the column's foot.
def _lens(k, phi):
    a, t, d = math.radians(phi), math.radians(TILT), LENS_D
    h = d * math.cos(t)
    bx, by, bz = h * math.cos(a), h * math.sin(a), HUB_Z - d * math.sin(t)
    fx, fy, fz = math.cos(t) * math.cos(a), math.cos(t) * math.sin(a), -math.sin(t)
    return dict(part=f"DK_MastCamera_Lamp_Lens{k}", facing=k, centre=[round(-bx, 3), round(bz, 3), round(-by, 3)], look=[round(-fx, 3), round(fz, 3), round(-fy, 3)])


LENS = [_lens(k, phi) for k, phi in PODS]
