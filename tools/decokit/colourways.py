"""Building colourways: the old town's own (brief 075, Sky 2026-10-29: "our red is too red - we changed it when not much in the game, but now it needs to go back
to old town"). No bpy.

This is the old Editor/DecoKit/DecoKitColourways.cs scheme the old reel ran (DecoKitStreetDemo.Colour = "plan+livery+soft"), exactly. Colour goes by ensemble,
never building by building: walls stay within a breath of cream, the strong colour runs along the horizontal lines (the shop fascia, the sill bands).
It replaces 074's 12 wall families (cream ... seagreen), 074-H's wall softening and neutral panel sheet, and 075's old* high-street additions.

  key           wall (DK_Limestone_Tex, DK_Render_White)   fascia (DK_Enamel_Teal)   sills (DK_Enamel_Seafoam)   trim (DK_Enamel_Ivory)   old source
  boulevard     the kit's                                  #C8553D                   the kit's                   the kit's                Boulevard: cream spine, one coral ribbon both sides
  circus        #EBDCB5                                    #1F6C67                   #C8553D                     the kit's                Circus: Naples-cream ring, the spine's accents inverted
  harbour       #E2E8E0                                    #1F7F7A                   #1F7F7A                     the kit's                Harbour: sea-glass cream, teal
  gardens       #E8D6C4                                    #B9653F                   #8FA88A                     the kit's                Gardens: sand-rose, terracotta, sage
  southside     #EEE3C9                                    #D9962B                   #2F7E86                     the kit's                Southside: warm white, marigold, petrol teal
  soft-apricot  #E8B793                                    #C8553D                   the kit's                   Lighter(wall)            Soft[0] on a Boulevard building
  soft-celadon  #B7CDB4                                    #C8553D                   the kit's                   Lighter(wall)            Soft[1]
  soft-rose     #DDA596                                    #C8553D                   the kit's                   Lighter(wall)            Soft[2]
  soft-ochre    #E2C07A                                    #C8553D                   the kit's                   Lighter(wall)            Soft[3]

`civic` and `base` keep the kit's materials. The plinth (DK_Stone_Dark) is the kit's everywhere, as the old rule. Lighter = halfway in linear from the wall to the
kit's ivory #EFE8D6 (old DecoKitColourways.Lighter). A slot a key leaves alone has no entry in the mapping, so the piece draws its kit material there.

Each variant is the old DecoKitColourways.Variant: the base .mat's own text (shader, keywords, textures, detail pair, smoothness, instancing flag) with only m_Name,
_BaseColor and _Color changed; a textured base (DK_Limestone_Tex on the old facade_panel_v1 sheet) is tinted so the sheet's mean lands on the colour
(tint = linear(hex) / linear(the sheet's sRGB mean, every 7th texel), clamped at 1), which reproduces the old Paint/<base>_<HEX>.mat _BaseColor (reported per
material as oldDelta in Data/colourways.json). DK_Limestone_Tex itself is the legacy .mat again (the old sheet, white tint): no neutral sheet.
Same shader + same keywords = no new variant and no new SetPass. Names: <base>__<key>; guids derived from the name.

  python3 tools/decokit/colourways.py [out_dir]   removes 074-B's neutral sheet from out/decokit-ground (run.ts runs it before Blender)
  unity_mats.write calls write_mats(): the colourway .mats and Data/colourways.json (the mapping Latch reads through contracts/world/kit.json `colourways`).
Deterministic: no clock, no randomness.
"""

import json
import os
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
GROUND = Path(os.environ.get("DECOKIT_GROUND") or REPO / "out/decokit-ground")
RETIRED_TEX = ("wall_panel_neutral_v1.png", "wall_panel_neutral_v1.png.meta", "colourways-tex.json")  # 074-B's neutral sheet: gone with the softening

