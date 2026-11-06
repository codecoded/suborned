"""Brief 097 (main session, 2026-11-06): the DecoKit cutaway shaders' two new opt-in material keywords, written from URP 17.6.0's own pass file
plus asserted, marked edits, as brief 077's make_leaf_hlsl.py writes the leaf passes.

  vertex AO      (Lit Cutaway, Complex Lit Cutaway; ForwardLit; no keyword: a mesh without colours reads white): occlusion = the vertex colour's R (tools/decokit vertex_ao.py bakes it in Blender;
                 a mesh without colours reads white = none). It multiplies URP's occlusion (indirect diffuse and specular) and, by DK_VERTEX_AO_DIRECT,
                 the albedo, so a contact still reads in sun. The global _DKVertexAOOff (1) turns it off for the Air's A/B without a keyword change.
  _DK_INTERIOR   (Lit Cutaway, Complex Lit Cutaway; ForwardLit, fragment): a room behind the glass (DK_Interior097.hlsl), off with _DKInteriorOff.
  _DK_WIND       (all three cutaway shaders; ForwardLit, ShadowCaster, DepthOnly, DepthNormals): DK_Wind097.hlsl's sway and flutter on the
                 object-space position before the pass's vertex function.

Writes Shaders/DecoKit/DK_Forward097.hlsl and patches DK_LitCutaway.shader, DK_ComplexLitCutaway.shader and DK_Cutaway.hlsl in place, then re-runs brief 077's
make_leaf_hlsl.py so DK_LitCutawayLeaf.shader follows the Lit shader (each edit asserted once; a file already patched is left as it is).
Usage: python3 -I tools/shaders/make_dk097.py   (from the repo; reads the URP package from unity/Library/PackageCache)"""
import hashlib
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
SH = REPO / "unity/Assets/Suborned/Shaders/DecoKit"
URP = next((REPO / "unity/Library/PackageCache").glob("com.unity.render-pipelines.universal@*"))
STOCK_SHA = "7f209a93cbb316ad9e79192985f4d5ced99324714daf3d2686cd4c109bc9111c"  # URP 17.6.0 LitForwardPass.hlsl (the leaf copy's source too)
MARK = "brief 097"


def edit(src, pairs, where):
    for a, b in pairs:
        n = src.count(a)
        assert n == 1, (where, a[:90], n)
        src = src.replace(a, b)
    return src


