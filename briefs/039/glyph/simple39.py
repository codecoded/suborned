# Glyph, brief 039 (2026-10-26): the simplified redraw. Sky: "Simplify the visuals, it's way too crowded. Just having the agent facing who they're shooting should be enough
# to not need those lines everywhere." The same moment in A, B and C, the state carried by the body (real figures in real library clips at the play camera: plate39.py),
# and over it only what a body cannot show. python3 -I briefs/039/glyph/simple39.py [A B C]
import sys
sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/039/glyph")
from board39 import *            # g39, heroes39, scene39, glib: the kit's marks
import plate39 as pl
import numpy as np, cv2

# the moment (game px): the cell, then the three rivals (the same places as the first board; R1 stands 10 px nearer the planter so the planter can stand in front of him)
S_, F_, T_, E_ = (850, 596), (800, 652), (893, 657), (822, 731)
R1_, R2_, R3_ = (1308, 552), (1185, 382), (1110, 690)
CLEAN = None
def clean_plate():
    global CLEAN
    if CLEAN is None: CLEAN = np.array(Image.open(pl.TMP + "plate_clean.png").convert("RGB"))
    return CLEAN

# ----------------------------------------------------------------------------- the bodies of each option
def bodies(opt):
    """the figures of one option: (cell, rivals) as plate39.Fig lists. The poses are library clips on the real figures; the ones the build has not imported are named in the proposal."""
    kneel = ("Pistol_Kneeling_Idle", 40, True); duck = ("cower", 47, False); aim = ("aim_pistol", 40, False)
    cell = {"S": pl.Fig("scout", aim[0], aim[1], S_, face=R2_, lib=False), "T": pl.Fig("tech", aim[0], aim[1], T_, face=R3_, lib=False),
            "F": pl.Fig("fixer", "hit_large", 16, F_, face=R2_, lib=False) if opt in "AB" else pl.Fig("fixer", aim[0], aim[1], F_, face=R2_, lib=False),
            "E": pl.Fig("enforcer", "carry_shoulder", 12, E_, heading=200.0, lib=False, extra=[dict(id="councillor", clip="carried_drape", frame=20, heading=20.0)])}
    r1 = pl.Fig("rival_b", kneel[0], kneel[1], R1_, face=S_, lib=kneel[2])
    if opt == "A":
        r2 = pl.Fig("rival_d", kneel[0], kneel[1], R2_, face=F_, lib=True)
        r3 = pl.Fig("rival", "hit_large", 10, R3_, face=T_, lib=False)
    elif opt == "B":
        r2 = pl.Fig("rival_d", "Crouching_Idle", 20, R2_, face=F_, lib=True)
        r3 = pl.Fig("rival", duck[0], duck[1], R3_, face=T_, lib=duck[2])
    else:
        r2 = pl.Fig("rival_d", kneel[0], kneel[1], R2_, face=F_, lib=True)
        r3 = pl.Fig("rival", aim[0], aim[1], R3_, face=T_, lib=False)
    return cell, {"1": r1, "2": r2, "3": r3}

def lay_option(opt, save=True, overrides=None):
    cell, riv = bodies(opt)
    for k, v in (overrides or {}).items():
        g = cell.get(k) or riv.get(k)
        for a, b in v.items(): setattr(g, a, b)
    clean = clean_plate(); plate = clean.copy()
    figs = list(cell.values()) + list(riv.values())
    for g in sorted(figs, key=lambda g: g.feet[1]):
        occ = ()
        if g is riv["2"]: occ = (PL_HEDGE,)
        if g is riv["1"]: occ = (PL_S,)
        pl.lay(plate, g, clean=clean, occluders=occ)
    path = pl.TMP + "scene_%s%s.png" % (opt, "_" + "".join(sorted(overrides)) if overrides else "")
    if save: Image.fromarray(plate).save(path)
    return path, cell, riv

# ----------------------------------------------------------------------------- the marks that are left
# Sky's rule: no aim, fire or focus lines at all; a state goes on the body where a pose can carry it; an overlay only for what a body cannot show.
# Kept: the ring under the selected agent, the focus on the one focused man (ring, lock disc, his pips), the beat's marker (C). Effects the build already has stay: a muzzle flash, a hit spark, stone chips.
import count39 as C39
_REG = C39.REG
def _m(kind, group="street"): _REG.append((kind, group))

def ring_sel(fr, feet):
    """the selected agent's ring on the ground (as built: the capture's bold cyan ring; the unselected cell have none): one line, turquoise-light"""
    _m("ring"); stroke(fr.world, ellipse_pts(feet, 0.47), TURQL, 2.6, closed=True)

def ring_focus(fr, feet):
    """the ring under the focus: the threat red of 049's rings (he fires on the cell), one line; no ticks (the body shows the exchange)"""
    _m("ring"); gline(fr, ellipse_pts(feet, 0.54), RED, 3.0, closed=True)

def focus_marker(fr, head, cams=(SCOUT,)):
    """the lock disc over the focus (067 D, as built), above his head in screen space, with the cameo of who holds it"""
    _m("disc"); reticle39(fr, (head[0], head[1] - 50), "locked", cams=list(cams))

def focus_pips(fr, head, n, left, flash=0):
    _m("pips"); pips(fr, head[0], head[1] - 11, n, left, hostile=True, flash=flash, pitch=7.0, dia=5.6)

