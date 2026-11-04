using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 096, Glyph's key-only fixes (Sky 2026-11-04: "I hate keyboard shortcuts - people have to learn them"; the audit in briefs/096/glyph/index.html §2):
    /// a click for every key a player needs. The pill's pause glyph pauses and resumes (Space); the scope's rim turns the view a quarter (Q / E); the fire line's Stop
    /// ends the fire (X, or the hold preset's stop); resting the pointer on a price for 0.6 s shows its breakdown, as Alt does for everything at once. No buttons for
    /// speed or next event (Sky's call 6). Each does exactly what its key does, so a replay is the same either way.</summary>
    public sealed partial class HandoverHost
    {
        /// <summary>The pause glyph clicked: as Space does in play.</summary>
        public void PauseClick()
        {
            if (Game == null || Game.Over || Modal != ModalGate.Modal.None || Starting || FightLabSpace()) return;
            Paused = !Paused; Status = Paused ? "Paused." : "Running.";
            if (!Paused) { if (ShowBriefing) ClockStarted = Time.unscaledTime; ShowBriefing = false; } else Save();
        }

        /// <summary>A turn button on the scope's rim: a quarter step, as Q (-1) or E (+1).</summary>
        public void TurnClick(int quarters)
        {
            if (Game == null || !ModalGate.AllowsCamera(Modal) || Following) return;
            TurnQueueCancel(); TurnBy(quarters < 0 ? -1 : 1);
        }

        /// <summary>The fire line's Stop: as X (the hold preset's stop, else cease fire).</summary>
        public void StopClick()
        {
            if (Game == null || Modal != ModalGate.Modal.None) return;
            if (HoldOn) HoldStop(); else if (!OneInput) CeaseFire(); else DrawOrHolster();
        }

        /// <summary>The pointer has rested on one price (a hovered thing, a ring way, the hold's pill) for PricePeekS: its breakdown shows, as with Alt.</summary>
        public bool PricePeek { get; private set; }
        public const float PricePeekS = 0.6f;
        int pricePeekKey; float pricePeekSince = -1f;

        /// <summary>Per frame from ShowSight, after Alt is read.</summary>
        void PricePeekFrame()
        {
            int k = 0;
            if (Ring != null && Ring.Hover >= 0) k = 7919 + Ring.Hover * 31 + (Ring.Plan?.T?.Key?.GetHashCode() ?? 0);
            else if (HoverIntent != null) k = 104729 + (HoverIntent.Label?.GetHashCode() ?? 0) * 17 + (Hover?.Key?.GetHashCode() ?? 0);
            else if (Hover != null) k = 31 + (Hover.Key?.GetHashCode() ?? 0);
            if (k != pricePeekKey) { pricePeekKey = k; pricePeekSince = Time.unscaledTime; }
            PricePeek = k != 0 && !Peeking && Ending.Length == 0 && Modal == ModalGate.Modal.None && Time.unscaledTime - pricePeekSince >= PricePeekS;
        }
    }
}
