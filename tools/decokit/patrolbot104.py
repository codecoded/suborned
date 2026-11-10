"""Brief 104 (Vela, 2026-11-10; Sky picked the ball-balancer, then "model it"): the patrol robot `PatrolBot104`, modelled from the committed sheets
briefs/104/concepts/patrol-robot-turnaround.png (front, left, back, right, top), patrol-robot-details.png (faces, the chest hatch and its rail) and
patrol-robot-chest-sidearm.png (closed, opening, open). A sage-green and cream enamel egg balancing on a ball: two dark metal halves (the sheets show dark chrome) with a cream band and a
chrome hub on each pole; a dark glass face in a cream frame; round chrome vents with slats on both sides; a chrome post on the crown carrying a halo ring and
a ball tip; two short cream arms with mitten hands; a hatch of three cream slats under the chin, hinged at its top edge, over a sidearm on a rail.
1.30 m to the top of the halo's ball, 0.89 m across the hands (the square's dock bay is 0.98 x 1.0 x 2.14 m).

Rigid parts, no skin: each object stands at its own pivot (geo.Shape.part pivot=), so code rolls, leans, hinges and slides them. Exported siblings, as every kit piece:
  DK_PatrolBot104           Body: the egg, its plates, the vents, the crown's chrome bell, the hatch's well, hinges and rail. Pivot: the ball contact (0, 0, 0.40);
                            it leans and turns about it. Every other part follows the Body except the Ball.
  DK_PatrolBot104_Ball      pivot at its centre (0, 0, 0.20); rolls in code. Axle along x (its hubs at the sides), the cream band round its equator.
  DK_PatrolBot104_ChestPanel  the hatch, hinged at its top edge (axis along x); open = about -105 deg about x (the bottom swings forward and up).
  DK_PatrolBot104_Gadget    the sidearm on its rail, pivot at its rear on the rail; out = 0.11 m along -y (Blender front; kit +z).
  DK_PatrolBot104_ArmL / _ArmR        shoulder pivot (L = the robot's own left, Blender +x, kit -x).
  DK_PatrolBot104_ForearmL / _ForearmR  the hand from the elbow ball, pivot at that ball's centre.
  DK_PatrolBot104_Halo      the post, ring and ball tip; pivot at the post's foot on the crown (0, 0, 1.105).
  DK_PatrolBot104_Screen    the face glass, UVs 0..1 across the visible glass (u to the robot's left as you face it, v up); pivot at its centre.
Materials (shared, no per-robot block): DK_RobotEnamel_Sage (new row, the sheets' green), DK_RobotEnamel_Cream, DK_Nickel_Satin (civic_common: the civic chrome
that keeps a lit and a shaded side in the zone), DK_Iron (the ball's halves, the hatch's well and rail, the sidearm's shoe and cowl, the vents' backs), DK_PatrolBot104_Screen (the blank glass)."""

import math

from mathutils import Matrix, Vector

import civic_common  # noqa: F401  (registers DK_Nickel_Satin)
from kit import asset
from mats import RobotPal, get, register
from patrolbot104_surf import Cap, Egg, _smooth, annulus, plate, rows_grid, shell, squircle, superellipse, weld
from signals import ball

register("DK_Enamel_Sage", "enamel", "#7E8C54")      # the sheets' sage green (lit body #A8A982 on the turnaround's back view, a touch darker as albedo)
register("DK_PatrolBot104_Screen", "enamel", "#191613")  # the blank dark glass; the six faces are proposed as materials beside it (briefs/104/vela/patrolbot/notes.md)
pal = RobotPal()

