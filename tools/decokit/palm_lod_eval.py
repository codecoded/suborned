"""Palm LOD distance sweep (Vela, brief 028): every level of both palms at the third-person camera's distances, at the panel's pixel scale.

blender -b -P tools/decokit/palm_lod_eval.py -- <out_dir>
Writes <out_dir>/<piece>_L<k>_d<dist>_az<az>.png (RGBA, palm only, no haze) for the board and metrics.json.
Metrics are made in palm_lod_metrics.py (numpy) from these files.
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
import palm_lod_scene as S  # noqa: E402

OUT = Path(sys.argv[sys.argv.index("--") + 1]); OUT.mkdir(parents=True, exist_ok=True)
import os  # noqa: E402
LEVELS = [int(x) for x in os.environ.get("LEVELS", "0,1,2").split(",")]
DIST = [int(x) for x in os.environ["DIST"].split(",")] if os.environ.get("DIST") else [25, 35, 45, 55, 65, 75, 90, 105, 120, 140, 160, 185, 210, 240, 280, 320, 350]
AZ = [0, 60, 120]
sc, cd, co = S.setup((1100, 1100), samples=64)
for name, zc in (("CoconutPalm_Street", 5.5), ("CoconutPalm", 4.2)):
    objs = [S.make(name, lv) for lv in LEVELS]
    for k, (lv, o) in enumerate(zip(LEVELS, objs)):
        for j, p in enumerate(objs):
            S.hide(p, j != k)
        for d in DIST:
            for az in AZ:
                S.shoot_persp(OUT / f"{name}_L{lv}_d{d}_az{az}.png", d, (1100, 1100), zc, cd, co, az=az)
    for p in objs:
        S.hide(p, True)
print("[palm-eval] done")
