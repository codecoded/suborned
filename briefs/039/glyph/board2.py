# Glyph, brief 039, board 2 (all three options): one move each, ordered and shown. python3 -I briefs/039/glyph/board2.py
import sys
sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/039/glyph")
from board39 import *

OUTB = R + "briefs/039/glyph/"
GRILLE = (1040, 604)
R3B = (1146, 664)                      # a second rival, within 3 m of R3, for the Enforcer's burst

def fit(fr, cx, cy, w=336, h=246, to=(560, 410)):
    return fr.crop((cx - w // 2, cy - h // 2, cx + w // 2, cy + h // 2)).convert("RGB").resize(to, Image.LANCZOS)

def with_r3b(fr):
    stand_rival(fr, cutouts()["A"], R3B, flip=True)

# ----------------------------------------------------------------------------- Mark
def mark_offer():
    fr = scene(); ring39(fr, R2F, threat=True, ours=False); cover_disc(fr, MARK_HG, False, "rival"); cover_arc(fr, R2F, 200, 70, False, CREAM); rival_pips(fr, "r2", 3, 3)
    brackets(fr, R2F, preview=True)
    pill39(fr, (R2F[0], R2H[1] - 20), "Hold", [("txt", "Mark · 1 s")], "mark", faces=[SCOUT])
    return fit(fr, 1175, 338)
def mark_shown():
    fr = scene(); ring39(fr, R2F, threat=True, ours=True); cover_disc(fr, MARK_HG, False, "rival", struck=True); cover_arc(fr, R2F, 200, 70, False, CREAM, dim=True); rival_pips(fr, "r2", 3, 3)
    brackets(fr, R2F, frac=0.78); walk_to(fr, R2F, (1118, 452))
    bubble(fr, (HEAD["scout"][0] + 4, HEAD["scout"][1] - 6), "Left planter.") if False else None
    for a in ("tech", "fixer"): tracer(fr, gun(a), hit_pt("r2")); spark(fr, hit_pt("r2"), 0.25, 0.7)
    return fit(fr, 1175, 338)

# ----------------------------------------------------------------------------- Talk down
def talk_offer():
    fr = scene(); ring39(fr, R3F, threat=True); rival_pips(fr, "r3", 3, 1)
    offer_disc(fr, (R3H[0], R3H[1] - 18), "megaphone")
    pill39(fr, (R3H[0], R3H[1] - 36), "Hold", [("txt", "Talk down · 2 s")], "megaphone", faces=[FIXER])
    return fit(fr, 1110, 600)
def talk_shown():
    fr = scene(); ring39(fr, R3F, side="rival", threat=False, reach="out"); rival_pips(fr, "r3", 3, 1)
    shout(fr, CELL["fixer"], 0.62, 12.0)
    stroke(fr.world, [CELL["fixer"], (R3F[0] - 30, R3F[1] - 4)], INKDEEP, 4.0, dash=(2, 4.6), alpha=120); stroke(fr.world, [CELL["fixer"], (R3F[0] - 30, R3F[1] - 4)], CREAM, 1.8, dash=(2, 4.6))
    lay = layer(fr); disc39(lay, R3H[0] + 4, R3H[1] - 22, 20, "run"); fr.hud.alpha_composite(lay)
    bubble(fr, (R3H[0], R3H[1] - 30), "Go, go!") if False else None
    crew_tally(fr, 1560, 112, 3, 1, broken=1, mark_at=1)
    return fr.crop((800, 505, 1360, 735)).convert("RGB").resize((560, 230), Image.LANCZOS) if False else fit(fr, 1010, 640, 460, 337, (560, 410))

# ----------------------------------------------------------------------------- Steam
def steam_offer():
    fr = scene(); grille(fr, GRILLE, lit=True, marker=True); ring_m(fr, GRILLE, 4.0, CREAM, dash=(6, 5), w=2.0, alpha=230)
    pill39(fr, (GRILLE[0], GRILLE[1] - 30 - 24), "Hold", [("txt", "Steam · 1 s")], "steam", faces=[TECH])
    ring39(fr, R3F, threat=True); rival_pips(fr, "r3", 3, 3)
    return fit(fr, 1040, 600, 380, 278)
def steam_shown():
    fr = scene(); grille(fr, GRILLE, lit=False, marker=False); cloud(fr, GRILLE, 4.0, 3, 1.9, 1.15)
    ring_m(fr, GRILLE, 4.0, CREAM, dash=(3, 7), w=1.6, alpha=170)
    ring39(fr, R3F, threat=False, reach="out"); rival_pips(fr, "r3", 3, 3)
    lay = layer(fr); disc39(lay, GRILLE[0], GRILLE[1] - 64, 22, "steam", ring=TURQL); fr.hud.alpha_composite(lay)
    window = (GRILLE[0] + 44, GRILLE[1] - 64); WH = 38.0; f = JB(14.5); lay = layer(fr); w = 11 + 16 + 6 + tw("8 s", f) + 12; x = window[0] - 4
    plate_dark(lay, x, window[1] - WH / 2, w, WH); ico(lay, "steam", x + 11 + 8, window[1] - 1, 15); text(lay, (x + 11 + 16 + 6, window[1] - 0.5), "8 s", f)
    stroke(lay, [(x + 11, window[1] + 10.5), (x + w - 11, window[1] + 10.5)], INKDEEP, 2.5, alpha=153, round_caps=True); stroke(lay, [(x + 11, window[1] + 10.5), (x + 11 + (w - 22) * 0.8, window[1] + 10.5)], TURQL, 2.5, round_caps=True); fr.hud.alpha_composite(lay)
    return fit(fr, 1040, 600, 380, 278)

# ----------------------------------------------------------------------------- Covering fire
def cover_offer():
    fr = scene(); with_r3b(fr); ring39(fr, R3F, threat=True); rival_pips(fr, "r3", 3, 3)
    h2 = (R3B[0], R3B[1] - 64); pips(fr, h2[0], h2[1] - 11, 3, 3, hostile=True)
    ring_m(fr, R3F, 3.0, CREAM, dash=(5, 5), w=1.8, alpha=200)
    lay = layer(fr); disc39(lay, HEAD["enforcer"][0], HEAD["enforcer"][1] - 27, 24, "s_carrying"); fr.hud.alpha_composite(lay)
    aim_cursor(fr, (R3F[0], (R3F[1] + R3H[1]) / 2 + 4), odds=0.35, hold=0.0)
    pill39(fr, (R3H[0], R3H[1] - 22), "Right hold", [("txt", "Covering fire · 4 s")], "burst", faces=[ENF], harm=True)
    return fit(fr, 1030, 640, 520, 381)
def cover_shown():
    fr = scene(); with_r3b(fr); ring39(fr, R3F, threat=True); rival_pips(fr, "r3", 3, 3)
    ring_m(fr, R3F, 3.0, CREAM, dash=(5, 5), w=1.8, alpha=200)
    a = gun("enforcer"); b = hit_pt("r3"); fan(fr, a, b, spread_deg=7.5, n=3, miss_past=0.8); muzzle(fr, (a[0] + 4, a[1]), ang=-5)
    lay = layer(fr); disc39(lay, HEAD["enforcer"][0] - 14, HEAD["enforcer"][1] - 27, 24, "s_carrying"); disc39(lay, HEAD["enforcer"][0] + 14, HEAD["enforcer"][1] - 27, 24, "burst"); fr.hud.alpha_composite(lay)
    pin_disc(fr, (R3H[0] + 31, R3H[1] - 11), 0.9); pin_disc(fr, (R3B[0] + 31, R3B[1] - 75), 0.9)
    rival_pips(fr, "r3", 3, 3); pips(fr, R3B[0], R3B[1] - 64 - 11, 3, 3, hostile=True)
    return fit(fr, 1030, 640, 520, 381)

# ----------------------------------------------------------------------------- the three ways to order a move (Mark shown)
def mark_ring():
    """a left click on a rival opens his ring: the likely way at 12 o'clock (Mark, the Scout's face under it), Fire dashed and last, the full panel behind the dots"""
    fr = scene(); ring39(fr, R2F, threat=True); cover_disc(fr, MARK_HG, False, "rival"); cover_arc(fr, R2F, 200, 70, False, CREAM); rival_pips(fr, "r2", 3, 3)
    C = (R2F[0] + 6, R2F[1] - 26)
    ring67(fr, C, [dict(icon="mark", word="Mark", fields=[("time", "1 s")], cams=[SCOUT]), dict(icon="fire", word="Fire", fields=[("time", "9 s")], violent=True), dict(icon="dot", more=True)], lit=0)
    return fr.crop((C[0] - 200, C[1] - 170, C[0] + 262, C[1] + 168)).convert("RGB").resize((560, 410), Image.LANCZOS)

def mark_hold():
    """a left hold on a rival in a fight takes the only calm way: the pill is the offer pill (Hold, the verb and its time, the doer's face), the sweep fills while held"""
    fr = scene(); ring39(fr, R2F, threat=True); cover_disc(fr, MARK_HG, False, "rival"); cover_arc(fr, R2F, 200, 70, False, CREAM); rival_pips(fr, "r2", 3, 3); brackets(fr, R2F, preview=True)
    pill39(fr, (R2F[0], R2H[1] - 20), "Hold", [("txt", "Mark · 1 s")], "mark", faces=[SCOUT], frac=0.55)
    return fit(fr, 1175, 338)

def mark_arm():
    """a click on the Scout's ready disc arms the move: the disc lights, and every valid target wears its glyph in a dashed ring"""
    fr = scene()
    for w in ("r1", "r2", "r3"):
        f, h = RIVS[w]; ring39(fr, f, threat=True); rival_pips(fr, w, 3, 3); offer_disc(fr, (h[0], h[1] - 26), "mark")
    cover_disc(fr, MARK_PL, True, "rival"); cover_disc(fr, MARK_HG, False, "rival")
    roster_in_fight(fr, (5, 5, 5, 8), (5, 5, 5, 8), moves=("armed", "ready", "ready", "ready"))
    world = fr.crop((1000, 255, 1440, 725)).convert("RGB").resize((448, 478), Image.LANCZOS)
    row = fr.crop((0, 22, 236, 112)).convert("RGB").resize((472, 180), Image.LANCZOS)
    im = Image.new("RGB", (560, 180 + 8 + 478), BG); im.paste(row, (44, 0)); im.paste(world, (56, 188)); return im

def build():
    cols = [
        ("Scout · Mark", mark_offer(), mark_shown(), "a rival in cover she sees, 30 m", "his cover stops counting for 10 s; his next move is drawn", roster_row(0, "busy", 0.6, "1 s"), roster_row(0, "cooling", 0.35)),
        ("Fixer · Talk down", talk_offer(), talk_shown(), "a hurt rival within 12 m", "he breaks: gun down, running; the crew's count takes him", roster_row(1, "busy", 0.5, "2 s"), roster_row(1, "cooling", 0.35)),
        ("Tech · Steam", steam_offer(), steam_shown(), "a heat-main grille within 24 m", "no line crosses its 4 m for 8 s", roster_row(2, "busy", 0.4, "1 s"), roster_row(2, "cooling", 0.35)),
        ("Enforcer · Covering fire", cover_offer(), cover_shown(), "a rival in his reach, walking Vale", "everyone within 3 m of him is pinned; it never hits", roster_row(3, "busy", 0.7, "4 s"), roster_row(3, "cooling", 0.35)),
    ]
    row_off = [dict(im=c[1], cap=c[0], small="Offer: " + c[3] + ". The pill is the price: the verb and the time; the face is who does it.") for c in cols]
    row_shown = [dict(im=c[2], cap="Shown", small=c[4].capitalize() + ".") for c in cols]
    row_ro = []
    for c in cols:
        a_, b_ = c[5], c[6]; im = Image.new("RGB", (a_.width, a_.height + b_.height + 8), BG); im.paste(a_, (0, 0)); im.paste(b_, (0, a_.height + 8))
        row_ro.append(dict(im=im, cap="On the roster", small="While it runs the busy chip; after it the disc fills over 20 s, then glows."))
    row_how = [dict(im=mark_ring(), cap="1 \u00b7 Click the target", small="The ring opens on him: Mark first, lit, the Scout's face under it; Fire dashed and last."),
               dict(im=mark_hold(), cap="2 \u00b7 Hold on the target", small="Mark is the only calm way, so a left hold takes it: the pill's sweep fills, let go commits."),
               dict(im=mark_arm(), cap="3 \u00b7 Click the disc", small="The disc lights and every valid target wears the move's glyph; click one. Esc puts it back.")]
    return grid([row_off, row_shown, row_ro, row_how], OUTB + "board-2-the-four-moves.png", "039 · one move each · ordered and shown · the same in A, B and C",
        "An order like any other (row 4 shows the three ways, Mark as the example). The Enforcer carrying Vale has no other fire: his right hold on a hostile is Covering fire. The roster disc says ready, cooling or shut; the busy chip takes its place while a move runs.")

if __name__ == "__main__":
    for nm, fn in (("mo", mark_offer), ("ms", mark_shown), ("to", talk_offer), ("ts", talk_shown), ("so", steam_offer), ("ss", steam_shown), ("co", cover_offer), ("cs", cover_shown)):
        im = fn(); im.save(TMP + "b2_%s.png" % nm); print(nm, im.size)
    print(build())
