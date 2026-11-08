// The rulebook's fight.cover (engine 0.50.0, brief 098; Aster's ruling on the 0.49.0 report): fighters hold cover. Read here, apart from
// Rules.cs (over the 500-line mark), after ai and fight.spacing. Required: a rulebook without it is pinned before 0.50.0 (unknown = reject).
// Its `leave` and `lapse` are ai.cover.leave and ai.engageLapse, one value each, named here so the law reads whole; the law's behaviour is
// GameCoverHold.cs.
using System;

namespace Suborned.Sim
{
    public sealed partial class Rules
    {
        /// <summary>fight.cover.reach: a fighter whose target is in his reach steps only to cover within this share of his weapon's effective reach
        /// of that target (per mille), with the line to him.</summary>
        public int CoverHoldReachPerMille { get; private set; }

        void ReadCoverHold(JNode fg)
        {
            var n = fg.Opt("cover") ?? throw new FormatException("fight.cover is missing: engine 0.50.0 runs the law that fighters hold cover (brief 098)");
            n.Only("reach", "leave", "lapse", "rule", "tag", "src", "proposed", "reason");
            if (n["rule"].Str().Length == 0) throw new FormatException("fight.cover.rule is empty");
            CoverHoldReachPerMille = RI(n["reach"]);
            if (CoverHoldReachPerMille < 100 || CoverHoldReachPerMille > 1000) throw new FormatException("fight.cover.reach is 100 to 1000 per mille of the effective reach: cover a fighter can still fight from");
            if (RI(n["leave"]) != CoverLeaveS || RI(n["lapse"]) != EngageLapseS) throw new FormatException("fight.cover.leave and lapse are ai.cover.leave and ai.engageLapse (one value each)");
        }
    }
}
