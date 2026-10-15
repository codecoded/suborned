import sys, itertools, time, json
sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/012/cell/tech-final/scripts")
import tf_proxy as tp
from tf_proxy import *
def prof(inner_knee, inner_hem, outer_hem, hem_t, back_knee, back_hem):
    return [(-0.10, 0.095, 0.17, 0.13, 0.14), (0.15, 0.100, 0.18, 0.14, 0.15), (0.45, inner_knee, 0.18, 0.15, back_knee), (hem_t, inner_hem, outer_hem, 0.14, back_hem)]
P = dict(P_DEFAULT)
rows = []
for drv in (False, True):
  for inner_hem in (0.15, 0.13, 0.115, 0.10):
    for hem_t in (0.665, 0.615):
      for back_knee in (0.15, 0.12, 0.10):
        tp.VARIANTS["T"] = dict(profile=prof(min(inner_hem, 0.14), inner_hem, 0.22, hem_t, back_knee, back_knee), mode="sec" if drv else "ts", P="drive" if drv else None, label="t")
        r = run_all(P, only=["T"])["T"]
        w = {m: max(r[pn]["metrics"][m][0] for pn in r) for m in ("leg_leg", "through_leg")}
        per = {pn: round(max(r[pn]["metrics"]["leg_leg"][0], r[pn]["metrics"]["through_leg"][0]) * 100, 1) for pn in r}
        rows.append((drv, inner_hem, hem_t, back_knee, round(w["leg_leg"] * 100, 1), round(w["through_leg"] * 100, 1)))
        print(rows[-1], flush=True)
