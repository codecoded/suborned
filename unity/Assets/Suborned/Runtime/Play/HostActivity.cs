using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>What each agent is doing, cancelling it, the briefing and the dialogue log (Sky 2026-10-15: "there's nothing showing me what the
    /// cell player is doing when they're busy, nor is there a way to cancel their action"; "everything is very unclear on what to do").
    /// The activity is the engine's (Game.ActivityOf, read-only, every tick) plus this host's own queue: the act an agent walks to give
    /// (walk-then-act), shown as "then: Listen". Cancel is the engine's cancel order; the queued act is dropped with it. Every line in the
    /// dialogue log comes from the run (an order given, a refusal, the engine's log), its words from PlayText, so a replay reads the same.</summary>
    public sealed partial class HandoverHost
    {
        public PlayText Text { get; private set; }

        /// <summary>One agent's activity as the HUD shows it: the engine's, named for the player, with the host's queued act.</summary>
        public sealed class Doing
        {
            public string Verb = "", Phase = "idle", Label = "Ready", Glyph = "", Queued = "", QueuedGlyph = "", OnDone = "", CancelText = "", CancelVerb = "", CancelCode = "";
            public int Progress = -1;   // per mille, -1 = no bar (idle, open-ended)
            public int TicksLeft = -1;  // -1 = open-ended
            public bool CanCancel;
            public Vector3? ActAt;      // walking to an act: where the act happens (the HUD draws its icon there)
            public bool Busy => Phase != "idle";
        }

        readonly Doing[] doing = { new(), new(), new(), new() };
        public Doing DoingOf(int k) => doing[k];
        readonly List<Vector3> actLine = new();

        /// <summary>Recomputed after every step and every order (the HUD only reads it).</summary>
        void Track()
        {
            if (Game == null || views == null) return;
            for (int k = 0; k < 4; k++)
            {
                Activity a = null; if (!Guard("activity", () => a = Game.ActivityOf((OpId)k))) return;
                var d = doing[k]; var op = Game.Ops[k]; var q = pending[k];
                d.Verb = a.Verb; d.Phase = a.Phase; d.OnDone = a.OnDone; d.CanCancel = a.CanCancel || q != null;
                d.CancelCode = a.CancelCost ?? "";
                d.CancelText = Text != null && Text.CancelCosts.TryGetValue(a.CancelCost, out var cct) ? cct : a.CancelText;
                d.Progress = a.TicksLeft < 0 || a.Phase is "idle" or "down" ? -1 : Mathf.Clamp(a.Progress, 0, 1000);
                d.TicksLeft = a.Phase == "firing" ? -1 : a.TicksLeft;
                d.Queued = q == null ? "" : "then: " + ActName(q.Do, q.Approach, q.Target, true);
                d.QueuedGlyph = q == null ? "" : Glyph(q.Do, q.Approach);
                d.Glyph = Glyph(a.Verb, op.Busy?.Approach ?? "");
                d.ActAt = q != null ? pendingAt[k] : null;
                string who = a.Phase == "holding" && a.Verb == "carry" && IsOpName(a.Target) ? "the " + Cap(a.Target) : Name(a.Target);
                string textVerb = a.Verb == "turn" ? op.Busy?.Approach ?? "" : a.Verb;
                string said = Text?.Doing(a.Phase == "acting" && a.Verb == "holster" ? "holstering" : a.Phase, a.Phase is "acting" or "holding" ? textVerb : "", who);
                d.Label = op.Inside && a.Phase == "idle" ? "Inside the Office" : said != null ? said + (a.Phase is "walking" or "running" && q != null ? $" to {ActPlaceName(k, q)}" : "") : a.Phase switch
                {
                    "idle" => op.Inside ? "Inside the Office" : "Ready",
                    "down" => "Down",
                    "walking" or "running" => (a.Phase == "running" ? "Running" : "Walking") + (q != null ? $" to {ActPlaceName(k, q)}" : ""),
                    "firing" => $"Firing on {Name(a.Target)}",
                    "drawing" => "Drawing",
                    "holding" => a.Verb == "loop" ? "Holding the loop" : a.Verb == "take" ? "Holding Vale: to the car" : a.Verb == "carry" ? $"Carrying the {a.Target}" : "Holding",
                    _ => Doingly(a.Verb, op.Busy?.Approach ?? "", a.Target),
                };
                // what the cancel stops: the engine's activity; walking to an act, the act is dropped as well
                d.CancelVerb = a.Phase is "walking" or "running" && q != null ? ActName(q.Do, q.Approach, q.Target, false) : a.Phase is "walking" or "running" ? "the walk" : a.Phase == "firing" ? "fire" : ActName(a.Verb, op.Busy?.Approach ?? "", a.Target, false);
                if (a.Phase is "walking" or "running" && q != null) d.CancelText = $"{d.CancelText} {ActName(q.Do, q.Approach, "", false)} is dropped.";
                else if (!a.CanCancel && q != null) d.CancelText = $"{ActName(q.Do, q.Approach, "", false)} is dropped.";

                // the rest of a walk to an act, on the ground
                if (q != null && op.Walk != null && !op.Inside)
                {
                    actLine.Clear(); var pts = op.Walk.Points; long done = op.WalkMm, run = 0;
                    actLine.Add(frame.World(op.Pos) + Vector3.up * 0.08f);
                    for (int i = 1; i < pts.Length; i++) { run += G.Isqrt(G.D2(pts[i - 1], pts[i]) * 100); if (run > done) actLine.Add(frame.World(pts[i]) + Vector3.up * 0.08f); }
                    views.ShowAct(k, actLine);
                    d.ActAt = frame.World(op.Walk.End);
                }
                else views.ShowAct(k, null);
            }
            FeedTrack();
            SceneBubbles(); TrackVale(); // brief 014
        }

        string Name(string key) => key switch
        {
            "" => "", "hostiles" => "the hostiles", _ when key.StartsWith("camera:") => CameraName(key[7..]),
            _ when Game != null && !IsOpName(key) && key is not ("waitress" or "driver" or "guard" or "councillor") && Game.PersonById(key) is Person cp => PersonName(cp), // a passer-by (brief 013)
            _ when Game != null && Game.S.Stashes.Find(x => x.Id == key) is StashPlace sp => StashName(sp),
            _ => Text?.TargetName(key, key.StartsWith("security") ? "the response unit" : "the " + key) ?? key,
        };

        string ActPlaceName(int k, Order q) => q.Do switch
        {
            "loop" or "reroute" => "the cabinet", "enter" => "the public door", "extract" => "the getaway car", "take" => "Vale",
            _ => q.Target.Length > 0 ? Name(q.Target) : "the act",
        };

        /// <summary>"Pay Inez", "Watch", "Loop camera": an act's name from the text's action labels.</summary>
        string ActName(string verb, string approach, string target, bool withTarget)
        {
            var key = verb == "turn" ? approach : verb;
            var label = Text?.Action(key, target).label ?? key;
            bool person = target is "waitress" or "driver" or "guard" or "councillor";
            return withTarget && person && verb != "take" ? $"{(label == "Listen" ? "Listen to" : label)} {Name(target)}" : label;
        }

        /// <summary>The roster's line for an act in progress: "Watching Inez", "Paying the driver", "Looping the camera".</summary>
        string Doingly(string verb, string approach, string target)
        {
            string who = Name(target);
            return (verb == "turn" ? approach : verb) switch
            {
                "watch" => $"Watching {who}", "listen" => $"Listening to {who}", "ask" => $"Asking {who}", "pay" => $"Paying {who}", "help" => $"Helping {who}",
                "pressure" => $"Pressuring {who}", "loop" => "Looping the camera", "reroute" => "Rerouting the patrol", "mislead" => "Tipping off the rivals",
                "enter" => "Inside: finding Vale", "take" => "Taking Vale", "carry" => $"Lifting the {target}", "holster" => "Holstering", "call" => $"Calling {who}",
                var v => v.Length > 0 ? char.ToUpperInvariant(v[0]) + v[1..] : "Busy",
            };
        }

        public static string Glyph(string verb, string approach) => verb switch
        {
            "watch" => "eye", "listen" => "ear", "turn" => approach == "pay" ? "$" : approach, "call" => "call", "loop" => "loop", "reroute" => "route",
            "mislead" => "tip", "enter" => "in", "take" => "take", "extract" => "car", "fire" => "fire", "move" => "›", "draw" => "draw", "carry" => "lift", "takedown" => "takedown", "restrain" => "restrain", "stash" => "stash", "drop" => "drop", "" => "", _ => verb,
        };

        /// <summary>Cancel costs that lose nothing (the text's cancelCosts keys): a walk stopped, a weapon holstered or a fire ceased, nothing under way.</summary>
        static readonly HashSet<string> CancelFree = new() { "", "stop", "holster", "cease", "nothing", "none", "down" };

        /// <summary>Cancel for one agent (the roster ✕, the in-world ✕): the queued act is dropped, and the engine's cancel order stops what it shows.
        /// Silent (Glyph #4, Fable E3, §4): the strip shows the agent stopped; only a cancel that loses something (time spent, money back but a
        /// face remembered, the loop gone) closes its card, or opens one, with that cost. A walk stopped is not an event.</summary>
        public void CancelAgent(int k, Vector2? screen)
        {
            if (Game == null || Fault != null || Game.Over) return;
            var d = doing[k]; string cost = d.CancelText, what = d.CancelVerb.Length > 0 ? d.CancelVerb : "it";
            bool walking = d.Phase is "walking" or "running", costly = !walking && !CancelFree.Contains(d.CancelCode);
            pending[k] = null; pendingAt[k] = null;
            Activity a = null; Guard("activity", () => a = Game.ActivityOf((OpId)k));
            if (a != null && a.CanCancel && costly) FeedCancelled(k, cost, what);
            else CloseActCard(k);
            if (a != null && a.CanCancel) Issue(new Order { Op = Op.Name((OpId)k), Do = "cancel" }, null, null, true);
            Track(); intentKey = "";
        }

        /// <summary>The cancel key (C): every selected agent (nothing to cancel says nothing).</summary>
        public void CancelSelected()
        {
            int n = 0; for (int k = 0; k < 4; k++) if (Selected[k] && (doing[k].CanCancel || pending[k] != null)) { CancelAgent(k, null); n++; }
            if (n == 0) Status = "nothing to cancel";
        }

        // ---------------------------------------------------------------- the briefing and the dialogue log

        /// <summary>The briefing card: open at the start (the clock paused), B brings it back.</summary>
        public bool ShowBriefing { get; set; }
        /// <summary>The dialogue log, expanded (L or its header) or collapsed to its last line.</summary>
        public bool LogOpen { get; set; }

        public sealed class Said { public int Tick; public float Born; public string Speaker = "", Name = "", Text = ""; public int Agent = -1; public Feedback Kind; }
        public readonly List<Said> Talk = new();

        void Say(string speaker, string text, Feedback kind = Feedback.Info)
        {
            if (string.IsNullOrEmpty(text)) return;
            int agent = Array.IndexOf(OpKeys, speaker);
            Talk.Add(new Said { Born = Time.unscaledTime, Tick = Game.Tick, Speaker = speaker, Name = Text?.Speaker(speaker) ?? speaker, Text = text, Agent = agent, Kind = kind });
            FeedSay(speaker, text);
            PushBubble(speaker, text); // brief 014: speech is a bubble at the speaker
            while (Talk.Count > 200) Talk.RemoveAt(0);
        }

        static readonly string[] OpKeys = { "scout", "fixer", "tech", "enforcer" };

        string Fill(string line, string who, int m = 0, long usd = 0, string op = "") =>
            line.Replace("{who}", who).Replace("{op}", op).Replace("{m}", m.ToString()).Replace("{usd}", $"${usd:N0}");

        /// <summary>One beat: the line the text has for (event, verb, speaker, …), picked deterministically, into the log. False when there is none.</summary>
        bool Beat(string ev, string verb, string speaker, string outcome = null, string temperament = null, string about = null, string who = "", Feedback kind = Feedback.Info)
        {
            if (Text == null) return false;
            var line = Text.Pick(new PlayText.Query { Event = ev, Verb = verb, Speaker = speaker, Outcome = outcome, Temperament = temperament, About = about });
            if (line == null) return false;
            int k = Array.IndexOf(OpKeys, speaker);
            Say(speaker, Fill(line, who, 0, 0, k >= 0 ? AgentNames[k] : ""), kind);
            return true;
        }

        /// <summary>A world or end event (the text's `events`): up to `most` of its speakers who have a line say it.</summary>
        readonly Dictionary<string, int> beatAt = new();

        void WorldBeat(PlayText.EventText e, string outcome, string who, Feedback kind, int most)
        {
            // a city event that repeats (a drawn weapon seen each tick, gunfire) speaks at most once every 15 s
            if (e.Kind == "world" && beatAt.TryGetValue(e.Id, out var last) && Game.Tick - last < 15 * Game.Hz) return;
            beatAt[e.Id] = Game.Tick;
            int said = 0;
            foreach (var sp in e.Speakers)
            {
                if (said >= most) break;
                string temper = sp is "waitress" or "driver" or "guard" or "councillor" ? Game.PersonByRole(sp).Temperament : null;
                if (Beat(e.Id, null, sp, outcome, temper, null, who, kind)) said++;
            }
        }

        static string VerbOf(Order o) => o.Do == "turn" ? o.Approach : o.Do;

        /// <summary>An order given (a click, a panel card): the agent's `ack` beat; given on the spot (no walk first), the `arrive` beat follows.</summary>
        void SayOrder(OpId by, Order o, ActWalk w)
        {
            string speaker = Op.Name(by), verb = VerbOf(o);
            if (!Beat("ack", verb, speaker, who: Name(o.Target), kind: Feedback.Ok))
                Say(speaker, w != null ? $"Heading to {(w.Place.Length > 0 ? PlaceName(w.Place) : Name(o.Target))}." : "On it.", Feedback.Ok);
            if (w == null) SayArrive(by, o);
            if (o.Do == "call") FeedCall(o.Target);
        }

        /// <summary>The agent is in reach and the act begins (after a walk, the act FollowUp gives).</summary>
        void SayArrive(OpId by, Order o)
        {
            var verb = VerbOf(o);
            if (Text != null && Text.Exchange.TryGetValue(verb, out var ex) && ex.Contains("arrive")) { arriveSaying = true; try { Beat("arrive", verb, Op.Name(by), who: Name(o.Target)); } finally { arriveSaying = false; } } // Fable §6: arrive is history only
        }

        /// <summary>The refusal said for the player: the text's lock line for its code, filled (a port of `fillLock`); else the engine's words.</summary>
        public string Humanise(Order o, Answer a)
        {
            var line = LockLine(o, a);
            if (line != null) return line;
            string agent = o.Op.Length > 0 ? char.ToUpperInvariant(o.Op[0]) + o.Op[1..] : "";
            return $"{agent}: {a.why}";
        }

        /// <summary>A port of text.schema.ts `fillLock`: the lock line for Answer.code with {who} (the target), {op} (the agent), {m} (Answer.m,
        /// or cm / 100), {usd} ("$" and thousands) and {tick} (the mission clock) filled; "?" for a number the engine didn't give. Null for a
        /// code the text has no line for (and for an older text keyed by patterns, `lockOf` over the words).</summary>
        public string LockLine(Order o, Answer a)
        {
            if (Text == null) return null;
            var l = (a.code ?? "").Length > 0 ? Text.LockByCode(a.code) : null;
            l ??= Text.LockOf(a.why ?? "");
            if (l == null || l.Line.Length == 0) return null;
            string who = o.Target.Length > 0 ? Name(o.Target) : "them";
            string op = o.Op.Length > 0 && o.Op != "cell" ? AgentNames[(int)Op.Parse(o.Op)] : "The agent";
            string m = a.m >= 0 ? a.m.ToString() : a.cm >= 0 ? Math.Round(a.cm / 100.0, MidpointRounding.AwayFromZero).ToString() : "?";
            string usd = a.usd >= 0 ? "$" + a.usd.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) : "?";
            string tick = a.tick >= 0 ? MissionClock(a.tick) : "?";
            return l.Line.Replace("{who}", who).Replace("{op}", op).Replace("{m}", m).Replace("{usd}", usd).Replace("{tick}", tick);
        }

        // engine log codes that land an order's act, and the verb and agent whose exchange they belong to
        static readonly Dictionary<string, (string verb, string op)> Lands = new()
        {
            ["loop.on"] = ("loop", "tech"), ["patrol.rerouted"] = ("reroute", "tech"), ["rivals.misled"] = ("mislead", "scout"),
            ["enter.out"] = ("enter", "enforcer"), ["take.done"] = ("take", "enforcer"),
            ["takedown.out"] = ("takedown", "enforcer"), ["takedown.dead"] = ("takedown", "enforcer"),
        };

        /// <summary>The engine's log speaks, in the text's beats: what the Scout learned (`read`), a person's answer and the Fixer's reaction
        /// (`reply`, `done`), an act landing (`done` and the exchange's world beats, in exchange order), a contact's confirmation, the city's
        /// events, the end (`win` then the `unnamed` cliffhanger, or `fail` by code). A log code the text neither speaks nor silences is said
        /// in the engine's own words.</summary>
        void SayLanded(LogLine l)
        {
            if (Text == null) return;
            if (quiet.HasValue && quiet.Value.Tick == l.Tick && quiet.Value.Code == l.Code && quiet.Value.Text == l.Text) return; // its card is out of sight: so are its lines
            string code = l.Code, role = RoleIn(l);
            if (code.StartsWith("crowd.")) return; // the crowd speaks by its states (CityBeats): one passer-by near the cell, not the log's counts
            if (code is "scout.watch.watched" or "tech.watch.watched" && role == null)
            {   // a machine watched (Sky 2026-10-18: the Tech watches machines too): its sight learned, the patrol's rounds once known
                string speaker = code.StartsWith("tech.") ? "tech" : "scout";
                var mm = System.Text.RegularExpressions.Regex.Match(l.Text, @"watched the (\w+)( camera)?");
                string about = !mm.Success ? null : mm.Groups[2].Success ? "camera-" + mm.Groups[1].Value : mm.Groups[1].Value;
                string outcome = about == "patrol" ? (l.Text.Contains(", and its routine") ? "routine" : "sight") : null;
                if (about == null || !Beat("read", "watch", speaker, outcome, null, about, "", Feedback.Ok)) Say(speaker, l.Text, Feedback.Ok);
                return;
            }
            if (code.StartsWith("scout.") && role != null)
            {
                bool listen = code.StartsWith("scout.listen");
                var p = Game.PersonByRole(role);
                if (!Beat("read", listen ? "listen" : "watch", "scout", null, listen ? p.Temperament : null, role, Name(role), Feedback.Ok))
                    Say("scout", listen ? $"{Cap(Name(role))} {NeedWords(p.Need)}. {Cap(TemperWords(p.Temperament))}." : $"I have {Name(role)}'s routine.", Feedback.Ok);
                return;
            }
            if (code.StartsWith("turn.") && role != null)
            {
                var parts = code.Split('.'); string approach = parts.Length > 2 ? parts[1] : "", outcome = parts[^1];
                foreach (var x in Game.Log) if (x.Tick == l.Tick && x.Code == "report") { outcome = "suspicious"; break; } // reported: the `suspicious` voice
                var p = Game.PersonByRole(role);
                var kind = outcome == "turned" || outcome == "kept" ? Feedback.Ok : outcome == "refused" || outcome == "suspicious" ? Feedback.Refused : Feedback.Info;
                if (!Beat("reply", approach, role, outcome, p.Temperament, null, Name(role), kind)) Say(role, l.Text, kind);
                Beat("done", "turn", "fixer", outcome, null, null, Name(role), kind);
                return;
            }
            if (Lands.TryGetValue(code, out var land) && Text.Exchange.TryGetValue(land.verb, out var ex))
            {
                int from = Math.Max(0, ex.IndexOf("arrive") + 1);
                for (int i = from; i < ex.Count; i++)
                {
                    var id = ex[i];
                    if (id == "done") Beat("done", land.verb, land.op, kind: Feedback.Ok);
                    else if (Text.Events.TryGetValue(id, out var e) && e.Log.Contains(code)) WorldBeat(e, null, "", Feedback.Info, id == "taken" ? 1 : 2);
                }
                return;
            }
            if (code.StartsWith("call.") && Text.Events.TryGetValue("confirm", out var cf))
            {
                Beat("confirm", "call", code[5..], who: Name(code[5..]), kind: Feedback.Ok);
                foreach (var e in Text.Events.Values) if (e.Id != "confirm" && e.Kind == "world" && e.Log.Contains(code)) WorldBeat(e, null, "", Feedback.Info, 1);
                return;
            }
            if (code == "success")
            {
                if (Text.Events.TryGetValue("win", out var w)) WorldBeat(w, null, "", Feedback.Ok, 3);
                if (Text.Events.TryGetValue("cliffhanger", out var c)) WorldBeat(c, "unnamed", "", Feedback.Info, 2); // `named` waits on Sky
                return;
            }
            // an agent hit down speaks for themself (the event lists all four; the first with a line would always be the Scout)
            foreach (var ag in OpKeys) if (code == ag + ".down" && ag != "enforcer") { Beat("agent-down", null, ag, kind: Feedback.Refused); return; }
            if (code.StartsWith("fail.") && Text.Events.TryGetValue("fail", out var fl)) { WorldBeat(fl, code[5..], "", Feedback.Refused, 2); return; }
            bool spoke = false;
            foreach (var e in Text.Events.Values)
            {
                if (e.Kind is "order" or "reply" or "read" or "end") continue;
                foreach (var lc in e.Log)
                    if (code == lc || (lc.EndsWith(".") && code.StartsWith(lc))) { WorldBeat(e, null, role != null ? Name(role) : "", Feedback.Info, 2); spoke = true; break; }
            }
            if (spoke) return;
            foreach (var sl in Text.Silent) if (code == sl || (sl.EndsWith(".") && code.StartsWith(sl))) return;
            Say("", l.Text, code.EndsWith(".refused") ? Feedback.Refused : Feedback.Info);
        }

        string[] crowdSt; readonly Dictionary<string, int> sawCount = new();
        static readonly string[] CrowdOrder = { "startled", "fleeing", "returning", "sheltered", "back" };

        /// <summary>The city's voices with no log line: a passer-by whose fight state changes (crowd-state: startled, fleeing, sheltered,
        /// returning, back) speaks once per change, the one nearest the cell within 40 m, never the whole crowd, and one kind at most every
        /// 10 s; a person who has just seen a shot (the memory saw-gunfire written) says so (saw-gunfire), Inez and the guard by name.</summary>
        void CityBeats()
        {
            if (Text == null) return;
            var ws = Game.Walkers;
            if (crowdSt == null || crowdSt.Length != ws.Count) { crowdSt = new string[ws.Count]; for (int i = 0; i < ws.Count; i++) crowdSt[i] = ws[i].St; }
            string bestKind = null; long bestD = long.MaxValue;
            for (int i = 0; i < ws.Count; i++)
            {
                string was = crowdSt[i], now = ws[i].St; if (was == now) continue;
                crowdSt[i] = now;
                string kind = now.Length == 0 ? "back" : now;
                var at = Game.CrowdPos(ws[i]) ?? ws[i].At;
                long d = long.MaxValue; for (int k = 0; k < 4; k++) if (!Game.Ops[k].Inside) d = Math.Min(d, G.D2(Game.Ops[k].Pos, at));
                if (d > 4000L * 4000L) continue;
                if (bestKind == null || Array.IndexOf(CrowdOrder, kind) < Array.IndexOf(CrowdOrder, bestKind) || (kind == bestKind && d < bestD)) { bestKind = kind; bestD = d; }
            }
            if (bestKind != null && !(beatAt.TryGetValue("crowd-state/" + bestKind, out var t0) && Game.Tick - t0 < 10 * Game.Hz))
            { beatAt["crowd-state/" + bestKind] = Game.Tick; Beat("crowd-state", null, "crowd", bestKind, kind: Feedback.Info); }
            // saw-gunfire: a new memory of a shot
            string speaker = null;
            void Saw(Person p, string sp)
            {
                int n = 0; foreach (var m in p.Memory) if (m.Act == "saw-gunfire") n++;
                sawCount.TryGetValue(p.Id, out var before); if (n == before) return;
                sawCount[p.Id] = n; if (n > before && (speaker == null || sp != "crowd")) speaker = sp;
            }
            Saw(Game.Inez, "waitress"); Saw(Game.GuardP, "guard");
            foreach (var w in ws) Saw(w.P, "crowd");
            if (speaker != null && !(beatAt.TryGetValue("saw-gunfire/" + speaker, out var t1) && Game.Tick - t1 < 15 * Game.Hz))
            { beatAt["saw-gunfire/" + speaker] = Game.Tick; Beat("saw-gunfire", null, speaker, kind: Feedback.Refused); }
        }

        /// <summary>The text's clock beats with no log line of their own (the 5:30 chime, the 5:40 last turn), on the tick they fall.</summary>
        void ClockBeats()
        {
            if (Text == null) return;
            foreach (var e in Text.Events.Values)
                if (e.Clock.Length > 0 && e.Log.Count == 0 && Game.Tick == ClockTick(e.Clock)) WorldBeat(e, null, "", Feedback.Info, 1);
        }

        static int ClockTick(string mss) { var p = mss.Split(':'); return (int.Parse(p[0]) * 60 + int.Parse(p[1])) * Game.Hz; }

        static string RoleIn(LogLine l)
        {
            foreach (var r in new[] { "waitress", "driver", "guard", "councillor" }) if (l.Text.Contains($"({r})") || l.Code.EndsWith("." + r)) return r;
            return null;
        }

        static string Cap(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

        static string NeedWords(string need) => need switch
        {
            "debt" => "is in debt", "cash" => "needs money", "advancement" => "wants to get on at work", "care" => "needs care for someone",
            "housing" => "needs a place to live", "kin" => "is worried for family", "papers" => "needs papers", "secret" => "is keeping a secret",
            "" => "keeps their need to themselves", _ => $"needs {need.Replace('-', ' ')}",
        };

        static string TemperWords(string t) => t switch
        {
            "fearful" => "fearful by temperament", "idealist" => "an idealist", "loyal" => "loyal by temperament",
            "mercenary" => "mercenary by temperament", "" => "hard to read", _ => t.Replace('-', ' '),
        };
    }
}
