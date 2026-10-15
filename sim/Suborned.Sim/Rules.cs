// The rules, read as data and compiled at load: contracts/rules/rulebook.json (Keel) and contracts/people/people.rules.json (Motive).
// Everything the engine reads is checked when the rules load and turned into typed fields: every object's keys (unknown = reject,
// GDD §01), every condition and effect kind with its values, where each kind may stand (no cash-covers outside an event's needs,
// nothing that prices inside a price line, no fit outside a Turn), every v:/p: ref resolved to an integer within its unit's bounds,
// every id (temperament, approach, scale, need, tie kind, memory act, traceable act, event, debrief line) known. After load nothing
// here reads JSON by path or throws on rule content, so Game.Step() can't fail on a bad rulebook (Redline E1).
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public enum Band { Hero, Neutral, Menace }
    public enum Fit { Suits, Worse, Refused }

    public sealed class Cond
    {
        public readonly string K; public readonly JNode N;
        public Cond(JNode n) { N = n; K = n["k"].Str(); Rules.CheckCond(this); }
        public string S(string key) => N[key].Str();
        public bool B(string key) => N[key].Bool();
        public List<string> L(string key) => N[key].Strs();
    }
    public sealed class Eff
    {
        public readonly string Do; public readonly JNode N;
        public Eff(JNode n) { N = n; Do = n["do"].Str(); Rules.CheckEff(this); }
        public string S(string key) => N[key].Str();
        public List<string> L(string key) => N[key].Strs();
    }
    public sealed class Line
    {
        public readonly string Id; public readonly List<Cond> When = new List<Cond>(); public readonly string? Usd, S, Trail, Orders;
        public long UsdV, SV, TrailV, OrdersV; // resolved at load
        public Line(JNode n)
        {
            n.Only("line", "when", "usd", "s", "trail", "orders");
            Id = n["line"].Str();
            if (n.Opt("when") is JNode w) foreach (var c in w.Items) When.Add(new Cond(c));
            Usd = n.Opt("usd")?.Str(); S = n.Opt("s")?.Str(); Trail = n.Opt("trail")?.Str(); Orders = n.Opt("orders")?.Str();
        }
    }
    public sealed class ROutcome
    {
        public readonly string Id; public readonly List<Cond> When = new List<Cond>(); public readonly List<Eff> Changes = new List<Eff>();
        public ROutcome(JNode n)
        {
            n.Only("id", "when", "changes"); Id = n["id"].Str();
            foreach (var c in n["when"].Items) When.Add(new Cond(c));
            foreach (var e in n["changes"].Items) Changes.Add(new Eff(e));
        }
    }
    public sealed class Also
    {
        public readonly List<Cond> When = new List<Cond>(); public readonly List<Eff> Changes = new List<Eff>();
        public Also(JNode n) { n.Only("when", "changes"); foreach (var c in n["when"].Items) When.Add(new Cond(c)); foreach (var e in n["changes"].Items) Changes.Add(new Eff(e)); }
    }
    public sealed class Witness
    {
        public readonly List<Cond> When = new List<Cond>(); public readonly string Act;
        public Witness(JNode n) { n.Only("when", "act"); if (n.Opt("when") is JNode w) foreach (var c in w.Items) When.Add(new Cond(c)); Act = n["act"].Str(); }
    }
    public sealed class Event
    {
        public readonly string Id, By, Phase;
        public readonly List<Cond> Needs = new List<Cond>(); public readonly List<Line> Price = new List<Line>();
        public readonly List<ROutcome> Outcomes = new List<ROutcome>(); public readonly List<Also> Also = new List<Also>(); public readonly List<Witness> Witnesses = new List<Witness>();
        public Event(JNode n)
        {
            n.Only("id", "by", "phase", "tag", "src", "proposed", "reason", "needs", "price", "outcomes", "also", "witnesses", "how", "order", "band");
            Id = n["id"].Str(); By = n["by"].Str(); Phase = n["phase"].Str();
            foreach (var c in n["needs"].Items) Needs.Add(new Cond(c));
            if (n.Opt("price") is JNode p) foreach (var l in p.Items) Price.Add(new Line(l));
            foreach (var o in n["outcomes"].Items) Outcomes.Add(new ROutcome(o));
            if (n.Opt("also") is JNode a) foreach (var x in a.Items) Also.Add(new Also(x));
            if (n.Opt("witnesses") is JNode w) foreach (var x in w.Items) Witnesses.Add(new Witness(x));
            if (Outcomes.Count == 0 || Outcomes[Outcomes.Count - 1].When.Count != 0) throw new FormatException($"{Id}: the last outcome must have no conditions");
        }
        public bool IsTurn => Id.StartsWith("turn.", StringComparison.Ordinal);
        public string Approach => IsTurn ? Id.Substring(5) : "";
    }

    public sealed class Temperament
    {
        public string Id = ""; public HashSet<string> RespondsTo = new HashSet<string>(StringComparer.Ordinal), Refuses = new HashSet<string>(StringComparer.Ordinal);
        public bool ReportsApproach; public int[] Start = new int[4];
    }
    public sealed class Approach { public string Id = "", Requires = ""; public int Usd, S, Trail; }
    public sealed class Need { public string Id = "", Requires = ""; public HashSet<string> MetBy = new HashSet<string>(StringComparer.Ordinal); }

    public sealed class Rules
    {
        public static readonly string[] ScaleNames = { "bravery", "resentment", "fear", "loyalty" };
        public const int Bravery = 0, Resentment = 1, Fear = 2, Loyalty = 3;
        static readonly string[] Bands = { "hero", "neutral", "menace" }, Fits = { "suits", "worse", "refused" }, Knows = { "routine", "need", "temperament", "refused" };

        static readonly Dictionary<string, string[]> CondKeys = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["tier"] = new[] { "is" }, ["turned"] = new[] { "is", "of" }, ["known"] = new[] { "what" }, ["requires"] = new[] { "approach" },
            ["need-met-by"] = new[] { "approach" }, ["need-is"] = new[] { "in" }, ["need-met"] = new[] { "is" }, ["reach"] = new[] { "m" },
            ["cash-covers"] = new string[0], ["memory"] = new[] { "act", "has", "this", "of" }, ["fit"] = new[] { "is" }, ["temperament"] = new[] { "in" },
            ["standing"] = new[] { "band" }, ["orders-reached"] = new[] { "is" }, ["reports"] = new string[0], ["reports-approach"] = new string[0],
            ["outcome"] = new[] { "is" }, ["talker"] = new[] { "is" }, ["lies"] = new[] { "is" }, ["holds-fact"] = new[] { "is", "how" },
            ["scale"] = new[] { "scale", "op", "than" }, ["trail"] = new[] { "atLeast" }, ["keeper-position"] = new string[0], ["introduces-along"] = new string[0],
            ["tie"] = new[] { "kinds" }, ["tie-between-holds"] = new[] { "is" }, ["act"] = new[] { "in" }, ["within"] = new[] { "m", "sightline" },
            ["cell"] = new[] { "is" }, ["called"] = new[] { "is" }, ["exposed"] = new[] { "is" }, ["count"] = new[] { "event", "below" },
        };
        static readonly Dictionary<string, string[]> EffKeys = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["charge"] = new[] { "parts" }, ["turn"] = new string[0], ["move"] = new[] { "on" }, ["need-met"] = new string[0], ["learn"] = new[] { "what", "of" },
            ["forget"] = new[] { "what" }, ["remember"] = new[] { "act" }, ["report"] = new string[0], ["expose"] = new string[0],
            ["trail"] = new[] { "add", "per" }, ["standing"] = new[] { "step", "per" }, ["alert"] = new[] { "atLeast", "per" }, ["lead"] = new[] { "from" },
            ["spend-hop"] = new string[0], ["tell"] = new[] { "along", "hops" }, ["lapse-tie"] = new string[0], ["replace-need"] = new[] { "stream" },
            ["schedule"] = new[] { "fight" }, ["debrief"] = new[] { "line" },
        };
        /// <summary>The kinds the engine itself evaluates inside a mission (the interpreted events: scout.*, turn.*; the reports table).</summary>
        static readonly HashSet<string> MissionKinds = new HashSet<string>(StringComparer.Ordinal)
        { "tier", "turned", "known", "requires", "need-met-by", "need-is", "need-met", "reach", "cash-covers", "memory", "fit", "temperament", "standing", "orders-reached", "reports", "reports-approach", "outcome", "talker", "lies", "holds-fact", "scale", "trail" };
        /// <summary>The effects the engine applies inside a mission's interpreted events.</summary>
        static readonly HashSet<string> MissionEffects = new HashSet<string>(StringComparer.Ordinal) { "charge", "turn", "move", "need-met", "learn", "forget", "remember", "report", "debrief" };

        internal static void CheckCond(Cond c)
        {
            if (!CondKeys.TryGetValue(c.K, out var keys)) throw new FormatException($"{c.N.Path}: condition kind '{c.K}' is unknown (unknown = reject)");
            var all = new List<string>(keys) { "k" }; c.N.Only(all.ToArray());
        }
        internal static void CheckEff(Eff e)
        {
            if (!EffKeys.TryGetValue(e.Do, out var keys)) throw new FormatException($"{e.N.Path}: effect kind '{e.Do}' is unknown (unknown = reject)");
            var all = new List<string>(keys) { "do" }; e.N.Only(all.ToArray());
        }

        public readonly JNode Book, People;
        readonly Dictionary<string, long> values = new Dictionary<string, long>(StringComparer.Ordinal);
        readonly Dictionary<string, long> refs = new Dictionary<string, long>(StringComparer.Ordinal);
        readonly Dictionary<string, Event> events = new Dictionary<string, Event>(StringComparer.Ordinal);
        public readonly List<Event> Events = new List<Event>();
        readonly Dictionary<string, Temperament> temps = new Dictionary<string, Temperament>(StringComparer.Ordinal);
        public readonly List<string> TemperamentIds = new List<string>();
        readonly Dictionary<string, Approach> approaches = new Dictionary<string, Approach>(StringComparer.Ordinal);
        public readonly Dictionary<string, Need> Needs = new Dictionary<string, Need>(StringComparer.Ordinal);
        public readonly List<string> NeedIds = new List<string>();
        public readonly HashSet<string> TieKinds = new HashSet<string>(StringComparer.Ordinal);
        public readonly int[] ScaleMin = new int[4], ScaleMax = new int[4];
        public readonly List<string>[] ScaleSteps = new List<string>[4];
        /// <summary>A scale value as its step word (people.rules scales.*.steps: every number reads as a word).</summary>
        public string Step(int scale, int v) => v >= 0 && v < ScaleSteps[scale].Count ? ScaleSteps[scale][v] : v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        public readonly int[] TalkerLeft, TalkerRight; public readonly int LieBraveryAtLeast; public readonly bool LieTalker, LieHelped; public readonly string LieGivesHero, LieGivesOtherwise;
        public readonly List<string> Facts;
        public readonly List<string> KnownBy;
        readonly Dictionary<string, Fit> fitRows = new Dictionary<string, Fit>(StringComparer.Ordinal), fitApproach = new Dictionary<string, Fit>(StringComparer.Ordinal);
        public readonly List<List<Cond>> Reports = new List<List<Cond>>();
        public readonly Dictionary<string, (int scale, int by)[]> Ladders = new Dictionary<string, (int, int)[]>(StringComparer.Ordinal);
        public readonly Dictionary<string, int> Traceable = new Dictionary<string, int>(StringComparer.Ordinal); // +1 hero, −1 menace, 0 none
        public readonly HashSet<string> MemoryActs = new HashSet<string>(StringComparer.Ordinal);
        public readonly Dictionary<string, string> FaceFact = new Dictionary<string, string>(StringComparer.Ordinal);
        public readonly HashSet<string> DebriefLines = new HashSet<string>(StringComparer.Ordinal);
        public readonly string HelpedAct, RefusedAct;
        public readonly int MemoryCap;
        public readonly int[] AlertByIncident; public readonly int PerShot, ViolenceSeen;
        public readonly int StandingStart, StandingStep, HeroAt, MenaceAt;
        public readonly int WitnessRangeCm, WitnessTrail, ApproachReachCm, ListenReachCm, TrailOpen;
        // cell
        public readonly int CellSpeed, RunSpeed, CarryRunSpeed, ActReachCm, LoopS, LoopTrail, TetherCm, RerouteS, RerouteTrail, RerouteAwayS, MisleadS, MisleadTrail, MisleadAwayS, CallTrail, EnterS, TakeS, TakeTrailUnturned, ShotEveryS, FireReachCm, FireRivalsReachCm, ShotTrail, Hits, EnforcerHealth;
        // security
        public readonly int RespWitnessS, RespSeenS, RespUnseenS, RespCivilianS, RespCheckInS, RespRivalsUnseenS, CheckInS, SealS, SecuritySpeed, RivalsSpeed, RivalsDrawnS, RivalsRetreatS, PatrolBreakCm, PatrolPeriodS, PatrolChase, AimS, GuardFireS, PatrolFireS, SecurityFireS, RivalsFireS;
        // perception
        public readonly int NearCm, Eye, DoorReachCm, DoorHalfCm, FacingReachCm, FacingHalfCm, SecReachCm, SecHalfCm, RivReachCm, RivHalfCm, PatrolDepthCm, PatrolHalfBase, PatrolHalfPerMille;
        // the final fight
        public readonly int SafeHostiles, SafeMinus, SafeWarnS, SafeAmbushCm, LoudOfficers, LoudAlert, LoudCivilian, VolleyS, HoldCm; public readonly bool FightTraced;
        // the investigation and between missions
        public readonly int InvestigatorsBase, InvestigatorsPerAlert, InvestigatorHops, WordHops, WordQuietLoyalty, IntroductionsPerContact, NeedWeightScale;
        public readonly Dictionary<string, string> StreamKeys = new Dictionary<string, string>(StringComparer.Ordinal);
        public readonly Dictionary<string, bool> TieIntroduces = new Dictionary<string, bool>(StringComparer.Ordinal);
        public readonly List<string> ScaleStepNames = new List<string>();

        public Rules(string rulebookJson, string peopleJson)
        {
            Book = JNode.Parse(rulebookJson, "rulebook"); People = JNode.Parse(peopleJson, "people");
            var b = Book; var pp = People;
            b.Only("schema", "author", "gdd", "rule", "reads", "tags", "units", "values", "standing", "alert", "fit", "reports", "ladders", "memory", "exposure", "traceable", "cell", "security", "perception", "fight", "investigation", "preview", "streams", "phases", "debrief", "events");
            if (b["schema"].Str() != "suborned.rulebook/1") throw new FormatException("rulebook schema is not suborned.rulebook/1");
            pp.Only("schema", "author", "gdd", "rule", "person", "tiers", "scales", "temperaments", "institutions", "approaches", "needs", "ties", "knowledge", "talker", "positions", "temperamentsBy", "names");
            if (pp["schema"].Str() != "suborned.people.rules/1") throw new FormatException("people rules schema is not suborned.people.rules/1");

            // ---- people rules: what the engine reads, strictly
            for (int i = 0; i < 4; i++)
            {
                var sc = pp["scales"][ScaleNames[i]]; sc.Only("min", "max", "integer", "steps", "to", "src", "proposed", "reason");
                ScaleMin[i] = sc["min"].Int(); ScaleMax[i] = sc["max"].Int();
                if (ScaleMin[i] != 0 || ScaleMax[i] < 1 || ScaleMax[i] > 10) throw new FormatException($"people.scales.{ScaleNames[i]} is {ScaleMin[i]}..{ScaleMax[i]}");
            }
            foreach (var key in pp["scales"].Keys) if (Array.IndexOf(ScaleNames, key) < 0) throw new FormatException($"people.scales.{key} is unknown");
            foreach (var kv in pp["approaches"].Entries)
            {
                if (kv.Key == "src") continue;
                var a = kv.Value; a.Only("requires", "usd", "timeS", "trail", "leaves");
                var ap = new Approach { Id = kv.Key, Requires = a["requires"].Str(), Usd = Bounded(a["usd"], 0, 1_000_000_000), S = Bounded(a["timeS"], 1, 3600), Trail = Bounded(a["trail"], 0, 100) };
                if (ap.Requires != "nothing" && ap.Requires != "routine" && ap.Requires != "need") throw new FormatException($"{a.Path}.requires '{ap.Requires}'");
                approaches[kv.Key] = ap;
            }
            foreach (var kv in pp["temperaments"].Entries)
            {
                var v = kv.Value; v.Only("name", "respondsTo", "refuses", "reportsApproach", "institution", "hero", "menace", "start", "src", "proposed", "reason");
                var t = new Temperament { Id = kv.Key, ReportsApproach = v["reportsApproach"].Bool() };
                foreach (var a in v["respondsTo"].Strs()) t.RespondsTo.Add(Known(approaches, a, "approach", v.Path));
                foreach (var a in v["refuses"].Strs()) t.Refuses.Add(Known(approaches, a, "approach", v.Path));
                var st = v["start"]; st.Only(ScaleNames);
                for (int i = 0; i < 4; i++) { t.Start[i] = st[ScaleNames[i]].Int(); if (t.Start[i] < ScaleMin[i] || t.Start[i] > ScaleMax[i]) throw new FormatException($"{st.Path}.{ScaleNames[i]} is off its scale"); }
                temps[kv.Key] = t; TemperamentIds.Add(kv.Key);
            }
            pp["needs"].Only("rule", "list", "requiresKinds");
            foreach (var kv in pp["needs"]["list"].Entries)
            {
                var v = kv.Value; v.Only("metBy", "help", "requires", "src", "proposed", "reason");
                var n = new Need { Id = kv.Key, Requires = v["requires"].Str() };
                foreach (var a in v["metBy"].Strs()) n.MetBy.Add(Known(approaches, a, "approach", v.Path));
                if (Array.IndexOf(new[] { "nothing", "home", "work", "family-tie", "rival-tie" }, n.Requires) < 0) throw new FormatException($"{v.Path}.requires '{n.Requires}'");
                Needs[kv.Key] = n; NeedIds.Add(kv.Key);
            }
            foreach (var kv in pp["ties"]["kinds"].Entries) { TieKinds.Add(kv.Key); TieIntroduces[kv.Key] = kv.Value["introduces"].Bool(); }
            for (int i = 0; i < 4; i++) { var steps = pp["scales"][ScaleNames[i]].Opt("steps"); ScaleSteps[i] = steps == null ? new List<string>() : steps.Strs(); }
            var tk = pp["talker"]; tk.Only("when", "test", "deterministic", "gives", "facts", "src", "lie");
            tk["test"].Only("left", "op", "right");
            if (tk["test"]["op"].Str() != ">") throw new FormatException("people.talker.test.op is not >");
            TalkerLeft = Scales(tk["test"]["left"].Strs()); TalkerRight = Scales(tk["test"]["right"].Strs());
            Facts = tk["facts"].Strs();
            var lie = tk["lie"]; lie.Only("when", "gives", "src", "proposed", "reason"); lie["when"].Only("talker", "helped", "braveryAtLeast"); lie["gives"].Only("hero", "otherwise");
            LieTalker = lie["when"]["talker"].Bool(); LieHelped = lie["when"]["helped"].Bool(); LieBraveryAtLeast = Bounded(lie["when"]["braveryAtLeast"], 0, ScaleMax[Bravery]);
            LieGivesHero = lie["gives"]["hero"].Str(); LieGivesOtherwise = lie["gives"]["otherwise"].Str();

            // ---- the rulebook's values, bounded by unit (Redline E3, E9)
            foreach (var kv in b["values"].Entries)
            {
                var v = kv.Value; v.Only("v", "unit", "tag", "src", "proposed", "reason");
                var unit = v["unit"].Str(); long x = v["v"].Long();
                var (lo, hi) = unit switch
                {
                    "s" => (1L, 3600L), "usd" => (-1_000_000_000L, 1_000_000_000L), "m" => (0L, 1000L), "cm" => (0L, 100_000L), "cm-per-s" => (1L, 10_000L),
                    "trail" => (0L, 100L), "alert" => (0L, 3L), "standing" => (-1000L, 1000L), "orders" => (0L, 10L), "scale-steps" => (-10L, 10L),
                    "entries" => (1L, 100L), "hops" => (0L, 10L), "count" => (0L, 100L), "per-mille" => (0L, 1_000_000L),
                    _ => throw new FormatException($"{v.Path}.unit '{unit}' is unknown"),
                };
                if (x < lo || x > hi) throw new FormatException($"{v.Path} = {x} is outside {lo}..{hi} for unit {unit}");
                values[kv.Key] = x;
                var tag = v["tag"].Str(); if (tag != "B" && tag != "S" && tag != "L") throw new FormatException($"{v.Path}.tag '{tag}'");
            }
            ResolveAllRefs(b);

            // ---- sections, strictly, into fields
            var s = b["standing"]; s.Only("start", "step", "hero", "menace", "rule", "tag", "src", "proposed", "reason");
            StandingStart = RI(s["start"]); StandingStep = RI(s["step"]); HeroAt = RI(s["hero"]); MenaceAt = RI(s["menace"]);
            var al = b["alert"]; al.Only("levels", "byIncident", "perShot", "violenceSeen", "rule", "tag", "src");
            var by = al["byIncident"].Items; AlertByIncident = new int[by.Count]; for (int i = 0; i < by.Count; i++) AlertByIncident[i] = RI(by[i]);
            if (AlertByIncident.Length == 0) throw new FormatException("alert.byIncident is empty");
            PerShot = RI(al["perShot"]); ViolenceSeen = RI(al["violenceSeen"]);
            var fit = b["fit"]; fit.Only("from", "approaches", "standing", "rule", "tag", "src", "proposed", "reason");
            fit["from"].Only("suits", "refused", "otherwise");
            foreach (var kv in fit["approaches"].Entries) { kv.Value.Only("fit", "tag", "src", "proposed", "reason"); Known(approaches, kv.Key, "approach", kv.Value.Path); fitApproach[kv.Key] = ParseFit(kv.Value["fit"]); }
            foreach (var r in fit["standing"].Items)
            {
                r.Only("band", "temperament", "approaches", "fit", "tag", "src", "proposed", "reason");
                var band = OneOf(r["band"], Bands); var t = Known(temps, r["temperament"].Str(), "temperament", r.Path); var f = ParseFit(r["fit"]);
                foreach (var a in r["approaches"].Strs()) { var key = band + "/" + t + "/" + Known(approaches, a, "approach", r.Path); if (fitRows.ContainsKey(key)) throw new FormatException($"fit.standing: {key} twice"); fitRows[key] = f; }
            }
            var mem = b["memory"]; mem.Only("entry", "cap", "whenFull", "faceFacts", "acts", "reads", "rule", "tag", "src");
            MemoryCap = RI(mem["cap"]);
            foreach (var kv in mem["acts"].Entries)
            {
                kv.Value.Only("facts"); MemoryActs.Add(kv.Key);
                var f = kv.Value["facts"]; if (f.Kind == JKind.Str) { if (f.Str() != "as-told") throw new FormatException($"{f.Path}"); } else foreach (var x in f.Strs()) if (!Facts.Contains(x)) throw new FormatException($"{f.Path}: fact '{x}' is not in people.rules talker.facts");
            }
            foreach (var kv in mem["faceFacts"].Entries) { var fact = kv.Value.Str(); if (!Facts.Contains(fact)) throw new FormatException($"memory.faceFacts.{kv.Key}: fact '{fact}' is unknown"); FaceFact[kv.Key] = fact; }
            mem["reads"].Only("helped", "refused");
            HelpedAct = KnownAct(mem["reads"]["helped"]); RefusedAct = KnownAct(mem["reads"]["refused"]);
            b["exposure"].Only("rule", "tag", "src", "proposed", "reason");
            foreach (var kv in b["traceable"].Entries)
            {
                kv.Value.Only("standing", "tag", "src", "proposed", "reason");
                Traceable[kv.Key] = OneOf(kv.Value["standing"], new[] { "hero", "menace", "none" }) switch { "hero" => 1, "menace" => -1, _ => 0 };
            }
            var lad = b["ladders"]; lad.Only("rule", "moves", "tag", "src");
            foreach (var kv in lad["moves"].Entries)
            {
                kv.Value.Only("add", "tag", "src", "proposed", "reason");
                var list = new List<(int, int)>();
                foreach (var a in kv.Value["add"].Items) { a.Only("scale", "by"); list.Add((ScaleIndex(a["scale"].Str()), RI(a["by"]))); }
                Ladders[kv.Key] = list.ToArray();
            }
            foreach (var kv in b["debrief"]["lines"].Entries) { kv.Value.Only("fields", "src"); DebriefLines.Add(kv.Key); }
            b["debrief"].Only("lines", "rule", "tag", "src");
            WitnessRangeCm = VI("witness-range-m") * 100; WitnessTrail = VI("witness-trail"); ApproachReachCm = VI("approach-reach-m") * 100; ListenReachCm = VI("listen-reach-m") * 100; TrailOpen = VI("trail-open");

            var c = b["cell"]; c.Only("speed", "runSpeed", "carryRunSpeed", "reach", "loop", "reroute", "mislead", "call", "enter", "take", "fire", "rule", "tag", "src");
            CellSpeed = RI(c["speed"]); RunSpeed = RI(c["runSpeed"]); CarryRunSpeed = RI(c["carryRunSpeed"]); ActReachCm = RI(c["reach"]) * 100;
            c["loop"].Only("s", "trail", "tether"); LoopS = RI(c["loop"]["s"]); LoopTrail = RI(c["loop"]["trail"]); TetherCm = RI(c["loop"]["tether"]) * 100;
            c["reroute"].Only("s", "trail", "away"); RerouteS = RI(c["reroute"]["s"]); RerouteTrail = RI(c["reroute"]["trail"]); RerouteAwayS = RI(c["reroute"]["away"]);
            c["mislead"].Only("s", "trail", "away"); MisleadS = RI(c["mislead"]["s"]); MisleadTrail = RI(c["mislead"]["trail"]); MisleadAwayS = RI(c["mislead"]["away"]);
            c["call"].Only("trail"); CallTrail = RI(c["call"]["trail"]);
            c["enter"].Only("s"); EnterS = RI(c["enter"]["s"]);
            c["take"].Only("s", "trailUnturned"); TakeS = RI(c["take"]["s"]); TakeTrailUnturned = RI(c["take"]["trailUnturned"]);
            var fi = c["fire"]; fi.Only("every", "reach", "reachRivals", "trail", "hits", "enforcerHealth");
            ShotEveryS = RI(fi["every"]); FireReachCm = RI(fi["reach"]) * 100; FireRivalsReachCm = RI(fi["reachRivals"]) * 100; ShotTrail = RI(fi["trail"]); Hits = RI(fi["hits"]); EnforcerHealth = RI(fi["enforcerHealth"]);
            if (Hits < 1 || EnforcerHealth < 1) throw new FormatException("cell.fire: hits and enforcerHealth must be at least 1");
            var se = b["security"]; se.Only("response", "checkIn", "seal", "speed", "rivals", "patrol", "aim", "fireEvery", "rule", "tag", "src");
            var re = se["response"]; re.Only("witness", "seen", "unseen", "civilian", "checkIn", "rivalsUnseen");
            RespWitnessS = RI(re["witness"]); RespSeenS = RI(re["seen"]); RespUnseenS = RI(re["unseen"]); RespCivilianS = RI(re["civilian"]); RespCheckInS = RI(re["checkIn"]); RespRivalsUnseenS = RI(re["rivalsUnseen"]);
            CheckInS = RI(se["checkIn"]); SealS = RI(se["seal"]); SecuritySpeed = RI(se["speed"]);
            se["rivals"].Only("speed", "drawn", "retreat"); RivalsSpeed = RI(se["rivals"]["speed"]); RivalsDrawnS = RI(se["rivals"]["drawn"]); RivalsRetreatS = RI(se["rivals"]["retreat"]);
            se["patrol"].Only("breakTo", "period", "chase"); PatrolBreakCm = RI(se["patrol"]["breakTo"]) * 100; PatrolPeriodS = RI(se["patrol"]["period"]); PatrolChase = RI(se["patrol"]["chase"]);
            AimS = RI(se["aim"]);
            se["fireEvery"].Only("guard", "patrol", "security", "rivals"); GuardFireS = RI(se["fireEvery"]["guard"]); PatrolFireS = RI(se["fireEvery"]["patrol"]); SecurityFireS = RI(se["fireEvery"]["security"]); RivalsFireS = RI(se["fireEvery"]["rivals"]);
            var pe = b["perception"]; pe.Only("near", "eye", "door", "facing", "security", "rivals", "patrol", "rule", "tag", "src");
            NearCm = RI(pe["near"]) * 100; Eye = RI(pe["eye"]);
            pe["door"].Only("reach", "half"); DoorReachCm = RI(pe["door"]["reach"]) * 100; DoorHalfCm = RI(pe["door"]["half"]) * 100;
            pe["facing"].Only("reach", "half"); FacingReachCm = RI(pe["facing"]["reach"]) * 100; FacingHalfCm = RI(pe["facing"]["half"]) * 100;
            pe["security"].Only("reach", "half"); SecReachCm = RI(pe["security"]["reach"]) * 100; SecHalfCm = RI(pe["security"]["half"]) * 100;
            pe["rivals"].Only("reach", "half"); RivReachCm = RI(pe["rivals"]["reach"]) * 100; RivHalfCm = RI(pe["rivals"]["half"]) * 100;
            pe["patrol"].Only("depth", "halfBase", "halfPerMille"); PatrolDepthCm = RI(pe["patrol"]["depth"]) * 100; PatrolHalfBase = RI(pe["patrol"]["halfBase"]); PatrolHalfPerMille = RI(pe["patrol"]["halfPerMille"]);
            var fg = b["fight"]; fg.Only("safe", "loud", "volley", "hold", "traced", "tracedReason", "rule", "tag", "src");
            fg["safe"].Only("hostiles", "minus", "warn", "ambush"); SafeHostiles = RI(fg["safe"]["hostiles"]); SafeMinus = RI(fg["safe"]["minus"]); SafeWarnS = RI(fg["safe"]["warn"]); SafeAmbushCm = RI(fg["safe"]["ambush"]) * 100;
            fg["loud"].Only("officers", "alert", "civilian"); LoudOfficers = RI(fg["loud"]["officers"]); LoudAlert = RI(fg["loud"]["alert"]); LoudCivilian = RI(fg["loud"]["civilian"]);
            VolleyS = RI(fg["volley"]); HoldCm = RI(fg["hold"]) * 100; FightTraced = fg["traced"].Bool();
            var inv = b["investigation"]; inv.Only("base", "perAlert", "hops", "rule", "tag", "src", "proposed", "reason");
            InvestigatorsBase = RI(inv["base"]); InvestigatorsPerAlert = RI(inv["perAlert"]); InvestigatorHops = RI(inv["hops"]);
            WordHops = VI("word-hops"); WordQuietLoyalty = VI("word-quiet-loyalty"); IntroductionsPerContact = VI("introductions-per-contact"); NeedWeightScale = VI("need-weight-scale");
            foreach (var kv in b["streams"]["keys"].Entries) StreamKeys[kv.Key] = kv.Value.Str();

            // ---- events: parsed, every id checked, every kind where it may stand
            foreach (var e in b["events"].Items) { var ev = new Event(e); if (events.ContainsKey(ev.Id)) throw new FormatException($"event {ev.Id} repeats"); events[ev.Id] = ev; Events.Add(ev); }
            foreach (var r in b["reports"].Items)
            {
                r.Only("id", "when", "tag", "src", "proposed", "reason");
                var list = new List<Cond>(); foreach (var x in r["when"].Items) list.Add(new Cond(x));
                foreach (var x in list) { ValidateCond(x, null, "reports"); if (x.K == "cash-covers" || x.K == "orders-reached" || x.K == "reports" || x.K == "outcome") throw new FormatException($"{x.N.Path}: '{x.K}' may not stand in the reports table"); if (!MissionKinds.Contains(x.K)) throw new FormatException($"{x.N.Path}: '{x.K}' is not evaluated in a mission"); }
                Reports.Add(list);
            }
            foreach (var ev in Events) ValidateEvent(ev);
            var pv = b["preview"]; pv.Only("rule", "unknown", "knownBy", "knownByRule", "tag", "src");
            KnownBy = pv["knownBy"].Strs(); foreach (var k in KnownBy) Ev(k);
            var ph = b["phases"]; ph.Only("mission-start", "between-missions", "iterate", "tag", "src");
            foreach (var k in ph["mission-start"].Strs()) Ev(k); foreach (var k in ph["between-missions"].Strs()) Ev(k);
            var stm = b["streams"]; stm.Only("hash", "generator", "draw", "keys", "weightScale", "weights", "rule", "tag", "src");
        }

        static int Bounded(JNode n, long lo, long hi) { long v = n.Long(); if (v < lo || v > hi) throw new FormatException($"{n.Path} = {v} is outside {lo}..{hi}"); return (int)v; }
        static string Known<T>(Dictionary<string, T> d, string id, string what, string where) => d.ContainsKey(id) ? id : throw new FormatException($"{where}: {what} '{id}' is unknown (unknown = reject)");
        string KnownAct(JNode n) { var a = n.Str(); if (!MemoryActs.Contains(a)) throw new FormatException($"{n.Path}: memory act '{a}' is unknown"); return a; }
        static string OneOf(JNode n, string[] allowed) { var v = n.Str(); if (Array.IndexOf(allowed, v) < 0) throw new FormatException($"{n.Path} = '{v}' is not one of {string.Join(", ", allowed)}"); return v; }
        static Fit ParseFit(JNode n) => OneOf(n, Fits) switch { "suits" => Fit.Suits, "worse" => Fit.Worse, _ => Fit.Refused };
        static int[] Scales(List<string> names) { var o = new int[names.Count]; for (int i = 0; i < o.Length; i++) o[i] = ScaleIndex(names[i]); return o; }
        int RI(JNode n) { if (n.Kind != JKind.Str) throw new FormatException($"{n.Path} is not a ref"); return (int)Num(n.Str()); }

        void ValidateEvent(Event ev)
        {
            bool interpreted = ev.Id.StartsWith("turn.", StringComparison.Ordinal) || ev.Id.StartsWith("scout.", StringComparison.Ordinal);
            bool choosesOnly = ev.Id == "investigator.question"; // the engine reads its needs and picks its outcome; its effects are the investigation's own
            if (ev.IsTurn) Known(approaches, ev.Approach, "approach", ev.Id);
            var outs = new HashSet<string>(StringComparer.Ordinal); foreach (var o in ev.Outcomes) if (!outs.Add(o.Id)) throw new FormatException($"{ev.Id}: outcome {o.Id} repeats");
            void Check(IEnumerable<Cond> cs, string part)
            {
                foreach (var c in cs)
                {
                    ValidateCond(c, ev, part, outs);
                    if (c.K == "cash-covers" && part != "needs") throw new FormatException($"{c.N.Path}: cash-covers may stand only in an event's needs");
                    if (part == "price" && (c.K == "orders-reached" || c.K == "reports" || c.K == "outcome")) throw new FormatException($"{c.N.Path}: '{c.K}' may not price a line");
                    if (c.K == "outcome" && (part == "needs" || part == "outcomes")) throw new FormatException($"{c.N.Path}: outcome stands only in also and witnesses");
                    if ((c.K == "fit" || c.K == "orders-reached") && !ev.IsTurn) throw new FormatException($"{c.N.Path}: '{c.K}' reads a Turn's approach");
                    if ((interpreted || (choosesOnly && part != "also")) && !MissionKinds.Contains(c.K)) throw new FormatException($"{c.N.Path}: '{c.K}' is not evaluated in a mission event");
                }
            }
            Check(ev.Needs, "needs");
            foreach (var l in ev.Price)
            {
                Check(l.When, "price");
                l.UsdV = l.Usd != null ? Num(l.Usd) : 0; l.SV = l.S != null ? Num(l.S) : 0; l.TrailV = l.Trail != null ? Num(l.Trail) : 0; l.OrdersV = l.Orders != null ? Num(l.Orders) : 0;
            }
            if (ev.Price.Count > 0 && ev.Price[0].Id != "base") throw new FormatException($"{ev.Id}: the first price line is the base");
            foreach (var o in ev.Outcomes) { Check(o.When, "outcomes"); foreach (var e in o.Changes) ValidateEff(e, ev, interpreted); }
            foreach (var a in ev.Also) { Check(a.When, "also"); foreach (var e in a.Changes) ValidateEff(e, ev, interpreted); }
            foreach (var w in ev.Witnesses) { Check(w.When, "witnesses"); if (!Traceable.ContainsKey(w.Act)) throw new FormatException($"{ev.Id}: witness act '{w.Act}' is not traceable"); }
        }

        void ValidateCond(Cond c, Event? ev, string part, HashSet<string>? outs = null)
        {
            var n = c.N; string w = n.Path;
            void Bool(string k) { if (n.Has(k)) n[k].Bool(); }
            switch (c.K)
            {
                case "tier": OneOf(n["is"], new[] { "simulated", "background" }); break;
                case "turned": Bool("is"); if (n.Has("of")) OneOf(n["of"], new[] { "friend" }); break;
                case "known": OneOf(n["what"], Knows); break;
                case "requires": case "need-met-by": Known(approaches, c.S("approach"), "approach", w); break;
                case "need-is": foreach (var x in c.L("in")) Known(Needs, x, "need", w); break;
                case "need-met": case "orders-reached": case "talker": case "lies": case "cell": case "called": case "exposed": case "tie-between-holds": Bool("is"); break;
                case "reach": case "within": Num(c.S("m")); if (c.K == "within") OneOf(n["sightline"], new[] { "3d" }); break;
                case "memory": if (!MemoryActs.Contains(c.S("act"))) throw new FormatException($"{w}: memory act '{c.S("act")}' is unknown"); Bool("has"); if (n.Has("this")) OneOf(n["this"], new[] { "mission" }); if (n.Has("of")) OneOf(n["of"], new[] { "friend" }); break;
                case "fit": foreach (var x in c.L("is")) if (Array.IndexOf(Fits, x) < 0) throw new FormatException($"{w}: fit '{x}' is unknown"); break;
                case "temperament": foreach (var x in c.L("in")) Known(temps, x, "temperament", w); break;
                case "standing": foreach (var x in c.L("band")) if (Array.IndexOf(Bands, x) < 0) throw new FormatException($"{w}: band '{x}' is unknown"); break;
                case "outcome": foreach (var x in c.L("is")) if (outs == null || !outs.Contains(x)) throw new FormatException($"{w}: outcome '{x}' is not one of {ev?.Id}'s"); break;
                case "holds-fact": Bool("is"); if (n.Has("how")) OneOf(n["how"], new[] { "any", "witnessed" }); break;
                case "scale":
                    ScaleIndex(c.S("scale")); OneOf(n["op"], new[] { "<", "<=", "==", ">=", ">" });
                    var than = n["than"]; if (than.Kind == JKind.Str) Num(than.Str()); else { than.Only("scale"); ScaleIndex(than["scale"].Str()); }
                    break;
                case "trail": Num(c.S("atLeast")); break;
                case "tie": foreach (var x in c.L("kinds")) if (!TieKinds.Contains(x)) throw new FormatException($"{w}: tie kind '{x}' is unknown"); break;
                case "act": foreach (var x in c.L("in")) if (!Traceable.ContainsKey(x)) throw new FormatException($"{w}: act '{x}' is not traceable"); break;
                case "count": Ev(c.S("event")); Num(c.S("below")); break;
            }
        }

        void ValidateEff(Eff e, Event ev, bool interpreted)
        {
            var n = e.N; string w = n.Path;
            if (interpreted && !MissionEffects.Contains(e.Do)) throw new FormatException($"{w}: effect '{e.Do}' is not applied in a mission event");
            switch (e.Do)
            {
                case "charge": foreach (var x in e.L("parts")) if (x != "usd" && x != "s" && x != "trail") throw new FormatException($"{w}: charge part '{x}'"); break;
                case "move": if (!Ladders.ContainsKey(e.S("on"))) throw new FormatException($"{w}: move '{e.S("on")}' is unknown"); break;
                case "learn": foreach (var x in e.L("what")) OneOfStr(x, Knows, w); OneOf(n["of"], new[] { "subject", "needs-known", "friend" }); if (interpreted && e.S("of") != "subject") throw new FormatException($"{w}: a mission event learns of its subject"); if (e.L("what").Contains("refused") && !ev.IsTurn) throw new FormatException($"{w}: only a Turn learns a refusal"); break;
                case "forget": foreach (var x in e.L("what")) OneOfStr(x, Knows, w); break;
                case "remember": if (!MemoryActs.Contains(e.S("act"))) throw new FormatException($"{w}: memory act '{e.S("act")}' is unknown"); break;
                case "trail": Num(e.S("add")); OneOf(n["per"], new[] { "incident" }); break;
                case "standing": Num(e.S("step")); OneOf(n["per"], new[] { "incident" }); break;
                case "alert": Num(e.S("atLeast")); OneOf(n["per"], new[] { "incident" }); break;
                case "lead": OneOf(n["from"], new[] { "facts", "lie" }); break;
                case "tell": foreach (var x in e.L("along")) if (!TieKinds.Contains(x)) throw new FormatException($"{w}: tie kind '{x}' is unknown"); Num(e.S("hops")); break;
                case "replace-need": if (Book["streams"]["keys"].Opt(e.S("stream")) == null) throw new FormatException($"{w}: stream '{e.S("stream")}' is unknown"); break;
                case "schedule": OneOf(n["fight"], new[] { "ambush" }); break;
                case "debrief": if (!DebriefLines.Contains(e.S("line"))) throw new FormatException($"{w}: debrief line '{e.S("line")}' is unknown"); break;
            }
        }
        static void OneOfStr(string v, string[] allowed, string where) { if (Array.IndexOf(allowed, v) < 0) throw new FormatException($"{where}: '{v}' is not one of {string.Join(", ", allowed)}"); }

        void ResolveAllRefs(JNode n)
        {
            switch (n.Kind)
            {
                case JKind.Str: var s = n.Str(); if (s.Length > 2 && (s.StartsWith("v:", StringComparison.Ordinal) || s.StartsWith("p:", StringComparison.Ordinal)) && IsRef(s)) Num(s); break;
                case JKind.Arr: foreach (var x in n.Items) ResolveAllRefs(x); break;
                case JKind.Obj: foreach (var kv in n.Entries) ResolveAllRefs(kv.Value); break;
            }
        }
        static bool IsRef(string s)
        {
            for (int i = 2; i < s.Length; i++) { char c = s[i]; if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '-' || c == '.')) return false; }
            return true;
        }

        /// <summary>A value of the rulebook by id ("loop-s").</summary>
        public long V(string id) => values.TryGetValue(id, out var v) ? v : throw new FormatException($"rulebook has no value '{id}'");
        public int VI(string id) => (int)V(id);
        /// <summary>A v: or p: ref to its integer; every ref in the rulebook is resolved at load, so in play this is a lookup.</summary>
        public long Num(string reff)
        {
            if (refs.TryGetValue(reff, out var hit)) return hit;
            long v;
            if (reff.StartsWith("v:", StringComparison.Ordinal)) v = V(reff.Substring(2));
            else if (reff.StartsWith("p:", StringComparison.Ordinal))
            {
                JNode x = People;
                foreach (var part in reff.Substring(2).Split('.')) { var y = x.Opt(part); if (y == null) throw new FormatException($"{reff} does not resolve in people.rules.json"); x = y; }
                v = x.Long();
            }
            else throw new FormatException($"'{reff}' is not a ref");
            if (v < -1_000_000_000L || v > 1_000_000_000L) throw new FormatException($"{reff} = {v} is out of range");
            refs[reff] = v; return v;
        }

        public Event Ev(string id) => events.TryGetValue(id, out var e) ? e : throw new FormatException($"rulebook has no event '{id}'");
        public bool HasEvent(string id) => events.ContainsKey(id);
        public Temperament Temp(string id) => temps.TryGetValue(id, out var t) ? t : throw new FormatException($"temperament '{id}' is unknown (unknown = reject)");
        public bool HasTemperament(string id) => temps.ContainsKey(id);
        public Approach App(string id) => approaches.TryGetValue(id, out var a) ? a : throw new FormatException($"approach '{id}' is unknown (unknown = reject)");
        public bool HasApproach(string id) => approaches.ContainsKey(id);
        public string ApproachRequires(string a) => App(a).Requires;
        public bool NeedMetBy(string need, string approach) => Needs.TryGetValue(need, out var n) && n.MetBy.Contains(approach);
        public void CheckNeed(string need) { if (!Needs.ContainsKey(need)) throw new FormatException($"need '{need}' is unknown (unknown = reject)"); }

        public Band BandOf(int standing) => standing >= HeroAt ? Band.Hero : standing <= MenaceAt ? Band.Menace : Band.Neutral;
        public static string BandId(Band b) => b == Band.Hero ? "hero" : b == Band.Menace ? "menace" : "neutral";

        /// <summary>Sky Q4: a standing row wins, then an approach row, then respondsTo (suits) / refuses (refused) / otherwise worse.</summary>
        public Fit FitOf(string approach, string temperament, Band band)
        {
            if (fitRows.TryGetValue(BandId(band) + "/" + temperament + "/" + approach, out var f)) return f;
            if (fitApproach.TryGetValue(approach, out var a)) return a;
            var t = Temp(temperament);
            if (t.Refuses.Contains(approach)) return Fit.Refused;
            if (t.RespondsTo.Contains(approach)) return Fit.Suits;
            return Fit.Worse;
        }

        /// <summary>§04 Talker test, read from people.rules: sum(left) > sum(right).</summary>
        public bool IsTalker(int[] sc)
        {
            int l = 0, r = 0;
            foreach (var i in TalkerLeft) l += sc[i];
            foreach (var i in TalkerRight) r += sc[i];
            return l > r;
        }
        public static int ScaleIndex(string s) { int i = Array.IndexOf(ScaleNames, s); if (i < 0) throw new FormatException($"scale '{s}' is unknown"); return i; }
        public int Clamp(int scale, int v) => Math.Max(ScaleMin[scale], Math.Min(ScaleMax[scale], v));
    }
}
