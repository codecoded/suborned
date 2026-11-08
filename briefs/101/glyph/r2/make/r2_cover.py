"""Room on screen, round 2: each direction drawn alone on a transparent layer and read as tools/hud-budget reads a capture's .hud.png
(alpha >= 24 at 1920 x 1080: keylines, shadows and halos count; rings on the ground are drawn on the HUD layer here and count too), beside
today's captures. Writes coverage.json. Re-render: python3 -I r2_today.py && python3 -I r2_street.py && python3 -I r2_tokens.py && python3 -I r2_cover.py"""
import sys, os, json
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from r2_base import *
import r2_street as S, r2_tokens as T, r2_today as D

MOMENTS = ['rest', 'quiet', 'loud', 'aim', 'busy']

if __name__ == '__main__':
    today = {k: v[1] for k, v in D.render().items()}
    MODE['clear'] = True
    s = {k: v[1] for k, v in S.render().items()}; t = {k: v[1] for k, v in T.render().items()}
    out = {'method': 'share of the 1920x1080 screen where the HUD layer alone has alpha >= 24 (tools/hud-budget HUD_ALPHA, its hud mask); keylines, shadows and the mocks\' ground rings count',
           'today': {'source': 'build 0.2.268 captures (out/scratch/glyph/101r2/src, SHA256.txt); aim and busy with the pieces they lack laid on from other captures\' HUD layers (r2_today.py)'},
           'moments': {'rest': 'roster, clock, scope, Book', 'quiet': '3:28, a guard radioing it in (4 s) and Our car boxed in (route-g-radio)',
                       'loud': '2:34, the seal (3:07) and Police coming, here in 0:42 (loud.seed3)', 'aim': '2:31, the first shot: the Enforcer on the guard at 5 m (loud.seed3)',
                       'busy': '3:47.5, the final fight: the seal at 1:54, Police coming 1 of 5, two bubbles, the car held by its blocker, Fixer and Tech firing on him, the player\'s hold on him, a report about to land (from 4:54), Popovic off screen'},
           'rows': {k: {'today': round(today[k] * 100, 2), 'street': round(s[k] * 100, 2), 'tokens': round(t[k] * 100, 2)} for k in MOMENTS}}
    json.dump(out, open(OUT2 + 'make/coverage.json', 'w'), indent=1)
    print(f"{'':6} {'today':>7} {'street':>7} {'tokens':>7}")
    for k in MOMENTS: r = out['rows'][k]; print(f"{k:6} {r['today']:6.2f}% {r['street']:6.2f}% {r['tokens']:6.2f}%")
