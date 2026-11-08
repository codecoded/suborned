"""Brief 101, round 3 (Glyph): the moments. Each is a real capture (0.2.286 or 0.2.287) with its old-kit pieces lifted and the new ones drawn
in the built street kit (r3_base). Per piece: its screen share on today's HUD layer and drawn new (alpha >= 24 at 1080, as tools/hud-budget reads
a .hud.png); per frame: the whole HUD's share before and after. Writes briefs/101/glyph/r3/<key>.png, the 2x copies in out/scratch/glyph/101r3/
and make/coverage.json. Glyph. Mock only."""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from r3_base import *

PIECES = []  # (frame, piece, old px, new px)
LAYERS3 = {}  # key: (the new pieces' layer, what stays of today's HUD alpha, the boxes lifted): r3_budget writes the HUD layers from them


def render(key, pieces, extra=(), keep=()):
    """pieces: (name, box to lift, draw(P)); extra: boxes lifted with nothing drawn (the docked card). Returns (before, after) shares."""
    boxes = [b for _, b, _ in pieces if b] + list(extra); frame_im, rest = lift(key, boxes, keep); L = layer()
    for name, box, draw in pieces:
        Q = layer(); draw(Q); L.img.alpha_composite(Q.img); PIECES.append((key, name, old_px(key, box) if box else 0, cover_px(Q)))
    for b in extra: PIECES.append((key, 'docked card (removed)', old_px(key, b), 0))
    _, al = real(key); before = float((al >= 24).mean()); after = frame_cover(rest, L)
    im = compose(frame_im, L); os.makedirs(OUT3, exist_ok=True); os.makedirs(SCR3, exist_ok=True); LAYERS3[key] = (L, rest, boxes)
    im.resize((1920, 1080), Image.LANCZOS).convert('RGB').save(OUT3 + f'{key}.png'); im.convert('RGB').save(SCR3 + f'{key}-2x.png')
    print(f'{key:7} before {before * 100:.2f} %  after {after * 100:.2f} %'); return before, after


def quiet():
    # the Office door's step markers (one box each, so the dotted reach ring round the knocked-out guard stays): the camera turn, the body, the
    # unknown watcher at the door, the next step (the Scout watches)
    return render('quiet', [
        ('attention callout', (918, 180, 1002, 286), lambda P: callout(P, 960, 220, 62, None, agent='enforcer', ticks=1, to=282, red_eye=True)),
        ('pop: Camera on', (898, 337, 1023, 381), lambda P: pop(P, 960, 359, 'eye_open', 'Camera on')),
        ('attention clock', (408, 70, 512, 108), lambda P: attention(P, 426, 91, .55, 'noticed')),
        ('door: camera turn', (469, 209, 497, 238), lambda P: tab3(P, 472, 212, 23, 'turn_view')),
        ('door: body', (497, 193, 525, 221), lambda P: tab3(P, 498, 194, 26, 'body_out')),
        ('door: watcher', (494, 221, 528, 255), lambda P: tab3(P, 498, 225, 26, 'eye_unk')),
        ('door: next step', (527, 222, 586, 254), lambda P: capsule(P, 530, 226, 'watch', '', face_='scout', live=True))])


def window():
    return render('window', [
        ('done chip', (864, 217, 1057, 286), lambda P: done_chip(P, 960, 226, 'fixer', 'ok', [('eye_shut', 'Guard away')])),
        ('window marker', (919, 286, 1001, 332), lambda P: window_mark(P, 937, 309, 174, 180)),
        ('door marker', (943, 483, 977, 517), lambda P: tab3(P, 947, 487, 26, 'eye_open', col=AMBER_S)),
        ('hint disc', (943, 1029, 977, 1063), lambda P: tab3(P, 947, 1033, 26, 'p_unknown'))])


def lozenge(P, right, top, title, line, count, roundel):
    """The news lozenge as 0.2.287 draws it (HudToolkit.Crucial: 50 px, the book's deep teal, a cream inline 3.5 px in, the roundel 58 px over its left
    end, the title Jost Bold 17 in page cream, the why line Jost Medium 13 in the enamel line, "1 of n ›" under its right end), with `roundel` cut
    from the capture. Returns its left end."""
    w = 40 + max(P.tw(title, 17, 'Jost-Bold'), P.tw(line, 13, 'Jost-Medium')) + 24; x = right - w; h = 50
    shadow(P, x, top, w, h, 25, dy=2.5, a=84, blur=.1); P.rrect(x, top, w, h, 25, fill=DEEP); P.rrect(x + 3.5, top + 3.5, w - 7, h - 7, 21.5, line=PAGE, lw=1.4)
    P.paste(roundel.resize((58 * P.s, 58 * P.s), Image.LANCZOS), x - 26, top - 4)
    P.text(x + 40, top + 17, title, 17, col=PAGE, face='Jost-Bold'); P.text(x + 40, top + 35, line, 13, col=LINE3, face='Jost-Medium')
    cw = P.tw(count, 11.5, 'Jost-SemiBold') + 16; P.rrect(right - cw, top + h - 4, cw, 18, 9, fill=BODY3); P.text(right - cw + 8, top + h + 5, count, 11.5, col=PAGE, face='Jost-SemiBold')
    return x


