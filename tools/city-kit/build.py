"""Run in Blender background. Write the playable pieces, reusable modules and editable source."""

from pathlib import Path
import json, sys
import bpy

sys.path.insert(0, str(Path(__file__).resolve().parent))
from architecture import Building, building, shop, marquee, clock_fin, upper
from ferry import use_sheet

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
OUT = ROOT / "unity/Assets/Suborned/Scenery/LanternKit"
MAPS = ROOT / "unity/Assets/Suborned/Materials/Play/LanternKit"
PLAN = json.loads((ROOT / "contracts/world/harbour-district.plan.json").read_text())
COURT = next(p for p in PLAN["megastructures"] if p["kind"] == "residential courtyard")


def court_pair(s):
    cx, cy = COURT["clock"]["at"]
    for wing in COURT["wings"]:
        if wing["id"] not in ("east", "market"):
            continue
        x0, x1, y0, y1 = wing["rect"]
        with s.place((x0 + x1) / 2 - cx, (y0 + y1) / 2 - cy):
            building(s, x1 - x0, y1 - y0, wing["radius"], wing["floors"], wing["id"] == "east")


def run(s):
    # This run joins occupied party walls. Match the existing roof datum and ribbon heights
    # from the measured six-metre sockets, so neither side of the return opens onto the sky.
    s.box(0, 7.50, 0, 18, 13, 16.15, "stone")
    if s.rank == 2:
        for i, display in enumerate(("flowers", "bureau", "produce")):
            x = (i - 1) * 6
            s.panel(x - 3, x + 3, 0.04, 0.40, 3.85, display)
            for z in (6.05, 9.45, 12.85):
                s.panel(x - 2.85, x + 2.85, 0.04, z, z + 2.35, "shade")
    else:
        for i, kind in enumerate((1, 0, 2)):
            with s.place((i - 1) * 6, 0):
                shop(s, 6, kind)
                for j, z in enumerate((6.05, 9.45, 12.85)):
                    upper(s, 6, z, 2.35, (i + j) % 4)
    for z in (4.0, 8.4, 11.8, 15.2):
        height = 2.04 if z == 4 else 0.99
        s.box(0, 0.38, z, 18, 0.94, height, "stone")
        s.box(0, -0.14, z + 0.02, 18, 0.26, 0.15, "cream")
        s.box(0, -0.005, z + height - 0.095, 18, 0.26, 0.085, "jade")
    s.box(0, 7.02, 16.13, 18, 14.19, 0.16, "roof")
    marquee(s, 7.5)


def straight(s, kind):
    shop(s, 6, kind)
    upper(s, 6, 4.4, 2.8, kind)


def export(name, fn, ceilings):
    collection = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(collection)
    models = []
    counts = []
    for rank in range(3):
        s = Building(name + " " + ("Close", "Street", "Horizon")[rank], rank)
        fn(s)
        obj, count = s.mesh()
        # Adjacent bays can share a coplanar face. Count the validated mesh the FBX reader will use.
        if obj.data.validate():
            obj.data.update()
            obj.data.calc_loop_triangles()
            count = len(obj.data.loop_triangles)
        models.append(obj)
        counts.append(count)
        if count > ceilings[rank]:
            raise ValueError(f"{name} L{rank} {count} exceeds {ceilings[rank]}")
    if not counts[0] > counts[1] > counts[2]:
        raise ValueError((name, counts))
    for key, at in [("East", (1, 0, 0)), ("North", (0, 1, 0))]:
        ob = bpy.data.objects.new(key, None)
        ob.location = at
        bpy.context.collection.objects.link(ob)
        models.append(ob)
    bpy.ops.object.select_all(action="DESELECT")
    for ob in models:
        ob.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=str(OUT / (name + ".fbx")),
        use_selection=True,
        object_types={"MESH", "EMPTY"},
        axis_forward="-Z",
        axis_up="Y",
        bake_anim=False,
        add_leaf_bones=False,
        mesh_smooth_type="FACE",
        path_mode="STRIP",
    )
    for obj in models:
        bpy.context.collection.objects.unlink(obj)
        collection.objects.link(obj)
        if obj.type == "EMPTY":
            obj.name = name + " " + obj.name
    print("Lantern:", name, counts, flush=True)
    return counts


def main():
    work = Path(sys.argv[sys.argv.index("--") + 1]).resolve()
    work.mkdir(parents=True, exist_ok=True)
    OUT.mkdir(parents=True, exist_ok=True)
    use_sheet(MAPS / "LanternBase.jpg")
    result = {}
    for name, fn, ceil in [
        ("ExchangeCourt", court_pair, (20000, 14500, 1350)),
        ("ExchangeShops", run, (3100, 2550, 300)),
        ("TicketBay", lambda s: straight(s, 0), (500, 390, 265)),
        ("FloristBay", lambda s: straight(s, 1), (500, 390, 265)),
        ("MarketBay", lambda s: straight(s, 2), (500, 390, 265)),
        ("BladeCanopy", lambda s: marquee(s, 7.5), (485, 290, 80)),
        ("ClockFin", lambda s: clock_fin(s, 12.72), (425, 315, 175)),
    ]:
        result[name] = export(name, fn, ceil)
    (work / "triangles.json").write_text(json.dumps(result, indent=2) + "\n")
    bpy.ops.wm.save_as_mainfile(filepath=str(work / "Lantern Exchange.blend"))


if __name__ == "__main__":
    main()
