"""Brief 097 (Vela, 2026-11-06): the four Handover-zone street props that drew as procedural boxes, as DecoKit pieces. Concepts (Codex, $0):
paid/097-props/codex/ (police-post-A picked over B; mast-camera, civic-robot-dock, dumpster), prompts and picks in briefs/097/vela/props.md.

PolicePost        Street  1.0 x 1.0 x 2.8 m (quiet.ts QUIET.size): pass 2 (concept A): a round teal enamel drum on a honed limestone plinth, brass hairline at its foot, chrome rings,
                          an ivory front panel with the hand-set in its chrome cradle and a round speaker, an ivory crown and the opal lamp dome (part Lamp, DK_Lamp:
                          the host lights it when a Talker calls). Talkers phone the police from here: the dome and the column read it at play zoom.
MastCamera        Signals 8 m civic camera mast (public.ts / transit.ts, item 0.4 x 0.4 x 8): limestone flared foot, a tapering ivory pole with seafoam collars,
                          a chrome yoke and the streamline camera pod (part Head: turns about the mast; its lens looks -Y, Unity +Z), tally lamp on top.
CivicRobotDock    Street  1.2 x 1.2 x 2.6 m (catalogue civic-robot-dock): an ivory alcove open to the front on a limestone base, seafoam fins and hood band,
                          an opal light strip under the hood lip, a chrome charging column with a dial on the back wall, chrome cradle shoes on a charcoal pad.
Dumpster_Civic    Street  2.0 x 1.1 x 1.47 m (the stash container, inside the Dumpster slot 2.1 x 1.186 x 1.513): a rounded teal enamel body on charcoal feet,
                          three ivory roll lids on a chrome hinge bar, coral lid pulls, a chrome kick rail, a brass hairline round the body.
Conventions as the other street pieces: metres, Z up, pivot = footprint centre at the foot (z 0), front -Y. Palette materials only (DK_Enamel_*, DK_Limestone,
DK_Chrome, DK_Brass_Civic, DK_Lamp, DK_GlassDark): no new material. The AO bake (vertex_ao.PIECES) is on for all four."""

import math

import mats
from geo import arc, dome, rrect
from kit import asset
from mats import pal, register
from signals import FRONT, SIDE_R, puck, slab

register("DK_Brass_Civic", "brass", mats.BRASS)
# Sky 2026-11-06 ("why you reduce the colors ... converging on blue"): the old town's colourway enamels, each piece in its old colour family (no new material where a colourway .mat exists)
register("DK_Enamel_Teal__boulevard", "enamel", "#C8553D")   # boulevard coral
register("DK_Enamel_Teal__southside", "enamel", "#D9962B")   # southside marigold
register("DK_Enamel_Seafoam__gardens", "enamel", "#8FA88A")  # gardens sage
  # hairline brass (civic_common registers the same row)


def rloft(s, secs, mat, n=4, cap_bottom=False, cap_top=True):
    """A rounded-rect loft: secs = [(w, d, r, z)], each corner arc n segments (equal point counts)."""
    return s.loft([(rrect(w, d, r, n), z) for w, d, r, z in secs], mat, cap_bottom=cap_bottom, cap_top=cap_top)


def rband(s, w, d, r, z0, z1, mat, n=4, lip=0.006):
    """A thin proud band round a rounded-rect body (a ring or hairline): out by `lip`, soft top and bottom."""
    e = (z1 - z0) / 3
    rloft(s, [(w, d, r, z0), (w + 2 * lip, d + 2 * lip, r + lip, z0 + e), (w + 2 * lip, d + 2 * lip, r + lip, z1 - e), (w, d, r, z1)], mat, n, cap_top=False)


# ----------------------------------------------------------------------------- police post (pass 2: concept A's round drum)
DR, PLINTH, TOPC = 0.36, 0.14, 2.30   # drum radius, its foot and top
PAN = dict(z0=0.56, z1=1.9, half=0.5)   # the cream panel set into the front: height range, half-angle (radians) at its widest


