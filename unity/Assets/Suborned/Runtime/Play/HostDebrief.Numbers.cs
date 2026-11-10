using System;
using System.Collections.Generic;
using System.Globalization;
using Suborned.Sim;

namespace Suborned.Play
{
    /// <summary>Brief 099, the run's five numbers (Glyph's concept A) and the fold each opens, every one from data the host already reads:
    ///  · Time: the end's tick; a win's spare time against Popovic's car leaving (the site's valeAtCar); a fail's reason by its cause (debrief.page.next.by).
    ///  · Spent: the purse less the cash at the end (exact); what it went on from the turns that landed (the log's person lines, the ledger's crew pay). The engine has no cash
    ///    rows yet (Keel, after law L9), so a fold names each payment and shows an amount only when one payment is the whole sum.
    ///  · Trail: the ledger's rows (HostDebrief.Trail).
    ///  · Alert: the highest it reached; each rise worded by brief 100's why rows (the live ones; for a run not stepped here, the same words built at the end, which may say less).
    ///  · Standing: the debrief's line for the next mission (from, to, band, its cause, every move), in debrief.standing's words.
    /// The words are DebriefWords' (Fable's debrief.page, else the marked stand-ins).</summary>
    public sealed partial class HandoverHost
    {
        void DebriefStats(BookPage p, DebriefWords dw, bool win)
        {
            var g = Game;
            string timeWhy = win ? (g.S.ValeAtCar * Game.Hz - g.Tick is int spare && spare > 0 ? dw.F(new[] { "reasons", "time", "win" }, true, ("clock", MissionClock(spare))) : "") : NextWords(dw, "time") is { Length: > 0 } nt ? nt : p.Name;
            p.Stats.Add(new BookStat { Id = "time", Icon = "p_time", Label = dw.W("numbers", "time"), Value = MissionClock(g.Tick), Why = timeWhy });
            var paid = Payments(); string key = paid.Count == 0 ? "none" : paid.Count > 1 ? "both" : paid[0].approach == "help" ? "help" : "pay";
            p.Stats.Add(new BookStat { Id = "cash", Icon = "p_cash", Label = dw.W("numbers", "cash"), Value = Usd(Spent()), Why = dw.F(new[] { "reasons", "cash", key }, true, ("who", paid.Count == 1 ? DebriefWho(paid[0].who) : null)) });
            p.Stats.Add(new BookStat { Id = "trail", Icon = "p_trail", Label = dw.W("numbers", "trail"), Value = g.Trail > 0 ? "+" + g.Trail : "0", Why = TrailWhy(dw) });
            var rises = AlertRises(); int top = g.Alert; foreach (var r in rises) top = Math.Max(top, r.To);
            var topRise = rises.Find(r => r.To == top);
            p.Stats.Add(new BookStat { Id = "alert", Icon = "p_alert", Label = dw.W("numbers", "alert"), Value = top.ToString(), Hot = top > 0, Why = top == 0 ? dw.F(new[] { "reasons", "alert", "none" }, true) : topRise != null ? DebriefWords.Fill(topRise.ShortWhere, true) : "" });
            if (EndDebrief?.Next.Standing is { Count: > 0 } sl)
            {
                var s = sl[0]; string band = Text != null && Text.StandingBands.TryGetValue(s.Band, out var b) ? b : s.Band;
                p.Stats.Add(new BookStat { Id = "standing", Icon = s.Band == "hero" ? "mood_friendly" : s.Band == "menace" ? "mood_scared" : "mood_uneasy", Label = dw.W("numbers", "standing"), Value = $"{band} {Signed(s.To)}".Trim(), Hot = s.Band == "menace", Why = StandingCause(s.Cause, s.Who) });
            }
        }

