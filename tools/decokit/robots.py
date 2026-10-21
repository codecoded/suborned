"""The city's service robots (props sheet: service robots 1-3, 5, 6) - key characters, so each is a personality
first: a dignified butler in a tailcoat, a keen porter in a pillbox cap, a TV-headed waiter, a doting nanny in an
apron, a shopkeeper waving under a striped awning. Faces are dot-matrix displays (robot_parts.display). Original
designs in the kit livery: cream and ivory enamel, seafoam lower bodies, chrome joints, coral accents, glowing
amber and teal displays. Front at -Y, resting on z = 0 (hover bases glow teal underneath). Each head
is a movable part (geo.Shape.part) that turns on its neck (RobotIdle), carrying a Face screen the engine draws."""

import math

from geo import dome, fillet, rrect
from kit import asset
from mats import RobotPal

pal = RobotPal()  # enamels as their detailed robot variants (mats.py)
from robot_parts import (antenna, arm, badge, bowtie, bud_vase, cloche, cup, display, emblem, glass, heart, hose,
                         hover_disc, lights, panel, pennant, profile_rad, rivets, screen, shoulder, stud, surf, teapot,
                         towel, tray, vents, vseam)
from signals import ball, band, puck, soft
from vehicles_parts import lamp, patch, wheel


# ----------------------------------------------------------------- butler

BT_SKIRT = [(0.21, 0.15), (0.22, 0.4), (0.21, 0.7)]
BT_TORSO = [(0.215, 0.74), (0.235, 0.9), (0.24, 1.1), (0.225, 1.22), (0.17, 1.3), (0.07, 1.33), (0, 1.335)]
BT_HEAD = [(0.14 * math.cos(t), 1.56 + (0.13 if t < 0 else 0.175) * math.sin(t))
           for t in (math.pi * (k / 20 - 0.5) for k in range(21))]  # an egg: fuller below the brow


@asset("RobotButler", "Robots", bevel=0, lod=4000)
def robot_butler(s):
    """Tall butler on a hover saucer: seafoam skirt, an ivory shirt front under a black enamel tailcoat with tails,
    bow tie, gold buttons and watch chain, a coral pocket square; an egg head with a display of calm half-lidded
    amber eyes; a tea tray on one hand, a towel over the other forearm."""
    hover_disc(s, 0.4, 0.06, 0.17, lamps=12)
    s.lathe(soft(BT_SKIRT, 0.02), pal.Enamel_Seafoam, seg=24, cap_bottom=False, cap_top=False)
    sk = profile_rad(BT_SKIRT)
    vents(s, -0.08, 0.08, -0.215, 0.3, 4)
    for a in (-35, -145):
        vseam(s, sk, 0.2, 0.68, a)
    rivets(s, sk, 0.665, 12, r=0.007)
    band(s, 0.21, 0.232, 0.69, 0.75, pal.Chrome, seg=24)
    s.lathe(soft(BT_TORSO, 0.04), pal.Enamel_Ivory, seg=24, cap_bottom=False)
    tr = profile_rad(BT_TORSO)
    # tailcoat: black enamel cut away at the waist, lapels closing to a V at the chest, chrome silk edges
    cut = profile_rad([(58, 0.78), (40, 0.95), (22, 1.1), (27, 1.24)])  # half-opening at the front, deg
    panel(s, lambda z: tr(z) + 0.001, [0.78 + 0.0354 * k for k in range(14)], lambda z: (-90 + cut(z), 270 - cut(z)),
          0.008, pal.Bakelite, n=24)
    for sg in (-1, 1):
        s.sweep([tuple(surf(tr, z, -90 + sg * cut(z), 0.006)[0]) for z in (0.78 + 0.046 * k for k in range(11))],
                0.004, pal.Chrome, seg=6)
    for a0, a1 in ((52, 86), (94, 128)):
        patch(s, lambda z: 0.228, a0, a1, [0.42, 0.52, 0.62, 0.72, 0.8], 0.008, pal.Bakelite, n=8)
    for z in (0.98, 1.06, 1.14):
        stud(s, *surf(tr, z, -90), 0.012, pal.Gold)
    chain = [surf(tr, 1.06 - 0.06 * math.sin(math.pi * k / 8), -90 + 36 * k / 8, 0.004)[0] for k in range(9)]
    s.sweep([tuple(p) for p in chain], 0.0028, pal.Gold, seg=6)  # watch chain to the pocket
    p, n = surf(tr, 1.15, -40, 0.009)
    emblem(s, p, n, [(-0.03, -0.008), (0.03, -0.008), (0.022, 0.02), (0.008, 0.042), (-0.004, 0.022),
                     (-0.02, 0.034)], pal.Enamel_Coral)  # pocket square
    bowtie(s, *surf(tr, 1.27, -90, 0.014), 0.11, pal.Bakelite)
    for z in (1.33, 1.37, 1.41):
        band(s, 0.04, 0.052, z, z + 0.025, pal.Chrome, seg=12)
    with s.part("Head") as h:  # turns on the neck
        h.lathe(BT_HEAD, pal.Enamel_Ivory, seg=28)
        face = display(h, profile_rad(BT_HEAD), 1.6, 0.2, 0.075)
        with h.part("Face") as f:
            screen(f, face, 0.19, 0.066, 1.6, pal.Face_Amber)
        for sg in (-1, 1):
            with h.at((sg * 0.137, 0, 1.56), (0, sg * 90, 0)):
                h.lathe(puck(0.03, -0.005, 0.012, 0.005), pal.Chrome, seg=10)
            antenna(h, (sg * 0.05, 0.0, 1.72), 0.12, (sg * 0.02, 0.0))
    for sg in (-1, 1):
        shoulder(s, (sg * 0.245, 0, 1.18), sg, 0.055)
    arm(s, [(0.27, 0, 1.18), (0.32, -0.05, 0.97), (0.26, -0.32, 1.0)], curl=0.4)
    top = tray(s, (0.2, -0.46, 1.04))
    teapot(s, (0.15, -0.43, top))
    cup(s, (0.27, -0.52, top))
    arm(s, [(-0.27, 0, 1.18), (-0.32, -0.02, 0.96), (-0.27, -0.24, 0.92)], curl=0.8)
    towel(s, (-0.293, -0.13, 0.936), (0.05, -0.22, -0.04))


