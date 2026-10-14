// The debrief (§08 [B]) as data: the top-down test's nextCity, with ids instead of sentences (Fable writes the words, Glyph lays
// them out). Public security from the final Alert and any shot; the investigation from the Trail and named causes; contacts and
// assets; the route summary; the interventions beyond the route.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed class Debrief
    {
        public string PublicSecurity = "", InvestigationState = "", InvestigationCause = "";
        public int Contacts; public bool Loud;
        public string FightBranch = ""; public int FightHostiles, FightDownedByEnforcer, FightDownedByGuard, FightHits; public readonly List<string> FightWhy = new List<string>();
        public readonly List<string> Assets = new List<string>(), Route = new List<string>(), Extra = new List<string>(), Chain = new List<string>();

        public static Debrief Of(Game g)
        {
            var d = new Debrief { Loud = g.Gunfire > 0 };
            d.PublicSecurity = g.Alert >= 3 ? "high" : d.Loud ? "raised" : "calm";
            bool inezExposed = g.GuardReachedCafe;
            (d.InvestigationState, d.InvestigationCause) =
                g.Inez.Down ? ("active", "waitress-shot") :
                g.Driver.Down ? ("active", "driver-shot") :
                g.InezWitnessedShooting ? ("active", "waitress-witnessed-shooting") :
                g.InezMethod == "pressure" ? ("active", "waitress-can-identify-fixer") :
                inezExposed ? ("active", "staged-distraction") :
                g.Trail >= 3 ? ("active", "several-interventions") :
                g.Trail > 0 ? ("early", "few-leads") : ("little", "");
            if (g.Inez.Turned && !g.Inez.Down) d.Contacts++;
            if (g.Driver.Turned && !g.Driver.Down) d.Contacts++;
            if (g.Inez.Turned && !g.Inez.Down)
                d.Assets.Add(g.InezWitnessedShooting ? "waitress-witnessed-shooting" : g.InezMethod == "pressure" ? "waitress-may-identify-fixer" : inezExposed ? "waitress-may-be-questioned" : "waitress-introduces-cafe");
            if (g.Driver.Turned && !g.Driver.Down) d.Assets.Add("driver-alters-pickups");
            d.Chain.AddRange(g.Chain);
            d.FightBranch = g.FightBranch; d.FightHostiles = g.Hostiles.Count; d.FightDownedByEnforcer = g.DownedByEnforcer; d.FightDownedByGuard = g.DownedByGuard; d.FightHits = g.FightHits; d.FightWhy.AddRange(g.FightWhy);

            var relevant = new HashSet<string>(StringComparer.Ordinal);
            if (g.RouteKind == "front")
            {
                relevant.Add("loop");
                if (g.RouteGuard == "shot") relevant.Add("fire:guard");
                else { relevant.Add("turn." + (g.InezMethod.Length > 0 ? g.InezMethod : "help") + ":waitress"); relevant.Add("call:waitress"); }
                if (g.RouteBot == "rerouted") relevant.Add("reroute");
                d.Route.Add(g.RouteGuard == "shot" ? (g.InezWitnessedShooting ? "guard-shot-beside-waitress" : "guard-shot-away-from-waitress") : "waitress-pulls-guard-away");
                d.Route.Add("entrance-camera-looped");
                d.Route.Add(g.RouteBot == "rerouted" ? "patrol-diverted" : "entry-timed-between-sweeps");
                d.Route.Add("vale-taken-through-office"); d.Route.Add("vale-reaches-getaway-car");
            }
            else if (g.RouteKind == "service" || g.RouteKind == "pickup")
            {
                if (g.RouteKind == "service") relevant.Add("turn.help:driver"); else relevant.Add("call:driver");
                if (g.RouteRival == "mislead") relevant.Add("mislead"); else if (g.RouteRival == "fire") relevant.Add("fire:rivals");
                d.Route.Add(g.RouteKind == "service" ? "driver-moves-vale-to-service-lane" : "pickup-camera-blocked");
                if (g.RouteRival.Length > 0) d.Route.Add(g.RouteRival == "fire" ? "rivals-driven-back" : "rivals-follow-false-tip");
                d.Route.Add("vale-intercepted-outside"); d.Route.Add("vale-reaches-getaway-car");
            }
            // interventions that left a trace beyond the route (the top-down test's traceLabels), counted in the order first taken
            var traced = new[] { "turn.ask:waitress", "turn.pay:waitress", "turn.help:waitress", "turn.pressure:waitress", "call:waitress", "turn.help:driver", "turn.pay:driver",
                "call:driver", "loop", "reroute", "mislead", "fire:guard", "fire:waitress", "fire:driver", "fire:rivals" };
            var order = new List<string>(); var count = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var a in g.Actions)
            {
                if (Array.IndexOf(traced, a) < 0 || relevant.Contains(a)) continue;
                if (!count.ContainsKey(a)) { count[a] = 0; order.Add(a); }
                count[a]++;
            }
            foreach (var a in order) d.Extra.Add($"{count[a]} {a}");
            return d;
        }

        public void Write(JWriter w)
        {
            w.Obj().Prop("publicSecurity", PublicSecurity).Open("investigation").Prop("state", InvestigationState).Prop("cause", InvestigationCause).EndObj()
                .Prop("contacts", Contacts).Prop("loud", Loud);
            w.OpenArr("assets"); foreach (var a in Assets) w.Str(a); w.EndArr();
            w.OpenArr("route"); foreach (var a in Route) w.Str(a); w.EndArr();
            w.OpenArr("extraInterventions"); foreach (var a in Extra) w.Str(a); w.EndArr();
            w.OpenArr("chain"); foreach (var a in Chain) w.Str(a); w.EndArr();
            if (FightBranch.Length > 0)
            {
                w.Open("finalFight").Prop("branch", FightBranch).Prop("hostiles", FightHostiles).Prop("downedByEnforcer", FightDownedByEnforcer).Prop("downedByGuard", FightDownedByGuard).Prop("hitsTaken", FightHits);
                w.OpenArr("why"); foreach (var a in FightWhy) w.Str(a); w.EndArr(); w.EndObj();
            }
            w.EndObj();
        }
    }

    /// <summary>Loading and playing: the engine's inputs from the repo, a script played to its end, a replay from its orders.</summary>
    public static class Engine
    {
        public const string RulebookFile = "contracts/rules/rulebook.json", PeopleRulesFile = "contracts/people/people.rules.json", SiteFile = "sim/data/handover.site.json", PeopleFile = "sim/data/handover.people.json";

        public sealed class Inputs
        {
            public string Rulebook = "", PeopleRules = "", Site = "", People = ""; public Rules Rules = null!; public Site SiteData = null!; public Crowd? Crowd;
            /// <summary>The inputs' hash over their canonical text (no BOM, LF line ends: Redline E4), so a Windows checkout replays a Mac record.</summary>
            public ulong Hash { get { unchecked { return Sim.Hash.Of(Rulebook) ^ (Sim.Hash.Of(PeopleRules) * 3) ^ (Sim.Hash.Of(Site) * 7) ^ (Sim.Hash.Of(People) * 11); } } }
        }
        static Inputs? cache; static string? cacheRoot;
        public static Inputs Load(string root)
        {
            if (cache != null && cacheRoot == root) return cache;
            string rd(string f) => System.IO.File.ReadAllText(System.IO.Path.Combine(root, f));
            var i = FromTexts(rd(RulebookFile), rd(PeopleRulesFile), rd(SiteFile), rd(PeopleFile));
            cache = i; cacheRoot = root; return i;
        }
        /// <summary>The same inputs from their texts, for a host that reads files its own way (Unity: TextAssets or StreamingAssets).</summary>
        /// <summary>The engine's version for run records: bump it when a change moves any replay. The assembly's MVID is recorded beside it.</summary>
        public const string Version = "suborned.sim/0.3.0";
        public static string Mvid => typeof(Engine).Assembly.ManifestModule.ModuleVersionId.ToString("D");

        public static Inputs FromTexts(string rulebook, string peopleRules, string site, string? people = null)
        {
            rulebook = JNode.Canon(rulebook); peopleRules = JNode.Canon(peopleRules); site = JNode.Canon(site); people = people == null ? null : JNode.Canon(people);
            var i = new Inputs { Rulebook = rulebook, PeopleRules = peopleRules, Site = site, People = people ?? "" };
            i.Rules = new Rules(rulebook, peopleRules); i.SiteData = new Site(site);
            if (people != null)
            {
                var cast = new List<string>(); foreach (var c in i.SiteData.Cast) cast.Add(c.Id);
                i.Crowd = new Crowd(people, i.Rules, cast);
            }
            return i;
        }
        /// <summary>The repository root: the nearest directory above `from` holding contracts/rules/rulebook.json.</summary>
        public static string FindRoot(string from)
        {
            var d = new System.IO.DirectoryInfo(from);
            while (d != null) { if (System.IO.File.Exists(System.IO.Path.Combine(d.FullName, RulebookFile))) return d.FullName; d = d.Parent; }
            throw new System.IO.FileNotFoundException($"no {RulebookFile} above {from}");
        }

        public static Game New(Inputs i, Start? start = null) => new Game(i.Rules, i.SiteData, start, i.Hash.ToString(System.Globalization.CultureInfo.InvariantCulture), i.Crowd);

        /// <summary>Play a start state and its orders (sorted by tick, stable) to `until` or the outcome.</summary>
        public static Game Play(Inputs i, Start start, IReadOnlyList<Order> orders, int until)
        {
            var g = New(i, start);
            var list = new List<(int idx, Order o)>(); for (int k = 0; k < orders.Count; k++) list.Add((k, orders[k]));
            list.Sort((a, b) => a.o.Tick != b.o.Tick ? a.o.Tick.CompareTo(b.o.Tick) : a.idx.CompareTo(b.idx));
            foreach (var (_, o) in list)
            {
                if (o.Tick > until) break; // orders past `until` are not played (Redline I2)
                g.RunTo(o.Tick); if (g.Over) break;
                if (g.Tick == o.Tick) g.Issue(o);
                if (g.Over) break;
            }
            g.RunTo(until); g.Finish();
            return g;
        }
        /// <summary>Replay a finished game from its own start state and order log.</summary>
        public static Game Replay(Inputs i, Game played)
        {
            var orders = new List<Order>(); foreach (var (o, _, _) in played.Orders) orders.Add(o);
            return Play(i, played.Start, orders, played.Tick);
        }
    }
}
