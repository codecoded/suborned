// The rival crew's poses and reads (brief 059 K8; moved out of GameCrew.cs, which was over the 500-line mark, by brief 098's L9): where a member
// stands and faces, his beat, his cone, what a Watch shows of him. Brief 098 L9 (GameBodies.cs): a member on foot is a body; MemberPose and
// MemberPos read where the law laid him, MemberPoseRaw where his own rules put him (a member in the final fight is his fight body's).
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        // ---------------------------------------------------------------- poses and reads

        /// <summary>Where a member stands now (his fight body's place once he fights), or the car's while aboard; on foot, where L9 laid him.</summary>
        public Pt MemberPos(CrewMember m, int tick) => CrewLaid(m) ? Laid(MemberPosRaw(m, tick), crewLay[m.N - 1], tick, CrewPlan(m)) : MemberPosRaw(m, tick);
        /// <summary>Brief 098 L9: a member the law lays himself (on foot, up, not his fight body).</summary>
        bool CrewLaid(CrewMember m) => !m.Fight && !m.Gun.Down && m.N >= 1 && m.N <= crewLay.Length && (m.Mode == CrewMode.Walking || m.Mode == CrewMode.Posted || m.Mode == CrewMode.Talking || m.Mode == CrewMode.Boarding);
        /// <summary>Where his own rules put a member (L9 lays him from here).</summary>
        Pt MemberPosRaw(CrewMember m, int tick)
        {
            if (m.Fight && m.H != null) return m.H.Pos(tick);
            if (m.Gun.Down) return BodyOf(m.P) is Body b ? BodyPos(b) : m.DownAt; // engine 0.33.0: a body taken down, carried or stashed
            switch (m.Mode)
            {
                case CrewMode.Walking: return m.Disp != null ? DispPose(m.Disp, rivSpeed, tick).pos : WalkPos(m, tick);
                case CrewMode.Posted:
                    if (m.Disp != null) return DispPose(m.Disp, rivSpeed, tick).pos;
                    if (m.Post == "lookout") { var (b, into) = CrewRules.BeatAt(Cr.LookoutLoop, m.Id, tick - m.PostTick); int pace = Cr.LookoutPaceCm; int off = b.Move > 0 ? pace * into / (b.S * Hz) : b.Move < 0 ? pace - pace * into / (b.S * Hz) : b.AtPace ? pace : 0; return m.Place + new Pt(0, off); }
                    return m.Place;
                case CrewMode.Talking: case CrewMode.Boarding: return m.Place;
                default: return RivalCarPose()?.pos ?? S.Stand(S.RivalsPark);
            }
        }
        /// <summary>A member on the street now (on foot, up, not gone): his place and facing; null while aboard, down or gone (off the street). Laid by L9.</summary>
        public (Pt pos, Pt facing)? MemberPose(CrewMember m) { var p = MemberPoseRaw(m); return p.HasValue && CrewLaid(m) ? (Laid(p.Value.pos, crewLay[m.N - 1], Tick, CrewPlan(m)), p.Value.facing) : p; }
        (Pt pos, Pt facing)? MemberPoseRaw(CrewMember m)
        {
            if (m.Gun.Down || m.Mode == CrewMode.Gone) return null;
            if (m.Fight && m.H != null)
            {
                var h = m.H; if (!h.Present) return null; var p = h.Pos(Tick);
                var tp = h.Gun.Target.Length > 0 ? PosOf(h.Gun.Target) : null;
                if (tp.HasValue && tp.Value != p) return (p, tp.Value - p);
                if (h.Run != null && h.Moving(Tick)) return (p, h.Run.Heading((long)Math.Max(0, Tick - h.RunStart) * h.Speed));
                return (p, FaceFrom(p, Car));
            }
            switch (m.Mode)
            {
                case CrewMode.Walking:
                    if (m.Disp != null) return DispPose(m.Disp, rivSpeed, Tick);
                    { long mm = (long)Math.Max(0, Tick - m.WalkStart) * m.Speed; return (m.Walk!.At(mm), mm >= m.Walk.Length ? Back(m.Walk) : m.Walk.Heading(mm)); }
                case CrewMode.Posted:
                    if (m.Disp != null) return DispPose(m.Disp, rivSpeed, Tick);
                    return (MemberPosRaw(m, Tick), Yaw(m.Base, BeatOf(m).beat.Yaw));
                case CrewMode.Talking: case CrewMode.Boarding: return (m.Place, m.Base);
                default: return null;
            }
        }
        /// <summary>Brief 098 L9 (groups spread): a member drawn to the Office front faces his own sector, all from one line: the middle man out
        /// from the front to the street (toward the crew's car), the next out to his side by 45°, the flanks by 90° (his side: FrontPlace's k).
        /// With the law off, the way he came.</summary>
        Pt FrontFace(CrewMember m, Pt back)
        {
            var o = S.At(S.RivalCarFront) - S.Stand(S.RivalsFront); if (!R.BodiesOn || o == default) return back;
            int[] ks = { 0, 1, -1, 2, -2 }; int k = ks[Math.Min(m.N - 1, ks.Length - 1)];
            return k == 0 ? o : Rot(o, (Math.Abs(k) == 1 ? 45 : 90) * -Math.Sign(k)); // FrontPlace sets k > 0 at the street line turned -90°: he turns that way
        }
        (CrewBeat beat, int into) BeatOf(CrewMember m)
        {
            var loop = m.Post == "lookout" ? Cr.LookoutLoop : Cr.PostedLoop;
            var id = m.Post == "lookout" ? m.Id : m.N <= Cr.Posted ? m.Id : "rival-1"; // the wheelman on foot plays the posted loop on rival-1's phase
            return CrewRules.BeatAt(loop, loop.PhaseS.ContainsKey(id) ? id : "rival-1", Tick - Math.Max(0, m.PostTick));
        }
        /// <summary>The base facing turned by a beat's yaw (rulebook rivals.beats.rule): cos/sin per mille 30 → (866, 500), 60 → (500, 866).</summary>
        static Pt Yaw(Pt f, int yaw)
        {
            if (yaw == 0) return f;
            int c = Math.Abs(yaw) == 30 ? 866 : 500, s = (Math.Abs(yaw) == 30 ? 500 : 866) * Math.Sign(yaw);
            var r = new Pt((int)(((long)f.X * c - (long)f.Y * s) / 1000), (int)(((long)f.X * s + (long)f.Y * c) / 1000));
            return r == new Pt(0, 0) ? f : r;
        }
        /// <summary>Host read (never hashed): the beat a member plays now ("" while aboard or in the fight), for his pose.</summary>
        public string MemberBeat(string id)
        {
            var m = CrewOf(id); if (m == null || m.Fight || m.Gun.Down) return "";
            if (m.Mode == CrewMode.Aboard) return m == Wheelman ? CrewRules.BeatAt(Cr.WheelmanLoop, m.Id, Tick - CarStTick).beat.Pose : "";
            if (m.Mode == CrewMode.Talking) return "talk";
            if (m.Mode == CrewMode.Boarding) return "board";
            if (m.Mode == CrewMode.Walking) return "walk";
            if (m.Mode != CrewMode.Posted) return "";
            var b = BeatOf(m).beat.Pose;
            if (b == "lean") { bool cover = false; foreach (var c in LiveCover) if (G.Within(c.At, m.Place, R.CoverSnapCm)) { cover = true; break; } if (!cover) b = "watch"; }
            return b;
        }

        /// <summary>A member's cone sees p (perception.rivals; its own sight ignores people): on foot, on the street, drawn or watching it from
        /// their arrival (ai.intent.rivals.street).</summary>
        public bool MemberWatching(CrewMember m, Pt p)
        {
            if (!RivalsDrawn && !R.Intent.RivalsStreet) return false;
            var pose = MemberPose(m); if (!pose.HasValue) return false;
            return G.InCone(pose.Value.pos, pose.Value.facing, R.RivReachCm, R.RivHalfCm, near, p) && Sees(pose.Value.pos, p);
        }
        /// <summary>Any member of the crew on foot sees p (RivalsStreetWatching's crew-wide answer).</summary>
        bool CrewSees(Pt p) { foreach (var m in Crew) if (!m.Fight && MemberWatching(m, p)) return true; return false; }
        /// <summary>The crew on the map and watching the street (ai.intent.rivals.street, or drawn), any member on foot and up.</summary>
        bool CrewStreetOn { get { if (!RivalsDrawn && !R.Intent.RivalsStreet) return false; foreach (var m in Crew) if (!m.Fight && Up(m) && m.OnFoot) return true; return false; } }

        /// <summary>Pulse §8 (host read, never hashed): what a Watch on the rivals shows of a member or the car: the next move's kind, its tick (−1
        /// for none) and where. The car: stops (its block point's arrival), back (after a break). The posted three: spring (the ring round the
        /// cell's car). The lookout (engine 0.41.0): follows (the target's car, from when it is due to leave), back (a misled man's return to his post).</summary>
        public (string kind, int tick, Pt at)? RivalNext(string id)
        {
            if (id == "car:rivals" || id == "rivals")
            {
                switch (CarSt)
                {
                    case CrewCar.Away: return ("back", RivalsDelayedUntil, BlockAt(StandForCrew()).at);
                    case CrewCar.Entering: case CrewCar.Returning: if (CarMoving) return ("stops", ccStart + ccProf!.Length - 1, ccPath!.End); return null;
                    default: return null;
                }
            }
            var m = CrewOf(id); if (m == null || !Up(m)) return null;
            if (m.Mode == CrewMode.Posted && m.Post == "spot") return ("spring", -1, Car);
            if (m.Mode == CrewMode.Posted && m.Post == "misled") return ("back", MisledBackTick(m), m == Lookout ? S.Stand(S.RivalLookout) : S.Stand(CrewStand.Ambush[Math.Max(0, m.Spot)]));
            if (m == Lookout && m.Mode == CrewMode.Posted && m.Post == "lookout" && (Vale == ValeAt.Office || Vale == ValeAt.Leaving || Vale == ValeAt.Pickup)) return ("follows", T(valeAtCarS) + T(R.ValeInS), CarPos());
            return null;
        }

    }
}
