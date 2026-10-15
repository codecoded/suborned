// Redline's engine review (briefs/011/review/redline-engine.html), each finding as a test that would have caught it.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Suborned.Sim;
using Xunit;

public class RedlineTests
{
    static readonly Engine.Inputs I = Repo.Inputs;
    static void Rejects(string rulebook, string people, string site, string what)
    {
        var e = Record.Exception(() => Engine.FromTexts(rulebook, people, site, I.People));
        Assert.True(e is FormatException, $"{what}: loaded, should be rejected at load ({e?.GetType().Name})");
    }
    static string Sub(string text, string from, string to) { Assert.Contains(from, text); return text.Replace(from, to); }

    [Fact]
    public void E1_unknown_is_rejected_at_load_everywhere_the_engine_reads()
    {
        string rb = I.Rulebook, pr = I.PeopleRules, st = I.Site;
        Rejects(Sub(rb, "\"schema\": \"suborned.rulebook/1\",", "\"schema\": \"suborned.rulebook/1\", \"stray\": 1,"), pr, st, "an unknown top-level key");
        Rejects(Sub(rb, "\"watch-s\": { \"v\": 20,", "\"watch-s\": { \"vv\": 1, \"v\": 52,"), pr, st, "an unknown key in a value");
        Rejects(Sub(rb, "\"speed\": \"v:op-speed-cm-s\"", "\"sped\": \"v:op-speed-cm-s\", \"speed\": \"v:op-speed-cm-s\""), pr, st, "an unknown key in the cell section");
        Rejects(Sub(rb, "\"ask\": { \"fit\": \"refused\"", "\"ask\": { \"fit\": \"refusedd\""), pr, st, "a fit typo");
        Rejects(Sub(rb, "\"when\": [{ \"k\": \"reports-approach\" }", "\"when\": [{ \"k\": \"mood\" }"), pr, st, "an unknown condition kind in the reports");
        Rejects(Sub(rb, "\"add\": [{ \"scale\": \"bravery\", \"by\": \"v:bravery-per-job\" }]", "\"add\": [{ \"scale\": \"bravry\", \"by\": \"v:bravery-per-job\" }]"), pr, st, "a ladder on an unknown scale");
        Rejects(Sub(rb, "{ \"k\": \"temperament\", \"in\": [\"mercenary\"] }], \"usd\": \"v:mercenary-hero-usd\"", "{ \"k\": \"temperament\", \"in\": [\"mercenaryy\"] }], \"usd\": \"v:mercenary-hero-usd\""), pr, st, "a temperament typo in a condition");
        Rejects(Sub(rb, "\"debt-cleared\": { \"standing\": \"hero\"", "\"debt-cleared\": { \"standing\": \"heroo\""), pr, st, "a traceable standing typo");
        Rejects(Sub(rb, "\"knownBy\": [\"scout.listen\"]", "\"knownBy\": [\"scout.lisen\"]"), pr, st, "knownBy naming no event");
        Rejects(rb, Sub(pr, "\"schema\": \"suborned.people.rules/1\",", "\"schema\": \"suborned.people.rules/1\", \"stray\": 1,"), st, "an unknown key at the people rules' root");
        Rejects(rb, Sub(pr, "\"respondsTo\": [\"help\"], \"refuses\": [\"pay\"]", "\"respondsToo\": [], \"respondsTo\": [\"help\"], \"refuses\": [\"pay\"]"), st, "an unknown key in a temperament");
        Rejects(rb, pr, Sub(st, "\"schema\": \"suborned.sim.site/1\",", "\"schema\": \"suborned.sim.site/1\",\n \"stray\": 1,"), "an unknown key at the site's root");
        Rejects(rb, pr, Regex.Replace(st, "\"purse\": 100000,", "\"purse\": 100000.5,"), "a float purse");
        Rejects(Sub(rb, "\"watch-s\": { \"v\": 20,", "\"watch-s\": { \"v\": 0,"), pr, st, "a 0 s action (E3)");
        Rejects(Sub(rb, "\"pointBlank\": \"v:point-blank-m\",", "\"pointBlank\": \"v:point-blank-m\", \"spread\": \"v:point-blank-m\","), pr, st, "an unknown key in the weapons section (Sky 2026-10-15 fights)");
        Rejects(Sub(rb, "\"pistol-eff-m\": { \"v\": 15,", "\"pistol-eff-m\": { \"v\": 26,"), pr, st, "a pistol's effective band beyond its max");
        Rejects(Sub(rb, "\"health-guard\": { \"v\": 3,", "\"health-guard\": { \"v\": 0,"), pr, st, "a health of 0");
        Rejects(Sub(rb, "\"draw-ticks\": { \"v\": 10,", "\"draw-ticks\": { \"v\": 0,"), pr, st, "a 0-tick draw (unit ticks ≥ 1)");
        Rejects(rb, pr, Sub(st, "\"arms\": 3,", "\"arms\": -1,"), "negative Arms");
        Rejects(Sub(rb, "\"turn\": \"aborted\",", "\"turn\": \"forgotten\","), pr, st, "a cancel line the engine doesn't do (Sky 2026-10-15 playtest)");
        Rejects(Sub(rb, "\"watch\": \"lost\",", "\"watch\": \"kept\","), pr, st, "a fixed cancel line changed");
        Rejects(Sub(rb, "{ \"id\": \"not-yet\", \"when\": [],", "{ \"id\": \"not-yet\", \"when\": [{ \"k\": \"cash-covers\" }],"), pr, st, "cash-covers in an outcome (it recursed until the stack overflowed)");
    }

