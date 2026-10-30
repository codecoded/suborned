// Brief 079 note 2 (Vela). Modes 0 to 3 draw exactly as before (their _Fall 1, _Absorb 0 are the defaults, and Blend One OneMinusSrcAlpha with alpha 0 is additive).
//   _Mode 4 (beam, soft): the open cone, 12 sides: brightest at the nozzle, down to _Floor at the ground by v^_Fall, and fading across its width with the surface's turn from the camera (|N.V|^_Soft), so it has no hard edge and no flat strip.
//   _Mode 5 (ring spill): the disc grown to r 1.05 m: the clock ring's own light on the paving, both sides of the ring, gone by _Radius away from _Inner (the ring's centre line, m). Nothing at the disc's centre: no pale plate.
//   _Absorb: this share of what is beneath is taken away where it shines, so on sunlit cream the light tints the paving rather than bleaching it to white.
// Fog fades it to nothing (colour black), as before; alpha fades with it.
Shader "Suborned/Cross Glow"
{
    Properties
    {
        _BaseColor("Colour", Color) = (0.608, 0.812, 0.761, 1)
        _Strength("Strength", Float) = 2.0
        _Alpha("Alpha", Range(0, 1)) = 0.28
        [Enum(Beam,0,Halo,1,Field,2,Pool,3,BeamSoft,4,RingSpill,5)] _Mode("Mode", Float) = 0
        _Floor("Floor", Range(0, 1)) = 0.25
        _Inner("Inner radius (m)", Float) = 0.15
        _Radius("Radius (m)", Float) = 0.47
        _Fall("Fall-off power", Float) = 1
        _Soft("Beam softness", Float) = 2
        _Absorb("Tint the surface by", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            Name "CrossGlow"
            Tags { "LightMode" = "UniversalForward" }
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor; half _Strength; half _Alpha; half _Mode; half _Floor; half _Inner; half _Radius; half _Fall; half _Soft; half _Absorb;
            CBUFFER_END

            struct A { float4 pos : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; half fog : TEXCOORD1; float3 nrm : TEXCOORD2; float3 view : TEXCOORD3; UNITY_VERTEX_OUTPUT_STEREO };

            V vert(A i)
            {
                V o; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 ws = TransformObjectToWorld(i.pos.xyz);
                o.pos = TransformWorldToHClip(ws);
                o.uv = i.uv;
                o.fog = ComputeFogFactor(o.pos.z);
                o.nrm = TransformObjectToWorldNormal(i.normal);
                o.view = GetWorldSpaceNormalizeViewDir(ws); // the orthographic play camera's direction too
                return o;
            }

            half4 frag(V i) : SV_Target
            {
                half k;
                if (_Mode < 0.5) k = lerp(_Floor, 1.0h, saturate(i.uv.y));
                else if (_Mode < 1.5) k = lerp(1.0h, _Floor, smoothstep(_Inner, _Radius, length(i.uv)));
                else if (_Mode < 2.5) k = pow(saturate(1.0h - abs(i.uv.y) / _Radius), 1.5h);
                else if (_Mode < 3.5) k = lerp(_Floor, 1.0h, pow(saturate(1.0h - i.uv.y), 1.5h));
                else if (_Mode < 4.5) k = lerp(_Floor, 1.0h, pow(saturate(i.uv.y), _Fall)) * pow(abs(dot(normalize(i.nrm), normalize(i.view))), _Soft);
                else k = pow(saturate(1.0h - abs(length(i.uv) - _Inner) / _Radius), _Fall);
                half3 c = _BaseColor.rgb * (_Strength * _Alpha * k);
                c = MixFogColor(c, half3(0, 0, 0), i.fog);
                half a = k * _Absorb; a = MixFogColor(half3(a, a, a), half3(0, 0, 0), i.fog).r; // fades with the fog as the colour does; with _Absorb 0 alpha is 0, as before
                return half4(c, a);
            }
            ENDHLSL
        }
    }
}
