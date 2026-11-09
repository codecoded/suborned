using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 096, the rivals round in play (engine 0.41.0 GameCrewTurn.cs; Motive briefs/096/motive/rivals.html, rivals.json lookoutJob and thinning;
    /// Sky 2026-11-04 "all recommended"): the cell's ways on a member of the rival crew on foot, the lookout and the three posted round our car.
    ///  · Watch (any agent): the crew's timetable, once (as on their car).
    ///  · Listen (the Scout): his temperament, Mercenary or Loyal.
    ///  · Pay (the Fixer): the engine's price (rivals.turn: $30,000, 35 s, +2 T); a Mercenary takes it and walks off for good, a Loyal man refuses, the money
    ///    stays with the cell and his crew count the Fixer a foe. The card's outcome is "?" until a Listen (or a refusal) taught him. A camera that would tape
    ///    it marks the card as on a person's (HostTape; engine 0.52.0).
    ///  · Mislead (the Scout): cell.mislead's time and Trail; he goes to watch elsewhere for rivals.lookout.misled s and walks back, the card says when.
    ///  · Knock out, Kill, then carry, tie and hide: the takedown and body rows as on any person (engine 0.33.0 rivals.peopleRule; HostTakedown).
    /// Every price is the engine's (CrewQuote, MisleadBackTick); every lock is the engine's own answer (LearnQuery, CrewActQuery) and the order
    /// still asks the engine. Nothing here is a rule. Words are Fable's to change (text.json book.crew; placeholders below when absent).</summary>
    public sealed partial class HandoverHost
    {
        /// <summary>A member of the rival crew by his id, by the engine's own list (CrewOf; none on the fight range).</summary>
        bool IsCrew(string id) => !string.IsNullOrEmpty(id) && Game != null && !Game.Range && Game.CrewOf(id) != null;
        /// <summary>A member of the crew as the ring's target: on foot or posted (Kind rivals, his id); never the rival car, never the fight's hostile.</summary>
        bool CrewTarget(Target t) => t != null && t.Kind == "rivals" && IsCrew(t.Key);
        static Target CrewTargetOf(string id) => new Target { Kind = "rivals", Key = id, Id = id, Label = id };

        /// <summary>The member posted as the lookout (his role, rivals.lookout), "" none.</summary>
        public string LookoutId() => Game?.Crew.Find(x => x.Role == "lookout")?.Id ?? "";

        /// <summary>The member in the player's words: the lookout by his job, the others as the crew's one ("a rival").</summary>
        string CrewName(string id) => Game?.CrewOf(id) is CrewMember m && m.Role == "lookout" ? BookWordsFile.Get().CrewLookout : Text?.HoldersPhrase(id) ?? "a rival";

        /// <summary>The Scout's, Fixer's or anyone's way on a member (the plate's slot id: watch, listen, pay, mislead), priced by the engine; null when the
        /// way isn't his to offer (aboard, down, gone, in the fight: those have their own targets, or none).</summary>
        Intent CrewVerb(OpId by, int slot, string id, Target t)
        {
            var g = Game; var m = g.CrewOf(t.Key); if (m == null || m.Gun.Down || m.Paid || m.Fight || !m.OnFoot) return null;
            var w = BookWordsFile.Get(); string name = CrewName(m.Id);
            Intent I(string verb, string approach) => new Intent { Slot = slot, Label = Text?.WithTarget(approach.Length > 0 ? approach : verb, name) ?? $"{verb} {name}", Order = new Order { Op = Op.Name(by), Do = verb, Target = m.Id, Approach = approach } };
            switch (id)
            {
                case "watch":
                    {
                        if (g.RivalsWatched) return null; // the timetable is known: a Watch teaches nothing (already-watched), as on their car
                        var i = I("watch", ""); i.Time = $"{g.R.SightWatchS} s"; i.Trail = "T +0"; i.Outcome = w.CrewTimetable; return i;
                    }
                case "listen":
                    {
                        if (by != OpId.Scout) return null;
                        var i = I("listen", ""); var q = g.CrewQuote(i.Order, m); i.Time = $"{q.S} s"; i.Trail = "T +0"; i.Outcome = w.CrewTemperamentWord; i.Exact = true; return i;
                    }
                case "pay":
                    {
                        if (by != OpId.Fixer) return null;
                        var i = I("turn", "pay"); var q = g.CrewQuote(i.Order, m);
                        i.Usd = $"${q.Usd:N0}"; i.Time = $"{q.S} s"; i.Trail = $"T +{q.Trail}"; i.Alert = "A 0"; i.Outcome = q.Outcome; i.Exact = m.KTemperament; // private (rivals.turn): he reports nothing
                        Tape(i, q); // engine 0.52.0 (CrewQuote's QuoteRecord): a live camera in view tapes the Pay, its mark as on a person's card (HostTape)
                        i.Rows.Add(q.Outcome switch { "turned" => ("pay", w.CrewPayTakes), "refused" => ("talker", w.CrewPayRefuses), _ => ("p_unknown", w.CrewPayUnknown) });
                        return i;
                    }
                case "mislead":
                    {
                        if (by != OpId.Scout) return null;
                        var i = I("mislead", ""); var q = g.CrewQuote(i.Order, m); i.Time = $"{q.S} s"; i.Trail = $"T +{q.Trail}"; i.Outcome = "misled";
                        int back = -1; Guard("mislead back", () => back = g.MisleadBackTick(m.Id));
                        if (back >= 0) i.Rows.Add(("nextevent", w.CrewBack.Replace("{t}", MissionClock(back))));
                        i.Rows.Add(("sawyou", w.CrewFace));
                        return i;
                    }
            }
            return null;
        }

        /// <summary>The card's words for a way on a member (BuildPanel's Words): its name and what it does; false for any other way (the takedown and body rows
        /// keep HostTakedown's, Fire its own).</summary>
        bool CrewWords(Intent v, Target t)
        {
            var o = v.Order; if (!CrewTarget(t) || o == null || o.Do is not ("watch" or "listen" or "turn" or "mislead") || Game.CrewOf(o.Target) is not CrewMember m) return false;
            var w = BookWordsFile.Get(); string verb = o.Do == "turn" ? o.Approach : o.Do;
            v.Name = Text?.WithTarget(verb, CrewName(m.Id)) ?? v.Label;
            v.Does = o.Do switch
            {
                "watch" => w.CrewWatchDoes,
                "listen" => w.CrewListenDoes,
                "turn" => (m.KTemperament ? m.Temperament == "mercenary" ? w.CrewPayDoesMercenary : w.CrewPayDoesLoyal : w.CrewPayDoes).Replace("{usd}", v.Usd),
                _ => m.Role == "lookout" ? w.CrewMisleadDoesLookout : w.CrewMisleadDoesPosted,
            };
            return true;
        }

        /// <summary>Why a Pay or Mislead on a member can't be given now: the engine's own answer (Game.CrewActQuery, engine 0.43.0: as Issue would, needs-routine
        /// on a Pay before a Watch on the crew included). A busy agent the hold queues for (HoldOn) and the mission's end are the host's (Locked), as for LearnQuery.</summary>
        Answer CrewLock(OpId by, Order o)
        {
            var a = new Answer { ok = true, code = "", why = "" };
            Guard("crew query", () => a = Game.CrewActQuery(Op.Name(by), o.Do == "turn" ? "pay" : "mislead", o.Target));
            return a.ok || a.code is "agent-busy" or "mission-over" ? new Answer { ok = true, code = "", why = "" } : a;
        }

        /// <summary>The walk in to a member before the act (the engine's CrewWalkIn walks it): the host's walk to within reach, in seconds; 0 in reach.</summary>
        int CrewWalkS(OpId by, Order o) => ActPlace(by, o, CrewTargetOf(o.Target))?.Seconds ?? 0;

        /// <summary>The act card's footer once a Listen or Pay on a member lands (HostFeed's FooterOf), from the engine's state.</summary>
        string CrewFooter(FeedCard c)
        {
            var w = BookWordsFile.Get(); var m = Game.CrewOf(c.Target ?? ""); if (m == null) return "Done.";
            string who = Cap(CrewName(m.Id));
            if (c.Verb == "listen") { if (!m.KTemperament) return w.CrewHeardNothing; c.Tone = Feedback.Ok; return (m.Temperament == "mercenary" ? w.CrewHeardMercenary : w.CrewHeardLoyal).Replace("{who}", who); }
            if (m.Paid) { c.Tone = Feedback.Ok; return w.CrewPaid.Replace("{who}", who); }
            if (m.Refused) { c.Tone = Feedback.Refused; return w.CrewRefused.Replace("{who}", who); }
            c.Tone = Feedback.Info; return w.CrewPayMissed.Replace("{who}", who);
        }

        /// <summary>A chip under a crucial card clicked (the lookout card's Mislead, Pay, Take): its agent selected, the view on the lookout, and his ring opened
        /// on him for that agent; the card stays. A chip with no agent is a label.</summary>
        public void CrucialFix(int i)
        {
            var c = CrucialNow; if (c == null || i < 0 || i >= c.FixBy.Count || c.FixBy[i].Length == 0 || Game == null || !ModalGate.AllowsClicks(Modal)) return;
            OpId by; try { by = Op.Parse(c.FixBy[i]); } catch (FormatException) { return; }
            if (!Able((int)by)) { Pop($"{Cap(AgentName((int)by))} can't now.", Vector2.zero, null, Feedback.Refused, 2f); return; }
            var id = c.Kind == "lookout" ? LookoutId() : ""; var at = id.Length > 0 && Game.PosOf(id) is Pt p ? frame.World(p) : c.Place;
            Select(by, false); if (!at.HasValue) return;
            LookAt(at.Value);
            if (id.Length == 0 || Game.CrewOf(id) is not CrewMember m || m.Gun.Down || m.Paid || m.Fight) return; // down or gone: the view goes there, nothing to open
            var t = new Target { Kind = "rivals", Key = id, Id = id, Label = CrewName(id) };
            Vector2 screen = cam ? (Vector2)cam.WorldToScreenPoint(at.Value + Vector3.up * 1.5f) : new Vector2(Screen.width / 2f, Screen.height / 2f);
            ContextPlan plan = null; if (Guard("panel", () => plan = ContextFor(t, screen)) && plan != null) OpenRing(plan, screen, false);
        }
    }
}