def _drum_pt(a, z, r):
    """A point on the drum at angle a from the front (-Y), height z, radius r."""
    return (r * math.sin(a), -r * math.cos(a), z)


def _panel(s, r, z0, z1, half, mat, nu=5, nv=9):
    """A curved keyhole panel on the drum: full width over the lower body, a round top (half-circle in angle x height)."""
    h, rt = z1 - z0, half * r          # the top's radius in metres along the surface
    def width(v):
        z = z0 + v * h
        top = z1 - rt
        if z <= top:
            return half
        return half * math.sqrt(max(0.0, 1 - ((z - top) / rt) ** 2))
    s.sheet(lambda u, v: _drum_pt((u - 0.5) * 2 * max(width(v), 0.002), z0 + v * h, r), nu, nv, mat)
    return [(_drum_pt(-width(k / 12), z0 + k / 12 * h, r)) for k in range(13)] + [(_drum_pt(width(1 - k / 12), z0 + (1 - k / 12) * h, r)) for k in range(1, 13)]


@asset("PolicePost", "Street", bevel=0.0)
def police_post(s):
    rloft(s, [(0.98, 0.98, 0.3, 0.0), (0.98, 0.98, 0.3, 0.085), (0.96, 0.96, 0.29, 0.105), (0.9, 0.9, 0.27, 0.115), (0.84, 0.84, 0.3, 0.12),
              (0.84, 0.84, 0.3, PLINTH)], pal.Limestone, n=4)                                                     # the honed limestone plinth
    s.lathe([(DR + 0.012, PLINTH), (DR + 0.017, PLINTH + 0.004), (DR + 0.017, PLINTH + 0.012), (DR + 0.012, PLINTH + 0.016)], pal.Brass_Civic, seg=24,
            cap_bottom=False, cap_top=False)                                                                     # the brass hairline at the foot
    s.lathe([(DR + 0.012, PLINTH + 0.016), (DR + 0.02, PLINTH + 0.03), (DR + 0.02, PLINTH + 0.1), (DR, PLINTH + 0.12)], pal.Enamel_Ivory, seg=24,
            cap_bottom=False, cap_top=False)                                                                     # the ivory skirt
    s.lathe([(DR, PLINTH + 0.12), (DR, TOPC - 0.06), (DR + 0.01, TOPC - 0.03), (DR + 0.02, TOPC)], pal.Enamel_Teal, seg=24,
            cap_bottom=False, cap_top=False)                                                                     # the round teal enamel drum
    for z in (0.33, 0.38, 2.0, 2.05):                                                                            # chrome ring pairs, bottom and top
        s.lathe(ring_prof(DR, DR + 0.012, z, z + 0.022), pal.Chrome, seg=24, cap_bottom=False, cap_top=False)
    s.lathe([(DR + 0.02, TOPC), (0.4, TOPC + 0.03), (0.405, TOPC + 0.09), (0.39, TOPC + 0.13), (0.3, TOPC + 0.17), (0.25, TOPC + 0.18)],
            pal.Enamel_Ivory, seg=24, cap_bottom=False, cap_top=False)                                           # the ivory crown
    s.lathe(ring_prof(0.403, 0.413, TOPC + 0.05, TOPC + 0.075), pal.Chrome, seg=24, cap_bottom=False, cap_top=False)
    with s.at((0, 0, TOPC + 0.18)):
        s.lathe([(0.25, -0.01), (0.265, 0.005), (0.265, 0.025), (0.25, 0.04), (0.24, 0.04)], pal.Chrome, seg=24, cap_bottom=False, cap_top=False)
        with s.part("Lamp") as p:                                                                               # the opal dome (2.52 -> 2.78 m)
            p.lathe([(0.245, 0.04)] + dome(0.245, 0.045, 0.23, n=5)[1:], pal.Lamp, seg=24, cap_bottom=False)
        s.lathe(puck(0.032, 0.265, 0.3, 0.01, n=2), pal.Chrome, seg=10)                                          # the finial knob, 2.78 m
    rim = _panel(s, DR + 0.006, PAN["z0"], PAN["z1"], PAN["half"], pal.Enamel_Ivory)                              # the cream panel set into the front
    s.sweep(rim + rim[:1], 0.009, pal.Chrome, seg=5, caps=False)                                                  # its chrome bead
    _panel(s, DR + 0.01, 1.0, 1.66, 0.3, pal.Enamel_Charcoal, nu=4, nv=6)                                        # the hand-set's dark recess
    hood = [_drum_pt((k / 8 - 0.5) * 0.7, 1.7, DR + 0.012) for k in range(9)]
    s.sweep(hood, 0.03, pal.Enamel_Teal__boulevard, seg=6)                                                                # the seafoam brow over the recess
    with s.at((0, -(DR + 0.012), 1.32), FRONT):     # front hardware: local x across, y up, +Z out of the drum
        hs = [(-0.04, 0.28, 0.03), (-0.055, 0.2, 0.055), (-0.058, 0.0, 0.058), (-0.055, -0.14, 0.055), (-0.04, -0.22, 0.03)]
        s.sweep(hs, [0.02, 0.022, 0.018, 0.022, 0.02], pal.Chrome, seg=6)                                         # the chrome hand-set
        for y in (0.28, -0.22):
            with s.at((-0.04, y, 0.03)):
                s.lathe(puck(0.036, -0.02, 0.016, 0.01, n=1), pal.Chrome, seg=8)                                  # its ear and mouth cups
        s.sweep([(0.03, 0.22, 0.0), (0.07, 0.22, 0.03), (0.08, 0.16, 0.04), (0.06, 0.1, 0.03), (0.03, 0.1, 0.0)], 0.011, pal.Chrome, seg=6)  # cradle
        s.sweep([(0.05, 0.13, 0.04), (0.07, -0.04, 0.03), (0.06, -0.2, 0.02), (0.03, -0.28, 0.0)], 0.006, pal.Enamel_Charcoal, seg=5)    # cord
    with s.at((0, -(DR + 0.008), 0.8), FRONT):      # the round speaker
        s.lathe(puck(0.09, 0.0, 0.02, 0.007, n=1), pal.Chrome, seg=16)
        s.lathe(puck(0.068, 0.014, 0.024, 0.003, n=1), pal.Enamel_Charcoal, seg=16)
        for k in range(-2, 3):
            s.box((-0.055 + 0.007 * abs(k), 0.024 * k - 0.004, 0.022), (0.055 - 0.007 * abs(k), 0.024 * k + 0.004, 0.028), pal.Chrome)
    s.solid((-0.49, -0.49, 0), (0.49, 0.49, 2.8))


