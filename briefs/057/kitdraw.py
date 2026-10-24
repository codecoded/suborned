# Glyph, brief 057: a small drawing kit that redraws the 0.2.99 ring pills from the game's own kit sprites, to scale with Sky's retina shots.
# Everything is read from unity/Assets/Suborned/UI/Resources/Kit (sprites, icons, badges, fonts); no colour here lacks a source.
from PIL import Image, ImageDraw, ImageFont
import numpy as np
R = "/Users/sky/Projects/suborned/"
K = R + "unity/Assets/Suborned/UI/Resources/Kit/"
SHOT = "/Users/sky/.t3/userdata/attachments/6f2fb9e2-3d6f-4cb6-bf62-66a9d065638d-86892b12-3d65-42e1-8543-b82513f95414.png"
SHOT2 = "/Users/sky/.t3/userdata/attachments/6f2fb9e2-3d6f-4cb6-bf62-66a9d065638d-492b3c42-f03a-4aaf-b086-ccc2b0e27e8f.png"
S = 2.98   # shot px per game px, measured on the ring (RingOuter 84 + rim = 178 game px = 530 shot px)
CREAM = (247, 244, 236); INKDEEP = (11, 22, 21); SLOTINK = (10, 38, 36); TURQL = (96, 224, 234); DIM = (176, 205, 199)
_cache = {}
def load(path):
    if path not in _cache: _cache[path] = Image.open(path).convert("RGBA")
    return _cache[path]
def font(name, game_px):
    return ImageFont.truetype(K + "fonts/" + name, int(round(game_px * S)))
def tint(im, rgb):
    a = im.split()[3]; out = Image.new("RGBA", im.size, rgb + (255,)); out.putalpha(a); return out
def icon(name, game_px, rgb=CREAM):
    im = load(K + "icons/singles/%s@2x.png" % name); n = int(round(game_px * S))
    return tint(im.resize((n, n), Image.LANCZOS), rgb)
def sprite(rel, game_h=None, game_w=None):
    im = load(K + rel + "@2x.png")
    f = (game_h * S / im.height) if game_h else (game_w * S / im.width)
    return im.resize((max(1, int(round(im.width * f))), max(1, int(round(im.height * f)))), Image.LANCZOS)
def nine(rel, w, h, sl, f):
    """nine-slice the @2x sprite to w x h shot px; slice sl (@2x px); f = scale of the sprite to the shot"""
    im = load(K + rel + "@2x.png"); s = int(round(sl * f)); sw, sh = im.size
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    xs = [0, sl, sw - sl, sw]; ys = [0, sl, sh - sl, sh]
    xd = [0, s, w - s, w]; yd = [0, s, h - s, h]
    for i in range(3):
        for j in range(3):
            if xd[i + 1] <= xd[i] or yd[j + 1] <= yd[j]: continue
            piece = im.crop((xs[i], ys[j], xs[i + 1], ys[j + 1])).resize((xd[i + 1] - xd[i], yd[j + 1] - yd[j]), Image.LANCZOS)
            out.paste(piece, (xd[i], yd[j]))
    return out
def plate(w, lit=False):
    """plate_dark at the ring's pill size: 30 game px tall element (22 visible), w shot px wide"""
    h = int(round(30 * S)); p = nine("markers/plate_dark", w, h, 24, S / 2)
    if lit:
        a = np.array(p).astype(float); a[..., 0] *= 0.75; a[..., :3] = np.clip(a[..., :3], 0, 255); p = Image.fromarray(a.astype("uint8"), "RGBA")
    return p
def paste(base, im, x, y):
    base.alpha_composite(im, (int(round(x)), int(round(y))))
def text_w(txt, fnt): return fnt.getlength(txt)

# ---- the ring's pill: word, then fields (icon + number), then marks; game-px metrics tuned against the shot's Pay pill
G = lambda v: v * S
WORD = lambda: font("Jost-Regular-Tab.ttf", 14.4)
NUM = lambda: font("Jost-Bold.ttf", 13.7)
def tok_w(t):
    k = t[0]
    if k == "word": return text_w(t[1], WORD())
    if k == "num": return text_w(t[1], NUM())
    if k == "icon": return G(t[2] if len(t) > 2 else 15)
    if k == "gap": return G(t[1])
    if k == "badge": return G(t[2] if len(t) > 2 else 17)
    if k == "w": return G(t[1])
    if k == "fact": return G(SHAPES[t[1]][0])
    if k == "mark": return G(16)
    if k == "cameo": return G(18)
    if k == "struck": return font("Jost-Regular-Tab.ttf", t[2] if len(t) > 2 else 11.5).getlength(t[1]) + 4
    return 0
