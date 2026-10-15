using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Suborned.Sim;
using UnityEngine;
using Path = System.IO.Path;

namespace Suborned.Play
{
    /// <summary>The words the play host shows (brief 011 D; Sky: "nothing telling me about the mission", "not enough dialogue"): the briefing,
    /// the objective tracker, each action's label and plain line, what unlocks a locked row, and the dialogue log's lines. Fable writes them
    /// as data, contracts/missions/handover/text.json (schema suborned.text/1, contracts/missions/handover/text.schema.ts), which
    /// `bun tools/sim/unity.ts` validates and copies to StreamingAssets/sim/text.json. Until that file lands the placeholders below stand in
    /// (same shape), and anything the file lacks falls back to them. Nothing is generated at runtime: `Lookup` and `PickIndex` are ports of
    /// the schema's `lookup` and `pickIndex` (fnv1a32 over UTF-16 code units, as JS charCodeAt), so the same run shows the same words.</summary>
    public sealed class PlayText
    {
        public sealed class ActionText { public string Label = "", Does = ""; public readonly Dictionary<string, string> OnTarget = new(); }
        public sealed class LockText { public string Code = "", Line = ""; public readonly List<Regex> Match = new(); }
        public sealed class EventText { public string Id = "", Kind = "", Clock = ""; public readonly List<string> Log = new(), Speakers = new(); }
        public sealed class Entry { public string Event = "", Verb, Speaker = "", Outcome, Temperament, About; public readonly List<string> Lines = new(); }
        public struct Query { public string Event, Verb, Speaker, Outcome, Temperament, About; }

        public string Source { get; private set; } = "placeholder (Fable's text.json is not in StreamingAssets/sim yet)";
        public string Title = "", Objective = "", Win = "", ClockLine = "", Close = "8:30", PurseLine = "", Tracker = "", Seed = "handover-1";
        public readonly List<string> Card = new(), Hints = new();
        public readonly List<(string code, string line)> Fails = new();
        /// <summary>The status pill's marks (briefing.timeline): m:ss, label, and whether the cell knows it before it happens.</summary>
        public readonly List<(string at, string label, bool known)> Timeline = new();
        public readonly Dictionary<string, string> Cell = new(), Speakers = new(), TargetNames = new(), TargetAbout = new();
        public readonly Dictionary<string, ActionText> Actions = new();
        public readonly List<LockText> Locks = new();
        public readonly Dictionary<string, EventText> Events = new();
        public readonly Dictionary<string, List<string>> Exchange = new();
        public readonly List<string> Silent = new();
        /// <summary>The roster's "doing now" words: by phase ("walking"), or phase.verb ("acting.watch", "holding.loop"); {who} is filled.</summary>
        public readonly Dictionary<string, string> Activity = new();
        /// <summary>A cancel's consequence by the engine's cost id (Activity.CancelCost: stop, lost, aborted, ends, …).</summary>
        public readonly Dictionary<string, string> CancelCosts = new();
        public readonly List<(string seed, string key, int n, int count, int index)> Vectors = new();
        /// <summary>The training hints (Motive's table, Fable's words): id → priority, line, and the prose trigger, ring and gone-when.</summary>
        public sealed class Hint { public string Id = "", Line = "", Agent = "", Verb = "", Trigger = "", Rings = "", GoneWhen = ""; public int Priority; }
        public readonly List<Hint> Hints2 = new();
        public readonly Dictionary<string, string> Toasts = new();
        public string LoudCaptured = "";
        public readonly List<(string sample, string code)> LockSamples = new();
        public readonly List<Entry> Dialogue = new();

        // ---------------------------------------------------------------- reading

        public static PlayText Load()
        {
            var t = Placeholder();
            var path = Path.Combine(SimBundle.Dir, "text.json");
            if (!File.Exists(path)) return t;
            try { var f = new PlayText(); f.Read(JNode.Parse(File.ReadAllText(path), "text.json")); f.Fill(t); f.Source = "text.json (Fable)"; return f; }
            catch (Exception e) { Debug.LogWarning($"Play: text.json not read ({e.Message}); the placeholders stand in"); return t; }
        }

