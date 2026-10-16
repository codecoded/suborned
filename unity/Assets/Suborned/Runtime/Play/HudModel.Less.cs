using System.Collections.Generic;
using System.Text.RegularExpressions;
using Suborned.Sim;

namespace Suborned.Play
{
    /// <summary>Brief 014 (Fable's price grammar, briefs/014/fable-less.md §2; Glyph's icons, glyph-less.md §2): one line per way. The verb's
    /// name, then only the fields that are not zero, in the fixed order name · $ · time · Trail · Alert · eye · learns · pace · mark, each an
    /// icon and a number; an unknown is "?"; the time is the walk plus the act. Which fields an order shows is the text's `less.price.verbs`.
    /// Every number is the engine's quote (Preview, QuoteTakedown, SeenAt, the rulebook), read through the host's Intent: nothing is priced
    /// here. Alt adds the zeros, the walk split from the act, and the takedown's found, wake, door and report numbers as fragments.</summary>
    public sealed partial class HudModel
    {
        public sealed class PField { public string Kind = "", Icon = "", Text = "", Eye = "", Mark = "", Tip = ""; public bool Found; public readonly List<string> Icons = new(); }

        static readonly string[] DefaultFields = { "name", "usd", "time", "trail", "alert", "eye", "mark" };
        static readonly Regex Num = new(@"(\d+)"), Plus = new(@"\+(\d+)"), AtLeast = new(@"≥\s*(\d+)"), Arrow = new(@"→\s*(\d+)"), Speed = new(@"(\d+(?:\.\d+)?) m/s");

        /// <summary>The eye's hover line (Glyph, "Eyes, revised"): one sentence.</summary>
        public static string EyeTip(string eye, string seenBy) => eye == "open" ? $"Seen by {(string.IsNullOrEmpty(seenBy) || seenBy == "?" ? "a watcher" : seenBy)}." : eye == "unk" ? "Unknown. Watch first." : "Unseen.";

        /// <summary>Fable's field order (less.price.order), the price line's columns.</summary>
        public static readonly string[] FieldOrder = { "usd", "time", "trail", "alert", "eye", "learns", "pace", "mark", "hits", "walk", "run" };

        /// <summary>Compact money for a price line (Sky 2026-10-16: the line stays short): 100,000 → 100k, 1,500 → 1.5k, under 1,000 as it is;
        /// a range "40,000–60,000" → "40k–60k". The status pill and the debrief keep whole dollars.</summary>
        public static string CompactUsd(string s) => System.Text.RegularExpressions.Regex.Replace(s ?? "", @"\d{1,3}(,\d{3})+|\d{4,}", mm =>
        {
            long v = long.Parse(mm.Value.Replace(",", "")); if (v < 1000) return mm.Value;
            return v % 1000 == 0 ? $"{v / 1000}k" : $"{v / 1000.0:0.#}k";
        });

        /// <summary>The order's key in less.price.verbs: a turn by its approach, else its verb.</summary>
        public static string PriceVerb(Order o) => o == null ? "" : o.Do == "turn" ? o.Approach : o.Do;

        /// <summary>The verb's name for a row (at most two words): the way for a takedown, else the text's label, shortened where the target is
        /// the sheet's header (Enter the Office → Enter; Vale into the car → Into car).</summary>
        public static string ShortName(HandoverHost h, HandoverHost.Intent i)
        {
            var o = i.Order; if (o == null) return i.Label;
            var t = h.Text;
            if (o.Do == "takedown" && t != null && t.Actions.TryGetValue("takedown", out var td) && td.Ways.TryGetValue(o.How ?? "", out var w)) return w.label;
            if (o.Do == "takedown") return o.How == "kill" ? "Kill" : "Knock out";
            if (o.Do == "enter") return "Enter";
            if (o.Do == "extract") return "Into car";
            if (o.Do == "loop") return "Loop";
            if (o.Do == "stash") return "Hide";
            if (o.Do == "reroute") return "Reroute";
            var label = t?.Action(PriceVerb(o), null).label ?? PriceVerb(o);
            return label;
        }

        /// <summary>A second one-word tag where several rows share a name (the cabinet's loops: entrance, pickup, mast).</summary>
        public static string Tag(HandoverHost.Intent i) => i.Order?.Do == "loop" ? (i.Order.Target is "entrance" or "pickup" ? i.Order.Target : "mast") : "";

