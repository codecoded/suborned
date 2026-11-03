# Glyph, brief 039 (2026-10-26): B on poses alone, and the pose sheet for Forma. python3 -I briefs/039/glyph/boardP.py [bpose poses]
import sys
sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/039/glyph")
from boardS import *

SPOT = (1000, 570)                  # an empty stretch of paving to stand a pose on

def pose_tile(fid, clip, frame, lib, heading=220.0, extra=None, feet=SPOT):
    """one pose on the clean paving at the play camera: (1x tile 90 x 102 px, 3x tile, the Fig with its bbox and dims)"""
    plate = clean_plate().copy(); fg = pl.Fig(fid, clip, frame, feet, heading=heading, lib=lib, extra=extra)
    pl.lay(plate, fg)
    t1 = Image.fromarray(plate[feet[1] - 80:feet[1] + 22, feet[0] - 45:feet[0] + 45]); t3 = t1.resize((t1.width * 3, t1.height * 3), Image.LANCZOS)
    return t1, t3, fg

STATES = [  # label, fig, clip, frame, lib
    ("stands, aims", "rival", "aim_pistol", 40, False),
    ("kneels in cover", "rival", "Pistol_Kneeling_Idle", 40, True),
    ("pinned, in cover: squat", "rival", "Crouching_Idle", 20, True),
    ("pinned, open: duck", "rival", "cower", 47, False),
    ("hurt: stagger", "rival", "hit_large", 10, False),
    ("hit: flinch", "rival", "hit", 8, False)]
COVER_M = 0.95        # the hedge and the planters: a metre of stone and shrub (the cover the shown frames use)
PXM = 38.571 * 0.788  # screen px per metre standing up, at 38 degrees

def state_strip():
    tiles = []
    for lab, fid, clip, fr, lib in STATES:
        t1, t3, fg = pose_tile(fid, clip, fr, lib); tiles.append((lab, t1, t3, fg))
    return tiles

def measures_table(st, w, h):
    """what each pose changes at true size, measured on the renders: height above the ground, the screen height and the pixels it covers, and what shows above a one-metre cover"""
    im = Image.new("RGB", (w, h), BG); d = ImageDraw.Draw(im); head = board_font(30); f = body_font(23); fb = board_font(23)
    d.text((4, 0), "What each state changes at true size", font=head, fill=BCREAM)
    d.text((4, 42), "measured on the renders (1 px = 1 px of a 1080p frame)", font=body_font(21), fill=(176, 205, 199))
    cols = [(4, "state"), (292, "high"), (366, "tall"), (440, "covers"), (540, "over a 1 m cover")]
    y = 84
    for x, t in cols: d.text((x, y), t, font=fb, fill=(176, 205, 199))
    d.text((366, y + 24), "px", font=body_font(19), fill=(176, 205, 199)); d.text((440, y + 24), "px", font=body_font(19), fill=(176, 205, 199)); d.text((292, y + 24), "m", font=body_font(19), fill=(176, 205, 199))
    y += 58
    rows = []
    for lab, t1, t3, fg in st:
        z = fg.dims["zmax"] if fg.dims else 0.0; hp = fg.bbox[3] - fg.bbox[1]; over = max(0.0, z - COVER_M) * PXM
        rows.append((lab, z, hp, fg.area, over))
        d.text((4, y), lab, font=f, fill=BCREAM); d.text((292, y), "%.2f" % z, font=f, fill=BCREAM); d.text((366, y), "%d" % hp, font=f, fill=BCREAM); d.text((440, y), "%d" % fg.area, font=f, fill=BCREAM); d.text((540, y), "%d px" % round(over), font=f, fill=BCREAM)
        y += 36
    return im, rows

