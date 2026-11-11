using System.Collections.Generic;
using Suborned.Sim;

namespace Suborned.Play
{
    /// <summary>Brief 108 (Glyph's pages, briefs/108/glyph/pages; Sky 2026-11-11: "Yes, much better and clearer"): the briefing's words, text.json's book.brief block, placeholders
    /// for Fable: each page's title, the mission's story lines, the places (with the ways in among them) and what to look out for, the pager's words. All of it data: a word
    /// changes in the text, never in code. A text without the block shows no briefing pages (the host falls back to the job's name alone, never a blank).</summary>
    public sealed partial class BookWordsFile
    {
        public sealed class BriefRow { public string Id = "", Icon = "", Face = "", Word = "", Line = "", View = "", Way = "", Chip = "", When = ""; }
        public readonly List<string> BriefTitles = new(), Story = new();
        public readonly List<BriefRow> Places = new(), Dangers = new();
        public string BriefNext = "Next", BriefBack = "Back", BriefSkip = "Skip to Begin";
        public bool HasBrief => BriefTitles.Count == 3;

        void ReadBrief(JNode b)
        {
            static string S(JNode n, string k) => n?.Opt(k) is JNode v && v.Kind == JKind.Str ? v.Str() : "";
            static string Or(string a, string d) => a.Length > 0 ? a : d;
            if (b.Opt("brief") is not JNode br) return;
            BriefNext = Or(S(br, "next"), BriefNext); BriefBack = Or(S(br, "back"), BriefBack); BriefSkip = Or(S(br, "skip"), BriefSkip);
            foreach (var x in br["titles"].Items) BriefTitles.Add(x.Str());
            foreach (var x in br["story"].Items) Story.Add(x.Str());
            static BriefRow Row(JNode x) => new() { Id = S(x, "id"), Icon = S(x, "icon"), Face = S(x, "face"), Word = S(x, "word"), Line = S(x, "line"), View = S(x, "view"), Way = S(x, "way"), Chip = S(x, "chip"), When = S(x, "when") };
            foreach (var x in br["places"].Items) Places.Add(Row(x));
            foreach (var x in br["dangers"].Items) Dangers.Add(Row(x));
        }
    }
}
