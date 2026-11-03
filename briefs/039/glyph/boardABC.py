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
    """the blocker behind the west planter (full cover, x0.25), three of the cell on him: Motive's measure is 5.5 s counted, 1.0 s marked. Native 2 px per game px, wide enough that the pill is never cut."""
    fr = scene(); ring39(fr, R1F, threat=True); cover_disc(fr, MARK_PL, True, "rival", struck=marked); cover_arc(fr, R1F, 185, 60, True, CREAM, dim=marked); rival_pips(fr, "r1", 3, 3)
    if marked: brackets(fr, R1F, frac=0.9)
    aim_cursor(fr, (R1F[0], (R1F[1] + R1H[1]) / 2 + 6), odds=0.8 if marked else 0.12, badge="struck" if marked else "counts")
    pill39(fr, (R1F[0], R1H[1] - 20), "Right hold", [("icon", "s_downed"), ("txt", "1 s" if marked else "6 s")], "fire", faces=[SCOUT, FIXER, TECH], harm=True)
    return fr.crop(sq(1292, 440, 400, 293)).convert("RGB")

def board_B():
    fr = hero(hero_B); sc = fr.crop(HERO_BOX).convert("RGB"); ro = fr.crop((0, 0, 236, 470)).convert("RGB")
    row1 = [dict(im=sc, cap="B · Cover and pin", small="B's own changes: full cover quarters a hit and partial halves it (inside its arc, never inside 5 m), and a shot at a man pins him (he fires at half and does not advance). The one mark B adds is the pin disc; the shared cover discs, the shield on the cursor and the flank are what the fight is now about. Mark takes his cover away for 10 s; a flank beats it.", w=1560),
            dict(im=ro, cap="Roster", small="A move disc beside each face: ready, cooling (the Scout's), or shut.", w=464)]
    seq = [dict(im=b_pin(0), cap="Shot at", small="The planter takes it (stone chips, the disc's ring leaves once); the pin disc appears with its 2 s ring full."),
           dict(im=b_pin(1), cap="Pinned", small="The ring drains. He fires at half: one streak, not two. His post does not move."),
           dict(im=b_pin(2), cap="Shot again", small="Hit or miss restarts the ring: a held pin is a few shots, not a wall."),
           dict(im=b_pin(3), cap="Let go", small="The ring runs out, the disc fades, he moves again: his next move is drawn when marked.")]
    seq2 = [dict(im=b_number(False), cap="His cover is in the number", small="The blocker in the planter, three of the cell on him: the pill counts his cover, 6 s (Motive measured 5.5 s). The cursor's badge says why: full cover counts."),
            dict(im=b_number(True), cap="Marked: the number drops", small="The Scout's Mark strikes his shield and the number falls to 1 s (Motive: 1.0 s). That is the reward for a second of the Scout's time, readable before it is paid.")]
    return grid([row1, seq, seq2], OUTB + "board-B-cover-and-pin.png", "039 · B · Cover and pin · Motive's recommendation",
        "Health is the floor (ops 5, Enforcer 8), hostiles 3; cover ×0.25 full and ×0.5 partial inside its arc; shots pin. Drawn from the kit over the 0.2.122 opening square; rivals are cut from the 18 Oct capture, poses are stand-ins; seconds are illustrative.")

# ----------------------------------------------------------------------------- C: showdown beats
def rim(fr, w=2.5, alpha=0.8):
    """the beat's rim: one turquoise-light line round the screen, inset 3 px (the lab's freeze rim, 027 frame A, without its glow: the budget counts the glow)"""
    W, H = fr.hud.size[0] / S, fr.hud.size[1] / S; lay = layer(fr)
    stroke(lay, [(3, 3), (W - 3, 3), (W - 3, H - 3), (3, H - 3)], TURQL, w, alpha=int(255 * alpha), closed=True); fr.hud.alpha_composite(lay)

