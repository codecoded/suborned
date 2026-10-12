"""Deco Kit material library: one table drives Blender shading and the Unity URP materials.

Colours are sRGB hex sampled from the reference sheet (Concepts/DecoKitReference). Kinds:
  enamel   glossy vitreous enamel: rough base + mirror clear coat (URP Complex Lit clear coat)
  ceramic  glazed tile: satin, no coat          stone  limestone / terrazzo: matte
  chrome   polished metal                        metal  brushed / gold metal
  rubber   tyres, seals                          glass  tinted transparent
  emit     lamp glass, signals, screens (strength in Blender W/m^2-ish units, Unity nits scale)
`export_json()` writes the table for Unity (Assets/TheSuborned/Art/DecoKit/decokit_materials.json).
"""

import json
import os
from pathlib import Path

import bpy

LIB = {
    # enamel
    "DK_Enamel_Ivory": ("enamel", "#EFE8D6"), "DK_Enamel_Cream": ("enamel", "#F3DDB0"),
    "DK_Enamel_Teal": ("enamel", "#1F6C67"), "DK_Enamel_Seafoam": ("enamel", "#72B5A7"),
    "DK_Enamel_Mint": ("enamel", "#A8D3BE"), "DK_Enamel_Red": ("enamel", "#C42A1F"),
    "DK_Enamel_Mustard": ("enamel", "#EEA82D"), "DK_Enamel_Powder": ("enamel", "#86ADD3"),
    "DK_Enamel_Royal": ("enamel", "#2C56A6"), "DK_Enamel_Charcoal": ("enamel", "#2B2F33"),
    # ceramic paving
    "DK_Tile_Ivory": ("ceramic", "#E8E1CF"), "DK_Tile_Grey": ("ceramic", "#B9B7AF"),
    "DK_Tile_Blue": ("ceramic", "#9DBBDD"), "DK_Tile_Red": ("ceramic", "#D8674F"),
    "DK_Tile_Mint": ("ceramic", "#8CC2AE"), "DK_Tile_Teal": ("ceramic", "#3E8A80"),
    # stone
    "DK_Limestone": ("stone", "#E7DECA"), "DK_Stone_Dark": ("stone", "#4E5152"),
    "DK_Soil": ("stone", "#3B2C22"),
    # metal
    "DK_Chrome": ("chrome", "#EDEDED"), "DK_Steel": ("metal", "#A9ABAE"),
    "DK_Gold": ("metal", "#D6AE62"), "DK_Iron": ("metal", "#3A3C3E"),
    # misc
    "DK_Rubber": ("rubber", "#1B1C1E"), "DK_Glass": ("glass", "#CFEDE6"),
    "DK_GlassDark": ("glass", "#35585A"),
    "DK_Lamp": ("emit", "#FFE2A6", 6.0), "DK_Signal_Red": ("emit", "#FF3B26", 4.0),
    "DK_Signal_Amber": ("emit", "#FFB21E", 4.0), "DK_Signal_Green": ("emit", "#2EE07A", 4.0),
    "DK_Screen": ("emit", "#7FE3D4", 1.5), "DK_RobotEye": ("emit", "#FF8A1E", 3.0),
    # textured (TEXTURES below): decals are alpha-keyed sheets, tiles are metric-scale albedo
    "DK_Decal_Transit": ("decal", "#FFFFFF"), "DK_Terrazzo_Ivory": ("stone", "#FFFFFF"),
    "DK_Terrazzo_Seafoam": ("stone", "#FFFFFF"), "DK_Limestone_Tex": ("stone", "#FFFFFF"),
    "DK_Poster_Travel": ("art", "#FFFFFF"), "DK_Lightbox_Travel": ("emit", "#FFFFFF", 1.2),
    "DK_Banner_Comet": ("fabric", "#FFFFFF"), "DK_Awning_Teal": ("fabric", "#FFFFFF"),
    "DK_Steel_Brushed": ("metal", "#FFFFFF"), "DK_Asphalt": ("stone", "#FFFFFF"),
    # foliage cards: white-keyed cutouts (derive_maps.py), alpha-tested and two-sided in both engines
    "DK_Leaf_PalmFeather": ("cutout", "#FFFFFF"), "DK_Leaf_PalmFan": ("cutout", "#FFFFFF"),
    "DK_Leaf_Cypress": ("cutout", "#FFFFFF"), "DK_Leaf_Shrub": ("cutout", "#FFFFFF"),
    "DK_Flower_Coral": ("cutout", "#FFFFFF"),
}
DECAL = dict(recycle_teal=0, recycle_mustard=1, tickets_screen=2, keypad=3, lockers_1_6=4, lockers_7_12=5,
             harbour_line_7=6, bus_stop=7, no_parking=8, crossing=9, civic_plaza_arrow=10, info=11,
             city_crest=12, caution=13, vesper_transit=14, city_utility=15)