def forward(check=False):
    stock = URP / "Shaders/LitForwardPass.hlsl"
    sha = hashlib.sha256(stock.read_bytes()).hexdigest()
    assert sha == STOCK_SHA, f"URP's LitForwardPass.hlsl changed ({sha}): review the edits below against the new file"
    src = edit(stock.read_text(), [
        ('#include "LitInput.hlsl"', '#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"'),
        ("    float2 dynamicLightmapUV  : TEXCOORD2;\n    UNITY_VERTEX_INPUT_INSTANCE_ID\n};",
         "    float2 dynamicLightmapUV  : TEXCOORD2;\n#if 1 // brief 097: the vertex AO is keyword-free (a mesh without colours reads white)\n    half4 color         : COLOR; // DK 097: the baked AO in R\n#endif\n    UNITY_VERTEX_INPUT_INSTANCE_ID\n};"),
        ("    float4 positionCS               : SV_POSITION;\n\n    UNITY_VERTEX_INPUT_INSTANCE_ID",
         "#if 1 // brief 097: the vertex AO is keyword-free (a mesh without colours reads white)\n    half dkAO                       : COLOR; // DK 097\n#endif\n    float4 positionCS               : SV_POSITION;\n\n    UNITY_VERTEX_INPUT_INSTANCE_ID"),
        ("    output.positionCS = vertexInput.positionCS;\n\n    return output;",
         "#if 1 // brief 097: the vertex AO is keyword-free (a mesh without colours reads white)\n    output.dkAO = input.color.r; // DK 097\n#endif\n    output.positionCS = vertexInput.positionCS;\n\n    return output;"),
        ("    InitializeStandardLitSurfaceData(input.uv, surfaceData);\n",
         "    InitializeStandardLitSurfaceData(input.uv, surfaceData);\n"
         "    // DK 097: the baked vertex AO into URP's occlusion (indirect light) and, by DK_VERTEX_AO_DIRECT, into the albedo (so a contact reads in sun)\n"
         "#if 1 // brief 097: the vertex AO is keyword-free (a mesh without colours reads white)\n"
         "    half dkAO = lerp(half(1.0), saturate(input.dkAO), half(1.0) - half(_DKVertexAOOff));\n"
         "    surfaceData.occlusion *= dkAO;\n"
         "    surfaceData.albedo *= lerp(half(1.0), dkAO, half(DK_VERTEX_AO_DIRECT));\n"
         "#endif\n"
         "#if defined(_DK_INTERIOR)\n"
         "    { // DK 097: a room behind the glass (DK_Interior097.hlsl), as emission under the glass's Fresnel; the pane's own colour darkened to tinted glass\n"
         "        float3 dkP = ComputeWorldSpacePosition(GetNormalizedScreenSpaceUV(input.positionCS), input.positionCS.z, UNITY_MATRIX_I_VP);\n"
         "        half3 dkV = GetWorldSpaceNormalizeViewDir(dkP); half3 dkN = normalize(input.normalWS);\n"
         "        half dkF = half(0.04) + half(0.96) * pow(half(1.0) - saturate(dot(dkN, dkV)), half(5.0));\n"
         "        half dkOn = half(1.0) - half(_DKInteriorOff);\n"
         "        surfaceData.emission += DKInterior(dkP, dkN, dkV) * (half(1.0) - dkF) * dkOn;\n"
         "        surfaceData.albedo *= lerp(half(1.0), half(DK_INTERIOR_GLASS), dkOn);\n"
         "    }\n"
         "#endif\n"),
    ], "LitForwardPass")
    head = (f"// Brief 097: DecoKit/Lit Cutaway's and Complex Lit Cutaway's ForwardLit pass with the baked vertex AO (the colour's R) and the rooms behind the glass.\n"
            f"// URP 17.6.0's LitForwardPass.hlsl (sha256 {STOCK_SHA}) with the edits marked \"DK 097\": the vertex AO always (a mesh with no colour stream reads white = none,\n// so no keyword doubles the variants); the rooms behind the glass under _DK_INTERIOR.\n"
            f"// Written by tools/shaders/make_dk097.py: do not edit; to follow a URP upgrade, re-run it on the new file.\n"
            "#pragma shader_feature_local_fragment _DK_INTERIOR\n"
            "#ifndef DK_VERTEX_AO_GLOBALS\n#define DK_VERTEX_AO_GLOBALS\n"
            "float _DKVertexAOOff;                 // global: 1 = no vertex AO (the Air's A/B); unset = 0 = on\n"
            "#define DK_VERTEX_AO_DIRECT 0.45      // the share of the AO that also darkens direct light (contact shade in sun)\n"
            "float _DKInteriorOff;                 // global: 1 = no rooms behind the glass (the Air's A/B); unset = 0 = on\n"
            "#define DK_INTERIOR_GLASS 0.35        // what stays of the pane's colour over a room (tinted glass)\n#endif\n"
            "#include \"DK_Interior097.hlsl\"\n")
    text = head + src + "\n"
    if check:
        ok = (SH / "DK_Forward097.hlsl").read_text() == text
        print("PASS" if ok else "FAIL", "DK_Forward097.hlsl")
        return ok
    (SH / "DK_Forward097.hlsl").write_text(text)
    print("DK_Forward097.hlsl written")
    return True


WIND = '            #pragma shader_feature_local _DK_WIND // brief 097 (not _vertex: a stage-only keyword paired the leaf copy vertex and fragment programs wrongly on Metal)\n            #include "DK_Wind097.hlsl"\n'


