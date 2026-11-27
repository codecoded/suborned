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
        readonly Dictionary<(Mesh,int,uint),Mesh> distantFronts = new();
        readonly Dictionary<(Vector2Int tile, Material finish, int layer, uint light, ShadowCastingMode cast),List<(MeshRenderer close,Mesh mid,Mesh far,Mesh shadow)>> frontageTiles = new();
        readonly Dictionary<string,Material> distantPaint = new();
        readonly Dictionary<string,Texture2D> finishStrips = new();

        static bool DecoratedBay(string name) => name.StartsWith("DK_Stream") || name.StartsWith("DK_PortholeBay") || name.StartsWith("DK_BalconyBay");

        Mesh FarMesh(Mesh src, int level, uint slots = uint.MaxValue)
        {
            if(distantFronts.TryGetValue((src,level,slots),out var painted)) return painted;
            var vertices=src.vertices; var normals=src.normals;
            var xyz=new List<Vector3>(); var ns=new List<Vector3>(); var uv=new List<Vector2>(); var tris=new List<int>();
            using var raw=Mesh.AcquireReadOnlyMeshData(src);
            var data=raw[0]; bool wide=data.indexFormat==IndexFormat.UInt32;
            var index32=wide ? data.GetIndexData<int>() : default;
            var index16=wide ? default : data.GetIndexData<ushort>();
            for(int slot=0;slot<src.subMeshCount;slot++)
            {
                if ((slots & (1u << slot)) == 0) continue;
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
            var mesh=new Mesh {name=src.name+(slots == uint.MaxValue ? " distant enamel" : " solid shadow")};
            mesh.SetVertices(xyz); mesh.SetNormals(ns); mesh.SetUVs(0,uv); mesh.SetTriangles(tris,0);
            mesh.bounds=src.bounds;
            distantFronts.Add((src,level,slots),mesh);
            return mesh;
        }

        // The coarse facade keeps one draw. Its existing swatch UVs also index the authored
        // metal and smoothness values, so a chrome rail and a painted sill catch light differently.
        Texture2D FinishStrip(Material[] slots)
        {
            var values = new Color32[Swatches];
            for (int i = 0; i < slots.Length; i++)
            {
                var m = slots[i];
                // Highly textured masonry already uses the kit's matte distance finish. Its
                // smoothness multiplier alone would turn stone with a packed mask into a mirror.
                bool mapped = m && m.IsKeywordEnabled("_METALLICSPECGLOSSMAP");
                float metal = m && !mapped && m.HasProperty("_Metallic") ? m.GetFloat("_Metallic") : 0f;
                float gloss = m && !mapped && m.HasProperty("_Smoothness") ? m.GetFloat("_Smoothness") : .35f;
                values[i] = new Color(metal,0,0,gloss);
            }
            string key = string.Join(",",values.Select(c => c.r * 256 + c.a));
            if (finishStrips.TryGetValue(key,out var strip)) return strip;
            strip = new Texture2D(Swatches,1,TextureFormat.RGBA32,false,true) {
                name = "Harbour facade metal and smoothness", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
            };
            strip.SetPixels32(values);
            strip.Apply(false,true);
            finishStrips.Add(key,strip);
            return strip;
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
            made.SetTexture("_BaseMap",strip);made.SetColor("_BaseColor",Color.white);
            made.SetTexture("_MetallicGlossMap",FinishStrip(slots));made.EnableKeyword("_METALLICSPECGLOSSMAP");
            made.SetFloat("_Smoothness",1f);
            var glow=new Texture2D(Swatches,1,TextureFormat.RGBA32,false){name="Distant occupied windows",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            glow.SetPixels(windows);glow.Apply(false,true);
            made.EnableKeyword("_EMISSION");made.SetTexture("_EmissionMap",glow);made.SetColor("_EmissionColor",new Color(.65f,.57f,.44f));
            distantPaint.Add(key,made);return made;
        }

        // Transparent windows and shaders without a shadow pass leave genuine openings. A cutout
        // or a two-sided caster needs its own shader, so keep the authored shadow for that bay.
        static uint SolidShadowSlots(Material[] materials)
        {
            uint mask = 0;
            for (int i = 0; i < materials.Length; i++)
            {
                var m = materials[i];
                if (!m || m.FindPass("ShadowCaster") < 0 || !m.GetShaderPassEnabled("ShadowCaster")) continue;
                if (m.renderQueue >= (int)RenderQueue.AlphaTest || m.IsKeywordEnabled("_ALPHATEST_ON") ||
                    (m.HasProperty("_Cull") && (int)m.GetFloat("_Cull") != (int)CullMode.Back)) return 0;
                mask |= 1u << i;
            }
            return mask;
        }

        void DistantFront(MeshRenderer close, Mesh source, bool simplifyShadow)
        {
            var mats=close.sharedMaterials;
            if(source.subMeshCount>Swatches || source.subMeshCount!=mats.Length || source.lodCount<2) return;
            var centre=close.bounds.center;
            var tile=new Vector2Int(Mathf.FloorToInt(centre.x/44f),Mathf.FloorToInt(centre.z/44f));
            var key=(tile,FarPaint(mats),close.gameObject.layer,close.renderingLayerMask,close.shadowCastingMode);
            if(!frontageTiles.TryGetValue(key,out var list)) frontageTiles.Add(key,list=new());
            int level = Mathf.Min(3,source.lodCount-1);
            uint mask = simplifyShadow && close.shadowCastingMode == ShadowCastingMode.On ? SolidShadowSlots(mats) : 0;
            list.Add((close,FarMesh(source,level),FarMesh(source,source.lodCount-1),mask != 0 ? FarMesh(source,level,mask) : null));
        }

        void FinishDistantFronts(Transform parent, Vector3 civic)
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
                var near = new List<Renderer>();
                foreach (var bay in bays)
                {
                    near.Add(bay.close);
                    if (!bay.shadow) continue;
                    var silhouette = Painted(bay.shadow.name,bay.shadow,bay.close.transform);
                    silhouette.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                    silhouette.receiveShadows = false;
                    bay.close.shadowCastingMode = ShadowCastingMode.Off;
                    near.Add(silhouette);
                }
                var detailed = near.ToArray();
                var middle=bays.Select(b=>(Renderer)Painted(b.mid.name,b.mid,b.close.transform)).ToArray();
                var group=root.AddComponent<LODGroup>();
                // Convert the individual bay's distance thresholds into the projected size of the whole cluster.
                float reference=bays.Average(b=>b.close.bounds.size.magnitude);
                float scale=bounds.size.magnitude/reference;
                float high=Mathf.Min(.72f,.14f*scale);
                float mid=Mathf.Min(.35f,.075f*scale);
                if (bays.All(b => b.close.bounds.SqrDistance(civic) > 75f * 75f))
                {
                    // The next threshold removed the window divisions across Lantern Lane.
                    // This setting keeps the shopfront while grouping the smaller bays behind it.
                    high = Mathf.Min(.72f, high * 1.35f);
                    mid = Mathf.Min(.42f, mid * 1.27f);
                }
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
                    group.SetLODs(new[]{new LOD(high,detailed),new LOD(mid,middle),new LOD(end,new Renderer[]{far})});
                }
                group.RecalculateBounds();
            }
            Debug.Log($"Harbour facades: {frontageTiles.Count} palette and street clusters, {frontageTiles.Count(p=>p.Value.Count>1)} combined far meshes");
        }
    }
}
