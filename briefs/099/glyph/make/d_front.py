"""Concept A · The front page. One spread, at a glance: the outcome and its line; the way in you chose, its start-page rows answered
(when each came true, the one that lost it in orange); five numbers, each with its one line; what the city does now; the run along
the foot, the cell's acts as their faces. Every number opens its explainer as the book's fold. Glyph, brief 099. Mock only."""
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))  # python3 -I drops the script folder
from d_parts import *
from d_data import page

FRAME = {'quiet-win.route-a': 'win', 'fail-lookout': 'fail', 'loud-win.seed3': 'loud'}


def front(M, detail=None):
    P = Page(2); light_page(P, FRAME[M['name']], M['subject']); tabs(P)
    y = head(P, M) + 30
    y = conds(P, M, X0, y, X1 - X0)
    if M['kind'] == 'fail': y = next_box(P, M, X0, y + 22, X1 - X0) + 34
    else: y += 26
    ty = max(y, 470); _, boxes = tiles(P, M, 880, ty, X1 - 880, sel=detail, widths=[150, 178, 168, 160, 209])
    if not detail:
        if M['kind'] == 'win': city_rows(P, M, 880, ty + 206, X1 - 880)
        strip(P, M, 880, 790 if M['kind'] == 'win' else max(ty + 205, 790), X1 - 880)
    ways_on(P, M['seed'])
    if detail == 'trail':
        bx = boxes['trail']; dim_page(P, keep=[bx, (880, ty + 176, X1 + 10, 990), (1090, 950, 1880, 1070)], alpha=170)
        fold_trail(P, M, 880, ty + 178, X1 - 880, (bx[0] + bx[2]) / 2, by='what'); ways_on(P, M['seed'])
    return P


if __name__ == '__main__':
    import sys
    for n, det, out in (('quiet-win.route-a', None, 'A-front-win'), ('fail-lookout', None, 'A-front-fail'), ('loud-win.seed3', 'trail', 'A-front-detail')):
        if len(sys.argv) > 1 and out not in sys.argv[1:]: continue
        P = front(page(n), det); save(P, out); print(out, P.words, 'words')
