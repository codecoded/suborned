"""Unity .meta and .mat text for the stand-in people (brief 020). Pure python, no bpy. GUIDs are md5 of the asset path under People/,
so the same inputs give the same bytes. Layouts follow the kit's (tools/decokit/unity_mats.py; Kit/Textures/*.meta; Kit/Models/*.fbx.meta)."""
import hashlib

LIT = "933532a4fcc9baf4fa0491de14d08ed7"   # URP Lit (Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader). Not the kit's 2529b42...: that is DK_LitCutaway, which fades building faces and has no business on a person


def guid(rel):
    return hashlib.md5(f"suborned.people/{rel}".encode()).hexdigest()


def _f(x):
    return f"{x:.8g}"


# ---------------------------------------------------------------- textures

def tex_meta(rel, kind, size):
    """kind: albedo (sRGB) | normal | data (metal/occlusion/smoothness, linear) | emissive (sRGB)."""
    srgb = 0 if kind in ("normal", "data") else 1
    ttype = 1 if kind == "normal" else 0
    return f"""fileFormatVersion: 2
guid: {guid(rel)}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 1
    sRGBTexture: {srgb}
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: {size}
  textureSettings:
    serializedVersion: 2
    filterMode: 2
    aniso: 4
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 1
  lightmap: 0
  compressionQuality: 50
  spriteMode: 0
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 0
  spriteTessellationMethod: 0
  spriteTessellationDetail: -1
  spriteGeometrySubdivision: -1
  textureType: {ttype}
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: {size}
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Standalone
    maxTextureSize: {size}
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData:
    physicsShape: []
    bones: []
    spriteID:
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""


# ---------------------------------------------------------------- materials

def mat_meta(rel):
    return (f"fileFormatVersion: 2\nguid: {guid(rel)}\nNativeFormatImporter:\n  externalObjects: {{}}\n  mainObjectFileID: 2100000\n  userData: \n"
            "  assetBundleName: \n  assetBundleVariant: \n")


def _tex(slot, rel):
    if rel is None:
        return f"    - {slot}:\n        m_Texture: {{fileID: 0}}\n        m_Scale: {{x: 1, y: 1}}\n        m_Offset: {{x: 0, y: 0}}\n"
    return f"    - {slot}:\n        m_Texture: {{fileID: 2800000, guid: {guid(rel)}, type: 3}}\n        m_Scale: {{x: 1, y: 1}}\n        m_Offset: {{x: 0, y: 0}}\n"


def mat_text(name, base=None, normal=None, metal=None, emissive=None, emission=1.0, smoothness=1.0, metallic=1.0, tint=(1, 1, 1), transparent=False):
    """URP Lit. `metal` is one texture used twice: R metallic + A smoothness (the metallic map) and G occlusion (the occlusion map)."""
    kw = []
    if normal: kw.append("_NORMALMAP")
    if metal: kw += ["_METALLICSPECGLOSSMAP", "_OCCLUSIONMAP"]
    if emissive: kw.append("_EMISSION")
    kws = "".join(f"\n  - {k}" for k in kw) if kw else " []"
    slots = [("_BaseMap", base), ("_BumpMap", normal), ("_EmissionMap", emissive), ("_MetallicGlossMap", metal), ("_OcclusionMap", metal),
             ("_DetailAlbedoMap", None), ("_DetailMask", None), ("_DetailNormalMap", None), ("_MainTex", base), ("_ParallaxMap", None), ("_SpecGlossMap", None)]
    tex = "".join(_tex(s, r) for s, r in sorted(slots))
    fl = {"_AddPrecomputedVelocity": 0, "_AlphaClip": 0, "_AlphaToMask": 0, "_Blend": 0, "_BlendModePreserveSpecular": 1, "_BumpScale": 1,
          "_ClearCoatMask": 0, "_ClearCoatSmoothness": 0, "_Cull": 2, "_Cutoff": 0.5, "_DetailAlbedoMapScale": 1, "_DetailNormalMapScale": 1,
          "_DstBlend": 10 if transparent else 0, "_DstBlendAlpha": 10 if transparent else 0, "_EnvironmentReflections": 1,
          "_Metallic": _f(0.0 if not metal else 1.0), "_OcclusionStrength": 1, "_Parallax": 0.005, "_QueueOffset": 0, "_ReceiveShadows": 1,
          "_Smoothness": _f(smoothness), "_SmoothnessTextureChannel": 0, "_SpecularHighlights": 1, "_SrcBlend": 5 if transparent else 1,
          "_SrcBlendAlpha": 1, "_Surface": 1 if transparent else 0, "_WorkflowMode": 1, "_XRMotionVectorsPass": 1, "_ZWrite": 0 if transparent else 1}
    floats = "".join(f"    - {k}: {v}\n" for k, v in sorted(fl.items()))
    e = emission if emissive else 0
    cols = [("_BaseColor", f"{{r: {_f(tint[0])}, g: {_f(tint[1])}, b: {_f(tint[2])}, a: 1}}"), ("_Color", f"{{r: {_f(tint[0])}, g: {_f(tint[1])}, b: {_f(tint[2])}, a: 1}}"),
            ("_EmissionColor", f"{{r: {_f(e)}, g: {_f(e)}, b: {_f(e)}, a: 1}}"), ("_SpecColor", "{r: 0.2, g: 0.2, b: 0.2, a: 1}")]
    colors = "".join(f"    - {k}: {v}\n" for k, v in sorted(cols))
    fid = -int.from_bytes(hashlib.sha256(f"suborned.people.matversion/{name}".encode()).digest()[:8], "big") >> 1 or -1
    return (f"%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &{fid}\nMonoBehaviour:\n  m_ObjectHideFlags: 11\n"
            "  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n"
            "  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: d0353a89b1f911e48b9e16bdc9f2e058, type: 3}\n  m_Name: \n"
            "  m_EditorClassIdentifier: Unity.RenderPipelines.Universal.Editor::UnityEditor.Rendering.Universal.AssetVersion\n  version: 10\n"
            "--- !u!21 &2100000\nMaterial:\n  serializedVersion: 8\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n"
            f"  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  m_Name: {name}\n  m_Shader: {{fileID: 4800000, guid: {LIT}, type: 3}}\n"
            f"  m_Parent: {{fileID: 0}}\n  m_ModifiedSerializedProperties: 0\n  m_ValidKeywords:{kws}\n  m_InvalidKeywords: []\n  m_LightmapFlags: {1 if emissive else 4}\n"
            f"  m_EnableInstancingVariants: 0\n  m_DoubleSidedGI: 0\n  m_CustomRenderQueue: {3000 if transparent else -1}\n  stringTagMap:\n    RenderType: {'Transparent' if transparent else 'Opaque'}\n  disabledShaderPasses: []\n"
            "  m_LockedProperties: \n  m_SavedProperties:\n    serializedVersion: 3\n    m_TexEnvs:\n" + tex + "    m_Ints: []\n    m_Floats:\n" + floats
            + "    m_Colors:\n" + colors + "  m_BuildTextureStacks: []\n  m_AllowLocking: 1\n")


# ---------------------------------------------------------------- models

_MODEL_HEAD = """fileFormatVersion: 2
guid: {guid}
ModelImporter:
  serializedVersion: 24600
  internalIDToNameTable: []
  externalObjects:
{externals}  materials:
    materialImportMode: 2
    materialName: 0
    materialSearch: 1
    materialLocation: 1
    searchTexturesGlobally: 0
  animations:
    legacyGenerateAnimations: 4
    bakeSimulation: 0
    resampleCurves: 1
    optimizeGameObjects: 0
    removeConstantScaleCurves: 0
    motionNodeName:
    animationImportErrors:
    animationImportWarnings:
    animationRetargetingWarnings:
    animationDoRetargetingWarnings: 0
    importAnimatedCustomProperties: 0
    importConstraints: 0
    animationCompression: 1
    animationRotationError: 0.5
    animationPositionError: 0.5
    animationScaleError: 0.5
    animationWrapMode: 0
    extraExposedTransformPaths: []
    extraUserProperties: []
    clipAnimations:{clips}
    isReadable: 0
  meshes:
    lODScreenPercentages: []
    globalScale: 1
    meshCompression: 0
    addColliders: 0
    useSRGBMaterialColor: 1
    sortHierarchyByName: 1
    preBakeConvexCollisionMesh: 0
    preBakeTriangleCollisionMesh: 0
    importPhysicalCameras: 0
    importVisibility: 0
    importBlendShapes: 0
    importCameras: 0
    importLights: 0
    nodeNameCollisionStrategy: 1
    fileIdsGeneration: 2
    swapUVChannels: 0
    generateSecondaryUV: 0
    useFileUnits: 1
    keepQuads: 0
    weldVertices: 1
    bakeAxisConversion: 0
    preserveHierarchy: 0
    skinWeightsMode: 0
    maxBonesPerVertex: 4
    minBoneWeight: 0.001
    optimizeBones: 1
    generateMeshLods: 0
    meshLodGenerationFlags: 0
    maximumMeshLod: -1
    importUVs: -1
    importVertexColors: 1
    meshOptimizationFlags: -1
    indexFormat: 0
    secondaryUVAngleDistortion: 8
    secondaryUVAreaDistortion: 15.000001
    secondaryUVHardAngle: 88
    secondaryUVMarginMethod: 1
    secondaryUVMinLightmapResolution: 40
    secondaryUVMinObjectScale: 1
    secondaryUVPackMargin: 4
    useFileScale: 1
    strictVertexDataChecks: 0
  tangentSpace:
    normalSmoothAngle: 60
    normalImportMode: 0
    tangentImportMode: 3
    normalCalculationMode: 4
    legacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes: 0
    blendShapeNormalImportMode: 1
    normalSmoothingSource: 0
    calculateBlendshapeNormalsDeltaFromImportedNormals: 0
  referencedClips: []
  importAnimation: {import_anim}
  humanDescription:
    serializedVersion: 3
    human: []
    skeleton: []
    armTwist: 0.5
    foreArmTwist: 0.5
    upperLegTwist: 0.5
    legTwist: 0.5
    armStretch: 0.05
    legStretch: 0.05
    feetSpacing: 0
    globalScale: 1
    rootMotionBoneName:
    hasTranslationDoF: 0
    hasExtraRoot: 0
    skeletonHasParents: 1
  lastHumanDescriptionAvatarSource: {{instanceID: 0}}
  autoGenerateAvatarMappingIfUnspecified: 1
  animationType: 3
  humanoidOversampling: 1
  avatarSetup: {avatar_setup}
  addHumanoidExtraRootOnlyWhenUsingAvatar: 1
  remapMaterialsIfMaterialImportModeIsNone: 0
  additionalBone: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def model_meta(rel, materials=None, avatar_source_guid=None):
    """A figure FBX: Humanoid, avatar made from this model, no animation, materials bound by name to the given {name: mat-rel}."""
    ext = ""
    for n, r in sorted((materials or {}).items()):
        ext += f"  - first:\n      type: UnityEngine:Material\n      assembly: UnityEngine.CoreModule\n      name: {n}\n    second: {{fileID: 2100000, guid: {guid(r)}, type: 2}}\n"
    if not ext: ext = "  []\n" if False else ""
    externals = ext if ext else ""
    text = _MODEL_HEAD.format(guid=guid(rel), externals=externals, clips=" []", import_anim=0, avatar_setup=1)
    if not externals: text = text.replace("  externalObjects:\n  materials:", "  externalObjects: {}\n  materials:")
    return text


