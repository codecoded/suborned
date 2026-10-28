"""Landmark livery (brief 075-C step 2, Vela; rows C2, C4, C5 of briefs/075/designs.md). No bpy.

The old DecoKit city painted its landmarks over the kit materials (the old Editor/DecoKit/DecoKitColourways.cs, `livery` layer; its output is the old
Art/DecoKit/Generated/Paint/ set). Ours kept the kit's ivory, so the sphere, the lattices and SpaceHQ read ivory. This ports that livery as tinted
material variants, the same way as 074's colourways: each variant is its base .mat's own text (shader, keywords, textures, detail pair, smoothness,
metallic, instancing flag) with only m_Name, _BaseColor and _Color changed. Same shader + same keywords = no new variant, no new SetPass.

  key        base material     hex       old source                                           pieces (the old rule)
  vermilion  DK_Steel_Ivory    #B8432E   `Vermilion` (Paint/DK_Steel_Ivory_B8432E.mat)        every piece in kit categories Landmarks, Skyport, Sky (lattices, trusses, gantries, masts)
  brick      DK_Hull_Ivory     #9C2E2B   `Signature` SphereStation (Paint/DK_Hull_Ivory_9C2E2B) SphereStation (the hull)
  gold       DK_Crown_Gold     #F2C14E   `Signature` SpaceHQ on DK_Gold (Paint/DK_Gold_F2C14E_m100s45) SpaceHQ_Portal (ribs, beads, orbits, crown: pal.Gold there is DK_Crown_Gold)

A textured base (DK_Hull_Ivory on hull_panels_v1) is tinted the old way (DecoKitColourways.Variant): tint = linear(hex) / linear(the texture's sRGB mean,
every 7th texel), so the texture's mean lands on the hex; this reproduces the old Paint .mat's _BaseColor. The old gold also set metallic 1 and
smoothness 0.45; here only the base colour changes (DK_Crown_Gold keeps metallic 1, smoothness 0.7): a smoothness change is Sky's/Glint's call.

Names: <base>__<key> (DK_Steel_Ivory__vermilion); guids derived from the name. Data/livery.json is the mapping the catalogue copies into
contracts/world/kit.json `landmarks` (Latch swaps per instance; Splice's export table briefs/075/landmarks.json keys its slots on the same keys).
unity_mats.write calls write_mats() after the colourways. Deterministic: no clock, no randomness.
"""

import json
import re
from pathlib import Path

LIVERY = {  # key -> (base material, sRGB hex, the old source, pieces: a list of ids or {"categories": [...]})
    "vermilion": ("DK_Steel_Ivory", "#B8432E", "DecoKitColourways.Vermilion (Paint/DK_Steel_Ivory_B8432E.mat)", {"categories": ["Landmarks", "Skyport", "Sky"]}),
    "brick": ("DK_Hull_Ivory", "#9C2E2B", "DecoKitColourways.Signature[SphereStation] (Paint/DK_Hull_Ivory_9C2E2B.mat)", ["SphereStation"]),
    "gold": ("DK_Crown_Gold", "#F2C14E", "DecoKitColourways.Signature[SpaceHQ] on DK_Gold, metallic 1 smoothness 0.45 (Paint/DK_Gold_F2C14E_m100s45.mat); base colour only here", ["SpaceHQ_Portal", "DiscTower@crown-vista"]),  # brief 075 golden: the Crown Vista tower's gold shaft (vista075.py)
}
OLD_PAINT = {"vermilion": "DK_Steel_Ivory_B8432E", "brick": "DK_Hull_Ivory_9C2E2B", "gold": "DK_Gold_F2C14E_m100s45"}


def lin(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def gam(c):
    c = min(max(c, 0.0), 1.0)
    return c * 12.92 if c <= 0.0031308 else 1.055 * c ** (1 / 2.4) - 0.055


def hex_rgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) / 255 for i in (0, 2, 4))


def _base_map_guid(text):
    m = re.search(r"    - _BaseMap:\n\s+m_Texture: \{fileID: 2800000, guid: ([0-9a-f]{32})", text)
    return m and m.group(1)


