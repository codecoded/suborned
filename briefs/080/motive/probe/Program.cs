// Brief 080 (Motive): how far runs spread from nudged starts, same fixed orders. Reads only; nudges applied from outside the engine.
using System; using System.Collections.Generic; using System.IO; using System.Linq; using System.Reflection; using Suborned.Sim;
static class P {
  const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
  static object? Get(Game g,string n){ var t=typeof(Game); return t.GetField(n,F)?.GetValue(g) ?? t.GetProperty(n,F)?.GetValue(g); }
  static uint H(string s)=>Streams.Fnv1a32(s);
  static IEnumerable<Gun> Shooters(Game g){ foreach(var o in g.Ops) yield return o.Gun; yield return g.GuardGun; yield return g.PatrolGun; foreach(var s in g.SecGuns) yield return s; foreach(var m in g.Crew) yield return m.Gun; foreach(var h in g.Hostiles) yield return h.Gun; foreach(var u in g.Units) yield return u.Gun; }
  static readonly string[] OpNames={"scout","fixer","tech","enforcer"};
  static void SeedAcc(Game g,int seed){
    var foes=new List<string>{"guard","patrol"}; foes.AddRange(g.SecGuns.Select(s=>s.Id)); foes.AddRange(g.Crew.Select(m=>m.Id)); foes.AddRange(g.Hostiles.Select(h=>h.Id)); foes.AddRange(g.Units.Select(u=>u.Id));
    foreach(var gun in Shooters(g)){ if(gun.W==null) continue; gun.Mem??=new SortedDictionary<string,int>(StringComparer.Ordinal);
      var ts=OpNames.Contains(gun.Id)?foes:(IEnumerable<string>)OpNames;
      foreach(var t in ts) if(!gun.Mem.ContainsKey(t)){ // one start per shooter, whatever the target
        gun.Mem[t]=(int)(H(seed+"/acc/"+gun.Id)%1000); }
    }
  }
  static void SeedCrowd(Game g,int seed,HashSet<string> keep,int spreadS){
    foreach(var w in g.Walkers){ if(keep.Contains(w.P.Id)) continue;
      int k=(int)(H(seed+"/phase/"+w.P.Id)%(uint)(2*spreadS*10+1))-spreadS*10;
      w.PacePct=(int)(H(seed+"/pace/"+w.P.Id)%21);
      var f=(int[])w.From.Clone(); var to=(int[])w.To.Clone();
      for(int i=1;i<f.Length;i++) f[i]=Math.Max(0,f[i]+k); for(int i=0;i<to.Length-1;i++) to[i]=Math.Max(1,to[i]+k);
      w.From=f; w.To=to; }
  }
  static int[] A=new int[6]; static int RivalHp=3; // scout fixer tech guard officer rival
  static int Aim(string id){ if(id=="enforcer")return A[6-6]+0==0?75:75; if(id=="scout")return A[0]; if(id=="fixer")return A[1]; if(id=="tech")return A[2]; if(id=="guard")return A[3]; if(id=="patrol"||id.StartsWith("robot"))return A[3]-5; if(id.StartsWith("rival"))return A[5]; return A[4]; }
  static bool Trained(string id)=>!(id=="scout"||id=="fixer"||id=="tech"||id.StartsWith("rival"));
  static int HpOf(string id){ if(id=="enforcer")return 8; if(id=="scout"||id=="fixer"||id=="tech")return 5; if(id=="guard")return 3; if(id=="patrol"||id.StartsWith("robot"))return 4; if(id.StartsWith("rival"))return RivalHp; return 3; }
  static void Roll(Game g,int seed,bool sheet,int t,HashSet<string> done){
    foreach(var gun in Shooters(g)){ if(gun.W==null) continue;
      if(sheet&&done.Add(gun.Id)){ int hp=HpOf(gun.Id); gun.Max=hp; if(gun.Health>hp||gun.Health==3||gun.Health==5||gun.Health==8||gun.Health==4) gun.Health=hp; }
      long fPm=1000; if(sheet) fPm=(long)Aim(gun.Id)*1000/(Trained(gun.Id)?750:562);
      long u=H(seed+"/roll/"+gun.Id+"/"+t)%1000; gun.Acc=(int)(1000-u*1000/fPm); }
  }
  sealed class Run{ public string Res="",Fail=""; public int End,FightS=-1,Hits,Down,HpLost,T; public int FirstLog=-1; public int Alert; public Dictionary<int,int> Moved=new(); public Dictionary<int,int> ActMoved=new(); public List<string> Log=new(); }
  static Run Play(Engine.Inputs i,string txt,string fam,int seed,List<Dictionary<string,Pt>>? baseline,List<Dictionary<string,Pt>>? record){
    var sc=Script.Parse(txt); if(fam=="rivals"||fam=="all"||fam=="all+roll"||fam=="sheet+all"){ sc.Start.RivalsAtS=190-15+(int)(H(seed+"/rivals")%31); }
    var g=Engine.New(i,sc.Start); int until=sc.Until<=0?Game.T(i.SiteData.Over):sc.Until;
    var keep=new HashSet<string>(); foreach(var r in new[]{"waitress","driver","councillor"}) if(g.GetType().GetMethod("PersonByRole",F)?.Invoke(g,new object[]{r}) is Person p) keep.Add(p.Id); if(g.GuardP!=null) keep.Add(g.GuardP.Id);
    if(fam=="crowd"||fam=="crowd+sheet"||fam=="all"||fam=="all+roll"||fam=="sheet+all") SeedCrowd(g,seed,keep,20);
    var list=sc.Orders.Select((o,k)=>(k,o)).OrderBy(x=>x.o.Tick).ThenBy(x=>x.k).ToList(); int oi=0;
    var goOrder=sc.Orders.LastOrDefault(o=>o.Do=="go"); var run=new Run(); int sample=0; var done=new HashSet<string>();
    for(int t=0;t<=until;t++){
      if(fam=="acc"||fam=="all") SeedAcc(g,seed);
      if(fam=="roll"||fam=="all+roll") Roll(g,seed,false,t,done);
      if(fam=="sheet"||fam=="sheet+all"||fam=="crowd+sheet") Roll(g,seed,true,t,done);
      if(t>0) g.RunTo(t); if(g.Over&&g.Tick!=t)break;
      while(oi<list.Count&&list[oi].o.Tick==t){ g.Issue(list[oi].o); oi++; }
      if(t%100==0){ var pos=new Dictionary<string,Pt>();
        foreach(var o in g.Ops) if(!o.Inside&&!o.InCar) pos[Op.Name(o.Id)]=o.Pos;
        foreach(var m in g.Crew) if(g.PosOf(m.Id) is Pt mp) pos[m.Id]=mp;
        foreach(var h in g.Hostiles) if(g.PosOf(h.Id) is Pt hp) pos[h.Id]=hp;
        foreach(var id in new[]{"guard","patrol","waitress","driver","councillor"}) if(g.PosOf(id) is Pt ip) pos[id]=ip;
        foreach(var w in g.Walkers) if(g.PosOf(w.P.Id) is Pt wp) pos["w:"+w.P.Id]=wp;
        record?.Add(pos);
        if(baseline!=null&&sample<baseline.Count){ var b=baseline[sample]; int mv=0,am=0,tot=0; foreach(var kv in pos){ if(b.TryGetValue(kv.Key,out var bp)){ tot++; if(G.D2(kv.Value,bp)>100*100){ mv++; if(!kv.Key.StartsWith("w:")) am++; } } } run.Moved[t/10]=mv; run.ActMoved[t/10]=am; }
        sample++; }
      if(g.Over) break;
      if(goOrder!=null&&t>goOrder.Tick&&t%10==0&&!g.Over){ goOrder.Tick=t; g.Issue(goOrder); }
    }
    if(!g.Over) g.RunTo(until);
    run.Res=g.Result.ToString(); run.Fail=g.FailCode; run.End=g.Tick; run.Alert=g.Alert; run.T=g.Trail;
    var fs=Convert.ToInt32(Get(g,"FightSprung")??int.MaxValue); var fo=Convert.ToInt32(Get(g,"FightOverTick")??int.MaxValue);
    if(fs>=0&&fs<(1<<28)){ int e=fo<(1<<28)?fo:g.Tick; run.FightS=(e-fs)/10; }
    foreach(var o in g.Ops){ run.HpLost+=o.Gun.Max-o.Gun.Health; if(o.Gun.Down) run.Down++; }
    foreach(var l in g.Log) run.Log.Add(l.Tick+"|"+l.Code);
    return run;
  }
  static string Clock(int ticks)=>$"{ticks/600}:{(ticks/10)%60:00}";
  static int Main(string[] a){
    var root=a[0]; var i=Engine.Load(root); var dir=a[1]; int seeds=int.Parse(a[2]); var routes=a[3].Split(','); A=a[4].Split(',').Select(int.Parse).ToArray(); RivalHp=int.Parse(a[5]);
    foreach(var name in routes){ var txt=File.ReadAllText(System.IO.Path.Combine(dir,name+".json"));
      var rec=new List<Dictionary<string,Pt>>(); var b=Play(i,txt,"base",0,null,rec);
      Console.WriteLine($"\n## {name} base {b.Res} {b.Fail} end {Clock(b.End)} fight {b.FightS}s hp-lost {b.HpLost} down {b.Down} T {b.T} A {b.Alert}");
      foreach(var fam in new[]{"roll","sheet"}){
        var rs=new List<Run>(); for(int s=1;s<=seeds;s++) rs.Add(Play(i,txt,fam,s,rec,null));
        int wins=rs.Count(r=>r.Res=="Success"); var fails=string.Join(",",rs.Where(r=>r.Res!="Success").GroupBy(r=>r.Res=="Failure"?r.Fail:r.Res).OrderByDescending(x=>x.Count()).Select(x=>x.Key+"×"+x.Count()));
        var fl=rs.Where(r=>r.FightS>=0).Select(r=>r.FightS).OrderBy(x=>x).ToList(); var hp=rs.Select(r=>r.HpLost).OrderBy(x=>x).ToList(); var en=rs.Select(r=>r.End).OrderBy(x=>x).ToList(); var dn=rs.Select(r=>r.Down).Sum();
        int firstLog(Run r){ for(int k=0;k<Math.Min(r.Log.Count,b.Log.Count);k++) if(r.Log[k]!=b.Log[k]) return int.Parse(r.Log[k].Split('|')[0]); return r.Log.Count==b.Log.Count?-1:0; }
        var fd=rs.Select(firstLog).Where(x=>x>=0).OrderBy(x=>x).ToList();
        string med(List<int> l)=>l.Count==0?"-":l[l.Count/2].ToString();
        // divergence of actors (cell, cast, rivals, police) and of the crowd, at fixed clock marks
        var marks=new[]{60,120,180,240,300,360,420}; var mv=string.Join(" ",marks.Select(m=>{ var x=rs.Where(r=>r.ActMoved.ContainsKey(m)).ToList(); if(x.Count==0) return $"{m/60}:{m%60:00}=-"; int am=x.Select(r=>r.ActMoved[m]).OrderBy(v=>v).ToList()[x.Count/2], cm=x.Select(r=>r.Moved[m]-r.ActMoved[m]).OrderBy(v=>v).ToList()[x.Count/2]; return $"{m/60}:{m%60:00}={am}a/{cm}c"; }));
        Console.WriteLine($"{fam,-7} win {wins}/{rs.Count} [{fails}] end {Clock(en[0])}..{Clock(en[^1])} fight {(fl.Count==0?"-":$"{fl[0]}..{fl[^1]} med {med(fl)}")}s hp {hp[0]}..{hp[^1]} med {med(hp)} downs/run {(double)dn/rs.Count:0.00} log-differs med {(fd.Count==0?"never":Clock(fd[fd.Count/2]))} ({fd.Count}/{rs.Count})");
        Console.WriteLine($"        moved>1m median {mv}");
      }
    }
    return 0;
  }
}
