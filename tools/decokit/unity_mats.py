"""Unity .mat files for the DecoKit materials the legacy kit never had (v0.17 accents, planets, beacon) and the ones retuned
by mats.WORLD_RETUNE. Data-driven from decokit_materials.json (= mats.LIB); no bpy. Written to <out>/Materials/<name>.mat + .meta.

Which: untextured enamel / metal / emit rows with no .mat in the legacy kit root (the DecoKit the catalogue reads), plus the
retuned names. A name the legacy kit has (the retuned ones) keeps the legacy guid (prefabs and importers bind by it), read from
the legacy root: unity/Assets/Suborned/Kit is git-ignored and only there after `bun run kit`, so a fresh checkout gave other
bytes. A new name gets a guid derived from the name, so the same table gives the same bytes. URP Lit, the layout of the kit's existing mats."""

import hashlib
import json
import os
import re
from pathlib import Path

LIT = "2529b42165e1d45a8bcfb0013bb5aba3"       # the shader guid every kit .mat uses (Complex Lit for enamel: 206122...)
COMPLEX = "206122040e60d470e92cddeccf1aacf6"
SUPPORTED = ("enamel", "metal", "brass", "emit", "ceramic", "stone", "chrome", "rubber", "roadmetal", "roadpaint")


def legacy_roots():
    home = lambda p: Path(os.path.expanduser(p))  # noqa: E731
    old, store = home(os.environ.get("SUBORNED_OLD", "~/Projects/the-suborned")), home(os.environ.get("SUBORNED_STORE", "~/Projects/the-suborned-store"))
    return [old / "game/Assets/TheSuborned/Art/DecoKit", store / "DecoKit"]


def legacy_mats():
    """The legacy kit's Materials folder: the root the catalogue reads (tools/kit/store.ts kitRoot, the first with a manifest).
    Which .mat are written and the retuned guids come from it, so a machine without it stops here rather than write another set."""
    root = next((r for r in legacy_roots() if (r / "decokit_manifest.json").exists()), None)
    if root is None:
        raise SystemExit("[DK-EXPORT] legacy DecoKit not found (set SUBORNED_OLD or SUBORNED_STORE): the .mat set and the retuned guids come from it")
    return root / "Materials"


def tex_guid(file):
    """The guid of a texture this generator writes (ground_tex.py, lit_art_text): derived from the file name, so the same table gives the same bytes."""
    return hashlib.md5(f"suborned.decokit.tex/{file}".encode()).hexdigest()


