"""Play-zoom slim variants of kit candidates (brief 020 C), as brief 008 did for CafeSet_Tulip_Play and Cypress_Play: the same builder as the base piece with an L0 triangle
cap (kit.asset cap=, one collapse decimate), so the shape and materials are the piece's own. At play zoom a person is 40 to 60 px, a robot or a truck about the same;
the base pieces are 10 to 30k triangles, and the street category's LOD1 starts below 0.8 % of the screen height, which a robot never reaches, so without a cap every one is drawn whole.
Names: <Base>_Play. Same pivot, size and parts as the base."""

from kit import REGISTRY, asset

CAPS = {"RobotPorter": 2600, "ServiceRobot": 1800, "RobotWaiter": 2200, "RobotButler": 2600, "BoxTruck": 3600, "CoffeeCart": 3000, "BusShelter": 2800, "CurvedKiosk": 3200, "TramCanopy": 3000}


def _register():
    for base, cap in CAPS.items():
        spec = REGISTRY.get(base)
        if spec is None:  # the family of the base failed to import: reported by kit.load, nothing to alias
            continue
        asset(base + "_Play", spec["cat"], bevel=spec["bevel"], cap=cap)(spec["fn"])


_register()
