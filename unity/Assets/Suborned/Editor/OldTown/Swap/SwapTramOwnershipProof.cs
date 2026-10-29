#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
using P=Suborned.OldTown.Editor.Swap.SwapTramJoin.P;
using G=Suborned.OldTown.Editor.Swap.SwapTramJoin;
namespace Suborned.OldTown.Editor.Swap
{
    /// <summary>Read-only post-save triangle ownership check; never regenerates or captures.</summary>
    internal static class SwapTramOwnershipProof
    {
        sealed class Face{public Vector3 A,B,C;public float Y;public string Material,Mesh;}
        static double Area(List<P> p){double a=0;for(int i=1;i+1<p.Count;i++)a+=G.Cross(p[i]-p[0],p[i+1]-p[0]);return Math.Abs(a)*.5;}
        static double Intersection(Face a,Face b)
        {
            P origin=new P(b.A.x,b.A.z);var poly=new List<P>{new P(a.A.x,a.A.z)-origin,new P(a.B.x,a.B.z)-origin,new P(a.C.x,a.C.z)-origin};
            var clip=new[]{new P(0,0),new P(b.B.x,b.B.z)-origin,new P(b.C.x,b.C.z)-origin};double sign=G.Cross(clip[1],clip[2])>0?1:-1;
            for(int i=0;i<3;i++){var x=clip[i];var u=clip[(i+1)%3]-x;var next=new List<P>();
                for(int j=0;j<poly.Count;j++){var v=poly[j];var w=poly[(j+1)%poly.Count];double d=G.Cross(u,v-x)*sign,e=G.Cross(u,w-x)*sign;if(d>=0)next.Add(v);if((d>=0)!=(e>=0))next.Add(v+(w-v)*(d/(d-e)));}poly=next;}
            return Area(poly);
        }
        static IEnumerable<Face> Faces(MeshFilter mf,Func<int,bool> use)
        {
            var rd=SwapTramMeshRead.Read(mf.sharedMesh,mf.transform.localToWorldMatrix);
            for(int s=0;s<rd.Subs.Count;s++)if(use(s))for(int i=0;i<rd.Subs[s].Length;i+=3){var ix=rd.Subs[s];var a=rd.Verts[ix[i]].W;var b=rd.Verts[ix[i+1]].W;var c=rd.Verts[ix[i+2]].W;
                float lo=Mathf.Min(a.y,Mathf.Min(b.y,c.y)),hi=Mathf.Max(a.y,Mathf.Max(b.y,c.y));
                if(lo<-.001f||hi>.0061f||hi-lo>1e-6f)continue;
                var mats=mf.GetComponent<MeshRenderer>().sharedMaterials;var f=new Face{A=a,B=b,C=c,Y=(a.y+b.y+c.y)/3,Material=s<mats.Length&&mats[s]?mats[s].name:"none",Mesh=mf.name};if(Area(new List<P>{new(a.x,a.z),new(b.x,b.z),new(c.x,c.z)})>1e-12)yield return f;
            }
        }
        internal static object Check(string path)
        {
                var grid=new Dictionary<Vector2Int,List<Face>>();long tileFaces=0;
                foreach(var mf in SwapTramRender.Owners.Keys)foreach(var f in Faces(mf,s=>true))
                {
                    tileFaces++;int x0=Mathf.FloorToInt(Mathf.Min(f.A.x,Mathf.Min(f.B.x,f.C.x))/2),x1=Mathf.FloorToInt(Mathf.Max(f.A.x,Mathf.Max(f.B.x,f.C.x))/2);
                    int y0=Mathf.FloorToInt(Mathf.Min(f.A.z,Mathf.Min(f.B.z,f.C.z))/2),y1=Mathf.FloorToInt(Mathf.Max(f.A.z,Mathf.Max(f.B.z,f.C.z))/2);
                    for(int x=x0;x<=x1;x++)for(int y=y0;y<=y1;y++){var key=new Vector2Int(x,y);if(!grid.TryGetValue(key,out var list))grid[key]=list=new();list.Add(f);}
                }
                long signals=0,overlaps=0;double area=0,maxArea=0;var examples=new List<object>();var largest=new List<(double area,object detail)>();
                foreach(var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude))
                {
                    var mr=mf.GetComponent<MeshRenderer>();if(!mr||!mr.enabled||!mf.sharedMesh)continue;var mats=mr.sharedMaterials;
                    bool Light(int s)=>s<mats.Length&&mats[s]&&(mats[s].name.EndsWith("-crosslight")||mats[s].shader.name=="Suborned/Signal Dots");
                    if(!Enumerable.Range(0,mats.Length).Any(Light))continue;
                    foreach(var f in Faces(mf,Light))
                    {
                        signals++;var seen=new HashSet<Face>();int x0=Mathf.FloorToInt(Mathf.Min(f.A.x,Mathf.Min(f.B.x,f.C.x))/2),x1=Mathf.FloorToInt(Mathf.Max(f.A.x,Mathf.Max(f.B.x,f.C.x))/2),y0=Mathf.FloorToInt(Mathf.Min(f.A.z,Mathf.Min(f.B.z,f.C.z))/2),y1=Mathf.FloorToInt(Mathf.Max(f.A.z,Mathf.Max(f.B.z,f.C.z))/2);
                        for(int x=x0;x<=x1;x++)for(int y=y0;y<=y1;y++)if(grid.TryGetValue(new Vector2Int(x,y),out var fs))foreach(var tile in fs)
                        {
                            if(!seen.Add(tile)||Math.Abs(tile.Y-f.Y)>.000001)continue;double a=Intersection(f,tile);if(a<=1e-12)continue;
                            overlaps++;area+=a;maxArea=Math.Max(maxArea,a);largest.Add((a,new Dictionary<string,object>{{"area",a},{"signalMesh",f.Mesh},{"signalMaterial",f.Material},{"signalY",f.Y},{"tileMaterial",tile.Material},{"tileMesh",tile.Mesh},{"tileY",tile.Y},{"point",new[]{f.A.x,f.A.z-OldTownSwap.DZ}}}));if(examples.Count<20)examples.Add(new object[]{mf.name,a,new[]{(f.A.x+f.B.x+f.C.x)/3,(f.A.z+f.B.z+f.C.z)/3-OldTownSwap.DZ}});
                        }
                    }
                }
                var result=new Dictionary<string,object>{{"status","read-only saved candidate check; no overlap accepted"},{"tilePlanarFaces",tileFaces},{"signalTriangles",signals},{"coplanarPositiveAreaPairs",overlaps},{"coplanarIntersectionAreaM2",area},{"largestIntersectionM2",maxArea},{"examples",examples},{"largestOverlaps",largest.OrderByDescending(p=>p.area).Take(30).Select(p=>p.detail).ToArray()},{"arithmetic","double planar triangle intersection of actual float world vertices; heights within 1 micrometre; pair area >1e-12m2 recorded, no bounds proxy or audit filtering"}};
                File.WriteAllText(path,SwapTramJson.Encode(result));return result;
        }
        public static void Run()
        {
            try { EditorSceneManager.OpenScene("Assets/Scenes/probe-recovery075-tramsettled.unity",OpenSceneMode.Single);var root=Object.FindObjectsByType<Transform>().Single(t=>t.name==SwapNetwork.RailsRoot);SwapTramRender.BindExisting(root);Check(Path.GetFullPath(Path.Combine(Application.dataPath,"../../briefs/075/recovery/tram-corrected/saved-ownership.json"))); }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
#endif
