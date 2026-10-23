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
SUPPORTED = ("enamel", "metal", "brass", "emit", "ceramic", "stone", "chrome", "rubber")


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
    if g.get("detail"):
        base = re.sub(r"(- _DetailNormalMapScale: )[0-9.]+", rf"\g<1>{_f(g.get('detailBump', 1.0))}", base)
    return base


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


def write(table_path, dst, retuned):
    """Write the .mat for the missing and retuned materials; returns the names."""
    rows = json.loads(Path(table_path).read_text())["materials"]
    legacy = legacy_mats()
    out = []
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
        if row["texture"] or row["kind"] not in SUPPORTED:
            continue
        if n not in retuned and (legacy / f"{n}.mat").exists():
            continue
        d = Path(dst) / "Materials"
        d.mkdir(parents=True, exist_ok=True)
        (d / f"{n}.mat").write_text(mat_text(row))
        (d / f"{n}.mat.meta").write_text(meta_text(n, legacy))
        out.append(n)
    return out
