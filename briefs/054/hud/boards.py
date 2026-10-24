# Boards for "HUD for 0.29" (brief 054). Reads the lab's 2x shots (out/scratch/glyph/2x/<scene>.png, made by testbed/hud-style/shot.ts) and the icon PNGs; writes the boards beside this file.
# Run: python3 -I briefs/054/hud/boards.py   (the lab server must have been up when the shots were taken)
from PIL import Image, ImageDraw, ImageFont
import os
R = "/Users/sky/Projects/suborned/"
S = R + "out/scratch/glyph/2x/"
OUT = R + "briefs/054/hud/"
F = R + "testbed/hud-style/fonts/Jost-Medium-Tab.ttf"
BG = (23, 48, 47); INK = (247, 244, 236); DIM = (176, 205, 199)
def font(n): return ImageFont.truetype(F, n)
def crop(scene, x, y, w, h):   # game px box -> its 2x picture
    return Image.open(S + scene + ".png").convert("RGB").crop((x * 2, y * 2, (x + w) * 2, (y + h) * 2))
def sheet(panels, cols, name, title, pad=30, capH=64, head=90):
    rows = [panels[k:k + cols] for k in range(0, len(panels), cols)]
    rh = [max(p[0].height for p in r) + capH + pad for r in rows]
    rw = [sum(p[0].width + pad for p in r) for r in rows]
    im = Image.new("RGB", (pad + max(rw), head + sum(rh)), BG); d = ImageDraw.Draw(im)
    d.text((pad, 24), title, font=font(34), fill=INK)
    y = head
    for r, h in zip(rows, rh):
        x = pad
        for pic, cap, sub in r:
            d.text((x, y), cap, font=font(26), fill=INK); d.text((x, y + 32), sub, font=font(20), fill=DIM)
            im.paste(pic, (x, y + capH)); x += pic.width + pad
        y += h
    im.save(OUT + name); print(name, im.size)
def wide(scene, x, y, w, h, W=1280):
    return crop(scene, x, y, w, h).resize((W, int(W * h / w)), Image.LANCZOS)
BOX = (120, 470, 760, 420)       # the café terrace: the customer, Inez, the guard, the pill
sheet([
    (crop("w29-quiet", *BOX), "1  Hover her, Pressure armed, Alert 0 or 1", "she does not care: hover ring only, no cone. Inez wears the uneasy disc"),
    (crop("w29-cone-a2", *BOX), "2  The same at Alert 2", "she would see it: her cone, rim only, cut by the café wall"),
    (crop("w29-takedown", 20, 640, 760, 420), "3  Takedown armed on the passer-by (any Alert)", "her cone with the topiary's dead ground hatched, and the 1.5 m near-body ring"),
    (wide("w29-guard-ring", 0, 330, 1000, 656), "4  Hover the off-duty guard, same Pressure (the whole ring, 1.3x smaller)", "security see all round: a thin 13 m red ring, clipped by the café wall and the corner building"),
], 2, "board-01-civilian-cone-when.png", "Civilian sight on hover, only where it matters (2x crops of the play-zoom lab frames)")
sheet([
    (crop("w29-alt", 20, 380, 1100, 700), "5  Alt, Alert 2: the armed order's people (cap 6)", "her cone and his ring, one rim each; everyone else stays bare"),
    (wide("w29-told-apart", 60, 640, 1560, 290, 2100), "6  Told apart on one pavement (cut to 5 m and 4 m to fit)", "civilian cone: cream rim, no fill · security cone: red, filled · security ring: thin red · near-body ring: dashed cream, 1.5 m · hover ring (as built)"),
], 1, "board-02-alt-and-told-apart.png", "Alt, and the marks side by side")
# the fan
sheet([
    (crop("w29-fan-today", 700, 400, 700, 460), "7  Today: the fan runs through the cypress", "flat fill 38 %, rim 0.085 m, no shadow"),
    (crop("w29-fan-cut", 700, 400, 700, 460), "8  0.28: Fan.Shadows cut out", "a 6.2 m cypress on the guard's line hides the strip behind it; the cut has its own thin rim"),
], 2, "board-04-security-fan-shadows.png", "Security sight with real heights (the door guard on his landing, eye 2.35 m)")
# mood, price, verbs
icons = Image.new("RGB", (900, 220), BG); di = ImageDraw.Draw(icons)
for i, n in enumerate(["friendly", "uneasy", "scared"]):
    a = Image.open(R + "briefs/054/hud/icons/mood_%s@2x.png" % n).convert("RGBA")
    big = a.resize((128, 128), Image.LANCZOS); icons.paste(Image.new("RGB", big.size, INK), (20 + i * 290, 14), big)
    for j, s in enumerate((32, 16)):
        sm = a.resize((s, s), Image.LANCZOS); icons.paste(Image.new("RGB", sm.size, INK), (170 + i * 290 + j * 52, 70), sm)
    di.text((20 + i * 290, 160), n, font=font(24), fill=DIM)
sheet([
    (crop("w29-mood", 150, 510, 440, 230), "9  Mood on the person: one kit disc, one face, no words", "friendly, uneasy, scared. Inez also carries the Scout's listening disc: two icons, the most"),
    (icons, "10  The three faces at 64 px, 32 px (the disc) and 16 px", "solid pictograms, 64 px @2x, cream; one family, three silhouettes"),
    (crop("w29-price", 90, 100, 980, 350), "11  The price line: before a Watch, after a Watch, after Listen", "dashed = the Scout's read (likely: Pressure on the uneasy; dashed half: Help is worse), solid kit marks once Listen has told the temperament"),
    (crop("w29-watch-on", 740, 770, 440, 260), "12  Verb plate, Watch offered", "Pressure wears the dashed check, Help the dashed half"),
    (crop("w29-watch-off", 740, 770, 440, 260), "13  No Watch to give (the driver)", "no Watch knob; the rows close up, none is struck or empty"),
], 2, "board-03-mood-watch-price.png", "Watch offer, mood and the price line")