# ----------------------------------------------------------------------------- mast camera
MAST, HEAD = 7.4, 7.68       # pole top, the pod's axis height (the pod's top at 7.85, its tally 7.9 m)


def pod(s):
    """The camera pod along local +Z (forward), axis at the origin."""
    prof = [(0.0, -0.46), (0.05, -0.44), (0.085, -0.38), (0.11, -0.26), (0.125, -0.1), (0.128, 0.08), (0.12, 0.2), (0.105, 0.27)]
    s.lathe(prof, pal.Enamel_Ivory, seg=20, cap_top=False, cap_bottom=False)
    s.lathe([(0.105, 0.27), (0.112, 0.29), (0.108, 0.31), (0.09, 0.315)], pal.Chrome, seg=20, cap_bottom=False, cap_top=False)   # the bezel
    s.lathe([(0.09, 0.312)] + dome(0.09, 0.312, 0.035, n=3)[1:], pal.GlassDark, seg=20, cap_bottom=False)                         # the lens
    s.lathe(ring_prof(0.129, 0.135, -0.06, 0.0), pal.Enamel_Teal__boulevard, seg=20, cap_bottom=False, cap_top=False)            # the coral band
    for a in (0, 120, 240):                                                                                                         # three tail fins
        with s.at((0, 0, 0), (0, 0, a + 90)):
            s.extrude([(0.06, -0.46), (0.17, -0.5), (0.2, -0.47), (0.1, -0.3)], -0.008, 0.008, pal.Enamel_Teal__boulevard)


