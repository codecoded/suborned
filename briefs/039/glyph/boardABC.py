# Glyph, brief 039, boards A, B, C and the pick board. python3 -I briefs/039/glyph/boardABC.py
import sys
sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/039/glyph")
from board39 import *

OUTB = R + "briefs/039/glyph/"
def fitbox(fr, box, to):
    return fr.crop(box).convert("RGB").resize(to, Image.LANCZOS)
def sq(cx, cy, w=280, h=205): return (cx - w // 2, cy - h // 2, cx + w // 2, cy + h // 2)

def hero(fn, **kw):
    fr = scene(); fn(fr, **kw); return fr

# ----------------------------------------------------------------------------- A: a rival lasts
def a_step(n_left, flash=0, shots=3, hit=True, label=None):
    fr = scene()
    if n_left > 0:
        ring39(fr, R3F, threat=True, ours=True)
        for k in ("scout", "fixer", "tech")[:shots]: tracer(fr, gun(k), hit_pt("r3"))
        if hit: spark(fr, hit_pt("r3"), 0.25)
    rival_pips(fr, "r3", 5, n_left, flash=flash)
    if n_left == 0:
        lay = layer(fr); disc39(lay, R3F[0], R3H[1] - 30, 18, "s_downed"); fr.hud.alpha_composite(lay)
    return fr.crop(sq(1105, 650)).convert("RGB").resize((560, 410), Image.LANCZOS)

def board_A():
    fr = hero(hero_A); sc = fr.crop(HERO_BOX).convert("RGB"); ro = fr.crop((0, 0, 236, 470)).convert("RGB")
    seq = [dict(im=a_step(5), cap="First volley", small="Three sidearms at a man in the open: five pips, none lost yet."),
           dict(im=a_step(3, 2), cap="Second", small="Two hit: two pips pop (a ring each, 0.25 s)."),
           dict(im=a_step(1, 2), cap="Third", small="He is still up, still shooting: the player has time to answer."),
           dict(im=a_step(0, 1, hit=False), cap="Down in about 4 s", small="Today he is down in 1 s. Nothing here is new on the street.")]
    row1 = [dict(im=sc, cap="A · Tougher", small="A's own change is health: rivals and officers five pips, the cell six, the Enforcer ten, so the first volley no longer decides it. It adds nothing to read. The marks here (the focus reticle, the cover discs, a move disc each, the crew tally) are the parts every option shares: boards 0, 1 and 2.", w=1560),
            dict(im=ro, cap="Roster", small="Pip rows to ten; the stance tag and the weapons-free toggle are gone.", w=464)]
    return grid([row1, seq], OUTB + "board-A-tougher.png", "039 · A · Tougher",
        "Health up (ops 6, Enforcer 10, rivals and officers 5), cover as today. Drawn from the kit over the 0.2.122 opening square; rivals are cut from the 18 Oct capture, poses are stand-ins; seconds are illustrative.")

# ----------------------------------------------------------------------------- B: cover and pin
def b_pin(stage):
    fr = scene(); ring39(fr, R1F, ours=True); cover_disc(fr, MARK_PL, True, "rival", took=stage in (0, 2)); cover_arc(fr, R1F, 185, 60, True, CREAM); rival_pips(fr, "r1", 3, 3)
    a = gun("fixer")
    if stage in (0, 2): tracer(fr, a, (1290, 540)); chip_puff(fr, (1288, 538))
    if stage == 0: pin_disc(fr, (R1H[0] + 31, R1H[1] - 11), 1.0, 24)
    if stage == 1: pin_disc(fr, (R1H[0] + 31, R1H[1] - 11), 0.55); streak(fr, (R1F[0] - 12, R1F[1] - 36), (HEAD["tech"][0] + 10, HEAD["tech"][1] + 26), ts=(0.35,), L=16)
    if stage == 2: pin_disc(fr, (R1H[0] + 31, R1H[1] - 11), 1.0)
    if stage == 3: walk_to(fr, R1F, (1230, 600))
    return fr.crop(sq(1290, 530, 300, 220)).convert("RGB").resize((560, 410), Image.LANCZOS)

def b_number(marked):
    fr = scene(); ring39(fr, R2F, threat=True); cover_disc(fr, MARK_HG, False, "rival", struck=marked); cover_arc(fr, R2F, 200, 70, False, CREAM, dim=marked); rival_pips(fr, "r2", 3, 3)
    if marked: brackets(fr, R2F, frac=0.9)
    aim_cursor(fr, (R2F[0], (R2F[1] + R2H[1]) / 2 + 6), odds=0.8 if marked else 0.3, badge="struck" if marked else "half")
    pill39(fr, (R2F[0], R2H[1] - 20), "Right hold", [("icon", "s_downed"), ("txt", "3 s" if marked else "9 s")], "fire", faces=[SCOUT, FIXER, TECH], harm=True)
    return fr.crop(sq(1180, 338, 320, 234)).convert("RGB").resize((560, 410), Image.LANCZOS)

def board_B():
    fr = hero(hero_B); sc = fr.crop(HERO_BOX).convert("RGB"); ro = fr.crop((0, 0, 236, 470)).convert("RGB")
    row1 = [dict(im=sc, cap="B · Cover and pin", small="B's own changes: full cover quarters a hit and partial halves it (inside its arc, never inside 5 m), and a shot at a man pins him (he fires at half and does not advance). The one mark B adds is the pin disc; the shared cover discs, the shield on the cursor and the flank are what the fight is now about. Mark takes his cover away for 10 s; a flank beats it.", w=1560),
            dict(im=ro, cap="Roster", small="A move disc beside each face: ready, cooling (the Scout's), or shut.", w=464)]
    seq = [dict(im=b_pin(0), cap="Shot at", small="The planter takes it (stone chips, the disc's ring leaves once); the pin disc appears with its 2 s ring full."),
           dict(im=b_pin(1), cap="Pinned", small="The ring drains. He fires at half: one streak, not two. His post does not move."),
           dict(im=b_pin(2), cap="Shot again", small="Hit or miss restarts the ring: a held pin is a few shots, not a wall."),
           dict(im=b_pin(3), cap="Let go", small="The ring runs out, the disc fades, he moves again: his next move is drawn when marked.")]
    seq2 = [dict(im=b_number(False), cap="His cover is in the number", small="Right hold shows the time to down with his cover counted (9 s here, an illustrative number). The cursor's badge says why (counts half)."),
            dict(im=b_number(True), cap="Marked: the number drops", small="Scout's Mark strikes his shield and the number falls to 3 s. That is the reward for flanking, readable before it is paid."),
            dict(im=b_pin(0).resize((1, 1)) if False else Image.new("RGB", (1, 1), BG), cap="", small="")]
    seq2 = seq2[:2]
    return grid([row1, seq, seq2], OUTB + "board-B-cover-and-pin.png", "039 · B · Cover and pin · Motive's recommendation",
        "Health is the floor (ops 5, Enforcer 8), hostiles 3; cover ×0.25 full and ×0.5 partial inside its arc; shots pin. Drawn from the kit over the 0.2.122 opening square; rivals are cut from the 18 Oct capture, poses are stand-ins; seconds are illustrative.")

# ----------------------------------------------------------------------------- C: showdown beats
def rim(fr, w=2.5, alpha=0.8):
    """the beat's rim: one turquoise-light line round the screen, inset 3 px (the lab's freeze rim, 027 frame A, without its glow: the budget counts the glow)"""
    W, H = fr.hud.size[0] / S, fr.hud.size[1] / S; lay = layer(fr)
    stroke(lay, [(3, 3), (W - 3, 3), (W - 3, H - 3), (3, H - 3)], TURQL, w, alpha=int(255 * alpha), closed=True); fr.hud.alpha_composite(lay)

def slow_glyph(fr):
    """the status pill's speed glyph in a beat: the double chevron with only the first lit (slow)"""
    x0, y0, x1, y1 = [int(v * S) for v in (1514, 25, 1541, 47)]
    arr = np.array(fr.base.convert("RGB")); sub = arr[y0:y1, x0:x1].copy(); m = np.ones(sub.shape[:2], np.uint8) * 255; arr[y0:y1, x0:x1] = cv2.inpaint(sub, m, 4, cv2.INPAINT_TELEA); fr.base = Image.fromarray(arr).convert("RGBA")
    lay = layer(fr)
    for i, al in enumerate((255, 70)):
        x = 1520 + i * 8.5; pts = [(x, 30), (x + 6.5, 36), (x, 42)]; stroke(lay, pts, TURQL, 3.6, alpha=al, round_caps=True)
    fr.hud.alpha_composite(lay)

def c_full():
    fr = Frame(SQSHOT)
    c = cutouts(); stand_rival(fr, c["C"], R1F); stand_rival(fr, c["C"], R2F, poly=PL_HEDGE, flip=True); stand_rival(fr, c["A"], R3F)
    hero_C(fr); rim(fr); slow_glyph(fr)
    return fr

def c_cause(cause):
    fr = scene(); ring39(fr, R3F, threat=True)
    lay = layer(fr)
    if cause == "bang": beat_marker(fr, R3H[0], R3H[1] - 56, "bang", 0.95); rival_pips(fr, "r3", 3, 3); beat_ring(fr, R3F, 0.95, 0.95)
    elif cause == "rival": beat_marker(fr, R3H[0], R3H[1] - 56, None, 0.95, face=RIVAL); rival_pips(fr, "r3", 3, 3); beat_ring(fr, R3F, 0.95, 0.95)
    elif cause == "down":
        fr = scene(); y = HEAD["fixer"]; beat_marker(fr, y[0], y[1] - 66, "s_downed", 0.95, face=None); beat_ring(fr, CELL["fixer"], 0.7, 0.95)
        lay = layer(fr); disc39(lay, y[0], y[1] - 20, 22, "s_downed"); fr.hud.alpha_composite(lay)
        return fr.crop(sq(840, 650, 300, 220)).convert("RGB").resize((560, 410), Image.LANCZOS)
    elif cause == "car":
        fr = Frame(R + "briefs/039/glyph/base/getaway-car-yaw210.png", blot=[(458, 218, 552, 252)])
        beat_marker(fr, 960, 440, "car", 0.95)
        return fr.crop((780, 340, 1100, 575)).convert("RGB").resize((560, 411), Image.LANCZOS)
    return fr.crop(sq(1110, 620, 300, 220)).convert("RGB").resize((560, 410), Image.LANCZOS)

def c_life(stage):
    fr = scene(); ring39(fr, R2F, threat=True); ring39(fr, R3F, threat=True); rival_pips(fr, "r2", 3, 3); rival_pips(fr, "r3", 3, 3)
    for a, b in (((R2F[0] - 10, R2F[1] - 36), (HEAD["fixer"][0] + 8, HEAD["fixer"][1] + 24)), ((R3F[0] - 12, R3F[1] - 36), (HEAD["tech"][0] + 8, HEAD["tech"][1] + 24))):
        if stage < 2: threat(fr, a, b)
    if stage == 0: beat_ring(fr, R3F, 0.95, 0.95); beat_marker(fr, R3H[0], R3H[1] - 56, None, 1.0, face=RIVAL)
    if stage == 1: beat_ring(fr, R3F, 0.95, 0.5); beat_marker(fr, R3H[0], R3H[1] - 56, None, 0.5, face=RIVAL)
    if stage == 2:
        a_, b_ = CELL["enforcer"], (996, 770); stroke(fr.world, [a_, b_], INKDEEP, 4.4, dash=(2.0, 4.6), alpha=150); stroke(fr.world, [a_, b_], TURQL, 2.4, dash=(2.0, 4.6)); move_ring(fr, b_, 0.18)
        lay = layer(fr); disc(lay, b_[0], b_[1] - 22, 20, "s_moving"); fr.hud.alpha_composite(lay)
    return fr.crop(sq(1000, 640, 440, 322)).convert("RGB").resize((560, 410), Image.LANCZOS)

def board_C():
    full = c_full().compose(); fw = full.convert("RGB").resize((1560, 878), Image.LANCZOS)
    row1 = [dict(im=fw, cap="C · Showdown beats", small="At four moments (the fight opens, a hostile arrives, one of the cell goes down, the car comes free) the clock drops to a quarter for 2 s of game time, about 8 s on the wall. The subject wears a window marker, a ring leaves it, who aims at whom is drawn, every move refills; any order ends it. The street is otherwise as A.", w=1560)]
    causes = [dict(im=c_cause(c), cap=t, small=s) for c, t, s in (("bang", "The fight opens", "The first shot."), ("rival", "A hostile arrives", "His face."), ("down", "One of the cell goes down", "Hers."), ("car", "The car comes free", "The car."))]
    life = [dict(im=c_life(0), cap="Opens", small="The ring leaves the subject; the marker's line is full; the aim lines show who is in whose sights."),
            dict(im=c_life(1), cap="Mid beat", small="The line drains over 8 s. The player thinks: the move discs are ready."),
            dict(im=c_life(2), cap="Any order ends it", small="The marker and the lines ease out in 0.2 s; time returns to full.")]
    return grid([row1, causes, life], OUTB + "board-C-showdown-beats.png", "039 · C · Showdown beats",
        "A and B's street plus one time rule. The marker is the window marker of the kit (plate_dark, icon, underline) with a quarter-time mark. Drawn over the 0.2.122 opening square; rivals are stand-ins.")

# ----------------------------------------------------------------------------- the pick board
def board_pick():
    A = hero(hero_A); B = hero(hero_B); C = hero(hero_C)
    cx = lambda p, dx, dy: (p[0] + dx, p[1] + dy)
    callout(A, cx((R3H[0], R3H[1] - 11), 44, 0), 1); callout(A, cx((HEAD["enforcer"][0], HEAD["enforcer"][1] - 11), 52, 0), 1)
    callout(B, cx(R3H, 52, -26), 1); callout(B, cx(R2H, 52, -26), 1)
    callout(C, cx(R3H, 46, -82), 1); callout(C, cx(R3F, 40, 10), 2); callout(C, (1020, 600), 3)
    ims = [f.crop((700, 230, 1440, 830)).convert("RGB").resize((740, 600), Image.LANCZOS) for f in (A, B, C)]
    row = [dict(im=ims[0], cap="A \u00b7 Tougher", small="1 longer pip rows (rival 5, cell 6, Enforcer 10). Nothing else to read. A rival lasts 4 s, not 1."),
           dict(im=ims[1], cap="B \u00b7 Cover and pin \u00b7 recommended", small="1 the pin disc on a man being shot at. Cover is worth \u00d70.25, so the cover discs, the cursor's shield and the flank are what the fight is about."),
           dict(im=ims[2], cap="C \u00b7 Showdown beats", small="1 the beat's marker  2 its ring  3 who aims at whom. Time is at \u00bc for 2 s of game time (8 s on the wall); any order ends it.")]
    return grid([row], OUTB + "board-pick-A-B-C.png", "039 \u00b7 the same moment in A, B and C",
        "The cell, four drawn, three rivals in the square: one behind the planter (the blocker), one behind the hedge (the Scout holds the focus on him), one in the open. Every frame carries the parts all three share (the focus reticle, cover discs, a move disc each, the crew tally, the pips); the numbers point at what each option adds.", min_w=0)

if __name__ == "__main__":
    import sys as _s
    which = _s.argv[1:] or ["A", "B", "C", "pick"]
    for k in which:
        print(k, {"A": board_A, "B": board_B, "C": board_C, "pick": board_pick}[k]())
