using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Suborned.Play
{
    /// <summary>Brief 108 (Aster, after "On the street by ?." in the Journal): a value put into a card's words, or, when the engine honestly has none yet, the clause that holds it
    /// dropped. Never a "?" standing in for a time or a number (a hidden fallback). A clause is a sentence, or a " · " part of a plate's line; a line whose only clause held it
    /// comes back empty, and the caller shows nothing.</summary>
    public static class Clause
    {
        static readonly Regex Sentences = new(@"(?<=[.!?])\s+");

        public static string Fill(string s, string token, string value)
        {
            if (string.IsNullOrEmpty(s) || !s.Contains(token)) return s ?? "";
            if (value != null) return s.Replace(token, value);
            var kept = new List<string>();
            foreach (var sentence in Sentences.Split(s.Trim()))
            {
                if (!sentence.Contains(token)) { kept.Add(sentence); continue; }
                var parts = sentence.Split(" · "); if (parts.Length < 2) continue;
                var left = new List<string>(); foreach (var p in parts) if (!p.Contains(token)) left.Add(p);
                if (left.Count > 0) kept.Add(string.Join(" · ", left));
            }
            return string.Join(" ", kept).Trim();
        }

        /// <summary>The engine's own clock in its line ("on the street 2:08.2"): the m:ss after the words, or null.</summary>
        public static string ClockAfter(string text, string words)
        {
            if (string.IsNullOrEmpty(text)) return null;
            var m = Regex.Match(text, Regex.Escape(words) + @"\s+(\d+:\d\d)");
            return m.Success ? m.Groups[1].Value : null;
        }
    }
}
