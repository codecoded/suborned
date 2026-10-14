"""The crossing signal "eye" and the kerb stud (brief 009, A).

SignalEye: a chrome orb hovering over the kerb, a round display on each of two opposite faces, a thin light beam down to a glowing disc on the paving where people
wait. Sphere, not saucer (the concepts: eye, play, play-2, play-avenue and eye-cafe are spheres, only eye-2 is a saucer): a sphere shows its display
from any yaw and from the play camera's 38 degrees above, where a saucer's upper hull hides a display set in its rim; its silhouette is a circle at 30 px; it is
the cheapest read per pixel; and the kit already speaks in chrome spheres (lamps, bollards, the Records globe).

Parts (separate objects, all sharing the piece origin = the ground point under the beam, so any of them can move by a z offset or turn about the vertical axis):
  DK_SignalEye        chrome body, finial, nozzle, two brass bezels, two dark display plates, the emitter lens. Casts shadows (the one caster).
  DK_SignalEye_Walk   the walking figure on both faces, DK_Cross_Walk. Shown by default.
  DK_SignalEye_Stop   the standing figure on both faces, DK_Cross_Stop (red). Hidden by default; Latch enables one of the two.
  DK_SignalEye_Beam   open 8-sided cone, DK_Cross_Beam (glow); UV u = round, v = 0 at the ground to 1 at the nozzle, for a fade.
  DK_SignalEye_Halo   ground ring DK_Cross_Light and a translucent disc DK_Cross_Halo; the disc's UV is its x, y in metres from the beam centre.
Front -Y is display A, +Y is display B; both always show the same aspect. Hover: the orb centre is ZC above the ground, the nozzle 2.18 m."""

import math

from crossing_common import flat, strip
from geo import circle, dome
from kit import asset
from mats import pal

R, ZC, A = 0.42, 2.70, 0.255            # orb radius, hover height of its centre, display plate radius
D = math.sqrt(R * R - A * A)             # distance of the plate plane from the centre: the plate's rim sits on the sphere
FACES = ((90, 0, 0), (90, 0, 180))       # local +Z out of the face: -Y (A) and +Y (B)
FIG = 1.15                               # pictogram scale (figure 0.4 m tall on a 0.51 m plate)


def bone(a, b, w):
    """A limb as a CCW quad from a to b, w wide."""
    dx, dy = b[0] - a[0], b[1] - a[1]
    L = math.hypot(dx, dy)
    nx, ny = -dy / L * w / 2, dx / L * w / 2
    return [(a[0] - nx, a[1] - ny), (b[0] - nx, b[1] - ny), (b[0] + nx, b[1] + ny), (a[0] + nx, a[1] + ny)]


def figure(s, kind, z, mat, hi):
    """Walking (arms and legs apart) or standing (arms down, legs together) figure in the local XY of a face, at local height z."""
    k = lambda p: (p[0] * FIG, p[1] * FIG)  # noqa: E731
    head = (0.012 if kind == "walk" else 0.0, 0.145)
    flat(s, circle(0.038 * FIG if kind == "walk" else 0.04 * FIG, 8 if hi else 4, *k(head)), z, mat)
    if kind == "walk":
        limbs = [((0.0, 0.095), (-0.008, -0.02), 0.07), ((0.0, -0.015), (0.075, -0.165), 0.034), ((-0.01, -0.015), (-0.085, -0.165), 0.034)]
        arms = [((0.02, 0.085), (0.085, 0.005), 0.026), ((-0.015, 0.085), (-0.07, 0.01), 0.026)]
    else:
        limbs = [((0.0, 0.095), (0.0, -0.03), 0.09), ((-0.022, -0.03), (-0.025, -0.165), 0.04), ((0.022, -0.03), (0.025, -0.165), 0.04)]
        arms = [((-0.062, 0.09), (-0.066, -0.04), 0.03), ((0.062, 0.09), (0.066, -0.04), 0.03)]
    for a, b, w in limbs + (arms if hi else []):
        flat(s, bone(k(a), k(b), w * FIG), z, mat)


