// An order script (brief 011 B, Waymark's route scripts): a start state and timed orders in the engine's order format.
// {"schema": "suborned.sim.script/1", "mission": "handover", "start": {...}, "orders": [{"tick": 0, "op": "scout", "do": "move", "to": "d:Courtyard"}, ...], "until": 4800}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Suborned.Sim
{
    /// <summary>A run record (Redline E4): what a replay must match. inputs = the inputs' canonical hash; engine = Engine.Version (and
    /// the assembly MVID, for information); start = the start state's hash; checkpoints = every (tick, hash); outcome.</summary>
    public sealed class Expect
    {
        public string Inputs = "", Engine = "", Mvid = "", Start = "", Outcome = ""; public readonly List<(int tick, string hash)> Checkpoints = new List<(int, string)>();
    }

    public sealed class Script
    {
        public Start Start = new Start(); public readonly List<Order> Orders = new List<Order>(); public int Until; public Expect? Expect;
        public static Script Parse(string json)
        {
            var n = JNode.Parse(json, "script");
            n.Only("schema", "mission", "note", "start", "orders", "until", "expect");
            if (n["schema"].Str() != "suborned.sim.script/1") throw new FormatException("script schema is not suborned.sim.script/1");
            if (n["mission"].Str() != "handover") throw new FormatException("only the Handover plays (brief 011)");
            var s = new Script { Start = Start.Parse(n.Opt("start")), Until = n.Opt("until")?.Int() ?? 0 }; // 0: to the mission's close
            foreach (var o in n["orders"].Items) s.Orders.Add(Order.Parse(o));
            if (n.Opt("expect") is JNode e)
            {
                e.Only("inputs", "engine", "mvid", "start", "outcome", "checkpoints");
                var x = new Expect { Inputs = e["inputs"].Str(), Engine = e["engine"].Str(), Mvid = e.Opt("mvid")?.Str() ?? "", Start = e["start"].Str(), Outcome = e["outcome"].Str() };
                foreach (var c in e["checkpoints"].Items) { var pair = c.Items; if (pair.Count != 2) throw new FormatException($"{c.Path}: [tick, hash]"); x.Checkpoints.Add((pair[0].Int(), pair[1].Str())); }
                s.Expect = x;
            }
            return s;
        }
        /// <summary>The run as a script: its start state, its orders (rejected ones too: they are the player's), and its record.</summary>
        public static string Write(Game g, ulong inputsHash)
        {
            var w = new JWriter().Obj().Prop("schema", "suborned.sim.script/1").Prop("mission", "handover").Key("start").Raw(g.Start.Canonical()).OpenArr("orders");
            foreach (var (o, _, _) in g.Orders) o.Write(w);
            w.EndArr().Prop("until", g.Tick);
            w.Open("expect").Prop("inputs", Hash.Hex(inputsHash)).Prop("engine", Engine.Version).Prop("mvid", Engine.Mvid).Prop("start", Hash.Hex(g.StartHash)).Prop("outcome", g.Result.ToString().ToLowerInvariant());
            w.OpenArr("checkpoints"); foreach (var (t, h) in g.Checkpoints) w.Arr().Num(t).Str(Hash.Hex(h)).EndArr(); w.EndArr().EndObj();
            return w.EndObj().ToString();
        }
        /// <summary>Check a replay against the record: "" if it matches, else the first difference.</summary>
        public string Verify(Game g, ulong inputsHash)
        {
            if (Expect == null) return "";
            if (Expect.Inputs != Hash.Hex(inputsHash)) return $"the inputs differ from the record ({Expect.Inputs} recorded, {Hash.Hex(inputsHash)} now)";
            if (Expect.Engine != Engine.Version) return $"the engine differs from the record ({Expect.Engine} recorded, {Engine.Version} now)";
            if (Expect.Start != Hash.Hex(g.StartHash)) return "the start state differs from the record";
            int n = Math.Min(Expect.Checkpoints.Count, g.Checkpoints.Count);
            for (int i = 0; i < n; i++)
                if (Expect.Checkpoints[i].tick != g.Checkpoints[i].tick || Expect.Checkpoints[i].hash != Hash.Hex(g.Checkpoints[i].hash))
                    return $"checkpoint {i} differs at tick {g.Checkpoints[i].tick}";
            if (Expect.Checkpoints.Count != g.Checkpoints.Count) return $"{g.Checkpoints.Count} checkpoints, the record has {Expect.Checkpoints.Count}";
            if (Expect.Outcome != g.Result.ToString().ToLowerInvariant()) return "the outcome differs from the record";
            return "";
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
