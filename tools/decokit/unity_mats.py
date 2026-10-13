"""Unity .mat files for the DecoKit materials the legacy kit never had (v0.17 accents, planets, beacon) and the ones retuned
by mats.WORLD_RETUNE. Data-driven from decokit_materials.json (= mats.LIB); no bpy. Written to <out>/Materials/<name>.mat + .meta.

Which: untextured enamel / metal / emit rows with no .mat in the legacy kit root (the DecoKit the catalogue reads), plus the
retuned names. A name the legacy kit has (the retuned ones) keeps the legacy guid (prefabs and importers bind by it), read from
the legacy root: unity/Assets/Suborned/Kit is git-ignored and only there after `bun run kit`, so a fresh checkout gave other
bytes. A new name gets a guid derived from the name, so the same table gives the same bytes. URP Lit, the layout of the kit's existing mats."""

import hashlib
import json
import os
from pathlib import Path

LIT = "2529b42165e1d45a8bcfb0013bb5aba3"       # the shader guid every kit .mat uses (Complex Lit for enamel: 206122...)
COMPLEX = "206122040e60d470e92cddeccf1aacf6"
SUPPORTED = ("enamel", "metal", "emit", "ceramic", "stone", "chrome", "rubber")


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


def write(table_path, dst, retuned):
    """Write the .mat for the missing and retuned materials; returns the names."""
    rows = json.loads(Path(table_path).read_text())["materials"]
    legacy = legacy_mats()
    out = []
    for row in rows:
        n = row["name"]
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
