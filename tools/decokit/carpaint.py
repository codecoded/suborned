"""1950s car paints for presentation traffic (brief 075 district roll-out, step 1, Vela; for Pulse's presentation life and Latch's LifeInstall). No bpy.

The kit sedans (cars070.py: Sedan_Official deep teal over an ivory roof, Sedan_Getaway coral over seafoam) are the mission's cars; traffic drawn from the same body
needs other colours, or every car in the street reads as Vale's or the cell's. This writes a palette of car-paint variants, the same way as the colourways and the
livery: each is DK_CarEnamel_Ivory.mat's own text (shader, keywords, the car_tex.py flake / film / orange-peel detail pair, clear coat, smoothness) with only m_Name,
_BaseColor and _Color changed, so no new shader variant and no new SetPass. Base colour only (Aster's brief).

A paint key = a body colour + a roof colour. The sedan has two paint slots (cars070.sedan_v2): `body` (the tub, nose, tail) and `roof` (the hardtop, the A and C posts and
the flank flash). Per piece, the slot's base material: Sedan_Official body DK_CarEnamel_Teal, roof DK_CarEnamel_Ivory; Sedan_Getaway body DK_CarEnamel_Coral, roof
DK_CarEnamel_Seafoam. A traffic car swaps body -> keys[k].body and roof -> keys[k].roof; chrome, glass, lamps and whitewalls stay. No key repeats the official's or the
getaway's own pair (teal / ivory, coral / seafoam), so a mission car still reads as itself.

Colours: the period's two-tone pastels and saturated enamels (Tropical Turquoise, Larkspur Blue, Dusk Pink, Seafoam, Canary, Orchid; Matador Red, Harbor Blue, Tangerine,
Onyx) under a cream roof, plus one reversed (cream body, turquoise roof). No gold or brass body (brass stays a line). Names: DK_CarPaint_<Colour> (+ .meta, guid from the
name); Data/carpaint.json is the mapping the catalogue copies into contracts/world/kit.json `carPaints`. Deterministic.
"""

import json
from pathlib import Path

BASE = "DK_CarEnamel_Ivory"
COLOURS = {  # material suffix -> sRGB hex
    "Cream": "#F2E8CF", "Turquoise": "#3FA9A5", "Larkspur": "#7FA7D3", "DuskPink": "#E8A3AE", "Seafoam": "#9CCFB4", "Canary": "#F0CF6A",
    "Orchid": "#B5A2D4", "MatadorRed": "#B3262C", "HarborBlue": "#2D57A3", "Tangerine": "#E0773A", "Onyx": "#24272B",
}
KEYS = {  # key -> (body, roof, family)
    "tropical-turquoise": ("Turquoise", "Cream", "pastel"), "larkspur-blue": ("Larkspur", "Cream", "pastel"), "dusk-pink": ("DuskPink", "Cream", "pastel"),
    "seafoam-green": ("Seafoam", "Cream", "pastel"), "canary-yellow": ("Canary", "Cream", "pastel"), "orchid": ("Orchid", "Cream", "pastel"),
    "matador-red": ("MatadorRed", "Cream", "saturated"), "harbor-blue": ("HarborBlue", "Cream", "saturated"), "tangerine": ("Tangerine", "Cream", "saturated"),
    "onyx": ("Onyx", "Cream", "saturated"), "cream-turquoise": ("Cream", "Turquoise", "reversed"),
}
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
    out = []
    for c, hx in COLOURS.items():
        name = f"DK_CarPaint_{c}"
        (d / f"{name}.mat").write_text(recolour(text, name, hex_rgb(hx)))
        (d / f"{name}.mat.meta").write_text(meta_text(name, legacy))
        out.append(name)
    keys = {k: dict(body=f"DK_CarPaint_{b}", roof=f"DK_CarPaint_{r}", bodyHex=COLOURS[b], roofHex=COLOURS[r], family=fam) for k, (b, r, fam) in KEYS.items()}
    data = dict(schema="suborned.carpaint/1", brief="075 roll-out", source="tools/decokit/carpaint.py", base=BASE,
                rule="A traffic sedan swaps its piece's `body` slot material for keys[k].body and its `roof` slot material for keys[k].roof; everything else stays. "
                     "Same shader, keywords and detail pair as DK_CarEnamel_Ivory: only the base colour differs. Presentation only (Pulse mixes, Latch swaps).",
                slots=SLOTS, keys=keys, colours={f"DK_CarPaint_{c}": hx for c, hx in COLOURS.items()})
    (Path(dst) / "Data").mkdir(parents=True, exist_ok=True)
    (Path(dst) / "Data" / "carpaint.json").write_text(json.dumps(data, indent=1) + "\n")
    print(f"[carpaint] {len(out)} paints, {len(keys)} keys -> {d}")
    return out