BALL_R = 0.20                      # the ball: 0.41 m on the sheet
CONTACT = 2 * BALL_R               # the Body's pivot, where the egg's tip rests on the ball
SCREEN = (0.91, 55.0, 0.12, 3.6)   # the glass: centre height, half-angle (deg), half-height (m), superellipse exponent
FRAME = (64.0, 0.142, 0.23, 3.0, 2.4)  # the cream frame round it: half-angle, half-height up and down from the glass centre, exponents above and below
COLS = 36                          # the skin's columns, 10 deg apart from the back (-180)
HATCH = (0.50, 0.665, 40.0)        # the hatch's opening in the skin: bottom, top, half-angle (on the skin's columns)
PANEL = (0.497, 0.668, 42.0, 0.006)  # the hatch's plate: bottom, top (the hinge), half-angle, thickness proud of the skin
GADGET = (0.0, -0.045, 0.541)      # the sidearm's pivot: its rear, on the rail
SHOULDER = (90.0, 0.70)            # (theta, z) of the shoulders on the skin
ARM = (42.0, 18.0)                 # arms: degrees below the horizontal, degrees toward the front
HINT = (0.0, -1.0, 0.35)           # the palms face forward and a little up
HALO = (0.0, 0.0, 1.105)
ZS = [0.401, 0.404, 0.409, 0.416, 0.426, 0.44, 0.457, 0.477, 0.50, 0.525, 0.555, 0.585, 0.615, 0.64, 0.665, 0.70, 0.74, 0.78, 0.82, 0.86, 0.90,
      0.94, 0.975, 1.005, 1.03, 1.05, 1.066, 1.078, 1.087]  # open at the crown, under the chrome bell


def frame_at(o, z_axis, y_hint=(0, 0, 1)):
    """(loc, Euler degrees) for s.at(): local +Z along z_axis, local +Y toward y_hint."""
    Z = Vector(z_axis).normalized()
    Y = Vector(y_hint)
    Y = (Y - Z * Y.dot(Z)).normalized()
    X = Y.cross(Z)
    e = Matrix((X, Y, Z)).transposed().to_euler("XYZ")
    return tuple(o), tuple(math.degrees(a) for a in e)


# ----------------------------------------------------------------------------- outlines on the skin (theta deg, z m)
def screen_edge(a):
    zc, th, h, n = SCREEN
    c, s = superellipse(a, n)
    return th * c, zc + h * s


def frame_edge(a):
    th, up, dn, n_up, n_dn = FRAME
    c, s = superellipse(a, n_up if math.sin(a) >= 0 else n_dn)
    return th * c, SCREEN[0] + (up if s >= 0 else dn) * s


def in_frame(th, z, k=0.88):
    th_, up, dn, n_up, n_dn = FRAME
    dz = z - SCREEN[0]
    n = n_up if dz >= 0 else n_dn
    return (abs(th) / (th_ * k)) ** n + (abs(dz) / ((up if dz >= 0 else dn) * k)) ** n < 1.0


def cheek_top(z):
    """Half-width (deg) of the cream side panels' rounded top about theta 82, the arm's socket on its crest."""
    t = (z - 0.645) / 0.08
    return 26.0 if t <= 0 else 26.0 * math.sqrt(max(0.0, 1 - t * t))


def cheek_lo(z):
    """Front edge of the cream side panel (deg): the green bib's edge, closing to a point at the tip."""
    return max(56.0 * _smooth((z - 0.425) / 0.095), 82.0 - cheek_top(z))


def cheek_hi(z):
    """Back edge (deg): the green back's edge, closing at the tip."""
    return min(108.0 + 72.0 * (1 - _smooth((z - 0.425) / 0.095)), 82.0 + cheek_top(z))


def in_cheek(th, z, m=3.5):
    return 0.43 <= z <= 0.70 and cheek_lo(z) + m <= abs(th) <= cheek_hi(z) - m


def in_hatch(th0, th1, z0, z1):
    b, t, h = HATCH
    return th0 >= -h - 1e-6 and th1 <= h + 1e-6 and z0 >= b - 1e-6 and z1 <= t + 1e-6


def hidden(th0, th1, z0, z1):
    c = [(th0, z0), (th1, z0), (th0, z1), (th1, z1)]
    return in_hatch(th0, th1, z0, z1) or all(in_frame(*p) for p in c) or all(in_cheek(*p) for p in c)


