// Waymark scratch (brief 027): a scripted loud player. Plays the loud reference's head (orders before HEAD_END) with its own waits, then in the
// final fight gives the locks a player would: each agent on its own target (or all on one), re-locking as targets fall, by a named rule.
// Only orders, all logged: the run it prints is an order script the engine replays (WRITE=<file>).
// args: base.json  env: S=x,y F=x,y T=x,y (waits, cm)  LOCK=scout:security-2,fixer:security-1,...  RELOCK=none|near|blocker|threat|focus
//       STANCE=free|return  ENF=go|wait  BOARD=enf|all  LOG=1  WRITE=file  DRAWAT=tick (draw early: a drawn weapon is seen)
// batch (brief 042, Splice; the one-play path above is unchanged): `player --batch <jobs.jsonl | -> [--jobs n]` plays many variants in one process on
//       all cores. Each input line is {"base":"base.json","env":{"S":"975,-4625",...}} (env = the variant's overrides on top of the process env);
//       each output line, in input order, is {"out":<exactly what a process would print to stdout>,"err":<its stderr>,"code":0|1}.
using System; using System.Collections.Generic; using System.IO; using System.Linq; using System.Threading; using Suborned.Sim;
static class P {
  [ThreadStatic] static Dictionary<string, string>? Over; // batch: this play's env overrides (a thread plays one variant at a time)
  static string Env(string k, string d = "") => (Over != null && Over.TryGetValue(k, out var ov) ? ov : Environment.GetEnvironmentVariable(k)) ?? d;
  static Pt? Pos(string k) { var v = Env(k); if (v == "") return null; var a = v.Split(','); return new Pt(int.Parse(a[0]), int.Parse(a[1])); }
  static int Main(string[] a) {
    if (a.Length > 0 && a[0] == "--batch") return BatchMain(a);
    return Run(a, Console.Out, Console.Error, null);
  }
  static Engine.Inputs? inputs; static readonly object inputsLock = new();
  static Engine.Inputs Load() { lock (inputsLock) { return inputs ??= Engine.Load(Engine.FindRoot(Directory.GetCurrentDirectory())); } }
  static int Run(string[] a, TextWriter so, TextWriter se, Engine.Inputs? preloaded) {
    var i = preloaded ?? Engine.Load(Engine.FindRoot(Directory.GetCurrentDirectory()));
    var sc = Script.Parse(File.ReadAllText(a[0]));
    var S = Pos("S"); var F = Pos("F"); var Tp = Pos("T");
    var orders = new List<Order>();
    int cut = int.Parse(Env("KEEP", "2255")); bool quiet = Env("QUIET") == "1";
    foreach (var o in sc.Orders) if (o.Tick < cut && !(quiet && o.Do == "panic")) orders.Add(o); // the fight's orders are this player's
    foreach (var (n, pt) in new[] { ("scout", S), ("fixer", F), ("tech", Tp) }) { if (!pt.HasValue) continue; var last = orders.LastOrDefault(o => o.Op == n && o.Do == "move"); if (last != null) { last.Point = pt; last.To = ""; } }
    var dropOps0 = Env("DROPHEAD").Split(',', StringSplitOptions.RemoveEmptyEntries); if (dropOps0.Length > 0) { int da0 = int.Parse(Env("DROPAT", "1510")); orders = orders.Where(o => !(dropOps0.Contains(o.Op) && o.Tick >= da0)).ToList(); }
    if (Env("ADD") != "") { var add = Script.Parse("{\"schema\":\"suborned.sim.script/1\",\"mission\":\"handover\",\"start\":{},\"until\":6000,\"orders\":" + File.ReadAllText(Env("ADD")) + "}"); orders.AddRange(add.Orders); }
    var g = Engine.New(i, sc.Start); orders = orders.OrderBy(o => o.Tick).ToList(); int k = 0;
    string[] three = { "scout", "fixer", "tech" };
    var locks = Env("LOCK").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Split(':')).ToDictionary(x => x[0], x => x[1]);
    string relock = Env("RELOCK", "near"), stance = Env("STANCE", "free"), enf = Env("ENF", "go");
    int drawAt = int.Parse(Env("DRAWAT", "-1")), delay = int.Parse(Env("DELAY", "0"));
    var locks2 = Env("LOCK2").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Split(':')).ToDictionary(x => x[0], x => x[1]);
    bool l2 = false, l1 = false, m2 = false, m3 = false, l3 = false; var idleSince = new Dictionary<string, int>();
    var way = Env("ENFPATH").Split(';', StringSplitOptions.RemoveEmptyEntries).Select(x => { var c = x.Split(','); return new Pt(int.Parse(c[0]), int.Parse(c[1])); }).ToList(); int wp = 0; bool atBay = false;
    var esc = Env("ESCORT").Split(';', StringSplitOptions.RemoveEmptyEntries).Select(x => { var q = x.Split(':'); var c = q[1].Split(','); return (q[0], new Pt(int.Parse(c[0]), int.Parse(c[1]))); }).ToList();
    int escEvery = int.Parse(Env("ESCEVERY", "20")); string escStop = Env("ESCSTOP");
    var relockOps = Env("RELOCKOPS", "scout,fixer,tech").Split(',');
    bool fight = false, enfIn = false, enfMoving = false; int fightTick = -1;
    var log = new List<string>(); string L2C = Env("L2CODE", "security.officer-down");
    Order O(string op, string d, string t = "") => new Order { Tick = g.Tick, Op = op, Do = d, Target = t };
    bool Give(Order o) { var r = g.Issue(o); if (r.ok) log.Add($"{Report.Clock(g.Tick)} {o.Op} {o.Do} {o.Target}{o.To}"); return r.ok; }
    var given = new List<Order>();
    bool G2(Order o) { o.Tick = g.Tick; if (Give(o)) { given.Add(o); return true; } return false; }
    Op OpN(string n) => g.Ops.First(x => x.Id.ToString().ToLowerInvariant() == n);
    IEnumerable<Hostile> Live() => g.Hostiles.Where(h => h.Present && !h.Down && h.Gun.St != "break" && h.Gun.St != "gone");
    long D(Pt p, Pt q) => (long)(p.X - q.X) * (p.X - q.X) + (long)(p.Y - q.Y) * (p.Y - q.Y);
    string Pick(Op op) { var relock0 = relock; relock = Env("RELOCK_" + Suborned.Sim.Op.Name(op.Id), relock); try { return Pick0(op); } finally { relock = relock0; } }
    string Pick0(Op op) {
      if (relock == "rivals" || relock == "rivals-near") { var pr = g.PreviewFire(op.Id, "rivals", false); if (g.RivalsPresent && !g.RivalsGun.Down && pr.Refused == "" && !pr.Walk) return "rivals"; if (relock == "rivals") return ""; }
      var opts = Live().Where(h => { var p = g.PreviewFire(op.Id, h.Id); return p.Refused == "" && !p.Walk; }).ToList();
      if (opts.Count == 0) return "";
      if (relock == "blocker" && opts.FirstOrDefault(h => h.Blocker) is Hostile b) return b.Id;
      if (relock == "threat") { var t = opts.Where(h => h.Gun.Target == "enforcer").OrderBy(h => D(h.Pos(g.Tick), op.Pos)).FirstOrDefault() ?? opts.FirstOrDefault(h => h.Blocker); if (t != null) return t.Id; }
      if (relock == "focus") { foreach (var n in three) { var o2 = OpN(n); if (o2 != op && o2.Gun.Order.Length > 0 && opts.Any(h => h.Id == o2.Gun.Order)) return o2.Gun.Order; } }
      return opts.OrderBy(h => D(h.Pos(g.Tick), op.Pos)).ThenBy(h => h.Id, StringComparer.Ordinal).First().Id;
    }
    while (!g.Over && g.Tick < 6000) {
      while (k < orders.Count && orders[k].Tick == g.Tick) { g.Issue(orders[k]); given.Add(orders[k]); k++; }
      if (drawAt >= 0 && g.Tick == drawAt) foreach (var n in three) G2(O(n, "draw"));
      if (Env("FIREAT") != "" && g.Tick == int.Parse(Env("FIREAT"))) foreach (var x in Env("LOCKA").Split(',')) { var q = x.Split(':'); if (!G2(O(q[0], "fire", q[1]))) se.WriteLine($"refused {x}: {g.Issue(O(q[0], "fire", q[1])).code}"); }
      if (Env("STANCEAT") != "" && g.Tick == int.Parse(Env("STANCEAT"))) foreach (var n in three) G2(new Order { Op = n, Do = "stance", Stance = Env("STANCEA", "free") });
      if (!fight && g.FightBranch == (quiet ? "safe" : "loud") && (!quiet || g.Tick >= g.FightSprung)) {
        fight = true; fightTick = g.Tick;
        foreach (var n in three) { if (!OpN(n).Gun.Drawn) G2(O(n, "draw")); G2(new Order { Op = n, Do = "stance", Stance = stance }); }
        if (enf == "go" && !quiet) { if (way.Count > 0) { G2(new Order { Op = "enforcer", Do = "move", Point = way[wp++] }); } else G2(new Order { Op = "enforcer", Do = "move", To = "d:Getaway bay" }); enfMoving = true; }
      }
      if (fight) {
        if (quiet && !enfMoving && g.Log.Any(l => l.Code == "fight.over")) { G2(new Order { Op = "enforcer", Do = "move", To = "d:Getaway bay" }); enfMoving = true; }
        if (quiet && enfIn) { G2(new Order { Op = "cell", Do = "go" }); }
        if (!quiet && !enfMoving && enf == "waitriv" && (g.Log.Any(l => l.Code == "rivals.retreat") || (Env("WAITMAX") != "" && g.Tick >= fightTick + int.Parse(Env("WAITMAX"))))) { if (way.Count > 0) G2(new Order { Op = "enforcer", Do = "move", Point = way[wp++] }); else G2(new Order { Op = "enforcer", Do = "move", To = "d:Getaway bay" }); enfMoving = true; }
        if (!quiet && !enfMoving && enf == "wait" && !Live().Any(h => h.Blocker)) { G2(new Order { Op = "enforcer", Do = "move", To = "d:Getaway bay" }); enfMoving = true; }
        if (!l1 && g.Tick >= fightTick + int.Parse(Env("LOCKDELAY", "0"))) { l1 = true; foreach (var (n, t) in locks) G2(O(n, "fire", t)); }
        if (!l3 && Env("LOCK3") != "" && g.Log.Any(l => l.Code == Env("L3CODE", "security.down"))) { l3 = true; foreach (var x in Env("LOCK3").Split(',')) { var q = x.Split(':'); G2(new Order { Tick = g.Tick, Op = q[0], Do = "fire", Target = q[1], Focus = q.Length > 2 && q[2] == "focus" }); } }
        if (!l2 && locks2.Count > 0 && g.Log.Any(l => l.Code == L2C) && g.Tick >= g.Log.First(l => l.Code == L2C).Tick + int.Parse(Env("LOCK2DELAY", "0"))) { l2 = true; foreach (var (n, t) in locks2) G2(O(n, "fire", t)); }
        if (!m2 && Env("MOVE2") != "" && g.Log.Any(l => l.Code == Env("MOVE2CODE", "security.down"))) { m2 = true; foreach (var x in Env("MOVE2").Split(';')) { var q = x.Split(':'); var c = q[1].Split(','); G2(new Order { Op = q[0], Do = "move", Point = new Pt(int.Parse(c[0]), int.Parse(c[1])), Run = q.Length > 2 && q[2] == "run" }); } }
        if (!m3 && Env("MOVE3") != "" && g.Log.Any(l => l.Code == Env("MOVE3CODE", "rivals.retreat"))) { m3 = true; foreach (var x in Env("MOVE3").Split(';')) { var q = x.Split(':'); var c = q[1].Split(','); G2(new Order { Op = q[0], Do = "move", Point = new Pt(int.Parse(c[0]), int.Parse(c[1])), Run = q.Length > 2 && q[2] == "run" }); } }
        if (relock != "none" && g.Tick > fightTick) foreach (var n in three) { if (!relockOps.Contains(n)) continue; var op = OpN(n); if (op.Down || op.InCar || op.Gun.Order.Length > 0 || op.Busy != null || op.Walk != null) { idleSince.Remove(n); continue; }
          if (!idleSince.ContainsKey(n)) idleSince[n] = g.Tick; if (g.Tick - idleSince[n] < delay) continue; var t = Pick(op); if (t != "") { G2(O(n, "fire", t)); idleSince.Remove(n); } }
        var e = g.Enforcer;
        if (enfMoving && e.Walk == null && e.Busy == null && !e.InCar && wp < way.Count) { var w = way[wp++]; G2(new Order { Op = "enforcer", Do = "move", Point = w }); }
        else if (!e.InCar && enfMoving && e.Walk == null && e.Busy == null && way.Count > 0 && !atBay) { if (Env("ENFCLEAR") != "1" || !Live().Any()) { atBay = true; G2(new Order { Op = "enforcer", Do = "move", To = "d:Getaway bay" }); } }
        else if (!e.InCar && enfMoving && e.Walk == null && e.Busy == null) G2(O("enforcer", "board", "car"));
        if (esc.Count > 0 && !e.InCar && g.Tick % escEvery == 0 && !(escStop != "" && g.Log.Any(l => l.Code == escStop))) foreach (var (n, off) in esc) { var op = OpN(n); if (op.Down || op.InCar || op.Busy != null) continue; var to = new Pt(e.Pos.X + off.X, e.Pos.Y + off.Y); if (D(op.Pos, to) > 150L * 150) G2(new Order { Op = n, Do = "move", Point = to }); }
        if (e.InCar) enfIn = true;
        bool road = Env("FIGHTOUT") == "1" ? !Live().Any() : !Live().Any(h => h.Blocker);
        if (!quiet && enfIn && (road || Env("BOARD") == "all")) foreach (var n in three) { var op = OpN(n); if (op.Down || op.InCar || op.Busy != null) continue;
            if (D(op.Pos, g.S.Stand("d:Getaway bay")) > 30L * 30) { if (op.Walk == null) G2(new Order { Op = n, Do = "move", To = "d:Getaway bay" }); } else G2(O(n, "board", "car")); }
        if (!quiet && enfIn && three.All(n => OpN(n).InCar || OpN(n).Down)) { if (G2(new Order { Op = "cell", Do = "go" })) { } }
      }
      if (Env("DBG") == "1" && fight && g.Tick <= fightTick + 25) foreach (var n in three) { var op = OpN(n); so.WriteLine($"{Report.Clock(g.Tick)} {n} order={op.Gun.Order} target={op.Gun.Target} next={op.Gun.NextShot} drawn={op.Gun.Drawn}/{op.Gun.DrawReady}"); }
      if (Env("TRACE") != "" && g.Tick >= int.Parse(Env("TRACE")) && g.Tick % int.Parse(Env("TEVERY","10")) == 0) {
        string P2(Pt p) => $"({p.X/100.0:0.#},{p.Y/100.0:0.#})";
        var rp = g.RivalsPose(); var sb = new System.Text.StringBuilder($"T {Report.Clock(g.Tick)} A{g.Alert} ");
        foreach (var o in g.Ops) sb.Append($"{o.Id.ToString().ToLowerInvariant()[0]}{P2(o.Pos)}h{o.Gun.Health}{(o.Down?"X":"")}{(o.Gun.Drawn?"d":"")}>{o.Gun.Target} ");
        sb.Append($"| R{(rp.HasValue?P2(rp.Value.pos):"-")}h{g.RivalsGun.Health} drawn={g.RivalsDrawn} st={g.RivalsGun.St}>{g.RivalsGun.Target} |");
        foreach (var h in g.Hostiles.Where(h => h.Present && !h.Down)) sb.Append($" {h.Id}{P2(h.Pos(g.Tick))}>{h.Gun.Target}");
        if (g.Hostiles.Count == 0) foreach (var id in new[] { "security-1", "security-2" }.Concat(g.Units.Select(u => u.Id))) { var pp = g.PosOf(id); if (pp.HasValue) sb.Append($" {id}{P2(pp.Value)}"); }
        so.WriteLine(sb.ToString());
      }
      g.Step();
    }
    g.Finish();
    int lost = g.Ops.Sum(o => o.Gun.Max - Math.Max(0, o.Gun.Health)), max = g.Ops.Sum(o => o.Gun.Max);
    var down = string.Join(",", g.Ops.Where(o => o.Down).Select(o => o.Id.ToString().ToLowerInvariant()));
    var leftL = string.Join(",", g.Log.Where(l => l.Code == "captured").Select(l => l.Text.Split(' ')[1])); so.WriteLine($"{g.Result} {g.FailCode} left[{leftL}] {Report.Clock(g.Tick)} T{g.Trail} A{g.Alert} hp-lost {lost * 100 / max}% down[{down}] foes-down {g.Hostiles.Count(h => h.Down)}/{g.Hostiles.Count(h => h.Present)}");
    if (Env("LOG") == "1") { foreach (var l in log) so.WriteLine("  > " + l); foreach (var l in g.Log.Where(l => l.Tick >= 2250 && (l.Code.StartsWith("shot") || l.Code.Contains("down") || l.Code.StartsWith("fight") || l.Code.StartsWith("ai.join") || l.Code.StartsWith("board") || l.Code.StartsWith("fail") || l.Code.StartsWith("fire")))) so.WriteLine($"   {Report.Clock(l.Tick)} {l.Code} {l.Text}"); }
    if (Env("WRITE") is var wf && wf != "") File.WriteAllText(wf, Script.Write(g, i.Hash) + "\n");
    return 0;
  }
  // ---- batch (brief 042) ----
  static int BatchMain(string[] a) {
    string src = a.Length > 1 ? a[1] : "-"; int jobsN = Environment.ProcessorCount;
    for (int q = 2; q + 1 < a.Length; q++) if (a[q] == "--jobs") jobsN = Math.Max(1, int.Parse(a[q + 1]));
    var lines = (src == "-" ? Console.In.ReadToEnd() : File.ReadAllText(src)).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    var specs = lines.Select(l => { using var d = System.Text.Json.JsonDocument.Parse(l); var r = d.RootElement;
      var env = new Dictionary<string, string>(); if (r.TryGetProperty("env", out var e)) foreach (var kv in e.EnumerateObject()) env[kv.Name] = kv.Value.GetString() ?? "";
      return (bs: r.GetProperty("base").GetString()!, env); }).ToList();
    var inp = Load(); var res = new string?[specs.Count]; int next = -1, emit = 0; var emitLock = new object(); var so = new StreamWriter(Console.OpenStandardOutput(), new System.Text.UTF8Encoding(false)) { AutoFlush = false };
    void Work() {
      int k;
      while ((k = Interlocked.Increment(ref next)) < specs.Count) {
        var o = new StringWriter(); var e = new StringWriter(); int code = 0; Over = specs[k].env;
        try { Run(new[] { specs[k].bs }, o, e, inp); } catch (Exception ex) { code = 1; e.WriteLine(ex.ToString()); } finally { Over = null; }
        var line = System.Text.Json.JsonSerializer.Serialize(new { @out = o.ToString(), err = e.ToString(), code });
        lock (emitLock) { res[k] = line; while (emit < res.Length && res[emit] != null) { so.Write(res[emit]); so.Write('\n'); res[emit] = ""; emit++; } so.Flush(); }
      }
    }
    var ts = new Thread[Math.Max(1, Math.Min(jobsN, specs.Count))];
    for (int t = 0; t < ts.Length; t++) { ts[t] = new Thread(Work, 64 << 20); ts[t].Start(); }
    foreach (var t in ts) t.Join();
    return 0;
  }
}
