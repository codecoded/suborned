// Brief 079: per tick, each person against each car's footprint and each pair of fighters' distance. Reads only.
using System; using System.Collections.Generic; using System.IO; using System.Linq; using System.Reflection; using Suborned.Sim;
static class P {
  sealed class Span { public string Key=""; public int From, To, N; public long Peak; public int PeakT; public Pt PeakA, PeakB; }
  static void Hit(Dictionary<string,List<Span>> d, string key, int t, long v, Pt a, Pt b, bool biggerWorse) {
    if(!d.TryGetValue(key,out var l)) d[key]=l=new List<Span>();
    var s=l.Count>0?l[^1]:null;
    if(s==null||s.To<t-1){ s=new Span{Key=key,From=t,To=t,N=0,Peak=v,PeakT=t,PeakA=a,PeakB=b}; l.Add(s);} 
    s.To=t; s.N++; if(biggerWorse? v>s.Peak : v<s.Peak){s.Peak=v;s.PeakT=t;s.PeakA=a;s.PeakB=b;}
  }
  static (long along,long across) Local(Pt at, Pt h, Pt p){ long n=G.Isqrt((long)h.X*h.X+(long)h.Y*h.Y); if(n==0)n=1; long dx=p.X-at.X,dy=p.Y-at.Y; return ((dx*h.X+dy*h.Y)/n,(dx*h.Y-dy*h.X)/n); }
  static IEnumerable<(string id,Pt p,string kind)> Fighters(Game g){
    foreach(var m in g.Crew){ if(g.PosOf(m.Id) is Pt p && m.Mode!=CrewMode.Aboard && m.Mode!=CrewMode.Gone && !m.Gun.Down) yield return (m.Id,p,"rival"); }
    foreach(var h in g.Hostiles){ if(h.Id.StartsWith("rival"))continue; if(h.Present&&!h.Down&&h.Gun.St!="gone") yield return (h.Id,h.Pos(g.Tick),"police"); }
    foreach(var id in new[]{"security-1","security-2","guard","patrol"}) if(g.PosOf(id) is Pt p && g.GunOf(id) is Gun gn && !gn.Down) yield return (id,p,"sec");
  }
  static int Main(string[] a){
    var root=a[0]; var i=Engine.Load(root); var dir=a[1]; var filter=a.Length>2?a[2]:""; bool tl=a.Length>3;
    var ofi=typeof(Game).GetMethod("OfficialHeadingLive",BindingFlags.NonPublic|BindingFlags.Instance)!;
    int H=i.Rules.CarLenCm/2, W=i.Rules.CarWCm/2;
    Console.WriteLine($"car body {i.Rules.CarLenCm}x{i.Rules.CarWCm} clear {i.Rules.CarClearCm}");
    foreach(var f in Directory.GetFiles(dir,"*.json").OrderBy(x=>x,StringComparer.Ordinal)){
      var txt=File.ReadAllText(f); if(!txt.Contains("suborned.sim.script/1"))continue; var name=System.IO.Path.GetFileNameWithoutExtension(f); if(filter!=""&&!name.Contains(filter))continue;
      var sc=Script.Parse(txt); var g=Engine.New(i,sc.Start); int until=sc.Until<=0?Game.T(i.SiteData.Over):sc.Until;
      var list=sc.Orders.Select((o,k)=>(k,o)).OrderBy(x=>x.o.Tick).ThenBy(x=>x.k).ToList(); int oi=0;
      var car=new Dictionary<string,List<Span>>(); var pair=new Dictionary<string,List<Span>>();
      var last=new Dictionary<string,(Pt,string,string,Pt)>(); var car0=new Dictionary<string,List<Span>>();
      for(int t=0;t<=until;t++){
        if(t>0) g.RunTo(t); if(g.Over&&g.Tick!=t)break;
        while(oi<list.Count&&list[oi].o.Tick==t){ g.Issue(list[oi].o); oi++; }
        var cars=g.StandingCars().Select(c=>(c.Id,c.At,c.Heading,mov:"")).ToList();
        if(g.GetawayDriving) cars.Add(("car:getaway",g.GetawayCarPos(),g.GetawayHeading(),"driving"));
        if(!g.StandingCars().Any(c=>c.Id=="car:rivals")&&g.RivalCarPose() is var rp&&rp.HasValue) cars.Add(("car:rivals",rp.Value.pos,rp.Value.heading,"driving"));
        if(!g.StandingCars().Any(c=>c.Id=="car:official")) cars.Add(("car:official",g.CarPos(),(Pt)ofi.Invoke(g,null)!,"driving"));
        var cell=new List<(string,Pt)>(); foreach(var op in g.Ops) if(!op.Inside&&!op.InCar&&!op.CarriedBy.HasValue&&!op.Gun.Down) cell.Add((op.Id.ToString().ToLower(),op.Pos));
        var fi=Fighters(g).GroupBy(x=>x.id).Select(x=>x.First()).ToList();
        var people=cell.Select(c=>(c.Item1,c.Item2)).Concat(fi.Select(x=>(x.id,x.p))).ToList();
        foreach(var (who,p) in people) foreach(var c in cars){
          var (al,ac)=Local(c.At,c.Heading,p); long ox=Math.Abs(al)-H, oy=Math.Abs(ac)-W;
          if(ox<0&&oy<0){ long depth=Math.Min(-ox,-oy); Hit(car,$"{who} in {c.Id}{(c.mov!=""?" ("+c.mov+")":"")}",t,depth,p,c.At,true); }
          else { long d=G.Isqrt(Math.Max(0,ox)*Math.Max(0,ox)+Math.Max(0,oy)*Math.Max(0,oy)); if(d<25) Hit(car0,$"{who} within 25cm of {c.Id}{(c.mov!=""?" ("+c.mov+")":"")}",t,d,p,c.At,false);}
        }
        for(int x=0;x<fi.Count;x++)for(int y=x+1;y<fi.Count;y++){ long d=G.Dist(fi[x].p,fi[y].p); if(d<60) Hit(pair,$"{fi[x].id}+{fi[y].id}",t,d,fi[x].p,fi[y].p,false);}        
        for(int x=0;x<cell.Count;x++)for(int y=x+1;y<cell.Count;y++){ long d=G.Dist(cell[x].Item2,cell[y].Item2); if(d<60) Hit(pair,$"{cell[x].Item1}+{cell[y].Item1}",t,d,cell[x].Item2,cell[y].Item2,false);}        
        if(tl&&t>=int.Parse(a[3])&&t<=int.Parse(a[4])){ foreach(var h in g.Hostiles) if(h.Id.StartsWith("officer")&&(!last.TryGetValue(h.Id,out var w)||w!=(h.At,h.Why,h.Gun.St,h.Home))){ last[h.Id]=(h.At,h.Why,h.Gun.St,h.Home); Console.WriteLine($"  t{t} {h.Id} pos {h.Pos(t)} end {h.At} home {h.Home} why {h.Why} st {h.Gun.St} kiteAt {h.Gun.KiteAt} by {h.Gun.KiteBy}"); } }
        if(tl&&false){ Console.WriteLine($"  t{t} "+string.Join(" ",people.Select(q=>$"{q.Item1}@{q.Item2}"))+" | "+string.Join(" ",cars.Select(c=>$"{c.Id}@{c.At}h{c.Heading}{c.mov}"))); }
        if(g.Over)break;
      }
      g.Finish();
      int carT=car.Values.SelectMany(s=>s).Sum(s=>s.N), pairT=pair.Values.SelectMany(s=>s).Sum(s=>s.N), p50=0;
      Console.WriteLine($"== {name}: {g.Result} {g.FailCode} over t{g.Tick} hash {Hash.Hex(g.Checkpoints[^1].hash)} carTicks {carT} pairTicks<60 {pairT}");
      foreach(var kv in car.OrderBy(k=>k.Value[0].From)) foreach(var s in kv.Value) Console.WriteLine($"   CAR  {s.Key}: t{s.From}-{s.To} ({s.N}) deepest {s.Peak}cm at t{s.PeakT} {s.PeakA} car {s.PeakB}");
      foreach(var kv in car0.OrderBy(k=>k.Value[0].From)) foreach(var s in kv.Value) Console.WriteLine($"   NEAR {s.Key}: t{s.From}-{s.To} ({s.N}) closest {s.Peak}cm at t{s.PeakT} {s.PeakA}");
      foreach(var kv in pair.OrderBy(k=>k.Value[0].From)) foreach(var s in kv.Value) Console.WriteLine($"   PAIR {s.Key}: t{s.From}-{s.To} ({s.N}) closest {s.Peak}cm at t{s.PeakT} {s.PeakA}|{s.PeakB}");
      _=p50;
    }
    return 0;
  }
}
