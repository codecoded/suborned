using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>The scene feed (Sky 2026-10-15; Glyph's round 6b card structure, briefs/011/ui/round6b; Fable's `scenes` in text.json). Every
    /// card is one event: kind (learned, approach, call, city, warning, fight: the header tint), verb icon, title, where, clock, NEW; lines with
    /// speaker chips; a highlighted result footer; a tick (K acknowledges the newest; a click on a card pans to its place).
    ///  · An act card opens when an agent's act begins (Game.ActivityOf → acting). Watch and Listen play an overheard scene, a Turn an
    ///    approach scene: its beats arrive as the act's progress runs (an approach holds back the target's reply and the Fixer's reaction
    ///    until it lands, when the scene for the real outcome plays out); the footer is the scene's result with {who} {need} {temperament}
    ///    {usd} {alert} filled from the engine. Other acts (loop, reroute, mislead, enter, take, carry) get the lines said and a result.
    ///  · A call card plays the call scene when the Fixer calls a turned contact; city cards play the city scenes for a world event (a scene
    ///    marked in-sight only when an agent stands within 13 m of the place with a clear line); other events the cell should see (a runner
    ///    seen, security called, the fight) get warning or fight cards with the lines said.
    ///  · Beats are revealed one at a time (an act's by its progress; a call or city scene one every 1.5 s of mission time), so the feed is
    ///    read in real time; nothing pauses. Unacknowledged cards stay; acknowledged ones go to the history (L). The cell's chatter is the
    ///    ticker's; a refusal is a pill at the click; neither is ever a card (calm-down batch 1: Glyph's review #1, Fable §2). The feed reads
    ///    the run and never changes it, so a replay is untouched.</summary>
    public sealed partial class HandoverHost
    {
        public sealed class FeedLine { public string Speaker = "", Name = "", Text = ""; public bool Reply; }
        public sealed class FeedCard
        {
            public int Id, Agent = -1, Started, Ended = -1, Revealed, NextBeatAt = -1, Progress = -1, TicksLeft = -1;
            public string Kind = "learned", Verb = "", Target = "", Title = "", Where = "", Footer = "", Event = "", PlaceKey = "", Icon = "";
            public readonly List<FeedLine> Lines = new();
            public PlayText.Scene Scene; public int SceneN; public long Paid; public Vector3? Place; public bool Live, Read; public Feedback Tone = Feedback.Info;
            public bool Typing => Revealed < Lines.Count;
        }
        /// <summary>Cards not yet acknowledged (oldest first), and the acknowledged history (newest last).</summary>
        public readonly List<FeedCard> Feed = new(), FeedHistory = new();
        public bool FeedHistoryOpen { get; set; }
        public string FeedFilter { get; set; } = "all"; // all · learned · turned · city · warnings (Glyph's history filters)
        public int Unread { get { int n = 0; foreach (var c in Feed) if (!c.Live && !c.Read) n++; return n; } }
        readonly FeedCard[] actCard = new FeedCard[4];
        int feedIds;

        void ResetFeed() { Feed.Clear(); FeedHistory.Clear(); Array.Clear(actCard, 0, 4); feedIds = 0; lastEvent = null; }

        static readonly HashSet<string> FeedActs = new() { "watch", "listen", "turn", "loop", "reroute", "mislead", "enter", "take", "carry" };
        static bool IsRole(string s) => s is "waitress" or "driver" or "guard" or "councillor";

        FeedCard NewCard(string kind, string title, string icon, string where, Vector3? place, string placeKey = "")
        {
            var c = new FeedCard { Id = ++feedIds, Kind = kind, Title = title, Icon = icon, Where = where, Started = Game.Tick, Place = place, PlaceKey = placeKey };
            Feed.Add(c);
            while (Feed.Count > 30) { var old = Feed.Find(x => !x.Live); if (old == null) break; Feed.Remove(old); old.Read = true; FeedHistory.Add(old); }
            return c;
        }

        void Fill(FeedCard c, List<(string speaker, string text, bool reply)> beats)
        {
            c.Lines.Clear();
            foreach (var (sp, tx, rep) in beats) if (tx.Length > 0) c.Lines.Add(new FeedLine { Speaker = sp, Name = Text?.Speaker(sp) ?? sp, Text = Fill(tx, Name(c.Target)), Reply = rep });
        }

        string PlaceWords(string target) => target switch
        {
            "waitress" => "café door", "driver" => "the pickup", "guard" => "Office door", "councillor" => "the Office", "entrance" or "cabinet" => "control cabinet",
            "patrol" => "the patrol", "rivals" => "the pickup", _ => "",
        };

        /// <summary>After each Track: an act starting opens its card (with its scene), its progress reveals beats, its end lands the card.</summary>
        void FeedTrack()
        {
            for (int k = 0; k < 4; k++)
            {
                var d = doing[k]; var c = actCard[k]; var op = Game.Ops[k];
                if (d.Phase == "acting" && FeedActs.Contains(d.Verb) && (c == null || !c.Live))
                {
                    string target = op.Busy?.Target ?? "", approach = op.Busy?.Approach ?? "", verb = d.Verb == "turn" ? approach : d.Verb;
                    var label = Text?.Action(verb, target).label ?? verb;
                    string kind = d.Verb is "watch" or "listen" ? "learned" : d.Verb == "turn" ? "approach" : "learned";
                    var place = IsRole(target) ? TargetWorld(new Target { Kind = "person", Key = target }) : (Vector3?)frame.World(op.Pos);
                    c = actCard[k] = NewCard(kind, label, Glyph(d.Verb, approach), $"{(target.Length > 0 && IsRole(target) ? Name(target) + " · " : "")}{PlaceWords(target)}".Trim(' ', '·'), place, IsRole(target) ? target : "op/" + Op.Name((OpId)k));
                    c.Agent = k; c.Verb = verb; c.Target = target; c.Live = true; c.Paid = op.BusyPaid;
                    if (Text != null && IsRole(target))
                    {
                        var p = Game.PersonByRole(target);
                        if (d.Verb is "watch" or "listen") c.Scene = Text.LookupScene(new PlayText.SceneQuery { Kind = "overheard", Verb = d.Verb, About = target, Temperament = p.Temperament });
                        else if (d.Verb == "turn")
                        {
                            // the outcome isn't known until it lands: lead in with the scene for the card's own preview (or turned), reply held back
                            var guess = op.BusyQuote?.Outcome is string qo && qo != "?" && qo.Length > 0 ? qo : "turned";
                            c.Scene = Text.LookupScene(new PlayText.SceneQuery { Kind = "approach", Verb = approach, About = target, Outcome = guess, Temperament = p.Temperament });
                        }
                        if (c.Scene != null) { c.SceneN = Text.SceneUse(c.Scene); Fill(c, Text.PlayScene(c.Scene, c.SceneN)); }
                    }
                }
                if (c == null || !c.Live) continue;
                if (d.Phase == "acting")
                {
                    c.Progress = d.Progress; c.TicksLeft = d.TicksLeft;
                    if (c.Scene != null) // beats by progress; an approach stops before the reply
                    {
                        int lead = c.Lines.FindIndex(l => l.Reply); if (lead < 0) lead = Math.Max(0, c.Lines.Count - 1);
                        c.Revealed = Math.Max(c.Revealed, Math.Min(lead, 1 + (int)((long)Math.Max(0, d.Progress) * lead / 1000)));
                    }
                }
                else Land(c);
            }
            // beats due by the clock (a call, a city scene, an act that has landed)
            foreach (var f in Feed) if (f.Revealed < f.Lines.Count && f.NextBeatAt >= 0 && Game.Tick >= f.NextBeatAt) { f.Revealed++; f.NextBeatAt = Game.Tick + 15; }
        }

        /// <summary>An act card lands: an approach plays the scene for the real outcome; the rest of the beats come every 1.5 s; the footer is the
        /// scene's result filled from the engine (or the host's words where there is no scene).</summary>
        void Land(FeedCard c)
        {
            c.Live = false; c.Progress = -1; c.Ended = Game.Tick;
            if (c.Footer.Length > 0) return; // cancelled
            if (Text != null && IsRole(c.Target) && c.Verb is "ask" or "pay" or "help" or "pressure")
            {
                var p = Game.PersonByRole(c.Target); string outcome = TurnOutcome(c.Target);
                var sc = Text.LookupScene(new PlayText.SceneQuery { Kind = "approach", Verb = c.Verb, About = c.Target, Outcome = outcome, Temperament = p.Temperament });
                if (sc != null && sc != c.Scene) { int kept = c.Revealed; c.Scene = sc; c.SceneN = Text.SceneUse(sc); Fill(c, Text.PlayScene(sc, c.SceneN)); c.Revealed = Math.Min(kept, c.Lines.Count); }
                c.Tone = outcome is "turned" or "kept" ? Feedback.Ok : outcome is "refused" or "suspicious" ? Feedback.Refused : Feedback.Info;
            }
            c.NextBeatAt = Game.Tick; // the remaining beats follow one by one
            c.Footer = c.Scene != null ? Result(c.Scene.Result, c) : FooterOf(c);
            if (c.Verb is "watch" or "listen" && c.Footer.Length > 0) c.Tone = Feedback.Ok;
        }

        /// <summary>How the last approach on a person went, from the engine's log (turn.&lt;approach&gt;.&lt;outcome&gt;; reported = suspicious).</summary>
        string TurnOutcome(string role)
        {
            for (int i = Game.Log.Count - 1; i >= 0; i--)
            {
                var l = Game.Log[i]; if (l.Tick != Game.Tick) break;
                if (l.Code == "report") return "suspicious";
            }
            for (int i = Game.Log.Count - 1; i >= 0 && i >= Game.Log.Count - 40; i--)
            {
                var l = Game.Log[i];
                if (l.Code.StartsWith("turn.") && l.Text.Contains($"({role})")) return l.Code[(l.Code.LastIndexOf('.') + 1)..];
            }
            var p = Game.PersonByRole(role); return p.Turned ? "turned" : "not-yet";
        }

        /// <summary>A scene's result line with {who} {need} {temperament} {usd} {alert} from the engine (Fable's placeholders).</summary>
        string Result(string line, FeedCard c)
        {
            var p = IsRole(c.Target) ? Game.PersonByRole(c.Target) : null;
            string temper = p == null || p.Temperament.Length == 0 ? "?" : char.ToUpperInvariant(p.Temperament[0]) + p.Temperament[1..];
            return line.Replace("{who}", c.Target.Length > 0 ? Name(c.Target) : "them").Replace("{need}", p?.Need ?? "?").Replace("{temperament}", temper)
                .Replace("{usd}", c.Paid > 0 ? $"${c.Paid:N0}" : "$0").Replace("{alert}", Game.Alert.ToString());
        }

        /// <summary>What came of an act with no scene, from the engine's state when it ends.</summary>
        string FooterOf(FeedCard c)
        {
            var g = Game; string who = Name(c.Target);
            switch (c.Verb)
            {
                case "watch" when Game.WatcherKind(c.Target).Length > 0:
                    {
                        var k = g.Known(c.Target); if (k == null || !k.Sight) return "Nothing new.";
                        c.Tone = Feedback.Ok;
                        return $"Learned: what {who} sees{(k.Routine && k.RoutineNeedsS > 0 ? ", and its rounds" : k.RoutineNeedsS > 0 ? $" ({k.WatchedS} of {k.RoutineNeedsS} s toward its rounds)" : "")}.";
                    }
                case "watch": { var p = g.PersonByRole(c.Target); return p.KRoutine ? $"Learned: {who}'s routine." : "Nothing learned."; }
                case "listen": { var p = g.PersonByRole(c.Target); return !p.KNeed && !p.KTemperament ? "Nothing learned." : $"Learned: {Cap(who)} {NeedWords(p.Need)}; {TemperWords(p.Temperament)}."; }
                case "loop": c.Tone = g.LoopOn ? Feedback.Ok : Feedback.Info; return g.LoopOn ? "Camera looped. The Tech is tied to the cabinet." : "The loop didn't hold.";
                case "reroute": c.Tone = Feedback.Ok; return "The patrol is off its sweep for a while.";
                case "mislead": c.Tone = g.RivalsPresent ? Feedback.Info : Feedback.Ok; return g.RivalsPresent ? "The tip missed." : "The rivals follow the false tip. They may be back.";
                case "enter": return g.Vale == ValeAt.WithEnforcer ? "Out at the service lane with Vale." : g.Ops[(int)OpId.Enforcer].Inside ? "Inside the Office." : "Back out without Vale.";
                case "take": c.Tone = g.Vale == ValeAt.WithEnforcer ? Feedback.Ok : Feedback.Info; return g.Vale == ValeAt.WithEnforcer ? "Vale is with the Enforcer: get him to the car." : "Vale was not taken.";
                case "carry": return "Lifted: walk them to the car.";
            }
            return "Done.";
        }

        /// <summary>A line said: an act card with no scene takes its agent's and its person's lines; other NPC lines go to the event card of the
        /// same tick, or a card of their own (NPC lines live in the feed; the cell's chatter is the ticker and the history's Cell filter).</summary>
        void FeedSay(string speaker, string text)
        {
            bool Open(FeedCard c) => c != null && c.Scene == null && (c.Live || c.Ended == Game.Tick);
            int k = Array.IndexOf(OpKeys, speaker);
            if (k >= 0 && Open(actCard[k])) { Add(actCard[k], speaker, text); return; }
            for (int i = 0; i < 4; i++) { var c = actCard[i]; if (Open(c) && c.Target == speaker) { Add(c, speaker, text); return; } }
            if (k >= 0) return; // the cell's own words: the ticker
            for (int i = 0; i < 4; i++) { var c = actCard[i]; if (c != null && c.Scene != null && c.Target == speaker && (c.Live || c.Ended == Game.Tick)) return; } // its scene says it
            if (speaker.Length == 0) return; // the engine's own sentence with no speaker: the log and history, never a card line or an untitled card
            if (Text == null || !Text.Speakers.ContainsKey(speaker)) return; // a speaker the text doesn't name ("cell") makes no card (the "Cell" rows)
            if (lastEvent != null && lastEvent.Started == Game.Tick) { if (lastEvent.Scene == null) Add(lastEvent, speaker, text); return; } // a scene says it already
            var nc = NewCard(speaker is "patrol" or "security" or "watcher" or "guard" ? "warning" : "city", Text?.Speaker(speaker) ?? speaker, "·", "", WorldOf(speaker), ViewKey(speaker));
            Add(nc, speaker, text); nc.Revealed = 1; lastEvent = nc;
        }

        void Add(FeedCard c, string speaker, string text) { c.Lines.Add(new FeedLine { Speaker = speaker, Name = Text?.Speaker(speaker) ?? speaker, Text = text }); if (c.Scene == null) c.Revealed = c.Lines.Count; }

        /// <summary>A cancel that lost something (CancelAgent decides) closes the agent's card with what it cost; with no act card live (the loop
        /// held, Vale carried), a card of its own named for the act: "Loop the entrance camera cancelled · The loop ends and the camera sees again."</summary>
        void FeedCancelled(int k, string cost, string what = "")
        {
            var c = actCard[k];
            if (c == null || !c.Live)
            {
                var n = NewCard("city", $"{Cap(what)} cancelled", "close_x", "", frame.World(Game.Ops[k].Pos), "op/" + Op.Name((OpId)k));
                n.Agent = k; n.Footer = cost; n.Tone = Feedback.Info; lastEvent = n; return;
            }
            c.Live = false; c.Progress = -1; c.Ended = Game.Tick; c.Footer = $"Cancelled: {cost}"; c.Tone = Feedback.Info;
        }

        /// <summary>A cancel that lost nothing: a live act card (if any) goes without a word; stopping is not an event.</summary>
        void CloseActCard(int k)
        {
            var c = actCard[k]; if (c == null || !c.Live) return;
            c.Live = false; Feed.Remove(c); actCard[k] = null;
        }

        /// <summary>The Fixer calls a turned contact: the call scene plays out, a beat every 1.5 s.</summary>
        void FeedCall(string role)
        {
            if (Text == null) return;
            var p = Game.PersonByRole(role);
            var sc = Text.LookupScene(new PlayText.SceneQuery { Kind = "call", Verb = "call", About = role, Temperament = p.Temperament });
            var c = NewCard("call", Text.Action("call", role).label, "call", $"{Name(role)} · {PlaceWords(role)}", TargetWorld(new Target { Kind = "person", Key = role }), role);
            c.Agent = (int)OpId.Fixer; c.Verb = "call"; c.Target = role;
            if (sc != null) { c.Scene = sc; c.SceneN = Text.SceneUse(sc); Fill(c, Text.PlayScene(sc, c.SceneN)); c.Footer = Result(sc.Result, c); }
            else c.Footer = $"{Cap(Name(role))} acts on the call.";
            c.Revealed = 1; c.NextBeatAt = Game.Tick + 15; c.Tone = Feedback.Ok;
        }

        /// <summary>A call the contact's lover refuses (the engine's summon-* codes): a call card with Fable's summon-refused lines from both
        /// ends of the tie, and the lock line for the code as its result.</summary>
        void FeedSummonRefused(string role, Order o, Answer a)
        {
            if (Text == null) return;
            var p = Game.PersonByRole(role);
            var c = NewCard("call", Text.Action("call", role).label, "call", $"{Name(role)} · {PlaceWords(role)}", TargetWorld(new Target { Kind = "person", Key = role }), role);
            c.Agent = (int)OpId.Fixer; c.Verb = "call"; c.Target = role; c.Tone = Feedback.Refused;
            foreach (var sp in new[] { role, p.Lover?.Role }) if (sp != null && Text.Pick(new PlayText.Query { Event = "summon-refused", Speaker = sp, Outcome = a.code }) is string line) Add(c, sp, Fill(line, Name(sp)));
            c.Footer = LockLine(o, a) ?? a.why; c.Revealed = Math.Min(1, c.Lines.Count); c.NextBeatAt = Game.Tick + 15;
        }

        // events that get a card of their own when no city scene covers them: kind, title, where it points. Titles and results in the player's
        // words from Fable's review (messages.html §1 D, 2026-10-16), host placeholders until text.json carries events[id].card; never the
        // engine's sentence (its log keeps it).
        static readonly Dictionary<string, (string kind, string title, string place)> FeedEvents = new()
        {
            ["rivals.arrive"] = ("city", "Rival car arrives", "rivals/0"), ["rivals.misled"] = ("city", "Rivals sent away", "rivals/0"), ["rivals.drawn"] = ("warning", "Rivals heading in", "rivals/0"),
            ["guard.at-cafe"] = ("city", "The guard is at the café", "guard"), ["guard.back"] = ("city", "Guard back on the door", "guard"),
            ["vale.exits"] = ("city", "Vale comes out", "councillor"), ["vale.at-car"] = ("city", "Vale reaches his car", "councillor"),
            ["car.blocks"] = ("city", "The car blocks the camera", "car/official"), ["car.leaves"] = ("city", "The official car leaves", "car/official"),
            ["loop.ends"] = ("warning", "The loop ends", "camera/entrance"), ["security.called"] = ("warning", "Security is coming", "security/0"), ["patrol.breaks"] = ("warning", "The patrol turns on you", "patrol"),
            ["run.seen"] = ("warning", "Runner seen", ""), ["report"] = ("warning", "Reported", ""), ["fight.loud"] = ("fight", "Police coming in", "security/0"), ["fight.safe"] = ("fight", "The rivals wait at the car", "car/getaway"),
            ["fight.ambush"] = ("fight", "Ambush", "car/getaway"), ["fight.over"] = ("fight", "The fight is over", "car/getaway"), ["captured"] = ("fight", "Captured", ""),
            ["scout.down"] = ("fight", "Scout is down", "op/scout"), ["fixer.down"] = ("fight", "Fixer is down", "op/fixer"), ["tech.down"] = ("fight", "Tech is down", "op/tech"),
        };
        /// <summary>City scenes' card titles by the text's event id (Fable's table D).</summary>
        static readonly Dictionary<string, string> SceneTitles = new() { ["rivals-arrive"] = "Rival car arrives", ["chime"] = "Handover chime", ["guard-at-cafe"] = "The guard is at the café" };

        /// <summary>Events that happen at a place: the cell hears of them only in sight (Fable: rivals.arrive, guard.at-cafe "in sight only").</summary>
        static readonly Dictionary<string, string> SeenAt = new() { ["rivals.arrive"] = "rivals-arrive", ["guard.at-cafe"] = "guard-at-cafe" };

        /// <summary>A log line: the city scenes for its world event (in-sight ones only when an agent can see the place), else its own card, which
        /// keeps the same sight rule: an event whose scenes are all in-sight only, or that happens at a place, makes no card when no agent sees it.</summary>
        void FeedEvent(LogLine l)
        {
            bool hidden = false;
            if (Text != null)
                foreach (var e in Text.Events.Values)
                {
                    if (!e.Log.Contains(l.Code)) continue;
                    var r = CityScene(e.Id, l);
                    if (r == Seen.Played) return;
                    if (r == Seen.Hidden) hidden = true;
                }
            if (hidden) return;
            if (SeenAt.TryGetValue(l.Code, out var placeEv) && EventPlace(placeEv).at is Pt pa && !CellSees(pa)) return;
            if (!FeedEvents.TryGetValue(l.Code, out var ev)) return;
            var c = NewCard(ev.kind, ev.title, ev.kind == "fight" ? "fire" : ev.kind == "warning" ? "!" : "·", "", null, ev.place);
            c.Event = l.Code; c.Footer = EventResult(l); c.Tone = ev.kind is "warning" or "fight" ? Feedback.Refused : Feedback.Info;
            lastEvent = c;
        }
        FeedCard lastEvent;

        /// <summary>A fallback card's result, in the player's words (Fable's table D): the numbers from the engine, never its sentence.</summary>
        string EventResult(LogLine l)
        {
            var g = Game; string t = l.Text;
            string Num() { var m = System.Text.RegularExpressions.Regex.Match(t, @"\d+"); return m.Success ? m.Value : "?"; }
            string Between(string a, string b) { int i = t.IndexOf(a, StringComparison.Ordinal); if (i < 0) return null; i += a.Length; int j = t.IndexOf(b, i, StringComparison.Ordinal); return j > i ? t[i..j] : null; }
            switch (l.Code)
            {
                case "rivals.arrive": return "Parked by the pickup. They want Vale too.";
                case "rivals.misled": return "They may be back.";
                case "rivals.drawn": return "They heard the shots.";
                case "guard.at-cafe": return "Away from the door.";
                case "guard.back": return "Watching the door again.";
                case "vale.exits": return g.PickupService ? "Walking to the service lane." : "Walking to the official car.";
                case "vale.at-car": return g.PickupService ? "In the service lane." : "Under the pickup camera.";
                case "car.blocks": return "The pickup camera is blind.";
                case "car.leaves": return "Heading for the service lane.";
                case "loop.ends": return "The camera sees again.";
                case "security.called": return "From the north street.";
                case "patrol.breaks": return t.Contains("gunfire") ? "Toward the gunfire." : "Toward the drawn weapon.";
                case "run.seen": { var who = Between("The ", " sees"); var ag = Between("sees the ", " running"); return who != null && ag != null ? $"The {who} saw the {Cap(ag)} run. Alert {g.Alert}." : $"Someone saw a runner. Alert {g.Alert}."; }
                case "report": { var who = Between("The ", " reports"); return who != null ? $"The {who} reported the attempt. Alert {g.Alert}." : $"Reported. Alert {g.Alert}."; }
                case "fight.loud": return $"{Num()} officers, from the north street." + (g.Lockdown < int.MaxValue / 8 ? $" The district seals at {MissionClock(g.Lockdown)}." : "");
                case "fight.safe": return $"{Num()} of them. The car can't leave while they stand.";
                case "fight.ambush": return t.Contains("warning") ? "Inez's warning came in time. You saw it first." : "The rivals open fire at the car.";
                case "fight.over": return "The last of them is down or gone. The car can go.";
                case "captured": return t;
            }
            return l.Code.EndsWith(".down") ? "Another agent can carry them." : "";
        }

        enum Seen { None, Played, Hidden }

        /// <summary>A world event's city scene: the first whose `sight` holds (always, or an agent within 13 m of the place with a clear line);
        /// Hidden when it has scenes but none the cell can see now.</summary>
        Seen CityScene(string ev, LogLine? l)
        {
            if (Text == null) return Seen.None;
            var r = Seen.None;
            foreach (var sc in Text.ScenesFor("city", ev))
            {
                var (at, key, where) = EventPlace(ev);
                if (sc.Sight == "in-sight" && !(at.HasValue && CellSees(at.Value))) { r = Seen.Hidden; continue; }
                var c = NewCard("city", SceneTitles.TryGetValue(ev, out var st) ? st : Title(ev.Replace('-', ' ')), "·", where, at.HasValue ? frame.World(at.Value) : null, key);
                c.Event = ev; c.Scene = sc; c.SceneN = Text.SceneUse(sc); Fill(c, Text.PlayScene(sc, c.SceneN)); c.Footer = Result(sc.Result, c);
                c.Revealed = 1; c.NextBeatAt = Game.Tick + 15; lastEvent = c;
                return Seen.Played;
            }
            return r;
        }

        (Pt? at, string key, string where) EventPlace(string ev) => ev switch
        {
            "rivals-arrive" => (Game.RivalsPose()?.pos ?? (Game.S.HasPlace("rival-park") ? Game.S.At("rival-park") : (Pt?)null), "rivals/0", "the pickup"),
            "guard-at-cafe" => (Game.GuardPose()?.pos, "guard", "café"),
            "chime" => (Game.S.At(Game.S.GuardPost), "marker/door", "the Office"),
            _ => (null, "", ""),
        };

        /// <summary>An agent within 13 m of a point with a clear line (Fable's in-sight: the Scout's witness range).</summary>
        bool CellSees(Pt p)
        {
            for (int k = 0; k < 4; k++) { var op = Game.Ops[k]; if (op.Inside || op.Down) continue; if (G.Within(op.Pos, p, 1300) && Game.S.Sight(op.Pos, p, Game.R.Eye)) return true; }
            return false;
        }

        /// <summary>The clock's city scenes (the chime), on their tick.</summary>
        void FeedClock()
        {
            if (Text == null) return;
            foreach (var e in Text.Events.Values) if (e.Clock.Length > 0 && e.Log.Count == 0 && Game.Tick == ClockTick(e.Clock)) _ = CityScene(e.Id, null);
        }

        /// <summary>The going-loud commit: a fight card with the card's numbers.</summary>
        void FeedLoud(LoudCard card, string what)
        {
            var c = NewCard("fight", "Going loud", "fire", what, SelectedWorld(), "");
            c.Footer = $"Alert {card.AlertTo}{(card.PoliceInS >= 0 ? $" · police in {card.PoliceInS} s" : "")}{(card.Seal >= 0 ? $" · sealed at {MissionClock(card.Seal)}" : "")}";
            c.Tone = Feedback.Refused; lastEvent = c;
        }

        /// <summary>Acknowledge a card (its tick): it leaves the stack for the history. A click on the card's body pans to its place.</summary>
        public void Acknowledge(FeedCard c, bool pan)
        {
            if (c == null) return;
            if (pan && PlaceNow(c) is Vector3 at) LookAt(at);
            if (c.Live || pan) return; // a click pans; the tick (or K) acknowledges
            c.Read = true; c.Revealed = c.Lines.Count; Feed.Remove(c); FeedHistory.Add(c); while (FeedHistory.Count > 60) FeedHistory.RemoveAt(0);
        }

        public void ReadAll() { foreach (var c in Feed.ToArray()) if (!c.Live) Acknowledge(c, false); }

        /// <summary>Where a card points now: its thing's view while it is on the street, else where it was.</summary>
        public Vector3? PlaceNow(FeedCard c)
        {
            if (c.PlaceKey.Length > 0) { var t = views.Find(c.PlaceKey); if (t && t.gameObject.activeSelf) return t.position; }
            if (c.Agent >= 0 && c.Place == null) return frame.World(Game.Ops[c.Agent].Pos);
            return c.Place;
        }

        /// <summary>K: the newest finished card.</summary>
        public void AcknowledgeNewest() { for (int i = Feed.Count - 1; i >= 0; i--) if (!Feed[i].Live) { Acknowledge(Feed[i], false); return; } }
    }
}
