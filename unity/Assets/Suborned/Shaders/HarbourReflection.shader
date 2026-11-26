// One mirrored silhouette from the actual pavilion. Project its depth onto the water so
// the opaque quay and passers-by occlude it. The existing sea supplies its own colour and glints.
Shader "Suborned/Harbour Reflection"
{
    Properties
    {
        _BaseMap("Stone, glass and planting",2D)="white"{}
        _EmissionMap("Window light",2D)="black"{}
        _WaterY("Water surface height",Float)=-2
    }
    SubShader
    {
        Tags {"RenderType"="Transparent" "Queue"="Transparent-60" "RenderPipeline"="UniversalPipeline"}
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);
            CBUFFER_START(UnityPerMaterial)
                float _WaterY;
            CBUFFER_END
            struct Input { float4 position:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0; };
            struct Vary { float4 position:SV_POSITION;float3 water:TEXCOORD0;half3 normal:TEXCOORD1;float2 uv:TEXCOORD2;half fog:TEXCOORD3;float depth:TEXCOORD4; };
            Vary vert(Input i)
            {
                Vary o; float3 mirror=TransformObjectToWorld(i.position.xyz);
                float3 cameraPos=GetCameraPositionWS();
                // Project along the actual view rays in either a perspective or tactical orthographic camera.
                float3 ray=lerp(mirror-cameraPos,GetViewForwardDir(),unity_OrthoParams.w);
                float depth=max(0,_WaterY-mirror.y);
                float3 water=mirror+ray*((_WaterY-mirror.y)/min(-.0001,ray.y));
                float wave=sin(dot(water.xz,float2(.54,.82))+_Time.y*1.24)+.34*sin(dot(water.xz,float2(1.89,-.78))-_Time.y);
                mirror.xz+=float2(.11,.073)*wave*saturate(depth*.30);
                float4 clipPos=TransformWorldToHClip(mirror);
                float4 plane=TransformWorldToHClip(water+float3(0,.028,0));
                clipPos.z=plane.z/max(.0001,plane.w)*clipPos.w;
                o.position=clipPos; o.water=water; o.depth=_WaterY-mirror.y;
                o.normal=TransformObjectToWorldNormal(i.normal)*half3(1,-1,1);
                o.uv=i.uv; o.fog=ComputeFogFactor(plane.z);
                return o;
            }
            half4 frag(Vary i):SV_Target
            {
                clip(i.depth);
                half3 base=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb;
                half3 lamps=SAMPLE_TEXTURE2D(_EmissionMap,sampler_EmissionMap,i.uv).rgb;
                Light sun=GetMainLight();
                half3 n=normalize(i.normal);
                half3 reflected=base*(SampleSH(n)+sun.color*saturate(dot(n,sun.direction))*.70)+lamps*.72;
                float ripples=sin(i.water.z*3.3+i.water.x*.71+_Time.y*1.6)+.56*sin(i.water.x*1.8-i.water.z*4.1-_Time.y);
                half grazing=1-saturate(GetWorldSpaceNormalizeViewDir(i.water).y);
                half opacity=(.11+.34*grazing*grazing)*(.75+.17*ripples);
                return half4(MixFog(reflected*half3(.70,.85,.91),i.fog),opacity);
            }
            ENDHLSL
        }
    }
}