def card():
    im, _ = real('card'); rd = im.crop((1444, 999, 1502, 1057)).convert('RGBA'); m = Image.new('L', (58, 58), 0); ImageDraw.Draw(m).ellipse((0, 0, 57, 57), fill=255); rd.putalpha(m)
    line = "a camera saw the Fixer stay too long · 2:46"  # illustrative: 36 characters of why, Fable's words to come
    def news(P): lozenge(P, 1830, 1003, 'Robots coming', line, '1 of 4 ›', rd)
    def inez(P):  # the card went to the Book: Inez's key face top right carries the new dot while it reads; a click opens her page
        P.disc(1784 + 17, 126 - 2, 11, fill=TURQ, ring=INK, rw=1.6)
    return render('card', [
        ('news lozenge (40-char why)', (1416, 995, 1836, 1072), news),
        ("Inez's new dot (top right)", None, inez),
        ('car marker', (902, 571, 936, 605), lambda P: tab3(P, 906, 575, 26, 'car')),
        ('hint disc', (943, 1029, 977, 1063), lambda P: tab3(P, 947, 1033, 26, 'p_unknown'))],
        extra=[(1476, 748, 1912, 954)], keep=[(1838, 990, 1912, 1062)])


def conv():
    learned = ["Debt: Bureau loan, and she's behind.", 'Secret: seeing Arthur, the door guard.']; opens = ['Help: clear her Bureau loan.', 'Pressure: threaten to expose the affair.']
    def slip_(P):
        Q = layer(); x0, y0, x1, y1 = slip(Q, 0, 0, 'waitress', 'Inez', learned, opens); w, h = x1 - x0, y1 - y0
        slip(P, 960 - w / 2 + 14, 404 - 26 - (y1 - 0), 'waitress', 'Inez', learned, opens, to=(960, 404))
    return render('conv', [('person card (on Inez)', (744, 226, 1178, 421), slip_),
                           ('(kept) Inez pip', None, lambda P: P.art('badges/npc_waitress', 947, 408, 26)),
                           ('hint disc', (943, 1029, 977, 1063), lambda P: tab3(P, 947, 1033, 26, 'p_unknown'))], keep=[(960, 421, 14), (991, 421, 14)])




def ring():
    def price(P, x, y, verb, marks, face_=None):
        x += label(P, x, y, None, verb) + 8
        if face_: face3(P, face_, x, y - 9, 18, rw=1.6); x += 24
        for icon, fig, soft in marks:
            if icon == 'mark_no': P.disc(x - 1.2, y - 9.2, 18.4, fill=PAGE, ring=None); badge(P, 'mark_no', x, y - 8, 16); x += 22; continue
            kicon(P, icon, x, y - 8, 16, fill=SOFT if soft else INK, kw=1.8); x += 19
            if fig: x += tag(P, x, y, fig, 15) + 9
            else: x += 4
    return render('ring', [('ring: Watch plate and name', (916, 368, 1006, 403), lambda P: (price(P, 924, 380, 'Watch', [], face_='scout'), tag(P, 960, 394, 'Inez', 13, fill=SOFT, anchor='mm'))),
                           ('ring: Ask plate', (1055, 476, 1228, 505), lambda P: price(P, 1062, 490, 'Ask', [('p_trail', '+1', 0), ('p_alert', '1', 0), ('eye_shut', '', 1), ('mark_no', '', 0)])),
                           ('ring: Help plate', (868, 583, 1052, 613), lambda P: price(P, 874, 598, 'Help', [('p_cash', '0', 0), ('p_trail', '+1', 0), ('eye_shut', '', 1), ('mark_no', '', 0)])),
                           ('ring: cream rim (for chrome)', None, lambda P: (arc(P, 959.5, 490, 88.6, 0, 360, (0x0b, 0x16, 0x15, 150), 4.4), arc(P, 959.5, 490, 88.6, 0, 360, PAGE, 2.2)))])


def main():
    res = {k: f() for k, f in (('quiet', quiet), ('window', window), ('card', card), ('conv', conv), ('ring', ring))}
    rows = [dict(frame=f, piece=p, old_px=o, new_px=n, old_pct=round(o / 20736, 3), new_pct=round(n / 20736, 3)) for f, p, o, n in PIECES if not p.startswith('(kept)')]  # % of 1920 x 1080
    doc = dict(method='HUD layer alone, alpha >= 24 at 1920 x 1080 (tools/hud-budget); old: the capture .hud.png inside the piece box (+4 px); new: the piece drawn alone',
               frames={k: dict(build=SHOTS[k][1], moment=SHOTS[k][2], before_pct=round(b * 100, 2), after_pct=round(a * 100, 2)) for k, (b, a) in res.items()}, pieces=rows)
    json.dump(doc, open(OUT3 + 'make/coverage.json', 'w'), indent=1)
    for r in rows: print(f"{r['frame']:7} {r['piece']:30} old {r['old_px']:6} px ({r['old_pct']:.3f} %)  new {r['new_px']:6} px ({r['new_pct']:.3f} %)")


if __name__ == '__main__':
    main()
