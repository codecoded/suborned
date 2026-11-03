using System;
using Suborned.Sim;

namespace Suborned.Play
{
    /// <summary>Brief 080: the fire chip's words from the engine's preview (FirePreview.Acc, .Sum) and Fable's less.odds; the Enforcer's warning flash. Reads only.</summary>
    public sealed partial class HandoverHost
    {
        /// <summary>The per-cent chance a preview gives, on a rolled game with no refusal; -1 otherwise. Acc is read from the stop point on a walk-in.</summary>
        public int OddsOf(FirePreview fp) => OddsOf(fp, Game);
        public static int OddsOf(FirePreview fp, Game g) => g != null && g.Rolled && fp != null && fp.Refused.Length == 0 && fp.Acc > 0 ? fp.Acc / 10 : -1;

        public string OddsChipLine(int pct, string seconds) => (Text?.OddsChip ?? "{pct} % · down in about {s} s").Replace("{pct}", pct.ToString()).Replace("{s}", seconds);
        public string OddsBareLine(int pct) => (Text?.OddsChipBare ?? "{pct} %").Replace("{pct}", pct.ToString());

        static string Signed(int n) => n > 0 ? "+" + n : n < 0 ? "−" + (-n) : "0";

        /// <summary>Alt on the fire chip (Fable's less.odds): the sum, its limits, the streak note. "" with no rolled chance to explain.</summary>
        public string OddsAltLine()
        {
            var fp = OfferPreview; if (Text == null || OddsOf(fp) < 0 || Text.OddsSum.Length == 0) return "";
            var s = fp.Sum;
            string sum = Text.OddsSum.Replace("{pct}", s.Pct.ToString()).Replace("{aim}", s.Aim.ToString()).Replace("{range}", Signed(s.Range)).Replace("{cover}", Signed(s.Cover)).Replace("{move}", Signed(s.Move));
            return string.Join("\n", sum, Text.OddsLimits, Text.OddsStreakHead + ": " + Text.OddsStreakHits, Text.OddsStreakMisses);
        }

        const float EnforcerWarnFlashS = 4f;
        /// <summary>The Enforcer's warning went out within the last few seconds of mission time (the roster's flash; the card and the beat come off the log line).</summary>
        public bool EnforcerWarnFlash => Game != null && Game.EnforcerWarnedAt >= 0 && (Game.Tick - Game.EnforcerWarnedAt) < EnforcerWarnFlashS * Game.Hz;
    }
}