def erase_marks(fr, box, keep=lambda sub: (sub.min(axis=2) > 110), pad=4):
    """board only: take bright marks out of the capture's status pill. The mask is the marks' own pixels (grown 2.5 px), the fill comes from the dark teal round them."""
    x0, y0, x1, y1 = [int(v * S) for v in box]; p = int(pad * S); arr = np.array(fr.base.convert("RGB")); sub = arr[y0 - p:y1 + p, x0 - p:x1 + p].copy()
    m = np.zeros(sub.shape[:2], np.uint8); m[p:-p, p:-p] = keep(sub)[p:-p, p:-p].astype(np.uint8) * 255; m = cv2.dilate(m, np.ones((5, 5), np.uint8))
    arr[y0 - p:y1 + p, x0 - p:x1 + p] = cv2.inpaint(sub, m, 5, cv2.INPAINT_TELEA); fr.base = Image.fromarray(arr).convert("RGBA")

def slow_glyph(fr):
    """the status pill's speed glyph in a beat: the double chevron with only the first lit (slow)"""
    erase_marks(fr, (1514, 25, 1541, 47), keep=lambda sub: (sub[..., 2].astype(int) > 150) & (sub[..., 1].astype(int) > 150) & (sub[..., 0].astype(int) < 150))
    lay = layer(fr)
    for i, al in enumerate((255, 70)):
        x = 1520 + i * 8.5; pts = [(x, 30), (x + 6.5, 36), (x, 42)]; stroke(lay, pts, TURQL, 3.6, alpha=al, round_caps=True)
    fr.hud.alpha_composite(lay)

def fight_clock(fr, clock="6:12", nxt="6:19", lamps=2):
    """board only: the opening frame's status pill says 0:05, next 3:10, no Alert. In a fight the clock is late, the next mark is a few seconds on, and two lamps are lit
    (as the build draws them: a cream disc in the ring). The old digits are erased from the pill's own dark teal, then redrawn in the kit's Jost Bold."""
    erase_marks(fr, (1443, 26, 1493, 47)); erase_marks(fr, (1565, 29, 1601, 44)); lay = layer(fr)
    text(lay, (1445.6, 36.4), clock, JB(22), CREAM); text(lay, (1568.6, 36.6), nxt, JB(15), CREAM)
    for i in range(lamps): stroke(lay, circ((1810 + 17.5 * i, 36.25), 4.9, 20), CREAM, 0.1, closed=True, fill=CREAM)
    fr.hud.alpha_composite(lay)

def c_full():
    fr = Frame(SQSHOT)
    c = cutouts(); stand_rival(fr, c["C"], R1F); stand_rival(fr, c["C"], R2F, poly=PL_HEDGE, flip=True); stand_rival(fr, c["A"], R3F)
    hero_C(fr); rim(fr); slow_glyph(fr); fight_clock(fr)
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

