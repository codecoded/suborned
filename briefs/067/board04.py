# Glyph, brief 067, board 04 (H): the conversation scene from the ground view. python3 -I briefs/067/board04.py -> briefs/067/board-04-conversation-from-the-ground.png
import sys, math; sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/067")
from g67 import *
OUTD = R + "briefs/067/"
AS_BUILT_1 = R + "briefs/033/latch-scenes-v2/09-scene-2-help.png"        # the build: the person alone (a pair more than 3 m apart)
AS_BUILT_2 = R + "briefs/033/latch-scenes-v2/09-scene-1-listen.png"      # the build: over the cell member's shoulder, his back in the foreground

PPM = 96           # px per metre in the plan
def ctr(m, o): return (o[0] + m[0] * PPM, o[1] - m[1] * PPM)

def plan():
    W, H = 1160, 760; im = Image.new("RGBA", (W, H), BG + (255,)); d = ImageDraw.Draw(im); o = (W / 2 - 40, 330)
    f = board_font(24); fs = body_font(21)
    # the pair: agent (left) and subject (right), 1.5 m apart, facing each other
    A = ctr((-0.75, 0), o); B = ctr((0.75, 0), o)
    # the sweep: the camera's candidate positions, +-45 deg round the perpendicular, 15 deg steps, 3.6 m out (south = down)
    r = 3.6 * PPM
    for k in (-25, -12, 0, 12, 25):
        a = math.radians(90 + k); p = (o[0] + r * math.cos(a), o[1] + r * math.sin(a))
        col = CREAM if k == 0 else (176, 205, 199); rad = 11 if k == 0 else 6
        d.ellipse((p[0] - rad, p[1] - rad, p[0] + rad, p[1] + rad), fill=col + (255,) if k == 0 else None, outline=col + (255,), width=3)
    d.arc((o[0] - r, o[1] - r, o[0] + r, o[1] + r), 60, 120, fill=(176, 205, 199, 255), width=2)
    # the field of view of the first candidate (35 deg vertical, 16:9: about 58 deg wide)
    c = (o[0], o[1] + r); half = math.radians(29.15); L = r + 0.9 * PPM
    ov = Image.new("RGBA", im.size, (0, 0, 0, 0)); od = ImageDraw.Draw(ov)
    od.polygon([c, (c[0] - L * math.sin(half), c[1] - L * math.cos(half)), (c[0] + L * math.sin(half), c[1] - L * math.cos(half))], fill=(96, 224, 234, 46), outline=(96, 224, 234, 190))
    im.alpha_composite(ov); d = ImageDraw.Draw(im)
    # the people (circles, a nose toward each other)
    for P, dirx, rel in ((A, 1, SCOUT), (B, -1, FIXER)):
        d.ellipse((P[0] - 17, P[1] - 17, P[0] + 17, P[1] + 17), fill=(247, 244, 236, 255), outline=(11, 22, 21, 255), width=3)
        d.polygon([(P[0] + dirx * 17, P[1] - 8), (P[0] + dirx * 31, P[1]), (P[0] + dirx * 17, P[1] + 8)], fill=(247, 244, 236, 255))
    d.text((A[0], A[1] - 38), "agent", font=fs, fill=CREAM + (255,), anchor="mm"); d.text((B[0], B[1] - 38), "subject", font=fs, fill=CREAM + (255,), anchor="mm")
    # the chase camera: behind the agent, on its heading
    d.polygon([(40, o[1] - 14), (74, o[1] - 24), (74, o[1] + 24), (40, o[1] + 14)], fill=(176, 205, 199, 255)); d.text((57, o[1] + 56), "the chase\ncamera", font=fs, fill=(176, 205, 199, 255), anchor="mm", align="center")
    d.line([(80, o[1]), (A[0] - 34, A[1])], fill=(176, 205, 199, 255), width=2)
    # labels
    d.text((c[0] + 40, c[1] + 4), "1 · the profile two-shot: 3.6 m, 1.9 m high, 8 deg down, 35 deg", font=fs, fill=CREAM + (255,), anchor="lm")
    d.text((c[0] + 40, c[1] + 34), "candidates: the perpendicular, then +-12 and +-25 deg (a face turns", font=fs, fill=(176, 205, 199, 255), anchor="lm")
    d.text((c[0] + 40, c[1] + 62), "from the lens by 115 deg at most); then again at 0.8 of the distance", font=fs, fill=(176, 205, 199, 255), anchor="lm")
    d.text((o[0], 40), "plan", font=f, fill=CREAM + (255,), anchor="mm")
    return im

