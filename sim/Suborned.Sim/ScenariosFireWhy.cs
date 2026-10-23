// Engine 0.25.0 scenarios: why a hostile fires on an operative (Sky playing 0.2.56: "Enforcer was shot by the door guard - I couldn't tell
// if that's because weapon was out, carrying Vale or coz they shot police etc?"). Read-only: Game.FireWhyOf, FireCauses, FireWhyFirst and
// the log code ai.why (GameFireWhy.cs). Each case reads the cause the hostile has for the agent it takes.
//    FW-DRAWN     the guard takes the Enforcer for the weapon he draws in his sight at Alert 2 (drawn; not fired, not Vale)
//    FW-FIRED     the Enforcer shoots at the guard: the guard took him for the draw (drawn, face); once he has fired, fired is the guard's
//                 strongest cause; the next hostile to take him has fired first
//    FW-VALE      route fight-loud-enforcer-down's orders: the rival crew takes the Enforcer for Vale alone (it reads drawn, vale, seen;
//                 not fired); any line naming both lists fired before vale (the fixed order)
//    FW-VIOLENCE  the door guard sees the takedown fail: he takes the holstered Enforcer for violence (strongest) and his known face
//    FW-FACE      HI-ROBOT: the terrace customers' report carries the Enforcer's face; a robot takes him holstered for his face alone
//    FW-SEEN      the crew takes the drawn Enforcer for the draw; he holsters; it stays on him for what it saw (seen), its first causes kept
//    FW-RANGE     the fight range: the officer takes the holstered Scout by the range's every-operative rule (range)
// Every case also checks the log: ai.why once a pair, never "no cause", and each line's pair is FireWhyFirst's.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        /// <summary>The hostile `by` on agent `op` now (FireWhyOf), or null.</summary>
        static FireWhy? Why(Run r, string op, string by) { foreach (var w in r.G.FireWhyOf(Op.Parse(op))) if (w.By == by) return w; return null; }
        /// <summary>The first hostile on agent `op` (FireWhyOf) whose causes satisfy `ok`, or null.</summary>
        static FireWhy? WhyAny(Run r, string op, Func<FireWhy, bool> ok) { foreach (var w in r.G.FireWhyOf(Op.Parse(op))) if (ok(w)) return w; return null; }
        static string Cs(string[] c) => c.Length == 0 ? "none" : string.Join(", ", c);

        /// <summary>ai.why: once a (hostile, agent) pair, each with a cause, each matching the read's first record.</summary>
        static void FireWhyLog(Run r)
        {
            var g = r.G; var lines = g.Log.Where(l => l.Code == "ai.why").ToList();
            r.Is(lines.Count > 0, $"ai.why is logged ({lines.Count})");
            var pairs = new HashSet<string>(); bool once = true, cause = true, match = true;
            foreach (var l in lines)
            {
                var w = l.Text.Split(' '); string by = w[0], op = w[3];
                if (!pairs.Add(by + "/" + op)) once = false;
                if (l.Text.Contains("no cause")) cause = false;
                var f = g.FireWhyFirst(by, Op.Parse(op)); if (f.tick != l.Tick || !l.Text.EndsWith(": " + string.Join(", ", f.causes) + ".", StringComparison.Ordinal)) match = false;
            }
            r.Is(once, "once a pair"); r.Is(cause, "every line names a cause"); r.Is(match, "each line is FireWhyFirst's (tick and causes)");
        }

        /// <summary>Route fight-loud-enforcer-down's orders (contracts/missions/handover/routes, 2026-10-22), copied so the case stands alone.</summary>
        static readonly string[] FightLoudOrders =
        {
            "{\"tick\":0,\"op\":\"tech\",\"do\":\"move\",\"to\":\"cabinet\",\"run\":true}",
            "{\"tick\":0,\"op\":\"enforcer\",\"do\":\"move\",\"to\":[5650,-4750],\"run\":true}",
            "{\"tick\":166,\"op\":\"tech\",\"do\":\"loop\",\"target\":\"entrance\"}",
            "{\"tick\":786,\"op\":\"tech\",\"do\":\"reroute\",\"target\":\"patrol\"}",
            "{\"tick\":1236,\"op\":\"enforcer\",\"do\":\"fire\",\"target\":\"guard\"}",
            "{\"tick\":1276,\"op\":\"enforcer\",\"do\":\"move\",\"to\":\"d:Office entrance\",\"run\":true}",
            "{\"tick\":1312,\"op\":\"enforcer\",\"do\":\"enter\"}",
            "{\"tick\":1992,\"op\":\"enforcer\",\"do\":\"move\",\"to\":\"police-1\"}",
        };

        static void FireWhyCases()
        {
            Add("FW-DRAWN", "—", "engine 0.25.0: at Alert 2 the Enforcer draws in the door guard's sight; the guard takes him for the draw (drawn), not for Vale or a shot", r =>
            {
                var g = r.G;
                r.To(Math.Max(r.Send(Enf, "d:Office approach"), r.Send(Fixer, "d:Official drop-off"))); // F14-OPEN's Alert 2: the Fixer draws on the pickup camera
                r.Do(Fixer, "draw"); r.Is(r.Until(() => g.Alert >= 2, T(3)), "the camera shows a drawn weapon: Alert 2"); r.Do(Fixer, "holster"); r.Wait(T(2));
                r.Eq(g.FireWhyOf(OpId.Enforcer).Count, 0, "holstered, nobody is on the Enforcer");
                r.Is(g.FireCauses(GuardR, OpId.Enforcer).Length == 0, "the guard has no cause for him");
                r.Do(Enf, "draw");
                r.Is(r.Until(() => g.GuardGun.Target == Enf, T(3)), "the guard sees the draw and aims");
                var w = Why(r, Enf, GuardR);
                r.Is(w.HasValue && w.Value.Aiming && w.Value.Main == "drawn", $"FireWhyOf(enforcer): the guard, aiming, main cause drawn ({(w.HasValue ? Cs(w.Value.Causes) : "-")})");
                r.Is(w.HasValue && !w.Value.Causes.Contains("vale") && !w.Value.Causes.Contains("fired"), "not Vale, not a shot");
                r.Is(w.HasValue && w.Value.First.SequenceEqual(w.Value.Causes) && w.Value.Since == g.Tick, "its first causes are these, this tick");
                r.Is(g.Log.Exists(l => l.Code == "ai.why" && l.Tick == g.Tick && l.Text.StartsWith("guard takes the enforcer as its target: drawn", StringComparison.Ordinal)), "ai.why: guard takes the enforcer as its target: drawn");
                r.Eq(g.FireWhyOf(OpId.Fixer).Count, 0, "the Fixer, holstered, is nobody's target");
                FireWhyLog(r);
            });

            Add("FW-FIRED", "—", "engine 0.25.0: the Enforcer shoots at the guard from the Office approach: the guard took him for the draw, then has fired as his strongest cause; the next hostile to take him has fired first", r =>
            {
                var g = r.G;
                r.To(r.Send(Enf, "d:Office approach"));
                r.Fire(Enf, GuardR); int t0 = g.Tick;
                r.Is(r.Until(() => r.Shots().Exists(x => x.by == Enf) || g.Over, T(10)), "the Enforcer fires");
                var gw = Why(r, Enf, GuardR);
                r.Is(gw.HasValue && gw.Value.Since >= t0 && gw.Value.First.SequenceEqual(new[] { "drawn", "face" }), $"the guard took him as the fire order drew his weapon: drawn, face ({(gw.HasValue ? Cs(gw.Value.First) : "-")})");
                r.Is(gw.HasValue && gw.Value.Causes.Length > 0 && gw.Value.Causes[0] == "fired", $"once he has fired, the guard's strongest cause is that he fired ({(gw.HasValue ? Cs(gw.Value.Causes) : "-")}); Main {(gw.HasValue ? gw.Value.Main : "-")}");
                r.Is(r.Until(() => WhyAny(r, Enf, x => x.By != GuardR && x.Since >= t0) != null || g.Over, T(30)), "another hostile takes him");
                var w = WhyAny(r, Enf, x => x.By != GuardR && x.Since >= t0);
                r.Is(w.HasValue && w.Value.First.Length > 0 && w.Value.First[0] == "fired", $"{(w.HasValue ? w.Value.By : "-")} takes the Enforcer: fired first ({(w.HasValue ? Cs(w.Value.First) : "-")})");
                FireWhyLog(r);
            });

            Add("FW-VALE", "—", "engine 0.25.0: the loud take (the orders of route fight-loud-enforcer-down): the rival crew takes the Enforcer for Vale (its list is drawn, vale, seen; a shot at the guard is not on it); police list fired before vale", r =>
            {
                var g = r.G;
                foreach (var js in FightLoudOrders)
                {
                    var o = Order.Parse(JNode.Parse(js)); g.RunTo(o.Tick); if (g.Over) break;
                    o.Tick = g.Tick; r.Is(g.Issue(o).ok, $"{o.Op} {o.Do} taken at {Report.Clock(g.Tick)}");
                }
                r.Is(r.Until(() => WhyAny(r, Enf, x => x.By.StartsWith("rival", StringComparison.Ordinal)) != null || g.Over, T(150)), "the rivals take the Enforcer");
                var w = WhyAny(r, Enf, x => x.By.StartsWith("rival", StringComparison.Ordinal));
                r.Is(w.HasValue && w.Value.First.Contains("vale") && !w.Value.First.Contains("fired"), $"{(w.HasValue ? w.Value.By : "-")}: vale, never fired ({(w.HasValue ? Cs(w.Value.First) : "-")})");
                r.Is(w.HasValue && w.Value.Main == "vale", "Vale is the strongest cause it has");
                r.Is(r.Op(Enf).Gun.Fired, "he has fired (at the guard)");
                foreach (var l in g.Log.Where(l => l.Code == "ai.why" && l.Text.Contains(" the enforcer ") && l.Text.Contains("vale") && l.Text.Contains("fired")))
                    r.Is(l.Text.IndexOf("fired", StringComparison.Ordinal) < l.Text.IndexOf("vale", StringComparison.Ordinal), $"fixed order, fired before vale ({l.Text})");
                FireWhyLog(r);
            });

            Add("FW-VIOLENCE", "—", "engine 0.25.0: N51's takedown in the door guard's cone fails; the guard takes the holstered Enforcer for the violence he saw (and the face it gave him)", r =>
            {
                var g = r.G;
                r.To(r.SendTo(Enf, AheadOfGuard(r, 120)));
                r.Takedown("guard", "knockout");
                r.Is(g.Log.Exists(l => l.Code == "takedown.fails") && g.MarkedFoe(Enf), "the takedown fails: security saw violence");
                r.Is(!r.Op(Enf).Gun.Drawn && !r.Op(Enf).Gun.Fired, "the Enforcer is holstered and has fired nothing");
                r.Is(r.Until(() => g.GuardGun.Target == Enf || g.Over, T(10)), "the guard takes him");
                var w = Why(r, Enf, GuardR);
                r.Is(w.HasValue && w.Value.Main == "violence", $"main cause violence ({(w.HasValue ? Cs(w.Value.Causes) : "-")})");
                r.Is(w.HasValue && w.Value.Causes.SequenceEqual(new[] { "violence", "face" }), "violence, then his face (Alert 2: the police face rung)");
                FireWhyLog(r);
            });

            Add("FW-FACE", "—", "engine 0.25.0: HI-ROBOT: the terrace customers' report carries the Enforcer's face; at Alert 3 a robot takes him holstered for his face alone", r =>
            {
                var g = r.G; TerraceTakedown(r);
                r.Is(r.Until(() => g.KnownFaces.Contains(Enf), T(60)), "a report lands with the Enforcer's face");
                r.Is(!g.MarkedFoe(Enf), "security did not see the violence itself");
                r.Is(r.Until(() => g.FireWhyOf(OpId.Enforcer).Count > 0 || g.Over, T(120)), "a hostile takes him");
                var w = g.FireWhyOf(OpId.Enforcer)[0];
                r.Is(w.First.SequenceEqual(new[] { "face" }) && w.Main == "face", $"{w.By} takes him for his face alone ({Cs(w.First)})");
                r.Is(!r.Op(Enf).Gun.Drawn && !r.Op(Enf).Gun.Fired, "holstered, never fired");
                FireWhyLog(r);
            });

            Add("FW-SEEN", "—", "engine 0.25.0: HI-RIVALS: the calm crew takes the drawn Enforcer for the draw; he holsters at once; it stays on him for what it saw (seen), its first causes kept (drawn first)", r =>
            {
                var g = r.G; Free(r, false);
                r.To(r.SendTo(Enf, new Pt(8600, -3400)));
                r.Is(r.Until(() => g.RivalsPresent, T(g.S.RivalsArrive) + T(5) - g.Tick), "the rival crew is here");
                var park = g.S.Stand(g.S.RivalsPark); var e = Beside(r, park) ?? park;
                r.To(r.SendTo(Enf, e));
                r.Is(g.RivalsStreetWatching(r.Op(Enf).Pos), "he stands in the crew's cone");
                r.Is(g.FireCauses("rivals", OpId.Enforcer).Length == 0, "holstered, the calm crew has no cause");
                r.Do(Enf, "draw");
                r.Is(r.Until(() => g.RivalsGun.Target == Enf || g.Over, T(6)), "the crew takes the drawn Enforcer");
                var w0 = Why(r, Enf, "rivals");
                r.Is(w0.HasValue && w0.Value.Main == "drawn", $"for the draw ({(w0.HasValue ? Cs(w0.Value.First) : "-")})");
                r.Do(Enf, "holster");
                r.Is(r.Until(() => !r.Op(Enf).Gun.Drawn, T(3)), "he holsters");
                r.Is(g.RivalsSawAct.Contains(Enf), "the crew saw him act");
                var w = Why(r, Enf, "rivals");
                r.Is(w.HasValue && w.Value.Causes.SequenceEqual(new[] { "seen" }), $"holstered, the crew's cause is what it saw: seen ({(w.HasValue ? Cs(w.Value.Causes) : "-")})");
                r.Is(w.HasValue && w0.HasValue && w.Value.First.SequenceEqual(w0.Value.First) && w.Value.First[0] == "drawn", $"its first causes are kept ({(w.HasValue ? Cs(w.Value.First) : "-")})");
                FireWhyLog(r);
            });
        }

        static void FireWhyRange()
        {
            AddRange("FW-RANGE", "today", "lane", "engine 0.25.0: the fight range's bodies fire on every operative: the officer takes the holstered Scout, cause range", r =>
            {
                var g = r.G; var h = g.RangeSlot("near", 1);
                int t = r.SendTo("scout", Ray(r, h, 0, 1000)); int k0 = 0; foreach (var op in new[] { "enforcer", "fixer", "tech" }) t = Math.Max(t, r.SendTo(op, Ray(r, h, 0, 3500 + 300 * k0++)));
                r.To(t); Spawned(r, "police", "near", 1);
                r.Is(r.Until(() => g.FireWhyOf(OpId.Scout).Count > 0, Game.T(10)), "the officer takes the holstered Scout");
                var w = g.FireWhyOf(OpId.Scout)[0];
                r.Is(w.By == "officer-1" && w.Causes.SequenceEqual(new[] { "range" }) && w.First.SequenceEqual(new[] { "range" }), $"officer-1, cause range ({w.By}: {Cs(w.Causes)})");
                r.Is(!r.Op("scout").Gun.Drawn, "holstered");
                r.Until(() => ShotsAt(r, "scout").Count > 0, Game.T(10));
                var w2 = Why(r, "scout", "officer-1");
                r.Is(w2.HasValue && w2.Value.Firing && w2.Value.Aiming, "firing and aiming");
                FireWhyLog(r);
            });
        }
    }
}
