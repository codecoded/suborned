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
        public string FightBranch = ""; public int FightHostiles, FightDownedByEnforcer, FightDownedByGuard, FightDownedByCell, FightHits, FightShots;
        public readonly List<string> Captured = new List<string>(); public readonly List<(string op, int health, int max)> Cell = new List<(string, int, int)>();
        public Investigation Inv = new Investigation(); public Between Next = new Between(); public int Sightings; public readonly List<string> FightWhy = new List<string>();
        public readonly List<string> Assets = new List<string>(), Route = new List<string>(), Extra = new List<string>(), Chain = new List<string>();
        /// <summary>brief 018 §3: everyone not in the car when it left, with fate and why.</summary>
        public readonly List<(string op, string fate, string why)> LeftBehind = new List<(string, string, string)>();

        public static Debrief Of(Game g)
        {
            var d = new Debrief { Loud = g.Gunfire > 0 };
            d.PublicSecurity = g.Alert >= 3 ? "high" : d.Loud ? "raised" : "calm";
            bool inezExposed = g.GuardReachedCafe;
            d.Inv = g.Investigate(); d.Next = g.BetweenMissions(d.Inv); d.Sightings = g.Sightings.Count;
            // §08: little → early → active by Trail (0 / 1–2 / 3+); a named cause takes precedence: a civilian shot, a witness of violence, the
            // Fixer's face given up to the investigators (rulebook investigation), the staged distraction; then the count
            (d.InvestigationState, d.InvestigationCause) =
                g.GunOf("waitress")!.Down ? ("active", "waitress-shot") :
                g.GunOf("driver")!.Down ? ("active", "driver-shot") :
                g.InezWitnessedShooting ? ("active", "waitress-witnessed-shooting") :
                g.InezMethod == "pressure" ? ("active", "waitress-can-identify-fixer") :
                d.Inv.NamedCause.Length > 0 ? ("active", d.Inv.NamedCause) :
                inezExposed ? ("active", "staged-distraction") :
                g.Trail >= 3 ? ("active", "several-interventions") :
                g.Trail > 0 ? ("early", "few-leads") : ("little", "");
            if (g.Inez.Turned && !g.Inez.Down) d.Contacts++;
            if (g.Driver.Turned && !g.Driver.Down) d.Contacts++;
            if (g.Inez.Turned && !g.Inez.Down)
                d.Assets.Add(g.InezWitnessedShooting ? "waitress-witnessed-shooting" : g.InezMethod == "pressure" ? "waitress-may-identify-fixer" : inezExposed ? "waitress-may-be-questioned" : "waitress-introduces-cafe");
            if (g.Driver.Turned && !g.Driver.Down) d.Assets.Add("driver-alters-pickups");
            d.Chain.AddRange(g.Chain);
            d.FightBranch = g.FightBranch; d.FightHostiles = g.Hostiles.Count; d.FightDownedByEnforcer = g.DownedByEnforcer; d.FightDownedByGuard = g.DownedByGuard; d.FightDownedByCell = g.DownedByCell; d.FightHits = g.FightHits; d.FightShots = g.FightShots; d.FightWhy.AddRange(g.FightWhy);
            d.Captured.AddRange(g.Captured); d.LeftBehind.AddRange(g.LeftBehindAtGo); foreach (var op in g.Ops) d.Cell.Add((Sim.Op.Name(op.Id), op.Gun.Health, op.Gun.Max)); // wounds carry over (§08)

            var relevant = new HashSet<string>(StringComparer.Ordinal);
            if (g.RouteKind == "front")
            {
                relevant.Add("loop");
                if (g.RouteGuard == "shot") relevant.Add("fire:guard");
                else if (g.RouteGuard == "waitress") { relevant.Add("turn." + (g.InezMethod.Length > 0 ? g.InezMethod : "help") + ":waitress"); relevant.Add("call:waitress"); }
                if (g.RouteBot == "rerouted") relevant.Add("reroute");
                d.Route.Add(g.RouteGuard == "shot" ? (g.InezWitnessedShooting ? "guard-shot-beside-waitress" : "guard-shot-away-from-waitress")
                    : g.RouteGuard == "knocked-out" ? "guard-knocked-out" : g.RouteGuard == "killed" ? "guard-killed" // a takedown (brief 013 E4), not a shooting
                    : "waitress-pulls-guard-away");
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
            w.Open("investigators").Prop("count", Inv.Investigators).Prop("hops", Inv.Hops).Prop("sightings", Sightings).OpenArr("findings");
            foreach (var x in Inv.Findings)
            {
                w.Obj().Prop("hop", x.Hop).Prop("who", x.Who).Prop("outcome", x.Outcome).Prop("lead", x.Lead);
                w.OpenArr("gives"); foreach (var f in x.Facts) w.Str(f); w.EndArr();
                if (x.Cause.Length > 0) w.Prop("because", x.Cause);
                w.EndObj();
            }
            w.EndArr();
            if (Inv.Hunted.Count > 0) { w.OpenArr("hunted"); foreach (var x in Inv.Hunted) w.Str(x); w.EndArr(); } // brief 018: written only when set
            w.EndObj();
            w.Open("nextMission").Prop("ambush", Next.Ambush);
            if (Next.Cases.Count > 0 || Next.CampaignTrail != 0) // brief 013: written only when a takedown left something, so older debriefs read as before
                w.Prop("campaignTrail", Next.CampaignTrail).Prop("campaignStanding", Next.CampaignStanding).Prop("openCases", Next.OpenCases).Prop("nextLevel", Next.NextLevel); w.OpenArr("ambushBy"); foreach (var a in Next.AmbushBy) w.Str(a); w.EndArr();
            w.OpenArr("standing"); // brief 014: one line per district (rulebook debrief.lines.standing) and the Standing the next mission starts at
            foreach (var l in Next.Standing)
            {
                w.Obj().Prop("district", l.District).Prop("from", l.From).Prop("to", l.To).Prop("band", l.Band).Prop("cause", l.Cause).Prop("who", l.Who)
                 .Prop("mission", l.End - l.From).Prop("found", l.Found).Prop("word", l.Word).Prop("wordUncapped", l.WordUncapped).Prop("drift", l.Drift).OpenArr("moves");
                foreach (var (d, c, who) in l.Moves) w.Obj().Prop("delta", d).Prop("cause", c).Prop("who", who).EndObj();
                w.EndArr().EndObj();
            }
            w.EndArr();
            if (Next.Captives.Count > 0 || Next.CarSeenBy.Length > 0) // brief 018 §3: written only when the car left someone or was seen
            {
                if (Next.CarSeenBy.Length > 0) w.Prop("carSeenBy", Next.CarSeenBy);
                w.OpenArr("captives");
                foreach (var c in Next.Captives)
                {
                    w.Obj().Prop("who", c.Op).Prop("why", c.Why).Prop("heldAt", c.HeldAt).Prop("holds", c.Holds).Prop("talksAt", c.TalksAt).Prop("rescueUntil", c.RescueUntil)
                     .Prop("bravery", c.Sc[0]).Prop("resentment", c.Sc[1]).Prop("fear", c.Sc[2]).Prop("loyalty", c.Sc[3]).Prop("left", c.Left).Prop("right", c.Right).OpenArr("gives");
                    foreach (var f in c.Facts) w.Str(f);
                    w.EndArr().EndObj();
                }
                w.EndArr();
            }
            w.OpenArr("lines"); foreach (var l in Next.Lines) w.Obj().Prop("line", l.Line).Prop("who", l.Who).Prop("detail", l.Detail).EndObj(); w.EndArr();
            w.OpenArr("contacts");
            foreach (var c in Next.Contacts)
                w.Obj().Prop("who", c.Role.Length > 0 ? c.Role : c.Id).Prop("name", c.Name).Prop("need", c.Need).Prop("needKnown", c.KNeed)
                 .Prop("bravery", c.Sc[0]).Prop("resentment", c.Sc[1]).Prop("fear", c.Sc[2]).Prop("loyalty", c.Sc[3]).EndObj();
            w.EndArr().EndObj();
            if (FightBranch.Length > 0)
            {
                w.Open("finalFight").Prop("branch", FightBranch).Prop("hostiles", FightHostiles).Prop("downedByEnforcer", FightDownedByEnforcer).Prop("downedByGuard", FightDownedByGuard).Prop("downedByCell", FightDownedByCell).Prop("hitsTaken", FightHits).Prop("shots", FightShots);
                w.OpenArr("why"); foreach (var a in FightWhy) w.Str(a); w.EndArr(); w.EndObj();
            }
            w.OpenArr("cell"); foreach (var (op, hp, max) in Cell) w.Obj().Prop("op", op).Prop("health", hp).Prop("max", max).EndObj(); w.EndArr();
            w.OpenArr("captured"); foreach (var c in Captured) w.Str(c); w.EndArr();
            if (LeftBehind.Count > 0) { w.OpenArr("leftBehind"); foreach (var (op, fate, why) in LeftBehind) w.Obj().Prop("who", op).Prop("fate", fate).Prop("why", why).EndObj(); w.EndArr(); } // brief 018
            w.EndObj();
        }
    }

    /// <summary>Loading and playing: the engine's inputs from the repo, a script played to its end, a replay from its orders.</summary>
    public static class Engine
    {
        public const string RulebookFile = "contracts/rules/rulebook.json", PeopleRulesFile = "contracts/people/people.rules.json", SiteFile = "sim/data/handover.site.json", PeopleFile = "sim/data/handover.people.json",
            ProvisionalFile = "sim/data/handover.provisional.json", // brief 013: the security level until the plan and mission carry it
            TakedownFile = "sim/data/handover.takedown.json";       // brief 013: stashes, report points, docks and posts (Splice, tools/sim/takedown.ts)

        public sealed class Inputs
        {
            public string Rulebook = "", PeopleRules = "", Site = "", People = "", Provisional = "", Takedown = ""; public Rules Rules = null!; public Site SiteData = null!; public Crowd? Crowd;
            /// <summary>The inputs' hash over their canonical text (no BOM, LF line ends: Redline E4), so a Windows checkout replays a Mac record.</summary>
            public ulong Hash { get { unchecked { var h = Sim.Hash.Of(Rulebook) ^ (Sim.Hash.Of(PeopleRules) * 3) ^ (Sim.Hash.Of(Site) * 7) ^ (Sim.Hash.Of(People) * 11); if (Provisional.Length > 0) h ^= Sim.Hash.Of(Provisional) * 13; if (Takedown.Length > 0) h ^= Sim.Hash.Of(Takedown) * 17; return h; } } } // a host without brief 013's files hashes as before, and its record says so
        }
        static Inputs? cache; static string? cacheRoot;
        public static Inputs Load(string root)
        {
            if (cache != null && cacheRoot == root) return cache;
            string rd(string f) => System.IO.File.ReadAllText(System.IO.Path.Combine(root, f));
            string? opt(string f) => System.IO.File.Exists(System.IO.Path.Combine(root, f)) ? rd(f) : null;
            var i = FromTexts(rd(RulebookFile), rd(PeopleRulesFile), rd(SiteFile), rd(PeopleFile), opt(ProvisionalFile), opt(TakedownFile));
            cache = i; cacheRoot = root; return i;
        }
        /// <summary>The same inputs from their texts, for a host that reads files its own way (Unity: TextAssets or StreamingAssets).</summary>
        /// <summary>The engine's version for run records: bump it when a change moves any replay. The assembly's MVID is recorded beside it.</summary>
        public const string Version = "suborned.sim/0.7.0"; // 0.7.0: the getaway (brief 018, Sky 2026-10-18): the hold is 8 m or the exit lane (site getaway.exit) with one blocker per fight, Board and Go replace Extract, left behind at Go (captured or walks out), seen leaving (+1 T, the car hunted), the custody projection; the rulebook, the people rules and the site move every inputs hash. 0.6.3: open needs phase 1 (brief 017): the people file is suborned.sim.people/2, every person carries a need record (kind, label, size, holder), read strictly and never acted on; behaviour unchanged, only the inputs hash moves. 0.6.2: a public fight moves Standing once, by whom the cell fought (fight.public, Sky 2026-10-16; retires 0.6.1's gunfire.heard). 0.6.1: a public gunfight costs Standing (gunfire.heard, Sky 2026-10-16). 0.6.0: brief 014 (private turning; Standing starts at +2, the word and the drift between missions). 0.5.0: brief 013 (takedowns, bodies, reports, the response dial, check-in marks)
        public static string Mvid => typeof(Engine).Assembly.ManifestModule.ModuleVersionId.ToString("D");

        public static Inputs FromTexts(string rulebook, string peopleRules, string site, string? people = null, string? provisional = null, string? takedown = null)
        {
            rulebook = JNode.Canon(rulebook); peopleRules = JNode.Canon(peopleRules); site = JNode.Canon(site); people = people == null ? null : JNode.Canon(people);
            provisional = provisional == null ? null : JNode.Canon(provisional); takedown = takedown == null ? null : JNode.Canon(takedown);
            var i = new Inputs { Rulebook = rulebook, PeopleRules = peopleRules, Site = site, People = people ?? "", Provisional = provisional ?? "", Takedown = takedown ?? "" };
            i.Rules = new Rules(rulebook, peopleRules); i.SiteData = new Site(site);
            if (provisional != null) i.SiteData.AddProvisional(provisional);
            if (takedown != null) i.SiteData.AddTakedownWorld(takedown);
            if (people != null)
            {
                var cast = new List<string>(); foreach (var c in i.SiteData.Cast) cast.Add(c.Id);
                i.Crowd = new Crowd(people, i.Rules, cast);
                i.Crowd.Warm(i.SiteData); // every routine leg's walk, once, at load: no tick pays for it
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
            if (until <= 0) until = Game.T(i.SiteData.Over); // a script without `until` plays to the close mission.json gives
            var list = new List<(int idx, Order o)>(); for (int k = 0; k < orders.Count; k++) list.Add((k, orders[k]));
            list.Sort((a, b) => a.o.Tick != b.o.Tick ? a.o.Tick.CompareTo(b.o.Tick) : a.idx.CompareTo(b.idx));
            foreach (var (_, o) in list)
            {
                if (o.Tick > until) break; // orders past `until` are not played (Redline I2)
                g.RunTo(o.Tick);
                if (g.Over && g.Tick != o.Tick) break; // the mission ended before this order's tick: nothing later was given
                if (g.Tick == o.Tick) g.Issue(o);      // at the tick it ended, the played run took (and refused) it: so does the replay
            }
            g.RunTo(until); g.Finish();
            return g;
        }
        /// <summary>Replay a finished game from its own start state and order log.</summary>
        public static Game Replay(Inputs i, Game played)
        {
            var orders = new List<Order>(); foreach (var (o, _, _, _) in played.Orders) orders.Add(o);
            return Play(i, played.Start, orders, played.Tick);
        }
    }
}
