# Glyph, brief 039, board 1 (all three options): readable cover. python3 -I briefs/039/glyph/board1.py
import sys
sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/039/glyph")
from board39 import *

OUTB = R + "briefs/039/glyph/"
BENCH = (968, 747); BENCH_K = (990, 766)         # the long bench by the Enforcer: a free cover; the kneel point behind it
TECH_F = CELL["tech"]; BOX = (280, 205)

def box(cx, cy, w=280, h=205): return (cx - w // 2, cy - h // 2, cx + w // 2, cy + h // 2)

def t_quiet():
    fr = scene(); rest_marks(fr); cover_disc(fr, (850, 548 - 6), True, None, live=False) if False else None
    return tile(fr, box(880, 480))
def t_live():
    fr = scene(); ring39(fr, R1F); ring39(fr, R2F, threat=True)
    cover_disc(fr, MARK_PL, True, "rival"); cover_arc(fr, R1F, 185, 60, True, CREAM); cover_disc(fr, MARK_HG, False, "rival"); cover_arc(fr, R2F, 200, 70, False, CREAM)
    rival_pips(fr, "r1", 3, 3); rival_pips(fr, "r2", 3, 3)
    return tile(fr, box(1240, 455))
def t_snap():
    fr = scene(); rest_marks(fr)
    cover_arc(fr, BENCH_K, 0, 75, True, TURQL); tether(fr, (1034, 776), (BENCH_K[0], BENCH_K[1] + 4)); move_ring(fr, (BENCH_K[0], BENCH_K[1] + 4), 0.05)
    cover_disc(fr, BENCH, True, "cell", lit=True); cursor_arrow(fr, (1030, 778))
    return tile(fr, box(990, 700))
def t_flank():
    fr = scene(); ring39(fr, R2F, threat=True); cover_disc(fr, MARK_HG, False, "rival", struck=True); cover_arc(fr, R2F, 200, 70, False, CREAM, dim=True); rival_pips(fr, "r2", 3, 3)
    ring = (1272, 430); sight(fr, ring, (R2F[0] + 10, R2F[1] - 4)); move_ring(fr, ring, 0.55)
    cursor_arrow(fr, (1286, 448))
    return tile(fr, box(1190, 400))

def cursors():
    ims = []
    for badge, where, caption in (("counts", "r1", "counts"), ("half", "r2", "counts half"), ("struck", "r2", "does not count")):
        fr = scene(); f, h = RIVS[where]; ring39(fr, f, threat=True)
        if where == "r1": cover_disc(fr, MARK_PL, True, "rival"); cover_arc(fr, R1F, 185, 60, True, CREAM)
        else:
            cover_disc(fr, MARK_HG, False, "rival", struck=badge == "struck"); cover_arc(fr, R2F, 200, 70, False, CREAM, dim=badge == "struck")
            if badge == "struck": brackets(fr, R2F, frac=0.72)
        rival_pips(fr, where, 3, 3); aim_cursor(fr, (f[0], (f[1] + h[1]) / 2 + 6), odds={"counts": 0.1, "half": 0.35, "struck": 0.8}[badge], badge=badge)
        c = fr.crop((f[0] - 85, f[1] - 112, f[0] + 85, f[1] + 58)).convert("RGB"); d = ImageDraw.Draw(c)
        ims.append(c)
    return hstack(ims, gap=14)

def lands():
    ims = []
    for kind in ("hit", "miss", "cover"):
        fr = scene()
        if kind in ("hit", "miss"):
            f, h = RIVS["r3"]; ring39(fr, f, threat=True, ours=True); rival_pips(fr, "r3", 3, 3 if kind == "miss" else 2, flash=0 if kind == "miss" else 1)
            a = gun("tech"); b = hit_pt("r3"); tracer(fr, a, b); muzzle(fr, (a[0] + 4, a[1] - 1), ang=-5)
            if kind == "hit": spark(fr, b, 0.25)
            else: puff(fr, (b[0] + 18, b[1] + 8), 7)
            c = fr.crop((f[0] - 85, f[1] - 108, f[0] + 85, f[1] + 62)).convert("RGB")
        else:
            f, h = R1F, R1H; ring39(fr, f, ours=True); rival_pips(fr, "r1", 3, 3); cover_disc(fr, MARK_PL, True, "rival", took=True); cover_arc(fr, R1F, 185, 60, True, CREAM)
            a = gun("fixer"); tracer(fr, a, (1290, 540)); chip_puff(fr, (1288, 538)); muzzle(fr, (a[0] + 4, a[1] - 1), ang=-5)
            c = fr.crop((f[0] - 85, f[1] - 112, f[0] + 85, f[1] + 58)).convert("RGB")
        ims.append(c)
    return hstack(ims, gap=14)

def your_cover(flanked):
    fr = scene(); rest_marks(fr); feet = (TECH_F[0] + 4, TECH_F[1] + 6)
    ring39(fr, R3F, threat=True, ours=True); rival_pips(fr, "r3", 3, 3)
    cover_disc(fr, (928, 706), True, "cell", struck=flanked)
    cover_arc(fr, feet, 180 if flanked else 8, 70, True, TURQL, dim=flanked)
    under_fire_arc(fr, feet, R3F)
    streak(fr, (R3F[0] - 12, R3F[1] - 36), (TECH_F[0] + 12, TECH_F[1] - 30)); tracer(fr, gun("tech"), hit_pt("r3"))
    return tile(fr, box(1010, 665))

def key():
    """the grammar on one panel: a key for the crew, not a game piece"""
    W, H = 1100, 410; fr = blank_frame(W / 2, H / 2); rows = [("shield", None, False), ("shield_half", None, False), ("shield", None, True), ("ring", "cell", False), ("ring", "rival", False), ("ring", None, False)]
    txt = ["full cover: counts inside its arc, never inside 5 m", "partial cover: counts half", "struck: his cover does not count (flanked, or marked)", "turquoise-light ring: the cell holds it", "violet ring: a rival holds it (amber: police)", "chrome ring: nobody does"]
    for i, (kind, owner, struck) in enumerate(rows):
        y = 28 + i * 29.5; cover_disc(fr, (22, y), kind != "shield_half", owner if kind == "ring" else None, struck=struck)
    im = fr.compose().convert("RGB"); d = ImageDraw.Draw(im); s_ = body_font(22)
    for i, t in enumerate(txt): d.text((70, (28 + i * 29.5) * 2), t, font=s_, fill=(247, 244, 236), anchor="lm")
    return im

def build():
    row1 = [dict(im=t_quiet(), cap="Quiet until it matters", small="Within 10 m of a drawn agent: a small shield disc at 55 %. No arc."),
            dict(im=t_live(), cap="Live when someone holds it", small="The disc grows a ring in the holder's colour and the arc shows the side it shields: solid full, dashed partial."),
            dict(im=t_snap(), cap="The snap", small="Within 1.5 m the move ring leaps to the point, wears the shield, and the disc lights."),
            dict(im=t_flank(), cap="The flank, before you walk", small="A dotted line of fire from where the move ends: his disc flips to struck, because it comes from outside his arc.")]
    row2 = [dict(im=cursors(), cap="The cursor says it", small="The shield badge: counts, counts half, does not count. The ticks open as the odds fall."),
            dict(im=lands(), cap="Where a round lands", small="On him (a spark and a pip pops), past him (a ring: a miss), on the cover (stone chips and the disc's ring leaves once).")]
    row3 = [dict(im=your_cover(False), cap="Your cover works", small="The red arc (who shoots you, built) sits inside your cover's arc."),
            dict(im=your_cover(True), cap="Your cover is flanked", small="The red arc lies outside it: the disc is struck. Move."),
            dict(im=key(), cap="The key", small="A shield is a place, an arc is a side, a ring is a holder, a strike is no.")]
    return grid([row1, row2, row3], OUTB + "board-1-readable-cover.png", "039 · readable cover · the same in A, B and C",
        "The lab's cover marks (HudToolkit.Cover, Game.CoverMarks) restyled in the kit, quiet by default. The values differ by option (B: full ×0.25, partial ×0.5; A and C as today); the marks do not. Over the 0.2.122 opening square; rivals are stand-ins; seconds are illustrative.")

if __name__ == "__main__":
    for nm, fn in (("quiet", t_quiet), ("live", t_live), ("snap", t_snap), ("flank", t_flank), ("curs", cursors), ("lands", lands), ("yc0", lambda: your_cover(False)), ("yc1", lambda: your_cover(True)), ("key", key)):
        im = fn(); im.save(TMP + "b1_%s.png" % nm); print(nm, im.size)
    print(build())
