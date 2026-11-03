# Glyph, brief 039, board 0 (all three options): the one input. python3 -I briefs/039/glyph/board0.py
import sys
sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/039/glyph")
from board39 import *

OUTB = R + "briefs/039/glyph/"
CX, CY = 1185, 352                      # R2's chest (the hedge): the cursor's place
FBOX = (1050, 222, 1330, 427)           # the focus tiles' crop: 280 x 205 game px -> 560 x 410

def base_R2(pips_n=3, left=3, cover=True, threat=True, ours=False, extra=None):
    fr = scene(); ring39(fr, R2F, threat=threat, ours=ours)
    if cover: cover_disc(fr, MARK_HG, False, "rival", live=True); cover_arc(fr, R2F, 200, 70, False, CREAM)
    rival_pips(fr, "r2", pips_n, left)
    return fr

def f_hover():
    fr = base_R2(); aim_cursor(fr, (CX, CY), odds=0.35, badge="half")
    return tile(fr, FBOX)
def f_letgo():
    fr = base_R2(); aim_cursor(fr, (CX, CY), odds=0.35, badge="half")
    pill39(fr, (R2F[0], R2H[1] - 12), "Let go", [("icon", "s_downed"), ("txt", "9 s")], "fire", faces=[SCOUT], harm=True, ready=True)
    return tile(fr, FBOX)
def f_locked():
    fr = base_R2(ours=True); focus(fr, "r2", cams=(SCOUT,)); tracer(fr, gun("scout"), hit_pt("r2")); muzzle(fr, (gun("scout")[0] + 4, gun("scout")[1] - 1), ang=-5)
    return tile(fr, (1040, 215, 1320, 420))

def f_standdown():
    """F again: holster. The reticle lets go (0.25 s fade), the firing stops, the weapon slot hollows"""
    fr = scene(); ring39(fr, R2F, threat=True); cover_disc(fr, MARK_HG, False, "rival", live=True); cover_arc(fr, R2F, 200, 70, False, CREAM); rival_pips(fr, "r2", 3, 3)
    h = R2H; reticle39(fr, (h[0], h[1] - 50), "released", cams=[SCOUT], scale=0.85, alpha=0.45)
    a = tile(fr, (1090, 225, 1260, 430))
    g = scene(); roster_in_fight(g, (5, 5, 5, 8), (5, 4, 5, 8), drawn=False, discs=False, firing=(), under_fire=())
    b = tile(g, (0, 30, 110, 235))
    return hstack([a, b], gap=10)

def waits(reason):
    fr = scene(); R_ = {"aim": "r3", "walk": "r3", "noline": "r1"}[reason]; f, h = RIVS[R_]; ring39(fr, f, threat=reason == "aim", ours=False, reach="out" if reason == "walk" else "in"); rival_pips(fr, R_, 3, 3)
    if reason == "noline":
        cover_disc(fr, MARK_PL, True, "rival"); cover_arc(fr, R1F, 185, 60, True, CREAM); tracer(fr, gun("scout"), (1262, 530)); chip_puff(fr, (1258, 528))
    if reason == "walk":
        stroke(fr.world, [CELL["scout"], (f[0] - 60, f[1] - 14)], INKDEEP, 4.4, dash=(2.0, 4.6), alpha=150); stroke(fr.world, [CELL["scout"], (f[0] - 60, f[1] - 14)], CREAM, 2.0, dash=(2.0, 4.6))
    reticle39(fr, (h[0], h[1] - 50), "held", cams=[SCOUT], reason=reason, turn=24)
    box = {"aim": (1010, 540, 1210, 710), "walk": (1010, 540, 1210, 710), "noline": (1218, 400, 1418, 570)}[reason]
    return tile(fr, box)


# ----------------------------------------------------------------------------- third person (the build's own capture, brief 049's tp-aim): an officer by the cypress is the stand-in hostile
TPSRC = R + "briefs/049/build/tp-aim.png"
TPBOX = (470, 412, 848, 790)                     # game px: 378 x 378, the officer at its middle; the old ring on the car stays outside it
OFC_HEAD = (693, 567); OFC_CHEST = (696, 612)    # the officer's head top and chest, game px
def tp_base():
    """the capture with its own old ground ring (a white ellipse on the car) healed away: only near-white, near-grey pixels of that thin line are inpainted"""
    import os
    out = TMP + "tp_aim_clean.png"
    if not os.path.exists(out):
        im = np.array(Image.open(TPSRC).convert("RGB")); x0, y0, x1, y1 = 1600, 1090, 1900, 1190; sub = im[y0:y1, x0:x1].copy()
        mx, mn = sub.max(axis=2).astype(int), sub.min(axis=2).astype(int); m = ((mn > 200) & (mx - mn < 16)).astype(np.uint8) * 255; m = cv2.dilate(m, np.ones((5, 5), np.uint8))
        im[y0:y1, x0:x1] = cv2.inpaint(sub, m, 4, cv2.INPAINT_TELEA); Image.fromarray(im).save(out)
    return out
