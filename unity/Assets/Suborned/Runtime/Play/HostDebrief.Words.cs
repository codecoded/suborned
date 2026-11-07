using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Suborned.Sim;
using UnityEngine;
using File = System.IO.File;
using Path = System.IO.Path;

namespace Suborned.Play
{
    /// <summary>Brief 099, the debrief page's words, by key, never in code: text.json's `debrief.page` (Fable's block, contracts/missions/handover/debrief.schema.ts,
    /// written through the style agent), and for a key that block does not have yet the clearly marked stand-ins (Resources/Debrief099/standins.json: Glyph's
    /// words in Fable's shape, briefs/099/glyph/words.md). Each key switches to Fable's the moment it exists. A key in neither shows nothing; a text whose fill
    /// is left unfilled shows nothing (never half a sentence). Read once, at the first ending: nothing here costs a frame in play.</summary>
    public sealed class DebriefWords
    {
        JNode fable, stand, root;
        /// <summary>The keys read from the stand-ins so far (the proof and the report print them: what Fable has still to write).</summary>
        public readonly SortedSet<string> FromStandIn = new(StringComparer.Ordinal);
        /// <summary>The keys read from Fable's block so far.</summary>
        public readonly SortedSet<string> FromFable = new(StringComparer.Ordinal);
        public bool HasFable => fable != null;

        static DebriefWords one;
        public static DebriefWords Get() => one ??= Load();
        /// <summary>Tests: read the files again (a test that swaps text.json or the stand-ins).</summary>
        public static void Reset() => one = null;

        static DebriefWords Load()
        {
            var w = new DebriefWords();
            try
            {
                var path = Path.Combine(SimBundle.Dir, "text.json");
                if (File.Exists(path)) { w.root = JNode.Parse(File.ReadAllText(path), "text.json").Opt("debrief"); w.fable = w.root?.Opt("page"); }
            }
            catch (Exception e) { Debug.LogWarning($"Debrief: text.json's debrief.page not read ({e.Message}); the stand-ins show"); }
            try
            {
                string s = Resources.Load<TextAsset>("Debrief099/standins")?.text;
                if (s == null) { var p = Path.Combine(Application.dataPath, "Suborned", "Resources", "Debrief099", "standins.json"); if (File.Exists(p)) s = File.ReadAllText(p); } // the Editor's checks and hostlogic read the file itself
                if (s != null) w.stand = JNode.Parse(s, "standins.json");
            }
            catch (Exception e) { Debug.LogWarning($"Debrief: the stand-ins not read ({e.Message})"); }
            return w;
        }

        static JNode At(JNode n, string[] path) { for (int i = 0; n != null && i < path.Length; i++) n = n.Opt(path[i]); return n; }

        /// <summary>The words at a key path (debrief.page.reasons.time.win: "reasons", "time", "win"): Fable's when written, else the stand-in, else "".</summary>
        public string W(params string[] path)
        {
            string key = string.Join(".", path);
            if (At(fable, path) is { Kind: JKind.Str } f && f.Str().Length > 0) { FromFable.Add(key); return f.Str(); }
            if (At(stand, path) is { Kind: JKind.Str } s && s.Str().Length > 0) { FromStandIn.Add(key); return s.Str(); }
            return "";
        }

        /// <summary>The end card's own words in text.json's debrief block (headline, result: Fable's, no stand-in), "" when missing.</summary>
        public string Debrief(params string[] path) => At(root, path) is { Kind: JKind.Str } n ? n.Str() : "";

        /// <summary>The keys under a path in either source, Fable's first (a record such as how, beats, trail.rows, next.by).</summary>
        public List<string> Keys(params string[] path)
        {
            var l = new List<string>();
            foreach (var src in new[] { fable, stand }) if (At(src, path) is { Kind: JKind.Obj } o) foreach (var k in o.Keys) if (!l.Contains(k)) l.Add(k);
            return l;
        }

        static readonly Regex Hole = new(@"\{[a-z][a-zA-Z-]*\}", RegexOptions.CultureInvariant);
        /// <summary>The words with their fills; "" when the words are missing or a fill is left unfilled (a row says nothing rather than guess). `cap`: the first letter up.</summary>
        public static string Fill(string s, bool cap, params (string k, string v)[] fills)
        {
            if (string.IsNullOrEmpty(s)) return "";
            foreach (var (k, v) in fills) if (v != null) s = s.Replace("{" + k + "}", v);
            if (Hole.IsMatch(s)) return "";
            return cap && s.Length > 0 ? char.ToUpperInvariant(s[0]) + s[1..] : s;
        }

        /// <summary>The words at a path, filled (see Fill).</summary>
        public string F(string[] path, bool cap, params (string k, string v)[] fills) => Fill(W(path), cap, fills);
    }
}
