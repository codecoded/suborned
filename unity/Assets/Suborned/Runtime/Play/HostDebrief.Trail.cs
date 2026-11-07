using System;
using System.Collections.Generic;
using Suborned.Sim;

namespace Suborned.Play
{
    /// <summary>Brief 099, the Trail number and its fold (Glyph's A-front-detail.png: "+18 Hasti and Larthi shot three rivals · +6 Arruns shot the guard …"): every Trail row of
    /// the engine's ledger (Game.Ledger, what "trail", its cause), each worded by its cause's key (Fable's debrief.page.trail.rows) and given the agent behind it from the run
    /// itself: a shooting by the shot lines the engine logged on that target at that row's tick (the first shot for the fire, the hit for the hit, the hit that dropped them for
    /// the fall), an act by the order that gave it. A shooting is one row a target group (the guard, the rival crew, the police, anyone else); the same words twice are one row.
    /// A cause with no words is counted in the bar and left out of the rows (DebriefUnworded lists it), never guessed at.</summary>
    public sealed partial class HandoverHost
    {
        public sealed class TrailRow { public int Tick, Delta; public string Cause = "", Kind = "", Target = "", Op = ""; }
        /// <summary>The proof reads it: ledger causes the page had no words for.</summary>
        public readonly List<string> DebriefUnworded = new();

        /// <summary>The ledger's Trail rows with their kind, target and agent.</summary>
        public List<TrailRow> TrailRows()
        {
            var g = Game; var l = new List<TrailRow>();
            foreach (var (tick, what, delta, _, cause) in g.Ledger)
            {
                if (what != "trail") continue; var p = cause.Split(':'); string head = p[0], kind, target = "", op = "";
                switch (head)
                {
                    case "gunfire": case "hit": case "down": kind = "shot"; target = string.Join(":", p, 1, p.Length - 1); op = Shooter(target, tick, head != "gunfire", head == "gunfire"); break;
                    case "pay" when p.Length == 2: kind = "pay"; target = p[1]; op = OrderOp("turn", target, tick); break; // a rival crew member paid off (engine 0.41.0)
                    case "mislead": kind = "mislead"; target = p.Length > 1 ? p[1] : ""; op = OrderOp("mislead", target, tick); break;
                    case "call": kind = "call"; target = p.Length > 1 ? p[1] : ""; break;
                    case "recorded": kind = "recorded"; target = p.Length >= 5 ? "camera:" + p[4] : ""; break;
                    case "loop": kind = "loop"; (op, target) = OrderOf("loop", tick); if (target.Length > 0) target = "camera:" + target; break;
                    case "disable": kind = "disable"; target = p.Length > 1 ? "camera:" + p[1] : ""; op = OrderOp("disable", p.Length > 1 ? p[1] : "", tick); break;
                    case "reroute": kind = "reroute"; (op, _) = OrderOf("reroute", tick); break;
                    case "takedown": kind = p.Length > 1 && p[1] is "kill" or "shot" ? "kill" : "knockout"; target = p.Length > 2 ? p[2] : ""; op = OrderOp("takedown", target, tick); break;
                    case "witnessed": kind = "seen"; break;
                    case "warning-heeded": kind = "warned"; target = p.Length > 1 ? p[1] : ""; break;
                    case "car-seen": kind = "car-seen"; target = p.Length > 1 ? p[1] : ""; break;
                    case "lent-seen": kind = "lent-seen"; target = p.Length > 1 ? p[1] : ""; break;
                    case "car-lost": kind = "car-lost"; break;
                    case "take": kind = "take-unturned"; break;
                    default: kind = head.StartsWith("turn.", StringComparison.Ordinal) ? head[5..] : head; target = p.Length > 1 ? p[1] : ""; if (head.StartsWith("turn.", StringComparison.Ordinal)) op = OrderOp("turn", target, tick); break;
                }
                l.Add(new TrailRow { Tick = tick, Delta = delta, Cause = cause, Kind = kind, Target = target, Op = op });
            }
            return l;
        }

