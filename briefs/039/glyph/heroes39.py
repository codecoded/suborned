# Glyph, brief 039 (2026-10-26): the hero frames. One scene (scene39.py), one moment, three treatments. Every mark is drawn from the kit by g39.py; poses are stand-ins.
# The moment: the cell, all four drawn, fights three rivals in the square: R1 behind the west planter (the blocker), R2 behind the café hedge, R3 in the open.
import sys
sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/039/glyph")
from g39 import *

R1F, R1H = RIVS["r1"]; R2F, R2H = RIVS["r2"]; R3F, R3H = RIVS["r3"]
def chest(f, h): return (f[0], (f[1] + h[1]) / 2 + 3)                 # the middle of the body
def gun(k): h = HEAD[k]; return (h[0] + 9, h[1] + 20)                  # a muzzle: right of the chest
def hit_pt(which): f, h = RIVS[which]; return chest(f, h)
MARK_PL = (1263, 479)       # the west planter's cover disc (on the prop), the kneel point east of it where R1 stands
MARK_HG = (1150, 399)       # the café hedge's
REST = [(831, 433), (996, 436), (968, 747)]   # other covers in reach (the hedge wall, the long bench): rest marks only

# ----------------------------------------------------------------------------- the cell in the fight (every option)
def cell_in_fight(fr, pips_n, hp, enf_n, fire=("scout", "fixer", "tech"), carry=True):
    """firing marks over the shooters, the Enforcer's carry disc, the cell's pips over their heads (the health the option gives them)"""
    for k in fire: firing_mark(fr, (HEAD[k][0], HEAD[k][1] - 27))
    if carry: lay = layer(fr); disc39(lay, HEAD["enforcer"][0], HEAD["enforcer"][1] - 27, 24, "s_carrying"); fr.hud.alpha_composite(lay)
    for k, n, h in (("scout", pips_n, hp[0]), ("fixer", pips_n, hp[1]), ("tech", pips_n, hp[2]), ("enforcer", enf_n, hp[3])):
        pips(fr, HEAD[k][0], HEAD[k][1] - 11, n, h, hostile=False, pitch=7.0 if n <= 6 else 5.2, dia=5.2 if n <= 6 else 4.4)

def rival_pips(fr, which, n, left, flash=0, dx=0): h = RIVS[which][1]; pips(fr, h[0] + dx, h[1] - 11, n, left, hostile=True, flash=flash, pitch=7.0, dia=5.6)
def rest_marks(fr):
    for p in REST: cover_disc(fr, p, True, None, live=False)
def focus(fr, which, cams=(SCOUT, FIXER), state="locked", reason="aim"):
    h = RIVS[which][1]; reticle39(fr, (h[0], h[1] - 50), state, cams=list(cams), reason=reason)
def volley(fr, shooter, which, hit=True, flash=False):
    a = gun(shooter); b = hit_pt(which); tracer(fr, a, b)
    if flash: muzzle(fr, (a[0] + 4, a[1] - 1), ang=-5)
    if hit: spark(fr, b, 0.25)
    else: puff(fr, (b[0] + (b[0] - a[0]) * 0.07, b[1] + (b[1] - a[1]) * 0.07), 7)

# ----------------------------------------------------------------------------- the moment, shared: who shoots whom
def shots_common(fr, hits=True):
    """The Scout was ordered onto R2 (the reticle); the Fixer answers R2 on his own; the Tech answers R3. R2 and R3 shoot the Fixer and the Tech. The Enforcer carries Vale."""
    volley(fr, "scout", "r2", True, flash=True); volley(fr, "fixer", "r2", False); volley(fr, "tech", "r3", True)
    streak(fr, (R3F[0] - 12, R3F[1] - 36), (HEAD["tech"][0] + 10, HEAD["tech"][1] + 26)); streak(fr, (R2F[0] - 10, R2F[1] - 36), (HEAD["fixer"][0] + 10, HEAD["fixer"][1] + 24), ts=(0.3, 0.7))
    under_fire_arc(fr, CELL["tech"], R3F); under_fire_arc(fr, CELL["fixer"], R2F)

