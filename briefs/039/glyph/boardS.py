# Glyph, brief 039 (2026-10-26): the simplified boards. python3 -I briefs/039/glyph/boardS.py [pick close before]
# Numbers: today's from count39.py (the old code, instrumented), the new from simple39.py (each mark is registered where it is drawn). Never typed in.
import sys
sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/039/glyph")
from simple39 import *
import count39 as C39

OUT = R + "briefs/039/glyph/"
BOX = (718, 262, 1350, 772)           # the street crop of the pick board, game px (632 x 510)
TS = 1.8                              # px per game px in the tiles (the first pick board used 1.76)
FOOT = body_font(24); FOOTB = board_font(30)
PIPS_TODAY = {"A": 7 + 0, "B": 7, "C": 7}                                                         # pip rows today (three rivals and four cell); the pips themselves:
PIPS_DRAWN_TODAY = {"A": 5 * 3 + 6 + 6 + 6 + 10, "B": 3 * 3 + 5 + 5 + 5 + 8, "C": 3 * 3 + 5 + 5 + 5 + 8}   # 43, 32, 32
PIPS_DRAWN_NOW = {"A": 5, "B": 3, "C": 3}                                                         # the focus's row only

def today_numbers():
    t = C39.today(); out = {}
    for o in "ABC":
        k = t[o]["street_by_kind"]; eff = sum(k.get(x, 0) for x in ("flash", "spark", "puff")); n = t[o]["street_with_cell_rings"]
        out[o] = dict(street=n, effects=eff, overlays=n - eff, hud=t[o]["hud"], lines=k.get("line", 0), arcs=k.get("arc", 0), rings=k.get("ring", 0) + 4, discs=k.get("disc", 0), pip_rows=k.get("pips", 0), pips=PIPS_DRAWN_TODAY[o])
    return out

def now_numbers(info, o):
    k = info["street_by_kind"]; eff = sum(k.get(x, 0) for x in ("flash", "spark", "puff")); n = info["street"]
    return dict(street=n, effects=eff, overlays=n - eff, hud=info["hud"], lines=k.get("line", 0), arcs=k.get("arc", 0), rings=k.get("ring", 0), discs=k.get("disc", 0), pip_rows=k.get("pips", 0), pips=PIPS_DRAWN_NOW[o])

def tile_of(fr, box=BOX, scale=TS):
    im = fr.crop(box).convert("RGB"); return im.resize((int(round((box[2] - box[0]) * scale)), int(round((box[3] - box[1]) * scale))), Image.LANCZOS)

def calls(fr, items):
    """board annotations (never in the game): numbered cream discs next to a body"""
    for (x, y), n in items: callout(fr, (x, y), n)

def frames():
    return {o: hero(o) for o in "ABC"}

def counts_tile(w, h, today, nows):
    """today's marks against the new ones, one dot each; the board's own numbers"""
    im = Image.new("RGB", (w, h), BG); d = ImageDraw.Draw(im); head = board_font(36); big = board_font(72); body = body_font(25); small = body_font(22); bold = board_font(27)
    d.text((6, 0), "Marks on the street", font=head, fill=BCREAM)
    d.text((6, 48), "one dot is one mark", font=small, fill=(176, 205, 199))
    y = 100
    for nm in "ABC":
        t, n = today[nm], nows[nm]
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
    d.text((6, y), "Pips drawn over heads: A %d to %d, B %d to %d, C %d to %d." % (today["A"]["pips"], nows["A"]["pips"], today["B"]["pips"], nows["B"]["pips"], today["C"]["pips"], nows["C"]["pips"]), font=body, fill=(176, 205, 199)); y += 34
    d.text((6, y), "Lines (tracers, streaks, aim lines): %d to %d." % (today["A"]["lines"], nows["A"]["lines"]), font=body, fill=(176, 205, 199)); y += 34
    d.text((6, y), "In the roster and tally: %d, %d, %d to %d, %d, %d." % tuple([today[o]["hud"] for o in "ABC"] + [nows[o]["hud"] for o in "ABC"]), font=body, fill=(176, 205, 199)); y += 34
    d.text((6, y), "Street and roster together: %d, %d, %d to %d, %d, %d." % tuple([today[o]["street"] + today[o]["hud"] for o in "ABC"] + [nows[o]["street"] + nows[o]["hud"] for o in "ABC"]), font=body, fill=BCREAM)
    return im