IVORY = "#EFE8D6"  # the kit's DK_Enamel_Ivory (old Lighter's target)
BOULEVARD_FASCIA = "#C8553D"
ENSEMBLES = {  # old DecoKitColourways Ensemble(wall, fascia, sills); None = the kit's own colour
    "boulevard": (None, "#C8553D", None),
    "circus": ("#EBDCB5", "#1F6C67", "#C8553D"),
    "harbour": ("#E2E8E0", "#1F7F7A", "#1F7F7A"),
    "gardens": ("#E8D6C4", "#B9653F", "#8FA88A"),
    "southside": ("#EEE3C9", "#D9962B", "#2F7E86"),
}
SOFT = {"soft-apricot": "#E8B793", "soft-celadon": "#B7CDB4", "soft-rose": "#DDA596", "soft-ochre": "#E2C07A"}  # old Soft, in its order
FAMILIES = tuple(ENSEMBLES) + tuple(SOFT)
SLOTS = {"DK_Limestone_Tex": "wall", "DK_Render_White": "wall", "DK_Enamel_Teal": "fascia", "DK_Enamel_Seafoam": "sills", "DK_Enamel_Ivory": "trim"}  # old Walls, Fascia, Sills, Trim
# what earlier runs wrote and this one must remove (074-B/H's 12 families and slots, 075's old* high street): nothing stale is catalogued or copied
STALE_FAMILIES = ("cream", "peach", "mint", "sky", "sand", "ivory", "blush", "powder", "butter", "lilac", "apricot", "seagreen", "oldapricot", "oldceladon", "oldrose", "oldochre")
STALE_BASES = ("DK_Limestone_Tex", "DK_Render_White", "DK_Stone_Dark", "DK_Enamel_Seafoam", "DK_Enamel_Teal", "DK_Enamel_Ivory")
OLD_SOURCE = "~/Projects/the-suborned/game/Assets/TheSuborned/Editor/DecoKit/DecoKitColourways.cs (scheme plan+livery+soft, DecoKitStreetDemo.Colour)"


# ------------------------------------------------------------------ colour maths (sRGB)
def lin(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def gam(c):
    c = min(max(c, 0.0), 1.0)
    return c * 12.92 if c <= 0.0031308 else 1.055 * c ** (1 / 2.4) - 0.055


def hex_rgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) / 255 for i in (0, 2, 4))


def rgb_hex(c):
    return "#%02X%02X%02X" % tuple(round(min(max(x, 0.0), 1.0) * 255) for x in c)


def lighter(h):
    """Old DecoKitColourways.Lighter: halfway (linear) from the wall colour to the kit's ivory."""
    return rgb_hex([gam((lin(a) + lin(b)) * 0.5) for a, b in zip(hex_rgb(h), hex_rgb(IVORY))])


def family_colours():
    """key -> {slot: sRGB hex} for the slots the key paints (a slot it leaves to the kit is absent)."""
    out = {}
    for k, (w, f, s) in ENSEMBLES.items():
        out[k] = {n: v for n, v in (("wall", w), ("fascia", f), ("sills", s)) if v}
    for k, w in SOFT.items():
        out[k] = dict(wall=w, fascia=BOULEVARD_FASCIA, trim=lighter(w))
    return out


# ------------------------------------------------------------------ the .mat set (called by unity_mats.write)
def _f(x):
    return f"{x:.8g}"


def _recolour(text, name, rgb):
    """`text` (a base .mat) with only its name and base colour changed."""
    col = f"{{r: {_f(rgb[0])}, g: {_f(rgb[1])}, b: {_f(rgb[2])}, a: 1}}"
    text = re.sub(r"(  m_Name: )\S+", rf"\g<1>{name}", text, count=1)
    text, n = re.subn(r"(    - _BaseColor: )\{[^}]*\}", rf"\g<1>{col}", text)
    assert n == 1, f"{name}: no _BaseColor"
    return re.sub(r"(    - _Color: )\{[^}]*\}", rf"\g<1>{col}", text)


def _old_paint(legacy, base, hx):
    """The old Paint/<base>_<HEX>.mat's _BaseColor (the old DecoKitColourways output), or None."""
    p = legacy.parent / "Generated" / "Paint" / f"{base}_{hx.lstrip('#').upper()}.mat"
    if not p.exists():
        return None
    m = re.search(r"    - _BaseColor: \{r: ([\d.e-]+), g: ([\d.e-]+), b: ([\d.e-]+)", p.read_text())
    return [float(x) for x in m.groups()] if m else None


