# Brief 024, "Revision: calm ring, clear default". Draws the mockups for both moments on Sky's two screenshots.
#   python3 -I briefs/024/calm-ring/retouch.py     (paints the old plates out; writes base-*.png)
#   python3 -I briefs/024/calm-ring/build.py       (draws the new marks; writes the PNGs; needs rsvg-convert and Pillow)
# Everything drawn here is mockup. The world is Sky's build 0.2.34 shot, retouched only where the old plates stood.
import math, os, subprocess
from PIL import Image, ImageFont

here = os.path.dirname(os.path.abspath(__file__))
FUT = "/System/Library/Fonts/Supplemental/Futura.ttc"
_f = {}
def font(size, bold):
    k = (round(size, 1), bold)
    if k not in _f: _f[k] = ImageFont.truetype(FUT, size, index=2 if bold else 0)
    return _f[k]
def tw(s, size, bold=False): return font(size, bold).getlength(s)

C = dict(teal="#0f4a45", teal2="#1b857b", cream="#f4ecd8", slate="#323f3b", slate2="#55675f", cell="#4fe3e8",
         brass="#c9a35a", ink="#12302d", coral="#e0604a", rim="#c9dbd3")
def esc(s): return s.replace("&", "&amp;").replace("<", "&lt;")

# ------------------------------------------------------------------ small drawing helpers
def pol(cx, cy, r, deg):          # degrees clockwise from the top
    t = math.radians(deg); return cx + r * math.sin(t), cy - r * math.cos(t)
def sector(cx, cy, r0, r1, a0, a1):
    x0, y0 = pol(cx, cy, r1, a0); x1, y1 = pol(cx, cy, r1, a1)
    x2, y2 = pol(cx, cy, r0, a1); x3, y3 = pol(cx, cy, r0, a0)
    big = 1 if (a1 - a0) % 360 > 180 else 0
    return f"M{x0:.1f} {y0:.1f}A{r1} {r1} 0 {big} 1 {x1:.1f} {y1:.1f}L{x2:.1f} {y2:.1f}A{r0} {r0} 0 {big} 0 {x3:.1f} {y3:.1f}Z"
def arc(cx, cy, r, a0, a1):
    x0, y0 = pol(cx, cy, r, a0); x1, y1 = pol(cx, cy, r, a1)
    return f"M{x0:.1f} {y0:.1f}A{r} {r} 0 {1 if (a1 - a0) % 360 > 180 else 0} 1 {x1:.1f} {y1:.1f}"
def png_icon(name, cx, cy, h, extra=""):
    im = Image.open(f"{here}/icons/{name}.png"); w = h * im.width / im.height
    return f'<image href="icons/{name}.png" x="{cx - w / 2:.1f}" y="{cy - h / 2:.1f}" width="{w:.1f}" height="{h:.1f}" {extra}/>', w
def dots(cx, cy, s, col=C["cream"]):
    return "".join(f'<circle cx="{cx + d * s:.1f}" cy="{cy}" r="{s * 0.34:.1f}" fill="{col}"/>' for d in (-1, 0, 1))
FOUND = '<path d="M10 2l8 14H2z" fill="none" stroke="{c}" stroke-width="2.2" stroke-linejoin="round"/><path d="M10 8v4" stroke="{c}" stroke-width="2.2" stroke-linecap="round"/>'
def found_glyph(x, y, s, col=C["cream"]):
    return f'<g transform="translate({x:.1f} {y - s / 2:.1f}) scale({s / 20:.3f})">{FOUND.format(c=col)}</g>'
def cursor(x, y, k=1.0):
    return (f'<g transform="translate({x} {y}) scale({k})"><path d="M0 0v26l7-6 5 11 5-2-5-11h9z" fill="#fff" stroke="{C["ink"]}" '
            f'stroke-width="1.8" stroke-linejoin="round"/></g>')

# ------------------------------------------------------------------ the one price plate
# parts: ("verb", text) ("t", text) ("trail", text, found?) ("chip", text)   ... one line, never wraps
def plate_layout(parts, sc, lead=True, more=False):
    pad, gap = 16 * sc, 11 * sc
    fb, fm = 25 * sc, 23 * sc
    w = pad + (46 * sc + gap if lead else 0)
    items = []
    for i, p in enumerate(parts):
        if i:                                  # separator dot between parts
            w += gap * 0.9; items.append(("sep", w, 0)); w += gap * 0.9
        k = p[0]
        if k == "verb": ww = tw(p[1], fb, True); items.append(("verb", w, ww, p[1]))
        elif k == "t": ww = tw(p[1], fm); items.append(("t", w, ww, p[1]))
        elif k == "trail":
            ww = tw(p[1], fm) + (tw(" if found", fm) if p[2] else 0); items.append(("trail", w, ww, p[1], p[2], 0))
        elif k == "chip":
            ww = tw(p[1], 21 * sc, True) + 28 * sc; items.append(("chip", w, ww, p[1]))
        w += ww
    if more: w += gap * 1.2 + 52 * sc; items.append(("more", w - 52 * sc, 52 * sc))
    return w + pad - (0 if not more else 0), items