# ----------------------------------------------------------------- porter

PT_BARREL = [(0.3, 0.35), (0.4, 0.43), (0.44, 0.56), (0.45, 0.7), (0.435, 0.84), (0.41, 0.91)] + dome(0.41, 0.91, 0.27, 8)[1:]
PT_NECK = 5  # the barrel/dome seam, under the upper chrome band


@asset("RobotPorter", "Robots", bevel=0, lod=4000)
def robot_porter(s):
    """Broad barrel porter on rubber treads: a display on the dome of wide amber eyes and a grin, a coral
    pillbox cap worn at an angle, riveted bands, a pennant; bellows arms on a trolley of suitcases and a hatbox."""
    for sg in (-1, 1):
        s.rbox((sg * 0.31 - 0.1, -0.44, 0.0), (sg * 0.31 + 0.1, 0.44, 0.28), 0.13, pal.Rubber)
        for k in range(15):  # track cleats across the tread's top
            y = -0.36 + 0.72 * k / 14
            s.box((sg * 0.31 - 0.1, y - 0.012, 0.27), (sg * 0.31 + 0.1, y + 0.012, 0.29), pal.Rubber)
        for y in (-0.28, -0.093, 0.093, 0.28):
            wheel(s, (sg * 0.41, y, 0.14), 0.085, 0.03, rin=0.055, hub=0.5, seg=10, side=sg)
        hose(s, [(sg * 0.24, 0.3, 0.5), (sg * 0.33, 0.4, 0.42), (sg * 0.25, 0.36, 0.32)], 0.02)
    s.rbox((-0.22, -0.34, 0.12), (0.22, 0.34, 0.38), 0.06, pal.Enamel_Seafoam)
    s.rbox((-0.21, -0.37, 0.14), (0.21, -0.33, 0.19), 0.018, pal.Chrome)  # bumper
    for x in (-0.1, 0.0, 0.1):
        lamp(s, (x, -0.34, 0.27), (0, -1, 0), 0.015, lens=pal.Signal_Amber, depth=0.01, seg=8)
    s.lathe(soft(PT_BARREL[:PT_NECK + 1], 0.03), pal.Enamel_Ivory, seg=32, cap_top=False)
    br = profile_rad(PT_BARREL)
    band(s, 0.448, 0.466, 0.64, 0.71, pal.Chrome, seg=32)
    band(s, 0.408, 0.422, 0.895, 0.925, pal.Chrome, seg=32)
    rivets(s, br, 0.6, 16, r=0.007)
    rivets(s, br, 0.75, 16, r=0.007)
    for a in (-10, 30, 70, 110, 150, 190):
        vseam(s, br, 0.44, 0.59, a)
        vseam(s, br, 0.76, 0.89, a)
    with s.part("Head") as h:  # the dome turns like a turret under the band
        h.lathe(PT_BARREL[PT_NECK:], pal.Enamel_Ivory, seg=32, cap_bottom=False)
        lights(h, br, 1.085, 7, -30, 210, r=0.015)
        face = display(h, br, 1.02, 0.34, 0.15, cr=0.03)  # wide eyes and a grin on the dome
        with h.part("Face") as f:
            screen(f, face, 0.33, 0.14, 1.02, pal.Face_Amber)
        with h.at((0.02, 0.0, 1.143), (0, 6, 0)):  # porter's pillbox cap, worn at a jaunty angle
            h.lathe(soft([(0, 0), (0.15, 0.0), (0.155, 0.02), (0.15, 0.1), (0, 0.105)], 0.01), pal.Enamel_Coral, seg=24)
            band(h, 0.153, 0.161, 0.018, 0.045, pal.Gold, seg=24)
            with h.at((0, -0.14, 0.012), (10, 0, 0)):
                h.extrude([(0.1 * math.cos(t), 0.075 * math.sin(t)) for t in (math.pi + math.pi * k / 12 for k in range(13))],
                          0.0, 0.012, pal.Bakelite, smooth_sides=True)  # peak
            badge(h, (0, -0.156, 0.068), (0, -1, 0), 0.024, face=pal.Enamel_Teal)
        pennant(h, (0.14, 0.3, 1.0), 0.55, pal.Enamel_Coral, lean=(0.02, 0.04))
    for sg in (-1, 1):  # shoulder pucks, bellows arms reaching to the trolley handle
        shoulder(s, (sg * 0.445, -0.02, 0.78), sg, 0.1)
        p0, p1 = (sg * 0.5, -0.05, 0.78), (sg * 0.26, -0.64, 0.74)
        hose(s, [p0, ((p0[0] + p1[0]) / 2 + sg * 0.03, -0.35, 0.8), p1], 0.048, n=18)
        s.sweep([p1, (sg * 0.25, -0.67, 0.74)], 0.04, pal.Chrome, seg=10)
        with s.at((sg * 0.25, -0.66, 0.74), (90, 0, 0)):
            s.lathe(puck(0.045, -0.02, 0.02, 0.01), pal.Chrome, seg=10)  # the clamp on the handle
    # trolley: chrome frame, deck on casters, a red and a cream suitcase with stickers and a tag, a hatbox
    Y0 = -0.66
    H = [(-0.28, Y0 + 0.02, 0.18), (-0.28, Y0, 0.74), (0.28, Y0, 0.74), (0.28, Y0 + 0.02, 0.18)]
    s.sweep(fillet(H, 0.06, n=4, closed=False), 0.016, pal.Chrome, seg=8)
    s.rbox((-0.3, Y0 - 0.64, 0.14), (0.3, Y0 + 0.04, 0.18), 0.015, pal.Steel)
    for x in (-0.25, 0.25):
        for y in (Y0 - 0.57, Y0 - 0.04):
            wheel(s, (x, y, 0.07), 0.065, 0.035, rin=0.04, hub=0.5, seg=10)
    s.rbox((-0.26, Y0 - 0.6, 0.18), (0.26, Y0 - 0.12, 0.44), 0.03, pal.Enamel_Red)
    s.rbox((-0.2, Y0 - 0.54, 0.44), (0.2, Y0 - 0.2, 0.62), 0.025, pal.Enamel_Cream)
    yc = Y0 - 0.36
    for z, w in ((0.44, 0.26), (0.62, 0.2)):
        s.sweep(fillet([(-0.06, yc, z - 0.002), (-0.06, yc, z + 0.035), (0.06, yc, z + 0.035),
                        (0.06, yc, z - 0.002)], 0.015, n=3, closed=False), 0.008, pal.Chrome, seg=6)
        s.box((-w - 0.004, yc - 0.06, z - 0.1), (w + 0.004, yc - 0.03, z - 0.02), pal.Leather_Black)  # strap
    for (y, z, r, m) in ((Y0 - 0.48, 0.34, 0.045, pal.Enamel_Teal), (Y0 - 0.3, 0.26, 0.035, pal.Enamel_Mustard),
                         (Y0 - 0.2, 0.36, 0.03, pal.Enamel_Ivory)):  # travel stickers on the red case's flank
        with s.at((0.261, y, z), (0, 90, 0)):
            s.lathe(puck(r, 0.0, 0.003, 0.001), m, seg=14)
    s.sweep([(-0.03, yc, 0.475), (-0.05, yc - 0.01, 0.43), (-0.055, yc - 0.012, 0.41)], 0.002, pal.Iron, seg=4)
    with s.at((-0.055, yc - 0.012, 0.37), (0, 0, 8)):
        s.rbox((-0.022, -0.004, -0.04), (0.022, 0.004, 0.04), 0.004, pal.Enamel_Cream)  # luggage tag
    with s.at((0.04, Y0 - 0.37, 0.62)):  # hatbox: teal drum, cream lid, a coral ribbon
        s.lathe([(0, 0), (0.12, 0.0), (0.12, 0.1)], pal.Enamel_Teal, seg=24, cap_top=False)
        s.lathe(puck(0.126, 0.09, 0.125, 0.006), pal.Enamel_Cream, seg=24)
        s.box((-0.127, -0.012, 0.0), (0.127, 0.012, 0.128), pal.Enamel_Coral)