def elev():
    W, H = 740, 760; im = Image.new("RGBA", (W, H), BG + (255,)); d = ImageDraw.Draw(im); f = body_font(21); P = 74
    gy = 600; px = 600                                              # the ground line, the pair's x (the section is along the camera's axis: both people overlap)
    d.line([(20, gy), (W - 20, gy)], fill=(176, 205, 199, 255), width=3)
    d.rounded_rectangle((px - 15, gy - 1.8 * P, px + 15, gy), radius=11, fill=CREAM + (255,))
    look = (px, gy - 1.35 * P)                                      # the point both cameras look at: the pair's chest
    ov = Image.new("RGBA", im.size, (0, 0, 0, 0)); od = ImageDraw.Draw(ov)
    # 1 · the profile two-shot: 3.6 m out, 1.9 m high, 8 deg down, 35 deg field
    lc = (px - 3.6 * P, gy - 1.9 * P); ax = math.atan2(look[1] - lc[1], look[0] - lc[0]); half = math.radians(17.5); L = 5.0 * P
    od.polygon([lc, (lc[0] + L * math.cos(ax - half), lc[1] + L * math.sin(ax - half)), (lc[0] + L * math.cos(ax + half), lc[1] + L * math.sin(ax + half))], fill=(247, 244, 236, 40), outline=(247, 244, 236, 170))
    # 2 · the high two-shot: the play pitch (38 deg) at 7 m
    hx = 7 * math.cos(math.radians(38)) * P; hy = 7 * math.sin(math.radians(38)) * P; hc = (look[0] - hx, look[1] - hy)
    od.polygon([hc, (look[0] + 40, look[1] + 70), (look[0] - 120, look[1] - 20)], fill=(96, 224, 234, 34), outline=(96, 224, 234, 150))
    im.alpha_composite(ov); d = ImageDraw.Draw(im)
    d.line([hc, look], fill=(96, 224, 234, 255), width=3); d.rectangle((hc[0] - 15, hc[1] - 11, hc[0] + 15, hc[1] + 11), fill=(96, 224, 234, 255))
    d.line([lc, look], fill=CREAM + (255,), width=3); d.rectangle((lc[0] - 13, lc[1] - 9, lc[0] + 13, lc[1] + 9), fill=CREAM + (255,))
    d.text((24, 40), "elevation, along the camera's axis", font=board_font(24), fill=CREAM + (255,))
    d.text((hc[0] + 26, hc[1] - 30), "2 · the high two-shot: the play pitch, 38 deg, 7 m;", font=f, fill=CREAM + (255,)); d.text((hc[0] + 26, hc[1] - 4), "the occlusion cut and the silhouettes stay on", font=f, fill=(176, 205, 199, 255))
    d.text((lc[0] - 90, lc[1] + 34), "1 · 1.9 m high, 8 deg down", font=f, fill=CREAM + (255,))
    d.text((24, 650), "3 · the intercut: a pair more than 6 m apart (a call): each speaker alone,", font=f, fill=CREAM + (255,)); d.text((24, 678), "2.6 m, from where they face, cut with the bubble", font=f, fill=(176, 205, 199, 255))
    d.text((24, 722), "no over-the-shoulder, no one-person scene", font=f, fill=(176, 205, 199, 255))
    return im

if __name__ == "__main__":
    a = Image.open(AS_BUILT_1).convert("RGB").resize((960, 540), Image.LANCZOS); b = Image.open(AS_BUILT_2).convert("RGB").resize((960, 540), Image.LANCZOS)
    p = plan().convert("RGB"); e = elev().convert("RGB")
    diag = Image.new("RGB", (p.width + 24 + e.width, max(p.height, e.height)), BG); diag.paste(p, (0, 0)); diag.paste(e, (p.width + 24, 0))
    items = [
        (a, 0, 0, "As built: the person alone", "a pair more than 3 m apart is shot as one person facing the lens: the other speaker is not in the frame and the bubble floats beside her."),
        (b, 1000, 0, "As built: over the shoulder", "the fallback when neither side is clear: the cell member's back fills a third of the frame, his face is hidden, a palm stands in the foreground."),
        (diag, 0, 680, "The ladder: the first shot that works", "1 the profile two-shot (both faces, nothing between) · 2 the high two-shot (the play camera zoomed to the pair) · 3 the intercut (a call, or a pair too far apart). Always both people, never an over-the-shoulder."),
    ]
    print(compose(items, OUTD + "board-04-conversation-from-the-ground.png", "067 · H · the conversation scene from the ground view",
          "Frames are brief 033's captures of the build (briefs/033/latch-scenes-v2). The diagrams are plan and elevation, 96 px per metre; the people are set to idle and turned to each other at the cut."))
