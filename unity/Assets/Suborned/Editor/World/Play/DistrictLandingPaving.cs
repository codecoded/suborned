using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.World.Play
{
    sealed partial class DistrictStreetArchitecture
    {
        // A flat terrazzo compass and matching twin edge bands. The paving remains on the surveyed deck.
        // Four different stone surfaces share one renderer. There is no decal projector or added shadow pass.
        void LandingPaving(Transform parent, Vector3 edgeA, Vector3 edgeB, Vector3 inward, float deckY, bool market)
        {
            var along = (edgeB-edgeA).normalized;
            var centre = Vector3.Lerp(edgeA,edgeB,market ? .30f : .50f) + inward*(market ? 7.85f : 6.7f);
            centre.y=deckY+.022f;
            var s = new Surface(4);
            Vector3 Polar(float angle, float radius) => new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*radius;
            void Ring(int mat, float inner, float outer)
            {
                const int segments=64;
                for (int i=0;i<segments;i++)
                {
                    float a=i*Mathf.PI*2/segments, b=(i+1)*Mathf.PI*2/segments;
                    s.Face(mat,Polar(a,outer),Polar(b,outer),Polar(b,inner),Polar(a,inner),true);
                }
            }
            Ring(0,2.98f,3.18f); Ring(1,2.84f,2.92f); Ring(2,2.71f,2.80f);
            for (int i=0;i<16;i++)
            {
                float a=i*Mathf.PI/8, r=i%4==0 ? 2.69f : i%2==0 ? 2.02f : 1.34f;
                var tip=Polar(a,r); var left=Polar(a-Mathf.PI/16,.53f); var right=Polar(a+Mathf.PI/16,.53f);
                s.Face(0,Vector3.zero,left,tip,tip,true);
                s.Face(i%4==0 ? 2 : 1,Vector3.zero,tip,right,right,true);
            }
            Ring(3,0,.24f);
            // The pair of stripes follows the water so the edge can be read in motion and at tactical zoom.
            void Band(int mat, float from, float to)
            {
                var a=edgeA-centre+along*.40f; var b=edgeB-centre-along*.40f; a.y=b.y=0;
                var q=a+inward*from; var r=b+inward*from; var v=b+inward*to; var u=a+inward*to;
                if (Vector3.Cross(r-q,v-q).y>0) s.Face(mat,q,r,v,u,true); else s.Face(mat,u,v,r,q,true);
            }
            Band(0,.55f,.78f); Band(1,.86f,.97f);
            var go=new GameObject(market ? "Market Pier compass paving" : "Skyport compass paving");
            go.transform.SetParent(parent,false); go.transform.position=centre;
            go.AddComponent<MeshFilter>().sharedMesh=s.Finish(go.name);
            var renderer=go.AddComponent<MeshRenderer>();
            renderer.sharedMaterials=new[]{Colour("Tile_Teal",false),Colour("Terrazzo_Seafoam",false),Colour("Terrazzo_Ivory",false),Colour("Bronze",false)};
            renderer.shadowCastingMode=ShadowCastingMode.Off;
        }
    }
}
