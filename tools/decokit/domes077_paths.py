"""Brief 077 (Vela): dump the plan paths the dome-family pieces plant along, read from the generator itself (civic_street._run's paths,
civic_cafe.cafe_terrace's runs, plaza.py's arc constants), so the testbed composition fits the stonework exactly.
/Applications/Blender.app/Contents/MacOS/Blender -b -P tools/decokit/domes077_paths.py  -> out/decokit/troughs077/paths.json"""
import json
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
from geo import arc  # noqa: E402
from plaza_parts import offset  # noqa: E402
from civic_common import HF, front_path, split_runs  # noqa: E402
import civic_cafe  # noqa: E402
import plaza  # noqa: E402

out = {"runs": {}, "terrace": {}, "arc": {}}
for kind in ("2", "4", "Corner", "End"):
    if kind == "2":
        path = [(-1.0, 0.0), (1.0, 0.0)]
    elif kind == "4":
        path = [(-2.0, 0.0), (0.0, 0.0), (2.0, 0.0)]
    elif kind == "Corner":
        path = [(-1.0, -1.0), (0.0, -1.0)] + arc(0, 0, 1.0, 270, 360, 6)[1:] + [(1.0, 1.0)]
    else:
        path = [(-1.0, 0.0), (0.0, 0.0), (0.3, 0.0)]
    xs, ys = [p[0] for p in path], [p[1] for p in path]
    cx, cy = (min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2
    out["runs"][kind] = {"path": [[x - cx, y - cy] for x, y in path], "w": 0.62, "z_top": 0.6, "coping_half": 0.34, "coping_top": 0.655,
                         "end_cap": [0.3 - cx, -cy, 0.34] if kind == "End" else None}
PW_ = front_path(6, 4, flat_x=(-HF, -civic_cafe.GATE, 0.0, civic_cafe.GATE, HF))
runs = split_runs(offset(PW_, civic_cafe.WALL_A), lambda x, y: abs(x) >= civic_cafe.GATE - 1e-6 and y < 3.0)
out["terrace"] = {"runs": [[list(p) for p in r] for r in runs], "variants": ["AB"[k % 2] for k in range(len(runs))], "w": 0.6, "z_top": 0.62,
                  "coping_half": 0.33, "coping_top": 0.675}
out["arc"] = {"R": plaza.ARC_R, "HW": plaza.ARC_HW, "HALF_DEG": plaza.ARC_HALF, "H": plaza.ARC_H, "C": plaza.ARC_C,
              "outline": [list(p) for p in plaza.arc_outline(plaza.ARC_HW)]}
# the crown troughs: the Records Office roof terrace (records_tower.roof, lod 0) and the café crown (cafe_upper.crown), in their pieces' own frames
import records_tower as rt  # noqa: E402
import cafe_upper as cu  # noqa: E402
T = offset(rt.wing_path(8), 1.05, True)
rr = [q for q in (rt.runs(T, lambda x, y: not rt.in_tower(x, y, 0.45)) or [T]) if len(q) > 2]
out["records_roof"] = {"runs": [[list(p) for p in r] for r in rr], "w": 0.7, "z_top": 14.2 + 0.5, "coping_half": 0.38, "coping_top": 14.2 + 0.5 + 0.055}
out["cafe_crown"] = {}
for n in (3, 4):
    eaves = cu.PLINTH + cu.GROUND + cu.STOREY * (n - 1)
    TT = offset(front_path(10, 6), 0.9)
    out["cafe_crown"][str(n)] = {"runs": [[list(p) for p in TT]], "w": 0.8, "eaves": eaves, "z_top": eaves + 0.05 + 0.55, "coping_half": 0.43, "coping_top": eaves + 0.05 + 0.55 + 0.055}
p = HERE.parents[1] / "out/decokit/troughs077/paths.json"
p.write_text(json.dumps(out, indent=1) + "\n")
print("PATHS077", {k: len(v["path"]) for k, v in out["runs"].items()}, "terrace runs", len(runs))
