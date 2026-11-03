using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using Suborned.OldTown.Editor.Swap;
using J = Suborned.OldTown.Editor.Swap.SwapTramJoin;
using P = Suborned.OldTown.Editor.Swap.SwapTramJoin.P;
class Check
{
    static int assertions, pairs, mapped;
    static void Must(bool value, string message) { assertions++; if (!value) throw new Exception(message); }
    static double D(P a, P b) => (a-b).Length;
    static P Unit(P p) => p*(1/p.Length);
    static P N(P p) => new P(-p.Y,p.X);
    static J.Tile T(string id, P c, P u, double s) => new J.Tile(id,c,u,s);
    static P Read(JsonElement p) => new P(p[0].GetDouble(),p[1].GetDouble());
    static void Reject(Action a, string name) { bool rejected=false; try { a(); } catch(ArgumentException) { rejected=true; } Must(rejected,"not rejected: "+name); }
    // Independent convex polygon intersection area: catches positive-area overlap, not just common vertices.
    static double Overlap(P[] a, P[] b)
    {
        var p=a.ToList();
        for(int k=0;k<b.Length;k++) {
            P x=b[k],u=b[(k+1)%b.Length]-x; var next=new List<P>();
            for(int i=0;i<p.Count;i++) {
                P v=p[i],w=p[(i+1)%p.Count]; double dv=J.Cross(u,v-x),dw=J.Cross(u,w-x);
                if(dv>=0) next.Add(v);
                if((dv>=0)!=(dw>=0)) next.Add(v+(w-v)*(dv/(dv-dw)));
            } p=next;
        }
        double area=0; for(int i=0;i<p.Count;i++) area+=J.Cross(p[i],p[(i+1)%p.Count]);
        return Math.Abs(area)*.5;
    }
    static void Validate(J.Footprint[] fs,bool closed=false)
    {
        for(int i=0;i<fs.Length;i++) {
            var f=fs[i];
            foreach(var p in f.Polygon) Must(Math.Abs(J.Dot(p-f.Source.Centre,f.Normal))<=f.HalfWidth+1e-8,"width changed");
            foreach(double lat in new[]{-f.HalfWidth,-.5,0,.5,f.HalfWidth}) {
                Must(D(f.Map(0,lat), f.Source.Centre+f.Normal*lat)<1e-9,"centre/cross-section shifted");
                foreach(double along in new[]{-1,-.75,0,.75,1}) {
                    var p=f.Map(along,lat); Must(Math.Abs(J.Dot(p-f.Source.Centre,f.Normal)-lat)<1e-8,"lateral gauge changed"); mapped++;
                }
            }
            if(i+1<fs.Length || closed) {
                var g=fs[(i+1)%fs.Length]; Must(Object.ReferenceEquals(f.End,g.Start),"seam not shared");
                Must(Overlap(f.Polygon,g.Polygon)<1e-7,"positive area seam overlap"); pairs++;
                for(int k=0;k<=20;k++) {
                    double lat=-f.HalfWidth+2*f.HalfWidth*k/20;
                    Must(D(f.Map(1,lat),g.Map(-1,lat))<1e-7,"seam cross-section gap");
                }
            }
        }
    }
    static (P[],J.Tile[]) Arc(double r,int count,bool closed=false)
    {
        double sweep=closed?Math.PI*2:Math.PI/2,delta=sweep/count;
        var route=Enumerable.Range(0,closed?count:count+1).Select(i=>new P(r*Math.Cos(i*delta),r*Math.Sin(i*delta))).ToArray();
        var ts=Enumerable.Range(0,count).Select(i=> {double a=(i+.5)*delta;return T("arc"+i,new P(r*Math.Cos(a),r*Math.Sin(a)),new P(-Math.Sin(a),Math.Cos(a)),r*delta);}).ToArray();
        return (route,ts);
    }
    static void Main()
    {
        var straight=J.Build(new[]{new P(0,0),new P(10,0)},new[]{T("b",new P(3.04,0),new P(1,0),2.04),T("a",new P(1.01,0),new P(1,0),2.02)});
        Validate(straight); Must(Math.Abs(straight[0].Start.Right.X)<1e-9,"open start cap"); Must(Math.Abs(straight[1].End.Right.X-4.06)<1e-9,"actual step cap");
        var lone=J.Build(new[]{new P(0,0),new P(3,0)},new[]{T("one",new P(1.2,0),new P(1,0),2.4)}); Validate(lone);
        foreach(double r in new[]{23.2,26.8}) {
            var a=Arc(r,20);
            P Reflect(P p)=>new P(-p.X,p.Y);
            Validate(J.Build(a.Item1.Select(Reflect).ToArray(),a.Item2.Select(t=>T(t.Id,Reflect(t.Centre),Reflect(t.Tangent),t.Step))));
            var fs=J.Build(a.Item1,a.Item2.Reverse());Validate(fs);
            for(int i=0;i<=2000;i++) {double t=(Math.PI/2)*i/2000; foreach(double lat in new[]{-.6,0,.6}) {
                P p=new P((r-lat)*Math.Cos(t),(r-lat)*Math.Sin(t));
                // Exact open caps stop at the original tangent interval, so exclude the end slivers.
                if(i>50 && i<1950) Must(fs.Any(f=>f.Contains(p)),"dense exact-arc coverage");
            }}
            var loop=Arc(r,80,true);Validate(J.Build(loop.Item1,loop.Item2,true),true);
            // Full quarter arc plus tangent straights, both transitions included.
            var route=new List<P>{new P(r,-4)};route.AddRange(a.Item1);route.Add(new P(-4,r));
            var tiles=new List<J.Tile>{T("before",new P(r,-1),new P(0,1),2)};
            tiles.AddRange(a.Item2);tiles.Add(T("after",new P(-1,r),new P(-1,0),2));
            var trans=J.Build(route,tiles);Validate(trans);
            foreach(var f in trans) foreach(var seam in new[]{f.Start,f.End}) for(int k=0;k<=20;k++) Must(f.Contains(seam.At(-1+k*.1,1)),"transition seam coverage");
        }
        // Smoothly sampled figure-eight: crossing has distinct equally close stations,
        // while each route turn is below 90 degrees.
        var routeX=Enumerable.Range(0,201).Select(i=> {double t=-Math.PI/2+2*Math.PI*i/200;return new P(10*Math.Sin(t),5*Math.Sin(2*t));}).ToArray();
        Reject(()=>J.Build(routeX,new[]{T("x",new P(0,0),new P(1,1),2)}),"ambiguous crossing");
        Reject(()=>J.Build(new[]{new P(0,0),new P(0,0)},new[]{T("a",new P(0,0),new P(1,0),2)}),"zero route edge");
        Reject(()=>J.Build(new[]{new P(0,0),new P(4,0),new P(0,0)},new[]{T("a",new P(1,0),new P(1,0),2)}),"cusp");
        Reject(()=>J.Build(new[]{new P(0,0),new P(10,0)},new[]{T("a",new P(1,0),new P(1,0),2),T("b",new P(1,0),new P(1,0),2)}),"duplicate station");
        Reject(()=>J.Build(new[]{new P(0,0),new P(10,0)},new[]{T("a",new P(1,0),new P(1,0),2),T("b",new P(7,0),new P(1,0),2)}),"missing interval");
        Reject(()=>J.Build(new[]{new P(0,0),new P(10,0)},new[]{T("a",new P(1,0),new P(1,0),2),T("b",new P(3,.001),new P(1,0),2)}),"parallel displaced");
        Reject(()=>J.Build(new[]{new P(0,0),new P(10,0)},new[]{T("a",new P(1,0),new P(0,0),2)}),"zero tangent");
        Reject(()=>J.Build(new[]{new P(0,0),new P(10,0)},new[]{T("a",new P(1,0),new P(1,0),double.NaN)}),"nonfinite step");
        var tight=Arc(.5,20);
        Reject(()=>J.Build(tight.Item1,tight.Item2),"width folds inside tight radius");
        Export();
        Console.WriteLine($"PASS {assertions} assertions; {pairs} adjacent intersections have no positive-area overlap; {mapped} mapped vertices retain lateral coordinate. No Unity launch.");
    }
    static void Export()
    {
        var world=JsonDocument.Parse(File.ReadAllText("out/world/harbour-district.world.json")).RootElement;
        var all=new List<J.Footprint>(); int tracks=0; var rejected=new List<string>();
        foreach(var tr in world.GetProperty("transit").GetProperty("tram").EnumerateArray()) {
            string edge=tr.GetProperty("id").GetString();
            var route=tr.GetProperty("line").EnumerateArray().Select(Read).ToList();
            bool closed=D(route[0],route[route.Count-1])<.002; if(closed)route.RemoveAt(route.Count-1);
            double spacing=tr.GetProperty("spacing").GetDouble();
            var groups=new Dictionary<int,List<J.Tile>>();
            foreach(var p in world.GetProperty("ground").GetProperty("pieces").EnumerateArray()) {
                if(p.GetProperty("kind").GetString()!="tram-rail" || p.GetProperty("edge").GetString()!=edge) continue;
                P c=Read(p.GetProperty("at"));double yaw=p.GetProperty("yaw").GetDouble();P u=new P(Math.Cos(yaw),Math.Sin(yaw));
                double best=double.MaxValue,signed=0;
                for(int i=0;i<(closed?route.Count:route.Count-1);i++) {
                    P v=route[(i+1)%route.Count]-route[i],d=Unit(v);double s=Math.Max(0,Math.Min(v.Length,J.Dot(c-route[i],d)));
                    double dist=D(c,route[i]+d*s); if(dist<best) {best=dist;signed=J.Dot(c-route[i],N(d));}
                }
                int key=spacing==0?0:(signed>0?1:-1);
                if(!groups.ContainsKey(key))groups[key]=new List<J.Tile>();
                double len=p.GetProperty("size")[0].GetDouble();int n=Math.Max(1,(int)Math.Round(len/2));double step=len/n;
                for(int i=0;i<n;i++) groups[key].Add(T(p.GetProperty("id").GetString()+"#"+i,c+u*(-len/2+step*(i+.5)),u,step));
            }
            foreach(var group in groups) {
                double off=group.Key*spacing/2;var offsetRoute=new List<P>();
                for(int i=0;i<route.Count;i++) {
                    P prev=Unit(route[i]-route[(i-1+route.Count)%route.Count]),next=Unit(route[(i+1)%route.Count]-route[i]);
                    P shift=(!closed && i==0)?N(next):(!closed && i==route.Count-1)?N(prev):(N(prev)+N(next))*(1/(1+J.Dot(prev,next)));
                    offsetRoute.Add(route[i]+shift*off);
                }
                // Partition only at explicitly rejected geometric joins, never by IDs or sample positions.
                // This does NOT waive these joins: every rejection is reported for integration.
                double Station(J.Tile t) {
                    double best=double.MaxValue,station=0,carry=0;
                    for(int i=0;i<offsetRoute.Count-1;i++) {
                        P v=offsetRoute[i+1]-offsetRoute[i],d=Unit(v);double x=Math.Max(0,Math.Min(v.Length,J.Dot(t.Centre-offsetRoute[i],d)));
                        double dist=D(t.Centre,offsetRoute[i]+d*x);if(dist<best){best=dist;station=carry+x;}carry+=v.Length;
                    }return station;
                }
                var sorted=group.Value.OrderBy(Station).ToArray();var run=new List<J.Tile>();int count=0;
                void Flush() {if(run.Count==0)return;var fs=J.Build(offsetRoute,run,false);Validate(fs);all.AddRange(fs);count+=fs.Length;run.Clear();}
                foreach(var tile in sorted) {
                    if(run.Count>0) {
                        try {J.Build(offsetRoute,new[]{run[run.Count-1],tile},false);}
                        catch(ArgumentException ex) {rejected.Add(edge+" offset "+off+": "+ex.Message);Flush();}
                    }
                    run.Add(tile);
                }
                Flush();tracks++;
                Console.WriteLine($"export {edge} offset {off}: {count} tiles, unsupported boundaries retained in rejected-joins.json");
            }
        }
        var evidence=JsonDocument.Parse(File.ReadAllText("briefs/075/recovery/tram-gaps/evidence.json")).RootElement;
        foreach(var r in evidence.GetProperty("rows").EnumerateArray()) {
            foreach(P p in new[]{Read(r.GetProperty("sample").GetProperty("q")),Read(r.GetProperty("arc").GetProperty("exact_point")),Read(r.GetProperty("arc").GetProperty("centre_point"))})
                Must(all.Any(f=>f.Contains(p)),"six/exact-arc coverage");
        }
        File.WriteAllText("briefs/075/recovery/tram-joins/rejected-joins.json",JsonSerializer.Serialize(rejected,new JsonSerializerOptions {WriteIndented=true}));
        Console.WriteLine($"Explicitly rejected export boundaries: {rejected.Count} (no production closure claim)");
        var output=all.Select(f=>new {id=f.Source.Id,step=f.Source.Step,polygon=f.Polygon.Select(p=>new[]{p.X,p.Y}).ToArray()}).ToArray();
        File.WriteAllText("briefs/075/recovery/tram-joins/footprints.json",JsonSerializer.Serialize(output));
        Console.WriteLine($"PASS pinned export: {tracks} tracks, {all.Count} tiles; 6/6 original + 6/6 exact-arc lateral + 6/6 exact centre probes at unchanged 2m width.");
    }
}
