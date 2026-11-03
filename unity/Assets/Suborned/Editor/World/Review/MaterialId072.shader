// Brief 072-F (Latch, 2026-10-27): the material-id pass of After072's screen-area census. Each (material, layer) of the scene gets a twin
// material with this shader and its id in _IdColor (24 bits, linear); the play camera renders the frame into a float target with post,
// SSAO and fog off, and the pixels are counted per id. What the frame shows is kept: the architecture fade's dithered cut (DK_Cutaway, the
// same globals and keyword the real shaders read, only on materials whose own shader has the cutting variant: _CanCut), alpha-tested
// maps (_ALPHATEST_ON with the source's _BaseMap and _Cutoff), the source's cull mode. _Skip drops a material (the opaque-only render).
// Never used in the game; editor review only.
Shader "Hidden/Suborned/MaterialId072"
{
    Properties
    {
        _IdColor("Id", Color) = (0, 0, 0, 1)
        _BaseMap("Base", 2D) = "white" {}
        _Cutoff("Cutoff", Float) = 0.5
        _Skip("Skip", Float) = 0
        _CanCut("Can cut", Float) = 0
        _Cull("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Assets/Suborned/Shaders/DecoKit/DK_Cutaway.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _IdColor;
            float4 _BaseMap_ST;
            float _Cutoff;
            float _Skip;
            float _CanCut;
            float _Cull;
        CBUFFER_END
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

        struct IdAttributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
        struct IdVaryings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };

        IdVaryings IdVert(IdAttributes i)
        {
            IdVaryings o;
            UNITY_SETUP_INSTANCE_ID(i);
            UNITY_TRANSFER_INSTANCE_ID(i, o);
            o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
            o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
            return o;
        }

        void IdClip(IdVaryings i)
        {
            UNITY_SETUP_INSTANCE_ID(i);
            if (_Skip > 0.5) clip(-1.0);
        #if defined(_ALPHATEST_ON)
            clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).a - _Cutoff);
        #endif
            if (_CanCut > 0.5) DKCutaway(i.positionCS);
        }

        float4 IdFrag(IdVaryings i) : SV_Target { IdClip(i); return float4(_IdColor.rgb, 1.0); }
        float4 IdDepthFrag(IdVaryings i) : SV_Target { IdClip(i); return 0; }
        float4 IdNormalsFrag(IdVaryings i) : SV_Target { IdClip(i); return float4(0, 0, 1, 0); }
        ENDHLSL

        Pass
        {
            Name "Id"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull] ZWrite On ZTest LEqual
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex IdVert
            #pragma fragment IdFrag
            #pragma multi_compile_fragment _ _DK_CUT
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            Cull [_Cull] ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex IdVert
            #pragma fragment IdDepthFrag
            #pragma multi_compile_fragment _ _DK_CUT
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            Cull [_Cull] ZWrite On
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex IdVert
            #pragma fragment IdNormalsFrag
            #pragma multi_compile_fragment _ _DK_CUT
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            ENDHLSL
        }
    }
}
