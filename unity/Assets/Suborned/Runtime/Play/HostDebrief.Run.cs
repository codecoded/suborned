using System;
using System.Collections.Generic;
using Suborned.Sim;

namespace Suborned.Play
{
    /// <summary>Brief 099, the foot of the debrief (Glyph's concept A): what the city does now, from the debrief the host builds on every ending (who was left behind, the
    /// investigation and its cause, cases, more watch, the hunt for our car, who we kept, word of mouth), in debrief's own words and the page's; and the run along the
    /// foot on the book's track: the mission's own stops, the cell's acts as their agents' faces, the city's answers, the end. Each beat is the first log line of its kind
    /// (a fight each time one starts); past eight, the city's quiet beats give way first. Reads only.</summary>
    public sealed partial class HandoverHost
    {
        void DebriefCity(BookPage p, DebriefWords dw)
        {
            var d = EndDebrief; if (d == null) return; var t = Text;
            void Add(string icon, string head, string line) { if (head.Length > 0) p.City.Add(new BookCity { Icon = icon, Head = head, Line = line }); }
            foreach (var (op, fate, why) in d.LeftBehind) // a capture is news; one who walked out is the end as planned
                if (fate == "captured" && IsOpName(op) && t != null) Add(fate == "captured" ? "close_x" : "s_moving", $"{AgentName((int)Op.Parse(op))} · {(t.GetawayFate.TryGetValue(fate, out var fw) ? fw : "")}".TrimEnd(' ', '·'), t.GetawayWhy.TryGetValue(why, out var ww) ? ww : "");
            // {missions} is the count alone, as Fable's block has it ("Killing found · {missions}-mission case"; debrief.schema.ts, the style agent's fill "a number of missions")
            string cause = d.InvestigationCause, ck = cause.Split(':')[0], who = cause.Contains(':') ? Name(cause[(cause.IndexOf(':') + 1)..]) : null;
            Add("found", dw.Debrief("investigation", d.InvestigationState).TrimEnd('.'), DebriefWords.Fill(dw.Debrief("causes", ck), false, ("who", who)));
            foreach (var (cw, kind, place, missions) in d.Next.Cases) Add("papers", dw.F(new[] { "city", "case", "head" }, true, ("who", DebriefWho(cw))), dw.F(new[] { "city", "case", "line" }, false, ("missions", missions.ToString())));
            foreach (var (place, guards, cameras, missions) in d.Next.AreaWatch)
                Add("eye_open", dw.F(new[] { "city", "watch", "head" }, true, ("guards", guards.ToString()), ("cameras", cameras.ToString()), ("place", t != null && t.WhyPlaces.TryGetValue(place, out var pw) ? pw : null)), dw.F(new[] { "city", "watch", "line" }, false, ("missions", missions.ToString())));
            if (d.Inv.Hunted.Contains("cell-car")) Add("car", dw.F(new[] { "city", "hunt", "head" }, true), dw.F(new[] { "city", "hunt", "line" }, false, ("by", t != null && t.GetawayBy.TryGetValue(PlayText.RoleOfId(d.Next.CarSeenBy), out var by) ? by : null)));
            foreach (var a in d.Assets) Add("turned", dw.W("city", "kept"), dw.Debrief("assets", a).TrimEnd('.'));
            var words = d.Next.Lines.FindAll(x => x.Line == "word"); string face = null; int faces = 0;
            foreach (var x in words) foreach (var r in CellRoles) if (x.Detail.Contains(r + "-face")) { face ??= r; if (r == face) faces++; }
            if (face != null) Add("talker", dw.F(new[] { "city", "face" }, true, ("n", faces.ToString()), ("op", AgentName((int)Op.Parse(face)))), dw.W("city", "word", "line"));
            else if (words.Count > 0) Add("talker", dw.F(new[] { "city", "word", "head" }, true, ("n", words.Count.ToString())), dw.W("city", "word", "line"));
        }
        /// <summary>The run's last beat: away (a win), withdrew (a withdrawal, where the words have it), lost.</summary>
        string EndBeat(bool win, DebriefWords dw) => win ? "away" : Game.Result == Outcome.Withdrawn && dw.W("beats", "withdrew").Length > 0 ? "withdrew" : "lost";

        /// <summary>A beat: its tick, the beats key, icon, the agent whose act it was (a face), our lane (the deep disc) and hot (the one that lost it).</summary>
        sealed class RunBeat { public int Tick, Rank; public string Key = "", Icon = "", Op = "", Who = ""; public bool Ours, Hot, City; }