# material -> (texture file in Textures/gen or derived, metres per tile; decals ignore scale)
TEXTURES = {
    "DK_Decal_Transit": ("decals_transit_v1_alpha.png", 1.0),
    "DK_Terrazzo_Ivory": ("terrazzo_ivory_v1.png", 1.5), "DK_Terrazzo_Seafoam": ("terrazzo_seafoam_v1.png", 1.5),
    "DK_Limestone_Tex": ("facade_panel_v1.png", 2.0),  # pave_sheet.py: 1x2 m precast panels, read at play zoom
    "DK_Poster_Travel": ("poster_travel_v1.png", 1.0), "DK_Lightbox_Travel": ("poster_travel_v1.png", 1.0),
    "DK_Banner_Comet": ("banner_comet_teal_v1.png", 1.0), "DK_Awning_Teal": ("awning_stripe_teal_v1.png", 1.2),
    "DK_Steel_Brushed": ("brushed_steel_v1.png", 0.5), "DK_Asphalt": ("asphalt_warm_v1.png", 4.0),
    "DK_Leaf_PalmFeather": ("palm_frond_feather_v1_alpha.png", 1.0), "DK_Leaf_PalmFan": ("palm_frond_fan_v1_alpha.png", 1.0),
    "DK_Leaf_Cypress": ("cypress_foliage_v1_alpha.png", 1.0), "DK_Leaf_Shrub": ("shrub_leaves_v1_alpha.png", 1.0),
    "DK_Flower_Coral": ("flower_cluster_coral_v1_alpha.png", 1.0),
    # pave_sheet.py sets (albedo + _normal + _mask): sheets are UV-mapped per module, the kerb granite is metric
    "DK_Pave_Flags": ("pave_flags_v1.png", 4.0), "DK_Gutter_Setts": ("gutter_setts_v1.png", 4.0),
    "DK_Granite_Kerb": ("granite_kerb_v1.png", 0.5), "DK_Granite_Kerb_B": ("granite_kerb_v1.png", 0.5),
    "DK_Asphalt_Fine": ("asphalt_fine_v1.png", 1.5),
    "DK_Hull_Ivory": ("hull_panels_v1.png", 8.0), "DK_Hull_Silver": ("hull_panels_v1.png", 8.0),
}
UV_MAPPED = ("decal", "art", "emit", "fabric", "cutout", "paving", "face")  # these use the face's explicit (decal) UVs, not metric tiling
SOURCE_ART = Path(os.environ.get("DECOKIT_SOURCEART") or Path(os.environ.get("SUBORNED_STORE", "~/Projects/the-suborned-store")).expanduser() / "source-art/DecoKit")
TEXDIRS = (SOURCE_ART / "Textures/derived", SOURCE_ART / "Textures/gen")