# ----------------------------------------------------------------------------- A, tougher
def hero_A(fr):
    """A: the same fight with bodies that last. Rivals five pips, the cell six, the Enforcer ten. Nothing else is new on the street."""
    ring39(fr, R1F); ring39(fr, R2F, threat=True, ours=True); ring39(fr, R3F, threat=True, ours=True)
    rest_marks(fr); cover_disc(fr, MARK_PL, True, "rival"); cover_disc(fr, MARK_HG, False, "rival")
    shots_common(fr); cell_in_fight(fr, 6, (6, 4, 6, 10), 10)
    rival_pips(fr, "r1", 5, 5); rival_pips(fr, "r2", 5, 4, flash=1); rival_pips(fr, "r3", 5, 3, flash=2)
    focus(fr, "r2", cams=(SCOUT,))
    roster_in_fight(fr, (6, 6, 6, 10), (6, 4, 6, 10), flash_k=1)
    crew_tally(fr, 1560, 112, 3, 0, mark_at=1)

# ----------------------------------------------------------------------------- B, cover and pin
def hero_B(fr):
    """B: cover is worth something. R2 behind the hedge is marked (his shield struck, shots land, next move shown); R3 in the open is pinned by the Tech's fire; R1 holds the planter."""
    ring39(fr, R1F); ring39(fr, R2F, threat=True, ours=True); ring39(fr, R3F, threat=True, ours=True)
    rest_marks(fr); cover_disc(fr, MARK_PL, True, "rival"); cover_arc(fr, R1F, 185, 60, True, CREAM); cover_disc(fr, MARK_HG, False, "rival", struck=True)
    brackets(fr, R2F, frac=0.72); walk_to(fr, R2F, (1118, 452))
    shots_common(fr); cell_in_fight(fr, 5, (5, 4, 5, 8), 8)
    rival_pips(fr, "r1", 3, 3); rival_pips(fr, "r2", 3, 2, flash=1); rival_pips(fr, "r3", 3, 2, flash=1)
    pin_disc(fr, (R3H[0] + 31, R3H[1] - 11), 0.78); pin_disc(fr, (R2H[0] + 31, R2H[1] - 11), 0.5)
    focus(fr, "r2", cams=(SCOUT,))
    roster_in_fight(fr, (5, 5, 5, 8), (5, 4, 5, 8), moves=("cooling", "ready", "ready", "ready"), fracs=(0.15, 1, 1, 1), flash_k=1)
    crew_tally(fr, 1560, 112, 3, 0, mark_at=1)

# ----------------------------------------------------------------------------- C, showdown beats
def hero_C(fr, cause="rival"):
    """C: a hostile arrives and the clock drops to a quarter for 2 s of game time. The subject is ringed and carries the window marker; who aims at whom is drawn; every move refills."""
    ring39(fr, R1F); ring39(fr, R2F, threat=True); ring39(fr, R3F, threat=True)
    rest_marks(fr); cover_disc(fr, MARK_PL, True, "rival"); cover_disc(fr, MARK_HG, False, "rival")
    for a, b in (((R1F[0] - 10, R1F[1] - 38), (HEAD["enforcer"][0] + 8, HEAD["enforcer"][1] + 26)), ((R2F[0] - 10, R2F[1] - 36), (HEAD["fixer"][0] + 8, HEAD["fixer"][1] + 24)), ((R3F[0] - 12, R3F[1] - 36), (HEAD["tech"][0] + 8, HEAD["tech"][1] + 24))): threat(fr, a, b)
    volley(fr, "scout", "r2", True); volley(fr, "tech", "r3", False)
    cell_in_fight(fr, 5, (5, 5, 5, 8), 8)
    rival_pips(fr, "r1", 3, 3); rival_pips(fr, "r2", 3, 3); rival_pips(fr, "r3", 3, 3)
    beat_ring(fr, R3F, 0.95, 0.8); beat_marker(fr, R3H[0], R3H[1] - 82, "bang", 0.8, face=RIVAL)
    roster_in_fight(fr, (5, 5, 5, 8), (5, 5, 5, 8))
    for k in range(4): refill(fr, k)
    crew_tally(fr, 1560, 112, 3, 0, mark_at=1)

HERO_BOX = (716, 230, 1496, 830)        # game px: the hero crop (780 x 600 -> 1560 x 1200 at 2x)
ROST_BOX = (0, 0, 232, 470)

if __name__ == "__main__":
    for nm, fn in (("A", hero_A), ("B", hero_B), ("C", hero_C)):
        fr = scene(); fn(fr)
        fr.crop(HERO_BOX).convert("RGB").save(TMP + "hero%s.png" % nm); fr.crop(ROST_BOX).convert("RGB").save(TMP + "hero%s_roster.png" % nm)
        print(nm, budget(fr, SQHUD, True))
