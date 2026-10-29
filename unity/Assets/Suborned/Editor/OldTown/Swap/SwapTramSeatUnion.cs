#if UNITY_EDITOR
using System;using System.Collections.Generic;using System.Linq;using UnityEngine;
namespace Suborned.OldTown.Editor.Swap
{
    /// <summary>Replace an actual connected convex source-seat triangulation by its identical
    /// boundary. Area equality is checked in translated doubles. Never invent a wider light mask.</summary>
    internal static class SwapTramSeatUnion
    {
        internal sealed class Face{internal Vector3 A,B,C;internal string Source;}
        internal static long ConvexComponents,TriangleFallbacks;
        static double Area(Vector2[] p){double a=0;for(int i=1;i+1<p.Length;i++)a+=((double)p[i].x-p[0].x)*((double)p[i+1].y-p[0].y)-((double)p[i].y-p[0].y)*((double)p[i+1].x-p[0].x);return Math.Abs(a)*.5;}
        internal static void Add(SwapShapes masks,List<Face> faces)
        {
            if(faces.Count==0)return;var parent=Enumerable.Range(0,faces.Count).ToArray();int Find(int i){while(parent[i]!=i){parent[i]=parent[parent[i]];i=parent[i];}return i;}
            var vertex=new Dictionary<Vector2,int>();
            for(int i=0;i<faces.Count;i++)foreach(var p in new[]{faces[i].A,faces[i].B,faces[i].C}){var key=new Vector2(p.x,p.z);if(vertex.TryGetValue(key,out var j))parent[Find(i)]=Find(j);else vertex.Add(key,i);}
            foreach(var group in Enumerable.Range(0,faces.Count).GroupBy(Find))
            {
                var fs=group.Select(i=>faces[i]).ToArray();var points=fs.SelectMany(f=>new[]{new Vector2(f.A.x,f.A.z),new Vector2(f.B.x,f.B.z),new Vector2(f.C.x,f.C.z)}).ToArray();
                var temp=new SwapShapes();var hull=temp.Add("seat",fs[0].Source,points);double sum=fs.Sum(f=>Area(new[]{new Vector2(f.A.x,f.A.z),new Vector2(f.B.x,f.B.z),new Vector2(f.C.x,f.C.z)}));double boundary=Area(hull.Poly);
                double roundoff=(fs.Length*32+points.Length*16)*2.220446049250313e-16*(sum+boundary);
                if(Math.Abs(sum-boundary)<=roundoff)
                {
                    masks.Add("source-convex-enamel-seat",fs[0].Source,hull.Poly,fs.Min(f=>Math.Min(f.A.y,Math.Min(f.B.y,f.C.y))),fs.Max(f=>Math.Max(f.A.y,Math.Max(f.B.y,f.C.y))));ConvexComponents++;
                }
                else
                {
                    foreach(var f in fs)masks.Add("source-seat-triangle",f.Source,new[]{new Vector2(f.A.x,f.A.z),new Vector2(f.B.x,f.B.z),new Vector2(f.C.x,f.C.z)},Mathf.Min(f.A.y,Mathf.Min(f.B.y,f.C.y)),Mathf.Max(f.A.y,Mathf.Max(f.B.y,f.C.y)));TriangleFallbacks+=fs.Length;
                }
            }
        }
    }
}
#endif
