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
        /// <summary>Brief 029: a fight range scenario's layout (open, dense; Engine.LoadRange); "" for the Handover's.</summary>
        public string Layout = "";
    }

    /// <summary>A scenario being played: the game, and the checks it made.</summary>
    public sealed class Run
    {
        public readonly Game G; public readonly List<(bool ok, string what)> Checks = new List<(bool, string)>();
        /// <summary>The inputs the run was made with (Scenarios.Run sets it; null for a run made by hand).</summary>
        public Engine.Inputs? Inputs;
        public Run(Game g) { G = g; }
        /// <summary>Brief 027 (the fight-lab matrix only): orders a scripted player gives each tick of a fight, before the step. Null in every scenario.</summary>
        public Action? PerTick;
        /// <summary>Tuning.md §7.11.3 (b) (the prepared player, lab): fire first on whoever fires on the Vale holder, then the blocker, then the nearest; off, the nearest.</summary>
        public bool Focus;
        /// <summary>Brief 027 B5 (the fight-lab matrix with coverSnap on): the perfect player uses the cover marks. Standing with a target in its
        /// weapon's max ring and no point held, an agent clicks 1 m short of the nearest free mark (Game.CoverMarks) that shields against that
        /// target and keeps it in reach with a line, and the snap lands it; holding a point with a target in reach, it holds instead of advancing.
        /// False in every scenario and for every other preset, so their runs are as before.</summary>
        public bool CoverPlayer;
        readonly Dictionary<string, int> coverSeekAt = new Dictionary<string, int>(StringComparer.Ordinal);
        const int SupportCm = 1500;
        /// <summary>The cover player's step for one agent: true when it gave a move (or holds its point), so Engage leaves it there this tick.</summary>
        bool CoverStep(string name, Op e, Weapon w, Pt np, long bd, bool line, List<string> ts)
        {
            if (!CoverPlayer || !G.CoverSnapOn) return false;
            if (e.Walk != null) return e.Gun.Cover.Length > 0; // on its way to a point it snapped to: let it get there
            if (bd > (long)w.MaxCm * w.MaxCm) return false;
            if (e.Gun.Cover.Length > 0) return line; // holding a point with the target in reach and a line: hold, fire from cover
            if (coverSeekAt.TryGetValue(name, out var at) && G.Tick < at) return false;
            coverSeekAt[name] = G.Tick + Game.T(3);
            int sec = G.Tick / Game.Hz; CoverMark? best = null; int bestN = 0;
            bool valeOut = G.Vale == ValeAt.WithEnforcer || G.Vale == ValeAt.Car; var en = G.OpOf(OpId.Enforcer); var holder = G.Vale == ValeAt.Car || en.Inside ? G.S.At(G.S.GetawayCar) : en.Pos;
            var foes = new List<Pt>(); foreach (var id in ts) { var p = G.PosOf(id); if (p.HasValue && G.GunOf(id) is Gun fg && !fg.Down) foes.Add(p.Value); }
            foreach (var m in G.CoverMarks(e.Id))
            {
                if (m.HeldBy.Length > 0) continue;
                var cp = G.S.Cover.Find(x => x.Id == m.Id)!;
                if (!CoverRules.Protects(cp, np, sec, G.R.PointBlankCm) || !G.Within(m.At, np, w.MaxCm) || !G.S.Sight(m.At, np, G.R.Eye)) continue;
                if (valeOut && !G.Within(m.At, holder, SupportCm)) continue; // Motive tuning.md §7.5: once Vale is out, a mark within 15 m of whoever holds him (support, not a cover hunt)
                int n = 0; foreach (var f in foes) if (CoverRules.Protects(cp, f, sec, G.R.PointBlankCm)) n++; // the most of the foes it shields against, then the nearest
                if (best == null || n > bestN || (n == bestN && m.DistanceCm < best.DistanceCm)) { best = m; bestN = n; }
            }
            if (best == null) return false;
            long d = Math.Max(1, best.DistanceCm); Pt click = best.At;
            if (d > 100) { var q = new Pt(best.At.X + (int)((e.Pos.X - best.At.X) * 100 / d), best.At.Y + (int)((e.Pos.Y - best.At.Y) * 100 / d)); if (G.S.Grid.Free(q) && G.PreviewSnap(e.Id, q)?.Id == best.Id) click = q; }
            return G.Issue(new Order { Tick = G.Tick, Op = name, Do = "move", Point = click }).ok;
        }
        public bool Is(bool ok, string what) { Checks.Add((ok, what)); return ok; }
        /// <summary>A balance line a design note predicts (tuning.md §8.7): recorded with its verdict, never a failed check, so the engine's
        /// numbers go back to the line's author (Motive) instead of being tuned to it. The suite's Lines() lists them.</summary>
        public bool Line(bool ok, string what) { Checks.Add((true, (ok ? "line met: " : "LINE MISSED: ") + what)); Lines.Add((ok, what)); return ok; }
        public readonly List<(bool ok, string what)> Lines = new List<(bool, string)>();
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
        /// <summary>Brief 022: a player who means to speak to (or watch) a server who works her round waits until she is back at her post (the
        /// HUD's "back in n s", Game.RoundNext); a held server is already standing still. Off-round, or for anyone else, nothing waits.</summary>
        public void ForHer(string verb, string target)
        {
            if (!G.RoundsOn || (verb != "listen" && verb != "watch" && verb != "turn") || target.Length == 0) return;
            Person? p = null; foreach (var x in G.People) if (x.Role == target || x.Id == target) p = x;
            if (p == null || p.Down) return;
            var pose = G.PersonPose(p); if (pose.HasValue && pose.Value.phase == "held") return;
            int n = G.RoundNext(p); if (n > 0) G.RunTo(G.Tick + n);
        }
        public bool Do(string op, string verb, string target = "", string approach = "")
        {
            ForHer(verb, target);
            var (ok, why) = G.Issue(new Order { Tick = G.Tick, Op = op, Do = verb, Target = target, Approach = approach });
            return Is(ok, $"{op} {verb} {target} {approach} taken at {Report.Clock(G.Tick)}{(ok ? "" : ": " + why)}".Replace("  ", " "));
        }
        /// <summary>Give an order now and check it was refused.</summary>
        public bool Refused(string op, string verb, string target, string what, string approach = "")
        {
            var (ok, _) = G.Issue(new Order { Tick = G.Tick, Op = op, Do = verb, Target = target, Approach = approach });
            return Is(!ok, what);
        }
        public bool Try(string op, string verb, string target = "", string approach = "") { ForHer(verb, target); return TryNow(op, verb, target, approach); }
        bool TryNow(string op, string verb, string target, string approach) => G.Issue(new Order { Tick = G.Tick, Op = op, Do = verb, Target = target, Approach = approach }).ok;
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
        public void Done(string op) { var o = G.OpOf(Sim.Op.Parse(op)); while (o.Act != null && !G.Over) G.Step(); if (o.Busy != null) G.RunTo(o.BusyEnd); } // engine 0.23.0: a walk in first
        /// <summary>Give a fire order now (until the target is down); force for a civilian or a contact (the host's hold sends it).</summary>
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
                    if (e.Down || e.Inside || e.InCar || e.Busy != null || w == null) continue; // in the car: no fire, and a move would step out (brief 018)
                    string near = ""; Pt np = default; long bd = long.MaxValue;
                    int rank = 9;
                    foreach (var id in ts)
                    {
                        var at = G.PosOf(id); if (!at.HasValue || G.GunOf(id)!.Down) continue; long d = G.D2(e.Pos, at.Value);
                        if (G.FightOverTick != int.MaxValue / 4 && G.Hostiles.Exists(h => h.Id == id)) continue; // tuning.md §9.1: after fight.over a hostile is an ordinary person (traced); the scripted player leaves them
                        int k = 2;
                        if (Focus) { var hg = G.GunOf(id)!; bool onHolder = G.Vale == ValeAt.WithEnforcer && hg.Target == "enforcer" && hg.Fired; bool blocker = G.Hostiles.Exists(h => h.Id == id && h.Blocker); k = onHolder ? 0 : blocker ? 1 : 2; } // §7.11.3 (b)
                        if (k < rank || (k == rank && d < bd)) { rank = k; bd = d; near = id; np = at.Value; }
                    }
                    if (near.Length == 0) { if (carToEnforcer && name == "enforcer" && e.Walk == null) G.Issue(new Order { Tick = G.Tick, Op = name, Do = "move", Point = G.S.Stand(G.S.GetawayCar) }); continue; }
                    if (name == "enforcer" && G.Vale == ValeAt.WithEnforcer) { if (e.Walk != null) G.Issue(new Order { Tick = G.Tick, Op = name, Do = "move", Point = e.Pos }); continue; } // brief 018: walking Vale he doesn't fire; he holds back while the others fight
                    bool line = G.S.Sight(e.Pos, np, G.R.Eye);
                    bool inEff = line && bd <= (long)(w.EffCm - 200) * (w.EffCm - 200);
                    bool firedOn = G.GunOf(near)!.Target == name;
                    if (e.Gun.Order.Length == 0 && line && bd <= (long)w.MaxCm * w.MaxCm && (inEff || (advance && e.Walk == null) || firedOn)) G.Issue(new Order { Tick = G.Tick, Op = name, Do = "fire", Target = near });
                    // tuning.md §7.11.3 (a), the ping-pong: a fire order's walk into range (fire.close; today's weapons.close) wins until its target is
                    // down or out of reach, so neither the cover seek nor the move toward the target re-orders an agent walking in to fire
                    if (e.CloseWalk && e.Gun.Order.Length > 0) continue;
                    if (CoverStep(name, e, w, np, bd, line, ts)) continue; // brief 027 B5: the matrix's cover player only
                    if (!advance) continue; // holding a post
                    if (!inEff)
                    {
                        var to = G.S.Grid.NearestFree(np);
                        // §7.11.3 (a): a move the cover snap moved counts as arrived at the snapped point: re-aim only when the target has moved 2 m
                        // from where the move would end now (the snap of the same click)
                        Pt? ends = to.HasValue ? (G.PreviewSnap(e.Id, to.Value)?.At ?? to.Value) : (Pt?)null;
                        if (to.HasValue && (e.Walk == null || !G.Within(e.Walk.End, ends!.Value, 200))) G.Issue(new Order { Tick = G.Tick, Op = name, Do = "move", Point = to.Value }); // re-aim the walk only when the target has moved 2 m
                    }
                    else if (e.Walk != null) G.Issue(new Order { Tick = G.Tick, Op = name, Do = "move", Point = e.Pos }); // in reach: stand and shoot
                }
                PerTick?.Invoke(); // brief 027 (the fight-lab matrix): a player's extra orders each tick (the field); null in every scenario
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
        public void FightItOut(int limitS = 120, params string[] with) => FightItOut(false, limitS, with);
        /// <summary>Fight it out (brief 018 §5): until the fight is over (the crew broken or every hostile down), not only until the car is free.</summary>
        public void FightItOutAll(int limitS = 120, params string[] with) => FightItOut(true, limitS, with);
        void FightItOut(bool all, int limitS, string[] with)
        {
            Func<bool> free = all ? () => !G.CarHeld() && G.FightOverTick != int.MaxValue / 4 : () => !G.CarHeld();
            var ops = new List<string> { "enforcer" }; ops.AddRange(with);
            int end = G.Tick + Game.T(limitS); var car = G.S.At(G.S.GetawayCar);
            while (!G.Over && G.Tick < end)
            {
                Engage(() => { var l = new List<string>(); foreach (var h in G.Hostiles) if (h.Present && !h.Down) l.Add(h.Id); l.AddRange(CityGunmen()); return l; },
                       free, Math.Max(1, (end - G.Tick) / Game.Hz), ops.ToArray(), true);
                if (G.Over || G.Tick >= end) return;
                // the fight is over: the engine ends the fire orders still open on the broken crew (rulebook fight.over, Motive 2026-10-19),
                // so the helper calls nothing off (the cease it gave here before 0.8.2 is gone; N95 holds without it)
                if (!G.Within(Op("enforcer").Pos, car, G.R.AtReachCm))
                {
                    int at = SendTo("enforcer", G.S.Stand(G.S.GetawayCar));
                    while (!G.Over && G.Tick < at && free()) G.Step();
                }
                if (free() && G.Within(Op("enforcer").Pos, car, G.R.AtReachCm)) return;
            }
        }
        /// <summary>The getaway (brief 018): each named operative walks to the car if it isn't at it and boards (the Enforcer first, carrying
        /// Vale), then Go once the car is free. Checks each order was taken; returns Go's answer. Anyone not named stays out (left behind).</summary>
        public Answer GetAway(params string[] ops)
        {
            var car = G.S.At(G.S.GetawayCar);
            foreach (var name in ops)
            {
                if (G.Over) break;
                var o = Op(name); if (o.Down || o.InCar) continue;
                if (!G.Within(o.Pos, car, G.R.AtReachCm)) { int at = SendTo(name, G.S.Stand(G.S.GetawayCar)); while (!G.Over && G.Tick < at) G.Step(); }
                if (G.Over) break;
                Do(name, "board", "car"); Done(name); if (!G.Over) G.Step();
            }
            if (G.Over) return default;
            var a = G.Issue(new Order { Tick = G.Tick, Op = "cell", Do = "go" });
            Is(a.ok, $"go taken at {Report.Clock(G.Tick)}{(a.ok ? "" : ": " + a.why)}");
            return a;
        }
        /// <summary>The Enforcer boards with Vale and the car goes (brief 018: Extract retired into Board and Go).</summary>
        public Answer Extract() => GetAway("enforcer");
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
            var r = new Run(Engine.New(i, s.Start)) { Inputs = i };
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
            if (id.StartsWith("S", StringComparison.Ordinal) && !id.Contains('-')) /* the 28 are S1–S28; Motive's S9-* (tuning.md §9.5) are Handover cases */ { s.Start.FinalFight = false; s.Start.Crowd = false; s.Start.Security ??= 1; }
            all!.Add(s); return s;
        }
        static int T(int s) => Game.T(s);

    }
}