def _texture(guid, roots):
    for r in roots:
        if not r.exists():
            continue
        for meta in sorted(r.glob("*.meta")):
            if f"guid: {guid}" in meta.read_text(errors="ignore"):
                return meta.with_suffix("")
    raise SystemExit(f"[livery] no texture with guid {guid} in {[str(r) for r in roots]}")


def _srgb_mean(png):
    """The old DecoKitColourways.Mean: sRGB mean of every 7th texel (GetPixels32 order: bottom row first)."""
    import numpy as np
    try:
        from PIL import Image
        a = np.asarray(Image.open(png).convert("RGB"), dtype=np.float64)[::-1].reshape(-1, 3)[::7] / 255.0
    except ImportError:  # inside Blender (no Pillow): its byte buffer as floats, not colour-managed, bottom row first like GetPixels32
        import bpy
        img = bpy.data.images.load(str(png), check_existing=False)
        try:
            a = np.empty(img.size[0] * img.size[1] * 4, dtype=np.float32)
            img.pixels.foreach_get(a)
            a = a.reshape(-1, 4)[::7, :3].astype(np.float64)
        finally:
            bpy.data.images.remove(img)
    return [float(x) for x in a.mean(axis=0)]


def tint(text, hx, tex_roots):
    """The base colour a variant of base .mat `text` takes so it reads `hx` (sRGB 0..1 floats); a textured base is tinted so the texture's mean lands on hx."""
    want = hex_rgb(hx)
    g = _base_map_guid(text)
    if not g:
        return list(want), None
    png = _texture(g, tex_roots)
    mean = _srgb_mean(png)
    return [gam(min(1.0, lin(w) / lin(m))) for w, m in zip(want, mean)], dict(texture=png.name, srgbMean=[round(x, 6) for x in mean])


def _old_base_colour(legacy, key):
    p = legacy.parent / "Generated" / "Paint" / f"{OLD_PAINT[key]}.mat"
    if not p.exists():
        return None
    m = re.search(r"    - _BaseColor: \{r: ([\d.e-]+), g: ([\d.e-]+), b: ([\d.e-]+)", p.read_text())
    return [float(x) for x in m.groups()] if m else None


def write_mats(dst, legacy, meta_text, recolour):
    """<base>__<key>.mat (+ .meta) for every livery key from the base .mat as written (generated) or the legacy one, and Data/livery.json; returns the names."""
    d = Path(dst) / "Materials"
    d.mkdir(parents=True, exist_ok=True)
    roots = [Path(dst) / "Textures", legacy.parent / "Textures"]
    out, keys = [], {}
    for key, (base, hx, src, pieces) in LIVERY.items():
        own = d / f"{base}.mat"
        text = (own if own.exists() else legacy / f"{base}.mat").read_text()
        rgb, tex = tint(text, hx, roots)
        name = f"{base}__{key}"
        (d / f"{name}.mat").write_text(recolour(text, name, rgb))
        (d / f"{name}.mat.meta").write_text(meta_text(name, legacy))
        out.append(name)
        old = _old_base_colour(legacy, key)
        keys[key] = dict(base=base, material=name, hex=hx, source=src, baseFrom="generated" if own.exists() else "legacy",
                         baseColor=[round(x, 6) for x in rgb], **({"tintedOver": tex} if tex else {}),
                         **({"oldBaseColor": old, "oldDelta": round(max(abs(a - b) for a, b in zip(rgb, old)), 6)} if old else {}),
                         pieces=pieces)
    data = dict(schema="suborned.livery/1", brief="075-C", source="tools/decokit/livery.py",
                rule="A landmark instance swaps each base material below for its key's variant where its piece is listed (or in a listed kit category). "
                     "Same shader, keywords and textures as the base: only the base colour differs. Ported from the old DecoKitColourways `livery` layer.",
                keys=keys)
    (Path(dst) / "Data").mkdir(parents=True, exist_ok=True)
    (Path(dst) / "Data" / "livery.json").write_text(json.dumps(data, indent=1) + "\n")
    print(f"[livery] {', '.join(out)} -> {d}")
    return out