def cell(i, cols=4, rows=4):
    """UV quad (BL, BR, TR, TL) of cell i (row-major from the top-left) in a decal sheet."""
    c, r = i % cols, i // cols
    u0, u1, v1 = c / cols, (c + 1) / cols, 1 - r / rows
    v0 = v1 - 1 / rows
    return ((u0, v0), (u1, v0), (u1, v1), (u0, v1))


def _texture(name):
    if name not in TEXTURES:
        return None, 1.0
    file, scale = TEXTURES[name]
    for d in TEXDIRS:
        if (d / file).exists():
            return bpy.data.images.load(str(d / file), check_existing=True), scale
    return None, scale

# (base roughness, metallic, coat, coat roughness, alpha)
KIND = {
    "enamel": (0.32, 0.0, 1.0, 0.03, 1.0), "ceramic": (0.38, 0.0, 0.0, 0.0, 1.0),
    "stone": (0.75, 0.0, 0.0, 0.0, 1.0), "chrome": (0.05, 1.0, 0.0, 0.0, 1.0),
    "metal": (0.3, 1.0, 0.0, 0.0, 1.0), "rubber": (0.8, 0.0, 0.0, 0.0, 1.0),
    "glass": (0.02, 0.0, 0.0, 0.0, 0.3), "emit": (0.2, 0.0, 0.0, 0.0, 1.0),
    "decal": (0.3, 0.0, 1.0, 0.03, 1.0), "art": (0.45, 0.0, 0.6, 0.05, 1.0), "fabric": (0.85, 0.0, 0.0, 0.0, 1.0),
    "cutout": (0.85, 0.0, 0.0, 0.0, 1.0),  # + near-zero specular (get): dark leaves on bent card normals read as satin
    "satin": (0.5, 1.0, 0.0, 0.0, 1.0),  # brushed/cast metal that must not mirror the studio black (ground.py)
    "paving": (0.5, 0.0, 0.0, 0.0, 1.0),  # sheet-mapped stone (paving.py); roughness comes from the _mask alpha
    "face": (0.2, 0.0, 0.0, 0.0, 0.0),  # robot display face: drawn by DK_RobotFace.shader in Unity, clear here
}


def register(name, kind, hexstr, *extra):
    """Family modules add their own materials at import time (never edit LIB from two places)."""
    LIB.setdefault(name, (kind, hexstr, *extra))
    return name


def srgb(hexstr):
    h = hexstr.lstrip("#")
    return tuple(int(h[i:i + 2], 16) / 255 for i in (0, 2, 4))


def linear(c):
    return tuple(x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c)


def get(name):
    """Get-or-create the Blender material for a LIB entry."""
    m = bpy.data.materials.get(name)
    if m is not None:
        return m
    kind, hexstr, *extra = LIB[name]
    rough, metal, coat, coat_rough, alpha = KIND[kind]
    col = (*linear(srgb(hexstr)), 1.0)
    m = bpy.data.materials.new(name)
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = col
    b.inputs["Roughness"].default_value = rough
    b.inputs["Metallic"].default_value = metal
    b.inputs["Coat Weight"].default_value = coat
    b.inputs["Coat Roughness"].default_value = coat_rough
    if kind == "glass":  # alpha-blended film, as Unity renders it: transmission speckles at icon samples
        b.inputs["IOR"].default_value = 1.45
        m.surface_render_method = "BLENDED"
        b.inputs["Alpha"].default_value = alpha
    if kind == "cutout":
        b.inputs["Specular IOR Level"].default_value = 0.1
    if kind == "face":
        m.surface_render_method = "BLENDED"
        b.inputs["Alpha"].default_value = alpha
    if kind == "emit":
        b.inputs["Emission Color"].default_value = col
        b.inputs["Emission Strength"].default_value = extra[0]
    img, scale = _texture(name)
    if img is not None:
        nt = m.node_tree
        tex = nt.nodes.new("ShaderNodeTexImage")
        tex.image = img
        if metric_tiling(name, kind):  # metric tiling from the 1 m box-projected UVs
            mp, tc = nt.nodes.new("ShaderNodeMapping"), nt.nodes.new("ShaderNodeTexCoord")
            mp.inputs["Scale"].default_value = (1 / scale, 1 / scale, 1)
            nt.links.new(tc.outputs["UV"], mp.inputs["Vector"])
            nt.links.new(mp.outputs["Vector"], tex.inputs["Vector"])
        if hexstr.upper() != "#FFFFFF":  # tinted texture (a second stone lot): Unity multiplies _BaseColor too
            mul = nt.nodes.new("ShaderNodeMix")
            mul.data_type, mul.blend_type = "RGBA", "MULTIPLY"
            mul.inputs["Factor"].default_value = 1.0
            mul.inputs["B"].default_value = col
            nt.links.new(tex.outputs["Color"], mul.inputs["A"])
            nt.links.new(mul.outputs["Result"], b.inputs["Base Color"])
        else:
            nt.links.new(tex.outputs["Color"], b.inputs["Base Color"])
        if kind == "emit":  # lit artwork (lightboxes): the texture is the emission
            nt.links.new(tex.outputs["Color"], b.inputs["Emission Color"])
        if kind in ("decal", "cutout"):
            nt.links.new(tex.outputs["Alpha"], b.inputs["Alpha"])
            m.surface_render_method = "DITHERED"
        _pbr_maps(nt, b, tex, TEXTURES[name][0])
    m.diffuse_color = col  # viewport solid colour
    return m


