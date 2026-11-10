"""Brief 104 (Vela, 2026-11-10; Sky picked the ball-balancer, then "model it"): the patrol robot `PatrolBot104`, modelled from the committed sheets
briefs/104/concepts/patrol-robot-turnaround.png (front, left, back, right, top), patrol-robot-details.png (faces, the chest hatch and its rail) and
patrol-robot-chest-sidearm.png (closed, opening, open). A sage-green and cream enamel egg balancing on a ball: two warm gunmetal halves with a cream band and a
chrome hub on each pole; a dark glass face in a cream frame; round chrome vents with slats on both sides; a 1950s police beacon on the crown (Sky, 2026-11-10:
"a siren light on his head instead of antenna"; it replaced the halo ring); two short cream arms with mitten hands; a thick hatch of three cream slats under
the chin on two barrel hinges, over a well with a chrome rim, ribs, an amber lamp and the rail the sidearm slides on.
1.27 m to the beacon's knob, 0.89 m across the hands (the square's dock bay is 0.98 x 1.0 x 2.14 m).

Rigid parts, no skin: each object stands at its own pivot (geo.Shape.part pivot=), so code rolls, leans, hinges and slides them. Exported siblings, as every kit piece:
  DK_PatrolBot104           Body: the egg, its plates, the vents, the crown's chrome bell, the hatch's well (rim, ribs, lamp, rail) and the hinges' fixed knuckles.
                            Pivot: the ball contact (0, 0, 0.40); it leans and turns about it. Every other part follows the Body except the Ball.
  DK_PatrolBot104_Ball      pivot at its centre (0, 0, 0.20); rolls in code. Axle along x (its hubs at the sides), the cream band round its equator.
  DK_PatrolBot104_ChestPanel  the hatch, hinged on the hinge line at its top edge (axis along x); open = about -150 deg about x (the bottom swings forward and up).
  DK_PatrolBot104_Gadget    the sidearm on its rail, pivot at its rear on the rail; out = 0.11 m along -y (Blender front; kit +z).
  DK_PatrolBot104_ArmL / _ArmR        shoulder pivot (L = the robot's own left, Blender +x, kit -x).
  DK_PatrolBot104_ForearmL / _ForearmR  the hand from the elbow ball, pivot at that ball's centre.
  DK_PatrolBot104_Beacon    the siren: chrome base, red glass dome, chrome cage and knob; pivot at its foot on the crown (0, 0, 1.105). The game lights its glass.
  DK_PatrolBot104_BeaconLamp  the mirror and bulb inside the dome; pivot on the vertical axis (0, 0, 1.178), spins about it (kit y). Seen only through a see-through dome.
  DK_PatrolBot104_Screen    the face glass, UVs 0..1 across the visible glass (u to the robot's left as you face it, v up); pivot at its centre.
Materials (shared, no per-robot block): DK_RobotEnamel_Sage (the sheets' green), DK_RobotEnamel_Cream, DK_Nickel_Satin (civic_common: the civic chrome that keeps a
lit and a shaded side in the zone), DK_Gunmetal_Warm (new: the ball's halves, the well and the hatch's inner face), DK_Iron (the sidearm's shoe and cowl, the vents'
backs, the rail's blocks), DK_Glow_Amber (the well's lamp, as robot_parts.py), DK_PatrolBot104_Beacon (new: the beacon's red glass and bulb, emission off as built,
driven by the game), DK_PatrolBot104_Screen (the blank glass)."""

import math

from mathutils import Matrix, Vector

import civic_common  # noqa: F401  (registers DK_Nickel_Satin)
from kit import asset
from mats import RobotPal, get, register
from patrolbot104_surf import Cap, Egg, _smooth, annulus, plate, rows_grid, shell, squircle, superellipse, weld
from signals import ball

register("DK_Enamel_Sage", "enamel", "#7E8C54")      # the sheets' sage green (lit body #A8A982 on the turnaround's back view, a touch darker as albedo)
register("DK_PatrolBot104_Screen", "enamel", "#191613")  # the blank dark glass; the six faces are proposed as materials beside it (briefs/104/vela/patrolbot/notes.md)
register("DK_PatrolBot104_Beacon", "emit", "#C4231A", 0.0)  # the siren's red glass (emit kind: glossy); emission 0 as built, the game drives it (as DK_Eye_Lens)
register("DK_Gunmetal_Warm", "brass", "#6F675D")        # warm satin gunmetal (brass kind keeps a diffuse share: holds its colour in shade, doesn't wash pale as nickel did)
register("DK_Glow_Amber", "emit", "#F28A1E", 1.1)       # as robot_parts.py (register is first-wins)
pal = RobotPal()