        void DebriefRun(BookPage p, DebriefWords dw, bool win)
        {
            var g = Game; var beats = new List<RunBeat>(); var seen = new HashSet<string>(); bool byLookout = !win && g.Cause is FailCause fc && fc.Why == "lookout";
            void Add(int tick, string key, string icon, string op = "", bool ours = false, bool city = false, bool hot = false, string who = "") { if (seen.Add(key)) beats.Add(new RunBeat { Tick = tick, Key = key, Icon = icon, Op = op, Ours = ours || op.Length > 0, City = city, Hot = hot, Who = who }); }
            foreach (var x in g.Log)
            {
                string c = x.Code; int dot = c.IndexOf('.'); string head = dot > 0 ? c[..dot] : c;
                if (IsOpName(head) && c.EndsWith(".watch.watched", StringComparison.Ordinal)) Add(x.Tick, "watched", "watch", head);
                else if (IsOpName(head) && c.EndsWith(".listen.heard", StringComparison.Ordinal)) Add(x.Tick, "listened", "listen", head);
                else if (c.StartsWith("turn.", StringComparison.Ordinal) && c.EndsWith(".turned", StringComparison.Ordinal)) { int a = x.Text.IndexOf(" (", StringComparison.Ordinal), b = x.Text.IndexOf("):", StringComparison.Ordinal); string role = a > 0 && b > a ? x.Text[(a + 2)..b] : ""; if (seen.Add("turned:" + role)) beats.Add(new RunBeat { Tick = x.Tick, Key = "turned", Icon = "turned", Op = OrderOp("turn", role, x.Tick), Ours = true, Who = Cap(Name(role)) }); }
                else switch (c)
                    {
                        case "loop.on": Add(x.Tick, "loop", "loop", OrderOf("loop", x.Tick).op); break;
                        case "patrol.rerouted": Add(x.Tick, "reroute", "reroute", OrderOf("reroute", x.Tick).op); break;
                        case "call.waitress": Add(x.Tick, "call", "s_calling", city: true); break;
                        case "enter.in": Add(x.Tick, "in", "enter", OrderOf("enter", x.Tick).op is { Length: > 0 } eo ? eo : "enforcer"); break;
                        case "enter.out": Add(x.Tick, "out", "s_carrying"); break;
                        case "rivals.coming": Add(x.Tick, "rivals", "car"); break;
                        case "rivals.lookout-posted": Add(x.Tick, "lookout", "sawyou", city: !byLookout, hot: byLookout); break;
                        case "camera.record-lands": Add(x.Tick, "taped", "sawyou", city: true); break;
                        case "vale.exits": Add(x.Tick, "vale-out", "s_moving"); break;
                        case "report.landed": Add(x.Tick, "report", "radio", city: true); break;
                        case "car.seen": Add(x.Tick, "car-seen", "eye_open", city: true); break;
                    }
            }
            foreach (var (tick, what, _, now, _) in g.Ledger) if (what == "alert") { if (seen.Add("alert")) beats.Add(new RunBeat { Tick = tick, Key = "alert", Icon = "p_alert", City = true, Who = now.ToString() }); break; }
            foreach (var f in g.Fights) beats.Add(new RunBeat { Tick = f.Start, Key = f.Final ? "fight" : "shots", Icon = "fire", Ours = true });
            beats.Sort((a, b) => a.Tick.CompareTo(b.Tick));
            bool lost = g.Result == Outcome.Failure; beats.Add(new RunBeat { Tick = g.Tick, Key = EndBeat(win, dw), Icon = win ? "car" : "close_x", Ours = !lost, Hot = lost }); // a withdrawal ends on its own word, not the loss's orange
            // the order the beats give way in: the quiet city beats first, then a watch, then the second beat on; the end always stays. Past eight they go here; the page shows fewer
            // when its labels need the room (BookLayout.FitRun reads the rank)
            for (var left = new List<RunBeat>(beats); left.Count > 0;)
            {
                int i = left.FindIndex(b => b.City && !b.Hot); if (i < 0) i = left.FindIndex(b => b.Key == "watched"); if (i < 0 || i == left.Count - 1) i = left.Count > 2 ? 1 : 0;
                left[i].Rank = left.Count - 1; left.RemoveAt(i);
            }
            beats.RemoveAll(b => b.Rank >= p.TrackMax);
            foreach (var b in beats)
            {
                string word = dw.F(new[] { "beats", b.Key }, true, ("who", b.Who), ("alert", b.Who)); if (word.Length == 0) continue;
                p.Track.Add(new BookStop { Icon = b.Icon, Word = word, Time = MissionClock(b.Tick), Face = b.Op.Length > 0 && IsOpName(b.Op) ? $"portraits/cameo_{b.Op}_normal" : "", Past = b.Ours, Hot = b.Hot, Rank = b.Rank });
            }
        }
    }
}