# ----------------------------------------------------------------- waiter

WT_CONE = [(0.345, 0.55), (0.32, 0.66), (0.27, 0.82), (0.21, 0.98), (0.15, 1.12), (0.13, 1.15), (0.08, 1.165), (0, 1.17)]


@asset("RobotWaiter", "Robots", bevel=0, lod=4000)
def robot_waiter(s):
    """Squat bell waiter on one glowing ball wheel: seafoam skirt with amber studs under a chrome flare, a broad
    cream cone with gold buttons and a coral bow tie; a TV head with rabbit-ear aerials whose screen shows a teal
    smile; a cloche and a bud vase on the tray, a napkin over the other arm."""
    with s.at((0, 0, 0.19)):
        ball(s, 0.19, pal.Rubber, seg=24, n=10)
    lamp(s, (0, -0.19, 0.19), (0, -1, 0), 0.075, lens=pal.Glow_Seafoam, housing=pal.Chrome, hr=0.092, hd=0.015,
         depth=0.02, seg=16)
    skirt = [(0.12, 0.29), (0.34, 0.29), (0.39, 0.33), (0.4, 0.38), (0.38, 0.45), (0.35, 0.49)]
    s.lathe(soft(skirt, 0.02), pal.Enamel_Seafoam, seg=32)
    lights(s, lambda z: 0.398, 0.37, 7, -165, -15)
    s.lathe(soft([(0.34, 0.48), (0.42, 0.5), (0.43, 0.525), (0.41, 0.55), (0.34, 0.56)], 0.008), pal.Chrome, seg=32,
            cap_bottom=False, cap_top=False)
    rivets(s, lambda z: 0.425, 0.524, 16, r=0.007)
    s.lathe(soft(WT_CONE, 0.02), pal.Enamel_Cream, seg=32, cap_bottom=False)
    cr = profile_rad(WT_CONE)
    band(s, 0.274, 0.288, 0.79, 0.815, pal.Chrome, seg=32)
    for a in (-35, -145, 35, 145):
        vseam(s, cr, 0.58, 0.78, a)
        vseam(s, cr, 0.83, 1.08, a)
    for z in (0.64, 0.72, 0.9, 0.98):
        stud(s, *surf(cr, z, -90), 0.011, pal.Gold)
    lights(s, cr, 1.07, 5, 20, 160, lens=pal.Glow_Seafoam, r=0.013)
    bowtie(s, *surf(cr, 1.1, -90, 0.012), 0.09, pal.Enamel_Coral)
    # TV head: ivory cabinet, chrome bezel, a bulging dark screen with a dot-matrix face
    scr = lambda x, z: -0.088 - 0.013 * math.sqrt(max(0.0, 1 - (x / 0.07) ** 2 - ((z - 1.245) / 0.056) ** 2))  # noqa: E731
    tv = lambda x, z: (x, scr(x, z), z)  # noqa: E731
    with s.part("Head") as h:  # swivels on the cone's tip
        h.rbox((-0.1, -0.08, 1.16), (0.1, 0.08, 1.33), 0.03, pal.Enamel_Ivory)
        h.rbox((-0.085, -0.09, 1.175), (0.085, -0.07, 1.315), 0.02, pal.Chrome)
        with h.at((0, -0.088, 1.245), (90, 0, 0), (0.07, 0.056, 0.013)):
            ball(h, 1.0, pal.Bakelite, seg=24, n=10)
        with h.part("Face") as f:
            screen(f, tv, 0.12, 0.1, 1.245, pal.Face_Teal, nv=10)
        for sg in (-1, 1):
            with h.at((sg * 0.1, 0.0, 1.21 + (0.05 if sg > 0 else 0.0)), (0, 90, 0)):
                h.lathe(puck(0.018, -0.004, 0.014, 0.004), pal.Chrome, seg=8)  # tuning knobs on the flank
            antenna(h, (sg * 0.03, 0.02, 1.33), 0.17, (sg * 0.075, 0.01))  # rabbit ears
    for sg in (-1, 1):
        shoulder(s, (sg * 0.19, 0, 1.0), sg, 0.042)
    arm(s, [(0.19, 0, 1.0), (0.33, -0.03, 0.97), (0.4, -0.08, 1.1)], curl=0.35)
    top = tray(s, (0.44, -0.1, 1.14), 0.17)
    cloche(s, (0.41, -0.12, top))
    bud_vase(s, (0.53, -0.06, top))
    glass(s, (0.47, 0.0, top))
    arm(s, [(-0.19, 0, 1.0), (-0.3, -0.03, 0.84), (-0.27, -0.22, 0.76)], curl=0.8)
    towel(s, (-0.285, -0.125, 0.81), (0.03, -0.19, -0.08), w=0.11, drop=(0.11, 0.13))


