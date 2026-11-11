"""Brief 108 (Glyph): three briefing directions as 1920x1080 frames in one self-contained page."""
import base64, io, sys
from pathlib import Path
from PIL import Image

ROOT = Path("/Users/sky/Projects/suborned")
KIT = ROOT / "unity/Assets/Suborned/UI/Resources/Kit"
OUT = ROOT / "briefs/108/glyph"
OUT.mkdir(parents=True, exist_ok=True)


def b64(data, mime):
    return f"data:{mime};base64," + base64.b64encode(data).decode()


def img_uri(im, fmt="JPEG", q=86):
    buf = io.BytesIO()
    if fmt == "JPEG":
        im.convert("RGB").save(buf, "JPEG", quality=q)
        return b64(buf.getvalue(), "image/jpeg")
    im.save(buf, "PNG", optimize=True)
    return b64(buf.getvalue(), "image/png")


def face(path, box=None, size=360):
    im = Image.open(path).convert("RGB")
    if box:
        im = im.crop(box)
    im = im.resize((size, size), Image.LANCZOS)
    return img_uri(im)


FONTS = {w: b64((KIT / f"fonts/Jost-{w}.ttf").read_bytes(), "font/ttf") for w in ["Medium", "SemiBold", "Bold"]}
POPOVIC = face(KIT / "portraits/hero_councillor@2x.png", size=432)
INEZ = face(ROOT / "paid/063-portraits/concepts/inez_A.png", (180, 0, 800, 620))
DRIVER = face(ROOT / "paid/063-portraits/concepts/driver_A.png", (120, 0, 820, 700))
CELL = {r: face(KIT / f"portraits/portrait_{r}_normal@2x.png", size=132) for r in ["scout", "fixer", "tech", "enforcer"]}
street = Image.open(ROOT / "briefs/101/glyph/A-page-rest.png").convert("RGB").crop((540, 0, 1660, 1080))
STREET = img_uri(street, q=80)
GUARD = img_uri(Image.open(KIT / "badges/signal_guard@2x.png").convert("RGBA"), "PNG")

ICONS = ["enter", "car", "take", "units", "pause", "chev_up", "protection", "eye_open", "unknown", "close_x", "papers", "run", "lock"]
icon_css = "\n".join(
    f".i-{n}{{--u:url({b64((KIT / f'icons/singles/{n}@2x.png').read_bytes(), 'image/png')})}}" for n in ICONS
)

SUN = '<svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="#17302f" stroke-width="2.2" stroke-linecap="round"><circle cx="12" cy="12" r="4.2" fill="#17302f"/><path d="M12 2.5v2.6M12 18.9v2.6M2.5 12h2.6M18.9 12h2.6M5.3 5.3l1.8 1.8M16.9 16.9l1.8 1.8M5.3 18.7l1.8-1.8M16.9 7.1l1.8-1.8"/></svg>'


def ic(name, size, color="#f7f4ec", extra=""):
    return f'<i class="ic i-{name}" style="width:{size}px;height:{size}px;color:{color};{extra}"></i>'


def chev(size, color, rot=90):
    return ic("chev_up", size, color, f"transform:rotate({rot}deg)")


def chrome(page_left, tabs="std"):
    """Clock, look pill, the side tabs (only the live ones at the start), the folded dev dial."""
    rail_x = 1830 if tabs == "std" else 1852
    sm = tabs != "std"
    out = [f'<div class="page" style="left:{page_left}px"></div>']
    out.append('<div class="clock">' + ic("pause", 18) + '<span>0:00</span></div>')
    out.append(f'<div class="look">{SUN}<span>Clear</span>{chev(13, "#17302f", 180)}</div>')
    for word, icon, y, cur in [("Cell", "units", 330, False), ("Job", "take", 500, True), ("Menu", "pause", 930, False)]:
        d = (92 if cur else 74) if sm else (108 if cur else 84)
        fs = (18 if cur else 14) if sm else (22 if cur else 16)
        out.append(
            f'<div class="tab{" cur" if cur else ""}" style="left:{rail_x - d / 2}px;top:{y - d / 2}px;width:{d}px;height:{d}px">'
            f'{ic(icon, d * .46)}<span style="font-size:{fs}px">{word}</span></div>'
        )
    out.append('<div class="dev"><span>Dial</span><b>DEV</b>' + chev(12, "#b0cdc7", 0) + "</div>")
    return "\n".join(out)


