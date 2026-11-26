Shader "Suborned/Harbour/Shop windows"
{
    Properties
    {
        _InteriorAtlas("Four occupied rooms", 2D) = "white" {}
        _Shop("Shop variation", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        HLSLINCLUDE
        #pragma target 4.5
        #pragma multi_compile_instancing
        #pragma multi_compile_fragment _ _DK_CUT
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "DK_Cutaway.hlsl"
        TEXTURE2D(_InteriorAtlas); SAMPLER(sampler_InteriorAtlas);
        CBUFFER_START(UnityPerMaterial)
            float _Shop;
        CBUFFER_END
        struct Vertex { float4 pos : POSITION; float3 normal : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
        struct Pixel { float4 pos : SV_POSITION; float3 world : TEXCOORD0; half3 normal : TEXCOORD1; float3 local : TEXCOORD2; half fog : TEXCOORD3; };
        Pixel Vert(Vertex v)
        {
            UNITY_SETUP_INSTANCE_ID(v);
            Pixel o;
            o.local = v.pos.xyz;
            o.world = TransformObjectToWorld(v.pos.xyz);
            o.normal = TransformObjectToWorldNormal(v.normal);
            o.pos = TransformWorldToHClip(o.world);
            o.fog = ComputeFogFactor(o.pos.z);
            return o;
        }
        ENDHLSL
        Pass
        {
            Name "Windows"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Window
            #pragma multi_compile_fog
            half4 Window(Pixel i) : SV_Target
            {
                float3 hiddenGround;
                float veil = DKCutaway(i.pos, hiddenGround);
                half3 n = normalize(i.normal);
                half3 v = GetWorldSpaceNormalizeViewDir(i.world);
                float3 tangent = normalize(float3(n.z, 0, -n.x) + float3(0.0001, 0, 0));
                float along = dot(i.world, tangent);
                bool upper = i.local.y > 4.0;
                float height = upper ? frac((i.local.y - 4.30) / 2.05) : saturate((i.local.y - 0.45) / 2.94);
                float room = along / 4.35;
                float2 front = float2(frac(room), height);
                // A shallow reveal meets each jamb before a grazing ray reaches the back wall.
                float incidence = max(0.06, dot(n, v));
                float2 travel = -float2(dot(tangent, v) / 4.35, v.y / 2.94) * (0.90 / incidence);
                float2 inset = front + travel;
                float2 outside = max(-inset, inset - 1);
                half reveal = smoothstep(0.0, 0.10, max(outside.x, outside.y));
                float tile = upper ? 0.0 : frac((floor(room) + _Shop) * .25) * 4.0;
                float2 tilePos = float2(fmod(tile, 2.0), floor(tile * .5));
                float2 sampleAt = (tilePos + clamp(inset, .008, .992)) * .5;
                float2 continuous = (float2(room, i.local.y / 2.94) + travel) * .5;
                half3 back = SAMPLE_TEXTURE2D_GRAD(_InteriorAtlas, sampler_InteriorAtlas, sampleAt, ddx(continuous), ddy(continuous)).rgb;
                half day = saturate(dot(unity_AmbientSky.rgb, half3(.24,.64,.12)) * 1.6);
                half3 inside = back * lerp(half3(.85,.63,.41), half3(.94,.94,.88), day);
                half3 jamb = lerp(half3(.10,.065,.045), half3(.40,.31,.19), saturate(inset.y));
                inside = lerp(inside, jamb * (.68 + .32 * day), reveal * .82);
                half3 mirrorRay = reflect(-v, n);
                half3 reflection = lerp(half3(.20,.18,.13), half3(.35,.56,.63), smoothstep(-.25,.58,mirrorRay.y));
                half fresnel = pow(1 - saturate(dot(n, v)), 3);
                half3 glass = lerp(inside, reflection * (.12 + .88 * day), .095 + .60 * fresnel);
                glass = lerp(glass, _DKCutVeil.rgb, veil);
                return half4(MixFog(glass, i.fog), 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly" Tags { "LightMode"="DepthOnly" }
            ColorMask 0 ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Depth
            half4 Depth(Pixel i) : SV_Target { DKCutaway(i.pos); return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals" Tags { "LightMode"="DepthNormalsOnly" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Normals
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            half4 Normals(Pixel i) : SV_Target
            {
                DKCutaway(i.pos);
                half3 n = normalize(i.normal);
                #ifdef _GBUFFER_NORMALS_OCT
                    return half4(PackFloat2To888(saturate(PackNormalOctQuadEncode(n) * .5 + .5)), 0);
                #else
                    return half4(n, 0);
                #endif
            }
            ENDHLSL
        }
    }
}
