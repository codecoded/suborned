using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Suborned.OldTown.Editor.Swap;
static class Probe
{
    static SwapMesh.V V(float x,float z,float y=.005f)=>new SwapMesh.V{P=new Vector3(x,y,z),W=new Vector3(x,y,z),N=Vector3.up};
    static double Area(List<List<SwapMesh.V>> polys)
    {
        double sum=0;foreach(var p in polys)if(p.Count>=3)
        {
            var o=p[0].W;double area=0;
            for(int i=1;i+1<p.Count;i++)area+=((double)p[i].W.x-o.x)*((double)p[i+1].W.z-o.z)-((double)p[i].W.z-o.z)*((double)p[i+1].W.x-o.x);
            sum+=Math.Abs(area)*.5;
        }
        return sum;
    }
    static void Check(double got,double expected,string label)
    {if(Math.Abs(got-expected)>1e-7)throw new Exception($"{label}: {got:R} != {expected:R}");Console.WriteLine("PASS "+label);}
    static void Main()
    {
        foreach(float offset in new[]{0f,512f})
        {
            var square=new List<SwapMesh.V>{V(offset-2,-2),V(offset+2,-2),V(offset+2,2),V(offset-2,2)};
            var tri=new[]{new Vector2(offset,0),new Vector2(offset+1,0),new Vector2(offset,1)};
            foreach(bool reverse in new[]{false,true})
            {
                var ring=reverse?tri.Reverse().ToArray():tri;
                var mask=new SwapShapes.Shape{Poly=ring,Y0=.003f};
                var input=new List<List<SwapMesh.V>>{square};
                Check(Area(SwapTramInlays.Subtract(input,mask,true)),15.5,$"{offset} winding {reverse}");
                if(!ring.SequenceEqual(mask.Poly))throw new Exception("mask mutated");
                mask.Poly=new[]{ring[0],ring[0],ring[1],ring[2],ring[0]};
                Check(Area(SwapTramInlays.Subtract(input,mask,true)),15.5,"repeated endpoints");
                mask.Y0=.01f;
                Check(Area(SwapTramInlays.Subtract(input,mask,true)),16,"below mask unchanged");
            }
            var flat=new SwapShapes.Shape{Poly=new[]{new Vector2(offset,0),new Vector2(offset+1,0),new Vector2(offset+2,0)}};
            Check(Area(SwapTramInlays.Subtract(new List<List<SwapMesh.V>>{square},flat,false)),16,"degenerate mask unchanged");
        }
    }
}
