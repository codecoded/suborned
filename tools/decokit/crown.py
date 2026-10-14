"""Crown gold (Sky, 14 Oct 2026): DK_Gold and DK_Gold17 turned brass at street level and on facades; the pieces seen mainly against the sky keep their gold, as their own
material at today's value (DK_Crown_Gold #E3D681, DK_Crown_Gold17 #C9C08E; mats.py), until a far shot of both decides. DK_Far_Gold (the horizon) was never touched.

Which pieces: the test is "seen from the street or a deck at eye level (brass) or mainly against the sky (crown gold)".
  crown   category Crowns (atop a tower), Highrise (the 76 to 187 m towers: their gold is the crown, spire and rings), Skyline (the 26 to 44 m facade bays of the skyline
          blocks and the harbour-hotel saucer: kit tag `skyline`), Horizon; and the landmarks 60 m or taller (SPACEHQ, Skyport tower and portal, the liner dock, the parabolic arch)
  brass   everything else: Facades (the street-level frontage bays), street furniture and signs, the fountain and planters, the robot, the palms, the monument and the
          gate at the circus and the parade (28 m, seen from the plaza), the sky-rail station (a playable deck), the sky pad, the ocean liner at its quay (36 m)
The list is data: move a piece between the two here and rebuild it. The swap runs on the built object, so the generators keep saying pal.Gold."""

SWAP = {"DK_Gold": "DK_Crown_Gold", "DK_Gold17": "DK_Crown_Gold17"}
CROWN_CATS = {"Crowns", "Highrise", "Skyline", "Horizon"}
CROWN_LANDMARKS = {"SpaceHQ_Portal", "Skyport_Tower", "Skyport_Tower_Portal", "LinerDock", "LinerDock@liner-berth", "ParabolicArch"}


def is_crown(name, cat):
    return cat in CROWN_CATS or name in CROWN_LANDMARKS


def swap(obj, name, cat):
    """Crown pieces: every slot drawing DK_Gold / DK_Gold17 takes the crown material of the same colour."""
    if not is_crown(name, cat):
        return obj
    import mats
    for o in [obj, *obj.children_recursive]:
        for sl in getattr(o, "material_slots", ()):
            if sl.material and sl.material.name in SWAP:
                sl.material = mats.get(SWAP[sl.material.name])
    return obj