        /// <summary>Who fired on `target` at this tick, from the engine's shot lines ("{shooter} fires on {target} at …", as its own Scenarios.Shots reads them): the first
        /// shooter for the fire, the last hitter for a hit or a fall; "" when none is one of the cell.</summary>
        string Shooter(string target, int tick, bool hit, bool first)
        {
            string found = "";
            foreach (var x in Game.Log)
            {
                if (x.Tick < tick) continue; if (x.Tick > tick) break;
                if (x.Code != "shot.hit" && (hit || x.Code != "shot.miss")) continue;
                int at = x.Text.IndexOf(" fires on ", StringComparison.Ordinal); if (at <= 0) continue;
                string by = x.Text[..at], rest = x.Text[(at + 10)..]; int sp = rest.IndexOf(" at ", StringComparison.Ordinal); if (sp < 0 || rest[..sp] != target || !IsOpName(by)) continue;
                found = by; if (first) break;
            }
            return found;
        }

        /// <summary>The agent of the last accepted order of this verb on this target at or before the tick ("" none).</summary>
        string OrderOp(string verb, string target, int tick) { var o = LastOrder(verb, target, tick); return o?.Op ?? ""; }
        (string op, string target) OrderOf(string verb, int tick) { var o = LastOrder(verb, null, tick); return (o?.Op ?? "", o?.Target ?? ""); }
        Order LastOrder(string verb, string target, int tick)
        {
            var g = Game; for (int i = g.Orders.Count - 1; i >= 0; i--) { var (o, ok, _, _) = g.Orders[i]; if (ok && o.Do == verb && o.Tick <= tick && (target == null || o.Target == target)) return o; }
            return null;
        }

        /// <summary>The group a shot target belongs to: the guard, the rival crew, the police, or itself.</summary>
        static string ShotGroup(string t) { if (t == "guard") return "guard"; var r = PlayText.RoleOfId(t); return r is "rival" or "officer" or "robot" ? r : HostViews.IsCrewId(t) ? "rival" : t; }

        /// <summary>The Trail fold's rows, biggest first: each shooting group one row (its shooters' faces, "{op} shot {who}"), each act one row with its agent's face; the same words
        /// twice are one row, their times joined. Hot: the shooting share (the bar's orange).</summary>
        List<BookFoldRow> TrailFoldRows(List<TrailRow> rows, DebriefWords dw)
        {
            var o = new List<BookFoldRow>(); DebriefUnworded.Clear(); string and = dw.W("and");
            var groups = new List<string>(); var by = new Dictionary<string, (int n, List<string> ops, List<string> tgts, int t0, int t1)>();
            foreach (var r in rows)
            {
                if (r.Kind != "shot") continue; string gk = ShotGroup(r.Target);
                if (!by.TryGetValue(gk, out var e)) { groups.Add(gk); e = (0, new List<string>(), new List<string>(), r.Tick, r.Tick); }
                e.n += r.Delta; e.t1 = r.Tick; if (r.Op.Length > 0 && !e.ops.Contains(r.Op)) e.ops.Add(r.Op); if (!e.tgts.Contains(r.Target)) e.tgts.Add(r.Target); by[gk] = e;
            }
            foreach (var gk in groups)
            {
                var e = by[gk]; string who = gk is "rival" or "officer" or "robot" ? Text?.HoldersPhrase(string.Join(",", e.tgts)) ?? "" : DebriefWho(e.tgts[0]);
                string ops = AndList(e.ops.ConvertAll(x => AgentName((int)Op.Parse(x))), and);
                string words = dw.F(new[] { "trail", "rows", "shot", "row" }, true, ("op", ops), ("who", who));
                if (words.Length == 0) { DebriefUnworded.Add("shot:" + gk); continue; }
                var row = new BookFoldRow { Icon = "fire", Value = Signed(e.n), Words = words, When = MissionClock(e.t0) + (e.t1 - e.t0 > 3 * Game.Hz ? "–" + MissionClock(e.t1) : ""), Hot = true };
                foreach (var x in e.ops) row.Faces.Add($"portraits/cameo_{x}_normal"); o.Add(row);
            }
            var acts = new Dictionary<string, BookFoldRow>(); var sums = new Dictionary<string, int>();
            foreach (var r in rows)
            {
                if (r.Kind == "shot") continue;
                string words = dw.F(new[] { "trail", "rows", r.Kind, "row" }, true, ("op", r.Op.Length > 0 && IsOpName(r.Op) ? AgentName((int)Op.Parse(r.Op)) : null), ("who", DebriefWho(r.Target)), ("by", DebriefWho(r.Target)));
                if (words.Length == 0) { DebriefUnworded.Add(r.Cause); continue; }
                if (acts.TryGetValue(words, out var same)) { sums[words] += r.Delta; same.Value = Signed(sums[words]); same.When += ", " + MissionClock(r.Tick); continue; }
                var row = new BookFoldRow { Icon = TrailIcon(r.Kind), Value = Signed(r.Delta), Words = words, When = MissionClock(r.Tick) };
                if (r.Op.Length > 0 && IsOpName(r.Op)) row.Faces.Add($"portraits/cameo_{r.Op}_normal");
                acts[words] = row; sums[words] = r.Delta; o.Add(row);
            }
            o.Sort((a, b) => Num(b.Value).CompareTo(Num(a.Value))); // biggest first, stable enough for equal values: the list was in the ledger's order
            return o;
        }
        static int Num(string v) => int.TryParse(v.Replace("+", "").Replace("$", "").Replace(",", "").Replace("\u2212", "-"), out int n) ? n : 0; // HostOdds.Signed writes the true minus
        static string TrailIcon(string kind) => kind switch { "help" => "help", "pay" => "pay", "loop" => "loop", "reroute" => "reroute", "call" => "s_calling", "recorded" => "sawyou", "car-seen" or "lent-seen" => "eye_open", "seen" => "sawyou", "disable" => "loop", "mislead" => "mislead", "knockout" or "kill" => "takedown", _ => "p_trail" };
        /// <summary>"a, b and c" with the text's own joining word ("" for none: then only commas).</summary>
        static string AndList(List<string> xs, string and) { xs = xs.FindAll(x => !string.IsNullOrEmpty(x)); if (xs.Count <= 1) return xs.Count == 1 ? xs[0] : ""; return string.Join(", ", xs.GetRange(0, xs.Count - 1)) + (and.Length > 0 ? " " + and + " " : ", ") + xs[^1]; }
        /// <summary>A person, camera or watcher as the page names them (brief 100's names: "the guard", "the pickup camera", "a police officer"); "" for nothing known.</summary>
        string DebriefWho(string id) => string.IsNullOrEmpty(id) ? "" : WhyWho(id) is { Length: > 0 } w ? w : Name(id);

