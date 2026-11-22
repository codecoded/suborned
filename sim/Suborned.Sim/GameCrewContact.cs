namespace Suborned.Sim
{
    public sealed partial class Game
    {
        static bool LeavingStreet(string goal) => goal == "exit" || goal == "flee" || goal == "leave" || goal == "aboard";

        // Nearby crew hear the shot and can turn to help if they can place both
        // their crewmate and the shooter. An unrelated shot across the map tells them nothing.
        bool CrewUnderFire(CrewMember observer, Op shooter)
        {
            if (observer.Fight || !observer.OnFoot || !Up(observer) || CrewBrokenPre) return false;
            var name = Sim.Op.Name(shooter.Id);
            foreach (var ally in Crew)
            {
                if (ally == observer || ally.Fight || !ally.OnFoot || !ShotByOpRecently(ally.Gun, name)) continue;
                var here = MemberPos(observer, Tick);
                var there = MemberPos(ally, Tick);
                if (G.Within(here, there, R.RivReachCm) && Sees(here, there) && Sees(here, shooter.Pos)) return true;
            }
            return false;
        }

        // A street assignment stays on the member while contact owns his feet. Cover
        // is only worth walking to if he can still engage from it.
        void EngageStreet(CrewMember m, Pt pos, Pt target)
        {
            if (m.Mode != CrewMode.Walking && m.Mode != CrewMode.Posted) return;
            if (m.Disp?.Contact == true)
            {
                m.Disp.Face = Facing(pos, target);
                return;
            }
            if (m.Disp != null && !m.Disp.Press) return; // already moving off a shooter's line
            var g = m.Gun;
            Path? path = null;
            if (!PinHolds(g) && !Protected(pos, target))
            {
                var cover = FightFromCover(pos, target, g.W!.EffCm);
                if (cover != null)
                {
                    path = KitePath(m.Id, pos, cover.At, int.MaxValue / 20);
                    if (path != null && G.Within(path.End, cover.At, R.CoverSnapCm))
                        TakeCover(g, cover);
                    else path = null;
                }
            }
            m.Disp = new KiteDisp { Contact = true, P = path ?? new Path(new[] { pos, pos }),
                Start = Tick, Face = Facing(pos, target) };
        }

        void ClearDisplacement(CrewMember m)
        {
            if (m.Disp?.Contact == true) ReleaseCover(m.Gun);
            m.Disp = null;
        }

        bool StreetContactOwnsWalk(CrewMember m)
        {
            if (m.Disp?.Contact != true) return false;
            if (m.Gun.Target.Length > 0 || m.Gun.NoFoeSince < 0 || Tick - m.Gun.NoFoeSince < T(R.EngageLapseS)) return true;
            var here = MemberPos(m, Tick);
            ClearDisplacement(m);
            if (m.Mode == CrewMode.Walking && m.Walk != null)
                StartWalk(m, here, m.Walk.End, m.Goal, m.Speed);
            else if (m.Mode == CrewMode.Posted && here != m.Place)
                StartWalk(m, here, m.Place, "resume-post", rivSpeed);
            return false;
        }
    }
}