        static string S(JNode n, string k) => n.Opt(k) is JNode x && x.Kind == JKind.Str ? x.Str() : null;

        void Read(JNode n)
        {
            if (S(n, "schema") != "suborned.text/1") throw new FormatException($"schema {S(n, "schema")} (want suborned.text/1)");
            if (n.Opt("names") is JNode nm)
            {
                if (nm.Opt("speakers") is JNode sp) foreach (var kv in sp.Entries) Speakers[kv.Key] = kv.Value.Str();
                if (nm.Opt("targets") is JNode tg) foreach (var kv in tg.Entries) TargetNames[kv.Key] = kv.Value.Str();
            }
            if (n.Opt("briefing") is JNode b)
            {
                Title = S(b, "title") ?? ""; Objective = S(b, "objective") ?? ""; Win = S(b, "win") ?? ""; Tracker = S(b, "tracker") ?? "";
                if (b.Opt("card") is JNode c) Card.AddRange(c.Strs());
                if (b.Opt("fails") is JNode fs) foreach (var kv in fs.Entries) Fails.Add((kv.Key, kv.Value.Str()));
                if (b.Opt("clock") is JNode ck) { ClockLine = S(ck, "line") ?? ""; Close = S(ck, "close") ?? Close; }
                if (b.Opt("purse") is JNode pu) PurseLine = S(pu, "line") ?? "";
                if (b.Opt("hints") is JNode h) foreach (var kv in h.Entries) Hints.Add(kv.Value.Str());
                if (b.Opt("timeline") is JNode tl) foreach (var x in tl.Items) Timeline.Add((x["at"].Str(), x["label"].Str(), x["known"].Bool()));
                if (b.Opt("cell") is JNode ce) foreach (var kv in ce.Entries) Cell[kv.Key] = kv.Value.Str();
            }
            if (n.Opt("targets") is JNode ts) foreach (var kv in ts.Entries) { TargetNames.TryAdd(kv.Key, S(kv.Value, "name") ?? kv.Key); TargetAbout[kv.Key] = S(kv.Value, "about") ?? ""; }
            if (n.Opt("actions") is JNode acts)
                foreach (var kv in acts.Entries)
                {
                    var a = new ActionText { Label = S(kv.Value, "label") ?? kv.Key, Does = S(kv.Value, "does") ?? "" };
                    if (kv.Value.Opt("onTarget") is JNode ot) foreach (var o in ot.Entries) a.OnTarget[o.Key] = o.Value.Str();
                    Actions[kv.Key] = a;
                }
            if (n.Opt("locks") is JNode lo && lo.Kind == JKind.Obj) // suborned.text/1 since a103daf: code → { line, verbs }, keyed by Answer.code
                foreach (var kv in lo.Entries) Locks.Add(new LockText { Code = kv.Key, Line = S(kv.Value, "line") ?? "" });
            else if (n.Opt("locks") is JNode ls)
                foreach (var l in ls.Items)
                {
                    var lk = new LockText { Code = S(l, "code") ?? "", Line = S(l, "line") ?? "" };
                    if (l.Opt("match") is JNode mt) foreach (var m in mt.Strs()) lk.Match.Add(new Regex(m, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));
                    if (l.Opt("samples") is JNode sm) foreach (var x in sm.Strs()) LockSamples.Add((x, lk.Code));
                    Locks.Add(lk);
                }
            if (n.Opt("events") is JNode evs)
                foreach (var kv in evs.Entries)
                {
                    var e = new EventText { Id = kv.Key, Kind = S(kv.Value, "kind") ?? "", Clock = S(kv.Value, "clock") ?? "" };
                    if (kv.Value.Opt("log") is JNode lg) e.Log.AddRange(lg.Strs());
                    if (kv.Value.Opt("speakers") is JNode sk) e.Speakers.AddRange(sk.Strs());
                    Events[kv.Key] = e;
                }
            if (n.Opt("activity") is JNode av)
                foreach (var kv in av.Entries)
                    if (kv.Value.Kind == JKind.Str) Activity[kv.Key] = kv.Value.Str();
                    else foreach (var sub in kv.Value.Entries) Activity[$"{kv.Key}.{sub.Key}"] = sub.Value.Str();
            if (n.Opt("hints")?.Opt("items") is JNode hi)
                foreach (var kv in hi.Entries)
                {
                    var v = kv.Value; int.TryParse(v.Opt("priority")?.NumberText ?? "99", out var pr);
                    Hints2.Add(new Hint { Id = kv.Key, Priority = pr, Line = S(v, "line") ?? "", Agent = S(v, "agent") ?? "", Verb = S(v, "verb") ?? "", Trigger = S(v, "trigger") ?? "", Rings = S(v, "rings") ?? "", GoneWhen = S(v, "goneWhen") ?? "" });
                }
            Hints2.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            if (n.Opt("toasts") is JNode to) foreach (var kv in to.Entries) Toasts[kv.Key] = S(kv.Value, "line") ?? "";
            if (n.Opt("goingLoud") is JNode gl) LoudCaptured = S(gl, "captured") ?? "";
            if (n.Opt("cancelCosts") is JNode cc) foreach (var kv in cc.Entries) CancelCosts[kv.Key] = kv.Value.Str();
            if (n.Opt("exchange") is JNode ex) foreach (var kv in ex.Entries) Exchange[kv.Key] = kv.Value.Strs();
            if (n.Opt("silentLogs") is JNode sl) foreach (var kv in sl.Entries) Silent.Add(kv.Key);
            if (n.Opt("pick")?.Opt("vectors") is JNode vec)
                foreach (var v in vec.Items) Vectors.Add((v["seed"].Str(), v["key"].Str(), v["n"].Int(), v["count"].Int(), v["index"].Int()));
            if (n.Opt("dialogue") is JNode dl)
                foreach (var d in dl.Items)
                {
                    var e = new Entry { Event = S(d, "event") ?? "", Verb = S(d, "verb"), Speaker = S(d, "speaker") ?? "", Outcome = S(d, "outcome"), Temperament = S(d, "temperament"), About = S(d, "about") };
                    if (S(d, "pending") != null) continue; // a line waiting on Sky (the named cliffhanger): never shipped, the `unnamed` set plays
                    e.Lines.AddRange(d["lines"].Strs());
                    Dialogue.Add(e);
                }
        }

