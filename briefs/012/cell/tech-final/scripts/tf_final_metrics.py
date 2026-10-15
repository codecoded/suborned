import sys, json
sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/012/cell/tech-final/scripts")
import tf_proxy as tp
from tf_proxy import *
SLIM = [(-0.10, 0.095, 0.16, 0.13, 0.14), (0.15, 0.092, 0.11, 0.11, 0.12), (0.45, 0.080, 0.085, 0.085, 0.09), (0.88, 0.055, 0.06, 0.06, 0.06)]
tp.VARIANTS["SLIM"] = dict(profile=SLIM, mode="ts", P=None, label="control: slim trousers to the ankle, thigh and shin only")
r = run_all(dict(P_DEFAULT))
print(table(r))
for vn in ("FX10", "AD", "BELL"):
    v = VARIANTS[vn]; legs = {s: build_leg(sg, v["profile"]) for s, sg in SIDES.items()}
    W = weights(legs["R"], 1, v["mode"]); print(vn, "max influences/vertex", max_influences(W), "verts/leg", len(legs["R"]["t"]), "tris/leg", len(legs["R"]["faces"]) * 2, "bones used", sorted({BONES[i] for i in np.nonzero((W > 1e-4).any(0))[0]}))
for vn in ("AD", "FX10", "SLIM", "BELL"):
    print(vn, "kerb pose, kerb/ground:", r[vn]["kerb"]["metrics"].get("kerb"), r[vn]["kerb"]["metrics"].get("ground"), "angles", r[vn]["kerb"]["angles"], "ankle", r[vn]["kerb"]["ankle"])