# ----------------------------------------------------------------------------- the Body
def body(s):
    """The egg (sage, cream round the tip), its cream frame and side panels, the hatch's well with its hinges and the rail, the crown's bell."""
    bv = shell(s, COLS, ZS, lambda th, z: pal.Enamel_Cream if z < 0.45 else pal.Enamel_Sage, hidden)
    b, t, h = HATCH                                                   # the hatch's well: from the opening's rim back to a plate at y -0.035
    hz = [z for z in ZS if b <= z <= t]
    step = 360.0 / COLS
    hth = [-h + step * k for k in range(int(round(2 * h / step)) + 1)]
    rim = [(th, b) for th in hth] + [(h, z) for z in hz[1:]] + [(th, t) for th in hth[::-1][1:]] + [(-h, z) for z in hz[::-1][1:-1]]
    zc = 0.5 * (b + t)
    pts = [Egg.P(*q) for q in rim]
    n = len(pts)
    inner = [(p.x * 0.8, -0.035, zc + (p.z - zc) * 0.84) for p in pts]
    well = [[k, (k + 1) % n, n + (k + 1) % n, n + k] for k in range(n)] + [list(range(n, 2 * n))]
    bv += s._add([tuple(p) for p in pts] + inner, well, pal.Iron, False)
    weld(s, bv)
    ytop = Egg.P(0.0, t).y
    for x in (-0.075, 0.075):                                        # hinge knuckles under the opening's top edge
        s.sweep([(x - 0.016, ytop + 0.035, t - 0.012), (x + 0.016, ytop + 0.035, t - 0.012)], 0.0075, pal.Iron, seg=8)
    for x in (-0.02, 0.02):                                          # the rail the sidearm rides
        s.sweep([(x, -0.035, GADGET[2] - 0.005), (x, Egg.P(0.0, 0.536).y + 0.012, GADGET[2] - 0.005)], 0.005, pal.Iron, seg=6)
    m = 40                                                           # the cream frame round the face
    annulus(s, Egg, screen_edge, frame_edge, m, 2, pal.Enamel_Cream, t=0.006)
    zs = [0.415, 0.43, 0.455, 0.485, 0.52, 0.565, 0.61, 0.645, 0.675, 0.702, 0.722]
    for sg in (1, -1):                                               # the cream side panels, the arm's socket on their crest; meeting under the tip
        lo, hi = (cheek_lo, cheek_hi) if sg > 0 else (lambda z: -cheek_hi(z), lambda z: -cheek_lo(z))
        plate(s, Egg, rows_grid(zs, lo, hi, 6), pal.Enamel_Cream, t=0.005)
    s.lathe([(0.088, 1.066), (0.087, 1.077), (0.076, 1.09), (0.055, 1.101), (0.03, 1.109), (0.016, 1.112), (0.0, 1.113)], pal_chrome(), seg=20,
            cap_bottom=False)                                        # the chrome bell on the crown


def pal_chrome():
    return get("DK_Nickel_Satin")


def vent(s, sg):
    """A side vent: a chrome drum on the skin, a dark back and five round slats."""
    th, z = 92.0 * sg, 0.87
    with s.at(*frame_at(Egg.P(th, z), Egg.N(th, z))):
        s.lathe([(0.103, -0.042), (0.103, 0.026), (0.099, 0.034), (0.091, 0.037), (0.085, 0.033), (0.083, 0.024), (0.083, 0.012)], pal_chrome(), seg=16,
                cap_bottom=False, cap_top=False)
        s.lathe([(0.083, 0.012), (0.0, 0.012)], pal.Iron, seg=16, cap_bottom=False, cap_top=False)
        for k in range(5):
            y = -0.05 + 0.025 * k
            hl = math.sqrt(0.083 ** 2 - y * y) - 0.004
            s.sweep([(-hl, y, 0.021), (hl, y, 0.021)], 0.0075, pal_chrome(), seg=6)


