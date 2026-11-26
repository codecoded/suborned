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
        readonly Dictionary<(Mesh,int),Mesh> distantFronts = new();
        readonly Dictionary<(Vector2Int tile, Material finish, int layer, uint light, ShadowCastingMode cast),List<(MeshRenderer close,Mesh mid,Mesh far)>> frontageTiles = new();
        readonly Dictionary<string,Material> distantPaint = new();

        static bool DecoratedBay(string name) => name.StartsWith("DK_Stream") || name.StartsWith("DK_PortholeBay") || name.StartsWith("DK_BalconyBay");

        Mesh FarMesh(Mesh src, int level)
        {
            var vertices=src.vertices; var normals=src.normals;
            var xyz=new List<Vector3>(); var ns=new List<Vector3>(); var uv=new List<Vector2>(); var tris=new List<int>();
            if(distantFronts.TryGetValue((src,level),out var painted)) return painted;
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
            distantFronts.Add((src,level),mesh);
            return mesh;
        }

        Material FarPaint(Material[] slots)
        {
            var key=string.Join("/",slots.Select(m=>m ? m.GetEntityId().ToString() : "null"));
            if(distantPaint.TryGetValue(key,out var made)) return made;
            var colours=new Color[Swatches];
            var windows=new Color[Swatches];
            for(int i=0;i<slots.Length;i++)
            {
                var mat=slots[i];
                var c=mat && mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : new Color(.54f,.55f,.49f);
                if(mat && mat.name.Contains("Glass"))
                {
                    c=new Color(.21f,.42f,.45f);
                    windows[i]=new Color(.36f,.37f,.27f);
                }
                if(mat && (mat.name.Contains("Lobby") || mat.name.StartsWith("Harbour shop glass")))
                    windows[i]=new Color(.63f,.51f,.32f);
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
            var glow=new Texture2D(Swatches,1,TextureFormat.RGBA32,false){name="Distant occupied windows",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            glow.SetPixels(windows);glow.Apply(false,true);
            made.EnableKeyword("_EMISSION");made.SetTexture("_EmissionMap",glow);made.SetColor("_EmissionColor",new Color(.65f,.57f,.44f));
            distantPaint.Add(key,made);return made;
        }

        void DistantFront(MeshRenderer close, Mesh source)
        {
            var mats=close.sharedMaterials;
            if(source.subMeshCount>Swatches || source.subMeshCount!=mats.Length || source.lodCount<2) return;
            var centre=close.bounds.center;
            var tile=new Vector2Int(Mathf.FloorToInt(centre.x/44f),Mathf.FloorToInt(centre.z/44f));
            var key=(tile,FarPaint(mats),close.gameObject.layer,close.renderingLayerMask,close.shadowCastingMode);
            if(!frontageTiles.TryGetValue(key,out var list)) frontageTiles.Add(key,list=new());
            list.Add((close,FarMesh(source,Mathf.Min(3,source.lodCount-1)),FarMesh(source,source.lodCount-1)));
        }

        void FinishDistantFronts(Transform parent)
        {
            foreach(var pair in frontageTiles)
            {
                var (tile,finish,layer,light,cast)=pair.Key;
                var bays=pair.Value;
                var bounds=bays[0].close.bounds;
                foreach(var bay in bays) bounds.Encapsulate(bay.close.bounds);
                var root=new GameObject($"Harbour facades {tile.x}, {tile.y}");
                root.transform.SetParent(parent,false);root.transform.position=bounds.center;
                MeshRenderer Painted(string name, Mesh mesh, Transform owner)
                {
                    var go=new GameObject(name){layer=layer};go.transform.SetParent(owner,false);
                    go.AddComponent<MeshFilter>().sharedMesh=mesh;
                    var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=finish;r.renderingLayerMask=light;r.shadowCastingMode=cast;
                    return r;
                }
                var detailed=bays.Select(b=>(Renderer)b.close).ToArray();
                var middle=bays.Select(b=>(Renderer)Painted(b.mid.name,b.mid,b.close.transform)).ToArray();
                var group=root.AddComponent<LODGroup>();
                // Convert the individual bay's distance thresholds into the projected size of the whole cluster.
                float reference=bays.Average(b=>b.close.bounds.size.magnitude);
                float scale=bounds.size.magnitude/reference;
                float high=Mathf.Min(.72f,.14f*scale);
                float end=.0035f*scale;
                if(bays.Count==1) group.SetLODs(new[]{new LOD(high,detailed),new LOD(end,middle)});
                else
                {
                    var entries=bays.Select(b=>new CombineInstance{mesh=b.far,transform=root.transform.worldToLocalMatrix*b.close.localToWorldMatrix}).ToArray();
                    var mesh=new Mesh{name="Distant Harbour block"};
                    if(entries.Sum(e=>e.mesh.vertexCount)>65535) mesh.indexFormat=IndexFormat.UInt32;
                    mesh.CombineMeshes(entries,true,true);
                    var far=Painted(mesh.name,mesh,root.transform);
                    far.shadowCastingMode=ShadowCastingMode.Off;
                    group.SetLODs(new[]{new LOD(high,detailed),new LOD(Mathf.Min(.35f,.075f*scale),middle),new LOD(end,new Renderer[]{far})});
                }
                group.RecalculateBounds();
            }
            Debug.Log($"Harbour facades: {frontageTiles.Count} palette and street clusters, {frontageTiles.Count(p=>p.Value.Count>1)} combined far meshes");
        }
    }
}