def _lin(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def _f(x):
    return f"{x:.8g}"


def _guid(name, legacy):
    meta = legacy / f"{name}.mat.meta"
    if meta.exists():
        for ln in meta.read_text().splitlines():
            if ln.startswith("guid: "):
                return ln[6:].strip()
    return hashlib.md5(f"suborned.decokit.mat/{name}".encode()).hexdigest()


# Glint, brief 079 (Sky: "why is everything glistening?"): these metals keep their reflections and lose the sun's specular, the standard URP Lit "Specular Highlights" off. Their
# smoothness (0.95 to 1 x mask) drew the sun at up to 7072 in linear on a few pixels: the white fire on the sedans' trim, poles and the fountain's rings. Lowering the smoothness instead
# only widens the white and takes the mirror away (measured, briefs/079/glisten/index.html).
SUN_OFF = ("DK_Chrome", "DK_Steel", "DK_Fountain_Gold", "DK_Fountain_Silver", "DK_Cross_Housing")  # DK_Cross_Housing: Vela, brief 079 note 2, the crossing's metal rings (no .mat of that name unless the candidate is built)


def sun_off(text):
    """A .mat's text with the sun's specular off: the float and the keyword together, keywords sorted as Unity writes them. No shader edit: the keyword is already declared."""
    if "- _SpecularHighlights: 1\n" not in text:
        return text
    text = text.replace("- _SpecularHighlights: 1\n", "- _SpecularHighlights: 0\n")
    m = re.search(r"  m_ValidKeywords:(?: \[\]\n|\n((?:  - \S+\n)*))", text)
    keys = sorted({*re.findall(r"- (\S+)", m.group(1) or ""), "_SPECULARHIGHLIGHTS_OFF"})
    return text[: m.start()] + "  m_ValidKeywords:\n" + "".join(f"  - {k}\n" for k in keys) + text[m.end() :]


def _fid(name):
    return -int.from_bytes(hashlib.sha256(f"suborned.decokit.matversion/{name}".encode()).digest()[:8], "big") >> 1 or -1


def mat_text(row):
    name, kind, (r, g, b) = row["name"], row["kind"], row["color"]
    enamel, emit = kind == "enamel", kind == "emit"
    em = row["emission"]
    emission = f"{{r: {_f((_lin(r) * em) ** (1 / 2.2))}, g: {_f((_lin(g) * em) ** (1 / 2.2))}, b: {_f((_lin(b) * em) ** (1 / 2.2))}, a: {_f(em)}}}" if emit else "{r: 0, g: 0, b: 0, a: 1}"
    tex = "".join(f"    - {t}:\n        m_Texture: {{fileID: 0}}\n        m_Scale: {{x: 1, y: 1}}\n        m_Offset: {{x: 0, y: 0}}\n"
                  for t in sorted(["_BaseMap", "_BumpMap", "_DetailAlbedoMap", "_DetailMask", "_DetailNormalMap", "_EmissionMap", "_MetallicGlossMap",
                                   "_OcclusionMap", "_ParallaxMap", "_SpecGlossMap", "unity_Lightmaps", "unity_LightmapsInd", "unity_ShadowMasks"]
                                  + (["_ClearCoatMap"] if enamel else ["_MainTex"])))
    fl = {"_AddPrecomputedVelocity": 0, "_AlphaClip": 0, "_AlphaToMask": 0, "_Blend": 0, "_BlendModePreserveSpecular": 1, "_BumpScale": 1,
          "_ClearCoatMask": 1 if enamel else 0, "_ClearCoatSmoothness": _f(row["coatSmoothness"]) if enamel else 0, "_Cull": 2, "_Cutoff": 0.5,
          "_DetailAlbedoMapScale": 1, "_DetailNormalMapScale": 1, "_DstBlend": 0, "_DstBlendAlpha": 0, "_EnvironmentReflections": 1,
          "_Metallic": _f(row["metallic"]), "_OcclusionStrength": 1, "_Parallax": 0.005, "_QueueOffset": 0, "_ReceiveShadows": 1,
          "_ScreenSpaceReflections": 1, "_ScreenSpaceReflectionsContributeTransparent": 1, "_Smoothness": _f(row["smoothness"]),
          "_SmoothnessTextureChannel": 0, "_SpecularHighlights": 1, "_SrcBlend": 1, "_SrcBlendAlpha": 1, "_Surface": 0, "_WorkflowMode": 1,
          "_XRMotionVectorsPass": 1, "_ZWrite": 1}
    if enamel:
        fl.update({"_BlendOp": 0, "_ClearCoat": 1})
    else:
        fl.update({"_GlossMapScale": 0, "_Glossiness": 0, "_GlossyReflections": 0})
    floats = "".join(f"    - {k}: {v}\n" for k, v in sorted(fl.items()))
    base = f"{{r: {_f(r)}, g: {_f(g)}, b: {_f(b)}, a: 1}}"
    cols = [("_BaseColor", base)] + ([] if enamel else [("_Color", base)]) + [("_EmissionColor", emission), ("_SpecColor", "{r: 0.2, g: 0.2, b: 0.2, a: 1}")]
    colors = "".join(f"    - {k}: {v}\n" for k, v in cols)
    kw = "\n  - _CLEARCOAT" if enamel else ("\n  - _EMISSION" if emit else " []")
    kw = "  m_ValidKeywords:" + (kw if kw != " []" else " []")
    return (f"%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &{_fid(name)}\nMonoBehaviour:\n  m_ObjectHideFlags: 11\n"
            "  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n"
            "  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: d0353a89b1f911e48b9e16bdc9f2e058, type: 3}\n  m_Name: \n"
            "  m_EditorClassIdentifier: Unity.RenderPipelines.Universal.Editor::UnityEditor.Rendering.Universal.AssetVersion\n  version: 10\n"
            "--- !u!21 &2100000\nMaterial:\n  serializedVersion: 8\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n"
            f"  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  m_Name: {name}\n  m_Shader: {{fileID: 4800000, guid: {COMPLEX if enamel else LIT}, type: 3}}\n"
            f"  m_Parent: {{fileID: 0}}\n  m_ModifiedSerializedProperties: 0\n{kw}\n  m_InvalidKeywords: []\n  m_LightmapFlags: {1 if emit else 4}\n"
            "  m_EnableInstancingVariants: 0\n  m_DoubleSidedGI: 0\n  m_CustomRenderQueue: -1\n  stringTagMap:\n    RenderType: Opaque\n  disabledShaderPasses: []\n"
            "  m_LockedProperties: \n  m_SavedProperties:\n    serializedVersion: 3\n    m_TexEnvs:\n" + tex + "    m_Ints: []\n    m_Floats:\n" + floats
            + "    m_Colors:\n" + colors + "  m_BuildTextureStacks: []\n  m_AllowLocking: 1\n")


def meta_text(name, legacy):
    return ("fileFormatVersion: 2\nguid: " + _guid(name, legacy) + "\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 2100000\n  userData: \n"
            "  assetBundleName: \n  assetBundleVariant: \n")


def clear_text(row, legacy):
    """A window-pane .mat (kinds `clear` and `sheen`, brief 020): the legacy DK_GlassDark.mat (premultiplied transparent, no shadow caster, queue 3000) with this row's
    name, colour, alpha and smoothness."""
    t = (legacy / "DK_GlassDark.mat").read_text()
    r, g, b = row["color"]
    t = t.replace("m_Name: DK_GlassDark", f"m_Name: {row['name']}")
    t = re.sub(r"(- _BaseColor: )\{[^}]*\}", rf"\g<1>{{r: {_f(r)}, g: {_f(g)}, b: {_f(b)}, a: {_f(row['alpha'])}}}", t)
    t = re.sub(r"(- _Color: )\{[^}]*\}", rf"\g<1>{{r: {_f(r)}, g: {_f(g)}, b: {_f(b)}, a: {_f(row['alpha'])}}}", t)
    return re.sub(r"(- _Smoothness: )[0-9.]+", rf"\g<1>{_f(row['smoothness'])}", t)


def film_text(row):
    """DK_GlassDark (brief 048): the window film on Suborned/DecoKit/GlassSky (a fake sky, no probe). Properties come from mats.GLASS_FILM through the row's `film`; the guid stays the
    legacy one (the FBXs and prefabs bind by it), only the shader and the values change."""
    f, name = row["film"], row["name"]
    lin = lambda h: tuple(_lin(int(h.lstrip("#")[i:i + 2], 16) / 255) for i in (0, 2, 4))  # noqa: E731
    col = lambda c, a=1.0: f"{{r: {_f(c[0])}, g: {_f(c[1])}, b: {_f(c[2])}, a: {_f(a)}}}"  # noqa: E731
    r, g, b = lin(f.get("glaze") or "#%02x%02x%02x" % tuple(round(c * 255) for c in row["color"]))
    colors = [("_BaseColor", col((r, g, b), f["alpha"])), ("_SkyHorizon", col(lin(f["horizon"]))), ("_SkyZenith", col(lin(f["zenith"]))), ("_Street", col(lin(f["street"])))]
    floats = [("_Cull", 2), ("_Edge", f["edge"]), ("_Fresnel", f["fresnel"]), ("_Sheen", f["sheen"]), ("_Glow", f.get("glow", 0.85))]
    return ("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!21 &2100000\nMaterial:\n  serializedVersion: 8\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n"
            f"  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  m_Name: {name}\n  m_Shader: {{fileID: 4800000, guid: {f['shader']}, type: 3}}\n"
            "  m_Parent: {fileID: 0}\n  m_ModifiedSerializedProperties: 0\n  m_ValidKeywords: []\n  m_InvalidKeywords: []\n  m_LightmapFlags: 4\n  m_EnableInstancingVariants: 0\n"
            "  m_DoubleSidedGI: 0\n  m_CustomRenderQueue: 3000\n  stringTagMap:\n    RenderType: Transparent\n  disabledShaderPasses:\n  - SHADOWCASTER\n  - DepthOnly\n"
            "  m_LockedProperties: \n  m_SavedProperties:\n    serializedVersion: 3\n    m_TexEnvs: []\n    m_Ints: []\n    m_Floats:\n"
            + "".join(f"    - {k}: {_f(v)}\n" for k, v in floats) + "    m_Colors:\n" + "".join(f"    - {k}: {v}\n" for k, v in colors) + "  m_BuildTextureStacks: []\n  m_AllowLocking: 1\n")


def lit_art_text(row, legacy, tex_guid):
    """A textured lit-from-within .mat (`emit` kind with a texture and no legacy .mat: the café's floor and paintings, brief 020): the legacy DK_Lightbox_Travel.mat with its
    texture swapped, the emission strength and the metric tiling (m_Scale = 1 / tileMetres, where the row has one)."""
    t = (legacy / "DK_Lightbox_Travel.mat").read_text()
    old = re.search(r"_BaseMap:\n\s+m_Texture: \{fileID: 2800000, guid: ([0-9a-f]{32})", t).group(1)
    t = t.replace(old, tex_guid).replace("m_Name: DK_Lightbox_Travel", f"m_Name: {row['name']}")
    em = row["emission"]
    v = _f(em ** (1 / 2.2))
    t = re.sub(r"(- _EmissionColor: )\{[^}]*\}", rf"\g<1>{{r: {v}, g: {v}, b: {v}, a: {_f(em)}}}", t)
    if row.get("tileMetres"):
        k = _f(1 / row["tileMetres"])
        t = re.sub(rf"(guid: {tex_guid}, type: 3\}}\n\s+m_Scale: )\{{x: 1, y: 1\}}", rf"\g<1>{{x: {k}, y: {k}}}", t)
    return t


# Brief 028 enamel fix (Glint, 2026-10-24): environment-reflection scale per material (1 = the scene's reflection as it is; the testbed's road used envMapIntensity 0.35).
# URP Lit has no such control, so the Lit shaders that opt in (DK_LitCutaway's ForwardLit, once Glint's patch is in: out/scratch/glint/road-envscale/) read it from the field
# they do not otherwise use, _ClearCoatMask, as scale = 1 - _ClearCoatMask (0 = untouched, so every other material stays as it is). Inert until that shader patch lands.
# Scope: the road's own materials only; chrome, glass and the rest of the kit keep the scene's reflection (light.json reflection.intensity 0.8).
ENV_SCALE = {"DK_Ground_RoadEnamel": 0.35, "DK_Ground_RoadPearl": 0.35}


def pbr_text(row, legacy):
    """A textured ground .mat (brief 026): URP Lit, albedo + normal + mask (R metallic, G occlusion, A smoothness, the old pave sheets' packing), optional detail
    pair, the tile as m_Scale = 1 / metres per UV unit (so a mesh with UV = world metres tiles at its real size). row['pbr'] is mats.GROUND's entry."""
    g = row["pbr"]
    tint = tuple(int(g["tint"].lstrip("#")[i:i + 2], 16) / 255 for i in (0, 2, 4)) if g.get("tint") else (1.0, 1.0, 1.0)
    base = mat_text(dict(name=row["name"], kind="stone", color=tint, smoothness=g["smoothness"], metallic=0.0, emission=0.0, coatSmoothness=0.0))
    sx, sy = (1 / g["tile"][0], 1 / g["tile"][1])
    scale = {"_BaseMap": (sx, sy), "_MainTex": (sx, sy), "_BumpMap": (sx, sy), "_MetallicGlossMap": (sx, sy), "_OcclusionMap": (sx, sy)}
    dsx, dsy = (1 / g["detailTile"], 1 / g["detailTile"]) if g.get("detail") else (1, 1)
    slots = {"_BaseMap": g["albedo"], "_MainTex": g["albedo"], "_BumpMap": g.get("normal"), "_MetallicGlossMap": g.get("mask"), "_OcclusionMap": g.get("mask"),
             "_DetailAlbedoMap": g.get("detail"), "_DetailNormalMap": g.get("detailNormal")}
    for slot, file in slots.items():
        if not file:
            continue
        sc = scale.get(slot, (dsx, dsy))
        old = f"    - {slot}:\n        m_Texture: {{fileID: 0}}\n        m_Scale: {{x: 1, y: 1}}\n"
        assert old in base, slot
        base = base.replace(old, f"    - {slot}:\n        m_Texture: {{fileID: 2800000, guid: {file if re.fullmatch('[0-9a-f]{32}', file) else tex_guid(file)}, type: 3}}\n"
                                 f"        m_Scale: {{x: {_f(sc[0])}, y: {_f(sc[1])}}}\n")
    kws = (["_DETAIL_MULX2"] if g.get("detail") else []) + (["_METALLICSPECGLOSSMAP"] if g.get("mask") else []) + (["_NORMALMAP"] if g.get("normal") else []) \
        + (["_OCCLUSIONMAP"] if g.get("mask") else [])
    base = base.replace("  m_ValidKeywords: []", "  m_ValidKeywords:" + "".join(f"\n  - {k}" for k in kws) if kws else "  m_ValidKeywords: []")
    base = re.sub(r"(- _BumpScale: )[0-9.]+", rf"\g<1>{_f(g.get('bump', 1.0))}", base)
    if row["name"] in ENV_SCALE:  # see ENV_SCALE: the field is 1 - the reflection scale
        base = re.sub(r"(- _ClearCoatMask: )[0-9.]+", rf"\g<1>{_f(1.0 - ENV_SCALE[row['name']])}", base)
    if g.get("detail"):
        base = re.sub(r"(- _DetailNormalMapScale: )[0-9.]+", rf"\g<1>{_f(g.get('detailBump', 1.0))}", base)
    return base


def emitmap_text(row):
    """An emission-only signal .mat (kind `emitmap`, brief 061): URP Lit, base colour BLACK, the emission colour (the row's hex x its strength: the state's default, replaced per head by a
    MaterialPropertyBlock) and, where the row has a texture, that texture as _BaseMap AND _EmissionMap with the alpha clip on at the row's cutoff (a glyph atlas draws only its glyphs; the clock
    ring's _Cutoff is its fill). No specular, no reflections, no ambient: it shows its emission and nothing else. Queue 2450, alpha-tested (written with a texture only)."""
    t = mat_text(dict(row, kind="emit"))
    t = re.sub(r"(- _BaseColor: )\{[^}]*\}", r"\g<1>{r: 0, g: 0, b: 0, a: 1}", t)
    t = re.sub(r"(- _Color: )\{[^}]*\}", r"\g<1>{r: 0, g: 0, b: 0, a: 1}", t)
    t = re.sub(r"(- _Smoothness: )[0-9.eE+-]+", r"\g<1>0", t)
    t = re.sub(r"(- _SpecularHighlights: )1", r"\g<1>0", t)
    t = re.sub(r"(- _EnvironmentReflections: )1", r"\g<1>0", t)
    t = re.sub(r"(- _ReceiveShadows: )1", r"\g<1>0", t)
    if row["texture"]:
        g = tex_guid(row["texture"])
        for slot in ("_BaseMap", "_MainTex", "_EmissionMap"):
            old = f"    - {slot}:\n        m_Texture: {{fileID: 0}}\n"
            assert old in t, slot
            t = t.replace(old, f"    - {slot}:\n        m_Texture: {{fileID: 2800000, guid: {g}, type: 3}}\n")
        t = re.sub(r"(- _AlphaClip: )0", r"\g<1>1", t)
        t = re.sub(r"(- _AlphaToMask: )0", r"\g<1>1", t)
        t = re.sub(r"(- _Cutoff: )[0-9.]+", rf"\g<1>{_f(row.get('cutoff', 0.5))}", t)
        t = t.replace("  m_ValidKeywords:\n  - _EMISSION", "  m_ValidKeywords:\n  - _ALPHATEST_ON\n  - _EMISSION")
        t = t.replace("m_CustomRenderQueue: -1", "m_CustomRenderQueue: 2450").replace("RenderType: Opaque", "RenderType: TransparentCutout")
    return t


def robot_enamel_text(row):
    """DK_RobotEnamel_*: the enamel .mat (Complex Lit, clear coat) with the robot detail pair: albedo (linear, x2) and normal at the row's bump, tiled 0.5 m x 2 m."""
    d = row["robotDetail"]
    base = mat_text(row)
    sx, sy = 1 / d["tile"][0], 1 / d["tile"][1]
    for slot, file in (("_DetailAlbedoMap", d["detail"]), ("_DetailNormalMap", d["detailNormal"])):
        old = f"    - {slot}:\n        m_Texture: {{fileID: 0}}\n        m_Scale: {{x: 1, y: 1}}\n"
        assert old in base, slot
        base = base.replace(old, f"    - {slot}:\n        m_Texture: {{fileID: 2800000, guid: {tex_guid(file)}, type: 3}}\n        m_Scale: {{x: {_f(sx)}, y: {_f(sy)}}}\n")
    base = base.replace("  m_ValidKeywords:\n  - _CLEARCOAT", "  m_ValidKeywords:\n  - _CLEARCOAT\n  - _DETAIL_MULX2")
    return re.sub(r"(- _DetailNormalMapScale: )[0-9.]+", rf"\g<1>{_f(d['bump'])}", base)


# Brief 072-F (Vela, Sky 2026-10-27: "Anywhere with a flat color, no texture, we need to give it one, as long as it doesn't hurt performance"): the untextured kit rows take
# the colourless surface detail pair of their kind (surface_tex.py) in the slots the .mat already has (_DetailAlbedoMap x2 + _DetailNormalMap, keyword _DETAIL_MULX2), as the
# robot and car enamels do. Texture, not palette: no new material, no colour, no other property changes; a legacy row keeps its legacy .mat text and guid, only the
# detail is added. One extra variant per shader at most (Lit [_DETAIL_MULX2]; Complex Lit [_CLEARCOAT _DETAIL_MULX2] already exists for the robot enamels).
# Tile (metres per repeat, m_Scale = 1 / tile on the kit's 1 m box-projected UVs) and bump come from surface_tex.py's report, so the maps and the .mat agree.
SURFACE_PAIR = {"enamel": "enamel", "ceramic": "enamel", "metal": "metal", "chrome": "metal", "brass": "metal", "satin": "metal", "roadmetal": "metal",
                "stone": "limestone", "rubber": "limestone", "fabric": "limestone"}
SURFACE_BUMP = {"chrome": 0.5, "rubber": 0.5, "fabric": 0.5}  # x the pair's bump: chrome faint (polished), soft goods a matte grain until a fabric layer exists
SURFACE_BUMP_NAME = ((r"^DK_Cloth_", 0.5),)                    # cloth registered as stone: the same half grain
# Not this round, with the reason (flat.md lists them open or excluded): the census's own exclusions (emissive, glass, faces, sky bodies, water, foliage, café dressing),
# and the opaque window stand-ins, which wait for Glint (a glaze mottle could read as dirty glass; judge in game light).
SURFACE_HOLD = ((r"Window_(Film|Teal|Tower)", "open (Glint): opaque enamel standing in for glass; the enamel pair at half bump, or none, judged in game light"),
                (r"^DK_Calib_", "a calibration swatch (18 % grey): stays flat by definition"))


def surface_report():
    """surface_tex.py's report (tile per layer, bump per kit pair): run.ts writes it before Blender. Missing = reject: the .mat would name maps nobody made."""
    p = Path(os.environ.get("DECOKIT_GROUND") or Path(__file__).resolve().parents[2] / "out/decokit-ground") / "surface-tex.json"
    if not p.exists():
        raise SystemExit(f"[DK-EXPORT] no {p}: run python3 tools/decokit/surface_tex.py (bun run decokit does) before the .mat set")
    d = json.loads(p.read_text())
    return {l["name"]: l["tile"] for l in d["layers"]}, {k: v["bump"] for k, v in d["kitPairs"].items()}


def surface_detail(row, report):
    """The detail this row takes: dict(pair, tile, bump, albedo, normal), or None (textured, another path's row, not a kind with a pair, excluded or held)."""
    from flat_census import EXCLUDE  # the census's exclusions, one list for both
    n, kind = row["name"], row["kind"]
    if row["texture"] or row.get("pbr") or row.get("film") or row.get("robotDetail") or kind not in SURFACE_PAIR:
        return None
    if any(re.search(p, n) for p, _ in EXCLUDE) or any(re.search(p, n) for p, _ in SURFACE_HOLD):
        return None
    tiles, bumps = report
    pair = SURFACE_PAIR[kind]
    k = next((f for p, f in SURFACE_BUMP_NAME if re.search(p, n)), SURFACE_BUMP.get(kind, 1.0))
    return dict(pair=pair, tile=tiles[pair], bump=round(bumps[pair] * k, 4), albedo=f"surface_{pair}_detail_v1.png", normal=f"surface_{pair}_detail_v1_normal.png")


def with_detail(text, det):
    """`text` (a Lit or Complex Lit .mat) with the surface pair in its detail slots, _DETAIL_MULX2 and the pair's bump; nothing else changes."""
    k = _f(1 / det["tile"])
    for slot, file in (("_DetailAlbedoMap", det["albedo"]), ("_DetailNormalMap", det["normal"])):
        old = f"    - {slot}:\n        m_Texture: {{fileID: 0}}\n        m_Scale: {{x: 1, y: 1}}\n"
        assert old in text, slot
        text = text.replace(old, f"    - {slot}:\n        m_Texture: {{fileID: 2800000, guid: {tex_guid(file)}, type: 3}}\n        m_Scale: {{x: {k}, y: {k}}}\n")
    if "  m_ValidKeywords: []\n" in text:
        text = text.replace("  m_ValidKeywords: []\n", "  m_ValidKeywords:\n  - _DETAIL_MULX2\n")
    else:
        m = re.search(r"  m_ValidKeywords:\n((?:  - \S+\n)+)", text)
        kws = sorted(set(m.group(1).split()[1::2]) | {"_DETAIL_MULX2"})
        text = text[:m.start()] + "  m_ValidKeywords:\n" + "".join(f"  - {w}\n" for w in kws) + text[m.end():]
    assert re.search(r"- _DetailAlbedoMapScale: 1\n", text), "a scaled detail would need _DETAIL_SCALED (another variant)"
    return re.sub(r"(- _DetailNormalMapScale: )[0-9.]+", rf"\g<1>{_f(det['bump'])}", text)


def write(table_path, dst, retuned):
    """Write the .mat for the missing and retuned materials; returns the names."""
    rows = json.loads(Path(table_path).read_text())["materials"]
    legacy = legacy_mats()
    report = surface_report()
    out, detailed = [], {}
    for row in rows:
        n = row["name"]
        d = Path(dst) / "Materials"
        if row.get("film"):  # a window film on its own shader (mats.GLASS_FILM): written whether or not the legacy kit has the name (it has: the guid is kept)
            d.mkdir(parents=True, exist_ok=True)
            (d / f"{n}.mat").write_text(film_text(row))
            (d / f"{n}.mat.meta").write_text(meta_text(n, legacy))
            out.append(n)
            continue
        if row["kind"] in ("clear", "sheen") and not (legacy / f"{n}.mat").exists():  # window panes
            d.mkdir(parents=True, exist_ok=True)
            (d / f"{n}.mat").write_text(clear_text(row, legacy))
            (d / f"{n}.mat.meta").write_text(meta_text(n, legacy))
            out.append(n)
            continue
        if row["kind"] == "emitmap":  # an emission-only signal surface (the lens rim, the face atlas, the clock ring): written whether or not the legacy kit has the name
            d.mkdir(parents=True, exist_ok=True)
            (d / f"{n}.mat").write_text(emitmap_text(row))
            (d / f"{n}.mat.meta").write_text(meta_text(n, legacy))
            out.append(n)
            continue
        if row.get("robotDetail"):  # a robot enamel: written whether or not the legacy kit has the name (it has not)
            d.mkdir(parents=True, exist_ok=True)
            (d / f"{n}.mat").write_text(robot_enamel_text(row))
            (d / f"{n}.mat.meta").write_text(meta_text(n, legacy))
            out.append(n)
            continue
        if row.get("pbr"):  # a ground material (mats.GROUND): textured Lit with its own maps; the .mat is written whether or not the legacy kit has the name
            d.mkdir(parents=True, exist_ok=True)
            (d / f"{n}.mat").write_text(pbr_text(row, legacy))
            (d / f"{n}.mat.meta").write_text(meta_text(n, legacy))
            out.append(n)
            continue
        if row["texture"] and row["kind"] == "emit" and not (legacy / f"{n}.mat").exists():  # lit art: a texture + .meta with a name-derived guid, and the .mat
            tg = tex_guid(row["texture"])
            tm = (legacy / "../Textures/poster_travel_v1.png.meta").resolve().read_text()
            (Path(dst) / "Textures").mkdir(parents=True, exist_ok=True)
            (Path(dst) / "Textures" / f"{row['texture']}.meta").write_text(re.sub(r"guid: [0-9a-f]{32}", f"guid: {tg}", tm, count=1))
            d.mkdir(parents=True, exist_ok=True)
            (d / f"{n}.mat").write_text(lit_art_text(row, legacy, tg))
            (d / f"{n}.mat.meta").write_text(meta_text(n, legacy))
            out.append(n)
            continue
        det = surface_detail(row, report)
        if det:  # brief 072-F: the surface pair on an untextured row; a legacy row keeps its own .mat text (no colour change), a generated or retuned row its table's
            own = n not in retuned and (legacy / f"{n}.mat").exists()
            if not own and row["kind"] not in SUPPORTED:
                continue
            d.mkdir(parents=True, exist_ok=True)
            (d / f"{n}.mat").write_text(with_detail((legacy / f"{n}.mat").read_text() if own else mat_text(row), det))
            (d / f"{n}.mat.meta").write_text(meta_text(n, legacy))
            out.append(n)
            detailed[n] = {k: det[k] for k in ("pair", "tile", "bump")}
            continue
        if row["texture"] or row["kind"] not in SUPPORTED:
            continue
        if n not in retuned and (legacy / f"{n}.mat").exists():
            continue
        d = Path(dst) / "Materials"
        d.mkdir(parents=True, exist_ok=True)
        (d / f"{n}.mat").write_text(mat_text(row))
        (d / f"{n}.mat.meta").write_text(meta_text(n, legacy))
        out.append(n)
    import colourways  # brief 075 old palette: the old DecoKitColourways sets (five ensembles + Soft) from the base .mats just written
    out += colourways.write_mats(dst, legacy, meta_text)
    import livery  # brief 075-C: the landmark livery (vermilion lattices, brick sphere, SpaceHQ gold), the same recolour as the colourways
    out += livery.write_mats(dst, legacy, meta_text, colourways._recolour)
    import posters  # brief 075-F1: the billboard ad and poster-sheet variants (sheet B + offsets), the same no-new-SetPass swap
    out += posters.write_mats(dst, legacy, meta_text, tex_guid)
    import carpaint  # brief 075 roll-out: the 1950s car paints for presentation traffic (body / roof variants of DK_CarEnamel_Ivory), the same recolour
    out += carpaint.write_mats(dst, legacy, meta_text, colourways._recolour)
    for n in SUN_OFF:  # brief 079: after the colourways, which keep the base text as it was (only these four changed; the measured set)
        f = Path(dst) / "Materials" / f"{n}.mat"
        if f.exists():
            f.write_text(sun_off(f.read_text()))
    (Path(dst) / "surface-detail-mats.json").write_text(json.dumps(dict(schema="suborned.surfacedetail/1", brief="072-F", source="tools/decokit/unity_mats.py surface_detail",
                                                                       materials=dict(sorted(detailed.items()))), indent=1) + "\n")
    return out
