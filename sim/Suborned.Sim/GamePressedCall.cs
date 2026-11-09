// Engine 0.38.0 (brief 085; Sky approved, Motive briefs/085/motive/pressure-guard.md §1): a caller who remembers a Pressure warns her lover when
// he reaches her: he stays summon.pressed.stay and runs back at summon.pressed.speed. Nothing else about the visit moves. GuardAwayUntil, already
// hashed, carries the shorter window; the reads below are for the host and hash nothing. Engine 0.51.0 (brief 102, option 1): the visit reads the
// pair (GameCall.cs VisitPair), so it is the short one if either of them was pressured, whichever of them was called.
using System;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>This visit came from a pressed call: true from the call on. The host tells the arrival with it: on the log codes guard.at-cafe
        /// (he reaches her) and guard.leaves-cafe (he turns back), both at their usual place and both also fired by an unpressed visit.</summary>
        public bool GuardCallPressed { get; private set; }
        /// <summary>He is on the running leg of a pressed visit, the run clip's read; the phase stays "returning to door". An alarm on this leg changes nothing (no recall, no re-timing); false if an alarm recalled him before he turned back (he goes at a guard's pace), or when he is held or down.</summary>
        public bool GuardRunningBack => GuardCallPressed && !guardRunning && !GuardHeld && !GuardDown && Tick >= LureTick + walkT + stayT && Tick < GuardAwayUntil;

        bool VisitIsPressed(Person leaves, Person visited) => R.SummonPressedAct != null && (leaves.Has(R.SummonPressedAct) || visited.Has(R.SummonPressedAct));
        int RunTicks(Path back) => (int)((back.Length + R.SummonPressedSpeed - 1) / R.SummonPressedSpeed);

        /// <summary>The visit's two walks from where the one who leaves stands now (the door guard): to the side of the one visited and back (rulebook
        /// summon; brief 022: to a server's post, stopping at her side). `walk` is SummonRefusal's.</summary>
        (Path outWalk, Path backWalk, int walkT, Pt face) VisitRoute(Person visited, Person leaves, int walk)
        {
            var sv = ServerOf(visited);
            var from = GuardPose()!.Value.pos; var to = sv != null ? sv.At : PersonPos(visited)!.Value;
            var outWalk = new Path(Walk.Route(Walk.NearestFree(from) ?? from, Walk.NearestFree(to) ?? to)!);
            int t = walk; Pt face = S.CafeFacing;
            if (sv != null) // brief 022: he walks to her post and stops rounds.close from it, at her side (PartnerStop), facing her
            {
                var side = PartnerStop(visited, leaves); // baked free and walkable from his post; a stop his grid cannot reach falls back as no stop does
                var toSide = side.HasValue ? Walk.Route(Walk.NearestFree(from) ?? from, side.Value) : null;
                if (toSide != null) outWalk = new Path(toSide);
                else outWalk = outWalk.Cut(Math.Max(0, outWalk.Length - R.PartnersCloseCm * 10L)); // the file holds no stop: the old stop, on his route
                t = (int)((outWalk.Length + peopleWalk - 1) / peopleWalk); // engine 0.34.0: the city's walk (crowd.walk)
                face = to - outWalk.End;
            }
            return (outWalk, new Path(Walk.Route(Walk.NearestFree(outWalk.End) ?? outWalk.End, Walk.NearestFree(from) ?? from)!), t, face);
        }

        /// <summary>How long a call on this role now would clear the door, in ticks, for the Pressure and Help cards (read only, never hashed): plain, an
        /// unpressed visit; pressed, a pressed one. Either end of the pair (option 1: the guard's own call too). Null while the visit wouldn't go (the
        /// card's read of the same refusals: no visit to play, the one called or the one who'd leave saw violence, he's busy, …), or with no pressed call.</summary>
        public (int plain, int pressed)? GuardCallWindows(string callerRole)
        {
            var p = PersonByRole(callerRole); if (R.SummonPressedAct == null || CallAbsent(p) != null || SummonRefusal(p, out int walk).code != null) return null;
            var (leaves, visited) = VisitPair(p)!.Value;
            var v = VisitRoute(visited, leaves, walk);
            return (2 * v.walkT + T(R.SummonStayS), v.walkT + T(R.SummonPressedStayS) + RunTicks(v.backWalk));
        }
    }
}
