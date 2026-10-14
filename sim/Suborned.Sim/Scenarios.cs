// The 28 scenarios of the top-down test (~/Projects/game-source/docs/prototypes/suborned-world-sim.test.js, sha256 c41a8d78…),
// rebased on the real civic block, the rulebook and the r013 cast, plus Suture's N-cases the engine owns. Each keeps its intent;
// its positions and times are the real block's (sim/data/handover.site.json), and every change from the prototype is said beside it.
// They live in the engine DLL so the xunit tests, the CLI and Unity (brief 011 D) run the very same code.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed class Scenario
    {
        public string Id = "", Intent = "", Proto = ""; public Start Start = new Start(); public Action<Run> Body = _ => { };
    }

    /// <summary>A scenario being played: the game, and the checks it made.</summary>
    public sealed class Run
    {
        public readonly Game G; public readonly List<(bool ok, string what)> Checks = new List<(bool, string)>();
        public Run(Game g) { G = g; }
        public bool Is(bool ok, string what) { Checks.Add((ok, what)); return ok; }
        public void Eq<T>(T a, T b, string what) => Is(EqualityComparer<T>.Default.Equals(a, b), what + " (got " + Inv(a) + ", want " + Inv(b) + ")");
        static string Inv(object? v) => v is IFormattable f ? f.ToString(null, System.Globalization.CultureInfo.InvariantCulture) : v?.ToString() ?? "";
        /// <summary>Give an order now and check it was taken.</summary>
        public bool Do(string op, string verb, string target = "", string approach = "")
        {
            var (ok, why) = G.Issue(new Order { Tick = G.Tick, Op = op, Do = verb, Target = target, Approach = approach });
            return Is(ok, $"{op} {verb} {target} {approach} taken at {Report.Clock(G.Tick)}{(ok ? "" : ": " + why)}".Replace("  ", " "));
        }
        /// <summary>Give an order now and check it was refused.</summary>
        public bool Refused(string op, string verb, string target, string what, string approach = "")
        {
            var (ok, _) = G.Issue(new Order { Tick = G.Tick, Op = op, Do = verb, Target = target, Approach = approach });
            return Is(!ok, what);
        }
        public bool Try(string op, string verb, string target = "", string approach = "") => G.Issue(new Order { Tick = G.Tick, Op = op, Do = verb, Target = target, Approach = approach }).ok;
        /// <summary>Send an operative to a place; returns the tick they arrive.</summary>
        public int Send(string op, string place)
        {
            var (ok, why) = G.Issue(new Order { Tick = G.Tick, Op = op, Do = "move", To = place });
            Is(ok, $"{op} can walk to {place}{(ok ? "" : ": " + why)}");
            return Arrival(op);
        }
        public int SendTo(string op, Pt at)
        {
            var (ok, why) = G.Issue(new Order { Tick = G.Tick, Op = op, Do = "move", Point = at });
            Is(ok, $"{op} can walk to {at}{(ok ? "" : ": " + why)}");
            return Arrival(op);
        }
        public int Arrival(string op)
        {
            var o = G.OpOf(Sim.Op.Parse(op)); if (o.Walk == null) return G.Tick;
            int speed = G.R.CellSpeed; long left = o.Walk.Length - o.WalkMm;
            return G.Tick + (int)((left + speed - 1) / speed);
        }
        public void To(int tick) => G.RunTo(tick);
        public void Wait(int ticks) => G.Advance(ticks);
        /// <summary>Run until the operative's action in progress lands.</summary>
        public void Done(string op) { var o = G.OpOf(Sim.Op.Parse(op)); if (o.Busy != null) G.RunTo(o.BusyEnd); }
        /// <summary>The Enforcer fights it out as a player would: fire when the shot is ready and a hostile is in reach, else close on the
        /// nearest one still up; then to the car. Orders only, so the run replays from its log. Returns when the car is free or the run ends.</summary>
        public void FightItOut(int limitS = 120)
        {
            int end = G.Tick + Game.T(limitS), reach = G.R.FireReachCm;
            while (!G.Over && G.Tick < end && G.CarHeld())
            {
                var e = Op("enforcer");
                Hostile? near = null; long bd = long.MaxValue;
                foreach (var h in G.Hostiles) { if (h.Down || !h.Present) continue; long d = G.D2(e.Pos, h.Pos(G.Tick)); if (d < bd) { bd = d; near = h; } }
                if (near == null) { if (e.Walk == null) SendTo("enforcer", G.S.Stand(G.S.GetawayCar)); G.Step(); continue; }
                if (G.Tick >= G.EnforcerNextShot && e.Busy == null && G.Issue(new Order { Tick = G.Tick, Op = "enforcer", Do = "fire", Target = "hostiles" }).ok) { G.Step(); continue; }
                if (bd > (long)(reach - 200) * (reach - 200) && (e.Walk == null || e.Walk.End != G.S.Grid.NearestFree(near.Pos(G.Tick))))
                {
                    var to = G.S.Grid.NearestFree(near.Pos(G.Tick));
                    if (to.HasValue) G.Issue(new Order { Tick = G.Tick, Op = "enforcer", Do = "move", Point = to.Value });
                }
                G.Step();
            }
            if (G.Over) return;
            var car = G.S.At(G.S.GetawayCar);
            if (!G.Within(Op("enforcer").Pos, car, G.R.ActReachCm)) To(SendTo("enforcer", G.S.Stand(G.S.GetawayCar)));
        }
        public Pt At(string place) => G.S.At(place);
        public Pt Stand(string place) => G.S.Stand(place);
        public Op Op(string op) => G.OpOf(Sim.Op.Parse(op));
    }

    public static partial class Scenarios
    {
        static List<Scenario>? all;
        public static List<Scenario> All { get { if (all == null) { all = new List<Scenario>(); Handover(); } return all; } }
        public static Scenario? Find(string id) { foreach (var s in All) if (string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase)) return s; return null; }
        public static Run Run(Engine.Inputs i, Scenario s)
        {
            var r = new Run(Engine.New(i, s.Start));
            s.Body(r);
            r.G.Finish();
            return r;
        }
        static Scenario Add(string id, string proto, string intent, Action<Run> body, Start? start = null)
        {
            var s = new Scenario { Id = id, Proto = proto, Intent = intent, Body = body, Start = start ?? new Start() };
            // The 28 are the top-down test's: it had no final fight and no crowd. N10, N11 and N12 test those; the 28 keep their own world.
            if (id.StartsWith("S", StringComparison.Ordinal)) { s.Start.FinalFight = false; s.Start.Crowd = false; }
            all!.Add(s); return s;
        }
        static int T(int s) => Game.T(s);
    }
}