def tp_tile(kind):
    fr = Frame(tp_base()); pips(fr, OFC_HEAD[0], OFC_HEAD[1] - 11, 3, 3, hostile=True, pitch=7.0, dia=5.6)
    if kind == "hover":
        aim_cursor(fr, OFC_CHEST, odds=0.4)
        pill39(fr, (OFC_HEAD[0], OFC_HEAD[1] - 24), "Right hold", [("icon", "s_downed"), ("txt", "4 s")], "fire", faces=[ENF], harm=True)
    else:
        reticle39(fr, (OFC_HEAD[0], OFC_HEAD[1] - 50), "locked", cams=[ENF])
    return fr.crop(TPBOX).convert("RGB")

def hint_tile():
    """the first-time hint: it shares the goal line's place, bottom centre (Fable's words, <= 6, retired for good once the verb is used). Drawn over the 0.2.122 frame's own goal line."""
    fr = scene(); lay = layer(fr); f = JB(14.5); words = "Right hold to focus."; w = tw(words, f) + 26; x = 960 - w / 2; y = 1031
    plate_dark(lay, x, y, w, 29); text(lay, (x + 13, y + 14.5), words, f); fr.hud.alpha_composite(lay)
    return tile(fr, (800, 990, 1120, 1080))

def roster_cut(im2x, k=0.8):
    """rows one and two of the roster and the squad bar under it (game px 0-235 and 425-475), at 80 %"""
    top = im2x.crop((0, 0, 640, 470)); bot = im2x.crop((0, 850, 640, 950)); out = Image.new("RGB", (640, 470 + 14 + 100), BG); out.paste(top, (0, 0)); out.paste(bot, (0, 484))
    return out.resize((int(640 * k), int(584 * k)), Image.LANCZOS)
def roster_before():
    return roster_cut(Image.open(R + "briefs/041/build/0311/proof-2160/hold-free-toggle.png").convert("RGB").crop((0, 0, 640, 1000)))
def roster_after():
    fr = scene(); roster_in_fight(fr, (5, 5, 5, 8), (5, 4, 5, 8), moves=("cooling", "ready", "ready", "ready"), fracs=(0.15, 1, 1, 1), firing=(0, 1, 2), under_fire=(1,))
    return roster_cut(tile(fr, (0, 0, 320, 500)))

def build():
    row1 = [dict(im=f_hover(), cap="Hover a hostile", small="The cursor is the crosshair. Its ticks open as the odds fall; the shield badge says his cover counts, counts half, or is struck."),
            dict(im=f_letgo(), cap="Right hold", small="One pill: the crosshair, the time to down with his cover in it, the shooters' faces. Let go commits."),
            dict(im=f_locked(), cap="Let go: the lock", small="The reticle over his head, never hidden by cover (067 D). Ticks on his ring: a shot is in the exchange, ours or theirs."),
            dict(im=f_standdown(), cap="F again: stand down", small="The reticle lets go, the firing stops, the weapon slot hollows. There is no X.")]
    row2 = [dict(im=roster_before(), cap="0.2.122", small="Toggle, tag, stances: retire."), dict(im=roster_after(), cap="F draws", small="Slot, draw button, pips, a move disc."),
            dict(im=waits("aim"), cap="It waits: aiming", small="Held: a dashed ring turns; the disc says why."), dict(im=waits("walk"), cap="It waits: walking in", small="Footprints: out of reach."),
            dict(im=waits("noline"), cap="It waits: no line", small="A cross: the planter is between.")]
    hint = hint_tile()
    row3 = [dict(im=tp_tile("hover"), cap="Third person: hover", small="The same pill over the thing under the crosshair. The build's own capture; the officer is the stand-in."),
            dict(im=tp_tile("lock"), cap="Third person: the lock", small="The same reticle, in screen space: the cypress is between and it still shows."),
            dict(im=hint, cap="The one hint", small="Once, at the first draw in a fight, in the goal line's place (Fable's words).")]
    return grid([row1, row2, row3], OUTB + "board-0-the-one-input.png", "039 \u00b7 the one input \u00b7 the same in A, B and C",
        "F draws, a right hold focuses, F holsters. Drawn from the kit over the 0.2.122 opening square at play zoom (the left roster tile is a real 0.2.122 frame) and the build's third-person capture; the rivals are cut from the 18 Oct capture of the same figures, poses are stand-ins; seconds on pills are illustrative.")

if __name__ == "__main__":
    for nm, fn in (("hover", f_hover), ("letgo", f_letgo), ("locked", f_locked), ("down", f_standdown)):
        im = fn(); im.save(TMP + "b0_%s.png" % nm); print(nm, im.size)
    print(build())
