using System.Collections.Generic;
using Suborned.Sim;

namespace Suborned.Play
{
    /// <summary>Brief 096 §6 (Sky 2026-11-04: "the End card needs to be plain english and clear but not too formal"; his run read "Rivals took
    /// Popovic. The rival crew got to Popovic first." with Popovic in the cell's unguarded car): the end card's head, line and the story after
    /// the car, from the engine's own reason (Game.Cause: Fail and Why; the seal by Car). Fable's debrief.cause and debrief.story
    /// (briefs/096/fable/words.md §1). Reads only: the cause, the culprits and Game.CarHolders() at the end tick.</summary>
    public sealed partial class HandoverHost
    {
        public struct EndWords { public string Head, Line; public List<string> Story; }

        /// <summary>The key debrief.cause words a cause by: its why; the seal's car state ("unknown" when the engine gave none).</summary>
        public static string CauseKey(FailCause c) => c == null ? "" : c.Fail == "sealed" ? (SealCarOf(c).car is { Length: > 0 } car ? car : "unknown") : c.Why;

        /// <summary>The cause's words with their fills; Head null when the text has none for it (the card keeps less.end.fail and
        /// briefing.fails). {holders} is names.holders over `holders` (Game.CarHolders() at the end; the seal's own holders when that is
        /// empty); a story line that names them is left out when there are none.</summary>
        public EndWords CauseWords(FailCause c, IReadOnlyList<string> holders)
        {
            var w = new EndWords { Story = new List<string>() }; var t = Text;
            var ct = c != null && t != null ? t.CauseFor(c.Fail, CauseKey(c)) : null; if (ct == null) return w;
            var ids = new List<string>(holders ?? new string[0]); if (ids.Count == 0 && c.Fail == "sealed") ids.AddRange(c.Holders);
            string hp = ids.Count > 0 ? t.HoldersPhrase(string.Join(",", ids)) : "";
            string agent = c.Ops.Count > 0 ? "the " + (t.Speakers.TryGetValue(c.Ops[0], out var ag) ? ag : c.Ops[0]) : "us";
            string Fill(string s) => Cap(s.Replace("{by}", FailByWords(c, null)).Replace("{agent}", agent).Replace("{holders}", hp).Replace("{vale}", ValeWord));
            w.Head = ct.Head.Length > 0 ? Fill(ct.Head).TrimEnd('.') : null; w.Line = ct.Line.Length > 0 ? Fill(ct.Line) : null;
            foreach (var k in ct.Story)
                if (t.DebriefStory.TryGetValue(k, out var s) && s.Length > 0 && !(s.Contains("{holders}") && hp.Length == 0)) w.Story.Add(Fill(s));
            return w;
        }

        /// <summary>The ended run's words (the end card and the bots' check read the same).</summary>
        public EndWords EndCauseWords() => Game?.Cause is FailCause c ? CauseWords(c, Game.CarHolders()) : new EndWords { Story = new List<string>() };
    }
}
