// Brief 101 (Sky's idea, 2026-11-09; Glyph's look B, briefs/101/glyph/outline/notes.md): the hover outline's passes, drawn by
// Suborned.HoverOutlinePass. What a left click would act on is drawn into a mask the size of its screen box (the camera's view cropped to
// the box, 1:1 with the target's pixels), with its own depth; the line is drawn from that mask after post, so the kit's ink and cream land as
// they are: ink #17302f 1.5 px at 92 %, then cream #f7f4ec 1 px at 85 %, both outside the thing's edge, at 1080p and scaled with the
// target's height (the pass gives the widths and the colours). Where something nearer the camera than the thing stands, the line is hidden;
// a person the silhouettes draw through cover keeps the line round the silhouette, and only another person in front hides it.
//  · MaskA / MaskB: the thing fading in (A) and the one fading out (B): R / G = the part that makes a line (above its clip height: a mast's
//    head, not its post), B / A = all of it (no line is drawn over the thing itself). DOTS instancing, so the GPU Resident Drawer's
//    renderers draw too.
//  · People: every person's depth (SilhouettePass.PersonLayer), only while a silhouetted person is outlined: is the cover a person?
//  · Edge: the line, over the box only (a quad from the vertex id), blended over the picture.
Shader "Hidden/Suborned/HoverOutline"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        float4 _OutlineClip; // x: thing A's clip height (world y), y: thing B's; below it a thing hides a line and makes none
        struct MaskIn { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
        struct MaskV { float4 positionCS : SV_POSITION; float worldY : TEXCOORD0; };
        MaskV MaskVert(MaskIn i)
        {
            MaskV o = (MaskV)0;
            UNITY_SETUP_INSTANCE_ID(i);
            float3 ws = TransformObjectToWorld(i.positionOS.xyz);
            o.positionCS = TransformWorldToHClip(ws); o.worldY = ws.y;
            return o;
        }
        ENDHLSL

        Pass
        {
            Name "MaskA"
            Tags { "LightMode" = "SubornedOutlineA" }
            ZTest LEqual ZWrite On Cull Off ColorMask RB
            HLSLPROGRAM
            #pragma vertex MaskVert
            #pragma fragment FragA
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            half4 FragA(MaskV i) : SV_Target { return half4(i.worldY >= _OutlineClip.x ? 1.0 : 0.0, 0.0, 1.0, 0.0); }
            ENDHLSL
        }

        Pass
        {
            Name "MaskB"
            Tags { "LightMode" = "SubornedOutlineB" }
            ZTest LEqual ZWrite On Cull Off ColorMask GA
            HLSLPROGRAM
            #pragma vertex MaskVert
            #pragma fragment FragB
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            half4 FragB(MaskV i) : SV_Target { return half4(0.0, i.worldY >= _OutlineClip.y ? 1.0 : 0.0, 0.0, 1.0); }
            ENDHLSL
        }

        Pass
        {
            Name "People"
            Tags { "LightMode" = "SubornedOutlinePeople" }
            ZTest LEqual ZWrite On Cull Off ColorMask 0
            HLSLPROGRAM
            #pragma vertex MaskVert
            #pragma fragment FragP
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            half4 FragP(MaskV i) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "Edge"
            Tags { "LightMode" = "SubornedOutlineEdge" }
            ZTest Always ZWrite Off Cull Off
            Blend One OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex EdgeVert
            #pragma fragment EdgeFrag
            #pragma target 4.5
            TEXTURE2D(_OutlineMask);
            TEXTURE2D_FLOAT(_OutlineDepthA);
            TEXTURE2D_FLOAT(_OutlineDepthB);
            TEXTURE2D_FLOAT(_OutlinePeople);
            TEXTURE2D_FLOAT(_OutlineScene);
            SamplerState outline_point_clamp_sampler;
            float4 _OutlineBox;    // the box on the target: x0, y0 (px from the bottom left), width, height
            float4 _OutlineTarget; // the target's width and height (px); z: clip y's sign (-1 where the target is drawn flipped)
            float4 _OutlineRows;   // x: the masks' rows run from the top (1) or the bottom (0); y: the scene depth's
            float4 _OutlineAlpha;  // x, y: A and B (their fades); z, w: A, B are silhouetted people (1)
            float4 _OutlineWidths; // x: ink (px), y: cream (px), z: the search reach (px)
            float4 _OutlineDepths; // x: near, y: far (m); z: cover counts when nearer by this (m); w: a person is the cover within this (m)
            float4 _OutlineInk, _OutlineCream; // linear colour, alpha

            struct EdgeV { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            EdgeV EdgeVert(uint id : SV_VertexID)
            {
                float2 c = float2(id == 1 || id == 2 || id == 4 ? 1.0 : 0.0, id == 2 || id == 4 || id == 5 ? 1.0 : 0.0);
                float2 px = _OutlineBox.xy + c * _OutlineBox.zw;
                float2 ndc = px / _OutlineTarget.xy * 2.0 - 1.0;
                EdgeV o; o.positionCS = float4(ndc.x, ndc.y * _OutlineTarget.z, 0.5, 1.0); o.uv = c;
                return o;
            }

            int2 Row(int2 p, float h, float top) { return int2(p.x, top > 0.5 ? (int)h - 1 - p.y : p.y); }
            float Eye(float raw)
            {
            #if UNITY_REVERSED_Z
                return _OutlineDepths.y - (_OutlineDepths.y - _OutlineDepths.x) * raw;
            #else
                return _OutlineDepths.x + (_OutlineDepths.y - _OutlineDepths.x) * raw;
            #endif
            }
            bool Empty(float raw)
            {
            #if UNITY_REVERSED_Z
                return raw <= 0.0;
            #else
                return raw >= 1.0;
            #endif
            }

            // the line at distance d (px, centre to centre) from the thing's nearest texel, whose depth is `raw`; empty where the cover hides it
            half4 Line(float d, float raw, float scene, float people, float silhouette, float alpha)
            {
                float e = d - 0.5, wi = _OutlineWidths.x, wc = _OutlineWidths.y;
                float ink = saturate(wi - e + 0.5), cream = max(0.0, saturate(wi + wc - e + 0.5) - ink);
                if (ink + cream <= 0.0) return 0;
                bool covered = scene < Eye(raw) - _OutlineDepths.z;
                if (covered && silhouette > 0.5) covered = !Empty(people) && Eye(people) < scene + _OutlineDepths.w; // the silhouette shows through cover, not through a person
                if (covered) return 0;
                float ia = ink * _OutlineInk.a, ca = cream * _OutlineCream.a;
                return half4(_OutlineInk.rgb * ia + _OutlineCream.rgb * ca, ia + ca) * alpha;
            }

            half4 EdgeFrag(EdgeV i) : SV_Target
            {
                float w = _OutlineBox.z, h = _OutlineBox.w;
                int2 p = int2(floor(i.uv * _OutlineBox.zw));
                float4 here = LOAD_TEXTURE2D(_OutlineMask, Row(p, h, _OutlineRows.x));
                bool wantA = _OutlineAlpha.x > 0.0 && here.b < 0.5, wantB = _OutlineAlpha.y > 0.0 && here.a < 0.5;
                if (!wantA && !wantB) discard;
                float r = _OutlineWidths.z; int R = (int)ceil(r);
                float bestA = 1e9, bestB = 1e9; int2 qa = int2(0, 0), qb = int2(0, 0);
                [loop] for (int dy = -R; dy <= R; dy++)
                {
                    [loop] for (int dx = -R; dx <= R; dx++)
                    {
                        float d2 = (float)(dx * dx + dy * dy); if (d2 > r * r) continue;
                        int2 q = p + int2(dx, dy); if (q.x < 0 || q.y < 0 || q.x >= (int)w || q.y >= (int)h) continue;
                        float4 m = LOAD_TEXTURE2D(_OutlineMask, Row(q, h, _OutlineRows.x));
                        if (m.r > 0.5 && d2 < bestA) { bestA = d2; qa = q; }
                        if (m.g > 0.5 && d2 < bestB) { bestB = d2; qb = q; }
                    }
                }
                wantA = wantA && bestA < 1e8; wantB = wantB && bestB < 1e8;
                if (!wantA && !wantB) discard;
                float2 suv = (_OutlineBox.xy + float2(p) + 0.5) / _OutlineTarget.xy; if (_OutlineRows.y > 0.5) suv.y = 1.0 - suv.y;
                float scene = Eye(SAMPLE_TEXTURE2D_LOD(_OutlineScene, outline_point_clamp_sampler, suv, 0).r);
                float people = LOAD_TEXTURE2D(_OutlinePeople, Row(p, h, _OutlineRows.x)).r;
                half4 col = 0;
                if (wantB) col = Line(sqrt(bestB), LOAD_TEXTURE2D(_OutlineDepthB, Row(qb, h, _OutlineRows.x)).r, scene, people, _OutlineAlpha.w, _OutlineAlpha.y);
                if (wantA) { half4 a = Line(sqrt(bestA), LOAD_TEXTURE2D(_OutlineDepthA, Row(qa, h, _OutlineRows.x)).r, scene, people, _OutlineAlpha.z, _OutlineAlpha.x); col = a + col * (1.0 - a.a); }
                if (col.a <= 0.0) discard;
                return col;
            }
            ENDHLSL
        }
    }
}
