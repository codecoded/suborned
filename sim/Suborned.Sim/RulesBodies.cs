// The rulebook's `bodies` (brief 098, law L9 Bodies; Sky 2026-11-07: "ensuring NPC's don't overlap each, they have real boundaries"; engine
// 0.46.0, Keel): every person on the street has a body, and no two bodies on their feet overlap. Read here, apart from Rules.cs (over the
// 500-line mark). Every key is required: a rulebook without them is pinned before 0.46.0 (unknown = reject). The law's behaviour is GameBodies.cs.
using System;

namespace Suborned.Sim
{
    public sealed partial class Rules
    {
        /// <summary>bodies.on: the law runs (the game plays it on; 0 only in a comparison build).</summary>
        public bool BodiesOn { get; private set; }
        /// <summary>bodies.radius: a body's radius (cm); two bodies on their feet are never closer than twice it.</summary>
        public int BodyRadiusCm { get; private set; }
        /// <summary>bodies.space: personal space (cm, centre to centre) a person of the city keeps from anyone standing, where there is room.</summary>
        public int BodySpaceCm { get; private set; }
        /// <summary>bodies.queue: a queue's step (cm) behind whoever holds a stop.</summary>
        public int BodyQueueCm { get; private set; }
        /// <summary>bodies.crew: how far apart armed people sharing a post stand (cm).</summary>
        public int BodyCrewCm { get; private set; }
        /// <summary>bodies.back: how fast a displaced body comes back to its own place (cm a tick).</summary>
        public int BodyBackCmT { get; private set; }
        /// <summary>bodies.shove: the most a body's displacement changes in a tick (cm).</summary>
        public int BodyShoveCmT { get; private set; }
        /// <summary>bodies.search: how far round its own place a body looks for room (cm).</summary>
        public int BodySearchCm { get; private set; }
        /// <summary>Two radii: the closest two bodies on their feet ever stand, centre to centre.</summary>
        public int BodyMinCm => 2 * BodyRadiusCm;

        void ReadBodies(JNode b)
        {
            var n = b.Opt("bodies") ?? throw new FormatException("bodies is missing: engine 0.46.0 runs law L9, Bodies (brief 098)");
            n.Only("on", "radius", "space", "queue", "crew", "back", "shove", "search", "rule", "tag", "src", "proposed", "reason");
            if (n["rule"].Str().Length == 0) throw new FormatException("bodies.rule is empty");
            int on = RI(n["on"]); if (on != 0 && on != 1) throw new FormatException("bodies.on is 0 or 1 (the law's switch)");
            BodiesOn = on == 1; BodyRadiusCm = RI(n["radius"]); BodySpaceCm = RI(n["space"]); BodyQueueCm = RI(n["queue"]); BodyCrewCm = RI(n["crew"]);
            BodyBackCmT = RI(n["back"]) / Game.Hz; BodyShoveCmT = RI(n["shove"]) / Game.Hz; BodySearchCm = RI(n["search"]);
            if (BodyRadiusCm < 10 || BodyRadiusCm > 60) throw new FormatException("bodies.radius is 10 to 60 cm (a person)");
            if (BodySpaceCm < BodyMinCm || BodyQueueCm < BodyMinCm) throw new FormatException("bodies.space and bodies.queue are at least two radii: never inside two bodies");
            if (BodyCrewCm < BodySpaceCm) throw new FormatException("bodies.crew is at least bodies.space");
            if (BodyBackCmT < 1 || BodyShoveCmT < BodyBackCmT) throw new FormatException("bodies.back is at least 10 cm/s, and bodies.shove at least bodies.back");
            if (BodySearchCm < BodyMinCm) throw new FormatException("bodies.search is at least two radii");
            if (BodiesOn && BodyClearCm < BodyMinCm) throw new FormatException($"fight.spacing.clear ({BodyClearCm} cm) is under two body radii ({BodyMinCm} cm): the fight's spacing and law L9 must agree");
        }
    }
}
