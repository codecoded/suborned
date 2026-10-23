// Engine 0.20.0 scenarios: the final fight follows per-agent intent (Motive tuning.md §15.2, §15.5; rulebook ai.intent.final agent).
// PROVISIONAL: Sky 2026-10-22, "I am not sure...I think so - sounds right"; built so Sky can play it, confirmed after play.
//    F15-CLEAN   a holstered Scout never seen acting, inside an officer's effective ring on a line in the loud final fight, draws no fire;
//                the same run with final every (the control) is fired on, so the post is in harm's way
//    F15-DRAWN   the same Scout draws there in the officers' sight: he is fired on, first (weapons free off: return fires only when fired on)
//    F15-FACE    a Scout whose face is known (the guard's drop-it warning for a seen draw, heeded) is fired on there holstered
// F15-RANGE and F15-SWEEP (the 77 range runs and the matrix rows identical both ways) are tests: sim/Suborned.Sim.Tests/FinalIntent.cs.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        /// <summary>Tuning.md §15.2: the same inputs with ai.intent.final read as `v` (every or agent): the controls and the both-ways checks.
        /// The site, the crowd and the rounds are shared (no rule they read moves); only the rules are parsed again. The texts, and so the inputs
        /// hash and every start hash, stay the original's: a behaviour-only comparison, as `hashes --start-text` (checkpoints compare directly).
        /// Never a run record: a script written from it would claim the original rulebook.</summary>
        public static Engine.Inputs WithFinal(Engine.Inputs i, string v)
        {
            if (v != "every" && v != "agent") throw new ArgumentException($"ai.intent.final '{v}' (every, agent)");
            var re = new Regex("\"final\"\\s*:\\s*\"(every|agent)\"");
            if (re.Matches(i.Rulebook).Count != 1) throw new FormatException("the rulebook names ai.intent.final once");
            string rb = re.Replace(i.Rulebook, $"\"final\": \"{v}\"");
            return new Engine.Inputs
            {
                Rulebook = i.Rulebook, PeopleRules = i.PeopleRules, Site = i.Site, People = i.People, Provisional = i.Provisional, Takedown = i.Takedown, Rounds = i.Rounds,
                Rules = new Rules(rb, i.PeopleRules), SiteData = i.SiteData, Crowd = i.Crowd,
            };
        }

        /// <summary>A post 6 to 10 m from the first police stand, free, reachable, with a line from the stand: inside an officer's effective
        /// ring (15 m) once it holds there, about 70 m from the Office door, out of every camera's view.</summary>
        static Pt PolicePost(Run r)
        {
            var g = r.G; var st = g.S.Stand(g.S.FightPolice[0]);
            for (int d = 600; d <= 1000; d += 50)
                foreach (var dir in new[] { new Pt(1, 0), new Pt(1, -1), new Pt(1, 1), new Pt(0, 1), new Pt(0, -1), new Pt(-1, 0) })
                {
                    var q = st + new Pt(dir.X * d, dir.Y * d);
                    if (g.S.Grid.Free(q) && g.S.Grid.Route(r.Op(Scout).Pos, q) != null && g.S.Sight(st, q, g.R.Eye) && !g.CameraWatching("entrance", q) && !g.CameraWatching("pickup", q)) return q;
                }
            throw new InvalidOperationException("no post beside the first police stand");
        }

        /// <summary>The Scout to the police post; the Tech loops the entrance camera and reroutes the patrol; the Enforcer shoots the guard from beside the door, goes in, and comes out with Vale: a loud
        /// final fight. The Scout stays holstered and gives no order after the walk unless the case does. Returns the fight's first tick.</summary>
        static int F15Loud(Run r, Pt post, Action? scoutFirst = null)
        {
            var g = r.G;
            int at = Math.Max(r.Send(Tech, "cabinet"), r.SendTo(Enf, BesideDoor)); // N10's loud route (LoudToCar)
            if (scoutFirst == null) at = Math.Max(at, r.SendTo(Scout, post)); else { scoutFirst(); r.SendTo(Scout, post); } // scoutFirst: the Scout's own business on the way (F15-FACE)
            r.To(Math.Max(at, g.Tick));
            r.Do(Tech, "loop", "entrance"); r.Done(Tech); r.Do(Tech, "reroute", "patrol"); r.Done(Tech);
            int mark = g.NextMarkOf(g.GuardP, g.Tick); if (mark - g.Tick < T(30)) r.To(mark + 1);
            r.Fire(Enf, GuardR); r.Is(r.Until(() => g.GuardDown, T(30)), "the Enforcer shoots the guard down");
            r.To(r.Send(Enf, "d:Office entrance"));
            r.Do(Enf, "enter"); r.Done(Enf);
            r.Is(r.Until(() => g.FightBranch.Length > 0, T(30)), "the Enforcer comes out with Vale: the final fight");
            r.Eq(g.FightBranch, "loud", "a loud final fight");
            return g.Tick;
        }

        /// <summary>Officers (final-fight police bodies) up, present, with the Scout inside their effective ring on a clear line.</summary>
        static List<string> Exposing(Game g, Pt at)
        {
            var l = new List<string>();
            foreach (var h in g.Hostiles)
            {
                if (!h.Id.StartsWith("officer-", StringComparison.Ordinal) || h.Gun.Down || !h.Present || h.Gun.W == null) continue;
                var p = h.Pos(g.Tick); long eff = h.Gun.W.EffCm;
                if (G.D2(p, at) <= eff * eff && g.S.Sight(p, at, g.R.Eye)) l.Add(h.Id);
            }
            return l;
        }

        /// <summary>Step `window` ticks (or to the end); count the ticks an officer has the Scout in its ring on a line.</summary>
        static int Exposed(Run r, int window, Func<bool>? stop = null)
        {
            var g = r.G; int n = 0, end = g.Tick + window;
            while (g.Tick < end && !g.Over && (stop == null || !stop())) { if (Exposing(g, r.Op(Scout).Pos).Count > 0) n++; g.Step(); }
            return n;
        }

        static void FinalIntentCases()
        {
            Add("F15-CLEAN", "—", "tuning.md §15.2 (PROVISIONAL, Sky to confirm after play): in the loud final fight a holstered Scout never seen acting, inside an officer's effective ring on a line, draws no fire; the control (final every) is fired on there", r =>
            {
                var g = r.G; r.Is(!g.R.Intent.FinalEvery, "the rulebook's final fight is per agent (ai.intent.final agent)");
                var post = PolicePost(r); int t0 = F15Loud(r, post);
                r.Is(r.Until(() => Exposing(g, r.Op(Scout).Pos).Count > 0, T(60)), $"an officer has the Scout in its effective ring on a line ({Report.Clock(g.Tick)})");
                int exposed = Exposed(r, T(30));
                r.Is(exposed >= T(5), $"for at least 5 s of the next 30 ({exposed / 10}.{exposed % 10} s)"); // integers: the report is the same in every culture (Redline E7)
                r.Eq(ShotsAt(r, Scout, t0).Count, 0, "nobody fires on the Scout: he did nothing they saw and his face is unknown");
                r.Is(!r.Op(Scout).Gun.Drawn && !r.Op(Scout).Gun.Fired && !g.KnownFaces.Contains(Scout), "holstered, never fired, face unknown");
                r.Is(g.Hostiles.Count(h => h.Id.StartsWith("officer-", StringComparison.Ordinal) && h.Present && !h.Gun.Down) >= 1 && g.FightBranch == "loud" && g.FightOverTick > g.Tick, "the officers are up and the final fight still on");

                // the control: the same orders with final every (the rule before 0.20.0): the post is in harm's way. A run made by hand
                // (suborned-sim hashes) carries no inputs: the control is then left to Scenarios.Run (the tests); it moves no checkpoint of this run.
                if (r.Inputs == null) return;
                var every = WithFinal(r.Inputs!, "every"); var c = new Run(Engine.New(every, g.Start)) { Inputs = every };
                r.Is(c.G.R.Intent.FinalEvery, "the control reads final every");
                int c0 = F15Loud(c, post);
                c.Until(() => Exposing(c.G, c.Op(Scout).Pos).Count > 0, T(60));
                Exposed(c, T(30), () => ShotsAt(c, Scout, c0).Count > 0);
                r.Is(ShotsAt(c, Scout, c0).Count > 0, $"control: with final every the officers fire on the holstered Scout at the same post ({ShotsAt(c, Scout, c0).Count} shots; first at {(ShotsAt(c, Scout, c0).Count > 0 ? Report.Clock(ShotsAt(c, Scout, c0)[0].tick) : "-")})");
                foreach (var (ok, what) in c.Checks) if (!ok) r.Is(false, "control: " + what);
            });

            Add("F15-DRAWN", "—", "tuning.md §15.2: the same Scout draws in the officers' sight in the final fight: he is a foe for his own act and is fired on first (weapons free off: return fires only when fired on)", r =>
            {
                var g = r.G; Free(r, false);
                var post = PolicePost(r); int t0 = F15Loud(r, post);
                r.Is(r.Until(() => Exposing(g, r.Op(Scout).Pos).Count > 0, T(60)), "an officer has the Scout in its effective ring on a line");
                r.Wait(T(3));
                r.Eq(ShotsAt(r, Scout, t0).Count, 0, "holstered, not fired on");
                r.Do(Scout, "draw"); int drew = g.Tick;
                r.Is(r.Until(() => ShotsAt(r, Scout, drew).Count > 0, T(10)), "drawn in their sight, he is fired on");
                var first = ShotsAt(r, Scout, drew).First();
                r.Is(first.by.StartsWith("officer-", StringComparison.Ordinal) || first.by.StartsWith("unit", StringComparison.Ordinal) || first.by.StartsWith("robot-", StringComparison.Ordinal), $"by the police ({first.by})");
                r.Is(!r.Shots().Exists(x => x.by == Scout && x.tick <= first.tick), "he had fired nothing: the draw alone made him a foe");
            });

            Add("F15-FACE", "—", "tuning.md §15.2: a Scout whose face is known (a seen draw earlier: the guard's drop-it warning, heeded) is fired on holstered in the final fight", r =>
            {
                var g = r.G; var ap = r.Stand("d:Office approach"); Pt? cone = null;
                for (int d = 200; d <= 1200 && cone == null; d += 50) foreach (var dir in new[] { new Pt(1, 0), new Pt(-1, 0), new Pt(1, -1), new Pt(-1, -1), new Pt(0, -1), new Pt(1, 1), new Pt(-1, 1) })
                { var q = ap + new Pt(dir.X * d, dir.Y * d); if (cone == null && g.S.Grid.Free(q) && g.S.Grid.Route(r.Op(Scout).Pos, q) != null && g.GuardWatching(q, false) && !g.CameraWatching("entrance", q) && !g.CameraWatching("pickup", q)) cone = q; }
                if (cone == null) { r.Is(false, "a post in the guard's cone beside the Office approach"); return; }
                var post = PolicePost(r);
                int t0 = F15Loud(r, post, () =>
                {
                    r.To(r.SendTo(Scout, cone.Value));
                    r.Do(Scout, "draw");
                    r.Is(r.Until(() => g.Warnings.Count > 0, T(2)), "the guard warns him: drop it");
                    r.Is(g.KnownFaces.Contains(Scout), "the guard has his face");
                    r.Do(Scout, "holster"); r.Is(r.Until(() => g.Warnings.Count == 0, T(3)), "heeded");
                    r.Eq(ShotsAt(r, Scout).Count, 0, "nobody has fired on him");
                });
                r.Is(g.Alert >= g.R.Intent.PoliceFace, $"the final fight is at Alert {g.Alert}: a known face is a police foe");
                r.Is(r.Until(() => ShotsAt(r, Scout, t0).Count > 0, T(90)), "the officers fire on his known face");
                r.Is(!r.Op(Scout).Gun.Drawn && !r.Op(Scout).Gun.Fired, "holstered, never fired: for his own face, not the Enforcer's acts");
            });
        }
    }
}
