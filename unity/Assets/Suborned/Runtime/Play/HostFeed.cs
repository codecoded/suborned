using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>The scene feed (Sky 2026-10-15: cards at the bottom right showing the cell's conversations and what is happening, so the player
    /// sees an act complete and what it learned, and can acknowledge it). Structure only, placeholder words and look until Glyph's round 6b
    /// and Fable's `scenes` land:
    ///  · an act card opens when an agent's act begins (Game.ActivityOf goes to "acting"); while it runs it shows the act's progress and
    ///    fills with the lines said by that agent and the person it is about; when the act lands, a footer says what came of it, read from
    ///    the engine's state (the routine learned, the need and temperament, turned or refused, the loop holding…); cancelled, it says so;
    ///  · an event card for what happens in the city that the cell should see (the rivals arrive, the guard at the café, Vale comes out, the
    ///    alarm), with the city's lines;
    ///  · a card stays until acknowledged: a click on it (which also pans to its place) or K for the oldest; the header counts the unread,
    ///    and acknowledged cards go to a collapsed history. Real time, no pause; only the cards themselves take clicks.
    /// The feed reads the run and never changes it, so a replay is untouched.</summary>
    public sealed partial class HandoverHost
    {
        public sealed class FeedCard
        {
            public int Id, Agent = -1, Started; public string Kind = "act", Verb = "", Target = "", Title = "", Footer = "", Event = "";
            public readonly List<(string who, string text)> Lines = new();
            public Vector3? Place; public string PlaceKey = ""; public bool Live, Read; public int Progress = -1, TicksLeft = -1, Ended = -1; public Feedback Tone = Feedback.Info;
        }
        /// <summary>Cards not yet acknowledged (live ones first in age order), and the acknowledged history (newest last).</summary>
        public readonly List<FeedCard> Feed = new(), FeedHistory = new();
        public bool FeedHistoryOpen { get; set; }
        public int Unread { get { int n = 0; foreach (var c in Feed) if (!c.Live && !c.Read) n++; return n; } }
        readonly FeedCard[] actCard = new FeedCard[4];
        readonly string[] lastPhase = new string[4];
        int feedIds;

        void ResetFeed() { Feed.Clear(); FeedHistory.Clear(); Array.Clear(actCard, 0, 4); Array.Clear(lastPhase, 0, 4); feedIds = 0; }

        static readonly HashSet<string> FeedActs = new() { "watch", "listen", "turn", "loop", "reroute", "mislead", "enter", "take", "carry" };

        /// <summary>After each Track: an act starting opens its card; a card's progress follows the act; an act ending closes it.</summary>
        void FeedTrack()
        {
            for (int k = 0; k < 4; k++)
            {
                var d = doing[k]; string was = lastPhase[k]; lastPhase[k] = d.Phase;
                var c = actCard[k];
                if (d.Phase == "acting" && FeedActs.Contains(d.Verb) && (c == null || !c.Live))
                {
                    var op = Game.Ops[k]; string target = op.Busy?.Target ?? "", approach = op.Busy?.Approach ?? "";
                    c = actCard[k] = new FeedCard { Id = ++feedIds, Agent = k, Verb = d.Verb == "turn" ? approach : d.Verb, Target = target, Live = true, Started = Game.Tick, Title = $"{AgentNames[k]} · {d.Label}" };
                    c.Place = target.Length > 0 ? TargetWorld(new Target { Kind = target is "waitress" or "driver" or "guard" or "councillor" ? "person" : target, Key = target }) ?? frame.World(op.Pos) : frame.World(op.Pos);
                    Feed.Add(c);
                }
                if (c != null && c.Live)
                {
                    if (d.Phase == "acting") { c.Progress = d.Progress; c.TicksLeft = d.TicksLeft; }
                    else
                    {
                        c.Live = false; c.Progress = -1; c.Ended = Game.Tick;
                        if (c.Footer.Length == 0) c.Footer = FooterOf(c);
                    }
                }
            }
        }

        /// <summary>What came of an act, from the engine's state when it ends (placeholder words; Fable's `scenes` will say it).</summary>
        string FooterOf(FeedCard c)
        {
            var g = Game; string who = Name(c.Target);
            switch (c.Verb)
            {
                case "watch": { var p = g.PersonByRole(c.Target); c.Tone = p.KRoutine ? Feedback.Ok : Feedback.Info; return p.KRoutine ? $"Learned: {who}'s routine." : "Nothing learned."; }
                case "listen":
                    {
                        var p = g.PersonByRole(c.Target);
                        if (!p.KNeed && !p.KTemperament) return "Nothing learned.";
                        c.Tone = Feedback.Ok;
                        return $"Learned: {Cap(who)} {NeedWords(p.Need)}; {TemperWords(p.Temperament)}.";
                    }
                case "ask": case "pay": case "help": case "pressure":
                    {
                        var p = g.PersonByRole(c.Target);
                        if (p.Turned) { c.Tone = Feedback.Ok; return $"Turned: {who} is with you. Call them when it counts."; }
                        if (p.KnownRefused.Contains(c.Verb)) { c.Tone = Feedback.Refused; return $"Refused: {who} said no, and will remember the approach."; }
                        return $"Not yet: {who} hasn't decided.";
                    }
                case "loop": c.Tone = g.LoopOn ? Feedback.Ok : Feedback.Info; return g.LoopOn ? "The entrance camera repeats one minute while the Tech stays at the cabinet." : "The loop didn't hold.";
                case "reroute": c.Tone = Feedback.Ok; return "The patrol is off its sweep for a while.";
                case "mislead": c.Tone = g.RivalsPresent ? Feedback.Info : Feedback.Ok; return g.RivalsPresent ? "The tip missed." : "The rivals follow the false tip. They may be back.";
                case "enter": return g.Vale == ValeAt.WithEnforcer ? "Out at the service lane with Vale." : g.Ops[(int)OpId.Enforcer].Inside ? "Inside the Office." : "Back out without Vale.";
                case "take": c.Tone = g.Vale == ValeAt.WithEnforcer ? Feedback.Ok : Feedback.Info; return g.Vale == ValeAt.WithEnforcer ? "Vale is with the Enforcer: get him to the car." : "Vale was not taken.";
                case "carry": return "Lifted: walk them to the car.";
            }
            return "Done.";
        }

        /// <summary>A line said goes to the live card of the agent who said it, or of the act about the person who said it.</summary>
        void FeedSay(string speaker, string text)
        {
            int k = Array.IndexOf(OpKeys, speaker);
            // a card takes lines while its act runs, and on the tick it lands (the read, the reply and the Fixer's word come with the landing)
            bool Open(FeedCard c) => c != null && (c.Live || c.Ended == Game.Tick);
            if (k >= 0 && Open(actCard[k])) { actCard[k].Lines.Add((Text?.Speaker(speaker) ?? speaker, text)); return; }
            for (int i = 0; i < 4; i++)
            {
                var c = actCard[i]; if (!Open(c) || c.Target != speaker) continue;
                c.Lines.Add((Text?.Speaker(speaker) ?? speaker, text)); return;
            }
        }

        /// <summary>A cancel closes the agent's card with what it cost.</summary>
        void FeedCancelled(int k, string cost)
        {
            var c = actCard[k]; if (c == null || !c.Live) return;
            c.Live = false; c.Progress = -1; c.Ended = Game.Tick; c.Footer = $"Cancelled: {cost}"; c.Tone = Feedback.Info;
        }

        // the city's events that get a card (placeholder set until Glyph and Fable say which), and where each points
        static readonly Dictionary<string, (string title, string place)> FeedEvents = new()
        {
            ["rivals.arrive"] = ("Rivals arrive", "rivals/0"), ["rivals.misled"] = ("The rivals leave", "rivals/0"), ["rivals.drawn"] = ("Rivals drawn to the gunfire", "rivals/0"),
            ["guard.at-cafe"] = ("The guard at the café", "guard"), ["guard.back"] = ("The guard is back on the door", "guard"),
            ["vale.exits"] = ("Vale comes out", "councillor"), ["vale.at-car"] = ("Vale at his car", "councillor"),
            ["car.blocks"] = ("The car blocks the pickup camera", "car/official"), ["car.leaves"] = ("The official car leaves", "car/official"),
            ["loop.ends"] = ("The loop ends", "camera/entrance"), ["security.called"] = ("Security called", "security/0"), ["patrol.breaks"] = ("The patrol breaks its route", "patrol"),
            ["run.seen"] = ("A runner was seen", ""), ["report"] = ("Reported", ""), ["fight.loud"] = ("The fight: police", "security/0"), ["fight.safe"] = ("The fight: the rivals wait", "car/getaway"),
            ["fight.over"] = ("The fight is over", "car/getaway"), ["captured"] = ("Captured", ""),
        };

        /// <summary>A log line that gets a card: the engine's sentence, and the city's lines said for it (gathered for a few ticks).</summary>
        void FeedEvent(LogLine l)
        {
            if (!FeedEvents.TryGetValue(l.Code, out var ev)) return;
            var c = new FeedCard { Id = ++feedIds, Kind = "event", Event = l.Code, Title = ev.title, Started = Game.Tick, Footer = l.Text, PlaceKey = ev.place,
                Tone = l.Code is "security.called" or "report" or "run.seen" or "captured" or "rivals.drawn" ? Feedback.Refused : Feedback.Info };
            lastEvent = c; Feed.Add(c);
            while (Feed.Count > 24) { var old = Feed.Find(x => !x.Live); if (old == null) break; Feed.Remove(old); old.Read = true; FeedHistory.Add(old); }
        }
        FeedCard lastEvent;

        /// <summary>The city's lines said in the same tick as an event card go on it.</summary>
        void FeedCity(string speaker, string text)
        {
            if (lastEvent != null && lastEvent.Started == Game.Tick && Array.IndexOf(OpKeys, speaker) < 0) lastEvent.Lines.Add((Text?.Speaker(speaker) ?? speaker, text));
        }

        /// <summary>Acknowledge a card: it leaves the queue for the history; a click also pans to its place.</summary>
        public void Acknowledge(FeedCard c, bool pan)
        {
            if (c == null) return;
            if (pan && PlaceNow(c) is Vector3 at) LookAt(at);
            if (c.Live) return; // a live card stays until its act ends (the pan still helps)
            c.Read = true; Feed.Remove(c); FeedHistory.Add(c); while (FeedHistory.Count > 40) FeedHistory.RemoveAt(0);
        }

        /// <summary>Where a card points now: its thing's view while it is on the street (the rivals, the guard, a car), else where it was.</summary>
        public Vector3? PlaceNow(FeedCard c)
        {
            if (c.PlaceKey.Length > 0) { var t = views.Find(c.PlaceKey); if (t && t.gameObject.activeSelf) return t.position; }
            if (c.Agent >= 0 && c.Place == null) return frame.World(Game.Ops[c.Agent].Pos);
            return c.Place;
        }

        /// <summary>K: the oldest finished card.</summary>
        public void AcknowledgeOldest() { foreach (var c in Feed) if (!c.Live) { Acknowledge(c, false); return; } }
    }
}