    [Fact]
    public void E1_step_never_throws()
    {
        var g = Engine.New(I);
        for (int i = 0; i < 5000 && !g.Over; i++) g.Step();
        Assert.True(g.FailCode != "engine-error", g.FailText);
    }

    [Fact]
    public void E2_a_refusal_gives_back_exactly_what_was_taken()
    {
        var s = new Start(); s.Known["guard"] = new List<string> { "routine" };
        s.Temperament["guard"] = "loyal"; // the premise: Menace refuses a Loyal person's Pay, whoever the cast's guard is (r016's is a Mercenary)
        var g = Engine.New(I, s);
        var run = new Run(g);
        run.To(run.Send("fixer", "d:Office approach"));
        var q = g.Preview("pay", "guard");
        long before = g.Cash;
        Assert.True(run.Do("fixer", "turn", "guard", "pay"));
        long taken = before - g.Cash;
        Assert.Equal(q.Usd, taken);
        g.Standing = I.Rules.MenaceAt; // standing moves while the order runs: Menace refuses a Loyal's Pay
        run.Done("fixer");
        Assert.Contains(g.Log, l => l.Code == "turn.pay.refused");
        Assert.Equal(before, g.Cash);
    }

    [Fact]
    public void E5_cones_hold_at_district_scale()
    {
        Assert.False(G.InCone(new Pt(0, 0), new Pt(65536, 0), 3000, 1500, 300, new Pt(65536, 0)));
        Assert.False(G.InCone(new Pt(0, 0), new Pt(46341, 0), 3000, 1500, 300, new Pt(92682, 0)));
        Assert.True(G.InCone(new Pt(0, 0), new Pt(65536, 0), 3000, 1500, 300, new Pt(2900, 100)));
    }

    [Fact]
    public void E4_crlf_and_bom_replay_to_the_same_hashes()
    {
        string Win(string s) => "\uFEFF" + s.Replace("\n", "\r\n");
        var w = Engine.FromTexts(Win(I.Rulebook), Win(I.PeopleRules), Win(I.Site), Win(I.People));
        Assert.Equal(I.Hash, w.Hash);
        var a = Scenarios.Run(I, Scenarios.Find("S10")!).G; var b = Scenarios.Run(w, Scenarios.Find("S10")!).G;
        Assert.Equal(a.Checkpoints, b.Checkpoints);
    }

    [Fact]
    public void E4_the_checkpoint_hash_covers_what_the_debrief_reads()
    {
        var a = Engine.New(I); var b = Engine.New(I);
        b.GuardReachedCafe = true;
        Assert.NotEqual(Hash.State(a), Hash.State(b));
    }

    [Fact]
    public void E11_json_edges()
    {
        Assert.Equal(1, JNode.Parse("\uFEFF{\"a\": 1}")["a"].Int());
        Assert.Throws<FormatException>(() => JNode.Parse("{\"a\": 01}"));
        Assert.Throws<FormatException>(() => JNode.Parse("{\"a\": \"x\ty\"}"));
        Assert.Throws<FormatException>(() => JNode.Parse("{\"a\": \"\\u12\"}"));
        Assert.Throws<FormatException>(() => JNode.Parse("{\"a\": -}"));
    }

    [Theory]
    [InlineData("sv-SE")] [InlineData("ar-SA")] [InlineData("fa-IR")] [InlineData("tr-TR")]
    public void E7_every_report_is_the_same_in_every_culture(string culture)
    {
        string All()
        {
            var parts = new List<string>();
            foreach (var s in Scenarios.All) { var r = Scenarios.Run(I, s); parts.Add(Report.Json(s.Id, r.G, r.Checks, true)); }
            return string.Join("\n", parts);
        }
        var keep = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture; var inv = All();
            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture); var other = All();
            if (inv != other)
            {
                var a = inv.Split('\n'); var b = other.Split('\n'); int i = 0; while (i < a.Length && i < b.Length && a[i] == b[i]) i++;
                int j = 0; while (j < a[i].Length && j < b[i].Length && a[i][j] == b[i][j]) j++;
                Assert.Fail($"{culture} differs: …{a[i].Substring(Math.Max(0, j - 60), Math.Min(120, a[i].Length - Math.Max(0, j - 60)))}");
            }
        }
        finally { Thread.CurrentThread.CurrentCulture = keep; }
    }
}
