using System.Collections.Generic;
using Suborned.Sim;

namespace Suborned.Play
{
    /// <summary>Brief 096 §6 (Fable, briefs/096/fable/words.md §1; Sky 2026-11-04: "the End card needs to be plain english and clear but not too
    /// formal"): the end card worded by the engine's own reason for the fail, never one line per code. debrief.cause[fail][key] = { head, line,
    /// story? }: key is Game.Cause.Why, except the seal (keyed by Cause.Car, "unknown" when the engine gave none). story lists debrief.story keys,
    /// whose lines follow "Popovic reached the getaway car." in the route.</summary>
    public sealed partial class PlayText
    {
        public sealed class CauseText { public string Head = "", Line = ""; public readonly List<string> Story = new(); }
        /// <summary>debrief.cause by "fail|key".</summary>
        public readonly Dictionary<string, CauseText> DebriefCause = new();
        /// <summary>debrief.story: the lines after the car (blocked, unguarded, no-go, no-driver).</summary>
        public readonly Dictionary<string, string> DebriefStory = new();

        void ReadCause(JNode d)
        {
            if (d?.Opt("story") is JNode st && st.Kind == JKind.Obj) foreach (var kv in st.Entries) if (kv.Value.Kind == JKind.Str) DebriefStory[kv.Key] = kv.Value.Str();
            if (d?.Opt("cause") is not JNode cs || cs.Kind != JKind.Obj) return;
            foreach (var f in cs.Entries)
            {
                if (f.Value.Kind != JKind.Obj) continue;
                foreach (var w in f.Value.Entries)
                {
                    if (w.Value.Kind != JKind.Obj) continue;
                    var c = new CauseText { Head = S(w.Value, "head") ?? "", Line = S(w.Value, "line") ?? "" };
                    if (w.Value.Opt("story") is JNode sl && sl.Kind == JKind.Arr) c.Story.AddRange(sl.Strs());
                    DebriefCause[f.Key + "|" + w.Key] = c;
                }
            }
        }

        /// <summary>The cause's words, or null when the text has none (why "unresolved", a cause Fable has not worded).</summary>
        public CauseText CauseFor(string fail, string key) => DebriefCause.TryGetValue((fail ?? "") + "|" + (key ?? ""), out var c) ? c : null;
    }
}
