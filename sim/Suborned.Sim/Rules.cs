// The rules, read as data: contracts/rules/rulebook.json (Keel) and contracts/people/people.rules.json (Motive). The engine knows
// the closed set of condition kinds (`k`) and effect kinds (`do`) and nothing else: any other kind, any unknown key in an event,
// any v:/p: ref that does not resolve to an integer is a load error (GDD §01: unknown = reject). The person-level events (Turn,
// Watch, Listen) run straight from their data here; contracts/rules/rulebook.ref.ts is the same logic in TypeScript, and the two
// agree on rulebook.test.ts's cases (Suborned.Sim.Tests RulesTests).
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
    }

    public sealed class Temperament
    {
        public string Id = ""; public HashSet<string> RespondsTo = new HashSet<string>(StringComparer.Ordinal), Refuses = new HashSet<string>(StringComparer.Ordinal);
        public bool ReportsApproach; public int[] Start = new int[4];
    }

    public sealed class Rules
    {
        public static readonly string[] ScaleNames = { "bravery", "resentment", "fear", "loyalty" };
        public const int Bravery = 0, Resentment = 1, Fear = 2, Loyalty = 3;

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
        readonly Dictionary<string, Event> events = new Dictionary<string, Event>(StringComparer.Ordinal);
        public readonly List<Event> Events = new List<Event>();
        readonly Dictionary<string, Temperament> temps = new Dictionary<string, Temperament>(StringComparer.Ordinal);
        public readonly List<string> TemperamentIds = new List<string>();
        public readonly int[] ScaleMin = new int[4], ScaleMax = new int[4];
        public readonly List<string> KnownBy;
        public readonly int MemoryCap;

        public Rules(string rulebookJson, string peopleJson)
        {
            Book = JNode.Parse(rulebookJson, "rulebook"); People = JNode.Parse(peopleJson, "people");
            if (Book["schema"].Str() != "suborned.rulebook/1") throw new FormatException("rulebook schema is not suborned.rulebook/1");
            if (People["schema"].Str() != "suborned.people.rules/1") throw new FormatException("people rules schema is not suborned.people.rules/1");
            foreach (var kv in Book["values"].Entries) values[kv.Key] = kv.Value["v"].Long();
            for (int i = 0; i < 4; i++) { var sc = People["scales"][ScaleNames[i]]; ScaleMin[i] = sc["min"].Int(); ScaleMax[i] = sc["max"].Int(); }
            foreach (var kv in People["temperaments"].Entries)
            {
                var t = new Temperament { Id = kv.Key, ReportsApproach = kv.Value["reportsApproach"].Bool() };
                foreach (var a in kv.Value["respondsTo"].Strs()) t.RespondsTo.Add(a);
                foreach (var a in kv.Value["refuses"].Strs()) t.Refuses.Add(a);
                for (int i = 0; i < 4; i++) t.Start[i] = kv.Value["start"][ScaleNames[i]].Int();
                temps[kv.Key] = t; TemperamentIds.Add(kv.Key);
            }
            foreach (var e in Book["events"].Items) { var ev = new Event(e); if (events.ContainsKey(ev.Id)) throw new FormatException($"event {ev.Id} repeats"); events[ev.Id] = ev; Events.Add(ev); }
            KnownBy = Book["preview"]["knownBy"].Strs();
            MemoryCap = (int)Num(Book["memory"]["cap"].Str());
            ResolveAllRefs(Book);
            foreach (var ev in Events) foreach (var o in ev.Outcomes) foreach (var c in o.Changes) if (c.Do == "move" && Book["ladders"]["moves"].Opt(c.S("on")) == null) throw new FormatException($"{ev.Id}: move {c.S("on")} is unknown");
        }

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
            for (int i = 2; i < s.Length; i++) { char c = s[i]; if (!(char.IsLetterOrDigit(c) || c == '-' || c == '.')) return false; }
            return true;
        }

        /// <summary>A value of the rulebook by id ("loop-s").</summary>
        public long V(string id) => values.TryGetValue(id, out var v) ? v : throw new FormatException($"rulebook has no value '{id}'");
        public int VI(string id) => (int)V(id);
        /// <summary>A v: or p: ref to its integer.</summary>
        public long Num(string reff)
        {
            if (reff.StartsWith("v:", StringComparison.Ordinal)) return V(reff.Substring(2));
            if (!reff.StartsWith("p:", StringComparison.Ordinal)) throw new FormatException($"'{reff}' is not a ref");
            JNode x = People;
            foreach (var part in reff.Substring(2).Split('.')) { var y = x.Opt(part); if (y == null) throw new FormatException($"{reff} does not resolve in people.rules.json"); x = y; }
            return x.Long();
        }
        /// <summary>A section ref of the rulebook, e.g. Ref("cell", "loop", "s") → the integer it names.</summary>
        public long Ref(params string[] path) { JNode x = Book; foreach (var p in path) x = x[p]; return Num(x.Str()); }
        public int RefI(params string[] path) => (int)Ref(path);

        public Event Ev(string id) => events.TryGetValue(id, out var e) ? e : throw new FormatException($"rulebook has no event '{id}'");
        public Temperament Temp(string id) => temps.TryGetValue(id, out var t) ? t : throw new FormatException($"temperament '{id}' is unknown (unknown = reject)");
        public string ApproachRequires(string a) => People["approaches"][a]["requires"].Str();
        public bool NeedMetBy(string need, string approach) => People["needs"]["list"][need]["metBy"].Strs().Contains(approach);
        public void CheckNeed(string need) { if (People["needs"]["list"].Opt(need) == null) throw new FormatException($"need '{need}' is unknown (unknown = reject)"); }

        public Band BandOf(int standing) => standing >= Ref("standing", "hero") ? Band.Hero : standing <= Ref("standing", "menace") ? Band.Menace : Band.Neutral;
        public static string BandId(Band b) => b == Band.Hero ? "hero" : b == Band.Menace ? "menace" : "neutral";

        /// <summary>Sky Q4: a standing row wins, then an approach row, then respondsTo (suits) / refuses (refused) / otherwise worse.</summary>
        public Fit FitOf(string approach, string temperament, Band band)
        {
            foreach (var r in Book["fit"]["standing"].Items)
                if (r["band"].Str() == BandId(band) && r["temperament"].Str() == temperament && r["approaches"].Strs().Contains(approach)) return ParseFit(r["fit"].Str());
            if (Book["fit"]["approaches"].Opt(approach) is JNode a) return ParseFit(a["fit"].Str());
            var t = Temp(temperament);
            if (t.Refuses.Contains(approach)) return Fit.Refused;
            if (t.RespondsTo.Contains(approach)) return Fit.Suits;
            return Fit.Worse;
        }
        static Fit ParseFit(string s) => s == "suits" ? Fit.Suits : s == "worse" ? Fit.Worse : s == "refused" ? Fit.Refused : throw new FormatException($"fit '{s}'");

        /// <summary>§04 Talker test, read from people.rules: sum(left) > sum(right).</summary>
        public bool IsTalker(int[] sc)
        {
            int l = 0, r = 0;
            foreach (var s in People["talker"]["test"]["left"].Strs()) l += sc[ScaleIndex(s)];
            foreach (var s in People["talker"]["test"]["right"].Strs()) r += sc[ScaleIndex(s)];
            return l > r;
        }
        public static int ScaleIndex(string s) { int i = Array.IndexOf(ScaleNames, s); if (i < 0) throw new FormatException($"scale '{s}' is unknown"); return i; }
        public int Clamp(int scale, int v) => Math.Max(ScaleMin[scale], Math.Min(ScaleMax[scale], v));
        public JNode Move(string id) => Book["ladders"]["moves"][id];
        public string HelpedAct => Book["memory"]["reads"]["helped"].Str();
        public string RefusedAct => Book["memory"]["reads"]["refused"].Str();
    }
}
