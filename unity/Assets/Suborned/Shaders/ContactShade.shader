// Contact shade (brief 026): the ambient occlusion where things meet the ground, laid as thin multiply strips and blobs on the ground
// (World/Light/ContactShade.cs). Glint's finding: URP SSAO does nothing at the orthographic play zoom (the depth slopes it reads are
// flat there), and the old city's SSAO 0.4 -> 0.9 changed no play-zoom pixel (briefs/026/old-decokit/compare-template.txt), so the
// contact darkening is geometry: drawn the same at every zoom, one draw per material per tile, no lighting, no shadow.
//   _Mode 0 (strip): UV v = 0 at the contact line (a wall's foot, a kerb face) to 1 at the strip's far edge; u unused.
//   _Mode 1 (blob):  UV = x, z in [-1, 1] across the piece's footprint ellipse.
// The darkening = _Strength x falloff, towards _Tint (a cool shade, not black). Fog fades it to nothing (multiply by white).
Shader "Suborned/Contact Shade"
{
    Properties
    {
        _Tint("Shade colour", Color) = (0.55, 0.53, 0.6, 1)
        _Strength("Strength", Range(0, 1)) = 0.5
        _Power("Falloff power", Float) = 2.0
        [Enum(Strip,0,Blob,1)] _Mode("Mode", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent-200" "IgnoreProjector" = "True" }
        Pass
        {
            Name "ContactShade"
            Tags { "LightMode" = "UniversalForward" }
            Blend DstColor Zero
            ZWrite Off
            Cull Off
            Offset -1, -1
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint; half _Strength; half _Power; half _Mode;
            CBUFFER_END

            struct A { float4 pos : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; half fog : TEXCOORD1; };

            V vert(A i)
            {
                V o;
                o.pos = TransformObjectToHClip(i.pos.xyz);
                o.uv = i.uv;
                o.fog = ComputeFogFactor(o.pos.z);
                return o;
            }

            half4 frag(V i) : SV_Target
            {
                half k = _Mode < 0.5 ? pow(saturate(1.0h - i.uv.y), _Power) : pow(saturate(1.0h - length(i.uv)), _Power);
                half3 c = lerp(half3(1, 1, 1), _Tint.rgb, saturate(k * _Strength));
                c = MixFogColor(c, half3(1, 1, 1), i.fog);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
