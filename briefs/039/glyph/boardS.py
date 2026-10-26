# Glyph, brief 039 (2026-10-26): the simplified boards. python3 -I briefs/039/glyph/boardS.py [pick close bpose poses before]
import sys
sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/039/glyph")
from simple39 import *
import count39 as C39

OUT = R + "briefs/039/glyph/"
BOX = (718, 262, 1350, 772)           # the street crop of the pick board, game px (632 x 510)
TS = 1.8                              # px per game px in the tiles (the first pick board used 1.76)
TODAY = {"A": dict(street=37, overlays=33, effects=4, pips=43, lines=5, hud=18), "B": dict(street=39, overlays=35, effects=4, pips=32, lines=5, hud=18), "C": dict(street=34, overlays=32, effects=2, pips=32, lines=5, hud=22)}
FOOT = body_font(24); FOOTB = board_font(30)

def tile_of(fr, box=BOX, scale=TS):
    im = fr.crop(box).convert("RGB"); return im.resize((int(round((box[2] - box[0]) * scale)), int(round((box[3] - box[1]) * scale))), Image.LANCZOS)

def calls(fr, items):
    """board annotations (never in the game): numbered cream discs next to a body"""
    for (x, y), n in items: callout(fr, (x, y), n)

def frames():
    return {o: hero(o) for o in "ABC"}

def now_numbers(info):
    k = info["street_by_kind"]; eff = sum(k.get(x, 0) for x in ("flash", "spark", "puff")); n = info["street"]
    return dict(street=n, effects=eff, overlays=n - eff)

