"""The four pages. Every line's source is in ../notes.md; words are draft for Fable. Glyph, brief 085."""
from page import *
ROOT2 = '/Users/sky/Projects/suborned/'
OUT = ROOT2 + 'briefs/085/glyph/book/'
CELL = {'scout': ('Scout', 'Listens to Inez and the driver. Gives the rivals a false tip.'), 'fixer': ('Fixer', "Turns Inez or the driver, and calls Inez when it's time."),
        'tech': ('Tech', 'Loops the door camera, moves the patrol, drives our car round.'), 'enforcer': ('Enforcer', 'Takes Vale inside or at his car. Only he can carry him.')}


def big_face(P, role, x, y, d):
    src = Image.open(ROOT2 + f'briefs/033/portraits/{role}_states.png').convert('RGBA').crop((96, 86, 644, 634)).resize((round(d * P.s),) * 2, Image.LANCZOS)
    m = Image.new('L', (src.width * 3, src.height * 3), 0); ImageDraw.Draw(m).ellipse((0, 0, m.width - 1, m.height - 1), fill=255)
    src.putalpha(m.resize(src.size, Image.LANCZOS)); P.paste(src, x, y); P.disc(x - 1, y - 1, d + 2, fill=None, ring=INK_DEEP, rw=1); P.disc(x, y, d, fill=None, ring=CHROME, rw=3)


def verb(P, x, y, w, icon, word, does, price=None, now=None, shut=False):
    """A way: the ring's icon and word, the action's own line, its time, and who it can go on right now."""
    P.disc_icon(icon, x, y + 4, 58, shut=shut); P.put(x + 76, y + 18, word, 30, col=SOFT if shut else INK, face='Jost-Bold')
    tw = P.tw(word, 30, 'Jost-Bold'); cx = x + 76 + tw + 14
    if price: cx += P.plate(cx, y + 2, price, 'p_time', h=32, size=16) + 8
    P.wrap(x + 76, y + 54, does, w - 76 - 290, size=24)
    if now:
        nx = x + w - 270
        for kind, payload in now:
            if kind == 'lock': P.plate(nx, y + 30, payload, 'lock', h=36, size=17, col=DIM)
            else:
                P.face(payload[0], nx, y + 8, 66, rw=2); P.put(nx + 33, y + 92, payload[1], 18, anchor='mm', face='Jost-SemiBold')
                if len(payload) > 2: P.disc(nx + 44, y + 4, 26, fill=INK_DEEP, ring=CHROME, rw=1); P.icon('lock', nx + 50, y + 10, 14)
                nx += 90


def cell_page():
    P = Page(); P.chrome('Your cell', '0:05', '$100,000')
    y = 116
    for r, (n, line) in CELL.items():
        on = r == 'scout'
        if on: P.rrect(46, y - 6, 420, 208, 6, fill=PRINT, line=(0xd8, 0xd3, 0xc4, 255), lw=1)
        P.face(r, 58, y + 14, 92, ring=TURQ if on else CHROME, rw=3 if on else 2)
        P.head(166, y + 26, n, size=19)
        P.wrap(166, y + 62, line, 286, size=23)
        y += 222
    x = 500
    big_face(P, 'scout', x + 12, 122, 270)
    P.put(x + 147, 428, 'Scout', 52, face='Jost-Bold', anchor='mm')
    bx = x + 22
    bx += P.plate(bx, 470, 'Free', 's_ready', h=36, size=17) + 10
    P.plate(bx, 470, 'Sidearm', 'fire', h=36, size=17)
    P.wrap(x + 12, 546, 'Sees furthest: 30 m. The others see less.', 280, size=23)
    X = x + 322; W = 1864 - X
    P.head(X, 124, 'What the Scout can do', size=19); P.head(X + W - 270, 124, 'On who, now', size=19)
    P.hair(X, 1864, 146)
    verb(P, X, 164, W, 'watch', 'Watch', 'Learn where they go and when. No Trail, but it takes time.', '20 s', [('f', ('waitress', 'Inez')), ('f', ('guard', 'Guard'))])
    P.hair(X, 1864, 300)
    verb(P, X, 318, W, 'listen', 'Listen', 'Learn what they need and who they are. Stand close; it takes a while.', '30 s', [('f', ('driver', 'Driver')), ('f', ('waitress', 'Inez', 1)), ('f', ('guard', 'Guard', 1))])
    P.hair(X, 1864, 454)
    verb(P, X, 472, W, 'mislead', 'Tip', 'Send the rivals the wrong way for a while. They may come back.', '30 s', [('lock', 'No rivals yet')], shut=True)
    P.hair(X, 1864, 608)
    P.head(X, 650, 'Everyone in the cell can', size=19)
    cx = X
    for ic, word in (('s_moving', 'Walk'), ('run', 'Run'), ('watch', 'Watch'), ('fire', 'Fire'), ('s_carrying', 'Carry'), ('car', 'Board'), ('car', 'Drive')):
        P.disc_icon(ic, cx + 6, 684, 62); P.put(cx + 37, 770, word, 23, anchor='mm', face='Jost-SemiBold'); cx += 128
    return P


