using Suborned.Sim;

namespace Suborned.Play
{
    /// <summary>Brief 101 (Sky 2026-11-08, "I agree": no hit chance in play): brief 080's fire chip odds, its Alt breakdown (the sum, its limits,
    /// the streak note) and a rested price's one-line sum are gone; the hit rate stays in the debrief's numbers. Left here: the signed number
    /// the debrief writes, and the Enforcer's warning flash. Reads only.</summary>
    public sealed partial class HandoverHost
    {
        static string Signed(int n) => n > 0 ? "+" + n : n < 0 ? "−" + (-n) : "0";

        const float EnforcerWarnFlashS = 4f;
        /// <summary>The Enforcer's warning went out within the last few seconds of mission time (the roster's flash; the card and the beat come off the log line).</summary>
        public bool EnforcerWarnFlash => Game != null && Game.EnforcerWarnedAt >= 0 && (Game.Tick - Game.EnforcerWarnedAt) < EnforcerWarnFlashS * Game.Hz;
    }
}