def write_mats(dst, legacy, meta_text):
    """<base>__<key>.mat (+ .meta) for every slot a key paints, from the base .mat as written (generated) or the legacy one, and Data/colourways.json;
    returns the names this wrote."""
    sys.path.insert(0, str(Path(__file__).parent))
    import livery  # the old Variant's texture tint (livery.tint: linear hex / linear sRGB mean of the base map, every 7th texel)
    d = Path(dst) / "Materials"
    d.mkdir(parents=True, exist_ok=True)
    for b in STALE_BASES:  # earlier runs' sets (and 074-B's generated DK_Limestone_Tex on the neutral sheet): gone, so the legacy .mat is the kit's again
        for f in STALE_FAMILIES:
            for x in (d / f"{b}__{f}.mat", d / f"{b}__{f}.mat.meta"):
                x.unlink(missing_ok=True)
    if (legacy / "DK_Limestone_Tex.mat").exists():
        for x in (d / "DK_Limestone_Tex.mat", d / "DK_Limestone_Tex.mat.meta"):
            x.unlink(missing_ok=True)
    roots = [Path(dst) / "Textures", legacy.parent / "Textures"]
    fam = family_colours()
    out, mapping, check = [], {}, {}
    for base, slot in SLOTS.items():
        src = d / f"{base}.mat" if (d / f"{base}.mat").exists() else legacy / f"{base}.mat"
        text = src.read_text()
        for f in FAMILIES:
            hx = fam[f].get(slot)
            if not hx:
                continue
            name = f"{base}__{f}"
            rgb, tex = livery.tint(text, hx, roots)
            (d / f"{name}.mat").write_text(_recolour(text, name, rgb))
            (d / f"{name}.mat.meta").write_text(meta_text(name, legacy))
            mapping.setdefault(base, {})[f] = name
            out.append(name)
            old = _old_paint(legacy, base, hx)
            check[name] = dict(hex=hx, baseFrom="generated" if src.parent == d else "legacy", baseColor=[round(x, 6) for x in rgb], **({"tintedOver": tex} if tex else {}),
                               **({"oldPaint": f"Paint/{base}_{hx.lstrip('#')}.mat", "oldBaseColor": old, "oldDelta": round(max(abs(a - b) for a, b in zip(rgb, old)), 6)} if old else {"oldPaint": None}))
    data = dict(schema="suborned.colourways/1", brief="075 (old palette; was 074-B, 074-H)", source="tools/decokit/colourways.py",
                rule="A building's colourway (export `colourway`, a facade.json family) swaps each base material listed for it below for its variant; a slot not listed, `base` "
                     "and `civic` keep the kit material. Same shader, keywords and textures as the base: only the base colour differs. The old DecoKitColourways scheme "
                     "plan+livery+soft, exactly: five ensembles and the high street's Soft palette.",
                families=list(FAMILIES), keep=["base", "civic"], slots=SLOTS, materials=mapping, colours=fam,
                old=dict(source=OLD_SOURCE, ensembles={k: dict(zip(("wall", "fascia", "sills"), v)) for k, v in ENSEMBLES.items()}, soft=SOFT, ivory=IVORY,
                         lighter="halfway in linear from the wall to the ivory", tint="textured base: linear(hex) / linear(sRGB mean of the base map, every 7th texel), clamped at 1"),
                check=check)
    bad = {n: c["oldDelta"] for n, c in check.items() if c.get("oldDelta", 0) > 1e-5}
    if bad:
        raise SystemExit(f"[colourways] differs from the old Paint .mat: {bad}")
    (Path(dst) / "Data").mkdir(parents=True, exist_ok=True)
    (Path(dst) / "Data" / "colourways.json").write_text(json.dumps(data, indent=1) + "\n")
    print(f"[colourways] {len(out)} materials over {len(FAMILIES)} keys; {sum(1 for c in check.values() if c.get('oldBaseColor'))} matched to the old Paint .mats (max delta "
          f"{max([c.get('oldDelta', 0) for c in check.values()] or [0]):.2g})")
    return out


def clean(out=GROUND):
    """run.ts step: remove 074-B's neutral sheet (no material names it now)."""
    for n in RETIRED_TEX:
        (Path(out) / n).unlink(missing_ok=True)
    print(f"[colourways] old scheme: no neutral sheet ({out})")


if __name__ == "__main__":
    clean(Path(sys.argv[1]) if len(sys.argv) > 1 else GROUND)
