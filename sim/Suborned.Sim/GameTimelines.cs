// The guard's and the patrol's timelines (moved out of Game.cs, brief 098 step 1: files stay small; behaviour unchanged): the summoned guard's
// visit to the café and his walk or run back, and the rerouted patrol's return to its sweep. Called from the tick's schedule (Game.StepInner).
using System;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        void GuardTimeline()
        {
            if (LureTick < 0 || GuardHeld || GuardDown) return;
            // Alert above summon.alertMax ends the visit: he heads straight back at a guard's pace (guardSpeed; Motive, motive-lovers.md §1.3)
            if (!guardRunning && Tick < GuardAwayUntil && Alert > R.SummonAlertMax && !GuardRunningBack) // brief 085 (ruling): a pressed run back keeps its pace
            {
                var from = GuardPose()!.Value.pos;
                guardRun = new Path(Walk.Route(Walk.NearestFree(from) ?? from, S.Stand(S.GuardPost)) ?? new[] { from, S.Stand(S.GuardPost) });
                guardRunning = true; LureTick = Tick; GuardAwayUntil = Tick + (int)((guardRun.Length + guardSpeed - 1) / guardSpeed);
                Note("guard.recalled", "The alarm is up: the guard heads back to his post.");
                return;
            }
            if (guardRunning) { if (Tick == GuardAwayUntil) { guardRunning = false; Note("guard.back", "The guard is back at the office entrance."); } return; }
            if (Tick == LureTick + walkT) { GuardReachedCafe = true; Note("guard.at-cafe", GuardCallPressed ? $"The guard reaches {InezName} at the café. She tells him someone knows about them." : $"The guard reaches {InezName} at the café. She keeps him talking, away from the door."); }
            if (Tick == LureTick + walkT + stayT) Note("guard.leaves-cafe", GuardCallPressed ? "The guard leaves the café and runs back toward his post." : "The guard leaves the café and walks back toward his post.");
            if (Tick == GuardAwayUntil) Note("guard.back", "The guard returns to the office entrance.");
        }
        /// <summary>engine 0.24.0 (brief 043 §6): the rerouted patrol's false orders run out and it is back on its entrance sweep (PatrolPose
        /// returns to the sweep on this tick); before, only PatrolAwayUntil passing said so. The window marker's closing code.</summary>
        void PatrolTimeline()
        {
            if (PatrolAwayUntil > 0 && Tick == PatrolAwayUntil && !PatrolDown) Note("patrol.returns", "The false orders run out. The patrol is back on its sweep."); // engine 0.34.0: no place named (brief 067 A)
        }
    }
}
