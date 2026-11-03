"""What the game knows at each moment, as data the options draw, and the pieces they share. Every word's source is in notes.md; words are draft for Fable."""
from kit import *

JOB = {'waitress': 'café waitress', 'guard': 'door guard', 'driver': "Vale's driver", 'councillor': 'councillor'}
OPENING = {
 'waitress': dict(name='Inez', pips='t--', facts=[('debt', 'told', 'Told: debt'), (None, 'unk', 'Nature ?'), (None, 'unk', 'Secret ?')],
                  ways=[('scout', 'watch', 'Watch', ''), ('scout', 'listen', 'Listen', 'shut'), ('fixer', 'ask', 'Ask', '')], next='watch', learn='listen'),
 'guard': dict(name='Guard', pips='---', facts=[(None, 'unk', 'Needs ?'), (None, 'unk', 'Nature ?'), (None, 'unk', 'Secret ?')],
               ways=[('scout', 'watch', 'Watch', ''), ('scout', 'listen', 'Listen', 'shut'), ('enforcer', 'way_knock', '', 'harm')], next='watch', learn='listen'),
 'driver': dict(name='Driver', pips='t--', facts=[('cash', 'told', 'Told: cash'), (None, 'unk', 'Nature ?'), (None, 'unk', 'Secret ?')],
                ways=[('scout', 'listen', 'Listen', ''), ('fixer', 'pay', 'Pay', ''), ('fixer', 'ask', 'Ask', '')], next='listen', learn='listen'),
 'councillor': dict(name='Vale', pips='', facts=[('s_inside', 'learned', 'Inside'), ('p_time', 'learned', 'Out 4:50'), ('take', 'learned', 'Take alive')], ways=[], next=None, time='4:50'),
}
LEARNED = dict(OPENING)
LEARNED['waitress'] = dict(name='Inez', pips='lll', new=True, mood='mood_uneasy', facts=[('debt', 'learned', 'Bureau loan'), ('fearful', 'learned', 'Fearful'), ('lovers', 'learned', 'Loves guard')],
                           ways=[('fixer', 'help', 'Help', 'new', 'mark_half'), ('fixer', 'pressure', 'Pressure', 'new', 'mark_ok'), ('fixer', 'pay', 'Pay', '', 'mark_ok')], next='help')
LEARNED['guard'] = dict(name='Okafor', pips='--l', facts=[(None, 'unk', 'Needs ?'), (None, 'unk', 'Nature ?'), ('lovers', 'learned', 'Loves Inez')],
                        ways=[('scout', 'watch', 'Watch', ''), ('scout', 'listen', 'Listen', 'shut'), ('enforcer', 'way_knock', '', 'harm')], next='watch', learn='listen')
ORDER = ['waitress', 'guard', 'driver', 'councillor']


def slot(L, kind, x, y, d, icon, state):
    """A fact's slot, as Intel draws it: need a circle, nature a rounded square, secret a tab; the rim says how it is known."""
    w = d + 6 if kind == 2 else d; r = d / 2 if kind == 0 else 6 if kind == 1 else 5
    L.rrect(x, y, w, d, r, fill=(0x0a, 0x26, 0x24, 255)); L.rim(x, y, w, d, r, state)
    if icon: L.icon(icon, x + (w - d * .68) / 2, y + d * .16, d * .68, alpha=.85 if state == 'told' else 1)
    else: L.text(x + w / 2, y + d / 2, '?', d * .58, anchor='mm', alpha=.75)
    return w


def pill(L, x, y, way, h=30, size=14.5):
    """A way: plate_dark, the verb's glyph, its word, its fit mark (kit badges, as the ring's). Shut ways struck through, a lock for the glyph;
    violent ones by shape (a dashed inner edge, no word), as the ring does."""
    who, icon, word, st = way[:4]; mark = way[4] if len(way) > 4 else None
    w = 9 + 16 + (3 + L.tw(word, size) if word else 0) + (3 + 16 if mark else 0) + 9
    L.nine('plate_dark', x, y, w, h, alpha=.6 if st == 'shut' else 1)
    cx = x + 9
    L.icon('lock' if st == 'shut' else icon, cx, y + (h - 15) / 2, 15, alpha=.85 if st == 'shut' else 1); cx += 18
    if word:
        wtx = L.tw(word, size); L.text(cx, y + h / 2 - .5, word, size, col=DIM if st == 'shut' else CREAM)
        if st == 'shut': L.line([(cx - 1, y + h / 2), (cx + wtx + 1, y + h / 2)], col=DIM, lw=1.3)
        cx += wtx + 3
    if mark: L.art(f'badges/{mark}', cx, y + (h - 16) / 2, 16)
    if st == 'new': L.rrect(x + 1.5, y + 1.5, w - 3, h - 3, 7, line=TURQ, lw=2)
    if st == 'harm': L.rim(x + 3, y + 3, w - 6, h - 6, 5, 'told', col=LINE, lw=1.2)
    return w


def ways_row(L, x, y, ways, h=30, size=14.5, gap=4):
    """The ways grouped by who can do them: the doer's face (never the name), then that doer's pills."""
    cx, who = x, None
    for wy in ways:
        if wy[0] != who:
            if who is not None: cx += 4
            who = wy[0]; L.face(who, cx, y + 2, h - 4, rw=1.4); cx += h - 4 + 4
        cx += pill(L, cx, y, wy, h, size) + gap
    return cx


def card_body(L, x, y, w, p, role, head=True):
    """Name, job, three fact rows (what's known and what's hidden), the hairline, the ways. Returns the bottom."""
    if head:
        L.text(x, y + 11, p['name'], 19); nw = L.tw(p['name'], 19); L.text(x + nw + 9, y + 12, '·  ' + JOB[role], 14, col=DIM)
        y += 28
    for i, (ic, st, word) in enumerate(p['facts']):
        slot(L, i, x, y + i * 28, 24, ic, st); L.text(x + 38, y + i * 28 + 12, word, 15.5, col=DIM if st == 'unk' else CREAM)
        learn = p.get('learn')
        if learn and st != 'learned': L.icon(learn, x + w - 18, y + i * 28 + 3, 18, col=DIM)
    y += 3 * 28 + 2
    if p['ways']:
        L.line([(x, y + .5), (x + w, y + .5)], col=BRASS, lw=1); y += 8
        ways_row(L, x, y, p['ways']); y += 30
    return y


def stem(L, x0, y0, x1, y1, col=CREAM):
    L.line([(x0, y0), (x1, y1)], col=col, lw=1.6)
    L.disc(x1 - 3, y1 - 3, 6, fill=col, ring=None)


def bubble(L, x, y, t, who=None, size=14.5, tail=None, face='Barlow-Medium', maxw=None):
    """A speech bubble as built: a cream tag, ink Barlow, a little tail."""
    lines = t if isinstance(t, list) else [t]; w = max(L.tw(s, size, face) for s in lines) + 16; h = len(lines) * (size + 5) + 7
    L.rrect(x, y, w, h, 5, fill=(0xf4, 0xf1, 0xe8, 240), line=(0x8d, 0x8f, 0x87, 255), lw=1)
    for i, s in enumerate(lines): L.text(x + 8, y + 4 + (size + 5) * (i + .5), s, size, col=INK, face=face)
    return w, h