# ---------------------------------------------------------------- people
GROUPS_OPEN = [('People', [('waitress', 'Inez', 't--'), ('guard', 'Guard', '---'), ('driver', 'Driver', 't--'), ('councillor', 'Vale', '')])]
GROUPS_TURN = [('Turned', [('waitress', 'Inez', 'lll')]), ('Not yet', [('guard', 'Okafor', '--l'), ('driver', 'Driver', 't--'), ('councillor', 'Vale', '')])]


def pips(P, x, y, s):
    for k, c in enumerate(s):
        dx = x + k * 13
        if c == 'l': P.disc(dx, y, 8, fill=INK, ring=None)
        elif c == 't': P.disc(dx, y, 8, fill=None, ring=INK, rw=1.6); P.disc(dx + 2, y + 2, 4, fill=INK, ring=None)
        else: P.disc(dx, y, 8, fill=None, ring=SOFT, rw=1)


def index(P, groups, selected):
    y = 112
    for g, rows in groups:
        P.head(56, y + 8, g, size=18); y += 24
        for r, n, s in rows:
            if r == selected: P.rrect(46, y, 326, 80, 6, fill=PRINT, line=(0xd8, 0xd3, 0xc4, 255), lw=1)
            P.face(r, 56, y + 9, 62, ring=TURQ if r == selected else CHROME, rw=2.5 if r == selected else 1.8)
            P.put(132, y + 28, n, 25, face='Jost-SemiBold')
            if s: pips(P, 132, y + 48, s)
            else: P.put(132, y + 56, 'Out 4:50', 18, col=SOFT)
            y += 86
        y += 8
    # how it holds for hundreds: an illustration, its counts made up
    y0 = 690
    P.hair(56, 372, y0 - 12); P.head(56, y0 + 8, 'Later, with hundreds', size=16)
    P.put(56, y0 + 38, 'Grouped by what they give', 20, col=SOFT)
    yy = y0 + 66
    for ic, word, n in (('cash', 'Cash', 42), ('fire', 'Arms', 17), ('listen', 'Tips', 96), ('enter', 'Access', 58)):
        P.icon(ic, 56, yy, 20, col=INK); P.put(82, yy + 10, word, 20, face='Jost-SemiBold'); P.put(372, yy + 10, str(n), 20, anchor='rm', col=SOFT)
        for i in range(max(2, round(n / 96 * 11))):
            P.disc(160 + i * 15, yy + 4, 12, fill=(0x7f, 0x96, 0x90, 255) if i % 4 else TEAL, ring=None)
        yy += 36
    P.put(56, yy + 10, 'Numbers made up', 18, col=SOFT)


