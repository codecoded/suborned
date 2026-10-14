// An order script (brief 011 B, Waymark's route scripts): a start state and timed orders in the engine's order format.
// {"schema": "suborned.sim.script/1", "mission": "handover", "start": {...}, "orders": [{"tick": 0, "op": "scout", "do": "move", "to": "d:Courtyard"}, ...], "until": 4800}
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Suborned.Sim
{
    public sealed class Script
    {
        public Start Start = new Start(); public readonly List<Order> Orders = new List<Order>(); public int Until;
        public static Script Parse(string json)
        {
            var n = JNode.Parse(json, "script");
            n.Only("schema", "mission", "note", "start", "orders", "until");
            if (n["schema"].Str() != "suborned.sim.script/1") throw new FormatException("script schema is not suborned.sim.script/1");
            if (n["mission"].Str() != "handover") throw new FormatException("only the Handover plays (brief 011)");
            var s = new Script { Start = Start.Parse(n.Opt("start")), Until = n.Opt("until")?.Int() ?? 4800 };
            foreach (var o in n["orders"].Items) s.Orders.Add(Order.Parse(o));
            return s;
        }
        public static string Write(Game g)
        {
            var w = new JWriter().Obj().Prop("schema", "suborned.sim.script/1").Prop("mission", "handover").Key("start").Raw(g.Start.Canonical()).OpenArr("orders");
            foreach (var (o, _, _) in g.Orders) o.Write(w);
            return w.EndArr().Prop("until", g.Tick).EndObj().ToString();
        }
    }

    public static class Report
    {
        public static string Clock(int tick) { int s = tick / Game.Hz; return $"{s / 60}:{(s % 60).ToString("00", CultureInfo.InvariantCulture)}.{tick % Game.Hz}"; }
        public static string Json(string id, Game g, List<(bool ok, string what)> checks, bool replayIdentical)
        {
            var w = new JWriter().Obj().Prop("id", id).Prop("outcome", g.Result.ToString().ToLowerInvariant()).Prop("fail", g.FailCode).Prop("tick", g.Tick)
                .Prop("cash", g.Cash).Prop("trail", g.Trail).Prop("alert", g.Alert).Prop("gunfire", g.Gunfire).Prop("standing", g.Standing)
                .Prop("startHash", Hash.Hex(g.StartHash)).Prop("finalHash", Hash.Hex(g.Checkpoints[g.Checkpoints.Count - 1].hash)).Prop("replayIdentical", replayIdentical);
            w.OpenArr("checkpoints"); foreach (var (t, h) in g.Checkpoints) w.Obj().Prop("tick", t).Prop("hash", Hash.Hex(h)).EndObj(); w.EndArr();
            w.OpenArr("checks"); foreach (var c in checks) w.Obj().Prop("ok", c.ok).Prop("what", c.what).EndObj(); w.EndArr();
            w.OpenArr("orders"); foreach (var (o, ok, why) in g.Orders) { w.Obj().Key("order").Raw(o.ToString()).Prop("ok", ok); if (!ok) w.Prop("why", why); w.EndObj(); } w.EndArr();
            w.OpenArr("log"); foreach (var l in g.Log) w.Obj().Prop("tick", l.Tick).Prop("code", l.Code).Prop("text", l.Text).EndObj(); w.EndArr();
            if (g.Result == Outcome.Success) { w.Key("debrief"); var d = new JWriter(); Debrief.Of(g).Write(d); w.Raw(d.ToString()); }
            return w.EndObj().ToString();
        }
    }
}
