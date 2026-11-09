"""Deco Kit asset registry. Family modules register builders with @asset; `build` makes one object.

Conventions: metres, Z up, pivot = footprint centre at z = 0, front faces -Y (Blender front view).
"""

import importlib

import crown
from geo import Shape

REGISTRY = {}
FAMILIES = ("street", "signals", "booths", "ground", "vehicles", "structures", "plants", "plaza", "paving", "skyline", "landmarks", "rooftop", "skycraft", "facades", "lowrise", "roofdeck", "highrise", "alleys", "furnish", "signage", "skyport", "mobility", "robots", "calib", "regen", "atompunk", "horizon", "liner", "civic008", "crossing009", "crossing_opts", "crossing_lens", "cafe_grand", "cafe_upper", "cars020", "cars067", "cars070", "play020", "street020", "ground_set", "restore026", "cabinet", "lamps026", "road_ribbon", "cafe_fascia", "palms026", "pave026", "cafe_testbed", "pavilion", "fountain_tb", "palms_avenue", "palm_lod", "records_open", "robot_attention", "sides", "records_tower", "shopfill", "streamstack", "tram075", "vista075", "skyrail075", "caps075", "crossing079", "hover_camera", "civic_props097", "civic_props097b", "civic_props097c", "civic_props097d", "civic_mast101")


def asset(name, cat, bevel=0.005, lod=0, lean=1.0, cap=0, far=None, near=None, floor=None, retired=None, footprint=None):
    """lod = triangle target of a decimated distance version (LOD1) exported beside the asset; 0 = none.
    lean = segment scale for lathes, sweeps and rounded boxes (1 = as written): small street props seen at 15-120 px
    at play zoom take lean < 1 and bevel 0 to meet the per-category triangle caps (out/review/lod-thresholds.html).
    cap = L0 triangle cap: a collapse decimate (ratio cap / built triangles) on the piece itself, after lean; 0 = none.
    far = a hand-built L2 (fn(shape), same pivot and axes): exported as the L2 instead of the 6% decimation, for signatures whose
    silhouette the collapse destroys (OceanLiner, brief 007 round 3).
    near = a hand-built L1 (fn(shape), same pivot and axes, parts included): exported as the L1 instead of the 25% decimation, and for
    pieces under the 400-triangle LOD floor too (brief 009: flat and tiny crossing pieces, where a collapse would wreck the shape).
    floor = where the piece meets the ground (brief 055, K-1: a piece that stands on the ground is modelled on it, so its pivot is its foot):
    "seat" lifts or lowers the whole piece (parts too) so its lowest vertex is at z 0 (flat paving, tree pits, a hover robot); "clamp" leaves the piece
    where it is and flattens any vertex below z 0 onto it (foliage cards that droop under the ground, a window box's sprays below its sill).
    retired = a reason string (brief 061): the piece is marked for retirement. Its builder stays until Splice has re-cut the export; export_kit writes the reason into the manifest row (`retired`) so the
    catalogue step can list it. Retiring in kit.json is Splice's: drop its `use` tag from tools/kit/tags.ts, then set status `rejected` once the world export places none.
    footprint = (x, depth) in metres (brief 079): the manifest's size and bounds across the ground, when a piece's light spills past the thing itself (a stud's glow on the road, a signal's pool): the catalogue and the world bind the thing."""
    def deco(fn):
        REGISTRY[name] = dict(cat=cat, fn=fn, bevel=bevel, lod=lod, lean=lean, cap=cap, far=far, near=near, floor=floor, retired=retired, footprint=footprint)
        return fn
    return deco


def load(strict=False):
    """Import every family. One broken sibling (e.g. mid-edit) is reported and skipped unless strict."""
    for fam in FAMILIES:
        try:
            importlib.import_module(fam)
        except ModuleNotFoundError as e:
            if e.name != fam:
                raise
        except Exception as e:  # noqa: BLE001 - surfaced loudly, never silently
            if strict:
                raise
            print(f"[DK] WARNING family '{fam}' failed to import and is skipped: {e!r}")
    return REGISTRY


def build(name, col=None):
    spec = REGISTRY[name]
    s = Shape(f"DK_{name}")
    s.lean = spec.get("lean", 1.0)
    s.up_uv = spec["cat"] == "Robots"
    spec["fn"](s)
    obj = s.build(col, bevel=spec["bevel"])
    obj["dk_cat"] = spec["cat"]
    if spec.get("floor"):
        _floor(obj, spec["floor"])
    if spec.get("cap"):
        _cap(obj, spec["cap"])
    return crown.swap(obj, name, spec["cat"])


def _floor(obj, mode):
    """Brief 055 K-1: put the piece's foot on z 0 (see asset(floor=)). Vertex edits before the modifiers, on the root and its parts alike (they share the origin)."""
    objs = [obj, *obj.children_recursive]
    low = min(v.co.z for o in objs for v in o.data.vertices)
    if mode == "seat":
        for o in objs:
            for v in o.data.vertices:
                v.co.z -= low
    elif mode == "clamp":
        for o in objs:
            for v in o.data.vertices:
                if v.co.z < 0.0:
                    v.co.z = 0.0
    else:
        raise ValueError(f"floor={mode!r}: seat or clamp")


def _cap(obj, cap):
    """L0 triangle cap: one collapse decimate over the piece and its parts at ratio cap / built triangles (evaluated)."""
    import bpy
    objs = [obj, *obj.children_recursive]
    dg = bpy.context.evaluated_depsgraph_get()
    tris = 0
    for o in objs:
        me = bpy.data.meshes.new_from_object(o.evaluated_get(dg), depsgraph=dg)
        tris += sum(len(p.vertices) - 2 for p in me.polygons)
        bpy.data.meshes.remove(me)
    if tris > cap:
        for o in objs:
            m = o.modifiers.new("Cap", "DECIMATE")
            m.ratio = round(cap / tris, 4)


def build_lod(name, tag, col=None):
    """The hand-built L1 (kit.asset near=) or L2 (far=) of an asset, named DK_<name>_<tag> (no bevel: it is seen at a few dozen pixels)."""
    spec = REGISTRY[name]
    s = Shape(f"DK_{name}_{tag}")
    spec["near" if tag == "L1" else "far"](s)
    return crown.swap(s.build(col, bevel=0.0), name, spec["cat"])


def build_far(name, col=None):
    return build_lod(name, "L2", col)
