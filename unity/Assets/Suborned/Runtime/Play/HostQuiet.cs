using System;
using System.Collections.Generic;

namespace Suborned.Play
{
    /// <summary>Brief 102 (Sky 2026-11-08, on the agents' barks: "Maybe even drop them? They will feel repetitive"; on a scripted line: "that's exactly the kind of scripting
    /// that bugs me"): the cell is quiet in play. One rule, here: a line of the cell's own (an order taken, the stock "On it." and "Heading to …" too, an arrival, a landing, a
    /// read, a warning, a moment in the street) is not said: no bubble, no Journal line, no ticker. The cards and marks carry the news (the lozenge's "Rivals nearing our car"
    /// and "Enforcer badly hurt", the act rings, the portraits, the window marks).
    ///  · The scenes keep the cell's voice: the Fixer's approaches and their openers, the Fixer's calls and the Listen scenes play by their own paths (HostConvo, the feed's
    ///    scenes) and never meet this rule. CellSpeaks names the moments that are a scene's lines but come through the bark path, so they still speak.
    ///  · A moment Sky misses gets its line back by adding its key to CellSpeaks: "done/loop" (the Tech's loop landing), "enforcer-warning", "ack/takedown"; "*" brings back
    ///    all the old chatter (for a comparison, as the host logic's checks do).
    ///  · The world's people speak as before: a quiet line still takes its turn at its moment (WorldBeat's `most`), so the same people speak at the same moments, and its pick
    ///    is still made, so every later line is the variant it was. View only: the engine never hears of any of this, and a replay is untouched.</summary>
    public sealed partial class HandoverHost
    {
        /// <summary>The moments the cell still speaks at in the street, by "event/verb" (or the event alone, for every verb).</summary>
        public static readonly HashSet<string> CellSpeaks = new(StringComparer.Ordinal)
        {
            "arrive/ask", "arrive/pay", "arrive/help", "arrive/pressure", // the Fixer's opener to the person (the approach scenes play it; this path, only for a driven Fixer)
            "done/turn",   // the Fixer's answer to the person's reply in an approach, when no scene says it
            "done/take",   // the Enforcer taking Popovic at his car (Popovic's reply, `taken`, is his own)
            "done/enter",  // the Enforcer bringing Popovic out of the Office
            "cliffhanger", // the Fixer answering Popovic in the car, at the win
        };

        public sealed class CellLine { public int Tick; public string Moment = "", Speaker = "", Text = ""; public bool Said; }
        /// <summary>For checks: the cell's lines that met the rule this run, said or kept quiet (the last 400).</summary>
        public readonly List<CellLine> CellLines = new();

        static bool IsCellVoice(string speaker) => speaker is "scout" or "fixer" or "tech" or "enforcer" or "cell";
        static string Moment(string ev, string verb) => string.IsNullOrEmpty(verb) ? ev ?? "" : $"{ev}/{verb}";

        /// <summary>The rule. True: this is the cell's line at a moment the cell keeps quiet, so it is not said. A line with no moment is quiet; anyone else's line never is.</summary>
        bool CellQuiet(string speaker, string moment, string text)
        {
            if (!IsCellVoice(speaker)) return false;
            moment ??= ""; int slash = moment.IndexOf('/');
            bool said = CellSpeaks.Contains(moment) || slash > 0 && CellSpeaks.Contains(moment[..slash]) || CellSpeaks.Contains("*");
            CellLines.Add(new CellLine { Tick = Game?.Tick ?? 0, Moment = moment, Speaker = speaker, Text = text ?? "", Said = said });
            while (CellLines.Count > 400) CellLines.RemoveAt(0);
            return !said;
        }
    }
}