        /// <summary>Whatever Fable's file leaves empty is taken from the placeholders (so a partial file still plays).</summary>
        void Fill(PlayText p)
        {
            if (Title.Length == 0) Title = p.Title; if (Objective.Length == 0) Objective = p.Objective; if (Win.Length == 0) Win = p.Win;
            if (Tracker.Length == 0) Tracker = p.Tracker; if (ClockLine.Length == 0) ClockLine = p.ClockLine; if (PurseLine.Length == 0) PurseLine = p.PurseLine;
            if (Card.Count == 0) Card.AddRange(p.Card); if (Hints.Count == 0) Hints.AddRange(p.Hints); if (Fails.Count == 0) Fails.AddRange(p.Fails);
            foreach (var kv in p.Cell) Cell.TryAdd(kv.Key, kv.Value);
            foreach (var kv in p.Speakers) Speakers.TryAdd(kv.Key, kv.Value);
            foreach (var kv in p.TargetNames) TargetNames.TryAdd(kv.Key, kv.Value);
            foreach (var kv in p.Actions) Actions.TryAdd(kv.Key, kv.Value);
            foreach (var kv in p.Activity) Activity.TryAdd(kv.Key, kv.Value);
            // locks, events, the exchange and the dialogue are the file's whole: placeholder lines never mix into Fable's
        }