def ring_prof(r0, r1, z0, z1):
    return [(r0, z0), (r1, z0 + (z1 - z0) * 0.2), (r1, z1 - (z1 - z0) * 0.2), (r0, z1)]


@asset("MastCamera", "Signals", bevel=0.0)
def mast_camera(s):
    s.lathe([(0.0, 0.0), (0.2, 0.0), (0.2, 0.06), (0.19, 0.09), (0.15, 0.13), (0.12, 0.2), (0.1, 0.32), (0.09, 0.42)], pal.Limestone, seg=24)  # the flared foot
    s.lathe([(0.09, 0.42), (0.1, 0.44), (0.1, 0.56), (0.08, 0.58)], pal.Enamel_Teal__southside, seg=20, cap_bottom=False, cap_top=False)          # foot collar
    s.lathe([(0.091, 0.416), (0.094, 0.42), (0.094, 0.428), (0.091, 0.432)], pal.Brass_Civic, seg=20, cap_bottom=False, cap_top=False)    # brass hairline
    s.lathe([(0.075, 0.58), (0.07, 2.0), (0.058, 4.5), (0.048, MAST - 0.12)], pal.Enamel_Ivory, seg=16, cap_bottom=False, cap_top=False)  # the tapering pole
    for z, r in ((1.6, 0.072), (MAST - 0.12, 0.048)):
        s.lathe(ring_prof(r, r + 0.012, z - 0.03, z + 0.03), pal.Chrome, seg=16, cap_bottom=False, cap_top=False)
    s.lathe([(0.048, MAST - 0.12), (0.07, MAST - 0.1), (0.07, MAST), (0.05, MAST + 0.02)], pal.Enamel_Teal__southside, seg=16, cap_bottom=False)  # top collar
    with s.part("Head") as h:
        h.lathe(puck(0.06, MAST + 0.02, MAST + 0.07, 0.015, n=2), pal.Chrome, seg=16)                                                   # the turntable
        h.sweep([(-0.205, 0, HEAD), (-0.205, 0, MAST + 0.1), (-0.15, 0, MAST + 0.05), (0.15, 0, MAST + 0.05), (0.205, 0, MAST + 0.1), (0.205, 0, HEAD)],
                0.016, pal.Chrome, seg=8)                                                                                                  # the yoke
        for x in (-0.195, 0.195):
            with h.at((x, 0, HEAD), (0, 90, 0)):
                h.lathe(puck(0.035, -0.012, 0.012, 0.006, n=1), pal.Chrome, seg=10)                                                        # the pivots
        with h.at((0, 0, HEAD), FRONT, scale=1.25):   # 1.15 m long: reads as the pod at play zoom
            pod(h)
        with h.at((0, 0.06, HEAD + 0.155)):
            h.lathe(puck(0.03, -0.005, 0.01, 0.004, n=1), pal.Chrome, seg=10)
            h.lathe([(0.022, 0.008)] + dome(0.022, 0.01, 0.025, n=3)[1:], pal.Lamp, seg=10, cap_bottom=False)                              # the tally
    s.solid((-0.2, -0.2, 0), (0.2, 0.2, MAST))


# ----------------------------------------------------------------------------- civic robot dock
DW, DD, BASE, HOOD = 1.16, 1.12, 0.14, 2.28


