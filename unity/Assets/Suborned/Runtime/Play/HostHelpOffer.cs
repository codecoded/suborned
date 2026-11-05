using Suborned.Sim;

namespace Suborned.Play
{
    /// <summary>Engine 0.44.0 (brief 096, Sky 2026-11-05 option 1; Motive help-fearful.md; Keel report-help-fearful.md "What the host must show"): the
    /// turn card's extra rows from the engine's price lines. Before the Scout's Listen a Help on a told need is a small offer, priced alone ($0, 35 s,
    /// +1 T) and refused by everyone: the card shows it offered, not locked, its outcome slot the refusal mark with Fable's words
    /// (HudModel.OutcomeWords, less.alt.outcome.small-offer). A remembered refusal (memory `refusal`) adds its line to every later Help and Pay (+$10,000, +1 T) and Pressure (+1 T): the row names
    /// it so the bigger price reads. The ring and the hold never take the small offer as the best way (ContextRank.Refuses). Reads only.</summary>
    public sealed partial class HandoverHost
    {
        /// <summary>The knowledge the rulebook's turn.&lt;approach&gt; needs, read from its own needs: `known` (engine 0.44.0: Help needs the need told, the
        /// briefing's word or heard; its lock is still needs-need), else `requires` or `leverage` (the approach's people-rules gate), else nothing. The card's lock
        /// follows the engine; the done chip's unlock pills (HostDone.Gates) keep the people-rules gate, so the Listen still shows Help opening.</summary>
        string TurnNeeds(string approach)
        {
            var R = Game.R; string id = "turn." + approach; if (!R.HasEvent(id)) return R.ApproachRequires(approach);
            foreach (var c in R.Ev(id).Needs) { if (c.K == "known") return c.S("what"); if (c.K is "requires" or "leverage") return R.ApproachRequires(c.S("approach")); /* leverage: the engine reads the same gate (GameOrders) */ }
            return "nothing";
        }

        void OfferRows(Intent i, Quote q)
        {
            if (q == null || i.Order?.Do != "turn") return;
            var rb = q.Lines.Find(l => l.Line == "refused-before" && l.Holds == "yes");
            if (rb == null || rb.Usd <= 0 && rb.Trail <= 0) return;
            string word = Text?.LockByCode("refusal-remembered")?.Short is { Length: > 0 } s ? s : "Refused before";
            i.Rows.Add(("refused", $"{word}: {(rb.Usd > 0 ? $"+${rb.Usd:N0} · " : "")}+{rb.Trail} T"));
        }
    }
}