        long Spent() { var g = Game; return Math.Max(0, (g.Start.Cash ?? g.S.Purse) - g.Cash); }
        static string Usd(long v) => "$" + v.ToString("N0", CultureInfo.InvariantCulture);
        string StandingCause(string cause, string who) => Text != null && Text.StandingCauses.TryGetValue(cause, out var c) ? DebriefWords.Fill(c.Split(new[] { ". " }, StringSplitOptions.None)[0], true, ("who", who.Length > 0 ? Name(who) : null)).TrimEnd('.') : ""; // its first sentence; a {who} with nobody named says nothing
        /// <summary>A fail's own words under next.by[fail][key] (the key as the end card keys its cause: HostEndWords.CauseKey).</summary>
        string NextWords(DebriefWords dw, string part) => Game.Cause is FailCause c ? dw.W("next", "by", c.Fail, CauseKey(c), part) : "";

        /// <summary>The payments that landed: a contact turned by paying or helping (the engine's person line: "{name} ({role}): turn.{approach} → turned."), a rival paid off (the ledger's pay:&lt;id&gt;).</summary>
        List<(int tick, string approach, string who, string op)> Payments()
        {
            var g = Game; var l = new List<(int, string, string, string)>();
            foreach (var x in g.Log)
            {
                if (x.Code is not ("turn.pay.turned" or "turn.help.turned")) continue; int a = x.Text.IndexOf(" (", StringComparison.Ordinal), b = x.Text.IndexOf("):", StringComparison.Ordinal); if (a < 0 || b < a) continue;
                string role = x.Text[(a + 2)..b], ap = x.Code.Split('.')[1]; l.Add((x.Tick, ap, role, OrderOp("turn", role, x.Tick)));
            }
            foreach (var (tick, what, _, _, cause) in g.Ledger) if (what == "trail" && cause.StartsWith("pay:", StringComparison.Ordinal)) { var id = cause[4..]; l.Add((tick, "pay", id, OrderOp("turn", id, tick))); }
            l.Sort((x, y) => x.Item1.CompareTo(y.Item1)); return l;
        }

        /// <summary>Each Alert rise of the ledger with brief 100's words: the live why row for that rise, else the same words built now.</summary>
        List<WhyRow> AlertRises()
        {
            var o = new List<WhyRow>(); var g = Game;
            foreach (var (tick, what, delta, now, cause) in g.Ledger)
            {
                if (what != "alert") continue; var r = WhyRows.Find(x => x.What == "alert" && x.Tick == tick && x.To == now);
                if (r == null && Text?.HasWhy == true) try { r = AlertWords(cause, now - delta, now, false, tick); if (r != null) { r.Tick = tick; r.From = now - delta; r.To = now; } } catch (Exception) { r = null; }
                o.Add(r ?? new WhyRow { Tick = tick, From = now - delta, To = now });
            }
            return o;
        }