def person_head(P, role, name, job, line, box, x=412, w=450):
    h = round(w * (box[3] - box[1]) / (box[2] - box[0]))
    P.picture(P.still(box), x, 118, w, h)
    y = 118 + h + 48
    P.put(x, y, name, 50, face='Jost-Bold'); P.put(x, y + 44, job, 25, col=SOFT)
    return P.wrap(x, y + 92, line, w, size=25)


def facts(P, x, y, rows):
    for i, (ic, st, word, note) in enumerate(rows):
        P.rrect(x, y, 40, 40, 20 if i == 0 else 8, fill=BAND)
        P.rim(x + 4, y + 4, 32, 32, 16 if i == 0 else 6, st)
        if ic: P.icon(ic, x + 9, y + 9, 22)
        else: P.text(x + 20, y + 20, '?', 23, anchor='mm', alpha=.8)
        P.put(x + 54, y + 20, word, 28, col=SOFT if st == 'unk' else INK, face='Jost-SemiBold')
        if note: P.put(x + 54 + P.tw(word, 28, 'Jost-SemiBold') + 14, y + 21, note, 23, col=SOFT)
        y += 52
    return y


def meter(P, x, y, word, val, top, step):
    P.put(x, y, word, 24, face='Jost-SemiBold')
    for k in range(top + 1):
        cx = x + 96 + k * 21
        if k < val: P.disc(cx, y - 8, 16, fill=TEAL, ring=None)
        else: P.disc(cx, y - 8, 16, fill=None, ring=SOFT, rw=1.2)
    P.put(x + 200, y, step, 22, col=SOFT)


def guard_page():
    P = Page(); P.chrome('The people', '0:05', '$100,000'); index(P, GROUPS_OPEN, 'guard')
    person_head(P, 'guard', 'The guard', 'door guard', "Takes his coffee at Inez's café before he goes off shift.", (1234, 240, 1555, 466))
    X = 900
    P.head(X, 124, 'What we know', size=18); P.hair(X, 1400, 146)
    y = facts(P, X, 164, [(None, 'unk', 'Needs ?', None), (None, 'unk', 'Nature ?', None), (None, 'unk', 'Secret ?', None)])
    P.put(X, y + 14, 'Told: close to Inez', 25, col=SOFT)
    y += 72
    P.head(X, y + 12, 'What we could try', size=18); P.hair(X, 1400, y + 34); y += 52
    cx = X; P.face('scout', cx, y + 2, 34, rw=1.4); cx += 42
    cx += pill(P, cx, y, ('scout', 'watch', 'Watch', ''), h=38, size=18) + 6
    pill(P, cx, y, ('scout', 'listen', 'Listen', 'shut'), h=38, size=18); y += 48
    cx = X; P.face('fixer', cx, y + 2, 34, rw=1.4); cx += 42; cx += pill(P, cx, y, ('fixer', 'ask', 'Ask', ''), h=38, size=18) + 16
    P.face('enforcer', cx, y + 2, 34, rw=1.4); cx += 42; pill(P, cx, y, ('enforcer', 'way_knock', '', 'harm'), h=38, size=18)
    X2 = 1440
    for i, (h, line) in enumerate((('How we turned him', 'Not turned'), ("What he'll do", 'Nothing yet'), ('How far we trust him', '?'), ('What he knows about us', 'Nothing'), ('Between us', 'Nothing yet'))):
        yy = 124 + i * 124
        P.head(X2, yy, h, size=18); P.hair(X2, 1864, yy + 22); P.put(X2, yy + 56, line, 28, col=SOFT, face='Jost-SemiBold')
    return P


