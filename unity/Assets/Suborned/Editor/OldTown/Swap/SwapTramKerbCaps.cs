#if UNITY_EDITOR
using System;using System.Collections.Generic;using System.Linq;using UnityEngine;using Object=UnityEngine.Object;
namespace Suborned.OldTown.Editor.Swap
{
    /// <summary>Vertical granite end faces on the ACTUAL union boundary of the bed, derived
    /// from the clipped kerb's open edges. No horizontal cap, source/collider mutation or bed widening.</summary>
    internal static class SwapTramKerbCaps
    {
        internal static readonly List<object> Evidence=new();
        sealed class Plane{public Vector2 A,B,N;public double C,Error;}
        static double Ulp(float v){int b=BitConverter.SingleToInt32Bits(Math.Abs(v));return (double)BitConverter.Int32BitsToSingle(b+1)-Math.Abs(v);}
        static List<Vector2> Hull(IEnumerable<Vector2> input)
        {
            var ps=input.Distinct().OrderBy(p=>p.x).ThenBy(p=>p.y).ToArray();var h=new List<Vector2>();
            double Cross(Vector2 a,Vector2 b,Vector2 c)=>((double)b.x-a.x)*((double)c.y-a.y)-((double)b.y-a.y)*((double)c.x-a.x);
            foreach(var p in ps){while(h.Count>1&&Cross(h[h.Count-2],h[h.Count-1],p)<=0)h.RemoveAt(h.Count-1);h.Add(p);}int n=h.Count+1;
            for(int i=ps.Length-2;i>=0;i--){var p=ps[i];while(h.Count>=n&&Cross(h[h.Count-2],h[h.Count-1],p)<=0)h.RemoveAt(h.Count-1);h.Add(p);}if(h.Count>1)h.RemoveAt(h.Count-1);return h;
        }
        internal static void Apply()
        {
            var bed=new SwapShapes();foreach(var f in SwapTramRender.Geometry)bed.Add("bed",f.Source.Id,SwapTramRender.Polygon(f));
            foreach(var root in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).Where(t=>t.name=="Tram street kerbs (old Kerb_Granite on E12's cut edges)"))foreach(var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if(!mf.sharedMesh.name.Contains("(tram surface cut)"))continue;var rd=SwapTramMeshRead.Read(mf.sharedMesh,mf.transform.localToWorldMatrix);
                double localBound=4*Math.Pow(2,-23)*Math.Max(1,rd.Verts.Max(v=>Math.Max(Math.Abs(v.P.x),Math.Max(Math.Abs(v.P.y),Math.Abs(v.P.z)))));
                var points=new List<Vector3>();var world=new List<Vector3>();var vid=new int[rd.Verts.Length];
                for(int i=0;i<vid.Length;i++)
                {
                    var p=rd.Verts[i].P;int j=points.FindIndex(q=>((double)p.x-q.x)*((double)p.x-q.x)+((double)p.y-q.y)*((double)p.y-q.y)+((double)p.z-q.z)*((double)p.z-q.z)<=localBound*localBound);
                    if(j<0){j=points.Count;points.Add(p);world.Add(rd.Verts[i].W);}vid[i]=j;
                }
                var edges=new Dictionary<(int,int),int>();
                for(int s=0;s<rd.Subs.Count;s++)for(int i=0;i<rd.Subs[s].Length;i+=3)for(int k=0;k<3;k++){int a=vid[rd.Subs[s][i+k]],b=vid[rd.Subs[s][i+(k+1)%3]];if(a==b)continue;var key=(Math.Min(a,b),Math.Max(a,b));edges.TryGetValue(key,out int count);edges[key]=count+1;}
                var boundary=edges.Where(p=>p.Value==1).SelectMany(p=>new[]{p.Key.Item1,p.Key.Item2}).Distinct().ToArray();if(boundary.Length==0)continue;
                var mr=mf.GetComponent<MeshRenderer>();var box=SwapMesh.XZ(mr.bounds);var planes=new List<Plane>();
                foreach(var shape in bed.Near(box).Where(h=>h.Overlaps(box)))for(int i=0;i<shape.Poly.Length;i++)
                {
                    var a=shape.Poly[i];var b=shape.Poly[(i+1)%shape.Poly.Length];var d=b-a;if(d.sqrMagnitude==0)continue;var n=new Vector2(-d.y,d.x).normalized;double c=-(double)n.x*a.x-(double)n.y*a.y;
                    double error=4*(Ulp(a.x)+Ulp(a.y)+Ulp(b.x)+Ulp(b.y));
                    if(planes.Any(p=>Math.Abs((double)p.N.x*n.y-(double)p.N.y*n.x)<=error/d.magnitude&&Math.Abs(p.C-c)<=error))continue;
                    planes.Add(new Plane{A=a,B=b,N=n,C=c,Error=error});
                }
                var wr=new SwapMesh.Write(rd,true);for(int s=0;s<rd.Subs.Count;s++)for(int i=0;i<rd.Subs[s].Length;i+=3)wr.Keep(s,rd.Subs[s][i],rd.Subs[s][i+1],rd.Subs[s][i+2]);int caps=0;
                foreach(var plane in planes)
                {
                    var ids=boundary.Where(i=>Math.Abs((double)plane.N.x*world[i].x+(double)plane.N.y*world[i].z+plane.C)<=plane.Error).ToArray();if(ids.Length<3)continue;
                    var tangent=new Vector3(plane.N.y,0,-plane.N.x);var origin=new Vector3(plane.A.x,0,plane.A.y);
                    var hull=Hull(ids.Select(i=>new Vector2(Vector3.Dot(world[i]-origin,tangent),world[i].y)));if(hull.Count<3)continue;
                    double area=0;for(int i=1;i+1<hull.Count;i++)area+=Math.Abs(((double)hull[i].x-hull[0].x)*((double)hull[i+1].y-hull[0].y)-((double)hull[i].y-hull[0].y)*((double)hull[i+1].x-hull[0].x))*.5;
                    if(area==0)continue;var centre=origin+tangent*hull.Average(p=>p.x)+Vector3.up*hull.Average(p=>p.y);float probe=(float)plane.Error;
                    bool In(Vector2 q)=>bed.Hit(q)!=null;
                    if(!In(new Vector2(centre.x+plane.N.x*probe,centre.z+plane.N.y*probe))||In(new Vector2(centre.x-plane.N.x*probe,centre.z-plane.N.y*probe)))continue;
                    var normalWorld=new Vector3(plane.N.x,0,plane.N.y);var normal=mf.transform.localToWorldMatrix.transpose.MultiplyVector(normalWorld).normalized;var poly=new List<SwapMesh.V>();
                    // Match the adjacent source granite's physical UV density, rather than a new palette/material.
                    double density=0;int samples=0;foreach(var ix in rd.Subs)for(int i=0;i<ix.Length;i+=3)
                    {
                        var a=rd.Verts[ix[i]];var b=rd.Verts[ix[i+1]];var c=rd.Verts[ix[i+2]];var gu=SwapTramFrames.Gradient(a.W,b.W,c.W,a.U0.x,b.U0.x,c.U0.x);var gv=SwapTramFrames.Gradient(a.W,b.W,c.W,a.U0.y,b.U0.y,c.U0.y);double d=Math.Sqrt(gu.magnitude*gv.magnitude);if(d>0&&double.IsFinite(d)){density+=d;samples++;}
                    }
                    density=samples>0?density/samples:0;
                    foreach(var p in hull){var w=origin+tangent*p.x+Vector3.up*p.y;poly.Add(new SwapMesh.V{P=mf.transform.InverseTransformPoint(w),W=w,N=normal,T=new Vector4(tangent.x,tangent.y,tangent.z,1),U0=new Vector2((float)(p.x*density),(float)(p.y*density)),C=Color.white});}
                    wr.Poly(0,poly);caps++;Evidence.Add(new Dictionary<string,object>{{"mesh",mf.name},{"capVertices",hull.Count},{"areaM2",area},{"physicalUvDensity",density},{"sourceDensitySamples",samples},{"normalY",0},{"localWeldRoundoffBound",localBound},{"worldPlaneRoundoffBound",plane.Error},{"plane",new[]{(double)plane.N.x,plane.N.y,plane.C}},{"unionBoundaryProbe",probe}});
                }
                if(caps>0){mf.sharedMesh=wr.Mesh(mf.sharedMesh.name+" (closed tram kerb)");UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(mf);}
            }
        }
    }
}
#endif
