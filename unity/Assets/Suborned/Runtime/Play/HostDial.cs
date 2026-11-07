using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 098 step 0, the dial (Sky 2026-11-07: "I should say, add 3 more guards, take some away, bring in more cars etc - the game then just does that, no long
    /// processing, and I play and get a feel"): the host's side of Keel's start.counts (sim Start.cs, GamePopulation.cs). How many guards, people and (step 1) passing cars the
    /// next game has, by kind (Population.Kinds), set on the start page's dev panel (HudToolkit.Dial) or by `-counts guards=19,people=150,cars=16` (the shot rig, perf runs). The engine builds that
    /// world at the start from the seed and contracts/rules/population.json (in the bundle); the counts are part of the start, so the saved session carries them beside the
    /// seed (Script.Write) and seed + counts + orders replay. A kind at today's number is left unset, and a start that sets none is today's game byte for byte.
    /// Which counts a game plays:
    ///  · Replay and Restart (BeginSameSeed) keep the counts of the game just played, as they keep its seed;
    ///  · the panel's Play (and Begin, when the panel's numbers changed) builds the street behind the page again with them, on the same seed, then begins;
    ///  · else -counts, as given; else, for a person playing, the numbers last played (PlayerPrefs, so Sky iterates across launches). A proof, a batch row or a
    ///    probe (HostSeed's rule for the seed) plays today's world unless it passes -counts, and never reads or writes the numbers.
    /// Each start's census (Game.Census: asked, plays, the note, who was added and who left) goes to the player log.</summary>
    public sealed partial class HandoverHost
    {
        static bool countsPinRead; static SortedDictionary<string, int> countsPin;
        /// <summary>`-counts guards=19,people=150`: every new game plays these counts, as given (null: none). Read once from the command line.</summary>
        public static SortedDictionary<string, int> CountsPin
        {
            get { if (!countsPinRead) { countsPinRead = true; if (GameBoot.ArgValue("-counts") is string v) countsPin = ParseCounts(v); } return countsPin; }
            set { countsPinRead = true; countsPin = value; }
        }

        /// <summary>"guards=19,people=150" (':' for '=' too): kind → count, each a kind the dial builds (unknown = reject), 0..Population.CountCap.</summary>
        public static SortedDictionary<string, int> ParseCounts(string spec)
        {
            var d = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (var raw in (spec ?? "").Split(','))
            {
                var part = raw.Trim(); if (part.Length == 0) continue;
                var kv = part.Split(new[] { '=', ':' }, 2);
                if (kv.Length != 2 || Array.IndexOf(Population.Kinds, kv[0].Trim()) < 0 || !int.TryParse(kv[1].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var n) || n > Population.CountCap)
                    throw new FormatException($"-counts: '{part}' is not kind=n (kinds: {string.Join(", ", Population.Kinds)}; n 0..{Population.CountCap})");
                d[kv[0].Trim()] = n;
            }
            return d;
        }

        /// <summary>A person playing: the numbers last played persist (PlayerPrefs). A proof, a batch row or a probe neither reads nor writes them.</summary>
        static bool DialPrefs => !(Application.isBatchMode || GameBoot.Automation && !Application.isEditor);
        public const string DialPrefPrefix = "suborned.dial.";

        /// <summary>The next Begin plays these counts once (Replay, Restart, the panel's Play), over the pin and the numbers last played; empty: none.</summary>
        SortedDictionary<string, int> countsAgain;
        List<KindCensus> dialToday; Engine.Inputs dialTodayFor;
        /// <summary>Today's zone by kind (Population.Today: the people file's count, the floor and the most); null without the dial's rules, and in the fight range.</summary>
        public List<KindCensus> DialToday
        {
            get
            {
                var i = Bundle?.Inputs; if (i?.Population == null || rangeMode) return null;
                if (!ReferenceEquals(dialTodayFor, i)) { dialToday = Population.Today(i); dialTodayFor = i; }
                return dialToday;
            }
        }
        public KindCensus DialTodayOf(string kind) { var t = DialToday; if (t != null) foreach (var c in t) if (c.Kind == kind) return c; return null; }

        /// <summary>What the game behind the page plays of a kind: its census when the dial is on, else today's number.</summary>
        public int DialPlays(string kind)
        {
            if (Game != null && Game.DialOn) foreach (var c in Game.Census) if (c.Kind == kind) return c.Count;
            return DialTodayOf(kind)?.Today ?? 0;
        }

        Dictionary<string, int> dialEdit; Game dialEditOn;
        /// <summary>The numbers set on the panel for the game behind it; null when none (a new game drops an edit made on the one before).</summary>
        Dictionary<string, int> DialEdit => dialEdit != null && ReferenceEquals(dialEditOn, Game) ? dialEdit : null;
        /// <summary>The number the panel shows for a kind: the one set on it, else what the game behind it plays.</summary>
        public int DialShown(string kind) => DialEdit is { } e && e.TryGetValue(kind, out var v) ? v : DialPlays(kind);
        /// <summary>The panel's numbers differ from the game behind the page: Play (or Begin) builds it again first.</summary>
        public bool DialPending { get { var e = DialEdit; if (e == null) return false; foreach (var kv in e) if (kv.Value != DialPlays(kv.Key)) return true; return false; } }

        /// <summary>A − or + on the panel: the kind's number by `by`, kept within its floor (the cast never leave) and its most.</summary>
        public void DialStep(string kind, int by)
        {
            var t = DialTodayOf(kind); if (t == null || Game == null) return;
            DialEditOpen(); dialEdit[kind] = Mathf.Clamp(dialEdit[kind] + by, t.Floor, t.Max);
        }
        /// <summary>The panel's Today: every kind back to today's number.</summary>
        public void DialReset() { var t = DialToday; if (t == null || Game == null) return; DialEditOpen(); foreach (var c in t) dialEdit[c.Kind] = c.Today; }
        void DialEditOpen()
        {
            if (DialEdit != null) return;
            dialEdit = new Dictionary<string, int>(StringComparer.Ordinal); dialEditOn = Game;
            foreach (var k in Population.Kinds) dialEdit[k] = DialPlays(k);
        }

        /// <summary>The panel's Play: the street behind the page built again with the panel's numbers (the same seed, so only the numbers change), then the start
        /// sequence as Begin gives it. Numbers unchanged: Begin as it is.</summary>
        public void DialPlay()
        {
            if (Game == null || rangeMode || Fault != null) return;
            if (!DialPending) { dialEdit = null; if (ShowBriefing) BriefingBegin(); return; }
            var c = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (var kv in dialEdit) if (DialTodayOf(kv.Key) is { } t && kv.Value != t.Today) c[kv.Key] = kv.Value; // a kind at today's number stays unset: today's world byte for byte
            if (DialPrefs) try { foreach (var k in Population.Kinds) { if (c.TryGetValue(k, out var n)) PlayerPrefs.SetInt(DialPrefPrefix + k, n); else PlayerPrefs.DeleteKey(DialPrefPrefix + k); } PlayerPrefs.Save(); }
                catch (Exception e) { Debug.LogWarning($"Dial: the numbers were not kept for the next launch ({e.Message}); this game plays them"); } // a save that fails never stops the game
            countsAgain = c; seedAgain = Game.Seed; dialEdit = null;
            var t0 = System.Diagnostics.Stopwatch.GetTimestamp(); Begin(); DialBuildMs = (System.Diagnostics.Stopwatch.GetTimestamp() - t0) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            Debug.Log($"Dial: Play built the street in {DialBuildMs:0} ms (the engine's start and every view)");
            if (Fault == null && ShowBriefing) BriefingBegin();
        }
        /// <summary>What the panel's last Play took to build the street (Begin: the engine's start and every view), in ms; -1 before one.</summary>
        public double DialBuildMs { get; private set; } = -1;

        /// <summary>NewFightGame, after the seed: the counts this game plays (none: today's world).</summary>
        void CountsForBegin(Start s)
        {
            var c = countsAgain; countsAgain = null;
            if (rangeMode) return; // the fight range has no people file: the dial builds none
            c ??= CountsPin ?? (DialPrefs ? DialLast() : null);
            if (c != null) foreach (var kv in c) s.Counts[kv.Key] = kv.Value;
        }

        /// <summary>Replay and Restart (HostSeed.BeginSameSeed): the next Begin keeps the counts of the game just played (none kept as none).</summary>
        void DialKeep() => countsAgain = Game != null ? new SortedDictionary<string, int>(Game.Start.Counts, StringComparer.Ordinal) : null;

        /// <summary>The numbers last played (PlayerPrefs), by kind; null when none differ from today's.</summary>
        static SortedDictionary<string, int> DialLast()
        {
            SortedDictionary<string, int> c = null;
            foreach (var k in Population.Kinds) if (PlayerPrefs.HasKey(DialPrefPrefix + k)) (c ??= new SortedDictionary<string, int>(StringComparer.Ordinal))[k] = Mathf.Clamp(PlayerPrefs.GetInt(DialPrefPrefix + k), 0, Population.CountCap);
            return c;
        }

        /// <summary>A new game's census to the player log (a line a kind; with no counts, one line saying the world is today's); returns the game.</summary>
        Game DialStarted(Game g)
        {
            if (g == null || rangeMode) return g;
            if (!g.DialOn) { var t = DialToday; Debug.Log($"Dial: unset, today's world (seed {g.Seed}){(t == null ? "" : ": " + string.Join(", ", t.ConvertAll(c => $"{c.Kind} {c.Today}")))}"); return g; }
            foreach (var c in g.Census) Debug.Log($"Dial: {CensusLine(c)} (seed {g.Seed})");
            return g;
        }

        /// <summary>One kind's census in words: asked, plays, today's, the floor and the most, the note, who was added and who left.</summary>
        public static string CensusLine(KindCensus c)
        {
            var sb = new StringBuilder();
            sb.Append(c.Kind).Append(" asked ").Append(c.Asked.HasValue ? c.Asked.Value.ToString(CultureInfo.InvariantCulture) : "unset").Append(", plays ").Append(c.Count)
              .Append(" (today ").Append(c.Today).Append(", fewest ").Append(c.Floor).Append(", most ").Append(c.Max).Append(')');
            if (c.Note.Length > 0) sb.Append(": ").Append(c.Note);
            sb.Append("; added ").Append(c.Added.Count); if (c.Added.Count > 0) sb.Append(" (").Append(c.Added[0]).Append(c.Added.Count > 1 ? ".." + c.Added[c.Added.Count - 1] : "").Append(')');
            sb.Append("; removed ").Append(c.Removed.Count); if (c.Removed.Count > 0) sb.Append(" (").Append(string.Join(", ", c.Removed)).Append(')');
            return sb.ToString();
        }
    }
}
