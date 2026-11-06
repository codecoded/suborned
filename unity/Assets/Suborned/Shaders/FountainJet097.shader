// Brief 097: the atom fountain's jets and landing rings as the testbed drew them (fountain-water.js: "reflective flowing fountain streams",
// "jet landing ripples"), on the piece's own UVs (the testbed's, kept by fountain_tb.py). _Mode 0, a jet: a flow of bright and thin beads running
// along u (sin(u * 62 - t * 12)), a Fresnel lean to the street's reflection probe, a little sun; _Mode 1, a landing ring: rings running out from the
// centre of its UV square. Transparent, no depth write, no shadow. The sun's glint on a stream is clamped so the play bloom (clamp 4) takes it as
// sparkle, never a white bar (GDD §15 less shine).
Shader "Suborned/Fountain Jet"
{
    Properties
    {
        _BaseColor("Colour", Color) = (0.74, 0.894, 0.91, 0.5)
        _Emission("Emission", Color) = (0.102, 0.325, 0.376, 0.055)
        [Enum(Jet,0,Ring,1)] _Mode("Mode", Float) = 0
        _Flow("Flow (beads per u, speed)", Vector) = (62, 12, 0, 0)
        _Glint("Sun glint (power, max)", Vector) = (220, 2.5, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor; half4 _Emission; half _Mode; float4 _Flow; float4 _Glint;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; half3 normalWS : TEXCOORD1; float2 uv : TEXCOORD2; half fog : TEXCOORD3; UNITY_VERTEX_INPUT_INSTANCE_ID };
            V vert(A i)
            {
                V o = (V)0; UNITY_SETUP_INSTANCE_ID(i); UNITY_TRANSFER_INSTANCE_ID(i, o);
                VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS = p.positionCS; o.positionWS = p.positionWS; o.normalWS = TransformObjectToWorldNormal(i.normalOS); o.uv = i.uv; o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }
            half4 frag(V i, half face : VFACE) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float t = _Time.y;
                half3 n = normalize(i.normalWS) * (face > 0 ? 1.0h : -1.0h);
                half3 v = GetWorldSpaceNormalizeViewDir(i.positionWS);
                half a = _BaseColor.a; half3 c = _BaseColor.rgb;
                if (_Mode < 0.5)
                {
                    float flow = sin(i.uv.x * _Flow.x - t * _Flow.y);
                    float bead = pow(saturate(sin(i.uv.x * _Flow.x * 0.37 - t * _Flow.y * 0.83) * 0.5 + 0.5), 6.0);
                    a *= 0.88 + 0.12 * flow; c *= 0.92 + 0.08 * flow + 0.25 * bead;
                }
                else
                {
                    float ring = length(i.uv - 0.5) * 2.0;
                    a *= (0.5 + 0.5 * sin(ring * 20.0 - t * 6.0)) * saturate((1.0 - ring) * 3.0);
                }
                Light sun = GetMainLight(TransformWorldToShadowCoord(i.positionWS), i.positionWS, half4(1, 1, 1, 1));
                half lit = sun.shadowAttenuation * sun.distanceAttenuation;
                half3 diffuse = c * (SampleSH(n) + sun.color * (0.35 + 0.65 * saturate(dot(n, sun.direction))) * lit);
                half fres = pow(1.0h - saturate(abs(dot(n, v))), 3.0h);
                half3 refl = GlossyEnvironmentReflection(reflect(-v, n), i.positionWS, 0.08h, 1.0h, GetNormalizedScreenSpaceUV(i.positionCS));
                half spec = min(pow(saturate(dot(reflect(-sun.direction, n), v)), _Glint.x) * lit * 8.0h, _Glint.y);
                half3 col = diffuse + refl * (0.2h + 0.6h * fres) + sun.color * spec + _Emission.rgb * _Emission.a * 4.0h;
                a = saturate(a + fres * 0.25h + spec * 0.2h);
                col = MixFog(col, i.fog);
                return half4(col, a);
            }
            ENDHLSL
        }
    }
}
