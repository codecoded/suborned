"""Brief 108 round 2 (Glyph): the briefing as three calm pages (the mission, the place, look out for), then Begin.
Design mocks only. python3 -I briefs/108/glyph/pages/src/build_pages.py  ->  pages/briefing-pages.html (+ ?only=<id> for one frame)."""
import base64, io
from pathlib import Path
from PIL import Image

ROOT = Path("/Users/sky/Projects/suborned")
KIT = ROOT / "unity/Assets/Suborned/UI/Resources/Kit"
HERE = Path(__file__).parent
OUT = HERE.parent


def b64(data, mime):
    return f"data:{mime};base64," + base64.b64encode(data).decode()


def uri(im, fmt="JPEG", q=84):
    buf = io.BytesIO()
    if fmt == "JPEG":
        im.convert("RGB").save(buf, "JPEG", quality=q)
        return b64(buf.getvalue(), "image/jpeg")
    im.save(buf, "PNG", optimize=True)
    return b64(buf.getvalue(), "image/png")


def face(path, box=None, size=320):
    im = Image.open(path).convert("RGB")
    if box:
        im = im.crop(box)
    return uri(im.resize((size, size), Image.LANCZOS))


def badge(name, size=128):
    return uri(Image.open(KIT / f"badges/{name}@2x.png").convert("RGBA").resize((size, size), Image.LANCZOS), "PNG")


def street(src, x0, w, patches=()):
    """A crop of a play capture, from x0, w wide; patches copy pavement over leftover HUD bits: (box, dy)."""
    im = Image.open(ROOT / src).convert("RGB")
    for (l, t, r, b), dy in patches:
        im.paste(im.crop((l, t - dy, r, b - dy)), (l, t))
    return uri(im.crop((x0, 0, x0 + w, 1080)), q=80)


FONTS = {w: b64((KIT / f"fonts/Jost-{w}.ttf").read_bytes(), "font/ttf") for w in ["Medium", "SemiBold", "Bold"]}
POPOVIC = face(KIT / "portraits/hero_councillor@2x.png", size=432)
INEZ = face(ROOT / "paid/063-portraits/concepts/inez_A.png", (180, 0, 800, 620), 160)
DRIVER = face(ROOT / "paid/063-portraits/concepts/driver_A.png", (120, 0, 820, 700), 160)
B = {n: badge(n) for n in ["signal_guard", "npc_watcher", "signal_rival", "npc_patrol"]}
HUD_BITS = [((1120, 1030, 1200, 1080), 60), ((1372, 648, 1452, 682), 46)]  # the cell's little faces at the bottom of the door capture
SQUARE = street("briefs/103/latch/start-overhead-day.png", 170, 1240, [((1420, 1030, 1480, 1080), 60)])
DOOR_P2 = street("briefs/103/latch/office-door-yaw210.png", 420, 1240, HUD_BITS)
DOOR_P3 = street("briefs/103/latch/office-door-yaw210.png", 700, 1000, HUD_BITS)

ICONS = ["car", "enter", "take", "units", "pause", "chev_up", "close_x", "nextevent", "p_alert"]
icon_css = "\n".join(f".i-{n}{{--u:url({b64((KIT / f'icons/singles/{n}@2x.png').read_bytes(), 'image/png')})}}" for n in ICONS)
SUN = '<svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="#17302f" stroke-width="2.2" stroke-linecap="round"><circle cx="12" cy="12" r="4.2" fill="#17302f"/><path d="M12 2.5v2.6M12 18.9v2.6M2.5 12h2.6M18.9 12h2.6M5.3 5.3l1.8 1.8M16.9 16.9l1.8 1.8M5.3 18.7l1.8-1.8M16.9 7.1l1.8-1.8"/></svg>'


def ic(name, size, color="#f7f4ec", extra=""):
    return f'<i class="ic i-{name}" style="width:{size}px;height:{size}px;color:{color};{extra}"></i>'


def chev(size, color, rot=90):
    return ic("chev_up", size, color, f"transform:rotate({rot}deg)")


def t(x, y, s, size, weight="Bold", color="#17302f", extra=""):
    return f'<div class="t" style="left:{x}px;top:{y}px;font-size:{size}px;font-family:Jost{weight};color:{color};{extra}">{s}</div>'


