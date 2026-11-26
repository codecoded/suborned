Shader "Suborned/Harbour/Shop windows"
{
    Properties
    {
        _InteriorAtlas("Four occupied rooms", 2D) = "white" {}
        _Shop("Shop variation", Float) = 0
        [Toggle] _FittedSash("Window fitted to a single surveyed opening", Float) = 0
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
            float _FittedSash;
        CBUFFER_END
        struct Vertex { float4 pos : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; float2 cell : TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID };
        struct Pixel { float4 pos : SV_POSITION; float3 world : TEXCOORD0; half3 normal : TEXCOORD1; float3 local : TEXCOORD2; half fog : TEXCOORD3; float4 pane : TEXCOORD4; };
        Pixel Vert(Vertex v)
        {
            UNITY_SETUP_INSTANCE_ID(v);
            Pixel o;
            o.local = v.pos.xyz;
            o.pane = float4(v.uv,v.cell);
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
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            half3 FittedSash(Pixel i, half3 n, half3 v)
            {
                float2 uv = i.pane.xy;
                float aspect = clamp(i.pane.w, .3, 4);
                float2 edge = min(uv,1-uv) * float2(aspect,1);
                float nearest = min(edge.x,edge.y);
                float aa = max(.001, fwidth(nearest));
                // A slight reveal at each jamb, with the pane tucked behind a projecting sill.
                // Keep the sash in place as the view moves. Only the room inside it shifts.
                float3 tangent = normalize(float3(-n.z,0,n.x) + float3(.00001,0,0));
                float2 slope = float2(dot(tangent,v)/aspect,v.y) / max(.16,dot(v,n));
                float2 behind = uv - clamp(slope,-2.5,2.5) * .045 * smoothstep(.04,.18,nearest);
                float2 cell = float2(fmod(i.pane.z,2.0),floor(i.pane.z*.5));
                float2 atlas = (cell + clamp(behind,.01,.99)) * .5;
                half3 room = SAMPLE_TEXTURE2D_GRAD(_InteriorAtlas,sampler_InteriorAtlas,atlas,ddx(uv)*.5,ddy(uv)*.5).rgb;
                half day = saturate(dot(unity_AmbientSky.rgb,half3(.24,.64,.12))*1.8);
                half3 reflected = reflect(-v,n);
                half fres = .045 + .36 * pow(1-saturate(dot(n,v)),4);
                half3 sky = lerp(half3(.14,.12,.09),half3(.42,.52,.57),smoothstep(-.1,.55,reflected.y));
                half3 pane = lerp(room*(.42+.61*day),sky*(.16+.84*day),fres);
                half occlusion = .73 + .27 * smoothstep(.015,.105,min(1-uv.y,edge.x));
                half rim = 1-smoothstep(.015-aa,.015+aa,nearest);
                half sill = 1-smoothstep(.018-aa,.018+aa,uv.y);
                return lerp(pane*occlusion,lerp(half3(.095,.080,.056),half3(.33,.27,.20),sill)*(.34+.66*day),rim);
            }
            half4 Window(Pixel i) : SV_Target
            {
                float3 hiddenGround;
                float veil = DKCutaway(i.pos, hiddenGround);
                half3 n = normalize(i.normal);
                half3 v = GetWorldSpaceNormalizeViewDir(i.world);
                if (_FittedSash > .5)
                    return half4(MixFog(lerp(FittedSash(i,n,v),_DKCutVeil.rgb,veil),i.fog),1);
                float3 tangent = normalize(float3(n.z, 0, -n.x) + float3(0.0001, 0, 0));
                float along = dot(i.world, tangent);
                bool upper = i.local.y > 4.0;
                float height = upper ? frac((i.local.y - 4.30) / 2.05) : saturate((i.local.y - 0.45) / 2.94);
                float room = along / 4.35;
                float2 front = float2(frac(room), height);
                // A shallow reveal meets each jamb before a grazing ray reaches the back wall.
                float incidence = max(0.06, dot(n, v));
                float2 travel = -float2(dot(tangent, v) / 4.35, v.y / 2.94) * (0.62 / incidence);
                float2 inset = front + travel;
                float2 outside = max(-inset, inset - 1);
                float edge = max(outside.x, outside.y);
                float aa = clamp(fwidth(edge), .001, .035);
                half reveal = smoothstep(-aa, aa, edge);
                float tile = upper ? 0.0 : frac((floor(room) + _Shop) * .25) * 4.0;
                float2 tilePos = float2(fmod(tile, 2.0), floor(tile * .5));
                float2 sampleAt = (tilePos + clamp(inset, .008, .992)) * .5;
                float2 continuous = (float2(room, i.local.y / 2.94) + travel) * .5;
                half3 back = SAMPLE_TEXTURE2D_GRAD(_InteriorAtlas, sampler_InteriorAtlas, sampleAt, ddx(continuous), ddy(continuous)).rgb;
                half day = saturate(dot(unity_AmbientSky.rgb, half3(.24,.64,.12)) * 1.6);
                half3 inside = back * lerp(half3(.72,.67,.55), half3(.75,.80,.80), day);
                Light sun = GetMainLight(TransformWorldToShadowCoord(i.world));
                half lightOnGlass = saturate(dot(n,sun.direction)) * sun.shadowAttenuation;
                half jambShade = smoothstep(0,.17,min(front.x,1-front.x)) * smoothstep(0,.15,1-front.y);
                inside *= lerp(1, (.54 + .31*lightOnGlass) * (.80 + .20*jambShade), day);
                // A ray hitting the side sees a linen reveal, the slab or the display floor. Sampling the
                // edge of the picture here stretched shelving into horizontal stripes as the scout walked past.
                float2 sideT = (step(0,travel) - front) / (sign(travel + 1e-7) * max(abs(travel),1e-5));
                float depth = saturate(min(sideT.x,sideT.y));
                half3 jamb = lerp(half3(.27,.25,.18), half3(.14,.125,.095), depth);
                if (sideT.y < sideT.x) jamb = travel.y > 0 ? half3(.36,.34,.27) : half3(.18,.115,.085);
                inside = lerp(inside, jamb * (.46 + .54 * day), reveal);
                if (upper)
                {
                    float blind = .25 + .39 * frac(floor(room) * .382 + _Shop * .17);
                    float slats = smoothstep(.32,.70,frac(i.world.y * 8.0));
                    slats = lerp(slats,.5,saturate(fwidth(i.world.y)*18));
                    inside = lerp(inside,half3(.45,.41,.30) * (.82 + .13 * slats), smoothstep(blind,blind+.018,height)*.84);
                }
                half3 mirrorRay = reflect(-v, n);
                half3 reflection = lerp(half3(.20,.18,.13), half3(.35,.56,.63), smoothstep(-.25,.58,mirrorRay.y));
                half4 probe = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0,samplerunity_SpecCube0,mirrorRay,3);
                reflection = lerp(reflection,DecodeHDREnvironment(probe,unity_SpecCube0_HDR),.52);
                half fresnel = pow(1 - saturate(dot(n, v)), 3);
                half3 glass = lerp(inside, reflection * (.12 + .88 * day), (upper ? .31 : .16) + .57 * fresnel);
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