def counts_tile(w, h, nows):
    """today's marks against the new ones, one dot each (a dot is a mark); the board's own numbers, from the code that draws them (count39.py, simple39.py)"""
    im = Image.new("RGB", (w, h), BG); d = ImageDraw.Draw(im); head = board_font(36); big = board_font(72); body = body_font(25); small = body_font(22); bold = board_font(27)
    d.text((6, 0), "Marks on the street", font=head, fill=BCREAM)
    d.text((6, 48), "one dot is one mark", font=small, fill=(176, 205, 199))
    y = 100
    for nm in "ABC":
        t, n = TODAY[nm], nows[nm]
        d.text((6, y + 22), nm, font=big, fill=BCREAM)
        x0 = 82; r = 6.0; gap = 17.5; per = 20
        d.text((x0, y), "today", font=bold, fill=(176, 205, 199)); d.text((x0 + 92, y), "%d" % t["street"], font=bold, fill=BCREAM)
        for i in range(t["street"]):
            cx = x0 + (i % per) * gap + r; cy = y + 44 + (i // per) * gap + r; d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=(247, 244, 236))
        yy = y + 44 + ((t["street"] - 1) // per + 1) * gap + 12
        d.text((x0, yy), "now", font=bold, fill=(96, 224, 234)); d.text((x0 + 92, yy), "%d" % n["street"], font=bold, fill=(96, 224, 234))
        d.text((x0 + 150, yy + 3), "%d drawn over it, %d the world's own" % (n["overlays"], n["effects"]), font=small, fill=(176, 205, 199))
        for i in range(n["street"]):
            cx = x0 + (i % per) * gap + r; cy = yy + 44 + (i // per) * gap + r; d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=(96, 224, 234))
        y = yy + 44 + ((n["street"] - 1) // per + 1) * gap + 40
    d.text((6, y), "Pips drawn: A 43 to 5, B and C 32 to 3 (the focus only).", font=body, fill=(176, 205, 199)); y += 34
    d.text((6, y), "Lines (tracers, streaks, aim lines): 5 to 0.", font=body, fill=(176, 205, 199)); y += 34
    d.text((6, y), "In the roster and tally: 18, 18, 22 to 18, 18, 18.", font=body, fill=(176, 205, 199))
    return im

def board_pick():
    F = frames(); nows = {o: now_numbers(F[o][1]) for o in "ABC"}
    cA, cB, cC = F["A"][1], F["B"][1], F["C"][1]
    calls(F["A"][0], [((R2_[0] + 26, R2_[1] - 32), 1), ((cA["riv"]["3"].chest[0] + 26, cA["riv"]["3"].chest[1] - 2), 2), ((cA["cell"]["F"].chest[0] + 24, cA["cell"]["F"].chest[1] + 4), 3)])
    calls(F["B"][0], [((R2_[0] + 40, R2_[1] - 6), 1), ((cB["riv"]["3"].chest[0] + 24, cB["riv"]["3"].chest[1] + 4), 2), ((R1_[0] + 26, R1_[1] - 22), 3)])
    calls(F["C"][0], [((cC["riv"]["3"].head[0] + 40, cC["riv"]["3"].head[1] - 38), 1), ((cC["riv"]["3"].chest[0] + 26, cC["riv"]["3"].chest[1] + 6), 2), ((cC["riv"]["2"].head[0] + 28, cC["riv"]["2"].head[1] + 6), 3)])
    A, B, Cc = (tile_of(F[o][0]) for o in "ABC")
    ro = F["B"][0].crop((0, 0, 236, 420)).convert("RGB")
    t4 = Image.new("RGB", (A.width, A.height), BG); t4.paste(ro, (0, 0)); ct = counts_tile(A.width - ro.width - 36, A.height, nows); t4.paste(ct, (ro.width + 36, 0))
    r1 = [dict(im=A, cap="A · Tougher", small="1 He kneels behind the hedge and leans out to fire. 2 A hit throws his arm up. 3 She staggers back, hurt. Nothing here is new: a rival lasts 4 s, not 1, so every hit has to read on the body."),
          dict(im=B, cap="B · Cover and pin · Motive's pick", small="1 Shot at behind the hedge, he drops below it: only a lump shows. 2 Shot at in the open, he ducks. 3 In cover and not shot at, he kneels. The stone chips say the cover took the round.")]
    r2 = [dict(im=Cc, cap="C · Showdown beats", small="1 The beat's one plate: ¼× and its line, over the man who has arrived. 2 He steps up and aims. 3 Hit, he flinches. The bodies are A's; the clock does the rest."),
          dict(im=t4, cap="The roster, as built, and the counts", small="Left: the roster column (health, weapon slot, draw button, the one move each). Right: marks drawn on the street, today's board against this one.")]
    path = OUT + "board-pick-A-B-C-simple.png"
    sz = grid([r1, r2], path, "039 · the same moment in A, B and C, simplified (Sky, 2026-10-26)",
        "The body carries the fight: it faces who it shoots and fires, kneels in cover, drops when pinned, staggers when hurt, carries Vale on the shoulder. Over it, only what a body cannot show. Same square, cell and rivals as board-pick-A-B-C.png; the figures are real clips on the real figures at the play camera (a mock-up). Numbers are the board's, not the game's.",
        min_w=0, top=186)
    return path, sz, F, nows

def close_crops(F, nows):
    paths = []
    names = {"A": "A · Tougher", "B": "B · Cover and pin", "C": "C · Showdown beats"}
    for o in "ABC":
        fr = F[o][0]; im = fr.crop(HERO_BOX).convert("RGB"); n = nows[o]
        foot = Image.new("RGB", (im.width, 66), BG); d = ImageDraw.Draw(foot)
        d.text((16, 14), names[o], font=FOOTB, fill=BCREAM)
        d.text((16 + d.textlength(names[o], font=FOOTB) + 28, 18), "%d marks on the street (%d overlays, %d effects) against %d today · 2 px per game px, the figures 106 px tall" % (n["street"], n["overlays"], n["effects"], TODAY[o]["street"]), font=FOOT, fill=(176, 205, 199))
        out = Image.new("RGB", (im.width, im.height + foot.height)); out.paste(im, (0, 0)); out.paste(foot, (0, im.height))
        p = OUT + "close-%s-simple.png" % o; out.save(p); paths.append(p)
    return paths

if __name__ == "__main__":
    which = sys.argv[1:] or ["pick", "close"]
    if "pick" in which or "close" in which:
        path, sz, F, nows = board_pick(); print(path, sz)
    if "close" in which:
        F2 = frames(); print(close_crops(F2, nows))
