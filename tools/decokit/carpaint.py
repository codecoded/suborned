"""1950s car paints for presentation traffic (brief 075 district roll-out, step 1, Vela; for Pulse's presentation life and Latch's LifeInstall). No bpy.

The kit sedans (cars070.py: Sedan_Official deep teal over an ivory roof, Sedan_Getaway coral over seafoam) are the mission's cars; traffic drawn from the same body
needs other colours, or every car in the street reads as Vale's or the cell's. This writes a palette of car-paint variants, the same way as the colourways and the
livery: each is DK_CarEnamel_Ivory.mat's own text (shader, keywords, the car_tex.py flake / film / orange-peel detail pair, clear coat, smoothness) with only m_Name,
_BaseColor and _Color changed, so no new shader variant and no new SetPass. Base colour only (Aster's brief).

A paint key = a body colour + a roof colour. The sedan has two paint slots (cars070.sedan_v2): `body` (the tub, nose, tail) and `roof` (the hardtop, the A and C posts and
the flank flash). Per piece, the slot's base material: Sedan_Official body DK_CarEnamel_Teal, roof DK_CarEnamel_Ivory; Sedan_Getaway body DK_CarEnamel_Coral, roof
DK_CarEnamel_Seafoam. A traffic car swaps body -> keys[k].body and roof -> keys[k].roof; chrome, glass, lamps and whitewalls stay. No key repeats the official's or the
getaway's own pair (teal / ivory, coral / seafoam), so a mission car still reads as itself.

Colours (brief 075 old palette, Sky 2026-10-29: "back to old town"; Aster: "the old reel's car colours"): the old reel's, exactly. The old DecoKitCitizens.Car kept every
car body the RetroFutureMobility glTFs' own "polished warm ivory" (#F9F0D7, baseColorFactor) and painted its accent panels ("turquoise enamel") one of eight enamels,
seeded per car: #0A6D82 #E25E47 #E7B84C #78C4A2 #B53A31 #5F9FC6 #27406A #2F7D5C. Here the body is that ivory and the roof slot (the hardtop, posts and flank flash: the
sedan's accent panels) the enamel. Replaces the roll-out's 11 two-tones (Tropical Turquoise ... Onyx). Names: DK_CarPaint_<Colour> (+ .meta, guid from the name);
Data/carpaint.json is the mapping the catalogue copies into contracts/world/kit.json `carPaints`. Deterministic.
"""

import json
from pathlib import Path

BASE = "DK_CarEnamel_Ivory"
OLD_SOURCE = "~/Projects/the-suborned/game/Assets/TheSuborned/Editor/DecoKit/DecoKitCitizens.cs (Enamels; bodies: RFM_* glTF 'polished warm ivory')"
COLOURS = {  # material suffix -> sRGB hex: the old body ivory and DecoKitCitizens.Enamels in their order
    "Ivory": "#F9F0D7", "Turquoise": "#0A6D82", "Tomato": "#E25E47", "Canary": "#E7B84C", "Seafoam": "#78C4A2", "Cherry": "#B53A31", "Sky": "#5F9FC6",
    "Navy": "#27406A", "Green": "#2F7D5C",
}
KEYS = {  # key -> (body, roof, family): one per old enamel, over the old ivory body
    "ivory-turquoise": ("Ivory", "Turquoise", "old"), "ivory-tomato": ("Ivory", "Tomato", "old"), "ivory-canary": ("Ivory", "Canary", "old"),
    "ivory-seafoam": ("Ivory", "Seafoam", "old"), "ivory-cherry": ("Ivory", "Cherry", "old"), "ivory-sky": ("Ivory", "Sky", "old"),
    "ivory-navy": ("Ivory", "Navy", "old"), "ivory-green": ("Ivory", "Green", "old"),
}
STALE = ("Cream", "Larkspur", "DuskPink", "Canary", "Orchid", "MatadorRed", "HarborBlue", "Tangerine", "Onyx")  # the roll-out's paints this replaces (Turquoise, Seafoam, Canary reused)
SLOTS = {"Sedan_Official": {"body": "DK_CarEnamel_Teal", "roof": "DK_CarEnamel_Ivory"}, "Sedan_Getaway": {"body": "DK_CarEnamel_Coral", "roof": "DK_CarEnamel_Seafoam"}}


def hex_rgb(h):
    h = h.lstrip("#")
    return [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)]


def write_mats(dst, legacy, meta_text, recolour):
    d = Path(dst) / "Materials"
    d.mkdir(parents=True, exist_ok=True)
    own = d / f"{BASE}.mat"
    if not own.exists():
        raise SystemExit(f"[carpaint] {own} missing: the car enamels are written first (unity_mats robot/car detail rows)")
    text = own.read_text()
    for c in STALE:  # an earlier run's paints: gone, so nothing stale is catalogued or copied
        if c not in COLOURS:
            for x in (d / f"DK_CarPaint_{c}.mat", d / f"DK_CarPaint_{c}.mat.meta"):
                x.unlink(missing_ok=True)
    out = []
    for c, hx in COLOURS.items():
        name = f"DK_CarPaint_{c}"
        (d / f"{name}.mat").write_text(recolour(text, name, hex_rgb(hx)))
        (d / f"{name}.mat.meta").write_text(meta_text(name, legacy))
        out.append(name)
    keys = {k: dict(body=f"DK_CarPaint_{b}", roof=f"DK_CarPaint_{r}", bodyHex=COLOURS[b], roofHex=COLOURS[r], family=fam) for k, (b, r, fam) in KEYS.items()}
    data = dict(schema="suborned.carpaint/1", brief="075 roll-out; old palette", oldSource=OLD_SOURCE, source="tools/decokit/carpaint.py", base=BASE,
                rule="A traffic sedan swaps its piece's `body` slot material for keys[k].body and its `roof` slot material for keys[k].roof; everything else stays. "
                     "Same shader, keywords and detail pair as DK_CarEnamel_Ivory: only the base colour differs. Presentation only (Pulse mixes, Latch swaps).",
                slots=SLOTS, keys=keys, colours={f"DK_CarPaint_{c}": hx for c, hx in COLOURS.items()})
    (Path(dst) / "Data").mkdir(parents=True, exist_ok=True)
    (Path(dst) / "Data" / "carpaint.json").write_text(json.dumps(data, indent=1) + "\n")
    print(f"[carpaint] {len(out)} paints, {len(keys)} keys -> {d}")
    return out