BALL_R = 0.20                      # the ball: 0.41 m on the sheet
CONTACT = 2 * BALL_R               # the Body's pivot, where the egg's tip rests on the ball
SCREEN = (0.91, 55.0, 0.12, 3.6)   # the glass: centre height, half-angle (deg), half-height (m), superellipse exponent
FRAME = (64.0, 0.142, 0.23, 3.0, 2.4)  # the cream frame round it: half-angle, half-height up and down from the glass centre, exponents above and below
COLS = 36                          # the skin's columns, 10 deg apart from the back (-180)
HATCH = (0.50, 0.665, 40.0)        # the hatch's opening in the skin: bottom, top, half-angle (on the skin's columns)
PANEL = (0.497, 0.668, 42.0, 0.009, -0.016)  # the hatch's slab: bottom, top (the hinge), half-angle, face proud of the skin, back under it (25 mm thick)
GADGET = (0.0, -0.045, 0.541)      # the sidearm's pivot: its rear, on the rail
SHOULDER = (90.0, 0.70)            # (theta, z) of the shoulders on the skin
ARM = (42.0, 18.0)                 # arms: degrees below the horizontal, degrees toward the front
HINT = (0.0, -1.0, 0.35)           # the palms face forward and a little up
BEACON = (0.0, 0.0, 1.105)         # the beacon's foot on the crown's bell
LAMP = (0.0, 0.0, 1.178)           # the spinning mirror's pivot, on the vertical axis (its middle, at the beacon's scale)
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
    bv += s._add([tuple(p) for p in pts] + inner, well, gunmetal(), False)
    weld(s, bv)
    well_detail(s, pts, inner)
    m = 36                                                           # the cream frame round the face
    annulus(s, Egg, screen_edge, frame_edge, m, 2, pal.Enamel_Cream, t=0.006)
    zs = [0.415, 0.43, 0.455, 0.485, 0.52, 0.565, 0.61, 0.645, 0.675, 0.702, 0.722]
    for sg in (1, -1):                                               # the cream side panels, the arm's socket on their crest; meeting under the tip
        lo, hi = (cheek_lo, cheek_hi) if sg > 0 else (lambda z: -cheek_hi(z), lambda z: -cheek_lo(z))
        plate(s, Egg, rows_grid(zs, lo, hi, 6), pal.Enamel_Cream, t=0.005)
    s.lathe([(0.088, 1.066), (0.087, 1.077), (0.076, 1.09), (0.055, 1.101), (0.0, 1.104)], pal_chrome(), seg=20,
            cap_bottom=False)                                        # the chrome bell on the crown; its top is under the beacon's foot


def pal_chrome():
    return get("DK_Nickel_Satin")


def gunmetal():
    return get("DK_Gunmetal_Warm")


def hinge_line():
    """A point on the hatch's hinge line (it runs along x): the slab's outer face at its top edge, the ChestPanel's pivot."""
    b, t, h, th, back = PANEL
    return Egg.P(0.0, t) + Egg.N(0.0, t) * th


HINGE_BODY = ((-0.098, -0.072), (-0.022, 0.022), (0.072, 0.098))   # the Body's knuckles on the hinge line; the panel's sit between them
HINGE_PANEL = ((-0.071, -0.023), (0.023, 0.071))