def _pbr_maps(nt, b, tex, file):
    """A pave_sheet.py set (<stem>_mask.png beside it) also drives normal + roughness, as Unity's Lit reads them."""
    stem = file[:-4]
    maps = [TEXDIRS[0] / f"{stem}_{k}.png" for k in ("normal", "mask")]
    if not all(p.exists() for p in maps):
        return
    vec = tex.inputs["Vector"].links[0].from_socket if tex.inputs["Vector"].is_linked else None
    nodes = []
    for p in maps:
        n = nt.nodes.new("ShaderNodeTexImage")
        n.image = bpy.data.images.load(str(p), check_existing=True)
        n.image.colorspace_settings.name = "Non-Color"
        n.image.alpha_mode = "CHANNEL_PACKED"
        if vec is not None:
            nt.links.new(vec, n.inputs["Vector"])
        nodes.append(n)
    nm = nt.nodes.new("ShaderNodeNormalMap")
    nt.links.new(nodes[0].outputs["Color"], nm.inputs["Color"])
    nt.links.new(nm.outputs["Normal"], b.inputs["Normal"])
    inv = nt.nodes.new("ShaderNodeMath")
    inv.operation, inv.inputs[0].default_value = "SUBTRACT", 1.0
    nt.links.new(nodes[1].outputs["Alpha"], inv.inputs[1])
    nt.links.new(inv.outputs["Value"], b.inputs["Roughness"])


class _Pal:
    """Attribute access: pal.Enamel_Red -> DK_Enamel_Red."""

    def __getattr__(self, key):
        return get("DK_" + key)


pal = _Pal()


def metric_tiling(name, kind):
    return kind not in UV_MAPPED or name == "DK_Awning_Teal"


def export_json(path):
    rows = []
    for name, (kind, hexstr, *extra) in LIB.items():
        rough, metal, coat, coat_rough, alpha = KIND[kind]
        tex = TEXTURES.get(name)
        rows.append(dict(name=name, kind=kind, color=list(srgb(hexstr)), smoothness=1 - rough, metallic=metal,
                         coat=coat, coatSmoothness=1 - coat_rough, alpha=alpha,
                         emission=extra[0] if extra else 0.0, texture=tex[0] if tex else "",
                         tileMetres=tex[1] if tex and metric_tiling(name, kind) else 0.0))
    Path(path).parent.mkdir(parents=True, exist_ok=True)
    Path(path).write_text(json.dumps(dict(materials=rows), indent=1))
