using System;
using System.Collections.Generic;
using Suborned.Sim;

namespace Suborned.Play
{
    /// <summary>Brief 099, the start page's rows answered (Glyph's concept A): each condition of book.job.needs (the same vocabulary the start page ticks, HostBook.Needs) and the
    /// first tick it held this run. The host reads them once a tick as it steps (CrucialTrack), so the time is when the start page itself ticked it; a run the host did not
    /// step from 0:00 (continued from a save, or adopted from a headless play) is replayed from its start and orders once, on the spot, the same check each tick. How a row
    /// came true is the log line that made it so, at or just before that tick (Fable's debrief.page.how keys); a camera that taped what it should not have seen says so.
    /// Reads only: nothing here changes the run.</summary>
    public sealed partial class HandoverHost
    {
        /// <summary>The first tick each condition held, by "way|id"; the log codes seen so far; the run they belong to and the tick the reading began at.</summary>
        readonly Dictionary<string, int> needFirst = new(); readonly HashSet<string> needCodes = new(); int needLogAt; Game needGame; int needFrom;
        /// <summary>The proof reads it: the rows' history came from a replay (a run not stepped here from 0:00), and what that took (ms).</summary>
        public bool NeedsReplayed { get; private set; } public double NeedsReplayMs { get; private set; }

        void NeedsReset() { needFirst.Clear(); needCodes.Clear(); needLogAt = 0; needGame = Game; needFrom = Game?.Tick ?? 0; NeedsReplayed = false; }

        /// <summary>Once a tick (CrucialTrack): the conditions that hold now, once each.</summary>
        void NeedsTrack() { if (Game == null) return; if (needGame != Game) NeedsReset(); NeedsSee(Game, needFirst, needCodes, ref needLogAt); }

        static void NeedsSee(Game g, Dictionary<string, int> first, HashSet<string> codes, ref int logAt)
        {
            for (; logAt < g.Log.Count; logAt++) codes.Add(g.Log[logAt].Code);
            foreach (var kv in BookWordsFile.Get().Needs)
                foreach (var n in kv.Value)
                {
                    string key = kv.Key + "|" + n.Id; if (first.ContainsKey(key)) continue;
                    if (n.Known.Length > 0 && CheckOn(g, n.Known, codes) != true) continue;
                    if (CheckOn(g, n.Check, codes) == true) first[key] = g.Tick;
                }
        }

        /// <summary>At the page: the end's own tick read too (an order given with the clock stopped can end the run between steps); a run not read from 0:00 replayed once.</summary>
        void NeedsAtEnd()
        {
            var g = Game; if (g == null) return;
            if (needGame != g || needFrom > 0) NeedsReplay(g);
            NeedsSee(g, needFirst, needCodes, ref needLogAt);
        }

        /// <summary>The run again from its start and its orders, off screen, each tick read as play reads it; then thrown away (FailCamStart's replay, stepped a tick at a time).</summary>
        void NeedsReplay(Game played)
        {
            var t0 = System.Diagnostics.Stopwatch.GetTimestamp(); needFirst.Clear(); needCodes.Clear(); needLogAt = 0; needGame = played; needFrom = 0;
            var list = new List<(int i, Order o)>(); for (int i = 0; i < played.Orders.Count; i++) list.Add((i, played.Orders[i].order));
            list.Sort((a, b) => a.o.Tick != b.o.Tick ? a.o.Tick.CompareTo(b.o.Tick) : a.i.CompareTo(b.i));
            try
            {
                var g = Engine.New(PlayInputs, played.Start); var codes = new HashSet<string>(); int at = 0, nx = 0; var first = new Dictionary<string, int>();
                while (!g.Over && g.Tick < played.Tick)
                {
                    while (nx < list.Count && list[nx].o.Tick <= g.Tick) { var o = list[nx++].o; if (o.Tick == g.Tick) g.Issue(o); if (g.Over) break; }
                    NeedsSee(g, first, codes, ref at); if (g.Over) break; g.Step();
                }
                while (!g.Over && nx < list.Count && list[nx].o.Tick <= g.Tick) { var o = list[nx++].o; if (o.Tick == g.Tick) g.Issue(o); }
                NeedsSee(g, first, codes, ref at);
                foreach (var kv in first) needFirst[kv.Key] = kv.Value;
                NeedsReplayed = true;
            }
            catch (Exception e) { UnityEngine.Debug.LogWarning($"Debrief: the rows' replay failed ({e.GetType().Name}: {e.Message}); the rows read the end only"); }
            NeedsReplayMs = (System.Diagnostics.Stopwatch.GetTimestamp() - t0) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        }

