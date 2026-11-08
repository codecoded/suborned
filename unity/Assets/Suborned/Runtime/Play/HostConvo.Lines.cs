using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 085 (Fable's handover-to-latch.md): which lines an act that has landed plays. A conversation (scenes.conversations, found by the engine's outcome, never a count of orders) plays whole,
    /// its cards after it; an act with none plays today's scenes.items lines whole, Mislead the rival's `rivals-misled` line (brief 102: the Scout's `done` is a landing, quiet in play). First time counts by the conversation's id, else the scene's key, else
    /// verb and target for Mislead. An interrupted act plays no scene. When no scene may play the same lines stay as bubbles. View only: the engine never hears of any of this.</summary>
    public sealed partial class HandoverHost
    {
        readonly HashSet<string> convoMet = new(StringComparer.Ordinal);
        /// <summary>For checks: the scene each landing chose (a conversation's id, a scene's key), this run.</summary>
        public readonly List<string> ConvoPicks = new();
        /// <summary>A landing whose lines a scene or the card's own bubbles already say: its reply, done and confirm lines stay in the history, not over heads.</summary>
        int convoQuietTick = -1; readonly HashSet<string> convoQuiet = new(StringComparer.Ordinal);
        bool ConvoQuiet(string speaker) => Game != null && convoQuietTick >= 0 && Game.Tick - convoQuietTick <= 1 && convoQuiet.Contains(speaker); /* the tick after too: an order given just before a step logs on it */
        void QuietNow(params string[] who) { convoQuietTick = Game.Tick; convoQuiet.Clear(); foreach (var w in who) convoQuiet.Add(w); Bubbles.RemoveAll(b => b.Tick == Game.Tick && convoQuiet.Contains(b.Speaker)); Callouts.RemoveAll(c => c.Speech != null && c.Speech.Tick == Game.Tick && convoQuiet.Contains(c.Speech.Speaker)); /* brief 085: its inset goes with the bubble */ }

        static bool ConvoVerb(string v) => v is "listen" or "ask" or "pay" or "help" or "pressure" or "call" or "mislead";

        /// <summary>The act really landed this tick (its log line), rather than being cut short: an interrupted act plays no scene.</summary>
        bool ActLanded(FeedCard c)
        {
            var log = Game.Log;
            for (int i = log.Count - 1; i >= 0; i--)
            {
                var l = log[i]; if (l.Tick != Game.Tick) break;
                if (c.Verb == "mislead" ? l.Code == "rivals.misled" : c.Verb == "listen" ? l.Text.Contains($"({c.Target})") && !l.Code.EndsWith(".down") : l.Code.StartsWith("turn." + c.Verb + ".") && l.Text.Contains($"({c.Target})")) return true;
            }
            return false;
        }

        /// <summary>Fable's table: the query for an act on a cast person, read from the engine at its landing.</summary>
        PlayText.ConvQuery ConvoQuery(string verb, string role)
        {
            var p = Game.PersonByRole(role); var q = new PlayText.ConvQuery { Verb = verb, About = role, Temperament = p.Temperament, Turned = p.Turned };
            if (verb is "ask" or "pay" or "help" or "pressure") q.Outcome = TurnOutcome(role);
            string tag = $"({role})";
            foreach (var l in Game.Log)
            {
                if (!l.Code.StartsWith("turn.") || !l.Text.Contains(tag)) continue;
                if (l.Tick < Game.Tick && l.Code.StartsWith("turn.help.")) q.Helped = true;
                if (l.Code.EndsWith(".turned")) { var ps = l.Code.Split('.'); if (ps.Length > 2) q.TurnedBy = ps[1]; }
            }
            return q;
        }

        /// <summary>Why a scene can't play now (null: it can).</summary>
        string ConvoBlock(Convo c)
        {
            var g = Game;
            if (g == null || g.Over || Ending.Length > 0 || Fault != null) return "over";
            if (MenuOpen || ShowBriefing || FailCamOn) return "modal";
            if (Frozen || TargetFreeze) return "freeze"; // a freeze, or target mode (Y)
            if (g.FightOn || g.Alert >= 2) return "fight";
            var op = g.Ops[c.Agent]; if (op.Down || op.Inside) return "agent";
            if (c.KeyB.Length == 0 || ConvoView(c.KeyB) is not Transform t || !t.gameObject.activeInHierarchy) return "unseen";
            foreach (var (sp, _) in c.Lines)
            {
                if (!IsRole(sp) && Array.IndexOf(OpKeys, sp) < 0) continue; // a voice with no figure in the street (Milena, a dispatcher): no one to wait for
                if (ConvoView(sp) is not Transform st || !st.gameObject.activeInHierarchy) return "unseen";
                if (CastOf(sp) is Person p && p.Down) return "person";
                int ok = Array.IndexOf(OpKeys, sp); if (ok >= 0 && (g.Ops[ok].Down || g.Ops[ok].Inside)) return "agent";
            }
            if (CastOf(c.KeyB) is Person pb && pb.Down) return "person";
            return null;
        }

        /// <summary>An act landed (FeedTrack's Land, FeedCall, which lands at once): its lines and whether a cutaway plays them; a card's own lines stay for bubbles otherwise.</summary>
        void ConvoLanded(int k, string verb, string target, FeedCard card)
        {
            if (!ConvoVerb(verb) || Text == null) return;
            string agent = Op.Name((OpId)k); bool rival = verb == "mislead" || target.Contains("rivals");
            var c = new Convo { Agent = k, Verb = verb, Target = target, Card = card, KeyA = agent, KeyB = rival ? "rivals" : target, Alone = verb == "call" };
            var conv = IsRole(target) ? Text.LookupConversation(ConvoQuery(verb, target)) : null;
            if (conv != null)
            {
                c.Id = conv.Id; c.MetKey = conv.Id; foreach (var b in conv.Beats) { c.Lines.Add((b.Speaker, b.Line)); c.Fields.Add(b); } c.Learned.AddRange(conv.Learned); c.Opens.AddRange(conv.Opens);
            }
            else { c.MetKey = verb == "mislead" ? $"mislead/{target}" : card?.Scene != null ? PlayText.SceneKey(card.Scene) : $"{verb}/{target}"; ConvoItems(c, card, verb, agent, rival); }
            ConvoPicks.Add(c.MetKey); string why = c.Lines.Count == 0 ? "no lines" : controls.conversations == ConversationScenes.Never ? "never" : controls.conversations == ConversationScenes.FirstEachScene && convoMet.Contains(c.MetKey) ? "met" : ConvoNow != null || convoWant != null ? "another scene" : ConvoBlock(c);
            if (why != null) { ConvoLast = $"bubbles ({why}): {verb} {target}"; ConvoRelease(c); return; }
            convoMet.Add(c.MetKey); convoWant = c;
            var said = new List<string> { agent, target }; if (rival) said.Add("rivals"); foreach (var (sp, _) in c.Lines) said.Add(sp);
            QuietNow(said.ToArray());
            if (card != null)
            {
                if (conv != null) { card.Lines.Clear(); foreach (var b in conv.Beats) card.Lines.Add(new FeedLine { Speaker = b.Speaker, Name = Text.ConvName(b.Speaker), Text = b.Line }); if (conv.Learned.Count > 0) card.Footer = conv.Learned[0]; }
                card.Revealed = card.Bubbled = card.Lines.Count; card.NextBeatAt = -1; // the whole scene stays in the history, and isn't bubbled again
            }
        }

        /// <summary>No conversation: the scene's lines, the same words as today, past the reply. Listen frames the overheard pair when they stand together (brief 102: the Scout's arrive, quiet in play, no longer opens it).</summary>
        void ConvoItems(Convo c, FeedCard card, string verb, string agent, bool rival)
        {
            var lines = card?.Lines;
            string Peek(string ev, string v, string sp) { var l = Text.PeekLine(new PlayText.Query { Event = ev, Verb = v, Speaker = sp }); return string.IsNullOrEmpty(l) ? null : Fill(l, Name(c.Target), 0, 0, AgentNames[c.Agent]); }
            switch (verb)
            {
                case "mislead":
                    if (Peek("done", "mislead", agent) is string dn && !CellQuiet(agent, "done/mislead", dn)) c.Lines.Add((agent, dn)); // brief 102: her report is a landing, quiet in play; the rival's answer is his own
                    if (Peek("rivals-misled", null, "rivals") is string rm) c.Lines.Add(("rivals", rm));
                    break;
                case "listen":
                    if (lines == null || lines.Count == 0) break;
                    if (Peek("arrive", "listen", agent) is string ar && !CellQuiet(agent, "arrive/listen", ar)) c.Lines.Add((agent, ar)); // brief 102: the Scout listens; she doesn't announce it
                    foreach (var l in lines) if (l.Text.Length > 0) c.Lines.Add((l.Speaker, l.Text));
                    foreach (var l in lines) if (l.Speaker != c.Target && IsRole(l.Speaker) && ConvoView(l.Speaker) is Transform os && os.gameObject.activeInHierarchy && ConvoView(c.Target) is Transform ts && Vector3.Distance(os.position, ts.position) < 6f) { c.KeyA = l.Speaker; break; }
                    break;
                default:
                    if (lines != null) foreach (var l in lines) if (l.Text.Length > 0) c.Lines.Add((l.Speaker, l.Text));
                    break;
            }
        }

        /// <summary>No cutaway for these lines: they play whole as bubbles, a beat every 1.5 s of mission time, from the first; the reply and the Fixer's reaction aren't said a second time over them.</summary>
        void ConvoRelease(Convo c)
        {
            var card = c.Card;
            if (c.Id.Length > 0) { ConvoReleaseAgreed(c); return; }
            if (card == null || card.Scene == null || c.Verb == "call") return; // a call's scene is already running (FeedCall)
            card.Revealed = 0; card.Bubbled = 0; card.NextBeatAt = Game.Tick;
            if (c.Verb is not "listen" and not "mislead") QuietNow(Op.Name((OpId)c.Agent), c.Target);
        }

        /// <summary>Brief 085: no cutaway, but the act has a conversation: its own agreed words, never the old scene's. The street gets its first three lines as bubbles, each held to read, one after
        /// another, since ten lines over a head are too many and hold nothing up; the feed card and the history keep the whole scene; the cards follow, on the person (HostConvo.Cards). The guard's
        /// call line stays (his inset on any Call), so only the agent's and the person's lines are silenced.</summary>
        void ConvoReleaseAgreed(Convo c)
        {
            QuietNow(Op.Name((OpId)c.Agent), c.Target);
            var card = c.Card; var conv = Text.Conversations.Find(x => x.Id == c.Id);
            if (card != null && conv != null) { card.Lines.Clear(); foreach (var b in conv.Beats) card.Lines.Add(new FeedLine { Speaker = b.Speaker, Name = Text.ConvName(b.Speaker), Text = b.Line }); if (conv.Learned.Count > 0) card.Footer = conv.Learned[0]; card.Revealed = card.Bubbled = card.Lines.Count; card.NextBeatAt = -1; }
            float at = Time.unscaledTime; int n = 0;
            foreach (var (sp, tx) in c.Lines) { if (n++ >= StreetAgreedLines) break; streetDue.Add((at, sp, tx, false)); at += ReadS(tx); }
            if (c.HasCards) ConvoCardsAfter(c, at);
        }
        public const int StreetAgreedLines = 3;

        /// <summary>An act cut short: it says nothing more, and its card is no longer waiting on lines.</summary>
        void ConvoCutShort(FeedCard c) { c.Revealed = c.Bubbled = c.Lines.Count; c.NextBeatAt = -1; }
    }
}
