"""Brief 107 (Vela): the night glows of the civic ground (Glint's weather page: "the seafoam inlay bands and the planter feet as light set in the
ground"; Aster's briefs/107/concepts/night.png). Day is untouched: neither glow draws anything until the night light turns it up.

DK_Inlay_SeafoamNight  the paving inlay's twin (InlayBand_06_2 / _12_2, InlayArc_R2_W06 / R4_W06 / R9_W12): the same ceramic row as DK_Inlay_Seafoam,
                       so the .mat is written the same (base, smoothness, detail pair) with emission black. RoadLight raises its _EmissionColor at
                       dusk (INLAY_NIGHT below). A twin, not the old name, so nothing else that ever draws DK_Inlay_Seafoam lights up with it.
DK_Glow_PlanterFoot    kind `glow` (no .mat is written: Unity's Materials/Glow, Suborned/Cross Glow mode 3 "pool", additive, double-sided, as DK_Road_Pool):
                       a strip at the foot of each civic stone planter, lying on the paving (FOOT_W out from the wall) and up the wall face (FOOT_H),
                       v = 0 on the foot line, 1 at its far edge, so the pool is brightest where wall meets ground. Its own object `<name>_glow`
                       in every level (shadows off). The row's alpha is the NIGHT value; the Unity .mat is written at _Alpha 0 (day draws nothing).
Built by glows107_extra.py (the planters, an extra root) and civic_paving.py (the inlays). Renders: review107_glows.py. Notes: briefs/107/vela/glows.md."""

import mats
import civic_common  # noqa: F401  (registers DK_Inlay_Seafoam first: the twin copies its row)
from mats import register

INLAY = register("DK_Inlay_SeafoamNight", *mats.LIB["DK_Inlay_Seafoam"])
FOOT_HEX, FOOT_STRENGTH, FOOT_ALPHA_NIGHT, FOOT_ALPHA_EVENING = "#FFD49A", 2.0, 0.42, 0.25
FOOT = register("DK_Glow_PlanterFoot", "glow", FOOT_HEX, FOOT_STRENGTH, FOOT_ALPHA_NIGHT)

# The inlay's night emission, linear HDR = linear(INLAY_HEX) x level (URP Lit adds _EmissionColor to what the surface reflects).
INLAY_HEX, INLAY_EVENING, INLAY_NIGHT = "#6FE3C8", 0.15, 0.3

FOOT_W, FOOT_H, LIFT, PROUD = 0.30, 0.12, 0.004, 0.003   # pool width on the paving, wash up the wall, lift off the paving, off the wall face (m)

# The civic stone planters in the extra root (out/decokit-097-extra): the Handover zone draws PlanterRun_4_A/B/C (12), PlanterArc (4, the fountain
# garden) and CafeTerrace (1, the terrace planters); the other runs are built too, so every PlanterRun reads the same at night.
PLANTERS = tuple(f"PlanterRun_{k}_{v}" for k in ("2", "4", "Corner", "End") for v in "ABC") + ("PlanterArc", "CafeTerrace")
# Stone slots: the foot is traced on these faces only (never the leaves or petals that spill over the wall).
STONE = ("DK_Limestone", "DK_Limestone_Tex", "DK_Faience_Teal", "DK_Accent_Gold")
# The companion whose soil closes a planter that has none of its own (CafeTerrace's planting is a separate piece at the same pivot).
COMPANION = {"CafeTerrace": "Models/Plaza/DK_CafeTerrace_Planting.fbx"}
