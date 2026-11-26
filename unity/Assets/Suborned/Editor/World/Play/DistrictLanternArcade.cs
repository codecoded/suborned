using System.Linq;
using Suborned.Play;
using Suborned.Sim;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.World.Play
{
    sealed partial class DistrictStreetArchitecture
    {
        sealed partial class Surface
        {
            public void Pane(int material, Vector3 a, Vector3 b, Vector3 c, Vector3 d, float u0, float u1)
            {
                int i=vertices.Count;
                vertices.AddRange(new[] {a,b,c,d});
                uv.AddRange(new[] {new Vector2(u0,0),new Vector2(u1,0),new Vector2(u1,1),new Vector2(u0,1)});
                indices[material].AddRange(new[] {i,i+1,i+2,i,i+2,i+3});
            }
        }

        const float Spring = 4.90f, Rise = 3.26f;

        static Vector3 Vault(float radius, float t, float z, float offset)
        {
            float a = (t-.5f)*Mathf.PI;
            var normal = new Vector3(Mathf.Sin(a)*Rise,Mathf.Cos(a)*radius,0).normalized;
            return new Vector3(radius*Mathf.Sin(a),Spring + Rise*Mathf.Cos(a),z) + normal*offset;
        }

        static Mesh Ironwork(float radius, float halfLength, bool near)
        {
            var s = new Surface(5);
            int steps = near ? 32 : 14;
            void Rib(int mat,float z,float depth,float lower,float upper)
            {
                for(int k=0;k<steps;k++)
                {
                    float a=(float)k/steps,b=(float)(k+1)/steps;
                    var al=Vault(radius,a,z-depth,lower); var bl=Vault(radius,b,z-depth,lower);
                    var ah=Vault(radius,a,z-depth,upper); var bh=Vault(radius,b,z-depth,upper);
                    var v=Vector3.forward*(depth*2);
                    s.Face(mat,ah,bh,bl,al); s.Face(mat,al+v,bl+v,bh+v,ah+v);
                    s.Face(mat,ah,ah+v,bh+v,bh); s.Face(mat,al,bl,bl+v,al+v);
                }
            }
            foreach(float z in new[]{-halfLength,0,halfLength})
            {
                bool portal=z!=0;
                Rib(1,z,portal?.21f:.14f,-.19f,.11f);
                if (portal)
                {
                    Rib(0,z,.16f,.79f,.99f);
                    Rib(2,z,.224f,-.17f,-.105f);
                    Rib(2,z,.176f,.83f,.90f);
                    for(int k=0;k<steps;k++)
                    {
                        float a=(float)k/steps,b=(float)(k+1)/steps;
                        var al=Vault(radius,a,z-.134f,.10f); var bl=Vault(radius,b,z-.134f,.10f);
                        var ah=Vault(radius,a,z-.134f,.81f); var bh=Vault(radius,b,z-.134f,.81f);
                        s.Pane(4,bl,al,ah,bh,1-b,1-a);
                        var v=Vector3.forward*.268f;
                        s.Pane(4,al+v,bl+v,bh+v,ah+v,a,b);
                    }
                }
                for(int side=-1;side<=1;side+=2)
                {
                    float wall=side*radius;
                    // Brackets and spreader plates are cantilevered from the surveyed walls.
                    s.Solid(0,wall,z,.72f,.94f,3.56f,2.00f,.16f);
                    s.Solid(1,wall,z,.96f,1.09f,5.32f,.29f,.18f);
                    s.Solid(2,wall,z,.98f,1.11f,5.52f,.075f,.20f);
                    s.Beam(1,new Vector3(wall,3.83f,z),Vault(radius,side<0?.14f:.86f,z,-.12f),.16f,.18f);
                    if(near)
                        for(int q=-1;q<=1;q++)
                            s.Beam(2,new Vector3(wall-side*.37f,3.78f,z+q*.18f),
                                new Vector3(wall-side*.37f,5.27f,z+q*.18f),.055f,.065f);
                }
            }
            for(int k=1;k<12;k++)
            {
                float t=k/12f;
                s.Beam(k%3==0?0:1,Vault(radius,t,-halfLength,.07f),Vault(radius,t,halfLength,.07f),
                    k%3==0?.12f:.063f,k%3==0?.16f:.085f);
            }
            for(int side=-1;side<=1;side+=2)
            {
                float x=side*radius;
                s.Beam(1,new Vector3(x,Spring,-halfLength),new Vector3(x,Spring,halfLength),.30f,.32f);
                s.Beam(2,new Vector3(x,Spring+.14f,-halfLength),new Vector3(x,Spring+.14f,halfLength),.34f,.055f);
            }
            // A porcelain rosette above each attachment keeps the ground clear.
            foreach(float z in new[]{-halfLength,halfLength})
                s.Solid(3,0,z,.32f,.43f,Spring+Rise-.29f,.18f,.16f);
            return s.Finish(near ? "Leaded arcade with fluted brackets" : "Arcade silhouette");
        }

        Material LeadedFrieze()
        {
            var tex=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Suborned/Materials/Play/LanternGlass.jpg");
            if(!tex) throw new BuildFailedException("The Lantern arcade transom glass is missing");
            var m=new Material(Colour("Enamel_Ivory",true)){name="Lantern arcade • leaded seaglass"};
            m.SetColor("_BaseColor",Color.white); m.SetTexture("_BaseMap",tex); m.SetFloat("_Metallic",.14f);
            m.SetFloat("_Smoothness",.63f);
            m.EnableKeyword("_EMISSION");m.SetTexture("_EmissionMap",tex);m.SetColor("_EmissionColor",new Color(.43f,.42f,.34f));
            return m;
        }

        public void LanternArcade(Transform root, SiteFrame frame, DistrictMap map)
        {
            GalleryPaving(root,frame,map.Ground.Galleries);
            var street = map.Streets.Single(s=>s.Id=="road/lantern-lane").Path.Points;
            var mouth=frame.World(street[0]); var into=frame.World(street[1])-mouth;
            float length=into.magnitude; into/=length;
            var facing=-into;
            var right=Vector3.Cross(Vector3.up,facing).normalized;
            var candidates=map.Ground.Galleries.Select(b=>(bay:b,p:frame.World(b.At)))
                .Where(b=>Vector3.Dot(b.p-mouth,right) is > 3 and < 11 &&
                    Vector3.Dot(b.p-mouth,into) is > 0 && Vector3.Dot(b.p-mouth,into)<length)
                .OrderBy(b=>Vector3.Dot(b.p-mouth,into));
            foreach(var (bay,p) in candidates)
            {
                float half=bay.WidthCm*.005f;
                // Find an uninterrupted opposite wall to carry all three ribs.
                float span=0;
                for(float d=14;d<=25;d+=.25f)
                {
                    var other=p-right*d;
                    if(!new[]{-half,0,half}.All(z=>map.Ground.IsWall(frame.Engine(other+facing*z)))) continue;
                    span=d;break;
                }
                if(span==0) continue;
                var centre=p-right*(span*.5f);centre.y=.025f;
                float radius=span*.5f+.34f;
                var station=new GameObject("Lantern Terraces • glass arcade").transform;
                station.SetParent(root,false);station.SetPositionAndRotation(centre,Quaternion.LookRotation(facing));
                var mats=new[]{Colour("Limestone_Tex",true),Colour("Enamel_Teal",true),Colour("Bronze",true),Colour("Enamel_Ivory",true),LeadedFrieze()};
                Draw(station,"Vault and transoms",centre,station.eulerAngles.y,(Ironwork(radius,half,true),Ironwork(radius,half,false)),mats,true);
                RoofGlass(station,radius,half);
                ArcadeLantern(station);
                Debug.Log($"Lantern arcade • span {radius*2:F1} m, length {half*2:F1} m, centre {centre}, opening faces {facing}");
                return;
            }
            throw new BuildFailedException("Lantern Terraces has no paired walls for the glass arcade");
        }

        void RoofGlass(Transform parent,float radius,float half)
        {
            var s=new Surface(1);
            for(int k=1;k<11;k++)
            {
                float a=k/12f,b=(k+1)/12f;
                s.Face(0,Vault(radius,a,-half,.12f),Vault(radius,a,half,.12f),Vault(radius,b,half,.12f),Vault(radius,b,-half,.12f));
            }
            var glass=new Material(Colour("Glass",true)){name="Open arcade • clear wired glass"};
            glass.SetColor("_BaseColor",new Color(.64f,.87f,.89f,.11f));
            glass.SetFloat("_Cull",0);glass.SetFloat("_Smoothness",.76f);
            var go=new GameObject("Clear panes");go.transform.SetParent(parent,false);
            go.AddComponent<MeshFilter>().sharedMesh=s.Finish(go.name);
            var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=glass;r.shadowCastingMode=ShadowCastingMode.Off;
            r.renderingLayerMask|=ArchitectureFade.Layer;
        }
    }
}
