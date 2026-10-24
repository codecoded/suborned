# Glyph, brief 057: board of the three lovers icons (python3 -I briefs/057/lovers/board.py -> briefs/057/board-13-lovers.png)
import sys, os
sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/057")
import kitdraw as kd
from kitdraw import *
HERE = os.path.dirname(os.path.abspath(__file__))
BG = (23, 48, 47)
CANDS = [("kin", K + "icons/singles/kin@2x.png", "today: kin"),
         ("a", HERE + "/lovers_a@2x.png", "A  two hearts"), ("b", HERE + "/lovers_b@2x.png", "B  couple + heart  (pick)"), ("c", HERE + "/lovers_c@2x.png", "C  couple in a heart")]
def ic(path, game_px, rgb=CREAM):
    im = Image.open(path).convert("RGBA"); n = int(round(game_px * S)); return tint(im.resize((n, n), Image.LANCZOS), rgb)
FT = lambda n, w="Medium": ImageFont.truetype(K + "fonts/Jost-%s.ttf" % w if os.path.exists(K + "fonts/Jost-%s.ttf" % w) else K + "fonts/Jost-Bold.ttf", n)
try: CAP = ImageFont.truetype(R + "testbed/hud-style/fonts/Jost-Medium-Tab.ttf", 24); CAPS = ImageFont.truetype(R + "testbed/hud-style/fonts/Jost-Medium-Tab.ttf", 18)
except Exception: CAP = CAPS = ImageFont.load_default()
BOLD = lambda px: font("Jost-Bold.ttf", px)
def token(path, state="learned"):
    """the Intel secret tab (19 x 15 game px, rim solid = heard, dashed = told) with the candidate icon at 10 px"""
    im = token_img("secret", None, state); g = ic(path, 10); im.alpha_composite(g, (int((im.width - g.width) / 2), int((im.height - g.height) / 2))); return im
def chip_line(path, txt="Loves the guard"):
    """a Done-chip row: icon 16 + Jost Bold 15 on panel_dark (as the gain chip's fact line)"""
    w = int(G(10 + 16 + 6 + 12) + BOLD(15).getlength(txt)); h = int(G(30))
    p = nine("markers/plate_dark", w, h, 24, S / 2); d = ImageDraw.Draw(p)
    g = ic(path, 16); p.alpha_composite(g, (int(G(10)), int((h - g.height) / 2)))
    d.text((G(10 + 16 + 6), h / 2 + G(0.6)), txt, font=BOLD(15), fill=CREAM, anchor="lm"); return p
def build():
    rows = len(CANDS); colw = 300; W = 1720; H = 150 + rows * 232 + 150
    im = Image.new("RGB", (W, H), BG); d = ImageDraw.Draw(im)
    d.text((30, 20), "057 - the lovers icon: three candidates, pick B (named `lovers`)", font=CAP, fill=CREAM)
    d.text((30, 56), "Solid white pictograms, 64 px @2x, as the kit's singles; shown tinted cream at game px x 2.98 (1080p shot scale) on the kit's deep teal.", font=CAPS, fill=DIM)
    heads = ["big (64 px)", "10 px", "12 px", "14 px", "16 px", "Intel tab", "told (dashed)", "Done chip line"]
    xs = [250, 460, 545, 630, 715, 860, 990, 1120]
    y0 = 110
    for i, h in enumerate(heads): d.text((xs[i], y0), h, font=CAPS, fill=DIM)
    for r, (k, path, cap) in enumerate(CANDS):
        y = y0 + 40 + r * 232
        d.text((30, y + 90), cap, font=CAPS, fill=CREAM if k == "b" else DIM)
        if k == "b": d.rounded_rectangle((20, y - 8, W - 20, y + 212), radius=14, outline=(96, 224, 234), width=2)
        big = ic(path, 64 / S * 2.9 / 2.9 * 1.0, CREAM); big = Image.open(path).convert("RGBA").resize((192, 192), Image.LANCZOS); big = tint(big, CREAM)
        d.rectangle((xs[0], y, xs[0] + 192, y + 192), fill=SLOTINK); im.paste(big, (xs[0], y), big)
        x = xs[1]
        for s in (10, 12, 14, 16):
            cell = Image.new("RGBA", (int(G(24)), int(G(24))), SLOTINK + (255,)); g = ic(path, s); cell.alpha_composite(g, ((cell.width - g.width) // 2, (cell.height - g.height) // 2))
            im.paste(cell.convert("RGB"), (x, y + 20 + (cell.height - cell.height))); x += cell.width + 10
        # reposition: the loop above steps by cell widths
        t = token(path); im.paste(t.convert("RGB") if False else Image.new("RGB", t.size, BG), (xs[5], y + 30)); im.paste(t, (xs[5], y + 30), t)
        t2 = token(path, "told"); im.paste(t2, (xs[6], y + 30), t2)
        c = chip_line(path); im.paste(c, (xs[7] - 40 if False else xs[7], y + 25), c)
        # true 1x (game px) strip
        x1 = xs[5]
        for s in (10, 12, 14, 16):
            g = Image.open(path).convert("RGBA").resize((s, s), Image.LANCZOS); g = tint(g, CREAM); c1 = Image.new("RGBA", (s + 8, s + 8), SLOTINK + (255,)); c1.alpha_composite(g, (4, 4)); im.paste(c1, (x1, y + 120)); x1 += s + 14
    y = y0 + 40 + rows * 232
    notes = ["Pick B: the only candidate that says two people before it says love; it stays distinct from `kin` (a standing adult and child) by the heart over the pair and by the busts.",
             "A reads fastest at 10 px but a lone heart pair is the 'like'/health shape (s_hurt is a half heart): fallback if B turns muddy in the Intel tab.  C is neat but its dots read as a button at 12 px.",
             "No outline, no gradient, no colour: alpha only, tinted cream in USS (.i-lovers). Latch: swap `kin` for `lovers` where the secret is an affair (HostDone.cs:268, HudToolkit.Intel.cs:204)."]
    for i, n in enumerate(notes): d.text((30, y + 10 + i * 30), n, font=CAPS, fill=CREAM if i == 0 else DIM)
    im.save("/Users/sky/Projects/suborned/briefs/057/board-13-lovers.png"); print(im.size)
build()