def pill_width(tokens, padl=6, padr=6):
    return int(sum(tok_w(t) for t in tokens) + G(padl + padr) + 2 * G(4))
def draw_pill(base, tokens, x, yc, lit=False, padl=6, padr=6, width=None, extra=None):
    """x = visible left edge (shot px), yc = visible centre y. returns the visible box and the x of each token (for marks)."""
    w = width or pill_width(tokens, padl, padr)
    el = plate(w, lit); eh = el.height
    paste(base, el, x - G(4), yc - eh / 2)
    cx = x + G(padl) + 0; xs = []
    d = ImageDraw.Draw(base)
    for t in tokens:
        k = t[0]; xs.append(cx)
        if k == "word":
            f = WORD(); d.text((cx, yc + G(0.9)), t[1], font=f, fill=CREAM, anchor="lm")
        elif k == "num":
            f = NUM(); d.text((cx, yc + G(0.6)), t[1], font=f, fill=t[2] if len(t) > 2 else CREAM, anchor="lm")
        elif k == "icon":
            sz = t[2] if len(t) > 2 else 15
            im = icon(t[1], sz, t[3] if len(t) > 3 else CREAM); paste(base, im, cx, yc - im.height / 2)
        elif k == "badge":
            sz = t[2] if len(t) > 2 else 17
            im = sprite(t[1], game_h=sz); paste(base, im, cx, yc - im.height / 2)
        elif k == "fact":
            im = token_img(t[1], t[2], t[3] if len(t) > 3 else "learned"); paste(base, im, cx, yc - im.height / 2)
            if len(t) > 4 and t[4]: nub(base, cx + im.width - G(0.8), yc - im.height / 2 + G(0.8))
        elif k == "mark":
            im = sprite("badges/mark_" + t[1], game_h=16); paste(base, im, cx, yc - im.height / 2)
            pass
        elif k == "cameo":
            im = cameo_img(t[1]); paste(base, im, cx, yc - im.height / 2)
        elif k == "struck":
            struck(base, cx, yc, t[1], t[2] if len(t) > 2 else 11.5)
        cx += tok_w(t)
    return (x, yc - G(11), x + w - G(8), yc + G(11)), xs


