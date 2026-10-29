using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Suborned.OldTown.Editor.Swap;
using G=Suborned.OldTown.Editor.Swap.SwapTramJoin;
class SourceCheck
{
 static int count;
 static void Assert(bool p,string m){count++;if(!p)throw new Exception(m);}
 static void Reject(Action a){bool ok=false;try{a();}catch(ArgumentException){ok=true;}Assert(ok,"missing/cusp accepted");}
 static void Main(){
 var fs=SwapTramSource.Load(Directory.GetCurrentDirectory(),"out/world/harbour-district.world.json");
 Assert(fs.Length==1584,"count");double maxGap=0,maxFloatGap=0;int pairs=0;
 for(int i=0;i<fs.Length;i++){
  var f=fs[i];foreach(var p in f.Polygon)Assert(Math.Abs(G.Dot(p-f.Source.Centre,f.Normal))<=1+1e-9,"width");
  if(i==fs.Length-1)continue;var g=fs[i+1];Assert(ReferenceEquals(f.End,g.Start),"shared seam");pairs++;
  foreach(double lateral in new[]{-1,-.5575,-.5,-.4625,0,.4625,.5,.5575,1}){
   var a=f.Map(1,lateral);var b=g.Map(-1,lateral);maxGap=Math.Max(maxGap,(a-b).Length);
   maxFloatGap=Math.Max(maxFloatGap,Math.Sqrt(Math.Pow((float)a.X-(float)b.X,2)+Math.Pow((float)(a.Y+206)-(float)(b.Y+206),2)));
   Assert((a-b).Length<1e-9,"seam position");
   foreach(double along in new[]{-1,-.75,0,.75,1})Assert(Math.Abs(G.Dot(f.Map(along,lateral)-f.Source.Centre,f.Normal)-lateral)<1e-9,"gauge");
  }
  // Separating seam puts both convex polygons in opposite half planes: no positive-area overlap.
  var u=f.End.Left-f.End.Right;
  Assert(f.Polygon.All(p=>G.Cross(u,p-f.End.Right)>=-1e-8),"prior crosses seam");
  Assert(g.Polygon.All(p=>G.Cross(u,p-f.End.Right)<=1e-8),"next crosses seam");
 }
 var s=new G.Segment("straight",new G.P(0,0),new G.P(1,0),4,0);
 G.Interval I(double a,double b)=>new G.Interval(new G.Tile("t",s.At((a+b)/2),s.Direction,b-a),s,a,b);
 Reject(()=>G.BuildSource(new[]{I(0,1),I(2,4)}));Reject(()=>G.BuildSource(new[]{I(0,3),I(2,4)}));Reject(()=>G.BuildSource(new[]{I(0,3)}));
 var cusp=new G.Segment("cusp",new G.P(4,0),new G.P(-1,0),2,0);
 Reject(()=>G.BuildSource(new[]{I(0,4),new G.Interval(new G.Tile("c",new G.P(3,0),new G.P(-1,0),2),cusp,0,2)}));
 var circle=new G.Segment("circle",new G.P(25,0),new G.P(0,1),Math.PI*50,1.0/25);
 var closed=Enumerable.Range(0,80).Select(i=>{double a=circle.Length*i/80,b=circle.Length*(i+1)/80;return new G.Interval(new G.Tile("loop"+i,circle.At((a+b)/2),circle.Tangent((a+b)/2),b-a),circle,a,b);}).ToArray();
 var loop=G.BuildSource(closed,true);Assert(ReferenceEquals(loop[0].Start,loop[79].End),"closed source seam");
 File.WriteAllText("briefs/075/recovery/tram-settled/source-footprints.json", "["+string.Join(",",fs.Select(f=>"{\"id\":\""+f.Source.Id+"\",\"polygon\":["+string.Join(",",f.Polygon.Select(p=>"["+p.X.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+p.Y.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"]"))+"]}"))+"]");
 Console.WriteLine($"PASS {count} assertions; complete single open graph {fs.Length} tiles / {pairs} shared seams; max exact seam gap {maxGap:R}m; float gap {maxFloatGap:R}m; missing/overlap/end/cusp rejection retained.");
 }
}
