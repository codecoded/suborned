using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 085 (Fable's handover-to-latch.md, Glyph section 2; engine 0.38.0): what a pressed call says in the street, clock running: on guard.at-cafe her line then his, in place of the
    /// guard-at-cafe scene, the Scout's line included (the feed's beat when no agent is near); on guard.leaves-cafe only the Scout's line (his may still run on as he runs); on guard.recalled his line
    /// is dropped if it hasn't shown. They are found by route, not by whether a cutaway played, so Skip, Never and First time still get them. A speaker off screen speaks from a docked live inset, and
    /// so do the guard's lines on any Call: nothing else new does. Also the Door price field. All view only.</summary>
    public sealed partial class HandoverHost
    {
        readonly List<(float due, string speaker, string text)> streetDue = new();
        void ResetStreet() { streetDue.Clear(); doorRead = (-1, "", null); }

        PlayText.ConvStreet StreetFor(string code)
        {
            if (Text == null) return null;
            foreach (var role in new[] { "waitress" })
            {
                var conv = Text.LookupConversation(ConvoQuery("call", role)); if (conv == null) continue;
                foreach (var u in conv.Street) if (u.On == code && u.Pressed) return u;
            }
            return null;
        }

        /// <summary>A log line of a pressed visit: true when its words are said here (the usual card and lines stay quiet).</summary>
        bool PressedStreet(LogLine l)
        {
            var g = Game; if (!g.GuardCallPressed) return false;
            if (l.Code == "guard.recalled") { streetDue.RemoveAll(d => d.speaker == "guard"); return false; }
            if (l.Code == "guard.leaves-cafe") { Beat("guard-leaves-cafe", null, "scout", kind: Feedback.Info); return true; } // never "Break's over" on a run
            if (l.Code != "guard.at-cafe" || StreetFor(l.Code) is not PlayText.ConvStreet u || u.Beats.Count == 0) return false;
            var card = NewCard("city", SceneTitles.TryGetValue("guard-at-cafe", out var st) ? st : "The guard is at the café", "·", "café", WorldOf("guard"), "guard"); card.Event = "guard-at-cafe"; card.Footer = u.Beat; lastEvent = card;
            foreach (var b in u.Beats) card.Lines.Add(new FeedLine { Speaker = b.Speaker, Name = Text.ConvName(b.Speaker), Text = b.Line }); card.Revealed = card.Bubbled = card.Lines.Count;
            if (!(g.GuardPose() is { } gp && CellSees(gp.pos))) { var (badge, tinted) = BeatBadge("call"); PushBeat(u.Beat, badge, tinted, null); } // no agent near: the feed says it
            float at = Time.unscaledTime; foreach (var b in u.Beats) { streetDue.Add((at, b.Speaker, b.Line)); at += ReadS(b.Line); }
            StreetFrame(); return true;
        }

        /// <summary>Per frame: a street line whose time has come, held as long as it takes to read. His follows him, as any bubble follows its speaker.</summary>
        void StreetFrame()
        {
            float now = Time.unscaledTime;
            for (int i = 0; i < streetDue.Count; i++)
            {
                var (due, sp, tx) = streetDue[i]; if (due > now) continue;
                streetDue.RemoveAt(i--); int k = Array.IndexOf(OpKeys, sp);
                Talk.Add(new Said { Born = now, Tick = Game.Tick, Speaker = sp, Name = Text?.ConvName(sp) ?? sp, Text = tx, Agent = k, Kind = Feedback.Info });
                var b = new Bubble { Speaker = sp, Text = tx, Born = now, Life = ReadS(tx), Tick = Game.Tick, Inset = true };
                Bubbles.Add(b); SpeechInset(b); while (Bubbles.Count > 12) Bubbles.RemoveAt(0);
            }
        }

        /// <summary>Sky said yes (2026-10-31): the guard's lines on any Call use the live inset when he's off screen. He is on a call from its tick until he is back at his door.</summary>
        bool GuardOnACall(string speaker) => speaker == "guard" && Game != null && Game.LureTick >= 0 && Game.Tick < Game.GuardAwayUntil;

        /// <summary>Glyph section 2: a line said off screen shows in a docked inset of its speaker alone, one at a time, with no agent badge. On screen the HUD draws no inset: the ordinary bubble says it.</summary>
        void SpeechInset(Bubble b)
        {
            Callouts.RemoveAll(c => c.Speech != null);
            if (views == null || Game == null) return;
            Callouts.Add(new Callout { Watcher = "speech/" + b.Speaker, View = b.Speaker, Op = "", Stage = 1, Seq = ++calloutSeq, Born = b.Born, Until = b.Born + b.Life, Speech = b });
        }

        (int tick, string role, string clock) doorRead = (-1, "", null);
        /// <summary>The Door price field (Fable's handover, Glyph section 3): "1:40" for the Pressure way on Inez, and for the Call way once she's been pressured; null hides it. Read from the
        /// route from where he stands now, so never cached past the tick. The engine's read covers a role with no lover, the guard down or busy, a call already made, Alert over the call's limit.</summary>
        public string DoorClock(Order o)
        {
            if (Game == null || !IsRole(o.Target)) return null;
            if (doorRead.tick == Game.Tick && doorRead.role == o.Target + o.Do) return doorRead.clock;
            string clock = null; var w = Game.GuardCallWindows(o.Target);
            if (w != null && !(o.Do == "call" && !Game.PersonByRole(o.Target).Has(Game.R.SummonPressedAct))) clock = DoorWords(w.Value.pressed);
            doorRead = (Game.Tick, o.Target + o.Do, clock); return clock;
        }
        /// <summary>Ticks at 10 a second, to the nearest 10 s: 989 reads 1:40.</summary>
        public static string DoorWords(int ticks) { int s = (ticks + 50) / 100 * 10; return $"{s / 60}:{s % 60:00}"; }
    }
}
