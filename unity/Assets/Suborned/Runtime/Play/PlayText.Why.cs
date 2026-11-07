using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Suborned.Sim;

namespace Suborned.Play
{
    /// <summary>Brief 100 (Sky 2026-11-07, playing: "It would be good to explain why an alert raised, or why police came"; Fable's words,
    /// briefs/100/fable/words.md): text.json's `why` block, read by key. The cause of every Alert rise (`alert`, keyed by the engine's ledger
    /// cause through WhyKeyOf) and every police call (`police`), the reporter while a report is still on its way (`onWay`), and the Alert
    /// meter's list (`meter`). The words are Fable's and may change: the host reads them by key and never keeps a copy. A text with no `why`
    /// block shows no why (HasWhy false), never a guess. WhyKeyOf, Words and FillWhy are ports of why.schema.ts's whyKeyOf, whyWords and fillWhy.</summary>
    public sealed partial class PlayText
    {
        public sealed class WhyText { public string Title = "", Short = "", Line = ""; }
        public sealed class WhyCause { public readonly WhyText Base = new(); public readonly Dictionary<string, WhyText> Variants = new(); }
        /// <summary>`by` (a reporter who is no person: camera, robot, police, desk, round, witness, another), `places` ({where}), `effect` (by Alert), `onWay.to` ({to}), `meter`.</summary>
        public readonly Dictionary<string, string> WhyBy = new(), WhyPlaces = new(), WhyEffect = new(), OnWayTo = new(), WhyMeter = new();
        public readonly Dictionary<string, WhyCause> WhyAlert = new(), WhyPolice = new();
        /// <summary>`onWay.ways`: how a report travels (radio, walk, run, look, camera, screen, robot, checkin, tell): its plate and its line.</summary>
        public readonly Dictionary<string, (string plate, string line)> OnWayWays = new();
        public string WhyAgainShort = "", WhyAgainLine = "", OnWayFix = "", OnWayNofix = "", OnWayStoppedPlate = "", OnWayStoppedLine = "";
        /// <summary>The text has Fable's why block: the host words the causes; without it, it shows none.</summary>
        public bool HasWhy => WhyAlert.Count > 0;

        void ReadWhy(JNode w)
        {
            if (w == null || w.Kind != JKind.Obj) return;
            static void Strs(JNode n, Dictionary<string, string> into) { if (n != null && n.Kind == JKind.Obj) foreach (var kv in n.Entries) if (kv.Value.Kind == JKind.Str) into[kv.Key] = kv.Value.Str(); }
            static WhyText Words(JNode n) => new() { Title = S(n, "title") ?? "", Short = S(n, "short") ?? "", Line = S(n, "line") ?? "" };
            static void Causes(JNode n, Dictionary<string, WhyCause> into)
            {
                if (n == null || n.Kind != JKind.Obj) return;
                foreach (var kv in n.Entries)
                {
                    if (kv.Value.Kind != JKind.Obj) continue;
                    var c = new WhyCause(); var b = Words(kv.Value); c.Base.Title = b.Title; c.Base.Short = b.Short; c.Base.Line = b.Line;
                    if (kv.Value.Opt("variants") is JNode vs && vs.Kind == JKind.Obj) foreach (var v in vs.Entries) if (v.Value.Kind == JKind.Obj) c.Variants[v.Key] = Words(v.Value);
                    into[kv.Key] = c;
                }
            }
            Strs(w.Opt("by"), WhyBy); Strs(w.Opt("places"), WhyPlaces); Strs(w.Opt("effect"), WhyEffect); Strs(w.Opt("meter"), WhyMeter); WhyMeter.Remove("rule");
            Causes(w.Opt("alert"), WhyAlert); Causes(w.Opt("police"), WhyPolice);
            if (w.Opt("again") is JNode ag) { WhyAgainShort = S(ag, "short") ?? ""; WhyAgainLine = S(ag, "line") ?? ""; }
            if (w.Opt("onWay") is JNode ow && ow.Kind == JKind.Obj)
            {
                Strs(ow.Opt("to"), OnWayTo);
                if (ow.Opt("ways") is JNode wy && wy.Kind == JKind.Obj) foreach (var kv in wy.Entries) if (kv.Value.Kind == JKind.Obj) OnWayWays[kv.Key] = (S(kv.Value, "plate") ?? "", S(kv.Value, "line") ?? "");
                OnWayFix = S(ow, "fix") ?? ""; OnWayNofix = S(ow, "nofix") ?? "";
                if (ow.Opt("stopped") is JNode st) { OnWayStoppedPlate = S(st, "plate") ?? ""; OnWayStoppedLine = S(st, "line") ?? ""; }
            }
        }