        /// <summary>The line's fields for an intent (zero fields hidden), in Fable's order.</summary>
        public static List<PField> PriceLine(HandoverHost h, HandoverHost.Intent i)
        {
            var l = new List<PField>(); var o = i.Order; if (o == null) return l;
            var verb = PriceVerb(o);
            IList<string> fields = h.Text != null && h.Text.PriceVerbs.TryGetValue(verb, out var fs) ? fs : DefaultFields;
            bool td = o.Do == "takedown" && i.Td != null; bool foundSaid = false;
            PField F(string kind, string icon, string text) { var f = new PField { Kind = kind, Icon = icon, Text = text }; l.Add(f); return f; }
            foreach (var k in fields)
                switch (k)
                {
                    case "usd":
                        if (i.Usd.Length > 0 && i.Usd != "$0") F("usd", "p_cash", CompactUsd(i.Usd.TrimStart('$').Replace("–$", "–")));
                        break;
                    case "time":
                        {
                            var m = Num.Match(i.Time ?? ""); if (!m.Success) break;
                            int s = int.Parse(m.Groups[1].Value) + System.Math.Max(0, i.WalkS);
                            F("time", "p_time", $"{s} s");
                            break;
                        }
                    case "trail":
                        {
                            if (o.Do == "fire") { F("trail", "p_trail", $"+{h.Game.R.HitTrail}/hit"); break; }
                            int n = td ? i.Td.TrailIfFound : Plus.Match(i.Trail ?? "") is var pm && pm.Success ? int.Parse(pm.Groups[1].Value) : 0;
                            if (n <= 0) break;
                            var f = F("trail", "p_trail", $"+{n}");
                            if (td && !foundSaid && i.SeenMark != "no") { f.Found = true; foundSaid = true; } // Glyph §2: the found glyph once before the numbers that only happen if the body is found
                            break;
                        }
                    case "alert":
                        {
                            var a = i.Alert ?? ""; string v = null;
                            if (AtLeast.Match(a) is var al && al.Success) v = "≥" + al.Groups[1].Value;
                            else if (Arrow.Match(a) is var ar && ar.Success) v = ar.Groups[1].Value;
                            else if (a.StartsWith("A ") && Num.Match(a) is var an && an.Success && an.Groups[1].Value != "0") v = an.Groups[1].Value;
                            if (v == null) break;
                            var f = F("alert", "p_alert", v);
                            if (td && !foundSaid && i.SeenMark != "no") { f.Found = true; foundSaid = true; }
                            break;
                        }
                    case "eye":
                        {
                            string eye = i.SeenMark.Length > 0 ? (i.SeenMark == "no" ? "open" : i.SeenMark == "unk" ? "unk" : "shut")
                                : i.SeenQ != null ? HandoverHost.EyeOf(i.SeenQ) : i.Seen == "?" ? "unk" : i.Seen.Length > 0 ? "open" : "shut";
                            var ef = F("eye", "eye_" + (eye == "unk" ? "unk" : eye), ""); ef.Eye = eye; ef.Tip = EyeTip(eye, i.Seen);
                            break;
                        }
                    case "learns":
                        {
                            var f = F("learns", "", "");
                            if (o.Do == "watch") f.Icons.Add(i.Outcome == "its sight" ? "cone" : "routine");
                            else if (o.Do == "listen") { f.Icons.Add("p_unknown"); f.Icons.Add("p_unknown"); } // need and temperament, unknown until heard (as Intel draws them)
                            if (f.Icons.Count == 0) l.Remove(f);
                            break;
                        }
                    case "pace":
                        foreach (var (_, text) in i.Rows) if (Speed.Match(text) is var sm && sm.Success) { F("pace", "s_carrying", sm.Groups[1].Value + " m/s"); break; }
                        break;
                    case "mark":
                        {
                            string mk = null;
                            switch (o.Do)
                            {
                                case "turn": mk = i.Orders > 1 && i.Outcome == "turned" ? "half" : MarkOf(i.Outcome); break;
                                case "call": mk = i.Outcome == "ready" ? "ok" : "no"; break;
                                case "extract": mk = "ok"; break;
                                case "enter": if (i.SeenQ != null && i.SeenQ.Seen == "yes") mk = "no"; break;
                            }
                            if (mk != null) F("mark", "", "").Mark = mk;
                            break;
                        }
                }
            if (HudToolkit.Stress) foreach (var f in l) f.Text = f.Kind switch { "usd" => "100k–140k", "time" => "88 s", "trail" => "+12", "alert" => "≥2", "pace" => "1.4 m/s", _ => f.Text };
            return l;
        }

        /// <summary>Alt's fragments under a way: the walk split from the act, the zero fields, and the takedown's numbers (units on the scene, the
        /// door held, the wake, a report stopped, the ties of a kill). Icons and numbers only.</summary>
        public static List<PField> AltLine(HandoverHost h, HandoverHost.Intent i)
        {
            var l = new List<PField>(); var o = i.Order; if (o == null) return l;
            PField F(string icon, string text) { var f = new PField { Icon = icon, Text = text }; l.Add(f); return f; }
            var act = Num.Match(i.Time ?? "");
            if (o.Do == "takedown" && i.Td is TakedownQuote q) // Glyph §4: units, the door held, the wake, a report stopped (the walk split would be a fifth word)
            {
                if (q.PoliceInS >= 0) F("units", $"{q.PoliceInS} s");
                if (q.HoldsDoors.Count > 0) F("door_held", $"{q.HoldS / 60}:{q.HoldS % 60:00}");
                if (o.How == "knockout" && q.WakesAt >= 0) F("body_out", HandoverHost.MissionClock(q.WakesAt));
                if (o.How == "kill" && q.Ties > 0) F("talker", q.Ties.ToString());
                foreach (var (icon, text) in i.Rows) if (icon == "call" && Num.Match(text) is var rm && rm.Success) F("call", rm.Groups[1].Value + " s");
                return l;
            }
            if (i.WalkS > 0 && act.Success) { F("s_moving", $"{i.WalkS} s"); F("p_time", $"{act.Groups[1].Value} s"); }
            if ((i.Usd ?? "") is "$0" or "") F("p_cash", "0");
            if (!Plus.IsMatch(i.Trail ?? "") || (i.Trail ?? "").Contains("+0")) F("p_trail", "+0");
            if ((i.Alert ?? "") is "A 0" or "") F("p_alert", "0");
            return l;
        }
    }
}
