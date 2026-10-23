"""Palm LOD triangle totals for the district (Vela, brief 028): what the third-person camera draws of the 947 street palms, with and without levels.

python3 tools/decokit/palm_lod_district.py [world.json] [out.json]
Placements are read from out/world/harbour-district.world.json (street[] kit refs, `bun run world`). Views: tp-street and tp-worst as Redline measured them (brief 048),
plus every palm's own foot as a camera spot (a player standing on the pavement) at four headings. Frustum: 55 degrees vertical, 16:10, far 350 m. Switch distances in metres at scale 1.
"""
import json
import math
import sys

import numpy as np

W = sys.argv[1] if len(sys.argv) > 1 else "out/world/harbour-district.world.json"
TRIS = {"CoconutPalm": (6310, 1330, 514), "CoconutPalm_Street": (8230, 1330, 514)}
SW = (60.0, 140.0)           # L0 -> L1, L1 -> L2 at scale 1 (screen sizes in the brief are these distances for each piece)
w = json.load(open(W))
P = [(s["at"][0], s["at"][1], s["kit"]["id"], s["kit"]["scale"][0]) for s in w["street"] if (s.get("kit") or {}).get("id") in TRIS]
X = np.array([p[0] for p in P]); Y = np.array([p[1] for p in P]); K = np.array([p[2] == "CoconutPalm_Street" for p in P]); SC = np.array([p[3] for p in P])
T0 = np.where(K, 8230, 6310); T1 = np.full(len(P), 1330); T2 = np.full(len(P), 514)
asp = 16 / 10; hh = math.atan(math.tan(math.radians(55 / 2)) * asp)


def view(px, py, hd, far=350.0, sw=SW):
    th = math.radians(hd); fx, fy = math.sin(th), math.cos(th)
    dx, dy = X - px, Y - py; d = np.hypot(dx, dy)
    a = np.arctan2(dx * fy - dy * fx, dx * fx + dy * fy)
    vis = (d < far) & (d > 0.5) & (np.abs(a) < hh + 0.04)
    eff = d / SC                                   # a palm scaled up switches later, as Unity's LOD size does
    l0 = vis & (eff < sw[0]); l1 = vis & (eff >= sw[0]) & (eff < sw[1]); l2 = vis & (eff >= sw[1])
    base = int(T0[vis].sum()); new = int(T0[l0].sum() + T1[l1].sum() + T2[l2].sum())
    return dict(palms=int(vis.sum()), L0=int(l0.sum()), L1=int(l1.sum()), L2=int(l2.sum()), tris_all_L0=base, tris_levels=new)


out = {"switch_m": SW, "palms": len(P), "district_tris_all_L0": int(T0.sum()), "district_tris_all_L1": int(T1.sum()), "district_tris_all_L2": int(T2.sum()),
       "views": {"tp-street": view(-159, -252.5, 180), "tp-worst": view(-169, -257.5, 90)}}
rs = [view(px, py, hd) for px, py in zip(X[::3], Y[::3]) for hd in (0, 90, 180, 270)]
rs = [r for r in rs if r["palms"] > 0]
for key in ("palms", "tris_all_L0", "tris_levels"):
    v = np.array([r[key] for r in rs]); out[f"spots_{key}"] = dict(median=int(np.median(v)), p90=int(np.percentile(v, 90)), max=int(v.max()))
v = np.array([r["tris_levels"] / max(r["tris_all_L0"], 1) for r in rs]); out["spots_ratio"] = dict(median=round(float(np.median(v)), 3), p90=round(float(np.percentile(v, 90)), 3))
print(json.dumps(out, indent=1))
if len(sys.argv) > 2:
    open(sys.argv[2], "w").write(json.dumps(out, indent=1))
