"""Brief 026 (Sky, 2026-10-20: "old models were all better"): the detail the brief 007 round 3 caps and the brief 020 / 008 slim copies took out, put back. Loaded last, so it overrides.
Why they were poor: kit.asset(lean=, cap=) cut segments and then collapse-decimated the piece to a triangle target (an octagon hydrant, flat-shaded robots); the slim copies
(FanPalm_Play, Planter_Urn_Play, CafeSet_Tulip_Play, PottedTopiary_Play, Cypress_Play, PlazaSphereLamp_Play) were redrawn with fewer cards and sides. The ids stay, so slots,
tags and kit refs do not move; each is now the original's builder, uncapped.
Placed in the hundreds (Bollard 352, RecyclingBins 284, FanPalm_Play 195): Bollard too (20 Oct, Sky: compression goes through Sky; it had lean 0.6 for 1.4k triangles); the others come back whole (LODs carry the distance).
PneumaticPost_Play (the old post in a 0.6 m footprint) and Hydrant are changed at their source (street020.py, street.py)."""

from kit import REGISTRY

ORIGINALS = {"FanPalm_Play": "FanPalm", "Planter_Urn_Play": "Planter_Urn", "CafeSet_Tulip_Play": "CafeSet_Tulip", "PottedTopiary_Play": "PottedTopiary",
             "Cypress_Play": "Cypress", "PlazaSphereLamp_Play": "PlazaSphereLamp"}
UNCAP = ("RobotPorter_Play", "ServiceRobot_Play", "RobotWaiter_Play", "RobotButler_Play", "RobotNanny_Play", "RobotShopkeeper_Play", "BoxTruck_Play", "CoffeeCart_Play",
         "BusShelter_Play", "CurvedKiosk_Play", "TramCanopy_Play", "RecyclingBins", "CCTVPole", "DomeCamera")
LEAN = {"Bollard": 1.0, "CCTVPole": 1.0, "DomeCamera": 1.0}

for play, base in ORIGINALS.items():
    if play in REGISTRY and base in REGISTRY:
        spec = dict(REGISTRY[base])
        spec.update(lean=1.0, cap=0)
        REGISTRY[play] = spec
for n in UNCAP:
    if n in REGISTRY:
        REGISTRY[n]["cap"] = 0
        REGISTRY[n]["lean"] = LEAN.get(n, 1.0)
if "Bollard" in REGISTRY:
    REGISTRY["Bollard"].update(lean=LEAN["Bollard"], cap=0)


# 20 Oct (Sky: "old models please"): the old twin-pod PhoneBooth and the old CurvedKiosk are the booth and the newsstand. Their ids PhoneBooth and CurvedKiosk are the full pieces (2.6 m, 6.1 m);
# the two slot ids that hold the plan's narrower boxes are the SAME designs, not new ones: PhoneBooth_Single = the pod with its seafoam hood alone on the coral drum, Newsstand = the
# curved kiosk at 0.46 (3.0 m wide, 2.0 deep, 3.0 m to the fin; the old city placed it scaled 0.43 as CurvedKiosk_Play did).
from kit import asset  # noqa: E402
from booths import booth_body  # noqa: E402
from structures import curved_kiosk  # noqa: E402


@asset("PhoneBooth_Single", "Booths", bevel=0.0)
def phone_booth_single(s):
    booth_body(s, single=True)


@asset("Newsstand", "Structures", bevel=0.012)
def newsstand_curved(s):
    with s.at((0, 0, 0), scale=0.46):
        curved_kiosk(s)
