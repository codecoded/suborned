// The rulebook's rivals rules of brief 096's second round (Motive's rivals page, briefs/096/motive/rivals.html, rivals.json; Sky 2026-11-04
// "all recommended"; engine 0.41.0, Keel): the unclaimed shooting (`security.unclaimed`, Q1 a) and a blast's Standing near city people
// (`car.target.blastStanding`). The crew's own (the block on arrival, the lookout, the temperaments) are CrewRules'. Every key is required: a
// rulebook without them is pinned before 0.41.0 (unknown = reject).
using System;

namespace Suborned.Sim
{
    public sealed partial class Rules
    {
        /// <summary>security.unclaimed.hold: the response holds the scene this long after it arrives; an operative in its sight then claims the shooting (ticks are the caller's).</summary>
        public int UnclaimedHoldS { get; private set; }
        /// <summary>security.unclaimed.scene (engine 0.42.0, Motive V2 b): in the hold, an operative this near the scene in the response's sight places the cell (cm).</summary>
        public int UnclaimedSceneCm { get; private set; }
        /// <summary>security.unclaimed.fired "placed" (engine 0.43.0, Motive V3): while a gang shooting is pending and unplaced, having fired makes nobody a foe.</summary>
        public bool FiredPlaced { get; private set; }
        /// <summary>car.target.blastStanding: the Standing a cell-lit blast moves with a city person within BlastNearCm, once a car.</summary>
        public int BlastStanding { get; private set; } public int BlastNearCm { get; private set; }

        void ReadRivals096(JNode b)
        {
            var u = b["security"].Opt("unclaimed") ?? throw new FormatException("security.unclaimed is missing: engine 0.41.0 files an unclaimed shooting as a gang shooting (brief 096, Motive Q1 a)");
            u.Only("hold", "scene", "fired", "rule", "tag", "src", "proposed", "reason");
            var fr = u.Opt("fired")?.Str() ?? throw new FormatException("security.unclaimed.fired is missing: engine 0.43.0 reads who knows an agent fired while a gang shooting is unplaced (brief 096, Motive V3)");
            if (fr != "placed" && fr != "always") throw new FormatException($"security.unclaimed.fired: placed or always, not '{fr}'"); FiredPlaced = fr == "placed";
            if (u["rule"].Str().Length == 0) throw new FormatException("security.unclaimed.rule is empty");
            UnclaimedHoldS = RI(u["hold"]); if (UnclaimedHoldS < 1) throw new FormatException("security.unclaimed.hold is at least 1 s");
            UnclaimedSceneCm = RI(u.Opt("scene") ?? throw new FormatException("security.unclaimed.scene is missing: engine 0.42.0 places the cell only near the scene in the hold (brief 096, Motive V2)")) * 100;
            if (UnclaimedSceneCm < 100) throw new FormatException("security.unclaimed.scene is at least 1 m");
            var ct = b["car"]["target"];
            var bs = ct.Opt("blastStanding") ?? throw new FormatException("car.target.blastStanding is missing: engine 0.41.0 moves Standing for a blast near city people (brief 096)");
            bs.Only("v", "near", "rule", "tag", "src", "proposed", "reason");
            if (bs["rule"].Str().Length == 0) throw new FormatException("car.target.blastStanding.rule is empty");
            BlastStanding = RI(bs["v"]); BlastNearCm = RI(bs["near"]) * 100;
            if (BlastStanding > 0 || BlastNearCm < 100) throw new FormatException("car.target.blastStanding: v is a Menace step (0 or below), near at least 1 m");
        }
    }
}
