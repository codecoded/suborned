// Brief 101 (the hover outline, HostViews.Outline): the material of the outline's stand-ins and twins (the Office door's arched panel, the
// cabinet's box, a service door's leaf, and a copy of each plain mesh the outline draws). They are drawn only into the outline's mask, by
// Suborned.HoverOutlinePass with its own material: this pass is here so that pass's renderer list finds them (LightMode SubornedOutline), and
// no pass of the camera's own (forward, depth, shadows, motion) has anything to draw. No DOTS instancing on purpose: the GPU Resident Drawer
// leaves these renderers alone, so turning one on or off rebuilds nothing.
Shader "Hidden/Suborned/HoverOutlineStandIn"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "StandIn"
            Tags { "LightMode" = "SubornedOutline" }
            ZWrite Off ColorMask 0
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 Vert(float4 positionOS : POSITION) : SV_POSITION { return TransformObjectToHClip(positionOS.xyz); }
            half4 Frag(float4 positionCS : SV_POSITION) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