def inez_page():
    P = Page(); P.chrome('The people', '2:10', '$60,000', trail='2'); index(P, GROUPS_TURN, 'waitress')
    person_head(P, 'waitress', 'Inez', 'café waitress', 'Works the door and the tables. The guard stops by for coffee.', (830, 240, 1150, 466))
    X = 900
    P.head(X, 124, 'What we know', size=18); P.hair(X, 1400, 146)
    y = facts(P, X, 164, [('debt', 'learned', 'Bureau loan', 'cleared'), ('fearful', 'learned', 'Fearful', None), ('lovers', 'learned', 'Loves Okafor', None)])
    y += 18; P.head(X, y + 12, 'How we turned her', size=18); P.hair(X, 1400, y + 34)
    y += 52; pw = pill(P, X, y, ('fixer', 'help', 'Help', ''), h=38, size=18); P.put(X + pw + 14, y + 19, 'twice · $40,000', 26, face='Jost-SemiBold')
    y += 76; P.head(X, y + 12, "What she'll do", size=18); P.hair(X, 1400, y + 34)
    y += 52; P.face('fixer', X, y + 2, 34, rw=1.4); pw = pill(P, X + 42, y, ('fixer', 'call', 'Call', 'new'), h=38, size=18); P.plate(X + 42 + pw + 8, y, 'Door 2:54', 'p_time', h=38, size=18)
    P.wrap(X, y + 66, 'Draws the guard off the door. Timing matters.', 490, size=25)
    X2 = 1440
    P.head(X2, 124, 'How far we trust her', size=18); P.hair(X2, 1864, 146)
    meter(P, X2, 178, 'Loyal', 2, 2, 'trusting'); meter(P, X2, 216, 'Afraid', 1, 2, 'uneasy'); meter(P, X2, 254, 'Bitter', 0, 1, 'compliant'); meter(P, X2, 292, 'Brave', 0, 2, 'informant')
    P.plate(X2, 318, 'Questioned, she keeps quiet', 'talker', h=38, size=18)
    P.head(X2, 396, 'What she knows about us', size=18); P.hair(X2, 1864, 418)
    P.face('fixer', X2, 432, 40, rw=1.4); P.put(X2 + 54, 452, "The Fixer's face", 25, face='Jost-SemiBold')
    P.icon('p_time', X2 + 7, 487, 26, col=INK); P.put(X2 + 54, 500, 'Where and when', 25, face='Jost-SemiBold')
    P.head(X2, 562, 'Between us', size=18); P.hair(X2, 1864, 584)
    yy = 600
    for t, who, ic, word in (('0:29', 'scout', 'watch', 'Watched her'), ('0:59', 'scout', 'listen', 'Listened in'), ('1:35', 'fixer', 'help', 'Helped: not ready'), ('2:10', 'fixer', 'help', 'Helped: turned')):
        P.put(X2, yy + 20, t, 23, col=SOFT, face='Jost-SemiBold'); P.face(who, X2 + 62, yy, 40, rw=1.4); P.icon(ic, X2 + 112, yy + 8, 24, col=INK); P.put(X2 + 146, yy + 20, word, 25)
        yy += 56
    return P


