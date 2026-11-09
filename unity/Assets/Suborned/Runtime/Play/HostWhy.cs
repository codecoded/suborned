using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 100, the why (Sky 2026-11-07, playing: "It would be good to explain why an alert raised, or why police came"): one row for each Alert
    /// rise and each police call, worded by Fable's `why` block (text.json, PlayText.Why) from the host's own reads, never the engine's sentences:
    ///  · an Alert rise is a Game.Ledger row ("alert", its cause): its key by WhyKeyOf; who reported it from the report that landed on that tick (Game.Reps,
    ///    the attention, car and camera reports, the cut screens, the entry report: HostWhy.Reports), the shot that raised it (Game.ShotsThisTick), the
    ///    act's place (TdAct.Scene) or the reporter's;
    ///  · a police call is a log code (security.called, security.gang, alarm.dispatches, response.reinforces, doors.lock, fight.loud, split.police, seal.starts)
    ///    keyed from structured state: the live seal origin (Game.SealOrigin: what was reported and by whom, set on that tick), the scene holds, the response.
    /// The crucial Alert and Police cards take their row's short and place (HostCrucial), the meter lists them (HudToolkit.Why), the Journal keeps the line.
    /// Where the engine gives no cause the row says nothing rather than guess (the gaps: briefs/100/latch/notes.md). Read only: nothing here changes the run.</summary>
    public sealed partial class HandoverHost
    {
        public sealed class WhyRow
        {
            public int Id, Tick, From, To; public string What = "alert", Key = "", Variant = "", Cause = "", Title = "", Short = "", Line = "", Where = "", Again = "";
            public bool ByCount; public Vector3? Place;
            /// <summary>The short and its place ("shots fired at the Office door"): the card's line and the meter's row; "" when nothing could be worded.</summary>
            public string ShortWhere => Short.Length == 0 ? "" : Where.Length > 0 ? $"{Short} {Where}" : Short;
        }
        /// <summary>Every rise and call this run, oldest first.</summary>
        public readonly List<WhyRow> WhyRows = new();
        /// <summary>The host check reads it: a why that threw (it is caught, the run goes on, the row says nothing).</summary>
        public int WhyErrors { get; private set; }
        int whyLedgerAt, whyLogAt, whyHolds, whyPassFrom;
        /// <summary>The police calls read this pass, by the tick they were logged at (an order's own consequences are logged as it is given, between steps).</summary>
        readonly List<(int tick, string key)> policeNow = new();

        void WhyReset()
        {
            WhyRows.Clear(); WhyErrors = 0; policeNow.Clear(); WhyReportsReset();
            whyLedgerAt = Game?.Ledger.Count ?? 0; whyLogAt = Game?.Log.Count ?? 0; whyHolds = Game?.SceneHolds.Count ?? 0;
            if (Game != null && Game.Alert > 0 && Text?.HasWhy == true) { var r = Row(Game.Tick, "alert", "start", "", 0, null); r.To = Game.Alert; Word(r, "alert", null, new()); } // an Alert set at the start
        }

        void WhySafe(string what, Action a) { try { a(); } catch (Exception e) { WhyErrors++; if (WhyErrors <= 3) Debug.LogWarning($"Play: the why for {what} failed and says nothing ({e.GetType().Name}: {e.Message})"); } }

        /// <summary>Once a tick (from CrucialTrack, before its cards): the reports that moved, then the ledger's Alert rows, then the log's police calls. Each is read at
        /// its own tick: an order given between steps logs its consequences (a failed takedown's call) at the tick it was given, read here at the next.</summary>
        void WhyTrack()
        {
            var g = Game; if (g == null || Text == null || !Text.HasWhy) return;
            whyPassFrom = WhyRows.Count;
            WhySafe("reports", WhyReports);
            if (whyLedgerAt > g.Ledger.Count) whyLedgerAt = g.Ledger.Count;
            for (; whyLedgerAt < g.Ledger.Count; whyLedgerAt++) { var (tick, what, d, now, cause) = g.Ledger[whyLedgerAt]; if (what == "alert") WhySafe(cause, () => AlertRow(tick, now - d, now, cause)); }
            if (whyLogAt > g.Log.Count) whyLogAt = g.Log.Count;
            policeNow.Clear(); for (; whyLogAt < g.Log.Count; whyLogAt++) { var l = g.Log[whyLogAt]; WhySafe(l.Code, () => PoliceCode(l)); }
            if (policeNow.Count > 0) WhySafe("police", PoliceRows);
            whyHolds = g.SceneHolds.Count;
        }

        WhyRow Row(int tick, string what, string key, string cause, int from, Pt? at)
        {
            var r = new WhyRow { Id = ++feedIds, Tick = tick, What = what, Key = key ?? "", Cause = cause ?? "", From = from, To = Game.Alert, Place = WhyWorld(at) };
            WhyRows.Add(r); return r;
        }

        /// <summary>The row's words from the text: a variant over its base, filled; a text left with a fill nothing filled is dropped, never shown half-done.</summary>
        void Word(WhyRow r, string group, string variant, Dictionary<string, string> f)
        {
            r.Variant = variant ?? "";
            var w = Text.Words(group, r.Key, variant); if (w == null) return;
            f["alert"] = r.To.ToString();
            string T(string s, bool cap) { var o = PlayText.FillWhy(s, f, cap); return PlayText.Unfilled(o) ? "" : o; }
            r.Title = T(w.Title, true); r.Short = T(w.Short, false); r.Line = T(w.Line, true);
            r.Where = NoPlace(r.Key) || !f.TryGetValue("where", out var wh) ? "" : wh;
            if (r.ByCount && Text.WhyAgainLine.Length > 0) r.Again = Text.WhyAgainLine;
        }
        /// <summary>The causes with no place (Fable's meter rule: start, more, cordon, loud, split; and the seal, which is the clock's).</summary>
        static bool NoPlace(string key) => key is "start" or "more" or "cordon" or "loud" or "split" or "seal" or "gang" or "staff" or "let-go" or "cut" or "loiter"; // a cut camera names its own place; brief 102 (Fable, guide /24): the loitering card's short names who saw whom, its place in the hover line

        // ---------------------------------------------------------------- the Alert rises

        void AlertRow(int tick, int from, int to, string cause)
        {
            var row = AlertWords(cause, from, to, true, tick);
            if (row != null) row.To = to;
        }

        /// <summary>The words for an Alert cause (an engine ledger cause), as a row; `keep` adds it to WhyRows (a report that landed at the Alert already reached
        /// is worded the same way, for the police call it set off, and not kept).</summary>
        WhyRow AlertWords(string cause, int from, int to, bool keep, int tick)
        {
            var g = Game; var key = PlayText.WhyKeyOf(cause); var p = cause.Split(':');
            var f = new Dictionary<string, string>(); string variant = null; Pt? at = null; bool byCount = false;
            string Rest(int i, int less = 0) => p.Length - less > i ? string.Join(":", p, i, p.Length - i - less) : "";
            switch (key)
            {
                case "seen": case "carry": case "dead": case "down": case "woken": case "noise": case "mark": case "missing":
                    {
                        string victim = Rest(2); var act = ActByVictim(victim); var land = LandedFor(cause);
                        f["about"] = WhyWho(victim);
                        string by = land?.By ?? NoticedNow(act);
                        if (by.Length > 0) f["who"] = WhoAbout(by, victim); else if (key is "seen" or "carry") f["who"] = WhyBy("witness");
                        at = act?.Scene ?? land?.At;
                        bool vale = g.PersonByRole("councillor") is Person vp && (victim == vp.Id || victim == "councillor");
                        if (key == "seen" && act != null && act.Take) variant = "vale";
                        if (key == "carry" && vale) variant = "vale";
                        if (key == "woken" && vale && (by == "councillor" || g.PersonById(by)?.Role == "councillor")) variant = "vale";
                        if (key == "mark") { f["clock"] = MissionClock(tick); if (!((g.PersonById(victim) ?? g.PersonByRole(victim))?.KRoutine ?? false)) variant = "unknown"; }
                        byCount = to > OwnLevel(key, act);
                        break;
                    }
                case "cut": { var cam = p.Length >= 4 ? p[3] : ""; f["about"] = WhyWho("camera:" + cam); at = WhyPos("camera:" + cam); break; }
                case "approach": f["who"] = WhyWho(p[1]); at = WhyPos(p[1]); break;
                case "staff": case "let-go": at = g.S.HasPlace(g.S.GuardPost) ? g.S.At(g.S.GuardPost) : null; break;
                case "confirmed": { var victim = Rest(2); f["about"] = WhyWho(victim); at = ActByVictim(victim)?.Scene; break; }
                case "gunfire": Gunfire(Rest(1), tick, f, ref variant, ref at); break;
                case "violence":
                    if (p.Length >= 3 && p[1] == "blast") { variant = "blast"; at = ShotAtThing(Rest(2), tick); }
                    else if (p.Length == 2) { variant = "shot"; f["about"] = WhyWho(p[1]); at = WhyPos(p[1]) ?? ShotAtThing(p[1], tick); }
                    else { string target = Rest(1, 1), witness = p[^1]; f["who"] = WhoAbout(witness, target); f["about"] = WhyWho(target); at = WhyPos(target) ?? ShotAtThing(target, tick) ?? WhyPos(witness); }
                    break;
                case "drawn": case "warned": { string w = Rest(1, 1); f["who"] = WhyWho(w); f["agent"] = TheAgent(p[^1]); at = WhyPos(p[^1]); break; }
                case "drawn-people": f["agent"] = TheAgent(p[^1]); at = WhyPos(p[^1]); break;
                case "fought": f["about"] = WhyWho(Rest(1)); at = WhyPos(Rest(1)); break;
                case "loiter": { string w = Rest(1, 1); f["who"] = WhyWho(w); f["agent"] = TheAgent(p[^1]); at = LandedFor(cause)?.At ?? WhyPos(p[^1]); break; }
                case "recorded": if (p.Length >= 5) { if (p[1] == "pressure") variant = "pressure"; f["about"] = WhyWho(p[2]); f["who"] = WhyWho("camera:" + p[4]); at = LandedFor(cause)?.At ?? WhyPos(p[2]); } break;
                case "car": f["who"] = WhyWho(Rest(1)); at = LandedFor(cause)?.At ?? g.GetawayCarPos(); break;
                case "entry": at = g.S.HasPlace(g.S.GuardPost) ? g.S.At(g.S.GuardPost) : null; break;
                case "sighting":
                    {
                        string w = Rest(1); f["who"] = WhyWho(w); Sighting s = null;
                        for (int i = g.Sightings.Count - 1; i >= 0; i--) if (g.Sightings[i].Watcher == w && g.Sightings[i].From == tick) { s = g.Sightings[i]; break; }
                        if (s != null) { f["agent"] = TheAgent(s.Op); at = s.At; }
                        break;
                    }
            }
            f["where"] = WhyWhere(at);
            WhyRow row;
            if (keep) { row = Row(tick, "alert", key, cause, from, at); }
            else row = new WhyRow { Tick = tick, What = "alert", Key = key ?? "", Cause = cause, From = from, To = to, Place = WhyWorld(at) };
            row.To = to; row.ByCount = byCount;
            if (key != null) Word(row, "alert", variant, f);
            return row;
        }

        /// <summary>gunfire:&lt;x&gt; names the target when the cell fires and the shooter when anyone else does: the round that raised it this tick says which
        /// (Game.ShotsThisTick, in the order they resolved). The cell's: {agent} fired at {about}; another's: {who} opened fire (variant other).</summary>
        void Gunfire(string x, int tick, Dictionary<string, string> f, ref string variant, ref Pt? at)
        {
            foreach (var s in Game.ShotsThisTick)
            {
                if (s.Tick != tick) continue;
                if (IsOpName(s.By) && s.Target == x) { f["agent"] = TheAgent(s.By); f["about"] = WhyWho(x); at = Game.PersonById(x) != null ? s.At : s.From; return; }
                if (s.By == x && !IsOpName(x)) { variant = "other"; f["who"] = WhyWho(x); at = s.From; return; }
            }
        }
        /// <summary>Where a round this tick landed on a thing (a car blown up, a person fired on), when the engine keeps no other place for it.</summary>
        Pt? ShotAtThing(string id, int tick) { foreach (var s in Game.ShotsThisTick) if (s.Tick == tick && s.Target == id) return s.At; return null; }

        /// <summary>The takedown a report is about: the latest act on that victim (TdAct.Victim is the engine's id, a role for the cast).</summary>
        TdAct ActByVictim(string victim) { var acts = Game.Acts; for (int i = acts.Count - 1; i >= 0; i--) if (acts[i].Victim == victim) return acts[i]; return null; }

        /// <summary>A report kind's own Alert (rulebook alert.*; a knockout at most takedown.koAlertMax): above it, the count of incidents raised it (`again`).</summary>
        int OwnLevel(string kind, TdAct act)
        {
            var R = Game.R;
            int sev = kind switch { "seen" => R.AlertSeen, "carry" => R.AlertCarry, "dead" => R.AlertDead, "down" => R.AlertDown, "woken" => R.AlertWoken, "noise" => R.AlertNoise, "mark" => R.AlertMark, "missing" => R.AlertMissing, _ => 3 };
            if (!(act?.Kill ?? false) && kind is "down" or "dead" or "woken" or "noise" or "mark" or "missing") sev = Math.Min(sev, R.KoAlertMax);
            return sev;
        }

        // ---------------------------------------------------------------- the police calls

        /// <summary>A log code that is a police call: its key from the engine's structured state on this tick; PoliceRows words them once the tick's codes are in.</summary>
        void PoliceCode(LogLine l)
        {
            var g = Game; string key = l.Code switch
            {
                "security.gang" => "gang", "security.called" => CalledKey(l.Tick), "alarm.dispatches" => g.Alert < 2 && g.SecurityArrives >= int.MaxValue / 8 ? "robots" : "report",
                "response.reinforces" => "more", "doors.lock" => "cordon", "fight.loud" => "loud", "split.police" => "split", "seal.starts" => "seal", _ => null,
            };
            if (key != null) policeNow.Add((l.Tick, key));
        }
        /// <summary>security.called: what the live seal origin says was reported on this tick (gunfire, an attack, a drawn weapon, an empty post); a held scene a body
        /// found; a gang shooting still filed; else a report that reached the desk.</summary>
        string CalledKey(int tick)
        {
            var g = Game; var o = g.SealOrigin;
            if (o != null && o.Tick == tick && o.Report.Length > 0)
            {
                var p = o.Report.Split(':'); string rest = string.Join(":", p, 1, p.Length - 1);
                switch (p[0])
                {
                    case "gunfire":
                        if (HostViews.IsCrewId(rest) || rest is "car:rivals" or "rivals") return "rivals";
                        if (rest == "patrol") return "patrol";
                        if (rest is "waitress" or "driver" or "councillor" || g.PersonById(rest) is Person cp && cp.Civilian) return "shot-at";
                        return o.Reporter == null || o.Reporter.Id == "heard" ? "heard" : "witness";
                    case "attack": return "attack";
                    case "drawn-seen": return "drawn";
                    case "post-empty": return "post";
                }
            }
            if (g.SceneHolds.Count > whyHolds) return "body";
            if (g.GangPending) return "gang";
            return "report";
        }

        /// <summary>This pass's police calls as rows, a tick at a time: the most telling one (a report-reached-the-desk or a robots call gives way to a call that says what
        /// was seen), each other kind once; {cause} is the Alert rise of the same tick, else the report that landed now.</summary>
        void PoliceRows()
        {
            var g = Game;
            for (int i = 0; i < policeNow.Count;)
            {
                int tick = policeNow[i].tick, j = i; var keys = new List<string>(); while (j < policeNow.Count && policeNow[j].tick == tick) keys.Add(policeNow[j++].key); i = j;
                bool specific = keys.Exists(k => k is not ("report" or "robots" or "more" or "seal" or "cordon"));
                var done = new HashSet<string>();
                foreach (var key in keys)
                {
                    if (!done.Add(key) || specific && key is "report" or "robots") continue;
                    if (key is "report" && done.Contains("robots") || key is "robots" && done.Contains("report")) continue;
                    WhySafe("police " + key, () => PoliceRow(tick, key));
                }
            }
        }
        void PoliceRow(int tick, string key)
        {
            var g = Game; var f = new Dictionary<string, string>(); Pt? at = null; string variant = null; WhyRow cause = key is "report" or "robots" ? CauseNow(tick) : null;
            switch (key)
            {
                case "heard": case "witness": case "shot-at": case "patrol": case "rivals": case "drawn": case "attack":
                    {
                        var o = g.SealOrigin; var p = o.Report.Split(':'); string rest = string.Join(":", p, 1, p.Length - 1);
                        at = o.ScenePlaced ? o.Scene : (Pt?)null;
                        if (o.Reporter != null) f["who"] = WhyWho(o.Reporter.Id);
                        if (key is "shot-at" or "attack") f["about"] = WhyWho(rest);
                        if (key == "drawn") { f["agent"] = TheAgent(p[^1]); at ??= WhyPos(p[^1]); }
                        break;
                    }
                case "post": at = g.S.HasPlace(g.S.GuardPost) ? g.S.At(g.S.GuardPost) : null; if (!(g.PersonByRole("guard")?.KRoutine ?? false)) variant = "unknown"; break;
                case "body": at = g.SceneHolds.Count > 0 ? g.SceneHolds[^1].At : null; break;
                case "gang": at = g.GangScenePoints.Count > 0 ? g.GangScenePoints[0] : null; break;
                case "report": case "robots": if (cause != null) f["cause"] = cause.Short; break;
                case "loud": if (g.Gunfire == 0) variant = "alert"; break;
                case "seal": if (g.SealOrigin is CauseOrigin so && so.Report.Length > 0 && OriginWords(so) is WhyRow c) f["cause"] = c.Short; f["clock"] = g.Lockdown < int.MaxValue / 8 ? MissionClock(g.Lockdown) : ""; break;
            }
            f["where"] = cause != null ? cause.Where : WhyWhere(at);
            var r = Row(tick, key == "seal" ? "seal" : "police", key, key, g.Alert, at);
            if (cause != null) r.Place = cause.Place;
            Word(r, "police", variant, f);
        }
        /// <summary>{cause} for a call that a report set off: the Alert rise of its tick read this pass, else the report that landed now (worded as a rise is, not kept).</summary>
        WhyRow CauseNow(int tick)
        {
            var g = Game;
            for (int i = WhyRows.Count - 1; i >= whyPassFrom; i--) if (WhyRows[i].What == "alert" && WhyRows[i].Tick == tick && WhyRows[i].Short.Length > 0) return WhyRows[i];
            foreach (var l in landedNow) { var w = AlertWords(l.Cause, g.Alert, g.Alert, false, tick); if (w.Short.Length > 0) return w; }
            return null;
        }
        /// <summary>The words of what the seal clock began from (Game.SealOrigin): the Alert rise of its tick (the shot, the drawn gun, the failed takedown, the empty
        /// post all raise one), else its report worded as a rise; null when neither says anything.</summary>
        WhyRow OriginWords(CauseOrigin o)
        {
            for (int i = WhyRows.Count - 1; i >= 0; i--) if (WhyRows[i].What == "alert" && WhyRows[i].Tick == o.Tick && WhyRows[i].Short.Length > 0) return WhyRows[i];
            string cause = o.Report; var p = cause.Split(':');
            if (p[0] == "drawn-seen" && p.Length == 2 && o.Reporter != null) cause = $"drawn-seen:{o.Reporter.Id}:{p[1]}"; // the origin names the watcher apart (the ledger's cause holds both)
            if (PlayText.WhyKeyOf(cause) == null) return null;
            var w = AlertWords(cause, Game.Alert, Game.Alert, false, o.Tick);
            return w.Short.Length > 0 ? w : null;
        }

        /// <summary>A row in sentences for the Journal and the meter's hover: its line, `again` when the count of reports raised it, and what the Alert set off (effect).</summary>
        public string WhyFull(WhyRow w)
        {
            if (w == null) return "";
            string effect = w.What == "alert" && Text != null && Text.WhyEffect.TryGetValue(w.To.ToString(), out var e) ? e : "";
            return WhyJoin(WhyJoin(w.Line, w.Again), effect);
        }
        static string WhyJoin(string a, string b) => string.IsNullOrEmpty(a) ? b ?? "" : string.IsNullOrEmpty(b) ? a : a + " " + b;

        // ---------------------------------------------------------------- the meter's list (part 3)

        /// <summary>What the Alert meter lists, newest first (Fable's meter rule): every Alert rise, and the police calls but for one that only repeats the rise of its
        /// tick (a report that reached the desk); the same call again ("More police") is listed once, at its newest; at most `max` rows.</summary>
        public List<WhyRow> WhyMeterRows(int max = 6)
        {
            var o = new List<WhyRow>(); string lastPolice = "";
            for (int i = 0; i < WhyRows.Count; i++)
            {
                var r = WhyRows[i];
                if (r.What == "police")
                {
                    if (r.Short.Length == 0 && r.Title.Length == 0) continue;
                    if (r.Key is "report" or "robots" && WhyRows.Exists(x => x.What == "alert" && x.Tick == r.Tick)) continue;
                    if (r.Key == lastPolice && o.Count > 0) { o[^1] = r; continue; } // the same call again ("More police"): its newest stands for both
                    lastPolice = r.Key;
                }
                else if (r.What != "alert") continue;
                else lastPolice = "";
                o.Add(r);
            }
            o.Reverse(); if (o.Count > max) o.RemoveRange(max, o.Count - max);
            return o;
        }
        /// <summary>The meter's two lines for a row: `when` ("2:33 · Alert 2", a police call "2:33 · Police coming", the start "Alert 1 from the start") over `what` ("shots fired at the Office door").</summary>
        public (string when, string what) WhyMeterLines(WhyRow r)
        {
            if (Text == null || r == null) return ("", "");
            string M(string k) => Text.WhyMeter.TryGetValue(k, out var v) ? v : "";
            var f = new Dictionary<string, string> { ["clock"] = MissionClock(r.Tick), ["alert"] = r.To.ToString(), ["title"] = r.Title, ["short"] = r.Short, ["where"] = r.Where };
            string F(string x) { var t = PlayText.FillWhy(x, f, false).Trim(); return PlayText.Unfilled(t) ? "" : t; }
            if (r.Key == "start") return (F(M("start")), "");
            string what = r.Where.Length > 0 ? M("what") : M("what").Replace("{where}", ""); // a cause with no place drops it (Fable's meter rule)
            return (F(r.What == "police" ? M("police") : M("when")), r.Short.Length > 0 ? F(what) : "");
        }
        /// <summary>The meter's head ("Why it's Alert 2"), or `empty` while nothing has raised it.</summary>
        public string WhyMeterHead()
        {
            if (Text == null || Game == null) return "";
            string k = WhyRows.Exists(r => r.What == "alert") || Game.Alert > 0 ? "head" : "empty";
            return Text.WhyMeter.TryGetValue(k, out var v) ? PlayText.FillWhy(v, new Dictionary<string, string> { ["alert"] = Game.Alert.ToString() }) : "";
        }

        /// <summary>The row of a kind ("alert", "police") read this pass for the crucial card this pass makes: the newest rise; the first call that says why the police
        /// come (a lockdown or more police of the same tick only when there is no other); null when there is none (or no why).</summary>
        public WhyRow WhyNow(string what)
        {
            if (what == "police") for (int i = whyPassFrom; i < WhyRows.Count; i++) if (WhyRows[i].What == what && WhyRows[i].Key is not ("cordon" or "more")) return WhyRows[i]; // the call that says why they come, before the lockdown it brings
            for (int i = WhyRows.Count - 1; i >= whyPassFrom; i--) if (WhyRows[i].What == what) return WhyRows[i];
            return null;
        }
    }
}