# ---- facts: the Intel card's three slot shapes (circle need, rounded square temperament, tab secret), solid = learned
INK = (23, 48, 47)
SHAPES = {"need": (15, 15, 7.5), "temp": (15, 15, 4.5), "secret": (19, 15, 4.0)}
def token_img(kind, ic, state="learned", size_scale=1.0):
    """a slot-shaped token as an RGBA image (4x supersampled). state: learned (solid rim), told (long dashes), unk (dots and ?)"""
    w, h, r = SHAPES[kind]; w *= size_scale; h *= size_scale; r *= size_scale
    W, H = int(round(w * S)), int(round(h * S)); Q = 4
    im = Image.new("RGBA", (W * Q, H * Q), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    pad = 2 * Q
    d.rounded_rectangle((pad, pad, W * Q - pad - 1, H * Q - pad - 1), radius=r * S * Q, fill=SLOTINK + (255,))
    lw = int(round(1.3 * S / 2 * Q))   # 1.3 px rim at the kit's 2x
    def rim(box, rad, dash):
        import math
        if not dash:
            d.rounded_rectangle(box, radius=rad, outline=CREAM + (255,), width=lw); return
        # dashed: sample the rounded rect perimeter
        x0, y0, x1, y1 = box; pts = []
        n = 480
        # perimeter walk
        per = []
        def arc(cx, cy, a0):
            for i in range(9):
                a = math.radians(a0 + 90 * i / 8); per.append((cx + rad * math.cos(a), cy + rad * math.sin(a)))
        per.append((x0 + rad, y0)); per.append((x1 - rad, y0)); arc(x1 - rad, y0 + rad, -90)
        per.append((x1, y1 - rad)); arc(x1 - rad, y1 - rad, 0); per.append((x0 + rad, y1)); arc(x0 + rad, y1 - rad, 90)
        per.append((x0, y0 + rad)); arc(x0 + rad, y0 + rad, 180); per.append((x0 + rad, y0))
        L = [0]
        for i in range(1, len(per)): L.append(L[-1] + math.dist(per[i - 1], per[i]))
        on, off = dash; tot = L[-1]; t = 0; j = 1
        def at(s):
            nonlocal j
            j = 1
            while j < len(L) - 1 and L[j] < s: j += 1
            f = (s - L[j - 1]) / max(1e-6, L[j] - L[j - 1]); a, b = per[j - 1], per[j]
            return (a[0] + (b[0] - a[0]) * f, a[1] + (b[1] - a[1]) * f)
        while t < tot:
            seg = [at(min(tot, t + k * (on / 6))) for k in range(7)]
            d.line(seg, fill=CREAM + (255,), width=lw); t += on + off
    box = (pad, pad, W * Q - pad - 1, H * Q - pad - 1)
    if state == "learned": rim(box, r * S * Q, None)
    elif state == "told": rim(box, r * S * Q, (3 * S / 2 * Q, 2 * S / 2 * Q))
    else: rim(box, r * S * Q, (1.0 * S / 2 * Q, 2.2 * S / 2 * Q))
    im = im.resize((W, H), Image.LANCZOS)
    if state == "unk":
        dd = ImageDraw.Draw(im); f = font("Jost-Bold.ttf", 12.5); dd.text((W / 2, H / 2 + G(0.4)), "?", font=f, fill=CREAM + (180,), anchor="mm")
    elif ic:
        g = icon(ic, 10 * size_scale); im.alpha_composite(g, (int((W - g.width) / 2), int((H - g.height) / 2)))
    return im
def nub(base, x, y, g=6):
    """the kit's dot_unread, centred on x, y"""
    im = sprite("markers/dot_unread", game_h=g); paste(base, im, x - im.width / 2, y - im.height / 2)
def sparkle(base, cx, cy, r=6.0):
    import math
    Q = 4; n = int(round(r * S * 2.6)); im = Image.new("RGBA", (n * Q, n * Q), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    c = n * Q / 2; R_ = r * S * Q; pts = []
    for i in range(8):
        a = math.radians(-90 + 45 * i); rr = R_ if i % 2 == 0 else R_ * 0.34; pts.append((c + rr * math.cos(a), c + rr * math.sin(a)))
    d.polygon(pts, fill=CREAM + (255,), outline=INKDEEP + (255,)); 
    im = im.resize((n, n), Image.LANCZOS); paste(base, im, cx - n / 2, cy - n / 2)
def struck(base, x, yc, txt, size=11.5):
    """a dim old value with a line through it (the number the card showed before): returns its width"""
    f = font("Jost-Regular-Tab.ttf", size); d = ImageDraw.Draw(base); w = f.getlength(txt)
    d.text((x, yc + G(0.5)), txt, font=f, fill=DIM, anchor="lm"); d.line((x - 2, yc + G(0.3), x + w + 2, yc + G(0.3)), fill=DIM, width=max(2, int(G(0.7))))
    return w

CHROME = (203, 198, 195)
def cameo_img(badge, game=18):
    """the kit's cameo: the picture in a chrome ring (2 px #cbc6c3 over a 1 px ink-deep line), as the faces in the person pills"""
    n = int(round(game * S)); Q = 4; im = Image.new("RGBA", (n * Q, n * Q), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    d.ellipse((0, 0, n * Q - 1, n * Q - 1), fill=INKDEEP + (255,))
    r1 = int(round(1 * S / 2 * Q)); r2 = int(round(2 * S / 2 * Q)) + r1
    d.ellipse((r1, r1, n * Q - 1 - r1, n * Q - 1 - r1), fill=CHROME + (255,))
    d.ellipse((r2, r2, n * Q - 1 - r2, n * Q - 1 - r2), fill=(11, 22, 21, 255))
    im = im.resize((n, n), Image.LANCZOS)
    inner = int(round((game - 2 * 2.2) * S)); b = load(K + badge + "@2x.png").resize((inner, inner), Image.LANCZOS)
    m = Image.new("L", (inner, inner), 0); ImageDraw.Draw(m).ellipse((0, 0, inner - 1, inner - 1), fill=255)
    b = b.copy(); a = np.minimum(np.array(b.split()[3]), np.array(m)); b.putalpha(Image.fromarray(a))
    im.alpha_composite(b, ((n - inner) // 2, (n - inner) // 2)); return im
