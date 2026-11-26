using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.World.Play
{
    sealed partial class DistrictStreetArchitecture
    {
        // A flyby bay can have fifteen paint, glass and hardware slots. At a distance where its whole facade
        // occupies a tenth of the screen, bake the actual material colours into a single opaque draw instead.
        const int Swatches = 32;
        readonly Dictionary<Mesh,Mesh> distantFronts = new();
        readonly Dictionary<string,Material> distantPaint = new();

        static bool DecoratedBay(string name) => name.StartsWith("DK_Stream") || name.StartsWith("DK_PortholeBay") || name.StartsWith("DK_BalconyBay");

        static Mesh FarMesh(Mesh src)
        {
            var vertices=src.vertices; var normals=src.normals;
            var xyz=new List<Vector3>(); var ns=new List<Vector3>(); var uv=new List<Vector2>(); var tris=new List<int>();
            int level=Mathf.Min(3,src.lodCount-1);
            using var raw=Mesh.AcquireReadOnlyMeshData(src);
            var data=raw[0]; bool wide=data.indexFormat==IndexFormat.UInt32;
            var index32=wide ? data.GetIndexData<int>() : default;
            var index16=wide ? default : data.GetIndexData<ushort>();
            for(int slot=0;slot<src.subMeshCount;slot++)
            {
                var seen=new Dictionary<int,int>();
                var sub=data.GetSubMesh(slot); var range=src.GetLod(slot,level);
                for(int i=0;i<range.indexCount;i++)
                {
                    int index=sub.indexStart+checked((int)range.indexStart)+i;
                    int old=sub.baseVertex+(wide ? index32[index] : index16[index]);
                    if(!seen.TryGetValue(old,out int at))
                    {
                        at=xyz.Count; seen.Add(old,at); xyz.Add(vertices[old]); ns.Add(normals[old]);
                        uv.Add(new Vector2((slot+.5f)/Swatches,.5f));
                    }
                    tris.Add(at);
                }
            }
            var mesh=new Mesh {name=src.name+" distant enamel"};
            mesh.SetVertices(xyz); mesh.SetNormals(ns); mesh.SetUVs(0,uv); mesh.SetTriangles(tris,0);
            mesh.bounds=src.bounds;
            return mesh;
        }

        Material FarPaint(Material[] slots)
        {
            var key=string.Join("/",slots.Select(m=>m ? m.GetEntityId().ToString() : "null"));
            if(distantPaint.TryGetValue(key,out var made)) return made;
            var colours=new Color[Swatches];
            for(int i=0;i<slots.Length;i++)
            {
                var mat=slots[i];
                var c=mat && mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : new Color(.54f,.55f,.49f);
                if(mat && mat.name.Contains("Glass")) c=new Color(.38f,.56f,.56f);
                if(mat && mat.name.StartsWith("Harbour shop glass")) c=new Color(.50f,.38f,.26f);
                if(mat && mat.name.Contains("Chrome")) c=new Color(.72f,.76f,.73f);
                c.a=1; colours[i]=c;
            }
            var strip=new Texture2D(Swatches,1,TextureFormat.RGBA32,false) {name="Distant frontage enamel samples",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            strip.SetPixels(colours);strip.Apply(false,true);
            var shader=Shader.Find("DecoKit/Lit Cutaway");
            if(!shader) throw new BuildFailedException("Missing facade lighting for Harbour distance meshes");
            made=new Material(shader) {name="Distant enamel and limestone",enableInstancing=true};
            made.SetTexture("_BaseMap",strip);made.SetColor("_BaseColor",Color.white);made.SetFloat("_Smoothness",.35f);
            distantPaint.Add(key,made);return made;
        }

        void DistantFront(MeshRenderer close, Mesh source)
        {
            var mats=close.sharedMaterials;
            if(source.subMeshCount>Swatches || source.subMeshCount!=mats.Length || source.lodCount<2) return;
            if(!distantFronts.TryGetValue(source,out var mesh)) distantFronts.Add(source,mesh=FarMesh(source));
            var far=new GameObject(mesh.name);far.layer=close.gameObject.layer;far.transform.SetParent(close.transform,false);
            far.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=far.AddComponent<MeshRenderer>();renderer.sharedMaterial=FarPaint(mats);
            renderer.renderingLayerMask=close.renderingLayerMask;renderer.shadowCastingMode=close.shadowCastingMode;
            var group=close.gameObject.AddComponent<LODGroup>();
            group.SetLODs(new[]{new LOD(.14f,new Renderer[]{close}),new LOD(.0035f,new Renderer[]{renderer})});
            group.RecalculateBounds();
        }
    }
}
