"""Civic block (brief 008, B1) shared parts: the materials these pieces add, the café's front outline, hairline and
planting helpers. Colour roles (Sky, 14 Oct): seafoam and deep teal are the kit enamels; coral, flamingo and peach
(contract accents) are for banners and flowers; gold is hairline, frame and bead only (DK_Brass_Civic, never a fat panel). 14 Oct (Sky, first Unity build): the civic hairline is the painted
brass #99784D (dE2000 21.3 from the guard colour; the world gold #D0C848 read yellow-green), banners are seafoam, deep teal is an
accent of 0.3 m or less (a sign body, a reveal, a lamp sleeve), never a field, fascia or cap.

Frames: pieces face -Y with the facade / front line at y = 0 and the building on +Y; pivot = footprint centre at z = 0
(runs and furniture) or the front-centre of the wall they dress (café bay, entrance), as each docstring says."""

import math
import random

import mats
import spacehq  # noqa: F401  (registers DK_Lamp_Deco, dE2000 24.6 from the guard colour; DK_Lamp is 19.3, so civic lamps use Deco)
from geo import arc, dome
from mats import look_hex, register
from plaza_parts import lift
from structures import along, sec, tube

for _n, _hex in (("Coral", look_hex("accent_coral")), ("Flamingo", look_hex("accent_flamingo")), ("Peach", look_hex("accent_peach"))):
    register(f"DK_Cloth_{_n}Soft", "stone", _hex)    # banners (contract accents, dE2000 >= 21 from the signals); stone kind: a .mat is written for it
    register(f"DK_Bloom_{_n}", "stone", _hex)         # flowers: matte petals
register("DK_Cloth_Seafoam", "stone", mats.LIB["DK_Enamel_Seafoam"][1])
register("DK_Cloth_Cream", "stone", mats.LIB["DK_Fabric_Ivory"][1] if "DK_Fabric_Ivory" in mats.LIB else "#EFE8D6")
register("DK_Bloom_Cream", "stone", "#F6EBCB")
register("DK_Leaf_Civic", "stone", "#6A9B5C")        # planting greens: far from every signal (dE2000 > 40)
register("DK_Leaf_CivicDark", "stone", "#46794C")
register("DK_Inlay_Seafoam", "ceramic", mats.LIB["DK_Enamel_Seafoam"][1])  # paving inlay: the kit seafoam, as glazed tile
register("DK_Inlay_Teal", "ceramic", mats.LIB["DK_Enamel_Teal"][1])
register("DK_Brass_Civic", "metal", "#99784D")      # hairline gold: the brass of gp-entrance's door posts (shaded brass), 21.3 dE2000 from the guard colour; DK_Accent_Gold #D0C848 stays for the other families
register("DK_Banner_Comet_Seafoam", "fabric", "#FFFFFF")  # banner_comet_seafoam_v1.png: derive008.py recolours the kit comet banner to the seafoam field
mats.TEXTURES.setdefault("DK_Banner_Comet_Seafoam", ("banner_comet_seafoam_v1.png", 1.0))
register("DK_Pool_Civic", "enamel", "#3A9AA6")      # fountain pool water, opaque: AtomFountain's DK_Water (alpha 0.3 glass, #6FB7C4) reads white under the sky
# civic flower cards: the district's DK_Flower_Coral card with its red-orange blooms moved to the civic coral / flamingo (derive008.py). Override targets only
# (civic-look.json `materials` on the PlanterArc masses): no civic piece draws them, so the exporter copies nothing; overrides008.py writes their .mat.
for _n, _f in (("DK_Flower_CoralSoft", "flower_cluster_coralsoft_v1_alpha.png"), ("DK_Flower_Flamingo", "flower_cluster_flamingo_v1_alpha.png")):
    register(_n, "cutout", "#FFFFFF")
    mats.TEXTURES.setdefault(_n, (_f, 1.0))
from mats import pal  # noqa: E402

HF, R = 5.7, 5.3  # café front: 11.4 m flat between two R 5.3 arcs, 22 m in all (civic-block.json, rule C-0)
SEAM = 0.004


def front_path(na=8, nf=3, hf=HF, r=R, flat_x=None):
    """The café's ground-storey outline, left end -> right end, building on the left (+Y): arc, flat, arc.
    Starts at (-hf-r, r), tangent to the side wall, ends at (hf+r, r). flat_x = explicit x of the flat's vertices (a run
    may end exactly at a door or gate edge)."""
    left = arc(-hf, r, r, 180, 270, na)
    flat = [(x, 0.0) for x in flat_x] if flat_x else [(-hf + 2 * hf * i / nf, 0.0) for i in range(nf + 1)]
    right = arc(hf, r, r, 270, 360, na)
    return left[:-1] + flat + right[1:]


