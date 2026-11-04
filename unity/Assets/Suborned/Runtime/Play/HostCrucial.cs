using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 096, the slim feed (Sky 2026-11-04: "the main feed bottom right of the screen should really only show crucial info that lasts until clicked on to dismiss
    /// (then shows the next etc)"; Glyph's mocks 3a, 3b, 3d; Sky's calls 4 and 7, "all. recommended"): one crucial card at a time, kept until clicked, "1 of n"; queued most
    /// urgent first (police, agent down, rivals for our car, Alert, seen, Popovic out, rival car, turned or refused, body found). A card leaves by itself only once
    /// it stops being true (the rivals turn back from the car, the agent is up again). Everything, crucial or not, is in the book's Journal. The clock runs; nothing
    /// pauses. Read from the engine's log, ledger and state once a tick (from FeedClock): nothing here changes the run, so a replay is untouched.</summary>
    public sealed partial class HandoverHost
    {
        public sealed class CrucialCard
        {
            public int Id, Tick, Rank, Value, Agent = -1; public string Kind = "", Code = "", Role = "", Title = "", Line = "", Face = "", Icon = "", View = ""; public Vector3? Place; public bool Read, Gone;
            public readonly List<(string icon, string word)> Fixes = new();
        }
        /// <summary>The kinds in their order of urgency (Sky's call 7); turned and refused share a rank. Brief 096, the lend (engine 0.42.0): Popovic out (vale.out)
        /// tops it, since from then the police countdown runs; the lookout watching Popovic's car and our car boxed in rank with the rivals for our car; Popovic's car
        /// lent to us with Popovic out of the Office.</summary>
        public static readonly string[] CrucialOrder = { "vale-out", "police", "down", "for-car", "alert", "seen", "target-out", "rival-car", "turned", "body" };
        static string RankAs(string kind) => kind switch { "refused" => "turned", "lookout" or "boxed" => "for-car", "lent" => "target-out", _ => kind };
        /// <summary>Cards waiting to be read, the shown one first; and every crucial card this run, oldest first (the Journal marks them).</summary>
        public readonly List<CrucialCard> CrucialQueue = new(), CrucialLog = new();
        public CrucialCard CrucialNow => CrucialQueue.Count > 0 ? CrucialQueue[0] : null;

        int crucialLogAt, crucialLedgerAt, crucialAlert, crucialSeenAt = -1000; readonly bool[] crucialDown = new bool[4]; bool crucialForCar;

        void ResetCrucial()
        {
            if (BookOpen) BookShut(true); // a new run shuts the book (Begin resets the feed); the start opens it again on Job
            CrucialQueue.Clear(); CrucialLog.Clear(); Array.Clear(crucialDown, 0, 4); crucialForCar = false; crucialSeenAt = -1000;
            crucialLogAt = Game?.Log.Count ?? 0; crucialLedgerAt = Game?.Ledger.Count ?? 0; crucialAlert = Game?.Alert ?? 0;
            if (Game != null) for (int k = 0; k < 4; k++) crucialDown[k] = Game.Ops[k].Down;
            journalSeen = 0; JournalOpenId = -1; JournalFrom = 0; JournalFilter = "all";
        }

        static int CrucialRank(string kind) { int i = Array.IndexOf(CrucialOrder, RankAs(kind)); return i < 0 ? CrucialOrder.Length : i; }

        CrucialCard Crucial(string kind, string code = "", string role = "", int agent = -1)
        {
            var w = BookWordsFile.Get().Card(kind);
            var c = new CrucialCard { Id = ++feedIds, Tick = Game.Tick, Kind = kind, Rank = CrucialRank(kind), Code = code, Role = role, Agent = agent, Line = w.Line };
            string agentName = agent >= 0 ? AgentName(agent) : "";
            c.Title = w.Title.Replace("{agent}", agentName).Replace("{who}", role.Length > 0 ? Cap(Name(role)) : "").Replace("{vale}", ValeWord);
            c.Fixes.AddRange(w.Fixes);
            CrucialQueue.Add(c); CrucialLog.Add(c);
            CrucialQueue.Sort((a, b) => a.Rank != b.Rank ? a.Rank.CompareTo(b.Rank) : a.Id.CompareTo(b.Id));
            return c;
        }

        /// <summary>Once a tick: new log lines, ledger lines and state changes that make a card; cards no longer true leave.</summary>
        void CrucialTrack()
        {
            var g = Game; if (g == null) return;
            for (; crucialLogAt < g.Log.Count; crucialLogAt++) CrucialFromLog(g.Log[crucialLogAt]);
            for (; crucialLedgerAt < g.Ledger.Count; crucialLedgerAt++)
            {
                var (tick, what, _, _, cause) = g.Ledger[crucialLedgerAt];
                if (what != "trail" || !(cause.StartsWith("witnessed:", StringComparison.Ordinal) || cause.StartsWith("car-seen:", StringComparison.Ordinal))) continue;
                if (tick - crucialSeenAt < 5 * Game.Hz) continue; crucialSeenAt = tick; // one card for a burst of witnesses
                var c = Crucial("seen", "trail:" + cause); c.Icon = "sawyou"; c.Line = ""; c.Place = SelectedWorld();
            }
            if (g.Alert > crucialAlert) { var c = Crucial("alert", "alert"); c.Value = g.Alert; c.Icon = "p_alert"; c.View = "marker/door"; } crucialAlert = Math.Max(crucialAlert, g.Alert);
            for (int k = 0; k < 4; k++)
            {
                bool down = g.Ops[k].Down; if (down && !crucialDown[k]) { string r = Op.Name((OpId)k); var c = Crucial("down", r + ".down", agent: k); c.Face = $"portraits/cameo_{r}_hurt"; c.View = "op/" + r; c.Place = frame.World(g.Ops[k].Pos); }
                crucialDown[k] = down;
            }
            bool forCar = g.RivalsForCar.Count > 0;
            if (forCar && !crucialForCar) { var c = Crucial("for-car", "rivals.for-car"); c.Icon = "car"; c.View = "car/getaway"; }
            crucialForCar = forCar;
            // a card that stops being true leaves by itself (Sky's call 4); it stays in the Journal
            for (int i = CrucialQueue.Count - 1; i >= 0; i--) { var c = CrucialQueue[i]; if (!CrucialTrue(c)) { c.Gone = true; CrucialQueue.RemoveAt(i); } }
        }

        bool CrucialTrue(CrucialCard c) => c.Kind switch
        {
            "for-car" => Game.RivalsForCar.Count > 0,
            "down" => c.Agent >= 0 && Game.Ops[c.Agent].Down,
            "boxed" => StillBoxed(),
            "lookout" => LookoutWatching(),
            _ => true,
        };

        void CrucialFromLog(LogLine l)
        {
            string code = l.Code ?? "";
            // Sky 2026-11-05 ("sure"): the seal is never a feed card; its plate under the clock (brief 093) carries the countdown, in the Journal like every event
            if (code is "security.called" or "fight.loud" || code.StartsWith("alarm.dispatches", StringComparison.Ordinal))
            { if (!CrucialQueue.Exists(x => x.Kind == "police")) { var c = Crucial("police", code); c.Face = "badges/signal_police"; c.View = "security/0"; } }
            else if (code == "vale.out") { var c = Crucial("vale-out", code); c.Icon = "car"; c.View = "car/getaway"; } // brief 096 (car.lend L5): Popovic is out; the rest to our car before the police
            else if (code == "lend.lent") { var c = Crucial("lent", code, "councillor"); c.Face = "badges/npc_councillor"; c.View = "car/official"; } // L1: Popovic's car is ours, the Enforcer at its wheel
            else if (code == "rivals.lookout-posted") { var c = Crucial("lookout", code); c.Face = "badges/signal_rival"; c.View = "car/official"; c.Place = LookoutAt(); } // engine 0.41.0: deal with him, or he reaches Popovic's car
            else if (code is "rivals.block" or "rivals.back") { if (!CrucialQueue.Exists(x => x.Kind == "boxed")) { var c = Crucial("boxed", code); c.Icon = "car"; c.View = "car/rivals"; } } // engine 0.41.0: the rival car across our way out
            else if (code == "vale.exits") { var c = Crucial("target-out", code, "councillor"); c.Face = "badges/npc_councillor"; c.View = "councillor"; }
            else if (code == "rivals.coming") { var c = Crucial("rival-car", code); c.Face = "badges/signal_rival"; c.View = "car/rivals"; }
            else if (code == "report.confirmed") { var c = Crucial("body", code); c.Icon = "body_out"; }
            else if (code.StartsWith("turn.", StringComparison.Ordinal))
            {
                string outcome = code[(code.LastIndexOf('.') + 1)..]; if (outcome is not ("turned" or "refused")) return;
                string role = ""; foreach (var r in new[] { "waitress", "driver", "guard", "councillor" }) if (l.Text != null && l.Text.Contains($"({r})")) { role = r; break; }
                if (role.Length == 0) return;
                var c = Crucial(outcome, code, role); c.Face = role is "waitress" or "driver" ? $"portraits/cameo_{role}_normal" : HudModel.PersonBadge(role); c.View = role;
            }
        }

        /// <summary>The card's dim line now: its place or what (live for a count or a countdown), then the clock it came at.</summary>
        public string CrucialLine(CrucialCard c)
        {
            string line = c.Line;
            if (line.Length > 0)
            {
                line = line.Replace("{n}", (c.Kind == "for-car" ? Math.Max(1, Game.RivalsForCar.Count) : c.Value).ToString())
                    .Replace("{seal}", Game.Lockdown < int.MaxValue / 8 ? MissionClock(Game.Lockdown) : "?").Replace("{by}", "")
                    .Replace("{s}", RespSoonestS >= 0 ? $"{RespSoonestS} s" : "?").Replace("{police}", Game.SplitPoliceAt < LendInf ? MissionClock(Game.SplitPoliceAt) : "?");
                line = line.Trim();
            }
            return line.Length > 0 ? $"{line} · {MissionClock(c.Tick)}" : MissionClock(c.Tick);
        }

        /// <summary>Our car is still boxed in (engine 0.40.0 car.box: GetawayBoxers).</summary>
        bool StillBoxed() { List<string> bx = null; Guard("boxers", () => bx = Game.GetawayBoxers()); return bx != null && bx.Count > 0; }
        /// <summary>The rival lookout is still up and watching Popovic's car, or following it (engine 0.41.0 rivals.lookout): not down or paid; a misled one is walking away.</summary>
        bool LookoutWatching() => Game.Crew.Exists(m => !m.Gun.Down && !m.Paid && (m.Mode == CrewMode.Posted && m.Post == "lookout" || m.Mode == CrewMode.Walking && m.Goal is "lookout" or "follow"));
        Vector3? LookoutAt() { var m = Game.Crew.Find(x => x.Post == "lookout" || x.Goal == "lookout"); return m == null ? (Vector3?)null : frame.World(m.Place); }

        /// <summary>A click on the card: it is read and the next one shows.</summary>
        public void CrucialDismiss() { if (CrucialQueue.Count == 0) return; CrucialQueue[0].Read = true; CrucialQueue.RemoveAt(0); }

        /// <summary>A click on the card's place: the view pans there; the card stays.</summary>
        public void CrucialPan()
        {
            var c = CrucialNow; if (c == null) return;
            var t = c.View.Length > 0 ? views?.Find(c.View) : null;
            if (t && t.gameObject.activeInHierarchy) LookAt(t.position); else if (c.Place is Vector3 at) LookAt(at);
        }
    }
}