# ----------------------------------------------------------------------------- the pick board (2 x 2: A, B, C at 1.76 px per game px, and the key)
def key_panel(W=580, H=485):
    """what a frame carries: the parts all three share, then the one mark each option adds (a key for the board, never a game piece)"""
    fr = blank_frame(W, H); lay = layer(fr); body = body_font(24); head = board_font(30); rows = []
    # shared
    y = 64
    reticle39(fr, (44, y - 10), "locked", cams=[SCOUT]); rows.append((y, "The focus: a disc over his head that cover never hides. It follows him, and waits (dashed) when he cannot be shot."))
    y += 62
    cover_disc(fr, (24, y), True, "rival"); cover_disc(fr, (54, y), False, "cell", lit=True); cover_disc(fr, (84, y), True, "rival", struck=True)
    rows.append((y, "Cover: a shield on the prop. The ring says who holds it; a strike says it does not count.", 112))
    y += 52
    for i, st in enumerate(("ready", "cooling", "shut")): move_disc(fr, i, st, 0.4, x=24 + 30 * i, y=y)
    rows.append((y, "One move each beside the portrait: ready, cooling (20 s), shut.", 112))
    y += 48
    crew_tally(fr, 14, y, 3, 1, 0, mark_at=1); rows.append((y, "His crew, with a tick over the pip where it breaks.", 112))
    # one each
    y += 92
    pips(fr, 30, y, 5, 3, hostile=True, pitch=7.0, dia=5.6); pips(fr, 30, y + 14, 10, 10, hostile=False, pitch=5.2, dia=4.4); callout(fr, (84, y + 6), 1)
    rows.append((y + 6, "A \u00b7 longer pip rows: rival 5, cell 6, Enforcer 10.", 112)); y += 62
    pin_disc(fr, (30, y), 0.78, 24); pin_disc(fr, (62, y), 0.3, 24); callout(fr, (92, y), 1)
    rows.append((y, "B \u00b7 pinned: he fires at half and holds. The ring drains in 2 s; each shot restarts it.", 112)); y += 56
    beat_marker(fr, 52, y - 19, "bang", 0.8); callout(fr, (92, y), 1)
    rows.append((y, "C \u00b7 the beat: \u00bc time for 2 s of game time; a ring and aim lines; any order ends it.", 112)); y += 56
    im = fr.compose().convert("RGB"); d = ImageDraw.Draw(im)
    d.text((28, 22), "In every option", font=head, fill=BCREAM); d.text((28, int((rows[3][0] + 58) * 2)), "Only in one", font=head, fill=BCREAM)
    for r in rows:
        yy = r[0]; x0 = (r[2] if len(r) > 2 else 112) * 2 + 16
        for k, ln in enumerate(wrap_to(r[1], body, W * 2 - x0 - 24)): d.text((x0, int(yy * 2) - 14 * (len(wrap_to(r[1], body, W * 2 - x0 - 24)) - 1) + 29 * k), ln, font=body, fill=(176, 205, 199), anchor="lm")
    return im

def board_pick():
    A = hero(hero_A); B = hero(hero_B, marked=False); C = hero(hero_C)
    cx = lambda p, dx, dy: (p[0] + dx, p[1] + dy)
    callout(A, cx((R3H[0], R3H[1] - 11), 44, 0), 1); callout(A, cx((HEAD["enforcer"][0], HEAD["enforcer"][1] - 11), 52, 0), 1)
    callout(B, cx(R3H, 56, -22), 1); callout(B, cx(R2H, 56, -22), 1)
    callout(C, cx(R3H, 46, -82), 1); callout(C, cx(R3F, 40, 10), 2); callout(C, (1004, 626), 3)
    box = (740, 238, 1400, 790); size = (1160, 970)
    ims = [f.crop(box).convert("RGB").resize(size, Image.LANCZOS) for f in (A, B, C)]
    key = key_panel(580, 485)
    r1 = [dict(im=ims[0], cap="A \u00b7 Tougher", small="1 longer pip rows. Nothing else to read: a rival lasts 4 s, not 1, so every hit has to be loud (a spark, a pip popping, a thud)."),
          dict(im=ims[1], cap="B \u00b7 Cover and pin \u00b7 Motive's pick", small="1 the pin disc on a man being shot at. Cover is worth \u00d70.25, so the cover discs, the cursor's shield and the flank are what the fight is about.")]
    r2 = [dict(im=ims[2], cap="C \u00b7 Showdown beats", small="1 the beat's marker  2 its ring  3 who aims at whom. The clock is at \u00bc for 2 s of game time (about 8 s on the wall); any order ends it."),
          dict(im=key, cap="", small="")]
    return grid([r1, r2], OUTB + "board-pick-A-B-C.png", "039 \u00b7 the same moment in A, B and C",
        "The cell with all four drawn (the Enforcer carries Vale) and three rivals in the square: one behind the planter (the blocker), one behind the hedge (the Scout holds the focus on him), one in the open. Drawn from the kit over the 0.2.122 opening square at 1.76 px per game px; rivals are cut from the 18 Oct capture, poses are stand-ins; seconds are illustrative. The numbers point at what each option adds.", min_w=0, top=200)

if __name__ == "__main__":
    import sys as _s
    which = _s.argv[1:] or ["A", "B", "C", "pick"]
    for k in which:
        print(k, {"A": board_A, "B": board_B, "C": board_C, "pick": board_pick}[k]())
