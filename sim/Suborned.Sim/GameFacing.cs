// The way a walker of the city faces (moved out of GameTakedown.cs, over the 500-line mark, by brief 098 L9's give and take, engine 0.47.0):
// FacingOf is the engine's read of the way a person faces now (their walk, a fright's source, a stop's facing, the law's own facings: a sector at a
// shared post, sideways while squeezing past someone, a chair taken instead); RoutineFacing the way their routine alone has them face.
using System;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>The way a person faces now, if the engine knows it: a walker on the move, the way they walk; else none.</summary>
        Pt? FacingOf(Person p)
        {
            var w = WalkerOf(p); if (w == null || !Start.Crowd || w.P.Down) return null;
            if (SqueezeFacing(w) is Pt sq) return sq; // brief 098 L9 (engine 0.47.0): squeezing past someone, turned sideways
            if (w.St.Length > 0)
            {
                if (w.Run != null && (w.St == "fleeing" || w.St == "returning" || w.St == "reporting" || w.St == "checking") && Tick >= w.RunStart)
                { long mm = (long)(Tick - w.RunStart) * (w.Pace > 0 ? w.Pace : w.St == "fleeing" ? R.CrowdRunSpeed : peopleWalk); if (mm < w.Run.Length) return w.Run.Heading(mm); }
                // brief 054 §3.3 (cone): the startled face what startled them
                if (stopFace != null && w.St == "startled" && lastShooterFor.TryGetValue(w.P.Id, out var src)) { var d = src - w.At; if (d != new Pt(0, 0)) return d; }
                return null;
            }
            return BodyFace(w) ?? RoutineFacing(w, Tick); // brief 098 L9: a guard at a post another holds faces his own sector
        }
        /// <summary>The way the routine has a person of the crowd facing at a tick (their leg, the stop's facing); a read for a card too.</summary>
        Pt? RoutineFacing(Walker w, int tick)
        {
            var c = People2!;
            for (int i = 0; i + 1 < w.Place.Length; i++)
                if (tick >= w.To[i] && tick < w.From[i + 1] && w.Place[i] != "beyond" && w.Place[i + 1] != "beyond")
                {
                    var path = CrowdPath(w.Place[i], w.Place[i + 1], w.To[i]); int span = w.From[i + 1] - w.To[i];
                    if (span <= 0) return null;
                    long lm = LegMm(w, path, tick - w.To[i], span, LeavesPost(w, i));
                    if (stopFace != null && lm >= path.Length && StopFace(w.Place[i + 1]) is Pt sf) return sf; // brief 054 §3.3: early at the leg's end, waiting
                    return path.Heading(lm);
                }
            // brief 054 §3.3 (cone): at a stop on the street, the stop's facing
            if (stopFace != null) for (int i = 0; i < w.Place.Length; i++) if (Tick >= w.From[i] && Tick < w.To[i] && w.Out[i]) return StopFace(w.Place[i]);
            return null;
        }
    }
}