        /// <summary>A number's fold, built when it opens.</summary>
        BookFoldView DebriefFoldOf(string id, DebriefWords dw, bool win)
        {
            var g = Game; BookFoldView f;
            switch (id)
            {
                case "trail": return TrailFold(dw);
                case "time":
                    f = Fold("time", MissionClock(g.Tick), dw);
                    if (Text != null) foreach (var at in Text.PageClock)
                        {
                            var bw = BookWordsFile.Get(); bw.Stops.TryGetValue(at, out var st); var (_, label, _) = Text.Timeline.Find(x => x.at == at);
                            string word = bw.Stations.TryGetValue(at, out var sw) ? sw : Text.TimelineShort.TryGetValue(at, out var sh) ? sh : label ?? at;
                            int logged = -1; if (st != null && st.Log.Length > 0) foreach (var x in g.Log) if (x.Code == st.Log) { logged = x.Tick; break; }
                            f.Rows.Add(new BookFoldRow { Icon = st?.Icon ?? "nextevent", Value = logged >= 0 ? MissionClock(logged) : at, Words = word });
                        }
                    f.Rows.Add(new BookFoldRow { Icon = win ? "car" : "close_x", Value = MissionClock(g.Tick), Words = dw.W("beats", EndBeat(win, dw)), Hot = g.Result == Outcome.Failure, Keep = true }); // the end always shows
                    f.Rows.Sort((a, b) => Clock(a.Value).CompareTo(Clock(b.Value)));
                    return f;
                case "cash":
                    f = Fold("cash", Usd(Spent()), dw); var paid = Payments();
                    foreach (var (tick, ap, who, op) in paid)
                    {
                        var row = new BookFoldRow { Icon = ap == "help" ? "help" : "pay", Value = paid.Count == 1 ? Usd(Spent()) : "", When = MissionClock(tick), Words = dw.F(new[] { "trail", "rows", ap, "row" }, true, ("op", IsOpName(op) ? AgentName((int)Op.Parse(op)) : null), ("who", DebriefWho(who))) };
                        if (IsOpName(op)) row.Faces.Add($"portraits/cameo_{op}_normal"); if (row.Words.Length > 0) f.Rows.Add(row);
                    }
                    return f;
                case "alert":
                    f = Fold("alert", AlertTop().ToString(), dw);
                    foreach (var r in AlertRises()) { var w = DebriefWords.Fill(r.ShortWhere, true); if (w.Length > 0) f.Rows.Add(new BookFoldRow { Icon = "p_alert", Value = "+" + (r.To - r.From), Words = w, When = MissionClock(r.Tick), Hot = r.To >= 2 }); }
                    return f;
                case "standing":
                    if (EndDebrief?.Next.Standing is not { Count: > 0 } sl) return null;
                    var s = sl[0]; string band = Text != null && Text.StandingBands.TryGetValue(s.Band, out var b) ? b : s.Band; f = Fold("standing", $"{band} {Signed(s.To)}".Trim(), dw);
                    for (int i = 0; i < s.Moves.Count; i++)
                    {
                        var (delta, cause, who) = s.Moves[i]; string w = StandingCause(cause, who); if (w.Length == 0) continue;
                        f.Rows.Add(new BookFoldRow { Icon = delta > 0 ? "mood_friendly" : "mood_scared", Value = Signed(delta), Words = w, When = i < g.StandingMoves.Count && g.StandingMoves[i].cause == cause ? MissionClock(g.StandingMoves[i].tick) : "", Hot = delta < 0 });
                    }
                    return f;
            }
            return null;
        }
        int AlertTop() { int top = Game.Alert; foreach (var (_, what, _, now, _) in Game.Ledger) if (what == "alert") top = Math.Max(top, now); return top; }
        static int Clock(string mmss) { var p = mmss.Split(':'); return p.Length == 2 && int.TryParse(p[0], out int m) && int.TryParse(p[1], out int s) ? m * 60 + s : int.MaxValue; }
        BookFoldView Fold(string id, string value, DebriefWords dw) => new BookFoldView { Id = id, Title = (dw.W("numbers", id) + " " + value).Trim(), Head = dw.W("folds", id, "head"), Means = dw.W("folds", id, "means"), Rest = id is "alert" or "standing" ? dw.W("folds", "rest") : "" };

        /// <summary>A fail's next time: what to deal with (debrief.page.next.by), and the cell's own ways on the culprit where the crucial card of the cause names them (book.feed.cards
        /// by the cause's why: the lookout's Mislead, Pay and Take, each with its agent). Faces and words, never a button: the page gives no order.</summary>
        void DebriefNext(BookPage p, DebriefWords dw, bool win)
        {
            if (win || Game.Cause is not FailCause c) return; string line = NextWords(dw, "line");
            if (Game.Log.Exists(x => x.Code == "car.shot-out")) { var bw = BookWordsFile.Get(); line = (line.Length > 0 ? line + " " : "") + (Game.Lent && Game.LentId != "car:official" ? bw.NextShotOut : bw.NextShotOut + " " + bw.NextTaken); } // brief 105 (engine 0.53.0): our car shot out; a car stood free to take
            if (line.Length == 0) return;
            p.NextHead = dw.W("next", "head"); p.NextLine = line;
            if (BookWordsFile.Get().FeedCards.TryGetValue(c.Why, out var card))
                for (int i = 0; i < card.Fixes.Count && i < card.FixBy.Count; i++)
                {
                    var agent = card.FixBy[i]; if (!IsOpName(agent)) continue;
                    p.NextWays.Add(new BookChip { Face = $"portraits/cameo_{agent}_normal", Icon = card.Fixes[i].icon, Word = card.Fixes[i].word, Who = AgentName((int)Op.Parse(agent)) });
                }
        }
    }
}