# ----------------------------------------------------------------- nanny

NN_BASE = [(0, 0.1), (0.24, 0.1), (0.32, 0.15), (0.355, 0.24), (0.36, 0.33)]
NN_EGG = [(0.36, 0.34), (0.395, 0.46), (0.41, 0.6), (0.395, 0.76), (0.34, 0.9), (0.25, 1.0), (0.15, 1.05)]


@asset("RobotNanny", "Robots", bevel=0, lod=4000)
def robot_nanny(s):
    """Egg-bodied nanny on a seafoam wheel base, head tilted fondly toward a glass bubble pram where a baby in a
    bonnet sits up under a coral blanket: smiling dot-matrix eyes glowing on a black visor, a lace
    apron with a heart, a coral sash tied in a bow at the back."""
    s.lathe(soft(NN_BASE, 0.03), pal.Enamel_Seafoam, seg=32, cap_top=False)
    band(s, 0.357, 0.373, 0.325, 0.355, pal.Chrome, seg=32)
    s.lathe(soft(NN_EGG, 0.04), pal.Enamel_Ivory, seg=32, cap_bottom=False, cap_top=False)
    eg = profile_rad(NN_EGG)
    lamp(s, (0, -0.345, 0.22), (0, -1, -0.2), 0.022, lens=pal.Glow_Seafoam, depth=0.012, seg=8)
    for sg in (-1, 1):
        wheel(s, (sg * 0.35, 0.03, 0.155), 0.155, 0.08, rin=0.1, hub=0.55, seg=20, side=sg)
    # apron: skirt and bib in ivory with a scalloped hem and a coral heart; a coral sash with a bow behind
    patch(s, lambda z: eg(z) + 0.003, -150, -30, [0.4 + 0.03 * k for k in range(9)], 0.007, pal.Fabric_Ivory, n=16)
    patch(s, lambda z: eg(z) + 0.003, -118, -62, [0.62 + 0.035 * k for k in range(8)], 0.007, pal.Fabric_Ivory, n=8)
    for k in range(11):
        p, n = surf(eg, 0.398, -145 + 11 * k, 0.004)
        with s.at(tuple(p), (0, 0, -145 + 11 * k), (1, 0.4, 0.8)):
            ball(s, 0.018, pal.Fabric_Ivory, seg=8, n=5)
    emblem(s, *surf(eg, 0.8, -90, 0.009), heart(0.036), pal.Enamel_Coral)
    band(s, eg(0.64) - 0.004, eg(0.64) + 0.012, 0.62, 0.66, pal.Enamel_Coral, seg=32)
    bowtie(s, (0, eg(0.64) + 0.016, 0.64), (0, 1, 0), 0.14, pal.Enamel_Coral)
    for a0, a1 in ((78, 86), (94, 102)):
        patch(s, lambda z: eg(z) + 0.012, a0, a1, [0.5, 0.54, 0.58, 0.62], 0.004, pal.Enamel_Coral, n=3)
    s.lathe(soft([(0.15, 1.04), (0.23, 1.05), (0.245, 1.09), (0.18, 1.12), (0.12, 1.12)], 0.012), pal.Enamel_Seafoam,
            seg=28, cap_bottom=False, cap_top=False)
    with s.at((0, 0, 1.12), (12, 7, 0)), s.part("Head") as h:  # tilted toward the pram: helmet, visor, face, cog
        with h.at((0, 0, 0.13), (0, 0, 0), (1.3, 1.12, 0.84)):
            ball(h, 0.17, pal.Enamel_Ivory, seg=28, n=12)
        with h.at((0, -0.105, 0.105), (0, 0, 0), (1.3, 0.8, 0.72)):
            ball(h, 0.13, pal.Bakelite, seg=24, n=12)
        vy = lambda x, z: -0.105 - 0.104 * math.sqrt(max(0.0, 1 - (x / 0.169) ** 2 - ((z - 0.105) / 0.0936) ** 2))  # noqa: E731
        visor = lambda x, z: (x, vy(x, z), z)  # noqa: E731
        with h.part("Face") as f:
            screen(f, visor, 0.26, 0.15, 0.1, pal.Face_Teal, nu=16, nv=8)
        for sg in (-1, 1):
            with h.at((sg * 0.219, 0, 0.13), (0, sg * 90, 0)):
                h.lathe(puck(0.05, -0.012, 0.014, 0.006), pal.Chrome, seg=14)
                h.lathe(puck(0.03, 0.01, 0.02, 0.004), pal.Enamel_Seafoam, seg=12)
        with h.at((0, 0.02, 0.285), (0, 90, 0)):  # the little cog on top
            h.lathe(puck(0.045, -0.015, 0.015, 0.006), pal.Chrome, seg=12)
            for k in range(8):
                a = math.radians(45 * k)
                with h.at((0.05 * math.cos(a), 0.05 * math.sin(a), 0), (0, 0, 45 * k)):
                    h.box((-0.01, -0.01, -0.012), (0.01, 0.01, 0.012), pal.Chrome)
    for sg in (-1, 1):
        shoulder(s, (sg * 0.385, -0.02, 0.8), sg, 0.06)
        arm(s, [(sg * 0.41, -0.02, 0.8), (sg * 0.39, -0.3, 0.72), (sg * 0.22, -0.5, 0.79)], r=0.025, curl=0.85)
    # the pram: a glass bubble in a teal cradle, a coral pompom on top; a baby under a blanket; chrome frame, handle
    Y, Z, R = -0.88, 0.52, 0.25
    e0 = math.radians(-18)  # the cradle's rim, below the bubble's equator

    def arc(r, a0, a1, n):
        return [(r * math.cos(a0 + (a1 - a0) * k / n), r * math.sin(a0 + (a1 - a0) * k / n)) for k in range(n + 1)]

    with s.at((0, Y, Z)):
        s.lathe(arc(R + 0.012, -math.pi / 2, e0, 10) + arc(R - 0.004, e0, -math.pi / 2, 10)[1:], pal.Enamel_Seafoam,
                seg=32)
        with s.at((0, 0, (R + 0.01) * math.sin(e0))):
            band(s, (R - 0.01) * math.cos(e0), (R + 0.03) * math.cos(e0), -0.008, 0.012, pal.Chrome, seg=32)
        rivets(s, lambda z: R + 0.012 * math.cos(e0), -0.13, 12, mat=pal.Chrome)
        s.lathe(arc(R, e0, math.pi / 2, 14), pal.Glass, seg=32, cap_bottom=False)
        with s.at((0, 0, R + 0.012)):
            ball(s, 0.026, pal.Enamel_Coral, seg=12, n=8)
        s.lathe(puck(0.19, -0.175, -0.15, 0.01), pal.Fabric_Ivory, seg=24)  # mattress
        with s.at((0, -0.05, -0.13), (0, 0, 0), (1, 1.4, 0.6)):
            ball(s, 0.1, pal.Enamel_Coral, seg=16, n=10)  # blanket
        with s.at((0, 0.06, -0.035)):  # the baby, sitting up: a bonnet, dark bead eyes, a waving hand
            ball(s, 0.052, pal.Skin, seg=14, n=10)
            with s.at((0, 0.016, 0.006)):
                ball(s, 0.058, pal.Fabric_Ivory, seg=14, n=10)
            for sg in (-1, 1):
                with s.at((sg * 0.018, -0.05, 0.004)):
                    ball(s, 0.0065, pal.Bakelite, seg=6, n=4)
        with s.at((0.07, -0.01, -0.03)):
            ball(s, 0.018, pal.Skin, seg=8, n=5)
    for sg in (-1, 1):
        s.sweep(fillet([(sg * 0.18, Y - 0.2, 0.07), (sg * 0.2, Y, Z - 0.27), (sg * 0.18, Y + 0.2, 0.07)], 0.08, n=4,
                       closed=False), 0.012, pal.Chrome, seg=6)
        for dy in (-0.2, 0.2):
            wheel(s, (sg * 0.18, Y + dy, 0.06), 0.06, 0.03, rin=0.04, hub=0.5, seg=10)
    s.sweep(fillet([(-0.19, Y + 0.14, Z - 0.16), (-0.22, -0.5, 0.79), (0.22, -0.5, 0.79), (0.19, Y + 0.14, Z - 0.16)],
                   0.06, n=4, closed=False), 0.014, pal.Chrome, seg=8)
    s.sweep([(-0.12, -0.5, 0.79), (0.12, -0.5, 0.79)], 0.02, pal.Rubber, seg=8)  # grip