        /// <summary>The file's own test vectors, run on this port: the pick vectors (pickIndex) and every lock sample (lockOf). Empty = all pass.</summary>
        public List<string> SelfTest()
        {
            var bad = new List<string>();
            foreach (var v in Vectors) { int got = PickIndex(v.seed, v.key, v.n, v.count); if (got != v.index) bad.Add($"pick {v.seed}|{v.key}|{v.n} of {v.count}: {got}, want {v.index}"); }
            foreach (var (sample, code) in LockSamples) { var got = LockOf(sample)?.Code; if (got != code) bad.Add($"lock '{sample}': {got ?? "none"}, want {code}"); }
            return bad;
        }

        /// <summary>The roster's words for an activity ("Watching {who}" filled), else null.</summary>
        public string Doing(string phase, string verb, string who)
        {
            string w = null;
            if (verb.Length > 0 && Activity.TryGetValue($"{phase}.{verb}", out var a)) w = a;
            else if (Activity.TryGetValue(phase, out var b)) w = b;
            return w?.Replace("{who}", who);
        }

        // ---------------------------------------------------------------- the lookups

        public string Speaker(string id) => Speakers.TryGetValue(id, out var s) ? s : id.Length == 0 ? "" : char.ToUpperInvariant(id[0]) + id[1..];
        public string TargetName(string key, string fallback) => TargetNames.TryGetValue(key, out var s) ? s : fallback;

        /// <summary>An action's label and plain line ("Pay Inez", "slip her cash for a favour; she'll owe you, but the payment can be traced").</summary>
        public (string label, string does) Action(string verb, string target)
        {
            if (!Actions.TryGetValue(verb, out var a)) return (verb, "");
            var does = target != null && a.OnTarget.TryGetValue(target, out var on) ? on : a.Does;
            return (a.Label, does);
        }

        /// <summary>What unlocks a refusal or a locked row: the first lock whose pattern matches its text; null when none does.</summary>
        /// <summary>The lock for an engine refusal code (Answer.code), or null when the text has none.</summary>
        public LockText LockByCode(string code) { foreach (var l in Locks) if (l.Code == code) return l; return null; }

        public LockText LockOf(string why)
        {
            if (string.IsNullOrEmpty(why)) return null;
            foreach (var l in Locks) foreach (var m in l.Match) if (m.IsMatch(why)) return l;
            return null;
        }

        public Entry Lookup(Query q)
        {
            Entry best = null; int bestScore = -1;
            foreach (var e in Dialogue)
            {
                if (e.Event != q.Event || e.Speaker != q.Speaker) continue;
                int score = 0; bool ok = true;
                void F(string have, string want) { if (!ok || have == null) return; if (have != want) ok = false; else score++; }
                F(e.Verb, q.Verb); F(e.Outcome, q.Outcome); F(e.Temperament, q.Temperament); F(e.About, q.About);
                if (ok && score > bestScore) { best = e; bestScore = score; }
            }
            return best;
        }

        public static string LineKey(Query q)
        {
            var parts = new List<string>(6);
            foreach (var x in new[] { q.Event, q.Verb, q.Speaker, q.Outcome, q.Temperament, q.About }) if (x != null) parts.Add(x);
            return string.Join("/", parts);
        }

        public static uint Fnv1a32(string key)
        {
            uint h = 0x811c9dc5;
            foreach (char c in key) { h ^= c; h = unchecked(h * 0x01000193); }
            return h;
        }

        /// <summary>The variant for the n-th use (0-based) of a key in a run; a repeat of the previous use's variant moves on one.</summary>
        public static int PickIndex(string seed, string key, int n, int count)
        {
            int prev = -1, at = 0;
            for (int i = 0; i <= n; i++)
            {
                at = (int)(Fnv1a32($"{seed}|{key}|{i}") % (uint)count);
                if (count > 1 && at == prev) at = (at + 1) % count;
                prev = at;
            }
            return at;
        }

        readonly Dictionary<string, int> uses = new();
        public void ResetUses() => uses.Clear();

        /// <summary>A line for a query, or null; each use of a key advances its count, so a run's n-th use always shows the same variant.</summary>
        public string Pick(Query q)
        {
            var e = Lookup(q); if (e == null || e.Lines.Count == 0) return null;
            var key = LineKey(q); uses.TryGetValue(key, out var n); uses[key] = n + 1;
            return e.Lines[PickIndex(Seed, key, n, e.Lines.Count)];
        }