        /// <summary>why.schema.ts ALERT_CAUSES: every Alert cause Fable has words for.</summary>
        public static readonly string[] AlertCauses = { "seen", "carry", "dead", "down", "woken", "noise", "mark", "missing", "cut", "approach", "staff", "let-go", "confirmed",
            "gunfire", "violence", "drawn", "drawn-people", "warned", "fought", "loiter", "recorded", "car", "entry", "sighting", "start" };
        static readonly Dictionary<string, string> WhyHeads = new() { ["confirmed"] = "confirmed", ["gunfire"] = "gunfire", ["violence-seen"] = "violence", ["drawn-warned"] = "warned",
            ["takedown-fails"] = "fought", ["loiter"] = "loiter", ["recorded"] = "recorded", ["car-reported"] = "car", ["entry"] = "entry", ["sighting"] = "sighting" };

        /// <summary>The `alert` key for an engine ledger cause (Game.Ledger, what "alert"), or null for a cause with no words (a new engine cause):
        /// report:&lt;kind&gt;:&lt;who&gt; by its kind (report:cut:camera:&lt;id&gt; is cut); report:&lt;role&gt; an approach reported, except the Office staff's and
        /// Popovic's reports of a cancelled Enter and Take; drawn-seen:people is drawn-people; then the cause's first part.</summary>
        public static string WhyKeyOf(string cause)
        {
            if (string.IsNullOrEmpty(cause)) return null;
            var p = cause.Split(':'); string head = p[0];
            if (head == "report")
            {
                if (p.Length == 2) return p[1] == "office-staff" ? "staff" : p[1] == "councillor" ? "let-go" : "approach";
                return p.Length > 2 && Array.IndexOf(AlertCauses, p[1]) >= 0 ? p[1] : null;
            }
            if (head == "drawn-seen") return p.Length > 1 && p[1] == "people" ? "drawn-people" : "drawn";
            return WhyHeads.TryGetValue(head, out var k) ? k : null;
        }

        /// <summary>A cause's words, a variant's over its base (whyWords); null when the text has no such cause.</summary>
        public WhyText Words(string group, string key, string variant = null)
        {
            var d = group == "police" ? WhyPolice : WhyAlert;
            if (key == null || !d.TryGetValue(key, out var c)) return null;
            WhyText v = variant != null && c.Variants.TryGetValue(variant, out var x) ? x : null;
            return new WhyText { Title = Or(v?.Title, c.Base.Title), Short = Or(v?.Short, c.Base.Short), Line = Or(v?.Line, c.Base.Line) };
            static string Or(string a, string b) => string.IsNullOrEmpty(a) ? b : a;
        }

        static readonly Regex WhyFill = new(@"\{([a-z]+)\}", RegexOptions.Compiled);
        /// <summary>Fill a why text (fillWhy): a fill with no value (or an empty one) stays as written; `cap` capitalises the first letter (a title or a line; a short stays lower case).</summary>
        public static string FillWhy(string s, IReadOnlyDictionary<string, string> v, bool cap = true)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var o = WhyFill.Replace(s, m => v != null && v.TryGetValue(m.Groups[1].Value, out var x) && !string.IsNullOrEmpty(x) ? x : m.Value); // an empty fill is an unknown one
            return cap && o.Length > 0 ? char.ToUpperInvariant(o[0]) + o[1..] : o;
        }
        /// <summary>A why text still carries a fill nothing filled ("{who}"): the host shows nothing rather than a guess.</summary>
        public static bool Unfilled(string s) => !string.IsNullOrEmpty(s) && WhyFill.IsMatch(s);
    }
}
