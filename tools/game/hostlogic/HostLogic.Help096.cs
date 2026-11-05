// Brief 096, engine 0.44.0 (Sky 2026-11-05 option 1; Motive help-fearful.md; Keel report-help-fearful.md "What the host must show"): Help on Inez before the
// Scout's Listen is a small offer she refuses: the Fixer's card offers it (not locked) at $0 · 35 s · +1 T with the refusal mark and Fable's words, and the
// ring's quick click and the hold never take it as the best way. After the Listen a Fearful Inez takes the full offer once ($40,000, turns); after a refused
// small offer the card names the remembered refusal's line (+$10,000, +1 T). Same harness as HostLogic.cs: the real engine and text.json, no views.
using System.IO;
using System.Linq;
using NUnit.Framework;
using Suborned.Play;
using Suborned.Sim;
using Order = Suborned.Sim.Order;

public class Help096
{
    static HandoverHost.PanelRow HelpRow(HandoverHost h) { h.Select(OpId.Fixer, false); return Host.Plan(h, "waitress").Pn.Rows.FirstOrDefault(r => r.I?.Order?.Do == "turn" && r.I.Order.Approach == "help"); }
    static string Words(HandoverHost h, HandoverHost.PanelRow r) => (string)Host.Call(new HudModel(), "OutcomeWords", r.I, "waitress", h.Text);
    static string Mark(HandoverHost.PanelRow r) => (string)Host.Call(new HudModel(), "MarkOf", r.I.Outcome);

    /// <summary>Route A's own orders before its Help (the Scout's Watch and Listen), each given as the player would; `smallOffer`: the Fixer's Help at once,
    /// before the Listen, in place of his walk to the courtyard. Runs on until the Listen has been heard and the Fixer is free.</summary>
    static HandoverHost Listened(bool smallOffer)
    {
        var h = Host.Fresh(); Host.Set(h, "HintsOn", false); var g = h.Game; var inez = g.PersonByRole("waitress");
        var orders = Script.Parse(File.ReadAllText(System.IO.Path.Combine(Root.Path, "contracts/missions/handover/routes/route-a.json"))).Orders.Select((o, i) => (o, i)).OrderBy(p => p.o.Tick).ThenBy(p => p.i).Select(p => p.o).Where(o => o.Op != "fixer" || !smallOffer && o.Do == "move").TakeWhile(o => o.Tick < 596).ToList();
        if (smallOffer) h.GiveScripted(new Order { Op = "fixer", Do = "turn", Target = "waitress", Approach = "help" });
        foreach (var o in orders) { if (g.Over) break; if (o.Tick > g.Tick) h.RunTo(o.Tick); if (o.Do is "watch" or "listen") h.GiveScripted(o); else h.Issue(o); Host.Call(h, "Track"); }
        Assert.That(Host.Until(h, () => inez.KNeed && g.Ops[(int)OpId.Fixer].Busy == null, 2000), $"the Listen never landed by {g.Tick}");
        Assert.That(h.Fault, Is.Null, h.Status); return h;
    }

    [Test] public void BeforeTheListenHelpIsASmallOfferOfferedNotLockedAndNeverTheBestWay()
    {
        var h = Host.Fresh(); Host.Set(h, "HintsOn", false); Assert.That(h.Game.PersonByRole("waitress").KNeed, Is.False, "nothing heard yet");
        var r = HelpRow(h); Assert.That(r, Is.Not.Null, "no Help row on Inez before the Listen");
        Assert.That(r.Available, $"offered, not locked: {r.Reason}"); Assert.That(r.I.Outcome, Is.EqualTo("small-offer"));
        Assert.That((r.I.Usd, r.I.Time, r.I.Trail, r.I.Orders), Is.EqualTo(("$0", "35 s", "T +1", 1L)), "the small offer, priced alone");
        var line = HudModel.PriceLine(h, r.I); Assert.That(line.Find(f => f.Kind == "usd")?.Text, Is.EqualTo("0"), "the card shows its $0"); Assert.That(line.Find(f => f.Kind == "trail")?.Text, Is.EqualTo("+1"));
        Assert.That(Words(h, r), Is.EqualTo(h.Text.AltOutcome["small-offer"])); Assert.That(Mark(r), Is.EqualTo("no"), "the refusal mark");
        var plan = Host.Plan(h, "waitress"); var pick = plan.Choice.Pick >= 0 ? plan.Row(plan.Choice.Pick) : null;
        Assert.That(plan.Choice.Act == ContextRank.Act.Default && pick?.I?.Order?.Approach == "help", Is.False, $"the quick click takes the small offer ({plan.Choice.Act}, {plan.Choice.Why})");
        int main = (int)Host.Call(h, "MainWay", plan); Assert.That(main >= 0 && plan.Row(main)?.I?.Order?.Approach == "help", Is.False, "the hold takes the small offer");
        h.Select(OpId.Fixer, false); var guard = Host.Plan(h, "guard").Pn.Rows.FirstOrDefault(x => x.I?.Order?.Approach == "help");
        Assert.That(guard == null || !guard.Available, "the guard has told nothing: his Help stays locked");
    }

    [Test] public void AfterTheListenAFearfulInezTakesTheFullOfferOnce()
    {
        var h = Listened(false); var inez = h.Game.PersonByRole("waitress"); Assume.That(inez.Temperament, Is.EqualTo("fearful"), "this seed's Inez");
        var r = HelpRow(h); Assert.That(r.Available, r.Reason);
        Assert.That((r.I.Outcome, r.I.Orders, r.I.Usd, r.I.Trail), Is.EqualTo(("turned", 1L, "$40,000", "T +1")), "the full offer, one visit");
        Assert.That(Words(h, r), Is.EqualTo(h.Text.AltOutcome["ok"])); Assert.That(Mark(r), Is.EqualTo("ok")); Assert.That(r.I.Rows.Any(x => x.text.Contains("+$10,000")), Is.False, "no refusal to remember");
        var plan = Host.Plan(h, "waitress"); Assert.That(plan.Choice.Act == ContextRank.Act.Default && plan.Row(plan.Choice.Pick)?.I?.Order?.Approach == "help", $"now the best way: {plan.Choice.Act} {plan.Choice.Why}");
    }

    [Test] public void AfterARefusedSmallOfferTheFullOfferCarriesTheRefusalLine()
    {
        var h = Listened(true); var g = h.Game; var inez = g.PersonByRole("waitress"); Assume.That(inez.Temperament, Is.EqualTo("fearful"), "this seed's Inez");
        int li = g.Log.FindIndex(l => l.Code == "turn.help.small-offer"); Assert.That(li, Is.GreaterThanOrEqualTo(0), "the small offer was never refused"); var log = g.Log[li];
        var land = HandoverHost.LandingOf(g, log); Assert.That((land.Agent, land.Outcome), Is.EqualTo(((int)OpId.Fixer, "small-offer")), "the plate and the chip read the landing");
        Assert.That(inez.Turned, Is.False);
        var r = HelpRow(h); Assert.That(r.Available, r.Reason);
        Assert.That((r.I.Outcome, r.I.Orders, r.I.Usd, r.I.Trail), Is.EqualTo(("turned", 1L, "$50,000", "T +2")), "the full offer, the refusal's line on top");
        Assert.That(r.I.Rows.Any(x => x.text.EndsWith("+$10,000 · +1 T")), string.Join(" | ", r.I.Rows.Select(x => x.text)));
        Host.NoTokens("help rows", r.I.Rows.Select(x => x.text).ToArray());
    }
}
