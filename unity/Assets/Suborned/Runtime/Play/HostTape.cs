using System;
using System.Collections.Generic;
using Suborned.Sim;

namespace Suborned.Play
{
    /// <summary>Brief 096 (engine 0.40.0, Keel's gdd-text-rules.md §05 cameras and attention; Motive's rules page; Sky 2026-11-04 "yeah, they should have been punished
    /// for not turning off camera"): what a watcher would make of an act, on its card before the commit.
    /// · The camera mark on Pay and Pressure (Quote.Recorded*): a live camera the cell knows would record it (the eye open, red), one it hasn't watched ("?", the
    ///   unknown eye); the record's Trail is in the Trail and its Alert in the Alert, each marked "?" for the unwatched one (T ?+n, A ?→n); the eye's hover and
    ///   the panel row say which camera and when its desk sees it;
    ///   Alt adds "loop ends m:ss" when a loop runs out inside the act. Help, Ask and Listen stay private: the engine gives them no mark.
    /// · The parked car on the Drive card (Game.ParkedQuote): on private ground, who would see the car standing there and how long until they report it.
    /// Reads only.</summary>
    public sealed partial class HandoverHost
    {
        public sealed partial class Intent
        {
            /// <summary>The act's camera mark: "yes" a camera the cell knows would record it, "?" one it doesn't know, "" none; which camera; seconds to its desk;
            /// the tick a loop on it ends inside the act (-1 none); the eye's hover line.</summary>
            public string Tape = "", TapeBy = "", TapeTip = ""; public int TapeInS = -1, TapeLoopEnds = -1;
            /// <summary>The Drive card's parked notice: "yes" a known watcher would see the car there, "?" an unknown one could, "" none; the hover line.</summary>
            public string Parked = "", ParkedTip = "";
        }

        static int AlertTo(string alert) { var a = alert ?? ""; int i = a.IndexOf('→'); return i >= 0 && int.TryParse(a[(i + 1)..].Trim(), out var v) ? v : 0; }
        static int TrailPlus(string trail) { var a = trail ?? ""; int i = a.IndexOf('+'); if (i < 0) return 0; int j = i + 1; while (j < a.Length && char.IsDigit(a[j])) j++; return j > i + 1 ? int.Parse(a[(i + 1)..j]) : 0; }

        /// <summary>A Pay or Pressure card's camera mark from the engine's quote.</summary>
        void Tape(Intent i, Quote q)
        {
            if (q == null || q.Recorded is not ("yes" or "?")) return;
            var w = BookWordsFile.Get(); string cam = CameraName(q.RecordedBy);
            i.Tape = q.Recorded; i.TapeBy = q.RecordedBy; i.TapeInS = q.RecordInS; i.TapeLoopEnds = q.RecordLoopEnds;
            if (q.Recorded == "yes") { i.Trail = $"T +{TrailPlus(i.Trail) + q.RecordTrail}"; i.Alert = $"A →{Math.Max(AlertTo(i.Alert), q.RecordAlert)}"; }
            else { i.Trail = $"T ?+{TrailPlus(i.Trail) + q.RecordTrail}"; if (AlertTo(i.Alert) == 0) i.Alert = $"A ?→{q.RecordAlert}"; } // brief 101 (Motive): a camera not yet watched may tape it: its Trail in the Trail as its Alert in the Alert, both marked "?"
            var tip = Clause.Fill((q.Recorded == "yes" ? w.TapeSeen : w.TapeMaybe).Replace("{camera}", cam).Replace("{alert}", q.RecordAlert.ToString()).Replace("{trail}", q.RecordTrail.ToString()),
                "{s}", q.RecordInS >= 0 ? q.RecordInS.ToString() : null); // brief 108: no time drops its clause, never "?"
            if (q.RecordLoopEnds >= 0) tip += " · " + w.TapeLoopEnds.Replace("{t}", MissionClock(q.RecordLoopEnds));
            i.TapeTip = Cap(tip); i.Rows.Add((q.Recorded == "yes" ? "eye_open" : "eye_unk", i.TapeTip));
        }

        /// <summary>The Drive card's parked notice for a stand (rulebook attention.parked): who would see our car standing there, and when they would report it.</summary>
        void ParkedNote(Intent i, Stand st)
        {
            (bool priv, List<string> seenBy, List<string> maybe, int noticeS, int waryS, int reportS, int patrolBack) q = default;
            if (!Guard("parked quote", () => q = Game.ParkedQuote(st.Id)) || !q.priv || q.seenBy == null) return;
            bool known = q.seenBy.Count > 0; if (!known && (q.maybe == null || q.maybe.Count == 0)) return;
            var w = BookWordsFile.Get(); string who = string.Join(", ", (known ? q.seenBy : q.maybe).ConvertAll(WatcherName));
            string tip = (known ? w.ParkedSeen : w.ParkedMaybe).Replace("{who}", who).Replace("{s}", q.reportS.ToString());
            if (q.patrolBack >= 0) tip += " · " + w.ParkedBack.Replace("{t}", MissionClock(q.patrolBack));
            i.Parked = known ? "yes" : "?"; i.ParkedTip = Cap(tip); i.Rows.Add((known ? "eye_open" : "eye_unk", i.ParkedTip));
            i.Alert = known ? $"A →{Math.Max(1, Game.Alert)}" : $"A ?→{Math.Max(1, Game.Alert)}";
        }
    }
}
