// A member of the response sent by an alarm (moved out of GameResponse.cs, which was over the 500-line mark, by brief 098's L9), and L9's
// reads of it: where the law laid it, and where an armed group sent to one place stands (groups spread: bodies.crew apart, each its sector).
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    /// <summary>One member of the response (a robot or an officer) sent by an alarm: where it starts and goes, what it is doing.</summary>
    public sealed class Unit
    {
        public string Id = ""; public bool Robot, Patrol, Legacy; public Gun Gun = null!; public int Speed;
        public Pt From, Dest; public int Launch; public Path? Route; public int Act = -1; public bool Scene, Cordon;
        public string St = "wait";   // wait (not yet on the street), walk, check, search, hold, back, gone
        public int Until, Arrive; public bool Verified;
        public readonly List<string> ToOpen = new List<string>(); public int NextOpen = -1;
        public bool OnStreet(int tick) => St != "wait" && St != "gone" && (Legacy || tick >= Launch);
        /// <summary>Brief 098 L9 (GameBodies.cs): where the law laid it; Slot: its place in the group sent to its destination (0 the first: the
        /// place itself; then the crew spots round it), which sets its sector.</summary>
        public BodyLay Body; public int Slot;
    }

    public sealed partial class Game
    {
        /// <summary>Where a unit stands and faces (GameResponse.cs's UnitPoseRaw), laid by L9.</summary>
        public (Pt pos, Pt facing)? UnitPose(Unit u) { var p = UnitPoseRaw(u); return p.HasValue && !u.Legacy && !u.Patrol ? (Laid(p.Value.pos, u.Body, Tick, UnitPlan(u)), p.Value.facing) : p; }

        /// <summary>Brief 098 L9 (groups spread): where a unit's walk ends. The first bound for a place stands at it; each next one at the next crew
        /// spot round it (bodies.crew off, the way they came in giving the spots' facing), and faces its sector there; fight.spacing's ClearStop
        /// then keeps it clear of every claim. With the law off: ClearStop on the place, as before.</summary>
        Pt UnitStop(Unit u, Pt from)
        {
            var to = Walk.NearestFree(u.Dest) ?? u.Dest;
            if (!R.BodiesOn) return ClearStop(u.Id, to);
            int k = 0; foreach (var o in Units) if (o != u && !o.Legacy && !o.Patrol && o.Route != null && o.St != "gone" && o.St != "back" && G.Within(o.Dest, u.Dest, 100)) k++;
            u.Slot = k;
            return ClearStop(u.Id, CrewSpot(to, to - from, k));
        }
    }
}
