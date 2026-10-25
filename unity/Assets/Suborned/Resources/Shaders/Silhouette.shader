// Brief 056 F (Sky 2026-10-25: "the soft circle cut with silhouettes"): the override material of Suborned.SilhouettePass. A person who
// matters (the cell, hostiles, key people: rendering-layer bit 22) is drawn again, flat, only where something nearer the camera covers it,
// so nobody who matters is ever lost behind a wall the capsule cut cannot reach. Behaviour from the lab (testbed/controls/occlusion.js
// addSilhouette), not its look: the colours are the kit's ink and cream until Glint tunes them.
//  · Mark: every person (bit 21) at its own visible depth writes stencil bit 128, so another person in front never takes a silhouette over
//    them (the lab drew silhouettes before the people for the same reason).
//  · Edge: the person grown along its normals by _EdgeM, depth Greater, where no person is visible: a cream rim.
//  · Fill: the person, depth Greater, the same stencil test: the flat fill inside the rim.
// Both silhouette passes test a depth pulled _MarginM towards the camera, so a person never silhouettes over itself or over a kerb its feet
// dip into: only what stands that much nearer counts as cover.
Shader "Hidden/Suborned/Silhouette"
{
    Properties
    {
        _Fill ("Fill", Color) = (0.090, 0.188, 0.184, 1)
        _Edge ("Edge", Color) = (0.969, 0.957, 0.925, 1)
        _EdgeM ("Edge width (m)", Float) = 0.035
        _MarginM ("Depth margin (m)", Float) = 0.35
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _Fill;
            half4 _Edge;
            float _EdgeM;
            float _MarginM;
        CBUFFER_END
        struct SilAttributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
        struct SilVaryings { float4 positionCS : SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };
        SilVaryings SilVert(SilAttributes i, float grow, float margin)
        {
            SilVaryings o = (SilVaryings)0;
            UNITY_SETUP_INSTANCE_ID(i);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            float3 ws = TransformObjectToWorld(i.positionOS.xyz);
            ws += normalize(TransformObjectToWorldNormal(i.normalOS)) * grow;
            ws -= GetViewForwardDir() * margin; // towards the camera: the orthographic view does not move it on the screen
            o.positionCS = TransformWorldToHClip(ws);
            return o;
        }
        ENDHLSL

        Pass
        {
            Name "Mark"
            ZTest LEqual ZWrite Off ColorMask 0 Cull Back
            Offset -1, -1
            Stencil { Ref 128 ReadMask 128 WriteMask 128 Comp Always Pass Replace }
            HLSLPROGRAM
            #pragma vertex MarkVert
            #pragma fragment MarkFrag
            SilVaryings MarkVert(SilAttributes i) { return SilVert(i, 0.0, 0.0); }
            half4 MarkFrag(SilVaryings i) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "Edge"
            ZTest Greater ZWrite Off Cull Back
            Stencil { Ref 128 ReadMask 128 Comp NotEqual }
            HLSLPROGRAM
            #pragma vertex EdgeVert
            #pragma fragment EdgeFrag
            SilVaryings EdgeVert(SilAttributes i) { return SilVert(i, _EdgeM, _MarginM); }
            half4 EdgeFrag(SilVaryings i) : SV_Target { return _Edge; }
            ENDHLSL
        }

        Pass
        {
            Name "Fill"
            ZTest Greater ZWrite Off Cull Back
            Stencil { Ref 128 ReadMask 128 Comp NotEqual }
            HLSLPROGRAM
            #pragma vertex FillVert
            #pragma fragment FillFrag
            SilVaryings FillVert(SilAttributes i) { return SilVert(i, 0.0, _MarginM); }
            half4 FillFrag(SilVaryings i) : SV_Target { return _Fill; }
            ENDHLSL
        }
    }
}