def portrait(cx, cy, d, ring=6):
    return f'<div class="hero" style="left:{cx - d / 2}px;top:{cy - d / 2}px;width:{d}px;height:{d}px;border-width:{ring}px"><img src="{POPOVIC}"></div>'


def cameo(src, x, y, d, ring=3, cls=""):
    return f'<div class="cameo {cls}" style="left:{x}px;top:{y}px;width:{d}px;height:{d}px;border-width:{ring}px"><img src="{src}"></div>'


def text(x, y, s, size, weight="Bold", color="#17302f", extra=""):
    return f'<div class="t" style="left:{x}px;top:{y}px;font-size:{size}px;font-family:Jost{weight};color:{color};{extra}">{s}</div>'


def begin(x, y, w, h, word="Begin", sub=""):
    inner = f'<span style="font-size:{h * .5:.0f}px">{word}</span>{chev(h * .22, "#f7f4ec")}'
    if sub:
        inner = f'<div class="bcol"><span style="font-size:{h * .42:.0f}px">{word}</span><em>{sub}</em></div>{chev(h * .22, "#f7f4ec")}'
    return f'<div class="begin" style="left:{x}px;top:{y}px;width:{w}px;height:{h}px;border-radius:{h / 2}px">{inner}</div>'


def link(x, y, word, icon="unknown", size=24):
    return (f'<div class="link" style="left:{x}px;top:{y}px;font-size:{size}px">'
            f'<span class="ring">{ic(icon, size * .78, "#1f6c67")}</span>{word}{chev(size * .6, "#1f6c67")}</div>')


def head(x, y, title_size, sub_size, kicker_size=22):
    return (text(x, y, "THE HANDOVER", kicker_size, "SemiBold", "#1f6c67", "letter-spacing:.26em")
            + text(x, y + kicker_size * 1.35, "Take Popovic alive", title_size)
            + text(x, y + kicker_size * 1.35 + title_size * 1.12, "to the getaway car", sub_size, "Medium", "#4e625f"))


def fact(x, y, icon, word, size=26, color="#17302f"):
    return (f'<div class="fact" style="left:{x}px;top:{y}px;font-size:{size}px;color:{color}">'
            f'<span class="disc" style="width:{size * 1.55}px;height:{size * 1.55}px">{ic(icon, size * .95)}</span>{word}</div>')


# ---------------------------------------------------------------- A · One choice (the poster)
def way_card(x, y, w, h, icon, word, when, who_img, who, state=""):
    return (f'<div class="way {state}" style="left:{x}px;top:{y}px;width:{w}px;height:{h}px">'
            f'<span class="wicon">{ic(icon, 44)}</span>'
            f'<div class="t" style="left:104px;top:26px;font-size:50px;font-family:JostBold;color:#f7f4ec">{word}</div>'
            f'<div class="t" style="left:106px;top:92px;font-size:30px;font-family:JostMedium;color:#b0cdc7">{when}</div>'
            f'<div class="cameo" style="left:30px;top:{h - 84}px;width:56px;height:56px;border-width:3px"><img src="{who_img}"></div>'
            f'<div class="t" style="left:104px;top:{h - 74}px;font-size:28px;font-family:JostSemiBold;color:#f7f4ec">{who}</div>'
            f'<span class="more">{chev(18, "#f7f4ec", 0)}</span></div>')