def split_runs(path, keep):
    """Consecutive runs (>= 2 points) of the path's points where keep(x, y) holds: breaks at doors and openings."""
    runs, cur = [], []
    for p in path:
        if keep(*p):
            cur.append(p)
        else:
            runs, cur = runs + ([cur] if len(cur) > 1 else []), []
    return runs + ([cur] if len(cur) > 1 else [])


def hair(s, path, a, z, w, mat, closed=False):
    """Square hairline w x w along a plan path at lateral offset a (negative = outward) and height z."""
    tube(s, lift(path), sec(a - w / 2, a + w / 2, z - w / 2, z + w / 2), mat, closed=closed)


def band(s, path, a0, a1, z0, z1, mat, closed=False):
    tube(s, lift(path), sec(a0, a1, z0, z1), mat, closed=closed)


def dome_lobe(s, c, r, h, mat, seg=10, n=3):
    """Low shrub cushion: open-bottomed dome, r wide, h high, base at c."""
    with s.at(c):
        s.lathe(dome(r, 0, h, n), mat, seg=seg, cap_bottom=False)


def bloom(s, c, r, mat, seg=6):
    with s.at(c):
        s.lathe(dome(r, 0, r * 0.7, 2), mat, seg=seg, cap_bottom=False)


VARIANTS = {
    "A": dict(bloom=("Bloom_Coral", "Bloom_Cream"), leaf=("Leaf_Civic", "Leaf_CivicDark"), seed=11, h=0.36, flowers=1.0),
    "B": dict(bloom=("Bloom_Flamingo", "Bloom_Peach"), leaf=("Leaf_CivicDark", "Leaf_Civic"), seed=23, h=0.30, flowers=0.8),
    "C": dict(bloom=("Bloom_Cream", "Bloom_Peach"), leaf=("Leaf_CivicDark", "Leaf_Civic"), seed=37, h=0.44, flowers=0.45),
}


def trough(s, path, z_top=0.6, w=0.62, closed=False, mat=None):
    """Limestone trough swept along a plan path (centred): plinth, body, coping slab (24 tris per path segment)."""
    mat = mat or pal.Limestone
    h = w / 2
    tube(s, lift(path), sec(-h + 0.02, h - 0.02, 0.0, z_top), mat, closed=closed)
    tube(s, lift(path), sec(-h - 0.03, h + 0.03, z_top, z_top + 0.055), mat, closed=closed)  # coping slab (the planting hides the top)
    tube(s, lift(path), sec(-h - 0.005, h + 0.005, 0.0, 0.1), mat, closed=closed)  # plinth


def planting(s, path, variant, z0=0.6, step=0.5, closed=False, ends=0.12, w=0.5, rows=1, blooms=3):
    """Hedge band, one (or two staggered) rows of leaf cushions and small blooms over a trough along `path` (`variant`
    A / B / C: neighbours differ). The cushions use 10 segments and 3 rings: under the kit's 38 deg sharp angle, so they shade
    smooth; a 2 m run is about 450 tris."""
    v = VARIANTS[variant]
    rng = random.Random(v["seed"] * 1000 + len(path))
    leaf_a, leaf_b = (getattr(pal, n) for n in v["leaf"])
    tube(s, lift(path), [(-w / 2, z0), (w / 2, z0), (w / 2 - 0.02, z0 + 0.1), (w / 2 - 0.08, z0 + 0.15), (-w / 2 + 0.08, z0 + 0.15), (-w / 2 + 0.02, z0 + 0.1)],
         leaf_b, closed=closed)
    pts = along(path + path[:1] if closed else path, step, margin=ends)
    for i, (p, t) in enumerate(pts):
        n = (-t.y, t.x)
        for row, off in enumerate((-0.11, 0.11) if rows == 2 else (0.0,)):
            f = 0.5 * row  # stagger the rows by half a step along the run
            j = off + rng.uniform(-0.03, 0.03)
            q = (p.x + n[0] * j + t.x * step * f, p.y + n[1] * j + t.y * step * f)
            dome_lobe(s, (q[0], q[1], z0 + 0.12), rng.uniform(0.25, 0.31), v["h"] * rng.uniform(0.75, 1.15), leaf_a if (i + row) % 2 else leaf_b, seg=10, n=3)
            for _ in range(blooms if rng.random() < v["flowers"] else 0):
                k = rng.uniform(-0.12, 0.12)
                m = rng.uniform(-0.16, 0.16)
                bloom(s, (q[0] + n[0] * k + t.x * m, q[1] + n[1] * k + t.y * m, z0 + 0.12 + v["h"] * rng.uniform(0.6, 0.85)), rng.uniform(0.09, 0.14),
                      getattr(pal, v["bloom"][rng.randrange(len(v["bloom"]))]), seg=5)
