// Pulse scratch: per-run record of what the rival crew did under fire.
using System; using System.Collections.Generic; using System.Globalization; using System.IO; using System.Linq; using Suborned.Sim;
static class RivalStay
{
    sealed class M { public int FirstShotAt=-1, ShotsAt, Fired, FiredAfterShot, Left=-1, Down=-1, LeftHp=-1, LeftDist=-1, MaxHp, Hits; public string LeftHow=""; public int BackOffs; public int BackOffCm; public int WalkStartDist=-1, InFightT, HiddenT, AnswerT, UpAfterShotT; public bool Moving; public string LeftState="";}
    public static int Run(Engine.Inputs inputs, string root, string[] args)
    {
        _ = Scenarios.All.Count;
        int n=int.Parse(args[Array.IndexOf(args,"--seeds")+1],CultureInfo.InvariantCulture); var outp=args[Array.IndexOf(args,"--out")+1];
        int from=Array.IndexOf(args,"--seed-from") is var fi && fi>0?int.Parse(args[fi+1],CultureInfo.InvariantCulture):1;
        var files=new List<string>(); for(int k=1;k<args.Length;k++){var a=args[k]; if(a=="--seeds"||a=="--out"||a=="--seed-from"){k++;continue;} if(Directory.Exists(a)) files.AddRange(Directory.GetFiles(a,"*.json").OrderBy(x=>x,StringComparer.Ordinal)); else files.Add(a);}        
        var items=new List<(string f,int s)>(); foreach(var f in files) for(int s=0;s<n;s++) items.Add((f,from+s));
        var res=Par.Map(items.Count,BatchCli.Jobs(args),i=>Scenarios.Bounded(()=>One(inputs,root,items[i].f,items[i].s)));
        using var w=new StreamWriter(outp); int errs=0; foreach(var (v,e) in res){ if(e!=null){errs++; Console.Error.WriteLine(e.Message);} else w.WriteLine(v);} Console.WriteLine($"{items.Count-errs} runs, {errs} errors"); return errs>0?1:0;
    }
    static string One(Engine.Inputs inputs,string root,string file,int seed)
    {
        var plan=Adaptive.Load(file,root); var ms=new Dictionary<string,M>(); int preBreak=-1, finBreak=-1, back=-1, fightOver=-1, firstAt=-1, firstHitBack=-1, sprung=-1, fightStart=-1; string branch="";
        int logAt=0; var snap=new List<string>();
        void Snap(Game g,string what){ foreach(var c in g.Crew){ var m=ms[c.Id]; var p=g.MemberPose(c); int d=-1; if(p.HasValue){ long best=long.MaxValue; foreach(var o in g.Ops) if(!o.Down&&!o.Inside&&!o.InCar){var dd=G.D2(o.Pos,p.Value.pos); if(dd<best)best=dd;} if(best<long.MaxValue)d=(int)(Math.Sqrt(best)/100);} snap.Add($"{what}|{c.Id}|{(c.Gun.Down?"down":c.Mode==CrewMode.Gone?"gone":c.Mode.ToString())}|hp{c.Gun.Health}|d{d}|{(c.H!=null?c.H.Gun.St:"")}|{c.Goal}"); } }
        Adaptive.OnTick=(g,t)=>{
            if(ms.Count==0) foreach(var c in g.Crew) ms[c.Id]=new M{MaxHp=c.Gun.Health};
            foreach(var e in g.ShotsThisTick){
                if(ms.TryGetValue(e.Target,out var v)){ if(v.FirstShotAt<0)v.FirstShotAt=t; v.ShotsAt++; if(e.Hit)v.Hits++; if(firstAt<0)firstAt=t; }
                if(ms.TryGetValue(e.By,out var b)){ b.Fired++; if(b.FirstShotAt>=0)b.FiredAfterShot++; if(firstAt>=0&&firstHitBack<0)firstHitBack=t; }
            }
            for(;logAt<g.Log.Count;logAt++){ var l=g.Log[logAt]; 
                if(l.Code=="rivals.retreat"&&preBreak<0){preBreak=l.Tick;Snap(g,"pre");}
                if(l.Code=="ai.break"&&finBreak<0){finBreak=l.Tick;Snap(g,"fin");}
                if(l.Code=="rivals.back"&&back<0)back=l.Tick; if(l.Code=="fight.over"&&fightOver<0)fightOver=l.Tick; }
            if(g.FightSprung<int.MaxValue/4&&sprung<0)sprung=g.FightSprung; if(g.FightBranch.Length>0&&fightStart<0){fightStart=t;branch=g.FightBranch;}
            foreach(var c in g.Crew){ var m=ms[c.Id];
                if(c.Gun.Down){ if(m.Down<0)m.Down=t; continue; }
                var h=c.H; bool broke=(h!=null&&(h.Gun.St=="break"||h.Gun.St=="gone"))||(c.Mode==CrewMode.Walking&&c.Goal=="flee")||(preBreak>=0&&c.Mode==CrewMode.Walking&&c.Goal=="exit")||(preBreak>=0&&g.CrewBrokenPre&&(c.Mode==CrewMode.Aboard||c.Mode==CrewMode.Gone));
                if(broke&&m.Left<0){ m.Left=t; m.LeftHp=c.Gun.Health; m.LeftHow=(h!=null?"fin":"pre")+":"+c.Mode; }
                if(m.FirstShotAt>=0&&m.Left<0) m.UpAfterShotT++;
                // back-off: a continuous walk, not broken, that ends ≥5 m farther from the nearest op than it began
                var pose=g.MemberPose(c); if(!pose.HasValue||broke) {m.Moving=false; continue;}
                long best=long.MaxValue; Pt? nop=null; foreach(var o in g.Ops) if(!o.Down&&!o.Inside&&!o.InCar){var dd=G.D2(o.Pos,pose.Value.pos); if(dd<best){best=dd;nop=o.Pos;}}
                if(nop==null){m.Moving=false;continue;} int dist=(int)Math.Sqrt(best);
                bool walking=h!=null? h.Moving(t) : c.Disp!=null;
                if(firstAt>=0){ m.InFightT++; if(!g.PSees(pose.Value.pos,nop.Value)) m.HiddenT++; if(h!=null&&h.Why=="answer"&&h.Moving(t)) m.AnswerT++; if(h==null&&c.Disp!=null) m.AnswerT++; }
                if(firstAt>=0){ if(walking&&!m.Moving){m.Moving=true;m.WalkStartDist=dist;} else if(!walking&&m.Moving){m.Moving=false; if(dist-m.WalkStartDist>=500){m.BackOffs++;m.BackOffCm+=dist-m.WalkStartDist;}} }
            }
        };
        Adaptive.Extra x; Game gm; try { gm=Adaptive.Play(inputs,plan,seed,out x);} finally {Adaptive.OnTick=null;}
        var w=new JWriter().Obj().Prop("plan",plan.Id).Prop("seed",seed).Prop("outcome",gm.Result.ToString().ToLowerInvariant()).Prop("fail",gm.FailCode).Prop("tick",gm.Tick).Prop("branch",branch).Prop("fightStart",fightStart).Prop("sprung",sprung).Prop("preBreak",preBreak).Prop("finBreak",finBreak).Prop("back",back).Prop("fightOver",fightOver).Prop("firstAt",firstAt).Prop("trail",gm.Trail).Prop("alert",gm.Alert).Prop("standing",gm.Standing);
        w.Prop("hpLost",gm.CellHpLost).Prop("opsDown",gm.CellDownCount).Prop("cellFightS",gm.CellFightS).Prop("cellShots",gm.CellShots).Prop("cellHits",gm.CellHits);
        w.OpenArr("crew"); foreach(var kv in ms){var m=kv.Value; w.Obj().Prop("id",kv.Key).Prop("firstShot",m.FirstShotAt).Prop("shotsAt",m.ShotsAt).Prop("hits",m.Hits).Prop("fired",m.Fired).Prop("firedAfter",m.FiredAfterShot).Prop("left",m.Left).Prop("leftHp",m.LeftHp).Prop("leftHow",m.LeftHow).Prop("down",m.Down).Prop("backOffs",m.BackOffs).Prop("backOffCm",m.BackOffCm).Prop("inFight",m.InFightT).Prop("hidden",m.HiddenT).Prop("answer",m.AnswerT).Prop("upAfter",m.UpAfterShotT).Prop("maxHp",m.MaxHp).EndObj();} w.EndArr();
        if(gm.Cause!=null){w.Key("cause");gm.Cause.Write(w);} Snap(gm,"end"); w.OpenArr("snap"); foreach(var s in snap) w.Str(s); w.EndArr();
        return w.EndObj().ToString();
    }
}