# ----------------------------------------------------------------- shopkeeper

SK_LOWER = [(0.21, 0.33), (0.235, 0.42), (0.25, 0.6)]
SK_BODY = [(0.25, 0.62), (0.27, 0.8), (0.275, 1.3)] + dome(0.275, 1.3, 0.27, 8)[1:]
SK_NECK = 2  # the capsule/nose-cone seam, under the chrome band


@asset("RobotShopkeeper", "Robots", bevel=0, lod=4000)
def robot_shopkeeper(s):
    """Rocket-bodied shop assistant waving hello: a cream capsule with a lit display case of goods under a striped
    scalloped awning, a display face of round teal eyes and a smile, a coral spike; cash-register keys on a
    vented seafoam lower body on short chrome legs and a glowing wheeled pad; a shopping bag with a baguette."""
    s.rbox((-0.26, -0.21, 0.05), (0.26, 0.21, 0.14), 0.04, pal.Enamel_Seafoam)  # pad
    s.box((-0.18, -0.214, 0.075), (0.18, -0.205, 0.105), pal.Glow_Seafoam)
    s.extrude(rrect(0.42, 0.32, 0.08, 6), 0.035, 0.05, pal.Glow_Seafoam)
    for x in (-0.18, 0.18):
        for y in (-0.13, 0.13):
            wheel(s, (x, y, 0.045), 0.045, 0.03, rin=0.03, hub=0.5, seg=8)
    for x in (-0.09, 0.09):  # short legs
        s.sweep([(x, 0, 0.14), (x, 0, 0.33)], 0.028, pal.Chrome, seg=8)
        with s.at((x, 0, 0.2)):
            ball(s, 0.036, pal.Chrome, seg=10, n=7)
    s.lathe(soft([(0, 0.3), (0.15, 0.3)] + SK_LOWER, 0.03), pal.Enamel_Seafoam, seg=28, cap_top=False)
    lr = profile_rad(SK_LOWER)
    for sg in (-1, 1):  # side vents
        for k in range(5):
            z = 0.4 + 0.035 * k
            s.box((sg * 0.244 - 0.006, -0.08, z), (sg * 0.244 + 0.006, 0.08, z + 0.014), pal.Iron)
    patch(s, lr, -124, -56, [0.43, 0.47, 0.51, 0.55], 0.004, pal.Chrome, n=10)  # cash register keys
    keys = (pal.Enamel_Ivory, pal.Enamel_Coral, pal.Enamel_Mustard, pal.Enamel_Teal)
    for j, z in enumerate((0.455, 0.52)):
        for i, a in enumerate((-114, -98, -82, -66)):
            p, _ = surf(lr, z, a, 0.004)
            with s.at(tuple(p), (90, 0, a + 90)):
                s.rbox((-0.02, -0.014, -0.004), (0.02, 0.014, 0.016), 0.005, keys[(i + j) % 4])
    band(s, 0.246, 0.264, 0.59, 0.63, pal.Chrome, seg=28)
    s.lathe(soft(SK_BODY[:SK_NECK + 1], 0.03), pal.Enamel_Cream, seg=32, cap_bottom=False, cap_top=False)
    bd = profile_rad(SK_BODY)
    band(s, 0.274, 0.29, 1.28, 1.31, pal.Chrome, seg=32)
    with s.part("Head") as h:  # the nose cone turns above the band
        h.lathe(SK_BODY[SK_NECK:], pal.Enamel_Cream, seg=32, cap_bottom=False)
        h.lathe([(0.055, 1.54), (0.03, 1.63), (0.008, 1.7), (0, 1.705)], pal.Enamel_Coral, seg=16)
        lights(h, bd, 1.5, 6, -30, 210, r=0.012)
        face = display(h, bd, 1.4, 0.24, 0.1)
        with h.part("Face") as f:
            screen(f, face, 0.23, 0.09, 1.4, pal.Face_Teal)
        for sg in (-1, 1):
            antenna(h, (sg * 0.13, 0.0, 1.5), 0.14, (sg * 0.04, 0.0))
    # display case, proud of the chest: cream shell, lit back, two rows of goods, glass in a chrome frame
    X0, X1, Z0, Z1, YB, YF = -0.17, 0.17, 0.96, 1.24, -0.27, -0.36
    for mn, mx in (((X0 - 0.02, YF, Z0 - 0.02), (X1 + 0.02, -0.2, Z0)), ((X0 - 0.02, YF, Z1), (X1 + 0.02, -0.2, Z1 + 0.02)),
                   ((X0 - 0.02, YF, Z0), (X0, -0.2, Z1)), ((X1, YF, Z0), (X1 + 0.02, -0.2, Z1))):
        s.box(mn, mx, pal.Enamel_Cream)
    s.box((X0, YB - 0.006, Z0), (X1, YB, Z1), pal.LampSoft)
    zs = (Z0 + 0.005, Z0 + 0.14)
    s.box((X0, YF + 0.006, zs[1] - 0.008), (X1, YB, zs[1]), pal.Chrome)  # shelf
    goods = ((-0.12, 0, 0.06, 0.07, pal.Enamel_Coral), (-0.04, 0, 0.05, 0.1, pal.Enamel_Mustard),
             (0.05, 0, 0.06, 0.06, pal.Enamel_Teal), (0.12, 0, 0.04, 0.09, pal.Enamel_Red),
             (-0.1, 1, 0.07, 0.05, pal.Enamel_Teal), (0.0, 1, 0.05, 0.08, pal.Enamel_Coral),
             (0.1, 1, 0.06, 0.06, pal.Enamel_Mustard))
    for x, row, w, h, m in goods:
        z = zs[row]
        if w < 0.055:  # bottles
            with s.at((x, (YB + YF) / 2, z)):
                s.lathe([(0, 0), (0.02, 0), (0.02, h * 0.65), (0.008, h * 0.8), (0.008, h), (0, h)], m, seg=10)
        else:
            s.rbox((x - w / 2, YF + 0.02, z), (x + w / 2, YB - 0.01, z + h), 0.006, m)
    s.box((X0, YF + 0.004, Z0), (X1, YF + 0.008, Z1), pal.Glass)
    for mn, mx in (((X0 - 0.02, YF - 0.015, Z0 - 0.02), (X1 + 0.02, YF, Z0 + 0.01)),
                   ((X0 - 0.02, YF - 0.015, Z1 - 0.01), (X1 + 0.02, YF, Z1 + 0.02)),
                   ((X0 - 0.02, YF - 0.015, Z0), (X0 + 0.01, YF, Z1)), ((X1 - 0.01, YF - 0.015, Z0), (X1 + 0.02, YF, Z1))):
        s.box(mn, mx, pal.Chrome)
    s.sweep(fillet([(-0.1, -0.28, 0.9), (-0.1, -0.31, 0.9), (0.1, -0.31, 0.9), (0.1, -0.28, 0.9)], 0.015, n=3,
                   closed=False), 0.009, pal.Chrome, seg=6)  # drawer handle
    # striped awning over the case: eight coral and cream stripes sloping out, a scalloped valance
    L, W, T = 0.27, 0.05, 0.012
    stripe = lambda k: pal.Enamel_Coral if k % 2 == 0 else pal.Enamel_Cream  # noqa: E731
    with s.at((0, -0.17, 1.31), (16, 0, 0)):
        for k in range(8):
            s.box((-0.2 + W * k, -L, -T / 2), (-0.2 + W * (k + 1), 0, T / 2), stripe(k))
    fy, fz = -0.17 - L * math.cos(math.radians(16)), 1.31 - L * math.sin(math.radians(16))
    for k in range(8):
        with s.at((-0.175 + W * k, fy + 0.004, fz), (90, 0, 0)):
            s.extrude([(-0.025, 0.004)] + [(0.025 * math.cos(t), 0.025 * math.sin(t))
                                           for t in (math.pi + math.pi * i / 10 for i in range(11))] + [(0.025, 0.004)],
                      -0.004, 0.004, stripe(k), smooth_sides=True)
    for sg in (-1, 1):
        shoulder(s, (sg * 0.28, 0, 1.18), sg, 0.055)
    arm(s, [(-0.31, 0, 1.18), (-0.43, -0.04, 1.3), (-0.45, -0.08, 1.48)], curl=0.0)  # waving hello
    tip, _ = arm(s, [(0.31, 0, 1.18), (0.37, -0.02, 0.98), (0.35, -0.06, 0.8)], curl=0.9)
    # a paper shopping bag hanging from the hand, a baguette and greens poking out
    s.rbox((0.29, -0.13, 0.34), (0.43, -0.01, 0.58), 0.008, pal.Enamel_Mustard)
    for x in (0.32, 0.4):
        s.sweep(fillet([(x, -0.07, 0.575), (x, -0.07, 0.64), (0.35 + (x - 0.36) * 0.2, -0.066, 0.72)], 0.03, n=3,
                       closed=False), 0.004, pal.Enamel_Cream, seg=6)
    s.sweep([(0.33, -0.05, 0.5), (0.36, -0.08, 0.64), (0.385, -0.105, 0.74)], [0.022, 0.024, 0.016], pal.Timber, seg=8)
    for k in range(3):
        a = math.radians(-30 + 30 * k)
        s.sweep([(0.4, -0.04, 0.55), (0.41 + 0.04 * math.sin(a), -0.03, 0.66), (0.42 + 0.07 * math.sin(a), -0.02, 0.7)],
                [0.008, 0.011, 0.003], pal.Stem, seg=6)