def clip_meta(rel, name, take, first, last, loop, feet=False):
    """A clip-only FBX (Mixamo library file): Humanoid, its own avatar, one clip `name` from take `take`, looping or not."""
    lp = 1 if loop else 0
    body = f"""
    - serializedVersion: 19
      name: {name}
      takeName: {take}
      internalID: 0
      firstFrame: {first}
      lastFrame: {last}
      wrapMode: 0
      orientationOffsetY: 0
      level: 0
      cycleOffset: 0
      loop: 0
      hasAdditiveReferencePose: 0
      loopTime: {lp}
      loopBlend: {lp}
      loopBlendOrientation: 1
      loopBlendPositionY: 1
      loopBlendPositionXZ: 0
      keepOriginalOrientation: 1
      keepOriginalPositionY: {{orig_y}}
      keepOriginalPositionXZ: 1
      heightFromFeet: {{from_feet}}
      mirror: 0
      bodyMask: 01000000010000000100000001000000010000000100000001000000010000000100000001000000010000000100000001000000010000000100000001000000
      curves: []
      events: []
      transformMask: []
      maskType: 3
      maskSource: {{instanceID: 0}}
      additiveReferencePoseFrame: 0"""
    body = body.replace('{orig_y}', '0' if feet else '1').replace('{from_feet}', '1' if feet else '0')
    return _MODEL_HEAD.format(guid=guid(rel), externals="", clips=body, import_anim=1, avatar_setup=1).replace("  externalObjects:\n  materials:", "  externalObjects: {}\n  materials:")


def prop_meta(rel, materials=None):
    """A held prop FBX: a static mesh (no rig, no animation), materials bound by name to the given {name: mat-rel}."""
    text = model_meta(rel, materials)
    return text.replace("animationType: 3", "animationType: 0").replace("avatarSetup: 1", "avatarSetup: 0")
