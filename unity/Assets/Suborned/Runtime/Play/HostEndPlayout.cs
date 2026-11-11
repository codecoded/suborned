using UnityEngine;
using UnityEngine.InputSystem;

namespace Suborned.Play
{
    /// <summary>Brief 109 (Sky on 0.2.330: "never just end abruptly"): an ending where someone leaves plays out on screen before the end card. The engine
    /// stops on the deciding tick; the view drives the leaving on (Popovic driven away: HostViews.LeavingOfficial; our getaway's Go: HostViews.Leaving;
    /// a rival car already driving off: HostViews.LeavingRivals), and the card waits while Popovic's car or ours is still in the city's view, up to
    /// EndPlayoutMaxS. The view follows the car out (JuiceFollow, as the getaway's follow) until the player moves the camera; a click shows the card at
    /// once (Esc still opens the pause menu). With a failure the fail camera plays it and follows the car (HostFailCam). On for a player's launch, off for
    /// automation, so proofs and checks see the card as before: `-endplay on|off` (Juice109.Decide's rule). View only: the run, its replay and hash are
    /// untouched.</summary>
    public sealed partial class HandoverHost
    {
        public static readonly bool EndPlayout = Juice109.Decide(System.Environment.GetCommandLineArgs(), "-endplay");
        const float EndPlayoutMaxS = 20f;
        Suborned.Sim.Game endHeldFor; float endHeldSince; bool endHeldSkipped, endFollowOff;

        /// <summary>The run has ended and its leaving is still playing: the end card waits.</summary>
        public bool EndHeld
        {
            get
            {
                if (!EndPlayout || Game == null || !Game.Over || Ending.Length == 0 || FailCamOn || failPrep != null || views == null) return false;
                if (endHeldFor != Game) { endHeldFor = Game; endHeldSince = Time.unscaledTime; endHeldSkipped = false; endFollowOff = false; }
                if (endHeldSkipped || Time.unscaledTime - endHeldSince > EndPlayoutMaxS) return false;
                return views.OfficialLeavingNow || views.GetawayLeavingNow;
            }
        }

        /// <summary>Per frame once ended: while the leaving plays, a click shows the card; the view follows the car until the player moves it.</summary>
        void EndPlayoutFrame()
        {
            if (!EndHeld) return;
            var ms = UnityEngine.InputSystem.Mouse.current;
            if (ms != null && ms.leftButton.wasPressedThisFrame) { endHeldSkipped = true; Debug.Log("Play: the end card shown before the leaving played out (a click)"); return; }
            Vector3? at = views.OfficialLeavingAt ?? (views.GetawayLeavingNow && views.Find("car/getaway") is Transform gt ? gt.position : (Vector3?)null);
            if (!endFollowOff && at is Vector3 a && !Following && !JuiceFollow(a)) endFollowOff = true;
        }

        /// <summary>The fail camera was skipped by a click: the run as it ended shows its card at once (its leaving plays on behind it).</summary>
        void EndPlayoutSkip(Suborned.Sim.Game g) { endHeldFor = g; endHeldSince = Time.unscaledTime; endHeldSkipped = true; }
    }
}