def chrome(page_left):
    out = [f'<div class="page" style="left:{page_left}px"></div>',
           '<div class="clock">' + ic("pause", 18) + "<span>0:00</span></div>",
           f'<div class="look">{SUN}<span>Clear</span>{chev(13, "#17302f", 180)}</div>']
    for word, icon, y, cur in [("Cell", "units", 330, False), ("Job", "take", 500, True), ("Menu", "pause", 930, False)]:
        d, fs = (108, 22) if cur else (84, 16)
        out.append(f'<div class="tab{" cur" if cur else ""}" style="left:{1830 - d / 2}px;top:{y - d / 2}px;width:{d}px;height:{d}px">{ic(icon, d * .46)}<span style="font-size:{fs}px">{word}</span></div>')
    out.append('<div class="dev"><span>Dial</span><b>DEV</b>' + chev(12, "#b0cdc7", 0) + "</div>")
    return "\n".join(out)


def nav(left, right, page, last=False, back=True, skip=False):
    """Back · 1 · 2 · 3 · Next (or Begin on the last page). Every piece is a click; the arrow keys are an extra."""
    out = []
    y = 948
    if back:
        out.append(f'<div class="back" style="left:{left}px;top:{y + 14}px">{chev(16, "#1f6c67", -90)}<span>Back</span></div>')
    if skip:
        out.append(f'<div class="skip" style="left:{left}px;top:{y + 18}px"><span>Skip to Begin</span>{chev(14, "#4e625f")}</div>')
    w = 3 * 38 + 2 * 26
    cx = (left + 112 + right - (270 if last else 190)) / 2
    dots = "".join(
        (f'<span class="bar"></span>' if i else "") + f'<span class="dot{" on" if i + 1 == page else ""}{" done" if i + 1 < page else ""}">{i + 1}</span>'
        for i in range(3))
    out.append(f'<div class="dots" style="left:{cx - w / 2}px;top:{y + 12}px">{dots}</div>')
    if last:
        out.append(f'<div class="begin" style="left:{right - 270}px;top:{y - 6}px;width:270px;height:92px;border-radius:46px"><span style="font-size:46px">Begin</span>{chev(20, "#f7f4ec")}</div>')
    else:
        out.append(f'<div class="next" style="left:{right - 190}px;top:{y}px;width:190px;height:76px"><span>Next</span>{chev(17, "#f7f4ec")}</div>')
    return "\n".join(out)


def kicker(x, y, size=22):
    return t(x, y, "THE HANDOVER", size, "SemiBold", "#1f6c67", "letter-spacing:.26em")


def pin(x, y, word, primary=True, img=None, tail="down"):
    """A world label: the kit's dark plate, a chrome tail at (x, y) pointing at the thing."""
    cls = "pin" + ("" if primary else " quiet") + f" tail-{tail}"
    pic = f'<img src="{img}">' if img else ""
    return f'<div class="{cls}" style="left:{x}px;top:{y}px"><div class="plate">{pic}<span>{word}</span></div></div>'


# ------------------------------------------------------------------ page 1 · the mission
def p1(returning=False):
    p = [f'<img class="street" src="{SQUARE}">', chrome(740)]
    p.append(f'<div class="hero" style="left:{880 - 210}px;top:{390 - 210}px;width:420px;height:420px"><img src="{POPOVIC}"></div>')
    p.append(kicker(1120, 200))
    p.append(t(1120, 232, "Take Popovic alive", 70))
    story = ["Councillor Popovic keeps a file on us.",
             "Tonight, as his shift ends, he carries it out of the Records Office.",
             "We take him alive, put him in our car and drive away."]
    p.append('<div class="story" style="left:1122px;top:360px;width:620px">' + "".join(f"<p>{s}</p>" for s in story) + "</div>")
    p.append(nav(930, 1740, 1, back=False, skip=returning))
    return "\n".join(p)


# ------------------------------------------------------------------ page 2 · the place
PLACES = [("car", None, "Our car", "We start here, in the square.", ""),
          (None, INEZ, "Inez's café", "Inez waits on the tables.", ""),
          ("enter", None, "The front door", "Into the Records Office.", "Way in · before 5:30"),
          (None, DRIVER, "The pickup", "His driver waits with his car.", "Way in · from 5:50")]


def p2(stop):
    left = 1100
    src = SQUARE if stop == 1 else DOOR_P2
    p = [f'<img class="street" src="{src}">', chrome(left)]
    x = 1210
    p.append(kicker(x, 110, 20))
    p.append(t(x, 140, "Where it happens", 54))
    rows = []
    for i, (icon, img, name, line, way) in enumerate(PLACES):
        on = i == stop
        disc = f'<span class="pdisc">{ic(icon, 34)}</span>' if icon else f'<span class="pface"><img src="{img}"></span>'
        chip = f'<span class="waychip">{ic("enter" if i == 2 else "car", 18)}{way}{chev(12, "#f7f4ec", 180)}</span>' if way else ""
        rows.append(f'<div class="prow{" on" if on else ""}">{disc}<div><b>{name}</b><em>{line}</em>{chip}</div></div>')
    p.append(f'<div class="places" style="left:{x - 18}px;top:262px;width:{1745 - x + 18}px">{"".join(rows)}</div>')
    if stop == 1:   # the opening view: the café, our car
        p.append(pin(905, 214, "Inez's café", tail="up"))
        p.append(pin(728, 880, "Our car", primary=False))
    else:           # the Office's west face: the public door
        p.append(pin(538, 398, "Front door"))
    p.append(nav(x - 4, 1745, 2))
    return "\n".join(p)


