"""Each level at play zoom (Vela, brief 028 "Palm LODs"): orthographic, half-height 14 m (zoom 14), pitch 38, yaw 30, the panel's 1864 px: 66 px a metre. Shows why the levels never take over there.

blender -b -P tools/decokit/palm_lod_play.py -- <out_dir>      writes <piece>_L<k>_play<half>.png (RGBA, crown and trunk top, no haze: the play camera takes none)
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
import palm_lod_scene as S  # noqa: E402

OUT = Path(sys.argv[sys.argv.index("--") + 1]); OUT.mkdir(parents=True, exist_ok=True)
sc, cd, co = S.setup((760, 900), samples=64)
for name, zc in (("CoconutPalm_Street", 7.2), ("CoconutPalm", 5.2)):
    objs = [S.make(name, lv) for lv in (0, 1, 2)]
    for lv in range(3):
        for j, p in enumerate(objs):
            S.hide(p, j != lv)
        for half in (14.0, 18.5):
            S.shoot_ortho(OUT / f"{name}_L{lv}_play{half:g}.png", (760, 900), zc, cd, co, half=half)
    for p in objs:
        S.hide(p, True)
print("[palm-play] done")