def fx_flash(fr, at, ang=-5): _m("flash"); muzzle(fr, at, ang=ang)
def fx_spark(fr, at, t=0.25): _m("spark"); spark(fr, at, t)
def fx_chips(fr, at):
    """cover took the round: stone chips and a ring on the prop, with the ink underlay the other effects wear (the hedge's stone is as pale as the chips)"""
    _m("puff")
    for dx, dy, r in ((-5, -3, 2.4), (4, -6, 2.0), (5, 3, 2.2), (-2, 5, 1.8)):
        c = (at[0] + dx, at[1] + dy); stroke(fr.hud, circ(c, r + 1.1, 12), INKDEEP, 0.1, closed=True, fill=INKDEEP, fill_alpha=170); stroke(fr.hud, circ(c, r, 12), CREAM, 0.1, closed=True, fill=CREAM)
    stroke(fr.hud, circ(at, 8.5, 24), INKDEEP, 3.4, alpha=150, closed=True); stroke(fr.hud, circ(at, 8.5, 24), CREAM, 1.5, alpha=220, closed=True)
def fx_beat(fr, head, face=RIVAL): _m("plate"); beat_marker(fr, head[0], head[1] - 47, "bang", 0.8, face=face)

def muzzle_pt(feet, heading, ahead=0.85, rise=1.3):
    d = pl.screen_dir(heading, ahead, rise); return (feet[0] + d[0], feet[1] + d[1])

def pin_mark(fr, c, size=20):
    """the fallback only: the kit's dark disc with its down chevron beside a pinned man; no draining ring"""
    _m("disc"); lay_ = layer(fr); disc39(lay_, c[0], c[1], size, "chev_down", rgb=CREAM); fr.hud.alpha_composite(lay_)

def extra_pin_discs(fr, cell, riv):
    for k in ("2", "3"): pin_mark(fr, (riv[k].head[0] + 30, riv[k].head[1] - 2 if k == "3" else riv[k].head[1] + 8))

D_FLANK = (1262, 426)
def extra_moving(fr, cell, riv):
    """B while a move is being ordered near cover: the cursor's ring on the ground and, on his cover, the disc with a strike (from there the hedge no longer shields him)"""
    _m("ring"); move_ring(fr, D_FLANK, 0.38)
    _m("disc"); cover_disc(fr, (R2_[0] - 6, R2_[1] + 20), True, "rival", struck=True)
    cursor_arrow(fr, (D_FLANK[0] + 10, D_FLANK[1] + 8))

def hud_in_fight(fr, opt):
    """the roster column and the crew tally: as built (health pips, weapon slot, draw button, the one move each); nothing new here, the move discs live in the roster"""
    pips_by = {"A": (6, 6, 6, 10), "B": (5, 5, 5, 8), "C": (5, 5, 5, 8)}[opt]; hp = {"A": (6, 4, 6, 10), "B": (5, 4, 5, 8), "C": (5, 5, 5, 8)}[opt]
    moves = ("cooling", "ready", "ready", "ready") if opt == "B" else ("ready",) * 4; fracs = (0.15, 1, 1, 1) if opt == "B" else (1, 1, 1, 1)
    under = (1,) if opt in "AB" else ()
    roster_in_fight(fr, pips_by, hp, moves=moves, fracs=fracs, flash_k=1 if opt in "AB" else None, under_fire=under)
    crew_tally(fr, 1560, 112, 3, 0, mark_at=1)

def hero(opt, with_hud=True, extra=None, overrides=None, skip=()):
    """the frame of one option: the bodies (lay_option) and what is left over them"""
    path, cell, riv = lay_option(opt, overrides=overrides)
    fr = Frame(path); _REG.clear()
    ring_sel(fr, S_)
    r2, r3, r1 = riv["2"], riv["3"], riv["1"]
    ring_focus(fr, R2_); focus_marker(fr, r2.head)
    if opt == "A":
        focus_pips(fr, r2.head, 5, 4, flash=1)
        fx_flash(fr, muzzle_pt(S_, cell["S"].heading, 0.62, 1.3)); fx_flash(fr, muzzle_pt(R2_, r2.heading, 0.8, 0.95)); fx_spark(fr, r3.chest)
    elif opt == "B":
        focus_pips(fr, r2.head, 3, 2, flash=1)
        fx_flash(fr, muzzle_pt(S_, cell["S"].heading, 0.62, 1.3)); fx_flash(fr, muzzle_pt(T_, cell["T"].heading, 0.62, 1.3))
        
        if "chips" not in skip: fx_chips(fr, (R2_[0] - 14, R2_[1] + 20))
        fx_spark(fr, r3.chest)
    else:
        focus_pips(fr, r2.head, 3, 3)
        fx_flash(fr, muzzle_pt(S_, cell["S"].heading, 0.62, 1.3)); fx_flash(fr, muzzle_pt(R3_, r3.heading, 0.9, 1.35))
        fx_spark(fr, (r2.head[0] + 2, r2.head[1] + 9)); fx_beat(fr, r3.head)
    if extra: extra(fr, cell, riv)
    street = dict(C39.collections.Counter(k for k, g in _REG if g == "street"))
    n_street = sum(1 for k, g in _REG if g == "street")
    if with_hud:
        before = len(_REG); hud_in_fight(fr, opt)
    return fr, dict(street=n_street, street_by_kind=street, cell=cell, riv=riv)

if __name__ == "__main__":
    for o in (sys.argv[1:] or ["A", "B", "C"]):
        path, cell, riv = lay_option(o)
        print(o, path, {k: (g.bbox, g.head) for k, g in list(cell.items()) + list(riv.items())})
        Image.open(path).crop((700, 280, 1500, 800)).resize((1600, 1040), Image.LANCZOS).save(pl.TMP + "scene_%s_crop.png" % o)
        fr, info = hero(o); fr.crop(HERO_BOX).convert("RGB").save(pl.TMP + "simple_%s.png" % o); print(o, "street marks", info["street"], info["street_by_kind"])