def eye(s, lod):
    hi = lod == 0
    seg, rings, pseg = (20, 7, 20) if hi else (10, 3, 8)
    with s.at((0, 0, ZC), FACES[0]):  # the body: a sphere cut flat at both display planes, revolved about the display axis (local Z = -Y)
        phi0 = math.asin(A / R)
        s.lathe([(R * math.sin(phi0 + (math.pi - 2 * phi0) * k / rings), -R * math.cos(phi0 + (math.pi - 2 * phi0) * k / rings)) for k in range(rings + 1)],
                pal.Chrome, seg=seg, cap_bottom=False, cap_top=False)
    with s.at((0, 0, ZC)):
        s.lathe([(0.07, R - 0.02), (0.04, R + 0.035), (0, R + 0.11)] if hi else [(0.07, R - 0.02), (0, R + 0.11)], pal.Chrome, seg=10 if hi else 6, cap_bottom=False)  # finial
        s.lathe([(0.04, -(R + 0.07)), (0.055, -(R + 0.045)), (0.10, -(R - 0.03))] if hi else [(0.04, -(R + 0.07)), (0.10, -(R - 0.03))],
                pal.Chrome, seg=10 if hi else 6, cap_bottom=False, cap_top=False)                                                                # nozzle (open: the sphere and the lens close it)
        if hi:
            s.lathe([(0, -(R + 0.072)), (0.032, -(R + 0.072))], pal.Cross_Light, seg=10, cap_top=False)                                                      # emitter lens, facing down
    for rot in FACES:
        with s.at((0, 0, ZC), rot):
            s.lathe([(A, D), (0, D)], pal.Enamel_Charcoal, seg=pseg, cap_bottom=False)                                                                          # display plate
            foot = (A + 0.05, math.sqrt(R * R - (A + 0.05) ** 2) - 0.004)
            s.lathe([foot, (A + 0.013, D + 0.013), (A - 0.003, D + 0.001)] if hi else [foot, (A - 0.003, D + 0.008)], pal.Brass_Civic, seg=pseg, cap_bottom=False, cap_top=False)  # bezel
    for name, mat in (("Walk", pal.Cross_Walk), ("Stop", pal.Cross_Stop)):
        for rot in FACES:
            with s.at((0, 0, ZC), rot), s.part(name) as p:
                figure(p, name.lower(), D + 0.004, mat, hi)
    zb, zt = 0.012, ZC - (R + 0.075)
    with s.part("Beam") as b:
        b.sheet(lambda u, v: ((0.17 - 0.125 * v) * math.cos(2 * math.pi * u), (0.17 - 0.125 * v) * math.sin(2 * math.pi * u), zb + (zt - zb) * v), 8 if hi else 6, 1, pal.Cross_Beam)
    with s.part("Halo") as h:
        n = 20 if hi else 8
        strip(h, circle(0.56, n), circle(0.47, n), 0.006, pal.Cross_Light, closed=True)
        flat(h, circle(0.47, n), 0.005, pal.Cross_Halo)


@asset("SignalEye", "Signals", bevel=0.0, near=lambda s: eye(s, 1))
def signal_eye(s):
    eye(s, 0)


def stud(s, lod):
    seg, n = (10, 3) if lod == 0 else (6, 1)
    s.lathe([(0.20, 0.0), (0.20, 0.008), (0.16, 0.014)] + dome(0.16, 0.014, 0.07, n)[1:], pal.Brass_Civic, seg=seg, cap_bottom=False)


@asset("KerbStud", "Street", bevel=0.0, near=lambda s: stud(s, 1))
def kerb_stud(s):
    """Brass dome stud, 0.40 m across its flange and 84 mm high, open underneath: set along the kerb edge (and, as in the play concept, at a beam's foot)."""
    stud(s, 0)
