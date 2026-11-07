// suborned-sim dial traffic (brief 098 step 1, law L8): a script played under the dial's cars with every tick watched, for the law's evidence.
//   dial traffic [script] [--counts cars=8,...] [--seed n] [--every s] [--json f]
//     per tick: the cars on the zone's roads, moving and standing; two passing cars' footprints meeting; a mission car's meeting a passing
//     car's; anyone on foot under a moving car; the longest a car has stood; trips started and left. Exit 1 if a moving car is ever over a
//     person, or two moving passing cars meet (the law's own promises); the rest is information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Suborned.Sim;

static class TrafficCli
{
    public static int Run(Engine.Inputs inputs, string root, string[] args)
    {
        string script = args.Length > 2 && !args[2].StartsWith("--", StringComparison.Ordinal) ? args[2] : "contracts/missions/handover/routes/route-a.json";
        var sc = Script.Parse(File.ReadAllText(File.Exists(script) ? script : System.IO.Path.Combine(root, script)));
        int ci = Array.IndexOf(args, "--counts"), si = Array.IndexOf(args, "--seed"), ei = Array.IndexOf(args, "--every");
        DialCli.Apply(sc.Start, ci > 0 ? DialCli.Parse(args[ci + 1]) : DialCli.Parse("cars=8"));
        if (si > 0) sc.Start.Seed = int.Parse(args[si + 1], CultureInfo.InvariantCulture);
        int every = ei > 0 ? int.Parse(args[ei + 1], CultureInfo.InvariantCulture) * Game.Hz : 30 * Game.Hz;
        var g = Engine.New(inputs, sc.Start);
        var orders = sc.Orders.OrderBy(o => o.Tick).ToList(); int oi = 0, until = sc.Until > 0 ? sc.Until : Game.T(inputs.SiteData.Over);
        int carCar = 0, carMission = 0, overPerson = 0, maxStand = 0, ticks = 0; long zone = 0; string worstStand = "";
        var stoodSince = new Dictionary<string, int>(StringComparer.Ordinal); var lines = new List<string>();
        int hl = inputs.Rules.CarLenCm / 2, hw = inputs.Rules.CarWCm / 2;
        while (g.Tick < until && !g.Over)
        {
            while (oi < orders.Count && orders[oi].Tick == g.Tick) g.Issue(orders[oi++]);
            g.Step(); ticks++;
            var tv = g.TrafficNow(); zone += tv.Count(v => v.InZone);
            for (int a = 0; a < tv.Count; a++)
            {
                var ra = CarGeo.Rect(tv[a].At, tv[a].Heading, tv[a].Length / 2, tv[a].Width / 2);
                for (int b = a + 1; b < tv.Count; b++) if ((tv[a].Speed > 0 || tv[b].Speed > 0) && CarGeo.PolysMeet(ra, CarGeo.Rect(tv[b].At, tv[b].Heading, tv[b].Length / 2, tv[b].Width / 2))) { carCar++; if (carCar <= 5) lines.Add($"{Report.Clock(g.Tick)} {tv[a].Id} ({tv[a].At} h {tv[a].Heading} v {tv[a].Speed} {tv[a].Mode}: {g.TrafficWhy(tv[a].Id)}) meets {tv[b].Id} ({tv[b].At} h {tv[b].Heading} v {tv[b].Speed} {tv[b].Mode}: {g.TrafficWhy(tv[b].Id)})"); }
                foreach (var m in new[] { ("car:getaway", g.GetawayCarPos(), g.GetawayHeading()), ("car:official", g.CarAt("car:official") ?? default, g.CarAt("car:official").HasValue ? g.StandingCars().Find(s => s.Id == "car:official").Heading : default), ("car:rivals", g.RivalCarPose()?.pos ?? default, g.RivalCarPose()?.heading ?? default) })
                {
                    if (m.Item3.X == 0 && m.Item3.Y == 0) continue;
                    if (CarGeo.PolysMeet(ra, CarGeo.Rect(m.Item2, m.Item3, hl, hw))) { carMission++; if (carMission <= 5) lines.Add($"{Report.Clock(g.Tick)} {tv[a].Id} ({tv[a].State}/{tv[a].Mode}) at {tv[a].At} h {tv[a].Heading} meets {m.Item1} at {m.Item2} h {m.Item3}: {g.TrafficWhy(tv[a].Id)}"); }
                }
                if (tv[a].Speed > 0) foreach (var w in g.Walkers) if (g.CrowdPos(w) is Pt p && CarGeo.Inside(p, ra)) { overPerson++; if (overPerson <= 5) lines.Add($"{Report.Clock(g.Tick)} {tv[a].Id} moving over {w.P.Id}"); }
                if (tv[a].Speed == 0 && tv[a].State == "stopped") { if (!stoodSince.ContainsKey(tv[a].Id)) stoodSince[tv[a].Id] = g.Tick; int s = g.Tick - stoodSince[tv[a].Id]; if (s > maxStand) { maxStand = s; worstStand = $"{tv[a].Id} ({tv[a].Mode}) at {tv[a].At} since {Report.Clock(stoodSince[tv[a].Id])}"; } if (s == Game.T(30) && Array.IndexOf(args, "--why") > 0) { lines.Add($"  {Report.Clock(g.Tick)} {tv[a].Id} stood 30 s at {tv[a].At}: {g.TrafficWhy(tv[a].Id)}"); foreach (var id in new[] { "scout", "fixer", "tech", "enforcer", "waitress", "driver", "guard", "councillor", "rival-1", "rival-2", "rival-3", "rival-4", "rival-5", "patrol" }) if (g.PosOf(id) is Pt pp && G.Within(pp, tv[a].At, 900)) lines.Add($"      near: {id} at {pp}"); foreach (var w in g.Walkers) if (g.CrowdPos(w) is Pt wp && G.Within(wp, tv[a].At, 900)) lines.Add($"      near: {w.P.Id} ({w.P.Job}, {w.St}) at {wp}"); } }
                else stoodSince.Remove(tv[a].Id);
            }
            if (g.Tick == 1 || g.Tick == 100) foreach (var v in tv) lines.Add($"  {Report.Clock(g.Tick)} {v.Id} {v.Model} at {v.At} h {v.Heading} v {v.Speed} {v.State}/{v.Mode} zone {v.InZone}: {g.TrafficWhy(v.Id)}");
            if (g.Tick % every == 0) lines.Add($"{Report.Clock(g.Tick)} in zone {tv.Count(v => v.InZone)}, on runways {tv.Count(v => !v.InZone)}, moving {tv.Count(v => v.Speed > 0)}, standing {tv.Count(v => v.Standing)}, trips {g.TrafficTrips}");
        }
        g.Finish();
        foreach (var l in lines) Console.Error.WriteLine(l);
        var census = g.Census.Find(c => c.Kind == Population.Cars);
        Console.Error.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{System.IO.Path.GetFileName(script)} seed {sc.Start.Seed} cars {census?.Count}: {g.Result} {g.FailCode} at {Report.Clock(g.Tick)}; mean in zone {(double)zone / Math.Max(1, ticks):0.0}; trips {g.TrafficTrips}; moving cars meeting {carCar} car-ticks, meeting a mission car {carMission}, over a person {overPerson}; longest stood {maxStand / Game.Hz} s {worstStand}"));
        int ji = Array.IndexOf(args, "--json");
        if (ji > 0) File.WriteAllText(args[ji + 1], new JWriter().Obj().Prop("script", System.IO.Path.GetFileName(script)).Prop("seed", sc.Start.Seed).Prop("cars", census?.Count ?? 0).Prop("result", g.Result.ToString()).Prop("fail", g.FailCode)
            .Key("meanInZone").Raw(((double)zone / Math.Max(1, ticks)).ToString("0.00", CultureInfo.InvariantCulture)).Prop("trips", g.TrafficTrips).Prop("carCar", carCar).Prop("carMission", carMission).Prop("overPerson", overPerson).Prop("longestStoodS", maxStand / Game.Hz).EndObj().ToString() + "\n");
        return overPerson > 0 || carCar > 0 ? 1 : 0;
    }
}
