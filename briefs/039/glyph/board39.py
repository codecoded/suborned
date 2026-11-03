# Glyph, brief 039: a small board composer. Rows of tiles at native 2 px per game px, each with a Jost caption and a Barlow note wrapped to the tile's own width.
import sys
sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/039/glyph")
from heroes39 import *

CAPF, SMALLF = board_font(29), body_font(22)
def wrap_to(txt, font, width):
    words = txt.split(); lines = []; cur = ""
    for w in words:
        t = (cur + " " + w).strip()
        if font.getlength(t) <= width: cur = t
        else: lines.append(cur); cur = w
    if cur: lines.append(cur)
    return lines

def grid(rows, path, title, sub, W=2400, pad=28, gap=20, row_gap=34, top=176, bottom=26, min_w=0):
    """rows: list of rows; a row is a list of dicts(im=PIL RGB, cap=str, small=str, w=optional wrap width). Tiles flow left to right; the row's height is its tallest tile plus its text."""
    prepared = []; total_h = top
    for row in rows:
        items = []; rh = 0
        for t in row:
            im = t["im"]; wrap_w = max(t.get("w", im.width), 200)
            cl = wrap_to(t.get("cap", ""), CAPF, wrap_w) if t.get("cap") else []; sl = wrap_to(t.get("small", ""), SMALLF, wrap_w) if t.get("small") else []
            th = im.height + 12 + 36 * len(cl) + 8 + 29 * len(sl); items.append((t, cl, sl, th, wrap_w)); rh = max(rh, th)
        prepared.append((items, rh)); total_h += rh + row_gap
    H = total_h + bottom - row_gap
    out = Image.new("RGB", (W, H), BG); d = ImageDraw.Draw(out); d.text((pad, 26), title, font=board_font(46), fill=BCREAM)
    for k, ln in enumerate(wrap_to(sub, body_font(25), W - 2 * pad)): d.text((pad, 88 + 31 * k), ln, font=body_font(25), fill=(176, 205, 199))
    y = top
    for items, rh in prepared:
        x = pad; row_w = sum(max(t["im"].width, wr) for t, _, _, _, wr in items) + gap * (len(items) - 1)
        for t, cl, sl, th, wr in items:
            im = t["im"]; out.paste(im, (x, y)); yy = y + im.height + 12
            for ln in cl: d.text((x, yy), ln, font=CAPF, fill=BCREAM); yy += 36
            yy += 8
            for ln in sl: d.text((x, yy), ln, font=SMALLF, fill=(176, 205, 199)); yy += 29
            x += max(im.width, wr) + gap
        if x - gap > W - pad + 1: print("WARNING: row wider than the board:", x - gap, ">", W - pad)
        y += rh + row_gap
    out.save(path); return out.size

def tile(fr, box, up=1.0, **kw):
    im = fr.crop(box, **kw).convert("RGB")
    return im if up == 1.0 else im.resize((int(im.width * up), int(im.height * up)), Image.LANCZOS)

def hstack(ims, gap=16, bg=BG):
    W = sum(i.width for i in ims) + gap * (len(ims) - 1); H = max(i.height for i in ims); out = Image.new("RGB", (W, H), bg); x = 0
    for i in ims: out.paste(i, (x, 0)); x += i.width + gap
    return out
