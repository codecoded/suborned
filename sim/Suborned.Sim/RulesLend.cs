// The rulebook's lend (brief 096's third round: Motive's lend addendum, briefs/096/motive/lend.html, lend.json; Sky 2026-11-05 "build the lend
// now", decided 2026-10-26 in brief 071; engine 0.42.0, Keel): `car.lend`, the turned driver's car as a second cell car, two Go's and the police
// clock after the target is out. `split` is the switch (getaway.split, 071 §9): at 0 every route is 0.41.0's one-car getaway. Every key is
// required: a rulebook without them is pinned before 0.42.0 (unknown = reject).
using System;

namespace Suborned.Sim
{
    public sealed partial class Rules
    {
        /// <summary>car.lend.split: the lend and the two cars run (071's getaway.split).</summary>
        public bool LendSplit { get; private set; }
        /// <summary>car.lend.seats: standing operatives the lent car takes.</summary>
        public int LendSeats { get; private set; }
        /// <summary>car.lend.police: seconds from vale.out until the police close in (the mission ends).</summary>
        public int SplitPoliceS { get; private set; }

        void ReadLend(JNode b)
        {
            var l = b["car"].Opt("lend") ?? throw new FormatException("car.lend is missing: engine 0.42.0 runs the lend (brief 096, Motive's lend addendum)");
            l.Only("split", "seats", "police", "refuse", "rule", "tag", "src", "proposed", "reason");
            if (l["rule"].Str().Length == 0) throw new FormatException("car.lend.rule is empty");
            int sp = RI(l["split"]); if (sp != 0 && sp != 1) throw new FormatException("car.lend.split is 0 or 1 (the switch)");
            LendSplit = sp == 1; LendSeats = RI(l["seats"]); SplitPoliceS = RI(l["police"]);
            if (LendSeats != 1) throw new FormatException("car.lend.seats: the engine seats one standing operative in the lent car (Motive L3)");
            if (SplitPoliceS < 1) throw new FormatException("car.lend.police is at least 1 s");
            var rf = l["refuse"]; rf.Only("board", "drive", "gone");
            if (rf["board"].Str() != "board-lent-full" || rf["drive"].Str() != "drive-lent" || rf["gone"].Str() != "car-left") throw new FormatException("car.lend.refuse: board board-lent-full, drive drive-lent, gone car-left (the engine's codes)");
        }
    }
}
