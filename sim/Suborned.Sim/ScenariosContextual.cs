// The contextual act's engine side (brief 030, Sky 2026-10-20; rulebook weapons.close, weapons.autoHolster): a weapon the fight drew holsters
// after the fight, one drawn with F stays; a fire order out of reach walks in outside the lab; a driven agent is not walked, and the host
// previews the walk-in for one. Each also runs twice and replays from its orders (the Theory tests).
using System;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        static void ContextualCases()
        {
            Add("N176", "—", "Auto-holster (brief 030 §5; weapons.autoHolster 4 s): the Enforcer's carbine, drawn by his fire order on the guard (route fail-enforcer-down's orders), is holstered by the engine 4 s after the fight goes quiet (fight.public); in a twin where he drew it with a draw order (F) a tick before, it stays drawn.", r =>
            {
                var g = r.G;
                const string orders = "[{\"tick\":0,\"op\":\"enforcer\",\"do\":\"move\",\"to\":\"d:Office approach\"},{\"tick\":354,\"op\":\"enforcer\",\"do\":\"fire\",\"target\":\"guard\"}]";
                Script(r, orders);
                r.Is(r.Op(Enf).Gun.Drawn && r.Op(Enf).Gun.DrawnBy == "fire", $"drawn by the fire order ({r.Op(Enf).Gun.DrawnBy})");
                if (!r.Is(r.Until(() => g.Fights.Count > 0, T(120)), $"the fight goes quiet and ends ({g.Result} {g.FailCode} at {Report.Clock(g.Tick)})")) return;
                int end = g.Fights[0].End;
                if (!r.Is(r.Until(() => g.Log.Exists(l => l.Code == "holster.auto"), T(30)), $"the engine holsters the fight's weapon ({g.Result} at {Report.Clock(g.Tick)})")) return;
                var line = g.Log.First(l => l.Code == "holster.auto");
                r.Is(line.Tick >= end + T(g.R.AutoHolsterS) && line.Text.StartsWith("enforcer", StringComparison.Ordinal), $"the Enforcer's, {line.Tick - end} ticks after the fight ended (4 s, or later while a hostile has him in sight)");
                r.Wait(g.R.HolsterTicks + 1);
                r.Is(!r.Op(Enf).Gun.Drawn && r.Op(Enf).Gun.DrawnBy == "", "his carbine is holstered");
                // the twin: drawn with F a tick before the fire order (N125's orders)
                var t = Twin(g, new Start());
                foreach (var o in new[] { new Order { Tick = 0, Op = Enf, Do = "move", To = "d:Office approach" }, new Order { Tick = 353, Op = Enf, Do = "draw" }, new Order { Tick = 354, Op = Enf, Do = "fire", Target = GuardR } })
                { t.RunTo(o.Tick); t.Issue(o); }
                t.RunTo(Math.Max(t.Tick, end + T(g.R.AutoHolsterS) + T(5)));
                r.Is(!t.Log.Exists(l => l.Code == "holster.auto") && (t.OpOf(OpId.Enforcer).Down || (t.OpOf(OpId.Enforcer).Gun.Drawn && t.OpOf(OpId.Enforcer).Gun.DrawnBy == "key")), $"drawn with F, it stays drawn (by {t.OpOf(OpId.Enforcer).Gun.DrawnBy})");
            });

            Add("N177", "—", "A fire order out of reach walks in outside the lab (brief 030; weapons.close): the Scout's fire order on the guard from the car is taken and walks her into her pistol's effective ring, where she fires; driven (cell.drive), the engine does not walk her, and PreviewFireWalkIn shows the host the walk-in it would make.", r =>
            {
                var g = r.G; r.To(T(2));
                var twin = Twin(g, new Start()); twin.RunTo(T(2));
                var dv = twin.Issue(new Order { Tick = twin.Tick, Op = Scout, Do = "drive", Point = twin.OpOf(OpId.Scout).Pos });
                r.Is(dv.ok && twin.OpOf(OpId.Scout).Driven, $"the twin's Scout is driven ({dv.code})");
                var plain = twin.PreviewFire(OpId.Scout, GuardR); var walkIn = twin.PreviewFireWalkIn(OpId.Scout, GuardR);
                r.Is(!plain.Walk && walkIn.Refused == "" && walkIn.Walk && walkIn.WalkS > 0, $"driven: no engine walk, and the walk-in preview ({walkIn.WalkS} s to {walkIn.WalkTo})");
                var tf = twin.Issue(new Order { Tick = twin.Tick, Op = Scout, Do = "fire", Target = GuardR });
                r.Is(tf.ok && !twin.OpOf(OpId.Scout).CloseWalk, "driven, the fire order is taken and holds: the engine does not walk her");
                var pv = g.PreviewFire(OpId.Scout, GuardR);
                r.Is(pv.Refused == "" && pv.Walk && pv.WalkS > 0, $"the chip shows the walk into range ({pv.WalkS} s)");
                r.Fire(Scout, GuardR);
                r.Is(r.Op(Scout).CloseWalk && r.Op(Scout).Walk != null, "the Scout walks into range");
                r.Is(r.Until(() => r.Shots().Exists(s => s.by == "scout"), T(90)), "and fires");
                r.Is(!g.Log.Exists(l => l.Code == "fire.lapses"), "the order never lapses");
            });
        }
    }
}
