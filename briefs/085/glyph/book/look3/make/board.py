"""Extension sheets and the phone boards. Every frame on a sheet is drawn from data by the same code. Glyph, brief 085."""
import sys
sys.path.insert(0, '.')
from options import *
W_ = 1000; B = (12, 52, 52)
def pic(n, w=W_): im = Image.open(OUT + n + '.png').convert('RGB'); return im.resize((w, round(im.height * w / im.width)), Image.LANCZOS)
def crop(n, box, w=W_): im = Image.open(OUT + n + '.png').convert('RGB').crop(box); return im.resize((w, round(im.height * w / im.width)), Image.LANCZOS)
def bar(t, subs=(), tag=None):
    f1, f2 = font('Jost-Bold', 50), font('Jost-Bold', 34); im = Image.new('RGB', (W_, 84 + 48 * len(subs) + 12), B); d = ImageDraw.Draw(im); d.text((30, 16), t, font=f1, fill=CREAM)
    for i, s in enumerate(subs): d.text((30, 82 + i * 48), s, font=f2, fill=DIM)
    if tag:
        tw = f2.getlength(tag); d.rounded_rectangle((W_ - tw - 66, 18, W_ - 24, 78), 14, outline=TURQ, width=4); d.text((W_ - tw - 45, 26), tag, font=f2, fill=TURQ)
    return im
def sheet(o, nm):
    ks = ['inez', 'facts12', 'unknown', 'agent', 'job', 'list8', 'list200', 'cash']; w, h = 620, 349; S_ = Image.new('RGB', (1920, 1080), B); d = ImageDraw.Draw(S_); f = font('Jost-Bold', 24)
    for i, k in enumerate(ks):
        x, y = 15 + (i % 3) * (w + 15), 4 + (i // 3) * (h + 11 + 22); d.text((x, y - 2), PAGES[k][0], font=f, fill=CREAM); S_.paste(pic(f'{o}-{nm}-{k}', w), (x, y + 26))
    x, y = 15 + 2 * (w + 15), 4 + 2 * (h + 33); f2 = font('Jost-Bold', 32)
    for i, t in enumerate(['Same code each time.', 'Only the data changes.', 'Nothing here is placed by hand.']): d.text((x + 10, y + 90 + i * 50), t, font=f2, fill=DIM)
    S_.save(OUT + f'{o}-{nm}-sheet.png')
def grid(names, cols=2):
    w = (W_ - 8 * (cols - 1)) // cols; h = round(w * 9 / 16); rows = -(-len(names) // cols); im = Image.new('RGB', (W_, rows * (h + 8)), B)
    for i, n in enumerate(names): im.paste(pic(n, w), ((i % cols) * (w + 8), (i // cols) * (h + 8)))
    return im
def stack(parts, name):
    H = sum(p.height for p in parts); b = Image.new('RGB', (W_, H), B); y = 0
    for p in parts: b.paste(p, (0, y)); y += p.height
    b.save(OUT + name, optimize=True); print(name, b.size)
sheet('A', 'pole'); sheet('C', 'hud')
stack([bar('The book, round four', ['Flat colour, real type, real facts.', 'One template. Words are draft.']), bar('A · The pole', ['Sections are round signs on a pole.'], tag='Pick this'), pic('A-pole-inez'), crop('A-pole-inez', (640, 40, 1460, 700)), pic('A-pole-job'), bar('One choice for Sky', ['With a starburst, it says retro future louder,', 'but breaks the one-gesture limit.']), pic('A-pole-inez-with-starburst')], 'board.png')
stack([bar('A holds anything, no redesign', ['Same code, new data only.']), grid(['A-pole-unknown', 'A-pole-agent', 'A-pole-facts12', 'A-pole-cash', 'A-pole-list8', 'A-pole-list200'])], 'board-2.png')
stack([bar('C · The HUD, grown', ['Our enamel and signs. Consistent,', 'but closer to a dark app.'], tag='Second'), pic('C-hud-inez'), pic('C-hud-job'), grid(['C-hud-unknown', 'C-hud-list200'])], 'board-3.png')
stack([bar('B · The deck: rejected', ['A white card with side tabs: a web modal.']), pic('B-deck-inez')], 'board-4.png')
