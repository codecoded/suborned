// Brief 108 (Sky on 0.2.330, the Journal: "should wrap"; the 1:44 "The alarm is answered" row ran past its card under the People sign and said "robots are coming" twice).
// Same harness as HostLogic.cs: the join of a cause and a card's result, and an open row's words fitted to its card by the layout every row uses.
using System.Linq;
using NUnit.Framework;
using Suborned.Play;

public class Journal108
{
    static float Tw(string s, float size, string font) => (s?.Length ?? 0) * size * .52f;

    [Test] public void TheJoinDropsASentenceTheCauseAlreadySays()
    {
        var h = Host.Fresh(); string J(string a, string b) => (string)Host.Call(h, "WhyJoin", a, b);
        Assert.That(J("A report reached the desk, so robots are coming to look.", "Robots are coming. On the street by 1:52."), Is.EqualTo("A report reached the desk, so robots are coming to look. On the street by 1:52."));
        Assert.That(J("A report reached the desk, so the police are coming.", "Robots and police are coming. On the street by 2:10."), Is.EqualTo("A report reached the desk, so the police are coming. Robots and police are coming. On the street by 2:10."), "a sentence that says more stays");
        Assert.That(J("Robots are coming.", "Robots are coming."), Is.EqualTo("Robots are coming."));
        Assert.That(J("", "B."), Is.EqualTo("B.")); Assert.That(J("A.", ""), Is.EqualTo("A."));
    }

    [Test] public void AnOpenRowsWordsWrapInsideItsCard()
    {
        var row = new BookRow { Id = 7, Time = "1:44", Title = "The alarm is answered", Where = "the mast camera saw the Fixer idling by the café door at the terrace's west end", Open = true, Crucial = true,
            Result = "A report reached the desk, so robots are coming to look. On the street by 1:52, and they search the square and the café front before anything else." };
        row.Lines.Add(("portraits/cameo_scout_normal", "Whoever goes, tell them to go careful, because the desk has its robots out on the square and the terrace both."));
        var p = new BookPage { Section = "journal", Name = "Journal" }; p.Rows.Add(row); p.Rows.Add(new BookRow { Id = 0, Time = "0:00", Title = "The job begins" });
        var r = BookLayout.Place(p, Tw); var it = r.Of(BookSlot.Rows).First(); var text = r.RowText[it.Index];
        foreach (var l in text.Lines.SelectMany(x => x)) Assert.That(Tw(l, 20f, "") , Is.LessThanOrEqualTo(BookLayout.RowW - BookLayout.RowLineX - 24f + .01f), $"'{l}' runs past the card");
        foreach (var l in text.Result) Assert.That(Tw(l, 20f, ""), Is.LessThanOrEqualTo(BookLayout.RowW - BookLayout.RowResultX - 24f + .01f), $"'{l}' runs past the card");
        Assert.That(text.Result.Length, Is.GreaterThan(1), "the long result wraps"); Assert.That(text.Lines[0].Length, Is.GreaterThan(1), "the long line wraps");
        float tx = BookLayout.RowTextX + Tw(row.Title, 23f, "") + 16f; Assert.That(text.Where, Does.EndWith("…")); Assert.That(tx + Tw(text.Where, 19f, ""), Is.LessThanOrEqualTo(BookLayout.RowW - 40f + .01f), "the where is cut where the card ends");
        Assert.That(BookLayout.RowX + BookLayout.RowW, Is.LessThan(BookLayout.SignX - 54f), "the card stops short of the signs");
        float want = BookLayout.RowH + 28f + BookLayout.LineH + (text.Lines[0].Length - 1) * BookLayout.RowLineH + 48f + (text.Result.Length - 1) * BookLayout.RowLineH;
        Assert.That(it.At.height, Is.EqualTo(want).Within(.01f), "the card is as tall as its wrapped words"); var next = r.Of(BookSlot.Rows).Skip(1).First(); Assert.That(next.At.y, Is.GreaterThanOrEqualTo(it.At.yMax - .01f), "the next row starts below it");
    }

    [Test] public void ATimeTheEngineHasNotSetDropsItsClauseNeverAQuestionMark()
    {
        Assert.That(Clause.Fill("Robots are coming. On the street by {clock}.", "{clock}", "2:08"), Is.EqualTo("Robots are coming. On the street by 2:08."));
        Assert.That(Clause.Fill("Robots are coming. On the street by {clock}.", "{clock}", null), Is.EqualTo("Robots are coming."));
        Assert.That(Clause.Fill("Wait {tick} · car held", "{tick}", null), Is.EqualTo("car held"), "a plate's part goes, the rest stays");
        Assert.That(Clause.Fill("Seals at {seal}", "{seal}", null), Is.Empty, "a line with nothing else shows nothing");
        Assert.That(Clause.ClockAfter("The desk sends the response (level 2, watchful): 1 robot, 0 units, on the street 2:08.2.", "on the street"), Is.EqualTo("2:08"));
        // the answered alarm with robots only (no police unit to wait for) takes the engine's own arrival
        var h = Host.Fresh(); var line = new Suborned.Sim.LogLine { Tick = 1182, Code = "alarm.dispatches", Text = "The desk sends the response (level 2, watchful): 1 robot, 0 units, on the street 2:08.2." };
        var got = (string)Host.Call(h, "FillCard", "Robots are coming. On the street by {clock}.", line, null);
        Assert.That(got, Is.EqualTo("Robots are coming. On the street by 2:08."));
        Assert.That(got, Does.Not.Contain("?"));
    }
}
