using System.Collections.Generic;
using Suborned.Sim;

namespace Suborned.Play
{
    /// <summary>Brief 096 (Glyph's mocks briefs/096/glyph; Sky 2026-11-04: "all. recommended"): the book's new words, placeholders for Fable in text.json's
    /// book block: the start (book.start), the Journal (book.journal), the HUD's Book key (book.key), the slim feed's crucial cards (book.feed) and the held Go
    /// (book.heldGo). Each block is optional: a text without one shows the host's own placeholder, never a blank.</summary>
    public sealed partial class BookWordsFile
    {
        public sealed class FeedCardWords { public string Title = "", Line = ""; public readonly List<(string icon, string word)> Fixes = new(); }

        public string Begin = "Begin", Shared = "", PurseWord = "Purse", HeatWord = "Heat";
        public readonly Dictionary<string, string> StartCell = new(), Stations = new();
        public string JournalSub = "", JournalNew = "{n} new", JournalBegins = "The job begins", JournalEmpty = "";
        public readonly List<(string id, string word)> JournalFilters = new();
        public string KeyWord = "Book", StopWord = "Stop", FeedOf = "{i} of {n}", HeldBy = "held by", HeldFix = "";
        public readonly Dictionary<string, FeedCardWords> FeedCards = new();

        void Read096(JNode b)
        {
            static string S(JNode n, string k) => n?.Opt(k) is JNode v && v.Kind == JKind.Str ? v.Str() : "";
            static string Or(string a, string b) => a.Length > 0 ? a : b;
            if (b.Opt("start") is JNode st)
            {
                Begin = Or(S(st, "begin"), Begin); Shared = S(st, "shared"); PurseWord = Or(S(st, "purse"), PurseWord); HeatWord = Or(S(st, "heat"), HeatWord);
                if (st.Opt("cell") is JNode c) foreach (var kv in c.Entries) StartCell[kv.Key] = kv.Value.Str();
                if (st.Opt("stations") is JNode sn) foreach (var kv in sn.Entries) Stations[kv.Key] = kv.Value.Str();
            }
            if (b.Opt("journal") is JNode j)
            {
                JournalSub = S(j, "sub"); JournalNew = Or(S(j, "new"), JournalNew); JournalBegins = Or(S(j, "begins"), JournalBegins); JournalEmpty = S(j, "empty");
                if (j.Opt("filters") is JNode fl) foreach (var x in fl.Items) JournalFilters.Add((S(x, "id"), S(x, "word")));
            }
            if (b.Opt("key") is JNode k) { KeyWord = Or(S(k, "word"), KeyWord); StopWord = Or(S(k, "stop"), StopWord); }
            if (b.Opt("feed") is JNode f)
            {
                FeedOf = Or(S(f, "of"), FeedOf);
                if (f.Opt("cards") is JNode cs)
                    foreach (var kv in cs.Entries)
                    {
                        var w = new FeedCardWords { Title = S(kv.Value, "title"), Line = S(kv.Value, "line") };
                        if (kv.Value.Opt("fixes") is JNode fx) foreach (var x in fx.Items) w.Fixes.Add((S(x, "icon"), S(x, "word")));
                        FeedCards[kv.Key] = w;
                    }
            }
            if (b.Opt("heldGo") is JNode h) { HeldBy = Or(S(h, "heldBy"), HeldBy); HeldFix = S(h, "fix"); }
        }

        /// <summary>A crucial card's words by its kind; the kind itself when the text has none (never blank).</summary>
        public FeedCardWords Card(string kind) => FeedCards.TryGetValue(kind, out var w) ? w : new FeedCardWords { Title = kind };
    }
}
