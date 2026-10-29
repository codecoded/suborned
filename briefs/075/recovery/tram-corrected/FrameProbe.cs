using System;using UnityEngine;using Suborned.OldTown.Editor.Swap;
class FrameProbe
{
 static int n;
 static void Check(bool p,string m){n++;if(!p)throw new Exception(m);}
 static SwapMesh.V V(Vector3 p,Vector3 normal,bool mirror)=>new SwapMesh.V{P=p,W=p,N=normal.normalized,T=new Vector4(3,2,1,-1),U0=new Vector2((mirror?-1:1)*(2*p.x+p.z),.5f*p.x+3*p.z)};
 static void Main(){foreach(float off in new[]{0,512})foreach(bool mirror in new[]{false,true})foreach(var normal in new[]{Vector3.up,new Vector3(.2f,1,.1f)}){
 var a=V(new Vector3(off,0,0),normal,mirror);var b=V(new Vector3(off+2.5f,0,0),normal,mirror);var c=V(new Vector3(off+.25f,0,1),normal,mirror);
 SwapTramFrames.Derivatives(a,b,c,out var du,out var dv,out bool regular);Check(regular,"regular UV");var t=SwapTramFrames.Frame(a,du,dv,regular);var tv=new Vector3(t.x,t.y,t.z);
 Check(Math.Abs(tv.magnitude-1)<1e-6,"unit");Check(Math.Abs(Vector3.Dot(tv,a.N))<1e-6,"orthogonal");Check((Vector3.Dot(Vector3.Cross(a.N,tv),dv)<0?-1:1)==t.w,"handedness");
 var gu=SwapTramFrames.Gradient(a.P,b.P,c.P,a.U0.x,b.U0.x,c.U0.x);var gv=SwapTramFrames.Gradient(a.P,b.P,c.P,a.U0.y,b.U0.y,c.U0.y);
 Check((gu-new Vector3(mirror?-2:2,0,mirror?-1:1)).magnitude<1e-6,"U physical density");Check((gv-new Vector3(.5f,0,3)).magnitude<1e-6,"V physical density");}
 var v=V(Vector3.zero,Vector3.up,false);var fallback=SwapTramFrames.Frame(v,Vector3.zero,Vector3.zero,false);Check(Math.Abs(new Vector3(fallback.x,fallback.y,fallback.z).magnitude-1)<1e-6,"degenerate UV fallback");
 Console.WriteLine($"PASS {n} frame/density/handedness checks, including mirrored UVs, smooth normals, degenerate charts and 512m translation");}
}
