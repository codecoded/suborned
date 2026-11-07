using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 098 L9, the give and take as the host draws it (Keel, engine 0.47.0; briefs/098/keel/give-and-take.html, "What Latch must draw").
    /// Places need nothing new: the engine moves every body at its own pace, so the views draw CrowdPos as it is. Read once a tick from
    /// Game.BodyMove (never hashed), for the city's people (the only ones it moves this way):
    ///  · squeezing past (SqueezeWith, SqueezeFacing): the figure turns side-on toward the way the engine faces it (across the gap, toward the other)
    ///    as it walks its way at the engine's 0.5 m/s on the walk clip's slowest stride. The turn is the figure's own yaw, SqueezeTurnDeg off its
    ///    walk: every figure imports with Optimize Game Objects (brief 069 A), so the spine is not a transform a pass after the clip could turn;
    ///    a true side-step wants a strafe pair in the clip set (Forma);
    ///  · making room (RoomFor, RoomAt): an ordinary walk to the side and back; standing aside, the figure faces the one passing;
    ///  · a change of plans (AltChair): seated in the chair it took instead of its own (HostSeats.SeatNow).
    /// View only: the engine is not told.</summary>
    public sealed partial class HostViews
    {
        /// <summary>The most a squeezing figure turns from its walk toward its side-on facing (deg): enough for the shoulders to read across the
        /// gap at play zoom, little enough that the walk clip's feet don't skate.</summary>
        const float SqueezeTurnDeg = 50f;
        readonly Dictionary<string, Vector3> squeezeFace = new();   // crowd view key → the side-on facing (world), this tick
        readonly Dictionary<string, string> altChair = new();       // crowd view key → the chair it took instead of its own, this tick
        /// <summary>For the proofs and the shots: the give and take drawn so far (squeeze, room and change-of-plans spells seen, by view key).</summary>
        public readonly HashSet<string> SqueezeSeen = new(), RoomSeen = new(), ReplanSeen = new();

        /// <summary>Per tick, after the crowd's places: what each person of the city is doing in the give and take.</summary>
        void GiveTick(Game g)
        {
            squeezeFace.Clear(); altChair.Clear();
            if (!g.R.BodiesOn) return;
            for (int i = 0; i < g.Walkers.Count && i < crowdKeys.Length; i++)
            {
                if (!atNow[i].HasValue || g.BodyMove(g.Walkers[i].P.Id) is not BodyMoveRead m) continue;
                var key = crowdKeys[i];
                if (m.SqueezeWith != null && (m.SqueezeFacing.X != 0 || m.SqueezeFacing.Y != 0)) { squeezeFace[key] = frame.Dir(m.SqueezeFacing); SqueezeSeen.Add(key); }
                if (m.RoomFor != null)
                {   // standing aside: face the one passing (walking there and back, the figure faces its walk as anyone does)
                    RoomSeen.Add(key);
                    if (g.BodyInfo(m.RoomFor) is { } by && things.TryGetValue(key, out var th))
                    { var d = frame.World(by.at) - th.to; d.y = 0f; if (d.sqrMagnitude > 0.04f) faces[key] = d.normalized; }
                }
                if (m.Replanned) { ReplanSeen.Add(key); if (m.AltChair != null) altChair[key] = m.AltChair; }
            }
        }

        /// <summary>The turn (deg) a figure walking along `d` takes toward its side-on facing while it squeezes past someone; 0 otherwise.</summary>
        float SqueezeTurn(string key, Vector3 d)
        {
            if (!squeezeFace.TryGetValue(key, out var sf)) return 0f;
            return Mathf.Clamp(Vector3.SignedAngle(d, sf, Vector3.up), -SqueezeTurnDeg, SqueezeTurnDeg);
        }

        /// <summary>The chair a crowd view took instead of its own this tick (BodyMove.AltChair), else null.</summary>
        string AltChairOf(string key) => altChair.TryGetValue(key, out var c) ? c : null;
    }
}