def board_bpose():
    fb, cB = hero("B")
    calls(fb, [((R2_[0] + 40, R2_[1] - 6), 1), ((cB["riv"]["3"].chest[0] + 24, cB["riv"]["3"].chest[1] + 4), 2), ((R1_[0] + 26, R1_[1] - 22), 3)])
    im1 = fb.compose().convert("RGB").resize((1920, 1080), Image.LANCZOS).crop(BOX)
    fB2, iB2 = hero("B")
    def crop3(box, k=3.0): c = fB2.crop(box).convert("RGB"); return c.resize((int(c.width * k / 2), int(c.height * k / 2)), Image.LANCZOS)
    c_r2 = crop3((1128, 276, 1238, 416)); c_r3 = crop3((1062, 600, 1172, 740)); c_r1 = crop3((1262, 480, 1352, 580))
    gap = 12; zw = c_r2.width + c_r3.width + c_r1.width + 2 * gap; zh = max(c_r2.height, c_r3.height, c_r1.height)
    z = Image.new("RGB", (zw, zh), BG); x = 0
    for c in (c_r2, c_r3, c_r1): z.paste(c, (x, 0)); x += c.width + gap
    st = state_strip(); sw = 270 + 12
    strip3 = Image.new("RGB", (len(st) * sw, 306), BG); strip1 = Image.new("RGB", (len(st) * sw, 102), BG)
    for i, (lab, t1, t3, fg) in enumerate(st): strip3.paste(t3, (i * sw, 0)); strip1.paste(t1, (i * sw + (270 - 90) // 2, 0))
    labels = Image.new("RGB", (len(st) * sw, 34), BG); dl = ImageDraw.Draw(labels)
    for i, (lab, t1, t3, fg) in enumerate(st): dl.text((i * sw + 4, 2), lab, font=body_font(23), fill=BCREAM)
    strip = Image.new("RGB", (strip3.width, 306 + 34 + 102 + 10), BG); strip.paste(strip3, (0, 0)); strip.paste(labels, (0, 308)); strip.paste(strip1, (0, 308 + 34 + 8))
    tw_ = 2400 - 56 - 632 - 20 - zw - 20
    tab, rows = measures_table(st, tw_, 440)
    R_ = {lab: (zz, hp, ar, ov) for lab, zz, hp, ar, ov in rows}
    stand, kneel, squat, duck = R_["stands, aims"], R_["kneels in cover"], R_["pinned, in cover: squat"], R_["pinned, open: duck"]
    film1 = Image.new("RGB", (6 * 100, 102), BG); film3 = Image.new("RGB", (6 * 282, 306), BG)
    for i, fr_ in enumerate((1, 9, 18, 27, 38, 47)):
        t1, t3, fg = pose_tile("rival", "cower", fr_, False); film1.paste(t1, (i * 100 + 5, 0)); film3.paste(t3, (i * 282, 0))
    film = Image.new("RGB", (film3.width, 306 + 8 + 102), BG); film.paste(film3, (0, 0)); film.paste(film1, (0, 314))
    fP, iP = hero("B", extra=extra_pin_discs); fM, iM = hero("B", extra=extra_moving, skip=("chips",))
    tP = tile_of(fP); tM = tile_of(fM)
    r1 = [dict(im=Image.fromarray(np.array(im1)), cap="B at true size", small="1 px is 1 px of a 1080p frame: the figures are 53 px tall. 1 He sinks below the hedge, only a lump shows. 2 He ducks in the open. 3 He kneels, not shot at.", w=632),
          dict(im=z, cap="The same three, three times larger", small="Left to right: pinned behind the hedge (squat), pinned in the open (duck), kneeling in cover and not shot at. The lock disc, ring and pips over the first are the focus, not the pin.", w=zw),
          dict(im=tab, cap="", small="", w=tw_)]
    r2 = [dict(im=strip, cap="The same man in the states B asks a body to carry (above: three times; below: true size)", small="Library clips on one figure at the play camera, facing the camera. Standing to squat is %d px at true size; kneeling to squat %d; kneeling to duck %d. Behind a metre of cover the kneeler shows %d px, the ducker %d, the squatter %d." % (stand[1] - squat[1], kneel[1] - squat[1], abs(kneel[1] - duck[1]), round(kneel[3]), round(duck[3]), round(squat[3])), w=strip.width)]
    r2b = [dict(im=film, cap="The duck as it plays (the build's cower, every 0.3 s, then held)", small="Six frames of one second and a half: the drop is what the eye catches, 17 px lower and hunched. Held for the pin's 2 s, it is the pin's own timer: when he stands, it is over. A still cannot show this; the fight range can.", w=film.width)]
    r3 = [dict(im=tP, cap="If the body is not enough: B plus the pin disc", small="The kit's chevron disc beside each pinned man, no draining ring: two marks, ten in all. Only if the squat and the duck do not read in play.", w=tP.width),
          dict(im=tM, cap="B while a move is being ordered near cover", small="Cover marks exist only now: the cursor's ring, and on his hedge a disc with a strike (from there the hedge no longer shields him). Two marks that go when the move lands: ten with B's own eight.", w=tM.width)]
    path = OUT + "board-B-on-poses-simple.png"
    sz = grid([r1, r2, r2b, r3], path, "039 \u00b7 B on poses alone: does it read at 40\u201360 px?",
        "B's frame from the pick board with no pin disc, no cover disc and no arc: the body is all there is. The same frame at the size the player meets it, the man in each state B needs, and what B would add if it does not read.", min_w=0, top=150)
    return path, sz, rows

# ----------------------------------------------------------------------------- the pose sheet for Forma
CARDS = [  # title, figure, clip, frame, library?, heading, status, note
    ("Face and aim", "scout", "aim_pistol", 40, False, 100.0, "in the build", "aim_pistol (Pistol_Aim) for the sidearm, aim for the carbine. The turn to him in 0.2 s is the host's: the body must be square to the target, because the facing is the line."),
    ("Fire: a kick per shot", "enforcer", "shoot", 6, False, 100.0, "build, and to make", "shoot (Firing_Rifle, 0.6 s) is the carbine's kick. The sidearm has none: an additive 0.12 s kick on aim_pistol (to make). The library's Shooting_Pistol lunges too far from the aim; not advised."),
    ("In cover: kneel", "rival", "Pistol_Kneeling_Idle", 40, True, 250.0, "in the library", "Pistol_Kneeling_Idle; in: Pistol_Stand_To_Kneel (1.0 s); out: Pistol_Kneel_To_Stand. Carbine: Rifle_Kneel_Idle. The build's crouch is as low (1.31 m) but has no gun up. The lean out to fire, and back, is to make."),
    ("Pinned in cover: squat", "rival", "Crouching_Idle", 20, True, 250.0, "library, and to make", "Crouching_Idle: the head drops below a metre of cover. Held for the pin's 2 s, restarted by each shot. One arm up over the cover, firing blind, is to make."),
    ("Pinned in the open: duck", "rival", "cower", 47, False, 250.0, "in the build", "cower (Ducking, 1.5 s): hold the last frame for the pin's 2 s. It shortens him by 0.6 m and hunches him; the one pose that already reads in the open."),
    ("Hurt: stagger", "rival", "hit_large", 10, False, 250.0, "in the build", "hit_large (React_Large_From_Front): an arm thrown up, the head back. The library has From_Left, From_Right and From_Back: pick by the side the round came from."),
    ("Hit: flinch", "rival", "hit", 8, False, 250.0, "in the build", "hit (React_Small, 1.3 s): a bend and a twist, a few px at true size: it needs its spark, thud and grunt. A walker (the Enforcer with Vale): Walking_Hit_Reaction (library)."),
    ("Badly hurt: hunched", "rival", "Injured_Hurting_Idle", 70, True, 250.0, "in the library", "Injured_Hurting_Idle: bent at the waist, held while health is under a third (the portrait's badly-hurt state), so the cell's weak man shows on the street, not only in the roster."),
    ("Down", "rival", "kill_victim_back", 60, False, 250.0, "in the build", "kill_victim_back falls backward (3.7 s) and lying holds him. The library has the fall to the shot's side (Standing_React_Death_Left, _Right, _Forward) and Crouch_Death for a kneeling man in cover."),
    ("Carry: Vale on the shoulder", "enforcer", "carry_shoulder", 12, False, 200.0, "in the build", "carry_shoulder with carried_drape on Vale, seated on the right shoulder (the build has both). The carry disc is gone: the body is the mark."),
]

def card(spec, w=446):
    title, fid, clip, frame, lib, heading, status, note = spec
    extra = [dict(id="councillor", clip="carried_drape", frame=20, heading=heading + 180.0)] if title.startswith("Carry") else None
    t1, t3, fg = pose_tile(fid, clip, frame, lib, heading=heading, extra=extra)
    zmax = fg.dims["zmax"] if fg.dims else 0.0; hp = fg.bbox[3] - fg.bbox[1]
    im = Image.new("RGB", (w, 306 + 190), BG); d = ImageDraw.Draw(im); tf = board_font(29); bf = body_font(21); sf = body_font(20)
    im.paste(t3, (0, 0)); im.paste(t1, (282, 0))
    d.text((282, 114), "true size", font=sf, fill=(176, 205, 199)); d.text((282, 140), "%.2f m high" % zmax, font=bf, fill=BCREAM); d.text((282, 166), "%d px tall" % hp, font=bf, fill=BCREAM)
    d.text((282, 206), status, font=sf, fill=(96, 224, 234)); d.text((282, 232), "frame %d" % frame, font=sf, fill=(176, 205, 199))
    y = 314; d.text((0, y), title, font=tf, fill=BCREAM); y += 38
    for ln in wrap_to(note, bf, w - 4)[:5]: d.text((0, y), ln, font=bf, fill=(176, 205, 199)); y += 26
    return im

def board_poses():
    cards = [card(c) for c in CARDS]
    rows = [[dict(im=c, cap="", small="", w=c.width) for c in cards[:5]], [dict(im=c, cap="", small="", w=c.width) for c in cards[5:]]]
    path = OUT + "board-poses-simple.png"
    sz = grid(rows, path, "039 \u00b7 the poses the simple fight needs (Forma, Latch)",
        "Real clips on the real figures at the play camera, three times larger, with true size beside. In the build means the clip is in unity/Assets/Suborned/People/Clips; in the library means Mixamo's, in the-suborned-store/source-art/Anim/mixamo_library, not imported; to make is a pose or a blend that neither has. Whether the host plays a clip yet is Latch's.", min_w=0, top=160, W=2400, gap=22, row_gap=28)
    return path, sz

if __name__ == "__main__":
    which = sys.argv[1:] or ["bpose"]
    if "poses" in which: print(board_poses())
    if "bpose" in which:
        p_, sz_, rows_ = board_bpose(); print(p_, sz_)
        for r in rows_: print(r)
