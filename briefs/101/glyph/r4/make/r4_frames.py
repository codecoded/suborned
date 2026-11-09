"""Brief 101, round 4 (Glyph): the frames. Each is a real 0.2.290 capture with the old pieces lifted (round 3's fills) and the new ones drawn in the
built street kit (r4_base). Writes briefs/101/glyph/r4/<name>.png (1920 x 1080), the 2x copies in out/scratch/glyph/101r4/ and, for r4_budget.py, each
mock's new layer, what stays of today's HUD alpha and its manifest (today's, the lifted pieces' elements out, the new pieces' in, named as the build
names them). Glyph. Mock only."""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from r4_base import *

LINES = {
    'call': "It's time, Miss Nikolaou. Call Arthur across now, the way we said, and you'll do fine.",  # text.json conversations[8] beats[0], the Fixer
    'line': "People talk on a terrace, and I listen. It's yours, Miss Nikolaou, and you needn't pay a penny of it back.",  # conversations[2] beats[2], the Fixer
    'guard': "There's no running past this post.",  # dialogue[290], run-seen, the guard
    'inez': 'Someone knows about us, love. They looked suspicious.',  # conversations[9] street[0], Inez
}
MOCKS = {}  # name: dict(key, layer, rest, boxes, drop, add, boxes_new): r4_budget writes the HUD layers and manifests from them
BOXES = {}  # name: {piece: (x0, y0, x1, y1)}: where the new pieces landed, for the crops and the 2x check


def render(name, key, boxes, keep, draw, drop=(), add=(), thin=()):
    """`boxes`: old pieces lifted; `draw(P)` returns {piece: box} of the new ones; `drop`: today's manifest texts that give way although nothing is lifted
    for them (none here); `add`: the new manifest elements (kind, (x, y, w, h), text)."""
    frame_im, rest = lift(key, boxes, keep) if boxes else (real(key)[0], real(key)[1])
    if thin: frame_im, rest = thin_fill(frame_im, rest, key, thin)
    L = layer(); got = draw(L) or {}; BOXES[name] = got
    MOCKS[name] = dict(key=key, layer=L, rest=rest, boxes=list(boxes) + list(thin), drop=list(drop), add=list(add))
    im = compose(frame_im, L); os.makedirs(OUT4, exist_ok=True); os.makedirs(SCR4, exist_ok=True)
    im.resize((1920, 1080), Image.LANCZOS).convert('RGB').save(OUT4 + f'{name}.png'); im.convert('RGB').save(SCR4 + f'{name}-2x.png')
    _, al = real(key); before = float((al >= 24).mean()); after = float((frame_alpha(rest, L) >= 24).mean())
    print(f'{name:11} today {before * 100:.2f} %  mock {after * 100:.2f} %'); return got


def xywh(b): x0, y0, w, h = b; return (round(x0), round(y0), round(w), round(h))


# ---------------------------------------------------------------- item 9: the docked line
def fight():
    """4:15 at the square: the Fixer's call line and a rival's at their speakers, the guard's line docked at the top edge (three lines today). The
    guard's docked line becomes the street bubble beside his face, and the oldest of the three, the Fixer's line, gives way."""
    def draw(P):
        b = docked_bubble(P, LINES['guard'], (1112, 40), 'l'); return {'docked line': (b[0], b[1], b[0] + b[2], b[1] + b[3])}
    got = render('fight', 'fight', [(660, 322, 1256, 359), (850, 23, 1083, 58)], [(1112, 40, 29)], draw, thin=[(953, 357, 962, 404)])
    x0, y0, x1, y1 = got['docked line']
    MOCKS['fight']['add'] = [('speech', (round(x0), round(y0), round(x1 - x0), round(y1 - y0)), LINES['guard'])]


def inset():
    """2:39: Inez's line said off screen, docked at the left edge under the camera's callout. Today's plate (two rows at 320 px) becomes one row of
    the street bubble; it keeps 10 px off the scope's cones button under it (the side place, slid up 7 px)."""
    def draw(P):
        b = docked_bubble(P, LINES['inez'], (40, 882), 'r', dy=-7); return {'docked line': (b[0], b[1], b[0] + b[2], b[1] + b[3])}
    got = render('inset', 'inset', [(70, 856, 362, 899), (108, 899, 362, 906)], [(40, 882, 29), (73, 896, 110, 934)], draw)
    x0, y0, x1, y1 = got['docked line']
    MOCKS['inset']['add'] = [('speech', (round(x0), round(y0), round(x1 - x0), round(y1 - y0)), LINES['inez'])]


# ---------------------------------------------------------------- item 10: the verb row
ENF = (['enter', 'fire', 'takedown', 'watch'], ['Enter', 'Fire', 'Knock out', 'Watch'], [1, 2, 3, 4])  # the Enforcer's four (the plate's order)


def plate():
    """The Action plate on, Knock out armed (r29): the take-control tab, then the verb row; the armed tab's name under it; the cursor's armed verb keyed."""
    def draw(P):
        take_tab(P, 310); right = verb_row(P, 310, ENF[0], armed=2, names=ENF[1]); cursor_badge(P, 737, 442, 'takedown')
        return {'take control tab': (100, 295, 130, 325), 'verb row': (row_x(), 295, right, 345), 'armed verb at the cursor': (735, 440, 757, 462)}
    got = render('plate', 'plate', [(100, 265, 373, 354), (205, 349, 287, 393), (728, 433, 764, 469)], [(84, 279, 97, 347)], draw)
    MOCKS['plate']['add'] = [('image', xywh((100, 295, 30, 30)), 'take control tab'), ('image', xywh((row_x(), 295, got['verb row'][2] - row_x(), 50)), 'verb row'),
                             ('image', (735, 440, 22, 22), 'armed verb at the cursor')]


