using System;
using System.Collections.Generic;
using Suborned.Sim;

namespace Suborned.Play
{
    public sealed partial class PlayText
    {
        public readonly Dictionary<string, string> SealLive = new()
        {
            ["heading"] = "District seals in {time}",
            ["empty"] = "Get Popovic into the car and leave.",
            ["ready"] = "Leave in the getaway car. Press Go.",
            ["held"] = "Clear the exit, then press Go.",
            ["cause"] = "Security is closing the exits."
        };

        void ReadSeal(JNode sd)
        {
            if (sd == null) return;
            foreach (var (k, into) in new[] { ("tip", SealedTip), ("live", SealLive) } /* line and story: debrief.cause (brief 096) */)
                if (sd.Opt(k) is JNode m) foreach (var kv in m.Entries) if (kv.Value.Kind == JKind.Str) into[kv.Key] = kv.Value.Str();
        }

        public string SealHeading(int seconds)
        {
            int n = Math.Max(0, seconds);
            return SealLive["heading"].Replace("{time}", $"{n / 60}:{n % 60:00}");
        }
    }
}