def shell_poly(w, d, r, t, n=5):
    """Plan of the alcove's U-shaped shell (open to -Y): outer rounded back corners, inner cavity, CCW."""
    hw, hd = w / 2, d / 2
    out = [(hw, -hd)] + arc(hw - r, hd - r, r, 0, 90, n) + arc(-hw + r, hd - r, r, 90, 180, n) + [(-hw, -hd)]
    ri = max(r - t, 0.04)
    inn = [(-hw + t, -hd)] + arc(-hw + t + ri, hd - t - ri, ri, 180, 90, n) + arc(hw - t - ri, hd - t - ri, ri, 90, 0, n) + [(hw - t, -hd)]
    return out + inn


@asset("CivicRobotDock", "Street", bevel=0.0)
def civic_robot_dock(s):
    rloft(s, [(1.2, 1.2, 0.22, 0.0), (1.2, 1.2, 0.22, BASE - 0.025), (1.17, 1.17, 0.2, BASE)], pal.Limestone, n=5)                 # the honed base
    s.extrude(shell_poly(DW, DD, 0.3, 0.09), BASE, HOOD, pal.Enamel_Ivory, smooth_sides=True)                                       # the alcove shell
    for x in (-1, 1):                                                                                                                # seafoam fins on the cheeks
        xs = x * (DW / 2 - 0.045)
        s.rbox((xs - 0.05, -DD / 2 - 0.03, BASE), (xs + 0.05, -DD / 2 + 0.06, HOOD + 0.05), 0.035, pal.Enamel_Teal__boulevard, seg=2)
    rloft(s, [(1.14, 1.16, 0.28, HOOD - 0.02), (1.2, 1.22, 0.3, HOOD + 0.01), (1.2, 1.22, 0.3, HOOD + 0.11), (1.17, 1.19, 0.29, HOOD + 0.14)],
          pal.Enamel_Ivory, n=5, cap_bottom=True, cap_top=False)                                                                     # the hood, soft rims
    rloft(s, [(1.17, 1.19, 0.29, HOOD + 0.14), (1.13, 1.15, 0.27, HOOD + 0.16), (1.13, 1.15, 0.27, HOOD + 0.23), (1.06, 1.08, 0.25, HOOD + 0.25)],
          pal.Enamel_Teal__boulevard, n=5, cap_top=False)                                                                                    # its seafoam band
    rloft(s, [(1.06, 1.08, 0.25, HOOD + 0.25), (0.9, 0.92, 0.24, HOOD + 0.28), (0.8, 0.82, 0.22, HOOD + 0.3)], pal.Enamel_Ivory, n=5)  # the cap
    s.sweep([(-0.42, -0.6, HOOD - 0.01), (-0.2, -0.635, HOOD - 0.01), (0.2, -0.635, HOOD - 0.01), (0.42, -0.6, HOOD - 0.01)], 0.022, pal.Lamp, seg=8)  # the light strip
    with s.at((0, DD / 2 - 0.09, 0)):   # the back wall: charging column, dial, brass hairline
        s.sweep([(0, -0.03, BASE + 0.12), (0, -0.03, 1.55)], 0.035, pal.Chrome, seg=10)
        for z in (0.6, 1.1):
            with s.at((0, -0.03, z)):
                s.lathe(puck(0.05, -0.02, 0.02, 0.008, n=1), pal.Chrome, seg=10)
        with s.at((0, 0, 1.78), FRONT):
            s.lathe(puck(0.14, 0.0, 0.03, 0.01, n=2), pal.Chrome, seg=24)
            s.lathe(puck(0.115, 0.02, 0.036, 0.004, n=1), pal.Enamel_Ivory, seg=24)
            s.box((-0.006, -0.01, 0.036), (0.006, 0.09, 0.042), pal.Enamel_Charcoal)                                                 # the dial's hand
        with s.at((0, 0, 1.0), FRONT):
            s.box((-0.4, -0.006, 0.0), (0.4, 0.006, 0.006), pal.Brass_Civic)
    with s.at((0, 0.02, BASE)):         # the charging pad and the cradle shoes the robot's feet stand in
        s.lathe(puck(0.36, 0.0, 0.025, 0.008, n=1), pal.Enamel_Charcoal, seg=28)
        s.lathe(ring_prof(0.36, 0.375, 0.0, 0.03), pal.Chrome, seg=28, cap_bottom=False, cap_top=False)
        for x in (-0.2, 0.2):
            s.rbox((x - 0.08, -0.12, 0.02), (x + 0.08, 0.16, 0.16), 0.05, pal.Chrome, seg=2)
    s.solid((-0.6, -0.6, 0), (0.6, 0.6, BASE))
    s.solid((-DW / 2, DD / 2 - 0.09, BASE), (DW / 2, DD / 2, HOOD + 0.3))


