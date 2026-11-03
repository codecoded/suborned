using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Suborned.OldTown.Editor.Swap;
class WindingCheck
{
 static SwapMesh.V V(float x,float z)=>new SwapMesh.V{P=new Vector3(x,.005f,z),W=new Vector3(x,.005f,z),N=Vector3.up};
 static double Area(List<List<SwapMesh.V>> ps){double sum=0;foreach(var p in ps){double a=0;for(int i=0;i<p.Count;i++){var v=p[i].W;var w=p[(i+1)%p.Count].W;a+=(double)v.x*w.z-(double)v.z*w.x;}sum+=Math.Abs(a)*.5;}return sum;}
 static void Main()
 {
  var square=new List<SwapMesh.V>{V(-2,-2),V(2,-2),V(2,2),V(-2,2)};
  var points=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(0,1)};
  foreach(bool reverse in new[]{false,true}){
   var mask=new SwapShapes.Shape{Poly=reverse?points.Reverse().ToArray():points,Y0=.003f};
   double area=Area(SwapTramInlays.Subtract(new List<List<SwapMesh.V>>{square},mask,true));
   Console.WriteLine($"{(reverse?"CW":"CCW")} actual retained area {area:R}; expected 15.5; error {area-15.5:R}");
  }
 }
}