def board_pick():
    F = frames(); today = today_numbers(); nows = {o: now_numbers(F[o][1], o) for o in "ABC"}
    cA, cB, cC = F["A"][1], F["B"][1], F["C"][1]
    calls(F["A"][0], [((R2_[0] + 26, R2_[1] - 32), 1), ((cA["riv"]["3"].chest[0] + 26, cA["riv"]["3"].chest[1] - 2), 2), ((cA["cell"]["F"].chest[0] + 24, cA["cell"]["F"].chest[1] + 4), 3)])
    calls(F["B"][0], [((R2_[0] + 40, R2_[1] - 6), 1), ((cB["riv"]["3"].chest[0] + 24, cB["riv"]["3"].chest[1] + 4), 2), ((R1_[0] + 26, R1_[1] - 22), 3)])
    calls(F["C"][0], [((cC["riv"]["3"].head[0] + 40, cC["riv"]["3"].head[1] - 38), 1), ((cC["riv"]["3"].chest[0] + 26, cC["riv"]["3"].chest[1] + 6), 2), ((cC["riv"]["2"].head[0] + 28, cC["riv"]["2"].head[1] + 6), 3)])
    A, B, Cc = (tile_of(F[o][0]) for o in "ABC")
    ro = F["B"][0].crop((0, 0, 236, 420)).convert("RGB")
    t4 = Image.new("RGB", (A.width, A.height), BG); t4.paste(ro, (0, 0)); ct = counts_tile(A.width - ro.width - 36, A.height, today, nows); t4.paste(ct, (ro.width + 36, 0))
    r1 = [dict(im=A, cap="A · Tougher", small="1 He kneels behind the hedge and leans out to fire. 2 A hit throws his arm up. 3 She staggers back, hurt. Nothing here is new: a rival lasts 4 s, not 1, so every hit has to read on the body."),
          dict(im=B, cap="B · Cover and pin · Motive's pick", small="1 Shot at behind the hedge, he drops below it: only a lump shows. 2 Shot at in the open, he ducks. 3 In cover and not shot at, he kneels. The stone chips say the cover took the round.")]
    r2 = [dict(im=Cc, cap="C · Showdown beats", small="1 The beat's one plate: ¼× and its line, over the man who has arrived. 2 He steps up and aims. 3 Hit, he flinches. The bodies are A's; the clock does the rest."),
          dict(im=t4, cap="The roster, as built, and the counts", small="Left: the roster column (health, weapon slot, draw button, the one move each). Right: marks drawn on the street, today's board against this one.")]
    path = OUT + "board-pick-A-B-C-simple.png"
    sz = grid([r1, r2], path, "039 · the same moment in A, B and C, simplified (Sky, 2026-10-26)",
        "The body carries the fight: it faces who it shoots and fires, kneels in cover, drops when pinned, staggers when hurt, carries Vale on the shoulder. Over it, only what a body cannot show. Same square, cell and rivals as board-pick-A-B-C.png. A mock-up: the figures are real clips on the real figures at the play camera; the aim, stagger, flinch, duck and carry are in the build, the kneel and the squat are library clips it has not imported (Forma). Numbers are the board's, not the game's.",
        min_w=0, top=216)
    return path, sz, F, today, nows

def close_crops(F, today, nows):
    paths = []
    names = {"A": "A · Tougher", "B": "B · Cover and pin", "C": "C · Showdown beats"}
    for o in "ABC":
        fr = F[o][0]; im = fr.crop(HERO_BOX).convert("RGB"); n = nows[o]
        foot = Image.new("RGB", (im.width, 66), BG); d = ImageDraw.Draw(foot)
        d.text((16, 14), names[o], font=FOOTB, fill=BCREAM)
        d.text((16 + d.textlength(names[o], font=FOOTB) + 28, 18), "%d marks on the street (%d overlays, %d effects) against %d today · 2 px per game px, the figures 106 px tall" % (n["street"], n["overlays"], n["effects"], today[o]["street"]), font=FOOT, fill=(176, 205, 199))
        out = Image.new("RGB", (im.width, im.height + foot.height)); out.paste(im, (0, 0)); out.paste(foot, (0, im.height))
        p = OUT + "close-%s-simple.png" % o; out.save(p); paths.append(p)
    return paths

def board_before_after(F, today, nows):
    """today's three frames (board-pick-A-B-C.png, the old code) beside the simplified ones, the same crop"""
    rows = []
    kw = {"A": ({}, hero_A), "B": (dict(marked=False), hero_B), "C": ({}, hero_C)}
    names = {"A": "A · Tougher", "B": "B · Cover and pin", "C": "C · Showdown beats"}
    for o in "ABC":
        old = scene(); kw[o][1](old, **kw[o][0]); new = F[o][0]
        sc = 1.25; a = tile_of(old, BOX, sc); b = tile_of(new, BOX, sc); t = today[o]; n = nows[o]
        rows.append([dict(im=a, cap="%s · today" % names[o], small="%d marks on the street: %d overlays (4 of them the cell's own rings) and %d effects. %d pips drawn." % (t["street"], t["overlays"], t["effects"], t["pips"]), w=a.width),
                     dict(im=b, cap="%s · now" % names[o], small="%d marks: %d overlays and %d effects. %d pips drawn, on the focus." % (n["street"], n["overlays"], n["effects"], n["pips"]), w=b.width)])
    path = OUT + "board-before-after-simple.png"
    sz = grid(rows, path, "039 · today's board and the simplified one, the same crop", "Left: the three frames Sky saw (board-pick-A-B-C.png), redrawn by the same code. Right: the simplified frames. The old rivals are the 18 Oct stand-ins; the new figures are real clips on the real figures.", W=1700, min_w=0, top=176, gap=20)
    return path, sz

if __name__ == "__main__":
    which = sys.argv[1:] or ["pick", "close", "before"]
    path, sz, F, today, nows = board_pick(); print(path, sz)
    print("today", {o: today[o] for o in "ABC"}); print("now", {o: nows[o] for o in "ABC"})
    if "close" in which: print(close_crops(F, today, nows))
    if "before" in which: print(board_before_after(F, today, nows))