        /// <summary>The Trail fold: what it means, a bar of every point, the rows, the shooting law with the rulebook's own numbers.</summary>
        BookFoldView TrailFold(DebriefWords dw)
        {
            var rows = TrailRows(); var f = new BookFoldView { Id = "trail", Title = dw.W("numbers", "trail") + " " + Signed(Game.Trail), Head = dw.W("trail", "byWhat").ToLowerInvariant(), Means = dw.W("trail", "means"), Rest = dw.W("folds", "rest") };
            var r = Game.R; f.Law = dw.F(new[] { "trail", "law" }, false, ("fire", r.GunfireTrail.ToString()), ("hit", r.HitTrail.ToString()), ("down", r.DownTrail.ToString()));
            if (!rows.Exists(x => x.Kind == "shot")) f.Law = "";
            foreach (var row in TrailFoldRows(rows, dw)) { f.Rows.Add(row); f.Pips.Add((Num(row.Value), row.Hot)); }
            int shown = 0; foreach (var (n, _) in f.Pips) shown += n; if (Game.Trail - shown > 0) f.Pips.Add((Game.Trail - shown, false)); // the unworded points still count in the bar
            return f;
        }

        /// <summary>The Trail number's one line: nothing traced; the shooting's share when it is half or more; else the biggest causes' short words.</summary>
        string TrailWhy(DebriefWords dw)
        {
            var rows = TrailRows(); int tot = 0, shot = 0; foreach (var r in rows) { tot += r.Delta; if (r.Kind == "shot") shot += r.Delta; }
            if (tot == 0) return dw.F(new[] { "reasons", "trail", "none" }, true);
            if (shot * 2 >= tot) return dw.F(new[] { "reasons", "trail", "shooting" }, true, ("n", shot.ToString()), ("trail", tot.ToString()));
            var by = new Dictionary<string, int>(); var order = new List<string>();
            foreach (var r in rows) { var s = dw.F(new[] { "trail", "rows", r.Kind, "short" }, false, ("who", DebriefWho(r.Target))); if (s.Length == 0) continue; if (!by.ContainsKey(s)) { by[s] = 0; order.Add(s); } by[s] += r.Delta; }
            order.Sort((a, b) => by[b].CompareTo(by[a])); if (order.Count > 3) order.RemoveRange(3, order.Count - 3);
            return dw.F(new[] { "reasons", "trail", "list" }, true, ("list", AndList(order, dw.W("and"))));
        }
    }
}
