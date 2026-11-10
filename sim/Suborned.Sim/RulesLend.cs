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

        /// <summary>car.take.seats (engine 0.53.0, brief 105): seats by kind (a passing car's model, "rivals" the rival car); -1 for a kind not named.</summary>
        public int TakeSeat(string kind) => takeSeats.TryGetValue(kind, out var n) ? n : -1;
        readonly System.Collections.Generic.Dictionary<string, int> takeSeats = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>car.go.waits "board" (engine 0.43.0, Motive G1): a Go given while an operative climbs into the car waits for that Board.</summary>
        public bool GoWaitsBoard { get; private set; }

        void ReadLend(JNode b)
        {
            var go = b["car"].Opt("go") ?? throw new FormatException("car.go is missing: engine 0.43.0 reads whether a Go waits for a Board under way (brief 096, Motive G1)");
            go.Only("waits", "rule", "tag", "src", "proposed", "reason"); if (go["rule"].Str().Length == 0) throw new FormatException("car.go.rule is empty");
            var gw = go["waits"].Str(); if (gw != "board" && gw != "none") throw new FormatException($"car.go.waits: board or none, not '{gw}'"); GoWaitsBoard = gw == "board";
            var tk = b["car"].Opt("take") ?? throw new FormatException("car.take is missing: engine 0.53.0 runs Take car (brief 105, Motive round2 §1)");
            tk.Only("seats", "refuse", "rule", "tag", "src", "proposed", "reason"); if (tk["rule"].Str().Length == 0) throw new FormatException("car.take.rule is empty");
            foreach (var kv in tk["seats"].Entries) { int n = RI(kv.Value); if (n < 1) throw new FormatException($"car.take.seats.{kv.Key} is at least 1"); takeSeats[kv.Key] = n; }
            if (!takeSeats.ContainsKey("rivals")) throw new FormatException("car.take.seats names the rival car (rivals)");
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