# ----------------------------------------------------------------------------- dumpster
BL, BD, BZ0, BZ1 = 2.0, 1.08, 0.12, 1.16


@asset("Dumpster_Civic", "Street", bevel=0.0)
def dumpster_civic(s):
    s.rbox((-BL / 2, -BD / 2, BZ0), (BL / 2, BD / 2, BZ1), 0.16, pal.Enamel_Seafoam__gardens, seg=4)                                         # the rounded teal body
    hl = [(x, y) for x, y in rrect(BL + 0.012, BD + 0.012, 0.16, 4)]
    s.sweep([(x, y, 0.98) for x, y in hl + hl[:1]], 0.005, pal.Brass_Civic, seg=4, caps=False)                                      # brass hairline round it
    s.rbox((-BL / 2 + 0.03, -BD / 2 + 0.03, BZ1 - 0.04), (BL / 2 - 0.03, BD / 2 - 0.03, BZ1 + 0.02), 0.03, pal.Enamel_Ivory, seg=2)  # the lid deck
    lw, hd = (BL - 0.12) / 3, BD / 2 - 0.05
    arch = [(hd * math.cos(math.pi * k / 12), 0.28 * math.sin(math.pi * k / 12) ** 0.7) for k in range(13)]   # (y, z): the lid rolls front to back
    for i in range(3):                                                                                                               # three roll lids
        cx = -BL / 2 + 0.06 + lw * (i + 0.5)
        with s.at((cx, 0, BZ1 + 0.02), SIDE_R):
            s.extrude(arch, -lw * 0.485, lw * 0.485, pal.Enamel_Ivory, smooth_sides=True)
        with s.at((cx, -BD / 2 + 0.03, BZ1 - 0.01), FRONT):                                                                          # coral pull on the deck's front lip
            s.rbox((-0.08, -0.022, -0.005), (0.08, 0.022, 0.03), 0.012, pal.Enamel_Coral, seg=1)
        for e in (-1, 1):                                                                                                            # chrome lid ribs over the roll
            s.sweep([(cx + e * lw * 0.49, y, BZ1 + 0.02 + z + 0.004) for y, z in arch], 0.012, pal.Chrome, seg=6)
    s.sweep([(-BL / 2 + 0.04, BD / 2 + 0.02, BZ1 + 0.06), (BL / 2 - 0.04, BD / 2 + 0.02, BZ1 + 0.06)], 0.022, pal.Chrome, seg=8)    # the hinge bar
    rail = [(-0.88, -BD / 2 + 0.02, 0.4), (-0.86, -BD / 2 - 0.05, 0.4), (0.86, -BD / 2 - 0.05, 0.4), (0.88, -BD / 2 + 0.02, 0.4)]
    s.sweep(rail, 0.022, pal.Chrome, seg=8)                                                                                         # the kick rail
    for x in (-0.78, 0.78):
        for y in (-0.36, 0.36):
            with s.at((x, y, 0)):
                s.lathe(puck(0.075, 0.0, BZ0 + 0.02, 0.02, n=2), pal.Enamel_Charcoal, seg=12)                                       # the feet
    s.solid((-BL / 2, -BD / 2, 0), (BL / 2, BD / 2, BZ1 + 0.3))
