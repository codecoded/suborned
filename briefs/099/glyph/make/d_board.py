"""The board for Sky's phone: 1000 px wide, as look4's boards. Each concept: its win large, its fail and one number opened side by side,
then the opened explainer cropped to read. Glyph, brief 099. Mock only."""
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))  # python3 -I drops the script folder
from d_base import *

W = 1000; BAND = (12, 52, 52); G = OUT
CONCEPTS = [
    ('A', 'front', 'A · The front page', 'Everything at a glance: your way in, its start-page rows answered with the time each came true; five numbers with their reasons; what the city does now; the run along the foot.',
     (870, 638, 1760, 958), 'Trail opened as the book’s fold: what left it, biggest first'),
    ('B', 'clock', 'B · The clock', 'The run to scale. Your acts above the line, the city below; each number steps up under the act that caused it. A fail shows the window that lost it.',
     (860, 210, 1760, 760), 'Trail opened on the clock: each rise named where it happened'),
    ('C', 'people', 'C · The people', 'The cell as four cards (numbers, health, who saw them, their moment), then who the run touched and what each is to us now. Numbers in one band.',
     (990, 170, 1760, 520), 'Trail opened on the people: each agent’s share on their card'),
]


def text(d, x, y, t, size, col=CREAM, face_='Jost-Bold', w=None, lead=1.25):
    f = font(face_, size)
    if not w: d.text((x, y), t, font=f, fill=col); return y + size * lead
    line = ''
    for word in t.split():
        trial = (line + ' ' + word).strip()
        if f.getlength(trial) > w and line: d.text((x, y), line, font=f, fill=col); y += size * lead; line = word
        else: line = trial
    d.text((x, y), line, font=f, fill=col); return y + size * lead


def band(t, sub=None, h=None):
    im = Image.new('RGB', (W, 400), BAND); d = ImageDraw.Draw(im); y = text(d, 26, 20, t, 40)
    if sub: y = text(d, 26, y + 2, sub, 22, col=DIM[:3], face_='Jost-Medium', w=W - 52, lead=1.3)
    return im.crop((0, 0, W, int(y + 18)))


def fit_w(p, w): im = Image.open(p).convert('RGB'); return im.resize((w, round(im.height * w / im.width)), Image.LANCZOS)


def board():
    parts = [band('The debrief, told like the book', 'Three ideas on the light page. Every number is from a real run played headless in the sim (a quiet win, a fail, a loud win). Each concept shows a win, a fail, and one number opened.')]
    for k, nm, title, sub, crop, cap in CONCEPTS:
        parts.append(band(title, sub))
        parts.append(fit_w(G + f'{k}-{nm}-win.png', W))
        row = Image.new('RGB', (W, 0), (247, 244, 236)); hw = (W - 12) // 2
        a, b = fit_w(G + f'{k}-{nm}-fail.png', hw), fit_w(G + f'{k}-{nm}-detail.png', hw)
        row = Image.new('RGB', (W, a.height + 44), (247, 244, 236)); row.paste(a, (0, 44)); row.paste(b, (hw + 12, 44)); d = ImageDraw.Draw(row)
        text(d, 10, 9, 'A fail: their lookout', 22, col=INK[:3]); text(d, hw + 22, 9, 'Opened: Trail on the loud win', 22, col=INK[:3])
        parts.append(row)
        c = Image.open(G + f'{k}-{nm}-detail.png').convert('RGB').crop(crop); c = c.resize((W, round(c.height * W / c.width)), Image.LANCZOS)
        cap_im = Image.new('RGB', (W, 44), (247, 244, 236)); text(ImageDraw.Draw(cap_im), 10, 9, cap, 22, col=SOFT[:3], face_='Jost-SemiBold')
        parts.append(cap_im); parts.append(c)
    parts.append(band('My pick: A, the front page', 'It is the start page answered: the rows the player read at 0:00 come back ticked with the time each came true, so a win shows the plan working and a fail points at the row that lost it. All five numbers sit in view, each with its reason, and most of its pieces are the start page’s own. Borrow C’s cards for the Cell tab, and B’s clock for the Journal.'))
    H = sum(p.height for p in parts) + 6 * len(parts); out = Image.new('RGB', (W, H), BAND); y = 0
    for p in parts: out.paste(p, (0, y)); y += p.height + 6
    out.save(G + 'board.png'); return out.size


if __name__ == '__main__':
    print(board())