def frame_a(opened=False):
    p = [chrome(740)]
    p.append(portrait(880, 300, 380))
    p.append(head(1100, 122, 66, 34))
    p.append(fact(1102, 348, "car", "Rivals arrive at 2:35", 26))
    p.append(link(1440, 352, "What can go wrong", "unknown", 24))
    if not opened:
        p.append(way_card(930, 500, 390, 240, "enter", "Front door", "Before 5:30", INEZ, "Inez"))
        p.append(way_card(1345, 500, 390, 240, "car", "Pickup", "From 5:50", DRIVER, "The driver"))
    else:
        p.append(way_card(930, 500, 390, 240, "enter", "Front door", "Before 5:30", INEZ, "Inez", "open"))
        p.append(way_card(1345, 500, 390, 240, "car", "Pickup", "From 5:50", DRIVER, "The driver", "dim"))
        steps = ["The guard is off the door", "The entrance camera can't see the door", "The Enforcer is inside before 5:30",
                 "Popovic reaches our car", "Go, and the job's done"]
        rows = "".join(f'<div class="step"><i class="tick"></i><span>{s}</span></div>' for s in steps)
        p.append(f'<div class="steps" style="left:930px;top:768px;width:500px">'
                 f'<div class="tail" style="left:170px"></div>'
                 f'<div class="who">{cameo_inline(INEZ)}Inez<span class="gdisc"><img src="{GUARD}"></span>The guard'
                 f'<span class="x">{ic("close_x", 16, "#4e625f")}</span></div>{rows}</div>')
    p.append(begin(1445, 922, 290, 96))
    return "\n".join(p)


def cameo_inline(src):
    return f'<span class="cin"><img src="{src}"></span>'


# ---------------------------------------------------------------- B · The clock (lanes on one timeline)
def frame_b():
    p = [chrome(1000, tabs="slim")]
    p.append(portrait(1050, 210, 260, 5))
    p.append(head(1210, 104, 56, 30, 20))
    p.append(link(1212, 300, "What can go wrong", "unknown", 22))
    x0, x1, end = 1330, 1745, 490.0
    X = lambda s: x0 + (x1 - x0) * s / end
    # ruler
    p.append(f'<div class="rule" style="left:{x0}px;top:470px;width:{x1 - x0}px"></div>')
    for s, lab in [(0, "0:00"), (490, "8:10")]:
        p.append(f'<div class="tickm" style="left:{X(s) - 1}px;top:462px"></div>')
        p.append(text(X(s) - 30, 484, lab, 22, "Medium", "#4e625f", "width:60px;text-align:center"))
    # rivals: a dashed line through both lanes
    rx = X(155)
    p.append(f'<div class="dash" style="left:{rx - 1}px;top:440px;height:330px"></div>')
    p.append(f'<div class="rmark" style="left:{rx - 22}px;top:396px">{ic("car", 26)}</div>')
    p.append(text(rx + 30, 400, "Rivals 2:35", 24, "SemiBold"))
    # lanes
    lanes = [("enter", "Front door", INEZ, "Inez", 0, 330, "5:30", "end", 540),
             ("car", "Pickup", DRIVER, "The driver", 350, 490, "5:50", "start", 680)]
    for icon, word, img, who, a, b, t, at, y in lanes:
        p.append(f'<div class="lane" style="left:1040px;top:{y - 14}px;width:{x1 - 1040 + 10}px;height:118px"></div>')
        p.append(f'<span class="lhead" style="left:1056px;top:{y + 6}px">{ic(icon, 30)}</span>')
        p.append(text(1116, y + 2, word, 34))
        p.append(cameo(img, 1116, y + 52, 38, 2))
        p.append(text(1164, y + 56, who, 22, "SemiBold", "#4e625f"))
        p.append(f'<div class="bar" style="left:{X(a)}px;top:{y + 18}px;width:{X(b) - X(a)}px"></div>')
        tx = X(b) - 70 if at == "end" else X(a) + 8
        p.append(text(tx, y + 62, t, 28, "Bold", "#17302f", "width:62px;text-align:" + ("right" if at == "end" else "left")))
    # cell, begin
    for i, r in enumerate(["scout", "fixer", "tech", "enforcer"]):
        p.append(cameo(CELL[r], 1092 + i * 58, 942, 50, 3))
    p.append(link(1328, 950, "Your cell", "units", 22))
    p.append(begin(1495, 925, 250, 86))
    return "\n".join(p)


