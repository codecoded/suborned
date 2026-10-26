# Glyph, brief 039 (2026-10-26): the count of marks, by the code that draws them. A mark is one separately drawn thing the eye has to parse: a ring (its ticks are part of it), an arc, a line (one dashed line is one),
# a disc or plate (its icon, cameos and rings are part of it), a pip row (its pips are part of it), a spark, a flash, a puff. Board annotations (the numbered callouts) are not marks.
# `street` = on the map; `hud` = in the roster column and the crew tally; the capture's own always-on HUD (portraits, status pill, scope, goal line, notes key) is the same before and after and is not counted here.
# Today's board = heroes39.hero_A, hero_B(marked=False) and hero_C, the three frames of board-pick-A-B-C.png. python3 -I briefs/039/glyph/count39.py
import sys, json, collections
sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/039/glyph")
import g39, heroes39 as H
from scene39 import Frame, SQSHOT

REG = []
def _wrap(mod, name, kind, group="street"):
    fn = getattr(mod, name)
    def w(*a, **k): REG.append((kind, group)); return fn(*a, **k)
    setattr(mod, name, w)

# street marks, as the old heroes name them
for n, kind in (("ring39", "ring"), ("under_fire_arc", "arc"), ("firing_mark", "disc"), ("pips", "pips"), ("tracer", "line"), ("streak", "line"), ("muzzle", "flash"), ("spark", "spark"), ("puff", "puff"),
                ("cover_arc", "arc"), ("pin_disc", "disc"), ("brackets", "ring"), ("walk_to", "line"), ("reticle39", "disc"), ("beat_marker", "plate"), ("beat_ring", "ring"), ("threat", "line"), ("disc39", "disc")):
    _wrap(H, n, kind)
_wrap(H, "cover_disc", "disc")
# the roster column and the tally
for n, kind in (("roster_pips", "pips"), ("sidearm_mark", "disc"), ("portrait_under_fire", "ring"), ("sidearm_button", "button"), ("move_disc", "disc")): _wrap(g39, n, kind, "hud")
_wrap(H, "crew_tally", "plate", "hud"); _wrap(H, "refill", "ring", "hud")

def count(fn, **kw):
    REG.clear(); fr = Frame(SQSHOT); fn(fr, **kw)
    c = collections.Counter(REG); street = sum(v for (k, g), v in c.items() if g == "street"); hud = sum(v for (k, g), v in c.items() if g == "hud")
    by = collections.Counter(k for (k, g) in REG if g == "street")
    return dict(street=street, hud=hud, street_by_kind=dict(by), hud_by_kind=dict(collections.Counter(k for (k, g) in REG if g == "hud")))

def today():
    out = {}
    for name, fn, kw in (("A", H.hero_A, {}), ("B", H.hero_B, dict(marked=False)), ("C", H.hero_C, {})):
        r = count(fn, **kw); r["cell_rings_in_capture"] = 4; r["street_with_cell_rings"] = r["street"] + 4; out[name] = r
    return out

if __name__ == "__main__":
    t = today(); print(json.dumps(t, indent=1))
