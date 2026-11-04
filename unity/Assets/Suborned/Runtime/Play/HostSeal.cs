using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 057 §4 (Sky 2026-10-24, lost at 7:56: "huh? I failed mission for shots heard but that was a long time ago..."): the seal's
    /// clock, the getaway car's state and the fail line that says which of three things happened. Reads only (Game.Lockdown, SealS, CarHolders,
    /// AnyInCar, the fail's FailCause). Keel is adding FailCause.Car ("empty" | "ready" | "held") with the holder ids and, perhaps, a public seal
    /// origin; until they land these read them by name (reflection) and fall back to "unknown", so the card says the plain sealed line.</summary>
    public sealed partial class HandoverHost
    {
        const int NoSeal = int.MaxValue / 4;
        /// <summary>The seal is running: the response set Game.Lockdown and the run is not over.</summary>
        public bool SealOn => Game != null && !Game.Over && Game.Lockdown < NoSeal && Game.Lockdown > Game.Tick;
        /// <summary>Seconds until the seal (0 when none).</summary>
        public int SealLeftS => SealOn ? (Game.Lockdown - Game.Tick + Game.Hz - 1) / Game.Hz : 0;
        int sealSpanFrom = -1, sealSpanLock = -1;
        /// <summary>The underline's whole: the span since the marker first showed or the engine last moved the seal (HostDone's windowSpan rule).</summary>
        public int SealSpanS
        {
            get
            {
                if (!SealOn) { sealSpanFrom = sealSpanLock = -1; return 0; }
                if (sealSpanLock != Game.Lockdown) { sealSpanLock = Game.Lockdown; sealSpanFrom = Game.Tick; foreach (var w in Game.Windows()) if (w.Kind == "seal" && w.From >= 0) sealSpanFrom = w.From; } // engine 0.30: OpenWindow.From, the tick the clock was last set or pulled earlier
                return Math.Max(1, (Game.Lockdown - sealSpanFrom + Game.Hz - 1) / Game.Hz);
            }
        }
        /// <summary>Vale aboard the getaway car (Go's precondition): the engine's AnyInCar counts Vale.</summary>
        public bool ValeAboard => Game != null && Game.Vale == ValeAt.Car;

        /// <summary>What started the seal, as one icon for the marker's hover disc (brief 057 §4.1): fire = the shooting, eye_open = a missed
        /// check-in or a camera (the desk), radio = shots heard, police = an officer or robot reported or confirmed it. From the engine's seal
        /// origin when it is readable (FailCause's CauseOrigin; Keel may expose it live), else from the log.</summary>
        public string SealCauseIcon()
        {
            var g = Game; if (g == null) return "";
            CauseOrigin o = g.SealOrigin; // engine 0.30.0: the report whose seal time runs now
            if (g.Cause is FailCause fc && fc.Origin != null) o ??= fc.Origin;
            if (o != null)
            {
                if (o.Because == "loud-fight") return "fire";
                var k = o.Reporter?.Kind ?? "";
                return k is "desk" or "camera" ? "eye_open" : k == "heard" ? "radio" : k is "officer" or "robot" or "guard" or "patrol" ? "badge:signal_police" : o.Report.StartsWith("gunfire", StringComparison.Ordinal) ? "radio" : "badge:signal_police";
            }
            return "badge:signal_police";
        }

        /// <summary>FailCause.Car and its holders by name (Keel's addition), else ("", ""). Holders come back as Answer.ids would ("rival-1,officer-2").</summary>
        public static (string car, string holders) SealCarOf(FailCause c)
        {
            if (c == null || c.Fail != "sealed") return ("", "");
            string car = c.Car ?? ""; string ids = string.Join(",", c.Holders); // engine 0.30.0
            return (car is "empty" or "ready" or "held" ? car : "", ids);
        }

        /// <summary>The fail camera's tip for the car's state (Fable's less.sealed.tip); null where the text has none. The card's line and the
        /// story after the car are debrief.cause's (brief 096, HostEndWords).</summary>
        public string SealedTip(FailCause c)
        {
            var (car, ids) = SealCarOf(c); var t = Text; if (t == null || car.Length == 0) return null;
            return t.SealedTip.TryGetValue(car, out var tp) ? tp?.Replace("{holders}", t.HoldersPhrase(ids)) : null;
        }
        /// <summary>The car state the fail recorded ("" unknown): the end card drops "Vale reached the getaway car" when he had not.</summary>
        public string SealedCar => Game?.Cause is FailCause c ? SealCarOf(c).car : "";
    }
}