def well_detail(s, rim, inner):
    """The well as a mechanism: a chrome lip round the opening, two ribs on the back plate each carrying an amber lamp (below the open panel, so the play camera
    sees them), the nickel rail on its blocks, and the Body's knuckles of the two barrel hinges."""
    c = sum(rim, Vector()) / len(rim)
    lip = [p for k, p in enumerate(rim) if k % 2 == 0 or (rim[k - 1] - p).normalized().dot((rim[(k + 1) % len(rim)] - p).normalized()) > -0.95]  # every other point, keeping the corners
    s.sweep([tuple(p + (c - p).normalized() * 0.003) for p in lip + lip[:1]], 0.0042, pal_chrome(), seg=5, caps=False)
    yb = inner[0][1] - 0.002                                         # just proud of the back plate
    zb = [q[2] for q in inner]
    lo, hi = min(zb) + 0.012, max(zb) - 0.012
    for x in (-0.066, 0.066):                                        # ribs either side of the rail, a lamp on each: a nickel bezel, an amber lens (local z = -y)
        s.sweep([(x, yb + 0.002, lo), (x, yb - 0.004, lo + 0.008), (x, yb - 0.004, hi - 0.008), (x, yb + 0.002, hi)], 0.0055, pal_chrome(), seg=6)
        with s.at((x, yb - 0.004, 0.552), (90.0, 0.0, 0.0)):
            s.lathe([(0.0145, -0.002), (0.0145, 0.006), (0.0115, 0.0075)], pal_chrome(), seg=10, cap_bottom=False, cap_top=False)
            s.lathe([(0.0115, 0.0075), (0.0095, 0.0125), (0.005, 0.0155), (0.0, 0.0165)], get("DK_Glow_Amber"), seg=10, cap_bottom=False)
    z = GADGET[2] - 0.005
    yf = Egg.P(0.0, 0.536).y + 0.012
    s.rbox((-0.032, yb - 0.016, z - 0.012), (0.032, yb + 0.002, z + 0.004), 0.004, pal.Iron, seg=1)  # the rail's rear block on the back plate
    for x in (-0.02, 0.02):                                          # the rail, nickel so it reads in the well
        s.sweep([(x, yb - 0.01, z), (x, yf, z)], 0.005, pal_chrome(), seg=6)
    s.rbox((-0.028, yf - 0.002, z - 0.011), (0.028, yf + 0.012, z + 0.003), 0.003, pal.Iron, seg=1)   # its front stop at the rim
    hl = hinge_line()
    for x0, x1 in HINGE_BODY:
        s.sweep([(x0, hl.y, hl.z), (x1, hl.y, hl.z)], 0.0075, pal_chrome(), seg=8)


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
    """Two warm gunmetal halves on an axle along x, the cream band raised round the equator, a chrome hub plate on each pole."""
    R, w, e = BALL_R, 0.057, 0.0025
    Rc = R - e                                                       # the chrome halves sit just under the band, which carries the robot
    pw = math.acos(w / Rc)
    half = [(Rc * math.sin(pw * k / 6), -Rc * math.cos(pw * k / 6)) for k in range(7)]
    with s.part("Ball", pivot=(0.0, 0.0, R)) as b:
        with b.at((0.0, 0.0, R), (0.0, 90.0, 0.0)):                 # local z = the axle (Blender x)
            b.lathe(half, gunmetal(), seg=20, cap_bottom=False, cap_top=False)
            b.lathe([(x, -z) for x, z in half[::-1]], gunmetal(), seg=20, cap_bottom=False, cap_top=False)
            rb = math.sqrt(R * R - w * w)
            band = [(half[-1][0], -w), (rb, -w), (R, 0.0), (rb, w), (half[-1][0], w)]
            b.lathe(band, pal.Enamel_Cream, seg=20, cap_bottom=False, cap_top=False)
        for rot in (0.0, 180.0):                                     # the hubs: a raised stadium on each pole, long fore and aft
            with b.at((0.0, 0.0, R), (0.0, 0.0, rot)):
                L, H = 0.118, 0.05                                   # a rounded stadium: the unit square onto a superellipse, 8 x 2 quads
                grid = [[(L * x, H * y) for x, y in (squircle(i / 8, j / 2, 2.6) for i in range(9))] for j in range(3)]
                plate(b, Cap(Rc), grid, pal_chrome(), t=0.009)