# ------------------------------------------------------------------ page 3 · look out for
DANGERS = [("signal_guard", "The guard", "He stands on the front door.", ""),
           ("npc_watcher", "Cameras", "Someone watches them live.", ""),
           ("signal_rival", "Another crew", "They want Popovic too.", "2:35"),
           ("npc_patrol", "The patrol robot", "It walks the yard behind the Office.", ""),
           ("police", "The police", "Noise brings them.", "")]


def p3():
    p = [f'<img class="street" src="{DOOR_P3}">', chrome(740)]
    x = 930
    p.append(kicker(x, 110))
    p.append(t(x, 142, "Look out for", 66))
    rows = []
    for i, (bd, name, line, when) in enumerate(DANGERS):
        tm = f'<span class="when">{ic("nextevent", 16)}{when}</span>' if when else ""
        pic = f'<span class="ddisc">{ic("p_alert", 38)}</span>' if bd == "police" else f'<img src="{B[bd]}">'
        rows.append(f'<div class="drow{" on" if i == 0 else ""}">{pic}<div><b>{name}{tm}</b><em>{line}</em></div></div>')
    p.append(f'<div class="dangers" style="left:{x - 18}px;top:262px;width:{1745 - x + 18}px">{"".join(rows)}</div>')
    p.append(pin(246, 395, "The guard", img=B["signal_guard"]))
    p.append(pin(668, 372, "Camera", primary=False, img=B["npc_watcher"]))
    p.append(nav(x - 4, 1745, 3, last=True))
    return "\n".join(p)


FRAMES = [
    ("p1", "1 · The mission", p1(), "A little about the job, as a story: who Popovic is, why we want him, what we're doing. No Back on the first page."),
    ("p2a", "2 · The place (first stop)", p2(1), "Arriving on page 2, the page slides right to give the street room and the camera visits each place in turn; the row being shown lights up. Here: Inez's café, with our car below."),
    ("p2b", "2 · The place (the front door)", p2(2), "The front door is a way in, so its row carries a chip with its time; a click on the chip opens its steps (as round 1's A2). A click on any row sends the camera there and stops the tour."),
    ("p3", "3 · Look out for, then Begin", p3(), "Each danger wears the marker the player will see over it in the street. A hover (here: the guard) shows it on the street; one off-screen turns the camera to it. Begin replaces Next."),
    ("p1r", "1 · The mission, for a returning player", p1(True), "After the first play (a restart or a second run), page 1 adds a quiet 'Skip to Begin' where Back would sit. A first-time player always reads the three pages."),
]

css = (HERE / "base.css").read_text() + (HERE / "pages.css").read_text()
fonts = "\n".join(f"@font-face{{font-family:Jost{w};src:url({u}) format('truetype')}}" for w, u in FONTS.items())
sections = "\n".join(
    f'<section class="dir" data-k="{k}"><h2><b>{name.split(" · ")[0]}</b> {name.split(" · ", 1)[1]}</h2><p class="cap">{cap}</p>'
    f'<div class="fwrap"><div class="frame">{body}</div></div></section>' for k, name, body, cap in FRAMES)
note = (HERE / "note.html").read_text()
html = f"""<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Briefing Pages</title><style>{fonts}\n{icon_css}\n{css}</style></head><body>
<header class="top"><h1>The briefing as three pages</h1><p>Brief 108, round 2 · Glyph · 2026-11-11 · design only, 1920×1080 frames · words are placeholders for Fable</p></header>
{note}
{sections}
<script>
const q=new URLSearchParams(location.search), only=q.get('only');
if(only){{document.body.classList.add('raw');document.querySelectorAll('.dir').forEach(s=>{{if(s.dataset.k!==only)s.remove()}});document.querySelectorAll('header.top,.note').forEach(e=>e.remove());}}
function fit(){{document.querySelectorAll('.fwrap').forEach(w=>w.style.setProperty('--s',only?1:w.clientWidth/1920))}}
addEventListener('resize',fit);fit();
</script></body></html>"""
(OUT / "briefing-pages.html").write_text(html)
print("wrote", OUT / "briefing-pages.html", len(html) // 1024, "KB")
