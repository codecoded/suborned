// The rulebook's `bodies` (brief 098, law L9 Bodies; Sky 2026-11-07: "ensuring NPC's don't overlap each, they have real boundaries"; engine
// 0.46.0, Keel): every person on the street has a body, and no two bodies on their feet overlap; engine 0.47.0, give and take (Sky 2026-11-07:
// "Do all but the jumping back to spot should be better to dealt with"): nobody jumps; people make room, squeeze past (squeeze, squeezePace) and
// change plans (replan); engine 0.49.0, the walk (Sky 2026-11-08, "Gap between planter and fountain should be walkable"): a walker keeps its radius
// and `margin` from every solid (WalkClearCm, the site's walk raster). Read here, apart from Rules.cs (over the 500-line mark). Every key is required:
// a rulebook without them is pinned before 0.46.0, 0.47.0 or 0.49.0 (unknown = reject). The law's behaviour is GameBodies.cs and its partners.
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
        /// <summary>bodies.shove: the most a body's displacement changes in a tick when it must make room now (cm), never over its own pace (engine 0.47.0).</summary>
        public int BodyShoveCmT { get; private set; }
        /// <summary>bodies.search: how far round its own place a body looks for room (cm).</summary>
        public int BodySearchCm { get; private set; }
        /// <summary>bodies.squeeze: the half-width a body turned sideways takes toward one it squeezes past (cm); two squeezing past each other stand
        /// at least twice it apart, everyone else two radii (engine 0.47.0, give and take).</summary>
        public int BodySqueezeCm { get; private set; }
        /// <summary>bodies.squeezePace: how fast a body squeezes past (cm a tick).</summary>
        public int BodySqueezeCmT { get; private set; }
        /// <summary>bodies.replan: a person of the city held this long with nothing working changes plans (ticks).</summary>
        public int BodyReplanT { get; private set; }
        /// <summary>bodies.margin (engine 0.49.0, brief 098: what is walkable is what you see): how much more than its radius a walker keeps from every
        /// solid (cm).</summary>
        public int BodyMarginCm { get; private set; }
        /// <summary>Two radii: the closest two bodies on their feet ever stand, centre to centre.</summary>
        public int BodyMinCm => 2 * BodyRadiusCm;
        /// <summary>The walk clearance: a walker's centre stays this far from every solid (the site's walk raster is cut to it: grid.body).</summary>
        public int WalkClearCm => BodyRadiusCm + BodyMarginCm;

        void ReadBodies(JNode b)
        {
            var n = b.Opt("bodies") ?? throw new FormatException("bodies is missing: engine 0.46.0 runs law L9, Bodies (brief 098)");
            n.Only("on", "radius", "space", "queue", "crew", "back", "shove", "search", "squeeze", "squeezePace", "replan", "margin", "rule", "tag", "src", "proposed", "reason");
            if (n["rule"].Str().Length == 0) throw new FormatException("bodies.rule is empty");
            int on = RI(n["on"]); if (on != 0 && on != 1) throw new FormatException("bodies.on is 0 or 1 (the law's switch)");
            BodiesOn = on == 1; BodyRadiusCm = RI(n["radius"]); BodySpaceCm = RI(n["space"]); BodyQueueCm = RI(n["queue"]); BodyCrewCm = RI(n["crew"]);
            BodyBackCmT = RI(n["back"]) / Game.Hz; BodyShoveCmT = RI(n["shove"]) / Game.Hz; BodySearchCm = RI(n["search"]);
            BodySqueezeCm = RI(n["squeeze"]); BodySqueezeCmT = Math.Max(1, RI(n["squeezePace"]) / Game.Hz); BodyReplanT = RI(n["replan"]) * Game.Hz; // engine 0.47.0 (give and take): every key required, unknown = reject
            BodyMarginCm = RI(n["margin"]); // engine 0.49.0: required
            if (BodyMarginCm < 0 || BodyMarginCm > 20) throw new FormatException("bodies.margin is 0 to 20 cm: a walker keeps its body and a little more from a solid, never a layout standard's width");
            if (BodySqueezeCm < 10 || BodySqueezeCm >= BodyRadiusCm) throw new FormatException("bodies.squeeze is 10 cm to under a radius: a body turned sideways is narrower, never nothing");
            if (BodySqueezeCmT < 1 || BodySqueezeCmT * Game.Hz > BodyShoveCmT * Game.Hz) throw new FormatException("bodies.squeezePace is at least 10 cm/s and no more than bodies.shove");
            if (BodyReplanT < Game.Hz) throw new FormatException("bodies.replan is at least 1 s");
            if (BodyRadiusCm < 10 || BodyRadiusCm > 60) throw new FormatException("bodies.radius is 10 to 60 cm (a person)");
            if (BodySpaceCm < BodyMinCm || BodyQueueCm < BodyMinCm) throw new FormatException("bodies.space and bodies.queue are at least two radii: never inside two bodies");
            if (BodyCrewCm < BodySpaceCm) throw new FormatException("bodies.crew is at least bodies.space");
            if (BodyBackCmT < 1 || BodyShoveCmT < BodyBackCmT) throw new FormatException("bodies.back is at least 10 cm/s, and bodies.shove at least bodies.back");
            if (BodySearchCm < BodyMinCm) throw new FormatException("bodies.search is at least two radii");
            if (BodiesOn && BodyClearCm < BodyMinCm) throw new FormatException($"fight.spacing.clear ({BodyClearCm} cm) is under two body radii ({BodyMinCm} cm): the fight's spacing and law L9 must agree");
        }
    }
}