def patch_shader(name, forward_stock):
    p = SH / name
    s = p.read_text()
    if MARK in s:
        print(name, "already patched"); return
    pairs = [
        ("            #pragma vertex LitPassVertex\n", "            #pragma vertex DKLitPassVertex // brief 097: DK_Wind097.hlsl's wrapper\n"),
        ("            #pragma vertex DepthOnlyVertex\n", "            #pragma vertex DKDepthOnlyVertex // brief 097\n"),
        ("            #pragma vertex DepthNormalsVertex\n", "            #pragma vertex DKDepthNormalsVertex // brief 097\n"),
    ]
    for d in ("DK_CUT_FORWARD", "DK_CUT_SHADOW", "DK_CUT_DEPTH", "DK_CUT_DEPTH_NORMALS"):
        pairs.append((f'            #define {d}\n            #include "DK_Cutaway.hlsl"', f'            #define {d}\n{WIND}            #include "DK_Cutaway.hlsl"'))
    if forward_stock:
        pairs.append(('            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"\n',
                      '            #include_with_pragmas "DK_Forward097.hlsl" // brief 097: URP\'s LitForwardPass.hlsl with the vertex AO and _DK_INTERIOR\n'))
    p.write_text(edit(s, pairs, name))
    print(name, "patched")


def patch_cutaway():
    p = SH / "DK_Cutaway.hlsl"
    s = p.read_text()
    if MARK in s:
        print("DK_Cutaway.hlsl already patched"); return
    s = edit(s, [
        ("#define DK_CUT_LAYER (1u << 20)  // Suborned.ArchitectureFade.Layer\n",
         "#define DK_CUT_LAYER (1u << 20)  // Suborned.ArchitectureFade.Layer\n#ifndef DK_WIND_OS\n#define DK_WIND_OS(p) // brief 097: DK_Wind097.hlsl's wind, where a shader includes it\n#endif\n"),
        ("    Varyings v = ShadowPassVertex(input);\n", "    DK_WIND_OS(input.positionOS.xyz); // brief 097: the same wind as the camera's passes\n    Varyings v = ShadowPassVertex(input);\n"),
    ], "DK_Cutaway.hlsl")
    p.write_text(s)
    print("DK_Cutaway.hlsl patched")


def check():
    """--check: nothing written; the forward copy must be what forward() writes and every patched file must carry its marks."""
    ok = forward(check=True)
    for f, marks in (("DK_LitCutaway.shader", ("DKLitPassVertex", "DK_Wind097.hlsl", "DK_Forward097.hlsl")), ("DK_ComplexLitCutaway.shader", ("DKLitPassVertex", "DK_Wind097.hlsl", "DK_Forward097.hlsl")),
                     ("DK_LitCutawayLeaf.shader", ("DKLitPassVertex", "DK_Wind097.hlsl")), ("DK_Cutaway.hlsl", ("DK_WIND_OS(input.positionOS.xyz)",))):
        s = (SH / f).read_text(); miss = [m for m in marks if m not in s]
        print("PASS" if not miss else "FAIL", f, "missing " + ", ".join(miss) if miss else ""); ok = ok and not miss
    return ok


if __name__ == "__main__":
    if "--check" in sys.argv:
        sys.exit(0 if check() else 1)
    forward()
    patch_cutaway()
    patch_shader("DK_LitCutaway.shader", True)
    patch_shader("DK_ComplexLitCutaway.shader", True)
    # the leaf sibling is brief 077's generator's output from DK_LitCutaway.shader (tools/kit/leaf-shader.ts checks it byte for byte): re-run it
    import subprocess
    r = subprocess.run(["python3", "-I", str(REPO / "briefs/077/shader/make_leaf_hlsl.py"), str(URP), str(SH)], capture_output=True, text=True)
    print(r.stdout.strip()); assert r.returncode == 0, r.stderr
    sys.exit(0)