        // ---------------------------------------------------------------- placeholders (Latch, until Fable's file lands; same shape)

        static PlayText Placeholder()
        {
            var t = new PlayText
            {
                Title = "The Handover",
                Objective = "Take Councillor Vale alive and get him into your getaway car.",
                Win = "Vale, alive, in your getaway car.",
                ClockLine = "Clock 0:00 is 17:24:30. The chime is at 5:30, Vale leaves at 5:50, the district closes at 8:30.",
                PurseLine = "$100,000 for the whole job. Every payment can be traced.",
                Tracker = "Take Vale alive to the getaway car · closes {close}",
            };
            t.Card.AddRange(new[]
            {
                "Councillor Vale signed the clearance order for the marked block.",
                "At shift handover he leaves the Civic Records Office for his official car.",
                "Take him alive. Nobody in the city should know it was you.",
            });
            t.Fails.AddRange(new[]
            {
                ("vale-dead", "Vale is killed."), ("vale-leaves", "Vale drives off in the official car."), ("rivals-reach-vale", "The rival crew reaches Vale first."),
                ("sealed", "The district seals."), ("enforcer-down", "The Enforcer goes down."), ("entry-seen-guard", "Your entry is seen by the guard, a live camera or the patrol."),
            });
            t.Hints.Add("The front door: Inez at the café can draw the door guard away, if she's on your side.");
            t.Hints.Add("The pickup: Vale's driver needs money. A turned driver can move the car or block the pickup camera.");
            t.Cell["scout"] = "Scout: watches and listens; learns routines, needs and temperaments.";
            t.Cell["fixer"] = "Fixer: turns people (ask, pay, help, pressure) and calls them once turned.";
            t.Cell["tech"] = "Tech: loops a camera or reroutes the patrol from the control cabinet.";
            t.Cell["enforcer"] = "Enforcer: enters, takes Vale, puts him in the car; fights.";
            foreach (var (k, v) in new[] { ("scout", "Scout"), ("fixer", "Fixer"), ("tech", "Tech"), ("enforcer", "Enforcer"), ("waitress", "Inez"), ("driver", "Driver"), ("guard", "Guard"), ("councillor", "Vale") })
                t.Speakers[k] = v;
            foreach (var (k, v) in new[] { ("waitress", "Inez"), ("driver", "the driver"), ("guard", "the guard"), ("councillor", "Vale"), ("patrol", "the patrol"), ("rivals", "the rival crew") })
                t.TargetNames[k] = v;
            void A(string verb, string label, string does) => t.Actions[verb] = new ActionText { Label = label, Does = does };
            A("watch", "Watch", "learn their routine from a distance: where they go, and when");
            A("listen", "Listen", "stand close and listen: learn what they need and what kind of person they are");
            A("ask", "Ask", "ask a favour outright: free, but they may refuse and remember the Fixer");
            A("pay", "Pay", "slip them cash for a favour: they'll owe you, but the payment can be traced");
            A("help", "Help", "solve their problem for them: loyal after, if you know what they need");
            A("pressure", "Pressure", "lean on them: quick and cheap, but it leaves resentment and a trail");
            A("call", "Call", "ring a turned contact and ask for the favour now");
            A("loop", "Loop camera", "patch an old recording into the entrance feed; the Tech must stay at the cabinet");
            A("reroute", "Reroute patrol", "send the patrol false orders: it leaves the entrance sweep for a while");
            A("mislead", "False tip", "a false tip sends the rival crew elsewhere for a while");
            A("enter", "Enter", "slip in at the public door to reach Vale; anyone watching the door sees");
            A("take", "Take Vale", "seize Vale at his car");
            A("extract", "Into the car", "Vale into your getaway car: the job is done");
            A("fire", "Fire", "fire until they are down: the run goes loud");
            void Lk(string code, string pattern, string line) { var l = new LockText { Code = code, Line = line }; l.Match.Add(new Regex(pattern, RegexOptions.IgnoreCase)); t.Locks.Add(l); }
            Lk("needs-routine", "needs .*routine", "Watch them first (Scout) to learn their routine.");
            Lk("needs-need", "needs .*need", "Listen to them first (Scout) to learn what they need.");
            Lk("needs-temperament", "needs .*temperament", "Listen to them first (Scout) to learn their temperament.");
            Lk("busy", "busy until", "Wait until the current act lands, or cancel it (C).");
            Lk("cash", "not enough cash", "You can't afford the worst case; try a cheaper approach.");
            Lk("refused", "refused this already", "They said no to this; try another approach.");
            Lk("vale-inside", "Vale is not inside", "Only while Vale is in the Office (before 5:50).");
            Lk("vale-car", "not at his car", "Wait for Vale to reach his car (after 5:50).");
            Lk("no-vale", "don't have Vale", "Take Vale first (Enforcer).");
            Lk("no-rivals", "no rivals", "The rival crew arrives later.");
            void E(string id, string kind, string[] log, params string[] speakers) { var e = new EventText { Id = id, Kind = kind }; if (log != null) e.Log.AddRange(log); e.Speakers.AddRange(speakers); t.Events[id] = e; }
            E("ack", "order", null, "scout", "fixer", "tech", "enforcer");
            E("reply", "reply", null, "waitress", "driver", "guard");
            E("read", "read", null, "scout");
            foreach (var (k, v) in new[] { ("idle", "Ready"), ("down", "Down"), ("walking", "Walking"), ("running", "Running"), ("drawing", "Drawing"), ("holstering", "Holstering"),
                ("firing", "Firing on {who}"), ("holding.loop", "Holding the loop"), ("holding.take", "Carrying Vale to the car"), ("holding.carry", "Carrying {who}"),
                ("acting.watch", "Watching {who}"), ("acting.listen", "Listening to {who}"), ("acting.ask", "Asking {who}"), ("acting.pay", "Paying {who}"), ("acting.help", "Helping {who}"),
                ("acting.pressure", "Pressing {who}"), ("acting.loop", "Looping the camera"), ("acting.reroute", "Rerouting the patrol"), ("acting.mislead", "Tipping off the rivals"),
                ("acting.enter", "Inside the Office"), ("acting.take", "Taking Vale"), ("acting.carry", "Lifting {who}") })
                t.Activity[k] = v;
            void D(string ev, string verb, string speaker, string outcome, params string[] lines) { var e = new Entry { Event = ev, Verb = verb, Speaker = speaker, Outcome = outcome }; e.Lines.AddRange(lines); t.Dialogue.Add(e); }
            D("ack", null, "scout", null, "On my way.", "Moving.");
            D("ack", "watch", "scout", null, "I'll keep an eye on {who}.", "Watching {who}.");
            D("ack", "listen", "scout", null, "Getting close enough to hear {who}.", "Listening in on {who}.");
            D("ack", "mislead", "scout", null, "I'll feed the rivals a story.", "One false tip, coming up.");
            D("ack", null, "fixer", null, "Let me talk to {who}.", "Leave {who} to me.");
            D("ack", "call", "fixer", null, "Calling {who} now.", "Ringing {who}.");
            D("ack", null, "tech", null, "On it, heading to the cabinet.", "Going to the cabinet.");
            D("ack", null, "enforcer", null, "Moving.", "On it.");
            D("ack", "fire", "enforcer", null, "Firing.", "Contact.");
            D("reply", null, "waitress", "turned", "All right. Tell me what you need.", "Fine. I'll help you.");
            D("reply", null, "waitress", "refused", "I can't talk here. Please go.", "No. Leave me be.");
            D("reply", null, "driver", "turned", "Fine. Just this once.", "All right, I'll do it.");
            D("reply", null, "driver", "refused", "Not interested.", "Find someone else.");
            D("reply", null, "guard", "refused", "Move along.", "Not a chance.");
            D("reply", null, "guard", "turned", "I never saw you.", "Make it quick.");
            return t;
        }
    }
}
