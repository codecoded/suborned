"""The HUD's share of the screen, by the budget tool's 'world' method (tools/hud-budget: the frame with the HUD against the frame without):
today's three captures, and each direction at rest (roster, clock, scope, Book; no card, seal or hover) and in its three moments' frames."""
import sys, os, json
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from h_base import *
import h_page as A, h_sign as B, h_street as C


def today(path):
    return float((np.array(Image.open(R + path + '.hud.png').convert('RGBA'))[:, :, 3] >= 24).mean())


def rest(mod, name):
    P0 = base('quiet'); P = base('quiet'); mod.roster(P, QUIET); mod.scope(P, 'quiet')  # at rest: roster, clock, key people, scope, Book
    if mod is A: mod.clock_corner(P, QUIET); mod.book_corner(P)
    else: mod.clock(P, QUIET); mod.book(P)
    save(P, name); return hud_cover(P0, P)


if __name__ == '__main__':
    for k in ('loud', 'quiet', 'aim'): frame(k)
    for d, mod in (('A', A), ('B', B), ('C', C)): mod.render()  # the pictures
    for d, f in (('A', 'A-page'), ('B', 'B-sign'), ('C', 'C-street')): rest({'A': A, 'B': B, 'C': C}[d], f + '-rest')
    MODE['clear'] = True  # the measure: the HUD alone, alpha >= 24, as tools/hud-budget's 'hud' mask (HUD_ALPHA)
    out = {'method': 'share of the 1920x1080 screen where the HUD layer alone has alpha >= 24 (tools/hud-budget HUD_ALPHA, its hud mask); shadows and halos count',
           'today': {'rest (start-overhead-day)': round(today('out/batch/shots/start-overhead-day') * 100, 1), 'loud (why-loud-police)': round(today(FRAMES['loud'][0][len(R):]) * 100, 1),
                     'quiet (why-quiet-radio)': round(today(FRAMES['quiet'][0][len(R):]) * 100, 1), 'aim (fight-loud-checkpoint)': round(today(FRAMES['aim'][0][len(R):]) * 100, 1)},
           'rest': {'A': round(rest(A, 'A-page-rest') * 100, 1), 'B': round(rest(B, 'B-sign-rest') * 100, 1), 'C': round(rest(C, 'C-street-rest') * 100, 1)}}
    for d, mod in (('A', A), ('B', B), ('C', C)): out[d] = {k: round(v[1] * 100, 1) for k, v in mod.render().items()}
    json.dump(out, open(OUT + 'make/coverage.json', 'w'), indent=1); print(json.dumps(out, indent=1))
