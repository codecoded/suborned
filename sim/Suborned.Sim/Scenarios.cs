// The 28 scenarios of the top-down test (~/Projects/game-source/docs/prototypes/suborned-world-sim.test.js, sha256 c41a8d78…),
// rebased on the real civic block, the rulebook and the promoted cast (r016 since 2026-10-16), plus Suture's N-cases the engine owns. Each keeps its intent;
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
        static string Inv(object? v)
        {
            if (v is System.Runtime.CompilerServices.ITuple t) // a tuple's own ToString is the culture's (a negative item reads "−1" in sv-SE: Redline E7)
            {
                var parts = new string[t.Length]; for (int i = 0; i < t.Length; i++) parts[i] = Inv(t[i]);
                return "(" + string.Join(", ", parts) + ")";
            }
            return v is IFormattable f ? f.ToString(null, System.Globalization.CultureInfo.InvariantCulture) : v?.ToString() ?? "";
        }
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
        public int Send(string op, string place, bool run = false)
        {
            var (ok, why) = G.Issue(new Order { Tick = G.Tick, Op = op, Do = "move", To = place, Run = run });
            Is(ok, $"{op} can {(run ? "run" : "walk")} to {place}{(ok ? "" : ": " + why)}");
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
            int speed = o.WalkSpeed > 0 ? o.WalkSpeed : G.R.CellSpeed; long left = o.Walk.Length - o.WalkMm;
            return G.Tick + (int)((left + speed - 1) / speed);
        }
        public void To(int tick) => G.RunTo(tick);
        public void Wait(int ticks) => G.Advance(ticks);
        /// <summary>Run until the operative's action in progress lands.</summary>
        public void Done(string op) { var o = G.OpOf(Sim.Op.Parse(op)); if (o.Busy != null) G.RunTo(o.BusyEnd); }
        /// <summary>Give a fire order now (until the target is down); force for a civilian or a contact (the Ctrl case).</summary>
        public bool Fire(string op, string target, bool force = false)
        {
            var (ok, why) = G.Issue(new Order { Tick = G.Tick, Op = op, Do = "fire", Target = target, Force = force });
            return Is(ok, $"{op} fires on {target} at {Report.Clock(G.Tick)}{(ok ? "" : ": " + why)}");
        }
        /// <summary>Step until the condition holds, the run ends, or the limit passes; returns whether it held.</summary>
        public bool Until(Func<bool> cond, int limitTicks = 1200)
        {
            int end = G.Tick + limitTicks;
            while (!cond() && !G.Over && G.Tick < end) G.Step();
            return cond();
        }
        /// <summary>The shots in the log, in order: (tick, shooter, target, hit).</summary>
        public List<(int tick, string by, string target, bool hit)> Shots()
        {
            var l = new List<(int, string, string, bool)>();
            foreach (var x in G.Log)
            {
                if (x.Code != "shot.hit" && x.Code != "shot.miss") continue;
                var by = x.Text.Substring(0, x.Text.IndexOf(" fires on ", StringComparison.Ordinal));
                var rest = x.Text.Substring(by.Length + 10); var t = rest.Substring(0, rest.IndexOf(" at ", StringComparison.Ordinal));
                l.Add((x.Tick, by, t, x.Code == "shot.hit"));
            }
            return l;
        }
        /// <summary>The cell fights as a player would (Sky 2026-10-15 fights): each armed operative named fires on the nearest target up
        /// with a line in its weapon's reach, walks into its effective reach and stops there to shoot (walking halves the aim). Orders only,
        /// so the run replays from its log. Runs until `done`, the run ends, or the limit passes.</summary>
        public void Engage(Func<List<string>> targets, Func<bool> done, int limitS, string[] ops, bool carToEnforcer, bool advance = true)
        {
            int end = G.Tick + Game.T(limitS);
            while (!done() && !G.Over && G.Tick < end)
            {
                var ts = targets();
                foreach (var name in ops)
                {
                    var e = Op(name); var w = e.Gun.W;
                    if (e.Down || e.Inside || e.Busy != null || w == null) continue;
                    string near = ""; Pt np = default; long bd = long.MaxValue;
                    foreach (var id in ts) { var at = G.PosOf(id); if (!at.HasValue || G.GunOf(id)!.Down) continue; long d = G.D2(e.Pos, at.Value); if (d < bd) { bd = d; near = id; np = at.Value; } }
                    if (near.Length == 0) { if (carToEnforcer && name == "enforcer" && e.Walk == null) G.Issue(new Order { Tick = G.Tick, Op = name, Do = "move", Point = G.S.Stand(G.S.GetawayCar) }); continue; }
                    bool line = G.S.Sight(e.Pos, np, G.R.Eye);
                    bool inEff = line && bd <= (long)(w.EffCm - 200) * (w.EffCm - 200);
                    bool firedOn = G.GunOf(near)!.Target == name;
                    if (e.Gun.Order.Length == 0 && line && bd <= (long)w.MaxCm * w.MaxCm && (inEff || (advance && e.Walk == null) || firedOn)) G.Issue(new Order { Tick = G.Tick, Op = name, Do = "fire", Target = near });
                    if (!advance) continue; // holding a post
                    if (!inEff)
                    {
                        var to = G.S.Grid.NearestFree(np);
                        if (to.HasValue && (e.Walk == null || !G.Within(e.Walk.End, to.Value, 200))) G.Issue(new Order { Tick = G.Tick, Op = name, Do = "move", Point = to.Value }); // re-aim the walk only when the target has moved 2 m
                    }
                    else if (e.Walk != null) G.Issue(new Order { Tick = G.Tick, Op = name, Do = "move", Point = e.Pos }); // in reach: stand and shoot
                }
                G.Step();
            }
        }
        /// <summary>The city's gunmen up on the street: the response unit's officers, a chasing patrol, drawn rivals.</summary>
        public List<string> CityGunmen()
        {
            var ids = new List<string>(); foreach (var s in G.SecGuns) ids.Add(s.Id);
            if (G.PatrolChasing && !G.PatrolDown) ids.Add("patrol");
            if (G.RivalsDrawn && G.RivalsPresent) ids.Add("rivals");
            return ids;
        }
        /// <summary>Hold posts and fire on the city's gunmen once they come into effective reach (or fire on the operative).</summary>
        public void FightOff(Func<bool> done, int limitS, params string[] ops) => Engage(CityGunmen, done, limitS, ops, false, false);
        /// <summary>The final fight: fight the hostiles (and the city's gunmen) until the car is free, walk the Enforcer to it, and fight
        /// again if a hostile comes back within hold of the car on the way.</summary>
        public void FightItOut(int limitS = 120, params string[] with)
        {
            var ops = new List<string> { "enforcer" }; ops.AddRange(with);
            int end = G.Tick + Game.T(limitS); var car = G.S.At(G.S.GetawayCar);
            while (!G.Over && G.Tick < end)
            {
                Engage(() => { var l = new List<string>(); foreach (var h in G.Hostiles) if (h.Present && !h.Down) l.Add(h.Id); l.AddRange(CityGunmen()); return l; },
                       () => !G.CarHeld(), Math.Max(1, (end - G.Tick) / Game.Hz), ops.ToArray(), true);
                if (G.Over || G.Tick >= end) return;
                if (!G.Within(Op("enforcer").Pos, car, G.R.AtReachCm))
                {
                    int at = SendTo("enforcer", G.S.Stand(G.S.GetawayCar));
                    while (!G.Over && G.Tick < at && !G.CarHeld()) G.Step();
                }
                if (!G.CarHeld() && G.Within(Op("enforcer").Pos, car, G.R.AtReachCm)) return;
            }
        }
        /// <summary>A takedown order now (brief 013): knockout or kill, on a person's role or id; checks it was taken.</summary>
        public bool Takedown(string target, string how)
        {
            var a = G.Issue(new Order { Tick = G.Tick, Op = "enforcer", Do = "takedown", Target = target, How = how });
            return Is(a.ok, $"enforcer takedown ({how}) {target} taken at {Report.Clock(G.Tick)}{(a.ok ? "" : ": " + a.why)}");
        }
        /// <summary>A body order now (carry, drop, stash, restrain) by an operative; checks it was taken.</summary>
        public bool Body(string op, string verb, string target = "")
        {
            var a = G.Issue(new Order { Tick = G.Tick, Op = op, Do = verb, Target = target });
            return Is(a.ok, $"{op} {verb} {target} taken at {Report.Clock(G.Tick)}{(a.ok ? "" : ": " + a.why)}".Replace("  ", " "));
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
            // Brief 013: they also keep the top-down test's response, security level 1 (routine: today's dispatch and seal, no robots at Alert 2);
            // the mission's own level (the civic district's 2) is the N cases' and the route scripts'.
            if (id.StartsWith("S", StringComparison.Ordinal)) { s.Start.FinalFight = false; s.Start.Crowd = false; s.Start.Security ??= 1; }
            all!.Add(s); return s;
        }
        static int T(int s) => Game.T(s);
    }
}
