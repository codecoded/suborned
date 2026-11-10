// Brief 105 round 2 (engine 0.53.0; Motive briefs/105/motive/round2.md §1, car.take; Sky 2026-11-10: "if everyone is out, and there's other
// cars still around, then they can take those ones e.g. the rivals car").
//    K3-TAKE   as K3-PRESS until the getaway car is shot out; then the rival car: with its wheelman at the wheel only the Enforcer may take it
//              (car-wheelman for the Fixer), knocking him out at the door with Vale in hand; with the wheel free the Fixer takes it and the
//              Enforcer boards with Vale; its Go is the win (vale.out)
using System;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        static void Car105()
        {
            Add("K3-TAKE", "105", "Engine 0.53.0 (car.take): as K3-PRESS until the getaway car is shot out and everyone is out with Vale; then the rival car. With its wheelman at the wheel, only the Enforcer may take it (car-wheelman for the Fixer): he knocks the wheelman out at the door with Vale in hand and is at the wheel with him. With nobody at its wheel, the Fixer takes it (car.taken) and the Enforcer boards with Vale. Its Go is the win, fixed (vale.out), refused only while boxed or held.", r =>
            {
                var g = r.G;
                if (!ShotOut(r)) return;
                var rc = g.CarAt("car:rivals");
                if (!r.Is(rc.HasValue, "the rival car stands on the street")) return;
                var spot = g.Walk.Nearest(rc!.Value, q => g.CarBodyCm("car:rivals", q) is int d && d >= 0 && d <= g.R.CarReachCm, 800);
                if (!r.Is(spot.HasValue, "a spot at its door")) return;
                var fx = g.OpOf(OpId.Fixer); var e = g.OpOf(OpId.Enforcer);
                bool At(Pt q) => g.CarBodyCm("car:rivals", q) is int d && d >= 0 && d <= g.R.CarReachCm;
                var spot2 = g.Walk.Nearest(rc.Value, q => At(q) && G.D2(q, spot!.Value) >= 80L * 80, 800) ?? spot!.Value;
                r.To(Math.Max(r.SendTo(Fixer, spot!.Value), r.SendTo(Enf, spot2)));
                r.Until(() => g.Over || (fx.Walk == null && e.Walk == null), T(5));
                for (int k = 0; k < 3 && !g.Over && !At(fx.Pos); k++) { var q = g.Walk.Nearest(fx.Pos, At, 800); if (q.HasValue) r.To(r.SendTo(Fixer, q.Value)); } // the body spacing may stop him short: on again, as click-to-act would
                r.Line(At(fx.Pos) && At(e.Pos), $"both at its door ({g.CarBodyCm("car:rivals", fx.Pos)} cm, {g.CarBodyCm("car:rivals", e.Pos)} cm)");
                if (!r.Is(!g.Over && !fx.Down && !e.Down, $"the Fixer and the Enforcer at the rival car's door ({g.Result} {g.FailCode})")) return;
                string Code(string op) { var a = g.Issue(new Order { Tick = g.Tick, Op = op, Do = "take-car", Target = "car:rivals" }); return a.ok ? "" : a.code ?? ""; }
                var c = Code(Fixer); OpId taker = OpId.Fixer;
                if (c == "car-wheelman")
                {
                    r.Is(g.Log.Exists(l => l.Code == "order.rejected" && l.Text.Contains("the Enforcer takes him out")), "the wheelman is the Enforcer's: car-wheelman for the Fixer");
                    var ce = Code(Enf); if (!r.Is(ce == "", $"the Enforcer takes the wheelman out at the door, Vale in hand ({ce})")) return;
                    taker = OpId.Enforcer;
                    r.Until(() => e.InLent || g.Over || e.Down, T(g.R.BoardS + g.R.KoS + g.R.CarryS + 1));
                    if (e.Down) // ten seconds at the door under the crew's fire: the downed-Enforcer law (its own brief, round2 §2) may end it first
                    {
                        r.Line(false, $"the Enforcer fell at the door before the take landed ({g.FailCode}/{g.Cause?.Why})");
                        r.Is(g.FailCode != "getaway-lost" && g.Cause?.Why != "reached-car", "never a take from a car, never getaway-lost");
                        return;
                    }
                    r.Is(g.Log.Exists(l => l.Code == "take-car.wheelman"), "the wheelman is knocked out at the door");
                }
                else if (!r.Is(c == "", $"nobody at the wheel: the Fixer takes the rival car ({c})")) return;
                else r.Until(() => fx.InLent || g.Over, T(g.R.BoardS + 1));
                r.Is(g.Lent && g.LentId == "car:rivals" && g.OpOf(taker).InLent, "the rival car is ours (car.taken)");
                if (taker == OpId.Fixer)
                {
                    var b = g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "board", Target = "car:rivals" });
                    if (!r.Is(b.ok, $"the Enforcer boards it with Vale ({b.code})")) return;
                    r.Until(() => g.Vale == ValeAt.Lent || g.Over, T(g.R.BoardS + g.R.CarryS + 1));
                }
                r.Is(g.Vale == ValeAt.Lent, $"Vale in the back of the rival car ({g.Vale})");
                var go = g.Issue(new Order { Tick = g.Tick, Op = "cell", Do = "go", Target = "car:rivals" });
                r.Line(go.ok, $"its Go ({go.code}): refused only boxed or held");
                if (go.ok) r.Is(g.ValeOutTick == g.Tick && g.CarAt("car:rivals") == null, "vale.out: the win is fixed and the car is gone");
            }, new Start { WeaponsFree = false, OneInput = false });
        }

        /// <summary>K3-PRESS's opening, to the getaway car shot out with everyone out (car.shot-out).</summary>
        static bool ShotOut(Run r)
        {
            var g = r.G;
            if (!RouteAAboard(r)) return false;
            foreach (var op in new[] { Scout, Fixer, Tech }) { var o = r.Op(op); if (!o.Down && !o.InCar) { g.Issue(new Order { Tick = g.Tick, Op = op, Do = "cease" }); r.SendTo(op, g.GetawaySeat); } }
            int until = g.Tick + T(90);
            while (!g.Over && g.Tick < until && !g.Log.Exists(l => l.Code == "car.shot-out"))
            {
                foreach (var op in new[] { Scout, Fixer, Tech }) { var o = r.Op(op); if (!o.Down && !o.InCar && o.Busy == null && o.Walk == null && g.AtGetaway(o.Pos)) g.Issue(new Order { Tick = g.Tick, Op = op, Do = "board", Target = "car" }); }
                g.Step();
            }
            return r.Is(g.Log.Exists(l => l.Code == "car.shot-out") && !g.Over && g.Vale == ValeAt.WithEnforcer, $"the getaway car shot out, everyone out with Vale ({g.Result} {g.FailCode} at {Report.Clock(g.Tick)})");
        }
    }
}
