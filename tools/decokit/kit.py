"""Deco Kit asset registry. Family modules register builders with @asset; `build` makes one object.

Conventions: metres, Z up, pivot = footprint centre at z = 0, front faces -Y (Blender front view).
"""

import importlib

from geo import Shape

REGISTRY = {}
FAMILIES = ("street", "signals", "booths", "ground", "vehicles", "structures", "plants", "plaza", "paving", "skyline", "landmarks", "rooftop", "skycraft", "facades", "lowrise", "roofdeck", "highrise", "alleys", "furnish", "signage", "skyport", "mobility", "robots", "calib", "regen")


def asset(name, cat, bevel=0.005, lod=0):
    """lod = triangle target of a decimated distance version (LOD1) exported beside the asset; 0 = none."""
    def deco(fn):
        REGISTRY[name] = dict(cat=cat, fn=fn, bevel=bevel, lod=lod)
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
    spec["fn"](s)
    obj = s.build(col, bevel=spec["bevel"])
    obj["dk_cat"] = spec["cat"]
    return obj
