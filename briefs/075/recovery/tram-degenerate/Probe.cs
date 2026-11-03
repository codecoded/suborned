using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Suborned.OldTown.Editor.Swap;
using UnityEngine;

static class Probe
{
    static SwapMesh.V V(float x, float y, float z) => new SwapMesh.V
    {
        P = new Vector3(x,y,z), W = new Vector3(x,y,z), N = new Vector3(0,1,0),
        U0 = new Vector2(x,z), C = new Color(.2f,.4f,.6f,1)
    };
    static void Check(bool yes, string name) { if (!yes) throw new Exception(name); }
    static SwapMesh.Write Writer()
    {
        // No native Mesh is created or read: test the real managed clipping/writer code.
        var src=(SwapMesh.Read)RuntimeHelpers.GetUninitializedObject(typeof(SwapMesh.Read));
        src.Subs=new List<int[]> { Array.Empty<int>() }; src.Verts=Array.Empty<SwapMesh.V>();
        return new SwapMesh.Write(src);
    }
    static double Area(SwapMesh.Write w)
    {
        var vs=(List<SwapMesh.V>)typeof(SwapMesh.Write).GetField("verts",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(w);
        var ss=(List<List<int>>)typeof(SwapMesh.Write).GetField("subs",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(w);
        double total=0;
        foreach(var s in ss) for(int i=0;i<s.Count;i+=3)
        {
            var a=vs[s[i]];var b=vs[s[i+1]];var c=vs[s[i+2]];
            Check(SwapMesh.HasArea(a,b,c),"output has no collapsed triangle");
            foreach(var v in new[]{a,b,c})
            {
                Check(v.U0.x==v.P.x && v.U0.y==v.P.z,"UV interpolation retained");
                Check(Math.Abs(v.C.r-.2f)<1e-6,"colour retained");
            }
            double ux=(double)b.P.x-a.P.x,uy=(double)b.P.y-a.P.y,uz=(double)b.P.z-a.P.z;
            double vx=(double)c.P.x-a.P.x,vy=(double)c.P.y-a.P.y,vz=(double)c.P.z-a.P.z;
            total+=.5*Math.Sqrt(Math.Pow(uy*vz-uz*vy,2)+Math.Pow(uz*vx-ux*vz,2)+Math.Pow(ux*vy-uy*vx,2));
        }
        return total;
    }
    static void Main()
    {
        var a=V(0,0,0);var b=V(1,0,0);var c=V(0,0,1);
        Check(!SwapMesh.HasArea(a,a,c),"duplicate rejected");
        Check(!SwapMesh.HasArea(a,b,V(2,0,0)),"collinear rejected");
        Check(SwapMesh.HasArea(a,V(1e-8f,0,0),V(0,0,1)),"thin triangle retained");
        Check(SwapMesh.HasArea(a,V(1e-12f,0,0),V(0,0,1e-12f)),"tiny triangle retained");
        Check(SwapMesh.HasArea(a,V(0,1,0),V(0,0,1)),"vertical face retained");
        var edge=SwapMesh.Clip(new List<SwapMesh.V>{a,b,c},new SwapMesh.Half(1,0,-1));
        Check(edge.Count==1,"boundary duplicate collapsed");
        var w=Writer();w.Poly(0,edge);Check(w.Triangles==0,"point emits no triangles");
        w=Writer();w.Poly(0,new List<SwapMesh.V>{a,b,V(2,0,0),V(2,0,1)});
        Check(w.Triangles==1 && Math.Abs(Area(w)-1)<1e-8,"collinear fan retained area");
        foreach(float angle in new[]{0f,.37f})
        {
            float co=(float)Math.Cos(angle),si=(float)Math.Sin(angle);
            SwapMesh.V R(float x,float z)=>V(co*x-si*z,0,si*x+co*z);
            var poly=new List<SwapMesh.V>{R(-2,-1),R(2,-1),R(2,1),R(-2,1)};
            var regions=SwapMesh.Outside(new Rect(-1,-1,2,2));
            if(angle!=0) // rotate the half-planes with the source rectangle
                for(int i=0;i<regions.Length;i++)for(int j=0;j<regions[i].Length;j++)
                {var h=regions[i][j];regions[i][j]=new SwapMesh.Half(co*h.A-si*h.B,si*h.A+co*h.B,h.C);}
            w=Writer();SwapMesh.ClipInto(w,0,poly[0],poly[1],poly[2],regions);
            SwapMesh.ClipInto(w,0,poly[0],poly[2],poly[3],regions);
            Check(Math.Abs(Area(w)-4)<1e-5,"rotated cut retains correct area");
        }
        var repeated=new List<SwapMesh.V>{a,b,c};
        for(int i=0;i<10;i++)repeated=SwapMesh.Clip(repeated,new SwapMesh.Half(1,0,0));
        w=Writer();w.Poly(0,repeated);Check(w.Triangles==1 && Math.Abs(Area(w)-.5)<1e-8,"repeated cut stable");
        Console.WriteLine("PASS: boundary, duplicate, collinear, thin, tiny, vertical, rotated and repeated-cut checks; area and attributes retained.");
    }
}
