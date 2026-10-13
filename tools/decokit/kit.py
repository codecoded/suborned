"""Deco Kit asset registry. Family modules register builders with @asset; `build` makes one object.

Conventions: metres, Z up, pivot = footprint centre at z = 0, front faces -Y (Blender front view).
"""

import importlib

from geo import Shape

REGISTRY = {}
FAMILIES = ("street", "signals", "booths", "ground", "vehicles", "structures", "plants", "plaza", "paving", "skyline", "landmarks", "rooftop", "skycraft", "facades", "lowrise", "roofdeck", "highrise", "alleys", "furnish", "signage", "skyport", "mobility", "robots", "calib", "regen", "atompunk", "horizon", "liner", "civic008")


def asset(name, cat, bevel=0.005, lod=0, lean=1.0, cap=0, far=None):
    """lod = triangle target of a decimated distance version (LOD1) exported beside the asset; 0 = none.
    lean = segment scale for lathes, sweeps and rounded boxes (1 = as written): small street props seen at 15-120 px
    at play zoom take lean < 1 and bevel 0 to meet the per-category triangle caps (out/review/lod-thresholds.html).
    cap = L0 triangle cap: a collapse decimate (ratio cap / built triangles) on the piece itself, after lean; 0 = none.
    far = a hand-built L2 (fn(shape), same pivot and axes): exported as the L2 instead of the 6% decimation, for signatures whose
    silhouette the collapse destroys (OceanLiner, brief 007 round 3)."""
    def deco(fn):
        REGISTRY[name] = dict(cat=cat, fn=fn, bevel=bevel, lod=lod, lean=lean, cap=cap, far=far)
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
    spec["fn"](s)
    obj = s.build(col, bevel=spec["bevel"])
    obj["dk_cat"] = spec["cat"]
    if spec.get("cap"):
        _cap(obj, spec["cap"])
    return obj


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


def build_far(name, col=None):
    """The hand-built L2 of an asset with far=, named DK_<name>_L2 (no bevel: it is seen at a few dozen pixels)."""
    spec = REGISTRY[name]
    s = Shape(f"DK_{name}_L2")
    spec["far"](s)
    return s.build(col, bevel=0.0)
