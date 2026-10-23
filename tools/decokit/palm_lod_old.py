"""The 20 Oct generator levels (25 % and 6 % collapse decimation of the exported FBX), swept like the new ones, for the "before" of the board.

blender -b -P tools/decokit/palm_lod_old.py -- <out_dir>     writes <piece>_L0/L1/L2_d<dist>_az<az>.png where L1 and L2 are out/decokit/Models/Plants/DK_<piece>_L1/_L2.fbx
"""
import sys
from pathlib import Path

import bpy

sys.path.insert(0, str(Path(__file__).parent))
import palm_lod_scene as S  # noqa: E402

OUT = Path(sys.argv[sys.argv.index("--") + 1]); OUT.mkdir(parents=True, exist_ok=True)
FBX = Path(__file__).resolve().parents[2] / "out/decokit/Models/Plants"
sc, cd, co = S.setup((1100, 1100), samples=64)


def fbx_level(path, donor):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(path))
    new = [o for o in bpy.data.objects if o not in before]
    mats = {m.name: m for o in [donor, *donor.children_recursive] if o.type == "MESH" for m in o.data.materials if m}
    for o in new:
        if o.type == "MESH":
            for i, s in enumerate(o.material_slots):
                nm = s.material.name.split(".")[0] if s.material else ""
                if nm in mats:
                    o.material_slots[i].material = mats[nm]
    return S.link(new[0] if new[0].parent is None else new[0].parent), new


for name, zc in (("CoconutPalm_Street", 5.5), ("CoconutPalm", 4.2)):
    l0 = S.make(name, 0)
    levels = {0: [l0]}
    for lv in (1, 2):
        root, new = fbx_level(FBX / f"DK_{name}_L{lv}.fbx", l0)
        levels[lv] = [o for o in new]
    print("[old]", name, {lv: sum(len(p.vertices) - 2 for o in v if o.type == "MESH" for p in o.data.polygons) for lv, v in levels.items()})
    for lv, objs in levels.items():
        for l2, v in levels.items():
            for o in v:
                S.hide(o, l2 != lv)
        for d in (60, 95, 140, 200):
            for az in (0, 60, 120):
                S.shoot_persp(OUT / f"{name}_L{lv}_d{d}_az{az}.png", d, (1100, 1100), zc, cd, co, az=az)
    for v in levels.values():
        for o in v:
            S.hide(o, True)
print("[old] done")
