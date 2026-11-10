"""Brief 104 (Vela, 2026-11-10): the camera family seen as Sky sees it, before and after. Sky on MastCamera_Lamp's heads at play zoom (build 0.2.310): "this ugly is
poorly done - find any others like it and fix". Renders each camera piece from its exported FBX with review101_mast's light and dress (the kit's material rows x the
baked vertex AO), at the play camera's angle (orthographic, 38 degrees down), from above the way the opening camera meets it.

/Applications/Blender.app/Contents/MacOS/Blender -b -P tools/decokit/review104_cameras.py -- <out_dir> <decokit_root> [--only A,B]
  <id>_play_y<yaw>.png    the whole piece at play zoom 10 on the Air's fullscreen (93.2 px/m), a 1.75 m figure beside it for scale.
  <id>_crop_y<yaw>.png    the head(s) at 160 px/m: the density of Sky's crop of the mast (fe96bb36..., 2026-11-10).
  <id>_close.png          a perspective three-quarter of the head(s) from above, for the finish (not a play view).
  yaw 180 = from Blender +Y: the opening view of the mast at B (the backs of its heads, as Sky saw them); yaw 30 = the lens side.
  Prints each piece's L0 triangles ([104] tris) and, for MastCamera_Lamp, review101's lens check (centre 7.68 m, bearings 0/60/120)."""
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

A = sys.argv[sys.argv.index("--") + 1:]
OUT, ROOT = Path(A[0]), Path(A[1])
ONLY = set(A[A.index("--only") + 1].split(",")) if "--only" in A else None
OUT.mkdir(parents=True, exist_ok=True)
sys.argv = [sys.argv[0], "--", str(OUT), str(ROOT), "--capture-none"]   # review101 reads its own argv at import; give it ours, then use only its helpers
AIR, CROP = 1864 / 20, 160.0

# id -> (FBX, where the piece stands (Blender), its nose-down tilt (deg), the head(s) to frame: centre and size (m), the play frame's centre height and height (m))
PIECES = {
    "MastCamera_Lamp": ("DK_MastCamera_Lamp.fbx", (0, 0, 0), 0, (0.0, 0.0, 7.72), 2.3, 3.6, 9.0),
    "MastCamera": ("DK_MastCamera.fbx", (0, 0, 0), 0, (0.0, -0.2, 7.72), 1.8, 3.6, 9.0),
    "HoverCamera": ("DK_HoverCamera.fbx", (0, 0, 4.78), 16.7, (0.0, 0.0, 4.78), 1.4, 2.2, 6.0),   # the host hovers it at the pole lens, 16.7 deg nose down
    "DomeCamera": ("DK_DomeCamera.fbx", (0, 0, 3.2), 0, (0.0, -0.25, 3.45), 0.9, 1.8, 5.0),       # on a wall at 3.2 m
}


def helpers():
    """review101_mast's stage, dress, loader and camera, without running its review."""
    src = (Path(__file__).parent / "review101_mast.py").read_text()
    src = src.split("\nif \"--capture\" in A:")[0]
    g = {"__name__": "review101_helpers", "__file__": str(Path(__file__).parent / "review101_mast.py")}
    exec(compile(src, "review101_mast.py", "exec"), g)   # noqa: S102  (our own review module, cut before its entry point)
    return g


H = helpers()


def tris(objs):
    dg = bpy.context.evaluated_depsgraph_get()
    n = 0
    for o in objs:
        me = o.evaluated_get(dg).to_mesh()
        n += sum(len(p.vertices) - 2 for p in me.polygons)
        o.evaluated_get(dg).to_mesh_clear()
    return n


def ortho(sc, target, yaw, size_m, ppm, name, aspect=1.0):
    w, h = int(round(size_m * aspect * ppm / 2) * 2), int(round(size_m * ppm / 2) * 2)
    H["cam"](sc, target, yaw, 38, 60, ortho=max(w, h) / ppm, res=(w, h))
    H["shoot"](sc, name)


def piece(pid):
    fbx, at, tilt, focus, size, pz, ph = PIECES[pid]
    sc = H["setup"]()
    objs = H["load"](ROOT / "Models/Signals" / fbx)
    n = tris(objs)
    print(f"[104] tris {pid} {n}")
    if pid == "MastCamera_Lamp":
        H["lens_check"](objs)
    rig = bpy.data.objects.new("rig", None)   # the FBX roots carry the axis bake: stand and tilt them through a parent at the pivot
    sc.collection.objects.link(rig)
    for o in objs:
        if o.parent is None:
            o.parent = rig
    rig.location, rig.rotation_euler = Vector(at), (math.radians(tilt), 0, 0)
    H["figure"]("scale", (1.3, 1.1))
    for yaw in (180, 30):
        ortho(sc, (0.0, 0.0, pz), yaw, ph, AIR, f"{pid}_play_y{yaw:03d}.png", aspect=0.6)
        ortho(sc, focus, yaw, size, CROP, f"{pid}_crop_y{yaw:03d}.png")
    H["cam"](sc, focus, 200, 34, size * 1.9, res=(1200, 900), lens=50)
    H["shoot"](sc, f"{pid}_close.png")


for pid in PIECES:
    if ONLY is None or pid in ONLY:
        piece(pid)