# ---------------------------------------------------------------- the job
def job_page():
    P = Page(); P.chrome('The job', '2:40', '$60,000', trail='3')
    P.picture(P.still((60, 228, 452, 550)), 62, 120, 200, 164)
    P.wrap(286, 136, 'Take Councillor Vale alive to the getaway car.', 330, size=29, face='Jost-Bold')
    P.wrap(286, 226, 'Comes out at 4:50 and leaves by car.', 330, size=23, col=SOFT)
    P.head(62, 322, 'What ends it', size=18); P.hair(62, 620, 344)
    y = 356
    for ic, t in (('body_dead', 'Vale dies'), ('car', 'Vale leaves in his car'), ('signal_rival', 'Rivals reach Vale first'), ('eye_open', 'Guard or camera sees us go in'), ('s_downed', 'Enforcer down, Vale not in the car'), ('car', 'Getaway car wrecked'), ('lock', 'The district seals')):
        if ic.startswith('signal'): P.art('badges/' + ic, 62, y, 30)
        else: P.icon(ic, 62, y + 2, 28, col=INK)
        P.put(104, y + 15, t, 25); y += 44
    X = 660
    P.head(X, 124, 'Two ways in', size=18); P.hair(X, 1380, 146)
    def way(y, name, when, steps, line):
        P.put(X, y + 18, name, 32, face='Jost-Bold'); P.plate(X + P.tw(name, 32, 'Jost-Bold') + 16, y + 2, when, 'p_time', h=32, size=16)
        b = P.wrap(X, y + 62, line, 710, size=23); cx = X; yy = b - 2
        for role, word, mark in steps:
            P.face(role, cx, yy, 48, rw=1.8)
            if mark: P.art(f'badges/{mark}', cx + 32, yy - 3, 20)
            P.put(cx + 56, yy + 24, word, 21, face='Jost-SemiBold'); cx += 56 + P.tw(word, 21, 'Jost-SemiBold') + 20
        return yy + 66
    y = way(158, 'Front door', 'Before 4:50', [('waitress', 'Turned', 'mark_ok'), ('guard', 'Called away', 'mark_ok'), ('tech', 'Camera 1:05', 'mark_ok'), ('enforcer', 'Inside', None)], 'Inez and the door guard are close. Turn her to call him off and loop his camera.')
    y = way(y + 6, 'Pickup', 'From 5:10', [('driver', 'Told: cash', None)], 'Vale rides from the official bay; his driver is short of cash. Turn him before 4:40 and he brings Vale to the service bay.')
    P.put(X, y + 8, "The ways we've seen. There may be others.", 23, col=SOFT)
    X3 = 1420
    P.head(X3, 124, 'So far', size=18); P.hair(X3, 1864, 146)
    yy = 158
    for t, who, ic, word in (('0:29', 'scout', 'watch', 'Watched Inez'), ('0:59', 'scout', 'listen', 'Heard: Bureau loan'), ('1:35', 'fixer', 'help', 'Helped Inez'), ('1:45', 'tech', 'loop', 'Door camera looped'), ('2:10', 'fixer', 'turn_person', 'Inez turned'), ('2:10', 'fixer', 'call', 'Called Inez'), ('2:12', 'enforcer', 'enter', 'Went in'), ('2:35', None, 'car', 'A rival car turned in')):
        P.put(X3, yy + 16, t, 21, col=SOFT, face='Jost-SemiBold')
        if who: P.face(who, X3 + 56, yy, 32, rw=1.3)
        P.icon(ic, X3 + 96, yy + 5, 22, col=INK); P.put(X3 + 126, yy + 16, word, 23); yy += 46
    P.put(X3, yy + 14, 'All of it: the journal', 23, col=TEAL, face='Jost-SemiBold')
    yc = 852
    P.head(62, 756, 'The clock', size=18)
    stations = [('2:35', 'A rival car turns in', True), ('4:30', 'Handover chime', False), ('4:40', 'Last chance to turn the driver', False), ('4:50', 'Vale leaves the Office', False), ('5:10', "Vale's car leaves", False)]
    pos = [70, 450, 800, 1220, 1590]; nowx = 70 + 28
    P.line([(70, yc), (pos[-1], yc)], col=INK, lw=2); P.line([(70, yc), (nowx, yc)], col=TEAL, lw=5)
    for (t, lab, past), cx in zip(stations, pos):
        P.disc(cx - 8, yc - 8, 16, fill=TEAL if past else PAPER, ring=TEAL if past else INK, rw=2)
        P.put(cx - 8, yc + 36, t, 30, col=SOFT if past else INK); P.put(cx - 8, yc + 74, lab, 23, col=SOFT if past else INK)
        if past: P.art('badges/mark_ok', cx - 28, yc - 28, 20)
    P.disc(nowx - 5, yc - 5, 10, fill=TEAL, ring=None); P.plate(nowx - 6, yc - 44, 'Now 2:40', None, h=30, size=16)
    return P