# ---------------------------------------------------------------- C · Pick your way (two doors)
def tile(x, y, w, h, icon, word, img, who, when, picked):
    cls = "tile picked" if picked else "tile"
    ink, sub = ("#f7f4ec", "#b0cdc7") if picked else ("#17302f", "#4e625f")
    return (f'<div class="{cls}" style="left:{x}px;top:{y}px;width:{w}px;height:{h}px">'
            f'<div class="cameo big" style="left:{w / 2 - 92}px;top:34px;width:184px;height:184px;border-width:5px"><img src="{img}"></div>'
            f'<span class="tdisc" style="left:{w / 2 + 52}px;top:162px">{ic(icon, 30)}</span>'
            f'<div class="t c" style="left:0;top:240px;width:{w}px;font-size:48px;font-family:JostBold;color:{ink}">{word}</div>'
            f'<div class="t c" style="left:0;top:304px;width:{w}px;font-size:28px;font-family:JostSemiBold;color:{sub}">{who}</div>'
            f'<div class="when" style="left:{w / 2 - 95}px;top:356px;width:190px">{when}</div>'
            + (f'<span class="pick">{ic("chev_up", 0)}</span>' if False else "")
            + "</div>")


def frame_c():
    p = [chrome(740)]
    p.append(portrait(830, 190, 250, 5))
    p.append(head(1000, 92, 62, 30))
    p.append(link(1002, 262, "What can go wrong", "unknown", 22))
    p.append(text(930, 360, "How do we get to him?", 32, "SemiBold"))
    p.append(tile(930, 420, 390, 430, "enter", "Front door", INEZ, "Inez", "Before 5:30", True))
    p.append(tile(1345, 420, 390, 430, "car", "Pickup", DRIVER, "The driver", "From 5:50", False))
    p.append(fact(932, 878, "car", "Rivals arrive at 2:35", 24))
    p.append(fact(1240, 878, "eye_open", "Streets are calm", 24))
    p.append(begin(930, 950, 805, 96, "Begin", "by the front door"))
    return "\n".join(p)


FRAMES = [
    ("A", "One choice", frame_a(), "The poster: Popovic, the job, two big way cards, one time each, Begin."),
    ("A2", "One choice · a way opened", frame_a(True), "A, after clicking Front door: its steps open under it (the faces still turn the street); a second click or ✕ folds it."),
    ("B", "The clock", frame_b(), "The plan as two lanes on one 0:00–8:10 line: when each way is open, when the rivals come."),
    ("C", "Pick your way", frame_c(), "Two doors, faces big: the player picks a way before Begin (or doesn't); Begin says which."),
]

CSS = Path(__file__).with_name("mocks.css").read_text()
fonts_css = "\n".join(f"@font-face{{font-family:Jost{w};src:url({u}) format('truetype')}}" for w, u in FONTS.items())

frames_html = "\n".join(
    f'<section class="dir" id="f{k}" data-k="{k}"><h2><b>{k}</b> {name}</h2><p class="cap">{cap}</p>'
    f'<div class="fwrap"><div class="frame"><img class="street" src="{STREET}">{body}</div></div></section>'
    for k, name, body, cap in FRAMES
)
today = Image.open(ROOT / "briefs/107/glyph/job-look-closed.jpg").convert("RGB").resize((960, 540), Image.LANCZOS)
NOTE = Path(__file__).with_name("note.html").read_text().replace("{TODAY}", img_uri(today, q=82))
html = f"""<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Briefing Directions</title><style>{fonts_css}\n{icon_css}\n{CSS}</style></head><body>
<header class="top"><h1>The Job page · three directions</h1><p>Brief 108 · Glyph · 2026-11-11 · design only, 1920×1080 frames · words are placeholders for Fable</p></header>
{NOTE}
{frames_html}
<script>
const q=new URLSearchParams(location.search), only=q.get('only');
if(only){{document.body.classList.add('raw');document.querySelectorAll('.dir').forEach(s=>{{if(s.dataset.k!==only)s.remove()}});document.querySelectorAll('header.top,.note').forEach(e=>e.remove());}}
function fit(){{document.querySelectorAll('.fwrap').forEach(w=>{{w.style.setProperty('--s',only?1:w.clientWidth/1920)}})}}
addEventListener('resize',fit);fit();
</script></body></html>"""
(OUT / "briefing-directions.html").write_text(html)
print("wrote", OUT / "briefing-directions.html", len(html) // 1024, "KB")
