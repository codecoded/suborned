import sys
sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/012/cell/tech-final/scripts")
import tf_proxy as tp
from tf_proxy import *
for inner in (0.15, 0.13, 0.115, 0.105, 0.095):
    prof = [(-0.10, 0.095, 0.17, 0.13, 0.14), (0.05, min(inner, 0.11), 0.18, 0.15, 0.15), (0.30, inner, 0.17, 0.17, 0.17)]
    tp.VARIANTS["B2"] = dict(profile=prof, mode="rigid", P=None, label="b")
    r = run_all(dict(P_DEFAULT), only=["B2"])["B2"]
    print(inner, {p: round(d["metrics"]["leg_leg"][0] * 100, 1) for p, d in r.items() if d["metrics"]["leg_leg"][0] > 0})
