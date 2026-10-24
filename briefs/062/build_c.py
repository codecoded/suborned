# Glyph, brief 062: direction C "Lobby cards": three framed pictures over the key art, like cards in a cinema foyer.
import sys; sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/062")
from common import *
import portraits as pt, build_b as bb
def build():
    im = Image.open(R + "unity/Assets/Suborned/Resources/Loading/loading_bg.png").convert("RGBA").resize((3840, 2160), Image.LANCZOS)
    im = im.filter(ImageFilter.GaussianBlur(10)); ov = Image.new("RGBA", im.size, INKDEEP + (150,)); im.alpha_composite(ov); vignette(im, 0.5, 0.4)
    w = bb.stage(); 
    txt(im, (960, 96), "THE HANDOVER", "Jost-Medium.ttf", 16, CREAM, "ms", 0.34, shadow=(0.5, 1))
    txt(im, (960, 146), "Vale has a file on you.", "Jost-Bold.ttf", 40, CREAM, "ms", shadow=(0.5, 2))
    W, H, y = 500, 610, 190; xs = (64, 600, 1136)
    pics = []
    ph = pt.photo("councillor", (P(W - 48), P(430)), silver=True); pics.append(("TARGET", ph, None))
    d = w.crop((P(1123 - 245), P(252 - 215), P(1123 + 245), P(252 + 215))).resize((P(W - 48), P(430)), Image.LANCZOS); pics.append(("FRONT DOOR", d, "waitress"))
    c = w.crop((P(1152 - 190), P(346 - 230), P(1152 + 300), P(346 + 200))).resize((P(W - 48), P(430)), Image.LANCZOS); pics.append(("PICKUP", c, "driver"))
    for i, (head, pic, face) in enumerate(pics):
        x = xs[i]; card = frame("panels/panel_card", W, H, 24, 0.5); shadowed(im, card, x, y, blur=22, alpha=0.55, dy=12)
        txt(im, (x + 26, y + 22), head, "Jost-Bold.ttf", 12, CREAM, "ls", 0.14)
        pic = rounded(pic, P(8)); rim = Image.new("RGBA", (pic.width + P(6), pic.height + P(6)), INKDEEP + (255,)); rim = rounded(rim, P(10)); put(im, rim, x + 24 - 3, y + 36 - 3); put(im, pic, x + 24, y + 36)
        if i == 0:
            txt(im, (x + 26, y + 36 + 430 + 54), "Councillor Vale", "Jost-Bold.ttf", 32, INK, "ls"); ico(im, "take", x + 38, y + 36 + 430 + 88, 22, TEXTCARD); txt(im, (x + 60, y + 36 + 430 + 95), "Alive", "Jost-SemiBold.ttf", 19, TEXTCARD, "ls")
            ico(im, "papers", x + 190, y + 36 + 430 + 88, 22, TEXTCARD); txt(im, (x + 212, y + 36 + 430 + 95), "Records Office", "Jost-SemiBold.ttf", 19, TEXTCARD, "ls")
        else:
            cameo(im, x + 66, y + 36 + 430 + 62, 76, pt.file_photo(pt.B + face + ".png", (300, 300), (160, 190, 720, 750)))
            yc = y + 36 + 430 + 62; cx = x + 66
            ico(im, "pointer", cx + 62, yc, 22, TEXTCARD)
            sp = sprite("markers/disc_dark_30", game_h=60); put(im, sp, cx + 128, yc, "mm")
            if i == 1: put(im, sprite("badges/signal_guard", game_h=38), cx + 128, yc, "mm")
            else: ico(im, "car", cx + 128, yc, 32)
            ico(im, "pointer", cx + 194, yc, 22, TEXTCARD)
            put(im, sp, cx + 260, yc, "mm"); ico(im, "enter" if i == 1 else "take", cx + 260, yc, 32)
    # the clock on the fourth, round card
    cx, cy, rr = 1796, 330, 100
    d = ImageDraw.Draw(im, "RGBA"); ang = 360 * 330 / 510.0
    sh = Image.new("RGBA", (P(rr * 2 + 20), P(rr * 2 + 20)), (0, 0, 0, 0)); sd = ImageDraw.Draw(sh); sd.ellipse((P(10), P(10), P(rr * 2 + 10), P(rr * 2 + 10)), fill=CREAM + (255,), outline=CHROME + (255,), width=P(5)); shadowed(im, sh, cx - rr - 10, cy - rr - 10, blur=20, alpha=0.5, dy=10)
    d.ellipse((P(cx - rr + 12), P(cy - rr + 12), P(cx + rr - 12), P(cy + rr - 12)), fill=(236, 233, 224, 255), outline=DEEPTEAL + (255,), width=P(3))
    d.pieslice((P(cx - rr + 20), P(cy - rr + 20), P(cx + rr - 20), P(cy + rr - 20)), -90, -90 + ang, fill=(176, 226, 226, 255))
    for k in range(17):
        a = math.radians(360 * k / 17); r0 = rr - 28 if k % 2 else rr - 34
        d.line((P(cx + r0 * math.sin(a)), P(cy - r0 * math.cos(a)), P(cx + (rr - 22) * math.sin(a)), P(cy - (rr - 22) * math.cos(a))), fill=INK + (255,), width=P(1.6))
    ha = math.radians(ang); d.line((P(cx), P(cy), P(cx + (rr - 40) * math.sin(ha)), P(cy - (rr - 40) * math.cos(ha))), fill=INK + (255,), width=P(4)); d.ellipse((P(cx - 7), P(cy - 7), P(cx + 7), P(cy + 7)), fill=INK + (255,))
    ico(im, "enter", cx, cy - 56, 26, DEEPTEAL); txt(im, (cx, cy + 62), "5:30", "Jost-Bold.ttf", 34, INK, "ms")
    txt(im, (cx - rr + 16, cy + rr + 36), "0:00", "Jost-Bold.ttf", 16, CREAM, "ls", shadow=(0.5, 1)); txt(im, (cx + rr - 16, cy + rr + 36), "8:30", "Jost-Bold.ttf", 16, CREAM, "rs", shadow=(0.5, 1))
    bb.strip_bottom(im); return im
if __name__ == "__main__":
    im = build(); im.convert("RGB").save(OUT + "dir-c-cards.png"); im.resize((1920, 1080), Image.LANCZOS).convert("RGB").save(SC + "dir-c-1080.png"); print("ok")
