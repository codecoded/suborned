# Glyph, brief 057 part 2: board of the patrol cameo disc (python3 -I briefs/057/patrol/board.py -> briefs/057/board-14-patrol-disc.png)
import sys, os
sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/057")
from kitdraw import *
import kitdraw as kd
HERE = os.path.dirname(os.path.abspath(__file__))
BG = (23, 48, 47); PAVE = (226, 214, 190)   # the pavement behind the in-world cameos: the cream stone of the street, for contrast only
CAP = ImageFont.truetype(R + "testbed/hud-style/fonts/Jost-Medium-Tab.ttf", 24); CAPS = ImageFont.truetype(R + "testbed/hud-style/fonts/Jost-Medium-Tab.ttf", 18)
def L(p): return Image.open(p).convert("RGBA")
def big(p, n=192): return L(p).resize((n, n), Image.NEAREST if False else Image.LANCZOS)
def cameo_with(face_rel, game=18):
    return kd.cameo_img(face_rel, game)
def build():
    W, H = 1700, 1290; im = Image.new("RGB", (W, H), BG); d = ImageDraw.Draw(im)
    d.text((30, 20), "057 - the patrol's cameo disc: slot ready for Forma's face (brief 063)", font=CAP, fill=CREAM)
    d.text((30, 56), "The disc is the kit's own npc_* badge (same bezel, same enamel field, same ink); only the face layer changes. Stand-in screen drawn here until Forma's art arrives.", font=CAPS, fill=DIM)
    # 1 anatomy: template with slot guides, template alone, stand-in
    y = 110; d.text((30, y), "1  The slot (48 px badge, shown x4)", font=CAP, fill=CREAM)
    sc = 4; n = 48 * sc
    tpl = L(HERE + "/npc_disc_template@2x.png").resize((n, n), Image.LANCZOS)
    g = tpl.copy(); gd = ImageDraw.Draw(g)
    def circ(r, col, w=2, dash=False): gd.ellipse(((24 - r) * sc, (24 - r) * sc, (24 + r) * sc, (24 + r) * sc), outline=col, width=w)
    circ(17, (96, 224, 234)); circ(14.5, (240, 160, 32), 2)
    b = [9.5, 9.5, 38.5, 38.5]; gd.rectangle([v * sc for v in b], outline=(255, 255, 255), width=2)
    x0 = 30
    for pic, cap, sub in [(tpl, "the disc, empty", "the five npc_* discs, glyphs lifted"), (g, "the slot", "field r 17 · safe r 14.5 · box 29 px"),
                          (L(HERE + "/npc_patrol@2x.png").resize((n, n), Image.LANCZOS), "stand-in screen", "ink screen, cream dot smile"),
                          (L(HERE + "/npc_patrol_face@2x.png").resize((n, n), Image.LANCZOS), "face for Cameo()", "no bezel: Cameo() adds the ring")]:
        d.rectangle((x0, y + 40, x0 + n, y + 40 + n), fill=BG); im.paste(pic, (x0, y + 40), pic)
        d.text((x0, y + 40 + n + 6), cap, font=CAPS, fill=CREAM); d.text((x0, y + 40 + n + 30), sub[:46], font=CAPS, fill=DIM)
        if len(sub) > 46: d.text((x0, y + 40 + n + 52), sub[46:], font=CAPS, fill=DIM)
        x0 += n + 90
    # 2 the family at game size (24 px), npc_* vs the new one, on plate_dark (known-people discs, action panel header)
    y2 = 470; d.text((30, y2), "2  The family at 24 game px (known-people discs, action panel header)", font=CAP, fill=CREAM)
    names = ["npc_waitress", "npc_driver", "npc_councillor", "npc_watcher", "npc_passer"]
    pw = int(G(24 * 6 + 8 * 7)); strip = nine("markers/plate_dark", pw, int(G(36)), 24, S / 2); sd = strip
    xx = G(10)
    for nm in names + ["PATROL"]:
        p = K + "badges/%s@2x.png" % nm if nm != "PATROL" else HERE + "/npc_patrol@2x.png"
        b2 = L(p).resize((int(G(24)), int(G(24))), Image.LANCZOS); strip.alpha_composite(b2, (int(xx), int((strip.height - b2.height) / 2))); xx += G(32)
    im.paste(strip, (30, y2 + 40), strip)
    d.text((30 + pw + 30, y2 + 50), "waitress · driver · councillor · watcher · passer · patrol (stand-in)", font=CAPS, fill=DIM)
    d.text((30 + pw + 30, y2 + 78), "the patrol is the one machine here: the screen face is a machine's, the bezel and field are the family's", font=CAPS, fill=DIM)
    # 3 in the Reroute pill: today (signal_unit stand-in: amber) vs the face in the Cameo ring
    y3 = 640; d.text((30, y3), "3  Where it lands: the Reroute pill, the wait-spot cameo and the edge chip (the HUD's Cameo ring, 2 px chrome over 1 px ink-deep)", font=CAP, fill=CREAM)
    rows = [("today: signal_unit (amber, a signal colour on a pill)", "badges/signal_unit"), ("npc_patrol_face in the same Cameo ring", "badges/npc_patrol_face")]
    for r, (cap, rel) in enumerate(rows):
        yy = y3 + 44 + r * 175
        d.text((30, yy), cap, font=CAPS, fill=DIM)
        pill = Image.new("RGBA", (int(G(260)), int(G(44))), PAVE + (255,))
        tk = [("word", "Reroute"), ("gap", 5), ("cameo", rel), ("gap", 9), ("icon", "p_time"), ("gap", 1), ("num", "80 s"), ("gap", 10), ("icon", "p_trail"), ("gap", 1), ("num", "+1")]
        draw_pill(pill, tk, G(8), pill.height / 2, lit=True)
        im.paste(pill.convert("RGB"), (30, yy + 28))
        # wait-spot cameo on paving: dashed ellipse and the cameo (18 px)
        wx = 760
        sp = Image.new("RGBA", (int(G(70)), int(G(44))), PAVE + (255,)); sd2 = ImageDraw.Draw(sp)
        cx, cy = sp.width / 2, sp.height * 0.62; rx, ry = G(23), G(11)
        import math
        for k in range(36):
            a0 = 360 * k / 36; sd2.arc((cx - rx, cy - ry, cx + rx, cy + ry), a0, a0 + 6, fill=INKDEEP, width=int(G(0.9)))
        c = cameo_with(rel, 18); sp.alpha_composite(c, (int(cx - c.width / 2), int(cy - ry - c.height * 0.55)))
        im.paste(sp.convert("RGB"), (wx, yy + 28))
        # edge chip: plate_dark 22 px, cameo 12 + pointer 12
        ex = 1000; chip = nine("markers/plate_dark", int(G(46)), int(G(30)), 24, S / 2); c2 = cameo_with(rel, 12); chip.alpha_composite(c2, (int(G(9)), int((chip.height - c2.height) / 2)))
        pt = icon("pointer", 12); chip.alpha_composite(pt, (int(G(9 + 12 + 5)), int((chip.height - pt.height) / 2))); im.paste(chip, (ex, yy + 34), chip)
        # the cameo alone at 18 and 12 (game px) and true 1x
        x1 = 1160
        for s in (24, 18, 12):
            c3 = cameo_with(rel, s); bgc = Image.new("RGBA", (int(G(30)), int(G(30))), PAVE + (255,)); bgc.alpha_composite(c3, ((bgc.width - c3.width) // 2, (bgc.height - c3.height) // 2)); im.paste(bgc.convert("RGB"), (x1, yy + 28)); x1 += bgc.width + 14
        x1 += 30
        for s in (24, 18, 12):
            c4 = kd.cameo_img(rel, s).resize((s, s), Image.LANCZOS); im.paste(c4, (x1, yy + 40), c4); x1 += s + 12
    # 4 how it drops in
    y4 = y3 + 44 + 2 * 175 + 20
    import textwrap
    txt = ["Drop-in: Forma's face goes to briefs/057/patrol/, then `python3 -I briefs/057/patrol/disc.py build FACE.png` (a glyph: ink on cream, fitted to the 29 px box) or `build FACE.png --fill` (a portrait: fills the field like the cell's cameos); `install` copies npc_patrol@2x.png and npc_patrol_face@2x.png into Kit/badges/ (kit.uss has .s-npc_patrol).",
           "Latch: HudModel.Badge: the patrol -> badges/npc_patrol; HudToolkit.Reroute.cs RrCameo and the edge chip: badges/signal_unit -> badges/npc_patrol_face (inside Cameo(), which supplies the ring).",
           "Art for Forma: ink #17302f on the field's cream, or a cream-enamel head with its dark dot-matrix screen; no colour but the kit's; reads at 12 px (the edge chip): a screen and two eyes, nothing finer than 3 dots."]
    yy = y4
    for i, para in enumerate(txt):
        for ln in textwrap.wrap(para, 175): d.text((30, yy), ln, font=CAPS, fill=CREAM if i < 2 else DIM); yy += 26
        yy += 6
    im.save("/Users/sky/Projects/suborned/briefs/057/board-14-patrol-disc.png"); print(im.size)
build()
