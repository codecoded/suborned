// Engine 0.44.0 (brief 096, Sky 2026-11-05 on paying Inez, option 1: "I think 1 is fine for now"; Motive briefs/096/motive/help-fearful.md):
// a price line that stands alone (rulebook price line `alone`). turn.help's small-offer line holds while the need is told but not heard
// (the briefing's word, no Listen): the card is that line only ($0, the base time and Trail), so no cash is asked or taken, no orders are
// counted and no other line holds. Its outcome (small-offer) is the rulebook's: a refusal everyone gives, remembered as any refusal.
// Read-only on the card: nothing here changes state.
namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>The first line with `alone` whose conditions hold makes the card: its money, time and Trail, one order, and only it holds.</summary>
        void QuoteAlone(Quote q, Event ev, PCtx x)
        {
            for (int i = 0; i < ev.Price.Count; i++)
            {
                var l = ev.Price[i];
                if (!l.Alone || !AllHold(l.When, x)) continue;
                q.Usd = l.UsdV; q.S = l.SV; q.Trail = l.TrailV; q.WorstUsd = l.UsdV; q.Orders = 1;
                for (int j = 0; j < q.Lines.Count; j++) q.Lines[j].Holds = j == i ? "yes" : "no";
                return;
            }
        }
    }
}