def hatch(s):
    """The hatch: a 25 mm sage slab over the opening, three cream slats, its knuckles on the hinge line; inside, a gunmetal liner with two nickel stiffeners."""
    b, t, h, th, back = PANEL
    hinge = hinge_line()
    rc, rz = 7.0, 0.02
    corner = lambda z: rc * (1 - math.sqrt(max(0.0, 1 - (max(0.0, b + rz - z, z - (t - rz)) / rz) ** 2)))  # noqa: E731
    zs = [b, 0.505, 0.525, 0.56, 0.6, 0.64, 0.66, t]
    with s.part("ChestPanel", pivot=tuple(hinge)) as p:
        plate(p, Egg, rows_grid(zs, lambda z: -h + corner(z), lambda z: h - corner(z), 6), pal.Enamel_Sage, t=th, back=back)
        li, z0, z1 = 0.8 * h, b + 0.014, t - 0.016                   # the inner face: a gunmetal liner inset on the slab's back, facing into the well

        def liner(u, v, off=0.0):
            th_, z_ = li - 2 * li * u, z0 + (z1 - z0) * v
            return Egg.P(th_, z_) + Egg.N(th_, z_) * (back - 0.0015 - off)
        p.sheet(liner, 6, 4, gunmetal())
        for u in (0.22, 0.78):                                       # two stiffeners down the liner
            p.sweep([tuple(liner(u, v, 0.004)) for v in (0.06, 0.5, 0.94)], 0.005, pal_chrome(), seg=6)
        for x0, x1 in HINGE_PANEL:                                   # the panel's knuckles on the hinge line
            p.sweep([(x0, hinge.y, hinge.z), (x1, hinge.y, hinge.z)], 0.0075, pal_chrome(), seg=8)
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
                     (0.0135, 0.028), (0.0135, 0.033), (0.0165, 0.035), (0.0165, 0.044), (0.0095, 0.045), (0.0095, 0.04)], pal_chrome(), seg=10,
                    cap_bottom=False, cap_top=False)
            g.lathe([(0.0095, 0.04), (0.0, 0.04)], pal.Iron, seg=10, cap_bottom=False, cap_top=False)


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


K = 1.12                           # the beacon's scale about its foot (drawn at 1.0, enlarged so it reads at play zoom)
DOME = [(0.066, 1.131), (0.068, 1.15), (0.068, 1.18), (0.064, 1.2), (0.054, 1.218), (0.038, 1.231), (0.018, 1.238), (0.0, 1.24)]  # the glass, (r, z) at scale 1


def bk(prof):
    """A beacon profile (r, z) at scale K about the beacon's foot."""
    return [(K * r, BEACON[2] + K * (z - BEACON[2])) for r, z in prof]


def beacon(s):
    """The siren: a stepped chrome base on the crown's bell, a red glass dome, a four-bar chrome cage and a knob; inside, a mirror and bulb that spin."""
    glass = get("DK_PatrolBot104_Beacon")
    with s.part("Beacon", pivot=BEACON) as b:
        b.lathe(bk([(0.05, 1.095), (0.07, 1.105), (0.074, 1.112), (0.078, 1.118), (0.078, 1.127), (0.072, 1.132), (0.064, 1.132)]), pal_chrome(), seg=16,
                cap_bottom=False, cap_top=False)                     # foot sunk in the crown's bell
        b.lathe(bk(DOME), glass, seg=16, cap_bottom=False)
        b.lathe(bk([(0.014, 1.236), (0.019, 1.24), (0.019, 1.245), (0.012, 1.251), (0.0, 1.253)]), pal_chrome(), seg=10, cap_bottom=False)
        for k in range(4):                                           # the cage: four bars from the base's rim over the dome to the knob
            a = math.pi / 4 + k * math.pi / 2
            path = [(0.074, 1.128)] + [(r + 0.006, z + 0.002) for r, z in DOME[1:6]] + [(0.017, 1.243)]
            b.sweep([(r * math.cos(a), r * math.sin(a), z) for r, z in bk(path)], 0.0045, pal_chrome(), seg=5, caps=False)
    with s.part("BeaconLamp", pivot=LAMP) as m:                      # a curved mirror behind a bulb, both turning about the vertical axis
        m.lathe(bk([(0.0, 1.135), (0.012, 1.14), (0.016, 1.165), (0.014, 1.185), (0.0, 1.195)]), glass, seg=8)

        def mirror(u, v, r=0.05):
            a = math.radians(-75 + 150 * u)
            return (K * r * math.sin(a), K * r * math.cos(a), BEACON[2] + K * (1.14 + 0.05 * v - BEACON[2]))
        m.sheet(mirror, 6, 1, pal_chrome())
        m.sheet(lambda u, v: mirror(1 - u, v, 0.052), 6, 1, pal_chrome())


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
    beacon(s)
    screen(s)