        /// <summary>The ways, in the needs' own order: which the run took (the most of its own rows came true; a tie, the first) and its rows answered, the way's own column as the
        /// start page shows it under its sign; the conditions every way shares after the first (go, left-safe: the start page's line under both) only where they decided a fail.
        /// A fail marks the row it lost at: the first that never came true and names Popovic's state (every way ends on him), else the first that never came true; the rows
        /// after it are spare (the first to give way on a full page).</summary>
        void DebriefTicks(BookPage p, DebriefWords dw, bool win)
        {
            var bw = BookWordsFile.Get(); string way = null; int best = -1;
            foreach (var kv in bw.Needs) { int n = 0; foreach (var x in kv.Value) if (needFirst.ContainsKey(kv.Key + "|" + x.Id)) n++; if (n > best) { best = n; way = kv.Key; } }
            if (way == null) return;
            p.WayWord = Text?.BriefWord("ways." + way, null) ?? way; p.WayIcon = way == "pickup" ? "car" : "enter"; p.WayNote = dw.W("way", "yours"); // the job page's own way word and sign (BookJobPage)
            var list = new List<BookWordsFile.Need>(bw.Needs[way]); var tail = SharedTail(bw); if (win) list.RemoveAll(n => tail.Contains(n.Id)); int lost = -1;
            if (Game.Result == Outcome.Failure) { for (int i = 0; i < list.Count && lost < 0; i++) if (!needFirst.ContainsKey(way + "|" + list[i].Id) && list[i].Check.Contains("vale:")) lost = i; for (int i = 0; i < list.Count && lost < 0; i++) if (!needFirst.ContainsKey(way + "|" + list[i].Id)) lost = i; }
            for (int i = 0; i < list.Count; i++)
            {
                var n = list[i]; var t = new BookTick { Words = BookTimes(n.Words) };
                if (needFirst.TryGetValue(way + "|" + n.Id, out int at)) { t.Met = true; t.Note = Dot(HowOf(n.Check, at, dw), MissionClock(at)); }
                else if (i == lost) { t.Lost = true; var h = p.Name.Length > 0 ? char.ToLowerInvariant(p.Name[0]) + p.Name[1..] : ""; t.Note = Dot(h, MissionClock(Game.Tick)); }
                else
                {
                    t.Unknown = n.Known.Length > 0 && CheckOn(Game, n.Known, null) != true;
                    var taped = Taped(n.Check); t.Note = taped >= 0 ? Dot(dw.W("how", "taped"), MissionClock(taped)) : t.Unknown ? "" : dw.W("how", "never"); t.Spare = lost >= 0 && i > lost;
                }
                p.Ticks.Add(t);
            }
        }
        /// <summary>The conditions every way has, after the first of them (BookJobStart's shared tail: the start page sets them once under the shared line).</summary>
        static HashSet<string> SharedTail(BookWordsFile bw)
        {
            var o = new HashSet<string>(); List<BookWordsFile.Need> first = null; foreach (var kv in bw.Needs) { first = kv.Value; break; } if (first == null || bw.Needs.Count < 2) return o;
            bool seen = false; foreach (var n in first) { bool all = true; foreach (var kv in bw.Needs) all &= kv.Value.Exists(x => x.Id == n.Id); if (!all) continue; if (seen) o.Add(n.Id); seen = true; }
            return o;
        }
        static string Dot(string a, string b) => a.Length == 0 ? b : b.Length == 0 ? a : a + " · " + b;

        /// <summary>The log codes that make a condition kind true, and the how key each gives (Fable's debrief.page.how; the takedown's kill is the stand-ins' "killed").</summary>
        static readonly (string kind, string code, string how)[] HowCodes =
        {
            ("guard-away", "call.waitress", "called"), ("guard-away", "guard.down", "shot"), ("guard-away", "takedown.out", "knocked"), ("guard-away", "takedown.dead", "killed"),
            ("camera-blind", "loop.on", "looped"), ("camera-blind", "camera.cut", "cut"), ("camera-blind", "fire.camera-dark", "shot-out"), ("inside", "enter.in", "in"),
        };

        /// <summary>How a condition came true: the latest log line of its kind at or before the tick (within a minute), or for a turned contact the turn that landed on them
        /// ("(role)" in the engine's person line), by its approach. "" when nothing says (a state such as Popovic in the car needs no how: its time is the row's).</summary>
        string HowOf(string check, int tick, DebriefWords dw)
        {
            var g = Game; int from = tick - 60 * Game.Hz;
            foreach (var part in check.Split('|'))
            {
                var t = part.Trim(); int c = t.IndexOf(':'); string kind = c < 0 ? t : t[..c], arg = c < 0 ? "" : t[(c + 1)..];
                for (int i = g.Log.Count - 1; i >= 0; i--)
                {
                    var l = g.Log[i]; if (l.Tick > tick) continue; if (l.Tick < from) break;
                    if (kind == "turned" && l.Code.StartsWith("turn.", StringComparison.Ordinal) && l.Code.EndsWith(".turned", StringComparison.Ordinal) && l.Text.Contains("(" + arg + ")"))
                        { string ap = l.Code.Split('.')[1]; string key = ap switch { "pay" => "paid", "help" => "helped", "pressure" => "pressed", "ask" => "asked", _ => "" }; return key.Length > 0 ? dw.W("how", key) : ""; }
                    foreach (var (k, code, how) in HowCodes) if (k == kind && l.Code == code) return dw.W("how", how);
                }
            }
            return "";
        }

        /// <summary>A camera condition never met whose camera taped an act this run (the ledger's recorded:…:camera:&lt;id&gt;): the tick it did, else -1.</summary>
        int Taped(string check)
        {
            foreach (var part in check.Split('|'))
            {
                var t = part.Trim(); if (!t.StartsWith("camera-blind:", StringComparison.Ordinal)) continue; string cam = t[13..];
                foreach (var (tick, what, _, _, cause) in Game.Ledger) if (what == "trail" && cause.StartsWith("recorded:", StringComparison.Ordinal) && cause.EndsWith(":camera:" + cam, StringComparison.Ordinal)) return tick;
            }
            return -1;
        }
    }
}
