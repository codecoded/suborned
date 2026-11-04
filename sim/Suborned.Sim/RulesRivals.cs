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
        /// <summary>car.target.blastStanding: the Standing a cell-lit blast moves with a city person within BlastNearCm, once a car.</summary>
        public int BlastStanding { get; private set; } public int BlastNearCm { get; private set; }

        void ReadRivals096(JNode b)
        {
            var u = b["security"].Opt("unclaimed") ?? throw new FormatException("security.unclaimed is missing: engine 0.41.0 files an unclaimed shooting as a gang shooting (brief 096, Motive Q1 a)");
            u.Only("hold", "rule", "tag", "src", "proposed", "reason");
            if (u["rule"].Str().Length == 0) throw new FormatException("security.unclaimed.rule is empty");
            UnclaimedHoldS = RI(u["hold"]); if (UnclaimedHoldS < 1) throw new FormatException("security.unclaimed.hold is at least 1 s");
            var ct = b["car"]["target"];
            var bs = ct.Opt("blastStanding") ?? throw new FormatException("car.target.blastStanding is missing: engine 0.41.0 moves Standing for a blast near city people (brief 096)");
            bs.Only("v", "near", "rule", "tag", "src", "proposed", "reason");
            if (bs["rule"].Str().Length == 0) throw new FormatException("car.target.blastStanding.rule is empty");
            BlastStanding = RI(bs["v"]); BlastNearCm = RI(bs["near"]) * 100;
            if (BlastStanding > 0 || BlastNearCm < 100) throw new FormatException("car.target.blastStanding: v is a Menace step (0 or below), near at least 1 m");
        }
    }
}