def seed():
    """The Action plate on, Alt held, nothing armed (the seed proof): the tab and the row; no keys on Alt either (Sky's no-legend rule)."""
    def draw(P):
        take_tab(P, 310); right = verb_row(P, 310, ENF[0]); return {'take control tab': (100, 295, 130, 325), 'verb row': (row_x(), 295, right, 325)}
    got = render('seed', 'seed', [(100, 265, 373, 354)], [(84, 279, 97, 347)], draw)
    MOCKS['seed']['add'] = [('image', (100, 295, 30, 30), 'take control tab'), ('image', (row_x(), 295, got['verb row'][2] - row_x(), 30), 'verb row')]


def strip():
    """The strip after Shift (the Scout's Watch, Listen, Mislead): the same row, shown 2.5 s, its tabs taking clicks while it shows."""
    def draw(P):
        take_tab(P, 52); right = verb_row(P, 52, ['watch', 'listen', 'mislead']); return {'take control tab': (100, 37, 130, 67), 'verb row': (row_x(), 37, right, 67)}
    got = render('strip', 'strip', [(100, 15, 263, 89)], [(84, 19, 97, 87), (52, 52, 32)], draw)
    MOCKS['strip']['add'] = [('image', (100, 37, 30, 30), 'take control tab'), ('image', (row_x(), 37, got['verb row'][2] - row_x(), 30), 'verb row')]


STATES = [('at rest', dict()), ('hovered: its name, then its key', dict(hover=1, keys=ENF[2])), ('armed: deep teal, its name under it', dict(armed=2)),
          ('shut: Fire while carrying Popovic', dict(shut=(1,)))]


def states():
    """The verb row's states on the plate frame's lifted street (the Enforcer's four), and the Fixer's turn ways with their outcome badges over a hovered
    person (illustrative: the Fixer's face is not the selected one in this capture). Writes out/scratch/glyph/101r4/state-<n>.png at 2x."""
    frame_im, _ = lift('plate', [(100, 265, 373, 354), (205, 349, 287, 393), (728, 433, 764, 469)], [(84, 279, 97, 347)]); base = frame_im.resize((3840, 2160), Image.LANCZOS)
    for i, (_, kw) in enumerate(STATES):
        L = layer(); take_tab(L, 310); verb_row(L, 310, ENF[0], names=ENF[1], **kw); im = base.copy().convert('RGBA'); im.alpha_composite(L.img)
        im.crop((0, 2 * 262, 2 * 420, 2 * 362)).convert('RGB').save(SCR4 + f'state-{i}.png')
    L = layer(); take_tab(L, 138); verb_row(L, 138, ['ask', 'pay', 'help', 'pressure'])
    x = row_x(); tab_badge(L, x + 36, 123, 30, 'mark_half'); tab_badge(L, x + 72, 123, 30, 'mark_ok'); tab_badge(L, x + 108, 123, 30, 'mark_no')
    im = base.copy().convert('RGBA'); im.alpha_composite(L.img); im.crop((0, 2 * 90, 2 * 420, 2 * 190)).convert('RGB').save(SCR4 + 'state-4.png')


# ---------------------------------------------------------------- the wrap, for Sky
def line():
    """r123's card frame, 4:27: the Fixer's 21-word line of the Help. A: one row as built (712 px). B: two rows at 460. The bottom stays where the build
    puts it over her head (34 px, 16 more over her activity ring), so a second row grows upward."""
    for opt, wrap in (('one', None), ('wrap', WRAP)):
        def draw(P, wrap=wrap):
            b = street_bubble(P, 'fixer', LINES['line'], (826, 531), wrap=wrap, keep_y=482); return {'bubble': (b[0], b[1], b[0] + b[2], b[1] + b[3])}
        got = render(f'line-{opt}', 'line', [(466, 449, 1187, 487)], [], draw, thin=[(821, 486, 832, 534)])
        x0, y0, x1, y1 = got['bubble']; MOCKS[f'line-{opt}']['add'] = [('speech', (round(x0), round(y0), round(x1 - x0), round(y1 - y0)), LINES['line'])]


def call():
    """Route A's call, 2:59: the Fixer's 17-word line, which ran to x 2120. Both options as clamped (Latch's fix 5: 14 px inside the screen) and kept
    10 px off the Scout's edge face (x 1879), which the clamp alone still covers. A: one row (590 px). B: two rows at 460."""
    for opt, wrap in (('one', None), ('wrap', WRAP)):
        def draw(P, wrap=wrap):
            b = street_bubble(P, 'fixer', LINES['call'], (1825, 1014), wrap=wrap, keep_y=980, xmax=1879 - 10)
            edge_face(P, 'scout', 1879, 964); return {'bubble': (b[0], b[1], b[0] + b[2], b[1] + b[3])}
        got = render(f'call-{opt}', 'call', [(1526, 947, 1920, 983), (1876, 960, 1913, 998)], [(1866, 983, 1888, 999)], draw, thin=[(1821, 982, 1830, 1002)])
        x0, y0, x1, y1 = got['bubble']; MOCKS[f'call-{opt}']['add'] = [('speech', (round(x0), round(y0), round(x1 - x0), round(y1 - y0)), LINES['call'])]


def main():
    fight(); inset(); plate(); seed(); strip(); line(); call(); states()
    json.dump({k: {p: [round(v, 1) for v in b] for p, b in d.items()} for k, d in BOXES.items()}, open(OUT4 + 'make/boxes.json', 'w'), indent=1)


if __name__ == '__main__':
    main()
