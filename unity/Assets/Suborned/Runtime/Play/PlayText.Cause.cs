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

        /// <summary>The debrief's words (moved here from Read in brief 100, so PlayText.cs stays the size it was): Standing, between missions,
        /// the getaway, the route, the final fight and its why, then the end card by cause.</summary>
        void ReadDebrief(JNode n)
        {
            if (n.Opt("debrief")?.Opt("standing") is JNode ds)
            {
                StandingLine = S(ds, "line") ?? StandingLine;
                if (ds.Opt("bands") is JNode sb) foreach (var kv in sb.Entries) StandingBands[kv.Key] = kv.Value.Str();
                if (ds.Opt("causes") is JNode sc2) foreach (var kv in sc2.Entries) StandingCauses[kv.Key] = kv.Value.Str();
            }
            if (n.Opt("debrief")?.Opt("between") is JNode dbw) foreach (var kv in dbw.Entries) if (kv.Value.Kind == JKind.Str) DebriefBetween[kv.Key] = kv.Value.Str();
            if (n.Opt("debrief")?.Opt("getaway") is JNode dga) // brief 018 §3: the getaway's debrief words
            {
                GetawayHoldsOut = S(dga, "holdsOut") ?? GetawayHoldsOut; GetawayPost = S(dga, "post") ?? GetawayPost;
                foreach (var (k, into) in new[] { ("fate", GetawayFate), ("why", GetawayWhy), ("by", GetawayBy) }) if (dga.Opt(k) is JNode m) foreach (var kv in m.Entries) into[kv.Key] = kv.Value.Str();
            }
            if (n.Opt("debrief")?.Opt("route") is JNode dr && dr.Kind == JKind.Obj) foreach (var kv in dr.Entries) if (kv.Value.Kind == JKind.Str) Route[kv.Key] = kv.Value.Str();
            if (n.Opt("debrief")?.Opt("fight") is JNode dfi && dfi.Kind == JKind.Obj) foreach (var kv in dfi.Entries) if (kv.Value.Kind == JKind.Str) DebriefFight[kv.Key] = kv.Value.Str(); // the Final fight line by branch (loud, safe)
            if (n.Opt("debrief")?.Opt("fightWhy") is JNode dfw && dfw.Kind == JKind.Obj) foreach (var kv in dfw.Entries) if (kv.Value.Kind == JKind.Str) FightWhy[kv.Key] = kv.Value.Str(); // engine 0.33.0: a clause per id of finalFight.why
            ReadCause(n.Opt("debrief")); // brief 096 (Fable): the end card by cause (debrief.cause) and the story after the car (debrief.story)
        }

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
