using System;
using System.Linq;
using Suborned.Play;
using Suborned.Sim;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Suborned.World.Play
{
    static partial class DistrictFerryGardens
    {
        const string Art = "Assets/Suborned/Materials/Play/Ferry";
        static Texture2D Sheet(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(Art + name + ".png")
            ?? throw new BuildFailedException("Missing Harbour Ferries sheet " + name);

        sealed class Kit
        {
            readonly GameObject prefab;
            readonly Material finish;
            readonly float[] thresholds;
            public Kit(string name, Material finish, params float[] thresholds)
            {
                prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Suborned/Scenery/"+name+".fbx");
                if(!prefab) throw new BuildFailedException("Missing Harbour scenery " + name);
                this.finish=finish; this.thresholds=thresholds;
            }

            public MeshRenderer Place(Transform parent, Vector3 at, Quaternion heading, float widthScale=1f)
            {
                var holder=new GameObject(prefab.name).transform;
                holder.SetParent(parent,false);
                var model=Object.Instantiate(prefab,holder).transform;
                var markers=model.GetComponentsInChildren<Transform>(true);
                var east=markers.Single(t=>t.name=="East");
                var north=markers.Single(t=>t.name=="North");
                // Use the authored metre and compass. FBX importers can place the conversion on the root or its children.
                Vector3 axis=north.position-model.position;
                model.localScale/=axis.magnitude;
                model.rotation=Quaternion.FromToRotation(axis.normalized,Vector3.forward)*model.rotation;
                if(Vector3.Dot(east.position-model.position,Vector3.right)<0) holder.localScale=new Vector3(-1,1,1);
                holder.localScale=Vector3.Scale(holder.localScale,new Vector3(widthScale,1,1));
                holder.SetPositionAndRotation(at,heading);
                var tiers=model.GetComponentsInChildren<MeshRenderer>(true).OrderByDescending(r=>r.GetComponent<MeshFilter>().sharedMesh.vertexCount).ToArray();
                if(tiers.Length!=3) throw new BuildFailedException(prefab.name+" needs three authored LODs");
                foreach(var r in tiers)
                {
                    r.sharedMaterial=finish;
                    r.renderingLayerMask|=ArchitectureFade.Layer;
                    r.reflectionProbeUsage=ReflectionProbeUsage.Simple;
                }
                tiers[2].shadowCastingMode=ShadowCastingMode.Off;
                var group=model.GetComponent<LODGroup>();
                if(!group) group=holder.gameObject.AddComponent<LODGroup>();
                group.SetLODs(tiers.Select((r,i)=>new LOD(thresholds[i],new Renderer[]{r})).ToArray());
                group.RecalculateBounds();
                Object.DestroyImmediate(east.gameObject);
                Object.DestroyImmediate(north.gameObject);
                return tiers[2];
            }
        }

        public static void Install(Scene scene, Transform root, SiteFrame frame, DistrictGround ground)
        {
            var outer=ground.Waterfront.Where(s=>s.Id=="quay/3").OrderByDescending(s=>s.B.Y).First();
            var shore=frame.World(new Pt(outer.Centre.X,outer.B.Y));
            shore.y=outer.EastCm*.01f;
            var east=frame.Dir(new Pt(0,100)).normalized;
            var north=frame.Dir(new Pt(-100,0)).normalized;
            var heading=Quaternion.LookRotation(north);
            var paint=new Material(Shader.Find("DecoKit/Lit Cutaway")) { name="Harbour stone, glazing and planted terraces", enableInstancing=true };
            paint.SetTexture("_BaseMap",Sheet("Colour"));
            paint.SetTexture("_MetallicGlossMap",Sheet("Metal")); paint.EnableKeyword("_METALLICSPECGLOSSMAP");
            paint.SetFloat("_Smoothness",.93f);
            paint.SetTexture("_EmissionMap",Sheet("Lights")); paint.SetColor("_EmissionColor",new Color(.83f,.74f,.61f));
            paint.EnableKeyword("_EMISSION");

            var hall=new Kit("FerryPavilion",paint,.36f,.105f,.0035f);
            var pavilion=hall.Place(root,shore+east*12.70f,heading);
            int beds=ResidentialGardens(scene,root,frame,ground,paint,heading);
            var sea=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<MeshRenderer>())
                .FirstOrDefault(r=>r.name=="Sea" && r.transform.parent && r.transform.parent.name=="Harbour");
            if(!sea || !sea.sharedMaterial.HasProperty("_Waves")) throw new BuildFailedException("Harbour Ferries requires the played harbour sea");
            var water=new Material(sea.sharedMaterial){name="Sheltered harbour water"};
            water.SetVector("_Waves",new Vector4(53,17,.24f,.80f));
            water.SetVector("_Sun",new Vector4(280,3.8f,0,0));
            water.SetVector("_Sky",new Vector4(.29f,.66f,0,0));
            water.SetColor("_Deep",new Color(.031f,.224f,.305f));
            water.SetColor("_Scatter",new Color(.07f,.42f,.38f));
            sea.sharedMaterial=water;
            Reflection(root,pavilion,sea.bounds.center.y,paint);
            Debug.Log($"Harbour Ferries at {pavilion.bounds.center}, {pavilion.bounds.size} m, {beds} residential roof beds above the public streets");
        }

        static void Reflection(Transform root, MeshRenderer source, float level, Material finish)
        {
            var shader=Shader.Find("Suborned/Harbour Reflection");
            if(!shader) throw new BuildFailedException("Missing Harbour Ferries reflection shader");
            var mirror=new GameObject("Harbour Ferries reflection").transform;
            mirror.SetParent(root,false); mirror.position=new Vector3(0,2*level,0); mirror.localScale=new Vector3(1,-1,1);
            // Keep the imported mesh's axis conversion, metre and handedness by copying its actual transform chain.
            var chain=new System.Collections.Generic.Stack<Transform>();
            for(var t=source.transform;t!=root;t=t.parent) chain.Push(t);
            var parent=mirror;
            foreach(var src in chain)
            {
                var next=new GameObject(src.name).transform; next.SetParent(parent,false);
                next.localPosition=src.localPosition; next.localRotation=src.localRotation; next.localScale=src.localScale; parent=next;
            }
            parent.gameObject.AddComponent<MeshFilter>().sharedMesh=source.GetComponent<MeshFilter>().sharedMesh;
            var r=parent.gameObject.AddComponent<MeshRenderer>();
            var mat=new Material(shader){name="Quayside water reflection"};
            mat.SetTexture("_BaseMap",finish.GetTexture("_BaseMap"));mat.SetTexture("_EmissionMap",finish.GetTexture("_EmissionMap"));mat.SetFloat("_WaterY",level);
            r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
            r.lightProbeUsage=LightProbeUsage.Off;r.reflectionProbeUsage=ReflectionProbeUsage.Off;
            var group=parent.gameObject.AddComponent<LODGroup>();
            group.SetLODs(new[]{new LOD(.05f,new Renderer[]{r})});group.RecalculateBounds();
        }
    }
}
