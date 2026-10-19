// Motive 023 scratch probe (2026-10-19). Run: dotnet run -- walk | run | script <route.json>... (ran against engine 0.8.2, Suborned.Sim.dll built 05:44)
// Motive 023 scratch probe: S05 with the Enforcer running to the door (running is free once loud). Reads the engine, changes nothing.
using System;
using System.IO;
using Suborned.Sim;
static class P
{
    static int Max(params int[] xs) { int m = int.MinValue; foreach (var x in xs) m = Math.Max(m, x); return m; }
    static int Main(string[] a)
    {
        bool runDoor = a.Length > 0 && a[0] == "run";
        if (a.Length > 0 && a[0] == "script")
        {
            var inp0 = Engine.Load(Engine.FindRoot("/Users/sky/Projects/suborned"));
            for (int k = 1; k < a.Length; k++)
            {
                var sc = Script.Parse(File.ReadAllText(a[k]));
                var gg = Engine.Play(inp0, sc.Start, sc.Orders, sc.Until);
                int seal = gg.SecurityArrives >= (int.MaxValue / 4) ? -1 : gg.SecurityArrives + Game.T(gg.SealS);
                Console.WriteLine($"{System.IO.Path.GetFileName(a[k]),-34} {gg.Result,-9} end {Report.Clock(gg.Tick)}  arrives {(gg.SecurityArrives >= (int.MaxValue / 4) ? "-" : Report.Clock(gg.SecurityArrives))}  seal {(seal < 0 ? "-" : Report.Clock(seal))}  margin {(seal < 0 ? "-" : ((seal - gg.Tick) / 10.0).ToString("0.0") + " s")}");
            }
            return 0;
        }
        var root = Engine.FindRoot("/Users/sky/Projects/suborned");
        var inp = Engine.Load(root);
        var r = new Run(Engine.New(inp, Scenarios.Find("S05")!.Start));
        var g = r.G;
        const string Scout = "scout", Fixer = "fixer", Tech = "tech", Enf = "enforcer", Inez = "waitress", GuardR = "guard";
        var LanePostA = new Pt(7050, -1680); var LanePostB = new Pt(7050, -1450);
        r.SendTo(Enf, new Pt(600, -2500));
        r.To(Max(r.Send(Scout, "d:Courtyard"), r.Send(Fixer, "d:Courtyard"), r.Send(Tech, "cabinet")));
        r.Do(Scout, "watch", Inez); r.Do(Tech, "loop", "entrance");
        r.Done(Scout); r.To(r.Send(Scout, "beside-inez"));
        r.Do(Scout, "listen", Inez); r.Done(Scout);
        r.SendTo(Scout, new Pt(8250, -3100));
        for (int i = 0; i < 4 && !g.Inez.Turned; i++)
        {
            bool last = g.Preview("help", Inez).Outcome == "turned";
            r.Do(Fixer, "turn", Inez, "help");
            if (last) { r.Done(Tech); r.Do(Tech, "reroute", "patrol"); }
            r.Done(Fixer);
        }
        r.Do(Fixer, "call", Inez);
        r.To(g.RivalAt); r.To(Math.Max(g.Tick, r.Arrival(Scout))); r.Do(Scout, "mislead", "rivals");
        r.To(g.LureTick + g.VisitWalkTicks);
        r.Fire(Enf, GuardR);
        r.Until(() => g.GuardDown, Game.T(30));
        int shot = g.Tick;
        r.To(r.Send(Enf, "d:Office entrance", runDoor));
        Console.WriteLine($"shot down {Report.Clock(shot)}  at door {Report.Clock(g.Tick)}  security arrives {Report.Clock(g.SecurityArrives)}");
        r.Do(Enf, "holster");
        r.Do(Enf, "enter");
        r.To(Max(r.SendTo(Fixer, LanePostA), r.SendTo(Tech, LanePostB)));
        r.FightOff(() => g.SecurityDown || !r.Op(Enf).Inside, 120, Fixer, Tech);
        r.Done(Enf);
        int outT = g.Tick;
        r.Send(Enf, "d:Getaway bay");
        var car = r.At("d:Getaway bay"); int reach = g.R.AtReachCm;
        while (!g.Over && !G.Within(r.Op(Enf).Pos, car, reach)) g.Step();
        Console.WriteLine($"out {Report.Clock(outT)}  at car {Report.Clock(g.Tick)}  over={g.Over}");
        if (!g.Over) r.Extract();
        g.Finish();
        foreach (var c in r.Checks) if (!c.ok) Console.WriteLine("  [FAIL] " + c.what);
        foreach (var l in g.Log) if (l.Code.StartsWith("fail") || l.Code == "success" || l.Code.StartsWith("security.") || l.Code.StartsWith("enter.") || l.Code.StartsWith("fight.")) Console.WriteLine($"  {Report.Clock(l.Tick)}  {l.Code}  {l.Text}");
        Console.WriteLine($"result {g.Result}");
        return 0;
    }
}
