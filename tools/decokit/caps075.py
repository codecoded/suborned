"""Brief 075, old buildings (Sky 2026-10-28: "where's the rounded corners etc - why can't we just use the old buildings ?"): the caps the old frontage
system (old Editor/DecoKit/DecoKitFrontage.cs) needs that our export cannot make.

The old placer put every end and corner with `mirror:` (a -1 X scale on the instance) whenever the round had to fall on the module's -X side: an end that
closes the head of a run (Run, last == End), the tail-side ends of two of the four circus crescents, and the corner at the head of every run. Our world
export has no mirror (briefs/075/notes.md, Splice 075-E), so each such cap gets an explicit mirrored twin `<id>_M`: the same builder under a -X scale about
the pivot (geo.Shape.at flips the winding), so it is the old instance exactly, its kit frame mirrored. Pivot, materials, category (so crown gold as the base),
LOD build (decimated, or the base's hand-built near / far, mirrored too) all follow the base. Declared collision volumes are mirrored in x.

Kit frame (export_kit: kit x = -Blender x): the base rounds toward kit +X; `_M` rounds toward kit -X. pivotOffset x changes sign (StreamEnd_6 0.81 -> -0.81).

Also the two ends the old class table asked for and fell back from (old DecoKitFrontage.Of: "fallbacks, so the plan builds before the new facade family
lands"): PortholeEnd_7 and BalconyEnd_8, skyline.py's stream_end at the bays' own storey counts and bodies, so a P7 or B8 run ends at its own height (the
fallbacks StreamEnd_6 / StreamEnd_9W stand 5.8 m under / 3.4 m over the bay). ArcadeEnd_3 is not built: the old placed A3 only mid-run (Southside S3/A3/S3)
or after a corner (HallCornerWing, which the old table names as its end and corner).
"""

from kit import REGISTRY, asset
import facades  # noqa: F401  (registers DecoEnd_10, DecoCorner_10, StreamCorner_6)
import facades_tower  # noqa: F401  (CornerTower_12)
import highrise  # noqa: F401  (StreamEnd_4, StreamCorner_4)
import lowrise  # noqa: F401  (StreamEnd_2 / _3 / _3W, StreamCorner_2 / _3)
import plaza  # noqa: F401  (HallCornerWing)
import skyline  # noqa: F401  (StreamEnd_6 / _9 / _9W, StreamCorner_9)
import streamstack  # noqa: F401  (StreamEndStack_*, StreamCornerStack_*)
from skyline import stream_end

# the intended ends (old table: P7 -> PortholeEnd_7, B8 -> BalconyEnd_8): the bays' bodies (PortholeBay_7 white render; BalconyBay_8 limestone)
asset("PortholeEnd_7", "Facades", bevel=0.0)(stream_end(7, "Render_White"))
asset("BalconyEnd_8", "Facades", bevel=0.0)(stream_end(8, "Limestone_Tex"))

ENDS = ["StreamEnd_2", "StreamEnd_3", "StreamEnd_3W", "StreamEnd_4", "StreamEnd_6", "StreamEnd_9", "StreamEnd_9W", "DecoEnd_10",
        "PortholeEnd_7", "BalconyEnd_8", "StreamEndStack_Base", "StreamEndStack_Mid", "StreamEndStack_Top"]
CORNERS = ["StreamCorner_2", "StreamCorner_3", "StreamCorner_4", "StreamCorner_6", "StreamCorner_9", "DecoCorner_10", "HallCornerWing", "CornerTower_12",
           "StreamCornerStack_Base", "StreamCornerStack_Mid", "StreamCornerStack_Top"]
MIRRORED = ENDS + CORNERS


def mirrored(fn):
    """fn under a -X scale about the pivot; volumes it declares (builder frame, outside s.at) mirrored after."""
    if fn is None:
        return None

    def build(s):
        n0 = len(s.volumes)
        with s.at(scale=(-1.0, 1.0, 1.0)):
            fn(s)
        for v in s.volumes[n0:]:
            v["min"][0], v["max"][0] = -v["max"][0], -v["min"][0]
    return build


for _id in MIRRORED:
    _b = REGISTRY[_id]
    asset(f"{_id}_M", _b["cat"], bevel=_b["bevel"], lod=_b["lod"], lean=_b["lean"], cap=_b["cap"], near=mirrored(_b["near"]), far=mirrored(_b["far"]),
          floor=_b["floor"])(mirrored(_b["fn"]))
