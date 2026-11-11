"""Brief 107 (Vela): the night glows of the civic ground (Glint's weather page: "the seafoam inlay bands and the planter feet as light set in the
ground"; Aster's briefs/107/concepts/night.png). Day is untouched: neither glow draws anything until the night light turns it up.

DK_Inlay_SeafoamNight  the paving inlay's twin (InlayBand_06_2 / _12_2, InlayArc_R2_W06 / R4_W06 / R9_W12): the same ceramic row as DK_Inlay_Seafoam,
                       so the .mat is written the same (base, smoothness, detail pair) with emission black. RoadLight raises its _EmissionColor at
                       dusk (INLAY_NIGHT below). A twin, not the old name, so nothing else that ever draws DK_Inlay_Seafoam lights up with it.
DK_Glow_PlanterFoot    kind `glow` (no .mat is written: Unity's Materials/Glow, Suborned/Cross Glow mode 3 "pool", additive, double-sided, as DK_Road_Pool):
                       a strip at the foot of each civic stone planter, lying on the paving (FOOT_W out from the wall) and up the wall face (FOOT_H),
                       v = 0 on the foot line, 1 at its far edge, so the pool is brightest where wall meets ground. Its own object `<name>_glow`
                       in every level (shadows off). The row's alpha is the NIGHT value; the Unity .mat is written at _Alpha 0 (day draws nothing).
DK_Glow_InlayEdge      kind `glow`: the inlays' edge light (round 2, after Sky turned the flat band glow off), Cross Glow mode 2; see EDGE below.
Built by glows107_extra.py (the planters, an extra root) and civic_paving.py (the inlays and their edges). Renders: review107_glows.py. Notes: briefs/107/vela/glows.md."""

import mats
import civic_common  # noqa: F401  (registers DK_Inlay_Seafoam first: the twin copies its row)
from mats import register

INLAY = register("DK_Inlay_SeafoamNight", *mats.LIB["DK_Inlay_Seafoam"])
FOOT_HEX, FOOT_STRENGTH, FOOT_ALPHA_NIGHT, FOOT_ALPHA_EVENING = "#FFD49A", 2.0, 0.42, 0.25
FOOT = register("DK_Glow_PlanterFoot", "glow", FOOT_HEX, FOOT_STRENGTH, FOOT_ALPHA_NIGHT)

# The inlay's night emission, linear HDR = linear(INLAY_HEX) x level (URP Lit adds _EmissionColor to what the surface reflects).
# Off since 0.2.317 (Sky 2026-11-11: "horrible paving colors at night ... that blue circle": a whole band lit flat read as a decal): the band stays dark
# and only its edges light (EDGE below). The twin material stays, so the band can still be raised a little if Glint wants it.
INLAY_HEX, INLAY_EVENING, INLAY_NIGHT = "#6FE3C8", 0.15, 0.3

# DK_Glow_InlayEdge (brief 107 round 2, Vela): the concept's thin line of light along each long edge of the seafoam bands (briefs/107/concepts/night.png:
# the bands dark, a bright line at their borders). Kind `glow` (no .mat written: Unity's Materials/Glow/DK_Glow_InlayEdge.mat, Suborned/Cross Glow
# _Mode 2 "field": k = (1 - |v| / _Radius) ^ 1.5, additive, double-sided). One flat strip per long edge, from the border EDGE_W into the band,
# v = metres from the border, so the light is brightest ON the band's edge and fades to nothing EDGE_W in. It stays inside the band's outline (the
# piece's footprint, size and pivot unchanged); _Radius must equal EDGE_W. The row's alpha is the NIGHT value; the Unity .mat is written at _Alpha 0 (day draws exactly nothing).
EDGE_HEX, EDGE_STRENGTH, EDGE_ALPHA_NIGHT, EDGE_ALPHA_EVENING = "#B4F5E2", 2.0, 0.6, 0.3
EDGE_W = 0.06      # m from the border into the band: about 3 px at play zoom, its bright core 1 to 2 px
EDGE_LIFT = 0.0015  # m over the inlay's top (4 mm proud of the slab), so it never fights the enamel
EDGE = register("DK_Glow_InlayEdge", "glow", EDGE_HEX, EDGE_STRENGTH, EDGE_ALPHA_NIGHT)

FOOT_W, FOOT_H, LIFT, PROUD = 0.30, 0.12, 0.004, 0.003   # pool width on the paving, wash up the wall, lift off the paving, off the wall face (m)

# The civic stone planters in the extra root (out/decokit-097-extra): the Handover zone draws PlanterRun_4_A/B/C (12), PlanterArc (4, the fountain
# garden) and CafeTerrace (1, the terrace planters); the other runs are built too, so every PlanterRun reads the same at night.
PLANTERS = tuple(f"PlanterRun_{k}_{v}" for k in ("2", "4", "Corner", "End") for v in "ABC") + ("PlanterArc", "CafeTerrace")
# Stone slots: the foot is traced on these faces only (never the leaves or petals that spill over the wall).
STONE = ("DK_Limestone", "DK_Limestone_Tex", "DK_Faience_Teal", "DK_Accent_Gold")
# The companion whose soil closes a planter that has none of its own (CafeTerrace's planting is a separate piece at the same pivot).
COMPANION = {"CafeTerrace": "Models/Plaza/DK_CafeTerrace_Planting.fbx"}