# ----------------------------------------------------------------------------- parts
def ball_part(s):
    """Two chrome halves on an axle along x, the cream band raised round the equator, a chrome hub plate on each pole."""
    R, w, e = BALL_R, 0.057, 0.0025
    Rc = R - e                                                       # the chrome halves sit just under the band, which carries the robot
    pw = math.acos(w / Rc)
    half = [(Rc * math.sin(pw * k / 6), -Rc * math.cos(pw * k / 6)) for k in range(7)]
    with s.part("Ball", pivot=(0.0, 0.0, R)) as b:
        with b.at((0.0, 0.0, R), (0.0, 90.0, 0.0)):                 # local z = the axle (Blender x)
            b.lathe(half, pal.Iron, seg=24, cap_bottom=False, cap_top=False)
            b.lathe([(x, -z) for x, z in half[::-1]], pal.Iron, seg=24, cap_bottom=False, cap_top=False)
            rb = math.sqrt(R * R - w * w)
            band = [(half[-1][0], -w), (rb, -w), (R, 0.0), (rb, w), (half[-1][0], w)]
            b.lathe(band, pal.Enamel_Cream, seg=24, cap_bottom=False, cap_top=False)
        for rot in (0.0, 180.0):                                     # the hubs: a raised stadium on each pole, long fore and aft
            with b.at((0.0, 0.0, R), (0.0, 0.0, rot)):
                L, H = 0.118, 0.05                                   # a rounded stadium: the unit square onto a superellipse, 8 x 2 quads
                grid = [[(L * x, H * y) for x, y in (squircle(i / 8, j / 2, 2.6) for i in range(9))] for j in range(3)]
                plate(b, Cap(Rc), grid, pal_chrome(), t=0.009)


def hatch(s):
    """The hatch: a sage slab over the opening, three cream slats, hinged along its top edge."""
    b, t, h, th = PANEL
    hinge = Egg.P(0.0, t) + Egg.N(0.0, t) * th
    rc, rz = 7.0, 0.02
    corner = lambda z: rc * (1 - math.sqrt(max(0.0, 1 - (max(0.0, b + rz - z, z - (t - rz)) / rz) ** 2)))  # noqa: E731
    zs = [b, 0.505, 0.525, 0.56, 0.6, 0.64, 0.66, t]
    with s.part("ChestPanel", pivot=tuple(hinge)) as p:
        plate(p, Egg, rows_grid(zs, lambda z: -h + corner(z), lambda z: h - corner(z), 6), pal.Enamel_Sage, t=th, back=-0.004)
        for z, half in ((0.636, 0.0875), (0.600, 0.0725), (0.564, 0.06)):   # the slats, longest on top (front view: 0.175, 0.145, 0.12 m)
            tm = math.degrees(math.asin(half / Egg.P(90.0, z).x))
            path = [Egg.P(-tm + 2 * tm * i / 6, z) + Egg.N(-tm + 2 * tm * i / 6, z) * (th + 0.0058) for i in range(7)]
            p.sweep([tuple(v) for v in path], [0.0035, 0.0078] + [0.0088] * 3 + [0.0078, 0.0035], pal.Enamel_Cream, seg=6)


def gadget(s):
    """The sidearm, chunky and round-nosed: a cream body on a dark shoe on the rail, a dark cowl with a chrome knob, a finned chrome barrel with a dark bore
    (cream as on the details sheet, so it reads against the dark well)."""
    x, y, z = GADGET
    with s.part("Gadget", pivot=GADGET) as g:
        g.rbox((-0.026, y - 0.06, z), (0.026, y + 0.003, z + 0.012), 0.004, pal.Iron, seg=1)
        g.rbox((-0.034, y - 0.063, z + 0.006), (0.034, y + 0.005, z + 0.058), 0.014, pal.Enamel_Cream, seg=1)
        g.rbox((-0.022, y - 0.055, z + 0.052), (0.022, y - 0.007, z + 0.066), 0.007, pal.Iron, seg=1)
        with g.at((0.0, y - 0.022, z + 0.063)):
            g.lathe([(0.011, 0.0), (0.011, 0.006), (0.007, 0.011), (0.0, 0.012)], pal_chrome(), seg=10, cap_bottom=False)
        with g.at((0.0, y - 0.063, z + 0.029), (90.0, 0.0, 0.0)):  # local z = forward (-y)
            g.lathe([(0.0145, -0.004), (0.0145, 0.004), (0.018, 0.006), (0.018, 0.011), (0.0135, 0.013), (0.0135, 0.019), (0.018, 0.021), (0.018, 0.026),
                     (0.0135, 0.028), (0.0135, 0.033), (0.0165, 0.035), (0.0165, 0.044), (0.0095, 0.045), (0.0095, 0.04)], pal_chrome(), seg=12,
                    cap_bottom=False, cap_top=False)
            g.lathe([(0.0095, 0.04), (0.0, 0.04)], pal.Iron, seg=12, cap_bottom=False, cap_top=False)


