// Water (brief 007 round 2): the district's water and the horizon sea share this material, so the two never seam. Lit by the main
// light (shadows received), with a Fresnel gradient toward a lighter horizon colour (the lagoon mixed toward the haze), a warm sheen
// toward the sun, distance fog, and the sea fading fully into the haze from _FadeFrom (metres about _FadeCentre, plan x/y) to _FadeTo.
// Colours are data: _BaseColor = look `water`, _Horizon and _Haze from the look's fog colour; the builder sets them (Look.Water).
Shader "Suborned/Water"
{
    Properties
    {
        _BaseColor ("Water (look `water`)", Color) = (0.24, 0.55, 0.58, 1)
        _Horizon ("Grazing colour (lighter toward the horizon)", Color) = (0.6, 0.78, 0.76, 1)
        _Haze ("Haze colour", Color) = (0.95, 0.91, 0.83, 1)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.6
        _Fresnel ("Fresnel strength", Range(0, 1)) = 0.85
        _Sheen ("Sun sheen strength", Range(0, 2)) = 0.6
        _FadeCentre ("Fade centre (plan x, y)", Vector) = (0, 0, 0, 0)
        _FadeFrom ("Fade from (m)", Float) = 4000
        _FadeTo ("Fade to (m)", Float) = 8000
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor, _Horizon, _Haze;
                half _Smoothness, _Fresnel, _Sheen;
                float4 _FadeCentre; float _FadeFrom, _FadeTo;
            CBUFFER_END
            struct A { float4 pos : POSITION; float3 nrm : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 pos : SV_POSITION; float3 wpos : TEXCOORD0; float3 nrm : TEXCOORD1; float fog : TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO };
            V vert(A a)
            {
                V v; UNITY_SETUP_INSTANCE_ID(a); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(v);
                v.wpos = TransformObjectToWorld(a.pos.xyz);
                v.pos = TransformWorldToHClip(v.wpos);
                v.nrm = TransformObjectToWorldNormal(a.nrm);
                v.fog = ComputeFogFactor(v.pos.z);
                return v;
            }
            half4 frag(V v) : SV_Target
            {
                float3 n = normalize(v.nrm);
                float3 view = normalize(GetWorldSpaceViewDir(v.wpos));
                Light sun = GetMainLight(TransformWorldToShadowCoord(v.wpos));
                half shadow = sun.shadowAttenuation;
                half ndl = saturate(dot(n, sun.direction));
                half3 ambient = SampleSH(n);
                half3 col = _BaseColor.rgb * (sun.color * ndl * shadow + ambient);
                // Fresnel (Schlick, F0 0.02): lighter toward the horizon
                half nv = saturate(dot(n, view));
                half f = 0.02 + 0.98 * pow(1 - nv, 5);
                col = lerp(col, _Horizon.rgb * (0.55 + 0.45 * ndl), saturate(f * _Fresnel + (1 - nv) * 0.35 * _Fresnel));
                // warm sheen toward the sun: a tight highlight and a broad glow, both in the sun's colour
                float3 r = reflect(-view, n);
                half rl = saturate(dot(r, sun.direction));
                half spec = pow(rl, lerp(8, 512, _Smoothness)) * 4 + pow(rl, 12) * 0.25;
                col += sun.color * spec * _Sheen * shadow;
                col = MixFog(col, v.fog);
                // the sea dissolves into the haze toward the far shore
                float d = distance(v.wpos.xz, _FadeCentre.xy);
                col = lerp(col, _Haze.rgb, smoothstep(_FadeFrom, _FadeTo, d));
                return half4(col, 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 pos : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 pos : SV_POSITION; };
            V vert(A a) { V v; UNITY_SETUP_INSTANCE_ID(a); v.pos = TransformObjectToHClip(a.pos.xyz); return v; }
            half frag(V v) : SV_Target { return v.pos.z; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 pos : POSITION; float3 nrm : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 pos : SV_POSITION; float3 nrm : TEXCOORD0; };
            V vert(A a) { V v; UNITY_SETUP_INSTANCE_ID(a); v.pos = TransformObjectToHClip(a.pos.xyz); v.nrm = TransformObjectToWorldNormal(a.nrm); return v; }
            half4 frag(V v) : SV_Target { return half4(NormalizeNormalPerPixel(v.nrm), 0); }
            ENDHLSL
        }
    }
}