def plate(x, y, sc, parts, lead_icon=None, lead_h=34, more=False, tail=None):
    """x,y = top-left. tail = ('up', tx) a pointer up to the thing, or ('left', ty) one to the left."""
    w, items = plate_layout(parts, sc, lead_icon is not None, more)
    h = 50 * sc; r = 13 * sc
    s = f'<rect x="{x + 3 * sc}" y="{y + 5 * sc}" width="{w}" height="{h}" rx="{r}" fill="#000" opacity="0.34" filter="url(#blur)"/>'
    pointer = ""
    if tail and tail[0] == "up":
        tx = tail[1]; pointer = f'<path d="M{tx - 9 * sc} {y + 2} L{tx} {y - 12 * sc} L{tx + 9 * sc} {y + 2}Z" fill="{C["teal"]}" stroke="{C["rim"]}" stroke-width="{2.6 * sc}" stroke-linejoin="round"/>'
    if tail and tail[0] == "left":
        ty = tail[1]; pointer = f'<path d="M{x + 2} {ty - 11 * sc} L{x - 16 * sc} {ty + 4 * sc} L{x + 2} {ty + 14 * sc}Z" fill="{C["teal"]}" stroke="{C["rim"]}" stroke-width="{2.6 * sc}" stroke-linejoin="round"/>'
    s += pointer
    s += f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{r}" fill="url(#plateg)" stroke="{C["rim"]}" stroke-width="{2.8 * sc}"/>'
    if tail:   # hide the seam where the pointer meets the plate
        if tail[0] == "up": s += f'<rect x="{tail[1] - 7 * sc}" y="{y - 1}" width="{14 * sc}" height="{5 * sc}" fill="{C["teal"]}"/>'
        else: s += f'<rect x="{x - 1}" y="{tail[1] - 8 * sc}" width="{5 * sc}" height="{20 * sc}" fill="{C["teal"]}"/>'
    s += f'<rect x="{x + 4 * sc}" y="{y + 4 * sc}" width="{w - 8 * sc}" height="{h - 8 * sc}" rx="{r - 3 * sc}" fill="none" stroke="{C["brass"]}" stroke-width="{1.2 * sc}" opacity="0.7"/>'
    cy = y + h / 2
    if lead_icon:
        ic, _ = png_icon(lead_icon, x + 16 * sc + 23 * sc, cy, lead_h * sc); s += ic
    by = cy + 8.6 * sc
    for it in items:
        k = it[0]; ix = x + it[1]
        if k == "sep": s += f'<circle cx="{ix:.1f}" cy="{cy:.1f}" r="{2.3 * sc:.1f}" fill="{C["cream"]}" opacity="0.55"/>'
        elif k == "verb": s += f'<text x="{ix:.1f}" y="{by:.1f}" font-family="Futura" font-weight="bold" font-size="{25 * sc:.1f}" fill="{C["cream"]}">{esc(it[3])}</text>'
        elif k == "t": s += f'<text x="{ix:.1f}" y="{by:.1f}" font-family="Futura" font-size="{23 * sc:.1f}" fill="{C["cream"]}">{esc(it[3])}</text>'
        elif k == "trail":
            s += f'<text x="{ix:.1f}" y="{by:.1f}" font-family="Futura" font-size="{23 * sc:.1f}" fill="{C["cream"]}">{esc(it[3])}</text>'
            if it[4]: s += f'<text x="{ix + tw(it[3] + " ", 23 * sc):.1f}" y="{by:.1f}" font-family="Futura" font-size="{23 * sc:.1f}" fill="{C["cream"]}" fill-opacity="0.78">if found</text>'
        elif k == "chip":
            cw = it[2]; chh = 33 * sc
            s += f'<rect x="{ix:.1f}" y="{cy - chh / 2:.1f}" width="{cw:.1f}" height="{chh:.1f}" rx="{chh / 2:.1f}" fill="{C["coral"]}"/>'
            s += f'<text x="{ix + cw / 2:.1f}" y="{by - 1.2 * sc:.1f}" text-anchor="middle" font-family="Futura" font-weight="bold" font-size="{21 * sc:.1f}" fill="#fff6ea">{esc(it[3])}</text>'
        elif k == "more":
            mw = it[2]; chh = 32 * sc
            s += f'<rect x="{ix:.1f}" y="{cy - chh / 2:.1f}" width="{mw:.1f}" height="{chh:.1f}" rx="{chh / 2:.1f}" fill="#0b2f2b" stroke="{C["rim"]}" stroke-width="{1.8 * sc}"/>'
            s += dots(ix + mw / 2, cy, 8.6 * sc)
    return s, w, h

# ------------------------------------------------------------------ the ring (moment 1)
CX, CY = 725, 340
R0, R1, G = 63, 160, 2.6
WEDGES = [  # name, centre angle, icon, harm
    ("take", 0, "take", False), ("watch", 90, "watch", False), ("knock", 180, "knock", True), ("more", 270, None, False)]
def ring(hover=None, opened_more=False, hover_extra=None):
    s = ""
    # gap lines first, in the cream the build uses
    for a in (45, 135, 225, 315):
        x0, y0 = pol(CX, CY, R0 - 2, a); x1, y1 = pol(CX, CY, R1 + 3, a)
        s += f'<line x1="{x0:.1f}" y1="{y0:.1f}" x2="{x1:.1f}" y2="{y1:.1f}" stroke="#e7dcc3" stroke-width="5"/>'
    # the wedges
    default = "take"
    body = ""; strokes = ""
    for name, ca, icon, harm in WEDGES:
        a0, a1 = ca - 45 + G, ca + 45 - G
        d = sector(CX, CY, R0, R1, a0, a1)
        lit = hover == name or (name == "more" and opened_more)
        fill = C["teal2"] if name == default else (C["slate2"] if lit else C["slate"])
        body += f'<path d="{d}" fill="{fill}" fill-opacity="1"/>'
        if lit and name != default: strokes += f'<path d="{d}" fill="none" stroke="{C["cream"]}" stroke-width="7" stroke-linejoin="round"/>'
        if harm: body += f'<path d="{arc(CX, CY, R1 - 6, a0 + 1.5, a1 - 1.5)}" fill="none" stroke="#f4ecd8" stroke-width="3.4" stroke-dasharray="11 8" opacity="0.9"/>'
    # the default tab: the wedge sticks out and carries a point. Shape, not colour, says "this one".
    tab_pts = f"{CX - 30},{CY - R1 + 7} {CX + 30},{CY - R1 + 7} {CX},{CY - R1 - 27}"
    dflt = sector(CX, CY, R0, R1, -45 + G, 45 - G)
    s += f'<polygon points="{tab_pts}" fill="{C["teal2"]}" stroke="{C["cream"]}" stroke-width="7" stroke-linejoin="round"/>'
    s += f'<path d="{dflt}" fill="none" stroke="{C["cream"]}" stroke-width="7" stroke-linejoin="round"/>'
    s += strokes + body
    s += f'<polygon points="{CX - 29},{CY - R1 + 9} {CX + 29},{CY - R1 + 9} {CX},{CY - R1 - 25}" fill="{C["teal2"]}"/>'
    s += f'<path d="{dflt}" fill="{C["teal2"]}" fill-opacity="1"/>'
    # icons
    for name, ca, icon, harm in WEDGES:
        ix, iy = pol(CX, CY, 112, ca)
        if name == "take": s += png_icon("take", ix, iy + 2, 46)[0]
        elif icon: s += png_icon(icon, ix, iy, 44 if icon != "watch" else 40)[0]
        else: s += dots(ix, iy, 15)
    # the centre: Sky's own pixels (the Enforcer in his ring)
    s += (f'<clipPath id="cc"><circle cx="{CX}" cy="{CY}" r="58"/></clipPath>'
          f'<image href="before-ring.png" x="0" y="0" width="2000" height="1070" clip-path="url(#cc)"/>'
          f'<circle cx="{CX}" cy="{CY}" r="58" fill="none" stroke="#e7dcc3" stroke-width="3"/>')
    # "..." opened: a second arc holds the harm ways
    if opened_more:
        d = sector(CX, CY, R1 + 10, R1 + 84, 236, 304)
        s += f'<path d="{d}" fill="{C["slate"]}" fill-opacity="0.97" stroke="#e7dcc3" stroke-width="4" stroke-linejoin="round"/>'
        s += f'<path d="{arc(CX, CY, R1 + 79, 238, 302)}" fill="none" stroke="#f4ecd8" stroke-width="3.4" stroke-dasharray="11 8" opacity="0.9"/>'
        for nm, a in (("dead", 252), ("gun", 288)):
            ix, iy = pol(CX, CY, R1 + 47, a)
            if hover_extra == nm:
                s += f'<circle cx="{ix:.1f}" cy="{iy:.1f}" r="31" fill="{C["slate2"]}" stroke="{C["cream"]}" stroke-width="4"/>'
            s += png_icon(nm, ix, iy, 40 if nm == "dead" else 30)[0]
    return s

def defs():
    return ('<defs><filter id="blur" x="-10%" y="-30%" width="120%" height="180%"><feGaussianBlur stdDeviation="4"/></filter>'
            f'<linearGradient id="plateg" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#1a5c54"/><stop offset="1" stop-color="{C["teal"]}"/></linearGradient></defs>')

def svg(w, h, inner): return f'<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" viewBox="0 0 {w} {h}" width="{w}" height="{h}">{defs()}{inner}</svg>'

PX, PY = 893, 229   # the plate hangs from the driver's pin, where the old Watch plate was
def moment1(state):
    s = '<image href="base-ring.png" x="0" y="0" width="2000" height="1070"/>'
    if state == "rest":
        s += ring()
        p, w, h = plate(PX, PY, 0.92, [("verb", "Take the driver"), ("t", "47 s"), ("trail", "+2 Trail", False)], "take", 34, tail=("up", 931))
    elif state == "knock":
        s += ring(hover="knock")
        p, w, h = plate(PX, PY, 0.92, [("verb", "Knock out"), ("t", "16 s"), ("trail", "+4 Trail", True), ("chip", "raises Alert")], "knock", 36, tail=("up", 931))
        s += p; s += cursor(748, 452, 1.0); return s
    elif state == "more":
        s += ring(opened_more=True, hover_extra="dead")
        p, w, h = plate(PX, PY, 0.92, [("verb", "Kill"), ("t", "13 s"), ("trail", "+6 Trail", True), ("chip", "raises Alert")], "dead", 36, tail=("up", 931))
        s += p; s += cursor(pol(CX, CY, R1 + 47, 252)[0] + 6, pol(CX, CY, R1 + 47, 252)[1] + 8, 1.0); return s
    s += p
    return s

# ------------------------------------------------------------------ moment 2 (1188 x 692, a retina grab: the HUD is drawn at 1.6 x)
def moment2():
    s = '<image href="base-hover.png" x="0" y="0" width="1188" height="692"/>'
    # the ground ring, as it was, on its own
    s += f'<ellipse cx="418" cy="487" rx="97" ry="53" fill="none" stroke="#000" stroke-opacity="0.25" stroke-width="9" transform="translate(0 3)" filter="url(#blur)"/>'
    s += f'<ellipse cx="418" cy="487" rx="97" ry="53" fill="none" stroke="#f6f1e6" stroke-width="6"/>'
    sc = 1.3
    p, w, h = plate(478, 352, sc, [("verb", "Loop camera"), ("t", "75 s"), ("trail", "+1 Trail", False)], "loop", 36, more=True, tail=("left", 410))
    s += p
    s += cursor(338, 392, 1.7)
    return s

def render(name, w, h, inner):
    p = f"{here}/{name}.svg"
    open(p, "w").write(svg(w, h, inner))
    subprocess.run(["rsvg-convert", p, "-o", f"{here}/{name}.png"], check=True)
    os.remove(p)

# ------------------------------------------------------------------ the sheets (PIL)
from PIL import ImageDraw
PAPER, INK, TEAL = (239, 231, 211), (18, 48, 45), (15, 74, 69)
def F(size, bold=False): return font(size, bold)
def wrap(d, text, f, width):
    out, line = [], ""
    for w_ in text.split():
        t = (line + " " + w_).strip()
        if d.textlength(t, font=f) <= width: line = t
        else: out.append(line); line = w_
    if line: out.append(line)
    return out
def tile(canvas, d, img, box, crop, label, caption, accent=TEAL):
    x, y, w = box
    im = Image.open(f"{here}/{img}").convert("RGB").crop(crop)
    h = round(im.height * w / im.width)
    im = im.resize((w, h), Image.LANCZOS)
    d.rectangle((x, y - 30, x + w, y - 4), fill=accent)
    d.text((x + 10, y - 28), label, font=F(17, True), fill=(244, 236, 216))
    canvas.paste(im, (x, y)); d.rectangle((x - 1, y - 1, x + w, y + h), outline=accent, width=2)
    yy = y + h + 8
    for ln in wrap(d, caption, F(18), w): d.text((x, yy), ln, font=F(18), fill=INK); yy += 24
    return h, yy
def sheet(name, title, sub):
    c = Image.new("RGB", (1920, 1080), PAPER); d = ImageDraw.Draw(c)
    d.text((20, 14), title, font=F(30, True), fill=INK); d.text((20, 52), sub, font=F(17), fill=(70, 84, 80))
    return c, d
def note_strip(d, y, items):
    d.rectangle((20, y, 1900, y + 1), fill=INK)
    x = 20
    for head, body in items:
        d.text((x, y + 12), head, font=F(18, True), fill=INK)
        yy = y + 38
        for ln in wrap(d, body, F(17), 590): d.text((x, yy), ln, font=F(17), fill=(50, 64, 60)); yy += 23
        x += 630

def full(src, out, scale, w, h):
    c = Image.new("RGB", (1920, 1080), (20, 35, 31))
    im = Image.open(f"{here}/{src}").convert("RGB").resize((round(w * scale), round(h * scale)), Image.LANCZOS)
    c.paste(im, ((1920 - im.width) // 2, (1080 - im.height) // 2)); c.save(f"{here}/{out}")

def sheets():
    # moment 1: 2 x 2 tiles of the same crop of Sky's 2000 x 1070 shot
    crop = (40, 100, 1500, 570)
    c, d = sheet("m1", "The ring on the driver", "Moment 1 of 2 · before is Sky's screenshot of build 0.2.34 · after is the calm ring (brief 024) · the History panel is open in Sky's shot only")
    W = 940; xs = (20, 960)
    h1, y1 = tile(c, d, "before-ring.png", (xs[0], 110, W), crop, "BEFORE · 0.2.34", "Five price plates round one ring, each with warning marks, a key badge and a name tag. Nothing says which wedge a click takes.", (150, 60, 50))
    tile(c, d, "work-m1-rest.png", (xs[1], 110, W), crop, "AFTER · the ring has just opened", "Four wedges, icons only. The wedge with the pointer tab is what E and a left click take. The one line says it in words.")
    h2, y2 = tile(c, d, "work-m1-knock.png", (xs[0], 560, W), crop, "AFTER · pointer on Knock out", "The same one line now carries that way's price. A harmful way has a dashed rim and one coral 'raises Alert'; nothing else.")
    tile(c, d, "work-m1-more.png", (xs[1], 560, W), crop, "AFTER · the dots opened, pointer on Kill", "Kill and Fire sit behind the dots. The line follows the pointer here too.")
    note_strip(d, 960, [("One line, one place", "It hangs from the target. It never moves between wedges, so the eye does not travel."),
                        ("The default is a shape", "A wedge that sticks out with a pointer tab, at the top. Colour is only a second cue."),
                        ("Three ways and the dots", "The ring shows the default, the next way and the lightest harm. The rest sit behind the dots.")])
    c.save(f"{here}/moment1-ring-before-after.png")
    # moment 2
    c, d = sheet("m2", "The hover on a cabinet", "Moment 2 of 2 · before is Sky's screenshot of build 0.2.34 (a high-resolution grab) · after is the calm marker")
    crop2 = (0, 0, 1188, 692)
    tile(c, d, "before-hover.png", (20, 110, 940), crop2, "BEFORE · 0.2.34", "A name tag, a ring with three small icons, and a plate with an icon and numbers but no verb.", (150, 60, 50))
    tile(c, d, "work-m2-after.png", (960, 110, 940), crop2, "AFTER · pointer on the cabinet", "One line that says the verb: 'Loop camera', the time, the Trail. A left click or E does exactly this. The dots cap means more ways sit under a right click.")
    note_strip(d, 770, [("No name tag", "The verb names the target ('Take the driver') or the target is plain from the model ('Loop camera' on the cabinet)."),
                        ("No icons on the ground ring", "The ring on the ground only marks the target. The icon lives in the line, once."),
                        ("Same line as the ring", "The line you read on a hover is the line the tab wedge shows when the ring opens.")])
    c.save(f"{here}/moment2-hover-before-after.png")
    for st, nm in (("rest", "moment1-ring-after.png"), ("knock", "moment1-ring-after-knock.png"), ("more", "moment1-ring-after-dots.png")):
        full(f"work-m1-{st}.png", nm, 0.96, 2000, 1070)
    full("work-m2-after.png", "moment2-hover-after.png", 1.5, 1188, 692)

if __name__ == "__main__":
    for st in ("rest", "knock", "more"): render(f"work-m1-{st}", 2000, 1070, moment1(st))
    render("work-m2-after", 1188, 692, moment2())
    sheets()
    print("ok")