def arm(s, sg):
    """A short cream arm from a chrome collar, an elbow ball, a cuffed mitten with its thumb up. sg = +1 the robot's left (Blender +x)."""
    tag = "L" if sg > 0 else "R"
    down, fwd = (math.radians(a) for a in ARM)
    d = Vector((sg * math.cos(down) * math.cos(fwd), -math.cos(down) * math.sin(fwd), -math.sin(down)))
    S = Egg.P(sg * SHOULDER[0], SHOULDER[1])
    E = S + d * 0.13
    loc, rot = frame_at(S, d, HINT)
    with s.part(f"Arm{tag}", pivot=tuple(S)) as a, a.at(loc, rot):
        a.lathe([(0.036, -0.03), (0.037, 0.0), (0.034, 0.014), (0.027, 0.022)], pal_chrome(), seg=12, cap_bottom=False)
        a.lathe([(0.022, 0.012), (0.03, 0.022), (0.033, 0.045), (0.032, 0.1), (0.027, 0.117), (0.019, 0.123)], pal.Enamel_Cream, seg=12, cap_bottom=False)
    with s.part(f"Forearm{tag}", pivot=tuple(E)) as f, f.at(loc, rot):
        ball(f, 0.024, pal_chrome(), seg=10, n=5, z=0.13)
        f.lathe([(0.021, 0.143), (0.029, 0.149), (0.031, 0.161), (0.027, 0.168)], pal.Enamel_Cream, seg=12, cap_bottom=False, cap_top=False)
        with f.at((0.0, 0.0, 0.205), scale=(0.044, 0.026, 0.055)):
            ball(f, 1.0, pal.Enamel_Cream, seg=12, n=7)
        hint = Vector(HINT).normalized()
        up = 1.0 if (hint - d * d.dot(hint)).normalized().cross(d).z >= 0 else -1.0  # the thumb on the upper side of the hand (local x = y cross z)
        f.sweep([(up * 0.03, -0.003, 0.17), (up * 0.043, -0.006, 0.192), (up * 0.05, -0.008, 0.212)], [0.0135, 0.0128, 0.0115], pal.Enamel_Cream, seg=8)
        with f.at((up * 0.05, -0.008, 0.212)):
            ball(f, 0.0115, pal.Enamel_Cream, seg=8, n=4)


def halo(s):
    """The crown's chrome post with its ring and ball tip."""
    with s.part("Halo", pivot=HALO) as h:
        h.lathe([(0.0125, 1.1), (0.0115, 1.245)], pal_chrome(), seg=10)
        h.lathe([(0.0115, 1.182), (0.019, 1.186), (0.019, 1.194), (0.0115, 1.198)], pal_chrome(), seg=10, cap_bottom=False, cap_top=False)
        h.lathe([(0.0115, 1.226), (0.017, 1.231), (0.0115, 1.236)], pal_chrome(), seg=10, cap_bottom=False, cap_top=False)
        ring = [(0.186 * math.cos(2 * math.pi * k / 28), 0.186 * math.sin(2 * math.pi * k / 28), 1.19) for k in range(29)]
        h.sweep(ring, 0.0085, pal_chrome(), seg=5, caps=False)
        ball(h, 0.034, pal_chrome(), seg=12, n=6, z=1.262)


def screen(s):
    """The face glass in the frame's recess, UVs 0..1 across it."""
    zc, th, hh, n = SCREEN
    centre = Egg.P(0.0, zc)

    def at(u, v):
        x, y = squircle(u, v, n)
        return Egg.P(th * x, zc + hh * y) + Egg.N(th * x, zc + hh * y) * 0.0015
    with s.part("Screen", pivot=tuple(centre)) as g:
        g.sheet(at, 14, 8, get("DK_PatrolBot104_Screen"))


@asset("PatrolBot104", "Robots", bevel=0)
def patrol_bot_104(s):
    """The ball-balancing patrol robot of brief 104 (module docstring)."""
    s.pivot = (0.0, 0.0, CONTACT)
    body(s)
    for sg in (1, -1):
        vent(s, sg)
        arm(s, sg)
    ball_part(s)
    hatch(s)
    gadget(s)
    halo(s)
    screen(s)

