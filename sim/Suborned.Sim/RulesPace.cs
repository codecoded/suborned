// Engine 0.55.0 (brief 110; Sky 2026-11-11, after playing 0.2.330: "I think we need to reduce the time things take to happen ... some of these things
// take a while. I ended up just shooting my way through the game."). The rulebook's `pace`: one dial, `act` (a percentage), on the time of every act
// the cell orders, a law and not a sum: each value `acts` names is read as round(v × act / 100) seconds, at least 1, at load, for every reader (the
// engine's acts, the cards' prices, the host's reads of R), so the card and the act always agree. 100 is the time each value says by itself.
// With no `pace` (a rulebook pinned before 0.55.0) nothing is scaled: it plays and hashes as 0.54.0.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Rules
    {
        /// <summary>The act-pace dial (rulebook pace.act), in percent; 100 with no pace section.</summary>
        public int ActPacePct { get; private set; } = 100;
        /// <summary>The refs the dial scales (rulebook pace.acts), in the rulebook's order; empty with no pace section.</summary>
        public IReadOnlyList<string> PaceActs => paceActs;
        readonly List<string> paceActs = new List<string>();

        /// <summary>At load, after `values` and before any ref is read: every ref in pace.acts takes the dial's share of its seconds.</summary>
        void ApplyPace(JNode b)
        {
            if (b.Opt("pace") is not JNode pn) return;
            pn.Only("act", "acts", "rule", "tag", "src", "proposed", "reason");
            if (pn["rule"].Str().Length == 0) throw new FormatException("pace.rule is empty");
            long pct = Num(pn["act"].Str()); if (pct < 10 || pct > 100) throw new FormatException($"pace.act = {pct}: the dial runs 10 to 100 (%)");
            ActPacePct = (int)pct;
            foreach (var a in pn["acts"].Items)
            {
                var r = a.Str();
                if (paceActs.Contains(r)) throw new FormatException($"pace.acts names {r} twice");
                if (!(r.StartsWith("v:", StringComparison.Ordinal) && r.EndsWith("-s", StringComparison.Ordinal)) && !(r.StartsWith("p:approaches.", StringComparison.Ordinal) && r.EndsWith(".timeS", StringComparison.Ordinal)))
                    throw new FormatException($"pace.acts: {r} is no act's seconds (a v:…-s value or p:approaches.<id>.timeS)");
                if (r.StartsWith("v:", StringComparison.Ordinal) && b["values"].Opt(r.Substring(2))?.Opt("unit")?.Str() is string u && u != "s") throw new FormatException($"pace.acts: {r} is not in seconds");
                long v = Num(r), scaled = Math.Max(1L, (v * pct + 50) / 100);
                refs[r] = scaled; if (r.StartsWith("v:", StringComparison.Ordinal)) values[r.Substring(2)] = scaled;
                paceActs.Add(r);
            }
        }
    }
}
