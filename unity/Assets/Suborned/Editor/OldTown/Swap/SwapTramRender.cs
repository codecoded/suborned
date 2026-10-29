#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using G = Suborned.OldTown.Editor.Swap.SwapTramJoin;
using Object = UnityEngine.Object;

namespace Suborned.OldTown.Editor.Swap
{
    /// <summary>Render-only deformation. Never assigns a collider or changes a source mesh/transform.
    /// The same immutable footprints feed mapping, ground subtraction and the audit.</summary>
    internal static class SwapTramRender
    {
        internal static readonly Dictionary<Transform,G.Footprint> Applied = new();
        internal static G.Footprint[] Geometry;
        internal static Vector2[] Polygon(G.Footprint f) => f.Polygon.Select(p => new Vector2((float)p.X,(float)(p.Y+OldTownSwap.DZ))).ToArray();
        internal const string SurfaceRoot="Joined tram render surfaces (source topology)";
        internal static readonly Dictionary<Transform,List<MeshFilter>> SurfaceFilters=new();
        internal static readonly Dictionary<MeshFilter,G.Footprint> Owners=new();
        internal static IEnumerable<MeshFilter> Filters(Transform tile)=>SurfaceFilters.TryGetValue(tile,out var fs)?fs:tile.GetComponentsInChildren<MeshFilter>(true);
        internal static void BindExisting(Transform root)
        {
            if(Geometry!=null&&root.Cast<Transform>().All(t=>Applied.ContainsKey(t)))return;
            bool joined=root.GetComponentsInChildren<MeshFilter>(true).Any(mf=>mf.sharedMesh&&(mf.sharedMesh.name.Contains("(source tram join)")||AssetDatabase.GetAssetPath(mf.sharedMesh).StartsWith("Assets/Suborned/OldTownArt/Swap/TramSettled/")));
            var surfaces=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).SingleOrDefault(t=>t.name==SurfaceRoot);
            if(!joined&&!surfaces)return;
            var repo=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"..",".."));
            var geometry=SwapTramSource.Load(repo,"out/world/harbour-district.world.json");
            var doc=SwapTramSource.Obj(Suborned.World.Json.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(repo,SwapTramSource.Sidecar))));
            var ids=SwapTramSource.Arr(doc["tiles"]).Select(x=>(string)SwapTramSource.Obj(x)["id"]).ToArray();var byId=geometry.ToDictionary(f=>f.Source.Id);
            var children=root.Cast<Transform>().ToArray();if(children.Length!=ids.Length)throw new InvalidOperationException("persisted tram placement count");
            Applied.Clear();SurfaceFilters.Clear();Owners.Clear();Geometry=geometry;
            for(int i=0;i<ids.Length;i++)
            {
                var f=byId[ids[i]];Applied.Add(children[i],f);
                var fs=surfaces?surfaces.Cast<Transform>().Where(t=>t.name==ids[i]).SelectMany(t=>t.GetComponentsInChildren<MeshFilter>(true)).ToList():children[i].GetComponentsInChildren<MeshFilter>(true).ToList();
                if(fs.Count==0)throw new InvalidOperationException("missing saved joined surface: "+ids[i]);SurfaceFilters.Add(children[i],fs);foreach(var mf in fs)Owners.Add(mf,f);
            }
        }
        internal static void Apply(Transform root, G.Footprint[] geometry, OldTownSwap.Report report)
        {
            var placed=root.Cast<Transform>().ToArray();
            if(placed.Length!=geometry.Length) throw new InvalidOperationException("tram placement count mismatch");
            // Existing placement order is export parent order, then subdivision. Match that order
            // against explicit pinned source IDs; names and spatial nearest guesses are not topology.
            var repo=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"..",".."));
            var doc=SwapTramSource.Obj(Suborned.World.Json.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(repo,SwapTramSource.Sidecar))));
            var ids=SwapTramSource.Arr(doc["tiles"]).Select(x=>(string)SwapTramSource.Obj(x)["id"]).ToArray();
            var byId=geometry.ToDictionary(f=>f.Source.Id); Applied.Clear();SurfaceFilters.Clear();Owners.Clear();Geometry=geometry;
            if(Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).Any(t=>t.name==SurfaceRoot))throw new InvalidOperationException("joined surface root already exists; use pristine source scene");
            var surfaceRoot=new GameObject(SurfaceRoot).transform;surfaceRoot.SetParent(root.parent,false);
            if(surfaceRoot.localToWorldMatrix!=Matrix4x4.identity)throw new InvalidOperationException("joined render frame must be identity");
            long triangles=0,caps=0;double maxScale=0,minScale=double.MaxValue,maxLateral=0,maxHeight=0;
            for(int index=0;index<placed.Length;index++)
            {
                var tile=placed[index]; var f=byId[ids[index]]; Applied.Add(tile,f);
                var oldDir=new Vector2(tile.right.x,tile.right.z);double sign=oldDir.x*f.Direction.X+oldDir.y*f.Direction.Y>=0?1:-1;
                bool first=ReferenceEquals(f,geometry[0]),last=ReferenceEquals(f,geometry[geometry.Length-1]);
                foreach(var mf in tile.GetComponentsInChildren<MeshFilter>(true))
                {
                    var mesh=mf.sharedMesh;if(!mesh)continue;
                    var rd=SwapTramMeshRead.Read(mesh,mf.transform.localToWorldMatrix);var wr=new SwapMesh.Write(rd,true);
                    // Compose the local hierarchy: inverse(world)*world loses tens of micrometres
                    // at the far map coordinates, enough to falsely exceed the exact +/-1 source cap.
                    var toRoot=Matrix4x4.identity;
                    for(var tr=mf.transform;tr!=tile;tr=tr.parent)
                    {
                        if(!tr)throw new InvalidOperationException("tram filter is outside its placement root");
                        toRoot=Matrix4x4.TRS(tr.localPosition,tr.localRotation,tr.localScale)*toRoot;
                    }
                    var fromRoot=toRoot.inverse;
                    foreach(var v in rd.Verts) {var p=toRoot.MultiplyPoint3x4(v.P); if(Mathf.Abs(p.x)>1.00001f || Mathf.Abs(p.z)>1.00001f)throw new InvalidOperationException($"tram mesh outside original 2m source domain: {f.Source.Id}, {mf.name}, root-local {p:R}, hierarchy {toRoot}");}
                    for(int sub=0;sub<rd.Subs.Count;sub++)
                    {
                        if(mesh.GetSubMesh(sub).topology!=UnityEngine.MeshTopology.Triangles)throw new InvalidOperationException("tram nontriangle source");
                        var ix=rd.Subs[sub];
                        for(int j=0;j<ix.Length;j+=3)
                        {
                            var tri=new List<SwapMesh.V>{rd.Verts[ix[j]],rd.Verts[ix[j+1]],rd.Verts[ix[j+2]]};
                            var p=tri.Select(v=>toRoot.MultiplyPoint3x4(v.P)).ToArray();
                            if(p.All(v=>Mathf.Abs(v.x-1)<1e-6f) && !(sign>0?last:first) || p.All(v=>Mathf.Abs(v.x+1)<1e-6f) && !(sign>0?first:last)) {caps++;continue;}
                            // UV derivatives in original root metres. Extend each existing chart with
                            // physical along distance, avoiding texture density scaling with a short step.
                            Vector3 gu=SwapTramFrames.Gradient(p[0],p[1],p[2],tri[0].U0.x,tri[1].U0.x,tri[2].U0.x),gv=SwapTramFrames.Gradient(p[0],p[1],p[2],tri[0].U0.y,tri[1].U0.y,tri[2].U0.y);
                            // Clip in SOURCE root local x, not float world coordinates.
                            List<SwapMesh.V> Half(bool positive)
                            {
                                var o=new List<SwapMesh.V>();
                                for(int k=0;k<3;k++) {var a=tri[k];var b=tri[(k+1)%3];float da=p[k].x*(positive?1:-1),db=p[(k+1)%3].x*(positive?1:-1);
                                    if(da>=0)o.Add(a);if((da>=0)!=(db>=0))o.Add(SwapMesh.V.Lerp(a,b,da/(da-db)));
                                }return o;
                            }
                            bool crosses=p.Any(v=>v.x<0)&&p.Any(v=>v.x>0);
                            foreach(var poly in crosses?new[]{Half(false),Half(true)}:new[]{tri})
                            {
                                var output=new List<SwapMesh.V>();
                                foreach(var v0 in poly)
                                {
                                    var v=v0;var local=toRoot.MultiplyPoint3x4(v.P);
                                    double a=Math.Max(-1,Math.Min(1,local.x*sign)),lat=Math.Max(-1,Math.Min(1,local.z*sign));
                                    var mapped=f.Map(a,lat);var world=new Vector3((float)mapped.X,v.W.y,(float)(mapped.Y+OldTownSwap.DZ));
                                    var seam=a<0?f.Start:f.End;var mid=f.Source.Centre+f.Normal*lat;
                                    double along=G.Dot(seam.At(lat,1)-mid,f.Direction),scale=a<0?-along:along;
                                    double shear=Math.Abs(a)*G.Dot((seam.Left-seam.Right)*.5,f.Direction);
                                    maxScale=Math.Max(maxScale,scale);minScale=Math.Min(minScale,scale);
                                    var d=new Vector3((float)f.Direction.X,0,(float)f.Direction.Y);var n=new Vector3((float)f.Normal.X,0,(float)f.Normal.Y);
                                    var jac=Matrix4x4.identity;jac.SetColumn(0,(Vector4)(d*(float)(scale*sign)));jac.SetColumn(1,new Vector4(0,1,0,0));jac.SetColumn(2,(Vector4)((n+d*(float)shear)*(float)sign));
                                    var normalRoot=fromRoot.transpose.MultiplyVector(v.N);
                                    var nw=jac.inverse.transpose.MultiplyVector(normalRoot).normalized;
                                    v.N=nw;
                                    if(rd.HasT){var tr=toRoot.MultiplyVector(new Vector3(v.T.x,v.T.y,v.T.z));var tw=jac.MultiplyVector(tr);var tl=tw;tl=(tl-v.N*Vector3.Dot(tl,v.N)).normalized;v.T=new Vector4(tl.x,tl.y,tl.z,v.T.w);}
                                    double newAlong=G.Dot(mapped-f.Source.Centre,f.Direction)*sign;
                                    // Evaluate the source chart in the ACTUAL stored float geometry,
                                    // so coordinate quantisation cannot silently change texture density.
                                    var actual=new G.P((double)world.x,(double)world.z-OldTownSwap.DZ)-f.Source.Centre;
                                    double x=G.Dot(actual,f.Direction)*sign,z=G.Dot(actual,f.Normal)*sign,y=(double)world.y-tile.position.y;
                                    double dx=x-p[0].x,dy=y-p[0].y,dz=z-p[0].z;
                                    v.U0=new Vector2((float)(tri[0].U0.x+gu.x*dx+gu.y*dy+gu.z*dz),(float)(tri[0].U0.y+gv.x*dx+gv.y*dy+gv.z*dz));
                                    v.P=world;v.W=world;
                                    maxHeight=Math.Max(maxHeight,Math.Abs(world.y-v0.W.y));
                                    maxLateral=Math.Max(maxLateral,Math.Abs(G.Dot(mapped-f.Source.Centre,f.Normal)-lat));output.Add(v);
                                }
                                SwapTramDensity.Record(output,gu,gv,tri[0],p[0],f,sign,tile.position.y);
                                wr.Poly(sub,output);
                            }
                        }
                    }
                    // One common float render frame gives both sides exactly the same seam
                    // vertices. Original transforms, source MeshFilters and collider references stay.
                    var go=new GameObject(f.Source.Id);go.layer=mf.gameObject.layer;go.tag=mf.gameObject.tag;go.transform.SetParent(surfaceRoot,false);
                    GameObjectUtility.SetStaticEditorFlags(go,GameObjectUtility.GetStaticEditorFlags(mf.gameObject));
                    var generated=go.AddComponent<MeshFilter>();generated.sharedMesh=wr.Mesh(mesh.name+" (source tram join)");
                    var original=mf.GetComponent<MeshRenderer>();var renderer=go.AddComponent<MeshRenderer>();EditorUtility.CopySerialized(original,renderer);
                    renderer.sharedMaterials=original.sharedMaterials;renderer.enabled=original.enabled;
                    var block=new MaterialPropertyBlock();original.GetPropertyBlock(block);renderer.SetPropertyBlock(block);
                    for(int k=0;k<original.sharedMaterials.Length;k++){var perMaterial=new MaterialPropertyBlock();original.GetPropertyBlock(perMaterial,k);renderer.SetPropertyBlock(perMaterial,k);}
                    original.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(original);
                    if(!SurfaceFilters.TryGetValue(tile,out var list))SurfaceFilters[tile]=list=new();list.Add(generated);Owners.Add(generated,f);triangles+=wr.Triangles;
                }
            }
            report.Log($"source tram joins: {placed.Length} tiles, {triangles} triangles; {caps} internal cap triangles removed; along scale [{minScale:R},{maxScale:R}], lateral error {maxLateral:R}, height error {maxHeight:R}; source/collider meshes and transforms untouched");
        }
    }
}
#endif
