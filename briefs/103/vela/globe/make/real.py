"""Brief 103 (Vela, 2026-11-10): the real pieces beside the board. The board's rig (briefs/103/vela/globe/make/render.py: stage, tower, guard, light,
material response, cameras, masks) is loaded without its main(); the entrances are built by the generator itself (kit.build), no patch:
  after   tools/decokit/civic_records.py as it is now (the ball on its columns, satin nickel; the finials too)
  before  civic_records.py at <rev> (default HEAD: the chrome relief globe), registered over it in this session only
RecordsEntrance at yaw 210 and 300 (with the door's hover masks), RecordsEntrance_Official at 210 and 300 on a plain wall (no tower is placed for it here).
Renders only: nothing is exported, the kit and Unity are not touched.

bun run guard /Applications/Blender.app/Contents/MacOS/Blender -b -P briefs/103/vela/globe/make/real.py -- out/scratch/vela/globe/real [--rev HEAD] [--samples 64] [--threads 6]
python3 briefs/103/vela/globe/make/real_sheet.py"""
import importlib.util
import subprocess
import sys
from pathlib import Path

import bpy

REPO = Path(__file__).resolve().parents[5]
RIG = REPO / "briefs/103/vela/globe/make/render.py"
rig = {"__file__": str(RIG), "__name__": "rig103"}
exec(compile(RIG.read_text().rsplit("\nmain()", 1)[0], str(RIG), "exec"), rig)   # the rig's functions; its main() is not run
A = rig["A"]
REV = A[A.index("--rev") + 1] if "--rev" in A else "HEAD"
import kit  # noqa: E402  (render.py put tools/decokit on sys.path and imported civic_records: the registry holds the current builders)

FRAME_O = [(sx * 3.4, 0.3, 0.0) for sx in (-1, 1)] + [(sx * 3.4, -3.3, 0.0) for sx in (-1, 1)] + [(sx * 3.2, -0.3, 6.0) for sx in (-1, 1)]


def head_builders():
    """The builders of civic_records.py at REV, registered over the current ones (kit.REGISTRY is by name)."""
    src = subprocess.run(["git", "-C", str(REPO), "show", f"{REV}:tools/decokit/civic_records.py"], capture_output=True, text=True, check=True).stdout
    path = Path(rig["OUT"]) / "civic_records_rev.py"
    path.write_text(src)
    spec = importlib.util.spec_from_file_location("civic_records_rev", path)
    spec.loader.exec_module(importlib.util.module_from_spec(spec))


def build(name, tag, sun):
    obj = kit.build(name)
    obj.name = f"{name}-{tag}"
    ms = rig["meshes"](obj)
    for o in ms:
        for sl in o.material_slots:
            if sl.material:
                rig["game_look"](sl.material)
    rig["sun_off"](sun, ms)
    return obj


def drop(prefix):
    for o in [o for o in bpy.data.objects if o.name.startswith(prefix)]:
        bpy.data.objects.remove(o, do_unlink=True)


def public(sc, sun, door, tag):
    build("RecordsEntrance", tag, sun)
    for yaw in (210, 300):
        rig["camera"](sc, yaw, rig["FRAME"], rig["PPM"])
        rig["shoot"](sc, f"entrance-{tag}-y{yaw:03d}.png")
        rig["masks"](sc, yaw, f"entrance-{tag}", door, list(sc.objects))
    drop("RecordsEntrance-")


def official(sc, sun, tag):
    build("RecordsEntrance_Official", tag, sun)
    for yaw in (210, 300):
        rig["camera"](sc, yaw, FRAME_O, rig["PPM"])
        rig["shoot"](sc, f"official-{tag}-y{yaw:03d}.png")
    drop("RecordsEntrance_Official-")


def main():
    sc, sun = rig["stage"]()
    scene0 = set(bpy.data.objects)
    rig["tower"](sun)
    rig["guard"]()
    door = rig["door_standin"]()
    public(sc, sun, door, "after")
    # the official front stands on tower-official's flat face: a plain stone wall here, the public front's tower and guard hidden
    office = [o for o in bpy.data.objects if o not in scene0 and o.type == "MESH"]
    for o in office:
        o.hide_render = True
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0.0, 0.25, 3.5))
    wall = bpy.context.object
    wall.name, wall.scale = "wall", (9.0, 0.5, 7.0)
    wall.data.materials.append(rig["stone_mat"]("wall", "#E7DECA", 0.75))
    official(sc, sun, "after")
    head_builders()
    official(sc, sun, "before")
    wall.hide_render = True
    for o in office:
        o.hide_render = o is door                      # the door's stand-in stays hidden, as the rig keeps it
    public(sc, sun, door, "before")


main()
